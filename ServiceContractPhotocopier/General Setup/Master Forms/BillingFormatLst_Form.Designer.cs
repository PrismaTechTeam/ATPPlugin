using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class BillingFormatLst_Form
    {
        /// <summary> Required designer variable. </summary>
        private System.ComponentModel.IContainer components = null;

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private SimpleButton BtnNew;
        private SimpleButton BtnEdit;
        private SimpleButton BtnCopyNew;
        private SimpleButton BtnSave;
        private SimpleButton BtnCancel;
        private SimpleButton BtnDelete;
        private SimpleButton BtnRefresh;
        private SimpleButton BtnExit;

        private GridControl GridFormats;
        private GridView GridViewFormats;

        private LabelControl LblCode;
        private TextEdit TxtCode;
        private LabelControl LblName;
        private TextEdit TxtName;
        private CheckEdit ChkInactive;
        private LabelControl LblRemark;
        private TextEdit TxtRemark;

        private GroupControl GrpInvoices;
        private RadioGroup RgInvoices;
        private GroupControl GrpRental;
        private RadioGroup RgRental;
        private GroupControl GrpMeter;
        private RadioGroup RgMeter;

        private LabelControl LblPreview;
        private LabelControl LblPreviewNote;
        private GridControl GridPreview;
        private GridView GridViewPreview;

        private PanelControl PanelStatus;
        private LabelControl LblStatus;

        /// <summary> Clean up any resources being used. </summary>
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
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.BtnNew = new SimpleButton();
            this.BtnEdit = new SimpleButton();
            this.BtnCopyNew = new SimpleButton();
            this.BtnSave = new SimpleButton();
            this.BtnCancel = new SimpleButton();
            this.BtnDelete = new SimpleButton();
            this.BtnRefresh = new SimpleButton();
            this.BtnExit = new SimpleButton();
            this.GridFormats = new GridControl();
            this.GridViewFormats = new GridView();
            this.LblCode = new LabelControl();
            this.TxtCode = new TextEdit();
            this.LblName = new LabelControl();
            this.TxtName = new TextEdit();
            this.ChkInactive = new CheckEdit();
            this.LblRemark = new LabelControl();
            this.TxtRemark = new TextEdit();
            this.GrpInvoices = new GroupControl();
            this.RgInvoices = new RadioGroup();
            this.GrpRental = new GroupControl();
            this.RgRental = new RadioGroup();
            this.GrpMeter = new GroupControl();
            this.RgMeter = new RadioGroup();
            this.LblPreview = new LabelControl();
            this.LblPreviewNote = new LabelControl();
            this.GridPreview = new GridControl();
            this.GridViewPreview = new GridView();
            this.PanelStatus = new PanelControl();
            this.LblStatus = new LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).BeginInit();
            this.PanelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).BeginInit();
            this.GrpInvoices.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgInvoices.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).BeginInit();
            this.GrpRental.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgRental.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).BeginInit();
            this.GrpMeter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgMeter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).BeginInit();
            this.PanelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Billing Format";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new Point(0, 0);
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.Size = new Size(1050, 56);
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelToolbar
            //
            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Location = new Point(0, 56);
            this.PanelToolbar.Name = "PanelToolbar";
            this.PanelToolbar.Size = new Size(1050, 62);
            this.PanelToolbar.TabIndex = 1;
            this.PanelToolbar.Controls.Add(this.BtnNew);
            this.PanelToolbar.Controls.Add(this.BtnEdit);
            this.PanelToolbar.Controls.Add(this.BtnCopyNew);
            this.PanelToolbar.Controls.Add(this.BtnSave);
            this.PanelToolbar.Controls.Add(this.BtnCancel);
            this.PanelToolbar.Controls.Add(this.BtnDelete);
            this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);
            //
            // BtnNew
            //
            this.BtnNew.Location = new Point(8, 6);
            this.BtnNew.Name = "BtnNew";
            this.BtnNew.Size = new Size(86, 50);
            this.BtnNew.TabIndex = 0;
            this.BtnNew.Text = "New";
            this.BtnNew.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnNew.Click += new System.EventHandler(this.OnNew);
            //
            // BtnEdit
            //
            this.BtnEdit.Location = new Point(98, 6);
            this.BtnEdit.Name = "BtnEdit";
            this.BtnEdit.Size = new Size(86, 50);
            this.BtnEdit.TabIndex = 1;
            this.BtnEdit.Text = "Edit";
            this.BtnEdit.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnEdit.Click += new System.EventHandler(this.OnEdit);
            //
            // BtnCopyNew
            //
            this.BtnCopyNew.Location = new Point(188, 6);
            this.BtnCopyNew.Name = "BtnCopyNew";
            this.BtnCopyNew.Size = new Size(120, 50);
            this.BtnCopyNew.TabIndex = 2;
            this.BtnCopyNew.Text = "Copy to New";
            this.BtnCopyNew.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnCopyNew.Click += new System.EventHandler(this.OnCopyToNew);
            //
            // BtnSave
            //
            this.BtnSave.Location = new Point(312, 6);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new Size(86, 50);
            this.BtnSave.TabIndex = 3;
            this.BtnSave.Text = "Save";
            this.BtnSave.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnSave.Click += new System.EventHandler(this.OnSave);
            //
            // BtnCancel
            //
            this.BtnCancel.Location = new Point(402, 6);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new Size(86, 50);
            this.BtnCancel.TabIndex = 4;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnCancel.Click += new System.EventHandler(this.OnCancel);
            //
            // BtnDelete
            //
            this.BtnDelete.Location = new Point(492, 6);
            this.BtnDelete.Name = "BtnDelete";
            this.BtnDelete.Size = new Size(86, 50);
            this.BtnDelete.TabIndex = 5;
            this.BtnDelete.Text = "Delete";
            this.BtnDelete.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnDelete.Click += new System.EventHandler(this.OnDelete);
            //
            // BtnRefresh
            //
            this.BtnRefresh.Location = new Point(588, 6);
            this.BtnRefresh.Name = "BtnRefresh";
            this.BtnRefresh.Size = new Size(92, 50);
            this.BtnRefresh.TabIndex = 6;
            this.BtnRefresh.Text = "Refresh";
            this.BtnRefresh.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            //
            // BtnExit
            //
            this.BtnExit.Location = new Point(684, 6);
            this.BtnExit.Name = "BtnExit";
            this.BtnExit.Size = new Size(92, 50);
            this.BtnExit.TabIndex = 7;
            this.BtnExit.Text = "Exit (F2)";
            this.BtnExit.ImageOptions.Location = ImageLocation.MiddleLeft;
            this.BtnExit.Click += new System.EventHandler(this.OnExit);
            //
            // GridFormats
            //
            this.GridFormats.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.GridFormats.Location = new Point(14, 128);
            this.GridFormats.MainView = this.GridViewFormats;
            this.GridFormats.Name = "GridFormats";
            this.GridFormats.Size = new Size(1020, 196);
            this.GridFormats.TabIndex = 2;
            this.GridFormats.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewFormats});
            //
            // GridViewFormats
            //
            this.GridViewFormats.GridControl = this.GridFormats;
            this.GridViewFormats.Name = "GridViewFormats";
            this.GridViewFormats.OptionsBehavior.Editable = false;
            this.GridViewFormats.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewFormats.OptionsView.ShowGroupPanel = false;
            //
            // LblCode
            //
            this.LblCode.Location = new Point(14, 341);
            this.LblCode.Name = "LblCode";
            this.LblCode.Size = new Size(100, 16);
            this.LblCode.TabIndex = 3;
            this.LblCode.Text = "Format Code :";
            //
            // TxtCode
            //
            this.TxtCode.Location = new Point(120, 338);
            this.TxtCode.Name = "TxtCode";
            this.TxtCode.Properties.MaxLength = 20;
            this.TxtCode.Size = new Size(180, 22);
            this.TxtCode.TabIndex = 4;
            //
            // LblName
            //
            this.LblName.Location = new Point(320, 341);
            this.LblName.Name = "LblName";
            this.LblName.Size = new Size(50, 16);
            this.LblName.TabIndex = 5;
            this.LblName.Text = "Name :";
            //
            // TxtName
            //
            this.TxtName.Location = new Point(392, 338);
            this.TxtName.Name = "TxtName";
            this.TxtName.Properties.MaxLength = 100;
            this.TxtName.Size = new Size(480, 22);
            this.TxtName.TabIndex = 6;
            //
            // ChkInactive
            //
            this.ChkInactive.Location = new Point(892, 338);
            this.ChkInactive.Name = "ChkInactive";
            this.ChkInactive.Properties.Caption = "Inactive";
            this.ChkInactive.Size = new Size(100, 22);
            this.ChkInactive.TabIndex = 7;
            //
            // LblRemark
            //
            this.LblRemark.Location = new Point(14, 371);
            this.LblRemark.Name = "LblRemark";
            this.LblRemark.Size = new Size(100, 16);
            this.LblRemark.TabIndex = 8;
            this.LblRemark.Text = "Note :";
            //
            // TxtRemark
            //
            this.TxtRemark.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.TxtRemark.Location = new Point(120, 368);
            this.TxtRemark.Name = "TxtRemark";
            this.TxtRemark.Properties.MaxLength = 200;
            this.TxtRemark.Size = new Size(914, 22);
            this.TxtRemark.TabIndex = 9;
            //
            // GrpInvoices
            //
            this.GrpInvoices.Controls.Add(this.RgInvoices);
            this.GrpInvoices.Location = new Point(14, 400);
            this.GrpInvoices.Name = "GrpInvoices";
            this.GrpInvoices.Size = new Size(330, 122);
            this.GrpInvoices.TabIndex = 10;
            this.GrpInvoices.Text = "How many invoices?";
            //
            // RgInvoices
            //
            this.RgInvoices.Dock = DockStyle.Fill;
            this.RgInvoices.Location = new Point(2, 23);
            this.RgInvoices.Name = "RgInvoices";
            this.RgInvoices.Properties.Appearance.BackColor = Color.Transparent;
            this.RgInvoices.Properties.Appearance.Options.UseBackColor = true;
            this.RgInvoices.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgInvoices.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("ONE", "One invoice for everything"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("RS", "Rental on its own invoice"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("PM", "One invoice per machine"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("PMS", "Per machine, rental apart")});
            this.RgInvoices.Size = new Size(326, 97);
            this.RgInvoices.TabIndex = 0;
            //
            // GrpRental
            //
            this.GrpRental.Controls.Add(this.RgRental);
            this.GrpRental.Location = new Point(354, 400);
            this.GrpRental.Name = "GrpRental";
            this.GrpRental.Size = new Size(330, 122);
            this.GrpRental.TabIndex = 11;
            this.GrpRental.Text = "Rental lines";
            //
            // RgRental
            //
            this.RgRental.Dock = DockStyle.Fill;
            this.RgRental.Location = new Point(2, 23);
            this.RgRental.Name = "RgRental";
            this.RgRental.Properties.Appearance.BackColor = Color.Transparent;
            this.RgRental.Properties.Appearance.Options.UseBackColor = true;
            this.RgRental.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgRental.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("A", "One line for all machines"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("M", "One line per model"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("S", "One line per machine")});
            this.RgRental.Size = new Size(326, 97);
            this.RgRental.TabIndex = 0;
            //
            // GrpMeter
            //
            this.GrpMeter.Controls.Add(this.RgMeter);
            this.GrpMeter.Location = new Point(694, 400);
            this.GrpMeter.Name = "GrpMeter";
            this.GrpMeter.Size = new Size(340, 122);
            this.GrpMeter.TabIndex = 12;
            this.GrpMeter.Text = "Black && Colour lines";
            //
            // RgMeter
            //
            this.RgMeter.Dock = DockStyle.Fill;
            this.RgMeter.Location = new Point(2, 23);
            this.RgMeter.Name = "RgMeter";
            this.RgMeter.Properties.Appearance.BackColor = Color.Transparent;
            this.RgMeter.Properties.Appearance.Options.UseBackColor = true;
            this.RgMeter.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgMeter.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("A", "One BK + one CL for all machines"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("M", "One BK + one CL per model"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("S", "BK + CL per machine")});
            this.RgMeter.Size = new Size(336, 97);
            this.RgMeter.TabIndex = 0;
            //
            // LblPreview
            //
            this.LblPreview.Location = new Point(14, 534);
            this.LblPreview.Name = "LblPreview";
            this.LblPreview.Size = new Size(160, 16);
            this.LblPreview.TabIndex = 13;
            this.LblPreview.Text = "Sample Invoice :";
            //
            // LblPreviewNote
            //
            this.LblPreviewNote.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.LblPreviewNote.Appearance.ForeColor = Color.DimGray;
            this.LblPreviewNote.AutoSizeMode = LabelAutoSizeMode.None;
            this.LblPreviewNote.Location = new Point(180, 534);
            this.LblPreviewNote.Name = "LblPreviewNote";
            this.LblPreviewNote.Size = new Size(854, 16);
            this.LblPreviewNote.TabIndex = 14;
            this.LblPreviewNote.Text = "";
            //
            // GridPreview
            //
            this.GridPreview.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            this.GridPreview.Location = new Point(14, 556);
            this.GridPreview.MainView = this.GridViewPreview;
            this.GridPreview.Name = "GridPreview";
            this.GridPreview.Size = new Size(1020, 196);
            this.GridPreview.TabIndex = 15;
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
            // PanelStatus
            //
            this.PanelStatus.Controls.Add(this.LblStatus);
            this.PanelStatus.Dock = DockStyle.Bottom;
            this.PanelStatus.Location = new Point(0, 764);
            this.PanelStatus.Name = "PanelStatus";
            this.PanelStatus.Size = new Size(1050, 26);
            this.PanelStatus.TabIndex = 16;
            //
            // LblStatus
            //
            this.LblStatus.Location = new Point(10, 5);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new Size(600, 16);
            this.LblStatus.TabIndex = 0;
            this.LblStatus.Text = "Billing Format";
            //
            // BillingFormatLst_Form
            //
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1050, 790);
            this.Controls.Add(this.GridFormats);
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
            this.Controls.Add(this.LblPreview);
            this.Controls.Add(this.LblPreviewNote);
            this.Controls.Add(this.GridPreview);
            this.Controls.Add(this.PanelStatus);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.Font = new Font("Segoe UI", 9F);
            this.KeyPreview = true;
            this.MinimumSize = new Size(900, 640);
            this.Name = "BillingFormatLst_Form";
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Billing Format";
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).EndInit();
            this.PanelStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RgMeter.Properties)).EndInit();
            this.GrpMeter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RgRental.Properties)).EndInit();
            this.GrpRental.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RgInvoices.Properties)).EndInit();
            this.GrpInvoices.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).EndInit();
            this.PanelToolbar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
