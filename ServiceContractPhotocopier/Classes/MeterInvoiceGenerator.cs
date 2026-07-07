using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
        }

        public class Progress { public int Total; public int Done; public int Failed; public string CurrentLabel; }
        public delegate void ProgressHandler(Progress p);

        public List<string> ErrorLog { get; } = new List<string>();
        public List<string> CreatedDocNos { get; } = new List<string>();
        public int Total { get; private set; }
        public int Done { get; private set; }
        public int Failed { get; private set; }

        private readonly DBSetting _db;
        private readonly DateTime _docDate;
        private readonly DateTime _readingDate;

        public MeterInvoiceGenerator(DBSetting db, DateTime docDate, DateTime readingDate)
        {
            _db = db;
            _docDate = docDate;
            _readingDate = readingDate;
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
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = ScpInvoiceBuilder.BuildInvoice(
                        _db, j.DebtorCode, j.RefDocNo, j.Description, _docDate, _readingDate, j.Lines);
                    doc.Save();                                  // save programmatically — no UI dialog
                    string docNo = doc.DocNo;
                    WriteMeterTrans(Convert.ToInt64(doc.DocKey), docNo, j.Lines);   // only after a confirmed save
                    Done++;
                    CreatedDocNos.Add(docNo);
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
        {
            using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("MeterTrans"))
                {
                    try
                    {
                        foreach (MeterBillLine ln in lines)
                        {
                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP_MeterTrans] (ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, " +
                                "MeterTransDate, MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                "VALUES (@simt,@si,@code,@dt,@rd,@dk,@rmk)", cn, tx);
                            cmd.Parameters.AddWithValue("@simt", ln.ItemMeterKey);
                            cmd.Parameters.AddWithValue("@si", ln.ItemKey);
                            cmd.Parameters.AddWithValue("@code", ln.MeterTypeCode);
                            cmd.Parameters.AddWithValue("@dt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@rd", ln.Current);
                            cmd.Parameters.AddWithValue("@dk", invoiceDocKey);
                            cmd.Parameters.AddWithValue("@rmk", ln.ColorLabel + " meter - Invoice " + docNo);
                            cmd.ExecuteNonQuery();
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
