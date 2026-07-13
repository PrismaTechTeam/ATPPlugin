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
                if (dlg.ShowDialog(this) == DialogResult.OK) RebuildItemsView();
            }
        }

        private void BtnDelItem_Click(object sender, EventArgs e)
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return;
            int idx = Convert.ToInt32(GridViewItems.GetRowCellValue(rh, "No")) - 1;
            if (idx < 0 || idx >= _items.Count) return;
            _items.RemoveAt(idx);
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
                        tx.Commit();
                    }
                }
                XtraMessageBox.Show("Contract saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
