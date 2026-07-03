using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.ServiceNote.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Service Note",
    MenuOrder = 300,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_NOTE,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_NOTE)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceNoteLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zvSCP_ServiceNoteList"; } }
        protected override string ViewName    { get { return "zvSCP_ServiceNoteList"; } }
        protected override string KeyColumn   { get { return "ServiceNoteCode"; } }
        protected override string FormCaption { get { return "Service Note"; } }
        public ServiceNoteLst_Form() { InitializeComponent(); }
        public ServiceNoteLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public ServiceNoteLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

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
