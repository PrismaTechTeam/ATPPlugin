using System;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Meter Reading settings, in four groups: what the list SHOWS (expired / inactive / late
    /// readings), AUTO-FETCH and its cutoff, the INVOICING rules (grouping, the completeness guard,
    /// rental grouping), and MAINTENANCE — clearing this period's staged readings.
    ///
    /// Everything persists to Z_PumsConfig via PumsConfig; nothing is written until OK.
    /// </summary>
    public partial class MeterReadingSetting_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private int _year, _month;   // billing period selected on the Meter Reading screen

        public MeterReadingSetting_Form()
        {
            InitializeComponent();
        }

        public MeterReadingSetting_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            _year = DateTime.Today.Year;
            _month = DateTime.Today.Month;
            if (_dbSetting != null)
            {
                this.ChkIncludeExpired.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS, PumsConfig.DEFAULT_INCLUDE_EXPIRED_ITEMS);
                this.ChkIncludeInactive.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_INCLUDE_INACTIVE, PumsConfig.DEFAULT_INCLUDE_INACTIVE);
                this.ChkIncludeLate.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_INCLUDE_LATE_READINGS, PumsConfig.DEFAULT_INCLUDE_LATE_READINGS);
                this.ChkAutoFetch.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_AUTO_FETCH_ENABLED, PumsConfig.DEFAULT_AUTO_FETCH_ENABLED);
                string cutDay = PumsConfig.Get(_dbSetting, PumsConfig.KEY_AUTO_FETCH_CUTOFF_DAY, PumsConfig.DEFAULT_AUTO_FETCH_CUTOFF_DAY);
                this.CmbCutoffDay.SelectedIndex = string.Equals(cutDay, "ON", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                string cutTime = PumsConfig.Get(_dbSetting, PumsConfig.KEY_AUTO_FETCH_CUTOFF, PumsConfig.DEFAULT_AUTO_FETCH_CUTOFF);
                TimeSpan t;
                if (!TimeSpan.TryParse(cutTime, out t)) t = new TimeSpan(23, 59, 0);
                this.TimeCutoff.EditValue = new DateTime(2000, 1, 1).Add(t);
                string grouping = PumsConfig.Get(_dbSetting, PumsConfig.KEY_METER_GROUPING, PumsConfig.DEFAULT_METER_GROUPING).Trim().ToUpperInvariant();
                this.CmbGrouping.SelectedIndex = grouping == PumsConfig.METER_GROUPING_DEBTOR ? 1
                    : grouping == PumsConfig.METER_GROUPING_MACHINE ? 2 : 0;
                this.ChkGroupGuard.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_METER_GROUP_GUARD, PumsConfig.DEFAULT_METER_GROUP_GUARD);
                this.ChkGroupRental.Checked =
                    PumsConfig.GetBool(_dbSetting, PumsConfig.KEY_GROUP_RENTAL_BY_METER, PumsConfig.DEFAULT_GROUP_RENTAL_BY_METER);
            }
        }

        /// <summary>Full form: the caller passes the billing period currently selected on the Meter
        /// Reading screen — the Clear Staged Readings button operates on THAT period only.</summary>
        public MeterReadingSetting_Form(DBSetting dbSetting, int year, int month) : this(dbSetting)
        {
            _year = year;
            _month = month;
        }

        /// <summary>True if the user wants expired service items shown in the meter list.</summary>
        public bool IncludeExpired { get { return this.ChkIncludeExpired.Checked; } }

        // ===== DANGER ZONE: clear the period's staged readings (back to before-Fetch) =====
        // Protections: invoiced rows are NEVER touched; LOCKED snapshot rows are NEVER touched;
        // a count preview + explicit warning + typed "CLEAR" confirmation are all required; every
        // deleted reading is appended to the immutable audit log as CLEARED first.
        private void BtnClearStaging_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            string period = new System.Globalization.CultureInfo("en-US").DateTimeFormat.GetMonthName(_month) + " " + _year;
            try
            {
                System.Data.DataTable c = _dbSetting.GetDataTable(
                    "SELECT " +
                    "SUM(CASE WHEN InvoicedDocKey IS NOT NULL THEN 1 ELSE 0 END) AS Invoiced, " +
                    "SUM(CASE WHEN InvoicedDocKey IS NULL AND LockedAt IS NOT NULL THEN 1 ELSE 0 END) AS Locked, " +
                    "SUM(CASE WHEN InvoicedDocKey IS NULL AND LockedAt IS NULL THEN 1 ELSE 0 END) AS Clearable " +
                    "FROM dbo.zSCP2_MeterEntry WHERE PeriodYear=" + _year + " AND PeriodMonth=" + _month, false);
                int invoiced = c.Rows[0]["Invoiced"] == DBNull.Value ? 0 : Convert.ToInt32(c.Rows[0]["Invoiced"]);
                int locked = c.Rows[0]["Locked"] == DBNull.Value ? 0 : Convert.ToInt32(c.Rows[0]["Locked"]);
                int clearable = c.Rows[0]["Clearable"] == DBNull.Value ? 0 : Convert.ToInt32(c.Rows[0]["Clearable"]);

                if (clearable == 0)
                {
                    XtraMessageBox.Show("Nothing to clear for " + period + ".\r\n" +
                        (invoiced > 0 ? invoiced + " invoiced reading(s) are protected.\r\n" : "") +
                        (locked > 0 ? locked + " LOCKED snapshot reading(s) are protected." : ""),
                        "Clear Staged Readings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (XtraMessageBox.Show(
                        "This will DELETE " + clearable + " staged reading(s) for " + period + " —\r\n" +
                        "the screen goes back to its BEFORE-FETCH state (readings must be fetched or keyed in again).\r\n\r\n" +
                        "PROTECTED (never touched):\r\n" +
                        "   • " + invoiced + " invoiced reading(s)\r\n" +
                        "   • " + locked + " LOCKED auto-fetch snapshot reading(s)\r\n\r\n" +
                        "Every deleted reading is recorded in the audit log first.\r\n\r\nContinue?",
                        "Clear Staged Readings — " + period,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                string typed = XtraInputBox.Show(
                    "FINAL CONFIRMATION\r\n\r\nType   CLEAR   (in capital letters) to delete " +
                    clearable + " staged reading(s) for " + period + ":",
                    "Clear Staged Readings", "");
                if (!string.Equals(typed, "CLEAR", StringComparison.Ordinal))
                {
                    if (typed != null && typed.Length > 0)
                        XtraMessageBox.Show("Confirmation text did not match — nothing was deleted.",
                            "Clear Staged Readings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string who = "";
                try { who = (AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? "").Replace("'", "''"); }
                catch { }
                // Audit BEFORE deleting, then delete — un-invoiced, un-locked rows only.
                _dbSetting.ExecuteNonQuery(
                    "INSERT INTO dbo.zSCP2_MeterReadingLog (ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, Source, DocNo, CreatedBy) " +
                    "SELECT ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, 'CLEARED', '', N'" + who + "' " +
                    "FROM dbo.zSCP2_MeterEntry " +
                    "WHERE PeriodYear=" + _year + " AND PeriodMonth=" + _month + " AND InvoicedDocKey IS NULL AND LockedAt IS NULL");
                _dbSetting.ExecuteNonQuery(
                    "DELETE FROM dbo.zSCP2_MeterEntry " +
                    "WHERE PeriodYear=" + _year + " AND PeriodMonth=" + _month + " AND InvoicedDocKey IS NULL AND LockedAt IS NULL");

                XtraMessageBox.Show(clearable + " staged reading(s) cleared for " + period + ".\r\n" +
                    "The list will reload — Fetch again for a fresh round.",
                    "Clear Staged Readings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;   // caller reloads the meter list
                this.Close();
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Clear failed:\r\n" + ex.Message, "Clear Staged Readings", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_dbSetting != null)
            {
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_INCLUDE_EXPIRED_ITEMS, this.ChkIncludeExpired.Checked);
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_INCLUDE_INACTIVE, this.ChkIncludeInactive.Checked);
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_INCLUDE_LATE_READINGS, this.ChkIncludeLate.Checked);
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_AUTO_FETCH_ENABLED, this.ChkAutoFetch.Checked);
                PumsConfig.Set(_dbSetting, PumsConfig.KEY_AUTO_FETCH_CUTOFF_DAY,
                    this.CmbCutoffDay.SelectedIndex == 1 ? "ON" : "BEFORE");
                DateTime tv = this.TimeCutoff.EditValue is DateTime ? (DateTime)this.TimeCutoff.EditValue : new DateTime(2000, 1, 1, 23, 59, 0);
                PumsConfig.Set(_dbSetting, PumsConfig.KEY_AUTO_FETCH_CUTOFF, tv.ToString("HH:mm"));
                PumsConfig.Set(_dbSetting, PumsConfig.KEY_METER_GROUPING,
                    this.CmbGrouping.SelectedIndex == 1 ? PumsConfig.METER_GROUPING_DEBTOR
                    : this.CmbGrouping.SelectedIndex == 2 ? PumsConfig.METER_GROUPING_MACHINE
                    : PumsConfig.METER_GROUPING_FOLLOW);
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_METER_GROUP_GUARD, this.ChkGroupGuard.Checked);
                PumsConfig.SetBool(_dbSetting, PumsConfig.KEY_GROUP_RENTAL_BY_METER, this.ChkGroupRental.Checked);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
