using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Creates AND SAVES AutoCount Sales Invoices from selected meter-billing lines, headlessly
    /// (no per-invoice dialog — that was the old flow's pain point). Per-invoice failures are captured
    /// in <see cref="ErrorLog"/> and do not stop the run. zSCP_MeterTrans is written only after each
    /// invoice is saved. Mirrors SiStGenerator so the Meter Reading progress dialog can reuse the shape.
    /// </summary>
    public class MeterInvoiceGenerator
    {
        /// <summary>One invoice to generate = one contract (Group) or one service item (Separate).</summary>
        public class InvoiceJob
        {
            public string DebtorCode = "";
            public string RefDocNo = "";
            public string Description = "";
            public string Label = "";                 // shown in the progress dialog
            public List<MeterBillLine> Lines;
            // The BILLED PERIOD's billing date (genYear/genMonth + the machine's billing day).
            // Backdated runs (billing May in July) must NOT stamp today: the invoice lands in the
            // right month series (MR2605.*) and next month's baseline query can find this billing.
            public DateTime? DocDate;
        }

        public class Progress { public int Total; public int Done; public int Failed; public string CurrentLabel; }
        public delegate void ProgressHandler(Progress p);

        public List<string> ErrorLog { get; } = new List<string>();
        public List<string> CreatedDocNos { get; } = new List<string>();
        public int Total { get; private set; }
        public int Done { get; private set; }
        public int Failed { get; private set; }
        /// <summary>Meters processed WITHOUT an invoice line (charge = 0): last reading advanced and
        /// the period marked NO CHARGE, but nothing billed.</summary>
        public int NoChargeMeters { get; private set; }

        private readonly DBSetting _db;
        private readonly DateTime _docDate;
        private readonly DateTime _readingDate;
        private readonly int _periodYear;
        private readonly int _periodMonth;

        // Per-contract snapshot text (strategy rules + billing flags AS OF this run) — written into
        // zSCP2_ContractSnapshot alongside every generated invoice, inside the stamp transaction.
        private readonly Dictionary<long, string> _contractSnapshots;

        public MeterInvoiceGenerator(DBSetting db, DateTime docDate, DateTime readingDate, int periodYear, int periodMonth)
            : this(db, docDate, readingDate, periodYear, periodMonth, null) { }

        public MeterInvoiceGenerator(DBSetting db, DateTime docDate, DateTime readingDate, int periodYear, int periodMonth,
            Dictionary<long, string> contractSnapshots)
        {
            _db = db;
            _docDate = docDate;
            _readingDate = readingDate;
            _periodYear = periodYear;
            _periodMonth = periodMonth;
            _contractSnapshots = contractSnapshots;
        }

        public void Run(IList<InvoiceJob> jobs, ProgressHandler onProgress, Func<bool> isCancelled)
        {
            Total = jobs != null ? jobs.Count : 0;
            Done = 0;
            Failed = 0;

            for (int i = 0; i < Total; i++)
            {
                if (isCancelled != null && isCancelled()) break;
                InvoiceJob j = jobs[i];
                if (onProgress != null)
                    onProgress(new Progress { Total = Total, Done = Done, Failed = Failed, CurrentLabel = j.Label });

                try
                {
                    // Zero-charge meters (amount 0 after the minimum floor) never appear on the
                    // invoice — their reading still rolls forward and the period is marked NO CHARGE.
                    // A job where EVERY meter is zero produces NO invoice at all.
                    //
                    // ...unless the contract is on a Billing Format, because the customer's own book
                    // does the opposite. Tangkak's MR2607.1413, 1414 and 1415 are RM0.00 invoices,
                    // e-Invoiced and sent: usage fell under the 5,000 FOC allowance, and the hospital
                    // still wants the reading and the allowance on record. Pasir Gudang's rental
                    // invoice MR2607.1107 is three FOC lines totalling 0.00 for the same reason.
                    List<MeterBillLine> billable = new List<MeterBillLine>();
                    List<MeterBillLine> zero = new List<MeterBillLine>();
                    foreach (MeterBillLine ln in j.Lines)
                        // Lines that stay on the invoice even at RM 0 (user transparency rule — every
                        // meter SHOWS): committed-minimum top-ups, and any rental a strategy freed or
                        // waived (its StrategyNote explains the 0.00). Everything else zero-charge goes
                        // the NO-CHARGE stamp route.
                        ((ln.Charge > 0m || ln.AlwaysBill || ln.NewMoneyRules
                          || (ln.IsRental && !string.IsNullOrEmpty(ln.StrategyNote)))
                            ? billable : zero).Add(ln);

                    if (billable.Count > 0)
                    {
                        DateTime jobDate = j.DocDate ?? _docDate;
                        AutoCount.Invoicing.Sales.Invoice.Invoice doc = ScpInvoiceBuilder.BuildInvoice(
                            _db, j.DebtorCode, j.RefDocNo, j.Description, jobDate, j.DocDate ?? _readingDate, billable);
                        doc.Save();                                  // save programmatically — no UI dialog
                        string docNo = doc.DocNo;
                        long docKey = Convert.ToInt64(doc.DocKey);
                        try
                        {
                            WriteMeterTrans(docKey, docNo, billable, j.DocDate);   // only after a confirmed save
                        }
                        catch (Exception stampEx)
                        {
                            // doc.Save() and the stamp run in SEPARATE transactions. If the stamp fails
                            // the invoice would persist UNSTAMPED — the next Generate would not see it
                            // and would bill the same meters + period AGAIN (double-bill). Compensate:
                            // delete the just-saved invoice so the period stays cleanly re-generatable.
                            try
                            {
                                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                                    UserSession.CurrentUserSession, _db).Delete(docKey);
                                ErrorLog.Add("[" + j.Label + "]  stamp failed - invoice " + docNo +
                                    " was rolled back (deleted); re-run Generate for this row. Cause: " + ShortError(stampEx));
                            }
                            catch (Exception delEx)
                            {
                                // Compensation failed too: the invoice EXISTS but the period is NOT
                                // stamped. Re-generating would double-bill — tell the operator exactly
                                // what to do instead of hiding it.
                                ErrorLog.Add("[" + j.Label + "]  CRITICAL: invoice " + docNo + " was saved but the " +
                                    "meter/period stamp FAILED and the invoice could not be auto-deleted. DO NOT " +
                                    "re-generate this row - delete invoice " + docNo + " manually first. " +
                                    "Stamp error: " + ShortError(stampEx) + "  Delete error: " + ShortError(delEx));
                            }
                            throw stampEx;   // count the job as Failed either way
                        }
                        CreatedDocNos.Add(docNo);
                    }
                    if (zero.Count > 0)
                    {
                        WriteNoCharge(zero, j.DocDate);
                        NoChargeMeters += zero.Count;
                    }
                    Done++;
                }
                catch (Exception ex)
                {
                    Failed++;
                    ErrorLog.Add("[" + j.Label + "]  " + ShortError(ex));
                }
            }

            if (onProgress != null)
                onProgress(new Progress { Total = Total, Done = Done, Failed = Failed, CurrentLabel = "" });
        }

        private void WriteMeterTrans(long invoiceDocKey, string docNo, List<MeterBillLine> lines)
        { WriteMeterTrans(invoiceDocKey, docNo, lines, null); }

        private void WriteMeterTrans(long invoiceDocKey, string docNo, List<MeterBillLine> lines, DateTime? periodDate)
        {
            DateTime transDate = periodDate ?? DateTime.Now;
            using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("MeterTrans"))
                {
                    try
                    {
                        foreach (MeterBillLine ln in lines)
                        {
                            // Synthesized lines (COMMIT-MIN rule top-up) have no physical meter — no
                            // ItemMeterKey to stamp; inserting key 0 would violate the FK and roll the
                            // whole invoice back. The charge is on the invoice; nothing to roll forward.
                            if (ln.ItemMeterKey <= 0) continue;
                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP_MeterTrans] (ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, " +
                                "MeterTransDate, MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                "VALUES (@simt,@si,@code,@dt,@rd,@dk,@rmk)", cn, tx);
                            cmd.Parameters.AddWithValue("@simt", ln.ItemMeterKey);
                            cmd.Parameters.AddWithValue("@si", ln.ItemKey);
                            cmd.Parameters.AddWithValue("@code", ln.MeterTypeCode);
                            // Billing date = the BILLED PERIOD's billing day (not today): a backdated
                            // May run stamps 28/05, so June's baseline query finds this billing and the
                            // reading chain stays correct month after month.
                            cmd.Parameters.AddWithValue("@dt", transDate);
                            cmd.Parameters.AddWithValue("@rd", ln.Current);
                            cmd.Parameters.AddWithValue("@dk", invoiceDocKey);
                            cmd.Parameters.AddWithValue("@rmk", ln.ColorLabel + " meter - Invoice " + docNo);
                            cmd.ExecuteNonQuery();

                            // Billing-period guard: stamp the staging row so this meter + period can
                            // never be invoiced twice (Generate skips stamped rows).
                            SqlCommand st = new SqlCommand(
                                "UPDATE dbo.zSCP2_MeterEntry SET InvoicedDocKey=@dk, InvoicedDocNo=@dn, InvoicedAt=GETDATE(), StrategyCode=@sc " +
                                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo; " +
                                "IF @@ROWCOUNT=0 INSERT INTO dbo.zSCP2_MeterEntry " +
                                "  (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source,InvoicedDocKey,InvoicedDocNo,InvoicedAt,StrategyCode) " +
                                "  VALUES (@imk,@yr,@mo,@rd2,@dt2,@src,@dk,@dn,GETDATE(),@sc)", cn, tx);
                            st.Parameters.AddWithValue("@dk", invoiceDocKey);
                            st.Parameters.AddWithValue("@dn", docNo ?? "");
                            st.Parameters.AddWithValue("@imk", ln.ItemMeterKey);
                            st.Parameters.AddWithValue("@yr", _periodYear);
                            st.Parameters.AddWithValue("@mo", _periodMonth);
                            st.Parameters.AddWithValue("@rd2", ln.Current);
                            st.Parameters.AddWithValue("@dt2", (object)(ln.AuditDate ?? transDate));
                            st.Parameters.AddWithValue("@src", "INVOICE");
                            st.Parameters.AddWithValue("@sc", ln.StrategyCode ?? "");
                            st.ExecuteNonQuery();

                            // Immutable audit trail: the billed reading is appended to the log with its
                            // invoice number, baseline, usage AND the pricing as billed — the complete
                            // historical meter record behind the amount (incl. the strategy in force).
                            ScpMeterReadingLog.Append(cn, tx, ln.ItemMeterKey, _periodYear, _periodMonth,
                                ln.Current, ln.AuditDate ?? transDate, ScpMeterReadingLog.SOURCE_INVOICE, docNo ?? "",
                                ln.Last, ln.Usage, ln.Rate, ln.MinCharges, ln.Foc, ln.RebatePct, ln.Charge,
                                ln.StrategyCode, ln.StrategyNote);
                        }

                        // Contract snapshot: freeze the deal (strategy rules + billing flags) as it was
                        // for THIS invoice — editing the strategy later can never rewrite this history.
                        if (_contractSnapshots != null)
                        {
                            HashSet<long> snapped = new HashSet<long>();
                            foreach (MeterBillLine ln in lines)
                            {
                                if (ln.ContractKey <= 0 || !snapped.Add(ln.ContractKey)) continue;
                                string snap;
                                if (!_contractSnapshots.TryGetValue(ln.ContractKey, out snap)) continue;
                                SqlCommand sc = new SqlCommand(
                                    "INSERT INTO dbo.zSCP2_ContractSnapshot " +
                                    "(ContractKey, ContractNo, InvoiceDocKey, InvoiceDocNo, PeriodYear, PeriodMonth, SnapshotText, CreatedBy) " +
                                    "VALUES (@ck,@cno,@dk,@dn,@yr,@mo,@txt,@by)", cn, tx);
                                sc.Parameters.AddWithValue("@ck", ln.ContractKey);
                                sc.Parameters.AddWithValue("@cno", ln.ContractNo ?? "");
                                sc.Parameters.AddWithValue("@dk", invoiceDocKey);
                                sc.Parameters.AddWithValue("@dn", docNo ?? "");
                                sc.Parameters.AddWithValue("@yr", _periodYear);
                                sc.Parameters.AddWithValue("@mo", _periodMonth);
                                sc.Parameters.AddWithValue("@txt", snap ?? "");
                                string by = "";
                                try { by = UserSession.CurrentUserSession != null ? UserSession.CurrentUserSession.LoginUserID : ""; } catch { }
                                sc.Parameters.AddWithValue("@by", by ?? "");
                                sc.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Zero-charge meters: the reading rolls forward (an UNBILLED zSCP_MeterTrans row — next
        // period's baseline) and the staging row is stamped "NO CHARGE" so the period reads as
        // processed and Generate never picks it again. InvoicedDocKey stays NULL on purpose:
        // ReconcileDeletedInvoices only inspects real doc keys, so this stamp is never rolled back.
        private void WriteNoCharge(List<MeterBillLine> lines)
        { WriteNoCharge(lines, null); }

        private void WriteNoCharge(List<MeterBillLine> lines, DateTime? periodDate)
        {
            DateTime ncDate = periodDate ?? DateTime.Now;
            using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("MeterNoCharge"))
                {
                    try
                    {
                        foreach (MeterBillLine ln in lines)
                        {
                            // Synthesized lines (COMMIT-MIN rule top-up) have no physical meter to stamp.
                            if (ln.ItemMeterKey <= 0) continue;
                            // A flat/rental meter with FOC months remaining is FREE this period (RM0) —
                            // the free-months counter is decremented so rent resumes when it hits 0.
                            bool freeRental = ln.IsFlat && ln.Foc > 0m;
                            string noChargeRemark = ln.WaivedRental
                                ? "RENTAL WAIVED (" + ln.MeterTypeCode + ") - " + (ln.StrategyNote ?? "strategy target met")
                                : freeRental
                                    ? "RENTAL FREE (" + ln.MeterTypeCode + ") - FOC month"
                                    : ln.ColorLabel + " meter - NO CHARGE (zero amount)";

                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP_MeterTrans] (ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, " +
                                "MeterTransDate, MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                "VALUES (@simt,@si,@code,@dt,@rd,NULL,@rmk)", cn, tx);
                            cmd.Parameters.AddWithValue("@simt", ln.ItemMeterKey);
                            cmd.Parameters.AddWithValue("@si", ln.ItemKey);
                            cmd.Parameters.AddWithValue("@code", ln.MeterTypeCode);
                            cmd.Parameters.AddWithValue("@dt", ncDate);
                            cmd.Parameters.AddWithValue("@rd", ln.Current);
                            cmd.Parameters.AddWithValue("@rmk", noChargeRemark);
                            cmd.ExecuteNonQuery();

                            // Consume one free-rental month (countdown model): FOC Qty -= 1, floored at 0.
                            if (freeRental)
                            {
                                SqlCommand dec = new SqlCommand(
                                    "UPDATE dbo.zSCP2_ItemMeter SET FOCQty = CASE WHEN FOCQty > 0 THEN FOCQty - 1 ELSE 0 END, " +
                                    "LastModified=GETDATE() WHERE ItemMeterKey=@imk", cn, tx);
                                dec.Parameters.AddWithValue("@imk", ln.ItemMeterKey);
                                dec.ExecuteNonQuery();
                            }

                            SqlCommand st = new SqlCommand(
                                "UPDATE dbo.zSCP2_MeterEntry SET InvoicedDocNo=@dn, InvoicedAt=GETDATE(), StrategyCode=@sc " +
                                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo; " +
                                "IF @@ROWCOUNT=0 INSERT INTO dbo.zSCP2_MeterEntry " +
                                "  (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source,InvoicedDocNo,InvoicedAt,StrategyCode) " +
                                "  VALUES (@imk,@yr,@mo,@rd2,@dt2,@src,@dn,GETDATE(),@sc)", cn, tx);
                            string stampNo = ln.WaivedRental ? "RENTAL WAIVED" : (freeRental ? "RENTAL FREE" : "NO CHARGE");
                            st.Parameters.AddWithValue("@dn", stampNo);
                            st.Parameters.AddWithValue("@imk", ln.ItemMeterKey);
                            st.Parameters.AddWithValue("@yr", _periodYear);
                            st.Parameters.AddWithValue("@mo", _periodMonth);
                            st.Parameters.AddWithValue("@rd2", ln.Current);
                            st.Parameters.AddWithValue("@dt2", (object)(ln.AuditDate ?? ncDate));
                            st.Parameters.AddWithValue("@src", "INVOICE");
                            st.Parameters.AddWithValue("@sc", ln.StrategyCode ?? "");
                            st.ExecuteNonQuery();

                            ScpMeterReadingLog.Append(cn, tx, ln.ItemMeterKey, _periodYear, _periodMonth,
                                ln.Current, ln.AuditDate ?? ncDate,
                                ln.WaivedRental ? "RENTAL-WAIVED" : (freeRental ? "RENTAL-FREE" : "NO-CHARGE"), stampNo,
                                ln.Last, ln.Usage, ln.Rate, ln.MinCharges, ln.Foc, ln.RebatePct, 0m,
                                ln.StrategyCode, ln.StrategyNote);
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        private static string ShortError(Exception ex)
        {
            string m = ex.Message;
            if (ex.InnerException != null) m += " -> " + ex.InnerException.Message;
            return m.Length > 300 ? m.Substring(0, 300) : m;
        }
    }
}
