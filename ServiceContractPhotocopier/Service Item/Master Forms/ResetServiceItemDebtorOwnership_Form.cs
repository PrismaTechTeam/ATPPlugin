using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.ServiceItem.MasterForms
{
    /// <summary>
    /// Reset Service Item Debtor Ownership — bulk-transfer machines from one debtor to another.
    /// Writes history row to zSCP_ServiceItemDebtorHistory before updating zSCP_ServiceItem.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Reset Service Item Debtor Ownership",
    // MenuOrder = 420,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_RESET_ITEM_DEBTOR,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_RESET_ITEM_DEBTOR)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ResetServiceItemDebtorOwnership_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dt;

        public ResetServiceItemDebtorOwnership_Form() { InitializeComponent(); }
        public ResetServiceItemDebtorOwnership_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ResetServiceItemDebtorOwnership_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e) { }

        private void OnLoadItems(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFromDebtor.Text))
            { XtraMessageBox.Show("From Debtor is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                _dt = _dbSetting.GetDataTable("SELECT ServiceItemKey, ServiceItemCode, StockCode, [Description] FROM [dbo].[zSCP_ServiceItem] WHERE DebtorCode = N'" + SQLString(TxtFromDebtor.Text.Trim()) + "' ORDER BY ServiceItemCode", false);
                GridItems.DataSource = _dt;
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnReset(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtToDebtor.Text))
            { XtraMessageBox.Show("To Debtor is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                int[] sel = GridViewItems.GetSelectedRows();
                if (sel == null || sel.Length == 0)
                { XtraMessageBox.Show("Select one or more items.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                string toDebtor = SQLString(TxtToDebtor.Text.Trim());
                string fromDebtor = SQLString(TxtFromDebtor.Text.Trim());
                int updated = 0;
                foreach (int idx in sel)
                {
                    var row = GridViewItems.GetDataRow(idx);
                    if (row == null) continue;
                    long siKey = Convert.ToInt64(row["ServiceItemKey"]);
                    string siCode = SQLString(row["ServiceItemCode"].ToString());
                    _dbSetting.ExecuteNonQuery("INSERT INTO [dbo].[zSCP_ServiceItemDebtorHistory] (ServiceItemKey, ServiceItemCode, DebtorCode, StartDate, EndDate, Remark) VALUES (" +
                        siKey + ", N'" + siCode + "', N'" + fromDebtor + "', NULL, '" + DateTime.Today.ToString("yyyy-MM-dd") + "', N'Reset Debtor Ownership')");
                    _dbSetting.ExecuteNonQuery("UPDATE [dbo].[zSCP_ServiceItem] SET DebtorCode=N'" + toDebtor + "', Modified=GETDATE(), LastModified=GETDATE() WHERE ServiceItemKey=" + siKey);
                    updated++;
                }
                XtraMessageBox.Show("Transferred " + updated + " item(s) from " + TxtFromDebtor.Text + " to " + TxtToDebtor.Text, "Reset Ownership",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnLoadItems(sender, e);
            }
            catch (Exception ex) { XtraMessageBox.Show("Reset failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnClose(object sender, EventArgs e) { this.Close(); }
    }
}
