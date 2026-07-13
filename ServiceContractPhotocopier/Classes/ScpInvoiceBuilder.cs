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
        public string ItemName = "";      // service item description (for the invoice line)
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
        /// <summary>Computes Usage, UseMin and Charge in-place from the line's readings + config.</summary>
        public static void ComputeCharge(MeterBillLine ln)
        {
            decimal usage = ln.Current - ln.Last;
            if (usage < 0m) usage = 0m;
            ln.Usage = usage;

            decimal billable = usage - ln.Foc;
            if (billable < 0m) billable = 0m;
            if (ln.RebatePct > 0m) billable = billable * (1m - ln.RebatePct / 100m);

            ln.UseMin = (ln.Rate == 0m && ln.MinCharges > 0m);
            if (ln.UseMin)
            {
                ln.Charge = ln.MinCharges;
            }
            else
            {
                decimal c = billable * ln.Rate;
                if (c < ln.MinCharges) c = ln.MinCharges;
                ln.Charge = c;
            }
        }

        /// <summary>
        /// Builds (but does NOT save) an Invoice document from the given billable lines. Caller opens
        /// FormInvoiceEntry for review/save, then writes zSCP_MeterTrans only after a confirmed save.
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

            doc.DebtorCode = debtorCode;
            doc.DocDate = docDate;
            doc.Description = description;
            doc.RefDocNo = refDocNo ?? "";
            if (doc.DetailCount > 0) doc.ClearDetails();

            string readingDateStr = readingDate.ToString("dd/MM/yyyy");

            foreach (MeterBillLine ln in lines)
            {
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(ln.ACItemCode)) dtl.ItemCode = ln.ACItemCode;
                dtl.Description = (string.IsNullOrEmpty(ln.ItemName) ? ln.SerialNumber : ln.ItemName)
                                  + " - " + ln.ColorLabel + " Meter Charge";
                if (ln.UseMin || ln.Rate == 0m)
                { dtl.Qty = 1m; dtl.UnitPrice = ln.Charge; }
                else
                { dtl.Qty = ln.Usage > 0m ? ln.Usage : 1m; dtl.UnitPrice = ln.Rate; }

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dSerial = doc.AddDetail();
                dSerial.Description = "Serial : " + ln.SerialNumber + "   (" + ln.MeterTypeCode + ")";

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dCur = doc.AddDetail();
                string curDateStr = ln.AuditDate.HasValue ? ln.AuditDate.Value.ToString("dd/MM/yyyy") : readingDateStr;
                dCur.Description = "Current Meter Reading (" + curDateStr + ") : " + ln.Current.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dPrev = doc.AddDetail();
                string lastDateStr = ln.LastDate.HasValue ? ln.LastDate.Value.ToString("dd/MM/yyyy") : "N/A";
                dPrev.Description = "Previous Meter Reading (" + lastDateStr + ") : " + ln.Last.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dUsage = doc.AddDetail();
                dUsage.Description = ln.ColorLabel + " Meter Usage : " + ln.Usage.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dBlank = doc.AddDetail();
                dBlank.Description = "";
            }
            return doc;
        }
    }
}
