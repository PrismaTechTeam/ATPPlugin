namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class BulkEmailInvoice_Form
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
            this.LblFrom = new DevExpress.XtraEditors.LabelControl();
            this.DtFrom = new DevExpress.XtraEditors.DateEdit();
            this.LblTo = new DevExpress.XtraEditors.LabelControl();
            this.DtTo = new DevExpress.XtraEditors.DateEdit();
            this.ChkMeterOnly = new DevExpress.XtraEditors.CheckEdit();
            this.LblDebtor = new DevExpress.XtraEditors.LabelControl();
            this.SluDebtor = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.SluDebtorView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.BtnLoad = new DevExpress.XtraEditors.SimpleButton();
            this.BtnReset = new DevExpress.XtraEditors.SimpleButton();
            this.LblEmailSrc = new DevExpress.XtraEditors.LabelControl();
            this.CmbEmailSource = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnSelectAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnMailSetting = new DevExpress.XtraEditors.SimpleButton();
            this.BtnChangeEmail = new DevExpress.XtraEditors.SimpleButton();
            this.BtnEmail = new DevExpress.XtraEditors.SimpleButton();
            this.LblCount = new DevExpress.XtraEditors.LabelControl();
            this.GridInv = new DevExpress.XtraGrid.GridControl();
            this.GridViewInv = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.RepoSel = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkMeterOnly.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtor.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbEmailSource.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridInv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).BeginInit();
            this.SuspendLayout();
            //
            // PanelHeaderTop  (native AutoCount green header; hint hidden in the ctor)
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Bulk Email Invoice";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(1280, 34);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelFilter
            //
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnSelectAll);
            this.PanelFilter.Controls.Add(this.BtnMailSetting);
            this.PanelFilter.Controls.Add(this.BtnChangeEmail);
            this.PanelFilter.Controls.Add(this.BtnEmail);
            this.PanelFilter.Controls.Add(this.LblCount);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 42);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1280, 164);
            this.PanelFilter.TabIndex = 1;
            //
            // GrpFilter
            //
            this.GrpFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GrpFilter.Appearance.Options.UseFont = true;
            this.GrpFilter.Controls.Add(this.LblFrom);
            this.GrpFilter.Controls.Add(this.DtFrom);
            this.GrpFilter.Controls.Add(this.LblTo);
            this.GrpFilter.Controls.Add(this.DtTo);
            this.GrpFilter.Controls.Add(this.ChkMeterOnly);
            this.GrpFilter.Controls.Add(this.LblDebtor);
            this.GrpFilter.Controls.Add(this.SluDebtor);
            this.GrpFilter.Controls.Add(this.BtnLoad);
            this.GrpFilter.Controls.Add(this.BtnReset);
            this.GrpFilter.Controls.Add(this.LblEmailSrc);
            this.GrpFilter.Controls.Add(this.CmbEmailSource);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(560, 150);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            //
            // Row 1 — Date From / To + meter-only
            //
            this.LblFrom.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFrom.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblFrom.Appearance.Options.UseFont = true;
            this.LblFrom.Appearance.Options.UseForeColor = true;
            this.LblFrom.Location = new System.Drawing.Point(10, 29);
            this.LblFrom.Name = "LblFrom";
            this.LblFrom.Size = new System.Drawing.Size(58, 15);
            this.LblFrom.TabIndex = 0;
            this.LblFrom.Text = "Date From:";
            //
            this.DtFrom.EditValue = null;
            this.DtFrom.Location = new System.Drawing.Point(88, 25);
            this.DtFrom.Name = "DtFrom";
            this.DtFrom.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.DtFrom.Properties.Appearance.Options.UseFont = true;
            this.DtFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtFrom.Size = new System.Drawing.Size(105, 22);
            this.DtFrom.TabIndex = 1;
            //
            this.LblTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblTo.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblTo.Appearance.Options.UseFont = true;
            this.LblTo.Appearance.Options.UseForeColor = true;
            this.LblTo.Location = new System.Drawing.Point(205, 29);
            this.LblTo.Name = "LblTo";
            this.LblTo.Size = new System.Drawing.Size(18, 15);
            this.LblTo.TabIndex = 2;
            this.LblTo.Text = "To:";
            //
            this.DtTo.EditValue = null;
            this.DtTo.Location = new System.Drawing.Point(232, 25);
            this.DtTo.Name = "DtTo";
            this.DtTo.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.DtTo.Properties.Appearance.Options.UseFont = true;
            this.DtTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtTo.Size = new System.Drawing.Size(105, 22);
            this.DtTo.TabIndex = 3;
            //
            this.ChkMeterOnly.Location = new System.Drawing.Point(348, 26);
            this.ChkMeterOnly.Name = "ChkMeterOnly";
            this.ChkMeterOnly.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkMeterOnly.Properties.Appearance.Options.UseFont = true;
            this.ChkMeterOnly.Properties.Caption = "Meter-billing invoices only";
            this.ChkMeterOnly.Size = new System.Drawing.Size(200, 22);
            this.ChkMeterOnly.TabIndex = 4;
            //
            // Row 2 — Customer + Filter / Reset
            //
            this.LblDebtor.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDebtor.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDebtor.Appearance.Options.UseFont = true;
            this.LblDebtor.Appearance.Options.UseForeColor = true;
            this.LblDebtor.Location = new System.Drawing.Point(10, 61);
            this.LblDebtor.Name = "LblDebtor";
            this.LblDebtor.Size = new System.Drawing.Size(54, 15);
            this.LblDebtor.TabIndex = 5;
            this.LblDebtor.Text = "Customer:";
            //
            this.SluDebtor.Location = new System.Drawing.Point(88, 57);
            this.SluDebtor.Name = "SluDebtor";
            this.SluDebtor.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.SluDebtor.Properties.Appearance.Options.UseFont = true;
            this.SluDebtor.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluDebtor.Properties.NullText = "(all customers)";
            this.SluDebtor.Properties.PopupView = this.SluDebtorView;
            this.SluDebtor.Size = new System.Drawing.Size(249, 22);
            this.SluDebtor.TabIndex = 6;
            //
            // SluDebtorView
            //
            this.SluDebtorView.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.SluDebtorView.Name = "SluDebtorView";
            this.SluDebtorView.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.SluDebtorView.OptionsView.ShowGroupPanel = false;
            //
            this.BtnLoad.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnLoad.Appearance.Options.UseFont = true;
            this.BtnLoad.Location = new System.Drawing.Point(350, 55);
            this.BtnLoad.Name = "BtnLoad";
            this.BtnLoad.Size = new System.Drawing.Size(98, 28);
            this.BtnLoad.TabIndex = 7;
            this.BtnLoad.Text = "Filter";
            this.BtnLoad.Click += new System.EventHandler(this.BtnLoad_Click);
            //
            this.BtnReset.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnReset.Appearance.Options.UseFont = true;
            this.BtnReset.Location = new System.Drawing.Point(454, 55);
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.Size = new System.Drawing.Size(66, 28);
            this.BtnReset.TabIndex = 8;
            this.BtnReset.Text = "Reset";
            this.BtnReset.Click += new System.EventHandler(this.BtnReset_Click);
            //
            // Row 3 — Email to
            //
            this.LblEmailSrc.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblEmailSrc.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblEmailSrc.Appearance.Options.UseFont = true;
            this.LblEmailSrc.Appearance.Options.UseForeColor = true;
            this.LblEmailSrc.Location = new System.Drawing.Point(10, 94);
            this.LblEmailSrc.Name = "LblEmailSrc";
            this.LblEmailSrc.Size = new System.Drawing.Size(47, 15);
            this.LblEmailSrc.TabIndex = 9;
            this.LblEmailSrc.Text = "Email to:";
            //
            this.CmbEmailSource.Location = new System.Drawing.Point(88, 90);
            this.CmbEmailSource.Name = "CmbEmailSource";
            this.CmbEmailSource.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.CmbEmailSource.Properties.Appearance.Options.UseFont = true;
            this.CmbEmailSource.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbEmailSource.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbEmailSource.Size = new System.Drawing.Size(249, 22);
            this.CmbEmailSource.TabIndex = 10;
            this.CmbEmailSource.SelectedIndexChanged += new System.EventHandler(this.CmbEmailSource_SelectedIndexChanged);
            //
            // Action buttons  (right of GrpFilter, 150x50 with icons — house style)
            //
            this.BtnSelectAll.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSelectAll.Appearance.Options.UseFont = true;
            this.BtnSelectAll.Location = new System.Drawing.Point(584, 8);
            this.BtnSelectAll.Name = "BtnSelectAll";
            this.BtnSelectAll.Size = new System.Drawing.Size(150, 50);
            this.BtnSelectAll.TabIndex = 1;
            this.BtnSelectAll.Text = "Select All";
            this.BtnSelectAll.Click += new System.EventHandler(this.BtnSelectAll_Click);
            //
            // BtnChangeEmail
            //
            this.BtnChangeEmail.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnChangeEmail.Appearance.Options.UseFont = true;
            this.BtnChangeEmail.Location = new System.Drawing.Point(740, 8);
            this.BtnChangeEmail.Name = "BtnChangeEmail";
            this.BtnChangeEmail.Size = new System.Drawing.Size(150, 50);
            this.BtnChangeEmail.TabIndex = 3;
            this.BtnChangeEmail.Text = "Change Email";
            this.BtnChangeEmail.Click += new System.EventHandler(this.BtnChangeEmail_Click);
            //
            this.BtnMailSetting.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnMailSetting.Appearance.Options.UseFont = true;
            this.BtnMailSetting.Location = new System.Drawing.Point(896, 8);
            this.BtnMailSetting.Name = "BtnMailSetting";
            this.BtnMailSetting.Size = new System.Drawing.Size(150, 50);
            this.BtnMailSetting.TabIndex = 2;
            this.BtnMailSetting.Text = "Email Setting";
            this.BtnMailSetting.Click += new System.EventHandler(this.BtnMailSetting_Click);
            //
            // BtnEmail  (red bold — same style as Generate Invoice)
            //
            this.BtnEmail.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnEmail.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnEmail.Appearance.Options.UseFont = true;
            this.BtnEmail.Appearance.Options.UseForeColor = true;
            this.BtnEmail.Location = new System.Drawing.Point(1052, 8);
            this.BtnEmail.Name = "BtnEmail";
            this.BtnEmail.Size = new System.Drawing.Size(170, 50);
            this.BtnEmail.TabIndex = 3;
            this.BtnEmail.Text = "Email Selected";
            this.BtnEmail.Click += new System.EventHandler(this.BtnEmail_Click);
            //
            // LblCount  (status line under the action buttons)
            //
            this.LblCount.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblCount.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblCount.Appearance.Options.UseFont = true;
            this.LblCount.Appearance.Options.UseForeColor = true;
            this.LblCount.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblCount.Location = new System.Drawing.Point(588, 66);
            this.LblCount.Name = "LblCount";
            this.LblCount.Size = new System.Drawing.Size(500, 16);
            this.LblCount.TabIndex = 4;
            this.LblCount.Text = "0 invoice(s)";
            //
            // GridInv / GridViewInv  (Fill — below PanelFilter)
            //
            this.GridInv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridInv.Location = new System.Drawing.Point(0, 206);
            this.GridInv.MainView = this.GridViewInv;
            this.GridInv.Name = "GridInv";
            this.GridInv.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoSel});
            this.GridInv.Size = new System.Drawing.Size(1280, 522);
            this.GridInv.TabIndex = 2;
            this.GridInv.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewInv});
            //
            // GridViewInv
            //
            this.GridViewInv.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewInv.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewInv.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewInv.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewInv.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewInv.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewInv.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewInv.Appearance.Row.Options.UseFont = true;
            this.GridViewInv.GridControl = this.GridInv;
            this.GridViewInv.Name = "GridViewInv";
            this.GridViewInv.OptionsView.ShowAutoFilterRow = true;
            this.GridViewInv.OptionsView.ShowGroupPanel = false;
            this.GridViewInv.RowHeight = 24;
            //
            // RepoSel
            //
            this.RepoSel.AutoHeight = false;
            this.RepoSel.Name = "RepoSel";
            //
            // BulkEmailInvoice_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 728);
            this.Controls.Add(this.GridInv);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelHeaderTop);
            this.Name = "BulkEmailInvoice_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bulk Email Invoice";
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkMeterOnly.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtor.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluDebtorView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbEmailSource.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridInv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewInv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoSel)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.PanelControl PanelFilter;
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblFrom;
        private DevExpress.XtraEditors.DateEdit DtFrom;
        private DevExpress.XtraEditors.LabelControl LblTo;
        private DevExpress.XtraEditors.DateEdit DtTo;
        private DevExpress.XtraEditors.CheckEdit ChkMeterOnly;
        private DevExpress.XtraEditors.LabelControl LblDebtor;
        private DevExpress.XtraEditors.SearchLookUpEdit SluDebtor;
        private DevExpress.XtraGrid.Views.Grid.GridView SluDebtorView;
        private DevExpress.XtraEditors.SimpleButton BtnLoad;
        private DevExpress.XtraEditors.SimpleButton BtnReset;
        private DevExpress.XtraEditors.LabelControl LblEmailSrc;
        private DevExpress.XtraEditors.ComboBoxEdit CmbEmailSource;
        private DevExpress.XtraEditors.SimpleButton BtnSelectAll;
        private DevExpress.XtraEditors.SimpleButton BtnMailSetting;
        private DevExpress.XtraEditors.SimpleButton BtnChangeEmail;
        private DevExpress.XtraEditors.SimpleButton BtnEmail;
        private DevExpress.XtraEditors.LabelControl LblCount;
        private DevExpress.XtraGrid.GridControl GridInv;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewInv;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit RepoSel;
    }
}
