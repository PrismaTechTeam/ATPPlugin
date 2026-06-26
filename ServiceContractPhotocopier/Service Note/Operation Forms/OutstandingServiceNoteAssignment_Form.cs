using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceNote.OperationForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Outstanding Service Note Assignment Inquiry",
    // ParentMenuCaption = "Inquiry", MenuOrder = 50, ParentMenuOrder = 700,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_OUTSTANDING_NOTE_ASSIGNMENT,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_OUTSTANDING_NOTE_ASSIGNMENT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class OutstandingServiceNoteAssignment_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_OutstandingServiceNoteAssignment"; } }
        protected override string ViewName    { get { return "zvSCP_OutstandingServiceNoteAssignment"; } }
        protected override string KeyColumn   { get { return "ServiceNoteCode"; } }
        protected override string FormCaption { get { return "Outstanding Service Note Assignment Inquiry"; } }
        public OutstandingServiceNoteAssignment_Form() { InitializeComponent(); }
        public OutstandingServiceNoteAssignment_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public OutstandingServiceNoteAssignment_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new ServiceNote_Form(_dbSetting))
            {
                f.ShowDialog(this);
                LoadData();
            }
        }
    }
}
