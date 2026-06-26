using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Person Listing Report",
    // ParentMenuCaption = "Reports", MenuOrder = 110, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServicePersonListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Person Listing Report"; } }
        public ServicePersonListingReport_Form() { InitializeComponent(); }
        public ServicePersonListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServicePersonListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
