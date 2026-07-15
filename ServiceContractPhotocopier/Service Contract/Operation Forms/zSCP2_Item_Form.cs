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

        // --- overhaul: header + More Header + Note/Remarks (persisted by PersistItemExtras) ---
        public string ItemCode = "";
        public string GradeCode = "";
        public string Note = "";
        public string Remark1 = "";
        public string Remark2 = "";
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
            dt.Columns.Add("MeterRole", typeof(string));
            dt.Columns.Add("MinimumCharges", typeof(decimal));
            dt.Columns.Add("ChargesRate", typeof(decimal));
            dt.Columns.Add("MeterMultiPriceCode", typeof(string));
            dt.Columns.Add("RebateQtyInPercent", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("InitialReading", typeof(decimal));
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

            LblBillDayHint.Text = "0 = follow contract day (" + _contractBillingDay + ")";

            // Embedded add (from within a contract): show the parent Contract No READ-ONLY so the user
            // knows the item's home. The contract itself can't be changed here (that's the "read-only"
            // scenario) — only the item fields are editable.
            if (!_standalone && !string.IsNullOrEmpty(_parentContractNo))
            {
                DevExpress.XtraEditors.LabelControl lblC = new DevExpress.XtraEditors.LabelControl();
                lblC.Text = "Contract No";
                lblC.Location = new System.Drawing.Point(14, 290);
                this.Controls.Add(lblC); lblC.BringToFront();
                DevExpress.XtraEditors.TextEdit txtC = new DevExpress.XtraEditors.TextEdit();
                txtC.Location = new System.Drawing.Point(120, 287);
                txtC.Size = new System.Drawing.Size(214, 20);
                txtC.Text = _parentContractNo;
                txtC.Properties.ReadOnly = true;
                txtC.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                this.Controls.Add(txtC); txtC.BringToFront();
            }

            // Standalone-new (opened from "Maintain Service Item"): the item needs a home. A new row 5
            // (made by pushing the two groups down) lets the user attach it to an EXISTING contract, or
            // leave that empty and pick a Customer — the caller then auto-creates a contract for it.
            // Created in code — the strict designer file stays untouched.
            if (_standalone)
            {
                DevExpress.XtraEditors.LabelControl lblContract = new DevExpress.XtraEditors.LabelControl();
                lblContract.Text = "Contract No";
                lblContract.Location = new System.Drawing.Point(14, 290);
                this.Controls.Add(lblContract);
                lblContract.BringToFront();

                _lkContract = new DevExpress.XtraEditors.SearchLookUpEdit();
                _lkContract.Location = new System.Drawing.Point(120, 287);
                _lkContract.Size = new System.Drawing.Size(214, 20);
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
                    if (_lkCustomer != null)
                        _lkCustomer.Enabled = (_lkContract.EditValue == null || _lkContract.EditValue == DBNull.Value);
                };
                this.Controls.Add(_lkContract);
                _lkContract.BringToFront();

                _lblCustomer = new DevExpress.XtraEditors.LabelControl();
                _lblCustomer.Text = "Customer *";
                _lblCustomer.Location = new System.Drawing.Point(470, 290);
                this.Controls.Add(_lblCustomer);
                _lblCustomer.BringToFront();

                _lkCustomer = new DevExpress.XtraEditors.SearchLookUpEdit();
                _lkCustomer.Location = new System.Drawing.Point(600, 287);
                _lkCustomer.Size = new System.Drawing.Size(210, 20);
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
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnItemFormClosing);
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
                    "MeterMultiPriceCode, RebateQtyInPercent, FOCQty " +
                    "FROM [dbo].[zSCP_MeterType] WHERE Inactive='N' ORDER BY MeterTypeCode", false);
            }
            catch { _meterTypeLookup = new DataTable(); }
            RepoMeterType.DataSource = _meterTypeLookup;
            RepoMeterType.DisplayMember = "MeterTypeCode";
            RepoMeterType.ValueMember = "MeterTypeCode";
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
        private void BtnAddMeter_Click(object sender, EventArgs e)
        {
            DataRow r = _meters.NewRow();
            r["MeterRole"] = "NA";
            r["MinimumCharges"] = 0m;
            r["ChargesRate"] = 0m;
            r["MeterMultiPriceCode"] = "";
            r["RebateQtyInPercent"] = 0m;
            r["FOCQty"] = 0m;
            r["InitialReading"] = 0m;
            _meters.Rows.Add(r);
        }

        private void BtnDelMeter_Click(object sender, EventArgs e)
        {
            int rh = GridViewMeters.FocusedRowHandle;
            if (rh >= 0) GridViewMeters.DeleteRow(rh);
        }

        private void GridViewMeters_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "MeterTypeCode" || _meterTypeLookup == null) return;
            string code = e.Value == null ? "" : e.Value.ToString();
            DataRow[] found = _meterTypeLookup.Select("MeterTypeCode='" + code.Replace("'", "''") + "'");
            if (found.Length == 0) return;
            DataRow m = found[0];
            int rh = e.RowHandle;
            GridViewMeters.SetRowCellValue(rh, "MinimumCharges", m["MinimumCharges"]);
            GridViewMeters.SetRowCellValue(rh, "ChargesRate", m["ChargesRate"]);
            GridViewMeters.SetRowCellValue(rh, "MeterMultiPriceCode", m["MeterMultiPriceCode"]);
            GridViewMeters.SetRowCellValue(rh, "RebateQtyInPercent", m["RebateQtyInPercent"]);
            GridViewMeters.SetRowCellValue(rh, "FOCQty", m["FOCQty"]);
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
            { XtraMessageBox.Show("Serial Number is required (it is the key the meter API matches on).", "Validation"); return; }
            if (_standalone && SelectedContractKey == 0 && SelectedDebtorCode.Length == 0)
            { XtraMessageBox.Show("Pick a Contract No, or pick a Customer (a new contract is then created).", "Validation"); return; }

            int bk = 0, cl = 0;
            foreach (DataRow r in _meters.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string role = (r["MeterRole"] == null ? "" : r["MeterRole"].ToString()).Trim().ToUpperInvariant();
                if (role == "BK") bk++;
                else if (role == "CL") cl++;
                if (string.IsNullOrWhiteSpace(r["MeterTypeCode"] as string))
                { XtraMessageBox.Show("Every meter row must have a Meter Type.", "Validation"); return; }
            }
            if (bk > 1) { XtraMessageBox.Show("Only one meter can be tagged Black (BK).", "Validation"); return; }
            if (cl > 1) { XtraMessageBox.Show("Only one meter can be tagged Colour (CL).", "Validation"); return; }

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
            _meters.AcceptChanges();
            _data.Meters = _meters;
            if (_viewItemSp != null) _viewItemSp.PostEditor();
            if (_itemSpareParts != null) { _itemSpareParts.AcceptChanges(); _data.SpareParts = _itemSpareParts; }

            // Extended fields (persisted by PersistItemExtras in the caller's transaction).
            _data.ItemCode = _sluItemCode != null && _sluItemCode.EditValue != null ? _sluItemCode.EditValue.ToString().Trim() : "";
            _data.GradeCode = _sluGrade != null && _sluGrade.EditValue != null ? _sluGrade.EditValue.ToString().Trim() : "";
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
            OpenNativeDepartmentEditor();
        }

        // "+" button on the Project dropdown: open AutoCount's OWN "New Project" form.
        private void SluProject_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Plus) return;
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

                _meters.Rows.Clear();
                DataTable mt = _db.GetDataTable(
                    "SELECT MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                    "RebateQtyInPercent, FOCQty FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + key + " ORDER BY ItemMeterKey", false);
                foreach (DataRow s in mt.Rows)
                {
                    DataRow nr = _meters.NewRow();
                    nr["MeterTypeCode"] = s["MeterTypeCode"];
                    nr["MeterRole"] = s["MeterRole"];
                    nr["MinimumCharges"] = s["MinimumCharges"];
                    nr["ChargesRate"] = s["ChargesRate"];
                    nr["MeterMultiPriceCode"] = s["MeterMultiPriceCode"];
                    nr["RebateQtyInPercent"] = s["RebateQtyInPercent"];
                    nr["FOCQty"] = s["FOCQty"];
                    nr["InitialReading"] = 0m;    // unique per physical machine — starts fresh
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
                    "JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                    "ORDER BY i.ServiceItemNo", false);
            }
            catch { return 0; }
            object k = ServiceContractPhotocopier.Classes.CommonForms.AdvanceSearch_Form.Pick(
                this, "Copy From Service Item", dt, "ItemKey",
                new string[] { "ServiceItemNo", "SerialNumber", "CompanyName" },
                new string[] { "Service Item No", "Serial Number", "Company Name" },
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
        private DevExpress.XtraGrid.GridControl _gridItemSp;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewItemSp;

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
            ChkInactive.Location = new System.Drawing.Point(600, 261);  // make room for Grade on the right column

            // 2. Header: Item Code (SearchLookUpEdit over the Item master) where Stock Location was.
            DevExpress.XtraEditors.LabelControl lblItemCode = new DevExpress.XtraEditors.LabelControl();
            lblItemCode.Text = "Item Code"; lblItemCode.Location = new System.Drawing.Point(14, 238);
            this.Controls.Add(lblItemCode); lblItemCode.BringToFront();
            _itemHdrLookup = zSCP2_Contract_Form.LoadItemLookup(_db);
            _sluItemCode = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluItemCode.Location = new System.Drawing.Point(120, 235);
            _sluItemCode.Size = new System.Drawing.Size(214, 20);
            _sluItemCode.Properties.NullText = "";
            _sluItemCode.Properties.DataSource = _itemHdrLookup;
            _sluItemCode.Properties.ValueMember = "ItemCode";
            _sluItemCode.Properties.DisplayMember = "ItemCode";
            _sluItemCode.EditValueChanged += new EventHandler(ItemCodeHeader_Changed);
            this.Controls.Add(_sluItemCode); _sluItemCode.BringToFront();

            // 3. Header: Grade Code (SearchLookUpEdit over zSCP_LK_ServiceItemGrade) + "+" create, right column.
            DevExpress.XtraEditors.LabelControl lblGrade = new DevExpress.XtraEditors.LabelControl();
            lblGrade.Text = "Grade Code"; lblGrade.Location = new System.Drawing.Point(470, 238);
            this.Controls.Add(lblGrade); lblGrade.BringToFront();
            _sluGrade = new DevExpress.XtraEditors.SearchLookUpEdit();
            _sluGrade.Location = new System.Drawing.Point(600, 235);
            _sluGrade.Size = new System.Drawing.Size(160, 20);
            _sluGrade.Properties.NullText = "";
            _sluGrade.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo),
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Plus)});
            _sluGrade.Properties.ValueMember = "ServiceItemGradeCode";
            _sluGrade.Properties.DisplayMember = "ServiceItemGradeCode";
            _sluGrade.Properties.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(SluGrade_ButtonClick);
            LoadGradeLookup();
            this.Controls.Add(_sluGrade); _sluGrade.BringToFront();

            // 4. Tab control below the header.
            _tabMain = new DevExpress.XtraTab.XtraTabControl();
            _tabMain.Location = new System.Drawing.Point(5, 315);
            _tabMain.Size = new System.Drawing.Size(this.ClientSize.Width - 10, this.ClientSize.Height - 320);
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

            // 5. Tab 1 "Item & Meter": Item Provided grid (Fill) + the Meter group reparented on top.
            BuildItemSparePartsGrid(_pgItemMeter);
            GrpMeters.Dock = System.Windows.Forms.DockStyle.Top;
            GrpMeters.Height = 240;
            _pgItemMeter.Controls.Add(GrpMeters);   // Top added after the Fill grid

            // 6. Tab 3 "More Header" + Tab 5/6 Note/Remarks + Tab 2 Preventive.
            BuildItemMoreHeaderTab(_pgMoreHeader);
            BuildNoteRemarkTabs();
            BuildPreventiveTab(_pgPreventive);
            BuildDebtorHistoryTab(_pgDebtorHist);
            ExtendItemRibbon();

            // 7. Populate the header + More Header + Note/Remarks controls from _data (loaded at step 0).
            _sluItemCode.EditValue = string.IsNullOrEmpty(_data.ItemCode) ? null : (object)_data.ItemCode;
            _sluGrade.EditValue = string.IsNullOrEmpty(_data.GradeCode) ? null : (object)_data.GradeCode;
            foreach (System.Collections.Generic.KeyValuePair<string, DevExpress.XtraEditors.TextEdit> kv in _imh)
                if (_data.MoreHeader.ContainsKey(kv.Key)) kv.Value.Text = _data.MoreHeader[kv.Key];
            if (_imhDelAddress != null && _data.MoreHeader.ContainsKey("DelAddress")) _imhDelAddress.Text = _data.MoreHeader["DelAddress"];
            _txtNote.Text = _data.Note ?? "";
            _txtRemark1.Text = _data.Remark1 ?? "";
            _txtRemark2.Text = _data.Remark2 ?? "";
        }

        private static readonly string[] _imhCols = {
            "City","PostalCode","State","Country","Fax","Ref1","Ref2","Ref3","Ref4",
            "DelBranchCode","DelBranchName","DelAddress","DelCity","DelPostalCode","DelState","DelCountry",
            "DelPhone","DelFax","DelEmail","DelContactPerson" };

        private void LoadExtrasFromDb(long itemKey)
        {
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT ItemCode, GradeCode, Note, Remark1, Remark2, " + string.Join(", ", _imhCols) +
                    ", PMActive, PMIntervalType, PMIntervalValue, PMStartDate, PMLastServiceDate, PMNextServiceDate, PMDept, PMJob, PMLocation" +
                    " FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + itemKey, false);
                if (dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                _data.ItemCode = AsS(r["ItemCode"]); _data.GradeCode = AsS(r["GradeCode"]);
                _data.Note = AsS(r["Note"]); _data.Remark1 = AsS(r["Remark1"]); _data.Remark2 = AsS(r["Remark2"]);
                foreach (string c in _imhCols) _data.MoreHeader[c] = AsS(r[c]);
                _data.PMActive = AsS(r["PMActive"]) == "Y";
                _data.PMIntervalType = AsS(r["PMIntervalType"]);
                _data.PMIntervalValue = r["PMIntervalValue"] == DBNull.Value ? 0 : Convert.ToInt32(r["PMIntervalValue"]);
                _data.PMStartDate = r["PMStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMStartDate"]);
                _data.PMLastServiceDate = r["PMLastServiceDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMLastServiceDate"]);
                _data.PMNextServiceDate = r["PMNextServiceDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["PMNextServiceDate"]);
                _data.PMDept = AsS(r["PMDept"]); _data.PMJob = AsS(r["PMJob"]); _data.PMLocation = AsS(r["PMLocation"]);
            }
            catch { }
        }

        private static string AsS(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }

        // Clipboard ribbon group on the Item Provided grid (mirrors the contract's Clipboard group).
        private const string SI_CLIP = "ATP-SCP-SI-V1";
        private void ExtendItemRibbon()
        {
            DevExpress.XtraBars.BarButtonItem bCW = MakeBar("Copy Whole Document", ItemClipCopyWhole);
            DevExpress.XtraBars.BarButtonItem bCS = MakeBar("Copy Selected Details", ItemClipCopySelected);
            DevExpress.XtraBars.BarButtonItem bCX = MakeBar("Copy as Spreadsheet", ItemClipCopySpreadsheet);
            DevExpress.XtraBars.BarButtonItem bPW = MakeBar("Paste Whole Document", ItemClipPasteWhole);
            DevExpress.XtraBars.BarButtonItem bPI = MakeBar("Paste Item Detail Only", ItemClipPasteItems);
            RibbonCtl.Items.AddRange(new DevExpress.XtraBars.BarItem[] { bCW, bCS, bCX, bPW, bPI });
            DevExpress.XtraBars.Ribbon.RibbonPageGroup grp = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            grp.Text = "Clipboard";
            grp.ItemLinks.Add(bCW); grp.ItemLinks.Add(bCS); grp.ItemLinks.Add(bCX); grp.ItemLinks.Add(bPW); grp.ItemLinks.Add(bPI);
            ribbonPageHome.Groups.Add(grp);
            try
            {
                float dpi = 96f; try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img = AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                bCW.ImageOptions.Image = img.GetSmallImage_CopyToClipboard();
                bCS.ImageOptions.Image = img.GetSmallImage_CopySelectedToClipboard();
                bCX.ImageOptions.Image = img.GetSmallImage_CopyAsSpreadsheet();
                bPW.ImageOptions.Image = img.GetSmallImage_PasteToClipboard();
                bPI.ImageOptions.Image = img.GetSmallImage_PasteSelectedToClipoard();
            }
            catch { }
        }

        private DevExpress.XtraBars.BarButtonItem MakeBar(string caption, DevExpress.XtraBars.ItemClickEventHandler h)
        {
            DevExpress.XtraBars.BarButtonItem b = new DevExpress.XtraBars.BarButtonItem();
            b.Caption = caption; b.Name = "bar_" + caption.Replace(" ", "");
            b.ItemClick += h;
            return b;
        }

        private string ItemProvidedTsv(bool selectedOnly)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("No\tItem Code\tDescription\tUnlimited\tUOM\tQuantity\tDiscount\tUnit Price\tAmount\tTax Type\tTax Inclusive\tTax %\tTax Amount\tAmount After Tax");
            System.Collections.Generic.IEnumerable<int> handles;
            if (selectedOnly) { handles = _viewItemSp.GetSelectedRows(); }
            else { System.Collections.Generic.List<int> all = new System.Collections.Generic.List<int>(); for (int i = 0; i < _viewItemSp.RowCount; i++) all.Add(_viewItemSp.GetVisibleRowHandle(i)); handles = all; }
            foreach (int rh in handles)
            {
                if (rh < 0) continue;
                sb.Append(Cell(rh, "No")).Append('\t').Append(Cell(rh, "ItemCode")).Append('\t').Append(Cell(rh, "Description")).Append('\t')
                  .Append(Cell(rh, "Unlimited")).Append('\t').Append(Cell(rh, "UOM")).Append('\t').Append(Cell(rh, "Quantity")).Append('\t')
                  .Append(Cell(rh, "Discount")).Append('\t').Append(Cell(rh, "UnitPrice")).Append('\t').Append(Cell(rh, "Amount")).Append('\t')
                  .Append(Cell(rh, "TaxType")).Append('\t').Append(Cell(rh, "TaxInclusive")).Append('\t').Append(Cell(rh, "TaxRate")).Append('\t')
                  .Append(Cell(rh, "TaxAmount")).Append('\t').Append(Cell(rh, "AmountAfterTax")).AppendLine();
            }
            return sb.ToString();
        }

        private string Cell(int rh, string field)
        {
            object v = _viewItemSp.GetRowCellValue(rh, field);
            return v == null || v == DBNull.Value ? "" : v.ToString().Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        private void SetClip(string text)
        { try { System.Windows.Forms.Clipboard.SetText(text); XtraMessageBox.Show("Copied.", "Clipboard"); } catch (Exception ex) { XtraMessageBox.Show("Clipboard failed:\r\n" + ex.Message, "Error"); } }

        private void ItemClipCopyWhole(object s, DevExpress.XtraBars.ItemClickEventArgs e) { _viewItemSp.PostEditor(); SetClip(SI_CLIP + "\r\n" + ItemProvidedTsv(false)); }
        private void ItemClipCopySelected(object s, DevExpress.XtraBars.ItemClickEventArgs e)
        { if (_viewItemSp.GetSelectedRows() == null || _viewItemSp.GetSelectedRows().Length == 0) { XtraMessageBox.Show("Select rows first.", "Copy Selected"); return; } _viewItemSp.PostEditor(); SetClip(ItemProvidedTsv(true)); }
        private void ItemClipCopySpreadsheet(object s, DevExpress.XtraBars.ItemClickEventArgs e) { _viewItemSp.PostEditor(); SetClip(ItemProvidedTsv(false)); }
        private void ItemClipPasteWhole(object s, DevExpress.XtraBars.ItemClickEventArgs e) { PasteItemRows(true); }
        private void ItemClipPasteItems(object s, DevExpress.XtraBars.ItemClickEventArgs e) { PasteItemRows(false); }

        private void PasteItemRows(bool requireTag)
        {
            string text = null;
            try { text = System.Windows.Forms.Clipboard.ContainsText() ? System.Windows.Forms.Clipboard.GetText() : null; } catch { }
            if (string.IsNullOrEmpty(text)) { XtraMessageBox.Show("Clipboard is empty.", "Paste"); return; }
            if (requireTag && !text.StartsWith(SI_CLIP)) { XtraMessageBox.Show("The clipboard does not contain a copied Item-Provided document.", "Paste"); return; }
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int added = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0 || line.StartsWith(SI_CLIP)) continue;
                string[] p = line.Split('\t');
                if (p.Length < 3 || p[0] == "No") continue;   // skip header row
                DataRow r = _itemSpareParts.NewRow();
                r["SparePartKey"] = 0L; r["ItemKey"] = DBNull.Value; r["Bound"] = false;
                r["ItemCode"] = At(p, 1); r["Description"] = At(p, 2); r["Unlimited"] = At(p, 3) == "True" || At(p, 3) == "Y";
                r["UOM"] = At(p, 4); r["Quantity"] = DecOr(At(p, 5)); r["Discount"] = At(p, 6); r["UnitPrice"] = DecOr(At(p, 7));
                r["TaxType"] = At(p, 9); r["TaxInclusive"] = At(p, 10) == "True" || At(p, 10) == "Y"; r["TaxRate"] = DecOr(At(p, 11));
                r["Pos"] = _itemSpareParts.Rows.Count;
                zSCP2_Contract_Form.ComputeSpareRow(r);
                _itemSpareParts.Rows.Add(r); added++;
            }
            if (added == 0) { XtraMessageBox.Show("No item rows found on the clipboard.", "Paste"); return; }
            _dirty = true; RenumberItemSp(); _viewItemSp.RefreshData();
            XtraMessageBox.Show(added + " row(s) pasted.", "Pasted");
        }

        private static string At(string[] a, int i) { return (i >= 0 && i < a.Length) ? a[i] : ""; }
        private static decimal DecOr(string s) { decimal d; return decimal.TryParse(s, out d) ? d : 0m; }

        // Single source of truth for the item's EXTENDED columns (Item Code, Grade, More Header, Note,
        // Remarks). Called by BOTH persistence paths (contract editor upsert loop + standalone
        // InsertItemTree) right after the core item row + its ItemKey exist, inside their transaction.
        internal static void PersistItemExtras(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, ItemEditData d, long itemKey)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("UPDATE [dbo].[zSCP2_Item] SET ItemCode=@ItemCode, GradeCode=@GradeCode, ")
              .Append("Note=@Note, Remark1=@Remark1, Remark2=@Remark2, ")
              .Append("PMActive=@PMActive, PMIntervalType=@PMIntervalType, PMIntervalValue=@PMIntervalValue, ")
              .Append("PMStartDate=@PMStartDate, PMLastServiceDate=@PMLastServiceDate, PMNextServiceDate=@PMNextServiceDate, ")
              .Append("PMDept=@PMDept, PMJob=@PMJob, PMLocation=@PMLocation");
            foreach (string c in _imhCols) sb.Append(", [").Append(c).Append("]=@").Append(c);
            sb.Append(", LastModified=GETDATE() WHERE ItemKey=@ik");
            using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sb.ToString(), conn, tx))
            {
                cmd.Parameters.AddWithValue("@ItemCode", d.ItemCode ?? "");
                cmd.Parameters.AddWithValue("@GradeCode", d.GradeCode ?? "");
                cmd.Parameters.AddWithValue("@Note", (object)(d.Note ?? ""));
                cmd.Parameters.AddWithValue("@Remark1", d.Remark1 ?? "");
                cmd.Parameters.AddWithValue("@Remark2", d.Remark2 ?? "");
                cmd.Parameters.AddWithValue("@PMActive", d.PMActive ? "Y" : "N");
                cmd.Parameters.AddWithValue("@PMIntervalType", d.PMIntervalType ?? "NONE");
                cmd.Parameters.AddWithValue("@PMIntervalValue", d.PMIntervalValue);
                cmd.Parameters.AddWithValue("@PMStartDate", (object)d.PMStartDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMLastServiceDate", (object)d.PMLastServiceDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMNextServiceDate", (object)d.PMNextServiceDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PMDept", d.PMDept ?? "");
                cmd.Parameters.AddWithValue("@PMJob", d.PMJob ?? "");
                cmd.Parameters.AddWithValue("@PMLocation", d.PMLocation ?? "");
                foreach (string c in _imhCols)
                    cmd.Parameters.AddWithValue("@" + c, (d.MoreHeader != null && d.MoreHeader.ContainsKey(c)) ? (d.MoreHeader[c] ?? "") : "");
                cmd.Parameters.AddWithValue("@ik", itemKey);
                cmd.ExecuteNonQuery();
            }

            RecordDebtorHistory(conn, tx, d, itemKey);
        }

        // Auto-record a debtor-ownership-history row when the item's customer (its contract's debtor)
        // changes: close the previous open period and open a new one. No-op if unchanged.
        private static void RecordDebtorHistory(System.Data.SqlClient.SqlConnection conn,
            System.Data.SqlClient.SqlTransaction tx, ItemEditData d, long itemKey)
        {
            string debtor;
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(
                "SELECT ISNULL(c.DebtorCode,'') FROM [dbo].[zSCP2_Item] i LEFT JOIN [dbo].[zSCP2_Contract] c " +
                "ON c.ContractKey=i.ContractKey WHERE i.ItemKey=@ik", conn, tx))
            { q.Parameters.AddWithValue("@ik", itemKey); object o = q.ExecuteScalar(); debtor = o == null || o == DBNull.Value ? "" : o.ToString().Trim(); }
            if (debtor.Length == 0) return;

            long openKey = 0; string openDebtor = "";
            using (System.Data.SqlClient.SqlCommand q = new System.Data.SqlClient.SqlCommand(
                "SELECT TOP 1 ServiceItemDebtorHistoryKey, ISNULL(DebtorCode,'') FROM [dbo].[zSCP_ServiceItemDebtorHistory] " +
                "WHERE ServiceItemKey=@ik AND EndDate IS NULL ORDER BY StartDate DESC, ServiceItemDebtorHistoryKey DESC", conn, tx))
            {
                q.Parameters.AddWithValue("@ik", itemKey);
                using (System.Data.SqlClient.SqlDataReader r = q.ExecuteReader()) { if (r.Read()) { openKey = r.GetInt64(0); openDebtor = r.GetString(1); } }
            }
            if (openDebtor == debtor) return;   // unchanged -> nothing to record

            if (openKey > 0)
                using (System.Data.SqlClient.SqlCommand u = new System.Data.SqlClient.SqlCommand(
                    "UPDATE [dbo].[zSCP_ServiceItemDebtorHistory] SET EndDate=CAST(GETDATE() AS DATE), LastModified=GETDATE() WHERE ServiceItemDebtorHistoryKey=@k", conn, tx))
                { u.Parameters.AddWithValue("@k", openKey); u.ExecuteNonQuery(); }

            using (System.Data.SqlClient.SqlCommand ins = new System.Data.SqlClient.SqlCommand(
                "INSERT INTO [dbo].[zSCP_ServiceItemDebtorHistory] (ServiceItemKey, ServiceItemCode, DebtorCode, StartDate, Remark, LastModified) " +
                "VALUES (@ik, @code, @debtor, CAST(GETDATE() AS DATE), @remark, GETDATE())", conn, tx))
            {
                ins.Parameters.AddWithValue("@ik", itemKey);
                ins.Parameters.AddWithValue("@code", d.ServiceItemNo ?? "");
                ins.Parameters.AddWithValue("@debtor", debtor);
                ins.Parameters.AddWithValue("@remark", string.IsNullOrEmpty(d.GradeCode) ? "" : "Grade: " + d.GradeCode);
                ins.ExecuteNonQuery();
            }
        }

        private void ItemCodeHeader_Changed(object sender, EventArgs e)
        {
            _dirty = true;
            if (_sluItemCode.EditValue == null || _itemHdrLookup == null) return;
            string code = _sluItemCode.EditValue.ToString().Trim();
            DataRow[] f = _itemHdrLookup.Select("ItemCode='" + code.Replace("'", "''") + "'");
            if (f.Length > 0 && string.IsNullOrWhiteSpace(TxtDescription.Text)) TxtDescription.Text = f[0]["Description"].ToString();
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

            if (_data.ItemKey <= 0) return;   // a brand-new item has no history yet
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT h.DebtorCode AS [Debtor Code], ISNULL(d.CompanyName,'') AS [Debtor Name], " +
                    "ISNULL((SELECT GradeCode FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + _data.ItemKey + "),'') AS [Service Item Grade], " +
                    "ISNULL((SELECT c.ContractNo FROM [dbo].[zSCP2_Item] i JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey=i.ContractKey WHERE i.ItemKey=" + _data.ItemKey + "),'') AS [Contract No], " +
                    "h.StartDate AS [Service Start Date], h.EndDate AS [End Date] " +
                    "FROM [dbo].[zSCP_ServiceItemDebtorHistory] h LEFT JOIN [dbo].[Debtor] d ON d.AccNo=h.DebtorCode " +
                    "WHERE h.ServiceItemKey=" + _data.ItemKey + " ORDER BY h.StartDate", false);
                grid.DataSource = dt;
                view.PopulateColumns();
                view.BestFitColumns();
            }
            catch { }
        }

        // Preventive Maintenance tab (Image #126): Active gate + interval + service dates + Dept/Job/Location.
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

            _itemSpareParts = _data.SpareParts != null ? _data.SpareParts.Copy() : zSCP2_Contract_Form.CreateSparePartsTable();
            _gridItemSp.DataSource = _itemSpareParts.DefaultView;
            _itemSpareParts.DefaultView.Sort = "Pos";
            zSCP2_Contract_Form.ConfigureSpareView(_viewItemSp, _itemSpCheck, _itemSpItemRepo);
            _viewItemSp.RowHeight = 26;   // ~20% taller rows for easier editing
            RenumberItemSp();

            _viewItemSp.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(ItemSp_CellValueChanged);
        }

        private void ItemSp_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
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
