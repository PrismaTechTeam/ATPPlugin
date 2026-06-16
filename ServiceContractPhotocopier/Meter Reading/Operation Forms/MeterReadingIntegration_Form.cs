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
        private XtraTabPage _pageNo;
        private XtraTabPage _pageAll;
        private LabelControl _lblSummary;

        public MeterReadingIntegration_Form()
        {
            InitializeComponent();
            InitDefaults();
        }

        public MeterReadingIntegration_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            LoadData();
        }

        public MeterReadingIntegration_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            LoadData();
        }

        private void InitDefaults()
        {
            this.LblToday.Text = DateTime.Today.ToString("dd/MM/yyyy (ddd)");
            this.ChkShowAll.Checked = true;

            this.CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                this.CmbMonth.Properties.Items.Add(new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            this.CmbDay.Properties.Items.Clear();
            for (int d = 1; d <= 31; d++) this.CmbDay.Properties.Items.Add(d);
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;

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
            _pageNo = new XtraTabPage(); _pageNo.Text = "No API Data";
            _pageAll = new XtraTabPage(); _pageAll.Text = "All";
            _tabView.TabPages.AddRange(new XtraTabPage[] { _pageWith, _pageNo, _pageAll });

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
            // BK+CL pair is easy to tell apart at a glance.
            this.GridViewMeter.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewMeter_RowStyle);

            // Summary label sitting in the empty area to the right of the action buttons.
            _lblSummary = new LabelControl();
            _lblSummary.Name = "LblSummary";
            _lblSummary.AutoSizeMode = LabelAutoSizeMode.None;
            _lblSummary.Appearance.Font = new Font("Segoe UI", 9F);
            _lblSummary.Appearance.Options.UseFont = true;
            _lblSummary.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            _lblSummary.Location = new Point(1100, 14);
            _lblSummary.Size = new Size(440, 118);
            _lblSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.PanelFilter.Controls.Add(_lblSummary);
            _lblSummary.BringToFront();
        }

        // Each service item gets one of two background shades (white / super-light-blue), alternating
        // per item (NOT per physical row), so a meter pair stays the same colour.
        private void GridViewMeter_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;   // group rows
            object v = GridViewMeter.GetRowCellValue(e.RowHandle, "Shade");
            if (v != null && v != DBNull.Value && Convert.ToInt32(v) == 1)
            {
                e.Appearance.BackColor = Color.FromArgb(228, 241, 252);   // super light blue
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
            if (_tabView.SelectedTabPage == _pageWith) f = "[Status] Like 'Matched%'";
            else if (_tabView.SelectedTabPage == _pageNo) f = "[Status] = 'No API data'";
            GridViewMeter.ActiveFilterString = f;
        }

        private void UpdateTabCounts()
        {
            if (_dtGrid == null || _tabView == null) return;
            int with = 0, no = 0, sel = 0, untouched = 0;
            decimal selCharge = 0m;
            System.Collections.Generic.HashSet<string> items = new System.Collections.Generic.HashSet<string>();
            System.Collections.Generic.HashSet<string> contracts = new System.Collections.Generic.HashSet<string>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                items.Add(S(r["ItemKey"]));
                contracts.Add(S(r["ContractKey"]));
                string st = S(r["Status"]);
                if (st.StartsWith("Matched")) with++;
                else if (st == "No API data") no++;
                else untouched++;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                { sel++; selCharge += Dec(r["TotalCharges"]); }
            }
            _pageWith.Text = "With Meter Data (" + with + ")";
            _pageNo.Text = "No API Data (" + no + ")";
            _pageAll.Text = "All (" + _dtGrid.Rows.Count + ")";

            if (_lblSummary != null)
            {
                string fetched = (with == 0 && no == 0) ? "  (not fetched yet)" : "";
                _lblSummary.Text =
                    "Contracts: " + contracts.Count + "    Service Items: " + items.Count +
                    "    Meters: " + _dtGrid.Rows.Count + "\r\n" +
                    "✔ With meter data: " + with + fetched + "\r\n" +
                    "✖ No API data (need manual key-in): " + no + "\r\n" +
                    "Selected to bill: " + sel + "    Billing total: RM " + selCharge.ToString("n2");
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
            int idx = this.CmbDay.SelectedIndex;
            return (idx >= 0 && idx <= 30) ? idx + 1 : DateTime.Today.Day;
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

                string sql =
                    "SELECT i.ItemKey, c.ContractKey, c.ContractNo, i.ServiceItemNo, i.SerialNumber, " +
                    "c.DebtorCode, ISNULL(d.CompanyName,'') AS DebtorName, c.BillingMode, " +
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
                    "        FROM dbo.zSCP_MeterTrans t) z WHERE z.rn = 1) lr ON lr.ServiceItemMeterTypeKey = m.ItemMeterKey " +
                    "WHERE m.MeterRole IN ('BK','CL') AND i.Inactive='N' AND c.Inactive='N' " + dayFilter + searchFilter +
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
                    _dtGrid.Rows.Add(g);
                }

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
            dt.Columns.Add("MeterType", typeof(string));
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
            SetCol("MeterType", "Meter Type", 130, false);
            SetCol("MeterTypeName", "Meter Type Name", 170, false);
            SetNum("MinCharges", "Min. Charges", 85, false, "n2");
            SetNum("UnitPrice", "Unit Price", 85, false, "n4");
            SetNum("FOCQty", "FOC Qty", 70, false, "n0");
            SetNum("RebatePct", "Rebate (%)", 80, false, "n2");
            SetCol("LastReadDate", "Last Read Date", 100, false);
            SetNum("LastReading", "Last Reading", 95, false, "n0");
            SetNum("CurrentReading", "Current Reading", 100, true, "n0");
            SetNum("MeterUsage", "Meter Usage", 95, false, "n0");
            SetNum("TotalCharges", "Total Charges", 95, false, "n2");
            SetCol("UseMin", "Use Min.", 70, true);
            SetCol("Sel", "Selected", 65, true);
            SetCol("Status", "Status", 130, false);

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

        private void BtnReset_Click(object sender, EventArgs e)
        {
            this.TxtSearch.EditValue = null;
            this.ChkShowAll.Checked = true;
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;
            LoadData();
        }

        private void BtnFetch_Click(object sender, EventArgs e)
        {
            if (_dtGrid == null || _dtGrid.Rows.Count == 0)
            { XtraMessageBox.Show("Nothing to fetch — the list is empty.", "Fetch"); return; }
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                int month = SelectedMonth();
                int day = SelectedDay();
                IMeterReadingApiClient client = MeterReadingApiClientFactory.Create(_dbSetting);
                // One interface, two machine types: call once per status. Each DTO is tagged with the
                // endpoint that produced it (Online / Offline), so we know each item's machine type.
                Dictionary<string, MeterReadingDto> byCode = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
                foreach (MeterReadingDto d in client.GetReadings(MachineStatus.Online, month))
                    if (!string.IsNullOrWhiteSpace(d.Code) && QualifiesByDate(d, month, day)) byCode[d.Code.Trim()] = d;
                foreach (MeterReadingDto d in client.GetReadings(MachineStatus.Offline, month))
                    if (!string.IsNullOrWhiteSpace(d.Code) && QualifiesByDate(d, month, day) && !byCode.ContainsKey(d.Code.Trim()))
                        byCode[d.Code.Trim()] = d;

                int matchedMeters = 0;
                int onlineMeters = 0, offlineMeters = 0;
                System.Collections.Generic.HashSet<string> matchedItems = new System.Collections.Generic.HashSet<string>();
                foreach (DataRow r in _dtGrid.Rows)
                {
                    string code = S(r["ServiceItemNo"]).Trim();
                    MeterReadingDto dto;
                    if (code.Length > 0 && byCode.TryGetValue(code, out dto))
                    {
                        bool isOnline = dto.Status == MachineStatus.Online;
                        if (!string.IsNullOrWhiteSpace(dto.SerialNumber)) r["SerialNo"] = dto.SerialNumber.Trim();
                        r["MachineStatus"] = isOnline ? "ONLINE" : "OFFLINE";
                        r["CurrentReading"] = S(r["Role"]) == "CL" ? dto.TotalCL : dto.TotalBK;
                        Recalc(r);
                        r["Sel"] = true;
                        r["Status"] = "Matched (" + (isOnline ? "Online" : "Offline") + ")  " +
                            (dto.LastAuditDate.HasValue ? dto.LastAuditDate.Value.ToString("dd/MM/yyyy") : "");
                        matchedMeters++; matchedItems.Add(code);
                        if (isOnline) onlineMeters++; else offlineMeters++;
                    }
                    else { r["MachineStatus"] = ""; r["Status"] = "No API data"; }
                }
                GridMeter.RefreshDataSource();
                UpdateTabCounts();
                if (_tabView != null) _tabView.SelectedTabPage = _pageWith;  // jump to the matched view
                ApplyTabFilter();
                XtraMessageBox.Show(matchedItems.Count + " service item(s) / " + matchedMeters +
                    " meter(s) matched with last audit date on/before " + day + " " +
                    new CultureInfo("en-US").DateTimeFormat.GetMonthName(month) + ".\r\n" +
                    "   • Online machines: " + onlineMeters + " meter(s)\r\n" +
                    "   • Offline machines: " + offlineMeters + " meter(s)\r\n" +
                    "Use 'Select Matched' then 'Generate Invoice'.",
                    "Fetch complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Fetch failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor.Current = Cursors.Default; }
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

            Dictionary<string, List<MeterBillLine>> groups = new Dictionary<string, List<MeterBillLine>>();
            Dictionary<string, string> grpRef = new Dictionary<string, string>();
            Dictionary<string, string> grpDebtor = new Dictionary<string, string>();

            foreach (DataRow r in _dtGrid.Rows)
            {
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) continue;
                if (Dec(r["CurrentReading"]) <= 0m) continue;
                string mode = S(r["BillingMode"]);
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

                if (!groups.ContainsKey(groupKey))
                { groups[groupKey] = new List<MeterBillLine>(); grpRef[groupKey] = refNo; grpDebtor[groupKey] = ln.DebtorCode; }
                groups[groupKey].Add(ln);
            }

            if (groups.Count == 0)
            { XtraMessageBox.Show("No selected meter with a current reading.", "Generate Invoice"); return; }

            string monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(SelectedMonth());
            int created = 0; List<string> docNos = new List<string>();
            foreach (KeyValuePair<string, List<MeterBillLine>> grp in groups)
            {
                string desc = "Meter Billing " + monthName + " - " + grpRef[grp.Key];
                try
                {
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = ScpInvoiceBuilder.BuildInvoice(
                        _dbSetting, grpDebtor[grp.Key], grpRef[grp.Key], desc, DateTime.Today, DateTime.Now, grp.Value);
                    AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry invForm =
                        new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc);
                    invForm.ShowDialog(this);
                    bool saved = doc.DocumentState == AutoCount.Invoicing.InvoicingDocumentState.View
                                 && doc.DocNo != AutoCount.Const.AppConst.NewDocumentNo;
                    if (!saved) continue;
                    WriteMeterTrans(doc.DocKey, doc.DocNo, grp.Value);
                    created++; docNos.Add(doc.DocNo);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Invoice for " + grpRef[grp.Key] + " failed:\r\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            XtraMessageBox.Show(created + " invoice(s) created:\r\n" + string.Join(", ", docNos.ToArray()),
                "Generate Invoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        private void WriteMeterTrans(long invoiceDocKey, string docNo, List<MeterBillLine> lines)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("MeterTrans"))
                {
                    try
                    {
                        foreach (MeterBillLine ln in lines)
                        {
                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP_MeterTrans] (ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, " +
                                "MeterTransDate, MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                "VALUES (@simt,@si,@code,@dt,@rd,@dk,@rmk)", cn, tx);
                            cmd.Parameters.AddWithValue("@simt", ln.ItemMeterKey);
                            cmd.Parameters.AddWithValue("@si", ln.ItemKey);
                            cmd.Parameters.AddWithValue("@code", ln.MeterTypeCode);
                            cmd.Parameters.AddWithValue("@dt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@rd", ln.Current);
                            cmd.Parameters.AddWithValue("@dk", invoiceDocKey);
                            cmd.Parameters.AddWithValue("@rmk", ln.ColorLabel + " meter - Invoice " + docNo);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
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
