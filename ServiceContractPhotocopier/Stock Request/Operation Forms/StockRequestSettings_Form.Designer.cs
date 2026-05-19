namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    partial class StockRequestSettings_Form
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

        private DevExpress.XtraEditors.PanelControl PanelTitle;
        private DevExpress.XtraEditors.LabelControl LblTitle;
        private DevExpress.XtraEditors.LabelControl LblFromLocation;
        private DevExpress.XtraEditors.SearchLookUpEdit TxtFromLocation;
        private DevExpress.XtraGrid.Views.Grid.GridView TxtFromLocationView;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.CheckEdit ChkFlagControl;
        private DevExpress.XtraEditors.LabelControl LblFlagHint;
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTitle      = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle        = new DevExpress.XtraEditors.LabelControl();
            this.LblFromLocation = new DevExpress.XtraEditors.LabelControl();
            this.TxtFromLocation = new DevExpress.XtraEditors.SearchLookUpEdit();
            this.TxtFromLocationView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblHint         = new DevExpress.XtraEditors.LabelControl();
            this.ChkFlagControl  = new DevExpress.XtraEditors.CheckEdit();
            this.LblFlagHint     = new DevExpress.XtraEditors.LabelControl();
            this.BtnSave         = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel       = new DevExpress.XtraEditors.SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).BeginInit();
            this.PanelTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFromLocation.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFromLocationView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFlagControl.Properties)).BeginInit();
            this.SuspendLayout();

            // PanelTitle
            this.PanelTitle.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTitle.Appearance.Options.UseBackColor = true;
            this.PanelTitle.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTitle.Controls.Add(this.LblTitle);
            this.PanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTitle.Location = new System.Drawing.Point(0, 0);
            this.PanelTitle.Name = "PanelTitle";
            this.PanelTitle.Size = new System.Drawing.Size(440, 38);
            this.PanelTitle.TabIndex = 0;
            //
            // LblTitle
            //
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(14, 9);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(200, 20);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Stock Request Settings";

            // LblFromLocation
            this.LblFromLocation.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblFromLocation.Appearance.Options.UseFont = true;
            this.LblFromLocation.Location = new System.Drawing.Point(14, 60);
            this.LblFromLocation.Name = "LblFromLocation";
            this.LblFromLocation.Size = new System.Drawing.Size(135, 15);
            this.LblFromLocation.TabIndex = 1;
            this.LblFromLocation.Text = "Default From Location:";

            // TxtFromLocation
            this.TxtFromLocation.Location = new System.Drawing.Point(155, 56);
            this.TxtFromLocation.Name = "TxtFromLocation";
            this.TxtFromLocation.Properties.NullValuePrompt = "HQ";
            this.TxtFromLocation.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.TxtFromLocation.Properties.View = this.TxtFromLocationView;
            this.TxtFromLocation.Size = new System.Drawing.Size(265, 24);
            this.TxtFromLocation.TabIndex = 2;
            // Inner GridView for the lookup
            this.TxtFromLocationView.Name = "TxtFromLocationView";
            this.TxtFromLocationView.OptionsView.ShowGroupPanel = false;
            this.TxtFromLocationView.OptionsView.ShowIndicator = false;
            this.TxtFromLocationView.OptionsBehavior.AutoPopulateColumns = true;

            // LblHint
            this.LblHint.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.LblHint.Appearance.Options.UseFont = true;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(155, 86);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(265, 30);
            this.LblHint.TabIndex = 3;
            this.LblHint.Text = "Used as the From-Location on every Stock Transfer\r\ngenerated from a Stock Request Task.";

            // ChkFlagControl
            this.ChkFlagControl.Location = new System.Drawing.Point(14, 140);
            this.ChkFlagControl.Name = "ChkFlagControl";
            this.ChkFlagControl.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.ChkFlagControl.Properties.Appearance.Options.UseFont = true;
            this.ChkFlagControl.Properties.Caption = "Flag Control (overwrite duplicate pushes)";
            this.ChkFlagControl.Size = new System.Drawing.Size(280, 22);
            this.ChkFlagControl.TabIndex = 6;

            // LblFlagHint
            this.LblFlagHint.Appearance.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.LblFlagHint.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.LblFlagHint.Appearance.Options.UseFont = true;
            this.LblFlagHint.Appearance.Options.UseForeColor = true;
            this.LblFlagHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFlagHint.Location = new System.Drawing.Point(14, 166);
            this.LblFlagHint.Name = "LblFlagHint";
            this.LblFlagHint.Size = new System.Drawing.Size(406, 44);
            this.LblFlagHint.TabIndex = 7;
            this.LblFlagHint.Text = "Off (default) — re-pushing an open request is silently dropped.\r\nOn — the older row is deleted and the new payload replaces it as Update.\r\nClosed (Complete / Ignored) rows are never touched.";

            // BtnSave
            this.BtnSave.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnSave.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.BtnSave.Appearance.ForeColor = System.Drawing.Color.White;
            this.BtnSave.Appearance.Options.UseFont = true;
            this.BtnSave.Appearance.Options.UseBackColor = true;
            this.BtnSave.Appearance.Options.UseForeColor = true;
            this.BtnSave.Location = new System.Drawing.Point(238, 230);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(85, 30);
            this.BtnSave.TabIndex = 4;
            this.BtnSave.Text = "Save";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);

            // BtnCancel
            this.BtnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnCancel.Appearance.Options.UseFont = true;
            this.BtnCancel.Location = new System.Drawing.Point(335, 230);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 30);
            this.BtnCancel.TabIndex = 5;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // StockRequestSettings_Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 278);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.LblFlagHint);
            this.Controls.Add(this.ChkFlagControl);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.TxtFromLocation);
            this.Controls.Add(this.LblFromLocation);
            this.Controls.Add(this.PanelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StockRequestSettings_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Stock Request Settings";
            this.AcceptButton = this.BtnSave;
            this.CancelButton = this.BtnCancel;

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).EndInit();
            this.PanelTitle.ResumeLayout(false);
            this.PanelTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFromLocationView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFromLocation.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFlagControl.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
