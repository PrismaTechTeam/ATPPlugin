namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class InterBillBoard_Form
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
            DevExpress.XtraGrid.GridLevelNode gridLevelNode1 = new DevExpress.XtraGrid.GridLevelNode();
            DevExpress.XtraGrid.GridLevelNode gridLevelNode2 = new DevExpress.XtraGrid.GridLevelNode();
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelView = new DevExpress.XtraEditors.PanelControl();
            this.LblView = new DevExpress.XtraEditors.LabelControl();
            this.BtnViewInvoices = new DevExpress.XtraEditors.CheckButton();
            this.BtnViewContracts = new DevExpress.XtraEditors.CheckButton();
            this.PanelFilter = new DevExpress.XtraEditors.PanelControl();
            this.GrpFilter = new DevExpress.XtraEditors.GroupControl();
            this.LblBook = new DevExpress.XtraEditors.LabelControl();
            this.LblConn = new DevExpress.XtraEditors.LabelControl();
            this.LblMonth = new DevExpress.XtraEditors.LabelControl();
            this.LblUpTo = new DevExpress.XtraEditors.LabelControl();
            this.ChkShowEnded = new DevExpress.XtraEditors.CheckEdit();
            this.LblCustomers = new DevExpress.XtraEditors.LabelControl();
            this.PceCustomers = new DevExpress.XtraEditors.PopupContainerEdit();
            this.PopCustomers = new DevExpress.XtraEditors.PopupContainerControl();
            this.TxtCustomerSearch = new DevExpress.XtraEditors.TextEdit();
            this.ChkTickedOnly = new DevExpress.XtraEditors.CheckButton();
            this.BtnTickShown = new DevExpress.XtraEditors.SimpleButton();
            this.BtnUntickShown = new DevExpress.XtraEditors.SimpleButton();
            this.LblPickCount = new DevExpress.XtraEditors.LabelControl();
            this.GridCustomerPick = new DevExpress.XtraGrid.GridControl();
            this.GridViewCustomerPick = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColPickTicked = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPickName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPickCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoPickCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.BtnPickClear = new DevExpress.XtraEditors.SimpleButton();
            this.BtnPickOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateReady = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSetup = new DevExpress.XtraEditors.SimpleButton();
            this.LblShow = new DevExpress.XtraEditors.LabelControl();
            this.ChipAll = new DevExpress.XtraEditors.CheckButton();
            this.ChipReady = new DevExpress.XtraEditors.CheckButton();
            this.ChipWaiting = new DevExpress.XtraEditors.CheckButton();
            this.ChipChanged = new DevExpress.XtraEditors.CheckButton();
            this.ChipUnpriced = new DevExpress.XtraEditors.CheckButton();
            this.ChipNotTaken = new DevExpress.XtraEditors.CheckButton();
            this.ChipInvoiced = new DevExpress.XtraEditors.CheckButton();
            this.SplitMain = new DevExpress.XtraEditors.SplitContainerControl();
            this.GridContracts = new DevExpress.XtraGrid.GridControl();
            this.GridViewContractInvoices = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColCiKind = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCiDetail = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCiMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCiReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCiAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCiStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColInvoices = new DevExpress.XtraGrid.Columns.GridColumn();
            this.GridViewInvoiceLines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColIvCount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIlReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIlAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIlStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.GridInvoices = new DevExpress.XtraGrid.GridControl();
            this.GridViewInvoices = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColIvCustomer = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvContract = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvMonth = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvKind = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvDetail = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIvStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.GridViewContracts = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColHq = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCustomer = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPeriod = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBilled = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoMoney = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelDetailHead = new DevExpress.XtraEditors.PanelControl();
            this.LblDetailTitle = new DevExpress.XtraEditors.LabelControl();
            this.LblDetailSub = new DevExpress.XtraEditors.LabelControl();
            this.BtnOpenContract = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateThis = new DevExpress.XtraEditors.SimpleButton();
            this.LblSequence = new DevExpress.XtraEditors.LabelControl();
            this.BtnBillFrom = new DevExpress.XtraEditors.SimpleButton();
            this.BtnUndoSkip = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSkipMonth = new DevExpress.XtraEditors.SimpleButton();
            this.SplitDetail = new DevExpress.XtraEditors.SplitContainerControl();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMHq = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMMine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelAction = new DevExpress.XtraEditors.PanelControl();
            this.BtnApply = new DevExpress.XtraEditors.SimpleButton();
            this.BtnIgnore = new DevExpress.XtraEditors.SimpleButton();
            this.LblCustomer = new DevExpress.XtraEditors.LabelControl();
            this.SlkCustomer = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SlkCustomerView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.BtnTake = new DevExpress.XtraEditors.SimpleButton();
            this.GridReadings = new DevExpress.XtraGrid.GridControl();
            this.GridViewReadings = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColRMachine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRMeter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRLast = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRCurrent = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRCopies = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRAtHq = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRSaved = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoNum = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoReading = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoMoney2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PanelStatus = new DevExpress.XtraEditors.PanelControl();
            this.LblStatus = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelView)).BeginInit();
            this.PanelView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowEnded.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PceCustomers.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PopCustomers)).BeginInit();
            this.PopCustomers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCustomerSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridCustomerPick)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewCustomerPick)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoPickCheck)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).BeginInit();
            this.SplitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridContracts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContractInvoices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoiceLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridInvoices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoices)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailHead)).BeginInit();
            this.PanelDetailHead.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SplitDetail)).BeginInit();
            this.SplitDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelAction)).BeginInit();
            this.PanelAction.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SlkCustomer.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlkCustomerView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoReading)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).BeginInit();
            this.PanelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelHeaderTop  (native AutoCount green header; hint hidden in the ctor)
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Inter-Billing";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1280, 34);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelView  (the same view switch as the Meter Invoice Run)
            //
            this.PanelView.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelView.Appearance.Options.UseBackColor = true;
            this.PanelView.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelView.Controls.Add(this.LblView);
            this.PanelView.Controls.Add(this.BtnViewInvoices);
            this.PanelView.Controls.Add(this.BtnViewContracts);
            this.PanelView.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelView.Location = new System.Drawing.Point(0, 34);
            this.PanelView.Name = "PanelView";
            this.PanelView.Size = new System.Drawing.Size(1280, 42);
            this.PanelView.TabIndex = 5;
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
            this.BtnViewInvoices.GroupIndex = 7;
            this.BtnViewInvoices.Location = new System.Drawing.Point(56, 6);
            this.BtnViewInvoices.Name = "BtnViewInvoices";
            this.BtnViewInvoices.Size = new System.Drawing.Size(190, 30);
            this.BtnViewInvoices.TabIndex = 1;
            this.BtnViewInvoices.Text = "Invoices to generate";
            //
            // BtnViewContracts
            //
            this.BtnViewContracts.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnViewContracts.Appearance.Options.UseFont = true;
            this.BtnViewContracts.GroupIndex = 7;
            this.BtnViewContracts.Location = new System.Drawing.Point(250, 6);
            this.BtnViewContracts.Name = "BtnViewContracts";
            this.BtnViewContracts.Size = new System.Drawing.Size(190, 30);
            this.BtnViewContracts.TabIndex = 2;
            this.BtnViewContracts.TabStop = false;
            this.BtnViewContracts.Text = "Contracts";
            //
            // PanelFilter
            //
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnRefresh);
            this.PanelFilter.Controls.Add(this.BtnGenerateReady);
            this.PanelFilter.Controls.Add(this.BtnSetup);
            this.PanelFilter.Controls.Add(this.LblShow);
            this.PanelFilter.Controls.Add(this.ChipAll);
            this.PanelFilter.Controls.Add(this.ChipReady);
            this.PanelFilter.Controls.Add(this.ChipWaiting);
            this.PanelFilter.Controls.Add(this.ChipChanged);
            this.PanelFilter.Controls.Add(this.ChipUnpriced);
            this.PanelFilter.Controls.Add(this.ChipNotTaken);
            this.PanelFilter.Controls.Add(this.ChipInvoiced);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 76);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1280, 142);
            this.PanelFilter.TabIndex = 1;
            //
            // GrpFilter
            //
            this.GrpFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GrpFilter.Appearance.Options.UseFont = true;
            this.GrpFilter.Controls.Add(this.LblBook);
            this.GrpFilter.Controls.Add(this.LblConn);
            this.GrpFilter.Controls.Add(this.LblMonth);
            this.GrpFilter.Controls.Add(this.LblUpTo);
            this.GrpFilter.Controls.Add(this.ChkShowEnded);
            this.GrpFilter.Controls.Add(this.LblCustomers);
            this.GrpFilter.Controls.Add(this.PceCustomers);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(494, 128);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            //
            // LblBook
            //
            this.LblBook.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblBook.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblBook.Appearance.Options.UseFont = true;
            this.LblBook.Appearance.Options.UseForeColor = true;
            this.LblBook.Location = new System.Drawing.Point(12, 34);
            this.LblBook.Name = "LblBook";
            this.LblBook.Size = new System.Drawing.Size(22, 15);
            this.LblBook.TabIndex = 0;
            this.LblBook.Text = "HQ:";
            //
            // LblConn
            //
            this.LblConn.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblConn.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblConn.Appearance.Options.UseFont = true;
            this.LblConn.Appearance.Options.UseForeColor = true;
            this.LblConn.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblConn.Location = new System.Drawing.Point(78, 34);
            this.LblConn.Name = "LblConn";
            this.LblConn.Size = new System.Drawing.Size(404, 16);
            this.LblConn.TabIndex = 2;
            this.LblConn.Text = "";
            //
            // LblMonth
            //
            this.LblMonth.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblMonth.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblMonth.Appearance.Options.UseFont = true;
            this.LblMonth.Appearance.Options.UseForeColor = true;
            this.LblMonth.Location = new System.Drawing.Point(12, 66);
            this.LblMonth.Name = "LblMonth";
            this.LblMonth.Size = new System.Drawing.Size(41, 15);
            this.LblMonth.TabIndex = 3;
            this.LblMonth.Text = "Up to:";
            //
            // LblUpTo  (every contract is billed up to this month; nobody picks the month)
            //
            this.LblUpTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblUpTo.Appearance.Options.UseFont = true;
            this.LblUpTo.Location = new System.Drawing.Point(78, 66);
            this.LblUpTo.Name = "LblUpTo";
            this.LblUpTo.Size = new System.Drawing.Size(56, 15);
            this.LblUpTo.TabIndex = 4;
            this.LblUpTo.Text = "";
            //
            // ChkShowEnded
            //
            this.ChkShowEnded.Location = new System.Drawing.Point(290, 64);
            this.ChkShowEnded.Name = "ChkShowEnded";
            this.ChkShowEnded.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkShowEnded.Properties.Appearance.Options.UseFont = true;
            this.ChkShowEnded.Properties.Caption = "Show ended at HQ";
            this.ChkShowEnded.Size = new System.Drawing.Size(190, 20);
            this.ChkShowEnded.TabIndex = 6;
            //
            // LblCustomers
            //
            this.LblCustomers.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblCustomers.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblCustomers.Appearance.Options.UseFont = true;
            this.LblCustomers.Appearance.Options.UseForeColor = true;
            this.LblCustomers.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.LblCustomers.Location = new System.Drawing.Point(12, 91);
            this.LblCustomers.Name = "LblCustomers";
            this.LblCustomers.Size = new System.Drawing.Size(64, 30);
            this.LblCustomers.TabIndex = 7;
            this.LblCustomers.Text = "HQ customer:";
            //
            // PceCustomers  (drops PopCustomers: search, tick, OK)
            //
            this.PceCustomers.Location = new System.Drawing.Point(78, 95);
            this.PceCustomers.Name = "PceCustomers";
            this.PceCustomers.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.PceCustomers.Properties.Appearance.Options.UseFont = true;
            this.PceCustomers.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.PceCustomers.Properties.NullText = "All HQ customers";
            this.PceCustomers.Properties.PopupControl = this.PopCustomers;
            this.PceCustomers.Properties.ShowPopupCloseButton = false;
            this.PceCustomers.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.PceCustomers.Size = new System.Drawing.Size(404, 22);
            this.PceCustomers.TabIndex = 8;
            //
            // PopCustomers
            //
            this.PopCustomers.Controls.Add(this.TxtCustomerSearch);
            this.PopCustomers.Controls.Add(this.ChkTickedOnly);
            this.PopCustomers.Controls.Add(this.BtnTickShown);
            this.PopCustomers.Controls.Add(this.BtnUntickShown);
            this.PopCustomers.Controls.Add(this.LblPickCount);
            this.PopCustomers.Controls.Add(this.GridCustomerPick);
            this.PopCustomers.Controls.Add(this.BtnPickClear);
            this.PopCustomers.Controls.Add(this.BtnPickOk);
            this.PopCustomers.Location = new System.Drawing.Point(560, 220);
            this.PopCustomers.Name = "PopCustomers";
            this.PopCustomers.Size = new System.Drawing.Size(560, 480);
            this.PopCustomers.TabIndex = 9;
            //
            // TxtCustomerSearch
            //
            this.TxtCustomerSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtCustomerSearch.Location = new System.Drawing.Point(8, 8);
            this.TxtCustomerSearch.Name = "TxtCustomerSearch";
            this.TxtCustomerSearch.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.TxtCustomerSearch.Properties.Appearance.Options.UseFont = true;
            this.TxtCustomerSearch.Properties.NullValuePrompt = "Search name or code";
            this.TxtCustomerSearch.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtCustomerSearch.Size = new System.Drawing.Size(424, 24);
            this.TxtCustomerSearch.TabIndex = 0;
            //
            // ChkTickedOnly
            //
            this.ChkTickedOnly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ChkTickedOnly.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkTickedOnly.Appearance.Options.UseFont = true;
            this.ChkTickedOnly.Location = new System.Drawing.Point(440, 7);
            this.ChkTickedOnly.Name = "ChkTickedOnly";
            this.ChkTickedOnly.Size = new System.Drawing.Size(112, 26);
            this.ChkTickedOnly.TabIndex = 1;
            this.ChkTickedOnly.Text = "Ticked only";
            //
            // BtnTickShown
            //
            this.BtnTickShown.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnTickShown.Appearance.Options.UseFont = true;
            this.BtnTickShown.Location = new System.Drawing.Point(8, 40);
            this.BtnTickShown.Name = "BtnTickShown";
            this.BtnTickShown.Size = new System.Drawing.Size(110, 26);
            this.BtnTickShown.TabIndex = 2;
            this.BtnTickShown.Text = "Tick shown";
            //
            // BtnUntickShown
            //
            this.BtnUntickShown.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnUntickShown.Appearance.Options.UseFont = true;
            this.BtnUntickShown.Location = new System.Drawing.Point(124, 40);
            this.BtnUntickShown.Name = "BtnUntickShown";
            this.BtnUntickShown.Size = new System.Drawing.Size(110, 26);
            this.BtnUntickShown.TabIndex = 3;
            this.BtnUntickShown.Text = "Untick shown";
            //
            // LblPickCount
            //
            this.LblPickCount.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.LblPickCount.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblPickCount.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblPickCount.Appearance.Options.UseFont = true;
            this.LblPickCount.Appearance.Options.UseForeColor = true;
            this.LblPickCount.Appearance.Options.UseTextOptions = true;
            this.LblPickCount.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.LblPickCount.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblPickCount.Location = new System.Drawing.Point(244, 45);
            this.LblPickCount.Name = "LblPickCount";
            this.LblPickCount.Size = new System.Drawing.Size(308, 16);
            this.LblPickCount.TabIndex = 4;
            this.LblPickCount.Text = "";
            //
            // GridCustomerPick
            //
            this.GridCustomerPick.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.GridCustomerPick.Location = new System.Drawing.Point(8, 72);
            this.GridCustomerPick.MainView = this.GridViewCustomerPick;
            this.GridCustomerPick.Name = "GridCustomerPick";
            this.GridCustomerPick.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoPickCheck});
            this.GridCustomerPick.Size = new System.Drawing.Size(544, 364);
            this.GridCustomerPick.TabIndex = 5;
            this.GridCustomerPick.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewCustomerPick});
            //
            // GridViewCustomerPick  (a click anywhere on a row ticks it)
            //
            this.GridViewCustomerPick.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewCustomerPick.Appearance.Row.Options.UseFont = true;
            this.GridViewCustomerPick.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColPickTicked,
            this.ColPickName,
            this.ColPickCode});
            this.GridViewCustomerPick.GridControl = this.GridCustomerPick;
            this.GridViewCustomerPick.Name = "GridViewCustomerPick";
            this.GridViewCustomerPick.OptionsBehavior.Editable = false;
            this.GridViewCustomerPick.OptionsCustomization.AllowFilter = false;
            this.GridViewCustomerPick.OptionsCustomization.AllowGroup = false;
            this.GridViewCustomerPick.OptionsMenu.EnableColumnMenu = false;
            this.GridViewCustomerPick.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewCustomerPick.OptionsView.ShowGroupPanel = false;
            this.GridViewCustomerPick.OptionsView.ShowIndicator = false;
            //
            // ColPickTicked
            //
            this.ColPickTicked.Caption = " ";
            this.ColPickTicked.ColumnEdit = this.RepoPickCheck;
            this.ColPickTicked.FieldName = "Ticked";
            this.ColPickTicked.Name = "ColPickTicked";
            this.ColPickTicked.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.ColPickTicked.OptionsColumn.FixedWidth = true;
            this.ColPickTicked.Visible = true;
            this.ColPickTicked.VisibleIndex = 0;
            this.ColPickTicked.Width = 32;
            //
            // ColPickName
            //
            this.ColPickName.Caption = "Customer";
            this.ColPickName.FieldName = "Name";
            this.ColPickName.Name = "ColPickName";
            this.ColPickName.Visible = true;
            this.ColPickName.VisibleIndex = 1;
            this.ColPickName.Width = 380;
            //
            // ColPickCode
            //
            this.ColPickCode.Caption = "Code";
            this.ColPickCode.FieldName = "Code";
            this.ColPickCode.Name = "ColPickCode";
            this.ColPickCode.Visible = true;
            this.ColPickCode.VisibleIndex = 2;
            this.ColPickCode.Width = 110;
            //
            // RepoPickCheck
            //
            this.RepoPickCheck.AutoHeight = false;
            this.RepoPickCheck.Name = "RepoPickCheck";
            //
            // BtnPickClear
            //
            this.BtnPickClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnPickClear.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnPickClear.Appearance.Options.UseFont = true;
            this.BtnPickClear.Location = new System.Drawing.Point(8, 444);
            this.BtnPickClear.Name = "BtnPickClear";
            this.BtnPickClear.Size = new System.Drawing.Size(110, 28);
            this.BtnPickClear.TabIndex = 6;
            this.BtnPickClear.Text = "Untick all";
            //
            // BtnPickOk
            //
            this.BtnPickOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnPickOk.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnPickOk.Appearance.Options.UseFont = true;
            this.BtnPickOk.Location = new System.Drawing.Point(442, 444);
            this.BtnPickOk.Name = "BtnPickOk";
            this.BtnPickOk.Size = new System.Drawing.Size(110, 28);
            this.BtnPickOk.TabIndex = 7;
            this.BtnPickOk.Text = "OK";
            //
            // BtnRefresh
            //
            this.BtnRefresh.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnRefresh.Appearance.Options.UseFont = true;
            this.BtnRefresh.Location = new System.Drawing.Point(510, 8);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(146, 50);
            this.BtnRefresh.TabIndex = 1;
            this.BtnRefresh.Text = "Refresh";
            //
            // BtnGenerateReady  (red bold, same style as Generate on the Meter Invoice Run)
            //
            this.BtnGenerateReady.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnGenerateReady.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnGenerateReady.Appearance.Options.UseFont = true;
            this.BtnGenerateReady.Appearance.Options.UseForeColor = true;
            this.BtnGenerateReady.Location = new System.Drawing.Point(664, 8);
            this.BtnGenerateReady.Name = "BtnGenerateReady";
            this.BtnGenerateReady.Size = new System.Drawing.Size(196, 50);
            this.BtnGenerateReady.TabIndex = 2;
            this.BtnGenerateReady.Text = "Generate all ready";
            //
            // BtnSetup
            //
            this.BtnSetup.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSetup.Appearance.Options.UseFont = true;
            this.BtnSetup.Location = new System.Drawing.Point(868, 8);
            this.BtnSetup.Name = "BtnSetup";
            this.BtnSetup.Size = new System.Drawing.Size(146, 50);
            this.BtnSetup.TabIndex = 3;
            this.BtnSetup.Text = "Setup";
            //
            // LblShow
            //
            this.LblShow.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblShow.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblShow.Appearance.Options.UseFont = true;
            this.LblShow.Appearance.Options.UseForeColor = true;
            this.LblShow.Location = new System.Drawing.Point(510, 76);
            this.LblShow.Name = "LblShow";
            this.LblShow.Size = new System.Drawing.Size(34, 15);
            this.LblShow.TabIndex = 4;
            this.LblShow.Text = "Show:";
            //
            // ChipAll
            //
            this.ChipAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipAll.Appearance.Options.UseFont = true;
            this.ChipAll.Checked = true;
            this.ChipAll.GroupIndex = 1;
            this.ChipAll.Location = new System.Drawing.Point(552, 70);
            this.ChipAll.Name = "ChipAll";
            this.ChipAll.Size = new System.Drawing.Size(70, 28);
            this.ChipAll.TabIndex = 5;
            this.ChipAll.Text = "All";
            //
            // ChipReady
            //
            this.ChipReady.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipReady.Appearance.Options.UseFont = true;
            this.ChipReady.GroupIndex = 1;
            this.ChipReady.Location = new System.Drawing.Point(626, 70);
            this.ChipReady.Name = "ChipReady";
            this.ChipReady.Size = new System.Drawing.Size(86, 28);
            this.ChipReady.TabIndex = 6;
            this.ChipReady.TabStop = false;
            this.ChipReady.Text = "Ready";
            //
            // ChipWaiting
            //
            this.ChipWaiting.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipWaiting.Appearance.Options.UseFont = true;
            this.ChipWaiting.GroupIndex = 1;
            this.ChipWaiting.Location = new System.Drawing.Point(716, 70);
            this.ChipWaiting.Name = "ChipWaiting";
            this.ChipWaiting.Size = new System.Drawing.Size(146, 28);
            this.ChipWaiting.TabIndex = 7;
            this.ChipWaiting.TabStop = false;
            this.ChipWaiting.Text = "Waiting HQ reading";
            //
            // ChipChanged
            //
            this.ChipChanged.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipChanged.Appearance.Options.UseFont = true;
            this.ChipChanged.GroupIndex = 1;
            this.ChipChanged.Location = new System.Drawing.Point(866, 70);
            this.ChipChanged.Name = "ChipChanged";
            this.ChipChanged.Size = new System.Drawing.Size(118, 28);
            this.ChipChanged.TabIndex = 8;
            this.ChipChanged.TabStop = false;
            this.ChipChanged.Text = "Changed at HQ";
            //
            // ChipUnpriced
            //
            this.ChipUnpriced.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipUnpriced.Appearance.Options.UseFont = true;
            this.ChipUnpriced.GroupIndex = 1;
            this.ChipUnpriced.Location = new System.Drawing.Point(988, 70);
            this.ChipUnpriced.Name = "ChipUnpriced";
            this.ChipUnpriced.Size = new System.Drawing.Size(96, 28);
            this.ChipUnpriced.TabIndex = 9;
            this.ChipUnpriced.TabStop = false;
            this.ChipUnpriced.Text = "Unpriced";
            //
            // ChipNotTaken
            //
            this.ChipNotTaken.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipNotTaken.Appearance.Options.UseFont = true;
            this.ChipNotTaken.GroupIndex = 1;
            this.ChipNotTaken.Location = new System.Drawing.Point(1088, 70);
            this.ChipNotTaken.Name = "ChipNotTaken";
            this.ChipNotTaken.Size = new System.Drawing.Size(84, 28);
            this.ChipNotTaken.TabIndex = 10;
            this.ChipNotTaken.TabStop = false;
            this.ChipNotTaken.Text = "Not taken";
            //
            // ChipInvoiced
            //
            this.ChipInvoiced.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChipInvoiced.Appearance.Options.UseFont = true;
            this.ChipInvoiced.GroupIndex = 1;
            this.ChipInvoiced.Location = new System.Drawing.Point(1176, 70);
            this.ChipInvoiced.Name = "ChipInvoiced";
            this.ChipInvoiced.Size = new System.Drawing.Size(100, 28);
            this.ChipInvoiced.TabIndex = 11;
            this.ChipInvoiced.TabStop = false;
            this.ChipInvoiced.Text = "Up to date";
            //
            // SplitMain
            //
            this.SplitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitMain.Location = new System.Drawing.Point(0, 218);
            this.SplitMain.Name = "SplitMain";
            this.SplitMain.Panel1.Controls.Add(this.GridContracts);
            this.SplitMain.Panel1.Controls.Add(this.GridInvoices);
            this.SplitMain.Panel1.Text = "Panel1";
            this.SplitMain.Panel2.Controls.Add(this.SplitDetail);
            this.SplitMain.Panel2.Controls.Add(this.PanelDetailHead);
            this.SplitMain.Panel2.Text = "Panel2";
            this.SplitMain.Size = new System.Drawing.Size(1280, 558);
            this.SplitMain.SplitterPosition = 640;
            this.SplitMain.TabIndex = 2;
            //
            // GridContracts
            //
            this.GridContracts.Dock = System.Windows.Forms.DockStyle.Fill;
            gridLevelNode1.LevelTemplate = this.GridViewContractInvoices;
            gridLevelNode1.RelationName = "Invoices";
            this.GridContracts.LevelTree.Nodes.AddRange(new DevExpress.XtraGrid.GridLevelNode[] {
            gridLevelNode1});
            this.GridContracts.Location = new System.Drawing.Point(0, 0);
            this.GridContracts.MainView = this.GridViewContracts;
            this.GridContracts.Name = "GridContracts";
            this.GridContracts.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoMoney});
            this.GridContracts.Size = new System.Drawing.Size(640, 558);
            this.GridContracts.TabIndex = 0;
            this.GridContracts.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewContractInvoices,
            this.GridViewContracts});
            //
            // GridViewContracts
            //
            this.GridViewContracts.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewContracts.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewContracts.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewContracts.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewContracts.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewContracts.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewContracts.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewContracts.Appearance.Row.Options.UseFont = true;
            this.GridViewContracts.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewContracts.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewContracts.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewContracts.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewContracts.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewContracts.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewContracts.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewContracts.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewContracts.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewContracts.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewContracts.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColHq,
            this.ColMine,
            this.ColCustomer,
            this.ColPeriod,
            this.ColBilled,
            this.ColDue,
            this.ColInvoices,
            this.ColMachines,
            this.ColReadings,
            this.ColAmount,
            this.ColStatus});
            this.GridViewContracts.ColumnPanelRowHeight = 28;
            this.GridViewContracts.GridControl = this.GridContracts;
            this.GridViewContracts.Name = "GridViewContracts";
            this.GridViewContracts.OptionsBehavior.Editable = false;
            this.GridViewContracts.OptionsDetail.ShowDetailTabs = false;
            this.GridViewContracts.OptionsDetail.SmartDetailExpandButtonMode = DevExpress.XtraGrid.Views.Grid.DetailExpandButtonMode.CheckAllDetails;
            this.GridViewContracts.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewContracts.OptionsView.ColumnAutoWidth = true;
            this.GridViewContracts.OptionsView.ShowGroupPanel = false;
            this.GridViewContracts.OptionsView.ShowIndicator = false;
            this.GridViewContracts.OptionsView.ShowViewCaption = true;
            this.GridViewContracts.RowHeight = 24;
            this.GridViewContracts.ViewCaption = "HQ contracts";
            //
            // ColHq
            //
            this.ColHq.Caption = "HQ contract";
            this.ColHq.FieldName = "HqContractNo";
            this.ColHq.Name = "ColHq";
            this.ColHq.Visible = true;
            this.ColHq.VisibleIndex = 0;
            this.ColHq.Width = 116;
            //
            // ColMine
            //
            this.ColMine.Caption = "My contract";
            this.ColMine.FieldName = "LocalNo";
            this.ColMine.Name = "ColMine";
            this.ColMine.Visible = true;
            this.ColMine.VisibleIndex = 1;
            this.ColMine.Width = 100;
            //
            // ColCustomer
            //
            this.ColCustomer.Caption = "Customer";
            this.ColCustomer.FieldName = "Customer";
            this.ColCustomer.Name = "ColCustomer";
            this.ColCustomer.Visible = true;
            this.ColCustomer.VisibleIndex = 2;
            this.ColCustomer.Width = 130;
            //
            // ColPeriod  (the month the contract is at)
            //
            this.ColPeriod.Caption = "Month";
            this.ColPeriod.FieldName = "PeriodText";
            this.ColPeriod.Name = "ColPeriod";
            this.ColPeriod.Visible = true;
            this.ColPeriod.VisibleIndex = 3;
            this.ColPeriod.Width = 72;
            //
            // ColBilled  (months billed or skipped / months in the contract)
            //
            this.ColBilled.AppearanceCell.Options.UseTextOptions = true;
            this.ColBilled.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColBilled.Caption = "Billed";
            this.ColBilled.FieldName = "Progress";
            this.ColBilled.Name = "ColBilled";
            this.ColBilled.Visible = true;
            this.ColBilled.VisibleIndex = 4;
            this.ColBilled.Width = 56;
            //
            // ColDue  (months still to bill up to this month)
            //
            this.ColDue.AppearanceCell.Options.UseTextOptions = true;
            this.ColDue.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColDue.Caption = "Due";
            this.ColDue.FieldName = "Due";
            this.ColDue.Name = "ColDue";
            this.ColDue.Visible = true;
            this.ColDue.VisibleIndex = 5;
            this.ColDue.Width = 44;
            //
            // ColInvoices  (invoices made / invoices this month; expand the row to see them)
            //
            this.ColInvoices.AppearanceCell.Options.UseTextOptions = true;
            this.ColInvoices.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColInvoices.Caption = "Invoices";
            this.ColInvoices.FieldName = "InvoiceProgress";
            this.ColInvoices.Name = "ColInvoices";
            this.ColInvoices.Visible = true;
            this.ColInvoices.VisibleIndex = 6;
            this.ColInvoices.Width = 60;
            //
            // GridViewContractInvoices  (a contract's invoices, under its row)
            //
            this.GridViewContractInvoices.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewContractInvoices.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewContractInvoices.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewContractInvoices.Appearance.Row.Options.UseFont = true;
            this.GridViewContractInvoices.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewContractInvoices.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewContractInvoices.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewContractInvoices.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewContractInvoices.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColCiKind,
            this.ColCiDetail,
            this.ColCiMachines,
            this.ColCiReadings,
            this.ColCiAmount,
            this.ColCiStatus});
            this.GridViewContractInvoices.GridControl = this.GridContracts;
            this.GridViewContractInvoices.Name = "GridViewContractInvoices";
            this.GridViewContractInvoices.OptionsBehavior.Editable = false;
            this.GridViewContractInvoices.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewContractInvoices.OptionsView.ColumnAutoWidth = true;
            this.GridViewContractInvoices.OptionsView.ShowGroupPanel = false;
            this.GridViewContractInvoices.OptionsView.ShowIndicator = false;
            this.GridViewContractInvoices.RowHeight = 22;
            //
            // ColCiKind
            //
            this.ColCiKind.Caption = "Invoice";
            this.ColCiKind.FieldName = "Kind";
            this.ColCiKind.Name = "ColCiKind";
            this.ColCiKind.Visible = true;
            this.ColCiKind.VisibleIndex = 0;
            this.ColCiKind.Width = 116;
            //
            // ColCiDetail
            //
            this.ColCiDetail.Caption = " ";
            this.ColCiDetail.FieldName = "Detail";
            this.ColCiDetail.Name = "ColCiDetail";
            this.ColCiDetail.Visible = true;
            this.ColCiDetail.VisibleIndex = 1;
            this.ColCiDetail.Width = 80;
            //
            // ColCiMachines
            //
            this.ColCiMachines.AppearanceCell.Options.UseTextOptions = true;
            this.ColCiMachines.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColCiMachines.Caption = "Machines";
            this.ColCiMachines.FieldName = "Machines";
            this.ColCiMachines.Name = "ColCiMachines";
            this.ColCiMachines.Visible = true;
            this.ColCiMachines.VisibleIndex = 2;
            this.ColCiMachines.Width = 64;
            //
            // ColCiReadings
            //
            this.ColCiReadings.AppearanceCell.Options.UseTextOptions = true;
            this.ColCiReadings.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColCiReadings.Caption = "HQ readings";
            this.ColCiReadings.FieldName = "Readings";
            this.ColCiReadings.Name = "ColCiReadings";
            this.ColCiReadings.Visible = true;
            this.ColCiReadings.VisibleIndex = 3;
            this.ColCiReadings.Width = 80;
            //
            // ColCiAmount
            //
            this.ColCiAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColCiAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColCiAmount.Caption = "Amount (RM)";
            this.ColCiAmount.DisplayFormat.FormatString = "n2";
            this.ColCiAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCiAmount.FieldName = "Amount";
            this.ColCiAmount.Name = "ColCiAmount";
            this.ColCiAmount.Visible = true;
            this.ColCiAmount.VisibleIndex = 4;
            this.ColCiAmount.Width = 96;
            //
            // ColCiStatus
            //
            this.ColCiStatus.Caption = "Status";
            this.ColCiStatus.FieldName = "StatusText";
            this.ColCiStatus.Name = "ColCiStatus";
            this.ColCiStatus.Visible = true;
            this.ColCiStatus.VisibleIndex = 5;
            this.ColCiStatus.Width = 170;
            //
            // ColMachines
            //
            this.ColMachines.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColMachines.AppearanceCell.Options.UseTextOptions = true;
            this.ColMachines.Caption = "Machines";
            this.ColMachines.FieldName = "Machines";
            this.ColMachines.Name = "ColMachines";
            this.ColMachines.Visible = false;
            this.ColMachines.Width = 64;
            //
            // ColReadings
            //
            this.ColReadings.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColReadings.AppearanceCell.Options.UseTextOptions = true;
            this.ColReadings.Caption = "HQ readings";
            this.ColReadings.FieldName = "Readings";
            this.ColReadings.Name = "ColReadings";
            this.ColReadings.Visible = true;
            this.ColReadings.VisibleIndex = 7;
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
            this.ColAmount.Visible = true;
            this.ColAmount.VisibleIndex = 8;
            this.ColAmount.Width = 96;
            //
            // ColStatus
            //
            this.ColStatus.Caption = "Status";
            this.ColStatus.FieldName = "StatusText";
            this.ColStatus.Name = "ColStatus";
            this.ColStatus.Visible = true;
            this.ColStatus.VisibleIndex = 9;
            this.ColStatus.Width = 140;
            //
            // GridInvoices  (the board as invoices: a checklist, Invoiced once made)
            //
            this.GridInvoices.Dock = System.Windows.Forms.DockStyle.Fill;
            gridLevelNode2.LevelTemplate = this.GridViewInvoiceLines;
            gridLevelNode2.RelationName = "Invoices";
            this.GridInvoices.LevelTree.Nodes.AddRange(new DevExpress.XtraGrid.GridLevelNode[] {
            gridLevelNode2});
            this.GridInvoices.Location = new System.Drawing.Point(0, 0);
            this.GridInvoices.MainView = this.GridViewInvoices;
            this.GridInvoices.Name = "GridInvoices";
            this.GridInvoices.Size = new System.Drawing.Size(640, 558);
            this.GridInvoices.TabIndex = 1;
            this.GridInvoices.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewInvoiceLines,
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
            this.ColIvCustomer,
            this.ColIvContract,
            this.ColIvMonth,
            this.ColIvCount,
            this.ColIvReadings,
            this.ColIvAmount,
            this.ColIvStatus});
            this.GridViewInvoices.ColumnPanelRowHeight = 28;
            this.GridViewInvoices.GridControl = this.GridInvoices;
            this.GridViewInvoices.Name = "GridViewInvoices";
            this.GridViewInvoices.OptionsBehavior.Editable = false;
            this.GridViewInvoices.OptionsDetail.ShowDetailTabs = false;
            this.GridViewInvoices.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewInvoices.OptionsView.ColumnAutoWidth = true;
            this.GridViewInvoices.OptionsView.ShowGroupPanel = false;
            this.GridViewInvoices.OptionsView.ShowIndicator = false;
            this.GridViewInvoices.OptionsView.ShowViewCaption = true;
            this.GridViewInvoices.RowHeight = 24;
            this.GridViewInvoices.ViewCaption = "Invoices";
            //
            // ColIvCustomer
            //
            this.ColIvCustomer.Caption = "Customer";
            this.ColIvCustomer.FieldName = "Customer";
            this.ColIvCustomer.Name = "ColIvCustomer";
            this.ColIvCustomer.Visible = true;
            this.ColIvCustomer.VisibleIndex = 0;
            this.ColIvCustomer.Width = 140;
            //
            // ColIvContract
            //
            this.ColIvContract.Caption = "Contract";
            this.ColIvContract.FieldName = "ContractNo";
            this.ColIvContract.Name = "ColIvContract";
            this.ColIvContract.Visible = true;
            this.ColIvContract.VisibleIndex = 1;
            this.ColIvContract.Width = 100;
            //
            // ColIvMonth
            //
            this.ColIvMonth.Caption = "Month";
            this.ColIvMonth.FieldName = "PeriodText";
            this.ColIvMonth.Name = "ColIvMonth";
            this.ColIvMonth.Visible = true;
            this.ColIvMonth.VisibleIndex = 2;
            this.ColIvMonth.Width = 72;
            //
            // ColIvCount  (invoices made / invoices this month; expand the row to see them)
            //
            this.ColIvCount.AppearanceCell.Options.UseTextOptions = true;
            this.ColIvCount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIvCount.Caption = "Invoices";
            this.ColIvCount.FieldName = "InvoiceProgress";
            this.ColIvCount.Name = "ColIvCount";
            this.ColIvCount.Visible = true;
            this.ColIvCount.VisibleIndex = 3;
            this.ColIvCount.Width = 64;
            //
            // GridViewInvoiceLines  (a contract's invoices, under its row on the invoice list)
            //
            this.GridViewInvoiceLines.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewInvoiceLines.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewInvoiceLines.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewInvoiceLines.Appearance.Row.Options.UseFont = true;
            this.GridViewInvoiceLines.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewInvoiceLines.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewInvoiceLines.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewInvoiceLines.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewInvoiceLines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColIvKind,
            this.ColIvDetail,
            this.ColIvMachines,
            this.ColIlReadings,
            this.ColIlAmount,
            this.ColIlStatus});
            this.GridViewInvoiceLines.GridControl = this.GridInvoices;
            this.GridViewInvoiceLines.Name = "GridViewInvoiceLines";
            this.GridViewInvoiceLines.OptionsBehavior.Editable = false;
            this.GridViewInvoiceLines.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewInvoiceLines.OptionsView.ColumnAutoWidth = true;
            this.GridViewInvoiceLines.OptionsView.ShowGroupPanel = false;
            this.GridViewInvoiceLines.OptionsView.ShowIndicator = false;
            this.GridViewInvoiceLines.RowHeight = 22;
            //
            // ColIlReadings
            //
            this.ColIlReadings.AppearanceCell.Options.UseTextOptions = true;
            this.ColIlReadings.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIlReadings.Caption = "HQ readings";
            this.ColIlReadings.FieldName = "Readings";
            this.ColIlReadings.Name = "ColIlReadings";
            this.ColIlReadings.Visible = true;
            this.ColIlReadings.VisibleIndex = 3;
            this.ColIlReadings.Width = 80;
            //
            // ColIlAmount
            //
            this.ColIlAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColIlAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColIlAmount.Caption = "Amount (RM)";
            this.ColIlAmount.DisplayFormat.FormatString = "n2";
            this.ColIlAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColIlAmount.FieldName = "Amount";
            this.ColIlAmount.Name = "ColIlAmount";
            this.ColIlAmount.Visible = true;
            this.ColIlAmount.VisibleIndex = 4;
            this.ColIlAmount.Width = 96;
            //
            // ColIlStatus
            //
            this.ColIlStatus.Caption = "Status";
            this.ColIlStatus.FieldName = "StatusText";
            this.ColIlStatus.Name = "ColIlStatus";
            this.ColIlStatus.Visible = true;
            this.ColIlStatus.VisibleIndex = 5;
            this.ColIlStatus.Width = 170;
            //
            // ColIvKind
            //
            this.ColIvKind.Caption = "Invoice";
            this.ColIvKind.FieldName = "Kind";
            this.ColIvKind.Name = "ColIvKind";
            this.ColIvKind.Visible = true;
            this.ColIvKind.VisibleIndex = 0;
            this.ColIvKind.Width = 116;
            //
            // ColIvDetail
            //
            this.ColIvDetail.Caption = " ";
            this.ColIvDetail.FieldName = "Detail";
            this.ColIvDetail.Name = "ColIvDetail";
            this.ColIvDetail.Visible = true;
            this.ColIvDetail.VisibleIndex = 1;
            this.ColIvDetail.Width = 70;
            //
            // ColIvMachines
            //
            this.ColIvMachines.AppearanceCell.Options.UseTextOptions = true;
            this.ColIvMachines.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIvMachines.Caption = "Machines";
            this.ColIvMachines.FieldName = "Machines";
            this.ColIvMachines.Name = "ColIvMachines";
            this.ColIvMachines.Visible = true;
            this.ColIvMachines.VisibleIndex = 2;
            this.ColIvMachines.Width = 64;
            //
            // ColIvReadings
            //
            this.ColIvReadings.AppearanceCell.Options.UseTextOptions = true;
            this.ColIvReadings.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIvReadings.Caption = "HQ readings";
            this.ColIvReadings.FieldName = "Readings";
            this.ColIvReadings.Name = "ColIvReadings";
            this.ColIvReadings.Visible = true;
            this.ColIvReadings.VisibleIndex = 4;
            this.ColIvReadings.Width = 80;
            //
            // ColIvAmount
            //
            this.ColIvAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColIvAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColIvAmount.Caption = "Amount (RM)";
            this.ColIvAmount.DisplayFormat.FormatString = "n2";
            this.ColIvAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColIvAmount.FieldName = "Amount";
            this.ColIvAmount.Name = "ColIvAmount";
            this.ColIvAmount.Visible = true;
            this.ColIvAmount.VisibleIndex = 5;
            this.ColIvAmount.Width = 96;
            //
            // ColIvStatus
            //
            this.ColIvStatus.Caption = "Status";
            this.ColIvStatus.FieldName = "StatusText";
            this.ColIvStatus.Name = "ColIvStatus";
            this.ColIvStatus.Visible = true;
            this.ColIvStatus.VisibleIndex = 6;
            this.ColIvStatus.Width = 170;
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
            this.PanelDetailHead.Controls.Add(this.BtnOpenContract);
            this.PanelDetailHead.Controls.Add(this.BtnGenerateThis);
            this.PanelDetailHead.Controls.Add(this.LblSequence);
            this.PanelDetailHead.Controls.Add(this.BtnBillFrom);
            this.PanelDetailHead.Controls.Add(this.BtnUndoSkip);
            this.PanelDetailHead.Controls.Add(this.BtnSkipMonth);
            this.PanelDetailHead.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelDetailHead.Location = new System.Drawing.Point(0, 0);
            this.PanelDetailHead.Name = "PanelDetailHead";
            this.PanelDetailHead.Size = new System.Drawing.Size(630, 104);
            this.PanelDetailHead.TabIndex = 0;
            //
            // LblDetailTitle
            //
            this.LblDetailTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblDetailTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblDetailTitle.Appearance.Options.UseFont = true;
            this.LblDetailTitle.Appearance.Options.UseForeColor = true;
            this.LblDetailTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblDetailTitle.Location = new System.Drawing.Point(12, 10);
            this.LblDetailTitle.Name = "LblDetailTitle";
            this.LblDetailTitle.Size = new System.Drawing.Size(240, 24);
            this.LblDetailTitle.TabIndex = 0;
            this.LblDetailTitle.Text = "";
            //
            // LblDetailSub
            //
            this.LblDetailSub.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDetailSub.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDetailSub.Appearance.Options.UseFont = true;
            this.LblDetailSub.Appearance.Options.UseForeColor = true;
            this.LblDetailSub.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblDetailSub.Location = new System.Drawing.Point(12, 38);
            this.LblDetailSub.Name = "LblDetailSub";
            this.LblDetailSub.Size = new System.Drawing.Size(240, 16);
            this.LblDetailSub.TabIndex = 1;
            this.LblDetailSub.Text = "";
            //
            // BtnOpenContract
            //
            this.BtnOpenContract.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOpenContract.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnOpenContract.Appearance.Options.UseFont = true;
            this.BtnOpenContract.Location = new System.Drawing.Point(290, 10);
            this.BtnOpenContract.Name = "BtnOpenContract";
            this.BtnOpenContract.Size = new System.Drawing.Size(130, 50);
            this.BtnOpenContract.TabIndex = 2;
            this.BtnOpenContract.Text = "Open contract";
            //
            // BtnGenerateThis
            //
            this.BtnGenerateThis.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnGenerateThis.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.BtnGenerateThis.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnGenerateThis.Appearance.Options.UseFont = true;
            this.BtnGenerateThis.Appearance.Options.UseForeColor = true;
            this.BtnGenerateThis.Location = new System.Drawing.Point(426, 10);
            this.BtnGenerateThis.Name = "BtnGenerateThis";
            this.BtnGenerateThis.Size = new System.Drawing.Size(196, 50);
            this.BtnGenerateThis.TabIndex = 3;
            this.BtnGenerateThis.Text = "Generate this invoice";
            //
            // LblSequence
            //
            this.LblSequence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.LblSequence.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblSequence.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.LblSequence.Appearance.Options.UseFont = true;
            this.LblSequence.Appearance.Options.UseForeColor = true;
            this.LblSequence.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSequence.Location = new System.Drawing.Point(12, 75);
            this.LblSequence.Name = "LblSequence";
            this.LblSequence.Size = new System.Drawing.Size(232, 16);
            this.LblSequence.TabIndex = 4;
            this.LblSequence.Text = "";
            //
            // BtnBillFrom
            //
            this.BtnBillFrom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnBillFrom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnBillFrom.Appearance.Options.UseFont = true;
            this.BtnBillFrom.Location = new System.Drawing.Point(250, 70);
            this.BtnBillFrom.Name = "BtnBillFrom";
            this.BtnBillFrom.Size = new System.Drawing.Size(100, 26);
            this.BtnBillFrom.TabIndex = 5;
            this.BtnBillFrom.Text = "Bill from";
            //
            // BtnUndoSkip
            //
            this.BtnUndoSkip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnUndoSkip.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnUndoSkip.Appearance.Options.UseFont = true;
            this.BtnUndoSkip.Location = new System.Drawing.Point(356, 70);
            this.BtnUndoSkip.Name = "BtnUndoSkip";
            this.BtnUndoSkip.Size = new System.Drawing.Size(130, 26);
            this.BtnUndoSkip.TabIndex = 6;
            this.BtnUndoSkip.Text = "Undo skip";
            //
            // BtnSkipMonth
            //
            this.BtnSkipMonth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnSkipMonth.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSkipMonth.Appearance.Options.UseFont = true;
            this.BtnSkipMonth.Location = new System.Drawing.Point(492, 70);
            this.BtnSkipMonth.Name = "BtnSkipMonth";
            this.BtnSkipMonth.Size = new System.Drawing.Size(130, 26);
            this.BtnSkipMonth.TabIndex = 7;
            this.BtnSkipMonth.Text = "Skip month";
            //
            // SplitDetail
            //
            this.SplitDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SplitDetail.Horizontal = false;
            this.SplitDetail.Location = new System.Drawing.Point(0, 104);
            this.SplitDetail.Name = "SplitDetail";
            this.SplitDetail.Panel1.Controls.Add(this.GridMachines);
            this.SplitDetail.Panel1.Controls.Add(this.PanelAction);
            this.SplitDetail.Panel1.Text = "Panel1";
            this.SplitDetail.Panel2.Controls.Add(this.GridReadings);
            this.SplitDetail.Panel2.Text = "Panel2";
            this.SplitDetail.Size = new System.Drawing.Size(630, 488);
            this.SplitDetail.SplitterPosition = 220;
            this.SplitDetail.TabIndex = 1;
            //
            // GridMachines
            //
            this.GridMachines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridMachines.Location = new System.Drawing.Point(0, 0);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.Size = new System.Drawing.Size(630, 180);
            this.GridMachines.TabIndex = 0;
            this.GridMachines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMachines});
            //
            // GridViewMachines
            //
            this.GridViewMachines.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewMachines.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewMachines.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewMachines.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewMachines.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewMachines.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewMachines.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewMachines.Appearance.Row.Options.UseFont = true;
            this.GridViewMachines.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewMachines.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewMachines.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewMachines.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewMachines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColMSerial,
            this.ColMModel,
            this.ColMHq,
            this.ColMMine,
            this.ColMStatus});
            this.GridViewMachines.ColumnPanelRowHeight = 26;
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.Editable = false;
            this.GridViewMachines.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewMachines.OptionsView.ColumnAutoWidth = true;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
            this.GridViewMachines.OptionsView.ShowIndicator = false;
            this.GridViewMachines.OptionsView.ShowViewCaption = true;
            this.GridViewMachines.RowHeight = 22;
            this.GridViewMachines.ViewCaption = "Machines";
            //
            // ColMSerial
            //
            this.ColMSerial.Caption = "Serial";
            this.ColMSerial.FieldName = "Serial";
            this.ColMSerial.Name = "ColMSerial";
            this.ColMSerial.Visible = true;
            this.ColMSerial.VisibleIndex = 0;
            this.ColMSerial.Width = 90;
            //
            // ColMModel
            //
            this.ColMModel.Caption = "Model";
            this.ColMModel.FieldName = "Model";
            this.ColMModel.Name = "ColMModel";
            this.ColMModel.Visible = true;
            this.ColMModel.VisibleIndex = 1;
            this.ColMModel.Width = 110;
            //
            // ColMHq
            //
            this.ColMHq.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(236)))), ((int)(((byte)(248)))));
            this.ColMHq.AppearanceCell.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(23)))), ((int)(((byte)(69)))), ((int)(((byte)(122)))));
            this.ColMHq.AppearanceCell.Options.UseBackColor = true;
            this.ColMHq.AppearanceCell.Options.UseForeColor = true;
            this.ColMHq.Caption = "HQ";
            this.ColMHq.FieldName = "AtHq";
            this.ColMHq.Name = "ColMHq";
            this.ColMHq.Visible = true;
            this.ColMHq.VisibleIndex = 2;
            this.ColMHq.Width = 130;
            //
            // ColMMine
            //
            this.ColMMine.Caption = "Mine";
            this.ColMMine.FieldName = "Mine";
            this.ColMMine.Name = "ColMMine";
            this.ColMMine.Visible = true;
            this.ColMMine.VisibleIndex = 3;
            this.ColMMine.Width = 130;
            //
            // ColMStatus
            //
            this.ColMStatus.Caption = "Status";
            this.ColMStatus.FieldName = "StatusText";
            this.ColMStatus.Name = "ColMStatus";
            this.ColMStatus.Visible = true;
            this.ColMStatus.VisibleIndex = 4;
            this.ColMStatus.Width = 140;
            //
            // PanelAction
            //
            this.PanelAction.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelAction.Appearance.Options.UseBackColor = true;
            this.PanelAction.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelAction.Controls.Add(this.BtnApply);
            this.PanelAction.Controls.Add(this.BtnIgnore);
            this.PanelAction.Controls.Add(this.LblCustomer);
            this.PanelAction.Controls.Add(this.SlkCustomer);
            this.PanelAction.Controls.Add(this.BtnTake);
            this.PanelAction.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelAction.Location = new System.Drawing.Point(0, 180);
            this.PanelAction.Name = "PanelAction";
            this.PanelAction.Size = new System.Drawing.Size(630, 40);
            this.PanelAction.TabIndex = 1;
            //
            // BtnApply
            //
            this.BtnApply.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnApply.Appearance.Options.UseFont = true;
            this.BtnApply.Location = new System.Drawing.Point(8, 6);
            this.BtnApply.Name = "BtnApply";
            this.BtnApply.Size = new System.Drawing.Size(250, 28);
            this.BtnApply.TabIndex = 0;
            this.BtnApply.Text = "Apply";
            //
            // BtnIgnore
            //
            this.BtnIgnore.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnIgnore.Appearance.Options.UseFont = true;
            this.BtnIgnore.Location = new System.Drawing.Point(264, 6);
            this.BtnIgnore.Name = "BtnIgnore";
            this.BtnIgnore.Size = new System.Drawing.Size(100, 28);
            this.BtnIgnore.TabIndex = 1;
            this.BtnIgnore.Text = "Ignore";
            //
            // LblCustomer
            //
            this.LblCustomer.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblCustomer.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblCustomer.Appearance.Options.UseFont = true;
            this.LblCustomer.Appearance.Options.UseForeColor = true;
            this.LblCustomer.Location = new System.Drawing.Point(10, 12);
            this.LblCustomer.Name = "LblCustomer";
            this.LblCustomer.Size = new System.Drawing.Size(58, 15);
            this.LblCustomer.TabIndex = 2;
            this.LblCustomer.Text = "Customer:";
            //
            // SlkCustomer
            //
            this.SlkCustomer.Location = new System.Drawing.Point(76, 9);
            this.SlkCustomer.Name = "SlkCustomer";
            this.SlkCustomer.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SlkCustomer.Properties.NullText = "";
            this.SlkCustomer.Properties.PopupView = this.SlkCustomerView;
            this.SlkCustomer.Size = new System.Drawing.Size(330, 22);
            this.SlkCustomer.TabIndex = 3;
            //
            // SlkCustomerView
            //
            this.SlkCustomerView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SlkCustomerView.Name = "SlkCustomerView";
            this.SlkCustomerView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SlkCustomerView.OptionsView.ShowAutoFilterRow = true;
            this.SlkCustomerView.OptionsView.ShowGroupPanel = false;
            //
            // BtnTake
            //
            this.BtnTake.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnTake.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnTake.Appearance.Options.UseFont = true;
            this.BtnTake.Appearance.Options.UseForeColor = true;
            this.BtnTake.Location = new System.Drawing.Point(414, 6);
            this.BtnTake.Name = "BtnTake";
            this.BtnTake.Size = new System.Drawing.Size(130, 28);
            this.BtnTake.TabIndex = 4;
            this.BtnTake.Text = "Take contract";
            //
            // GridReadings
            //
            this.GridReadings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridReadings.Location = new System.Drawing.Point(0, 0);
            this.GridReadings.MainView = this.GridViewReadings;
            this.GridReadings.Name = "GridReadings";
            this.GridReadings.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoNum,
            this.RepoReading,
            this.RepoMoney2});
            this.GridReadings.Size = new System.Drawing.Size(630, 262);
            this.GridReadings.TabIndex = 0;
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
            this.ColRCopies,
            this.ColRAmount,
            this.ColRAtHq,
            this.ColRSaved});
            this.GridViewReadings.ColumnPanelRowHeight = 26;
            this.GridViewReadings.GridControl = this.GridReadings;
            this.GridViewReadings.Name = "GridViewReadings";
            this.GridViewReadings.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewReadings.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewReadings.OptionsView.ColumnAutoWidth = true;
            this.GridViewReadings.OptionsView.ShowGroupPanel = false;
            this.GridViewReadings.OptionsView.ShowIndicator = false;
            this.GridViewReadings.OptionsView.ShowViewCaption = true;
            this.GridViewReadings.RowHeight = 22;
            this.GridViewReadings.ViewCaption = "Readings";
            //
            // ColRMachine
            //
            this.ColRMachine.Caption = "Machine";
            this.ColRMachine.FieldName = "ServiceItemNo";
            this.ColRMachine.Name = "ColRMachine";
            this.ColRMachine.OptionsColumn.AllowEdit = false;
            this.ColRMachine.Visible = true;
            this.ColRMachine.VisibleIndex = 0;
            this.ColRMachine.Width = 100;
            //
            // ColRSerial
            //
            this.ColRSerial.Caption = "Serial";
            this.ColRSerial.FieldName = "Serial";
            this.ColRSerial.Name = "ColRSerial";
            this.ColRSerial.OptionsColumn.AllowEdit = false;
            this.ColRSerial.Width = 80;
            //
            // ColRMeter
            //
            this.ColRMeter.Caption = "Meter";
            this.ColRMeter.FieldName = "Meter";
            this.ColRMeter.Name = "ColRMeter";
            this.ColRMeter.OptionsColumn.AllowEdit = false;
            this.ColRMeter.Visible = true;
            this.ColRMeter.VisibleIndex = 1;
            this.ColRMeter.Width = 50;
            //
            // ColRLast
            //
            this.ColRLast.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRLast.AppearanceCell.Options.UseTextOptions = true;
            this.ColRLast.Caption = "Last";
            this.ColRLast.ColumnEdit = this.RepoNum;
            this.ColRLast.FieldName = "Last";
            this.ColRLast.Name = "ColRLast";
            this.ColRLast.OptionsColumn.AllowEdit = false;
            this.ColRLast.Visible = true;
            this.ColRLast.VisibleIndex = 2;
            this.ColRLast.Width = 76;
            //
            // ColRCurrent
            //
            this.ColRCurrent.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRCurrent.AppearanceCell.Options.UseTextOptions = true;
            this.ColRCurrent.AppearanceHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.ColRCurrent.AppearanceHeader.Options.UseFont = true;
            this.ColRCurrent.Caption = "Current";
            this.ColRCurrent.ColumnEdit = this.RepoReading;
            this.ColRCurrent.FieldName = "Current";
            this.ColRCurrent.Name = "ColRCurrent";
            this.ColRCurrent.Visible = true;
            this.ColRCurrent.VisibleIndex = 3;
            this.ColRCurrent.Width = 80;
            //
            // ColRCopies
            //
            this.ColRCopies.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRCopies.AppearanceCell.Options.UseTextOptions = true;
            this.ColRCopies.Caption = "Copies";
            this.ColRCopies.ColumnEdit = this.RepoNum;
            this.ColRCopies.FieldName = "Copies";
            this.ColRCopies.Name = "ColRCopies";
            this.ColRCopies.OptionsColumn.AllowEdit = false;
            this.ColRCopies.Visible = true;
            this.ColRCopies.VisibleIndex = 4;
            this.ColRCopies.Width = 64;
            //
            // ColRAmount
            //
            this.ColRAmount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.ColRAmount.AppearanceCell.Options.UseTextOptions = true;
            this.ColRAmount.Caption = "RM";
            this.ColRAmount.ColumnEdit = this.RepoMoney2;
            this.ColRAmount.FieldName = "Amount";
            this.ColRAmount.Name = "ColRAmount";
            this.ColRAmount.OptionsColumn.AllowEdit = false;
            this.ColRAmount.Visible = true;
            this.ColRAmount.VisibleIndex = 5;
            this.ColRAmount.Width = 70;
            //
            // ColRAtHq
            //
            this.ColRAtHq.Caption = "At HQ";
            this.ColRAtHq.FieldName = "AtHq";
            this.ColRAtHq.Name = "ColRAtHq";
            this.ColRAtHq.OptionsColumn.AllowEdit = false;
            this.ColRAtHq.Visible = true;
            this.ColRAtHq.VisibleIndex = 6;
            this.ColRAtHq.Width = 110;
            //
            // ColRSaved
            //
            this.ColRSaved.Caption = "Saved";
            this.ColRSaved.FieldName = "Saved";
            this.ColRSaved.Name = "ColRSaved";
            this.ColRSaved.OptionsColumn.AllowEdit = false;
            this.ColRSaved.Visible = true;
            this.ColRSaved.VisibleIndex = 7;
            this.ColRSaved.Width = 90;
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
            // InterBillBoard_Form
            //
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 728);
            this.Controls.Add(this.PopCustomers);
            this.Controls.Add(this.SplitMain);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelView);
            this.Controls.Add(this.PanelHeaderTop);
            this.Controls.Add(this.PanelStatus);
            this.Name = "InterBillBoard_Form";
            this.Text = "Inter-Billing";
            ((System.ComponentModel.ISupportInitialize)(this.PanelView)).EndInit();
            this.PanelView.ResumeLayout(false);
            this.PanelView.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowEnded.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PceCustomers.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PopCustomers)).EndInit();
            this.PopCustomers.ResumeLayout(false);
            this.PopCustomers.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCustomerSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridCustomerPick)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewCustomerPick)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoPickCheck)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).EndInit();
            this.SplitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridContracts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContractInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoiceLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetailHead)).EndInit();
            this.PanelDetailHead.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitDetail)).EndInit();
            this.SplitDetail.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelAction)).EndInit();
            this.PanelAction.ResumeLayout(false);
            this.PanelAction.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SlkCustomer.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlkCustomerView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoReading)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMoney2)).EndInit();
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
        private DevExpress.XtraEditors.CheckButton BtnViewContracts;
        private DevExpress.XtraEditors.PanelControl PanelFilter;
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblBook;
        private DevExpress.XtraEditors.LabelControl LblConn;
        private DevExpress.XtraEditors.LabelControl LblMonth;
        private DevExpress.XtraEditors.LabelControl LblUpTo;
        private DevExpress.XtraEditors.CheckEdit ChkShowEnded;
        private DevExpress.XtraEditors.LabelControl LblCustomers;
        private DevExpress.XtraEditors.PopupContainerEdit PceCustomers;
        private DevExpress.XtraEditors.PopupContainerControl PopCustomers;
        private DevExpress.XtraEditors.TextEdit TxtCustomerSearch;
        private DevExpress.XtraEditors.CheckButton ChkTickedOnly;
        private DevExpress.XtraEditors.SimpleButton BtnTickShown;
        private DevExpress.XtraEditors.SimpleButton BtnUntickShown;
        private DevExpress.XtraEditors.LabelControl LblPickCount;
        private DevExpress.XtraGrid.GridControl GridCustomerPick;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewCustomerPick;
        private DevExpress.XtraGrid.Columns.GridColumn ColPickTicked;
        private DevExpress.XtraGrid.Columns.GridColumn ColPickName;
        private DevExpress.XtraGrid.Columns.GridColumn ColPickCode;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoPickCheck;
        private DevExpress.XtraEditors.SimpleButton BtnPickClear;
        private DevExpress.XtraEditors.SimpleButton BtnPickOk;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateReady;
        private DevExpress.XtraEditors.SimpleButton BtnSetup;
        private DevExpress.XtraEditors.LabelControl LblShow;
        private DevExpress.XtraEditors.CheckButton ChipAll;
        private DevExpress.XtraEditors.CheckButton ChipReady;
        private DevExpress.XtraEditors.CheckButton ChipWaiting;
        private DevExpress.XtraEditors.CheckButton ChipChanged;
        private DevExpress.XtraEditors.CheckButton ChipUnpriced;
        private DevExpress.XtraEditors.CheckButton ChipNotTaken;
        private DevExpress.XtraEditors.CheckButton ChipInvoiced;
        private DevExpress.XtraEditors.SplitContainerControl SplitMain;
        private DevExpress.XtraGrid.GridControl GridContracts;
        private DevExpress.XtraGrid.Columns.GridColumn ColInvoices;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewContractInvoices;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiKind;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiDetail;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColCiStatus;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewInvoiceLines;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvCount;
        private DevExpress.XtraGrid.Columns.GridColumn ColIlReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColIlAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColIlStatus;
        private DevExpress.XtraGrid.GridControl GridInvoices;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewInvoices;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvCustomer;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvContract;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvMonth;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvKind;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvDetail;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColIvStatus;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewContracts;
        private DevExpress.XtraGrid.Columns.GridColumn ColHq;
        private DevExpress.XtraGrid.Columns.GridColumn ColMine;
        private DevExpress.XtraGrid.Columns.GridColumn ColCustomer;
        private DevExpress.XtraGrid.Columns.GridColumn ColMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColStatus;
        private DevExpress.XtraGrid.Columns.GridColumn ColPeriod;
        private DevExpress.XtraGrid.Columns.GridColumn ColBilled;
        private DevExpress.XtraGrid.Columns.GridColumn ColDue;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoMoney;
        private DevExpress.XtraEditors.PanelControl PanelDetailHead;
        private DevExpress.XtraEditors.LabelControl LblDetailTitle;
        private DevExpress.XtraEditors.LabelControl LblDetailSub;
        private DevExpress.XtraEditors.SimpleButton BtnOpenContract;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateThis;
        private DevExpress.XtraEditors.LabelControl LblSequence;
        private DevExpress.XtraEditors.SimpleButton BtnBillFrom;
        private DevExpress.XtraEditors.SimpleButton BtnUndoSkip;
        private DevExpress.XtraEditors.SimpleButton BtnSkipMonth;
        private DevExpress.XtraEditors.SplitContainerControl SplitDetail;
        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColMSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColMModel;
        private DevExpress.XtraGrid.Columns.GridColumn ColMHq;
        private DevExpress.XtraGrid.Columns.GridColumn ColMMine;
        private DevExpress.XtraGrid.Columns.GridColumn ColMStatus;
        private DevExpress.XtraEditors.PanelControl PanelAction;
        private DevExpress.XtraEditors.SimpleButton BtnApply;
        private DevExpress.XtraEditors.SimpleButton BtnIgnore;
        private DevExpress.XtraEditors.LabelControl LblCustomer;
        private DevExpress.XtraEditors.SearchLookUpEdit SlkCustomer;
        private DevExpress.XtraGrid.Views.Grid.GridView SlkCustomerView;
        private DevExpress.XtraEditors.SimpleButton BtnTake;
        private DevExpress.XtraGrid.GridControl GridReadings;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMachine;
        private DevExpress.XtraGrid.Columns.GridColumn ColRSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColRMeter;
        private DevExpress.XtraGrid.Columns.GridColumn ColRLast;
        private DevExpress.XtraGrid.Columns.GridColumn ColRCurrent;
        private DevExpress.XtraGrid.Columns.GridColumn ColRCopies;
        private DevExpress.XtraGrid.Columns.GridColumn ColRAmount;
        private DevExpress.XtraGrid.Columns.GridColumn ColRAtHq;
        private DevExpress.XtraGrid.Columns.GridColumn ColRSaved;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoNum;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoReading;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoMoney2;
        private DevExpress.XtraEditors.PanelControl PanelStatus;
        private DevExpress.XtraEditors.LabelControl LblStatus;
    }
}
