namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class DocNoFormat_Form
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
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnExit;
        private DevExpress.XtraGrid.GridControl Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView GridView;
        private DevExpress.XtraGrid.Columns.GridColumn ColDocType;
        private DevExpress.XtraGrid.Columns.GridColumn ColDescription;
        private DevExpress.XtraGrid.Columns.GridColumn ColFormatString;
        private DevExpress.XtraGrid.Columns.GridColumn ColNextNumber;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new DevExpress.XtraEditors.PanelControl();
            this.BtnSave = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.BtnExit = new DevExpress.XtraEditors.SimpleButton();
            this.Grid = new DevExpress.XtraGrid.GridControl();
            this.GridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColDocType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColFormatString = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColNextNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).BeginInit();
            this.PanelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Document Numbering Format";
            this.PanelHeaderTop.Hint = "Set the running number format for Service Contract (SC) and Service Item (SI), e.g. SC-<000000>. The Auto buttons draw the next number from here.";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1000, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelToolbar
            //
            this.PanelToolbar.Controls.Add(this.BtnSave);
            this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);
            this.PanelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelToolbar.Location = new System.Drawing.Point(0, 56);
            this.PanelToolbar.Name = "PanelToolbar";
            this.PanelToolbar.Size = new System.Drawing.Size(1000, 62);
            this.PanelToolbar.TabIndex = 1;
            //
            // BtnSave
            //
            this.BtnSave.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnSave.Location = new System.Drawing.Point(8, 6);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(86, 50);
            this.BtnSave.TabIndex = 0;
            this.BtnSave.Text = "Save";
            this.BtnSave.Click += new System.EventHandler(this.OnSave);
            //
            // BtnRefresh
            //
            this.BtnRefresh.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnRefresh.Location = new System.Drawing.Point(98, 6);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(86, 50);
            this.BtnRefresh.TabIndex = 1;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            //
            // BtnExit
            //
            this.BtnExit.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.BtnExit.Location = new System.Drawing.Point(188, 6);
            this.BtnExit.Name = "BtnExit";
            this.BtnExit.Size = new System.Drawing.Size(86, 50);
            this.BtnExit.TabIndex = 2;
            this.BtnExit.Text = "Exit (F2)";
            this.BtnExit.Click += new System.EventHandler(this.OnExit);
            //
            // Grid
            //
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 118);
            this.Grid.MainView = this.GridView;
            this.Grid.Name = "Grid";
            this.Grid.Size = new System.Drawing.Size(1000, 482);
            this.Grid.TabIndex = 2;
            this.Grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridView});
            //
            // GridView
            //
            this.GridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColDocType, this.ColDescription, this.ColFormatString, this.ColNextNumber});
            this.GridView.GridControl = this.Grid;
            this.GridView.Name = "GridView";
            this.GridView.OptionsView.ShowGroupPanel = false;
            this.GridView.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            //
            // ColDocType
            //
            this.ColDocType.Caption = "Doc Type";
            this.ColDocType.FieldName = "DocType";
            this.ColDocType.Name = "ColDocType";
            this.ColDocType.Visible = true;
            this.ColDocType.VisibleIndex = 0;
            this.ColDocType.Width = 90;
            //
            // ColDescription
            //
            this.ColDescription.Caption = "Description";
            this.ColDescription.FieldName = "Description";
            this.ColDescription.Name = "ColDescription";
            this.ColDescription.Visible = true;
            this.ColDescription.VisibleIndex = 1;
            this.ColDescription.Width = 220;
            //
            // ColFormatString
            //
            this.ColFormatString.Caption = "Format String";
            this.ColFormatString.FieldName = "FormatString";
            this.ColFormatString.Name = "ColFormatString";
            this.ColFormatString.Visible = true;
            this.ColFormatString.VisibleIndex = 2;
            this.ColFormatString.Width = 200;
            //
            // ColNextNumber
            //
            this.ColNextNumber.Caption = "Next Number";
            this.ColNextNumber.FieldName = "NextNumber";
            this.ColNextNumber.Name = "ColNextNumber";
            this.ColNextNumber.Visible = true;
            this.ColNextNumber.VisibleIndex = 3;
            this.ColNextNumber.Width = 110;
            //
            // DocNoFormat_Form
            //
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.Name = "DocNoFormat_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Document Numbering Format";
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).EndInit();
            this.PanelToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
