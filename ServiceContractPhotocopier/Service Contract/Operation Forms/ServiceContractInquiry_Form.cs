using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Service Contract Inquiry (IN MAINTENANCE)",
    ParentMenuCaption = "Inquiry", MenuOrder = 20, ParentMenuOrder = 700,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_CONTRACT_INQUIRY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_CONTRACT_INQUIRY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceContractInquiry_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_ServiceContractList"; } }
        protected override string ViewName    { get { return "zvSCP_ServiceContractList"; } }
        protected override string KeyColumn   { get { return "ServiceContractCode"; } }
        protected override bool UnderMaintenance { get { return true; } }   // reads a v1 view the v2 module never fills
        protected override string FormCaption { get { return "Service Contract Inquiry"; } }
        public ServiceContractInquiry_Form() { InitializeComponent(); }
        public ServiceContractInquiry_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceContractInquiry_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new ServiceContract_Form(_dbSetting))
            {
                f.ShowDialog(this);
                LoadData();
            }
        }
    }
}
