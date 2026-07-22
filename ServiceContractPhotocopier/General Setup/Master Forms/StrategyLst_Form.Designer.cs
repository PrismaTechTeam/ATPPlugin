using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    partial class StrategyLst_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private AutoCount.Controls.PanelHeader PanelHeaderTop;
        private PanelControl PanelToolbar;
        private SimpleButton BtnNew, BtnEdit, BtnCopyNew, BtnSave, BtnCancel, BtnDelete, BtnRefresh, BtnExit;
        private GridControl GridST; private GridView GridViewST;
        private GroupControl GrpDetail;
        private LabelControl LblCode, LblDesc, LblRemark;
        private TextEdit TxtCode, TxtDesc, TxtRemark;
        private CheckEdit ChkInactive;
        private ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl RulesEditor;

        private void InitializeComponent()
        {
            this.PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            this.PanelToolbar = new PanelControl();
            this.BtnNew = new SimpleButton(); this.BtnEdit = new SimpleButton(); this.BtnCopyNew = new SimpleButton();
            this.BtnSave = new SimpleButton(); this.BtnCancel = new SimpleButton(); this.BtnDelete = new SimpleButton();
            this.BtnRefresh = new SimpleButton(); this.BtnExit = new SimpleButton();
            this.GridST = new GridControl(); this.GridViewST = new GridView();
            this.GrpDetail = new GroupControl();
            this.LblCode = new LabelControl(); this.TxtCode = new TextEdit();
            this.LblDesc = new LabelControl(); this.TxtDesc = new TextEdit();
            this.LblRemark = new LabelControl(); this.TxtRemark = new TextEdit();
            this.ChkInactive = new CheckEdit();
            this.RulesEditor = new ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl();

            ((System.ComponentModel.ISupportInitialize)(this.GridST)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridViewST)).BeginInit();
            this.SuspendLayout();
            this.Text = "Strategy Maintenance"; this.ClientSize = new Size(1180, 820);
            this.StartPosition = FormStartPosition.CenterParent; this.MinimumSize = new Size(1040, 720);

            this.PanelHeaderTop.Dock = DockStyle.Top;
            this.PanelHeaderTop.Header = "Strategy Maintenance";
            this.PanelHeaderTop.Hint = "";
            this.PanelHeaderTop.Location = new Point(0, 0);
            this.PanelHeaderTop.Size = new Size(1180, 56);

            this.PanelToolbar.Dock = DockStyle.Top;
            this.PanelToolbar.Location = new Point(0, 56);
            this.PanelToolbar.Size = new Size(1180, 62);
            Tb(this.BtnNew, "New", 8, 6, 86); this.BtnNew.Click += new System.EventHandler(this.OnNew);
            Tb(this.BtnEdit, "Edit", 98, 6, 86); this.BtnEdit.Click += new System.EventHandler(this.OnEdit);
            Tb(this.BtnCopyNew, "Copy to New", 188, 6, 120); this.BtnCopyNew.Click += new System.EventHandler(this.OnCopyToNew);
            Tb(this.BtnSave, "Save", 312, 6, 86); this.BtnSave.Click += new System.EventHandler(this.OnSave);
            Tb(this.BtnCancel, "Cancel", 402, 6, 86); this.BtnCancel.Click += new System.EventHandler(this.OnCancel);
            Tb(this.BtnDelete, "Delete", 492, 6, 86); this.BtnDelete.Click += new System.EventHandler(this.OnDelete);
            Tb(this.BtnRefresh, "Refresh", 588, 6, 92); this.BtnRefresh.Click += new System.EventHandler(this.OnRefresh);
            Tb(this.BtnExit, "Exit (F2)", 684, 6, 92); this.BtnExit.Click += new System.EventHandler(this.OnExit);
            this.PanelToolbar.Controls.Add(this.BtnNew); this.PanelToolbar.Controls.Add(this.BtnEdit);
            this.PanelToolbar.Controls.Add(this.BtnCopyNew);
            this.PanelToolbar.Controls.Add(this.BtnSave); this.PanelToolbar.Controls.Add(this.BtnCancel);
            this.PanelToolbar.Controls.Add(this.BtnDelete); this.PanelToolbar.Controls.Add(this.BtnRefresh);
            this.PanelToolbar.Controls.Add(this.BtnExit);

            this.GridST.Location = new Point(14, 126); this.GridST.Size = new Size(1150, 168);
            this.GridST.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.GridViewST.GridControl = this.GridST; this.GridViewST.OptionsView.ShowGroupPanel = false; this.GridViewST.OptionsBehavior.Editable = false;
            this.GridViewST.OptionsView.ShowAutoFilterRow = true;
            this.GridST.MainView = this.GridViewST; this.GridST.ViewCollection.Add(this.GridViewST);

            this.GrpDetail.Text = "Detail"; this.GrpDetail.Location = new Point(14, 302); this.GrpDetail.Size = new Size(1150, 502);
            this.GrpDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            int lX = 14, eX = 150, eW = 260; int y = 28, gap = 28;
            Lbl(this.LblCode, "Strategy Code", lX, y); this.TxtCode.Location = new Point(eX, y); this.TxtCode.Width = eW;
            this.ChkInactive.Properties.Caption = "Inactive"; this.ChkInactive.Location = new Point(eX + eW + 20, y); y += gap;
            Lbl(this.LblDesc, "Description", lX, y); this.TxtDesc.Location = new Point(eX, y); this.TxtDesc.Width = 700; y += gap;
            Lbl(this.LblRemark, "Remark", lX, y); this.TxtRemark.Location = new Point(eX, y); this.TxtRemark.Width = 700; y += gap + 6;

            this.RulesEditor.Location = new Point(10, y); this.RulesEditor.Size = new Size(1132, 494 - y);
            this.RulesEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            this.GrpDetail.Controls.Add(this.LblCode); this.GrpDetail.Controls.Add(this.TxtCode); this.GrpDetail.Controls.Add(this.ChkInactive);
            this.GrpDetail.Controls.Add(this.LblDesc); this.GrpDetail.Controls.Add(this.TxtDesc);
            this.GrpDetail.Controls.Add(this.LblRemark); this.GrpDetail.Controls.Add(this.TxtRemark);
            this.GrpDetail.Controls.Add(this.RulesEditor);

            this.Controls.Add(this.GridST); this.Controls.Add(this.GrpDetail);
            this.Controls.Add(this.PanelToolbar);
            this.Controls.Add(this.PanelHeaderTop);

            ((System.ComponentModel.ISupportInitialize)(this.GridViewST)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GridST)).EndInit();
            this.ResumeLayout(false);
        }

        private static void Tb(SimpleButton b, string t, int x, int y, int w)
        { b.Text = t; b.Location = new Point(x, y); b.Width = w; b.Height = 50; b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft; }
        private static void Lbl(LabelControl l, string t, int x, int y) { l.Text = t; l.Location = new Point(x, y + 3); }
    }
}
