namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class zSCP2_Contract_Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private DevExpress.XtraBars.Ribbon.RibbonControl RibbonCtl;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageHome;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup grpSave;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup grpItem;
        private DevExpress.XtraBars.BarButtonItem barSave;
        private DevExpress.XtraBars.BarButtonItem barClose;
        private DevExpress.XtraBars.BarButtonItem barAddItem;
        private DevExpress.XtraBars.BarButtonItem barEditItem;
        private DevExpress.XtraBars.BarButtonItem barDelItem;

        private DevExpress.XtraEditors.PanelControl PanelHeaderFields;
        private DevExpress.XtraEditors.LabelControl LblContractNo;
        private DevExpress.XtraEditors.TextEdit TxtContractNo;
        private DevExpress.XtraEditors.SimpleButton BtnAutoNo;
        private DevExpress.XtraEditors.LabelControl LblType;
        private DevExpress.XtraEditors.TextEdit TxtContractType;
        private DevExpress.XtraEditors.LabelControl LblDebtor;
        private DevExpress.XtraEditors.LookUpEdit LkDebtorCode;
        private DevExpress.XtraEditors.LabelControl LblContractDate;
        private DevExpress.XtraEditors.DateEdit DtContractDate;
        private DevExpress.XtraEditors.LabelControl LblStartDate;
        private DevExpress.XtraEditors.DateEdit DtStartDate;
        private DevExpress.XtraEditors.LabelControl LblExpiryDate;
        private DevExpress.XtraEditors.DateEdit DtExpiryDate;
        private DevExpress.XtraEditors.LabelControl LblValue;
        private DevExpress.XtraEditors.SpinEdit SpnContractValue;
        private DevExpress.XtraEditors.LabelControl LblAddress;
        private DevExpress.XtraEditors.MemoEdit TxtAddress;
        private DevExpress.XtraEditors.LabelControl LblAttention;
        private DevExpress.XtraEditors.TextEdit TxtAttention;
        private DevExpress.XtraEditors.LabelControl LblPhone;
        private DevExpress.XtraEditors.TextEdit TxtPhone;
        private DevExpress.XtraEditors.LabelControl LblTerm;
        private DevExpress.XtraEditors.TextEdit TxtTerm;
        private DevExpress.XtraEditors.LabelControl LblArea;
        private DevExpress.XtraEditors.TextEdit TxtArea;
        private DevExpress.XtraEditors.LabelControl LblStaff;
        private DevExpress.XtraEditors.TextEdit TxtStaff;
        private DevExpress.XtraEditors.LabelControl LblBillDay;
        private DevExpress.XtraEditors.SpinEdit SpnBillingDay;
        private DevExpress.XtraEditors.LabelControl LblBillMode;
        private DevExpress.XtraEditors.CheckEdit ChkBillGroup;
        private DevExpress.XtraEditors.CheckEdit ChkBillSeparate;
        private DevExpress.XtraEditors.LabelControl LblDesc;
        private DevExpress.XtraEditors.TextEdit TxtDescription;
        private DevExpress.XtraEditors.CheckEdit ChkInactive;

        private DevExpress.XtraTab.XtraTabControl TabMain;
        private DevExpress.XtraTab.XtraTabPage PageItems;
        private DevExpress.XtraTab.XtraTabPage PageRemark;
        private DevExpress.XtraGrid.GridControl GridItems;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewItems;
        private DevExpress.XtraGrid.Columns.GridColumn ColNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColStock;
        private DevExpress.XtraGrid.Columns.GridColumn ColMachine;
        private DevExpress.XtraGrid.Columns.GridColumn ColBillDay;
        private DevExpress.XtraGrid.Columns.GridColumn ColBK;
        private DevExpress.XtraGrid.Columns.GridColumn ColCL;
        private DevExpress.XtraGrid.Columns.GridColumn ColInact;
        private DevExpress.XtraGrid.Columns.GridColumn ColExpiry;
        private DevExpress.XtraEditors.LabelControl LblRemark1;
        private DevExpress.XtraEditors.TextEdit TxtRemark1;
        private DevExpress.XtraEditors.LabelControl LblRemark2;
        private DevExpress.XtraEditors.TextEdit TxtRemark2;
        private DevExpress.XtraEditors.LabelControl LblNote;
        private DevExpress.XtraEditors.MemoEdit TxtNote;

        private void InitializeComponent()
        {
            this.RibbonCtl = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barSave = new DevExpress.XtraBars.BarButtonItem();
            this.barClose = new DevExpress.XtraBars.BarButtonItem();
            this.barAddItem = new DevExpress.XtraBars.BarButtonItem();
            this.barEditItem = new DevExpress.XtraBars.BarButtonItem();
            this.barDelItem = new DevExpress.XtraBars.BarButtonItem();
            this.ribbonPageHome = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.grpSave = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.grpItem = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.PanelHeaderFields = new DevExpress.XtraEditors.PanelControl();
            this.LblContractNo = new DevExpress.XtraEditors.LabelControl();
            this.TxtContractNo = new DevExpress.XtraEditors.TextEdit();
            this.BtnAutoNo = new DevExpress.XtraEditors.SimpleButton();
            this.LblType = new DevExpress.XtraEditors.LabelControl();
            this.TxtContractType = new DevExpress.XtraEditors.TextEdit();
            this.LblDebtor = new DevExpress.XtraEditors.LabelControl();
            this.LkDebtorCode = new DevExpress.XtraEditors.LookUpEdit();
            this.LblContractDate = new DevExpress.XtraEditors.LabelControl();
            this.DtContractDate = new DevExpress.XtraEditors.DateEdit();
            this.LblStartDate = new DevExpress.XtraEditors.LabelControl();
            this.DtStartDate = new DevExpress.XtraEditors.DateEdit();
            this.LblExpiryDate = new DevExpress.XtraEditors.LabelControl();
            this.DtExpiryDate = new DevExpress.XtraEditors.DateEdit();
            this.LblValue = new DevExpress.XtraEditors.LabelControl();
            this.SpnContractValue = new DevExpress.XtraEditors.SpinEdit();
            this.LblAddress = new DevExpress.XtraEditors.LabelControl();
            this.TxtAddress = new DevExpress.XtraEditors.MemoEdit();
            this.LblAttention = new DevExpress.XtraEditors.LabelControl();
            this.TxtAttention = new DevExpress.XtraEditors.TextEdit();
            this.LblPhone = new DevExpress.XtraEditors.LabelControl();
            this.TxtPhone = new DevExpress.XtraEditors.TextEdit();
            this.LblTerm = new DevExpress.XtraEditors.LabelControl();
            this.TxtTerm = new DevExpress.XtraEditors.TextEdit();
            this.LblArea = new DevExpress.XtraEditors.LabelControl();
            this.TxtArea = new DevExpress.XtraEditors.TextEdit();
            this.LblStaff = new DevExpress.XtraEditors.LabelControl();
            this.TxtStaff = new DevExpress.XtraEditors.TextEdit();
            this.LblBillDay = new DevExpress.XtraEditors.LabelControl();
            this.SpnBillingDay = new DevExpress.XtraEditors.SpinEdit();
            this.LblBillMode = new DevExpress.XtraEditors.LabelControl();
            this.ChkBillGroup = new DevExpress.XtraEditors.CheckEdit();
            this.ChkBillSeparate = new DevExpress.XtraEditors.CheckEdit();
            this.LblDesc = new DevExpress.XtraEditors.LabelControl();
            this.TxtDescription = new DevExpress.XtraEditors.TextEdit();
            this.ChkInactive = new DevExpress.XtraEditors.CheckEdit();
            this.TabMain = new DevExpress.XtraTab.XtraTabControl();
            this.PageItems = new DevExpress.XtraTab.XtraTabPage();
            this.PageRemark = new DevExpress.XtraTab.XtraTabPage();
            this.GridItems = new DevExpress.XtraGrid.GridControl();
            this.GridViewItems = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColStock = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMachine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBillDay = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBK = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCL = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColInact = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColExpiry = new DevExpress.XtraGrid.Columns.GridColumn();
            this.LblRemark1 = new DevExpress.XtraEditors.LabelControl();
            this.TxtRemark1 = new DevExpress.XtraEditors.TextEdit();
            this.LblRemark2 = new DevExpress.XtraEditors.LabelControl();
            this.TxtRemark2 = new DevExpress.XtraEditors.TextEdit();
            this.LblNote = new DevExpress.XtraEditors.LabelControl();
            this.TxtNote = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.RibbonCtl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelHeaderFields)).BeginInit();
            this.PanelHeaderFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtContractNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtContractType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtorCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtContractDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtContractDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStartDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStartDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtExpiryDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtExpiryDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnContractValue.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAddress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAttention.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtPhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtTerm.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtArea.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtStaff.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillingDay.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkBillGroup.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkBillSeparate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDescription.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TabMain)).BeginInit();
            this.TabMain.SuspendLayout();
            this.PageItems.SuspendLayout();
            this.PageRemark.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtNote.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // Ribbon
            //
            this.RibbonCtl.ExpandCollapseItem.Id = 0;
            this.RibbonCtl.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
                this.barSave, this.barClose, this.barAddItem, this.barEditItem, this.barDelItem});
            this.RibbonCtl.Location = new System.Drawing.Point(0, 0);
            this.RibbonCtl.MaxItemId = 6;
            this.RibbonCtl.Name = "Ribbon";
            this.RibbonCtl.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.RibbonCtl.ShowToolbarCustomizeItem = false;
            this.RibbonCtl.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
            this.RibbonCtl.ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.False;
            this.RibbonCtl.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
                this.ribbonPageHome});
            this.RibbonCtl.Size = new System.Drawing.Size(1180, 143);
            //
            // barSave
            //
            this.barSave.Caption = "Save";
            this.barSave.Id = 1;
            this.barSave.ImageOptions.ImageUri.Uri = "Save;Size32x32";
            this.barSave.Name = "barSave";
            this.barSave.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barSave.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barSave_ItemClick);
            //
            // barClose
            //
            this.barClose.Caption = "Close";
            this.barClose.Id = 2;
            this.barClose.ImageOptions.ImageUri.Uri = "Close;Size32x32";
            this.barClose.Name = "barClose";
            this.barClose.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barClose.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barClose_ItemClick);
            //
            // barAddItem
            //
            this.barAddItem.Caption = "Add Service Item";
            this.barAddItem.Id = 3;
            this.barAddItem.ImageOptions.ImageUri.Uri = "Add;Size32x32";
            this.barAddItem.Name = "barAddItem";
            this.barAddItem.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barAddItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barAddItem_ItemClick);
            //
            // barEditItem
            //
            this.barEditItem.Caption = "Edit";
            this.barEditItem.Id = 4;
            this.barEditItem.ImageOptions.ImageUri.Uri = "Edit;Size32x32";
            this.barEditItem.Name = "barEditItem";
            this.barEditItem.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barEditItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barEditItem_ItemClick);
            //
            // barDelItem
            //
            this.barDelItem.Caption = "Delete";
            this.barDelItem.Id = 5;
            this.barDelItem.ImageOptions.ImageUri.Uri = "Delete;Size32x32";
            this.barDelItem.Name = "barDelItem";
            this.barDelItem.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barDelItem.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barDelItem_ItemClick);
            //
            // ribbonPageHome
            //
            this.ribbonPageHome.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
                this.grpSave, this.grpItem});
            this.ribbonPageHome.Name = "ribbonPageHome";
            this.ribbonPageHome.Text = "Home";
            //
            // grpSave
            //
            this.grpSave.ItemLinks.Add(this.barSave);
            this.grpSave.ItemLinks.Add(this.barClose);
            this.grpSave.Name = "grpSave";
            this.grpSave.Text = "Save";
            //
            // grpItem
            //
            this.grpItem.ItemLinks.Add(this.barAddItem);
            this.grpItem.ItemLinks.Add(this.barEditItem);
            this.grpItem.ItemLinks.Add(this.barDelItem);
            this.grpItem.Name = "grpItem";
            this.grpItem.Text = "Service Item";
            //
            // PanelHeaderFields
            //
            this.PanelHeaderFields.Controls.Add(this.LblContractNo);
            this.PanelHeaderFields.Controls.Add(this.TxtContractNo);
            this.PanelHeaderFields.Controls.Add(this.BtnAutoNo);
            this.PanelHeaderFields.Controls.Add(this.LblType);
            this.PanelHeaderFields.Controls.Add(this.TxtContractType);
            this.PanelHeaderFields.Controls.Add(this.LblDebtor);
            this.PanelHeaderFields.Controls.Add(this.LkDebtorCode);
            this.PanelHeaderFields.Controls.Add(this.LblContractDate);
            this.PanelHeaderFields.Controls.Add(this.DtContractDate);
            this.PanelHeaderFields.Controls.Add(this.LblStartDate);
            this.PanelHeaderFields.Controls.Add(this.DtStartDate);
            this.PanelHeaderFields.Controls.Add(this.LblExpiryDate);
            this.PanelHeaderFields.Controls.Add(this.DtExpiryDate);
            this.PanelHeaderFields.Controls.Add(this.LblValue);
            this.PanelHeaderFields.Controls.Add(this.SpnContractValue);
            this.PanelHeaderFields.Controls.Add(this.LblAddress);
            this.PanelHeaderFields.Controls.Add(this.TxtAddress);
            this.PanelHeaderFields.Controls.Add(this.LblAttention);
            this.PanelHeaderFields.Controls.Add(this.TxtAttention);
            this.PanelHeaderFields.Controls.Add(this.LblPhone);
            this.PanelHeaderFields.Controls.Add(this.TxtPhone);
            this.PanelHeaderFields.Controls.Add(this.LblTerm);
            this.PanelHeaderFields.Controls.Add(this.TxtTerm);
            this.PanelHeaderFields.Controls.Add(this.LblArea);
            this.PanelHeaderFields.Controls.Add(this.TxtArea);
            this.PanelHeaderFields.Controls.Add(this.LblStaff);
            this.PanelHeaderFields.Controls.Add(this.TxtStaff);
            this.PanelHeaderFields.Controls.Add(this.LblBillDay);
            this.PanelHeaderFields.Controls.Add(this.SpnBillingDay);
            this.PanelHeaderFields.Controls.Add(this.LblBillMode);
            this.PanelHeaderFields.Controls.Add(this.ChkBillGroup);
            this.PanelHeaderFields.Controls.Add(this.ChkBillSeparate);
            this.PanelHeaderFields.Controls.Add(this.LblDesc);
            this.PanelHeaderFields.Controls.Add(this.TxtDescription);
            this.PanelHeaderFields.Controls.Add(this.ChkInactive);
            this.PanelHeaderFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderFields.Location = new System.Drawing.Point(0, 143);
            this.PanelHeaderFields.Name = "PanelHeaderFields";
            this.PanelHeaderFields.Size = new System.Drawing.Size(1180, 240);
            this.PanelHeaderFields.TabIndex = 1;
            //
            // LblContractNo
            //
            this.LblContractNo.Location = new System.Drawing.Point(12, 13);
            this.LblContractNo.Name = "LblContractNo";
            this.LblContractNo.Size = new System.Drawing.Size(58, 13);
            this.LblContractNo.TabIndex = 0;
            this.LblContractNo.Text = "Contract No";
            //
            // TxtContractNo
            //
            this.TxtContractNo.Location = new System.Drawing.Point(110, 10);
            this.TxtContractNo.Name = "TxtContractNo";
            this.TxtContractNo.Size = new System.Drawing.Size(150, 20);
            this.TxtContractNo.TabIndex = 1;
            //
            // BtnAutoNo
            //
            this.BtnAutoNo.Location = new System.Drawing.Point(264, 9);
            this.BtnAutoNo.Name = "BtnAutoNo";
            this.BtnAutoNo.Size = new System.Drawing.Size(58, 22);
            this.BtnAutoNo.TabIndex = 2;
            this.BtnAutoNo.Text = "Auto No";
            this.BtnAutoNo.Click += new System.EventHandler(this.BtnAutoNo_Click);
            //
            // LblType
            //
            this.LblType.Location = new System.Drawing.Point(12, 37);
            this.LblType.Name = "LblType";
            this.LblType.Size = new System.Drawing.Size(67, 13);
            this.LblType.TabIndex = 3;
            this.LblType.Text = "Contract Type";
            //
            // TxtContractType
            //
            this.TxtContractType.Location = new System.Drawing.Point(110, 34);
            this.TxtContractType.Name = "TxtContractType";
            this.TxtContractType.Size = new System.Drawing.Size(234, 20);
            this.TxtContractType.TabIndex = 4;
            //
            // LblDebtor
            //
            this.LblDebtor.Location = new System.Drawing.Point(12, 61);
            this.LblDebtor.Name = "LblDebtor";
            this.LblDebtor.Size = new System.Drawing.Size(72, 13);
            this.LblDebtor.TabIndex = 5;
            this.LblDebtor.Text = "Customer *";
            //
            // LkDebtorCode
            //
            this.LkDebtorCode.Location = new System.Drawing.Point(110, 58);
            this.LkDebtorCode.Name = "LkDebtorCode";
            this.LkDebtorCode.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.LkDebtorCode.Properties.NullText = "";
            this.LkDebtorCode.Size = new System.Drawing.Size(234, 20);
            this.LkDebtorCode.TabIndex = 6;
            //
            // LblContractDate
            //
            this.LblContractDate.Location = new System.Drawing.Point(12, 85);
            this.LblContractDate.Name = "LblContractDate";
            this.LblContractDate.Size = new System.Drawing.Size(67, 13);
            this.LblContractDate.TabIndex = 7;
            this.LblContractDate.Text = "Contract Date";
            //
            // DtContractDate
            //
            this.DtContractDate.EditValue = null;
            this.DtContractDate.Location = new System.Drawing.Point(110, 82);
            this.DtContractDate.Name = "DtContractDate";
            this.DtContractDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtContractDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtContractDate.Size = new System.Drawing.Size(150, 20);
            this.DtContractDate.TabIndex = 8;
            //
            // LblStartDate
            //
            this.LblStartDate.Location = new System.Drawing.Point(12, 109);
            this.LblStartDate.Name = "LblStartDate";
            this.LblStartDate.Size = new System.Drawing.Size(85, 13);
            this.LblStartDate.TabIndex = 9;
            this.LblStartDate.Text = "Service Start";
            //
            // DtStartDate
            //
            this.DtStartDate.EditValue = null;
            this.DtStartDate.Location = new System.Drawing.Point(110, 106);
            this.DtStartDate.Name = "DtStartDate";
            this.DtStartDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtStartDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtStartDate.Size = new System.Drawing.Size(150, 20);
            this.DtStartDate.TabIndex = 10;
            //
            // LblExpiryDate
            //
            this.LblExpiryDate.Location = new System.Drawing.Point(12, 133);
            this.LblExpiryDate.Name = "LblExpiryDate";
            this.LblExpiryDate.Size = new System.Drawing.Size(85, 13);
            this.LblExpiryDate.TabIndex = 11;
            this.LblExpiryDate.Text = "Service Expiry";
            //
            // DtExpiryDate
            //
            this.DtExpiryDate.EditValue = null;
            this.DtExpiryDate.Location = new System.Drawing.Point(110, 130);
            this.DtExpiryDate.Name = "DtExpiryDate";
            this.DtExpiryDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtExpiryDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtExpiryDate.Size = new System.Drawing.Size(150, 20);
            this.DtExpiryDate.TabIndex = 12;
            //
            // LblValue
            //
            this.LblValue.Location = new System.Drawing.Point(12, 157);
            this.LblValue.Name = "LblValue";
            this.LblValue.Size = new System.Drawing.Size(67, 13);
            this.LblValue.TabIndex = 13;
            this.LblValue.Text = "Contract Value";
            //
            // SpnContractValue
            //
            this.SpnContractValue.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnContractValue.Location = new System.Drawing.Point(110, 154);
            this.SpnContractValue.Name = "SpnContractValue";
            this.SpnContractValue.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SpnContractValue.Size = new System.Drawing.Size(150, 20);
            this.SpnContractValue.TabIndex = 14;
            //
            // LblAddress
            //
            this.LblAddress.Location = new System.Drawing.Point(380, 13);
            this.LblAddress.Name = "LblAddress";
            this.LblAddress.Size = new System.Drawing.Size(39, 13);
            this.LblAddress.TabIndex = 15;
            this.LblAddress.Text = "Address";
            //
            // TxtAddress
            //
            this.TxtAddress.Location = new System.Drawing.Point(452, 10);
            this.TxtAddress.Name = "TxtAddress";
            this.TxtAddress.Size = new System.Drawing.Size(340, 66);
            this.TxtAddress.TabIndex = 16;
            //
            // LblAttention
            //
            this.LblAttention.Location = new System.Drawing.Point(380, 85);
            this.LblAttention.Name = "LblAttention";
            this.LblAttention.Size = new System.Drawing.Size(45, 13);
            this.LblAttention.TabIndex = 17;
            this.LblAttention.Text = "Attention";
            //
            // TxtAttention
            //
            this.TxtAttention.Location = new System.Drawing.Point(452, 82);
            this.TxtAttention.Name = "TxtAttention";
            this.TxtAttention.Size = new System.Drawing.Size(340, 20);
            this.TxtAttention.TabIndex = 18;
            //
            // LblPhone
            //
            this.LblPhone.Location = new System.Drawing.Point(380, 109);
            this.LblPhone.Name = "LblPhone";
            this.LblPhone.Size = new System.Drawing.Size(30, 13);
            this.LblPhone.TabIndex = 19;
            this.LblPhone.Text = "Phone";
            //
            // TxtPhone
            //
            this.TxtPhone.Location = new System.Drawing.Point(452, 106);
            this.TxtPhone.Name = "TxtPhone";
            this.TxtPhone.Size = new System.Drawing.Size(200, 20);
            this.TxtPhone.TabIndex = 20;
            //
            // LblTerm
            //
            this.LblTerm.Location = new System.Drawing.Point(380, 133);
            this.LblTerm.Name = "LblTerm";
            this.LblTerm.Size = new System.Drawing.Size(25, 13);
            this.LblTerm.TabIndex = 21;
            this.LblTerm.Text = "Term";
            //
            // TxtTerm
            //
            this.TxtTerm.Location = new System.Drawing.Point(452, 130);
            this.TxtTerm.Name = "TxtTerm";
            this.TxtTerm.Size = new System.Drawing.Size(160, 20);
            this.TxtTerm.TabIndex = 22;
            //
            // LblArea
            //
            this.LblArea.Location = new System.Drawing.Point(380, 157);
            this.LblArea.Name = "LblArea";
            this.LblArea.Size = new System.Drawing.Size(23, 13);
            this.LblArea.TabIndex = 23;
            this.LblArea.Text = "Area";
            //
            // TxtArea
            //
            this.TxtArea.Location = new System.Drawing.Point(452, 154);
            this.TxtArea.Name = "TxtArea";
            this.TxtArea.Size = new System.Drawing.Size(160, 20);
            this.TxtArea.TabIndex = 24;
            //
            // LblStaff
            //
            this.LblStaff.Location = new System.Drawing.Point(12, 184);
            this.LblStaff.Name = "LblStaff";
            this.LblStaff.Size = new System.Drawing.Size(30, 13);
            this.LblStaff.TabIndex = 25;
            this.LblStaff.Text = "Agent";
            //
            // TxtStaff
            //
            this.TxtStaff.Location = new System.Drawing.Point(110, 181);
            this.TxtStaff.Name = "TxtStaff";
            this.TxtStaff.Size = new System.Drawing.Size(150, 20);
            this.TxtStaff.TabIndex = 26;
            //
            // LblBillDay
            //
            this.LblBillDay.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblBillDay.Location = new System.Drawing.Point(290, 184);
            this.LblBillDay.Name = "LblBillDay";
            this.LblBillDay.Size = new System.Drawing.Size(54, 13);
            this.LblBillDay.TabIndex = 27;
            this.LblBillDay.Text = "Billing Day";
            //
            // SpnBillingDay
            //
            this.SpnBillingDay.EditValue = new decimal(new int[] { 1, 0, 0, 0 });
            this.SpnBillingDay.Location = new System.Drawing.Point(372, 181);
            this.SpnBillingDay.Name = "SpnBillingDay";
            this.SpnBillingDay.Properties.IsFloatValue = false;
            this.SpnBillingDay.Properties.MaxValue = new decimal(new int[] { 31, 0, 0, 0 });
            this.SpnBillingDay.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
            this.SpnBillingDay.Size = new System.Drawing.Size(60, 20);
            this.SpnBillingDay.TabIndex = 28;
            //
            // LblBillMode
            //
            this.LblBillMode.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblBillMode.Location = new System.Drawing.Point(452, 184);
            this.LblBillMode.Name = "LblBillMode";
            this.LblBillMode.Size = new System.Drawing.Size(63, 13);
            this.LblBillMode.TabIndex = 29;
            this.LblBillMode.Text = "Billing Mode";
            //
            // ChkBillGroup
            //
            this.ChkBillGroup.Location = new System.Drawing.Point(524, 181);
            this.ChkBillGroup.Name = "ChkBillGroup";
            this.ChkBillGroup.Properties.Caption = "One invoice (group whole contract)";
            this.ChkBillGroup.Size = new System.Drawing.Size(230, 20);
            this.ChkBillGroup.TabIndex = 30;
            //
            // ChkBillSeparate
            //
            this.ChkBillSeparate.Location = new System.Drawing.Point(758, 181);
            this.ChkBillSeparate.Name = "ChkBillSeparate";
            this.ChkBillSeparate.Properties.Caption = "Separate invoice per service item";
            this.ChkBillSeparate.Size = new System.Drawing.Size(230, 20);
            this.ChkBillSeparate.TabIndex = 31;
            //
            // LblDesc
            //
            this.LblDesc.Location = new System.Drawing.Point(12, 211);
            this.LblDesc.Name = "LblDesc";
            this.LblDesc.Size = new System.Drawing.Size(56, 13);
            this.LblDesc.TabIndex = 32;
            this.LblDesc.Text = "Description";
            //
            // TxtDescription
            //
            this.TxtDescription.Location = new System.Drawing.Point(110, 208);
            this.TxtDescription.Name = "TxtDescription";
            this.TxtDescription.Size = new System.Drawing.Size(520, 20);
            this.TxtDescription.TabIndex = 33;
            //
            // ChkInactive
            //
            this.ChkInactive.Location = new System.Drawing.Point(648, 207);
            this.ChkInactive.Name = "ChkInactive";
            this.ChkInactive.Properties.Caption = "Inactive";
            this.ChkInactive.Size = new System.Drawing.Size(120, 20);
            this.ChkInactive.TabIndex = 34;
            //
            // TabMain
            //
            this.TabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabMain.Location = new System.Drawing.Point(0, 383);
            this.TabMain.Name = "TabMain";
            this.TabMain.SelectedTabPage = this.PageItems;
            this.TabMain.Size = new System.Drawing.Size(1180, 417);
            this.TabMain.TabIndex = 2;
            this.TabMain.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
                this.PageItems, this.PageRemark});
            //
            // PageItems
            //
            this.PageItems.Controls.Add(this.GridItems);
            this.PageItems.Name = "PageItems";
            this.PageItems.Size = new System.Drawing.Size(1174, 389);
            this.PageItems.Text = "Service Items";
            //
            // GridItems
            //
            this.GridItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridItems.Location = new System.Drawing.Point(0, 0);
            this.GridItems.MainView = this.GridViewItems;
            this.GridItems.Name = "GridItems";
            this.GridItems.Size = new System.Drawing.Size(1174, 389);
            this.GridItems.TabIndex = 0;
            this.GridItems.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewItems});
            //
            // GridViewItems
            //
            this.GridViewItems.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColNo, this.ColItemNo, this.ColSerial, this.ColStock, this.ColMachine,
                this.ColBillDay, this.ColBK, this.ColCL, this.ColInact, this.ColExpiry});
            this.GridViewItems.GridControl = this.GridItems;
            this.GridViewItems.Name = "GridViewItems";
            this.GridViewItems.OptionsBehavior.Editable = false;
            this.GridViewItems.OptionsView.ShowGroupPanel = false;
            this.GridViewItems.DoubleClick += new System.EventHandler(this.GridViewItems_DoubleClick);
            this.GridViewItems.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.GridViewItems_RowCellStyle);
            //
            // ColNo
            //
            this.ColNo.Caption = "No";
            this.ColNo.FieldName = "No";
            this.ColNo.Name = "ColNo";
            this.ColNo.Visible = true;
            this.ColNo.VisibleIndex = 0;
            this.ColNo.Width = 40;
            //
            // ColItemNo
            //
            this.ColItemNo.Caption = "Service Item No";
            this.ColItemNo.FieldName = "ServiceItemNo";
            this.ColItemNo.Name = "ColItemNo";
            this.ColItemNo.Visible = true;
            this.ColItemNo.VisibleIndex = 1;
            this.ColItemNo.Width = 120;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial Number";
            this.ColSerial.FieldName = "SerialNumber";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 2;
            this.ColSerial.Width = 130;
            //
            // ColStock
            //
            this.ColStock.Caption = "Provided Items";
            this.ColStock.FieldName = "Items";
            this.ColStock.Name = "ColStock";
            this.ColStock.Visible = true;
            this.ColStock.VisibleIndex = 3;
            this.ColStock.Width = 240;
            //
            // ColMachine
            //
            this.ColMachine.Caption = "Serial No";
            this.ColMachine.FieldName = "SerialNumber";
            this.ColMachine.Name = "ColMachine";
            this.ColMachine.Visible = false;
            this.ColMachine.VisibleIndex = -1;
            this.ColMachine.Width = 120;
            //
            // ColBillDay
            //
            this.ColBillDay.Caption = "Billing Day";
            this.ColBillDay.FieldName = "BillingDay";
            this.ColBillDay.Name = "ColBillDay";
            this.ColBillDay.Visible = true;
            this.ColBillDay.VisibleIndex = 4;
            this.ColBillDay.Width = 90;
            //
            // ColBK
            //
            this.ColBK.Caption = "Black Meter";
            this.ColBK.FieldName = "BKMeter";
            this.ColBK.Name = "ColBK";
            this.ColBK.Visible = true;
            this.ColBK.VisibleIndex = 5;
            this.ColBK.Width = 150;
            //
            // ColCL
            //
            this.ColCL.Caption = "Colour Meter";
            this.ColCL.FieldName = "CLMeter";
            this.ColCL.Name = "ColCL";
            this.ColCL.Visible = true;
            this.ColCL.VisibleIndex = 6;
            this.ColCL.Width = 150;
            //
            // ColInact
            //
            this.ColInact.Caption = "Inactive";
            this.ColInact.FieldName = "Inactive";
            this.ColInact.Name = "ColInact";
            this.ColInact.Visible = true;
            this.ColInact.VisibleIndex = 7;
            this.ColInact.Width = 60;
            //
            // ColExpiry
            //
            this.ColExpiry.Caption = "Expiry";
            this.ColExpiry.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColExpiry.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColExpiry.FieldName = "Expiry";
            this.ColExpiry.Name = "ColExpiry";
            this.ColExpiry.Visible = true;
            this.ColExpiry.VisibleIndex = 8;
            this.ColExpiry.Width = 100;
            //
            // PageRemark
            //
            this.PageRemark.Controls.Add(this.TxtNote);
            this.PageRemark.Controls.Add(this.LblNote);
            this.PageRemark.Controls.Add(this.TxtRemark2);
            this.PageRemark.Controls.Add(this.LblRemark2);
            this.PageRemark.Controls.Add(this.TxtRemark1);
            this.PageRemark.Controls.Add(this.LblRemark1);
            this.PageRemark.Name = "PageRemark";
            this.PageRemark.Size = new System.Drawing.Size(1174, 389);
            this.PageRemark.Text = "Remark / Note";
            //
            // LblRemark1
            //
            this.LblRemark1.Location = new System.Drawing.Point(12, 16);
            this.LblRemark1.Name = "LblRemark1";
            this.LblRemark1.Size = new System.Drawing.Size(48, 13);
            this.LblRemark1.TabIndex = 0;
            this.LblRemark1.Text = "Remark 1";
            //
            // TxtRemark1
            //
            this.TxtRemark1.Location = new System.Drawing.Point(110, 13);
            this.TxtRemark1.Name = "TxtRemark1";
            this.TxtRemark1.Size = new System.Drawing.Size(400, 20);
            this.TxtRemark1.TabIndex = 1;
            //
            // LblRemark2
            //
            this.LblRemark2.Location = new System.Drawing.Point(12, 42);
            this.LblRemark2.Name = "LblRemark2";
            this.LblRemark2.Size = new System.Drawing.Size(48, 13);
            this.LblRemark2.TabIndex = 2;
            this.LblRemark2.Text = "Remark 2";
            //
            // TxtRemark2
            //
            this.TxtRemark2.Location = new System.Drawing.Point(110, 39);
            this.TxtRemark2.Name = "TxtRemark2";
            this.TxtRemark2.Size = new System.Drawing.Size(400, 20);
            this.TxtRemark2.TabIndex = 3;
            //
            // LblNote
            //
            this.LblNote.Location = new System.Drawing.Point(12, 68);
            this.LblNote.Name = "LblNote";
            this.LblNote.Size = new System.Drawing.Size(23, 13);
            this.LblNote.TabIndex = 4;
            this.LblNote.Text = "Note";
            //
            // TxtNote
            //
            this.TxtNote.Location = new System.Drawing.Point(110, 65);
            this.TxtNote.Name = "TxtNote";
            this.TxtNote.Size = new System.Drawing.Size(600, 180);
            this.TxtNote.TabIndex = 5;
            //
            // zSCP2_Contract_Form
            //
            this.ClientSize = new System.Drawing.Size(1180, 800);
            this.Controls.Add(this.TabMain);
            this.Controls.Add(this.PanelHeaderFields);
            this.Controls.Add(this.RibbonCtl);
            this.Name = "zSCP2_Contract_Form";
            this.Ribbon = this.RibbonCtl;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Service Contract";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.RibbonCtl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtContractNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtContractType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LkDebtorCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtContractDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtContractDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStartDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStartDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtExpiryDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtExpiryDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnContractValue.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAddress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAttention.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtPhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtTerm.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtArea.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtStaff.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillingDay.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkBillGroup.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkBillSeparate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDescription.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelHeaderFields)).EndInit();
            this.PanelHeaderFields.ResumeLayout(false);
            this.PanelHeaderFields.PerformLayout();
            this.PageItems.ResumeLayout(false);
            this.PageRemark.ResumeLayout(false);
            this.PageRemark.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtNote.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TabMain)).EndInit();
            this.TabMain.ResumeLayout(false);
            this.PageItems.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
