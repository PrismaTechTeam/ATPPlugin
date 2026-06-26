using System.Data;
using AutoCount.Authentication;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.BaseForms;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Mechanic",
    // ParentMenuCaption = "General Setup",
    // MenuOrder = 120,
    // ParentMenuOrder = 600,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_MECHANIC,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_MECHANIC)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class MechanicLst_Form : ScpLookupLst_Form
    {
        protected override string TableName   { get { return "zSCP_Mechanic"; } }
        protected override string KeyColumn   { get { return "MechanicCode"; } }
        protected override string FormCaption { get { return "Mechanic"; } }
        public MechanicLst_Form() { InitializeComponent(); }
        public MechanicLst_Form(UserSession userSession) : base(userSession != null ? userSession.DBSetting : null) { InitializeComponent(); }
        public MechanicLst_Form(DBSetting dbSetting) : base(dbSetting) { InitializeComponent(); }

        protected override void OpenEditor(DataRow existingRow)
        {
            using (var f = new Mechanic_Form(_dbSetting, existingRow))
            {
                if (f.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) LoadData();
            }
        }
    }
}
