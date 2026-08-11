using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.Classes.CommonForms;

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
        private DataTable _contracts;   // picker source, re-filtered as the debtor range changes

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
            LoadLookups();
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

        // ───────────────────── filter ─────────────────────

        /// <summary>
        /// Fill the three pickers. Only customers and contracts that HAVE a contract in this book are
        /// offered — a filter listing names that can never match anything wastes the operator's time
        /// and makes an empty result look like a bug.
        /// </summary>
        private void LoadLookups()
        {
            if (_dbSetting == null) return;
            try
            {
                DataTable deb = _dbSetting.GetDataTable(
                    "SELECT DISTINCT c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM dbo.zSCP2_Contract c LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "WHERE ISNULL(c.DebtorCode,'') <> '' ORDER BY c.DebtorCode", false);
                BindDebtor(SluDebtorFrom, SluDebtorFromView, deb);
                BindDebtor(SluDebtorTo, SluDebtorToView, deb.Copy());
            }
            catch { }

            try
            {
                _contracts = _dbSetting.GetDataTable(
                    "SELECT c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName, " +
                    "  ISNULL(c.Description,'') AS Description " +
                    "FROM dbo.zSCP2_Contract c LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "ORDER BY c.ContractNo", false);
                BindContract();
            }
            catch { }

            // Narrowing the customer range narrows the contract list with it — picking a contract that
            // the debtor range already excludes is a contradiction the screen should not offer.
            SluDebtorFrom.EditValueChanged += new EventHandler(DebtorRange_Changed);
            SluDebtorTo.EditValueChanged += new EventHandler(DebtorRange_Changed);
        }

        private void BindDebtor(SearchLookUpEdit edit, DevExpress.XtraGrid.Views.Grid.GridView view, DataTable src)
        {
            edit.Properties.DataSource = src;
            edit.Properties.ValueMember = "DebtorCode";
            edit.Properties.DisplayMember = "DebtorCode";
            edit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            view.OptionsBehavior.AutoPopulateColumns = false;
            view.Columns.Clear();
            GridColumn c1 = view.Columns.AddVisible("DebtorCode");
            c1.Caption = "Code"; c1.Width = 90;
            GridColumn c2 = view.Columns.AddVisible("CompanyName");
            c2.Caption = "Customer"; c2.Width = 260;
            view.OptionsView.ShowAutoFilterRow = true;
            view.OptionsView.ShowIndicator = false;
        }

        private void BindContract()
        {
            if (_contracts == null) return;
            DataView dv = new DataView(_contracts);
            dv.RowFilter = ContractRowFilter();
            SluContract.Properties.DataSource = dv;
            SluContract.Properties.ValueMember = "ContractNo";
            SluContract.Properties.DisplayMember = "ContractNo";
            SluContract.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            SluContractView.OptionsBehavior.AutoPopulateColumns = false;
            SluContractView.Columns.Clear();
            GridColumn c1 = SluContractView.Columns.AddVisible("ContractNo");
            c1.Caption = "Contract No"; c1.Width = 120;
            GridColumn c2 = SluContractView.Columns.AddVisible("DebtorCode");
            c2.Caption = "Code"; c2.Width = 80;
            GridColumn c3 = SluContractView.Columns.AddVisible("CompanyName");
            c3.Caption = "Customer"; c3.Width = 240;
            GridColumn c4 = SluContractView.Columns.AddVisible("Description");
            c4.Caption = "Description"; c4.Width = 200;
            SluContractView.OptionsView.ShowAutoFilterRow = true;
            SluContractView.OptionsView.ShowIndicator = false;
        }

        private string ContractRowFilter()
        {
            string from = DebtorFrom(), to = DebtorTo();
            string f = "";
            if (from.Length > 0) f = "DebtorCode >= '" + from.Replace("'", "''") + "'";
            if (to.Length > 0)
                f += (f.Length > 0 ? " AND " : "") + "DebtorCode <= '" + to.Replace("'", "''") + "'";
            return f;
        }

        private void DebtorRange_Changed(object sender, EventArgs e)
        {
            if (_contracts == null) return;
            // A contract already picked that the new range excludes would silently contradict the
            // range, so it is dropped rather than left sitting there looking selected.
            string picked = ContractNo();
            DataView dv = SluContract.Properties.DataSource as DataView;
            if (dv != null) { try { dv.RowFilter = ContractRowFilter(); } catch { } }
            if (picked.Length > 0 && dv != null && dv.Find(picked) < 0) SluContract.EditValue = null;
        }

        /// <summary>Codes may be picked OR typed, so read the value and fall back to the text.</summary>
        private static string LookupText(SearchLookUpEdit edit)
        {
            if (edit.EditValue != null && edit.EditValue != DBNull.Value)
            {
                string v = Convert.ToString(edit.EditValue).Trim();
                if (v.Length > 0) return v;
            }
            string t = (edit.Text ?? "").Trim();
            return t == edit.Properties.NullText ? "" : t;
        }

        private string DebtorFrom() { return LookupText(SluDebtorFrom); }
        private string DebtorTo() { return LookupText(SluDebtorTo); }
        private string ContractNo() { return LookupText(SluContract); }

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
            string from = DebtorFrom(), to = DebtorTo();
            // An inverted range matches nothing. Say so, rather than returning an empty grid that
            // looks identical to "this month was never invoiced".
            if (from.Length > 0 && to.Length > 0 &&
                string.Compare(from, to, StringComparison.OrdinalIgnoreCase) > 0)
            {
                XtraMessageBox.Show(
                    "Debtor From (" + from + ") is after Debtor To (" + to + "), so no customer can fall " +
                    "inside that range.\r\n\r\nSwap them, or clear one for an open-ended range.",
                    "Filter Options", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _data = ScpMeterListing.Build(_dbSetting, SelectedYear(), SelectedMonth(),
                    from, to, ContractNo());
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
            SluDebtorFrom.EditValue = null;
            SluDebtorTo.EditValue = null;
            SluContract.EditValue = null;
            CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            CmbYear.SelectedItem = DateTime.Today.Year;
            // Reset means reset: the grid's own filter and grouping go too, or the operator clears
            // the filter box and still cannot see rows they know are there.
            try { GridViewListing.ActiveFilterString = ""; } catch { }
            try { GridViewListing.ClearColumnsFilter(); } catch { }
            try { GridViewListing.ClearGrouping(); } catch { }
            BindContract();
        }

        /// <summary>
        /// AutoCount's inquiry screens all end in the same place: a filter builder over the columns
        /// in front of you. This is that — conditions across any column, ANDs and ORs, on the rows
        /// already loaded.
        /// </summary>
        private void BtnAdvFilter_Click(object sender, EventArgs e)
        {
            if (!HasRows()) return;
            try { GridViewListing.ShowFilterEditor(null); }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the filter builder:\r\n" + ex.Message,
                    "Advanced Filter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
            string name = ReportDesignList_Form.Pick(this, _dbSetting, _userSession,
                ScpMeterListing.REPORT_TYPE, VisibleRows(), LayoutColumns(),
                "Preview with which report design?");
            if (string.IsNullOrEmpty(name)) return;   // cancelled, or nothing saved yet
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
            ReportDesignList_Form.Manage(this, _dbSetting, _userSession,
                ScpMeterListing.REPORT_TYPE, ds, LayoutColumns());
        }

        /// <summary>
        /// The columns a NEW design starts with: exactly the ones on screen, in the order and the
        /// relative widths the operator arranged them. Hiding a column here and pressing New means
        /// the printed report does not carry it either — which is what "design what I am looking at"
        /// has to mean for it to be worth anything.
        /// </summary>
        private System.Collections.Generic.List<ReportDesignList_Form.ReportColumn> LayoutColumns()
        {
            System.Collections.Generic.List<ReportDesignList_Form.ReportColumn> cols =
                new System.Collections.Generic.List<ReportDesignList_Form.ReportColumn>();
            for (int i = 0; i < GridViewListing.VisibleColumns.Count; i++)
            {
                GridColumn c = GridViewListing.VisibleColumns[i];
                if (c == null || string.IsNullOrEmpty(c.FieldName)) continue;
                ReportDesignList_Form.ReportColumn rc = new ReportDesignList_Form.ReportColumn();
                rc.Field = c.FieldName;
                rc.Caption = c.Caption;
                rc.Width = c.Width <= 0 ? 90 : c.Width;
                rc.Format = c.DisplayFormat.FormatString ?? "";
                rc.RightAlign = c.DisplayFormat.FormatType == DevExpress.Utils.FormatType.Numeric;
                cols.Add(rc);
            }
            return cols;
        }

        private AutoCount.Report.ReportTemplate LoadTemplate(string name, DataTable ds)
        {
            AutoCount.Report.ReportTemplate tpl = string.IsNullOrEmpty(name)
                ? AutoCount.Report.AutoCountReport.GetInstance().NewReport(ScpMeterListing.REPORT_TYPE, ds, _userSession)
                : AutoCount.Report.AutoCountReport.GetInstance().GetReport(name, ds, _userSession, true);
            // Every AutoCount report carries a script, and its references are built from the host
            // EXE's folder — repoint them at the assemblies actually loaded or the preview renders
            // nothing but "There are errors in scripts".
            if (tpl != null) ScpReportScripts.FixReferences(tpl.Report as DevExpress.XtraReports.UI.XtraReport);
            return tpl;
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
