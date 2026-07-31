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
    ///                                     target; the optional PARTIAL band (both in RM, demo
    ///                                     28/07 #24) fires when charges reach a LOWER RM threshold
    ///                                     and waives a FIXED RM amount instead of the whole rent
    ///   • Both ticked (SEQUENTIAL)      = free window FIRST, and AFTER it the target takes over —
    ///                                     "走完免费就靠 meter 去 waive".
    /// The dialog only edits values on the meter ROW — persisting rides the contract/item save.
    /// </summary>
    public partial class WaiveConfig_Form : XtraForm
    {
        public int FirstNMonths;            // 0 = no window condition
        public decimal TargetAmount;        // 0 = no usage condition
        public decimal PartialThreshold;    // 0 = no partial band; RM the charges must reach
        public decimal PartialAmount;       // RM waived off the rental when the partial band fires
        public string Scope = "BKCL";       // BKCL / BK / CL

        public WaiveConfig_Form()
        {
            InitializeComponent();
        }

        public WaiveConfig_Form(string meterTypeCode, int firstN, decimal target,
            decimal partialThreshold, decimal partialAmount, string scope)
            : this()
        {
            LblMeter.Text = "Waive meter:  " + (meterTypeCode ?? "");
            ChkFirstN.Checked = firstN > 0;
            SpnFirstN.Value = firstN > 0 ? firstN : 12;
            ChkTarget.Checked = target > 0m;
            SpnTarget.Value = target > 0m ? target : 500m;
            SpnPartialTh.Value = partialThreshold > 0m ? partialThreshold : 0m;
            SpnPartialAmt.Value = partialAmount > 0m ? partialAmount : 0m;
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
            SpnPartialTh.Enabled = ChkTarget.Checked;
            SpnPartialAmt.Enabled = ChkTarget.Checked;
            CmbScope.Enabled = ChkTarget.Checked;
            LblAlways.Visible = !ChkFirstN.Checked && !ChkTarget.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ChkFirstN.Checked && (int)SpnFirstN.Value < 1)
            { XtraMessageBox.Show("First N months must be at least 1.", "Validation"); return; }
            if (ChkTarget.Checked && SpnTarget.Value <= 0m)
            { XtraMessageBox.Show("Target amount must be more than 0.", "Validation"); return; }
            if (ChkTarget.Checked && ((SpnPartialTh.Value > 0m) != (SpnPartialAmt.Value > 0m)))
            {
                XtraMessageBox.Show("Partial waive needs BOTH amounts: the RM the charges must " +
                    "reach AND the RM to waive.\r\nLeave both 0 for no partial band.", "Validation");
                return;
            }
            if (ChkTarget.Checked && SpnPartialTh.Value >= SpnTarget.Value && SpnPartialTh.Value > 0m)
            {
                XtraMessageBox.Show("The partial threshold must be BELOW the full target " +
                    "(reaching the target already waives the whole rental).", "Validation");
                return;
            }
            FirstNMonths = ChkFirstN.Checked ? (int)SpnFirstN.Value : 0;
            TargetAmount = ChkTarget.Checked ? SpnTarget.Value : 0m;
            PartialThreshold = ChkTarget.Checked ? SpnPartialTh.Value : 0m;
            PartialAmount = ChkTarget.Checked ? SpnPartialAmt.Value : 0m;
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
        /// "WAIVE: 1st 13 mth" / "WAIVE: hit RM 500 (or RM 405 -> RM 162 off)" / combined.</summary>
        public static string Summary(int firstN, decimal target, decimal partialThreshold, decimal partialAmount)
        {
            if (firstN <= 0 && target <= 0m) return "WAIVE: always";
            string s = "WAIVE:";
            if (firstN > 0) s += " 1st " + firstN + " mth";
            if (target > 0m)
            {
                if (firstN > 0) s += " then";
                s += " hit RM " + target.ToString("0.##");
                if (partialThreshold > 0m && partialAmount > 0m)
                    s += " (or RM " + partialThreshold.ToString("0.##") +
                         " -> RM " + partialAmount.ToString("0.##") + " off)";
            }
            return s;
        }
    }
}
