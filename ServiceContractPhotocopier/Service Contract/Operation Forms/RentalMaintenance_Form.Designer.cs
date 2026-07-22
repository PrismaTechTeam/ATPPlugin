using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class RentalMaintenance_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private SimpleButton BtnSave, BtnAssign, BtnRefresh, BtnExit;
        private LabelControl LblCount;
        private GridControl GridRent; private GridView GridViewRent;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox RepoBasis;
        private DevExpress.XtraEditors.Repository.RepositoryItemDateEdit RepoStartDate;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.BtnSave = new SimpleButton(); this.BtnAssign = new SimpleButton();
            this.BtnRefresh = new SimpleButton(); this.BtnExit = new SimpleButton();
            this.LblCount = new LabelControl();
            this.GridRent = new GridControl(); this.GridViewRent = new GridView();
            this.RepoBasis = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.RepoStartDate = new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();

            this.SuspendLayout();
            this.Text = "Rental Maintenance"; this.ClientSize = new Size(1400, 760);
            this.StartPosition = FormStartPosition.CenterParent; this.MinimumSize = new Size(1100, 600);

            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Rental Maintenance";
            this.PanelHeaderTop.Hint = "All rental (flat-charge) meters across every machine. Edit inline and Save; Assign Rental adds a rental to a machine.";
            this.PanelHeaderTop.Location = new Point(0, 0);
            this.PanelHeaderTop.Size = new Size(1400, 56);

            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Location = new Point(0, 56);
            this.PanelToolbar.Size = new Size(1400, 62);
            Tb(this.BtnSave, "Save", 8, 6, 86); this.BtnSave.Click += new System.EventHandler(this.OnSave);
            Tb(this.BtnAssign, "Assign Rental", 98, 6, 130); this.BtnAssign.Click += new System.EventHandler(this.OnAssign);
            Tb(this.BtnRefresh, "Refresh", 232, 6, 92); this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            Tb(this.BtnExit, "Exit (F2)", 328, 6, 92); this.BtnExit.Click += new System.EventHandler(this.OnExit);
            this.LblCount.Text = ""; this.LblCount.Location = new Point(440, 26);
            this.PanelToolbar.Controls.Add(this.BtnSave); this.PanelToolbar.Controls.Add(this.BtnAssign);
            this.PanelToolbar.Controls.Add(this.BtnRefresh); this.PanelToolbar.Controls.Add(this.BtnExit);
            this.PanelToolbar.Controls.Add(this.LblCount);

            this.GridRent.Dock = DockStyle.Fill;
            this.GridViewRent.GridControl = this.GridRent;
            this.GridRent.MainView = this.GridViewRent; this.GridRent.ViewCollection.Add(this.GridViewRent);
            this.GridRent.RepositoryItems.Add(this.RepoBasis);
            this.GridRent.RepositoryItems.Add(this.RepoStartDate);

            this.Controls.Add(this.GridRent);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);
            this.ResumeLayout(false);
        }

        private static void Tb(SimpleButton b, string t, int x, int y, int w)
        { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 50; b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft; }
    }
}
