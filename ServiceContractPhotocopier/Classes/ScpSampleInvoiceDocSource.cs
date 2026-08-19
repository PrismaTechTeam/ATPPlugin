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
    /// billed, for what, how much — are written by hand; everything else is defaulted to the empty
    /// value of its own type so a layout that binds it prints a blank rather than throwing.</para>
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
            DateTime today = DateTime.Today;
            long dtlKey = -1;

            for (int i = 0; i < invoices.Count; i++)
            {
                SampleInvoiceDoc doc = invoices[i];
                long docKey = -(i + 1);

                decimal total = 0m;
                for (int j = 0; j < doc.Lines.Count; j++) total += doc.Lines[j].Amount;

                DebtorInfo who = LoadDebtor(db, doc.DebtorCode);
                if (who.CompanyName.Length == 0)
                    who.CompanyName = (doc.DebtorName ?? "").Trim();
                CurrencyInfo cur = LoadCurrency(db, who.CurrencyCode);

                DataRow m = master.NewRow();
                m["DocKey"] = docKey;
                Put(m, "DocNo", invoices.Count > 1 ? "SAMPLE-" + (i + 1) : "SAMPLE");
                Put(m, "DocDate", today);
                Put(m, "TaxDate", today);
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

                    DataRow d = detail.NewRow();
                    d["DtlKey"] = dtlKey--;
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
        /// Everything still unset becomes the empty value of its own type: "" for text, 0 for money.
        /// A stock layout binds far more columns than a sample knows about, and a bound NULL is the
        /// difference between a blank on the page and a rendering that stops.
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
                if (t == typeof(string)) row[c] = "";
                else if (t == typeof(decimal)) row[c] = 0m;
                else if (t == typeof(double)) row[c] = 0d;
                else if (t == typeof(float)) row[c] = 0f;
                else if (t == typeof(long)) row[c] = 0L;
                else if (t == typeof(int)) row[c] = 0;
                else if (t == typeof(short)) row[c] = (short)0;
                else if (t == typeof(byte)) row[c] = (byte)0;
                else if (t == typeof(bool)) row[c] = false;
                // Guid, DateTime and byte[] stay null: an invented one would read as a real value.
            }
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
