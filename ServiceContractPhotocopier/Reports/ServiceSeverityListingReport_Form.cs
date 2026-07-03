using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Severity Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 50, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceSeverityListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Severity Listing Report"; } }
        public ServiceSeverityListingReport_Form() { InitializeComponent(); }
        public ServiceSeverityListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceSeverityListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
