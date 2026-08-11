using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Report;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// The documents a contract asks to be sent ALONGSIDE the invoice: the Statement of Account and
    /// the Summary sales invoice meter listing (the customer's Appendix A).
    ///
    /// Both were configurable per contract long before anything read those settings — the tick boxes
    /// and template pickers saved to zSCP2_Contract and stopped there, while the tooltip told the
    /// operator the listing was "sent with the invoice and the SOA". This is the consumer that makes
    /// those settings true.
    ///
    /// Same split as the invoice renderer: <see cref="Prepare"/> touches AutoCount's report registry
    /// on the UI thread, <see cref="Attach"/> renders on the job's thread. A document a contract asks
    /// for and we cannot produce FAILS that recipient — quietly dropping it is how a setting becomes
    /// a lie.
    /// </summary>
    public class ScpExtraDocs
    {
        /// <summary>What one contract in this send asks for.</summary>
        private class Want
        {
            public string ContractNo = "";
            public string DebtorCode = "";
            public bool Soa;
            public string SoaLayout = "";
            public bool Listing;
            public string ListingLayout = "";
            public int Year;
            public int Month;
        }

        private readonly DBSetting _db;
        private readonly UserSession _us;
        private readonly object _gate = new object();
        private readonly Dictionary<string, List<Want>> _byDebtor =
            new Dictionary<string, List<Want>>(StringComparer.OrdinalIgnoreCase);
        private string _soaDefault = "";
        private string _listingDefault = "";

        /// <summary>Problems worth showing BEFORE the send starts, not one failed email at a time.</summary>
        public readonly List<string> Warnings = new List<string>();

        public ScpExtraDocs(DBSetting db, UserSession us) { _db = db; _us = us; }

        /// <summary>True when at least one contract in this batch asks for something extra.</summary>
        public bool Any { get { return _byDebtor.Count > 0; } }

        // ───────────────────────── prepare (UI thread) ─────────────────────────

        /// <summary>
        /// Work out which customer gets what. A contract's tick applies to the customer that
        /// contract belongs to, for the billing period the invoice was generated for.
        /// </summary>
        public void Prepare(List<long> docKeys)
        {
            _byDebtor.Clear();
            Warnings.Clear();
            if (_db == null || docKeys == null || docKeys.Count == 0) return;

            DataTable t;
            try
            {
                t = _db.GetDataTable(
                    "SELECT DISTINCT c.ContractNo, c.DebtorCode, " +
                    "  ISNULL(c.GenerateSOA,'N') AS GenerateSOA, ISNULL(c.SOAReportName,'') AS SOAReportName, " +
                    "  ISNULL(c.GenerateMeterListing,'N') AS GenerateMeterListing, " +
                    "  ISNULL(c.MeterListingReportName,'') AS MeterListingReportName, " +
                    "  me.PeriodYear, me.PeriodMonth " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = me.ItemMeterKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "WHERE me.InvoicedDocKey IN (" + Keys(docKeys) + ") " +
                    "  AND (ISNULL(c.GenerateSOA,'N') = 'Y' OR ISNULL(c.GenerateMeterListing,'N') = 'Y')",
                    false);
            }
            catch (Exception ex)
            {
                // Never silent: if we cannot tell who wants what, say so instead of sending
                // invoice-only and letting it look intentional.
                Warnings.Add("Could not read the contracts' SOA / listing settings: " + ex.Message);
                return;
            }

            foreach (DataRow r in t.Rows)
            {
                Want w = new Want();
                w.ContractNo = Str(r["ContractNo"]);
                w.DebtorCode = Str(r["DebtorCode"]);
                w.Soa = Str(r["GenerateSOA"]) == "Y";
                w.SoaLayout = Str(r["SOAReportName"]);
                w.Listing = Str(r["GenerateMeterListing"]) == "Y";
                w.ListingLayout = Str(r["MeterListingReportName"]);
                w.Year = r["PeriodYear"] == DBNull.Value ? 0 : Convert.ToInt32(r["PeriodYear"]);
                w.Month = r["PeriodMonth"] == DBNull.Value ? 0 : Convert.ToInt32(r["PeriodMonth"]);
                if (w.DebtorCode.Length == 0) continue;

                List<Want> list;
                if (!_byDebtor.TryGetValue(w.DebtorCode, out list))
                {
                    list = new List<Want>();
                    _byDebtor[w.DebtorCode] = list;
                }
                list.Add(w);
            }
            if (_byDebtor.Count == 0) return;

            // Resolve the book defaults once, and check now — while the operator is still looking at
            // a screen — that every layout a contract names actually exists.
            _soaDefault = DefaultLayout(AutoCount.ARAP.DebtorStatement.DebtorStatementReport.ReportType);
            _listingDefault = DefaultLayout(ScpMeterListing.REPORT_TYPE);

            // AutoCount excludes StatementType='N' debtors from every statement query, so a contract
            // ticked for SOA against such a debtor can never produce one. Better to say it here than
            // to fail that customer mid-send.
            try
            {
                DataTable mute = _db.GetDataTable(
                    "SELECT AccNo FROM dbo.Debtor WHERE ISNULL(StatementType,'') = 'N'", false);
                foreach (DataRow m in mute.Rows)
                {
                    string acc = Str(m["AccNo"]);
                    List<Want> l;
                    if (!_byDebtor.TryGetValue(acc, out l)) continue;
                    foreach (Want w in l)
                        if (w.Soa)
                        {
                            Warnings.Add("Customer " + acc + " is set to receive NO statement " +
                                "(Debtor Maintenance > Statement Type), so contract " + w.ContractNo +
                                "'s SOA cannot be produced.");
                            break;
                        }
                }
            }
            catch { }

            foreach (KeyValuePair<string, List<Want>> kv in _byDebtor)
                foreach (Want w in kv.Value)
                {
                    if (w.Soa && ResolveLayout(AutoCount.ARAP.DebtorStatement.DebtorStatementReport.ReportType,
                            w.SoaLayout, _soaDefault).Length == 0)
                        Warnings.Add("Contract " + w.ContractNo + " asks for an SOA but there is no " +
                            "Debtor Statement report design to print it with.");
                    if (w.Listing && ResolveLayout(ScpMeterListing.REPORT_TYPE,
                            w.ListingLayout, _listingDefault).Length == 0)
                        Warnings.Add("Contract " + w.ContractNo + " asks for the meter listing but there " +
                            "is no '" + ScpMeterListing.REPORT_TYPE + "' report design to print it with " +
                            "(create one in that screen's Report Design).");
                }
        }

        /// <summary>One line per customer for the confirmation grid, so the operator sees what is
        /// going out before it goes out.</summary>
        public string DescribeFor(string debtorCode)
        {
            List<Want> list;
            if (!_byDebtor.TryGetValue((debtorCode ?? "").Trim(), out list)) return "";
            bool soa = false, listing = false;
            foreach (Want w in list) { if (w.Soa) soa = true; if (w.Listing) listing = true; }
            if (soa && listing) return "SOA + Meter listing";
            if (soa) return "SOA";
            if (listing) return "Meter listing";
            return "";
        }

        // ───────────────────────── attach (job thread) ─────────────────────────

        /// <summary>
        /// Render and attach this customer's extras. Throws if a contract asked for a document that
        /// could not be produced — the recipient then fails with a reason, rather than receiving a
        /// short email that looks complete.
        /// </summary>
        public void Attach(DBSetting db, ScpEmailJob.Recipient r)
        {
            if (r == null) return;
            List<Want> list;
            if (!_byDebtor.TryGetValue((r.DebtorCode ?? "").Trim(), out list)) return;

            lock (_gate)
            {
                // One statement per customer, not per contract: a debtor has ONE account, so two
                // contracts both asking for an SOA still means one statement.
                Want soaWant = null;
                foreach (Want w in list) if (w.Soa) { soaWant = w; break; }
                if (soaWant != null)
                {
                    string file;
                    byte[] pdf = RenderSoa(r.DebtorCode, r.DebtorName, soaWant, out file);
                    if (pdf == null || pdf.Length == 0)
                        throw new Exception("Contract " + soaWant.ContractNo + " asks for an SOA but it " +
                            "could not be produced. Nothing was sent to this customer.");
                    Add(r, file, pdf);
                }

                // The listing is per CONTRACT — it is that contract's Appendix A, and a customer with
                // two contracts is owed two of them.
                foreach (Want w in list)
                {
                    if (!w.Listing) continue;
                    string file;
                    byte[] pdf = RenderListing(w, out file);
                    if (pdf == null || pdf.Length == 0)
                        throw new Exception("Contract " + w.ContractNo + " asks for the meter listing " +
                            "but it could not be produced. Nothing was sent to this customer.");
                    Add(r, file, pdf);
                }
            }
        }

        private static void Add(ScpEmailJob.Recipient r, string fileName, byte[] pdf)
        {
            AutoCount.Mail.AttachmentData a = new AutoCount.Mail.AttachmentData();
            a.FileName = fileName;
            a.Binary = pdf;
            r.Attachments.Add(a);
        }

        // ───────────────────────── SOA ─────────────────────────

        /// <summary>
        /// The customer's statement, built the way AutoCount's own Debtor Statement builds it, so the
        /// PDF is the same document they would get from A/R > Debtor Statement — not a lookalike.
        /// </summary>
        private byte[] RenderSoa(string accNo, string debtorName, Want w, out string fileName)
        {
            fileName = "Statement " + SafeFileName(accNo) + ".pdf";
            DateTime to = PeriodEnd(w);
            DateTime from = new DateTime(to.Year, to.Month, 1);

            AutoCount.ARAP.DebtorStatement.DebtorStatement st =
                AutoCount.ARAP.DebtorStatement.DebtorStatement.Create(_us);
            AutoCount.ARAP.DebtorStatement.DebtorStatementCriteria c =
                new AutoCount.ARAP.DebtorStatement.DebtorStatementCriteria();
            c.DebtorFilter.ByOne(accNo);
            c.FromDate = from;
            c.ToDate = to;
            // With this left false AutoCount adds "AND IsGroupCompany = 'F'" and a group-company
            // debtor comes back with no statement at all. We asked for ONE named debtor — give us
            // that debtor whichever kind they are.
            c.ShowGroupCompany = true;
            st.Inquire(c);

            object ds = st.GetReportDataSource();
            if (ds == null) return null;

            string layout = ResolveLayout(
                AutoCount.ARAP.DebtorStatement.DebtorStatementReport.ReportType, w.SoaLayout, _soaDefault);
            if (layout.Length == 0) return null;

            fileName = "Statement " + SafeFileName(accNo) + " " + to.ToString("yyyy-MM") + ".pdf";
            return RenderTo(layout, ds);
        }

        // ───────────────────────── meter listing ─────────────────────────

        /// <summary>That contract's Appendix A for the billing month, from the same builder the
        /// inquiry screen uses — one source of truth for what the listing says.</summary>
        private byte[] RenderListing(Want w, out string fileName)
        {
            fileName = "Meter Listing " + SafeFileName(w.ContractNo) + ".pdf";
            if (w.Year <= 0 || w.Month <= 0) return null;

            DataTable data = ScpMeterListing.Build(_db, w.Year, w.Month, w.DebtorCode, w.DebtorCode, w.ContractNo);
            if (data == null || data.Rows.Count == 0) return null;

            string layout = ResolveLayout(ScpMeterListing.REPORT_TYPE, w.ListingLayout, _listingDefault);
            if (layout.Length == 0) return null;

            fileName = "Meter Listing " + SafeFileName(w.ContractNo) + " " +
                w.Year.ToString("0000") + "-" + w.Month.ToString("00") + ".pdf";
            return RenderTo(layout, data);
        }

        // ───────────────────────── shared plumbing ─────────────────────────

        private byte[] RenderTo(string layoutName, object dataSource)
        {
            ReportTemplate tpl = AutoCountReport.GetInstance().GetReport(layoutName, dataSource, _us, true);
            XtraReport xr = tpl != null ? tpl.Report as XtraReport : null;
            if (xr == null) return null;
            ScpReportScripts.FixReferences(xr);
            xr.DataSource = dataSource;
            xr.CreateDocument();
            using (MemoryStream ms = new MemoryStream())
            {
                xr.ExportToPdf(ms);
                return ms.ToArray();
            }
        }

        /// <summary>The contract's own layout when it exists, else the book default. Returns "" when
        /// neither does — the caller turns that into a visible failure, never a silent omission.</summary>
        private string ResolveLayout(string reportType, string wanted, string fallback)
        {
            string name = (wanted ?? "").Trim();
            if (name.Length > 0 && LayoutExists(reportType, name)) return name;
            if (name.Length > 0)
                Warn("Report design '" + name + "' no longer exists — using the default instead.");
            return (fallback ?? "").Trim();
        }

        private bool LayoutExists(string reportType, string name)
        {
            try
            {
                DataTable list = AutoCountReport.GetInstance().GetReportList(_db, reportType);
                if (list == null) return false;
                foreach (DataRow r in list.Rows)
                    if (string.Equals(Str(r["ReportName"]), name, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            catch { }
            return false;
        }

        /// <summary>The book's chosen design, else the first that exists. Never prompts — a modal
        /// picker inside a bulk run cannot be answered per customer.</summary>
        private string DefaultLayout(string reportType)
        {
            string name = "";
            try { name = ReportDBUtil.Create(_db).GetDefaultReport(reportType) ?? ""; }
            catch { }
            if (name.Length > 0 && LayoutExists(reportType, name)) return name;
            try
            {
                DataTable list = AutoCountReport.GetInstance().GetReportList(_db, reportType);
                if (list != null && list.Rows.Count > 0) return Str(list.Rows[0]["ReportName"]);
            }
            catch { }
            return "";
        }

        private void Warn(string message)
        {
            if (!Warnings.Contains(message)) Warnings.Add(message);
        }

        private static DateTime PeriodEnd(Want w)
        {
            if (w.Year <= 0 || w.Month <= 0) return DateTime.Today;
            return new DateTime(w.Year, w.Month, DateTime.DaysInMonth(w.Year, w.Month));
        }

        private static string Keys(List<long> keys)
        {
            string[] s = new string[keys.Count];
            for (int i = 0; i < keys.Count; i++) s[i] = keys[i].ToString();
            return s.Length == 0 ? "0" : string.Join(",", s);
        }

        private static string Str(object v) { return v == null || v == DBNull.Value ? "" : Convert.ToString(v); }

        private static string SafeFileName(string s)
        {
            string name = s ?? "";
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in bad) name = name.Replace(c, '-');
            return name;
        }
    }
}
