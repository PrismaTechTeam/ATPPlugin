using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Combined Service Contract editor (module v2). One customer per contract; its machines
    /// (service items) are managed inline via a grid + child dialog. Billing Day (default) and
    /// Billing Mode (grouped one invoice vs separate per item) drive the Meter Reading billing run.
    /// </summary>
    public partial class zSCP2_Contract_Form : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        private readonly DBSetting _db;
        private long _contractKey;
        private bool _isNew;
        private bool _modeGuard;
        private bool _loading;
        private bool _dirty;       // unsaved-changes flag; drives the close confirmation (see CLAUDE.md rule 8)
        private bool _savedOk;     // set true after a successful save so closing skips the confirmation
        private readonly List<ItemEditData> _items = new List<ItemEditData>();
        // Items detached from this contract THIS session (still ContractKey=this in DB until save) —
        // made available again in the attach picker / inline lookup so "Remove then re-pick" works now.
        private readonly List<long> _detachedThisSession = new List<long>();
        private DataTable _orphanLookup;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _orphanRepo;
        private DataTable _debtorLookup;
        private DataTable _dtItemsView;

        public zSCP2_Contract_Form()
        {
            InitializeComponent();
        }

        public zSCP2_Contract_Form(DBSetting db) : this()
        {
            _db = db;
            _isNew = true;
            this.Load += new EventHandler(OnFormLoad);
        }

        public zSCP2_Contract_Form(DBSetting db, long contractKey) : this()
        {
            _db = db;
            _contractKey = contractKey;
            _isNew = false;
            this.Load += new EventHandler(OnFormLoad);
        }

        // Clone-as-new ("Copy to a new Service Contract"): a NEW contract pre-filled from sourceKey.
        private long _cloneFromKey;
        public zSCP2_Contract_Form(DBSetting db, long sourceKey, bool cloneAsNew) : this()
        {
            _db = db;
            _isNew = true;
            _cloneFromKey = cloneAsNew ? sourceKey : 0;
            this.Load += new EventHandler(OnFormLoad);
        }

        private long PickContract(DataTable src, string title)
        {
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, title, src, "ContractKey",
                new string[] { "ContractNo", "DebtorCode", "CompanyName" },
                new string[] { "Contract No", "Customer", "Company Name" },
                new int[] { 110, 90, 260 });
            long v; return (k != null && k != DBNull.Value && long.TryParse(k.ToString(), out v)) ? v : 0;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null) return;
            ApplyToolbarIcons();
            LoadCurrencyLabel();
            LoadDebtorLookup();
            LoadContractTypeLookup();
            LoadAgentLookup();
            LoadAreaLookup();
            LoadStrategyLookup();
            LoadDeptProjectLookups();
            WireBillingModeRadios();
            LkDebtorCode.EditValueChanged += new EventHandler(OnDebtorChanged);

            if (_isNew)
            {
                AutoPickContractNo();
                DtContractDate.EditValue = DateTime.Today;
                SpnBillingDay.Value = PumsConfig.GetInt(_db, PumsConfig.KEY_DEFAULT_BILLING_DAY, PumsConfig.DEFAULT_BILLING_DAY_VALUE);
                string mode = PumsConfig.Get(_db, PumsConfig.KEY_DEFAULT_BILLING_MODE, PumsConfig.DEFAULT_BILLING_MODE_VALUE);
                SetBillingMode(mode);
                // Copy-to-a-new: pre-fill from the source contract (fresh copies of its items).
                if (_cloneFromKey > 0) LoadContractAsTemplate(_cloneFromKey);
            }
            else
            {
                LoadContract();
            }

            SetupInlineOrphanLookup();
            EnableInlineItemEditing();
            BuildItemMeterPanel();
            RebuildItemsView();
            SetupSparePartsGrid();
            LoadSpareParts();
            BuildMoreHeaderTab();
            LoadMoreHeader();
            BuildStrategyTab();
            BuildGroupTab();
            InitBillingHeaderData();
            BuildBillingHistoryTab();
            BuildChangeHistoryTab();
            ApplyTemplateExtras();   // clone-at-open: spare parts / rules / More Header now have their tabs

            // Dirty tracking for the close confirmation: any header edit marks the form dirty. Wired
            // AFTER the initial load so loading an existing contract does not itself set the flag.
            WireDirtyTracking();
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnFormClosing);

            ExtendContractRibbon();
            // Clipboard group HIDDEN (user decision 2026-07-26): Copy/Paste Whole Document,
            // Copy Selected/Spreadsheet and Paste Item Detail are retired from the UI (handlers
            // kept in code). Do not re-show without an explicit user request.
            grpClipboard.Visible = false;
            // "Last day of month" RETIRED (user decision 2026-07-27): Billing Day is a plain 1..28
            // everywhere. The flag stored 31 internally (editor showed 28, DB said 31, billing
            // screen filed it under "31") — pure confusion. Migration v8 converts legacy rows to
            // day 28. Do not re-show this checkbox.
            ShowContractDates();
            UpdateServiceItemButtons();
            UpdateFormModeTitle();
            // Typing a custom Contract No must update the "— NEW (xxx)" caption live, not only at save.
            TxtContractNo.EditValueChanged += delegate { UpdateFormModeTitle(); };
            // Multi-row selection so "Copy Selected Details" can actually copy more than the focused
            // row (Ctrl / Shift + click the row indicator; plain cell clicks still edit inline).
            GridViewItems.OptionsSelection.MultiSelect = true;
            GridViewItems.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;
        }

        // Service items live in the in-memory _items list until Save, so Create / Edit / Delete work in
        // BOTH new and edit mode (the save loop inserts the contract header first, then the items).
        // Attach/inline-attach still need existing rows to look up, so they stay edit-mode-only.
        private void UpdateServiceItemButtons()
        {
            bool canAttach = !_isNew;
            // barAddItem is DELIBERATELY not linked to the ribbon (user decision 2026-07-26): machines
            // are added from the Service Item tab (Quick Add / inline attach), not from the ribbon.
            // Do not "fix" the missing link — only Edit and Delete live in the ribbon Item group.
            barAddItem.Enabled = true;
            barEditItem.Enabled = true;
            barDelItem.Enabled = true;
            if (BtnItemDelete != null) BtnItemDelete.Enabled = true;
            if (BtnItemAttach != null) BtnItemAttach.Enabled = canAttach;
            if (BtnItemDetach != null) BtnItemDetach.Enabled = canAttach;
            DevExpress.XtraGrid.Columns.GridColumn col = GridViewItems.Columns.ColumnByFieldName("ServiceItemNo");
            if (col != null) col.OptionsColumn.AllowEdit = canAttach;   // inline attach only in edit mode
            // The bottom NEW row is the only place the attach dropdown opens; existing rows stay locked.
            GridViewItems.OptionsView.NewItemRowPosition = canAttach
                ? DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom
                : DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            barDemoFill.Enabled = _isNew;   // demo random-fill only for a NEW contract
            // "Copy from other Service Contract" is a TEMPLATE fill: it CLEARS the item list, which on
            // a saved contract would silently detach every real machine at the next Save. NEW mode only.
            barCopyFrom.Enabled = _isNew;
            // Generate From Serial No: NEW mode only (hide after a new contract is saved -> edit mode).
            if (_barGenSerial != null)
                _barGenSerial.Visibility = _isNew
                    ? DevExpress.XtraBars.BarItemVisibility.Always
                    : DevExpress.XtraBars.BarItemVisibility.Never;
            LblItemsHint.Text = "";
            LblItemsHint.Visible = false;
        }

        // Contract Start / Contract Expiry on the header (middle column, under Agent). New machines
        // DEFAULT their Service Start / Expiry from these dates and each machine stays editable in
        // the items grid. (The designer's original spot for these editors sat UNDER the Attention
        // textbox — invisible — which is why they are relocated here in code.)
        private void ShowContractDates()
        {
            // (Labels/positions now live in the user's Designer layout - nothing to relocate.)
            // Expiry < Start is rejected ON THE SPOT (not only at Save): the offending entry is
            // cleared with a message, and the Expiry calendar greys out days before the Start.
            DtStartDate.EditValueChanged += new EventHandler(ContractDates_Changed);
            DtExpiryDate.EditValueChanged += new EventHandler(ContractDates_Changed);
            SyncExpiryCalendarMin();
        }

        private void ContractDates_Changed(object sender, EventArgs e)
        {
            if (_loading) { SyncExpiryCalendarMin(); return; }
            SyncExpiryCalendarMin();
            if (DtStartDate.EditValue == null || DtStartDate.EditValue == DBNull.Value) return;
            if (DtExpiryDate.EditValue == null || DtExpiryDate.EditValue == DBNull.Value) return;
            if (Convert.ToDateTime(DtExpiryDate.EditValue).Date >= Convert.ToDateTime(DtStartDate.EditValue).Date) return;
            XtraMessageBox.Show("Contract Expiry cannot be earlier than Contract Start.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            // Clear whichever field the user just changed so a dead date range never lingers.
            DevExpress.XtraEditors.DateEdit changed = sender as DevExpress.XtraEditors.DateEdit;
            if (changed != null) changed.EditValue = null;
            else DtExpiryDate.EditValue = null;
        }

        // The Expiry picker's calendar cannot go before the chosen Start (typed values are caught
        // by ContractDates_Changed above).
        private void SyncExpiryCalendarMin()
        {
            try
            {
                DtExpiryDate.Properties.MinValue =
                    (DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value)
                        ? Convert.ToDateTime(DtStartDate.EditValue).Date
                        : DateTime.MinValue;
            }
            catch { }
        }

        // Window title tells the mode at a glance: "— NEW" until the first save, then "— EDIT · no".
        private void UpdateFormModeTitle()
        {
            string no = TxtContractNo.Text.Trim();
            this.Text = _isNew
                ? "Service Contract — NEW" + (no.Length > 0 ? "  (" + no + ")" : "")
                : "Service Contract — EDIT  ·  " + no;
        }

        // New machines inherit the contract's dates as their starting Service Start / Expiry —
        // still editable per machine in the grid (an empty contract date stays empty on the item).
        private void ApplyContractDateDefaults(ItemEditData d)
        {
            if (d == null) return;
            if (!d.ServiceStartDate.HasValue && DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value)
                d.ServiceStartDate = Convert.ToDateTime(DtStartDate.EditValue).Date;
            if (!d.ServiceExpiryDate.HasValue && DtExpiryDate.EditValue != null && DtExpiryDate.EditValue != DBNull.Value)
                d.ServiceExpiryDate = Convert.ToDateTime(DtExpiryDate.EditValue).Date;
        }

        // "Generate From Serial No" — pick DO/IV lines that carry machine serials; auto-fill the
        // customer header (NEW mode) and auto-create one service item per machine.
        private DevExpress.XtraBars.BarButtonItem _barGenSerial;

        private void ExtendContractRibbon()
        {
            try
            {
                _barGenSerial = new DevExpress.XtraBars.BarButtonItem();
                _barGenSerial.Caption = "Generate From\r\nSerial No";
                _barGenSerial.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                _barGenSerial.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(BarGenSerial_ItemClick);
                RibbonCtl.Items.Add(_barGenSerial);
                grpItem.ItemLinks.Add(_barGenSerial);
                // Generate From Serial No is a NEW-contract-only action (it drives the whole header from
                // the picked document) — hidden once the contract exists / is being edited.
                _barGenSerial.Visibility = _isNew
                    ? DevExpress.XtraBars.BarItemVisibility.Always
                    : DevExpress.XtraBars.BarItemVisibility.Never;

                // "Apply Strategy to Meters" is RETIRED: every strategy rule kind now acts LIVE
                // at Generate (MeterReadingIntegration passes) — nothing is pushed onto meters.

                try
                {
                    float dpi = 96f;
                    try { dpi = this.DeviceDpi; } catch { }
                    AutoCount.Images.IAutoCountImage img =
                        AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                    _barGenSerial.ImageOptions.Image = img.GetLargeImage_Inquiry();
                }
                catch { }
            }
            catch { }
        }

        // ═══════════ APPLY-STRATEGY PUSH MODEL RETIRED (2026-07-25, user decision) ═══════════
        // The old flow required clicking "Apply to Meters" to materialise RENTAL-FREE-N /
        // COMMIT-MIN (and once FOC-REBATE) onto the meter columns — forget the click and the
        // deal silently never billed. ALL FOUR rule kinds now act LIVE inside the Generate run
        // (MeterReadingIntegration: ApplyGroupLimit / ApplyRentalFreeN / ApplyWaiveTarget /
        // ApplyCommittedMin), and every generated invoice stores a zSCP2_ContractSnapshot row
        // freezing the rules as billed. Do NOT reintroduce a push step.

        private void BarGenSerial_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (zSCP2_GenerateFromSerial_Form f = new zSCP2_GenerateFromSerial_Form(_db))
            {
                if (f.ShowDialog(this) != DialogResult.OK || f.Picked.Count == 0) return;

                zSCP2_GenerateFromSerial_Form.PickedSerial first = f.Picked[0];

                // NEW mode: the picked document drives the whole header — customer (which cascades into
                // address/attention/phone/term/area/agent + More Header via OnDebtorChanged) + Reference No.
                if (_isNew)
                {
                    LkDebtorCode.EditValue = first.DebtorCode;
                    TxtRefNo.Text = first.DocNo;
                }

                bool multiDebtor = false;
                foreach (zSCP2_GenerateFromSerial_Form.PickedSerial p in f.Picked)
                    if (!string.Equals(p.DebtorCode, first.DebtorCode, StringComparison.OrdinalIgnoreCase))
                        multiDebtor = true;

                // EDIT mode: the header keeps its customer — flag picked serials sold to someone else.
                bool debtorMismatch = false;
                if (!_isNew)
                {
                    string curDebtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
                    foreach (zSCP2_GenerateFromSerial_Form.PickedSerial p in f.Picked)
                        if (!string.Equals(p.DebtorCode, curDebtor, StringComparison.OrdinalIgnoreCase))
                            debtorMismatch = true;
                }

                // Serials already registered on ANY service item are skipped — the machine serial is the
                // key Meter Reading matches on, so silently duplicating it would corrupt billing.
                System.Collections.Generic.HashSet<string> dbSerials =
                    new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    DataTable ex1 = _db.GetDataTable("SELECT SerialNumber FROM [dbo].[zSCP2_Item] WHERE ISNULL(SerialNumber,'') <> ''", false);
                    foreach (DataRow r1 in ex1.Rows) dbSerials.Add(r1["SerialNumber"].ToString().Trim());
                }
                catch { }
                foreach (ItemEditData ex in _items)
                    if (!string.IsNullOrEmpty(ex.SerialNumber)) dbSerials.Remove(ex.SerialNumber.Trim());   // in-list items get the finer ItemCode+Serial check below

                // Preview numbering: show each generated item its upcoming CSSI number immediately
                // (same UX as the Auto button). The REAL numbers are drawn by ScpDocNo.Next() at save,
                // so previews are never persisted and can't collide across users.
                string fmtSI = null; int nextSI = 0;
                try
                {
                    DataTable fdt = _db.GetDataTable(
                        "SELECT FormatString, NextNumber FROM [dbo].[zSCP2_DocNoFormat] WHERE DocType='" +
                        ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_SERVICE_ITEM + "'", false);
                    if (fdt.Rows.Count > 0)
                    {
                        fmtSI = fdt.Rows[0]["FormatString"].ToString();
                        nextSI = Convert.ToInt32(fdt.Rows[0]["NextNumber"]);
                    }
                }
                catch { }
                int autosAhead = 0;   // items already queued with an auto number consume earlier previews
                foreach (ItemEditData ex in _items) if (ex.ServiceItemNoIsAuto) autosAhead++;

                int added = 0, skipped = 0, inDb = 0;
                foreach (zSCP2_GenerateFromSerial_Form.PickedSerial p in f.Picked)
                {
                    bool dup = false;
                    foreach (ItemEditData ex in _items)
                        if (string.Equals(ex.SerialNumber ?? "", p.SerialNo, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(ex.ItemCode ?? "", p.ItemCode, StringComparison.OrdinalIgnoreCase))
                            dup = true;
                    if (dup) { skipped++; continue; }
                    if (dbSerials.Contains(p.SerialNo.Trim())) { inDb++; continue; }

                    ItemEditData d = new ItemEditData();
                    d.Meters = zSCP2_Item_Form.CreateMetersTable();
                    d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
                    ApplyContractDateDefaults(d);   // Service Start / Expiry follow the contract dates
                    d.ServiceItemNo = fmtSI == null ? "" :
                        ServiceContractPhotocopier.Classes.ScpDocNo.Format(fmtSI, nextSI + autosAhead + added);
                    d.ServiceItemNoIsAuto = true;    // real number reserved by ScpDocNo.Next() at save time
                    d.ItemCode = p.ItemCode;
                    d.SerialNumber = p.SerialNo;
                    d.Description = p.ItemDesc;
                    _items.Add(d);
                    added++;
                }
                RebuildItemsView();
                _dirty = true;

                string msg = added + " service item(s) generated from the selected serial numbers.";
                if (skipped > 0) msg += "\r\n" + skipped + " skipped (same Item Code + Serial already in the list).";
                if (inDb > 0) msg += "\r\n" + inDb + " skipped (serial already registered on another service item).";
                if (multiDebtor) msg += "\r\nNote: the selection spans multiple customers — the header was filled from " + first.DebtorCode + ".";
                if (debtorMismatch) msg += "\r\nWarning: some picked serials were sold to a DIFFERENT customer than this contract's.";
                if (added > 0) msg += "\r\n\r\nNote: the generated machines have NO meters yet. Save the contract, then " +
                    "add each machine's meters in the Meter Configuration panel — a machine without meters cannot be billed.";
                XtraMessageBox.Show(msg, "Generate From Serial No", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void WireDirtyTracking()
        {
            EventHandler h = delegate { if (!_loading) _dirty = true; };
            TxtContractNo.EditValueChanged += h;
            SluContractType.EditValueChanged += h;
            LkDebtorCode.EditValueChanged += h;
            DtContractDate.EditValueChanged += h;
            DtStartDate.EditValueChanged += h;
            DtExpiryDate.EditValueChanged += h;
            SpnContractValue.EditValueChanged += h;
            SpnBillingDay.EditValueChanged += h;
            // Items without an override display the contract's day — keep the grid current when it changes.
            EventHandler bd = delegate { if (!_loading) RebuildItemsView(); };
            SpnBillingDay.EditValueChanged += bd;
            SluAgent.EditValueChanged += h;
            SluDept.EditValueChanged += h;
            SluProject.EditValueChanged += h;
            TxtAddress.EditValueChanged += h;
            TxtAttention.EditValueChanged += h;
            TxtPhone.EditValueChanged += h;
            TxtTerm.EditValueChanged += h;
            TxtArea.EditValueChanged += h;
            TxtDescription.EditValueChanged += h;
            TxtRemark1.EditValueChanged += h;
            TxtRemark2.EditValueChanged += h;
            TxtNote.EditValueChanged += h;
            ChkInactive.EditValueChanged += h;
            ChkRentalSeparate.EditValueChanged += h;
            if (SpnRentalDay != null) SpnRentalDay.EditValueChanged += h;
            if (SluInvoiceTemplate != null) SluInvoiceTemplate.EditValueChanged += h;
            if (ChkGenerateSOA != null) ChkGenerateSOA.EditValueChanged += h;
            if (SluSOATemplate != null) SluSOATemplate.EditValueChanged += h;
        }

        // Demo 28/07 #18: the rental-separate invoice's OWN billing day. The control lives in
        // the DESIGNER (GrpBilling) now; InitBillingHeaderData wires value + enable gating.
        private int _loadedRentalDay;
        // StrategyCode has no header control any more (user layout 01/08) - round-trips here.
        private string _loadedStrategyCode = "";

        // Demo 28/07 #9c: per-contract report templates — a 60-month deal agrees its paperwork
        // ONCE, so the contract remembers WHICH invoice layout (and optionally which SOA layout)
        // this customer receives; Bulk Email Invoice then picks it automatically. AutoCount
        // identifies report designs by NAME only (no separate code) — a renamed/deleted design is
        // flagged "MISSING?" here and billing falls back to the default layout instead of failing.
        private string _loadedInvRpt = "", _loadedSOARpt = "";
        private bool _loadedGenSOA;

        private static string TplVal(DevExpress.XtraEditors.SearchLookUpEdit ed, string fallback)
        {
            if (ed == null) return fallback ?? "";
            return ed.EditValue == null || ed.EditValue == DBNull.Value ? "" : ed.EditValue.ToString().Trim();
        }


        // Searchable popup listing every report DESIGN of the type (System + User rows straight
        // from AutoCount's report registry). A saved name that no longer exists is kept visible as
        // a "MISSING?" row instead of silently blanking out — renaming a design in the Report
        // Designer must not silently detach 60-month contracts from their agreed layout.
        private void ConfigureTplLookup(DevExpress.XtraEditors.SearchLookUpEdit ed,
            DevExpress.XtraGrid.Views.Grid.GridView view, string reportType, string savedName)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ReportName", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            try
            {
                DataTable src = AutoCount.Report.AutoCountReport.GetInstance().GetReportList(_db, reportType);
                foreach (DataRow r in src.Rows)
                    dt.Rows.Add(Convert.ToString(r["ReportName"]), Convert.ToString(r["Type"]));
            }
            catch { /* report registry unavailable — picker stays empty; the saved value still shows */ }
            if (!string.IsNullOrEmpty(savedName) && dt.Select("ReportName='" + savedName.Replace("'", "''") + "'").Length == 0)
                dt.Rows.Add(savedName, "MISSING? (renamed/deleted)");
            ed.Properties.DataSource = dt;
            ed.Properties.ValueMember = "ReportName";
            ed.Properties.DisplayMember = "ReportName";
            ed.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            ed.Properties.NullText = "(default layout)";
            ed.Properties.PopupView = view;
            view.OptionsBehavior.AutoPopulateColumns = false;
            view.OptionsView.ShowGroupPanel = false;
            DevExpress.XtraGrid.Columns.GridColumn cn = view.Columns.AddVisible("ReportName");
            cn.Caption = "Report Design"; cn.Width = 240;
            DevExpress.XtraGrid.Columns.GridColumn ctp = view.Columns.AddVisible("Type");
            ctp.Caption = "Type"; ctp.Width = 150;
            view.OptionsView.ShowAutoFilterRow = true;
            ed.EditValue = string.IsNullOrEmpty(savedName) ? null : (object)savedName;
        }

        // The billing header controls live in the DESIGNER now (user request 31/07 — the layout
        // is arranged in Design view); this only wires DATA, tooltips and enable gating.
        private void InitBillingHeaderData()
        {
            SpnRentalDay.Value = _loadedRentalDay;
            SpnRentalDay.Enabled = ChkRentalSeparate.Checked;
            ChkRentalSeparate.CheckedChanged += delegate { SpnRentalDay.Enabled = ChkRentalSeparate.Checked; };
            SpnRentalDay.ToolTip = "0 = rental rides the meter invoice date.\r\n" +
                "1-28 = the separate Rental- invoice is dated on THIS day: accrual rentals in the " +
                "billing month, prepayment rentals in the NEXT month (June run -> July 1 rental).";
            ConfigureTplLookup(SluInvoiceTemplate, SluInvoiceTemplateView, "Invoice Document", _loadedInvRpt);
            ConfigureTplLookup(SluSOATemplate, SluSOATemplateView, "Debtor Statement", _loadedSOARpt);
            SluInvoiceTemplate.ToolTip = "The AutoCount report design used for THIS contract's invoices " +
                "(bulk email / preview). Empty = the book's default Invoice Document layout.";
            ChkGenerateSOA.Checked = _loadedGenSOA;
            ChkGenerateSOA.ToolTip = "This customer receives a Statement of Account each cycle " +
                "(sent via A/R > Debtor Statement > Batch Mail).";
            SluSOATemplate.Enabled = ChkGenerateSOA.Checked;
            ChkGenerateSOA.CheckedChanged += delegate { SluSOATemplate.Enabled = ChkGenerateSOA.Checked; };
            SluSOATemplate.ToolTip = "The Debtor Statement report design for this customer's SOA. " +
                "Empty = the default statement layout.";
        }

        // CLAUDE.md rule 8: mirror AutoCount's create/edit behaviour — closing with unsaved changes
        // prompts for confirmation. A successful Save clears the flag so it closes silently.
        private void OnFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            // Prompt on UNSAVED changes — even after an earlier successful save (the form now stays
            // open after Save, so post-save edits are just as precious). _savedOk only means "saved
            // at least once": the eventual close reports OK so the contract listing refreshes.
            if (_dirty)
            {
                DialogResult r = XtraMessageBox.Show(
                    "You have unsaved changes. Discard them and close?", "Unsaved Changes",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) { e.Cancel = true; return; }
            }
            if (_savedOk && this.Modal) this.DialogResult = DialogResult.OK;
        }

        private void LoadDebtorLookup()
        {
            try
            {
                _debtorLookup = _db.GetDataTable(
                    "SELECT AccNo, CompanyName, ISNULL(Address1,'') AS Address1, ISNULL(Address2,'') AS Address2, " +
                    "ISNULL(Address3,'') AS Address3, ISNULL(Address4,'') AS Address4, ISNULL(Attention,'') AS Attention, " +
                    "ISNULL(Phone1,'') AS Phone1, ISNULL(AreaCode,'') AS AreaCode, ISNULL(SalesAgent,'') AS SalesAgent, " +
                    "ISNULL(DisplayTerm,'') AS DisplayTerm, ISNULL(PostCode,'') AS PostCode, ISNULL(Fax1,'') AS Fax1, " +
                    "ISNULL(EmailAddress,'') AS EmailAddress, ISNULL(DeliverAddr1,'') AS DeliverAddr1, " +
                    "ISNULL(DeliverAddr2,'') AS DeliverAddr2, ISNULL(DeliverAddr3,'') AS DeliverAddr3, " +
                    "ISNULL(DeliverAddr4,'') AS DeliverAddr4, ISNULL(DeliverPostCode,'') AS DeliverPostCode " +
                    "FROM dbo.Debtor ORDER BY AccNo", false);
            }
            catch { _debtorLookup = new DataTable(); }
            LkDebtorCode.Properties.DataSource = _debtorLookup;
            LkDebtorCode.Properties.DisplayMember = "AccNo";
            LkDebtorCode.Properties.ValueMember = "AccNo";
            // Demo 28/07 #26: the closed editor showed only the CODE — render "code — company
            // name" so the customer is identifiable right on the header (stored value stays the
            // code; the popup already shows both columns).
            LkDebtorCode.Properties.CustomDisplayText +=
                new DevExpress.XtraEditors.Controls.CustomDisplayTextEventHandler(LkDebtorCode_CustomDisplayText);
            // SearchLookUpEdit shows its columns through the popup GridView — show ONLY Code + Company
            // Name. AutoPopulateColumns must be OFF: the popup binds lazily, and a later auto-populate
            // would resurrect every Debtor column (PopulateColumns() here ran before binding = no-op).
            LkDebtorView.OptionsBehavior.AutoPopulateColumns = false;
            LkDebtorView.Columns.Clear();
            DevExpress.XtraGrid.Columns.GridColumn lkCode = LkDebtorView.Columns.AddVisible("AccNo");
            lkCode.Caption = "Code"; lkCode.Width = 100;
            DevExpress.XtraGrid.Columns.GridColumn lkName = LkDebtorView.Columns.AddVisible("CompanyName");
            lkName.Caption = "Company Name"; lkName.Width = 320;
            LkDebtorView.OptionsView.ShowAutoFilterRow = true;
        }

        private static void ShowLkCol(DevExpress.XtraGrid.Views.Grid.GridView v, string field, string caption, int width, int visibleIndex)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
        }

        // Contract Type dropdown: values come from Service Contract Type Maintenance
        // (zSCP_LK_ServiceContractType). Stores the CODE by value, so contracts already referencing a
        // type keep working even if the type's description changes (no hard FK; matched by code).
        private void LkDebtorCode_CustomDisplayText(object sender, DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            if (e.Value == null || e.Value == DBNull.Value || _debtorLookup == null) return;
            string code = e.Value.ToString().Trim();
            if (code.Length == 0) return;
            foreach (DataRow r in _debtorLookup.Rows)
                if (string.Equals(Convert.ToString(r["AccNo"]), code, StringComparison.OrdinalIgnoreCase))
                {
                    string name = Convert.ToString(r["CompanyName"]).Trim();
                    if (name.Length > 0) e.DisplayText = code + " — " + name;
                    return;
                }
        }

        private void LoadContractTypeLookup()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT ServiceContractTypeCode, Description FROM [dbo].[zSCP_LK_ServiceContractType] " +
                    "WHERE Inactive = 'N' ORDER BY ServiceContractTypeCode", false);
            }
            catch { dt = new DataTable(); }
            SluContractType.Properties.DataSource = dt;
            SluContractType.Properties.ValueMember = "ServiceContractTypeCode";
            SluContractType.Properties.DisplayMember = "ServiceContractTypeCode";
            // Popup shows the 2 datasource columns (Code + Description) auto-populated by the view.
        }

        // Agent dropdown: values come from AutoCount's Sales Agent Maintenance (dbo.SalesAgent).
        private void LoadAgentLookup()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT SalesAgent, ISNULL(Description,'') AS Description FROM [dbo].[SalesAgent] " +
                    "WHERE IsActive = 'T' ORDER BY SalesAgent", false);
            }
            catch { dt = new DataTable(); }
            SluAgent.Properties.DataSource = dt;
            SluAgent.Properties.ValueMember = "SalesAgent";
            SluAgent.Properties.DisplayMember = "SalesAgent";
            // Popup shows the 2 datasource columns (Agent + Description) auto-populated by the view.
        }

        // Strategy dropdown lists the active strategies (Strategy Maintenance master). Picking one with
        // NET billing shows a one-time explanation so the operator knows the billing mode changes.
        private DataTable _strategyLookup;

        private void LoadStrategyLookup()
        {
            try
            {
                _strategyLookup = _db.GetDataTable(
                    "SELECT StrategyCode, [Description], StrategyType, NetBilling FROM [dbo].[zSCP2_Strategy] " +
                    "WHERE Inactive = 'N' ORDER BY StrategyCode", false);
            }
            catch { _strategyLookup = new DataTable(); }
            // Picker removed from the header (user layout 01/08) - the lookup table stays for
            // any future strategy UI; StrategyCode itself round-trips via _loadedStrategyCode.
        }


        // NOTE: there used to be a popup here claiming a "raw" (non-NET) billing convention for
        // contracts without a NET strategy. No such mode exists — ScpInvoiceBuilder.ComputeCharge
        // always bills NET (FOC and rebate really deducted) for every contract. Removed as misleading.

        // Area dropdown lists AutoCount's own Area master (dbo.Area — the native Area Maintenance data).
        private void LoadAreaLookup()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT AreaCode, ISNULL(Description,'') AS Description FROM [dbo].[Area] ORDER BY AreaCode", false);
            }
            catch { dt = new DataTable(); }
            TxtArea.Properties.DataSource = dt;
            TxtArea.Properties.ValueMember = "AreaCode";
            TxtArea.Properties.DisplayMember = "AreaCode";
        }

        // Department + Project dropdowns list AutoCount's own masters (Dept / Project tables).
        private void LoadDeptProjectLookups()
        {
            try
            {
                SluDept.Properties.DataSource = _db.GetDataTable("SELECT DeptNo, Description FROM [dbo].[Dept] ORDER BY DeptNo", false);
                SluDept.Properties.ValueMember = "DeptNo";
                SluDept.Properties.DisplayMember = "DeptNo";
            }
            catch { }
            try
            {
                SluProject.Properties.DataSource = _db.GetDataTable("SELECT ProjNo, Description FROM [dbo].[Project] ORDER BY ProjNo", false);
                SluProject.Properties.ValueMember = "ProjNo";
                SluProject.Properties.DisplayMember = "ProjNo";
            }
            catch { }
        }

        // "+" on the Department dropdown: open AutoCount's OWN new-Department form.
        private void SluDept_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(AutoCount.GeneralMaint.Project.ProjectType.Department, session);
                AutoCount.GeneralMaint.Project.DepartmentEntity entity = cmd.NewDepartment(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Department))
                    form.ShowDialog(this);
                LoadDeptProjectLookups();
                string code = entity.Row["DeptNo"] == null ? "" : entity.Row["DeptNo"].ToString().Trim();
                if (code.Length > 0) SluDept.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Department failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // "+" on the Project dropdown: open AutoCount's OWN new-Project form.
        private void SluProject_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(AutoCount.GeneralMaint.Project.ProjectType.Project, session);
                AutoCount.GeneralMaint.Project.ProjectEntity entity = cmd.NewProject(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Project))
                    form.ShowDialog(this);
                LoadDeptProjectLookups();
                string code = entity.Row["ProjNo"] == null ? "" : entity.Row["ProjNo"].ToString().Trim();
                if (code.Length > 0) SluProject.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Project failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // "+" on the Contract Type dropdown: open the Service Contract Type edit dialog to add a new
        // one, then reload the list and select it. (Same edit form as the Type Maintenance menu.)
        private void SluContractType_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form(_db, null))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadContractTypeLookup();
                        if (!string.IsNullOrEmpty(f.SavedCode)) SluContractType.EditValue = f.SavedCode;
                    }
                }
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Contract Type failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // When the customer changes interactively, auto-fill address/contact/term/area/agent from the
        // Debtor master — the same behaviour as AutoCount's Quotation/Invoice entry. Guarded by _loading
        // so loading an existing contract does not overwrite its saved values.
        private void OnDebtorChanged(object sender, EventArgs e)
        {
            if (_loading || _debtorLookup == null) return;
            string code = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (code.Length == 0) return;
            DataRow[] found = _debtorLookup.Select("AccNo='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow d = found[0];

            string addr = string.Join("\r\n", new string[] {
                AsStr(d["Address1"]), AsStr(d["Address2"]), AsStr(d["Address3"]), AsStr(d["Address4"]) })
                .Trim('\r', '\n');
            TxtAddress.Text = addr;
            TxtAttention.Text = AsStr(d["Attention"]);
            TxtPhone.Text = AsStr(d["Phone1"]);
            TxtTerm.Text = AsStr(d["DisplayTerm"]);
            TxtArea.EditValue = SetOrNull(AsStr(d["AreaCode"]));
            SluAgent.EditValue = SetOrNull(AsStr(d["SalesAgent"]));

            // Map the debtor's info into the More Header tab too (only fields the Debtor master has —
            // AutoCount stores address as freeform Address1-4, so City/State/Country aren't derived).
            if (_mh != null && _mh.Count > 0)
            {
                MhSet("PostalCode", AsStr(d["PostCode"]));
                MhSet("Fax", AsStr(d["Fax1"]));
                // Delivery Address block from the debtor's delivery address.
                if (_mhDelAddress != null)
                    _mhDelAddress.Text = string.Join("\r\n", new string[] {
                        AsStr(d["DeliverAddr1"]), AsStr(d["DeliverAddr2"]), AsStr(d["DeliverAddr3"]), AsStr(d["DeliverAddr4"]) })
                        .Trim('\r', '\n');
                MhSet("DelPostalCode", AsStr(d["DeliverPostCode"]));
                MhSet("DelPhone", AsStr(d["Phone1"]));
                MhSet("DelFax", AsStr(d["Fax1"]));
                MhSet("DelEmail", AsStr(d["EmailAddress"]));
                MhSet("DelContactPerson", AsStr(d["Attention"]));
            }
        }

        // SearchLookUpEdit shows NullText when its value isn't in the list; empty string -> null so
        // the placeholder shows instead of a blank selected row.
        private static object SetOrNull(string code)
        {
            return string.IsNullOrEmpty(code) ? null : (object)code;
        }

        private void WireBillingModeRadios()
        {
            ChkBillGroup.CheckedChanged += new EventHandler(OnBillGroupChanged);
            ChkBillSeparate.CheckedChanged += new EventHandler(OnBillSeparateChanged);
        }

        private void OnBillGroupChanged(object sender, EventArgs e)
        {
            if (_modeGuard) return;
            _modeGuard = true;
            if (ChkBillGroup.Checked) ChkBillSeparate.Checked = false;
            else if (!ChkBillSeparate.Checked) ChkBillGroup.Checked = true; // keep one selected
            _modeGuard = false;
        }

        private void OnBillSeparateChanged(object sender, EventArgs e)
        {
            if (_modeGuard) return;
            _modeGuard = true;
            if (ChkBillSeparate.Checked) ChkBillGroup.Checked = false;
            else if (!ChkBillGroup.Checked) ChkBillSeparate.Checked = true;
            _modeGuard = false;
        }

        private void SetBillingMode(string mode)
        {
            _modeGuard = true;
            bool separate = string.Equals(mode, "S", StringComparison.OrdinalIgnoreCase);
            ChkBillSeparate.Checked = separate;
            ChkBillGroup.Checked = !separate;
            _modeGuard = false;
        }

        private string CurrentBillingMode()
        {
            return ChkBillSeparate.Checked ? "S" : "G";
        }

        private string _autoPeekNo;   // previewed auto number; if the box still shows it at save, a real one is drawn

        private void AutoPickContractNo()
        {
            // PREVIEW ONLY (peek, no consume) — clicking Auto never increments the counter. The real
            // number is reserved by ScpDocNo.Next() at save time (see BtnSave_Click).
            string peek = ServiceContractPhotocopier.Classes.ScpDocNo.Peek(
                _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_CONTRACT);
            if (string.IsNullOrEmpty(peek))
            {
                try
                {
                    object o = _db.ExecuteScalar(
                        "SELECT ISNULL(MAX(CONVERT(int, SUBSTRING(ContractNo,4,20))),0)+1 " +
                        "FROM [dbo].[zSCP2_Contract] WHERE ContractNo LIKE 'SC-%' AND ISNUMERIC(SUBSTRING(ContractNo,4,20))=1");
                    int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
                    peek = "SC-" + next.ToString("000000");
                }
                catch { peek = "SC-000001"; }
            }
            _autoPeekNo = peek;
            TxtContractNo.Text = peek;
        }

        private void BtnAutoNo_Click(object sender, EventArgs e) { AutoPickContractNo(); }

        // Real AutoCount toolbar icons (the exact ones AutoCount uses in its own detail grids), set in
        // code because the DevExpress ImageUri names were unreliable (some rendered empty). Icon-only
        // buttons are centered (MiddleCenter); icon+text buttons keep the icon on the left.
        private void ApplyToolbarIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));

                IconOnly(BtnSpInsert, img.GetSmallImage_Insert());
                IconOnly(BtnSpRemove, img.GetSmallImage_Delete());
                IconOnly(BtnSpUp, img.GetSmallImage_MoveUp());
                IconOnly(BtnSpDown, img.GetSmallImage_MoveDown());
                IconLeft(BtnItemAttach, img.GetSmallImage_Insert());
                IconLeft(BtnItemDetach, img.GetSmallImage_Delete());

                barCopyFrom.ImageOptions.Image = img.GetLargeImage_CopyFrom();
                barCopyToNew.ImageOptions.Image = img.GetLargeImage_New();
                barCopyWhole.ImageOptions.Image = img.GetSmallImage_CopyToClipboard();
                barCopySelected.ImageOptions.Image = img.GetSmallImage_CopySelectedToClipboard();
                barCopySpreadsheet.ImageOptions.Image = img.GetSmallImage_CopyAsSpreadsheet();   // #7: the missing icon
                barPasteWhole.ImageOptions.Image = img.GetSmallImage_PasteToClipboard();
                barPasteItems.ImageOptions.Image = img.GetSmallImage_PasteSelectedToClipoard();
                barDemoFill.ImageOptions.Image = img.GetLargeImage_Refresh();
            }
            catch { }   // icons are cosmetic — never block the form
        }

        private static void IconOnly(DevExpress.XtraEditors.SimpleButton b, System.Drawing.Image im)
        {
            b.Text = "";
            b.ImageOptions.Image = im;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
        }

        private static void IconLeft(DevExpress.XtraEditors.SimpleButton b, System.Drawing.Image im)
        {
            b.ImageOptions.Image = im;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        // The currency label ("RM") was removed with the user's 01/08 Designer redesign — the
        // layout item's caption covers it now. Kept as a no-op so call sites stay untouched.
        private void LoadCurrencyLabel()
        {
        }


        private void LoadContract()
        {
            _loading = true;
            try
            {
            DataTable dt = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + _contractKey, false);
            if (dt.Rows.Count == 0) { _isNew = true; AutoPickContractNo(); return; }
            DataRow r = dt.Rows[0];
            TxtContractNo.Text = AsStr(r["ContractNo"]);
            SluContractType.EditValue = SetOrNull(AsStr(r["ContractTypeCode"]));
            LkDebtorCode.EditValue = AsStr(r["DebtorCode"]);
            DtContractDate.EditValue = AsDate(r["ContractDate"]);
            DtStartDate.EditValue = AsDate(r["ServiceStartDate"]);
            DtExpiryDate.EditValue = AsDate(r["ServiceExpiryDate"]);
            SpnContractValue.Value = AsDec(r["ContractValue"]);
            // Month-end retired: any legacy day >28 displays (and re-saves) as 28.
            SpnBillingDay.Value = Math.Min(AsInt(r["BillingDay"], 1), 28);
            SpnBillingDay.Enabled = true;
            SetBillingMode(AsStr(r["BillingMode"]));
            TxtAddress.Text = AsStr(r["Address1"]);
            TxtAttention.Text = AsStr(r["Attention"]);
            TxtPhone.Text = AsStr(r["Phone"]);
            TxtTerm.Text = AsStr(r["TermCode"]);
            TxtArea.EditValue = SetOrNull(AsStr(r["AreaCode"]));
            if (r.Table.Columns.Contains("ReferenceNo")) TxtRefNo.Text = AsStr(r["ReferenceNo"]);
            SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
            SluDept.EditValue = r.Table.Columns.Contains("DeptNo") ? SetOrNull(AsStr(r["DeptNo"])) : null;
            SluProject.EditValue = r.Table.Columns.Contains("ProjNo") ? SetOrNull(AsStr(r["ProjNo"])) : null;
            TxtDescription.Text = AsStr(r["Description"]);
            TxtRemark1.Text = AsStr(r["Remark1"]);
            TxtRemark2.Text = AsStr(r["Remark2"]);
            TxtNote.Text = AsStr(r["Note"]);
            ChkInactive.Checked = AsStr(r["Inactive"]) == "Y";
            _loadedInactive = ChkInactive.Checked;
            _inactiveDate = r.Table.Columns.Contains("InactiveDate") && r["InactiveDate"] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(r["InactiveDate"]) : null;
            _inactiveReason = r.Table.Columns.Contains("InactiveReason") ? AsStr(r["InactiveReason"]) : "";
            UpdateInactiveInfoLabel();
            _loadedStrategyCode = r.Table.Columns.Contains("StrategyCode") ? AsStr(r["StrategyCode"]).Trim() : "";
            ChkRentalSeparate.Checked = r.Table.Columns.Contains("RentalSeparateInvoice") && AsStr(r["RentalSeparateInvoice"]) == "Y";
            _loadedRentalDay = r.Table.Columns.Contains("RentalBillingDay") && r["RentalBillingDay"] != DBNull.Value
                ? Math.Max(0, Math.Min(28, Convert.ToInt32(r["RentalBillingDay"]))) : 0;
            if (SpnRentalDay != null) SpnRentalDay.Value = _loadedRentalDay;
            _loadedInvRpt = r.Table.Columns.Contains("InvoiceReportName") ? AsStr(r["InvoiceReportName"]).Trim() : "";
            _loadedGenSOA = r.Table.Columns.Contains("GenerateSOA") && AsStr(r["GenerateSOA"]) == "Y";
            _loadedSOARpt = r.Table.Columns.Contains("SOAReportName") ? AsStr(r["SOAReportName"]).Trim() : "";
            if (SluInvoiceTemplate != null) SluInvoiceTemplate.EditValue = _loadedInvRpt.Length > 0 ? (object)_loadedInvRpt : null;
            if (ChkGenerateSOA != null) ChkGenerateSOA.Checked = _loadedGenSOA;
            if (SluSOATemplate != null) SluSOATemplate.EditValue = _loadedSOARpt.Length > 0 ? (object)_loadedSOARpt : null;
            // FOC reset: capture into fields; the controls may not exist yet (BuildStrategyTab runs
            // AFTER the constructor's LoadContract), so the UI binding happens in ApplyFocResetToUi —
            // called both here (post-save reload, controls exist) and at the end of BuildStrategyTab
            // (normal open). Binding only when non-null used to silently show "Monthly" for a Weekly
            // contract and wipe the setting on the next save.
            _focResetUnitDb = r.Table.Columns.Contains("FOCResetUnit") ? AsStr(r["FOCResetUnit"]) : "M";
            _focResetNDb = r.Table.Columns.Contains("FOCResetN") ? AsInt(r["FOCResetN"], 0) : 0;
            ApplyFocResetToUi();

            LoadItems();
            }
            finally { _loading = false; }
        }

        private void LoadItems()
        {
            _items.Clear();
            DataTable it = _db.GetDataTable(
                "SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=" + _contractKey + " ORDER BY Pos, ItemKey", false);
            foreach (DataRow r in it.Rows)
                _items.Add(LoadOneItem(_db, Convert.ToInt64(r["ItemKey"])));
        }

        // Load a single service item (+ its meters + item codes) into an ItemEditData. Reused by the
        // "+" attach picker so an existing contract-less item can be pulled into this contract.
        /// <summary>Full load of one service item (core + meters + item codes + spare parts).
        /// internal static so the item list's Edit can reuse it instead of duplicating the loader.</summary>
        internal static ItemEditData LoadOneItem(DBSetting db, long itemKey)
        {
            ItemEditData d = new ItemEditData();
            DataTable it = db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + itemKey, false);
            if (it.Rows.Count == 0) return d;
            DataRow r = it.Rows[0];
            d.ItemKey = itemKey;
            d.ServiceItemNo = AsStr(r["ServiceItemNo"]);
            d.SerialNumber = AsStr(r["SerialNumber"]);
            d.Description = AsStr(r["Description"]);
            d.BillingDayOverride = (r["BillingDayOverride"] == null || r["BillingDayOverride"] == DBNull.Value)
                ? (int?)null : Convert.ToInt32(r["BillingDayOverride"]);
            d.DepartmentCode = AsStr(r["DepartmentCode"]);
            d.JobCode = AsStr(r["JobCode"]);
            d.StockLocationCode = AsStr(r["StockLocationCode"]);
            d.Inactive = AsStr(r["Inactive"]) == "Y";
            d.ServiceExpiryDate = (r["ServiceExpiryDate"] == null || r["ServiceExpiryDate"] == DBNull.Value)
                ? (DateTime?)null : Convert.ToDateTime(r["ServiceExpiryDate"]);
            d.IsGroupItem = r.Table.Columns.Contains("IsGroupItem") && AsStr(r["IsGroupItem"]) == "Y";
            d.MachineMode = r.Table.Columns.Contains("MachineMode") ? AsStr(r["MachineMode"]).Trim().ToUpperInvariant() : "";
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();

            DataTable m = db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + itemKey + " ORDER BY ItemMeterKey", false);
            foreach (DataRow mr in m.Rows)
            {
                DataRow nr = d.Meters.NewRow();
                nr["MeterTypeCode"] = AsStr(mr["MeterTypeCode"]);
                nr["Description"] = mr.Table.Columns.Contains("Description") ? AsStr(mr["Description"]) : "";
                nr["MeterRole"] = AsStr(mr["MeterRole"]);
                nr["MachineSerialNo"] = mr.Table.Columns.Contains("MachineSerialNo") ? AsStr(mr["MachineSerialNo"]) : "";
                nr["MinimumCharges"] = AsDec(mr["MinimumCharges"]);
                nr["ChargesRate"] = AsDec(mr["ChargesRate"]);
                nr["MeterMultiPriceCode"] = AsStr(mr["MeterMultiPriceCode"]);
                nr["RebateQtyInPercent"] = AsDec(mr["RebateQtyInPercent"]);
                nr["FOCQty"] = AsDec(mr["FOCQty"]);
                nr["InitialReading"] = AsDec(mr["InitialReading"]);
                nr["CustomTiers"] = zSCP2_Item_Form.LoadCustomTiersCsv(db, Convert.ToInt64(mr["ItemMeterKey"]));
                nr["WaiveFirstNMonths"] = mr.Table.Columns.Contains("WaiveFirstNMonths") ? AsInt(mr["WaiveFirstNMonths"], 0) : 0;
                nr["WaiveTargetAmount"] = mr.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(mr["WaiveTargetAmount"]) : 0m;
                nr["WaivePartialThreshold"] = mr.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(mr["WaivePartialThreshold"]) : 0m;
                nr["WaivePartialAmount"] = mr.Table.Columns.Contains("WaivePartialAmount") ? AsDec(mr["WaivePartialAmount"]) : 0m;
                nr["WaiveScope"] = mr.Table.Columns.Contains("WaiveScope") && AsStr(mr["WaiveScope"]).Trim().Length > 0 ? AsStr(mr["WaiveScope"]) : "BKCL";
                d.Meters.Rows.Add(nr);
            }
            d.Meters.AcceptChanges();

            DataTable ics = db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=" + itemKey + " ORDER BY Pos, ItemCodeKey", false);
            foreach (DataRow ir in ics.Rows)
            {
                DataRow nr = d.ItemCodes.NewRow();
                nr["ItemCode"] = AsStr(ir["ItemCode"]);
                nr["Description"] = AsStr(ir["Description"]);
                nr["Qty"] = AsDec(ir["Qty"]);
                nr["SerialNumber"] = AsStr(ir["SerialNumber"]);
                d.ItemCodes.Rows.Add(nr);
            }
            d.ItemCodes.AcceptChanges();

            d.SpareParts = CreateSparePartsTable();
            DataTable sp = db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=" + itemKey + " ORDER BY Pos", false);
            int spPos = 0;
            foreach (DataRow s in sp.Rows)
            {
                DataRow nr = d.SpareParts.NewRow();
                nr["SparePartKey"] = s["SparePartKey"]; nr["ItemKey"] = itemKey; nr["Bound"] = false;
                nr["No"] = ++spPos;
                nr["ItemCode"] = AsStr(s["ItemCode"]); nr["Description"] = AsStr(s["Description"]);
                nr["SerialNumber"] = s.Table.Columns.Contains("SerialNumber") ? AsStr(s["SerialNumber"]) : "";
                nr["Unlimited"] = AsStr(s["Unlimited"]) == "Y"; nr["UOM"] = AsStr(s["UOM"]);
                nr["Quantity"] = AsDec(s["Quantity"]); nr["Discount"] = AsStr(s["Discount"]);
                nr["UnitPrice"] = AsDec(s["UnitPrice"]); nr["TaxType"] = AsStr(s["TaxType"]);
                nr["TaxInclusive"] = AsStr(s["TaxInclusive"]) == "Y"; nr["TaxRate"] = AsDec(s["TaxRate"]);
                nr["Pos"] = AsInt(s["Pos"], spPos);
                ComputeSpareRow(nr);
                d.SpareParts.Rows.Add(nr);
            }
            d.SpareParts.AcceptChanges();

            // Hydrate the extended columns + LoadedCtx snapshot. Without this, the contract save
            // loop (which persists EVERY item) would overwrite unopened items' Grade/Note/PM/context
            // overrides with empty defaults.
            zSCP2_Item_Form.LoadExtras(db, d, itemKey);
            return d;
        }

        // "+" Attach: pick a service item that has NO contract (ContractKey IS NULL) and pull it into
        // this contract. On save it is re-parented (its ItemKey + meter history are preserved).
        // Orphan (contract-less) service items available to attach: DB orphans (ContractKey IS NULL)
        // plus any detached in this session, minus the ones already in this contract.
        private DataTable LoadOrphanItems()
        {
            string extra = _detachedThisSession.Count > 0
                ? " OR ItemKey IN (" + string.Join(",", _detachedThisSession) + ")" : "";
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT ItemKey, ServiceItemNo, SerialNumber, ISNULL(Description,'') AS Description " +
                    "FROM [dbo].[zSCP2_Item] WHERE ContractKey IS NULL" + extra + " ORDER BY ServiceItemNo", false);
            }
            catch { dt = new DataTable(); }
            // drop ones already attached in this contract
            System.Collections.Generic.HashSet<long> here = new System.Collections.Generic.HashSet<long>();
            foreach (ItemEditData d in _items) if (d.ItemKey > 0) here.Add(d.ItemKey);
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
                if (here.Contains(Convert.ToInt64(dt.Rows[i]["ItemKey"]))) dt.Rows.RemoveAt(i);
            return dt;
        }

        private void BtnItemAttach_Click(object sender, EventArgs e)
        {
            DataTable loose = LoadOrphanItems();
            if (loose.Rows.Count == 0)
            { XtraMessageBox.Show("There are no unattached service items (items with no contract) to attach.", "Nothing to attach"); return; }

            long key = PickLooseItem(loose);
            if (key == 0) return;
            AttachOrphan(key);
        }

        private void AttachOrphan(long itemKey)
        {
            foreach (ItemEditData ex2 in _items)
                if (ex2.ItemKey == itemKey) { XtraMessageBox.Show("That item is already in this contract.", "Already added"); return; }
            _items.Add(LoadOneItem(_db, itemKey));
            _detachedThisSession.Remove(itemKey);
            _dirty = true;
            RebuildItemsView();
        }

        private long PickLooseItem(DataTable loose)
        {
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Attach Service Item (no contract)", loose, "ItemKey",
                new string[] { "ServiceItemNo", "SerialNumber", "Description" },
                new string[] { "Service Item No", "Machine Serial", "Description" },
                new int[] { 130, 110, 220 });
            long v; return (k != null && k != DBNull.Value && long.TryParse(k.ToString(), out v)) ? v : 0;
        }

        // "-" Remove selected service item from the contract (detach — the item survives as
        // contract-less; existing items get ContractKey=NULL on save, new ones are just dropped).
        // Detach: the item leaves THIS contract but survives contract-less (meters + history intact).
        private void BtnItemDetach_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            string no = string.IsNullOrEmpty(_items[idx].ServiceItemNo) ? "this service item" : _items[idx].ServiceItemNo;
            if (XtraMessageBox.Show("Detach " + no + " from this contract?\r\n\r\n" +
                "The service item itself is KEPT (it becomes contract-less, with all its meters and " +
                "history) and can be attached to a contract again later.\r\n" +
                "Its provided-item lines leave this contract with it.",
                "Detach from Contract", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            long removedKey = _items[idx].ItemKey;
            _items.RemoveAt(idx);
            // Existing item -> it becomes an orphan (contract-less) on save; make it re-pickable now.
            if (removedKey > 0 && !_detachedThisSession.Contains(removedKey)) _detachedThisSession.Add(removedKey);
            _dirty = true;
            RebuildItemsView();
        }

        private void RebuildItemsView()
        {
            _dtItemsView = new DataTable();
            _dtItemsView.Columns.Add("No", typeof(int));
            _dtItemsView.Columns.Add("ServiceItemNo", typeof(string));
            _dtItemsView.Columns.Add("ItemCode", typeof(string));
            _dtItemsView.Columns.Add("SerialNumber", typeof(string));
            _dtItemsView.Columns.Add("GradeCode", typeof(string));
            _dtItemsView.Columns.Add("ServiceType", typeof(string));
            _dtItemsView.Columns.Add("PurchaseDate", typeof(DateTime));
            _dtItemsView.Columns.Add("ReferenceNo", typeof(string));
            _dtItemsView.Columns.Add("Items", typeof(string));
            _dtItemsView.Columns.Add("BillingDay", typeof(string));
            _dtItemsView.Columns.Add("ServiceStart", typeof(DateTime));
            _dtItemsView.Columns.Add("BKMeter", typeof(string));
            _dtItemsView.Columns.Add("CLMeter", typeof(string));
            _dtItemsView.Columns.Add("Description", typeof(string));
            _dtItemsView.Columns.Add("Inactive", typeof(string));
            _dtItemsView.Columns.Add("MachineMode", typeof(string));
            _dtItemsView.Columns.Add("Expiry", typeof(DateTime));

            int n = 0;
            foreach (ItemEditData d in _items)
            {
                n++;
                if (d.IsGroupItem) continue;   // the group "machine" lives on the Group Deal tab
                DataRow r = _dtItemsView.NewRow();
                r["No"] = n;
                r["ServiceItemNo"] = string.IsNullOrWhiteSpace(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                r["ItemCode"] = d.ItemCode ?? "";
                r["SerialNumber"] = d.SerialNumber;
                r["GradeCode"] = d.GradeCode ?? "";
                r["ServiceType"] = d.ServiceTypeCode ?? "";
                r["PurchaseDate"] = (object)d.PurchaseDate ?? DBNull.Value;
                r["ReferenceNo"] = d.ReferenceNo ?? "";
                r["Items"] = ItemCodesSummary(d);
                r["BillingDay"] = d.BillingDayOverride.HasValue
                    ? d.BillingDayOverride.Value.ToString()
                    : ((int)SpnBillingDay.Value).ToString();
                r["ServiceStart"] = (object)d.ServiceStartDate ?? DBNull.Value;
                r["BKMeter"] = MeterForRole(d, "BK");
                r["CLMeter"] = MeterForRole(d, "CL");
                r["Description"] = d.Description ?? "";
                r["Inactive"] = d.Inactive ? "Y" : "N";
                r["MachineMode"] = d.MachineMode ?? "";
                r["Expiry"] = (object)d.ServiceExpiryDate ?? DBNull.Value;
                _dtItemsView.Rows.Add(r);
            }
            GridItems.DataSource = _dtItemsView;
            GridViewItems.BestFitColumns();
            RefreshOrphanLookup();   // keep the inline attach list current after add/detach/swap
            BindItemMeterPanel();    // rebind: the selected item's Meters table may have been replaced
            RefreshGroupTab();       // the Group Deal tab mirrors the group item's meters
        }

        private static string ItemCodesSummary(ItemEditData d)
        {
            if (d.ItemCodes == null || d.ItemCodes.Rows.Count == 0) return "";
            System.Collections.Generic.List<string> codes = new System.Collections.Generic.List<string>();
            foreach (DataRow r in d.ItemCodes.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string c = r["ItemCode"] == null ? "" : r["ItemCode"].ToString().Trim();
                if (c.Length > 0) codes.Add(c);
            }
            return string.Join(", ", codes.ToArray());
        }

        private static string MeterForRole(ItemEditData d, string role)
        {
            if (d.Meters == null) return "";
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string rr = (r["MeterRole"] == null ? "" : r["MeterRole"].ToString()).Trim().ToUpperInvariant();
                if (rr == role) return r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString();
            }
            return "";
        }

        // ===================== Meter Configuration panel (bottom of the Service Item tab) =====================
        // The selected service item's meters are viewed and edited RIGHT HERE — no need to open the item
        // dialog. The grid binds directly to that item's ItemEditData.Meters table, so edits ride the
        // normal contract save (new items -> InsertMeters, existing -> SaveMetersPreservingReadings).

        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _meterCfgRepoType;
        private DataTable _meterCfgLookup;
        private ItemEditData _meterCfgItem;   // the item currently bound in the panel

        // The Meter Configuration panel (group + grid + button bar) lives in the DESIGNER now
        // (user request 01/08); this configures DATA only: lookups, repository editors, columns.
        private void BuildItemMeterPanel()
        {
            LoadMeterCfgLookup();

            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repoType =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            DevExpress.XtraGrid.Views.Grid.GridView popup = new DevExpress.XtraGrid.Views.Grid.GridView();
            popup.OptionsView.ShowAutoFilterRow = true;
            popup.OptionsBehavior.AutoPopulateColumns = false;
            repoType.PopupView = popup;
            DevExpress.XtraGrid.Columns.GridColumn pCode = popup.Columns.AddVisible("MeterTypeCode");
            pCode.Caption = "Meter Type"; pCode.Width = 120;
            DevExpress.XtraGrid.Columns.GridColumn pDesc = popup.Columns.AddVisible("Description");
            pDesc.Caption = "Description"; pDesc.Width = 260;
            repoType.DataSource = _meterCfgLookup;
            repoType.DisplayMember = "MeterTypeCode";
            repoType.ValueMember = "MeterTypeCode";
            repoType.NullText = "";
            GridMeterCfg.RepositoryItems.Add(repoType);
            _meterCfgRepoType = repoType;   // kept so "Maintain Meter Type" can refresh its datasource

            DevExpress.XtraEditors.Repository.RepositoryItemComboBox repoRole =
                new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repoRole.Items.AddRange(new object[] { "BK", "CL", "RENTAL", "WAIVE", "COMMIT", "NA" });
            repoRole.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            GridMeterCfg.RepositoryItems.Add(repoRole);

            // Price button (no caption): opens the Multi-Price picker / tier editor for the row.
            DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit repoPricePick =
                new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            repoPricePick.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            repoPricePick.Buttons.Clear();
            DevExpress.XtraEditors.Controls.EditorButton priceBtn =
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            try
            {
                System.Drawing.Image priceImg =
                    DevExpress.Images.ImageResourceCache.Default.GetImage("images/business objects/bo_price_16x16.png");
                if (priceImg != null) priceBtn.ImageOptions.Image = priceImg;
                else priceBtn.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis;
            }
            catch { priceBtn.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis; }
            priceBtn.ToolTip = "Pick / edit the Multi-Price tier ladder for this meter";
            repoPricePick.Buttons.Add(priceBtn);
            repoPricePick.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(RepoPricePick_ButtonClick);
            GridMeterCfg.RepositoryItems.Add(repoPricePick);

            GridViewMeterCfg.OptionsBehavior.AutoPopulateColumns = false;
            GridViewMeterCfg.OptionsView.ShowGroupPanel = false;
            // First column: which service item these meters belong to (unbound — the panel always
            // shows ONE item's meters, so every row carries that item's number).
            DevExpress.XtraGrid.Columns.GridColumn colSi = GridViewMeterCfg.Columns.AddVisible("PanelServiceItemNo");
            colSi.Caption = "Service Item No";
            colSi.UnboundDataType = typeof(string);
            colSi.OptionsColumn.AllowEdit = false;
            colSi.Width = 120;
            GridViewMeterCfg.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(ViewMeterCfg_UnboundData);
            MeterCfgCol("MeterTypeCode", "Meter Type", 140, repoType);
            MeterCfgCol("Description", "Meter Description", 220, null);   // defaults from the type; editable per machine
            MeterCfgCol("MeterRole", "Role", 80, repoRole);
            MeterCfgCol("MinimumCharges", "Min Charges", 90, null);
            MeterCfgCol("ChargesRate", "Unit Price", 85, null);
            // Multi-Price shows the scheme state ("CODE" / "CODE (Modified)" / "(Custom)") and is
            // changed ONLY via the price button beside it — direct typing would bypass validation.
            DevExpress.XtraGrid.Columns.GridColumn colMultiPrice = MeterCfgCol("MeterMultiPriceCode", "Multi-Price", 110, null);
            colMultiPrice.OptionsColumn.AllowEdit = false;
            // Caption-less button column right after Multi-Price — opens the picker dialog.
            DevExpress.XtraGrid.Columns.GridColumn colPricePick = GridViewMeterCfg.Columns.AddVisible("PricePickBtn");
            colPricePick.Caption = " ";
            colPricePick.UnboundDataType = typeof(string);
            colPricePick.ColumnEdit = repoPricePick;
            colPricePick.Width = 28;
            colPricePick.OptionsColumn.ShowCaption = false;
            colPricePick.OptionsColumn.AllowSize = false;
            colPricePick.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            MeterCfgCol("RebateQtyInPercent", "Rebate %", 80, null);
            MeterCfgCol("FOCQty", "Free Qty", 80, null);
            MeterCfgCol("InitialReading", "Initial Reading", 100, null);
            GridViewMeterCfg.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(ViewMeterCfg_CellValueChanged);
            GridViewMeterCfg.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(ViewMeterCfg_RowCellStyle);
            GridViewMeterCfg.ShowingEditor += new System.ComponentModel.CancelEventHandler(ViewMeterCfg_ShowingEditor);
            GridViewMeterCfg.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(ViewMeterCfg_CustomColumnDisplayText);

            GridViewItems.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewItems_FocusedRowChangedMeterCfg);
        }

        private DevExpress.XtraGrid.Columns.GridColumn MeterCfgCol(string field, string caption, int width,
            DevExpress.XtraEditors.Repository.RepositoryItem edit)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewMeterCfg.Columns.AddVisible(field);
            c.Caption = caption;
            c.Width = width;
            if (edit != null) c.ColumnEdit = edit;
            return c;
        }

        private void LoadMeterCfgLookup()
        {
            try
            {
                _meterCfgLookup = _db.GetDataTable(
                    "SELECT MeterTypeCode, Description, MinimumCharges, ChargesRate, " +
                    "MeterMultiPriceCode, RebateQtyInPercent, FOCQty, ISNULL(IsFlatCharge,'N') AS IsFlatCharge, " +
                    "ISNULL(IsRentalWaive,'N') AS IsRentalWaive, ISNULL(DefaultRole,'') AS DefaultRole " +
                    "FROM [dbo].[zSCP_MeterType] WHERE Inactive='N' ORDER BY MeterTypeCode", false);
            }
            catch { _meterCfgLookup = new DataTable(); }
        }

        // "Maintain Meter Type": open the Meter Type maintenance module; when it saves & closes, reload
        // the lookup so this panel's Meter Type SearchLookUpEdit immediately shows new/edited types.
        private void BtnMeterCfgMaint_Click(object sender, EventArgs e)
        {
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.MeterTypeLst_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.MeterTypeLst_Form(_db))
                {
                    f.WindowState = System.Windows.Forms.FormWindowState.Normal;
                    f.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
                LoadMeterCfgLookup();
                if (_meterCfgRepoType != null) _meterCfgRepoType.DataSource = _meterCfgLookup;
                if (GridViewMeterCfg != null) GridViewMeterCfg.RefreshData();
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Open Meter Type maintenance failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void GridViewItems_FocusedRowChangedMeterCfg(object sender,
            DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            BindItemMeterPanel();
        }

        /// <summary>The ItemEditData behind the focused items-grid row (null on the NEW row / no rows).</summary>
        private ItemEditData FocusedItemData()
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return null;
            DataRowView drv = GridViewItems.GetRow(rh) as DataRowView;
            if (drv == null) return null;
            int no;
            if (!int.TryParse(Convert.ToString(drv.Row["No"]), out no)) return null;
            int idx = no - 1;
            return (idx >= 0 && idx < _items.Count) ? _items[idx] : null;
        }

        private void BindItemMeterPanel()
        {
            if (GrpMeterCfg == null) return;
            ItemEditData d = FocusedItemData();
            _meterCfgItem = d;
            if (d == null)
            {
                GrpMeterCfg.Text = "Meter Configuration  (select a service item above)";
                GridMeterCfg.DataSource = null;
                BtnMeterCfgAdd.Enabled = false;
                BtnMeterCfgDel.Enabled = false;
                return;
            }
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            if (!d.Meters.Columns.Contains("MachineSerialNo")) d.Meters.Columns.Add("MachineSerialNo", typeof(string));
            if (!d.Meters.Columns.Contains("Description")) d.Meters.Columns.Add("Description", typeof(string));
            // Meters may only be ADDED once the service item is saved (has a real number) — otherwise
            // there is no Service Item No to tell which meter belongs to which machine.
            bool saved = d.ItemKey > 0;
            int meterCount = 0;
            foreach (DataRow mrow in d.Meters.Rows) if (mrow.RowState != DataRowState.Deleted) meterCount++;
            // A cloned/pasted <NEW> machine already CARRIES its copied meters — say so, or the old
            // "save first, then add meters" caption reads as "the meters did not copy".
            GrpMeterCfg.Text = saved
                ? "Meter Configuration — " + d.ServiceItemNo
                : (meterCount > 0
                    ? "Meter Configuration — <NEW>  (" + meterCount + " meter(s) copied — they save together with the contract; new number at Save)"
                    : "Meter Configuration — <NEW>  (save the contract first, then add meters here)");
            GridMeterCfg.DataSource = d.Meters;
            BtnMeterCfgAdd.Enabled = saved;
            BtnMeterCfgDel.Enabled = true;
        }

        private void ViewMeterCfg_UnboundData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName != "PanelServiceItemNo" || !e.IsGetData) return;
            e.Value = _meterCfgItem == null ? ""
                : (string.IsNullOrEmpty(_meterCfgItem.ServiceItemNo) ? "<NEW>" : _meterCfgItem.ServiceItemNo);
        }

        // Price button on a meter row: pick a Multi-Price scheme + view/override its tier ladder.
        // The dialog validates the two billing traps (last bracket must be unlimited; a 0.00 free
        // band conflicts with Free Qty) and tells us when Free Qty must be zeroed.
        private void RepoPricePick_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            GridViewMeterCfg.CloseEditor();
            DataRow r = GridViewMeterCfg.GetFocusedDataRow();
            if (r == null) return;
            // On a RENTAL-WAIVE meter the same button opens the WAIVE configuration instead —
            // the engine evaluates these conditions at every Generate (no manual monitoring).
            string mtType = r["MeterTypeCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterTypeCode"]).Trim();
            if (IsWaiveType(mtType))
            {
                int wn0 = r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0;
                decimal wt0 = r.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(r["WaiveTargetAmount"]) : 0m;
                decimal wpt0 = r.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(r["WaivePartialThreshold"]) : 0m;
                decimal wpa0 = r.Table.Columns.Contains("WaivePartialAmount") ? AsDec(r["WaivePartialAmount"]) : 0m;
                string ws0 = r.Table.Columns.Contains("WaiveScope") ? AsStr(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form wdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form(mtType, wn0, wt0, wpt0, wpa0, ws0))
                {
                    if (wdlg.ShowDialog(this) != DialogResult.OK) return;
                    r["WaiveFirstNMonths"] = wdlg.FirstNMonths;
                    r["WaiveTargetAmount"] = wdlg.TargetAmount;
                    r["WaivePartialThreshold"] = wdlg.PartialThreshold;
                    r["WaivePartialAmount"] = wdlg.PartialAmount;
                    r["WaiveScope"] = wdlg.Scope;
                    _dirty = true;
                    GridViewMeterCfg.RefreshData();
                }
                return;
            }
            // Committed-minimum (MIN) meter: the button configures WHICH print charges count
            // toward the committed amount (BK / CL / both). The top-up maths runs at Generate.
            if (IsCommitType(mtType))
            {
                decimal camt = Math.Abs(r["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(r["MinimumCharges"]));
                string cs0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value ? Convert.ToString(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form cdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form(mtType, camt, cs0))
                {
                    if (cdlg.ShowDialog(this) != DialogResult.OK) return;
                    r["WaiveScope"] = cdlg.Scope;
                    _dirty = true;
                    GridViewMeterCfg.RefreshData();
                }
                return;
            }
            // Plain RENTAL meters have no per-copy ladder — the Multi-Price dialog is meaningless
            // there, so the button is a silent no-op.
            if (IsFlatType(mtType)) return;
            string cur = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string curCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            decimal foc = r["FOCQty"] == DBNull.Value ? 0m : Convert.ToDecimal(r["FOCQty"]);
            using (ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form dlg =
                new ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form(_db, cur, curCsv, foc))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                r["MeterMultiPriceCode"] = dlg.SelectedCode;
                r["CustomTiers"] = dlg.CustomCsv;
                _dirty = true;
                _ladderFocCache.Clear();   // scheme tiers may have changed elsewhere — recompute displays
                GridViewMeterCfg.RefreshData();
            }
        }

        private void BtnMeterCfgAdd_Click(object sender, EventArgs e)
        {
            ItemEditData d = _meterCfgItem;
            if (d == null) { XtraMessageBox.Show("Select a service item in the grid first.", "Meters"); return; }
            if (d.ItemKey <= 0)
            {
                XtraMessageBox.Show("This service item is not saved yet (<NEW>). Save the contract first — " +
                    "then its Service Item No exists and meters can be added here.", "Meters");
                return;
            }
            if (d.Meters == null) { d.Meters = zSCP2_Item_Form.CreateMetersTable(); GridMeterCfg.DataSource = d.Meters; }
            DataRow r = d.Meters.NewRow();
            r["MeterRole"] = "";   // NO default — the user must pick BK / CL / NA (save blocks an empty role)
            r["Description"] = "";
            r["MachineSerialNo"] = "";
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            r["CustomTiers"] = "";
            r["WaiveFirstNMonths"] = 0; r["WaiveTargetAmount"] = 0m; r["WaivePartialThreshold"] = 0m; r["WaivePartialAmount"] = 0m; r["WaiveScope"] = "BKCL";
            d.Meters.Rows.Add(r);
            GridViewMeterCfg.FocusedRowHandle = GridViewMeterCfg.RowCount - 1;
            _dirty = true;
        }

        private void BtnMeterCfgDel_Click(object sender, EventArgs e)
        {
            int rh = GridViewMeterCfg.FocusedRowHandle;
            if (rh < 0 || _meterCfgItem == null) return;
            // Removing a saved meter also removes its reading history on save (cascade) — make sure.
            string code = Convert.ToString(GridViewMeterCfg.GetRowCellValue(rh, "MeterTypeCode"));
            // Deleting the machine's only RENTAL while a WAIVE stays would break the waive rule.
            if (code.Trim().Length > 0 && !IsWaiveType(code)
                && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(code)
                && DeleteWouldOrphanWaive(_meterCfgItem, GridViewMeterCfg.GetDataRow(rh)))
            {
                XtraMessageBox.Show("This machine still has a RENTAL WAIVE meter — remove the waive line first " +
                    "(or keep a rental meter). A waive needs a rent to waive.", "Remove Meter");
                return;
            }
            if (_meterCfgItem.ItemKey > 0 && !string.IsNullOrEmpty(code))
            {
                DialogResult ok = XtraMessageBox.Show(
                    "Remove meter '" + code + "' from " + _meterCfgItem.ServiceItemNo + "?\r\n\r\n" +
                    "When the contract is saved, this meter AND its reading/invoice history are deleted permanently.",
                    "Remove Meter", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ok != DialogResult.Yes) return;
            }
            GridViewMeterCfg.DeleteRow(rh);
            _dirty = true;
        }

        // Rental (flat) meter rows shade amber — their home is now Rental Maintenance; editing here
        // still works (nothing is locked) but the colour steers users to the dedicated module.
        private static readonly System.Drawing.Color _rentalRowColor = System.Drawing.Color.FromArgb(255, 248, 225);

        private bool IsFlatType(string meterTypeCode)
        {
            if (_meterCfgLookup == null || string.IsNullOrEmpty(meterTypeCode)) return false;
            DataRow[] f = _meterCfgLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["IsFlatCharge"]) == "Y";
        }

        // Rental-Waive meter type: the master-style contra line whose firing the engine decides
        // per month from the row's Waive Configuration (the "..." button opens it).
        private bool IsWaiveType(string meterTypeCode)
        {
            if (_meterCfgLookup == null || !_meterCfgLookup.Columns.Contains("IsRentalWaive")
                || string.IsNullOrEmpty(meterTypeCode)) return false;
            DataRow[] f = _meterCfgLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["IsRentalWaive"]) == "Y";
        }

        // "Copy Meters To…": clone this machine's meters (all or picked) onto other machines of
        // the contract — in memory; the contract Save persists them. Identity never copies:
        // InitialReading resets to 0 and MachineSerialNo clears. Skips per target: duplicate meter
        // type, BK/CL role clash, and waive meters landing on a machine without a rental.
        private void BtnMeterCfgCopyTo_Click(object sender, EventArgs e)
        {
            ItemEditData src = _meterCfgItem;
            if (src == null || src.Meters == null)
            { XtraMessageBox.Show("Select a service item in the grid first.", "Copy Meters To"); return; }
            System.Collections.Generic.List<DataRow> meterRows = new System.Collections.Generic.List<DataRow>();
            System.Collections.Generic.List<string> meterLabels = new System.Collections.Generic.List<string>();
            foreach (DataRow r in src.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                meterRows.Add(r);
                string role = Convert.ToString(r["MeterRole"]).Trim();
                meterLabels.Add(t + (role.Length > 0 ? "   [" + role + "]" : ""));
            }
            if (meterRows.Count == 0)
            { XtraMessageBox.Show("This machine has no meters to copy.", "Copy Meters To"); return; }
            System.Collections.Generic.List<ItemEditData> targets = new System.Collections.Generic.List<ItemEditData>();
            System.Collections.Generic.List<string> targetLabels = new System.Collections.Generic.List<string>();
            foreach (ItemEditData d in _items)
            {
                if (object.ReferenceEquals(d, src)) continue;
                targets.Add(d);
                string no = string.IsNullOrEmpty(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                targetLabels.Add(no + (string.IsNullOrEmpty(d.SerialNumber) ? "" : "   (" + d.SerialNumber + ")"));
            }
            if (targets.Count == 0)
            { XtraMessageBox.Show("This contract has no other machine to copy to.", "Copy Meters To"); return; }

            using (ServiceContractPhotocopier.Classes.CommonForms.CopyMetersTo_Form dlg =
                new ServiceContractPhotocopier.Classes.CommonForms.CopyMetersTo_Form(meterLabels, targetLabels))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                int copied = 0, machines = 0, dupType = 0, dupRole = 0, waiveSkipped = 0;
                // Non-waive meters copy FIRST so a rental in the same batch satisfies the waive rule.
                System.Collections.Generic.List<DataRow> picked = new System.Collections.Generic.List<DataRow>();
                foreach (int mi in dlg.MeterIndices) picked.Add(meterRows[mi]);
                picked.Sort(delegate(DataRow a, DataRow b)
                {
                    bool wa = IsWaiveType(Convert.ToString(a["MeterTypeCode"]).Trim());
                    bool wb = IsWaiveType(Convert.ToString(b["MeterTypeCode"]).Trim());
                    return wa == wb ? 0 : (wa ? 1 : -1);
                });
                foreach (int ti in dlg.TargetIndices)
                {
                    ItemEditData tgt = targets[ti];
                    if (tgt.Meters == null) tgt.Meters = zSCP2_Item_Form.CreateMetersTable();
                    bool any = false;
                    foreach (DataRow srow in picked)
                    {
                        string t = Convert.ToString(srow["MeterTypeCode"]).Trim();
                        string role = Convert.ToString(srow["MeterRole"]).Trim().ToUpperInvariant();
                        bool clash = false, roleClash = false, tgtHasRental = false;
                        foreach (DataRow x in tgt.Meters.Rows)
                        {
                            if (x.RowState == DataRowState.Deleted) continue;
                            string xt = Convert.ToString(x["MeterTypeCode"]).Trim();
                            if (xt.Length == 0) continue;
                            if (string.Equals(xt, t, StringComparison.OrdinalIgnoreCase)) clash = true;
                            string xr = Convert.ToString(x["MeterRole"]).Trim().ToUpperInvariant();
                            if ((role == "BK" || role == "CL") && xr == role) roleClash = true;
                            if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(xt) && !IsWaiveType(xt)) tgtHasRental = true;
                        }
                        if (clash) { dupType++; continue; }
                        if (roleClash) { dupRole++; continue; }
                        if (IsWaiveType(t) && !tgtHasRental) { waiveSkipped++; continue; }
                        DataRow nr = tgt.Meters.NewRow();
                        foreach (System.Data.DataColumn col in tgt.Meters.Columns)
                            if (srow.Table.Columns.Contains(col.ColumnName))
                                nr[col.ColumnName] = srow[col.ColumnName];
                        nr["MachineSerialNo"] = "";
                        nr["InitialReading"] = 0m;   // the PHYSICAL counter's identity — never copied
                        tgt.Meters.Rows.Add(nr);
                        copied++; any = true;
                    }
                    if (any) machines++;
                }
                _dirty = true;
                RebuildItemsView();
                BindItemMeterPanel();
                string msg = copied + " meter(s) copied to " + machines + " machine(s).";
                if (dupType > 0) msg += "\r\n" + dupType + " skipped — the target already has that meter type.";
                if (dupRole > 0) msg += "\r\n" + dupRole + " skipped — the target already has a meter with that BK/CL role.";
                if (waiveSkipped > 0) msg += "\r\n" + waiveSkipped + " waive meter(s) skipped — the target has no RENTAL meter.";
                msg += "\r\n\r\nSave the contract to persist.";
                XtraMessageBox.Show(msg, "Copy Meters To", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // True when the machine's meter set violates "a waive needs a rental".
        private bool WaiveRuleBroken(ItemEditData d)
        {
            if (d == null || d.Meters == null) return false;
            bool hasWaive = false, hasRent = false;
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                if (IsWaiveType(t)) hasWaive = true;
                else if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t)) hasRent = true;
            }
            return hasWaive && !hasRent;
        }

        // Would removing THIS rental row leave a waive meter behind with no rental?
        private bool DeleteWouldOrphanWaive(ItemEditData d, DataRow removing)
        {
            if (d == null || d.Meters == null) return false;
            bool hasWaive = false, otherRent = false;
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted || ReferenceEquals(r, removing)) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                if (IsWaiveType(t)) hasWaive = true;
                else if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t)) otherRent = true;
            }
            return hasWaive && !otherRent;
        }

        // Committed-minimum (MIN) meter type — by code convention or the type's Default Role.
        private bool IsCommitType(string meterTypeCode)
        {
            if (string.IsNullOrEmpty(meterTypeCode)) return false;
            if (ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinMeterCode(meterTypeCode)) return true;
            if (_meterCfgLookup == null || !_meterCfgLookup.Columns.Contains("DefaultRole")) return false;
            DataRow[] f = _meterCfgLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["DefaultRole"]).Trim().ToUpperInvariant() == "COMMIT";
        }

        // Stock wording check: the text equals some meter type's default description.
        private bool IsTypeDefaultDescription(string desc)
        {
            if (_meterCfgLookup == null || string.IsNullOrEmpty(desc)) return false;
            foreach (DataRow t in _meterCfgLookup.Rows)
                if (string.Equals(Convert.ToString(t["Description"]).Trim(), desc, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // True when the machine already carries a real RENTAL meter (RA-* flat that is NOT a waive).
        private bool MachineHasRentalMeter(ItemEditData d)
        {
            if (d == null || d.Meters == null) return false;
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t) && !IsWaiveType(t)) return true;
            }
            return false;
        }

        private void ViewMeterCfg_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            // Unit Price and Free Qty are DEAD while a multi-price ladder is in effect (the ladder
            // prices the copies AND carries the free band) — grey both out; Free Qty shows the
            // ladder's own free quantity via CustomColumnDisplayText.
            if (e.Column != null && (e.Column.FieldName == "ChargesRate" || e.Column.FieldName == "FOCQty")
                && RowHasLadder(e.RowHandle))
            {
                e.Appearance.BackColor = System.Drawing.Color.Gainsboro;
                e.Appearance.ForeColor = System.Drawing.Color.Gray;
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            string type = Convert.ToString(GridViewMeterCfg.GetRowCellValue(e.RowHandle, "MeterTypeCode"));
            if (!IsFlatType(type)) return;
            // Pin the ForeColor too: on the FOCUSED row DevExpress paints white text, and white on
            // this pale amber is invisible — a freshly added rental row looked like it had no data.
            e.Appearance.BackColor = _rentalRowColor;
            e.Appearance.ForeColor = System.Drawing.Color.Black;
            e.Appearance.Options.UseBackColor = true;
            e.Appearance.Options.UseForeColor = true;
        }

        // A ladder governs the row when a scheme code is picked OR per-meter override tiers exist.
        private bool RowHasLadder(int rowHandle)
        {
            string code = Convert.ToString(GridViewMeterCfg.GetRowCellValue(rowHandle, "MeterMultiPriceCode"));
            string custom = Convert.ToString(GridViewMeterCfg.GetRowCellValue(rowHandle, "CustomTiers"));
            return !string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(custom);
        }

        // Unit Price / Free Qty are not editable while a ladder is in effect (use the price button).
        private void ViewMeterCfg_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewMeterCfg.FocusedColumn != null
                && (GridViewMeterCfg.FocusedColumn.FieldName == "ChargesRate" || GridViewMeterCfg.FocusedColumn.FieldName == "FOCQty")
                && RowHasLadder(GridViewMeterCfg.FocusedRowHandle))
                e.Cancel = true;
        }

        // Ladder free copies per scheme code (SELECT once, then cached; cleared after the picker runs).
        private readonly System.Collections.Generic.Dictionary<string, decimal> _ladderFocCache =
            new System.Collections.Generic.Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        private decimal LadderFocFor(string code, string customCsv)
        {
            if (!string.IsNullOrEmpty(customCsv))
                return ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(
                    zSCP2_Item_Form.ParseTiersCsv(customCsv));
            if (string.IsNullOrEmpty(code)) return 0m;
            decimal cached;
            if (_ladderFocCache.TryGetValue(code, out cached)) return cached;
            decimal foc = 0m;
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT MeterReading, UnitPrice FROM [dbo].[zSCP_MeterMultiPriceItem] " +
                    "WHERE MeterMultiPriceCode = N'" + code.Replace("'", "''") + "' ORDER BY MeterReading", false);
                System.Collections.Generic.List<decimal[]> tiers = new System.Collections.Generic.List<decimal[]>();
                foreach (DataRow t in dt.Rows)
                    tiers.Add(new decimal[] { AsDec(t["MeterReading"]), AsDec(t["UnitPrice"]) });
                foc = ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(tiers);
            }
            catch { }
            _ladderFocCache[code] = foc;
            return foc;
        }

        // Multi-Price cell text: scheme code / "code (Modified)" / "(Custom)" / "".
        // Free Qty cell text while a ladder is active: the LADDER's free quantity (read-only display —
        // the stored per-meter Free Qty is untouched and returns when the ladder is cleared).
        private void ViewMeterCfg_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.ListSourceRowIndex < 0) return;
            bool wantMulti = e.Column.FieldName == "MeterMultiPriceCode";
            bool wantFoc = e.Column.FieldName == "FOCQty";
            if (!wantMulti && !wantFoc) return;
            System.Data.DataView dv = GridMeterCfg.DataSource as System.Data.DataView;
            DataTable src = dv != null ? dv.Table : GridMeterCfg.DataSource as DataTable;
            if (src == null || e.ListSourceRowIndex >= src.DefaultView.Count) return;
            DataRow r = src.DefaultView[e.ListSourceRowIndex].Row;
            string code = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string custom = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            if (wantMulti)
            {
                // A waive meter shows its WAIVE deal here (the ladder cell is unused on flat rows).
                string mtType0 = Convert.ToString(r["MeterTypeCode"]);
                if (IsWaiveType(mtType0))
                {
                    int wn0 = r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0;
                    decimal wt0 = r.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(r["WaiveTargetAmount"]) : 0m;
                    decimal wpt0 = r.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(r["WaivePartialThreshold"]) : 0m;
                    decimal wpa0 = r.Table.Columns.Contains("WaivePartialAmount") ? AsDec(r["WaivePartialAmount"]) : 0m;
                    e.DisplayText = ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form.Summary(wn0, wt0, wpt0, wpa0);
                    return;
                }
                if (IsCommitType(mtType0))
                {
                    string sc0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value
                        ? Convert.ToString(r["WaiveScope"]).Trim().ToUpperInvariant() : "BKCL";
                    e.DisplayText = "MIN: counts " + (sc0 == "BK" ? "BK only" : (sc0 == "CL" ? "CL only" : "BK + CL"));
                    return;
                }
                if (custom.Length > 0) e.DisplayText = code.Length == 0 ? "(Custom)" : code + " (Modified)";
                else e.DisplayText = code;
                return;
            }
            if (code.Length == 0 && custom.Length == 0) return;   // no ladder — show the meter's own Free Qty
            e.DisplayText = LadderFocFor(code, custom).ToString("#,##0.##");
        }

        // Picking a Meter Type pre-fills its pricing (same behaviour as the item dialog); any edit dirties.
        private void ViewMeterCfg_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column != null && e.Column.FieldName == "MinimumCharges")
            {
                // Waive rows keep their amount NEGATIVE (the contra) — a positive entry flips.
                try
                {
                    string rowType = Convert.ToString(GridViewMeterCfg.GetRowCellValue(e.RowHandle, "MeterTypeCode")).Trim();
                    decimal mv = e.Value == null || e.Value == DBNull.Value ? 0m : Convert.ToDecimal(e.Value);
                    if (mv > 0m && IsWaiveType(rowType))
                        GridViewMeterCfg.SetRowCellValue(e.RowHandle, "MinimumCharges", -mv);
                }
                catch { }
            }
            if (e.Column == null || e.Column.FieldName != "MeterTypeCode" || _meterCfgLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _meterCfgLookup.Select("MeterTypeCode='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow m = found[0];
            int rh = e.RowHandle;
            // MACHINE INVARIANT: a RENTAL WAIVE meter requires a real RENTAL meter. ANY type change
            // that breaks it is rejected on the spot — picking a waive first, OR re-typing the only
            // rental away while a waive stays. A fresh row is removed WHOLE (no half-filled ghost);
            // an existing row snaps back to its original type (deleting it would kill its readings).
            if (WaiveRuleBroken(_meterCfgItem))
            {
                XtraMessageBox.Show("A RENTAL WAIVE meter needs a RENTAL meter on the same machine.\r\n" +
                    "Add/keep the rent line first (or remove the waive line).", "Meter");
                DataRow gr = GridViewMeterCfg.GetDataRow(rh);
                if (gr != null && gr.RowState == DataRowState.Added)
                    GridViewMeterCfg.DeleteRow(rh);
                else if (gr != null)
                {
                    gr["MeterTypeCode"] = gr["MeterTypeCode", DataRowVersion.Original];
                    gr["Description"] = gr["Description", DataRowVersion.Original];
                }
                GridViewMeterCfg.RefreshData();
                return;
            }
            // Re-picking a type refreshes the Description too — UNLESS the user hand-typed their own
            // (text matching some type's default is stock wording, not a customization).
            object curDesc = GridViewMeterCfg.GetRowCellValue(rh, "Description");
            string curDescS = curDesc == null || curDesc == DBNull.Value ? "" : curDesc.ToString().Trim();
            if (curDescS.Length == 0 || IsTypeDefaultDescription(curDescS))
                GridViewMeterCfg.SetRowCellValue(rh, "Description", m.Table.Columns.Contains("Description") ? m["Description"] : "");
            // Waive types carry their amount NEGATIVE on the machine (the contra line) — the
            // master's positive default flips sign on fill.
            if (IsWaiveType(code))
                GridViewMeterCfg.SetRowCellValue(rh, "MinimumCharges",
                    -Math.Abs(m["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(m["MinimumCharges"])));
            else
                GridViewMeterCfg.SetRowCellValue(rh, "MinimumCharges", m["MinimumCharges"]);
            GridViewMeterCfg.SetRowCellValue(rh, "ChargesRate", m["ChargesRate"]);
            GridViewMeterCfg.SetRowCellValue(rh, "MeterMultiPriceCode", m["MeterMultiPriceCode"]);
            GridViewMeterCfg.SetRowCellValue(rh, "RebateQtyInPercent", m["RebateQtyInPercent"]);
            GridViewMeterCfg.SetRowCellValue(rh, "FOCQty", m["FOCQty"]);
            // Role follows the new type when blank or a NON-colour tag (a re-typed rental must not
            // keep a stale RENTAL/WAIVE/COMMIT role); a hand-picked BK/CL is never clobbered.
            object curRole = GridViewMeterCfg.GetRowCellValue(rh, "MeterRole");
            string curRoleS = curRole == null ? "" : curRole.ToString().Trim().ToUpperInvariant();
            if (curRoleS == "" || curRoleS == "NA" || curRoleS == "RENTAL" || curRoleS == "WAIVE" || curRoleS == "COMMIT")
            {
                string defRole = m.Table.Columns.Contains("DefaultRole") ? Convert.ToString(m["DefaultRole"]).Trim().ToUpperInvariant() : "";
                string inferred = defRole.Length > 0 ? defRole
                    : InferMeterRole(code, m.Table.Columns.Contains("Description") ? Convert.ToString(m["Description"]) : "");
                if (inferred.Length > 0) GridViewMeterCfg.SetRowCellValue(rh, "MeterRole", inferred);
            }
        }

        // Infer BK (black) / CL (colour) from a meter type's code + description; rental and
        // committed-minimum types infer "NA" (they have no colour role). Blank = cannot tell —
        // the role stays EMPTY and the save validation forces the user to pick one consciously
        // (no silent "NA" default: a forgotten role would break Black/Colour billing + API fetch).
        internal static string InferMeterRole(string code, string desc)
        {
            string u = ((code ?? "") + " " + (desc ?? "")).ToUpperInvariant();
            if (u.Contains("COLOR") || u.Contains("COLOUR") || u.Contains(".CL.") || u.Contains("-CL")) return "CL";
            if (u.Contains(".BK.") || u.Contains("-BK") || u.Contains(" BK") || u.Contains("BLACK")) return "BK";
            if (u.Contains("(W)")) return "WAIVE";
            if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(code)) return "RENTAL";
            if (ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinMeterCode(code)) return "COMMIT";
            return "";
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            ApplyContractDateDefaults(d);   // Service Start / Expiry follow the contract dates
            // Add a New Service Item under THIS contract: contract shown read-only, billing day mapped.
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, d, (int)SpnBillingDay.Value, TxtContractNo.Text.Trim()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _items.Add(d);
                    _dirty = true;
                    RebuildItemsView();
                }
            }
        }

        private void BtnEditItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            // Embedded mode (4-arg): shows Contract No + Customer read-only and locks Contract Type —
            // the same experience as Add-from-contract and Edit-from-the-item-list.
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, _items[idx], (int)SpnBillingDay.Value, TxtContractNo.Text.Trim()))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) { _dirty = true; RebuildItemsView(); }
            }
        }

        // DELETE: permanently removes the service item from the DATABASE — meters, readings, staged
        // entries and invoice links cascade away. Guarded by a DOUBLE confirmation (warning + typed
        // DELETE). To merely take an item out of the contract, Detach is the right action.
        private void BtnDelItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            ItemEditData d = _items[idx];
            string no = string.IsNullOrEmpty(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;

            // Unsaved row: nothing exists in the DB yet — one confirm is enough.
            if (d.ItemKey <= 0)
            {
                if (XtraMessageBox.Show("Discard the new (unsaved) service item row?", "Delete Service Item",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                _items.RemoveAt(idx);
                _dirty = true;
                RebuildItemsView();
                return;
            }

            // Confirm #1: the SUPER warning.
            if (XtraMessageBox.Show(
                "PERMANENTLY DELETE service item " + no + "?\r\n\r\n" +
                "This deletes the machine from the database TOGETHER WITH:\r\n" +
                "   -  all its meters\r\n" +
                "   -  ALL meter readings and staged entries\r\n" +
                "   -  its invoice links (meter transactions)\r\n" +
                "   -  its ownership history and provided-item lines\r\n\r\n" +
                "THIS CANNOT BE UNDONE.\r\n\r\n" +
                "If you only want to take it out of this contract, use 'Detach from Contract' instead.\r\n\r\n" +
                "Continue to the final confirmation?",
                "DELETE Service Item — WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            // Confirm #2: type DELETE.
            string typed = DevExpress.XtraEditors.XtraInputBox.Show(
                "FINAL CONFIRMATION\r\n\r\nType  DELETE  (capital letters) to permanently delete " + no + ":",
                "DELETE Service Item — Final Confirm", "");
            if (typed == null || typed.Trim() != "DELETE")
            {
                XtraMessageBox.Show("Cancelled — nothing was deleted.", "Delete Service Item");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction("DelItem"))
                    {
                        try
                        {
                            ExecNonQuery(conn, tx, "DELETE FROM dbo.zSCP2_ContractSparePart WHERE ItemKey=@ik", P("@ik", d.ItemKey));
                            // zSCP2_ItemMeter / ItemCode / ItemDebtorHistory cascade from the item row;
                            // zSCP_MeterTrans + zSCP2_MeterEntry cascade from the meters. The reading
                            // log (zSCP2_MeterReadingLog) is append-only audit and is intentionally KEPT.
                            ExecNonQuery(conn, tx, "DELETE FROM dbo.zSCP2_Item WHERE ItemKey=@ik", P("@ik", d.ItemKey));
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _detachedThisSession.Remove(d.ItemKey);
            _items.RemoveAt(idx);
            _dirty = true;
            RebuildItemsView();
            XtraMessageBox.Show("Service item " + no + " was permanently deleted.", "Deleted",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GridViewItems_DoubleClick(object sender, EventArgs e)
        {
            // With inline editing on, double-click on an editable cell should edit the CELL, not open
            // the dialog. The full editor still opens from the No / Service Item No cells or Edit button.
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit =
                GridViewItems.CalcHitInfo(GridItems.PointToClient(System.Windows.Forms.Cursor.Position));
            if (hit.InRowCell && hit.Column != null &&
                hit.Column.OptionsColumn.AllowEdit && hit.Column.FieldName != "ServiceItemNo") return;
            BtnEditItem_Click(null, null);
        }

        // #5: the Service Item No column is an inline SearchLookUpEdit of ORPHAN items (contract-less +
        // detached-this-session). Picking one on a row attaches/swaps it in place. Other columns stay
        // read-only. Combined with the "+" attach button this lets a removed (orphaned) item be
        // re-chosen or a different one attached without leaving the grid.
        private void SetupInlineOrphanLookup()
        {
            _orphanLookup = LoadOrphanItems();
            _orphanRepo = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            _orphanRepo.DataSource = _orphanLookup;
            _orphanRepo.ValueMember = "ServiceItemNo";
            _orphanRepo.DisplayMember = "ServiceItemNo";
            _orphanRepo.NullText = "(pick an unattached item)";
            GridItems.RepositoryItems.Add(_orphanRepo);

            GridViewItems.OptionsBehavior.Editable = true;
            foreach (DevExpress.XtraGrid.Columns.GridColumn c in GridViewItems.Columns)
                c.OptionsColumn.AllowEdit = false;
            DevExpress.XtraGrid.Columns.GridColumn col = GridViewItems.Columns.ColumnByFieldName("ServiceItemNo");
            // NO ColumnEdit for display — the orphan lookup only knows UNATTACHED items, so a bound
            // item's number would render BLANK. The lookup is supplied at EDIT time only (same fix as
            // the spare-part Serial No column).
            if (col != null) col.OptionsColumn.AllowEdit = true;
            GridViewItems.CustomRowCellEditForEditing += new DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventHandler(GridViewItems_ItemNoEditor);
            GridViewItems.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewItems_ShowingEditor);
            GridViewItems.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewItems_CellValueChanged);
        }

        // Edit-time editors: Service Item No -> orphan attach picker; Item Code / Grade -> searchable
        // lookups. Supplied at EDIT time only so display never depends on the lookup contents.
        private void GridViewItems_ItemNoEditor(object sender, DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "ServiceItemNo" && _orphanRepo != null) e.RepositoryItem = _orphanRepo;
            else if (e.Column.FieldName == "ItemCode" && _inlineItemCodeRepo != null) e.RepositoryItem = _inlineItemCodeRepo;
            else if (e.Column.FieldName == "GradeCode" && _inlineGradeRepo != null) e.RepositoryItem = _inlineGradeRepo;
        }

        // ServiceItemNo: only the NEW row (bottom "add" row, or a still-blank row) may open the attach
        // dropdown. All other item fields: editable on REAL rows only — the bottom NEW row is purely
        // the attach picker.
        private void GridViewItems_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewItems.FocusedColumn == null) return;
            int rh = GridViewItems.FocusedRowHandle;
            if (GridViewItems.FocusedColumn.FieldName != "ServiceItemNo")
            {
                if (rh == DevExpress.XtraGrid.GridControl.NewItemRowHandle) e.Cancel = true;
                return;
            }
            if (rh == DevExpress.XtraGrid.GridControl.NewItemRowHandle) return;
            object cur = GridViewItems.GetRowCellValue(rh, "ServiceItemNo");
            bool blank = cur == null || cur == DBNull.Value || cur.ToString().Trim().Length == 0;
            if (!blank) e.Cancel = true;
        }

        // ===================== Inline item editing + quick CSSI creation =====================
        // Every user-editable service-item field can be changed straight in the grid (writes back into
        // the ItemEditData, saved by the normal contract save). "Quick Add Row" appends a blank CSSI —
        // its number shows <NEW> until Save draws a real one (the existing blank-number safety net).

        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _inlineItemCodeRepo;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _inlineGradeRepo;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox _inlineSerialRepo;
        private DataTable _inlineItemLookup;
        private DataTable _inlineGradeLookup;
        private DataTable _inlineSerialLookup;

        private void EnableInlineItemEditing()
        {
            try
            {
                _inlineItemLookup = _db.GetDataTable(
                    "SELECT ItemCode, ISNULL(Description,'') AS Description FROM dbo.Item ORDER BY ItemCode", false);
            }
            catch { _inlineItemLookup = new DataTable(); }
            _inlineItemCodeRepo = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            DevExpress.XtraGrid.Views.Grid.GridView vIc = new DevExpress.XtraGrid.Views.Grid.GridView();
            vIc.OptionsView.ShowAutoFilterRow = true;
            vIc.OptionsBehavior.AutoPopulateColumns = false;
            _inlineItemCodeRepo.PopupView = vIc;
            DevExpress.XtraGrid.Columns.GridColumn icCode = vIc.Columns.AddVisible("ItemCode");
            icCode.Caption = "Item Code"; icCode.Width = 110;
            DevExpress.XtraGrid.Columns.GridColumn icDesc = vIc.Columns.AddVisible("Description");
            icDesc.Caption = "Description"; icDesc.Width = 260;
            _inlineItemCodeRepo.DataSource = _inlineItemLookup;
            _inlineItemCodeRepo.DisplayMember = "ItemCode";
            _inlineItemCodeRepo.ValueMember = "ItemCode";
            _inlineItemCodeRepo.NullText = "";
            GridItems.RepositoryItems.Add(_inlineItemCodeRepo);

            try
            {
                _inlineGradeLookup = _db.GetDataTable(
                    "SELECT ServiceItemGradeCode, ISNULL(Description,'') AS Description " +
                    "FROM dbo.zSCP_LK_ServiceItemGrade WHERE ISNULL(Inactive,'N')='N' ORDER BY ServiceItemGradeCode", false);
            }
            catch { _inlineGradeLookup = new DataTable(); }
            _inlineGradeRepo = new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            DevExpress.XtraGrid.Views.Grid.GridView vGr = new DevExpress.XtraGrid.Views.Grid.GridView();
            vGr.OptionsView.ShowAutoFilterRow = true;
            vGr.OptionsBehavior.AutoPopulateColumns = false;
            _inlineGradeRepo.PopupView = vGr;
            DevExpress.XtraGrid.Columns.GridColumn grCode = vGr.Columns.AddVisible("ServiceItemGradeCode");
            grCode.Caption = "Grade"; grCode.Width = 90;
            DevExpress.XtraGrid.Columns.GridColumn grDesc = vGr.Columns.AddVisible("Description");
            grDesc.Caption = "Description"; grDesc.Width = 220;
            _inlineGradeRepo.DataSource = _inlineGradeLookup;
            _inlineGradeRepo.DisplayMember = "ServiceItemGradeCode";
            _inlineGradeRepo.ValueMember = "ServiceItemGradeCode";
            _inlineGradeRepo.NullText = "";
            GridItems.RepositoryItems.Add(_inlineGradeRepo);

            // Machine Serial: typeable combo — the dropdown lists the serials registered (ItemSerialNo)
            // for the row's Item Code, filled at edit time by GridViewItems_ShownEditor; free text allowed.
            try
            {
                _inlineSerialLookup = _db.GetDataTable(
                    "SELECT ItemCode, SerialNumber FROM dbo.ItemSerialNo " +
                    "WHERE ISNULL(SerialNumber,'') <> '' ORDER BY ItemCode, SerialNumber", false);
            }
            catch { _inlineSerialLookup = new DataTable(); }
            _inlineSerialRepo = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            _inlineSerialRepo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;   // typeable
            GridItems.RepositoryItems.Add(_inlineSerialRepo);
            GridViewItems.ShownEditor += new EventHandler(GridViewItems_ShownEditorSerial);

            DevExpress.XtraEditors.Repository.RepositoryItemDateEdit repoDate =
                new DevExpress.XtraEditors.Repository.RepositoryItemDateEdit();
            repoDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            repoDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            repoDate.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            repoDate.EditFormat.FormatString = "dd/MM/yyyy";
            repoDate.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            GridItems.RepositoryItems.Add(repoDate);

            DevExpress.XtraEditors.Repository.RepositoryItemComboBox repoYN =
                new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repoYN.Items.AddRange(new object[] { "N", "Y" });
            repoYN.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            GridItems.RepositoryItems.Add(repoYN);

            // Service Type: searchable dropdown over the Service Type list (zSCP_LK_ServiceType,
            // maintained under General Setup -> Service Type; seeded from the Contract Type list).
            DataTable stLookup;
            try
            {
                stLookup = _db.GetDataTable(
                    "SELECT ServiceTypeCode, ISNULL(Description,'') AS Description FROM dbo.zSCP_LK_ServiceType " +
                    "WHERE ISNULL(Inactive,'N')='N' ORDER BY ServiceTypeCode", false);
            }
            catch { stLookup = new DataTable(); }
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repoServiceType =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            DevExpress.XtraGrid.Views.Grid.GridView vSt = new DevExpress.XtraGrid.Views.Grid.GridView();
            vSt.OptionsView.ShowAutoFilterRow = true;
            vSt.OptionsBehavior.AutoPopulateColumns = false;
            repoServiceType.PopupView = vSt;
            DevExpress.XtraGrid.Columns.GridColumn stCode = vSt.Columns.AddVisible("ServiceTypeCode");
            stCode.Caption = "Service Type"; stCode.Width = 100;
            DevExpress.XtraGrid.Columns.GridColumn stDesc = vSt.Columns.AddVisible("Description");
            stDesc.Caption = "Description"; stDesc.Width = 220;
            repoServiceType.DataSource = stLookup;
            repoServiceType.DisplayMember = "ServiceTypeCode";
            repoServiceType.ValueMember = "ServiceTypeCode";
            repoServiceType.NullText = "";
            GridItems.RepositoryItems.Add(repoServiceType);

            // ONLINE/OFFLINE definition per machine — drives the ADVANCED invoice number format
            // (Plugin Option); the live fetch status is only the fallback when left undefined.
            DevExpress.XtraEditors.Repository.RepositoryItemComboBox repoMode =
                new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repoMode.Items.AddRange(new object[] { "", "ONLINE", "OFFLINE" });
            repoMode.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            GridItems.RepositoryItems.Add(repoMode);
            DevExpress.XtraGrid.Columns.GridColumn colMode = GridViewItems.Columns.AddVisible("MachineMode");
            colMode.Caption = "Online/Offline";
            colMode.Width = 95;
            colMode.OptionsColumn.AllowEdit = true;
            colMode.ColumnEdit = repoMode;

            SetItemColEditable("ItemCode", null);          // editor supplied at edit time (lookup)
            // Machine Serial: bind the designer column DIRECTLY (a hidden duplicate FieldName once
            // made ColumnByFieldName pick the wrong column and the visible cell stayed locked).
            ColSerial.OptionsColumn.AllowEdit = true;
            ColSerial.ColumnEdit = _inlineSerialRepo;      // permanent combo: arrow + typing
            SetItemColEditable("GradeCode", null);         // editor supplied at edit time (lookup)
            SetItemColEditable("ServiceType", repoServiceType);
            SetItemColEditable("PurchaseDate", repoDate);
            SetItemColEditable("ReferenceNo", null);
            SetItemColEditable("BillingDay", null);
            SetItemColEditable("ServiceStart", repoDate);
            SetItemColEditable("Expiry", repoDate);
            SetItemColEditable("Description", null);
            SetItemColEditable("Inactive", repoYN);

            // "Preventive..." button column: opens the item dialog directly on its Preventive
            // Maintenance tab for the clicked row.
            DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit repoPM =
                new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            repoPM.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            repoPM.Buttons.Clear();
            DevExpress.XtraEditors.Controls.EditorButton pmBtn =
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            pmBtn.Caption = "Preventive...";
            pmBtn.ImageOptions.ImageUri.Uri = "Recurrence;Size16x16";   // verified in the DX gallery
            repoPM.Buttons.Add(pmBtn);
            repoPM.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(RepoPM_ButtonClick);
            GridItems.RepositoryItems.Add(repoPM);
            DevExpress.XtraGrid.Columns.GridColumn colPM = GridViewItems.Columns.AddVisible("PMButton");
            colPM.Caption = "Preventive";
            colPM.UnboundDataType = typeof(string);
            colPM.OptionsColumn.AllowEdit = true;
            colPM.ColumnEdit = repoPM;
            colPM.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            colPM.Width = 90;

        }

        // Cascade: when the Machine Serial cell opens, fill its dropdown with the serials registered
        // for that row's Item Code (typing a free value is still allowed).
        private void GridViewItems_ShownEditorSerial(object sender, EventArgs e)
        {
            if (GridViewItems.FocusedColumn == null || GridViewItems.FocusedColumn.FieldName != "SerialNumber") return;
            DevExpress.XtraEditors.ComboBoxEdit ed = GridViewItems.ActiveEditor as DevExpress.XtraEditors.ComboBoxEdit;
            if (ed == null) return;
            ed.Properties.Items.Clear();
            string code = (GridViewItems.GetFocusedRowCellValue("ItemCode") ?? "").ToString().Trim();
            if (_inlineSerialLookup == null || code.Length == 0) return;
            foreach (DataRow r in _inlineSerialLookup.Select("ItemCode='" + code.Replace("'", "''") + "'"))
                ed.Properties.Items.Add(r["SerialNumber"].ToString());
        }

        private void SetItemColEditable(string field, DevExpress.XtraEditors.Repository.RepositoryItem edit)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewItems.Columns.ColumnByFieldName(field);
            if (c == null) return;
            c.OptionsColumn.AllowEdit = true;
            if (edit != null) c.ColumnEdit = edit;
        }

        // Preventive button on a row: open that item's editor directly on the Preventive tab.
        private void RepoPM_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            GridViewItems.PostEditor();
            GridViewItems.CloseEditor();
            ItemEditData d = FocusedItemData();
            if (d == null) return;
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, d, (int)SpnBillingDay.Value, TxtContractNo.Text.Trim()))
            {
                dlg.ShowPreventiveTab();
                if (dlg.ShowDialog(this) == DialogResult.OK) { _dirty = true; RebuildItemsView(); }
            }
        }

        private void BtnItemQuickAdd_Click(object sender, EventArgs e)
        {
            ItemEditData d = new ItemEditData();
            d.ServiceItemNo = "";                 // shows <NEW>; a real number is drawn at save
            d.ServiceItemNoIsAuto = false;
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            d.SpareParts = CreateSparePartsTable();
            ApplyContractDateDefaults(d);         // Service Start / Expiry follow the contract dates
            _items.Add(d);
            _dirty = true;
            RebuildItemsView();
            int rh = GridViewItems.DataRowCount - 1;
            if (rh >= 0)
            {
                GridViewItems.FocusedRowHandle = rh;
                DevExpress.XtraGrid.Columns.GridColumn c = GridViewItems.Columns.ColumnByFieldName("ItemCode");
                if (c != null) { GridViewItems.FocusedColumn = c; GridViewItems.ShowEditor(); }
            }
        }

        // Write an inline cell edit back into the row's ItemEditData (display table is throwaway).
        private void ApplyInlineItemEdit(DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.RowHandle < 0 || e.Column == null) return;
            object noVal = GridViewItems.GetRowCellValue(e.RowHandle, "No");
            if (noVal == null || noVal == DBNull.Value) return;
            int idx = Convert.ToInt32(noVal) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            ItemEditData d = _items[idx];
            string s = (e.Value == null || e.Value == DBNull.Value) ? "" : e.Value.ToString().Trim();
            switch (e.Column.FieldName)
            {
                case "ItemCode":
                    d.ItemCode = s;
                    // Same convenience as the item dialog: picking an item fills an empty Description.
                    if (string.IsNullOrEmpty(d.Description) && _inlineItemLookup != null && s.Length > 0)
                    {
                        DataRow[] f = _inlineItemLookup.Select("ItemCode='" + s.Replace("'", "''") + "'");
                        if (f.Length > 0)
                        {
                            d.Description = Convert.ToString(f[0]["Description"]);
                            GridViewItems.SetRowCellValue(e.RowHandle, "Description", d.Description);
                        }
                    }
                    break;
                case "SerialNumber": d.SerialNumber = s; break;
                case "GradeCode": d.GradeCode = s; break;
                case "ServiceType": d.ServiceTypeCode = s; break;
                case "PurchaseDate":
                    d.PurchaseDate = (e.Value == null || e.Value == DBNull.Value)
                        ? (DateTime?)null : Convert.ToDateTime(e.Value);
                    break;
                case "ReferenceNo": d.ReferenceNo = s; break;
                case "Description": d.Description = s; break;
                case "Inactive": d.Inactive = s.Equals("Y", StringComparison.OrdinalIgnoreCase); break;
                case "MachineMode":
                {
                    string mm = s.Trim().ToUpperInvariant();
                    d.MachineMode = mm == "ONLINE" || mm == "OFFLINE" ? mm : "";
                    break;
                }
                case "BillingDay":
                {
                    int bd;
                    // Same cap as the contract header and the item dialog: 1–28, so every month has the day.
                    d.BillingDayOverride = (int.TryParse(s, out bd) && bd >= 1 && bd <= 28) ? (int?)bd : null;
                    break;
                }
                case "ServiceStart":
                    d.ServiceStartDate = (e.Value == null || e.Value == DBNull.Value)
                        ? (DateTime?)null : Convert.ToDateTime(e.Value);
                    // Blanking a machine's date means "follow the contract" — snap the contract date
                    // back in immediately so the revert is VISIBLE, never a silent after-save surprise.
                    if (!d.ServiceStartDate.HasValue && DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value)
                    {
                        d.ServiceStartDate = Convert.ToDateTime(DtStartDate.EditValue).Date;
                        BeginInvoke(new MethodInvoker(RebuildItemsView));
                    }
                    break;
                case "Expiry":
                    d.ServiceExpiryDate = (e.Value == null || e.Value == DBNull.Value)
                        ? (DateTime?)null : Convert.ToDateTime(e.Value);
                    if (!d.ServiceExpiryDate.HasValue && DtExpiryDate.EditValue != null && DtExpiryDate.EditValue != DBNull.Value)
                    {
                        d.ServiceExpiryDate = Convert.ToDateTime(DtExpiryDate.EditValue).Date;
                        BeginInvoke(new MethodInvoker(RebuildItemsView));
                    }
                    break;
                default: return;
            }
            // A machine's Expiry can never sit before its Service Start — reject the edit on the
            // spot (the offending cell is cleared) instead of letting a dead date range linger.
            if ((e.Column.FieldName == "ServiceStart" || e.Column.FieldName == "Expiry")
                && d.ServiceStartDate.HasValue && d.ServiceExpiryDate.HasValue
                && d.ServiceExpiryDate.Value.Date < d.ServiceStartDate.Value.Date)
            {
                XtraMessageBox.Show("Expiry cannot be earlier than Service Start.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (e.Column.FieldName == "Expiry") d.ServiceExpiryDate = null;
                else d.ServiceStartDate = null;
                BeginInvoke(new MethodInvoker(RebuildItemsView));
            }
            _dirty = true;
        }

        private void RefreshOrphanLookup()
        {
            if (_orphanRepo == null) return;
            _orphanLookup = LoadOrphanItems();
            _orphanRepo.DataSource = _orphanLookup;
        }

        private void GridViewItems_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName != "ServiceItemNo") { ApplyInlineItemEdit(e); return; }
            string picked = e.Value == null ? "" : e.Value.ToString().Trim();
            if (picked.Length == 0) return;
            object noVal = e.RowHandle == DevExpress.XtraGrid.GridControl.NewItemRowHandle
                ? null : GridViewItems.GetRowCellValue(e.RowHandle, "No");
            if (noVal == null || noVal == DBNull.Value)
            {
                // NEW row: picking an orphan ATTACHES it as an additional service item.
                DataRow[] nf = _orphanLookup.Select("ServiceItemNo='" + picked.Replace("'", "''") + "'");
                if (nf.Length == 0) return;
                long nk = Convert.ToInt64(nf[0]["ItemKey"]);
                foreach (ItemEditData ex2 in _items)
                    if (ex2.ItemKey == nk) { BeginInvoke(new MethodInvoker(RebuildItemsView)); return; }
                _items.Add(LoadOneItem(_db, nk));
                _detachedThisSession.Remove(nk);
                _dirty = true;
                BeginInvoke(new MethodInvoker(delegate
                {
                    try { GridViewItems.CancelUpdateCurrentRow(); } catch { }
                    RebuildItemsView();
                }));
                return;
            }
            int idx = Convert.ToInt32(noVal) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            if (_items[idx].ServiceItemNo == picked) return;   // unchanged
            DataRow[] f = _orphanLookup.Select("ServiceItemNo='" + picked.Replace("'", "''") + "'");
            if (f.Length == 0) return;
            long newKey = Convert.ToInt64(f[0]["ItemKey"]);
            long oldKey = _items[idx].ItemKey;
            _items[idx] = LoadOneItem(_db, newKey);                 // swap the row's item
            _detachedThisSession.Remove(newKey);
            if (oldKey > 0 && !_detachedThisSession.Contains(oldKey)) _detachedThisSession.Add(oldKey);
            _dirty = true;
            BeginInvoke(new MethodInvoker(RebuildItemsView));  // rebuild after the edit commits
        }

        // Expiry column: RED bold if expired (before today), GREEN bold if still active.
        // RowCellStyle fires per cell per repaint — cache the bold font instead of allocating one each time.
        private System.Drawing.Font _boldFont;
        private static readonly System.Drawing.Color _expiredRed = System.Drawing.Color.FromArgb(198, 40, 40);
        private static readonly System.Drawing.Color _activeGreen = System.Drawing.Color.FromArgb(46, 125, 50);

        private void GridViewItems_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column != ColExpiry) return;
            object v = GridViewItems.GetRowCellValue(e.RowHandle, ColExpiry);
            if (v == null || v == DBNull.Value) return;
            DateTime exp = Convert.ToDateTime(v);
            e.Appearance.ForeColor = exp.Date < DateTime.Today ? _expiredRed : _activeGreen;
            if (_boldFont == null)
                _boldFont = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.Font = _boldFont;
        }

        // ===================== Spare Parts / Services Provided =====================

        private DataTable _spareParts;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _spCheck;
        private DataTable _spItemLookup;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _spItemRepo;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _spSerialRepo;
        private DataTable _spSerialLookup;   // full ItemSerialNo list; filtered per row by ItemCode

        internal static DataTable CreateSparePartsTable()
        {
            DataTable dt = new DataTable("SpareParts");
            dt.Columns.Add("SparePartKey", typeof(long));
            dt.Columns.Add("ItemKey", typeof(long));       // set => item-bound (read-only on the contract)
            dt.Columns.Add("Bound", typeof(bool));         // ItemKey present -> true
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemCode", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("SerialNumber", typeof(string));
            dt.Columns.Add("Unlimited", typeof(bool));
            dt.Columns.Add("UOM", typeof(string));
            dt.Columns.Add("Quantity", typeof(decimal));
            dt.Columns.Add("Discount", typeof(string));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("Amount", typeof(decimal));
            dt.Columns.Add("TaxType", typeof(string));
            dt.Columns.Add("TaxInclusive", typeof(bool));
            dt.Columns.Add("TaxRate", typeof(decimal));
            dt.Columns.Add("TaxAmount", typeof(decimal));
            dt.Columns.Add("AmountAfterTax", typeof(decimal));
            dt.Columns.Add("Pos", typeof(int));
            return dt;
        }

        private void SetupSparePartsGrid()
        {
            _spCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            GridSpareParts.RepositoryItems.Add(_spCheck);
            _spItemLookup = LoadItemLookup(_db);
            _spItemRepo = MakeItemCodeRepo(_spItemLookup);
            GridSpareParts.RepositoryItems.Add(_spItemRepo);
            _spSerialLookup = LoadSerialLookup(_db);
            _spSerialRepo = MakeSerialRepo(_spSerialLookup);
            GridSpareParts.RepositoryItems.Add(_spSerialRepo);

            GridViewSpareParts.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            GridViewSpareParts.OptionsBehavior.Editable = true;
            GridViewSpareParts.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(SpareParts_CellValueChanged);
            GridViewSpareParts.ShowingEditor += new System.ComponentModel.CancelEventHandler(SpareParts_ShowingEditor);
            GridViewSpareParts.CustomRowCellEditForEditing += new DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventHandler(SpareParts_SerialEditor);

            _spareParts = CreateSparePartsTable();
            GridSpareParts.DataSource = _spareParts.DefaultView;
            _spareParts.DefaultView.Sort = "Pos";
            ConfigureSpareView(GridViewSpareParts, _spCheck, _spItemRepo, _spSerialRepo);
            GridViewSpareParts.RowHeight = 26;   // ~20% taller rows for easier editing
        }

        // Item master lookup for the "Item Provided" grid's Item Code column (like invoice / stock
        // issue): pick an item code and its Description + UOM + Tax auto-fill.
        internal static DataTable LoadItemLookup(DBSetting db)
        {
            try
            {
                return db.GetDataTable(
                    "SELECT ItemCode, ISNULL(Description,'') AS Description, ISNULL(SalesUOM,'') AS UOM, " +
                    "ISNULL(TaxCode,'') AS TaxType FROM [dbo].[Item] ORDER BY ItemCode", false);
            }
            catch { return new DataTable(); }
        }

        internal static DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit MakeItemCodeRepo(DataTable itemLookup)
        {
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repo =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            repo.DataSource = itemLookup;
            repo.ValueMember = "ItemCode";
            repo.DisplayMember = "ItemCode";
            repo.NullText = "";
            return repo;   // popup grid auto-populates the 4 datasource columns
        }

        // Serial numbers from AutoCount's serial tracking (ItemSerialNo): the provided-item grid's
        // Serial No column picks from these (searchable popup shows ItemCode + SerialNumber).
        internal static DataTable LoadSerialLookup(DBSetting db)
        {
            try
            {
                return db.GetDataTable(
                    "SELECT SerialNumber, ItemCode FROM [dbo].[ItemSerialNo] ORDER BY ItemCode, SerialNumber", false);
            }
            catch { return new DataTable(); }
        }

        internal static DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit MakeSerialRepo(DataTable serialLookup)
        {
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repo =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            repo.DataSource = serialLookup;
            repo.ValueMember = "SerialNumber";
            repo.DisplayMember = "SerialNumber";
            repo.NullText = "";
            repo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;   // free-typing still allowed
            return repo;
        }

        // After an Item Code is picked, fill Description / UOM / Tax from the item master.
        internal static void FillFromItem(DataRow row, DataTable itemLookup)
        {
            if (row == null || itemLookup == null) return;
            string c = row["ItemCode"] == DBNull.Value ? "" : Convert.ToString(row["ItemCode"]).Trim();
            if (c.Length == 0) return;
            DataRow[] f = itemLookup.Select("ItemCode='" + c.Replace("'", "''") + "'");
            if (f.Length == 0) return;
            row["Description"] = f[0]["Description"];
            row["UOM"] = f[0]["UOM"];
            row["TaxType"] = f[0]["TaxType"];
        }

        // Shared spare-parts column layout — used by BOTH the contract editor and the service item
        // editor so the two grids match exactly.
        internal static void ConfigureSpareView(DevExpress.XtraGrid.Views.Grid.GridView v,
            DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit check,
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit itemRepo,
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit serialRepo = null)
        {
            v.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            v.OptionsBehavior.Editable = true;
            v.OptionsView.ShowGroupPanel = false;
            v.Columns.Clear();
            v.PopulateColumns();
            SpHide(v, "SparePartKey"); SpHide(v, "ItemKey"); SpHide(v, "Bound"); SpHide(v, "Pos");
            SpCol2(v, "No", "No", 40, 0, true);
            SpCol2(v, "ItemCode", "Item Code", 130, 1);
            if (itemRepo != null && v.Columns["ItemCode"] != null) v.Columns["ItemCode"].ColumnEdit = itemRepo;
            SpCol2(v, "Description", "Description", 220, 2);
            // Serial No has NO ColumnEdit: cells display their raw value (a filtered lookup as ColumnEdit
            // would blank out every serial that isn't in the current filter). The searchable, per-row
            // filtered picker is supplied at EDIT time via CustomRowCellEditForEditing in each form.
            SpCol2(v, "SerialNumber", "Serial No", 120, 3);
            SpBool2(v, check, "Unlimited", "Unlimited", 70, 4);
            SpCol2(v, "UOM", "UOM", 70, 5);
            SpCol2(v, "Quantity", "Quantity", 80, 6);
            SpCol2(v, "Discount", "Discount", 80, 7);
            SpCol2(v, "UnitPrice", "Unit Price", 90, 8);
            SpCol2(v, "Amount", "Amount", 90, 9, true);
            SpCol2(v, "TaxType", "Tax Type", 80, 10);
            SpBool2(v, check, "TaxInclusive", "Tax Inclusive", 90, 11);
            SpCol2(v, "TaxRate", "Tax (%)", 70, 12);
            SpCol2(v, "TaxAmount", "Tax Amount", 90, 13, true);
            SpCol2(v, "AmountAfterTax", "Amount After Tax", 110, 14, true);
        }

        private static void SpHide(DevExpress.XtraGrid.Views.Grid.GridView v, string field)
        { DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field]; if (c != null) c.Visible = false; }

        private static void SpCol2(DevExpress.XtraGrid.Views.Grid.GridView v, string field, string caption, int width, int visibleIndex, bool readOnly = false)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            if (caption != null) c.Caption = caption;
            if (width > 0) c.Width = width;
            if (visibleIndex >= 0) { c.Visible = true; c.VisibleIndex = visibleIndex; }
            if (readOnly) { c.OptionsColumn.AllowEdit = false; c.OptionsColumn.ReadOnly = true; }
        }

        private static void SpBool2(DevExpress.XtraGrid.Views.Grid.GridView v, DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit check, string field, string caption, int width, int visibleIndex)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
            c.ColumnEdit = check;
        }

        // Item-bound lines are read-only on the contract (they belong to a service item); block editing.
        private void SpareParts_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            object bound = GridViewSpareParts.GetRowCellValue(rh, "Bound");
            if (bound != null && bound != DBNull.Value && Convert.ToBoolean(bound)) e.Cancel = true;
        }

        // Serial No edit-time picker: the dropdown lists only the row's Item Code serials (empty until
        // one is picked). Display stays raw text, so other rows' serials are never blanked by the filter.
        private void SpareParts_SerialEditor(object sender, DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "SerialNumber") return;
            string code = Convert.ToString(GridViewSpareParts.GetRowCellValue(e.RowHandle, "ItemCode") ?? "").Trim();
            DataView dv = new DataView(_spSerialLookup);
            dv.RowFilter = code.Length == 0 ? "1=0" : "ItemCode='" + code.Replace("'", "''") + "'";
            _spSerialRepo.DataSource = dv;
            e.RepositoryItem = _spSerialRepo;
        }

        private void SpareParts_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column.FieldName == "Amount" || e.Column.FieldName == "TaxAmount" ||
                e.Column.FieldName == "AmountAfterTax" || e.Column.FieldName == "No") return;
            GridViewSpareParts.PostEditor();
            DataRowView drv = GridViewSpareParts.GetRow(e.RowHandle) as DataRowView;
            if (drv != null)
            {
                if (e.Column.FieldName == "ItemCode") FillFromItem(drv.Row, _spItemLookup);   // auto-fill Description/UOM/Tax
                ComputeSpareRow(drv.Row);
            }
            GridViewSpareParts.RefreshData();
        }

        internal static void ComputeSpareRow(DataRow r)
        {
            decimal qty = r["Quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Quantity"]);
            decimal price = r["UnitPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(r["UnitPrice"]);
            decimal gross = qty * price;
            // Discount: "10%" -> percent of gross; a plain number -> absolute amount off.
            string disc = r["Discount"] == DBNull.Value ? "" : Convert.ToString(r["Discount"]).Trim();
            decimal discAmt = 0m;
            if (disc.EndsWith("%"))
            {
                decimal p; if (decimal.TryParse(disc.TrimEnd('%').Trim(), out p)) discAmt = gross * p / 100m;
            }
            else { decimal a; if (decimal.TryParse(disc, out a)) discAmt = a; }
            decimal amount = gross - discAmt; if (amount < 0) amount = 0;
            decimal rate = r["TaxRate"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TaxRate"]);
            bool inclusive = r["TaxInclusive"] != DBNull.Value && Convert.ToBoolean(r["TaxInclusive"]);
            decimal taxAmt, afterTax;
            if (inclusive) { afterTax = amount; taxAmt = rate == 0 ? 0 : amount - (amount / (1 + rate / 100m)); }
            else { taxAmt = amount * rate / 100m; afterTax = amount + taxAmt; }
            r["Amount"] = decimal.Round(amount, 2);
            r["TaxAmount"] = decimal.Round(taxAmt, 2);
            r["AmountAfterTax"] = decimal.Round(afterTax, 2);
        }

        private void LoadSpareParts()
        {
            if (_spareParts == null) SetupSparePartsGrid();
            _spareParts.Rows.Clear();
            if (_isNew || _contractKey == 0) { RenumberSpareParts(); return; }
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT SparePartKey, ItemKey, ItemCode, Description, ISNULL(SerialNumber,'') AS SerialNumber, " +
                    "Unlimited, UOM, Quantity, Discount, " +
                    "UnitPrice, TaxType, TaxInclusive, TaxRate, Pos FROM [dbo].[zSCP2_ContractSparePart] " +
                    "WHERE ContractKey=" + _contractKey + " ORDER BY Pos", false);
                foreach (DataRow s in dt.Rows)
                {
                    DataRow r = _spareParts.NewRow();
                    r["SparePartKey"] = s["SparePartKey"];
                    r["ItemKey"] = s["ItemKey"];
                    r["Bound"] = s["ItemKey"] != DBNull.Value;
                    r["ItemCode"] = s["ItemCode"]; r["Description"] = s["Description"];
                    r["SerialNumber"] = s["SerialNumber"];
                    r["Unlimited"] = Convert.ToString(s["Unlimited"]) == "Y";
                    r["UOM"] = s["UOM"]; r["Quantity"] = s["Quantity"]; r["Discount"] = s["Discount"];
                    r["UnitPrice"] = s["UnitPrice"]; r["TaxType"] = s["TaxType"];
                    r["TaxInclusive"] = Convert.ToString(s["TaxInclusive"]) == "Y";
                    r["TaxRate"] = s["TaxRate"]; r["Pos"] = s["Pos"];
                    ComputeSpareRow(r);
                    _spareParts.Rows.Add(r);
                }
            }
            catch { }
            RenumberSpareParts();
        }

        private void RenumberSpareParts()
        {
            if (_spareParts == null) return;
            System.Data.DataView v = _spareParts.DefaultView;
            for (int i = 0; i < v.Count; i++) { v[i].Row["No"] = i + 1; v[i].Row["Pos"] = i; }
        }

        private void BtnSpInsert_Click(object sender, EventArgs e)
        {
            if (_spareParts == null) return;
            GridViewSpareParts.PostEditor();
            DataRow r = _spareParts.NewRow();
            r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
            r["ItemCode"] = ""; r["Description"] = ""; r["Unlimited"] = false; r["UOM"] = "";
            r["Quantity"] = 0m; r["Discount"] = ""; r["UnitPrice"] = 0m; r["Amount"] = 0m;
            r["TaxType"] = ""; r["TaxInclusive"] = false; r["TaxRate"] = 0m; r["TaxAmount"] = 0m;
            r["AmountAfterTax"] = 0m; r["Pos"] = _spareParts.Rows.Count;
            _spareParts.Rows.Add(r);
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
            GridViewSpareParts.FocusedRowHandle = GridViewSpareParts.RowCount - 1;
        }

        private void BtnSpRemove_Click(object sender, EventArgs e)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            DataRowView drv = GridViewSpareParts.GetRow(rh) as DataRowView;
            if (drv == null) return;
            if (drv.Row["ItemKey"] != DBNull.Value)
            { XtraMessageBox.Show("This spare part belongs to a service item and cannot be removed here. Edit it on the service item.", "Read-only"); return; }
            drv.Row.Delete();
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
        }

        private void BtnSpUp_Click(object sender, EventArgs e) { MoveSparePart(-1); }
        private void BtnSpDown_Click(object sender, EventArgs e) { MoveSparePart(1); }

        // Move Up/Down mirrors AutoCount's detail-grid reorder: swap the Pos of the two adjacent rows,
        // the Pos-sorted view then re-orders, and focus follows the moved row.
        private void MoveSparePart(int dir)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            int target = rh + dir;
            if (target < 0 || target >= GridViewSpareParts.RowCount) return;
            DataRowView a = GridViewSpareParts.GetRow(rh) as DataRowView;
            DataRowView b = GridViewSpareParts.GetRow(target) as DataRowView;
            if (a == null || b == null) return;
            int pa = Convert.ToInt32(a.Row["Pos"]), pb = Convert.ToInt32(b.Row["Pos"]);
            a.Row["Pos"] = pb; b.Row["Pos"] = pa;
            _dirty = true;
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();
            GridViewSpareParts.FocusedRowHandle = target;
        }

        // Persist a service item's own (item-bound) spare parts. These show read-only on the contract.
        internal static void SaveItemSpareParts(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey, long contractKey)
        {
            ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=@ik", P("@ik", itemKey));
            if (d.SpareParts == null) return;
            int pos = 0;
            foreach (DataRow r in d.SpareParts.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["ItemCode"] == DBNull.Value ? "" : Convert.ToString(r["ItemCode"]).Trim();
                if (code.Length == 0 && Convert.ToString(r["Description"]).Trim().Length == 0) continue;
                ExecNonQuery(conn, tx,
                    "INSERT INTO [dbo].[zSCP2_ContractSparePart] " +
                    "(ContractKey, ItemKey, ItemCode, Description, SerialNumber, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                    " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                    "VALUES (@ck, @ik, @code, @desc, @serial, @unl, @uom, @qty, @disc, @price, @ttype, @tinc, @trate, @pos, GETDATE())",
                    P("@ck", contractKey), P("@ik", itemKey), P("@code", code),
                    P("@desc", AsStr(r["Description"])),
                    P("@serial", AsStr(r["SerialNumber"])),
                    P("@unl", Convert.ToBoolean(r["Unlimited"]) ? "Y" : "N"),
                    P("@uom", AsStr(r["UOM"])), P("@qty", AsDec(r["Quantity"])),
                    P("@disc", AsStr(r["Discount"])), P("@price", AsDec(r["UnitPrice"])),
                    P("@ttype", AsStr(r["TaxType"])),
                    P("@tinc", Convert.ToBoolean(r["TaxInclusive"]) ? "Y" : "N"),
                    P("@trate", AsDec(r["TaxRate"])), P("@pos", pos++));
            }
        }

        private void SaveSpareParts(SqlConnection conn, SqlTransaction tx)
        {
            if (_spareParts == null) return;
            GridViewSpareParts.PostEditor();
            // Replace only contract-level lines; item-bound lines are owned by the service item and
            // are left untouched (they were shown read-only).
            ExecNonQuery(conn, tx,
                "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ContractKey=@ck AND ItemKey IS NULL",
                P("@ck", _contractKey));
            foreach (DataRowView drv in _spareParts.DefaultView)
            {
                DataRow r = drv.Row;
                if (r["ItemKey"] != DBNull.Value) continue;   // item-bound: not owned here
                string code = r["ItemCode"] == DBNull.Value ? "" : Convert.ToString(r["ItemCode"]).Trim();
                if (code.Length == 0 && Convert.ToString(r["Description"]).Trim().Length == 0) continue;
                ExecNonQuery(conn, tx,
                    "INSERT INTO [dbo].[zSCP2_ContractSparePart] " +
                    "(ContractKey, ItemKey, ItemCode, Description, SerialNumber, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                    " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                    "VALUES (@ck, NULL, @code, @desc, @serial, @unl, @uom, @qty, @disc, @price, @ttype, @tinc, @trate, @pos, GETDATE())",
                    P("@ck", _contractKey), P("@code", code),
                    P("@desc", AsStr(r["Description"])),
                    P("@serial", AsStr(r["SerialNumber"])),
                    P("@unl", Convert.ToBoolean(r["Unlimited"]) ? "Y" : "N"),
                    P("@uom", AsStr(r["UOM"])), P("@qty", AsDec(r["Quantity"])),
                    P("@disc", AsStr(r["Discount"])), P("@price", AsDec(r["UnitPrice"])),
                    P("@ttype", AsStr(r["TaxType"])),
                    P("@tinc", Convert.ToBoolean(r["TaxInclusive"]) ? "Y" : "N"),
                    P("@trate", AsDec(r["TaxRate"])), P("@pos", Convert.ToInt32(r["Pos"])));
            }
        }

        // ===================== More Header tab =====================
        // Built in code (many simple fields) into the designer's empty PageMoreHeader shell. Each edit
        // is keyed by its zSCP2_Contract column name so load/save is a simple loop.

        private readonly System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit> _mh
            = new System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit>();
        private DevExpress.XtraEditors.MemoEdit _mhDelAddress;

        // More Header fields live in the DESIGNER now (user request 01/08): this only registers
        // the designer editors into the _mh map (load/save read it) and wires dirty tracking.
        private void BuildMoreHeaderTab()
        {
            RegMh("City", TxtMhCity);
            RegMh("PostalCode", TxtMhPostalCode);
            RegMh("State", TxtMhState);
            RegMh("Country", TxtMhCountry);
            RegMh("Fax", TxtMhFax);
            RegMh("Ref1", TxtMhRef1);
            RegMh("Ref2", TxtMhRef2);
            RegMh("Ref3", TxtMhRef3);
            RegMh("Ref4", TxtMhRef4);
            RegMh("DelBranchCode", TxtMhDelBranchCode);
            RegMh("DelState", TxtMhDelState);
            RegMh("DelBranchName", TxtMhDelBranchName);
            RegMh("DelCountry", TxtMhDelCountry);
            RegMh("DelPhone", TxtMhDelPhone);
            RegMh("DelFax", TxtMhDelFax);
            RegMh("DelEmail", TxtMhDelEmail);
            RegMh("DelContactPerson", TxtMhDelContactPerson);
            RegMh("DelCity", TxtMhDelCity);
            RegMh("DelPostalCode", TxtMhDelPostalCode);
            _mhDelAddress = TxtMhDelAddress;
            _mhDelAddress.EditValueChanged += delegate { if (!_loading) _dirty = true; };
        }

        private void RegMh(string col, DevExpress.XtraEditors.TextEdit ed)
        {
            ed.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            _mh[col] = ed;
        }

        private void LoadMoreHeader()
        {
            if (_isNew || _contractKey == 0) return;
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT City, PostalCode, State, Country, Fax, Ref1, Ref2, Ref3, Ref4, " +
                    "DelBranchCode, DelBranchName, DelAddress, DelCity, DelPostalCode, DelState, " +
                    "DelCountry, DelPhone, DelFax, DelEmail, DelContactPerson " +
                    "FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + _contractKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _mh)
                    if (dt.Columns.Contains(kv.Key)) kv.Value.Text = AsStr(r[kv.Key]);
                if (_mhDelAddress != null) _mhDelAddress.Text = AsStr(r["DelAddress"]);
            }
            catch { }
        }

        private void SaveMoreHeader(SqlConnection conn, SqlTransaction tx)
        {
            ExecNonQuery(conn, tx,
                "UPDATE [dbo].[zSCP2_Contract] SET City=@City, PostalCode=@PostalCode, State=@State, " +
                "Country=@Country, Fax=@Fax, Ref1=@Ref1, Ref2=@Ref2, Ref3=@Ref3, Ref4=@Ref4, " +
                "DelBranchCode=@DelBranchCode, DelBranchName=@DelBranchName, DelAddress=@DelAddress, " +
                "DelCity=@DelCity, DelPostalCode=@DelPostalCode, DelState=@DelState, DelCountry=@DelCountry, " +
                "DelPhone=@DelPhone, DelFax=@DelFax, DelEmail=@DelEmail, DelContactPerson=@DelContactPerson " +
                "WHERE ContractKey=@ck",
                P("@City", MhVal("City")), P("@PostalCode", MhVal("PostalCode")), P("@State", MhVal("State")),
                P("@Country", MhVal("Country")), P("@Fax", MhVal("Fax")), P("@Ref1", MhVal("Ref1")),
                P("@Ref2", MhVal("Ref2")), P("@Ref3", MhVal("Ref3")), P("@Ref4", MhVal("Ref4")),
                P("@DelBranchCode", MhVal("DelBranchCode")), P("@DelBranchName", MhVal("DelBranchName")),
                P("@DelAddress", _mhDelAddress == null ? "" : _mhDelAddress.Text.Trim()),
                P("@DelCity", MhVal("DelCity")), P("@DelPostalCode", MhVal("DelPostalCode")),
                P("@DelState", MhVal("DelState")), P("@DelCountry", MhVal("DelCountry")),
                P("@DelPhone", MhVal("DelPhone")), P("@DelFax", MhVal("DelFax")),
                P("@DelEmail", MhVal("DelEmail")), P("@DelContactPerson", MhVal("DelContactPerson")),
                P("@ck", _contractKey));
        }

        private string MhVal(string col)
        {
            DevExpress.XtraEditors.TextEdit ed;
            return _mh.TryGetValue(col, out ed) ? (ed.Text ?? "").Trim() : "";
        }

        // ═══ Group Deal tab: ONE invisible "group machine" per contract (engine-driven .C) ═══
        // The group item's meters charge against the WHOLE fleet at Generate — group MIN tops up
        // to the SUM of every machine's scoped BK/CL charges, group WAIVE fires on the fleet
        // total, group RENTAL bills one rent. Edited IN PLACE on this tab (same experience as the
        // machine Meter Configuration panel — no machine dialog); hidden from the machine grid +
        // Maintain Service Item list. EDIT mode only (the contract must exist first).
        private DevExpress.XtraTab.XtraTabPage _pgGroup;
        private DevExpress.XtraGrid.GridControl _gridGroup;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewGroup;
        private DevExpress.XtraEditors.SimpleButton _btnGroupAdd;
        private DevExpress.XtraEditors.SimpleButton _btnGroupDel;
        private DevExpress.XtraEditors.LabelControl _lblGroupHint;
        private DevExpress.XtraEditors.LabelControl _lblGroupEmpty;
        private DataTable _groupEmptySchema;

        private ItemEditData FindGroupItem()
        {
            foreach (ItemEditData d in _items) if (d.IsGroupItem) return d;
            return null;
        }

        private ItemEditData EnsureGroupItem()
        {
            ItemEditData g = FindGroupItem();
            if (g != null) return g;
            g = new ItemEditData();
            g.IsGroupItem = true;
            g.Meters = zSCP2_Item_Form.CreateMetersTable();
            g.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            // Readable synthetic identity — unique via the contract no; never shown as a machine.
            g.ServiceItemNo = "GRP-" + TxtContractNo.Text.Trim();
            g.SerialNumber = "GROUP";
            g.Description = "GROUP DEAL (whole contract)";
            ApplyContractDateDefaults(g);
            _items.Add(g);   // appended LAST — machine grid numbering stays stable
            return g;
        }

        private void BuildGroupTab()
        {
            _pgGroup = new DevExpress.XtraTab.XtraTabPage();
            _pgGroup.Text = "Group Deal";
            // COMING SOON (user decision 2026-07-28): group/fleet-wide billing terms are not part
            // of the current release — the docs present them as "coming soon", so the tab must not
            // show either. Everything underneath stays wired; un-hide by removing this one line.
            _pgGroup.PageVisible = false;
            _groupEmptySchema = zSCP2_Item_Form.CreateMetersTable();

            System.Windows.Forms.Panel bar = new System.Windows.Forms.Panel();
            bar.Dock = System.Windows.Forms.DockStyle.Top;
            bar.Height = 30;
            _btnGroupAdd = new DevExpress.XtraEditors.SimpleButton();
            _btnGroupAdd.Text = "+";
            _btnGroupAdd.Location = new System.Drawing.Point(4, 3);
            _btnGroupAdd.Size = new System.Drawing.Size(28, 24);
            _btnGroupAdd.Click += new EventHandler(BtnGroupAdd_Click);
            bar.Controls.Add(_btnGroupAdd);
            _btnGroupDel = new DevExpress.XtraEditors.SimpleButton();
            _btnGroupDel.Text = "-";
            _btnGroupDel.Location = new System.Drawing.Point(36, 3);
            _btnGroupDel.Size = new System.Drawing.Size(28, 24);
            _btnGroupDel.Click += new EventHandler(BtnGroupDel_Click);
            bar.Controls.Add(_btnGroupDel);
            _lblGroupHint = new DevExpress.XtraEditors.LabelControl();
            _lblGroupHint.Text = "Group meters charge against the WHOLE contract at Generate: group MIN tops up to the fleet's total BK/CL charges · group WAIVE fires on the fleet total · group RENTAL bills one rent for all machines.";
            _lblGroupHint.Location = new System.Drawing.Point(72, 8);
            bar.Controls.Add(_lblGroupHint);

            _gridGroup = new DevExpress.XtraGrid.GridControl();
            _viewGroup = new DevExpress.XtraGrid.Views.Grid.GridView();
            _gridGroup.MainView = _viewGroup;
            _gridGroup.ViewCollection.Add(_viewGroup);
            _gridGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            _viewGroup.OptionsView.ShowGroupPanel = false;
            _viewGroup.OptionsBehavior.AutoPopulateColumns = false;

            // Same editors as the machine meter panel — own instances (repositories are per-grid).
            DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit repoTypeG =
                new DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit();
            DevExpress.XtraGrid.Views.Grid.GridView popupG = new DevExpress.XtraGrid.Views.Grid.GridView();
            popupG.OptionsView.ShowAutoFilterRow = true;
            popupG.OptionsBehavior.AutoPopulateColumns = false;
            repoTypeG.PopupView = popupG;
            DevExpress.XtraGrid.Columns.GridColumn pgCode = popupG.Columns.AddVisible("MeterTypeCode");
            pgCode.Caption = "Meter Type"; pgCode.Width = 120;
            DevExpress.XtraGrid.Columns.GridColumn pgDesc = popupG.Columns.AddVisible("Description");
            pgDesc.Caption = "Description"; pgDesc.Width = 260;
            repoTypeG.DataSource = _meterCfgLookup;
            repoTypeG.DisplayMember = "MeterTypeCode";
            repoTypeG.ValueMember = "MeterTypeCode";
            repoTypeG.NullText = "";
            _gridGroup.RepositoryItems.Add(repoTypeG);

            DevExpress.XtraEditors.Repository.RepositoryItemComboBox repoRoleG =
                new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repoRoleG.Items.AddRange(new object[] { "BK", "CL", "RENTAL", "WAIVE", "COMMIT", "NA" });
            repoRoleG.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _gridGroup.RepositoryItems.Add(repoRoleG);

            DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit repoBtnG =
                new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            repoBtnG.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
            repoBtnG.Buttons.Clear();
            DevExpress.XtraEditors.Controls.EditorButton gBtn =
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph);
            try
            {
                System.Drawing.Image gImg =
                    DevExpress.Images.ImageResourceCache.Default.GetImage("images/business objects/bo_price_16x16.png");
                if (gImg != null) gBtn.ImageOptions.Image = gImg;
                else gBtn.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis;
            }
            catch { gBtn.Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis; }
            gBtn.ToolTip = "Configure this group meter (Multi-Price ladder / Waive conditions / MIN scope)";
            repoBtnG.Buttons.Add(gBtn);
            repoBtnG.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(GroupRepoBtn_ButtonClick);
            _gridGroup.RepositoryItems.Add(repoBtnG);

            GroupCol("MeterTypeCode", "Meter Type", 170, repoTypeG);
            GroupCol("Description", "Meter Description", 280, null);
            GroupCol("MeterRole", "Role", 80, repoRoleG);
            GroupCol("MinimumCharges", "Min Charges", 100, null);
            GroupCol("ChargesRate", "Unit Price", 90, null);
            DevExpress.XtraGrid.Columns.GridColumn gMulti = GroupCol("MeterMultiPriceCode", "Multi-Price / Deal", 150, null);
            gMulti.OptionsColumn.AllowEdit = false;
            DevExpress.XtraGrid.Columns.GridColumn gPick = _viewGroup.Columns.AddVisible("GroupPickBtn");
            gPick.Caption = " ";
            gPick.UnboundDataType = typeof(string);
            gPick.ColumnEdit = repoBtnG;
            gPick.Width = 28;
            gPick.OptionsColumn.ShowCaption = false;
            gPick.OptionsColumn.AllowSize = false;
            gPick.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
            GroupCol("RebateQtyInPercent", "Rebate %", 80, null);
            GroupCol("FOCQty", "Free Qty", 80, null);
            _viewGroup.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GroupView_CellValueChanged);
            _viewGroup.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GroupView_RowCellStyle);
            _viewGroup.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(GroupView_CustomColumnDisplayText);

            _lblGroupEmpty = new DevExpress.XtraEditors.LabelControl();
            _lblGroupEmpty.Appearance.ForeColor = System.Drawing.Color.Gray;
            _lblGroupEmpty.Appearance.Options.UseForeColor = true;
            _lblGroupEmpty.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            _lblGroupEmpty.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            _lblGroupEmpty.Size = new System.Drawing.Size(640, 70);
            _lblGroupEmpty.Location = new System.Drawing.Point(40, 70);

            _pgGroup.Controls.Add(_lblGroupEmpty);
            _pgGroup.Controls.Add(_gridGroup);
            _pgGroup.Controls.Add(bar);
            _lblGroupEmpty.BringToFront();   // floats over the docked grid
            TabMain.TabPages.Insert(1, _pgGroup);   // right after "Service Item Under Contract"
            RefreshGroupTab();
        }

        private DevExpress.XtraGrid.Columns.GridColumn GroupCol(string field, string caption, int width,
            DevExpress.XtraEditors.Repository.RepositoryItem edit)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = _viewGroup.Columns.AddVisible(field);
            c.Caption = caption;
            c.Width = width;
            if (edit != null) c.ColumnEdit = edit;
            return c;
        }

        private void RefreshGroupTab()
        {
            if (_gridGroup == null) return;
            ItemEditData g = FindGroupItem();
            DataTable src = g != null && g.Meters != null ? g.Meters : _groupEmptySchema;
            if (!object.ReferenceEquals(_gridGroup.DataSource, src)) _gridGroup.DataSource = src;
            int liveRows = 0;
            foreach (DataRow mr in src.Rows) if (mr.RowState != DataRowState.Deleted) liveRows++;
            if (_lblGroupEmpty != null)
            {
                _lblGroupEmpty.Text = _isNew
                    ? "Save the contract first — the Group Deal is configured in EDIT mode."
                    : "No group meters yet.\r\n\r\nClick \"+\" to build the fleet deal — e.g. one GROUP RENTAL (one rent for all machines),\r\na GROUP WAIVE with its firing condition, or a GROUP MIN that tops the fleet up to a committed amount.";
                _lblGroupEmpty.Visible = liveRows == 0;
            }
            if (_btnGroupAdd != null) _btnGroupAdd.Enabled = !_isNew;
            if (_btnGroupDel != null) _btnGroupDel.Enabled = !_isNew;
        }

        private void BtnGroupAdd_Click(object sender, EventArgs e)
        {
            if (_isNew)
            { XtraMessageBox.Show("Save the contract first — the Group Deal is configured in EDIT mode.", "Group Deal"); return; }
            ItemEditData g = EnsureGroupItem();
            if (!object.ReferenceEquals(_gridGroup.DataSource, g.Meters)) _gridGroup.DataSource = g.Meters;
            DataRow r = g.Meters.NewRow();
            r["MeterRole"] = "";   // NO default — the user must pick (save blocks an empty role)
            r["Description"] = "";
            r["MachineSerialNo"] = "";
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            r["CustomTiers"] = "";
            r["WaiveFirstNMonths"] = 0; r["WaiveTargetAmount"] = 0m; r["WaivePartialThreshold"] = 0m; r["WaivePartialAmount"] = 0m; r["WaiveScope"] = "BKCL";
            g.Meters.Rows.Add(r);
            _viewGroup.FocusedRowHandle = _viewGroup.RowCount - 1;
            _dirty = true;
            RefreshGroupTab();
        }

        private void BtnGroupDel_Click(object sender, EventArgs e)
        {
            int rh = _viewGroup.FocusedRowHandle;
            ItemEditData g = FindGroupItem();
            if (rh < 0 || g == null) return;
            string code = Convert.ToString(_viewGroup.GetRowCellValue(rh, "MeterTypeCode"));
            if (code.Trim().Length > 0 && !IsWaiveType(code)
                && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(code)
                && DeleteWouldOrphanWaive(g, _viewGroup.GetDataRow(rh)))
            {
                XtraMessageBox.Show("The group still has a RENTAL WAIVE meter — remove the waive line first " +
                    "(or keep a rental meter). A waive needs a rent to waive.", "Group Deal");
                return;
            }
            if (g.ItemKey > 0 && !string.IsNullOrEmpty(code) &&
                XtraMessageBox.Show("Remove group meter '" + code + "'?\r\n\r\nWhen the contract is saved, this " +
                    "meter AND its billing history are deleted permanently.", "Group Deal",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            _viewGroup.DeleteRow(rh);
            _dirty = true;
            RefreshGroupTab();
        }

        // Mirrors the machine panel's type-pick behaviour on the GROUP grid: waive-needs-rental
        // invariant, description/role refresh from the new type, negative waive amounts.
        private void GroupView_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column != null && e.Column.FieldName == "MinimumCharges")
            {
                try
                {
                    string rowType = Convert.ToString(_viewGroup.GetRowCellValue(e.RowHandle, "MeterTypeCode")).Trim();
                    decimal mv = e.Value == null || e.Value == DBNull.Value ? 0m : Convert.ToDecimal(e.Value);
                    if (mv > 0m && IsWaiveType(rowType))
                        _viewGroup.SetRowCellValue(e.RowHandle, "MinimumCharges", -mv);
                }
                catch { }
            }
            if (e.Column == null || e.Column.FieldName != "MeterTypeCode" || _meterCfgLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _meterCfgLookup.Select("MeterTypeCode='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow m = found[0];
            int rh = e.RowHandle;
            if (WaiveRuleBroken(FindGroupItem()))
            {
                XtraMessageBox.Show("A RENTAL WAIVE meter needs a RENTAL meter in the group.\r\n" +
                    "Add the group rent line first (or remove the waive line).", "Group Deal");
                DataRow gr = _viewGroup.GetDataRow(rh);
                if (gr != null && gr.RowState == DataRowState.Added) _viewGroup.DeleteRow(rh);
                else if (gr != null)
                {
                    gr["MeterTypeCode"] = gr["MeterTypeCode", DataRowVersion.Original];
                    gr["Description"] = gr["Description", DataRowVersion.Original];
                }
                _viewGroup.RefreshData();
                return;
            }
            object curDesc = _viewGroup.GetRowCellValue(rh, "Description");
            string curDescS = curDesc == null || curDesc == DBNull.Value ? "" : curDesc.ToString().Trim();
            if (curDescS.Length == 0 || IsTypeDefaultDescription(curDescS))
                _viewGroup.SetRowCellValue(rh, "Description", m.Table.Columns.Contains("Description") ? m["Description"] : "");
            if (IsWaiveType(code))
                _viewGroup.SetRowCellValue(rh, "MinimumCharges",
                    -Math.Abs(m["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(m["MinimumCharges"])));
            else
                _viewGroup.SetRowCellValue(rh, "MinimumCharges", m["MinimumCharges"]);
            _viewGroup.SetRowCellValue(rh, "ChargesRate", m["ChargesRate"]);
            _viewGroup.SetRowCellValue(rh, "MeterMultiPriceCode", m["MeterMultiPriceCode"]);
            _viewGroup.SetRowCellValue(rh, "RebateQtyInPercent", m["RebateQtyInPercent"]);
            _viewGroup.SetRowCellValue(rh, "FOCQty", m["FOCQty"]);
            object curRole = _viewGroup.GetRowCellValue(rh, "MeterRole");
            string curRoleS = curRole == null ? "" : curRole.ToString().Trim().ToUpperInvariant();
            if (curRoleS == "" || curRoleS == "NA" || curRoleS == "RENTAL" || curRoleS == "WAIVE" || curRoleS == "COMMIT")
            {
                string defRole = m.Table.Columns.Contains("DefaultRole") ? Convert.ToString(m["DefaultRole"]).Trim().ToUpperInvariant() : "";
                string inferred = defRole.Length > 0 ? defRole
                    : InferMeterRole(code, m.Table.Columns.Contains("Description") ? Convert.ToString(m["Description"]) : "");
                if (inferred.Length > 0) _viewGroup.SetRowCellValue(rh, "MeterRole", inferred);
            }
        }

        // Mirrors the machine panel's "…" button on the GROUP grid: WAIVE config / MIN scope /
        // Multi-Price ladder (usage meters) / silent no-op on plain rentals.
        private void GroupRepoBtn_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            _viewGroup.CloseEditor();
            DataRow r = _viewGroup.GetFocusedDataRow();
            if (r == null) return;
            string mtType = r["MeterTypeCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterTypeCode"]).Trim();
            if (IsWaiveType(mtType))
            {
                int wn0 = r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0;
                decimal wt0 = r.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(r["WaiveTargetAmount"]) : 0m;
                decimal wpt0 = r.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(r["WaivePartialThreshold"]) : 0m;
                decimal wpa0 = r.Table.Columns.Contains("WaivePartialAmount") ? AsDec(r["WaivePartialAmount"]) : 0m;
                string ws0 = r.Table.Columns.Contains("WaiveScope") ? AsStr(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form wdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form(mtType, wn0, wt0, wpt0, wpa0, ws0))
                {
                    if (wdlg.ShowDialog(this) != DialogResult.OK) return;
                    r["WaiveFirstNMonths"] = wdlg.FirstNMonths;
                    r["WaiveTargetAmount"] = wdlg.TargetAmount;
                    r["WaivePartialThreshold"] = wdlg.PartialThreshold;
                    r["WaivePartialAmount"] = wdlg.PartialAmount;
                    r["WaiveScope"] = wdlg.Scope;
                    _dirty = true;
                    _viewGroup.RefreshData();
                }
                return;
            }
            if (IsCommitType(mtType))
            {
                decimal camt = Math.Abs(r["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(r["MinimumCharges"]));
                string cs0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value ? Convert.ToString(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form cdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form(mtType, camt, cs0))
                {
                    if (cdlg.ShowDialog(this) != DialogResult.OK) return;
                    r["WaiveScope"] = cdlg.Scope;
                    _dirty = true;
                    _viewGroup.RefreshData();
                }
                return;
            }
            if (IsFlatType(mtType)) return;   // plain rental: nothing to configure
            string cur = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string curCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            decimal foc = r["FOCQty"] == DBNull.Value ? 0m : Convert.ToDecimal(r["FOCQty"]);
            using (ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form dlg =
                new ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form(_db, cur, curCsv, foc))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                r["MeterMultiPriceCode"] = dlg.SelectedCode;
                r["CustomTiers"] = dlg.CustomCsv;
                _dirty = true;
                _ladderFocCache.Clear();
                _viewGroup.RefreshData();
            }
        }

        private void GroupView_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column != null && (e.Column.FieldName == "ChargesRate" || e.Column.FieldName == "FOCQty"))
            {
                string mp = Convert.ToString(_viewGroup.GetRowCellValue(e.RowHandle, "MeterMultiPriceCode"));
                string ct = Convert.ToString(_viewGroup.GetRowCellValue(e.RowHandle, "CustomTiers"));
                if (!string.IsNullOrEmpty(mp) || !string.IsNullOrEmpty(ct))
                {
                    e.Appearance.BackColor = System.Drawing.Color.Gainsboro;
                    e.Appearance.ForeColor = System.Drawing.Color.Gray;
                    e.Appearance.Options.UseBackColor = true;
                    e.Appearance.Options.UseForeColor = true;
                    return;
                }
            }
            string type = Convert.ToString(_viewGroup.GetRowCellValue(e.RowHandle, "MeterTypeCode"));
            if (!IsFlatType(type)) return;
            e.Appearance.BackColor = _rentalRowColor;
            e.Appearance.ForeColor = System.Drawing.Color.Black;
            e.Appearance.Options.UseBackColor = true;
            e.Appearance.Options.UseForeColor = true;
        }

        // Multi-Price / Deal cell on the GROUP grid: waive summary, MIN scope, or the ladder state.
        private void GroupView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.ListSourceRowIndex < 0 || e.Column.FieldName != "MeterMultiPriceCode") return;
            System.Data.DataView dv = _gridGroup.DataSource as System.Data.DataView;
            DataTable src = dv != null ? dv.Table : _gridGroup.DataSource as DataTable;
            if (src == null || e.ListSourceRowIndex >= src.DefaultView.Count) return;
            DataRow r = src.DefaultView[e.ListSourceRowIndex].Row;
            string mtType0 = Convert.ToString(r["MeterTypeCode"]);
            if (IsWaiveType(mtType0))
            {
                int wn0 = r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0;
                decimal wt0 = r.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(r["WaiveTargetAmount"]) : 0m;
                decimal wpt0 = r.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(r["WaivePartialThreshold"]) : 0m;
                decimal wpa0 = r.Table.Columns.Contains("WaivePartialAmount") ? AsDec(r["WaivePartialAmount"]) : 0m;
                e.DisplayText = ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form.Summary(wn0, wt0, wpt0, wpa0);
                return;
            }
            if (IsCommitType(mtType0))
            {
                string sc0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value
                    ? Convert.ToString(r["WaiveScope"]).Trim().ToUpperInvariant() : "BKCL";
                e.DisplayText = "MIN: counts " + (sc0 == "BK" ? "BK only" : (sc0 == "CL" ? "CL only" : "BK + CL"));
                return;
            }
            string code = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string custom = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            if (custom.Length > 0) e.DisplayText = code.Length == 0 ? "(Custom)" : code + " (Modified)";
            else e.DisplayText = code;
        }

        // ===================== Strategy tab (this contract's OWN strategy rules) =====================
        // The contract's editable COPY of strategy rules (zSCP2_ContractStrategyRule). Seeded from a master
        // template by "Copy Rules from Strategy Template", then edited here WITHOUT touching the template.
        // The billing pipeline reads these rows LIVE at Generate (the push step is retired). 2nd tab.
        private DevExpress.XtraTab.XtraTabPage _pgStrategy;
        private ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl _strategyEditor;
        private DevExpress.XtraEditors.SimpleButton _btnCopyTemplate;
        private DevExpress.XtraEditors.LabelControl _lblStrategyTabHint;
        private DevExpress.XtraEditors.LabelControl _lblFocReset;
        private DevExpress.XtraEditors.ComboBoxEdit _cmbFocReset;   // FOC reset period: Monthly / Weekly / Every N days
        private DevExpress.XtraEditors.SpinEdit _spnFocResetN;      // the N for "Every N days"

        private void BuildStrategyTab()
        {
            _pgStrategy = new DevExpress.XtraTab.XtraTabPage();
            _pgStrategy.Text = "Strategy";

            _strategyEditor = new ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl();
            _strategyEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            _strategyEditor.RulesChanged += delegate { if (!_loading) _dirty = true; };

            DevExpress.XtraEditors.PanelControl top = new DevExpress.XtraEditors.PanelControl();
            top.Dock = System.Windows.Forms.DockStyle.Top;
            top.Height = 74;

            // (The strategy template picker was removed with the user's 01/08 header redesign -
            // the contract's StrategyCode still loads/saves via _loadedStrategyCode.)

            _btnCopyTemplate = new DevExpress.XtraEditors.SimpleButton();
            _btnCopyTemplate.Text = "Copy Rules from Template";
            _btnCopyTemplate.Location = new System.Drawing.Point(325, 8);
            _btnCopyTemplate.Width = 190; _btnCopyTemplate.Height = 26;
            _btnCopyTemplate.Click += new EventHandler(BtnCopyTemplate_Click);
            top.Controls.Add(_btnCopyTemplate);

            // NOTE: no "Apply" button anywhere — the push model is retired; saved rules act LIVE at
            // every Generate run (see the retirement banner near ExtendContractRibbon).

            // FOC Reset period (contract-level): how often the FOC/rebate free allowance refreshes.
            _lblFocReset = new DevExpress.XtraEditors.LabelControl();
            _lblFocReset.Text = "FOC Reset";
            _lblFocReset.Location = new System.Drawing.Point(10, 49);
            top.Controls.Add(_lblFocReset);
            _cmbFocReset = new DevExpress.XtraEditors.ComboBoxEdit();
            _cmbFocReset.Location = new System.Drawing.Point(78, 45);
            _cmbFocReset.Width = 130;
            _cmbFocReset.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _cmbFocReset.Properties.Items.AddRange(new object[] { "Monthly", "Weekly", "Every N days" });
            _cmbFocReset.SelectedIndex = 0;   // default Monthly
            _cmbFocReset.SelectedIndexChanged += new EventHandler(CmbFocReset_Changed);
            top.Controls.Add(_cmbFocReset);
            _spnFocResetN = new DevExpress.XtraEditors.SpinEdit();
            _spnFocResetN.Location = new System.Drawing.Point(214, 45);
            _spnFocResetN.Width = 55;
            _spnFocResetN.Properties.IsFloatValue = false;
            _spnFocResetN.Properties.MinValue = 1m; _spnFocResetN.Properties.MaxValue = 365m;
            _spnFocResetN.EditValue = 3;
            _spnFocResetN.Visible = false;
            _spnFocResetN.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            top.Controls.Add(_spnFocResetN);

            _lblStrategyTabHint = new DevExpress.XtraEditors.LabelControl();
            _lblStrategyTabHint.Text = "FOC/rebate allowance refreshes every reset period (default Monthly). Copy from a Template, edit rules, Save — the rules act automatically when invoices are generated.";
            _lblStrategyTabHint.Location = new System.Drawing.Point(290, 49);
            top.Controls.Add(_lblStrategyTabHint);

            // Add the Fill control first, then the Top bar, so docking lays them out without overlap.
            _pgStrategy.Controls.Add(_strategyEditor);
            _pgStrategy.Controls.Add(top);
            TabMain.TabPages.Insert(1, _pgStrategy);   // second tab, right after "Service Item Under Contract"

            _strategyEditor.SetEditable(true);
            RefreshStrategyTab();
            ApplyFocResetToUi();   // bind the FOC reset loaded by LoadContract (controls now exist)

            // ═══ STRATEGY TAB HIDDEN ENTIRELY (user decision 2026-07-27) ═══
            // Deals now live ON the meters (waive meters + their config, MIN scope, multi-price),
            // so the WHOLE tab disappears — nothing is deleted: the rules editor, template picker
            // and FOC Reset all stay wired underneath. Saved rules still load, still SAVE, and
            // still bill live at Generate for legacy contracts; the FOC Reset value keeps loading
            // and saving with the contract unchanged. Un-hide by removing this one line.
            _pgStrategy.PageVisible = false;
        }

        // Last-loaded FOC reset values from the DB row; bound to the UI by ApplyFocResetToUi once
        // BuildStrategyTab has created the controls. New contract: defaults "M"/0 → Monthly.
        private string _focResetUnitDb = "M";
        private int _focResetNDb;

        // Inactive metadata: whether the LOADED row was already inactive (drives the deactivation
        // confirm — it only fires on the N→Y transition), plus WHEN/WHY it was deactivated.
        private bool _loadedInactive;
        private DateTime? _inactiveDate;
        private string _inactiveReason = "";
        private DevExpress.XtraEditors.LabelControl _lblInactiveInfo;

        // Small red "since dd/MM/yyyy — reason" note beside the Inactive checkbox.
        private void UpdateInactiveInfoLabel()
        {
            if (ChkInactive == null || ChkInactive.Parent == null) return;
            if (_lblInactiveInfo == null)
            {
                _lblInactiveInfo = new DevExpress.XtraEditors.LabelControl();
                _lblInactiveInfo.Appearance.ForeColor = System.Drawing.Color.Firebrick;
                _lblInactiveInfo.Appearance.Options.UseForeColor = true;
                _lblInactiveInfo.Location = new System.Drawing.Point(
                    ChkInactive.Location.X + ChkInactive.Width + 8, ChkInactive.Location.Y + 2);
                ChkInactive.Parent.Controls.Add(_lblInactiveInfo);
                _lblInactiveInfo.BringToFront();
            }
            _lblInactiveInfo.Text = _inactiveDate.HasValue
                ? "since " + _inactiveDate.Value.ToString("dd/MM/yyyy") +
                  (string.IsNullOrEmpty(_inactiveReason) ? "" : " — " + _inactiveReason)
                : "";
        }

        private void ApplyFocResetToUi()
        {
            if (_cmbFocReset == null) return;
            bool wasLoading = _loading;
            _loading = true;   // programmatic bind must not set _dirty (CmbFocReset_Changed checks _loading)
            try
            {
                _cmbFocReset.SelectedIndex = _focResetUnitDb == "W" ? 1 : (_focResetUnitDb == "D" ? 2 : 0);
                if (_focResetNDb > 0) _spnFocResetN.EditValue = _focResetNDb;
                _spnFocResetN.Visible = _cmbFocReset.SelectedIndex == 2;
            }
            finally { _loading = wasLoading; }
        }

        // Feed the control this contract's saved service items + load its own rules from the DB.
        // True when the contract's saved rules could NOT be loaded — the editor is locked and the
        // save skips the rules table, otherwise a load failure would delete-reinsert an EMPTY grid
        // and silently wipe the contract's real rules.
        private bool _strategyRulesLoadFailed;

        private void RefreshStrategyTab()
        {
            if (_strategyEditor == null) return;
            DataTable items = new DataTable();
            items.Columns.Add("ItemKey", typeof(long));
            items.Columns.Add("ServiceItemNo", typeof(string));
            items.Columns.Add("Description", typeof(string));
            foreach (ItemEditData d in _items)
                if (d.ItemKey > 0)
                    items.Rows.Add(d.ItemKey, string.IsNullOrEmpty(d.ServiceItemNo) ? ("#" + d.ItemKey) : d.ServiceItemNo, d.Description ?? "");
            _strategyEditor.SetServiceItems(items);
            try
            {
                _strategyEditor.LoadRules(ServiceContractPhotocopier.Classes.ScpStrategy.LoadContractRules(_db, _contractKey));
                _strategyRulesLoadFailed = false;
                _strategyEditor.SetEditable(true);
            }
            catch (Exception ex)
            {
                _strategyRulesLoadFailed = true;
                _strategyEditor.SetEditable(false);   // read-only: never let a failed load become a wipe on save
                XtraMessageBox.Show("Loading this contract's strategy rules FAILED — the Strategy tab is locked " +
                    "read-only so saving cannot wipe the saved rules.\r\n\r\n" + ex.Message,
                    "Strategy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CmbFocReset_Changed(object sender, EventArgs e)
        {
            if (_spnFocResetN != null) _spnFocResetN.Visible = _cmbFocReset.SelectedIndex == 2;   // Every N days
            if (!_loading) _dirty = true;
        }

        private void BtnCopyTemplate_Click(object sender, EventArgs e)
        {
            string code = _loadedStrategyCode ?? "";
            if (code.Length == 0)
            { XtraMessageBox.Show("Pick a Strategy on the header first, then click Copy.", "Copy Strategy Template"); return; }
            ServiceContractPhotocopier.Classes.StrategyDef def;
            try { def = ServiceContractPhotocopier.Classes.ScpStrategy.LoadByCode(_db, code); }
            catch (Exception ex)
            { XtraMessageBox.Show("Load template failed:\r\n" + ex.Message, "Copy Strategy Template"); return; }
            if (def == null || def.Rules.Count == 0)
            { XtraMessageBox.Show("Strategy '" + code + "' has no rules to copy (or is inactive).", "Copy Strategy Template"); return; }
            if (_strategyEditor.GetRules().Count > 0 &&
                XtraMessageBox.Show("Replace this contract's current strategy rules with the " + def.Rules.Count +
                    " rule(s) from template '" + code + "'?", "Copy Strategy Template",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _strategyEditor.LoadRules(def.Rules);   // ServiceItemKey defaults to 0 (all items)
            _dirty = true;
            XtraMessageBox.Show(def.Rules.Count + " rule(s) copied from template '" + code +
                "'. Bind any rule to a single Service Item if needed, then Save the contract.", "Copy Strategy Template",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Persist this contract's own strategy rules (replace-all) inside the contract save transaction.
        private void SaveContractStrategyRules(SqlConnection conn, SqlTransaction tx)
        {
            if (_strategyEditor == null || _contractKey <= 0) return;
            // The saved rules never loaded into the grid (editor is locked) — writing the empty grid
            // would WIPE the contract's real rules. Leave the rules table untouched this save.
            if (_strategyRulesLoadFailed) return;
            string seed = _loadedStrategyCode ?? "";
            ExecNonQuery(conn, tx, "DELETE FROM dbo.zSCP2_ContractStrategyRule WHERE ContractKey=@ck", P("@ck", _contractKey));
            foreach (ServiceContractPhotocopier.Classes.StrategyRule r in _strategyEditor.GetRules())
            {
                ExecNonQuery(conn, tx,
                    "INSERT INTO dbo.zSCP2_ContractStrategyRule (ContractKey, ServiceItemKeys, Seq, RuleKind, Scope, " +
                    "TargetAmount, PartialPct, FreeMonths, CommitAmount, FocCopies, RebatePct, NetBilling, LimitScope, LimitQty, SeededFromCode) " +
                    "VALUES (@ck,@sik,@seq,@kind,@scope,@tgt,@pp,@fm,@cm,@fc,@rb,@net,@ls,@lq,@seed)",
                    P("@ck", _contractKey), P("@sik", ServiceContractPhotocopier.Classes.ScpStrategy.JoinItemKeys(r.ServiceItemKeys)),
                    P("@seq", r.Seq), P("@kind", r.Kind),
                    P("@scope", r.Scope), P("@tgt", r.TargetAmount), P("@pp", r.PartialPct), P("@fm", r.FreeMonths),
                    P("@cm", r.CommitAmount), P("@fc", r.FocCopies), P("@rb", r.RebatePct),
                    P("@net", r.NetBilling ? "Y" : "N"), P("@ls", r.LimitScope == 'G' ? "G" : "S"),
                    P("@lq", r.LimitQty), P("@seed", seed));
            }
        }

        // ===================== Billing History tab ("what billing was generated") =====================
        private DevExpress.XtraTab.XtraTabPage _pgBillingHist;

        private void BuildBillingHistoryTab()
        {
            _pgBillingHist = PageBillingHistory;   // designer page (user layout 01/08)
            ServiceContractPhotocopier.Classes.ScpBillingHistory.BuildTab(_pgBillingHist, _db, "i.ContractKey", _contractKey);
        }

        private void RefreshBillingHistoryTab()
        {
            if (_pgBillingHist == null) return;
            ServiceContractPhotocopier.Classes.ScpBillingHistory.BuildTab(_pgBillingHist, _db, "i.ContractKey", _contractKey);
        }

        // ===================== Change History tab (field-level audit viewer) =====================
        // Read-only view over zSCP2_ContractAudit: every save's field diffs (old -> new, who, when),
        // plus rental / strategy-apply rows scoped to this contract. Answers "what did this field
        // say in June" for reports.

        private DevExpress.XtraTab.XtraTabPage _pgChangeHist;

        private void BuildChangeHistoryTab()
        {
            _pgChangeHist = PageChangeHistory;   // designer page (user layout 01/08)

            GridViewChangeHist.OptionsBehavior.Editable = false;
            GridViewChangeHist.OptionsView.ShowGroupPanel = false;
            GridViewChangeHist.OptionsView.ShowAutoFilterRow = true;
            RefreshChangeHistoryTab();
        }

        private void RefreshChangeHistoryTab()
        {
            if (GridChangeHist == null) return;
            DataTable dt = null;
            if (_contractKey > 0)
            {
                try
                {
                    dt = _db.GetDataTable(
                        "SELECT a.ChangedAt AS [When], a.ChangedBy AS [Who], a.ChangeSource AS [Source], " +
                        "a.FieldName AS [Field], ISNULL(a.OldValue,'') AS [Old], ISNULL(a.NewValue,'') AS [New], " +
                        "ISNULL(i.ServiceItemNo,'') AS [Item] " +
                        "FROM dbo.zSCP2_ContractAudit a " +
                        "LEFT JOIN dbo.zSCP2_Item i ON i.ItemKey = a.ItemKey " +
                        "WHERE a.ContractKey = " + _contractKey + " " +
                        "ORDER BY a.ChangedAt DESC, a.AuditKey DESC", false);
                }
                catch { }
            }
            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("When", typeof(DateTime));
                dt.Columns.Add("Who", typeof(string));
                dt.Columns.Add("Source", typeof(string));
                dt.Columns.Add("Field", typeof(string));
                dt.Columns.Add("Old", typeof(string));
                dt.Columns.Add("New", typeof(string));
                dt.Columns.Add("Item", typeof(string));
            }
            GridChangeHist.DataSource = dt;
            GridViewChangeHist.PopulateColumns();
            if (GridViewChangeHist.Columns["When"] != null)
            {
                GridViewChangeHist.Columns["When"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                GridViewChangeHist.Columns["When"].DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
                GridViewChangeHist.Columns["When"].Width = 130;
            }
            GridViewChangeHist.BestFitColumns();
        }

        private void MhSet(string col, string val)
        {
            DevExpress.XtraEditors.TextEdit ed;
            if (_mh.TryGetValue(col, out ed)) ed.Text = val ?? "";
        }

        // "Copy": copy the main contract contact/address into the Delivery Address block.
        private void DelCopy_Click(object sender, EventArgs e)
        {
            if (_mhDelAddress != null) _mhDelAddress.Text = TxtAddress.Text;
            MhSet("DelCity", MhVal("City"));
            MhSet("DelPostalCode", MhVal("PostalCode"));
            MhSet("DelState", MhVal("State"));
            MhSet("DelCountry", MhVal("Country"));
            MhSet("DelPhone", TxtPhone.Text);
            MhSet("DelFax", MhVal("Fax"));
            _dirty = true;
        }

        // "Search": pick one of the current customer's branches (dbo.Branch) and fill the delivery block.
        private void DelSearch_Click(object sender, EventArgs e)
        {
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString().Trim();
            if (debtor.Length == 0) { XtraMessageBox.Show("Pick a Customer first.", "Search"); return; }
            DataTable br;
            try
            {
                br = _db.GetDataTable(
                    "SELECT BranchCode, ISNULL(BranchName,'') AS BranchName, ISNULL(Address1,'') AS Address1, " +
                    "ISNULL(Address2,'') AS Address2, ISNULL(Address3,'') AS Address3, ISNULL(Address4,'') AS Address4, " +
                    "ISNULL(PostCode,'') AS PostCode, ISNULL(Phone1,'') AS Phone1, ISNULL(Fax1,'') AS Fax1, " +
                    "ISNULL(EmailAddress,'') AS EmailAddress, ISNULL(Contact,'') AS Contact " +
                    "FROM [dbo].[Branch] WHERE AccNo=N'" + debtor.Replace("'", "''") + "' ORDER BY BranchCode", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); return; }
            if (br.Rows.Count == 0) { XtraMessageBox.Show("This customer has no branches.", "Search"); return; }

            object sel = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Select Branch", br, "BranchCode",
                new string[] { "BranchCode", "BranchName", "Address1", "PostCode", "Phone1" },
                new string[] { "Branch Code", "Branch Name", "Address", "Post Code", "Phone" },
                new int[] { 90, 200, 200, 80, 100 });
            if (sel == null || sel == DBNull.Value) return;
            {
                DataRow[] f = br.Select("BranchCode='" + sel.ToString().Replace("'", "''") + "'");
                if (f.Length == 0) return;
                DataRow b = f[0];
                MhSet("DelBranchCode", AsStr(b["BranchCode"]));
                MhSet("DelBranchName", AsStr(b["BranchName"]));
                if (_mhDelAddress != null)
                    _mhDelAddress.Text = string.Join("\r\n", new string[] { AsStr(b["Address1"]), AsStr(b["Address2"]), AsStr(b["Address3"]), AsStr(b["Address4"]) }).Trim('\r', '\n');
                MhSet("DelPostalCode", AsStr(b["PostCode"]));
                MhSet("DelPhone", AsStr(b["Phone1"]));
                MhSet("DelFax", AsStr(b["Fax1"]));
                MhSet("DelEmail", AsStr(b["EmailAddress"]));
                MhSet("DelContactPerson", AsStr(b["Contact"]));
                _dirty = true;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            bool wasNew = _isNew;   // InsertContract flips _isNew during save; remember the entry state
            // Commit any in-progress inline edit (items grid + meter panel) so it rides this save.
            GridViewItems.PostEditor();
            GridViewItems.CloseEditor();
            if (GridViewMeterCfg != null)
            {
                GridViewMeterCfg.PostEditor();
                GridViewMeterCfg.CloseEditor();
                GridViewMeterCfg.UpdateCurrentRow();
            }
            if (string.IsNullOrWhiteSpace(TxtContractNo.Text))
            { XtraMessageBox.Show("Contract No is required.", "Validation"); return; }
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (string.IsNullOrWhiteSpace(debtor))
            { XtraMessageBox.Show("Customer (Debtor) is required.", "Validation"); return; }

            // Contract Expiry cannot be earlier than Contract Start.
            if (DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value &&
                DtExpiryDate.EditValue != null && DtExpiryDate.EditValue != DBNull.Value &&
                Convert.ToDateTime(DtExpiryDate.EditValue).Date < Convert.ToDateTime(DtStartDate.EditValue).Date)
            { XtraMessageBox.Show("Contract Expiry cannot be earlier than Contract Start.", "Validation"); return; }

            // Same rule per machine (final defence — the grid already rejects it inline).
            foreach (ItemEditData itv in _items)
            {
                if (itv.ServiceStartDate.HasValue && itv.ServiceExpiryDate.HasValue &&
                    itv.ServiceExpiryDate.Value.Date < itv.ServiceStartDate.Value.Date)
                {
                    XtraMessageBox.Show("Service item '" + (string.IsNullOrEmpty(itv.ServiceItemNo) ? "<NEW>" : itv.ServiceItemNo) +
                        "': Expiry cannot be earlier than Service Start.", "Validation");
                    return;
                }
            }

            // A waive contra without a rental meter would credit money from nowhere — block it.
            foreach (ItemEditData itw in _items)
            {
                if (itw.Meters == null) continue;
                bool hasWaive = false, hasRent = false;
                foreach (DataRow mrow in itw.Meters.Rows)
                {
                    if (mrow.RowState == DataRowState.Deleted) continue;
                    string t = Convert.ToString(mrow["MeterTypeCode"]).Trim();
                    if (t.Length == 0) continue;
                    if (IsWaiveType(t)) hasWaive = true;
                    else if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t)) hasRent = true;
                }
                if (hasWaive && !hasRent)
                {
                    XtraMessageBox.Show("Service item '" + (string.IsNullOrEmpty(itw.ServiceItemNo) ? "<NEW>" : itw.ServiceItemNo) +
                        "': a RENTAL WAIVE meter needs a RENTAL meter on the same machine — add the rent line first.",
                        "Validation");
                    return;
                }
            }

            // Deactivation guard — turning Inactive ON silently stops ALL billing + auto-fetch for the
            // whole contract, so: (1) confirm with an impact summary, (2) surface any staged readings
            // that are not invoiced yet (they would never be billed), (3) steer "the contract simply
            // ENDED" cases to the Expiry date instead, (4) stamp WHEN + WHY for the listings.
            if (ChkInactive.Checked && !_loadedInactive)
            {
                if (_contractKey > 0)
                {
                    int machineCount = 0;
                    foreach (ItemEditData itc in _items) if (!itc.Inactive) machineCount++;
                    int staged = 0;
                    try
                    {
                        object o = _db.ExecuteScalar(
                            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
                            "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                            "JOIN dbo.zSCP2_Item i2 ON i2.ItemKey = m.ItemKey " +
                            "WHERE i2.ContractKey = " + _contractKey + " AND e.InvoicedDocKey IS NULL");
                        staged = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
                    }
                    catch { }
                    string msg =
                        "Deactivate contract '" + TxtContractNo.Text.Trim() + "'?\r\n\r\n" +
                        "This STOPS all monthly billing and auto-fetch for its " + machineCount + " machine(s).\r\n" +
                        (staged > 0
                            ? "\r\n⚠  " + staged + " staged reading(s) are NOT invoiced yet — while the contract is " +
                              "inactive they will NEVER be billed. Consider generating them first.\r\n"
                            : "") +
                        "\r\nIf the contract simply ENDED, set the Service Expiry (To) date instead — that stops " +
                        "billing the same way AND keeps the end date on record.\r\n\r\nContinue?";
                    if (XtraMessageBox.Show(msg, "Deactivate Contract", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                        != DialogResult.Yes)
                        return;
                    string reason = XtraInputBox.Show("Reason for deactivating (optional, shown beside the checkbox):",
                        "Deactivate Contract", _inactiveReason ?? "");
                    if (reason == null) return;   // Cancel at the reason prompt backs out of the whole save
                    _inactiveReason = reason.Trim();
                }
                _inactiveDate = DateTime.Today;
            }
            else if (!ChkInactive.Checked)
            {
                // Active (or reactivated): never write a deactivation stamp. Cleared UNCONDITIONALLY —
                // a failed save may have left _inactiveDate/_inactiveReason behind from an aborted
                // deactivation, and those must not ride a later save of an ACTIVE contract.
                _inactiveDate = null;
                _inactiveReason = "";
            }

            // Reserve the REAL contract number now (only if new & still showing the auto-preview) so
            // the counter is consumed on save, not on every Auto click.
            if (_isNew && !string.IsNullOrEmpty(_autoPeekNo) && TxtContractNo.Text.Trim() == _autoPeekNo)
                TxtContractNo.Text = ServiceContractPhotocopier.Classes.ScpDocNo.Next(
                    _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_CONTRACT);

            // Embedded items showing an auto-preview number: reserve a real one each at save. The
            // blank-number check is a safety net — NO item may ever be inserted without a number.
            foreach (ItemEditData d in _items)
            {
                if (d.ServiceItemNoIsAuto || string.IsNullOrWhiteSpace(d.ServiceItemNo))
                {
                    d.ServiceItemNo = ServiceContractPhotocopier.Classes.ScpDocNo.Next(
                        _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_SERVICE_ITEM);
                    d.ServiceItemNoIsAuto = false;
                }
            }

            // Failure-path state protection: everything below runs in ONE transaction, but the save
            // helpers also mutate FORM state (_contractKey, _isNew, d.ItemKey). If the transaction
            // rolls back, that state must roll back too — otherwise the next Save UPDATEs a contract
            // row that never committed (dead key) and fails forever until the form is closed.
            bool entryIsNew = _isNew;
            long entryContractKey = _contractKey;
            long[] entryItemKeys = new long[_items.Count];
            for (int ki = 0; ki < _items.Count; ki++) entryItemKeys[ki] = _items[ki].ItemKey;
            System.Collections.Generic.List<string> extrasSkipped = new System.Collections.Generic.List<string>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        // Field-level change audit: snapshot BEFORE any of this save's updates. The
                        // contract row is updated twice (UpdateContract + SaveMoreHeader) — one
                        // before/after diff at the end covers both.
                        bool auditWasNew = _isNew;
                        System.Collections.Generic.Dictionary<string, string> auditBefore =
                            _isNew ? null : ServiceContractPhotocopier.Classes.ScpContractAudit.Snapshot(conn, tx, _contractKey);

                        if (_isNew) InsertContract(conn, tx, debtor);
                        else UpdateContract(conn, tx, debtor);

                        // Upsert items, preserving ItemKeys (attach/detach model). Existing items no
                        // longer in the contract are DETACHED (ContractKey=NULL), not deleted, so they
                        // survive as contract-less and keep their meter history.
                        System.Collections.Generic.HashSet<long> kept = new System.Collections.Generic.HashSet<long>();
                        foreach (ItemEditData d in _items) if (d.ItemKey > 0) kept.Add(d.ItemKey);
                        using (SqlCommand ex = new SqlCommand("SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=@ck", conn, tx))
                        {
                            ex.Parameters.AddWithValue("@ck", _contractKey);
                            System.Collections.Generic.List<long> present = new System.Collections.Generic.List<long>();
                            using (SqlDataReader rd = ex.ExecuteReader()) { while (rd.Read()) present.Add(rd.GetInt64(0)); }
                            foreach (long k in present)
                                if (!kept.Contains(k))
                                {
                                    ExecNonQuery(conn, tx, "UPDATE [dbo].[zSCP2_Item] SET ContractKey=NULL, LastModified=GETDATE() WHERE ItemKey=@ik", P("@ik", k));
                                    // The detached item no longer has an owner — close its open
                                    // ownership period so the history doesn't show it still owned.
                                    ExecNonQuery(conn, tx,
                                        "UPDATE [dbo].[zSCP2_ItemDebtorHistory] SET EndDate=CAST(GETDATE() AS DATE), LastModified=GETDATE() " +
                                        "WHERE ItemKey=@ik AND EndDate IS NULL", P("@ik", k));
                                    // Its item-bound provided lines leave the contract with it —
                                    // ContractKey is NOT NULL on that table, so they cannot be kept as
                                    // orphans, and leaving them shows lines of a machine we no longer hold.
                                    ExecNonQuery(conn, tx,
                                        "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=@ik", P("@ik", k));
                                }
                        }

                        int pos = 0;
                        foreach (ItemEditData d in _items)
                        {
                            long itemKey;
                            // An EXISTING item whose extended fields never hydrated (LoadExtras failed)
                            // must NOT have them persisted: the empty defaults would silently wipe its
                            // Grade / Note / PM / date + context overrides. New items have nothing to wipe.
                            bool extrasSafe = true;
                            if (d.ItemKey > 0)
                            {
                                extrasSafe = d.LoadedCtxValid;
                                UpdateItem(conn, tx, d, pos);
                                itemKey = d.ItemKey;
                                // Item codes carry no history -> safe to rebuild. METERS ARE NEVER WIPED:
                                // zSCP_MeterTrans + zSCP2_MeterEntry cascade-delete on zSCP2_ItemMeter, so a
                                // delete-and-rebuild would destroy the machine's whole reading history. They
                                // are diffed in place instead (update/insert; delete only removed meters).
                                ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=@ik", P("@ik", itemKey));
                                zSCP2_Item_Form.SaveMetersPreservingReadings(conn, tx, d, itemKey);
                            }
                            else
                            {
                                itemKey = InsertItem(conn, tx, d, pos);
                                d.ItemKey = itemKey;   // keep the in-memory item in sync with its new row
                                InsertMeters(conn, tx, d, itemKey);   // brand-new item: no readings exist yet
                            }
                            InsertItemCodes(conn, tx, d, itemKey);
                            SaveItemSpareParts(conn, tx, d, itemKey, _contractKey);
                            if (extrasSafe) zSCP2_Item_Form.PersistItemExtras(conn, tx, d, itemKey);
                            else extrasSkipped.Add(string.IsNullOrEmpty(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo);
                            pos++;
                        }
                        SaveSpareParts(conn, tx);
                        SaveMoreHeader(conn, tx);
                        SaveContractStrategyRules(conn, tx);   // this contract's own strategy rules copy

                        // Change History: diff old vs new contract row (both UPDATEs included); new
                        // contracts get a single CREATED marker. Never blocks the save.
                        if (auditWasNew)
                            ServiceContractPhotocopier.Classes.ScpContractAudit.WriteCreated(
                                conn, tx, _contractKey, TxtContractNo.Text.Trim(),
                                ServiceContractPhotocopier.Classes.ScpContractAudit.SOURCE_CONTRACT);
                        else if (auditBefore != null)
                            ServiceContractPhotocopier.Classes.ScpContractAudit.WriteDiff(
                                conn, tx, _contractKey, TxtContractNo.Text.Trim(), auditBefore,
                                ServiceContractPhotocopier.Classes.ScpContractAudit.Snapshot(conn, tx, _contractKey),
                                ServiceContractPhotocopier.Classes.ScpContractAudit.SOURCE_CONTRACT);
                        tx.Commit();
                    }
                }
                _dirty = false;
                _loadedInactive = ChkInactive.Checked;   // next toggle counts from this saved state
                UpdateInactiveInfoLabel();
                UpdateFormModeTitle();                   // NEW → EDIT after the first successful save
                if (extrasSkipped.Count > 0)
                    XtraMessageBox.Show("Note: " + extrasSkipped.Count + " service item(s) kept their extended fields " +
                        "unchanged (" + string.Join(", ", extrasSkipped.ToArray()) + ") because those fields failed to " +
                        "load when this contract was opened. Reopen the contract to edit them.",
                        "Saved with a note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (wasNew)
                {
                    // The contract now exists (has a ContractKey) -> switch to EDIT mode and STAY OPEN so
                    // the user can immediately add service items (Add/Attach are enabled only in edit mode).
                    _detachedThisSession.Clear();
                    LoadContract();          // reload header + (empty) items from the just-saved row
                    RebuildItemsView();
                    LoadSpareParts();
                    LoadMoreHeader();
                    RefreshStrategyTab();
                    RefreshBillingHistoryTab();
                    RefreshChangeHistoryTab();
                    _dirty = false;
                    UpdateServiceItemButtons();
                    _savedOk = true;   // "saved at least once" — the eventual close reports OK to the caller
                    XtraMessageBox.Show("Contract saved. You can now add service items.", "Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Stay OPEN after save (user decision 2026-07-27): keep working; Close is manual.
                    // Reload from the DB so every tab (incl. Change History) shows the saved state.
                    _savedOk = true;   // "saved at least once" — the eventual close reports OK to the caller
                    LoadContract();
                    RebuildItemsView();
                    LoadSpareParts();
                    LoadMoreHeader();
                    RefreshStrategyTab();
                    RefreshBillingHistoryTab();
                    RefreshChangeHistoryTab();
                    _dirty = false;
                    UpdateServiceItemButtons();
                    XtraMessageBox.Show("Contract saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // The transaction rolled back — roll the form state back with it, or the next Save
                // would UPDATE a contract row that never committed (and fail forever).
                _isNew = entryIsNew;
                _contractKey = entryContractKey;
                for (int ki = 0; ki < _items.Count && ki < entryItemKeys.Length; ki++)
                    _items[ki].ItemKey = entryItemKeys[ki];
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertContract(SqlConnection conn, SqlTransaction tx, string debtor)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Contract] " +
                "(ContractNo, ContractTypeCode, DebtorCode, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
                " ContractValue, BillingDay, BillOnMonthEnd, BillingMode, Address1, Attention, Phone, TermCode, AreaCode, StaffCode, " +
                " ReferenceNo, Description, Remark1, Remark2, Note, DeptNo, ProjNo, StrategyCode, RentalSeparateInvoice, RentalBillingDay, InvoiceReportName, GenerateSOA, SOAReportName, FOCResetUnit, FOCResetN, Inactive, InactiveDate, InactiveReason, Created, LastModified) " +
                "VALUES (@no,@type,@debtor,@cdate,@sdate,@edate,@val,@bday,@monthend,@bmode,@addr,@attn,@phone,@term,@area,@staff," +
                "@refno,@desc,@r1,@r2,@note,@dept,@proj,@strategy,@rentsep,@rentday,@invrpt,@gensoa,@soarpt,@focresetunit,@focresetn,@inact,@inactdate,@inactreason,GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                AddContractParams(cmd, debtor);
                _contractKey = Convert.ToInt64(cmd.ExecuteScalar());
            }
            _isNew = false;
        }

        private void UpdateContract(SqlConnection conn, SqlTransaction tx, string debtor)
        {
            string sql =
                "UPDATE [dbo].[zSCP2_Contract] SET ContractNo=@no, ContractTypeCode=@type, DebtorCode=@debtor, " +
                "ContractDate=@cdate, ServiceStartDate=@sdate, ServiceExpiryDate=@edate, ContractValue=@val, " +
                "BillingDay=@bday, BillOnMonthEnd=@monthend, BillingMode=@bmode, Address1=@addr, Attention=@attn, Phone=@phone, TermCode=@term, " +
                "AreaCode=@area, StaffCode=@staff, ReferenceNo=@refno, Description=@desc, Remark1=@r1, Remark2=@r2, Note=@note, " +
                "DeptNo=@dept, ProjNo=@proj, StrategyCode=@strategy, RentalSeparateInvoice=@rentsep, RentalBillingDay=@rentday, " +
                "InvoiceReportName=@invrpt, GenerateSOA=@gensoa, SOAReportName=@soarpt, " +
                "FOCResetUnit=@focresetunit, FOCResetN=@focresetn, " +
                "Inactive=@inact, InactiveDate=@inactdate, InactiveReason=@inactreason, " +
                "Modified=GETDATE(), LastModified=GETDATE() WHERE ContractKey=@ck";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                AddContractParams(cmd, debtor);
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.ExecuteNonQuery();
            }
        }

        private void AddContractParams(SqlCommand cmd, string debtor)
        {
            cmd.Parameters.AddWithValue("@no", TxtContractNo.Text.Trim());
            cmd.Parameters.AddWithValue("@type", SluContractType.EditValue == null ? "" : SluContractType.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@debtor", debtor);
            cmd.Parameters.AddWithValue("@cdate", DateParam(DtContractDate.EditValue));
            cmd.Parameters.AddWithValue("@sdate", DateParam(DtStartDate.EditValue));
            cmd.Parameters.AddWithValue("@edate", DateParam(DtExpiryDate.EditValue));
            cmd.Parameters.AddWithValue("@val", SpnContractValue.Value);
            // Month-end retired: the stored day is EXACTLY what the spinner shows (1..28), and the
            // BillOnMonthEnd column is pinned to 'N' (kept only so old readers stay harmless).
            cmd.Parameters.AddWithValue("@bday", (byte)(int)SpnBillingDay.Value);
            cmd.Parameters.AddWithValue("@monthend", "N");
            cmd.Parameters.AddWithValue("@bmode", CurrentBillingMode());
            cmd.Parameters.AddWithValue("@addr", TxtAddress.Text.Trim());
            cmd.Parameters.AddWithValue("@attn", TxtAttention.Text.Trim());
            cmd.Parameters.AddWithValue("@phone", TxtPhone.Text.Trim());
            cmd.Parameters.AddWithValue("@term", TxtTerm.Text.Trim());
            cmd.Parameters.AddWithValue("@area", TxtArea.EditValue == null ? "" : TxtArea.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@refno", TxtRefNo.Text.Trim());
            cmd.Parameters.AddWithValue("@staff", SluAgent.EditValue == null ? "" : SluAgent.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@dept", SluDept.EditValue == null ? "" : SluDept.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@proj", SluProject.EditValue == null ? "" : SluProject.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@desc", TxtDescription.Text.Trim());
            cmd.Parameters.AddWithValue("@r1", TxtRemark1.Text.Trim());
            cmd.Parameters.AddWithValue("@r2", TxtRemark2.Text.Trim());
            cmd.Parameters.AddWithValue("@note", (object)(TxtNote.Text ?? ""));
            cmd.Parameters.AddWithValue("@strategy", _loadedStrategyCode ?? "");
            cmd.Parameters.AddWithValue("@rentsep", ChkRentalSeparate.Checked ? "Y" : "N");
            cmd.Parameters.AddWithValue("@rentday", SpnRentalDay != null ? (object)(int)SpnRentalDay.Value : (object)_loadedRentalDay);
            cmd.Parameters.AddWithValue("@invrpt", TplVal(SluInvoiceTemplate, _loadedInvRpt));
            cmd.Parameters.AddWithValue("@gensoa", ChkGenerateSOA != null ? (ChkGenerateSOA.Checked ? "Y" : "N") : (_loadedGenSOA ? "Y" : "N"));
            cmd.Parameters.AddWithValue("@soarpt", TplVal(SluSOATemplate, _loadedSOARpt));
            string focResetUnit = _cmbFocReset != null && _cmbFocReset.SelectedIndex == 1 ? "W"
                : (_cmbFocReset != null && _cmbFocReset.SelectedIndex == 2 ? "D" : "M");
            cmd.Parameters.AddWithValue("@focresetunit", focResetUnit);
            cmd.Parameters.AddWithValue("@focresetn", focResetUnit == "D" && _spnFocResetN != null ? Convert.ToInt32(_spnFocResetN.Value) : 0);
            cmd.Parameters.AddWithValue("@inact", ChkInactive.Checked ? "Y" : "N");
            cmd.Parameters.AddWithValue("@inactdate", _inactiveDate.HasValue ? (object)_inactiveDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@inactreason", _inactiveReason ?? "");
        }

        private long InsertItem(SqlConnection conn, SqlTransaction tx, ItemEditData d, int pos)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Item] " +
                "(ContractKey, ServiceItemNo, SerialNumber, Description, BillingDayOverride, " +
                " DepartmentCode, JobCode, StockLocationCode, Pos, Inactive, IsGroupItem, MachineMode, LastModified) " +
                "VALUES (@ck,@no,@serial,@desc,@bday,@dept,@job,@loc,@pos,@inact,@isgrp,@mmode,GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                cmd.Parameters.AddWithValue("@loc", d.StockLocationCode ?? "");
                cmd.Parameters.AddWithValue("@pos", pos);
                cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@isgrp", d.IsGroupItem ? "Y" : "N");
                cmd.Parameters.AddWithValue("@mmode", d.MachineMode ?? "");
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        // Re-parent + update an EXISTING service item (used for attached items and edited items) so
        // its ItemKey (and meter history) is preserved instead of delete+reinsert.
        private void UpdateItem(SqlConnection conn, SqlTransaction tx, ItemEditData d, int pos)
        {
            string sql =
                // OwnerDebtorCode is cleared: attached to a contract, the CONTRACT owns the item — a
                // stale direct-owner would mis-resolve the COALESCE if the contract's debtor were blank.
                "UPDATE [dbo].[zSCP2_Item] SET ContractKey=@ck, OwnerDebtorCode='', ServiceItemNo=@no, SerialNumber=@serial, " +
                "Description=@desc, BillingDayOverride=@bday, DepartmentCode=@dept, JobCode=@job, " +
                "StockLocationCode=@loc, Pos=@pos, Inactive=@inact, IsGroupItem=@isgrp, MachineMode=@mmode, LastModified=GETDATE() WHERE ItemKey=@ik";
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", _contractKey);
                cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                cmd.Parameters.AddWithValue("@loc", d.StockLocationCode ?? "");
                cmd.Parameters.AddWithValue("@pos", pos);
                cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@isgrp", d.IsGroupItem ? "Y" : "N");
                cmd.Parameters.AddWithValue("@mmode", d.MachineMode ?? "");
                cmd.Parameters.AddWithValue("@ik", d.ItemKey);
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertItemCodes(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            if (d.ItemCodes == null) return;
            int pos = 0;
            foreach (DataRow r in d.ItemCodes.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["ItemCode"] == null ? "" : r["ItemCode"].ToString().Trim();
                if (string.IsNullOrEmpty(code)) continue;
                string sql =
                    "INSERT INTO [dbo].[zSCP2_ItemCode] (ItemKey, ItemCode, Description, Qty, SerialNumber, Pos, LastModified) " +
                    "VALUES (@ik,@code,@desc,@qty,@serial,@pos,GETDATE());";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@desc", r["Description"] == null ? "" : r["Description"].ToString());
                    cmd.Parameters.AddWithValue("@qty", AsDec(r["Qty"]));
                    cmd.Parameters.AddWithValue("@serial", r["SerialNumber"] == null ? "" : r["SerialNumber"].ToString());
                    cmd.Parameters.AddWithValue("@pos", pos++);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void InsertMeters(SqlConnection conn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            if (d.Meters == null) return;
            foreach (DataRow r in d.Meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string code = r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString().Trim();
                if (string.IsNullOrEmpty(code)) continue;
                string sql =
                    "INSERT INTO [dbo].[zSCP2_ItemMeter] " +
                    "(ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                    " RebateQtyInPercent, FOCQty, InitialReading, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialThreshold, WaivePartialAmount, WaiveScope, LastModified) " +
                    "VALUES (@ik,@code,@desc,@role,@mser,@min,@rate,@multi,@rebate,@foc,@init,@wn,@wt,@wpt,@wpa,@ws,GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@desc", r.Table.Columns.Contains("Description") && r["Description"] != DBNull.Value
                        ? (object)r["Description"].ToString() : "");
                    string role = r["MeterRole"] == null ? "" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    zSCP2_Item_Form.RequireMeterRole(role, code, d.ServiceItemNo);   // no silent "NA" default — empty role blocks the save
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@mser", r.Table.Columns.Contains("MachineSerialNo") && r["MachineSerialNo"] != DBNull.Value
                        ? ((string)r["MachineSerialNo"]).Trim() : "");
                    cmd.Parameters.AddWithValue("@min", AsDec(r["MinimumCharges"]));
                    cmd.Parameters.AddWithValue("@rate", AsDec(r["ChargesRate"]));
                    cmd.Parameters.AddWithValue("@multi", r["MeterMultiPriceCode"] == null ? "" : r["MeterMultiPriceCode"].ToString());
                    cmd.Parameters.AddWithValue("@rebate", AsDec(r["RebateQtyInPercent"]));
                    cmd.Parameters.AddWithValue("@foc", AsDec(r["FOCQty"]));
                    cmd.Parameters.AddWithValue("@init", AsDec(r["InitialReading"]));
                    cmd.Parameters.AddWithValue("@wn", r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0);
                    cmd.Parameters.AddWithValue("@wt", r.Table.Columns.Contains("WaiveTargetAmount") ? AsDec(r["WaiveTargetAmount"]) : 0m);
                    cmd.Parameters.AddWithValue("@wpt", r.Table.Columns.Contains("WaivePartialThreshold") ? AsDec(r["WaivePartialThreshold"]) : 0m);
                    cmd.Parameters.AddWithValue("@wpa", r.Table.Columns.Contains("WaivePartialAmount") ? AsDec(r["WaivePartialAmount"]) : 0m);
                    string wsv = r.Table.Columns.Contains("WaiveScope") ? AsStr(r["WaiveScope"]).Trim() : "";
                    cmd.Parameters.AddWithValue("@ws", wsv.Length > 0 ? wsv : "BKCL");
                    long newMeterKey = Convert.ToInt64(cmd.ExecuteScalar());
                    string tiersCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                        ? Convert.ToString(r["CustomTiers"]) : "";
                    zSCP2_Item_Form.SaveCustomTiers(conn, tx, newMeterKey, tiersCsv);
                }
            }
        }

        private static void ExecNonQuery(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                if (ps != null) cmd.Parameters.AddRange(ps);
                cmd.ExecuteNonQuery();
            }
        }

        private static SqlParameter P(string name, object val) { return new SqlParameter(name, val ?? DBNull.Value); }

        private void BtnClose_Click(object sender, EventArgs e) { this.Close(); }

        // ---- Ribbon button click wrappers (forward to the existing handlers) ----
        private void barSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnSave_Click(sender, System.EventArgs.Empty); }
        private void barClose_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnClose_Click(sender, System.EventArgs.Empty); }
        private void barAddItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnAddItem_Click(sender, System.EventArgs.Empty); }
        private void barEditItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnEditItem_Click(sender, System.EventArgs.Empty); }
        private void barDelItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e) { BtnDelItem_Click(sender, System.EventArgs.Empty); }

        // ===================== Demo Fill (random test data) =====================

        private static readonly Random _rng = new Random();
        private static readonly string[] _demoWords = {
            "Alpha","Beta","Gamma","Delta","Prime","Nova","Metro","Summit","Vertex","Pioneer",
            "Copier","Printer","MFP","Toner","Drum","Fuser","Roller","Panel","Sensor","Unit" };
        private static readonly string[] _demoCities = { "Johor Bahru","Kuala Lumpur","Penang","Ipoh","Melaka","Shah Alam","Klang" };
        private static readonly string[] _demoStates = { "Johor","Selangor","Penang","Perak","Melaka","Kedah","Pahang" };

        private string DW() { return _demoWords[_rng.Next(_demoWords.Length)]; }
        private string RandRef() { return DW() + "-" + _rng.Next(1000, 9999); }

        // Fills EVERY field with random values (different each click) and adds random rows to both grids
        // — a demo/testing helper.
        private void barDemoFill_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try { DemoFill(); }
            catch (Exception ex) { XtraMessageBox.Show("Demo fill failed:\r\n" + ex.Message, "Demo"); }
        }

        private void DemoFill()
        {
            // --- header ---
            SluContractType.EditValue = RandomLookupValue(SluContractType);
            if (_debtorLookup != null && _debtorLookup.Rows.Count > 0)
                LkDebtorCode.EditValue = _debtorLookup.Rows[_rng.Next(_debtorLookup.Rows.Count)]["AccNo"];   // triggers auto-fill
            SluAgent.EditValue = RandomLookupValue(SluAgent);

            DateTime cd = DateTime.Today.AddDays(-_rng.Next(0, 60));
            _loading = true;   // programmatic fill: keep the on-the-spot date validation quiet
            try
            {
                DtContractDate.EditValue = cd;
                DtStartDate.EditValue = cd;
                DtExpiryDate.EditValue = cd.AddMonths(_rng.Next(6, 36));
            }
            finally { _loading = false; }
            SyncExpiryCalendarMin();
            SpnContractValue.Value = _rng.Next(500, 50000);
            SpnBillingDay.Value = _rng.Next(1, 29);   // 1..28 inclusive — the boundary day stays testable
            SetBillingMode(_rng.Next(2) == 0 ? "G" : "S");
            TxtDescription.Text = "Demo contract " + DW() + " " + _rng.Next(100, 999);
            TxtRemark1.Text = "Remark " + DW(); TxtRemark2.Text = "Remark " + DW();
            TxtNote.Text = "Auto-generated demo note " + DW() + " " + _rng.Next(1000, 9999);

            // --- More Header (over the debtor-mapped values) ---
            MhSet("City", _demoCities[_rng.Next(_demoCities.Length)]);
            MhSet("State", _demoStates[_rng.Next(_demoStates.Length)]);
            MhSet("PostalCode", _rng.Next(10000, 99999).ToString());
            MhSet("Country", "Malaysia");
            MhSet("Ref1", RandRef()); MhSet("Ref2", RandRef()); MhSet("Ref3", RandRef()); MhSet("Ref4", RandRef());

            // NOTE: service items are NOT added here — they can only be added in edit mode (after the
            // contract is saved), so the demo just fills the header + more-header + item-provided.

            // --- Item Provided: add 1-3 random rows ---
            int nSp = _rng.Next(1, 4);
            for (int i = 0; i < nSp; i++)
            {
                DataRow r = _spareParts.NewRow();
                r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
                string code = ""; string desc = DW() + " part";
                if (_spItemLookup != null && _spItemLookup.Rows.Count > 0)
                {
                    DataRow it = _spItemLookup.Rows[_rng.Next(_spItemLookup.Rows.Count)];
                    code = Convert.ToString(it["ItemCode"]); desc = Convert.ToString(it["Description"]);
                    r["UOM"] = it["UOM"]; r["TaxType"] = it["TaxType"];
                }
                else { r["UOM"] = "UNIT"; r["TaxType"] = ""; }
                r["ItemCode"] = code; r["Description"] = desc; r["Unlimited"] = false;
                r["Quantity"] = _rng.Next(1, 20); r["Discount"] = ""; r["UnitPrice"] = _rng.Next(5, 500);
                r["TaxInclusive"] = false; r["TaxRate"] = 0m; r["Pos"] = _spareParts.Rows.Count;
                ComputeSpareRow(r);
                _spareParts.Rows.Add(r);
            }
            RenumberSpareParts();
            GridViewSpareParts.RefreshData();

            _dirty = true;
            RebuildItemsView();
            XtraMessageBox.Show("Random demo data filled (header + more header + " + nSp + " item-provided row(s)).\r\n" +
                "Save the contract, then add service items in edit mode.", "Demo Fill");
        }

        private object RandomLookupValue(DevExpress.XtraEditors.SearchLookUpEdit slu)
        {
            DataTable dt = slu.Properties.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0) return null;
            string vm = slu.Properties.ValueMember;
            if (string.IsNullOrEmpty(vm) || !dt.Columns.Contains(vm)) return null;
            return dt.Rows[_rng.Next(dt.Rows.Count)][vm];
        }

        private DataTable SafeGet(string sql)
        {
            try { return _db.GetDataTable(sql, false); } catch { return null; }
        }

        // ===================== Copy / Clipboard ribbon =====================

        // V2 adds: month-end flag, contract dates, value, ref no, remarks, note, rental-split flag on
        // the H line, and one M line per meter (type/role/pricing/CustomTiers) under each I line.
        // Paste still accepts V1 (same leading token layout; missing fields just stay untouched).
        private const string CLIP_HEADER_V1 = "ATP-SCP-DOC-V1";
        private const string CLIP_HEADER = "ATP-SCP-DOC-V2";

        // "Copy from other Service Contract": pick a saved contract and load its DEAL into THIS new
        // contract as a template — header + strategy rules + provided lines + More Header. Service
        // items are NEVER copied (a CSSI is one physical machine; see LoadContractAsTemplateCore).
        private void barCopyFrom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!_isNew)
            {
                // Belt-and-braces behind the Enabled gate: on a SAVED contract this fill would replace
                // (and at save, DETACH) every machine currently on it.
                XtraMessageBox.Show("'Copy from other Service Contract' fills a NEW contract as a template.\r\n" +
                    "To duplicate this contract, use 'Copy to a new Service Contract' instead.", "Copy from");
                return;
            }
            DataTable src;
            try
            {
                src = _db.GetDataTable(
                    "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM [dbo].[zSCP2_Contract] c LEFT JOIN [dbo].[Debtor] d ON d.AccNo=c.DebtorCode " +
                    "ORDER BY c.ContractNo", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); return; }
            long key = PickContract(src, "Copy from Service Contract");
            if (key == 0) return;
            if (!LoadContractAsTemplate(key)) return;   // failure already shown
            _dirty = true;
            XtraMessageBox.Show("Copied: header + strategy rules + provided lines.\r\n\r\n" +
                "Machines (CSSI) are NEVER copied — a CSSI is one physical machine. Bring machines in with " +
                "Quick Add, 'Attach existing Service Item' or 'Generate From Serial No', then Save.",
                "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // "Copy to a new Service Contract": open a NEW contract editor pre-filled from the current one.
        private void barCopyToNew_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_isNew && _contractKey == 0)
            { XtraMessageBox.Show("Save this contract first before copying it to a new one.", "Copy to new"); return; }
            if (_dirty &&
                XtraMessageBox.Show("You have UNSAVED changes. The copy reads the last SAVED version of this " +
                    "contract, so those changes will NOT be included.\r\n\r\nContinue anyway?", "Copy to new",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            // Non-modal like the list's editors — the source contract stays usable alongside.
            zSCP2_Contract_Form f = new zSCP2_Contract_Form(_db, _contractKey, true);
            f.Show(this);
        }

        // Serialize the document to the clipboard (tagged text): H = header, I = one service item,
        // M = one meter of the preceding I line. Every field is Tsv-escaped. Identity values (item
        // numbers, initial readings) are carried for reference only — a paste ALWAYS creates fresh
        // items with new numbers and zeroed initial readings.
        private void barCopyWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(CLIP_HEADER);
            sb.Append("H\t")
              .Append(Tsv(Cv(SluContractType))).Append('\t').Append(Tsv(Cv(LkDebtorCode))).Append('\t')
              .Append(Tsv(TxtAddress.Text)).Append('\t')
              .Append(Tsv(TxtAttention.Text)).Append('\t').Append(Tsv(TxtPhone.Text)).Append('\t')
              .Append(Tsv(TxtTerm.Text)).Append('\t').Append(Tsv(TxtArea.EditValue == null ? "" : TxtArea.EditValue.ToString())).Append('\t')
              .Append(Tsv(Cv(SluAgent))).Append('\t').Append(Tsv(TxtDescription.Text)).Append('\t')
              .Append(((int)SpnBillingDay.Value)).Append('\t').Append(CurrentBillingMode()).Append('\t')
              .Append("N").Append('\t')
              .Append(TsvDate(DtStartDate.EditValue)).Append('\t').Append(TsvDate(DtExpiryDate.EditValue)).Append('\t')
              .Append(SpnContractValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t')
              .Append(Tsv(TxtRefNo.Text)).Append('\t').Append(Tsv(TxtRemark1.Text)).Append('\t')
              .Append(Tsv(TxtRemark2.Text)).Append('\t').Append(Tsv(TxtNote.Text)).Append('\t')
              .Append(ChkRentalSeparate.Checked ? "Y" : "N").Append('\t')
              .Append(SpnRentalDay != null ? ((int)SpnRentalDay.Value).ToString() : "0").Append('\t')
              .Append(Tsv(TplVal(SluInvoiceTemplate, _loadedInvRpt))).Append('\t')
              .Append(ChkGenerateSOA != null && ChkGenerateSOA.Checked ? "Y" : "N").Append('\t')
              .Append(Tsv(TplVal(SluSOATemplate, _loadedSOARpt))).AppendLine();
            foreach (ItemEditData d in _items)
            {
                sb.Append("I\t").Append(Tsv(d.ServiceItemNo)).Append('\t').Append(Tsv(d.SerialNumber)).Append('\t')
                  .Append(Tsv(d.Description)).Append('\t')
                  .Append(d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value.ToString() : "").Append('\t')
                  .Append(Tsv(d.DepartmentCode)).Append('\t').Append(Tsv(d.JobCode)).Append('\t')
                  .Append(Tsv(d.StockLocationCode)).Append('\t').Append(d.Inactive ? "Y" : "N").Append('\t')
                  .Append(Tsv(d.ItemCode)).Append('\t').Append(Tsv(d.GradeCode)).Append('\t')
                  .Append(Tsv(d.ServiceTypeCode)).Append('\t')
                  .Append(d.PurchaseDate.HasValue ? d.PurchaseDate.Value.ToString("yyyy-MM-dd") : "").Append('\t')
                  .Append(Tsv(d.ReferenceNo)).Append('\t')
                  .Append(d.ServiceStartDate.HasValue ? d.ServiceStartDate.Value.ToString("yyyy-MM-dd") : "").Append('\t')
                  .Append(d.ServiceExpiryDate.HasValue ? d.ServiceExpiryDate.Value.ToString("yyyy-MM-dd") : "").AppendLine();
                if (d.Meters != null)
                    foreach (DataRow m in d.Meters.Rows)
                    {
                        if (m.RowState == DataRowState.Deleted) continue;
                        string mtype = m["MeterTypeCode"] == null || m["MeterTypeCode"] == DBNull.Value ? "" : m["MeterTypeCode"].ToString().Trim();
                        if (mtype.Length == 0) continue;
                        sb.Append("M\t").Append(Tsv(mtype)).Append('\t')
                          .Append(Tsv(MStr(m, "MeterRole"))).Append('\t').Append(Tsv(MStr(m, "Description"))).Append('\t')
                          .Append(MDec(m, "MinimumCharges")).Append('\t').Append(MDec(m, "ChargesRate")).Append('\t')
                          .Append(Tsv(MStr(m, "MeterMultiPriceCode"))).Append('\t')
                          .Append(MDec(m, "RebateQtyInPercent")).Append('\t').Append(MDec(m, "FOCQty")).Append('\t')
                          .Append(Tsv(MStr(m, "CustomTiers"))).AppendLine();
                    }
            }
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show("Whole document copied to clipboard (header + items + meters).", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
        }

        private static string MStr(DataRow r, string col)
        { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? r[col].ToString() : ""; }

        private static string MDec(DataRow r, string col)
        { return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDecimal(r[col]).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0"; }

        private static string TsvDate(object editValue)
        {
            if (editValue == null || editValue == DBNull.Value) return "";
            DateTime dt;
            return DateTime.TryParse(editValue.ToString(), out dt) ? dt.ToString("yyyy-MM-dd") : "";
        }

        // Copy the SELECTED service item rows as tab-separated (Excel-pasteable).
        private void barCopySelected_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] sel = GridViewItems.GetSelectedRows();
            if (sel == null || sel.Length == 0) { XtraMessageBox.Show("Select one or more service item rows first.", "Copy Selected"); return; }
            System.Collections.Generic.List<int> handles = new System.Collections.Generic.List<int>(sel);
            CopyItemsAsTsv(handles);
        }

        // Copy the WHOLE service item grid as a spreadsheet (TSV with a header row).
        private void barCopySpreadsheet_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Collections.Generic.List<int> handles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < GridViewItems.RowCount; i++) handles.Add(GridViewItems.GetVisibleRowHandle(i));
            CopyItemsAsTsv(handles);
        }

        // Exports EVERY column the item grid shows (kept in sync with RebuildItemsView's schema) —
        // the paste side maps rows back by these header captions, never by blind position.
        private void CopyItemsAsTsv(System.Collections.Generic.List<int> handles)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("No\tService Item No\tItem Code\tSerial Number\tGrade\tService Type\tPurchase Date\tReference No\tProvided Items\tBilling Day\tService Start\tBlack Meter\tColour Meter\tDescription\tInactive\tExpiry");
            int copied = 0;
            foreach (int rh in handles)
            {
                if (rh < 0) continue;
                sb.Append(Gv(rh, "No")).Append('\t').Append(Gv(rh, "ServiceItemNo")).Append('\t')
                  .Append(Gv(rh, "ItemCode")).Append('\t').Append(Gv(rh, "SerialNumber")).Append('\t')
                  .Append(Gv(rh, "GradeCode")).Append('\t').Append(Gv(rh, "ServiceType")).Append('\t')
                  .Append(Gv(rh, "PurchaseDate")).Append('\t').Append(Gv(rh, "ReferenceNo")).Append('\t')
                  .Append(Gv(rh, "Items")).Append('\t').Append(Gv(rh, "BillingDay")).Append('\t')
                  .Append(Gv(rh, "ServiceStart")).Append('\t').Append(Gv(rh, "BKMeter")).Append('\t')
                  .Append(Gv(rh, "CLMeter")).Append('\t').Append(Gv(rh, "Description")).Append('\t')
                  .Append(Gv(rh, "Inactive")).Append('\t').Append(Gv(rh, "Expiry")).AppendLine();
                copied++;
            }
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show(copied + " row(s) copied.", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
        }

        private string Gv(int rh, string field)
        {
            object v = GridViewItems.GetRowCellValue(rh, field);
            return v == null || v == DBNull.Value ? "" : v.ToString().Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        // Paste a whole document (from Copy Whole Document): REPLACES header + items in this form.
        private void barPasteWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (text == null || (!text.StartsWith(CLIP_HEADER) && !text.StartsWith(CLIP_HEADER_V1)))
            { XtraMessageBox.Show("The clipboard does not contain a copied Service Contract document.", "Paste"); return; }
            if (_items.Count > 0)
            {
                string warn = "Paste will REPLACE the header fields and all " + _items.Count +
                    " service item row(s) currently in this editor.";
                if (!_isNew) warn += "\r\n\r\nThis is a SAVED contract: its current machines would be DETACHED at the next Save.";
                if (XtraMessageBox.Show(warn + "\r\n\r\nContinue?", "Paste Whole Document",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            _items.Clear();
            ItemEditData last = null;
            foreach (string line in lines)
            {
                string[] p = line.Split('\t');
                if (p.Length == 0) continue;
                if (p[0] == "H")
                {
                    // Shared token layout for V1 (12 tokens) and V2 (V1 + month-end, dates, value,
                    // ref no, remarks, note, rental-split). At() returns "" past the end of a V1 line.
                    SluContractType.EditValue = SetOrNull(At(p, 1));
                    LkDebtorCode.EditValue = SetOrNull(At(p, 2));
                    TxtAddress.Text = At(p, 3); TxtAttention.Text = At(p, 4); TxtPhone.Text = At(p, 5);
                    TxtTerm.Text = At(p, 6); TxtArea.EditValue = SetOrNull(At(p, 7)); SluAgent.EditValue = SetOrNull(At(p, 8));
                    TxtDescription.Text = At(p, 9);
                    int bd; if (int.TryParse(At(p, 10), out bd)) SpnBillingDay.Value = Math.Min(Math.Max(bd, 1), 28);
                    SetBillingMode(At(p, 11));
                    if (p.Length > 12)
                    {
                        // token 12 was the month-end flag — retired, deliberately ignored on paste
                        _loading = true;   // programmatic fill: keep the on-the-spot date validation quiet
                        try
                        {
                            DtStartDate.EditValue = ParseClipDate(At(p, 13));
                            DtExpiryDate.EditValue = ParseClipDate(At(p, 14));
                        }
                        finally { _loading = false; }
                        SyncExpiryCalendarMin();
                        decimal cv;
                        if (decimal.TryParse(At(p, 15), System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.InvariantCulture, out cv)) SpnContractValue.Value = cv;
                        TxtRefNo.Text = At(p, 16); TxtRemark1.Text = At(p, 17);
                        TxtRemark2.Text = At(p, 18); TxtNote.Text = At(p, 19);
                        ChkRentalSeparate.Checked = At(p, 20) == "Y";
                        int rdClip;
                        if (SpnRentalDay != null && int.TryParse(At(p, 21), out rdClip))
                            SpnRentalDay.Value = Math.Max(0, Math.Min(28, rdClip));
                        if (SluInvoiceTemplate != null && At(p, 22).Length > 0) SluInvoiceTemplate.EditValue = At(p, 22);
                        if (ChkGenerateSOA != null) ChkGenerateSOA.Checked = At(p, 23) == "Y";
                        if (SluSOATemplate != null && At(p, 24).Length > 0) SluSOATemplate.EditValue = At(p, 24);
                    }
                }
                else if (p[0] == "I") { last = ItemFromTsv(p, 1); _items.Add(last); }
                else if (p[0] == "M" && last != null) AddMeterFromTsv(last, p);
            }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show("Document pasted. Items get NEW service item numbers at Save. Review and Save.", "Pasted");
        }

        private static object ParseClipDate(string s)
        {
            DateTime dt;
            return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dt) ? (object)dt : null;
        }

        // M line: MeterTypeCode, Role, Description, Min, Rate, MultiPrice, Rebate%, FOC, CustomTiers.
        // InitialReading is identity-specific and always starts at 0 on a pasted machine.
        private void AddMeterFromTsv(ItemEditData d, string[] p)
        {
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            DataRow r = d.Meters.NewRow();
            r["MeterTypeCode"] = At(p, 1);
            r["MeterRole"] = At(p, 2).Trim().ToUpperInvariant();
            r["Description"] = At(p, 3);
            r["MachineSerialNo"] = "";
            r["MinimumCharges"] = ClipDec(At(p, 4));
            r["ChargesRate"] = ClipDec(At(p, 5));
            r["MeterMultiPriceCode"] = At(p, 6);
            r["RebateQtyInPercent"] = ClipDec(At(p, 7));
            r["FOCQty"] = ClipDec(At(p, 8));
            r["InitialReading"] = 0m;
            r["CustomTiers"] = At(p, 9);
            d.Meters.Rows.Add(r);
        }

        private static decimal ClipDec(string s)
        {
            decimal v;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0m;
        }

        // Paste ONLY item detail rows: accepts our tagged I/M lines, or the spreadsheet TSV produced by
        // "Copy Selected / Copy as Spreadsheet" (recognised by its header row and mapped by CAPTION, so
        // columns can never land in the wrong fields). Bare TSV without a header row is mapped
        // conservatively — Serial + Description only, nothing deeper is guessed.
        private void barPasteItems_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (string.IsNullOrEmpty(text)) { XtraMessageBox.Show("Clipboard is empty.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int added = 0;
            ItemEditData last = null;
            string[] header = null;   // the spreadsheet header row, when present
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0) continue;
                string[] p = line.Split('\t');
                if (p[0] == CLIP_HEADER || p[0] == CLIP_HEADER_V1 || p[0] == "H") continue;
                if (p[0] == "No") { header = p; continue; }
                if (p[0] == "I") { last = ItemFromTsv(p, 1); _items.Add(last); added++; }
                else if (p[0] == "M" && last != null) AddMeterFromTsv(last, p);
                else if (header != null) { last = ItemFromSpreadsheetRow(header, p); _items.Add(last); added++; }
                else
                {
                    // Bare TSV: optional leading row number, then Service Item No (discarded — new
                    // numbers are drawn at Save), Serial, Description.
                    int off = 0; int rowNo;
                    if (p.Length > 1 && int.TryParse(p[0], out rowNo)) off = 1;
                    ItemEditData d = NewPastedItem();
                    d.SerialNumber = At(p, off + 1);
                    d.Description = At(p, off + 2);
                    _items.Add(d); last = d; added++;
                }
            }
            if (added == 0) { XtraMessageBox.Show("No item rows found on the clipboard.", "Paste"); return; }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show(added + " item(s) pasted. They get NEW service item numbers at Save.", "Pasted");
        }

        // Map a "Copy as Spreadsheet" row by its header captions — column re-orders or additions on the
        // copy side can then never mis-map a value here.
        private ItemEditData ItemFromSpreadsheetRow(string[] header, string[] p)
        {
            ItemEditData d = NewPastedItem();
            for (int i = 0; i < header.Length && i < p.Length; i++)
            {
                string v = p[i];
                switch (header[i])
                {
                    case "Serial Number": d.SerialNumber = v; break;
                    case "Item Code": d.ItemCode = v; break;
                    case "Grade": d.GradeCode = v; break;
                    case "Service Type": d.ServiceTypeCode = v; break;
                    case "Reference No": d.ReferenceNo = v; break;
                    case "Description": d.Description = v; break;
                    case "Inactive": d.Inactive = v == "Y"; break;
                    case "Purchase Date": { DateTime dt; if (DateTime.TryParse(v, out dt)) d.PurchaseDate = dt; break; }
                    case "Service Start": { DateTime dt; if (DateTime.TryParse(v, out dt)) d.ServiceStartDate = dt; break; }
                    case "Expiry": { DateTime dt; if (DateTime.TryParse(v, out dt)) d.ServiceExpiryDate = dt; break; }
                    // "Billing Day" shows the EFFECTIVE day (contract default or override) — it cannot
                    // be told apart from the default, so it is never pasted as a per-item override.
                    // "Black/Colour Meter" / "Provided Items" are display summaries; meters ride only
                    // on tagged M lines (Copy Whole Document).
                }
            }
            if (d.ServiceStartDate.HasValue && d.ServiceExpiryDate.HasValue &&
                d.ServiceExpiryDate.Value.Date < d.ServiceStartDate.Value.Date) d.ServiceExpiryDate = null;
            return d;
        }

        private ItemEditData NewPastedItem()
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            ApplyContractDateDefaults(d);   // Service Start / Expiry follow the contract dates
            d.ServiceItemNo = "";          // shows <NEW>; a real number is reserved at Save
            d.ServiceItemNoIsAuto = true;  // NEVER keep a pasted number — it belongs to the source item
            return d;
        }

        // Build a fresh ItemEditData from a tagged I line. The pasted Service Item No is deliberately
        // DISCARDED (auto-number at save): zSCP2_Item.ServiceItemNo is UNIQUE, so keeping the source
        // number made every Copy Whole -> Paste Whole save die on the unique key.
        private ItemEditData ItemFromTsv(string[] p, int off)
        {
            ItemEditData d = NewPastedItem();
            d.SerialNumber = At(p, off + 1); d.Description = At(p, off + 2);
            int bd; d.BillingDayOverride = (int.TryParse(At(p, off + 3), out bd) && bd >= 1 && bd <= 28) ? (int?)bd : null;
            d.DepartmentCode = At(p, off + 4); d.JobCode = At(p, off + 5); d.StockLocationCode = At(p, off + 6);
            d.Inactive = At(p, off + 7) == "Y";
            d.ItemCode = At(p, off + 8); d.GradeCode = At(p, off + 9); d.ServiceTypeCode = At(p, off + 10);
            DateTime pd;
            if (DateTime.TryParse(At(p, off + 11), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out pd)) d.PurchaseDate = pd;
            d.ReferenceNo = At(p, off + 12);
            DateTime sd;
            if (DateTime.TryParse(At(p, off + 13), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out sd)) d.ServiceStartDate = sd;
            DateTime xd;
            if (DateTime.TryParse(At(p, off + 14), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out xd)) d.ServiceExpiryDate = xd;
            if (d.ServiceStartDate.HasValue && d.ServiceExpiryDate.HasValue &&
                d.ServiceExpiryDate.Value.Date < d.ServiceStartDate.Value.Date) d.ServiceExpiryDate = null;
            return d;
        }

        private static string At(string[] a, int i) { return (i >= 0 && i < a.Length) ? a[i] : ""; }
        private static string Tsv(string s) { return (s ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " "); }
        private static string Cv(DevExpress.XtraEditors.BaseEdit ed) { return ed.EditValue == null ? "" : ed.EditValue.ToString(); }
        private static string SafeClipboardText()
        { try { return System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null; } catch { return null; } }

        // Load another contract's content into THIS form as a template (fresh copies; keeps this form new).
        // Spare parts / strategy rules / More Header need tabs that may not exist yet when this runs
        // from the clone constructor — those parts are deferred via _templateSourceKey and applied by
        // ApplyTemplateExtras() once OnFormLoad has built the tabs.
        private long _templateSourceKey;

        // Returns true when the template loaded; any failure is SHOWN (never swallowed — a silent
        // half-loaded template looks exactly like "the machines didn't copy").
        private bool LoadContractAsTemplate(long sourceKey)
        {
            try
            {
                LoadContractAsTemplateCore(sourceKey);
                return true;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Copy failed while loading the source contract:\r\n\r\n" +
                    ex.GetType().Name + ": " + ex.Message, "Copy from contract",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void LoadContractAsTemplateCore(long sourceKey)
        {
            _loading = true;
            try
            {
                DataTable dt = _db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + sourceKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                SluContractType.EditValue = SetOrNull(AsStr(r["ContractTypeCode"]));
                LkDebtorCode.EditValue = AsStr(r["DebtorCode"]);
                DtStartDate.EditValue = AsDate(r["ServiceStartDate"]);
                DtExpiryDate.EditValue = AsDate(r["ServiceExpiryDate"]);
                SpnContractValue.Value = AsDec(r["ContractValue"]);
                SpnBillingDay.Value = Math.Min(AsInt(r["BillingDay"], 1), 28);   // month-end retired: legacy 31 -> 28
                SetBillingMode(AsStr(r["BillingMode"]));
                TxtAddress.Text = AsStr(r["Address1"]); TxtAttention.Text = AsStr(r["Attention"]);
                TxtPhone.Text = AsStr(r["Phone"]); TxtTerm.Text = AsStr(r["TermCode"]);
                TxtArea.EditValue = SetOrNull(AsStr(r["AreaCode"])); SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
                TxtDescription.Text = AsStr(r["Description"]);
                TxtRemark1.Text = AsStr(r["Remark1"]); TxtRemark2.Text = AsStr(r["Remark2"]); TxtNote.Text = AsStr(r["Note"]);
                TxtRefNo.Text = r.Table.Columns.Contains("ReferenceNo") ? AsStr(r["ReferenceNo"]) : "";
                SluDept.EditValue = r.Table.Columns.Contains("DeptNo") ? SetOrNull(AsStr(r["DeptNo"])) : null;
                SluProject.EditValue = r.Table.Columns.Contains("ProjNo") ? SetOrNull(AsStr(r["ProjNo"])) : null;
                _loadedStrategyCode = r.Table.Columns.Contains("StrategyCode") ? AsStr(r["StrategyCode"]).Trim() : "";
                ChkRentalSeparate.Checked = r.Table.Columns.Contains("RentalSeparateInvoice") && AsStr(r["RentalSeparateInvoice"]) == "Y";
            _loadedRentalDay = r.Table.Columns.Contains("RentalBillingDay") && r["RentalBillingDay"] != DBNull.Value
                ? Math.Max(0, Math.Min(28, Convert.ToInt32(r["RentalBillingDay"]))) : 0;
            if (SpnRentalDay != null) SpnRentalDay.Value = _loadedRentalDay;
            _loadedInvRpt = r.Table.Columns.Contains("InvoiceReportName") ? AsStr(r["InvoiceReportName"]).Trim() : "";
            _loadedGenSOA = r.Table.Columns.Contains("GenerateSOA") && AsStr(r["GenerateSOA"]) == "Y";
            _loadedSOARpt = r.Table.Columns.Contains("SOAReportName") ? AsStr(r["SOAReportName"]).Trim() : "";
            if (SluInvoiceTemplate != null) SluInvoiceTemplate.EditValue = _loadedInvRpt.Length > 0 ? (object)_loadedInvRpt : null;
            if (ChkGenerateSOA != null) ChkGenerateSOA.Checked = _loadedGenSOA;
            if (SluSOATemplate != null) SluSOATemplate.EditValue = _loadedSOARpt.Length > 0 ? (object)_loadedSOARpt : null;
                _focResetUnitDb = r.Table.Columns.Contains("FOCResetUnit") ? AsStr(r["FOCResetUnit"]) : "M";
                _focResetNDb = r.Table.Columns.Contains("FOCResetN") ? AsInt(r["FOCResetN"], 0) : 0;
                ApplyFocResetToUi();   // no-op before the strategy tab exists; re-applied by OnFormLoad

                // ═══ SERVICE ITEMS ARE DELIBERATELY NOT COPIED (user decision 2026-07-26) ═══
                // A CSSI is ONE physical machine — cloning it would duplicate the machine serial and
                // make meter readings ambiguous. Copying a contract copies the DEAL (header, strategy
                // rules, provided lines, More Header); the machines are brought in explicitly via
                // Quick Add / Attach existing Service Item / Generate From Serial No.
                _items.Clear();
            }
            finally { _loading = false; }
            RebuildItemsView();
            _templateSourceKey = sourceKey;
            ApplyTemplateExtras();   // immediate when the tabs exist (Copy-from button); deferred otherwise
        }

        // Spare parts (contract-level lines), strategy RULES and More Header of the template source.
        // Runs once the target controls exist; clears _templateSourceKey only for the parts it managed.
        private void ApplyTemplateExtras()
        {
            if (_templateSourceKey <= 0) return;
            bool ready = _spareParts != null && _strategyEditor != null && _mh.Count > 0;
            if (!ready) return;
            long src = _templateSourceKey;
            _templateSourceKey = 0;
            try
            {
                // Contract-level provided lines (ItemKey NULL) ride along as fresh rows; item-bound
                // lines are re-created by each copied item's own save.
                _spareParts.Rows.Clear();
                DataTable sp = _db.GetDataTable(
                    "SELECT ItemCode, Description, Unlimited, UOM, Quantity, Discount, UnitPrice, TaxType, TaxInclusive, TaxRate, Pos " +
                    "FROM [dbo].[zSCP2_ContractSparePart] WHERE ContractKey=" + src + " AND ItemKey IS NULL ORDER BY Pos", false);
                foreach (DataRow s in sp.Rows)
                {
                    DataRow nr = _spareParts.NewRow();
                    nr["SparePartKey"] = 0L; nr["ItemKey"] = DBNull.Value; nr["Bound"] = false;
                    nr["ItemCode"] = s["ItemCode"]; nr["Description"] = s["Description"];
                    nr["Unlimited"] = AsStr(s["Unlimited"]) == "Y";
                    nr["UOM"] = s["UOM"]; nr["Quantity"] = s["Quantity"]; nr["Discount"] = s["Discount"];
                    nr["UnitPrice"] = s["UnitPrice"]; nr["TaxType"] = s["TaxType"];
                    nr["TaxInclusive"] = AsStr(s["TaxInclusive"]) == "Y"; nr["TaxRate"] = s["TaxRate"];
                    nr["Pos"] = s["Pos"];
                    ComputeSpareRow(nr);
                    _spareParts.Rows.Add(nr);
                }
                RenumberSpareParts();
                GridViewSpareParts.RefreshData();

                // The source contract's OWN rule copy seeds this contract's rules (renewal scenario) —
                // per-item bindings reset to "all items" because the copied items get new ItemKeys.
                System.Collections.Generic.List<ServiceContractPhotocopier.Classes.StrategyRule> rules =
                    ServiceContractPhotocopier.Classes.ScpStrategy.LoadContractRules(_db, src);
                foreach (ServiceContractPhotocopier.Classes.StrategyRule ru in rules) ru.ServiceItemKeys.Clear();
                _strategyEditor.LoadRules(rules);

                // More Header (delivery block + refs) — same table row, different column family.
                DataTable mhdt = _db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + src, false);
                if (mhdt.Rows.Count > 0)
                {
                    DataRow mr = mhdt.Rows[0];
                    foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _mh)
                        if (mr.Table.Columns.Contains(kv.Key)) kv.Value.Text = AsStr(mr[kv.Key]);
                    if (_mhDelAddress != null && mr.Table.Columns.Contains("DelAddress"))
                        _mhDelAddress.Text = AsStr(mr["DelAddress"]);
                }
            }
            catch { }   // template niceties must never block opening the editor
        }

        // ---- value helpers ----
        private static string AsStr(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal AsDec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static int AsInt(object o, int def) { int n; return (o != null && o != DBNull.Value && int.TryParse(o.ToString(), out n)) ? n : def; }
        private static object AsDate(object o) { return (o == null || o == DBNull.Value) ? null : (object)Convert.ToDateTime(o); }
        private static object DateParam(object editValue)
        {
            if (editValue == null || editValue == DBNull.Value) return DBNull.Value;
            DateTime dt;
            if (DateTime.TryParse(editValue.ToString(), out dt)) return dt;
            return DBNull.Value;
        }
    }
}
