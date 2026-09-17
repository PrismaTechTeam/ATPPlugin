using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One invoice the run would create: the unit the Meter Invoice Run screen shows, ticks and
    /// generates. It is the same unit <see cref="ScpInvoiceJobs.Build"/> bills -- a contract, its
    /// bill group, and which side of the rental split -- so a row on that screen IS a document.
    /// </summary>
    public sealed class InvoiceRunItem
    {
        public string JobKey = "";
        public long ContractKey;
        public string ContractNo = "";
        public string DebtorCode = "";
        public string Customer = "";
        public string BillGroup = "";
        public string MachineNo = "";          // set when the contract bills one invoice per machine
        public bool RentalSide;                // the rental-only invoice of a contract that bills its rent apart

        public HashSet<long> Machines = new HashSet<long>();
        public int MetersTotal;                // usage meters (BK / CL / other read counters) still to bill
        public int MetersRead;                 // of those, how many have a reading
        public decimal Amount;                 // what the rows add up to today
        public string InvoicedDocNo = "";      // when every row is already on an invoice this period
        public List<DataRow> Rows = new List<DataRow>();

        public const int READY = 0, WAITING = 1, INVOICED = 2;
        public int Status;

        /// <summary>The billing date this invoice is for: the period it was loaded for, on the
        /// rows' own billing day. Left at 0 by a caller that does not say which period this is,
        /// and then nothing is ever late.</summary>
        public int Year, Month, DueDay;

        public DateTime Due
        {
            get
            {
                if (Year <= 0 || Month <= 0) return DateTime.MaxValue;
                int last = DateTime.DaysInMonth(Year, Month);
                int d = DueDay < 1 ? 1 : (DueDay > last ? last : DueDay);
                return new DateTime(Year, Month, d);
            }
        }

        /// <summary>Its billing day has gone by and the invoice is still not made.</summary>
        public bool Overdue { get { return Status != INVOICED && Due < DateTime.Today; } }

        /// <summary>Days past the billing date; 0 when it is not late.</summary>
        public int DaysLate { get { return Overdue ? (int)(DateTime.Today - Due).TotalDays : 0; } }

        public int Missing { get { return MetersTotal - MetersRead; } }

        /// <summary>"Rental", "Meter" or "Rental + Meter" -- what the document carries.</summary>
        public string Kind = "";

        /// <summary>What the Invoice column says after the kind: the bill group, the machine, the line count.</summary>
        public string Detail = "";

        public string StatusText
        {
            get
            {
                if (Status == INVOICED) return "Invoiced · " + InvoicedDocNo;
                string late = Overdue ? "Overdue " + DaysLate + "d" : "";
                if (Status == WAITING)
                {
                    string miss = Missing + (Missing == 1 ? " reading missing" : " readings missing");
                    return late.Length > 0 ? late + " · " + miss : miss;
                }
                return late.Length > 0 ? late : "Ready";
            }
        }
    }

    /// <summary>
    /// The Meter Invoice Run's data: the rows to bill for a day, folded into the invoices they
    /// would make, and the one write the screen does (a keyed reading).
    ///
    /// <para>Everything about money and grouping is borrowed, not re-decided: rows come from
    /// <see cref="ScpBillingRows"/>, the invoice key mirrors <see cref="ScpInvoiceJobs"/>, and
    /// generating goes through <see cref="ScpInvoiceJobs.Build"/> like every other screen. This
    /// class only counts what those already decided.</para>
    /// </summary>
    public static class ScpInvoiceRun
    {
        /// <summary>Column added to the billing rows: the invoice a row belongs to.</summary>
        public const string COL_JOBKEY = "RunJobKey";
        /// <summary>Column added to the billing rows: a usage meter with nothing keyed yet.</summary>
        public const string COL_NEEDS = "RunNeedsReading";

        // ───────────────────────────── rows ─────────────────────────────

        /// <summary>The rows due on one day of one month, priced and prefilled exactly the way the
        /// Meter Reading Integration screen loads them, plus the two run columns.</summary>
        /// <summary>One month of a contract that the book is behind on.</summary>
        public sealed class OverdueMonth
        {
            public int Year;
            public int Month;
            /// <summary>The contracts of that month whose billing day has gone by unbilled.</summary>
            public List<long> Contracts = new List<long>();
            /// <summary>The earliest billing day among them -- how long the month has been waiting.</summary>
            public int FirstDay;
        }

        /// <summary>
        /// The months the book is behind on: for each, the contracts whose billing date has gone by
        /// with nothing invoiced for that period. One query, no engine load -- it answers in a
        /// fraction of a second on a book where loading the same months would take minutes, which is
        /// why the overdue view asks this FIRST and only loads what this names.
        /// </summary>
        public static List<OverdueMonth> OverdueMonths(DBSetting db, int monthsBack)
        {
            List<OverdueMonth> list = new List<OverdueMonth>();
            if (db == null) return list;
            if (monthsBack < 1) monthsBack = 1;
            bool includeInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);
            string live = includeInactive ? "1=1" : "ISNULL(c.Inactive,'N') = 'N'";
            // A month somebody deliberately skipped is settled, not late.
            string skipped = TableExists(db, "zSCP2_ContractPeriodSkip")
                ? "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ContractPeriodSkip sk " +
                  "                    WHERE sk.ContractKey = due.ContractKey " +
                  "                      AND sk.PeriodYear = YEAR(due.M) AND sk.PeriodMonth = MONTH(due.M) " +
                  "                      AND sk.UndoneAt IS NULL) "
                : "";
            string sql =
                "DECLARE @today DATE = CAST(GETDATE() AS DATE); " +
                "DECLARE @first DATE = DATEFROMPARTS(YEAR(DATEADD(MONTH,-" + (monthsBack - 1) + ",@today)), " +
                "                                    MONTH(DATEADD(MONTH,-" + (monthsBack - 1) + ",@today)), 1); " +
                "WITH months AS ( " +
                "    SELECT @first AS M " +
                "    UNION ALL SELECT DATEADD(MONTH,1,M) FROM months WHERE M < DATEFROMPARTS(YEAR(@today),MONTH(@today),1)), " +
                "due AS ( " +
                "    SELECT mo.M AS M, c.ContractKey, " +
                "           CASE WHEN ISNULL(c.BillingDay,1) < 1 THEN 1 " +
                "                WHEN ISNULL(c.BillingDay,1) > DAY(EOMONTH(mo.M)) THEN DAY(EOMONTH(mo.M)) " +
                "                ELSE ISNULL(c.BillingDay,1) END AS D " +
                "      FROM months mo " +
                "      JOIN dbo.zSCP2_Contract c ON " + live + " " +
                "       AND c.ServiceStartDate <= EOMONTH(mo.M) " +
                "       AND (c.ServiceExpiryDate IS NULL OR c.ServiceExpiryDate >= mo.M) " +
                // The back door for old data: months before where this book starts billing are
                // somebody else's, and are never late here.
                "       AND (ISNULL(c.BillFromPeriod,0) <= 0 " +
                "            OR YEAR(mo.M) * 100 + MONTH(mo.M) >= c.BillFromPeriod)) " +
                "SELECT YEAR(M) AS Y, MONTH(M) AS Mo, D, ContractKey " +
                "  FROM due " +
                " WHERE DATEFROMPARTS(YEAR(M), MONTH(M), D) < @today " +
                "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e " +
                "                     JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "                     JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "                    WHERE i.ContractKey = due.ContractKey " +
                "                      AND e.PeriodYear = YEAR(due.M) AND e.PeriodMonth = MONTH(due.M) " +
                "                      AND e.InvoicedDocKey IS NOT NULL) " +
                "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e2 " +
                "                     JOIN dbo.zSCP2_ItemMeter m2 ON m2.ItemMeterKey = e2.ItemMeterKey " +
                "                     JOIN dbo.zSCP2_Item i2 ON i2.ItemKey = m2.ItemKey " +
                "                    WHERE i2.ContractKey = due.ContractKey " +
                "                      AND e2.PeriodYear = YEAR(due.M) AND e2.PeriodMonth = MONTH(due.M) " +
                "                      AND ISNULL(e2.InvoicedDocNo,'') <> '') " + skipped +
                " ORDER BY Y, Mo, D OPTION (MAXRECURSION 200)";
            DataTable t = db.GetDataTable(sql, false);
            Dictionary<string, OverdueMonth> byMonth = new Dictionary<string, OverdueMonth>();
            foreach (DataRow r in t.Rows)
            {
                int y = Convert.ToInt32(r["Y"]), mo = Convert.ToInt32(r["Mo"]), d = Convert.ToInt32(r["D"]);
                string k = y + "-" + mo;
                OverdueMonth om;
                if (!byMonth.TryGetValue(k, out om))
                {
                    om = new OverdueMonth();
                    om.Year = y;
                    om.Month = mo;
                    om.FirstDay = d;
                    byMonth[k] = om;
                    list.Add(om);
                }
                if (d < om.FirstDay) om.FirstDay = d;
                om.Contracts.Add(Convert.ToInt64(r["ContractKey"]));
            }
            return list;
        }

        public static DataTable LoadRows(DBSetting db, int year, int month, int day, bool showAll, string search,
                                         out Dictionary<string, List<decimal[]>> ladders)
        {
            return LoadRows(db, year, month, day, showAll, search, null, out ladders);
        }

        /// <summary>
        /// The months that have to be billed BEFORE the ones chosen: for each chosen invoice, any
        /// earlier month of the same contract still carrying no invoice and not in the batch itself.
        /// Each comes back as "contract · Apr 2026 · 169 days late", oldest first.
        ///
        /// <para>A month is billed from where the month before it stopped. Skip April and bill June,
        /// and June's reading is measured against April's opening figure -- three months of copies on
        /// one invoice, and April and May can then never be billed at all. So the run refuses rather
        /// than warns, the same way the delete refuses to take an earlier month away.</para>
        /// </summary>
        public static List<string> EarlierUnbilledMonths(DBSetting db, ICollection<InvoiceRunItem> chosen, int monthsBack)
        {
            List<string> waiting = new List<string>();
            if (db == null || chosen == null || chosen.Count == 0) return waiting;

            // What is going out in this run, and the earliest month asked for per contract.
            HashSet<string> inBatch = new HashSet<string>();
            Dictionary<long, int> earliestAsked = new Dictionary<long, int>();
            Dictionary<long, string> nameOf = new Dictionary<long, string>();
            foreach (InvoiceRunItem it in chosen)
            {
                if (it.Year <= 0 || it.Month <= 0) continue;      // caller did not say which period
                int p = it.Year * 100 + it.Month;
                inBatch.Add(it.ContractKey + "|" + p);
                nameOf[it.ContractKey] = it.ContractNo;
                int had;
                if (!earliestAsked.TryGetValue(it.ContractKey, out had) || p < had) earliestAsked[it.ContractKey] = p;
            }
            if (earliestAsked.Count == 0) return waiting;

            List<string> seen = new List<string>();
            foreach (OverdueMonth om in OverdueMonths(db, monthsBack))
            {
                int p = om.Year * 100 + om.Month;
                foreach (long ck in om.Contracts)
                {
                    int asked;
                    if (!earliestAsked.TryGetValue(ck, out asked)) continue;
                    if (p >= asked) continue;                              // not earlier than what is going out
                    if (inBatch.Contains(ck + "|" + p)) continue;          // it is going out in this same run
                    int last = DateTime.DaysInMonth(om.Year, om.Month);
                    int d = om.FirstDay < 1 ? 1 : (om.FirstDay > last ? last : om.FirstDay);
                    DateTime due = new DateTime(om.Year, om.Month, d);
                    int lateDays = (int)(DateTime.Today - due).TotalDays;
                    string line = (nameOf.ContainsKey(ck) ? nameOf[ck] : "").Trim() + "  ·  " +
                                  due.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) +
                                  (lateDays > 0 ? "  ·  " + lateDays + " days late" : "");
                    if (!seen.Contains(line)) { seen.Add(line); waiting.Add(line); }
                }
            }
            return waiting;
        }

        /// <summary>What to tell somebody who tried to skip a month.</summary>
        public static string EarlierMonthsMessage(List<string> waiting)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("This cannot be invoiced yet: an earlier month of the same contract has never been billed.");
            sb.Append(Environment.NewLine).Append(Environment.NewLine);
            int shown = 0;
            foreach (string s in waiting)
            {
                if (shown++ >= 10)
                {
                    sb.Append("   … and ").Append(waiting.Count - 10).Append(" more").Append(Environment.NewLine);
                    break;
                }
                sb.Append("   ").Append(s).Append(Environment.NewLine);
            }
            sb.Append(Environment.NewLine);
            sb.Append("A month is billed from where the month before it stopped, so the oldest one goes first. ");
            sb.Append("Open \"Show all overdue\" and work down from the top.");
            return sb.ToString();
        }

        /// <summary>The same, narrowed to named contracts -- what the overdue view uses so a month
        /// costs one contract's work each instead of the whole book's.</summary>
        public static DataTable LoadRows(DBSetting db, int year, int month, int day, bool showAll, string search,
                                         ICollection<long> onlyContracts,
                                         out Dictionary<string, List<decimal[]>> ladders)
        {
            int monthEnd = DateTime.DaysInMonth(year, month);
            int target = day > monthEnd ? monthEnd : (day < 1 ? 1 : day);

            bool keepOverdue = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_SHOW_OVERDUE_ROWS,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_SHOW_OVERDUE_ROWS);
            bool includeExpired = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
            bool includeInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);

            // The row's OWN day (rental apart follows Rental invoice day), clamped to the month.
            // A row already invoiced this period always stays, so the Invoiced filter is complete.
            string due = "(CASE WHEN " + ScpBillingRows.RowDueDaySql + " > " + monthEnd +
                         " THEN " + monthEnd + " ELSE " + ScpBillingRows.RowDueDaySql + " END)";
            string dayFilter = showAll ? "" :
                " AND (" + due + (keepOverdue ? " <= " : " = ") + target +
                " OR EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry meD WHERE meD.ItemMeterKey = m.ItemMeterKey" +
                " AND meD.PeriodYear = " + year + " AND meD.PeriodMonth = " + month +
                " AND meD.InvoicedDocKey IS NOT NULL)) ";

            string searchFilter = "";
            string s = (search ?? "").Trim();
            if (s.Length > 0)
            {
                s = s.Replace("'", "''");
                searchFilter = " AND (i.ServiceItemNo LIKE '%" + s + "%' OR i.SerialNumber LIKE '%" + s +
                    "%' OR m.MachineSerialNo LIKE '%" + s +
                    "%' OR c.ContractNo LIKE '%" + s + "%' OR ISNULL(d.CompanyName,'') LIKE '%" + s + "%') ";
            }

            if (onlyContracts != null && onlyContracts.Count > 0)
            {
                System.Text.StringBuilder keys = new System.Text.StringBuilder();
                foreach (long ck in onlyContracts)
                {
                    if (keys.Length > 0) keys.Append(",");
                    keys.Append(ck);
                }
                searchFilter += " AND c.ContractKey IN (" + keys + ") ";
            }

            string inactiveFilter = includeInactive ? "1=1" : "i.Inactive='N' AND c.Inactive='N'";
            // A contract that shares another book's machines is billed in Inter-Billing, never here:
            // one contract, one place it is invoiced from.
            if (HasInterBillLinks(db))
                searchFilter += " AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_InterBillLink ibl " +
                                "WHERE ibl.EntityType = 'CONTRACT' AND ibl.LocalKey = c.ContractKey AND ibl.LocalKey > 0 " +
                                "  AND ibl.Status = 'LIVE' AND EXISTS (SELECT 1 FROM dbo.zSCP2_InterBillBook ibb " +
                                "  WHERE ibb.RemoteBookId = ibl.SourceBookId AND ISNULL(ibb.Inactive,'N') = 'N')) ";
            string expiryFilter = includeExpired ? "" :
                " AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                "OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(" + year + "," + month + ",1)) ";

            DataTable t = ScpBillingRows.Load(db, year, month, inactiveFilter, dayFilter, searchFilter, expiryFilter, out ladders);

            // Same order as the Meter Reading Integration screen: staged readings first, then the
            // prices a contract agreed for a merged line, then the flat meters fill themselves in.
            ScpBillingRows.PrefillFromStaging(db, t, month, year, ladders);
            MarkLate(t, target);
            ScpBillingRows.ApplyRentalGroupPrices(db, t);
            ScpBillingRows.ApplyMeterLinePrices(db, t);
            // The price passes change the rate but not the figure computed before them; a read
            // meter is priced again so the list says what the invoice will say (the engine bills
            // from the rate, not from the figure).
            foreach (DataRow r in t.Rows)
            {
                bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (flat || IsInvoiced(r) || !HasReading(r)) continue;
                ScpBillingRows.Recalc(r, ladders, year, month);
            }
            ScpBillingRows.AutoFillFlatMeters(db, t, year, month, ladders);

            if (!t.Columns.Contains(COL_JOBKEY)) t.Columns.Add(COL_JOBKEY, typeof(string));
            if (!t.Columns.Contains(COL_NEEDS)) t.Columns.Add(COL_NEEDS, typeof(bool));
            return t;
        }

        private static bool TableExists(DBSetting db, string name)
        {
            try
            {
                object o = db.ExecuteScalar(
                    "SELECT CASE WHEN OBJECT_ID('dbo." + name + "') IS NULL THEN 0 ELSE 1 END");
                return o != null && o != DBNull.Value && Convert.ToInt32(o) == 1;
            }
            catch { return false; }
        }

        private static bool HasInterBillLinks(DBSetting db)
        {
            try
            {
                object o = db.ExecuteScalar("SELECT CASE WHEN OBJECT_ID('dbo.zSCP2_InterBillLink') IS NULL THEN 0 ELSE 1 END");
                return o != null && o != DBNull.Value && Convert.ToInt32(o) == 1;
            }
            catch { return false; }
        }

        private static void MarkLate(DataTable t, int today)
        {
            if (t == null || !t.Columns.Contains("Late")) return;
            foreach (DataRow r in t.Rows)
            {
                int day = r["DueDay"] == DBNull.Value ? 0 : Convert.ToInt32(r["DueDay"]);
                bool billed = S(r["InvoicedDocNo"]).Trim().Length > 0;
                r["Late"] = !billed && day > 0 && day < today;
            }
        }

        /// <summary>The days any row in the book is due on -- what the day strip offers.</summary>
        public static List<int> DaysInUse(DBSetting db)
        {
            List<int> days = new List<int>();
            bool includeInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);
            string sql = "SELECT DISTINCT " + ScpBillingRows.RowDueDaySql + " AS d " +
                "FROM dbo.zSCP2_ItemMeter m " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "WHERE " + (includeInactive ? "1=1 " : "i.Inactive='N' AND c.Inactive='N' ") +
                "AND " + ScpBillingRows.RowDueDaySql + " BETWEEN 1 AND 31 ORDER BY d";
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandTimeout = 60;
                    using (SqlDataReader rd = cmd.ExecuteReader())
                        while (rd.Read()) days.Add(Convert.ToInt32(rd[0]));
                }
            }
            if (days.Count == 0) for (int d = 1; d <= 31; d++) days.Add(d);
            return days;
        }

        /// <summary>The Invoice grouping setting, read the way the generator reads it.</summary>
        public static string GroupingMode(DBSetting db)
        {
            try
            {
                string m = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_GROUPING,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_GROUPING).Trim().ToUpperInvariant();
                if (m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR ||
                    m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) return m;
            }
            catch { }
            return ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_FOLLOW;
        }

        // ───────────────────────────── invoices ─────────────────────────────

        /// <summary>The invoice a row lands on. Mirrors, step for step, the key
        /// <see cref="ScpInvoiceJobs.Build"/> composes -- a list that grouped differently from the
        /// generator would promise one document and produce another.</summary>
        public static string JobKeyOf(DataRow r, string grpMode)
        {
            long contractKey = D64(r["ContractKey"]);
            long itemKey = D64(r["ItemKey"]);
            string mode;
            if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR) mode = "G";
            else if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) mode = "S";
            else mode = S(r["BillingMode"]).Trim().ToUpperInvariant() == "S" ? "S" : "G";

            string key = mode == "S"
                ? ("C" + contractKey + "_I" + itemKey)
                : (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR
                    ? ("D" + S(r["DebtorCode"]))
                    : ("C" + contractKey));

            string billGroup = ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]));
            bool groupItem = r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
            if (groupItem) billGroup = "";
            if (billGroup.Length > 0 && mode != "S") key = "C" + contractKey + "_G" + billGroup;   // per machine outranks a bill group

            bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            bool rentSep = r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]);
            bool waive = r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);
            bool rentalJob = flat && rentSep && (ScpStrategy.IsRentalMeterCode(S(r["MeterType"])) || waive);
            if (rentalJob) key += "_R";
            return key;
        }

        /// <summary>A counter somebody reads: not rent, not a waive, not a minimum.</summary>
        public static bool IsUsageMeter(DataRow r)
        {
            if (r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"])) return false;
            if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) return false;
            string role = S(r["Role"]).Trim().ToUpperInvariant();
            return role != "RENTAL" && role != "WAIVE" && role != "COMMIT";
        }

        public static bool HasReading(DataRow r)
        {
            return Dec(r["CurrentReading"]) > 0m || Dec(r["FetchedReading"]) > 0m;
        }

        public static bool IsInvoiced(DataRow r)
        {
            return S(r["InvoicedDocNo"]).Trim().Length > 0;
        }

        /// <summary>A row that can be billed on its own. A waive cannot: it is the credit against
        /// a rent, prints with that rent, and is never stamped by itself -- so a rental invoice
        /// whose rent went out must not stay "Ready" on the strength of its waive rows.</summary>
        public static bool StandsAlone(DataRow r)
        {
            return !(r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]));
        }

        /// <summary>Fold the rows into the invoices they would make, and stamp each row with its
        /// invoice so the detail grid can filter on it.</summary>
        public static List<InvoiceRunItem> BuildItems(DataTable rows, string grpMode)
        {
            return BuildItems(rows, grpMode, 0, 0);
        }

        /// <summary>The same, told which period the rows were loaded for, so each invoice knows its
        /// own billing date and can say it is late.</summary>
        public static List<InvoiceRunItem> BuildItems(DataTable rows, string grpMode, int year, int month)
        {
            Dictionary<string, InvoiceRunItem> byKey = new Dictionary<string, InvoiceRunItem>();
            List<InvoiceRunItem> list = new List<InvoiceRunItem>();
            if (rows == null) return list;
            foreach (DataRow r in rows.Rows)
            {
                string key = JobKeyOf(r, grpMode);
                r[COL_JOBKEY] = key;
                r[COL_NEEDS] = IsUsageMeter(r) && !HasReading(r) && !IsInvoiced(r);
                InvoiceRunItem it;
                if (!byKey.TryGetValue(key, out it))
                {
                    it = new InvoiceRunItem();
                    it.JobKey = key;
                    it.ContractKey = D64(r["ContractKey"]);
                    it.ContractNo = S(r["ContractNo"]);
                    it.DebtorCode = S(r["DebtorCode"]);
                    it.Customer = S(r["Customer"]);
                    it.RentalSide = key.EndsWith("_R");
                    if (key.Contains("_I")) it.MachineNo = S(r["ServiceItemNo"]);
                    if (key.Contains("_G")) it.BillGroup = key.Substring(key.IndexOf("_G") + 2).Replace("_R", "");
                    it.Year = year;
                    it.Month = month;
                    byKey[key] = it;
                    list.Add(it);
                }
                // The invoice is due on the LAST of its rows' billing days: while one line of it is
                // still not due, the invoice as a whole is not late.
                int dueDay = rows.Columns.Contains("DueDay") && r["DueDay"] != DBNull.Value
                    ? Convert.ToInt32(r["DueDay"]) : 0;
                if (dueDay > it.DueDay) it.DueDay = dueDay;
                it.Rows.Add(r);
            }
            foreach (InvoiceRunItem it in list) Refresh(it);
            // An invoice made only of contras has nothing to send and nothing that was sent.
            list.RemoveAll(delegate (InvoiceRunItem it)
            {
                foreach (DataRow r in it.Rows) if (StandsAlone(r)) return false;
                return true;
            });
            list.Sort(delegate (InvoiceRunItem a, InvoiceRunItem b)
            {
                int c = string.Compare(a.Customer, b.Customer, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = string.Compare(a.ContractNo, b.ContractNo, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                if (a.RentalSide != b.RentalSide) return a.RentalSide ? -1 : 1;   // the rent before the copies
                c = string.Compare(a.BillGroup, b.BillGroup, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                return string.Compare(a.MachineNo, b.MachineNo, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>Recount one invoice from its rows -- after a reading is keyed, after a reload.</summary>
        public static void Refresh(InvoiceRunItem it)
        {
            it.Machines.Clear();
            it.MetersTotal = 0; it.MetersRead = 0; it.Amount = 0m; it.InvoicedDocNo = "";
            int pending = 0, invoiced = 0;
            bool anyRent = false, anyUsage = false;
            decimal invTotal = 0m;
            string invDoc = "";
            foreach (DataRow r in it.Rows)
            {
                it.Machines.Add(D64(r["ItemKey"]));
                if (IsInvoiced(r))
                {
                    invoiced++;
                    if (invDoc.Length == 0) invDoc = S(r["InvoicedDocNo"]).Trim();
                    // The billed rows carry their invoice's total when it is the latest one they saw.
                    if (S(r["LastInvNo"]).Trim() == S(r["InvoicedDocNo"]).Trim())
                    {
                        decimal t = Dec(r["InvTotal"]);
                        if (t > invTotal) invTotal = t;
                    }
                    continue;
                }
                if (StandsAlone(r)) pending++;
                bool needs = r[COL_NEEDS] != DBNull.Value && Convert.ToBoolean(r[COL_NEEDS]);
                if (IsUsageMeter(r))
                {
                    anyUsage = true;
                    it.MetersTotal++;
                    if (!needs) it.MetersRead++;
                }
                else
                {
                    string role = S(r["Role"]).Trim().ToUpperInvariant();
                    bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                    if (role == "RENTAL" || role == "WAIVE" || (flat && role != "COMMIT")) anyRent = true;
                }
                it.Amount += Dec(r["TotalCharges"]);
            }
            if (pending == 0 && invoiced > 0)
            {
                it.Status = InvoiceRunItem.INVOICED;
                it.InvoicedDocNo = invDoc;
                it.Amount = invTotal;
            }
            else
            {
                it.Status = it.Missing > 0 ? InvoiceRunItem.WAITING : InvoiceRunItem.READY;
            }
            if (it.RentalSide) it.Kind = "Rental";
            else if (anyRent && anyUsage) it.Kind = "Rental + Meter";
            else if (anyRent) it.Kind = "Rental";
            else it.Kind = "Meter";

            string detail = "";
            if (it.MachineNo.Length > 0) detail = it.MachineNo;
            else if (it.BillGroup.Length > 0) detail = it.BillGroup;
            int lines = pending > 0 ? pending : invoiced;
            detail = (detail.Length > 0 ? detail + " · " : "") + lines + (lines == 1 ? " row" : " rows");
            it.Detail = detail;
        }

        // ───────────────────────────── the one write ─────────────────────────────

        /// <summary>Stage one keyed reading for the period (0 removes it). Locked rows -- the
        /// billing-day snapshot -- are never overwritten, exactly as on the Meter Reading screen.</summary>
        public static void SaveReading(DBSetting db, long itemMeterKey, int year, int month, decimal reading)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("RunKeyIn"))
                {
                    try
                    {
                        SqlCommand chk = new SqlCommand(
                            "SELECT LockedAt FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn, tx);
                        chk.Parameters.AddWithValue("@imk", itemMeterKey);
                        chk.Parameters.AddWithValue("@yr", year);
                        chk.Parameters.AddWithValue("@mo", month);
                        object lockedAt = chk.ExecuteScalar();
                        if (lockedAt != null && lockedAt != DBNull.Value)
                            throw new InvalidOperationException("This reading was locked by the billing-day snapshot and cannot be changed here.");

                        if (reading <= 0m)
                        {
                            SqlCommand del = new SqlCommand(
                                "DELETE FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo AND InvoicedDocKey IS NULL", cn, tx);
                            del.Parameters.AddWithValue("@imk", itemMeterKey);
                            del.Parameters.AddWithValue("@yr", year);
                            del.Parameters.AddWithValue("@mo", month);
                            del.ExecuteNonQuery();
                        }
                        else
                        {
                            SqlCommand cmd = new SqlCommand(
                                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, ReadingDate=@dt, Source=@src, TrackingId='', LastModified=GETDATE() " +
                                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo AND LockedAt IS NULL; " +
                                "IF @@ROWCOUNT=0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo) " +
                                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source,TrackingId) " +
                                "VALUES (@imk,@yr,@mo,@rd,@dt,@src,'');", cn, tx);
                            cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                            cmd.Parameters.AddWithValue("@yr", year);
                            cmd.Parameters.AddWithValue("@mo", month);
                            cmd.Parameters.AddWithValue("@rd", reading);
                            cmd.Parameters.AddWithValue("@dt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@src", "MANUAL");
                            cmd.ExecuteNonQuery();
                            ScpMeterReadingLog.Append(cn, tx, itemMeterKey, year, month, reading, DateTime.Now, "MANUAL", "");
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ───────────────────────────── helpers ─────────────────────────────

        private static string S(object o) { return o == null || o == DBNull.Value ? "" : o.ToString(); }
        private static decimal Dec(object o)
        {
            if (o == null || o == DBNull.Value) return 0m;
            decimal d; return decimal.TryParse(o.ToString(), out d) ? d : 0m;
        }
        private static long D64(object o)
        {
            if (o == null || o == DBNull.Value) return 0L;
            long v; return long.TryParse(o.ToString(), out v) ? v : 0L;
        }
    }
}
