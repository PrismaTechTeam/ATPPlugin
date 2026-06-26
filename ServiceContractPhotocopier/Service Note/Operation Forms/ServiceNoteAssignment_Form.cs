using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.ServiceNote.OperationForms
{
    /// <summary>
    /// Service Note Assignment — shows all open unassigned/assigned service notes
    /// with bulk-assign to a selected service person.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Note Assignment",
    // MenuOrder = 310,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_NOTE_ASSIGNMENT,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_NOTE_ASSIGNMENT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteAssignment_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dt;

        public ServiceNoteAssignment_Form() { InitializeComponent(); }
        public ServiceNoteAssignment_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceNoteAssignment_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            try
            {
                var dtPer = _dbSetting.GetDataTable("SELECT ServicePersonCode FROM [dbo].[zSCP_ServicePerson] WHERE Inactive='N' ORDER BY ServicePersonCode", false);
                CmbServicePerson.Properties.Items.Clear();
                foreach (DataRow r in dtPer.Rows) CmbServicePerson.Properties.Items.Add(r[0].ToString());
                LoadNotes();
            }
            catch { }
        }

        private void LoadNotes()
        {
            _dt = _dbSetting.GetDataTable(
                "SELECT ServiceNoteKey, ServiceNoteCode, ServiceNoteDate, DebtorCode, DebtorName, " +
                "ServiceItemCode, AssignToServicePersonCode, ServiceStatusCode " +
                "FROM [dbo].[zSCP_ServiceNote] WHERE Closed='N' ORDER BY ServiceNoteDate DESC", false);
            GridNotes.DataSource = _dt;
        }

        private void OnAssign(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CmbServicePerson.Text))
            { XtraMessageBox.Show("Select a service person first.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                int[] sel = GridViewNotes.GetSelectedRows();
                if (sel == null || sel.Length == 0)
                { XtraMessageBox.Show("Select one or more rows.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                string per = SQLString(CmbServicePerson.Text);
                int updated = 0;
                foreach (int idx in sel)
                {
                    var row = GridViewNotes.GetDataRow(idx);
                    if (row == null) continue;
                    long key = Convert.ToInt64(row["ServiceNoteKey"]);
                    _dbSetting.ExecuteNonQuery("UPDATE [dbo].[zSCP_ServiceNote] SET AssignToServicePersonCode=N'" + per + "', Modified=GETDATE() WHERE ServiceNoteKey=" + key);
                    updated++;
                }
                XtraMessageBox.Show("Assigned " + updated + " note(s) to " + CmbServicePerson.Text, "Assignment",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadNotes();
            }
            catch (Exception ex) { XtraMessageBox.Show("Assign failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnClose(object sender, EventArgs e) { this.Close(); }
    }
}
