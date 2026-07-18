using System;
using System.Collections.Generic;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One billable meter line for the Meter Reading billing run (one machine + one colour role).
    /// </summary>
    public class MeterBillLine
    {
        public long ItemKey;
        public long ContractKey;
        public long ItemMeterKey;
        public string ContractNo = "";
        public string ItemNo = "";
        public string DebtorCode = "";
        public string SerialNumber = "";
        public string MeterTypeCode = "";
        public string MeterTypeName = "";
        public string ACItemCode = "";
        public string ItemName = "";      // service item no (label)
        public string ItemDesc = "";      // service item description (model/location text on the line)
        public string ColorLabel = "";   // "Black" or "Colour"
        public decimal Last;
        public decimal Current;
        public decimal Usage;
        public decimal Rate;
        public decimal MinCharges;
        public decimal Foc;
        public decimal RebatePct;
        public decimal Charge;
        public bool UseMin;
        public DateTime? LastDate;
        /// <summary>API LastAuditDate of the CURRENT reading — used as the MeterTrans date so the
        /// next period's "Last Read Date" is the real meter-read date (falls back to now if absent).</summary>
        public DateTime? AuditDate;
    }

    /// <summary>
    /// Shared meter-billing math + AutoCount invoice construction. Mirrors the proven
    /// MeterTypeTransactionEntry_Form pipeline (RecalcRow + BuildInvoiceDocument) so the new
    /// Meter Reading Integration flow produces identical invoices.
    /// </summary>
    public static class ScpInvoiceBuilder
    {
        /// <summary>Computes Usage, UseMin and Charge in-place from the line's readings + config.
        /// MASTER CONVENTION (verified against the customer's V8 meter invoices, e.g. MR2604.0006:
        /// usage 5645 with FOC 1000 was billed 5645 x 0.03 in full): the charge is RAW USAGE x RATE
        /// with the minimum-charge floor — FOC Qty and Rebate % are informational columns only and
        /// are never deducted from the billed amount.</summary>
        public static void ComputeCharge(MeterBillLine ln)
        {
            decimal usage = ln.Current - ln.Last;
            if (usage < 0m) usage = 0m;
            ln.Usage = usage;

            ln.UseMin = (ln.Rate == 0m && ln.MinCharges > 0m);
            if (ln.UseMin)
            {
                ln.Charge = ln.MinCharges;
            }
            else
            {
                decimal c = usage * ln.Rate;
                if (c < ln.MinCharges) c = ln.MinCharges;
                ln.Charge = c;
            }
        }

        /// <summary>
        /// Builds (but does NOT save) an Invoice document from the given billable lines.
        /// LEGACY FORMAT — mirrors how the customer's V8 book created meter invoices (verified against
        /// v8_atp_main salesinvoice/salesinvoiceitem, e.g. MR2603.0691):
        ///   • header Description = "Billing- [ref]"; Remark1 = the CSSI/contract ref.
        ///   • ONE detail line per meter: Description = "{item desc} S/N:{serial}- {meter name}",
        ///     Qty = billable copies, UnitPrice = rate; the reading breakdown lives in
        ///     FurtherDescription as the legacy 16-line structured block (legend + 15 values).
        ///   • Minimum-charge lines: Qty 1 × the minimum, FurtherDescription "*** Minimum Charges ***".
        /// </summary>
        public static AutoCount.Invoicing.Sales.Invoice.Invoice BuildInvoice(
            DBSetting db, string debtorCode, string refDocNo, string description,
            DateTime docDate, DateTime readingDate, List<MeterBillLine> lines)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(UserSession.CurrentUserSession, db);

            AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();
            if (doc == null)
                throw new InvalidOperationException("Failed to create a new invoice document.");

            // Meter invoices draw their number from the book's own Document Numbering Format (native
            // AutoCount numbering, per-month running set -> MR2607.0001 style). The format NAME is
            // configurable in Service Option; it is only applied when it actually exists, so a book
            // without it falls back to the IV default numbering.
            try
            {
                string fmtName = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_INVOICE_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_INVOICE_DOCNO_FORMAT).Trim();
                if (fmtName.Length > 0)
                {
                    System.Data.DataTable fmt = db.GetDataTable(
                        "SELECT TOP 1 Name FROM dbo.DocNoFormat WHERE DocType='IV' AND Name='" + fmtName.Replace("'", "''") + "'", false);
                    if (fmt.Rows.Count > 0) doc.DocNoFormatName = fmtName;
                }
            }
            catch { }

            doc.DebtorCode = debtorCode;
            doc.DocDate = docDate;
            doc.Description = description;
            doc.RefDocNo = refDocNo ?? "";
            doc.Remark1 = refDocNo ?? "";
            if (doc.DetailCount > 0) doc.ClearDetails();

            foreach (MeterBillLine ln in lines)
            {
                bool minBilled = ln.UseMin || ln.Rate == 0m || ln.Usage * ln.Rate < ln.MinCharges;
                string block = minBilled ? "*** Minimum Charges ***" : ComposeBreakdown(ln, readingDate);

                // Charge row — Qty = RAW usage x Rate (master convention: FOC/rebate shown but never
                // deducted); minimum-floor lines bill 1 x the minimum, tagged "*** Minimum Charges ***".
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(ln.ACItemCode)) dtl.ItemCode = ln.ACItemCode;
                dtl.Description = ComposeLineDescription(ln);
                if (minBilled)
                { dtl.Qty = 1m; dtl.UnitPrice = ln.Charge; }
                else
                { dtl.Qty = ln.Usage > 0m ? ln.Usage : 1m; dtl.UnitPrice = ln.Usage > 0m ? ln.Rate : ln.Charge; }
                dtl.FurtherDescription = block;

                // Reading text rows — exactly the master's content: Current / Previous / (FOC when >0)
                // / Usage, each carrying the same block, then a blank separator row. Dates dd/MM/yyyy,
                // readings plain digits (no thousands separators — the master prints "126699").
                DateTime curDate = ln.AuditDate ?? readingDate;
                string curDateStr = curDate.ToString("dd/MM/yyyy");
                string lastDateStr = ln.LastDate.HasValue ? ln.LastDate.Value.ToString("dd/MM/yyyy") : "";

                // Text rows carry NO account (master: acccode empty on description-only rows) — AddDetail
                // pre-fills the default sales account, so it is explicitly cleared here.
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dCur = doc.AddDetail();
                dCur.Description = "Current Meter Reading (" + curDateStr + ") : " + Num(ln.Current);
                dCur.FurtherDescription = block;
                dCur.AccNo = null;

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dPrev = doc.AddDetail();
                dPrev.Description = "Previous Meter Reading (" + lastDateStr + ") : " + Num(ln.Last);
                dPrev.FurtherDescription = block;
                dPrev.AccNo = null;

                if (ln.Foc > 0m)
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dFoc = doc.AddDetail();
                    dFoc.Description = "Meter FOC Qty : " + Num(ln.Foc);
                    dFoc.FurtherDescription = block;
                    dFoc.AccNo = null;
                }

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dUse = doc.AddDetail();
                dUse.Description = "Meter Charges Usage : " + Num(ln.Usage);
                dUse.FurtherDescription = block;
                dUse.AccNo = null;

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dBlank = doc.AddDetail();
                dBlank.Description = "";
                dBlank.AccNo = null;
            }
            return doc;
        }

        // Master convention (verified against v8_atp_main salesinvoiceitem): the line description is
        // the METER TYPE's description ONLY — e.g. "BK COPY + PRINT A4&A3". Machine-specific texts like
        // "PUSPEN ... S/N:4NL20240- BK COPY+PRINT A4&A3" exist in the master only because the customer
        // created per-machine meter types carrying that full text as the meter description; the item's
        // own description is never prefixed by the billing code.
        private static string ComposeLineDescription(MeterBillLine ln)
        {
            if (!string.IsNullOrEmpty(ln.MeterTypeName)) return ln.MeterTypeName.Trim();
            if (!string.IsNullOrEmpty(ln.MeterTypeCode)) return ln.MeterTypeCode.Trim();
            return ln.ItemName;
        }

        // The legacy 16-line More Description block (legend + 15 values), copied verbatim from the
        // customer's V8 meter invoices.
        private static string ComposeBreakdown(MeterBillLine ln, DateTime readingDate)
        {
            DateTime cur = ln.AuditDate ?? readingDate;
            System.Globalization.CultureInfo en = new System.Globalization.CultureInfo("en-US");
            string lastLong = ln.LastDate.HasValue ? LongDate(ln.LastDate.Value, en) : "";
            string lastShort = ln.LastDate.HasValue ? ln.LastDate.Value.ToString("yyyyMMdd") : "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("1. Mt Type; 2. Mt Name; 3. Min Chrg; 4. Chrg Rate; 5. FOC Qty; 6. Rebate %; " +
                          "7. Last Reading Date; 8. Last Reading Mt; 9. Mt Usage; 10. Total Chrg; 11. Multi Price; " +
                          "12. Rebate Qty; 13. Current Reading Date; 14. Last Reading Short Date; 15. Current Reading Short Date");
            sb.AppendLine(ln.MeterTypeCode);
            sb.AppendLine(ComposeLineDescription(ln));
            sb.AppendLine(Num(ln.MinCharges));
            sb.AppendLine(Num(ln.Rate));
            sb.AppendLine(Num(ln.Foc));
            sb.AppendLine(Num(ln.RebatePct));
            sb.AppendLine(lastLong);
            sb.AppendLine(Num(ln.Last));
            sb.AppendLine(Num(ln.Usage));
            sb.AppendLine(Num(ln.Charge));
            sb.AppendLine("");                       // multi price
            sb.AppendLine("0");                      // rebate qty
            sb.AppendLine(LongDate(cur, en));
            sb.AppendLine(lastShort);
            sb.Append(cur.ToString("yyyyMMdd"));
            return sb.ToString();
        }

        private static string Num(decimal v) { return v.ToString("0.######"); }

        // Legacy long stamp: "24 Feb 2026 5:10:06 pm" (lowercase am/pm).
        private static string LongDate(DateTime d, System.Globalization.CultureInfo en)
        {
            return d.ToString("d MMM yyyy h:mm:ss", en) + " " + d.ToString("tt", en).ToLowerInvariant();
        }
    }
}
