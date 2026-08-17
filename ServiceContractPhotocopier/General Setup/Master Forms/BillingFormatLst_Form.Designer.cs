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

        private PanelControl PanelDetail;
        private LabelControl LblCode;
        private TextEdit TxtCode;
        private LabelControl LblName;
        private TextEdit TxtName;
        private CheckEdit ChkInactive;
        private LabelControl LblRemark;
        private TextEdit TxtRemark;

        private PanelControl PanelQuestions;
        private GroupControl GrpInvoices;
        private RadioGroup RgInvoices;
        private GroupControl GrpRental;
        private RadioGroup RgRental;
        private GroupControl GrpMeter;
        private RadioGroup RgMeter;

        private SplitContainerControl SplitMain;
        private GridControl GridFormats;
        private GridView GridViewFormats;
        private PanelControl PanelPreviewHead;
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
            this.PanelDetail = new PanelControl();
            this.LblCode = new LabelControl();
            this.TxtCode = new TextEdit();
            this.LblName = new LabelControl();
            this.TxtName = new TextEdit();
            this.ChkInactive = new CheckEdit();
            this.LblRemark = new LabelControl();
            this.TxtRemark = new TextEdit();
            this.PanelQuestions = new PanelControl();
            this.GrpInvoices = new GroupControl();
            this.RgInvoices = new RadioGroup();
            this.GrpRental = new GroupControl();
            this.RgRental = new RadioGroup();
            this.GrpMeter = new GroupControl();
            this.RgMeter = new RadioGroup();
            this.SplitMain = new SplitContainerControl();
            this.GridFormats = new GridControl();
            this.GridViewFormats = new GridView();
            this.PanelPreviewHead = new PanelControl();
            this.LblPreview = new LabelControl();
            this.LblPreviewNote = new LabelControl();
            this.GridPreview = new GridControl();
            this.GridViewPreview = new GridView();
            this.PanelStatus = new PanelControl();
            this.LblStatus = new LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).BeginInit();
            this.PanelToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetail)).BeginInit();
            this.PanelDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelQuestions)).BeginInit();
            this.PanelQuestions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).BeginInit();
            this.GrpInvoices.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgInvoices.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).BeginInit();
            this.GrpRental.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgRental.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).BeginInit();
            this.GrpMeter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RgMeter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).BeginInit();
            this.SplitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelPreviewHead)).BeginInit();
            this.PanelPreviewHead.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).BeginInit();
            this.PanelStatus.SuspendLayout();
            this.SuspendLayout();
            //
            // PanelHeaderTop
            //
            // No Size is set on purpose. PanelHeader overwrites its own Height in OnLoad --
            // CalcNewY(44) with an empty Hint, CalcNewY(80) with one -- so a Size here is dead code
            // that only makes the file look as though it decides the header's height. Everything
            // below it is docked, so it does not need to know.
            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Billing Format";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Name = "PanelHeaderTop";
            this.PanelHeaderTop.TabIndex = 0;
            //
            // PanelToolbar
            //
            this.PanelToolbar.Controls.Add(this.BtnNew);
            this.PanelToolbar.Controls.Add(this.BtnEdit);
            this.PanelToolbar.Controls.Add(this.BtnCopyNew);
            this.PanelToolbar.Controls.Add(this.BtnSave);
            this.PanelToolbar.Controls.Add(this.BtnCancel);
            this.PanelToolbar.Controls.Add(this.BtnDelete);
            this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);
            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Name = "PanelToolbar";
            this.PanelToolbar.Size = new Size(1050, 62);
            this.PanelToolbar.TabIndex = 1;
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
            // PanelDetail
            //
            this.PanelDetail.Controls.Add(this.LblCode);
            this.PanelDetail.Controls.Add(this.TxtCode);
            this.PanelDetail.Controls.Add(this.LblName);
            this.PanelDetail.Controls.Add(this.TxtName);
            this.PanelDetail.Controls.Add(this.ChkInactive);
            this.PanelDetail.Controls.Add(this.LblRemark);
            this.PanelDetail.Controls.Add(this.TxtRemark);
            this.PanelDetail.Dock = DockStyle.Top;
            this.PanelDetail.Name = "PanelDetail";
            this.PanelDetail.Size = new Size(1050, 84);
            this.PanelDetail.TabIndex = 2;
            //
            // LblCode
            //
            this.LblCode.Location = new Point(14, 15);
            this.LblCode.Name = "LblCode";
            this.LblCode.Size = new Size(100, 16);
            this.LblCode.TabIndex = 0;
            this.LblCode.Text = "Format Code :";
            //
            // TxtCode
            //
            this.TxtCode.Location = new Point(120, 12);
            this.TxtCode.Name = "TxtCode";
            this.TxtCode.Properties.MaxLength = 20;
            this.TxtCode.Size = new Size(180, 22);
            this.TxtCode.TabIndex = 1;
            //
            // LblName
            //
            this.LblName.Location = new Point(320, 15);
            this.LblName.Name = "LblName";
            this.LblName.Size = new Size(50, 16);
            this.LblName.TabIndex = 2;
            this.LblName.Text = "Name :";
            //
            // TxtName
            //
            this.TxtName.Location = new Point(392, 12);
            this.TxtName.Name = "TxtName";
            this.TxtName.Properties.MaxLength = 100;
            this.TxtName.Size = new Size(480, 22);
            this.TxtName.TabIndex = 3;
            //
            // ChkInactive
            //
            // Anchored Right so it keeps its distance from the edge instead of drifting into the
            // middle of a maximized window.
            this.ChkInactive.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            this.ChkInactive.Location = new Point(892, 12);
            this.ChkInactive.Name = "ChkInactive";
            this.ChkInactive.Properties.Caption = "Inactive";
            this.ChkInactive.Size = new Size(100, 22);
            this.ChkInactive.TabIndex = 4;
            //
            // LblRemark
            //
            this.LblRemark.Location = new Point(14, 47);
            this.LblRemark.Name = "LblRemark";
            this.LblRemark.Size = new Size(100, 16);
            this.LblRemark.TabIndex = 5;
            this.LblRemark.Text = "Note :";
            //
            // TxtRemark
            //
            this.TxtRemark.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.TxtRemark.Location = new Point(120, 44);
            this.TxtRemark.Name = "TxtRemark";
            this.TxtRemark.Properties.MaxLength = 200;
            this.TxtRemark.Size = new Size(914, 22);
            this.TxtRemark.TabIndex = 6;
            //
            // PanelQuestions
            //
            // Add order matters: WinForms lays docked children out from the HIGHEST index down, so
            // the Fill control must go in first and the leftmost Left-docked one last.
            this.PanelQuestions.Controls.Add(this.GrpMeter);
            this.PanelQuestions.Controls.Add(this.GrpRental);
            this.PanelQuestions.Controls.Add(this.GrpInvoices);
            this.PanelQuestions.Dock = DockStyle.Top;
            this.PanelQuestions.Name = "PanelQuestions";
            this.PanelQuestions.Padding = new Padding(14, 6, 14, 6);
            this.PanelQuestions.Size = new Size(1050, 140);
            this.PanelQuestions.TabIndex = 3;
            //
            // GrpInvoices
            //
            this.GrpInvoices.Controls.Add(this.RgInvoices);
            this.GrpInvoices.Dock = DockStyle.Left;
            this.GrpInvoices.Name = "GrpInvoices";
            this.GrpInvoices.Size = new Size(340, 128);
            this.GrpInvoices.TabIndex = 0;
            this.GrpInvoices.Text = "How many invoices?";
            //
            // RgInvoices
            //
            // ItemVertAlignment defaults to Justify, which spreads the items over the whole height --
            // so a 4-item group and a 3-item group beside it get different row pitches and their
            // radio buttons do not line up. Top gives every group the same natural row height.
            this.RgInvoices.Dock = DockStyle.Fill;
            this.RgInvoices.Name = "RgInvoices";
            this.RgInvoices.Properties.Appearance.BackColor = Color.Transparent;
            this.RgInvoices.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgInvoices.Properties.ItemVertAlignment = DevExpress.XtraEditors.RadioItemVertAlignment.Top;
            this.RgInvoices.Properties.ItemHorzAlignment = DevExpress.XtraEditors.RadioItemHorzAlignment.Near;
            this.RgInvoices.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("ONE", "One invoice for everything"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("RS", "Rental on its own invoice"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("PM", "One invoice per machine"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("PMS", "Per machine, rental apart")});
            this.RgInvoices.TabIndex = 0;
            //
            // GrpRental
            //
            this.GrpRental.Controls.Add(this.RgRental);
            this.GrpRental.Dock = DockStyle.Left;
            this.GrpRental.Name = "GrpRental";
            this.GrpRental.Size = new Size(340, 128);
            this.GrpRental.TabIndex = 1;
            this.GrpRental.Text = "Rental lines";
            //
            // RgRental
            //
            this.RgRental.Dock = DockStyle.Fill;
            this.RgRental.Name = "RgRental";
            this.RgRental.Properties.Appearance.BackColor = Color.Transparent;
            this.RgRental.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgRental.Properties.ItemVertAlignment = DevExpress.XtraEditors.RadioItemVertAlignment.Top;
            this.RgRental.Properties.ItemHorzAlignment = DevExpress.XtraEditors.RadioItemHorzAlignment.Near;
            this.RgRental.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("A", "One line for all machines"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("M", "One line per model"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("S", "One line per machine")});
            this.RgRental.TabIndex = 0;
            //
            // GrpMeter
            //
            this.GrpMeter.Controls.Add(this.RgMeter);
            this.GrpMeter.Dock = DockStyle.Fill;
            this.GrpMeter.Name = "GrpMeter";
            this.GrpMeter.Size = new Size(342, 128);
            this.GrpMeter.TabIndex = 2;
            this.GrpMeter.Text = "Black && Colour lines";
            //
            // RgMeter
            //
            this.RgMeter.Dock = DockStyle.Fill;
            this.RgMeter.Name = "RgMeter";
            this.RgMeter.Properties.Appearance.BackColor = Color.Transparent;
            this.RgMeter.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RgMeter.Properties.ItemVertAlignment = DevExpress.XtraEditors.RadioItemVertAlignment.Top;
            this.RgMeter.Properties.ItemHorzAlignment = DevExpress.XtraEditors.RadioItemHorzAlignment.Near;
            this.RgMeter.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("A", "One BK + one CL for all machines"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("M", "One BK + one CL per model"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("S", "BK + CL per machine")});
            this.RgMeter.TabIndex = 0;
            //
            // SplitMain
            //
            // The list and the sample invoice share the remaining height through a splitter the user
            // can drag, instead of two absolutely-placed grids that can only be right at one size.
            this.SplitMain.Dock = DockStyle.Fill;
            this.SplitMain.Horizontal = false;
            this.SplitMain.Name = "SplitMain";
            this.SplitMain.Panel1.Controls.Add(this.GridFormats);
            this.SplitMain.Panel1.Text = "Panel1";
            this.SplitMain.Panel2.Controls.Add(this.GridPreview);
            this.SplitMain.Panel2.Controls.Add(this.PanelPreviewHead);
            this.SplitMain.Panel2.Text = "Panel2";
            this.SplitMain.Size = new Size(1050, 422);
            this.SplitMain.SplitterPosition = 200;
            this.SplitMain.TabIndex = 4;
            //
            // GridFormats
            //
            this.GridFormats.Dock = DockStyle.Fill;
            this.GridFormats.MainView = this.GridViewFormats;
            this.GridFormats.Name = "GridFormats";
            this.GridFormats.TabIndex = 0;
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
            this.GridViewFormats.OptionsView.ShowIndicator = false;
            //
            // PanelPreviewHead
            //
            this.PanelPreviewHead.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PanelPreviewHead.Controls.Add(this.LblPreview);
            this.PanelPreviewHead.Controls.Add(this.LblPreviewNote);
            this.PanelPreviewHead.Dock = DockStyle.Top;
            this.PanelPreviewHead.Name = "PanelPreviewHead";
            this.PanelPreviewHead.Size = new Size(1050, 24);
            this.PanelPreviewHead.TabIndex = 0;
            //
            // LblPreview
            //
            this.LblPreview.Location = new Point(2, 4);
            this.LblPreview.Name = "LblPreview";
            this.LblPreview.Size = new Size(110, 16);
            this.LblPreview.TabIndex = 0;
            this.LblPreview.Text = "Sample Invoice :";
            //
            // LblPreviewNote
            //
            this.LblPreviewNote.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.LblPreviewNote.Appearance.ForeColor = Color.DimGray;
            this.LblPreviewNote.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
            this.LblPreviewNote.AutoSizeMode = LabelAutoSizeMode.None;
            this.LblPreviewNote.Location = new Point(118, 4);
            this.LblPreviewNote.Name = "LblPreviewNote";
            this.LblPreviewNote.Size = new Size(920, 16);
            this.LblPreviewNote.TabIndex = 1;
            this.LblPreviewNote.Text = "";
            //
            // GridPreview
            //
            this.GridPreview.Dock = DockStyle.Fill;
            this.GridPreview.MainView = this.GridViewPreview;
            this.GridPreview.Name = "GridPreview";
            this.GridPreview.TabIndex = 1;
            this.GridPreview.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GridViewPreview});
            //
            // GridViewPreview
            //
            // This grid is a picture of a document: sorting or regrouping it destroys the invoice and
            // there is no way back, so the customisation options are all off.
            this.GridViewPreview.GridControl = this.GridPreview;
            this.GridViewPreview.Name = "GridViewPreview";
            this.GridViewPreview.OptionsBehavior.Editable = false;
            this.GridViewPreview.OptionsBehavior.AutoExpandAllGroups = false;
            this.GridViewPreview.OptionsCustomization.AllowSort = false;
            this.GridViewPreview.OptionsCustomization.AllowFilter = false;
            this.GridViewPreview.OptionsCustomization.AllowGroup = false;
            this.GridViewPreview.OptionsCustomization.AllowColumnMoving = false;
            this.GridViewPreview.OptionsMenu.EnableColumnMenu = false;
            this.GridViewPreview.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.GridViewPreview.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.GridViewPreview.OptionsView.ShowFooter = true;
            this.GridViewPreview.OptionsView.ShowGroupPanel = false;
            this.GridViewPreview.OptionsView.ShowIndicator = false;
            //
            // PanelStatus
            //
            this.PanelStatus.Controls.Add(this.LblStatus);
            this.PanelStatus.Dock = DockStyle.Bottom;
            this.PanelStatus.Name = "PanelStatus";
            this.PanelStatus.Size = new Size(1050, 26);
            this.PanelStatus.TabIndex = 5;
            //
            // LblStatus
            //
            this.LblStatus.AutoSizeMode = LabelAutoSizeMode.None;
            this.LblStatus.Location = new Point(10, 5);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new Size(700, 16);
            this.LblStatus.TabIndex = 0;
            this.LblStatus.Text = "Billing Format";
            //
            // BillingFormatLst_Form
            //
            // Font FIRST, then the dimensions it really measures, then the mode. Assigning Font after
            // Controls.Add makes WinForms auto-scale using the font in force BEFORE the assignment --
            // DevExpress's Tahoma 8.25pt, which measures (6,13) -- against the declared (7,15), for a
            // factor of 0.857 x 0.867. Only children still carrying the ambient font are affected, and
            // every DevExpress editor stamps its own, so the two GridControls moved and nothing else
            // did. That is what put the sample invoice 74px above where it was drawn, underneath the
            // question boxes. Declared in this order the factor is 1.0.
            this.Font = new Font("Segoe UI", 9F);
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1050, 790);
            // Docked bands, added Fill-first and header-last: WinForms positions docked children from
            // the highest index down, each taking a slice out of what is left, so no two of these can
            // ever share a pixel at any client size.
            this.Controls.Add(this.SplitMain);
            this.Controls.Add(this.PanelStatus);
            this.Controls.Add(this.PanelQuestions);
            this.Controls.Add(this.PanelDetail);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.KeyPreview = true;
            this.MinimumSize = new Size(1066, 700);
            this.Name = "BillingFormatLst_Form";
            this.ShowIcon = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Billing Format";
            ((System.ComponentModel.ISupportInitialize)(this.PanelStatus)).EndInit();
            this.PanelStatus.ResumeLayout(false);
            this.PanelStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelPreviewHead)).EndInit();
            this.PanelPreviewHead.ResumeLayout(false);
            this.PanelPreviewHead.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewFormats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridFormats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplitMain)).EndInit();
            this.SplitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RgMeter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpMeter)).EndInit();
            this.GrpMeter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RgRental.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpRental)).EndInit();
            this.GrpRental.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RgInvoices.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GrpInvoices)).EndInit();
            this.GrpInvoices.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PanelQuestions)).EndInit();
            this.PanelQuestions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TxtRemark.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChkInactive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PanelDetail)).EndInit();
            this.PanelDetail.ResumeLayout(false);
            this.PanelDetail.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PanelToolbar)).EndInit();
            this.PanelToolbar.ResumeLayout(false);
            this.PanelToolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
