using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Outstanding Service Contract Item Inquiry",
    // ParentMenuCaption = "Inquiry", MenuOrder = 60, ParentMenuOrder = 700,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_OUTSTANDING_CONTRACT_ITEM,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_OUTSTANDING_CONTRACT_ITEM)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class OutstandingServiceContractItem_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_OutstandingServiceContractItem"; } }
        protected override string ViewName    { get { return "zvSCP_OutstandingServiceContractItem"; } }
        protected override string KeyColumn   { get { return "ServiceContractCode"; } }
        protected override string FormCaption { get { return "Outstanding Service Contract Item Inquiry"; } }
        public OutstandingServiceContractItem_Form() { InitializeComponent(); }
        public OutstandingServiceContractItem_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public OutstandingServiceContractItem_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

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
