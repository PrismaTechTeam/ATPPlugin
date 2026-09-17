namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class PickDatabase_Form
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
        private DevExpress.XtraEditors.PanelControl PanelButtons;
        private DevExpress.XtraEditors.SimpleButton BtnSelect;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl Grid;
        private DevExpress.XtraGrid.Views.Grid.GridView GridView;
        private DevExpress.XtraGrid.Columns.GridColumn ColDatabase;
        private DevExpress.XtraGrid.Columns.GridColumn ColCompany;
        private DevExpress.XtraGrid.Columns.GridColumn ColPlugIn;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelButtons = new DevExpress.XtraEditors.PanelControl();
            this.BtnSelect = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.Grid = new DevExpress.XtraGrid.GridControl();
            this.GridView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColDatabase = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCompany = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPlugIn = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.PanelButtons)).BeginInit();
            this.PanelButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Choose a database";
            this.PanelHeaderTop.Hint = "Every database on that server. The ones marked as an account book are the ones this can read from; a book with the plug-in installed is one that can be linked to.";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(720, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // Grid
            //
            this.Grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Grid.Location = new System.Drawing.Point(0, 56);
            this.Grid.MainView = this.GridView;
            this.Grid.Name = "Grid";
            this.Grid.Size = new System.Drawing.Size(720, 388);
            this.Grid.TabIndex = 1;
            this.Grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
                this.GridView});
            this.Grid.DoubleClick += new System.EventHandler(this.OnGridDoubleClick);
            //
            // GridView
            //
            this.GridView.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                this.ColDatabase, this.ColCompany, this.ColPlugIn});
            this.GridView.GridControl = this.Grid;
            this.GridView.Name = "GridView";
            this.GridView.OptionsBehavior.Editable = false;
            this.GridView.OptionsView.ShowGroupPanel = false;
            this.GridView.OptionsView.ShowAutoFilterRow = true;
            //
            // ColDatabase
            //
            this.ColDatabase.Caption = "Database";
            this.ColDatabase.FieldName = "DatabaseName";
            this.ColDatabase.Name = "ColDatabase";
            this.ColDatabase.Visible = true;
            this.ColDatabase.VisibleIndex = 0;
            this.ColDatabase.Width = 230;
            //
            // ColCompany
            //
            this.ColCompany.Caption = "Company";
            this.ColCompany.FieldName = "CompanyName";
            this.ColCompany.Name = "ColCompany";
            this.ColCompany.Visible = true;
            this.ColCompany.VisibleIndex = 1;
            this.ColCompany.Width = 330;
            //
            // ColPlugIn
            //
            this.ColPlugIn.Caption = "Service Contract";
            this.ColPlugIn.FieldName = "PlugIn";
            this.ColPlugIn.Name = "ColPlugIn";
            this.ColPlugIn.Visible = true;
            this.ColPlugIn.VisibleIndex = 2;
            this.ColPlugIn.Width = 140;
            //
            // PanelButtons
            //
            this.PanelButtons.Controls.Add(this.LblHint);
            this.PanelButtons.Controls.Add(this.BtnSelect);
            this.PanelButtons.Controls.Add(this.BtnCancel);
            this.PanelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelButtons.Location = new System.Drawing.Point(0, 444);
            this.PanelButtons.Name = "PanelButtons";
            this.PanelButtons.Size = new System.Drawing.Size(720, 46);
            this.PanelButtons.TabIndex = 2;
            //
            // LblHint
            //
            this.LblHint.Location = new System.Drawing.Point(12, 16);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(280, 13);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Double-click a row to choose it.";
            //
            // BtnSelect
            //
            this.BtnSelect.Location = new System.Drawing.Point(514, 10);
            this.BtnSelect.Name = "BtnSelect";
            this.BtnSelect.Size = new System.Drawing.Size(90, 26);
            this.BtnSelect.TabIndex = 1;
            this.BtnSelect.Text = "Select";
            this.BtnSelect.Click += new System.EventHandler(this.OnSelect);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(612, 10);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 26);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.OnCancel);
            //
            // PickDatabase_Form
            //
            this.ClientSize = new System.Drawing.Size(720, 490);
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.PanelButtons);
            this.Controls.Add(this.PanelHeaderTop);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Name = "PickDatabase_Form";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Choose a database";
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Grid)).EndInit();
            this.PanelButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelButtons)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
