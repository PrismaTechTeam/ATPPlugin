using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.ServiceItem.MasterForms
{
    partial class ServiceItemLst_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private LabelControl LblTitle;
        private SimpleButton BtnNew, BtnEdit, BtnDelete, BtnRefresh, BtnExit;
        private GridControl Grid; private GridView GridView;

        private void InitializeComponent()
        {
            this.LblTitle = new LabelControl();
            this.BtnNew = new SimpleButton(); this.BtnEdit = new SimpleButton(); this.BtnDelete = new SimpleButton();
            this.BtnRefresh = new SimpleButton(); this.BtnExit = new SimpleButton();
            this.Grid = new GridControl(); this.GridView = new GridView();
            this.SuspendLayout();

            this.Text = "Maintain Service Item";
            this.ClientSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(900, 550);

            this.LblTitle.Text = "Maintain Service Item";
            this.LblTitle.Appearance.Font = new Font("Tahoma", 16F, FontStyle.Bold);
            this.LblTitle.Appearance.ForeColor = Color.FromArgb(180, 20, 40);
            this.LblTitle.Location = new Point(14, 8);

            int tbY = 14, bw = 80;
            Tb(this.BtnNew,     "+ New",     766,  tbY, bw); this.BtnNew.Anchor     = AnchorStyles.Top | AnchorStyles.Right; this.BtnNew.Click     += new System.EventHandler(this.OnNew);
            Tb(this.BtnEdit,    "Edit",      851,  tbY, bw); this.BtnEdit.Anchor    = AnchorStyles.Top | AnchorStyles.Right; this.BtnEdit.Click    += new System.EventHandler(this.OnEdit);
            Tb(this.BtnDelete,  "Delete",    936,  tbY, bw); this.BtnDelete.Anchor  = AnchorStyles.Top | AnchorStyles.Right; this.BtnDelete.Click  += new System.EventHandler(this.OnDelete);
            Tb(this.BtnRefresh, "Refresh",   1021, tbY, bw); this.BtnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right; this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            Tb(this.BtnExit,    "Exit (F2)", 1106, tbY, bw); this.BtnExit.Anchor    = AnchorStyles.Top | AnchorStyles.Right; this.BtnExit.Click    += new System.EventHandler(this.OnExit);

            this.Grid.Location = new Point(14, 50);
            this.Grid.Size = new Size(1172, 640);
            this.Grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.GridView.GridControl = this.Grid;
            this.GridView.OptionsView.ShowGroupPanel = false;
            this.GridView.OptionsView.ShowAutoFilterRow = true;
            this.GridView.OptionsBehavior.Editable = false;
            this.GridView.OptionsSelection.EnableAppearanceFocusedRow = true;
            this.Grid.MainView = this.GridView;
            this.Grid.ViewCollection.Add(this.GridView);
            AddCol(this.GridView, "ServiceItemCode",    "Service Tag",    140);
            AddCol(this.GridView, "Description",        "Description",    240);
            AddCol(this.GridView, "StockCode",          "Stock Code",     130);
            AddCol(this.GridView, "GradeDescription",   "Grade",          120);
            AddCol(this.GridView, "PurchaseDate",       "Purchase Date",  100);
            AddCol(this.GridView, "ContractNo",         "Contract No.",   120);
            AddCol(this.GridView, "DebtorCode",         "Debtor Code",    100);
            AddCol(this.GridView, "DebtorName",         "Debtor Name",    200);
            AddCol(this.GridView, "ServiceStartDate",   "Start",          90);
            AddCol(this.GridView, "ServiceExpiryDate",  "Expiry",         90);

            this.Controls.Add(this.LblTitle);
            this.Controls.Add(this.BtnNew); this.Controls.Add(this.BtnEdit); this.Controls.Add(this.BtnDelete);
            this.Controls.Add(this.BtnRefresh); this.Controls.Add(this.BtnExit);
            this.Controls.Add(this.Grid);
            this.ResumeLayout(false);
        }

        private static void Tb(SimpleButton b, string t, int x, int y, int w) { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 28; }
        private static void AddCol(GridView gv, string f, string c, int w)
        { var col = new GridColumn(); col.FieldName = f; col.Caption = c; col.Visible = true; col.Width = w; col.VisibleIndex = gv.Columns.Count; gv.Columns.Add(col); }
    }
}
