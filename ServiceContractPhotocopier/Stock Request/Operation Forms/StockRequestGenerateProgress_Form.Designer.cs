namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    partial class StockRequestGenerateProgress_Form
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
        private DevExpress.XtraEditors.ProgressBarControl Progress;
        private DevExpress.XtraEditors.LabelControl LblTotal;
        private DevExpress.XtraEditors.LabelControl LblDone;
        private DevExpress.XtraEditors.LabelControl LblFail;
        private DevExpress.XtraEditors.LabelControl LblCurrent;
        private DevExpress.XtraEditors.HyperLinkEdit LnkErrorLog;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.SimpleButton BtnClose;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTitle    = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle      = new DevExpress.XtraEditors.LabelControl();
            this.Progress      = new DevExpress.XtraEditors.ProgressBarControl();
            this.LblTotal      = new DevExpress.XtraEditors.LabelControl();
            this.LblDone       = new DevExpress.XtraEditors.LabelControl();
            this.LblFail       = new DevExpress.XtraEditors.LabelControl();
            this.LblCurrent    = new DevExpress.XtraEditors.LabelControl();
            this.LnkErrorLog   = new DevExpress.XtraEditors.HyperLinkEdit();
            this.BtnCancel     = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose      = new DevExpress.XtraEditors.SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).BeginInit();
            this.PanelTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Progress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LnkErrorLog.Properties)).BeginInit();
            this.SuspendLayout();

            // PanelTitle
            this.PanelTitle.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTitle.Appearance.Options.UseBackColor = true;
            this.PanelTitle.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTitle.Controls.Add(this.LblTitle);
            this.PanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTitle.Location = new System.Drawing.Point(0, 0);
            this.PanelTitle.Name = "PanelTitle";
            this.PanelTitle.Size = new System.Drawing.Size(520, 38);
            this.PanelTitle.TabIndex = 0;

            // LblTitle
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(14, 9);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(220, 20);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Generating SI / ST documents…";

            // Progress
            this.Progress.Location = new System.Drawing.Point(18, 58);
            this.Progress.Name = "Progress";
            this.Progress.Size = new System.Drawing.Size(484, 26);
            this.Progress.TabIndex = 1;
            this.Progress.Properties.PercentView = true;

            // LblTotal
            this.LblTotal.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblTotal.Appearance.Options.UseFont = true;
            this.LblTotal.Location = new System.Drawing.Point(18, 98);
            this.LblTotal.Name = "LblTotal";
            this.LblTotal.Size = new System.Drawing.Size(80, 15);
            this.LblTotal.TabIndex = 2;
            this.LblTotal.Text = "Total: 0";

            // LblDone
            this.LblDone.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblDone.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblDone.Appearance.Options.UseFont = true;
            this.LblDone.Appearance.Options.UseForeColor = true;
            this.LblDone.Location = new System.Drawing.Point(120, 98);
            this.LblDone.Name = "LblDone";
            this.LblDone.Size = new System.Drawing.Size(80, 15);
            this.LblDone.TabIndex = 3;
            this.LblDone.Text = "Done: 0";

            // LblFail
            this.LblFail.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.LblFail.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.LblFail.Appearance.Options.UseFont = true;
            this.LblFail.Appearance.Options.UseForeColor = true;
            this.LblFail.Location = new System.Drawing.Point(220, 98);
            this.LblFail.Name = "LblFail";
            this.LblFail.Size = new System.Drawing.Size(80, 15);
            this.LblFail.TabIndex = 4;
            this.LblFail.Text = "Failed: 0";

            // LblCurrent
            this.LblCurrent.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblCurrent.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.LblCurrent.Appearance.Options.UseFont = true;
            this.LblCurrent.Appearance.Options.UseForeColor = true;
            this.LblCurrent.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblCurrent.Location = new System.Drawing.Point(18, 128);
            this.LblCurrent.Name = "LblCurrent";
            this.LblCurrent.Size = new System.Drawing.Size(484, 18);
            this.LblCurrent.TabIndex = 5;
            this.LblCurrent.Text = "";

            // LnkErrorLog
            this.LnkErrorLog.Location = new System.Drawing.Point(18, 156);
            this.LnkErrorLog.Name = "LnkErrorLog";
            this.LnkErrorLog.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Underline);
            this.LnkErrorLog.Properties.Appearance.Options.UseFont = true;
            this.LnkErrorLog.Size = new System.Drawing.Size(220, 22);
            this.LnkErrorLog.TabIndex = 6;
            this.LnkErrorLog.Visible = false;
            this.LnkErrorLog.Click += new System.EventHandler(this.LnkErrorLog_Click);

            // BtnCancel
            this.BtnCancel.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnCancel.Appearance.Options.UseFont = true;
            this.BtnCancel.Location = new System.Drawing.Point(322, 192);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(85, 30);
            this.BtnCancel.TabIndex = 7;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // BtnClose
            this.BtnClose.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnClose.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.BtnClose.Appearance.ForeColor = System.Drawing.Color.White;
            this.BtnClose.Appearance.Options.UseFont = true;
            this.BtnClose.Appearance.Options.UseBackColor = true;
            this.BtnClose.Appearance.Options.UseForeColor = true;
            this.BtnClose.Location = new System.Drawing.Point(417, 192);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(85, 30);
            this.BtnClose.TabIndex = 8;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 240);
            this.Controls.Add(this.BtnClose);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.LnkErrorLog);
            this.Controls.Add(this.LblCurrent);
            this.Controls.Add(this.LblFail);
            this.Controls.Add(this.LblDone);
            this.Controls.Add(this.LblTotal);
            this.Controls.Add(this.Progress);
            this.Controls.Add(this.PanelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StockRequestGenerateProgress_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate SI / ST";

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).EndInit();
            this.PanelTitle.ResumeLayout(false);
            this.PanelTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Progress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LnkErrorLog.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
