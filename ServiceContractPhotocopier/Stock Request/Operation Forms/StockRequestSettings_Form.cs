using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.StockRequest.OperationForms
{
    /// <summary>
    /// Modal settings dialog for the Stock Request Task form.
    /// Currently exposes: default Stock Transfer From-Location (used by Generate SI/ST).
    /// </summary>
    public partial class StockRequestSettings_Form : XtraForm
    {
        private readonly DBSetting _dbSetting;

        public StockRequestSettings_Form() { InitializeComponent(); }

        public StockRequestSettings_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            if (_dbSetting != null)
            {
                LoadLocations();
                this.TxtFromLocation.EditValue = PumsConfig.Get(_dbSetting,
                    PumsConfig.KEY_DEFAULT_FROM_LOCATION,
                    PumsConfig.DEFAULT_FROM_LOCATION_VALUE);
                this.ChkFlagControl.Checked = PumsConfig.GetBool(_dbSetting,
                    PumsConfig.KEY_FLAG_CONTROL, false);
            }
        }

        private void LoadLocations()
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(_dbSetting.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT Location, ISNULL(Description,'') AS Description FROM Location " +
                    "WHERE ISNULL(IsActive,'Y') IN ('Y','T','1','True','true') ORDER BY Location", conn))
                {
                    conn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        da.Fill(dt);
                }
            }
            catch { /* fall back to an empty list — search box still typeable */ }

            this.TxtFromLocation.Properties.DataSource    = dt;
            this.TxtFromLocation.Properties.ValueMember   = "Location";
            this.TxtFromLocation.Properties.DisplayMember = "Location";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null)
            {
                this.DialogResult = DialogResult.Cancel;
                return;
            }
            string val = Convert.ToString(this.TxtFromLocation.EditValue ?? string.Empty).Trim();
            if (val.Length == 0) val = PumsConfig.DEFAULT_FROM_LOCATION_VALUE;
            PumsConfig.Set(_dbSetting, PumsConfig.KEY_DEFAULT_FROM_LOCATION, val);
            PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_FLAG_CONTROL, this.ChkFlagControl.Checked);
            this.DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
