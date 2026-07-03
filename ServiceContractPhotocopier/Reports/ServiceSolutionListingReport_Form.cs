using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Solution Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 60, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceSolutionListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Solution Listing Report"; } }
        public ServiceSolutionListingReport_Form() { InitializeComponent(); }
        public ServiceSolutionListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceSolutionListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
