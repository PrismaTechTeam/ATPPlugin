namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class zSCP2_Item_Form
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

        private DevExpress.XtraEditors.LabelControl LblServiceItemNo;
        private DevExpress.XtraEditors.TextEdit TxtServiceItemNo;
        private DevExpress.XtraEditors.SimpleButton BtnAutoNo;
        private DevExpress.XtraEditors.LabelControl LblSerial;
        private DevExpress.XtraEditors.TextEdit TxtSerial;
        private DevExpress.XtraEditors.LabelControl LblDesc;
        private DevExpress.XtraEditors.TextEdit TxtDescription;
        private DevExpress.XtraEditors.LabelControl LblBillDay;
        private DevExpress.XtraEditors.SpinEdit SpnBillingDayOverride;
        private DevExpress.XtraEditors.LabelControl LblBillDayHint;
        private DevExpress.XtraEditors.LabelControl LblDept;
        private DevExpress.XtraEditors.SearchLookUpEdit SluDept;
        private DevExpress.XtraGrid.Views.Grid.GridView SluDeptView;
        private DevExpress.XtraEditors.LabelControl LblJob;
        private DevExpress.XtraEditors.SearchLookUpEdit SluProject;
        private DevExpress.XtraGrid.Views.Grid.GridView SluProjectView;
        private DevExpress.XtraEditors.LabelControl LblLocation;
        private DevExpress.XtraEditors.TextEdit TxtLocation;
        private DevExpress.XtraEditors.CheckEdit ChkInactive;
        private DevExpress.XtraEditors.GroupControl GrpItemCodes;
        private DevExpress.XtraEditors.SimpleButton BtnAddItemCode;
        private DevExpress.XtraEditors.SimpleButton BtnDelItemCode;
        private DevExpress.XtraGrid.GridControl GridItemCodes;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewItemCodes;
        private DevExpress.XtraGrid.Columns.GridColumn ColIcCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColIcDesc;
        private DevExpress.XtraGrid.Columns.GridColumn ColIcQty;
        private DevExpress.XtraGrid.Columns.GridColumn ColIcSerial;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit RepoItemCode;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox RepoItemSerial;
        private DevExpress.XtraEditors.GroupControl GrpMeters;
        private DevExpress.XtraEditors.SimpleButton BtnAddMeter;
        private DevExpress.XtraEditors.SimpleButton BtnDelMeter;
        private DevExpress.XtraGrid.GridControl GridMeters;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMeters;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtRole;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtMin;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtRate;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtMulti;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtRebate;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtFOC;
        private DevExpress.XtraGrid.Columns.GridColumn ColMtInitial;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit RepoMeterType;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox RepoMeterRole;
        private DevExpress.XtraBars.Ribbon.RibbonControl RibbonCtl;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPageHome;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup grpSave;
        private DevExpress.XtraBars.BarButtonItem barSave;
        private DevExpress.XtraBars.BarButtonItem barClose;
        private DevExpress.XtraBars.BarButtonItem barCopyFrom;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup grpCopy;

        private void InitializeComponent()
        {
            this.LblServiceItemNo = new DevExpress.XtraEditors.LabelControl();
            this.TxtServiceItemNo = new DevExpress.XtraEditors.TextEdit();
            this.BtnAutoNo = new DevExpress.XtraEditors.SimpleButton();
            this.LblSerial = new DevExpress.XtraEditors.LabelControl();
            this.TxtSerial = new DevExpress.XtraEditors.TextEdit();
            this.LblDesc = new DevExpress.XtraEditors.LabelControl();
            this.TxtDescription = new DevExpress.XtraEditors.TextEdit();
            this.LblBillDay = new DevExpress.XtraEditors.LabelControl();
            this.SpnBillingDayOverride = new DevExpress.XtraEditors.SpinEdit();
            this.LblBillDayHint = new DevExpress.XtraEditors.LabelControl();
            this.LblDept = new DevExpress.XtraEditors.LabelControl();
            this.SluDept = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluDeptView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblJob = new DevExpress.XtraEditors.LabelControl();
            this.SluProject = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluProjectView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblLocation = new DevExpress.XtraEditors.LabelControl();
            this.TxtLocation = new DevExpress.XtraEditors.TextEdit();
            this.ChkInactive = new DevExpress.XtraEditors.CheckEdit();
            this.GrpItemCodes = new DevExpress.XtraEditors.GroupControl();
            this.BtnAddItemCode = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelItemCode = new DevExpress.XtraEditors.SimpleButton();
            this.GridItemCodes = new DevExpress.XtraGrid.GridControl();
            this.GridViewItemCodes = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColIcCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIcDesc = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIcQty = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIcSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoItemCode = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            this.RepoItemSerial = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.GrpMeters = new DevExpress.XtraEditors.GroupControl();
            this.BtnAddMeter = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelMeter = new DevExpress.XtraEditors.SimpleButton();
            this.GridMeters = new DevExpress.XtraGrid.GridControl();
            this.GridViewMeters = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMtCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtRole = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtMin = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtMulti = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtRebate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtFOC = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMtInitial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoMeterType = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            this.RepoMeterRole = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.RibbonCtl = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barSave = new DevExpress.XtraBars.BarButtonItem();
            this.barClose = new DevExpress.XtraBars.BarButtonItem();
            this.barCopyFrom = new DevExpress.XtraBars.BarButtonItem();
            this.ribbonPageHome = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.grpSave = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.grpCopy = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            ((System.ComponentModel.ISupportInitialize)(this.RibbonCtl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtServiceItemNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSerial.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDescription.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillingDayOverride.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDept.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDeptView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluProject.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluProjectView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtLocation.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpItemCodes)).BeginInit();
            this.GrpItemCodes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridItemCodes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItemCodes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoItemCode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoItemSerial)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeters)).BeginInit();
            this.GrpMeters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMeterType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMeterRole)).BeginInit();
            this.SuspendLayout();
            //
            // LblServiceItemNo
            //
            this.LblServiceItemNo.Location = new System.Drawing.Point(14, 160);
            this.LblServiceItemNo.Name = "LblServiceItemNo";
            this.LblServiceItemNo.Size = new System.Drawing.Size(76, 13);
            this.LblServiceItemNo.TabIndex = 0;
            this.LblServiceItemNo.Text = "Service Item No";
            //
            // TxtServiceItemNo
            //
            this.TxtServiceItemNo.Location = new System.Drawing.Point(120, 157);
            this.TxtServiceItemNo.Name = "TxtServiceItemNo";
            this.TxtServiceItemNo.Size = new System.Drawing.Size(150, 20);
            this.TxtServiceItemNo.TabIndex = 1;
            //
            // BtnAutoNo
            //
            this.BtnAutoNo.Location = new System.Drawing.Point(274, 156);
            this.BtnAutoNo.Name = "BtnAutoNo";
            this.BtnAutoNo.Size = new System.Drawing.Size(60, 22);
            this.BtnAutoNo.TabIndex = 2;
            this.BtnAutoNo.Text = "Auto";
            this.BtnAutoNo.Click += new System.EventHandler(this.BtnAutoNo_Click);
            //
            // LblSerial
            //
            this.LblSerial.Location = new System.Drawing.Point(14, 186);
            this.LblSerial.Name = "LblSerial";
            this.LblSerial.Size = new System.Drawing.Size(95, 13);
            this.LblSerial.TabIndex = 3;
            this.LblSerial.Text = "Serial Number *";
            //
            // TxtSerial
            //
            this.TxtSerial.Location = new System.Drawing.Point(120, 183);
            this.TxtSerial.Name = "TxtSerial";
            this.TxtSerial.Size = new System.Drawing.Size(214, 20);
            this.TxtSerial.TabIndex = 4;
            //
            // LblDesc
            //
            this.LblDesc.Location = new System.Drawing.Point(14, 212);
            this.LblDesc.Name = "LblDesc";
            this.LblDesc.Size = new System.Drawing.Size(56, 13);
            this.LblDesc.TabIndex = 5;
            this.LblDesc.Text = "Description";
            //
            // TxtDescription
            //
            this.TxtDescription.Location = new System.Drawing.Point(120, 209);
            this.TxtDescription.Name = "TxtDescription";
            this.TxtDescription.Size = new System.Drawing.Size(330, 20);
            this.TxtDescription.TabIndex = 6;
            //
            // LblLocation
            //
            this.LblLocation.Location = new System.Drawing.Point(14, 238);
            this.LblLocation.Name = "LblLocation";
            this.LblLocation.Size = new System.Drawing.Size(72, 13);
            this.LblLocation.TabIndex = 7;
            this.LblLocation.Text = "Stock Location";
            //
            // TxtLocation
            //
            this.TxtLocation.Location = new System.Drawing.Point(120, 235);
            this.TxtLocation.Name = "TxtLocation";
            this.TxtLocation.Size = new System.Drawing.Size(160, 20);
            this.TxtLocation.TabIndex = 8;
            //
            // LblBillDay
            //
            this.LblBillDay.Location = new System.Drawing.Point(470, 160);
            this.LblBillDay.Name = "LblBillDay";
            this.LblBillDay.Size = new System.Drawing.Size(100, 13);
            this.LblBillDay.TabIndex = 9;
            this.LblBillDay.Text = "Billing Day Override";
            //
            // SpnBillingDayOverride
            //
            this.SpnBillingDayOverride.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnBillingDayOverride.Location = new System.Drawing.Point(600, 157);
            this.SpnBillingDayOverride.Name = "SpnBillingDayOverride";
            this.SpnBillingDayOverride.Properties.IsFloatValue = false;
            this.SpnBillingDayOverride.Properties.MaxValue = new decimal(new int[] { 31, 0, 0, 0 });
            this.SpnBillingDayOverride.Properties.MinValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnBillingDayOverride.Size = new System.Drawing.Size(70, 20);
            this.SpnBillingDayOverride.TabIndex = 10;
            //
            // LblBillDayHint
            //
            this.LblBillDayHint.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.LblBillDayHint.Location = new System.Drawing.Point(676, 160);
            this.LblBillDayHint.Name = "LblBillDayHint";
            this.LblBillDayHint.Size = new System.Drawing.Size(200, 13);
            this.LblBillDayHint.TabIndex = 11;
            this.LblBillDayHint.Text = "0 = follow contract day";
            //
            // LblDept
            //
            this.LblDept.Location = new System.Drawing.Point(470, 186);
            this.LblDept.Name = "LblDept";
            this.LblDept.Size = new System.Drawing.Size(56, 13);
            this.LblDept.TabIndex = 12;
            this.LblDept.Text = "Department";
            //
            // SluDept
            //
            this.SluDept.Location = new System.Drawing.Point(600, 183);
            this.SluDept.Name = "SluDept";
            this.SluDept.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo),
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Plus)});
            this.SluDept.Properties.NullText = "";
            this.SluDept.Properties.PopupView = this.SluDeptView;
            this.SluDept.Properties.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.SluDept_ButtonClick);
            this.SluDept.Size = new System.Drawing.Size(160, 20);
            this.SluDept.TabIndex = 13;
            //
            // SluDeptView
            //
            this.SluDeptView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluDeptView.Name = "SluDeptView";
            this.SluDeptView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluDeptView.OptionsView.ShowGroupPanel = false;
            //
            // LblJob
            //
            this.LblJob.Location = new System.Drawing.Point(470, 212);
            this.LblJob.Name = "LblJob";
            this.LblJob.Size = new System.Drawing.Size(18, 13);
            this.LblJob.TabIndex = 14;
            this.LblJob.Text = "Project";
            //
            // SluProject
            //
            this.SluProject.Location = new System.Drawing.Point(600, 209);
            this.SluProject.Name = "SluProject";
            this.SluProject.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo),
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Plus)});
            this.SluProject.Properties.NullText = "";
            this.SluProject.Properties.PopupView = this.SluProjectView;
            this.SluProject.Properties.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.SluProject_ButtonClick);
            this.SluProject.Size = new System.Drawing.Size(160, 20);
            this.SluProject.TabIndex = 15;
            //
            // SluProjectView
            //
            this.SluProjectView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluProjectView.Name = "SluProjectView";
            this.SluProjectView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluProjectView.OptionsView.ShowGroupPanel = false;
            //
            // ChkInactive
            //
            this.ChkInactive.Location = new System.Drawing.Point(600, 234);
            this.ChkInactive.Name = "ChkInactive";
            this.ChkInactive.Properties.Caption = "Inactive";
            this.ChkInactive.Size = new System.Drawing.Size(120, 20);
            this.ChkInactive.TabIndex = 16;
            //
            // GrpItemCodes
            //
            this.GrpItemCodes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.GrpItemCodes.Controls.Add(this.BtnAddItemCode);
            this.GrpItemCodes.Controls.Add(this.BtnDelItemCode);
            this.GrpItemCodes.Controls.Add(this.GridItemCodes);
            this.GrpItemCodes.Location = new System.Drawing.Point(14, 265);
            this.GrpItemCodes.Name = "GrpItemCodes";
            this.GrpItemCodes.Size = new System.Drawing.Size(1122, 184);
            this.GrpItemCodes.TabIndex = 17;
            this.GrpItemCodes.Text = "Provided Items (items supplied to the customer under this service)";
            //
            // BtnAddItemCode
            //
            this.BtnAddItemCode.Location = new System.Drawing.Point(8, 26);
            this.BtnAddItemCode.Name = "BtnAddItemCode";
            this.BtnAddItemCode.Size = new System.Drawing.Size(90, 24);
            this.BtnAddItemCode.TabIndex = 0;
            this.BtnAddItemCode.Text = "+ Add Item";
            this.BtnAddItemCode.Click += new System.EventHandler(this.BtnAddItemCode_Click);
            //
            // BtnDelItemCode
            //
            this.BtnDelItemCode.Location = new System.Drawing.Point(102, 26);
            this.BtnDelItemCode.Name = "BtnDelItemCode";
            this.BtnDelItemCode.Size = new System.Drawing.Size(90, 24);
            this.BtnDelItemCode.TabIndex = 1;
            this.BtnDelItemCode.Text = "Delete Item";
            this.BtnDelItemCode.Click += new System.EventHandler(this.BtnDelItemCode_Click);
            //
            // GridItemCodes
            //
            this.GridItemCodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.GridItemCodes.Location = new System.Drawing.Point(8, 56);
            this.GridItemCodes.MainView = this.GridViewItemCodes;
            this.GridItemCodes.Name = "GridItemCodes";
            this.GridItemCodes.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
                this.RepoItemCode,
                this.RepoItemSerial});
            this.GridItemCodes.Size = new System.Drawing.Size(876, 120);
            this.GridItemCodes.TabIndex = 2;
            this.GridItemCodes.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewItemCodes});
            //
            // GridViewItemCodes
            //
            this.GridViewItemCodes.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColIcCode,
                this.ColIcDesc,
                this.ColIcQty,
                this.ColIcSerial});
            this.GridViewItemCodes.GridControl = this.GridItemCodes;
            this.GridViewItemCodes.Name = "GridViewItemCodes";
            this.GridViewItemCodes.OptionsView.ShowGroupPanel = false;
            this.GridViewItemCodes.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.GridViewItemCodes_CellValueChanged);
            //
            // ColIcCode
            //
            this.ColIcCode.Caption = "Item Code";
            this.ColIcCode.ColumnEdit = this.RepoItemCode;
            this.ColIcCode.FieldName = "ItemCode";
            this.ColIcCode.Name = "ColIcCode";
            this.ColIcCode.Visible = true;
            this.ColIcCode.VisibleIndex = 0;
            this.ColIcCode.Width = 200;
            //
            // ColIcDesc
            //
            this.ColIcDesc.Caption = "Description";
            this.ColIcDesc.FieldName = "Description";
            this.ColIcDesc.Name = "ColIcDesc";
            this.ColIcDesc.Visible = true;
            this.ColIcDesc.VisibleIndex = 1;
            this.ColIcDesc.Width = 380;
            //
            // ColIcQty
            //
            this.ColIcQty.Caption = "Qty";
            this.ColIcQty.DisplayFormat.FormatString = "n2";
            this.ColIcQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColIcQty.FieldName = "Qty";
            this.ColIcQty.Name = "ColIcQty";
            this.ColIcQty.Visible = true;
            this.ColIcQty.VisibleIndex = 2;
            this.ColIcQty.Width = 80;
            //
            // ColIcSerial
            //
            this.ColIcSerial.Caption = "Serial No";
            this.ColIcSerial.ColumnEdit = this.RepoItemSerial;
            this.ColIcSerial.FieldName = "SerialNumber";
            this.ColIcSerial.Name = "ColIcSerial";
            this.ColIcSerial.Visible = true;
            this.ColIcSerial.VisibleIndex = 3;
            this.ColIcSerial.Width = 200;
            //
            // RepoItemCode
            //
            this.RepoItemCode.AutoHeight = false;
            this.RepoItemCode.DisplayMember = "ItemCode";
            this.RepoItemCode.Name = "RepoItemCode";
            this.RepoItemCode.NullText = "";
            this.RepoItemCode.ValueMember = "ItemCode";
            //
            // RepoItemSerial
            //
            this.RepoItemSerial.AutoHeight = false;
            this.RepoItemSerial.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.RepoItemSerial.Name = "RepoItemSerial";
            //
            // GrpMeters
            //
            this.GrpMeters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.GrpMeters.Controls.Add(this.BtnAddMeter);
            this.GrpMeters.Controls.Add(this.BtnDelMeter);
            this.GrpMeters.Controls.Add(this.GridMeters);
            this.GrpMeters.Location = new System.Drawing.Point(14, 455);
            this.GrpMeters.Name = "GrpMeters";
            this.GrpMeters.Size = new System.Drawing.Size(1122, 372);
            this.GrpMeters.TabIndex = 18;
            this.GrpMeters.Text = "Meter Configuration (tag one Black + one Colour)";
            //
            // BtnAddMeter
            //
            this.BtnAddMeter.Location = new System.Drawing.Point(8, 26);
            this.BtnAddMeter.Name = "BtnAddMeter";
            this.BtnAddMeter.Size = new System.Drawing.Size(90, 24);
            this.BtnAddMeter.TabIndex = 0;
            this.BtnAddMeter.Text = "+ Add Meter";
            this.BtnAddMeter.Click += new System.EventHandler(this.BtnAddMeter_Click);
            //
            // BtnDelMeter
            //
            this.BtnDelMeter.Location = new System.Drawing.Point(102, 26);
            this.BtnDelMeter.Name = "BtnDelMeter";
            this.BtnDelMeter.Size = new System.Drawing.Size(90, 24);
            this.BtnDelMeter.TabIndex = 1;
            this.BtnDelMeter.Text = "Delete Meter";
            this.BtnDelMeter.Click += new System.EventHandler(this.BtnDelMeter_Click);
            //
            // GridMeters
            //
            this.GridMeters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.GridMeters.Location = new System.Drawing.Point(8, 56);
            this.GridMeters.MainView = this.GridViewMeters;
            this.GridMeters.Name = "GridMeters";
            this.GridMeters.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
                this.RepoMeterType,
                this.RepoMeterRole});
            this.GridMeters.Size = new System.Drawing.Size(876, 308);
            this.GridMeters.TabIndex = 2;
            this.GridMeters.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridViewMeters});
            //
            // GridViewMeters
            //
            this.GridViewMeters.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColMtCode,
                this.ColMtRole,
                this.ColMtMin,
                this.ColMtRate,
                this.ColMtMulti,
                this.ColMtRebate,
                this.ColMtFOC,
                this.ColMtInitial});
            this.GridViewMeters.GridControl = this.GridMeters;
            this.GridViewMeters.Name = "GridViewMeters";
            this.GridViewMeters.OptionsView.ShowGroupPanel = false;
            this.GridViewMeters.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.GridViewMeters_CellValueChanged);
            //
            // ColMtCode
            //
            this.ColMtCode.Caption = "Meter Type";
            this.ColMtCode.ColumnEdit = this.RepoMeterType;
            this.ColMtCode.FieldName = "MeterTypeCode";
            this.ColMtCode.Name = "ColMtCode";
            this.ColMtCode.Visible = true;
            this.ColMtCode.VisibleIndex = 0;
            this.ColMtCode.Width = 180;
            //
            // ColMtRole
            //
            this.ColMtRole.Caption = "Role (BK/CL)";
            this.ColMtRole.ColumnEdit = this.RepoMeterRole;
            this.ColMtRole.FieldName = "MeterRole";
            this.ColMtRole.Name = "ColMtRole";
            this.ColMtRole.Visible = true;
            this.ColMtRole.VisibleIndex = 1;
            this.ColMtRole.Width = 100;
            //
            // ColMtMin
            //
            this.ColMtMin.Caption = "Min Charges";
            this.ColMtMin.DisplayFormat.FormatString = "n2";
            this.ColMtMin.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMtMin.FieldName = "MinimumCharges";
            this.ColMtMin.Name = "ColMtMin";
            this.ColMtMin.Visible = true;
            this.ColMtMin.VisibleIndex = 2;
            this.ColMtMin.Width = 110;
            //
            // ColMtRate
            //
            this.ColMtRate.Caption = "Rate";
            this.ColMtRate.DisplayFormat.FormatString = "n4";
            this.ColMtRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMtRate.FieldName = "ChargesRate";
            this.ColMtRate.Name = "ColMtRate";
            this.ColMtRate.Visible = true;
            this.ColMtRate.VisibleIndex = 3;
            this.ColMtRate.Width = 110;
            //
            // ColMtMulti
            //
            this.ColMtMulti.Caption = "Multi-Price";
            this.ColMtMulti.FieldName = "MeterMultiPriceCode";
            this.ColMtMulti.Name = "ColMtMulti";
            this.ColMtMulti.Visible = true;
            this.ColMtMulti.VisibleIndex = 4;
            this.ColMtMulti.Width = 120;
            //
            // ColMtRebate
            //
            this.ColMtRebate.Caption = "Rebate %";
            this.ColMtRebate.DisplayFormat.FormatString = "n2";
            this.ColMtRebate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMtRebate.FieldName = "RebateQtyInPercent";
            this.ColMtRebate.Name = "ColMtRebate";
            this.ColMtRebate.Visible = true;
            this.ColMtRebate.VisibleIndex = 5;
            this.ColMtRebate.Width = 90;
            //
            // ColMtFOC
            //
            this.ColMtFOC.Caption = "Free Qty";
            this.ColMtFOC.DisplayFormat.FormatString = "n0";
            this.ColMtFOC.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMtFOC.FieldName = "FOCQty";
            this.ColMtFOC.Name = "ColMtFOC";
            this.ColMtFOC.Visible = true;
            this.ColMtFOC.VisibleIndex = 6;
            this.ColMtFOC.Width = 90;
            //
            // ColMtInitial
            //
            this.ColMtInitial.Caption = "Initial Reading";
            this.ColMtInitial.DisplayFormat.FormatString = "n0";
            this.ColMtInitial.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMtInitial.FieldName = "InitialReading";
            this.ColMtInitial.Name = "ColMtInitial";
            this.ColMtInitial.Visible = true;
            this.ColMtInitial.VisibleIndex = 7;
            this.ColMtInitial.Width = 110;
            //
            // RepoMeterType
            //
            this.RepoMeterType.AutoHeight = false;
            this.RepoMeterType.DisplayMember = "MeterTypeCode";
            this.RepoMeterType.Name = "RepoMeterType";
            this.RepoMeterType.NullText = "";
            this.RepoMeterType.ValueMember = "MeterTypeCode";
            //
            // RepoMeterRole
            //
            this.RepoMeterRole.AutoHeight = false;
            this.RepoMeterRole.Items.AddRange(new object[] {
                "BK",
                "CL",
                "NA"});
            this.RepoMeterRole.Name = "RepoMeterRole";
            this.RepoMeterRole.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            //
            // RibbonCtl
            //
            this.RibbonCtl.ExpandCollapseItem.Id = 0;
            this.RibbonCtl.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
                this.barSave, this.barClose, this.barCopyFrom});
            this.RibbonCtl.Location = new System.Drawing.Point(0, 0);
            this.RibbonCtl.MaxItemId = 4;
            this.RibbonCtl.Name = "RibbonCtl";
            this.RibbonCtl.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.RibbonCtl.ShowToolbarCustomizeItem = false;
            this.RibbonCtl.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
            this.RibbonCtl.ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.False;
            this.RibbonCtl.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
                this.ribbonPageHome});
            this.RibbonCtl.Size = new System.Drawing.Size(1150, 143);
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
            // barCopyFrom
            //
            this.barCopyFrom.Caption = "Copy From...";
            this.barCopyFrom.Id = 3;
            this.barCopyFrom.ImageOptions.ImageUri.Uri = "Copy;Size32x32";
            this.barCopyFrom.Name = "barCopyFrom";
            this.barCopyFrom.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barCopyFrom.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barCopyFrom_ItemClick);
            //
            // ribbonPageHome
            //
            this.ribbonPageHome.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
                this.grpSave, this.grpCopy});
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
            // grpCopy
            //
            this.grpCopy.ItemLinks.Add(this.barCopyFrom);
            this.grpCopy.Name = "grpCopy";
            this.grpCopy.Text = "Copy";
            //
            // zSCP2_Item_Form
            //
            this.ClientSize = new System.Drawing.Size(1150, 879);
            this.Controls.Add(this.GrpMeters);
            this.Controls.Add(this.GrpItemCodes);
            this.Controls.Add(this.ChkInactive);
            this.Controls.Add(this.TxtLocation);
            this.Controls.Add(this.LblLocation);
            this.Controls.Add(this.SluProject);
            this.Controls.Add(this.LblJob);
            this.Controls.Add(this.SluDept);
            this.Controls.Add(this.LblDept);
            this.Controls.Add(this.LblBillDayHint);
            this.Controls.Add(this.SpnBillingDayOverride);
            this.Controls.Add(this.LblBillDay);
            this.Controls.Add(this.TxtDescription);
            this.Controls.Add(this.LblDesc);
            this.Controls.Add(this.TxtSerial);
            this.Controls.Add(this.LblSerial);
            this.Controls.Add(this.BtnAutoNo);
            this.Controls.Add(this.TxtServiceItemNo);
            this.Controls.Add(this.LblServiceItemNo);
            this.Controls.Add(this.RibbonCtl);
            this.MinimumSize = new System.Drawing.Size(1000, 763);
            this.Name = "zSCP2_Item_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Service Item";
            ((System.ComponentModel.ISupportInitialize)(this.TxtServiceItemNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSerial.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDescription.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnBillingDayOverride.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDept.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDeptView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluProject.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluProjectView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtLocation.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridItemCodes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewItemCodes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoItemCode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoItemSerial)).EndInit();
            this.GrpItemCodes.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpItemCodes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMeterType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoMeterRole)).EndInit();
            this.GrpMeters.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RibbonCtl)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
