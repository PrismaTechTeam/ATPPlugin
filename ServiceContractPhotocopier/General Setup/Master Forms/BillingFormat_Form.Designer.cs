namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class BillingFormat_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private DevExpress.XtraEditors.LabelControl LblCode;
        private DevExpress.XtraEditors.TextEdit TxtCode;
        private DevExpress.XtraEditors.LabelControl LblName;
        private DevExpress.XtraEditors.TextEdit TxtName;
        private DevExpress.XtraEditors.LabelControl LblRemark;
        private DevExpress.XtraEditors.TextEdit TxtRemark;
        private DevExpress.XtraEditors.CheckEdit ChkInactive;

        private DevExpress.XtraEditors.GroupControl GrpInvoices;
        private DevExpress.XtraEditors.CheckEdit RdoOneInvoice;
        private DevExpress.XtraEditors.CheckEdit RdoRentalSeparate;
        private DevExpress.XtraEditors.CheckEdit RdoPerMachine;
        private DevExpress.XtraEditors.CheckEdit RdoPerMachineSeparate;

        private DevExpress.XtraEditors.GroupControl GrpRental;
        private DevExpress.XtraEditors.CheckEdit RdoRentalOneLine;
        private DevExpress.XtraEditors.CheckEdit RdoRentalPerModel;
        private DevExpress.XtraEditors.CheckEdit RdoRentalPerMachine;

        private DevExpress.XtraEditors.GroupControl GrpMeter;
        private DevExpress.XtraEditors.CheckEdit RdoMeterOneLine;
        private DevExpress.XtraEditors.CheckEdit RdoMeterPerModel;
        private DevExpress.XtraEditors.CheckEdit RdoMeterPerMachine;

        private DevExpress.XtraGrid.GridControl GridPreview;
        private DevExpress.XtraGrid.Views.Grid.GridView GridViewPreview;
        private DevExpress.XtraEditors.LabelControl LblPreviewNote;
        private DevExpress.XtraEditors.SimpleButton BtnSave;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;

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
            this.LblCode = new DevExpress.XtraEditors.LabelControl();
            this.TxtCode = new DevExpress.XtraEditors.TextEdit();
            this.LblName = new DevExpress.XtraEditors.LabelControl();
            this.TxtName = new DevExpress.XtraEditors.TextEdit();
            this.LblRemark = new DevExpress.XtraEditors.LabelControl();
            this.TxtRemark = new DevExpress.XtraEditors.TextEdit();
            this.ChkInactive = new DevExpress.XtraEditors.CheckEdit();
            this.GrpInvoices = new DevExpress.XtraEditors.GroupControl();
            this.RdoOneInvoice = new DevExpress.XtraEditors.CheckEdit();
            this.RdoRentalSeparate = new DevExpress.XtraEditors.CheckEdit();
            this.RdoPerMachine = new DevExpress.XtraEditors.CheckEdit();
            this.RdoPerMachineSeparate = new DevExpress.XtraEditors.CheckEdit();
            this.GrpRental = new DevExpress.XtraEditors.GroupControl();
            this.RdoRentalOneLine = new DevExpress.XtraEditors.CheckEdit();
            this.RdoRentalPerModel = new DevExpress.XtraEditors.CheckEdit();
            this.RdoRentalPerMachine = new DevExpress.XtraEditors.CheckEdit();
            this.GrpMeter = new DevExpress.XtraEditors.GroupControl();
            this.RdoMeterOneLine = new DevExpress.XtraEditors.CheckEdit();
            this.RdoMeterPerModel = new DevExpress.XtraEditors.CheckEdit();
            this.RdoMeterPerMachine = new DevExpress.XtraEditors.CheckEdit();
            this.GridPreview = new DevExpress.XtraGrid.GridControl();
            this.GridViewPreview = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblPreviewNote = new DevExpress.XtraEditors.LabelControl();
            this.BtnSave = new DevExpress.XtraEditors.SimpleButton();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).BeginInit();
            this.GrpInvoices.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RdoOneInvoice.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalSeparate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoPerMachine.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoPerMachineSeparate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).BeginInit();
            this.GrpRental.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalOneLine.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalPerModel.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalPerMachine.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).BeginInit();
            this.GrpMeter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterOneLine.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterPerModel.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterPerMachine.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).BeginInit();
            this.SuspendLayout();
            //
            // LblCode
            //
            this.LblCode.Location = new System.Drawing.Point(14, 17);
            this.LblCode.Name = "LblCode";
            this.LblCode.Size = new System.Drawing.Size(60, 16);
            this.LblCode.TabIndex = 0;
            this.LblCode.Text = "Code";
            //
            // TxtCode
            //
            this.TxtCode.Location = new System.Drawing.Point(80, 14);
            this.TxtCode.Name = "TxtCode";
            this.TxtCode.Properties.MaxLength = 20;
            this.TxtCode.Size = new System.Drawing.Size(160, 22);
            this.TxtCode.TabIndex = 1;
            //
            // LblName
            //
            this.LblName.Location = new System.Drawing.Point(258, 17);
            this.LblName.Name = "LblName";
            this.LblName.Size = new System.Drawing.Size(40, 16);
            this.LblName.TabIndex = 2;
            this.LblName.Text = "Name";
            //
            // TxtName
            //
            this.TxtName.Location = new System.Drawing.Point(304, 14);
            this.TxtName.Name = "TxtName";
            this.TxtName.Properties.MaxLength = 100;
            this.TxtName.Size = new System.Drawing.Size(360, 22);
            this.TxtName.TabIndex = 3;
            //
            // ChkInactive
            //
            this.ChkInactive.Location = new System.Drawing.Point(680, 14);
            this.ChkInactive.Name = "ChkInactive";
            this.ChkInactive.Properties.Caption = "Inactive";
            this.ChkInactive.Size = new System.Drawing.Size(90, 22);
            this.ChkInactive.TabIndex = 4;
            //
            // LblRemark
            //
            this.LblRemark.Location = new System.Drawing.Point(14, 47);
            this.LblRemark.Name = "LblRemark";
            this.LblRemark.Size = new System.Drawing.Size(60, 16);
            this.LblRemark.TabIndex = 5;
            this.LblRemark.Text = "Note";
            //
            // TxtRemark
            //
            this.TxtRemark.Location = new System.Drawing.Point(80, 44);
            this.TxtRemark.Name = "TxtRemark";
            this.TxtRemark.Properties.MaxLength = 200;
            this.TxtRemark.Size = new System.Drawing.Size(690, 22);
            this.TxtRemark.TabIndex = 6;
            //
            // GrpInvoices
            //
            this.GrpInvoices.Controls.Add(this.RdoOneInvoice);
            this.GrpInvoices.Controls.Add(this.RdoRentalSeparate);
            this.GrpInvoices.Controls.Add(this.RdoPerMachine);
            this.GrpInvoices.Controls.Add(this.RdoPerMachineSeparate);
            this.GrpInvoices.Location = new System.Drawing.Point(14, 78);
            this.GrpInvoices.Name = "GrpInvoices";
            this.GrpInvoices.Size = new System.Drawing.Size(250, 130);
            this.GrpInvoices.TabIndex = 7;
            this.GrpInvoices.Text = "How many invoices?";
            //
            // RdoOneInvoice
            //
            this.RdoOneInvoice.Location = new System.Drawing.Point(12, 26);
            this.RdoOneInvoice.Name = "RdoOneInvoice";
            this.RdoOneInvoice.Properties.Caption = "One invoice for everything";
            this.RdoOneInvoice.Properties.RadioGroupIndex = 1;
            this.RdoOneInvoice.Size = new System.Drawing.Size(228, 22);
            this.RdoOneInvoice.TabIndex = 0;
            this.RdoOneInvoice.TabStop = false;
            //
            // RdoRentalSeparate
            //
            this.RdoRentalSeparate.Location = new System.Drawing.Point(12, 50);
            this.RdoRentalSeparate.Name = "RdoRentalSeparate";
            this.RdoRentalSeparate.Properties.Caption = "Rental on its own invoice";
            this.RdoRentalSeparate.Properties.RadioGroupIndex = 1;
            this.RdoRentalSeparate.Size = new System.Drawing.Size(228, 22);
            this.RdoRentalSeparate.TabIndex = 1;
            this.RdoRentalSeparate.TabStop = false;
            //
            // RdoPerMachine
            //
            this.RdoPerMachine.Location = new System.Drawing.Point(12, 74);
            this.RdoPerMachine.Name = "RdoPerMachine";
            this.RdoPerMachine.Properties.Caption = "One invoice per machine";
            this.RdoPerMachine.Properties.RadioGroupIndex = 1;
            this.RdoPerMachine.Size = new System.Drawing.Size(228, 22);
            this.RdoPerMachine.TabIndex = 2;
            this.RdoPerMachine.TabStop = false;
            //
            // RdoPerMachineSeparate
            //
            this.RdoPerMachineSeparate.Location = new System.Drawing.Point(12, 98);
            this.RdoPerMachineSeparate.Name = "RdoPerMachineSeparate";
            this.RdoPerMachineSeparate.Properties.Caption = "Per machine, rental apart";
            this.RdoPerMachineSeparate.Properties.RadioGroupIndex = 1;
            this.RdoPerMachineSeparate.Size = new System.Drawing.Size(228, 22);
            this.RdoPerMachineSeparate.TabIndex = 3;
            this.RdoPerMachineSeparate.TabStop = false;
            //
            // GrpRental
            //
            this.GrpRental.Controls.Add(this.RdoRentalOneLine);
            this.GrpRental.Controls.Add(this.RdoRentalPerModel);
            this.GrpRental.Controls.Add(this.RdoRentalPerMachine);
            this.GrpRental.Location = new System.Drawing.Point(276, 78);
            this.GrpRental.Name = "GrpRental";
            this.GrpRental.Size = new System.Drawing.Size(240, 130);
            this.GrpRental.TabIndex = 8;
            this.GrpRental.Text = "Rental lines";
            //
            // RdoRentalOneLine
            //
            this.RdoRentalOneLine.Location = new System.Drawing.Point(12, 26);
            this.RdoRentalOneLine.Name = "RdoRentalOneLine";
            this.RdoRentalOneLine.Properties.Caption = "One line for all machines";
            this.RdoRentalOneLine.Properties.RadioGroupIndex = 2;
            this.RdoRentalOneLine.Size = new System.Drawing.Size(218, 22);
            this.RdoRentalOneLine.TabIndex = 0;
            this.RdoRentalOneLine.TabStop = false;
            //
            // RdoRentalPerModel
            //
            this.RdoRentalPerModel.Location = new System.Drawing.Point(12, 50);
            this.RdoRentalPerModel.Name = "RdoRentalPerModel";
            this.RdoRentalPerModel.Properties.Caption = "One line per model";
            this.RdoRentalPerModel.Properties.RadioGroupIndex = 2;
            this.RdoRentalPerModel.Size = new System.Drawing.Size(218, 22);
            this.RdoRentalPerModel.TabIndex = 1;
            this.RdoRentalPerModel.TabStop = false;
            //
            // RdoRentalPerMachine
            //
            this.RdoRentalPerMachine.Location = new System.Drawing.Point(12, 74);
            this.RdoRentalPerMachine.Name = "RdoRentalPerMachine";
            this.RdoRentalPerMachine.Properties.Caption = "One line per machine";
            this.RdoRentalPerMachine.Properties.RadioGroupIndex = 2;
            this.RdoRentalPerMachine.Size = new System.Drawing.Size(218, 22);
            this.RdoRentalPerMachine.TabIndex = 2;
            this.RdoRentalPerMachine.TabStop = false;
            //
            // GrpMeter
            //
            this.GrpMeter.Controls.Add(this.RdoMeterOneLine);
            this.GrpMeter.Controls.Add(this.RdoMeterPerModel);
            this.GrpMeter.Controls.Add(this.RdoMeterPerMachine);
            this.GrpMeter.Location = new System.Drawing.Point(528, 78);
            this.GrpMeter.Name = "GrpMeter";
            this.GrpMeter.Size = new System.Drawing.Size(242, 130);
            this.GrpMeter.TabIndex = 9;
            this.GrpMeter.Text = "Black && Colour lines";
            //
            // RdoMeterOneLine
            //
            this.RdoMeterOneLine.Location = new System.Drawing.Point(12, 26);
            this.RdoMeterOneLine.Name = "RdoMeterOneLine";
            this.RdoMeterOneLine.Properties.Caption = "One BK + one CL for all";
            this.RdoMeterOneLine.Properties.RadioGroupIndex = 3;
            this.RdoMeterOneLine.Size = new System.Drawing.Size(220, 22);
            this.RdoMeterOneLine.TabIndex = 0;
            this.RdoMeterOneLine.TabStop = false;
            //
            // RdoMeterPerModel
            //
            this.RdoMeterPerModel.Location = new System.Drawing.Point(12, 50);
            this.RdoMeterPerModel.Name = "RdoMeterPerModel";
            this.RdoMeterPerModel.Properties.Caption = "One BK + one CL per model";
            this.RdoMeterPerModel.Properties.RadioGroupIndex = 3;
            this.RdoMeterPerModel.Size = new System.Drawing.Size(220, 22);
            this.RdoMeterPerModel.TabIndex = 1;
            this.RdoMeterPerModel.TabStop = false;
            //
            // RdoMeterPerMachine
            //
            this.RdoMeterPerMachine.Location = new System.Drawing.Point(12, 74);
            this.RdoMeterPerMachine.Name = "RdoMeterPerMachine";
            this.RdoMeterPerMachine.Properties.Caption = "BK + CL per machine";
            this.RdoMeterPerMachine.Properties.RadioGroupIndex = 3;
            this.RdoMeterPerMachine.Size = new System.Drawing.Size(220, 22);
            this.RdoMeterPerMachine.TabIndex = 2;
            this.RdoMeterPerMachine.TabStop = false;
            //
            // LblPreviewNote
            //
            this.LblPreviewNote.Appearance.ForeColor = System.Drawing.Color.DimGray;
            this.LblPreviewNote.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.LblPreviewNote.Location = new System.Drawing.Point(14, 216);
            this.LblPreviewNote.Name = "LblPreviewNote";
            this.LblPreviewNote.Size = new System.Drawing.Size(756, 18);
            this.LblPreviewNote.TabIndex = 10;
            this.LblPreviewNote.Text = "";
            //
            // GridPreview
            //
            this.GridPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GridPreview.Location = new System.Drawing.Point(14, 238);
            this.GridPreview.MainView = this.GridViewPreview;
            this.GridPreview.Name = "GridPreview";
            this.GridPreview.Size = new System.Drawing.Size(756, 260);
            this.GridPreview.TabIndex = 11;
            this.GridPreview.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewPreview});
            //
            // GridViewPreview
            //
            this.GridViewPreview.GridControl = this.GridPreview;
            this.GridViewPreview.Name = "GridViewPreview";
            this.GridViewPreview.OptionsBehavior.Editable = false;
            this.GridViewPreview.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewPreview.OptionsView.ShowGroupPanel = false;
            this.GridViewPreview.OptionsView.ShowIndicator = false;
            //
            // BtnSave
            //
            this.BtnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnSave.Location = new System.Drawing.Point(594, 510);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(84, 30);
            this.BtnSave.TabIndex = 12;
            this.BtnSave.Text = "Save";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            //
            // BtnCancel
            //
            this.BtnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(686, 510);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(84, 30);
            this.BtnCancel.TabIndex = 13;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            //
            // BillingFormat_Form
            //
            this.AcceptButton = this.BtnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.BtnCancel;
            this.ClientSize = new System.Drawing.Size(784, 554);
            this.Controls.Add(this.LblCode);
            this.Controls.Add(this.TxtCode);
            this.Controls.Add(this.LblName);
            this.Controls.Add(this.TxtName);
            this.Controls.Add(this.ChkInactive);
            this.Controls.Add(this.LblRemark);
            this.Controls.Add(this.TxtRemark);
            this.Controls.Add(this.GrpInvoices);
            this.Controls.Add(this.GrpRental);
            this.Controls.Add(this.GrpMeter);
            this.Controls.Add(this.LblPreviewNote);
            this.Controls.Add(this.GridPreview);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.BtnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 560);
            this.Name = "BillingFormat_Form";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Billing Format";
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterPerMachine.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterPerModel.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoMeterOneLine.Properties)).EndInit();
            this.GrpMeter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalPerMachine.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalPerModel.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalOneLine.Properties)).EndInit();
            this.GrpRental.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoPerMachineSeparate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoPerMachine.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoRentalSeparate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RdoOneInvoice.Properties)).EndInit();
            this.GrpInvoices.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
