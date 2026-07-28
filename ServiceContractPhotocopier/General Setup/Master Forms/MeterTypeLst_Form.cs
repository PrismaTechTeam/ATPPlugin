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
        { if (_dbSetting == null) return; ApplyButtonIcons(); LoadGrid(); GridViewMT.FocusedRowChanged += delegate { PopulateDetail(); }; SetReadOnly(true); SetEditMode(false); }

        // Same AutoCount large icons as the Maintain Service Contract / Item toolbars.
        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                BtnNew.ImageOptions.Image = img.GetLargeImage_New();
                BtnEdit.ImageOptions.Image = img.GetLargeImage_Edit();
                BtnCopyNew.ImageOptions.Image = img.GetLargeImage_CopyTo2();
                BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                BtnCancel.ImageOptions.Image = img.GetLargeImage_Cancel();
                BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete2();
                BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                // Exit uses a DOOR icon (XAF exit-action) so it is not confused with the red-X
                // Cancel / Delete buttons.
                try
                {
                    System.Drawing.Image door = DevExpress.Images.ImageResourceCache.Default.GetImage("images/xaf/action_exit_32x32.png");
                    BtnExit.ImageOptions.Image = door ?? img.GetLargeImage_Close();
                }
                catch { BtnExit.ImageOptions.Image = img.GetLargeImage_Close(); }
            }
            catch { }   // icons are cosmetic — never block the form
        }

        // Edit mode = a New / Edit / Copy-to-New is in progress: only Save + Cancel (and Exit) are
        // usable, the grid is locked, and the list actions are disabled until Save or Cancel.
        private void SetEditMode(bool editing)
        {
            BtnNew.Enabled = !editing;
            BtnEdit.Enabled = !editing;
            BtnCopyNew.Enabled = !editing;
            BtnDelete.Enabled = !editing;
            BtnRefresh.Enabled = !editing;
            BtnSave.Enabled = editing;
            BtnCancel.Enabled = editing;
            GridMT.Enabled = !editing;   // lock row navigation while editing
        }

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
            ChkFlatCharge.Properties.ReadOnly = ro;
            ChkRentalWaive.Properties.ReadOnly = ro;
            CmbDefaultRole.Properties.ReadOnly = ro;
        }

        private void LoadGrid()
        {
            try
            {
                GridMT.DataSource = _dbSetting.GetDataTable(
                    "SELECT MeterTypeKey, MeterTypeCode, [Description], StockCode, MeterMultiPriceCode, " +
                    "ChargesRate, MinimumCharges, RebateQtyInPercent, FOCQty, ISNULL(IsFlatCharge,'N') AS IsFlatCharge, ISNULL(IsRentalWaive,'N') AS IsRentalWaive, ISNULL(DefaultRole,'') AS DefaultRole, Inactive " +
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
            ChkFlatCharge.Checked = V(row, "IsFlatCharge") == "Y";
            ChkRentalWaive.Checked = V(row, "IsRentalWaive") == "Y";
            CmbDefaultRole.Text = V(row, "DefaultRole");
            SetReadOnly(true);
            SetEditMode(false);
        }

        private void ClearDetail()
        {
            _selectedKey = 0; _isNewRow = false;
            TxtCode.Text = "";
            TxtDesc.Text = ""; TxtStockCode.Text = ""; TxtMultiPriceCode.Text = "";
            TxtMinCharges.Text = "0.00"; TxtChargesRate.Text = "0.000000";
            TxtRebateQty.Text = "0.00"; TxtFOCQty.Text = "0.00";
            ChkInactive.Checked = false;
            ChkFlatCharge.Checked = false;
            ChkRentalWaive.Checked = false;
            CmbDefaultRole.Text = "";
            SetReadOnly(true);
        }

        private void OnNew(object sender, EventArgs e)
        { ClearDetail(); _isNewRow = true; SetReadOnly(false); TxtCode.Properties.ReadOnly = false; SetEditMode(true); TxtCode.Focus(); }

        // Copy to New: duplicate the selected meter type into a fresh entry — EVERY field is copied
        // EXCEPT the Meter Type Code, which is left blank for the user to key a new one.
        private void OnCopyToNew(object sender, EventArgs e)
        {
            int rh = GridViewMT.FocusedRowHandle;
            if (rh < 0) return;
            DataRow row = GridViewMT.GetDataRow(rh);
            if (row == null) return;
            _selectedKey = 0; _isNewRow = true;
            TxtCode.Text = "";                                  // the only field NOT copied
            TxtDesc.Text = V(row, "Description");
            TxtStockCode.Text = V(row, "StockCode");
            TxtMultiPriceCode.Text = V(row, "MeterMultiPriceCode");
            TxtMinCharges.Text = D(row, "MinimumCharges", "0.00");
            TxtChargesRate.Text = D(row, "ChargesRate", "0.000000");
            TxtRebateQty.Text = D(row, "RebateQtyInPercent", "0.00");
            TxtFOCQty.Text = D(row, "FOCQty", "0.00");
            ChkInactive.Checked = V(row, "Inactive") == "Y";
            ChkFlatCharge.Checked = V(row, "IsFlatCharge") == "Y";
            ChkRentalWaive.Checked = V(row, "IsRentalWaive") == "Y";
            CmbDefaultRole.Text = V(row, "DefaultRole");
            SetReadOnly(false);
            TxtCode.Properties.ReadOnly = false;
            SetEditMode(true);
            TxtCode.Focus();
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); ClearDetail(); SetEditMode(false); }

        private void OnEdit(object sender, EventArgs e)
        { if (_selectedKey == 0) return; SetReadOnly(false); SetEditMode(true); TxtDesc.Focus(); }

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
                string fc = ChkFlatCharge.Checked ? "Y" : "N";
                string rw = ChkRentalWaive.Checked ? "Y" : "N";
                string drl = SQLString((CmbDefaultRole.Text ?? "").Trim().ToUpperInvariant());
                if (_isNewRow || _selectedKey == 0)
                    _dbSetting.ExecuteNonQuery("INSERT INTO [dbo].[zSCP_MeterType] (MeterTypeCode,[Description],StockCode,MeterMultiPriceCode,MinimumCharges,ChargesRate,RebateQtyInPercent,FOCQty,IsFlatCharge,IsRentalWaive,DefaultRole,Inactive) VALUES " +
                        "(N'" + c + "',N'" + d + "',N'" + s + "',N'" + m + "'," + mc.ToString("0.00") + "," + cr.ToString("0.000000") + "," + rq.ToString("0.00") + "," + fq.ToString("0.00") + ",'" + fc + "','" + rw + "',N'" + drl + "','" + ia + "')");
                else
                    _dbSetting.ExecuteNonQuery("UPDATE [dbo].[zSCP_MeterType] SET [Description]=N'" + d + "',StockCode=N'" + s + "',MeterMultiPriceCode=N'" + m + "'," +
                        "MinimumCharges=" + mc.ToString("0.00") + ",ChargesRate=" + cr.ToString("0.000000") + ",RebateQtyInPercent=" + rq.ToString("0.00") + ",FOCQty=" + fq.ToString("0.00") + ",IsFlatCharge='" + fc + "',IsRentalWaive='" + rw + "',DefaultRole=N'" + drl + "',Inactive='" + ia + "',LastModified=GETDATE() WHERE MeterTypeKey=" + _selectedKey);
                _isNewRow = false;
                LoadGrid();
                SetReadOnly(true);
                SetEditMode(false);
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

        private void OnCancel(object sender, EventArgs e) { PopulateDetail(); SetEditMode(false); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private static string V(DataRow r, string c) { return r[c] == DBNull.Value ? "" : r[c].ToString(); }
        private static string D(DataRow r, string c, string fmt) { return r[c] == DBNull.Value ? fmt : Convert.ToDecimal(r[c]).ToString(fmt); }
    }
}
