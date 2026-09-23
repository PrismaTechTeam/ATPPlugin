using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.MeterReading.Services;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    // Hidden from the menu (2026-09-14): this screen is the "Meters - fetch & key in" view inside
    // Meter Invoice Run now. Put the attribute back to list it on its own again.
    // [AutoCount.PlugIn.MenuItem("Meter Reading Integration", MenuOrder = 450, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MeterReadingIntegration_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;
        private DataTable _dtGrid;   // one row PER METER; item columns repeat (merged via AllowCellMerge)
        private XtraTabControl _tabView;
        private XtraTabPage _pageWith;
        private XtraTabPage _pageNo;
        private XtraTabPage _pageDone;   // this month's INVOICED machines — invoice-info column layout
        private XtraTabPage _pageConflict;
        private XtraTabPage _pageAll;   // kept but HIDDEN (PageVisible=false) — flip to true to bring it back
        private LabelControl _lblApiStatus;
        private DevExpress.XtraEditors.PanelControl _pnlFooter;
        private string _apiBaseUrl;
        private DevExpress.XtraEditors.PanelControl _pnlFetching;
        private DevExpress.XtraEditors.MarqueeProgressBarControl _marquee;
        private LabelControl _lblFetchMsg;
        private System.Windows.Forms.Timer _fetchTimer;
        private int _fetchElapsed;
        private int _fetchMsgIdx;
        private bool _suppressFilterEvent;   // guards programmatic Day/ShowAll changes from auto-reloading
        private DevExpress.XtraEditors.CheckEdit _chkInclude0Usage;
        private LabelControl _lblGroupingInfo;   // shows the Invoice grouping SETTING (no run-level toggle)
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _selCssiEditor;
        private static readonly string[] FETCH_MSGS = new string[] {
            "Contacting the meter API...",
            "Fetching online + offline readings...",
            "The server can be slow (~15s) - please hold on...",
            "Still working - matching machines to readings...",
            "Almost there - finishing up..."
        };
        private SimpleButton _btnSetting;
        private SimpleButton _btnViewSetting;    // #2(b): pick which columns the grid shows
        /// <summary>False when this screen is hosted as the Meters view of the Meter Invoice Run:
        /// generating happens over there, so the tick column is out of sight here. Everything the
        /// ticks drive (Select All, Generate) is untouched -- it is only not shown.</summary>
        /// <summary>False when hosted in the Meter Invoice Run: a double-click on a row no longer
        /// opens the machine's key-in detail dialog there (readings are keyed in the grid, or on
        /// the Invoices view). The contract, invoice and history double-clicks still work.</summary>
        public bool AllowRowDoubleClickDetail = true;
        private bool _showSelectColumn = true;
        public bool ShowSelectColumn
        {
            get { return _showSelectColumn; }
            set { _showSelectColumn = value; ApplySelectColumnVisibility(); }
        }

        /// <summary>The grids are shaped once and the user's saved layout is restored after every
        /// load, so a hidden tick column has to be hidden again at the end of each load -- and the
        /// moment the host turns it off.</summary>
        private void ApplySelectColumnVisibility()
        {
            if (_showSelectColumn) return;
            foreach (GridView v in _viewByPage.Values)
            {
                GridColumn c = v.Columns["SelCssi"];
                if (c != null && c.Visible) c.Visible = false;
            }
        }
        private ComboBoxEdit _cmbMeterFilter;    // #2(d): All / BK only / CL only / BK + CL -- the STATE (hidden)
        private CheckButton[] _meterFilterButtons;   // the same five choices as buttons, like the Invoice Run
        private LabelControl _lblLegendMust;     // #2(c): colour legend
        private LabelControl _lblLegendNo;
        private SimpleButton _btnMonthOverview;
        private ComboBoxEdit _cmbYear;   // explicit billing YEAR (defaults to the auto-resolved one)
        private LabelControl _lblOverdue;   // "July still has 12 rows unbilled" — click to go there
        private int _overdueYear, _overdueMonth;
        private Dictionary<int, int> _dayCounts = new Dictionary<int, int>();
        private LabelControl _lblDaySummary;   // "7th: 1,411 rows due - 467 still open"
        private bool _scopedGenerate;   // detail-dialog "Save & Generate": skip the #3 group guard
        // TEST-ONLY mock fetch (Ctrl+Shift+T reveals the button): a pasted JSON impersonates the
        // meter API so the REAL fetch pipeline can be exercised without the live endpoints.
        private SimpleButton _btnMockFetch;
        private ServiceContractPhotocopier.MeterReading.Services.IMeterReadingApiClient _testJsonClient;
        private static string _lastTestJson;   // remembered across form opens (session-wide)

        public MeterReadingIntegration_Form()
        {
            InitializeComponent();
            // Native AutoCount header (same as Maintain Service Contract) with the hint line hidden.
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            InitDefaults();
            // DEV/TEST hidden shortcut: Ctrl+Shift+3 wipes every meter-generated invoice (confirmed)
            // so billing runs can be repeated. No button on purpose — key combo only.
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(DevShortcut_KeyDown);
            // #2(a): a reading typed and left un-posted must save before the form dies (FormClosing
            // runs BEFORE the FormClosed layout auto-save below).
            this.FormClosing += delegate { try { ActiveView.CloseEditor(); ActiveView.UpdateCurrentRow(); } catch { } };
            // #1a: the user's grid layout (sorting/filter/columns) survives closing the module —
            // silently saved into AutoCount's native dbo.Layout as this user's assigned layout.
            this.FormClosed += delegate { SaveGridLayoutForCurrentUser(); };
        }

        private void DevShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.D3)
            {
                e.Handled = true;
                DevWipeGeneratedInvoices();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                e.Handled = true;
                ToggleMockFetchButton();
            }
        }

        // ── TEST Fetch (JSON): hidden button + paste dialog ──────────────────────────────────────

        /// <summary>Ctrl+Shift+T from a screen that holds this one inside it (Meter Invoice Run). A
        /// hosted form only hears keys while focus is in it, and after "Meters - fetch &amp; key in"
        /// is clicked the focus is on the host's button.</summary>
        internal void ToggleTestFetch()
        {
            ToggleMockFetchButton();
        }

        private void ToggleMockFetchButton()
        {
            if (_btnMockFetch == null)
            {
                _btnMockFetch = new SimpleButton();
                _btnMockFetch.Size = new Size(150, this.BtnFetch.Height);
                _btnMockFetch.Appearance.BackColor = Color.FromArgb(255, 236, 179);   // amber = TEST
                _btnMockFetch.Appearance.Options.UseBackColor = true;
                _btnMockFetch.Parent = this.BtnFetch.Parent;
                // After the last button on Fetch's row -- right beside Fetch it landed on Setting.
                int x = this.BtnFetch.Right + 8;
                foreach (Control c in this.BtnFetch.Parent.Controls)
                    if (c != _btnMockFetch && c.Visible && Math.Abs(c.Top - this.BtnFetch.Top) < 10 && c.Right + 8 > x)
                        x = c.Right + 8;
                _btnMockFetch.Location = new Point(x, this.BtnFetch.Top);
                _btnMockFetch.Click += new EventHandler(BtnMockFetch_Click);
                UpdateMockFetchButtonText();
            }
            _btnMockFetch.Visible = !_btnMockFetch.Visible;
            if (_btnMockFetch.Visible) _btnMockFetch.BringToFront();
        }

        private void UpdateMockFetchButtonText()
        {
            if (_btnMockFetch == null) return;
            _btnMockFetch.Text = _testJsonClient != null ? "TEST JSON ACTIVE ✓" : "TEST Fetch (JSON)";
        }

        private void BtnMockFetch_Click(object sender, EventArgs e)
        {
            using (DevExpress.XtraEditors.XtraForm dlg = new DevExpress.XtraEditors.XtraForm())
            {
                dlg.Text = "TEST Fetch — paste the fake API JSON";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(760, 520);
                dlg.MinimizeBox = false;

                LabelControl hint = new LabelControl();
                hint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                hint.Appearance.Options.UseTextOptions = true;
                hint.AutoSizeMode = LabelAutoSizeMode.None;
                hint.Location = new Point(10, 8);
                hint.Size = new Size(740, 44);
                hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                hint.Text = "Machines are matched by SerialNumber (fallback: Code = Service Item No). TotalBK / TotalCL " +
                    "are CUMULATIVE lifetime counters. Flat array = all ONLINE; use {\"Online\":[...],\"Offline\":[...]} " +
                    "to test offline + TrackingId. Missing LastAuditDate auto-fills day 1 of the selected period. " +
                    "The override stays active for every Fetch until you clear it.";
                dlg.Controls.Add(hint);

                MemoEdit memo = new MemoEdit();
                memo.Location = new Point(10, 56);
                memo.Size = new Size(740, 410);
                memo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                memo.Properties.Appearance.Font = new Font("Consolas", 9F);
                memo.Properties.WordWrap = false;
                memo.Properties.ScrollBars = ScrollBars.Both;
                memo.Text = string.IsNullOrEmpty(_lastTestJson) ? BuildSampleTestJson() : _lastTestJson;
                dlg.Controls.Add(memo);

                SimpleButton bUse = new SimpleButton();
                bUse.Text = "Use JSON + Fetch";
                bUse.Size = new Size(130, 26);
                bUse.Location = new Point(10, 478);
                bUse.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                dlg.Controls.Add(bUse);

                SimpleButton bClear = new SimpleButton();
                bClear.Text = "Clear override";
                bClear.Size = new Size(110, 26);
                bClear.Location = new Point(148, 478);
                bClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                bClear.Enabled = _testJsonClient != null;
                dlg.Controls.Add(bClear);

                SimpleButton bCancel = new SimpleButton();
                bCancel.Text = "Cancel";
                bCancel.Size = new Size(90, 26);
                bCancel.Location = new Point(660, 478);
                bCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                bCancel.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(bCancel);
                dlg.CancelButton = bCancel;

                bool doFetch = false;
                bUse.Click += new EventHandler(delegate
                {
                    string json = memo.Text.Trim();
                    if (json.Length == 0)
                    {
                        XtraMessageBox.Show("Paste a JSON first.", "TEST Fetch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    // Validate NOW so a typo fails here, not mid-fetch.
                    try
                    {
                        ServiceContractPhotocopier.MeterReading.Services.JsonPasteMeterReadingApiClient probe =
                            new ServiceContractPhotocopier.MeterReading.Services.JsonPasteMeterReadingApiClient(json);
                        int n = probe.GetReadings(ServiceContractPhotocopier.MeterReading.Services.MachineStatus.Online, SelectedYear(), SelectedMonth()).Count
                              + probe.GetReadings(ServiceContractPhotocopier.MeterReading.Services.MachineStatus.Offline, SelectedYear(), SelectedMonth()).Count;
                        if (n == 0)
                        {
                            XtraMessageBox.Show("The JSON parsed but contains 0 readings.", "TEST Fetch",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        _testJsonClient = probe;
                        _lastTestJson = json;
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show("JSON not valid:\r\n" + ex.Message, "TEST Fetch",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    doFetch = true;
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                });
                bClear.Click += new EventHandler(delegate
                {
                    _testJsonClient = null;
                    UpdateMockFetchButtonText();
                    dlg.DialogResult = DialogResult.Cancel;
                    dlg.Close();
                });

                dlg.ShowDialog(this);
                UpdateMockFetchButtonText();
                if (doFetch) BtnFetch_Click(this, EventArgs.Empty);
            }
        }

        /// <summary>Template JSON generated from the CURRENT grid: one entry per machine serial,
        /// TotalBK/TotalCL prefilled with last reading + 100 so charges appear immediately.</summary>
        private string BuildSampleTestJson()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("[\r\n");
            bool first = true;
            System.Collections.Generic.Dictionary<string, decimal[]> bySerial =
                new System.Collections.Generic.Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
            System.Collections.Generic.List<string> order = new System.Collections.Generic.List<string>();
            if (_dtGrid != null)
                foreach (DataRow r in _dtGrid.Rows)
                {
                    string sn = S(r["SerialNo"]).Trim();
                    string role = S(r["Role"]).Trim().ToUpperInvariant();
                    if (sn.Length == 0 || (role != "BK" && role != "CL")) continue;
                    decimal[] v;
                    if (!bySerial.TryGetValue(sn, out v)) { v = new decimal[2]; bySerial[sn] = v; order.Add(sn); }
                    v[role == "BK" ? 0 : 1] = Dec(r["LastReading"]) + 100m;
                }
            string audit = new DateTime(SelectedYear(), SelectedMonth(), 1).ToString("yyyy-MM-dd");
            foreach (string sn in order)
            {
                if (!first) sb.Append(",\r\n");
                first = false;
                decimal[] v = bySerial[sn];
                sb.Append("  { \"SerialNumber\": \"").Append(sn.Replace("\"", "\\\""))
                  .Append("\", \"TotalBK\": ").Append(v[0].ToString("0"))
                  .Append(", \"TotalCL\": ").Append(v[1].ToString("0"))
                  .Append(", \"LastAuditDate\": \"").Append(audit).Append("\" }");
            }
            sb.Append("\r\n]");
            return sb.ToString();
        }

        // TESTING ONLY: deletes ALL invoices this module generated (proper AutoCount delete, so GL /
        // stock postings reverse correctly), then reconciles the meter stamps and reloads — the whole
        // book is back to "nothing billed" and a test run can repeat.
        private void DevWipeGeneratedInvoices()
        {
            if (_dbSetting == null) return;
            DataTable keys;
            try
            {
                keys = _dbSetting.GetDataTable(
                    "SELECT DISTINCT iv.DocKey, iv.DocNo FROM dbo.IV iv WHERE iv.DocKey IN (" +
                    "SELECT InvoicedDocKey FROM dbo.zSCP2_MeterEntry WHERE InvoicedDocKey IS NOT NULL " +
                    "UNION SELECT SalesInvoiceDocKey FROM dbo.zSCP_MeterTrans WHERE SalesInvoiceDocKey IS NOT NULL)", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Lookup failed:\r\n" + ex.Message, "Dev Wipe"); return; }

            if (keys.Rows.Count == 0)
            {
                XtraMessageBox.Show("No meter-generated invoices found — nothing to delete.", "Dev Wipe");
                return;
            }
            if (XtraMessageBox.Show(
                    "TESTING SHORTCUT\r\n\r\nDelete ALL " + keys.Rows.Count + " meter-generated invoice(s) from AutoCount?\r\n\r\n" +
                    "FORCE: any credit note, payment or refund knocked off against them is DELETED FIRST " +
                    "(AutoCount refuses to delete an invoice that carries one). A payment that also pays " +
                    "OTHER invoices is deleted whole — this is a test-data reset, not an accounting correction.\r\n\r\n" +
                    "Meter stamps are released so billing can be repeated. This cannot be undone.",
                    "Dev Wipe — Generated Invoices", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            ServiceContractPhotocopier.Classes.ScpDeleteResult res =
                ServiceContractPhotocopier.Classes.ScpInvoiceDelete.Delete(_dbSetting, keys, true);
            LoadData();
            XtraMessageBox.Show(res.Summary, "Dev Wipe", MessageBoxButtons.OK,
                res.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // Native AutoCount toolbar icons — the SAME family and size the "Maintain Service Contract"
        // list and Stock Request Task use (GetLargeImage_*, MiddleLeft).
        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                SetBtnAcIcon(this.BtnRefresh, img.GetLargeImage_Refresh());
                SetBtnAcIcon(this.BtnFetch, img.GetLargeImage_Inquiry());
                SetBtnAcIcon(this.BtnSelfManualKeyIn, img.GetLargeImage_Approve());
                SetBtnAcIcon(this.BtnGenerateInvoice, img.GetLargeImage_New());
            }
            catch { }   // icons are cosmetic — never block the form over an image lookup
            // Filter/Reset are the small 28px buttons inside Filter Options — compact SVGs fit there.
            SetBtnSvgIcon(this.BtnFilter, "svgimages/xaf/action_filter.svg");
            SetBtnSvgIcon(this.BtnReset, "svgimages/xaf/action_reload.svg");
        }

        private static void SetBtnAcIcon(DevExpress.XtraEditors.SimpleButton btn, System.Drawing.Image image)
        {
            if (btn == null || image == null) return;
            btn.ImageOptions.Image = image;
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        private static void SetBtnSvgIcon(DevExpress.XtraEditors.SimpleButton btn, string svgName)
        {
            if (btn == null) return;
            DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(svgName);
            if (img == null) return;
            btn.ImageOptions.SvgImage = img;
            btn.ImageOptions.SvgImageSize = new System.Drawing.Size(20, 20);
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            btn.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
        }

        public MeterReadingIntegration_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            RestoreMeterFilter();   // needs _dbSetting, which only exists from here on
            PopulateDayCombo();
            InitApiStatus();
            LoadData();
        }

        public MeterReadingIntegration_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            RestoreMeterFilter();   // needs _dbSetting, which only exists from here on
            PopulateDayCombo();
            InitApiStatus();
            LoadData();
        }

        private void InitDefaults()
        {
            // "Edit:" caption + big date removed on request — the billing period already shows in the
            // Filter Options title.
            this.LblEditCaption.Visible = false;
            this.LblToday.Visible = false;
            this.ChkShowAll.Checked = false;   // default: filter by the selected billing Day (not show all)

            this.CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                this.CmbMonth.Properties.Items.Add(new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            this.CmbDay.Properties.Items.Clear();
            for (int d = 1; d <= 31; d++) this.CmbDay.Properties.Items.Add(d);
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;

            // Picking a Day filters to that day (auto-uncheck Show All); toggling Show All reloads too.
            this.CmbDay.SelectedIndexChanged += new EventHandler(CmbDay_SelectedIndexChanged);
            this.ChkShowAll.CheckedChanged += new EventHandler(ChkShowAll_CheckedChanged);


            SetupTabs();
        }

        // ───────────────────── one grid per tab ─────────────────────
        //
        // Each tab owns a REAL GridControl + GridView over the same in-memory table, filtered to
        // that tab's rows. It used to be one grid re-parented from tab to tab, which meant one set
        // of columns and one saved layout shared by all of them: arrange the Invoiced tab and the
        // arrangement followed you to Ready to Invoice, then got overwritten on the way back. A
        // grid per tab makes each tab's columns, sort, grouping and saved layouts simply its own.

        private readonly System.Collections.Generic.List<XtraTabPage> _pages =
            new System.Collections.Generic.List<XtraTabPage>();
        private readonly System.Collections.Generic.Dictionary<XtraTabPage, GridControl> _gridByPage =
            new System.Collections.Generic.Dictionary<XtraTabPage, GridControl>();
        private readonly System.Collections.Generic.Dictionary<XtraTabPage, GridView> _viewByPage =
            new System.Collections.Generic.Dictionary<XtraTabPage, GridView>();

        /// <summary>Set while a specific tab's grid is being built or reconfigured, so all the
        /// existing column-shaping code operates on THAT grid without needing a parameter threaded
        /// through every helper.</summary>
        private GridView _targetView;

        /// <summary>The grid the operator is looking at.</summary>
        private GridView ActiveView
        {
            get
            {
                if (_targetView != null) return _targetView;
                GridView v;
                if (_tabView != null && _tabView.SelectedTabPage != null &&
                    _viewByPage.TryGetValue(_tabView.SelectedTabPage, out v)) return v;
                return this.GridViewMeter;   // before the tabs exist (ctor wiring)
            }
        }

        private GridControl ActiveGrid
        {
            get
            {
                if (_targetView != null && _targetView.GridControl != null)
                    return _targetView.GridControl as GridControl;
                GridControl g;
                if (_tabView != null && _tabView.SelectedTabPage != null &&
                    _gridByPage.TryGetValue(_tabView.SelectedTabPage, out g)) return g;
                return this.GridMeter;
            }
        }

        /// <summary>Run an action against one tab's grid as if it were the active one.</summary>
        private void ForView(GridView v, System.Action action)
        {
            GridView prev = _targetView;
            _targetView = v;
            try { action(); }
            finally { _targetView = prev; }
        }

        /// <summary>
        /// Every tab's grid behaves identically — same editing rules, same colours, same merged
        /// cells — so they all get the same handlers. The handlers read the view from `sender`
        /// or from ActiveView, both of which resolve to whichever tab is in front.
        /// </summary>
        /// <summary>
        /// The view an event came FROM. Every tab's grid shares these handlers, and an event can
        /// arrive from a tab that is not in front — reading the active view there would answer with
        /// another grid's rows. `sender` is the truth; ActiveView is only the fallback.
        /// </summary>
        private GridView V(object sender)
        {
            GridView v = sender as GridView;
            return v != null ? v : ActiveView;
        }

        private void WireViewEvents(GridView v)
        {
            if (v == null) return;
            v.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMeter_CellValueChanged);
            // MERGED cells never activate their in-place editor, so the merged Select checkbox is
            // toggled by hand on mouse-down (SetRowCellValue fires CellValueChanged -> the existing
            // whole-CSSI propagation runs exactly as if the editor had been used).
            v.MouseDown += new System.Windows.Forms.MouseEventHandler(GridViewMeter_MouseDown);
            v.CellMerge +=
                new DevExpress.XtraGrid.Views.Grid.CellMergeEventHandler(GridViewMeter_CellMerge);
            // Per-item alternating row colour (super-light-blue / white), so each service item's
            // BK+CL pair is easy to tell apart at a glance. Conflicts override to light red.
            v.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewMeter_RowStyle);
            v.ShowingEditor +=
                new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditor);
            // Double-click a contract → open its detail / override form.
            v.DoubleClick += new EventHandler(GridViewMeter_DoubleClick);
            // FOC Qty of a ladder meter displays the LADDER's free copies (the engine ignores the
            // meter's own FOCQty when a ladder is in effect) — consistent with the contract grid.
            v.CustomColumnDisplayText +=
                new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(GridViewMeter_CustomColumnDisplayText);
            // "What the colours mean" lives in the column-header right-click menu, beside Column
            // Chooser and the layout items. The toolbar is full and is not the place for a
            // reference card — three rounds of shoving buttons into it made that plain.
            v.PopupMenuShowing +=
                new DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventHandler(GridViewMeter_PopupMenuShowing);
        }

        private void GridViewMeter_PopupMenuShowing(object sender,
            DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventArgs e)
        {
            if (e.MenuType != DevExpress.XtraGrid.Views.Grid.GridMenuType.Column || e.Menu == null) return;
            DevExpress.Utils.Menu.DXMenuItem item =
                new DevExpress.Utils.Menu.DXMenuItem("What the colours mean...",
                    new EventHandler(ColourKeyMenu_Click));
            item.BeginGroup = true;
            e.Menu.Items.Add(item);
        }

        private void ColourKeyMenu_Click(object sender, EventArgs e) { ShowColourKey(); }

        private void SetupTabs()
        {
            _tabView = new XtraTabControl();
            _tabView.Dock = DockStyle.Fill;
            // 3-tab layout (UX-panel outcome): Online/Offline were redundant — the machine status is a
            // row attribute, now shown as a coloured Machine Status column instead. Conflicts appears
            // only when there IS a conflict.
            // Colour-coded tab headers: green = data ready, orange = action needed, red = conflicts.
            // "Ready to Invoice" (was "With Reading"): the tab also holds RENTAL-ONLY machines that
            // have nothing to read — the old name looked broken next to their empty Reading cells.
            _pageWith = new XtraTabPage(); _pageWith.Text = "Ready to Invoice";
            _pageWith.Appearance.Header.ForeColor = Color.FromArgb(27, 94, 32);
            _pageWith.Appearance.Header.Options.UseForeColor = true;
            _pageWith.Appearance.Header.FontStyleDelta = FontStyle.Bold;
            _pageWith.Appearance.Header.Options.UseFont = true;
            _pageNo = new XtraTabPage(); _pageNo.Text = "Need Manual Key-In";
            _pageNo.Appearance.Header.ForeColor = Color.FromArgb(191, 87, 0);
            _pageNo.Appearance.Header.Options.UseForeColor = true;
            _pageNo.Appearance.Header.FontStyleDelta = FontStyle.Bold;
            _pageNo.Appearance.Header.Options.UseFont = true;
            _pageDone = new XtraTabPage(); _pageDone.Text = "Invoiced";
            _pageDone.Appearance.Header.ForeColor = Color.FromArgb(46, 125, 50);
            _pageDone.Appearance.Header.Options.UseForeColor = true;
            _pageDone.Appearance.Header.FontStyleDelta = FontStyle.Bold;
            _pageDone.Appearance.Header.Options.UseFont = true;
            _pageConflict = new XtraTabPage(); _pageConflict.Text = "⚠ Conflicts";
            _pageConflict.PageVisible = false;   // shown by UpdateTabCounts only when count > 0
            _pageConflict.Appearance.Header.ForeColor = Color.FromArgb(198, 40, 40);
            _pageConflict.Appearance.Header.Options.UseForeColor = true;
            _pageConflict.Appearance.Header.FontStyleDelta = FontStyle.Bold;
            _pageConflict.Appearance.Header.Options.UseFont = true;
            _pageAll = new XtraTabPage(); _pageAll.Text = "All";
            _pageAll.Appearance.Header.ForeColor = Color.FromArgb(33, 115, 199);
            _pageAll.Appearance.Header.Options.UseForeColor = true;
            _pageAll.Appearance.Header.FontStyleDelta = FontStyle.Bold;
            _pageAll.Appearance.Header.Options.UseFont = true;
            _pageAll.PageVisible = false;   // hidden on request — not deleted
            _tabView.TabPages.AddRange(new XtraTabPage[] { _pageWith, _pageNo, _pageDone, _pageConflict, _pageAll });

            // Tab one reuses the designer's grid; the rest get their own, styled from it so all
            // five look identical and only their CONTENT and arrangement differ.
            this.Controls.Remove(this.GridMeter);
            this.GridMeter.Dock = DockStyle.Fill;
            _pageWith.Controls.Add(this.GridMeter);
            _pages.Add(_pageWith); _pages.Add(_pageNo); _pages.Add(_pageDone);
            _pages.Add(_pageConflict); _pages.Add(_pageAll);
            for (int i = 0; i < _pages.Count; i++)
            {
                XtraTabPage page = _pages[i];
                GridControl g;
                GridView v;
                if (i == 0) { g = this.GridMeter; v = this.GridViewMeter; }
                else
                {
                    v = new GridView();
                    v.Appearance.Assign(this.GridViewMeter.Appearance);
                    v.OptionsView.ShowGroupPanel = this.GridViewMeter.OptionsView.ShowGroupPanel;
                    v.OptionsView.ShowViewCaption = this.GridViewMeter.OptionsView.ShowViewCaption;
                    v.ViewCaption = this.GridViewMeter.ViewCaption;
                    v.OptionsView.EnableAppearanceEvenRow = false;
                    v.OptionsView.EnableAppearanceOddRow = false;
                    v.RowHeight = this.GridViewMeter.RowHeight;
                    v.ColumnPanelRowHeight = this.GridViewMeter.ColumnPanelRowHeight;
                    v.Name = "GridViewMeter_" + i;
                    g = new GridControl();
                    g.Dock = DockStyle.Fill;
                    g.MainView = v;
                    g.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { v });
                    g.Name = "GridMeter_" + i;
                    page.Controls.Add(g);
                }
                v.Tag = page;      // ApplyTabColumnLayout asks the VIEW which tab it belongs to
                _gridByPage[page] = g;
                _viewByPage[page] = v;
                if (i > 0) WireViewEvents(v);   // tab one is wired by the constructor
            }
            _tabView.Dock = DockStyle.Fill;
            this.Controls.Add(_tabView);
            // A Dock=Fill control must sit at child-index 0 (same slot the designer used for
            // ActiveGrid) so it is laid out AFTER the docked-Top panels and only fills the area
            // left below them. Otherwise it covers the whole form and hides its own tab strip +
            // the grid's column headers behind the title/filter panels.
            this.Controls.SetChildIndex(_tabView, 0);
            _tabView.SelectedTabPage = _pageWith;
            _tabView.SelectedPageChanged += new TabPageChangedEventHandler(TabView_SelectedPageChanged);

            WireViewEvents(this.GridViewMeter);

            // Setting button (created in code to avoid touching the strict designer). Same 150x50 /
            // 156px rhythm as the rest of the toolbar row (510/666/822/978/1134).
            _btnSetting = new SimpleButton();
            _btnSetting.Text = "Setting";
            // Last of the five action buttons. At 150 wide on a 156 pitch the run needed 774px but
            // the band from x=510 to the panel's right margin is 762 — so this button hung 4px off
            // the edge and was clipped. 146 wide on a 154 pitch lands exactly on 1272, giving the
            // right an 8px margin to match the 8px the filter box has on the left.
            _btnSetting.Location = new Point(1126, 8);
            _btnSetting.Size = new Size(146, 50);
            _btnSetting.Click += new EventHandler(BtnSetting_Click);
            this.PanelFilter.Controls.Add(_btnSetting);
            _btnSetting.BringToFront();
            try
            {
                float dpi2 = 96f;
                try { dpi2 = this.DeviceDpi; } catch { }
                SetBtnAcIcon(_btnSetting,
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi2, dpi2)).GetLargeImage_Options());
            }
            catch { }

            // Demo 28/07 #1b: "Month Overview" — how many machines bill on each day of the
            // selected month and how many are already invoiced (created in code, strict designer).
            // #1b Month Overview as a HOVER icon (the old wide button broke the filter layout):
            // hovering shows the per-day machine counts as a tooltip; clicking still opens the
            // message box for a copyable view.
            _btnMonthOverview = new SimpleButton();
            _btnMonthOverview.Text = "";
            _btnMonthOverview.Location = new Point(242, 88);
            _btnMonthOverview.Size = new Size(26, 26);
            _btnMonthOverview.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            SetBtnSvgIcon(_btnMonthOverview, "svgimages/xaf/action_aboutinfo.svg");
            _btnMonthOverview.ImageOptions.ImageToTextIndent = 0;
            _btnMonthOverview.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            _btnMonthOverview.ToolTipTitle = "This Month — machines to bill";
            _btnMonthOverview.ToolTip = "How many machines fall on each billing day this month, and how " +
                "many are already invoiced. Hover for the summary, click for the full list.";
            _btnMonthOverview.MouseEnter += new EventHandler(BtnMonthOverview_MouseEnter);
            _btnMonthOverview.Click += new EventHandler(BtnMonthOverview_Click);
            this.GrpFilter.Controls.Add(_btnMonthOverview);
            _btnMonthOverview.BringToFront();

            // Billing YEAR made explicit and WYSIWYG: defaults to the current year and NEVER changes
            // itself (no auto "future month = last year" magic, no reset on month change — the user
            // rightly hated both). Cross-year runs (January billing December) = pick last year once.
            _cmbYear = new ComboBoxEdit();
            int yNow = DateTime.Today.Year;
            _cmbYear.Properties.Items.AddRange(new object[] { yNow - 2, yNow - 1, yNow, yNow + 1 });
            _cmbYear.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _cmbYear.Location = new Point(172, 90);
            _cmbYear.Size = new Size(64, 22);
            _cmbYear.SelectedItem = yNow;
            this.GrpFilter.Controls.Add(_cmbYear);
            _cmbYear.BringToFront();

            // Invoice grouping choice (overrides each contract's stored BillingMode when generating):
            //   ticked  = one invoice per CSSI (service item)
            //   unticked= one invoice per whole contract
            // Grouping choice: CHECKED (default) = one invoice per debtor/contract; unchecked = one
            // invoice per CSSI. Sits left-aligned under the action buttons.
            // Invoice grouping is a SETTING now (user decision 2026-08-04): Generate follows each
            // contract's own Billing Mode by default; the old "group same debtor" toggle lives in
            // Setting as an override. This label just shows the active mode.
            _lblGroupingInfo = new LabelControl();
            _lblGroupingInfo.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblGroupingInfo.Appearance.Options.UseFont = true;
            _lblGroupingInfo.Appearance.ForeColor = Color.FromArgb(27, 94, 32);
            _lblGroupingInfo.Appearance.Options.UseForeColor = true;
            _lblGroupingInfo.Location = new Point(510, 68);
            this.PanelFilter.Controls.Add(_lblGroupingInfo);
            _lblGroupingInfo.BringToFront();

            // ── Demo 28/07 #2: working-comfort row under the action buttons ──────────────
            // Meters filter (d) + Current Reading colour legend (c) + View Setting (b).
            // Laid out left to right with ONE gap constant, each control placed after the previous
            // one's real width. Hand-picked x positions gave this row gaps of 7, 18, 5 and 5 — and
            // any later addition had to guess at a slot, which is how a button ended up 15px on top
            // of a legend chip.
            const int ROW_Y = 94;          // row band top; tallest control is 28 high
            const int ROW_GAP = 12;
            int x = 510;

            LabelControl lblMeters = new LabelControl();
            lblMeters.Text = "Meters:";
            lblMeters.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblMeters.Size = new Size(46, 16);
            lblMeters.Appearance.ForeColor = Color.FromArgb(80, 80, 80);
            lblMeters.Appearance.Options.UseForeColor = true;
            lblMeters.Location = new Point(x, ROW_Y + (28 - lblMeters.Height) / 2);
            this.PanelFilter.Controls.Add(lblMeters);
            lblMeters.BringToFront();
            x += lblMeters.Width + ROW_GAP;

            _cmbMeterFilter = new ComboBoxEdit();
            _cmbMeterFilter.Properties.Items.AddRange(new object[] { "All", "BK only", "CL only", "BK + CL (meters only)", "Rental only" });
            _cmbMeterFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _cmbMeterFilter.SelectedIndex = 0;
            _cmbMeterFilter.Size = new Size(110, 22);
            _cmbMeterFilter.Location = new Point(x, ROW_Y + (28 - _cmbMeterFilter.Height) / 2);
            _cmbMeterFilter.ToolTip = "Show only black-and-white (BK) or colour (CL) meter rows.\r\n" +
                "Remembered per user — set it to BK today and it is still BK tomorrow.";
            _cmbMeterFilter.SelectedIndexChanged += new EventHandler(CmbMeterFilter_SelectedIndexChanged);
            this.PanelFilter.Controls.Add(_cmbMeterFilter);
            // The combo stays as the one place the choice lives (saved per user by index, read by
            // the filter and by the guard messages); it is just not the thing the user presses.
            // Five buttons press it, the way the Invoice Run shows its filters.
            _cmbMeterFilter.Visible = false;

            string[] meterCaps = new string[] { "All", "BK only", "CL only", "BK + CL", "Rental only" };
            int[] meterWidths = new int[] { 44, 62, 62, 68, 78 };
            _meterFilterButtons = new CheckButton[meterCaps.Length];
            for (int mi = 0; mi < meterCaps.Length; mi++)
            {
                CheckButton mb = new CheckButton();
                mb.Text = meterCaps[mi];
                mb.Tag = mi;
                mb.GroupIndex = 78;
                mb.Checked = mi == 0;
                mb.Size = new Size(meterWidths[mi], 28);
                mb.Location = new Point(x, ROW_Y);
                mb.ToolTip = mi == 4
                    ? "Only the rent and the waive that comes off it."
                    : "Show only these meter rows. Remembered per user - set it to BK today and it is still BK tomorrow.";
                mb.CheckedChanged += new EventHandler(MeterFilterButton_CheckedChanged);
                this.PanelFilter.Controls.Add(mb);
                mb.BringToFront();
                _meterFilterButtons[mi] = mb;
                x += mb.Width + 4;
            }
            x += ROW_GAP - 4;

            _lblLegendMust = MakeLegend("  MUST key in  ", Color.FromArgb(255, 213, 79), Color.FromArgb(102, 60, 0), true,
                "Current Reading cells in this colour still need a reading before you can invoice.");
            _lblLegendMust.Location = new Point(x, ROW_Y + (28 - _lblLegendMust.Height) / 2);
            this.PanelFilter.Controls.Add(_lblLegendMust);
            _lblLegendMust.BringToFront();
            x += _lblLegendMust.Width + ROW_GAP;

            _lblLegendNo = MakeLegend("  no key-in needed  ", Color.FromArgb(235, 235, 235), Color.DimGray, false,
                "Rental / waive / commitment rows, frozen snapshots and already-invoiced rows — nothing to key in.");
            _lblLegendNo.Location = new Point(x, ROW_Y + (28 - _lblLegendNo.Height) / 2);
            this.PanelFilter.Controls.Add(_lblLegendNo);
            _lblLegendNo.BringToFront();
            x += _lblLegendNo.Width + ROW_GAP;

            _btnViewSetting = new SimpleButton();
            _btnViewSetting.Text = "View Setting";
            _btnViewSetting.Size = new Size(120, 28);
            _btnViewSetting.Location = new Point(x, ROW_Y);
            _btnViewSetting.ToolTip = "Choose which columns this grid shows.";
            _btnViewSetting.Click += new EventHandler(BtnViewSetting_Click);
            this.PanelFilter.Controls.Add(_btnViewSetting);
            _btnViewSetting.BringToFront();

            // An earlier month with readings nobody billed. Months do NOT get mixed -- billing a
            // July row inside an October run would stamp it as October and the baseline chain would
            // never recover -- so this says where the work is and takes you there instead.
            //
            // It goes in the band under the four action buttons (y 58..94, the full width of the
            // panel), which is the only empty strip on this screen. Its first home was inside the
            // filter box at y 86, straight on top of the Month row -- a warning that hides the
            // control you need is worse than no warning.
            // What the chosen day is holding. It belongs to the STRIP -- last line inside the same
            // panel -- so it is always directly under the buttons and grows and moves with them.
            // Its first home was the status band at the top of the screen, where it landed on the
            // "Grouping: follow contract" line that was already living there.
            _lblDaySummary = new LabelControl();
            _lblDaySummary.AutoSizeMode = LabelAutoSizeMode.None;
            _lblDaySummary.Appearance.ForeColor = Color.FromArgb(60, 68, 74);
            _lblDaySummary.Appearance.Options.UseForeColor = true;
            _lblDaySummary.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;

            _lblOverdue = new LabelControl();
            _lblOverdue.AutoSizeMode = LabelAutoSizeMode.None;
            _lblOverdue.Location = new Point(790, 64);
            _lblOverdue.Size = new Size(480, 22);
            _lblOverdue.Appearance.BackColor = Color.FromArgb(253, 235, 231);
            _lblOverdue.Appearance.ForeColor = Color.FromArgb(150, 38, 22);
            _lblOverdue.Appearance.Options.UseBackColor = true;
            _lblOverdue.Appearance.Options.UseForeColor = true;
            _lblOverdue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            _lblOverdue.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblOverdue.ToolTip = "An earlier month still has readings nobody billed. Click to open "
                                + "it.\r\n\r\nNothing is billed across months: a reading belongs to the "
                                + "period it was taken in, and billing it inside another one would "
                                + "date it wrong for good.";
            _lblOverdue.Visible = false;
            _lblOverdue.Click += new EventHandler(LblOverdue_Click);
            this.PanelFilter.Controls.Add(_lblOverdue);
            _lblOverdue.BringToFront();

            RefreshGroupingInfo();
            BuildDayButtonStrip();

            // (The Contracts/Meters statistics block was removed on request — the tab captions and
            // the grid footer already carry the counts/totals.)

            // Footer: shows the meter-API URL + reachability (green = reachable, red = unreachable).
            _pnlFooter = new DevExpress.XtraEditors.PanelControl();
            _pnlFooter.Dock = DockStyle.Bottom;
            _pnlFooter.Height = 26;
            _pnlFooter.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            _lblApiStatus = new LabelControl();
            _lblApiStatus.Dock = DockStyle.Fill;
            _lblApiStatus.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblApiStatus.Appearance.Options.UseFont = true;
            _lblApiStatus.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            _lblApiStatus.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            _pnlFooter.Controls.Add(_lblApiStatus);
            this.Controls.Add(_pnlFooter);
            _tabView.BringToFront();   // keep the Dock=Fill grid above the bottom footer

            // "Thinking"-style fetch overlay: a marquee bar + cycling wording so the wait feels alive.
            _pnlFetching = new DevExpress.XtraEditors.PanelControl();
            _pnlFetching.Size = new Size(480, 100);
            _pnlFetching.Appearance.BackColor = Color.White;
            _pnlFetching.Appearance.Options.UseBackColor = true;
            _pnlFetching.Visible = false;
            LabelControl fetchTitle = new LabelControl();
            fetchTitle.Text = "Fetching meter readings";
            fetchTitle.Appearance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            fetchTitle.Appearance.Options.UseFont = true;
            fetchTitle.AutoSizeMode = LabelAutoSizeMode.None;
            fetchTitle.Location = new Point(24, 15);
            fetchTitle.Size = new Size(432, 22);
            _pnlFetching.Controls.Add(fetchTitle);
            _marquee = new DevExpress.XtraEditors.MarqueeProgressBarControl();
            _marquee.Location = new Point(24, 45);
            _marquee.Size = new Size(432, 16);
            _pnlFetching.Controls.Add(_marquee);
            _lblFetchMsg = new LabelControl();
            _lblFetchMsg.Appearance.Font = new Font("Segoe UI", 9F);
            _lblFetchMsg.Appearance.Options.UseFont = true;
            _lblFetchMsg.Appearance.ForeColor = Color.FromArgb(90, 90, 90);
            _lblFetchMsg.Appearance.Options.UseForeColor = true;
            _lblFetchMsg.AutoSizeMode = LabelAutoSizeMode.None;
            _lblFetchMsg.Location = new Point(24, 70);
            _lblFetchMsg.Size = new Size(432, 18);
            _pnlFetching.Controls.Add(_lblFetchMsg);
            this.Controls.Add(_pnlFetching);
            _fetchTimer = new System.Windows.Forms.Timer();
            _fetchTimer.Interval = 1000;
            _fetchTimer.Tick += new EventHandler(FetchTimer_Tick);

            // "Include 0 Meter Usage" filter (in the Filter Options group). Unticked = hide 0-usage rows.
            _chkInclude0Usage = new DevExpress.XtraEditors.CheckEdit();
            _chkInclude0Usage.Properties.Caption = "Include 0 Meter Usage   (invoiced-this-month rows always stay visible)";
            _chkInclude0Usage.Location = new Point(10, 122);
            _chkInclude0Usage.Size = new Size(478, 20);
            _chkInclude0Usage.Checked = false;   // default: hide 0-usage rows
            _chkInclude0Usage.CheckedChanged += new EventHandler(ChkInclude0Usage_CheckedChanged);
            this.GrpFilter.Controls.Add(_chkInclude0Usage);
            _chkInclude0Usage.BringToFront();
        }

        // Double-click any row → open the detail / override form for that whole contract.
        // EXCEPTIONS: double-click ON the Last Invoice No/Date cell opens THAT INVOICE directly;
        // on the Invoiced tab, double-click opens the machine's READING HISTORY (the audit trail
        // that explains how the invoice amount was produced).
        private void GridViewMeter_DoubleClick(object sender, EventArgs e)
        {
            DataRow r = V(sender).GetDataRow(V(sender).FocusedRowHandle);
            if (r == null) return;
            DevExpress.XtraGrid.Columns.GridColumn col = V(sender).FocusedColumn;
            if (col != null && (col.FieldName == "LastInvNo" || col.FieldName == "LastInvDate"))
            {
                string docNo = S(r["LastInvNo"]).Trim();
                if (docNo.Length > 0) { OpenInvoiceByDocNo(docNo); return; }
            }
            // Double-click the CONTRACT cell and you get the contract. The invoice columns already
            // work this way, so the column you clicked deciding where you land is the rule here, not
            // an exception. Everywhere else on the row still opens the machine's meter detail.
            if (col != null && col.FieldName == "ContractNo")
            {
                OpenContractByKey(D64(r["ContractKey"]), S(r["ContractNo"]));
                return;
            }
            if (_tabView != null && _tabView.SelectedTabPage == _pageDone)
            {
                using (MeterReadingHistory_Form h = new MeterReadingHistory_Form(
                    _dbSetting, D64(r["ItemKey"]), S(r["ServiceItemNo"]), S(r["SerialNo"]), S(r["LastInvNo"])))
                {
                    h.ShowDialog(this);
                }
                return;
            }
            // Demo 28/07 #3b: double-clicking a row opens THAT machine only — not the whole
            // contract's machines grouped together.
            if (!AllowRowDoubleClickDetail) return;
            OpenContractDetail(D64(r["ContractKey"]), S(r["ContractNo"]), S(r["Customer"]),
                D64(r["ItemKey"]), S(r["ServiceItemNo"]));
        }

        // Open an invoice in AutoCount's native Invoice entry form by its document number
        // (same proven pattern as the detail dialog's Generated Invoice grid).
        private void OpenInvoiceByDocNo(string docNo)
        {
            try
            {
                object k = _dbSetting.ExecuteScalar(
                    "SELECT DocKey FROM dbo.IV WHERE DocNo = N'" + docNo.Replace("'", "''") + "'");
                if (k == null || k == DBNull.Value)
                { XtraMessageBox.Show("Invoice " + docNo + " not found — it may have been deleted.", "Open Invoice"); return; }
                long docKey = Convert.ToInt64(k);
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                        AutoCount.Authentication.UserSession.CurrentUserSession, _dbSetting);
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(docKey);
                if (doc == null)
                { XtraMessageBox.Show("Invoice " + docNo + " could not be opened.", "Open Invoice"); return; }
                using (AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry f =
                    new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc))
                {
                    f.ShowDialog(this);
                }
                LoadData();   // the user may have edited/cancelled/deleted it inside the entry form
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Could not open the invoice:\r\n" + ex.Message, "Open Invoice"); }
        }

        // FOC Qty display: show the free allowance the CHARGE ENGINE actually deducts, so the
        // customer's own arithmetic (NET = Usage - FOC, Charge = NET x Rate) reconciles off the
        // screen every time. Two things make the raw FOCQty column lie:
        //   - a LADDER meter's free copies come from the ladder's 0.00 band; the engine ignores the
        //     meter's own FOCQty entirely when a ladder is in effect
        //   - the FOC RESET accrual multiplies the allowance when a bill spans N reset periods
        //     (weekly / every-N-days contracts), so the stored figure understates what was deducted
        private void GridViewMeter_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "FOCQty" || e.ListSourceRowIndex < 0) return;
            DataRow r = GridSourceRow(e.ListSourceRowIndex);
            if (r == null) return;
            int resetN = ServiceContractPhotocopier.Classes.ScpInvoiceJobs.FocResetCountFor(r, SelectedYear(), SelectedMonth());
            if (resetN < 1) resetN = 1;
            string code = S(r["MultiPriceCode"]);
            decimal applied;
            if (_ladders != null && ServiceContractPhotocopier.Classes.ScpMultiPrice.HasLadder(_ladders, code))
                applied = ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(_ladders[code]) * resetN;
            else if (resetN > 1)
                applied = Dec(r["FOCQty"]) * resetN;
            else
                return;   // plain meter, single reset period — the stored value is already the truth
            e.DisplayText = applied.ToString("#,##0.##");
        }

        // Resolve a LIST-SOURCE index to its DataRow. The grid is bound to the tab's filtered
        // DataView (ApplyTabFilter), NOT to _dtGrid.DefaultView — indexing the wrong one hands back
        // a DIFFERENT machine's row, which is how the FOC Qty column ended up showing one meter's
        // ladder allowance on another meter's line.
        private DataRow GridSourceRow(int listSourceRowIndex)
        {
            if (listSourceRowIndex < 0) return null;
            DataView dv = ActiveGrid.DataSource as DataView;
            if (dv != null) return listSourceRowIndex < dv.Count ? dv[listSourceRowIndex].Row : null;
            DataTable dt = ActiveGrid.DataSource as DataTable;
            if (dt != null) return listSourceRowIndex < dt.Rows.Count ? dt.Rows[listSourceRowIndex] : null;
            return null;
        }

        // Plain white rows (the per-item blue/white zebra shading was removed on request) — only
        // unresolved fetch conflicts still tint their row light red.
        /// <summary>
        /// Flags the rows whose day has gone and which still have not been billed.
        ///
        /// <para>Only the screen can decide this: the row knows the day it is due, but not the
        /// day being worked.</para>
        /// </summary>
        private void MarkLate(int today)
        {
            if (_dtGrid == null || !_dtGrid.Columns.Contains("Late")) return;
            foreach (DataRow r in _dtGrid.Rows)
            {
                int day = r["DueDay"] == DBNull.Value ? 0 : Convert.ToInt32(r["DueDay"]);
                bool billed = Convert.ToString(r["InvoicedDocNo"]).Trim().Length > 0;
                r["Late"] = !billed && day > 0 && day < today;
            }
        }

        private void GridViewMeter_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;   // group rows
            object cf = V(sender).GetRowCellValue(e.RowHandle, "HasConflict");
            if (cf != null && cf != DBNull.Value && Convert.ToBoolean(cf))
            {
                Color rc = Color.FromArgb(255, 224, 224);   // light red = unresolved conflict
                e.Appearance.BackColor = rc; e.Appearance.BackColor2 = rc;  // flat (no gradient)
                e.Appearance.ForeColor = Color.Black;       // focused-row white text vanishes on pale tints
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            // Its day has gone and it is still not billed. Two different situations wear two
            // different colours, because only one of them is ours to fix:
            //
            //   a rental, or a meter that HAS its reading  ->  red. We owe the customer an invoice.
            //   a meter still waiting for a reading        ->  amber. Nobody can bill it yet.
            object lt = V(sender).GetRowCellValue(e.RowHandle, "Late");
            if (lt != null && lt != DBNull.Value && Convert.ToBoolean(lt))
            {
                object nm = V(sender).GetRowCellValue(e.RowHandle, "NeedManual");
                bool waiting = nm != null && nm != DBNull.Value && Convert.ToBoolean(nm);
                Color lc = waiting ? Color.FromArgb(255, 243, 214) : Color.FromArgb(255, 226, 222);
                e.Appearance.BackColor = lc; e.Appearance.BackColor2 = lc;
                e.Appearance.ForeColor = waiting ? Color.Black : Color.FromArgb(140, 30, 20);
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            // Pale orange = the machine's expiry is BEFORE this billing month ("Include expired"
            // setting is showing it) — visible at a glance so it is never billed by accident.
            object xp = V(sender).GetRowCellValue(e.RowHandle, "IsExpired");
            if (xp != null && xp != DBNull.Value && Convert.ToBoolean(xp))
            {
                Color oc = Color.FromArgb(255, 236, 214);
                e.Appearance.BackColor = oc; e.Appearance.BackColor2 = oc;
                e.Appearance.ForeColor = Color.Black;       // same: keep expired rows readable when focused
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
            }
        }

        /// <summary>Open the contract itself, from the meter row that bills it.</summary>
        private void OpenContractByKey(long contractKey, string contractNo)
        {
            if (_dbSetting == null) return;
            if (contractKey <= 0)
            {
                XtraMessageBox.Show("This row is not linked to a contract" +
                    (contractNo.Length > 0 ? " (" + contractNo + ")" : "") + ".",
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form f =
                    new ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form(_dbSetting, contractKey);
                f.Show();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open contract " + contractNo + ":\r\n" + ex.Message,
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void TabView_SelectedPageChanged(object sender, TabPageChangedEventArgs e)
        {
            if (e.Page == null) return;
            // Nothing to move and nothing to re-shape: the tab already holds its own grid, with its
            // own columns and its own rows. Switching tabs is now just switching tabs.
            UpdateSelectAllButton();
        }

        private static string TabKey(XtraTabPage page)
        {
            if (page == null) return "";
            return string.IsNullOrEmpty(page.Name) ? page.Text : page.Name;
        }

        /// <summary>
        /// Re-bind EVERY tab, each to its own rows. All five grids read the same in-memory table
        /// through their own DataView, so a tab switch shows what that tab already holds instead of
        /// re-filtering and re-shaping one shared grid on the way in.
        /// </summary>
        private void ApplyTabFilter()
        {
            if (_tabView == null) return;
            // Demo #2(a): post any half-typed Current Reading BEFORE the DataView swap below —
            // posting fires CellValueChanged → Recalc + SaveInlineReading while the row is still
            // bound. Covers tab switches, the 0-usage toggle and the meter filter in one spot.
            try { ActiveView.CloseEditor(); ActiveView.UpdateCurrentRow(); } catch { }
            if (_dtGrid == null) { UpdateSelectAllButton(); return; }

            for (int i = 0; i < _pages.Count; i++)
            {
                XtraTabPage page = _pages[i];
                GridControl g;
                GridView v;
                if (!_gridByPage.TryGetValue(page, out g) || !_viewByPage.TryGetValue(page, out v)) continue;
                DataView dv = new DataView(_dtGrid, TabRowFilter(page), "", DataViewRowState.CurrentRows);
                g.DataSource = dv;
                v.ActiveFilterString = "";
                v.ExpandAllGroups();   // customer groups start open
            }
            UpdateSelectAllButton();   // caption follows the tab's selection state
        }

        /// <summary>The rows that belong to one tab.</summary>
        private string TabRowFilter(XtraTabPage page)
        {
            string f = "";
            // "Need Manual Key-In" membership is a SNAPSHOT (NeedManual flag, recomputed only at
            // load/fetch): keying a reading keeps the row on this tab so the operator can review what
            // they just typed — it only leaves the tab on the next Refresh / Fetch.
            // Tabs are PER ROW. Need Manual = the readings somebody still has to type: usage meters
            // (BK / CL) with nothing keyed yet. Rental, minimum and waive rows have no meter to read,
            // so they never sit there (asked 2026-09-11) -- they wait on Ready to Invoice, and whether
            // one may go out before its BK / CL is the Generate guard's question, not this tab's.
            if (page == _pageWith) f = "[NeedManual] = False AND ISNULL([InvoicedDocNo],'') = ''";
            else if (page == _pageNo) f = "[NeedManual] = True";
            // A contract that has not started yet stays off both working tabs until the day it
            // starts (feedback ATP-11): there is no reading to key and nothing to bill, so it is
            // only in the operator's way. It is not dropped from the run -- Invoiced still shows
            // it if somebody has already billed it (a start date moved later, say), and the
            // Conflicts tab still shows a clash worth looking at.
            if (page == _pageWith || page == _pageNo)
                f = "(" + f + ") AND [NotStarted] = False";
            else if (page == _pageDone) f = "[InvoicedDocNo] <> ''";   // this month's billed machines
            else if (page == _pageConflict) f = "[HasConflict] = True";

            // Hide zero-usage meters unless "Include 0 Meter Usage" is ticked. Exceptions that never
            // hide: rows that still BILL (minimum charges -> TotalCharges > 0, or Generate would miss
            // them), rows ALREADY INVOICED for the selected month (usage reset to 0 on billing), rows
            // that are not readings at all (rental, waive, minimum: nothing was ever read, so a usage
            // of 0 says nothing about them), and the whole "Need Manual Key-In" tab — its rows are
            // 0-usage BY DEFINITION (nothing keyed yet), so the usage filter would blank the tab out.
            if (_chkInclude0Usage != null && !_chkInclude0Usage.Checked && page != _pageNo)
            {
                string usage = "([MeterUsage] <> 0 OR [TotalCharges] <> 0 OR [InvoicedDocNo] <> '' OR [IsFlat] = True OR [IsWaive] = True OR [Role] = 'COMMIT')";
                f = string.IsNullOrEmpty(f) ? usage : "(" + f + ") AND " + usage;
            }
            // Demo #2(d): "Meters" filter — the operator working a BK-only (or CL-only) run does not
            // want the other meter's rows in the way. Rental/waive/commit rows are NOT meters, so any
            // non-"All" choice drops them too.
            string role = MeterRoleFilter();
            if (role.Length > 0) f = string.IsNullOrEmpty(f) ? role : "(" + f + ") AND " + role;
            // Each tab is its OWN datasource (DataView.RowFilter over the master table) — NOT the
            // grid's removable filter panel. The user can no longer X the system filter away and end
            // up selecting/generating from the wrong row set; the grid filter row stays purely theirs.
            return f;
        }

        // The Invoiced tab reads like an INVOICE HISTORY, not a key-in sheet: reading/pricing columns
        // hide, the invoice columns (No / Date / Total) carry the story. Other tabs restore the
        // normal working layout.
        private static readonly string[] _keyInLayoutCols = new string[] {
            "SelCssi", "MachineStatus", "MinCharges", "UnitPrice", "FOCQty", "RebatePct",
            "LastAuditDate", "CurrentReading", "MeterUsage", "TotalCharges" };

        private void ApplyTabColumnLayout()
        {
            // The view carries its own tab (set when the grid was built), so this is correct while
            // configuring a tab that is not in front. Reading the tab strip instead is what put the
            // key-in columns on the Invoiced tab.
            bool inv = (ActiveView.Tag as XtraTabPage) == _pageDone;
            foreach (string c in _keyInLayoutCols)
                if (ActiveView.Columns[c] != null) ActiveView.Columns[c].Visible = !inv;
            GridColumn tot = ActiveView.Columns["InvTotal"];
            if (tot != null)
            {
                tot.Visible = inv;
                if (inv && ActiveView.Columns["LastInvDate"] != null)
                    tot.VisibleIndex = ActiveView.Columns["LastInvDate"].VisibleIndex + 1;
            }
        }

        private void ChkInclude0Usage_CheckedChanged(object sender, EventArgs e) { ApplyTabFilter(); }

        private Form _colourKeyWin;

        /// <summary>
        /// Show / hide the colour key.
        ///
        /// It lives in its OWN small window, not as a panel on this form. Added to the form it
        /// became part of the form's scroll region, and a control positioned past the client edge
        /// pushed the whole screen out of place — the title clipped, the tabs cut off at the left.
        /// A reference card must not be able to move the thing it explains.
        /// </summary>
        private void ShowColourKey()
        {
            if (_colourKeyWin != null && !_colourKeyWin.IsDisposed)
            {
                _colourKeyWin.Close();
                _colourKeyWin = null;
                return;
            }
            _colourKeyWin = BuildColourKeyWindow();
            _colourKeyWin.Location = Control.MousePosition;   // opens where you asked for it
            _colourKeyWin.Show(this);           // owned: it closes with the module
        }

        /// <summary>
        /// The colour key: every tint the grid uses, with what it means, painted in the same colours
        /// the grid paints — read from one place so the card cannot drift from the grid it explains.
        /// </summary>
        private Form BuildColourKeyWindow()
        {
            Form w = new Form();
            w.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            w.Text = "What the colours mean";
            w.StartPosition = FormStartPosition.Manual;
            w.ShowInTaskbar = false;
            w.MinimizeBox = false;
            w.MaximizeBox = false;
            w.ClientSize = new Size(470, 214);
            w.BackColor = Color.White;
            w.FormClosed += delegate { _colourKeyWin = null; };

            AddColourKeyRow(w, 0, Color.FromArgb(255, 236, 214), Color.Black, false,
                "Whole row — the contract EXPIRED before this billing month");
            AddColourKeyRow(w, 1, Color.FromArgb(255, 224, 178), Color.FromArgb(191, 87, 0), true,
                "Machine Status — OFFLINE: the reading must be keyed in");
            AddColourKeyRow(w, 2, Color.FromArgb(200, 230, 201), Color.FromArgb(27, 94, 32), true,
                "Machine Status — ONLINE: the reading came from the API");
            AddColourKeyRow(w, 3, Color.FromArgb(255, 224, 224), Color.FromArgb(198, 40, 40), false,
                "Whole row — CONFLICT: keyed reading differs from the fetched one");
            AddColourKeyRow(w, 4, Color.FromArgb(255, 213, 79), Color.FromArgb(102, 60, 0), true,
                "Current Reading — MUST key in, still empty");
            AddColourKeyRow(w, 5, Color.FromArgb(255, 249, 196), Color.Black, false,
                "Current Reading — the column you type in");
            AddColourKeyRow(w, 6, Color.FromArgb(223, 240, 216), Color.FromArgb(27, 94, 32), false,
                "Total Charges — the money this row bills");
            AddColourKeyRow(w, 7, Color.FromArgb(200, 230, 201), Color.FromArgb(27, 94, 32), true,
                "Last Invoice No / Date — already invoiced this period");
            return w;
        }

        private void AddColourKeyRow(Form w, int index, Color back, Color fore, bool bold, string meaning)
        {
            int y = 10 + index * 25;
            LabelControl swatch = new LabelControl();
            swatch.Text = "";
            swatch.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            swatch.Size = new Size(30, 18);
            swatch.Location = new Point(12, y);
            swatch.Appearance.BackColor = back;
            swatch.Appearance.Options.UseBackColor = true;
            swatch.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            w.Controls.Add(swatch);

            LabelControl text = new LabelControl();
            text.Text = meaning;
            text.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            text.Size = new Size(410, 18);
            text.Location = new Point(50, y + 1);
            text.Appearance.Font = new Font("Segoe UI", 8.25F, bold ? FontStyle.Bold : FontStyle.Regular);
            text.Appearance.ForeColor = fore;
            text.Appearance.Options.UseFont = true;
            text.Appearance.Options.UseForeColor = true;
            w.Controls.Add(text);
        }

        // A colour swatch label for the Current Reading legend — same colours the cell style paints.
        private LabelControl MakeLegend(string text, Color back, Color fore, bool bold, string tip)
        {
            LabelControl l = new LabelControl();
            l.Text = text;
            l.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            Font f = new Font("Segoe UI", 8F, bold ? FontStyle.Bold : FontStyle.Regular);
            // MEASURED, not text.Length * 7. Seven pixels a character is right for about half the
            // alphabet: "no key-in needed" came out ~28px wider than its text, so the grey chip ran
            // on past its own label and collided with whatever sat beside it.
            l.Size = new Size(TextRenderer.MeasureText(text, f).Width + 10, 20);
            l.Appearance.Font = f;
            l.Appearance.Options.UseFont = true;
            l.Appearance.BackColor = back;
            l.Appearance.Options.UseBackColor = true;
            l.Appearance.ForeColor = fore;
            l.Appearance.Options.UseForeColor = true;
            l.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            l.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            l.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            l.ToolTip = tip;
            return l;
        }

        // RowFilter clause for the Meters combo (empty = All meters, no clause).
        private string MeterRoleFilter()
        {
            if (_cmbMeterFilter == null) return "";
            int i = _cmbMeterFilter.SelectedIndex;
            if (i == 1) return "[Role] = 'BK'";
            if (i == 2) return "[Role] = 'CL'";
            if (i == 3) return "([Role] = 'BK' OR [Role] = 'CL')";
            // The rental run: the rent, and the waive that comes off it, and nothing else. Appended
            // rather than slotted in beside "BK only" -- the choice is remembered as a NUMBER, and
            // reordering the list would silently change what everybody's saved setting means.
            if (i == 4) return "([Role] = 'RENTAL' OR [Role] = 'WAIVE' OR [IsFlat] = True)";
            return "";
        }

        // The Meters choice is remembered per user: a clerk who works the BK run should not have to
        // re-pick it every morning. It stays VISIBLE in the combo, so a filter that hides rows is
        // never a mystery — the reason is on screen.
        private string MeterFilterKey()
        {
            string user = "";
            try { user = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? ""; } catch { }
            return "METER_ROLE_FILTER_" + user;
        }

        private void RestoreMeterFilter()
        {
            if (_dbSetting == null || _cmbMeterFilter == null) return;
            try
            {
                int i = ServiceContractPhotocopier.Data.PumsConfig.GetInt(_dbSetting, MeterFilterKey(), 0);
                if (i < 0 || i >= _cmbMeterFilter.Properties.Items.Count) i = 0;
                bool prev = _suppressFilterEvent;
                _suppressFilterEvent = true;      // restoring is not the operator changing it
                _cmbMeterFilter.SelectedIndex = i;
                _suppressFilterEvent = prev;
            }
            catch { }
        }

        private void SaveMeterFilter()
        {
            if (_dbSetting == null || _cmbMeterFilter == null) return;
            try
            {
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting, MeterFilterKey(),
                    _cmbMeterFilter.SelectedIndex.ToString());
            }
            catch { }
        }

        private void CmbMeterFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncMeterFilterButtons();   // a restore or a programmatic change shows on the buttons too
            if (_suppressFilterEvent) return;
            SaveMeterFilter();
            ApplyTabFilter();
        }

        /// <summary>A button pressed = the combo's choice changed; the combo does the rest.</summary>
        private void MeterFilterButton_CheckedChanged(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (b == null || !b.Checked || _cmbMeterFilter == null || b.Tag == null) return;
            int i = Convert.ToInt32(b.Tag);
            if (_cmbMeterFilter.SelectedIndex != i) _cmbMeterFilter.SelectedIndex = i;
        }

        private void SyncMeterFilterButtons()
        {
            if (_meterFilterButtons == null || _cmbMeterFilter == null) return;
            int i = _cmbMeterFilter.SelectedIndex;
            if (i < 0 || i >= _meterFilterButtons.Length) return;
            if (!_meterFilterButtons[i].Checked) _meterFilterButtons[i].Checked = true;
        }

        // #2(b): let the user choose which columns the grid shows. The column chooser can do this
        // too, but only if you know to right-click the header — this is the discoverable door.
        // It applies to the tab in front, which is the tab whose columns you are looking at; every
        // tab now has its own grid, so each keeps its own choice.
        private void BtnViewSetting_Click(object sender, EventArgs e)
        {
            System.Collections.Generic.List<GridColumn> cols = new System.Collections.Generic.List<GridColumn>();
            foreach (string fn in _viewSettingCols)
            {
                GridColumn c = ActiveView.Columns[fn];
                if (c != null) cols.Add(c);
            }
            if (cols.Count == 0) return;

            GridView view = ActiveView;
            System.Collections.Generic.List<GridColumn> wasVisible = new System.Collections.Generic.List<GridColumn>();
            foreach (GridColumn c in view.Columns) if (c.Visible) wasVisible.Add(c);

            using (MeterViewSetting_Form f = new MeterViewSetting_Form(cols))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                f.ApplySelection();
            }

            // Scroll to the first newly-shown column. A column can come back anywhere along twenty-odd
            // columns of horizontal scroll, and one that is on screen but out of sight is
            // indistinguishable from one that never appeared.
            GridColumn firstShown = null;
            foreach (GridColumn c in view.Columns)
                if (c.Visible && !wasVisible.Contains(c)) { firstShown = c; break; }
            if (firstShown != null)
            {
                try { view.FocusedColumn = firstShown; } catch { }
            }

            // And say so if a column did NOT take. Twice now this has been reported as "not working"
            // with nothing on screen to say why; a change that silently fails to apply must name
            // itself rather than leave the operator to wonder whether they mis-clicked.
            System.Text.StringBuilder stuck = new System.Text.StringBuilder();
            foreach (GridColumn c in cols)
                if (c.Visible && c.VisibleIndex < 0)
                    stuck.Append(stuck.Length > 0 ? ", " : "").Append(c.Caption);
            if (stuck.Length > 0)
                XtraMessageBox.Show(
                    "These columns were switched on but the grid did not place them: " + stuck +
                    ".\r\n\r\nRight-click a column header > Column Chooser and drag them in from there.",
                    "View Setting", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }


        // Snapshot the "Need Manual Key-In" membership per MACHINE: a machine needs key-in when NONE
        // of its meters has a reading from anywhere (no key-in, no API value). Called only at load
        // and after a fetch — keying a reading later does NOT remove the row until the next reload.
        // Flat / rental meters have no meter reading: auto-bill them as quantity 1 x Unit Price (the
        // rental amount) every period. Reading is forced to LastReading+1 so usage = 1; the invoice
        // builder also forces usage 1 for flat lines (see ScpInvoiceBuilder.ComputeCharge). Already-
        // invoiced periods and manual overrides are left untouched.
        /// <summary>Where a contract owns the price of a merged rental line, that price wins over
        /// whatever each machine's own meter says.</summary>
        /// <remarks>
        /// Applied once, here, before anything computes money -- so the grid, the charge engine, the
        /// reading log and the invoice all see the same number and there is no second place where a
        /// rental gets priced. A machine added to a priced group next month is billed at the group's
        /// price without anyone opening its meter.
        ///
        /// <para>Nothing is touched unless the contract has a Billing Format, merges its rental lines,
        /// and someone has actually typed a price for that group. Rental only: black and colour keep
        /// their own per-meter rates.</para>
        /// </remarks>
        private void ApplyRentalGroupPrices()
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.ApplyRentalGroupPrices(_dbSetting, _dtGrid);
        }

        /// <summary>
        /// The rate an agreed BK or CL line charges, stamped onto every machine on that line.
        ///
        /// <para>A merged usage line has one Qty x Unit Price like any other, so machines whose own
        /// rates differ cannot share it. Agreeing a figure for the line is what makes them one line:
        /// without it they stay apart, correctly, because there is no honest single rate to print.
        /// This runs before the fold, so the fold sees them already agreeing.</para>
        /// </summary>
        private void ApplyMeterLinePrices()
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.ApplyMeterLinePrices(_dbSetting, _dtGrid);
        }

        /// <summary>Is this grid row a committed minimum? The same test the billing engine makes —
        /// role first, the legacy "MIN..." type name as the OR that keeps old machines working.</summary>
        private static bool IsCommitRow(DataRow r)
        {
            return ServiceContractPhotocopier.Classes.ScpBillingRows.IsCommitRow(r);
        }

        private void AutoFillFlatMeters()
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.AutoFillFlatMeters(
                _dbSetting, _dtGrid, SelectedYear(), SelectedMonth(), _ladders);
            SyncSelCssi();
        }

        private void RecomputeNeedManual()
        {
            if (_dtGrid == null) return;
            // PER ROW: a row "needs manual key-in" when it is a USAGE meter (BK / CL -- anything that
            // is read) with no reading and no invoice yet. Rental, minimum and waive rows have no
            // meter to read, so they are never on the Need Manual Key-In tab: that tab is the list of
            // readings somebody still has to type, and a rental on it is a row nobody can act on
            // (asked 2026-09-11). A machine whose BK is unread therefore shows its rental on Ready to
            // Invoice and its BK on Need Manual Key-In. Whether the rental may go out alone is the
            // Generate guard's question: same invoice -> it names the missing meter and stops; rental
            // on its own invoice -> it goes.
            foreach (DataRow r in _dtGrid.Rows)
                r["NeedManual"] = RowNeedsReading(r);
        }

        /// <summary>A reading somebody still has to type: a usage meter with nothing keyed and nothing
        /// fetched, not yet invoiced. Rental, waive and minimum rows are money, not readings.</summary>
        private bool RowNeedsReading(DataRow r)
        {
            if (r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"])) return false;     // rental / flat: auto-billed
            if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) return false;   // the contra off the rent
            string role = S(r["Role"]).Trim().ToUpperInvariant();
            if (role == "RENTAL" || role == "WAIVE" || role == "COMMIT") return false;          // minimum: an amount, no meter
            if (S(r["InvoicedDocNo"]).Trim().Length > 0) return false;                          // already invoiced this period
            bool hasReading = Dec(r["CurrentReading"]) > 0m || Dec(r["FetchedReading"]) > 0m;
            return !hasReading;
        }

        private void UpdateTabCounts()
        {
            if (_dtGrid == null || _tabView == null) return;
            // Count DISTINCT service items (machines), not meter rows: a colour copier has a BK + a
            // CL meter, so per-meter counts double it. Tabs/summary read more naturally per machine.
            int sel = 0;
            decimal selCharge = 0m;
            System.Collections.Generic.HashSet<string> allItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> withItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> onlineItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> offlineItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> conflictItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> needManualItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> invoicedItems = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> contracts = new System.Collections.Generic.HashSet<string>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                string itemKey = S(r["ItemKey"]);
                allItems.Add(itemKey);
                contracts.Add(S(r["ContractKey"]));
                string ms = S(r["MachineStatus"]);
                if (ms == "ONLINE") onlineItems.Add(itemKey);
                else if (ms == "OFFLINE") offlineItems.Add(itemKey);
                // The same three tests the tab filters make, row by row, so each count is the number
                // of machines that tab actually shows. A machine with its rental ready and its BK
                // unread is on BOTH Ready to Invoice and Need Manual Key-In, and is counted on both.
                bool inv = S(r["InvoicedDocNo"]).Trim().Length > 0;
                bool nm = r["NeedManual"] != DBNull.Value && Convert.ToBoolean(r["NeedManual"]);
                bool ns = r["NotStarted"] != DBNull.Value && Convert.ToBoolean(r["NotStarted"]);
                if (inv) invoicedItems.Add(itemKey);
                else if (ns) { }                     // not started: on neither tab, so on neither count
                else if (nm) needManualItems.Add(itemKey);
                else withItems.Add(itemKey);
                if (r["HasConflict"] != DBNull.Value && Convert.ToBoolean(r["HasConflict"])) conflictItems.Add(itemKey);
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                { sel++; selCharge += Dec(r["TotalCharges"]); }
            }
            _pageWith.Text = "Ready to Invoice (" + withItems.Count + ")";
            _pageNo.Text = "Need Manual Key-In (" + needManualItems.Count + ")";   // snapshot flag = what the tab actually shows
            _pageDone.Text = "Invoiced (" + invoicedItems.Count + ")";
            _pageAll.Text = "All (" + allItems.Count + ")";
            _pageConflict.Text = "⚠ Conflicts (" + conflictItems.Count + ")";
            // Conflicts tab only exists while there IS something to resolve; if it disappears from
            // under the user's feet, fall back to the Ready to Invoice view.
            bool hasConflicts = conflictItems.Count > 0;
            if (!hasConflicts && _tabView.SelectedTabPage == _pageConflict) _tabView.SelectedTabPage = _pageWith;
            _pageConflict.PageVisible = hasConflicts;
        }

        // Visual grouping WITHOUT real DataView grouping (which is incompatible with AllowCellMerge):
        //   • Contract / Customer / Mode  → merge across the whole CONTRACT (one cell per contract).
        //   • Service Item No / Serial     → merge across each SERVICE ITEM (one cell per item).
        //   • Every meter column keeps its own value (no merge).
        private void GridViewMeter_CellMerge(object sender, DevExpress.XtraGrid.Views.Grid.CellMergeEventArgs e)
        {
            string f = e.Column.FieldName;
            bool perContract = (f == "ContractNo" || f == "Mode");
            bool perDebtor = (f == "Customer");   // ONE merged cell per CUSTOMER, across their contracts
            bool perItem = (f == "ServiceItemNo" || f == "SerialNo" || f == "MachineStatus" || f == "SelCssi");
            if (!perContract && !perDebtor && !perItem) { e.Merge = false; e.Handled = true; return; }
            string keyField = perDebtor ? "DebtorCode" : (perContract ? "ContractKey" : "ItemKey");
            object k1 = V(sender).GetRowCellValue(e.RowHandle1, keyField);
            object k2 = V(sender).GetRowCellValue(e.RowHandle2, keyField);
            e.Merge = (k1 != null && k2 != null && k1.ToString() == k2.ToString());
            // Serial + status are per MACHINE: a multi-machine item's rows only merge within the same
            // machine serial (SelCssi stays per item — selection is per CSSI by design).
            if (e.Merge && (f == "SerialNo" || f == "MachineStatus"))
            {
                string s1 = S(V(sender).GetRowCellValue(e.RowHandle1, "SerialNo"));
                string s2 = S(V(sender).GetRowCellValue(e.RowHandle2, "SerialNo"));
                e.Merge = string.Equals(s1.Trim(), s2.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            e.Handled = true;
        }

        private int SelectedMonth()
        {
            int idx = this.CmbMonth.SelectedIndex;
            return (idx >= 0 && idx <= 11) ? idx + 1 : DateTime.Today.Month;
        }
        private int SelectedDay()
        {
            object v = this.CmbDay.SelectedItem;
            int d;
            if (v != null && int.TryParse(v.ToString(), out d) && d >= 1 && d <= 31) return d;
            return DateTime.Today.Day;
        }

        // Billing YEAR for the selected month: a month LATER than the current one means the previous
        // year (e.g. running December's billing in January 2027 -> 2026-12). Billing never targets a
        // future period, so "December selected in July" is last December, not next.
        private static int AutoYearFor(int month)
        {
            return month > DateTime.Today.Month ? DateTime.Today.Year - 1 : DateTime.Today.Year;
        }
        private int SelectedYear()
        {
            // Explicit Year combo wins; the auto rule (future month = last year) is only the default.
            if (_cmbYear != null && _cmbYear.SelectedItem != null)
            {
                int y;
                if (int.TryParse(_cmbYear.SelectedItem.ToString(), out y) && y > 2000) return y;
            }
            return AutoYearFor(SelectedMonth());
        }

        // Fill the Day combo with only the billing days actually in use (distinct effective billing
        // day = COALESCE(item override, contract day) across active items), instead of a fixed 1-31
        // list. Called after _dbSetting is set (InitDefaults runs before that, so it seeds 1-31).
        private void PopulateDayCombo()
        {
            if (_dbSetting == null) return;
            int prev = SelectedDay();
            try
            {
                bool inclInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);
                // The days that exist are the days ROWS are due on -- asked of the meters, not of
                // the contracts. A contract billing its meters on the 7th and its rental on the 1st
                // puts BOTH days on the strip; asking the contract alone gives only the 7th, and the
                // day the rental is due has no button to press.
                string sql = "SELECT DISTINCT " + ScpBillingRows.RowDueDaySql + " AS d " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "WHERE " + (inclInactive ? "1=1 " : "i.Inactive='N' AND c.Inactive='N' ") +
                    "AND " + ScpBillingRows.RowDueDaySql + " BETWEEN 1 AND 31 ORDER BY d";
                DataTable dt = QueryWithTimeout(sql, 60);
                if (dt != null && dt.Rows.Count > 0)
                {
                    this.CmbDay.Properties.Items.Clear();
                    foreach (DataRow r in dt.Rows)
                        this.CmbDay.Properties.Items.Add(Convert.ToInt32(r["d"]));
                }
            }
            catch { }
            int idx = this.CmbDay.Properties.Items.IndexOf(prev);
            if (idx < 0)
            {
                // Default = the NEAREST UPCOMING billing day (today 17th, days 1/7/20/25 -> 20).
                // No day left this month -> the earliest day (that's next month's first run).
                int today = DateTime.Today.Day;
                for (int i = 0; i < this.CmbDay.Properties.Items.Count; i++)
                    if (Convert.ToInt32(this.CmbDay.Properties.Items[i]) >= today) { idx = i; break; }
            }
            if (idx < 0 && this.CmbDay.Properties.Items.Count > 0) idx = 0;
            _suppressFilterEvent = true;
            this.CmbDay.SelectedIndex = idx;
            _suppressFilterEvent = false;
            RebuildDayButtons();   // the flyout-style day strip mirrors the (hidden) combo's items
        }

        // ── Day picker: flyout-style button strip (selected day = BLUE). The hidden CmbDay stays
        //    the value holder, so SelectedDay()/reload logic is completely untouched. ──
        private DevExpress.XtraEditors.PanelControl _pnlDays;

        /// <summary>
        /// Looks back for a month whose readings were never billed, and says so.
        ///
        /// <para>It counts one exact thing: staged readings in an EARLIER period that carry no
        /// invoice. Not "everything that could have been billed since the contract started" --
        /// that would flag every month of a three-year contract and mean nothing. A reading was
        /// keyed or fetched, and no invoice came of it: that is the miss worth chasing.</para>
        ///
        /// <para>Six months back, newest first. Older than that is not a reminder, it is an
        /// audit.</para>
        /// </summary>
        private void RefreshOverdueNotice(int year, int month)
        {
            if (_lblOverdue == null) return;
            _lblOverdue.Visible = false;
            _overdueYear = 0; _overdueMonth = 0;
            if (_dbSetting == null) return;
            try
            {
                int now = year * 12 + month;
                DataTable dt = QueryWithTimeout(
                    "SELECT TOP 1 e.PeriodYear AS y, e.PeriodMonth AS m, COUNT(*) AS n " +
                    "  FROM dbo.zSCP2_MeterEntry e " +
                    "  JOIN dbo.zSCP2_ItemMeter im ON im.ItemMeterKey = e.ItemMeterKey " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = im.ItemKey " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE e.InvoicedDocKey IS NULL AND i.Inactive = 'N' AND c.Inactive = 'N' " +
                    "   AND (e.PeriodYear * 12 + e.PeriodMonth) < " + now +
                    "   AND (e.PeriodYear * 12 + e.PeriodMonth) >= " + (now - 6) +
                    " GROUP BY e.PeriodYear, e.PeriodMonth " +
                    " ORDER BY e.PeriodYear DESC, e.PeriodMonth DESC", 30);
                if (dt == null || dt.Rows.Count == 0) return;
                _overdueYear = Convert.ToInt32(dt.Rows[0]["y"]);
                _overdueMonth = Convert.ToInt32(dt.Rows[0]["m"]);
                int n = Convert.ToInt32(dt.Rows[0]["n"]);
                string name = new CultureInfo("en-US").DateTimeFormat
                    .GetAbbreviatedMonthName(_overdueMonth) + " " + _overdueYear;
                _lblOverdue.Text = "   " + name + " still has " + n.ToString("n0") +
                                   (n == 1 ? " reading" : " readings") +
                                   " nobody billed        click to open that month  ›";
                _lblOverdue.Visible = true;
                _lblOverdue.BringToFront();
            }
            catch { }   // a reminder that cannot be worked out is not worth an error box
        }

        private void LblOverdue_Click(object sender, EventArgs e)
        {
            if (_overdueYear <= 0 || _overdueMonth < 1) return;
            if (_cmbYear != null)
            {
                string y = _overdueYear.ToString();
                if (!_cmbYear.Properties.Items.Contains(y)) _cmbYear.Properties.Items.Add(y);
                _cmbYear.SelectedItem = y;
            }
            this.CmbMonth.SelectedIndex = _overdueMonth - 1;   // fires the reload
            LoadData();
        }

        /// <summary>The strip is laid out again once the window exists: only then are the panel and
        /// the rows measured in the same units. Everything before this is at designer scale.</summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RebuildDayButtons();
        }

        private void BuildDayButtonStrip()
        {
            _pnlDays = new DevExpress.XtraEditors.PanelControl();
            _pnlDays.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            _pnlDays.Location = new Point(56, 54);
            _pnlDays.Size = new Size(432, 30);
            this.GrpFilter.Controls.Add(_pnlDays);
            _pnlDays.BringToFront();

            this.CmbDay.Visible = false;   // value holder only — the buttons drive it
            RebuildDayButtons();
        }

        /// <summary>
        /// The day strip: every billing day, and which of them have work.
        ///
        /// <para>The buttons stay put. A day that is quiet this month goes pale rather than
        /// disappearing -- the fifth button is the fifth button in June and in December, and a hand
        /// that has pressed it forty times should not have to read the row first. Hiding the quiet
        /// ones shortens the strip and moves everything else, which costs more than it saves.</para>
        ///
        /// <para>The counts do not go on the buttons. Fourteen numbers each carrying a second number
        /// is not something anyone reads; the one that matters -- what the day in front is holding --
        /// goes underneath in a sentence.</para>
        /// </summary>
        private void RebuildDayButtons()
        {
            if (_pnlDays == null) return;
            _pnlDays.Controls.Clear();
            int n = this.CmbDay.Properties.Items.Count;
            if (n == 0) return;

            int picked = SelectedDay();

            // How many fit on a line, and how many lines that needs. A book where every contract
            // bills on a different day has THIRTY-ONE of them: they do not fit on one line at any
            // width a person can hit, so the strip wraps and the box grows to hold it.
            //
            // EVERY number here is asked of a real control, never taken from the designer. This
            // screen runs at 150% on the machine it was built on: the panel the designer drew 432
            // wide is 648 on screen and a row the designer drew 26 high is 39. Mixing the two -- a
            // scaled panel measured against unscaled rows -- is what put the second row of days on
            // top of the Month line and pushed Filter and Reset out of the box.
            int rowH = Math.Max(24, this.CmbDay.Height + 4);   // a real row, at whatever scale we are
            int gap = Math.Max(2, rowH / 9);
            int minW = rowH;                                   // square is the smallest a day reads at
            int maxW = rowH + rowH / 2;
            int room = Math.Max(minW, _pnlDays.Width);
            int w = Math.Max(minW, Math.Min(maxW, ((room + gap) / n) - gap));
            int perRow = Math.Max(1, (room + gap) / (w + gap));
            int rows = (n + perRow - 1) / perRow;
            // Spread the last line's slack over all of them rather than leaving one lonely button:
            // 31 days at 13 a line is 13/13/5; at 11 a line it is 11/11/9, which reads as a block.
            if (rows > 1) perRow = (n + rows - 1) / rows;

            for (int i = 0; i < n; i++)
            {
                int day = Convert.ToInt32(this.CmbDay.Properties.Items[i]);
                int waiting;
                if (!_dayCounts.TryGetValue(day, out waiting)) waiting = 0;
                DevExpress.XtraEditors.CheckButton b = new DevExpress.XtraEditors.CheckButton();
                b.Text = day.ToString();
                b.GroupIndex = 77;   // radio behaviour across the strip
                b.Location = new Point((i % perRow) * (w + gap), 2 + (i / perRow) * (rowH + gap));
                b.Size = new Size(w, rowH);
                b.Tag = day;
                b.ToolTip = waiting > 0
                    ? waiting.ToString("n0") + (waiting == 1 ? " row" : " rows") +
                      " waiting on the " + Ordinal(day)
                    : "Nothing waiting on the " + Ordinal(day) + " this month";
                b.Checked = (day == picked);
                PaintDayButton(b, b.Checked, waiting > 0);
                b.CheckedChanged += new EventHandler(DayButton_CheckedChanged);
                _pnlDays.Controls.Add(b);
            }
            // The sentence goes on the line under the last row of buttons, inside the same panel.
            int lineTop = 2 + rows * (rowH + gap);
            int lineH = Math.Max(15, rowH - 8);
            if (_lblDaySummary != null)
            {
                _lblDaySummary.Location = new Point(2, lineTop);
                _lblDaySummary.Size = new Size(Math.Max(60, _pnlDays.Width - 4), lineH);
                _pnlDays.Controls.Add(_lblDaySummary);   // Clear() above dropped it; put it back
            }
            GrowForDayStrip(lineTop + lineH + 2);
            RefreshDaySummary(picked);
        }

        /// <summary>
        /// Makes room for a strip that wrapped, and gives it back when it un-wraps.
        ///
        /// <para>Everything under the strip moves by the same amount, and so do the two boxes around
        /// it. The alternative -- leaving the strip to overflow its panel -- hides the second half of
        /// the month behind the Month row, which is exactly the kind of quiet clipping this screen
        /// has already been caught doing twice.</para>
        /// </summary>
        private void GrowForDayStrip(int wanted)
        {
            if (_pnlDays == null) return;
            // Not until the window exists. Before that every size on the form is still in designer
            // units and the form has not scaled itself yet -- growing the box now and again after
            // scaling moves the same controls twice, by two different amounts.
            if (!this.IsHandleCreated || !this.Visible) return;
            int delta = wanted - _pnlDays.Height;
            if (delta == 0) return;

            int below = _pnlDays.Bottom;
            _pnlDays.Height = wanted;
            foreach (System.Windows.Forms.Control c in this.GrpFilter.Controls)
            {
                if (c == _pnlDays) continue;
                if (c.Top < below) continue;                       // above or beside the strip: stays
                c.Location = new Point(c.Left, c.Top + delta);
            }
            this.GrpFilter.Height += delta;
            this.PanelFilter.Height += delta;                      // docked Top, so the grid follows
        }

        /// <summary>The sentence under the strip: what the chosen day is holding, and what is still
        /// dragging along behind it from the days before.</summary>
        private void RefreshDaySummary(int picked)
        {
            if (_lblDaySummary == null) return;
            int due;
            if (!_dayCounts.TryGetValue(picked, out due)) due = 0;
            int behind = 0;
            foreach (KeyValuePair<int, int> kv in _dayCounts)
                if (kv.Key < picked) behind += kv.Value;

            if (due == 0 && behind == 0)
            {
                _lblDaySummary.Text = "Nothing waiting on the " + Ordinal(picked) + " this month.";
                _lblDaySummary.Appearance.ForeColor = Color.FromArgb(120, 128, 134);
                return;
            }
            string t = Ordinal(picked) + ":  " + due.ToString("n0") +
                       (due == 1 ? " row due" : " rows due");
            if (behind > 0)
                t += "   \u00b7   " + behind.ToString("n0") +
                     (behind == 1 ? " row" : " rows") + " still open from earlier days";
            _lblDaySummary.Text = t;
            _lblDaySummary.Appearance.ForeColor = behind > 0
                ? Color.FromArgb(150, 38, 22) : Color.FromArgb(60, 68, 74);
        }

        private static string Ordinal(int d)
        {
            if (d >= 11 && d <= 13) return d + "th";
            switch (d % 10)
            {
                case 1: return d + "st";
                case 2: return d + "nd";
                case 3: return d + "rd";
                default: return d + "th";
            }
        }

        /// <summary>
        /// How many rows are still waiting on each day, in the month on screen.
        ///
        /// <para>Waiting = the row exists and no invoice has claimed it for this period -- the same
        /// rule the list itself uses, asked one level up. A button that says 12 opens twelve
        /// rows.</para>
        /// </summary>
        private void LoadDayCounts(int year, int month)
        {
            _dayCounts = new Dictionary<int, int>();
            if (_dbSetting == null) return;
            try
            {
                bool inclInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);
                DataTable dt = QueryWithTimeout(
                    "SELECT " + ScpBillingRows.RowDueDaySql + " AS d, COUNT(*) AS n " +
                    "  FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE " + (inclInactive ? "1=1 " : "i.Inactive='N' AND c.Inactive='N' ") +
                    "   AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry me " +
                    "                    WHERE me.ItemMeterKey = m.ItemMeterKey " +
                    "                      AND me.PeriodYear = " + year +
                    "                      AND me.PeriodMonth = " + month +
                    "                      AND me.InvoicedDocKey IS NOT NULL) " +
                    " GROUP BY " + ScpBillingRows.RowDueDaySql, 30);
                if (dt == null) return;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["d"] == DBNull.Value) continue;
                    int d = Convert.ToInt32(r["d"]);
                    if (d >= 1 && d <= 31) _dayCounts[d] = Convert.ToInt32(r["n"]);
                }
            }
            catch { }   // no counts is a plain strip, not an error
        }

        private void DayButton_CheckedChanged(object sender, EventArgs e)
        {
            DevExpress.XtraEditors.CheckButton b = sender as DevExpress.XtraEditors.CheckButton;
            if (b == null) return;
            // Repaint the WHOLE strip from actual Checked state — only the picked day stays blue,
            // every other button reverts to the plain skin look.
            foreach (System.Windows.Forms.Control c in _pnlDays.Controls)
            {
                DevExpress.XtraEditors.CheckButton db = c as DevExpress.XtraEditors.CheckButton;
                if (db == null) continue;
                int dw;
                if (!_dayCounts.TryGetValue(Convert.ToInt32(db.Tag), out dw)) dw = 0;
                PaintDayButton(db, db.Checked, dw > 0);
            }
            if (!b.Checked) return;
            int day = Convert.ToInt32(b.Tag);
            if (!object.Equals(this.CmbDay.SelectedItem, day))
                this.CmbDay.SelectedItem = day;   // fires CmbDay_SelectedIndexChanged -> reload
        }

        private static void PaintDayButton(DevExpress.XtraEditors.CheckButton b, bool on)
        {
            PaintDayButton(b, on, true);
        }

        /// <summary>Blue when it is the day in front, pale when the month has nothing on it, plain
        /// otherwise. Pale is the whole point of keeping the quiet days: they are visibly quiet.</summary>
        private static void PaintDayButton(DevExpress.XtraEditors.CheckButton b, bool on, bool hasWork)
        {
            if (on)
            {
                b.Appearance.BackColor = Color.FromArgb(33, 115, 199);   // selected = blue
                b.Appearance.ForeColor = Color.White;
                b.Appearance.Options.UseBackColor = true;
                b.Appearance.Options.UseForeColor = true;
                b.Appearance.FontStyleDelta = FontStyle.Bold;
            }
            else if (!hasWork)
            {
                b.Appearance.BackColor = Color.Empty;
                b.Appearance.ForeColor = Color.FromArgb(168, 176, 182);   // quiet day: still there, visibly empty
                b.Appearance.Options.UseBackColor = false;
                b.Appearance.Options.UseForeColor = true;
                b.Appearance.FontStyleDelta = FontStyle.Regular;
            }
            else
            {
                b.Appearance.BackColor = Color.Empty;
                b.Appearance.ForeColor = Color.Empty;
                b.Appearance.Options.UseBackColor = false;
                b.Appearance.Options.UseForeColor = false;
                b.Appearance.FontStyleDelta = FontStyle.Bold;             // has work
            }
        }

        // ───────────────────── Load (one row per meter) ─────────────────────

        // An invoice DELETED (or cancelled) in AutoCount must release its meters for re-billing:
        // remove the billed readings it wrote (they would freeze Last Reading at the billed value)
        // and clear the billing-period guard stamps, so Generate no longer says "already invoiced".
        // Scope safety: only rows whose DocKey came from OUR generator (stamped in zSCP2_MeterEntry)
        // are touched — the 145k migrated legacy readings carry no DocKey and are never affected.
        private void ReconcileDeletedInvoices()
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.ReconcileDeletedInvoices(_dbSetting);
        }

        private void LoadData()
        {
            if (_dbSetting == null) return;
            ReconcileDeletedInvoices();
            try
            {
                int month = SelectedMonth();
                int year = SelectedYear();
                int target = SelectedDay();
                int monthEnd = DateTime.DaysInMonth(year, month);
                if (target > monthEnd) target = monthEnd;
                // Cutoff for the RED Last Audit Date flag: audited after this date + still not
                // invoiced = the invoice wasn't created on time.
                _periodCutoff = new DateTime(year, month, target);

                // Billing-day filter — with one carve-out: a machine INVOICED in the selected month
                // always stays in the dataset regardless of its billing day, so the Invoiced tab is
                // a complete month history.
                // The row's OWN day, not the contract's: a rental billing apart follows Rental
                // invoice day, its copies follow the billing due day. The rule is in ScpBillingRows
                // so the filter and the row cannot drift apart.
                string due = "(CASE WHEN " + ScpBillingRows.RowDueDaySql + " > " + monthEnd +
                             " THEN " + monthEnd + " ELSE " + ScpBillingRows.RowDueDaySql + " END)";
                // "<=", not "=": a row whose day has GONE and which still has not been billed stays
                // on the list -- in red -- instead of quietly disappearing until someone thinks to
                // look for it. Money nobody invoiced should not need finding.
                //
                // Meter Reading > Setting turns it off for anyone who wants the old behaviour: the
                // chosen day, and nothing else on it.
                bool keepOverdue = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_SHOW_OVERDUE_ROWS,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_SHOW_OVERDUE_ROWS);
                string dayFilter = this.ChkShowAll.Checked ? "" :
                    " AND (" + due + (keepOverdue ? " <= " : " = ") + target +
                    " OR EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry meD WHERE meD.ItemMeterKey = m.ItemMeterKey" +
                    " AND meD.PeriodYear = " + year + " AND meD.PeriodMonth = " + month +
                    " AND meD.InvoicedDocKey IS NOT NULL)) ";

                string search = (this.TxtSearch.EditValue ?? "").ToString().Trim();
                string searchFilter = "";
                if (search.Length > 0)
                {
                    string s = search.Replace("'", "''");
                    searchFilter = " AND (i.ServiceItemNo LIKE '%" + s + "%' OR i.SerialNumber LIKE '%" + s +
                        "%' OR m.MachineSerialNo LIKE '%" + s +
                        "%' OR c.ContractNo LIKE '%" + s + "%' OR ISNULL(d.CompanyName,'') LIKE '%" + s + "%') ";
                }

                // Expired service items are hidden unless the user opts in (Meter Reading > Setting).
                bool includeExpired = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
                // Setting "Include INACTIVE contracts / items": normally deactivated rows are hidden
                // (they stopped billing); on demand they show again so leftover un-invoiced readings
                // can be reviewed — and consciously billed — before the contract is retired for good.
                bool includeInactive = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_INACTIVE,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_INACTIVE);
                string inactiveFilter = includeInactive ? "1=1" : "i.Inactive='N' AND c.Inactive='N'";
                // A bound item stores its expiry as an OVERRIDE only (inherited dates are NULL on the
                // item row), so the effective expiry is COALESCE(item, contract).
                string expiryFilter = includeExpired ? "" :
                    " AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                    "OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(" + year + "," + month + ",1)) ";

                // Show the resolved billing period so cross-year runs are unambiguous (e.g. Dec = last Dec).
                this.GrpFilter.Text = "Filter Options   —   billing period: " +
                    new CultureInfo("en-US").DateTimeFormat.GetAbbreviatedMonthName(month) + " " + year;

                // The rows to bill come from ScpBillingRows, which inter-billing also uses. Only
                // the four filter clauses above are this screen's -- everything after the WHERE
                // is shared, so both screens see the same row for the same counter.
                _dtGrid = ServiceContractPhotocopier.Classes.ScpBillingRows.Load(
                    _dbSetting, year, month, inactiveFilter, dayFilter, searchFilter, expiryFilter,
                    out _ladders);

                PrefillFromStaging(SelectedMonth(), SelectedYear());
                MarkLate(target);
                ApplyRentalGroupPrices();
                ApplyMeterLinePrices();   // the contract's own price for a merged rental line, if set
                AutoFillFlatMeters();
                RecomputeNeedManual();

                // Shape EVERY tab's grid, not just the one in front — each needs its columns built
                // before it can be bound, and a tab the operator has not visited yet must already be
                // right when they get there.
                for (int i = 0; i < _pages.Count; i++)
                {
                    GridView v;
                    if (!_viewByPage.TryGetValue(_pages[i], out v)) continue;
                    ForView(v, delegate
                    {
                        ActiveGrid.DataSource = _dtGrid;   // columns come from the table
                        ConfigureGrid();                   // shapes once, incl. this tab's factory columns
                    });
                }
                WireNativeGridLayout();   // #1a: AutoCount-native layout restore + header menu, per tab
                UpdateTabCounts();
                ApplyTabFilter();
                RefreshOverdueNotice(year, month);
                LoadDayCounts(year, month);
                RebuildDayButtons();   // the strip follows the month it is showing
                ApplySelectColumnVisibility();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── #1a: grid layout persistence via AutoCount's NATIVE CustomizeGridLayout ──
        // Layouts live in dbo.Layout / dbo.LayoutUsers exactly like AutoCount's own screens:
        // right-click a column header -> Save Layout / Load Layout / Reset Layout / Layout Manager
        // (named templates, set-as-default, assign to users). The ctor auto-restores the user's
        // assigned layout (else the all-user default). On close we ALSO silently save a per-user
        // layout ("SCP Meter - <user>") and assign it, so the clerk never has to re-set anything.

        private const string GRID_LAYOUT_KEY = "SCP_METER_READING";   // dbo.Layout.FormName
        private readonly System.Collections.Generic.Dictionary<XtraTabPage, AutoCount.XtraUtils.CustomizeGridLayout>
            _layoutByPage = new System.Collections.Generic.Dictionary<XtraTabPage, AutoCount.XtraUtils.CustomizeGridLayout>();

        /// <summary>
        /// One CustomizeGridLayout per tab, each under its OWN form name. That is what makes the
        /// right-click menu offer the layouts saved for THIS tab: Save Layout on Invoiced lands in
        /// Invoiced's list and does not turn up on Ready to Invoice, where loading it made no sense.
        /// Each instance also restores that tab's assigned layout on open.
        /// </summary>
        private void WireNativeGridLayout()
        {
            if (_layoutByPage.Count > 0) return;
            try
            {
                AutoCount.Authentication.UserSession us = AutoCount.Authentication.UserSession.CurrentUserSession;
                if (us == null) return;
                for (int i = 0; i < _pages.Count; i++)
                {
                    XtraTabPage page = _pages[i];
                    GridView v;
                    if (!_viewByPage.TryGetValue(page, out v)) continue;
                    // Ctor restores this tab's layout from dbo.Layout (per-user assignment wins over
                    // default) and injects the layout menu into its column-header right-click popup.
                    AutoCount.XtraUtils.CustomizeGridLayout gl =
                        new AutoCount.XtraUtils.CustomizeGridLayout(us, LayoutKey(page), v);
                    // Our grid, our rules: every user gets the layout/column/export menu here (the
                    // native SYS_BHV_* rights default to admin-ish groups only).
                    gl.GetAccessRightSetting +=
                        new AutoCount.XtraUtils.GetCustomizeGridLayoutAccessRightSettingEventHandler(delegate
                        {
                            AutoCount.XtraUtils.CustomizeGridLayoutAccessRightSetting s =
                                new AutoCount.XtraUtils.CustomizeGridLayoutAccessRightSetting();
                            s.AllowCustomizeGridLayout = true;
                            s.AllowColumnChooser = true;
                            s.AllowColumnCaption = true;
                            s.AllowExportGridContent = true;
                            s.AllowPrintGridContent = true;
                            return s;
                        });
                    _layoutByPage[page] = gl;
                }
            }
            catch { }   // layout plumbing must never break the screen
        }

        private string LayoutKey(XtraTabPage page)
        {
            string key = TabKey(page);
            return key.Length == 0 ? GRID_LAYOUT_KEY : GRID_LAYOUT_KEY + "." + key;
        }

        /// <summary>
        /// Silent per-user auto-save on close, now once per TAB: each tab's arrangement is saved
        /// under that tab's own layout scope and assigned to this user, so every tab comes back the
        /// way it was left. One shared title across the tabs would have them overwriting each other.
        /// </summary>
        private void SaveGridLayoutForCurrentUser()
        {
            try
            {
                if (_dbSetting == null || _layoutByPage.Count == 0) return;
                string user = "";
                try { user = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? ""; } catch { }
                if (user.Length == 0) return;

                foreach (System.Collections.Generic.KeyValuePair<XtraTabPage, AutoCount.XtraUtils.CustomizeGridLayout> kv
                         in _layoutByPage)
                {
                    string tab = TabKey(kv.Key);
                    string title = "SCP Meter - " + user + (tab.Length > 0 ? " - " + tab : "");
                    if (title.Length > 60) title = title.Substring(0, 60);
                    try
                    {
                        if (!kv.Value.SaveLayout(title, false)) continue;   // title owned elsewhere — skip
                        string tEsc = title.Replace("'", "''"), uEsc = user.Replace("'", "''");
                        _dbSetting.ExecuteNonQuery(
                            "IF NOT EXISTS (SELECT 1 FROM dbo.LayoutUsers WHERE Title = N'" + tEsc + "' AND UserID = N'" + uEsc + "') " +
                            "INSERT INTO dbo.LayoutUsers (Title, UserID) VALUES (N'" + tEsc + "', N'" + uEsc + "')");
                    }
                    catch { }   // one tab failing to save must not cost the others theirs
                }
            }
            catch { }
        }

        // Demo 28/07 #1b: per-day billing workload of the selected month. Counts every active,
        // unexpired machine by its EFFECTIVE billing day (item override, else contract day) and
        // how many already carry an invoice stamp for the period — "who is left to bill" at a glance.
        // Hover: compute the overview (cached per billing period for 30s) and hand it to the
        // button's tooltip — MouseEnter fires before the tooltip's show delay, so it is in time.
        private string _ovCacheKey;
        private DateTime _ovCacheAt;
        private string _ovCacheText = "";

        private void BtnMonthOverview_MouseEnter(object sender, EventArgs e)
        {
            if (_dbSetting == null || _btnMonthOverview == null) return;
            try
            {
                string key = SelectedYear() + "-" + SelectedMonth();
                if (key != _ovCacheKey || (DateTime.Now - _ovCacheAt).TotalSeconds > 30)
                {
                    string monthName;
                    _ovCacheText = BuildMonthOverviewText(out monthName);
                    _btnMonthOverview.ToolTipTitle = "Month Overview  -  " + monthName;
                    _ovCacheKey = key;
                    _ovCacheAt = DateTime.Now;
                }
                _btnMonthOverview.ToolTip = _ovCacheText;
            }
            catch { }
        }

        private void BtnMonthOverview_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            try
            {
                string monthName;
                string text = BuildMonthOverviewText(out monthName);
                XtraMessageBox.Show(text, "Month Overview  -  " + monthName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exOv)
            { XtraMessageBox.Show("Overview failed:\r\n" + exOv.Message, "Month Overview"); }
        }

        private string BuildMonthOverviewText(out string monthName)
        {
            int year = SelectedYear(), month = SelectedMonth();
            monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(month) + " " + year;
            {
                string sql =
                    "SELECT COALESCE(i.BillingDayOverride, c.BillingDay) AS EffDay, " +
                    "COUNT(DISTINCT i.ItemKey) AS Machines, COUNT(DISTINCT inv.ItemKey) AS Invoiced " +
                    "FROM dbo.zSCP2_Item i " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN (SELECT DISTINCT m2.ItemKey FROM dbo.zSCP2_ItemMeter m2 " +
                    "  JOIN dbo.zSCP2_MeterEntry me ON me.ItemMeterKey = m2.ItemMeterKey " +
                    "  WHERE me.PeriodYear=" + year + " AND me.PeriodMonth=" + month +
                    "    AND me.InvoicedDocKey IS NOT NULL) inv ON inv.ItemKey = i.ItemKey " +
                    "WHERE i.Inactive='N' AND c.Inactive='N' " +
                    "AND EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter mm WHERE mm.ItemKey = i.ItemKey) " +
                    "AND (COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) IS NULL " +
                    "  OR COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) >= DATEFROMPARTS(" + year + "," + month + ",1)) " +
                    "GROUP BY COALESCE(i.BillingDayOverride, c.BillingDay) " +
                    "ORDER BY 1 OPTION (MAXDOP 1)";
                DataTable dt = QueryWithTimeout(sql, 60);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("Billing overview  -  " + monthName);
                sb.AppendLine("");
                int totM = 0, totI = 0;
                foreach (DataRow r in dt.Rows)
                {
                    string dayTxt = r["EffDay"] == DBNull.Value ? "(no day)" :
                        ("Day " + Convert.ToInt32(r["EffDay"]).ToString().PadLeft(2));
                    int mCnt = Convert.ToInt32(r["Machines"]);
                    int iCnt = Convert.ToInt32(r["Invoiced"]);
                    totM += mCnt; totI += iCnt;
                    sb.AppendLine(dayTxt + "  :  " + mCnt.ToString().PadLeft(4) + " machine(s)   -   " +
                                  iCnt + " invoiced, " + (mCnt - iCnt) + " to go");
                }
                if (dt.Rows.Count == 0) sb.AppendLine("No active machines found.");
                sb.AppendLine("");
                sb.AppendLine("TOTAL   :  " + totM + " machine(s)   -   " + totI + " invoiced, " +
                              (totM - totI) + " to go");
                return sb.ToString();
            }
        }

        private static DataTable NewGridTable()
        {
            // The shape lives with the rows -- see ScpBillingRows.
            return ServiceContractPhotocopier.Classes.ScpBillingRows.NewTable();
        }

        // Columns the user may never see (data drivers / plumbing) — never offered in View Setting.
        private static readonly string[] _systemHiddenCols = new string[] { "ItemKey", "ContractKey", "DebtorCode", "BillingMode", "ItemMeterKey", "ACItemCode", "Shade", "Sel", "InvoicedDocNo", "ItemDesc", "NeedManual", "Locked", "IsFlat", "IsWaive", "IsGroupItem", "MachineMode", "TrackingId", "WaiveFirstNMonths", "WaiveTargetAmount", "WaivePartialThreshold", "WaivePartialAmount", "WaiveScope", "StrategyCode", "RentSep", "PeriodByContract", "ContractStart", "RentalBillingDay", "RentalStartDate", "RentalMonths", "RentalBasis", "IsExpired", "NotStarted", "EffStart", "Late" };

        // Hidden by DEFAULT but user-selectable (column chooser / View Setting).
        private static readonly string[] _optionalCols = new string[] { "Mode", "BillingDay", "UseMin", "MultiPriceCode", "FOCResetUnit", "FOCResetN", "Status", "EntrySource", "FetchedReading", "HasConflict", "BillGroupCode", "Role", "DueDay" };

        // Everything View Setting lets the operator toggle — now including the reading and pricing
        // columns. They used to be excluded because ApplyTabColumnLayout re-forced them on every tab
        // switch, so choosing them did nothing; it now runs once, when a tab's grid is first built,
        // and the arrangement is the operator's from then on.
        //
        // Two are deliberately NOT here: Select (hiding it would leave no way to pick a row for
        // Generate) and Customer (the group header the rows hang from).
        private static readonly string[] _viewSettingCols = new string[] {
            "ContractNo", "ServiceItemNo", "SerialNo", "Mode", "BillingDay", "MeterType", "MeterTypeName",
            "MachineStatus", "MinCharges", "UnitPrice", "FOCQty", "RebatePct",
            "LastReadDate", "LastAuditDate", "LastFetchDate", "LastReading",
            "CurrentReading", "MeterUsage", "TotalCharges",
            "LastInvNo", "LastInvDate", "InvTotal", "UseMin", "Status", "EntrySource", "FetchedReading",
            "HasConflict", "BillGroupCode", "MultiPriceCode", "FOCResetUnit", "FOCResetN", "Role" };

        // ConfigureGrid is pure column/view SHAPING (captions, widths, default visibility, appearance,
        // summaries) — the DataTable schema is identical on every load, so re-running it only undoes
        // the user's own layout: it would wipe the restored native layout and any View Setting choice
        // on every Refresh/Fetch. Shape once, then leave the grid alone.

        private readonly System.Collections.Generic.List<GridView> _shaped =
            new System.Collections.Generic.List<GridView>();

        private void ConfigureGrid()
        {
            // Once per GRID. It used to be once per form, which was the same thing when there was
            // one grid and is emphatically not now.
            //
            // ApplyTabColumnLayout belongs HERE, inside the once-only path. Calling it on every load
            // put the eleven key-in columns back on every Refresh, Filter and Day click — which is
            // exactly how View Setting looked broken: the operator hid a column, touched the filter,
            // and it returned.
            if (_shaped.Contains(ActiveView)) return;
            _shaped.Add(ActiveView);
            ActiveView.OptionsBehavior.Editable = true;
            // Open the in-place editor on MOUSE DOWN: without this, the first click on a (merged)
            // Select cell only focuses it and the checkbox never toggles on a single click.
            ActiveView.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            // Merged-cell look (ONE checkbox / contract / CSSI / serial / status cell spanning the
            // machine's BK+CL rows) UNDER collapsible "Customer: ..." group rows.
            ActiveView.OptionsView.AllowCellMerge = true;
            ActiveView.OptionsView.ShowGroupPanel = false;
            ActiveView.OptionsBehavior.AutoExpandAllGroups = true;
            GridColumn cGrpCust = ActiveView.Columns["Customer"];
            if (cGrpCust != null) cGrpCust.GroupIndex = 0;
            ActiveView.OptionsView.ShowFooter = true;       // footer band for totals
            ActiveView.OptionsView.ColumnAutoWidth = false;

            // "Sel" (per-meter) stays as the hidden data driver; the user only sees the merged
            // per-CSSI "Select" checkbox (SelCssi), which drives both meter rows.
            foreach (string h in _systemHiddenCols)
                if (ActiveView.Columns[h] != null)
                {
                    ActiveView.Columns[h].Visible = false;
                    ActiveView.Columns[h].OptionsColumn.ShowInCustomizationForm = false;
                }
            // Locked rows: the Current Reading cell refuses to open its editor (snapshot is frozen).
            ActiveView.ShowingEditor -= new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditorLock);
            ActiveView.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditorLock);

            // Advanced filtering: per-column auto-filter row under the headers (+ the header funnel
            // menus). User filters layer on top of the tab's own DataView — they can never break the
            // tab's system scope.
            ActiveView.OptionsView.ShowAutoFilterRow = true;

            // Secondary columns hidden BY DEFAULT to keep the grid focused — still available through
            // the column chooser (right-click the header -> Column Chooser).
            // MachineStatus is VISIBLE now (green=ONLINE / orange=OFFLINE cell tint) — it replaced the
            // old Online/Offline tabs.
            foreach (string h in _optionalCols)
            {
                GridColumn hc = ActiveView.Columns[h];
                if (hc == null) continue;
                hc.Visible = false;
                hc.OptionsColumn.ShowInCustomizationForm = true;
            }
            GridColumn cBillGrp = ActiveView.Columns["BillGroupCode"];
            if (cBillGrp != null)
            {
                cBillGrp.Caption = "Bill Group"; cBillGrp.Width = 70;
                // Display-only here: the code is maintained on the CONTRACT (item grid); an edit in
                // this grid would regroup one run without persisting — silently reverting next load.
                cBillGrp.OptionsColumn.AllowEdit = false;
                cBillGrp.OptionsColumn.ReadOnly = true;
            }

            SetCol("SelCssi", "Select", 55, true);
            GridColumn selc = ActiveView.Columns["SelCssi"];
            if (selc != null)
            {
                if (!ShowSelectColumn) selc.Visible = false;   // hosted in the Invoice Run: no ticking here
                // Sorting by the checkbox makes ticked rows jump position mid-click — disable it.
                selc.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
                selc.SortOrder = DevExpress.Data.ColumnSortOrder.None;
                // Commit the toggle on the click itself (no waiting for focus to leave the cell).
                if (_selCssiEditor == null)
                {
                    _selCssiEditor = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
                    _selCssiEditor.EditValueChanged += new EventHandler(SelCssiEditor_EditValueChanged);
                    ActiveGrid.RepositoryItems.Add(_selCssiEditor);
                }
                selc.ColumnEdit = _selCssiEditor;
            }
            SetCol("ContractNo", "Contract", 110, false);
            SetCol("ServiceItemNo", "Service Item No", 120, false);
            SetCol("SerialNo", "Serial", 110, false);
            SetCol("MachineStatus", "Machine Status", 95, false);
            SetCol("Customer", "Customer", 220, false);
            SetCol("Role", "Role", 55, false);          // BK / CL / RENTAL / WAIVE / COMMIT / NA
            SetCol("Mode", "Mode", 70, false);
            SetCol("BillingDay", "Billing Day", 80, false);
            SetCol("MeterType", "Meter Type", 130, false);
            SetCol("MeterTypeName", "Meter Type Name", 170, false);
            SetNum("MinCharges", "Min. Charges", 85, false, "n2");
            SetNum("UnitPrice", "Unit Price", 85, false, "n4");
            SetNum("FOCQty", "FOC Qty", 70, false, "n0");
            SetNum("RebatePct", "Rebate (%)", 80, false, "n2");
            SetCol("LastReadDate", "Last Read Date", 100, false);
            // Editable for a KEYED reading only -- the day the counter was actually read
            // (feedback ATP-5). Every row that may not be touched is vetoed in ShowingEditor.
            SetCol("LastAuditDate", "Last Audit Date", 110, true);
            SetCol("LastFetchDate", "Last Fetch Date", 115, false);
            GridColumn cFd = ActiveView.Columns["LastFetchDate"];
            if (cFd != null)
            {
                cFd.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                cFd.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm";
            }
            SetNum("LastReading", "Last Reading", 95, false, "n0");
            SetNum("CurrentReading", "Current Reading", 100, true, "n0");
            SetNum("MeterUsage", "Meter Usage", 95, false, "n0");
            SetNum("TotalCharges", "Total Charges", 95, false, "n2");
            SetCol("LastInvNo", "Last Invoice No", 110, false);
            SetCol("LastInvDate", "Last Invoice Date", 105, false);
            GridColumn cInvDt = ActiveView.Columns["LastInvDate"];
            if (cInvDt != null)
            {
                cInvDt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                cInvDt.DisplayFormat.FormatString = "dd/MM/yyyy";
            }
            SetNum("InvTotal", "Invoice Total", 95, false, "n2");
            GridColumn cInvTot = ActiveView.Columns["InvTotal"];
            if (cInvTot != null)
            {
                cInvTot.Visible = false;   // Invoiced tab only (ApplyTabColumnLayout shows it)
                cInvTot.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(223, 240, 216);
                cInvTot.AppearanceCell.Options.UseBackColor = true;
                cInvTot.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                cInvTot.SummaryItem.DisplayFormat = "Σ {0:n2}";
            }
            SetCol("UseMin", "Use Min.", 70, true);
            SetCol("Status", "Status", 130, false);
            // Invoiced-this-period rows: paint the Last Invoice cells green so a 0 Current Reading is
            // unmistakably "already billed" rather than "no reading yet".
            ActiveView.RowCellStyle -= new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewMeter_LastInvCellStyle);
            ActiveView.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewMeter_LastInvCellStyle);

            // Highlight the two columns the operator actually works with: Current Reading (amber —
            // the key-in column) and Total Charges (green — the money). Column-level cell appearance
            // outranks the RowStyle shading, so the tint shows on every row.
            GridColumn cCurHl = ActiveView.Columns["CurrentReading"];
            if (cCurHl != null)
            {
                cCurHl.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(255, 249, 196);
                cCurHl.AppearanceCell.Options.UseBackColor = true;
                // The HEADER is tinted too, a shade stronger than the cells. A coloured column whose
                // header is grey like every other reads as an accident; carrying the colour up to the
                // title says "this column is the one you type in" from across the room.
                cCurHl.AppearanceHeader.BackColor = System.Drawing.Color.FromArgb(255, 236, 150);
                cCurHl.AppearanceHeader.ForeColor = System.Drawing.Color.FromArgb(102, 60, 0);
                cCurHl.AppearanceHeader.Options.UseBackColor = true;
                cCurHl.AppearanceHeader.Options.UseForeColor = true;
                cCurHl.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
                cCurHl.AppearanceHeader.Options.UseFont = true;
            }
            GridColumn cChgHl = ActiveView.Columns["TotalCharges"];
            if (cChgHl != null)
            {
                cChgHl.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(223, 240, 216);
                cChgHl.AppearanceCell.Options.UseBackColor = true;
                cChgHl.AppearanceHeader.BackColor = System.Drawing.Color.FromArgb(198, 230, 190);
                cChgHl.AppearanceHeader.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
                cChgHl.AppearanceHeader.Options.UseBackColor = true;
                cChgHl.AppearanceHeader.Options.UseForeColor = true;
                cChgHl.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
                cChgHl.AppearanceHeader.Options.UseFont = true;
            }

            // Cell merging is for the IDENTITY columns only (one checkbox / contract / CSSI / serial
            // spanning a machine's BK+CL rows). Every column carrying per-meter ARITHMETIC must keep
            // its own cell on its own row: when two adjacent meters happen to share a value the
            // merged cell reads as one figure covering both meters, and anyone reconciling
            // NET = Usage - FOC off the screen is then reading the wrong line.
            foreach (string nm in new string[] { "MeterType", "MeterTypeName", "MinCharges", "UnitPrice",
                "FOCQty", "RebatePct", "LastReadDate", "LastAuditDate", "LastFetchDate", "LastReading",
                "CurrentReading", "MeterUsage", "TotalCharges", "FetchedReading", "EntrySource",
                "UseMin", "MultiPriceCode", "Role" })
                if (ActiveView.Columns[nm] != null)
                    ActiveView.Columns[nm].OptionsColumn.AllowMerge = DevExpress.Utils.DefaultBoolean.False;

            // Freeze the identifier columns on the left so they stay visible when scrolling horizontally.
            // (Requires ColumnAutoWidth = false, set above.)
            foreach (string fx in new string[] { "SelCssi", "ContractNo", "ServiceItemNo", "SerialNo" })
                if (ActiveView.Columns[fx] != null)
                    ActiveView.Columns[fx].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;

            // Footer totals.
            GridColumn cChg = ActiveView.Columns["TotalCharges"];
            if (cChg != null)
            {
                cChg.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                cChg.SummaryItem.DisplayFormat = "Σ {0:n2}";
            }
            GridColumn cUse = ActiveView.Columns["MeterUsage"];
            if (cUse != null)
            {
                cUse.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                cUse.SummaryItem.DisplayFormat = "Σ {0:n0}";
            }
            GridColumn cSt = ActiveView.Columns["Status"];
            if (cSt != null)
            {
                cSt.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Count;
                cSt.SummaryItem.DisplayFormat = "{0} meters";
            }
        
            // Last, so it lands on top of the columns just shaped: this tab's factory
            // visibility (the Invoiced tab reads as invoice history, the others as key-in
            // sheets). From here the arrangement belongs to the operator.
            ApplyTabColumnLayout();
        }

        private void SetCol(string field, string caption, int width, bool editable)
        {
            GridColumn c = ActiveView.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width;
            c.OptionsColumn.AllowEdit = editable; c.OptionsColumn.ReadOnly = !editable;
        }

        private DateTime? _periodCutoff;   // selected billing day of the selected month (clamped)


        // Auto-fetch snapshot rows are FROZEN — no manual override of the Current Reading.
        private void GridViewMeter_ShowingEditorLock(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (V(sender).FocusedColumn == null || V(sender).FocusedColumn.FieldName != "CurrentReading") return;
            DataRow r = V(sender).GetDataRow(V(sender).FocusedRowHandle);
            if (r == null) return;
            if (r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"])) e.Cancel = true;
        }

        // Cell tints: GREEN bold Last Invoice No/Date when that invoice falls inside the SELECTED
        // billing period (= already billed now); Machine Status green=ONLINE / orange=OFFLINE
        // (replaces the removed Online/Offline tabs); RED Last Audit Date when the reading was
        // audited AFTER the billing day and the invoice still hasn't been created (billed late).
        private void GridViewMeter_LastInvCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null) return;
            // Current Reading traffic light (#2c): the operator must be able to tell "I have to key
            // this in" from "this row is not mine to fill" at a glance.
            //   GREY  = no key-in needed (rental / waive / commit / NA row, locked snapshot, or the
            //           period is already invoiced) — the editor also refuses to open (IsInlineEditable)
            //   AMBER bold = MUST key in, still empty
            //   pale amber (column default) = keyed in already
            if (e.Column.FieldName == "CurrentReading")
            {
                DataRow rc = V(sender).GetDataRow(e.RowHandle);
                if (rc == null) return;
                if (!IsInlineEditable(rc))
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(235, 235, 235);
                    e.Appearance.ForeColor = System.Drawing.Color.DimGray;
                    return;
                }
                decimal curv = rc["CurrentReading"] == DBNull.Value ? 0m : Convert.ToDecimal(rc["CurrentReading"]);
                if (curv <= 0m)
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 213, 79);
                    e.Appearance.ForeColor = System.Drawing.Color.FromArgb(102, 60, 0);
                    e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
                }
                return;   // filled-in rows keep the column's own pale amber
            }
            if (e.Column.FieldName == "LastAuditDate")
            {
                object av = V(sender).GetRowCellValue(e.RowHandle, "LastAuditDate");
                if (av == null || av == DBNull.Value || !_periodCutoff.HasValue) return;
                object invd = V(sender).GetRowCellValue(e.RowHandle, "InvoicedDocNo");
                bool billed = invd != null && invd != DBNull.Value && invd.ToString().Trim().Length > 0;
                if (!billed && Convert.ToDateTime(av).Date > _periodCutoff.Value.Date)
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 224, 224);
                    e.Appearance.ForeColor = System.Drawing.Color.FromArgb(198, 40, 40);
                    e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
                }
                return;
            }
            if (e.Column.FieldName == "MachineStatus")
            {
                object msv = V(sender).GetRowCellValue(e.RowHandle, "MachineStatus");
                string ms = msv == null || msv == DBNull.Value ? "" : msv.ToString();
                if (ms == "ONLINE")
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 230, 201);
                    e.Appearance.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
                    e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
                }
                else if (ms == "OFFLINE")
                {
                    e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 224, 178);
                    e.Appearance.ForeColor = System.Drawing.Color.FromArgb(191, 87, 0);
                    e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
                }
                return;
            }
            if (e.Column.FieldName != "LastInvNo" && e.Column.FieldName != "LastInvDate") return;
            object inv = V(sender).GetRowCellValue(e.RowHandle, "InvoicedDocNo");
            if (inv == null || inv == DBNull.Value || inv.ToString().Trim().Length == 0) return;
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 230, 201);
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
            e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
        }
        private void SetNum(string field, string caption, int width, bool editable, string fmt)
        {
            SetCol(field, caption, width, editable);
            GridColumn c = ActiveView.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c.DisplayFormat.FormatString = fmt;
        }

        // ───────────────────── Buttons ─────────────────────

        private void RefreshTimer_Tick(object sender, EventArgs e) { }
        private void BtnRefresh_Click(object sender, EventArgs e) { LoadData(); }
        private void BtnFilter_Click(object sender, EventArgs e) { LoadData(); }

        // Picking a specific billing day means "show only that day" — uncheck Show All (which would
        // otherwise override the day filter) and reload. Programmatic changes are guarded.
        /// <summary>Which side of the rental split a row is on: "R" when it will ride the
        /// rental-only invoice, "" when it goes with the copies. A WAIVE follows the rental, because
        /// it is the credit against it.</summary>
        private static string RentalSideOf(DataRow r)
        {
            if (!r.Table.Columns.Contains("RentSep")) return "";
            bool sep = r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]);
            if (!sep) return "";
            bool flat = r.Table.Columns.Contains("IsFlat")
                        && r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            if (!flat) return "";
            string role = S(r["Role"]).Trim().ToUpperInvariant();
            if (role == "RENTAL" || role == "WAIVE") return "R";
            return ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(S(r["MeterType"]))
                ? "R" : "";
        }

        /// <summary>The invoice a row belongs to: contract, bill group, rental side. Mirrors the key
        /// ScpInvoiceJobs builds, because a guard that groups differently from the generator will
        /// eventually block a run the generator would have handled perfectly.</summary>
        private static string JobKeyOf(DataRow r)
        {
            string bg = r.Table.Columns.Contains("BillGroupCode")
                ? ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]))
                : "";
            bool groupItem = r.Table.Columns.Contains("IsGroupItem")
                             && r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
            if (groupItem) bg = "";
            // One invoice per machine: the machine is the invoice, whatever its bill group.
            if (r.Table.Columns.Contains("BillingMode") && S(r["BillingMode"]).Trim().ToUpperInvariant() == "S")
                return D64(r["ContractKey"]) + "|I" + D64(r["ItemKey"]) + "|" + RentalSideOf(r);
            return D64(r["ContractKey"]) + "|" + bg + "|" + RentalSideOf(r);
        }

        private void CmbDay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvent || _dbSetting == null) return;
            _suppressFilterEvent = true;
            this.ChkShowAll.Checked = false;
            _suppressFilterEvent = false;
            LoadData();
        }

        private void ChkShowAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressFilterEvent || _dbSetting == null) return;
            LoadData();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            // #1a escape hatch: Ctrl+Reset deletes THIS user's auto-saved layout from dbo.Layout —
            // factory column layout on next open. (Named templates/defaults are managed in the
            // native Layout Manager: right-click a column header.)
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                try
                {
                    string u = (AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? "").Replace("'", "''");
                    string t = ("SCP Meter - " + u);
                    if (t.Length > 60) t = t.Substring(0, 60);
                    // LIKE, because the auto-save is now per tab ("... - <user> - <tab>") and a reset
                    // that cleared only one of the three would leave the others to spring back.
                    _dbSetting.ExecuteNonQuery("DELETE FROM dbo.LayoutUsers WHERE Title LIKE N'" + t + "%'");
                    _dbSetting.ExecuteNonQuery("DELETE FROM dbo.Layout WHERE Title LIKE N'" + t + "%'");
                    // Every tab's auto-save, not just the one in front: the titles share the prefix
                    // above, so the LIKE above already removed them all. Dropping the helpers stops
                    // the FormClosed auto-save from writing them straight back this session.
                    _layoutByPage.Clear();
                    XtraMessageBox.Show("Your saved grid layouts were cleared — reopen the module for the factory layout.",
                        "Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }
            this.TxtSearch.EditValue = null;
            _suppressFilterEvent = true;
            this.ChkShowAll.Checked = false;   // default: filter by the selected billing Day (not show all)
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            int rday = this.CmbDay.Properties.Items.IndexOf(DateTime.Today.Day);
            this.CmbDay.SelectedIndex = rday >= 0 ? rday : (this.CmbDay.Properties.Items.Count > 0 ? 0 : -1);
            _suppressFilterEvent = false;
            LoadData();
        }

        private async void BtnFetch_Click(object sender, EventArgs e)
        {
            if (_dtGrid == null || _dtGrid.Rows.Count == 0)
            { XtraMessageBox.Show("Nothing to fetch — the list is empty.", "Fetch"); return; }
            ActiveView.CloseEditor();
            ActiveView.UpdateCurrentRow();

            int month = SelectedMonth();
            int year = SelectedYear();
            // Clamp the day to the month's length so day 31 behaves in 30-day months (B-6).
            int day = Math.Min(SelectedDay(), DateTime.DaysInMonth(year, month));

            // GUARD (user-hit foot-gun): Fetch always follows the COMBO's period — if the grid is
            // still showing a different loaded period (the user changed Month/Day but never pressed
            // Filter), a fetch would paint period-B readings over the period-A view and stage them
            // under B while the operator believes they are working A. Auto-Filter first so the
            // loaded grid and the fetch period can never disagree.
            if (!_periodCutoff.HasValue || _periodCutoff.Value.Year != year ||
                _periodCutoff.Value.Month != month || _periodCutoff.Value.Day != day)
            {
                LoadData();
                if (_dtGrid == null || _dtGrid.Rows.Count == 0)
                { XtraMessageBox.Show("Nothing to fetch for the selected period — the list is empty.", "Fetch"); return; }
            }
            System.Collections.Generic.List<StageRow> toStage = new System.Collections.Generic.List<StageRow>();
            this.BtnFetch.Enabled = false;
            ShowFetching(true);
            System.Collections.Generic.List<MeterReadingDto> onlineList = null, offlineList = null;
            string onlineErr = null, offlineErr = null;
            try
            {
                // Run the (blocking) HTTP calls OFF the UI thread so the window stays responsive, and
                // catch each endpoint independently so a dead online endpoint still lets offline load.
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // TEST override (Ctrl+Shift+T button): a pasted JSON impersonates the API so the
                    // whole real pipeline below runs on fake data. Null = normal factory client.
                    IMeterReadingApiClient client = _testJsonClient != null
                        ? _testJsonClient : MeterReadingApiClientFactory.Create(_dbSetting);
                    // Fetch both endpoints CONCURRENTLY so the total wait is max(online, offline), not
                    // their sum — each can take ~15s server-side. GetReadings is stateless (a fresh
                    // HttpClient per call), so sharing one client across the two tasks is safe.
                    System.Threading.Tasks.Task tOn = System.Threading.Tasks.Task.Run(() =>
                        { try { onlineList = client.GetReadings(MachineStatus.Online, year, month); } catch (Exception ex) { onlineErr = ex.Message; } });
                    System.Threading.Tasks.Task tOff = System.Threading.Tasks.Task.Run(() =>
                        { try { offlineList = client.GetReadings(MachineStatus.Offline, year, month); } catch (Exception ex) { offlineErr = ex.Message; } });
                    System.Threading.Tasks.Task.WaitAll(tOn, tOff);
                });

                // Merge: online wins over offline for the same machine. Each DTO carries its endpoint.
                // Primary match key = MACHINE SERIAL (zSCP2_Item.SerialNumber is the CSSI's machine);
                // Code (= ServiceItemNo in the mock) is kept as a fallback for APIs without serials.
                Dictionary<string, MeterReadingDto> bySerial = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, MeterReadingDto> byCode = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
                bool includeLate = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_LATE_READINGS,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_LATE_READINGS);
                if (onlineList != null)
                    foreach (MeterReadingDto d in onlineList)
                    {
                        if (!QualifiesByDate(d, year, month, day, includeLate)) continue;
                        if (!string.IsNullOrWhiteSpace(d.SerialNumber)) bySerial[d.SerialNumber.Trim()] = d;
                        if (!string.IsNullOrWhiteSpace(d.Code)) byCode[d.Code.Trim()] = d;
                    }
                if (offlineList != null)
                    foreach (MeterReadingDto d in offlineList)
                    {
                        if (!QualifiesByDate(d, year, month, day, includeLate)) continue;
                        if (!string.IsNullOrWhiteSpace(d.SerialNumber) && !bySerial.ContainsKey(d.SerialNumber.Trim())) bySerial[d.SerialNumber.Trim()] = d;
                        if (!string.IsNullOrWhiteSpace(d.Code) && !byCode.ContainsKey(d.Code.Trim())) byCode[d.Code.Trim()] = d;
                    }

                int matchedMeters = 0;
                int onlineMeters = 0, offlineMeters = 0, conflicts = 0;
                System.Collections.Generic.HashSet<string> matchedItems = new System.Collections.Generic.HashSet<string>();

                // Distinct serial count per CSSI: the byCode fallback (Code = ServiceItemNo, shared by
                // every machine of a multi-machine item) is only safe when the item has ONE machine —
                // otherwise machine B could take machine A's totals.
                System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>> serialsByItem =
                    new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in _dtGrid.Rows)
                {
                    string it = S(r["ServiceItemNo"]).Trim();
                    if (it.Length == 0) continue;
                    System.Collections.Generic.HashSet<string> set;
                    if (!serialsByItem.TryGetValue(it, out set))
                    { set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase); serialsByItem[it] = set; }
                    string sn = S(r["SerialNo"]).Trim();
                    if (sn.Length > 0) set.Add(sn);
                }

                foreach (DataRow r in _dtGrid.Rows)
                {
                    // Already invoiced for this period — done; don't overwrite the reading or status.
                    if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;

                    // Machine serial first (the CSSI's machine identity); ServiceItemNo as fallback,
                    // but only for single-machine items (see serialsByItem above).
                    string serialKey = S(r["SerialNo"]).Trim();
                    string code = S(r["ServiceItemNo"]).Trim();
                    MeterReadingDto dto = null;
                    if (serialKey.Length > 0) bySerial.TryGetValue(serialKey, out dto);
                    if (dto == null && code.Length > 0)
                    {
                        System.Collections.Generic.HashSet<string> sset;
                        bool singleMachine = !serialsByItem.TryGetValue(code, out sset) || sset.Count <= 1;
                        if (singleMachine) byCode.TryGetValue(code, out dto);
                    }
                    if (dto != null)
                    {
                        // Snapshot-locked rows keep their frozen reading — the fresh API value must
                        // not touch them (that is the whole point of the cutoff).
                        if (r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"])) continue;
                        bool isOnline = dto.Status == MachineStatus.Online;
                        if (!string.IsNullOrWhiteSpace(dto.SerialNumber)) r["SerialNo"] = dto.SerialNumber.Trim();
                        r["MachineStatus"] = isOnline ? "ONLINE" : "OFFLINE";
                        // A date the operator typed stays on screen while the reading it belongs
                        // to is unchanged -- the staging UPDATE keeps it, so the grid must too
                        // (feedback ATP-5).
                        bool keptDate = r.Table.Columns.Contains("DateEdited")
                            && r["DateEdited"] != DBNull.Value && Convert.ToBoolean(r["DateEdited"]);
                        if (dto.LastAuditDate.HasValue && !keptDate) r["LastAuditDate"] = dto.LastAuditDate.Value;
                        // Offline readings come off a monthly report with a TrackingId — kept on the
                        // machine's rows (and staged) so Generate can stamp it into the invoice's
                        // Reference No. Online / unmatched rows carry ''.
                        r["TrackingId"] = isOnline ? "" : (dto.TrackingId ?? "").Trim();

                        // The API has exactly TWO counters — TotalBK and TotalCL — so ONLY the machine's
                        // black meter (role BK) and colour meter (role CL) receive a reading. Every other
                        // meter (rental, minimum, fax, … — role NA / flat) has NO counter and must NOT be
                        // given the BK reading. This was the "3rd meter copies the 1st meter" bug: those
                        // NA-role meters fell into the BK branch. They keep reading 0; flat ones are
                        // auto-billed, and MIN-style meters bill their minimum charge.
                        string role = S(r["Role"]).Trim().ToUpperInvariant();
                        if (role != "BK" && role != "CL") continue;

                        decimal apiVal = role == "CL" ? dto.TotalCL : dto.TotalBK;
                        r["FetchedReading"] = apiVal;

                        string src = S(r["EntrySource"]).ToUpperInvariant();
                        if (src == "MANUAL" && Dec(r["CurrentReading"]) != apiVal)
                        {
                            // Keep the saved manual value; surface the conflict for the user to accept
                            // (or reject) in the contract detail form. Do NOT auto-override.
                            r["HasConflict"] = true;
                            r["Status"] = "CONFLICT  API: " + apiVal.ToString("n0") +
                                          "  vs  Manual: " + Dec(r["CurrentReading"]).ToString("n0") +
                                          "  (double-click to resolve)";
                            conflicts++;
                        }
                        else
                        {
                            r["CurrentReading"] = apiVal;
                            r["EntrySource"] = isOnline ? "ONLINE" : "OFFLINE";
                            r["HasConflict"] = false;
                            Recalc(r);
                            // NOT auto-selected — the user picks rows (or Select All) before generating.
                            r["Status"] = "Matched (" + (isOnline ? "Online" : "Offline") + ")  " +
                                (dto.LastAuditDate.HasValue ? dto.LastAuditDate.Value.ToString("dd/MM/yyyy") : "");
                            toStage.Add(new StageRow(D64(r["ItemMeterKey"]), apiVal, dto.LastAuditDate, isOnline ? "ONLINE" : "OFFLINE", S(r["TrackingId"])));
                        }
                        matchedMeters++; matchedItems.Add(code);
                        if (isOnline) onlineMeters++; else offlineMeters++;
                    }
                    else
                    {
                        // No API data for this code — keep any saved manual reading as-is.
                        string src = S(r["EntrySource"]).ToUpperInvariant();
                        if (src != "MANUAL") { r["MachineStatus"] = ""; r["Status"] = "No API data"; }
                    }
                }

                // Persist the fetched readings so reopening the module shows them without re-fetching
                // (Fetch = update). Manual / conflict rows are left as-is (not in toStage).
                if (toStage.Count > 0)
                {
                    try { await System.Threading.Tasks.Task.Run(() => StageReadings(toStage, year, month)); }
                    catch { /* staging is best-effort; never fail the fetch on a staging write */ }
                }

                SyncSelCssi();
                RecomputeNeedManual();   // fetch results re-decide which machines still need key-in
                ActiveGrid.RefreshDataSource();
                UpdateTabCounts();
                if (_tabView != null) _tabView.SelectedTabPage = conflicts > 0 ? _pageConflict : _pageWith;
                ApplyTabFilter();

                // API status: green if both endpoints answered, red if both failed, grey if one failed.
                bool onlineOk = onlineErr == null, offlineOk = offlineErr == null;
                if (onlineOk && offlineOk) UpdateApiStatus(true, "OK");
                else if (!onlineOk && !offlineOk) UpdateApiStatus(false, "UNREACHABLE");
                else UpdateApiStatus(null, onlineOk ? "offline endpoint failed" : "online endpoint failed");

                string errNote = "";
                if (onlineErr != null) errNote += "   ⚠ Online endpoint failed: " + Trunc(onlineErr) + "\r\n";
                if (offlineErr != null) errNote += "   ⚠ Offline endpoint failed: " + Trunc(offlineErr) + "\r\n";

                XtraMessageBox.Show(matchedItems.Count + " service item(s) / " + matchedMeters +
                    " meter(s) matched with last audit date in " +
                    new CultureInfo("en-US").DateTimeFormat.GetMonthName(month) + " " + year + ".\r\n" +
                    "   (readings audited AFTER day " + day + " show a RED Last Audit Date = invoice not created on time)\r\n" +
                    "   • Online machines: " + onlineMeters + " meter(s)\r\n" +
                    "   • Offline machines: " + offlineMeters + " meter(s)\r\n" +
                    (conflicts > 0 ? "   ⚠ " + conflicts + " conflict(s) with saved manual readings — see the Conflicts tab, double-click a contract to resolve.\r\n" : "") +
                    errNote +
                    "Readings saved — next time just reopen this screen (no need to Fetch again unless you want fresh data).\r\n" +
                    "Tick the rows to bill (or 'Select All' on a tab), then 'Generate Invoice'.",
                    "Fetch complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateApiStatus(false, "error");
                XtraMessageBox.Show("Fetch failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowFetching(false);
                this.BtnFetch.Enabled = true;
            }
        }

        // ── API status footer + reachability ─────────────────────────────────
        private void InitApiStatus()
        {
            if (_dbSetting != null)
                _apiBaseUrl = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_BASE_URL,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_API_BASE_URL);
            UpdateApiStatus(null, "checking...");
            CheckApiReachable();
        }

        // Background reachability check of the API host (any HTTP reply = reachable). Non-blocking.
        private async void CheckApiReachable()
        {
            if (string.IsNullOrEmpty(_apiBaseUrl)) { UpdateApiStatus(null, "no URL configured"); return; }
            bool ok = false;
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                        using (System.Net.Http.HttpClient c = new System.Net.Http.HttpClient())
                        {
                            c.Timeout = TimeSpan.FromSeconds(8);
                            c.GetAsync(_apiBaseUrl).GetAwaiter().GetResult();
                            ok = true;
                        }
                    }
                    catch { ok = false; }
                });
            }
            catch { ok = false; }
            UpdateApiStatus(ok, ok ? "OK" : "UNREACHABLE");
        }

        // Paint the footer: green = reachable, red = unreachable, grey = unknown/partial.
        private void UpdateApiStatus(bool? ok, string note)
        {
            if (_lblApiStatus == null) return;
            Color c;
            if (ok == true) c = Color.FromArgb(46, 125, 50);
            else if (ok == false) c = Color.FromArgb(198, 40, 40);
            else c = Color.FromArgb(120, 120, 120);
            _lblApiStatus.Appearance.ForeColor = c;
            _lblApiStatus.Appearance.Options.UseForeColor = true;
            _lblApiStatus.Text = "● Meter API:  " + (_apiBaseUrl ?? "(not configured)") +
                (string.IsNullOrEmpty(note) ? "" : "     " + note);
        }

        private static string Trunc(string s)
        {
            return string.IsNullOrEmpty(s) ? s : (s.Length > 140 ? s.Substring(0, 140) + "..." : s);
        }

        // Show/hide the "thinking" fetch overlay (marquee bar + cycling wording), centered on the form.
        private void ShowFetching(bool on)
        {
            if (_pnlFetching == null) return;
            if (on)
            {
                _fetchElapsed = 0; _fetchMsgIdx = 0;
                if (_lblFetchMsg != null) _lblFetchMsg.Text = FETCH_MSGS[0] + "    (0s)";
                _pnlFetching.Location = new Point(
                    Math.Max(0, (this.ClientSize.Width - _pnlFetching.Width) / 2),
                    Math.Max(0, (this.ClientSize.Height - _pnlFetching.Height) / 2));
                _pnlFetching.Visible = true;
                _pnlFetching.BringToFront();
                if (_fetchTimer != null) _fetchTimer.Start();
            }
            else
            {
                if (_fetchTimer != null) _fetchTimer.Stop();
                _pnlFetching.Visible = false;
            }
        }

        // Tick once a second while fetching: count elapsed seconds, rotate the wording every 3s.
        private void FetchTimer_Tick(object sender, EventArgs e)
        {
            _fetchElapsed++;
            if (_fetchElapsed % 3 == 0) _fetchMsgIdx = (_fetchMsgIdx + 1) % FETCH_MSGS.Length;
            if (_lblFetchMsg != null) _lblFetchMsg.Text = FETCH_MSGS[_fetchMsgIdx] + "    (" + _fetchElapsed + "s)";
        }

        // A reading qualifies when its Last Audit Date is in the selected MONTH. With the Setting's
        // "Include readings audited AFTER the billing day" flag ON (default), late readings are
        // accepted too — shown with a RED Last Audit Date (= the invoice wasn't created on time),
        // still billable (difference-based usage self-corrects next month). Flag OFF = strict
        // on/before-the-billing-day matching, the original behaviour.
        private static bool QualifiesByDate(MeterReadingDto d, int year, int month, int day, bool includeLate)
        {
            if (!d.LastAuditDate.HasValue) return false;
            DateTime la = d.LastAuditDate.Value;
            if (la.Year != year || la.Month != month) return false;
            return includeLate || la.Day <= day;
        }

        // "Select Matched" — tick every meter that got a current reading.
        // "Select All" — toggles selection of every row on the CURRENT TAB only (the grid's active
        // filter). If any visible row is unticked → tick all; if all are ticked → untick all.
        private void BtnSelfManualKeyIn_Click(object sender, EventArgs e)
        {
            if (_dtGrid == null) return;
            ActiveView.CloseEditor();

            List<DataRow> visible = new List<DataRow>();
            for (int rh = 0; rh < ActiveView.RowCount; rh++)
            {
                DataRow r = ActiveView.GetDataRow(rh);
                if (r != null) visible.Add(r);
            }
            if (visible.Count == 0)
            { XtraMessageBox.Show("No rows on this tab.", "Select All"); return; }

            bool anyUnticked = false;
            foreach (DataRow r in visible)
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) { anyUnticked = true; break; }

            foreach (DataRow r in visible) r["Sel"] = anyUnticked;
            SyncSelCssi();
            ActiveGrid.RefreshDataSource();
            UpdateTabCounts();
            UpdateSelectAllButton();   // Select All <-> Unselect All
        }

        // Button caption mirrors the CURRENT TAB's state: all visible rows ticked -> "Unselect All",
        // otherwise "Select All".
        private void UpdateSelectAllButton()
        {
            if (_dtGrid == null) return;
            bool anyRow = false, anyUnticked = false;
            for (int rh = 0; rh < ActiveView.RowCount; rh++)
            {
                DataRow r = ActiveView.GetDataRow(rh);
                if (r == null) continue;
                anyRow = true;
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) { anyUnticked = true; break; }
            }
            this.BtnSelfManualKeyIn.Text = (anyRow && !anyUnticked) ? "Unselect All" : "Select All";
        }

        // ───────────────────── Manual key-in + staging (zSCP2_MeterEntry) ─────────────────────

        // Reload any saved current readings for this period back into the grid, so manual key-ins
        // (and previously accepted API overrides) survive a restart and reappear as "saved".
        private void PrefillFromStaging(int month, int year)
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.PrefillFromStaging(
                _dbSetting, _dtGrid, month, year, _ladders);
        }

        // One fetched reading queued for staging (persisted so a reopen shows it without re-fetching).
        private class StageRow
        {
            public long ItemMeterKey;
            public decimal Reading;
            public DateTime? Date;
            public string Source;
            public string Tracking;
            public StageRow(long k, decimal reading, DateTime? d, string s, string tid)
            { ItemMeterKey = k; Reading = reading; Date = d; Source = s; Tracking = tid ?? ""; }
        }

        // Persist a batch of fetched readings to zSCP2_MeterEntry in one transaction. Overwrites any
        // existing staged value for the same meter + period (Fetch = update). Safe to call off the UI thread.
        private void StageReadings(System.Collections.Generic.List<StageRow> rows, int year, int month)
        {
            if (rows == null || rows.Count == 0 || _dbSetting == null) return;
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("StageFetch"))
                {
                    try
                    {
                        foreach (StageRow sr in rows)
                            UpsertStaging(cn, tx, sr.ItemMeterKey, year, month, sr.Reading, sr.Date, sr.Source, sr.Tracking);
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Open the Meter Reading settings dialog; reload the list if the user changed a setting
        // (e.g. include/exclude expired service items).
        private void BtnSetting_Click(object sender, EventArgs e)
        {
            using (MeterReadingSetting_Form dlg = new MeterReadingSetting_Form(_dbSetting, SelectedYear(), SelectedMonth()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshGroupingInfo();
                    LoadData();
                }
            }
        }

        /// <summary>The Invoice grouping SETTING: FOLLOW (contract Billing Mode) / DEBTOR / MACHINE.</summary>
        private string GroupingMode()
        {
            try
            {
                string m = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_GROUPING,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_GROUPING).Trim().ToUpperInvariant();
                if (m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR ||
                    m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) return m;
            }
            catch { }
            return ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_FOLLOW;
        }

        private bool GroupGuardOn()
        {
            try
            {
                return ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_GROUP_GUARD,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_GROUP_GUARD);
            }
            catch { return true; }
        }

        private void RefreshGroupingInfo()
        {
            if (_lblGroupingInfo == null) return;
            string m = GroupingMode();
            _lblGroupingInfo.Text =
                m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR ? "Grouping: one invoice per customer  (Setting)" :
                m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE ? "Grouping: one invoice per machine  (Setting)" :
                "Grouping: follow contract Billing Mode  (Setting)";
        }

        // Insert-or-update one staged reading (unique per ItemMeterKey + period).
        private void UpsertStaging(SqlConnection cn, SqlTransaction tx, long itemMeterKey, int year, int month,
            decimal reading, DateTime? readingDate, string source, string trackingId)
        {
            // LOCKED rows (billing-day auto-fetch snapshot) can never be overridden — not by a later
            // fetch, not by manual key-in.
            SqlCommand chk = new SqlCommand(
                "SELECT LockedAt FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn, tx);
            chk.Parameters.AddWithValue("@imk", itemMeterKey);
            chk.Parameters.AddWithValue("@yr", year);
            chk.Parameters.AddWithValue("@mo", month);
            object lockedAt = chk.ExecuteScalar();
            if (lockedAt != null && lockedAt != DBNull.Value) return;

            SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, " +
                // A date somebody TYPED survives a re-save of the same reading (feedback ATP-5) --
                // including a fetch that comes back with the number already staged. A DIFFERENT
                // reading is a new reading and takes a fresh stamp.
                "ReadingDate = CASE WHEN ISNULL(ReadingDateEdited,'N')='Y' AND CurrentReading=@rd " +
                "                   THEN ReadingDate ELSE @dt END, " +
                "ReadingDateEdited = CASE WHEN ISNULL(ReadingDateEdited,'N')='Y' AND CurrentReading=@rd " +
                "                        THEN 'Y' ELSE 'N' END, " +
                "Source=@src, TrackingId=@tid, LastModified=GETDATE() " +
                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo AND LockedAt IS NULL; " +
                "IF @@ROWCOUNT=0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo) " +
                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source,TrackingId) " +
                "VALUES (@imk,@yr,@mo,@rd,@dt,@src,@tid);", cn, tx);
            cmd.Parameters.AddWithValue("@imk", itemMeterKey);
            cmd.Parameters.AddWithValue("@yr", year);
            cmd.Parameters.AddWithValue("@mo", month);
            cmd.Parameters.AddWithValue("@rd", reading);
            cmd.Parameters.AddWithValue("@dt", (object)readingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@src", source);
            cmd.Parameters.AddWithValue("@tid", trackingId ?? "");
            cmd.ExecuteNonQuery();
            // Immutable audit trail: every staged reading (manual or API) is also APPENDED to the log.
            ServiceContractPhotocopier.Classes.ScpMeterReadingLog.Append(
                cn, tx, itemMeterKey, year, month, reading, readingDate, source, "");
        }

        // Remove a staged reading (used when the user clears a reading back to 0).
        private void DeleteStaging(SqlConnection cn, SqlTransaction tx, long itemMeterKey, int year, int month)
        {
            SqlCommand cmd = new SqlCommand(
                "DELETE FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo", cn, tx);
            cmd.Parameters.AddWithValue("@imk", itemMeterKey);
            cmd.Parameters.AddWithValue("@yr", year);
            cmd.Parameters.AddWithValue("@mo", month);
            cmd.ExecuteNonQuery();
            // Audit: record that the staged reading was cleared (the log itself keeps prior rows).
            ServiceContractPhotocopier.Classes.ScpMeterReadingLog.Append(
                cn, tx, itemMeterKey, year, month, 0m, null,
                ServiceContractPhotocopier.Classes.ScpMeterReadingLog.SOURCE_CLEARED, "");
        }

        // ONE rule for "this Current Reading cell is the operator's to fill" (demo #2):
        // a usage meter (BK/CL), not flat/rental, not a locked auto-fetch snapshot, not invoiced
        // this period. Drives the inline editor gate AND the must-fill/not-fill cell colours.
        private bool IsInlineEditable(DataRow r)
        {
            if (r == null) return false;
            if (r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"])) return false;
            if (r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"])) return false;
            if (S(r["InvoicedDocNo"]).Trim().Length > 0) return false;
            string role = S(r["Role"]).Trim().ToUpperInvariant();
            return role == "BK" || role == "CL";
        }

        // Demo #2(a): Current Reading edits INLINE on any tab — allowed only where the cell is the
        // operator's to fill. Flat/rental, locked-snapshot and invoiced rows keep refusing the
        // editor (the Invoiced tab only shows invoiced rows, so it is read-only for free).
        private void GridViewMeter_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (V(sender).FocusedColumn == null) return;
            DataRow r = V(sender).GetDataRow(V(sender).FocusedRowHandle);
            // The day a keyed reading was taken (feedback ATP-5): same rules as Meter Invoice Run,
            // one shared judgement so the two screens cannot disagree.
            if (V(sender).FocusedColumn.FieldName == "LastAuditDate")
            {
                if (r == null || ServiceContractPhotocopier.Classes.ScpInvoiceRun.WhyDateFixed(r).Length > 0)
                    e.Cancel = true;
                return;
            }
            if (V(sender).FocusedColumn.FieldName != "CurrentReading") return;
            if (r == null || !IsInlineEditable(r)) e.Cancel = true;
        }

        // Demo #2(a): the grid twin of the detail dialog's save (manual / cleared-to-0 branches).
        // One short transaction per committed cell. Deliberately does NOT touch NeedManual — tab
        // membership stays a snapshot until the next Refresh/Fetch, same as the dialog.
        /// <summary>Move the day a keyed reading was taken (feedback ATP-5). The reading and the
        /// money stay where they are; the date moves, and with it what the invoice prints and what
        /// the next period counts from. Anything that must not be changed is refused with a reason
        /// and the cell goes back to what the database holds.</summary>
        private void SaveInlineReadingDate(DataRow r, object value)
        {
            if (_dbSetting == null || r == null) return;
            long imk = D64(r["ItemMeterKey"]);
            if (imk <= 0) return;
            int year = SelectedYear(), month = SelectedMonth();
            string no = ServiceContractPhotocopier.Classes.ScpInvoiceRun.WhyDateFixed(r);
            if (no.Length == 0 && (value == null || value == DBNull.Value))
                no = "A reading has to carry the day it was taken -- pick a date.";
            if (no.Length == 0)
                no = ServiceContractPhotocopier.Classes.ScpInvoiceRun.WhyDateWrong(r, Convert.ToDateTime(value));
            if (no.Length > 0)
            {
                XtraMessageBox.Show(no, "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStagedDate(r, imk, year, month);
                ActiveGrid.RefreshDataSource();
                return;
            }
            try
            {
                ServiceContractPhotocopier.Classes.ScpInvoiceRun.SaveReadingDate(
                    _dbSetting, imk, year, month, Convert.ToDateTime(value).Date);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The date was not saved:\r\n" + ex.Message,
                    "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            ShowStagedDate(r, imk, year, month);
            ActiveGrid.RefreshDataSource();
        }

        /// <summary>Put the staged reading date (and whether a person set it) back on the row --
        /// read back rather than assumed, because saving the SAME reading again keeps a hand-set
        /// date while a different reading takes a fresh stamp.</summary>
        private void ShowStagedDate(DataRow r, long itemMeterKey, int year, int month)
        {
            try
            {
                bool edited;
                DateTime? when = ServiceContractPhotocopier.Classes.ScpInvoiceRun.StagedDate(
                    _dbSetting, itemMeterKey, year, month, out edited);
                if (when.HasValue) r["LastAuditDate"] = when.Value; else r["LastAuditDate"] = DBNull.Value;
                if (r.Table.Columns.Contains("DateEdited")) r["DateEdited"] = edited;
            }
            catch { }
        }

        private void SaveInlineReading(DataRow r)
        {
            if (_dbSetting == null || r == null) return;
            long imk = D64(r["ItemMeterKey"]);
            if (imk <= 0) return;
            int year = SelectedYear(), month = SelectedMonth();
            decimal cv = Dec(r["CurrentReading"]);
            try
            {
                using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("InlineKeyIn"))
                    {
                        try
                        {
                            if (cv > 0m)
                                UpsertStaging(cn, tx, imk, year, month, cv, DateTime.Now, "MANUAL", S(r["TrackingId"]));
                            else
                                DeleteStaging(cn, tx, imk, year, month);
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                // NEVER lose the typed value silently: keep it in the grid, flag it, and say so.
                r["Status"] = "⚠ NOT SAVED — key in again";
                XtraMessageBox.Show("The reading was typed but NOT saved to the database:\r\n" + ex.Message +
                    "\r\n\r\nThe value stays in the grid for now but will be LOST on Refresh — " +
                    "key it in again once the connection is back.",
                    "Meter Reading", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cv > 0m)
            {
                r["EntrySource"] = "MANUAL";
                r["HasConflict"] = false;
                r["Sel"] = true;
                r["Status"] = "Manual (saved)  " + DateTime.Now.ToString("dd/MM/yyyy");
                ShowStagedDate(r, imk, year, month);   // the date this reading now carries
            }
            else
            {   // cleared to 0 → same full reset as the detail dialog's cleared branch
                r["EntrySource"] = ""; r["TrackingId"] = ""; r["HasConflict"] = false;
                r["FetchedReading"] = 0m; r["MachineStatus"] = ""; r["Sel"] = false; r["Status"] = "";
            }
            SyncSelCssi();   // Sel changed programmatically — keep the merged checkbox truthful
        }

        // Open the per-contract detail / override dialog. The dialog shows every meter of the contract
        // with its saved/manual Current vs the freshly-Fetched value; the user ticks "Accept fetched"
        // to override (typically to resolve a conflict). On OK we persist the accepted values to
        // staging (source ONLINE/OFFLINE) and update the grid in place.
        private void OpenContractDetail(long contractKey, string contractNo, string customer,
            long itemKey, string serviceItemNo)
        {
            if (_dtGrid == null) return;
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                if (D64(r["ContractKey"]) != contractKey) continue;
                // #3b: when opened from a machine's row, show only THAT machine's meters.
                if (itemKey > 0 && D64(r["ItemKey"]) != itemKey) continue;
                rows.Add(r);
            }
            if (rows.Count == 0) return;

            DataTable dt = new DataTable();
            dt.Columns.Add("ServiceItemNo", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("MeterType", typeof(string));
            dt.Columns.Add("Role", typeof(string));
            dt.Columns.Add("MeterTypeName", typeof(string));
            dt.Columns.Add("MinCharges", typeof(decimal));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("RebatePct", typeof(decimal));
            dt.Columns.Add("LastReadDate", typeof(DateTime));
            dt.Columns.Add("LastReading", typeof(decimal));
            dt.Columns.Add("CurrentReading", typeof(decimal));
            dt.Columns.Add("MeterUsage", typeof(decimal));
            dt.Columns.Add("TotalCharges", typeof(decimal));
            dt.Columns.Add("FetchedReading", typeof(decimal));
            dt.Columns.Add("Source", typeof(string));
            dt.Columns.Add("HasConflict", typeof(bool));
            dt.Columns.Add("AcceptFetched", typeof(bool));
            dt.Columns.Add("LastInvNo", typeof(string));
            dt.Columns.Add("LastInvDate", typeof(DateTime));
            dt.Columns.Add("ItemMeterKey", typeof(long));
            dt.Columns.Add("UseMin", typeof(bool));
            dt.Columns.Add("MultiPriceCode", typeof(string));   // ladder key ('#<meterkey>' = per-meter override)
            dt.Columns.Add("FocResetCount", typeof(int));
            dt.Columns.Add("IsFlat", typeof(bool));
            dt.Columns.Add("IsWaive", typeof(bool));
            dt.Columns.Add("Deal", typeof(string));        // effective pricing/deal summary (display-only)
            dt.Columns.Add("HasLadder", typeof(bool));     // a multi-price ladder drives price + FOC
            dt.Columns.Add("LadderFoc", typeof(decimal));  // the ladder's free band = the EFFECTIVE FOC
            foreach (DataRow r in rows)
            {
                DataRow d = dt.NewRow();
                d["ServiceItemNo"] = S(r["ServiceItemNo"]);
                d["SerialNo"] = S(r["SerialNo"]);
                d["MeterType"] = S(r["MeterType"]);
                d["Role"] = S(r["Role"]) == "CL" ? "Colour (CL)" : "Black (BK)";
                d["MeterTypeName"] = S(r["MeterTypeName"]);
                d["MinCharges"] = Dec(r["MinCharges"]);
                d["UnitPrice"] = Dec(r["UnitPrice"]);
                d["FOCQty"] = Dec(r["FOCQty"]);
                d["RebatePct"] = Dec(r["RebatePct"]);
                if (r["LastReadDate"] != DBNull.Value) d["LastReadDate"] = Convert.ToDateTime(r["LastReadDate"]);
                d["LastReading"] = Dec(r["LastReading"]);
                d["CurrentReading"] = Dec(r["CurrentReading"]);
                d["MeterUsage"] = Dec(r["MeterUsage"]);
                d["TotalCharges"] = Dec(r["TotalCharges"]);
                d["FetchedReading"] = Dec(r["FetchedReading"]);
                d["Source"] = S(r["EntrySource"]);
                bool conf = r["HasConflict"] != DBNull.Value && Convert.ToBoolean(r["HasConflict"]);
                d["HasConflict"] = conf;
                d["AcceptFetched"] = conf;   // pre-tick conflicts
                d["LastInvNo"] = S(r["LastInvNo"]);
                if (r["LastInvDate"] != DBNull.Value) d["LastInvDate"] = Convert.ToDateTime(r["LastInvDate"]);
                d["ItemMeterKey"] = D64(r["ItemMeterKey"]);
                d["UseMin"] = r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"]);
                d["MultiPriceCode"] = S(r["MultiPriceCode"]);
                d["FocResetCount"] = ServiceContractPhotocopier.Classes.ScpInvoiceJobs.FocResetCountFor(r, SelectedYear(), SelectedMonth());
                bool dIsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                bool dIsWaive = r.Table.Columns.Contains("IsWaive") && r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);
                d["IsFlat"] = dIsFlat;
                d["IsWaive"] = dIsWaive;

                // What ACTUALLY bills, in one sentence — the raw Unit Price / FOC Qty cells alone
                // mislead the key-in operator whenever a ladder or a deal config takes over.
                string mpc = S(r["MultiPriceCode"]);
                bool hasLadder = mpc.Length > 0 && _ladders != null && _ladders.ContainsKey(mpc);
                decimal ladderFoc = 0m;
                string ladderNext = "";
                if (hasLadder)
                {
                    foreach (decimal[] t in _ladders[mpc])
                    {
                        if (t[1] == 0m) { if (t[0] > ladderFoc) ladderFoc = t[0]; }
                        else { ladderNext = t[1].ToString("0.####"); break; }
                    }
                }
                d["HasLadder"] = hasLadder;
                d["LadderFoc"] = ladderFoc;
                string deal = "";
                if (dIsWaive)
                {
                    int wn = r["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["WaiveFirstNMonths"]);
                    decimal wt = Dec(r["WaiveTargetAmount"]);
                    decimal wpt = Dec(r["WaivePartialThreshold"]);
                    decimal wpa = Dec(r["WaivePartialAmount"]);
                    deal = ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form.Summary(wn, wt, wpt, wpa);
                    string wsc = S(r["WaiveScope"]).Trim().ToUpperInvariant();
                    if (wt > 0m && (wsc == "BK" || wsc == "CL")) deal += " on " + (wsc == "BK" ? "Black" : "Colour");
                }
                else if (dIsFlat && Dec(r["MinCharges"]) > 0m &&
                         ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinMeterCode(S(r["MeterType"])))
                {
                    string csc = S(r["WaiveScope"]).Trim().ToUpperInvariant();
                    deal = "MIN " + Dec(r["MinCharges"]).ToString("#,##0.00") + " on " +
                           (csc == "BK" ? "Black" : csc == "CL" ? "Colour" : "BK+CL") + " - tops up only";
                }
                else if (dIsFlat)
                {
                    decimal rentAmt = Dec(r["UnitPrice"]) > 0m ? Dec(r["UnitPrice"]) : Dec(r["MinCharges"]);
                    deal = "RENTAL " + rentAmt.ToString("#,##0.00") + " flat / month";
                }
                else if (hasLadder)
                {
                    deal = "MULTI-PRICE " + (mpc.StartsWith("#") ? "(custom)" : mpc) +
                           (ladderFoc > 0m ? ": first " + ladderFoc.ToString("#,##0") + " FREE" : "") +
                           (ladderNext.Length > 0 ? ", then " + ladderNext + "/copy" : "");
                }
                d["Deal"] = deal;
                dt.Rows.Add(d);
            }

            bool genReq = false;
            string dlgTitle = itemKey > 0 ? contractNo + "   ·   " + serviceItemNo : contractNo;
            using (MeterReadingDetail_Form f = new MeterReadingDetail_Form(dlgTitle, customer, dt, _dbSetting, contractKey))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                genReq = f.GenerateRequested;
            }

            int year = SelectedYear(), month = SelectedMonth(), applied = 0;
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("Override"))
                {
                    try
                    {
                        foreach (DataRow d in dt.Rows)
                        {
                            long imk = D64(d["ItemMeterKey"]);
                            DataRow gr = null;
                            foreach (DataRow r in rows) if (D64(r["ItemMeterKey"]) == imk) { gr = r; break; }
                            if (gr == null) continue;

                            bool accept = d["AcceptFetched"] != DBNull.Value && Convert.ToBoolean(d["AcceptFetched"]);
                            decimal fv = Dec(d["FetchedReading"]);
                            decimal cv = Dec(d["CurrentReading"]);   // possibly typed by the user

                            if (accept && fv > 0m)
                            {
                                // Accept the fetched API value (resolves a conflict).
                                string src = S(gr["MachineStatus"]) == "OFFLINE" ? "OFFLINE" : "ONLINE";
                                UpsertStaging(cn, tx, imk, year, month, fv, DateTime.Now, src, S(gr["TrackingId"]));
                                gr["CurrentReading"] = fv;
                                gr["EntrySource"] = src;
                                gr["HasConflict"] = false;
                                gr["Sel"] = true;
                                gr["Status"] = "Matched (" + (src == "OFFLINE" ? "Offline" : "Online") + ", overridden)  " +
                                               DateTime.Now.ToString("dd/MM/yyyy");
                                Recalc(gr);
                                applied++;
                            }
                            else if (cv > 0m)
                            {
                                // Manual key-in from the detail form. Skip API rows the user left unchanged.
                                string grSrc = S(gr["EntrySource"]).ToUpperInvariant();
                                bool isApi = grSrc == "ONLINE" || grSrc == "OFFLINE";
                                if (isApi && cv == Dec(gr["CurrentReading"])) continue;
                                UpsertStaging(cn, tx, imk, year, month, cv, DateTime.Now, "MANUAL", S(gr["TrackingId"]));
                                gr["CurrentReading"] = cv;
                                gr["EntrySource"] = "MANUAL";
                                gr["HasConflict"] = false;
                                gr["Sel"] = true;
                                gr["Status"] = "Manual (saved)  " + DateTime.Now.ToString("dd/MM/yyyy");
                                Recalc(gr);
                                applied++;
                            }
                            else
                            {
                                // Cleared back to 0 → delete any staged reading and reset the row.
                                if (Dec(gr["CurrentReading"]) > 0m || S(gr["EntrySource"]).Length > 0)
                                {
                                    DeleteStaging(cn, tx, imk, year, month);
                                    gr["CurrentReading"] = 0m;
                                    gr["EntrySource"] = "";
                                    gr["TrackingId"] = "";
                                    gr["HasConflict"] = false;
                                    gr["FetchedReading"] = 0m;
                                    gr["MachineStatus"] = "";
                                    gr["Sel"] = false;
                                    gr["Status"] = "";
                                    Recalc(gr);
                                    applied++;
                                }
                            }
                        }
                        tx.Commit();
                    }
                    catch (Exception ex) { tx.Rollback();
                        XtraMessageBox.Show("Override failed:\r\n" + ex.Message, "Override", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; }
                }
            }
            ActiveGrid.RefreshDataSource();
            UpdateTabCounts();
            ApplyTabFilter();

            if (genReq)
            {
                // "Save & Generate Invoice" from the detail dialog: bill THIS contract only. Other
                // contracts' ticks are parked and restored after the (modal) generate run.
                List<DataRow> parked = new List<DataRow>();
                foreach (DataRow r in _dtGrid.Rows)
                {
                    bool s = r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]);
                    if (s && D64(r["ContractKey"]) != contractKey) { parked.Add(r); r["Sel"] = false; }
                }
                foreach (DataRow r in rows)
                {
                    // Flat/rental meters have NO reading (CurrentReading stays 0 by design) — they must
                    // ride this generate too, or the dialog path silently drops the rent every time.
                    bool tickFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                    if ((tickFlat || Dec(r["CurrentReading"]) > 0m) && S(r["InvoicedDocNo"]).Trim().Length == 0)
                        r["Sel"] = true;
                }
                SyncSelCssi();
                // Scoped one-contract run: the #3 completeness guard must not demand the debtor's
                // OTHER machines here — the user explicitly asked to bill just this contract/machine.
                _scopedGenerate = true;
                try { BtnGenerateInvoice_Click(this, EventArgs.Empty); }
                finally { _scopedGenerate = false; }
                foreach (DataRow r in parked)
                    if (r.RowState != DataRowState.Detached) r["Sel"] = true;
                SyncSelCssi();
                ActiveGrid.RefreshDataSource();
                return;
            }

            if (applied > 0)
                XtraMessageBox.Show(applied + " reading(s) saved.", "Meter Reading",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // One click on the Select checkbox posts immediately (fires CellValueChanged right away).
        private void SelCssiEditor_EditValueChanged(object sender, EventArgs e)
        {
            ActiveView.PostEditor();
        }

        // One left-click anywhere on the (merged) Select cell toggles the whole machine.
        private void GridViewMeter_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Left) return;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = V(sender).CalcHitInfo(e.Location);
            if (hit.RowHandle < 0 || hit.Column == null || hit.Column.FieldName != "SelCssi" || !hit.InRowCell) return;
            object cur = V(sender).GetRowCellValue(hit.RowHandle, "SelCssi");
            bool v = !(cur != null && cur != DBNull.Value && Convert.ToBoolean(cur));
            V(sender).SetRowCellValue(hit.RowHandle, "SelCssi", v);
            DevExpress.Utils.DXMouseEventArgs dx = DevExpress.Utils.DXMouseEventArgs.GetMouseArgs(e);
            if (dx != null) dx.Handled = true;
        }

        private void GridViewMeter_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "LastAuditDate")
            {
                SaveInlineReadingDate(V(sender).GetDataRow(e.RowHandle), e.Value);
                return;
            }
            if (e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMin")
            {
                DataRow r = V(sender).GetDataRow(e.RowHandle);
                if (r != null)
                {
                    Recalc(r);
                    // Demo #2(a): persist inline key-ins immediately — the old grid path recalculated
                    // but never saved, so a Refresh silently lost the typed reading. UseMin stays
                    // grid-only. (Programmatic DataRow writes — dialog/fetch — don't raise this event.)
                    if (e.Column.FieldName == "CurrentReading" && IsInlineEditable(r))
                        SaveInlineReading(r);
                }
            }
            if (e.Column.FieldName == "SelCssi")
            {
                // The per-CSSI checkbox drives the whole machine: tick/untick BOTH meter rows.
                DataRow r = V(sender).GetDataRow(e.RowHandle);
                if (r != null && _dtGrid != null)
                {
                    bool v = r["SelCssi"] != DBNull.Value && Convert.ToBoolean(r["SelCssi"]);
                    long ik = D64(r["ItemKey"]);
                    foreach (DataRow x in _dtGrid.Rows)
                        if (D64(x["ItemKey"]) == ik) { x["Sel"] = v; x["SelCssi"] = v; }
                    V(sender).LayoutChanged();   // repaint merged cells; no full rebind mid-edit
                }
            }
            else if (e.Column.FieldName == "Sel")
            {
                // Per-meter tick: mirror the CSSI checkbox = "all this machine's meters are ticked".
                DataRow r = V(sender).GetDataRow(e.RowHandle);
                if (r != null && _dtGrid != null)
                {
                    long ik = D64(r["ItemKey"]);
                    bool all = true;
                    foreach (DataRow x in _dtGrid.Rows)
                        if (D64(x["ItemKey"]) == ik &&
                            !(x["Sel"] != DBNull.Value && Convert.ToBoolean(x["Sel"]))) { all = false; break; }
                    foreach (DataRow x in _dtGrid.Rows)
                        if (D64(x["ItemKey"]) == ik) x["SelCssi"] = all;
                }
            }
            if (e.Column.FieldName == "Sel" || e.Column.FieldName == "SelCssi" ||
                e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMin")
            {
                UpdateTabCounts();   // keep the billing-total summary live
                UpdateSelectAllButton();
            }
        }

        // Recompute every CSSI checkbox from its meter rows (call after any bulk Sel change).
        private void SyncSelCssi()
        {
            if (_dtGrid == null) return;
            Dictionary<long, bool> allByItem = new Dictionary<long, bool>();
            foreach (DataRow x in _dtGrid.Rows)
            {
                long ik = D64(x["ItemKey"]);
                bool sel = x["Sel"] != DBNull.Value && Convert.ToBoolean(x["Sel"]);
                bool cur;
                allByItem[ik] = allByItem.TryGetValue(ik, out cur) ? (cur && sel) : sel;
            }
            foreach (DataRow x in _dtGrid.Rows)
                x["SelCssi"] = allByItem[D64(x["ItemKey"])];
        }

        // Multi-price tier ladders for this billing run (code -> ascending [boundary, unitPrice]).
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<decimal[]>> _ladders;

        // FOC-reset accrual multiplier for a row: how many of the contract's reset periods fall in the
        // billed (monthly) span. 'M'/default = 1; 'W' ≈ 4; 'D' N ≈ daysInMonth/N. Applied to the FOC allowance.
        
        // Grid preview charge = the SAME engine the invoice uses (ScpInvoiceBuilder.ComputeCharge), so grid
        // and invoice can never diverge: NET (FOC copies + rebate deducted) + multi-price tier + min floor.
        private void Recalc(DataRow r)
        {
            ServiceContractPhotocopier.Classes.ScpBillingRows.Recalc(r, _ladders, SelectedYear(), SelectedMonth());
        }

        /// <summary>Which merged row this meter prints on, or null when it prints on its own.
        /// Rental merges on MergeGroupCode and copies on MergeGroupCodeMeter -- the two columns the
        /// contract's Billing Setup writes and the fold engine reads.</summary>
        private string MergeKeyOf(DataRow r)
        {
            if (r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"])) return null;
            bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            string g = S(r[flat ? "MergeGroupCode" : "MergeGroupCodeMeter"]).Trim();
            if (g.Length == 0) return null;
            return D64(r["ContractKey"]) + "|" + (flat ? "rental " : "copies ") + g.ToUpperInvariant();
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _dtGrid == null) return;
            ActiveView.CloseEditor();
            ActiveView.UpdateCurrentRow();

            // Group the selected meter lines: Group mode = one invoice per contract; Separate = per item.
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs;
            int genMonth = SelectedMonth(), genYear = SelectedYear();
            int alreadyInvoiced;
            string monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(genMonth) + " " + genYear;

            // Only rows on the CURRENT TAB (the grid's active filter) are considered.
            // Generate honours EVERY ticked row, not just the visible ones — a row ticked on one tab
            // and then hidden by a tab/filter switch must never be silently skipped (UX-panel finding).
            List<DataRow> visibleRows = new List<DataRow>();
            foreach (DataRow dr in _dtGrid.Rows) visibleRows.Add(dr);

            // Invoice grouping SETTING (2026-08-04): FOLLOW = each contract's own Billing Mode
            // (G = one invoice per contract, S = per machine); DEBTOR = legacy one-per-customer;
            // MACHINE = force per machine. The incomplete-group protections below are gated by the
            // "Block Generate when a group is incomplete" setting (default ON).
            string grpMode = GroupingMode();
            bool guardOn = GroupGuardOn();

            // Which INVOICE a row would land on. The completeness guards below ask "is anything
            // missing from this invoice", and until now they asked "is anything missing from this
            // contract" -- which is the same question only while a contract makes one invoice.
            //
            // It stopped being the same question the day a rental could bill apart from its copies.
            // Billing the rent on the 1st and the copies on the 7th is not a half-finished contract;
            // it is two invoices, each complete, a week apart. The old guard called the second half
            // "not ticked" and refused to generate the first.
            //
            // Keyed the way ScpInvoiceJobs keys a job: the contract, its bill group, and which side
            // of the rental split the row is on.

            // (helpers live below; this comment sits where the guards start)
            // The Meters filter drops RENTAL, WAIVE and COMMIT rows from the view whenever it is not
            // "All" -- deliberately, so a BK-only run is not cluttered by them. The cost is that the
            // operator cannot tick what is not on screen, and the completeness guards below then
            // report those rows "not ticked" without saying why they could not be ticked. Every guard
            // message carries this line while the filter is narrowing the view.
            string hiddenByFilter = _cmbMeterFilter == null ? ""
                : (_cmbMeterFilter.SelectedIndex == 4 ? "black, colour and minimum rows"
                : (MeterRoleFilter().Length == 0 ? "" : "rental, minimum and waive rows"));
            string filterNote = hiddenByFilter.Length == 0 ? ""
                : "\r\n" + "\r\n" +
                  "NOTE: the Meters filter is set to \"" + _cmbMeterFilter.Text + "\", so " +
                  hiddenByFilter + " are hidden from this list and cannot be ticked. " +
                  "Set it to All to see them.";

            // Demo 28/07 #3: "one invoice per customer" must be COMPLETE — if any of a ticked
            // customer's machines would MISS the grouped invoice (not ticked, or a usage meter
            // without a reading), abort with the exact list instead of quietly billing a partial
            // customer invoice that needs a manual credit note later. A scoped run (detail dialog's
            // "Save & Generate" for ONE contract) is exempt — that incompleteness is deliberate.
            if (guardOn && grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR && !_scopedGenerate)
            {
                HashSet<string> tickedDebtors = new HashSet<string>();
                foreach (DataRow dr in visibleRows)
                    if (dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"])
                        && S(dr["InvoicedDocNo"]).Trim().Length == 0)
                        tickedDebtors.Add(S(dr["DebtorCode"]) + "|" + RentalSideOf(dr));
                List<string> problems = new List<string>();
                foreach (DataRow dr in visibleRows)
                {
                    if (!tickedDebtors.Contains(S(dr["DebtorCode"]) + "|" + RentalSideOf(dr))) continue;
                    if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;   // already billed = fine
                    bool gTicked = dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"]);
                    bool gExpired = dr["IsExpired"] != DBNull.Value && Convert.ToBoolean(dr["IsExpired"]);
                    if (!gTicked && gExpired) continue;   // an unticked EXPIRED machine is a choice, not a miss
                    bool gFlat = dr["IsFlat"] != DBNull.Value && Convert.ToBoolean(dr["IsFlat"]);
                    string why = null;
                    if (!gTicked) why = "not ticked";
                    else if (!gFlat && Dec(dr["CurrentReading"]) <= 0m) why = "no reading keyed";
                    if (why == null) continue;
                    if (problems.Count < 25)
                        problems.Add(S(dr["DebtorCode"]) + "   " + S(dr["ServiceItemNo"]) + "   " +
                                     S(dr["MeterType"]) + "   -   " + why);
                    else { problems.Add("..."); break; }
                }
                if (problems.Count > 0)
                {
                    XtraMessageBox.Show(
                        "Grouped invoicing is ON (one invoice per customer), but these meters of the " +
                        "ticked customers would MISS the invoice:\r\n\r\n" +
                        string.Join("\r\n", problems.ToArray()) + "\r\n\r\n" +
                        "Key the readings / tick the machines first, or change Invoice grouping " +
                        "in Setting to bill per machine.\r\n" +
                        "Nothing was generated.",
                        "Incomplete customer group", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Same protection at CONTRACT level (user request 2026-08-04): a contract set to
            // "Group Services into One Invoice" (Billing Mode G) bills as ONE invoice per contract —
            // if some of its machines are ticked but others would MISS that invoice, abort with the
            // list. Only in FOLLOW mode (DEBTOR mode has the stronger customer-level guard above).
            if (guardOn && grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_FOLLOW && !_scopedGenerate)
            {
                HashSet<string> tickedJobs = new HashSet<string>();
                foreach (DataRow dr in visibleRows)
                    if (dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"])
                        && S(dr["InvoicedDocNo"]).Trim().Length == 0
                        && S(dr["BillingMode"]).Trim().ToUpperInvariant() != "S")
                        tickedJobs.Add(JobKeyOf(dr));
                if (tickedJobs.Count > 0)
                {
                    List<string> problems = new List<string>();
                    foreach (DataRow dr in visibleRows)
                    {
                        if (S(dr["BillingMode"]).Trim().ToUpperInvariant() == "S") continue;
                        if (!tickedJobs.Contains(JobKeyOf(dr))) continue;
                        if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;   // already billed = fine
                        bool cTicked = dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"]);
                        bool cExpired = dr["IsExpired"] != DBNull.Value && Convert.ToBoolean(dr["IsExpired"]);
                        if (!cTicked && cExpired) continue;   // an unticked EXPIRED machine is a choice, not a miss
                        bool cFlat = dr["IsFlat"] != DBNull.Value && Convert.ToBoolean(dr["IsFlat"]);
                        string why = null;
                        if (!cTicked) why = "not ticked";
                        else if (!cFlat && Dec(dr["CurrentReading"]) <= 0m) why = "no reading keyed";
                        if (why == null) continue;
                        if (problems.Count < 25)
                            problems.Add(S(dr["ContractNo"]) + "   " + S(dr["ServiceItemNo"]) + "   " +
                                         S(dr["MeterType"]) + "   -   " + why);
                        else { problems.Add("..."); break; }
                    }
                    if (problems.Count > 0)
                    {
                        XtraMessageBox.Show(
                            "These contracts are set to \"Group Services into One Invoice\", but some of " +
                            "their meters would MISS the contract's invoice:\r\n\r\n" +
                            string.Join("\r\n", problems.ToArray()) + "\r\n\r\n" +
                            "Key the readings / tick the machines first, or set the contract's Billing Mode " +
                            "to \"Separate invoice per service item\".\r\n" +
                            filterNote + "\r\n" + "Nothing was generated.",
                            "Incomplete contract group", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // A MERGED line must be billed whole. Machines put together in Billing Setup print as ONE
            // row -- "2 UNIT x 250.00" -- and that row's quantity, its price and its readings are the
            // members added up. Bill half of it and the customer gets a row that names a group and
            // charges for part of one, with no way to tell from the paper that anything is missing.
            //
            // This guard reads the SAME columns the fold engine reads, so it protects whatever the
            // contract's own screen was set to. The three guards below it are about which INVOICE a
            // machine lands on; this one is about which printed ROW, which is a different promise and
            // was the one nothing checked -- a contract billed per machine could still carry a merge.
            //
            // Rental and copies group separately (a fleet can share one rental and still bill its own
            // copies), so each side is checked against its own column and only against rows of its
            // own kind.
            if (guardOn && !_scopedGenerate)
            {
                HashSet<string> tickedMerges = new HashSet<string>();
                foreach (DataRow dr in visibleRows)
                {
                    if (dr["Sel"] == DBNull.Value || !Convert.ToBoolean(dr["Sel"])) continue;
                    if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;
                    string k = MergeKeyOf(dr);
                    if (k != null) tickedMerges.Add(k);
                }
                if (tickedMerges.Count > 0)
                {
                    List<string> problems = new List<string>();
                    foreach (DataRow dr in visibleRows)
                    {
                        string k = MergeKeyOf(dr);
                        if (k == null || !tickedMerges.Contains(k)) continue;
                        if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;   // already billed = fine
                        bool mTicked = dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"]);
                        bool mExpired = dr["IsExpired"] != DBNull.Value && Convert.ToBoolean(dr["IsExpired"]);
                        if (!mTicked && mExpired) continue;   // an unticked EXPIRED machine is a choice
                        bool mFlat = dr["IsFlat"] != DBNull.Value && Convert.ToBoolean(dr["IsFlat"]);
                        string why = null;
                        if (!mTicked) why = "not ticked";
                        else if (!mFlat && Dec(dr["CurrentReading"]) <= 0m) why = "no reading keyed";
                        if (why == null) continue;
                        if (problems.Count < 25)
                            problems.Add(S(dr["ContractNo"]) + "   " + S(dr["ServiceItemNo"]) + "   " +
                                         S(dr["MeterType"]) + "   (" + k.Substring(k.IndexOf('|') + 1) +
                                         ")   -   " + why);
                        else { problems.Add("..."); break; }
                    }
                    if (problems.Count > 0)
                    {
                        XtraMessageBox.Show(
                            "These machines are merged onto ONE printed row in Billing Setup, so they " +
                            "have to be billed together. Some of them would be left out:" +
                            "\r\n" + "\r\n" +
                            string.Join("\r\n", problems.ToArray()) +
                            "\r\n" + "\r\n" +
                            "Key the readings / tick the machines first, or un-merge them in the " +
                            "contract's Billing Setup." + "\r\n" + "Nothing was generated.",
                            "Incomplete merged row", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // Demo 28/07 #6: bill-group completeness — a Bill Group is billed as ONE invoice, so a
            // partially ticked / partially keyed group must not slip out as a partial invoice. Runs
            // regardless of the grouping setting (the Bill Group overrides it) -- except on a contract
            // billed one invoice per machine, where each machine is its own invoice anyway.
            if (guardOn && !_scopedGenerate)
            {
                HashSet<string> tickedGroups = new HashSet<string>();
                foreach (DataRow dr in visibleRows)
                {
                    if (dr["Sel"] == DBNull.Value || !Convert.ToBoolean(dr["Sel"])) continue;
                    if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;
                    string bg = S(dr["BillGroupCode"]).Trim();
                    if (bg.Length == 0) continue;
                    if (dr["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(dr["IsGroupItem"])) continue;
                    if (S(dr["BillingMode"]).Trim().ToUpperInvariant() == "S") continue;   // per machine: the group is not one invoice
                    tickedGroups.Add(D64(dr["ContractKey"]) + "_" + bg);
                }
                if (tickedGroups.Count > 0)
                {
                    List<string> problems = new List<string>();
                    foreach (DataRow dr in visibleRows)
                    {
                        string bg = S(dr["BillGroupCode"]).Trim();
                        if (bg.Length == 0) continue;
                        if (dr["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(dr["IsGroupItem"])) continue;
                        if (S(dr["BillingMode"]).Trim().ToUpperInvariant() == "S") continue;
                        if (!tickedGroups.Contains(D64(dr["ContractKey"]) + "_" + bg)) continue;
                        if (S(dr["InvoicedDocNo"]).Trim().Length > 0) continue;   // already billed = fine
                        bool gTicked = dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"]);
                        bool gExpired = dr["IsExpired"] != DBNull.Value && Convert.ToBoolean(dr["IsExpired"]);
                        if (!gTicked && gExpired) continue;   // an unticked EXPIRED machine is a choice, not a miss
                        bool gFlat = dr["IsFlat"] != DBNull.Value && Convert.ToBoolean(dr["IsFlat"]);
                        string why = null;
                        if (!gTicked) why = "not ticked";
                        else if (!gFlat && Dec(dr["CurrentReading"]) <= 0m) why = "no reading keyed";
                        if (why == null) continue;
                        if (problems.Count < 25)
                            problems.Add(S(dr["DebtorCode"]) + "   " + S(dr["ServiceItemNo"]) + "   [" + bg + "]   " +
                                         S(dr["MeterType"]) + "   -   " + why);
                        else { problems.Add("..."); break; }
                    }
                    if (problems.Count > 0)
                    {
                        XtraMessageBox.Show(
                            "Bill Group invoicing: these meters belong to a ticked machine's Bill Group " +
                            "but would MISS the group's invoice:\r\n\r\n" +
                            string.Join("\r\n", problems.ToArray()) + "\r\n\r\n" +
                            "Key the readings / tick the machines first — a Bill Group is billed as ONE invoice.\r\n" +
                            "Nothing was generated.",
                            "Incomplete bill group", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // Every figure of money is decided in one place, by one method, for every screen that
            // bills anything. See BuildInvoiceJobs.
            Dictionary<long, string> runSnapshots;
            string blockTitle, blockMessage;
            jobs = ServiceContractPhotocopier.Classes.ScpInvoiceJobs.Build(
                _dbSetting, _dtGrid, visibleRows, _ladders, genYear, genMonth, grpMode,
                out alreadyInvoiced, out runSnapshots, out blockTitle, out blockMessage);
            if (jobs == null)
            {
                XtraMessageBox.Show(blockMessage, blockTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (jobs.Count == 0)
            {
                XtraMessageBox.Show(alreadyInvoiced > 0
                        ? "All selected meters are ALREADY INVOICED for " + monthName + " — nothing to generate."
                        : "No selected meter with a current reading.",
                    "Generate Invoice");
                return;
            }

            List<MeterInvoiceGenerator.InvoiceJob> jobList = new List<MeterInvoiceGenerator.InvoiceJob>(jobs.Values);
            string skippedNote = alreadyInvoiced > 0
                ? "\r\n(" + alreadyInvoiced + " meter(s) skipped — already invoiced for " + monthName + ".)"
                : "";
            // EXPIRED machines in the run (visible only when the "Include expired" setting shows them):
            // never billed silently — the confirm says so in plain words.
            int expiredTicked = 0;
            foreach (DataRow xr in _dtGrid.Rows)
                if (xr["Sel"] != DBNull.Value && Convert.ToBoolean(xr["Sel"])
                    && S(xr["InvoicedDocNo"]).Trim().Length == 0
                    && xr["IsExpired"] != DBNull.Value && Convert.ToBoolean(xr["IsExpired"]))
                    expiredTicked++;
            string expiredNote = expiredTicked > 0
                ? "\r\n\r\n⚠  " + expiredTicked + " ticked row(s) belong to EXPIRED machines (past their expiry month) — " +
                  "continuing bills them anyway."
                : "";
            // Show what will be created, not just how many. Since a contract's format can fold
            // several machines onto one line, "3 invoices" no longer says whether the machines meant
            // to share a line actually did — the preview lists every invoice, its lines and the
            // machines behind each line, computed by the same fold the builder uses.
            using (MeterInvoicePreview_Form pv = new MeterInvoicePreview_Form(
                       _dbSetting, jobList, monthName,
                       ("They are saved automatically — no clicking through each one." + skippedNote + expiredNote)
                           .Replace("\r\n", "  ")))
            {
                if (pv.ShowDialog(this) != DialogResult.OK) return;
                jobList = pv.Approved;      // blocked customers are skipped, the rest still generate
            }
            if (jobList.Count == 0) return;

            // Run the generation on a worker task behind a progress dialog (invoices saved headlessly).
            using (MeterInvoiceGenerateProgress_Form dlg =
                new MeterInvoiceGenerateProgress_Form(_dbSetting, jobList, DateTime.Today, DateTime.Now, genYear, genMonth, runSnapshots))
            {
                dlg.ShowDialog(this);
            }
            LoadData();
        }

        /// <summary>
        /// Turns the rows to be billed into invoice jobs -- the whole of the money decision, and
        /// nothing else.
        ///
        /// <para>Group FOC pools, rental free months, waive targets, committed minimums, group
        /// tier pricing, the fold into printed lines: all of it happens here, and this is the ONLY
        /// place it happens. Any screen that bills something calls this. A second copy of it
        /// elsewhere would be a second set of answers about the same money, and the two would
        /// drift apart the first time a rule changed.</para>
        ///
        /// <para>It shows nothing and asks nothing. Where it must refuse -- two minimums over the
        /// same charges, a strategy that will not evaluate -- it returns null and says why in
        /// <paramref name="blockMessage"/>, leaving the caller to put that in front of somebody.
        /// That is what lets more than one screen call it.</para>
        ///
        /// <para>What it deliberately does NOT do is the completeness guards. Those are about what
        /// the operator ticked on this grid, so they stay with the grid.</para>
        /// </summary>
        
        // WAIVE-TARGET strategy (two-pass): once every selected line is grouped into jobs, sum each
        // CONTRACT's usage (non-flat) charges for this run; rental lines of contracts whose strategy
        // target is met bill RM0 ("RENTAL WAIVED"), PartialPct gives the proportional variant
        // (reach X% of target -> waive X% of the rental). FOC free months take precedence (they
        // already made the rental free). Rentals billed in a run with NO usage lines are NOT waived —
        // the evaluation can only see this run.
        // GROUP FOC LIMIT: the whole group shares ONE free-copy pool (LimitQty) across the contract's
        // covered usage meters (Scope BK/CL/both + optional Service Item set). Each machine has its OWN
        // usage; the pool frees up to LimitQty across the GROUP TOTAL and the excess is charged:
        //   freed = min(LimitQty, total group usage);  group pays for (total usage − freed).
        // The pool REPLACES the per-meter ("normal") FOC for the covered machines (so "group FOC 5000,
        // printed 10000 -> pay 5000" holds — it is the group's total free, not stacked on per-meter FOC).
        // Allocated PROPORTIONALLY by each machine's usage (fair + order-independent); rebate still applies.
        // Runs at generation (cross-machine — can't live in the per-row grid). Flat-rate meters; a machine
        // on a multi-price ladder keeps its ladder's own FOC (don't put those in a group pool).
        
        // COMMITTED MINIMUM ("MIN ...") meters: the master convention puts the committed minimum print charge
        // on its own meter (rate 0, minimum = committed). It should bill only the TOP-UP that brings the item's
        // print charges up to the committed minimum — not the full minimum every month. It is ALWAYS shown on
        // the invoice (top-up 0 when the print charges already meet the minimum) for transparency.
        //   top-up = max(0, committed − sum of the SAME service item's BK/CL print charges).
        // Single machine = the item's own BK/CL; group = a ".C" combine item whose BK/CL already hold the
        // group's combined readings (master .C convention) — the same per-item sum covers both.
        // Runs last (after group-FOC + waive) so the print charges it measures are final.
        // RENTAL-FREE-N acts LIVE and STATELESS: month number = billing period − rental start month + 1;
        // months 1..N bill the rental at RM 0. No countdown to push, decrement or corrupt — re-running,
        // skipping or re-generating a month can never lose track of which months were free.
        // Anchor: the meter's RentalStartDate, else the machine's effective Service Start.
        
        /// <summary>Does this usage line count towards that waive's target?
        ///
        /// <para>A waive is "print this much and the rent is on us". Which copies count is the whole
        /// question, and it has three answers: this machine's, the whole fleet's (a group machine),
        /// or the ones on the same rental line as the waive (CommitScope 'G'). The third is what a
        /// deal agreed over a merged line means, and without it a five-machine line whose members
        /// each print RM 200 against a line target of RM 800 fires nothing -- silently, because a
        /// waive that does not fire prints exactly like a quiet month.</para></summary>


        
        // The per-invoice contract snapshot text: header billing flags + the strategy rules AS OF
        // this run. Stored in zSCP2_ContractSnapshot next to every generated invoice so a later
        // strategy edit can never rewrite what THIS invoice was computed from.
        
        
        
        // Runs a query on a direct connection with an explicit command timeout (AutoCount's
        // GetDataTable uses a short default that the 145k-row meter join can exceed).
        private DataTable QueryWithTimeout(string sql, int seconds)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(_dbSetting.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = seconds;
                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
            return dt;
        }

        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal Dec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
    }
}
