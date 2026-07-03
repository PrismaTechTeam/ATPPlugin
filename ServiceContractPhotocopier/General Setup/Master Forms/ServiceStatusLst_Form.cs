using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Status",
    ParentMenuCaption = "General Setup",
    MenuOrder = 10,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_STATUS,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_STATUS)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceStatusLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceStatus"; } }
        protected override string KeyColumn   { get { return "ServiceStatusCode"; } }
        protected override string KeyField    { get { return "ServiceStatusKey"; } }
        protected override string FormCaption { get { return "Service Status"; } }
        protected override string StatusText  { get { return "Service status"; } }

        public ServiceStatusLst_Form() { InitializeComponent(); }
        public ServiceStatusLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceStatusLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
