using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Type",
    ParentMenuCaption = "General Setup",
    MenuOrder = 50,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_TYPE,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_TYPE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceTypeLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceType"; } }
        protected override string KeyColumn   { get { return "ServiceTypeCode"; } }
        protected override string KeyField    { get { return "ServiceTypeKey"; } }
        protected override string FormCaption { get { return "Service Type"; } }
        protected override string StatusText  { get { return "Service type"; } }

        public ServiceTypeLst_Form() { InitializeComponent(); }
        public ServiceTypeLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceTypeLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
