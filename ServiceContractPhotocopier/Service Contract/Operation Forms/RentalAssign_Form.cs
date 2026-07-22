using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Assign a RENTAL to a machine: pick the service item + a flat (rental) meter type + the
    /// amount / free months / period settings; OK inserts ONE zSCP2_ItemMeter row (mirroring the
    /// contract editor's InsertMeters column list) and audits the assignment (source RENTAL).
    /// </summary>
    public partial class RentalAssign_Form : XtraForm
    {
        private readonly DBSetting _db;
        private DataTable _items;
        private DataTable _types;

        public RentalAssign_Form() { InitializeComponent(); }
        public RentalAssign_Form(DBSetting db) : this()
        {
            _db = db;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null) return;
            try
            {
                _items = _db.GetDataTable(
                    "SELECT i.ItemKey, i.ServiceItemNo, ISNULL(i.SerialNumber,'') AS SerialNumber, " +
                    "ISNULL(c.ContractKey,0) AS ContractKey, ISNULL(c.ContractNo,'') AS ContractNo, " +
                    "ISNULL(c.DebtorCode,'') AS DebtorCode, ISNULL(d.CompanyName,'') AS Customer " +
                    "FROM dbo.zSCP2_Item i " +
                    "LEFT JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "WHERE i.Inactive = 'N' ORDER BY i.ServiceItemNo", false);
            }
            catch { _items = new DataTable(); }
            SluItem.Properties.DataSource = _items;
            SluItem.Properties.ValueMember = "ItemKey";
            SluItem.Properties.DisplayMember = "ServiceItemNo";
            SluItemView.OptionsBehavior.AutoPopulateColumns = false;
            SluItemView.Columns.Clear();
            AddCol(SluItemView, "ServiceItemNo", "Service Item", 120);
            AddCol(SluItemView, "SerialNumber", "Serial", 100);
            AddCol(SluItemView, "ContractNo", "Contract", 100);
            AddCol(SluItemView, "Customer", "Customer", 220);
            SluItemView.OptionsView.ShowAutoFilterRow = true;

            try
            {
                _types = _db.GetDataTable(
                    "SELECT MeterTypeCode, ISNULL([Description],'') AS Description, ISNULL(ChargesRate,0) AS ChargesRate, " +
                    "ISNULL(MinimumCharges,0) AS MinimumCharges " +
                    "FROM dbo.zSCP_MeterType WHERE ISNULL(IsFlatCharge,'N') = 'Y' AND Inactive = 'N' ORDER BY MeterTypeCode", false);
            }
            catch { _types = new DataTable(); }
            SluType.Properties.DataSource = _types;
            SluType.Properties.ValueMember = "MeterTypeCode";
            SluType.Properties.DisplayMember = "MeterTypeCode";
            SluTypeView.OptionsBehavior.AutoPopulateColumns = false;
            SluTypeView.Columns.Clear();
            AddCol(SluTypeView, "MeterTypeCode", "Rental Type", 150);
            AddCol(SluTypeView, "Description", "Description", 240);
            AddCol(SluTypeView, "ChargesRate", "Amount", 80);
            SluTypeView.OptionsView.ShowAutoFilterRow = true;
            SluType.EditValueChanged += new EventHandler(SluType_Changed);

            DtStart.EditValue = null;
        }

        private static void AddCol(DevExpress.XtraGrid.Views.Grid.GridView v, string field, string caption, int width)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = v.Columns.AddVisible(field);
            c.Caption = caption; c.Width = width;
        }

        // Picking a rental type defaults the amount fields from the type master.
        private void SluType_Changed(object sender, EventArgs e)
        {
            if (_types == null || SluType.EditValue == null) return;
            DataRow[] f = _types.Select("MeterTypeCode='" + SluType.EditValue.ToString().Replace("'", "''") + "'");
            if (f.Length == 0) return;
            SpnAmount.Value = Convert.ToDecimal(f[0]["ChargesRate"]);
            SpnMin.Value = Convert.ToDecimal(f[0]["MinimumCharges"]);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (SluItem.EditValue == null) { XtraMessageBox.Show("Pick a Service Item.", "Validation"); return; }
            if (SluType.EditValue == null) { XtraMessageBox.Show("Pick a Rental (flat) meter type.", "Validation"); return; }
            long itemKey = Convert.ToInt64(SluItem.EditValue);
            string type = SluType.EditValue.ToString().Trim();
            DataRow[] itemRow = _items.Select("ItemKey=" + itemKey);
            long ck = itemRow.Length > 0 ? Convert.ToInt64(itemRow[0]["ContractKey"]) : 0;
            string cno = itemRow.Length > 0 ? Convert.ToString(itemRow[0]["ContractNo"]) : "";

            try
            {
                // Friendly duplicate check before the unique key (ItemKey, MeterTypeCode, MachineSerialNo='').
                object dup = _db.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter WHERE ItemKey=" + itemKey +
                    " AND MeterTypeCode=N'" + type.Replace("'", "''") + "' AND ISNULL(MachineSerialNo,'')=''");
                if (dup != null && dup != DBNull.Value && Convert.ToInt32(dup) > 0)
                {
                    XtraMessageBox.Show("That machine already has rental type '" + type + "'. Edit it in the grid instead.",
                        "Already Assigned", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("RentalAssign"))
                    {
                        try
                        {
                            string desc = "";
                            DataRow[] t = _types.Select("MeterTypeCode='" + type.Replace("'", "''") + "'");
                            if (t.Length > 0) desc = Convert.ToString(t[0]["Description"]);

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO dbo.zSCP2_ItemMeter " +
                                "(ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate, " +
                                " MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, RentalStartDate, RentalMonths, RentalBasis, LastModified) " +
                                "VALUES (@ik, @type, @desc, 'NA', '', @min, @rate, '', 0, @foc, 0, @rsd, @rm, @rb, GETDATE())", cn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ik", itemKey);
                                cmd.Parameters.AddWithValue("@type", type);
                                cmd.Parameters.AddWithValue("@desc", desc ?? "");
                                cmd.Parameters.AddWithValue("@min", SpnMin.Value);
                                cmd.Parameters.AddWithValue("@rate", SpnAmount.Value);
                                cmd.Parameters.AddWithValue("@foc", SpnFreeMonths.Value);
                                cmd.Parameters.AddWithValue("@rsd", DtStart.EditValue == null || DtStart.EditValue == DBNull.Value
                                    ? (object)DBNull.Value : (object)Convert.ToDateTime(DtStart.EditValue).Date);
                                cmd.Parameters.AddWithValue("@rm", (int)SpnMonths.Value);
                                cmd.Parameters.AddWithValue("@rb", CmbBasis.SelectedIndex == 1 ? "P" : "A");
                                cmd.ExecuteNonQuery();
                            }

                            ScpContractAudit.WriteRow(cn, tx, ck, cno, itemKey, null, Guid.NewGuid(),
                                ScpContractAudit.SOURCE_RENTAL, "RENTAL.ASSIGN", null,
                                type + " @ " + SpnAmount.Value.ToString("0.00") +
                                (SpnFreeMonths.Value > 0 ? " (free " + SpnFreeMonths.Value.ToString("0") + " mth)" : ""));
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Assign failed:\r\n" + ex.Message, "Error"); return; }
            this.DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; }
    }
}
