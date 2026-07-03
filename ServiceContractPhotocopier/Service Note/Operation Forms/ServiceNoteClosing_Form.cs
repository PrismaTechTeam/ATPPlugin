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
    /// <summary>Service Note Closing dialog — pick note no + solution code + closed date → mark Closed='Y'.</summary>
    [AutoCount.PlugIn.MenuItem("Service Note Closing",
    MenuOrder = 320,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_NOTE_CLOSING,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_NOTE_CLOSING)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteClosing_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceNoteClosing_Form() { InitializeComponent(); }
        public ServiceNoteClosing_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceNoteClosing_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            DtClosedDate.DateTime = DateTime.Today;
            DtActualServiceDate.DateTime = DateTime.Today;
            if (_dbSetting == null) return;
            try
            {
                var dtSol = _dbSetting.GetDataTable("SELECT ServiceSolutionCode FROM [dbo].[zSCP_LK_ServiceSolution] WHERE Inactive='N' ORDER BY ServiceSolutionCode", false);
                CmbSolution.Properties.Items.Clear();
                foreach (DataRow r in dtSol.Rows) CmbSolution.Properties.Items.Add(r[0].ToString());
            }
            catch { }
        }

        private void OnClose(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNoteCode.Text))
            { XtraMessageBox.Show("Service Note No is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                string sql = "UPDATE [dbo].[zSCP_ServiceNote] SET " +
                    "Closed='Y', ClosedDate='" + DtClosedDate.DateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                    "ActualServiceDate='" + DtActualServiceDate.DateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                    "ServiceSolutionCode=N'" + SQLString(CmbSolution.Text ?? "") + "', " +
                    "ServiceSolutionRemark=N'" + SQLString(TxtSolutionRemark.Text ?? "") + "', " +
                    "ServiceStatusCode='CLOSED', Modified=GETDATE(), LastModified=GETDATE() " +
                    "WHERE ServiceNoteCode = N'" + SQLString(TxtNoteCode.Text.Trim()) + "'";
                _dbSetting.ExecuteNonQuery(sql);
                XtraMessageBox.Show("Service note closed.", "Closing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { XtraMessageBox.Show("Close failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
