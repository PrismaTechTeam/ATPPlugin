namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class BulkEmailTemplate_Form
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
            this.LblSubject = new DevExpress.XtraEditors.LabelControl();
            this.TxtSubject = new DevExpress.XtraEditors.TextEdit();
            this.LblBody = new DevExpress.XtraEditors.LabelControl();
            this.TxtBody = new DevExpress.XtraEditors.MemoEdit();
            this.LblTokens = new DevExpress.XtraEditors.LabelControl();
            this.BtnSave = new DevExpress.XtraEditors.SimpleButton();
            this.BtnDefault = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSubject.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtBody.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // LblSubject
            //
            this.LblSubject.Location = new System.Drawing.Point(14, 15);
            this.LblSubject.Name = "LblSubject";
            this.LblSubject.Size = new System.Drawing.Size(38, 13);
            this.LblSubject.Text = "Subject";
            //
            // TxtSubject
            //
            this.TxtSubject.Location = new System.Drawing.Point(70, 12);
            this.TxtSubject.Name = "TxtSubject";
            this.TxtSubject.Size = new System.Drawing.Size(430, 20);
            this.TxtSubject.TabIndex = 1;
            //
            // LblBody
            //
            this.LblBody.Location = new System.Drawing.Point(14, 44);
            this.LblBody.Name = "LblBody";
            this.LblBody.Size = new System.Drawing.Size(45, 13);
            this.LblBody.Text = "Message";
            //
            // TxtBody
            //
            this.TxtBody.Location = new System.Drawing.Point(70, 41);
            this.TxtBody.Name = "TxtBody";
            this.TxtBody.Size = new System.Drawing.Size(430, 180);
            this.TxtBody.TabIndex = 2;
            //
            // LblTokens
            //
            this.LblTokens.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(110)))), ((int)(((byte)(110)))));
            this.LblTokens.Appearance.Options.UseForeColor = true;
            this.LblTokens.Location = new System.Drawing.Point(70, 227);
            this.LblTokens.Name = "LblTokens";
            this.LblTokens.Size = new System.Drawing.Size(430, 13);
            this.LblTokens.Text = "Tokens: {AccNo} = customer code · {CompanyName} = customer name · {DocNos} = invoices";
            //
            // BtnSave
            //
            this.BtnSave.Location = new System.Drawing.Point(232, 252);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(90, 28);
            this.BtnSave.TabIndex = 3;
            this.BtnSave.Text = "Save";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            //
            // BtnDefault
            //
            this.BtnDefault.Location = new System.Drawing.Point(328, 252);
            this.BtnDefault.Name = "BtnDefault";
            this.BtnDefault.Size = new System.Drawing.Size(80, 28);
            this.BtnDefault.TabIndex = 4;
            this.BtnDefault.Text = "Default";
            this.BtnDefault.Click += new System.EventHandler(this.BtnDefault_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(414, 252);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(86, 28);
            this.BtnCancel.TabIndex = 5;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // BulkEmailTemplate_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 292);
            this.Controls.Add(this.LblSubject);
            this.Controls.Add(this.TxtSubject);
            this.Controls.Add(this.LblBody);
            this.Controls.Add(this.TxtBody);
            this.Controls.Add(this.LblTokens);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.BtnDefault);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BulkEmailTemplate_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bulk Email Template";
            ((System.ComponentModel.ISupportInitialize)(this.TxtSubject.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtBody.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblSubject;
        private DevExpress.XtraEditors.TextEdit TxtSubject;
        private DevExpress.XtraEditors.LabelControl LblBody;
        private DevExpress.XtraEditors.MemoEdit TxtBody;
        private DevExpress.XtraEditors.LabelControl LblTokens;
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnDefault;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
