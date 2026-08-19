using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Invoicing;
using AutoCount.Invoicing.Sales.Invoice;
using AutoCount.Report;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Renders the sample invoices through AutoCount's OWN Sales Invoice layout — the same design a
    /// real printed invoice uses — without an invoice existing.
    ///
    /// <para>The trick is that AutoCount's print path is already split in two: one half turns a
    /// DocKey into a DataSet, the other half turns a DataSet into a document. Only the first half
    /// needs a saved invoice. So this class builds the DataSet by hand — the same four tables, in the
    /// same order, with the same columns — and hands it to the second half untouched. Nothing is
    /// inserted, no document number is drawn, and nothing is posted. The book is opened READ-ONLY:
    /// the only statements it issues are SELECTs against views and master tables.</para>
    ///
    /// <para>Every column comes from the book, never from a hardcoded list: each table's schema is
    /// loaded with a <c>WHERE 1=0</c> query against the very view the real path reads, so a book with
    /// extra UDF columns, a different AutoCount build, or a renamed field still produces a row shaped
    /// exactly the way the layout expects. Only the handful of fields that carry MEANING — who is
    /// billed, for what, how much — are written by hand; every other number is zeroed so the tax and
    /// total arithmetic has something to work on, and every other text field is left NULL, which is
    /// what the book itself stores and what the layouts are drawn against.</para>
    ///
    /// <para>Failure is a return value, never an exception. The caller keeps the hand-drawn
    /// <see cref="ScpSampleInvoiceReport"/> as its fallback, and a preview that cannot be built must
    /// degrade to the one that can — not take the form down with it.</para>
    /// </summary>
    public static class ScpSampleInvoiceDocSource
    {
        /// <summary>What the layout is called when the contract names none.</summary>
        public const string ReportType = "Invoice Document";

        /// <summary>
        /// Builds a rendered document for the given sample invoices, using the book's real invoice
        /// layout. Several invoices come back as several documents in one report, exactly the way the
        /// real print path batches them.
        /// </summary>
        /// <param name="db">The account book. Read only — nothing is written.</param>
        /// <param name="us">The logged-in session; the report registry needs it.</param>
        /// <param name="templateName">The contract's Invoice Template. Empty (or missing) falls back
        /// to the book's default invoice layout.</param>
        /// <param name="invoices">The sample invoices the billing engine produced.</param>
        /// <param name="layoutName">The layout that was actually used, for the caller to show.</param>
        /// <param name="error">Why nothing came back. Empty on success.</param>
        /// <returns>A report with CreateDocument() already called, or null.</returns>
        public static XtraReport Build(DBSetting db, UserSession us, string templateName,
            IList<SampleInvoiceDoc> invoices, out string layoutName, out string error)
        {
            layoutName = "";
            error = "";
            if (db == null || us == null) { error = "No account book session."; return null; }
            if (invoices == null || invoices.Count == 0) { error = "Nothing to preview."; return null; }

            InvoiceListingReport rpt;
            object dataSource;
            try
            {
                rpt = InvoiceListingReport.Create(us);
                DataSet ds = BuildDataSet(db, invoices);

                // The book's own document controls: auto-numbering, line merging, the "Sub Total"
                // and "Unit Price" wording. Skipping this would print a document the operator does
                // not recognise as theirs.
                try { DocumentReportControl.RunDocumentReportControl(us, ds, "IV"); }
                catch { }

                dataSource = ToDocumentReportDataSet(rpt, us, ds, out error);
                if (dataSource == null) return null;
            }
            catch (Exception ex)
            {
                error = "Could not shape the invoice data: " + ex.Message;
                return null;
            }

            string name = (templateName ?? "").Trim();
            XtraReport xr = null;
            if (name.Length > 0)
            {
                xr = LoadLayout(rpt, us, name, dataSource);
                if (xr == null) name = "";      // named layout is gone — fall back, do not fail
            }
            if (xr == null)
            {
                name = DefaultLayoutName(db);
                if (name.Length == 0)
                {
                    error = "No '" + ReportType + "' report layout exists in this book.";
                    return null;
                }
                xr = LoadLayout(rpt, us, name, dataSource);
                if (xr == null)
                {
                    error = "The invoice layout '" + name + "' could not be loaded.";
                    return null;
                }
            }

            layoutName = name;
            try
            {
                xr.DataSource = dataSource;
                xr.CreateDocument();
            }
            catch (Exception ex)
            {
                error = "The invoice layout '" + name + "' could not render: " + ex.Message;
                return null;
            }
            return xr;
        }

        // ---- the layout ------------------------------------------------------------------------

        /// <summary>One layout, prepared the way the real print path prepares it.</summary>
        private static XtraReport LoadLayout(InvoiceListingReport rpt, UserSession us, string name, object dataSource)
        {
            try
            {
                ReportTemplate t = AutoCountReport.GetInstance().GetReport(name, dataSource, us, true);
                XtraReport xr = t != null ? t.Report as XtraReport : null;
                if (xr == null) return null;
                ApplyOption(rpt, xr);
                ScpReportScripts.Prepare(xr);
                return xr;
            }
            catch { return null; }
        }

        /// <summary>The book's chosen invoice layout, else the first one that exists.</summary>
        private static string DefaultLayoutName(DBSetting db)
        {
            string name = "";
            try { name = (ReportDBUtil.Create(db).GetDefaultReport(ReportType) ?? "").Trim(); }
            catch { }
            if (name.Length > 0) return name;
            try
            {
                DataTable list = AutoCountReport.GetInstance().GetReportList(db, ReportType);
                if (list != null && list.Rows.Count > 0)
                    name = Convert.ToString(list.Rows[0]["ReportName"]).Trim();
            }
            catch { }
            return name;
        }

        /// <summary>Margins / paper / print-in-black, as the default print path applies them.
        /// ApplyReportOption is private, so reflect; a miss just leaves the design as designed.</summary>
        private static void ApplyOption(InvoiceListingReport rpt, XtraReport xr)
        {
            try
            {
                MethodInfo aro = typeof(ReportTool).GetMethod("ApplyReportOption",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (aro != null) aro.Invoke(null, new object[] { xr, rpt.GetBasicReportOption() });
            }
            catch { }
        }

        /// <summary>
        /// The last step of AutoCount's own GetReportDataSource: wrap the four raw tables into the
        /// DocumentReportDataSet the layouts are designed against — relations, tax summary, number-
        /// to-words, e-invoice classification, the lot.
        ///
        /// <para>PreparingReportDataSet is private, so it is reached by reflection. That is
        /// deliberate: re-implementing it would mean re-implementing a dozen AutoCount helpers and
        /// then keeping them in step with every AutoCount release. Reflecting into it means the
        /// preview is prepared by the SAME code the printer uses, and if a future build renames it,
        /// this returns a clear reason instead of quietly drifting.</para>
        /// </summary>
        private static object ToDocumentReportDataSet(InvoiceListingReport rpt, UserSession us,
            DataSet ds, out string error)
        {
            error = "";
            MethodInfo mi = typeof(InvoiceListingReport).GetMethod("PreparingReportDataSet",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null)
            {
                error = "This AutoCount build no longer exposes InvoiceListingReport.PreparingReportDataSet.";
                return null;
            }
            try
            {
                return mi.Invoke(rpt, new object[] { ds });
            }
            catch (TargetInvocationException tex)
            {
                Exception inner = tex.InnerException ?? tex;
                error = "AutoCount could not prepare the invoice data set: " + inner.Message;
                return null;
            }
            catch (Exception ex)
            {
                error = "AutoCount could not prepare the invoice data set: " + ex.Message;
                return null;
            }
        }

        // ---- the data --------------------------------------------------------------------------

        /// <summary>
        /// The four tables AutoCount's LoadReportData produces, in the order PreparingReportDataSet
        /// consumes them — it takes Tables[0] three times and then TaxEntity by name, so the ORDER
        /// here is load-bearing.
        /// </summary>
        private static DataSet BuildDataSet(DBSetting db, IList<SampleInvoiceDoc> invoices)
        {
            DataSet ds = new DataSet();

            // Schema only. Same views, same aliases, same join as the real path — so the column set
            // (including the ItemPrice columns and the SG_* aliases) is identical.
            db.LoadDataSet(ds, "Master", "SELECT * FROM vInvoice WHERE 1=0", false);
            db.LoadDataSet(ds, "Detail",
                " SELECT A.*, C.SuppCustItemCode as CustomerItemCode, C.Ref,  A.Location AS SG_Location," +
                " A.ProjNo AS SG_ProjNo, A.DeptNo AS SG_DeptNo, A.BatchNo AS SG_BatchNo," +
                " A.FromDocNo AS SG_FromDocNo  FROM  vInvoiceDetail A INNER JOIN vInvoice B " +
                " ON A.DocKey = B.DocKey Left Outer Join ItemPrice C " +
                " ON (B.DebtorCode = C.AccNo AND A.ItemCode = C.ItemCode AND A.UOM = C.UOM)  WHERE 1=0", false);
            db.LoadDataSet(ds, "PackageDetail", " SELECT A.* FROM vInvoiceSubDetail A WHERE 1=0", false);
            db.LoadDataSet(ds, "TaxEntity", "SELECT * FROM vTaxEntity", false);

            DataTable master = ds.Tables["Master"];
            DataTable detail = ds.Tables["Detail"];

            int taxEntityId = FirstTaxEntityId(ds.Tables["TaxEntity"]);
            int qtyDecimals = QtyScale(db);
            DateTime today = DateTime.Today;

            int lineCount = 0;
            for (int i = 0; i < invoices.Count; i++) lineCount += invoices[i].Lines.Count;
            long dtlKey = -(lineCount > 0 ? lineCount : 1);

            for (int i = 0; i < invoices.Count; i++)
            {
                SampleInvoiceDoc doc = invoices[i];
                // Sentinel keys, negative so they can never collide with a real document — but
                // ASCENDING in the order the invoices were given. A layout that sorts by DocKey
                // (the stock ones do) would otherwise print the batch backwards.
                long docKey = -(invoices.Count - i);

                decimal total = 0m;
                for (int j = 0; j < doc.Lines.Count; j++) total += doc.Lines[j].Amount;

                // What the caller already worked out wins; the book fills whatever it left blank.
                DebtorInfo who = LoadDebtor(db, doc.DebtorCode);
                if (who.CompanyName.Length == 0) who.CompanyName = (doc.DebtorName ?? "").Trim();
                string[] addr = SplitAddress(doc.DebtorAddress);
                if (addr != null)
                {
                    who.Address1 = addr[0]; who.Address2 = addr[1];
                    who.Address3 = addr[2]; who.Address4 = addr[3];
                }
                if ((doc.Attention ?? "").Trim().Length > 0) who.Attention = doc.Attention.Trim();
                if ((doc.Terms ?? "").Trim().Length > 0) who.DisplayTerm = doc.Terms.Trim();
                CurrencyInfo cur = LoadCurrency(db, who.CurrencyCode);

                string docNo = (doc.DocNo ?? "").Trim();
                if (docNo.Length == 0) docNo = invoices.Count > 1 ? "SAMPLE-" + (i + 1) : "SAMPLE";
                DateTime docDate = doc.DocDate == default(DateTime) ? today : doc.DocDate;

                DataRow m = master.NewRow();
                m["DocKey"] = docKey;
                Put(m, "DocNo", docNo);
                Put(m, "DocDate", docDate);
                Put(m, "TaxDate", docDate);
                Put(m, "SalesAgent", (doc.Agent ?? "").Trim());
                Put(m, "SalesAgentDescription", (doc.Agent ?? "").Trim());
                Put(m, "Description", Note(doc));
                Put(m, "Ref", (doc.ContractNo ?? "").Trim());
                Put(m, "DebtorCode", (doc.DebtorCode ?? "").Trim());
                Put(m, "DebtorName", who.CompanyName);
                Put(m, "DebtorCompanyName", who.CompanyName);
                Put(m, "Attention", who.Attention);
                Put(m, "Phone1", who.Phone1);
                Put(m, "Fax1", who.Fax1);
                Put(m, "EmailAddress", who.EmailAddress);
                Put(m, "DisplayTerm", who.DisplayTerm);
                Put(m, "DebtorDisplayTerm", who.DisplayTerm);
                Put(m, "InvAddr1", who.Address1); Put(m, "InvAddr2", who.Address2);
                Put(m, "InvAddr3", who.Address3); Put(m, "InvAddr4", who.Address4);
                Put(m, "DebtorAddress1", who.Address1); Put(m, "DebtorAddress2", who.Address2);
                Put(m, "DebtorAddress3", who.Address3); Put(m, "DebtorAddress4", who.Address4);
                Put(m, "DeliverAddr1", who.Address1); Put(m, "DeliverAddr2", who.Address2);
                Put(m, "DeliverAddr3", who.Address3); Put(m, "DeliverAddr4", who.Address4);
                Put(m, "DebtorDeliverAddress1", who.Address1); Put(m, "DebtorDeliverAddress2", who.Address2);
                Put(m, "DebtorDeliverAddress3", who.Address3); Put(m, "DebtorDeliverAddress4", who.Address4);
                Put(m, "DeliverContact", who.Attention);
                Put(m, "DeliverPhone1", who.Phone1);
                Put(m, "DebtorPostCode", who.PostCode);
                Put(m, "CurrencyCode", cur.Code);
                Put(m, "DebtorCurrencyCode", cur.Code);
                Put(m, "CurrencyWord", cur.Word);
                Put(m, "CurrencyWord2", cur.Word2);
                Put(m, "CurrencySymbol", cur.Symbol);
                Put(m, "CurrencyRate", 1m);
                Put(m, "ToTaxCurrencyRate", 1m);
                Put(m, "InclusiveTax", "F");
                Put(m, "IsRoundAdj", "F");
                Put(m, "Cancelled", "F");
                Put(m, "Transferable", "T");
                Put(m, "PostToGL", "T");
                Put(m, "PostToStock", "T");
                Put(m, "CanSync", "F");
                Put(m, "SubmitEInvoice", "F");
                if (taxEntityId >= 0) Put(m, "TaxEntityID", taxEntityId);

                // Every money field a stock layout might print, all saying the same thing: this
                // invoice is worth what its lines are worth, with no tax and no rounding invented.
                string[] totals = {
                    "Total", "TotalExTax", "TotalWithTax", "NetTotal", "LocalNetTotal", "ExTax",
                    "LocalExTax", "TaxableAmt", "LocalTaxableAmt", "TaxCurrencyTaxableAmt",
                    "TaxCurrencyExTax", "FinalTotal", "TotalExTaxAndWHT", "LocalTotalExTaxAndWHT",
                    "NetTotalAfterWHT", "LocalNetTotalAfterWHT", "Outstanding", "NetOutstanding" };
                for (int t = 0; t < totals.Length; t++) Put(m, totals[t], total);

                FillBlanks(m);
                master.Rows.Add(m);

                for (int j = 0; j < doc.Lines.Count; j++)
                {
                    SampleInvoiceLine line = doc.Lines[j];
                    decimal qty = line.Qty;
                    decimal price = line.UnitPrice;
                    if (qty == 0m) { qty = 1m; price = line.Amount; }
                    if (qtyDecimals >= 0) qty = AtScale(qty, qtyDecimals);

                    DataRow d = detail.NewRow();
                    d["DtlKey"] = dtlKey++;
                    d["DocKey"] = docKey;
                    Put(d, "Seq", j + 1);
                    Put(d, "MainItem", "T");
                    Put(d, "DtlType", "N");
                    Put(d, "PrintOut", "T");
                    Put(d, "Transferable", "T");
                    Put(d, "AddToSubTotal", "T");
                    Put(d, "AddToCost", "F");
                    Put(d, "Description", (line.Description ?? "").Trim());
                    Put(d, "ItemDescription", (line.Description ?? "").Trim());
                    Put(d, "FurtherDescription", Sub(line));
                    Put(d, "ItemFurtherDescription", Sub(line));
                    Put(d, "Qty", qty);
                    Put(d, "SmallestQty", qty);
                    Put(d, "UnitPrice", price);
                    Put(d, "SmallestUnitPrice", price);
                    Put(d, "UnitPriceAfterDiscount", price);
                    Put(d, "UOMRate", 1m);
                    Put(d, "Rate", 1m);
                    Put(d, "SubTotal", line.Amount);
                    Put(d, "LocalSubTotal", line.Amount);
                    Put(d, "SubTotalExTax", line.Amount);
                    Put(d, "LocalSubTotalExTax", line.Amount);
                    Put(d, "SubTotalWithTax", line.Amount);
                    Put(d, "TaxableAmt", line.Amount);
                    Put(d, "LocalTaxableAmt", line.Amount);
                    Put(d, "TaxCurrencyTaxableAmt", line.Amount);

                    // NULL, not 0: AddValueTransferTables selects the rows where this IS NOT NULL and
                    // then goes back to the book for each one. A zero here would send it looking for
                    // sales order 0 on every single line.
                    FillBlanks(d, "ValueXferSODocKey");
                    if (detail.Columns["ValueXferSODocKey"] != null) d["ValueXferSODocKey"] = DBNull.Value;
                    detail.Rows.Add(d);
                }
            }
            return ds;
        }

        /// <summary>The caller's already-formatted address block back into the four lines the book
        /// keeps it in. Null when nothing was supplied, which leaves the debtor master to answer.</summary>
        private static string[] SplitAddress(string block)
        {
            string text = (block ?? "").Trim();
            if (text.Length == 0) return null;
            string[] raw = text.Replace("\r\n", "\n").Split('\n');
            string[] four = new string[4] { "", "", "", "" };
            int n = 0;
            for (int i = 0; i < raw.Length && n < 4; i++)
            {
                string line = raw[i].Trim();
                if (line.Length == 0) continue;
                four[n++] = line;
            }
            // More lines than the book has slots: the tail joins the last one rather than vanishing.
            for (int i = 4; i < raw.Length; i++)
            {
                string line = raw[i].Trim();
                if (line.Length > 0) four[3] = (four[3] + " " + line).Trim();
            }
            return four;
        }

        /// <summary>What this invoice is — said on the document itself, so a sample can never be
        /// mistaken for a real one just because it wears the real layout.</summary>
        private static string Note(SampleInvoiceDoc doc)
        {
            string note = (doc.Note ?? "").Trim();
            return note.Length > 0 ? "SAMPLE — " + note : "SAMPLE — not a real invoice";
        }

        /// <summary>The model / serial / covers / strategy block, as the further description a real
        /// layout already knows how to print under a line.</summary>
        private static string Sub(SampleInvoiceLine line)
        {
            List<string> parts = new List<string>();
            if ((line.SubDescription ?? "").Trim().Length > 0) parts.Add(line.SubDescription.Trim());
            if ((line.Covers ?? "").Trim().Length > 0) parts.Add("covers:  " + line.Covers.Trim());
            if ((line.Note ?? "").Trim().Length > 0) parts.Add(line.Note.Trim());
            return string.Join(Environment.NewLine, parts.ToArray());
        }

        /// <summary>Sets a column if the book has one by that name, trimming to what it will hold.
        /// A book without the column simply prints nothing there — it is never a failure.</summary>
        private static void Put(DataRow row, string column, object value)
        {
            DataColumn c = row.Table.Columns[column];
            if (c == null || value == null) return;
            if (c.DataType == typeof(string))
            {
                string s = Convert.ToString(value);
                if (c.MaxLength > 0 && s.Length > c.MaxLength) s = s.Substring(0, c.MaxLength);
                row[c] = s;
                return;
            }
            try { row[c] = Convert.ChangeType(value, c.DataType); }
            catch { }
        }

        /// <summary>
        /// Every number still unset becomes 0 — a stock layout binds far more money columns than a
        /// sample knows about, and tax and total arithmetic runs over all of them.
        ///
        /// <para>TEXT is deliberately left NULL. An empty string is not the same as no value on a
        /// printed page: a real invoice row leaves Discount, TaxCode and SerialNoList null, and a
        /// layout that hides a caption when its field is null will happily print a stray
        /// "Serial No.:" against an empty string. Matching what the book stores is what makes the
        /// sample read like the article. Verified against a real invoice rendered through the same
        /// design.</para>
        /// </summary>
        private static void FillBlanks(DataRow row, params string[] keepNull)
        {
            foreach (DataColumn c in row.Table.Columns)
            {
                if (!row.IsNull(c)) continue;
                bool skip = false;
                for (int i = 0; i < keepNull.Length; i++)
                    if (string.Equals(keepNull[i], c.ColumnName, StringComparison.OrdinalIgnoreCase)) skip = true;
                if (skip) continue;

                Type t = c.DataType;
                if (t == typeof(decimal)) row[c] = 0m;
                else if (t == typeof(double)) row[c] = 0d;
                else if (t == typeof(float)) row[c] = 0f;
                else if (t == typeof(long)) row[c] = 0L;
                else if (t == typeof(int)) row[c] = 0;
                else if (t == typeof(short)) row[c] = (short)0;
                else if (t == typeof(byte)) row[c] = (byte)0;
                else if (t == typeof(bool)) row[c] = false;
                // Text, Guid, DateTime and byte[] stay null, exactly as the book stores them.
            }
        }

        /// <summary>
        /// How many decimals the book stores a quantity with — the scale of vInvoiceDetail.Qty
        /// itself, so nothing is assumed about this particular AutoCount build. -1 if it cannot be
        /// read, which leaves the sample's own quantities alone.
        /// </summary>
        private static int QtyScale(DBSetting db)
        {
            try
            {
                object o = db.ExecuteScalar(
                    "SELECT scale FROM sys.columns WHERE object_id = OBJECT_ID('dbo.vInvoiceDetail')" +
                    " AND name = 'Qty'");
                if (o == null || o == DBNull.Value) return -1;
                return Convert.ToInt32(o);
            }
            catch { return -1; }
        }

        /// <summary>
        /// A quantity carrying the book's own number of decimals.
        ///
        /// <para>A stock invoice layout prints Qty with no format string at all — the decimals you
        /// see are the ones the stored value carries, rounded down to four by the report engine. So a
        /// hand-built 1 prints "1" where the real article prints "1.0000", and the sample looks
        /// subtly unlike the invoice it is previewing. Padding the value to the column's own scale
        /// puts it on exactly the footing a stored quantity has.</para>
        /// </summary>
        private static decimal AtScale(decimal v, int decimals)
        {
            if (decimals < 0) decimals = 0;
            if (decimals > 28) decimals = 28;
            try
            {
                return decimal.Parse(v.ToString("F" + decimals, System.Globalization.CultureInfo.InvariantCulture),
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { return v; }
        }

        private static int FirstTaxEntityId(DataTable taxEntity)
        {
            if (taxEntity == null || taxEntity.Rows.Count == 0) return -1;
            if (taxEntity.Columns["TaxEntityID"] == null) return -1;
            try { return Convert.ToInt32(taxEntity.Rows[0]["TaxEntityID"]); }
            catch { return -1; }
        }

        // ---- the book's own masters, so the bill-to block is real ------------------------------

        private class DebtorInfo
        {
            public string CompanyName = "", Address1 = "", Address2 = "", Address3 = "", Address4 = "";
            public string PostCode = "", Attention = "", Phone1 = "", Fax1 = "", EmailAddress = "";
            public string DisplayTerm = "", CurrencyCode = "";
        }

        private class CurrencyInfo
        {
            public string Code = "", Word = "", Word2 = "", Symbol = "";
        }

        /// <summary>The customer as the book knows them. An unknown code just leaves the block to the
        /// name the sample carried.</summary>
        private static DebtorInfo LoadDebtor(DBSetting db, string accNo)
        {
            DebtorInfo d = new DebtorInfo();
            string code = (accNo ?? "").Trim();
            if (code.Length == 0) return d;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT TOP 1 CompanyName, Address1, Address2, Address3, Address4, PostCode," +
                    " Attention, Phone1, Fax1, EmailAddress, DisplayTerm, CurrencyCode" +
                    " FROM dbo.Debtor WHERE AccNo = @AccNo", false,
                    new System.Data.SqlClient.SqlParameter("@AccNo", code));
                if (t == null || t.Rows.Count == 0) return d;
                DataRow r = t.Rows[0];
                d.CompanyName = Str(r, "CompanyName");
                d.Address1 = Str(r, "Address1"); d.Address2 = Str(r, "Address2");
                d.Address3 = Str(r, "Address3"); d.Address4 = Str(r, "Address4");
                d.PostCode = Str(r, "PostCode");
                d.Attention = Str(r, "Attention");
                d.Phone1 = Str(r, "Phone1");
                d.Fax1 = Str(r, "Fax1");
                d.EmailAddress = Str(r, "EmailAddress");
                d.DisplayTerm = Str(r, "DisplayTerm");
                d.CurrencyCode = Str(r, "CurrencyCode");
            }
            catch { }
            return d;
        }

        /// <summary>The customer's currency, else the book's first — the words a layout spells the
        /// total out in come from here.</summary>
        private static CurrencyInfo LoadCurrency(DBSetting db, string code)
        {
            CurrencyInfo m = new CurrencyInfo();
            try
            {
                DataTable t = null;
                string want = (code ?? "").Trim();
                if (want.Length > 0)
                    t = db.GetDataTable("SELECT TOP 1 CurrencyCode, CurrencyWord, CurrencyWord2," +
                        " CurrencySymbol FROM dbo.Currency WHERE CurrencyCode = @C", false,
                        new System.Data.SqlClient.SqlParameter("@C", want));
                if (t == null || t.Rows.Count == 0)
                    t = db.GetDataTable("SELECT TOP 1 CurrencyCode, CurrencyWord, CurrencyWord2," +
                        " CurrencySymbol FROM dbo.Currency ORDER BY CurrencyCode", false);
                if (t == null || t.Rows.Count == 0) return m;
                DataRow r = t.Rows[0];
                m.Code = Str(r, "CurrencyCode");
                m.Word = Str(r, "CurrencyWord");
                m.Word2 = Str(r, "CurrencyWord2");
                m.Symbol = Str(r, "CurrencySymbol");
            }
            catch { }
            return m;
        }

        private static string Str(DataRow r, string column)
        {
            if (r.Table.Columns[column] == null || r.IsNull(column)) return "";
            return Convert.ToString(r[column]).Trim();
        }
    }
}
