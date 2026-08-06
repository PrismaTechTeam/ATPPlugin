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
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    [AutoCount.PlugIn.MenuItem("Meter Reading Integration", MenuOrder = 450, ShowAsDialog = false)]
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
        private ComboBoxEdit _cmbMeterFilter;    // #2(d): All / BK only / CL only / BK + CL
        private LabelControl _lblLegendMust;     // #2(c): colour legend
        private LabelControl _lblLegendNo;
        private SimpleButton _btnMonthOverview;
        private ComboBoxEdit _cmbYear;   // explicit billing YEAR (defaults to the auto-resolved one)
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
            this.FormClosing += delegate { try { GridViewMeter.CloseEditor(); GridViewMeter.UpdateCurrentRow(); } catch { } };
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

        private void ToggleMockFetchButton()
        {
            if (_btnMockFetch == null)
            {
                _btnMockFetch = new SimpleButton();
                _btnMockFetch.Size = new Size(150, this.BtnFetch.Height);
                _btnMockFetch.Appearance.BackColor = Color.FromArgb(255, 236, 179);   // amber = TEST
                _btnMockFetch.Appearance.Options.UseBackColor = true;
                _btnMockFetch.Parent = this.BtnFetch.Parent;
                _btnMockFetch.Location = new Point(this.BtnFetch.Right + 8, this.BtnFetch.Top);
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
                    "TESTING SHORTCUT\r\n\r\nDelete ALL " + keys.Rows.Count + " meter-generated invoice(s) from AutoCount?\r\n" +
                    "Meter stamps are released so billing can be repeated. This cannot be undone.",
                    "Dev Wipe — Generated Invoices", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            int deleted = 0, failed = 0;
            System.Text.StringBuilder errs = new System.Text.StringBuilder();
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                    AutoCount.Authentication.UserSession.CurrentUserSession, _dbSetting);
            foreach (DataRow r in keys.Rows)
            {
                try { cmd.Delete(Convert.ToInt64(r["DocKey"])); deleted++; }
                catch (Exception ex)
                {
                    failed++;
                    if (errs.Length < 600) errs.AppendLine(r["DocNo"] + ": " + ex.Message);
                }
            }

            // #10: correction CNs are wiped too — otherwise live CNs point at deleted invoices and
            // their override rows keep winning the baseline, corrupting the next test run.
            try
            {
                DataTable cnKeys = _dbSetting.GetDataTable(
                    "SELECT DISTINCT CNDocKey, ISNULL(CNDocNo,'') AS CNDocNo FROM dbo.zSCP_MeterTrans WHERE CNDocKey IS NOT NULL", false);
                if (cnKeys.Rows.Count > 0)
                {
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand cnCmd =
                        AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                            AutoCount.Authentication.UserSession.CurrentUserSession, _dbSetting);
                    ServiceContractPhotocopier.Classes.ScpCnWatcher.Suppress = true;   // wipe = ours, no per-CN alerts
                    try
                    {
                        foreach (DataRow r in cnKeys.Rows)
                        {
                            try { cnCmd.Delete(Convert.ToInt64(r["CNDocKey"])); deleted++; }
                            catch (Exception ex)
                            {
                                failed++;
                                if (errs.Length < 600) errs.AppendLine(r["CNDocNo"] + ": " + ex.Message);
                            }
                        }
                    }
                    finally { ServiceContractPhotocopier.Classes.ScpCnWatcher.Suppress = false; }
                }
                // Belt-and-braces: any override row that survived (CN delete failed) is removed so
                // the wiped book's baselines are clean for the next run.
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP_MeterTrans WHERE CNDocKey IS NOT NULL");
            }
            catch { }

            ReconcileDeletedInvoices();   // clears stamps + billed readings of the now-deleted docs
            LoadData();
            XtraMessageBox.Show(
                "Deleted " + deleted + " invoice(s)." + (failed > 0 ? "\r\nFailed: " + failed + "\r\n" + errs : "") +
                "\r\nMeter stamps released — you can generate again.",
                "Dev Wipe", MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
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

            this.GridViewMeter.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMeter_CellValueChanged);
            // MERGED cells never activate their in-place editor, so the merged Select checkbox is
            // toggled by hand on mouse-down (SetRowCellValue fires CellValueChanged -> the existing
            // whole-CSSI propagation runs exactly as if the editor had been used).
            this.GridViewMeter.MouseDown += new System.Windows.Forms.MouseEventHandler(GridViewMeter_MouseDown);
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

            this.Controls.Remove(this.GridMeter);
            this.GridMeter.Dock = DockStyle.Fill;
            _pageWith.Controls.Add(this.GridMeter);
            _tabView.Dock = DockStyle.Fill;
            this.Controls.Add(_tabView);
            // A Dock=Fill control must sit at child-index 0 (same slot the designer used for
            // GridMeter) so it is laid out AFTER the docked-Top panels and only fills the area
            // left below them. Otherwise it covers the whole form and hides its own tab strip +
            // the grid's column headers behind the title/filter panels.
            this.Controls.SetChildIndex(_tabView, 0);
            _tabView.SelectedTabPage = _pageWith;
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
            // FOC Qty of a ladder meter displays the LADDER's free copies (the engine ignores the
            // meter's own FOCQty when a ladder is in effect) — consistent with the contract grid.
            this.GridViewMeter.CustomColumnDisplayText +=
                new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(GridViewMeter_CustomColumnDisplayText);

            // Setting button (created in code to avoid touching the strict designer). Same 150x50 /
            // 156px rhythm as the rest of the toolbar row (510/666/822/978/1134).
            _btnSetting = new SimpleButton();
            _btnSetting.Text = "Setting";
            _btnSetting.Location = new Point(1134, 8);
            _btnSetting.Size = new Size(150, 50);
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
            _btnMonthOverview.ToolTipTitle = "Month Overview";
            _btnMonthOverview.ToolTip = "hover to load...";
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
            LabelControl lblMeters = new LabelControl();
            lblMeters.Text = "Meters:";
            lblMeters.Appearance.ForeColor = Color.FromArgb(80, 80, 80);
            lblMeters.Appearance.Options.UseForeColor = true;
            lblMeters.Location = new Point(510, 100);
            this.PanelFilter.Controls.Add(lblMeters);
            lblMeters.BringToFront();

            _cmbMeterFilter = new ComboBoxEdit();
            _cmbMeterFilter.Properties.Items.AddRange(new object[] { "All", "BK only", "CL only", "BK + CL" });
            _cmbMeterFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _cmbMeterFilter.SelectedIndex = 0;
            _cmbMeterFilter.Location = new Point(562, 96);
            _cmbMeterFilter.Size = new Size(110, 22);
            _cmbMeterFilter.ToolTip = "Show only black-and-white (BK) or colour (CL) meter rows.";
            _cmbMeterFilter.SelectedIndexChanged += new EventHandler(CmbMeterFilter_SelectedIndexChanged);
            this.PanelFilter.Controls.Add(_cmbMeterFilter);
            _cmbMeterFilter.BringToFront();

            _lblLegendMust = MakeLegend("  MUST key in  ", Color.FromArgb(255, 213, 79), Color.FromArgb(102, 60, 0), true,
                "Current Reading cells in this colour still need a reading before you can invoice.");
            _lblLegendMust.Location = new Point(690, 97);
            this.PanelFilter.Controls.Add(_lblLegendMust);
            _lblLegendMust.BringToFront();

            _lblLegendNo = MakeLegend("  no key-in needed  ", Color.FromArgb(235, 235, 235), Color.DimGray, false,
                "Rental / waive / commitment rows, frozen snapshots and already-invoiced rows — nothing to key in.");
            _lblLegendNo.Location = new Point(800, 97);
            this.PanelFilter.Controls.Add(_lblLegendNo);
            _lblLegendNo.BringToFront();

            _btnViewSetting = new SimpleButton();
            _btnViewSetting.Text = "View Setting";
            _btnViewSetting.Location = new Point(958, 94);
            _btnViewSetting.Size = new Size(120, 28);
            _btnViewSetting.ToolTip = "Choose which columns this grid shows.";
            _btnViewSetting.Click += new EventHandler(BtnViewSetting_Click);
            this.PanelFilter.Controls.Add(_btnViewSetting);
            _btnViewSetting.BringToFront();

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
            DataRow r = GridViewMeter.GetDataRow(GridViewMeter.FocusedRowHandle);
            if (r == null) return;
            DevExpress.XtraGrid.Columns.GridColumn col = GridViewMeter.FocusedColumn;
            if (col != null && (col.FieldName == "LastInvNo" || col.FieldName == "LastInvDate"))
            {
                string docNo = S(r["LastInvNo"]).Trim();
                if (docNo.Length > 0) { OpenInvoiceByDocNo(docNo); return; }
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

        // FOC Qty display: a ladder meter's free copies come from the LADDER's 0.00 band, not the
        // meter's own FOCQty column (which the engine ignores under a ladder).
        private void GridViewMeter_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "FOCQty" || e.ListSourceRowIndex < 0) return;
            if (_dtGrid == null || _ladders == null || e.ListSourceRowIndex >= _dtGrid.DefaultView.Count) return;
            DataRow r = _dtGrid.DefaultView[e.ListSourceRowIndex].Row;
            string code = S(r["MultiPriceCode"]);
            if (code.Length == 0 || !ServiceContractPhotocopier.Classes.ScpMultiPrice.HasLadder(_ladders, code)) return;
            e.DisplayText = ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(_ladders[code]).ToString("#,##0.##");
        }

        // Plain white rows (the per-item blue/white zebra shading was removed on request) — only
        // unresolved fetch conflicts still tint their row light red.
        private void GridViewMeter_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;   // group rows
            object cf = GridViewMeter.GetRowCellValue(e.RowHandle, "HasConflict");
            if (cf != null && cf != DBNull.Value && Convert.ToBoolean(cf))
            {
                Color rc = Color.FromArgb(255, 224, 224);   // light red = unresolved conflict
                e.Appearance.BackColor = rc; e.Appearance.BackColor2 = rc;  // flat (no gradient)
                e.Appearance.ForeColor = Color.Black;       // focused-row white text vanishes on pale tints
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            // Pale orange = the machine's expiry is BEFORE this billing month ("Include expired"
            // setting is showing it) — visible at a glance so it is never billed by accident.
            object xp = GridViewMeter.GetRowCellValue(e.RowHandle, "IsExpired");
            if (xp != null && xp != DBNull.Value && Convert.ToBoolean(xp))
            {
                Color oc = Color.FromArgb(255, 236, 214);
                e.Appearance.BackColor = oc; e.Appearance.BackColor2 = oc;
                e.Appearance.ForeColor = Color.Black;       // same: keep expired rows readable when focused
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
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
            // Demo #2(a): post any half-typed Current Reading BEFORE the DataView swap below —
            // posting fires CellValueChanged → Recalc + SaveInlineReading while the row is still
            // bound. Covers tab switches, the 0-usage toggle and the meter filter in one spot.
            try { GridViewMeter.CloseEditor(); GridViewMeter.UpdateCurrentRow(); } catch { }
            string f = "";
            // "Need Manual Key-In" membership is a SNAPSHOT (NeedManual flag, recomputed only at
            // load/fetch): keying a reading keeps the row on this tab so the operator can review what
            // they just typed — it only leaves the tab on the next Refresh / Fetch.
            // Tabs are PER MACHINE (grouped by the NeedManual flag), so a machine's rental + BK/CL rows
            // always appear together on the same tab. Ready to Invoice = ready machines (all usage read, or
            // rental-only) that aren't invoiced yet; Need Manual = machines still missing a reading.
            if (_tabView.SelectedTabPage == _pageWith) f = "[NeedManual] = False AND ISNULL([InvoicedDocNo],'') = ''";
            else if (_tabView.SelectedTabPage == _pageNo) f = "[NeedManual] = True";
            else if (_tabView.SelectedTabPage == _pageDone) f = "[InvoicedDocNo] <> ''";   // this month's billed machines
            else if (_tabView.SelectedTabPage == _pageConflict) f = "[HasConflict] = True";

            // Hide zero-usage meters unless "Include 0 Meter Usage" is ticked. Exceptions that never
            // hide: rows that still BILL (minimum charges -> TotalCharges > 0, or Generate would miss
            // them), rows ALREADY INVOICED for the selected month (usage reset to 0 on billing), and
            // the whole "Need Manual Key-In" tab — its rows are 0-usage BY DEFINITION (nothing keyed
            // yet), so the usage filter would blank the tab out.
            if (_chkInclude0Usage != null && !_chkInclude0Usage.Checked && _tabView.SelectedTabPage != _pageNo)
            {
                string usage = "([MeterUsage] <> 0 OR [TotalCharges] <> 0 OR [InvoicedDocNo] <> '' OR [IsFlat] = True)";
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
            if (_dtGrid != null)
            {
                DataView dv = new DataView(_dtGrid, f, "", DataViewRowState.CurrentRows);
                GridMeter.DataSource = dv;
                GridViewMeter.ActiveFilterString = "";
                GridViewMeter.ExpandAllGroups();   // customer groups start open on every tab switch
            }
            ApplyTabColumnLayout();
            UpdateSelectAllButton();   // caption follows the tab's selection state
        }

        // The Invoiced tab reads like an INVOICE HISTORY, not a key-in sheet: reading/pricing columns
        // hide, the invoice columns (No / Date / Total) carry the story. Other tabs restore the
        // normal working layout.
        private static readonly string[] _keyInLayoutCols = new string[] {
            "SelCssi", "MachineStatus", "MinCharges", "UnitPrice", "FOCQty", "RebatePct",
            "LastAuditDate", "CurrentReading", "MeterUsage", "TotalCharges" };

        private void ApplyTabColumnLayout()
        {
            bool inv = _tabView.SelectedTabPage == _pageDone;
            foreach (string c in _keyInLayoutCols)
                if (GridViewMeter.Columns[c] != null) GridViewMeter.Columns[c].Visible = !inv;
            GridColumn tot = GridViewMeter.Columns["InvTotal"];
            if (tot != null)
            {
                tot.Visible = inv;
                if (inv && GridViewMeter.Columns["LastInvDate"] != null)
                    tot.VisibleIndex = GridViewMeter.Columns["LastInvDate"].VisibleIndex + 1;
            }
        }

        private void ChkInclude0Usage_CheckedChanged(object sender, EventArgs e) { ApplyTabFilter(); }

        // A colour swatch label for the Current Reading legend — same colours the cell style paints.
        private LabelControl MakeLegend(string text, Color back, Color fore, bool bold, string tip)
        {
            LabelControl l = new LabelControl();
            l.Text = text;
            l.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            l.Size = new Size(text.Length * 7, 20);
            l.Appearance.Font = new Font("Segoe UI", 8F, bold ? FontStyle.Bold : FontStyle.Regular);
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
            return "";
        }

        private void CmbMeterFilter_SelectedIndexChanged(object sender, EventArgs e) { ApplyTabFilter(); }

        // #2(b): let the user choose which columns the grid shows. The column chooser can do this
        // too, but only if you know to right-click the header — this is the discoverable door, and
        // it only lists the columns that are actually the user's to decide (the tab-owned key-in
        // columns and the system drivers stay out of reach).
        private void BtnViewSetting_Click(object sender, EventArgs e)
        {
            System.Collections.Generic.List<GridColumn> cols = new System.Collections.Generic.List<GridColumn>();
            foreach (string fn in _viewSettingCols)
            {
                GridColumn c = GridViewMeter.Columns[fn];
                if (c != null) cols.Add(c);
            }
            if (cols.Count == 0) return;
            using (MeterViewSetting_Form f = new MeterViewSetting_Form(cols))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                f.ApplySelection();
            }
        }

        // Snapshot the "Need Manual Key-In" membership per MACHINE: a machine needs key-in when NONE
        // of its meters has a reading from anywhere (no key-in, no API value). Called only at load
        // and after a fetch — keying a reading later does NOT remove the row until the next reload.
        // Flat / rental meters have no meter reading: auto-bill them as quantity 1 x Unit Price (the
        // rental amount) every period. Reading is forced to LastReading+1 so usage = 1; the invoice
        // builder also forces usage 1 for flat lines (see ScpInvoiceBuilder.ComputeCharge). Already-
        // invoiced periods and manual overrides are left untouched.
        private void AutoFillFlatMeters()
        {
            if (_dtGrid == null) return;
            foreach (DataRow r in _dtGrid.Rows)
            {
                if (r["IsFlat"] == DBNull.Value || !Convert.ToBoolean(r["IsFlat"])) continue;
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;   // billed this period already
                if (r.Table.Columns.Contains("IsWaive") && r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]))
                {
                    // Waive contra: whether (and how much) it fires is decided at Generate from its
                    // Waive Configuration — the preview must show 0.00, never a positive amount.
                    r["CurrentReading"] = 0m;
                    r["MeterUsage"] = 0m;
                    r["TotalCharges"] = 0m;
                    r["EntrySource"] = "WAIVE-AUTO";
                    continue;
                }
                if (r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"])) continue;
                decimal rate = Dec(r["UnitPrice"]);
                decimal min = Dec(r["MinCharges"]);
                decimal foc = Dec(r["FOCQty"]);        // rental FOC Qty = free-rental months remaining
                decimal charge;
                if (foc > 0m)
                {
                    // Free-rental month: RM0 this period; the FOC-months counter is decremented at
                    // Generate (per the countdown model), then normal rental resumes at FOC = 0.
                    charge = 0m;
                    r["EntrySource"] = "RENTAL FREE";
                }
                else
                {
                    charge = rate;                     // the flat rental amount (rate, or Min Charges)
                    if (charge < min) charge = min;
                    r["EntrySource"] = "RENTAL";
                }
                // Rental has NO meter reading — reading + usage stay 0 (still shown), the charge is the
                // flat rental / minimum payment. It bills as long as that amount > 0.
                r["CurrentReading"] = 0m;
                r["MeterUsage"] = 0m;
                r["TotalCharges"] = charge;
                // Rental period n/N ("MONTHLY RENTAL (3/12)") — replaces the blank slot in the meter
                // type name so the grid AND the invoice line both show the current period.
                if (r["RentalStartDate"] != DBNull.Value && Convert.ToInt32(r["RentalMonths"]) > 0)
                    r["MeterTypeName"] = ServiceContractPhotocopier.Classes.ScpStrategy.ComposeRentalPeriodText(
                        S(r["MeterTypeName"]), Convert.ToDateTime(r["RentalStartDate"]),
                        Convert.ToInt32(r["RentalMonths"]), S(r["RentalBasis"]) == "P" ? 'P' : 'A',
                        SelectedYear(), SelectedMonth());
            }
        }

        private void RecomputeNeedManual()
        {
            if (_dtGrid == null) return;
            // PER MACHINE: a machine "needs manual key-in" when it still has a USAGE (non-flat) meter
            // with no reading that isn't invoiced yet. Flat/rental meters (auto-billed) and already-
            // invoiced meters never require key-in. The whole machine shares ONE flag, so its meters
            // never split across the Ready-to-Invoice / Need Manual tabs — a rental row always sits in the
            // SAME tab as its BK/CL siblings (that split was what confused users).
            Dictionary<long, bool> needManual = new Dictionary<long, bool>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                long ik = D64(r["ItemKey"]);
                if (!needManual.ContainsKey(ik)) needManual[ik] = false;
                bool isFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (isFlat) continue;                                    // rental: auto-billed, never manual
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;   // already invoiced this period
                bool hasReading = Dec(r["CurrentReading"]) > 0m || Dec(r["FetchedReading"]) > 0m;
                if (!hasReading) needManual[ik] = true;                  // a usage meter still needs a reading
            }
            foreach (DataRow r in _dtGrid.Rows)
                r["NeedManual"] = needManual[D64(r["ItemKey"])];
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
            System.Collections.Generic.Dictionary<string, bool> invByItem = new System.Collections.Generic.Dictionary<string, bool>();
            System.Collections.Generic.Dictionary<string, bool> nmByItem = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                string itemKey = S(r["ItemKey"]);
                allItems.Add(itemKey);
                contracts.Add(S(r["ContractKey"]));
                string ms = S(r["MachineStatus"]);
                if (ms == "ONLINE") onlineItems.Add(itemKey);
                else if (ms == "OFFLINE") offlineItems.Add(itemKey);
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) invByItem[itemKey] = true;
                if (r["NeedManual"] != DBNull.Value && Convert.ToBoolean(r["NeedManual"])) nmByItem[itemKey] = true;
                if (r["HasConflict"] != DBNull.Value && Convert.ToBoolean(r["HasConflict"])) conflictItems.Add(itemKey);
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                { sel++; selCharge += Dec(r["TotalCharges"]); }
            }
            // Bucket each MACHINE into exactly one of Invoiced / Need Manual / Ready to Invoice — mirrors the
            // per-machine tab filters so a machine with mixed rows (e.g. rental + unread BK) counts once.
            foreach (string itemKey in allItems)
            {
                bool inv; invByItem.TryGetValue(itemKey, out inv);
                bool nm; nmByItem.TryGetValue(itemKey, out nm);
                if (inv) invoicedItems.Add(itemKey);
                else if (nm) needManualItems.Add(itemKey);
                else withItems.Add(itemKey);
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
            object k1 = GridViewMeter.GetRowCellValue(e.RowHandle1, keyField);
            object k2 = GridViewMeter.GetRowCellValue(e.RowHandle2, keyField);
            e.Merge = (k1 != null && k2 != null && k1.ToString() == k2.ToString());
            // Serial + status are per MACHINE: a multi-machine item's rows only merge within the same
            // machine serial (SelCssi stays per item — selection is per CSSI by design).
            if (e.Merge && (f == "SerialNo" || f == "MachineStatus"))
            {
                string s1 = S(GridViewMeter.GetRowCellValue(e.RowHandle1, "SerialNo"));
                string s2 = S(GridViewMeter.GetRowCellValue(e.RowHandle2, "SerialNo"));
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
                string sql = "SELECT DISTINCT COALESCE(i.BillingDayOverride, c.BillingDay) AS d " +
                    "FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "WHERE " + (inclInactive ? "1=1 " : "i.Inactive='N' AND c.Inactive='N' ") +
                    // Only machines that can actually appear in the meter list (they HAVE meters) —
                    // otherwise a meterless test item creates a dead day button showing 0 rows. Any meter
                    // role counts now (BK/CL usage meters + rental/min/fax NA meters all show).
                    "AND EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter m WHERE m.ItemKey = i.ItemKey) " +
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

        private void RebuildDayButtons()
        {
            if (_pnlDays == null) return;
            _pnlDays.Controls.Clear();
            int n = this.CmbDay.Properties.Items.Count;
            if (n == 0) return;
            int w = Math.Max(30, Math.Min(44, (432 / n) - 4));
            for (int i = 0; i < n; i++)
            {
                int day = Convert.ToInt32(this.CmbDay.Properties.Items[i]);
                DevExpress.XtraEditors.CheckButton b = new DevExpress.XtraEditors.CheckButton();
                b.Text = day.ToString();
                b.GroupIndex = 77;   // radio behaviour across the strip
                b.Location = new Point(i * (w + 4), 2);
                b.Size = new Size(w, 26);
                b.Tag = day;
                b.Checked = (this.CmbDay.SelectedIndex == i);
                PaintDayButton(b, b.Checked);
                b.CheckedChanged += new EventHandler(DayButton_CheckedChanged);
                _pnlDays.Controls.Add(b);
            }
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
                if (db != null) PaintDayButton(db, db.Checked);
            }
            if (!b.Checked) return;
            int day = Convert.ToInt32(b.Tag);
            if (!object.Equals(this.CmbDay.SelectedItem, day))
                this.CmbDay.SelectedItem = day;   // fires CmbDay_SelectedIndexChanged -> reload
        }

        private static void PaintDayButton(DevExpress.XtraEditors.CheckButton b, bool on)
        {
            if (on)
            {
                b.Appearance.BackColor = Color.FromArgb(33, 115, 199);   // selected = blue
                b.Appearance.ForeColor = Color.White;
                b.Appearance.Options.UseBackColor = true;
                b.Appearance.Options.UseForeColor = true;
                b.Appearance.FontStyleDelta = FontStyle.Bold;
            }
            else
            {
                b.Appearance.BackColor = Color.Empty;
                b.Appearance.ForeColor = Color.Empty;
                b.Appearance.Options.UseBackColor = false;
                b.Appearance.Options.UseForeColor = false;
                b.Appearance.FontStyleDelta = FontStyle.Regular;
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
            try
            {
                // Audit BEFORE the delete: the billed readings about to be un-billed are appended to the
                // immutable log as INVOICE-DELETED — stamped with WHO ran the reconcile and carrying the
                // original invoice's baseline/usage over from its INVOICE log row.
                string who = "";
                try { who = (AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? "").Replace("'", "''"); }
                catch { }
                _dbSetting.ExecuteNonQuery(
                    "INSERT INTO dbo.zSCP2_MeterReadingLog (ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, Source, DocNo, CreatedBy, LastReading, [Usage], UnitPrice, MinCharges, FOCQty, RebatePct, Charge) " +
                    "SELECT t.ServiceItemMeterTypeKey, ISNULL(me.PeriodYear,0), ISNULL(me.PeriodMonth,0), " +
                    "t.MeterTransReading, t.MeterTransDate, 'INVOICE-DELETED', ISNULL(me.InvoicedDocNo,''), N'" + who + "', " +
                    "orig.LastReading, orig.[Usage], orig.UnitPrice, orig.MinCharges, orig.FOCQty, orig.RebatePct, orig.Charge " +
                    "FROM dbo.zSCP_MeterTrans t " +
                    "LEFT JOIN dbo.zSCP2_MeterEntry me ON me.InvoicedDocKey = t.SalesInvoiceDocKey AND me.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                    "OUTER APPLY (SELECT TOP 1 l.LastReading, l.[Usage], l.UnitPrice, l.MinCharges, l.FOCQty, l.RebatePct, l.Charge FROM dbo.zSCP2_MeterReadingLog l " +
                    "  WHERE l.ItemMeterKey = t.ServiceItemMeterTypeKey AND l.Source = 'INVOICE' " +
                    "  AND l.DocNo = ISNULL(me.InvoicedDocNo,'') ORDER BY l.LogKey DESC) orig " +
                    "WHERE t.SalesInvoiceDocKey IS NOT NULL " +
                    "AND t.SalesInvoiceDocKey IN (SELECT me2.InvoicedDocKey FROM dbo.zSCP2_MeterEntry me2 WHERE me2.InvoicedDocKey IS NOT NULL) " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.SalesInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                _dbSetting.ExecuteNonQuery(
                    "DELETE t FROM dbo.zSCP_MeterTrans t " +
                    "WHERE t.SalesInvoiceDocKey IS NOT NULL " +
                    "AND t.SalesInvoiceDocKey IN (SELECT me.InvoicedDocKey FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocKey IS NOT NULL) " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.SalesInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                _dbSetting.ExecuteNonQuery(
                    "UPDATE me SET InvoicedDocKey=NULL, InvoicedDocNo='', InvoicedAt=NULL " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "WHERE me.InvoicedDocKey IS NOT NULL " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = me.InvoicedDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                // #10: reading overrides whose CN was deleted/cancelled roll back the same way.
                ServiceContractPhotocopier.Classes.ScpCreditNoteBuilder.ReconcileDeletedCreditNotes(_dbSetting);
            }
            catch { }   // reconciliation is best-effort; the load itself must never be blocked
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
                string dayFilter = this.ChkShowAll.Checked ? "" :
                    " AND ((CASE WHEN COALESCE(i.BillingDayOverride,c.BillingDay) > " + monthEnd +
                    " THEN " + monthEnd + " ELSE COALESCE(i.BillingDayOverride,c.BillingDay) END) = " + target +
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

                string sql =
                    // Per-meter machine serial: a multi-machine CSSI assigns meters to provided units
                    // (m.MachineSerialNo); '' falls back to the CSSI's own header machine serial.
                    "SELECT i.ItemKey, c.ContractKey, c.ContractNo, i.ServiceItemNo, " +
                    "ISNULL(i.Description,'') AS ItemDesc, " +
                    "COALESCE(NULLIF(m.MachineSerialNo,''), i.SerialNumber) AS SerialNumber, " +
                    "c.DebtorCode, ISNULL(d.CompanyName,'') AS DebtorName, c.BillingMode, " +
                    "ISNULL(c.StrategyCode,'') AS StrategyCode, ISNULL(c.RentalSeparateInvoice,'N') AS RentSep, " +
                    "ISNULL(c.PeriodFollowContract,'N') AS PeriodByContract, c.ServiceStartDate AS ContractStart, " +
                    "ISNULL(c.RentalBillingDay,0) AS RentalBillingDay, " +
                    "ISNULL(c.FOCResetUnit,'M') AS FOCResetUnit, ISNULL(c.FOCResetN,0) AS FOCResetN, " +
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS EffBillingDay, " +
                    "ISNULL(i.IsGroupItem,'N') AS IsGroupItem, ISNULL(i.MachineMode,'') AS MachineMode, " +
                    "ISNULL(i.BillGroupCode,'') AS BillGroupCode, " +
                    "m.ItemMeterKey, m.MeterRole, m.MeterTypeCode, ISNULL(mt.Description,'') AS MeterTypeName, " +
                    // Invoice line Item Code = the meter type's stock code (master convention: metertype.stockcode
                    // goes on the charge row); ACItemCode is an explicit override when set.
                    "ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS ACItemCode, ISNULL(m.MinimumCharges,0) AS MinCharges, " +
                    "ISNULL(m.ChargesRate,0) AS UnitPrice, ISNULL(m.FOCQty,0) AS FOCQty, " +
                    // Per-meter tier override (zSCP2_ItemMeterPrice) beats the scheme code; its ladder is
                    // keyed '#<ItemMeterKey>' in the loaded ladder map (ScpMultiPrice.MeterLadderKey).
                    // Joined (pm), NOT a correlated EXISTS — the correlated form ballooned this query's
                    // memory grant and stalled it on RESOURCE_SEMAPHORE when the server was memory-squeezed.
                    "CASE WHEN pm.ItemMeterKey IS NOT NULL " +
                    "     THEN '#' + CAST(m.ItemMeterKey AS varchar(20)) " +
                    "     ELSE ISNULL(NULLIF(m.MeterMultiPriceCode,''), ISNULL(mt.MeterMultiPriceCode,'')) END AS MultiPriceCode, " +
                    "ISNULL(m.RebateQtyInPercent,0) AS RebatePct, ISNULL(m.InitialReading,0) AS InitReading, " +
                    "ISNULL(mt.IsFlatCharge,'N') AS IsFlatCharge, ISNULL(mt.IsRentalWaive,'N') AS IsRentalWaive, " +
                    "ISNULL(m.WaiveFirstNMonths,0) AS WaiveFirstNMonths, ISNULL(m.WaiveTargetAmount,0) AS WaiveTargetAmount, " +
                    "ISNULL(m.WaivePartialThreshold,0) AS WaivePartialThreshold, " +
                    "ISNULL(m.WaivePartialAmount,0) AS WaivePartialAmount, ISNULL(m.WaiveScope,'BKCL') AS WaiveScope, " +
                    "m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, ISNULL(m.RentalBasis,'A') AS RentalBasis, " +
                    "COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) AS EffExpiry, " +
                    "COALESCE(i.ServiceStartDate, c.ServiceStartDate) AS EffStart, " +
                    "lr.LastReading, lr.LastDate, " +
                    "ISNULL(li.LastInvNo,'') AS LastInvNo, li.LastInvAt, ISNULL(li.LastInvTotal,0) AS LastInvTotal " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                    "LEFT JOIN (SELECT DISTINCT ItemMeterKey FROM dbo.zSCP2_ItemMeterPrice) pm ON pm.ItemMeterKey = m.ItemMeterKey " +
                    // Latest reading per meter in ONE pass over zSCP_MeterTrans (window function),
                    // instead of a correlated TOP-1 OUTER APPLY per row (which timed out on 145k rows).
                    "LEFT JOIN (SELECT z.ServiceItemMeterTypeKey, z.MeterTransReading AS LastReading, z.MeterTransDate AS LastDate " +
                    "  FROM (SELECT t.ServiceItemMeterTypeKey, t.MeterTransReading, t.MeterTransDate, " +
                    // A row BILLED in the selected period ALWAYS becomes the baseline (Last Reading +
                    // Last Read Date follow the just-billed reading), even if stray legacy rows carry
                    // out-of-order dates — then normal latest-date ordering.
                    "        ROW_NUMBER() OVER (PARTITION BY t.ServiceItemMeterTypeKey ORDER BY " +
                    "          CASE WHEN t.SalesInvoiceDocKey IS NOT NULL AND t.MeterTransDate >= DATEFROMPARTS(" + year + "," + month + ",1) THEN 1 ELSE 0 END DESC, " +
                    "          t.MeterTransDate DESC, t.MeterTransKey DESC) AS rn " +
                    // Last reading baseline = latest MeterTrans STRICTLY BEFORE the selected billing
                    // month, PLUS any BILLED reading inside the month (SalesInvoiceDocKey set): once a
                    // meter is invoiced, its Last Reading moves to the billed value so the remaining
                    // usage shows 0 — and reverts automatically if that invoice is later deleted
                    // (ReconcileDeletedInvoices removes the billed row).
                    "        FROM dbo.zSCP_MeterTrans t WHERE t.MeterTransDate < DATEFROMPARTS(" + year + "," + month + ",1) " +
                    "           OR (t.SalesInvoiceDocKey IS NOT NULL AND t.MeterTransDate < DATEADD(MONTH, 1, DATEFROMPARTS(" + year + "," + month + ",1)))) z WHERE z.rn = 1) lr ON lr.ServiceItemMeterTypeKey = m.ItemMeterKey " +
                    // Last invoice ever generated for this meter (from the zSCP2_MeterEntry stamps) —
                    // lets the operator tell "0 usage because ALREADY INVOICED" apart from "no reading".
                    "LEFT JOIN (SELECT z2.ItemMeterKey, z2.InvoicedDocNo AS LastInvNo, z2.InvoicedAt AS LastInvAt, " +
                    "         ivh.NetTotal AS LastInvTotal " +
                    "  FROM (SELECT me.ItemMeterKey, me.InvoicedDocNo, me.InvoicedAt, me.InvoicedDocKey, " +
                    "        ROW_NUMBER() OVER (PARTITION BY me.ItemMeterKey ORDER BY me.InvoicedAt DESC) AS rn " +
                    "        FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocKey IS NOT NULL) z2 " +
                    "  LEFT JOIN dbo.IV ivh ON ivh.DocKey = z2.InvoicedDocKey " +
                    "  WHERE z2.rn = 1) li " +
                    "ON li.ItemMeterKey = m.ItemMeterKey " +
                    // ALL meters of the item are shown — BK/CL usage meters AND the non-reading ones
                    // (rental RA-*, minimum MIN-*, fax, etc., role NA). Previously only BK/CL showed,
                    // which made the item's other meters look "missing".
                    "WHERE " + inactiveFilter + " " + dayFilter + searchFilter + expiryFilter +
                    "ORDER BY c.DebtorCode, c.ContractNo, i.ServiceItemNo, m.MeterRole " +   // customers cluster together
                    // Memory-squeeze armour. On this dev box Windows pressure shrank SQL's query-workspace
                    // pool to ~18MB and the PARALLEL plan's REQUIRED grant (~50MB, scales with DOP) could
                    // never fit -> permanent RESOURCE_SEMAPHORE queue -> timeout. MAXDOP 1 cuts the required
                    // minimum to a few MB (fits any pool); MAX_GRANT_PERCENT makes oversized asks spill to
                    // tempdb instead of queuing. ~4k result rows — serial is fast anyway.
                    "OPTION (MAXDOP 1, MAX_GRANT_PERCENT = 20)";
                DataTable src = QueryWithTimeout(sql, 180);

                _dtGrid = NewGridTable();
                _ladders = ServiceContractPhotocopier.Classes.ScpMultiPrice.LoadLadders(_dbSetting);   // tiered pricing
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
                    g["SelCssi"] = false;
                    g["ContractNo"] = S(r["ContractNo"]);
                    g["ServiceItemNo"] = S(r["ServiceItemNo"]);
                    g["SerialNo"] = S(r["SerialNumber"]);
                    g["ItemDesc"] = S(r["ItemDesc"]);
                    g["Customer"] = S(r["DebtorCode"]) + " - " + S(r["DebtorName"]);
                    g["Mode"] = S(r["BillingMode"]) == "S" ? "Separate" : "Group";
                    if (r["EffBillingDay"] != DBNull.Value) g["BillingDay"] = Convert.ToInt32(r["EffBillingDay"]);
                    g["MeterType"] = S(r["MeterTypeCode"]);
                    g["MeterTypeName"] = S(r["MeterTypeName"]);
                    g["MinCharges"] = Dec(r["MinCharges"]);
                    g["UnitPrice"] = Dec(r["UnitPrice"]);
                    g["FOCQty"] = Dec(r["FOCQty"]);
                    g["MultiPriceCode"] = S(r["MultiPriceCode"]);
                    g["FOCResetUnit"] = S(r["FOCResetUnit"]);
                    g["FOCResetN"] = r["FOCResetN"] == DBNull.Value ? 0 : Convert.ToInt32(r["FOCResetN"]);
                    g["RebatePct"] = Dec(r["RebatePct"]);
                    if (r["LastDate"] != DBNull.Value) g["LastReadDate"] = Convert.ToDateTime(r["LastDate"]);
                    g["LastReading"] = (r["LastReading"] == DBNull.Value) ? Dec(r["InitReading"]) : Dec(r["LastReading"]);
                    g["CurrentReading"] = 0m;
                    g["MeterUsage"] = 0m;
                    g["TotalCharges"] = 0m;
                    g["UseMin"] = (Dec(r["UnitPrice"]) == 0m && Dec(r["MinCharges"]) > 0m);
                    g["LastInvNo"] = S(r["LastInvNo"]);
                    if (r["LastInvAt"] != DBNull.Value) g["LastInvDate"] = Convert.ToDateTime(r["LastInvAt"]);
                    g["InvTotal"] = Dec(r["LastInvTotal"]);
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
                    // A waive meter is flat BY DEFINITION (it has no reading) even if the type's
                    // rental flag was left unticked in the master.
                    g["IsWaive"] = S(r["IsRentalWaive"]) == "Y";
                    g["IsGroupItem"] = S(r["IsGroupItem"]) == "Y";
                    g["MachineMode"] = S(r["MachineMode"]);
                    g["BillGroupCode"] = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]));
                    g["IsFlat"] = S(r["IsFlatCharge"]) == "Y" || S(r["IsRentalWaive"]) == "Y";
                    g["WaiveFirstNMonths"] = r["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["WaiveFirstNMonths"]);
                    g["WaiveTargetAmount"] = Dec(r["WaiveTargetAmount"]);
                    g["WaivePartialThreshold"] = Dec(r["WaivePartialThreshold"]);
                    g["WaivePartialAmount"] = Dec(r["WaivePartialAmount"]);
                    g["WaiveScope"] = S(r["WaiveScope"]);
                    // Expired = effective expiry BEFORE the billing month's 1st (still billable IN its
                    // final month — this only flags machines whose last billable month is already over).
                    g["IsExpired"] = r["EffExpiry"] != DBNull.Value &&
                        Convert.ToDateTime(r["EffExpiry"]).Date < new DateTime(year, month, 1);
                    if (r["EffStart"] != DBNull.Value) g["EffStart"] = Convert.ToDateTime(r["EffStart"]);
                    if (r["ContractStart"] != DBNull.Value) g["ContractStart"] = Convert.ToDateTime(r["ContractStart"]);
                    g["StrategyCode"] = S(r["StrategyCode"]);
                    g["RentSep"] = S(r["RentSep"]) == "Y";
                    g["PeriodByContract"] = S(r["PeriodByContract"]) == "Y";
                    g["RentalBillingDay"] = r["RentalBillingDay"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalBillingDay"]);
                    if (r["RentalStartDate"] != DBNull.Value) g["RentalStartDate"] = Convert.ToDateTime(r["RentalStartDate"]);
                    g["RentalMonths"] = r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]);
                    g["RentalBasis"] = S(r["RentalBasis"]) == "P" ? "P" : "A";
                    _dtGrid.Rows.Add(g);
                }

                PrefillFromStaging(SelectedMonth(), SelectedYear());
                AutoFillFlatMeters();
                RecomputeNeedManual();

                GridMeter.DataSource = _dtGrid;
                ConfigureGrid();
                WireNativeGridLayout();   // #1a: AutoCount-native layout restore + header menu
                UpdateTabCounts();
                ApplyTabFilter();
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
        private AutoCount.XtraUtils.CustomizeGridLayout _gridLayout;

        private void WireNativeGridLayout()
        {
            if (_gridLayout != null) return;
            try
            {
                AutoCount.Authentication.UserSession us = AutoCount.Authentication.UserSession.CurrentUserSession;
                if (us == null) return;
                // Ctor restores the layout from dbo.Layout (per-user assignment wins over default)
                // and injects the layout menu into the column-header right-click popup.
                _gridLayout = new AutoCount.XtraUtils.CustomizeGridLayout(us, GRID_LAYOUT_KEY, GridViewMeter);
                // Our grid, our rules: every user gets the layout/column/export menu here (the
                // native SYS_BHV_* rights default to admin-ish groups only).
                _gridLayout.GetAccessRightSetting +=
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
                ApplyTabColumnLayout();   // per-tab hidden columns win over the restored snapshot
            }
            catch { }   // layout plumbing must never break the screen
        }

        // Silent per-user auto-save on close (#1a "no re-setting"): saved as a NAMED layout owned
        // by this user and assigned via dbo.LayoutUsers, so the ctor restores it next open. The
        // native Save Layout / templates / defaults keep working on top.
        private void SaveGridLayoutForCurrentUser()
        {
            try
            {
                if (_gridLayout == null || _dbSetting == null) return;
                string user = "";
                try { user = AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? ""; } catch { }
                if (user.Length == 0) return;
                string title = "SCP Meter - " + user;
                if (title.Length > 60) title = title.Substring(0, 60);
                if (!_gridLayout.SaveLayout(title, false)) return;   // title owned by another grid — skip
                string tEsc = title.Replace("'", "''"), uEsc = user.Replace("'", "''");
                _dbSetting.ExecuteNonQuery(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.LayoutUsers WHERE Title = N'" + tEsc + "' AND UserID = N'" + uEsc + "') " +
                    "INSERT INTO dbo.LayoutUsers (Title, UserID) VALUES (N'" + tEsc + "', N'" + uEsc + "')");
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
            DataTable dt = new DataTable();
            // ---- visible: item columns (these merge), then meter columns ----
            dt.Columns.Add("SelCssi", typeof(bool));   // ONE merged checkbox per CSSI — ticks/unticks the whole machine (both meters)
            dt.Columns.Add("ContractNo", typeof(string));
            dt.Columns.Add("ServiceItemNo", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("ItemDesc", typeof(string));        // item description (invoice line text; hidden)
            dt.Columns.Add("MachineStatus", typeof(string));   // ONLINE / OFFLINE (set on fetch, per item)
            dt.Columns.Add("Customer", typeof(string));
            dt.Columns.Add("Mode", typeof(string));
            dt.Columns.Add("BillingDay", typeof(int));   // effective billing day = COALESCE(item override, contract day)
            dt.Columns.Add("MeterType", typeof(string));
            dt.Columns.Add("MeterTypeName", typeof(string));
            dt.Columns.Add("MinCharges", typeof(decimal));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("MultiPriceCode", typeof(string));   // tiered-pricing ladder code (hidden; drives the tier price)
            dt.Columns.Add("FOCResetUnit", typeof(string));     // contract FOC reset period M/W/D (hidden)
            dt.Columns.Add("FOCResetN", typeof(int));           // N for 'D' (hidden)
            dt.Columns.Add("RebatePct", typeof(decimal));
            dt.Columns.Add("LastReadDate", typeof(DateTime));
            dt.Columns.Add("LastAuditDate", typeof(DateTime));   // API audit date of the CURRENT fetched reading
            dt.Columns.Add("LastFetchDate", typeof(DateTime));   // when the reading was last fetched/saved into staging
            dt.Columns.Add("LastReading", typeof(decimal));
            dt.Columns.Add("CurrentReading", typeof(decimal));
            dt.Columns.Add("MeterUsage", typeof(decimal));
            dt.Columns.Add("TotalCharges", typeof(decimal));
            dt.Columns.Add("LastInvNo", typeof(string));      // last invoice generated for this meter (any period)
            dt.Columns.Add("LastInvDate", typeof(DateTime));  // its date — GREEN when it falls in the selected billing period (= already invoiced now)
            dt.Columns.Add("InvTotal", typeof(decimal));      // that invoice's NetTotal (shown on the Invoiced tab only)
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
            dt.Columns.Add("TrackingId", typeof(string));     // offline report id ("MR-yymmdd-nnn"); '' = none (hidden)
            dt.Columns.Add("FetchedReading", typeof(decimal)); // last value the API returned for this meter (hidden)
            dt.Columns.Add("IsFlat", typeof(bool));            // flat/rental meter: auto-billed qty 1 x price, no reading
            dt.Columns.Add("IsWaive", typeof(bool));           // Rental-Waive contra meter (engine decides firing)
            dt.Columns.Add("IsGroupItem", typeof(bool));       // the contract's GROUP machine (fleet-total deals)
            dt.Columns.Add("MachineMode", typeof(string));     // DEFINED online/offline ('' = use fetch status)
            dt.Columns.Add("BillGroupCode", typeof(string));   // #6 bill-group split ('' = none)
            dt.Columns.Add("WaiveFirstNMonths", typeof(int));
            dt.Columns.Add("WaiveTargetAmount", typeof(decimal));
            dt.Columns.Add("WaivePartialThreshold", typeof(decimal));
            dt.Columns.Add("WaivePartialAmount", typeof(decimal));
            dt.Columns.Add("WaiveScope", typeof(string));
            dt.Columns.Add("StrategyCode", typeof(string));    // contract's strategy in force (hidden; stamped at generate)
            dt.Columns.Add("RentSep", typeof(bool));           // contract flag: rental billed on its own invoice (hidden)
            dt.Columns.Add("PeriodByContract", typeof(bool));  // #16: invoice display dates follow the contract cycle (hidden)
            dt.Columns.Add("RentalBillingDay", typeof(int));   // rental-separate invoice's own day; 0 = follow meter date (hidden)
            dt.Columns.Add("RentalStartDate", typeof(DateTime)); // rental period anchor (hidden; n/N)
            dt.Columns.Add("RentalMonths", typeof(int));       // rental total periods N (hidden)
            dt.Columns.Add("RentalBasis", typeof(string));     // A accrual / P prepayment (hidden)
            dt.Columns.Add("NeedManual", typeof(bool));        // SNAPSHOT tab membership: set at load/fetch, NOT live —
                                                               // keying a reading must not make the row vanish mid-work (hidden)
            dt.Columns.Add("Locked", typeof(bool));            // billing-day auto-fetch snapshot lock — reading frozen (hidden)
            dt.Columns.Add("HasConflict", typeof(bool));       // saved-manual value differs from a fresh API value (hidden)
            dt.Columns.Add("IsExpired", typeof(bool));         // effective expiry BEFORE the billing month (row tinted; Generate warns)
            dt.Columns.Add("EffStart", typeof(DateTime));      // effective service start (RENTAL-FREE-N month anchor; hidden)
            dt.Columns.Add("ContractStart", typeof(DateTime)); // CONTRACT service start (#16 period anchor - one period per invoice; hidden)
            dt.Columns.Add("InvoicedDocNo", typeof(string));   // non-empty = this meter+period is already invoiced (hidden)
            return dt;
        }

        // Columns the user may never see (data drivers / plumbing) — never offered in View Setting.
        private static readonly string[] _systemHiddenCols = new string[] { "ItemKey", "ContractKey", "DebtorCode", "BillingMode", "ItemMeterKey", "ACItemCode", "Shade", "Sel", "InvoicedDocNo", "ItemDesc", "NeedManual", "Locked", "IsFlat", "IsWaive", "IsGroupItem", "MachineMode", "TrackingId", "WaiveFirstNMonths", "WaiveTargetAmount", "WaivePartialThreshold", "WaivePartialAmount", "WaiveScope", "StrategyCode", "RentSep", "PeriodByContract", "ContractStart", "RentalBillingDay", "RentalStartDate", "RentalMonths", "RentalBasis", "IsExpired", "EffStart" };

        // Hidden by DEFAULT but user-selectable (column chooser / View Setting).
        private static readonly string[] _optionalCols = new string[] { "Mode", "BillingDay", "UseMin", "MultiPriceCode", "FOCResetUnit", "FOCResetN", "Status", "EntrySource", "FetchedReading", "HasConflict", "BillGroupCode", "Role" };

        // Everything View Setting lets the user toggle. Excludes the columns ApplyTabColumnLayout
        // owns (it re-forces their Visible on every tab switch, so a user choice there would not
        // stick), the Customer group column and the system drivers above.
        private static readonly string[] _viewSettingCols = new string[] { "ContractNo", "ServiceItemNo", "SerialNo", "Mode", "BillingDay", "MeterType", "MeterTypeName", "LastReadDate", "LastFetchDate", "LastReading", "LastInvNo", "LastInvDate", "UseMin", "Status", "EntrySource", "FetchedReading", "HasConflict", "BillGroupCode", "MultiPriceCode", "FOCResetUnit", "FOCResetN", "Role" };

        // ConfigureGrid is pure column/view SHAPING (captions, widths, default visibility, appearance,
        // summaries) — the DataTable schema is identical on every load, so re-running it only undoes
        // the user's own layout: it would wipe the restored native layout and any View Setting choice
        // on every Refresh/Fetch. Shape once, then leave the grid alone.
        private bool _gridShaped;

        private void ConfigureGrid()
        {
            if (_gridShaped) return;
            _gridShaped = true;
            GridViewMeter.OptionsBehavior.Editable = true;
            // Open the in-place editor on MOUSE DOWN: without this, the first click on a (merged)
            // Select cell only focuses it and the checkbox never toggles on a single click.
            GridViewMeter.OptionsBehavior.EditorShowMode = DevExpress.Utils.EditorShowMode.MouseDown;
            // Merged-cell look (ONE checkbox / contract / CSSI / serial / status cell spanning the
            // machine's BK+CL rows) UNDER collapsible "Customer: ..." group rows.
            GridViewMeter.OptionsView.AllowCellMerge = true;
            GridViewMeter.OptionsView.ShowGroupPanel = false;
            GridViewMeter.OptionsBehavior.AutoExpandAllGroups = true;
            GridColumn cGrpCust = GridViewMeter.Columns["Customer"];
            if (cGrpCust != null) cGrpCust.GroupIndex = 0;
            GridViewMeter.OptionsView.ShowFooter = true;       // footer band for totals
            GridViewMeter.OptionsView.ColumnAutoWidth = false;

            // "Sel" (per-meter) stays as the hidden data driver; the user only sees the merged
            // per-CSSI "Select" checkbox (SelCssi), which drives both meter rows.
            foreach (string h in _systemHiddenCols)
                if (GridViewMeter.Columns[h] != null)
                {
                    GridViewMeter.Columns[h].Visible = false;
                    GridViewMeter.Columns[h].OptionsColumn.ShowInCustomizationForm = false;
                }
            // Locked rows: the Current Reading cell refuses to open its editor (snapshot is frozen).
            GridViewMeter.ShowingEditor -= new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditorLock);
            GridViewMeter.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewMeter_ShowingEditorLock);

            // Advanced filtering: per-column auto-filter row under the headers (+ the header funnel
            // menus). User filters layer on top of the tab's own DataView — they can never break the
            // tab's system scope.
            GridViewMeter.OptionsView.ShowAutoFilterRow = true;

            // Secondary columns hidden BY DEFAULT to keep the grid focused — still available through
            // the column chooser (right-click the header -> Column Chooser).
            // MachineStatus is VISIBLE now (green=ONLINE / orange=OFFLINE cell tint) — it replaced the
            // old Online/Offline tabs.
            foreach (string h in _optionalCols)
            {
                GridColumn hc = GridViewMeter.Columns[h];
                if (hc == null) continue;
                hc.Visible = false;
                hc.OptionsColumn.ShowInCustomizationForm = true;
            }
            GridColumn cBillGrp = GridViewMeter.Columns["BillGroupCode"];
            if (cBillGrp != null)
            {
                cBillGrp.Caption = "Bill Group"; cBillGrp.Width = 70;
                // Display-only here: the code is maintained on the CONTRACT (item grid); an edit in
                // this grid would regroup one run without persisting — silently reverting next load.
                cBillGrp.OptionsColumn.AllowEdit = false;
                cBillGrp.OptionsColumn.ReadOnly = true;
            }

            SetCol("SelCssi", "Select", 55, true);
            GridColumn selc = GridViewMeter.Columns["SelCssi"];
            if (selc != null)
            {
                // Sorting by the checkbox makes ticked rows jump position mid-click — disable it.
                selc.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
                selc.SortOrder = DevExpress.Data.ColumnSortOrder.None;
                // Commit the toggle on the click itself (no waiting for focus to leave the cell).
                if (_selCssiEditor == null)
                {
                    _selCssiEditor = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
                    _selCssiEditor.EditValueChanged += new EventHandler(SelCssiEditor_EditValueChanged);
                    GridMeter.RepositoryItems.Add(_selCssiEditor);
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
            SetCol("LastAuditDate", "Last Audit Date", 110, false);
            SetCol("LastFetchDate", "Last Fetch Date", 115, false);
            GridColumn cFd = GridViewMeter.Columns["LastFetchDate"];
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
            GridColumn cInvDt = GridViewMeter.Columns["LastInvDate"];
            if (cInvDt != null)
            {
                cInvDt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                cInvDt.DisplayFormat.FormatString = "dd/MM/yyyy";
            }
            SetNum("InvTotal", "Invoice Total", 95, false, "n2");
            GridColumn cInvTot = GridViewMeter.Columns["InvTotal"];
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
            GridViewMeter.RowCellStyle -= new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewMeter_LastInvCellStyle);
            GridViewMeter.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewMeter_LastInvCellStyle);

            // Highlight the two columns the operator actually works with: Current Reading (amber —
            // the key-in column) and Total Charges (green — the money). Column-level cell appearance
            // outranks the RowStyle shading, so the tint shows on every row.
            GridColumn cCurHl = GridViewMeter.Columns["CurrentReading"];
            if (cCurHl != null)
            {
                cCurHl.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(255, 249, 196);
                cCurHl.AppearanceCell.Options.UseBackColor = true;
                cCurHl.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
                cCurHl.AppearanceHeader.Options.UseFont = true;
            }
            GridColumn cChgHl = GridViewMeter.Columns["TotalCharges"];
            if (cChgHl != null)
            {
                cChgHl.AppearanceCell.BackColor = System.Drawing.Color.FromArgb(223, 240, 216);
                cChgHl.AppearanceCell.Options.UseBackColor = true;
                cChgHl.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
                cChgHl.AppearanceHeader.Options.UseFont = true;
            }

            // Freeze the identifier columns on the left so they stay visible when scrolling horizontally.
            // (Requires ColumnAutoWidth = false, set above.)
            foreach (string fx in new string[] { "SelCssi", "ContractNo", "ServiceItemNo", "SerialNo" })
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

        private DateTime? _periodCutoff;   // selected billing day of the selected month (clamped)


        // Auto-fetch snapshot rows are FROZEN — no manual override of the Current Reading.
        private void GridViewMeter_ShowingEditorLock(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewMeter.FocusedColumn == null || GridViewMeter.FocusedColumn.FieldName != "CurrentReading") return;
            DataRow r = GridViewMeter.GetDataRow(GridViewMeter.FocusedRowHandle);
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
                DataRow rc = GridViewMeter.GetDataRow(e.RowHandle);
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
                object av = GridViewMeter.GetRowCellValue(e.RowHandle, "LastAuditDate");
                if (av == null || av == DBNull.Value || !_periodCutoff.HasValue) return;
                object invd = GridViewMeter.GetRowCellValue(e.RowHandle, "InvoicedDocNo");
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
                object msv = GridViewMeter.GetRowCellValue(e.RowHandle, "MachineStatus");
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
            object inv = GridViewMeter.GetRowCellValue(e.RowHandle, "InvoicedDocNo");
            if (inv == null || inv == DBNull.Value || inv.ToString().Trim().Length == 0) return;
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(200, 230, 201);
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(27, 94, 32);
            e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
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
                    _dbSetting.ExecuteNonQuery("DELETE FROM dbo.LayoutUsers WHERE Title = N'" + t + "'");
                    _dbSetting.ExecuteNonQuery("DELETE FROM dbo.Layout WHERE Title = N'" + t + "'");
                    _gridLayout = null;   // stop the FormClosed auto-save from resurrecting it this session
                    XtraMessageBox.Show("Your saved grid layout was cleared — reopen the module for the factory layout.",
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
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

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
                        if (dto.LastAuditDate.HasValue) r["LastAuditDate"] = dto.LastAuditDate.Value;
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
            GridViewMeter.CloseEditor();

            List<DataRow> visible = new List<DataRow>();
            for (int rh = 0; rh < GridViewMeter.RowCount; rh++)
            {
                DataRow r = GridViewMeter.GetDataRow(rh);
                if (r != null) visible.Add(r);
            }
            if (visible.Count == 0)
            { XtraMessageBox.Show("No rows on this tab.", "Select All"); return; }

            bool anyUnticked = false;
            foreach (DataRow r in visible)
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) { anyUnticked = true; break; }

            foreach (DataRow r in visible) r["Sel"] = anyUnticked;
            SyncSelCssi();
            GridMeter.RefreshDataSource();
            UpdateTabCounts();
            UpdateSelectAllButton();   // Select All <-> Unselect All
        }

        // Button caption mirrors the CURRENT TAB's state: all visible rows ticked -> "Unselect All",
        // otherwise "Select All".
        private void UpdateSelectAllButton()
        {
            if (_dtGrid == null) return;
            bool anyRow = false, anyUnticked = false;
            for (int rh = 0; rh < GridViewMeter.RowCount; rh++)
            {
                DataRow r = GridViewMeter.GetDataRow(rh);
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
            if (_dtGrid == null) return;
            Dictionary<long, DataRow> byMeter = new Dictionary<long, DataRow>();
            string sql = "SELECT ItemMeterKey, CurrentReading, ReadingDate, Source, InvoicedDocNo, InvoicedAt, LockedAt, LastModified, " +
                         "ISNULL(TrackingId,'') AS TrackingId " +
                         "FROM dbo.zSCP2_MeterEntry WHERE PeriodYear=" + year + " AND PeriodMonth=" + month;
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
                r["TrackingId"] = S(s["TrackingId"]).Trim();
                if (s["ReadingDate"] != DBNull.Value) r["LastAuditDate"] = Convert.ToDateTime(s["ReadingDate"]);
                if (s["LastModified"] != DBNull.Value) r["LastFetchDate"] = Convert.ToDateTime(s["LastModified"]);
                Recalc(r);   // restored but NOT auto-selected — user picks rows before generating
                string dt = (s["ReadingDate"] != DBNull.Value) ? "  " + Convert.ToDateTime(s["ReadingDate"]).ToString("dd/MM/yyyy") : "";
                if (src == "ONLINE") { r["MachineStatus"] = "ONLINE"; r["Status"] = "Online (saved)" + dt; }
                else if (src == "OFFLINE") { r["MachineStatus"] = "OFFLINE"; r["Status"] = "Offline (saved)" + dt; }
                else { r["Status"] = "Manual (saved)" + dt; }

                // Billing-day auto-fetch snapshot: frozen — no fetch/manual override until invoiced.
                if (s["LockedAt"] != DBNull.Value)
                {
                    r["Locked"] = true;
                    r["Status"] = "🔒 AUTO-FETCHED (locked " +
                        Convert.ToDateTime(s["LockedAt"]).ToString("dd/MM/yyyy HH:mm") + ")" + dt;
                }

                // Billing-period guard: already invoiced for this period -> show it and lock out of Generate.
                string invNo = S(s["InvoicedDocNo"]).Trim();
                if (invNo.Length > 0)
                {
                    r["InvoicedDocNo"] = invNo;
                    r["LastInvNo"] = invNo;
                    if (s["InvoicedAt"] != DBNull.Value) r["LastInvDate"] = Convert.ToDateTime(s["InvoicedAt"]);
                    string invAt = (s["InvoicedAt"] != DBNull.Value) ? "  " + Convert.ToDateTime(s["InvoicedAt"]).ToString("dd/MM/yyyy") : "";
                    r["Status"] = "INVOICED " + invNo + invAt;
                }
            }
            SyncSelCssi();
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
                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, ReadingDate=@dt, Source=@src, TrackingId=@tid, LastModified=GETDATE() " +
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
            if (GridViewMeter.FocusedColumn == null ||
                GridViewMeter.FocusedColumn.FieldName != "CurrentReading") return;
            DataRow r = GridViewMeter.GetDataRow(GridViewMeter.FocusedRowHandle);
            if (r == null || !IsInlineEditable(r)) e.Cancel = true;
        }

        // Demo #2(a): the grid twin of the detail dialog's save (manual / cleared-to-0 branches).
        // One short transaction per committed cell. Deliberately does NOT touch NeedManual — tab
        // membership stays a snapshot until the next Refresh/Fetch, same as the dialog.
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
                d["FocResetCount"] = FocResetCountFor(r);
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
            GridMeter.RefreshDataSource();
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
                GridMeter.RefreshDataSource();
                return;
            }

            if (applied > 0)
                XtraMessageBox.Show(applied + " reading(s) saved.", "Meter Reading",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // One click on the Select checkbox posts immediately (fires CellValueChanged right away).
        private void SelCssiEditor_EditValueChanged(object sender, EventArgs e)
        {
            GridViewMeter.PostEditor();
        }

        // One left-click anywhere on the (merged) Select cell toggles the whole machine.
        private void GridViewMeter_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button != System.Windows.Forms.MouseButtons.Left) return;
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = GridViewMeter.CalcHitInfo(e.Location);
            if (hit.RowHandle < 0 || hit.Column == null || hit.Column.FieldName != "SelCssi" || !hit.InRowCell) return;
            object cur = GridViewMeter.GetRowCellValue(hit.RowHandle, "SelCssi");
            bool v = !(cur != null && cur != DBNull.Value && Convert.ToBoolean(cur));
            GridViewMeter.SetRowCellValue(hit.RowHandle, "SelCssi", v);
            DevExpress.Utils.DXMouseEventArgs dx = DevExpress.Utils.DXMouseEventArgs.GetMouseArgs(e);
            if (dx != null) dx.Handled = true;
        }

        private void GridViewMeter_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMin")
            {
                DataRow r = GridViewMeter.GetDataRow(e.RowHandle);
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
                DataRow r = GridViewMeter.GetDataRow(e.RowHandle);
                if (r != null && _dtGrid != null)
                {
                    bool v = r["SelCssi"] != DBNull.Value && Convert.ToBoolean(r["SelCssi"]);
                    long ik = D64(r["ItemKey"]);
                    foreach (DataRow x in _dtGrid.Rows)
                        if (D64(x["ItemKey"]) == ik) { x["Sel"] = v; x["SelCssi"] = v; }
                    GridViewMeter.LayoutChanged();   // repaint merged cells; no full rebind mid-edit
                }
            }
            else if (e.Column.FieldName == "Sel")
            {
                // Per-meter tick: mirror the CSSI checkbox = "all this machine's meters are ticked".
                DataRow r = GridViewMeter.GetDataRow(e.RowHandle);
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
        private int FocResetCountFor(DataRow r)
        {
            string unit = r.Table.Columns.Contains("FOCResetUnit") ? S(r["FOCResetUnit"]) : "M";
            int n = r.Table.Columns.Contains("FOCResetN") && r["FOCResetN"] != DBNull.Value ? Convert.ToInt32(r["FOCResetN"]) : 0;
            int periodDays = System.DateTime.DaysInMonth(SelectedYear(), SelectedMonth());
            return ServiceContractPhotocopier.Classes.ScpMultiPrice.FocResetCount(unit, n, periodDays);
        }

        // Grid preview charge = the SAME engine the invoice uses (ScpInvoiceBuilder.ComputeCharge), so grid
        // and invoice can never diverge: NET (FOC copies + rebate deducted) + multi-price tier + min floor.
        private void Recalc(DataRow r)
        {
            MeterBillLine ln = new MeterBillLine();
            ln.IsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            ln.Current = Dec(r["CurrentReading"]);
            ln.Last = Dec(r["LastReading"]);
            ln.Rate = Dec(r["UnitPrice"]);
            ln.MinCharges = Dec(r["MinCharges"]);
            ln.Foc = Dec(r["FOCQty"]);
            ln.RebatePct = Dec(r["RebatePct"]);
            ln.MultiPriceCode = S(r["MultiPriceCode"]);
            ln.FocResetCount = FocResetCountFor(r);
            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, _ladders);
            // Honour the user's "Use Min." tick (force the minimum charge) without clobbering their input.
            if (r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"])) ln.Charge = ln.MinCharges;
            r["MeterUsage"] = ln.Usage;
            r["TotalCharges"] = ln.Charge;
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _dtGrid == null) return;
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            // Group the selected meter lines: Group mode = one invoice per contract; Separate = per item.
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs =
                new Dictionary<string, MeterInvoiceGenerator.InvoiceJob>();
            int genMonth = SelectedMonth(), genYear = SelectedYear();
            int alreadyInvoiced = 0;
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
                        tickedDebtors.Add(S(dr["DebtorCode"]));
                List<string> problems = new List<string>();
                foreach (DataRow dr in visibleRows)
                {
                    if (!tickedDebtors.Contains(S(dr["DebtorCode"]))) continue;
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
                HashSet<long> tickedContracts = new HashSet<long>();
                foreach (DataRow dr in visibleRows)
                    if (dr["Sel"] != DBNull.Value && Convert.ToBoolean(dr["Sel"])
                        && S(dr["InvoicedDocNo"]).Trim().Length == 0
                        && S(dr["BillingMode"]).Trim().ToUpperInvariant() != "S")
                        tickedContracts.Add(D64(dr["ContractKey"]));
                if (tickedContracts.Count > 0)
                {
                    List<string> problems = new List<string>();
                    foreach (DataRow dr in visibleRows)
                    {
                        if (S(dr["BillingMode"]).Trim().ToUpperInvariant() == "S") continue;
                        if (!tickedContracts.Contains(D64(dr["ContractKey"]))) continue;
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
                            "Nothing was generated.",
                            "Incomplete contract group", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // Demo 28/07 #6: bill-group completeness — a Bill Group is billed as ONE invoice, so a
            // partially ticked / partially keyed group must not slip out as a partial invoice. Runs
            // regardless of the grouping setting (the Bill Group overrides it).
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

            foreach (DataRow r in visibleRows)
            {
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) continue;
                // Billing-period guard: this meter + period already has an invoice — never bill twice.
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) { alreadyInvoiced++; continue; }
                // Usage meters need a reading > 0 to bill; flat/rental meters bill on their flat amount
                // even with reading 0 (that is the whole point — no meter to read).
                bool rowFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (!rowFlat && Dec(r["CurrentReading"]) <= 0m) continue;
                // Invoice grouping (SETTING, 2026-08-04 — closes ISSUES.md D-2):
                //   FOLLOW  = the contract's own Billing Mode decides: G -> ONE invoice per CONTRACT
                //             ("Group Services into One Invoice"), S -> one per machine.
                //   DEBTOR  = legacy override: ONE invoice per CUSTOMER across all their contracts.
                //   MACHINE = force one invoice per machine.
                long contractKey = D64(r["ContractKey"]);
                long itemKey = D64(r["ItemKey"]);
                string mode;
                if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR) mode = "G";
                else if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) mode = "S";
                else mode = S(r["BillingMode"]).Trim().ToUpperInvariant() == "S" ? "S" : "G";
                string groupKey = mode == "S"
                    ? ("C" + contractKey + "_I" + itemKey)
                    : (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR
                        ? ("D" + S(r["DebtorCode"]))
                        : ("C" + contractKey));
                // Demo 28/07 #6: an explicit Bill Group on the machine OVERRIDES the grouping setting
                // (same philosophy as the per-contract template #9c): same contract + same code = ONE
                // invoice. Bill Group splits INVOICES only — never the deal scope (strategy passes stay
                // contract-wide). Fleet group machines (IsGroupItem) never carry a code -> legacy key,
                // so their fleet-total MIN/WAIVE lines stay on the invoice that has the prints.
                string billGroup = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]));
                bool rowIsGroupItem = r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
                if (rowIsGroupItem) billGroup = "";
                // Contract flag "Rental separate invoice": flat (rental/min) lines split into their own
                // job — one extra invoice per group ("Rental- [ref]") instead of riding the meter invoice.
                bool rentSep = r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]);
                // Only actual RENTAL meters split onto the rental-separate invoice — a committed-minimum
                // ("MIN ...") meter is a print charge and must stay on the meter invoice with BK/CL.
                bool rentalJob = rowFlat && rentSep && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(S(r["MeterType"]));
                string refNo = mode == "S" ? S(r["ServiceItemNo"]) : S(r["ContractNo"]);
                if (billGroup.Length > 0) { groupKey = "C" + contractKey + "_G" + billGroup; refNo = S(r["ContractNo"]); }
                if (rentalJob) groupKey += "_R";   // rental suffix composes: 2 groups x rentSep = 4 jobs

                MeterBillLine ln = new MeterBillLine();
                ln.ItemKey = itemKey;
                ln.ContractKey = contractKey;
                ln.ItemMeterKey = D64(r["ItemMeterKey"]);
                ln.ContractNo = S(r["ContractNo"]);
                ln.ItemNo = S(r["ServiceItemNo"]);
                ln.DebtorCode = S(r["DebtorCode"]);
                ln.SerialNumber = S(r["SerialNo"]);
                ln.ItemName = S(r["ServiceItemNo"]);
                ln.ItemDesc = S(r["ItemDesc"]);
                ln.MeterTypeCode = S(r["MeterType"]);
                ln.MeterTypeName = S(r["MeterTypeName"]);
                ln.ACItemCode = S(r["ACItemCode"]);
                // Role-aware label: NA meters (scan/plotter/other usage) must NOT masquerade as "Black" —
                // that folded them into committed-min print sums and BK-scoped strategy passes.
                string roleUp = S(r["Role"]).Trim().ToUpperInvariant();
                ln.ColorLabel = roleUp == "CL" ? "Colour" : (roleUp == "BK" ? "Black" : "Usage");
                ln.Last = Dec(r["LastReading"]);
                ln.Current = Dec(r["CurrentReading"]);
                ln.Usage = Dec(r["MeterUsage"]);
                ln.Rate = Dec(r["UnitPrice"]);
                ln.MinCharges = Dec(r["MinCharges"]);
                ln.Foc = Dec(r["FOCQty"]);
                ln.RebatePct = Dec(r["RebatePct"]);
                ln.MultiPriceCode = S(r["MultiPriceCode"]);
                ln.FocResetCount = FocResetCountFor(r);
                ln.IsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                // Charge via the SAME engine the grid used, so the invoice matches the preview exactly
                // (NET: FOC copies + rebate deducted, multi-price tier, min floor). Sets BillCopies / EffUnitPrice.
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, _ladders);
                if (r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"])) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
                ln.IsRental = ln.IsFlat && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(S(r["MeterType"]));
                // The machine's DEFINED mode beats the live fetch status (deterministic numbering).
                string mmode = S(r["MachineMode"]).Trim().ToUpperInvariant();
                ln.MachineStatus = mmode.Length > 0 ? mmode : S(r["MachineStatus"]);
                ln.TrackingId = S(r["TrackingId"]).Trim();
                ln.IsGroupItem = r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
                ln.IsWaiveMeter = r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);
                // Scope is shared: waive meters use it for the target sum, MIN meters for the
                // committed-minimum printed sum (BK only / CL only / both).
                string wsc = S(r["WaiveScope"]).Trim().ToUpperInvariant();
                ln.WaiveScope = wsc == "BK" || wsc == "CL" ? wsc : "BKCL";
                if (ln.IsWaiveMeter)
                {
                    ln.WaiveFirstNMonths = r["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["WaiveFirstNMonths"]);
                    ln.WaiveTargetAmount = Dec(r["WaiveTargetAmount"]);
                    ln.WaivePartialThreshold = Dec(r["WaivePartialThreshold"]);
                    ln.WaivePartialAmount = Dec(r["WaivePartialAmount"]);
                }
                // Committed-minimum ("MIN ...") meter: bill the TOP-UP to the committed amount over the
                // item's print charges (computed in ApplyCommittedMin), and always show it (transparency).
                if (ln.IsFlat && ln.MinCharges > 0m &&
                    ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinMeterCode(ln.MeterTypeCode))
                {
                    ln.IsCommittedMin = true;
                    ln.CommittedAmount = ln.MinCharges;
                    ln.AlwaysBill = true;
                }
                ln.StrategyCode = S(r["StrategyCode"]);
                if (r["RentalStartDate"] != DBNull.Value) ln.RentalStartDate = Convert.ToDateTime(r["RentalStartDate"]);
                ln.RentalMonths = r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]);
                ln.RentalBasis = S(r["RentalBasis"]) == "P" ? 'P' : 'A';
                if (r["EffStart"] != DBNull.Value) ln.EffStartDate = Convert.ToDateTime(r["EffStart"]);
                if (S(r["EntrySource"]) == "RENTAL FREE") { ln.StrategyNote = "RENTAL FREE - FOC month"; ln.AlwaysBill = true; }
                if (r["LastReadDate"] != DBNull.Value) ln.LastDate = Convert.ToDateTime(r["LastReadDate"]);
                if (r["LastAuditDate"] != DBNull.Value) ln.AuditDate = Convert.ToDateTime(r["LastAuditDate"]);
                // #16: contract-cycle billing period for the invoice DISPLAY (flag on the contract).
                // Anchor day = the CONTRACT's service start day (contract-level on purpose: one
                // invoice shows ONE period even when items carry their own start-date overrides);
                // fallback billing day, then 1. The period is the cycle window that ENDS in the
                // billed month — the one this month's reading actually measured (user-verified):
                //   day-1 contract, July run  -> 01/07 - 31/07 (the billed month itself)
                //   day-25 contract, Aug run  -> 25/07 - 24/08
                if (r["PeriodByContract"] != DBNull.Value && Convert.ToBoolean(r["PeriodByContract"]))
                {
                    int anchorDay = r["ContractStart"] != DBNull.Value ? Convert.ToDateTime(r["ContractStart"]).Day
                        : (r["BillingDay"] != DBNull.Value && Convert.ToInt32(r["BillingDay"]) >= 1 ? Convert.ToInt32(r["BillingDay"]) : 1);
                    if (anchorDay <= 1)
                    {
                        ln.PeriodStart = new DateTime(genYear, genMonth, 1);
                        ln.PeriodEnd = new DateTime(genYear, genMonth, DateTime.DaysInMonth(genYear, genMonth));
                    }
                    else
                    {
                        int dimB = DateTime.DaysInMonth(genYear, genMonth);
                        ln.PeriodEnd = new DateTime(genYear, genMonth, (anchorDay - 1) > dimB ? dimB : (anchorDay - 1));
                        int py = genYear, pm = genMonth - 1;
                        if (pm < 1) { pm = 12; py--; }
                        int dimP = DateTime.DaysInMonth(py, pm);
                        ln.PeriodStart = new DateTime(py, pm, anchorDay > dimP ? dimP : anchorDay);
                    }
                }

                MeterInvoiceGenerator.InvoiceJob job;
                if (!jobs.TryGetValue(groupKey, out job))
                {
                    job = new MeterInvoiceGenerator.InvoiceJob();
                    job.DebtorCode = ln.DebtorCode;
                    job.RefDocNo = refNo;
                    // Invoice date = the BILLED PERIOD's billing day (row's effective day, clamped),
                    // so a May run dated 28/05 lands in the MR2605.* number series — never "today".
                    int effDay = r["BillingDay"] == DBNull.Value ? 28 : Convert.ToInt32(r["BillingDay"]);
                    int dim = DateTime.DaysInMonth(genYear, genMonth);
                    job.DocDate = new DateTime(genYear, genMonth, effDay > dim ? dim : (effDay < 1 ? 1 : effDay));
                    // Demo 28/07 #18: the rental-separate job gets its OWN invoice day when the
                    // contract sets one ("rental 是跟头标的,meter 是跟尾标的"): accrual rentals date
                    // in the billing month, prepayment rentals in the NEXT month (June run -> July 1).
                    if (rentalJob)
                    {
                        int rentDay = r.Table.Columns.Contains("RentalBillingDay") && r["RentalBillingDay"] != DBNull.Value
                            ? Convert.ToInt32(r["RentalBillingDay"]) : 0;
                        if (rentDay >= 1 && rentDay <= 28)
                        {
                            int ry = genYear, rm = genMonth;
                            if (S(r["RentalBasis"]) == "P") { rm++; if (rm > 12) { rm = 1; ry++; } }
                            int rdim = DateTime.DaysInMonth(ry, rm);
                            job.DocDate = new DateTime(ry, rm, rentDay > rdim ? rdim : rentDay);
                        }
                    }
                    // Legacy header text (verified against the customer's V8 meter invoices);
                    // rental-only invoices get their own header so the two are distinguishable.
                    job.Description = (rentalJob ? "Rental- [" : "Billing- [") + refNo
                        + (billGroup.Length > 0 ? " / " + billGroup : "") + "]";
                    job.Label = refNo + (billGroup.Length > 0 ? " / " + billGroup : "") + (rentalJob ? " (rental)" : "");
                    job.Lines = new List<MeterBillLine>();
                    jobs[groupKey] = job;
                }
                job.Lines.Add(ln);
            }

            // Strategy passes over the whole run (cross-machine — cannot live in the per-row grid preview).
            // ALL rule kinds act LIVE here — no "Apply to Meters" push step exists any more:
            //   1) GROUP FOC LIMIT   — pools a shared free-copy quota across the group's machines,
            //   2) RENTAL-FREE-N     — stateless: billing month number vs the rental's start month,
            //   3) WAIVE-TARGET      — waives rental when the (net-of-pool) usage charges reach the target,
            //   4) COMMITTED MIN     — MIN meters AND COMMIT-MIN rules top up the item's print charges.
            // A failure here ABORTS the run: silently billing WITHOUT the strategy would produce a wrong
            // invoice with no error — worse than no invoice.
            Dictionary<long, StrategyDef> runStrats;
            Dictionary<long, string> runSnapshots;
            try
            {
                HashSet<long> runCks = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb0 in jobs.Values)
                    foreach (MeterBillLine l0 in jb0.Lines)
                        if (l0.ContractKey > 0) runCks.Add(l0.ContractKey);
                runStrats = ServiceContractPhotocopier.Classes.ScpStrategy.LoadForContracts(_dbSetting, runCks);
                runSnapshots = BuildContractSnapshots(runCks, runStrats, genYear, genMonth);
                ApplyGroupLimit(jobs, runStrats);
                ApplyRentalFreeN(jobs, runStrats, genYear, genMonth);
                ApplyWaiveMeters(jobs, genYear, genMonth);   // waive METERS (master-style contra, engine-decided)
                ApplyWaiveTarget(jobs, runStrats);
                ApplyCommittedMin(jobs, runStrats);
            }
            catch (Exception stratEx)
            {
                XtraMessageBox.Show("Strategy evaluation failed — generation ABORTED, no invoice was created.\r\n\r\n" +
                    stratEx.Message + "\r\n\r\nFix the cause (or clear the contract's strategy rules) and Generate again.",
                    "Generate Invoice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Offline readings reference their monthly report by TrackingId — when any line in a job
            // has one, the invoice's Reference No becomes the job's DISTINCT ids, comma-joined in line
            // order ("MR-260724-041, MR-260724-052"). IV.RefDocNo is nvarchar(30): only as many WHOLE
            // ids as fit are joined — an id is never cut in half. No ids -> the usual CSSI/contract ref.
            // Demo 28/07 #4: only BK/CL rows are staged with a TrackingId, so a rental-separate
            // ("_R") or flat-only job carried none even when the SAME machine's offline report id
            // was sitting on its usage rows — every line falls back to its MACHINE's id.
            Dictionary<long, string> tidByItem = new Dictionary<long, string>();
            foreach (DataRow tidRow in _dtGrid.Rows)
            {
                string tidCell = S(tidRow["TrackingId"]).Trim();
                if (tidCell.Length == 0) continue;
                long tidItem = D64(tidRow["ItemKey"]);
                if (!tidByItem.ContainsKey(tidItem)) tidByItem[tidItem] = tidCell;
            }
            foreach (MeterInvoiceGenerator.InvoiceJob jbT in jobs.Values)
            {
                List<string> tids = new List<string>();
                foreach (MeterBillLine lT in jbT.Lines)
                {
                    string tid = (lT.TrackingId ?? "").Trim();
                    if (tid.Length == 0) tidByItem.TryGetValue(lT.ItemKey, out tid);
                    tid = (tid ?? "").Trim();
                    if (tid.Length > 0 && !tids.Contains(tid)) tids.Add(tid);
                }
                if (tids.Count == 0) continue;
                string joined = "";
                foreach (string tid in tids)
                {
                    string next = joined.Length == 0 ? tid : joined + ", " + tid;
                    if (next.Length > 30) break;
                    joined = next;
                }
                if (joined.Length > 0) jbT.RefDocNo = joined;
            }

            // Progress dialog shows WHICH MACHINE(S) are being billed — service item nos, not the contract.
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
            {
                List<string> nos = new List<string>();
                foreach (MeterBillLine l2 in jb.Lines)
                    if (!string.IsNullOrEmpty(l2.ItemNo) && !nos.Contains(l2.ItemNo)) nos.Add(l2.ItemNo);
                string lbl = string.Join(", ", nos.ToArray());
                if (lbl.Length > 70) lbl = lbl.Substring(0, 67) + "…(" + nos.Count + " items)";
                if (lbl.Length > 0) jb.Label = lbl;
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
            if (XtraMessageBox.Show(this,
                    "Generate " + jobList.Count + " invoice(s) for " + monthName + " now?\r\nThey are saved automatically — no clicking through each one." + skippedNote + expiredNote,
                    "Generate Invoice", MessageBoxButtons.YesNo,
                    expiredTicked > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Run the generation on a worker task behind a progress dialog (invoices saved headlessly).
            using (MeterInvoiceGenerateProgress_Form dlg =
                new MeterInvoiceGenerateProgress_Form(_dbSetting, jobList, DateTime.Today, DateTime.Now, genYear, genMonth, runSnapshots))
            {
                dlg.ShowDialog(this);
            }
            LoadData();
        }

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
        private void ApplyGroupLimit(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats)
        {
            {
                HashSet<long> cks = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (!l.IsFlat && l.ContractKey > 0) cks.Add(l.ContractKey);
                if (cks.Count == 0 || strats == null || strats.Count == 0) return;

                foreach (long ck in cks)
                {
                    StrategyDef sd;
                    if (!strats.TryGetValue(ck, out sd)) continue;
                    foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_LIMIT))
                    {
                        // LIMIT is ALWAYS group-pooled now (the builder forces 'G'); legacy 'S' rows are
                        // pooled too rather than silently doing nothing.
                        if (rule.LimitQty <= 0m) continue;
                        // Gather this contract's covered usage lines (scope + item set), deterministic order.
                        // Only BK/CL print meters join the pool ("BK+CL usage" scope means exactly that —
                        // NA meters like scan/plotter stay out). Ladder meters stay out too: their FOC lives
                        // in the ladder's own 0.00 band and ComputeCharge ignores ln.Foc for them, so a pool
                        // share allocated there would be silently DISCARDED (the group would lose free copies).
                        List<MeterBillLine> members = new List<MeterBillLine>();
                        foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                            foreach (MeterBillLine l in jb.Lines)
                            {
                                if (l.IsFlat || l.ContractKey != ck) continue;
                                if (l.ColorLabel != "Black" && l.ColorLabel != "Colour") continue;
                                if (ServiceContractPhotocopier.Classes.ScpMultiPrice.HasLadder(_ladders, l.MultiPriceCode)) continue;
                                if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                                if (rule.Scope == "BK" && l.ColorLabel != "Black") continue;
                                if (rule.Scope == "CL" && l.ColorLabel != "Colour") continue;
                                members.Add(l);
                            }
                        members.Sort(delegate (MeterBillLine a, MeterBillLine b)
                        {
                            int c = a.ItemKey.CompareTo(b.ItemKey);
                            return c != 0 ? c : a.ItemMeterKey.CompareTo(b.ItemMeterKey);
                        });

                        if (members.Count == 0) continue;
                        // Total RAW usage of the group; the pool frees min(LimitQty, total usage).
                        decimal totalUsage = 0m;
                        foreach (MeterBillLine l in members) totalUsage += l.Usage;
                        if (totalUsage <= 0m) continue;
                        decimal poolUsed = Math.Min(rule.LimitQty, totalUsage);

                        // Proportional share per machine (whole copies; remainder to the last so the shares
                        // sum EXACTLY to poolUsed). The share REPLACES the machine's per-meter FOC.
                        decimal allocated = 0m;
                        for (int i = 0; i < members.Count; i++)
                        {
                            MeterBillLine l = members[i];
                            decimal share = (i == members.Count - 1)
                                ? poolUsed - allocated
                                : Math.Round(poolUsed * l.Usage / totalUsage, 0, MidpointRounding.AwayFromZero);
                            if (share < 0m) share = 0m;
                            if (share > l.Usage) share = l.Usage;   // never free more than this machine printed
                            allocated += share;
                            l.Foc = share;   // REPLACE per-meter FOC with the group pool share
                            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
                            l.StrategyNote = "GROUP FOC: " + share.ToString("0") + " free (pool " +
                                             poolUsed.ToString("0") + "/" + totalUsage.ToString("0") +
                                             " of " + rule.LimitQty.ToString("0") + ")";
                        }
                    }
                }
            }
        }

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
        private void ApplyRentalFreeN(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats, int genYear, int genMonth)
        {
            if (strats == null || strats.Count == 0) return;
            HashSet<long> waiveOwners = ItemsWithWaiveMeters(jobs);
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                {
                    if (!l.IsRental || l.ContractKey <= 0 || l.Charge <= 0m) continue;
                    // A machine with a WAIVE METER owns its rental deal there — the strategy rule
                    // must not zero the rent too (double waive). Waive lines themselves never apply.
                    if (l.IsWaiveMeter || waiveOwners.Contains(l.ItemKey)) continue;
                    StrategyDef sd;
                    if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                    foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_RENTAL_FREE_N))
                    {
                        if (rule.FreeMonths <= 0) continue;
                        if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                        DateTime? anchor = l.RentalStartDate ?? l.EffStartDate;
                        if (!anchor.HasValue)
                        {
                            l.StrategyNote = "RENTAL FREE rule skipped - no rental start / service start date to count from";
                            continue;
                        }
                        int monthNo = (genYear * 12 + genMonth) - (anchor.Value.Year * 12 + anchor.Value.Month) + 1;
                        if (monthNo >= 1 && monthNo <= rule.FreeMonths)
                        {
                            l.Charge = 0m;
                            l.UseMin = false;
                            l.StrategyNote = "RENTAL FREE month " + monthNo + "/" + rule.FreeMonths + " (strategy)";
                            // User rule: every meter SHOWS on the invoice — a freed rental prints as a
                            // 0.00 line with this note, never silently vanishes into a NO-CHARGE stamp.
                            l.AlwaysBill = true;
                        }
                        break;   // one free-N rule per rental line
                    }
                }
        }

        // ═══ WAIVE METERS (master-style contra, engine-decided) ═══
        // A Rental-Waive meter (its own item code, e.g. RA-MONTH(W)-13MTH) bills NEGATIVE when its
        // per-meter Waive Configuration says so — replacing the master's manual monthly decision:
        //   window : FirstN > 0  -> fires only in months 1..N (anchor RentalStartDate, else the
        //            machine's effective service start)
        //   target : Target > 0  -> the MACHINE's scoped BK/CL charges must reach it; the partial %
        //            band gives a partial contra (amount x partial%)
        //   neither             -> ALWAYS fires (legacy master behavior)
        // Fired  -> Charge = -amount (AlwaysBill: the contra PRINTS with its note).
        // Silent -> Charge = 0 (NO-CHARGE stamp closes the meter's period, no invoice line).
        private void ApplyWaiveMeters(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs, int genYear, int genMonth)
        {
            List<MeterBillLine> usage = new List<MeterBillLine>();
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                    if (!l.IsFlat) usage.Add(l);

            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                {
                    if (!l.IsWaiveMeter) continue;
                    // The waive amount is stored NEGATIVE on the meter row (master convention, user
                    // rule 2026-07-27) — the engine is sign-agnostic and the fired contra always
                    // bills MINUS the magnitude, whatever sign was keyed.
                    decimal amount = l.MinCharges != 0m ? Math.Abs(l.MinCharges) : Math.Abs(l.Rate);
                    if (amount <= 0m) { l.Charge = 0m; l.UseMin = false; continue; }

                    // SEQUENTIAL semantics (user decision): months 1..N = free window, ALWAYS waived;
                    // AFTER the window the target condition (when set) takes over. Window-only = waive
                    // stops after N months; target-only = by usage from day one; neither = always.
                    bool fire = true;
                    bool inWindow = false;
                    string note = "RENTAL WAIVE";
                    if (l.WaiveFirstNMonths > 0)
                    {
                        DateTime? anchor = l.RentalStartDate ?? l.EffStartDate;
                        if (!anchor.HasValue) { l.Charge = 0m; l.UseMin = false; continue; }   // no date to count from
                        int monthNo = (genYear * 12 + genMonth) - (anchor.Value.Year * 12 + anchor.Value.Month) + 1;
                        if (monthNo >= 1 && monthNo <= l.WaiveFirstNMonths)
                        {
                            inWindow = true;
                            note = "RENTAL WAIVE month " + monthNo + "/" + l.WaiveFirstNMonths;
                        }
                        else if (l.WaiveTargetAmount <= 0m)
                            fire = false;   // window over and no usage condition -> the waive retires
                    }
                    decimal waiveAmt = amount;
                    if (fire && !inWindow && l.WaiveTargetAmount > 0m)
                    {
                        decimal total = 0m;
                        foreach (MeterBillLine u in usage)
                        {
                            // Per-MACHINE deal normally; the GROUP machine counts the WHOLE fleet.
                            if (l.IsGroupItem ? u.ContractKey != l.ContractKey : u.ItemKey != l.ItemKey) continue;
                            if (u.ColorLabel != "Black" && u.ColorLabel != "Colour") continue;
                            if (l.WaiveScope == "BK" && u.ColorLabel != "Black") continue;
                            if (l.WaiveScope == "CL" && u.ColorLabel != "Colour") continue;
                            total += u.Charge;
                        }
                        if (total >= l.WaiveTargetAmount)
                            note += " - charges " + total.ToString("0.00") + " >= target " + l.WaiveTargetAmount.ToString("0.00");
                        // Partial band in RM (demo 28/07 #24): charges reach RM X -> waive RM Y
                        // (was a % of the target — the customer agrees deals in RM, not %).
                        else if (l.WaivePartialThreshold > 0m && l.WaivePartialAmount > 0m
                                 && total >= l.WaivePartialThreshold)
                        {
                            waiveAmt = Math.Min(l.WaivePartialAmount, amount);
                            note += " - PARTIAL: charges " + total.ToString("0.00") +
                                    " >= " + l.WaivePartialThreshold.ToString("0.00") +
                                    " -> waive " + waiveAmt.ToString("0.00");
                        }
                        else fire = false;
                    }
                    if (fire)
                    {
                        l.Charge = -Math.Round(waiveAmt, 2);   // the CONTRA line (negative)
                        l.UseMin = false;
                        l.AlwaysBill = true;                              // negative lines must print
                        l.StrategyNote = note;
                    }
                    else
                    {
                        l.Charge = 0m;   // not this month -> no line; NO-CHARGE stamp closes the period
                        l.UseMin = false;
                        l.StrategyNote = "";
                    }
                }
        }

        private static HashSet<long> ItemsWithWaiveMeters(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs)
        {
            HashSet<long> set = new HashSet<long>();
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                    if (l.IsWaiveMeter) set.Add(l.ItemKey);
            return set;
        }

        // The per-invoice contract snapshot text: header billing flags + the strategy rules AS OF
        // this run. Stored in zSCP2_ContractSnapshot next to every generated invoice so a later
        // strategy edit can never rewrite what THIS invoice was computed from.
        private Dictionary<long, string> BuildContractSnapshots(HashSet<long> cks,
            Dictionary<long, StrategyDef> strats, int genYear, int genMonth)
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            // Header flags from the loaded grid rows (first row per contract carries them).
            Dictionary<long, string> header = new Dictionary<long, string>();
            foreach (DataRow r in _dtGrid.Rows)
            {
                long ck = D64(r["ContractKey"]);
                if (ck <= 0 || header.ContainsKey(ck)) continue;
                header[ck] = "Contract " + S(r["ContractNo"]) + "  |  Customer " + S(r["DebtorCode"]) +
                    "  |  Strategy '" + S(r["StrategyCode"]) + "'  |  FOC Reset " + S(r["FOCResetUnit"]) +
                    (S(r["FOCResetUnit"]) == "D" ? "/" + S(r["FOCResetN"]) : "") +
                    "  |  RentalSeparate " + (r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]) ? "Y" : "N") +
                    "  |  PeriodFollowContract " + (r["PeriodByContract"] != DBNull.Value && Convert.ToBoolean(r["PeriodByContract"]) ? "Y" : "N");
            }
            foreach (long ck in cks)
            {
                StrategyDef sd;
                strats.TryGetValue(ck, out sd);
                string head;
                if (!header.TryGetValue(ck, out head)) head = "Contract key " + ck;
                map[ck] = "Billing period " + genYear + "-" + genMonth.ToString("00") + "\r\n" + head + "\r\n" +
                          ServiceContractPhotocopier.Classes.ScpStrategy.SerializeRules(sd);
            }
            return map;
        }

        private void ApplyCommittedMin(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats)
        {
            {
                // Sum the actual PRINT charges per service item (non-flat BK/CL usage meters),
                // split by colour so a MIN meter can count BK only / CL only / both (its scope).
                Dictionary<long, decimal> printBkByItem = new Dictionary<long, decimal>();
                Dictionary<long, decimal> printClByItem = new Dictionary<long, decimal>();
                Dictionary<long, decimal> printBkByContract = new Dictionary<long, decimal>();
                Dictionary<long, decimal> printClByContract = new Dictionary<long, decimal>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (!l.IsFlat && (l.ColorLabel == "Black" || l.ColorLabel == "Colour") && l.ItemKey > 0)
                        {
                            Dictionary<long, decimal> bucket = l.ColorLabel == "Black" ? printBkByItem : printClByItem;
                            decimal t;
                            bucket.TryGetValue(l.ItemKey, out t);
                            bucket[l.ItemKey] = t + l.Charge;
                            Dictionary<long, decimal> cbucket = l.ColorLabel == "Black" ? printBkByContract : printClByContract;
                            decimal tc;
                            cbucket.TryGetValue(l.ContractKey, out tc);
                            cbucket[l.ContractKey] = tc + l.Charge;
                        }

                // Items already carrying a MIN meter — the COMMIT-MIN rule must not double-charge them.
                HashSet<long> minMeterItems = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (!l.IsCommittedMin) continue;
                        minMeterItems.Add(l.ItemKey);
                        decimal pBk, pCl;
                        // The GROUP machine's MIN commits against the WHOLE fleet's print charges.
                        if (l.IsGroupItem)
                        {
                            printBkByContract.TryGetValue(l.ContractKey, out pBk);
                            printClByContract.TryGetValue(l.ContractKey, out pCl);
                        }
                        else
                        {
                            printBkByItem.TryGetValue(l.ItemKey, out pBk);
                            printClByItem.TryGetValue(l.ItemKey, out pCl);
                        }
                        string cscope = (l.WaiveScope ?? "BKCL").Trim().ToUpperInvariant();
                        decimal printed = cscope == "BK" ? pBk : (cscope == "CL" ? pCl : pBk + pCl);
                        decimal topUp = l.CommittedAmount - printed;
                        if (topUp < 0m) topUp = 0m;
                        l.PrintedAmount = printed;
                        l.Charge = topUp;
                        l.Foc = 0m;
                        l.UseMin = false;
                        l.StrategyNote = "COMMITTED MIN " + l.CommittedAmount.ToString("0.00") + ": printed " +
                                         printed.ToString("0.00") + " -> top-up " + topUp.ToString("0.00");
                    }

                // COMMIT-MIN RULES act LIVE too (no meter push): for every covered item WITHOUT a MIN
                // meter, a top-up line is synthesized when its scoped print charges fall short.
                if (strats == null || strats.Count == 0) return;
                // ONE top-up per item per RUN (not per job): with rental-separate ("_R") or Bill Group
                // splits an item's lines can span jobs — the old per-job dedup synthesized a SECOND
                // full-amount top-up on the rental-only job (printed=0 there). The top-up prints once,
                // on the job carrying the item's usage charges, and counts printed across ALL jobs.
                Dictionary<long, MeterInvoiceGenerator.InvoiceJob> hostByItem =
                    new Dictionary<long, MeterInvoiceGenerator.InvoiceJob>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (l.ItemKey > 0 && !l.IsFlat && !hostByItem.ContainsKey(l.ItemKey))
                            hostByItem[l.ItemKey] = jb;
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (l.ItemKey > 0 && !hostByItem.ContainsKey(l.ItemKey))
                            hostByItem[l.ItemKey] = jb;   // flat-only item: any job carrying it
                HashSet<long> doneItems = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                {
                    List<MeterBillLine> extra = new List<MeterBillLine>();
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (l.ItemKey <= 0 || l.ContractKey <= 0) continue;
                        if (hostByItem[l.ItemKey] != jb) continue;   // synthesize only on the host job
                        if (!doneItems.Add(l.ItemKey)) continue;
                        if (minMeterItems.Contains(l.ItemKey)) continue;   // MIN meter already rules this item
                        StrategyDef sd;
                        if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                        foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_COMMIT_MIN))
                        {
                            if (rule.CommitAmount <= 0m) continue;
                            if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                            // Scoped print charges of THIS item across ALL jobs (run-wide buckets built
                            // above — identical filter to the old inner loop, but complete after splits).
                            decimal pBk, pCl;
                            printBkByItem.TryGetValue(l.ItemKey, out pBk);
                            printClByItem.TryGetValue(l.ItemKey, out pCl);
                            decimal printed = rule.Scope == "BK" ? pBk : (rule.Scope == "CL" ? pCl : pBk + pCl);
                            decimal topUp = rule.CommitAmount - printed;
                            if (topUp < 0m) topUp = 0m;
                            MeterBillLine minLn = new MeterBillLine();
                            minLn.ItemKey = l.ItemKey;
                            minLn.ContractKey = l.ContractKey;
                            minLn.ContractNo = l.ContractNo;
                            minLn.ItemNo = l.ItemNo;
                            minLn.ItemName = l.ItemName;
                            minLn.ItemDesc = l.ItemDesc;
                            minLn.DebtorCode = l.DebtorCode;
                            minLn.SerialNumber = l.SerialNumber;
                            minLn.MeterTypeCode = "COMMIT-MIN";
                            minLn.MeterTypeName = "MINIMUM COMMITTED PRINT CHARGES";
                            minLn.IsFlat = true;
                            minLn.IsCommittedMin = true;
                            minLn.AlwaysBill = true;   // transparency: shown even at RM 0
                            minLn.CommittedAmount = rule.CommitAmount;
                            minLn.PrintedAmount = printed;
                            minLn.Charge = topUp;
                            minLn.StrategyCode = l.StrategyCode;
                            minLn.StrategyNote = "COMMITTED MIN (rule) " + rule.CommitAmount.ToString("0.00") +
                                                 ": printed " + printed.ToString("0.00") + " -> top-up " + topUp.ToString("0.00");
                            extra.Add(minLn);
                            break;   // one committed-min line per item
                        }
                    }
                    foreach (MeterBillLine x in extra) jb.Lines.Add(x);
                }
            }
        }

        private void ApplyWaiveTarget(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats)
        {
            {
                HashSet<long> cks = new HashSet<long>();
                List<MeterBillLine> usage = new List<MeterBillLine>();   // all non-flat (BK/CL) usage lines
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (l.ContractKey > 0) cks.Add(l.ContractKey);
                        if (!l.IsFlat && l.ContractKey > 0) usage.Add(l);
                    }
                if (cks.Count == 0 || strats == null || strats.Count == 0) return;
                HashSet<long> waiveOwners = ItemsWithWaiveMeters(jobs);

                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (!l.IsRental || l.Foc > 0m || l.Charge <= 0m) continue;
                        // Machines with a WAIVE METER: the deal lives there (see ApplyWaiveMeters).
                        if (l.IsWaiveMeter || waiveOwners.Contains(l.ItemKey)) continue;
                        StrategyDef sd;
                        if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                        // The strategy is a bundle of rules — pick the WAIVE-TARGET rule that applies to THIS
                        // rental line: an item-specific rule (its ServiceItemKeys set contains this item) wins
                        // over a contract-wide rule (empty ServiceItemKeys = all items).
                        StrategyRule wr = null;
                        foreach (StrategyRule cand in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_WAIVE_TARGET))
                        {
                            if (cand.ServiceItemKeys.Count > 0 && cand.ServiceItemKeys.Contains(l.ItemKey)) { wr = cand; break; }
                            if (cand.ServiceItemKeys.Count == 0 && wr == null) wr = cand;
                        }
                        if (wr == null || wr.TargetAmount <= 0m) continue;
                        // Target total = usage charges of the SAME contract, filtered by the rule's Service
                        // Item set (empty = all) and its Scope ("BK"=Black only / "CL"=Colour only / else both).
                        decimal total = 0m;
                        foreach (MeterBillLine u in usage)
                        {
                            if (u.ContractKey != l.ContractKey) continue;
                            // Only BK/CL PRINT charges count toward the target — the scope choices are
                            // "Black / Colour / BK+CL usage", so NA meters (scan/plotter) never count.
                            if (u.ColorLabel != "Black" && u.ColorLabel != "Colour") continue;
                            if (wr.ServiceItemKeys.Count > 0 && !wr.ServiceItemKeys.Contains(u.ItemKey)) continue;
                            if (wr.Scope == "BK" && u.ColorLabel != "Black") continue;
                            if (wr.Scope == "CL" && u.ColorLabel != "Colour") continue;
                            total += u.Charge;
                        }
                        if (total >= wr.TargetAmount)
                        {
                            l.Charge = 0m;
                            l.WaivedRental = true;
                            l.StrategyNote = "WAIVED: meter charges " + total.ToString("0.00") +
                                             " >= target " + wr.TargetAmount.ToString("0.00");
                            l.AlwaysBill = true;   // waived = still printed at 0.00 with the note (user rule)
                        }
                        else if (wr.PartialPct > 0m && total >= wr.TargetAmount * wr.PartialPct / 100m)
                        {
                            l.Charge = Math.Round(l.Charge * (1m - wr.PartialPct / 100m), 2);
                            l.StrategyNote = "PARTIAL WAIVE " + wr.PartialPct.ToString("0.##") + "%: meter charges " +
                                             total.ToString("0.00") + " >= " +
                                             (wr.TargetAmount * wr.PartialPct / 100m).ToString("0.00") +
                                             " (" + wr.PartialPct.ToString("0.##") + "% of target " + wr.TargetAmount.ToString("0.00") + ")";
                        }
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
