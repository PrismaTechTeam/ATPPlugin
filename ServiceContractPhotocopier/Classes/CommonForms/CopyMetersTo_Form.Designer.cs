namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class CopyMetersTo_Form
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
            this.LblMeters = new DevExpress.XtraEditors.LabelControl();
            this.LstMeters = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.LblTargets = new DevExpress.XtraEditors.LabelControl();
            this.LstTargets = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.BtnAllTargets = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.LstMeters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LstTargets)).BeginInit();
            this.SuspendLayout();
            //
            // LblMeters
            //
            this.LblMeters.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblMeters.Appearance.Options.UseFont = true;
            this.LblMeters.Location = new System.Drawing.Point(12, 10);
            this.LblMeters.Name = "LblMeters";
            this.LblMeters.Size = new System.Drawing.Size(120, 13);
            this.LblMeters.Text = "Copy WHICH meters:";
            //
            // LstMeters
            //
            this.LstMeters.CheckOnClick = true;
            this.LstMeters.Location = new System.Drawing.Point(12, 30);
            this.LstMeters.Name = "LstMeters";
            this.LstMeters.Size = new System.Drawing.Size(250, 220);
            //
            // LblTargets
            //
            this.LblTargets.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblTargets.Appearance.Options.UseFont = true;
            this.LblTargets.Location = new System.Drawing.Point(276, 10);
            this.LblTargets.Name = "LblTargets";
            this.LblTargets.Size = new System.Drawing.Size(130, 13);
            this.LblTargets.Text = "To WHICH machines:";
            //
            // LstTargets
            //
            this.LstTargets.CheckOnClick = true;
            this.LstTargets.Location = new System.Drawing.Point(276, 30);
            this.LstTargets.Name = "LstTargets";
            this.LstTargets.Size = new System.Drawing.Size(250, 220);
            //
            // BtnAllTargets
            //
            this.BtnAllTargets.Location = new System.Drawing.Point(276, 256);
            this.BtnAllTargets.Name = "BtnAllTargets";
            this.BtnAllTargets.Size = new System.Drawing.Size(90, 24);
            this.BtnAllTargets.Text = "Tick all";
            this.BtnAllTargets.Click += new System.EventHandler(this.BtnAllTargets_Click);
            //
            // BtnOK
            //
            this.BtnOK.Location = new System.Drawing.Point(340, 292);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(90, 28);
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(436, 292);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 28);
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // CopyMetersTo_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(538, 330);
            this.Controls.Add(this.LblMeters);
            this.Controls.Add(this.LstMeters);
            this.Controls.Add(this.LblTargets);
            this.Controls.Add(this.LstTargets);
            this.Controls.Add(this.BtnAllTargets);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CopyMetersTo_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Copy Meters To";
            ((System.ComponentModel.ISupportInitialize)(this.LstMeters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LstTargets)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblMeters;
        private DevExpress.XtraEditors.CheckedListBoxControl LstMeters;
        private DevExpress.XtraEditors.LabelControl LblTargets;
        private DevExpress.XtraEditors.CheckedListBoxControl LstTargets;
        private DevExpress.XtraEditors.SimpleButton BtnAllTargets;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
