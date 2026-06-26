using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Top Service Stock Code Report",
    // ParentMenuCaption = "Reports", MenuOrder = 160, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class TopServiceStockCodeReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Top Service Stock Code Report"; } }
        public TopServiceStockCodeReport_Form() { InitializeComponent(); }
        public TopServiceStockCodeReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public TopServiceStockCodeReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
