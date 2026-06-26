using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Problem Listing Report",
    // ParentMenuCaption = "Reports", MenuOrder = 70, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceProblemListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Problem Listing Report"; } }
        public ServiceProblemListingReport_Form() { InitializeComponent(); }
        public ServiceProblemListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceProblemListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
