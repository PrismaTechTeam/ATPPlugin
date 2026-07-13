using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// List form for the combined Service Contract module v2 (zSCP2_*). Single entry point that
    /// replaces the old separate "Maintain Service Contract" + "Maintain Service Item" menus.
    /// </summary>
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    [AutoCount.PlugIn.MenuItem("Maintain Service Contract", MenuOrder = 200, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class zSCP2_ContractLst_Form : XtraForm
    {
        protected DBSetting _dbSetting;   // protected: shared with the "Maintain Service Item" alias subclass

        public zSCP2_ContractLst_Form() { InitializeComponent(); ApplyAutoCountToolbarImages(); }

        // Native AutoCount toolbar look: the official large icons (same ones Stock Item / Address
        // Maintenance use), image left + caption right, default boxed button style.
        private void ApplyAutoCountToolbarImages()
        {
            try
            {
                // GetAutoCountImage expects DPI-based dimensions (96/120/144...). Native AutoCount
                // forms use AutoScaleMode=Dpi so their AutoScaleDimensions ARE the DPI; ours is
                // Font-based (7,14) which would shrink the icons to ~1% — pass the real DPI instead.
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                this.BtnNew.ImageOptions.Image = img.GetLargeImage_New();
                this.BtnEdit.ImageOptions.Image = img.GetLargeImage_Edit();
                this.BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete2();
                this.BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                this.BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }   // icons are cosmetic — never block the form over an image lookup
        }

        public zSCP2_ContractLst_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public zSCP2_ContractLst_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            LoadGrid();
            GridView.DoubleClick += delegate { OnEdit(null, null); };
        }

        // Virtual so the "Maintain Service Item" alias can present the SAME data at service-item
        // level (one row per CSSI) while every CRUD handler below keeps working off row["ContractKey"].
        protected virtual void LoadGrid()
        {
            try
            {
                Grid.DataSource = _dbSetting.GetDataTable(
                    "SELECT ContractKey, ContractNo, DebtorCode, DebtorName, ContractDate, " +
                    "ServiceStartDate, ServiceExpiryDate, ContractValue, BillingDay, BillingMode, " +
                    "ItemCount, Inactive FROM [dbo].[zvSCP2_ContractList] ORDER BY ContractNo", false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private DataRow GetSelectedRow()
        {
            int rh = GridView.FocusedRowHandle;
            return rh < 0 ? null : GridView.GetDataRow(rh);
        }

        // Virtual so the "Maintain Service Item" alias can open the Service Item editor instead.
        protected virtual void OnNew(object sender, EventArgs e)
        {
            using (zSCP2_Contract_Form f = new zSCP2_Contract_Form(_dbSetting))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnEdit(object sender, EventArgs e)
        {
            DataRow row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ContractKey"]);
            using (zSCP2_Contract_Form f = new zSCP2_Contract_Form(_dbSetting, key))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            DataRow row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ContractKey"]);
            string code = row["ContractNo"].ToString();
            if (XtraMessageBox.Show("Delete contract '" + code + "' and all its machines / meter config?",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                // FK cascade removes zSCP2_Item -> zSCP2_ItemMeter automatically.
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + key);
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
