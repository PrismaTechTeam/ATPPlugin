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
    /// Plugin Option — the plugin's own settings centre (tab per area; "Service Option" is one tab,
    /// carrying the service checkboxes, defaults and the Meter Invoice No. Format).
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Plugin Option...",
    MenuOrder = 900,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_OPTION,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_OPTION)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Normal, false)]
    public partial class ServiceOption_Form : XtraForm
    {
        private DBSetting _dbSetting;

        public ServiceOption_Form() { InitializeComponent(); ApplyButtonIcons(); }

        // Same AutoCount large icons as the other maintenance modules.
        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                BtnCancel.ImageOptions.Image = img.GetLargeImage_Cancel();
            }
            catch { }   // icons are cosmetic — never block the form
        }
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

                // Meter invoice numbering: pick one of the book's native IV Document Numbering Formats.
                try
                {
                    CmbMeterInvFormat.Properties.Items.Clear();
                    System.Data.DataTable fmts = _dbSetting.GetDataTable(
                        "SELECT Name FROM dbo.DocNoFormat WHERE DocType='IV' ORDER BY Name", false);
                    foreach (System.Data.DataRow r in fmts.Rows)
                        CmbMeterInvFormat.Properties.Items.Add(r["Name"] as string ?? "");
                }
                catch { }
                CmbMeterInvFormat.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_INVOICE_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_INVOICE_DOCNO_FORMAT);
                ChkInvDescFromItem.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_DESC_FROM_ITEM,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INVOICE_DESC_FROM_ITEM);
                ChkLegacyDataBlock.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_LEGACY_DATA_BLOCK,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INVOICE_LEGACY_DATA_BLOCK);
                ChkLegacyDataBlock.ToolTip = "The old V8 invoice design reads 17 coded lines out of Further " +
                    "Description BY POSITION. On any other layout they print as raw numbers, which is why " +
                    "this is off - the readable reading rows are already on the invoice.";

                // #10: numbering format for meter-correction CREDIT NOTES (DocType 'CN');
                // empty = the book's default CN numbering.
                try
                {
                    CmbCnFormat.Properties.Items.Clear();
                    CmbCnFormat.Properties.Items.Add("");
                    System.Data.DataTable cnf = _dbSetting.GetDataTable(
                        "SELECT Name FROM dbo.DocNoFormat WHERE DocType='CN' ORDER BY Name", false);
                    foreach (System.Data.DataRow r in cnf.Rows)
                        CmbCnFormat.Properties.Items.Add(r["Name"] as string ?? "");
                }
                catch { }
                CmbCnFormat.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_CN_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_CN_DOCNO_FORMAT);

                // Contract & Item No. tab (customer feedback 07/08 "A" and "B").
                ChkItemRefFromContract.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_REF_FROM_CONTRACT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_ITEM_REF_FROM_CONTRACT);
                ChkItemNoFromContract.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_NO_FROM_CONTRACT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_ITEM_NO_FROM_CONTRACT);
                ChkItemNoRenumber.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_NO_FOLLOW_CONTRACT_RENAME,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_ITEM_NO_FOLLOW_CONTRACT_RENAME);
                OnItemNoFromContractChanged(null, EventArgs.Empty);

                ChkWhitelistOn.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST_ON,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST_ON);
                MemoWhitelist.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_EMAIL_WHITELIST);

                // Advanced No. Format by machine status: same IV format list as the default combo.
                try
                {
                    CmbInvFmtOnline.Properties.Items.Clear();
                    CmbInvFmtOffline.Properties.Items.Clear();
                    foreach (object it0 in CmbMeterInvFormat.Properties.Items)
                    {
                        CmbInvFmtOnline.Properties.Items.Add(it0);
                        CmbInvFmtOffline.Properties.Items.Add(it0);
                    }
                }
                catch { }
                ChkInvFmtAdvanced.Checked = ServiceContractPhotocopier.Data.PumsConfig.GetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ADVANCED, false);
                CmbInvFmtOnline.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ONLINE, "");
                CmbInvFmtOffline.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_OFFLINE, "");
                EventHandler advToggle = delegate
                {
                    CmbInvFmtOnline.Enabled = ChkInvFmtAdvanced.Checked;
                    CmbInvFmtOffline.Enabled = ChkInvFmtAdvanced.Checked;
                };
                ChkInvFmtAdvanced.CheckedChanged += advToggle;
                advToggle(null, EventArgs.Empty);

                LoadApiTab();
            }
            catch { }
        }

        // ===== API tab: connection profiles (dbo.zSCP2_ApiProfile) =====

        private bool _apiLoading;

        private void LoadApiTab()
        {
            try
            {
                CmbApiProfile.Properties.Items.Clear();
                System.Data.DataTable dt = _dbSetting.GetDataTable(
                    "SELECT ProfileName FROM [dbo].[zSCP2_ApiProfile] ORDER BY ProfileName", false);
                foreach (System.Data.DataRow r in dt.Rows)
                    CmbApiProfile.Properties.Items.Add(r["ProfileName"].ToString());
            }
            catch { }
            CmbApiProfile.SelectedIndexChanged += new EventHandler(CmbApiProfile_Changed);

            // Start on the ACTIVE profile; when none is recorded yet, show the raw live config values.
            string active = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_ACTIVE_PROFILE, "");
            _apiLoading = true;
            CmbApiProfile.Text = active;
            _apiLoading = false;
            if (active.Length > 0 && LoadApiProfileFields(active)) return;
            TxtApiBaseUrl.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_BASE_URL,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_API_BASE_URL);
            TxtApiToken.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_KEY, "");
            CmbApiMode.Text = ServiceContractPhotocopier.Data.PumsConfig.Get(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_MODE,
                ServiceContractPhotocopier.Data.PumsConfig.METER_API_MODE_MOCK);
            SpnApiTimeout.Value = ServiceContractPhotocopier.Data.PumsConfig.GetInt(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_TIMEOUT_MS,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_API_TIMEOUT_MS);
        }

        private void CmbApiProfile_Changed(object sender, EventArgs e)
        {
            if (_apiLoading) return;
            LoadApiProfileFields((CmbApiProfile.Text ?? "").Trim());
        }

        private bool LoadApiProfileFields(string profileName)
        {
            if (string.IsNullOrEmpty(profileName)) return false;
            try
            {
                System.Data.DataTable dt = _dbSetting.GetDataTable(
                    "SELECT BaseUrl, Token, Mode, TimeoutMs FROM [dbo].[zSCP2_ApiProfile] WHERE ProfileName = N'" +
                    SQLString(profileName) + "'", false);
                if (dt.Rows.Count == 0) return false;
                System.Data.DataRow r = dt.Rows[0];
                TxtApiBaseUrl.Text = r["BaseUrl"].ToString();
                TxtApiToken.Text = r["Token"].ToString();
                CmbApiMode.Text = r["Mode"].ToString();
                int t; int.TryParse(r["TimeoutMs"].ToString(), out t);
                SpnApiTimeout.Value = t >= 1000 ? t : 15000;
                return true;
            }
            catch { return false; }
        }

        // Upsert the profile row AND copy its values into the live METER_API_* config keys so the
        // API client (LiveMeterReadingApiClient etc.) keeps reading config exactly as before.
        private void SaveApiTab()
        {
            string prof = (CmbApiProfile.Text ?? "").Trim();
            string url = (TxtApiBaseUrl.Text ?? "").Trim().TrimEnd('/');
            string token = (TxtApiToken.Text ?? "").Trim();
            string mode = (CmbApiMode.Text ?? "").Trim().ToUpperInvariant();
            if (mode != ServiceContractPhotocopier.Data.PumsConfig.METER_API_MODE_LIVE)
                mode = ServiceContractPhotocopier.Data.PumsConfig.METER_API_MODE_MOCK;
            int timeout = (int)SpnApiTimeout.Value;

            if (prof.Length > 0) UpsertApiProfile(prof);

            ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_BASE_URL, url);
            ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_KEY, token);
            ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_MODE, mode);
            ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_TIMEOUT_MS, timeout.ToString());
            ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_API_ACTIVE_PROFILE, prof);
        }

        // "New" — ask for a name, create the profile from the CURRENT field values, select it.
        private void OnApiNewProfile(object sender, EventArgs e)
        {
            string name = XtraInputBox.Show("New API profile name:", "New Profile", "");
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;
            _apiLoading = true;
            CmbApiProfile.Text = name;
            _apiLoading = false;
            UpsertApiProfile(name);
            if (!CmbApiProfile.Properties.Items.Contains(name)) CmbApiProfile.Properties.Items.Add(name);
            XtraMessageBox.Show("Profile '" + name + "' created. Adjust the fields and press Save Profile,\r\n" +
                "or press the toolbar Save to also make it the active connection.",
                "New Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // "Save Profile" — store the current fields under the selected profile (does NOT activate it).
        private void OnApiSaveProfile(object sender, EventArgs e)
        {
            string prof = (CmbApiProfile.Text ?? "").Trim();
            if (prof.Length == 0)
            { XtraMessageBox.Show("Pick or create a profile first.", "Save Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                UpsertApiProfile(prof);
                if (!CmbApiProfile.Properties.Items.Contains(prof)) CmbApiProfile.Properties.Items.Add(prof);
                XtraMessageBox.Show("Profile '" + prof + "' saved.", "Save Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { XtraMessageBox.Show("Save profile failed:\r\n" + ex.Message, "Error"); }
        }

        private void UpsertApiProfile(string prof)
        {
            string url = (TxtApiBaseUrl.Text ?? "").Trim().TrimEnd('/');
            string token = (TxtApiToken.Text ?? "").Trim();
            string mode = (CmbApiMode.Text ?? "").Trim().ToUpperInvariant();
            if (mode != ServiceContractPhotocopier.Data.PumsConfig.METER_API_MODE_LIVE)
                mode = ServiceContractPhotocopier.Data.PumsConfig.METER_API_MODE_MOCK;
            int timeout = (int)SpnApiTimeout.Value;
            _dbSetting.ExecuteNonQuery(
                "MERGE [dbo].[zSCP2_ApiProfile] AS t USING (SELECT N'" + SQLString(prof) + "' AS ProfileName) AS s " +
                "ON t.ProfileName = s.ProfileName " +
                "WHEN MATCHED THEN UPDATE SET BaseUrl=N'" + SQLString(url) + "', Token=N'" + SQLString(token) +
                "', Mode=N'" + SQLString(mode) + "', TimeoutMs=" + timeout + ", LastModified=GETDATE() " +
                "WHEN NOT MATCHED THEN INSERT (ProfileName, BaseUrl, Token, Mode, TimeoutMs) VALUES " +
                "(N'" + SQLString(prof) + "', N'" + SQLString(url) + "', N'" + SQLString(token) + "', N'" +
                SQLString(mode) + "', " + timeout + ");");
        }

        private void OnApiDeleteProfile(object sender, EventArgs e)
        {
            string prof = (CmbApiProfile.Text ?? "").Trim();
            if (prof.Length == 0) return;
            if (XtraMessageBox.Show("Delete API profile '" + prof + "'?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                _dbSetting.ExecuteNonQuery(
                    "DELETE FROM [dbo].[zSCP2_ApiProfile] WHERE ProfileName = N'" + SQLString(prof) + "'");
                CmbApiProfile.Properties.Items.Remove(prof);
                CmbApiProfile.Text = "";
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
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

        // Renumber-on-rename only means anything while the numbers follow the contract at all.
        private void OnItemNoFromContractChanged(object sender, EventArgs e)
        {
            ChkItemNoRenumber.Enabled = ChkItemNoFromContract.Checked;
            if (!ChkItemNoFromContract.Checked) ChkItemNoRenumber.Checked = false;
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
                // Meter invoice numbering lives in PumsConfig (the meter subsystem's config store).
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_INVOICE_DOCNO_FORMAT,
                    (CmbMeterInvFormat.Text ?? "").Trim());
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_CN_DOCNO_FORMAT,
                    (CmbCnFormat.Text ?? "").Trim());
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_DESC_FROM_ITEM,
                    ChkInvDescFromItem.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_LEGACY_DATA_BLOCK,
                    ChkLegacyDataBlock.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST_ON,
                    ChkWhitelistOn.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_EMAIL_WHITELIST,
                    MemoWhitelist.Text ?? "");
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ADVANCED,
                    ChkInvFmtAdvanced.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ONLINE,
                    (CmbInvFmtOnline.Text ?? "").Trim());
                ServiceContractPhotocopier.Data.PumsConfig.Set(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_OFFLINE,
                    (CmbInvFmtOffline.Text ?? "").Trim());
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_REF_FROM_CONTRACT,
                    ChkItemRefFromContract.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_NO_FROM_CONTRACT,
                    ChkItemNoFromContract.Checked);
                ServiceContractPhotocopier.Data.PumsConfig.SetBool(_dbSetting,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_ITEM_NO_FOLLOW_CONTRACT_RENAME,
                    ChkItemNoFromContract.Checked && ChkItemNoRenumber.Checked);

                // API tab: upsert the selected profile and make it the active connection.
                SaveApiTab();

                XtraMessageBox.Show("Plugin options saved.", "Plugin Option", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
