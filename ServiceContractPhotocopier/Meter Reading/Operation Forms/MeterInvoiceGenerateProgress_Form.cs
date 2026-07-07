using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Modal progress dialog for the Generate Invoice job. Runs MeterInvoiceGenerator on a worker
    /// task (invoices are saved programmatically — no per-invoice dialog) and reports counts back.
    /// </summary>
    public partial class MeterInvoiceGenerateProgress_Form : XtraForm
    {
        private readonly MeterInvoiceGenerator _gen;
        private readonly IList<MeterInvoiceGenerator.InvoiceJob> _jobs;
        private CancellationTokenSource _cts;
        private Task _task;

        public MeterInvoiceGenerateProgress_Form() { InitializeComponent(); }

        public MeterInvoiceGenerateProgress_Form(DBSetting db, IList<MeterInvoiceGenerator.InvoiceJob> jobs,
            DateTime docDate, DateTime readingDate) : this()
        {
            _jobs = jobs ?? new List<MeterInvoiceGenerator.InvoiceJob>();
            _gen = new MeterInvoiceGenerator(db, docDate, readingDate);
            this.LblTotal.Text = "Total: " + _jobs.Count;
            this.LblDone.Text  = "Done: 0";
            this.LblFail.Text  = "Failed: 0";
            this.LblCurrent.Text = "";
            this.LnkErrorLog.Visible = false;
            this.Progress.Properties.Minimum = 0;
            this.Progress.Properties.Maximum = Math.Max(1, _jobs.Count);
            this.Progress.EditValue = 0;
            this.BtnClose.Enabled = false;
            this.Load += new EventHandler(StartRun);
        }

        /// <summary>Invoices actually created (for the caller's summary).</summary>
        public int CreatedCount { get { return _gen != null ? _gen.Done : 0; } }
        public List<string> CreatedDocNos { get { return _gen != null ? _gen.CreatedDocNos : new List<string>(); } }

        private void StartRun(object sender, EventArgs e)
        {
            _cts = new CancellationTokenSource();
            _task = Task.Run(() =>
            {
                _gen.Run(_jobs, OnProgress, () => _cts.IsCancellationRequested);
            });
            _task.ContinueWith(_ =>
            {
                if (this.IsHandleCreated)
                    this.BeginInvoke(new Action(OnRunFinished));
            });
        }

        private void OnProgress(MeterInvoiceGenerator.Progress p)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(new Action(() =>
            {
                this.Progress.EditValue = p.Done + p.Failed;
                this.LblDone.Text    = "Done: "   + p.Done;
                this.LblFail.Text    = "Failed: " + p.Failed;
                this.LblCurrent.Text = string.IsNullOrEmpty(p.CurrentLabel) ? "" : "Working on: " + p.CurrentLabel;
            }));
        }

        private void OnRunFinished()
        {
            this.LblCurrent.Text = "Finished.  " + _gen.Done + " invoice(s) created.";
            this.BtnCancel.Enabled = false;
            this.BtnClose.Enabled = true;
            if (_gen.ErrorLog.Count > 0)
            {
                this.LnkErrorLog.Visible = true;
                this.LnkErrorLog.Text = "View error log (" + _gen.ErrorLog.Count + ")";
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (_cts != null) _cts.Cancel();
            this.BtnCancel.Enabled = false;
            this.LblCurrent.Text = "Cancelling...";
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void LnkErrorLog_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Path.GetTempPath(),
                "meter-invoice-errors-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            File.WriteAllLines(path, _gen.ErrorLog);
            try { System.Diagnostics.Process.Start(path); } catch { }
        }
    }
}
