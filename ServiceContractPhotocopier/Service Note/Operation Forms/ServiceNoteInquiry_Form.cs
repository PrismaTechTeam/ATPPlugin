using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceNote.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Service Note Inquiry",
    ParentMenuCaption = "Inquiry", MenuOrder = 30, ParentMenuOrder = 700,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_NOTE_INQUIRY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_NOTE_INQUIRY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteInquiry_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_ServiceNoteList"; } }
        protected override string ViewName    { get { return "zvSCP_ServiceNoteList"; } }
        protected override string KeyColumn   { get { return "ServiceNoteCode"; } }
        protected override string FormCaption { get { return "Service Note Inquiry"; } }
        public ServiceNoteInquiry_Form() { InitializeComponent(); }
        public ServiceNoteInquiry_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceNoteInquiry_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

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
