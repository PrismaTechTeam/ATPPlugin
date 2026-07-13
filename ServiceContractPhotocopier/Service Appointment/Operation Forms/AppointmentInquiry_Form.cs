using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceAppointment.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Service Appointment Inquiry",
    ParentMenuCaption = "Inquiry", MenuOrder = 40, ParentMenuOrder = 700,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_APPOINTMENT_INQUIRY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_APPOINTMENT_INQUIRY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class AppointmentInquiry_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_AppointmentCalendar"; } }
        protected override string ViewName    { get { return "zvSCP_AppointmentCalendar"; } }
        protected override string KeyColumn   { get { return "AppointmentKey"; } }
        protected override string FormCaption { get { return "Service Appointment Inquiry"; } }
        public AppointmentInquiry_Form() { InitializeComponent(); }
        public AppointmentInquiry_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public AppointmentInquiry_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

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
