using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// The send screen, in two modes over one grid because they show the same thing at different
    /// points in time:
    ///   CONFIRM  — what is ABOUT to go out. Grouped by customer and auto-expanded, so the operator
    ///              sees "one email per customer, these invoices attached" before committing.
    ///   PROGRESS — what a running (or finished) job DID. Reads the job tables, so it also shows a
    ///              run someone else started on another PC, and survives this form being closed.
    ///
    /// The wording comes from each contract's own email template - resolved before the job starts,
    /// never retyped here. That is the point of dropping AutoCount's Batch Mail dialog: the operator
    /// should not be able to accidentally send one customer another customer's agreed wording.
    /// </summary>
    public partial class BulkEmailJob_Form : XtraForm
    {
        private DBSetting _db;
        private List<ScpEmailJob.Recipient> _pending;   // CONFIRM mode
        private string _fromName = "", _fromEmail = "";
        private ScpEmailTemplates.Template _defaultTpl;
        private long _jobKey;                           // PROGRESS mode
        private DataTable _grid;

        public BulkEmailJob_Form()
        {
            InitializeComponent();
        }

        /// <summary>CONFIRM mode: review, then start the job.</summary>
        public BulkEmailJob_Form(DBSetting db, List<ScpEmailJob.Recipient> recipients,
            string fromName, string fromEmail, ScpEmailTemplates.Template defaultTpl) : this()
        {
            _db = db; _pending = recipients; _fromName = fromName; _fromEmail = fromEmail;
            _defaultTpl = defaultTpl;
            BuildConfirmGrid();
        }

        /// <summary>PROGRESS mode: watch a job. Pass 0 for "the most recent one".</summary>
        public BulkEmailJob_Form(DBSetting db, long jobKey) : this()
        {
            _db = db;
            _jobKey = jobKey > 0 ? jobKey : LatestJobKey(db);
            EnterProgressMode();
        }

        // ───────────────────────── CONFIRM ─────────────────────────

        private void BuildConfirmGrid()
        {
            _grid = NewGridTable();
            int docs = 0;
            foreach (ScpEmailJob.Recipient r in _pending)
            {
                docs += r.DocNos.Count;
                for (int i = 0; i < r.DocNos.Count; i++)
                {
                    DataRow g = _grid.NewRow();
                    g["Customer"] = r.DebtorCode + "  " + r.DebtorName;
                    g["DocNo"] = r.DocNos[i];
                    g["Email"] = r.Email;
                    g["Template"] = r.Template != null ? r.Template.Name : "(default)";
                    g["Status"] = string.IsNullOrEmpty((r.Email ?? "").Trim()) ? "NO EMAIL" : "";
                    g["Note"] = "";
                    _grid.Rows.Add(g);
                }
            }
            BindGrid();

            int noEmail = 0;
            foreach (ScpEmailJob.Recipient r in _pending)
                if (string.IsNullOrEmpty((r.Email ?? "").Trim())) noEmail++;

            LblHeadline.Text = "Confirm: " + _pending.Count + " email(s), " + docs + " invoice(s)";
            LblSub.Text = "One email per customer with that customer's invoices attached. The subject " +
                "and message come from each contract's own email template." +
                (noEmail > 0 ? "   ⚠ " + noEmail + " customer(s) have no email address and will be skipped." : "");
            BtnConfirm.Visible = true;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (_pending == null || _pending.Count == 0) return;
            if (ScpEmailJob.IsRunning)
            {
                XtraMessageBox.Show("A send is already running on this PC. Open Send Progress to watch it.",
                    "Send Invoices", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                _jobKey = ScpEmailJob.Start(_db, _pending, _fromName, _fromEmail, _defaultTpl);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Send Invoices", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _pending = null;
            EnterProgressMode();
        }

        // ───────────────────────── PROGRESS ─────────────────────────

        private void EnterProgressMode()
        {
            BtnConfirm.Visible = false;
            PanelStatus.Visible = true;
            _grid = NewGridTable();
            BindGrid();
            // Every label is set from the REAL state inside RefreshProgress — nothing is assumed
            // here. Saying "Sending..." before looking is how this screen ended up claiming a send
            // was in flight when no job existed at all.
            RefreshProgress();
        }

        private void Ticker_Tick(object sender, EventArgs e) { RefreshProgress(); }

        private void RefreshProgress()
        {
            // Nothing has ever been sent from this book: say so plainly instead of leaving the
            // designer's placeholder text on screen.
            if (_db == null || _jobKey <= 0)
            {
                Ticker.Enabled = false;
                Marquee.Visible = false;
                LblHeadline.Text = "Send history";
                LblSub.Text = "This screen shows the most recent send, including one started on another PC.";
                LblBig.Text = "No send has been run yet";
                LblBig.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);
                LblCounts.Text = "Tick the invoices on the previous screen and press Email Selected to start one.";
                return;
            }
            string status = "RUNNING";
            int total = 0, ok = 0, failed = 0, skipped = 0;
            try
            {
                DataTable hdr = _db.GetDataTable(
                    "SELECT Status, TotalCount, SuccessCount, FailedCount, SkippedCount " +
                    "FROM dbo.zSCP2_EmailJob WHERE JobKey=" + _jobKey, false);
                if (hdr.Rows.Count > 0)
                {
                    status = Convert.ToString(hdr.Rows[0]["Status"]);
                    total = Convert.ToInt32(hdr.Rows[0]["TotalCount"]);
                    ok = Convert.ToInt32(hdr.Rows[0]["SuccessCount"]);
                    failed = Convert.ToInt32(hdr.Rows[0]["FailedCount"]);
                    skipped = Convert.ToInt32(hdr.Rows[0]["SkippedCount"]);
                }

                DataTable items = _db.GetDataTable(
                    "SELECT DebtorCode, DebtorName, Email, DocNos, Status, ISNULL(ErrorMsg,'') AS ErrorMsg, SentAt " +
                    "FROM dbo.zSCP2_EmailJobItem WHERE JobKey=" + _jobKey + " ORDER BY ItemKey", false);
                _grid.Rows.Clear();
                foreach (DataRow s in items.Rows)
                {
                    DataRow g = _grid.NewRow();
                    g["Customer"] = Convert.ToString(s["DebtorCode"]) + "  " + Convert.ToString(s["DebtorName"]);
                    g["DocNo"] = Convert.ToString(s["DocNos"]);
                    g["Email"] = Convert.ToString(s["Email"]);
                    g["Template"] = "";
                    g["Status"] = Convert.ToString(s["Status"]);
                    g["Note"] = Convert.ToString(s["ErrorMsg"]);
                    _grid.Rows.Add(g);
                }
                GridItems.RefreshDataSource();
                GridViewItems.ExpandAllGroups();
            }
            catch { }

            int done = ok + failed + skipped;
            bool running = status == "RUNNING";
            Marquee.Visible = running;
            LblCounts.Text = done + " of " + total + " processed     ✔ " + ok + " sent     ✖ " + failed +
                " failed     ⊘ " + skipped + " skipped";

            Ticker.Enabled = running;   // only poll while there is something to poll for
            if (running)
            {
                LblBig.Text = "Sending email...";
                LblBig.Appearance.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
                LblHeadline.Text = "Sending";
                LblSub.Text = "You can close this window — the send keeps running. " +
                    "Reopen it any time from Send Progress.";
            }
            else
            {
                LblBig.Text = status == "ABORTED" ? "Stopped before finishing" : "Completed";
                LblBig.Appearance.ForeColor = failed > 0
                    ? System.Drawing.Color.FromArgb(198, 40, 40)
                    : System.Drawing.Color.FromArgb(27, 94, 32);
                LblHeadline.Text = "Send finished";
                LblSub.Text = "Every send is recorded — a customer can be sent to again, and both times are kept.";
            }
        }

        private static long LatestJobKey(DBSetting db)
        {
            try
            {
                DataTable t = db.GetDataTable("SELECT TOP 1 JobKey FROM dbo.zSCP2_EmailJob ORDER BY JobKey DESC", false);
                return t.Rows.Count > 0 ? Convert.ToInt64(t.Rows[0]["JobKey"]) : 0;
            }
            catch { return 0; }
        }

        // ───────────────────────── grid ─────────────────────────

        private static DataTable NewGridTable()
        {
            DataTable t = new DataTable("Send");
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("DocNo", typeof(string));
            t.Columns.Add("Email", typeof(string));
            t.Columns.Add("Template", typeof(string));
            t.Columns.Add("Status", typeof(string));
            t.Columns.Add("Note", typeof(string));
            return t;
        }

        private void BindGrid()
        {
            GridItems.DataSource = _grid;
            GridViewItems.Columns.Clear();
            GridViewItems.PopulateColumns();
            Col("Customer", "Customer", 260, 0);
            Col("DocNo", "Invoice No", 190, 1);
            Col("Email", "Email", 220, 2);
            Col("Template", "Template", 130, 3);
            Col("Status", "Status", 90, 4);
            Col("Note", "Note", 300, 5);
            // Grouped by customer and expanded, so the "one email per customer" shape is the first
            // thing you see rather than something you have to work out from a flat list.
            GridColumn c = GridViewItems.Columns["Customer"];
            if (c != null) c.GroupIndex = 0;
            GridViewItems.ExpandAllGroups();
            GridViewItems.RowStyle -= new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewItems_RowStyle);
            GridViewItems.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewItems_RowStyle);
        }

        private void Col(string field, string caption, int width, int visibleIndex)
        {
            GridColumn c = GridViewItems.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
        }

        private void GridViewItems_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            object v = GridViewItems.GetRowCellValue(e.RowHandle, "Status");
            string s = v == null || v == DBNull.Value ? "" : v.ToString();
            if (s == "FAILED")
            {
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 224, 224);
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(198, 40, 40);
            }
            else if (s == "SENT")
            {
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(212, 239, 212);
            }
            else if (s == "SKIPPED" || s == "NO EMAIL")
            {
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 236, 179);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Ticker.Enabled = false;
            this.Close();
        }
    }
}
