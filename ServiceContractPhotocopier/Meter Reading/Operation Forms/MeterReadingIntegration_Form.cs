using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Meter Reading Integration", MenuOrder = 450)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class MeterReadingIntegration_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;

        public MeterReadingIntegration_Form()
        {
            InitializeComponent();
            InitDefaults();
            LoadData();
        }

        public MeterReadingIntegration_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
        }

        public MeterReadingIntegration_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
        }

        private void InitDefaults()
        {
            this.LblToday.Text = DateTime.Today.ToString("dd/MM/yyyy (ddd)");
            this.ChkShowAll.Checked = true;

            // Populate Month combobox with January..December and select the current month.
            this.CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                this.CmbMonth.Properties.Items.Add(new System.Globalization.CultureInfo("en-US")
                    .DateTimeFormat.GetMonthName(m));
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            // Populate Day combobox with 1..31 and select today.
            this.CmbDay.Properties.Items.Clear();
            for (int d = 1; d <= 31; d++) this.CmbDay.Properties.Items.Add(d);
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;
        }

        // ---------- Refresh ----------

        private void RefreshTimer_Tick(object sender, EventArgs e) { /* timer disabled */ }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        // ---------- Stubs (UI shell only) ----------

        private void LoadData()
        {
            // Empty shell DataTable so the grid renders its column headers immediately.
            // Real data binding will replace this once the meter-reading source is wired.
            DataTable dt = new DataTable();
            dt.Columns.Add("Selected",       typeof(bool));
            dt.Columns.Add("ReadingId",      typeof(string));
            dt.Columns.Add("ReadingDate",    typeof(DateTime));
            dt.Columns.Add("MachineNo",      typeof(string));
            dt.Columns.Add("SerialNo",       typeof(string));
            dt.Columns.Add("Customer",       typeof(string));
            dt.Columns.Add("Technician",     typeof(string));
            dt.Columns.Add("MeterTypeBlack", typeof(string));
            dt.Columns.Add("ReadingBlack",   typeof(decimal));
            dt.Columns.Add("MeterTypeColor", typeof(string));
            dt.Columns.Add("ReadingColor",   typeof(decimal));
            dt.Columns.Add("Status",         typeof(string));
            dt.Columns.Add("ReceivedAt",     typeof(DateTime));
            this.GridMeter.DataSource = dt;
            this.GridViewMeter.BestFitColumns();
        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            this.TxtSearch.EditValue = null;
            this.ChkShowAll.Checked  = true;
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            this.CmbDay.SelectedIndex   = DateTime.Today.Day - 1;
            LoadData();
        }

        private void BtnFetch_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(this,
                "Fetch from Pump System is not wired up yet.",
                "Meter Reading Integration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSelfManualKeyIn_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(this,
                "Self Manual Key In is not wired up yet.",
                "Meter Reading Integration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show(this,
                "Generate Invoice is not wired up yet.",
                "Meter Reading Integration", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
