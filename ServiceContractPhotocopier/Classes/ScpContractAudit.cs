using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Field-level change audit for contracts (and their items/meters) — writes append-only rows to
    /// zSCP2_ContractAudit inside the caller's transaction. One SAVE = one ChangeSetId. The audit
    /// answers "what did this field say on date X" (June report printed ABC, October EFD):
    ///   value as of @asof = last NewValue changed on/before @asof
    ///                       else OldValue of the EARLIEST change after @asof
    ///                       else the current column value (never changed).
    /// </summary>
    public static class ScpContractAudit
    {
        public const string SOURCE_CONTRACT = "CONTRACT";
        public const string SOURCE_STRATEGY_APPLY = "STRATEGY-APPLY";
        public const string SOURCE_RENTAL = "RENTAL";
        public const string SOURCE_OWNERSHIP = "OWNERSHIP";

        // Every audited zSCP2_Contract column: UpdateContract's set + SaveMoreHeader's set + v5's two.
        private static readonly string[] Cols = new string[]
        {
            "ContractNo","ContractTypeCode","DebtorCode","ContractDate",
            "ServiceStartDate","ServiceExpiryDate","ContractValue","BillingDay","BillOnMonthEnd","BillingMode",
            "Address1","Attention","Phone","TermCode","AreaCode","StaffCode","ReferenceNo","Description",
            "Remark1","Remark2","Note","DeptNo","ProjNo","Inactive","InactiveDate","InactiveReason",
            "City","PostalCode","State","Country","Fax","Ref1","Ref2","Ref3","Ref4",
            "DelBranchCode","DelBranchName","DelAddress","DelCity","DelPostalCode","DelState","DelCountry",
            "DelPhone","DelFax","DelEmail","DelContactPerson",
            "StrategyCode","RentalSeparateInvoice","FOCResetUnit","FOCResetN"
        };

        /// <summary>Reads the audited columns of one contract as normalised strings (dates yyyy-MM-dd,
        /// decimals invariant trimmed, NULL -> ""). Missing columns (older schema) read as "".</summary>
        public static Dictionary<string, string> Snapshot(SqlConnection cn, SqlTransaction tx, long contractKey)
        {
            Dictionary<string, string> snap = new Dictionary<string, string>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder("SELECT ");
            for (int i = 0; i < Cols.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("[").Append(Cols[i]).Append("]");
            }
            sb.Append(" FROM dbo.zSCP2_Contract WHERE ContractKey=@ck");
            using (SqlCommand cmd = new SqlCommand(sb.ToString(), cn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", contractKey);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        for (int i = 0; i < Cols.Length; i++)
                            snap[Cols[i]] = Normalise(r.IsDBNull(i) ? null : r.GetValue(i));
                }
            }
            return snap;
        }

        /// <summary>Diffs two snapshots and writes ONE audit row per changed field (shared ChangeSetId).
        /// Returns the number of rows written. Never throws — audit must not block the save.</summary>
        public static int WriteDiff(SqlConnection cn, SqlTransaction tx, long contractKey, string contractNo,
            Dictionary<string, string> before, Dictionary<string, string> after, string source)
        {
            if (before == null || after == null) return 0;
            int written = 0;
            Guid changeSet = Guid.NewGuid();
            try
            {
                foreach (string col in Cols)
                {
                    string ov, nv;
                    before.TryGetValue(col, out ov);
                    after.TryGetValue(col, out nv);
                    if ((ov ?? "") == (nv ?? "")) continue;
                    WriteRow(cn, tx, contractKey, contractNo, null, null, changeSet, source, col, ov, nv);
                    written++;
                }
            }
            catch { }
            return written;
        }

        /// <summary>Writes one audit row. Used by WriteDiff and directly by Rental Maintenance /
        /// Apply-Strategy (which pass ItemKey/ItemMeterKey). Never throws.</summary>
        public static void WriteRow(SqlConnection cn, SqlTransaction tx, long contractKey, string contractNo,
            long? itemKey, long? itemMeterKey, Guid changeSet, string source,
            string field, string oldValue, string newValue)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_ContractAudit " +
                    "(ContractKey, ContractNo, ItemKey, ItemMeterKey, ChangeSetId, ChangeSource, FieldName, OldValue, NewValue, ChangedBy) " +
                    "VALUES (@ck, @no, @ik, @mk, @cs, @src, @f, @o, @n, @by)", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@ck", contractKey);
                    cmd.Parameters.AddWithValue("@no", contractNo ?? "");
                    cmd.Parameters.AddWithValue("@ik", itemKey.HasValue ? (object)itemKey.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@mk", itemMeterKey.HasValue ? (object)itemMeterKey.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@cs", changeSet);
                    cmd.Parameters.AddWithValue("@src", source ?? "");
                    cmd.Parameters.AddWithValue("@f", field ?? "");
                    cmd.Parameters.AddWithValue("@o", (object)oldValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@n", (object)newValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@by", CurrentUser());
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        /// <summary>Single "record created" marker for brand-new contracts.</summary>
        public static void WriteCreated(SqlConnection cn, SqlTransaction tx, long contractKey, string contractNo, string source)
        {
            WriteRow(cn, tx, contractKey, contractNo, null, null, Guid.NewGuid(), source, "CREATED", null, contractNo);
        }

        internal static string Normalise(object v)
        {
            if (v == null || v == DBNull.Value) return "";
            if (v is DateTime) return ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss").EndsWith("00:00:00")
                ? ((DateTime)v).ToString("yyyy-MM-dd") : ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss");
            if (v is decimal) return ((decimal)v).ToString("0.######", CultureInfo.InvariantCulture);
            if (v is double) return ((double)v).ToString("0.######", CultureInfo.InvariantCulture);
            return v.ToString();
        }

        internal static string CurrentUser()
        {
            try
            {
                AutoCount.Authentication.UserSession s = AutoCount.Authentication.UserSession.CurrentUserSession;
                return s != null ? (s.LoginUserID ?? "") : "";
            }
            catch { return ""; }
        }
    }
}
