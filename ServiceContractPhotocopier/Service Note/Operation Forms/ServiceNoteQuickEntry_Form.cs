using System;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.ServiceNote.OperationForms
{
    /// <summary>
    /// Service Note - Quick Entry — minimal one-page form for fast front-desk intake.
    /// Fields: Date, Debtor, Service Item, Problem, Severity, Description, Assign To. Save + New workflow.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Note - Quick Entry",
    // MenuOrder = 150,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_NOTE_QUICK_ENTRY,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_NOTE_QUICK_ENTRY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteQuickEntry_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceNoteQuickEntry_Form() { InitializeComponent(); }
        public ServiceNoteQuickEntry_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceNoteQuickEntry_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            DtNoteDate.DateTime = DateTime.Now;
            if (_dbSetting == null) return;
            try
            {
                var dtSt = _dbSetting.GetDataTable("SELECT ServiceStatusCode FROM [dbo].[zSCP_LK_ServiceStatus] WHERE Inactive='N' ORDER BY ServiceStatusCode", false);
                var dtSv = _dbSetting.GetDataTable("SELECT ServiceSeverityCode FROM [dbo].[zSCP_LK_ServiceSeverity] WHERE Inactive='N' ORDER BY ServiceSeverityCode", false);
                var dtPr = _dbSetting.GetDataTable("SELECT ServiceProblemCode FROM [dbo].[zSCP_LK_ServiceProblem] WHERE Inactive='N' ORDER BY ServiceProblemCode", false);
                var dtPer = _dbSetting.GetDataTable("SELECT ServicePersonCode FROM [dbo].[zSCP_ServicePerson] WHERE Inactive='N' ORDER BY ServicePersonCode", false);
                CmbStatus.Properties.Items.Clear(); foreach (System.Data.DataRow r in dtSt.Rows) CmbStatus.Properties.Items.Add(r[0].ToString());
                CmbSeverity.Properties.Items.Clear(); foreach (System.Data.DataRow r in dtSv.Rows) CmbSeverity.Properties.Items.Add(r[0].ToString());
                CmbProblem.Properties.Items.Clear(); foreach (System.Data.DataRow r in dtPr.Rows) CmbProblem.Properties.Items.Add(r[0].ToString());
                CmbAssignTo.Properties.Items.Clear(); foreach (System.Data.DataRow r in dtPer.Rows) CmbAssignTo.Properties.Items.Add(r[0].ToString());
                if (CmbStatus.Properties.Items.Count > 0) CmbStatus.SelectedIndex = 0;
            }
            catch { }
        }

        private void OnSaveAndNew(object sender, EventArgs e)
        {
            SaveRecord();
            TxtNoteCode.Text = "";
            TxtServiceItemCode.Text = "";
            TxtDescription.Text = "";
            TxtNoteCode.Focus();
        }

        private void OnSaveAndClose(object sender, EventArgs e)
        {
            SaveRecord();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SaveRecord()
        {
            if (string.IsNullOrWhiteSpace(TxtNoteCode.Text))
            { XtraMessageBox.Show("Service Note No is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(TxtDebtorCode.Text))
            { XtraMessageBox.Show("Debtor Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                string sql = "INSERT INTO [dbo].[zSCP_ServiceNote] " +
                    "(ServiceNoteCode, ServiceNoteDate, ServiceStatusCode, ServiceItemCode, ServiceSeverityCode, ServiceProblemCode, " +
                    " AssignToServicePersonCode, DebtorCode, [Description], Created, Modified) VALUES " +
                    "(N'" + SQLString(TxtNoteCode.Text.Trim()) + "', '" + DtNoteDate.DateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                    "N'" + SQLString(CmbStatus.Text ?? "OPEN") + "', N'" + SQLString(TxtServiceItemCode.Text ?? "") + "', " +
                    "N'" + SQLString(CmbSeverity.Text ?? "") + "', N'" + SQLString(CmbProblem.Text ?? "") + "', " +
                    "N'" + SQLString(CmbAssignTo.Text ?? "") + "', N'" + SQLString(TxtDebtorCode.Text.Trim()) + "', " +
                    "N'" + SQLString(TxtDescription.Text ?? "") + "', GETDATE(), GETDATE())";
                _dbSetting.ExecuteNonQuery(sql);
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
