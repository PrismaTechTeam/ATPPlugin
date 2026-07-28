namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class CommitConfig_Form
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
            this.LblMeter = new DevExpress.XtraEditors.LabelControl();
            this.LblAmount = new DevExpress.XtraEditors.LabelControl();
            this.LblScope = new DevExpress.XtraEditors.LabelControl();
            this.CmbScope = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.CmbScope.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // LblMeter
            //
            this.LblMeter.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblMeter.Appearance.Options.UseFont = true;
            this.LblMeter.Location = new System.Drawing.Point(14, 12);
            this.LblMeter.Name = "LblMeter";
            this.LblMeter.Size = new System.Drawing.Size(60, 13);
            this.LblMeter.Text = "MIN meter:";
            //
            // LblAmount
            //
            this.LblAmount.Location = new System.Drawing.Point(14, 40);
            this.LblAmount.Name = "LblAmount";
            this.LblAmount.Size = new System.Drawing.Size(240, 13);
            this.LblAmount.Text = "Committed minimum:";
            //
            // LblScope
            //
            this.LblScope.Location = new System.Drawing.Point(14, 70);
            this.LblScope.Name = "LblScope";
            this.LblScope.Size = new System.Drawing.Size(130, 13);
            this.LblScope.Text = "Count print charges from";
            //
            // CmbScope
            //
            this.CmbScope.Location = new System.Drawing.Point(160, 67);
            this.CmbScope.Name = "CmbScope";
            this.CmbScope.Properties.Items.AddRange(new object[] { "BK + CL usage", "BK only", "CL only" });
            this.CmbScope.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbScope.Size = new System.Drawing.Size(140, 20);
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.Gray;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Location = new System.Drawing.Point(14, 98);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(360, 13);
            this.LblHint.Text = "At Generate the MIN line bills the TOP-UP: charges 270, committed 300 = bills 30.";
            //
            // BtnOK
            //
            this.BtnOK.Location = new System.Drawing.Point(200, 126);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(90, 28);
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(298, 126);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 28);
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // CommitConfig_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 166);
            this.Controls.Add(this.LblMeter);
            this.Controls.Add(this.LblAmount);
            this.Controls.Add(this.LblScope);
            this.Controls.Add(this.CmbScope);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CommitConfig_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Committed Minimum Configuration";
            ((System.ComponentModel.ISupportInitialize)(this.CmbScope.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblMeter;
        private DevExpress.XtraEditors.LabelControl LblAmount;
        private DevExpress.XtraEditors.LabelControl LblScope;
        private DevExpress.XtraEditors.ComboBoxEdit CmbScope;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
