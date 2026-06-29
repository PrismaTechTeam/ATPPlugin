using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using ServiceContractPhotocopier.Data;
using ServiceContractPhotocopier.StockRequest;

namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Stock Request Task", MenuOrder = 440)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class StockRequestTask_Form : XtraForm
    {
        private const int RefreshSeconds = 5;
        private static readonly Color ColorNew      = Color.FromArgb(187, 222, 251); // light blue
        private static readonly Color ColorUpdate   = Color.FromArgb(255, 245, 157); // yellow
        private static readonly Color ColorComplete = Color.FromArgb(165, 214, 167); // green
        private static readonly Color ColorIgnore   = Color.FromArgb(207, 216, 220); // grey

        private DBSetting _dbSetting;
        private UserSession _userSession;
        private int _countdown = RefreshSeconds;
        private bool _suppressFilterCheck;
        // Track whether each grid has had its column setup done. After the first bind,
        // re-binds preserve user column changes (order, width, hide) via SaveLayoutToStream.
        private bool _issueColumnsReady;
        private bool _transferColumnsReady;
        // Shared Location dropdown editor — loaded once, applied to all Location columns.
        private DataTable _locationsDt;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _locationLookup;
        // Warning banner for technician-derived locations that don't exist in AutoCount yet.
        private DevExpress.XtraEditors.PanelControl _locBanner;
        private DevExpress.XtraEditors.LabelControl _lblLocWarn;
        private DevExpress.XtraEditors.SimpleButton _btnCreateLoc;
        private List<string> _missingLocations;

        public StockRequestTask_Form()
        {
            InitializeComponent();
            InitDefaults();
            SetupLocationBanner();
            ApplyButtonIcons();   // toolbar SVG icons (controls themselves now live in the designer)
            // Highlight cancellation-request transfer rows (approval=No on an already-generated id)
            this.GridViewTransfer.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewTransfer_RowStyle);
            // Highlight change/cancel-request stock issue rows (re-sent id with different/zero qty)
            this.GridViewIssue.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewIssue_RowStyle);
            this.Load    += new EventHandler(StockRequestTask_Form_Load);
            this.Resize  += new EventHandler(StockRequestTask_Form_Resize);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(StockRequestTask_Form_KeyDown);
            // Lock the checkbox cell on rows whose Status is Complete/Ignore
            this.GridViewIssue.ShowingEditor    += new System.ComponentModel.CancelEventHandler(GridView_ShowingEditor);
            this.GridViewTransfer.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridView_ShowingEditor);
            // Keep "Select All New..." ↔ "Unselect All New" in sync with manual tick changes
            this.GridViewIssue.CellValueChanged    += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridView_CellValueChanged);
            this.GridViewTransfer.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridView_CellValueChanged);
            // Double-click a Complete row → open the generated AutoCount document
            this.GridViewIssue.DoubleClick    += new EventHandler(GridIssue_DoubleClick);
            this.GridViewTransfer.DoubleClick += new EventHandler(GridTransfer_DoubleClick);
            // Status-color swatch column rendering
            this.GridViewIssue.CustomDrawCell    += new DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventHandler(GridView_CustomDrawCell);
            this.GridViewTransfer.CustomDrawCell += new DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventHandler(GridView_CustomDrawCell);
            // Right-click context: Export + Save/Load layout
            AttachGridMenu(this.GridIssue,    this.GridViewIssue,    "StockRequestTask.Issue");
            AttachGridMenu(this.GridTransfer, this.GridViewTransfer, "StockRequestTask.Transfer");
        }

        private void AttachGridMenu(DevExpress.XtraGrid.GridControl grid, GridView view, string layoutKey)
        {
            // Full native DevExpress grid menus (sort / group / column chooser / best fit /
            // filter editor / find panel / auto-filter row …).
            view.OptionsMenu.EnableColumnMenu = true;     // header right-click
            view.OptionsMenu.EnableFooterMenu = true;     // footer right-click
            view.OptionsMenu.EnableGroupPanelMenu = true; // group-by box right-click
            view.OptionsMenu.ShowConditionalFormattingItem = true;
            // (Group-By Box stays hidden by default; the column menu's "Show Group By Box" toggles it.)

            // Append Export + Layout actions to the native menus (DevExpress has no built-in export item).
            view.PopupMenuShowing -= GridView_PopupMenuShowing;
            view.PopupMenuShowing += GridView_PopupMenuShowing;

            TryLoadSavedLayout(view, layoutKey);
        }

        private void GridView_PopupMenuShowing(object sender, DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            GridView view = sender as GridView;
            if (view == null) return;
            DevExpress.XtraGrid.GridControl grid = view.GridControl;
            string layoutKey = (grid == this.GridIssue) ? "StockRequestTask.Issue" : "StockRequestTask.Transfer";

            DevExpress.Utils.Menu.DXMenuItem xls = new DevExpress.Utils.Menu.DXMenuItem(
                "Export to Excel...", (s, a) => ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", "xlsx"));
            xls.BeginGroup = true;
            e.Menu.Items.Add(xls);
            e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Export to PDF...", (s, a) => ExportGrid(grid, "PDF files (*.pdf)|*.pdf", "pdf")));
            e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Export to CSV...", (s, a) => ExportGrid(grid, "CSV files (*.csv)|*.csv", "csv")));
            e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Export to Text...", (s, a) => ExportGrid(grid, "Text files (*.txt)|*.txt", "txt")));

            DevExpress.Utils.Menu.DXMenuItem save = new DevExpress.Utils.Menu.DXMenuItem(
                "Save Layout", (s, a) => SaveGridLayout(view, layoutKey));
            save.BeginGroup = true;
            e.Menu.Items.Add(save);
            e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Load Layout", (s, a) => LoadGridLayout(view, layoutKey)));
            e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Reset Layout", (s, a) => ResetGridLayout(view, layoutKey)));
        }

        private void ExportGrid(DevExpress.XtraGrid.GridControl grid, string filter, string ext)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = filter;
                dlg.FileName = "StockRequest-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + ext;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    switch (ext)
                    {
                        case "xlsx": grid.ExportToXlsx(dlg.FileName); break;
                        case "pdf":  grid.ExportToPdf(dlg.FileName);  break;
                        case "csv":  grid.ExportToCsv(dlg.FileName);  break;
                        default:     grid.ExportToText(dlg.FileName); break;
                    }
                    try { System.Diagnostics.Process.Start(dlg.FileName); } catch { }
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show(this, "Export failed:\r\n" + ex.Message,
                        "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveGridLayout(GridView view, string layoutKey)
        {
            if (_dbSetting == null) return;
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    view.SaveLayoutToStream(ms);
                    string xml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    Data.PumsConfig.Set(_dbSetting, "GRID_LAYOUT::" + layoutKey, xml);
                }
                XtraMessageBox.Show(this, "Layout saved.", "Save Layout",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Save layout failed:\r\n" + ex.Message,
                    "Save Layout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadGridLayout(GridView view, string layoutKey) => TryLoadSavedLayout(view, layoutKey, true);

        private void TryLoadSavedLayout(GridView view, string layoutKey, bool notify = false)
        {
            if (_dbSetting == null) return;
            try
            {
                string xml = Data.PumsConfig.Get(_dbSetting, "GRID_LAYOUT::" + layoutKey, null);
                if (string.IsNullOrEmpty(xml)) { if (notify) XtraMessageBox.Show(this, "No saved layout."); return; }
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))
                    view.RestoreLayoutFromStream(ms);
            }
            catch { /* tolerate corrupted layout */ }
        }

        private void ResetGridLayout(GridView view, string layoutKey)
        {
            if (_dbSetting == null) return;
            Data.PumsConfig.Set(_dbSetting, "GRID_LAYOUT::" + layoutKey, string.Empty);
            LoadGrids();
        }

        private void GridIssue_DoubleClick(object sender, EventArgs e)    => OpenGeneratedDocFromGrid((GridView)sender, false);
        private void GridTransfer_DoubleClick(object sender, EventArgs e) => OpenGeneratedDocFromGrid((GridView)sender, true);

        private void OpenGeneratedDocFromGrid(GridView v, bool isTransfer)
        {
            if (v == null || v.FocusedRowHandle < 0 || _userSession == null) return;
            string status = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Status"));
            if (!string.Equals(status, "Complete", StringComparison.OrdinalIgnoreCase)) return;

            object docObj = v.GetRowCellValue(v.FocusedRowHandle, "GeneratedDocNo");
            if (docObj == null || docObj == DBNull.Value) return;
            string docNo = docObj.ToString();
            if (string.IsNullOrWhiteSpace(docNo)) return;

            try
            {
                if (isTransfer)
                    AutoCount.Stock.StockTransfer.FormStockTransferCmd.EditDocument(_userSession, docNo);
                else
                    AutoCount.Stock.StockIssue.FormStockIssueCmd.EditDocument(_userSession, docNo);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this,
                    "Could not open AutoCount document '" + docNo + "':\r\n\r\n" + ex.Message,
                    "Open Document", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridView_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            string field = e.Column.FieldName ?? string.Empty;

            if (string.Equals(field, "Selected", StringComparison.OrdinalIgnoreCase))
            {
                bool anyNewTicked = AnyNewRowTicked(this.GridIssue.DataSource as DataTable)
                                 || AnyNewRowTicked(this.GridTransfer.DataSource as DataTable);
                this.BtnGenerateSISTAll.Text = anyNewTicked ? "Unselect All New" : "Select All New...";
                return;
            }
            // Persist Location override edits to the underlying Z_Pums table.
            if (IsLocationField(field) && _dbSetting != null)
            {
                GridView v = sender as GridView;
                if (v == null || e.RowHandle < 0) return;
                object keyObj = v.GetRowCellValue(e.RowHandle, "AutoKey");
                if (keyObj == null || keyObj == DBNull.Value) return;
                long autoKey = Convert.ToInt64(keyObj);
                string newValue = e.Value == null ? null : Convert.ToString(e.Value);
                try
                {
                    if (v == this.GridViewIssue)
                    {
                        StockRequestRepository.SetIssueLocationOverride(_dbSetting, autoKey, newValue);
                    }
                    else if (v == this.GridViewTransfer)
                    {
                        if (string.Equals(field, "FromLocation", StringComparison.OrdinalIgnoreCase))
                            StockRequestRepository.SetTransferFromOverride(_dbSetting, autoKey, newValue);
                        else if (string.Equals(field, "ToLocation", StringComparison.OrdinalIgnoreCase))
                            StockRequestRepository.SetTransferToOverride(_dbSetting, autoKey, newValue);

                        // Inline validation: From == To is invalid. Pop the message box
                        // immediately so the operator notices before clicking Generate.
                        string from = Convert.ToString(v.GetRowCellValue(e.RowHandle, "FromLocation")) ?? string.Empty;
                        string to   = Convert.ToString(v.GetRowCellValue(e.RowHandle, "ToLocation"))   ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(to)
                            && string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            XtraMessageBox.Show(this,
                                "From Location and To Location cannot be the same ('" + from.Trim() + "').\r\n\r\n"
                                + "Pick a different value for one of them.",
                                "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show(this,
                        "Failed to save Location:\r\n\r\n" + ex.Message,
                        "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void StockRequestTask_Form_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Alt+0 → open the test-data cleanup dialog
            if (e.Control && e.Alt && e.KeyCode == Keys.D0)
            {
                if (_dbSetting == null || _userSession == null) return;
                using (DeleteTestData_Form dlg = new DeleteTestData_Form(_dbSetting, _userSession))
                    dlg.ShowDialog(this);
                LoadGrids();
                ResetCountdown();
                e.Handled = true;
            }

            // Ctrl+Shift+Delete → password-gated hard delete of the focused row (+ its AutoCount doc).
            if (e.Control && e.Shift && e.KeyCode == Keys.Delete)
            {
                PurgeFocusedRow();
                e.Handled = true;
            }
        }

        // Permanently delete the focused row from the PUMS table; if it already generated an
        // AutoCount document, delete that document too. Gated by the password 'atp09'.
        private void PurgeFocusedRow()
        {
            if (_dbSetting == null) return;
            GridView v; bool isTransfer; string table, idCol;
            if (this.GridIssue.ContainsFocus) { v = this.GridViewIssue; isTransfer = false; table = "Z_PumsStockIssue"; idCol = "StockIssueId"; }
            else if (this.GridTransfer.ContainsFocus) { v = this.GridViewTransfer; isTransfer = true; table = "Z_PumsStockTransfer"; idCol = "RequestId"; }
            else
            {
                XtraMessageBox.Show(this, "Click a row in the Stock Issue or Stock Transfer grid first, then press Ctrl+Shift+Delete.",
                    "Delete row", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (v.FocusedRowHandle < 0) return;

            long autoKey = Convert.ToInt64(v.GetRowCellValue(v.FocusedRowHandle, "AutoKey"));
            string id = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, idCol));
            object docObj = v.GetRowCellValue(v.FocusedRowHandle, "GeneratedDocNo");
            string docNo = (docObj == null || docObj == DBNull.Value) ? "" : docObj.ToString();

            string pwd = ShowPasswordPrompt();
            if (pwd == null) return;                 // cancelled
            if (pwd != "atp09")
            {
                XtraMessageBox.Show(this, "Incorrect password — nothing deleted.", "Delete row",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserSession session = _userSession ?? UserSession.CurrentUserSession;
            try
            {
                // Delete the generated AutoCount document first (if any).
                if (!string.IsNullOrWhiteSpace(docNo) && session != null)
                {
                    if (isTransfer)
                        AutoCount.Stock.StockTransfer.StockTransferCommand.Create(session, _dbSetting).Delete(docNo);
                    else
                        AutoCount.Stock.StockIssue.StockIssueCommand.Create(session, _dbSetting).Delete(docNo);
                }
                // Delete the PUMS row.
                using (System.Data.SqlClient.SqlConnection cn = new System.Data.SqlClient.SqlConnection(_dbSetting.ConnectionString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(
                    "DELETE FROM " + table + " WHERE AutoKey=@k", cn))
                {
                    cmd.Parameters.AddWithValue("@k", autoKey);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                PumsLog.Write(_dbSetting, PumsLog.TYPE_WARN, isTransfer ? "DeleteStockTransferRow" : "DeleteStockIssueRow", id,
                    "Row deleted via Ctrl+Shift+Delete" + (string.IsNullOrWhiteSpace(docNo) ? "." : " (document " + docNo + " also deleted)."),
                    null, docNo, session != null ? session.LoginUserID : null);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Delete failed:\r\n" + ex.Message, "Delete row",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadGrids();
            ResetCountdown();
            XtraMessageBox.Show(this, "Deleted " + id +
                (string.IsNullOrWhiteSpace(docNo) ? "." : " and its AutoCount document " + docNo + "."),
                "Delete row", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Small modal password prompt; returns the entered text, or null if cancelled.
        private string ShowPasswordPrompt()
        {
            using (XtraForm f = new XtraForm())
            {
                f.Text = "Confirm delete";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
                f.ClientSize = new Size(330, 120);
                LabelControl lbl = new LabelControl();
                lbl.Text = "Enter password to permanently delete this row\r\n(and its AutoCount document, if any):";
                lbl.Location = new Point(16, 14); lbl.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
                lbl.Size = new Size(300, 34);
                TextEdit txt = new TextEdit();
                txt.Location = new Point(16, 54); txt.Width = 298;
                txt.Properties.UseSystemPasswordChar = true;
                SimpleButton ok = new SimpleButton { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(158, 86), Width = 75 };
                SimpleButton cancel = new SimpleButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(239, 86), Width = 75 };
                f.AcceptButton = ok; f.CancelButton = cancel;
                f.Controls.Add(lbl); f.Controls.Add(txt); f.Controls.Add(ok); f.Controls.Add(cancel);
                return f.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
            }
        }

        private void GridView_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GridView v = sender as GridView;
            if (v == null || v.FocusedColumn == null) return;
            string field  = v.FocusedColumn.FieldName ?? string.Empty;
            string status = Convert.ToString(v.GetRowCellValue(v.FocusedRowHandle, "Status"));

            if (string.Equals(field, "Selected", StringComparison.OrdinalIgnoreCase))
            {
                // Only Complete rows are locked. Ignore rows can still be re-ticked so the
                // operator can generate the SI/ST for them after un-ignoring intent.
                if (status == "Complete") e.Cancel = true;
                return;
            }
            // Location dropdowns — editable only on New / Ignore rows.
            // Update rows are mid-lifecycle and Complete rows are already posted to AutoCount.
            if (IsLocationField(field))
            {
                bool editable = string.Equals(status, "New",    StringComparison.OrdinalIgnoreCase)
                             || string.Equals(status, "Ignore", StringComparison.OrdinalIgnoreCase);
                if (!editable) e.Cancel = true;
            }
        }

        private static bool IsLocationField(string field)
        {
            return string.Equals(field, "Location",     StringComparison.OrdinalIgnoreCase)
                || string.Equals(field, "FromLocation", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field, "ToLocation",   StringComparison.OrdinalIgnoreCase);
        }

        private void ChkShowGrid_CheckedChanged(object sender, EventArgs e)
        {
            // At least one must remain on — re-tick the one that was just turned off.
            if (!this.ChkShowIssue.Checked && !this.ChkShowTransfer.Checked)
            {
                CheckEdit other = (sender == this.ChkShowIssue) ? this.ChkShowTransfer : this.ChkShowIssue;
                other.Checked = true;
                return;
            }
            // Hiding a grid also clears its ticks so a later Generate doesn't pick up
            // selections the operator can no longer see.
            if (!this.ChkShowIssue.Checked)    UntickAllSelected(this.GridIssue.DataSource as DataTable);
            if (!this.ChkShowTransfer.Checked) UntickAllSelected(this.GridTransfer.DataSource as DataTable);
            this.GridViewIssue.RefreshData();
            this.GridViewTransfer.RefreshData();
            ApplyShowLayout();
        }

        private void ApplyShowLayout()
        {
            // Both on  → splitter visible, both grids 50/50
            // Issue only      → Panel2 collapsed; Panel1 (Issue) fills the form
            // Transfer only   → Panel1 collapsed; Panel2 (Transfer) fills the form
            bool both = this.ChkShowIssue.Checked && this.ChkShowTransfer.Checked;
            if (both)
            {
                this.SplitContainer.Collapsed = false;
                this.SplitContainer.CollapsePanel = DevExpress.XtraEditors.SplitCollapsePanel.None;
                CenterSplitter();
            }
            else if (this.ChkShowIssue.Checked)
            {
                this.SplitContainer.CollapsePanel = DevExpress.XtraEditors.SplitCollapsePanel.Panel2;
                this.SplitContainer.Collapsed = true;
            }
            else
            {
                this.SplitContainer.CollapsePanel = DevExpress.XtraEditors.SplitCollapsePanel.Panel1;
                this.SplitContainer.Collapsed = true;
            }
        }

        private void StockRequestTask_Form_Load(object sender, EventArgs e)
        {
            ApplyShowLayout();
        }

        private void StockRequestTask_Form_Resize(object sender, EventArgs e)
        {
            CenterSplitter();
        }

        private void CenterSplitter()
        {
            // DevExpress: Horizontal=true → side-by-side (SplitterPosition along X / Width).
            //             Horizontal=false → top/bottom (SplitterPosition along Y / Height).
            int extent = this.SplitContainer.Horizontal
                ? this.SplitContainer.Width
                : this.SplitContainer.Height;
            if (extent > 0) this.SplitContainer.SplitterPosition = extent / 2;
        }

        public StockRequestTask_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            LoadGrids();
            ResetCountdown();
            this.RefreshTimer.Start();
        }

        public StockRequestTask_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            LoadGrids();
            ResetCountdown();
            this.RefreshTimer.Start();
        }

        private void InitDefaults()
        {
            DateTime today = DateTime.Today;
            this.DtFrom.DateTime = today.AddMonths(-1);
            this.DtTo.DateTime   = today.AddDays(1);
            this.ChkPendingOnly.Checked    = true;
            this.ChkFilterIssue.Checked    = false; // both unticked → search applies to both grids
            this.ChkFilterTransfer.Checked = false;
            this.ChkFilterBoth.Checked     = false; // legacy, hidden
            this.ChkShowIssue.Checked      = true;
            this.ChkShowTransfer.Checked   = true;
        }

        // ---------- Missing-location warning banner + auto-create ----------

        // Builds the (initially hidden) amber banner that sits between the filter panel and the
        // grids. Created in code to avoid touching the strict designer file.
        private void SetupLocationBanner()
        {
            _locBanner = new DevExpress.XtraEditors.PanelControl();
            _locBanner.Dock = DockStyle.Top;
            _locBanner.Height = 38;
            _locBanner.Appearance.BackColor = Color.FromArgb(255, 243, 205); // soft amber
            _locBanner.Appearance.Options.UseBackColor = true;
            _locBanner.Visible = false;

            _lblLocWarn = new DevExpress.XtraEditors.LabelControl();
            _lblLocWarn.Dock = DockStyle.Fill;
            _lblLocWarn.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            _lblLocWarn.Appearance.Font = new Font("Segoe UI", 9.5F);
            _lblLocWarn.Appearance.Options.UseFont = true;
            _lblLocWarn.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            _lblLocWarn.Padding = new Padding(12, 0, 0, 0);

            _btnCreateLoc = new DevExpress.XtraEditors.SimpleButton();
            _btnCreateLoc.Text = "Auto-create Locations";
            _btnCreateLoc.Dock = DockStyle.Right;
            _btnCreateLoc.Width = 210;
            _btnCreateLoc.Click += new EventHandler(BtnCreateLoc_Click);

            _locBanner.Controls.Add(_lblLocWarn);   // fill (added first → back → fills remainder)
            _locBanner.Controls.Add(_btnCreateLoc); // right (front → docks first)
            this.Controls.Add(_locBanner);
            // Place between the Fill SplitContainer (index 0) and the Top filter panel, so the
            // banner shows directly above the grids.
            this.Controls.SetChildIndex(_locBanner, 1);
        }

        // Scans the loaded grids for the (normalized) locations they will use and shows the banner
        // when any aren't in the AutoCount Location master.
        private void RefreshLocationBanner()
        {
            if (_locBanner == null || _dbSetting == null) return;
            HashSet<string> wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectLocs(this.GridIssue.DataSource as DataTable, wanted, "Location");
            CollectLocs(this.GridTransfer.DataSource as DataTable, wanted, "FromLocation", "ToLocation");

            List<string> missing;
            try { missing = StockRequestRepository.FilterMissingLocations(_dbSetting, wanted); }
            catch { missing = new List<string>(); }
            _missingLocations = missing;

            if (missing.Count == 0) { _locBanner.Visible = false; return; }
            int show = Math.Min(missing.Count, 8);
            string list = string.Join(", ", missing.GetRange(0, show).ToArray()) + (missing.Count > show ? " …" : "");
            _lblLocWarn.Text = "⚠  Found " + missing.Count +
                " technician name(s) not in AutoCount Stock Location: " + list +
                "   —  auto-create?";
            _btnCreateLoc.Text = "Auto-create " + missing.Count + " Location(s)";
            _locBanner.Visible = true;
        }

        private void CollectLocs(DataTable dt, HashSet<string> set, params string[] fields)
        {
            if (dt == null) return;
            foreach (string f in fields)
            {
                if (!dt.Columns.Contains(f)) continue;
                foreach (DataRow r in dt.Rows)
                {
                    string code = SiStGenerator.NormalizeLocation(Convert.ToString(r[f]));
                    if (!string.IsNullOrWhiteSpace(code)) set.Add(code);
                }
            }
        }

        private void BtnCreateLoc_Click(object sender, EventArgs e)
        {
            if (_missingLocations == null || _missingLocations.Count == 0) return;
            UserSession session = _userSession ?? UserSession.CurrentUserSession;
            if (session == null)
            {
                XtraMessageBox.Show(this, "No active AutoCount session — cannot create locations.",
                    "Auto-create Locations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (XtraMessageBox.Show(this,
                    "Create the following " + _missingLocations.Count + " Stock Location(s) in AutoCount?\r\n\r\n" +
                    string.Join(", ", _missingLocations.ToArray()),
                    "Auto-create Locations", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int created = 0;
            List<string> errs = new List<string>();
            try
            {
                AutoCount.Stock.Location.LocationMaintenance lm =
                    AutoCount.Stock.Location.LocationMaintenance.CreateLocationMaint(session, _dbSetting);
                foreach (string code in _missingLocations)
                {
                    try
                    {
                        AutoCount.Stock.Location.LocationEntity ent = lm.NewLocation();
                        ent.Location = code;
                        ent.Description = code;   // technician name as description
                        ent.IsActive = "Y";
                        ent.Save();
                        created++;
                        PumsLog.Write(_dbSetting, PumsLog.TYPE_INFO, "AutoCreateLocation", code,
                            "Stock Location auto-created from technician name.", null, null,
                            session.LoginUserID);
                    }
                    catch (Exception ex) { errs.Add(code + ": " + SiStGenerator_ShortError(ex)); }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Failed to create locations:\r\n" + ex.Message,
                    "Auto-create Locations", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Refresh the shared Location dropdown source so the new codes are pickable.
            try
            {
                if (_locationsDt != null)
                {
                    DataTable fresh = StockRequestRepository.LoadLocations(_dbSetting);
                    _locationsDt = fresh;
                    if (_locationLookup != null) _locationLookup.DataSource = fresh;
                }
            }
            catch { /* non-fatal */ }

            LoadGrids();   // re-evaluates the banner (hides it when nothing is missing)

            string msg = created + " location(s) created.";
            if (errs.Count > 0) msg += "\r\n\r\nFailed:\r\n" + string.Join("\r\n", errs.ToArray());
            XtraMessageBox.Show(this, msg, "Auto-create Locations", MessageBoxButtons.OK,
                errs.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private static string SiStGenerator_ShortError(Exception ex)
        {
            string m = ex.Message ?? ex.ToString();
            return m.Length > 200 ? m.Substring(0, 200) : m;
        }

        // ---------- Data loading ----------

        private void EnsureLocationLookup()
        {
            if (_locationLookup != null || _dbSetting == null) return;
            try { _locationsDt = StockRequestRepository.LoadLocations(_dbSetting); }
            catch { _locationsDt = new DataTable(); }

            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit r =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            r.DataSource    = _locationsDt;
            r.ValueMember   = "Location";
            r.DisplayMember = "Location";
            r.NullText      = "";
            r.PopupView.OptionsBehavior.AutoPopulateColumns = true;
            this.GridIssue.RepositoryItems.Add(r);
            this.GridTransfer.RepositoryItems.Add(r);
            _locationLookup = r;
        }

        private void ApplyLocationEditor(GridView v, string fieldName)
        {
            if (_locationLookup == null) return;
            DevExpress.XtraGrid.Columns.GridColumn col = v.Columns.ColumnByFieldName(fieldName);
            if (col == null) return;
            col.ColumnEdit = _locationLookup;
            col.OptionsColumn.AllowEdit  = true;
            col.OptionsColumn.ReadOnly   = false;
            col.OptionsColumn.AllowFocus = true;
        }

        private void LoadGrids()
        {
            if (_dbSetting == null) return;
            EnsureLocationLookup();

            DateTime fromDate = this.DtFrom.DateTime.Date;
            DateTime toDate   = this.DtTo.DateTime.Date;
            string search     = this.TxtSearch.Text ?? string.Empty;
            bool pendingOnly  = this.ChkPendingOnly.Checked;
            bool hideIgnore   = _chkHideIgnore != null && _chkHideIgnore.Checked;
            // Always load both grids; visibility is driven by ChkShowIssue / ChkShowTransfer
            // via the SplitContainer's CollapsePanel — independent of data fetch.
            const bool showIssue    = true;
            const bool showTransfer = true;

            // "Filter by …" scope: if a scope checkbox is ticked, the search string only
            // applies to that grid. The other grid gets an empty search (date/pending still apply).
            // If neither is ticked → search applies to both (default).
            bool scopeIssue    = this.ChkFilterIssue.Checked;
            bool scopeTransfer = this.ChkFilterTransfer.Checked;
            bool noScope       = !scopeIssue && !scopeTransfer;
            string searchForIssue    = (noScope || scopeIssue)    ? search : string.Empty;
            string searchForTransfer = (noScope || scopeTransfer) ? search : string.Empty;

            try
            {
                if (showIssue)
                {
                    DataTable dt = StockRequestRepository.LoadStockIssue(_dbSetting, fromDate, toDate, searchForIssue, pendingOnly, hideIgnore);
                    ReapplySelections(dt, _reselectIssue);
                    RebindPreservingLayout(this.GridIssue, this.GridViewIssue, dt, _issueColumnsReady,
                        () =>
                        {
                            HideColumn(this.GridViewIssue, "AutoKey");
                            HideColumn(this.GridViewIssue, "StockIssueId");
                            HideColumn(this.GridViewIssue, "Description");
                            // Location column is now shown (user request) — it's
                            // populated by SQL with Technician/AutoCount value.
                            FormatDateTimeColumn(this.GridViewIssue, "ReceivedAt");
                            FormatDateTimeColumn(this.GridViewIssue, "CompletedAt");
                            LockColumnsExceptSelected(this.GridViewIssue);
                            ApplyLocationEditor(this.GridViewIssue, "Location");
                            this.GridViewIssue.BestFitColumns();
                            _issueColumnsReady = true;
                        });
                    // Re-apply on every bind — layout restore may clobber ColumnEdit.
                    ApplyLocationEditor(this.GridViewIssue, "Location");
                }
                else
                {
                    this.GridIssue.DataSource = null;
                }

                if (showTransfer)
                {
                    string defaultLoc = PumsConfig.Get(_dbSetting,
                        PumsConfig.KEY_DEFAULT_FROM_LOCATION,
                        PumsConfig.DEFAULT_FROM_LOCATION_VALUE);
                    DataTable dt = StockRequestRepository.LoadStockTransfer(
                        _dbSetting, fromDate, toDate, searchForTransfer, pendingOnly, defaultLoc, hideIgnore);
                    ReapplySelections(dt, _reselectTransfer);
                    RebindPreservingLayout(this.GridTransfer, this.GridViewTransfer, dt, _transferColumnsReady,
                        () =>
                        {
                            HideColumn(this.GridViewTransfer, "AutoKey");
                            FormatDateTimeColumn(this.GridViewTransfer, "ReceivedAt");
                            FormatDateTimeColumn(this.GridViewTransfer, "CompletedAt");
                            // From/To Location placed right after Unit; TransferType after ToLocation
                            MoveColumnAfter(this.GridViewTransfer, "FromLocation", "Unit");
                            MoveColumnAfter(this.GridViewTransfer, "ToLocation",   "FromLocation");
                            MoveColumnAfter(this.GridViewTransfer, "TransferType", "ToLocation");
                            LockColumnsExceptSelected(this.GridViewTransfer);
                            ApplyLocationEditor(this.GridViewTransfer, "FromLocation");
                            ApplyLocationEditor(this.GridViewTransfer, "ToLocation");
                            this.GridViewTransfer.BestFitColumns();
                            _transferColumnsReady = true;
                        });
                    // Re-apply on every bind — layout restore may clobber ColumnEdit.
                    ApplyLocationEditor(this.GridViewTransfer, "FromLocation");
                    ApplyLocationEditor(this.GridViewTransfer, "ToLocation");
                }
                else
                {
                    this.GridTransfer.DataSource = null;
                }

                // Selections have been consumed (or were null) — clear so subsequent
                // Filter / Reset clicks start with a clean tick state.
                _reselectIssue = null;
                _reselectTransfer = null;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "Failed to load Stock Request data:\n\n" + ex.Message,
                    "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // After reload, sync the toggle button to actual tick state (preserved selections
            // may still leave New rows checked).
            bool anyNewTicked = AnyNewRowTicked(this.GridIssue.DataSource as DataTable)
                             || AnyNewRowTicked(this.GridTransfer.DataSource as DataTable);
            this.BtnGenerateSISTAll.Text = anyNewTicked ? "Unselect All New" : "Select All New...";

            RefreshLocationBanner();
        }

        // ---------- Auto-refresh countdown ----------

        // 5-second auto-refresh. The countdown is shown in the Refresh button's caption.
        private void ResetCountdown()
        {
            _countdown = RefreshSeconds;
            this.BtnRefresh.Text = "Refresh (" + _countdown + ")";
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _countdown--;
            if (_countdown <= 0)
            {
                CaptureSelections();
                LoadGrids();
                ResetCountdown();
                return;
            }
            this.BtnRefresh.Text = "Refresh (" + _countdown + ")";
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            CaptureSelections();
            LoadGrids();
            ResetCountdown();
        }

        // ---- Preserve ticked rows across an auto/manual refresh ----

        private System.Collections.Generic.HashSet<long> _reselectIssue;
        private System.Collections.Generic.HashSet<long> _reselectTransfer;

        private void CaptureSelections()
        {
            // DevExpress only writes a cell editor's pending value back to the bound
            // DataTable when the editor closes / row commits. If we read the table
            // mid-edit (e.g. the timer fires while the checkbox cell is still active)
            // the freshly-ticked row looks unticked. Force a commit on both grids first.
            CommitGridEdits(this.GridViewIssue);
            CommitGridEdits(this.GridViewTransfer);

            _reselectIssue    = CollectSelectedAutoKeys(this.GridIssue.DataSource as DataTable);
            _reselectTransfer = CollectSelectedAutoKeys(this.GridTransfer.DataSource as DataTable);
        }

        private static void CommitGridEdits(GridView v)
        {
            if (v == null) return;
            try
            {
                v.CloseEditor();      // ends the active in-cell editor, fires CellValueChanged
                v.UpdateCurrentRow(); // pushes the row's pending edits into the data source
            }
            catch { /* nothing to commit */ }
        }

        private static System.Collections.Generic.HashSet<long> CollectSelectedAutoKeys(DataTable dt)
        {
            System.Collections.Generic.HashSet<long> set = new System.Collections.Generic.HashSet<long>();
            if (dt == null || !dt.Columns.Contains("Selected") || !dt.Columns.Contains("AutoKey")) return set;
            foreach (DataRow r in dt.Rows)
            {
                if (r["Selected"] is bool b && b) set.Add(Convert.ToInt64(r["AutoKey"]));
            }
            return set;
        }

        private static void ReapplySelections(DataTable dt, System.Collections.Generic.HashSet<long> keys)
        {
            if (dt == null || keys == null || keys.Count == 0) return;
            if (!dt.Columns.Contains("Selected") || !dt.Columns.Contains("AutoKey")) return;
            foreach (DataRow r in dt.Rows)
            {
                if (keys.Contains(Convert.ToInt64(r["AutoKey"]))) r["Selected"] = true;
            }
        }

        // ---------- "Filter by …" scope checkboxes (independent toggles) ----------

        private void ChkFilterIssue_CheckedChanged(object sender, EventArgs e)    { /* applied on next Filter click */ }
        private void ChkFilterTransfer_CheckedChanged(object sender, EventArgs e) { /* applied on next Filter click */ }
        private void ChkFilterBoth_CheckedChanged(object sender, EventArgs e)     { /* legacy, hidden */ }

        // ---------- Grid painting ----------

        private static void HideColumn(GridView v, string fieldName)
        {
            DevExpress.XtraGrid.Columns.GridColumn col = v.Columns.ColumnByFieldName(fieldName);
            if (col != null) col.Visible = false;
        }

        private static void FormatDateTimeColumn(GridView v, string fieldName)
        {
            DevExpress.XtraGrid.Columns.GridColumn col = v.Columns.ColumnByFieldName(fieldName);
            if (col == null) return;
            col.DisplayFormat.FormatType   = DevExpress.Utils.FormatType.DateTime;
            col.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss";
        }

        /// <summary>
        /// Re-orders a visible column so that it sits immediately after another visible column.
        /// </summary>
        private static void MoveColumnAfter(GridView v, string fieldToMove, string anchorField)
        {
            DevExpress.XtraGrid.Columns.GridColumn toMove = v.Columns.ColumnByFieldName(fieldToMove);
            DevExpress.XtraGrid.Columns.GridColumn anchor = v.Columns.ColumnByFieldName(anchorField);
            if (toMove == null || anchor == null) return;
            toMove.VisibleIndex = anchor.VisibleIndex + 1;
        }

        /// <summary>
        /// Rebinds the grid's data source without trashing user column customisations.
        /// On the first call (columnsReady == false) the configure action runs to set
        /// up hidden columns / formats; on subsequent calls the current layout is saved,
        /// the data swapped, and the layout restored — so order/width/hide survive.
        /// </summary>
        private static void RebindPreservingLayout(DevExpress.XtraGrid.GridControl grid, GridView view,
            DataTable dt, bool columnsReady, Action configureFirstTime)
        {
            if (!columnsReady)
            {
                grid.DataSource = dt;
                configureFirstTime?.Invoke();
                return;
            }
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                view.SaveLayoutToStream(ms);
                grid.DataSource = dt;
                ms.Position = 0;
                view.RestoreLayoutFromStream(ms);
            }
            // Restoring a layout can flip preview/auto-filter back on; lock them off again.
            view.OptionsView.ShowPreview       = false;
            view.OptionsView.RowAutoHeight     = false;
            view.OptionsView.ShowAutoFilterRow = false;
        }

        private static void LockColumnsExceptSelected(GridView v)
        {
            // Defensive: turn off any GridView feature that could create an unexpected
            // panel (preview, auto-filter row, group panel, master-detail expansion).
            // A previously-saved layout could re-enable them otherwise.
            v.OptionsView.ShowGroupPanel    = false;
            v.OptionsView.ShowPreview       = false;
            v.OptionsView.RowAutoHeight     = false;
            v.OptionsView.ShowAutoFilterRow = false;
            v.OptionsView.ShowFooter        = false;
            v.OptionsDetail.EnableMasterViewMode = false;
            v.OptionsFind.AlwaysVisible     = false;

            for (int i = 0; i < v.Columns.Count; i++)
            {
                DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[i];
                bool isSelectCol = string.Equals(c.FieldName, "Selected", StringComparison.OrdinalIgnoreCase);
                c.OptionsColumn.AllowEdit  = isSelectCol;
                c.OptionsColumn.ReadOnly   = !isSelectCol;
                c.OptionsColumn.AllowFocus = isSelectCol;
                if (isSelectCol)
                {
                    c.Caption = "✓";
                    c.Width   = 36;
                    c.MinWidth = 28;
                    c.VisibleIndex = 0;
                    c.OptionsColumn.FixedWidth = true;
                    c.OptionsFilter.AllowFilter = false;
                    DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit rep =
                        new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
                    rep.AutoHeight = false;
                    v.GridControl.RepositoryItems.Add(rep);
                    c.ColumnEdit = rep;
                }
            }
            // #29 — add an unbound status-colour swatch column at position 1 (after Selected)
            if (v.Columns.ColumnByName("StatusSwatch") == null)
            {
                DevExpress.XtraGrid.Columns.GridColumn swatch = v.Columns.AddVisible("StatusSwatch");
                swatch.Caption = " ";
                swatch.Width = 18;
                swatch.MinWidth = 14;
                swatch.OptionsColumn.AllowEdit   = false;
                swatch.OptionsColumn.ReadOnly    = true;
                swatch.OptionsColumn.AllowFocus  = false;
                swatch.OptionsColumn.AllowSort   = DevExpress.Utils.DefaultBoolean.False;
                swatch.OptionsColumn.FixedWidth  = true;
                swatch.OptionsFilter.AllowFilter = false;
                swatch.UnboundType = DevExpress.Data.UnboundColumnType.String;
                swatch.VisibleIndex = 1;
            }
        }

        private void GridView_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "StatusSwatch" || e.RowHandle < 0) return;
            GridView v = sender as GridView;
            if (v == null) return;
            string status = Convert.ToString(v.GetRowCellValue(e.RowHandle, "Status"));
            Color? bg = null;
            if      (status == "New")      bg = ColorNew;
            else if (status == "Update")   bg = ColorUpdate;
            else if (status == "Complete") bg = ColorComplete;
            else if (status == "Ignore")   bg = ColorIgnore;
            if (bg == null) return;
            using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(bg.Value))
                e.Graphics.FillRectangle(brush, e.Bounds);
            e.Handled = true;
        }

        private void GridView_RowStyle(object sender, RowStyleEventArgs e)
        {
            GridView v = sender as GridView;
            if (v == null || e.RowHandle < 0) return;
            object statusObj = v.GetRowCellValue(e.RowHandle, "Status");
            if (statusObj == null) return;
            string status = statusObj.ToString();
            Color? bg = null;
            if      (status == "New")      bg = ColorNew;
            else if (status == "Update")   bg = ColorUpdate;
            else if (status == "Complete") bg = ColorComplete;
            else if (status == "Ignore")   bg = ColorIgnore;
            if (bg == null) return;
            e.Appearance.BackColor  = bg.Value;
            e.Appearance.BackColor2 = bg.Value;
            e.Appearance.Options.UseBackColor = true;
            e.HighPriority = true;
        }

        private void GridView_CustomDrawEmptyForeground(object sender, CustomDrawEventArgs e)
        {
            string msg = "No Stock Request Task Found";
            Font font = new Font("Segoe UI", 13F, FontStyle.Regular);
            SizeF sz = e.Graphics.MeasureString(msg, font);
            float x = e.Bounds.Left + (e.Bounds.Width - sz.Width) / 2f;
            float y = e.Bounds.Top + (e.Bounds.Height - sz.Height) / 2f - 20f;
            e.Graphics.DrawString(msg, font, new SolidBrush(Color.FromArgb(180, 180, 180)), x, y);
            font.Dispose();
            e.Handled = true;
        }

        // ---------- Button handlers ----------

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            LoadGrids();
            ResetCountdown();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today;
            this.TxtSearch.EditValue = null;
            this.DtFrom.DateTime = today.AddMonths(-1);
            this.DtTo.DateTime   = today.AddDays(1);
            this.ChkPendingOnly.Checked = true;
            this.ChkFilterBoth.Checked  = true;
            LoadGrids();
            ResetCountdown();
        }

        private void BtnGenerateSIST_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _userSession == null)
            {
                XtraMessageBox.Show(this,
                    "Generate SI/ST is only available when launched from AutoCount (no user session in dev launcher).",
                    "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Only include data from grids that are currently visible.
            DataTable issueDt    = this.ChkShowIssue.Checked    ? (this.GridIssue.DataSource    as DataTable) : null;
            DataTable transferDt = this.ChkShowTransfer.Checked ? (this.GridTransfer.DataSource as DataTable) : null;
            System.Collections.Generic.List<SiStGenerator.Job> jobs =
                SiStGenerator.BuildJobsFromGrids(issueDt, transferDt);
            if (jobs.Count == 0)
            {
                XtraMessageBox.Show(this, "Tick at least one row first.",
                    "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Pre-flight validation: any ticked Stock Transfer row whose From == To
            // is rejected before we touch AutoCount, with a clear message box.
            string sameLocError = ValidateTransferLocations(transferDt);
            if (sameLocError != null)
            {
                XtraMessageBox.Show(this, sameLocError,
                    "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Distributed lock — only one network user may run this at a time.
            string user = _userSession.LoginUserID ?? "?";
            string machine = Environment.MachineName;
            string holder = PumsTaskLock.TryAcquire(_dbSetting, PumsTaskLock.LOCK_KEY_GENERATE, user, machine);
            if (holder != null)
            {
                XtraMessageBox.Show(this,
                    "Generate SI/ST is currently running on this account book by:\r\n\r\n   " + holder +
                    "\r\n\r\nWait for it to finish, then try again.",
                    "Stock Request Task", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fromLoc = PumsConfig.Get(_dbSetting,
                PumsConfig.KEY_DEFAULT_FROM_LOCATION,
                PumsConfig.DEFAULT_FROM_LOCATION_VALUE);

            try
            {
                using (StockRequestGenerateProgress_Form dlg =
                    new StockRequestGenerateProgress_Form(_dbSetting, _userSession, jobs, fromLoc))
                {
                    dlg.ShowDialog(this);
                }
            }
            finally
            {
                PumsTaskLock.Release(_dbSetting, PumsTaskLock.LOCK_KEY_GENERATE, user);
            }

            LoadGrids();
            ResetCountdown();
        }

        private void BtnGenerateSISTAll_Click(object sender, EventArgs e)
        {
            // Only act on grids the user can see — hidden grids stay untouched.
            DataTable issueDt    = this.ChkShowIssue.Checked    ? (this.GridIssue.DataSource    as DataTable) : null;
            DataTable transferDt = this.ChkShowTransfer.Checked ? (this.GridTransfer.DataSource as DataTable) : null;

            // Toggle logic considers only the visible grids.
            bool anyTicked = AnyNewRowTicked(issueDt) || AnyNewRowTicked(transferDt);
            if (anyTicked)
            {
                UntickAllSelected(issueDt);
                UntickAllSelected(transferDt);
                this.BtnGenerateSISTAll.Text = "Select All New...";
            }
            else
            {
                SelectAllByStatus(issueDt,    "New");
                SelectAllByStatus(transferDt, "New");
                this.BtnGenerateSISTAll.Text = "Unselect All New";
            }
            this.GridViewIssue.RefreshData();
            this.GridViewTransfer.RefreshData();
        }

        /// <summary>
        /// Returns an error message if any ticked Stock Transfer row has
        /// From Location == To Location; otherwise returns null.
        /// </summary>
        private static string ValidateTransferLocations(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Selected")
                || !dt.Columns.Contains("FromLocation") || !dt.Columns.Contains("ToLocation"))
                return null;

            System.Collections.Generic.List<string> bad = new System.Collections.Generic.List<string>();
            foreach (DataRow r in dt.Rows)
            {
                if (!(r["Selected"] is bool b && b)) continue;
                string from = Convert.ToString(r["FromLocation"]) ?? string.Empty;
                string to   = Convert.ToString(r["ToLocation"])   ?? string.Empty;
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) continue;
                if (string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    string id = dt.Columns.Contains("RequestId") ? Convert.ToString(r["RequestId"]) : "?";
                    bad.Add("  • " + id + " (both '" + from.Trim() + "')");
                }
            }
            if (bad.Count == 0) return null;
            return "Stock Transfer From Location and To Location cannot be the same.\r\n\r\n"
                 + "Affected row(s):\r\n" + string.Join("\r\n", bad) + "\r\n\r\n"
                 + "Change the Location on those rows before generating.";
        }

        private static bool AnyNewRowTicked(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Selected") || !dt.Columns.Contains("Status")) return false;
            foreach (DataRow r in dt.Rows)
            {
                if (r["Selected"] is bool b && b
                    && string.Equals(Convert.ToString(r["Status"]), "New", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void UntickAllSelected(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Selected")) return;
            foreach (DataRow r in dt.Rows) r["Selected"] = false;
        }

        private static void SelectAllByStatus(DataTable dt, string status)
        {
            if (dt == null || !dt.Columns.Contains("Selected")) return;
            foreach (DataRow r in dt.Rows)
            {
                string s = r.Table.Columns.Contains("Status") ? Convert.ToString(r["Status"]) : "";
                r["Selected"] = string.Equals(s, status, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void BtnMarkIgnore_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            int n = MarkSelectedAsIgnore(_dbSetting,
                this.GridIssue.DataSource as DataTable, "Z_PumsStockIssue");
            n += MarkSelectedAsIgnore(_dbSetting,
                this.GridTransfer.DataSource as DataTable, "Z_PumsStockTransfer");
            if (n == 0)
            {
                XtraMessageBox.Show(this, "Tick at least one row first.",
                    "Mark as Ignore", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadGrids();
            ResetCountdown();
        }

        private static int MarkSelectedAsIgnore(DBSetting db, DataTable dt, string table)
        {
            if (dt == null || !dt.Columns.Contains("Selected")) return 0;
            System.Collections.Generic.List<long> keys = new System.Collections.Generic.List<long>();
            foreach (DataRow r in dt.Rows)
            {
                if (r["Selected"] is bool b && b) keys.Add(Convert.ToInt64(r["AutoKey"]));
            }
            if (keys.Count == 0) return 0;
            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(db.ConnectionString))
            {
                conn.Open();
                // Only flip rows whose current Status is New or Update — never trample Complete.
                string sql = "UPDATE " + table +
                             " SET Status = 'Ignore' WHERE Status IN ('New','Update') AND AutoKey IN (" +
                             string.Join(",", keys) + ")";
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
            return keys.Count;
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (StockRequestSettings_Form dlg = new StockRequestSettings_Form(_dbSetting))
                dlg.ShowDialog(this);
        }

        private void BtnViewLog_Click(object sender, EventArgs e)
        {
            using (StockRequestLog_Form dlg = new StockRequestLog_Form(_dbSetting))
                dlg.ShowDialog(this);
        }

        // ---------- Toolbar icons (controls + layout now live in the designer) ----------

        // Colourful DevExpress XAF SVG icons on the toolbar buttons (so the UI isn't plain text).
        private void ApplyButtonIcons()
        {
            SetBtnIcon(this.BtnRefresh,          "svgimages/xaf/action_refresh.svg");
            SetBtnIcon(this.BtnFilter,           "svgimages/xaf/action_filter.svg");
            SetBtnIcon(this.BtnReset,            "svgimages/xaf/action_reload.svg");
            SetBtnIcon(this.BtnGenerateSIST,     "svgimages/xaf/action_new.svg");
            SetBtnIcon(this.BtnGenerateSISTAll,  "svgimages/xaf/action_validation_validate.svg");
            SetBtnIcon(this.BtnMarkIgnore,       "svgimages/xaf/state_validation_warning.svg");
            SetBtnIcon(this.BtnSettings,         "svgimages/xaf/action_edit.svg");
            SetBtnIcon(this.BtnViewLog,          "svgimages/xaf/action_aboutinfo.svg");
            SetBtnIcon(_btnApproveChange,        "svgimages/xaf/action_validation_validate.svg");
            SetBtnIcon(_btnSelUpdate,            "svgimages/xaf/action_validation_validate.svg");
            SetBtnIcon(_btnSelCancel,            "svgimages/xaf/action_cancel.svg");
            SetBtnIcon(_btnSelRequest,           "svgimages/xaf/action_new.svg");
        }

        private static void SetBtnIcon(DevExpress.XtraEditors.SimpleButton btn, string svgName)
        {
            if (btn == null) return;
            DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(svgName);
            if (img == null) return;
            btn.ImageOptions.SvgImage = img;
            btn.ImageOptions.SvgImageSize = new Size(20, 20);
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            // Keep the icons' native colours (don't recolor to the skin's monochrome).
            btn.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
        }

        // "Hide Ignore" filter checkbox handler (the control itself lives in the designer).
        private void ChkHideIgnore_CheckedChanged(object sender, EventArgs e)
        {
            LoadGrids();
            ResetCountdown();
        }

        // Stock Issue change/cancel-request rows: Update = light yellow, Cancel (qty 0) = light red.
        private void GridViewIssue_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            string ch = Convert.ToString(this.GridViewIssue.GetRowCellValue(e.RowHandle, "IssueChange"));
            if (string.Equals(ch, "Cancel", StringComparison.OrdinalIgnoreCase)) PaintRow(e, CancelColor);
            else if (string.Equals(ch, "Update", StringComparison.OrdinalIgnoreCase)) PaintRow(e, UpdateColor);
        }

        private static readonly Color UpdateColor = Color.FromArgb(255, 245, 157); // flat yellow
        private static readonly Color CancelColor = Color.FromArgb(255, 205, 210); // flat red

        // Solid (non-gradient) row fill: BackColor2 == BackColor so the skin can't gradient it.
        private static void PaintRow(DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e, Color c)
        {
            e.Appearance.BackColor = c;
            e.Appearance.BackColor2 = c;
            e.Appearance.Options.UseBackColor = true;
        }

        // Approve re-sent change requests:
        //   Stock Issue    : different qty → update existing doc; qty 0 → cancel it.
        //   Stock Transfer : approval=Yes + different qty → update existing doc.
        //   (Stock Transfer cancel, approval=No, is the separate "Cancel Transfer" button.)
        private void BtnApproveChange_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            UserSession session = _userSession ?? UserSession.CurrentUserSession;
            if (session == null)
            {
                XtraMessageBox.Show(this, "No active AutoCount session — cannot apply changes.",
                    "Approve Change", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DataTable dti = this.GridIssue.DataSource as DataTable;
            DataTable dtt = this.GridTransfer.DataSource as DataTable;

            List<DataRow> issueTargets = new List<DataRow>();
            if (dti != null && dti.Columns.Contains("IssueChange"))
                foreach (DataRow r in dti.Rows)
                {
                    if (!(r["Selected"] is bool b && b)) continue;
                    string ch = Convert.ToString(r["IssueChange"]);
                    if ((ch == "Update" || ch == "Cancel") && !string.IsNullOrWhiteSpace(Convert.ToString(r["OriginalDocNo"])))
                        issueTargets.Add(r);
                }

            List<DataRow> xferTargets = new List<DataRow>();
            if (dtt != null && dtt.Columns.Contains("TransferChange"))
                foreach (DataRow r in dtt.Rows)
                {
                    if (!(r["Selected"] is bool b && b)) continue;
                    string tc = Convert.ToString(r["TransferChange"]);
                    if ((tc == "Update" || tc == "Cancel") && !string.IsNullOrWhiteSpace(Convert.ToString(r["OriginalDocNo"])))
                        xferTargets.Add(r);
                }

            if (issueTargets.Count == 0 && xferTargets.Count == 0)
            {
                XtraMessageBox.Show(this,
                    "Tick at least one 'Update' or 'Cancel' row (use 'Select All Update' / 'Select All Cancel' for bulk).",
                    "Approve Change", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.Text.StringBuilder list = new System.Text.StringBuilder();
            foreach (DataRow r in issueTargets)
                list.AppendLine("  • Issue " + Convert.ToString(r["StockIssueId"]) + " → doc " + Convert.ToString(r["OriginalDocNo"]) +
                                " (" + Convert.ToString(r["IssueChange"]) + " qty " + Convert.ToString(r["Quantity"]) + ")");
            foreach (DataRow r in xferTargets)
                list.AppendLine("  • Transfer " + Convert.ToString(r["RequestId"]) + " → doc " + Convert.ToString(r["OriginalDocNo"]) +
                                " (" + Convert.ToString(r["TransferChange"]) + " qty " + Convert.ToString(r["Qty"]) + ")");
            if (XtraMessageBox.Show(this,
                    "Apply the following change(s) to the existing AutoCount document(s)?\r\n\r\n" + list +
                    "\r\nUpdate = change quantity; Cancel = mark the document Cancelled.",
                    "Approve Change", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SiStGenerator gen = new SiStGenerator(_dbSetting, session, PumsConfig.Get(
                _dbSetting, PumsConfig.KEY_DEFAULT_FROM_LOCATION, PumsConfig.DEFAULT_FROM_LOCATION_VALUE));

            int done = 0;
            List<string> errs = new List<string>();

            foreach (DataRow r in issueTargets)
            {
                string id = Convert.ToString(r["StockIssueId"]);
                string docNo = Convert.ToString(r["OriginalDocNo"]);
                string ch = Convert.ToString(r["IssueChange"]);
                long autoKey = Convert.ToInt64(r["AutoKey"]);
                try
                {
                    if (ch == "Cancel")
                    {
                        AutoCount.Stock.StockIssue.StockIssueCommand cmd =
                            AutoCount.Stock.StockIssue.StockIssueCommand.Create(session, _dbSetting);
                        if (!cmd.CancelDocument(docNo)) { errs.Add("Issue " + id + " (" + docNo + "): not cancelled (locked/already cancelled?)"); continue; }
                        StockRequestRepository.MarkIssueCancelled(_dbSetting, autoKey, id, docNo);
                        PumsLog.Write(_dbSetting, PumsLog.TYPE_INFO, "CancelStockIssue", id,
                            "Cancelled AutoCount Stock Issue " + docNo + " (qty 0 approved).", null, docNo, session.LoginUserID);
                    }
                    else
                    {
                        string nd = gen.ProcessSingleJob(BuildEditJob(r, dti, false, docNo, Convert.ToString(r["StockIssueNo"])));
                        StockRequestRepository.MarkIssueChangeApplied(_dbSetting, autoKey, nd);
                        PumsLog.Write(_dbSetting, PumsLog.TYPE_INFO, "UpdateStockIssue", id,
                            "Updated AutoCount Stock Issue " + nd + " to qty " + Convert.ToString(r["Quantity"]) + ".", null, nd, session.LoginUserID);
                    }
                    done++;
                }
                catch (Exception ex)
                {
                    errs.Add("Issue " + id + " (" + docNo + "): " + ex.Message);
                    PumsLog.Write(_dbSetting, PumsLog.TYPE_ERROR, "ApproveStockIssueChange", id, ex.Message, null, ex.ToString(), session.LoginUserID);
                }
            }

            foreach (DataRow r in xferTargets)
            {
                string id = Convert.ToString(r["RequestId"]);
                string docNo = Convert.ToString(r["OriginalDocNo"]);
                string tc = Convert.ToString(r["TransferChange"]);
                long autoKey = Convert.ToInt64(r["AutoKey"]);
                try
                {
                    if (tc == "Cancel")
                    {
                        AutoCount.Stock.StockTransfer.StockTransferCommand cmd =
                            AutoCount.Stock.StockTransfer.StockTransferCommand.Create(session, _dbSetting);
                        if (!cmd.CancelDocument(docNo)) { errs.Add("Transfer " + id + " (" + docNo + "): not cancelled (locked/already cancelled?)"); continue; }
                        StockRequestRepository.MarkTransferCancelled(_dbSetting, autoKey, id, docNo);
                        PumsLog.Write(_dbSetting, PumsLog.TYPE_INFO, "CancelStockTransfer", id,
                            "Cancelled AutoCount Stock Transfer " + docNo + " (approval=No).", null, docNo, session.LoginUserID);
                    }
                    else
                    {
                        string nd = gen.ProcessSingleJob(BuildEditJob(r, dtt, true, docNo, id));
                        StockRequestRepository.MarkTransferChangeApplied(_dbSetting, autoKey, nd);
                        PumsLog.Write(_dbSetting, PumsLog.TYPE_INFO, "UpdateStockTransfer", id,
                            "Updated AutoCount Stock Transfer " + nd + " to qty " + Convert.ToString(r["Qty"]) + ".", null, nd, session.LoginUserID);
                    }
                    done++;
                }
                catch (Exception ex)
                {
                    errs.Add("Transfer " + id + " (" + docNo + "): " + ex.Message);
                    PumsLog.Write(_dbSetting, PumsLog.TYPE_ERROR, "ApproveStockTransferChange", id, ex.Message, null, ex.ToString(), session.LoginUserID);
                }
            }

            LoadGrids();
            ResetCountdown();
            string msg = done + " change(s) applied.";
            if (errs.Count > 0) msg += "\r\n\r\nFailed:\r\n" + string.Join("\r\n", errs.ToArray());
            XtraMessageBox.Show(this, msg, "Approve Change", MessageBoxButtons.OK,
                errs.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // Tick every Update row (both grids) so they can be bulk-applied via Approve Change.
        private void BtnSelectAllUpdate_Click(object sender, EventArgs e)
        {
            int n = TickByChange("Update");
            XtraMessageBox.Show(this, n + " 'Update' row(s) selected. Click 'Approve Change' to apply.",
                "Select All Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Tick every Cancel row (both grids) so they can be bulk-applied via Approve Change.
        private void BtnSelectAllCancel_Click(object sender, EventArgs e)
        {
            int n = TickByChange("Cancel");
            XtraMessageBox.Show(this, n + " 'Cancel' row(s) selected. Click 'Approve Change' to apply.",
                "Select All Cancel", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private int TickByChange(string change)
        {
            int n = TickGrid(this.GridIssue.DataSource as DataTable, "IssueChange", change)
                  + TickGrid(this.GridTransfer.DataSource as DataTable, "TransferChange", change);
            this.GridViewIssue.RefreshData();
            this.GridViewTransfer.RefreshData();
            return n;
        }

        private static int TickGrid(DataTable dt, string changeCol, string change)
        {
            if (dt == null || !dt.Columns.Contains(changeCol) || !dt.Columns.Contains("Selected")) return 0;
            int n = 0;
            foreach (DataRow r in dt.Rows)
                if (string.Equals(Convert.ToString(r[changeCol]), change, StringComparison.OrdinalIgnoreCase))
                { r["Selected"] = true; n++; }
            return n;
        }

        // Toggle: select (or unselect) every request that has NO generated document yet
        // (i.e. not yet turned into a Stock Issue / Transfer), across both grids.
        private void BtnSelectAllRequest_Click(object sender, EventArgs e)
        {
            DataTable di = this.GridIssue.DataSource as DataTable;
            DataTable dt = this.GridTransfer.DataSource as DataTable;
            bool anyTicked = AnyNoDocTicked(di) || AnyNoDocTicked(dt);
            bool select = !anyTicked;   // none ticked → select all; otherwise unselect

            int n = SetNoDocSelected(di, select) + SetNoDocSelected(dt, select);
            this.GridViewIssue.RefreshData();
            this.GridViewTransfer.RefreshData();
            if (_btnSelRequest != null)
                _btnSelRequest.Text = select ? "Unselect All Request" : "Select All Request";

            XtraMessageBox.Show(this,
                (select ? n + " request(s) with no document selected." : "Selection cleared."),
                "Select All Request", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // A row that still needs a document: no GeneratedDocNo and not Ignored/Cancelled/Complete.
        private static bool IsNoDocRow(DataRow r)
        {
            string doc = r.Table.Columns.Contains("GeneratedDocNo") ? Convert.ToString(r["GeneratedDocNo"]) : "";
            string st = r.Table.Columns.Contains("Status") ? Convert.ToString(r["Status"]) : "";
            return string.IsNullOrWhiteSpace(doc)
                   && st != "Ignore" && st != "Cancelled" && st != "Complete";
        }

        private static int SetNoDocSelected(DataTable dt, bool sel)
        {
            if (dt == null || !dt.Columns.Contains("Selected")) return 0;
            int n = 0;
            foreach (DataRow r in dt.Rows)
                if (IsNoDocRow(r)) { r["Selected"] = sel; if (sel) n++; }
            return n;
        }

        private static bool AnyNoDocTicked(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("Selected")) return false;
            foreach (DataRow r in dt.Rows)
                if (IsNoDocRow(r) && r["Selected"] is bool b && b) return true;
            return false;
        }

        // Build an "edit the existing document" job from a grid row (Status=Update + ExistingDocNo).
        private static SiStGenerator.Job BuildEditJob(DataRow r, DataTable dt, bool isTransfer, string existingDocNo, string label)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (DataColumn c in dt.Columns) dict[c.ColumnName] = r[c];
            return new SiStGenerator.Job
            {
                IsTransfer = isTransfer,
                AutoKey = Convert.ToInt64(r["AutoKey"]),
                Label = label,
                Status = "Update",
                ExistingDocNo = existingDocNo,
                Row = dict
            };
        }

        // Transfer change-request rows: Update = light yellow, Cancel = light red.
        private void GridViewTransfer_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            string ch = Convert.ToString(this.GridViewTransfer.GetRowCellValue(e.RowHandle, "TransferChange"));
            if (string.Equals(ch, "Cancel", StringComparison.OrdinalIgnoreCase)) PaintRow(e, CancelColor);
            else if (string.Equals(ch, "Update", StringComparison.OrdinalIgnoreCase)) PaintRow(e, UpdateColor);
        }

    }
}
