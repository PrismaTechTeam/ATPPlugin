using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// In-memory model for one SERVICE ITEM (under a contract) + its meter config + the AutoCount
    /// item codes it provides. The parent contract editor owns a List of these and persists them on save.
    /// </summary>
    public class ItemEditData
    {
        public long ItemKey;
        public string ServiceItemNo = "";
        public string SerialNumber = "";
        public string Description = "";
        public int? BillingDayOverride;   // null = inherit contract
        public string DepartmentCode = "";
        public string JobCode = "";
        public string StockLocationCode = "";
        public bool Inactive;
        public bool ServiceItemNoIsAuto;   // true = number is an auto-preview; the save path draws a fresh committed number
        public DateTime? ServiceExpiryDate;   // from master; null = none. Drives the Expiry column colour.
        public DataTable Meters;          // schema = CreateMetersTable()
        public DataTable ItemCodes;       // schema = CreateItemCodesTable()
        public DataTable SpareParts;      // schema = zSCP2_Contract_Form.CreateSparePartsTable(); item-bound spare parts
        public bool IsGroupItem;          // the contract's invisible GROUP "machine" (fleet-total deals)
        public string MachineMode = "";   // DEFINED ONLINE/OFFLINE ('' = undefined) — drives the
                                          // advanced invoice number format (fetch status = fallback)
        public string BillGroupCode = ""; // #6 Bill Group split billing ('' = not grouped)
        // Which printed LINE this machine belongs on, and the word that line carries ("HEAVY DUTY").
        // Not to be confused with BillGroupCode above, which picks the INVOICE.
        public string LineGroupCode = "";
        /// <summary>Which printed LINE this machine merges onto ("" = follow the contract's line
        /// mode). Set from the Rental Price screen, not typed on the machine.</summary>
        public string MergeGroupCode = "";

        // --- overhaul: header + More Header + Note/Remarks (persisted by PersistItemExtras) ---
        public string ItemCode = "";
        public string GradeCode = "";
        public DateTime? PurchaseDate;         // migrated from master serviceitem.purchasedate
        public string ServiceTypeCode = "";    // per-item service type (zSCP_LK_ServiceType)
        public string Note = "";
        public string Remark1 = "";
        public string Remark2 = "";
        // --- contract-mirrored header context (same as the contract, minus billing checkboxes + value) ---
        public string ReferenceNo = "";
        public string ContractTypeCode = "";
        public string StaffCode = "";              // Agent
        public DateTime? ServiceStartDate;
        public string Address1 = "";
        public string Attention = "";
        public string Phone = "";
        public string TermCode = "";
        public string AreaCode = "";
        // Snapshot of the EFFECTIVE context at load time (contract-inherited display values). Lets
        // PersistItemExtras tell "user changed this" apart from "this was just the inherited value",
        // so untouched fields keep following the contract instead of being solidified onto the item.
        public System.Collections.Generic.Dictionary<string, string> LoadedCtx
            = new System.Collections.Generic.Dictionary<string, string>();
        public DateTime? LoadedStart;
        public DateTime? LoadedExpiry;
        public bool LoadedCtxValid;
        // Multi-machine mode: meters are assigned to provided units (Item Provided rows) by that unit's
        // serial (Meters.MachineSerialNo). OFF = classic one-CSSI-one-machine (all meters serial='').
        public bool MultiMachine;
        // More Header block keyed by zSCP2_Item column name (City/PostalCode/State/Country/Fax/Ref1-4 + Del*).
        public System.Collections.Generic.Dictionary<string, string> MoreHeader
            = new System.Collections.Generic.Dictionary<string, string>();
        // --- Preventive Maintenance (Phase 2) ---
        public bool PMActive;
        public string PMIntervalType = "NONE";
        public int PMIntervalValue;
        public DateTime? PMStartDate;
        public DateTime? PMLastServiceDate;
        public DateTime? PMNextServiceDate;
        public string PMDept = "";
        public string PMJob = "";
        public string PMLocation = "";
    }

    /// <summary>
    /// Child dialog: edit one service item — its serial + BK/CL meter configuration, plus the grid of
    /// AutoCount item codes it provides (Item Code + Description + Qty + Serial No). Returns to parent on OK.
    /// </summary>
    public partial class zSCP2_Item_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly ItemEditData _data;
        private readonly int _contractBillingDay;
        private DataTable _meters;
        private DataTable _itemCodes;
        private DataTable _meterTypeLookup;
        private DataTable _itemLookup;
        private DataTable _serialLookup;
        private bool _standalone;                                   // opened from "Maintain Service Item" New
        // --- overhaul code-built controls ---
        private DevExpress.XtraEditors.SearchLookUpEdit _sluItemCode;   // header Item Code (Item master)
        private DataTable _itemHdrLookup;
        private DevExpress.XtraEditors.SearchLookUpEdit _sluGrade;      // header Grade Code (+ create)
        // --- contract-mirrored header controls (same as contract, minus billing checkboxes + value) ---
        private DevExpress.XtraEditors.SearchLookUpEdit _sluCType;      // Contract Type (+ create)
        private DevExpress.XtraEditors.SearchLookUpEdit _sluAgentI;     // Agent
        private DevExpress.XtraEditors.TextEdit _txtRefNo;
        private DevExpress.XtraEditors.DateEdit _dtStart, _dtExpiry;
        private DevExpress.XtraEditors.MemoEdit _txtAddr;
        private DevExpress.XtraEditors.TextEdit _txtAttn, _txtPhone, _txtTerm, _txtArea;
        private DevExpress.XtraTab.XtraTabControl _tabMain;
        private DevExpress.XtraTab.XtraTabPage _pgItemMeter, _pgPreventive, _pgMoreHeader, _pgDebtorHist, _pgNote, _pgRemark;
        private readonly System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit> _imh
            = new System.Collections.Generic.Dictionary<string, DevExpress.XtraEditors.TextEdit>();   // item More Header fields
        private DevExpress.XtraEditors.MemoEdit _imhDelAddress;
        private DevExpress.XtraEditors.MemoEdit _txtNote;
        private DevExpress.XtraEditors.TextEdit _txtRemark1, _txtRemark2;
        // --- Preventive Maintenance controls (Phase 2) ---
        private DevExpress.XtraEditors.CheckEdit _pmActive;
        private DevExpress.XtraEditors.ComboBoxEdit _pmIntervalType;
        private DevExpress.XtraEditors.SpinEdit _pmIntervalValue;
        private DevExpress.XtraEditors.DateEdit _pmStart, _pmLast, _pmNext;
        private DevExpress.XtraEditors.SearchLookUpEdit _pmDept, _pmJob;
        private DevExpress.XtraEditors.TextEdit _pmLocation;
        private DevExpress.XtraEditors.LabelControl _lblCustomer;
        private DevExpress.XtraEditors.SearchLookUpEdit _lkCustomer;
        private DevExpress.XtraEditors.SearchLookUpEdit _lkContract;   // standalone: attach to an EXISTING contract
        private string _parentContractNo;                          // embedded add: shows contract read-only
        private DevExpress.XtraEditors.SimpleButton _btnDelBranchSearch;   // demo 28/07 #11+14
        private DevExpress.XtraEditors.SimpleButton _btnDelCopyContract;

        public zSCP2_Item_Form()
        {
            InitializeComponent();
        }

        public zSCP2_Item_Form(DBSetting db, ItemEditData data, int contractBillingDay) : this()
        {
            _db = db;
            _data = data;
            _contractBillingDay = contractBillingDay;
            this.Load += new EventHandler(OnFormLoad);
        }

        /// <summary>Standalone-new mode: the item is created on its own (from "Maintain Service Item"),
        /// so the dialog also asks for the Customer — the caller then creates the item's own contract.</summary>
        public zSCP2_Item_Form(DBSetting db, ItemEditData data, int contractBillingDay, bool standaloneNewItem)
            : this(db, data, contractBillingDay)
        {
            _standalone = standaloneNewItem;
        }

        /// <summary>Embedded-add mode (from within a contract): shows the parent Contract No READ-ONLY
        /// so the user knows the item's home and cannot change it here.</summary>
        public zSCP2_Item_Form(DBSetting db, ItemEditData data, int contractBillingDay, string parentContractNo)
            : this(db, data, contractBillingDay)
        {
            _parentContractNo = parentContractNo;
        }

        /// <summary>Debtor picked in standalone mode ("" when not standalone / nothing picked).</summary>
        public string SelectedDebtorCode
        {
            get
            {
                return (_lkCustomer == null || _lkCustomer.EditValue == null)
                    ? "" : _lkCustomer.EditValue.ToString().Trim();
            }
        }

        /// <summary>Existing contract picked in standalone mode (0 = none → caller creates a new one).</summary>
        public long SelectedContractKey
        {
            get
            {
                if (_lkContract == null || _lkContract.EditValue == null || _lkContract.EditValue == DBNull.Value) return 0;
                long k;
                return long.TryParse(_lkContract.EditValue.ToString(), out k) ? k : 0;
            }
        }

        /// <summary>Schema for the per-item meter grid (shared by parent + dialog).</summary>
        public static DataTable CreateMetersTable()
        {
            DataTable dt = new DataTable("Meters");
            dt.Columns.Add("MeterTypeCode", typeof(string));
            dt.Columns.Add("Description", typeof(string));      // per-meter description (defaults from the type)
            dt.Columns.Add("MeterRole", typeof(string));
            dt.Columns.Add("MachineSerialNo", typeof(string));   // '' = the CSSI's own header machine
            dt.Columns.Add("MinimumCharges", typeof(decimal));
            dt.Columns.Add("ChargesRate", typeof(decimal));
            dt.Columns.Add("MeterMultiPriceCode", typeof(string));
            dt.Columns.Add("RebateQtyInPercent", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("InitialReading", typeof(decimal));
            // Per-meter tier override, serialized "boundary|price;boundary|price;..." (invariant).
            // "" = none (the scheme code alone applies). Persisted to zSCP2_ItemMeterPrice on save.
            dt.Columns.Add("CustomTiers", typeof(string));
            // Per-meter WAIVE configuration (Rental-Waive meter types only). FirstN=0 & Target=0
            // -> ALWAYS waive; both set -> SEQUENTIAL (free window first, then by target).
            // The billing engine decides at Generate.
            dt.Columns.Add("WaiveFirstNMonths", typeof(int));
            dt.Columns.Add("WaiveTargetAmount", typeof(decimal));
            dt.Columns.Add("WaivePartialThreshold", typeof(decimal));
            dt.Columns.Add("WaivePartialAmount", typeof(decimal));
            dt.Columns.Add("WaiveScope", typeof(string));
            return dt;
        }

        /// <summary>Schema for the provided AutoCount item-code grid (shared by parent + dialog).</summary>
        public static DataTable CreateItemCodesTable()
        {
            DataTable dt = new DataTable("ItemCodes");
            dt.Columns.Add("ItemCode", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Qty", typeof(decimal));
            dt.Columns.Add("SerialNumber", typeof(string));
            return dt;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null || _data == null) return;

            // Window title tells the mode at a glance: NEW machine vs EDIT of an existing one.
            this.Text = _data.ItemKey > 0
                ? "Service Item — EDIT  ·  " + (string.IsNullOrEmpty(_data.ServiceItemNo) ? ("#" + _data.ItemKey) : _data.ServiceItemNo)
                : "Service Item — NEW";

            LblBillDayHint.Text = "0 = follow contract day (" + _contractBillingDay + ")";

            // Embedded add/edit (not standalone-new): show the item's OWNERSHIP read-only — its contract
            // (or "(no contract)") + its customer. Ownership changes only via the ribbon's Change
            // Ownership action, never by typing here.
            if (!_standalone && (!string.IsNullOrEmpty(_parentContractNo) || _data.ItemKey > 0))
            {
                DevExpress.XtraEditors.LabelControl lblC = new DevExpress.XtraEditors.LabelControl();
                lblC.Text = "Contract No";
                lblC.Location = new System.Drawing.Point(14, 149);
                this.Controls.Add(lblC); lblC.BringToFront();
                _txtParentContractRO = new DevExpress.XtraEditors.TextEdit();
                _txtParentContractRO.Location = new System.Drawing.Point(120, 146);
                _txtParentContractRO.Size = new System.Drawing.Size(280, 20);
                _txtParentContractRO.Properties.ReadOnly = true;
                _txtParentContractRO.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                this.Controls.Add(_txtParentContractRO); _txtParentContractRO.BringToFront();

                DevExpress.XtraEditors.LabelControl lblCu = new DevExpress.XtraEditors.LabelControl();
                lblCu.Text = "Customer";
                lblCu.Location = new System.Drawing.Point(470, 125);
                this.Controls.Add(lblCu); lblCu.BringToFront();
                _txtCustomerRO = new DevExpress.XtraEditors.TextEdit();
                _txtCustomerRO.Location = new System.Drawing.Point(600, 122);
                _txtCustomerRO.Size = new System.Drawing.Size(280, 20);
                _txtCustomerRO.Properties.ReadOnly = true;
                _txtCustomerRO.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                this.Controls.Add(_txtCustomerRO); _txtCustomerRO.BringToFront();

                RefreshOwnershipHeader();
            }

            // Standalone-new (opened from "Maintain Service Item"): the item needs a home. A new row 5
            // (made by pushing the two groups down) lets the user attach it to an EXISTING contract, or
            // leave that empty and pick a Customer — the caller then auto-creates a contract for it.
            // Created in code — the strict designer file stays untouched.
            if (_standalone)
            {
                DevExpress.XtraEditors.LabelControl lblContract = new DevExpress.XtraEditors.LabelControl();
                lblContract.Text = "Contract No";
                lblContract.Location = new System.Drawing.Point(14, 149);
                this.Controls.Add(lblContract);
                lblContract.BringToFront();

                _lkContract = new DevExpress.XtraEditors.SearchLookUpEdit();
                _lkContract.Location = new System.Drawing.Point(120, 146);
                _lkContract.Size = new System.Drawing.Size(280, 20);
                _lkContract.Properties.NullText = "(create new contract)";
                _lkContract.Properties.ValueMember = "ContractKey";
                _lkContract.Properties.DisplayMember = "ContractNo";
                // SearchLookUpEdit: popup GridView auto-populates from the (already minimal) datasource.
                try
                {
                    // Only SAVED contracts appear here — an unsaved contract cannot take service items.
                    _lkContract.Properties.DataSource = _db.GetDataTable(
                        "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                        "FROM [dbo].[zSCP2_Contract] c LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                        "WHERE c.Inactive = 'N' ORDER BY c.ContractNo", false);
                }
                catch { }
                _lkContract.EditValueChanged += delegate
                {
                    bool none = (_lkContract.EditValue == null || _lkContract.EditValue == DBNull.Value);
                    // Context fields stay locked either way — they only RENDER from the picked contract.
                    // Picking a contract = "this item belongs to it": inherit its shared fields into the
                    // header (customer, type, agent, dates, address block, dept/proj, More Header).
                    // Everything stays editable — the user can override any of it before saving.
                    if (!none)
                    {
                        long ck; long.TryParse(_lkContract.EditValue.ToString(), out ck);
                        if (ck > 0) ApplyContractContextByKey(ck);
                    }
                };
                this.Controls.Add(_lkContract);
                _lkContract.BringToFront();

                _lblCustomer = new DevExpress.XtraEditors.LabelControl();
                _lblCustomer.Text = "Customer *";
                _lblCustomer.Location = new System.Drawing.Point(470, 125);
                this.Controls.Add(_lblCustomer);
                _lblCustomer.BringToFront();

                _lkCustomer = new DevExpress.XtraEditors.SearchLookUpEdit();
                _lkCustomer.Location = new System.Drawing.Point(600, 122);
                _lkCustomer.Size = new System.Drawing.Size(280, 20);
                _lkCustomer.Properties.NullText = "Select customer...";
                _lkCustomer.Properties.ValueMember = "AccNo";
                _lkCustomer.Properties.DisplayMember = "AccNo";
                try
                {
                    _lkCustomer.Properties.DataSource = _db.GetDataTable(
                        "SELECT AccNo, CompanyName FROM [dbo].[Debtor] ORDER BY AccNo", false);
                }
                catch { }
                this.Controls.Add(_lkCustomer);
                _lkCustomer.BringToFront();
            }

            LoadMeterTypeLookup();
            LoadItemLookup();
            LoadSerialLookup();
            LoadDeptProjectLookups();
            GridViewItemCodes.ShownEditor += new EventHandler(GridViewItemCodes_ShownEditor);

            TxtServiceItemNo.Text = _data.ServiceItemNo;
            TxtSerial.Text = _data.SerialNumber;
            TxtDescription.Text = _data.Description;
            SpnBillingDayOverride.Value = _data.BillingDayOverride.HasValue ? _data.BillingDayOverride.Value : 0;
            SluDept.EditValue = string.IsNullOrEmpty(_data.DepartmentCode) ? null : (object)_data.DepartmentCode;
            SluProject.EditValue = string.IsNullOrEmpty(_data.JobCode) ? null : (object)_data.JobCode;   // JobCode column stores the AutoCount ProjNo
            TxtLocation.Text = _data.StockLocationCode;
            ChkInactive.Checked = _data.Inactive;

            _itemCodes = _data.ItemCodes != null ? _data.ItemCodes.Copy() : CreateItemCodesTable();
            GridItemCodes.DataSource = _itemCodes;   // Provided-Items grid is hidden in the overhaul; kept bound to avoid null refs

            _meters = _data.Meters != null ? _data.Meters.Copy() : CreateMetersTable();
            if (!_meters.Columns.Contains("MachineSerialNo")) _meters.Columns.Add("MachineSerialNo", typeof(string));
            if (!_meters.Columns.Contains("Description")) _meters.Columns.Add("Description", typeof(string));
            // Designer defines the meter columns; without this, binding would auto-append a stray
            // plain-text MachineSerialNo column next to the code-built combo one.
            GridViewMeters.OptionsBehavior.AutoPopulateColumns = false;
            GridMeters.DataSource = _meters;

            BuildOverhaulUI();   // header Item Code/Grade, hide Stock Location + Provided Items, build the tab layout

            if (string.IsNullOrEmpty(_data.ServiceItemNo)) AutoPickServiceItemNo();

            // Save/close confirmation (CLAUDE.md rule 8): mark dirty on any edit, wired after load.
            EventHandler mark = delegate { _dirty = true; };
            TxtServiceItemNo.EditValueChanged += mark;
            TxtSerial.EditValueChanged += mark;
            TxtDescription.EditValueChanged += mark;
            SpnBillingDayOverride.EditValueChanged += mark;
            SluDept.EditValueChanged += mark;
            SluProject.EditValueChanged += mark;
            TxtLocation.EditValueChanged += mark;
            ChkInactive.EditValueChanged += mark;
            _itemCodes.RowChanged += delegate { _dirty = true; };
            _itemCodes.RowDeleted += delegate { _dirty = true; };
            _meters.RowChanged += delegate { _dirty = true; };
            _meters.RowDeleted += delegate { _dirty = true; };
            if (_itemSpareParts != null) { _itemSpareParts.RowChanged += delegate { _dirty = true; }; _itemSpareParts.RowDeleted += delegate { _dirty = true; }; }
            if (_lkCustomer != null) _lkCustomer.EditValueChanged += mark;
            if (_lkContract != null) _lkContract.EditValueChanged += mark;
            ApplyFieldEditability();
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnItemFormClosing);
        }

        // A service item is contract-bound: only the item-own fields stay editable. Allowed —
        // NEW mode: Service Item No, Contract picker, Item Code, Machine Serial No, Grade Code,
        // Reference No, Service Start/To, Billing Day Override, Description (+ Inactive).
        // EDIT mode: the same minus Service Item No and the Contract picker (contract/customer moves
        // go through the Change Ownership dialog). Everything else RENDERS from the contract.
        private void ApplyFieldEditability()
        {
            bool isNew = _data.ItemKey <= 0;

            TxtServiceItemNo.Properties.ReadOnly = !isNew;
            BtnAutoNo.Enabled = isNew;
            if (_lkContract != null) _lkContract.Properties.ReadOnly = !isNew;

            // Contract-rendered fields are ALWAYS locked — even before a contract is chosen. They can
            // only be filled by selecting a contract (its context renders in). The customer is part of
            // that context, so the picker is locked too; ownership moves go through Change Ownership.
            if (_lkCustomer != null) _lkCustomer.Enabled = false;
            ApplyContextEditability();
        }

        // Contract-rendered context (customer info, type, agent, dept/proj + the whole More Header
        // tab): permanently read-only on the item — values arrive only by picking a contract.
        private void ApplyContextEditability()
        {
            if (_sluCType != null) _sluCType.Properties.ReadOnly = true;
            if (_sluAgentI != null) _sluAgentI.Properties.ReadOnly = true;
            if (_txtAddr != null) _txtAddr.Properties.ReadOnly = true;
            if (_txtAttn != null) _txtAttn.Properties.ReadOnly = true;
            if (_txtPhone != null) _txtPhone.Properties.ReadOnly = true;
            if (_txtTerm != null) _txtTerm.Properties.ReadOnly = true;
            if (_txtArea != null) _txtArea.Properties.ReadOnly = true;
            SluDept.Properties.ReadOnly = true;
            SluProject.Properties.ReadOnly = true;
            foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _imh)
                kv.Value.Properties.ReadOnly = true;
            if (_imhDelAddress != null) _imhDelAddress.Properties.ReadOnly = true;
            if (_pgMoreHeader != null) SetButtonsIn(_pgMoreHeader, false);
        }

        private static void SetButtonsIn(System.Windows.Forms.Control root, bool enabled)
        {
            foreach (System.Windows.Forms.Control c in root.Controls)
            {
                DevExpress.XtraEditors.SimpleButton b = c as DevExpress.XtraEditors.SimpleButton;
                if (b != null) b.Enabled = enabled;
                if (c.Controls.Count > 0) SetButtonsIn(c, enabled);
            }
        }

        private bool _dirty;
        private bool _savedOk;

        private void OnItemFormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (_savedOk || !_dirty || this.DialogResult == DialogResult.OK) return;
            DialogResult r = XtraMessageBox.Show(
                "You have unsaved changes. Discard them and close?", "Unsaved Changes",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) e.Cancel = true;
        }

        private void LoadMeterTypeLookup()
        {
            try
            {
                _meterTypeLookup = _db.GetDataTable(
                    "SELECT MeterTypeCode, Description, MinimumCharges, ChargesRate, " +
                    "MeterMultiPriceCode, RebateQtyInPercent, FOCQty, ISNULL(IsFlatCharge,'N') AS IsFlatCharge, " +
                    "ISNULL(IsRentalWaive,'N') AS IsRentalWaive, ISNULL(DefaultRole,'') AS DefaultRole " +
                    "FROM [dbo].[zSCP_MeterType] WHERE Inactive='N' ORDER BY MeterTypeCode", false);
            }
            catch { _meterTypeLookup = new DataTable(); }
            RepoMeterType.DataSource = _meterTypeLookup;
            RepoMeterType.DisplayMember = "MeterTypeCode";
            RepoMeterType.ValueMember = "MeterTypeCode";
            ApplyPricePickIcon();
        }

        // Price glyph on the caption-less multi-price button column (cosmetic; ellipsis fallback).
        private void ApplyPricePickIcon()
        {
            try
            {
                System.Drawing.Image priceImg =
                    DevExpress.Images.ImageResourceCache.Default.GetImage("images/business objects/bo_price_16x16.png");
                if (priceImg != null && RepoMtPricePick.Buttons.Count > 0)
                {
                    RepoMtPricePick.Buttons[0].Kind = DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph;
                    RepoMtPricePick.Buttons[0].ImageOptions.Image = priceImg;
                }
                if (RepoMtPricePick.Buttons.Count > 0)
                    RepoMtPricePick.Buttons[0].ToolTip = "Pick / edit the Multi-Price tier ladder for this meter";
            }
            catch { }
        }

        // Price button on a meter row: pick a Multi-Price scheme + view/override its tier ladder.
        // The dialog validates the billing traps (last bracket unlimited; 0.00 free band vs Free
        // Qty conflict) and reports when Free Qty must be zeroed. _meters.RowChanged marks dirty.
        private bool IsWaiveType(string meterTypeCode)
        {
            if (_meterTypeLookup == null || !_meterTypeLookup.Columns.Contains("IsRentalWaive") || string.IsNullOrEmpty(meterTypeCode)) return false;
            DataRow[] f = _meterTypeLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["IsRentalWaive"]) == "Y";
        }

        // Committed-minimum (MIN) meter type — by code convention or the type's Default Role.
        private bool IsCommitTypeLk(string meterTypeCode)
        {
            if (string.IsNullOrEmpty(meterTypeCode)) return false;
            if (ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinMeterCode(meterTypeCode)) return true;
            if (_meterTypeLookup == null || !_meterTypeLookup.Columns.Contains("DefaultRole")) return false;
            DataRow[] f = _meterTypeLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["DefaultRole"]).Trim().ToUpperInvariant() == "COMMIT";
        }

        // Flat-charge meter type (rental / committed-minimum) — no reading, no per-copy ladder.
        private bool IsFlatTypeLk(string meterTypeCode)
        {
            if (_meterTypeLookup == null || !_meterTypeLookup.Columns.Contains("IsFlatCharge") || string.IsNullOrEmpty(meterTypeCode)) return false;
            DataRow[] f = _meterTypeLookup.Select("MeterTypeCode='" + meterTypeCode.Replace("'", "''") + "'");
            return f.Length > 0 && Convert.ToString(f[0]["IsFlatCharge"]) == "Y";
        }

        // True when the machine's meter set violates "a waive needs a rental".
        private bool WaiveRuleBrokenI()
        {
            if (_meters == null) return false;
            bool hasWaive = false, hasRent = false;
            foreach (DataRow r in _meters.Rows)
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
        private bool DeleteWouldOrphanWaiveI(DataRow removing)
        {
            if (_meters == null) return false;
            bool hasWaive = false, otherRent = false;
            foreach (DataRow r in _meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted || ReferenceEquals(r, removing)) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                if (IsWaiveType(t)) hasWaive = true;
                else if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t)) otherRent = true;
            }
            return hasWaive && !otherRent;
        }

        // Stock wording check: the text equals some meter type's default description.
        private bool IsTypeDefaultDescription(string desc)
        {
            if (_meterTypeLookup == null || string.IsNullOrEmpty(desc)) return false;
            foreach (DataRow t in _meterTypeLookup.Rows)
                if (string.Equals(Convert.ToString(t["Description"]).Trim(), desc, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // True when the machine already carries a real RENTAL meter (RA-* flat that is NOT a waive).
        private bool MetersHaveRental()
        {
            if (_meters == null) return false;
            foreach (DataRow r in _meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0) continue;
                if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t) && !IsWaiveType(t)) return true;
            }
            return false;
        }

        private void RepoMtPricePick_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            GridViewMeters.CloseEditor();
            DataRow r = GridViewMeters.GetFocusedDataRow();
            if (r == null) return;
            // On a RENTAL-WAIVE meter the same button opens the WAIVE configuration instead —
            // window / target conditions the engine evaluates at every Generate.
            string mtType = r["MeterTypeCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterTypeCode"]).Trim();
            if (IsWaiveType(mtType))
            {
                int wn0 = r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0;
                decimal wt0 = r.Table.Columns.Contains("WaiveTargetAmount") && r["WaiveTargetAmount"] != DBNull.Value ? Convert.ToDecimal(r["WaiveTargetAmount"]) : 0m;
                decimal wpt0 = r.Table.Columns.Contains("WaivePartialThreshold") && r["WaivePartialThreshold"] != DBNull.Value ? Convert.ToDecimal(r["WaivePartialThreshold"]) : 0m;
                decimal wpa0 = r.Table.Columns.Contains("WaivePartialAmount") && r["WaivePartialAmount"] != DBNull.Value ? Convert.ToDecimal(r["WaivePartialAmount"]) : 0m;
                string ws0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value ? Convert.ToString(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form wdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.WaiveConfig_Form(mtType, wn0, wt0, wpt0, wpa0, ws0))
                {
                    if (wdlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                    r["WaiveFirstNMonths"] = wdlg.FirstNMonths;
                    r["WaiveTargetAmount"] = wdlg.TargetAmount;
                    r["WaivePartialThreshold"] = wdlg.PartialThreshold;
                    r["WaivePartialAmount"] = wdlg.PartialAmount;
                    r["WaiveScope"] = wdlg.Scope;
                    GridViewMeters.RefreshData();
                }
                return;
            }
            // Committed-minimum (MIN) meter: the button configures WHICH print charges count
            // toward the committed amount (BK / CL / both). The top-up maths runs at Generate.
            if (IsCommitTypeLk(mtType))
            {
                decimal camt = Math.Abs(r["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(r["MinimumCharges"]));
                string cs0 = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value ? Convert.ToString(r["WaiveScope"]) : "BKCL";
                using (ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form cdlg =
                    new ServiceContractPhotocopier.Classes.CommonForms.CommitConfig_Form(mtType, camt, cs0))
                {
                    if (cdlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                    r["WaiveScope"] = cdlg.Scope;
                    GridViewMeters.RefreshData();
                }
                return;
            }
            // Plain RENTAL meters have no per-copy ladder — the Multi-Price dialog is meaningless
            // there, so the button is a silent no-op.
            if (IsFlatTypeLk(mtType)) return;
            string cur = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string curCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            decimal foc = r["FOCQty"] == DBNull.Value ? 0m : Convert.ToDecimal(r["FOCQty"]);
            using (ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form dlg =
                new ServiceContractPhotocopier.Classes.CommonForms.MultiPricePicker_Form(_db, cur, curCsv, foc))
            {
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;
                r["MeterMultiPriceCode"] = dlg.SelectedCode;
                r["CustomTiers"] = dlg.CustomCsv;
                _ladderFocCache.Clear();   // scheme tiers may have changed elsewhere — recompute displays
                GridViewMeters.RefreshData();
            }
        }

        // A ladder governs the row when a scheme code is picked OR per-meter override tiers exist.
        private bool MeterRowHasLadder(int rowHandle)
        {
            string code = Convert.ToString(GridViewMeters.GetRowCellValue(rowHandle, "MeterMultiPriceCode"));
            string custom = Convert.ToString(GridViewMeters.GetRowCellValue(rowHandle, "CustomTiers"));
            return !string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(custom);
        }

        // Unit Price and Free Qty are DEAD while a ladder is in effect — grey both out
        // (the ladder prices the copies AND carries the free band).
        private void GridViewMeters_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null || (e.Column.FieldName != "ChargesRate" && e.Column.FieldName != "FOCQty")
                || !MeterRowHasLadder(e.RowHandle)) return;
            e.Appearance.BackColor = System.Drawing.Color.Gainsboro;
            e.Appearance.ForeColor = System.Drawing.Color.Gray;
            e.Appearance.Options.UseBackColor = true;
            e.Appearance.Options.UseForeColor = true;
        }

        // ...and not editable (change pricing through the price button instead).
        private void GridViewMeters_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewMeters.FocusedColumn != null
                && (GridViewMeters.FocusedColumn.FieldName == "ChargesRate" || GridViewMeters.FocusedColumn.FieldName == "FOCQty")
                && MeterRowHasLadder(GridViewMeters.FocusedRowHandle))
                e.Cancel = true;
        }

        // Ladder free copies per scheme code (SELECT once, then cached; cleared after the picker runs).
        private readonly System.Collections.Generic.Dictionary<string, decimal> _ladderFocCache =
            new System.Collections.Generic.Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        private decimal LadderFocFor(string code, string customCsv)
        {
            if (!string.IsNullOrEmpty(customCsv))
                return ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(ParseTiersCsv(customCsv));
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
                    tiers.Add(new decimal[] {
                        t["MeterReading"] == DBNull.Value ? 0m : Convert.ToDecimal(t["MeterReading"]),
                        t["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(t["UnitPrice"]) });
                foc = ServiceContractPhotocopier.Classes.ScpMultiPrice.LadderFreeCopies(tiers);
            }
            catch { }
            _ladderFocCache[code] = foc;
            return foc;
        }

        // Multi-Price cell text: scheme code / "code (Modified)" / "(Custom)" / "".
        // Free Qty cell text while a ladder is active: the LADDER's free quantity (display only —
        // the stored per-meter Free Qty is untouched and returns when the ladder is cleared).
        private void GridViewMeters_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.ListSourceRowIndex < 0) return;
            bool wantMulti = e.Column.FieldName == "MeterMultiPriceCode";
            bool wantFoc = e.Column.FieldName == "FOCQty";
            if (!wantMulti && !wantFoc) return;
            if (_meters == null || e.ListSourceRowIndex >= _meters.DefaultView.Count) return;
            DataRow r = _meters.DefaultView[e.ListSourceRowIndex].Row;
            string code = r["MeterMultiPriceCode"] == DBNull.Value ? "" : Convert.ToString(r["MeterMultiPriceCode"]).Trim();
            string custom = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                ? Convert.ToString(r["CustomTiers"]) : "";
            if (wantMulti)
            {
                if (custom.Length > 0) e.DisplayText = code.Length == 0 ? "(Custom)" : code + " (Modified)";
                else e.DisplayText = code;
                return;
            }
            if (code.Length == 0 && custom.Length == 0) return;   // no ladder — show the meter's own Free Qty
            e.DisplayText = LadderFocFor(code, custom).ToString("#,##0.##");
        }

        private void LoadItemLookup()
        {
            try
            {
                _itemLookup = _db.GetDataTable(
                    "SELECT ItemCode, Description FROM dbo.Item ORDER BY ItemCode", false);
            }
            catch { _itemLookup = new DataTable(); }
            RepoItemCode.DataSource = _itemLookup;
            RepoItemCode.DisplayMember = "ItemCode";
            RepoItemCode.ValueMember = "ItemCode";
        }

        private void LoadSerialLookup()
        {
            // AutoCount's per-item serial registry (ItemSerialNo): ItemCode -> SerialNumber.
            try
            {
                _serialLookup = _db.GetDataTable(
                    "SELECT ItemCode, SerialNumber FROM dbo.ItemSerialNo " +
                    "WHERE ISNULL(SerialNumber,'') <> '' ORDER BY ItemCode, SerialNumber", false);
            }
            catch { _serialLookup = new DataTable(); }
        }

        // Cascade: when the Serial No cell opens, fill its dropdown with the serials that belong
        // to that row's chosen Item Code (typing a free value is still allowed).
        private void GridViewItemCodes_ShownEditor(object sender, EventArgs e)
        {
            if (GridViewItemCodes.FocusedColumn == null || GridViewItemCodes.FocusedColumn.FieldName != "SerialNumber") return;
            DevExpress.XtraEditors.ComboBoxEdit ed = GridViewItemCodes.ActiveEditor as DevExpress.XtraEditors.ComboBoxEdit;
            if (ed == null) return;
            ed.Properties.Items.Clear();
            string code = (GridViewItemCodes.GetFocusedRowCellValue("ItemCode") ?? "").ToString().Trim();
            if (_serialLookup == null || code.Length == 0) return;
            foreach (DataRow r in _serialLookup.Select("ItemCode='" + code.Replace("'", "''") + "'"))
                ed.Properties.Items.Add(r["SerialNumber"].ToString());
        }

        private string _autoPeekNo;   // the previewed auto number; if the box still shows it at save, a real number is drawn

        private void AutoPickServiceItemNo()
        {
            // PREVIEW ONLY (peek, no consume) — so clicking Auto repeatedly never increments the counter.
            // The real number is reserved by ScpDocNo.Next() at save time (see the list/contract save path).
            string peek = ServiceContractPhotocopier.Classes.ScpDocNo.Peek(
                _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_SERVICE_ITEM);
            if (string.IsNullOrEmpty(peek))
            {
                // No format row yet — fall back to a legacy MAX+1 preview.
                try
                {
                    object o = _db.ExecuteScalar(
                        "SELECT ISNULL(MAX(CONVERT(int, SUBSTRING(ServiceItemNo,4,20))),0)+1 " +
                        "FROM [dbo].[zSCP2_Item] WHERE ServiceItemNo LIKE 'SI-%' AND ISNUMERIC(SUBSTRING(ServiceItemNo,4,20))=1");
                    int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
                    peek = "SI-" + next.ToString("000000");
                }
                catch { peek = "SI-000001"; }
            }
            _autoPeekNo = peek;
            TxtServiceItemNo.Text = peek;
        }

        private void BtnAutoNo_Click(object sender, EventArgs e) { AutoPickServiceItemNo(); }

        // ---- Item-code grid ----
        private void BtnAddItemCode_Click(object sender, EventArgs e)
        {
            DataRow r = _itemCodes.NewRow();
            r["ItemCode"] = "";
            r["Description"] = "";
            r["Qty"] = 1m;
            r["SerialNumber"] = "";
            _itemCodes.Rows.Add(r);
        }

        private void BtnDelItemCode_Click(object sender, EventArgs e)
        {
            int rh = GridViewItemCodes.FocusedRowHandle;
            if (rh >= 0) GridViewItemCodes.DeleteRow(rh);
        }

        private void GridViewItemCodes_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "ItemCode" || _itemLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _itemLookup.Select("ItemCode='" + code.Replace("'", "''") + "'");
            if (found.Length > 0)
                GridViewItemCodes.SetRowCellValue(e.RowHandle, "Description", found[0]["Description"]);
        }

        // ---- Meter grid ----
        // ===================== Multi-machine meters =====================
        // OFF (default): the classic model — every meter belongs to the CSSI's own header machine.
        // ON: each meter is assigned to one PROVIDED UNIT (an Item Provided row), keyed by that unit's
        // Serial No — one CSSI can then hold several machines, each with its own meters.

        private void BuildMultiMachineMeters()
        {
            // Replace the designer's text buttons with the same +/-/up/down icon toolbar the Item
            // Provided grid uses, so the two grids read as one family.
            BtnAddMeter.Visible = false;
            BtnDelMeter.Visible = false;
            AutoCount.Images.IAutoCountImage mimg = null;
            try
            {
                float dpi = 96f; try { dpi = this.DeviceDpi; } catch { }
                mimg = AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
            }
            catch { }
            DevExpress.XtraEditors.SimpleButton mIns = new DevExpress.XtraEditors.SimpleButton();
            mIns.ToolTip = "Add Meter";
            mIns.Location = new System.Drawing.Point(8, 25); mIns.Size = new System.Drawing.Size(30, 24);
            mIns.Click += new EventHandler(BtnAddMeter_Click); GrpMeters.Controls.Add(mIns); mIns.BringToFront();
            DevExpress.XtraEditors.SimpleButton mRem = new DevExpress.XtraEditors.SimpleButton();
            mRem.ToolTip = "Delete Meter";
            mRem.Location = new System.Drawing.Point(42, 25); mRem.Size = new System.Drawing.Size(30, 24);
            mRem.Click += new EventHandler(BtnDelMeter_Click); GrpMeters.Controls.Add(mRem); mRem.BringToFront();
            DevExpress.XtraEditors.SimpleButton mUp = new DevExpress.XtraEditors.SimpleButton();
            mUp.ToolTip = "Move Up";
            mUp.Location = new System.Drawing.Point(80, 25); mUp.Size = new System.Drawing.Size(30, 24);
            mUp.Click += delegate { MeterMove(-1); }; GrpMeters.Controls.Add(mUp); mUp.BringToFront();
            DevExpress.XtraEditors.SimpleButton mDn = new DevExpress.XtraEditors.SimpleButton();
            mDn.ToolTip = "Move Down";
            mDn.Location = new System.Drawing.Point(114, 25); mDn.Size = new System.Drawing.Size(30, 24);
            mDn.Click += delegate { MeterMove(1); }; GrpMeters.Controls.Add(mDn); mDn.BringToFront();
            if (mimg != null)
            {
                SetIcon(mIns, mimg.GetSmallImage_Insert());
                SetIcon(mRem, mimg.GetSmallImage_Delete());
                SetIcon(mUp, mimg.GetSmallImage_MoveUp());
                SetIcon(mDn, mimg.GetSmallImage_MoveDown());
            }

            _chkMultiMachine = new DevExpress.XtraEditors.CheckEdit();
            _chkMultiMachine.Text = "Meters per provided item (multi-machine)";
            _chkMultiMachine.Location = new System.Drawing.Point(158, 27);
            _chkMultiMachine.Size = new System.Drawing.Size(280, 20);
            GrpMeters.Controls.Add(_chkMultiMachine);
            _chkMultiMachine.BringToFront();
            // ══════════════ DESIGN DISCARDED (2026-07-24, user decision) ══════════════
            // The "multi-machine CSSI" idea (ONE service item carrying SEVERAL physical units,
            // meters assigned per provided-unit serial) is ABANDONED: one CSSI = ONE machine.
            // The checkbox is HIDDEN, not removed — items already saved with per-unit serials
            // (MachineSerialNo set / MultiMachine=Y) still load, display and bill unchanged.
            // Do NOT resurface this checkbox or extend the per-unit meter design.
            _chkMultiMachine.Visible = false;

            // Meter Type maintenance shortcut: create/edit meter types without leaving the item, then
            // the Meter Type dropdown refreshes with the new rows.
            DevExpress.XtraEditors.SimpleButton mMaint = new DevExpress.XtraEditors.SimpleButton();
            mMaint.Text = "Meter Types...";
            mMaint.ToolTip = "Open the Meter Type maintenance module";
            mMaint.Location = new System.Drawing.Point(448, 25);
            mMaint.Size = new System.Drawing.Size(120, 24);
            mMaint.Click += new EventHandler(BtnMeterMaintenance_Click);
            GrpMeters.Controls.Add(mMaint);
            mMaint.BringToFront();

            // A rented machine always carries a rental meter, so asking for the meter is asking the
            // same question twice. Tick this and the row appears already typed, roled and described
            // -- the only thing left is how much a month.
            _chkNeedRental = new DevExpress.XtraEditors.CheckEdit();
            _chkNeedRental.Text = "Need rental";
            _chkNeedRental.ToolTip = "This machine is rented -- adds the RENTAL meter, you just fill in the monthly amount";
            _chkNeedRental.Location = new System.Drawing.Point(584, 27);
            _chkNeedRental.Size = new System.Drawing.Size(120, 20);
            GrpMeters.Controls.Add(_chkNeedRental);
            _chkNeedRental.BringToFront();
            _suppressRentalEvt = true;
            _chkNeedRental.Checked = FindRentalMeterRow() != null;
            _suppressRentalEvt = false;
            _chkNeedRental.CheckedChanged += new EventHandler(ChkNeedRental_CheckedChanged);
            // Adding or deleting a rental by hand must move the tick too, or it would state the
            // opposite of what the grid shows.
            _meters.RowChanged += new DataRowChangeEventHandler(Meters_RentalWatch);
            _meters.RowDeleted += new DataRowChangeEventHandler(Meters_RentalWatch);

            // READ-ONLY: the machine assignment is decided when the meter is ADDED (focused Item
            // Provided row -> + Add Meter). To reassign, delete the meter and re-add it on the right
            // unit — in-grid editing of the assignment is deliberately not allowed.
            _colMachineSerial = new DevExpress.XtraGrid.Columns.GridColumn();
            _colMachineSerial.FieldName = "MachineSerialNo";
            _colMachineSerial.Caption = "Machine Serial";
            _colMachineSerial.Width = 130;
            _colMachineSerial.OptionsColumn.AllowEdit = false;
            _colMachineSerial.OptionsColumn.ReadOnly = true;
            GridViewMeters.Columns.Add(_colMachineSerial);

            // Read-only companion column: the Item Code of the machine each meter is assigned to
            // (unbound, derived live from the Item Provided rows; '' serial = the header machine).
            _colMachineItemCode = new DevExpress.XtraGrid.Columns.GridColumn();
            _colMachineItemCode.FieldName = "MachineItemCode";
            _colMachineItemCode.Caption = "Item Code";
            _colMachineItemCode.UnboundDataType = typeof(string);
            _colMachineItemCode.Width = 120;
            _colMachineItemCode.OptionsColumn.AllowEdit = false;
            _colMachineItemCode.OptionsColumn.ReadOnly = true;
            GridViewMeters.Columns.Add(_colMachineItemCode);
            GridViewMeters.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(GridViewMeters_UnboundData);
            // Rental (flat) rows shade amber — maintained in the Rental Maintenance module.
            GridViewMeters.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewMeters_FlatRowStyle);

            _suppressMultiEvt = true;
            _chkMultiMachine.Checked = _data.MultiMachine;
            _suppressMultiEvt = false;
            ApplyMultiMachineUi(_data.MultiMachine);
            _chkMultiMachine.CheckedChanged += new EventHandler(ChkMultiMachine_CheckedChanged);
        }

        private void ApplyMultiMachineUi(bool on)
        {
            if (on)
            {
                _colMachineSerial.Visible = true; _colMachineSerial.VisibleIndex = 1;
                _colMachineItemCode.Visible = true; _colMachineItemCode.VisibleIndex = 2;
            }
            else { _colMachineSerial.Visible = false; _colMachineItemCode.Visible = false; }
            GrpMeters.Text = on
                ? "Meter Configuration (per machine — one Black + one Colour each)"
                : "Meter Configuration (tag one Black + one Colour)";
        }

        // Unbound "Item Code" cell = the item code of the provided unit whose serial the meter is
        // assigned to; '' serial resolves to the header machine's own Item Code.
        private void GridViewMeters_UnboundData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (!e.IsGetData || e.Column != _colMachineItemCode) return;
            DataRowView drv = e.Row as DataRowView;
            string serial = drv == null ? "" : (((drv.Row["MachineSerialNo"] as string) ?? "").Trim());
            if (serial.Length == 0)
            {
                e.Value = _sluItemCode != null && _sluItemCode.EditValue != null ? _sluItemCode.EditValue.ToString() : "";
                return;
            }
            e.Value = "";
            if (_itemSpareParts == null) return;
            foreach (DataRow r in _itemSpareParts.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string sn = (r.Table.Columns.Contains("SerialNumber") && r["SerialNumber"] != DBNull.Value ? (string)r["SerialNumber"] : "").Trim();
                if (!string.Equals(sn, serial, StringComparison.OrdinalIgnoreCase)) continue;
                e.Value = r["ItemCode"] == DBNull.Value ? "" : (string)r["ItemCode"];
                return;
            }
        }

        private void ChkMultiMachine_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressMultiEvt) return;
            bool on = _chkMultiMachine.Checked;
            if (!on)
            {
                int assigned = 0;
                foreach (DataRow r in _meters.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    if (((r["MachineSerialNo"] as string) ?? "").Trim().Length > 0) assigned++;
                }
                if (assigned > 0)
                {
                    if (XtraMessageBox.Show(
                            "Turning multi-machine off clears the machine assignment on " + assigned + " meter(s) — " +
                            "they all become meters of this service item's own machine. Continue?",
                            "Multi-machine", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    {
                        _suppressMultiEvt = true; _chkMultiMachine.Checked = true; _suppressMultiEvt = false;
                        return;
                    }
                    foreach (DataRow r in _meters.Rows)
                        if (r.RowState != DataRowState.Deleted) r["MachineSerialNo"] = "";
                }
            }
            ApplyMultiMachineUi(on);
            _dirty = true;
        }

        // Reorder meter rows by swapping full row contents with the neighbour (the grid is bound in
        // table order, so a value swap IS the visual move).
        private void MeterMove(int delta)
        {
            GridViewMeters.PostEditor();
            int rh = GridViewMeters.FocusedRowHandle;
            int target = rh + delta;
            if (rh < 0 || target < 0 || target >= GridViewMeters.RowCount) return;
            DataRowView a = GridViewMeters.GetRow(rh) as DataRowView;
            DataRowView b = GridViewMeters.GetRow(target) as DataRowView;
            if (a == null || b == null) return;
            object[] tmp = a.Row.ItemArray;
            a.Row.ItemArray = b.Row.ItemArray;
            b.Row.ItemArray = tmp;
            GridViewMeters.FocusedRowHandle = target;
            _dirty = true;
        }

        // Distinct non-empty serials of the (non-deleted) Item Provided rows.
        private System.Collections.Generic.List<string> ProvidedSerials()
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
            System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_itemSpareParts != null)
            {
                foreach (DataRow r in _itemSpareParts.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string sn = (r.Table.Columns.Contains("SerialNumber") ? (r["SerialNumber"] as string ?? "") : "").Trim();
                    if (sn.Length > 0 && seen.Add(sn)) list.Add(sn);
                }
            }
            return list;
        }

        private void BtnAddMeter_Click(object sender, EventArgs e)
        {
            // Multi-machine: a focused Item Provided row (with a serial) is a CONVENIENCE — the new
            // meter inherits its serial. No row selected / no serial = '' = the header machine; the
            // assignment can also be set later in the Machine Serial column. Never blocks.
            string machineSerial = "";
            // The Item Provided grid is hidden now — only honour its focused row when it is actually
            // visible, otherwise an invisible row 0 would silently tag every new meter.
            if (_chkMultiMachine != null && _chkMultiMachine.Checked && _viewItemSp != null &&
                _grpItemSp != null && _grpItemSp.Visible)
            {
                _viewItemSp.PostEditor();
                int rh = _viewItemSp.FocusedRowHandle;
                DataRowView drv = rh < 0 ? null : _viewItemSp.GetRow(rh) as DataRowView;
                machineSerial = drv == null ? "" : ((drv.Row["SerialNumber"] as string) ?? "").Trim();
            }
            DataRow r = _meters.NewRow();
            r["MeterRole"] = "";   // NO default — the user must pick BK / CL / NA (save blocks an empty role)
            r["Description"] = "";
            r["MachineSerialNo"] = machineSerial;
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            r["CustomTiers"] = "";
            r["WaiveFirstNMonths"] = 0; r["WaiveTargetAmount"] = 0m; r["WaivePartialThreshold"] = 0m; r["WaivePartialAmount"] = 0m; r["WaiveScope"] = "BKCL";
            _meters.Rows.Add(r);
        }

        private void BtnDelMeter_Click(object sender, EventArgs e)
        {
            int rh = GridViewMeters.FocusedRowHandle;
            if (rh < 0) return;
            DataRowView drvDel = GridViewMeters.GetRow(rh) as DataRowView;
            string delType = drvDel == null || drvDel.Row["MeterTypeCode"] == DBNull.Value ? "" : drvDel.Row["MeterTypeCode"].ToString().Trim();
            // Deleting the machine's only RENTAL while a WAIVE stays would break the waive rule.
            if (delType.Length > 0 && !IsWaiveType(delType)
                && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(delType)
                && DeleteWouldOrphanWaiveI(drvDel.Row))
            {
                XtraMessageBox.Show("This machine still has a RENTAL WAIVE meter — remove the waive line first " +
                    "(or keep a rental meter). A waive needs a rent to waive.", "Remove Meter");
                return;
            }
            // Removing a meter from a SAVED machine deletes that meter's whole reading history at save
            // (zSCP_MeterTrans / zSCP2_MeterEntry cascade on zSCP2_ItemMeter). The contract's meter
            // panel already warns for exactly this — the dialog must not be the silent path.
            if (_data != null && _data.ItemKey > 0)
            {
                DataRowView drv = GridViewMeters.GetRow(rh) as DataRowView;
                string mt = drv == null || drv.Row["MeterTypeCode"] == DBNull.Value ? "" : drv.Row["MeterTypeCode"].ToString();
                bool unsavedRow = drv != null && drv.Row.RowState == DataRowState.Added;
                if (!unsavedRow && mt.Length > 0 &&
                    XtraMessageBox.Show("Remove meter '" + mt + "'?\r\n\r\nWhen you save, this meter AND its meter " +
                        "reading history (all readings + billing log for this counter) are permanently deleted.",
                        "Remove Meter", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            GridViewMeters.DeleteRow(rh);
        }

        // ---- Need rental ----
        // The machine's own RENTAL meter, or null. A WAIVE is not one: it is the contra that takes
        // the rent away again, and it needs a real rental sitting next to it.
        private DataRow FindRentalMeterRow()
        {
            if (_meters == null) return null;
            foreach (DataRow r in _meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string t = Convert.ToString(r["MeterTypeCode"]).Trim();
                if (t.Length == 0 || IsWaiveType(t)) continue;
                if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(t)) return r;
            }
            return null;
        }

        // Add or delete a rental in the grid and the tick follows — it must never claim the opposite
        // of what the rows show.
        private void Meters_RentalWatch(object sender, DataRowChangeEventArgs e)
        {
            if (_chkNeedRental == null || _suppressRentalEvt) return;
            bool has = FindRentalMeterRow() != null;
            if (_chkNeedRental.Checked == has) return;
            _suppressRentalEvt = true;
            _chkNeedRental.Checked = has;
            _suppressRentalEvt = false;
        }

        private void ChkNeedRental_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressRentalEvt) return;
            if (_chkNeedRental.Checked) AddStandardRentalMeter();
            else RemoveRentalMeter();
        }

        // Ticked: drop in the RENTAL meter, already typed, roled and described, and put the cursor on
        // the amount. Everything else about a rental is the same every time — only the money differs.
        private void AddStandardRentalMeter()
        {
            if (FindRentalMeterRow() != null) return;   // already has one; the tick just caught up

            DataRow[] std = _meterTypeLookup == null ? new DataRow[0]
                : _meterTypeLookup.Select("MeterTypeCode='" +
                    ServiceContractPhotocopier.Classes.ScpStrategy.METER_TYPE_RENTAL.Replace("'", "''") + "'");

            _suppressRentalEvt = true;
            DataRow r = _meters.NewRow();
            r["MeterRole"] = "RENTAL";
            r["MeterTypeCode"] = std.Length > 0 ? Convert.ToString(std[0]["MeterTypeCode"]) : "";
            r["Description"] = std.Length > 0 && std[0].Table.Columns.Contains("Description")
                ? Convert.ToString(std[0]["Description"]) : "";
            r["MachineSerialNo"] = "";
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;          // the amount the user is about to type
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            r["CustomTiers"] = "";
            r["WaiveFirstNMonths"] = 0; r["WaiveTargetAmount"] = 0m; r["WaivePartialThreshold"] = 0m;
            r["WaivePartialAmount"] = 0m; r["WaiveScope"] = "BKCL";
            _meters.Rows.Add(r);
            _suppressRentalEvt = false;
            _dirty = true;

            if (std.Length == 0)
                XtraMessageBox.Show("The standard '" + ServiceContractPhotocopier.Classes.ScpStrategy.METER_TYPE_RENTAL +
                    "' meter type is not in this book, so the row is waiting for a type.\r\n\r\n" +
                    "Pick a rental type in the row, or create the standard one in Meter Types...",
                    "Need rental");

            // Land on the money: the amount is the only thing this row still needs.
            GridViewMeters.RefreshData();
            int rh = GridViewMeters.GetRowHandle(_meters.Rows.IndexOf(r));
            if (rh < 0) return;
            GridViewMeters.FocusedRowHandle = rh;
            GridViewMeters.FocusedColumn = std.Length > 0 ? ColMtRate : ColMtCode;
            GridMeters.Focus();
            GridViewMeters.ShowEditor();
        }

        // Unticked: take the rental away — through the same two gates the Delete button uses, because
        // this is the same deletion. A waive left with nothing to waive is refused; a saved rental
        // takes its whole reading history with it and says so first.
        private void RemoveRentalMeter()
        {
            DataRow r = FindRentalMeterRow();
            if (r == null) return;
            string t = Convert.ToString(r["MeterTypeCode"]).Trim();

            if (DeleteWouldOrphanWaiveI(r))
            {
                XtraMessageBox.Show("This machine still has a RENTAL WAIVE meter — remove the waive line first " +
                    "(or keep the rental). A waive needs a rent to waive.", "Need rental");
                _suppressRentalEvt = true; _chkNeedRental.Checked = true; _suppressRentalEvt = false;
                return;
            }
            if (_data != null && _data.ItemKey > 0 && r.RowState != DataRowState.Added && t.Length > 0 &&
                XtraMessageBox.Show("Remove the rental meter '" + t + "'?\r\n\r\nWhen you save, this meter AND its " +
                    "reading history (all readings + billing log for this counter) are permanently deleted.",
                    "Need rental", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                _suppressRentalEvt = true; _chkNeedRental.Checked = true; _suppressRentalEvt = false;
                return;
            }
            _suppressRentalEvt = true;
            r.Delete();
            _suppressRentalEvt = false;
            _dirty = true;
            GridViewMeters.RefreshData();
        }

        // "Meter Types..." — open the Meter Type maintenance module; when it closes, reload the
        // Meter Type dropdown so freshly created types are immediately pickable.
        private void BtnMeterMaintenance_Click(object sender, EventArgs e)
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
                LoadMeterTypeLookup();   // refresh the dropdown datasource with any new/edited types
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Open Meter Type maintenance failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Rental/flat rows: amber background (their home is the Rental Maintenance module).
        private void GridViewMeters_FlatRowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (_meterTypeLookup == null || !_meterTypeLookup.Columns.Contains("IsFlatCharge")) return;
            string type = Convert.ToString(GridViewMeters.GetRowCellValue(e.RowHandle, "MeterTypeCode"));
            if (string.IsNullOrEmpty(type)) return;
            DataRow[] f = _meterTypeLookup.Select("MeterTypeCode='" + type.Replace("'", "''") + "'");
            if (f.Length == 0 || Convert.ToString(f[0]["IsFlatCharge"]) != "Y") return;
            // ForeColor pinned too — the focused row's white text disappears on the pale amber.
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 248, 225);
            e.Appearance.ForeColor = System.Drawing.Color.Black;
            e.Appearance.Options.UseBackColor = true;
            e.Appearance.Options.UseForeColor = true;
        }

        private void GridViewMeters_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "MinimumCharges")
            {
                // Waive rows keep their amount NEGATIVE (the contra) — a positive entry flips.
                try
                {
                    string rowType = Convert.ToString(GridViewMeters.GetRowCellValue(e.RowHandle, "MeterTypeCode")).Trim();
                    decimal mv = e.Value == null || e.Value == DBNull.Value ? 0m : Convert.ToDecimal(e.Value);
                    if (mv > 0m && IsWaiveType(rowType))
                        GridViewMeters.SetRowCellValue(e.RowHandle, "MinimumCharges", -mv);
                }
                catch { }
            }
            if (e.Column == null || e.Column.FieldName != "MeterTypeCode" || _meterTypeLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _meterTypeLookup.Select("MeterTypeCode='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow m = found[0];
            int rh = e.RowHandle;
            // MACHINE INVARIANT: a RENTAL WAIVE meter requires a real RENTAL meter. ANY type change
            // that breaks it is rejected on the spot — picking a waive first, OR re-typing the only
            // rental away while a waive stays. A fresh row is removed WHOLE (no half-filled ghost);
            // an existing row snaps back to its original type (deleting it would kill its readings).
            if (WaiveRuleBrokenI())
            {
                XtraMessageBox.Show("A RENTAL WAIVE meter needs a RENTAL meter on the same machine.\r\n" +
                    "Add/keep the rent line first (or remove the waive line).", "Meter");
                DataRow grw = GridViewMeters.GetDataRow(rh);
                if (grw != null && grw.RowState == DataRowState.Added)
                    GridViewMeters.DeleteRow(rh);
                else if (grw != null)
                {
                    grw["MeterTypeCode"] = grw["MeterTypeCode", DataRowVersion.Original];
                    grw["Description"] = grw["Description", DataRowVersion.Original];
                }
                GridViewMeters.RefreshData();
                return;
            }
            // Re-picking a type refreshes the Description too — UNLESS the user hand-typed their own
            // (text matching some type's default is stock wording, not a customization).
            object curDesc = GridViewMeters.GetRowCellValue(rh, "Description");
            string curDescS = curDesc == null || curDesc == DBNull.Value ? "" : curDesc.ToString().Trim();
            if (curDescS.Length == 0 || IsTypeDefaultDescription(curDescS))
                GridViewMeters.SetRowCellValue(rh, "Description", m.Table.Columns.Contains("Description") ? m["Description"] : "");
            // Waive types carry their amount NEGATIVE on the machine (the contra line).
            if (IsWaiveType(code))
                GridViewMeters.SetRowCellValue(rh, "MinimumCharges",
                    -Math.Abs(m["MinimumCharges"] == DBNull.Value ? 0m : Convert.ToDecimal(m["MinimumCharges"])));
            else
                GridViewMeters.SetRowCellValue(rh, "MinimumCharges", m["MinimumCharges"]);
            GridViewMeters.SetRowCellValue(rh, "ChargesRate", m["ChargesRate"]);
            GridViewMeters.SetRowCellValue(rh, "MeterMultiPriceCode", m["MeterMultiPriceCode"]);
            GridViewMeters.SetRowCellValue(rh, "RebateQtyInPercent", m["RebateQtyInPercent"]);
            GridViewMeters.SetRowCellValue(rh, "FOCQty", m["FOCQty"]);
            // Default BK/CL role from the meter type name when still unset (usage meters must have a
            // role or the Meter Reading fetch skips them).
            object curRole = GridViewMeters.GetRowCellValue(rh, "MeterRole");
            string curRoleS = curRole == null ? "" : curRole.ToString().Trim().ToUpperInvariant();
            if (curRoleS == "" || curRoleS == "NA" || curRoleS == "RENTAL" || curRoleS == "WAIVE" || curRoleS == "COMMIT")
            {
                string defRole = m.Table.Columns.Contains("DefaultRole") ? Convert.ToString(m["DefaultRole"]).Trim().ToUpperInvariant() : "";
                string inferred = defRole.Length > 0 ? defRole
                    : zSCP2_Contract_Form.InferMeterRole(code, m.Table.Columns.Contains("Description") ? Convert.ToString(m["Description"]) : "");
                if (inferred.Length > 0) GridViewMeters.SetRowCellValue(rh, "MeterRole", inferred);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewItemCodes.CloseEditor();
            GridViewItemCodes.UpdateCurrentRow();
            GridViewMeters.CloseEditor();
            GridViewMeters.UpdateCurrentRow();

            if (string.IsNullOrWhiteSpace(TxtServiceItemNo.Text))
            { XtraMessageBox.Show("Service Item No is required.", "Validation"); return; }
            if (string.IsNullOrWhiteSpace(TxtSerial.Text))
            { XtraMessageBox.Show("Machine Serial No is required (it is the key the meter API matches on).", "Validation"); return; }

            // Every meter row must carry an EXPLICIT Role — there is deliberately no default, so a
            // forgotten role is caught HERE while the dialog is still open (the DB save would reject
            // it anyway, but by then the dialog is closed and the edits would be lost).
            if (_meters != null)
            {
                foreach (DataRow mr in _meters.Rows)
                {
                    if (mr.RowState == DataRowState.Deleted) continue;
                    string mtype = (mr["MeterTypeCode"] == DBNull.Value ? "" : Convert.ToString(mr["MeterTypeCode"])).Trim();
                    if (mtype.Length == 0) continue;
                    string mrole = (mr["MeterRole"] == DBNull.Value ? "" : Convert.ToString(mr["MeterRole"])).Trim().ToUpperInvariant();
                    if (mrole != "BK" && mrole != "CL" && mrole != "NA"
                        && mrole != "RENTAL" && mrole != "WAIVE" && mrole != "COMMIT")
                    {
                        XtraMessageBox.Show(
                            "Meter '" + mtype + "' has NO Role.\r\n\r\nPick BK (black), CL (colour), RENTAL, " +
                            "WAIVE, COMMIT or NA on the meter row — the role decides Black/Colour " +
                            "billing, strategy scopes and the reading fetch.", "Validation");
                        return;
                    }
                }
            }
            if (_standalone && SelectedContractKey == 0 && SelectedDebtorCode.Length == 0)
            { XtraMessageBox.Show("Pick a Contract No, or pick a Customer (a new contract is then created).", "Validation"); return; }

            // The machine serial is what Meter Reading Integration looks a machine up by — a duplicate
            // makes readings ambiguous. Warn (legacy data may have duplicates, so allow an override).
            try
            {
                string ser = TxtSerial.Text.Trim().Replace("'", "''");
                DataTable dup = _db.GetDataTable(
                    "SELECT TOP 1 ServiceItemNo FROM [dbo].[zSCP2_Item] " +
                    "WHERE LTRIM(RTRIM(SerialNumber))='" + ser + "' AND ItemKey<>" + _data.ItemKey, false);
                if (dup.Rows.Count > 0 &&
                    XtraMessageBox.Show(
                        "Machine Serial No '" + TxtSerial.Text.Trim() + "' is already used by service item " +
                        dup.Rows[0]["ServiceItemNo"] + ".\r\nMeter reading matches machines by this serial — continue anyway?",
                        "Duplicate Serial", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            catch { }

            // Meter rules are PER MACHINE. Single mode: all meters belong to the header machine (one
            // group). Multi mode: rows group by their assigned provided-unit serial — each unit gets
            // its own <=1 BK, <=1 CL and unique-meter-type rules (DB identity: item + type + serial).
            bool multi = _chkMultiMachine != null && _chkMultiMachine.Checked;
            if (_viewItemSp != null) _viewItemSp.PostEditor();
            System.Collections.Generic.List<string> provided = ProvidedSerials();

            // Item Provided must not repeat: same Item Code + same Serial No is a duplicate row.
            // The same Item Code with DIFFERENT serials is fine (several units of one model).
            if (_itemSpareParts != null)
            {
                System.Collections.Generic.HashSet<string> unitKeys =
                    new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in _itemSpareParts.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string code = (r["ItemCode"] == DBNull.Value ? "" : (string)r["ItemCode"]).Trim();
                    if (code.Length == 0) continue;
                    string sn = (r.Table.Columns.Contains("SerialNumber") && r["SerialNumber"] != DBNull.Value ? (string)r["SerialNumber"] : "").Trim();
                    if (!unitKeys.Add(code + "|" + sn))
                    {
                        XtraMessageBox.Show(
                            "Item Provided has a duplicate: '" + code + "'" +
                            (sn.Length > 0 ? " with serial '" + sn + "'" : " (no serial)") +
                            ".\r\nThe same item code is allowed only with different serial numbers.",
                            "Validation");
                        return;
                    }
                }
            }
            System.Collections.Generic.Dictionary<string, int> bkPer = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            System.Collections.Generic.Dictionary<string, int> clPer = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            System.Collections.Generic.HashSet<string> typePerMachine =
                new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string mtype = (r["MeterTypeCode"] as string ?? "").Trim();
                if (mtype.Length == 0)
                { XtraMessageBox.Show("Every meter row must have a Meter Type.", "Validation"); return; }
                string mser = multi ? ((r["MachineSerialNo"] as string) ?? "").Trim() : "";
                // Machine Serial is OPTIONAL ('' = this service item's own header machine). When set,
                // it must point at an actual Item Provided row.
                if (multi && mser.Length > 0)
                {
                    bool known = false;
                    foreach (string sn in provided) if (string.Equals(sn, mser, StringComparison.OrdinalIgnoreCase)) { known = true; break; }
                    if (!known)
                    { XtraMessageBox.Show("Machine Serial '" + mser + "' is not on any Item Provided row. Add the unit (with its Serial No) first.", "Validation"); return; }
                }
                if (!typePerMachine.Add(mser.ToUpperInvariant() + "|" + mtype))
                { XtraMessageBox.Show("Meter type '" + mtype + "' appears more than once" + (multi ? " for machine '" + mser + "'" : "") + ". Each meter type can only be configured once per machine.", "Validation"); return; }
                string role = (r["MeterRole"] == null ? "" : r["MeterRole"].ToString()).Trim().ToUpperInvariant();
                if (role == "BK") { int n; bkPer.TryGetValue(mser, out n); bkPer[mser] = n + 1; }
                else if (role == "CL") { int n; clPer.TryGetValue(mser, out n); clPer[mser] = n + 1; }
            }
            foreach (System.Collections.Generic.KeyValuePair<string, int> kv in bkPer)
                if (kv.Value > 1)
                { XtraMessageBox.Show("Only one meter can be tagged Black (BK)" + (multi ? " per machine ('" + kv.Key + "')" : "") + ".", "Validation"); return; }
            foreach (System.Collections.Generic.KeyValuePair<string, int> kv in clPer)
                if (kv.Value > 1)
                { XtraMessageBox.Show("Only one meter can be tagged Colour (CL)" + (multi ? " per machine ('" + kv.Key + "')" : "") + ".", "Validation"); return; }

            // A waive contra without a rental meter would credit money from nowhere — block it.
            {
                bool hasWaive = false, hasRent = false;
                foreach (DataRow wr in _meters.Rows)
                {
                    if (wr.RowState == DataRowState.Deleted) continue;
                    string wt = Convert.ToString(wr["MeterTypeCode"]).Trim();
                    if (wt.Length == 0) continue;
                    if (IsWaiveType(wt)) hasWaive = true;
                    else if (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(wt)) hasRent = true;
                }
                if (hasWaive && !hasRent)
                {
                    XtraMessageBox.Show("A RENTAL WAIVE meter needs a RENTAL meter on the same machine — " +
                        "add the rent line first.", "Validation");
                    return;
                }
            }

            // LAST validation BEFORE the first _data mutation: _data IS the caller's live item (shared
            // by reference), so any early-return below this point would leave phantom edits that ride
            // the next contract save even after the user cancels this dialog.
            DateTime? vStart = _dtStart != null && _dtStart.EditValue != null && _dtStart.EditValue != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(_dtStart.EditValue) : null;
            DateTime? vExpiry = _dtExpiry != null && _dtExpiry.EditValue != null && _dtExpiry.EditValue != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(_dtExpiry.EditValue) : null;
            if (vStart.HasValue && vExpiry.HasValue && vExpiry.Value < vStart.Value)
            { XtraMessageBox.Show("Service expiry date cannot be earlier than the start date.", "Validation"); return; }

            _data.ServiceItemNo = TxtServiceItemNo.Text.Trim();
            // Number still shows the auto-preview & the user didn't type their own -> reserve a real one at insert.
            _data.ServiceItemNoIsAuto = (!string.IsNullOrEmpty(_autoPeekNo) && _data.ServiceItemNo == _autoPeekNo);
            _data.SerialNumber = TxtSerial.Text.Trim();
            _data.Description = TxtDescription.Text.Trim();
            int bd = (int)SpnBillingDayOverride.Value;
            _data.BillingDayOverride = (bd >= 1 && bd <= 31) ? (int?)bd : null;
            _data.DepartmentCode = SluDept.EditValue == null ? "" : SluDept.EditValue.ToString().Trim();
            _data.JobCode = SluProject.EditValue == null ? "" : SluProject.EditValue.ToString().Trim();   // AutoCount ProjNo
            _data.StockLocationCode = TxtLocation.Text.Trim();
            _data.Inactive = ChkInactive.Checked;
            _itemCodes.AcceptChanges();
            _data.ItemCodes = _itemCodes;
            _data.MultiMachine = multi;
            if (!multi)
                foreach (DataRow r in _meters.Rows)
                    if (r.RowState != DataRowState.Deleted) r["MachineSerialNo"] = "";
            _meters.AcceptChanges();
            _data.Meters = _meters;
            if (_viewItemSp != null) _viewItemSp.PostEditor();
            if (_itemSpareParts != null) { _itemSpareParts.AcceptChanges(); _data.SpareParts = _itemSpareParts; }

            // Extended fields (persisted by PersistItemExtras in the caller's transaction).
            _data.ItemCode = _sluItemCode != null && _sluItemCode.EditValue != null ? _sluItemCode.EditValue.ToString().Trim() : "";
            _data.GradeCode = _sluGrade != null && _sluGrade.EditValue != null ? _sluGrade.EditValue.ToString().Trim() : "";
            // Contract-mirrored header context.
            _data.ContractTypeCode = _sluCType != null && _sluCType.EditValue != null ? _sluCType.EditValue.ToString().Trim() : "";
            _data.StaffCode = _sluAgentI != null && _sluAgentI.EditValue != null ? _sluAgentI.EditValue.ToString().Trim() : "";
            _data.ReferenceNo = _txtRefNo != null ? _txtRefNo.Text.Trim() : "";
            _data.Address1 = _txtAddr != null ? _txtAddr.Text.Trim() : "";
            _data.Attention = _txtAttn != null ? _txtAttn.Text.Trim() : "";
            _data.Phone = _txtPhone != null ? _txtPhone.Text.Trim() : "";
            _data.TermCode = _txtTerm != null ? _txtTerm.Text.Trim() : "";
            _data.AreaCode = _txtArea != null ? _txtArea.Text.Trim() : "";
            if (_dtStart != null) _data.ServiceStartDate = vStart;    // validated BEFORE the mutation block above
            if (_dtExpiry != null) _data.ServiceExpiryDate = vExpiry;
            foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _imh)
                _data.MoreHeader[kv.Key] = (kv.Value.Text ?? "").Trim();
            if (_imhDelAddress != null) _data.MoreHeader["DelAddress"] = _imhDelAddress.Text ?? "";
            _data.Note = _txtNote != null ? _txtNote.Text : "";
            _data.Remark1 = _txtRemark1 != null ? _txtRemark1.Text.Trim() : "";
            _data.Remark2 = _txtRemark2 != null ? _txtRemark2.Text.Trim() : "";

            // Preventive Maintenance + auto Next Service Date.
            if (_pmActive != null)
            {
                _data.PMActive = _pmActive.Checked;
                _data.PMIntervalType = _pmIntervalType.EditValue == null ? "NONE" : _pmIntervalType.EditValue.ToString();
                _data.PMIntervalValue = (int)_pmIntervalValue.Value;
                _data.PMStartDate = _pmStart.EditValue == null || _pmStart.EditValue == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(_pmStart.EditValue);
                _data.PMLastServiceDate = _pmLast.EditValue == null || _pmLast.EditValue == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(_pmLast.EditValue);
                _data.PMDept = _pmDept.EditValue == null ? "" : _pmDept.EditValue.ToString().Trim();
                _data.PMJob = _pmJob.EditValue == null ? "" : _pmJob.EditValue.ToString().Trim();
                _data.PMLocation = _pmLocation.Text.Trim();
                _data.PMNextServiceDate = ComputeNextService(_data);
                if (_pmNext != null) _pmNext.EditValue = (object)_data.PMNextServiceDate;
            }

            _savedOk = true;   // skip the unsaved-changes prompt on the close that follows
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // Department + Project dropdowns list AutoCount's own masters (Dept / Project tables).
        private void LoadDeptProjectLookups()
        {
            try
            {
                SluDept.Properties.DataSource = _db.GetDataTable(
                    "SELECT DeptNo, Description FROM [dbo].[Dept] ORDER BY DeptNo", false);
                SluDept.Properties.ValueMember = "DeptNo";
                SluDept.Properties.DisplayMember = "DeptNo";
            }
            catch { }
            try
            {
                SluProject.Properties.DataSource = _db.GetDataTable(
                    "SELECT ProjNo, Description FROM [dbo].[Project] ORDER BY ProjNo", false);
                SluProject.Properties.ValueMember = "ProjNo";
                SluProject.Properties.DisplayMember = "ProjNo";
            }
            catch { }
        }

        // "+" button on the Department dropdown: open AutoCount's OWN "New Department" form
        // (FormProjectEdit, ProjectType.Department) — the exact module AutoCount uses.
        private void SluDept_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            if (SluDept.Properties.ReadOnly) return;   // contract-rendered field
            OpenNativeDepartmentEditor();
        }

        // "+" button on the Project dropdown: open AutoCount's OWN "New Project" form.
        private void SluProject_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            if (SluProject.Properties.ReadOnly) return;   // contract-rendered field
            OpenNativeProjectEditor();
        }

        private void OpenNativeDepartmentEditor()
        {
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(
                        AutoCount.GeneralMaint.Project.ProjectType.Department, session);
                AutoCount.GeneralMaint.Project.DepartmentEntity entity =
                    cmd.NewDepartment(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Department))
                {
                    form.ShowDialog(this);
                }
                LoadDeptProjectLookups();
                string code = entity.Row["DeptNo"] == null ? "" : entity.Row["DeptNo"].ToString().Trim();
                if (code.Length > 0) SluDept.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Department failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void OpenNativeProjectEditor()
        {
            try
            {
                AutoCount.Authentication.UserSession session = AutoCount.Authentication.UserSession.CurrentUserSession;
                AutoCount.GeneralMaint.Project.ProjectDeptCommand cmd =
                    AutoCount.GeneralMaint.Project.ProjectDeptCommand.Create(
                        AutoCount.GeneralMaint.Project.ProjectType.Project, session);
                AutoCount.GeneralMaint.Project.ProjectEntity entity =
                    cmd.NewProject(AutoCount.GeneralMaint.Project.ProjectLevel.Top, "");
                using (AutoCount.GeneralMaint.Project.FormProjectEdit form =
                    new AutoCount.GeneralMaint.Project.FormProjectEdit(entity, AutoCount.GeneralMaint.Project.ProjectType.Project))
                {
                    form.ShowDialog(this);
                }
                LoadDeptProjectLookups();
                string code = entity.Row["ProjNo"] == null ? "" : entity.Row["ProjNo"].ToString().Trim();
                if (code.Length > 0) SluProject.EditValue = code;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Project failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // "Copy From..." — copy another service item's configuration into this one, EXCLUDING its
        // unique identity: Service Item No, Serial Number, provided-item serials, initial readings.
        private void barCopyFrom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            long key = PickServiceItem();
            if (key == 0) return;
            try
            {
                DataTable it = _db.GetDataTable("SELECT * FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + key, false);
                if (it.Rows.Count == 0) return;
                DataRow r = it.Rows[0];

                TxtDescription.Text = r["Description"] as string ?? "";
                SpnBillingDayOverride.Value = r["BillingDayOverride"] == DBNull.Value ? 0 : Convert.ToInt32(r["BillingDayOverride"]);
                string dept = r["DepartmentCode"] as string ?? "";
                string proj = r["JobCode"] as string ?? "";
                SluDept.EditValue = dept.Length == 0 ? null : (object)dept;
                SluProject.EditValue = proj.Length == 0 ? null : (object)proj;
                TxtLocation.Text = r["StockLocationCode"] as string ?? "";

                _itemCodes.Rows.Clear();
                DataTable ic = _db.GetDataTable(
                    "SELECT ItemCode, Description, Qty FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=" + key + " ORDER BY Pos", false);
                foreach (DataRow s in ic.Rows)
                {
                    DataRow nr = _itemCodes.NewRow();
                    nr["ItemCode"] = s["ItemCode"];
                    nr["Description"] = s["Description"];
                    nr["Qty"] = s["Qty"];
                    nr["SerialNumber"] = "";      // unique per physical machine — not copied
                    _itemCodes.Rows.Add(nr);
                }

                // Replacing the meters of a SAVED machine drops the vanished meters' reading history at
                // save — same warning as a manual meter delete, before anything is touched.
                if (_data != null && _data.ItemKey > 0 && _meters.Rows.Count > 0 &&
                    XtraMessageBox.Show("Copy From REPLACES this machine's current meters. Meters that disappear " +
                        "lose their reading history when you save. Continue?", "Copy From",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                _meters.Rows.Clear();
                DataTable mt = _db.GetDataTable(
                    "SELECT ItemMeterKey, MeterTypeCode, MeterRole, [Description], MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                    "RebateQtyInPercent, FOCQty, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialThreshold, WaivePartialAmount, WaiveScope " +
                    "FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + key + " ORDER BY ItemMeterKey", false);
                foreach (DataRow s in mt.Rows)
                {
                    DataRow nr = _meters.NewRow();
                    nr["MeterTypeCode"] = s["MeterTypeCode"];
                    nr["MeterRole"] = s["MeterRole"];
                    nr["Description"] = s["Description"];
                    nr["MinimumCharges"] = s["MinimumCharges"];
                    nr["ChargesRate"] = s["ChargesRate"];
                    nr["MeterMultiPriceCode"] = s["MeterMultiPriceCode"];
                    nr["RebateQtyInPercent"] = s["RebateQtyInPercent"];
                    nr["FOCQty"] = s["FOCQty"];
                    nr["InitialReading"] = 0m;    // unique per physical machine — starts fresh
                    nr["CustomTiers"] = LoadCustomTiersCsv(_db, Convert.ToInt64(s["ItemMeterKey"]));   // the deal's own ladder copies too
                    nr["WaiveFirstNMonths"] = s["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(s["WaiveFirstNMonths"]);
                    nr["WaiveTargetAmount"] = s["WaiveTargetAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(s["WaiveTargetAmount"]);
                    nr["WaivePartialThreshold"] = s["WaivePartialThreshold"] == DBNull.Value ? 0m : Convert.ToDecimal(s["WaivePartialThreshold"]);
                    nr["WaivePartialAmount"] = s["WaivePartialAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(s["WaivePartialAmount"]);
                    nr["WaiveScope"] = s["WaiveScope"] == DBNull.Value ? "BKCL" : Convert.ToString(s["WaiveScope"]);
                    _meters.Rows.Add(nr);
                }

                GridItemCodes.RefreshDataSource();
                GridMeters.RefreshDataSource();
            }
            catch (Exception ex) { XtraMessageBox.Show("Copy failed:\r\n" + ex.Message, "Error"); }
        }

        // Small picker dialog: choose the service item to copy from (searchable).
        private long PickServiceItem()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT i.ItemKey, i.ServiceItemNo, i.SerialNumber, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM [dbo].[zSCP2_Item] i " +
                    "LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +   // contract-less machines are copy sources too
                    "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                    "ORDER BY i.ServiceItemNo", false);
            }
            catch { return 0; }
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Copy From Service Item", dt, "ItemKey",
                new string[] { "ServiceItemNo", "SerialNumber", "CompanyName" },
                new string[] { "Service Item No", "Machine Serial", "Company Name" },
                new int[] { 130, 110, 220 });
            long v; return (k != null && k != DBNull.Value && long.TryParse(k.ToString(), out v)) ? v : 0;
        }

        // ===================== Spare Parts provided by this Service Item =====================
        // These are stored in zSCP2_ContractSparePart with ItemKey set, so they show read-only on the
        // parent contract. Built in code below the Meter group (reuses the contract's shared column
        // layout + compute so the two grids are identical).

        private DataTable _itemSpareParts;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _itemSpCheck;
        private DataTable _itemSpItemLookup;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _itemSpItemRepo;
        private DevExpress.XtraEditors.Repository.RepositoryItemSearchLookUpEdit _itemSpSerialRepo;
        private DataTable _itemSpSerialLookup;                          // full ItemSerialNo list (filtered per row)
        // --- multi-machine meters ---
        private DevExpress.XtraEditors.CheckEdit _chkMultiMachine;
        private DevExpress.XtraGrid.Columns.GridColumn _colMachineSerial;
        private DevExpress.XtraGrid.Columns.GridColumn _colMachineItemCode;
        private bool _suppressMultiEvt;
        // --- need rental ---
        private DevExpress.XtraEditors.CheckEdit _chkNeedRental;
        private bool _suppressRentalEvt;
        // --- ownership ---
        private DevExpress.XtraEditors.TextEdit _txtParentContractRO;   // read-only Contract No display
        private DevExpress.XtraEditors.TextEdit _txtCustomerRO;         // read-only Customer display
        private DevExpress.XtraEditors.LabelControl _lblExpiryBig;      // big red/green expiry badge
        private DevExpress.XtraBars.BarButtonItem _barChangeOwner;
        /// <summary>Enables the ribbon's Change Ownership action. Set by "Maintain Service Item" only —
        /// the contract editor keeps it off because its save loop re-parents items back to itself.</summary>
        public bool AllowOwnershipChange;
        private DevExpress.XtraGrid.GridControl _gridItemSp;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewItemSp;
        private DevExpress.XtraEditors.GroupControl _grpItemSp;   // "Item Provided" container — hidden (#5)

        private static void SetIcon(DevExpress.XtraEditors.SimpleButton b, System.Drawing.Image im)
        {
            b.ImageOptions.Image = im;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
        }

        // ================= Overhaul UI (all code-built; no strict-designer surgery) =================
        private void BuildOverhaulUI()
        {
            // 0. For an existing item, load its extended columns into _data first so every tab builder
            //    below populates from real values (the caller's LoadOneItem doesn't carry the extras).
            if (_data.ItemKey > 0) LoadExtrasFromDb(_data.ItemKey);

            // 1. Hide the removed pieces (kept in the designer to avoid risky deletion).
            LblLocation.Visible = false; TxtLocation.Visible = false;   // Stock Location removed from UI
            GrpItemCodes.Visible = false;                               // "Provided Items" grid removed

            // 1b. Reposition the designer header controls into a dense two-column layout.
            //     Rows are 24px apart (fields at ROW_Y, labels +3). Description drops to a full-width
            //     row at the bottom so it can stretch, with Inactive tucked at its right (as on the contract).
            LblServiceItemNo.Location = new System.Drawing.Point(14, 125);
            TxtServiceItemNo.Location = new System.Drawing.Point(120, 122);
            TxtServiceItemNo.Size = new System.Drawing.Size(200, 20);
            BtnAutoNo.Location = new System.Drawing.Point(326, 121);
            LblSerial.Location = new System.Drawing.Point(14, 197);
            TxtSerial.Location = new System.Drawing.Point(120, 194);
            TxtSerial.Size = new System.Drawing.Size(280, 20);
            // Billing Day Override sits in the LEFT column (row 9) - its hint sits beside it.
            LblBillDay.Location = new System.Drawing.Point(14, 341);
            SpnBillingDayOverride.Location = new System.Drawing.Point(120, 338);
            SpnBillingDayOverride.Size = new System.Drawing.Size(80, 20);
            LblBillDayHint.Location = new System.Drawing.Point(206, 341);
            LblDept.Location = new System.Drawing.Point(470, 317);
            SluDept.Location = new System.Drawing.Point(600, 314);
            SluDept.Size = new System.Drawing.Size(280, 20);
            LblJob.Location = new System.Drawing.Point(470, 341);
            SluProject.Location = new System.Drawing.Point(600, 338);
            SluProject.Size = new System.Drawing.Size(280, 20);
            // Description: full-width bottom row, stretched; Inactive at its right end.
            LblDesc.Location = new System.Drawing.Point(14, 365);
            TxtDescription.Location = new System.Drawing.Point(120, 362);
            TxtDescription.Size = new System.Drawing.Size(760, 20);
            ChkInactive.Location = new System.Drawing.Point(900, 364);

            // 2. Header: Item Code (SearchLookUpEdit over the Item master) — left column, row 2.
            MakeLbl("Item Code", 14, 173);
            _itemHdrLookup = zSCP2_Contract_Form.LoadItemLookup(_db);
            _sluItemCode = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluItemCode.Location = new System.Drawing.Point(120, 170);
            _sluItemCode.Size = new System.Drawing.Size(280, 20);
            _sluItemCode.Properties.NullText = "";
            _sluItemCode.Properties.DataSource = _itemHdrLookup;
            _sluItemCode.Properties.ValueMember = "ItemCode";
            _sluItemCode.Properties.DisplayMember = "ItemCode";
            _sluItemCode.EditValueChanged += new EventHandler(ItemCodeHeader_Changed);
            _sluItemCode.EditValueChanged += MarkDirty;
            this.Controls.Add(_sluItemCode); _sluItemCode.BringToFront();

            // 3. Header: Grade Code (SearchLookUpEdit over zSCP_LK_ServiceItemGrade) + "+" create — left column, row 4.
            MakeLbl("Grade Code", 14, 221);
            _sluGrade = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluGrade.Location = new System.Drawing.Point(120, 218);
            _sluGrade.Size = new System.Drawing.Size(280, 20);
            _sluGrade.Properties.NullText = "";
            _sluGrade.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo),
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Plus)});
            _sluGrade.Properties.ValueMember = "ServiceItemGradeCode";
            _sluGrade.Properties.DisplayMember = "ServiceItemGradeCode";
            _sluGrade.Properties.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(SluGrade_ButtonClick);
            _sluGrade.EditValueChanged += MarkDirty;
            LoadGradeLookup();
            this.Controls.Add(_sluGrade); _sluGrade.BringToFront();

            // 3b. Contract-mirrored context fields (same as the contract, minus billing checkboxes + Contract Value).
            BuildContractContextHeader();
            // Bound to a contract: Contract Type follows the contract and is READ-ONLY here (edit it on
            // the contract). The other context fields stay editable as per-item overrides.
            if (!string.IsNullOrEmpty(_parentContractNo)) _sluCType.Properties.ReadOnly = true;

            // 4. Tab control below the header.
            _tabMain = new DevExpress.XtraTab.XtraTabControl();
            _tabMain.Location = new System.Drawing.Point(5, 388);
            _tabMain.Size = new System.Drawing.Size(this.ClientSize.Width - 10, this.ClientSize.Height - 393);
            _tabMain.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _pgItemMeter = new DevExpress.XtraTab.XtraTabPage(); _pgItemMeter.Text = "Item & Meter";
            _pgPreventive = new DevExpress.XtraTab.XtraTabPage(); _pgPreventive.Text = "Preventive";
            _pgMoreHeader = new DevExpress.XtraTab.XtraTabPage(); _pgMoreHeader.Text = "More Header";
            _pgDebtorHist = new DevExpress.XtraTab.XtraTabPage(); _pgDebtorHist.Text = "Debtors Ownership History";
            _pgNote = new DevExpress.XtraTab.XtraTabPage(); _pgNote.Text = "Note";
            _pgRemark = new DevExpress.XtraTab.XtraTabPage(); _pgRemark.Text = "Remarks";
            _tabMain.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
                _pgItemMeter, _pgPreventive, _pgMoreHeader, _pgDebtorHist, _pgNote, _pgRemark });
            this.Controls.Add(_tabMain);

            // 5. Tab 1 "Item & Meter": the Meter grid fills the whole tab. The Item Provided grid is
            //    still BUILT (so _itemSpareParts keeps loading/saving existing rows untouched) but is
            //    HIDDEN — the header's Item Code + Machine Serial No define the machine now.
            BuildItemSparePartsGrid(_pgItemMeter);
            if (_grpItemSp != null) _grpItemSp.Visible = false;
            GrpMeters.Dock = System.Windows.Forms.DockStyle.Fill;
            _pgItemMeter.Controls.Add(GrpMeters);
            GrpMeters.BringToFront();
            BuildMultiMachineMeters();              // per-unit meters (checkbox + Machine Serial column)

            // 6. Tab 3 "More Header" + Tab 5/6 Note/Remarks + Tab 2 Preventive.
            BuildItemMoreHeaderTab(_pgMoreHeader);
            BuildNoteRemarkTabs();
            BuildPreventiveTab(_pgPreventive);
            BuildDebtorHistoryTab(_pgDebtorHist);
            // Billing History: what invoices this machine's meters generated (read-only, shared impl).
            DevExpress.XtraTab.XtraTabPage pgBillHist = new DevExpress.XtraTab.XtraTabPage();
            pgBillHist.Text = "Billing History";
            _tabMain.TabPages.Add(pgBillHist);
            ServiceContractPhotocopier.Classes.ScpBillingHistory.BuildTab(pgBillHist, _db, "i.ItemKey", _data.ItemKey);
            ExtendItemRibbon();

            // 6b. New item under a known contract: inherit that contract's context so the header isn't blank.
            if (_data.ItemKey <= 0 && !string.IsNullOrEmpty(_parentContractNo)) PrefillContextFromContract(_parentContractNo);

            // 7. Populate the header + More Header + Note/Remarks controls from _data (loaded at step 0).
            _sluItemCode.EditValue = string.IsNullOrEmpty(_data.ItemCode) ? null : (object)_data.ItemCode;
            _sluGrade.EditValue = string.IsNullOrEmpty(_data.GradeCode) ? null : (object)_data.GradeCode;
            _sluCType.EditValue = string.IsNullOrEmpty(_data.ContractTypeCode) ? null : (object)_data.ContractTypeCode;
            _sluAgentI.EditValue = string.IsNullOrEmpty(_data.StaffCode) ? null : (object)_data.StaffCode;
            _txtRefNo.Text = _data.ReferenceNo ?? "";
            _txtAddr.Text = _data.Address1 ?? "";
            _txtAttn.Text = _data.Attention ?? "";
            _txtPhone.Text = _data.Phone ?? "";
            _txtTerm.Text = _data.TermCode ?? "";
            _txtArea.Text = _data.AreaCode ?? "";
            _dtStart.EditValue = _data.ServiceStartDate.HasValue ? (object)_data.ServiceStartDate.Value : null;
            _dtExpiry.EditValue = _data.ServiceExpiryDate.HasValue ? (object)_data.ServiceExpiryDate.Value : null;
            // Dept/Project were populated from _data BEFORE the contract prefill ran (they're designer
            // controls set in OnFormLoad), so push the inherited values in now for a new item.
            if (_data.ItemKey <= 0)
            {
                if (SluDept.EditValue == null && !string.IsNullOrEmpty(_data.DepartmentCode)) SluDept.EditValue = _data.DepartmentCode;
                if (SluProject.EditValue == null && !string.IsNullOrEmpty(_data.JobCode)) SluProject.EditValue = _data.JobCode;
            }
            foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _imh)
                if (_data.MoreHeader.ContainsKey(kv.Key)) kv.Value.Text = _data.MoreHeader[kv.Key];
            if (_imhDelAddress != null && _data.MoreHeader.ContainsKey("DelAddress")) _imhDelAddress.Text = _data.MoreHeader["DelAddress"];
            _txtNote.Text = _data.Note ?? "";
            _txtRemark1.Text = _data.Remark1 ?? "";
            _txtRemark2.Text = _data.Remark2 ?? "";
            UpdateExpiryBadge();
            _dirty = false;   // programmatic population above must not look like a user edit
        }

        private void MarkDirty(object sender, EventArgs e) { _dirty = true; }

        private DevExpress.XtraEditors.LabelControl MakeLbl(string text, int x, int y)
        {
            DevExpress.XtraEditors.LabelControl l = new DevExpress.XtraEditors.LabelControl();
            l.Text = text; l.Location = new System.Drawing.Point(x, y);
            this.Controls.Add(l); l.BringToFront();
            return l;
        }

        private DevExpress.XtraEditors.TextEdit MakeTxt(int x, int y, int w, int maxLen)
        {
            DevExpress.XtraEditors.TextEdit t = new DevExpress.XtraEditors.TextEdit();
            t.Location = new System.Drawing.Point(x, y);
            t.Size = new System.Drawing.Size(w, 20);
            if (maxLen > 0) t.Properties.MaxLength = maxLen;
            t.EditValueChanged += MarkDirty;
            this.Controls.Add(t); t.BringToFront();
            return t;
        }

        private DevExpress.XtraEditors.DateEdit MakeDate(int x, int y, int w)
        {
            DevExpress.XtraEditors.DateEdit d = new DevExpress.XtraEditors.DateEdit();
            d.Location = new System.Drawing.Point(x, y);
            d.Size = new System.Drawing.Size(w, 20);
            d.EditValue = null;
            d.EditValueChanged += MarkDirty;
            this.Controls.Add(d); d.BringToFront();
            return d;
        }

        // Contract-mirrored header context: same fields as the contract, minus the billing checkboxes and
        // Contract Value. Left column rows 6-9 (Contract Type / Reference No / Service Start..To / Agent),
        // right column rows 1-5 (Address / Attention / Phone / Term / Area).
        private void BuildContractContextHeader()
        {
            // Contract Type (SearchLookUpEdit + "+" create, mirroring the contract's SluContractType).
            MakeLbl("Contract Type", 14, 245);
            _sluCType = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluCType.Location = new System.Drawing.Point(120, 242);
            _sluCType.Size = new System.Drawing.Size(280, 20);
            _sluCType.Properties.NullText = "";
            _sluCType.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo),
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Plus)});
            _sluCType.Properties.ValueMember = "ServiceContractTypeCode";
            _sluCType.Properties.DisplayMember = "ServiceContractTypeCode";
            _sluCType.Properties.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(SluCType_ButtonClick);
            _sluCType.EditValueChanged += MarkDirty;
            LoadContractTypeLookupI();
            this.Controls.Add(_sluCType); _sluCType.BringToFront();

            // Reference No.
            MakeLbl("Reference No", 14, 269);
            _txtRefNo = MakeTxt(120, 266, 280, 80);

            // Service Start Date [ To ] Expiry.
            MakeLbl("Service Start Date", 14, 293);
            _dtStart = MakeDate(120, 290, 125);
            MakeLbl("To", 253, 293);
            _dtExpiry = MakeDate(275, 290, 125);

            // Agent (SearchLookUpEdit over AutoCount SalesAgent, mirroring the contract's SluAgent).
            MakeLbl("Agent", 14, 317);
            _sluAgentI = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluAgentI.Location = new System.Drawing.Point(120, 314);
            _sluAgentI.Size = new System.Drawing.Size(280, 20);
            _sluAgentI.Properties.NullText = "";
            _sluAgentI.Properties.ValueMember = "SalesAgent";
            _sluAgentI.Properties.DisplayMember = "SalesAgent";
            _sluAgentI.EditValueChanged += MarkDirty;
            LoadAgentLookupI();
            this.Controls.Add(_sluAgentI); _sluAgentI.BringToFront();

            // Right column: Address (memo spanning rows 1-3) + Attention / Phone / Term / Area.
            MakeLbl("Address", 470, 149);
            _txtAddr = new DevExpress.XtraEditors.MemoEdit();
            _txtAddr.Location = new System.Drawing.Point(600, 146);
            _txtAddr.Size = new System.Drawing.Size(280, 62);
            _txtAddr.EditValueChanged += MarkDirty;
            this.Controls.Add(_txtAddr); _txtAddr.BringToFront();

            MakeLbl("Attention", 470, 221); _txtAttn = MakeTxt(600, 218, 280, 100);
            MakeLbl("Phone", 470, 245); _txtPhone = MakeTxt(600, 242, 280, 40);
            MakeLbl("Term", 470, 269); _txtTerm = MakeTxt(600, 266, 280, 40);
            MakeLbl("Area", 470, 293); _txtArea = MakeTxt(600, 290, 280, 40);

            // Big expiry badge (top-right): GREEN while the service is still covered, RED once the
            // expiry date has passed. Follows the header's effective expiry live.
            MakeLbl("Service Expiry", 920, 150);
            _lblExpiryBig = new DevExpress.XtraEditors.LabelControl();
            _lblExpiryBig.Location = new System.Drawing.Point(920, 168);
            _lblExpiryBig.Appearance.Font = new System.Drawing.Font("Tahoma", 22F, System.Drawing.FontStyle.Bold);
            _lblExpiryBig.Appearance.Options.UseFont = true;
            _lblExpiryBig.Appearance.Options.UseForeColor = true;
            _lblExpiryBig.Text = "";
            this.Controls.Add(_lblExpiryBig); _lblExpiryBig.BringToFront();
            _dtExpiry.EditValueChanged += delegate { UpdateExpiryBadge(); };
        }

        // Green = not yet expired; red = past expiry; gray = no expiry set.
        private void UpdateExpiryBadge()
        {
            if (_lblExpiryBig == null || _dtExpiry == null) return;
            if (_dtExpiry.EditValue == null || _dtExpiry.EditValue == DBNull.Value)
            {
                _lblExpiryBig.Text = "(no expiry)";
                _lblExpiryBig.Appearance.ForeColor = System.Drawing.Color.Gray;
                return;
            }
            DateTime exp = Convert.ToDateTime(_dtExpiry.EditValue).Date;
            _lblExpiryBig.Text = exp.ToString("dd/MM/yyyy");
            _lblExpiryBig.Appearance.ForeColor = exp < DateTime.Today
                ? System.Drawing.Color.Firebrick
                : System.Drawing.Color.Green;
        }

        private void LoadContractTypeLookupI()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT ServiceContractTypeCode, Description FROM [dbo].[zSCP_LK_ServiceContractType] " +
                    "WHERE Inactive = 'N' ORDER BY ServiceContractTypeCode", false);
            }
            catch { dt = new DataTable(); }
            _sluCType.Properties.DataSource = dt;
        }

        private void LoadAgentLookupI()
        {
            DataTable dt;
            try
            {
                dt = _db.GetDataTable(
                    "SELECT SalesAgent, ISNULL(Description,'') AS Description FROM [dbo].[SalesAgent] " +
                    "WHERE IsActive = 'T' ORDER BY SalesAgent", false);
            }
            catch { dt = new DataTable(); }
            _sluAgentI.Properties.DataSource = dt;
        }

        // "+" on Contract Type opens the master and reselects the freshly-created code (mirrors the contract form).
        private void SluCType_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            if (_sluCType.Properties.ReadOnly) return;   // bound to a contract: type is contract-owned
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceContractType_Form(_db, null))
                {
                    if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        LoadContractTypeLookupI();
                        if (!string.IsNullOrEmpty(f.SavedCode)) _sluCType.EditValue = f.SavedCode;
                    }
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Could not open Contract Type maintenance:\r\n" + ex.Message, "Error"); }
        }

        // New item under an existing contract: copy that contract's context into _data so the header shows it.
        // Fill-if-empty only (a Copy-to-New template keeps its own values); every field stays editable.
        private void PrefillContextFromContract(string contractNo)
        {
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT TOP 1 ContractTypeCode, StaffCode, ServiceStartDate, ServiceExpiryDate, " +
                    "Address1, Attention, Phone, TermCode, AreaCode, DeptNo, ProjNo, " +
                    "[" + string.Join("], [", _imhCols) + "] " +
                    "FROM [dbo].[zSCP2_Contract] WHERE ContractNo=" +
                    "'" + contractNo.Replace("'", "''") + "'", false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                if (string.IsNullOrEmpty(_data.ContractTypeCode)) _data.ContractTypeCode = AsS(r["ContractTypeCode"]);
                if (string.IsNullOrEmpty(_data.StaffCode)) _data.StaffCode = AsS(r["StaffCode"]);
                if (string.IsNullOrEmpty(_data.Address1)) _data.Address1 = AsS(r["Address1"]);
                if (string.IsNullOrEmpty(_data.Attention)) _data.Attention = AsS(r["Attention"]);
                if (string.IsNullOrEmpty(_data.Phone)) _data.Phone = AsS(r["Phone"]);
                if (string.IsNullOrEmpty(_data.TermCode)) _data.TermCode = AsS(r["TermCode"]);
                if (string.IsNullOrEmpty(_data.AreaCode)) _data.AreaCode = AsS(r["AreaCode"]);
                if (string.IsNullOrEmpty(_data.DepartmentCode)) _data.DepartmentCode = AsS(r["DeptNo"]);
                if (string.IsNullOrEmpty(_data.JobCode)) _data.JobCode = AsS(r["ProjNo"]);   // item's JobCode stores the AutoCount ProjNo
                if (!_data.ServiceStartDate.HasValue && r["ServiceStartDate"] != DBNull.Value) _data.ServiceStartDate = Convert.ToDateTime(r["ServiceStartDate"]);
                if (!_data.ServiceExpiryDate.HasValue && r["ServiceExpiryDate"] != DBNull.Value) _data.ServiceExpiryDate = Convert.ToDateTime(r["ServiceExpiryDate"]);
                foreach (string c in _imhCols)
                {
                    string cur; _data.MoreHeader.TryGetValue(c, out cur);
                    if (string.IsNullOrEmpty(cur)) _data.MoreHeader[c] = AsS(r[c]);
                }
            }
            catch { }
        }

        // Standalone: the user picked a contract in the Contract No dropdown — pull that contract's
        // shared fields straight into the CONTROLS (an explicit pick means "inherit from this one",
        // so values are overwritten; the user can still edit everything afterwards).
        private void ApplyContractContextByKey(long contractKey)
        {
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT TOP 1 ContractTypeCode, StaffCode, DebtorCode, ServiceStartDate, ServiceExpiryDate, " +
                    "Address1, Attention, Phone, TermCode, AreaCode, DeptNo, ProjNo, " +
                    "[" + string.Join("], [", _imhCols) + "] " +
                    "FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + contractKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                if (_sluCType != null) _sluCType.EditValue = string.IsNullOrEmpty(AsS(r["ContractTypeCode"])) ? null : (object)AsS(r["ContractTypeCode"]);
                if (_sluAgentI != null) _sluAgentI.EditValue = string.IsNullOrEmpty(AsS(r["StaffCode"])) ? null : (object)AsS(r["StaffCode"]);
                if (_dtStart != null) _dtStart.EditValue = r["ServiceStartDate"] == DBNull.Value ? null : r["ServiceStartDate"];
                if (_dtExpiry != null) _dtExpiry.EditValue = r["ServiceExpiryDate"] == DBNull.Value ? null : r["ServiceExpiryDate"];
                if (_txtAddr != null) _txtAddr.Text = AsS(r["Address1"]);
                if (_txtAttn != null) _txtAttn.Text = AsS(r["Attention"]);
                if (_txtPhone != null) _txtPhone.Text = AsS(r["Phone"]);
                if (_txtTerm != null) _txtTerm.Text = AsS(r["TermCode"]);
                if (_txtArea != null) _txtArea.Text = AsS(r["AreaCode"]);
                SluDept.EditValue = string.IsNullOrEmpty(AsS(r["DeptNo"])) ? null : (object)AsS(r["DeptNo"]);
                SluProject.EditValue = string.IsNullOrEmpty(AsS(r["ProjNo"])) ? null : (object)AsS(r["ProjNo"]);
                // Show the contract's customer in the (disabled) Customer picker so the link is visible.
                if (_lkCustomer != null) _lkCustomer.EditValue = string.IsNullOrEmpty(AsS(r["DebtorCode"])) ? null : (object)AsS(r["DebtorCode"]);
                foreach (string c in _imhCols)
                    if (_imh.ContainsKey(c)) _imh[c].Text = AsS(r[c]);
                if (_imhDelAddress != null) _imhDelAddress.Text = AsS(r["DelAddress"]);
            }
            catch { }
        }

        private static readonly string[] _imhCols = {
            "City","PostalCode","State","Country","Fax","Ref1","Ref2","Ref3","Ref4",
            "DelBranchCode","DelBranchName","DelAddress","DelCity","DelPostalCode","DelState","DelCountry",
            "DelPhone","DelFax","DelEmail","DelContactPerson" };

        private void LoadExtrasFromDb(long itemKey) { LoadExtras(_db, _data, itemKey); }

        // Static so zSCP2_Contract_Form.LoadOneItem hydrates the SAME extras for every item in a
        // contract: the contract save loop persists ALL items, and persisting an item whose extras
        // were never loaded would wipe them (empty defaults, no LoadedCtx snapshot).
        internal static void LoadExtras(DBSetting db, ItemEditData data, long itemKey)
        {
            try
            {
                // Context fields (ContractType/Agent/dates/address/…) fall back to the parent contract when the
                // item hasn't overridden them, so legacy items and items-under-a-contract show the contract's data.
                // Reference No stays item-specific (a per-document reference), never inherited.
                // More Header columns inherit the SAME way — previously they read only i.[col], so an item
                // with no own values showed an EMPTY More Header while its contract's tab was full.
                System.Text.StringBuilder mh = new System.Text.StringBuilder();
                foreach (string cCol in _imhCols)
                {
                    if (mh.Length > 0) mh.Append(", ");
                    mh.Append("COALESCE(NULLIF(i.[").Append(cCol).Append("],''), c.[").Append(cCol).Append("], '') AS [").Append(cCol).Append("]");
                }
                string iMh = mh.ToString();
                DataTable dt = db.GetDataTable(
                    "SELECT i.ItemCode, i.GradeCode, i.PurchaseDate, i.ServiceTypeCode, i.Note, i.Remark1, i.Remark2, " + iMh +
                    ", i.ReferenceNo AS ReferenceNo" +
                    ", COALESCE(NULLIF(i.ContractTypeCode,''), c.ContractTypeCode, '') AS ContractTypeCode" +
                    ", COALESCE(NULLIF(i.StaffCode,''), c.StaffCode, '') AS StaffCode" +
                    ", COALESCE(i.ServiceStartDate, c.ServiceStartDate) AS ServiceStartDate" +
                    ", COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) AS ServiceExpiryDate" +
                    ", COALESCE(NULLIF(i.Address1,''), c.Address1, '') AS Address1" +
                    ", COALESCE(NULLIF(i.Attention,''), c.Attention, '') AS Attention" +
                    ", COALESCE(NULLIF(i.Phone,''), c.Phone, '') AS Phone" +
                    ", COALESCE(NULLIF(i.TermCode,''), c.TermCode, '') AS TermCode" +
                    ", COALESCE(NULLIF(i.AreaCode,''), c.AreaCode, '') AS AreaCode" +
                    ", i.PMActive, i.PMIntervalType, i.PMIntervalValue, i.PMStartDate, i.PMLastServiceDate, i.PMNextServiceDate, i.PMDept, i.PMJob, i.PMLocation" +
                    ", i.MultiMachine" +
                    " FROM [dbo].[zSCP2_Item] i LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey=i.ContractKey" +
                    " WHERE i.ItemKey=" + itemKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                data.ItemCode = AsS(r["ItemCode"]); data.GradeCode = AsS(r["GradeCode"]);
                data.PurchaseDate = r["PurchaseDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PurchaseDate"]);
                data.ServiceTypeCode = AsS(r["ServiceTypeCode"]);
                data.Note = AsS(r["Note"]); data.Remark1 = AsS(r["Remark1"]); data.Remark2 = AsS(r["Remark2"]);
                foreach (string c in _imhCols) data.MoreHeader[c] = AsS(r[c]);
                data.ReferenceNo = AsS(r["ReferenceNo"]); data.ContractTypeCode = AsS(r["ContractTypeCode"]);
                data.StaffCode = AsS(r["StaffCode"]); data.Address1 = AsS(r["Address1"]);
                data.Attention = AsS(r["Attention"]); data.Phone = AsS(r["Phone"]);
                data.TermCode = AsS(r["TermCode"]); data.AreaCode = AsS(r["AreaCode"]);
                data.ServiceStartDate = r["ServiceStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ServiceStartDate"]);
                if (data.ServiceExpiryDate == null && r["ServiceExpiryDate"] != DBNull.Value) data.ServiceExpiryDate = Convert.ToDateTime(r["ServiceExpiryDate"]);
                // Snapshot the effective (inherited) context so the save can detect what the user changed.
                data.LoadedCtx["ContractTypeCode"] = data.ContractTypeCode;
                data.LoadedCtx["StaffCode"] = data.StaffCode;
                data.LoadedCtx["Address1"] = data.Address1;
                data.LoadedCtx["Attention"] = data.Attention;
                data.LoadedCtx["Phone"] = data.Phone;
                data.LoadedCtx["TermCode"] = data.TermCode;
                data.LoadedCtx["AreaCode"] = data.AreaCode;
                foreach (string c in _imhCols) data.LoadedCtx[c] = data.MoreHeader.ContainsKey(c) ? data.MoreHeader[c] : "";
                data.LoadedStart = data.ServiceStartDate;
                data.LoadedExpiry = data.ServiceExpiryDate;
                data.LoadedCtxValid = true;
                data.PMActive = AsS(r["PMActive"]) == "Y";
                data.PMIntervalType = AsS(r["PMIntervalType"]);
                data.PMIntervalValue = r["PMIntervalValue"] == DBNull.Value ? 0 : Convert.ToInt32(r["PMIntervalValue"]);
                data.PMStartDate = r["PMStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMStartDate"]);
                data.PMLastServiceDate = r["PMLastServiceDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMLastServiceDate"]);
                data.PMNextServiceDate = r["PMNextServiceDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMNextServiceDate"]);
                data.PMDept = AsS(r["PMDept"]); data.PMJob = AsS(r["PMJob"]); data.PMLocation = AsS(r["PMLocation"]);
                data.MultiMachine = AsS(r["MultiMachine"]) == "Y";
            }
            catch
            {
                // LoadedCtxValid stays false: the contract save loop SKIPS PersistItemExtras for this
                // item — persisting the empty defaults here would silently wipe its Grade / Note / PM /
                // date + context overrides. (The save surfaces a "kept unchanged" note when it skips.)
                data.LoadedCtxValid = false;
            }
        }

        private static string AsS(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }

        // Clipboard ribbon group on the Item Provided grid (mirrors the contract's Clipboard group).
        private void ExtendItemRibbon()
        {
            // (Clipboard group removed — it operated on the Item Provided grid, which is hidden now.)

            // Ownership: transfer the item to another contract / customer (existing items, list only —
            // the contract editor's save loop would re-parent the item back, so it stays off there).
            _barChangeOwner = MakeBar("Change" + System.Environment.NewLine + "Ownership", BarChangeOwner_Click);
            _barChangeOwner.Enabled = AllowOwnershipChange && _data.ItemKey > 0;
            RibbonCtl.Items.AddRange(new DevExpress.XtraBars.BarItem[] { _barChangeOwner });
            DevExpress.XtraBars.Ribbon.RibbonPageGroup grpOwn = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            grpOwn.Text = "Ownership";
            grpOwn.ItemLinks.Add(_barChangeOwner);
            ribbonPageHome.Groups.Add(grpOwn);

            // Demo group (mirrors the contract's): random-fill every header field. NEW items only.
            _barDemoFill = MakeBar("Demo Fill" + System.Environment.NewLine + "(Random)", barDemoFill_ItemClick);
            _barDemoFill.Enabled = (_data.ItemKey <= 0);
            RibbonCtl.Items.AddRange(new DevExpress.XtraBars.BarItem[] { _barDemoFill });
            DevExpress.XtraBars.Ribbon.RibbonPageGroup grpDemo = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            grpDemo.Text = "Demo";
            grpDemo.ItemLinks.Add(_barDemoFill);
            ribbonPageHome.Groups.Add(grpDemo);

            try
            {
                float dpi = 96f; try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img = AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                _barDemoFill.ImageOptions.Image = img.GetLargeImage_Refresh();
                _barDemoFill.ImageOptions.LargeImage = img.GetLargeImage_Refresh();
                _barChangeOwner.ImageOptions.Image = img.GetLargeImage_Edit();
                _barChangeOwner.ImageOptions.LargeImage = img.GetLargeImage_Edit();
            }
            catch { }
            _barDemoFill.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            _barChangeOwner.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
        }

        // ===================== Demo Fill (random test data) =====================

        private DevExpress.XtraBars.BarButtonItem _barDemoFill;
        private static readonly Random _rngI = new Random();
        private static readonly string[] _demoWordsI = {
            "Alpha","Beta","Gamma","Delta","Prime","Nova","Metro","Summit","Vertex","Pioneer",
            "Copier","Printer","MFP","Toner","Drum","Fuser","Roller","Panel","Sensor","Unit" };
        private static readonly string[] _demoCitiesI = { "Johor Bahru","Kuala Lumpur","Penang","Ipoh","Melaka","Shah Alam","Klang" };
        private static readonly string[] _demoStatesI = { "Johor","Selangor","Penang","Perak","Melaka","Kedah","Pahang" };

        private string DWI() { return _demoWordsI[_rngI.Next(_demoWordsI.Length)]; }
        private string RandRefI() { return DWI() + "-" + _rngI.Next(1000, 9999); }

        // Picks a random value from a SearchLookUpEdit's bound datasource (null-safe).
        private object RandomLookupValueI(DevExpress.XtraEditors.SearchLookUpEdit slu)
        {
            if (slu == null) return null;
            DataTable dt = slu.Properties.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0) return null;
            string vm = slu.Properties.ValueMember;
            if (string.IsNullOrEmpty(vm) || !dt.Columns.Contains(vm)) return null;
            return dt.Rows[_rngI.Next(dt.Rows.Count)][vm];
        }

        private void barDemoFill_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try { DemoFillItem(); }
            catch (Exception ex) { XtraMessageBox.Show("Demo fill failed:\r\n" + ex.Message, "Demo"); }
        }

        // Fills every header field with random values (different each click). Per the agreed behaviour
        // this does NOT add grid rows — header + More Header + Note/Remarks only.
        private void DemoFillItem()
        {
            TxtSerial.Text = "SN" + _rngI.Next(100000, 999999);
            TxtDescription.Text = "Demo " + DWI() + " " + _rngI.Next(100, 999);

            if (_sluItemCode != null) _sluItemCode.EditValue = RandomLookupValueI(_sluItemCode);
            if (_sluGrade != null) _sluGrade.EditValue = RandomLookupValueI(_sluGrade);
            if (_sluCType != null) _sluCType.EditValue = RandomLookupValueI(_sluCType);
            if (_sluAgentI != null) _sluAgentI.EditValue = RandomLookupValueI(_sluAgentI);
            if (SluDept != null) SluDept.EditValue = RandomLookupValueI(SluDept);
            if (SluProject != null) SluProject.EditValue = RandomLookupValueI(SluProject);

            if (_txtRefNo != null) _txtRefNo.Text = RandRefI();
            DateTime sd = DateTime.Today.AddDays(-_rngI.Next(0, 60));
            if (_dtStart != null) _dtStart.EditValue = sd;
            if (_dtExpiry != null) _dtExpiry.EditValue = sd.AddMonths(_rngI.Next(6, 36));

            if (_txtAddr != null) _txtAddr.Text = _rngI.Next(1, 200) + ", Jalan " + DWI() + Environment.NewLine + _demoCitiesI[_rngI.Next(_demoCitiesI.Length)];
            if (_txtAttn != null) _txtAttn.Text = "Mr. " + DWI();
            if (_txtPhone != null) _txtPhone.Text = "01" + _rngI.Next(0, 9) + "-" + _rngI.Next(100, 999) + _rngI.Next(1000, 9999);
            if (_txtTerm != null) _txtTerm.Text = "C.O.D.";
            if (_txtArea != null) _txtArea.Text = _demoStatesI[_rngI.Next(_demoStatesI.Length)];

            SpnBillingDayOverride.Value = _rngI.Next(0, 28);

            // More Header
            ImhSet("City", _demoCitiesI[_rngI.Next(_demoCitiesI.Length)]);
            ImhSet("State", _demoStatesI[_rngI.Next(_demoStatesI.Length)]);
            ImhSet("PostalCode", _rngI.Next(10000, 99999).ToString());
            ImhSet("Country", "Malaysia");
            ImhSet("Ref1", RandRefI()); ImhSet("Ref2", RandRefI()); ImhSet("Ref3", RandRefI()); ImhSet("Ref4", RandRefI());

            if (_txtNote != null) _txtNote.Text = "Auto-generated demo note " + DWI() + " " + _rngI.Next(1000, 9999);
            if (_txtRemark1 != null) _txtRemark1.Text = "Remark " + DWI();
            if (_txtRemark2 != null) _txtRemark2.Text = "Remark " + DWI();

            _dirty = true;
        }

        private void ImhSet(string col, string val)
        {
            if (_imh != null && _imh.ContainsKey(col) && _imh[col] != null) _imh[col].Text = val;
        }

        private DevExpress.XtraBars.BarButtonItem MakeBar(string caption, DevExpress.XtraBars.ItemClickEventHandler h)
        {
            DevExpress.XtraBars.BarButtonItem b = new DevExpress.XtraBars.BarButtonItem();
            b.Caption = caption;
            // Name must stay an identifier: captions may carry line breaks/brackets for ribbon display.
            System.Text.StringBuilder nm = new System.Text.StringBuilder("bar_");
            foreach (char ch in caption) if (char.IsLetterOrDigit(ch)) nm.Append(ch);
            b.Name = nm.ToString();
            b.ItemClick += h;
            return b;
        }

        // (Item Provided clipboard copy/paste handlers removed with the ribbon Clipboard group — the
        // grid itself is hidden now.)

        // Single source of truth for the item's EXTENDED columns (Item Code, Grade, More Header, Note,
        // Remarks). Called by BOTH persistence paths (contract editor upsert loop + standalone
        // InsertItemTree) right after the core item row + its ItemKey exist, inside their transaction.
        internal static void PersistItemExtras(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, ItemEditData d, long itemKey)
        {
            // ---- inherit-dedupe --------------------------------------------------------------------
            // A bound item's context columns store ONLY true overrides. Anything equal to the parent
            // contract is stored ''/NULL so LoadExtrasFromDb keeps COALESCE-inheriting it — that is what
            // makes a later contract edit show up on all its items automatically. Untouched fields
            // (value == the LoadedCtx snapshot) keep their raw stored value, so a contract edit in the
            // SAME save (contract row updates before the item loop) can't masquerade as an override.
            string[] ctxScalars = { "ContractTypeCode", "StaffCode", "Address1", "Attention", "Phone", "TermCode", "AreaCode" };
            System.Collections.Generic.List<string> ctxAll = new System.Collections.Generic.List<string>(ctxScalars);
            ctxAll.AddRange(_imhCols);
            bool hasC = false;
            System.Collections.Generic.Dictionary<string, string> rawCtx = new System.Collections.Generic.Dictionary<string, string>();
            System.Collections.Generic.Dictionary<string, string> curCtx = new System.Collections.Generic.Dictionary<string, string>();
            object rawStart = DBNull.Value, rawExpiry = DBNull.Value;
            DateTime? curStart = null, curExpiry = null;
            System.Text.StringBuilder qb = new System.Text.StringBuilder(
                "SELECT CASE WHEN c.ContractKey IS NULL THEN 0 ELSE 1 END AS HasC, " +
                "i.ServiceStartDate AS [r_Start], i.ServiceExpiryDate AS [r_Expiry], " +
                "c.ServiceStartDate AS [c_Start], c.ServiceExpiryDate AS [c_Expiry]");
            foreach (string c in ctxAll)
                qb.Append(", ISNULL(i.[").Append(c).Append("],'') AS [r_").Append(c)
                  .Append("], ISNULL(c.[").Append(c).Append("],'') AS [c_").Append(c).Append("]");
            qb.Append(" FROM [dbo].[zSCP2_Item] i LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey=i.ContractKey WHERE i.ItemKey=@ik");
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(qb.ToString(), conn, tx))
            {
                q.Parameters.AddWithValue("@ik", itemKey);
                using (System.Data.SqlClient.SqlDataReader r = q.ExecuteReader())
                {
                    if (r.Read())
                    {
                        hasC = Convert.ToInt32(r["HasC"]) == 1;
                        foreach (string c in ctxAll)
                        {
                            rawCtx[c] = Convert.ToString(r["r_" + c]);
                            curCtx[c] = Convert.ToString(r["c_" + c]);
                        }
                        rawStart = r["r_Start"]; rawExpiry = r["r_Expiry"];
                        curStart = r["c_Start"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["c_Start"]);
                        curExpiry = r["c_Expiry"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["c_Expiry"]);
                    }
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("UPDATE [dbo].[zSCP2_Item] SET ItemCode=@ItemCode, GradeCode=@GradeCode, ")
              .Append("PurchaseDate=@PurchaseDate, ServiceTypeCode=@ServiceTypeCode, ")
              .Append("Note=@Note, Remark1=@Remark1, Remark2=@Remark2, ")
              .Append("ReferenceNo=@ReferenceNo, ContractTypeCode=@ContractTypeCode, StaffCode=@StaffCode, ")
              .Append("ServiceStartDate=@ServiceStartDate, ServiceExpiryDate=@ServiceExpiryDate, ")
              .Append("Address1=@Address1, Attention=@Attention, Phone=@Phone, TermCode=@TermCode, AreaCode=@AreaCode, ")
              .Append("PMActive=@PMActive, PMIntervalType=@PMIntervalType, PMIntervalValue=@PMIntervalValue, ")
              .Append("PMStartDate=@PMStartDate, PMLastServiceDate=@PMLastServiceDate, PMNextServiceDate=@PMNextServiceDate, ")
              .Append("PMDept=@PMDept, PMJob=@PMJob, PMLocation=@PMLocation, MultiMachine=@MultiMachine");
            foreach (string c in _imhCols) sb.Append(", [").Append(c).Append("]=@").Append(c);
            sb.Append(", LastModified=GETDATE() WHERE ItemKey=@ik");
            using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sb.ToString(), conn, tx))
            {
                cmd.Parameters.AddWithValue("@ItemCode", d.ItemCode ?? "");
                cmd.Parameters.AddWithValue("@GradeCode", d.GradeCode ?? "");
                cmd.Parameters.AddWithValue("@PurchaseDate", (object)d.PurchaseDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ServiceTypeCode", d.ServiceTypeCode ?? "");
                cmd.Parameters.AddWithValue("@Note", (object)(d.Note ?? ""));
                cmd.Parameters.AddWithValue("@Remark1", d.Remark1 ?? "");
                cmd.Parameters.AddWithValue("@Remark2", d.Remark2 ?? "");
                cmd.Parameters.AddWithValue("@ReferenceNo", d.ReferenceNo ?? "");
                cmd.Parameters.AddWithValue("@ContractTypeCode", CtxStore(d.ContractTypeCode, LCtx(d, "ContractTypeCode"), d.LoadedCtxValid, DGet(rawCtx, "ContractTypeCode"), DGet(curCtx, "ContractTypeCode"), hasC));
                cmd.Parameters.AddWithValue("@StaffCode", CtxStore(d.StaffCode, LCtx(d, "StaffCode"), d.LoadedCtxValid, DGet(rawCtx, "StaffCode"), DGet(curCtx, "StaffCode"), hasC));
                cmd.Parameters.AddWithValue("@ServiceStartDate", CtxStoreDate(d.ServiceStartDate, d.LoadedStart, d.LoadedCtxValid, rawStart, curStart, hasC));
                cmd.Parameters.AddWithValue("@ServiceExpiryDate", CtxStoreDate(d.ServiceExpiryDate, d.LoadedExpiry, d.LoadedCtxValid, rawExpiry, curExpiry, hasC));
                cmd.Parameters.AddWithValue("@Address1", CtxStore(d.Address1, LCtx(d, "Address1"), d.LoadedCtxValid, DGet(rawCtx, "Address1"), DGet(curCtx, "Address1"), hasC));
                cmd.Parameters.AddWithValue("@Attention", CtxStore(d.Attention, LCtx(d, "Attention"), d.LoadedCtxValid, DGet(rawCtx, "Attention"), DGet(curCtx, "Attention"), hasC));
                cmd.Parameters.AddWithValue("@Phone", CtxStore(d.Phone, LCtx(d, "Phone"), d.LoadedCtxValid, DGet(rawCtx, "Phone"), DGet(curCtx, "Phone"), hasC));
                cmd.Parameters.AddWithValue("@TermCode", CtxStore(d.TermCode, LCtx(d, "TermCode"), d.LoadedCtxValid, DGet(rawCtx, "TermCode"), DGet(curCtx, "TermCode"), hasC));
                cmd.Parameters.AddWithValue("@AreaCode", CtxStore(d.AreaCode, LCtx(d, "AreaCode"), d.LoadedCtxValid, DGet(rawCtx, "AreaCode"), DGet(curCtx, "AreaCode"), hasC));
                cmd.Parameters.AddWithValue("@PMActive", d.PMActive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@PMIntervalType", d.PMIntervalType ?? "NONE");
                cmd.Parameters.AddWithValue("@PMIntervalValue", d.PMIntervalValue);
                cmd.Parameters.AddWithValue("@PMStartDate", (object)d.PMStartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMLastServiceDate", (object)d.PMLastServiceDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMNextServiceDate", (object)d.PMNextServiceDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMDept", d.PMDept ?? "");
                cmd.Parameters.AddWithValue("@PMJob", d.PMJob ?? "");
                cmd.Parameters.AddWithValue("@PMLocation", d.PMLocation ?? "");
                cmd.Parameters.AddWithValue("@MultiMachine", d.MultiMachine ? "Y" : "N");
                foreach (string c in _imhCols)
                {
                    string nv = (d.MoreHeader != null && d.MoreHeader.ContainsKey(c)) ? (d.MoreHeader[c] ?? "") : "";
                    cmd.Parameters.AddWithValue("@" + c, CtxStore(nv, LCtx(d, c), d.LoadedCtxValid, DGet(rawCtx, c), DGet(curCtx, c), hasC));
                }
                cmd.Parameters.AddWithValue("@ik", itemKey);
                cmd.ExecuteNonQuery();
            }

            RecordDebtorHistory(conn, tx, d, itemKey);
        }

        // Save an item's meter configuration WITHOUT wiping zSCP2_ItemMeter: that table is the FK parent
        // of zSCP_MeterTrans and zSCP2_MeterEntry with ON DELETE CASCADE, so deleting a meter row
        // destroys that meter's entire reading history. Meters are matched on their natural key
        // (MeterTypeCode + normalized MeterRole) and updated in place; only meters the user actually
        // removed are deleted (and only those lose their readings). Shared by the contract editor's
        // save loop and the item list's update path.
        internal static void SaveMetersPreservingReadings(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, ItemEditData d, long itemKey)
        {
            DataTable existing = new DataTable();
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(
                "SELECT ItemMeterKey, MeterTypeCode, MeterRole, ISNULL(MachineSerialNo,'') AS MachineSerialNo " +
                "FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=@ik", conn, tx))
            {
                q.Parameters.AddWithValue("@ik", itemKey);
                using (System.Data.SqlClient.SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(q)) da.Fill(existing);
            }

            System.Collections.Generic.List<long> keep = new System.Collections.Generic.List<long>();
            // Unmatched candidates. Matched rows are REMOVED from this pool so two grid rows can never
            // silently merge onto one DB row.
            System.Collections.Generic.List<DataRow> pool = new System.Collections.Generic.List<DataRow>();
            foreach (DataRow e in existing.Rows) pool.Add(e);
            if (d.Meters != null)
            {
                foreach (DataRow r in d.Meters.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string type = r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString().Trim();
                    if (type.Length == 0) continue;
                    string role = NormalizeMeterRole(r["MeterRole"]);
                    RequireMeterRole(role, type, d.ServiceItemNo);   // empty role -> throw, tx rolls back, nothing saved
                    string mser = r.Table.Columns.Contains("MachineSerialNo")
                        ? ((r["MachineSerialNo"] as string) ?? "").Trim() : "";

                    // The DB identity is UNIQUE(ItemKey, MeterTypeCode, MachineSerialNo) — role is NOT
                    // part of the key. So match on type+machine and let the UPDATE rewrite the role:
                    // re-tagging BK<->CL is an in-place edit of the SAME physical counter and keeps its
                    // readings. (Matching type+role instead would route a re-tag to the INSERT branch
                    // and violate the unique key, because the old row is only deleted after the loop.)
                    long hit = 0; DataRow hitRow = null;
                    foreach (DataRow e in pool)
                    {
                        if (string.Equals(Convert.ToString(e["MeterTypeCode"]).Trim(), type, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(Convert.ToString(e["MachineSerialNo"]).Trim(), mser, StringComparison.OrdinalIgnoreCase))
                        { hit = Convert.ToInt64(e["ItemMeterKey"]); hitRow = e; break; }
                    }

                    string tiersCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                        ? Convert.ToString(r["CustomTiers"]) : "";
                    if (hit > 0)
                    {
                        pool.Remove(hitRow);
                        using (System.Data.SqlClient.SqlCommand up = new System.Data.SqlClient.SqlCommand(
                            "UPDATE [dbo].[zSCP2_ItemMeter] SET MeterRole=@role, [Description]=@desc, MinimumCharges=@min, ChargesRate=@rate, " +
                            "MeterMultiPriceCode=@mp, RebateQtyInPercent=@reb, FOCQty=@foc, InitialReading=@init, " +
                            "WaiveFirstNMonths=@wn, WaiveTargetAmount=@wt, WaivePartialThreshold=@wpt, WaivePartialAmount=@wpa, WaiveScope=@ws, " +
                            "LastModified=GETDATE() WHERE ItemMeterKey=@mk", conn, tx))
                        {
                            up.Parameters.AddWithValue("@role", role);
                            AddMeterRowParams(up, r);
                            up.Parameters.AddWithValue("@mk", hit);
                            up.ExecuteNonQuery();
                        }
                        SaveCustomTiers(conn, tx, hit, tiersCsv);
                        keep.Add(hit);
                    }
                    else
                    {
                        using (System.Data.SqlClient.SqlCommand ins = new System.Data.SqlClient.SqlCommand(
                            "INSERT INTO [dbo].[zSCP2_ItemMeter] (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, " +
                            "ChargesRate, MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, " +
                            "WaiveFirstNMonths, WaiveTargetAmount, WaivePartialThreshold, WaivePartialAmount, WaiveScope, LastModified) " +
                            "VALUES (@ik,@type,@desc,@role,@mser,@min,@rate,@mp,@reb,@foc,@init,@wn,@wt,@wpt,@wpa,@ws,GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@ik", itemKey);
                            ins.Parameters.AddWithValue("@type", type);
                            ins.Parameters.AddWithValue("@role", role);
                            ins.Parameters.AddWithValue("@mser", mser);
                            AddMeterRowParams(ins, r);
                            long newKey = Convert.ToInt64(ins.ExecuteScalar());
                            SaveCustomTiers(conn, tx, newKey, tiersCsv);
                            keep.Add(newKey);
                        }
                    }
                }
            }

            foreach (DataRow e in existing.Rows)
            {
                long k = Convert.ToInt64(e["ItemMeterKey"]);
                if (keep.Contains(k)) continue;
                using (System.Data.SqlClient.SqlCommand del = new System.Data.SqlClient.SqlCommand(
                    "DELETE FROM [dbo].[zSCP2_ItemMeter] WHERE ItemMeterKey=@mk", conn, tx))
                { del.Parameters.AddWithValue("@mk", k); del.ExecuteNonQuery(); }
            }
        }

        // BK / CL / NA pass through; empty or unknown returns "" so the save VALIDATION rejects it.
        // (The old version silently defaulted unknown to "NA" — a forgotten role then quietly broke
        // Black/Colour billing and the API fetch. The user must pick the role consciously.)
        private static string NormalizeMeterRole(object roleValue)
        {
            string role = (roleValue == null || roleValue == DBNull.Value) ? "" : roleValue.ToString().Trim().ToUpperInvariant();
            return (role == "BK" || role == "CL" || role == "NA"
                 || role == "RENTAL" || role == "WAIVE" || role == "COMMIT") ? role : "";
        }

        // ===== Per-meter multi-price tier override (zSCP2_ItemMeterPrice) =====
        // Serialized in the Meters DataTable's "CustomTiers" column as "boundary|price;boundary|price"
        // (invariant culture). "" = no override — the meter's MeterMultiPriceCode scheme (if any) applies.

        /// <summary>The meter's override tiers as CSV ("" when none).</summary>
        internal static string LoadCustomTiersCsv(AutoCount.Data.DBSetting db, long itemMeterKey)
        {
            if (itemMeterKey <= 0) return "";
            try
            {
                DataTable dt = db.GetDataTable(
                    "SELECT MeterReading, UnitPrice FROM [dbo].[zSCP2_ItemMeterPrice] " +
                    "WHERE ItemMeterKey=" + itemMeterKey + " ORDER BY MeterReading", false);
                System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
                foreach (DataRow r in dt.Rows)
                    parts.Add(Convert.ToDecimal(r["MeterReading"]).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        + "|" + Convert.ToDecimal(r["UnitPrice"]).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
                return string.Join(";", parts.ToArray());
            }
            catch { return ""; }
        }

        /// <summary>Replace the meter's override tiers from CSV (inside the caller's save transaction).</summary>
        internal static void SaveCustomTiers(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, long itemMeterKey, string csv)
        {
            using (System.Data.SqlClient.SqlCommand del = new System.Data.SqlClient.SqlCommand(
                "DELETE FROM [dbo].[zSCP2_ItemMeterPrice] WHERE ItemMeterKey=@mk", conn, tx))
            { del.Parameters.AddWithValue("@mk", itemMeterKey); del.ExecuteNonQuery(); }
            foreach (decimal[] t in ParseTiersCsv(csv))
            {
                using (System.Data.SqlClient.SqlCommand ins = new System.Data.SqlClient.SqlCommand(
                    "INSERT INTO [dbo].[zSCP2_ItemMeterPrice] (ItemMeterKey, MeterReading, UnitPrice) VALUES (@mk,@mr,@up)", conn, tx))
                {
                    ins.Parameters.AddWithValue("@mk", itemMeterKey);
                    ins.Parameters.AddWithValue("@mr", t[0]);
                    ins.Parameters.AddWithValue("@up", t[1]);
                    ins.ExecuteNonQuery();
                }
            }
        }

        /// <summary>"boundary|price;boundary|price" -&gt; [boundary, price] rows (bad parts skipped).</summary>
        internal static System.Collections.Generic.List<decimal[]> ParseTiersCsv(string csv)
        {
            System.Collections.Generic.List<decimal[]> rows = new System.Collections.Generic.List<decimal[]>();
            if (string.IsNullOrEmpty(csv)) return rows;
            foreach (string part in csv.Split(';'))
            {
                string[] ab = part.Split('|');
                if (ab.Length != 2) continue;
                decimal mr, up;
                if (decimal.TryParse(ab[0], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out mr) &&
                    decimal.TryParse(ab[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out up) &&
                    mr > 0m)
                    rows.Add(new decimal[] { mr, up });
            }
            return rows;
        }

        /// <summary>Throws when a meter row's role is not an explicit BK / CL / NA — called by every
        /// meter save path so an empty role can never reach the DB.</summary>
        internal static void RequireMeterRole(string role, string meterTypeCode)
        { RequireMeterRole(role, meterTypeCode, null); }

        internal static void RequireMeterRole(string role, string meterTypeCode, string serviceItemNo)
        {
            if (role == "BK" || role == "CL" || role == "NA"
                || role == "RENTAL" || role == "WAIVE" || role == "COMMIT") return;
            string who = string.IsNullOrEmpty(serviceItemNo) ? "" : "Service item '" + serviceItemNo + "': ";
            throw new ApplicationException(
                who + "Meter '" + meterTypeCode + "' has NO Role.\r\n\r\n" +
                "Pick BK (black), CL (colour), RENTAL, WAIVE, COMMIT or NA on the meter row — " +
                "the role decides Black/Colour billing, strategy scopes and the reading fetch. " +
                "Nothing was saved.");
        }

        private static void AddMeterRowParams(System.Data.SqlClient.SqlCommand cmd, DataRow r)
        {
            cmd.Parameters.AddWithValue("@desc", r.Table.Columns.Contains("Description") && r["Description"] != DBNull.Value
                ? (object)r["Description"].ToString() : "");
            cmd.Parameters.AddWithValue("@min", r["MinimumCharges"] == DBNull.Value ? (object)0m : r["MinimumCharges"]);
            cmd.Parameters.AddWithValue("@rate", r["ChargesRate"] == DBNull.Value ? (object)0m : r["ChargesRate"]);
            cmd.Parameters.AddWithValue("@mp", r["MeterMultiPriceCode"] == null || r["MeterMultiPriceCode"] == DBNull.Value ? "" : r["MeterMultiPriceCode"].ToString());
            cmd.Parameters.AddWithValue("@reb", r["RebateQtyInPercent"] == DBNull.Value ? (object)0m : r["RebateQtyInPercent"]);
            cmd.Parameters.AddWithValue("@foc", r["FOCQty"] == DBNull.Value ? (object)0m : r["FOCQty"]);
            cmd.Parameters.AddWithValue("@init", r["InitialReading"] == DBNull.Value ? (object)0m : r["InitialReading"]);
            cmd.Parameters.AddWithValue("@wn", r.Table.Columns.Contains("WaiveFirstNMonths") && r["WaiveFirstNMonths"] != DBNull.Value ? Convert.ToInt32(r["WaiveFirstNMonths"]) : 0);
            cmd.Parameters.AddWithValue("@wt", r.Table.Columns.Contains("WaiveTargetAmount") && r["WaiveTargetAmount"] != DBNull.Value ? Convert.ToDecimal(r["WaiveTargetAmount"]) : 0m);
            cmd.Parameters.AddWithValue("@wpt", r.Table.Columns.Contains("WaivePartialThreshold") && r["WaivePartialThreshold"] != DBNull.Value ? Convert.ToDecimal(r["WaivePartialThreshold"]) : 0m);
            cmd.Parameters.AddWithValue("@wpa", r.Table.Columns.Contains("WaivePartialAmount") && r["WaivePartialAmount"] != DBNull.Value ? Convert.ToDecimal(r["WaivePartialAmount"]) : 0m);
            string wsv = r.Table.Columns.Contains("WaiveScope") && r["WaiveScope"] != DBNull.Value ? Convert.ToString(r["WaiveScope"]).Trim() : "";
            cmd.Parameters.AddWithValue("@ws", wsv.Length > 0 ? (object)wsv : (object)"BKCL");
        }

        // Decide what a context column actually stores. Unbound item: the value as typed. Bound item:
        // untouched (== load snapshot) keeps whatever the row already stored (override or inherit-'');
        // touched stores '' when it now matches the contract (re-inherit) else the override itself.
        private static string CtxStore(string newVal, string loadedVal, bool loadedValid, string rawVal, string contractVal, bool hasContract)
        {
            string nv = (newVal ?? "").Trim();
            if (!hasContract) return nv;
            if (loadedValid && nv == (loadedVal ?? "").Trim()) return rawVal ?? "";
            return string.Equals(nv, (contractVal ?? "").Trim(), StringComparison.OrdinalIgnoreCase) ? "" : nv;
        }

        private static object CtxStoreDate(DateTime? newVal, DateTime? loadedVal, bool loadedValid, object rawVal, DateTime? contractVal, bool hasContract)
        {
            if (!hasContract) return newVal.HasValue ? (object)newVal.Value : DBNull.Value;
            if (loadedValid)
            {
                bool sameAsLoaded = (!newVal.HasValue && !loadedVal.HasValue)
                    || (newVal.HasValue && loadedVal.HasValue && newVal.Value.Date == loadedVal.Value.Date);
                if (sameAsLoaded) return rawVal == null ? DBNull.Value : rawVal;
            }
            if (newVal.HasValue && contractVal.HasValue && newVal.Value.Date == contractVal.Value.Date) return DBNull.Value;
            return newVal.HasValue ? (object)newVal.Value : DBNull.Value;
        }

        private static string LCtx(ItemEditData d, string key)
        {
            string v;
            return (d.LoadedCtx != null && d.LoadedCtx.TryGetValue(key, out v)) ? v : null;
        }

        private static string DGet(System.Collections.Generic.Dictionary<string, string> dict, string key)
        {
            string v;
            return dict.TryGetValue(key, out v) ? v : "";
        }

        // Auto-record a debtor-ownership-history row when the item's customer (its contract's debtor)
        // changes: close the previous open period and open a new one. No-op if unchanged.
        // Writes to the v2 table zSCP2_ItemDebtorHistory (keyed to zSCP2_Item.ItemKey) — NOT the legacy
        // zSCP_ServiceItemDebtorHistory, whose FK targets the v1 zSCP_ServiceItem.
        // Returns true when an ownership change was actually recorded (owner differed from the last
        // open period). The effective owner is the contract's debtor, or OwnerDebtorCode when the item
        // has no contract — so a re-attach to a contract of the SAME customer records nothing.
        private static bool RecordDebtorHistory(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, ItemEditData d, long itemKey)
        {
            string debtor = "", contractNo = "";
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(
                "SELECT ISNULL(COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')),''), ISNULL(c.ContractNo,'') FROM [dbo].[zSCP2_Item] i " +
                "LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey=i.ContractKey WHERE i.ItemKey=@ik", conn, tx))
            {
                q.Parameters.AddWithValue("@ik", itemKey);
                using (System.Data.SqlClient.SqlDataReader r = q.ExecuteReader()) { if (r.Read()) { debtor = r.GetString(0).Trim(); contractNo = r.GetString(1).Trim(); } }
            }
            if (debtor.Length == 0) return false;

            long openKey = 0; string openDebtor = "";
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(
                "SELECT TOP 1 HistoryKey, ISNULL(DebtorCode,'') FROM [dbo].[zSCP2_ItemDebtorHistory] " +
                "WHERE ItemKey=@ik AND EndDate IS NULL ORDER BY StartDate DESC, HistoryKey DESC", conn, tx))
            {
                q.Parameters.AddWithValue("@ik", itemKey);
                using (System.Data.SqlClient.SqlDataReader r = q.ExecuteReader()) { if (r.Read()) { openKey = r.GetInt64(0); openDebtor = r.GetString(1).Trim(); } }
            }
            if (openDebtor == debtor) return false;   // unchanged -> nothing to record

            if (openKey > 0)
                using (System.Data.SqlClient.SqlCommand u = new System.Data.SqlClient.SqlCommand(
                    "UPDATE [dbo].[zSCP2_ItemDebtorHistory] SET EndDate=CAST(GETDATE() AS DATE), LastModified=GETDATE() WHERE HistoryKey=@k", conn, tx))
                { u.Parameters.AddWithValue("@k", openKey); u.ExecuteNonQuery(); }

            using (System.Data.SqlClient.SqlCommand ins = new System.Data.SqlClient.SqlCommand(
                "INSERT INTO [dbo].[zSCP2_ItemDebtorHistory] (ItemKey, ServiceItemNo, DebtorCode, GradeCode, ContractNo, StartDate, LastModified) " +
                "VALUES (@ik, @code, @debtor, @grade, @cno, CAST(GETDATE() AS DATE), GETDATE())", conn, tx))
            {
                ins.Parameters.AddWithValue("@ik", itemKey);
                ins.Parameters.AddWithValue("@code", d.ServiceItemNo ?? "");
                ins.Parameters.AddWithValue("@debtor", debtor);
                ins.Parameters.AddWithValue("@grade", d.GradeCode ?? "");
                ins.Parameters.AddWithValue("@cno", contractNo);
                ins.ExecuteNonQuery();
            }
            return true;
        }

        // ===================== Change Ownership =====================
        // Ownership = the item's contract (owner is that contract's debtor), or a customer directly
        // when contract-less (OwnerDebtorCode). Applied immediately in its own transaction; a real
        // owner change closes the open history period and opens a new one on the history tab.

        private void BarChangeOwner_Click(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string cur = "(none)";
            try
            {
                DataTable dt = _db.GetDataTable(OwnershipQuery(_data.ItemKey), false);
                if (dt.Rows.Count > 0)
                {
                    string cno = dt.Rows[0]["ContractNo"] as string ?? "";
                    string owner = dt.Rows[0]["OwnerCode"] as string ?? "";
                    string name = dt.Rows[0]["CompanyName"] as string ?? "";
                    string who = name.Length > 0 ? owner + " — " + name : owner;
                    cur = cno.Length > 0 ? "Contract " + cno + "   (" + who + ")"
                        : (owner.Length > 0 ? who + "   (no contract)" : "(none)");
                }
            }
            catch { }
            using (zSCP2_ChangeOwnership_Form dlg = new zSCP2_ChangeOwnership_Form(_db, cur))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try { ApplyOwnershipChange(dlg.ToContract, dlg.SelectedContractKey, dlg.SelectedDebtorCode); }
                catch (Exception ex) { XtraMessageBox.Show("Ownership change failed:\r\n" + ex.Message, "Error"); }
            }
        }

        private static string OwnershipQuery(long itemKey)
        {
            return
                "SELECT TOP 1 ISNULL(c.ContractNo,'') AS ContractNo, " +
                "ISNULL(COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')),'') AS OwnerCode, " +
                "ISNULL(dd.CompanyName,'') AS CompanyName " +
                "FROM [dbo].[zSCP2_Item] i " +
                "LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                "LEFT JOIN [dbo].[Debtor] dd ON dd.AccNo = COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')) " +
                "WHERE i.ItemKey=" + itemKey;
        }

        private void ApplyOwnershipChange(bool toContract, long contractKey, string debtorCode)
        {
            bool changed;
            using (System.Data.SqlClient.SqlConnection cn = new System.Data.SqlClient.SqlConnection(_db.ConnectionString))
            {
                cn.Open();
                using (System.Data.SqlClient.SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(
                            toContract
                                ? "UPDATE [dbo].[zSCP2_Item] SET ContractKey=@ck, OwnerDebtorCode='', LastModified=GETDATE() WHERE ItemKey=@ik"
                                : "UPDATE [dbo].[zSCP2_Item] SET ContractKey=NULL, OwnerDebtorCode=@d, LastModified=GETDATE() WHERE ItemKey=@ik", cn, tx))
                        {
                            if (toContract) cmd.Parameters.AddWithValue("@ck", contractKey);
                            else cmd.Parameters.AddWithValue("@d", debtorCode ?? "");
                            cmd.Parameters.AddWithValue("@ik", _data.ItemKey);
                            cmd.ExecuteNonQuery();
                        }
                        // Item-bound provided lines follow the machine: re-keyed to the new contract, or
                        // removed when the machine goes contract-less (ContractKey is NOT NULL on that
                        // table) — otherwise the OLD contract keeps showing lines of a machine it lost.
                        using (System.Data.SqlClient.SqlCommand sp = new System.Data.SqlClient.SqlCommand(
                            toContract
                                ? "UPDATE [dbo].[zSCP2_ContractSparePart] SET ContractKey=@ck, LastModified=GETDATE() WHERE ItemKey=@ik"
                                : "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=@ik", cn, tx))
                        {
                            if (toContract) sp.Parameters.AddWithValue("@ck", contractKey);
                            sp.Parameters.AddWithValue("@ik", _data.ItemKey);
                            sp.ExecuteNonQuery();
                        }
                        changed = RecordDebtorHistory(cn, tx, _data, _data.ItemKey);
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
            RefreshOwnershipHeader();
            RefreshDebtorHistoryTab();
            XtraMessageBox.Show(changed
                ? "Ownership transferred — recorded in the Debtors Ownership History tab."
                : "Binding updated — same customer, so no ownership change was recorded.",
                "Change Ownership", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Repaint the read-only Contract No + Customer boxes from the item's CURRENT ownership.
        private void RefreshOwnershipHeader()
        {
            if (_txtParentContractRO == null) return;
            string cno = _parentContractNo ?? "", owner = "", name = "";
            try
            {
                DataTable dt = _data.ItemKey > 0
                    ? _db.GetDataTable(OwnershipQuery(_data.ItemKey), false)
                    : _db.GetDataTable(
                        "SELECT TOP 1 ISNULL(c.ContractNo,'') AS ContractNo, ISNULL(c.DebtorCode,'') AS OwnerCode, " +
                        "ISNULL(dd.CompanyName,'') AS CompanyName " +
                        "FROM [dbo].[zSCP2_Contract] c LEFT JOIN [dbo].[Debtor] dd ON dd.AccNo = c.DebtorCode " +
                        "WHERE c.ContractNo='" + cno.Replace("'", "''") + "'", false);
                if (dt.Rows.Count > 0)
                {
                    cno = dt.Rows[0]["ContractNo"] as string ?? "";
                    owner = dt.Rows[0]["OwnerCode"] as string ?? "";
                    name = dt.Rows[0]["CompanyName"] as string ?? "";
                }
            }
            catch { }
            _txtParentContractRO.Text = cno.Length > 0 ? cno : "(no contract — owned by customer)";
            _txtCustomerRO.Text = owner.Length > 0 ? (name.Length > 0 ? owner + " — " + name : owner) : "";
        }

        private void RefreshDebtorHistoryTab()
        {
            if (_pgDebtorHist == null) return;
            _pgDebtorHist.Controls.Clear();
            BuildDebtorHistoryTab(_pgDebtorHist);
        }

        private void ItemCodeHeader_Changed(object sender, EventArgs e)
        {
            _dirty = true;
            PopulateSerialCombo();
            if (_sluItemCode.EditValue == null || _itemHdrLookup == null) return;
            string code = _sluItemCode.EditValue.ToString().Trim();
            DataRow[] f = _itemHdrLookup.Select("ItemCode='" + code.Replace("'", "''") + "'");
            if (f.Length > 0 && string.IsNullOrWhiteSpace(TxtDescription.Text)) TxtDescription.Text = f[0]["Description"].ToString();
        }

        // Machine Serial No is a TYPEABLE combo: its dropdown lists the serial numbers registered for
        // the chosen Item Code (dbo.ItemSerialNo), but any free-text serial is still accepted.
        private void PopulateSerialCombo()
        {
            string keep = TxtSerial.Text;
            TxtSerial.Properties.Items.Clear();
            string ic = _sluItemCode == null || _sluItemCode.EditValue == null ? "" : _sluItemCode.EditValue.ToString().Trim();
            if (ic.Length > 0)
            {
                try
                {
                    DataTable dt = _db.GetDataTable(
                        "SELECT SerialNumber FROM [dbo].[ItemSerialNo] WHERE ItemCode='" + ic.Replace("'", "''") +
                        "' ORDER BY SerialNumber", false);
                    foreach (DataRow r in dt.Rows)
                        TxtSerial.Properties.Items.Add(r["SerialNumber"].ToString());
                }
                catch { }
            }
            TxtSerial.Text = keep;
        }

        private void LoadGradeLookup()
        {
            try
            {
                _sluGrade.Properties.DataSource = _db.GetDataTable(
                    "SELECT ServiceItemGradeCode, Description FROM [dbo].[zSCP_LK_ServiceItemGrade] WHERE Inactive='N' ORDER BY ServiceItemGradeCode", false);
            }
            catch { }
        }

        // "+" on Grade: open the plugin's own Service Item Grade editor, then reselect (mirror SluContractType_ButtonClick).
        private void SluGrade_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceItemGrade_Form f =
                    new ServiceContractPhotocopier.GeneralSetup.MasterForms.ServiceItemGrade_Form(_db, null))
                {
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        LoadGradeLookup();
                        if (!string.IsNullOrEmpty(f.SavedCode)) _sluGrade.EditValue = f.SavedCode;
                    }
                }
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Create Grade failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // More Header tab for the item — mirrors the contract's _mh pattern, keyed by zSCP2_Item column names.
        private void BuildItemMoreHeaderTab(System.Windows.Forms.Control page)
        {
            ImhField(page, "City", "City", 12, 14, 200);
            ImhField(page, "PostalCode", "Postal Code", 430, 14, 200);
            ImhField(page, "State", "State", 12, 40, 200);
            ImhField(page, "Country", "Country", 430, 40, 200);
            ImhField(page, "Fax", "Fax", 12, 66, 200);
            ImhField(page, "Ref1", "Ref 1", 430, 66, 200);
            ImhField(page, "Ref2", "Ref 2", 12, 92, 200);
            ImhField(page, "Ref3", "Ref 3", 430, 92, 200);
            ImhField(page, "Ref4", "Ref 4", 12, 118, 200);

            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            grp.Text = "Delivery Address";
            grp.Location = new System.Drawing.Point(12, 150);
            grp.Size = new System.Drawing.Size(820, 210);
            page.Controls.Add(grp);

            ImhField(grp, "DelBranchCode", "Branch Code", 10, 28, 180);
            ImhField(grp, "DelState", "State", 430, 28, 180);
            ImhField(grp, "DelBranchName", "Branch Name", 10, 54, 180);
            ImhField(grp, "DelCountry", "Country", 430, 54, 180);
            DevExpress.XtraEditors.LabelControl lblAddr = new DevExpress.XtraEditors.LabelControl();
            lblAddr.Text = "Address"; lblAddr.Location = new System.Drawing.Point(10, 83); grp.Controls.Add(lblAddr);
            _imhDelAddress = new DevExpress.XtraEditors.MemoEdit();
            _imhDelAddress.Location = new System.Drawing.Point(110, 80); _imhDelAddress.Size = new System.Drawing.Size(200, 60);
            _imhDelAddress.EditValueChanged += delegate { _dirty = true; };
            grp.Controls.Add(_imhDelAddress);
            ImhField(grp, "DelPhone", "Phone", 430, 83, 180);
            ImhField(grp, "DelFax", "Fax", 430, 109, 180);
            ImhField(grp, "DelEmail", "Email", 430, 135, 180);
            ImhField(grp, "DelContactPerson", "Contact Person", 430, 161, 180);
            ImhField(grp, "DelCity", "City", 10, 150, 180);
            ImhField(grp, "DelPostalCode", "Postal Code", 10, 176, 180);

            // Demo 28/07 #11+14: the machine's branch/location comes from the customer's REGISTERED
            // branches (AutoCount A/R > Debtor > Branch) — the search lists only THIS debtor's
            // branches, and picking one fills the whole block ("一选了它就把全部东西填进去").
            // Everything stays editable afterwards. "From Contract" re-copies the contract default.
            _btnDelBranchSearch = new DevExpress.XtraEditors.SimpleButton();
            _btnDelBranchSearch.Text = "Search Branch";
            _btnDelBranchSearch.Location = new System.Drawing.Point(620, 24);
            _btnDelBranchSearch.Size = new System.Drawing.Size(94, 24);
            _btnDelBranchSearch.Click += new EventHandler(BtnDelBranchSearch_Click);
            grp.Controls.Add(_btnDelBranchSearch);
            _btnDelCopyContract = new DevExpress.XtraEditors.SimpleButton();
            _btnDelCopyContract.Text = "From Contract";
            _btnDelCopyContract.Location = new System.Drawing.Point(718, 24);
            _btnDelCopyContract.Size = new System.Drawing.Size(94, 24);
            _btnDelCopyContract.Click += new EventHandler(BtnDelCopyContract_Click);
            grp.Controls.Add(_btnDelCopyContract);
        }

        /// <summary>Effective debtor for the branch search: the standalone picker's customer, else
        /// the saved item's contract debtor (or direct owner), else the embedded parent contract's.</summary>
        private string ResolveDebtorForBranch()
        {
            string d = SelectedDebtorCode;
            try
            {
                if (d.Length == 0 && _data != null && _data.ItemKey > 0)
                {
                    DataTable t = _db.GetDataTable(
                        "SELECT ISNULL(COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')),'') AS D " +
                        "FROM [dbo].[zSCP2_Item] i LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                        "WHERE i.ItemKey=" + _data.ItemKey, false);
                    if (t.Rows.Count > 0) d = Convert.ToString(t.Rows[0]["D"]).Trim();
                }
                if (d.Length == 0 && !string.IsNullOrEmpty(_parentContractNo))
                {
                    DataTable t2 = _db.GetDataTable(
                        "SELECT ISNULL(DebtorCode,'') AS D FROM [dbo].[zSCP2_Contract] WHERE ContractNo=N'" +
                        _parentContractNo.Replace("'", "''") + "'", false);
                    if (t2.Rows.Count > 0) d = Convert.ToString(t2.Rows[0]["D"]).Trim();
                }
            }
            catch { }
            return d;
        }

        private void BtnDelBranchSearch_Click(object sender, EventArgs e)
        {
            string debtor = ResolveDebtorForBranch();
            if (debtor.Length == 0)
            {
                XtraMessageBox.Show("No customer resolved yet — the branch list is per customer.\r\n" +
                    "Pick / attach the customer first.", "Search Branch");
                return;
            }
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
            if (br.Rows.Count == 0)
            {
                XtraMessageBox.Show("Customer '" + debtor + "' has no registered branches.\r\n\r\n" +
                    "Register them in AutoCount: A/R > Debtor > (edit the debtor) > Branch tab.", "Search Branch");
                return;
            }
            object sel = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Select Branch — " + debtor, br, "BranchCode",
                new string[] { "BranchCode", "BranchName", "Address1", "PostCode", "Phone1" },
                new string[] { "Branch Code", "Branch Name", "Address", "Post Code", "Phone" },
                new int[] { 90, 200, 200, 80, 100 });
            if (sel == null || sel == DBNull.Value) return;
            DataRow[] f = br.Select("BranchCode='" + sel.ToString().Replace("'", "''") + "'");
            if (f.Length == 0) return;
            DataRow b = f[0];
            ImhSet("DelBranchCode", AsS(b["BranchCode"]));
            ImhSet("DelBranchName", AsS(b["BranchName"]));
            if (_imhDelAddress != null)
                _imhDelAddress.Text = string.Join("\r\n", new string[] { AsS(b["Address1"]), AsS(b["Address2"]), AsS(b["Address3"]), AsS(b["Address4"]) }).Trim('\r', '\n');
            ImhSet("DelPostalCode", AsS(b["PostCode"]));
            ImhSet("DelPhone", AsS(b["Phone1"]));
            ImhSet("DelFax", AsS(b["Fax1"]));
            ImhSet("DelEmail", AsS(b["EmailAddress"]));
            ImhSet("DelContactPerson", AsS(b["Contact"]));
            _dirty = true;
        }

        // "By default 跟 contract": copy the CONTRACT's delivery block onto the machine (falls back
        // to the contract's main address when the contract has no delivery block of its own).
        private void BtnDelCopyContract_Click(object sender, EventArgs e)
        {
            string where = "";
            if (_data != null && _data.ItemKey > 0)
                where = "ContractKey = (SELECT ContractKey FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + _data.ItemKey + ")";
            else if (SelectedContractKey > 0)
                where = "ContractKey=" + SelectedContractKey;
            else if (!string.IsNullOrEmpty(_parentContractNo))
                where = "ContractNo=N'" + _parentContractNo.Replace("'", "''") + "'";
            if (where.Length == 0) { XtraMessageBox.Show("No contract to copy from yet.", "From Contract"); return; }
            DataTable t;
            try
            {
                t = _db.GetDataTable(
                    "SELECT ISNULL(DelBranchCode,'') AS DelBranchCode, ISNULL(DelBranchName,'') AS DelBranchName, " +
                    "ISNULL(DelAddress,'') AS DelAddress, ISNULL(DelCity,'') AS DelCity, ISNULL(DelPostalCode,'') AS DelPostalCode, " +
                    "ISNULL(DelState,'') AS DelState, ISNULL(DelCountry,'') AS DelCountry, ISNULL(DelPhone,'') AS DelPhone, " +
                    "ISNULL(DelFax,'') AS DelFax, ISNULL(DelEmail,'') AS DelEmail, ISNULL(DelContactPerson,'') AS DelContactPerson, " +
                    "ISNULL(Address1,'') AS Address1, ISNULL(City,'') AS City, ISNULL(PostalCode,'') AS PostalCode, " +
                    "ISNULL(State,'') AS State, ISNULL(Country,'') AS Country, ISNULL(Phone,'') AS Phone, " +
                    "ISNULL(Fax,'') AS Fax, ISNULL(Attention,'') AS Attention " +
                    "FROM [dbo].[zSCP2_Contract] WHERE " + where, false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); return; }
            if (t.Rows.Count == 0) { XtraMessageBox.Show("Contract not found.", "From Contract"); return; }
            DataRow c = t.Rows[0];
            bool hasDel = AsS(c["DelBranchCode"]).Trim().Length > 0 || AsS(c["DelAddress"]).Trim().Length > 0
                || AsS(c["DelCity"]).Trim().Length > 0 || AsS(c["DelPostalCode"]).Trim().Length > 0;
            if (hasDel)
            {
                ImhSet("DelBranchCode", AsS(c["DelBranchCode"]));
                ImhSet("DelBranchName", AsS(c["DelBranchName"]));
                if (_imhDelAddress != null) _imhDelAddress.Text = AsS(c["DelAddress"]);
                ImhSet("DelCity", AsS(c["DelCity"]));
                ImhSet("DelPostalCode", AsS(c["DelPostalCode"]));
                ImhSet("DelState", AsS(c["DelState"]));
                ImhSet("DelCountry", AsS(c["DelCountry"]));
                ImhSet("DelPhone", AsS(c["DelPhone"]));
                ImhSet("DelFax", AsS(c["DelFax"]));
                ImhSet("DelEmail", AsS(c["DelEmail"]));
                ImhSet("DelContactPerson", AsS(c["DelContactPerson"]));
            }
            else
            {
                ImhSet("DelBranchCode", ""); ImhSet("DelBranchName", "");
                if (_imhDelAddress != null) _imhDelAddress.Text = AsS(c["Address1"]);
                ImhSet("DelCity", AsS(c["City"]));
                ImhSet("DelPostalCode", AsS(c["PostalCode"]));
                ImhSet("DelState", AsS(c["State"]));
                ImhSet("DelCountry", AsS(c["Country"]));
                ImhSet("DelPhone", AsS(c["Phone"]));
                ImhSet("DelFax", AsS(c["Fax"]));
                ImhSet("DelEmail", "");
                ImhSet("DelContactPerson", AsS(c["Attention"]));
            }
            _dirty = true;
        }

        private void ImhField(System.Windows.Forms.Control parent, string col, string caption, int x, int y, int width)
        {
            DevExpress.XtraEditors.LabelControl lbl = new DevExpress.XtraEditors.LabelControl();
            lbl.Text = caption; lbl.Location = new System.Drawing.Point(x, y + 3);
            parent.Controls.Add(lbl);
            DevExpress.XtraEditors.TextEdit ed = new DevExpress.XtraEditors.TextEdit();
            ed.Location = new System.Drawing.Point(x + 98, y);
            ed.Size = new System.Drawing.Size(width, 20);
            ed.EditValueChanged += delegate { _dirty = true; };
            parent.Controls.Add(ed);
            _imh[col] = ed;
        }

        private void BuildNoteRemarkTabs()
        {
            _txtNote = new DevExpress.XtraEditors.MemoEdit();
            _txtNote.Location = new System.Drawing.Point(12, 12);
            _txtNote.Size = new System.Drawing.Size(700, 300);
            _txtNote.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _txtNote.EditValueChanged += delegate { _dirty = true; };
            _pgNote.Controls.Add(_txtNote);

            DevExpress.XtraEditors.LabelControl l1 = new DevExpress.XtraEditors.LabelControl();
            l1.Text = "Remark 1"; l1.Location = new System.Drawing.Point(12, 16); _pgRemark.Controls.Add(l1);
            _txtRemark1 = new DevExpress.XtraEditors.TextEdit();
            _txtRemark1.Location = new System.Drawing.Point(90, 13); _txtRemark1.Size = new System.Drawing.Size(500, 20);
            _txtRemark1.EditValueChanged += delegate { _dirty = true; }; _pgRemark.Controls.Add(_txtRemark1);
            DevExpress.XtraEditors.LabelControl l2 = new DevExpress.XtraEditors.LabelControl();
            l2.Text = "Remark 2"; l2.Location = new System.Drawing.Point(12, 42); _pgRemark.Controls.Add(l2);
            _txtRemark2 = new DevExpress.XtraEditors.TextEdit();
            _txtRemark2.Location = new System.Drawing.Point(90, 39); _txtRemark2.Size = new System.Drawing.Size(500, 20);
            _txtRemark2.EditValueChanged += delegate { _dirty = true; }; _pgRemark.Controls.Add(_txtRemark2);
        }

        // Debtors Ownership History tab (Image #127): read-only grid of the item's customer-change history.
        private void BuildDebtorHistoryTab(System.Windows.Forms.Control page)
        {
            DevExpress.XtraGrid.GridControl grid = new DevExpress.XtraGrid.GridControl();
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            DevExpress.XtraGrid.Views.Grid.GridView view = new DevExpress.XtraGrid.Views.Grid.GridView(grid);
            grid.MainView = view; grid.ViewCollection.Add(view);
            view.OptionsBehavior.Editable = false;
            view.OptionsView.ShowGroupPanel = false;
            page.Controls.Add(grid);

            // Always bind SOMETHING so the column headers render. A brand-new item has no history yet,
            // but an empty grid with no columns at all just looks broken.
            DataTable dt = null;
            if (_data.ItemKey > 0)
            {
                try
                {
                    dt = _db.GetDataTable(
                        "SELECT h.DebtorCode AS [Debtor Code], ISNULL(d.CompanyName,'') AS [Debtor Name], " +
                        "h.GradeCode AS [Service Item Grade], h.ContractNo AS [Contract No], " +
                        "h.StartDate AS [Service Start Date], h.EndDate AS [End Date], h.Remark AS [Remark] " +
                        "FROM [dbo].[zSCP2_ItemDebtorHistory] h LEFT JOIN [dbo].[Debtor] d ON d.AccNo=h.DebtorCode " +
                        "WHERE h.ItemKey=" + _data.ItemKey + " ORDER BY h.StartDate", false);
                }
                catch { dt = null; }
            }
            if (dt == null) dt = EmptyDebtorHistoryTable();
            grid.DataSource = dt;
            view.PopulateColumns();
            view.BestFitColumns();
        }

        // Same shape as the history SELECT, so the grid shows its headers even with no rows.
        private static DataTable EmptyDebtorHistoryTable()
        {
            DataTable t = new DataTable();
            t.Columns.Add("Debtor Code", typeof(string));
            t.Columns.Add("Debtor Name", typeof(string));
            t.Columns.Add("Service Item Grade", typeof(string));
            t.Columns.Add("Contract No", typeof(string));
            t.Columns.Add("Service Start Date", typeof(DateTime));
            t.Columns.Add("End Date", typeof(DateTime));
            t.Columns.Add("Remark", typeof(string));
            return t;
        }

        // Preventive Maintenance tab (Image #126): Active gate + interval + service dates + Dept/Job/Location.
        /// <summary>Compact "Preventive Maintenance only" mode (used by the contract grid's Preventive
        /// button column): every other tab is stripped, the header area is covered by the tab control,
        /// and the form shrinks to just the PM page + the ribbon's Save/Close. Safe to call before
        /// ShowDialog — applied once the form shows.</summary>
        internal void ShowPreventiveTab()
        {
            this.Shown += delegate
            {
                if (_tabMain == null || _pgPreventive == null) return;
                this.Text = "Preventive Maintenance" +
                    (string.IsNullOrEmpty(_data.ServiceItemNo) ? "" : " — " + _data.ServiceItemNo);
                for (int i = _tabMain.TabPages.Count - 1; i >= 0; i--)
                    if (!object.ReferenceEquals(_tabMain.TabPages[i], _pgPreventive))
                        _tabMain.TabPages.RemoveAt(i);
                _tabMain.SelectedTabPage = _pgPreventive;
                int topY = 145;
                if (RibbonCtl != null) topY = RibbonCtl.Height + 4;
                this.ClientSize = new System.Drawing.Size(850, topY + 300);
                _tabMain.Location = new System.Drawing.Point(5, topY);
                _tabMain.Size = new System.Drawing.Size(this.ClientSize.Width - 10, this.ClientSize.Height - topY - 5);
                _tabMain.BringToFront();
            };
        }

        private void BuildPreventiveTab(System.Windows.Forms.Control page)
        {
            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            grp.Text = "Preventive Maintenance";
            grp.Location = new System.Drawing.Point(12, 12); grp.Size = new System.Drawing.Size(400, 210);
            page.Controls.Add(grp);

            _pmActive = new DevExpress.XtraEditors.CheckEdit();
            _pmActive.Text = "Active Preventive Maintenance";
            _pmActive.Location = new System.Drawing.Point(12, 26); _pmActive.Size = new System.Drawing.Size(240, 20);
            _pmActive.CheckedChanged += new EventHandler(PMActive_Changed);
            grp.Controls.Add(_pmActive);

            PmLabel(grp, "Interval Type", 12, 56);
            _pmIntervalType = new DevExpress.XtraEditors.ComboBoxEdit();
            _pmIntervalType.Location = new System.Drawing.Point(120, 53); _pmIntervalType.Size = new System.Drawing.Size(120, 20);
            _pmIntervalType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            _pmIntervalType.Properties.Items.AddRange(new object[] { "NONE", "DAY", "WEEK", "MONTH", "YEAR" });
            _pmIntervalType.EditValueChanged += delegate { _dirty = true; };
            grp.Controls.Add(_pmIntervalType);

            PmLabel(grp, "Interval Value", 12, 82);
            _pmIntervalValue = new DevExpress.XtraEditors.SpinEdit();
            _pmIntervalValue.Location = new System.Drawing.Point(120, 79); _pmIntervalValue.Size = new System.Drawing.Size(80, 20);
            _pmIntervalValue.Properties.IsFloatValue = false; _pmIntervalValue.Properties.MinValue = 0; _pmIntervalValue.Properties.MaxValue = 999;
            _pmIntervalValue.EditValueChanged += delegate { _dirty = true; };
            grp.Controls.Add(_pmIntervalValue);

            _pmStart = PmDate(grp, "Start Date", 12, 108);
            _pmLast = PmDate(grp, "Last Service Date", 12, 134);
            _pmNext = PmDate(grp, "Next Service Date", 12, 160);
            _pmNext.Properties.ReadOnly = true; _pmNext.Enabled = false;   // auto-computed on save

            // Right side: Department / Job / Location.
            PmLabel(page, "Department", 430, 15);
            _pmDept = PmLookup(page, 520, 12, "SELECT DeptNo, Description FROM [dbo].[Dept] ORDER BY DeptNo", "DeptNo");
            PmLabel(page, "Job", 430, 41);
            _pmJob = PmLookup(page, 520, 38, "SELECT ProjNo, Description FROM [dbo].[Project] ORDER BY ProjNo", "ProjNo");
            PmLabel(page, "Location", 430, 67);
            _pmLocation = new DevExpress.XtraEditors.TextEdit();
            _pmLocation.Location = new System.Drawing.Point(520, 64); _pmLocation.Size = new System.Drawing.Size(180, 20);
            _pmLocation.EditValueChanged += delegate { _dirty = true; };
            page.Controls.Add(_pmLocation);

            // populate from _data (loaded by LoadExtrasFromDb for existing items).
            _pmActive.Checked = _data.PMActive;
            _pmIntervalType.EditValue = string.IsNullOrEmpty(_data.PMIntervalType) ? "NONE" : _data.PMIntervalType;
            _pmIntervalValue.Value = _data.PMIntervalValue;
            _pmStart.EditValue = (object)_data.PMStartDate;
            _pmLast.EditValue = (object)_data.PMLastServiceDate;
            _pmNext.EditValue = (object)_data.PMNextServiceDate;
            _pmDept.EditValue = string.IsNullOrEmpty(_data.PMDept) ? null : (object)_data.PMDept;
            _pmJob.EditValue = string.IsNullOrEmpty(_data.PMJob) ? null : (object)_data.PMJob;
            _pmLocation.Text = _data.PMLocation;
            PMActive_Changed(null, null);
        }

        private void PmLabel(System.Windows.Forms.Control parent, string text, int x, int y)
        {
            DevExpress.XtraEditors.LabelControl l = new DevExpress.XtraEditors.LabelControl();
            l.Text = text; l.Location = new System.Drawing.Point(x, y + 3); parent.Controls.Add(l);
        }

        private DevExpress.XtraEditors.DateEdit PmDate(System.Windows.Forms.Control parent, string caption, int x, int y)
        {
            PmLabel(parent, caption, x, y);
            DevExpress.XtraEditors.DateEdit d = new DevExpress.XtraEditors.DateEdit();
            d.Location = new System.Drawing.Point(120, y - 3); d.Size = new System.Drawing.Size(120, 20);
            d.EditValue = null;
            d.EditValueChanged += delegate { _dirty = true; };
            parent.Controls.Add(d);
            return d;
        }

        private DevExpress.XtraEditors.SearchLookUpEdit PmLookup(System.Windows.Forms.Control parent, int x, int y, string sql, string member)
        {
            DevExpress.XtraEditors.SearchLookUpEdit slu = new DevExpress.XtraEditors.SearchLookUpEdit();
            slu.Location = new System.Drawing.Point(x, y); slu.Size = new System.Drawing.Size(180, 20);
            slu.Properties.NullText = "";
            slu.Properties.ValueMember = member; slu.Properties.DisplayMember = member;
            try { slu.Properties.DataSource = _db.GetDataTable(sql, false); } catch { }
            slu.EditValueChanged += delegate { _dirty = true; };
            parent.Controls.Add(slu);
            return slu;
        }

        private void PMActive_Changed(object sender, EventArgs e)
        {
            bool on = _pmActive.Checked;
            _pmIntervalType.Enabled = on; _pmIntervalValue.Enabled = on;
            _pmStart.Enabled = on; _pmLast.Enabled = on;
            _pmDept.Enabled = on; _pmJob.Enabled = on; _pmLocation.Enabled = on;
            if (sender != null) _dirty = true;
        }

        // Next Service Date = (Last Service Date, else Start Date) + IntervalValue * IntervalType.
        private static DateTime? ComputeNextService(ItemEditData d)
        {
            if (!d.PMActive || d.PMIntervalValue <= 0 || string.IsNullOrEmpty(d.PMIntervalType) || d.PMIntervalType == "NONE") return null;
            DateTime? baseDate = d.PMLastServiceDate ?? d.PMStartDate;
            if (!baseDate.HasValue) return null;
            DateTime b = baseDate.Value;
            switch (d.PMIntervalType)
            {
                case "DAY": return b.AddDays(d.PMIntervalValue);
                case "WEEK": return b.AddDays(7 * d.PMIntervalValue);
                case "MONTH": return b.AddMonths(d.PMIntervalValue);
                case "YEAR": return b.AddYears(d.PMIntervalValue);
                default: return null;
            }
        }

        // Builds the "Item Provided" group into the given container (the Item & Meter tab), docked to
        // fill under the Meter grid. Toolbar (insert/remove/up/down icons) on a top bar, grid fills.
        private void BuildItemSparePartsGrid(System.Windows.Forms.Control parent)
        {
            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            _grpItemSp = grp;
            grp.Text = "Item Provided";
            grp.Dock = System.Windows.Forms.DockStyle.Fill;

            _gridItemSp = new DevExpress.XtraGrid.GridControl();
            _gridItemSp.Dock = System.Windows.Forms.DockStyle.Fill;
            _viewItemSp = new DevExpress.XtraGrid.Views.Grid.GridView(_gridItemSp);
            _gridItemSp.MainView = _viewItemSp;
            _gridItemSp.ViewCollection.Add(_viewItemSp);
            grp.Controls.Add(_gridItemSp);   // Fill added first

            DevExpress.XtraEditors.PanelControl bar = new DevExpress.XtraEditors.PanelControl();
            bar.Dock = System.Windows.Forms.DockStyle.Top;
            bar.Height = 32;
            grp.Controls.Add(bar);           // Top added after Fill

            AutoCount.Images.IAutoCountImage tbimg = null;
            try
            {
                float dpi = 96f; try { dpi = this.DeviceDpi; } catch { }
                tbimg = AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
            }
            catch { }

            DevExpress.XtraEditors.SimpleButton bIns = new DevExpress.XtraEditors.SimpleButton();
            bIns.ToolTip = "Insert Row";
            bIns.Location = new System.Drawing.Point(6, 4); bIns.Size = new System.Drawing.Size(30, 24);
            bIns.Click += new EventHandler(ItemSpInsert_Click); bar.Controls.Add(bIns);
            DevExpress.XtraEditors.SimpleButton bRem = new DevExpress.XtraEditors.SimpleButton();
            bRem.ToolTip = "Remove Row";
            bRem.Location = new System.Drawing.Point(40, 4); bRem.Size = new System.Drawing.Size(30, 24);
            bRem.Click += new EventHandler(ItemSpRemove_Click); bar.Controls.Add(bRem);
            DevExpress.XtraEditors.SimpleButton bUp = new DevExpress.XtraEditors.SimpleButton();
            bUp.ToolTip = "Move Up";
            bUp.Location = new System.Drawing.Point(78, 4); bUp.Size = new System.Drawing.Size(30, 24);
            bUp.Click += delegate { ItemSpMove(-1); }; bar.Controls.Add(bUp);
            DevExpress.XtraEditors.SimpleButton bDn = new DevExpress.XtraEditors.SimpleButton();
            bDn.ToolTip = "Move Down";
            bDn.Location = new System.Drawing.Point(112, 4); bDn.Size = new System.Drawing.Size(30, 24);
            bDn.Click += delegate { ItemSpMove(1); }; bar.Controls.Add(bDn);
            if (tbimg != null)
            {
                SetIcon(bIns, tbimg.GetSmallImage_Insert());
                SetIcon(bRem, tbimg.GetSmallImage_Delete());
                SetIcon(bUp, tbimg.GetSmallImage_MoveUp());
                SetIcon(bDn, tbimg.GetSmallImage_MoveDown());
            }

            parent.Controls.Add(grp);

            _itemSpCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            _gridItemSp.RepositoryItems.Add(_itemSpCheck);
            _itemSpItemLookup = zSCP2_Contract_Form.LoadItemLookup(_db);
            _itemSpItemRepo = zSCP2_Contract_Form.MakeItemCodeRepo(_itemSpItemLookup);
            _gridItemSp.RepositoryItems.Add(_itemSpItemRepo);
            _itemSpSerialLookup = zSCP2_Contract_Form.LoadSerialLookup(_db);
            _itemSpSerialRepo = zSCP2_Contract_Form.MakeSerialRepo(_itemSpSerialLookup);
            _gridItemSp.RepositoryItems.Add(_itemSpSerialRepo);
            // Serial No: display stays raw text; the EDIT-time dropdown lists only the row's Item Code
            // serials (empty until one is picked). See ItemSp_SerialEditor.
            _viewItemSp.CustomRowCellEditForEditing += new DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventHandler(ItemSp_SerialEditor);

            _itemSpareParts = _data.SpareParts != null ? _data.SpareParts.Copy() : zSCP2_Contract_Form.CreateSparePartsTable();
            if (!_itemSpareParts.Columns.Contains("SerialNumber")) _itemSpareParts.Columns.Add("SerialNumber", typeof(string));
            _gridItemSp.DataSource = _itemSpareParts.DefaultView;
            _itemSpareParts.DefaultView.Sort = "Pos";
            zSCP2_Contract_Form.ConfigureSpareView(_viewItemSp, _itemSpCheck, _itemSpItemRepo, _itemSpSerialRepo);
            _viewItemSp.RowHeight = 26;   // ~20% taller rows for easier editing
            RenumberItemSp();

            _viewItemSp.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(ItemSp_CellValueChanged);
        }

        // Serial No edit-time picker: dropdown filtered to the row's Item Code (empty until one is
        // picked). No ColumnEdit is set for display, so other rows' serials always render as raw text.
        private void ItemSp_SerialEditor(object sender, DevExpress.XtraGrid.Views.Grid.CustomRowCellEditEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "SerialNumber") return;
            string code = Convert.ToString(_viewItemSp.GetRowCellValue(e.RowHandle, "ItemCode") ?? "").Trim();
            DataView dv = new DataView(_itemSpSerialLookup);
            dv.RowFilter = code.Length == 0 ? "1=0" : "ItemCode='" + code.Replace("'", "''") + "'";
            _itemSpSerialRepo.DataSource = dv;
            e.RepositoryItem = _itemSpSerialRepo;
        }

        private void ItemSp_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            // Keep the meter grid's derived Item Code cells in sync with provided-row edits.
            if (GridViewMeters != null) GridViewMeters.RefreshData();
            if (e.Column.FieldName == "Amount" || e.Column.FieldName == "TaxAmount" ||
                e.Column.FieldName == "AmountAfterTax" || e.Column.FieldName == "No") return;
            _viewItemSp.PostEditor();
            DataRowView drv = _viewItemSp.GetRow(e.RowHandle) as DataRowView;
            if (drv != null)
            {
                if (e.Column.FieldName == "ItemCode") zSCP2_Contract_Form.FillFromItem(drv.Row, _itemSpItemLookup);
                zSCP2_Contract_Form.ComputeSpareRow(drv.Row);
            }
            _viewItemSp.RefreshData();
        }

        private void RenumberItemSp()
        {
            System.Data.DataView v = _itemSpareParts.DefaultView;
            for (int i = 0; i < v.Count; i++) { v[i].Row["No"] = i + 1; v[i].Row["Pos"] = i; }
        }

        private void ItemSpInsert_Click(object sender, EventArgs e)
        {
            _viewItemSp.PostEditor();
            DataRow r = _itemSpareParts.NewRow();
            r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
            r["ItemCode"] = ""; r["Description"] = ""; r["Unlimited"] = false; r["UOM"] = "";
            r["Quantity"] = 0m; r["Discount"] = ""; r["UnitPrice"] = 0m; r["Amount"] = 0m;
            r["TaxType"] = ""; r["TaxInclusive"] = false; r["TaxRate"] = 0m; r["TaxAmount"] = 0m;
            r["AmountAfterTax"] = 0m; r["Pos"] = _itemSpareParts.Rows.Count;
            _itemSpareParts.Rows.Add(r);
            _dirty = true; RenumberItemSp(); _viewItemSp.RefreshData();
            _viewItemSp.FocusedRowHandle = _viewItemSp.RowCount - 1;
        }

        private void ItemSpRemove_Click(object sender, EventArgs e)
        {
            int rh = _viewItemSp.FocusedRowHandle;
            if (rh < 0) return;
            DataRowView drv = _viewItemSp.GetRow(rh) as DataRowView;
            if (drv == null) return;
            drv.Row.Delete();
            _dirty = true; RenumberItemSp(); _viewItemSp.RefreshData();
        }

        private void ItemSpMove(int dir)
        {
            int rh = _viewItemSp.FocusedRowHandle;
            if (rh < 0) return;
            int target = rh + dir;
            if (target < 0 || target >= _viewItemSp.RowCount) return;
            DataRowView a = _viewItemSp.GetRow(rh) as DataRowView;
            DataRowView b = _viewItemSp.GetRow(target) as DataRowView;
            if (a == null || b == null) return;
            int pa = Convert.ToInt32(a.Row["Pos"]), pb = Convert.ToInt32(b.Row["Pos"]);
            a.Row["Pos"] = pb; b.Row["Pos"] = pa;
            _dirty = true; RenumberItemSp(); _viewItemSp.RefreshData();
            _viewItemSp.FocusedRowHandle = target;
        }

        // Ribbon Save/Close (same toolbar style as the contract editor) — same logic as OK/Cancel.
        private void barSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BtnOK_Click(sender, EventArgs.Empty);
        }

        private void barClose_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BtnCancel_Click(sender, EventArgs.Empty);
        }
    }
}
