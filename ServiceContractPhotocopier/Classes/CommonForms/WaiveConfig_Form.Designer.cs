namespace ServiceContractPhotocopier.Classes.CommonForms
{
    partial class WaiveConfig_Form
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
            this.ChkFirstN = new DevExpress.XtraEditors.CheckEdit();
            this.SpnFirstN = new DevExpress.XtraEditors.SpinEdit();
            this.LblFirstNUnit = new DevExpress.XtraEditors.LabelControl();
            this.ChkTarget = new DevExpress.XtraEditors.CheckEdit();
            this.LblTarget = new DevExpress.XtraEditors.LabelControl();
            this.SpnTarget = new DevExpress.XtraEditors.SpinEdit();
            this.LblPartialTh = new DevExpress.XtraEditors.LabelControl();
            this.SpnPartialTh = new DevExpress.XtraEditors.SpinEdit();
            this.LblPartialAmt = new DevExpress.XtraEditors.LabelControl();
            this.SpnPartialAmt = new DevExpress.XtraEditors.SpinEdit();
            this.LblScope = new DevExpress.XtraEditors.LabelControl();
            this.CmbScope = new DevExpress.XtraEditors.ComboBoxEdit();
            this.LblAlways = new DevExpress.XtraEditors.LabelControl();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.ChkFirstN.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFirstN.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkTarget.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnTarget.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnPartialTh.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnPartialAmt.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbScope.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // LblMeter
            //
            this.LblMeter.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblMeter.Appearance.Options.UseFont = true;
            this.LblMeter.Location = new System.Drawing.Point(14, 12);
            this.LblMeter.Name = "LblMeter";
            this.LblMeter.Size = new System.Drawing.Size(70, 13);
            this.LblMeter.Text = "Waive meter:";
            //
            // ChkFirstN
            //
            this.ChkFirstN.Location = new System.Drawing.Point(12, 40);
            this.ChkFirstN.Name = "ChkFirstN";
            this.ChkFirstN.Properties.Caption = "Always waive first N months:";
            this.ChkFirstN.Size = new System.Drawing.Size(180, 20);
            //
            // SpnFirstN
            //
            this.SpnFirstN.EditValue = new decimal(new int[] { 12, 0, 0, 0 });
            this.SpnFirstN.Location = new System.Drawing.Point(200, 40);
            this.SpnFirstN.Name = "SpnFirstN";
            this.SpnFirstN.Properties.IsFloatValue = false;
            this.SpnFirstN.Properties.MaxValue = new decimal(new int[] { 240, 0, 0, 0 });
            this.SpnFirstN.Properties.MinValue = new decimal(new int[] { 1, 0, 0, 0 });
            this.SpnFirstN.Size = new System.Drawing.Size(70, 20);
            //
            // LblFirstNUnit
            //
            this.LblFirstNUnit.Location = new System.Drawing.Point(276, 43);
            this.LblFirstNUnit.Name = "LblFirstNUnit";
            this.LblFirstNUnit.Size = new System.Drawing.Size(36, 13);
            this.LblFirstNUnit.Text = "month(s)";
            //
            // ChkTarget
            //
            this.ChkTarget.Location = new System.Drawing.Point(12, 72);
            this.ChkTarget.Name = "ChkTarget";
            this.ChkTarget.Properties.Caption = "Waive when print charges hit the target (after the free window):";
            this.ChkTarget.Size = new System.Drawing.Size(370, 20);
            //
            // LblTarget
            //
            this.LblTarget.Location = new System.Drawing.Point(32, 101);
            this.LblTarget.Name = "LblTarget";
            this.LblTarget.Size = new System.Drawing.Size(60, 13);
            this.LblTarget.Text = "Target (RM)";
            //
            // SpnTarget
            //
            this.SpnTarget.EditValue = new decimal(new int[] { 500, 0, 0, 0 });
            this.SpnTarget.Location = new System.Drawing.Point(140, 98);
            this.SpnTarget.Name = "SpnTarget";
            this.SpnTarget.Properties.DisplayFormat.FormatString = "n2";
            this.SpnTarget.Properties.MaxValue = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.SpnTarget.Size = new System.Drawing.Size(100, 20);
            //
            // LblPartialTh
            //
            this.LblPartialTh.Location = new System.Drawing.Point(32, 129);
            this.LblPartialTh.Name = "LblPartialTh";
            this.LblPartialTh.Size = new System.Drawing.Size(90, 13);
            this.LblPartialTh.Text = "Partial: hit (RM)";
            //
            // SpnPartialTh
            //
            this.SpnPartialTh.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnPartialTh.Location = new System.Drawing.Point(140, 126);
            this.SpnPartialTh.Name = "SpnPartialTh";
            this.SpnPartialTh.Properties.DisplayFormat.FormatString = "n2";
            this.SpnPartialTh.Properties.MaxValue = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.SpnPartialTh.Size = new System.Drawing.Size(100, 20);
            //
            // LblPartialAmt
            //
            this.LblPartialAmt.Location = new System.Drawing.Point(32, 157);
            this.LblPartialAmt.Name = "LblPartialAmt";
            this.LblPartialAmt.Size = new System.Drawing.Size(90, 13);
            this.LblPartialAmt.Text = "-> waive (RM)";
            //
            // SpnPartialAmt
            //
            this.SpnPartialAmt.EditValue = new decimal(new int[] { 0, 0, 0, 0 });
            this.SpnPartialAmt.Location = new System.Drawing.Point(140, 154);
            this.SpnPartialAmt.Name = "SpnPartialAmt";
            this.SpnPartialAmt.Properties.DisplayFormat.FormatString = "n2";
            this.SpnPartialAmt.Properties.MaxValue = new decimal(new int[] { 100000000, 0, 0, 0 });
            this.SpnPartialAmt.Size = new System.Drawing.Size(100, 20);
            //
            // LblScope
            //
            this.LblScope.Location = new System.Drawing.Point(32, 185);
            this.LblScope.Name = "LblScope";
            this.LblScope.Size = new System.Drawing.Size(85, 13);
            this.LblScope.Text = "Count usage from";
            //
            // CmbScope
            //
            this.CmbScope.Location = new System.Drawing.Point(140, 182);
            this.CmbScope.Name = "CmbScope";
            this.CmbScope.Properties.Items.AddRange(new object[] { "BK + CL usage", "BK only", "CL only" });
            this.CmbScope.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbScope.Size = new System.Drawing.Size(140, 20);
            //
            // LblAlways
            //
            this.LblAlways.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(96)))), ((int)(((byte)(0)))));
            this.LblAlways.Appearance.Options.UseForeColor = true;
            this.LblAlways.Location = new System.Drawing.Point(14, 214);
            this.LblAlways.Name = "LblAlways";
            this.LblAlways.Size = new System.Drawing.Size(380, 13);
            this.LblAlways.Text = "Nothing ticked = ALWAYS waive — the contra bills every month.";
            //
            // BtnOK
            //
            this.BtnOK.Location = new System.Drawing.Point(200, 242);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(90, 28);
            this.BtnOK.Text = "OK";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new System.Drawing.Point(298, 242);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 28);
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // WaiveConfig_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 282);
            this.Controls.Add(this.LblMeter);
            this.Controls.Add(this.ChkFirstN);
            this.Controls.Add(this.SpnFirstN);
            this.Controls.Add(this.LblFirstNUnit);
            this.Controls.Add(this.ChkTarget);
            this.Controls.Add(this.LblTarget);
            this.Controls.Add(this.SpnTarget);
            this.Controls.Add(this.LblPartialTh);
            this.Controls.Add(this.SpnPartialTh);
            this.Controls.Add(this.LblPartialAmt);
            this.Controls.Add(this.SpnPartialAmt);
            this.Controls.Add(this.LblScope);
            this.Controls.Add(this.CmbScope);
            this.Controls.Add(this.LblAlways);
            this.Controls.Add(this.BtnOK);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WaiveConfig_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Waive Configuration";
            ((System.ComponentModel.ISupportInitialize)(this.ChkFirstN.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFirstN.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkTarget.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnTarget.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnPartialTh.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnPartialAmt.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbScope.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblMeter;
        private DevExpress.XtraEditors.CheckEdit ChkFirstN;
        private DevExpress.XtraEditors.SpinEdit SpnFirstN;
        private DevExpress.XtraEditors.LabelControl LblFirstNUnit;
        private DevExpress.XtraEditors.CheckEdit ChkTarget;
        private DevExpress.XtraEditors.LabelControl LblTarget;
        private DevExpress.XtraEditors.SpinEdit SpnTarget;
        private DevExpress.XtraEditors.LabelControl LblPartialTh;
        private DevExpress.XtraEditors.SpinEdit SpnPartialTh;
        private DevExpress.XtraEditors.LabelControl LblPartialAmt;
        private DevExpress.XtraEditors.SpinEdit SpnPartialAmt;
        private DevExpress.XtraEditors.LabelControl LblScope;
        private DevExpress.XtraEditors.ComboBoxEdit CmbScope;
        private DevExpress.XtraEditors.LabelControl LblAlways;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
