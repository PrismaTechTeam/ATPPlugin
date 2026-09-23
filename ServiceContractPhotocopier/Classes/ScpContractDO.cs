using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// The delivery orders of a contract's machines (feedback ATP-13): which DO a machine came
    /// from or went out on, whether a machine can go out on a new one, and making that DO.
    ///
    /// <para>The link is the SERIAL NUMBER, read live from AutoCount's own serial transactions
    /// (dbo.SerialNoTrans) and never copied into the plug-in. A contract built with "Generate
    /// From Serial No" has its machines' serials on the DO it came from; a DO made here carries
    /// them too. Either way the answer is the same question -- is this serial on a live DO to this
    /// customer -- and a DO later cancelled or deleted in AutoCount stops counting at once, where
    /// a stored link would have gone stale.</para>
    ///
    /// <para>Whether a serial is still in stock cannot be read off ItemSerialNo.Qty alone: on the
    /// client's book 39 serials already delivered on a DO still show Qty 1 there (and 38 others
    /// were never registered). So a serial is available only when AutoCount has it with Qty > 0
    /// AND it has gone out (DO / IV / CS) no more often than it has come back (DR / CN).</para>
    /// </summary>
    public static class ScpContractDO
    {
        /// <summary>A machine's latest live DO to its contract's customer.</summary>
        public class MachineDO
        {
            public long ItemKey;
            public long DocKey;
            public string DocNo = "";
            public DateTime? DocDate;
        }

        /// <summary>A machine proposed for a new DO.</summary>
        public class Candidate
        {
            public long ItemKey;
            public string ServiceItemNo = "";
            public string ItemCode = "";
            public string SerialNo = "";
            public string Location = "";   // the machine's stock location; empty = AutoCount's default
        }

        /// <summary>One machine's answer: may it go out, and if not, why not.</summary>
        public class Check
        {
            public Candidate Machine;
            public bool Ok;
            public string Reason = "";
        }

        // Latest live DO (to the contract's own customer) per machine carrying a serial. A machine's
        // serial matches a serial transaction when the item codes agree, or when the machine has no
        // item code to compare.
        private const string MACHINE_DO_SQL =
            "SELECT x.ItemKey, x.DocKey, x.DocNo, x.DocDate, x.ContractKey FROM (" +
            "  SELECT i.ItemKey, i.ContractKey, d.DocKey, d.DocNo, d.DocDate, " +
            "         ROW_NUMBER() OVER (PARTITION BY i.ItemKey ORDER BY d.DocDate DESC, d.DocKey DESC) AS rn " +
            "  FROM dbo.zSCP2_Item i " +
            "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
            "  JOIN dbo.SerialNoTrans t ON t.DocType = 'DO' AND ISNULL(t.Cancelled,'F') <> 'T' " +
            "       AND t.FromSerialNo = i.SerialNumber " +
            "       AND (ISNULL(i.ItemCode,'') = '' OR t.ItemCode = i.ItemCode) " +
            "  JOIN dbo.DO d ON d.DocKey = t.DocKey AND ISNULL(d.Cancelled,'F') <> 'T' AND d.DebtorCode = c.DebtorCode " +
            "  WHERE ISNULL(i.SerialNumber,'') <> '' {0}" +
            ") x WHERE x.rn = 1";

        /// <summary>Each machine of one contract that is on a live DO, keyed by ItemKey.</summary>
        public static Dictionary<long, MachineDO> ForContract(DBSetting db, long contractKey)
        {
            Dictionary<long, MachineDO> map = new Dictionary<long, MachineDO>();
            if (db == null || contractKey <= 0) return map;
            DataTable t = db.GetDataTable(string.Format(MACHINE_DO_SQL, " AND i.ContractKey = " + contractKey + " "), false);
            foreach (DataRow r in t.Rows)
            {
                MachineDO m = new MachineDO();
                m.ItemKey = Convert.ToInt64(r["ItemKey"]);
                m.DocKey = Convert.ToInt64(r["DocKey"]);
                m.DocNo = Convert.ToString(r["DocNo"]).Trim();
                m.DocDate = r["DocDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["DocDate"]);
                map[m.ItemKey] = m;
            }
            return map;
        }

        /// <summary>Every contract's DOs, as the contract list shows them: "DO 00001634, DO 00001644"
        /// (oldest first), keyed by ContractKey. A contract missing from the result has no DO yet.</summary>
        public static Dictionary<long, string> SummaryByContract(DBSetting db)
        {
            Dictionary<long, SortedDictionary<string, string>> per = new Dictionary<long, SortedDictionary<string, string>>();
            DataTable t = db.GetDataTable(string.Format(MACHINE_DO_SQL, " "), false);
            foreach (DataRow r in t.Rows)
            {
                long ck = Convert.ToInt64(r["ContractKey"]);
                string no = Convert.ToString(r["DocNo"]).Trim();
                DateTime dt = r["DocDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["DocDate"]);
                SortedDictionary<string, string> set;
                if (!per.TryGetValue(ck, out set)) { set = new SortedDictionary<string, string>(); per[ck] = set; }
                set[dt.ToString("yyyyMMdd") + "|" + no] = no;
            }
            Dictionary<long, string> result = new Dictionary<long, string>();
            foreach (KeyValuePair<long, SortedDictionary<string, string>> kv in per)
            {
                List<string> nos = new List<string>();
                foreach (string no in kv.Value.Values) if (!nos.Contains(no)) nos.Add(no);
                result[kv.Key] = string.Join(", ", nos.ToArray());
            }
            return result;
        }

        /// <summary>
        /// Whether each machine can go out on a new DO to this customer, read fresh from the book.
        /// Called when the list is offered AND again immediately before the DO is saved, so a serial
        /// delivered by someone else in between is caught rather than delivered twice.
        /// </summary>
        public static List<Check> CheckAvailable(DBSetting db, List<Candidate> machines)
        {
            List<Check> result = new List<Check>();
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                foreach (Candidate m in machines)
                {
                    Check c = new Check();
                    c.Machine = m;
                    string ic = (m.ItemCode ?? "").Trim(), sn = (m.SerialNo ?? "").Trim();
                    if (ic.Length == 0) { c.Reason = "no Item Code"; result.Add(c); continue; }
                    if (sn.Length == 0) { c.Reason = "no Machine Serial"; result.Add(c); continue; }

                    SqlCommand cmd = new SqlCommand(
                        "SELECT " +
                        " (SELECT TOP 1 Qty FROM dbo.ItemSerialNo WHERE ItemCode=@ic AND SerialNumber=@sn) AS StockQty, " +
                        " (SELECT COUNT(*) FROM dbo.SerialNoTrans t WHERE t.ItemCode=@ic AND t.FromSerialNo=@sn " +
                        "    AND ISNULL(t.Cancelled,'F')<>'T' AND t.DocType IN ('DO','IV','CS')) AS OutCount, " +
                        " (SELECT COUNT(*) FROM dbo.SerialNoTrans t WHERE t.ItemCode=@ic AND t.FromSerialNo=@sn " +
                        "    AND ISNULL(t.Cancelled,'F')<>'T' AND t.DocType IN ('DR','CN')) AS BackCount, " +
                        " (SELECT TOP 1 h.DocType + '|' + h.DocNo + '|' + CONVERT(varchar(10), h.DocDate, 103) + '|' + h.DebtorCode " +
                        "    FROM dbo.SerialNoTrans t " +
                        "    JOIN (SELECT 'DO' AS DocType, DocKey, DocNo, DocDate, DebtorCode FROM dbo.DO WHERE ISNULL(Cancelled,'F')<>'T' " +
                        "          UNION ALL SELECT 'IV', DocKey, DocNo, DocDate, DebtorCode FROM dbo.IV WHERE ISNULL(Cancelled,'F')<>'T' " +
                        "          UNION ALL SELECT 'CS', DocKey, DocNo, DocDate, DebtorCode FROM dbo.CS WHERE ISNULL(Cancelled,'F')<>'T') h " +
                        "      ON h.DocType = t.DocType AND h.DocKey = t.DocKey " +
                        "    WHERE t.ItemCode=@ic AND t.FromSerialNo=@sn AND ISNULL(t.Cancelled,'F')<>'T' " +
                        "    ORDER BY h.DocDate DESC, t.TransKey DESC) AS LastOut", cn);
                    cmd.Parameters.AddWithValue("@ic", ic);
                    cmd.Parameters.AddWithValue("@sn", sn);
                    object qty = null; int outs = 0, backs = 0; string lastOut = "";
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            qty = rd.IsDBNull(0) ? null : (object)Convert.ToInt32(rd.GetValue(0));
                            outs = Convert.ToInt32(rd.GetValue(1));
                            backs = Convert.ToInt32(rd.GetValue(2));
                            lastOut = rd.IsDBNull(3) ? "" : rd.GetString(3);
                        }
                    }
                    if (outs > backs)
                    {
                        // Delivered, and not returned since -- whatever ItemSerialNo.Qty says.
                        string[] p = lastOut.Split('|');
                        c.Reason = p.Length >= 4
                            ? "already delivered on " + p[1] + " (" + p[2] + ", customer " + p[3] + ")"
                            : "already delivered, and not returned since";
                    }
                    else if (qty == null)
                        c.Reason = "serial " + sn + " is not in AutoCount's stock -- receive it first (GRN / Purchase Invoice with the serial)";
                    else if ((int)qty <= 0)
                        c.Reason = "serial " + sn + " is out of stock (quantity " + qty + ")";
                    else
                        c.Ok = true;
                    result.Add(c);
                }
            }
            return result;
        }

        /// <summary>
        /// Make ONE delivery order for these machines and save it through AutoCount's own document
        /// logic -- numbering, stock, and the serial transactions that link each machine to it. The
        /// machines are checked once more first; if any is no longer available nothing is made.
        /// Machines go out at no price: they are billed through the contract, not sold on the DO.
        /// Returns the new DO number.
        /// </summary>
        public static string CreateDO(DBSetting db, string debtorCode, string contractNo, string deptNo, string projNo,
                                      DateTime docDate, List<Candidate> machines, out long docKey)
        {
            docKey = 0;
            if (machines == null || machines.Count == 0)
                throw new InvalidOperationException("No machines were chosen for the DO.");

            // The check the user asked for: at the moment of making the DO, not only when the list
            // was shown.
            List<string> refused = new List<string>();
            foreach (Check c in CheckAvailable(db, machines))
                if (!c.Ok) refused.Add(c.Machine.ServiceItemNo + ": " + c.Reason);
            if (refused.Count > 0)
                throw new InvalidOperationException("No DO was made -- since the list was shown:\r\n\r\n   " +
                    string.Join("\r\n   ", refused.ToArray()));

            AutoCount.Invoicing.Sales.DeliveryOrder.DeliveryOrderCommand cmd =
                AutoCount.Invoicing.Sales.DeliveryOrder.DeliveryOrderCommand.Create(UserSession.CurrentUserSession, db);
            AutoCount.Invoicing.Sales.DeliveryOrder.DeliveryOrder doc = cmd.AddNew();
            if (doc == null) throw new InvalidOperationException("AutoCount did not open a new delivery order.");
            // Serials are given below, so AutoCount must not stop to ask for them on screen.
            doc.DisableShowSerialNumberEntryForm();
            doc.DebtorCode = debtorCode;
            doc.DocDate = docDate.Date;
            doc.Ref = contractNo ?? "";
            doc.Description = "Delivery for service contract " + (contractNo ?? "");
            foreach (Candidate m in machines)
            {
                AutoCount.Invoicing.Sales.DeliveryOrder.DeliveryOrderDetail d = doc.AddDetail();
                d.ItemCode = m.ItemCode.Trim();
                if (!string.IsNullOrEmpty(m.Location)) d.Location = m.Location.Trim();
                if (!string.IsNullOrEmpty(deptNo)) d.DeptNo = deptNo;
                if (!string.IsNullOrEmpty(projNo)) d.ProjNo = projNo;
                d.Qty = 1m;
                d.UnitPrice = 0m;
                d.AddSerialNumberTransactionRecord(m.SerialNo.Trim());
            }
            doc.Save();
            docKey = doc.DocKey;
            return doc.DocNo;
        }
    }
}
