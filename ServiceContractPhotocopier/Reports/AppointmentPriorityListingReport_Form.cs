using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Appointment Priority Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 90, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class AppointmentPriorityListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Appointment Priority Listing Report"; } }
        public AppointmentPriorityListingReport_Form() { InitializeComponent(); }
        public AppointmentPriorityListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public AppointmentPriorityListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
