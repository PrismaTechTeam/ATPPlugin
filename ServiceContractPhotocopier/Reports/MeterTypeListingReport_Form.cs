using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Meter Type Listing Report",
    ParentMenuCaption = "Reports", MenuOrder = 180, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MeterTypeListingReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Meter Type Listing Report"; } }
        public MeterTypeListingReport_Form() { InitializeComponent(); }
        public MeterTypeListingReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public MeterTypeListingReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
