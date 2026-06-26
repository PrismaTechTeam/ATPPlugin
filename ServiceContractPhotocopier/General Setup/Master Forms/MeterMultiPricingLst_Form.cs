using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Meter Multi Pricing",
    // ParentMenuCaption = "General Setup", MenuOrder = 140, ParentMenuOrder = 600,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_METER_MULTI_PRICE,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_METER_MULTI_PRICE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class MeterMultiPricingLst_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private string _selectedCode = "";
        private bool _isNewRow = false;
        private DataTable _dtItems;

        public MeterMultiPricingLst_Form() { InitializeComponent(); }
        public MeterMultiPricingLst_Form(UserSession userSession) : this()
        { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public MeterMultiPricingLst_Form(DBSetting dbSetting) : this()
        { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            _dtItems = new DataTable();
            _dtItems.Columns.Add("MeterReading", typeof(decimal));
            _dtItems.Columns.Add("UnitPrice", typeof(decimal));
            GridItems.DataSource = _dtItems;
            LoadGrid();
            GridViewMP.FocusedRowChanged += delegate { PopulateDetail(); };
            SetReadOnly(true);
        }

        private void SetReadOnly(bool ro)
        {
            TxtCode.Properties.ReadOnly = true;
            TxtDesc.Properties.ReadOnly = ro;
            GridViewItems.OptionsBehavior.Editable = !ro;
            GridViewItems.OptionsView.NewItemRowPosition = ro
                ? DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.None
                : DevExpress.XtraGrid.Views.Grid.NewItemRowPosition.Bottom;
            BtnAddRow.Enabled = !ro; BtnDelRow.Enabled = !ro;
            BtnRowUp.Enabled = !ro; BtnRowDown.Enabled = !ro;
        }

        private int CurrentItemIndex()
        {
            int rh = GridViewItems.FocusedRowHandle;
            if (rh < 0) return -1;
            var row = GridViewItems.GetDataRow(rh);
            if (row == null) return -1;
            return _dtItems.Rows.IndexOf(row);
        }

        private void OnAddItemRow(object sender, EventArgs e)
        {
            GridViewItems.CloseEditor(); GridViewItems.UpdateCurrentRow();
            var nr = _dtItems.NewRow();
            nr["MeterReading"] = 0m; nr["UnitPrice"] = 0m;
            _dtItems.Rows.Add(nr);
            int last = _dtItems.Rows.Count - 1;
            GridViewItems.FocusedRowHandle = GridViewItems.GetRowHandle(last);
            GridViewItems.FocusedColumn = GridViewItems.Columns["MeterReading"];
            GridViewItems.ShowEditor();
        }

        private void OnDeleteItemRow(object sender, EventArgs e)
        {
            int idx = CurrentItemIndex();
            if (idx < 0) return;
            GridViewItems.CloseEditor();
            _dtItems.Rows[idx].Delete();
            _dtItems.AcceptChanges();
        }

        private void OnMoveItemUp(object sender, EventArgs e) { MoveItem(-1); }
        private void OnMoveItemDown(object sender, EventArgs e) { MoveItem(+1); }

        private void MoveItem(int delta)
        {
            int idx = CurrentItemIndex();
            if (idx < 0) return;
            int target = idx + delta;
            if (target < 0 || target >= _dtItems.Rows.Count) return;
            GridViewItems.CloseEditor(); GridViewItems.UpdateCurrentRow();
            object[] a = _dtItems.Rows[idx].ItemArray;
            object[] b = _dtItems.Rows[target].ItemArray;
            _dtItems.Rows[idx].ItemArray = b;
            _dtItems.Rows[target].ItemArray = a;
            GridViewItems.FocusedRowHandle = GridViewItems.GetRowHandle(target);
        }

        private void LoadGrid()
        {
            try
            {
                GridMP.DataSource = _dbSetting.GetDataTable(
                    "SELECT MeterMultiPriceCode, [Description] FROM [dbo].[zSCP_MeterMultiPrice] ORDER BY MeterMultiPriceCode", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void PopulateDetail()
        {
            int rh = GridViewMP.FocusedRowHandle;
            if (rh < 0) { ClearDetail(); return; }
            var row = GridViewMP.GetDataRow(rh);
            if (row == null) { ClearDetail(); return; }
            _selectedCode = row["MeterMultiPriceCode"].ToString();
            _isNewRow = false;
            TxtCode.Text = _selectedCode;
            TxtDesc.Text = row["Description"] == DBNull.Value ? "" : row["Description"].ToString();

            try
            {
                var dt = _dbSetting.GetDataTable(
                    "SELECT MeterReading, UnitPrice FROM [dbo].[zSCP_MeterMultiPriceItem] WHERE MeterMultiPriceCode = N'" + SQLString(_selectedCode) + "' ORDER BY MeterReading", false);
                _dtItems.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    var nr = _dtItems.NewRow();
                    nr["MeterReading"] = r["MeterReading"];
                    nr["UnitPrice"] = r["UnitPrice"];
                    _dtItems.Rows.Add(nr);
                }
            }
            catch { }
            SetReadOnly(true);
        }

        private void ClearDetail()
        {
            _selectedCode = "";
            _isNewRow = false;
            TxtCode.Text = "";
            TxtDesc.Text = "";
            _dtItems.Clear();
            SetReadOnly(true);
        }

        private void OnNew(object sender, EventArgs e)
        { ClearDetail(); _isNewRow = true; SetReadOnly(false); TxtCode.Properties.ReadOnly = false; TxtCode.Focus(); }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); ClearDetail(); }

        private void OnEdit(object sender, EventArgs e)
        { if (string.IsNullOrEmpty(_selectedCode)) return; SetReadOnly(false); TxtDesc.Focus(); }

        private void OnSave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text))
            { XtraMessageBox.Show("Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                string c = SQLString(TxtCode.Text.Trim());
                string d = SQLString(TxtDesc.Text ?? "");

                GridViewItems.CloseEditor();
                GridViewItems.UpdateCurrentRow();

                if (_isNewRow || string.IsNullOrEmpty(_selectedCode))
                {
                    _dbSetting.ExecuteNonQuery("INSERT INTO [dbo].[zSCP_MeterMultiPrice] (MeterMultiPriceCode, [Description]) VALUES (N'" + c + "', N'" + d + "')");
                    _selectedCode = TxtCode.Text.Trim();
                    _isNewRow = false;
                }
                else
                {
                    _dbSetting.ExecuteNonQuery("UPDATE [dbo].[zSCP_MeterMultiPrice] SET [Description]=N'" + d + "', LastModified=GETDATE() WHERE MeterMultiPriceCode=N'" + SQLString(_selectedCode) + "'");
                }

                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_MeterMultiPriceItem] WHERE MeterMultiPriceCode = N'" + SQLString(_selectedCode) + "'");
                foreach (DataRow row in _dtItems.Rows)
                {
                    if (row.RowState == DataRowState.Deleted) continue;
                    decimal mr = row["MeterReading"] == DBNull.Value ? 0m : Convert.ToDecimal(row["MeterReading"]);
                    decimal up = row["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(row["UnitPrice"]);
                    _dbSetting.ExecuteNonQuery("INSERT INTO [dbo].[zSCP_MeterMultiPriceItem] (MeterMultiPriceCode, MeterReading, UnitPrice) VALUES " +
                        "(N'" + SQLString(_selectedCode) + "', " + mr.ToString("0.00") + ", " + up.ToString("0.000000") + ")");
                }

                LoadGrid();
                SetReadOnly(true);
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedCode)) return;
            if (XtraMessageBox.Show("Delete pricing scheme '" + _selectedCode + "' and all its tiers?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_MeterMultiPriceItem] WHERE MeterMultiPriceCode = N'" + SQLString(_selectedCode) + "'");
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_MeterMultiPrice] WHERE MeterMultiPriceCode = N'" + SQLString(_selectedCode) + "'");
                ClearDetail();
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { PopulateDetail(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
