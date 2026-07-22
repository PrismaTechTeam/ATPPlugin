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
            this.ChkIncludeLate = new DevExpress.XtraEditors.CheckEdit();
            this.ChkAutoFetch = new DevExpress.XtraEditors.CheckEdit();
            this.LblCutoffDay = new DevExpress.XtraEditors.LabelControl();
            this.CmbCutoffDay = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblCutoffTime = new DevExpress.XtraEditors.LabelControl();
            this.TimeCutoff = new DevExpress.XtraEditors.TimeEdit();
            this.BtnClearStaging = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOk = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeLate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkAutoFetch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbCutoffDay.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeCutoff.Properties)).BeginInit();
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
            // ChkIncludeLate
            //
            this.ChkIncludeLate.Location = new System.Drawing.Point(20, 71);
            this.ChkIncludeLate.Name = "ChkIncludeLate";
            this.ChkIncludeLate.Properties.Caption = "Include readings audited AFTER the billing day (RED Last Audit Date)";
            this.ChkIncludeLate.Size = new System.Drawing.Size(440, 20);
            this.ChkIncludeLate.TabIndex = 2;
            //
            // ChkAutoFetch
            //
            this.ChkAutoFetch.Location = new System.Drawing.Point(20, 103);
            this.ChkAutoFetch.Name = "ChkAutoFetch";
            this.ChkAutoFetch.Properties.Caption = "AUTO-FETCH on each billing day (snapshot + LOCK, no later override)";
            this.ChkAutoFetch.Size = new System.Drawing.Size(440, 20);
            this.ChkAutoFetch.TabIndex = 3;
            //
            // LblCutoffDay
            //
            this.LblCutoffDay.Location = new System.Drawing.Point(40, 132);
            this.LblCutoffDay.Name = "LblCutoffDay";
            this.LblCutoffDay.Size = new System.Drawing.Size(80, 14);
            this.LblCutoffDay.TabIndex = 4;
            this.LblCutoffDay.Text = "Reading cutoff:";
            //
            // CmbCutoffDay
            //
            this.CmbCutoffDay.Location = new System.Drawing.Point(140, 129);
            this.CmbCutoffDay.Name = "CmbCutoffDay";
            this.CmbCutoffDay.Properties.Items.AddRange(new object[] {
            "Day BEFORE billing day",
            "ON the billing day"});
            this.CmbCutoffDay.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbCutoffDay.Size = new System.Drawing.Size(180, 20);
            this.CmbCutoffDay.TabIndex = 5;
            //
            // LblCutoffTime
            //
            this.LblCutoffTime.Location = new System.Drawing.Point(332, 132);
            this.LblCutoffTime.Name = "LblCutoffTime";
            this.LblCutoffTime.Size = new System.Drawing.Size(20, 14);
            this.LblCutoffTime.TabIndex = 6;
            this.LblCutoffTime.Text = "at";
            //
            // TimeCutoff
            //
            this.TimeCutoff.EditValue = new System.DateTime(2000, 1, 1, 23, 59, 0, 0);
            this.TimeCutoff.Location = new System.Drawing.Point(358, 129);
            this.TimeCutoff.Name = "TimeCutoff";
            this.TimeCutoff.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.TimeCutoff.Properties.DisplayFormat.FormatString = "HH:mm";
            this.TimeCutoff.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.TimeCutoff.Properties.EditFormat.FormatString = "HH:mm";
            this.TimeCutoff.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.TimeCutoff.Properties.Mask.EditMask = "HH:mm";
            this.TimeCutoff.Size = new System.Drawing.Size(80, 20);
            this.TimeCutoff.TabIndex = 7;
            //
            // BtnClearStaging
            //
            this.BtnClearStaging.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.BtnClearStaging.Appearance.Options.UseForeColor = true;
            this.BtnClearStaging.Location = new System.Drawing.Point(20, 165);
            this.BtnClearStaging.Name = "BtnClearStaging";
            this.BtnClearStaging.Size = new System.Drawing.Size(150, 28);
            this.BtnClearStaging.TabIndex = 8;
            this.BtnClearStaging.Text = "Clear Staged Readings…";
            this.BtnClearStaging.Click += new System.EventHandler(this.BtnClearStaging_Click);
            //
            // BtnOk
            //
            this.BtnOk.Location = new System.Drawing.Point(180, 165);
            this.BtnOk.Name = "BtnOk";
            this.BtnOk.Size = new System.Drawing.Size(80, 28);
            this.BtnOk.TabIndex = 2;
            this.BtnOk.Text = "OK";
            this.BtnOk.Click += new System.EventHandler(this.BtnOk_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(268, 165);
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
            this.ClientSize = new System.Drawing.Size(480, 210);
            this.Controls.Add(this.BtnClearStaging);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnOk);
            this.Controls.Add(this.TimeCutoff);
            this.Controls.Add(this.LblCutoffTime);
            this.Controls.Add(this.CmbCutoffDay);
            this.Controls.Add(this.LblCutoffDay);
            this.Controls.Add(this.ChkAutoFetch);
            this.Controls.Add(this.ChkIncludeLate);
            this.Controls.Add(this.ChkIncludeExpired);
            this.Controls.Add(this.LblHint);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MeterReadingSetting_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meter Reading Settings";
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeExpired.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkIncludeLate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkAutoFetch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbCutoffDay.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeCutoff.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeExpired;
        private DevExpress.XtraEditors.CheckEdit ChkIncludeLate;
        private DevExpress.XtraEditors.CheckEdit ChkAutoFetch;
        private DevExpress.XtraEditors.LabelControl LblCutoffDay;
        private DevExpress.XtraEditors.ComboBoxEdit CmbCutoffDay;
        private DevExpress.XtraEditors.LabelControl LblCutoffTime;
        private DevExpress.XtraEditors.TimeEdit TimeCutoff;
        private DevExpress.XtraEditors.SimpleButton BtnClearStaging;
        private DevExpress.XtraEditors.SimpleButton BtnOk;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
