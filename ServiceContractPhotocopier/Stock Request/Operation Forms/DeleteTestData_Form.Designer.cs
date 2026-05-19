namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    partial class DeleteTestData_Form
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
        private DevExpress.XtraEditors.LabelControl LblWarning;
        private DevExpress.XtraEditors.LabelControl LblSummary;
        private DevExpress.XtraEditors.LabelControl LblStatus;
        private DevExpress.XtraEditors.SimpleButton BtnDelete;
        private DevExpress.XtraEditors.SimpleButton BtnClose;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTitle = new DevExpress.XtraEditors.PanelControl();
            this.LblTitle   = new DevExpress.XtraEditors.LabelControl();
            this.LblWarning = new DevExpress.XtraEditors.LabelControl();
            this.LblSummary = new DevExpress.XtraEditors.LabelControl();
            this.LblStatus  = new DevExpress.XtraEditors.LabelControl();
            this.BtnDelete  = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClose   = new DevExpress.XtraEditors.SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).BeginInit();
            this.PanelTitle.SuspendLayout();
            this.SuspendLayout();

            // PanelTitle
            this.PanelTitle.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.PanelTitle.Appearance.Options.UseBackColor = true;
            this.PanelTitle.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelTitle.Controls.Add(this.LblTitle);
            this.PanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTitle.Location = new System.Drawing.Point(0, 0);
            this.PanelTitle.Name = "PanelTitle";
            this.PanelTitle.Size = new System.Drawing.Size(520, 40);
            this.PanelTitle.TabIndex = 0;

            // LblTitle
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(14, 10);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(280, 21);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Delete All Test Data (Ctrl+Alt+0)";

            // LblWarning
            this.LblWarning.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblWarning.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.LblWarning.Appearance.Options.UseFont = true;
            this.LblWarning.Appearance.Options.UseForeColor = true;
            this.LblWarning.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblWarning.Location = new System.Drawing.Point(14, 56);
            this.LblWarning.Name = "LblWarning";
            this.LblWarning.Size = new System.Drawing.Size(490, 44);
            this.LblWarning.TabIndex = 1;
            this.LblWarning.Text = "This will delete every PUMS Stock Issue / Stock Transfer request from the\r\nplugin tables AND delete the matching AutoCount Stock Issue / Stock\r\nTransfer documents. There is no undo.";

            // LblSummary
            this.LblSummary.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblSummary.Appearance.Options.UseFont = true;
            this.LblSummary.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblSummary.Location = new System.Drawing.Point(14, 110);
            this.LblSummary.Name = "LblSummary";
            this.LblSummary.Size = new System.Drawing.Size(490, 36);
            this.LblSummary.TabIndex = 2;
            this.LblSummary.Text = "";

            // LblStatus
            this.LblStatus.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.LblStatus.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.LblStatus.Appearance.Options.UseFont = true;
            this.LblStatus.Appearance.Options.UseForeColor = true;
            this.LblStatus.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblStatus.Location = new System.Drawing.Point(14, 156);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new System.Drawing.Size(490, 32);
            this.LblStatus.TabIndex = 3;
            this.LblStatus.Text = "";

            // BtnDelete
            this.BtnDelete.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.BtnDelete.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnDelete.Appearance.ForeColor = System.Drawing.Color.White;
            this.BtnDelete.Appearance.Options.UseFont = true;
            this.BtnDelete.Appearance.Options.UseBackColor = true;
            this.BtnDelete.Appearance.Options.UseForeColor = true;
            this.BtnDelete.Location = new System.Drawing.Point(220, 200);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new System.Drawing.Size(180, 32);
            this.BtnDelete.TabIndex = 4;
            this.BtnDelete.Text = "Delete Everything";
            this.BtnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            // BtnClose
            this.BtnClose.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BtnClose.Appearance.Options.UseFont = true;
            this.BtnClose.Location = new System.Drawing.Point(414, 200);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(90, 32);
            this.BtnClose.TabIndex = 5;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 250);
            this.Controls.Add(this.BtnClose);
            this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.LblStatus);
            this.Controls.Add(this.LblSummary);
            this.Controls.Add(this.LblWarning);
            this.Controls.Add(this.PanelTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeleteTestData_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Delete All Test Data";

            ((System.ComponentModel.ISupportInitialize)(this.PanelTitle)).EndInit();
            this.PanelTitle.ResumeLayout(false);
            this.PanelTitle.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
