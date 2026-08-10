namespace ServiceContractPhotocopier
{
    partial class MeterCN_Form
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
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.PanelHeader = new DevExpress.XtraEditors.PanelControl();
            this.LblInvoice = new DevExpress.XtraEditors.LabelControl();
            this.LblInvoiceVal = new DevExpress.XtraEditors.LabelControl();
            this.LblExistingCN = new DevExpress.XtraEditors.LabelControl();
            this.LblCNDate = new DevExpress.XtraEditors.LabelControl();
            this.DtCNDate = new DevExpress.XtraEditors.DateEdit();
            this.LblReason = new DevExpress.XtraEditors.LabelControl();
            this.TxtReason = new DevExpress.XtraEditors.TextEdit();
            this.ChkKnockOff = new DevExpress.XtraEditors.CheckEdit();
            this.GridLines = new DevExpress.XtraGrid.GridControl();
            this.GridViewLines = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMachine = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMeter = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBilledReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMinReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMaxReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBilledUsage = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColBilledCharge = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCorrectReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCreditCopies = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCreditAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.RepoNum = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.RepoNum2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.LblTotal = new DevExpress.XtraEditors.LabelControl();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnGenerate = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelHeader)).BeginInit();
            this.PanelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DtCNDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtCNDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtReason.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkKnockOff.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // LblHint
            //
            this.LblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LblHint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.LblHint.Appearance.Options.UseTextOptions = true;
            this.LblHint.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblHint.Location = new System.Drawing.Point(12, 8);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(956, 30);
            this.LblHint.TabIndex = 0;
            this.LblHint.Text = "Key the CORRECT reading per meter. Credit Copies = Last Reading - Correct; a REAL " +
    "Sales Credit Note is created and the corrected reading becomes this machine\'s La" +
    "st Reading for future billing (the latest CN always wins, whichever invoice it co" +
    "rrects).";
            //
            // PanelHeader
            //
            this.PanelHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PanelHeader.Controls.Add(this.LblInvoice);
            this.PanelHeader.Controls.Add(this.LblInvoiceVal);
            this.PanelHeader.Controls.Add(this.LblExistingCN);
            this.PanelHeader.Controls.Add(this.LblCNDate);
            this.PanelHeader.Controls.Add(this.DtCNDate);
            this.PanelHeader.Controls.Add(this.LblReason);
            this.PanelHeader.Controls.Add(this.TxtReason);
            this.PanelHeader.Controls.Add(this.ChkKnockOff);
            this.PanelHeader.Location = new System.Drawing.Point(12, 42);
            this.PanelHeader.Name = "PanelHeader";
            this.PanelHeader.Size = new System.Drawing.Size(956, 78);
            this.PanelHeader.TabIndex = 1;
            //
            // LblInvoice
            //
            this.LblInvoice.Location = new System.Drawing.Point(10, 8);
            this.LblInvoice.Name = "LblInvoice";
            this.LblInvoice.Size = new System.Drawing.Size(41, 13);
            this.LblInvoice.TabIndex = 0;
            this.LblInvoice.Text = "Invoice:";
            //
            // LblInvoiceVal
            //
            this.LblInvoiceVal.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.LblInvoiceVal.Appearance.Options.UseFont = true;
            this.LblInvoiceVal.Location = new System.Drawing.Point(60, 8);
            this.LblInvoiceVal.Name = "LblInvoiceVal";
            this.LblInvoiceVal.Size = new System.Drawing.Size(4, 13);
            this.LblInvoiceVal.TabIndex = 1;
            //
            // LblExistingCN
            //
            this.LblExistingCN.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(120)))), ((int)(((byte)(0)))));
            this.LblExistingCN.Appearance.Options.UseForeColor = true;
            this.LblExistingCN.Location = new System.Drawing.Point(500, 8);
            this.LblExistingCN.Name = "LblExistingCN";
            this.LblExistingCN.Size = new System.Drawing.Size(4, 13);
            this.LblExistingCN.TabIndex = 2;
            this.LblExistingCN.Visible = false;
            //
            // LblCNDate
            //
            this.LblCNDate.Location = new System.Drawing.Point(10, 36);
            this.LblCNDate.Name = "LblCNDate";
            this.LblCNDate.Size = new System.Drawing.Size(46, 13);
            this.LblCNDate.TabIndex = 3;
            this.LblCNDate.Text = "CN Date:";
            //
            // DtCNDate
            //
            this.DtCNDate.EditValue = null;
            this.DtCNDate.Location = new System.Drawing.Point(60, 33);
            this.DtCNDate.Name = "DtCNDate";
            this.DtCNDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtCNDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtCNDate.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtCNDate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtCNDate.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtCNDate.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtCNDate.Size = new System.Drawing.Size(110, 20);
            this.DtCNDate.TabIndex = 4;
            //
            // LblReason
            //
            this.LblReason.Location = new System.Drawing.Point(190, 36);
            this.LblReason.Name = "LblReason";
            this.LblReason.Size = new System.Drawing.Size(42, 13);
            this.LblReason.TabIndex = 5;
            this.LblReason.Text = "Reason:";
            //
            // TxtReason
            //
            this.TxtReason.Location = new System.Drawing.Point(238, 33);
            this.TxtReason.Name = "TxtReason";
            this.TxtReason.Size = new System.Drawing.Size(330, 20);
            this.TxtReason.TabIndex = 6;
            //
            // ChkKnockOff
            //
            this.ChkKnockOff.EditValue = true;
            this.ChkKnockOff.Location = new System.Drawing.Point(586, 33);
            this.ChkKnockOff.Name = "ChkKnockOff";
            this.ChkKnockOff.Properties.Caption = "Knock off against this invoice";
            this.ChkKnockOff.Size = new System.Drawing.Size(220, 20);
            this.ChkKnockOff.TabIndex = 7;
            //
            // GridLines
            //
            this.GridLines.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GridLines.Location = new System.Drawing.Point(12, 126);
            this.GridLines.MainView = this.GridViewLines;
            this.GridLines.Name = "GridLines";
            this.GridLines.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.RepoNum,
            this.RepoNum2});
            this.GridLines.Size = new System.Drawing.Size(956, 356);
            this.GridLines.TabIndex = 2;
            this.GridLines.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewLines});
            //
            // GridViewLines
            //
            this.GridViewLines.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColMachine,
            this.ColSerial,
            this.ColMeter,
            this.ColBilledReading,
            this.ColMinReading,
            this.ColMaxReading,
            this.ColBilledUsage,
            this.ColRate,
            this.ColBilledCharge,
            this.ColCorrectReading,
            this.ColCreditCopies,
            this.ColCreditAmount});
            this.GridViewLines.GridControl = this.GridLines;
            this.GridViewLines.Name = "GridViewLines";
            this.GridViewLines.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            this.GridViewLines.OptionsView.ShowGroupPanel = false;
            //
            // ColMachine
            //
            this.ColMachine.Caption = "Machine";
            this.ColMachine.FieldName = "ServiceItemNo";
            this.ColMachine.Name = "ColMachine";
            this.ColMachine.OptionsColumn.AllowEdit = false;
            this.ColMachine.Visible = true;
            this.ColMachine.VisibleIndex = 0;
            this.ColMachine.Width = 120;
            //
            // ColSerial
            //
            this.ColSerial.Caption = "Serial";
            this.ColSerial.FieldName = "Serial";
            this.ColSerial.Name = "ColSerial";
            this.ColSerial.OptionsColumn.AllowEdit = false;
            this.ColSerial.Visible = true;
            this.ColSerial.VisibleIndex = 1;
            this.ColSerial.Width = 90;
            //
            // ColMeter
            //
            this.ColMeter.Caption = "Meter";
            this.ColMeter.FieldName = "MeterTypeName";
            this.ColMeter.Name = "ColMeter";
            this.ColMeter.OptionsColumn.AllowEdit = false;
            this.ColMeter.Visible = true;
            this.ColMeter.VisibleIndex = 2;
            this.ColMeter.Width = 190;
            //
            // ColBilledReading
            //
            this.ColBilledReading.Caption = "Last Reading";
            this.ColBilledReading.DisplayFormat.FormatString = "n0";
            this.ColBilledReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            // BaseReading, not the raw billed figure: once an earlier CN has moved this machine's
            // reading, the credit is measured from where the meter STANDS NOW - which is what the
            // agreed scenario's "Last Reading" column means.
            this.ColBilledReading.FieldName = "BaseReading";
            this.ColBilledReading.Name = "ColBilledReading";
            this.ColBilledReading.OptionsColumn.AllowEdit = false;
            this.ColBilledReading.Visible = true;
            this.ColBilledReading.VisibleIndex = 3;
            this.ColBilledReading.Width = 90;
            //
            // ColMinReading
            //
            this.ColMinReading.Caption = "Min Reading";
            this.ColMinReading.DisplayFormat.FormatString = "n0";
            this.ColMinReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            // The floor a CN on THIS invoice may correct down to - the reading it started from.
            // Going lower belongs to the earlier invoice, so the limit is shown, not just enforced.
            this.ColMinReading.FieldName = "LastReading";
            this.ColMinReading.Name = "ColMinReading";
            this.ColMinReading.OptionsColumn.AllowEdit = false;
            this.ColMinReading.ToolTip = "A CN on this invoice cannot correct below this reading.";
            this.ColMinReading.Visible = true;
            this.ColMinReading.VisibleIndex = 4;
            this.ColMinReading.Width = 90;
            //
            // ColMaxReading
            //
            this.ColMaxReading.Caption = "Max Reading";
            this.ColMaxReading.DisplayFormat.FormatString = "n0";
            this.ColMaxReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            // The ceiling: MIN(this invoice's Max meter, the machine's current reading). Shown so
            // the allowed window reads straight off the row - Min Reading .. Max Reading.
            this.ColMaxReading.FieldName = "MaxAllowed";
            this.ColMaxReading.Name = "ColMaxReading";
            this.ColMaxReading.OptionsColumn.AllowEdit = false;
            this.ColMaxReading.ToolTip = "A CN on this invoice cannot correct above this reading.";
            this.ColMaxReading.Visible = true;
            this.ColMaxReading.VisibleIndex = 5;
            this.ColMaxReading.Width = 90;
            //
            // ColBilledUsage
            //
            this.ColBilledUsage.Caption = "Billed Usage";
            this.ColBilledUsage.DisplayFormat.FormatString = "n0";
            this.ColBilledUsage.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColBilledUsage.FieldName = "BilledUsage";
            this.ColBilledUsage.Name = "ColBilledUsage";
            this.ColBilledUsage.OptionsColumn.AllowEdit = false;
            this.ColBilledUsage.Visible = true;
            this.ColBilledUsage.VisibleIndex = 6;
            this.ColBilledUsage.Width = 85;
            //
            // ColRate
            //
            this.ColRate.Caption = "Rate";
            this.ColRate.DisplayFormat.FormatString = "n4";
            this.ColRate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColRate.FieldName = "Rate";
            this.ColRate.Name = "ColRate";
            this.ColRate.OptionsColumn.AllowEdit = false;
            this.ColRate.Visible = true;
            this.ColRate.VisibleIndex = 7;
            this.ColRate.Width = 70;
            //
            // ColBilledCharge
            //
            this.ColBilledCharge.Caption = "Billed Charge";
            this.ColBilledCharge.DisplayFormat.FormatString = "n2";
            this.ColBilledCharge.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColBilledCharge.FieldName = "BilledCharge";
            this.ColBilledCharge.Name = "ColBilledCharge";
            this.ColBilledCharge.OptionsColumn.AllowEdit = false;
            this.ColBilledCharge.Visible = true;
            this.ColBilledCharge.VisibleIndex = 8;
            this.ColBilledCharge.Width = 85;
            //
            // ColCorrectReading
            //
            // THE input column: a clear yellow and bold text so it is obvious this is the one cell
            // the user fills in. Every other column on this row is read-only history.
            this.ColCorrectReading.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(241)))), ((int)(((byte)(118)))));
            this.ColCorrectReading.AppearanceCell.Options.UseBackColor = true;
            this.ColCorrectReading.AppearanceCell.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.ColCorrectReading.AppearanceCell.Options.UseFont = true;
            this.ColCorrectReading.AppearanceHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(241)))), ((int)(((byte)(118)))));
            this.ColCorrectReading.AppearanceHeader.Options.UseBackColor = true;
            this.ColCorrectReading.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.ColCorrectReading.AppearanceHeader.Options.UseFont = true;
            this.ColCorrectReading.Caption = "Correct Reading";
            this.ColCorrectReading.ColumnEdit = this.RepoNum;
            this.ColCorrectReading.DisplayFormat.FormatString = "n0";
            this.ColCorrectReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCorrectReading.FieldName = "CorrectReading";
            this.ColCorrectReading.Name = "ColCorrectReading";
            this.ColCorrectReading.Visible = true;
            this.ColCorrectReading.VisibleIndex = 9;
            this.ColCorrectReading.Width = 100;
            //
            // ColCreditCopies
            //
            this.ColCreditCopies.Caption = "Credit Copies";
            this.ColCreditCopies.DisplayFormat.FormatString = "n0";
            this.ColCreditCopies.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCreditCopies.FieldName = "CreditCopies";
            this.ColCreditCopies.Name = "ColCreditCopies";
            this.ColCreditCopies.OptionsColumn.AllowEdit = false;
            this.ColCreditCopies.Visible = true;
            this.ColCreditCopies.VisibleIndex = 10;
            this.ColCreditCopies.Width = 90;
            //
            // ColCreditAmount
            //
            this.ColCreditAmount.Caption = "Credit Amount";
            this.ColCreditAmount.ColumnEdit = this.RepoNum2;
            this.ColCreditAmount.DisplayFormat.FormatString = "n2";
            this.ColCreditAmount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCreditAmount.FieldName = "CreditAmount";
            this.ColCreditAmount.Name = "ColCreditAmount";
            this.ColCreditAmount.Visible = true;
            this.ColCreditAmount.VisibleIndex = 11;
            this.ColCreditAmount.Width = 95;
            //
            // RepoNum
            //
            this.RepoNum.AutoHeight = false;
            this.RepoNum.Mask.EditMask = "n0";
            this.RepoNum.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoNum.Name = "RepoNum";
            //
            // RepoNum2
            //
            this.RepoNum2.AutoHeight = false;
            this.RepoNum2.Mask.EditMask = "n2";
            this.RepoNum2.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.RepoNum2.Name = "RepoNum2";
            //
            // LblTotal
            //
            this.LblTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LblTotal.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.LblTotal.Appearance.Options.UseFont = true;
            this.LblTotal.Location = new System.Drawing.Point(12, 490);
            this.LblTotal.Name = "LblTotal";
            this.LblTotal.Size = new System.Drawing.Size(80, 15);
            this.LblTotal.TabIndex = 3;
            this.LblTotal.Text = "Credit total:  0.00";
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.BtnGenerate);
            this.PanelBottom.Controls.Add(this.BtnCancel);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 516);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(980, 44);
            this.PanelBottom.TabIndex = 4;
            //
            // BtnGenerate
            //
            this.BtnGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnGenerate.ImageOptions.ImageUri.Uri = "Apply;Size16x16";
            this.BtnGenerate.Location = new System.Drawing.Point(738, 9);
            this.BtnGenerate.Name = "BtnGenerate";
            this.BtnGenerate.Size = new System.Drawing.Size(130, 26);
            this.BtnGenerate.TabIndex = 0;
            this.BtnGenerate.Text = "Generate CN";
            this.BtnGenerate.Click += new System.EventHandler(this.BtnGenerate_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(878, 9);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(90, 26);
            this.BtnCancel.TabIndex = 1;
            this.BtnCancel.Text = "Cancel";
            //
            // MeterCN_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(980, 560);
            this.Controls.Add(this.LblHint);
            this.Controls.Add(this.PanelHeader);
            this.Controls.Add(this.GridLines);
            this.Controls.Add(this.LblTotal);
            this.Controls.Add(this.PanelBottom);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(860, 480);
            this.Name = "MeterCN_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Correct with Credit Note";
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.PanelBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RepoNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridLines)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkKnockOff.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtReason.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtCNDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtCNDate.Properties.CalendarTimeProperties)).EndInit();
            this.PanelHeader.ResumeLayout(false);
            this.PanelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelHeader)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.PanelControl PanelHeader;
        private DevExpress.XtraEditors.LabelControl LblInvoice;
        private DevExpress.XtraEditors.LabelControl LblInvoiceVal;
        private DevExpress.XtraEditors.LabelControl LblExistingCN;
        private DevExpress.XtraEditors.LabelControl LblCNDate;
        private DevExpress.XtraEditors.DateEdit DtCNDate;
        private DevExpress.XtraEditors.LabelControl LblReason;
        private DevExpress.XtraEditors.TextEdit TxtReason;
        private DevExpress.XtraEditors.CheckEdit ChkKnockOff;
        private DevExpress.XtraGrid.GridControl GridLines;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewLines;
        private DevExpress.XtraGrid.Columns.GridColumn ColMachine;
        private DevExpress.XtraGrid.Columns.GridColumn ColSerial;
        private DevExpress.XtraGrid.Columns.GridColumn ColMeter;
        private DevExpress.XtraGrid.Columns.GridColumn ColBilledReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColMinReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColMaxReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColBilledUsage;
        private DevExpress.XtraGrid.Columns.GridColumn ColRate;
        private DevExpress.XtraGrid.Columns.GridColumn ColBilledCharge;
        private DevExpress.XtraGrid.Columns.GridColumn ColCorrectReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColCreditCopies;
        private DevExpress.XtraGrid.Columns.GridColumn ColCreditAmount;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoNum;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit RepoNum2;
        private DevExpress.XtraEditors.LabelControl LblTotal;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnGenerate;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
    }
}
