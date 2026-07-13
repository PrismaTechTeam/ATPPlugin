using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Person",
    ParentMenuCaption = "General Setup",
    MenuOrder = 100,
    ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_PERSON,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_PERSON)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServicePersonLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_ServicePerson"; } }
        protected override string KeyColumn   { get { return "ServicePersonCode"; } }
        protected override string FormCaption { get { return "Service Person"; } }
        public ServicePersonLst_Form() { InitializeComponent(); }
        public ServicePersonLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServicePersonLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new ServicePerson_Form(_dbSetting, existingRow))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) LoadData();
            }
        }
    }
}
