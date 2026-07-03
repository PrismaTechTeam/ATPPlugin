using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.Reports
{
    [AutoCount.PlugIn.MenuItem("Top Service Stock Code by Department Report",
    ParentMenuCaption = "Reports", MenuOrder = 170, ParentMenuOrder = 800)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class TopServiceStockCodeByDeptReport_Form : ScpPlaceholder_Form
    {
        protected override string FormCaption { get { return "Top Service Stock Code by Department Report"; } }
        public TopServiceStockCodeByDeptReport_Form() { InitializeComponent(); }
        public TopServiceStockCodeByDeptReport_Form(UserSession userSession) : base(userSession) { InitializeComponent(); }
        public TopServiceStockCodeByDeptReport_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }
    }
}
