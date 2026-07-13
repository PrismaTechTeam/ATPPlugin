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

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null) return;
            LoadDebtorLookup();
            LoadContractTypeLookup();
            LoadAgentLookup();
            WireBillingModeRadios();
            LkDebtorCode.EditValueChanged += new EventHandler(OnDebtorChanged);

            if (_isNew)
            {
                AutoPickContractNo();
                DtContractDate.EditValue = DateTime.Today;
                SpnBillingDay.Value = PumsConfig.GetInt(_db, PumsConfig.KEY_DEFAULT_BILLING_DAY, PumsConfig.DEFAULT_BILLING_DAY_VALUE);
                string mode = PumsConfig.Get(_db, PumsConfig.KEY_DEFAULT_BILLING_MODE, PumsConfig.DEFAULT_BILLING_MODE_VALUE);
                SetBillingMode(mode);
            }
            else
            {
                LoadContract();
            }

            RebuildItemsView();
            SetupSparePartsGrid();
            LoadSpareParts();
            BuildMoreHeaderTab();
            LoadMoreHeader();

            // Dirty tracking for the close confirmation: any header edit marks the form dirty. Wired
            // AFTER the initial load so loading an existing contract does not itself set the flag.
            WireDirtyTracking();
            _dirty = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(OnFormClosing);
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
            SluAgent.EditValueChanged += h;
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
                    "ISNULL(DisplayTerm,'') AS DisplayTerm FROM dbo.Debtor ORDER BY AccNo", false);
            }
            catch { _debtorLookup = new DataTable(); }
            LkDebtorCode.Properties.DataSource = _debtorLookup;
            LkDebtorCode.Properties.DisplayMember = "AccNo";
            LkDebtorCode.Properties.ValueMember = "AccNo";
            LkDebtorCode.Properties.Columns.Clear();
            LkDebtorCode.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AccNo", "Code", 90));
            LkDebtorCode.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CompanyName", "Name", 220));
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
            TxtArea.Text = AsStr(d["AreaCode"]);
            SluAgent.EditValue = SetOrNull(AsStr(d["SalesAgent"]));
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
            SetBillingMode(AsStr(r["BillingMode"]));
            TxtAddress.Text = AsStr(r["Address1"]);
            TxtAttention.Text = AsStr(r["Attention"]);
            TxtPhone.Text = AsStr(r["Phone"]);
            TxtTerm.Text = AsStr(r["TermCode"]);
            TxtArea.Text = AsStr(r["AreaCode"]);
            SluAgent.EditValue = SetOrNull(AsStr(r["StaffCode"]));
            TxtDescription.Text = AsStr(r["Description"]);
            TxtRemark1.Text = AsStr(r["Remark1"]);
            TxtRemark2.Text = AsStr(r["Remark2"]);
            TxtNote.Text = AsStr(r["Note"]);
            ChkInactive.Checked = AsStr(r["Inactive"]) == "Y";

            LoadItems();
            }
            finally { _loading = false; }
        }

        private void LoadItems()
        {
            _items.Clear();
            DataTable it = _db.GetDataTable(
                "SELECT * FROM [dbo].[zSCP2_Item] WHERE ContractKey=" + _contractKey + " ORDER BY Pos, ItemKey", false);
            foreach (DataRow r in it.Rows)
            {
                ItemEditData d = new ItemEditData();
                d.ItemKey = Convert.ToInt64(r["ItemKey"]);
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

                DataTable m = _db.GetDataTable(
                    "SELECT * FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + d.ItemKey + " ORDER BY ItemMeterKey", false);
                foreach (DataRow mr in m.Rows)
                {
                    DataRow nr = d.Meters.NewRow();
                    nr["MeterTypeCode"] = AsStr(mr["MeterTypeCode"]);
                    nr["MeterRole"] = AsStr(mr["MeterRole"]);
                    nr["MinimumCharges"] = AsDec(mr["MinimumCharges"]);
                    nr["ChargesRate"] = AsDec(mr["ChargesRate"]);
                    nr["MeterMultiPriceCode"] = AsStr(mr["MeterMultiPriceCode"]);
                    nr["RebateQtyInPercent"] = AsDec(mr["RebateQtyInPercent"]);
                    nr["FOCQty"] = AsDec(mr["FOCQty"]);
                    nr["InitialReading"] = AsDec(mr["InitialReading"]);
                    d.Meters.Rows.Add(nr);
                }
                d.Meters.AcceptChanges();

                DataTable ics = _db.GetDataTable(
                    "SELECT * FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=" + d.ItemKey + " ORDER BY Pos, ItemCodeKey", false);
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
                _items.Add(d);
            }
        }

        private void RebuildItemsView()
        {
            _dtItemsView = new DataTable();
            _dtItemsView.Columns.Add("No", typeof(int));
            _dtItemsView.Columns.Add("ServiceItemNo", typeof(string));
            _dtItemsView.Columns.Add("SerialNumber", typeof(string));
            _dtItemsView.Columns.Add("Items", typeof(string));
            _dtItemsView.Columns.Add("BillingDay", typeof(string));
            _dtItemsView.Columns.Add("BKMeter", typeof(string));
            _dtItemsView.Columns.Add("CLMeter", typeof(string));
            _dtItemsView.Columns.Add("Inactive", typeof(string));
            _dtItemsView.Columns.Add("Expiry", typeof(DateTime));

            int n = 1;
            foreach (ItemEditData d in _items)
            {
                DataRow r = _dtItemsView.NewRow();
                r["No"] = n++;
                r["ServiceItemNo"] = d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber;
                r["Items"] = ItemCodesSummary(d);
                r["BillingDay"] = d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value.ToString() : "(contract)";
                r["BKMeter"] = MeterForRole(d, "BK");
                r["CLMeter"] = MeterForRole(d, "CL");
                r["Inactive"] = d.Inactive ? "Y" : "N";
                r["Expiry"] = (object)d.ServiceExpiryDate ?? DBNull.Value;
                _dtItemsView.Rows.Add(r);
            }
            GridItems.DataSource = _dtItemsView;
            GridViewItems.BestFitColumns();
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

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            ItemEditData d = new ItemEditData();
            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, d, (int)SpnBillingDay.Value))
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
            using (zSCP2_Item_Form dlg = new zSCP2_Item_Form(_db, _items[idx], (int)SpnBillingDay.Value))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) { _dirty = true; RebuildItemsView(); }
            }
        }

        private void BtnDelItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            if (XtraMessageBox.Show("Remove the selected service item from this contract?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _items.RemoveAt(idx);
            _dirty = true;
            RebuildItemsView();
        }

        private void GridViewItems_DoubleClick(object sender, EventArgs e) { BtnEditItem_Click(null, null); }

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

        private static DataTable CreateSparePartsTable()
        {
            DataTable dt = new DataTable("SpareParts");
            dt.Columns.Add("SparePartKey", typeof(long));
            dt.Columns.Add("ItemKey", typeof(long));       // set => item-bound (read-only on the contract)
            dt.Columns.Add("Bound", typeof(bool));         // ItemKey present -> true
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemCode", typeof(string));
            dt.Columns.Add("Description", typeof(string));
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

            GridViewSpareParts.OptionsView.NewItemRowPosition = DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None;
            GridViewSpareParts.OptionsBehavior.Editable = true;
            GridViewSpareParts.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(SpareParts_CellValueChanged);
            GridViewSpareParts.ShowingEditor += new System.ComponentModel.CancelEventHandler(SpareParts_ShowingEditor);

            _spareParts = CreateSparePartsTable();
            GridSpareParts.DataSource = _spareParts.DefaultView;
            _spareParts.DefaultView.Sort = "Pos";

            GridViewSpareParts.Columns.Clear();
            GridViewSpareParts.PopulateColumns();
            SpCol("SparePartKey", null, 0, -1); if (GridViewSpareParts.Columns["SparePartKey"] != null) GridViewSpareParts.Columns["SparePartKey"].Visible = false;
            SpCol("ItemKey", null, 0, -1); if (GridViewSpareParts.Columns["ItemKey"] != null) GridViewSpareParts.Columns["ItemKey"].Visible = false;
            SpCol("Bound", null, 0, -1); if (GridViewSpareParts.Columns["Bound"] != null) GridViewSpareParts.Columns["Bound"].Visible = false;
            SpCol("Pos", null, 0, -1); if (GridViewSpareParts.Columns["Pos"] != null) GridViewSpareParts.Columns["Pos"].Visible = false;
            SpCol("No", "No", 40, 0, true);
            SpCol("ItemCode", "Item Code", 130, 1);
            SpCol("Description", "Description", 240, 2);
            SpBoolCol("Unlimited", "Unlimited", 70, 3);
            SpCol("UOM", "UOM", 70, 4);
            SpCol("Quantity", "Quantity", 80, 5);
            SpCol("Discount", "Discount", 80, 6);
            SpCol("UnitPrice", "Unit Price", 90, 7);
            SpCol("Amount", "Amount", 90, 8, true);
            SpCol("TaxType", "Tax Type", 80, 9);
            SpBoolCol("TaxInclusive", "Tax Inclusive", 90, 10);
            SpCol("TaxRate", "Tax (%)", 70, 11);
            SpCol("TaxAmount", "Tax Amount", 90, 12, true);
            SpCol("AmountAfterTax", "Amount After Tax", 110, 13, true);
        }

        private void SpCol(string field, string caption, int width, int visibleIndex, bool readOnly = false)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewSpareParts.Columns[field];
            if (c == null) return;
            if (caption != null) c.Caption = caption;
            if (width > 0) c.Width = width;
            if (visibleIndex >= 0) { c.Visible = true; c.VisibleIndex = visibleIndex; }
            if (readOnly) { c.OptionsColumn.AllowEdit = false; c.OptionsColumn.ReadOnly = true; }
        }

        private void SpBoolCol(string field, string caption, int width, int visibleIndex)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewSpareParts.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
            c.ColumnEdit = _spCheck;
        }

        // Item-bound lines are read-only on the contract (they belong to a service item); block editing.
        private void SpareParts_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int rh = GridViewSpareParts.FocusedRowHandle;
            if (rh < 0) return;
            object bound = GridViewSpareParts.GetRowCellValue(rh, "Bound");
            if (bound != null && bound != DBNull.Value && Convert.ToBoolean(bound)) e.Cancel = true;
        }

        private void SpareParts_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            _dirty = true;
            if (e.Column.FieldName == "Amount" || e.Column.FieldName == "TaxAmount" ||
                e.Column.FieldName == "AmountAfterTax" || e.Column.FieldName == "No") return;
            GridViewSpareParts.PostEditor();
            DataRowView drv = GridViewSpareParts.GetRow(e.RowHandle) as DataRowView;
            if (drv != null) ComputeSpareRow(drv.Row);
            GridViewSpareParts.RefreshData();
        }

        private static void ComputeSpareRow(DataRow r)
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
                    "SELECT SparePartKey, ItemKey, ItemCode, Description, Unlimited, UOM, Quantity, Discount, " +
                    "UnitPrice, TaxType, TaxInclusive, TaxRate, Pos FROM [dbo].[zSCP2_ContractSparePart] " +
                    "WHERE ContractKey=" + _contractKey + " ORDER BY Pos", false);
                foreach (DataRow s in dt.Rows)
                {
                    DataRow r = _spareParts.NewRow();
                    r["SparePartKey"] = s["SparePartKey"];
                    r["ItemKey"] = s["ItemKey"];
                    r["Bound"] = s["ItemKey"] != DBNull.Value;
                    r["ItemCode"] = s["ItemCode"]; r["Description"] = s["Description"];
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
                    "(ContractKey, ItemKey, ItemCode, Description, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                    " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                    "VALUES (@ck, NULL, @code, @desc, @unl, @uom, @qty, @disc, @price, @ttype, @tinc, @trate, @pos, GETDATE())",
                    P("@ck", _contractKey), P("@code", code),
                    P("@desc", AsStr(r["Description"])),
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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtContractNo.Text))
            { XtraMessageBox.Show("Contract No is required.", "Validation"); return; }
            string debtor = LkDebtorCode.EditValue == null ? "" : LkDebtorCode.EditValue.ToString();
            if (string.IsNullOrWhiteSpace(debtor))
            { XtraMessageBox.Show("Customer (Debtor) is required.", "Validation"); return; }

            // Reserve the REAL contract number now (only if new & still showing the auto-preview) so
            // the counter is consumed on save, not on every Auto click.
            if (_isNew && !string.IsNullOrEmpty(_autoPeekNo) && TxtContractNo.Text.Trim() == _autoPeekNo)
                TxtContractNo.Text = ServiceContractPhotocopier.Classes.ScpDocNo.Next(
                    _db, ServiceContractPhotocopier.Classes.ScpDocNo.DOCTYPE_CONTRACT);

            // Embedded items showing an auto-preview number: reserve a real one each at save.
            foreach (ItemEditData d in _items)
            {
                if (d.ServiceItemNoIsAuto)
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
                        if (_isNew) InsertContract(conn, tx, debtor);
                        else UpdateContract(conn, tx, debtor);

                        // delete-then-reinsert children (cascade removes meters when item rows go)
                        ExecNonQuery(conn, tx, "DELETE FROM [dbo].[zSCP2_Item] WHERE ContractKey=@ck",
                            P("@ck", _contractKey));

                        int pos = 0;
                        foreach (ItemEditData d in _items)
                        {
                            long itemKey = InsertItem(conn, tx, d, pos++);
                            InsertMeters(conn, tx, d, itemKey);
                            InsertItemCodes(conn, tx, d, itemKey);
                        }
                        SaveSpareParts(conn, tx);
                        SaveMoreHeader(conn, tx);
                        tx.Commit();
                    }
                }
                XtraMessageBox.Show("Contract saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _dirty = false;
                _savedOk = true;   // skip the unsaved-changes prompt on the close that follows
                this.DialogResult = DialogResult.OK;
                this.Close();
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
                " ContractValue, BillingDay, BillingMode, Address1, Attention, Phone, TermCode, AreaCode, StaffCode, " +
                " Description, Remark1, Remark2, Note, Inactive, Created, LastModified) " +
                "VALUES (@no,@type,@debtor,@cdate,@sdate,@edate,@val,@bday,@bmode,@addr,@attn,@phone,@term,@area,@staff," +
                "@desc,@r1,@r2,@note,@inact,GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);";
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
                "BillingDay=@bday, BillingMode=@bmode, Address1=@addr, Attention=@attn, Phone=@phone, TermCode=@term, " +
                "AreaCode=@area, StaffCode=@staff, Description=@desc, Remark1=@r1, Remark2=@r2, Note=@note, " +
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
            cmd.Parameters.AddWithValue("@bday", (byte)SpnBillingDay.Value);
            cmd.Parameters.AddWithValue("@bmode", CurrentBillingMode());
            cmd.Parameters.AddWithValue("@addr", TxtAddress.Text.Trim());
            cmd.Parameters.AddWithValue("@attn", TxtAttention.Text.Trim());
            cmd.Parameters.AddWithValue("@phone", TxtPhone.Text.Trim());
            cmd.Parameters.AddWithValue("@term", TxtTerm.Text.Trim());
            cmd.Parameters.AddWithValue("@area", TxtArea.Text.Trim());
            cmd.Parameters.AddWithValue("@staff", SluAgent.EditValue == null ? "" : SluAgent.EditValue.ToString().Trim());
            cmd.Parameters.AddWithValue("@desc", TxtDescription.Text.Trim());
            cmd.Parameters.AddWithValue("@r1", TxtRemark1.Text.Trim());
            cmd.Parameters.AddWithValue("@r2", TxtRemark2.Text.Trim());
            cmd.Parameters.AddWithValue("@note", (object)(TxtNote.Text ?? ""));
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
                    "(ItemKey, MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                    " RebateQtyInPercent, FOCQty, InitialReading, LastModified) " +
                    "VALUES (@ik,@code,@role,@min,@rate,@multi,@rebate,@foc,@init,GETDATE());";
                using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@ik", itemKey);
                    cmd.Parameters.AddWithValue("@code", code);
                    string role = r["MeterRole"] == null ? "NA" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    if (role != "BK" && role != "CL") role = "NA";
                    cmd.Parameters.AddWithValue("@role", role);
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
