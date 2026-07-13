using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Item Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 150, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceItemListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Item Listing Report"; } }
        public ServiceItemListingReport_Form() { InitializeComponent(); }
        public ServiceItemListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceItemListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
