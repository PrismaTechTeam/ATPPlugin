using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.ServiceItem.MasterForms
{
    // RETIRED (module v2): service items are now managed inside zSCP2_Contract_Form. Menu entry removed.
    // [AutoCount.PlugIn.MenuItem("Maintain Service Item",
    //     MenuOrder = 210,
    //     OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_ITEM,
    //     VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_ITEM)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, false)]
    public partial class ServiceItemLst_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceItemLst_Form() { InitializeComponent(); }
        public ServiceItemLst_Form(UserSession userSession) : this()
        { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceItemLst_Form(DBSetting dbSetting) : this()
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
                    "SELECT ServiceItemKey, ServiceItemCode, StockCode, [Description], ServiceItemGradeCode, GradeDescription, " +
                    "PurchaseDate, UnitPrice, ContractNo, DebtorCode, DebtorName, ServiceStartDate, ServiceExpiryDate " +
                    "FROM [dbo].[zvSCP_ServiceItemList] ORDER BY ServiceItemCode", false);
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
            // If hosted inside the dev launcher's tabbed shell, route the
            // "+ New" action to a new tab via IFormShellHost. Otherwise fall
            // back to a modal ShowDialog (production AutoCount behavior).
            Form host = this.FindForm();
            IFormShellHost shell = host as IFormShellHost;
            if (shell != null)
            {
                shell.OpenFormByTitle("New Service Item");
                return;
            }
            using (ServiceItem_Form f = new ServiceItem_Form(_dbSetting))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnEdit(object sender, EventArgs e)
        {
            DataRow row = GetSelectedRow();
            if (row == null) return;

            // Edit needs the row, so the catalog factory can't construct the
            // form for us — we build it here and ask the shell to embed it.
            Form host = this.FindForm();
            IFormShellHost shell = host as IFormShellHost;
            if (shell != null)
            {
                string code = row["ServiceItemCode"] != null ? row["ServiceItemCode"].ToString() : "Service Item";
                ServiceItem_Form f = new ServiceItem_Form(_dbSetting, row);
                shell.OpenFormInTab("Edit Service Item: " + code, f);
                return;
            }
            using (ServiceItem_Form f = new ServiceItem_Form(_dbSetting, row))
            { f.ShowDialog(this); LoadGrid(); }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ServiceItemKey"]);
            string code = row["ServiceItemCode"].ToString();
            if (XtraMessageBox.Show("Delete service item '" + code + "' and all its meter types?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_ServiceItemMeterType] WHERE ServiceItemKey=" + key);
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP_ServiceItem] WHERE ServiceItemKey=" + key);
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
