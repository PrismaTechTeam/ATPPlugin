using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class MeterTypeLst_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private SimpleButton BtnNew, BtnEdit, BtnSave, BtnCancel, BtnDelete, BtnRefresh, BtnExit;
        private GridControl GridMT; private GridView GridViewMT;
        private GroupControl GrpDetail;
        private LabelControl LblCode, LblDesc, LblStock, LblMultiPrice, LblMinCharges, LblChargesRate, LblRebateQty, LblFOCQty;
        private TextEdit TxtCode, TxtDesc, TxtStockCode, TxtMultiPriceCode, TxtMinCharges, TxtChargesRate, TxtRebateQty, TxtFOCQty;
        private CheckEdit ChkInactive;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.BtnNew = new SimpleButton(); this.BtnEdit = new SimpleButton(); this.BtnSave = new SimpleButton();
            this.BtnCancel = new SimpleButton(); this.BtnDelete = new SimpleButton();
            this.BtnRefresh = new SimpleButton(); this.BtnExit = new SimpleButton();
            this.GridMT = new GridControl(); this.GridViewMT = new GridView();
            this.GrpDetail = new GroupControl();
            this.LblCode = new LabelControl(); this.TxtCode = new TextEdit();
            this.LblDesc = new LabelControl(); this.TxtDesc = new TextEdit();
            this.LblStock = new LabelControl(); this.TxtStockCode = new TextEdit();
            this.LblMultiPrice = new LabelControl(); this.TxtMultiPriceCode = new TextEdit();
            this.LblMinCharges = new LabelControl(); this.TxtMinCharges = new TextEdit();
            this.LblChargesRate = new LabelControl(); this.TxtChargesRate = new TextEdit();
            this.LblRebateQty = new LabelControl(); this.TxtRebateQty = new TextEdit();
            this.LblFOCQty = new LabelControl(); this.TxtFOCQty = new TextEdit();
            this.ChkInactive = new CheckEdit();

            this.SuspendLayout();
            this.Text = "Meter Type"; this.ClientSize = new Size(1050, 720);
            this.StartPosition = FormStartPosition.CenterParent; this.MinimumSize = new Size(900, 620);

            // Native AutoCount green header (same as Maintain Service Contract / Item; hint hidden).
            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Meter Type";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new Point(0, 0);
            this.PanelHeaderTop.Size = new Size(1050, 56);

            // Toolbar row — 86x50 icon buttons, identical to the Maintain Service Contract list.
            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Location = new Point(0, 56);
            this.PanelToolbar.Size = new Size(1050, 62);
            Tb(this.BtnNew, "New", 8, 6, 86); this.BtnNew.Click += new System.EventHandler(this.OnNew);
            Tb(this.BtnEdit, "Edit", 98, 6, 86); this.BtnEdit.Click += new System.EventHandler(this.OnEdit);
            Tb(this.BtnSave, "Save", 188, 6, 86); this.BtnSave.Click += new System.EventHandler(this.OnSave);
            Tb(this.BtnCancel, "Cancel", 278, 6, 86); this.BtnCancel.Click += new System.EventHandler(this.OnCancel);
            Tb(this.BtnDelete, "Delete", 368, 6, 86); this.BtnDelete.Click += new System.EventHandler(this.OnDelete);
            Tb(this.BtnRefresh, "Refresh", 458, 6, 92); this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            Tb(this.BtnExit, "Exit (F2)", 556, 6, 92); this.BtnExit.Click += new System.EventHandler(this.OnExit);
            this.PanelToolbar.Controls.Add(this.BtnNew); this.PanelToolbar.Controls.Add(this.BtnEdit);
            this.PanelToolbar.Controls.Add(this.BtnSave); this.PanelToolbar.Controls.Add(this.BtnCancel);
            this.PanelToolbar.Controls.Add(this.BtnDelete); this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);

            this.GridMT.Location = new Point(14, 128); this.GridMT.Size = new Size(1020, 260);
            this.GridMT.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.GridViewMT.GridControl = this.GridMT; this.GridViewMT.OptionsView.ShowGroupPanel = false; this.GridViewMT.OptionsBehavior.Editable = false;
            this.GridMT.MainView = this.GridViewMT; this.GridMT.ViewCollection.Add(this.GridViewMT);
            AddCol(this.GridViewMT, "MeterTypeCode", "Meter Type Code", 150); AddCol(this.GridViewMT, "Description", "Description", 220);
            AddCol(this.GridViewMT, "StockCode", "Stock Code", 130); AddCol(this.GridViewMT, "MeterMultiPriceCode", "Multi Price Code", 140);
            AddCol(this.GridViewMT, "ChargesRate", "Charges Rate", 100); AddCol(this.GridViewMT, "MinimumCharges", "Min. Charges", 100);
            AddCol(this.GridViewMT, "RebateQtyInPercent", "Rebate Qty (%)", 100); AddCol(this.GridViewMT, "FOCQty", "FOC (Qty)", 80);
            AddCol(this.GridViewMT, "Inactive", "Inactive", 60);

            this.GrpDetail.Text = "Detail"; this.GrpDetail.Location = new Point(14, 396); this.GrpDetail.Size = new Size(1020, 310);
            this.GrpDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            int lX = 14, eX = 170, eW = 300; int y = 28, gap = 26;
            Lbl(this.LblCode, "Meter Type Code", lX, y); this.TxtCode.Location = new Point(eX, y); this.TxtCode.Width = eW;
            this.ChkInactive.Properties.Caption = "Inactive"; this.ChkInactive.Location = new Point(eX + eW + 20, y); y += gap;

            Lbl(this.LblDesc, "Description", lX, y); this.TxtDesc.Location = new Point(eX, y); this.TxtDesc.Width = 560; y += gap;

            Lbl(this.LblStock, "Service/Stock Item", lX, y); this.TxtStockCode.Location = new Point(eX, y); this.TxtStockCode.Width = eW; y += gap;

            Lbl(this.LblMultiPrice, "Meter Multi Price Code", lX, y); this.TxtMultiPriceCode.Location = new Point(eX, y); this.TxtMultiPriceCode.Width = eW; y += gap;

            Lbl(this.LblChargesRate, "Charges Rate (Base UOM)", lX, y); this.TxtChargesRate.Location = new Point(eX, y); this.TxtChargesRate.Width = 150; y += gap;

            Lbl(this.LblMinCharges, "Minimum Charges", lX, y); this.TxtMinCharges.Location = new Point(eX, y); this.TxtMinCharges.Width = 150; y += gap;

            Lbl(this.LblFOCQty, "FOC Qty", lX, y); this.TxtFOCQty.Location = new Point(eX, y); this.TxtFOCQty.Width = 150; y += gap;

            Lbl(this.LblRebateQty, "Rebate Qty (%)", lX, y); this.TxtRebateQty.Location = new Point(eX, y); this.TxtRebateQty.Width = 150; y += gap + 8;

            this.GrpDetail.Controls.Add(this.LblCode); this.GrpDetail.Controls.Add(this.TxtCode); this.GrpDetail.Controls.Add(this.ChkInactive);
            this.GrpDetail.Controls.Add(this.LblDesc); this.GrpDetail.Controls.Add(this.TxtDesc);
            this.GrpDetail.Controls.Add(this.LblStock); this.GrpDetail.Controls.Add(this.TxtStockCode);
            this.GrpDetail.Controls.Add(this.LblMultiPrice); this.GrpDetail.Controls.Add(this.TxtMultiPriceCode);
            this.GrpDetail.Controls.Add(this.LblChargesRate); this.GrpDetail.Controls.Add(this.TxtChargesRate);
            this.GrpDetail.Controls.Add(this.LblMinCharges); this.GrpDetail.Controls.Add(this.TxtMinCharges);
            this.GrpDetail.Controls.Add(this.LblFOCQty); this.GrpDetail.Controls.Add(this.TxtFOCQty);
            this.GrpDetail.Controls.Add(this.LblRebateQty); this.GrpDetail.Controls.Add(this.TxtRebateQty);

            this.Controls.Add(this.GridMT); this.Controls.Add(this.GrpDetail);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.ResumeLayout(false);
        }

        // Toolbar button: 50px tall with the icon on the left (icons assigned at runtime).
        private static void Tb(SimpleButton b, string t, int x, int y, int w)
        { b.Text=t; b.Location=new Point(x,y); b.Width=w; b.Height=50; b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft; }
        private static void Lbl(LabelControl l, string t, int x, int y) { l.Text=t; l.Location=new Point(x,y+3); }
        private static void AddCol(GridView gv, string f, string c, int w)
        { var col=new GridColumn(); col.FieldName=f; col.Caption=c; col.Visible=true; col.Width=w; col.VisibleIndex=gv.Columns.Count; gv.Columns.Add(col); }
    }
}
