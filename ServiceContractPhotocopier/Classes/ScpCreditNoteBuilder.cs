using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One meter line of a CN reading correction (demo 28/07 #10).</summary>
    public class CnCorrectionLine
    {
        public long ItemMeterKey;
        public long ItemKey;
        public string MeterTypeCode = "";
        public string MeterTypeName = "";
        public string ACItemCode = "";
        public string ServiceItemNo = "";
        public string Serial = "";
        public string DeptNo = "";
        public string ProjNo = "";
        public decimal BilledReading;
        public decimal BilledUsage;
        public decimal CorrectReading;
        public decimal CreditCopies;      // BilledReading - CorrectReading (>= 0)
        public decimal Rate;              // billed EFFECTIVE unit price (INVOICE log), master fallback
        public decimal CreditAmount;      // CreditCopies x Rate, user-editable override
        public bool AmountOverridden;
        public DateTime BilledTransDate;  // the original billed zSCP_MeterTrans.MeterTransDate
        public int PeriodYear;            // original zSCP2_MeterEntry stamp period (0 = unknown)
        public int PeriodMonth;
    }

    /// <summary>
    /// Demo 28/07 #10: corrects a wrongly-billed meter invoice with a REAL AutoCount Sales Credit
    /// Note, and overrides the machine's LAST METER READING so later months bill from the corrected
    /// value. The override is a zSCP_MeterTrans row dated the ORIGINAL billed row + 1 second — it
    /// wins the billing baseline for the NEXT unbilled month by (date, key) order, and multiple CNs
    /// on one invoice resolve by insert order = CN number order (the user's "latest CN wins" rule).
    /// Deleting/cancelling the CN in AutoCount rolls the override back (ReconcileDeletedCreditNotes).
    /// </summary>
    public static class ScpCreditNoteBuilder
    {
        /// <summary>Creates the Sales CN + the reading-override rows + the audit log in one flow.
        /// Returns the CN DocNo. Throws on failure (the CN itself is compensated/deleted when the
        /// override stamping fails). knockOffWarning is non-empty when the optional AR knock-off
        /// failed — the CN still stands, just un-knocked.</summary>
        public static string CreateCorrectionCN(DBSetting db, string debtorCode, long invoiceDocKey,
            string invoiceDocNo, DateTime cnDate, string reason, bool knockOff,
            List<CnCorrectionLine> lines, out long cnDocKey, out string knockOffWarning)
        {
            knockOffWarning = "";
            List<CnCorrectionLine> billable = new List<CnCorrectionLine>();
            foreach (CnCorrectionLine l in lines)
                if (l.CreditCopies > 0m && l.CreditAmount >= 0m) billable.Add(l);
            if (billable.Count == 0) throw new InvalidOperationException("Nothing to credit.");

            AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand cmd =
                AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                    AutoCount.Authentication.UserSession.CurrentUserSession, db);
            AutoCount.Invoicing.Sales.CreditNote.CreditNote doc = cmd.AddNew();
            if (doc == null) throw new InvalidOperationException("Failed to create a new credit note document.");

            // Optional numbering format — guarded like the invoice path (DocType 'CN' here).
            try
            {
                string fmtName = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_CN_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_CN_DOCNO_FORMAT).Trim();
                if (fmtName.Length > 0)
                {
                    System.Data.DataTable fmt = db.GetDataTable(
                        "SELECT TOP 1 Name FROM dbo.DocNoFormat WHERE DocType='CN' AND Name='" +
                        fmtName.Replace("'", "''") + "'", false);
                    if (fmt.Rows.Count > 0) doc.DocNoFormatName = fmtName;
                }
            }
            catch { }

            doc.DebtorCode = debtorCode;
            doc.DocDate = cnDate;
            doc.Description = Trunc("Meter correction - Invoice " + invoiceDocNo, 80);   // CN.Description nvarchar(80)
            doc.RefDocNo = invoiceDocNo;
            doc.OurInvoiceNo = Trunc(invoiceDocNo, 100);
            doc.Reason = Trunc(reason ?? "", 80);                                        // CN.Reason nvarchar(80)
            if (doc.DetailCount > 0) doc.ClearDetails();

            decimal totalCredit = 0m;
            foreach (CnCorrectionLine l in billable)
            {
                AutoCount.Invoicing.Sales.CreditNote.CreditNoteDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(l.ACItemCode)) dtl.ItemCode = l.ACItemCode;
                // CNDTL.Description is nvarchar(100): compact wording, meter name truncated to fit;
                // the invoice no lives on the header (RefDocNo/OurInvoiceNo) and the full arithmetic
                // in FurtherDescription, so nothing is lost.
                string corrSuffix = " - CORRECTION " + Num(l.BilledReading) + " -> " + Num(l.CorrectReading);
                string meterName = string.IsNullOrEmpty(l.MeterTypeName) ? l.MeterTypeCode : l.MeterTypeName;
                dtl.Description = Trunc(meterName, 100 - corrSuffix.Length) + corrSuffix;
                dtl.Qty = l.CreditCopies;
                dtl.UnitPrice = l.AmountOverridden && l.CreditCopies != 0m
                    ? Math.Round(l.CreditAmount / l.CreditCopies, 6) : l.Rate;
                dtl.FurtherDescription =
                    "Machine " + l.ServiceItemNo + (l.Serial.Length > 0 ? " (S/N " + l.Serial + ")" : "") + "\r\n" +
                    "Billed reading " + Num(l.BilledReading) + " @ " + Num(l.Rate) +
                    "; corrected to " + Num(l.CorrectReading) +
                    "; credit " + Num(l.CreditCopies) + " copies = " + l.CreditAmount.ToString("0.00");
                // CRITICAL: default GoodsReturn='T' posts a stock-IN for stock item codes — this is
                // a billing-only correction on a service meter, never a goods return.
                dtl.GoodsReturn = false;
                if (!string.IsNullOrEmpty(l.DeptNo)) dtl.DeptNo = l.DeptNo;
                if (!string.IsNullOrEmpty(l.ProjNo)) dtl.ProjNo = l.ProjNo;
                totalCredit += l.CreditAmount;
            }

            doc.Save();
            string cnDocNo = doc.DocNo;
            cnDocKey = Convert.ToInt64(doc.DocKey);

            // Reading-override rows + audit log — one transaction; failure deletes the CN again
            // (same compensation pattern as MeterInvoiceGenerator's invoice stamps).
            try
            {
                using (SqlConnection cn = new SqlConnection(db.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("MeterCN"))
                    {
                        try
                        {
                            foreach (CnCorrectionLine l in billable)
                            {
                                SqlCommand ins = new SqlCommand(
                                    "INSERT INTO dbo.zSCP_MeterTrans (ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, " +
                                    "MeterTransDate, MeterTransReading, SalesInvoiceDocKey, CNDocKey, CNDocNo, CorrectedInvoiceDocKey, Remark) " +
                                    "VALUES (@imk,@ik,@code,@dt,@rd,NULL,@cnk,@cnno,@ivk,@rmk)", cn, tx);
                                ins.Parameters.AddWithValue("@imk", l.ItemMeterKey);
                                ins.Parameters.AddWithValue("@ik", l.ItemKey);
                                ins.Parameters.AddWithValue("@code", l.MeterTypeCode ?? "");
                                // SAME date as the billed row: the billed month keeps showing invoiced
                                // (NULL SalesInvoiceDocKey excludes this row there), and the NEXT month's
                                // baseline resolves the tie by MeterTransKey DESC = insert order — which
                                // is also how multiple CNs pick "the latest CN wins". A +1s offset would
                                // break on a billed row stamped 23:59:59 of the month's last day.
                                ins.Parameters.AddWithValue("@dt", l.BilledTransDate);
                                ins.Parameters.AddWithValue("@rd", l.CorrectReading);
                                ins.Parameters.AddWithValue("@cnk", cnDocKey);
                                ins.Parameters.AddWithValue("@cnno", cnDocNo ?? "");
                                ins.Parameters.AddWithValue("@ivk", invoiceDocKey);
                                ins.Parameters.AddWithValue("@rmk", "CN " + cnDocNo + ": " + Num(l.BilledReading) +
                                    " -> " + Num(l.CorrectReading) + " (Invoice " + invoiceDocNo + ")");
                                ins.ExecuteNonQuery();

                                ScpMeterReadingLog.Append(cn, tx, l.ItemMeterKey, l.PeriodYear, l.PeriodMonth,
                                    l.CorrectReading, cnDate, ScpMeterReadingLog.SOURCE_CN, cnDocNo ?? "",
                                    l.BilledReading, -l.CreditCopies,
                                    l.Rate, null, null, null, -l.CreditAmount,
                                    null, "Correction of Invoice " + invoiceDocNo);
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception stampEx)
            {
                // Flag-based compensation: never let the DELETE's own exception masquerade as the
                // stamp failure (the operator must always learn whether the CN still exists).
                bool rolledBack = false;
                Exception delEx = null;
                try
                {
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                        AutoCount.Authentication.UserSession.CurrentUserSession, db).Delete(cnDocKey);
                    rolledBack = true;
                }
                catch (Exception ex2) { delEx = ex2; }
                throw new InvalidOperationException(rolledBack
                    ? "Reading override failed - the credit note was rolled back. " + stampEx.Message
                    : "Reading override failed AND the credit note " + cnDocNo +
                      " could not be deleted - delete it manually in AutoCount. " +
                      stampEx.Message + " / " + (delEx != null ? delEx.Message : ""), stampEx);
            }

            // Optional AR knock-off against the original invoice — second transaction, best-effort.
            if (knockOff)
            {
                try
                {
                    // Cap at the AR invoice's outstanding (AutoCount's own auto-knock-off does the
                    // same) so a partly-paid invoice is never over-knocked.
                    decimal amount = totalCredit;
                    try
                    {
                        System.Data.DataTable ost = db.GetDataTable(
                            "SELECT ISNULL(B.Outstanding,0) AS Outstanding FROM dbo.IV A " +
                            "LEFT JOIN dbo.ARInvoice B ON A.ReferDocKey = B.DocKey WHERE A.DocKey = " + invoiceDocKey, false);
                        if (ost.Rows.Count > 0)
                        {
                            decimal outstanding = Convert.ToDecimal(ost.Rows[0]["Outstanding"]);
                            if (outstanding < amount) amount = outstanding;
                        }
                    }
                    catch { }
                    if (amount <= 0m)
                    {
                        knockOffWarning = "Credit note created, but invoice " + invoiceDocNo +
                            " has no outstanding balance to knock off - knock it off manually if needed.";
                    }
                    else
                    {
                        long arcnKey = doc.ReferDocKey;
                        AutoCount.ARAP.ARCN.ARCNDataAccess da = AutoCount.ARAP.ARCN.ARCNDataAccess.Create(
                            AutoCount.Authentication.UserSession.CurrentUserSession, db);
                        AutoCount.ARAP.ARCN.ARCNEntity arcn = da.GetARCN(arcnKey);
                        // KnockOff returns FALSE (silent no-op) when the invoice has no AR record.
                        if (!arcn.KnockOff("RI", invoiceDocNo, amount))
                            knockOffWarning = "Credit note created, but invoice " + invoiceDocNo +
                                " has no AR record to knock off against - knock it off manually in AR Credit Note Entry.";
                        else
                            da.SaveARCN(arcn, AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID);
                    }
                }
                catch (Exception koEx)
                {
                    knockOffWarning = "Credit note created, but the knock-off against " + invoiceDocNo +
                        " failed: " + koEx.Message + "\r\nKnock it off manually in AR Credit Note Entry.";
                }
            }
            return cnDocNo;
        }

        /// <summary>Rolls back reading overrides whose CN was deleted or cancelled in AutoCount:
        /// audits them as CN-DELETED (using the denormalized CNDocNo — dbo.CN may be gone), then
        /// removes the override rows so the baseline reverts. Best-effort, never throws.</summary>
        public static void ReconcileDeletedCreditNotes(DBSetting db)
        {
            try
            {
                string who = "";
                try { who = (AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? "").Replace("'", "''"); }
                catch { }
                db.ExecuteNonQuery(
                    "INSERT INTO dbo.zSCP2_MeterReadingLog (ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, " +
                    "Source, DocNo, CreatedBy, LastReading, [Usage], UnitPrice, Charge) " +
                    "SELECT t.ServiceItemMeterTypeKey, ISNULL(orig.PeriodYear,0), ISNULL(orig.PeriodMonth,0), " +
                    "t.MeterTransReading, t.MeterTransDate, '" + ScpMeterReadingLog.SOURCE_CN_DELETED + "', " +
                    "ISNULL(t.CNDocNo,''), N'" + who + "', orig.LastReading, orig.[Usage], orig.UnitPrice, orig.Charge " +
                    "FROM dbo.zSCP_MeterTrans t " +
                    "OUTER APPLY (SELECT TOP 1 l.PeriodYear, l.PeriodMonth, l.LastReading, l.[Usage], l.UnitPrice, l.Charge " +
                    "  FROM dbo.zSCP2_MeterReadingLog l " +
                    "  WHERE l.ItemMeterKey = t.ServiceItemMeterTypeKey AND l.Source = '" + ScpMeterReadingLog.SOURCE_CN + "' " +
                    "    AND l.DocNo = ISNULL(t.CNDocNo,'') ORDER BY l.LogKey DESC) orig " +
                    "WHERE t.CNDocKey IS NOT NULL " +
                    "AND (NOT EXISTS (SELECT 1 FROM dbo.CN c WHERE c.DocKey = t.CNDocKey AND ISNULL(c.Cancelled,'F') <> 'T') " +
                    "  OR (t.CorrectedInvoiceDocKey IS NOT NULL AND NOT EXISTS " +
                    "      (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.CorrectedInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')))");
                // Roll back when EITHER side dies: the CN itself, OR the corrected invoice (a stale
                // override must never shadow a re-generated bill for the same month).
                db.ExecuteNonQuery(
                    "DELETE t FROM dbo.zSCP_MeterTrans t " +
                    "WHERE t.CNDocKey IS NOT NULL " +
                    "AND (NOT EXISTS (SELECT 1 FROM dbo.CN c WHERE c.DocKey = t.CNDocKey AND ISNULL(c.Cancelled,'F') <> 'T') " +
                    "  OR (t.CorrectedInvoiceDocKey IS NOT NULL AND NOT EXISTS " +
                    "      (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.CorrectedInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')))");
            }
            catch { }   // reconcile is best-effort; never block a screen load
        }

        private static string Num(decimal v) { return v.ToString("0.######"); }

        private static string Trunc(string s, int max)
        {
            if (s == null) return "";
            if (max < 0) max = 0;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
