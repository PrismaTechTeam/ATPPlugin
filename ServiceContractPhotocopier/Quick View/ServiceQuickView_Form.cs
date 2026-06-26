using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.QuickView
{
    /// <summary>
    /// Service - Quick View dashboard — matches UI/08-service-quick-view/01-dashboard.png.
    /// Shows counts for Today/Tomorrow appointments, Open service notes, and the top service stock codes.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service - Quick View",
    // MenuOrder = 100,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_QUICK_VIEW,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_QUICK_VIEW)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceQuickView_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceQuickView_Form() { InitializeComponent(); }
        public ServiceQuickView_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceQuickView_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            RefreshStats();
        }

        private void OnRefresh(object sender, EventArgs e) { RefreshStats(); }

        private void RefreshStats()
        {
            try
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                string tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

                int todayAppts = ToInt(_dbSetting.ExecuteScalar("SELECT COUNT(*) FROM [dbo].[zSCP_Appointment] WHERE CAST(StartTime AS DATE) = '" + today + "'"));
                int tomorrowAppts = ToInt(_dbSetting.ExecuteScalar("SELECT COUNT(*) FROM [dbo].[zSCP_Appointment] WHERE CAST(StartTime AS DATE) = '" + tomorrow + "'"));
                int openNotes = ToInt(_dbSetting.ExecuteScalar("SELECT COUNT(*) FROM [dbo].[zSCP_ServiceNote] WHERE Closed='N'"));
                int overdueNotes = ToInt(_dbSetting.ExecuteScalar("SELECT COUNT(*) FROM [dbo].[zSCP_ServiceNote] WHERE Closed='N' AND AppointmentDate IS NOT NULL AND AppointmentDate < GETDATE()"));

                LblTodayCount.Text = todayAppts.ToString();
                LblTomorrowCount.Text = tomorrowAppts.ToString();
                LblOpenNotesCount.Text = openNotes.ToString();
                LblOverdueCount.Text = overdueNotes.ToString();

                // Top service notes grid (last 5)
                var dtNotes = _dbSetting.GetDataTable("SELECT TOP 10 ServiceNoteCode, ServiceNoteDate, DebtorCode, ServiceStatusCode, [Description] FROM [dbo].[zSCP_ServiceNote] WHERE Closed='N' ORDER BY ServiceNoteDate DESC", false);
                GridNotes.DataSource = dtNotes;

                // Top appointments (today & tomorrow)
                var dtAppts = _dbSetting.GetDataTable("SELECT TOP 10 StartTime, Subject, DebtorCode, ServicePersonCode, AppointmentTypeCode FROM [dbo].[zSCP_Appointment] WHERE CAST(StartTime AS DATE) >= '" + today + "' ORDER BY StartTime", false);
                GridAppts.DataSource = dtAppts;

                // Top service stock codes
                var dtTop = _dbSetting.GetDataTable("SELECT TOP 10 StockCode, COUNT(*) AS [Count] FROM [dbo].[zSCP_ServiceItem] WHERE StockCode <> '' GROUP BY StockCode ORDER BY COUNT(*) DESC", false);
                GridTopStock.DataSource = dtTop;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Refresh failed:\r\n" + ex.Message, "Error");
            }
        }

        private static int ToInt(object v)
        {
            if (v == null || v == DBNull.Value) return 0;
            return Convert.ToInt32(v);
        }

        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
