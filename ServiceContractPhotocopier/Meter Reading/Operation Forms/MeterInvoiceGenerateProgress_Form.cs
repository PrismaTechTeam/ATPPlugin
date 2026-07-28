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
        // Humane progress: elapsed clock + remaining-time estimate + projected finish time.
        private DateTime _startedAt;
        private System.Windows.Forms.Timer _clock;
        private int _processed;
        private LabelControl _lblEta;

        public MeterInvoiceGenerateProgress_Form() { InitializeComponent(); }

        public MeterInvoiceGenerateProgress_Form(DBSetting db, IList<MeterInvoiceGenerator.InvoiceJob> jobs,
            DateTime docDate, DateTime readingDate, int periodYear, int periodMonth)
            : this(db, jobs, docDate, readingDate, periodYear, periodMonth, null) { }

        public MeterInvoiceGenerateProgress_Form(DBSetting db, IList<MeterInvoiceGenerator.InvoiceJob> jobs,
            DateTime docDate, DateTime readingDate, int periodYear, int periodMonth,
            Dictionary<long, string> contractSnapshots) : this()
        {
            _jobs = jobs ?? new List<MeterInvoiceGenerator.InvoiceJob>();
            _gen = new MeterInvoiceGenerator(db, docDate, readingDate, periodYear, periodMonth, contractSnapshots);
            this.LblTotal.Text = "Total: " + _jobs.Count;
            this.LblDone.Text  = "Done: 0";
            this.LblFail.Text  = "Failed: 0";
            this.LblCurrent.Text = "";
            this.LnkErrorLog.Visible = false;
            this.Progress.Properties.Minimum = 0;
            this.Progress.Properties.Maximum = Math.Max(1, _jobs.Count);
            this.Progress.EditValue = 0;
            this.BtnClose.Enabled = false;

            _lblEta = new LabelControl();
            _lblEta.Location = new System.Drawing.Point(18, 199);
            _lblEta.AutoSizeMode = LabelAutoSizeMode.None;
            _lblEta.Size = new System.Drawing.Size(296, 18);
            _lblEta.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _lblEta.Appearance.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
            _lblEta.Appearance.Options.UseFont = true;
            _lblEta.Appearance.Options.UseForeColor = true;
            _lblEta.Text = "";
            this.Controls.Add(_lblEta);
            _lblEta.BringToFront();

            this.Load += new EventHandler(StartRun);
        }

        /// <summary>Invoices actually created (for the caller's summary).</summary>
        public int CreatedCount { get { return _gen != null ? _gen.CreatedDocNos.Count : 0; } }
        public List<string> CreatedDocNos { get { return _gen != null ? _gen.CreatedDocNos : new List<string>(); } }

        private void StartRun(object sender, EventArgs e)
        {
            _startedAt = DateTime.Now;
            _clock = new System.Windows.Forms.Timer();
            _clock.Interval = 1000;
            _clock.Tick += new EventHandler(Clock_Tick);
            _clock.Start();

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
                _processed = p.Done + p.Failed;
                this.Progress.EditValue = _processed;
                this.LblDone.Text    = "Done: "   + p.Done;
                this.LblFail.Text    = "Failed: " + p.Failed;
                this.LblCurrent.Text = string.IsNullOrEmpty(p.CurrentLabel) ? "" : "Working on: " + p.CurrentLabel;
            }));
        }

        // Once a second: elapsed time; after a few invoices, also the projected remaining time and
        // the clock time it should finish — so the operator can walk away and come back on time.
        private void Clock_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - _startedAt;
            string t = "Elapsed " + FmtSpan(elapsed);
            int total = _jobs.Count;
            if (_processed > 0 && _processed < total && elapsed.TotalSeconds >= 3)
            {
                double perJob = elapsed.TotalSeconds / _processed;
                TimeSpan remaining = TimeSpan.FromSeconds(perJob * (total - _processed));
                DateTime finishAt = DateTime.Now.Add(remaining);
                t += "   •   ~" + FmtSpan(remaining) + " left   •   finish ≈ " + finishAt.ToString("h:mm tt");
            }
            _lblEta.Text = t;
        }

        private static string FmtSpan(TimeSpan s)
        {
            if (s.TotalHours >= 1) return string.Format("{0}:{1:00}:{2:00}", (int)s.TotalHours, s.Minutes, s.Seconds);
            return string.Format("{0:00}:{1:00}", s.Minutes, s.Seconds);
        }

        private void OnRunFinished()
        {
            if (_clock != null) _clock.Stop();
            _lblEta.Text = "Completed in " + FmtSpan(DateTime.Now - _startedAt);
            this.LblCurrent.Text = "Finished.  " + _gen.CreatedDocNos.Count + " invoice(s) created." +
                (_gen.NoChargeMeters > 0 ? "  " + _gen.NoChargeMeters + " zero-amount meter(s) marked NO CHARGE." : "");
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
