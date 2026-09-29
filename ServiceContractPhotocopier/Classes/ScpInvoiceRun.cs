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

        public const int READY = 0, WAITING = 1, INVOICED = 2, UNPRICED = 3, NO_ITEM = 4, BACKWARD = 5;
        public int Status;

        /// <summary>Rows that would bill with no price at all -- a rental or a counter with no rate,
        /// no minimum and no ladder (#65, 29/9). The invoice cannot go until each has one.</summary>
        public int Unpriced;
        /// <summary>Meter types whose invoice line would have no item of this book (#66, 29/9).</summary>
        public List<string> NoItemTypes = new List<string>();
        /// <summary>Counters read BELOW their last reading -- replaced, reset or mistyped (#16, 29/9). The
        /// preview refused them; the list said Ready until then.</summary>
        public int Backwards;
        /// <summary>Machines whose serial another active machine also has, so a fetch gives both the same
        /// readings (#27, 29/9). A warning -- the invoice can still go.</summary>
        public int SameSerial;

        /// <summary>The billing date this invoice is for: the period it was loaded for, on the
        /// rows' own billing day. Left at 0 by a caller that does not say which period this is,
        /// and then nothing is ever late.</summary>
        public int Year, Month, DueDay;

        /// <summary>The invoice date the operator chose instead of the default (feedback ATP-6 /
        /// ATP-9); null = the default. Kept by the run screen for the session.</summary>
        public DateTime? DocDateOverride;

        /// <summary>The invoice's Reference No, typed once for the whole invoice (feedback ATP-8,
        /// user 26/9: "reference no is for the invoice, not one per meter"); "" = the usual Ref --
        /// PUMS's report no., else the machine or contract no. Kept by the run screen for the session.</summary>
        public string RefOverride = "";

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
                string warn = SameSerial > 0 ? " · same serial as another machine" : "";
                if (Status == UNPRICED || Status == NO_ITEM || Status == BACKWARD)
                {
                    string why = Status == UNPRICED ? "Unpriced (" + Unpriced + ")"
                               : Status == NO_ITEM ? "No item code · " + string.Join(", ", NoItemTypes.ToArray())
                               : "Reading below last (" + Backwards + ")";
                    return (late.Length > 0 ? late + " · " + why : why) + warn;
                }
                if (Status == WAITING)
                {
                    string miss = Missing + (Missing == 1 ? " reading missing" : " readings missing");
                    return (late.Length > 0 ? late + " · " + miss : miss) + warn;
                }
                return (late.Length > 0 ? late : "Ready") + warn;
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
        /// <summary>The row would bill with no price (see <see cref="MarkPricesAndItems"/>).</summary>
        public const string COL_UNPRICED = "RunUnpriced";
        /// <summary>The row's meter type, when its invoice line would have no item of this book; "" otherwise.</summary>
        public const string COL_NOITEM = "RunNoItem";
        /// <summary>Another active machine has this row's serial.</summary>
        public const string COL_SAMESERIAL = "RunSameSerial";
        /// <summary>The net total of the invoice this row is on THIS period (0 for a NO CHARGE stamp).</summary>
        public const string COL_DOCTOTAL = "RunDocTotal";
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
                "       AND (c.ServiceStartDate <= EOMONTH(mo.M) " +
                // ATP-10: a contract billing its rental in advance owes its first month's rental in
                // the month before it starts.
                "            OR (ISNULL(c.RentalBasis,'A') = 'P' AND c.ServiceStartDate <= EOMONTH(DATEADD(MONTH, 1, mo.M)) " +
                "                AND EXISTS (SELECT 1 FROM dbo.zSCP2_Item ir JOIN dbo.zSCP2_ItemMeter mr ON mr.ItemKey = ir.ItemKey " +
                "                             WHERE ir.ContractKey = c.ContractKey AND ISNULL(ir.Inactive,'N') = 'N' " +
                "                               AND " + ScpStrategy.RentalRoleSql("mr") + "))) " +
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
                // ATP-10: the last month of a contract that bills nothing but a rental in advance --
                // that rental went out the month before -- owes nothing.
                "   AND NOT (EXISTS (SELECT 1 FROM dbo.zSCP2_Contract cx WHERE cx.ContractKey = due.ContractKey " +
                "                      AND ISNULL(cx.RentalBasis,'A') = 'P' AND cx.ServiceExpiryDate IS NOT NULL " +
                "                      AND YEAR(cx.ServiceExpiryDate) * 100 + MONTH(cx.ServiceExpiryDate) = YEAR(due.M) * 100 + MONTH(due.M)) " +
                "            AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_Item ix JOIN dbo.zSCP2_ItemMeter mx ON mx.ItemKey = ix.ItemKey " +
                "                             WHERE ix.ContractKey = due.ContractKey AND ISNULL(ix.Inactive,'N') = 'N' " +
                "                               AND NOT " + ScpStrategy.RentalRoleSql("mx") + " " +
                "                               AND UPPER(ISNULL(mx.MeterRole,'')) <> 'WAIVE')) " +
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
            // The terms that span the contract -- the committed minimum, the waive, group tiers --
            // priced by the engine that bills them, so the list says what Generate will charge.
            PriceTerms(db, t, ladders, year, month, null);

            if (!t.Columns.Contains(COL_JOBKEY)) t.Columns.Add(COL_JOBKEY, typeof(string));
            if (!t.Columns.Contains(COL_NEEDS)) t.Columns.Add(COL_NEEDS, typeof(bool));
            MarkPricesAndItems(db, t);
            return t;
        }

        /// <summary>
        /// Two things an invoice cannot go without, marked on each row still to bill so the list
        /// stops them before Generate does not (29/9):
        /// <list type="bullet">
        /// <item><b>a price</b> (#65): a rental, or a black or colour counter WITH copies this month
        /// (counted in <see cref="Refresh"/>), with no rate, no minimum and no ladder -- its own, a
        /// scheme, or its copy group's -- bills nothing. It showed green Ready
        /// and went out at 0.00, or on the old layout was stamped NO CHARGE and could never be
        /// billed again. The same test as the Inter-Billing board's Unpriced.</item>
        /// <item><b>an item of this book</b> (#66): a line with an item code AutoCount does not have
        /// fails when the invoice is saved, and one with none posts to the default sales account.
        /// A counter never billed (role NA) needs none. The same test as the board's No item code.</item>
        /// </list>
        /// </summary>
        public static void MarkPricesAndItems(DBSetting db, DataTable t)
        {
            if (t == null) return;
            if (!t.Columns.Contains(COL_UNPRICED)) t.Columns.Add(COL_UNPRICED, typeof(bool));
            if (!t.Columns.Contains(COL_NOITEM)) t.Columns.Add(COL_NOITEM, typeof(string));
            if (!t.Columns.Contains(COL_SAMESERIAL)) t.Columns.Add(COL_SAMESERIAL, typeof(bool));
            if (!t.Columns.Contains(COL_DOCTOTAL)) t.Columns.Add(COL_DOCTOTAL, typeof(decimal));
            if (t.Rows.Count == 0 || db == null) return;

            // Serials more than one active machine carries (#27).
            HashSet<string> shared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dup = db.GetDataTable(
                    "SELECT LTRIM(RTRIM(SerialNumber)) AS Serial FROM dbo.zSCP2_Item " +
                    " WHERE ISNULL(Inactive,'N') = 'N' AND ISNULL(IsGroupItem,'N') = 'N' AND LTRIM(RTRIM(ISNULL(SerialNumber,''))) <> '' " +
                    " GROUP BY LTRIM(RTRIM(SerialNumber)) HAVING COUNT(DISTINCT ItemKey) > 1", false);
                foreach (DataRow r in dup.Rows) shared.Add(S(r["Serial"]));
            }
            catch { }

            // The invoice each billed row is on this period, by number: the row said the total of the
            // meter's LATEST invoice of any month (#29 / #33 -- SC 000000032's September MR2609.0835,
            // 1,101.11, read 1,824.39, November's).
            Dictionary<string, decimal> docTotal = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            List<string> docs = new List<string>();
            foreach (DataRow r in t.Rows)
            {
                string no = S(r["InvoicedDocNo"]).Trim();
                if (no.Length > 0 && !docs.Contains(no)) docs.Add(no);
            }
            if (docs.Count > 0)
            {
                List<string> quoted = new List<string>();
                foreach (string d in docs) quoted.Add("N'" + d.Replace("'", "''") + "'");
                try
                {
                    DataTable iv = db.GetDataTable("SELECT DocNo, ISNULL(NetTotal,0) AS NetTotal FROM dbo.IV WHERE DocNo IN (" +
                                                   string.Join(",", quoted.ToArray()) + ")", false);
                    foreach (DataRow r in iv.Rows) docTotal[S(r["DocNo"]).Trim()] = Dec(r["NetTotal"]);
                }
                catch { }
            }

            // Copy groups that agreed a ladder, per colour: "contract|BK|GROUP".
            HashSet<string> groupLadders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable g = db.GetDataTable(
                    "SELECT ContractKey, UPPER(LTRIM(RTRIM(ISNULL(GroupCode,'')))) AS GroupCode, " +
                    "       ISNULL(LadderBk,'') AS LadderBk, ISNULL(LadderCl,'') AS LadderCl " +
                    "  FROM dbo.zSCP2_ContractRentalPrice WHERE Side = 'M' " +
                    "   AND (ISNULL(LadderBk,'') <> '' OR ISNULL(LadderCl,'') <> '')", false);
                foreach (DataRow r in g.Rows)
                {
                    string k = S(r["ContractKey"]) + "|";
                    if (S(r["LadderBk"]).Trim().Length > 0) groupLadders.Add(k + "BK|" + S(r["GroupCode"]));
                    if (S(r["LadderCl"]).Trim().Length > 0) groupLadders.Add(k + "CL|" + S(r["GroupCode"]));
                }
            }
            catch { }

            // Which of the rows' item codes this book has, in one query.
            HashSet<string> codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in t.Rows)
            {
                string c = t.Columns.Contains("ACItemCode") ? S(r["ACItemCode"]).Trim() : "";
                if (c.Length > 0) codes.Add(c);
            }
            HashSet<string> items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (codes.Count > 0)
            {
                List<string> quoted = new List<string>();
                foreach (string c in codes) quoted.Add("N'" + c.Replace("'", "''") + "'");
                try
                {
                    DataTable it = db.GetDataTable("SELECT ItemCode FROM dbo.Item WHERE ItemCode IN (" + string.Join(",", quoted.ToArray()) + ")", false);
                    foreach (DataRow r in it.Rows) items.Add(S(r["ItemCode"]).Trim());
                }
                catch { items = null; }   // not asked -- nothing is marked rather than everything
            }

            foreach (DataRow r in t.Rows)
            {
                r[COL_UNPRICED] = false;
                r[COL_NOITEM] = "";
                r[COL_SAMESERIAL] = shared.Contains(S(r["SerialNo"]).Trim());
                decimal dt;
                r[COL_DOCTOTAL] = docTotal.TryGetValue(S(r["InvoicedDocNo"]).Trim(), out dt) ? dt : 0m;
                if (IsInvoiced(r)) continue;
                string role = S(r["Role"]).Trim().ToUpperInvariant();
                if (role == "NA") continue;
                bool waive = r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);

                if (!waive && !ScpBillingRows.IsCommitRow(r) && (role == "RENTAL" || role == "BK" || role == "CL")
                    && Dec(r["UnitPrice"]) == 0m && Dec(r["MinCharges"]) == 0m && S(r["MultiPriceCode"]).Trim().Length == 0)
                {
                    string grp = S(r["MergeGroupCodeMeter"]).Trim().ToUpperInvariant();
                    bool byGroup = (role == "BK" || role == "CL") && grp.Length > 0 &&
                                   groupLadders.Contains(S(r["ContractKey"]) + "|" + role + "|" + grp);
                    if (!byGroup) r[COL_UNPRICED] = true;
                }

                if (items != null)
                {
                    string code = t.Columns.Contains("ACItemCode") ? S(r["ACItemCode"]).Trim() : "";
                    if (code.Length == 0 || !items.Contains(code)) r[COL_NOITEM] = S(r["MeterType"]).Trim();
                }
            }
        }

        /// <summary>
        /// What a contract's own terms do to the money this month, worked out by the engine that
        /// bills them: the committed minimum's top-up, the rental waive, a group's agreed tiers,
        /// the strategy rules.
        ///
        /// <para>The rows are priced one by one, and a row by itself cannot know any of those -- a
        /// committed minimum is measured against the other meters' copies, a waive against the
        /// charges it targets. So the list showed 0.00 for both and "decided at Generate", and an
        /// invoice with a waive about to fire read 500.00 too high (user, 22/09: Calculation Test
        /// could show it and this screen could not). Now the pending rows go through
        /// <see cref="ScpInvoiceJobs.Build"/> -- a copy of them; Build only reads -- and each row
        /// shows the charge its invoice line will carry.</para>
        ///
        /// <para>Priced as if every pending invoice of the contract went out together, which is
        /// what Generate all ready does. A meter with no reading yet counts as nothing, exactly as
        /// Generate would count it today. <paramref name="onlyContracts"/> null = every contract.</para>
        /// </summary>
        public static void PriceTerms(DBSetting db, DataTable t, Dictionary<string, List<decimal[]>> ladders,
            int year, int month, HashSet<long> onlyContracts)
        {
            if (t == null || t.Rows.Count == 0) return;
            // Only the contracts that HAVE such a term: every other row is already priced right by
            // itself, and the engine's pass costs reads per contract (a whole month of the test book
            // took 45 s for all 2,922 contracts; 86 of them carry a minimum or a waive).
            HashSet<long> withTerms = ContractsWithTerms(db);
            HashSet<long> wanted = new HashSet<long>();
            foreach (DataRow r in t.Rows)
            {
                if (IsInvoiced(r)) continue;
                long ck = D64(r["ContractKey"]);
                if (onlyContracts != null && !onlyContracts.Contains(ck)) continue;
                bool isWaive = r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);
                bool isFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (isWaive || (isFlat && ScpBillingRows.IsCommitRow(r)) || withTerms.Contains(ck)) wanted.Add(ck);
            }
            if (wanted.Count == 0) return;

            DataTable copy = t.Copy();
            List<DataRow> all = new List<DataRow>();
            foreach (DataRow r in copy.Rows)
            {
                r["Sel"] = wanted.Contains(D64(r["ContractKey"])) && !IsInvoiced(r);
                all.Add(r);
            }
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs;
            try
            {
                int alreadyInvoiced;
                Dictionary<long, string> snapshots;
                string blockTitle, blockMessage;
                jobs = ScpInvoiceJobs.Build(db, copy, all, ladders, year, month, GroupingMode(db),
                    out alreadyInvoiced, out snapshots, out blockTitle, out blockMessage);
            }
            catch { return; }   // the list keeps its row-by-row figures; Generate says what is wrong
            if (jobs == null) return;   // e.g. a minimum counted twice -- Generate refuses it, with the reason

            Dictionary<long, decimal> charge = new Dictionary<long, decimal>();
            foreach (MeterInvoiceGenerator.InvoiceJob j in jobs.Values)
                foreach (MeterBillLine ln in j.Lines) charge[ln.ItemMeterKey] = ln.Charge;
            foreach (DataRow r in t.Rows)
            {
                if (IsInvoiced(r) || !wanted.Contains(D64(r["ContractKey"]))) continue;
                decimal c;
                if (charge.TryGetValue(D64(r["ItemMeterKey"]), out c)) r["TotalCharges"] = c;
            }
        }

        /// <summary>
        /// A TEST Fetch JSON for one contract's machines, to copy into TEST Fetch (Ctrl+Shift+T on
        /// the Meters view). One entry per machine serial, in the format the fake-API reader takes:
        /// TotalBK / TotalCL are lifetime counters, written here as the last reading + 100 (TEST
        /// Fetch's own sample rule) so a charge appears; the tester changes them to try other months.
        /// LastAuditDate is today when today is in the month, else the 1st. Nothing is fetched here.
        /// </summary>
        public static string TestFetchJson(DataTable rows, long contractKey, int year, int month)
        {
            if (rows == null) return "";
            List<string> order = new List<string>();
            Dictionary<string, string> codeOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> serialOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, decimal?[]> totals = new Dictionary<string, decimal?[]>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in rows.Rows)
            {
                if (D64(r["ContractKey"]) != contractKey) continue;
                string role = S(r["Role"]).Trim().ToUpperInvariant();
                if (role != "BK" && role != "CL") continue;
                string serial = S(r["SerialNo"]).Trim();
                string code = S(r["ServiceItemNo"]).Trim();
                string key = serial.Length > 0 ? serial : "#" + code;
                decimal?[] v;
                if (!totals.TryGetValue(key, out v))
                {
                    v = new decimal?[2];
                    totals[key] = v;
                    order.Add(key);
                    codeOf[key] = code;
                    serialOf[key] = serial;
                }
                v[role == "BK" ? 0 : 1] = Dec(r["LastReading"]) + 100m;
            }
            if (order.Count == 0) return "";

            DateTime today = DateTime.Today;
            DateTime audit = today.Year == year && today.Month == month ? today : new DateTime(year, month, 1);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[\r\n");
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                decimal?[] v = totals[key];
                sb.Append("  { \"Code\": \"").Append(JsonText(codeOf[key])).Append("\"");
                if (serialOf[key].Length > 0) sb.Append(", \"SerialNumber\": \"").Append(JsonText(serialOf[key])).Append("\"");
                if (v[0].HasValue) sb.Append(", \"TotalBK\": ").Append(v[0].Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
                if (v[1].HasValue) sb.Append(", \"TotalCL\": ").Append(v[1].Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(", \"LastAuditDate\": \"").Append(audit.ToString("yyyy-MM-dd")).Append("\" }");
                sb.Append(i < order.Count - 1 ? ",\r\n" : "\r\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string JsonText(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>Contracts whose money depends on more than one row at a time besides their
        /// minimum and waive meters: agreed group tiers and strategy rules. Never throws -- an older
        /// book without the tables simply has none.</summary>
        private static HashSet<long> ContractsWithTerms(DBSetting db)
        {
            HashSet<long> set = new HashSet<long>();
            try
            {
                DataTable k = db.GetDataTable(
                    "SELECT DISTINCT ContractKey FROM dbo.zSCP2_ContractRentalPrice " +
                    "WHERE ISNULL(LadderBk,'') <> '' OR ISNULL(LadderCl,'') <> ''", false);
                foreach (DataRow r in k.Rows) set.Add(D64(r["ContractKey"]));
            }
            catch { }
            try
            {
                DataTable k = db.GetDataTable("SELECT DISTINCT ContractKey FROM dbo.zSCP2_ContractStrategyRule", false);
                foreach (DataRow r in k.Rows) set.Add(D64(r["ContractKey"]));
            }
            catch { }
            return set;
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

        /// <summary>The date this invoice takes when nobody changes it -- the same rule Build applies,
        /// from the invoice's first row (which is also the row Build dates the job from).</summary>
        public static DateTime DefaultDocDate(InvoiceRunItem it)
        {
            if (it == null) return DateTime.Today;
            if (it.Rows.Count == 0 || it.Year <= 0 || it.Month <= 0) return it.Due == DateTime.MaxValue ? DateTime.Today : it.Due;
            return ScpInvoiceJobs.DocDateFor(it.Rows[0], it.Year, it.Month, it.RentalSide);
        }

        /// <summary>The date the invoice will carry: the operator's, else the default.</summary>
        public static DateTime EffectiveDocDate(InvoiceRunItem it)
        {
            return it != null && it.DocDateOverride.HasValue ? it.DocDateOverride.Value : DefaultDocDate(it);
        }

        /// <summary>Why this date cannot be the invoice's, or "" when it can. It must stay in the month
        /// of its default: that month decides the number series (MR2609.*) and where next month's
        /// readings start, and a date outside it would move the invoice into another period.</summary>
        public static string WhyDocDateWrong(InvoiceRunItem it, DateTime when)
        {
            DateTime def = DefaultDocDate(it);
            if (when.Year != def.Year || when.Month != def.Month)
                return "The invoice date has to stay in " + def.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")) +
                       " -- the month this invoice is billed in, which decides its number series and where next month's readings start.";
            return "";
        }

        /// <summary>The latest day any of this invoice's readings was taken, or null -- for "use the
        /// last reading date" (a machine that broke down on the 15th was last read on the 14th).</summary>
        public static DateTime? LastReadingDate(InvoiceRunItem it)
        {
            DateTime? best = null;
            if (it == null) return null;
            foreach (DataRow r in it.Rows)
            {
                if (!IsUsageMeter(r) || !HasReading(r)) continue;
                if (!r.Table.Columns.Contains("LastAuditDate") || r["LastAuditDate"] == DBNull.Value) continue;
                DateTime d = Convert.ToDateTime(r["LastAuditDate"]).Date;
                if (!best.HasValue || d > best.Value) best = d;
            }
            return best;
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

        /// <summary>The row's contract starts after today (ScpBillingRows' NotStarted).</summary>
        public static bool IsNotStarted(DataRow r)
        {
            return r.Table.Columns.Contains("NotStarted") && r["NotStarted"] != DBNull.Value && Convert.ToBoolean(r["NotStarted"]);
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
                // Feedback ATP-11: a contract whose start date is still ahead has nothing to read and
                // nothing to bill, so it makes no invoice here until the day it starts. An invoice
                // already made for it still shows, and a rent billed in advance (ATP-10) is due before
                // the start -- ScpBillingRows clears the flag on that row. The Meters view hid these
                // rows from its tabs; this list, built from the same rows, did not (found 28/9).
                if (IsNotStarted(r) && !IsInvoiced(r))
                {
                    r[COL_JOBKEY] = "";
                    r[COL_NEEDS] = false;
                    continue;
                }
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
            it.Unpriced = 0;
            it.NoItemTypes.Clear();
            it.Backwards = 0;
            HashSet<long> sameSerial = new HashSet<long>();
            bool ownTotals = it.Rows.Count > 0 && it.Rows[0].Table.Columns.Contains(COL_DOCTOTAL);
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
                    if (ownTotals)
                    {
                        // Its own invoice's total (#29 / #33); a NO CHARGE stamp is 0.00.
                        if (S(r["InvoicedDocNo"]).Trim() == invDoc) invTotal = Dec(r[COL_DOCTOTAL]);
                    }
                    // Rows loaded elsewhere carry the meter's latest invoice total only.
                    else if (S(r["LastInvNo"]).Trim() == S(r["InvoicedDocNo"]).Trim())
                    {
                        decimal t = Dec(r["InvTotal"]);
                        if (t > invTotal) invTotal = t;
                    }
                    continue;
                }
                if (StandsAlone(r)) pending++;
                // No price only matters when there is something to price: a rental, or a counter with
                // copies this month. A colour counter that never prints (a mono machine's) bills 0.00
                // rightly and must not hold up the rent -- in the test book most rate-0 counters are
                // those; SC-002599's black counter, 62,162 copies at no rate, is the kind this stops.
                if (r.Table.Columns.Contains(COL_UNPRICED) && r[COL_UNPRICED] != DBNull.Value && Convert.ToBoolean(r[COL_UNPRICED])
                    && (!IsUsageMeter(r) || Dec(r["MeterUsage"]) > 0m)) it.Unpriced++;
                if (r.Table.Columns.Contains(COL_NOITEM))
                {
                    string noItem = S(r[COL_NOITEM]).Trim();
                    if (noItem.Length > 0 && !it.NoItemTypes.Contains(noItem)) it.NoItemTypes.Add(noItem);
                }
                bool needs = r[COL_NEEDS] != DBNull.Value && Convert.ToBoolean(r[COL_NEEDS]);
                if (r.Table.Columns.Contains(COL_SAMESERIAL) && r[COL_SAMESERIAL] != DBNull.Value && Convert.ToBoolean(r[COL_SAMESERIAL]))
                    sameSerial.Add(D64(r["ItemKey"]));
                if (IsUsageMeter(r))
                {
                    anyUsage = true;
                    // Below the last reading: the engine bills 0 copies and the preview refuses it (#16).
                    if (HasReading(r) && Dec(r["CurrentReading"]) > 0m && Dec(r["CurrentReading"]) < Dec(r["LastReading"])) it.Backwards++;
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
                // A price and an item first: keying the readings of an invoice that cannot go
                // anyway is work before its time.
                it.Status = it.Unpriced > 0 ? InvoiceRunItem.UNPRICED
                          : it.NoItemTypes.Count > 0 ? InvoiceRunItem.NO_ITEM
                          : it.Backwards > 0 ? InvoiceRunItem.BACKWARD
                          : it.Missing > 0 ? InvoiceRunItem.WAITING : InvoiceRunItem.READY;
            }
            it.SameSerial = sameSerial.Count;
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
                            throw new InvalidOperationException("This reading is locked and cannot be changed.");

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
                                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, " +
                                // A date somebody TYPED survives a re-save of the same reading
                                // (feedback ATP-5): they corrected the day it was read, and
                                // re-saving the same number is not a new reading. A DIFFERENT
                                // reading is, and takes a fresh stamp.
                                "ReadingDate = CASE WHEN ISNULL(ReadingDateEdited,'N')='Y' AND CurrentReading=@rd " +
                                "                   THEN ReadingDate ELSE @dt END, " +
                                "ReadingDateEdited = CASE WHEN ISNULL(ReadingDateEdited,'N')='Y' AND CurrentReading=@rd " +
                                "                        THEN 'Y' ELSE 'N' END, " +
                                // The reference follows the same rule (feedback ATP-8): one the
                                // operator typed stays with their reading; a PUMS report id does
                                // not survive a number that did not come from that report.
                                "TrackingId = CASE WHEN Source='MANUAL' THEN TrackingId ELSE '' END, " +
                                "Source=@src, LastModified=GETDATE() " +
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

        // ─────────────────────── the day a reading was taken (feedback ATP-5) ───────────────────────

        /// <summary>The longest reference an invoice can carry: IV.RefDocNo is nvarchar(30), and
        /// the invoice's Ref is built from the machines' references joined up to that length.</summary>
        public const int REF_MAX = 30;

        /// <summary>Why this row's reading reference cannot be typed, or "" when it can (feedback
        /// ATP-8). A reading fetched from PUMS arrives with its report id and the invoice prints it
        /// as its Ref; a reading keyed by hand arrives with nothing, so the operator gives it one --
        /// the slip, the photo, the call. Only readings that are the operator's take one.</summary>
        public static string WhyRefFixed(DataRow r)
        {
            if (r == null) return "There is nothing on this row.";
            if (!IsUsageMeter(r))
                return "A rental, a minimum and a waive have no reading, so there is no reading reference to give.";
            if (IsInvoiced(r))
                return "This period is already invoiced -- its Ref is on the invoice now.";
            if (r.Table.Columns.Contains("Locked") && r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"]))
                return "This reading is locked and cannot be changed.";
            if (!HasReading(r))
                return "Key the reading in first -- the reference belongs to a reading.";
            if (S(r["EntrySource"]).Trim().ToUpperInvariant() != "MANUAL")
                return "This reading came from PUMS, so it carries PUMS's own reference. "
                     + "Key the reading in by hand (or use Key in myself) to give it yours.";
            return "";
        }

        /// <summary>Give the machine's keyed readings of the period their reference. It is written to
        /// every MANUAL meter of the machine, not just the row it was typed on: one reading slip
        /// covers the black and the colour counter, and the invoice's Ref is built per machine.
        /// Returns how many meters now carry it.</summary>
        public static int SaveReadingRef(DBSetting db, long itemKey, int year, int month, string reference)
        {
            string refNo = (reference ?? "").Trim();
            if (refNo.Length > REF_MAX)
                throw new InvalidOperationException("A reference can be at most " + REF_MAX +
                    " characters -- that is all the invoice's Ref can hold.");
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.zSCP2_MeterEntry SET TrackingId=@ref, LastModified=GETDATE() " +
                    "WHERE PeriodYear=@yr AND PeriodMonth=@mo " +
                    "  AND ItemMeterKey IN (SELECT m.ItemMeterKey FROM dbo.zSCP2_ItemMeter m WHERE m.ItemKey=@ik) " +
                    "  AND Source='MANUAL' AND LockedAt IS NULL AND InvoicedDocKey IS NULL;", cn);
                cmd.Parameters.AddWithValue("@ref", refNo);
                cmd.Parameters.AddWithValue("@yr", year);
                cmd.Parameters.AddWithValue("@mo", month);
                cmd.Parameters.AddWithValue("@ik", itemKey);
                int n = cmd.ExecuteNonQuery();
                if (n == 0)
                    throw new InvalidOperationException("This machine has no keyed reading for the period to attach it to. Refresh and try again.");
                return n;
            }
        }

        /// <summary>Why this reading cannot be taken over by hand, or "" when it can. Taking over
        /// is for the machine that could not be trusted that month -- it broke down, it was swapped,
        /// it reported a stale counter -- where the NUMBER is right but it is not the machine's word
        /// any more. Typing the number again cannot say that: a grid raises nothing when the value
        /// does not change, so the reading stays the machine's and its date stays locked.</summary>
        public static string WhyCannotTakeOver(DataRow r)
        {
            if (r == null) return "There is nothing on this row.";
            if (!IsUsageMeter(r))
                return "A rental, a minimum and a waive have no counter to read.";
            if (IsInvoiced(r))
                return "This period is already invoiced. Delete the invoice first.";
            if (r.Table.Columns.Contains("Locked") && r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"]))
                return "This reading is locked and cannot be changed.";
            if (!HasReading(r))
                return "There is no reading to take over -- just key yours in.";
            string src = S(r["EntrySource"]).Trim().ToUpperInvariant();
            if (src == "MANUAL")
                return "This reading is already yours -- the reading and its date can both be typed.";
            if (src == "INTERBILL")
                return "This counter is read in the other book and billed across. Its reading is not this book's to take over.";
            return "";
        }

        /// <summary>Take a machine's reading over by hand, keeping the number and the date it came
        /// with. Nothing about the money changes: what changes is WHOSE reading it is -- the row
        /// becomes a manual entry, so its date can be corrected, and a later fetch that disagrees
        /// raises a conflict instead of overwriting it.</summary>
        public static void TakeOverReading(DBSetting db, long itemMeterKey, int year, int month)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("RunTakeOver"))
                {
                    try
                    {
                        decimal reading = 0m;
                        string src = "";
                        DateTime? when = null;
                        bool locked = false, invoiced = false, found = false;
                        SqlCommand chk = new SqlCommand(
                            "SELECT CurrentReading, Source, ReadingDate, LockedAt, InvoicedDocKey FROM dbo.zSCP2_MeterEntry " +
                            "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn, tx);
                        chk.Parameters.AddWithValue("@imk", itemMeterKey);
                        chk.Parameters.AddWithValue("@yr", year);
                        chk.Parameters.AddWithValue("@mo", month);
                        using (SqlDataReader rd = chk.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                found = true;
                                reading = rd.IsDBNull(0) ? 0m : rd.GetDecimal(0);
                                src = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim().ToUpperInvariant();
                                when = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2);
                                locked = !rd.IsDBNull(3);
                                invoiced = !rd.IsDBNull(4);
                            }
                        }
                        if (!found)
                            throw new InvalidOperationException("There is no reading staged for this meter and period -- key yours in instead.");
                        if (locked)
                            throw new InvalidOperationException("This reading is locked and cannot be changed.");
                        if (invoiced)
                            throw new InvalidOperationException("This period is already invoiced. Delete the invoice first.");
                        if (src == "INTERBILL")
                            throw new InvalidOperationException("This counter is read in the other book and billed across. Its reading is not this book's to take over.");

                        // The number and the date stay exactly as they arrived -- the operator adjusts
                        // the date afterwards if the machine's was wrong. TrackingId stays too: where
                        // an offline reading came from is evidence, and the invoice stamps it.
                        SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.zSCP2_MeterEntry SET Source='MANUAL', LastModified=GETDATE() " +
                            "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo " +
                            "  AND LockedAt IS NULL AND InvoicedDocKey IS NULL;", cn, tx);
                        cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                        cmd.Parameters.AddWithValue("@yr", year);
                        cmd.Parameters.AddWithValue("@mo", month);
                        if (cmd.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException("The reading changed underneath. Refresh and try again.");

                        ScpMeterReadingLog.Append(cn, tx, itemMeterKey, year, month, reading, when, "MANUAL", "");
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>The day the staged reading was taken, as the database now holds it, and whether
        /// a person typed it. Read back after a save instead of guessing: saving the SAME reading
        /// again keeps a hand-set date, saving a different one stamps a fresh date, and the screen
        /// has to show whichever actually happened.</summary>
        public static DateTime? StagedDate(DBSetting db, long itemMeterKey, int year, int month, out bool edited)
        {
            string ignored;
            return StagedDate(db, itemMeterKey, year, month, out edited, out ignored);
        }

        /// <summary>The staged date, whether a person set it, and the reading's reference (the PUMS
        /// report id, or the one keyed with a manual reading) -- all as the database now holds them.</summary>
        public static DateTime? StagedDate(DBSetting db, long itemMeterKey, int year, int month,
                                           out bool edited, out string reference)
        {
            edited = false;
            reference = "";
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT ReadingDate, ISNULL(ReadingDateEdited,'N'), ISNULL(TrackingId,'') FROM dbo.zSCP2_MeterEntry " +
                    "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn);
                cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                cmd.Parameters.AddWithValue("@yr", year);
                cmd.Parameters.AddWithValue("@mo", month);
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    edited = !rd.IsDBNull(1) && rd.GetString(1).Trim().ToUpperInvariant() == "Y";
                    reference = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim();
                    return rd.IsDBNull(0) ? (DateTime?)null : rd.GetDateTime(0);
                }
            }
        }

        /// <summary>Why the day this reading was taken cannot be changed on this row, or "" when it
        /// can. The date is only the operator's to set where the reading itself was theirs to type:
        /// a machine's own audit date, a locked billing-day snapshot and an invoiced period all keep
        /// the date they were given.</summary>
        public static string WhyDateFixed(DataRow r)
        {
            if (r == null) return "There is nothing on this row.";
            if (!IsUsageMeter(r))
                return "This row has no meter reading, so it has no date.";
            if (IsInvoiced(r))
                return "This month is already invoiced. Delete the invoice first to change the date.";
            if (r.Table.Columns.Contains("Locked") && r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"]))
                return "This reading is locked and cannot be changed.";
            if (!HasReading(r))
                return "Key in the reading first.";
            if (S(r["EntrySource"]).Trim().ToUpperInvariant() != "MANUAL")
                return "This reading came from the machine. Press Key in myself first to change its date.";
            return "";
        }

        /// <summary>Why this day cannot be the day the reading was taken, or "" when it can. Two
        /// rules, and both of them protect the NEXT period: this date is stamped on the meter
        /// transaction, so it becomes the following invoice's "Last Read Date".</summary>
        public static string WhyDateWrong(DataRow r, DateTime when)
        {
            if (when.Date > DateTime.Today)
                return "The date cannot be later than today (" + DateTime.Today.ToString("dd/MM/yyyy") + ").";   // user, 26/9: say it plainly
            if (r != null && r.Table.Columns.Contains("LastReadDate") && r["LastReadDate"] != DBNull.Value)
            {
                DateTime last = Convert.ToDateTime(r["LastReadDate"]).Date;
                if (when.Date < last)
                    return "The date cannot be earlier than the last reading (" + last.ToString("dd/MM/yyyy") + ").";
            }
            return "";
        }

        /// <summary>Move the day a KEYED reading was taken. The reading, its source and its money do
        /// not change -- only the day, which is the day the invoice prints, the day the meter
        /// transaction carries, and therefore next period's "Last Read Date". The row is marked as
        /// hand-set so a later fetch of the same number leaves it alone.</summary>
        public static void SaveReadingDate(DBSetting db, long itemMeterKey, int year, int month, DateTime when)
        {
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("RunReadDate"))
                {
                    try
                    {
                        decimal reading = 0m;
                        string src = "";
                        bool locked = false, invoiced = false, found = false;
                        SqlCommand chk = new SqlCommand(
                            "SELECT CurrentReading, Source, LockedAt, InvoicedDocKey FROM dbo.zSCP2_MeterEntry " +
                            "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn, tx);
                        chk.Parameters.AddWithValue("@imk", itemMeterKey);
                        chk.Parameters.AddWithValue("@yr", year);
                        chk.Parameters.AddWithValue("@mo", month);
                        using (SqlDataReader rd = chk.ExecuteReader())
                        {
                            if (rd.Read())
                            {
                                found = true;
                                reading = rd.IsDBNull(0) ? 0m : rd.GetDecimal(0);
                                src = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim().ToUpperInvariant();
                                locked = !rd.IsDBNull(2);
                                invoiced = !rd.IsDBNull(3);
                            }
                        }
                        if (!found)
                            throw new InvalidOperationException("There is no reading staged for this meter and period -- key the reading in first.");
                        if (locked)
                            throw new InvalidOperationException("This reading is locked and cannot be changed.");
                        if (invoiced)
                            throw new InvalidOperationException("This period is already invoiced. Delete the invoice first if its date is wrong.");
                        if (src != "MANUAL")
                            throw new InvalidOperationException("This reading came from the machine (" + src + "), so its date is the machine's own audit date.");

                        SqlCommand cmd = new SqlCommand(
                            "UPDATE dbo.zSCP2_MeterEntry SET ReadingDate=@dt, ReadingDateEdited='Y', LastModified=GETDATE() " +
                            "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo " +
                            "  AND LockedAt IS NULL AND InvoicedDocKey IS NULL AND Source='MANUAL';", cn, tx);
                        cmd.Parameters.AddWithValue("@imk", itemMeterKey);
                        cmd.Parameters.AddWithValue("@yr", year);
                        cmd.Parameters.AddWithValue("@mo", month);
                        cmd.Parameters.AddWithValue("@dt", when);
                        if (cmd.ExecuteNonQuery() == 0)
                            throw new InvalidOperationException("The reading changed while the date was being typed. Refresh and try again.");

                        // The reading log is the trail: same reading, new date, said out loud.
                        ScpMeterReadingLog.Append(cn, tx, itemMeterKey, year, month, reading, when, "MANUAL", "");
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
