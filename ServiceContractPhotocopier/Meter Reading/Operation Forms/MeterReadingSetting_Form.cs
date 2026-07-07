using System;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Meter Reading settings dialog. Currently one option: whether the meter list includes expired
    /// service items. Persists to Z_PumsConfig (INCLUDE_EXPIRED_ITEMS) via PumsConfig.
    /// </summary>
    public partial class MeterReadingSetting_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public MeterReadingSetting_Form()
        {
            InitializeComponent();
        }

        public MeterReadingSetting_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            if (_dbSetting != null)
                this.ChkIncludeExpired.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS, PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
        }

        /// <summary>True if the user wants expired service items shown in the meter list.</summary>
        public bool IncludeExpired { get { return this.ChkIncludeExpired.Checked; } }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_dbSetting != null)
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS, this.ChkIncludeExpired.Checked);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
