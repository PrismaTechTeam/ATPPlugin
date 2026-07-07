namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterReadingSetting_Form
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.ChkIncludeExpired = new DevExpress.XtraEditors.CheckEdit();
            this.BtnOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.Location = new System.Drawing.Point(20, 18);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(190, 14);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Options for the Meter Reading list.";
            //
            // ChkIncludeExpired
            //
            this.ChkIncludeExpired.Location = new System.Drawing.Point(20, 45);
            this.ChkIncludeExpired.Name = "ChkIncludeExpired";
            this.ChkIncludeExpired.Properties.Caption = "Include expired Service Item";
            this.ChkIncludeExpired.Size = new System.Drawing.Size(320, 20);
            this.ChkIncludeExpired.TabIndex = 1;
            //
            // BtnOk
            //
            this.BtnOk.Location = new System.Drawing.Point(180, 95);
            this.BtnOk.Name = "BtnOk";
            this.BtnOk.Size = new System.Drawing.Size(80, 28);
            this.BtnOk.TabIndex = 2;
            this.BtnOk.Text = "OK";
            this.BtnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(268, 95);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(80, 28);
            this.BtnCancel.TabIndex = 3;
            this.BtnCancel.Text = "Cancel";
            //
            // MeterReadingSetting_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(368, 140);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnOk);
            this.Controls.Add(this.ChkIncludeExpired);
            this.Controls.Add(this.LblHint);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MeterReadingSetting_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meter Reading Settings";
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeExpired;
        private DevExpress.XtraEditors.SimpleButton BtnOk;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
