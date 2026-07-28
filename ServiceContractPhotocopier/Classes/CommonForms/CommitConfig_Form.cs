using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// Committed-minimum (MIN) meter configuration: WHICH print charges count toward the committed
    /// amount — BK only / CL only / BK + CL. The committed amount itself is the meter row's
    /// Min Charges (shown read-only here). At Generate the engine bills the TOP-UP:
    /// e.g. scoped charges 270 with committed 300 -> the MIN line bills 30 (270 + 30 = 300).
    /// </summary>
    public partial class CommitConfig_Form : XtraForm
    {
        public string Scope = "BKCL";   // BKCL / BK / CL

        public CommitConfig_Form()
        {
            InitializeComponent();
        }

        public CommitConfig_Form(string meterTypeCode, decimal committedAmount, string scope)
            : this()
        {
            LblMeter.Text = "MIN meter:  " + (meterTypeCode ?? "");
            LblAmount.Text = "Committed minimum (the row's Min Charges):  RM " + committedAmount.ToString("#,##0.00");
            string sc = (scope ?? "").Trim().ToUpperInvariant();
            CmbScope.SelectedIndex = sc == "BK" ? 1 : (sc == "CL" ? 2 : 0);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Scope = CmbScope.SelectedIndex == 1 ? "BK" : (CmbScope.SelectedIndex == 2 ? "CL" : "BKCL");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
