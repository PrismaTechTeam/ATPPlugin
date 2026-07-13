using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Contract Type Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 20, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceContractTypeListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Contract Type Listing Report"; } }
        public ServiceContractTypeListingReport_Form() { InitializeComponent(); }
        public ServiceContractTypeListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceContractTypeListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
