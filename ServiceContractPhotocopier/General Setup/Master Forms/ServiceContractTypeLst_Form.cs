using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Contract Type",
    ParentMenuCaption = "General Setup",
    MenuOrder = 60,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_CONTRACT_TYPE,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_CONTRACT_TYPE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceContractTypeLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceContractType"; } }
        protected override string KeyColumn   { get { return "ServiceContractTypeCode"; } }
        protected override string KeyField    { get { return "ServiceContractTypeKey"; } }
        protected override string FormCaption { get { return "Service Contract Type"; } }
        protected override string StatusText  { get { return "Service contract type"; } }

        public ServiceContractTypeLst_Form() { InitializeComponent(); }
        public ServiceContractTypeLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceContractTypeLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
