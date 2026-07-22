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
        private DevExpress.XtraEditors.CheckEdit _chkPerCssi;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _selCssiEditor;
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
            // Native AutoCount header (same as Maintain Service Contract) with the hint line hidden.
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            InitDefaults();
            // DEV/TEST hidden shortcut: Ctrl+Shift+3 wipes every meter-generated invoice (confirmed)
            // so billing runs can be repeated. No button on purpose — key combo only.
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(DevShortcut_KeyDown);
        }

        private void DevShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.D3)
            {
                e.Handled = true;
                DevWipeGeneratedInvoices();
            }
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
            _pageWith = new XtraTabPage(); _pageWith.Text = "With Reading";
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

            // Invoice grouping choice (overrides each contract's stored BillingMode when generating):
            //   ticked  = one invoice per CSSI (service item)
            //   unticked= one invoice per whole contract
            // Grouping choice: CHECKED (default) = one invoice per debtor/contract; unchecked = one
            // invoice per CSSI. Sits left-aligned under the action buttons.
            _chkPerCssi = new DevExpress.XtraEditors.CheckEdit();
            _chkPerCssi.Properties.Caption = "Group same debtor into one invoice";
            _chkPerCssi.Properties.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _chkPerCssi.Properties.Appearance.Options.UseFont = true;
            _chkPerCssi.Properties.Appearance.ForeColor = Color.FromArgb(27, 94, 32);
            _chkPerCssi.Properties.Appearance.Options.UseForeColor = true;
            _chkPerCssi.Location = new Point(510, 64);
            _chkPerCssi.Size = new Size(300, 22);
            _chkPerCssi.Checked = true;
            this.PanelFilter.Controls.Add(_chkPerCssi);
            _chkPerCssi.BringToFront();
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
            OpenContractDetail(D64(r["ContractKey"]), S(r["ContractNo"]), S(r["Customer"]));
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
            // "Need Manual Key-In" membership is a SNAPSHOT (NeedManual flag, recomputed only at
            // load/fetch): keying a reading keeps the row on this tab so the operator can review what
            // they just typed — it only leaves the tab on the next Refresh / Fetch.
            // Tabs are PER MACHINE (grouped by the NeedManual flag), so a machine's rental + BK/CL rows
            // always appear together on the same tab. With Reading = ready machines (all usage read, or
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
            // never split across the With Reading / Need Manual tabs — a rental row always sits in the
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
            // Bucket each MACHINE into exactly one of Invoiced / Need Manual / With Reading — mirrors the
            // per-machine tab filters so a machine with mixed rows (e.g. rental + unread BK) counts once.
            foreach (string itemKey in allItems)
            {
                bool inv; invByItem.TryGetValue(itemKey, out inv);
                bool nm; nmByItem.TryGetValue(itemKey, out nm);
                if (inv) invoicedItems.Add(itemKey);
                else if (nm) needManualItems.Add(itemKey);
                else withItems.Add(itemKey);
            }
            _pageWith.Text = "With Reading (" + withItems.Count + ")";
            _pageNo.Text = "Need Manual Key-In (" + needManualItems.Count + ")";   // snapshot flag = what the tab actually shows
            _pageDone.Text = "Invoiced (" + invoicedItems.Count + ")";
            _pageAll.Text = "All (" + allItems.Count + ")";
            _pageConflict.Text = "⚠ Conflicts (" + conflictItems.Count + ")";
            // Conflicts tab only exists while there IS something to resolve; if it disappears from
            // under the user's feet, fall back to the With Reading view.
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
        private int SelectedYear()
        {
            return SelectedMonth() > DateTime.Today.Month ? DateTime.Today.Year - 1 : DateTime.Today.Year;
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
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS EffBillingDay, " +
                    "m.ItemMeterKey, m.MeterRole, m.MeterTypeCode, ISNULL(mt.Description,'') AS MeterTypeName, " +
                    // Invoice line Item Code = the meter type's stock code (master convention: metertype.stockcode
                    // goes on the charge row); ACItemCode is an explicit override when set.
                    "ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS ACItemCode, ISNULL(m.MinimumCharges,0) AS MinCharges, " +
                    "ISNULL(m.ChargesRate,0) AS UnitPrice, ISNULL(m.FOCQty,0) AS FOCQty, " +
                    "ISNULL(NULLIF(m.MeterMultiPriceCode,''), ISNULL(mt.MeterMultiPriceCode,'')) AS MultiPriceCode, " +
                    "ISNULL(m.RebateQtyInPercent,0) AS RebatePct, ISNULL(m.InitialReading,0) AS InitReading, " +
                    "ISNULL(mt.IsFlatCharge,'N') AS IsFlatCharge, " +
                    "m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, ISNULL(m.RentalBasis,'A') AS RentalBasis, " +
                    "lr.LastReading, lr.LastDate, " +
                    "ISNULL(li.LastInvNo,'') AS LastInvNo, li.LastInvAt, ISNULL(li.LastInvTotal,0) AS LastInvTotal " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
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
                    "WHERE i.Inactive='N' AND c.Inactive='N' " + dayFilter + searchFilter + expiryFilter +
                    "ORDER BY c.DebtorCode, c.ContractNo, i.ServiceItemNo, m.MeterRole";   // customers cluster together
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
                    g["IsFlat"] = S(r["IsFlatCharge"]) == "Y";
                    g["StrategyCode"] = S(r["StrategyCode"]);
                    g["RentSep"] = S(r["RentSep"]) == "Y";
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
            dt.Columns.Add("FetchedReading", typeof(decimal)); // last value the API returned for this meter (hidden)
            dt.Columns.Add("IsFlat", typeof(bool));            // flat/rental meter: auto-billed qty 1 x price, no reading
            dt.Columns.Add("StrategyCode", typeof(string));    // contract's strategy in force (hidden; stamped at generate)
            dt.Columns.Add("RentSep", typeof(bool));           // contract flag: rental billed on its own invoice (hidden)
            dt.Columns.Add("RentalStartDate", typeof(DateTime)); // rental period anchor (hidden; n/N)
            dt.Columns.Add("RentalMonths", typeof(int));       // rental total periods N (hidden)
            dt.Columns.Add("RentalBasis", typeof(string));     // A accrual / P prepayment (hidden)
            dt.Columns.Add("NeedManual", typeof(bool));        // SNAPSHOT tab membership: set at load/fetch, NOT live —
                                                               // keying a reading must not make the row vanish mid-work (hidden)
            dt.Columns.Add("Locked", typeof(bool));            // billing-day auto-fetch snapshot lock — reading frozen (hidden)
            dt.Columns.Add("HasConflict", typeof(bool));       // saved-manual value differs from a fresh API value (hidden)
            dt.Columns.Add("InvoicedDocNo", typeof(string));   // non-empty = this meter+period is already invoiced (hidden)
            return dt;
        }

        private void ConfigureGrid()
        {
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
            foreach (string h in new string[] { "ItemKey", "ContractKey", "DebtorCode", "BillingMode", "ItemMeterKey", "ACItemCode", "Role", "Shade", "Sel", "InvoicedDocNo", "ItemDesc", "NeedManual", "Locked", "IsFlat", "StrategyCode", "RentSep", "RentalStartDate", "RentalMonths", "RentalBasis" })
                if (GridViewMeter.Columns[h] != null) GridViewMeter.Columns[h].Visible = false;
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
            foreach (string h in new string[] { "Mode", "BillingDay", "UseMin", "MultiPriceCode", "Status", "EntrySource", "FetchedReading", "HasConflict" })
            {
                GridColumn hc = GridViewMeter.Columns[h];
                if (hc == null) continue;
                hc.Visible = false;
                hc.OptionsColumn.ShowInCustomizationForm = true;
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
            string sql = "SELECT ItemMeterKey, CurrentReading, ReadingDate, Source, InvoicedDocNo, InvoicedAt, LockedAt, LastModified " +
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
            using (MeterReadingSetting_Form dlg = new MeterReadingSetting_Form(_dbSetting, SelectedYear(), SelectedMonth()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadData();
            }
        }

        // Insert-or-update one staged reading (unique per ItemMeterKey + period).
        private void UpsertStaging(SqlConnection cn, SqlTransaction tx, long itemMeterKey, int year, int month,
            decimal reading, DateTime? readingDate, string source)
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
                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@rd, ReadingDate=@dt, Source=@src, LastModified=GETDATE() " +
                "WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo AND LockedAt IS NULL; " +
                "IF @@ROWCOUNT=0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey=@imk AND PeriodYear=@yr AND PeriodMonth=@mo) " +
                "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey,PeriodYear,PeriodMonth,CurrentReading,ReadingDate,Source) " +
                "VALUES (@imk,@yr,@mo,@rd,@dt,@src);", cn, tx);
            cmd.Parameters.AddWithValue("@imk", itemMeterKey);
            cmd.Parameters.AddWithValue("@yr", year);
            cmd.Parameters.AddWithValue("@mo", month);
            cmd.Parameters.AddWithValue("@rd", reading);
            cmd.Parameters.AddWithValue("@dt", (object)readingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@src", source);
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
                dt.Rows.Add(d);
            }

            bool genReq = false;
            using (MeterReadingDetail_Form f = new MeterReadingDetail_Form(contractNo, customer, dt, _dbSetting, contractKey))
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
                    if (Dec(r["CurrentReading"]) > 0m && S(r["InvoicedDocNo"]).Trim().Length == 0)
                        r["Sel"] = true;
                SyncSelCssi();
                BtnGenerateInvoice_Click(this, EventArgs.Empty);
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
                if (r != null) Recalc(r);
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

            foreach (DataRow r in visibleRows)
            {
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) continue;
                // Billing-period guard: this meter + period already has an invoice — never bill twice.
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) { alreadyInvoiced++; continue; }
                // Usage meters need a reading > 0 to bill; flat/rental meters bill on their flat amount
                // even with reading 0 (that is the whole point — no meter to read).
                bool rowFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (!rowFlat && Dec(r["CurrentReading"]) <= 0m) continue;
                // Invoice grouping: the checkbox overrides each contract's stored BillingMode for this run.
                // CHECKED = "Group same debtor into one invoice" — literally: ONE invoice per CUSTOMER,
                // across all their contracts. Unchecked = one invoice per CSSI.
                string mode = (_chkPerCssi != null && _chkPerCssi.Checked) ? "G" : "S";
                long contractKey = D64(r["ContractKey"]);
                long itemKey = D64(r["ItemKey"]);
                string groupKey = mode == "S" ? ("C" + contractKey + "_I" + itemKey) : ("D" + S(r["DebtorCode"]));
                // Contract flag "Rental separate invoice": flat (rental/min) lines split into their own
                // job — one extra invoice per group ("Rental- [ref]") instead of riding the meter invoice.
                bool rentSep = r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]);
                bool rentalJob = rowFlat && rentSep;
                if (rentalJob) groupKey += "_R";
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
                ln.ItemDesc = S(r["ItemDesc"]);
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
                ln.MultiPriceCode = S(r["MultiPriceCode"]);
                ln.IsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                // Charge via the SAME engine the grid used, so the invoice matches the preview exactly
                // (NET: FOC copies + rebate deducted, multi-price tier, min floor). Sets BillCopies / EffUnitPrice.
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, _ladders);
                if (r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"])) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
                ln.IsRental = ln.IsFlat && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(S(r["MeterType"]));
                ln.StrategyCode = S(r["StrategyCode"]);
                if (r["RentalStartDate"] != DBNull.Value) ln.RentalStartDate = Convert.ToDateTime(r["RentalStartDate"]);
                ln.RentalMonths = r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]);
                ln.RentalBasis = S(r["RentalBasis"]) == "P" ? 'P' : 'A';
                if (S(r["EntrySource"]) == "RENTAL FREE") ln.StrategyNote = "RENTAL FREE - FOC month";
                if (r["LastReadDate"] != DBNull.Value) ln.LastDate = Convert.ToDateTime(r["LastReadDate"]);
                if (r["LastAuditDate"] != DBNull.Value) ln.AuditDate = Convert.ToDateTime(r["LastAuditDate"]);

                MeterInvoiceGenerator.InvoiceJob job;
                if (!jobs.TryGetValue(groupKey, out job))
                {
                    job = new MeterInvoiceGenerator.InvoiceJob();
                    job.DebtorCode = ln.DebtorCode;
                    job.RefDocNo = refNo;
                    // Legacy header text (verified against the customer's V8 meter invoices);
                    // rental-only invoices get their own header so the two are distinguishable.
                    job.Description = (rentalJob ? "Rental- [" : "Billing- [") + refNo + "]";
                    job.Label = refNo + (rentalJob ? " (rental)" : "");
                    job.Lines = new List<MeterBillLine>();
                    jobs[groupKey] = job;
                }
                job.Lines.Add(ln);
            }

            // Strategy passes over the whole run (cross-machine — cannot live in the per-row grid preview):
            //   1) GROUP FOC LIMIT — pools a shared free-copy quota across the group's machines first,
            //   2) WAIVE-TARGET — waives rental when the (net-of-pool) usage charges reach the target.
            ApplyGroupLimit(jobs);
            ApplyWaiveTarget(jobs);

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
            if (XtraMessageBox.Show(this,
                    "Generate " + jobList.Count + " invoice(s) for " + monthName + " now?\r\nThey are saved automatically — no clicking through each one." + skippedNote,
                    "Generate Invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Run the generation on a worker task behind a progress dialog (invoices saved headlessly).
            using (MeterInvoiceGenerateProgress_Form dlg =
                new MeterInvoiceGenerateProgress_Form(_dbSetting, jobList, DateTime.Today, DateTime.Now, genYear, genMonth))
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
        // GROUP FOC LIMIT: the whole group shares ONE free-copy pool (LimitQty), accumulated across the
        // contract's usage meters that the rule covers (Scope BK/CL/both + optional Service Item set). The
        // pooled free copies are allocated across the members (sequentially), reducing each line's billed
        // copies; the excess is charged. Runs at generation (cross-machine — can't live in the per-row grid).
        private void ApplyGroupLimit(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs)
        {
            try
            {
                HashSet<long> cks = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (!l.IsFlat && l.ContractKey > 0) cks.Add(l.ContractKey);
                if (cks.Count == 0) return;
                Dictionary<long, StrategyDef> strats =
                    ServiceContractPhotocopier.Classes.ScpStrategy.LoadForContracts(_dbSetting, cks);
                if (strats.Count == 0) return;

                foreach (long ck in cks)
                {
                    StrategyDef sd;
                    if (!strats.TryGetValue(ck, out sd)) continue;
                    foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_LIMIT))
                    {
                        if (rule.LimitScope != 'G' || rule.LimitQty <= 0m) continue;
                        // Gather this contract's covered usage lines (scope + item set), deterministic order.
                        List<MeterBillLine> members = new List<MeterBillLine>();
                        foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                            foreach (MeterBillLine l in jb.Lines)
                            {
                                if (l.IsFlat || l.ContractKey != ck) continue;
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

                        decimal pool = rule.LimitQty;
                        foreach (MeterBillLine l in members)
                        {
                            if (pool <= 0m) break;
                            decimal free = Math.Min(pool, l.BillCopies);
                            if (free <= 0m) continue;
                            l.Foc += free;   // bump this machine's FOC by its slice of the shared pool
                            pool -= free;
                            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
                            l.StrategyNote = "GROUP FOC pool: " + free.ToString("0") + " free copies (of " +
                                             rule.LimitQty.ToString("0") + " shared)";
                        }
                    }
                }
            }
            catch { }   // strategy evaluation must never block invoice generation
        }

        private void ApplyWaiveTarget(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs)
        {
            try
            {
                HashSet<long> cks = new HashSet<long>();
                List<MeterBillLine> usage = new List<MeterBillLine>();   // all non-flat (BK/CL) usage lines
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (l.ContractKey > 0) cks.Add(l.ContractKey);
                        if (!l.IsFlat && l.ContractKey > 0) usage.Add(l);
                    }
                if (cks.Count == 0) return;
                Dictionary<long, StrategyDef> strats =
                    ServiceContractPhotocopier.Classes.ScpStrategy.LoadForContracts(_dbSetting, cks);
                if (strats.Count == 0) return;

                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (!l.IsRental || l.Foc > 0m || l.Charge <= 0m) continue;
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
            catch { }   // strategy evaluation must never block invoice generation
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
