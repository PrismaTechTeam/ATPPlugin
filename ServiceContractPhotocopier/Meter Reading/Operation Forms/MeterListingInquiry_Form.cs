using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Inquiry + report front end for the customer's Appendix A, "Summary sales invoice meter
    /// listing": filter a billing month, see one row per CSSI with the readings, free copies, rates
    /// and charges behind that month's invoice, then print it through AutoCount's own report engine.
    ///
    /// Report Design opens AutoCount's REAL designer bound to this data, so the layout is a normal
    /// AutoCount report design saved in dbo.Report — the same designs the contract's "Listing
    /// Template" picker offers and the bulk email prints.
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Summary Sales Invoice Meter Listing", MenuOrder = 460, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MeterListingInquiry_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;
        private DataTable _data;

        public MeterListingInquiry_Form()
        {
            InitializeComponent();
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
        }

        public MeterListingInquiry_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            _userSession = UserSession.CurrentUserSession;
            InitDefaults();
        }

        public MeterListingInquiry_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            _dbSetting = userSession != null ? userSession.DBSetting : null;
            InitDefaults();
        }

        private void InitDefaults()
        {
            CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                CmbMonth.Properties.Items.Add(new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            CmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            CmbYear.Properties.Items.Clear();
            int y = DateTime.Today.Year;
            for (int i = y - 3; i <= y + 1; i++) CmbYear.Properties.Items.Add(i);
            CmbYear.SelectedItem = y;

            _data = ScpMeterListing.NewTable();
            GridListing.DataSource = _data;
            ConfigureGrid();
        }

        private int SelectedMonth() { return CmbMonth.SelectedIndex < 0 ? DateTime.Today.Month : CmbMonth.SelectedIndex + 1; }

        private int SelectedYear()
        {
            int y;
            return CmbYear.SelectedItem != null && int.TryParse(Convert.ToString(CmbYear.SelectedItem), out y)
                ? y : DateTime.Today.Year;
        }

        // ───────────────────── grid ─────────────────────

        private void ConfigureGrid()
        {
            GridViewListing.Columns.Clear();
            GridViewListing.PopulateColumns();
            // Appendix A's own column order and captions — the customer reads this side by side with
            // their spreadsheet, so the wording matches theirs, not our field names.
            Col("CSSI", "CSSI", 110, 0);
            Col("Status", "Status", 70, 1);
            Col("Branch", "Branch", 170, 2);
            Col("Model", "Model", 120, 3);
            Col("SerialNo", "Serial No.", 100, 4);
            Col("ServiceVoucher", "Service Voucher", 120, 5);
            Date("CurrentServiceDate", "Current Service Date", 110, 6);
            Num("CurrentTotalBK", "Current TOTAL BK", 110, 7, "n0");
            Num("CurrentTotalCL", "Current TOTAL CL", 110, 8, "n0");
            Date("PreviousServiceDate", "Previous Service Date", 115, 9);
            Num("PreviousTotalBK", "Previous TOTAL BK", 115, 10, "n0");
            Num("PreviousTotalCL", "Previous TOTAL CL", 115, 11, "n0");
            Num("FOCBK", "FOC BK", 70, 12, "n0");
            Num("FOCCL", "FOC CL", 70, 13, "n0");
            Num("RateBK", "Rate of BK", 80, 14, "n4");
            Num("RateCL", "Rate of CL", 80, 15, "n4");
            Num("FixedRebateBKQty", "Fixed Rebate BK QTY", 120, 16, "n0");
            Num("FixedRebateCLQty", "Fixed Rebate CL QTY", 120, 17, "n0");
            Num("NetBK", "NET BK", 80, 18, "n0");
            Num("NetCL", "NET CL", 80, 19, "n0");
            Num("BKCharges", "BK CHARGES", 95, 20, "n2");
            Num("CLCharges", "CL CHARGES", 95, 21, "n2");
            Num("RentalAmt", "Rental Amt", 90, 22, "n2");
            Num("GrandTotal", "GRAND TOTAL", 100, 23, "n2");
            Col("InvNo", "INV NO.", 110, 24);
            // Context columns: available for grouping and for the report's title block, off by default.
            Hide("DebtorCode"); Hide("CustomerName"); Hide("ContractNo");
            Hide("PeriodYear"); Hide("PeriodMonth"); Hide("MonthEnd"); Hide("IsCombineRow");

            Total("NetBK", "n0"); Total("NetCL", "n0");
            Total("BKCharges", "n2"); Total("CLCharges", "n2");
            Total("RentalAmt", "n2"); Total("GrandTotal", "n2");

            GridViewListing.RowStyle -= new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewListing_RowStyle);
            GridViewListing.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewListing_RowStyle);
        }

        // The ".C COMBINE" row closes each contract and carries the money — bold it so the eye lands
        // on the same line the customer's own sheet underlines.
        private void GridViewListing_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            object v = GridViewListing.GetRowCellValue(e.RowHandle, "IsCombineRow");
            if (v == null || v == DBNull.Value || !Convert.ToBoolean(v)) return;
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 249, 196);
            e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;
        }

        private void Col(string field, string caption, int width, int visibleIndex)
        {
            GridColumn c = GridViewListing.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = visibleIndex;
        }

        private void Num(string field, string caption, int width, int visibleIndex, string fmt)
        {
            Col(field, caption, width, visibleIndex);
            GridColumn c = GridViewListing.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c.DisplayFormat.FormatString = fmt;
        }

        private void Date(string field, string caption, int width, int visibleIndex)
        {
            Col(field, caption, width, visibleIndex);
            GridColumn c = GridViewListing.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            c.DisplayFormat.FormatString = "dd/MM/yyyy";
        }

        private void Hide(string field)
        {
            GridColumn c = GridViewListing.Columns[field];
            if (c != null) { c.Visible = false; c.OptionsColumn.ShowInCustomizationForm = true; }
        }

        private void Total(string field, string fmt)
        {
            GridColumn c = GridViewListing.Columns[field];
            if (c == null) return;
            c.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            c.SummaryItem.DisplayFormat = "Σ {0:" + fmt + "}";
        }

        // ───────────────────── actions ─────────────────────

        private void BtnInquiry_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _data = ScpMeterListing.Build(_dbSetting, SelectedYear(), SelectedMonth(),
                    TxtDebtorFrom.Text.Trim(), TxtDebtorTo.Text.Trim(), TxtContract.Text.Trim());
                GridListing.DataSource = _data;
                ConfigureGrid();
                GridViewListing.BestFitColumns();
            }
            finally { Cursor.Current = Cursors.Default; }

            if (_data.Rows.Count == 0)
                XtraMessageBox.Show(
                    "No invoiced meter lines for " + MonthLabel() + ".\r\n\r\n" +
                    "The listing is built from invoices that have already been GENERATED — " +
                    "generate the month's meter invoices first.",
                    "Summary Sales Invoice Meter Listing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            TxtDebtorFrom.Text = ""; TxtDebtorTo.Text = ""; TxtContract.Text = "";
            CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            CmbYear.SelectedItem = DateTime.Today.Year;
            try { GridViewListing.ActiveFilterString = ""; } catch { }
        }

        private string MonthLabel()
        {
            return new CultureInfo("en-US").DateTimeFormat.GetMonthName(SelectedMonth()) + " " + SelectedYear();
        }

        /// <summary>
        /// What the report actually prints: the rows the user can SEE. Any grouping, sorting or
        /// column filter they applied in the grid carries into the printout — that is the whole
        /// point of filtering here before printing.
        /// </summary>
        private DataTable VisibleRows()
        {
            DataTable t = ScpMeterListing.NewTable();
            for (int h = 0; h < GridViewListing.RowCount; h++)
            {
                DataRow src = GridViewListing.GetDataRow(h);
                if (src == null) continue;   // group rows
                t.ImportRow(src);
            }
            if (t.Rows.Count == 0 && _data != null)
                foreach (DataRow r in _data.Rows) t.ImportRow(r);
            return t;
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (!HasRows()) return;
            string name = PickDesign("Preview with which report design?", false);
            if (name == null) return;   // cancelled
            try
            {
                AutoCount.Report.ReportTemplate tpl = LoadTemplate(name, VisibleRows());
                if (tpl == null) return;
                // The DX build shipped with AutoCount 2.2 has neither XtraReport.ShowPreviewDialog()
                // nor ReportPrintTool (both live in assemblies AutoCount does not ship). Build the
                // document and show the printing system's own preview — the same window AutoCount's
                // report screens use.
                tpl.Report.CreateDocument();
                DevExpress.XtraPrinting.PrintingSystem ps = tpl.Report.PrintingSystem as DevExpress.XtraPrinting.PrintingSystem;
                if (ps != null) ps.PreviewFormEx.ShowDialog(this);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Preview failed:\r\n" + ex.Message,
                    "Summary Sales Invoice Meter Listing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDesign_Click(object sender, EventArgs e)
        {
            if (_userSession == null) return;
            // Design against REAL rows where possible: an empty datasource gives the designer no
            // fields to drag, which is the most common "the designer is useless" complaint.
            DataTable ds = HasRows(false) ? VisibleRows() : ScpMeterListing.NewTable();
            string name = PickDesign("Create a new design, or modify which one?", true);
            if (name == null) return;
            try
            {
                AutoCount.Report.ReportTemplate tpl = name.Length == 0
                    ? AutoCount.Report.AutoCountReport.GetInstance().NewReport(ScpMeterListing.REPORT_TYPE, ds, _userSession)
                    : LoadTemplate(name, ds);
                if (tpl == null) return;
                // AutoCount's own designer — Save inside it writes back to dbo.Report under our type,
                // so the design immediately shows up in the contract's Listing Template picker.
                AutoCount.Report.ReportDesigner.DesignReport(tpl, name, _userSession, null);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the report designer:\r\n" + ex.Message,
                    "Report Design", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private AutoCount.Report.ReportTemplate LoadTemplate(string name, DataTable ds)
        {
            if (string.IsNullOrEmpty(name))
                return AutoCount.Report.AutoCountReport.GetInstance().NewReport(ScpMeterListing.REPORT_TYPE, ds, _userSession);
            return AutoCount.Report.AutoCountReport.GetInstance().GetReport(name, ds, _userSession, true);
        }

        /// <summary>
        /// Choose among this type's saved designs. Returns the design name, "" for a brand-new
        /// layout, or null when the user cancels. With no designs saved yet it goes straight to a
        /// new one rather than showing an empty list.
        /// </summary>
        private string PickDesign(string prompt, bool allowNew)
        {
            DataTable list = null;
            try { list = AutoCount.Report.AutoCountReport.GetInstance().GetReportList(_dbSetting, ScpMeterListing.REPORT_TYPE); }
            catch { }
            int count = list == null ? 0 : list.Rows.Count;
            if (count == 0) return allowNew ? "" : "";

            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = "Report Design";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new System.Drawing.Size(460, 320);
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;

                LabelControl lbl = new LabelControl();
                lbl.Text = prompt;
                lbl.Location = new System.Drawing.Point(12, 12);
                lbl.AutoSizeMode = LabelAutoSizeMode.None;
                lbl.Size = new System.Drawing.Size(436, 18);
                dlg.Controls.Add(lbl);

                ListBoxControl lst = new ListBoxControl();
                lst.Location = new System.Drawing.Point(12, 36);
                lst.Size = new System.Drawing.Size(436, 206);
                lst.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                foreach (DataRow r in list.Rows) lst.Items.Add(Convert.ToString(r["ReportName"]));
                lst.SelectedIndex = 0;
                dlg.Controls.Add(lst);

                string picked = null;
                SimpleButton bOk = new SimpleButton();
                bOk.Text = allowNew ? "Modify Selected" : "Use Selected";
                bOk.Size = new System.Drawing.Size(130, 28);
                bOk.Location = new System.Drawing.Point(12, 252);
                bOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                bOk.Click += delegate
                {
                    picked = lst.SelectedIndex >= 0 ? Convert.ToString(lst.SelectedItem) : "";
                    dlg.DialogResult = DialogResult.OK;
                };
                dlg.Controls.Add(bOk);

                if (allowNew)
                {
                    SimpleButton bNew = new SimpleButton();
                    bNew.Text = "New Design";
                    bNew.Size = new System.Drawing.Size(110, 28);
                    bNew.Location = new System.Drawing.Point(148, 252);
                    bNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                    bNew.Click += delegate { picked = ""; dlg.DialogResult = DialogResult.OK; };
                    dlg.Controls.Add(bNew);
                }

                SimpleButton bCancel = new SimpleButton();
                bCancel.Text = "Cancel";
                bCancel.Size = new System.Drawing.Size(90, 28);
                bCancel.Location = new System.Drawing.Point(358, 252);
                bCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                bCancel.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(bCancel);
                dlg.CancelButton = bCancel;

                return dlg.ShowDialog(this) == DialogResult.OK ? picked : null;
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (!HasRows()) return;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                sfd.FileName = "Summary Sales Invoice Meter Listing " + SelectedYear() + "-" +
                    SelectedMonth().ToString("00") + ".xlsx";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    GridViewListing.ExportToXlsx(sfd.FileName);
                    XtraMessageBox.Show("Exported to:\r\n" + sfd.FileName, "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Export failed:\r\n" + ex.Message, "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private bool HasRows() { return HasRows(true); }

        private bool HasRows(bool warn)
        {
            if (_data != null && _data.Rows.Count > 0) return true;
            if (warn)
                XtraMessageBox.Show("Run Inquiry first — there is nothing to print yet.",
                    "Summary Sales Invoice Meter Listing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
    }
}
