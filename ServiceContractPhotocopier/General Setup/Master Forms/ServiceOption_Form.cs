using System;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Service Option — tabbed global configuration. Persists to z_SysConfig table.
    /// </summary>
    // [MENU HIDDEN FOR CUSTOMER RELEASE - uncomment to show]
    // [AutoCount.PlugIn.MenuItem("Service Option...",
    // MenuOrder = 900,
    // OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_OPTION,
    // VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_OPTION)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class ServiceOption_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceOption_Form() { InitializeComponent(); }
        public ServiceOption_Form(UserSession userSession) : this() { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public ServiceOption_Form(DBSetting dbSetting) : this() { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            try
            {
                ChkShowStockPicture.Checked = GetBool("SCP.ShowStockPicture", false);
                ChkUseAlternativeItem.Checked = GetBool("SCP.UseAlternativeItem", false);
                ChkNegativeStockChecking.Checked = GetBool("SCP.NegativeStockChecking", true);
                ChkAutoGenSalesInvoice.Checked = GetBool("SCP.AutoGenerateSalesInvoice", false);
                ChkAutoCloseNote.Checked = GetBool("SCP.AutoCloseServiceNote", false);
                ChkAllowEditClosed.Checked = GetBool("SCP.AllowEditClosedNote", false);
                TxtDefaultServiceStatus.Text = GetStr("SCP.DefaultServiceStatus", "OPEN");
                TxtDefaultAppointmentPriority.Text = GetStr("SCP.DefaultAppointmentPriority", "NORMAL");
            }
            catch { }
        }

        private bool GetBool(string key, bool def)
        {
            try
            {
                var v = _dbSetting.ExecuteScalar("SELECT ConfigValue FROM [dbo].[z_SysConfig] WHERE ConfigName = N'" + SQLString(key) + "'");
                if (v == null || v == DBNull.Value) return def;
                var s = v.ToString().ToLower();
                return s == "true" || s == "1" || s == "y";
            }
            catch { return def; }
        }

        private string GetStr(string key, string def)
        {
            try
            {
                var v = _dbSetting.ExecuteScalar("SELECT ConfigValue FROM [dbo].[z_SysConfig] WHERE ConfigName = N'" + SQLString(key) + "'");
                if (v == null || v == DBNull.Value) return def;
                return v.ToString();
            }
            catch { return def; }
        }

        private void SetConfig(string key, string val, string desc, string type)
        {
            try
            {
                string delSql = "DELETE FROM [dbo].[z_SysConfig] WHERE ConfigName = N'" + SQLString(key) + "'";
                _dbSetting.ExecuteNonQuery(delSql);
                string insSql = "INSERT INTO [dbo].[z_SysConfig] (ConfigName, ConfigDesc, ConfigDataType, ConfigValue) VALUES (N'" +
                    SQLString(key) + "', N'" + SQLString(desc) + "', N'" + SQLString(type) + "', N'" + SQLString(val) + "')";
                _dbSetting.ExecuteNonQuery(insSql);
            }
            catch (Exception ex) { throw new Exception("SetConfig failed for " + key + ": " + ex.Message); }
        }

        private void OnSave(object sender, EventArgs e)
        {
            try
            {
                SetConfig("SCP.ShowStockPicture",         ChkShowStockPicture.Checked ? "True" : "False", "Show stock picture in service forms", "BOOL");
                SetConfig("SCP.UseAlternativeItem",       ChkUseAlternativeItem.Checked ? "True" : "False", "Allow alternative items in service notes", "BOOL");
                SetConfig("SCP.NegativeStockChecking",    ChkNegativeStockChecking.Checked ? "True" : "False", "Enable negative stock balance checking", "BOOL");
                SetConfig("SCP.AutoGenerateSalesInvoice", ChkAutoGenSalesInvoice.Checked ? "True" : "False", "Auto-generate sales invoice on note close", "BOOL");
                SetConfig("SCP.AutoCloseServiceNote",     ChkAutoCloseNote.Checked ? "True" : "False", "Auto-close service note on solution save", "BOOL");
                SetConfig("SCP.AllowEditClosedNote",      ChkAllowEditClosed.Checked ? "True" : "False", "Allow editing of closed service notes", "BOOL");
                SetConfig("SCP.DefaultServiceStatus",     TxtDefaultServiceStatus.Text ?? "OPEN", "Default service note status code", "STRING");
                SetConfig("SCP.DefaultAppointmentPriority", TxtDefaultAppointmentPriority.Text ?? "NORMAL", "Default appointment priority code", "STRING");

                XtraMessageBox.Show("Service options saved.", "Service Option", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
