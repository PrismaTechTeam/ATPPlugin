using System;
using System.Collections.Generic;
using System.IO;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Invoicing.Sales.Invoice;
using AutoCount.Report;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Renders invoice PDFs for the bulk email job.
    ///
    /// The split matters. Everything that touches AutoCount's report REGISTRY — looking a layout up
    /// by name, applying the book's report options — happens once, on the UI thread, in Prepare().
    /// After that <see cref="Render"/> only swaps a DataSource and exports, which is plain DevExpress
    /// work and runs happily on the job's thread.
    ///
    /// That is what stops the form freezing: previously every invoice was rendered up front, on the
    /// UI thread, before the operator got control back. Rendering is now spread through the job,
    /// alongside the sending it feeds.
    /// </summary>
    public class ScpInvoicePdfRenderer
    {
        private readonly DBSetting _db;
        private readonly UserSession _us;
        private InvoiceListingReport _rpt;
        private readonly Dictionary<string, XtraReport> _byTemplate =
            new Dictionary<string, XtraReport>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<long, string> _templateByDoc = new Dictionary<long, string>();
        private readonly object _gate = new object();

        /// <summary>Layout names named on a contract that no longer exist (renamed/deleted).</summary>
        public readonly List<string> MissingTemplates = new List<string>();
        /// <summary>
        /// The default layout, named ONLY when a document in this batch actually lands on it.
        /// The default is always prepared as a safety net, so "it was resolved" says nothing about
        /// whether anything uses it — that distinction is the whole point of this field.
        /// </summary>
        public string FallbackTemplateUsed = "";
        /// <summary>How many documents in the batch fall back to the default layout.</summary>
        public int FallbackDocCount = 0;
        private string _defaultName = "";

        public ScpInvoicePdfRenderer(DBSetting db, UserSession us)
        {
            _db = db; _us = us;
        }

        /// <summary>
        /// UI-THREAD ONLY. Resolves the report registry: which layout each document needs, and one
        /// live XtraReport per distinct layout. Cheap — a handful of designs, not one per invoice.
        /// </summary>
        public bool Prepare(List<long> docKeys, out string error)
        {
            error = "";
            try
            {
                _rpt = InvoiceListingReport.Create(_us);

                // #9c: the layout each invoice's CONTRACT names, resolved through the meter stamps.
                try
                {
                    System.Data.DataTable tpl = _db.GetDataTable(
                        "SELECT me.InvoicedDocKey AS DocKey, MAX(ISNULL(c.InvoiceReportName,'')) AS Tpl " +
                        "FROM dbo.zSCP2_MeterEntry me " +
                        "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = me.ItemMeterKey " +
                        "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                        "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                        "WHERE me.InvoicedDocKey IS NOT NULL AND ISNULL(c.InvoiceReportName,'') <> '' " +
                        "GROUP BY me.InvoicedDocKey", false);
                    foreach (System.Data.DataRow t in tpl.Rows)
                        _templateByDoc[Convert.ToInt64(t["DocKey"])] = Convert.ToString(t["Tpl"]).Trim();
                }
                catch { }   // unresolvable -> everything uses the default layout

                // One sample datasource is enough to instantiate each layout; Render swaps it later.
                if (docKeys.Count == 0) { error = "Nothing to render."; return false; }
                object sample = _rpt.GetReportDataSource(docKeys[0]);

                List<string> needed = new List<string>();
                needed.Add("");                                  // the default layout
                foreach (long dk in docKeys)
                {
                    string n;
                    if (_templateByDoc.TryGetValue(dk, out n) && n.Length > 0 && !needed.Contains(n))
                        needed.Add(n);
                }

                foreach (string name in needed)
                {
                    XtraReport xr = null;
                    if (name.Length > 0)
                    {
                        try
                        {
                            ReportTemplate t = AutoCountReport.GetInstance().GetReport(name, sample, _us, true);
                            xr = t != null ? t.Report as XtraReport : null;
                        }
                        catch { xr = null; }
                        if (xr == null) MissingTemplates.Add(name);
                        else { ApplyOption(xr); ScpReportScripts.FixReferences(xr); }
                    }
                    if (xr == null && name.Length > 0)
                    {
                        // Named layout is gone — that document falls back to the default entry.
                        continue;
                    }
                    if (xr == null) xr = ResolveDefault(sample);
                    if (xr == null)
                    {
                        error = "No 'Invoice Document' report layout exists in this book.";
                        return false;
                    }
                    _byTemplate[name] = xr;
                }

                // Only NOW can the fallback be reported honestly: count the documents that really
                // land on the default, rather than assuming any that resolved it are using it.
                foreach (long dk in docKeys)
                    if (ResolvedLayoutName(dk).Length == 0) FallbackDocCount++;
                if (FallbackDocCount > 0) FallbackTemplateUsed = _defaultName;

                return _byTemplate.ContainsKey("");
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// The default layout: the book's chosen one, else the first that exists. Never prompts — a
        /// modal picker during a bulk run stalls everything and cannot be answered per customer.
        /// </summary>
        private XtraReport ResolveDefault(object sample)
        {
            AutoCountReport api = AutoCountReport.GetInstance();
            string name = "";
            try { name = ReportDBUtil.Create(_db).GetDefaultReport("Invoice Document"); }
            catch { }
            if (string.IsNullOrEmpty(name))
            {
                try
                {
                    System.Data.DataTable list = api.GetReportList(_db, "Invoice Document");
                    if (list != null && list.Rows.Count > 0)
                        name = Convert.ToString(list.Rows[0]["ReportName"]).Trim();
                }
                catch { }
            }
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                ReportTemplate t = api.GetReport(name, sample, _us, true);
                XtraReport xr = t != null ? t.Report as XtraReport : null;
                if (xr == null) return null;
                ApplyOption(xr);
                ScpReportScripts.FixReferences(xr);
                _defaultName = name;
                return xr;
            }
            catch { return null; }
        }

        /// <summary>
        /// The layout a document will really be rendered with: the contract's, or "" when it named
        /// none — or named one that has since gone missing, which falls back the same way.
        /// </summary>
        private string ResolvedLayoutName(long docKey)
        {
            string name;
            if (!_templateByDoc.TryGetValue(docKey, out name) || name == null) return "";
            name = name.Trim();
            if (name.Length == 0) return "";
            return _byTemplate.ContainsKey(name) ? name : "";
        }

        /// <summary>The layout name to show the operator for one invoice, before anything is sent.</summary>
        public string LayoutFor(long docKey)
        {
            string name = ResolvedLayoutName(docKey);
            return name.Length > 0 ? name : (_defaultName.Length > 0 ? _defaultName + "  (default)" : "(default)");
        }

        /// <summary>Margins / paper / print-in-black, the same treatment the default print path
        /// gives a layout. ApplyReportOption is private, so reflect; a miss just leaves the design.</summary>
        private void ApplyOption(XtraReport xr)
        {
            try
            {
                System.Reflection.MethodInfo aro = typeof(ReportTool).GetMethod("ApplyReportOption",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (aro != null) aro.Invoke(null, new object[] { xr, _rpt.GetBasicReportOption() });
            }
            catch { }
        }

        /// <summary>
        /// JOB-THREAD SAFE. Produces one invoice PDF. Serialised on a single lock because the cached
        /// XtraReport instances are shared — rendering two documents through the same layout object
        /// at once would interleave their pages.
        /// </summary>
        public byte[] Render(long docKey, string docNo, out string fileName)
        {
            fileName = "Invoice " + SafeFileName(docNo) + ".pdf";
            lock (_gate)
            {
                string name;
                if (!_templateByDoc.TryGetValue(docKey, out name) || name == null) name = "";
                XtraReport xr;
                if (!_byTemplate.TryGetValue(name, out xr)) xr = _byTemplate[""];   // named one was missing
                if (xr == null) return null;

                object ds = _rpt.GetReportDataSource(docKey);
                xr.DataSource = ds;
                try
                {
                    xr.CreateDocument();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        xr.ExportToPdf(ms);
                        return ms.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw ScpReportScripts.Explain(ex, name.Length > 0 ? name : _defaultName);
                }
            }
        }

        private static string SafeFileName(string s)
        {
            string name = s ?? "";
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in bad) name = name.Replace(c, '-');
            return name;
        }
    }
}
