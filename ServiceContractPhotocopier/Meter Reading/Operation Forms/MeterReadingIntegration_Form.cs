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
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraTab;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.MeterReading.Services;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Meter Reading Integration", MenuOrder = 450)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class MeterReadingIntegration_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;
        private DataTable _dtGrid;   // one row PER METER; item columns repeat (merged via AllowCellMerge)
        private XtraTabControl _tabView;
        private XtraTabPage _pageWith;
        private XtraTabPage _pageOnline;
        private XtraTabPage _pageOffline;
        private XtraTabPage _pageNo;
        private XtraTabPage _pageConflict;
        private XtraTabPage _pageAll;
        private LabelControl _lblSummary;
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
        private DevExpress.XtraEditors.CheckEdit _chkPerCssi;
        private static readonly string[] FETCH_MSGS = new string[] {
            "Contacting the meter API...",
            "Fetching online + offline readings...",
            "The server can be slow (~15s) - please hold on...",
            "Still working - matching machines to readings...",
            "Almost there - finishing up..."
        };
        private SimpleButton _btnSetting;

        public MeterReadingIntegration_Form()
        {
            InitializeComponent();
            InitDefaults();
        }

        public MeterReadingIntegration_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            PopulateDayCombo();
            InitApiStatus();
            LoadData();
        }

        public MeterReadingIntegration_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            PopulateDayCombo();
            InitApiStatus();
            LoadData();
        }

        private void InitDefaults()
        {
            this.LblToday.Text = DateTime.Today.ToString("dd/MM/yyyy (ddd)");
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

            this.GridViewMeter.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMeter_CellValueChanged);
            this.GridViewMeter.CellMerge +=
                new DevExpress.XtraGrid.Views.Grid.CellMergeEventHandler(GridViewMeter_CellMerge);

            SetupTabs();
        }

        // Two views over the SAME in-memory data: switching tabs only re-filters by Status
        // (data is never reloaded/erased until Refresh or Fetch). One grid, re-parented per tab.
        private void SetupTabs()
        {
            _tabView = new XtraTabControl();
            _tabView.Dock = DockStyle.Fill;
            _pageWith = new XtraTabPage(); _pageWith.Text = "With Meter Data";
            _pageOnline = new XtraTabPage(); _pageOnline.Text = "Online";
            _pageOffline = new XtraTabPage(); _pageOffline.Text = "Offline";
            _pageNo = new XtraTabPage(); _pageNo.Text = "No API Data";
            _pageConflict = new XtraTabPage(); _pageConflict.Text = "Conflicts";
            _pageAll = new XtraTabPage(); _pageAll.Text = "All";
            _tabView.TabPages.AddRange(new XtraTabPage[] { _pageWith, _pageOnline, _pageOffline, _pageNo, _pageConflict, _pageAll });

            this.Controls.Remove(this.GridMeter);
            this.GridMeter.Dock = DockStyle.Fill;
            _pageAll.Controls.Add(this.GridMeter);
            _tabView.Dock = DockStyle.Fill;
            this.Controls.Add(_tabView);
            // A Dock=Fill control must sit at child-index 0 (same slot the designer used for
            // GridMeter) so it is laid out AFTER the docked-Top panels and only fills the area
            // left below them. Otherwise it covers the whole form and hides its own tab strip +
            // the grid's column headers behind the title/filter panels.
            this.Controls.SetChildIndex(_tabView, 0);
            _tabView.SelectedTabPage = _pageAll;
            _tabView.SelectedPageChanged += new TabPageChangedEventHandler(TabView_SelectedPageChanged);

            // Per-item alternating row colour (super-light-blue / white), so each service item's
            // BK+CL pair is easy to tell apart at a glance. Conflicts override to light red.
            this.GridViewMeter.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewMeter_RowStyle);
            // Current Reading is read-only in the grid (manual key-in removed); values come from Fetch.
            this.GridViewMeter.ShowingEditor +=
                new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditor);
            // Double-click a contract → open its detail / override form.
            this.GridViewMeter.DoubleClick += new EventHandler(GridViewMeter_DoubleClick);

            // Setting button (created in code to avoid touching the strict designer).
            _btnSetting = new SimpleButton();
            _btnSetting.Text = "Setting";
            _btnSetting.Location = new Point(1085, 46);
            _btnSetting.Size = new Size(120, 52);
            _btnSetting.Click += new EventHandler(BtnSetting_Click);
            this.PanelFilter.Controls.Add(_btnSetting);
            _btnSetting.BringToFront();

            // Invoice grouping choice (overrides each contract's stored BillingMode when generating):
            //   ticked  = one invoice per CSSI (service item)
            //   unticked= one invoice per whole contract
            _chkPerCssi = new DevExpress.XtraEditors.CheckEdit();
            _chkPerCssi.Properties.Caption = "Separate invoice per CSSI (else whole contract)";
            _chkPerCssi.Location = new Point(899, 100);
            _chkPerCssi.Size = new Size(300, 20);
            _chkPerCssi.Checked = false;
            this.PanelFilter.Controls.Add(_chkPerCssi);
            _chkPerCssi.BringToFront();

            // Summary label sitting to the right of the action buttons.
            _lblSummary = new LabelControl();
            _lblSummary.Name = "LblSummary";
            _lblSummary.AutoSizeMode = LabelAutoSizeMode.None;
            _lblSummary.Appearance.Font = new Font("Segoe UI", 9F);
            _lblSummary.Appearance.Options.UseFont = true;
            _lblSummary.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            _lblSummary.Location = new Point(1360, 14);
            _lblSummary.Size = new Size(470, 118);
            _lblSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.PanelFilter.Controls.Add(_lblSummary);
            _lblSummary.BringToFront();

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
            _chkInclude0Usage.Properties.Caption = "Include 0 Meter Usage";
            _chkInclude0Usage.Location = new Point(272, 92);
            _chkInclude0Usage.Size = new Size(200, 20);
            _chkInclude0Usage.Checked = true;
            _chkInclude0Usage.CheckedChanged += new EventHandler(ChkInclude0Usage_CheckedChanged);
            this.GrpFilter.Controls.Add(_chkInclude0Usage);
            _chkInclude0Usage.BringToFront();
        }

        // Double-click any row → open the detail / override form for that whole contract.
        private void GridViewMeter_DoubleClick(object sender, EventArgs e)
        {
            DataRow r = GridViewMeter.GetDataRow(GridViewMeter.FocusedRowHandle);
            if (r == null) return;
            OpenContractDetail(D64(r["ContractKey"]), S(r["ContractNo"]), S(r["Customer"]));
        }

        // Each service item gets one of two background shades (white / super-light-blue), alternating
        // per item (NOT per physical row), so a meter pair stays the same colour.
        private void GridViewMeter_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;   // group rows
            object cf = GridViewMeter.GetRowCellValue(e.RowHandle, "HasConflict");
            if (cf != null && cf != DBNull.Value && Convert.ToBoolean(cf))
            {
                Color rc = Color.FromArgb(255, 224, 224);   // light red = unresolved conflict
                e.Appearance.BackColor = rc; e.Appearance.BackColor2 = rc;  // flat (no gradient)
                e.Appearance.Options.UseBackColor = true;
                return;
            }
            object v = GridViewMeter.GetRowCellValue(e.RowHandle, "Shade");
            if (v != null && v != DBNull.Value && Convert.ToInt32(v) == 1)
            {
                Color bc = Color.FromArgb(228, 241, 252);   // super light blue
                e.Appearance.BackColor = bc; e.Appearance.BackColor2 = bc;  // flat (no gradient)
                e.Appearance.Options.UseBackColor = true;
            }
        }

        private void TabView_SelectedPageChanged(object sender, TabPageChangedEventArgs e)
        {
            if (e.Page == null) return;
            this.GridMeter.Parent = e.Page;
            this.GridMeter.Dock = DockStyle.Fill;
            ApplyTabFilter();
        }

        private void ApplyTabFilter()
        {
            if (_tabView == null) return;
            string f = "";
            if (_tabView.SelectedTabPage == _pageWith) f = "[CurrentReading] > 0 OR [FetchedReading] > 0";
            else if (_tabView.SelectedTabPage == _pageOnline) f = "[MachineStatus] = 'ONLINE'";
            else if (_tabView.SelectedTabPage == _pageOffline) f = "[MachineStatus] = 'OFFLINE'";
            else if (_tabView.SelectedTabPage == _pageNo) f = "[Status] = 'No API data'";
            else if (_tabView.SelectedTabPage == _pageConflict) f = "[HasConflict] = True";

            // Hide zero-usage meters unless "Include 0 Meter Usage" is ticked.
            if (_chkInclude0Usage != null && !_chkInclude0Usage.Checked)
            {
                string usage = "[MeterUsage] <> 0";
                f = string.IsNullOrEmpty(f) ? usage : "(" + f + ") AND " + usage;
            }
            GridViewMeter.ActiveFilterString = f;
        }

        private void ChkInclude0Usage_CheckedChanged(object sender, EventArgs e) { ApplyTabFilter(); }

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
            System.Collections.Generic.HashSet<string> contracts = new System.Collections.Generic.HashSet<string>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                string itemKey = S(r["ItemKey"]);
                allItems.Add(itemKey);
                contracts.Add(S(r["ContractKey"]));
                string ms = S(r["MachineStatus"]);
                if (ms == "ONLINE") { onlineItems.Add(itemKey); withItems.Add(itemKey); }
                else if (ms == "OFFLINE") { offlineItems.Add(itemKey); withItems.Add(itemKey); }
                if (Dec(r["CurrentReading"]) > 0m || Dec(r["FetchedReading"]) > 0m) withItems.Add(itemKey);
                if (r["HasConflict"] != DBNull.Value && Convert.ToBoolean(r["HasConflict"])) conflictItems.Add(itemKey);
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                { sel++; selCharge += Dec(r["TotalCharges"]); }
            }
            int noItems = allItems.Count - withItems.Count;   // machines with no reading at all
            _pageWith.Text = "With Meter Data (" + withItems.Count + ")";
            _pageOnline.Text = "Online (" + onlineItems.Count + ")";
            _pageOffline.Text = "Offline (" + offlineItems.Count + ")";
            _pageNo.Text = "No API Data (" + noItems + ")";
            _pageConflict.Text = "Conflicts (" + conflictItems.Count + ")";
            _pageAll.Text = "All (" + allItems.Count + ")";

            if (_lblSummary != null)
            {
                string fetched = (withItems.Count == 0 && noItems == 0) ? "  (not fetched yet)" : "";
                _lblSummary.Text =
                    "Contracts: " + contracts.Count + "    Service Items: " + allItems.Count +
                    "    Meters: " + _dtGrid.Rows.Count + "\r\n" +
                    "✔ With meter data: " + withItems.Count + fetched + "   (Online: " + onlineItems.Count + " / Offline: " + offlineItems.Count + ") machines\r\n" +
                    "✖ No API data (need manual key-in): " + noItems + (conflictItems.Count > 0 ? "      ⚠ Conflicts: " + conflictItems.Count : "") + "\r\n" +
                    "Selected to bill: " + sel + " meter(s)    Billing total: RM " + selCharge.ToString("n2");
            }
        }

        // Visual grouping WITHOUT real DataView grouping (which is incompatible with AllowCellMerge):
        //   • Contract / Customer / Mode  → merge across the whole CONTRACT (one cell per contract).
        //   • Service Item No / Serial     → merge across each SERVICE ITEM (one cell per item).
        //   • Every meter column keeps its own value (no merge).
        private void GridViewMeter_CellMerge(object sender, DevExpress.XtraGrid.Views.Grid.CellMergeEventArgs e)
        {
            string f = e.Column.FieldName;
            bool perContract = (f == "ContractNo" || f == "Customer" || f == "Mode");
            bool perItem = (f == "ServiceItemNo" || f == "SerialNo" || f == "MachineStatus");
            if (!perContract && !perItem) { e.Merge = false; e.Handled = true; return; }
            string keyField = perContract ? "ContractKey" : "ItemKey";
            object k1 = GridViewMeter.GetRowCellValue(e.RowHandle1, keyField);
            object k2 = GridViewMeter.GetRowCellValue(e.RowHandle2, keyField);
            e.Merge = (k1 != null && k2 != null && k1.ToString() == k2.ToString());
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

        // Fill the Day combo with only the billing days actually in use (distinct effective billing
        // day = COALESCE(item override, contract day) across active items), instead of a fixed 1-31
        // list. Called after _dbSetting is set (InitDefaults runs before that, so it seeds 1-31).
        private void PopulateDayCombo()
        {
            if (_dbSetting == null) return;
            int prev = SelectedDay();
            try
            {
                string sql = "SELECT DISTINCT COALESCE(i.BillingDayOverride, c.BillingDay) AS d " +
                    "FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "WHERE i.Inactive='N' AND c.Inactive='N' " +
                    "AND COALESCE(i.BillingDayOverride, c.BillingDay) BETWEEN 1 AND 31 ORDER BY d";
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
            if (idx < 0) idx = this.CmbDay.Properties.Items.IndexOf(DateTime.Today.Day);
            if (idx < 0 && this.CmbDay.Properties.Items.Count > 0) idx = 0;
            _suppressFilterEvent = true;
            this.CmbDay.SelectedIndex = idx;
            _suppressFilterEvent = false;
        }

        // ───────────────────── Load (one row per meter) ─────────────────────

        private void LoadData()
        {
            if (_dbSetting == null) return;
            try
            {
                int month = SelectedMonth();
                int year = DateTime.Today.Year;
                int target = SelectedDay();
                int monthEnd = DateTime.DaysInMonth(year, month);
                if (target > monthEnd) target = monthEnd;

                string dayFilter = this.ChkShowAll.Checked ? "" :
                    " AND (CASE WHEN COALESCE(i.BillingDayOverride,c.BillingDay) > " + monthEnd +
                    " THEN " + monthEnd + " ELSE COALESCE(i.BillingDayOverride,c.BillingDay) END) = " + target + " ";

                string search = (this.TxtSearch.EditValue ?? "").ToString().Trim();
                string searchFilter = "";
                if (search.Length > 0)
                {
                    string s = search.Replace("'", "''");
                    searchFilter = " AND (i.ServiceItemNo LIKE '%" + s + "%' OR i.SerialNumber LIKE '%" + s +
                        "%' OR c.ContractNo LIKE '%" + s + "%' OR ISNULL(d.CompanyName,'') LIKE '%" + s + "%') ";
                }

                // Expired service items are hidden unless the user opts in (Meter Reading > Setting).
                bool includeExpired = ServiceContractPhotocopier.Data.PumsConfig.GetBool(
                    _dbSetting, ServiceContractPhotocopier.Data.PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
                string expiryFilter = includeExpired ? "" :
                    " AND (i.ServiceExpiryDate IS NULL OR i.ServiceExpiryDate >= DATEFROMPARTS(" + year + "," + month + ",1)) ";

                string sql =
                    "SELECT i.ItemKey, c.ContractKey, c.ContractNo, i.ServiceItemNo, i.SerialNumber, " +
                    "c.DebtorCode, ISNULL(d.CompanyName,'') AS DebtorName, c.BillingMode, " +
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS EffBillingDay, " +
                    "m.ItemMeterKey, m.MeterRole, m.MeterTypeCode, ISNULL(mt.Description,'') AS MeterTypeName, " +
                    "ISNULL(mt.ACItemCode,'') AS ACItemCode, ISNULL(m.MinimumCharges,0) AS MinCharges, " +
                    "ISNULL(m.ChargesRate,0) AS UnitPrice, ISNULL(m.FOCQty,0) AS FOCQty, " +
                    "ISNULL(m.RebateQtyInPercent,0) AS RebatePct, ISNULL(m.InitialReading,0) AS InitReading, " +
                    "lr.LastReading, lr.LastDate " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                    // Latest reading per meter in ONE pass over zSCP_MeterTrans (window function),
                    // instead of a correlated TOP-1 OUTER APPLY per row (which timed out on 145k rows).
                    "LEFT JOIN (SELECT z.ServiceItemMeterTypeKey, z.MeterTransReading AS LastReading, z.MeterTransDate AS LastDate " +
                    "  FROM (SELECT t.ServiceItemMeterTypeKey, t.MeterTransReading, t.MeterTransDate, " +
                    "        ROW_NUMBER() OVER (PARTITION BY t.ServiceItemMeterTypeKey ORDER BY t.MeterTransDate DESC, t.MeterTransKey DESC) AS rn " +
                    // Last reading = latest MeterTrans STRICTLY BEFORE the selected billing month, so
                    // the baseline is the previous period's reading (never the current/future month).
                    "        FROM dbo.zSCP_MeterTrans t WHERE t.MeterTransDate < DATEFROMPARTS(" + year + "," + month + ",1)) z WHERE z.rn = 1) lr ON lr.ServiceItemMeterTypeKey = m.ItemMeterKey " +
                    "WHERE m.MeterRole IN ('BK','CL') AND i.Inactive='N' AND c.Inactive='N' " + dayFilter + searchFilter + expiryFilter +
                    "ORDER BY c.ContractNo, i.ServiceItemNo, m.MeterRole";
                DataTable src = QueryWithTimeout(sql, 180);

                _dtGrid = NewGridTable();
                Dictionary<long, int> shadeByContract = new Dictionary<long, int>();
                foreach (DataRow r in src.Rows)
                {
                    DataRow g = _dtGrid.NewRow();
                    long ck = D64(r["ContractKey"]);
                    int shade;
                    if (!shadeByContract.TryGetValue(ck, out shade))
                    {
                        shade = shadeByContract.Count % 2;   // alternate per contract → clean colour bands
                        shadeByContract[ck] = shade;
                    }
                    g["Shade"] = shade;
                    g["ContractNo"] = S(r["ContractNo"]);
                    g["ServiceItemNo"] = S(r["ServiceItemNo"]);
                    g["SerialNo"] = S(r["SerialNumber"]);
                    g["Customer"] = S(r["DebtorCode"]) + " - " + S(r["DebtorName"]);
                    g["Mode"] = S(r["BillingMode"]) == "S" ? "Separate" : "Group";
                    if (r["EffBillingDay"] != DBNull.Value) g["BillingDay"] = Convert.ToInt32(r["EffBillingDay"]);
                    g["MeterType"] = S(r["MeterTypeCode"]);
                    g["MeterTypeName"] = S(r["MeterTypeName"]);
                    g["MinCharges"] = Dec(r["MinCharges"]);
                    g["UnitPrice"] = Dec(r["UnitPrice"]);
                    g["FOCQty"] = Dec(r["FOCQty"]);
                    g["RebatePct"] = Dec(r["RebatePct"]);
                    if (r["LastDate"] != DBNull.Value) g["LastReadDate"] = Convert.ToDateTime(r["LastDate"]);
                    g["LastReading"] = (r["LastReading"] == DBNull.Value) ? Dec(r["InitReading"]) : Dec(r["LastReading"]);
                    g["CurrentReading"] = 0m;
                    g["MeterUsage"] = 0m;
                    g["TotalCharges"] = 0m;
                    g["UseMin"] = (Dec(r["UnitPrice"]) == 0m && Dec(r["MinCharges"]) > 0m);
                    g["Sel"] = false;
                    g["Status"] = "";
                    g["ItemKey"] = D64(r["ItemKey"]);
                    g["ContractKey"] = D64(r["ContractKey"]);
                    g["DebtorCode"] = S(r["DebtorCode"]);
                    g["BillingMode"] = S(r["BillingMode"]);
                    g["ItemMeterKey"] = D64(r["ItemMeterKey"]);
                    g["ACItemCode"] = S(r["ACItemCode"]);
                    g["Role"] = S(r["MeterRole"]);
                    g["EntrySource"] = "";
                    g["FetchedReading"] = 0m;
                    g["HasConflict"] = false;
                    _dtGrid.Rows.Add(g);
                }

                PrefillFromStaging(SelectedMonth(), DateTime.Today.Year);

                GridMeter.DataSource = _dtGrid;
                ConfigureGrid();
                UpdateTabCounts();
                ApplyTabFilter();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable NewGridTable()
        {
            DataTable dt = new DataTable();
            // ---- visible: item columns (these merge), then meter columns ----
            dt.Columns.Add("ContractNo", typeof(string));
            dt.Columns.Add("ServiceItemNo", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("MachineStatus", typeof(string));   // ONLINE / OFFLINE (set on fetch, per item)
            dt.Columns.Add("Customer", typeof(string));
            dt.Columns.Add("Mode", typeof(string));
            dt.Columns.Add("BillingDay", typeof(int));   // effective billing day = COALESCE(item override, contract day)
            dt.Columns.Add("MeterType", typeof(string));
            dt.Columns.Add("MeterTypeName", typeof(string));
            dt.Columns.Add("MinCharges", typeof(decimal));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("RebatePct", typeof(decimal));
            dt.Columns.Add("LastReadDate", typeof(DateTime));
            dt.Columns.Add("LastAuditDate", typeof(DateTime));   // API audit date of the CURRENT fetched reading
            dt.Columns.Add("LastReading", typeof(decimal));
            dt.Columns.Add("CurrentReading", typeof(decimal));
            dt.Columns.Add("MeterUsage", typeof(decimal));
            dt.Columns.Add("TotalCharges", typeof(decimal));
            dt.Columns.Add("UseMin", typeof(bool));
            dt.Columns.Add("Sel", typeof(bool));
            dt.Columns.Add("Status", typeof(string));
            // ---- hidden keys ----
            dt.Columns.Add("ItemKey", typeof(long));
            dt.Columns.Add("ContractKey", typeof(long));
            dt.Columns.Add("DebtorCode", typeof(string));
            dt.Columns.Add("BillingMode", typeof(string));
            dt.Columns.Add("ItemMeterKey", typeof(long));
            dt.Columns.Add("ACItemCode", typeof(string));
            dt.Columns.Add("Role", typeof(string));
            dt.Columns.Add("Shade", typeof(int));   // 0/1 per-item zebra shade (hidden)
            dt.Columns.Add("EntrySource", typeof(string));    // where CurrentReading came from: MANUAL/ONLINE/OFFLINE/'' (hidden)
            dt.Columns.Add("FetchedReading", typeof(decimal)); // last value the API returned for this meter (hidden)
            dt.Columns.Add("HasConflict", typeof(bool));       // saved-manual value differs from a fresh API value (hidden)
            return dt;
        }

        private void ConfigureGrid()
        {
            GridViewMeter.OptionsBehavior.Editable = true;
            GridViewMeter.OptionsView.AllowCellMerge = true;   // merge contract/item columns (no expand). NOTE: incompatible with real grouping.
            GridViewMeter.OptionsView.ShowGroupPanel = false;  // grouping is simulated via cell-merge; drag-panel would be non-functional
            GridViewMeter.OptionsView.ShowFooter = true;       // footer band for totals
            GridViewMeter.OptionsView.ColumnAutoWidth = false;

            foreach (string h in new string[] { "ItemKey", "ContractKey", "DebtorCode", "BillingMode", "ItemMeterKey", "ACItemCode", "Role", "Shade" })
                if (GridViewMeter.Columns[h] != null) GridViewMeter.Columns[h].Visible = false;

            SetCol("ContractNo", "Contract", 110, false);
            SetCol("ServiceItemNo", "Service Item No", 120, false);
            SetCol("SerialNo", "Serial", 110, false);
            SetCol("MachineStatus", "Machine Status", 95, false);
            SetCol("Customer", "Customer", 220, false);
            SetCol("Mode", "Mode", 70, false);
            SetCol("BillingDay", "Billing Day", 80, false);
            SetCol("MeterType", "Meter Type", 130, false);
            SetCol("MeterTypeName", "Meter Type Name", 170, false);
            SetNum("MinCharges", "Min. Charges", 85, false, "n2");
            SetNum("UnitPrice", "Unit Price", 85, false, "n4");
            SetNum("FOCQty", "FOC Qty", 70, false, "n0");
            SetNum("RebatePct", "Rebate (%)", 80, false, "n2");
            SetCol("LastReadDate", "Last Read Date", 100, false);
            SetCol("LastAuditDate", "Last Audit Date", 110, false);
            SetNum("LastReading", "Last Reading", 95, false, "n0");
            SetNum("CurrentReading", "Current Reading", 100, true, "n0");
            SetNum("MeterUsage", "Meter Usage", 95, false, "n0");
            SetNum("TotalCharges", "Total Charges", 95, false, "n2");
            SetCol("UseMin", "Use Min.", 70, true);
            SetCol("Sel", "Selected", 65, true);
            SetCol("Status", "Status", 130, false);

            // Freeze the identifier columns on the left so they stay visible when scrolling horizontally.
            // (Requires ColumnAutoWidth = false, set above.)
            foreach (string fx in new string[] { "ContractNo", "ServiceItemNo", "SerialNo" })
                if (GridViewMeter.Columns[fx] != null)
                    GridViewMeter.Columns[fx].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;

            // Footer totals.
            GridColumn cChg = GridViewMeter.Columns["TotalCharges"];
            if (cChg != null)
            {
                cChg.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                cChg.SummaryItem.DisplayFormat = "Σ {0:n2}";
            }
            GridColumn cUse = GridViewMeter.Columns["MeterUsage"];
            if (cUse != null)
            {
                cUse.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                cUse.SummaryItem.DisplayFormat = "Σ {0:n0}";
            }
            GridColumn cSt = GridViewMeter.Columns["Status"];
            if (cSt != null)
            {
                cSt.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Count;
                cSt.SummaryItem.DisplayFormat = "{0} meters";
            }
        }

        private void SetCol(string field, string caption, int width, bool editable)
        {
            GridColumn c = GridViewMeter.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width;
            c.OptionsColumn.AllowEdit = editable; c.OptionsColumn.ReadOnly = !editable;
        }
        private void SetNum(string field, string caption, int width, bool editable, string fmt)
        {
            SetCol(field, caption, width, editable);
            GridColumn c = GridViewMeter.Columns[field];
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
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            int month = SelectedMonth();
            int day = SelectedDay();
            int year = DateTime.Today.Year;
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
                    IMeterReadingApiClient client = MeterReadingApiClientFactory.Create(_dbSetting);
                    // Fetch both endpoints CONCURRENTLY so the total wait is max(online, offline), not
                    // their sum — each can take ~15s server-side. GetReadings is stateless (a fresh
                    // HttpClient per call), so sharing one client across the two tasks is safe.
                    System.Threading.Tasks.Task tOn = System.Threading.Tasks.Task.Run(() =>
                        { try { onlineList = client.GetReadings(MachineStatus.Online, month); } catch (Exception ex) { onlineErr = ex.Message; } });
                    System.Threading.Tasks.Task tOff = System.Threading.Tasks.Task.Run(() =>
                        { try { offlineList = client.GetReadings(MachineStatus.Offline, month); } catch (Exception ex) { offlineErr = ex.Message; } });
                    System.Threading.Tasks.Task.WaitAll(tOn, tOff);
                });

                // Merge: online wins over offline for the same code. Each DTO carries its endpoint.
                Dictionary<string, MeterReadingDto> byCode = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
                if (onlineList != null)
                    foreach (MeterReadingDto d in onlineList)
                        if (!string.IsNullOrWhiteSpace(d.Code) && QualifiesByDate(d, month, day)) byCode[d.Code.Trim()] = d;
                if (offlineList != null)
                    foreach (MeterReadingDto d in offlineList)
                        if (!string.IsNullOrWhiteSpace(d.Code) && QualifiesByDate(d, month, day) && !byCode.ContainsKey(d.Code.Trim()))
                            byCode[d.Code.Trim()] = d;

                int matchedMeters = 0;
                int onlineMeters = 0, offlineMeters = 0, conflicts = 0;
                System.Collections.Generic.HashSet<string> matchedItems = new System.Collections.Generic.HashSet<string>();
                foreach (DataRow r in _dtGrid.Rows)
                {
                    string code = S(r["ServiceItemNo"]).Trim();
                    MeterReadingDto dto;
                    if (code.Length > 0 && byCode.TryGetValue(code, out dto))
                    {
                        bool isOnline = dto.Status == MachineStatus.Online;
                        decimal apiVal = S(r["Role"]) == "CL" ? dto.TotalCL : dto.TotalBK;
                        if (!string.IsNullOrWhiteSpace(dto.SerialNumber)) r["SerialNo"] = dto.SerialNumber.Trim();
                        r["MachineStatus"] = isOnline ? "ONLINE" : "OFFLINE";
                        r["FetchedReading"] = apiVal;
                        if (dto.LastAuditDate.HasValue) r["LastAuditDate"] = dto.LastAuditDate.Value;

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
                            r["Sel"] = true;
                            r["Status"] = "Matched (" + (isOnline ? "Online" : "Offline") + ")  " +
                                (dto.LastAuditDate.HasValue ? dto.LastAuditDate.Value.ToString("dd/MM/yyyy") : "");
                            toStage.Add(new StageRow(D64(r["ItemMeterKey"]), apiVal, dto.LastAuditDate, isOnline ? "ONLINE" : "OFFLINE"));
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

                GridMeter.RefreshDataSource();
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
                    " meter(s) matched with last audit date on/before " + day + " " +
                    new CultureInfo("en-US").DateTimeFormat.GetMonthName(month) + ".\r\n" +
                    "   • Online machines: " + onlineMeters + " meter(s)\r\n" +
                    "   • Offline machines: " + offlineMeters + " meter(s)\r\n" +
                    (conflicts > 0 ? "   ⚠ " + conflicts + " conflict(s) with saved manual readings — see the Conflicts tab, double-click a contract to resolve.\r\n" : "") +
                    errNote +
                    "Readings saved — next time just reopen this screen (no need to Fetch again unless you want fresh data).\r\n" +
                    "Use 'Select Matched' then 'Generate Invoice'.",
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

        // A reading qualifies when its Last Audit Date is in the selected month AND on/before the
        // selected day (e.g. April + 16 → readings dated 01–16 April; 17/04, 20/04 are excluded).
        private static bool QualifiesByDate(MeterReadingDto d, int month, int day)
        {
            if (!d.LastAuditDate.HasValue) return false;
            DateTime la = d.LastAuditDate.Value;
            return la.Month == month && la.Day <= day;
        }

        // "Select Matched" — tick every meter that got a current reading.
        private void BtnSelfManualKeyIn_Click(object sender, EventArgs e)
        {
            if (_dtGrid == null) return;
            GridViewMeter.CloseEditor();
            int n = 0;
            foreach (DataRow r in _dtGrid.Rows)
            {
                bool ok = Dec(r["CurrentReading"]) > 0m;
                r["Sel"] = ok; if (ok) n++;
            }
            GridMeter.RefreshDataSource();
            UpdateTabCounts();
            XtraMessageBox.Show(n + " meter(s) selected. Click Generate Invoice.", "Select Matched",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ───────────────────── Manual key-in + staging (zSCP2_MeterEntry) ─────────────────────

        // Reload any saved current readings for this period back into the grid, so manual key-ins
        // (and previously accepted API overrides) survive a restart and reappear as "saved".
        private void PrefillFromStaging(int month, int year)
        {
            if (_dtGrid == null) return;
            Dictionary<long, DataRow> byMeter = new Dictionary<long, DataRow>();
            string sql = "SELECT ItemMeterKey, CurrentReading, ReadingDate, Source FROM dbo.zSCP2_MeterEntry " +
                         "WHERE PeriodYear=" + year + " AND PeriodMonth=" + month;
            DataTable saved = QueryWithTimeout(sql, 60);
            foreach (DataRow s in saved.Rows) byMeter[D64(s["ItemMeterKey"])] = s;
            if (byMeter.Count == 0) return;

            foreach (DataRow r in _dtGrid.Rows)
            {
                DataRow s;
                if (!byMeter.TryGetValue(D64(r["ItemMeterKey"]), out s)) continue;
                string src = S(s["Source"]).Trim().ToUpperInvariant();
                r["CurrentReading"] = Dec(s["CurrentReading"]);
                r["EntrySource"] = src;
                if (s["ReadingDate"] != DBNull.Value) r["LastAuditDate"] = Convert.ToDateTime(s["ReadingDate"]);
                r["Sel"] = true;
                Recalc(r);
                string dt = (s["ReadingDate"] != DBNull.Value) ? "  " + Convert.ToDateTime(s["ReadingDate"]).ToString("dd/MM/yyyy") : "";
                if (src == "ONLINE") { r["MachineStatus"] = "ONLINE"; r["Status"] = "Online (saved)" + dt; }
                else if (src == "OFFLINE") { r["MachineStatus"] = "OFFLINE"; r["Status"] = "Offline (saved)" + dt; }
                else { r["Status"] = "Manual (saved)" + dt; }
            }
        }

        // One fetched reading queued for staging (persisted so a reopen shows it without re-fetching).
        private class StageRow
        {
            public long ItemMeterKey;
            public decimal Reading;
            public DateTime? Date;
            public string Source;
            public StageRow(long k, decimal reading, DateTime? d, string s)
            { ItemMeterKey = k; Reading = reading; Date = d; Source = s; }
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
                            UpsertStaging(cn, tx, sr.ItemMeterKey, year, month, sr.Reading, sr.Date, sr.Source);
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
            using (MeterReadingSetting_Form dlg = new MeterReadingSetting_Form(_dbSetting))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadData();
            }
        }

        // Insert-or-update one staged reading (unique per ItemMeterKey + period).
        private void UpsertStaging(SqlConnection cn, SqlTransaction tx, long itemMeterKey, int year, int month,
            decimal reading, DateTime? readingDate, string source)
        {
            SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, ReadingDate=@dt, Source=@src, LastModified=GETDATE() " +
                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo; " +
                "IF @@ROWCOUNT=0 INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source) " +
                "VALUES (@imk,@yr,@mo,@rd,@dt,@src);", cn, tx);
            cmd.Parameters.AddWithValue("@imk", itemMeterKey);
            cmd.Parameters.AddWithValue("@yr", year);
            cmd.Parameters.AddWithValue("@mo", month);
            cmd.Parameters.AddWithValue("@rd", reading);
            cmd.Parameters.AddWithValue("@dt", (object)readingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@src", source);
            cmd.ExecuteNonQuery();
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
        }

        // Block editing the Current Reading unless manual mode is on (and never on an API-sourced row —
        // those are changed only through the contract detail / override form).
        private void GridViewMeter_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewMeter.FocusedColumn != null && GridViewMeter.FocusedColumn.FieldName == "CurrentReading")
            {
                e.Cancel = true;   // Current Reading is read-only (manual key-in removed)
            }
        }

        // Open the per-contract detail / override dialog. The dialog shows every meter of the contract
        // with its saved/manual Current vs the freshly-Fetched value; the user ticks "Accept fetched"
        // to override (typically to resolve a conflict). On OK we persist the accepted values to
        // staging (source ONLINE/OFFLINE) and update the grid in place.
        private void OpenContractDetail(long contractKey, string contractNo, string customer)
        {
            if (_dtGrid == null) return;
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow r in _dtGrid.Rows)
                if (D64(r["ContractKey"]) == contractKey) rows.Add(r);
            if (rows.Count == 0) return;

            DataTable dt = new DataTable();
            dt.Columns.Add("ServiceItemNo", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("MeterType", typeof(string));
            dt.Columns.Add("Role", typeof(string));
            dt.Columns.Add("LastReading", typeof(decimal));
            dt.Columns.Add("CurrentReading", typeof(decimal));
            dt.Columns.Add("FetchedReading", typeof(decimal));
            dt.Columns.Add("Source", typeof(string));
            dt.Columns.Add("HasConflict", typeof(bool));
            dt.Columns.Add("AcceptFetched", typeof(bool));
            dt.Columns.Add("ItemMeterKey", typeof(long));
            foreach (DataRow r in rows)
            {
                DataRow d = dt.NewRow();
                d["ServiceItemNo"] = S(r["ServiceItemNo"]);
                d["SerialNo"] = S(r["SerialNo"]);
                d["MeterType"] = S(r["MeterType"]);
                d["Role"] = S(r["Role"]) == "CL" ? "Colour (CL)" : "Black (BK)";
                d["LastReading"] = Dec(r["LastReading"]);
                d["CurrentReading"] = Dec(r["CurrentReading"]);
                d["FetchedReading"] = Dec(r["FetchedReading"]);
                d["Source"] = S(r["EntrySource"]);
                bool conf = r["HasConflict"] != DBNull.Value && Convert.ToBoolean(r["HasConflict"]);
                d["HasConflict"] = conf;
                d["AcceptFetched"] = conf;   // pre-tick conflicts
                d["ItemMeterKey"] = D64(r["ItemMeterKey"]);
                dt.Rows.Add(d);
            }

            using (MeterReadingDetail_Form f = new MeterReadingDetail_Form(contractNo, customer, dt))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
            }

            int year = DateTime.Today.Year, month = SelectedMonth(), applied = 0;
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
                                UpsertStaging(cn, tx, imk, year, month, fv, DateTime.Now, src);
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
                                UpsertStaging(cn, tx, imk, year, month, cv, DateTime.Now, "MANUAL");
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
            GridMeter.RefreshDataSource();
            UpdateTabCounts();
            ApplyTabFilter();
            if (applied > 0)
                XtraMessageBox.Show(applied + " reading(s) overridden with the fetched value.", "Override",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GridViewMeter_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMin")
            {
                DataRow r = GridViewMeter.GetDataRow(e.RowHandle);
                if (r != null) Recalc(r);
            }
            if (e.Column.FieldName == "Sel" || e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMin")
                UpdateTabCounts();   // keep the billing-total summary live
        }

        private void Recalc(DataRow r)
        {
            decimal cur = Dec(r["CurrentReading"]);
            decimal last = Dec(r["LastReading"]);
            decimal usage = cur - last; if (usage < 0m) usage = 0m;
            decimal billable = usage - Dec(r["FOCQty"]); if (billable < 0m) billable = 0m;
            decimal rebate = Dec(r["RebatePct"]); if (rebate > 0m) billable = billable * (1m - rebate / 100m);
            decimal rate = Dec(r["UnitPrice"]); decimal min = Dec(r["MinCharges"]);
            bool useMin = r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"]);
            decimal charge = useMin ? min : (billable * rate < min ? min : billable * rate);
            r["MeterUsage"] = usage;
            r["TotalCharges"] = charge;
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _dtGrid == null) return;
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            // Group the selected meter lines: Group mode = one invoice per contract; Separate = per item.
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs =
                new Dictionary<string, MeterInvoiceGenerator.InvoiceJob>();
            string monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(SelectedMonth());

            foreach (DataRow r in _dtGrid.Rows)
            {
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) continue;
                if (Dec(r["CurrentReading"]) <= 0m) continue;
                // Invoice grouping: the checkbox overrides each contract's stored BillingMode for this run.
                string mode = (_chkPerCssi != null && _chkPerCssi.Checked) ? "S" : "G";
                long contractKey = D64(r["ContractKey"]);
                long itemKey = D64(r["ItemKey"]);
                string groupKey = mode == "S" ? ("C" + contractKey + "_I" + itemKey) : ("C" + contractKey);
                string refNo = mode == "S" ? S(r["ServiceItemNo"]) : S(r["ContractNo"]);

                MeterBillLine ln = new MeterBillLine();
                ln.ItemKey = itemKey;
                ln.ContractKey = contractKey;
                ln.ItemMeterKey = D64(r["ItemMeterKey"]);
                ln.ContractNo = S(r["ContractNo"]);
                ln.ItemNo = S(r["ServiceItemNo"]);
                ln.DebtorCode = S(r["DebtorCode"]);
                ln.SerialNumber = S(r["SerialNo"]);
                ln.ItemName = S(r["ServiceItemNo"]);
                ln.MeterTypeCode = S(r["MeterType"]);
                ln.MeterTypeName = S(r["MeterTypeName"]);
                ln.ACItemCode = S(r["ACItemCode"]);
                ln.ColorLabel = S(r["Role"]) == "CL" ? "Colour" : "Black";
                ln.Last = Dec(r["LastReading"]);
                ln.Current = Dec(r["CurrentReading"]);
                ln.Usage = Dec(r["MeterUsage"]);
                ln.Rate = Dec(r["UnitPrice"]);
                ln.MinCharges = Dec(r["MinCharges"]);
                ln.Foc = Dec(r["FOCQty"]);
                ln.RebatePct = Dec(r["RebatePct"]);
                ln.Charge = Dec(r["TotalCharges"]);
                ln.UseMin = r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"]);
                if (r["LastReadDate"] != DBNull.Value) ln.LastDate = Convert.ToDateTime(r["LastReadDate"]);

                MeterInvoiceGenerator.InvoiceJob job;
                if (!jobs.TryGetValue(groupKey, out job))
                {
                    job = new MeterInvoiceGenerator.InvoiceJob();
                    job.DebtorCode = ln.DebtorCode;
                    job.RefDocNo = refNo;
                    job.Description = "Meter Billing " + monthName + " - " + refNo;
                    job.Label = refNo;
                    job.Lines = new List<MeterBillLine>();
                    jobs[groupKey] = job;
                }
                job.Lines.Add(ln);
            }

            if (jobs.Count == 0)
            { XtraMessageBox.Show("No selected meter with a current reading.", "Generate Invoice"); return; }

            List<MeterInvoiceGenerator.InvoiceJob> jobList = new List<MeterInvoiceGenerator.InvoiceJob>(jobs.Values);
            if (XtraMessageBox.Show(this,
                    "Generate " + jobList.Count + " invoice(s) now?\r\nThey are saved automatically — no clicking through each one.",
                    "Generate Invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Run the generation on a worker task behind a progress dialog (invoices saved headlessly).
            using (MeterInvoiceGenerateProgress_Form dlg =
                new MeterInvoiceGenerateProgress_Form(_dbSetting, jobList, DateTime.Today, DateTime.Now))
            {
                dlg.ShowDialog(this);
            }
            LoadData();
        }

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
