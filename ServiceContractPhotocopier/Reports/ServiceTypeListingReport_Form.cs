using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Type Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 30, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class ServiceTypeListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Type Listing Report"; } }
        public ServiceTypeListingReport_Form() { InitializeComponent(); }
        public ServiceTypeListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceTypeListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
