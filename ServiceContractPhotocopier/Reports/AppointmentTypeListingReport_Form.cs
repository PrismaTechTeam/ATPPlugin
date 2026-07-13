using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Appointment Type Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 80, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class AppointmentTypeListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Appointment Type Listing Report"; } }
        public AppointmentTypeListingReport_Form() { InitializeComponent(); }
        public AppointmentTypeListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public AppointmentTypeListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
