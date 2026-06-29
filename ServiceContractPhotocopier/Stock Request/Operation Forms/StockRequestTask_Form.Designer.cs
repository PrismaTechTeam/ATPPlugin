namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    partial class StockRequestTask_Form
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

        // ---- Title bar ----
        private DevExpress.XtraEditors.PanelControl PanelTitle;
        private DevExpress.XtraEditors.LabelControl LblTitle;

        // ---- Outer filter panel ----
        private DevExpress.XtraEditors.PanelControl PanelFilter;

        // ---- Filter group ----
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraEditors.CheckEdit ChkPendingOnly;
        private DevExpress.XtraEditors.LabelControl LblFrom;
        private DevExpress.XtraEditors.DateEdit DtFrom;
        private DevExpress.XtraEditors.LabelControl LblTo;
        private DevExpress.XtraEditors.DateEdit DtTo;
        private DevExpress.XtraEditors.SimpleButton BtnFilter;
        private DevExpress.XtraEditors.SimpleButton BtnReset;
        private DevExpress.XtraEditors.CheckEdit ChkFilterIssue;
        private DevExpress.XtraEditors.CheckEdit ChkFilterTransfer;
        private DevExpress.XtraEditors.CheckEdit ChkFilterBoth;
        // Two new "Show ..." toggles placed in PanelFilter under the Refresh button.
        private DevExpress.XtraEditors.CheckEdit ChkShowIssue;
        private DevExpress.XtraEditors.CheckEdit ChkShowTransfer;

        // ---- Action buttons ----
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateSIST;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateSISTAll;
        private DevExpress.XtraEditors.SimpleButton BtnMarkIgnore;
        private DevExpress.XtraEditors.SimpleButton BtnSettings;
        private DevExpress.XtraEditors.SimpleButton BtnViewLog;

        // ---- Change / bulk action buttons (row 2) ----
        private DevExpress.XtraEditors.SimpleButton _btnSelUpdate;
        private DevExpress.XtraEditors.SimpleButton _btnSelCancel;
        private DevExpress.XtraEditors.SimpleButton _btnApproveChange;
        private DevExpress.XtraEditors.SimpleButton _btnSelRequest;
        private DevExpress.XtraEditors.CheckEdit _chkHideIgnore;

        // ---- Row-colour legend ----
        private DevExpress.XtraEditors.LabelControl LblLegend;
        private System.Windows.Forms.Panel PnlSwNormal;
        private DevExpress.XtraEditors.LabelControl LblSwNormal;
        private System.Windows.Forms.Panel PnlSwUpdate;
        private DevExpress.XtraEditors.LabelControl LblSwUpdate;
        private System.Windows.Forms.Panel PnlSwCancel;
        private DevExpress.XtraEditors.LabelControl LblSwCancel;
        private System.Windows.Forms.Panel PnlSwComplete;
        private DevExpress.XtraEditors.LabelControl LblSwComplete;

        // ---- Auto-refresh timer ----
        private System.Windows.Forms.Timer RefreshTimer;

        // ---- Split container with two grids ----
        private DevExpress.XtraEditors.SplitContainerControl SplitContainer;
        private DevExpress.XtraGrid.GridControl GridIssue;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewIssue;
        private DevExpress.XtraGrid.GridControl GridTransfer;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewTransfer;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.PanelTitle = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle = new DevExpress.XtraEditors.LabelControl();
            this.PanelFilter = new DevExpress.XtraEditors.PanelControl();
            this.GrpFilter = new DevExpress.XtraEditors.GroupControl();
            this.LblSearch = new DevExpress.XtraEditors.LabelControl();
            this.TxtSearch = new DevExpress.XtraEditors.TextEdit();
            this.ChkPendingOnly = new DevExpress.XtraEditors.CheckEdit();
            this.LblFrom = new DevExpress.XtraEditors.LabelControl();
            this.DtFrom = new DevExpress.XtraEditors.DateEdit();
            this.LblTo = new DevExpress.XtraEditors.LabelControl();
            this.DtTo = new DevExpress.XtraEditors.DateEdit();
            this.BtnFilter = new DevExpress.XtraEditors.SimpleButton();
            this.BtnReset = new DevExpress.XtraEditors.SimpleButton();
            this.ChkFilterIssue = new DevExpress.XtraEditors.CheckEdit();
            this.ChkFilterTransfer = new DevExpress.XtraEditors.CheckEdit();
            this.ChkFilterBoth = new DevExpress.XtraEditors.CheckEdit();
            this._chkHideIgnore = new DevExpress.XtraEditors.CheckEdit();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.ChkShowIssue = new DevExpress.XtraEditors.CheckEdit();
            this.ChkShowTransfer = new DevExpress.XtraEditors.CheckEdit();
            this.BtnGenerateSIST = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateSISTAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnMarkIgnore = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSettings = new DevExpress.XtraEditors.SimpleButton();
            this.BtnViewLog = new DevExpress.XtraEditors.SimpleButton();
            this._btnSelUpdate = new DevExpress.XtraEditors.SimpleButton();
            this._btnSelCancel = new DevExpress.XtraEditors.SimpleButton();
            this._btnApproveChange = new DevExpress.XtraEditors.SimpleButton();
            this._btnSelRequest = new DevExpress.XtraEditors.SimpleButton();
            this.LblLegend = new DevExpress.XtraEditors.LabelControl();
            this.PnlSwNormal = new System.Windows.Forms.Panel();
            this.LblSwNormal = new DevExpress.XtraEditors.LabelControl();
            this.PnlSwUpdate = new System.Windows.Forms.Panel();
            this.LblSwUpdate = new DevExpress.XtraEditors.LabelControl();
            this.PnlSwCancel = new System.Windows.Forms.Panel();
            this.LblSwCancel = new DevExpress.XtraEditors.LabelControl();
            this.PnlSwComplete = new System.Windows.Forms.Panel();
            this.LblSwComplete = new DevExpress.XtraEditors.LabelControl();
            this.RefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.SplitContainer = new DevExpress.XtraEditors.SplitContainerControl();
            this.GridIssue = new DevExpress.XtraGrid.GridControl();
            this.GridViewIssue = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.GridTransfer = new DevExpress.XtraGrid.GridControl();
            this.GridViewTransfer = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).BeginInit();
            this.PanelTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkPendingOnly.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterIssue.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterTransfer.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterBoth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._chkHideIgnore.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowIssue.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowTransfer.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer.Panel1)).BeginInit();
            this.SplitContainer.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer.Panel2)).BeginInit();
            this.SplitContainer.Panel2.SuspendLayout();
            this.SplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridIssue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewIssue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridTransfer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewTransfer)).BeginInit();
            this.SuspendLayout();
            // 
            // PanelTitle
            // 
            this.PanelTitle.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTitle.Appearance.Options.UseBackColor = true;
            this.PanelTitle.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTitle.Controls.Add(this.LblTitle);
            this.PanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTitle.Location = new System.Drawing.Point(0, 0);
            this.PanelTitle.Name = "PanelTitle";
            this.PanelTitle.Size = new System.Drawing.Size(1820, 42);
            this.PanelTitle.TabIndex = 0;
            // 
            // LblTitle
            // 
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblTitle.Location = new System.Drawing.Point(18, 10);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(400, 22);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Stock Request Task";
            // 
            // PanelFilter
            // 
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.labelControl1);
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnRefresh);
            this.PanelFilter.Controls.Add(this.ChkShowIssue);
            this.PanelFilter.Controls.Add(this.ChkShowTransfer);
            this.PanelFilter.Controls.Add(this.BtnGenerateSIST);
            this.PanelFilter.Controls.Add(this.BtnGenerateSISTAll);
            this.PanelFilter.Controls.Add(this.BtnMarkIgnore);
            this.PanelFilter.Controls.Add(this.BtnSettings);
            this.PanelFilter.Controls.Add(this.BtnViewLog);
            this.PanelFilter.Controls.Add(this._btnSelUpdate);
            this.PanelFilter.Controls.Add(this._btnSelCancel);
            this.PanelFilter.Controls.Add(this._btnApproveChange);
            this.PanelFilter.Controls.Add(this._btnSelRequest);
            this.PanelFilter.Controls.Add(this.LblLegend);
            this.PanelFilter.Controls.Add(this.PnlSwNormal);
            this.PanelFilter.Controls.Add(this.LblSwNormal);
            this.PanelFilter.Controls.Add(this.PnlSwUpdate);
            this.PanelFilter.Controls.Add(this.LblSwUpdate);
            this.PanelFilter.Controls.Add(this.PnlSwCancel);
            this.PanelFilter.Controls.Add(this.LblSwCancel);
            this.PanelFilter.Controls.Add(this.PnlSwComplete);
            this.PanelFilter.Controls.Add(this.LblSwComplete);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 42);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1820, 136);
            this.PanelFilter.TabIndex = 1;
            // 
            // GrpFilter
            // 
            this.GrpFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GrpFilter.Appearance.Options.UseFont = true;
            this.GrpFilter.Controls.Add(this.LblSearch);
            this.GrpFilter.Controls.Add(this.TxtSearch);
            this.GrpFilter.Controls.Add(this.ChkPendingOnly);
            this.GrpFilter.Controls.Add(this.LblFrom);
            this.GrpFilter.Controls.Add(this.DtFrom);
            this.GrpFilter.Controls.Add(this.LblTo);
            this.GrpFilter.Controls.Add(this.DtTo);
            this.GrpFilter.Controls.Add(this.BtnFilter);
            this.GrpFilter.Controls.Add(this.BtnReset);
            this.GrpFilter.Controls.Add(this.ChkFilterIssue);
            this.GrpFilter.Controls.Add(this.ChkFilterTransfer);
            this.GrpFilter.Controls.Add(this.ChkFilterBoth);
            this.GrpFilter.Controls.Add(this._chkHideIgnore);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(494, 124);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            // 
            // LblSearch
            // 
            this.LblSearch.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblSearch.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblSearch.Appearance.Options.UseFont = true;
            this.LblSearch.Appearance.Options.UseForeColor = true;
            this.LblSearch.Location = new System.Drawing.Point(10, 29);
            this.LblSearch.Name = "LblSearch";
            this.LblSearch.Size = new System.Drawing.Size(62, 15);
            this.LblSearch.TabIndex = 0;
            this.LblSearch.Text = "Search Any:";
            // 
            // TxtSearch
            // 
            this.TxtSearch.Location = new System.Drawing.Point(78, 25);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Properties.NullValuePrompt = "Search anything...";
            this.TxtSearch.Size = new System.Drawing.Size(185, 20);
            this.TxtSearch.TabIndex = 1;
            // 
            // ChkPendingOnly
            // 
            this.ChkPendingOnly.Location = new System.Drawing.Point(272, 25);
            this.ChkPendingOnly.Name = "ChkPendingOnly";
            this.ChkPendingOnly.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkPendingOnly.Properties.Appearance.Options.UseFont = true;
            this.ChkPendingOnly.Properties.Caption = "Show Only New Task";
            this.ChkPendingOnly.Size = new System.Drawing.Size(175, 20);
            this.ChkPendingOnly.TabIndex = 2;
            // 
            // LblFrom
            // 
            this.LblFrom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFrom.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblFrom.Appearance.Options.UseFont = true;
            this.LblFrom.Appearance.Options.UseForeColor = true;
            this.LblFrom.Location = new System.Drawing.Point(10, 62);
            this.LblFrom.Name = "LblFrom";
            this.LblFrom.Size = new System.Drawing.Size(31, 15);
            this.LblFrom.TabIndex = 3;
            this.LblFrom.Text = "From:";
            // 
            // DtFrom
            // 
            this.DtFrom.EditValue = null;
            this.DtFrom.Location = new System.Drawing.Point(46, 58);
            this.DtFrom.Name = "DtFrom";
            this.DtFrom.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.DtFrom.Properties.Appearance.Options.UseFont = true;
            this.DtFrom.Properties.CalendarTimeProperties.DisplayFormat.FormatString = "HH:mm";
            this.DtFrom.Properties.CalendarTimeProperties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.CalendarTimeProperties.EditFormat.FormatString = "HH:mm";
            this.DtFrom.Properties.CalendarTimeProperties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtFrom.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtFrom.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtFrom.Size = new System.Drawing.Size(115, 22);
            this.DtFrom.TabIndex = 4;
            // 
            // LblTo
            // 
            this.LblTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblTo.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblTo.Appearance.Options.UseFont = true;
            this.LblTo.Appearance.Options.UseForeColor = true;
            this.LblTo.Location = new System.Drawing.Point(168, 62);
            this.LblTo.Name = "LblTo";
            this.LblTo.Size = new System.Drawing.Size(17, 15);
            this.LblTo.TabIndex = 5;
            this.LblTo.Text = "To:";
            // 
            // DtTo
            // 
            this.DtTo.EditValue = null;
            this.DtTo.Location = new System.Drawing.Point(186, 58);
            this.DtTo.Name = "DtTo";
            this.DtTo.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.DtTo.Properties.Appearance.Options.UseFont = true;
            this.DtTo.Properties.CalendarTimeProperties.DisplayFormat.FormatString = "HH:mm";
            this.DtTo.Properties.CalendarTimeProperties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.CalendarTimeProperties.EditFormat.FormatString = "HH:mm";
            this.DtTo.Properties.CalendarTimeProperties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtTo.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtTo.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtTo.Size = new System.Drawing.Size(115, 22);
            this.DtTo.TabIndex = 6;
            // 
            // BtnFilter
            // 
            this.BtnFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnFilter.Appearance.Options.UseFont = true;
            this.BtnFilter.Location = new System.Drawing.Point(309, 56);
            this.BtnFilter.Name = "BtnFilter";
            this.BtnFilter.Size = new System.Drawing.Size(70, 28);
            this.BtnFilter.TabIndex = 7;
            this.BtnFilter.Text = "Filter";
            this.BtnFilter.Click += new System.EventHandler(this.BtnFilter_Click);
            // 
            // BtnReset
            // 
            this.BtnReset.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnReset.Appearance.Options.UseFont = true;
            this.BtnReset.Location = new System.Drawing.Point(385, 56);
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.Size = new System.Drawing.Size(70, 28);
            this.BtnReset.TabIndex = 8;
            this.BtnReset.Text = "Reset";
            this.BtnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // ChkFilterIssue
            // 
            this.ChkFilterIssue.Location = new System.Drawing.Point(10, 92);
            this.ChkFilterIssue.Name = "ChkFilterIssue";
            this.ChkFilterIssue.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkFilterIssue.Properties.Appearance.Options.UseFont = true;
            this.ChkFilterIssue.Properties.Caption = "Filter by Stock Issues";
            this.ChkFilterIssue.Size = new System.Drawing.Size(170, 20);
            this.ChkFilterIssue.TabIndex = 8;
            this.ChkFilterIssue.CheckedChanged += new System.EventHandler(this.ChkFilterIssue_CheckedChanged);
            // 
            // ChkFilterTransfer
            // 
            this.ChkFilterTransfer.Location = new System.Drawing.Point(186, 92);
            this.ChkFilterTransfer.Name = "ChkFilterTransfer";
            this.ChkFilterTransfer.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkFilterTransfer.Properties.Appearance.Options.UseFont = true;
            this.ChkFilterTransfer.Properties.Caption = "Filter by Stock Transfer";
            this.ChkFilterTransfer.Size = new System.Drawing.Size(190, 20);
            this.ChkFilterTransfer.TabIndex = 9;
            this.ChkFilterTransfer.CheckedChanged += new System.EventHandler(this.ChkFilterTransfer_CheckedChanged);
            // 
            // ChkFilterBoth
            // 
            this.ChkFilterBoth.Location = new System.Drawing.Point(382, 92);
            this.ChkFilterBoth.Name = "ChkFilterBoth";
            this.ChkFilterBoth.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkFilterBoth.Properties.Appearance.Options.UseFont = true;
            this.ChkFilterBoth.Properties.Caption = "Filter Both";
            this.ChkFilterBoth.Size = new System.Drawing.Size(110, 20);
            this.ChkFilterBoth.TabIndex = 10;
            this.ChkFilterBoth.Visible = false;
            this.ChkFilterBoth.CheckedChanged += new System.EventHandler(this.ChkFilterBoth_CheckedChanged);
            // 
            // _chkHideIgnore
            // 
            this._chkHideIgnore.Location = new System.Drawing.Point(382, 92);
            this._chkHideIgnore.Name = "_chkHideIgnore";
            this._chkHideIgnore.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._chkHideIgnore.Properties.Appearance.Options.UseFont = true;
            this._chkHideIgnore.Properties.Caption = "Hide Ignore";
            this._chkHideIgnore.Size = new System.Drawing.Size(110, 20);
            this._chkHideIgnore.TabIndex = 11;
            this._chkHideIgnore.CheckedChanged += new System.EventHandler(this.ChkHideIgnore_CheckedChanged);
            // 
            // BtnRefresh
            // 
            this.BtnRefresh.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnRefresh.Appearance.Options.UseFont = true;
            this.BtnRefresh.Location = new System.Drawing.Point(510, 7);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(150, 50);
            this.BtnRefresh.TabIndex = 10;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // ChkShowIssue
            // 
            this.ChkShowIssue.Location = new System.Drawing.Point(1308, 25);
            this.ChkShowIssue.Name = "ChkShowIssue";
            this.ChkShowIssue.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.ChkShowIssue.Properties.Appearance.Options.UseFont = true;
            this.ChkShowIssue.Properties.Caption = "Show Stock Issues Request";
            this.ChkShowIssue.Size = new System.Drawing.Size(190, 20);
            this.ChkShowIssue.TabIndex = 15;
            this.ChkShowIssue.CheckedChanged += new System.EventHandler(this.ChkShowGrid_CheckedChanged);
            // 
            // ChkShowTransfer
            // 
            this.ChkShowTransfer.Location = new System.Drawing.Point(1308, 47);
            this.ChkShowTransfer.Name = "ChkShowTransfer";
            this.ChkShowTransfer.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.ChkShowTransfer.Properties.Appearance.Options.UseFont = true;
            this.ChkShowTransfer.Properties.Caption = "Show Stock Transfer Request";
            this.ChkShowTransfer.Size = new System.Drawing.Size(200, 20);
            this.ChkShowTransfer.TabIndex = 16;
            this.ChkShowTransfer.CheckedChanged += new System.EventHandler(this.ChkShowGrid_CheckedChanged);
            // 
            // BtnGenerateSIST
            // 
            this.BtnGenerateSIST.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.BtnGenerateSIST.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnGenerateSIST.Appearance.Options.UseFont = true;
            this.BtnGenerateSIST.Appearance.Options.UseForeColor = true;
            this.BtnGenerateSIST.Location = new System.Drawing.Point(666, 8);
            this.BtnGenerateSIST.Name = "BtnGenerateSIST";
            this.BtnGenerateSIST.Size = new System.Drawing.Size(150, 50);
            this.BtnGenerateSIST.TabIndex = 11;
            this.BtnGenerateSIST.Text = "Generate SI/ST";
            this.BtnGenerateSIST.Click += new System.EventHandler(this.BtnGenerateSIST_Click);
            // 
            // BtnGenerateSISTAll
            // 
            this.BtnGenerateSISTAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnGenerateSISTAll.Appearance.Options.UseFont = true;
            this.BtnGenerateSISTAll.Location = new System.Drawing.Point(510, 61);
            this.BtnGenerateSISTAll.Name = "BtnGenerateSISTAll";
            this.BtnGenerateSISTAll.Size = new System.Drawing.Size(150, 50);
            this.BtnGenerateSISTAll.TabIndex = 12;
            this.BtnGenerateSISTAll.Text = "Select All New";
            this.BtnGenerateSISTAll.Click += new System.EventHandler(this.BtnGenerateSISTAll_Click);
            // 
            // BtnMarkIgnore
            // 
            this.BtnMarkIgnore.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnMarkIgnore.Appearance.Options.UseFont = true;
            this.BtnMarkIgnore.Location = new System.Drawing.Point(978, 8);
            this.BtnMarkIgnore.Name = "BtnMarkIgnore";
            this.BtnMarkIgnore.Size = new System.Drawing.Size(150, 50);
            this.BtnMarkIgnore.TabIndex = 13;
            this.BtnMarkIgnore.Text = "Mark as Ignore";
            this.BtnMarkIgnore.Click += new System.EventHandler(this.BtnMarkIgnore_Click);
            // 
            // BtnSettings
            // 
            this.BtnSettings.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSettings.Appearance.Options.UseFont = true;
            this.BtnSettings.Location = new System.Drawing.Point(1134, 8);
            this.BtnSettings.Name = "BtnSettings";
            this.BtnSettings.Size = new System.Drawing.Size(150, 50);
            this.BtnSettings.TabIndex = 14;
            this.BtnSettings.Text = "Settings";
            this.BtnSettings.Click += new System.EventHandler(this.BtnSettings_Click);
            // 
            // BtnViewLog
            // 
            this.BtnViewLog.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnViewLog.Appearance.Options.UseFont = true;
            this.BtnViewLog.Location = new System.Drawing.Point(1134, 62);
            this.BtnViewLog.Name = "BtnViewLog";
            this.BtnViewLog.Size = new System.Drawing.Size(150, 50);
            this.BtnViewLog.TabIndex = 15;
            this.BtnViewLog.Text = "View Log";
            this.BtnViewLog.Click += new System.EventHandler(this.BtnViewLog_Click);
            // 
            // _btnSelUpdate
            // 
            this._btnSelUpdate.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnSelUpdate.Appearance.Options.UseFont = true;
            this._btnSelUpdate.Location = new System.Drawing.Point(666, 61);
            this._btnSelUpdate.Name = "_btnSelUpdate";
            this._btnSelUpdate.Size = new System.Drawing.Size(150, 50);
            this._btnSelUpdate.TabIndex = 17;
            this._btnSelUpdate.Text = "Select All Update";
            this._btnSelUpdate.Click += new System.EventHandler(this.BtnSelectAllUpdate_Click);
            // 
            // _btnSelCancel
            // 
            this._btnSelCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnSelCancel.Appearance.Options.UseFont = true;
            this._btnSelCancel.Location = new System.Drawing.Point(978, 61);
            this._btnSelCancel.Name = "_btnSelCancel";
            this._btnSelCancel.Size = new System.Drawing.Size(150, 50);
            this._btnSelCancel.TabIndex = 18;
            this._btnSelCancel.Text = "Select All Cancel";
            this._btnSelCancel.Click += new System.EventHandler(this.BtnSelectAllCancel_Click);
            // 
            // _btnApproveChange
            // 
            this._btnApproveChange.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnApproveChange.Appearance.Options.UseFont = true;
            this._btnApproveChange.Location = new System.Drawing.Point(822, 8);
            this._btnApproveChange.Name = "_btnApproveChange";
            this._btnApproveChange.Size = new System.Drawing.Size(150, 50);
            this._btnApproveChange.TabIndex = 19;
            this._btnApproveChange.Text = "Approve Changes";
            this._btnApproveChange.Click += new System.EventHandler(this.BtnApproveChange_Click);
            //
            // _btnSelRequest
            // 
            this._btnSelRequest.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._btnSelRequest.Appearance.Options.UseFont = true;
            this._btnSelRequest.Location = new System.Drawing.Point(822, 61);
            this._btnSelRequest.Name = "_btnSelRequest";
            this._btnSelRequest.Size = new System.Drawing.Size(150, 50);
            this._btnSelRequest.TabIndex = 28;
            this._btnSelRequest.Text = "Select All Request";
            this._btnSelRequest.Click += new System.EventHandler(this.BtnSelectAllRequest_Click);
            // 
            // LblLegend
            // 
            this.LblLegend.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.LblLegend.Appearance.Options.UseFont = true;
            this.LblLegend.Location = new System.Drawing.Point(1308, 77);
            this.LblLegend.Name = "LblLegend";
            this.LblLegend.Size = new System.Drawing.Size(62, 13);
            this.LblLegend.TabIndex = 21;
            this.LblLegend.Text = "Row colour:";
            // 
            // PnlSwNormal
            // 
            this.PnlSwNormal.BackColor = System.Drawing.Color.White;
            this.PnlSwNormal.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PnlSwNormal.Location = new System.Drawing.Point(1308, 96);
            this.PnlSwNormal.Name = "PnlSwNormal";
            this.PnlSwNormal.Size = new System.Drawing.Size(26, 16);
            this.PnlSwNormal.TabIndex = 22;
            // 
            // LblSwNormal
            // 
            this.LblSwNormal.Location = new System.Drawing.Point(1338, 97);
            this.LblSwNormal.Name = "LblSwNormal";
            this.LblSwNormal.Size = new System.Drawing.Size(20, 13);
            this.LblSwNormal.TabIndex = 23;
            this.LblSwNormal.Text = "New";
            // 
            // PnlSwUpdate
            // 
            this.PnlSwUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(245)))), ((int)(((byte)(157)))));
            this.PnlSwUpdate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PnlSwUpdate.Location = new System.Drawing.Point(1398, 96);
            this.PnlSwUpdate.Name = "PnlSwUpdate";
            this.PnlSwUpdate.Size = new System.Drawing.Size(26, 16);
            this.PnlSwUpdate.TabIndex = 24;
            // 
            // LblSwUpdate
            // 
            this.LblSwUpdate.Location = new System.Drawing.Point(1428, 97);
            this.LblSwUpdate.Name = "LblSwUpdate";
            this.LblSwUpdate.Size = new System.Drawing.Size(62, 13);
            this.LblSwUpdate.TabIndex = 25;
            this.LblSwUpdate.Text = "New Update";
            // 
            // PnlSwCancel
            // 
            this.PnlSwCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(205)))), ((int)(((byte)(210)))));
            this.PnlSwCancel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PnlSwCancel.Location = new System.Drawing.Point(1512, 96);
            this.PnlSwCancel.Name = "PnlSwCancel";
            this.PnlSwCancel.Size = new System.Drawing.Size(26, 16);
            this.PnlSwCancel.TabIndex = 26;
            // 
            // LblSwCancel
            // 
            this.LblSwCancel.Location = new System.Drawing.Point(1542, 97);
            this.LblSwCancel.Name = "LblSwCancel";
            this.LblSwCancel.Size = new System.Drawing.Size(60, 13);
            this.LblSwCancel.TabIndex = 27;
            this.LblSwCancel.Text = "New Cancel";
            //
            // PnlSwComplete
            //
            this.PnlSwComplete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(165)))), ((int)(((byte)(214)))), ((int)(((byte)(167)))));
            this.PnlSwComplete.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PnlSwComplete.Location = new System.Drawing.Point(1626, 96);
            this.PnlSwComplete.Name = "PnlSwComplete";
            this.PnlSwComplete.Size = new System.Drawing.Size(26, 16);
            this.PnlSwComplete.TabIndex = 28;
            //
            // LblSwComplete
            //
            this.LblSwComplete.Location = new System.Drawing.Point(1656, 97);
            this.LblSwComplete.Name = "LblSwComplete";
            this.LblSwComplete.Size = new System.Drawing.Size(58, 13);
            this.LblSwComplete.TabIndex = 29;
            this.LblSwComplete.Text = "Completed";
            // 
            // RefreshTimer
            // 
            this.RefreshTimer.Interval = 1000;
            this.RefreshTimer.Tick += new System.EventHandler(this.RefreshTimer_Tick);
            // 
            // SplitContainer
            // 
            this.SplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitContainer.Horizontal = false;
            this.SplitContainer.Location = new System.Drawing.Point(0, 178);
            this.SplitContainer.Name = "SplitContainer";
            // 
            // SplitContainer.Panel1
            // 
            this.SplitContainer.Panel1.Controls.Add(this.GridIssue);
            this.SplitContainer.Panel1.Text = "Panel1";
            // 
            // SplitContainer.Panel2
            // 
            this.SplitContainer.Panel2.Controls.Add(this.GridTransfer);
            this.SplitContainer.Panel2.Text = "Panel2";
            this.SplitContainer.Size = new System.Drawing.Size(1820, 522);
            this.SplitContainer.SplitterPosition = 270;
            this.SplitContainer.TabIndex = 2;
            // 
            // GridIssue
            // 
            this.GridIssue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridIssue.Location = new System.Drawing.Point(0, 0);
            this.GridIssue.MainView = this.GridViewIssue;
            this.GridIssue.Name = "GridIssue";
            this.GridIssue.Size = new System.Drawing.Size(1820, 270);
            this.GridIssue.TabIndex = 0;
            this.GridIssue.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewIssue});
            // 
            // GridViewIssue
            // 
            this.GridViewIssue.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewIssue.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.EvenRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.GridViewIssue.Appearance.EvenRow.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewIssue.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewIssue.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewIssue.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewIssue.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewIssue.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewIssue.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewIssue.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewIssue.Appearance.OddRow.BackColor = System.Drawing.Color.White;
            this.GridViewIssue.Appearance.OddRow.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewIssue.Appearance.Row.Options.UseFont = true;
            this.GridViewIssue.Appearance.ViewCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.GridViewIssue.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewIssue.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewIssue.Appearance.ViewCaption.Options.UseBackColor = true;
            this.GridViewIssue.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewIssue.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewIssue.ColumnPanelRowHeight = 28;
            this.GridViewIssue.GridControl = this.GridIssue;
            this.GridViewIssue.Name = "GridViewIssue";
            this.GridViewIssue.OptionsView.ShowGroupPanel = false;
            this.GridViewIssue.OptionsView.ShowViewCaption = true;
            this.GridViewIssue.RowHeight = 24;
            this.GridViewIssue.ViewCaption = "Stock Issue Request";
            this.GridViewIssue.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.GridView_RowStyle);
            this.GridViewIssue.CustomDrawEmptyForeground += new DevExpress.XtraGrid.Views.Base.CustomDrawEventHandler(this.GridView_CustomDrawEmptyForeground);
            // 
            // GridTransfer
            // 
            this.GridTransfer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridTransfer.Location = new System.Drawing.Point(0, 0);
            this.GridTransfer.MainView = this.GridViewTransfer;
            this.GridTransfer.Name = "GridTransfer";
            this.GridTransfer.Size = new System.Drawing.Size(1820, 242);
            this.GridTransfer.TabIndex = 0;
            this.GridTransfer.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewTransfer});
            // 
            // GridViewTransfer
            // 
            this.GridViewTransfer.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewTransfer.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.EvenRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.GridViewTransfer.Appearance.EvenRow.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewTransfer.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewTransfer.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewTransfer.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewTransfer.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewTransfer.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewTransfer.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewTransfer.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewTransfer.Appearance.OddRow.BackColor = System.Drawing.Color.White;
            this.GridViewTransfer.Appearance.OddRow.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewTransfer.Appearance.Row.Options.UseFont = true;
            this.GridViewTransfer.Appearance.ViewCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.GridViewTransfer.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewTransfer.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewTransfer.Appearance.ViewCaption.Options.UseBackColor = true;
            this.GridViewTransfer.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewTransfer.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewTransfer.ColumnPanelRowHeight = 28;
            this.GridViewTransfer.GridControl = this.GridTransfer;
            this.GridViewTransfer.Name = "GridViewTransfer";
            this.GridViewTransfer.OptionsView.ShowGroupPanel = false;
            this.GridViewTransfer.OptionsView.ShowViewCaption = true;
            this.GridViewTransfer.RowHeight = 24;
            this.GridViewTransfer.ViewCaption = "Stock Transfer Request";
            this.GridViewTransfer.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.GridView_RowStyle);
            this.GridViewTransfer.CustomDrawEmptyForeground += new DevExpress.XtraGrid.Views.Base.CustomDrawEventHandler(this.GridView_CustomDrawEmptyForeground);
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(1308, 7);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(80, 13);
            this.labelControl1.TabIndex = 29;
            this.labelControl1.Text = "Display Option:";
            // 
            // StockRequestTask_Form
            // 
            this.Appearance.Options.UseFont = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1820, 700);
            this.Controls.Add(this.SplitContainer);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "StockRequestTask_Form";
            this.Text = "Stock Request Task";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).EndInit();
            this.PanelTitle.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkPendingOnly.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterIssue.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterTransfer.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFilterBoth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._chkHideIgnore.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowIssue.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowTransfer.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer.Panel1)).EndInit();
            this.SplitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer.Panel2)).EndInit();
            this.SplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer)).EndInit();
            this.SplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridIssue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewIssue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridTransfer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewTransfer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl labelControl1;
    }
}
