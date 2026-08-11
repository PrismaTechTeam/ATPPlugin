using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Invoicing.Sales.Invoice;
using AutoCount.Mail;
using AutoCount.Report;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraReports.UI;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Bulk Email Invoice — filter posted sales invoices (date range / customer / meter-billing
    /// only / any-column auto-filter), tick the ones to send, and hand them to AutoCount's Batch
    /// Mail dialog: each customer receives ONE email with THEIR invoice PDFs attached (the invoice
    /// report is rendered per document with the DEFAULT "Invoice Document" layout). Sending rides
    /// AutoCount's own mail queue — every email is logged in the Server Mailing List with Resend.
    /// AutoCount ships batch email only for the Debtor Statement; this form fills the invoice gap.
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Bulk Email Invoice", MenuOrder = 460, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class BulkEmailInvoice_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;
        private DataTable _dt;
        private CheckEdit _chkUnsentOnly;     // #9b: "ready but not yet emailed" checklist filter
        private SimpleButton _btnTemplate;    // #9a: saved subject/body template
        private HashSet<InvoiceBatchMailEntity> _sentEntities;   // #9b: recipients Send actually dispatched

        // One email per customer: their invoice PDFs are the attachments. AccNo/CompanyName/DocNos
        // are reflected into the Batch Mail grid columns and usable as {tokens} in the message.
        private class InvoiceBatchMailEntity : BatchMail2Entity
        {
            public string AccNo { get; set; }
            public string CompanyName { get; set; }
            public string DocNos { get; set; }
            // FIELDS (not properties) so BatchMail2's reflection never turns them into grid columns.
            public List<long> DocKeyList = new List<long>();
            public List<string> DocNoList = new List<string>();
        }

        public BulkEmailInvoice_Form()
        {
            InitializeComponent();
            // Native AutoCount header (same as Maintain Service Contract) with the hint line hidden.
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            ConfigureGrid();
            this.CmbEmailSource.Properties.Items.Add("Debtor Email Address");
            this.CmbEmailSource.Properties.Items.Add("Statement Email (same as SOA Batch Mail)");
            this.CmbEmailSource.SelectedIndex = 0;
            ApplyFilterDefaults();

            // Demo 28/07 #9a/#9b (created in code — the strict designer stays untouched):
            // saved message template + the "which invoices are ready but not yet emailed" filter.
            _btnTemplate = new SimpleButton();
            _btnTemplate.Text = "Template…";
            _btnTemplate.Location = new System.Drawing.Point(350, 87);
            _btnTemplate.Size = new System.Drawing.Size(104, 28);
            _btnTemplate.Click += new EventHandler(BtnTemplate_Click);
            this.GrpFilter.Controls.Add(_btnTemplate);
            _chkUnsentOnly = new CheckEdit();
            _chkUnsentOnly.Properties.Caption = "Not yet emailed only";
            _chkUnsentOnly.Location = new System.Drawing.Point(88, 118);
            _chkUnsentOnly.Size = new System.Drawing.Size(240, 22);
            _chkUnsentOnly.CheckedChanged += delegate { ApplyUnsentFilter(); };
            this.GrpFilter.Controls.Add(_chkUnsentOnly);

            // DEV/TEST: Ctrl+Shift+3 clears the send history so the same invoices can be emailed
            // again from a clean slate. Same combo the Meter Reading screen uses for its wipe, so
            // there is one thing to remember. Key combo only — no button, on purpose.
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(DevShortcut_KeyDown);
        }

        private void DevShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control || !e.Shift || e.KeyCode != Keys.D3) return;
            e.Handled = true;
            DevWipeSendHistory();
        }

        /// <summary>
        /// Wipe every trace of past sends: the per-invoice log (which drives the Emailed column),
        /// the job rows behind Send Progress, and any leftover locks — a lock abandoned by a killed
        /// test run would otherwise block those invoices for fifteen minutes.
        /// The emails themselves are already gone; this only clears OUR record of them.
        /// </summary>
        private void DevWipeSendHistory()
        {
            if (_dbSetting == null) return;
            int logs = 0, jobs = 0, locks = 0;
            try
            {
                DataTable c = _dbSetting.GetDataTable(
                    "SELECT (SELECT COUNT(*) FROM dbo.zSCP2_EmailLog) AS L, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_EmailJob) AS J, " +
                    "       (SELECT COUNT(*) FROM dbo.zSCP2_EmailLock) AS K", false);
                if (c.Rows.Count > 0)
                {
                    logs = Convert.ToInt32(c.Rows[0]["L"]);
                    jobs = Convert.ToInt32(c.Rows[0]["J"]);
                    locks = Convert.ToInt32(c.Rows[0]["K"]);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not read the send history:\r\n" + ex.Message,
                    "Dev Wipe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (logs == 0 && jobs == 0 && locks == 0)
            {
                XtraMessageBox.Show("There is no send history to clear.", "Dev Wipe",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (ScpEmailJob.IsRunning)
            {
                XtraMessageBox.Show("A send is running right now — let it finish before clearing the history.",
                    "Dev Wipe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (XtraMessageBox.Show(
                    "TESTING SHORTCUT\r\n\r\nClear ALL send history so the same invoices can be emailed again?\r\n\r\n" +
                    "    " + logs + " sent-invoice record(s)\r\n" +
                    "    " + jobs + " send job(s) and their detail\r\n" +
                    "    " + locks + " leftover lock(s)\r\n\r\n" +
                    "The emails already went out — this only erases OUR record that they did. Cannot be undone.",
                    "Dev Wipe — Send History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_EmailJobItem");
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_EmailJob");
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_EmailLock");
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_EmailLog");
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Clearing failed:\r\n" + ex.Message, "Dev Wipe",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LoadData();
            XtraMessageBox.Show("Send history cleared — every invoice is 'not yet emailed' again.",
                "Dev Wipe", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ApplyUnsentFilter()
        {
            GridViewInv.ActiveFilterString =
                _chkUnsentOnly != null && _chkUnsentOnly.Checked ? "[EmailedAt] Is Null" : "";
        }

        private void BtnTemplate_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            using (BulkEmailTemplate_Form dlg = new BulkEmailTemplate_Form(_dbSetting))
                dlg.ShowDialog(this);
        }

        private void ApplyFilterDefaults()
        {
            this.ChkMeterOnly.Checked = true;
            this.SluDebtor.EditValue = null;
            DateTime today = DateTime.Today;
            this.DtFrom.DateTime = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            this.DtTo.DateTime = today;
        }

        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                SetBtnAcIcon(this.BtnSelectAll, img.GetLargeImage_Approve());
            }
            catch { }   // icons are cosmetic — never block the form over an image lookup
            SetBtnSvgIcon(this.BtnMailSetting, "svgimages/icon builder/actions_settings.svg", 24);
            SetBtnSvgIcon(this.BtnEmail, "svgimages/outlook inspired/mail.svg", 24);
            SetBtnSvgIcon(this.BtnLoad, "svgimages/xaf/action_filter.svg", 20);
            SetBtnSvgIcon(this.BtnReset, "svgimages/xaf/action_reload.svg", 20);
        }

        private static void SetBtnAcIcon(SimpleButton btn, System.Drawing.Image image)
        {
            if (btn == null || image == null) return;
            btn.ImageOptions.Image = image;
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        private static void SetBtnSvgIcon(SimpleButton btn, string svgName, int size)
        {
            if (btn == null) return;
            DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(svgName);
            if (img == null) return;
            btn.ImageOptions.SvgImage = img;
            btn.ImageOptions.SvgImageSize = new System.Drawing.Size(size, size);
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            btn.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
        }

        public BulkEmailInvoice_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            LoadDebtorLookup();
            LoadData();
        }

        public BulkEmailInvoice_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            LoadDebtorLookup();
            LoadData();
        }

        private void ConfigureGrid()
        {
            GridViewInv.OptionsBehavior.Editable = true;
            GridViewInv.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            GridViewInv.OptionsView.ColumnAutoWidth = false;
            GridViewInv.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewInv_RowCellStyle);
        }

        private void LoadDebtorLookup()
        {
            if (_dbSetting == null) return;
            DataTable lk;
            try
            {
                lk = _dbSetting.GetDataTable(
                    "SELECT AccNo, CompanyName FROM dbo.Debtor ORDER BY AccNo", false);
            }
            catch { lk = new DataTable(); }
            SluDebtor.Properties.DataSource = lk;
            SluDebtor.Properties.ValueMember = "AccNo";
            SluDebtor.Properties.DisplayMember = "AccNo";
            SluDebtor.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            SluDebtorView.OptionsBehavior.AutoPopulateColumns = false;
            SluDebtorView.Columns.Clear();
            GridColumn cNo = SluDebtorView.Columns.AddVisible("AccNo");
            cNo.Caption = "Debtor"; cNo.Width = 90;
            GridColumn cName = SluDebtorView.Columns.AddVisible("CompanyName");
            cName.Caption = "Company Name"; cName.Width = 260;
            SluDebtorView.OptionsView.ShowAutoFilterRow = true;
        }

        // ───────────────────────────── load + filter ─────────────────────────────

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            if (_dbSetting == null) return;
            DateTime from = DtFrom.DateTime.Date;
            DateTime to = DtTo.DateTime.Date;
            if (to < from) { DateTime t = from; from = to; to = t; }
            string debtor = SluDebtor.EditValue == null ? "" : SluDebtor.EditValue.ToString().Trim();

            // Cancelled invoices are excluded outright — emailing a cancelled document is never right.
            // "Meter-billing only" = invoices stamped by the meter billing run (zSCP2_MeterEntry).
            string sql =
                "SELECT IV.DocKey, IV.DocNo, IV.DocDate, IV.DebtorCode, IV.DebtorName, IV.NetTotal, " +
                "IV.[Description], ISNULL(D.EmailAddress,'') AS EmailAddress, " +
                "ISNULL(D.StatementEmail,'') AS StatementEmail, em.LastEmailAt " +
                "FROM dbo.IV IV LEFT JOIN dbo.Debtor D ON D.AccNo = IV.DebtorCode " +
                "LEFT JOIN (SELECT DocKey, MAX(SentAt) AS LastEmailAt FROM dbo.zSCP2_EmailLog GROUP BY DocKey) em " +
                "  ON em.DocKey = IV.DocKey " +
                "WHERE IV.Cancelled = 'F' AND IV.DocDate >= @from AND IV.DocDate <= @to " +
                (debtor.Length > 0 ? "AND IV.DebtorCode = @debtor " : "") +
                (ChkMeterOnly.Checked
                    ? "AND EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocKey = IV.DocKey) "
                    : "") +
                "ORDER BY IV.DebtorCode, IV.DocDate, IV.DocNo";

            DataTable raw;
            try
            {
                if (debtor.Length > 0)
                    raw = _dbSetting.GetDataTable(sql, false,
                        new SqlParameter("@from", from), new SqlParameter("@to", to),
                        new SqlParameter("@debtor", debtor));
                else
                    raw = _dbSetting.GetDataTable(sql, false,
                        new SqlParameter("@from", from), new SqlParameter("@to", to));
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Bulk Email Invoice",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _dt = new DataTable();
            _dt.Columns.Add("Sel", typeof(bool));
            _dt.Columns.Add("DocNo", typeof(string));
            _dt.Columns.Add("DocDate", typeof(DateTime));
            _dt.Columns.Add("DebtorCode", typeof(string));
            _dt.Columns.Add("DebtorName", typeof(string));
            _dt.Columns.Add("Email", typeof(string));
            _dt.Columns.Add("NetTotal", typeof(decimal));
            _dt.Columns.Add("Description", typeof(string));
            _dt.Columns.Add("DocKey", typeof(long));
            _dt.Columns.Add("EmailAddress", typeof(string));
            _dt.Columns.Add("StatementEmail", typeof(string));
            _dt.Columns.Add("EmailedAt", typeof(DateTime));   // #9b: last bulk-email send of this invoice
            _dt.Columns.Add("OpenContract", typeof(string));  // button column: jump to the contract
            foreach (DataRow r in raw.Rows)
            {
                DataRow d = _dt.NewRow();
                d["Sel"] = false;
                d["DocNo"] = Convert.ToString(r["DocNo"]);
                if (r["DocDate"] != DBNull.Value) d["DocDate"] = Convert.ToDateTime(r["DocDate"]);
                d["DebtorCode"] = Convert.ToString(r["DebtorCode"]);
                d["DebtorName"] = Convert.ToString(r["DebtorName"]);
                d["NetTotal"] = r["NetTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(r["NetTotal"]);
                d["Description"] = Convert.ToString(r["Description"]);
                d["DocKey"] = Convert.ToInt64(r["DocKey"]);
                d["EmailAddress"] = Convert.ToString(r["EmailAddress"]).Trim();
                d["StatementEmail"] = Convert.ToString(r["StatementEmail"]).Trim();
                if (r["LastEmailAt"] != DBNull.Value) d["EmailedAt"] = Convert.ToDateTime(r["LastEmailAt"]);
                _dt.Rows.Add(d);
            }
            FillEmailColumn();
            GridInv.DataSource = _dt;
            ConfigureColumns();
            ApplyUnsentFilter();
            UpdateCount();
        }

        private void ConfigureColumns()
        {
            foreach (string h in new string[] { "DocKey", "EmailAddress", "StatementEmail" })
                if (GridViewInv.Columns[h] != null) GridViewInv.Columns[h].Visible = false;
            SetCol("Sel", "Send", 55, true);
            GridColumn sel = GridViewInv.Columns["Sel"];
            if (sel != null) sel.ColumnEdit = RepoSel;
            SetCol("DocNo", "Invoice No", 110, false);
            SetCol("DocDate", "Date", 85, false);
            GridColumn cd = GridViewInv.Columns["DocDate"];
            if (cd != null)
            { cd.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime; cd.DisplayFormat.FormatString = "dd/MM/yyyy"; }
            SetCol("DebtorCode", "Customer", 90, false);
            SetCol("DebtorName", "Customer Name", 240, false);
            SetCol("Email", "Email", 220, false);
            SetCol("NetTotal", "Total", 90, false);
            GridColumn ct = GridViewInv.Columns["NetTotal"];
            if (ct != null)
            { ct.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric; ct.DisplayFormat.FormatString = "n2"; }
            SetCol("Description", "Description", 260, false);
            SetCol("EmailedAt", "Emailed", 120, false);
            GridColumn ce = GridViewInv.Columns["EmailedAt"];
            if (ce != null)
            { ce.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime; ce.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm"; }

            // Last column: jump straight to the contract behind this invoice. Chasing a query back
            // to the contract by hand is the most common thing an operator does from this screen.
            GridColumn co = GridViewInv.Columns["OpenContract"];
            if (co != null)
            {
                co.Caption = "Contract";
                co.Width = 90;
                co.Visible = true;
                co.VisibleIndex = GridViewInv.Columns.Count;
                co.OptionsColumn.AllowEdit = true;
                co.OptionsColumn.ReadOnly = false;
                co.OptionsFilter.AllowFilter = false;
                if (_openContractRepo == null)
                {
                    _openContractRepo = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
                    _openContractRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
                    _openContractRepo.Buttons.Clear();
                    DevExpress.XtraEditors.Controls.EditorButton b =
                        new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
                    b.Caption = "Open";
                    b.Width = 60;
                    _openContractRepo.Buttons.Add(b);
                    _openContractRepo.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(OpenContract_ButtonClick);
                    GridInv.RepositoryItems.Add(_openContractRepo);
                }
                co.ColumnEdit = _openContractRepo;
            }
        }

        private DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit _openContractRepo;

        /// <summary>
        /// Open the contract this invoice came from. A meter invoice usually covers ONE contract, so
        /// go straight there — asking every time would be a click for nothing. It can legitimately
        /// span several (debtor-level grouping bills a customer's contracts together), and only then
        /// is there a choice worth making, so only then is there a dialog.
        /// </summary>
        private void OpenContract_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            DataRow r = GridViewInv.GetDataRow(GridViewInv.FocusedRowHandle);
            if (r == null || _dbSetting == null) return;
            long docKey = Convert.ToInt64(r["DocKey"]);
            string docNo = Convert.ToString(r["DocNo"]);

            DataTable t;
            try
            {
                t = _dbSetting.GetDataTable(
                    "SELECT DISTINCT c.ContractKey, c.ContractNo, ISNULL(c.DebtorCode,'') AS DebtorCode, " +
                    "       ISNULL(c.Description,'') AS Description " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = me.ItemMeterKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "WHERE me.InvoicedDocKey = " + docKey + " ORDER BY c.ContractNo", false);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not look up the contract:\r\n" + ex.Message,
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (t.Rows.Count == 0)
            {
                XtraMessageBox.Show(
                    "No contract is linked to " + docNo + ".\r\n\r\n" +
                    "Only invoices generated by meter billing carry that link — a manually keyed " +
                    "invoice has nothing to open.",
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long key = Convert.ToInt64(t.Rows[0]["ContractKey"]);
            if (t.Rows.Count > 1)
            {
                long picked = PickContract(t, docNo);
                if (picked <= 0) return;
                key = picked;
            }
            try
            {
                ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form f =
                    new ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form(_dbSetting, key);
                f.Show();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the contract:\r\n" + ex.Message,
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Which of this invoice's contracts to open. Returns 0 when cancelled.</summary>
        private long PickContract(DataTable contracts, string docNo)
        {
            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = "Open Contract";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new System.Drawing.Size(520, 320);
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;

                LabelControl lbl = new LabelControl();
                lbl.Text = docNo + " covers " + contracts.Rows.Count + " contracts — which one?";
                lbl.Location = new System.Drawing.Point(12, 12);
                lbl.AutoSizeMode = LabelAutoSizeMode.None;
                lbl.Size = new System.Drawing.Size(496, 18);
                dlg.Controls.Add(lbl);

                DevExpress.XtraGrid.GridControl g = new DevExpress.XtraGrid.GridControl();
                DevExpress.XtraGrid.Views.Grid.GridView v = new DevExpress.XtraGrid.Views.Grid.GridView();
                g.MainView = v;
                g.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { v });
                v.GridControl = g;
                v.OptionsBehavior.Editable = false;
                v.OptionsView.ShowGroupPanel = false;
                g.Location = new System.Drawing.Point(12, 36);
                g.Size = new System.Drawing.Size(496, 228);
                g.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                g.DataSource = contracts;
                dlg.Controls.Add(g);
                v.PopulateColumns();
                if (v.Columns["ContractKey"] != null) v.Columns["ContractKey"].Visible = false;
                if (v.Columns["ContractNo"] != null) { v.Columns["ContractNo"].Caption = "Contract"; v.Columns["ContractNo"].Width = 120; }
                if (v.Columns["DebtorCode"] != null) { v.Columns["DebtorCode"].Caption = "Customer"; v.Columns["DebtorCode"].Width = 110; }
                if (v.Columns["Description"] != null) { v.Columns["Description"].Caption = "Description"; v.Columns["Description"].Width = 250; }

                long picked = 0;
                SimpleButton ok = new SimpleButton();
                ok.Text = "Open"; ok.Size = new System.Drawing.Size(90, 28);
                ok.Location = new System.Drawing.Point(324, 276);
                ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                ok.Click += new EventHandler(delegate
                {
                    DataRow sel = v.GetDataRow(v.FocusedRowHandle);
                    if (sel == null) return;
                    picked = Convert.ToInt64(sel["ContractKey"]);
                    dlg.DialogResult = DialogResult.OK;
                });
                dlg.Controls.Add(ok);

                SimpleButton cancel = new SimpleButton();
                cancel.Text = "Cancel"; cancel.Size = new System.Drawing.Size(90, 28);
                cancel.Location = new System.Drawing.Point(418, 276);
                cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancel.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(cancel);
                dlg.CancelButton = cancel;

                v.DoubleClick += new EventHandler(delegate { ok.PerformClick(); });

                return dlg.ShowDialog(this) == DialogResult.OK ? picked : 0;
            }
        }

        private void SetCol(string field, string caption, int width, bool editable)
        {
            GridColumn c = GridViewInv.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width;
            c.OptionsColumn.AllowEdit = editable; c.OptionsColumn.ReadOnly = !editable;
        }

        // Customers without an email address get an amber Email cell — visible before sending,
        // and the Batch Mail dialog lets the user type one on the spot anyway.
        private void GridViewInv_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "Email")
            {
                object v = GridViewInv.GetRowCellValue(e.RowHandle, "Email");
                string s = v == null || v == DBNull.Value ? "" : v.ToString().Trim();
                if (s.Length == 0)
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 236, 179);
                    e.Appearance.ForeColor = System.Drawing.Color.Black;
                }
                return;
            }
            // #9b checklist: a green Emailed cell = this invoice already went out at least once.
            if (e.Column.FieldName == "EmailedAt")
            {
                object v = GridViewInv.GetRowCellValue(e.RowHandle, "EmailedAt");
                if (v != null && v != DBNull.Value)
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(212, 239, 212);
                    e.Appearance.ForeColor = System.Drawing.Color.Black;
                }
            }
        }

        private void FillEmailColumn()
        {
            if (_dt == null) return;
            bool stmt = CmbEmailSource.SelectedIndex == 1;
            foreach (DataRow r in _dt.Rows)
                r["Email"] = stmt ? Convert.ToString(r["StatementEmail"]) : Convert.ToString(r["EmailAddress"]);
        }

        private void CmbEmailSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillEmailColumn();
            GridInv.RefreshDataSource();
        }

        /// <summary>
        /// Fix a customer's email without leaving this screen. The Batch Mail dialog can already
        /// override an address for ONE send; this writes it back to the customer so the next run
        /// (and the SOA, and everything else) has it too — which is what you actually want when the
        /// cell is blank because nobody ever filled it in.
        ///
        /// Whether it edits the Debtor's Email Address or the Statement Email follows the
        /// "Email to:" selector, so the field you are looking at is the field you change.
        /// </summary>
        private void BtnChangeEmail_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            int rh = GridViewInv.FocusedRowHandle;
            DataRow r = rh < 0 ? null : GridViewInv.GetDataRow(rh);
            if (r == null)
            {
                XtraMessageBox.Show("Pick the invoice row of the customer whose email you want to change.",
                    "Change Email", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool stmt = CmbEmailSource.SelectedIndex == 1;
            string field = stmt ? "StatementEmail" : "EmailAddress";
            string fieldLabel = stmt ? "Statement Email" : "Email Address";
            string accNo = Convert.ToString(r["DebtorCode"]).Trim();
            string company = Convert.ToString(r["DebtorName"]).Trim();
            string current = Convert.ToString(r[field]).Trim();

            string entered;
            if (!PromptForEmail(accNo, company, fieldLabel, current, out entered)) return;
            if (entered == current) return;

            try
            {
                using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE dbo.Debtor SET " + field + " = @v WHERE AccNo = @a", cn))
                    {
                        cmd.Parameters.AddWithValue("@v", entered);
                        cmd.Parameters.AddWithValue("@a", accNo);
                        cmd.ExecuteNonQuery();
                    }
                    // AutoCount caches the debtor list and only re-reads it when this counter moves.
                    // Without the bump, its own screens keep showing the OLD address indefinitely —
                    // a restart does not help, because the cache is keyed to the counter, not the run.
                    using (SqlCommand bump = new SqlCommand(
                        "UPDATE dbo.ChangeCount SET Counter = Counter + 1 WHERE TableName = 'Debtor'", cn))
                    {
                        bump.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not save the email:\r\n" + ex.Message,
                    "Change Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Every row of this customer shows the same address — update them all, not just the
            // focused one, or the grid contradicts itself.
            foreach (DataRow x in _dt.Rows)
                if (string.Equals(Convert.ToString(x["DebtorCode"]).Trim(), accNo, StringComparison.OrdinalIgnoreCase))
                    x[field] = entered;
            FillEmailColumn();
            GridInv.RefreshDataSource();
        }

        /// <summary>Small prompt for one email address. Returns false when cancelled.</summary>
        private bool PromptForEmail(string accNo, string company, string fieldLabel, string current, out string entered)
        {
            entered = "";
            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = "Change Email";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new System.Drawing.Size(460, 152);
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;

                LabelControl who = new LabelControl();
                who.Text = accNo + "   " + company;
                who.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                who.Appearance.Options.UseFont = true;
                who.AutoSizeMode = LabelAutoSizeMode.None;
                who.Location = new System.Drawing.Point(14, 14);
                who.Size = new System.Drawing.Size(430, 18);
                dlg.Controls.Add(who);

                LabelControl lbl = new LabelControl();
                lbl.Text = fieldLabel + ":";
                lbl.Location = new System.Drawing.Point(14, 46);
                dlg.Controls.Add(lbl);

                TextEdit txt = new TextEdit();
                txt.Location = new System.Drawing.Point(14, 64);
                txt.Size = new System.Drawing.Size(430, 22);
                txt.Text = current;
                txt.Properties.NullValuePrompt = "name@company.com";
                txt.Properties.NullValuePromptShowForEmptyValue = true;
                dlg.Controls.Add(txt);

                LabelControl hint = new LabelControl();
                hint.Text = "Saved onto the customer, so the next run and the SOA use it too.";
                hint.Appearance.ForeColor = System.Drawing.Color.FromArgb(110, 110, 110);
                hint.Appearance.Options.UseForeColor = true;
                hint.Location = new System.Drawing.Point(14, 92);
                dlg.Controls.Add(hint);

                SimpleButton ok = new SimpleButton();
                ok.Text = "Save"; ok.Size = new System.Drawing.Size(90, 28);
                ok.Location = new System.Drawing.Point(264, 114);
                dlg.Controls.Add(ok);

                SimpleButton cancel = new SimpleButton();
                cancel.Text = "Cancel"; cancel.Size = new System.Drawing.Size(90, 28);
                cancel.Location = new System.Drawing.Point(358, 114);
                cancel.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(cancel);
                dlg.CancelButton = cancel;

                string captured = null;
                ok.Click += new EventHandler(delegate
                {
                    string v = (txt.Text ?? "").Trim();
                    // Clearing it on purpose is allowed; anything else must at least look like an address.
                    if (v.Length > 0 && (v.IndexOf('@') <= 0 || v.IndexOf('.', v.IndexOf('@')) < 0))
                    {
                        XtraMessageBox.Show("That does not look like an email address.",
                            "Change Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    captured = v;
                    dlg.DialogResult = DialogResult.OK;
                });

                if (dlg.ShowDialog(this) != DialogResult.OK || captured == null) return false;
                entered = captured;
                return true;
            }
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            if (_dt == null) return;
            GridViewInv.CloseEditor();
            // Toggle: all ticked -> untick all; otherwise tick every row on the CURRENT filter view.
            bool allTicked = true;
            for (int i = 0; i < GridViewInv.RowCount; i++)
            {
                DataRow r = GridViewInv.GetDataRow(i);
                if (r != null && !(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) { allTicked = false; break; }
            }
            for (int i = 0; i < GridViewInv.RowCount; i++)
            {
                DataRow r = GridViewInv.GetDataRow(i);
                if (r != null) r["Sel"] = !allTicked;
            }
            GridInv.RefreshDataSource();
            UpdateCount();
        }

        private void UpdateCount()
        {
            if (_dt == null) { LblCount.Text = "0 invoice(s)"; return; }
            int total = _dt.Rows.Count, ticked = 0;
            foreach (DataRow r in _dt.Rows)
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) ticked++;
            LblCount.Text = ticked + " of " + total + " invoice(s) selected";
        }

        // ───────────────────────────── send ─────────────────────────────

        private void BtnEmail_Click(object sender, EventArgs e)
        {
            if (_dt == null) return;
            GridViewInv.CloseEditor();
            GridViewInv.UpdateCurrentRow();
            if (_userSession == null)
            {
                XtraMessageBox.Show("Open this screen from the Service & Contract menu to email invoices.",
                    "Bulk Email Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Dictionary<string, InvoiceBatchMailEntity> byDebtor = new Dictionary<string, InvoiceBatchMailEntity>(StringComparer.OrdinalIgnoreCase);
            List<InvoiceBatchMailEntity> entities = new List<InvoiceBatchMailEntity>();
            List<DataRow> ticked = new List<DataRow>();
            foreach (DataRow r in _dt.Rows)
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) ticked.Add(r);
            if (ticked.Count == 0)
            {
                XtraMessageBox.Show("Tick at least one invoice to email.", "Bulk Email Invoice");
                return;
            }

            // Group the ticked rows into one email per customer. NOTHING is rendered here — the
            // form used to build every PDF before the operator got control back, which froze it for
            // the whole batch. The job renders each customer's invoices as it reaches them.
            foreach (DataRow r in ticked)
            {
                string acc = Convert.ToString(r["DebtorCode"]);
                InvoiceBatchMailEntity ent;
                if (!byDebtor.TryGetValue(acc, out ent))
                {
                    ent = new InvoiceBatchMailEntity();
                    ent.AccNo = acc;
                    ent.CompanyName = Convert.ToString(r["DebtorName"]);
                    ent.RecipientName = ent.CompanyName;
                    ent.Email = Convert.ToString(r["Email"]).Trim();
                    ent.DocNos = "";
                    byDebtor[acc] = ent;
                    entities.Add(ent);
                }
                string docNo = Convert.ToString(r["DocNo"]);
                ent.DocNos = ent.DocNos.Length == 0 ? docNo : ent.DocNos + ", " + docNo;
                ent.DocKeyList.Add(Convert.ToInt64(r["DocKey"]));
                ent.DocNoList.Add(docNo);
            }

            // Resolving WHICH layout each invoice needs still touches AutoCount's report registry,
            // so it stays on this thread — but that is a handful of designs, not one pass per invoice.
            List<long> allDocKeys = new List<long>();
            foreach (DataRow r in ticked) allDocKeys.Add(Convert.ToInt64(r["DocKey"]));

            ScpInvoicePdfRenderer renderer = new ScpInvoicePdfRenderer(_dbSetting, _userSession);
            string prepErr;
            bool prepared;
            Cursor.Current = Cursors.WaitCursor;
            try { prepared = renderer.Prepare(allDocKeys, out prepErr); }
            finally { Cursor.Current = Cursors.Default; }
            if (!prepared)
            {
                XtraMessageBox.Show(
                    "The invoices cannot be rendered:\r\n\r\n" + prepErr + "\r\n\r\n" +
                    "Create an 'Invoice Document' layout in the Report Designer, then set it on the " +
                    "contract (Maintain Service Contract > Invoice Template).",
                    "Bulk Email Invoice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // A contract naming a layout that no longer exists is a MISTAKE — worth stopping for,
            // because the customer would silently receive the wrong-looking invoice. Everything else
            // about layouts is shown per invoice in the confirmation grid's Layout column; a popup
            // that just reports the normal case is noise.
            if (renderer.MissingTemplates.Count > 0)
                XtraMessageBox.Show(
                    "These contract invoice template(s) were NOT found (renamed or deleted in the " +
                    "Report Designer?) - the DEFAULT layout will be used for their invoices:\r\n\r\n" +
                    string.Join("\r\n", renderer.MissingTemplates.ToArray()) +
                    "\r\n\r\nFix the template name on the contract (Maintain Service Contract).",
                    "Bulk Email Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // Sender: the From configured in EMAIL SETTING is the authority. Email Setting keeps it
            // on THIS PC (%APPDATA%\AutoCount\...\MailServer.setting), separate from the SMTP host
            // and user which live in the book - so the local pair wins: it is what the operator
            // actually typed, and what AutoCount's own Batch Mail shows.
            string fromName = "", fromEmail = "";
            try
            {
                AutoCount.Settings.MailServerSetting ms = AutoCount.Settings.MailServerSetting.GetOrCreate(_dbSetting);
                if (ms != null)
                {
                    fromName = (ms.LocalPCFromName ?? "").Trim();
                    fromEmail = (ms.LocalPCFromEmail ?? "").Trim();
                    if (fromEmail.Length == 0) fromEmail = (ms.FromEmail ?? "").Trim();
                    if (fromName.Length == 0) fromName = (ms.FromName ?? "").Trim();
                }
            }
            catch { }
            if (fromEmail.Length == 0)
            {
                try
                {
                    DataTable p = _dbSetting.GetDataTable(
                        "SELECT CompanyName, ISNULL(EmailAddress,'') AS EmailAddress FROM dbo.Profile", false);
                    if (p.Rows.Count > 0)
                    {
                        if (fromName.Length == 0) fromName = Convert.ToString(p.Rows[0]["CompanyName"]);
                        fromEmail = Convert.ToString(p.Rows[0]["EmailAddress"]).Trim();
                    }
                }
                catch { }
            }
            if (fromEmail.Length == 0)
            {
                XtraMessageBox.Show(
                    "There is no sender address to send FROM.\r\n\r\n" +
                    "Set From Email in Email Setting (the button on this screen) - that is the address " +
                    "your customers will see the invoice come from.\r\n\r\n" +
                    "This is not the customer's address; that one is already on the grid.",
                    "Bulk Email Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ScpEmailTemplates.Template tplDef = ScpEmailTemplates.LoadDefault(_dbSetting);
            LoadPerDebtorTemplates();

            List<ScpEmailJob.Recipient> recipients = new List<ScpEmailJob.Recipient>();
            foreach (InvoiceBatchMailEntity ent in entities)
            {
                ScpEmailJob.Recipient rec = new ScpEmailJob.Recipient();
                rec.DebtorCode = ent.AccNo;
                rec.DebtorName = ent.CompanyName;
                rec.Email = (ent.Email ?? "").Trim();
                rec.DocKeys = ent.DocKeyList;
                rec.DocNos = ent.DocNoList;
                ScpEmailTemplates.Template own;
                if (_perDebtorTpl != null &&
                    _perDebtorTpl.TryGetValue((ent.AccNo ?? "").Trim().ToUpperInvariant(), out own))
                    rec.Template = own;
                recipients.Add(rec);
            }

            using (BulkEmailJob_Form dlg = new BulkEmailJob_Form(
                _dbSetting, recipients, fromName, fromEmail, tplDef, renderer))
            {
                dlg.ShowDialog(this);
            }
            LoadData();   // refresh the Emailed column / checklist
        }


        /// <summary>Watch the current (or last) send, including one another PC started.</summary>
        private void BtnProgress_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            using (BulkEmailJob_Form dlg = new BulkEmailJob_Form(_dbSetting, 0))
            {
                dlg.ShowDialog(this);
            }
            LoadData();
        }

        // #9b: write one zSCP2_EmailLog row per invoice of every recipient the user really sent to
        // (FormBatchMail2 copies the grid's possibly-edited Email back onto the entity before it
        // dispatches, so ent.Email is the final address).
        private void LogEmailed(HashSet<InvoiceBatchMailEntity> sentEntities)
        {
            if (_dbSetting == null || sentEntities == null || sentEntities.Count == 0) return;
            try
            {
                string by = _userSession != null ? Convert.ToString(_userSession.LoginUserID) : "";
                using (SqlConnection conn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    conn.Open();
                    foreach (InvoiceBatchMailEntity ent in sentEntities)
                    {
                        string email = (ent.Email ?? "").Trim();
                        for (int i = 0; i < ent.DocKeyList.Count; i++)
                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO dbo.zSCP2_EmailLog (DocKey, DocNo, DebtorCode, Email, SentAt, SentBy) " +
                                "VALUES (@dk,@dn,@acc,@em,GETDATE(),@by)", conn))
                            {
                                cmd.Parameters.AddWithValue("@dk", ent.DocKeyList[i]);
                                cmd.Parameters.AddWithValue("@dn", ent.DocNoList[i]);
                                cmd.Parameters.AddWithValue("@acc", ent.AccNo ?? "");
                                cmd.Parameters.AddWithValue("@em", email);
                                cmd.Parameters.AddWithValue("@by", by);
                                cmd.ExecuteNonQuery();
                            }
                    }
                }
            }
            catch { /* the send already happened - history logging must never break it */ }
        }

        // Per-customer email wording (contract.EmailTemplateKey -> template), plus the batch default
        // we handed to the dialog so ConvertBatchMessage can tell "untouched" from "operator typed".
        private System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpEmailTemplates.Template> _perDebtorTpl;
        private string _tplDefaultSubject = "";
        private string _tplDefaultBody = "";
        private string _tplDefaultStyle = "PLAIN";

        /// <summary>Debtor -> the template their contract asks for. Debtors with no contract-level
        /// choice are simply absent, and fall through to the default.</summary>
        private void LoadPerDebtorTemplates()
        {
            _perDebtorTpl = new System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpEmailTemplates.Template>();
            try
            {
                System.Data.DataTable dt = _dbSetting.GetDataTable(
                    "SELECT DISTINCT c.DebtorCode, t.TemplateKey, t.Name, t.Subject, t.Body, t.Style " +
                    "FROM dbo.zSCP2_Contract c " +
                    "JOIN dbo.zSCP2_EmailTemplate t ON t.TemplateKey = c.EmailTemplateKey " +
                    "WHERE c.EmailTemplateKey IS NOT NULL AND ISNULL(c.Inactive,'N') <> 'Y'", false);
                foreach (System.Data.DataRow r in dt.Rows)
                {
                    string acc = Convert.ToString(r["DebtorCode"]).Trim().ToUpperInvariant();
                    if (acc.Length == 0 || _perDebtorTpl.ContainsKey(acc)) continue;   // first contract wins
                    _perDebtorTpl[acc] = ServiceContractPhotocopier.Classes.ScpEmailTemplates.FromRow(r);
                }
            }
            catch { }   // no per-customer wording is a downgrade, not a failure
        }

        private string _mailStyle = "PLAIN";    // template style: PLAIN / STYLED (frame) / HTML (user-authored)
        private string _mailSenderCompany = ""; // header-bar company for the styled frame

        // {token} substitution per recipient — mirrors the Debtor Statement's Batch Mail behaviour.
        // Also the SEND signal: FormBatchMail2 only calls this while dispatching (Send clicked).
        private void ConvertBatchMessage(BatchMail2Entity entity, ref string fromName, ref string subject, ref string message)
        {
            InvoiceBatchMailEntity ent = entity as InvoiceBatchMailEntity;
            if (ent == null) return;
            if (_sentEntities != null) _sentEntities.Add(ent);
            // Per-customer wording, but ONLY while the operator has left the batch text alone. If
            // they typed their own subject/body in the dialog, that is a deliberate one-off for this
            // send and it wins for everyone - silently replacing what they just wrote would be worse
            // than ignoring a stored preference.
            _mailStyle = _tplDefaultStyle;
            if (_perDebtorTpl != null && subject == _tplDefaultSubject && message == _tplDefaultBody)
            {
                ServiceContractPhotocopier.Classes.ScpEmailTemplates.Template own;
                if (_perDebtorTpl.TryGetValue((ent.AccNo ?? "").Trim().ToUpperInvariant(), out own) && own != null)
                {
                    subject = own.Subject;
                    message = own.Body;
                    _mailStyle = own.Style;
                }
            }
            fromName = ReplaceTokens(fromName, ent);
            subject = ReplaceTokens(subject, ent);
            message = ReplaceTokens(message, ent);
            if (_mailStyle == "STYLED")
                message = ServiceContractPhotocopier.Classes.ScpMailHtml.BuildStyled(message, _mailSenderCompany);
            else if (_mailStyle == "HTML")
                message = ServiceContractPhotocopier.Classes.ScpMailHtml.EnsureHtml(message);
        }

        private static string ReplaceTokens(string text, InvoiceBatchMailEntity ent)
        {
            if (text == null) return "";
            text = text.Replace("{AccNo}", ent.AccNo ?? "");
            text = text.Replace("{CompanyName}", ent.CompanyName ?? "");
            text = text.Replace("{DocNos}", ent.DocNos ?? "");
            return text;
        }

        private static string SafeFileName(string name)
        {
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in bad) name = name.Replace(c, '-');
            return name;
        }

        // AutoCount's own SMTP configuration dialog — the same one the Batch Mail window opens.
        private void BtnMailSetting_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            using (FormMailSetting frm = new FormMailSetting(_dbSetting))
            {
                frm.ShowDialog(this);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            ApplyFilterDefaults();
            if (_chkUnsentOnly != null) _chkUnsentOnly.Checked = false;
            GridViewInv.ClearColumnsFilter();
            LoadData();
        }
    }
}
