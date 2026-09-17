namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class ScpMaintenance_Form
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
            this.LblFeature = new DevExpress.XtraEditors.LabelControl();
            this.LblNotice = new DevExpress.XtraEditors.LabelControl();
            this.LblWhen = new DevExpress.XtraEditors.LabelControl();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelHeaderTop.Header = "Under Maintenance";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new System.Drawing.Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new System.Drawing.Size(560, 34);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // LblFeature
            //
            this.LblFeature.Appearance.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.LblFeature.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.LblFeature.Appearance.Options.UseFont = true;
            this.LblFeature.Appearance.Options.UseForeColor = true;
            this.LblFeature.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblFeature.Location = new System.Drawing.Point(24, 56);
            this.LblFeature.Name = "LblFeature";
            this.LblFeature.Size = new System.Drawing.Size(512, 28);
            this.LblFeature.TabIndex = 1;
            this.LblFeature.Text = "This screen";
            //
            // LblNotice
            //
            this.LblNotice.Appearance.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.LblNotice.Appearance.Options.UseFont = true;
            this.LblNotice.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblNotice.Appearance.Options.UseTextOptions = true;
            this.LblNotice.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblNotice.Location = new System.Drawing.Point(24, 96);
            this.LblNotice.Name = "LblNotice";
            this.LblNotice.Size = new System.Drawing.Size(512, 64);
            this.LblNotice.TabIndex = 2;
            this.LblNotice.Text = "This screen is not ready yet. It is being rebuilt for the new Service & Contract module and is switched off until then, so nothing here is wrong with your data.";
            //
            // LblWhen
            //
            this.LblWhen.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblWhen.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblWhen.Appearance.Options.UseFont = true;
            this.LblWhen.Appearance.Options.UseForeColor = true;
            this.LblWhen.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblWhen.Appearance.Options.UseTextOptions = true;
            this.LblWhen.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblWhen.Location = new System.Drawing.Point(24, 168);
            this.LblWhen.Name = "LblWhen";
            this.LblWhen.Size = new System.Drawing.Size(512, 40);
            this.LblWhen.TabIndex = 3;
            this.LblWhen.Text = "It will come back in a later update. Menu items marked (IN MAINTENANCE) all open this notice.";
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnClose.Appearance.Options.UseFont = true;
            this.BtnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnClose.Location = new System.Drawing.Point(436, 228);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(100, 30);
            this.BtnClose.TabIndex = 4;
            this.BtnClose.Text = "Close";
            //
            // ScpMaintenance_Form
            //
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnClose;
            this.ClientSize = new System.Drawing.Size(560, 274);
            this.Controls.Add(this.BtnClose);
            this.Controls.Add(this.LblWhen);
            this.Controls.Add(this.LblNotice);
            this.Controls.Add(this.LblFeature);
            this.Controls.Add(this.PanelHeaderTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScpMaintenance_Form";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Under Maintenance";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private DevExpress.XtraEditors.LabelControl LblFeature;
        private DevExpress.XtraEditors.LabelControl LblNotice;
        private DevExpress.XtraEditors.LabelControl LblWhen;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
    }
}
