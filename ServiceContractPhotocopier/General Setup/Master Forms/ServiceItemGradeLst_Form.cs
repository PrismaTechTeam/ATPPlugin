using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Item Grade",
    ParentMenuCaption = "General Setup",
    MenuOrder = 70,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_ITEM_GRADE,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_ITEM_GRADE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceItemGradeLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_LK_ServiceItemGrade"; } }
        protected override string KeyColumn   { get { return "ServiceItemGradeCode"; } }
        protected override string KeyField    { get { return "ServiceItemGradeKey"; } }
        protected override string FormCaption { get { return "Service Item Grade"; } }
        protected override string StatusText  { get { return "Service item grade"; } }

        public ServiceItemGradeLst_Form() { InitializeComponent(); }
        public ServiceItemGradeLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceItemGradeLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
