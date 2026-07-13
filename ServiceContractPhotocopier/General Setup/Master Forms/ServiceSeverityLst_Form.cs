using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Severity",
    ParentMenuCaption = "General Setup",
    MenuOrder = 20,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_SEVERITY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_SEVERITY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceSeverityLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceSeverity"; } }
        protected override string KeyColumn   { get { return "ServiceSeverityCode"; } }
        protected override string KeyField    { get { return "ServiceSeverityKey"; } }
        protected override string FormCaption { get { return "Service Severity"; } }
        protected override string StatusText  { get { return "Service severity"; } }

        public ServiceSeverityLst_Form() { InitializeComponent(); }
        public ServiceSeverityLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceSeverityLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
