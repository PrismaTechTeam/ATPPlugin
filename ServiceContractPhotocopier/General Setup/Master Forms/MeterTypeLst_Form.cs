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
    [AutoCount.PlugIn.MenuItem("Meter Type",
    ParentMenuCaption = "General Setup", MenuOrder = 130, ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_METER_TYPE,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_METER_TYPE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MeterTypeLst_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private long _selectedKey = 0;
        private bool _isNewRow = false;

        public MeterTypeLst_Form() { InitializeComponent(); }
        public MeterTypeLst_Form(UserSession userSession) : this()
        { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public MeterTypeLst_Form(DBSetting dbSetting) : this()
        { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        { if (_dbSetting == null) return; LoadGrid(); GridViewMT.FocusedRowChanged += delegate { PopulateDetail(); }; SetReadOnly(true); }

        private void SetReadOnly(bool ro)
        {
            TxtCode.Properties.ReadOnly = true;
            TxtDesc.Properties.ReadOnly = ro;
            TxtStockCode.Properties.ReadOnly = ro;
            TxtMultiPriceCode.Properties.ReadOnly = ro;
            TxtMinCharges.Properties.ReadOnly = ro;
            TxtChargesRate.Properties.ReadOnly = ro;
            TxtRebateQty.Properties.ReadOnly = ro;
            TxtFOCQty.Properties.ReadOnly = ro;
            ChkInactive.Properties.ReadOnly = ro;
        }

        private void LoadGrid()
        {
            try
            {
                GridMT.DataSource = _dbSetting.GetDataTable(
                    "SELECT MeterTypeKey, MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, " +
                    "ChargesRate, MinimumCharges, RebateQtyInPercent, FOCQty, Inactive " +
                    "FROM [dbo].[zSCP_MeterType] ORDER BY MeterTypeCode", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void PopulateDetail()
        {
            int rh = GridViewMT.FocusedRowHandle;
            if (rh < 0) { ClearDetail(); return; }
            var row = GridViewMT.GetDataRow(rh);
            if (row == null) { ClearDetail(); return; }
            _selectedKey = Convert.ToInt64(row["MeterTypeKey"]);
            _isNewRow = false;
            TxtCode.Text = row["MeterTypeCode"].ToString();
            TxtDesc.Text = V(row, "Description");
            TxtStockCode.Text = V(row, "StockCode");
            TxtMultiPriceCode.Text = V(row, "MeterMultiPriceCode");
            TxtMinCharges.Text = D(row, "MinimumCharges", "0.00");
            TxtChargesRate.Text = D(row, "ChargesRate", "0.000000");
            TxtRebateQty.Text = D(row, "RebateQtyInPercent", "0.00");
            TxtFOCQty.Text = D(row, "FOCQty", "0.00");
            ChkInactive.Checked = V(row, "Inactive") == "Y";
            SetReadOnly(true);
        }

        private void ClearDetail()
        {
            _selectedKey = 0; _isNewRow = false;
            TxtCode.Text = "";
            TxtDesc.Text = ""; TxtStockCode.Text = ""; TxtMultiPriceCode.Text = "";
            TxtMinCharges.Text = "0.00"; TxtChargesRate.Text = "0.000000";
            TxtRebateQty.Text = "0.00"; TxtFOCQty.Text = "0.00";
            ChkInactive.Checked = false;
            SetReadOnly(true);
        }

        private void OnNew(object sender, EventArgs e)
        { ClearDetail(); _isNewRow = true; SetReadOnly(false); TxtCode.Properties.ReadOnly = false; TxtCode.Focus(); }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); ClearDetail(); }

        private void OnEdit(object sender, EventArgs e)
        { if (_selectedKey == 0) return; SetReadOnly(false); TxtDesc.Focus(); }

        private void OnSave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text))
            { XtraMessageBox.Show("Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                string c = SQLString(TxtCode.Text.Trim()), d = SQLString(TxtDesc.Text ?? "");
                string s = SQLString(TxtStockCode.Text ?? ""), m = SQLString(TxtMultiPriceCode.Text ?? "");
                decimal mc = 0m, cr = 0m, rq = 0m, fq = 0m;
                decimal.TryParse(TxtMinCharges.Text, out mc); decimal.TryParse(TxtChargesRate.Text, out cr);
                decimal.TryParse(TxtRebateQty.Text, out rq); decimal.TryParse(TxtFOCQty.Text, out fq);
                string ia = ChkInactive.Checked ? "Y" : "N";
                if (_isNewRow || _selectedKey == 0)
                    _dbSetting.ExecuteNonQuery("INSERT INTO [dbo].[zSCP_MeterType] (MeterTypeCode,[Description],StockCode,MeterMultiPriceCode,MinimumCharges,ChargesRate,RebateQtyInPercent,FOCQty,Inactive) VALUES " +
                        "(N'" + c + "',N'" + d + "',N'" + s + "',N'" + m + "'," + mc.ToString("0.00") + "," + cr.ToString("0.000000") + "," + rq.ToString("0.00") + "," + fq.ToString("0.00") + ",'" + ia + "')");
                else
                    _dbSetting.ExecuteNonQuery("UPDATE [dbo].[zSCP_MeterType] SET [Description]=N'" + d + "',StockCode=N'" + s + "',MeterMultiPriceCode=N'" + m + "'," +
                        "MinimumCharges=" + mc.ToString("0.00") + ",ChargesRate=" + cr.ToString("0.000000") + ",RebateQtyInPercent=" + rq.ToString("0.00") + ",FOCQty=" + fq.ToString("0.00") + ",Inactive='" + ia + "',LastModified=GETDATE() WHERE MeterTypeKey=" + _selectedKey);
                _isNewRow = false;
                LoadGrid();
                SetReadOnly(true);
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selectedKey == 0) return;
            if (XtraMessageBox.Show("Delete?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_MeterType] WHERE MeterTypeKey=" + _selectedKey); ClearDetail(); LoadGrid(); }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { PopulateDetail(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private static string V(DataRow r, string c) { return r[c] == DBNull.Value ? "" : r[c].ToString(); }
        private static string D(DataRow r, string c, string fmt) { return r[c] == DBNull.Value ? fmt : Convert.ToDecimal(r[c]).ToString(fmt); }
    }
}
