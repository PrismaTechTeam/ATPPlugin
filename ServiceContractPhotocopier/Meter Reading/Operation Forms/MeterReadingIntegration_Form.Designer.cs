namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterReadingIntegration_Form
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

        // ---- Title bar ----
        private DevExpress.XtraEditors.PanelControl PanelTitle;
        private DevExpress.XtraEditors.LabelControl LblTitle;

        // ---- Filter panel ----
        private DevExpress.XtraEditors.PanelControl PanelFilter;

        // ---- Filter group ----
        private DevExpress.XtraEditors.GroupControl GrpFilter;
        private DevExpress.XtraEditors.LabelControl LblSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraEditors.CheckEdit ChkShowAll;
        private DevExpress.XtraEditors.LabelControl LblDay;
        private DevExpress.XtraEditors.ComboBoxEdit CmbDay;
        private DevExpress.XtraEditors.SimpleButton BtnFilter;
        private DevExpress.XtraEditors.SimpleButton BtnReset;
        private DevExpress.XtraEditors.LabelControl LblMonth;
        private DevExpress.XtraEditors.ComboBoxEdit CmbMonth;

        // ---- Action buttons (right of GrpFilter, 52px tall) ----
        private DevExpress.XtraEditors.SimpleButton BtnRefresh;
        private DevExpress.XtraEditors.SimpleButton BtnFetch;
        private DevExpress.XtraEditors.SimpleButton BtnSelfManualKeyIn;
        private DevExpress.XtraEditors.SimpleButton BtnGenerateInvoice;
        private DevExpress.XtraEditors.LabelControl LblEditCaption;
        private DevExpress.XtraEditors.LabelControl LblToday;

        // ---- Auto-refresh timer ----
        private System.Windows.Forms.Timer RefreshTimer;

        // ---- Data grid ----
        private DevExpress.XtraGrid.GridControl GridMeter;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewMeter;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components         = new System.ComponentModel.Container();
            this.PanelTitle         = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle           = new DevExpress.XtraEditors.LabelControl();
            this.PanelFilter        = new DevExpress.XtraEditors.PanelControl();
            this.GrpFilter          = new DevExpress.XtraEditors.GroupControl();
            this.LblSearch          = new DevExpress.XtraEditors.LabelControl();
            this.TxtSearch          = new DevExpress.XtraEditors.TextEdit();
            this.ChkShowAll         = new DevExpress.XtraEditors.CheckEdit();
            this.LblDay             = new DevExpress.XtraEditors.LabelControl();
            this.CmbDay             = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnFilter          = new DevExpress.XtraEditors.SimpleButton();
            this.BtnReset           = new DevExpress.XtraEditors.SimpleButton();
            this.LblMonth           = new DevExpress.XtraEditors.LabelControl();
            this.CmbMonth           = new DevExpress.XtraEditors.ComboBoxEdit();
            this.BtnRefresh         = new DevExpress.XtraEditors.SimpleButton();
            this.BtnFetch           = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSelfManualKeyIn = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGenerateInvoice = new DevExpress.XtraEditors.SimpleButton();
            this.LblEditCaption     = new DevExpress.XtraEditors.LabelControl();
            this.LblToday           = new DevExpress.XtraEditors.LabelControl();
            this.RefreshTimer       = new System.Windows.Forms.Timer(this.components);
            this.GridMeter          = new DevExpress.XtraGrid.GridControl();
            this.GridViewMeter      = new DevExpress.XtraGrid.Views.Grid.GridView();

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).BeginInit();
            this.PanelTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).BeginInit();
            this.PanelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).BeginInit();
            this.GrpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowAll.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbDay.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeter)).BeginInit();
            this.SuspendLayout();

            // ================================================================
            // PanelTitle  (green bar)
            // ================================================================
            this.PanelTitle.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTitle.Appearance.Options.UseBackColor = true;
            this.PanelTitle.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTitle.Controls.Add(this.LblTitle);
            this.PanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTitle.Location = new System.Drawing.Point(0, 0);
            this.PanelTitle.Name = "PanelTitle";
            this.PanelTitle.Size = new System.Drawing.Size(1280, 42);
            this.PanelTitle.TabIndex = 0;
            //
            // LblTitle
            //
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblTitle.Location = new System.Drawing.Point(18, 10);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(600, 22);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Meter Reading Integration with Pump System";

            // ================================================================
            // PanelFilter
            // ================================================================
            this.PanelFilter.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.PanelFilter.Appearance.Options.UseBackColor = true;
            this.PanelFilter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelFilter.Controls.Add(this.GrpFilter);
            this.PanelFilter.Controls.Add(this.BtnRefresh);
            this.PanelFilter.Controls.Add(this.BtnFetch);
            this.PanelFilter.Controls.Add(this.BtnSelfManualKeyIn);
            this.PanelFilter.Controls.Add(this.BtnGenerateInvoice);
            this.PanelFilter.Controls.Add(this.LblEditCaption);
            this.PanelFilter.Controls.Add(this.LblToday);
            this.PanelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelFilter.Location = new System.Drawing.Point(0, 42);
            this.PanelFilter.Name = "PanelFilter";
            this.PanelFilter.Size = new System.Drawing.Size(1280, 136);
            this.PanelFilter.TabIndex = 1;

            // ================================================================
            // GrpFilter  (same dimensions as the Stock Request Task form)
            // ================================================================
            this.GrpFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GrpFilter.Appearance.Options.UseFont = true;
            this.GrpFilter.Controls.Add(this.LblSearch);
            this.GrpFilter.Controls.Add(this.TxtSearch);
            this.GrpFilter.Controls.Add(this.ChkShowAll);
            this.GrpFilter.Controls.Add(this.LblDay);
            this.GrpFilter.Controls.Add(this.CmbDay);
            this.GrpFilter.Controls.Add(this.BtnFilter);
            this.GrpFilter.Controls.Add(this.BtnReset);
            this.GrpFilter.Controls.Add(this.LblMonth);
            this.GrpFilter.Controls.Add(this.CmbMonth);
            this.GrpFilter.Location = new System.Drawing.Point(8, 6);
            this.GrpFilter.Name = "GrpFilter";
            this.GrpFilter.Size = new System.Drawing.Size(494, 124);
            this.GrpFilter.TabIndex = 0;
            this.GrpFilter.Text = "Filter Options";
            //
            // Row 1 — Search Any + textbox + Show All
            //
            this.LblSearch.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblSearch.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblSearch.Appearance.Options.UseFont = true;
            this.LblSearch.Appearance.Options.UseForeColor = true;
            this.LblSearch.Location = new System.Drawing.Point(10, 29);
            this.LblSearch.Name = "LblSearch";
            this.LblSearch.Size = new System.Drawing.Size(62, 15);
            this.LblSearch.TabIndex = 0;
            this.LblSearch.Text = "Search Any:";
            //
            this.TxtSearch.Location = new System.Drawing.Point(78, 25);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Properties.NullValuePrompt = "Search anything...";
            this.TxtSearch.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtSearch.Size = new System.Drawing.Size(185, 22);
            this.TxtSearch.TabIndex = 1;
            //
            this.ChkShowAll.Location = new System.Drawing.Point(272, 25);
            this.ChkShowAll.Name = "ChkShowAll";
            this.ChkShowAll.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkShowAll.Properties.Appearance.Options.UseFont = true;
            this.ChkShowAll.Properties.Caption = "Show All";
            this.ChkShowAll.Size = new System.Drawing.Size(175, 22);
            this.ChkShowAll.TabIndex = 2;
            //
            // Row 2 — Day + lookup + Filter + Reset
            //
            this.LblDay.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblDay.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblDay.Appearance.Options.UseFont = true;
            this.LblDay.Appearance.Options.UseForeColor = true;
            this.LblDay.Location = new System.Drawing.Point(10, 62);
            this.LblDay.Name = "LblDay";
            this.LblDay.Size = new System.Drawing.Size(28, 15);
            this.LblDay.TabIndex = 3;
            this.LblDay.Text = "Day:";
            //
            this.CmbDay.Location = new System.Drawing.Point(46, 58);
            this.CmbDay.Name = "CmbDay";
            this.CmbDay.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.CmbDay.Properties.Appearance.Options.UseFont = true;
            this.CmbDay.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbDay.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbDay.Properties.NullValuePrompt = "Select day...";
            this.CmbDay.Size = new System.Drawing.Size(130, 22);
            this.CmbDay.TabIndex = 4;
            //
            this.BtnFilter.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnFilter.Appearance.Options.UseFont = true;
            this.BtnFilter.Location = new System.Drawing.Point(309, 56);
            this.BtnFilter.Name = "BtnFilter";
            this.BtnFilter.Size = new System.Drawing.Size(70, 28);
            this.BtnFilter.TabIndex = 5;
            this.BtnFilter.Text = "Filter";
            this.BtnFilter.Click += new System.EventHandler(this.BtnFilter_Click);
            //
            this.BtnReset.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnReset.Appearance.Options.UseFont = true;
            this.BtnReset.Location = new System.Drawing.Point(385, 56);
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.Size = new System.Drawing.Size(70, 28);
            this.BtnReset.TabIndex = 6;
            this.BtnReset.Text = "Reset";
            this.BtnReset.Click += new System.EventHandler(this.BtnReset_Click);
            //
            // Row 3 — Month combobox
            //
            this.LblMonth.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblMonth.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblMonth.Appearance.Options.UseFont = true;
            this.LblMonth.Appearance.Options.UseForeColor = true;
            this.LblMonth.Location = new System.Drawing.Point(10, 96);
            this.LblMonth.Name = "LblMonth";
            this.LblMonth.Size = new System.Drawing.Size(42, 15);
            this.LblMonth.TabIndex = 7;
            this.LblMonth.Text = "Month:";
            //
            this.CmbMonth.Location = new System.Drawing.Point(58, 92);
            this.CmbMonth.Name = "CmbMonth";
            this.CmbMonth.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.CmbMonth.Properties.Appearance.Options.UseFont = true;
            this.CmbMonth.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.CmbMonth.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbMonth.Properties.NullValuePrompt = "Select month...";
            this.CmbMonth.Size = new System.Drawing.Size(130, 22);
            this.CmbMonth.TabIndex = 8;

            // ================================================================
            // Action buttons  (right of GrpFilter, 52px tall, y=28)
            // ================================================================
            //
            // LblEditCaption  ("Edit:" small caption — UPPER row, sits above the buttons)
            //
            this.LblEditCaption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.LblEditCaption.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.LblEditCaption.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.LblEditCaption.Appearance.Options.UseFont = true;
            this.LblEditCaption.Appearance.Options.UseForeColor = true;
            this.LblEditCaption.Location = new System.Drawing.Point(510, 4);
            this.LblEditCaption.Name = "LblEditCaption";
            this.LblEditCaption.Size = new System.Drawing.Size(28, 13);
            this.LblEditCaption.TabIndex = 14;
            this.LblEditCaption.Text = "Edit:";
            //
            // LblToday  (today's date — UPPER row, beside the Edit caption)
            //
            this.LblToday.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.LblToday.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.LblToday.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblToday.Appearance.Options.UseFont = true;
            this.LblToday.Appearance.Options.UseForeColor = true;
            this.LblToday.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblToday.Location = new System.Drawing.Point(510, 18);
            this.LblToday.Name = "LblToday";
            this.LblToday.Size = new System.Drawing.Size(220, 22);
            this.LblToday.TabIndex = 15;
            this.LblToday.Text = "";

            // ----- Action buttons row (LOWER, below the labels, y=30) -----
            //
            // BtnRefresh
            //
            this.BtnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnRefresh.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnRefresh.Appearance.Options.UseFont = true;
            this.BtnRefresh.Location = new System.Drawing.Point(510, 46);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new System.Drawing.Size(110, 52);
            this.BtnRefresh.TabIndex = 10;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            //
            // BtnFetch
            //
            this.BtnFetch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnFetch.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnFetch.Appearance.Options.UseFont = true;
            this.BtnFetch.Location = new System.Drawing.Point(628, 46);
            this.BtnFetch.Name = "BtnFetch";
            this.BtnFetch.Size = new System.Drawing.Size(95, 52);
            this.BtnFetch.TabIndex = 11;
            this.BtnFetch.Text = "Fetch";
            this.BtnFetch.Click += new System.EventHandler(this.BtnFetch_Click);
            //
            // BtnSelfManualKeyIn
            //
            this.BtnSelfManualKeyIn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnSelfManualKeyIn.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnSelfManualKeyIn.Appearance.Options.UseFont = true;
            this.BtnSelfManualKeyIn.Location = new System.Drawing.Point(731, 46);
            this.BtnSelfManualKeyIn.Name = "BtnSelfManualKeyIn";
            this.BtnSelfManualKeyIn.Size = new System.Drawing.Size(160, 52);
            this.BtnSelfManualKeyIn.TabIndex = 12;
            this.BtnSelfManualKeyIn.Text = "Select All";
            this.BtnSelfManualKeyIn.Click += new System.EventHandler(this.BtnSelfManualKeyIn_Click);
            //
            // BtnGenerateInvoice  (red bold, same style as Generate SI/ST)
            //
            this.BtnGenerateInvoice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.BtnGenerateInvoice.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.BtnGenerateInvoice.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnGenerateInvoice.Appearance.Options.UseFont = true;
            this.BtnGenerateInvoice.Appearance.Options.UseForeColor = true;
            this.BtnGenerateInvoice.Location = new System.Drawing.Point(899, 46);
            this.BtnGenerateInvoice.Name = "BtnGenerateInvoice";
            this.BtnGenerateInvoice.Size = new System.Drawing.Size(180, 52);
            this.BtnGenerateInvoice.TabIndex = 13;
            this.BtnGenerateInvoice.Text = "Generate Invoice";
            this.BtnGenerateInvoice.Click += new System.EventHandler(this.BtnGenerateInvoice_Click);

            // ================================================================
            // RefreshTimer (1-second tick for the countdown)
            // ================================================================
            this.RefreshTimer.Interval = 1000;
            this.RefreshTimer.Tick += new System.EventHandler(this.RefreshTimer_Tick);

            // ================================================================
            // GridMeter / GridViewMeter  (Fill — below PanelFilter)
            // ================================================================
            this.GridMeter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridMeter.Location = new System.Drawing.Point(0, 178);
            this.GridMeter.MainView = this.GridViewMeter;
            this.GridMeter.Name = "GridMeter";
            this.GridMeter.Size = new System.Drawing.Size(1280, 550);
            this.GridMeter.TabIndex = 2;
            this.GridMeter.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewMeter});
            //
            // GridViewMeter
            //
            this.GridViewMeter.Appearance.HeaderPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.GridViewMeter.Appearance.HeaderPanel.Options.UseFont = true;
            this.GridViewMeter.Appearance.HeaderPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(239)))), ((int)(((byte)(241)))));
            this.GridViewMeter.Appearance.HeaderPanel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(71)))), ((int)(((byte)(79)))));
            this.GridViewMeter.Appearance.HeaderPanel.Options.UseBackColor = true;
            this.GridViewMeter.Appearance.HeaderPanel.Options.UseForeColor = true;
            this.GridViewMeter.Appearance.Row.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.GridViewMeter.Appearance.Row.Options.UseFont = true;
            this.GridViewMeter.Appearance.EvenRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.GridViewMeter.Appearance.EvenRow.Options.UseBackColor = true;
            this.GridViewMeter.Appearance.OddRow.BackColor = System.Drawing.Color.White;
            this.GridViewMeter.Appearance.OddRow.Options.UseBackColor = true;
            this.GridViewMeter.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(227)))), ((int)(((byte)(242)))), ((int)(((byte)(253)))));
            this.GridViewMeter.Appearance.FocusedRow.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.GridViewMeter.Appearance.FocusedRow.Options.UseBackColor = true;
            this.GridViewMeter.Appearance.FocusedRow.Options.UseForeColor = true;
            this.GridViewMeter.Appearance.Empty.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.GridViewMeter.Appearance.Empty.Options.UseBackColor = true;
            this.GridViewMeter.GridControl = this.GridMeter;
            this.GridViewMeter.Name = "GridViewMeter";
            this.GridViewMeter.OptionsView.ShowGroupPanel = false;
            this.GridViewMeter.OptionsView.ShowViewCaption = true;
            this.GridViewMeter.ViewCaption = "Meter Reading";
            this.GridViewMeter.Appearance.ViewCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.GridViewMeter.Appearance.ViewCaption.ForeColor = System.Drawing.Color.Black;
            this.GridViewMeter.Appearance.ViewCaption.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.GridViewMeter.Appearance.ViewCaption.Options.UseFont = true;
            this.GridViewMeter.Appearance.ViewCaption.Options.UseForeColor = true;
            this.GridViewMeter.Appearance.ViewCaption.Options.UseBackColor = true;
            this.GridViewMeter.OptionsView.EnableAppearanceEvenRow = false;
            this.GridViewMeter.OptionsView.EnableAppearanceOddRow = false;
            this.GridViewMeter.RowHeight = 24;
            this.GridViewMeter.ColumnPanelRowHeight = 28;

            // ================================================================
            // MeterReadingIntegration_Form
            // ================================================================
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 728);
            // Grid first (Fill), then PanelFilter (Top, lower), then PanelTitle (Top, top).
            this.Controls.Add(this.GridMeter);
            this.Controls.Add(this.PanelFilter);
            this.Controls.Add(this.PanelTitle);
            this.Name = "MeterReadingIntegration_Form";
            this.Text = "Meter Reading Integration with Pump System";

            ((System.ComponentModel.ISupportInitialize)(this.GridViewMeter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridMeter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbMonth.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbDay.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkShowAll.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            this.GrpFilter.ResumeLayout(false);
            this.GrpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpFilter)).EndInit();
            this.PanelFilter.ResumeLayout(false);
            this.PanelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelFilter)).EndInit();
            this.PanelTitle.ResumeLayout(false);
            this.PanelTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
