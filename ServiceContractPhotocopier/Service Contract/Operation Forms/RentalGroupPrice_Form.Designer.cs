namespace ServiceContractPhotocopier
{
    partial class RentalGroupPrice_Form
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
            this.GridGroups = new DevExpress.XtraGrid.GridControl();
            this.GridViewGroups = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColGroupName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUnits = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColOnMachines = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUnitPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMonthly = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoPrice = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnFromMachines = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClear = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GridGroups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewGroups)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoPrice)).BeginInit();
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
            this.LblHint.Size = new System.Drawing.Size(700, 44);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "";
            //
            // GridGroups
            //
            this.GridGroups.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridGroups.Location = new System.Drawing.Point(12, 59);
            this.GridGroups.MainView = this.GridViewGroups;
            this.GridGroups.Name = "GridGroups";
            this.GridGroups.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoPrice});
            this.GridGroups.Size = new System.Drawing.Size(700, 260);
            this.GridGroups.TabIndex = 1;
            this.GridGroups.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewGroups});
            //
            // GridViewGroups
            //
            this.GridViewGroups.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColGroupName,
            this.ColUnits,
            this.ColOnMachines,
            this.ColUnitPrice,
            this.ColMonthly});
            this.GridViewGroups.GridControl = this.GridGroups;
            this.GridViewGroups.Name = "GridViewGroups";
            this.GridViewGroups.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewGroups.OptionsView.ShowGroupPanel = false;
            this.GridViewGroups.OptionsView.ColumnAutoWidth = true;
            //
            // ColGroupName
            //
            this.ColGroupName.Caption = "Group";
            this.ColGroupName.FieldName = "GroupName";
            this.ColGroupName.Name = "ColGroupName";
            this.ColGroupName.OptionsColumn.AllowEdit = false;
            this.ColGroupName.Visible = true;
            this.ColGroupName.VisibleIndex = 0;
            this.ColGroupName.Width = 230;
            //
            // ColUnits
            //
            this.ColUnits.Caption = "Machines";
            this.ColUnits.FieldName = "Units";
            this.ColUnits.MaxWidth = 90;
            this.ColUnits.Name = "ColUnits";
            this.ColUnits.OptionsColumn.AllowEdit = false;
            this.ColUnits.Visible = true;
            this.ColUnits.VisibleIndex = 1;
            this.ColUnits.Width = 90;
            //
            // ColOnMachines
            //
            this.ColOnMachines.Caption = "On the machines now";
            this.ColOnMachines.FieldName = "OnMachines";
            this.ColOnMachines.Name = "ColOnMachines";
            this.ColOnMachines.OptionsColumn.AllowEdit = false;
            this.ColOnMachines.Visible = true;
            this.ColOnMachines.VisibleIndex = 2;
            this.ColOnMachines.Width = 160;
            //
            // ColUnitPrice
            //
            this.ColUnitPrice.Caption = "Unit Price";
            this.ColUnitPrice.ColumnEdit = this.RepoPrice;
            this.ColUnitPrice.DisplayFormat.FormatString = "n2";
            this.ColUnitPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColUnitPrice.FieldName = "UnitPrice";
            this.ColUnitPrice.Name = "ColUnitPrice";
            this.ColUnitPrice.Visible = true;
            this.ColUnitPrice.VisibleIndex = 3;
            this.ColUnitPrice.Width = 110;
            //
            // ColMonthly
            //
            this.ColMonthly.Caption = "A month";
            this.ColMonthly.DisplayFormat.FormatString = "n2";
            this.ColMonthly.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMonthly.FieldName = "Monthly";
            this.ColMonthly.Name = "ColMonthly";
            this.ColMonthly.OptionsColumn.AllowEdit = false;
            this.ColMonthly.Visible = true;
            this.ColMonthly.VisibleIndex = 4;
            this.ColMonthly.Width = 110;
            //
            // RepoPrice
            //
            this.RepoPrice.AutoHeight = false;
            this.RepoPrice.DisplayFormat.FormatString = "n2";
            this.RepoPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.RepoPrice.EditFormat.FormatString = "n2";
            this.RepoPrice.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.RepoPrice.Mask.EditMask = "n2";
            this.RepoPrice.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoPrice.Mask.UseMaskAsDisplayFormat = true;
            this.RepoPrice.Name = "RepoPrice";
            //
            // LblSummary
            //
            this.LblSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblSummary.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(12, 327);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(700, 18);
            this.LblSummary.TabIndex = 2;
            this.LblSummary.Text = "";
            //
            // PanelBottom
            //
            this.PanelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelBottom.Controls.Add(this.BtnFromMachines);
            this.PanelBottom.Controls.Add(this.BtnClear);
            this.PanelBottom.Controls.Add(this.BtnOK);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Location = new System.Drawing.Point(12, 351);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(700, 44);
            this.PanelBottom.TabIndex = 3;
            //
            // BtnFromMachines
            //
            this.BtnFromMachines.Location = new System.Drawing.Point(9, 9);
            this.BtnFromMachines.Name = "BtnFromMachines";
            this.BtnFromMachines.Size = new System.Drawing.Size(150, 24);
            this.BtnFromMachines.TabIndex = 0;
            this.BtnFromMachines.Text = "Take from machines";
            this.BtnFromMachines.ToolTip = "Fill each group with what its machines already charge, where they agree on one figure";
            this.BtnFromMachines.Click += new System.EventHandler(this.BtnFromMachines_Click);
            //
            // BtnClear
            //
            this.BtnClear.ImageOptions.ImageUri.Uri = "Clear;Size16x16";
            this.BtnClear.Location = new System.Drawing.Point(167, 9);
            this.BtnClear.Name = "BtnClear";
            this.BtnClear.Size = new System.Drawing.Size(150, 24);
            this.BtnClear.TabIndex = 1;
            this.BtnClear.Text = "Clear all prices";
            this.BtnClear.ToolTip = "Back to each machine pricing its own rental";
            this.BtnClear.Click += new System.EventHandler(this.BtnClear_Click);
            //
            // BtnOK
            //
            this.BtnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnOK.ImageOptions.ImageUri.Uri = "Save;Size16x16";
            this.BtnOK.Location = new System.Drawing.Point(514, 9);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(85, 24);
            this.BtnOK.TabIndex = 2;
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(605, 9);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 24);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            //
            // RentalGroupPrice_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(724, 407);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.GridGroups);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 340);
            this.Name = "RentalGroupPrice_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Rental price - one price for the group";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewGroups)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridGroups)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridGroups;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewGroups;
        private DevExpress.XtraGrid.Columns.GridColumn ColGroupName;
        private DevExpress.XtraGrid.Columns.GridColumn ColUnits;
        private DevExpress.XtraGrid.Columns.GridColumn ColOnMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColUnitPrice;
        private DevExpress.XtraGrid.Columns.GridColumn ColMonthly;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoPrice;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnFromMachines;
        private DevExpress.XtraEditors.SimpleButton BtnClear;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
