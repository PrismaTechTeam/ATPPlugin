using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// Per-meter WAIVE configuration for a Rental-Waive meter (the master-style contra line, e.g.
    /// RA-MONTH(W)-13MTH). The billing engine reads these values at every Generate and decides by
    /// itself whether the waive fires this month — nobody watches usage manually any more.
    ///   • Nothing ticked                = ALWAYS waive (legacy master behavior: contra every month)
    ///   • "First N months"              = months 1..N always waived (anchor: rental start, else
    ///                                     the machine's effective service start); then it retires
    ///   • "Print charges hit target"    = fires when the MACHINE's scoped BK/CL charges reach the
    ///                                     target (the partial % band gives a partial contra)
    ///   • Both ticked (SEQUENTIAL)      = free window FIRST, and AFTER it the target takes over —
    ///                                     "走完免费就靠 meter 去 waive".
    /// The dialog only edits values on the meter ROW — persisting rides the contract/item save.
    /// </summary>
    public partial class WaiveConfig_Form : XtraForm
    {
        public int FirstNMonths;        // 0 = no window condition
        public decimal TargetAmount;    // 0 = no usage condition
        public decimal PartialPct = 100m;
        public string Scope = "BKCL";   // BKCL / BK / CL

        public WaiveConfig_Form()
        {
            InitializeComponent();
        }

        public WaiveConfig_Form(string meterTypeCode, int firstN, decimal target, decimal partialPct, string scope)
            : this()
        {
            LblMeter.Text = "Waive meter:  " + (meterTypeCode ?? "");
            ChkFirstN.Checked = firstN > 0;
            SpnFirstN.Value = firstN > 0 ? firstN : 12;
            ChkTarget.Checked = target > 0m;
            SpnTarget.Value = target > 0m ? target : 500m;
            SpnPartial.Value = partialPct >= 1m && partialPct <= 100m ? partialPct : 100m;
            string sc = (scope ?? "").Trim().ToUpperInvariant();
            CmbScope.SelectedIndex = sc == "BK" ? 1 : (sc == "CL" ? 2 : 0);
            ChkFirstN.CheckedChanged += new EventHandler(Conditions_Changed);
            ChkTarget.CheckedChanged += new EventHandler(Conditions_Changed);
            Conditions_Changed(this, EventArgs.Empty);
        }

        private void Conditions_Changed(object sender, EventArgs e)
        {
            SpnFirstN.Enabled = ChkFirstN.Checked;
            SpnTarget.Enabled = ChkTarget.Checked;
            SpnPartial.Enabled = ChkTarget.Checked;
            CmbScope.Enabled = ChkTarget.Checked;
            LblAlways.Visible = !ChkFirstN.Checked && !ChkTarget.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ChkFirstN.Checked && (int)SpnFirstN.Value < 1)
            { XtraMessageBox.Show("First N months must be at least 1.", "Validation"); return; }
            if (ChkTarget.Checked && SpnTarget.Value <= 0m)
            { XtraMessageBox.Show("Target amount must be more than 0.", "Validation"); return; }
            FirstNMonths = ChkFirstN.Checked ? (int)SpnFirstN.Value : 0;
            TargetAmount = ChkTarget.Checked ? SpnTarget.Value : 0m;
            PartialPct = ChkTarget.Checked ? SpnPartial.Value : 100m;
            Scope = CmbScope.SelectedIndex == 1 ? "BK" : (CmbScope.SelectedIndex == 2 ? "CL" : "BKCL");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>Short grid caption for a waive config, e.g. "WAIVE: always" /
        /// "WAIVE: 1st 13 mth" / "WAIVE: hit RM 500 (90%)" / combined.</summary>
        public static string Summary(int firstN, decimal target, decimal partialPct)
        {
            if (firstN <= 0 && target <= 0m) return "WAIVE: always";
            string s = "WAIVE:";
            if (firstN > 0) s += " 1st " + firstN + " mth";
            if (target > 0m)
            {
                if (firstN > 0) s += " then";
                s += " hit RM " + target.ToString("0.##");
                if (partialPct > 0m && partialPct < 100m) s += " (" + partialPct.ToString("0.##") + "%)";
            }
            return s;
        }
    }
}
