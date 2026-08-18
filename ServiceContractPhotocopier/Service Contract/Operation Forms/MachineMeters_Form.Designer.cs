namespace ServiceContractPhotocopier
{
    partial class MachineMeters_Form
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
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRental = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBK = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCL = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.RepoState = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.PanelActions = new DevExpress.XtraEditors.PanelControl();
            this.LblGive = new DevExpress.XtraEditors.LabelControl();
            this.BtnAddRental = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAddBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAddCL = new DevExpress.XtraEditors.SimpleButton();
            this.LblTake = new DevExpress.XtraEditors.LabelControl();
            this.BtnDelRental = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelBK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDelCL = new DevExpress.XtraEditors.SimpleButton();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnSelectAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoState)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelActions)).BeginInit();
            this.PanelActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblHint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblHint.Appearance.Options.UseTextOptions = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(12, 9);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(780, 44);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Tick the machines, then press what they need. Rent, black and colour are independent — " +
    "a machine can have all three, one, or none at all, and a machine nobody reads still bills its rent. " +
    "Adding leaves the price at 0; set the rate on the machine, or price a whole rental line in Lines & Price.";
            //
            // GridMachines
            //
            this.GridMachines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridMachines.Location = new System.Drawing.Point(12, 59);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel,
            this.RepoState});
            this.GridMachines.Size = new System.Drawing.Size(780, 330);
            this.GridMachines.TabIndex = 1;
            this.GridMachines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMachines});
            //
            // GridViewMachines
            //
            this.GridViewMachines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColSel,
            this.ColItemNo,
            this.ColSerial,
            this.ColModel,
            this.ColRental,
            this.ColBK,
            this.ColCL});
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
            this.GridViewMachines.OptionsView.ColumnAutoWidth = true;
            //
            // ColSel
            //
            this.ColSel.Caption = "Select";
            this.ColSel.ColumnEdit = this.RepoSel;
            this.ColSel.FieldName = "Sel";
            this.ColSel.MaxWidth = 55;
            this.ColSel.Name = "ColSel";
            this.ColSel.Visible = true;
            this.ColSel.VisibleIndex = 0;
            this.ColSel.Width = 55;
            //
            // ColItemNo
            //
            this.ColItemNo.Caption = "Service Item No";
            this.ColItemNo.FieldName = "ServiceItemNo";
            this.ColItemNo.Name = "ColItemNo";
            this.ColItemNo.OptionsColumn.AllowEdit = false;
            this.ColItemNo.Visible = true;
            this.ColItemNo.VisibleIndex = 1;
            this.ColItemNo.Width = 140;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial No";
            this.ColSerial.FieldName = "SerialNumber";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 2;
            this.ColSerial.Width = 130;
            //
            // ColModel
            //
            this.ColModel.Caption = "Model";
            this.ColModel.FieldName = "ItemCode";
            this.ColModel.Name = "ColModel";
            this.ColModel.OptionsColumn.AllowEdit = false;
            this.ColModel.Visible = true;
            this.ColModel.VisibleIndex = 3;
            this.ColModel.Width = 190;
            //
            // ColRental
            //
            this.ColRental.Caption = "Rental";
            this.ColRental.ColumnEdit = this.RepoState;
            this.ColRental.FieldName = "HasRental";
            this.ColRental.MaxWidth = 70;
            this.ColRental.Name = "ColRental";
            this.ColRental.Visible = true;
            this.ColRental.VisibleIndex = 4;
            this.ColRental.Width = 70;
            //
            // ColBK
            //
            this.ColBK.Caption = "BK";
            this.ColBK.ColumnEdit = this.RepoState;
            this.ColBK.FieldName = "HasBK";
            this.ColBK.MaxWidth = 55;
            this.ColBK.Name = "ColBK";
            this.ColBK.Visible = true;
            this.ColBK.VisibleIndex = 5;
            this.ColBK.Width = 55;
            //
            // ColCL
            //
            this.ColCL.Caption = "CL";
            this.ColCL.ColumnEdit = this.RepoState;
            this.ColCL.FieldName = "HasCL";
            this.ColCL.MaxWidth = 55;
            this.ColCL.Name = "ColCL";
            this.ColCL.Visible = true;
            this.ColCL.VisibleIndex = 6;
            this.ColCL.Width = 55;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // RepoState
            //
            this.RepoState.AutoHeight = false;
            this.RepoState.Name = "RepoState";
            //
            // PanelActions
            //
            this.PanelActions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelActions.Controls.Add(this.LblGive);
            this.PanelActions.Controls.Add(this.BtnAddRental);
            this.PanelActions.Controls.Add(this.BtnAddBK);
            this.PanelActions.Controls.Add(this.BtnAddCL);
            this.PanelActions.Controls.Add(this.LblTake);
            this.PanelActions.Controls.Add(this.BtnDelRental);
            this.PanelActions.Controls.Add(this.BtnDelBK);
            this.PanelActions.Controls.Add(this.BtnDelCL);
            this.PanelActions.Location = new System.Drawing.Point(12, 395);
            this.PanelActions.Name = "PanelActions";
            this.PanelActions.Size = new System.Drawing.Size(780, 76);
            this.PanelActions.TabIndex = 2;
            //
            // LblGive
            //
            this.LblGive.Location = new System.Drawing.Point(12, 12);
            this.LblGive.Name = "LblGive";
            this.LblGive.Size = new System.Drawing.Size(110, 13);
            this.LblGive.TabIndex = 0;
            this.LblGive.Text = "Ticked machines get:";
            //
            // BtnAddRental
            //
            this.BtnAddRental.Location = new System.Drawing.Point(150, 7);
            this.BtnAddRental.Name = "BtnAddRental";
            this.BtnAddRental.Size = new System.Drawing.Size(110, 24);
            this.BtnAddRental.TabIndex = 1;
            this.BtnAddRental.Text = "+ Rental";
            this.BtnAddRental.Click += new System.EventHandler(this.BtnAddRental_Click);
            //
            // BtnAddBK
            //
            this.BtnAddBK.Location = new System.Drawing.Point(268, 7);
            this.BtnAddBK.Name = "BtnAddBK";
            this.BtnAddBK.Size = new System.Drawing.Size(110, 24);
            this.BtnAddBK.TabIndex = 2;
            this.BtnAddBK.Text = "+ Black (BK)";
            this.BtnAddBK.Click += new System.EventHandler(this.BtnAddBK_Click);
            //
            // BtnAddCL
            //
            this.BtnAddCL.Location = new System.Drawing.Point(386, 7);
            this.BtnAddCL.Name = "BtnAddCL";
            this.BtnAddCL.Size = new System.Drawing.Size(110, 24);
            this.BtnAddCL.TabIndex = 3;
            this.BtnAddCL.Text = "+ Colour (CL)";
            this.BtnAddCL.Click += new System.EventHandler(this.BtnAddCL_Click);
            //
            // LblTake
            //
            this.LblTake.Location = new System.Drawing.Point(12, 46);
            this.LblTake.Name = "LblTake";
            this.LblTake.Size = new System.Drawing.Size(110, 13);
            this.LblTake.TabIndex = 4;
            this.LblTake.Text = "...or lose:";
            //
            // BtnDelRental
            //
            this.BtnDelRental.Location = new System.Drawing.Point(150, 41);
            this.BtnDelRental.Name = "BtnDelRental";
            this.BtnDelRental.Size = new System.Drawing.Size(110, 24);
            this.BtnDelRental.TabIndex = 5;
            this.BtnDelRental.Text = "- Rental";
            this.BtnDelRental.Click += new System.EventHandler(this.BtnDelRental_Click);
            //
            // BtnDelBK
            //
            this.BtnDelBK.Location = new System.Drawing.Point(268, 41);
            this.BtnDelBK.Name = "BtnDelBK";
            this.BtnDelBK.Size = new System.Drawing.Size(110, 24);
            this.BtnDelBK.TabIndex = 6;
            this.BtnDelBK.Text = "- Black (BK)";
            this.BtnDelBK.Click += new System.EventHandler(this.BtnDelBK_Click);
            //
            // BtnDelCL
            //
            this.BtnDelCL.Location = new System.Drawing.Point(386, 41);
            this.BtnDelCL.Name = "BtnDelCL";
            this.BtnDelCL.Size = new System.Drawing.Size(110, 24);
            this.BtnDelCL.TabIndex = 7;
            this.BtnDelCL.Text = "- Colour (CL)";
            this.BtnDelCL.Click += new System.EventHandler(this.BtnDelCL_Click);
            //
            // LblSummary
            //
            this.LblSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblSummary.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(12, 479);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(780, 18);
            this.LblSummary.TabIndex = 3;
            this.LblSummary.Text = "";
            //
            // PanelBottom
            //
            this.PanelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelBottom.Controls.Add(this.BtnSelectAll);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Location = new System.Drawing.Point(12, 503);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(780, 44);
            this.PanelBottom.TabIndex = 4;
            //
            // BtnSelectAll
            //
            this.BtnSelectAll.Location = new System.Drawing.Point(9, 9);
            this.BtnSelectAll.Name = "BtnSelectAll";
            this.BtnSelectAll.Size = new System.Drawing.Size(130, 24);
            this.BtnSelectAll.TabIndex = 0;
            this.BtnSelectAll.Text = "Select all / none";
            this.BtnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.ImageOptions.ImageUri.Uri = "Save;Size16x16";
            this.BtnOK.Location = new System.Drawing.Point(594, 9);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 24);
            this.BtnOK.TabIndex = 1;
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(685, 9);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 24);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "Cancel";
            //
            // MachineMeters_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(804, 559);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.GridMachines);
            this.Controls.Add(this.PanelActions);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 500);
            this.Name = "MachineMeters_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meters — which machines have rent, black and colour";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoState)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColModel;
        private DevExpress.XtraGrid.Columns.GridColumn ColRental;
        private DevExpress.XtraGrid.Columns.GridColumn ColBK;
        private DevExpress.XtraGrid.Columns.GridColumn ColCL;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoState;
        private DevExpress.XtraEditors.PanelControl PanelActions;
        private DevExpress.XtraEditors.LabelControl LblGive;
        private DevExpress.XtraEditors.SimpleButton BtnAddRental;
        private DevExpress.XtraEditors.SimpleButton BtnAddBK;
        private DevExpress.XtraEditors.SimpleButton BtnAddCL;
        private DevExpress.XtraEditors.LabelControl LblTake;
        private DevExpress.XtraEditors.SimpleButton BtnDelRental;
        private DevExpress.XtraEditors.SimpleButton BtnDelBK;
        private DevExpress.XtraEditors.SimpleButton BtnDelCL;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnSelectAll;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
