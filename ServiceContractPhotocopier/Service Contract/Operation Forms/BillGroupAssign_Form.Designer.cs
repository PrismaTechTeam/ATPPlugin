namespace ServiceContractPhotocopier
{
    partial class BillGroupAssign_Form
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
            this.ColItemCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDesc = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColGroup = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.LblGroup = new DevExpress.XtraEditors.LabelControl();
            this.CmbGroup = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnSelectAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAssign = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClear = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CmbGroup.Properties)).BeginInit();
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
            this.LblHint.Size = new System.Drawing.Size(740, 30);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Machines with the SAME group name are billed together on ONE invoice at Generate;" +
    " each group gets its own invoice. Machines with no group bill the normal way. Tick machines, type" +
    " a group name, press Assign.";
            //
            // GridMachines
            //
            this.GridMachines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridMachines.Location = new System.Drawing.Point(12, 45);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel});
            this.GridMachines.Size = new System.Drawing.Size(740, 380);
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
            this.ColItemCode,
            this.ColDesc,
            this.ColGroup});
            this.GridViewMachines.GridControl = this.GridMachines;
            this.GridViewMachines.Name = "GridViewMachines";
            this.GridViewMachines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewMachines.OptionsView.ShowGroupPanel = false;
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
            this.ColItemNo.Width = 130;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Machine Serial";
            this.ColSerial.FieldName = "SerialNumber";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 2;
            this.ColSerial.Width = 110;
            //
            // ColItemCode
            //
            this.ColItemCode.Caption = "Item Code";
            this.ColItemCode.FieldName = "ItemCode";
            this.ColItemCode.Name = "ColItemCode";
            this.ColItemCode.OptionsColumn.AllowEdit = false;
            this.ColItemCode.Visible = true;
            this.ColItemCode.VisibleIndex = 3;
            this.ColItemCode.Width = 110;
            //
            // ColDesc
            //
            this.ColDesc.Caption = "Description";
            this.ColDesc.FieldName = "Description";
            this.ColDesc.Name = "ColDesc";
            this.ColDesc.OptionsColumn.AllowEdit = false;
            this.ColDesc.Visible = true;
            this.ColDesc.VisibleIndex = 4;
            this.ColDesc.Width = 230;
            //
            // ColGroup
            //
            this.ColGroup.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.ColGroup.AppearanceCell.Options.UseFont = true;
            this.ColGroup.Caption = "Bill Group";
            this.ColGroup.FieldName = "BillGroup";
            this.ColGroup.Name = "ColGroup";
            this.ColGroup.OptionsColumn.AllowEdit = false;
            this.ColGroup.Visible = true;
            this.ColGroup.VisibleIndex = 5;
            this.ColGroup.Width = 90;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // LblSummary
            //
            this.LblSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblSummary.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblSummary.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(0)))));
            this.LblSummary.Appearance.Options.UseFont = true;
            this.LblSummary.Appearance.Options.UseForeColor = true;
            this.LblSummary.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblSummary.Appearance.Options.UseTextOptions = true;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(12, 431);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(740, 28);
            this.LblSummary.TabIndex = 2;
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.LblGroup);
            this.PanelBottom.Controls.Add(this.CmbGroup);
            this.PanelBottom.Controls.Add(this.BtnSelectAll);
            this.PanelBottom.Controls.Add(this.BtnAssign);
            this.PanelBottom.Controls.Add(this.BtnClear);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 465);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(764, 44);
            this.PanelBottom.TabIndex = 3;
            //
            // LblGroup
            //
            this.LblGroup.Location = new System.Drawing.Point(12, 15);
            this.LblGroup.Name = "LblGroup";
            this.LblGroup.Size = new System.Drawing.Size(53, 13);
            this.LblGroup.TabIndex = 0;
            this.LblGroup.Text = "Bill Group:";
            //
            // CmbGroup
            //
            this.CmbGroup.Location = new System.Drawing.Point(71, 11);
            this.CmbGroup.Name = "CmbGroup";
            this.CmbGroup.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            this.CmbGroup.Size = new System.Drawing.Size(130, 20);
            this.CmbGroup.TabIndex = 1;
            //
            // BtnSelectAll
            //
            this.BtnSelectAll.Location = new System.Drawing.Point(213, 9);
            this.BtnSelectAll.Name = "BtnSelectAll";
            this.BtnSelectAll.Size = new System.Drawing.Size(75, 24);
            this.BtnSelectAll.TabIndex = 2;
            this.BtnSelectAll.Text = "Tick All";
            this.BtnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            //
            // BtnAssign
            //
            this.BtnAssign.ImageOptions.ImageUri.Uri = "Apply;Size16x16";
            this.BtnAssign.Location = new System.Drawing.Point(294, 9);
            this.BtnAssign.Name = "BtnAssign";
            this.BtnAssign.Size = new System.Drawing.Size(125, 24);
            this.BtnAssign.TabIndex = 3;
            this.BtnAssign.Text = "Assign to Ticked";
            this.BtnAssign.Click += new System.EventHandler(this.BtnAssign_Click);
            //
            // BtnClear
            //
            this.BtnClear.ImageOptions.ImageUri.Uri = "Clear;Size16x16";
            this.BtnClear.Location = new System.Drawing.Point(425, 9);
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.Size = new System.Drawing.Size(105, 24);
            this.BtnClear.TabIndex = 4;
            this.BtnClear.Text = "Clear Ticked";
            this.BtnClear.Click += new System.EventHandler(this.BtnClear_Click);
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.ImageOptions.ImageUri.Uri = "Save;Size16x16";
            this.BtnOK.Location = new System.Drawing.Point(578, 9);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 24);
            this.BtnOK.TabIndex = 5;
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(669, 9);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 24);
            this.BtnCancel.TabIndex = 6;
            this.BtnCancel.Text = "Cancel";
            //
            // BillGroupAssign_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(764, 509);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.GridMachines);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 420);
            this.Name = "BillGroupAssign_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bill Group — split this contract into multiple invoices";
            this.Load += new System.EventHandler(this.OnFormLoad);
            ((System.ComponentModel.ISupportInitialize)(this.CmbGroup.Properties)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            this.PanelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
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
        private DevExpress.XtraGrid.Columns.GridColumn ColItemCode;
        private DevExpress.XtraGrid.Columns.GridColumn ColDesc;
        private DevExpress.XtraGrid.Columns.GridColumn ColGroup;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.LabelControl LblGroup;
        private DevExpress.XtraEditors.ComboBoxEdit CmbGroup;
        private DevExpress.XtraEditors.SimpleButton BtnSelectAll;
        private DevExpress.XtraEditors.SimpleButton BtnAssign;
        private DevExpress.XtraEditors.SimpleButton BtnClear;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
