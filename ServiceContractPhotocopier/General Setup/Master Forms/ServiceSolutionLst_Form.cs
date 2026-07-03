using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Solution",
    ParentMenuCaption = "General Setup",
    MenuOrder = 30,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_SOLUTION,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_SOLUTION)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceSolutionLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceSolution"; } }
        protected override string KeyColumn   { get { return "ServiceSolutionCode"; } }
        protected override string KeyField    { get { return "ServiceSolutionKey"; } }
        protected override string FormCaption { get { return "Service Solution"; } }
        protected override string StatusText  { get { return "Service solution"; } }

        public ServiceSolutionLst_Form() { InitializeComponent(); }
        public ServiceSolutionLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceSolutionLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
