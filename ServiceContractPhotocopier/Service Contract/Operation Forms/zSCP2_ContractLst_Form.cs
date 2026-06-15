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
    [AutoCount.PlugIn.MenuItem("Maintain Service Contract", MenuOrder = 200)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, false)]
    public partial class zSCP2_ContractLst_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public zSCP2_ContractLst_Form() { InitializeComponent(); }

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

        private void LoadGrid()
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

        private void OnNew(object sender, EventArgs e)
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
