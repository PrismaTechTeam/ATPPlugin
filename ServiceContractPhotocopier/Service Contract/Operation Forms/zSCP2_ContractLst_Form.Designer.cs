namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class zSCP2_ContractLst_Form
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

        protected AutoCount.Controls.PanelHeader PanelHeaderTop;   // protected: the "Maintain Service Item" alias retitles it
        protected DevExpress.XtraEditors.PanelControl PanelToolbar;   // protected: the item alias adds a Copy-to-New button
        private DevExpress.XtraEditors.SimpleButton BtnNew;
        private DevExpress.XtraEditors.SimpleButton BtnEdit;
        private DevExpress.XtraEditors.SimpleButton BtnDelete;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnExit;
        protected DevExpress.XtraGrid.GridControl Grid;                 // protected: the "Maintain Service Item"
        protected DevExpress.XtraGrid.Views.Grid.GridView GridView;    // alias rebuilds the list at item level
        private DevExpress.XtraGrid.Columns.GridColumn ColContractNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColDebtorCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColDebtorName;
        private DevExpress.XtraGrid.Columns.GridColumn ColContractDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColStartDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColExpiryDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColValue;
        private DevExpress.XtraGrid.Columns.GridColumn ColBillingDay;
        private DevExpress.XtraGrid.Columns.GridColumn ColBillingMode;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemCount;
        private DevExpress.XtraGrid.Columns.GridColumn ColInactive;
        private DevExpress.XtraGrid.Columns.GridColumn ColContractType;
        private DevExpress.XtraGrid.Columns.GridColumn ColMonthEnd;
        private DevExpress.XtraGrid.Columns.GridColumn ColAgent;
        private DevExpress.XtraGrid.Columns.GridColumn ColArea;
        private DevExpress.XtraGrid.Columns.GridColumn ColDescription;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new DevExpress.XtraEditors.PanelControl();
            this.BtnNew = new DevExpress.XtraEditors.SimpleButton();
            this.BtnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnExit = new DevExpress.XtraEditors.SimpleButton();
            this.Grid = new DevExpress.XtraGrid.GridControl();
            this.GridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColContractNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDebtorCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDebtorName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColContractDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColStartDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColExpiryDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColValue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBillingDay = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBillingMode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemCount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColInactive = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColContractType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMonthEnd = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColAgent = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColArea = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).BeginInit();
            this.PanelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop  (native AutoCount green header)
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Maintain Service Contract";
            this.PanelHeaderTop.Hint = "In this window, you can create, modify, or delete service contracts and their service items.";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1200, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelToolbar
            //
            this.PanelToolbar.Controls.Add(this.BtnNew);
            this.PanelToolbar.Controls.Add(this.BtnEdit);
            this.PanelToolbar.Controls.Add(this.BtnDelete);
            this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);
            this.PanelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelToolbar.Location = new System.Drawing.Point(0, 56);
            this.PanelToolbar.Name = "PanelToolbar";
            this.PanelToolbar.Size = new System.Drawing.Size(1200, 62);
            this.PanelToolbar.TabIndex = 1;
            //
            // BtnNew
            //
            this.BtnNew.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnNew.Location = new System.Drawing.Point(8, 6);
            this.BtnNew.Name = "BtnNew";
            this.BtnNew.Size = new System.Drawing.Size(86, 50);
            this.BtnNew.TabIndex = 0;
            this.BtnNew.Text = "New";
            this.BtnNew.Click += new System.EventHandler(this.OnNew);
            //
            // BtnEdit
            //
            this.BtnEdit.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnEdit.Location = new System.Drawing.Point(98, 6);
            this.BtnEdit.Name = "BtnEdit";
            this.BtnEdit.Size = new System.Drawing.Size(86, 50);
            this.BtnEdit.TabIndex = 1;
            this.BtnEdit.Text = "Edit";
            this.BtnEdit.Click += new System.EventHandler(this.OnEdit);
            //
            // BtnDelete
            //
            this.BtnDelete.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnDelete.Location = new System.Drawing.Point(188, 6);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(86, 50);
            this.BtnDelete.TabIndex = 2;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.Click += new System.EventHandler(this.OnDelete);
            //
            // BtnRefresh
            //
            this.BtnRefresh.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnRefresh.Location = new System.Drawing.Point(278, 6);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(86, 50);
            this.BtnRefresh.TabIndex = 3;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            //
            // BtnExit
            //
            this.BtnExit.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnExit.Location = new System.Drawing.Point(368, 6);
            this.BtnExit.Name = "BtnExit";
            this.BtnExit.Size = new System.Drawing.Size(86, 50);
            this.BtnExit.TabIndex = 4;
            this.BtnExit.Text = "Exit (F2)";
            this.BtnExit.Click += new System.EventHandler(this.OnExit);
            //
            // Grid
            //
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 118);
            this.Grid.MainView = this.GridView;
            this.Grid.Name = "Grid";
            this.Grid.Size = new System.Drawing.Size(1200, 582);
            this.Grid.TabIndex = 2;
            this.Grid.UseEmbeddedNavigator = true;
            this.Grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridView});
            //
            // GridView
            //
            this.GridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColContractNo,
                this.ColDebtorCode,
                this.ColDebtorName,
                this.ColContractDate,
                this.ColStartDate,
                this.ColExpiryDate,
                this.ColValue,
                this.ColBillingDay,
                this.ColBillingMode,
                this.ColItemCount,
                this.ColInactive,
                this.ColContractType,
                this.ColMonthEnd,
                this.ColAgent,
                this.ColArea,
                this.ColDescription});
            this.GridView.GridControl = this.Grid;
            this.GridView.Name = "GridView";
            this.GridView.OptionsBehavior.Editable = false;
            this.GridView.OptionsFind.AlwaysVisible = true;
            this.GridView.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.GridView.OptionsView.ColumnAutoWidth = false;
            this.GridView.OptionsView.ShowGroupPanel = true;
            //
            // ColContractNo
            //
            this.ColContractNo.Caption = "Contract No.";
            this.ColContractNo.FieldName = "ContractNo";
            this.ColContractNo.Name = "ColContractNo";
            this.ColContractNo.Visible = true;
            this.ColContractNo.VisibleIndex = 0;
            this.ColContractNo.Width = 130;
            //
            // ColDebtorCode
            //
            this.ColDebtorCode.Caption = "Customer Code";
            this.ColDebtorCode.FieldName = "DebtorCode";
            this.ColDebtorCode.Name = "ColDebtorCode";
            this.ColDebtorCode.Visible = true;
            this.ColDebtorCode.VisibleIndex = 2;
            this.ColDebtorCode.Width = 110;
            //
            // ColDebtorName
            //
            this.ColDebtorName.Caption = "Customer Name";
            this.ColDebtorName.FieldName = "DebtorName";
            this.ColDebtorName.Name = "ColDebtorName";
            this.ColDebtorName.Visible = true;
            this.ColDebtorName.VisibleIndex = 3;
            this.ColDebtorName.Width = 240;
            //
            // ColContractDate
            //
            this.ColContractDate.Caption = "Contract Date";
            this.ColContractDate.FieldName = "ContractDate";
            this.ColContractDate.Name = "ColContractDate";
            this.ColContractDate.Visible = true;
            this.ColContractDate.VisibleIndex = 4;
            this.ColContractDate.Width = 100;
            //
            // ColStartDate
            //
            this.ColStartDate.Caption = "Start Date";
            this.ColStartDate.FieldName = "ServiceStartDate";
            this.ColStartDate.Name = "ColStartDate";
            this.ColStartDate.Visible = true;
            this.ColStartDate.VisibleIndex = 5;
            this.ColStartDate.Width = 100;
            //
            // ColExpiryDate
            //
            this.ColExpiryDate.Caption = "Expiry Date";
            this.ColExpiryDate.FieldName = "ServiceExpiryDate";
            this.ColExpiryDate.Name = "ColExpiryDate";
            this.ColExpiryDate.Visible = true;
            this.ColExpiryDate.VisibleIndex = 6;
            this.ColExpiryDate.Width = 100;
            //
            // ColValue
            //
            this.ColValue.Caption = "Contract Value";
            this.ColValue.DisplayFormat.FormatString = "n2";
            this.ColValue.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColValue.FieldName = "ContractValue";
            this.ColValue.Name = "ColValue";
            this.ColValue.Visible = true;
            this.ColValue.VisibleIndex = 7;
            this.ColValue.Width = 110;
            //
            // ColBillingDay
            //
            this.ColBillingDay.Caption = "Billing Day";
            this.ColBillingDay.FieldName = "BillingDay";
            this.ColBillingDay.Name = "ColBillingDay";
            this.ColBillingDay.Visible = true;
            this.ColBillingDay.VisibleIndex = 8;
            this.ColBillingDay.Width = 80;
            //
            // ColBillingMode
            //
            this.ColBillingMode.Caption = "Billing Mode";
            this.ColBillingMode.FieldName = "BillingMode";
            this.ColBillingMode.Name = "ColBillingMode";
            this.ColBillingMode.Visible = true;
            this.ColBillingMode.VisibleIndex = 10;
            this.ColBillingMode.Width = 90;
            //
            // ColItemCount
            //
            this.ColItemCount.Caption = "Service Items";
            this.ColItemCount.FieldName = "ItemCount";
            this.ColItemCount.Name = "ColItemCount";
            this.ColItemCount.Visible = true;
            this.ColItemCount.VisibleIndex = 13;
            this.ColItemCount.Width = 90;
            //
            // ColInactive
            //
            this.ColInactive.Caption = "Inactive";
            this.ColInactive.FieldName = "Inactive";
            this.ColInactive.Name = "ColInactive";
            this.ColInactive.Visible = true;
            this.ColInactive.VisibleIndex = 15;
            this.ColInactive.Width = 60;
            //
            // ColContractType
            //
            this.ColContractType.Caption = "Contract Type";
            this.ColContractType.FieldName = "ContractTypeCode";
            this.ColContractType.Name = "ColContractType";
            this.ColContractType.Visible = true;
            this.ColContractType.VisibleIndex = 1;
            this.ColContractType.Width = 100;
            //
            // ColMonthEnd
            //
            this.ColMonthEnd.Caption = "Month End";
            this.ColMonthEnd.FieldName = "BillOnMonthEnd";
            this.ColMonthEnd.Name = "ColMonthEnd";
            this.ColMonthEnd.Visible = true;
            this.ColMonthEnd.VisibleIndex = 9;
            this.ColMonthEnd.Width = 70;
            //
            // ColAgent
            //
            this.ColAgent.Caption = "Agent";
            this.ColAgent.FieldName = "Agent";
            this.ColAgent.Name = "ColAgent";
            this.ColAgent.Visible = true;
            this.ColAgent.VisibleIndex = 11;
            this.ColAgent.Width = 80;
            //
            // ColArea
            //
            this.ColArea.Caption = "Area";
            this.ColArea.FieldName = "Area";
            this.ColArea.Name = "ColArea";
            this.ColArea.Visible = true;
            this.ColArea.VisibleIndex = 12;
            this.ColArea.Width = 80;
            //
            // ColDescription
            //
            this.ColDescription.Caption = "Description";
            this.ColDescription.FieldName = "Description";
            this.ColDescription.Name = "ColDescription";
            this.ColDescription.Visible = true;
            this.ColDescription.VisibleIndex = 14;
            this.ColDescription.Width = 180;
            //
            // zSCP2_ContractLst_Form
            //
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.Name = "zSCP2_ContractLst_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Maintain Service Contract";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).EndInit();
            this.PanelToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
