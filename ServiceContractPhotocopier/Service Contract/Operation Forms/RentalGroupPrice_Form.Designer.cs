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
            this.LblMachines = new DevExpress.XtraEditors.LabelControl();
            this.GridMachines = new DevExpress.XtraGrid.GridControl();
            this.GridViewMachines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColSel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColItemNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMergeGroup = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPrintsOn = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColOwnRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.PanelGroupBtns = new DevExpress.XtraEditors.PanelControl();
            this.BtnMerge = new DevExpress.XtraEditors.SimpleButton();
            this.BtnUngroup = new DevExpress.XtraEditors.SimpleButton();
            this.LblLines = new DevExpress.XtraEditors.LabelControl();
            this.GridLines = new DevExpress.XtraGrid.GridControl();
            this.GridViewLines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColLineName = new DevExpress.XtraGrid.Columns.GridColumn();
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
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelGroupBtns)).BeginInit();
            this.PanelGroupBtns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).BeginInit();
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
            this.LblHint.Size = new System.Drawing.Size(836, 58);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "";
            //
            // LblMachines
            //
            this.LblMachines.Location = new System.Drawing.Point(12, 75);
            this.LblMachines.Name = "LblMachines";
            this.LblMachines.Size = new System.Drawing.Size(400, 13);
            this.LblMachines.TabIndex = 1;
            this.LblMachines.Text = "Machines — tick the ones that belong on one line";
            //
            // GridMachines
            //
            this.GridMachines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridMachines.Location = new System.Drawing.Point(12, 94);
            this.GridMachines.MainView = this.GridViewMachines;
            this.GridMachines.Name = "GridMachines";
            this.GridMachines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel});
            this.GridMachines.Size = new System.Drawing.Size(836, 230);
            this.GridMachines.TabIndex = 2;
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
            this.ColMergeGroup,
            this.ColPrintsOn,
            this.ColOwnRate});
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
            this.ColItemNo.Width = 130;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial No";
            this.ColSerial.FieldName = "SerialNumber";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 2;
            this.ColSerial.Width = 120;
            //
            // ColModel
            //
            this.ColModel.Caption = "Model";
            this.ColModel.FieldName = "ItemCode";
            this.ColModel.Name = "ColModel";
            this.ColModel.OptionsColumn.AllowEdit = false;
            this.ColModel.Visible = true;
            this.ColModel.VisibleIndex = 3;
            this.ColModel.Width = 160;
            //
            // ColMergeGroup
            //
            this.ColMergeGroup.Caption = "Group";
            this.ColMergeGroup.FieldName = "MergeGroup";
            this.ColMergeGroup.Name = "ColMergeGroup";
            this.ColMergeGroup.OptionsColumn.AllowEdit = false;
            this.ColMergeGroup.Visible = true;
            this.ColMergeGroup.VisibleIndex = 4;
            this.ColMergeGroup.Width = 120;
            //
            // ColPrintsOn
            //
            this.ColPrintsOn.Caption = "Prints on";
            this.ColPrintsOn.FieldName = "PrintsOn";
            this.ColPrintsOn.Name = "ColPrintsOn";
            this.ColPrintsOn.OptionsColumn.AllowEdit = false;
            this.ColPrintsOn.Visible = true;
            this.ColPrintsOn.VisibleIndex = 5;
            this.ColPrintsOn.Width = 160;
            //
            // ColOwnRate
            //
            this.ColOwnRate.Caption = "Own rental";
            this.ColOwnRate.DisplayFormat.FormatString = "n2";
            this.ColOwnRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColOwnRate.FieldName = "OwnRate";
            this.ColOwnRate.Name = "ColOwnRate";
            this.ColOwnRate.OptionsColumn.AllowEdit = false;
            this.ColOwnRate.Visible = true;
            this.ColOwnRate.VisibleIndex = 6;
            this.ColOwnRate.Width = 100;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // PanelGroupBtns
            //
            this.PanelGroupBtns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelGroupBtns.Controls.Add(this.BtnMerge);
            this.PanelGroupBtns.Controls.Add(this.BtnUngroup);
            this.PanelGroupBtns.Location = new System.Drawing.Point(12, 330);
            this.PanelGroupBtns.Name = "PanelGroupBtns";
            this.PanelGroupBtns.Size = new System.Drawing.Size(836, 40);
            this.PanelGroupBtns.TabIndex = 3;
            //
            // BtnMerge
            //
            this.BtnMerge.Location = new System.Drawing.Point(9, 7);
            this.BtnMerge.Name = "BtnMerge";
            this.BtnMerge.Size = new System.Drawing.Size(210, 26);
            this.BtnMerge.TabIndex = 0;
            this.BtnMerge.Text = "Merge ticked into one line...";
            this.BtnMerge.ToolTip = "Put the ticked machines on the SAME printed line, whatever their models";
            this.BtnMerge.Click += new System.EventHandler(this.BtnMerge_Click);
            //
            // BtnUngroup
            //
            this.BtnUngroup.Location = new System.Drawing.Point(227, 7);
            this.BtnUngroup.Name = "BtnUngroup";
            this.BtnUngroup.Size = new System.Drawing.Size(160, 26);
            this.BtnUngroup.TabIndex = 1;
            this.BtnUngroup.Text = "Ungroup ticked";
            this.BtnUngroup.ToolTip = "Back to whatever the contract's line mode would do on its own";
            this.BtnUngroup.Click += new System.EventHandler(this.BtnUngroup_Click);
            //
            // LblLines
            //
            this.LblLines.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LblLines.Location = new System.Drawing.Point(12, 380);
            this.LblLines.Name = "LblLines";
            this.LblLines.Size = new System.Drawing.Size(500, 13);
            this.LblLines.TabIndex = 4;
            this.LblLines.Text = "Lines — what comes out, and what each rental line costs a month per machine";
            //
            // GridLines
            //
            this.GridLines.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridLines.Location = new System.Drawing.Point(12, 399);
            this.GridLines.MainView = this.GridViewLines;
            this.GridLines.Name = "GridLines";
            this.GridLines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoPrice});
            this.GridLines.Size = new System.Drawing.Size(836, 160);
            this.GridLines.TabIndex = 5;
            this.GridLines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewLines});
            //
            // GridViewLines
            //
            this.GridViewLines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColLineName,
            this.ColUnits,
            this.ColOnMachines,
            this.ColUnitPrice,
            this.ColMonthly});
            this.GridViewLines.GridControl = this.GridLines;
            this.GridViewLines.Name = "GridViewLines";
            this.GridViewLines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewLines.OptionsView.ShowGroupPanel = false;
            this.GridViewLines.OptionsView.ColumnAutoWidth = true;
            //
            // ColLineName
            //
            this.ColLineName.Caption = "Line";
            this.ColLineName.FieldName = "LineName";
            this.ColLineName.Name = "ColLineName";
            this.ColLineName.OptionsColumn.AllowEdit = false;
            this.ColLineName.Visible = true;
            this.ColLineName.VisibleIndex = 0;
            this.ColLineName.Width = 260;
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
            this.ColOnMachines.Width = 180;
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
            this.LblSummary.Location = new System.Drawing.Point(12, 567);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(836, 18);
            this.LblSummary.TabIndex = 6;
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
            this.PanelBottom.Location = new System.Drawing.Point(12, 591);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(836, 44);
            this.PanelBottom.TabIndex = 7;
            //
            // BtnFromMachines
            //
            this.BtnFromMachines.Location = new System.Drawing.Point(9, 9);
            this.BtnFromMachines.Name = "BtnFromMachines";
            this.BtnFromMachines.Size = new System.Drawing.Size(150, 24);
            this.BtnFromMachines.TabIndex = 0;
            this.BtnFromMachines.Text = "Take from machines";
            this.BtnFromMachines.ToolTip = "Fill each line with what its machines already charge, where they agree on one figure";
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
            this.BtnOK.Location = new System.Drawing.Point(650, 9);
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
            this.BtnCancel.Location = new System.Drawing.Point(741, 9);
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
            this.ClientSize = new System.Drawing.Size(860, 647);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.LblMachines);
            this.Controls.Add(this.GridMachines);
            this.Controls.Add(this.PanelGroupBtns);
            this.Controls.Add(this.LblLines);
            this.Controls.Add(this.GridLines);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(760, 600);
            this.Name = "RentalGroupPrice_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Lines & Rental Price — which machines print together, and what the line costs";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).EndInit();
            this.PanelGroupBtns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelGroupBtns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMachines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMachines)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.LabelControl LblMachines;
        private DevExpress.XtraGrid.GridControl GridMachines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMachines;
        private DevExpress.XtraGrid.Columns.GridColumn ColSel;
        private DevExpress.XtraGrid.Columns.GridColumn ColItemNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColModel;
        private DevExpress.XtraGrid.Columns.GridColumn ColMergeGroup;
        private DevExpress.XtraGrid.Columns.GridColumn ColPrintsOn;
        private DevExpress.XtraGrid.Columns.GridColumn ColOwnRate;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
        private DevExpress.XtraEditors.PanelControl PanelGroupBtns;
        private DevExpress.XtraEditors.SimpleButton BtnMerge;
        private DevExpress.XtraEditors.SimpleButton BtnUngroup;
        private DevExpress.XtraEditors.LabelControl LblLines;
        private DevExpress.XtraGrid.GridControl GridLines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewLines;
        private DevExpress.XtraGrid.Columns.GridColumn ColLineName;
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
