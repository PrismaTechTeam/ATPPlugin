using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class MeterMultiPricingLst_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private SimpleButton BtnNew, BtnEdit, BtnSave, BtnCancel, BtnDelete, BtnRefresh, BtnExit;
        private GridControl GridMP; private GridView GridViewMP;
        private LabelControl LblCode, LblDesc;
        private TextEdit TxtCode, TxtDesc;
        private LabelControl LblItems;
        private GridControl GridItems; private GridView GridViewItems;
        private SimpleButton BtnAddRow, BtnDelRow, BtnRowUp, BtnRowDown;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.BtnNew = new SimpleButton(); this.BtnEdit = new SimpleButton();
            this.BtnSave = new SimpleButton(); this.BtnCancel = new SimpleButton();
            this.BtnDelete = new SimpleButton(); this.BtnRefresh = new SimpleButton(); this.BtnExit = new SimpleButton();
            this.GridMP = new GridControl(); this.GridViewMP = new GridView();
            this.LblCode = new LabelControl(); this.TxtCode = new TextEdit();
            this.LblDesc = new LabelControl(); this.TxtDesc = new TextEdit();
            this.LblItems = new LabelControl();
            this.GridItems = new GridControl(); this.GridViewItems = new GridView();
            this.BtnAddRow = new SimpleButton(); this.BtnDelRow = new SimpleButton();
            this.BtnRowUp = new SimpleButton(); this.BtnRowDown = new SimpleButton();

            this.SuspendLayout();
            this.Text = "Meter Multi Pricing"; this.ClientSize = new Size(1050, 700);
            this.StartPosition = FormStartPosition.CenterParent; this.MinimumSize = new Size(900, 600);

            // Native AutoCount green header (same as Maintain Service Contract / Item; hint hidden).
            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Meter Multi Pricing";
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

            // Top grid — MultiPrice codes
            this.GridMP.Location = new Point(14, 128); this.GridMP.Size = new Size(1020, 220);
            this.GridMP.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.GridViewMP.GridControl = this.GridMP; this.GridViewMP.OptionsView.ShowGroupPanel = false;
            this.GridViewMP.OptionsBehavior.Editable = false;
            this.GridMP.MainView = this.GridViewMP; this.GridMP.ViewCollection.Add(this.GridViewMP);
            AddCol(this.GridViewMP, "MeterMultiPriceCode", "Multi Pricing Code", 260);
            AddCol(this.GridViewMP, "Description", "Description", 720);

            // Detail fields
            Lbl(this.LblCode, "Multi Pricing Code :", 14, 362);
            this.TxtCode.Location = new Point(160, 359); this.TxtCode.Width = 300;
            Lbl(this.LblDesc, "Description :", 14, 392);
            this.TxtDesc.Location = new Point(160, 389); this.TxtDesc.Width = 720;

            // Pricing rates grid
            Lbl(this.LblItems, "Meter Pricing Rates :", 14, 422);
            Tb2(this.BtnAddRow, "+ Add Row", 780, 419, 85); this.BtnAddRow.Anchor = AnchorStyles.Top | AnchorStyles.Right; this.BtnAddRow.Click += new System.EventHandler(this.OnAddItemRow);
            Tb2(this.BtnDelRow, "- Delete Row", 870, 419, 95); this.BtnDelRow.Anchor = AnchorStyles.Top | AnchorStyles.Right; this.BtnDelRow.Click += new System.EventHandler(this.OnDeleteItemRow);
            Tb2(this.BtnRowUp, "Move Up", 970, 419, 30); this.BtnRowUp.Text = "↑"; this.BtnRowUp.Anchor = AnchorStyles.Top | AnchorStyles.Right; this.BtnRowUp.Click += new System.EventHandler(this.OnMoveItemUp);
            Tb2(this.BtnRowDown, "Move Down", 1005, 419, 30); this.BtnRowDown.Text = "↓"; this.BtnRowDown.Anchor = AnchorStyles.Top | AnchorStyles.Right; this.BtnRowDown.Click += new System.EventHandler(this.OnMoveItemDown);
            this.GridItems.Location = new Point(14, 444); this.GridItems.Size = new Size(1020, 242);
            this.GridItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.GridViewItems.GridControl = this.GridItems; this.GridViewItems.OptionsView.ShowGroupPanel = false;
            this.GridViewItems.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            this.GridItems.MainView = this.GridViewItems; this.GridItems.ViewCollection.Add(this.GridViewItems);
            AddCol(this.GridViewItems, "MeterReading", "Meter Reading (<=)", 350);
            AddCol(this.GridViewItems, "UnitPrice", "Unit Price (Base UOM)", 350);

            this.Controls.Add(this.GridMP);
            this.Controls.Add(this.LblCode); this.Controls.Add(this.TxtCode);
            this.Controls.Add(this.LblDesc); this.Controls.Add(this.TxtDesc);
            this.Controls.Add(this.LblItems); this.Controls.Add(this.GridItems);
            this.Controls.Add(this.BtnAddRow); this.Controls.Add(this.BtnDelRow);
            this.Controls.Add(this.BtnRowUp); this.Controls.Add(this.BtnRowDown);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.ResumeLayout(false);
        }

        // Toolbar button: 50px tall with the icon on the left (icons assigned at runtime).
        private static void Tb(SimpleButton b, string t, int x, int y, int w)
        { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 50; b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft; }
        // Small inline row button (Add/Delete Row, arrows).
        private static void Tb2(SimpleButton b, string t, int x, int y, int w) { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 28; }
        private static void Lbl(LabelControl l, string t, int x, int y) { l.Text = t; l.Location = new Point(x, y + 3); }
        private static void AddCol(GridView gv, string f, string c, int w)
        { var col = new GridColumn(); col.FieldName = f; col.Caption = c; col.Visible = true; col.Width = w; col.VisibleIndex = gv.Columns.Count; gv.Columns.Add(col); }
    }
}
