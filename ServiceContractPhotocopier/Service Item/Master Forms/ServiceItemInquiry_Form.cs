using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceItem.MasterForms
{
    [AutoCount.PlugIn.MenuItem("Service Item Inquiry",
    ParentMenuCaption = "Inquiry", MenuOrder = 10, ParentMenuOrder = 700,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_ITEM_INQUIRY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_ITEM_INQUIRY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceItemInquiry_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_ServiceItemList"; } }
        protected override string ViewName    { get { return "zvSCP_ServiceItemList"; } }
        protected override string KeyColumn   { get { return "ServiceItemCode"; } }
        protected override string FormCaption { get { return "Service Item Inquiry"; } }
        public ServiceItemInquiry_Form() { InitializeComponent(); }
        public ServiceItemInquiry_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceItemInquiry_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new ServiceItem_Form(_dbSetting))
            {
                f.ShowDialog(this);
                LoadData();
            }
        }
    }
}
