using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>Where one contract stands in its run of months.</summary>
    public sealed class ScpContractSequence
    {
        public long ContractKey;
        /// <summary>YYYYMM of the contract's start; 0 = no start date.</summary>
        public int StartPeriod;
        /// <summary>YYYYMM of the last month anything on the contract can bill: the contract's end, or a
        /// machine's own later end. 0 = open-ended.</summary>
        public int EndPeriod;
        /// <summary>The first month this book bills (months before it were billed elsewhere).</summary>
        public int BillFrom;
        /// <summary>Months with at least one stamp in this book (an invoice, or a no-charge line).</summary>
        public HashSet<int> Billed = new HashSet<int>();
        /// <summary>Months skipped on purpose, with the reason.</summary>
        public Dictionary<int, string> Skipped = new Dictionary<int, string>();
        /// <summary>Months that have a stamp but also a counter or charge that should have been stamped
        /// and was not -- billed in part, or an invoice deleted since. Oldest first. Each one is only a
        /// suspect until the billing engine confirms something is still to bill.</summary>
        public List<int> Unfinished = new List<int>();
        /// <summary>The first month from BillFrom that is neither billed nor skipped.</summary>
        public int Next;

        /// <summary>The contract bills its rental in advance (ATP-10) and has a rental to bill: its first
        /// bill is the first month's rental, in the month BEFORE the start.</summary>
        public bool RentalInAdvance;

        /// <summary>The first month the sequence counts from: the later of the start and BillFrom -- a
        /// month before the start for a contract that bills its rental in advance.</summary>
        public int First
        {
            get
            {
                int start = StartPeriod > 0 && RentalInAdvance ? ScpBillingSequence.AddMonths(StartPeriod, -1) : StartPeriod;
                return start > 0 && start > BillFrom ? start : BillFrom;
            }
        }

        /// <summary>Months in the contract; 0 = open-ended or no start date.</summary>
        public int Total
        {
            get { return StartPeriod > 0 && EndPeriod >= StartPeriod ? ScpBillingSequence.Index(StartPeriod, EndPeriod) : 0; }
        }

        /// <summary>The last month with anything to bill, when it is not the end: a contract that bills
        /// nothing but a rental in advance paid its last month's rent the month before (ATP-10). 0 = the end.</summary>
        public int LastBillPeriod;
        private int LastDue { get { return LastBillPeriod > 0 ? LastBillPeriod : EndPeriod; } }

        /// <summary>Every month of the contract is billed or skipped.</summary>
        public bool Complete { get { return EndPeriod > 0 && Next > LastDue; } }

        public bool IsHandled(int period) { return Billed.Contains(period) || Skipped.ContainsKey(period); }

        /// <summary>How many months are done when <paramref name="open"/> is the month being worked:
        /// every month before it. Counted from the contract's start, so a contract taken over in its
        /// ninth month shows 8 done before this book bills anything.</summary>
        public int DoneBefore(int open)
        {
            int from = StartPeriod > 0 ? StartPeriod : First;
            if (from <= 0 || open <= from) return 0;
            int done = ScpBillingSequence.Index(from, open) - 1;
            int total = Total;
            return total > 0 && done > total ? total : done;
        }

        /// <summary>"8/36", or "8" for a contract with no end.</summary>
        public string ProgressText(int open)
        {
            int total = Total;
            return total > 0 ? DoneBefore(open) + "/" + total : DoneBefore(open).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Months from <paramref name="open"/> through <paramref name="upTo"/> (and not past the
        /// contract's end) that are still to bill.</summary>
        public int DueFrom(int open, int upTo)
        {
            if (open <= 0) return 0;
            int last = LastDue > 0 && LastDue < upTo ? LastDue : upTo;
            int n = 0;
            for (int p = open, guard = 0; p <= last && guard < 1200; p = ScpBillingSequence.AddMonths(p, 1), guard++)
                if (!IsHandled(p)) n++;
            return n;
        }

        /// <summary>The skip just before <paramref name="open"/>, if the month before it was skipped; 0 otherwise.</summary>
        public int SkipBefore(int open)
        {
            int prev = ScpBillingSequence.AddMonths(open, -1);
            return Skipped.ContainsKey(prev) ? prev : 0;
        }
    }

    /// <summary>
    /// A contract is billed one month after another. The next month to bill is not chosen by anyone:
    /// it is the first month, counting from where this book's billing starts, that has neither an
    /// invoice nor a recorded skip. So a month cannot be missed by picking the wrong one, and a
    /// contract that is behind shows how far.
    ///
    /// <para>"Billed" comes from the stamps billing leaves on the readings (zSCP2_MeterEntry.InvoicedDocNo,
    /// one per counter and month). A month with a stamp can still be unfinished -- one invoice of two
    /// went out, or an invoice was deleted and <see cref="ScpBillingRows.ReconcileDeletedInvoices"/>
    /// took its stamps back while a no-charge line's stamp stayed -- so every stamped month is also
    /// checked for a counter that should carry a stamp and does not (<see cref="ScpContractSequence.Unfinished"/>).</para>
    ///
    /// <para>Where billing starts is zSCP2_Contract.BillFromPeriod, written when a contract is taken
    /// from another book; with none, the contract's start month.</para>
    /// </summary>
    public static class ScpBillingSequence
    {
        public static int Period(int year, int month) { return year * 100 + month; }
        public static int Period(DateTime d) { return d.Year * 100 + d.Month; }
        public static int YearOf(int period) { return period / 100; }
        public static int MonthOf(int period) { return period % 100; }

        public static int AddMonths(int period, int n)
        {
            if (period <= 0) return 0;
            int idx = YearOf(period) * 12 + (MonthOf(period) - 1) + n;
            return (idx / 12) * 100 + (idx % 12) + 1;
        }

        /// <summary>1 for the first month itself, 2 for the month after, and so on.</summary>
        public static int Index(int first, int period)
        {
            return (YearOf(period) * 12 + MonthOf(period)) - (YearOf(first) * 12 + MonthOf(first)) + 1;
        }

        public static string Name(int period)
        {
            if (period <= 0) return "";
            return new DateTime(YearOf(period), MonthOf(period), 1).ToString("MMM yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>The sequences of the contracts given.</summary>
        public static Dictionary<long, ScpContractSequence> Load(DBSetting db, ICollection<long> contractKeys)
        {
            Dictionary<long, ScpContractSequence> map = new Dictionary<long, ScpContractSequence>();
            if (db == null || contractKeys == null || contractKeys.Count == 0) return map;
            bool includeExpired = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
            List<long> keys = new List<long>(contractKeys);
            for (int i = 0; i < keys.Count; i += 400)
            {
                List<string> part = new List<string>();
                for (int j = i; j < keys.Count && j < i + 400; j++) part.Add(keys[j].ToString(CultureInfo.InvariantCulture));
                string inList = string.Join(",", part.ToArray());

                // The end is the contract's end, or a machine's own end when it runs longer. A machine
                // with no end of its own ends with the contract.
                DataTable c = db.GetDataTable(
                    "SELECT c.ContractKey, c.ServiceStartDate, c.ServiceExpiryDate, c.BillFromPeriod, " +
                    "       ISNULL(c.RentalBasis,'A') AS RentalBasis, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item ir JOIN dbo.zSCP2_ItemMeter mr ON mr.ItemKey = ir.ItemKey " +
                    "         WHERE ir.ContractKey = c.ContractKey AND ISNULL(ir.Inactive,'N') = 'N' " +
                    "           AND " + ScpStrategy.RentalRoleSql("mr") + ") AS RentalMeters, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item io JOIN dbo.zSCP2_ItemMeter mo ON mo.ItemKey = io.ItemKey " +
                    "         WHERE io.ContractKey = c.ContractKey AND ISNULL(io.Inactive,'N') = 'N' " +
                    "           AND NOT " + ScpStrategy.RentalRoleSql("mo") + " AND UPPER(ISNULL(mo.MeterRole,'')) <> 'WAIVE') AS OtherMeters, " +
                    "       (SELECT MAX(i.ServiceExpiryDate) FROM dbo.zSCP2_Item i " +
                    "         WHERE i.ContractKey = c.ContractKey AND ISNULL(i.Inactive,'N') = 'N') AS LastItemEnd, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item i " +
                    "         WHERE i.ContractKey = c.ContractKey AND ISNULL(i.Inactive,'N') = 'N') AS ActiveItems, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_Item i " +
                    "         WHERE i.ContractKey = c.ContractKey AND ISNULL(i.Inactive,'N') = 'N' AND i.ServiceExpiryDate IS NULL) AS ItemsNoEnd " +
                    "  FROM dbo.zSCP2_Contract c WHERE c.ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in c.Rows)
                {
                    ScpContractSequence s = new ScpContractSequence();
                    s.ContractKey = Convert.ToInt64(r["ContractKey"]);
                    s.StartPeriod = r["ServiceStartDate"] == DBNull.Value ? 0 : Period(Convert.ToDateTime(r["ServiceStartDate"]));
                    int contractEnd = r["ServiceExpiryDate"] == DBNull.Value ? 0 : Period(Convert.ToDateTime(r["ServiceExpiryDate"]));
                    int itemEnd = r["LastItemEnd"] == DBNull.Value ? 0 : Period(Convert.ToDateTime(r["LastItemEnd"]));
                    int activeItems = Convert.ToInt32(r["ActiveItems"]);
                    int itemsNoEnd = Convert.ToInt32(r["ItemsNoEnd"]);
                    if (contractEnd > 0) s.EndPeriod = itemEnd > contractEnd ? itemEnd : contractEnd;
                    else s.EndPeriod = activeItems > 0 && itemsNoEnd == 0 ? itemEnd : 0;
                    s.RentalInAdvance = Convert.ToString(r["RentalBasis"]).Trim().ToUpperInvariant() == "P"
                                        && Convert.ToInt32(r["RentalMeters"]) > 0;
                    s.BillFrom = r["BillFromPeriod"] != DBNull.Value && Convert.ToInt32(r["BillFromPeriod"]) > 0
                        ? Convert.ToInt32(r["BillFromPeriod"])
                        : (s.RentalInAdvance && s.StartPeriod > 0 ? AddMonths(s.StartPeriod, -1) : s.StartPeriod);
                    if (s.RentalInAdvance && Convert.ToInt32(r["OtherMeters"]) == 0 && s.EndPeriod > 0)
                        s.LastBillPeriod = AddMonths(s.EndPeriod, -1);
                    map[s.ContractKey] = s;
                }

                DataTable b = db.GetDataTable(
                    "SELECT DISTINCT i.ContractKey, e.PeriodYear * 100 + e.PeriodMonth AS P " +
                    "  FROM dbo.zSCP2_MeterEntry e " +
                    "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    " WHERE i.ContractKey IN (" + inList + ") AND ISNULL(e.InvoicedDocNo,'') <> ''", false);
                foreach (DataRow r in b.Rows)
                {
                    ScpContractSequence s;
                    if (map.TryGetValue(Convert.ToInt64(r["ContractKey"]), out s)) s.Billed.Add(Convert.ToInt32(r["P"]));
                }

                DataTable k = db.GetDataTable(
                    "SELECT ContractKey, PeriodYear * 100 + PeriodMonth AS P, ISNULL(Reason,'') AS Reason " +
                    "  FROM dbo.zSCP2_ContractPeriodSkip WHERE UndoneAt IS NULL AND ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in k.Rows)
                {
                    ScpContractSequence s;
                    if (map.TryGetValue(Convert.ToInt64(r["ContractKey"]), out s)) s.Skipped[Convert.ToInt32(r["P"])] = Convert.ToString(r["Reason"]);
                }

                // Stamped months with a counter the engine would still bill: not a waive, not a
                // committed minimum, and either a flat charge or a counter that is read -- the same
                // test as the board's "still to bill", in SQL, for every contract at once.
                string expiry = includeExpired ? "" :
                    "   AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                    "        OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(s.Y, s.M, 1)) ";
                DataTable u = db.GetDataTable(
                    "SELECT DISTINCT s.ContractKey, s.Y * 100 + s.M AS P " +
                    "  FROM (SELECT DISTINCT i2.ContractKey, e.PeriodYear AS Y, e.PeriodMonth AS M " +
                    "          FROM dbo.zSCP2_MeterEntry e " +
                    "          JOIN dbo.zSCP2_ItemMeter m2 ON m2.ItemMeterKey = e.ItemMeterKey " +
                    "          JOIN dbo.zSCP2_Item i2 ON i2.ItemKey = m2.ItemKey " +
                    "         WHERE i2.ContractKey IN (" + inList + ") AND ISNULL(e.InvoicedDocNo,'') <> '') s " +
                    "  JOIN dbo.zSCP2_Item i ON i.ContractKey = s.ContractKey AND ISNULL(i.Inactive,'N') = 'N' " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey AND ISNULL(c.Inactive,'N') = 'N' " +
                    "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey " +
                    "  LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                    " WHERE UPPER(ISNULL(m.MeterRole,'')) <> 'WAIVE' AND ISNULL(mt.IsRentalWaive,'N') <> 'Y' " +
                    "   AND NOT (ISNULL(m.MinimumCharges,0) > 0 AND (UPPER(ISNULL(m.MeterRole,'')) = 'COMMIT' " +
                    "            OR UPPER(ISNULL(m.MeterTypeCode,'')) LIKE 'MIN%')) " +
                    "   AND (ISNULL(mt.IsFlatCharge,'N') = 'Y' OR UPPER(ISNULL(m.MeterRole,'')) NOT IN ('RENTAL','COMMIT')) " +
                    expiry +
                    // ATP-10, rental in advance: before the start only the rental is due, and in the
                    // rental's last month it is not (it was paid the month before).
                    "   AND NOT (ISNULL(c.RentalBasis,'A') = 'P' AND c.ServiceStartDate IS NOT NULL " +
                    "            AND s.Y * 100 + s.M < YEAR(c.ServiceStartDate) * 100 + MONTH(c.ServiceStartDate) " +
                    "            AND NOT " + ScpStrategy.RentalRoleSql("m") + ") " +
                    "   AND NOT (ISNULL(c.RentalBasis,'A') = 'P' AND " + ScpStrategy.RentalRoleSql("m") + " " +
                    "            AND COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NOT NULL " +
                    "            AND s.Y * 100 + s.M >= YEAR(COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate)) * 100 " +
                    "                                  + MONTH(COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate))) " +
                    "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry x WHERE x.ItemMeterKey = m.ItemMeterKey " +
                    "                      AND x.PeriodYear = s.Y AND x.PeriodMonth = s.M AND ISNULL(x.InvoicedDocNo,'') <> '')", false);
                foreach (DataRow r in u.Rows)
                {
                    ScpContractSequence s;
                    if (map.TryGetValue(Convert.ToInt64(r["ContractKey"]), out s)) s.Unfinished.Add(Convert.ToInt32(r["P"]));
                }
            }

            foreach (ScpContractSequence s in map.Values)
            {
                int p = s.First;
                if (p <= 0) p = Period(DateTime.Today);
                for (int guard = 0; guard < 1200 && s.IsHandled(p); guard++) p = AddMonths(p, 1);
                s.Next = p;
                s.Unfinished.Sort();
            }
            return map;
        }

        /// <summary>Records that a month of a contract is not billed, and why.</summary>
        public static void Skip(DBSetting db, long contractKey, int period, string reason, string userId)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ContractPeriodSkip WHERE ContractKey=@c AND PeriodYear=@y AND PeriodMonth=@m AND UndoneAt IS NULL) " +
                    "INSERT INTO dbo.zSCP2_ContractPeriodSkip (ContractKey, PeriodYear, PeriodMonth, Reason, SkippedBy, SkippedAt) " +
                    "VALUES (@c, @y, @m, @r, @u, GETDATE())", cn))
                {
                    cmd.Parameters.AddWithValue("@c", contractKey);
                    cmd.Parameters.AddWithValue("@y", YearOf(period));
                    cmd.Parameters.AddWithValue("@m", MonthOf(period));
                    string r = (reason ?? "").Trim();
                    cmd.Parameters.AddWithValue("@r", r.Length > 200 ? r.Substring(0, 200) : r);
                    cmd.Parameters.AddWithValue("@u", (userId ?? "").Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Puts a skipped month back to be billed. The skip stays on record as undone.</summary>
        public static void UndoSkip(DBSetting db, long contractKey, int period, string userId)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_ContractPeriodSkip SET UndoneAt = GETDATE(), UndoneBy = @u " +
                    " WHERE ContractKey=@c AND PeriodYear=@y AND PeriodMonth=@m AND UndoneAt IS NULL", cn))
                {
                    cmd.Parameters.AddWithValue("@c", contractKey);
                    cmd.Parameters.AddWithValue("@y", YearOf(period));
                    cmd.Parameters.AddWithValue("@m", MonthOf(period));
                    cmd.Parameters.AddWithValue("@u", (userId ?? "").Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Sets the first month this book bills a contract; 0 puts the contract's start back.</summary>
        public static void SetBillFrom(DBSetting db, long contractKey, int period)
        {
            db.ExecuteNonQuery(
                "UPDATE dbo.zSCP2_Contract SET BillFromPeriod = " +
                (period > 0 ? period.ToString(CultureInfo.InvariantCulture) : "NULL") +
                ", LastModified = GETDATE() WHERE ContractKey = " + contractKey);
        }

        /// <summary>The first month billed here, as stored; 0 = none stored.</summary>
        public static int BillFromOf(DBSetting db, long contractKey)
        {
            object o = db.ExecuteScalar("SELECT BillFromPeriod FROM dbo.zSCP2_Contract WHERE ContractKey = " + contractKey);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        /// <summary>The invoice numbers of a contract's month (real invoices only, not no-charge stamps).</summary>
        public static List<string> InvoiceNumbers(DBSetting db, long contractKey, int period)
        {
            List<string> list = new List<string>();
            DataTable t = db.GetDataTable(
                "SELECT DISTINCT e.InvoicedDocNo FROM dbo.zSCP2_MeterEntry e " +
                "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                " WHERE i.ContractKey = " + contractKey + " AND e.PeriodYear = " + YearOf(period) +
                "   AND e.PeriodMonth = " + MonthOf(period) + " AND e.InvoicedDocKey IS NOT NULL " +
                " ORDER BY e.InvoicedDocNo", false);
            foreach (DataRow r in t.Rows) list.Add(Convert.ToString(r["InvoicedDocNo"]).Trim());
            return list;
        }
    }
}
