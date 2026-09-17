using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// The one notice every menu item marked (IN MAINTENANCE) opens. The screen behind the menu
    /// item is never shown: it hands over to this dialog on load and closes itself, so the user
    /// sees one consistent message instead of an empty list or a scaffold.
    /// </summary>
    public partial class ScpMaintenance_Form : XtraForm
    {
        public ScpMaintenance_Form()
        {
            InitializeComponent();
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
        }

        public ScpMaintenance_Form(string featureName) : this()
        {
            string name = (featureName ?? "").Replace("(IN MAINTENANCE)", "").Trim();
            if (name.Length > 0) this.LblFeature.Text = name;
        }

        /// <summary>Show the notice for a feature, modally over its owner when there is one.</summary>
        public static void ShowFor(IWin32Window owner, string featureName)
        {
            using (ScpMaintenance_Form f = new ScpMaintenance_Form(featureName))
            {
                if (owner != null) f.ShowDialog(owner); else f.ShowDialog();
            }
        }

        /// <summary>For a form that stands in for a switched-off feature: keep it invisible, show
        /// the notice as soon as it loads, and close it right after. Call from the form's
        /// constructor, before it is shown.</summary>
        public static void TakeOver(Form standIn, string featureName)
        {
            if (standIn == null) return;
            standIn.Opacity = 0;
            standIn.ShowInTaskbar = false;
            standIn.Load += delegate
            {
                ShowFor(standIn, featureName);
                standIn.BeginInvoke(new MethodInvoker(standIn.Close));
            };
        }
    }
}
