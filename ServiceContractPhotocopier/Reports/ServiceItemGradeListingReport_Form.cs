using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Service Item Grade Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 10, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceItemGradeListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Item Grade Listing Report"; } }
        public ServiceItemGradeListingReport_Form() { InitializeComponent(); }
        public ServiceItemGradeListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceItemGradeListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
