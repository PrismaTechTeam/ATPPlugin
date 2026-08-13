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
            this.GrpShow = new DevExpress.XtraEditors.GroupControl();
            this.ChkIncludeExpired = new DevExpress.XtraEditors.CheckEdit();
            this.ChkIncludeInactive = new DevExpress.XtraEditors.CheckEdit();
            this.ChkIncludeLate = new DevExpress.XtraEditors.CheckEdit();
            this.GrpAutoFetch = new DevExpress.XtraEditors.GroupControl();
            this.ChkAutoFetch = new DevExpress.XtraEditors.CheckEdit();
            this.LblCutoffDay = new DevExpress.XtraEditors.LabelControl();
            this.CmbCutoffDay = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblCutoffTime = new DevExpress.XtraEditors.LabelControl();
            this.TimeCutoff = new DevExpress.XtraEditors.TimeEdit();
            this.GrpInvoice = new DevExpress.XtraEditors.GroupControl();
            this.LblGrouping = new DevExpress.XtraEditors.LabelControl();
            this.CmbGrouping = new DevExpress.XtraEditors.ComboBoxEdit();
            this.ChkGroupGuard = new DevExpress.XtraEditors.CheckEdit();
            this.ChkGroupRental = new DevExpress.XtraEditors.CheckEdit();
            this.GrpMaintenance = new DevExpress.XtraEditors.GroupControl();
            this.BtnClearStaging = new DevExpress.XtraEditors.SimpleButton();
            this.LblClearHint = new DevExpress.XtraEditors.LabelControl();
            this.BtnOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.GrpShow)).BeginInit();
            this.GrpShow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpAutoFetch)).BeginInit();
            this.GrpAutoFetch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoice)).BeginInit();
            this.GrpInvoice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMaintenance)).BeginInit();
            this.GrpMaintenance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeLate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkAutoFetch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbCutoffDay.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeCutoff.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbGrouping.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkGroupGuard.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkGroupRental.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(12, 12);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(536, 16);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Choose what the Meter Reading list shows, and how invoices are generated.";
            //
            // GrpShow
            //
            this.GrpShow.Controls.Add(this.ChkIncludeExpired);
            this.GrpShow.Controls.Add(this.ChkIncludeInactive);
            this.GrpShow.Controls.Add(this.ChkIncludeLate);
            this.GrpShow.Location = new System.Drawing.Point(12, 36);
            this.GrpShow.Name = "GrpShow";
            this.GrpShow.Size = new System.Drawing.Size(536, 116);
            this.GrpShow.TabIndex = 1;
            this.GrpShow.Text = "What the list shows";
            //
            // ChkIncludeExpired
            //
            this.ChkIncludeExpired.Location = new System.Drawing.Point(14, 28);
            this.ChkIncludeExpired.Name = "ChkIncludeExpired";
            this.ChkIncludeExpired.Properties.Caption = "Show expired service items";
            this.ChkIncludeExpired.Size = new System.Drawing.Size(508, 22);
            this.ChkIncludeExpired.TabIndex = 0;
            //
            // ChkIncludeInactive
            //
            this.ChkIncludeInactive.Location = new System.Drawing.Point(14, 56);
            this.ChkIncludeInactive.Name = "ChkIncludeInactive";
            this.ChkIncludeInactive.Properties.Caption = "Show inactive contracts and items (leftover readings still billable)";
            this.ChkIncludeInactive.Size = new System.Drawing.Size(508, 22);
            this.ChkIncludeInactive.TabIndex = 1;
            //
            // ChkIncludeLate
            //
            this.ChkIncludeLate.Location = new System.Drawing.Point(14, 84);
            this.ChkIncludeLate.Name = "ChkIncludeLate";
            this.ChkIncludeLate.Properties.Caption = "Show late readings - audited after the billing day (shown in red)";
            this.ChkIncludeLate.Size = new System.Drawing.Size(508, 22);
            this.ChkIncludeLate.TabIndex = 2;
            //
            // GrpAutoFetch
            //
            this.GrpAutoFetch.Controls.Add(this.ChkAutoFetch);
            this.GrpAutoFetch.Controls.Add(this.LblCutoffDay);
            this.GrpAutoFetch.Controls.Add(this.CmbCutoffDay);
            this.GrpAutoFetch.Controls.Add(this.LblCutoffTime);
            this.GrpAutoFetch.Controls.Add(this.TimeCutoff);
            this.GrpAutoFetch.Location = new System.Drawing.Point(12, 162);
            this.GrpAutoFetch.Name = "GrpAutoFetch";
            this.GrpAutoFetch.Size = new System.Drawing.Size(536, 92);
            this.GrpAutoFetch.TabIndex = 2;
            this.GrpAutoFetch.Text = "Auto-fetch";
            //
            // ChkAutoFetch
            //
            this.ChkAutoFetch.Location = new System.Drawing.Point(14, 28);
            this.ChkAutoFetch.Name = "ChkAutoFetch";
            this.ChkAutoFetch.Properties.Caption = "Auto-fetch on the billing day, then lock the snapshot";
            this.ChkAutoFetch.Size = new System.Drawing.Size(508, 22);
            this.ChkAutoFetch.TabIndex = 0;
            //
            // LblCutoffDay
            //
            this.LblCutoffDay.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblCutoffDay.Location = new System.Drawing.Point(36, 62);
            this.LblCutoffDay.Name = "LblCutoffDay";
            this.LblCutoffDay.Size = new System.Drawing.Size(95, 16);
            this.LblCutoffDay.TabIndex = 1;
            this.LblCutoffDay.Text = "Reading cutoff:";
            //
            // CmbCutoffDay
            //
            this.CmbCutoffDay.Location = new System.Drawing.Point(140, 59);
            this.CmbCutoffDay.Name = "CmbCutoffDay";
            this.CmbCutoffDay.Properties.Items.AddRange(new object[] {
            "Day before billing day",
            "On the billing day"});
            this.CmbCutoffDay.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbCutoffDay.Size = new System.Drawing.Size(190, 22);
            this.CmbCutoffDay.TabIndex = 2;
            //
            // LblCutoffTime
            //
            this.LblCutoffTime.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblCutoffTime.Location = new System.Drawing.Point(342, 62);
            this.LblCutoffTime.Name = "LblCutoffTime";
            this.LblCutoffTime.Size = new System.Drawing.Size(20, 16);
            this.LblCutoffTime.TabIndex = 3;
            this.LblCutoffTime.Text = "at";
            //
            // TimeCutoff
            //
            this.TimeCutoff.EditValue = new System.DateTime(2000, 1, 1, 23, 59, 0, 0);
            this.TimeCutoff.Location = new System.Drawing.Point(368, 59);
            this.TimeCutoff.Name = "TimeCutoff";
            this.TimeCutoff.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.TimeCutoff.Properties.DisplayFormat.FormatString = "HH:mm";
            this.TimeCutoff.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.TimeCutoff.Properties.EditFormat.FormatString = "HH:mm";
            this.TimeCutoff.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.TimeCutoff.Properties.Mask.EditMask = "HH:mm";
            this.TimeCutoff.Size = new System.Drawing.Size(90, 22);
            this.TimeCutoff.TabIndex = 4;
            //
            // GrpInvoice
            //
            this.GrpInvoice.Controls.Add(this.LblGrouping);
            this.GrpInvoice.Controls.Add(this.CmbGrouping);
            this.GrpInvoice.Controls.Add(this.ChkGroupGuard);
            this.GrpInvoice.Controls.Add(this.ChkGroupRental);
            this.GrpInvoice.Location = new System.Drawing.Point(12, 264);
            this.GrpInvoice.Name = "GrpInvoice";
            this.GrpInvoice.Size = new System.Drawing.Size(536, 120);
            this.GrpInvoice.TabIndex = 3;
            this.GrpInvoice.Text = "Invoicing and generation rules";
            //
            // LblGrouping
            //
            this.LblGrouping.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblGrouping.Location = new System.Drawing.Point(14, 31);
            this.LblGrouping.Name = "LblGrouping";
            this.LblGrouping.Size = new System.Drawing.Size(102, 16);
            this.LblGrouping.TabIndex = 0;
            this.LblGrouping.Text = "Invoice grouping:";
            //
            // CmbGrouping
            //
            this.CmbGrouping.Location = new System.Drawing.Point(140, 28);
            this.CmbGrouping.Name = "CmbGrouping";
            this.CmbGrouping.Properties.Items.AddRange(new object[] {
            "Follow each contract\'s Billing Mode",
            "Always group same debtor into one invoice",
            "Always one invoice per machine"});
            this.CmbGrouping.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbGrouping.Size = new System.Drawing.Size(330, 22);
            this.CmbGrouping.TabIndex = 1;
            //
            // ChkGroupGuard
            //
            this.ChkGroupGuard.Location = new System.Drawing.Point(14, 60);
            this.ChkGroupGuard.Name = "ChkGroupGuard";
            this.ChkGroupGuard.Properties.Caption = "Block Generate if any machine in a group has no reading";
            this.ChkGroupGuard.Size = new System.Drawing.Size(508, 22);
            this.ChkGroupGuard.TabIndex = 2;
            //
            // ChkGroupRental
            //
            this.ChkGroupRental.Location = new System.Drawing.Point(14, 88);
            this.ChkGroupRental.Name = "ChkGroupRental";
            this.ChkGroupRental.Properties.Caption = "Group rental by meter type - one line x quantity, not one line each";
            this.ChkGroupRental.Size = new System.Drawing.Size(508, 22);
            this.ChkGroupRental.TabIndex = 3;
            //
            // GrpMaintenance
            //
            this.GrpMaintenance.Controls.Add(this.BtnClearStaging);
            this.GrpMaintenance.Controls.Add(this.LblClearHint);
            this.GrpMaintenance.Location = new System.Drawing.Point(12, 394);
            this.GrpMaintenance.Name = "GrpMaintenance";
            this.GrpMaintenance.Size = new System.Drawing.Size(536, 88);
            this.GrpMaintenance.TabIndex = 4;
            this.GrpMaintenance.Text = "Maintenance";
            //
            // BtnClearStaging
            //
            this.BtnClearStaging.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnClearStaging.Appearance.Options.UseForeColor = true;
            this.BtnClearStaging.Location = new System.Drawing.Point(14, 28);
            this.BtnClearStaging.Name = "BtnClearStaging";
            this.BtnClearStaging.Size = new System.Drawing.Size(176, 28);
            this.BtnClearStaging.TabIndex = 0;
            this.BtnClearStaging.Text = "Clear Staged Readings...";
            this.BtnClearStaging.Click += new System.EventHandler(this.BtnClearStaging_Click);
            //
            // LblClearHint
            //
            this.LblClearHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblClearHint.Location = new System.Drawing.Point(14, 62);
            this.LblClearHint.Name = "LblClearHint";
            this.LblClearHint.Size = new System.Drawing.Size(508, 16);
            this.LblClearHint.TabIndex = 1;
            this.LblClearHint.Text = "Deletes this period\'s staged readings. Invoiced and locked rows are kept.";
            //
            // BtnOk
            //
            this.BtnOk.Location = new System.Drawing.Point(364, 496);
            this.BtnOk.Name = "BtnOk";
            this.BtnOk.Size = new System.Drawing.Size(88, 28);
            this.BtnOk.TabIndex = 5;
            this.BtnOk.Text = "OK";
            this.BtnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(460, 496);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(88, 28);
            this.BtnCancel.TabIndex = 6;
            this.BtnCancel.Text = "Cancel";
            //
            // MeterReadingSetting_Form
            //
            this.AcceptButton = this.BtnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(560, 536);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnOk);
            this.Controls.Add(this.GrpMaintenance);
            this.Controls.Add(this.GrpInvoice);
            this.Controls.Add(this.GrpAutoFetch);
            this.Controls.Add(this.GrpShow);
            this.Controls.Add(this.LblHint);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MeterReadingSetting_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meter Reading Settings";
            ((System.ComponentModel.ISupportInitialize)(this.ChkGroupRental.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkGroupGuard.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbGrouping.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeCutoff.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbCutoffDay.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkAutoFetch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeLate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).EndInit();
            this.GrpMaintenance.ResumeLayout(false);
            this.GrpMaintenance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMaintenance)).EndInit();
            this.GrpInvoice.ResumeLayout(false);
            this.GrpInvoice.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoice)).EndInit();
            this.GrpAutoFetch.ResumeLayout(false);
            this.GrpAutoFetch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpAutoFetch)).EndInit();
            this.GrpShow.ResumeLayout(false);
            this.GrpShow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpShow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.GroupControl GrpShow;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeExpired;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeInactive;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeLate;
        private DevExpress.XtraEditors.GroupControl GrpAutoFetch;
        private DevExpress.XtraEditors.CheckEdit ChkAutoFetch;
        private DevExpress.XtraEditors.LabelControl LblCutoffDay;
        private DevExpress.XtraEditors.ComboBoxEdit CmbCutoffDay;
        private DevExpress.XtraEditors.LabelControl LblCutoffTime;
        private DevExpress.XtraEditors.TimeEdit TimeCutoff;
        private DevExpress.XtraEditors.GroupControl GrpInvoice;
        private DevExpress.XtraEditors.LabelControl LblGrouping;
        private DevExpress.XtraEditors.ComboBoxEdit CmbGrouping;
        private DevExpress.XtraEditors.CheckEdit ChkGroupGuard;
        private DevExpress.XtraEditors.CheckEdit ChkGroupRental;
        private DevExpress.XtraEditors.GroupControl GrpMaintenance;
        private DevExpress.XtraEditors.SimpleButton BtnClearStaging;
        private DevExpress.XtraEditors.LabelControl LblClearHint;
        private DevExpress.XtraEditors.SimpleButton BtnOk;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
