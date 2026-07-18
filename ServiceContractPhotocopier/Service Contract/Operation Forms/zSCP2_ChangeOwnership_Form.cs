using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// "Change Ownership" dialog for a service item. Ownership is either a CONTRACT (the item is
    /// attached; its owner is that contract's debtor) or a CUSTOMER directly (no contract). The
    /// caller applies the change and records it in the Debtors Ownership History (a transfer to a
    /// contract of the SAME customer is a re-attach, not an ownership change).
    /// </summary>
    public partial class zSCP2_ChangeOwnership_Form : XtraForm
    {
        private readonly DBSetting _db;

        /// <summary>true = transfer to the picked contract; false = transfer to the picked customer.</summary>
        public bool ToContract { get { return ChkToContract.Checked; } }

        public long SelectedContractKey
        {
            get
            {
                if (!ChkToContract.Checked || SluContract.EditValue == null || SluContract.EditValue == DBNull.Value) return 0;
                long k; long.TryParse(SluContract.EditValue.ToString(), out k); return k;
            }
        }

        public string SelectedDebtorCode
        {
            get
            {
                if (ChkToContract.Checked || SluCustomer.EditValue == null || SluCustomer.EditValue == DBNull.Value) return "";
                return SluCustomer.EditValue.ToString().Trim();
            }
        }

        public zSCP2_ChangeOwnership_Form()
        {
            InitializeComponent();
        }

        public zSCP2_ChangeOwnership_Form(DBSetting db, string currentOwnership) : this()
        {
            _db = db;
            LblCurrent.Text = currentOwnership;
            LoadLookups();
            ChkToContract.CheckedChanged += new EventHandler(Radio_Changed);
            ChkToCustomer.CheckedChanged += new EventHandler(Radio_Changed);
            Radio_Changed(null, EventArgs.Empty);
        }

        private void LoadLookups()
        {
            try
            {
                DataTable c = _db.GetDataTable(
                    "SELECT c.ContractKey, c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM [dbo].[zSCP2_Contract] c LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                    "WHERE c.Inactive = 'N' ORDER BY c.ContractNo", false);
                SluContract.Properties.DataSource = c;
                SluContract.Properties.ValueMember = "ContractKey";
                SluContract.Properties.DisplayMember = "ContractNo";
            }
            catch { }
            try
            {
                DataTable d = _db.GetDataTable(
                    "SELECT AccNo, ISNULL(CompanyName,'') AS CompanyName FROM [dbo].[Debtor] ORDER BY AccNo", false);
                SluCustomer.Properties.DataSource = d;
                SluCustomer.Properties.ValueMember = "AccNo";
                SluCustomer.Properties.DisplayMember = "AccNo";
            }
            catch { }
        }

        private void Radio_Changed(object sender, EventArgs e)
        {
            SluContract.Enabled = ChkToContract.Checked;
            SluCustomer.Enabled = ChkToCustomer.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ChkToContract.Checked && SelectedContractKey <= 0)
            { XtraMessageBox.Show("Pick the contract to transfer this service item to.", "Validation"); return; }
            if (!ChkToContract.Checked && SelectedDebtorCode.Length == 0)
            { XtraMessageBox.Show("Pick the customer to transfer this service item to.", "Validation"); return; }
            this.DialogResult = DialogResult.OK;
        }
    }
}
