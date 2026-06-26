using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Advisor",
    // ParentMenuCaption = "General Setup",
    // MenuOrder = 110,
    // ParentMenuOrder = 600,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_ADVISOR,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_ADVISOR)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceAdvisorLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_ServiceAdvisor"; } }
        protected override string KeyColumn   { get { return "ServiceAdvisorCode"; } }
        protected override string FormCaption { get { return "Service Advisor"; } }
        public ServiceAdvisorLst_Form() { InitializeComponent(); }
        public ServiceAdvisorLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceAdvisorLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new ServiceAdvisor_Form(_dbSetting, existingRow))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) LoadData();
            }
        }
    }
}
