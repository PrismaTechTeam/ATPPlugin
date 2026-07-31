using System;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Bulk Email Invoice message template (demo feedback 28/07 #9a). Saved once per account book
    /// (Z_PumsConfig) — the Bulk Email screen uses it for every run, so nobody retypes the wording.
    /// Tokens are substituted per customer at send time:
    ///   {AccNo} = debtor code · {CompanyName} = debtor name · {DocNos} = that customer's invoice numbers.
    /// </summary>
    public partial class BulkEmailTemplate_Form : XtraForm
    {
        private readonly DBSetting _dbSetting;

        public BulkEmailTemplate_Form()
        {
            InitializeComponent();
        }

        public BulkEmailTemplate_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            if (_dbSetting != null)
            {
                TxtSubject.Text = PumsConfig.Get(_dbSetting,
                    PumsConfig.KEY_BULKMAIL_SUBJECT, PumsConfig.DEFAULT_BULKMAIL_SUBJECT);
                TxtBody.Text = PumsConfig.Get(_dbSetting,
                    PumsConfig.KEY_BULKMAIL_BODY, PumsConfig.DEFAULT_BULKMAIL_BODY);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) { this.DialogResult = DialogResult.Cancel; return; }
            string subject = (TxtSubject.Text ?? "").Trim();
            if (subject.Length == 0) subject = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            PumsConfig.Set(_dbSetting, PumsConfig.KEY_BULKMAIL_SUBJECT, subject);
            PumsConfig.Set(_dbSetting, PumsConfig.KEY_BULKMAIL_BODY, TxtBody.Text ?? "");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            TxtSubject.Text = PumsConfig.DEFAULT_BULKMAIL_SUBJECT;
            TxtBody.Text = PumsConfig.DEFAULT_BULKMAIL_BODY;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
