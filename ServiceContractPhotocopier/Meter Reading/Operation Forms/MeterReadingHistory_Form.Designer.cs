namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    partial class MeterReadingHistory_Form
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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PanelTop = new DevExpress.XtraEditors.PanelControl();
            this.LblHint = new DevExpress.XtraEditors.LabelControl();
            this.LblTitle = new DevExpress.XtraEditors.LabelControl();
            this.GridHist = new DevExpress.XtraGrid.GridControl();
            this.GridViewHist = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColMeterType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRole = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColPeriod = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColLastReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColReading = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUsage = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUnitPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColMinCharges = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColFOC = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColRebate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCharge = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColReadingDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColSource = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDocNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCreatedAt = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColCreatedBy = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PanelBottom = new DevExpress.XtraEditors.PanelControl();
            this.BtnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).BeginInit();
            this.PanelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridHist)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewHist)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).BeginInit();
            this.PanelBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelTop
            //
            this.PanelTop.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(94)))), ((int)(((byte)(32)))));
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.PanelTop.Controls.Add(this.LblHint);
            this.PanelTop.Controls.Add(this.LblTitle);
            this.PanelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.PanelTop.Location = new System.Drawing.Point(0, 0);
            this.PanelTop.Name = "PanelTop";
            this.PanelTop.Size = new System.Drawing.Size(1100, 62);
            this.PanelTop.TabIndex = 0;
            //
            // LblHint
            //
            this.LblHint.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Location = new System.Drawing.Point(16, 38);
            this.LblHint.Name = "LblHint";
            this.LblHint.Size = new System.Drawing.Size(560, 13);
            this.LblHint.TabIndex = 1;
            this.LblHint.Text = "Every reading event of this machine — the INVOICE rows (green) are the readings that produced the invoice amount.";
            //
            // LblTitle
            //
            this.LblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.LblTitle.Appearance.Options.UseFont = true;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblTitle.Location = new System.Drawing.Point(14, 12);
            this.LblTitle.Name = "LblTitle";
            this.LblTitle.Size = new System.Drawing.Size(140, 21);
            this.LblTitle.TabIndex = 0;
            this.LblTitle.Text = "Reading History";
            //
            // GridHist
            //
            this.GridHist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GridHist.Location = new System.Drawing.Point(0, 62);
            this.GridHist.MainView = this.GridViewHist;
            this.GridHist.Name = "GridHist";
            this.GridHist.Size = new System.Drawing.Size(1100, 478);
            this.GridHist.TabIndex = 1;
            this.GridHist.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewHist});
            //
            // GridViewHist
            //
            this.GridViewHist.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColMeterType,
            this.ColRole,
            this.ColPeriod,
            this.ColLastReading,
            this.ColReading,
            this.ColUsage,
            this.ColUnitPrice,
            this.ColMinCharges,
            this.ColFOC,
            this.ColRebate,
            this.ColCharge,
            this.ColReadingDate,
            this.ColSource,
            this.ColDocNo,
            this.ColCreatedAt,
            this.ColCreatedBy});
            this.GridViewHist.GridControl = this.GridHist;
            this.GridViewHist.Name = "GridViewHist";
            this.GridViewHist.OptionsBehavior.Editable = false;
            this.GridViewHist.OptionsView.ShowAutoFilterRow = true;
            this.GridViewHist.OptionsView.ShowGroupPanel = false;
            //
            // ColMeterType
            //
            this.ColMeterType.Caption = "Meter Type";
            this.ColMeterType.FieldName = "MeterTypeCode";
            this.ColMeterType.Name = "ColMeterType";
            this.ColMeterType.Visible = true;
            this.ColMeterType.VisibleIndex = 0;
            this.ColMeterType.Width = 140;
            //
            // ColRole
            //
            this.ColRole.Caption = "Meter";
            this.ColRole.FieldName = "MeterRole";
            this.ColRole.Name = "ColRole";
            this.ColRole.Visible = true;
            this.ColRole.VisibleIndex = 1;
            this.ColRole.Width = 60;
            //
            // ColPeriod
            //
            this.ColPeriod.Caption = "Period";
            this.ColPeriod.FieldName = "Period";
            this.ColPeriod.Name = "ColPeriod";
            this.ColPeriod.Visible = true;
            this.ColPeriod.VisibleIndex = 2;
            this.ColPeriod.Width = 70;
            //
            // ColLastReading
            //
            this.ColLastReading.Caption = "Last Reading";
            this.ColLastReading.DisplayFormat.FormatString = "n0";
            this.ColLastReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColLastReading.FieldName = "LastReading";
            this.ColLastReading.Name = "ColLastReading";
            this.ColLastReading.Visible = true;
            this.ColLastReading.VisibleIndex = 3;
            this.ColLastReading.Width = 95;
            //
            // ColReading
            //
            this.ColReading.Caption = "Reading";
            this.ColReading.DisplayFormat.FormatString = "n0";
            this.ColReading.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColReading.FieldName = "Reading";
            this.ColReading.Name = "ColReading";
            this.ColReading.Visible = true;
            this.ColReading.VisibleIndex = 4;
            this.ColReading.Width = 95;
            //
            // ColUsage
            //
            this.ColUsage.Caption = "Usage";
            this.ColUsage.DisplayFormat.FormatString = "n0";
            this.ColUsage.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColUsage.FieldName = "Usage";
            this.ColUsage.Name = "ColUsage";
            this.ColUsage.Visible = true;
            this.ColUsage.VisibleIndex = 5;
            this.ColUsage.Width = 85;
            //
            // ColUnitPrice
            //
            this.ColUnitPrice.Caption = "Unit Price";
            this.ColUnitPrice.DisplayFormat.FormatString = "n4";
            this.ColUnitPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColUnitPrice.FieldName = "UnitPrice";
            this.ColUnitPrice.Name = "ColUnitPrice";
            this.ColUnitPrice.Visible = true;
            this.ColUnitPrice.VisibleIndex = 6;
            this.ColUnitPrice.Width = 80;
            //
            // ColMinCharges
            //
            this.ColMinCharges.Caption = "Min. Charges";
            this.ColMinCharges.DisplayFormat.FormatString = "n2";
            this.ColMinCharges.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColMinCharges.FieldName = "MinCharges";
            this.ColMinCharges.Name = "ColMinCharges";
            this.ColMinCharges.Visible = true;
            this.ColMinCharges.VisibleIndex = 7;
            this.ColMinCharges.Width = 85;
            //
            // ColFOC
            //
            this.ColFOC.Caption = "FOC Qty";
            this.ColFOC.DisplayFormat.FormatString = "n0";
            this.ColFOC.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColFOC.FieldName = "FOCQty";
            this.ColFOC.Name = "ColFOC";
            this.ColFOC.Visible = true;
            this.ColFOC.VisibleIndex = 8;
            this.ColFOC.Width = 70;
            //
            // ColRebate
            //
            this.ColRebate.Caption = "Rebate (%)";
            this.ColRebate.DisplayFormat.FormatString = "n2";
            this.ColRebate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColRebate.FieldName = "RebatePct";
            this.ColRebate.Name = "ColRebate";
            this.ColRebate.Visible = true;
            this.ColRebate.VisibleIndex = 9;
            this.ColRebate.Width = 75;
            //
            // ColCharge
            //
            this.ColCharge.Caption = "Amount";
            this.ColCharge.DisplayFormat.FormatString = "n2";
            this.ColCharge.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.ColCharge.FieldName = "Charge";
            this.ColCharge.Name = "ColCharge";
            this.ColCharge.Visible = true;
            this.ColCharge.VisibleIndex = 10;
            this.ColCharge.Width = 85;
            //
            // ColReadingDate
            //
            this.ColReadingDate.Caption = "Reading Date";
            this.ColReadingDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.ColReadingDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColReadingDate.FieldName = "ReadingDate";
            this.ColReadingDate.Name = "ColReadingDate";
            this.ColReadingDate.Visible = true;
            this.ColReadingDate.VisibleIndex = 11;
            this.ColReadingDate.Width = 95;
            //
            // ColSource
            //
            this.ColSource.Caption = "Source";
            this.ColSource.FieldName = "Source";
            this.ColSource.Name = "ColSource";
            this.ColSource.Visible = true;
            this.ColSource.VisibleIndex = 12;
            this.ColSource.Width = 120;
            //
            // ColDocNo
            //
            this.ColDocNo.Caption = "Invoice No";
            this.ColDocNo.FieldName = "DocNo";
            this.ColDocNo.Name = "ColDocNo";
            this.ColDocNo.Visible = true;
            this.ColDocNo.VisibleIndex = 13;
            this.ColDocNo.Width = 110;
            //
            // ColCreatedAt
            //
            this.ColCreatedAt.Caption = "Done At";
            this.ColCreatedAt.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            this.ColCreatedAt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColCreatedAt.FieldName = "CreatedAt";
            this.ColCreatedAt.Name = "ColCreatedAt";
            this.ColCreatedAt.Visible = true;
            this.ColCreatedAt.VisibleIndex = 14;
            this.ColCreatedAt.Width = 125;
            //
            // ColCreatedBy
            //
            this.ColCreatedBy.Caption = "Done By";
            this.ColCreatedBy.FieldName = "CreatedBy";
            this.ColCreatedBy.Name = "ColCreatedBy";
            this.ColCreatedBy.Visible = true;
            this.ColCreatedBy.VisibleIndex = 15;
            this.ColCreatedBy.Width = 90;
            //
            // PanelBottom
            //
            this.PanelBottom.Controls.Add(this.BtnClose);
            this.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.PanelBottom.Location = new System.Drawing.Point(0, 540);
            this.PanelBottom.Name = "PanelBottom";
            this.PanelBottom.Size = new System.Drawing.Size(1100, 60);
            this.PanelBottom.TabIndex = 2;
            //
            // BtnClose
            //
            this.BtnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClose.Location = new System.Drawing.Point(966, 10);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(120, 40);
            this.BtnClose.TabIndex = 0;
            this.BtnClose.Text = "Close";
            this.BtnClose.Click += new System.EventHandler(this.BtnClose_Click);
            //
            // MeterReadingHistory_Form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1480, 620);
            this.Controls.Add(this.GridHist);
            this.Controls.Add(this.PanelBottom);
            this.Controls.Add(this.PanelTop);
            this.MinimizeBox = false;
            this.Name = "MeterReadingHistory_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Meter Reading — Reading History";
            ((System.ComponentModel.ISupportInitialize)(this.PanelTop)).EndInit();
            this.PanelTop.ResumeLayout(false);
            this.PanelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridHist)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewHist)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelBottom)).EndInit();
            this.PanelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl PanelTop;
        private DevExpress.XtraEditors.LabelControl LblHint;
        private DevExpress.XtraEditors.LabelControl LblTitle;
        private DevExpress.XtraGrid.GridControl GridHist;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewHist;
        private DevExpress.XtraGrid.Columns.GridColumn ColMeterType;
        private DevExpress.XtraGrid.Columns.GridColumn ColRole;
        private DevExpress.XtraGrid.Columns.GridColumn ColPeriod;
        private DevExpress.XtraGrid.Columns.GridColumn ColLastReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColReading;
        private DevExpress.XtraGrid.Columns.GridColumn ColUsage;
        private DevExpress.XtraGrid.Columns.GridColumn ColUnitPrice;
        private DevExpress.XtraGrid.Columns.GridColumn ColMinCharges;
        private DevExpress.XtraGrid.Columns.GridColumn ColFOC;
        private DevExpress.XtraGrid.Columns.GridColumn ColRebate;
        private DevExpress.XtraGrid.Columns.GridColumn ColCharge;
        private DevExpress.XtraGrid.Columns.GridColumn ColReadingDate;
        private DevExpress.XtraGrid.Columns.GridColumn ColSource;
        private DevExpress.XtraGrid.Columns.GridColumn ColDocNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColCreatedAt;
        private DevExpress.XtraGrid.Columns.GridColumn ColCreatedBy;
        private DevExpress.XtraEditors.PanelControl PanelBottom;
        private DevExpress.XtraEditors.SimpleButton BtnClose;
    }
}
