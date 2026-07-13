using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    // RETIRED (module v2): replaced by zSCP2_ContractLst_Form. Menu entry removed; form kept for reference.
    // [AutoCount.PlugIn.MenuItem("Maintain Service Contract",
    //     MenuOrder = 200,
    //     OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_CONTRACT,
    //     VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_CONTRACT)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class ServiceContractLst_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceContractLst_Form() { InitializeComponent(); }
        public ServiceContractLst_Form(UserSession userSession) : this()
        { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceContractLst_Form(DBSetting dbSetting) : this()
        { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

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
                    "SELECT ServiceContractKey, ServiceContractCode, ServiceContractTypeCode, ContractTypeDescription, " +
                    "ServiceContractDate, ServiceStartDate, ServiceExpiryDate, DebtorCode, DebtorName, " +
                    "ServiceContractValue, CurrencyCode, AreaCode, BranchCode, StaffCode, Inactive " +
                    "FROM [dbo].[zvSCP_ServiceContractList] ORDER BY ServiceContractCode", false);
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
            using (var f = new ServiceContract_Form(_dbSetting))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnEdit(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null) return;
            using (var f = new ServiceContract_Form(_dbSetting, row))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ServiceContractKey"]);
            string code = row["ServiceContractCode"].ToString();
            if (XtraMessageBox.Show("Delete contract '" + code + "' and all its lines?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_ServiceContractDTL] WHERE ServiceContractKey=" + key);
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_ServiceContractSVI] WHERE ServiceContractKey=" + key);
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_ServiceContract] WHERE ServiceContractKey=" + key);
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
