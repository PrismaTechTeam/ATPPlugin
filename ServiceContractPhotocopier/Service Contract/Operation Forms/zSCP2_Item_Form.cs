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
        public DateTime? ServiceExpiryDate;   // from master; null = none. Drives the Expiry column colour.
        public DataTable Meters;          // schema = CreateMetersTable()
        public DataTable ItemCodes;       // schema = CreateItemCodesTable()
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

            LoadMeterTypeLookup();
            LoadItemLookup();
            LoadSerialLookup();
            GridViewItemCodes.ShownEditor += new EventHandler(GridViewItemCodes_ShownEditor);

            TxtServiceItemNo.Text = _data.ServiceItemNo;
            TxtSerial.Text = _data.SerialNumber;
            TxtDescription.Text = _data.Description;
            SpnBillingDayOverride.Value = _data.BillingDayOverride.HasValue ? _data.BillingDayOverride.Value : 0;
            TxtDept.Text = _data.DepartmentCode;
            TxtJob.Text = _data.JobCode;
            TxtLocation.Text = _data.StockLocationCode;
            ChkInactive.Checked = _data.Inactive;

            _itemCodes = _data.ItemCodes != null ? _data.ItemCodes.Copy() : CreateItemCodesTable();
            GridItemCodes.DataSource = _itemCodes;

            _meters = _data.Meters != null ? _data.Meters.Copy() : CreateMetersTable();
            GridMeters.DataSource = _meters;

            if (string.IsNullOrEmpty(_data.ServiceItemNo)) AutoPickServiceItemNo();
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

        private void AutoPickServiceItemNo()
        {
            try
            {
                object o = _db.ExecuteScalar(
                    "SELECT ISNULL(MAX(CONVERT(int, SUBSTRING(ServiceItemNo,4,20))),0)+1 " +
                    "FROM [dbo].[zSCP2_Item] WHERE ServiceItemNo LIKE 'SI-%' AND ISNUMERIC(SUBSTRING(ServiceItemNo,4,20))=1");
                int next = (o == null || o == DBNull.Value) ? 1 : Convert.ToInt32(o);
                TxtServiceItemNo.Text = "SI-" + next.ToString("000000");
            }
            catch { TxtServiceItemNo.Text = "SI-000001"; }
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
            _data.SerialNumber = TxtSerial.Text.Trim();
            _data.Description = TxtDescription.Text.Trim();
            int bd = (int)SpnBillingDayOverride.Value;
            _data.BillingDayOverride = (bd >= 1 && bd <= 31) ? (int?)bd : null;
            _data.DepartmentCode = TxtDept.Text.Trim();
            _data.JobCode = TxtJob.Text.Trim();
            _data.StockLocationCode = TxtLocation.Text.Trim();
            _data.Inactive = ChkInactive.Checked;
            _itemCodes.AcceptChanges();
            _data.ItemCodes = _itemCodes;
            _meters.AcceptChanges();
            _data.Meters = _meters;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
