namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class InvoiceRun_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelView = new DevExpress.XtraEditors.PanelControl();
            this.LblView = new DevExpress.XtraEditors.LabelControl();
            this.BtnViewInvoices = new DevExpress.XtraEditors.CheckButton();
            this.BtnViewMeters = new DevExpress.XtraEditors.CheckButton();
            this.LblViewHint = new DevExpress.XtraEditors.LabelControl();
            this.PanelMeters = new DevExpress.XtraEditors.PanelControl();
            this.PanelFilter = new DevExpress.XtraEditors.PanelControl();
            this.GrpFilter = new DevExpress.XtraEditors.GroupControl();
            this.LblSearch = new DevExpress.XtraEditors.LabelControl();
            this.TxtSearch = new DevExpress.XtraEditors.TextEdit();
            this.LblMonth = new DevExpress.XtraEditors.LabelControl();
            this.CmbMonth = new DevExpress.XtraEditors.ComboBoxEdit();
            this.CmbYear = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblDay = new DevExpress.XtraEditors.LabelControl();
            this.PanelDays = new DevExpress.XtraEditors.PanelControl();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnFetch = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateReady = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateSelected = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSetting = new DevExpress.XtraEditors.SimpleButton();
            this.LblShow = new DevExpress.XtraEditors.LabelControl();
            this.ChipAll = new DevExpress.XtraEditors.CheckButton();
            this.ChipReady = new DevExpress.XtraEditors.CheckButton();
            this.ChipWaiting = new DevExpress.XtraEditors.CheckButton();
            this.ChipInvoiced = new DevExpress.XtraEditors.CheckButton();
            this.LblKind = new DevExpress.XtraEditors.LabelControl();
            this.ChipKindAll = new DevExpress.XtraEditors.CheckButton();
            this.ChipKindRental = new DevExpress.XtraEditors.CheckButton();
            this.ChipKindMeter = new DevExpress.XtraEditors.CheckButton();
            this.LblGrouping = new DevExpress.XtraEditors.LabelControl();
            this.SplitMain = new DevExpress.XtraEditors.SplitContainerControl();
            this.GridInvoices = new DevExpress.XtraGrid.GridControl();
            this.GridViewInvoices = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCustomer = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColContract = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColKind = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDetail = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColInvDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.RepoMoney = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelDetailHead = new DevExpress.XtraEditors.PanelControl();
            this.LblDetailTitle = new DevExpress.XtraEditors.LabelControl();
            this.LblDetailSub = new DevExpress.XtraEditors.LabelControl();
            this.LblFactMachines = new DevExpress.XtraEditors.LabelControl();
            this.LblFactReadings = new DevExpress.XtraEditors.LabelControl();
            this.LblFactMissing = new DevExpress.XtraEditors.LabelControl();
            this.LblFactAmount = new DevExpress.XtraEditors.LabelControl();
            this.ChkMissingOnly = new DevExpress.XtraEditors.CheckEdit();
            this.BtnPreview = new DevExpress.XtraEditors.SimpleButton();
            this.BtnHistory = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDeleteInvoice = new DevExpress.XtraEditors.SimpleButton();
            this.BtnTakeOver = new DevExpress.XtraEditors.SimpleButton();
            this.LblInvDate = new DevExpress.XtraEditors.LabelControl();
            this.DtInvDate = new DevExpress.XtraEditors.DateEdit();
            this.BtnInvDateFromReading = new DevExpress.XtraEditors.SimpleButton();
            this.LblInvRef = new DevExpress.XtraEditors.LabelControl();
            this.TxtInvRef = new DevExpress.XtraEditors.TextEdit();
            this.BtnOverdue = new DevExpress.XtraEditors.CheckButton();
            this.GridReadings = new DevExpress.XtraGrid.GridControl();
            this.GridViewReadings = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColRMachine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRMeter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLast = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRCurrent = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRUsage = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRSource = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRRef = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRMeterName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRMin = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRFoc = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRRebate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLastRead = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLastAudit = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLastFetch = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLastInv = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLastInvDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRInvTotal = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRMachineStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRInvoiced = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoRate = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoDate = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoDateEdit = new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();
            this.RepoRef = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoNum = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoReading = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoMoney2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelDetailFoot = new DevExpress.XtraEditors.PanelControl();
            this.LblDetailFoot = new DevExpress.XtraEditors.LabelControl();
            this.PanelStatus = new DevExpress.XtraEditors.PanelControl();
            this.LblStatus = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelView)).BeginInit();
            this.PanelView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbYear.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).BeginInit();
            this.SplitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridInvoices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailHead)).BeginInit();
            this.PanelDetailHead.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChkMissingOnly.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtInvDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtInvDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtInvRef.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoReading)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDateEdit.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDateEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRef)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailFoot)).BeginInit();
            this.PanelDetailFoot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).BeginInit();
            this.PanelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelHeaderTop  (native AutoCount green header; hint hidden in the ctor)
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Meter Invoice Run";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1280, 34);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelView  (Invoices <-> Meters: the same module, two ways of looking at the month)
            //
            this.PanelView.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.PanelView.Appearance.Options.UseBackColor = true;
            this.PanelView.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelView.Controls.Add(this.LblView);
            this.PanelView.Controls.Add(this.BtnViewInvoices);
            this.PanelView.Controls.Add(this.BtnViewMeters);
            this.PanelView.Controls.Add(this.LblViewHint);
            this.PanelView.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelView.Location = new System.Drawing.Point(0, 34);
            this.PanelView.Name = "PanelView";
            this.PanelView.Size = new System.Drawing.Size(1280, 42);
            this.PanelView.TabIndex = 1;
            //
            // LblView
            //
            this.LblView.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblView.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.LblView.Appearance.Options.UseFont = true;
            this.LblView.Appearance.Options.UseForeColor = true;
            this.LblView.Location = new System.Drawing.Point(12, 13);
            this.LblView.Name = "LblView";
            this.LblView.Size = new System.Drawing.Size(35, 15);
            this.LblView.TabIndex = 0;
            this.LblView.Text = "View:";
            //
            // BtnViewInvoices
            //
            this.BtnViewInvoices.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnViewInvoices.Appearance.Options.UseFont = true;
            this.BtnViewInvoices.Checked = true;
            this.BtnViewInvoices.GroupIndex = 3;
            this.BtnViewInvoices.Location = new System.Drawing.Point(56, 6);
            this.BtnViewInvoices.Name = "BtnViewInvoices";
            this.BtnViewInvoices.Size = new System.Drawing.Size(190, 30);
            this.BtnViewInvoices.TabIndex = 1;
            this.BtnViewInvoices.Text = "Invoices to generate";
            //
            // BtnViewMeters
            //
            this.BtnViewMeters.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnViewMeters.Appearance.Options.UseFont = true;
            this.BtnViewMeters.GroupIndex = 3;
            this.BtnViewMeters.Location = new System.Drawing.Point(250, 6);
            this.BtnViewMeters.Name = "BtnViewMeters";
            this.BtnViewMeters.Size = new System.Drawing.Size(230, 30);
            this.BtnViewMeters.TabIndex = 2;
            this.BtnViewMeters.TabStop = false;
            this.BtnViewMeters.Text = "Meters - fetch && key in";
            //
            // LblViewHint
            //
            this.LblViewHint.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblViewHint.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblViewHint.Appearance.Options.UseFont = true;
            this.LblViewHint.Appearance.Options.UseForeColor = true;
            this.LblViewHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblViewHint.Location = new System.Drawing.Point(496, 13);
            this.LblViewHint.Name = "LblViewHint";
            this.LblViewHint.Size = new System.Drawing.Size(770, 16);
            this.LblViewHint.TabIndex = 3;
            this.LblViewHint.Text = "Fetch or key in readings on the Meters view, then come back here to generate the invoices.";
            //
            // PanelMeters  (hosts the Meter Reading Integration screen, unchanged)
            //
            this.PanelMeters.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelMeters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PanelMeters.Location = new System.Drawing.Point(0, 76);
            this.PanelMeters.Name = "PanelMeters";
            this.PanelMeters.Size = new System.Drawing.Size(1280, 628);
            this.PanelMeters.TabIndex = 5;
            this.PanelMeters.Visible = false;
            //
            // PanelFilter
            //
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnRefresh);
            this.PanelFilter.Controls.Add(this.BtnFetch);
            this.PanelFilter.Controls.Add(this.BtnGenerateReady);
            this.PanelFilter.Controls.Add(this.BtnGenerateSelected);
            this.PanelFilter.Controls.Add(this.BtnSetting);
            this.PanelFilter.Controls.Add(this.LblShow);
            this.PanelFilter.Controls.Add(this.ChipAll);
            this.PanelFilter.Controls.Add(this.ChipReady);
            this.PanelFilter.Controls.Add(this.ChipWaiting);
            this.PanelFilter.Controls.Add(this.ChipInvoiced);
            this.PanelFilter.Controls.Add(this.LblKind);
            this.PanelFilter.Controls.Add(this.ChipKindAll);
            this.PanelFilter.Controls.Add(this.ChipKindRental);
            this.PanelFilter.Controls.Add(this.ChipKindMeter);
            this.PanelFilter.Controls.Add(this.LblGrouping);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 76);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1280, 164);
            this.PanelFilter.TabIndex = 1;
            //
            // GrpFilter
            //
            this.GrpFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GrpFilter.Appearance.Options.UseFont = true;
            this.GrpFilter.Controls.Add(this.LblSearch);
            this.GrpFilter.Controls.Add(this.TxtSearch);
            this.GrpFilter.Controls.Add(this.LblMonth);
            this.GrpFilter.Controls.Add(this.CmbMonth);
            this.GrpFilter.Controls.Add(this.CmbYear);
            this.GrpFilter.Controls.Add(this.LblDay);
            this.GrpFilter.Controls.Add(this.PanelDays);
            this.GrpFilter.Controls.Add(this.BtnOverdue);
            this.GrpFilter.Controls.Add(this.LblSummary);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(494, 150);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            //
            // LblSearch
            //
            this.LblSearch.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblSearch.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblSearch.Appearance.Options.UseFont = true;
            this.LblSearch.Appearance.Options.UseForeColor = true;
            this.LblSearch.Location = new System.Drawing.Point(12, 28);
            this.LblSearch.Name = "LblSearch";
            this.LblSearch.Size = new System.Drawing.Size(64, 15);
            this.LblSearch.TabIndex = 0;
            this.LblSearch.Text = "Search Any:";
            //
            // TxtSearch
            //
            this.TxtSearch.Location = new System.Drawing.Point(78, 25);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.TxtSearch.Properties.Appearance.Options.UseFont = true;
            this.TxtSearch.Properties.NullValuePrompt = "customer, contract, serial...";
            this.TxtSearch.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtSearch.Size = new System.Drawing.Size(240, 22);
            this.TxtSearch.TabIndex = 1;
            //
            // LblMonth
            //
            this.LblMonth.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblMonth.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblMonth.Appearance.Options.UseFont = true;
            this.LblMonth.Appearance.Options.UseForeColor = true;
            this.LblMonth.Location = new System.Drawing.Point(12, 58);
            this.LblMonth.Name = "LblMonth";
            this.LblMonth.Size = new System.Drawing.Size(41, 15);
            this.LblMonth.TabIndex = 2;
            this.LblMonth.Text = "Month:";
            //
            // CmbMonth
            //
            this.CmbMonth.Location = new System.Drawing.Point(78, 55);
            this.CmbMonth.Name = "CmbMonth";
            this.CmbMonth.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.CmbMonth.Properties.Appearance.Options.UseFont = true;
            this.CmbMonth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbMonth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbMonth.Size = new System.Drawing.Size(120, 22);
            this.CmbMonth.TabIndex = 3;
            //
            // CmbYear
            //
            this.CmbYear.Location = new System.Drawing.Point(204, 55);
            this.CmbYear.Name = "CmbYear";
            this.CmbYear.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.CmbYear.Properties.Appearance.Options.UseFont = true;
            this.CmbYear.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbYear.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbYear.Size = new System.Drawing.Size(70, 22);
            this.CmbYear.TabIndex = 4;
            //
            // LblDay
            //
            this.LblDay.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDay.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDay.Appearance.Options.UseFont = true;
            this.LblDay.Appearance.Options.UseForeColor = true;
            this.LblDay.Location = new System.Drawing.Point(12, 89);
            this.LblDay.Name = "LblDay";
            this.LblDay.Size = new System.Drawing.Size(28, 15);
            this.LblDay.TabIndex = 5;
            this.LblDay.Text = "Day:";
            //
            // PanelDays
            //
            this.PanelDays.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelDays.Location = new System.Drawing.Point(78, 85);
            this.PanelDays.Name = "PanelDays";
            this.PanelDays.Size = new System.Drawing.Size(404, 26);
            this.PanelDays.TabIndex = 6;
            //
            // BtnOverdue  (every billing date already gone by, this month and the months before it)
            //
            this.BtnOverdue.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnOverdue.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.BtnOverdue.Appearance.Options.UseFont = true;
            this.BtnOverdue.Appearance.Options.UseForeColor = true;
            this.BtnOverdue.Location = new System.Drawing.Point(78, 115);
            this.BtnOverdue.Name = "BtnOverdue";
            this.BtnOverdue.Size = new System.Drawing.Size(196, 26);
            this.BtnOverdue.TabIndex = 7;
            this.BtnOverdue.Text = "Show all overdue";
            //
            // LblSummary
            //
            this.LblSummary.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblSummary.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblSummary.Appearance.Options.UseFont = true;
            this.LblSummary.Appearance.Options.UseForeColor = true;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(12, 119);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(470, 20);
            this.LblSummary.TabIndex = 7;
            this.LblSummary.Text = "";
            //
            // BtnRefresh
            //
            this.BtnRefresh.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnRefresh.Appearance.Options.UseFont = true;
            this.BtnRefresh.Location = new System.Drawing.Point(510, 8);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(120, 50);
            this.BtnRefresh.TabIndex = 1;
            this.BtnRefresh.Text = "Refresh";
            //
            // BtnFetch  (runs the list screen's Fetch and shows it doing so)
            //
            this.BtnFetch.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnFetch.Appearance.Options.UseFont = true;
            this.BtnFetch.Location = new System.Drawing.Point(638, 8);
            this.BtnFetch.Name = "BtnFetch";
            this.BtnFetch.Size = new System.Drawing.Size(120, 50);
            this.BtnFetch.TabIndex = 9;
            this.BtnFetch.Text = "Fetch";
            //
            // BtnGenerateReady  (red bold, same style as Generate Invoice on the list screen)
            //
            this.BtnGenerateReady.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnGenerateReady.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnGenerateReady.Appearance.Options.UseFont = true;
            this.BtnGenerateReady.Appearance.Options.UseForeColor = true;
            this.BtnGenerateReady.Location = new System.Drawing.Point(766, 8);
            this.BtnGenerateReady.Name = "BtnGenerateReady";
            this.BtnGenerateReady.Size = new System.Drawing.Size(176, 50);
            this.BtnGenerateReady.TabIndex = 2;
            this.BtnGenerateReady.Text = "Generate all ready";
            //
            // BtnGenerateSelected
            //
            this.BtnGenerateSelected.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnGenerateSelected.Appearance.Options.UseFont = true;
            this.BtnGenerateSelected.Location = new System.Drawing.Point(950, 8);
            this.BtnGenerateSelected.Name = "BtnGenerateSelected";
            this.BtnGenerateSelected.Size = new System.Drawing.Size(176, 50);
            this.BtnGenerateSelected.TabIndex = 3;
            this.BtnGenerateSelected.Text = "Generate selected";
            //
            // BtnSetting
            //
            this.BtnSetting.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSetting.Appearance.Options.UseFont = true;
            this.BtnSetting.Location = new System.Drawing.Point(1134, 8);
            this.BtnSetting.Name = "BtnSetting";
            this.BtnSetting.Size = new System.Drawing.Size(120, 50);
            this.BtnSetting.TabIndex = 4;
            this.BtnSetting.Text = "Setting";
            //
            // LblShow
            //
            this.LblShow.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblShow.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblShow.Appearance.Options.UseFont = true;
            this.LblShow.Appearance.Options.UseForeColor = true;
            this.LblShow.Location = new System.Drawing.Point(510, 74);
            this.LblShow.Name = "LblShow";
            this.LblShow.Size = new System.Drawing.Size(36, 15);
            this.LblShow.TabIndex = 6;
            this.LblShow.Text = "Show:";
            //
            // ChipAll
            //
            this.ChipAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipAll.Appearance.Options.UseFont = true;
            this.ChipAll.Checked = true;
            this.ChipAll.GroupIndex = 1;
            this.ChipAll.Location = new System.Drawing.Point(556, 68);
            this.ChipAll.Name = "ChipAll";
            this.ChipAll.Size = new System.Drawing.Size(72, 28);
            this.ChipAll.TabIndex = 7;
            this.ChipAll.Text = "All";
            //
            // ChipReady
            //
            this.ChipReady.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipReady.Appearance.Options.UseFont = true;
            this.ChipReady.GroupIndex = 1;
            this.ChipReady.Location = new System.Drawing.Point(632, 68);
            this.ChipReady.Name = "ChipReady";
            this.ChipReady.Size = new System.Drawing.Size(92, 28);
            this.ChipReady.TabIndex = 8;
            this.ChipReady.TabStop = false;
            this.ChipReady.Text = "Ready";
            //
            // ChipWaiting
            //
            this.ChipWaiting.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipWaiting.Appearance.Options.UseFont = true;
            this.ChipWaiting.GroupIndex = 1;
            this.ChipWaiting.Location = new System.Drawing.Point(728, 68);
            this.ChipWaiting.Name = "ChipWaiting";
            this.ChipWaiting.Size = new System.Drawing.Size(100, 28);
            this.ChipWaiting.TabIndex = 9;
            this.ChipWaiting.TabStop = false;
            this.ChipWaiting.Text = "Waiting";
            //
            // ChipInvoiced
            //
            this.ChipInvoiced.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipInvoiced.Appearance.Options.UseFont = true;
            this.ChipInvoiced.GroupIndex = 1;
            this.ChipInvoiced.Location = new System.Drawing.Point(832, 68);
            this.ChipInvoiced.Name = "ChipInvoiced";
            this.ChipInvoiced.Size = new System.Drawing.Size(104, 28);
            this.ChipInvoiced.TabIndex = 10;
            this.ChipInvoiced.TabStop = false;
            this.ChipInvoiced.Text = "Invoiced";
            //
            // LblKind
            //
            this.LblKind.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblKind.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblKind.Appearance.Options.UseFont = true;
            this.LblKind.Appearance.Options.UseForeColor = true;
            this.LblKind.Location = new System.Drawing.Point(510, 110);
            this.LblKind.Name = "LblKind";
            this.LblKind.Size = new System.Drawing.Size(45, 15);
            this.LblKind.TabIndex = 11;
            this.LblKind.Text = "Invoice:";
            //
            // ChipKindAll
            //
            this.ChipKindAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipKindAll.Appearance.Options.UseFont = true;
            this.ChipKindAll.Checked = true;
            this.ChipKindAll.GroupIndex = 2;
            this.ChipKindAll.Location = new System.Drawing.Point(556, 104);
            this.ChipKindAll.Name = "ChipKindAll";
            this.ChipKindAll.Size = new System.Drawing.Size(120, 28);
            this.ChipKindAll.TabIndex = 12;
            this.ChipKindAll.Text = "Rental + Meter";
            //
            // ChipKindRental
            //
            this.ChipKindRental.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipKindRental.Appearance.Options.UseFont = true;
            this.ChipKindRental.GroupIndex = 2;
            this.ChipKindRental.Location = new System.Drawing.Point(680, 104);
            this.ChipKindRental.Name = "ChipKindRental";
            this.ChipKindRental.Size = new System.Drawing.Size(100, 28);
            this.ChipKindRental.TabIndex = 13;
            this.ChipKindRental.TabStop = false;
            this.ChipKindRental.Text = "Rental only";
            //
            // ChipKindMeter
            //
            this.ChipKindMeter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipKindMeter.Appearance.Options.UseFont = true;
            this.ChipKindMeter.GroupIndex = 2;
            this.ChipKindMeter.Location = new System.Drawing.Point(784, 104);
            this.ChipKindMeter.Name = "ChipKindMeter";
            this.ChipKindMeter.Size = new System.Drawing.Size(100, 28);
            this.ChipKindMeter.TabIndex = 14;
            this.ChipKindMeter.TabStop = false;
            this.ChipKindMeter.Text = "Meter only";
            //
            // LblGrouping
            //
            this.LblGrouping.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblGrouping.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblGrouping.Appearance.Options.UseFont = true;
            this.LblGrouping.Appearance.Options.UseForeColor = true;
            this.LblGrouping.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblGrouping.Location = new System.Drawing.Point(510, 140);
            this.LblGrouping.Name = "LblGrouping";
            this.LblGrouping.Size = new System.Drawing.Size(760, 18);
            this.LblGrouping.TabIndex = 15;
            this.LblGrouping.Text = "";
            //
            // SplitMain
            //
            this.SplitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitMain.Location = new System.Drawing.Point(0, 198);
            this.SplitMain.Name = "SplitMain";
            this.SplitMain.Panel1.Controls.Add(this.GridInvoices);
            this.SplitMain.Panel1.Text = "Panel1";
            this.SplitMain.Panel2.Controls.Add(this.GridReadings);
            this.SplitMain.Panel2.Controls.Add(this.PanelDetailHead);
            this.SplitMain.Panel2.Controls.Add(this.PanelDetailFoot);
            this.SplitMain.Panel2.Text = "Panel2";
            this.SplitMain.Size = new System.Drawing.Size(1280, 506);
            this.SplitMain.SplitterPosition = 760;
            this.SplitMain.TabIndex = 2;
            //
            // GridInvoices
            //
            this.GridInvoices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridInvoices.Location = new System.Drawing.Point(0, 0);
            this.GridInvoices.MainView = this.GridViewInvoices;
            this.GridInvoices.Name = "GridInvoices";
            this.GridInvoices.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel,
            this.RepoMoney});
            this.GridInvoices.Size = new System.Drawing.Size(760, 506);
            this.GridInvoices.TabIndex = 0;
            this.GridInvoices.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewInvoices});
            //
            // GridViewInvoices
            //
            this.GridViewInvoices.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewInvoices.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewInvoices.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewInvoices.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewInvoices.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewInvoices.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewInvoices.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewInvoices.Appearance.Row.Options.UseFont = true;
            this.GridViewInvoices.Appearance.GroupRow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewInvoices.Appearance.GroupRow.Options.UseFont = true;
            this.GridViewInvoices.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewInvoices.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewInvoices.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewInvoices.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewInvoices.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewInvoices.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewInvoices.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewInvoices.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewInvoices.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewInvoices.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewInvoices.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColSel,
            this.ColCustomer,
            this.ColContract,
            this.ColKind,
            this.ColDetail,
            this.ColMachines,
            this.ColReadings,
            this.ColAmount,
            this.ColStatus,
            this.ColDue,
            this.ColInvDate});
            this.GridViewInvoices.ColumnPanelRowHeight = 28;
            this.GridViewInvoices.GridControl = this.GridInvoices;
            this.GridViewInvoices.GroupFormat = "{1}";
            this.GridViewInvoices.Name = "GridViewInvoices";
            this.GridViewInvoices.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewInvoices.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewInvoices.OptionsView.ColumnAutoWidth = true;
            this.GridViewInvoices.OptionsView.ShowGroupPanel = false;
            this.GridViewInvoices.OptionsView.ShowIndicator = false;
            this.GridViewInvoices.OptionsView.ShowViewCaption = true;
            this.GridViewInvoices.RowHeight = 24;
            this.GridViewInvoices.ViewCaption = "Invoices";
            //
            // ColSel
            //
            this.ColSel.Caption = " ";
            this.ColSel.ColumnEdit = this.RepoSel;
            this.ColSel.FieldName = "Sel";
            this.ColSel.MaxWidth = 36;
            this.ColSel.Name = "ColSel";
            this.ColSel.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.ColSel.OptionsFilter.AllowFilter = false;
            this.ColSel.Visible = true;
            this.ColSel.VisibleIndex = 0;
            this.ColSel.Width = 36;
            //
            // ColCustomer
            //
            this.ColCustomer.Caption = "Customer";
            this.ColCustomer.FieldName = "Customer";
            this.ColCustomer.GroupIndex = 0;
            this.ColCustomer.Name = "ColCustomer";
            this.ColCustomer.OptionsColumn.AllowEdit = false;
            this.ColCustomer.Visible = true;
            this.ColCustomer.VisibleIndex = 1;
            this.ColCustomer.Width = 200;
            //
            // ColContract
            //
            this.ColContract.Caption = "Contract";
            this.ColContract.FieldName = "ContractNo";
            this.ColContract.Name = "ColContract";
            this.ColContract.OptionsColumn.AllowEdit = false;
            this.ColContract.Visible = true;
            this.ColContract.VisibleIndex = 2;
            this.ColContract.Width = 110;
            //
            // ColKind
            //
            this.ColKind.Caption = "Invoice";
            this.ColKind.FieldName = "Kind";
            this.ColKind.Name = "ColKind";
            this.ColKind.OptionsColumn.AllowEdit = false;
            this.ColKind.Visible = true;
            this.ColKind.VisibleIndex = 3;
            this.ColKind.Width = 110;
            //
            // ColDetail
            //
            this.ColDetail.Caption = " ";
            this.ColDetail.FieldName = "Detail";
            this.ColDetail.Name = "ColDetail";
            this.ColDetail.OptionsColumn.AllowEdit = false;
            this.ColDetail.Visible = true;
            this.ColDetail.VisibleIndex = 4;
            this.ColDetail.Width = 150;
            //
            // ColDue  (which month the invoice is for; only the overdue view shows it, because a
            //          day view is one date from top to bottom)
            //
            this.ColDue.Caption = "Month";
            this.ColDue.DisplayFormat.FormatString = "MMM yyyy";
            this.ColDue.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColDue.FieldName = "Due";
            this.ColDue.Name = "ColDue";
            this.ColDue.OptionsColumn.AllowEdit = false;
            this.ColDue.Visible = false;
            this.ColDue.Width = 90;
            //
            // ColMachines
            //
            this.ColMachines.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColMachines.AppearanceCell.Options.UseTextOptions = true;
            this.ColMachines.Caption = "Machines";
            this.ColMachines.FieldName = "Machines";
            this.ColMachines.Name = "ColMachines";
            this.ColMachines.OptionsColumn.AllowEdit = false;
            this.ColMachines.Visible = true;
            this.ColMachines.VisibleIndex = 5;
            this.ColMachines.Width = 70;
            //
            // ColReadings
            //
            this.ColReadings.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColReadings.AppearanceCell.Options.UseTextOptions = true;
            this.ColReadings.Caption = "Readings";
            this.ColReadings.FieldName = "Readings";
            this.ColReadings.Name = "ColReadings";
            this.ColReadings.OptionsColumn.AllowEdit = false;
            this.ColReadings.Visible = true;
            this.ColReadings.VisibleIndex = 6;
            this.ColReadings.Width = 80;
            //
            // ColAmount
            //
            this.ColAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColAmount.Caption = "Amount (RM)";
            this.ColAmount.ColumnEdit = this.RepoMoney;
            this.ColAmount.FieldName = "Amount";
            this.ColAmount.Name = "ColAmount";
            this.ColAmount.OptionsColumn.AllowEdit = false;
            this.ColAmount.Visible = true;
            this.ColAmount.VisibleIndex = 7;
            this.ColAmount.Width = 100;
            //
            // ColStatus
            //
            this.ColStatus.Caption = "Status";
            this.ColStatus.FieldName = "StatusText";
            this.ColStatus.Name = "ColStatus";
            this.ColStatus.OptionsColumn.AllowEdit = false;
            this.ColStatus.Visible = true;
            this.ColStatus.VisibleIndex = 8;
            this.ColStatus.Width = 170;
            //
            // ColInvDate  (the date the invoice will carry -- the due date unless the operator moved it)
            //
            this.ColInvDate.Caption = "Invoice Date";
            this.ColInvDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColInvDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColInvDate.FieldName = "InvDate";
            this.ColInvDate.Name = "ColInvDate";
            this.ColInvDate.OptionsColumn.AllowEdit = false;
            this.ColInvDate.Visible = true;
            this.ColInvDate.VisibleIndex = 9;
            this.ColInvDate.Width = 90;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // RepoMoney
            //
            this.RepoMoney.AutoHeight = false;
            this.RepoMoney.Mask.EditMask = "n2";
            this.RepoMoney.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoMoney.Mask.UseMaskAsDisplayFormat = true;
            this.RepoMoney.Name = "RepoMoney";
            //
            // PanelDetailHead
            //
            this.PanelDetailHead.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelDetailHead.Appearance.Options.UseBackColor = true;
            this.PanelDetailHead.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelDetailHead.Controls.Add(this.LblDetailTitle);
            this.PanelDetailHead.Controls.Add(this.LblDetailSub);
            this.PanelDetailHead.Controls.Add(this.LblFactMachines);
            this.PanelDetailHead.Controls.Add(this.LblFactReadings);
            this.PanelDetailHead.Controls.Add(this.LblFactMissing);
            this.PanelDetailHead.Controls.Add(this.LblFactAmount);
            this.PanelDetailHead.Controls.Add(this.ChkMissingOnly);
            this.PanelDetailHead.Controls.Add(this.BtnPreview);
            this.PanelDetailHead.Controls.Add(this.BtnHistory);
            this.PanelDetailHead.Controls.Add(this.BtnDeleteInvoice);
            this.PanelDetailHead.Controls.Add(this.BtnTakeOver);
            this.PanelDetailHead.Controls.Add(this.LblInvDate);
            this.PanelDetailHead.Controls.Add(this.DtInvDate);
            this.PanelDetailHead.Controls.Add(this.BtnInvDateFromReading);
            this.PanelDetailHead.Controls.Add(this.LblInvRef);
            this.PanelDetailHead.Controls.Add(this.TxtInvRef);
            this.PanelDetailHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelDetailHead.Location = new System.Drawing.Point(0, 0);
            this.PanelDetailHead.Name = "PanelDetailHead";
            this.PanelDetailHead.Size = new System.Drawing.Size(510, 208);
            this.PanelDetailHead.TabIndex = 0;
            //
            // LblDetailTitle
            //
            this.LblDetailTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblDetailTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblDetailTitle.Appearance.Options.UseFont = true;
            this.LblDetailTitle.Appearance.Options.UseForeColor = true;
            this.LblDetailTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblDetailTitle.Location = new System.Drawing.Point(12, 8);
            this.LblDetailTitle.Name = "LblDetailTitle";
            this.LblDetailTitle.Size = new System.Drawing.Size(330, 24);
            this.LblDetailTitle.TabIndex = 0;
            this.LblDetailTitle.Text = "Pick an invoice on the left";
            //
            // LblDetailSub
            //
            this.LblDetailSub.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDetailSub.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDetailSub.Appearance.Options.UseFont = true;
            this.LblDetailSub.Appearance.Options.UseForeColor = true;
            this.LblDetailSub.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblDetailSub.Location = new System.Drawing.Point(12, 34);
            this.LblDetailSub.Name = "LblDetailSub";
            this.LblDetailSub.Size = new System.Drawing.Size(486, 16);
            this.LblDetailSub.TabIndex = 1;
            this.LblDetailSub.Text = "";
            //
            // LblFactMachines
            //
            this.LblFactMachines.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFactMachines.Appearance.Options.UseFont = true;
            this.LblFactMachines.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFactMachines.Location = new System.Drawing.Point(12, 58);
            this.LblFactMachines.Name = "LblFactMachines";
            this.LblFactMachines.Size = new System.Drawing.Size(110, 16);
            this.LblFactMachines.TabIndex = 2;
            this.LblFactMachines.Text = "";
            //
            // LblFactReadings
            //
            this.LblFactReadings.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFactReadings.Appearance.Options.UseFont = true;
            this.LblFactReadings.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFactReadings.Location = new System.Drawing.Point(128, 58);
            this.LblFactReadings.Name = "LblFactReadings";
            this.LblFactReadings.Size = new System.Drawing.Size(124, 16);
            this.LblFactReadings.TabIndex = 3;
            this.LblFactReadings.Text = "";
            //
            // LblFactMissing
            //
            this.LblFactMissing.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblFactMissing.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.LblFactMissing.Appearance.Options.UseFont = true;
            this.LblFactMissing.Appearance.Options.UseForeColor = true;
            this.LblFactMissing.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFactMissing.Location = new System.Drawing.Point(258, 58);
            this.LblFactMissing.Name = "LblFactMissing";
            this.LblFactMissing.Size = new System.Drawing.Size(110, 16);
            this.LblFactMissing.TabIndex = 4;
            this.LblFactMissing.Text = "";
            //
            // LblFactAmount
            //
            this.LblFactAmount.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblFactAmount.Appearance.Options.UseFont = true;
            this.LblFactAmount.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFactAmount.Location = new System.Drawing.Point(374, 58);
            this.LblFactAmount.Name = "LblFactAmount";
            this.LblFactAmount.Size = new System.Drawing.Size(124, 16);
            this.LblFactAmount.TabIndex = 5;
            this.LblFactAmount.Text = "";
            //
            // ChkMissingOnly
            //
            this.ChkMissingOnly.Location = new System.Drawing.Point(10, 96);
            this.ChkMissingOnly.Name = "ChkMissingOnly";
            this.ChkMissingOnly.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkMissingOnly.Properties.Appearance.Options.UseFont = true;
            this.ChkMissingOnly.Properties.Caption = "Show only the readings still missing";
            this.ChkMissingOnly.Size = new System.Drawing.Size(250, 20);
            this.ChkMissingOnly.TabIndex = 6;
            //
            // BtnPreview
            //
            this.BtnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnPreview.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.BtnPreview.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnPreview.Appearance.Options.UseFont = true;
            this.BtnPreview.Appearance.Options.UseForeColor = true;
            this.BtnPreview.Location = new System.Drawing.Point(300, 82);
            this.BtnPreview.Name = "BtnPreview";
            this.BtnPreview.Size = new System.Drawing.Size(198, 50);
            this.BtnPreview.TabIndex = 7;
            this.BtnPreview.Text = "Generate this invoice";
            //
            // BtnHistory
            //
            this.BtnHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnHistory.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnHistory.Appearance.Options.UseFont = true;
            this.BtnHistory.Location = new System.Drawing.Point(346, 8);
            this.BtnHistory.Name = "BtnHistory";
            this.BtnHistory.Size = new System.Drawing.Size(152, 28);
            this.BtnHistory.TabIndex = 8;
            this.BtnHistory.Text = "Reading History";
            //
            // BtnDeleteInvoice  (only alive on an invoice that has been made; Del does the same)
            //
            this.BtnDeleteInvoice.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnDeleteInvoice.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnDeleteInvoice.Appearance.Options.UseFont = true;
            this.BtnDeleteInvoice.Appearance.Options.UseForeColor = true;
            this.BtnDeleteInvoice.Location = new System.Drawing.Point(12, 118);
            this.BtnDeleteInvoice.Name = "BtnDeleteInvoice";
            this.BtnDeleteInvoice.Size = new System.Drawing.Size(160, 26);
            this.BtnDeleteInvoice.TabIndex = 9;
            this.BtnDeleteInvoice.Text = "Delete this invoice";
            //
            // BtnTakeOver  (the machine's reading becomes the operator's, number and date kept)
            //
            this.BtnTakeOver.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnTakeOver.Appearance.Options.UseFont = true;
            this.BtnTakeOver.Location = new System.Drawing.Point(180, 118);
            this.BtnTakeOver.Name = "BtnTakeOver";
            this.BtnTakeOver.Size = new System.Drawing.Size(112, 26);
            this.BtnTakeOver.TabIndex = 10;
            this.BtnTakeOver.Text = "Key in myself";
            //
            // LblInvDate
            //
            this.LblInvDate.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblInvDate.Appearance.Options.UseFont = true;
            this.LblInvDate.Location = new System.Drawing.Point(12, 156);
            this.LblInvDate.Name = "LblInvDate";
            this.LblInvDate.Size = new System.Drawing.Size(66, 15);
            this.LblInvDate.TabIndex = 11;
            this.LblInvDate.Text = "Invoice date";
            //
            // DtInvDate  (the due date by default; the operator may move it within the same month)
            //
            this.DtInvDate.EditValue = null;
            this.DtInvDate.Location = new System.Drawing.Point(86, 153);
            this.DtInvDate.Name = "DtInvDate";
            this.DtInvDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtInvDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtInvDate.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtInvDate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtInvDate.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtInvDate.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtInvDate.Properties.Mask.EditMask = "dd/MM/yyyy";
            this.DtInvDate.Properties.Mask.UseMaskAsDisplayFormat = true;
            this.DtInvDate.Size = new System.Drawing.Size(110, 20);
            this.DtInvDate.TabIndex = 12;
            //
            // BtnInvDateFromReading  (a machine read for the last time on the 14th is billed on the 14th)
            //
            this.BtnInvDateFromReading.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnInvDateFromReading.Appearance.Options.UseFont = true;
            this.BtnInvDateFromReading.Location = new System.Drawing.Point(202, 151);
            this.BtnInvDateFromReading.Name = "BtnInvDateFromReading";
            this.BtnInvDateFromReading.Size = new System.Drawing.Size(150, 24);
            this.BtnInvDateFromReading.TabIndex = 13;
            this.BtnInvDateFromReading.Text = "Use last reading date";
            //
            // LblInvRef  (feedback ATP-8: the invoice's Reference No, typed once for the whole invoice)
            //
            this.LblInvRef.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblInvRef.Appearance.Options.UseFont = true;
            this.LblInvRef.Location = new System.Drawing.Point(12, 182);
            this.LblInvRef.Name = "LblInvRef";
            this.LblInvRef.Size = new System.Drawing.Size(71, 15);
            this.LblInvRef.TabIndex = 14;
            this.LblInvRef.Text = "Reference No";
            //
            // TxtInvRef
            //
            this.TxtInvRef.Location = new System.Drawing.Point(86, 179);
            this.TxtInvRef.Name = "TxtInvRef";
            this.TxtInvRef.Properties.MaxLength = 30;
            this.TxtInvRef.Properties.NullValuePrompt = "empty = PUMS report no. or contract no.";
            this.TxtInvRef.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtInvRef.Size = new System.Drawing.Size(266, 20);
            this.TxtInvRef.TabIndex = 15;
            //
            // GridReadings
            //
            this.GridReadings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridReadings.Location = new System.Drawing.Point(0, 208);
            this.GridReadings.MainView = this.GridViewReadings;
            this.GridReadings.Name = "GridReadings";
            this.GridReadings.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoNum,
            this.RepoReading,
            this.RepoMoney2,
            this.RepoRate,
            this.RepoDate,
            this.RepoDateEdit,
            this.RepoRef});
            this.GridReadings.Size = new System.Drawing.Size(510, 336);
            this.GridReadings.TabIndex = 1;
            this.GridReadings.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewReadings});
            //
            // GridViewReadings
            //
            this.GridViewReadings.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewReadings.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewReadings.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewReadings.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewReadings.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewReadings.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewReadings.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewReadings.Appearance.Row.Options.UseFont = true;
            this.GridViewReadings.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewReadings.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewReadings.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewReadings.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewReadings.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewReadings.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewReadings.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewReadings.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewReadings.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewReadings.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewReadings.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColRMachine,
            this.ColRSerial,
            this.ColRMeter,
            this.ColRLast,
            this.ColRCurrent,
            this.ColRSource,
            this.ColRRef,
            this.ColRUsage,
            this.ColRAmount,
            this.ColRMeterName,
            this.ColRMin,
            this.ColRPrice,
            this.ColRFoc,
            this.ColRRebate,
            this.ColRLastRead,
            this.ColRLastAudit,
            this.ColRLastFetch,
            this.ColRLastInv,
            this.ColRLastInvDate,
            this.ColRInvTotal,
            this.ColRMachineStatus,
            this.ColRInvoiced});
            this.GridViewReadings.ColumnPanelRowHeight = 28;
            this.GridViewReadings.GridControl = this.GridReadings;
            this.GridViewReadings.Name = "GridViewReadings";
            this.GridViewReadings.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewReadings.OptionsNavigation.EnterMoveNextColumn = false;
            this.GridViewReadings.OptionsView.ColumnAutoWidth = true;
            this.GridViewReadings.OptionsView.ShowGroupPanel = false;
            this.GridViewReadings.OptionsView.ShowIndicator = false;
            this.GridViewReadings.OptionsView.ShowViewCaption = true;
            this.GridViewReadings.RowHeight = 24;
            this.GridViewReadings.ViewCaption = "Meter Reading";
            //
            // ColRMachine
            //
            this.ColRMachine.Caption = "Machine";
            this.ColRMachine.FieldName = "ServiceItemNo";
            this.ColRMachine.Name = "ColRMachine";
            this.ColRMachine.OptionsColumn.AllowEdit = false;
            this.ColRMachine.Visible = true;
            this.ColRMachine.VisibleIndex = 0;
            this.ColRMachine.Width = 120;
            //
            // ColRSerial
            //
            this.ColRSerial.Caption = "Serial";
            this.ColRSerial.FieldName = "SerialNo";
            this.ColRSerial.Name = "ColRSerial";
            this.ColRSerial.OptionsColumn.AllowEdit = false;
            this.ColRSerial.Visible = true;
            this.ColRSerial.VisibleIndex = 1;
            this.ColRSerial.Width = 100;
            //
            // ColRMeter
            //
            this.ColRMeter.Caption = "Meter";
            this.ColRMeter.FieldName = "MeterType";
            this.ColRMeter.Name = "ColRMeter";
            this.ColRMeter.OptionsColumn.AllowEdit = false;
            this.ColRMeter.Visible = true;
            this.ColRMeter.VisibleIndex = 2;
            this.ColRMeter.Width = 70;
            //
            // ColRLast
            //
            this.ColRLast.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRLast.AppearanceCell.Options.UseTextOptions = true;
            this.ColRLast.Caption = "Last Reading";
            this.ColRLast.ColumnEdit = this.RepoNum;
            this.ColRLast.FieldName = "LastReading";
            this.ColRLast.Name = "ColRLast";
            this.ColRLast.OptionsColumn.AllowEdit = false;
            this.ColRLast.Visible = true;
            this.ColRLast.VisibleIndex = 4;
            this.ColRLast.Width = 90;
            //
            // ColRCurrent
            //
            this.ColRCurrent.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(249)))), ((int)(((byte)(219)))));
            this.ColRCurrent.AppearanceCell.Options.UseBackColor = true;
            this.ColRCurrent.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRCurrent.AppearanceCell.Options.UseTextOptions = true;
            this.ColRCurrent.AppearanceHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(249)))), ((int)(((byte)(219)))));
            this.ColRCurrent.AppearanceHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.ColRCurrent.AppearanceHeader.Options.UseBackColor = true;
            this.ColRCurrent.AppearanceHeader.Options.UseFont = true;
            this.ColRCurrent.Caption = "Current Reading";
            this.ColRCurrent.ColumnEdit = this.RepoReading;
            this.ColRCurrent.FieldName = "CurrentReading";
            this.ColRCurrent.Name = "ColRCurrent";
            this.ColRCurrent.Visible = true;
            this.ColRCurrent.VisibleIndex = 5;
            this.ColRCurrent.Width = 100;
            //
            // ColRUsage
            //
            this.ColRUsage.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRUsage.AppearanceCell.Options.UseTextOptions = true;
            this.ColRUsage.Caption = "Meter Usage";
            this.ColRUsage.ColumnEdit = this.RepoNum;
            this.ColRUsage.FieldName = "MeterUsage";
            this.ColRUsage.Name = "ColRUsage";
            this.ColRUsage.OptionsColumn.AllowEdit = false;
            this.ColRUsage.Visible = true;
            this.ColRUsage.VisibleIndex = 8;
            this.ColRUsage.Width = 80;
            //
            // ColRAmount
            //
            this.ColRAmount.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(245)))), ((int)(((byte)(233)))));
            this.ColRAmount.AppearanceCell.Options.UseBackColor = true;
            this.ColRAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColRAmount.AppearanceHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(245)))), ((int)(((byte)(233)))));
            this.ColRAmount.AppearanceHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.ColRAmount.AppearanceHeader.Options.UseBackColor = true;
            this.ColRAmount.AppearanceHeader.Options.UseFont = true;
            this.ColRAmount.Caption = "Total Charges";
            this.ColRAmount.ColumnEdit = this.RepoMoney2;
            this.ColRAmount.FieldName = "TotalCharges";
            this.ColRAmount.Name = "ColRAmount";
            this.ColRAmount.OptionsColumn.AllowEdit = false;
            this.ColRAmount.Visible = true;
            this.ColRAmount.VisibleIndex = 9;
            this.ColRAmount.Width = 90;
            //
            // ColRSource  (keyed or fetched -- the clerk must know which)
            //
            this.ColRSource.Caption = "Source";
            this.ColRSource.FieldName = "EntrySource";
            this.ColRSource.Name = "ColRSource";
            this.ColRSource.OptionsColumn.AllowEdit = false;
            this.ColRSource.Visible = true;
            this.ColRSource.VisibleIndex = 6;
            this.ColRSource.Width = 96;
            //
            // ColRRef  (the reading's reference: PUMS's report id, or the one keyed with a manual reading)
            //
            this.ColRRef.Caption = "PUMS Report No";
            this.ColRRef.ColumnEdit = this.RepoRef;
            this.ColRRef.FieldName = "TrackingId";
            this.ColRRef.Name = "ColRRef";
            this.ColRRef.OptionsColumn.AllowEdit = false;
            this.ColRRef.OptionsColumn.ReadOnly = true;
            this.ColRRef.Visible = true;
            this.ColRRef.VisibleIndex = 7;
            this.ColRRef.Width = 130;
            //
            // ColRMeterName  (the list screen's columns, off by default -- Column Chooser has them)
            //
            this.ColRMeterName.Caption = "Meter Type Name";
            this.ColRMeterName.FieldName = "MeterTypeName";
            this.ColRMeterName.Name = "ColRMeterName";
            this.ColRMeterName.OptionsColumn.AllowEdit = false;
            this.ColRMeterName.Width = 160;
            //
            // ColRMin
            //
            this.ColRMin.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRMin.AppearanceCell.Options.UseTextOptions = true;
            this.ColRMin.Caption = "Min. Charges";
            this.ColRMin.ColumnEdit = this.RepoMoney2;
            this.ColRMin.FieldName = "MinCharges";
            this.ColRMin.Name = "ColRMin";
            this.ColRMin.OptionsColumn.AllowEdit = false;
            this.ColRMin.Width = 90;
            //
            // ColRPrice
            //
            this.ColRPrice.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRPrice.AppearanceCell.Options.UseTextOptions = true;
            this.ColRPrice.Caption = "Unit Price";
            this.ColRPrice.ColumnEdit = this.RepoRate;
            this.ColRPrice.FieldName = "UnitPrice";
            this.ColRPrice.Name = "ColRPrice";
            this.ColRPrice.OptionsColumn.AllowEdit = false;
            this.ColRPrice.Width = 80;
            //
            // ColRFoc
            //
            this.ColRFoc.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRFoc.AppearanceCell.Options.UseTextOptions = true;
            this.ColRFoc.Caption = "FOC Qty";
            this.ColRFoc.ColumnEdit = this.RepoNum;
            this.ColRFoc.FieldName = "FOCQty";
            this.ColRFoc.Name = "ColRFoc";
            this.ColRFoc.OptionsColumn.AllowEdit = false;
            this.ColRFoc.Width = 70;
            //
            // ColRRebate
            //
            this.ColRRebate.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRRebate.AppearanceCell.Options.UseTextOptions = true;
            this.ColRRebate.Caption = "Rebate (%)";
            this.ColRRebate.ColumnEdit = this.RepoMoney2;
            this.ColRRebate.FieldName = "RebatePct";
            this.ColRRebate.Name = "ColRRebate";
            this.ColRRebate.OptionsColumn.AllowEdit = false;
            this.ColRRebate.Width = 76;
            //
            // ColRLastRead
            //
            this.ColRLastRead.Caption = "Last Read Date";
            this.ColRLastRead.ColumnEdit = this.RepoDate;
            this.ColRLastRead.FieldName = "LastReadDate";
            this.ColRLastRead.Name = "ColRLastRead";
            this.ColRLastRead.OptionsColumn.AllowEdit = false;
            this.ColRLastRead.Width = 100;
            //
            // ColRLastAudit
            //
            this.ColRLastAudit.Caption = "Last Audit Date";
            this.ColRLastAudit.ColumnEdit = this.RepoDateEdit;
            this.ColRLastAudit.FieldName = "LastAuditDate";
            this.ColRLastAudit.Name = "ColRLastAudit";
            this.ColRLastAudit.OptionsColumn.AllowEdit = true;
            this.ColRLastAudit.Visible = true;
            this.ColRLastAudit.VisibleIndex = 3;
            this.ColRLastAudit.Width = 110;
            //
            // ColRLastFetch
            //
            this.ColRLastFetch.Caption = "Last Fetch Date";
            this.ColRLastFetch.ColumnEdit = this.RepoDate;
            this.ColRLastFetch.FieldName = "LastFetchDate";
            this.ColRLastFetch.Name = "ColRLastFetch";
            this.ColRLastFetch.OptionsColumn.AllowEdit = false;
            this.ColRLastFetch.Width = 110;
            //
            // ColRLastInv
            //
            this.ColRLastInv.Caption = "Last Invoice";
            this.ColRLastInv.FieldName = "LastInvNo";
            this.ColRLastInv.Name = "ColRLastInv";
            this.ColRLastInv.OptionsColumn.AllowEdit = false;
            this.ColRLastInv.Width = 100;
            //
            // ColRLastInvDate
            //
            this.ColRLastInvDate.Caption = "Last Invoice Date";
            this.ColRLastInvDate.ColumnEdit = this.RepoDate;
            this.ColRLastInvDate.FieldName = "LastInvDate";
            this.ColRLastInvDate.Name = "ColRLastInvDate";
            this.ColRLastInvDate.OptionsColumn.AllowEdit = false;
            this.ColRLastInvDate.Width = 100;
            //
            // ColRInvTotal
            //
            this.ColRInvTotal.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRInvTotal.AppearanceCell.Options.UseTextOptions = true;
            this.ColRInvTotal.Caption = "Last Invoice Total";
            this.ColRInvTotal.ColumnEdit = this.RepoMoney2;
            this.ColRInvTotal.FieldName = "InvTotal";
            this.ColRInvTotal.Name = "ColRInvTotal";
            this.ColRInvTotal.OptionsColumn.AllowEdit = false;
            this.ColRInvTotal.Width = 100;
            //
            // ColRMachineStatus
            //
            this.ColRMachineStatus.Caption = "Machine Status";
            this.ColRMachineStatus.FieldName = "MachineStatus";
            this.ColRMachineStatus.Name = "ColRMachineStatus";
            this.ColRMachineStatus.OptionsColumn.AllowEdit = false;
            this.ColRMachineStatus.Width = 90;
            //
            // ColRInvoiced
            //
            this.ColRInvoiced.Caption = "Invoiced (this period)";
            this.ColRInvoiced.FieldName = "InvoicedDocNo";
            this.ColRInvoiced.Name = "ColRInvoiced";
            this.ColRInvoiced.OptionsColumn.AllowEdit = false;
            this.ColRInvoiced.Width = 110;
            //
            // RepoRate
            //
            this.RepoRate.AutoHeight = false;
            this.RepoRate.Mask.EditMask = "n4";
            this.RepoRate.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoRate.Mask.UseMaskAsDisplayFormat = true;
            this.RepoRate.Name = "RepoRate";
            //
            // RepoDate
            //
            this.RepoDate.AutoHeight = false;
            this.RepoDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.RepoDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.RepoDate.Name = "RepoDate";
            //
            // RepoDateEdit  (the day a keyed reading was taken -- the one date that is typed in)
            //
            this.RepoDateEdit.AutoHeight = false;
            this.RepoDateEdit.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.RepoDateEdit.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.RepoDateEdit.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.RepoDateEdit.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.RepoDateEdit.EditFormat.FormatString = "dd/MM/yyyy";
            this.RepoDateEdit.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.RepoDateEdit.Mask.EditMask = "dd/MM/yyyy";
            this.RepoDateEdit.Mask.UseMaskAsDisplayFormat = true;
            this.RepoDateEdit.Name = "RepoDateEdit";
            //
            // RepoRef  (at most 30 -- all the invoice's Ref can hold)
            //
            this.RepoRef.AutoHeight = false;
            this.RepoRef.MaxLength = 30;
            this.RepoRef.Name = "RepoRef";
            //
            // RepoNum
            //
            this.RepoNum.AutoHeight = false;
            this.RepoNum.Mask.EditMask = "n0";
            this.RepoNum.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoNum.Mask.UseMaskAsDisplayFormat = true;
            this.RepoNum.Name = "RepoNum";
            //
            // RepoReading
            //
            this.RepoReading.AutoHeight = false;
            this.RepoReading.Mask.EditMask = "n0";
            this.RepoReading.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoReading.Mask.UseMaskAsDisplayFormat = true;
            this.RepoReading.Name = "RepoReading";
            //
            // RepoMoney2
            //
            this.RepoMoney2.AutoHeight = false;
            this.RepoMoney2.Mask.EditMask = "n2";
            this.RepoMoney2.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoMoney2.Mask.UseMaskAsDisplayFormat = true;
            this.RepoMoney2.Name = "RepoMoney2";
            //
            // PanelDetailFoot
            //
            this.PanelDetailFoot.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelDetailFoot.Appearance.Options.UseBackColor = true;
            this.PanelDetailFoot.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelDetailFoot.Controls.Add(this.LblDetailFoot);
            this.PanelDetailFoot.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelDetailFoot.Location = new System.Drawing.Point(0, 476);
            this.PanelDetailFoot.Name = "PanelDetailFoot";
            this.PanelDetailFoot.Size = new System.Drawing.Size(510, 30);
            this.PanelDetailFoot.TabIndex = 2;
            //
            // LblDetailFoot
            //
            this.LblDetailFoot.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDetailFoot.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDetailFoot.Appearance.Options.UseFont = true;
            this.LblDetailFoot.Appearance.Options.UseForeColor = true;
            this.LblDetailFoot.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblDetailFoot.Location = new System.Drawing.Point(12, 7);
            this.LblDetailFoot.Name = "LblDetailFoot";
            this.LblDetailFoot.Size = new System.Drawing.Size(486, 16);
            this.LblDetailFoot.TabIndex = 0;
            this.LblDetailFoot.Text = "";
            //
            // PanelStatus
            //
            this.PanelStatus.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelStatus.Appearance.Options.UseBackColor = true;
            this.PanelStatus.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelStatus.Controls.Add(this.LblStatus);
            this.PanelStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelStatus.Location = new System.Drawing.Point(0, 704);
            this.PanelStatus.Name = "PanelStatus";
            this.PanelStatus.Size = new System.Drawing.Size(1280, 24);
            this.PanelStatus.TabIndex = 3;
            //
            // LblStatus
            //
            this.LblStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblStatus.Appearance.Options.UseFont = true;
            this.LblStatus.Appearance.Options.UseForeColor = true;
            this.LblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblStatus.Location = new System.Drawing.Point(10, 4);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new System.Drawing.Size(1200, 16);
            this.LblStatus.TabIndex = 0;
            this.LblStatus.Text = "";
            //
            // InvoiceRun_Form
            //
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 728);
            this.Controls.Add(this.SplitMain);
            this.Controls.Add(this.PanelMeters);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelView);
            this.Controls.Add(this.PanelHeaderTop);
            this.Controls.Add(this.PanelStatus);
            this.Name = "InvoiceRun_Form";
            this.Text = "Meter Invoice Run";
            ((System.ComponentModel.ISupportInitialize)(this.PanelView)).EndInit();
            this.PanelView.ResumeLayout(false);
            this.PanelView.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbYear.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).EndInit();
            this.SplitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailHead)).EndInit();
            this.PanelDetailHead.ResumeLayout(false);
            this.PanelDetailHead.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChkMissingOnly.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtInvDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtInvDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtInvRef.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoReading)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDateEdit.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoDateEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoRef)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailFoot)).EndInit();
            this.PanelDetailFoot.ResumeLayout(false);
            this.PanelDetailFoot.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).EndInit();
            this.PanelStatus.ResumeLayout(false);
            this.PanelStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.PanelControl PanelView;
        private DevExpress.XtraEditors.LabelControl LblView;
        private DevExpress.XtraEditors.CheckButton BtnViewInvoices;
        private DevExpress.XtraEditors.CheckButton BtnViewMeters;
        private DevExpress.XtraEditors.LabelControl LblViewHint;
        private DevExpress.XtraEditors.PanelControl PanelMeters;
        private DevExpress.XtraEditors.PanelControl PanelFilter;
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraEditors.LabelControl LblMonth;
        private DevExpress.XtraEditors.ComboBoxEdit CmbMonth;
        private DevExpress.XtraEditors.ComboBoxEdit CmbYear;
        private DevExpress.XtraEditors.LabelControl LblDay;
        private DevExpress.XtraEditors.PanelControl PanelDays;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnFetch;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateReady;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateSelected;
        private DevExpress.XtraEditors.SimpleButton BtnSetting;
        private DevExpress.XtraEditors.LabelControl LblShow;
        private DevExpress.XtraEditors.CheckButton ChipAll;
        private DevExpress.XtraEditors.CheckButton ChipReady;
        private DevExpress.XtraEditors.CheckButton ChipWaiting;
        private DevExpress.XtraEditors.CheckButton ChipInvoiced;
        private DevExpress.XtraEditors.LabelControl LblKind;
        private DevExpress.XtraEditors.CheckButton ChipKindAll;
        private DevExpress.XtraEditors.CheckButton ChipKindRental;
        private DevExpress.XtraEditors.CheckButton ChipKindMeter;
        private DevExpress.XtraEditors.LabelControl LblGrouping;
        private DevExpress.XtraEditors.SplitContainerControl SplitMain;
        private DevExpress.XtraGrid.GridControl GridInvoices;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewInvoices;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColCustomer;
        private DevExpress.XtraGrid.Columns.GridColumn ColContract;
        private DevExpress.XtraGrid.Columns.GridColumn ColKind;
        private DevExpress.XtraGrid.Columns.GridColumn ColDetail;
        private DevExpress.XtraGrid.Columns.GridColumn ColMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColStatus;
        private DevExpress.XtraGrid.Columns.GridColumn ColDue;
        private DevExpress.XtraGrid.Columns.GridColumn ColInvDate;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoMoney;
        private DevExpress.XtraEditors.PanelControl PanelDetailHead;
        private DevExpress.XtraEditors.LabelControl LblDetailTitle;
        private DevExpress.XtraEditors.LabelControl LblDetailSub;
        private DevExpress.XtraEditors.LabelControl LblFactMachines;
        private DevExpress.XtraEditors.LabelControl LblFactReadings;
        private DevExpress.XtraEditors.LabelControl LblFactMissing;
        private DevExpress.XtraEditors.LabelControl LblFactAmount;
        private DevExpress.XtraEditors.CheckEdit ChkMissingOnly;
        private DevExpress.XtraEditors.SimpleButton BtnPreview;
        private DevExpress.XtraEditors.SimpleButton BtnHistory;
        private DevExpress.XtraEditors.SimpleButton BtnDeleteInvoice;
        private DevExpress.XtraEditors.SimpleButton BtnTakeOver;
        private DevExpress.XtraEditors.LabelControl LblInvDate;
        private DevExpress.XtraEditors.DateEdit DtInvDate;
        private DevExpress.XtraEditors.SimpleButton BtnInvDateFromReading;
        private DevExpress.XtraEditors.LabelControl LblInvRef;
        private DevExpress.XtraEditors.TextEdit TxtInvRef;
        private DevExpress.XtraEditors.CheckButton BtnOverdue;
        private DevExpress.XtraGrid.GridControl GridReadings;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMachine;
        private DevExpress.XtraGrid.Columns.GridColumn ColRSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMeter;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLast;
        private DevExpress.XtraGrid.Columns.GridColumn ColRCurrent;
        private DevExpress.XtraGrid.Columns.GridColumn ColRUsage;
        private DevExpress.XtraGrid.Columns.GridColumn ColRAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColRSource;
        private DevExpress.XtraGrid.Columns.GridColumn ColRRef;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMeterName;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMin;
        private DevExpress.XtraGrid.Columns.GridColumn ColRPrice;
        private DevExpress.XtraGrid.Columns.GridColumn ColRFoc;
        private DevExpress.XtraGrid.Columns.GridColumn ColRRebate;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLastRead;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLastAudit;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLastFetch;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLastInv;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLastInvDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColRInvTotal;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMachineStatus;
        private DevExpress.XtraGrid.Columns.GridColumn ColRInvoiced;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoRate;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoDate;
        private DevExpress.XtraEditors.Repository.RepositoryItemDateEdit RepoDateEdit;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoRef;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoNum;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoReading;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoMoney2;
        private DevExpress.XtraEditors.PanelControl PanelDetailFoot;
        private DevExpress.XtraEditors.LabelControl LblDetailFoot;
        private DevExpress.XtraEditors.PanelControl PanelStatus;
        private DevExpress.XtraEditors.LabelControl LblStatus;
    }
}
