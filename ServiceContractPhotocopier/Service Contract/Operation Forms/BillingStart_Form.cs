using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// The way in for a contract that was already running before this system: the back door the
    /// ordinary rules leave open on purpose.
    ///
    /// <para>The run refuses to bill a month while an earlier one of the same contract has never
    /// been invoiced, and refuses to delete a month while a later one stands. Both are right for a
    /// contract this book has billed from the start, and both are wrong for one carried over from an
    /// old system, where the earlier months were billed somewhere else and this book has no record
    /// of them. Here the clerk says where this book takes over.</para>
    ///
    /// <para>Three things are written, and nothing else:
    /// <list type="bullet">
    /// <item>zSCP2_Contract.BillFromPeriod — the first month this book bills. Months before it are
    /// never late and never hold the next one back.</item>
    /// <item>zSCP2_ItemMeter.InitialReading — where each counter stood when this book took over, so
    /// the first invoice charges the copies of ONE month and not of the machine's whole life.</item>
    /// <item>one zSCP2_MeterEntry for the month BEFORE that, carrying the closing reading and the old
    /// system's invoice number. It has no DocKey, because that invoice is not in this book; it is
    /// the record of which document covered that month, and it is what makes the month read as
    /// settled.</item>
    /// </list></para>
    /// </summary>
    // Not on the menu: it belongs to ONE contract, and is opened from that contract's Billing
    // History tab beside "Open CN...".
    public partial class BillingStart_Form : XtraForm
    {
        private DBSetting _db;
        private UserSession _userSession;
        private DataTable _meters;
        private bool _loading;

        public BillingStart_Form(UserSession userSession, DBSetting dbSetting)
            : this(userSession, dbSetting, 0L)
        {
        }

        /// <summary>Opened from a contract's Billing History: that contract, and only that one.</summary>
        public BillingStart_Form(UserSession userSession, DBSetting dbSetting, long contractKey)
        {
            InitializeComponent();
            _userSession = userSession;
            _db = dbSetting;
            Wire();
            Boot();
            if (contractKey > 0)
            {
                _loading = true;
                this.LkContract.EditValue = contractKey;
                _loading = false;
                this.LkContract.Properties.ReadOnly = true;
                LoadMeters();
            }
        }

        private void Wire()
        {
            this.LkContract.EditValueChanged += new EventHandler(Contract_Changed);
            this.BtnFillDown.Click += new EventHandler(BtnFillDown_Click);
            this.BtnApply.Click += new EventHandler(BtnApply_Click);
            this.BtnClose.Click += new EventHandler(BtnClose_Click);
        }

        private void Boot()
        {
            for (int m = 1; m <= 12; m++)
                this.CmbFromMonth.Properties.Items.Add(
                    new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            this.CmbFromMonth.SelectedIndex = DateTime.Today.Month - 1;
            this.SpnFromYear.EditValue = (decimal)DateTime.Today.Year;
            LoadContracts();
            Say("");
        }

        // ───────────────────────────── the contract ─────────────────────────────

        private void LoadContracts()
        {
            try
            {
                DataTable t = _db.GetDataTable(
                    "SELECT c.ContractKey, RTRIM(c.ContractNo) AS ContractNo, " +
                    "       ISNULL(d.CompanyName, c.DebtorCode) AS Customer, c.ServiceStartDate, " +
                    "       RTRIM(c.ContractNo) + '  ·  ' + ISNULL(d.CompanyName, c.DebtorCode) AS Label " +
                    "  FROM dbo.zSCP2_Contract c " +
                    "  LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    " WHERE ISNULL(c.Inactive,'N') = 'N' " +
                    " ORDER BY c.ContractNo", false);
                this.LkContract.Properties.DataSource = t;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not read the contracts:\r\n" + ex.Message, this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private long ContractKey()
        {
            object v = this.LkContract.EditValue;
            return v == null || v == DBNull.Value ? 0L : Convert.ToInt64(v);
        }

        private void Contract_Changed(object sender, EventArgs e)
        {
            if (_loading) return;
            LoadMeters();
        }

        /// <summary>Every counter on the contract, with whatever this book already holds for it.</summary>
        private void LoadMeters()
        {
            long ck = ContractKey();
            _meters = new DataTable();
            _meters.Columns.Add("ItemMeterKey", typeof(long));
            _meters.Columns.Add("ServiceItemNo", typeof(string));
            _meters.Columns.Add("SerialNo", typeof(string));
            _meters.Columns.Add("MeterTypeCode", typeof(string));
            _meters.Columns.Add("MeterTypeName", typeof(string));
            _meters.Columns.Add("Closing", typeof(decimal));
            _meters.Columns.Add("OldInvoice", typeof(string));
            if (ck <= 0) { this.GridMeters.DataSource = _meters; Say(""); return; }

            try
            {
                DataTable t = _db.GetDataTable(
                    "SELECT m.ItemMeterKey, RTRIM(i.ServiceItemNo) AS ServiceItemNo, " +
                    "       RTRIM(COALESCE(NULLIF(m.MachineSerialNo,''), i.SerialNumber, '')) AS SerialNo, " +
                    "       RTRIM(m.MeterTypeCode) AS MeterTypeCode, " +
                    "       RTRIM(ISNULL(mt.Description,'')) AS MeterTypeName, " +
                    "       ISNULL(m.InitialReading,0) AS InitialReading " +
                    "  FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "  LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                    " WHERE i.ContractKey = " + ck + " " +
                    " ORDER BY i.ServiceItemNo, m.MeterTypeCode", false);
                foreach (DataRow r in t.Rows)
                {
                    DataRow n = _meters.NewRow();
                    n["ItemMeterKey"] = Convert.ToInt64(r["ItemMeterKey"]);
                    n["ServiceItemNo"] = Convert.ToString(r["ServiceItemNo"]);
                    n["SerialNo"] = Convert.ToString(r["SerialNo"]);
                    n["MeterTypeCode"] = Convert.ToString(r["MeterTypeCode"]);
                    n["MeterTypeName"] = Convert.ToString(r["MeterTypeName"]);
                    n["Closing"] = Convert.ToDecimal(r["InitialReading"]);
                    n["OldInvoice"] = "";
                    _meters.Rows.Add(n);
                }
                this.GridMeters.DataSource = _meters;

                // Where this book already thinks the contract starts, so re-opening the screen shows
                // what was set rather than today.
                int from = ScpBillingSequence.BillFromOf(_db, ck);
                _loading = true;
                if (from > 0)
                {
                    this.CmbFromMonth.SelectedIndex = (from % 100) - 1;
                    this.SpnFromYear.EditValue = (decimal)(from / 100);
                }
                _loading = false;
                Say(_meters.Rows.Count + (_meters.Rows.Count == 1 ? " counter" : " counters") +
                    (from > 0 ? "   ·   bills here from " + PeriodName(from) : "   ·   no start set yet"));
            }
            catch (Exception ex)
            {
                _loading = false;
                XtraMessageBox.Show("Could not read the counters:\r\n" + ex.Message, this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ───────────────────────────── the period ─────────────────────────────

        private int FromYear()
        {
            object v = this.SpnFromYear.EditValue;
            return v == null || v == DBNull.Value ? DateTime.Today.Year : Convert.ToInt32(v);
        }

        private int FromMonth()
        {
            return this.CmbFromMonth.SelectedIndex < 0 ? DateTime.Today.Month : this.CmbFromMonth.SelectedIndex + 1;
        }

        private int FromPeriod() { return FromYear() * 100 + FromMonth(); }

        private static string PeriodName(int period)
        {
            if (period <= 0) return "";
            return new CultureInfo("en-US").DateTimeFormat.GetAbbreviatedMonthName(period % 100) +
                   " " + (period / 100);
        }

        private void Say(string what)
        {
            this.LblFoot.Text = what;
        }

        // ───────────────────────────── the writes ─────────────────────────────

        private void BtnFillDown_Click(object sender, EventArgs e)
        {
            string no = Convert.ToString(this.TxtOldInvoice.EditValue).Trim();
            if (_meters == null || _meters.Rows.Count == 0) return;
            this.GridViewMeters.CloseEditor();
            foreach (DataRow r in _meters.Rows) r["OldInvoice"] = no;
            this.GridViewMeters.RefreshData();
        }

        private void BtnClose_Click(object sender, EventArgs e) { this.Close(); }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            long ck = ContractKey();
            if (ck <= 0)
            {
                XtraMessageBox.Show("Pick a contract first.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.GridViewMeters.CloseEditor();
            int period = FromPeriod();
            DateTime before = new DateTime(FromYear(), FromMonth(), 1).AddMonths(-1);

            int withReading = 0, withInvoice = 0;
            foreach (DataRow r in _meters.Rows)
            {
                if (Convert.ToDecimal(r["Closing"]) > 0m) withReading++;
                if (Convert.ToString(r["OldInvoice"]).Trim().Length > 0) withInvoice++;
            }

            string ask =
                "This contract bills here from " + PeriodName(period) + " onward." + Environment.NewLine +
                Environment.NewLine +
                "Everything before that is treated as settled elsewhere: those months are never late, " +
                "and they will not hold up " + PeriodName(period) + "." + Environment.NewLine +
                Environment.NewLine +
                withReading + " of " + _meters.Rows.Count + " counters carry a closing reading — that becomes " +
                "where each one stood when this book took over, so the first invoice charges one month's " +
                "copies." + Environment.NewLine;
            if (withInvoice > 0)
                ask += withInvoice + " carry the old system's invoice number, recorded against " +
                       before.ToString("MMM yyyy", CultureInfo.InvariantCulture) + "." + Environment.NewLine;
            ask += Environment.NewLine + "Apply?";

            if (XtraMessageBox.Show(ask, this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ScpBillingSequence.SetBillFrom(_db, ck, period);
                foreach (DataRow r in _meters.Rows)
                {
                    long imk = Convert.ToInt64(r["ItemMeterKey"]);
                    decimal closing = Convert.ToDecimal(r["Closing"]);
                    string oldNo = Convert.ToString(r["OldInvoice"]).Trim();
                    WriteOpening(imk, closing, oldNo, before);
                }
            }
            catch (Exception ex)
            {
                this.Cursor = old;
                XtraMessageBox.Show("Could not write it:\r\n" + ex.Message, this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.Cursor = old;
            Say("Saved — bills here from " + PeriodName(period) + ".");
            XtraMessageBox.Show(
                "Done. " + PeriodName(period) + " is now the first month this book bills for this contract." +
                Environment.NewLine + Environment.NewLine +
                "Meter Invoice Run will stop calling the earlier months overdue, and will let " +
                PeriodName(period) + " go out.",
                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Where the counter stood when this book took over, and which document covered the
        /// month before. The entry carries no DocKey: that invoice lives in the old system, and a key
        /// pointing at nothing in dbo.IV would read as a deleted invoice.</summary>
        private void WriteOpening(long itemMeterKey, decimal closing, string oldInvoiceNo, DateTime before)
        {
            if (closing > 0m)
                _db.ExecuteNonQuery(
                    "UPDATE dbo.zSCP2_ItemMeter SET InitialReading = " + closing.ToString(CultureInfo.InvariantCulture) +
                    ", LastModified = GETDATE() WHERE ItemMeterKey = " + itemMeterKey);

            if (oldInvoiceNo.Length == 0) return;
            string no = oldInvoiceNo.Replace("'", "''");
            _db.ExecuteNonQuery(
                "IF EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey = " + itemMeterKey +
                "           AND PeriodYear = " + before.Year + " AND PeriodMonth = " + before.Month + ") " +
                "  UPDATE dbo.zSCP2_MeterEntry " +
                "     SET CurrentReading = " + closing.ToString(CultureInfo.InvariantCulture) + ", " +
                "         InvoicedDocNo = N'" + no + "', Invoiced = 'Y', InvoicedAt = GETDATE(), " +
                "         Source = 'OPENING', LastModified = GETDATE() " +
                "   WHERE ItemMeterKey = " + itemMeterKey +
                "     AND PeriodYear = " + before.Year + " AND PeriodMonth = " + before.Month + " " +
                "ELSE " +
                "  INSERT INTO dbo.zSCP2_MeterEntry " +
                "    (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, " +
                "     Invoiced, InvoicedDocNo, InvoicedAt, StrategyCode, TrackingId, LastModified) " +
                "  VALUES (" + itemMeterKey + ", " + before.Year + ", " + before.Month + ", " +
                closing.ToString(CultureInfo.InvariantCulture) + ", EOMONTH(DATEFROMPARTS(" +
                before.Year + "," + before.Month + ",1)), 'OPENING', 'Y', N'" + no + "', GETDATE(), '', '', GETDATE())");
        }
    }
}
