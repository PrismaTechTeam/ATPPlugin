namespace ATPShadowMain
{
    partial class ShadowLauncherV2_Form
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

        // Chrome
        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblBreadcrumb;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraNavBar.NavBarControl NavLeft;
        private DevExpress.XtraTab.XtraTabControl TabsMain;
        private DevExpress.XtraEditors.LabelControl LblStatus;

        // Master tab dashboard scaffold
        private DevExpress.XtraTab.XtraTabPage TabPageMaster;
        private DevExpress.XtraEditors.PanelControl PanelDashboard;
        private DevExpress.XtraEditors.LabelControl LblTitle;
        private DevExpress.XtraEditors.LabelControl LblSubtitle;
        private DevExpress.XtraEditors.LabelControl LblQuickAccess;

        // Nav groups (11)
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_ServiceContract;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_ServiceItem;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_ServiceNote;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_ServiceAppointment;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_SetupPeople;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_SetupLookups;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_SetupMeter;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_StockRequest;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_ServiceOption;
        private DevExpress.XtraNavBar.NavBarGroup NavGroup_Reports;

        // Service Contract items
        private DevExpress.XtraNavBar.NavBarItem NavItem_MaintainServiceContract;
        private DevExpress.XtraNavBar.NavBarItem NavItem_NewServiceContract;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceContractInquiry;
        private DevExpress.XtraNavBar.NavBarItem NavItem_OutstandingContractItem;

        // Service Item items
        private DevExpress.XtraNavBar.NavBarItem NavItem_MaintainServiceItem;
        private DevExpress.XtraNavBar.NavBarItem NavItem_NewServiceItem;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceItemInquiry;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceItemTagSearch;
        private DevExpress.XtraNavBar.NavBarItem NavItem_GenerateItemFromSerial;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ResetItemDebtorOwnership;

        // Service Note items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteList;
        private DevExpress.XtraNavBar.NavBarItem NavItem_NewServiceNote;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteQuickEntry;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteClosing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteAssignment;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteInquiry;
        private DevExpress.XtraNavBar.NavBarItem NavItem_OutstandingNoteAssignment;

        // Service Appointment items
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentList;
        private DevExpress.XtraNavBar.NavBarItem NavItem_NewAppointment;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentCalendar;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentInquiry;
        private DevExpress.XtraNavBar.NavBarItem NavItem_PreventiveMaintenance;
        private DevExpress.XtraNavBar.NavBarItem NavItem_MeterTypeTransEntry;

        // Quick View items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceQuickView;

        // Setup - People items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServicePerson;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceAdvisor;
        private DevExpress.XtraNavBar.NavBarItem NavItem_Mechanic;

        // Setup - Lookups items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceStatus;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceSeverity;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceSolution;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceProblem;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceType;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceContractType;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceItemGrade;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentType;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentPriority;

        // Setup - Meter items
        private DevExpress.XtraNavBar.NavBarItem NavItem_MeterType;
        private DevExpress.XtraNavBar.NavBarItem NavItem_MeterMultiPricing;

        // Stock Request items
        private DevExpress.XtraNavBar.NavBarItem NavItem_StockRequestIntegration;

        // Service Option items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceOption;

        // Reports items
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceStatusListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceSeverityListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceSolutionListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceProblemListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceTypeListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceContractTypeListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceItemGradeListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentTypeListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_AppointmentPriorityListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServicePersonListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceAdvisorListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_MechanicListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_MeterTypeListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceNoteListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceContractListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_ServiceItemListing;
        private DevExpress.XtraNavBar.NavBarItem NavItem_TopServiceStockCode;
        private DevExpress.XtraNavBar.NavBarItem NavItem_TopServiceStockCodeByDept;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShadowLauncherV2_Form));
            this.PanelTop = new DevExpress.XtraEditors.PanelControl();
            this.LblBreadcrumb = new DevExpress.XtraEditors.LabelControl();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.NavLeft = new DevExpress.XtraNavBar.NavBarControl();
            this.NavGroup_ServiceAppointment = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_AppointmentList = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_NewAppointment = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentCalendar = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentInquiry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_PreventiveMaintenance = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_MeterTypeTransEntry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_ServiceContract = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_MaintainServiceContract = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_NewServiceContract = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceContractInquiry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_OutstandingContractItem = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_ServiceItem = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_MaintainServiceItem = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_NewServiceItem = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceItemInquiry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceItemTagSearch = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_GenerateItemFromSerial = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ResetItemDebtorOwnership = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_ServiceNote = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_ServiceNoteList = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_NewServiceNote = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceNoteQuickEntry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceNoteClosing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceNoteAssignment = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceNoteInquiry = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_OutstandingNoteAssignment = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceQuickView = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_SetupPeople = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_ServicePerson = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceAdvisor = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_Mechanic = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_SetupLookups = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_ServiceStatus = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceSeverity = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceSolution = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceProblem = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceType = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceContractType = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceItemGrade = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentType = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentPriority = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_SetupMeter = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_MeterType = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_MeterMultiPricing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_StockRequest = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_StockRequestIntegration = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_ServiceOption = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_ServiceOption = new DevExpress.XtraNavBar.NavBarItem();
            this.NavGroup_Reports = new DevExpress.XtraNavBar.NavBarGroup();
            this.NavItem_ServiceStatusListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceSeverityListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceSolutionListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceProblemListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceTypeListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceContractTypeListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceItemGradeListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentTypeListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_AppointmentPriorityListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServicePersonListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceAdvisorListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_MechanicListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_MeterTypeListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceNoteListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceContractListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_ServiceItemListing = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_TopServiceStockCode = new DevExpress.XtraNavBar.NavBarItem();
            this.NavItem_TopServiceStockCodeByDept = new DevExpress.XtraNavBar.NavBarItem();
            this.TabsMain = new DevExpress.XtraTab.XtraTabControl();
            this.TabPageMaster = new DevExpress.XtraTab.XtraTabPage();
            this.PanelDashboard = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle = new DevExpress.XtraEditors.LabelControl();
            this.LblSubtitle = new DevExpress.XtraEditors.LabelControl();
            this.LblQuickAccess = new DevExpress.XtraEditors.LabelControl();
            this.LblStatus = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NavLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TabsMain)).BeginInit();
            this.TabsMain.SuspendLayout();
            this.TabPageMaster.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDashboard)).BeginInit();
            this.PanelDashboard.SuspendLayout();
            this.SuspendLayout();
            // 
            // PanelTop
            // 
            this.PanelTop.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(179)))), ((int)(((byte)(66)))));
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.PanelTop.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTop.Controls.Add(this.LblBreadcrumb);
            this.PanelTop.Controls.Add(this.BtnRefresh);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(1097, 46);
            this.PanelTop.TabIndex = 0;
            // 
            // LblBreadcrumb
            // 
            this.LblBreadcrumb.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.LblBreadcrumb.Appearance.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.LblBreadcrumb.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblBreadcrumb.Appearance.Options.UseBackColor = true;
            this.LblBreadcrumb.Appearance.Options.UseFont = true;
            this.LblBreadcrumb.Appearance.Options.UseForeColor = true;
            this.LblBreadcrumb.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblBreadcrumb.Location = new System.Drawing.Point(17, 13);
            this.LblBreadcrumb.Name = "LblBreadcrumb";
            this.LblBreadcrumb.Size = new System.Drawing.Size(771, 26);
            this.LblBreadcrumb.TabIndex = 0;
            this.LblBreadcrumb.Text = "ATP  /  Master Menu";
            // 
            // BtnRefresh
            // 
            this.BtnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRefresh.Location = new System.Drawing.Point(1003, 10);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(77, 26);
            this.BtnRefresh.TabIndex = 1;
            this.BtnRefresh.Text = "Refresh";
            // 
            // NavLeft
            // 
            this.NavLeft.ActiveGroup = this.NavGroup_ServiceAppointment;
            this.NavLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.NavLeft.Groups.AddRange(new DevExpress.XtraNavBar.NavBarGroup[] {
            this.NavGroup_ServiceContract,
            this.NavGroup_ServiceItem,
            this.NavGroup_ServiceNote,
            this.NavGroup_ServiceAppointment,
            this.NavGroup_SetupPeople,
            this.NavGroup_SetupLookups,
            this.NavGroup_SetupMeter,
            this.NavGroup_StockRequest,
            this.NavGroup_ServiceOption,
            this.NavGroup_Reports});
            this.NavLeft.Items.AddRange(new DevExpress.XtraNavBar.NavBarItem[] {
            this.NavItem_MaintainServiceContract,
            this.NavItem_NewServiceContract,
            this.NavItem_ServiceContractInquiry,
            this.NavItem_OutstandingContractItem,
            this.NavItem_MaintainServiceItem,
            this.NavItem_NewServiceItem,
            this.NavItem_ServiceItemInquiry,
            this.NavItem_ServiceItemTagSearch,
            this.NavItem_GenerateItemFromSerial,
            this.NavItem_ResetItemDebtorOwnership,
            this.NavItem_ServiceNoteList,
            this.NavItem_NewServiceNote,
            this.NavItem_ServiceNoteQuickEntry,
            this.NavItem_ServiceNoteClosing,
            this.NavItem_ServiceNoteAssignment,
            this.NavItem_ServiceNoteInquiry,
            this.NavItem_OutstandingNoteAssignment,
            this.NavItem_AppointmentList,
            this.NavItem_NewAppointment,
            this.NavItem_AppointmentCalendar,
            this.NavItem_AppointmentInquiry,
            this.NavItem_PreventiveMaintenance,
            this.NavItem_MeterTypeTransEntry,
            this.NavItem_ServiceQuickView,
            this.NavItem_ServicePerson,
            this.NavItem_ServiceAdvisor,
            this.NavItem_Mechanic,
            this.NavItem_ServiceStatus,
            this.NavItem_ServiceSeverity,
            this.NavItem_ServiceSolution,
            this.NavItem_ServiceProblem,
            this.NavItem_ServiceType,
            this.NavItem_ServiceContractType,
            this.NavItem_ServiceItemGrade,
            this.NavItem_AppointmentType,
            this.NavItem_AppointmentPriority,
            this.NavItem_MeterType,
            this.NavItem_MeterMultiPricing,
            this.NavItem_StockRequestIntegration,
            this.NavItem_ServiceOption,
            this.NavItem_ServiceStatusListing,
            this.NavItem_ServiceSeverityListing,
            this.NavItem_ServiceSolutionListing,
            this.NavItem_ServiceProblemListing,
            this.NavItem_ServiceTypeListing,
            this.NavItem_ServiceContractTypeListing,
            this.NavItem_ServiceItemGradeListing,
            this.NavItem_AppointmentTypeListing,
            this.NavItem_AppointmentPriorityListing,
            this.NavItem_ServicePersonListing,
            this.NavItem_ServiceAdvisorListing,
            this.NavItem_MechanicListing,
            this.NavItem_MeterTypeListing,
            this.NavItem_ServiceNoteListing,
            this.NavItem_ServiceContractListing,
            this.NavItem_ServiceItemListing,
            this.NavItem_TopServiceStockCode,
            this.NavItem_TopServiceStockCodeByDept});
            this.NavLeft.Location = new System.Drawing.Point(0, 46);
            this.NavLeft.Name = "NavLeft";
            this.NavLeft.OptionsNavPane.ExpandedWidth = 206;
            this.NavLeft.OptionsNavPane.ShowExpandButton = false;
            this.NavLeft.OptionsNavPane.ShowOverflowButton = false;
            this.NavLeft.PaintStyleKind = DevExpress.XtraNavBar.NavBarViewKind.NavigationPane;
            this.NavLeft.Size = new System.Drawing.Size(206, 677);
            this.NavLeft.TabIndex = 1;
            this.NavLeft.Text = "navBarControl";
            // 
            // NavGroup_ServiceAppointment
            // 
            this.NavGroup_ServiceAppointment.Caption = "Service Appointment";
            this.NavGroup_ServiceAppointment.Expanded = true;
            this.NavGroup_ServiceAppointment.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_ServiceAppointment.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceAppointment.ImageOptions.LargeImage")));
            this.NavGroup_ServiceAppointment.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceAppointment.ImageOptions.SmallImage")));
            this.NavGroup_ServiceAppointment.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentList),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_NewAppointment),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentCalendar),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentInquiry),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_PreventiveMaintenance),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MeterTypeTransEntry)});
            this.NavGroup_ServiceAppointment.Name = "NavGroup_ServiceAppointment";
            // 
            // NavItem_AppointmentList
            // 
            this.NavItem_AppointmentList.Caption = "Appointment List";
            this.NavItem_AppointmentList.Name = "NavItem_AppointmentList";
            // 
            // NavItem_NewAppointment
            // 
            this.NavItem_NewAppointment.Caption = "New Appointment";
            this.NavItem_NewAppointment.Name = "NavItem_NewAppointment";
            // 
            // NavItem_AppointmentCalendar
            // 
            this.NavItem_AppointmentCalendar.Caption = "Appointment Calendar";
            this.NavItem_AppointmentCalendar.Name = "NavItem_AppointmentCalendar";
            // 
            // NavItem_AppointmentInquiry
            // 
            this.NavItem_AppointmentInquiry.Caption = "Appointment Inquiry";
            this.NavItem_AppointmentInquiry.Name = "NavItem_AppointmentInquiry";
            // 
            // NavItem_PreventiveMaintenance
            // 
            this.NavItem_PreventiveMaintenance.Caption = "Preventive Maintenance";
            this.NavItem_PreventiveMaintenance.Name = "NavItem_PreventiveMaintenance";
            // 
            // NavItem_MeterTypeTransEntry
            // 
            this.NavItem_MeterTypeTransEntry.Caption = "Meter Type Trans Entry";
            this.NavItem_MeterTypeTransEntry.Name = "NavItem_MeterTypeTransEntry";
            // 
            // NavGroup_ServiceContract
            // 
            this.NavGroup_ServiceContract.Caption = "Service Contract";
            this.NavGroup_ServiceContract.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_ServiceContract.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceContract.ImageOptions.LargeImage")));
            this.NavGroup_ServiceContract.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceContract.ImageOptions.SmallImage")));
            this.NavGroup_ServiceContract.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MaintainServiceContract),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_NewServiceContract),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceContractInquiry),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_OutstandingContractItem)});
            this.NavGroup_ServiceContract.Name = "NavGroup_ServiceContract";
            // 
            // NavItem_MaintainServiceContract
            // 
            this.NavItem_MaintainServiceContract.Caption = "Maintain Service Contract";
            this.NavItem_MaintainServiceContract.Name = "NavItem_MaintainServiceContract";
            // 
            // NavItem_NewServiceContract
            // 
            this.NavItem_NewServiceContract.Caption = "New Service Contract";
            this.NavItem_NewServiceContract.Name = "NavItem_NewServiceContract";
            // 
            // NavItem_ServiceContractInquiry
            // 
            this.NavItem_ServiceContractInquiry.Caption = "Service Contract Inquiry";
            this.NavItem_ServiceContractInquiry.Name = "NavItem_ServiceContractInquiry";
            // 
            // NavItem_OutstandingContractItem
            // 
            this.NavItem_OutstandingContractItem.Caption = "Outstanding Contract Item";
            this.NavItem_OutstandingContractItem.Name = "NavItem_OutstandingContractItem";
            // 
            // NavGroup_ServiceItem
            // 
            this.NavGroup_ServiceItem.Caption = "Service Item";
            this.NavGroup_ServiceItem.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_ServiceItem.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceItem.ImageOptions.LargeImage")));
            this.NavGroup_ServiceItem.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceItem.ImageOptions.SmallImage")));
            this.NavGroup_ServiceItem.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MaintainServiceItem),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_NewServiceItem),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceItemInquiry),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceItemTagSearch),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_GenerateItemFromSerial),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ResetItemDebtorOwnership)});
            this.NavGroup_ServiceItem.Name = "NavGroup_ServiceItem";
            // 
            // NavItem_MaintainServiceItem
            // 
            this.NavItem_MaintainServiceItem.Caption = "Maintain Service Item";
            this.NavItem_MaintainServiceItem.Name = "NavItem_MaintainServiceItem";
            // 
            // NavItem_NewServiceItem
            // 
            this.NavItem_NewServiceItem.Caption = "New Service Item";
            this.NavItem_NewServiceItem.Name = "NavItem_NewServiceItem";
            // 
            // NavItem_ServiceItemInquiry
            // 
            this.NavItem_ServiceItemInquiry.Caption = "Service Item Inquiry";
            this.NavItem_ServiceItemInquiry.Name = "NavItem_ServiceItemInquiry";
            // 
            // NavItem_ServiceItemTagSearch
            // 
            this.NavItem_ServiceItemTagSearch.Caption = "Service Item Tag Search";
            this.NavItem_ServiceItemTagSearch.Name = "NavItem_ServiceItemTagSearch";
            // 
            // NavItem_GenerateItemFromSerial
            // 
            this.NavItem_GenerateItemFromSerial.Caption = "Generate Item From Serial";
            this.NavItem_GenerateItemFromSerial.Name = "NavItem_GenerateItemFromSerial";
            // 
            // NavItem_ResetItemDebtorOwnership
            // 
            this.NavItem_ResetItemDebtorOwnership.Caption = "Reset Item Debtor Ownership";
            this.NavItem_ResetItemDebtorOwnership.Name = "NavItem_ResetItemDebtorOwnership";
            // 
            // NavGroup_ServiceNote
            // 
            this.NavGroup_ServiceNote.Caption = "Service Note";
            this.NavGroup_ServiceNote.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_ServiceNote.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceNote.ImageOptions.LargeImage")));
            this.NavGroup_ServiceNote.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceNote.ImageOptions.SmallImage")));
            this.NavGroup_ServiceNote.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteList),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_NewServiceNote),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteQuickEntry),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteClosing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteAssignment),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteInquiry),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_OutstandingNoteAssignment)});
            this.NavGroup_ServiceNote.Name = "NavGroup_ServiceNote";
            // 
            // NavItem_ServiceNoteList
            // 
            this.NavItem_ServiceNoteList.Caption = "Service Note List";
            this.NavItem_ServiceNoteList.Name = "NavItem_ServiceNoteList";
            // 
            // NavItem_NewServiceNote
            // 
            this.NavItem_NewServiceNote.Caption = "New Service Note";
            this.NavItem_NewServiceNote.Name = "NavItem_NewServiceNote";
            // 
            // NavItem_ServiceNoteQuickEntry
            // 
            this.NavItem_ServiceNoteQuickEntry.Caption = "Service Note Quick Entry";
            this.NavItem_ServiceNoteQuickEntry.Name = "NavItem_ServiceNoteQuickEntry";
            // 
            // NavItem_ServiceNoteClosing
            // 
            this.NavItem_ServiceNoteClosing.Caption = "Service Note Closing";
            this.NavItem_ServiceNoteClosing.Name = "NavItem_ServiceNoteClosing";
            // 
            // NavItem_ServiceNoteAssignment
            // 
            this.NavItem_ServiceNoteAssignment.Caption = "Service Note Assignment";
            this.NavItem_ServiceNoteAssignment.Name = "NavItem_ServiceNoteAssignment";
            // 
            // NavItem_ServiceNoteInquiry
            // 
            this.NavItem_ServiceNoteInquiry.Caption = "Service Note Inquiry";
            this.NavItem_ServiceNoteInquiry.Name = "NavItem_ServiceNoteInquiry";
            // 
            // NavItem_OutstandingNoteAssignment
            // 
            this.NavItem_OutstandingNoteAssignment.Caption = "Outstanding Note Assignment";
            this.NavItem_OutstandingNoteAssignment.Name = "NavItem_OutstandingNoteAssignment";
            // 
            // NavItem_ServiceQuickView
            // 
            this.NavItem_ServiceQuickView.Caption = "Service Quick View";
            this.NavItem_ServiceQuickView.Name = "NavItem_ServiceQuickView";
            // 
            // NavGroup_SetupPeople
            // 
            this.NavGroup_SetupPeople.Caption = "Setup - People";
            this.NavGroup_SetupPeople.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_SetupPeople.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupPeople.ImageOptions.LargeImage")));
            this.NavGroup_SetupPeople.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupPeople.ImageOptions.SmallImage")));
            this.NavGroup_SetupPeople.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServicePerson),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceAdvisor),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_Mechanic)});
            this.NavGroup_SetupPeople.Name = "NavGroup_SetupPeople";
            // 
            // NavItem_ServicePerson
            // 
            this.NavItem_ServicePerson.Caption = "Service Person";
            this.NavItem_ServicePerson.Name = "NavItem_ServicePerson";
            // 
            // NavItem_ServiceAdvisor
            // 
            this.NavItem_ServiceAdvisor.Caption = "Service Advisor";
            this.NavItem_ServiceAdvisor.Name = "NavItem_ServiceAdvisor";
            // 
            // NavItem_Mechanic
            // 
            this.NavItem_Mechanic.Caption = "Mechanic";
            this.NavItem_Mechanic.Name = "NavItem_Mechanic";
            // 
            // NavGroup_SetupLookups
            // 
            this.NavGroup_SetupLookups.Caption = "Setup - Lookups";
            this.NavGroup_SetupLookups.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_SetupLookups.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupLookups.ImageOptions.LargeImage")));
            this.NavGroup_SetupLookups.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupLookups.ImageOptions.SmallImage")));
            this.NavGroup_SetupLookups.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceStatus),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceSeverity),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceSolution),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceProblem),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceType),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceContractType),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceItemGrade),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentType),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentPriority)});
            this.NavGroup_SetupLookups.Name = "NavGroup_SetupLookups";
            // 
            // NavItem_ServiceStatus
            // 
            this.NavItem_ServiceStatus.Caption = "Service Status";
            this.NavItem_ServiceStatus.Name = "NavItem_ServiceStatus";
            // 
            // NavItem_ServiceSeverity
            // 
            this.NavItem_ServiceSeverity.Caption = "Service Severity";
            this.NavItem_ServiceSeverity.Name = "NavItem_ServiceSeverity";
            // 
            // NavItem_ServiceSolution
            // 
            this.NavItem_ServiceSolution.Caption = "Service Solution";
            this.NavItem_ServiceSolution.Name = "NavItem_ServiceSolution";
            // 
            // NavItem_ServiceProblem
            // 
            this.NavItem_ServiceProblem.Caption = "Service Problem";
            this.NavItem_ServiceProblem.Name = "NavItem_ServiceProblem";
            // 
            // NavItem_ServiceType
            // 
            this.NavItem_ServiceType.Caption = "Service Type";
            this.NavItem_ServiceType.Name = "NavItem_ServiceType";
            // 
            // NavItem_ServiceContractType
            // 
            this.NavItem_ServiceContractType.Caption = "Service Contract Type";
            this.NavItem_ServiceContractType.Name = "NavItem_ServiceContractType";
            // 
            // NavItem_ServiceItemGrade
            // 
            this.NavItem_ServiceItemGrade.Caption = "Service Item Grade";
            this.NavItem_ServiceItemGrade.Name = "NavItem_ServiceItemGrade";
            // 
            // NavItem_AppointmentType
            // 
            this.NavItem_AppointmentType.Caption = "Appointment Type";
            this.NavItem_AppointmentType.Name = "NavItem_AppointmentType";
            // 
            // NavItem_AppointmentPriority
            // 
            this.NavItem_AppointmentPriority.Caption = "Appointment Priority";
            this.NavItem_AppointmentPriority.Name = "NavItem_AppointmentPriority";
            // 
            // NavGroup_SetupMeter
            // 
            this.NavGroup_SetupMeter.Caption = "Setup - Meter";
            this.NavGroup_SetupMeter.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_SetupMeter.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupMeter.ImageOptions.LargeImage")));
            this.NavGroup_SetupMeter.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_SetupMeter.ImageOptions.SmallImage")));
            this.NavGroup_SetupMeter.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MeterType),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MeterMultiPricing)});
            this.NavGroup_SetupMeter.Name = "NavGroup_SetupMeter";
            // 
            // NavItem_MeterType
            // 
            this.NavItem_MeterType.Caption = "Meter Type";
            this.NavItem_MeterType.Name = "NavItem_MeterType";
            // 
            // NavItem_MeterMultiPricing
            // 
            this.NavItem_MeterMultiPricing.Caption = "Meter Multi Pricing";
            this.NavItem_MeterMultiPricing.Name = "NavItem_MeterMultiPricing";
            // 
            // NavGroup_StockRequest
            // 
            this.NavGroup_StockRequest.Caption = "Stock Request";
            this.NavGroup_StockRequest.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_StockRequest.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_StockRequest.ImageOptions.LargeImage")));
            this.NavGroup_StockRequest.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_StockRequest.ImageOptions.SmallImage")));
            this.NavGroup_StockRequest.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_StockRequestIntegration)});
            this.NavGroup_StockRequest.Name = "NavGroup_StockRequest";
            // 
            // NavItem_StockRequestIntegration
            // 
            this.NavItem_StockRequestIntegration.Caption = "Stock Request Integration";
            this.NavItem_StockRequestIntegration.Name = "NavItem_StockRequestIntegration";
            // 
            // NavGroup_ServiceOption
            // 
            this.NavGroup_ServiceOption.Caption = "Service Option";
            this.NavGroup_ServiceOption.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_ServiceOption.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceOption.ImageOptions.LargeImage")));
            this.NavGroup_ServiceOption.ImageOptions.SmallImage = ((System.Drawing.Image)(resources.GetObject("NavGroup_ServiceOption.ImageOptions.SmallImage")));
            this.NavGroup_ServiceOption.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceOption)});
            this.NavGroup_ServiceOption.Name = "NavGroup_ServiceOption";
            // 
            // NavItem_ServiceOption
            // 
            this.NavItem_ServiceOption.Caption = "Service Option";
            this.NavItem_ServiceOption.Name = "NavItem_ServiceOption";
            // 
            // NavGroup_Reports
            // 
            this.NavGroup_Reports.Caption = "Reports";
            this.NavGroup_Reports.GroupStyle = DevExpress.XtraNavBar.NavBarGroupStyle.SmallIconsText;
            this.NavGroup_Reports.ItemLinks.AddRange(new DevExpress.XtraNavBar.NavBarItemLink[] {
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceStatusListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceSeverityListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceSolutionListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceProblemListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceTypeListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceContractTypeListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceItemGradeListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentTypeListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_AppointmentPriorityListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServicePersonListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceAdvisorListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MechanicListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_MeterTypeListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceNoteListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceContractListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_ServiceItemListing),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_TopServiceStockCode),
            new DevExpress.XtraNavBar.NavBarItemLink(this.NavItem_TopServiceStockCodeByDept)});
            this.NavGroup_Reports.Name = "NavGroup_Reports";
            // 
            // NavItem_ServiceStatusListing
            // 
            this.NavItem_ServiceStatusListing.Caption = "Service Status Listing";
            this.NavItem_ServiceStatusListing.Name = "NavItem_ServiceStatusListing";
            // 
            // NavItem_ServiceSeverityListing
            // 
            this.NavItem_ServiceSeverityListing.Caption = "Service Severity Listing";
            this.NavItem_ServiceSeverityListing.Name = "NavItem_ServiceSeverityListing";
            // 
            // NavItem_ServiceSolutionListing
            // 
            this.NavItem_ServiceSolutionListing.Caption = "Service Solution Listing";
            this.NavItem_ServiceSolutionListing.Name = "NavItem_ServiceSolutionListing";
            // 
            // NavItem_ServiceProblemListing
            // 
            this.NavItem_ServiceProblemListing.Caption = "Service Problem Listing";
            this.NavItem_ServiceProblemListing.Name = "NavItem_ServiceProblemListing";
            // 
            // NavItem_ServiceTypeListing
            // 
            this.NavItem_ServiceTypeListing.Caption = "Service Type Listing";
            this.NavItem_ServiceTypeListing.Name = "NavItem_ServiceTypeListing";
            // 
            // NavItem_ServiceContractTypeListing
            // 
            this.NavItem_ServiceContractTypeListing.Caption = "Service Contract Type Listing";
            this.NavItem_ServiceContractTypeListing.Name = "NavItem_ServiceContractTypeListing";
            // 
            // NavItem_ServiceItemGradeListing
            // 
            this.NavItem_ServiceItemGradeListing.Caption = "Service Item Grade Listing";
            this.NavItem_ServiceItemGradeListing.Name = "NavItem_ServiceItemGradeListing";
            // 
            // NavItem_AppointmentTypeListing
            // 
            this.NavItem_AppointmentTypeListing.Caption = "Appointment Type Listing";
            this.NavItem_AppointmentTypeListing.Name = "NavItem_AppointmentTypeListing";
            // 
            // NavItem_AppointmentPriorityListing
            // 
            this.NavItem_AppointmentPriorityListing.Caption = "Appointment Priority Listing";
            this.NavItem_AppointmentPriorityListing.Name = "NavItem_AppointmentPriorityListing";
            // 
            // NavItem_ServicePersonListing
            // 
            this.NavItem_ServicePersonListing.Caption = "Service Person Listing";
            this.NavItem_ServicePersonListing.Name = "NavItem_ServicePersonListing";
            // 
            // NavItem_ServiceAdvisorListing
            // 
            this.NavItem_ServiceAdvisorListing.Caption = "Service Advisor Listing";
            this.NavItem_ServiceAdvisorListing.Name = "NavItem_ServiceAdvisorListing";
            // 
            // NavItem_MechanicListing
            // 
            this.NavItem_MechanicListing.Caption = "Mechanic Listing";
            this.NavItem_MechanicListing.Name = "NavItem_MechanicListing";
            // 
            // NavItem_MeterTypeListing
            // 
            this.NavItem_MeterTypeListing.Caption = "Meter Type Listing";
            this.NavItem_MeterTypeListing.Name = "NavItem_MeterTypeListing";
            // 
            // NavItem_ServiceNoteListing
            // 
            this.NavItem_ServiceNoteListing.Caption = "Service Note Listing";
            this.NavItem_ServiceNoteListing.Name = "NavItem_ServiceNoteListing";
            // 
            // NavItem_ServiceContractListing
            // 
            this.NavItem_ServiceContractListing.Caption = "Service Contract Listing";
            this.NavItem_ServiceContractListing.Name = "NavItem_ServiceContractListing";
            // 
            // NavItem_ServiceItemListing
            // 
            this.NavItem_ServiceItemListing.Caption = "Service Item Listing";
            this.NavItem_ServiceItemListing.Name = "NavItem_ServiceItemListing";
            // 
            // NavItem_TopServiceStockCode
            // 
            this.NavItem_TopServiceStockCode.Caption = "Top Service Stock Code";
            this.NavItem_TopServiceStockCode.Name = "NavItem_TopServiceStockCode";
            // 
            // NavItem_TopServiceStockCodeByDept
            // 
            this.NavItem_TopServiceStockCodeByDept.Caption = "Top Service Stock Code By Dept";
            this.NavItem_TopServiceStockCodeByDept.Name = "NavItem_TopServiceStockCodeByDept";
            // 
            // TabsMain
            // 
            this.TabsMain.ClosePageButtonShowMode = DevExpress.XtraTab.ClosePageButtonShowMode.InActiveTabPageHeader;
            this.TabsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabsMain.HeaderOrientation = DevExpress.XtraTab.TabOrientation.Horizontal;
            this.TabsMain.Location = new System.Drawing.Point(206, 46);
            this.TabsMain.Name = "TabsMain";
            this.TabsMain.SelectedTabPage = this.TabPageMaster;
            this.TabsMain.ShowTabHeader = DevExpress.Utils.DefaultBoolean.True;
            this.TabsMain.Size = new System.Drawing.Size(891, 677);
            this.TabsMain.TabIndex = 2;
            this.TabsMain.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.TabPageMaster});
            // 
            // TabPageMaster
            // 
            this.TabPageMaster.Controls.Add(this.PanelDashboard);
            this.TabPageMaster.Name = "TabPageMaster";
            this.TabPageMaster.ShowCloseButton = DevExpress.Utils.DefaultBoolean.False;
            this.TabPageMaster.Size = new System.Drawing.Size(889, 652);
            this.TabPageMaster.Text = "Master Menu";
            // 
            // PanelDashboard
            // 
            this.PanelDashboard.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.PanelDashboard.Appearance.Options.UseBackColor = true;
            this.PanelDashboard.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelDashboard.Controls.Add(this.LblTitle);
            this.PanelDashboard.Controls.Add(this.LblSubtitle);
            this.PanelDashboard.Controls.Add(this.LblQuickAccess);
            this.PanelDashboard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PanelDashboard.Location = new System.Drawing.Point(0, 0);
            this.PanelDashboard.Name = "PanelDashboard";
            this.PanelDashboard.Size = new System.Drawing.Size(889, 652);
            this.PanelDashboard.TabIndex = 0;
            // 
            // LblTitle
            // 
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(60)))), ((int)(((byte)(110)))));
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblTitle.Location = new System.Drawing.Point(21, 17);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(686, 30);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Service && Contract Dashboard";
            // 
            // LblSubtitle
            // 
            this.LblSubtitle.Appearance.Font = new System.Drawing.Font("Tahoma", 9F);
            this.LblSubtitle.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(120)))), ((int)(((byte)(140)))));
            this.LblSubtitle.Appearance.Options.UseFont = true;
            this.LblSubtitle.Appearance.Options.UseForeColor = true;
            this.LblSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSubtitle.Location = new System.Drawing.Point(21, 48);
            this.LblSubtitle.Name = "LblSubtitle";
            this.LblSubtitle.Size = new System.Drawing.Size(686, 17);
            this.LblSubtitle.TabIndex = 1;
            this.LblSubtitle.Text = "Live counts from your AED_ATPLUGIN001 database. Click a tile below for one-click " +
    "access.";
            // 
            // LblQuickAccess
            // 
            this.LblQuickAccess.Appearance.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.LblQuickAccess.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(60)))), ((int)(((byte)(110)))));
            this.LblQuickAccess.Appearance.Options.UseFont = true;
            this.LblQuickAccess.Appearance.Options.UseForeColor = true;
            this.LblQuickAccess.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblQuickAccess.Location = new System.Drawing.Point(21, 121);
            this.LblQuickAccess.Name = "LblQuickAccess";
            this.LblQuickAccess.Size = new System.Drawing.Size(343, 24);
            this.LblQuickAccess.TabIndex = 2;
            this.LblQuickAccess.Text = "Quick Access";
            // 
            // LblStatus
            // 
            this.LblStatus.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.LblStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.LblStatus.Appearance.Options.UseBackColor = true;
            this.LblStatus.Appearance.Options.UseForeColor = true;
            this.LblStatus.Appearance.Options.UseTextOptions = true;
            this.LblStatus.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.LblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.LblStatus.Location = new System.Drawing.Point(0, 723);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Padding = new System.Windows.Forms.Padding(7, 4, 0, 0);
            this.LblStatus.Size = new System.Drawing.Size(1097, 20);
            this.LblStatus.TabIndex = 3;
            this.LblStatus.Text = "User: —    DB: —    Open tabs: 0";
            // 
            // ShadowLauncherV2_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1097, 743);
            this.Controls.Add(this.TabsMain);
            this.Controls.Add(this.NavLeft);
            this.Controls.Add(this.PanelTop);
            this.Controls.Add(this.LblStatus);
            this.MinimumSize = new System.Drawing.Size(1024, 640);
            this.Name = "ShadowLauncherV2_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ATP Shadow Launcher V2 (tabbed)";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.NavLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TabsMain)).EndInit();
            this.TabsMain.ResumeLayout(false);
            this.TabPageMaster.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelDashboard)).EndInit();
            this.PanelDashboard.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
