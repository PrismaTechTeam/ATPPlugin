namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterListingInquiry_Form
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
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelFilter = new DevExpress.XtraEditors.PanelControl();
            this.GrpFilter = new DevExpress.XtraEditors.GroupControl();
            this.LblDebtorFrom = new DevExpress.XtraEditors.LabelControl();
            this.SluDebtorFrom = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluDebtorFromView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblDebtorTo = new DevExpress.XtraEditors.LabelControl();
            this.SluDebtorTo = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluDebtorToView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblContract = new DevExpress.XtraEditors.LabelControl();
            this.SluContract = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluContractView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblMonth = new DevExpress.XtraEditors.LabelControl();
            this.CmbMonth = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblYear = new DevExpress.XtraEditors.LabelControl();
            this.CmbYear = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnInquiry = new DevExpress.XtraEditors.SimpleButton();
            this.BtnAdvFilter = new DevExpress.XtraEditors.SimpleButton();
            this.BtnReset = new DevExpress.XtraEditors.SimpleButton();
            this.BtnPreview = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDesign = new DevExpress.XtraEditors.SimpleButton();
            this.BtnExport = new DevExpress.XtraEditors.SimpleButton();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.GridListing = new DevExpress.XtraGrid.GridControl();
            this.GridViewListing = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorFromView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorToView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContract.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContractView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbYear.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridListing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewListing)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Summary Sales Invoice Meter Listing";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1280, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelFilter
            //
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnInquiry);
            this.PanelFilter.Controls.Add(this.BtnPreview);
            this.PanelFilter.Controls.Add(this.BtnDesign);
            this.PanelFilter.Controls.Add(this.BtnExport);
            this.PanelFilter.Controls.Add(this.LblHint);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 56);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1280, 162);
            this.PanelFilter.TabIndex = 1;
            //
            // GrpFilter
            //
            this.GrpFilter.Controls.Add(this.LblDebtorFrom);
            this.GrpFilter.Controls.Add(this.SluDebtorFrom);
            this.GrpFilter.Controls.Add(this.LblDebtorTo);
            this.GrpFilter.Controls.Add(this.SluDebtorTo);
            this.GrpFilter.Controls.Add(this.LblContract);
            this.GrpFilter.Controls.Add(this.SluContract);
            this.GrpFilter.Controls.Add(this.LblMonth);
            this.GrpFilter.Controls.Add(this.CmbMonth);
            this.GrpFilter.Controls.Add(this.LblYear);
            this.GrpFilter.Controls.Add(this.CmbYear);
            this.GrpFilter.Controls.Add(this.BtnAdvFilter);
            this.GrpFilter.Controls.Add(this.BtnReset);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(600, 150);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            //
            // LblDebtorFrom
            //
            this.LblDebtorFrom.Location = new System.Drawing.Point(12, 32);
            this.LblDebtorFrom.Name = "LblDebtorFrom";
            this.LblDebtorFrom.Size = new System.Drawing.Size(72, 13);
            this.LblDebtorFrom.TabIndex = 0;
            this.LblDebtorFrom.Text = "Debtor From:";
            //
            // SluDebtorFrom
            //
            this.SluDebtorFrom.Location = new System.Drawing.Point(92, 29);
            this.SluDebtorFrom.Name = "SluDebtorFrom";
            this.SluDebtorFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluDebtorFrom.Properties.NullText = "(all)";
            this.SluDebtorFrom.Properties.PopupView = this.SluDebtorFromView;
            this.SluDebtorFrom.Size = new System.Drawing.Size(190, 20);
            this.SluDebtorFrom.TabIndex = 1;
            //
            // SluDebtorFromView
            //
            this.SluDebtorFromView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluDebtorFromView.Name = "SluDebtorFromView";
            this.SluDebtorFromView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluDebtorFromView.OptionsView.ShowGroupPanel = false;
            //
            // LblDebtorTo
            //
            this.LblDebtorTo.Location = new System.Drawing.Point(292, 32);
            this.LblDebtorTo.Name = "LblDebtorTo";
            this.LblDebtorTo.Size = new System.Drawing.Size(16, 13);
            this.LblDebtorTo.TabIndex = 2;
            this.LblDebtorTo.Text = "To:";
            //
            // SluDebtorTo
            //
            this.SluDebtorTo.Location = new System.Drawing.Point(316, 29);
            this.SluDebtorTo.Name = "SluDebtorTo";
            this.SluDebtorTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluDebtorTo.Properties.NullText = "(all)";
            this.SluDebtorTo.Properties.PopupView = this.SluDebtorToView;
            this.SluDebtorTo.Size = new System.Drawing.Size(190, 20);
            this.SluDebtorTo.TabIndex = 3;
            //
            // SluDebtorToView
            //
            this.SluDebtorToView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluDebtorToView.Name = "SluDebtorToView";
            this.SluDebtorToView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluDebtorToView.OptionsView.ShowGroupPanel = false;
            //
            // LblContract
            //
            this.LblContract.Location = new System.Drawing.Point(12, 62);
            this.LblContract.Name = "LblContract";
            this.LblContract.Size = new System.Drawing.Size(48, 13);
            this.LblContract.TabIndex = 4;
            this.LblContract.Text = "Contract:";
            //
            // SluContract
            //
            this.SluContract.Location = new System.Drawing.Point(92, 59);
            this.SluContract.Name = "SluContract";
            this.SluContract.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluContract.Properties.NullText = "(all)";
            this.SluContract.Properties.PopupView = this.SluContractView;
            this.SluContract.Size = new System.Drawing.Size(190, 20);
            this.SluContract.TabIndex = 5;
            //
            // SluContractView
            //
            this.SluContractView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluContractView.Name = "SluContractView";
            this.SluContractView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluContractView.OptionsView.ShowGroupPanel = false;
            //
            // LblMonth
            //
            this.LblMonth.Location = new System.Drawing.Point(292, 62);
            this.LblMonth.Name = "LblMonth";
            this.LblMonth.Size = new System.Drawing.Size(38, 13);
            this.LblMonth.TabIndex = 6;
            this.LblMonth.Text = "Month:";
            //
            // CmbMonth
            //
            this.CmbMonth.Location = new System.Drawing.Point(340, 59);
            this.CmbMonth.Name = "CmbMonth";
            this.CmbMonth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbMonth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbMonth.Size = new System.Drawing.Size(120, 20);
            this.CmbMonth.TabIndex = 7;
            //
            // LblYear
            //
            this.LblYear.Location = new System.Drawing.Point(470, 62);
            this.LblYear.Name = "LblYear";
            this.LblYear.Size = new System.Drawing.Size(26, 13);
            this.LblYear.TabIndex = 8;
            this.LblYear.Text = "Year:";
            //
            // CmbYear
            //
            this.CmbYear.Location = new System.Drawing.Point(502, 59);
            this.CmbYear.Name = "CmbYear";
            this.CmbYear.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbYear.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbYear.Size = new System.Drawing.Size(86, 20);
            this.CmbYear.TabIndex = 9;
            //
            // BtnAdvFilter
            //
            this.BtnAdvFilter.Location = new System.Drawing.Point(92, 92);
            this.BtnAdvFilter.Name = "BtnAdvFilter";
            this.BtnAdvFilter.Size = new System.Drawing.Size(190, 26);
            this.BtnAdvFilter.TabIndex = 10;
            this.BtnAdvFilter.Text = "Advanced Filter...";
            this.BtnAdvFilter.Click += new System.EventHandler(this.BtnAdvFilter_Click);
            //
            // BtnReset
            //
            this.BtnReset.Location = new System.Drawing.Point(316, 92);
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.Size = new System.Drawing.Size(190, 26);
            this.BtnReset.TabIndex = 11;
            this.BtnReset.Text = "Reset Filter";
            this.BtnReset.Click += new System.EventHandler(this.BtnReset_Click);
            //
            // BtnInquiry
            //
            this.BtnInquiry.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnInquiry.Appearance.Options.UseFont = true;
            this.BtnInquiry.Location = new System.Drawing.Point(620, 8);
            this.BtnInquiry.Name = "BtnInquiry";
            this.BtnInquiry.Size = new System.Drawing.Size(150, 50);
            this.BtnInquiry.TabIndex = 1;
            this.BtnInquiry.Text = "Inquiry";
            this.BtnInquiry.Click += new System.EventHandler(this.BtnInquiry_Click);
            //
            // BtnPreview
            //
            this.BtnPreview.Location = new System.Drawing.Point(776, 8);
            this.BtnPreview.Name = "BtnPreview";
            this.BtnPreview.Size = new System.Drawing.Size(150, 50);
            this.BtnPreview.TabIndex = 2;
            this.BtnPreview.Text = "Preview / Print";
            this.BtnPreview.Click += new System.EventHandler(this.BtnPreview_Click);
            //
            // BtnDesign
            //
            this.BtnDesign.Location = new System.Drawing.Point(932, 8);
            this.BtnDesign.Name = "BtnDesign";
            this.BtnDesign.Size = new System.Drawing.Size(150, 50);
            this.BtnDesign.TabIndex = 3;
            this.BtnDesign.Text = "Report Design";
            this.BtnDesign.Click += new System.EventHandler(this.BtnDesign_Click);
            //
            // BtnExport
            //
            this.BtnExport.Location = new System.Drawing.Point(1088, 8);
            this.BtnExport.Name = "BtnExport";
            this.BtnExport.Size = new System.Drawing.Size(150, 50);
            this.BtnExport.TabIndex = 4;
            this.BtnExport.Text = "Export to Excel";
            this.BtnExport.Click += new System.EventHandler(this.BtnExport_Click);
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Location = new System.Drawing.Point(620, 66);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(620, 40);
            this.LblHint.TabIndex = 5;
            this.LblHint.Text = "Filter Options narrows what is READ; Advanced Filter narrows what you already see, on any column.\r\nPreview and Report Design work on the rows on screen — filter first, then print.";
            //
            // GridListing
            //
            this.GridListing.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridListing.Location = new System.Drawing.Point(0, 218);
            this.GridListing.MainView = this.GridViewListing;
            this.GridListing.Name = "GridListing";
            this.GridListing.Size = new System.Drawing.Size(1280, 542);
            this.GridListing.TabIndex = 2;
            this.GridListing.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewListing});
            //
            // GridViewListing
            //
            this.GridViewListing.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewListing.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewListing.GridControl = this.GridListing;
            this.GridViewListing.Name = "GridViewListing";
            this.GridViewListing.OptionsBehavior.Editable = false;
            this.GridViewListing.OptionsView.ColumnAutoWidth = false;
            this.GridViewListing.OptionsView.ShowAutoFilterRow = true;
            this.GridViewListing.OptionsView.ShowFooter = true;
            this.GridViewListing.OptionsView.ShowGroupPanel = true;
            //
            // MeterListingInquiry_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 728);
            this.Controls.Add(this.GridListing);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelHeaderTop);
            this.Name = "MeterListingInquiry_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Summary Sales Invoice Meter Listing";
            ((System.ComponentModel.ISupportInitialize)(this.GridViewListing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridListing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbYear.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContractView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluContract.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorToView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorFromView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorFrom.Properties)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.PanelControl PanelFilter;
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblDebtorFrom;
        private DevExpress.XtraEditors.SearchLookUpEdit SluDebtorFrom;
        private DevExpress.XtraGrid.Views.Grid.GridView SluDebtorFromView;
        private DevExpress.XtraEditors.LabelControl LblDebtorTo;
        private DevExpress.XtraEditors.SearchLookUpEdit SluDebtorTo;
        private DevExpress.XtraGrid.Views.Grid.GridView SluDebtorToView;
        private DevExpress.XtraEditors.LabelControl LblContract;
        private DevExpress.XtraEditors.SearchLookUpEdit SluContract;
        private DevExpress.XtraGrid.Views.Grid.GridView SluContractView;
        private DevExpress.XtraEditors.LabelControl LblMonth;
        private DevExpress.XtraEditors.ComboBoxEdit CmbMonth;
        private DevExpress.XtraEditors.LabelControl LblYear;
        private DevExpress.XtraEditors.ComboBoxEdit CmbYear;
        private DevExpress.XtraEditors.SimpleButton BtnInquiry;
        private DevExpress.XtraEditors.SimpleButton BtnAdvFilter;
        private DevExpress.XtraEditors.SimpleButton BtnReset;
        private DevExpress.XtraEditors.SimpleButton BtnPreview;
        private DevExpress.XtraEditors.SimpleButton BtnDesign;
        private DevExpress.XtraEditors.SimpleButton BtnExport;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraGrid.GridControl GridListing;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewListing;
    }
}
