using System;
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

        public StockRequestTask_Form()
        {
            InitializeComponent();
            InitDefaults();
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
            view.OptionsMenu.EnableColumnMenu = true;     // header right-click → DevExpress built-in menu
            view.OptionsMenu.EnableFooterMenu = true;
            // Custom Row-area popup menu with Export + Layout actions
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Export to Excel...", null, (s, e) => ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", "xlsx"));
            menu.Items.Add("Export to Text...",  null, (s, e) => ExportGrid(grid, "Text files (*.txt)|*.txt",   "txt"));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Save Layout",  null, (s, e) => SaveGridLayout(view, layoutKey));
            menu.Items.Add("Load Layout",  null, (s, e) => LoadGridLayout(view, layoutKey));
            menu.Items.Add("Reset Layout", null, (s, e) => ResetGridLayout(view, layoutKey));
            grid.ContextMenuStrip = menu;
            // Try to load any saved layout on first attach
            TryLoadSavedLayout(view, layoutKey);
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
                    if (ext == "xlsx") grid.ExportToXlsx(dlg.FileName);
                    else                grid.ExportToText(dlg.FileName);
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
                    DataTable dt = StockRequestRepository.LoadStockIssue(_dbSetting, fromDate, toDate, searchForIssue, pendingOnly);
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
                        _dbSetting, fromDate, toDate, searchForTransfer, pendingOnly, defaultLoc);
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
    }
}
