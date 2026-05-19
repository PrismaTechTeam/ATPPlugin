using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    /// <summary>
    /// Modal progress dialog for the Generate SI/ST job. Runs the generator on a
    /// worker task and reports counts back to the UI thread.
    /// </summary>
    public partial class StockRequestGenerateProgress_Form : XtraForm
    {
        private readonly SiStGenerator _gen;
        private readonly IList<SiStGenerator.Job> _jobs;
        private CancellationTokenSource _cts;
        private Task _task;

        public StockRequestGenerateProgress_Form() { InitializeComponent(); }

        public StockRequestGenerateProgress_Form(DBSetting db, UserSession us,
            IList<SiStGenerator.Job> jobs, string defaultFromLocation) : this()
        {
            _jobs = jobs ?? new List<SiStGenerator.Job>();
            _gen = new SiStGenerator(db, us, defaultFromLocation);
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

        private void OnProgress(SiStGenerator.Progress p)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(new Action(() =>
            {
                int doneOrFailed = p.Done + p.Failed;
                this.Progress.EditValue = doneOrFailed;
                this.LblDone.Text    = "Done: "   + p.Done;
                this.LblFail.Text    = "Failed: " + p.Failed;
                this.LblCurrent.Text = string.IsNullOrEmpty(p.CurrentLabel) ? "" : "Working on: " + p.CurrentLabel;
            }));
        }

        private void OnRunFinished()
        {
            this.LblCurrent.Text = "Finished.";
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
            _cts?.Cancel();
            this.BtnCancel.Enabled = false;
            this.LblCurrent.Text = "Cancelling…";
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void LnkErrorLog_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Path.GetTempPath(),
                "sist-generate-errors-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            File.WriteAllLines(path, _gen.ErrorLog);
            try { System.Diagnostics.Process.Start(path); } catch { }
        }
    }
}
