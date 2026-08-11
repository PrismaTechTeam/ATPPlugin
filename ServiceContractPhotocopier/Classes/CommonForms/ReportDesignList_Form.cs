using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using AutoCount.Report;
using DevExpress.XtraEditors;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// The report designs saved under one report type, with the operations AutoCount's own Report
    /// Design Center offers: New, Design, Rename, Set as Default, Delete.
    ///
    /// Two modes, one list. MANAGE is the screen behind a "Report Design" button. PICK is the same
    /// list asked "which one do you want to print with?" — the operator should not have to learn two
    /// different pictures of the same designs.
    ///
    /// New does not hand the operator an empty page. It starts an A4 LANDSCAPE report — a listing is
    /// always wider than it is tall — with the columns they are currently looking at already laid
    /// out, so the first save produces something printable instead of a blank sheet they have to
    /// build field by field.
    /// </summary>
    public partial class ReportDesignList_Form : XtraForm
    {
        /// <summary>One column to lay out in a new design: what to bind, what to call it, how wide
        /// relative to its neighbours, and how to format it.</summary>
        public class ReportColumn
        {
            public string Field = "";
            public string Caption = "";
            public int Width = 100;
            public string Format = "";      // "n0", "n2", "dd/MM/yyyy"; empty = plain text
            public bool RightAlign = false;
        }

        private DBSetting _db;
        private UserSession _us;
        private string _reportType = "";
        private object _designerDataSource;
        private List<ReportColumn> _columns;
        private bool _pickMode;
        private string _picked;

        public ReportDesignList_Form()
        {
            InitializeComponent();
        }

        private ReportDesignList_Form(DBSetting db, UserSession us, string reportType,
            object designerDataSource, List<ReportColumn> columns, bool pickMode, string prompt) : this()
        {
            _db = db;
            _us = us;
            _reportType = reportType ?? "";
            _designerDataSource = designerDataSource;
            _columns = columns;
            _pickMode = pickMode;
            LblPrompt.Text = string.IsNullOrEmpty(prompt) ? "Report designs for " + _reportType : prompt;
            BtnUse.Visible = pickMode;
            this.Text = pickMode ? "Choose a Report Design" : "Report Design — " + _reportType;
            if (pickMode) this.AcceptButton = BtnUse;
            LoadList();
        }

        /// <summary>Open the manager. Everything happens inside; nothing is returned.</summary>
        public static void Manage(IWin32Window owner, DBSetting db, UserSession us, string reportType,
            object designerDataSource, List<ReportColumn> columns)
        {
            using (ReportDesignList_Form f = new ReportDesignList_Form(db, us, reportType,
                designerDataSource, columns, false, null))
            {
                f.ShowDialog(owner);
            }
        }

        /// <summary>
        /// Ask which design to use. Returns the design name, or null if the operator backed out.
        /// With nothing saved yet it offers to create one rather than showing an empty list.
        /// </summary>
        public static string Pick(IWin32Window owner, DBSetting db, UserSession us, string reportType,
            object designerDataSource, List<ReportColumn> columns, string prompt)
        {
            using (ReportDesignList_Form f = new ReportDesignList_Form(db, us, reportType,
                designerDataSource, columns, true, prompt))
            {
                if (f.GridViewDesigns.RowCount == 0)
                {
                    if (XtraMessageBox.Show(
                            "There is no report design for " + reportType + " yet.\r\n\r\nCreate one now?",
                            "Report Design", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        f.ShowDialog(owner);
                    return null;
                }
                f.ShowDialog(owner);
                return f._picked;
            }
        }

        // ───────────────────────── list ─────────────────────────

        private void LoadList()
        {
            DataTable list = null;
            try { list = AutoCountReport.GetInstance().GetReportList(_db, _reportType); }
            catch { }

            DataTable t = new DataTable("Designs");
            t.Columns.Add("ReportName", typeof(string));
            t.Columns.Add("Kind", typeof(string));
            t.Columns.Add("Default", typeof(string));
            if (list != null)
                foreach (DataRow r in list.Rows)
                {
                    DataRow g = t.NewRow();
                    g["ReportName"] = Convert.ToString(r["ReportName"]);
                    // AutoCount's own "Type" is localized; ask the API instead of matching English.
                    g["Kind"] = IsSystem(Convert.ToString(r["ReportName"])) ? "Built-in" : "User";
                    g["Default"] = Convert.ToString(r["Default"]);
                    t.Rows.Add(g);
                }
            GridDesigns.DataSource = t;
            if (GridViewDesigns.RowCount > 0 && GridViewDesigns.FocusedRowHandle < 0)
                GridViewDesigns.FocusedRowHandle = 0;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            string name = SelectedName();
            bool any = name.Length > 0;
            bool system = any && IsSystem(name);
            BtnDesign.Enabled = any;
            BtnUse.Enabled = any;
            BtnSetDefault.Enabled = any;
            // A built-in layout belongs to AutoCount, not to this book — it can be copied by saving
            // under a new name in the designer, but never renamed or deleted out from under it.
            BtnRename.Enabled = any && !system;
            BtnDelete.Enabled = any && !system;
        }

        private bool IsSystem(string name)
        {
            try { return AutoCountReport.GetInstance().IsSystemReport(name); }
            catch { return false; }
        }

        private string SelectedName()
        {
            if (GridViewDesigns.FocusedRowHandle < 0) return "";
            object v = GridViewDesigns.GetFocusedRowCellValue("ReportName");
            return v == null || v == DBNull.Value ? "" : Convert.ToString(v);
        }

        // ───────────────────────── actions ─────────────────────────

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (_us == null) return;
            try
            {
                ReportTemplate tpl = AutoCountReport.GetInstance()
                    .NewReport(_reportType, _designerDataSource, _us);
                XtraReport xr = tpl != null ? tpl.Report as XtraReport : null;
                if (xr == null)
                {
                    XtraMessageBox.Show("Could not start a new report design.", "Report Design",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                InitNewReport(xr);
                ScpReportScripts.FixReferences(xr);
                // "" as the name is what tells AutoCount's designer this is NEW: its Save prompts for
                // a name instead of silently overwriting something.
                ReportDesigner.DesignReport(tpl, "", _us, DesignerSaved);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the report designer:\r\n" + ex.Message,
                    "Report Design", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDesign_Click(object sender, EventArgs e)
        {
            string name = SelectedName();
            if (name.Length == 0 || _us == null) return;
            try
            {
                ReportTemplate tpl = AutoCountReport.GetInstance()
                    .GetReport(name, _designerDataSource, _us, true);
                if (tpl == null) return;
                ScpReportScripts.FixReferences(tpl.Report as XtraReport);
                ReportDesigner.DesignReport(tpl, name, _us, DesignerSaved);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the report designer:\r\n" + ex.Message,
                    "Report Design", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>The designer runs on its own UI thread, so its save event arrives on that thread —
        /// hop back before touching this form's grid.</summary>
        private void DesignerSaved(object sender, EventArgs e)
        {
            try
            {
                if (!this.IsHandleCreated || this.IsDisposed) return;
                this.BeginInvoke(new MethodInvoker(LoadList));
            }
            catch { }
        }

        private void BtnRename_Click(object sender, EventArgs e)
        {
            string name = SelectedName();
            if (name.Length == 0 || IsSystem(name)) return;
            string newName = XtraInputBox.Show("New name for this report design:", "Rename", name);
            if (newName == null) return;
            newName = newName.Trim();
            if (newName.Length == 0 || newName == name) return;
            try
            {
                ReportDBUtil util = ReportDBUtil.Create(_db);
                util.RenameReport(name, newName);
                // Renaming leaves the book's default pointing at a name that no longer exists —
                // AutoCount's own rename has the same hole. Follow the rename instead.
                if (string.Equals(CurrentDefault(), name, StringComparison.OrdinalIgnoreCase))
                    util.SetDefaultReport(_reportType, newName);
                LoadList();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Rename failed:\r\n" + ex.Message, "Report Design",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSetDefault_Click(object sender, EventArgs e)
        {
            string name = SelectedName();
            if (name.Length == 0) return;
            try
            {
                bool already = string.Equals(CurrentDefault(), name, StringComparison.OrdinalIgnoreCase);
                // Pressing it on the design that is already default clears it — the same button says
                // "this one" and "actually, none", which is how AutoCount's own tick behaves.
                ReportDBUtil.Create(_db).SetDefaultReport(_reportType, already ? "" : name);
                LoadList();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not change the default:\r\n" + ex.Message, "Report Design",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string CurrentDefault()
        {
            try { return ReportDBUtil.Create(_db).GetDefaultReport(_reportType) ?? ""; }
            catch { return ""; }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            string name = SelectedName();
            if (name.Length == 0) return;
            if (IsSystem(name))
            {
                XtraMessageBox.Show("'" + name + "' is a built-in layout and cannot be deleted.",
                    "Report Design", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (XtraMessageBox.Show(
                    "Delete the report design '" + name + "'?\r\n\r\n" +
                    "Anything set to print with it falls back to the default layout. This cannot be undone.",
                    "Report Design", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            try
            {
                ReportDBUtil util = ReportDBUtil.Create(_db);
                util.DeleteReport(name);
                // Leaving the default pointing at a deleted design makes every later print resolve to
                // nothing; clear it so the fallback picks a layout that exists.
                if (string.Equals(CurrentDefault(), name, StringComparison.OrdinalIgnoreCase))
                    util.SetDefaultReport(_reportType, "");
                LoadList();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Report Design",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnUse_Click(object sender, EventArgs e)
        {
            string name = SelectedName();
            if (name.Length == 0) return;
            _picked = name;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void GridViewDesigns_DoubleClick(object sender, EventArgs e)
        {
            if (_pickMode) BtnUse_Click(sender, e);
            else BtnDesign_Click(sender, e);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            UpdateButtons();
        }

        // ───────────────────────── new-design layout ─────────────────────────

        /// <summary>
        /// Give a brand-new design the shape a listing needs: A4 landscape, sensible margins, a title
        /// block, a repeating column-heading row and a detail row bound to the same columns the
        /// operator is looking at on screen.
        ///
        /// AutoCount's NewReport returns a report with NO bands at all — the DevExpress design panel
        /// would add only margins and a detail band, which is why "New" used to open on a blank page.
        /// </summary>
        private void InitNewReport(XtraReport xr)
        {
            // AutoCount seeds every new report with a script that touches AutoCount.Report.BaseReport,
            // which drags AutoCount.UI.dll in as a script reference — the one reference most likely
            // not to resolve, and when it does not the whole preview is replaced by "There are errors
            // in scripts". A listing has no use for __report, so the handler stays (Scripts.
            // OnBeforePrint names it) and the dependency goes.
            xr.ScriptsSource =
                "private void Report_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)\r\n" +
                "{\r\n" +
                "}\r\n";
            xr.Scripts.OnBeforePrint = "Report_BeforePrint";

            xr.Landscape = true;
            xr.PaperKind = System.Drawing.Printing.PaperKind.A4;
            xr.Margins = new System.Drawing.Printing.Margins(40, 40, 45, 45);

            TopMarginBand top = new TopMarginBand();
            top.HeightF = 45f;
            xr.Bands.Add(top);

            ReportHeaderBand rh = new ReportHeaderBand();
            rh.HeightF = 46f;
            xr.Bands.Add(rh);

            PageHeaderBand ph = new PageHeaderBand();
            ph.HeightF = 24f;
            xr.Bands.Add(ph);

            DetailBand detail = new DetailBand();
            detail.HeightF = 22f;
            xr.Bands.Add(detail);

            BottomMarginBand bottom = new BottomMarginBand();
            bottom.HeightF = 45f;
            xr.Bands.Add(bottom);

            float usable = UsableWidth(xr);

            XRLabel title = new XRLabel();
            title.Text = _reportType;
            title.Font = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
            title.LocationF = new DevExpress.Utils.PointFloat(0f, 6f);
            title.SizeF = new System.Drawing.SizeF(usable, 26f);
            rh.Controls.Add(title);

            if (_columns == null || _columns.Count == 0) return;

            float totalWidth = 0f;
            for (int i = 0; i < _columns.Count; i++)
                totalWidth += _columns[i].Width <= 0 ? 100 : _columns[i].Width;
            float scale = totalWidth > 0f ? usable / totalWidth : 1f;

            XRTable headRow = NewTable(usable, 22f);
            XRTableRow hr = new XRTableRow();
            headRow.Rows.Add(hr);
            XRTable dataRow = NewTable(usable, 20f);
            XRTableRow dr = new XRTableRow();
            dataRow.Rows.Add(dr);

            for (int i = 0; i < _columns.Count; i++)
            {
                ReportColumn c = _columns[i];
                float w = (c.Width <= 0 ? 100 : c.Width) * scale;

                XRTableCell hc = new XRTableCell();
                hc.Text = c.Caption;
                hc.WidthF = w;
                hc.Font = new System.Drawing.Font("Segoe UI", 8f, System.Drawing.FontStyle.Bold);
                hc.Borders = DevExpress.XtraPrinting.BorderSide.Bottom | DevExpress.XtraPrinting.BorderSide.Top;
                hc.TextAlignment = c.RightAlign
                    ? DevExpress.XtraPrinting.TextAlignment.MiddleRight
                    : DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
                hr.Cells.Add(hc);

                XRTableCell dc = new XRTableCell();
                dc.WidthF = w;
                dc.Font = new System.Drawing.Font("Segoe UI", 8f);
                dc.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
                dc.TextAlignment = c.RightAlign
                    ? DevExpress.XtraPrinting.TextAlignment.MiddleRight
                    : DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
                if (c.Format.Length > 0) dc.TextFormatString = "{0:" + c.Format + "}";
                dc.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[" + c.Field + "]"));
                dr.Cells.Add(dc);
            }

            ph.Controls.Add(headRow);
            detail.Controls.Add(dataRow);
        }

        private static XRTable NewTable(float width, float height)
        {
            XRTable t = new XRTable();
            t.LocationF = new DevExpress.Utils.PointFloat(0f, 0f);
            t.SizeF = new System.Drawing.SizeF(width, height);
            return t;
        }

        /// <summary>Printable width in report units. PageWidth/PageHeight describe the PAPER, so the
        /// landscape swap has to be applied here rather than assumed.</summary>
        private static float UsableWidth(XtraReport xr)
        {
            int w = xr.PageWidth, h = xr.PageHeight;
            int across = xr.Landscape ? Math.Max(w, h) : Math.Min(w, h);
            float usable = across - xr.Margins.Left - xr.Margins.Right;
            return usable < 100f ? 100f : usable;
        }
    }
}
