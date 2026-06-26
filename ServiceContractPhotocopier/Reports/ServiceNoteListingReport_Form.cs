using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Note Listing Report",
    // ParentMenuCaption = "Reports", MenuOrder = 130, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Service Note Listing Report"; } }
        public ServiceNoteListingReport_Form() { InitializeComponent(); }
        public ServiceNoteListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public ServiceNoteListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
