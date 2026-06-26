using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceAppointment.OperationForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Appointment",
    // MenuOrder = 400,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_APPOINTMENT,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_APPOINTMENT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class AppointmentLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_AppointmentCalendar"; } }
        protected override string ViewName    { get { return "zvSCP_AppointmentCalendar"; } }
        protected override string KeyColumn   { get { return "AppointmentKey"; } }
        protected override string FormCaption { get { return "Service Appointment List"; } }
        public AppointmentLst_Form() { InitializeComponent(); }
        public AppointmentLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public AppointmentLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new Appointment_Form(_dbSetting))
            {
                f.ShowDialog(this);
                LoadData();
            }
        }
    }
}
