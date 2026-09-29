using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One item this book needs before a contract can be taken or billed: a machine's model,
    /// or the item a meter type posts its invoice lines under.</summary>
    public sealed class ScpItemNeed
    {
        public string ItemCode = "";
        public string Description = "";
        /// <summary>True for a machine's model; false for a meter type's charge item.</summary>
        public bool IsModel;
        /// <summary>The meter type to point at the item once it exists; "" for a model.</summary>
        public string MeterType = "";
        /// <summary>The meter type has no item code yet and is given this one.</summary>
        public bool SetOnType;
        /// <summary>The model's machines, for the list shown before anything is created.</summary>
        public string Serials = "";
        // How HQ keeps the same item, copied when HQ has it.
        public bool FromHq;
        public string ItemGroup = "";
        public string ItemType = "";
        public string Uom = "UNIT";
        public bool StockControl;
        public bool HasSerialNo;

        /// <summary>"IR ADV DX C3935I   COPIER iR-ADV DX C3935I   (model of IBT-C01)".</summary>
        public string Line()
        {
            if (ItemCode.Length == 0) return "   machine " + Serials + "  --  has no model at HQ; set one on the machine there";
            return "   " + ItemCode + "   " + Description + "   (" +
                   (IsModel ? "model of " + Serials : "meter type " + MeterType + (SetOnType ? ", set as its item code" : "")) +
                   (FromHq ? ", as at HQ" : "") + ")";
        }
    }

    /// <summary>
    /// Creates the items a contract needs in this book, in one go (user, 29/9: "你可以做一键创建 itemcode 吗").
    ///
    /// <para>What is missing is the same the take refuses over and the board shows as No item code
    /// (<see cref="ScpInterBillTake.ItemCodeProblems"/>): each machine's model, and each meter type's item.
    /// An item HQ has is copied from HQ -- description, unit, stock and serial control, and its group and
    /// type when this book has them; otherwise it is made from the meter type (a charge: no stock, no
    /// serial) or the model code (a machine: serial numbered). A meter type with no item code at all takes
    /// HQ's item code for it, else its own code, and is pointed at the new item.</para>
    ///
    /// <para>Items are made through AutoCount's own item maintenance (ItemDataAccess), not an INSERT: the
    /// item master is several tables and a unit, and a hand-written row would be missing one.</para>
    /// </summary>
    public static class ScpInterBillItems
    {
        /// <summary>What a take of these (HQ's) machines needs. Run after the take has added any meter
        /// type this book lacked, as <see cref="ScpInterBillTake.TakeContract"/> does before it refuses.</summary>
        public static List<ScpItemNeed> ForTake(DBSetting localDb, string hqCs, List<ScpRemoteMachine> machines, bool checkMachines)
        {
            List<string[]> models = new List<string[]>();
            List<string> types = new List<string>();
            if (machines != null)
                foreach (ScpRemoteMachine m in machines)
                {
                    if (checkMachines && !m.IsGroupItem) models.Add(new string[] { (m.ItemCode ?? "").Trim(), (m.SerialNumber ?? "").Trim() });
                    foreach (ScpRemoteMeter t in m.Meters)
                    {
                        string code = (t.MeterTypeCode ?? "").Trim();
                        if (code.Length > 0 && (t.MeterRole ?? "").Trim().ToUpperInvariant() != "NA" && !Has(types, code)) types.Add(code);
                    }
                }
            return Needs(localDb, hqCs, models, types);
        }

        /// <summary>What a contract already taken needs: its active machines' models and its counters'
        /// meter types.</summary>
        public static List<ScpItemNeed> ForContract(DBSetting localDb, string hqCs, long contractKey)
        {
            List<string[]> models = new List<string[]>();
            List<string> types = new List<string>();
            DataTable mi = localDb.GetDataTable(
                "SELECT LTRIM(RTRIM(ISNULL(ItemCode,''))) AS Code, ISNULL(SerialNumber,'') AS Serial FROM dbo.zSCP2_Item " +
                " WHERE ContractKey = " + contractKey + " AND ISNULL(Inactive,'N') = 'N' AND ISNULL(IsGroupItem,'N') = 'N'", false);
            foreach (DataRow r in mi.Rows) models.Add(new string[] { S(r["Code"]), S(r["Serial"]) });
            DataTable mt = localDb.GetDataTable(
                "SELECT DISTINCT m.MeterTypeCode FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                " WHERE i.ContractKey = " + contractKey + " AND ISNULL(i.Inactive,'N') = 'N' AND UPPER(ISNULL(m.MeterRole,'')) <> 'NA'", false);
            foreach (DataRow r in mt.Rows) if (S(r["MeterTypeCode"]).Length > 0) types.Add(S(r["MeterTypeCode"]));
            return Needs(localDb, hqCs, models, types);
        }

        private static List<ScpItemNeed> Needs(DBSetting localDb, string hqCs, List<string[]> models, List<string> types)
        {
            List<ScpItemNeed> needs = new List<ScpItemNeed>();

            // Machines, one need per model, naming its machines.
            foreach (string[] m in models)
            {
                if (m[0].Length > 0 && LocalItem(localDb, m[0])) continue;
                ScpItemNeed n = null;
                foreach (ScpItemNeed x in needs) if (x.IsModel && string.Equals(x.ItemCode, m[0], StringComparison.OrdinalIgnoreCase)) n = x;
                if (n == null)
                {
                    n = new ScpItemNeed();
                    n.IsModel = true;
                    n.ItemCode = m[0];
                    n.Description = m[0];
                    n.HasSerialNo = true;   // a machine is known by its serial
                    needs.Add(n);
                }
                n.Serials += (n.Serials.Length > 0 ? ", " : "") + m[1];
            }

            // Meter types whose item is missing: the item they name, or the one they should name.
            if (types.Count > 0)
            {
                Dictionary<string, string> hqItemOfType = HqTypeItems(hqCs, types);
                foreach (string code in types)
                {
                    DataTable d = localDb.GetDataTable(
                        "SELECT ISNULL(mt.[Description],'') AS Descr, ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS Item " +
                        "  FROM dbo.zSCP_MeterType mt WHERE mt.MeterTypeCode = N'" + code.Replace("'", "''") + "'", false);
                    if (d.Rows.Count == 0) continue;   // not a meter type of this book: the take adds it first
                    string item = S(d.Rows[0]["Item"]);
                    if (item.Length > 0 && LocalItem(localDb, item)) continue;
                    ScpItemNeed n = new ScpItemNeed();
                    n.MeterType = code;
                    n.SetOnType = item.Length == 0;
                    string hqItem;
                    n.ItemCode = item.Length > 0 ? item
                               : hqItemOfType.TryGetValue(code, out hqItem) && hqItem.Length > 0 ? hqItem
                               : code;
                    n.Description = S(d.Rows[0]["Descr"]).Length > 0 ? S(d.Rows[0]["Descr"]) : code;
                    needs.Add(n);
                }
            }

            // How HQ keeps each of these items, when it has them.
            List<string> codes = new List<string>();
            foreach (ScpItemNeed n in needs) if (n.ItemCode.Length > 0 && !Has(codes, n.ItemCode)) codes.Add(n.ItemCode);
            if (codes.Count > 0 && !string.IsNullOrEmpty(hqCs))
            {
                using (SqlConnection cn = new SqlConnection(hqCs))
                {
                    cn.Open();
                    List<string> names = new List<string>();
                    for (int i = 0; i < codes.Count; i++) names.Add("@c" + i);
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT ItemCode, ISNULL(Description,'') AS Description, ISNULL(ItemGroup,'') AS ItemGroup, ISNULL(ItemType,'') AS ItemType, " +
                        "       ISNULL(BaseUOM,'') AS BaseUOM, ISNULL(StockControl,'F') AS StockControl, ISNULL(HasSerialNo,'F') AS HasSerialNo " +
                        "  FROM dbo.Item WHERE ItemCode IN (" + string.Join(",", names.ToArray()) + ")", cn))
                    {
                        for (int i = 0; i < codes.Count; i++) cmd.Parameters.AddWithValue(names[i], codes[i]);
                        using (SqlDataReader r = cmd.ExecuteReader())
                            while (r.Read())
                                foreach (ScpItemNeed n in needs)
                                {
                                    if (!string.Equals(n.ItemCode, S(r["ItemCode"]), StringComparison.OrdinalIgnoreCase)) continue;
                                    n.FromHq = true;
                                    if (S(r["Description"]).Length > 0) n.Description = S(r["Description"]);
                                    n.ItemGroup = S(r["ItemGroup"]);
                                    n.ItemType = S(r["ItemType"]);
                                    if (S(r["BaseUOM"]).Length > 0) n.Uom = S(r["BaseUOM"]);
                                    n.StockControl = S(r["StockControl"]).ToUpperInvariant() == "T";
                                    n.HasSerialNo = S(r["HasSerialNo"]).ToUpperInvariant() == "T";
                                }
                    }
                }
            }
            return needs;
        }

        /// <summary>
        /// Makes the items and points the meter types at theirs. An item that exists by now is left as it
        /// is. Returns what could not be done, or "" -- <paramref name="made"/> lists what was.
        /// </summary>
        public static string Create(DBSetting localDb, List<ScpItemNeed> needs, string userId, out List<string> made)
        {
            made = new List<string>();
            List<string> failed = new List<string>();
            if (localDb == null || needs == null) return "";
            AutoCount.Authentication.UserSession ses = AutoCount.Authentication.UserSession.CurrentUserSession;
            string who = (userId ?? "").Trim().Length > 0 ? userId.Trim() : "ADMIN";
            foreach (ScpItemNeed n in needs)
            {
                if (n.ItemCode.Length == 0) { failed.Add(n.Line().Trim()); continue; }
                try
                {
                    if (!LocalItem(localDb, n.ItemCode))
                    {
                        AutoCount.Stock.Item.ItemDataAccess da = AutoCount.Stock.Item.ItemDataAccess.Create(ses, localDb);
                        AutoCount.Stock.Item.ItemEntity it = da.NewItem();
                        it.ItemCode = n.ItemCode;
                        it.Description = n.Description.Length > 100 ? n.Description.Substring(0, 100) : n.Description;
                        // Group and type are this book's own lists: taken over only when the code is here.
                        if (n.ItemGroup.Length > 0 && Exists(localDb, "ItemGroup", "ItemGroup", n.ItemGroup)) it.ItemGroup = n.ItemGroup;
                        if (n.ItemType.Length > 0 && Exists(localDb, "ItemType", "ItemType", n.ItemType)) it.ItemType = n.ItemType;
                        // NewItem() comes with one blank unit row: name that one rather than adding a second.
                        if (it.UomCount > 0) it.GetUom(0).Uom = n.Uom;
                        else it.NewUom(n.Uom, 1m);
                        it.BaseUom = n.Uom;
                        it.SalesUom = n.Uom;
                        it.PurchaseUom = n.Uom;
                        it.ReportUom = n.Uom;
                        it.StockControl = n.StockControl;
                        it.HasSerialNo = n.HasSerialNo;
                        it.IsActive = true;
                        da.SaveData(it, who);
                        made.Add(n.ItemCode);
                    }
                    if (n.SetOnType && n.MeterType.Length > 0)
                    {
                        using (SqlConnection cn = new SqlConnection(localDb.ConnectionString))
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.zSCP_MeterType SET ACItemCode = @i, LastModified = GETDATE() " +
                            " WHERE MeterTypeCode = @t AND ISNULL(ACItemCode,'') = ''", cn))
                        {
                            cn.Open();
                            cmd.Parameters.AddWithValue("@i", n.ItemCode);
                            cmd.Parameters.AddWithValue("@t", n.MeterType);
                            cmd.ExecuteNonQuery();
                        }
                        made.Add("meter type " + n.MeterType + " -> " + n.ItemCode);
                    }
                }
                catch (Exception ex)
                {
                    failed.Add(n.ItemCode + ": " + ex.Message);
                }
            }
            return failed.Count == 0 ? "" : "Not done:" + Environment.NewLine + string.Join(Environment.NewLine, failed.ToArray());
        }

        private static Dictionary<string, string> HqTypeItems(string hqCs, List<string> types)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(hqCs) || types.Count == 0) return map;
            using (SqlConnection cn = new SqlConnection(hqCs))
            {
                cn.Open();
                List<string> names = new List<string>();
                for (int i = 0; i < types.Count; i++) names.Add("@t" + i);
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT MeterTypeCode, ISNULL(NULLIF(ACItemCode,''), ISNULL(StockCode,'')) AS Item FROM dbo.zSCP_MeterType " +
                    " WHERE MeterTypeCode IN (" + string.Join(",", names.ToArray()) + ")", cn))
                {
                    for (int i = 0; i < types.Count; i++) cmd.Parameters.AddWithValue(names[i], types[i]);
                    using (SqlDataReader r = cmd.ExecuteReader())
                        while (r.Read()) map[S(r["MeterTypeCode"])] = S(r["Item"]);
                }
            }
            return map;
        }

        private static bool LocalItem(DBSetting db, string code)
        {
            return Exists(db, "Item", "ItemCode", code);
        }

        private static bool Exists(DBSetting db, string table, string column, string value)
        {
            object o = db.ExecuteScalar("SELECT COUNT(*) FROM dbo.[" + table + "] WHERE [" + column + "] = N'" + (value ?? "").Replace("'", "''") + "'");
            return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
        }

        private static bool Has(List<string> list, string code)
        {
            foreach (string s in list) if (string.Equals(s, code, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string S(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
        }
    }
}
