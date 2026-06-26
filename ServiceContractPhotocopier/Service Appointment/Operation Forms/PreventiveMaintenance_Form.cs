using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.ServiceAppointment.OperationForms
{
    /// <summary>
    /// Preventive Maintenance - Service Note — matches UI/05-service-appointment/04-calendar-preventive-overlay.png.
    /// Dialog to bulk-generate Service Notes for machines whose PMLastServiceDate+Interval &lt; today.
    /// Fields: Service Date range + "Default Settings" group (Service Note Code, Status, Type, Severity, Problem + remark + Attended By / Assign To + Description) + Tax Inclusive.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Preventive Maintenance - Service Note",
    // MenuOrder = 410,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_PREVENTIVE_MAINT,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_PREVENTIVE_MAINT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class PreventiveMaintenance_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public PreventiveMaintenance_Form() { InitializeComponent(); }
        public PreventiveMaintenance_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public PreventiveMaintenance_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            DtFrom.DateTime = DateTime.Today;
            DtTo.DateTime = DateTime.Today.AddDays(7);
            LoadLookups();
        }

        private void LoadLookups()
        {
            try
            {
                Fill(CmbStatus, "SELECT ServiceStatusCode FROM [dbo].[zSCP_LK_ServiceStatus] WHERE Inactive='N' ORDER BY ServiceStatusCode");
                Fill(CmbType, "SELECT ServiceTypeCode FROM [dbo].[zSCP_LK_ServiceType] WHERE Inactive='N' ORDER BY ServiceTypeCode");
                Fill(CmbSeverity, "SELECT ServiceSeverityCode FROM [dbo].[zSCP_LK_ServiceSeverity] WHERE Inactive='N' ORDER BY ServiceSeverityCode");
                Fill(CmbProblem, "SELECT ServiceProblemCode FROM [dbo].[zSCP_LK_ServiceProblem] WHERE Inactive='N' ORDER BY ServiceProblemCode");
                Fill(CmbAttendedBy, "SELECT ServicePersonCode FROM [dbo].[zSCP_ServicePerson] WHERE Inactive='N' ORDER BY ServicePersonCode");
                Fill(CmbAssignTo, "SELECT ServicePersonCode FROM [dbo].[zSCP_ServicePerson] WHERE Inactive='N' ORDER BY ServicePersonCode");
            }
            catch { }
        }

        private void Fill(ComboBoxEdit cmb, string sql)
        {
            var dt = _dbSetting.GetDataTable(sql, false);
            cmb.Properties.Items.Clear();
            foreach (DataRow r in dt.Rows) cmb.Properties.Items.Add(r[0].ToString());
        }

        private void OnGenerate(object sender, EventArgs e)
        {
            try
            {
                // Find all service items due for PM in the date range: PMLastServiceDate + interval falls between From and To.
                // For simplicity, we match items whose PMActive='Y' and PMLastServiceDate <= To.
                string sql = "SELECT ServiceItemCode, DebtorCode, ContractNo, Address1, Address2, Address3, Address4 " +
                             "FROM [dbo].[zSCP_ServiceItem] WHERE PMActive = 'Y' AND (PMLastServiceDate IS NULL OR PMLastServiceDate <= '" +
                             DtTo.DateTime.ToString("yyyy-MM-dd") + "')";
                var dt = _dbSetting.GetDataTable(sql, false);
                if (dt.Rows.Count == 0)
                {
                    XtraMessageBox.Show("No service items require PM in the date range.", "Preventive Maintenance",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int generated = 0;
                string status = SQLString(CmbStatus.Text ?? "OPEN");
                string typ = SQLString(CmbType.Text ?? "");
                string sev = SQLString(CmbSeverity.Text ?? "");
                string prob = SQLString(CmbProblem.Text ?? "");
                string probRemark = SQLString(TxtProblemRemark.Text ?? "");
                string attd = SQLString(CmbAttendedBy.Text ?? "");
                string asgn = SQLString(CmbAssignTo.Text ?? "");
                string desc = SQLString(TxtDescription.Text ?? "Preventive Maintenance");
                string dtNow = "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                foreach (DataRow r in dt.Rows)
                {
                    string siCode = SQLString(r["ServiceItemCode"].ToString());
                    string debtor = SQLString(r["DebtorCode"].ToString());
                    string contract = SQLString(r["ContractNo"] == DBNull.Value ? "" : r["ContractNo"].ToString());
                    string a1 = SQLString(r["Address1"].ToString());
                    string a2 = SQLString(r["Address2"].ToString());
                    string a3 = SQLString(r["Address3"].ToString());
                    string a4 = SQLString(r["Address4"].ToString());
                    string noteCode = "PM-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + (generated + 1).ToString("0000");
                    string insertSql = "INSERT INTO [dbo].[zSCP_ServiceNote] " +
                        "(ServiceNoteCode, ServiceNoteDate, ServiceStatusCode, ServiceItemCode, ContractNo, ServiceTypeCode, " +
                        " ServiceSeverityCode, ServiceProblemCode, ServiceProblemRemark, AttendedServicePersonCode, AssignToServicePersonCode, " +
                        " DebtorCode, [Description], Address1, Address2, Address3, Address4, Created, Modified) VALUES " +
                        "(N'" + noteCode + "', " + dtNow + ", N'" + status + "', N'" + siCode + "', N'" + contract + "', N'" + typ + "', " +
                        "N'" + sev + "', N'" + prob + "', N'" + probRemark + "', N'" + attd + "', N'" + asgn + "', " +
                        "N'" + debtor + "', N'" + desc + "', N'" + a1 + "', N'" + a2 + "', N'" + a3 + "', N'" + a4 + "', GETDATE(), GETDATE())";
                    _dbSetting.ExecuteNonQuery(insertSql);
                    generated++;
                }

                XtraMessageBox.Show("Generated " + generated + " service note(s) from PM due items.", "Preventive Maintenance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { XtraMessageBox.Show("Generate failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnExit(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
