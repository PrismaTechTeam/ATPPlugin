using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// "Copy Meters To…" picker: LEFT = which of the source machine's meters to copy (all ticked by
    /// default), RIGHT = which other machines of the contract receive them. Pure selection dialog —
    /// the contract editor does the actual copying (and the duplicate/role/waive-rule skipping).
    /// </summary>
    public partial class CopyMetersTo_Form : XtraForm
    {
        public readonly List<int> MeterIndices = new List<int>();
        public readonly List<int> TargetIndices = new List<int>();

        public CopyMetersTo_Form()
        {
            InitializeComponent();
        }

        public CopyMetersTo_Form(List<string> meterLabels, List<string> targetLabels)
            : this()
        {
            foreach (string s in meterLabels)
                LstMeters.Items.Add(new DevExpress.XtraEditors.Controls.CheckedListBoxItem(s, true));
            foreach (string s in targetLabels)
                LstTargets.Items.Add(new DevExpress.XtraEditors.Controls.CheckedListBoxItem(s, false));
        }

        private void BtnAllTargets_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < LstTargets.Items.Count; i++)
                LstTargets.Items[i].CheckState = CheckState.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            MeterIndices.Clear();
            TargetIndices.Clear();
            for (int i = 0; i < LstMeters.Items.Count; i++)
                if (LstMeters.Items[i].CheckState == CheckState.Checked) MeterIndices.Add(i);
            for (int i = 0; i < LstTargets.Items.Count; i++)
                if (LstTargets.Items[i].CheckState == CheckState.Checked) TargetIndices.Add(i);
            if (MeterIndices.Count == 0)
            { XtraMessageBox.Show("Tick at least one meter to copy.", "Copy Meters To"); return; }
            if (TargetIndices.Count == 0)
            { XtraMessageBox.Show("Tick at least one target machine.", "Copy Meters To"); return; }
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
