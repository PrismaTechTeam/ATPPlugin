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
            BuildBillingHistoryTab();
            BuildChangeHistoryTab();

            // Dirty tracking for the close confirmation: any header edit marks the form dirty. Wired
            // AFTER the initial load so loading an existing contract does not itself set the flag.
            WireDirtyTracking();
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnFormClosing);

            ExtendContractRibbon();
            HideServiceStartDate();
            UpdateServiceItemButtons();
        }

        // Service items live in the in-memory _items list until Save, so Create / Edit / Delete work in
        // BOTH new and edit mode (the save loop inserts the contract header first, then the items).
        // Attach/inline-attach still need existing rows to look up, so they stay edit-mode-only.
        private void UpdateServiceItemButtons()
        {
            bool canAttach = !_isNew;
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
            // Generate From Serial No: NEW mode only (hide after a new contract is saved -> edit mode).
            if (_barGenSerial != null)
                _barGenSerial.Visibility = _isNew
                    ? DevExpress.XtraBars.BarItemVisibility.Always
                    : DevExpress.XtraBars.BarItemVisibility.Never;
            LblItemsHint.Text = "";
            LblItemsHint.Visible = false;
        }

        // The contract no longer maintains ANY service dates — each service item carries its own
        // start/expiry. The hidden editors keep whatever values were loaded so old rows keep their
        // dates on re-save.
        private void HideServiceStartDate()
        {
            LblStartDate.Visible = false;
            DtStartDate.Visible = false;
            LblExpiryDate.Visible = false;
            DtExpiryDate.Visible = false;
        }

        // "Generate From Serial No" — pick DO/IV lines that carry machine serials; auto-fill the
        // customer header (NEW mode) and auto-create one service item per machine.
        private DevExpress.XtraBars.BarButtonItem _barGenSerial;
        private DevExpress.XtraBars.BarButtonItem _barApplyStrategy;

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

                // "Apply Strategy to Meters": fan the attached strategy's parameters onto this
                // contract's BK/CL + rental meters in one audited click (the strategy fast path).
                _barApplyStrategy = new DevExpress.XtraBars.BarButtonItem();
                _barApplyStrategy.Caption = "Apply Strategy\r\nto Meters";
                _barApplyStrategy.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
                _barApplyStrategy.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(BarApplyStrategy_ItemClick);
                RibbonCtl.Items.Add(_barApplyStrategy);
                grpItem.ItemLinks.Add(_barApplyStrategy);

                try
                {
                    float dpi = 96f;
                    try { dpi = this.DeviceDpi; } catch { }
                    AutoCount.Images.IAutoCountImage img =
                        AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                    _barGenSerial.ImageOptions.Image = img.GetLargeImage_Inquiry();
                    _barApplyStrategy.ImageOptions.Image = img.GetLargeImage_Commit();
                }
                catch { }
            }
            catch { }
        }

        // Apply the attached strategy's RULE lines to this contract's meters — with a PREVIEW + confirm,
        // every write audited (source STRATEGY-APPLY). A strategy is a bundle of rules; each meter-push
        // rule (RENTAL-FREE-N, FOC-REBATE) materialises its numbers onto the meters, so the existing
        // billing engine (which reads FOCQty/RebateQtyInPercent off the meter) then honours it. COMMIT-MIN,
        // LIMIT, WAIVE-TARGET and INITIAL-METER are reported only (evaluated live at generation / manually).
        // Requires a saved, non-dirty contract so the in-memory grid can be reloaded to match the DB after.
        private void BarApplyStrategy_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ApplyStrategyToMeters();
        }

        // Fills / overrides this contract's BK/CL (and rental) meters from its own strategy rules, per each
        // rule's Scope + Service Item set. One audited transaction, with a preview. Shared by the ribbon
        // button and the Strategy tab's "Apply to Meters" button.
        private void ApplyStrategyToMeters()
        {
            if (_isNew || _contractKey <= 0)
            { XtraMessageBox.Show("Save the contract first, then apply the strategy.", "Apply Strategy"); return; }
            if (_dirty)
            { XtraMessageBox.Show("You have unsaved changes. Save the contract first — the apply writes directly to the saved meters.", "Apply Strategy"); return; }

            // Apply the CONTRACT'S OWN strategy rules (from the Strategy tab / zSCP2_ContractStrategyRule),
            // not the master template — the contract edits its own copy.
            System.Collections.Generic.Dictionary<long, ServiceContractPhotocopier.Classes.StrategyDef> smap =
                ServiceContractPhotocopier.Classes.ScpStrategy.LoadForContracts(_db, new long[] { _contractKey });
            ServiceContractPhotocopier.Classes.StrategyDef sd;
            if (!smap.TryGetValue(_contractKey, out sd) || sd.Rules.Count == 0)
            {
                XtraMessageBox.Show("This contract has no strategy rules yet.\r\n\r\nGo to the Strategy tab — copy from a " +
                    "template (or add rules), Save the contract, then Apply.", "Apply Strategy");
                return;
            }
            string code = string.IsNullOrEmpty(sd.Code)
                ? (SluStrategy.EditValue == null ? "(contract)" : SluStrategy.EditValue.ToString().Trim()) : sd.Code;

            // Build the preview: which meters change which field to what.
            DataTable meters;
            try
            {
                meters = _db.GetDataTable(
                    "SELECT m.ItemMeterKey, i.ItemKey, i.ServiceItemNo, m.MeterTypeCode, m.MeterRole, " +
                    "ISNULL(m.FOCQty,0) AS FOCQty, ISNULL(m.RebateQtyInPercent,0) AS RebatePct, " +
                    "ISNULL(m.MinimumCharges,0) AS MinCharges, ISNULL(mt.IsFlatCharge,'N') AS IsFlat " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                    "WHERE i.ContractKey = " + _contractKey + " ORDER BY i.ServiceItemNo, m.MeterTypeCode", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load meters failed:\r\n" + ex.Message, "Apply Strategy"); return; }

            // Meter-push changes keyed by (ItemMeterKey|column) so later rules override earlier ones on the
            // same field; old value is always the original DB value read above.
            System.Collections.Generic.Dictionary<string, object[]> changeMap =
                new System.Collections.Generic.Dictionary<string, object[]>();
            System.Text.StringBuilder reportNotes = new System.Text.StringBuilder();

            foreach (ServiceContractPhotocopier.Classes.StrategyRule rule in sd.Rules)
            {
                string kind = rule.Kind;
                if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_RENTAL_FREE_N)
                {
                    foreach (DataRow m in meters.Rows)
                    {
                        if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(Convert.ToInt64(m["ItemKey"]))) continue;
                        bool rental = Convert.ToString(m["IsFlat"]) == "Y" &&
                                      ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(Convert.ToString(m["MeterTypeCode"]));
                        if (!rental) continue;
                        StageChange(changeMap, m, "FOCQty", "FOCQty", AsDec(m["FOCQty"]), rule.FreeMonths);
                    }
                }
                else if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_FOC_REBATE)
                {
                    foreach (DataRow m in meters.Rows)
                    {
                        if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(Convert.ToInt64(m["ItemKey"]))) continue;
                        string role = Convert.ToString(m["MeterRole"]).Trim().ToUpperInvariant();
                        if (role != "BK" && role != "CL") continue;
                        if (!ScopeMatchesRole(rule.Scope, role)) continue;   // BK / CL / BK+CL
                        StageChange(changeMap, m, "FOCQty", "FOCQty", AsDec(m["FOCQty"]), rule.FocCopies);
                        StageChange(changeMap, m, "RebateQtyInPercent", "RebatePct", AsDec(m["RebatePct"]), rule.RebatePct);
                    }
                }
                else if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_COMMIT_MIN)
                {
                    // Committed minimum PRINT charge -> write MinimumCharges on the scoped BK/CL usage meters.
                    foreach (DataRow m in meters.Rows)
                    {
                        if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(Convert.ToInt64(m["ItemKey"]))) continue;
                        string role = Convert.ToString(m["MeterRole"]).Trim().ToUpperInvariant();
                        if (role != "BK" && role != "CL") continue;
                        if (!ScopeMatchesRole(rule.Scope, role)) continue;
                        StageChange(changeMap, m, "MinimumCharges", "MinCharges", AsDec(m["MinCharges"]), rule.CommitAmount);
                    }
                }
                else if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_LIMIT)
                {
                    if (rule.LimitScope == 'G')
                    {
                        bool hasCombine = false;
                        foreach (ItemEditData it in _items)
                            if ((it.ServiceItemNo ?? "").Trim().ToUpperInvariant().EndsWith(".C")) hasCombine = true;
                        reportNotes.AppendLine(hasCombine
                            ? "• GROUP LIMIT — .C combine item present; group billing flows through it."
                            : "• GROUP LIMIT WARNING — no '.C' combine item on this contract; create one to pool group FOC.");
                    }
                    else
                    {
                        // Single-machine FOC cap -> write FOCQty (free-copy cap) on the scoped BK/CL meters.
                        foreach (DataRow m in meters.Rows)
                        {
                            if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(Convert.ToInt64(m["ItemKey"]))) continue;
                            string role = Convert.ToString(m["MeterRole"]).Trim().ToUpperInvariant();
                            if (role != "BK" && role != "CL") continue;
                            if (!ScopeMatchesRole(rule.Scope, role)) continue;
                            StageChange(changeMap, m, "FOCQty", "FOCQty", AsDec(m["FOCQty"]), rule.LimitQty);
                        }
                    }
                }
                else if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_WAIVE_TARGET)
                {
                    reportNotes.AppendLine("• WAIVE-TARGET — evaluated live at invoice generation (target RM " +
                        rule.TargetAmount.ToString("0.00") + (rule.PartialPct > 0m ? ", partial " + rule.PartialPct.ToString("0.##") + "%" : "") + ").");
                }
                else if (kind == ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_INITIAL_METER)
                {
                    reportNotes.AppendLine("• INITIAL-METER — estimate readings are keyed manually in Meter Reading (no meter value pushed).");
                }
            }

            // Turn the staged map into a concrete change list (skip no-ops).
            System.Collections.Generic.List<object[]> changes = new System.Collections.Generic.List<object[]>();
            System.Text.StringBuilder preview = new System.Text.StringBuilder();
            foreach (object[] ch in changeMap.Values)
            {
                decimal oldV = (decimal)ch[3], newV = (decimal)ch[4];
                if (oldV == newV) continue;
                changes.Add(ch);
                preview.AppendLine(ch[6] + "  " + ch[7] + ":  " + (string)ch[5] + "  " +
                    oldV.ToString("0.##") + " → " + newV.ToString("0.##"));
            }

            if (changes.Count == 0)
            {
                string msg = "No meter fields need changing for strategy '" + code + "'.";
                if (reportNotes.Length > 0) msg += "\r\n\r\nNotes:\r\n" + reportNotes;
                XtraMessageBox.Show(msg, "Apply Strategy");
                return;
            }
            string pv = preview.ToString();
            if (pv.Length > 1600) pv = pv.Substring(0, 1600) + "\r\n… (" + changes.Count + " changes in total)";
            string body = "Apply strategy '" + code + "' to this contract's meters?\r\n\r\n" + pv;
            if (reportNotes.Length > 0) body += "\r\nNotes:\r\n" + reportNotes;
            body += "\r\n\r\nThis FILLS the meter fields (Min Charges / Rebate % / Free Qty). The meter grid is where\r\n" +
                    "the actual billing values live — you can still adjust any machine there afterwards.";
            if (XtraMessageBox.Show(body, "Apply Strategy — Preview", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("ApplyStrategy"))
                    {
                        try
                        {
                            Guid changeSet = Guid.NewGuid();
                            foreach (object[] ch in changes)
                            {
                                ExecNonQuery(cn, tx,
                                    "UPDATE dbo.zSCP2_ItemMeter SET [" + (string)ch[2] + "]=@v, LastModified=GETDATE() WHERE ItemMeterKey=@mk",
                                    P("@v", (decimal)ch[4]), P("@mk", Convert.ToInt64(ch[0])));
                                ServiceContractPhotocopier.Classes.ScpContractAudit.WriteRow(cn, tx, _contractKey,
                                    TxtContractNo.Text.Trim(), Convert.ToInt64(ch[1]), Convert.ToInt64(ch[0]), changeSet,
                                    ServiceContractPhotocopier.Classes.ScpContractAudit.SOURCE_STRATEGY_APPLY,
                                    "STRATEGY." + (string)ch[5],
                                    ((decimal)ch[3]).ToString("0.######"), ((decimal)ch[4]).ToString("0.######"));
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Apply failed:\r\n" + ex.Message, "Error"); return; }

            // Reload so the in-memory items match the DB (a later Save must not overwrite the apply).
            LoadItems();
            RebuildItemsView();
            BindItemMeterPanel();
            RefreshChangeHistoryTab();
            XtraMessageBox.Show(changes.Count + " meter field(s) updated and audited.\r\n\r\n" +
                "These values now live in the meter grid (Min Charges / Rebate % / Free Qty). If a machine " +
                "needs a special value, edit it there — that is what actually bills.", "Apply Strategy",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // A rule's Scope vs a meter's role: "BK"/"CL" targets that role only; ""/"BKCL" = both.
        private static bool ScopeMatchesRole(string scope, string role)
        {
            if (scope == "BK") return role == "BK";
            if (scope == "CL") return role == "CL";
            return true;
        }

        // Stages one meter-field change for Apply Strategy, keyed by (ItemMeterKey|column) so a later rule
        // overrides an earlier one on the same field; the old value is always the original DB value.
        private static void StageChange(System.Collections.Generic.Dictionary<string, object[]> map,
            DataRow m, string column, string fieldLabel, decimal oldVal, decimal newVal)
        {
            string key = Convert.ToString(m["ItemMeterKey"]) + "|" + column;
            map[key] = new object[] { m["ItemMeterKey"], m["ItemKey"], column, oldVal, newVal, fieldLabel,
                Convert.ToString(m["ServiceItemNo"]), Convert.ToString(m["MeterTypeCode"]) };
        }

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
            ChkMonthEnd.CheckedChanged += bd;
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
            SluStrategy.EditValueChanged += h;
            SluStrategy.EditValueChanged += new EventHandler(SluStrategy_EditValueChanged);
            ChkRentalSeparate.EditValueChanged += h;
        }

        // CLAUDE.md rule 8: mirror AutoCount's create/edit behaviour — closing with unsaved changes
        // prompts for confirmation. A successful Save clears the flag so it closes silently.
        private void OnFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (_savedOk || !_dirty) return;
            DialogResult r = XtraMessageBox.Show(
                "You have unsaved changes. Discard them and close?", "Unsaved Changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) e.Cancel = true;
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
            SluStrategy.Properties.DataSource = _strategyLookup;
            SluStrategy.Properties.ValueMember = "StrategyCode";
            SluStrategy.Properties.DisplayMember = "StrategyCode";
            SluStrategyView.OptionsBehavior.AutoPopulateColumns = false;
            SluStrategyView.Columns.Clear();
            DevExpress.XtraGrid.Columns.GridColumn sc = SluStrategyView.Columns.AddVisible("StrategyCode");
            sc.Caption = "Strategy"; sc.Width = 110;
            DevExpress.XtraGrid.Columns.GridColumn sd = SluStrategyView.Columns.AddVisible("Description");
            sd.Caption = "Description"; sd.Width = 240;
            DevExpress.XtraGrid.Columns.GridColumn st = SluStrategyView.Columns.AddVisible("StrategyType");
            st.Caption = "Kind"; st.Width = 110;
            SluStrategyView.OptionsView.ShowAutoFilterRow = true;
        }

        // "+" on the Strategy dropdown: open Strategy Maintenance; on close refresh + select the
        // last-saved code (SluContractType "+" pattern).
        private void SluStrategy_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.StrategyLst_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.StrategyLst_Form(_db))
                {
                    f.WindowState = System.Windows.Forms.FormWindowState.Normal;
                    f.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                    LoadStrategyLookup();
                    if (!string.IsNullOrEmpty(f.SavedCode)) SluStrategy.EditValue = f.SavedCode;
                }
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Open Strategy Maintenance failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Picking a NET-billing strategy changes the FOC/rebate money math — say so once, clearly.
        private void SluStrategy_EditValueChanged(object sender, EventArgs e)
        {
            if (_loading || _strategyLookup == null) return;
            string code = SluStrategy.EditValue == null ? "" : SluStrategy.EditValue.ToString().Trim();
            if (code.Length == 0) return;
            DataRow[] f = _strategyLookup.Select("StrategyCode='" + code.Replace("'", "''") + "'");
            if (f.Length > 0 && Convert.ToString(f[0]["NetBilling"]) == "Y")
                XtraMessageBox.Show(
                    "Strategy '" + code + "' uses NET billing:\r\n\r\n" +
                    "billed copies = usage − FOC − (usage − FOC) × rebate%\r\n\r\n" +
                    "Contracts WITHOUT this strategy keep the current convention (bill RAW usage; FOC/rebate " +
                    "shown on the invoice but not deducted).",
                    "NET Billing Strategy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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

        // Show the AutoCount home/default currency symbol (e.g. "RM") behind the Contract Value box.
        // Authoritative source: AutoCount stores it in dbo.Settings (Name='General') as JSON
        // (LocalCurrencySymbol / LocalCurrencyCode). Falls back to the rate=1.0 currency if unavailable.
        private void LoadCurrencyLabel()
        {
            try
            {
                object o = _db.ExecuteScalar(
                    "SELECT ISNULL(NULLIF(JSON_VALUE(Value,'$.LocalCurrencySymbol'),''), " +
                    "JSON_VALUE(Value,'$.LocalCurrencyCode')) FROM [dbo].[Settings] WHERE Name = 'General'");
                if (o == null || o == DBNull.Value || o.ToString().Trim().Length == 0)
                    o = _db.ExecuteScalar(
                        "SELECT TOP 1 ISNULL(NULLIF(LTRIM(RTRIM(CurrencySymbol)),''), CurrencyCode) " +
                        "FROM [dbo].[Currency] WHERE BankBuyRate = 1 ORDER BY CurrencyCode");
                LblCurrency.Text = (o == null || o == DBNull.Value) ? "" : o.ToString();
            }
            catch { LblCurrency.Text = ""; }
        }

        // "Last day of month": disable the day spin and use month-end (stored as day 31 -> the existing
        // month-end clamp yields the true last day for every month).
        private void ChkMonthEnd_CheckedChanged(object sender, EventArgs e)
        {
            SpnBillingDay.Enabled = !ChkMonthEnd.Checked;
            if (ChkMonthEnd.Checked) SpnBillingDay.Value = 31;
            if (!_loading) _dirty = true;
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
            SpnBillingDay.Value = AsInt(r["BillingDay"], 1);
            ChkMonthEnd.Checked = r.Table.Columns.Contains("BillOnMonthEnd") && AsStr(r["BillOnMonthEnd"]) == "Y";
            SpnBillingDay.Enabled = !ChkMonthEnd.Checked;
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
            SluStrategy.EditValue = r.Table.Columns.Contains("StrategyCode") ? SetOrNull(AsStr(r["StrategyCode"])) : null;
            ChkRentalSeparate.Checked = r.Table.Columns.Contains("RentalSeparateInvoice") && AsStr(r["RentalSeparateInvoice"]) == "Y";
            if (_cmbFocReset != null)
            {
                string fru = r.Table.Columns.Contains("FOCResetUnit") ? AsStr(r["FOCResetUnit"]) : "M";
                _cmbFocReset.SelectedIndex = fru == "W" ? 1 : (fru == "D" ? 2 : 0);
                if (r.Table.Columns.Contains("FOCResetN") && AsInt(r["FOCResetN"], 0) > 0) _spnFocResetN.EditValue = AsInt(r["FOCResetN"], 3);
                _spnFocResetN.Visible = _cmbFocReset.SelectedIndex == 2;
            }

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
                "history) and can be attached to a contract again later.",
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
            _dtItemsView.Columns.Add("Expiry", typeof(DateTime));

            int n = 1;
            foreach (ItemEditData d in _items)
            {
                DataRow r = _dtItemsView.NewRow();
                r["No"] = n++;
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
                    : (ChkMonthEnd.Checked ? "31" : ((int)SpnBillingDay.Value).ToString());
                r["ServiceStart"] = (object)d.ServiceStartDate ?? DBNull.Value;
                r["BKMeter"] = MeterForRole(d, "BK");
                r["CLMeter"] = MeterForRole(d, "CL");
                r["Description"] = d.Description ?? "";
                r["Inactive"] = d.Inactive ? "Y" : "N";
                r["Expiry"] = (object)d.ServiceExpiryDate ?? DBNull.Value;
                _dtItemsView.Rows.Add(r);
            }
            GridItems.DataSource = _dtItemsView;
            GridViewItems.BestFitColumns();
            RefreshOrphanLookup();   // keep the inline attach list current after add/detach/swap
            BindItemMeterPanel();    // rebind: the selected item's Meters table may have been replaced
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

        private DevExpress.XtraEditors.GroupControl _grpMeterCfg;
        private DevExpress.XtraGrid.GridControl _gridMeterCfg;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewMeterCfg;
        private DevExpress.XtraEditors.SimpleButton _btnMeterCfgAdd;
        private DevExpress.XtraEditors.SimpleButton _btnMeterCfgDel;
        private DevExpress.XtraEditors.SimpleButton _btnMeterCfgMaint;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _meterCfgRepoType;
        private DataTable _meterCfgLookup;
        private ItemEditData _meterCfgItem;   // the item currently bound in the panel

        private void BuildItemMeterPanel()
        {
            _grpMeterCfg = new DevExpress.XtraEditors.GroupControl();
            _grpMeterCfg.Text = "Meter Configuration";
            _grpMeterCfg.Dock = System.Windows.Forms.DockStyle.Bottom;
            _grpMeterCfg.Height = 250;
            PageItems.Controls.Add(_grpMeterCfg);

            _gridMeterCfg = new DevExpress.XtraGrid.GridControl();
            _viewMeterCfg = new DevExpress.XtraGrid.Views.Grid.GridView();
            _gridMeterCfg.MainView = _viewMeterCfg;
            _viewMeterCfg.GridControl = _gridMeterCfg;
            _gridMeterCfg.Dock = System.Windows.Forms.DockStyle.Fill;
            _grpMeterCfg.Controls.Add(_gridMeterCfg);

            System.Windows.Forms.Panel bar = new System.Windows.Forms.Panel();
            bar.Dock = System.Windows.Forms.DockStyle.Top;
            bar.Height = 30;
            _grpMeterCfg.Controls.Add(bar);

            _btnMeterCfgAdd = new DevExpress.XtraEditors.SimpleButton();
            _btnMeterCfgAdd.Text = "+";
            _btnMeterCfgAdd.Location = new System.Drawing.Point(4, 3);
            _btnMeterCfgAdd.Size = new System.Drawing.Size(28, 24);
            _btnMeterCfgAdd.Click += new EventHandler(BtnMeterCfgAdd_Click);
            bar.Controls.Add(_btnMeterCfgAdd);

            _btnMeterCfgDel = new DevExpress.XtraEditors.SimpleButton();
            _btnMeterCfgDel.Text = "-";
            _btnMeterCfgDel.Location = new System.Drawing.Point(36, 3);
            _btnMeterCfgDel.Size = new System.Drawing.Size(28, 24);
            _btnMeterCfgDel.Click += new EventHandler(BtnMeterCfgDel_Click);
            bar.Controls.Add(_btnMeterCfgDel);

            _btnMeterCfgMaint = new DevExpress.XtraEditors.SimpleButton();
            _btnMeterCfgMaint.Text = "Maintain Meter Type";
            _btnMeterCfgMaint.ImageOptions.ImageUri.Uri = "Edit;Size16x16";
            _btnMeterCfgMaint.Location = new System.Drawing.Point(72, 3);
            _btnMeterCfgMaint.Size = new System.Drawing.Size(150, 24);
            _btnMeterCfgMaint.ToolTip = "Open the Meter Type maintenance module. New / edited types are pickable here as soon as it closes.";
            _btnMeterCfgMaint.Click += new EventHandler(BtnMeterCfgMaint_Click);
            bar.Controls.Add(_btnMeterCfgMaint);

            DevExpress.XtraEditors.LabelControl hint = new DevExpress.XtraEditors.LabelControl();
            hint.Text = "Meters of the selected service item. Pick a Meter Type — pricing fills from the type and can be overridden per machine. Saved together with the contract.";
            hint.Location = new System.Drawing.Point(232, 8);
            bar.Controls.Add(hint);

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
            _gridMeterCfg.RepositoryItems.Add(repoType);
            _meterCfgRepoType = repoType;   // kept so "Maintain Meter Type" can refresh its datasource

            DevExpress.XtraEditors.Repository.RepositoryItemComboBox repoRole =
                new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            repoRole.Items.AddRange(new object[] { "BK", "CL", "NA" });
            repoRole.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _gridMeterCfg.RepositoryItems.Add(repoRole);

            _viewMeterCfg.OptionsBehavior.AutoPopulateColumns = false;
            _viewMeterCfg.OptionsView.ShowGroupPanel = false;
            // First column: which service item these meters belong to (unbound — the panel always
            // shows ONE item's meters, so every row carries that item's number).
            DevExpress.XtraGrid.Columns.GridColumn colSi = _viewMeterCfg.Columns.AddVisible("PanelServiceItemNo");
            colSi.Caption = "Service Item No";
            colSi.UnboundDataType = typeof(string);
            colSi.OptionsColumn.AllowEdit = false;
            colSi.Width = 120;
            _viewMeterCfg.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(ViewMeterCfg_UnboundData);
            MeterCfgCol("MeterTypeCode", "Meter Type", 140, repoType);
            MeterCfgCol("Description", "Meter Description", 220, null);   // defaults from the type; editable per machine
            MeterCfgCol("MeterRole", "Role (BK/CL)", 80, repoRole);
            MeterCfgCol("MinimumCharges", "Min Charges", 90, null);
            MeterCfgCol("ChargesRate", "Rate", 80, null);
            MeterCfgCol("MeterMultiPriceCode", "Multi-Price", 100, null);
            MeterCfgCol("RebateQtyInPercent", "Rebate %", 80, null);
            MeterCfgCol("FOCQty", "Free Qty", 80, null);
            MeterCfgCol("InitialReading", "Initial Reading", 100, null);
            _viewMeterCfg.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(ViewMeterCfg_CellValueChanged);
            _viewMeterCfg.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(ViewMeterCfg_RowCellStyle);

            GridViewItems.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewItems_FocusedRowChangedMeterCfg);
        }

        private DevExpress.XtraGrid.Columns.GridColumn MeterCfgCol(string field, string caption, int width,
            DevExpress.XtraEditors.Repository.RepositoryItem edit)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = _viewMeterCfg.Columns.AddVisible(field);
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
                    "MeterMultiPriceCode, RebateQtyInPercent, FOCQty, ISNULL(IsFlatCharge,'N') AS IsFlatCharge " +
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
                if (_viewMeterCfg != null) _viewMeterCfg.RefreshData();
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
            if (_grpMeterCfg == null) return;
            ItemEditData d = FocusedItemData();
            _meterCfgItem = d;
            if (d == null)
            {
                _grpMeterCfg.Text = "Meter Configuration  (select a service item above)";
                _gridMeterCfg.DataSource = null;
                _btnMeterCfgAdd.Enabled = false;
                _btnMeterCfgDel.Enabled = false;
                return;
            }
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            if (!d.Meters.Columns.Contains("MachineSerialNo")) d.Meters.Columns.Add("MachineSerialNo", typeof(string));
            if (!d.Meters.Columns.Contains("Description")) d.Meters.Columns.Add("Description", typeof(string));
            // Meters may only be ADDED once the service item is saved (has a real number) — otherwise
            // there is no Service Item No to tell which meter belongs to which machine.
            bool saved = d.ItemKey > 0;
            _grpMeterCfg.Text = saved
                ? "Meter Configuration — " + d.ServiceItemNo
                : "Meter Configuration — <NEW>  (save the contract first, then add meters here)";
            _gridMeterCfg.DataSource = d.Meters;
            _btnMeterCfgAdd.Enabled = saved;
            _btnMeterCfgDel.Enabled = true;
        }

        private void ViewMeterCfg_UnboundData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName != "PanelServiceItemNo" || !e.IsGetData) return;
            e.Value = _meterCfgItem == null ? ""
                : (string.IsNullOrEmpty(_meterCfgItem.ServiceItemNo) ? "<NEW>" : _meterCfgItem.ServiceItemNo);
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
            if (d.Meters == null) { d.Meters = zSCP2_Item_Form.CreateMetersTable(); _gridMeterCfg.DataSource = d.Meters; }
            DataRow r = d.Meters.NewRow();
            r["MeterRole"] = "NA";
            r["Description"] = "";
            r["MachineSerialNo"] = "";
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            d.Meters.Rows.Add(r);
            _viewMeterCfg.FocusedRowHandle = _viewMeterCfg.RowCount - 1;
            _dirty = true;
        }

        private void BtnMeterCfgDel_Click(object sender, EventArgs e)
        {
            int rh = _viewMeterCfg.FocusedRowHandle;
            if (rh < 0 || _meterCfgItem == null) return;
            // Removing a saved meter also removes its reading history on save (cascade) — make sure.
            string code = Convert.ToString(_viewMeterCfg.GetRowCellValue(rh, "MeterTypeCode"));
            if (_meterCfgItem.ItemKey > 0 && !string.IsNullOrEmpty(code))
            {
                DialogResult ok = XtraMessageBox.Show(
                    "Remove meter '" + code + "' from " + _meterCfgItem.ServiceItemNo + "?\r\n\r\n" +
                    "When the contract is saved, this meter AND its reading/invoice history are deleted permanently.",
                    "Remove Meter", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (ok != DialogResult.Yes) return;
            }
            _viewMeterCfg.DeleteRow(rh);
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

        private void ViewMeterCfg_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            string type = Convert.ToString(_viewMeterCfg.GetRowCellValue(e.RowHandle, "MeterTypeCode"));
            if (!IsFlatType(type)) return;
            e.Appearance.BackColor = _rentalRowColor;
            e.Appearance.Options.UseBackColor = true;
        }

        // Picking a Meter Type pre-fills its pricing (same behaviour as the item dialog); any edit dirties.
        private void ViewMeterCfg_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column == null || e.Column.FieldName != "MeterTypeCode" || _meterCfgLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _meterCfgLookup.Select("MeterTypeCode='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow m = found[0];
            int rh = e.RowHandle;
            // Description defaults from the type ONLY when the cell is still blank — never clobber a
            // description the user already keyed for this machine.
            object curDesc = _viewMeterCfg.GetRowCellValue(rh, "Description");
            if (curDesc == null || curDesc == DBNull.Value || curDesc.ToString().Trim().Length == 0)
                _viewMeterCfg.SetRowCellValue(rh, "Description", m.Table.Columns.Contains("Description") ? m["Description"] : "");
            _viewMeterCfg.SetRowCellValue(rh, "MinimumCharges", m["MinimumCharges"]);
            _viewMeterCfg.SetRowCellValue(rh, "ChargesRate", m["ChargesRate"]);
            _viewMeterCfg.SetRowCellValue(rh, "MeterMultiPriceCode", m["MeterMultiPriceCode"]);
            _viewMeterCfg.SetRowCellValue(rh, "RebateQtyInPercent", m["RebateQtyInPercent"]);
            _viewMeterCfg.SetRowCellValue(rh, "FOCQty", m["FOCQty"]);
            // Default the BK/CL role from the meter type name when the role is still unset — a usage
            // meter with no role would be skipped by the Meter Reading fetch (only BK/CL get readings).
            object curRole = _viewMeterCfg.GetRowCellValue(rh, "MeterRole");
            string curRoleS = curRole == null ? "" : curRole.ToString().Trim().ToUpperInvariant();
            if (curRoleS == "" || curRoleS == "NA")
            {
                string inferred = InferMeterRole(code, m.Table.Columns.Contains("Description") ? Convert.ToString(m["Description"]) : "");
                if (inferred.Length > 0) _viewMeterCfg.SetRowCellValue(rh, "MeterRole", inferred);
            }
        }

        // Infer BK (black) / CL (colour) from a meter type's code + description. Blank = leave as-is
        // (rental / minimum / fax types have no colour role).
        internal static string InferMeterRole(string code, string desc)
        {
            string u = ((code ?? "") + " " + (desc ?? "")).ToUpperInvariant();
            if (u.Contains("COLOR") || u.Contains("COLOUR") || u.Contains(".CL.") || u.Contains("-CL")) return "CL";
            if (u.Contains(".BK.") || u.Contains("-BK") || u.Contains(" BK") || u.Contains("BLACK")) return "BK";
            return "";
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
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
        private DevExpress.XtraEditors.SimpleButton _btnItemQuickAdd;

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

            _btnItemQuickAdd = new DevExpress.XtraEditors.SimpleButton();
            _btnItemQuickAdd.Text = "Quick Add Row";
            _btnItemQuickAdd.ImageOptions.ImageUri.Uri = "Add;Size16x16";
            _btnItemQuickAdd.Location = new System.Drawing.Point(6, 5);
            _btnItemQuickAdd.Size = new System.Drawing.Size(130, 24);
            _btnItemQuickAdd.ToolTip = "Add a blank service item (CSSI) and key it in directly in the grid. The number is drawn on Save.";
            _btnItemQuickAdd.Click += new EventHandler(BtnItemQuickAdd_Click);
            PnlItemBar.Controls.Add(_btnItemQuickAdd);
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
                case "BillingDay":
                {
                    int bd;
                    d.BillingDayOverride = (int.TryParse(s, out bd) && bd >= 1 && bd <= 31) ? (int?)bd : null;
                    break;
                }
                case "ServiceStart":
                    d.ServiceStartDate = (e.Value == null || e.Value == DBNull.Value)
                        ? (DateTime?)null : Convert.ToDateTime(e.Value);
                    break;
                case "Expiry":
                    d.ServiceExpiryDate = (e.Value == null || e.Value == DBNull.Value)
                        ? (DateTime?)null : Convert.ToDateTime(e.Value);
                    break;
                default: return;
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

        private void BuildMoreHeaderTab()
        {
            // Top block: two columns of contact fields.
            MhField("City", "City", 12, 14, 200);
            MhField("PostalCode", "Postal Code", 430, 14, 200);
            MhField("State", "State", 12, 40, 200);
            MhField("Country", "Country", 430, 40, 200);
            MhField("Fax", "Fax", 12, 66, 200);
            MhField("Ref1", "Ref 1", 430, 66, 200);
            MhField("Ref2", "Ref 2", 12, 92, 200);
            MhField("Ref3", "Ref 3", 430, 92, 200);
            MhField("Ref4", "Ref 4", 12, 118, 200);

            // Delivery Address group.
            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            grp.Text = "Delivery Address";
            grp.Location = new System.Drawing.Point(12, 150);
            grp.Size = new System.Drawing.Size(820, 210);
            PageMoreHeader.Controls.Add(grp);

            // Search = pick one of the customer's branches; Copy = copy the main contract address here.
            DevExpress.XtraEditors.SimpleButton btnSearch = new DevExpress.XtraEditors.SimpleButton();
            btnSearch.Text = "Search"; btnSearch.Location = new System.Drawing.Point(300, 27); btnSearch.Size = new System.Drawing.Size(60, 22);
            btnSearch.Click += new EventHandler(DelSearch_Click);
            grp.Controls.Add(btnSearch);
            DevExpress.XtraEditors.SimpleButton btnCopy = new DevExpress.XtraEditors.SimpleButton();
            btnCopy.Text = "Copy"; btnCopy.Location = new System.Drawing.Point(364, 27); btnCopy.Size = new System.Drawing.Size(55, 22);
            btnCopy.Click += new EventHandler(DelCopy_Click);
            grp.Controls.Add(btnCopy);

            MhFieldIn(grp, "DelBranchCode", "Branch Code", 10, 28, 180);
            MhFieldIn(grp, "DelState", "State", 430, 28, 180);
            MhFieldIn(grp, "DelBranchName", "Branch Name", 10, 54, 180);
            MhFieldIn(grp, "DelCountry", "Country", 430, 54, 180);

            DevExpress.XtraEditors.LabelControl lblAddr = new DevExpress.XtraEditors.LabelControl();
            lblAddr.Text = "Address"; lblAddr.Location = new System.Drawing.Point(10, 83);
            grp.Controls.Add(lblAddr);
            _mhDelAddress = new DevExpress.XtraEditors.MemoEdit();
            _mhDelAddress.Location = new System.Drawing.Point(110, 80);
            _mhDelAddress.Size = new System.Drawing.Size(200, 60);
            _mhDelAddress.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            grp.Controls.Add(_mhDelAddress);

            MhFieldIn(grp, "DelPhone", "Phone", 430, 83, 180);
            MhFieldIn(grp, "DelFax", "Fax", 430, 109, 180);
            MhFieldIn(grp, "DelEmail", "Email", 430, 135, 180);
            MhFieldIn(grp, "DelContactPerson", "Contact Person", 430, 161, 180);
            MhFieldIn(grp, "DelCity", "City", 10, 150, 180);
            MhFieldIn(grp, "DelPostalCode", "Postal Code", 10, 176, 180);
        }

        private void MhField(string col, string caption, int x, int y, int width)
        {
            MhFieldOn(PageMoreHeader, col, caption, x, y, width);
        }
        private void MhFieldIn(DevExpress.XtraEditors.GroupControl grp, string col, string caption, int x, int y, int width)
        {
            MhFieldOn(grp, col, caption, x, y, width);
        }
        private void MhFieldOn(System.Windows.Forms.Control parent, string col, string caption, int x, int y, int width)
        {
            DevExpress.XtraEditors.LabelControl lbl = new DevExpress.XtraEditors.LabelControl();
            lbl.Text = caption; lbl.Location = new System.Drawing.Point(x, y + 3);
            parent.Controls.Add(lbl);
            DevExpress.XtraEditors.TextEdit ed = new DevExpress.XtraEditors.TextEdit();
            ed.Location = new System.Drawing.Point(x + 98, y);
            ed.Size = new System.Drawing.Size(width, 20);
            ed.EditValueChanged += delegate { if (!_loading) _dirty = true; };
            parent.Controls.Add(ed);
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

        // ===================== Strategy tab (this contract's OWN strategy rules) =====================
        // The contract's editable COPY of strategy rules (zSCP2_ContractStrategyRule). Seeded from a master
        // template by "Copy Rules from Strategy Template", then edited here WITHOUT touching the template.
        // The billing pipeline + "Apply Strategy to Meters" read these rows. Inserted as the 2nd tab.
        private DevExpress.XtraTab.XtraTabPage _pgStrategy;
        private ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl _strategyEditor;
        private DevExpress.XtraEditors.SimpleButton _btnCopyTemplate;
        private DevExpress.XtraEditors.SimpleButton _btnApplyMeters;
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

            // Move the strategy TEMPLATE picker + "Rental separate invoice" off the header into this tab —
            // strategy lives here now. Reparent the existing controls (keeps their save/load/dirty wiring).
            if (LblStrategy != null && LblStrategy.Parent != null) LblStrategy.Parent.Controls.Remove(LblStrategy);
            if (SluStrategy != null && SluStrategy.Parent != null) SluStrategy.Parent.Controls.Remove(SluStrategy);
            if (ChkRentalSeparate != null && ChkRentalSeparate.Parent != null) ChkRentalSeparate.Parent.Controls.Remove(ChkRentalSeparate);

            if (LblStrategy != null) { LblStrategy.Text = "Strategy Template"; LblStrategy.Location = new System.Drawing.Point(10, 13); top.Controls.Add(LblStrategy); }
            if (SluStrategy != null) { SluStrategy.Location = new System.Drawing.Point(115, 9); SluStrategy.Width = 200; top.Controls.Add(SluStrategy); }

            _btnCopyTemplate = new DevExpress.XtraEditors.SimpleButton();
            _btnCopyTemplate.Text = "Copy Rules from Template";
            _btnCopyTemplate.Location = new System.Drawing.Point(325, 8);
            _btnCopyTemplate.Width = 190; _btnCopyTemplate.Height = 26;
            _btnCopyTemplate.Click += new EventHandler(BtnCopyTemplate_Click);
            top.Controls.Add(_btnCopyTemplate);

            // Fills/overrides the BK/CL (+ rental) meters from these rules, per each rule's scope. Save first.
            _btnApplyMeters = new DevExpress.XtraEditors.SimpleButton();
            _btnApplyMeters.Text = "Apply to Meters (override BK/CL)";
            _btnApplyMeters.Location = new System.Drawing.Point(520, 8);
            _btnApplyMeters.Width = 220; _btnApplyMeters.Height = 26;
            _btnApplyMeters.Click += new EventHandler(delegate { ApplyStrategyToMeters(); });
            top.Controls.Add(_btnApplyMeters);

            if (ChkRentalSeparate != null) { ChkRentalSeparate.Location = new System.Drawing.Point(760, 12); top.Controls.Add(ChkRentalSeparate); }

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
            _lblStrategyTabHint.Text = "FOC/rebate allowance refreshes every reset period (default Monthly). Copy from a Template, edit rules, Save, then Apply to Meters.";
            _lblStrategyTabHint.Location = new System.Drawing.Point(290, 49);
            top.Controls.Add(_lblStrategyTabHint);

            // Add the Fill control first, then the Top bar, so docking lays them out without overlap.
            _pgStrategy.Controls.Add(_strategyEditor);
            _pgStrategy.Controls.Add(top);
            TabMain.TabPages.Insert(1, _pgStrategy);   // second tab, right after "Service Item Under Contract"

            _strategyEditor.SetEditable(true);
            RefreshStrategyTab();
        }

        // Feed the control this contract's saved service items + load its own rules from the DB.
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
            _strategyEditor.LoadRules(ServiceContractPhotocopier.Classes.ScpStrategy.LoadContractRules(_db, _contractKey));
        }

        private void CmbFocReset_Changed(object sender, EventArgs e)
        {
            if (_spnFocResetN != null) _spnFocResetN.Visible = _cmbFocReset.SelectedIndex == 2;   // Every N days
            if (!_loading) _dirty = true;
        }

        private void BtnCopyTemplate_Click(object sender, EventArgs e)
        {
            string code = SluStrategy.EditValue == null ? "" : SluStrategy.EditValue.ToString().Trim();
            if (code.Length == 0)
            { XtraMessageBox.Show("Pick a Strategy on the header first, then click Copy.", "Copy Strategy Template"); return; }
            ServiceContractPhotocopier.Classes.StrategyDef def =
                ServiceContractPhotocopier.Classes.ScpStrategy.LoadByCode(_db, code);
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
            string seed = SluStrategy.EditValue == null ? "" : SluStrategy.EditValue.ToString().Trim();
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
            _pgBillingHist = new DevExpress.XtraTab.XtraTabPage();
            _pgBillingHist.Text = "Billing History";
            TabMain.TabPages.Add(_pgBillingHist);
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
        private DevExpress.XtraGrid.GridControl _gridChangeHist;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewChangeHist;

        private void BuildChangeHistoryTab()
        {
            _pgChangeHist = new DevExpress.XtraTab.XtraTabPage();
            _pgChangeHist.Text = "Change History";
            TabMain.TabPages.Add(_pgChangeHist);

            _gridChangeHist = new DevExpress.XtraGrid.GridControl();
            _viewChangeHist = new DevExpress.XtraGrid.Views.Grid.GridView();
            _gridChangeHist.MainView = _viewChangeHist;
            _viewChangeHist.GridControl = _gridChangeHist;
            _gridChangeHist.Dock = System.Windows.Forms.DockStyle.Fill;
            _viewChangeHist.OptionsBehavior.Editable = false;
            _viewChangeHist.OptionsView.ShowGroupPanel = false;
            _viewChangeHist.OptionsView.ShowAutoFilterRow = true;
            _pgChangeHist.Controls.Add(_gridChangeHist);
            RefreshChangeHistoryTab();
        }

        private void RefreshChangeHistoryTab()
        {
            if (_gridChangeHist == null) return;
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
            _gridChangeHist.DataSource = dt;
            _viewChangeHist.PopulateColumns();
            if (_viewChangeHist.Columns["When"] != null)
            {
                _viewChangeHist.Columns["When"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                _viewChangeHist.Columns["When"].DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
                _viewChangeHist.Columns["When"].Width = 130;
            }
            _viewChangeHist.BestFitColumns();
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
            if (_viewMeterCfg != null)
            {
                _viewMeterCfg.PostEditor();
                _viewMeterCfg.CloseEditor();
                _viewMeterCfg.UpdateCurrentRow();
            }
            if (string.IsNullOrWhiteSpace(TxtContractNo.Text))
            { XtraMessageBox.Show("Contract No is required.", "Validation"); return; }
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (string.IsNullOrWhiteSpace(debtor))
            { XtraMessageBox.Show("Customer (Debtor) is required.", "Validation"); return; }

            // Service Expiry (end) date cannot be earlier than the Service Start date.
            if (DtStartDate.EditValue != null && DtStartDate.EditValue != DBNull.Value &&
                DtExpiryDate.EditValue != null && DtExpiryDate.EditValue != DBNull.Value &&
                Convert.ToDateTime(DtExpiryDate.EditValue).Date < Convert.ToDateTime(DtStartDate.EditValue).Date)
            { XtraMessageBox.Show("Service Expiry (To) date cannot be earlier than the Service Start date.", "Validation"); return; }

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
                                }
                        }

                        int pos = 0;
                        foreach (ItemEditData d in _items)
                        {
                            long itemKey;
                            if (d.ItemKey > 0)
                            {
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
                            zSCP2_Item_Form.PersistItemExtras(conn, tx, d, itemKey);
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
                    XtraMessageBox.Show("Contract saved. You can now add service items.", "Saved",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    XtraMessageBox.Show("Contract saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _savedOk = true;   // skip the unsaved-changes prompt on the close that follows
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertContract(SqlConnection conn, SqlTransaction tx, string debtor)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Contract] " +
                "(ContractNo, ContractTypeCode, DebtorCode, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
                " ContractValue, BillingDay, BillOnMonthEnd, BillingMode, Address1, Attention, Phone, TermCode, AreaCode, StaffCode, " +
                " ReferenceNo, Description, Remark1, Remark2, Note, DeptNo, ProjNo, StrategyCode, RentalSeparateInvoice, FOCResetUnit, FOCResetN, Inactive, Created, LastModified) " +
                "VALUES (@no,@type,@debtor,@cdate,@sdate,@edate,@val,@bday,@monthend,@bmode,@addr,@attn,@phone,@term,@area,@staff," +
                "@refno,@desc,@r1,@r2,@note,@dept,@proj,@strategy,@rentsep,@focresetunit,@focresetn,@inact,GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);";
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
                "DeptNo=@dept, ProjNo=@proj, StrategyCode=@strategy, RentalSeparateInvoice=@rentsep, " +
                "FOCResetUnit=@focresetunit, FOCResetN=@focresetn, " +
                "Inactive=@inact, Modified=GETDATE(), LastModified=GETDATE() WHERE ContractKey=@ck";
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
            // Bill-on-month-end: store day 31 so the existing month-end clamp bills the true last day
            // of every month (31 -> 30/29/28 on short months, 31 on long ones).
            cmd.Parameters.AddWithValue("@bday", (byte)(ChkMonthEnd.Checked ? 31 : (int)SpnBillingDay.Value));
            cmd.Parameters.AddWithValue("@monthend", ChkMonthEnd.Checked ? "Y" : "N");
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
            cmd.Parameters.AddWithValue("@strategy", SluStrategy.EditValue == null ? "" : SluStrategy.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@rentsep", ChkRentalSeparate.Checked ? "Y" : "N");
            string focResetUnit = _cmbFocReset != null && _cmbFocReset.SelectedIndex == 1 ? "W"
                : (_cmbFocReset != null && _cmbFocReset.SelectedIndex == 2 ? "D" : "M");
            cmd.Parameters.AddWithValue("@focresetunit", focResetUnit);
            cmd.Parameters.AddWithValue("@focresetn", focResetUnit == "D" && _spnFocResetN != null ? Convert.ToInt32(_spnFocResetN.Value) : 0);
            cmd.Parameters.AddWithValue("@inact", ChkInactive.Checked ? "Y" : "N");
        }

        private long InsertItem(SqlConnection conn, SqlTransaction tx, ItemEditData d, int pos)
        {
            string sql =
                "INSERT INTO [dbo].[zSCP2_Item] " +
                "(ContractKey, ServiceItemNo, SerialNumber, Description, BillingDayOverride, " +
                " DepartmentCode, JobCode, StockLocationCode, Pos, Inactive, LastModified) " +
                "VALUES (@ck,@no,@serial,@desc,@bday,@dept,@job,@loc,@pos,@inact,GETDATE()); " +
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
                "StockLocationCode=@loc, Pos=@pos, Inactive=@inact, LastModified=GETDATE() WHERE ItemKey=@ik";
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
                    " RebateQtyInPercent, FOCQty, InitialReading, LastModified) " +
                    "VALUES (@ik,@code,@desc,@role,@mser,@min,@rate,@multi,@rebate,@foc,@init,GETDATE());";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@desc", r.Table.Columns.Contains("Description") && r["Description"] != DBNull.Value
                        ? (object)r["Description"].ToString() : "");
                    string role = r["MeterRole"] == null ? "NA" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    if (role != "BK" && role != "CL") role = "NA";
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@mser", r.Table.Columns.Contains("MachineSerialNo") && r["MachineSerialNo"] != DBNull.Value
                        ? ((string)r["MachineSerialNo"]).Trim() : "");
                    cmd.Parameters.AddWithValue("@min", AsDec(r["MinimumCharges"]));
                    cmd.Parameters.AddWithValue("@rate", AsDec(r["ChargesRate"]));
                    cmd.Parameters.AddWithValue("@multi", r["MeterMultiPriceCode"] == null ? "" : r["MeterMultiPriceCode"].ToString());
                    cmd.Parameters.AddWithValue("@rebate", AsDec(r["RebateQtyInPercent"]));
                    cmd.Parameters.AddWithValue("@foc", AsDec(r["FOCQty"]));
                    cmd.Parameters.AddWithValue("@init", AsDec(r["InitialReading"]));
                    cmd.ExecuteNonQuery();
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
            DtContractDate.EditValue = cd;
            DtStartDate.EditValue = cd;
            DtExpiryDate.EditValue = cd.AddMonths(_rng.Next(6, 36));
            SpnContractValue.Value = _rng.Next(500, 50000);
            ChkMonthEnd.Checked = false;
            SpnBillingDay.Value = _rng.Next(1, 28);
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

        private const string CLIP_HEADER = "ATP-SCP-DOC-V1";

        // "Copy from other Service Contract": pick a saved contract and load its content into THIS
        // (usually new) contract as a template — header + items (as fresh copies) + spare parts.
        private void barCopyFrom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
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
            LoadContractAsTemplate(key);
            _dirty = true;
            XtraMessageBox.Show("Copied. Review the details and Save to create this contract.", "Copied",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // "Copy to a new Service Contract": open a NEW contract editor pre-filled from the current one.
        private void barCopyToNew_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_isNew && _contractKey == 0)
            { XtraMessageBox.Show("Save this contract first before copying it to a new one.", "Copy to new"); return; }
            using (zSCP2_Contract_Form f = new zSCP2_Contract_Form(_db, _contractKey, true))
            { f.ShowDialog(this); }
        }

        // Serialize header + items to the clipboard (custom tagged text).
        private void barCopyWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(CLIP_HEADER);
            sb.Append("H\t")
              .Append(Cv(SluContractType)).Append('\t').Append(Cv(LkDebtorCode)).Append('\t')
              .Append(TxtAddress.Text.Replace("\r", " ").Replace("\n", " ")).Append('\t')
              .Append(TxtAttention.Text).Append('\t').Append(TxtPhone.Text).Append('\t')
              .Append(TxtTerm.Text).Append('\t').Append(TxtArea.EditValue == null ? "" : TxtArea.EditValue.ToString()).Append('\t')
              .Append(Cv(SluAgent)).Append('\t').Append(TxtDescription.Text).Append('\t')
              .Append(((int)SpnBillingDay.Value)).Append('\t').Append(CurrentBillingMode()).AppendLine();
            foreach (ItemEditData d in _items)
                sb.Append("I\t").Append(Tsv(d.ServiceItemNo)).Append('\t').Append(Tsv(d.SerialNumber)).Append('\t')
                  .Append(Tsv(d.Description)).Append('\t')
                  .Append(d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value.ToString() : "").Append('\t')
                  .Append(Tsv(d.DepartmentCode)).Append('\t').Append(Tsv(d.JobCode)).Append('\t')
                  .Append(Tsv(d.StockLocationCode)).Append('\t').Append(d.Inactive ? "Y" : "N").AppendLine();
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show("Whole document copied to clipboard.", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
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

        private void CopyItemsAsTsv(System.Collections.Generic.List<int> handles)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("No\tService Item No\tSerial Number\tProvided Items\tBilling Day\tBlack Meter\tColour Meter\tInactive\tExpiry");
            foreach (int rh in handles)
            {
                if (rh < 0) continue;
                sb.Append(Gv(rh, "No")).Append('\t').Append(Gv(rh, "ServiceItemNo")).Append('\t')
                  .Append(Gv(rh, "SerialNumber")).Append('\t').Append(Gv(rh, "Items")).Append('\t')
                  .Append(Gv(rh, "BillingDay")).Append('\t').Append(Gv(rh, "BKMeter")).Append('\t')
                  .Append(Gv(rh, "CLMeter")).Append('\t').Append(Gv(rh, "Inactive")).Append('\t')
                  .Append(Gv(rh, "Expiry")).AppendLine();
            }
            try { System.Windows.Forms.Clipboard.SetText(sb.ToString()); XtraMessageBox.Show(handles.Count + " row(s) copied.", "Copied"); }
            catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); }
        }

        private string Gv(int rh, string field)
        {
            object v = GridViewItems.GetRowCellValue(rh, field);
            return v == null || v == DBNull.Value ? "" : v.ToString().Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        // Paste a whole document (from Copy Whole Document): replaces header + items in this form.
        private void barPasteWhole_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (text == null || !text.StartsWith(CLIP_HEADER))
            { XtraMessageBox.Show("The clipboard does not contain a copied Service Contract document.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            _items.Clear();
            foreach (string line in lines)
            {
                string[] p = line.Split('\t');
                if (p.Length == 0) continue;
                if (p[0] == "H" && p.Length >= 13)
                {
                    SluContractType.EditValue = SetOrNull(p[1]);
                    LkDebtorCode.EditValue = SetOrNull(p[2]);
                    TxtAddress.Text = p[3]; TxtAttention.Text = p[4]; TxtPhone.Text = p[5];
                    TxtTerm.Text = p[6]; TxtArea.EditValue = SetOrNull(p[7]); SluAgent.EditValue = SetOrNull(p[8]);
                    TxtDescription.Text = p[9];
                    int bd; if (int.TryParse(p[10], out bd)) SpnBillingDay.Value = bd;
                    SetBillingMode(p[11]);
                }
                else if (p[0] == "I") _items.Add(ItemFromTsv(p, 1));
            }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show("Document pasted. Review and Save.", "Pasted");
        }

        // Paste ONLY item detail rows (accepts our I-lines or plain TSV): appends to the item list.
        private void barPasteItems_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string text = SafeClipboardText();
            if (string.IsNullOrEmpty(text)) { XtraMessageBox.Show("Clipboard is empty.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int added = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0) continue;
                string[] p = line.Split('\t');
                if (p[0] == CLIP_HEADER || p[0] == "H") continue;
                if (p[0] == "I") { _items.Add(ItemFromTsv(p, 1)); added++; }
                else if (p.Length >= 2 && p[0] != "No")   // plain TSV: No, ServiceItemNo, Serial, ...
                { _items.Add(ItemFromTsv(p, 1)); added++; }
            }
            if (added == 0) { XtraMessageBox.Show("No item rows found on the clipboard.", "Paste"); return; }
            _dirty = true; RebuildItemsView();
            XtraMessageBox.Show(added + " item(s) pasted.", "Pasted");
        }

        // Build a fresh ItemEditData (ItemKey=0 => inserted as new) from a tab-split line at offset.
        private ItemEditData ItemFromTsv(string[] p, int off)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            d.ServiceItemNo = At(p, off); d.SerialNumber = At(p, off + 1); d.Description = At(p, off + 2);
            int bd; d.BillingDayOverride = int.TryParse(At(p, off + 3), out bd) ? (int?)bd : null;
            d.DepartmentCode = At(p, off + 4); d.JobCode = At(p, off + 5); d.StockLocationCode = At(p, off + 6);
            d.Inactive = At(p, off + 7) == "Y";
            return d;
        }

        private static string At(string[] a, int i) { return (i >= 0 && i < a.Length) ? a[i] : ""; }
        private static string Tsv(string s) { return (s ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " "); }
        private static string Cv(DevExpress.XtraEditors.BaseEdit ed) { return ed.EditValue == null ? "" : ed.EditValue.ToString(); }
        private static string SafeClipboardText()
        { try { return System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null; } catch { return null; } }

        // Load another contract's content into THIS form as a template (fresh copies; keeps this form new).
        private void LoadContractAsTemplate(long sourceKey)
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
                SpnBillingDay.Value = AsInt(r["BillingDay"], 1);
                SetBillingMode(AsStr(r["BillingMode"]));
                TxtAddress.Text = AsStr(r["Address1"]); TxtAttention.Text = AsStr(r["Attention"]);
                TxtPhone.Text = AsStr(r["Phone"]); TxtTerm.Text = AsStr(r["TermCode"]);
                TxtArea.EditValue = SetOrNull(AsStr(r["AreaCode"])); SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
                TxtDescription.Text = AsStr(r["Description"]);
                TxtRemark1.Text = AsStr(r["Remark1"]); TxtRemark2.Text = AsStr(r["Remark2"]); TxtNote.Text = AsStr(r["Note"]);

                _items.Clear();
                DataTable it = _db.GetDataTable("SELECT ItemKey FROM [dbo].[zSCP2_Item] WHERE ContractKey=" + sourceKey + " ORDER BY Pos, ItemKey", false);
                foreach (DataRow ir in it.Rows)
                {
                    ItemEditData d = LoadOneItem(_db, Convert.ToInt64(ir["ItemKey"]));
                    d.ItemKey = 0;                 // fresh copy -> inserted as a new item
                    d.ServiceItemNoIsAuto = true;  // draw a new number on save
                    _items.Add(d);
                }
            }
            finally { _loading = false; }
            RebuildItemsView();
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
