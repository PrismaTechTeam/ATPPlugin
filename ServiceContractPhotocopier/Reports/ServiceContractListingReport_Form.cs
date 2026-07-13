using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Contract Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 140, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceContractListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Contract Listing Report"; } }
        public ServiceContractListingReport_Form() { InitializeComponent(); }
        public ServiceContractListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceContractListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
