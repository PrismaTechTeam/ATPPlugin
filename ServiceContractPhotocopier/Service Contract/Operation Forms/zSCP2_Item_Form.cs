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
        private DevExpress.XtraEditors.LabelControl _lblCustomer;
        private DevExpress.XtraEditors.LookUpEdit _lkCustomer;
        private DevExpress.XtraEditors.LookUpEdit _lkContract;      // standalone: attach to an EXISTING contract
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
                lblC.Location = new System.Drawing.Point(470, 264);
                this.Controls.Add(lblC); lblC.BringToFront();
                DevExpress.XtraEditors.TextEdit txtC = new DevExpress.XtraEditors.TextEdit();
                txtC.Location = new System.Drawing.Point(600, 261);
                txtC.Size = new System.Drawing.Size(160, 20);
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
                GrpItemCodes.Top += 30;
                GrpMeters.Top += 30;
                GrpMeters.Height -= 30;

                DevExpress.XtraEditors.LabelControl lblContract = new DevExpress.XtraEditors.LabelControl();
                lblContract.Text = "Contract No";
                lblContract.Location = new System.Drawing.Point(14, 264);
                this.Controls.Add(lblContract);
                lblContract.BringToFront();

                _lkContract = new DevExpress.XtraEditors.LookUpEdit();
                _lkContract.Location = new System.Drawing.Point(120, 261);
                _lkContract.Size = new System.Drawing.Size(214, 20);
                _lkContract.Properties.NullText = "(create new contract)";
                _lkContract.Properties.ValueMember = "ContractKey";
                _lkContract.Properties.DisplayMember = "ContractNo";
                _lkContract.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ContractNo", "Contract No", 90));
                _lkContract.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("DebtorCode", "Customer", 80));
                _lkContract.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CompanyName", "Company Name", 220));
                _lkContract.Properties.SearchMode = DevExpress.XtraEditors.Controls.SearchMode.AutoFilter;
                _lkContract.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
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
                _lblCustomer.Location = new System.Drawing.Point(470, 264);   // align with the right column (Dept/Project)
                this.Controls.Add(_lblCustomer);
                _lblCustomer.BringToFront();

                _lkCustomer = new DevExpress.XtraEditors.LookUpEdit();
                _lkCustomer.Location = new System.Drawing.Point(600, 261);
                _lkCustomer.Size = new System.Drawing.Size(210, 20);
                _lkCustomer.Properties.NullText = "Select customer...";
                _lkCustomer.Properties.ValueMember = "AccNo";
                _lkCustomer.Properties.DisplayMember = "AccNo";
                _lkCustomer.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("AccNo", "Code", 90));
                _lkCustomer.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CompanyName", "Company Name", 240));
                _lkCustomer.Properties.SearchMode = DevExpress.XtraEditors.Controls.SearchMode.AutoFilter;
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
            GridItemCodes.DataSource = _itemCodes;

            _meters = _data.Meters != null ? _data.Meters.Copy() : CreateMetersTable();
            GridMeters.DataSource = _meters;

            BuildItemSparePartsGrid();

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
            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = "Copy From Service Item";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.ClientSize = new System.Drawing.Size(440, 105);

                DevExpress.XtraEditors.LabelControl lbl = new DevExpress.XtraEditors.LabelControl();
                lbl.Text = "Service Item";
                lbl.Location = new System.Drawing.Point(14, 18);
                dlg.Controls.Add(lbl);

                DevExpress.XtraEditors.LookUpEdit lk = new DevExpress.XtraEditors.LookUpEdit();
                lk.Location = new System.Drawing.Point(100, 15);
                lk.Size = new System.Drawing.Size(320, 20);
                lk.Properties.ValueMember = "ItemKey";
                lk.Properties.DisplayMember = "ServiceItemNo";
                lk.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("ServiceItemNo", "Service Item No", 110));
                lk.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("SerialNumber", "Serial", 90));
                lk.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("CompanyName", "Company Name", 200));
                lk.Properties.SearchMode = DevExpress.XtraEditors.Controls.SearchMode.AutoFilter;
                try
                {
                    lk.Properties.DataSource = _db.GetDataTable(
                        "SELECT i.ItemKey, i.ServiceItemNo, i.SerialNumber, ISNULL(d.CompanyName,'') AS CompanyName " +
                        "FROM [dbo].[zSCP2_Item] i " +
                        "JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                        "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                        "ORDER BY i.ServiceItemNo", false);
                }
                catch { }
                dlg.Controls.Add(lk);

                SimpleButton ok = new SimpleButton();
                ok.Text = "OK"; ok.Location = new System.Drawing.Point(245, 62); ok.Size = new System.Drawing.Size(85, 28);
                ok.Click += delegate { dlg.DialogResult = DialogResult.OK; };
                dlg.Controls.Add(ok);
                SimpleButton cancel = new SimpleButton();
                cancel.Text = "Cancel"; cancel.Location = new System.Drawing.Point(336, 62); cancel.Size = new System.Drawing.Size(85, 28);
                cancel.Click += delegate { dlg.DialogResult = DialogResult.Cancel; };
                dlg.Controls.Add(cancel);

                if (dlg.ShowDialog(this) != DialogResult.OK || lk.EditValue == null || lk.EditValue == DBNull.Value) return 0;
                long k;
                return long.TryParse(lk.EditValue.ToString(), out k) ? k : 0;
            }
        }

        // ===================== Spare Parts provided by this Service Item =====================
        // These are stored in zSCP2_ContractSparePart with ItemKey set, so they show read-only on the
        // parent contract. Built in code below the Meter group (reuses the contract's shared column
        // layout + compute so the two grids are identical).

        private DataTable _itemSpareParts;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit _itemSpCheck;
        private DevExpress.XtraGrid.GridControl _gridItemSp;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewItemSp;

        private void BuildItemSparePartsGrid()
        {
            // Make room: shrink the Meter group and dock a Spare Parts group beneath it.
            GrpMeters.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            if (GrpMeters.Height > 220) GrpMeters.Height = 220;

            DevExpress.XtraEditors.GroupControl grp = new DevExpress.XtraEditors.GroupControl();
            grp.Text = "Spare Parts / Services Provided by this Service Item";
            grp.Location = new System.Drawing.Point(GrpMeters.Left, GrpMeters.Bottom + 8);
            grp.Size = new System.Drawing.Size(GrpMeters.Width, Math.Max(150, this.ClientSize.Height - GrpMeters.Bottom - 20));
            grp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.Controls.Add(grp);
            grp.BringToFront();

            DevExpress.XtraEditors.SimpleButton bIns = new DevExpress.XtraEditors.SimpleButton();
            bIns.Text = "Insert Row"; bIns.Location = new System.Drawing.Point(8, 26); bIns.Size = new System.Drawing.Size(90, 24);
            bIns.Click += new EventHandler(ItemSpInsert_Click); grp.Controls.Add(bIns);
            DevExpress.XtraEditors.SimpleButton bRem = new DevExpress.XtraEditors.SimpleButton();
            bRem.Text = "Remove Row"; bRem.Location = new System.Drawing.Point(102, 26); bRem.Size = new System.Drawing.Size(90, 24);
            bRem.Click += new EventHandler(ItemSpRemove_Click); grp.Controls.Add(bRem);
            DevExpress.XtraEditors.SimpleButton bUp = new DevExpress.XtraEditors.SimpleButton();
            bUp.Text = "Move Up"; bUp.Location = new System.Drawing.Point(200, 26); bUp.Size = new System.Drawing.Size(80, 24);
            bUp.Click += delegate { ItemSpMove(-1); }; grp.Controls.Add(bUp);
            DevExpress.XtraEditors.SimpleButton bDn = new DevExpress.XtraEditors.SimpleButton();
            bDn.Text = "Move Down"; bDn.Location = new System.Drawing.Point(284, 26); bDn.Size = new System.Drawing.Size(80, 24);
            bDn.Click += delegate { ItemSpMove(1); }; grp.Controls.Add(bDn);

            _gridItemSp = new DevExpress.XtraGrid.GridControl();
            _gridItemSp.Location = new System.Drawing.Point(2, 54);
            _gridItemSp.Size = new System.Drawing.Size(grp.Width - 6, grp.Height - 58);
            _gridItemSp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _viewItemSp = new DevExpress.XtraGrid.Views.Grid.GridView(_gridItemSp);
            _gridItemSp.MainView = _viewItemSp;
            _gridItemSp.ViewCollection.Add(_viewItemSp);
            grp.Controls.Add(_gridItemSp);

            _itemSpCheck = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            _gridItemSp.RepositoryItems.Add(_itemSpCheck);

            _itemSpareParts = _data.SpareParts != null ? _data.SpareParts.Copy() : zSCP2_Contract_Form.CreateSparePartsTable();
            _gridItemSp.DataSource = _itemSpareParts.DefaultView;
            _itemSpareParts.DefaultView.Sort = "Pos";
            zSCP2_Contract_Form.ConfigureSpareView(_viewItemSp, _itemSpCheck);
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
            if (drv != null) zSCP2_Contract_Form.ComputeSpareRow(drv.Row);
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
