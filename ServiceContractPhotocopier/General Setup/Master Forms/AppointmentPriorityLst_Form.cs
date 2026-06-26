using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Appointment Priority",
    // ParentMenuCaption = "General Setup",
    // MenuOrder = 90,
    // ParentMenuOrder = 600,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_APPOINTMENT_PRIORITY,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_APPOINTMENT_PRIORITY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class AppointmentPriorityLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_AppointmentPriority"; } }
        protected override string KeyColumn   { get { return "AppointmentPriorityCode"; } }
        protected override string KeyField    { get { return "AppointmentPriorityKey"; } }
        protected override string FormCaption { get { return "Appointment Priority"; } }
        protected override string StatusText  { get { return "Appointment priority"; } }

        public AppointmentPriorityLst_Form() { InitializeComponent(); }
        public AppointmentPriorityLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public AppointmentPriorityLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
