using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Mechanic Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 120, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MechanicListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Mechanic Listing Report"; } }
        public MechanicListingReport_Form() { InitializeComponent(); }
        public MechanicListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public MechanicListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
