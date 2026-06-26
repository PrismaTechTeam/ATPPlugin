using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Problem",
    // ParentMenuCaption = "General Setup",
    // MenuOrder = 40,
    // ParentMenuOrder = 600,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_PROBLEM,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_PROBLEM)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceProblemLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceProblem"; } }
        protected override string KeyColumn   { get { return "ServiceProblemCode"; } }
        protected override string KeyField    { get { return "ServiceProblemKey"; } }
        protected override string FormCaption { get { return "Service Problem"; } }
        protected override string StatusText  { get { return "Service problem"; } }

        public ServiceProblemLst_Form() { InitializeComponent(); }
        public ServiceProblemLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceProblemLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
