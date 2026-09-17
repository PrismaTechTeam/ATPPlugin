namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class InterBilling_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.PanelControl PanelToolbar;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnExit;
        private DevExpress.XtraEditors.LabelControl LblBook;
        private DevExpress.XtraEditors.ComboBoxEdit CboBook;
        private DevExpress.XtraEditors.LabelControl LblBookStatus;

        private DevExpress.XtraTab.XtraTabControl TabMain;
        private DevExpress.XtraTab.XtraTabPage TabIncoming;
        private DevExpress.XtraTab.XtraTabPage TabChanges;
        private DevExpress.XtraTab.XtraTabPage TabReadings;
        private DevExpress.XtraTab.XtraTabPage TabBill;

        private DevExpress.XtraGrid.GridControl GridContracts;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewContracts;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtDebtor;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtDescription;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtExpiry;
        private DevExpress.XtraGrid.Columns.GridColumn ColCtTaken;

        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColMcSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColMcModel;
        private DevExpress.XtraGrid.Columns.GridColumn ColMcDescription;
        private DevExpress.XtraGrid.Columns.GridColumn ColMcCounters;
        private DevExpress.XtraGrid.Columns.GridColumn ColMcStatus;

        private DevExpress.XtraEditors.PanelControl PanelTakeBar;
        private DevExpress.XtraEditors.LabelControl LblDebtor;
        private DevExpress.XtraEditors.SearchLookUpEdit LkDebtor;
        private DevExpress.XtraGrid.Views.Grid.GridView LkDebtorView;
        private DevExpress.XtraEditors.LabelControl LblMargin;
        private DevExpress.XtraEditors.SpinEdit SpnMargin;
        private DevExpress.XtraEditors.SimpleButton BtnTake;
        private DevExpress.XtraEditors.LabelControl LblTakeNote;
        private DevExpress.XtraEditors.LabelControl LblSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeExpired;

        private DevExpress.XtraGrid.GridControl GridChanges;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewChanges;
        private DevExpress.XtraGrid.Columns.GridColumn ColChContract;
        private DevExpress.XtraGrid.Columns.GridColumn ColChWhat;
        private DevExpress.XtraGrid.Columns.GridColumn ColChWhich;
        private DevExpress.XtraGrid.Columns.GridColumn ColChChange;
        private DevExpress.XtraGrid.Columns.GridColumn ColChTaken;
        private DevExpress.XtraEditors.PanelControl PanelChangeBar;
        private DevExpress.XtraEditors.SimpleButton BtnAcceptChange;
        private DevExpress.XtraEditors.SimpleButton BtnDismissChange;
        private DevExpress.XtraEditors.LabelControl LblChangeNote;

        private DevExpress.XtraGrid.GridControl GridReadings;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdContract;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdCounter;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdTheirs;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColRdStatus;
        private DevExpress.XtraEditors.PanelControl PanelReadBar;
        private DevExpress.XtraEditors.LabelControl LblPeriod;
        private DevExpress.XtraEditors.SpinEdit SpnYear;
        private DevExpress.XtraEditors.SpinEdit SpnMonth;
        private DevExpress.XtraEditors.SimpleButton BtnLookReadings;
        private DevExpress.XtraEditors.SimpleButton BtnBringReadings;
        private DevExpress.XtraEditors.LabelControl LblReadNote;

        private DevExpress.XtraGrid.GridControl GridBill;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewBill;
        private DevExpress.XtraGrid.Columns.GridColumn ColBlContract;
        private DevExpress.XtraGrid.Columns.GridColumn ColBlCustomer;
        private DevExpress.XtraGrid.Columns.GridColumn ColBlMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColBlReadings;
        private DevExpress.XtraGrid.Columns.GridColumn ColBlInvoiced;
        private DevExpress.XtraEditors.PanelControl PanelBillBar;
        private DevExpress.XtraEditors.LabelControl LblBillPeriod;
        private DevExpress.XtraEditors.SpinEdit SpnBillYear;
        private DevExpress.XtraEditors.SpinEdit SpnBillMonth;
        private DevExpress.XtraEditors.SimpleButton BtnPreviewInvoice;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateInvoice;
        private DevExpress.XtraEditors.LabelControl LblBillNote;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new DevExpress.XtraEditors.PanelControl();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnExit = new DevExpress.XtraEditors.SimpleButton();
            this.LblBook = new DevExpress.XtraEditors.LabelControl();
            this.CboBook = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblBookStatus = new DevExpress.XtraEditors.LabelControl();
            this.TabMain = new DevExpress.XtraTab.XtraTabControl();
            this.TabIncoming = new DevExpress.XtraTab.XtraTabPage();
            this.TabChanges = new DevExpress.XtraTab.XtraTabPage();
            this.TabReadings = new DevExpress.XtraTab.XtraTabPage();
            this.GridContracts = new DevExpress.XtraGrid.GridControl();
            this.GridViewContracts = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColCtNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCtDebtor = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCtDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCtMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCtExpiry = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCtTaken = new DevExpress.XtraGrid.Columns.GridColumn();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMcSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMcModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMcDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMcCounters = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMcStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelTakeBar = new DevExpress.XtraEditors.PanelControl();
            this.LblDebtor = new DevExpress.XtraEditors.LabelControl();
            this.LkDebtor = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.LkDebtorView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblMargin = new DevExpress.XtraEditors.LabelControl();
            this.SpnMargin = new DevExpress.XtraEditors.SpinEdit();
            this.BtnTake = new DevExpress.XtraEditors.SimpleButton();
            this.LblTakeNote = new DevExpress.XtraEditors.LabelControl();
            this.LblSearch = new DevExpress.XtraEditors.LabelControl();
            this.TxtSearch = new DevExpress.XtraEditors.TextEdit();
            this.ChkIncludeExpired = new DevExpress.XtraEditors.CheckEdit();
            this.GridChanges = new DevExpress.XtraGrid.GridControl();
            this.GridViewChanges = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColChContract = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColChWhat = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColChWhich = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColChChange = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColChTaken = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelChangeBar = new DevExpress.XtraEditors.PanelControl();
            this.BtnAcceptChange = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDismissChange = new DevExpress.XtraEditors.SimpleButton();
            this.LblChangeNote = new DevExpress.XtraEditors.LabelControl();
            this.GridReadings = new DevExpress.XtraGrid.GridControl();
            this.GridViewReadings = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColRdContract = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRdSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRdCounter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRdTheirs = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRdDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRdStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelReadBar = new DevExpress.XtraEditors.PanelControl();
            this.LblPeriod = new DevExpress.XtraEditors.LabelControl();
            this.SpnYear = new DevExpress.XtraEditors.SpinEdit();
            this.SpnMonth = new DevExpress.XtraEditors.SpinEdit();
            this.BtnLookReadings = new DevExpress.XtraEditors.SimpleButton();
            this.BtnBringReadings = new DevExpress.XtraEditors.SimpleButton();
            this.LblReadNote = new DevExpress.XtraEditors.LabelControl();
            this.TabBill = new DevExpress.XtraTab.XtraTabPage();
            this.GridBill = new DevExpress.XtraGrid.GridControl();
            this.GridViewBill = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColBlContract = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBlCustomer = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBlMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBlReadings = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBlInvoiced = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelBillBar = new DevExpress.XtraEditors.PanelControl();
            this.LblBillPeriod = new DevExpress.XtraEditors.LabelControl();
            this.SpnBillYear = new DevExpress.XtraEditors.SpinEdit();
            this.SpnBillMonth = new DevExpress.XtraEditors.SpinEdit();
            this.BtnPreviewInvoice = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateInvoice = new DevExpress.XtraEditors.SimpleButton();
            this.LblBillNote = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).BeginInit();
            this.PanelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CboBook.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TabMain)).BeginInit();
            this.TabMain.SuspendLayout();
            this.TabIncoming.SuspendLayout();
            this.TabChanges.SuspendLayout();
            this.TabReadings.SuspendLayout();
            this.TabBill.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridContracts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTakeBar)).BeginInit();
            this.PanelTakeBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtor.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtorView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMargin.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridChanges)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewChanges)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelChangeBar)).BeginInit();
            this.PanelChangeBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelReadBar)).BeginInit();
            this.PanelReadBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpnYear.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMonth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridBill)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewBill)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBillBar)).BeginInit();
            this.PanelBillBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillYear.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillMonth.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Inter-Billing";
            this.PanelHeaderTop.Hint = "Take a contract from the other company's account book and bill it here, to your own customer, at your own prices. They own the machines and the readings; you own the money.";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1200, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelToolbar
            //
            this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);
            this.PanelToolbar.Controls.Add(this.LblBook);
            this.PanelToolbar.Controls.Add(this.CboBook);
            this.PanelToolbar.Controls.Add(this.LblBookStatus);
            this.PanelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelToolbar.Location = new System.Drawing.Point(0, 56);
            this.PanelToolbar.Name = "PanelToolbar";
            this.PanelToolbar.Size = new System.Drawing.Size(1200, 62);
            this.PanelToolbar.TabIndex = 1;
            //
            // BtnRefresh
            //
            this.BtnRefresh.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnRefresh.Location = new System.Drawing.Point(8, 6);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(86, 50);
            this.BtnRefresh.TabIndex = 0;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            //
            // BtnExit
            //
            this.BtnExit.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnExit.Location = new System.Drawing.Point(98, 6);
            this.BtnExit.Name = "BtnExit";
            this.BtnExit.Size = new System.Drawing.Size(86, 50);
            this.BtnExit.TabIndex = 1;
            this.BtnExit.Text = "Exit";
            this.BtnExit.Click += new System.EventHandler(this.OnExit);
            //
            // LblBook
            //
            this.LblBook.Location = new System.Drawing.Point(206, 12);
            this.LblBook.Name = "LblBook";
            this.LblBook.Size = new System.Drawing.Size(60, 13);
            this.LblBook.TabIndex = 2;
            this.LblBook.Text = "Other book";
            //
            // CboBook
            //
            this.CboBook.Location = new System.Drawing.Point(206, 30);
            this.CboBook.Name = "CboBook";
            this.CboBook.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CboBook.Size = new System.Drawing.Size(360, 20);
            this.CboBook.TabIndex = 3;
            this.CboBook.SelectedIndexChanged += new System.EventHandler(this.OnBookChanged);
            //
            // LblBookStatus
            //
            this.LblBookStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblBookStatus.Location = new System.Drawing.Point(582, 12);
            this.LblBookStatus.Name = "LblBookStatus";
            this.LblBookStatus.Size = new System.Drawing.Size(600, 40);
            this.LblBookStatus.TabIndex = 4;
            this.LblBookStatus.Text = "";
            //
            // TabMain
            //
            this.TabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabMain.Location = new System.Drawing.Point(0, 118);
            this.TabMain.Name = "TabMain";
            this.TabMain.SelectedTabPage = this.TabIncoming;
            this.TabMain.Size = new System.Drawing.Size(1200, 582);
            this.TabMain.TabIndex = 2;
            this.TabMain.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
                this.TabIncoming, this.TabChanges, this.TabReadings, this.TabBill});
            //
            // TabIncoming
            //
            this.TabIncoming.Controls.Add(this.GridMachines);
            this.TabIncoming.Controls.Add(this.GridContracts);
            this.TabIncoming.Controls.Add(this.PanelTakeBar);
            this.TabIncoming.Name = "TabIncoming";
            this.TabIncoming.Size = new System.Drawing.Size(1194, 553);
            this.TabIncoming.Text = "1. Their contracts";
            //
            // PanelTakeBar
            //
            this.PanelTakeBar.Controls.Add(this.LblSearch);
            this.PanelTakeBar.Controls.Add(this.TxtSearch);
            this.PanelTakeBar.Controls.Add(this.ChkIncludeExpired);
            this.PanelTakeBar.Controls.Add(this.LblDebtor);
            this.PanelTakeBar.Controls.Add(this.LkDebtor);
            this.PanelTakeBar.Controls.Add(this.LblMargin);
            this.PanelTakeBar.Controls.Add(this.SpnMargin);
            this.PanelTakeBar.Controls.Add(this.BtnTake);
            this.PanelTakeBar.Controls.Add(this.LblTakeNote);
            this.PanelTakeBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelTakeBar.Location = new System.Drawing.Point(0, 453);
            this.PanelTakeBar.Name = "PanelTakeBar";
            this.PanelTakeBar.Size = new System.Drawing.Size(1194, 100);
            this.PanelTakeBar.TabIndex = 2;
            //
            // LblSearch
            //
            this.LblSearch.Location = new System.Drawing.Point(12, 14);
            this.LblSearch.Name = "LblSearch";
            this.LblSearch.Size = new System.Drawing.Size(36, 13);
            this.LblSearch.TabIndex = 0;
            this.LblSearch.Text = "Find";
            //
            // TxtSearch
            //
            this.TxtSearch.Location = new System.Drawing.Point(60, 10);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Size = new System.Drawing.Size(260, 20);
            this.TxtSearch.TabIndex = 1;
            //
            // ChkIncludeExpired
            //
            this.ChkIncludeExpired.Location = new System.Drawing.Point(336, 9);
            this.ChkIncludeExpired.Name = "ChkIncludeExpired";
            this.ChkIncludeExpired.Properties.Caption = "Show contracts that have ended over there";
            this.ChkIncludeExpired.Size = new System.Drawing.Size(280, 20);
            this.ChkIncludeExpired.TabIndex = 2;
            //
            // LblDebtor
            //
            this.LblDebtor.Location = new System.Drawing.Point(12, 46);
            this.LblDebtor.Name = "LblDebtor";
            this.LblDebtor.Size = new System.Drawing.Size(44, 13);
            this.LblDebtor.TabIndex = 3;
            this.LblDebtor.Text = "Bill it to";
            //
            // LkDebtor
            //
            this.LkDebtor.Location = new System.Drawing.Point(60, 42);
            this.LkDebtor.Name = "LkDebtor";
            this.LkDebtor.Properties.NullText = "";
            this.LkDebtor.Properties.PopupView = this.LkDebtorView;
            this.LkDebtor.Size = new System.Drawing.Size(360, 20);
            this.LkDebtor.TabIndex = 4;
            //
            // LkDebtorView
            //
            this.LkDebtorView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.LkDebtorView.Name = "LkDebtorView";
            this.LkDebtorView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.LkDebtorView.OptionsView.ShowGroupPanel = false;
            //
            // LblMargin
            //
            this.LblMargin.Location = new System.Drawing.Point(440, 46);
            this.LblMargin.Name = "LblMargin";
            this.LblMargin.Size = new System.Drawing.Size(50, 13);
            this.LblMargin.TabIndex = 5;
            this.LblMargin.Text = "Margin %";
            //
            // SpnMargin
            //
            this.SpnMargin.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnMargin.Location = new System.Drawing.Point(496, 42);
            this.SpnMargin.Name = "SpnMargin";
            this.SpnMargin.Properties.DisplayFormat.FormatString = "n2";
            this.SpnMargin.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.SpnMargin.Properties.EditFormat.FormatString = "n2";
            this.SpnMargin.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.SpnMargin.Properties.MaxValue = new decimal(new int[] { 100000, 0, 0, 0 });
            this.SpnMargin.Properties.MinValue = new decimal(new int[] { 100000, 0, 0, -2147483648 });
            this.SpnMargin.Size = new System.Drawing.Size(90, 20);
            this.SpnMargin.TabIndex = 6;
            //
            // BtnTake
            //
            this.BtnTake.Location = new System.Drawing.Point(600, 38);
            this.BtnTake.Name = "BtnTake";
            this.BtnTake.Size = new System.Drawing.Size(210, 28);
            this.BtnTake.TabIndex = 7;
            this.BtnTake.Text = "Make my contract from this";
            this.BtnTake.Click += new System.EventHandler(this.OnTake);
            //
            // LblTakeNote
            //
            this.LblTakeNote.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblTakeNote.Location = new System.Drawing.Point(820, 38);
            this.LblTakeNote.Name = "LblTakeNote";
            this.LblTakeNote.Size = new System.Drawing.Size(360, 50);
            this.LblTakeNote.TabIndex = 8;
            this.LblTakeNote.Text = "Their prices come across with your margin added, and stay editable in Meters & Pricing. Margin 0 bills exactly what they charge.";
            //
            // GridContracts
            //
            this.GridContracts.Dock = System.Windows.Forms.DockStyle.Left;
            this.GridContracts.Location = new System.Drawing.Point(0, 0);
            this.GridContracts.MainView = this.GridViewContracts;
            this.GridContracts.Name = "GridContracts";
            this.GridContracts.Size = new System.Drawing.Size(640, 453);
            this.GridContracts.TabIndex = 0;
            this.GridContracts.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewContracts});
            //
            // GridViewContracts
            //
            this.GridViewContracts.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColCtNo, this.ColCtDebtor, this.ColCtDescription, this.ColCtMachines,
                this.ColCtExpiry, this.ColCtTaken});
            this.GridViewContracts.GridControl = this.GridContracts;
            this.GridViewContracts.Name = "GridViewContracts";
            this.GridViewContracts.OptionsBehavior.Editable = false;
            this.GridViewContracts.OptionsView.ShowGroupPanel = false;
            this.GridViewContracts.OptionsView.ShowAutoFilterRow = true;
            //
            // ColCtNo
            //
            this.ColCtNo.Caption = "Their contract";
            this.ColCtNo.FieldName = "ContractNo";
            this.ColCtNo.Name = "ColCtNo";
            this.ColCtNo.Visible = true;
            this.ColCtNo.VisibleIndex = 0;
            this.ColCtNo.Width = 110;
            //
            // ColCtDebtor
            //
            this.ColCtDebtor.Caption = "Billed to (over there)";
            this.ColCtDebtor.FieldName = "DebtorName";
            this.ColCtDebtor.Name = "ColCtDebtor";
            this.ColCtDebtor.Visible = true;
            this.ColCtDebtor.VisibleIndex = 1;
            this.ColCtDebtor.Width = 160;
            //
            // ColCtDescription
            //
            this.ColCtDescription.Caption = "Description";
            this.ColCtDescription.FieldName = "Description";
            this.ColCtDescription.Name = "ColCtDescription";
            this.ColCtDescription.Visible = true;
            this.ColCtDescription.VisibleIndex = 2;
            this.ColCtDescription.Width = 170;
            //
            // ColCtMachines
            //
            this.ColCtMachines.Caption = "Machines";
            this.ColCtMachines.FieldName = "MachineCount";
            this.ColCtMachines.Name = "ColCtMachines";
            this.ColCtMachines.Visible = true;
            this.ColCtMachines.VisibleIndex = 3;
            this.ColCtMachines.Width = 70;
            //
            // ColCtExpiry
            //
            this.ColCtExpiry.Caption = "Ends";
            this.ColCtExpiry.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColCtExpiry.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColCtExpiry.FieldName = "ExpiryDate";
            this.ColCtExpiry.Name = "ColCtExpiry";
            this.ColCtExpiry.Visible = true;
            this.ColCtExpiry.VisibleIndex = 4;
            this.ColCtExpiry.Width = 80;
            //
            // ColCtTaken
            //
            this.ColCtTaken.Caption = "Here";
            this.ColCtTaken.FieldName = "Taken";
            this.ColCtTaken.Name = "ColCtTaken";
            this.ColCtTaken.Visible = true;
            this.ColCtTaken.VisibleIndex = 5;
            this.ColCtTaken.Width = 130;
            //
            // GridMachines
            //
            this.GridMachines.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridMachines.Location = new System.Drawing.Point(640, 0);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.Size = new System.Drawing.Size(554, 453);
            this.GridMachines.TabIndex = 1;
            this.GridMachines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewMachines});
            //
            // GridViewMachines
            //
            this.GridViewMachines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColMcSerial, this.ColMcModel, this.ColMcDescription, this.ColMcCounters,
                this.ColMcStatus});
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.Editable = false;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
            //
            // ColMcSerial
            //
            this.ColMcSerial.Caption = "Serial no";
            this.ColMcSerial.FieldName = "SerialNumber";
            this.ColMcSerial.Name = "ColMcSerial";
            this.ColMcSerial.Visible = true;
            this.ColMcSerial.VisibleIndex = 0;
            this.ColMcSerial.Width = 120;
            //
            // ColMcModel
            //
            this.ColMcModel.Caption = "Model";
            this.ColMcModel.FieldName = "ItemCode";
            this.ColMcModel.Name = "ColMcModel";
            this.ColMcModel.Visible = true;
            this.ColMcModel.VisibleIndex = 1;
            this.ColMcModel.Width = 130;
            //
            // ColMcDescription
            //
            this.ColMcDescription.Caption = "Description";
            this.ColMcDescription.FieldName = "Description";
            this.ColMcDescription.Name = "ColMcDescription";
            this.ColMcDescription.Visible = true;
            this.ColMcDescription.VisibleIndex = 2;
            this.ColMcDescription.Width = 160;
            //
            // ColMcCounters
            //
            this.ColMcCounters.Caption = "Counters";
            this.ColMcCounters.FieldName = "Counters";
            this.ColMcCounters.Name = "ColMcCounters";
            this.ColMcCounters.Visible = true;
            this.ColMcCounters.VisibleIndex = 3;
            this.ColMcCounters.Width = 140;
            //
            // ColMcStatus
            //
            this.ColMcStatus.Caption = "Status";
            this.ColMcStatus.FieldName = "Status";
            this.ColMcStatus.Name = "ColMcStatus";
            this.ColMcStatus.Visible = true;
            this.ColMcStatus.VisibleIndex = 4;
            this.ColMcStatus.Width = 80;
            //
            // TabChanges
            //
            this.TabChanges.Controls.Add(this.GridChanges);
            this.TabChanges.Controls.Add(this.PanelChangeBar);
            this.TabChanges.Name = "TabChanges";
            this.TabChanges.Size = new System.Drawing.Size(1194, 553);
            this.TabChanges.Text = "2. What they changed";
            //
            // GridChanges
            //
            this.GridChanges.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridChanges.Location = new System.Drawing.Point(0, 0);
            this.GridChanges.MainView = this.GridViewChanges;
            this.GridChanges.Name = "GridChanges";
            this.GridChanges.Size = new System.Drawing.Size(1194, 483);
            this.GridChanges.TabIndex = 0;
            this.GridChanges.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewChanges});
            //
            // GridViewChanges
            //
            this.GridViewChanges.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColChContract, this.ColChWhat, this.ColChWhich, this.ColChChange, this.ColChTaken});
            this.GridViewChanges.GridControl = this.GridChanges;
            this.GridViewChanges.Name = "GridViewChanges";
            this.GridViewChanges.OptionsBehavior.Editable = false;
            this.GridViewChanges.OptionsView.ShowGroupPanel = false;
            //
            // ColChContract
            //
            this.ColChContract.Caption = "My contract";
            this.ColChContract.FieldName = "ContractNo";
            this.ColChContract.Name = "ColChContract";
            this.ColChContract.Visible = true;
            this.ColChContract.VisibleIndex = 0;
            this.ColChContract.Width = 120;
            //
            // ColChWhat
            //
            this.ColChWhat.Caption = "What";
            this.ColChWhat.FieldName = "What";
            this.ColChWhat.Name = "ColChWhat";
            this.ColChWhat.Visible = true;
            this.ColChWhat.VisibleIndex = 1;
            this.ColChWhat.Width = 100;
            //
            // ColChWhich
            //
            this.ColChWhich.Caption = "Which";
            this.ColChWhich.FieldName = "Which";
            this.ColChWhich.Name = "ColChWhich";
            this.ColChWhich.Visible = true;
            this.ColChWhich.VisibleIndex = 2;
            this.ColChWhich.Width = 200;
            //
            // ColChChange
            //
            this.ColChChange.Caption = "Changed";
            this.ColChChange.FieldName = "Change";
            this.ColChChange.Name = "ColChChange";
            this.ColChChange.Visible = true;
            this.ColChChange.VisibleIndex = 3;
            this.ColChChange.Width = 480;
            //
            // ColChTaken
            //
            this.ColChTaken.Caption = "Taken";
            this.ColChTaken.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColChTaken.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColChTaken.FieldName = "TakenAt";
            this.ColChTaken.Name = "ColChTaken";
            this.ColChTaken.Visible = true;
            this.ColChTaken.VisibleIndex = 4;
            this.ColChTaken.Width = 90;
            //
            // PanelChangeBar
            //
            this.PanelChangeBar.Controls.Add(this.BtnAcceptChange);
            this.PanelChangeBar.Controls.Add(this.BtnDismissChange);
            this.PanelChangeBar.Controls.Add(this.LblChangeNote);
            this.PanelChangeBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelChangeBar.Location = new System.Drawing.Point(0, 483);
            this.PanelChangeBar.Name = "PanelChangeBar";
            this.PanelChangeBar.Size = new System.Drawing.Size(1194, 70);
            this.PanelChangeBar.TabIndex = 1;
            //
            // BtnAcceptChange
            //
            this.BtnAcceptChange.Location = new System.Drawing.Point(12, 20);
            this.BtnAcceptChange.Name = "BtnAcceptChange";
            this.BtnAcceptChange.Size = new System.Drawing.Size(190, 28);
            this.BtnAcceptChange.TabIndex = 0;
            this.BtnAcceptChange.Text = "Take this change too";
            this.BtnAcceptChange.Click += new System.EventHandler(this.OnAcceptChange);
            //
            // BtnDismissChange
            //
            this.BtnDismissChange.Location = new System.Drawing.Point(212, 20);
            this.BtnDismissChange.Name = "BtnDismissChange";
            this.BtnDismissChange.Size = new System.Drawing.Size(150, 28);
            this.BtnDismissChange.TabIndex = 1;
            this.BtnDismissChange.Text = "Leave mine as it is";
            this.BtnDismissChange.Click += new System.EventHandler(this.OnDismissChange);
            //
            // LblChangeNote
            //
            this.LblChangeNote.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblChangeNote.Location = new System.Drawing.Point(380, 14);
            this.LblChangeNote.Name = "LblChangeNote";
            this.LblChangeNote.Size = new System.Drawing.Size(790, 42);
            this.LblChangeNote.TabIndex = 2;
            this.LblChangeNote.Text = "Nothing here changes your contract until you say so. Readings are the exception - those always follow, because they were never yours.";
            //
            // TabReadings
            //
            this.TabReadings.Controls.Add(this.GridReadings);
            this.TabReadings.Controls.Add(this.PanelReadBar);
            this.TabReadings.Name = "TabReadings";
            this.TabReadings.Size = new System.Drawing.Size(1194, 553);
            this.TabReadings.Text = "3. Their readings";
            //
            // PanelReadBar
            //
            this.PanelReadBar.Controls.Add(this.LblPeriod);
            this.PanelReadBar.Controls.Add(this.SpnYear);
            this.PanelReadBar.Controls.Add(this.SpnMonth);
            this.PanelReadBar.Controls.Add(this.BtnLookReadings);
            this.PanelReadBar.Controls.Add(this.BtnBringReadings);
            this.PanelReadBar.Controls.Add(this.LblReadNote);
            this.PanelReadBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelReadBar.Location = new System.Drawing.Point(0, 0);
            this.PanelReadBar.Name = "PanelReadBar";
            this.PanelReadBar.Size = new System.Drawing.Size(1194, 70);
            this.PanelReadBar.TabIndex = 0;
            //
            // LblPeriod
            //
            this.LblPeriod.Location = new System.Drawing.Point(12, 26);
            this.LblPeriod.Name = "LblPeriod";
            this.LblPeriod.Size = new System.Drawing.Size(70, 13);
            this.LblPeriod.TabIndex = 0;
            this.LblPeriod.Text = "Year / month";
            //
            // SpnYear
            //
            this.SpnYear.EditValue = "2026";
            this.SpnYear.Location = new System.Drawing.Point(96, 22);
            this.SpnYear.Name = "SpnYear";
            this.SpnYear.Properties.MaxValue = new decimal(new int[] { 2999, 0, 0, 0 });
            this.SpnYear.Properties.MinValue = new decimal(new int[] { 2000, 0, 0, 0 });
            this.SpnYear.Size = new System.Drawing.Size(80, 20);
            this.SpnYear.TabIndex = 1;
            //
            // SpnMonth
            //
            this.SpnMonth.EditValue = "1";
            this.SpnMonth.Location = new System.Drawing.Point(184, 22);
            this.SpnMonth.Name = "SpnMonth";
            this.SpnMonth.Properties.MaxValue = new decimal(new int[] { 12, 0, 0, 0 });
            this.SpnMonth.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
            this.SpnMonth.Size = new System.Drawing.Size(60, 20);
            this.SpnMonth.TabIndex = 2;
            //
            // BtnLookReadings
            //
            this.BtnLookReadings.Location = new System.Drawing.Point(264, 18);
            this.BtnLookReadings.Name = "BtnLookReadings";
            this.BtnLookReadings.Size = new System.Drawing.Size(150, 28);
            this.BtnLookReadings.TabIndex = 3;
            this.BtnLookReadings.Text = "See what they have";
            this.BtnLookReadings.Click += new System.EventHandler(this.OnLookReadings);
            //
            // BtnBringReadings
            //
            this.BtnBringReadings.Location = new System.Drawing.Point(424, 18);
            this.BtnBringReadings.Name = "BtnBringReadings";
            this.BtnBringReadings.Size = new System.Drawing.Size(150, 28);
            this.BtnBringReadings.TabIndex = 4;
            this.BtnBringReadings.Text = "Bring them over";
            this.BtnBringReadings.Click += new System.EventHandler(this.OnBringReadings);
            //
            // LblReadNote
            //
            this.LblReadNote.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblReadNote.Location = new System.Drawing.Point(590, 14);
            this.LblReadNote.Name = "LblReadNote";
            this.LblReadNote.Size = new System.Drawing.Size(580, 42);
            this.LblReadNote.TabIndex = 5;
            this.LblReadNote.Text = "A reading already invoiced here is left alone. Everything else is replaced by theirs.";
            //
            // GridReadings
            //
            this.GridReadings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridReadings.Location = new System.Drawing.Point(0, 70);
            this.GridReadings.MainView = this.GridViewReadings;
            this.GridReadings.Name = "GridReadings";
            this.GridReadings.Size = new System.Drawing.Size(1194, 483);
            this.GridReadings.TabIndex = 1;
            this.GridReadings.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewReadings});
            //
            // GridViewReadings
            //
            this.GridViewReadings.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColRdContract, this.ColRdSerial, this.ColRdCounter, this.ColRdTheirs,
                this.ColRdDate, this.ColRdStatus});
            this.GridViewReadings.GridControl = this.GridReadings;
            this.GridViewReadings.Name = "GridViewReadings";
            this.GridViewReadings.OptionsBehavior.Editable = false;
            this.GridViewReadings.OptionsView.ShowGroupPanel = false;
            //
            // ColRdContract
            //
            this.ColRdContract.Caption = "My contract";
            this.ColRdContract.FieldName = "ContractNo";
            this.ColRdContract.Name = "ColRdContract";
            this.ColRdContract.Visible = true;
            this.ColRdContract.VisibleIndex = 0;
            this.ColRdContract.Width = 120;
            //
            // ColRdSerial
            //
            this.ColRdSerial.Caption = "Serial no";
            this.ColRdSerial.FieldName = "SerialNumber";
            this.ColRdSerial.Name = "ColRdSerial";
            this.ColRdSerial.Visible = true;
            this.ColRdSerial.VisibleIndex = 1;
            this.ColRdSerial.Width = 130;
            //
            // ColRdCounter
            //
            this.ColRdCounter.Caption = "Counter";
            this.ColRdCounter.FieldName = "Counter";
            this.ColRdCounter.Name = "ColRdCounter";
            this.ColRdCounter.Visible = true;
            this.ColRdCounter.VisibleIndex = 2;
            this.ColRdCounter.Width = 160;
            //
            // ColRdTheirs
            //
            this.ColRdTheirs.Caption = "Their reading";
            this.ColRdTheirs.DisplayFormat.FormatString = "#,##0";
            this.ColRdTheirs.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColRdTheirs.FieldName = "TheirReading";
            this.ColRdTheirs.Name = "ColRdTheirs";
            this.ColRdTheirs.Visible = true;
            this.ColRdTheirs.VisibleIndex = 3;
            this.ColRdTheirs.Width = 100;
            //
            // ColRdDate
            //
            this.ColRdDate.Caption = "Read on";
            this.ColRdDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColRdDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColRdDate.FieldName = "ReadingDate";
            this.ColRdDate.Name = "ColRdDate";
            this.ColRdDate.Visible = true;
            this.ColRdDate.VisibleIndex = 4;
            this.ColRdDate.Width = 90;
            //
            // ColRdStatus
            //
            this.ColRdStatus.Caption = "Here";
            this.ColRdStatus.FieldName = "Status";
            this.ColRdStatus.Name = "ColRdStatus";
            this.ColRdStatus.Visible = true;
            this.ColRdStatus.VisibleIndex = 5;
            this.ColRdStatus.Width = 260;
            //
            // TabBill
            //
            this.TabBill.Controls.Add(this.GridBill);
            this.TabBill.Controls.Add(this.PanelBillBar);
            this.TabBill.Name = "TabBill";
            this.TabBill.Size = new System.Drawing.Size(1194, 553);
            this.TabBill.Text = "4. Bill one contract";
            //
            // PanelBillBar
            //
            this.PanelBillBar.Controls.Add(this.LblBillPeriod);
            this.PanelBillBar.Controls.Add(this.SpnBillYear);
            this.PanelBillBar.Controls.Add(this.SpnBillMonth);
            this.PanelBillBar.Controls.Add(this.BtnPreviewInvoice);
            this.PanelBillBar.Controls.Add(this.BtnGenerateInvoice);
            this.PanelBillBar.Controls.Add(this.LblBillNote);
            this.PanelBillBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBillBar.Location = new System.Drawing.Point(0, 483);
            this.PanelBillBar.Name = "PanelBillBar";
            this.PanelBillBar.Size = new System.Drawing.Size(1194, 70);
            this.PanelBillBar.TabIndex = 1;
            //
            // LblBillPeriod
            //
            this.LblBillPeriod.Location = new System.Drawing.Point(12, 26);
            this.LblBillPeriod.Name = "LblBillPeriod";
            this.LblBillPeriod.Size = new System.Drawing.Size(70, 13);
            this.LblBillPeriod.TabIndex = 0;
            this.LblBillPeriod.Text = "Year / month";
            //
            // SpnBillYear
            //
            this.SpnBillYear.EditValue = "2026";
            this.SpnBillYear.Location = new System.Drawing.Point(96, 22);
            this.SpnBillYear.Name = "SpnBillYear";
            this.SpnBillYear.Properties.MaxValue = new decimal(new int[] { 2999, 0, 0, 0 });
            this.SpnBillYear.Properties.MinValue = new decimal(new int[] { 2000, 0, 0, 0 });
            this.SpnBillYear.Size = new System.Drawing.Size(80, 20);
            this.SpnBillYear.TabIndex = 1;
            //
            // SpnBillMonth
            //
            this.SpnBillMonth.EditValue = "1";
            this.SpnBillMonth.Location = new System.Drawing.Point(184, 22);
            this.SpnBillMonth.Name = "SpnBillMonth";
            this.SpnBillMonth.Properties.MaxValue = new decimal(new int[] { 12, 0, 0, 0 });
            this.SpnBillMonth.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
            this.SpnBillMonth.Size = new System.Drawing.Size(60, 20);
            this.SpnBillMonth.TabIndex = 2;
            //
            // BtnPreviewInvoice
            //
            this.BtnPreviewInvoice.Location = new System.Drawing.Point(264, 18);
            this.BtnPreviewInvoice.Name = "BtnPreviewInvoice";
            this.BtnPreviewInvoice.Size = new System.Drawing.Size(150, 28);
            this.BtnPreviewInvoice.TabIndex = 3;
            this.BtnPreviewInvoice.Text = "Preview invoice";
            this.BtnPreviewInvoice.Click += new System.EventHandler(this.OnPreviewInvoice);
            //
            // BtnGenerateInvoice
            //
            this.BtnGenerateInvoice.Location = new System.Drawing.Point(424, 18);
            this.BtnGenerateInvoice.Name = "BtnGenerateInvoice";
            this.BtnGenerateInvoice.Size = new System.Drawing.Size(150, 28);
            this.BtnGenerateInvoice.TabIndex = 4;
            this.BtnGenerateInvoice.Text = "Generate invoice";
            this.BtnGenerateInvoice.Click += new System.EventHandler(this.OnGenerateInvoice);
            //
            // LblBillNote
            //
            this.LblBillNote.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblBillNote.Location = new System.Drawing.Point(590, 14);
            this.LblBillNote.Name = "LblBillNote";
            this.LblBillNote.Size = new System.Drawing.Size(580, 42);
            this.LblBillNote.TabIndex = 5;
            this.LblBillNote.Text = "One contract at a time. The preview and the invoice are the same ones Meter Reading Integration produces - the same prices, the same rules.";
            //
            // GridBill
            //
            this.GridBill.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridBill.Location = new System.Drawing.Point(0, 0);
            this.GridBill.MainView = this.GridViewBill;
            this.GridBill.Name = "GridBill";
            this.GridBill.Size = new System.Drawing.Size(1194, 483);
            this.GridBill.TabIndex = 0;
            this.GridBill.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewBill});
            //
            // GridViewBill
            //
            this.GridViewBill.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColBlContract, this.ColBlCustomer, this.ColBlMachines, this.ColBlReadings,
                this.ColBlInvoiced});
            this.GridViewBill.GridControl = this.GridBill;
            this.GridViewBill.Name = "GridViewBill";
            this.GridViewBill.OptionsBehavior.Editable = false;
            this.GridViewBill.OptionsView.ShowGroupPanel = false;
            //
            // ColBlContract
            //
            this.ColBlContract.Caption = "My contract";
            this.ColBlContract.FieldName = "ContractNo";
            this.ColBlContract.Name = "ColBlContract";
            this.ColBlContract.Visible = true;
            this.ColBlContract.VisibleIndex = 0;
            this.ColBlContract.Width = 140;
            //
            // ColBlCustomer
            //
            this.ColBlCustomer.Caption = "Billed to";
            this.ColBlCustomer.FieldName = "Customer";
            this.ColBlCustomer.Name = "ColBlCustomer";
            this.ColBlCustomer.Visible = true;
            this.ColBlCustomer.VisibleIndex = 1;
            this.ColBlCustomer.Width = 260;
            //
            // ColBlMachines
            //
            this.ColBlMachines.Caption = "Machines";
            this.ColBlMachines.FieldName = "Machines";
            this.ColBlMachines.Name = "ColBlMachines";
            this.ColBlMachines.Visible = true;
            this.ColBlMachines.VisibleIndex = 2;
            this.ColBlMachines.Width = 80;
            //
            // ColBlReadings
            //
            this.ColBlReadings.Caption = "Readings this period";
            this.ColBlReadings.FieldName = "Readings";
            this.ColBlReadings.Name = "ColBlReadings";
            this.ColBlReadings.Visible = true;
            this.ColBlReadings.VisibleIndex = 3;
            this.ColBlReadings.Width = 150;
            //
            // ColBlInvoiced
            //
            this.ColBlInvoiced.Caption = "Already invoiced";
            this.ColBlInvoiced.FieldName = "Invoiced";
            this.ColBlInvoiced.Name = "ColBlInvoiced";
            this.ColBlInvoiced.Visible = true;
            this.ColBlInvoiced.VisibleIndex = 4;
            this.ColBlInvoiced.Width = 260;
            //
            // InterBilling_Form
            //
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.TabMain);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.Name = "InterBilling_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Inter-Billing";
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillYear.Properties)).EndInit();
            this.PanelBillBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBillBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewBill)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridBill)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnYear.Properties)).EndInit();
            this.PanelReadBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelReadBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewReadings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridReadings)).EndInit();
            this.PanelChangeBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelChangeBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewChanges)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridChanges)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMargin.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtorView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtor.Properties)).EndInit();
            this.PanelTakeBar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelTakeBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewContracts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridContracts)).EndInit();
            this.TabBill.ResumeLayout(false);
            this.TabReadings.ResumeLayout(false);
            this.TabChanges.ResumeLayout(false);
            this.TabIncoming.ResumeLayout(false);
            this.TabMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TabMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CboBook.Properties)).EndInit();
            this.PanelToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
