using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Reading History for ONE machine — the immutable zSCP2_MeterReadingLog trail that explains
    /// how each invoice amount was produced. INVOICE rows for the invoice the user came from are
    /// highlighted green; INVOICE-DELETED rows show light red.
    /// </summary>
    public partial class MeterReadingHistory_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly string _highlightDocNo;

        public MeterReadingHistory_Form()
        {
            InitializeComponent();
        }

        public MeterReadingHistory_Form(DBSetting db, long itemKey, string serviceItemNo,
            string serial, string highlightDocNo) : this()
        {
            _db = db;
            _highlightDocNo = (highlightDocNo ?? "").Trim();
            // Solid green header (the skin's gradient washes the white title out otherwise).
            this.PanelTop.LookAndFeel.UseDefaultLookAndFeel = false;
            this.PanelTop.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            this.LblTitle.Text = "Reading History   " + serviceItemNo +
                (string.IsNullOrEmpty(serial) ? "" : "      S/N: " + serial);
            this.GridViewHist.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewHist_RowStyle);
            LoadHistory(itemKey);
        }

        private void LoadHistory(long itemKey)
        {
            try
            {
                DataTable dt = _db.GetDataTable(
                    "SELECT im.MeterTypeCode, im.MeterRole, " +
                    "RIGHT('0' + CAST(l.PeriodMonth AS VARCHAR(2)), 2) + '/' + CAST(l.PeriodYear AS VARCHAR(4)) AS Period, " +
                    "l.LastReading, l.Reading, l.[Usage], l.UnitPrice, l.MinCharges, l.FOCQty, l.RebatePct, l.Charge, " +
                    "l.ReadingDate, l.Source, l.DocNo, l.CreatedAt, l.CreatedBy " +
                    "FROM dbo.zSCP2_MeterReadingLog l " +
                    "JOIN dbo.zSCP2_ItemMeter im ON im.ItemMeterKey = l.ItemMeterKey " +
                    "WHERE im.ItemKey = " + itemKey + " " +
                    "ORDER BY l.CreatedAt DESC, l.LogKey DESC", false);
                this.GridHist.DataSource = dt;
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Load history failed:\r\n" + ex.Message, "Reading History"); }
        }

        // Green = the INVOICE rows behind the invoice the user double-clicked from (or any invoice
        // row when none was passed); light red = readings rolled back by an invoice deletion.
        private void GridViewHist_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            string src = Convert.ToString(this.GridViewHist.GetRowCellValue(e.RowHandle, "Source") ?? "");
            string doc = Convert.ToString(this.GridViewHist.GetRowCellValue(e.RowHandle, "DocNo") ?? "").Trim();
            if (src == "INVOICE" && (_highlightDocNo.Length == 0 || string.Equals(doc, _highlightDocNo, StringComparison.OrdinalIgnoreCase)))
            {
                e.Appearance.BackColor = Color.FromArgb(200, 230, 201);
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.FontStyleDelta = FontStyle.Bold;
            }
            else if (src == "INVOICE-DELETED")
            {
                e.Appearance.BackColor = Color.FromArgb(255, 224, 224);
                e.Appearance.Options.UseBackColor = true;
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
