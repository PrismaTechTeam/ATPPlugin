using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Per-contract meter detail / override dialog. Opened by double-clicking a contract in the
    /// Meter Reading Integration grid. Shows every meter of the contract with its saved/manual
    /// Current reading beside the freshly-Fetched (API) value, and lets the user tick
    /// "Accept fetched" to override (used to resolve a manual-vs-API conflict). The bound DataTable
    /// is edited in place; the caller reads back the AcceptFetched flags after ShowDialog returns OK.
    /// </summary>
    public partial class MeterReadingDetail_Form : XtraForm
    {
        private DataTable _dt;
        private AutoCount.Data.DBSetting _db;
        private long _contractKey;
        // Tier ladders (schemes + per-meter '#key' overrides) so the live preview uses the SAME
        // engine as the main grid and the invoice — never a private flat-rate approximation.
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<decimal[]>> _ladders;
        // GENERATED INVOICE section (code-built, docked bottom)
        private DevExpress.XtraEditors.GroupControl _grpInv;
        private DevExpress.XtraGrid.GridControl _gridInv;
        private DevExpress.XtraGrid.Views.Grid.GridView _viewInv;

        public MeterReadingDetail_Form()
        {
            InitializeComponent();
        }

        public MeterReadingDetail_Form(string contractNo, string customer, DataTable dt) : this()
        {
            _dt = dt;
            this.LblTitle.Text = "Contract  " + contractNo + "      " + customer;
            this.GridDetail.DataSource = _dt;
            ConfigureGrid();
        }

        /// <summary>Full version: also shows the GENERATED INVOICE grid for this contract —
        /// double-click a row to open that invoice in AutoCount's Invoice module.</summary>
        /// <summary>Set when the user chose "Save &amp; Generate Invoice" — the caller saves the
        /// readings as usual and then runs Generate for THIS contract right away.</summary>
        public bool GenerateRequested;

        public MeterReadingDetail_Form(string contractNo, string customer, DataTable dt,
            AutoCount.Data.DBSetting db, long contractKey)
            : this(contractNo, customer, dt)
        {
            _db = db;
            _contractKey = contractKey;
            try { _ladders = ServiceContractPhotocopier.Classes.ScpMultiPrice.LoadLadders(db); } catch { }
            BuildGeneratedInvoiceSection();
            LoadGeneratedInvoices();

            // One-stop billing: key in the readings and fire the invoice without going back out.
            // Sits right-anchored beside OK, same 40px height as the rest of the bottom row.
            DevExpress.XtraEditors.SimpleButton btnGen = new DevExpress.XtraEditors.SimpleButton();
            btnGen.Text = "Save && Generate Invoice";
            btnGen.Size = new Size(240, 40);
            btnGen.Location = new Point(this.PanelBottom.ClientSize.Width - 510, 10);
            btnGen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnGen.Appearance.ForeColor = Color.FromArgb(198, 40, 40);
            btnGen.Appearance.Options.UseForeColor = true;
            btnGen.Appearance.FontStyleDelta = FontStyle.Bold;
            btnGen.Click += delegate { GenerateRequested = true; BtnOk_Click(btnGen, EventArgs.Empty); };
            this.PanelBottom.Controls.Add(btnGen);
            btnGen.BringToFront();
        }

        private void BuildGeneratedInvoiceSection()
        {
            _grpInv = new DevExpress.XtraEditors.GroupControl();
            _grpInv.Text = "Generated Invoice   (double-click to open in AutoCount)";
            _grpInv.Dock = DockStyle.Bottom;
            _grpInv.Height = 220;

            _gridInv = new DevExpress.XtraGrid.GridControl();
            _gridInv.Dock = DockStyle.Fill;
            _viewInv = new DevExpress.XtraGrid.Views.Grid.GridView(_gridInv);
            _gridInv.MainView = _viewInv;
            _gridInv.ViewCollection.Add(_viewInv);
            _viewInv.OptionsBehavior.Editable = false;
            _viewInv.OptionsView.ShowGroupPanel = false;
            _viewInv.DoubleClick += new EventHandler(ViewInv_DoubleClick);
            _grpInv.Controls.Add(_gridInv);

            this.Controls.Add(_grpInv);
            // Dock order: the invoice group must sit ABOVE the OK/Cancel bar, and the Fill grid must
            // lay out last — bring both forward in that order.
            _grpInv.BringToFront();
            this.GridDetail.BringToFront();
        }

        private void LoadGeneratedInvoices()
        {
            if (_db == null || _contractKey <= 0 || _gridInv == null) return;
            try
            {
                DataTable inv = _db.GetDataTable(
                    "SELECT iv.DocKey, iv.DocNo AS [Invoice No], iv.DocDate AS [Date], " +
                    "ISNULL(iv.NetTotal,0) AS [Total], " +
                    "CASE WHEN ISNULL(iv.Cancelled,'F')='T' THEN 'YES' ELSE '' END AS [Cancelled], " +
                    "COUNT(t.MeterTransKey) AS [Meters] " +
                    "FROM dbo.IV iv " +
                    "JOIN dbo.zSCP_MeterTrans t ON t.SalesInvoiceDocKey = iv.DocKey " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = t.ServiceItemKey " +
                    "WHERE i.ContractKey = " + _contractKey + " " +
                    "GROUP BY iv.DocKey, iv.DocNo, iv.DocDate, iv.NetTotal, iv.Cancelled " +
                    "ORDER BY iv.DocDate DESC, iv.DocNo DESC", false);
                _gridInv.DataSource = inv;
                _viewInv.PopulateColumns();
                if (_viewInv.Columns["DocKey"] != null) _viewInv.Columns["DocKey"].Visible = false;
                GridColumn cDate = _viewInv.Columns["Date"];
                if (cDate != null)
                {
                    cDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    cDate.DisplayFormat.FormatString = "dd/MM/yyyy";
                }
                GridColumn cTot = _viewInv.Columns["Total"];
                if (cTot != null)
                {
                    cTot.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    cTot.DisplayFormat.FormatString = "n2";
                }
                _viewInv.BestFitColumns();
            }
            catch { }
        }

        // Double-click an invoice row -> open that document in AutoCount's Invoice Entry.
        private void ViewInv_DoubleClick(object sender, EventArgs e)
        {
            int rh = _viewInv.FocusedRowHandle;
            if (rh < 0) return;
            object k = _viewInv.GetRowCellValue(rh, "DocKey");
            if (k == null || k == DBNull.Value) return;
            long docKey = Convert.ToInt64(k);
            try
            {
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                        AutoCount.Authentication.UserSession.CurrentUserSession, _db);
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(docKey);
                if (doc == null)
                { XtraMessageBox.Show("Invoice not found — it may have been deleted.", "Open Invoice"); LoadGeneratedInvoices(); return; }
                using (AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry f =
                    new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc))
                {
                    f.ShowDialog(this);
                }
                LoadGeneratedInvoices();   // reflect edits/cancellations made inside the entry form
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Could not open the invoice:\r\n" + ex.Message, "Open Invoice"); }
        }

        private void ConfigureGrid()
        {
            // Flat, solid-green header so the white title/hint stay high-contrast (the default
            // PanelControl gradient washed the text out near the top).
            this.PanelTop.LookAndFeel.UseDefaultLookAndFeel = false;
            this.PanelTop.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            this.PanelTop.Appearance.BackColor = Color.FromArgb(27, 94, 32);
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.LblTitle.Appearance.ForeColor = Color.White;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblHint.Appearance.ForeColor = Color.FromArgb(220, 237, 220);
            this.LblHint.Appearance.Font = new Font("Segoe UI", 8.5F);
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Appearance.Options.UseFont = true;

            this.GridViewDetail.OptionsBehavior.Editable = true;
            this.GridViewDetail.OptionsView.ColumnAutoWidth = false;
            this.GridViewDetail.OptionsView.ShowGroupPanel = false;
            this.GridViewDetail.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewDetail_RowStyle);

            if (this.GridViewDetail.Columns["ItemMeterKey"] != null) this.GridViewDetail.Columns["ItemMeterKey"].Visible = false;
            if (this.GridViewDetail.Columns["HasConflict"] != null) this.GridViewDetail.Columns["HasConflict"].Visible = false;

            SetCol("ServiceItemNo", "Service Item No", 115);
            SetCol("SerialNo", "Serial", 90);
            SetCol("MeterType", "Meter Type", 115);
            SetCol("Role", "Meter", 85);
            SetCol("MeterTypeName", "Meter Type Name", 150);
            // The EFFECTIVE deal per meter — ladder / waive config / MIN scope / rental — so the
            // key-in operator sees what actually bills, not just the raw meter-row numbers.
            SetCol("Deal", "Pricing / Deal (effective)", 215);
            GridColumn dealCol = this.GridViewDetail.Columns["Deal"];
            if (dealCol != null)
            {
                GridColumn afterCol = this.GridViewDetail.Columns["MeterTypeName"];
                if (afterCol != null) dealCol.VisibleIndex = afterCol.VisibleIndex + 1;
                dealCol.AppearanceCell.ForeColor = Color.FromArgb(21, 101, 192);
                dealCol.AppearanceCell.Options.UseForeColor = true;
            }
            if (this.GridViewDetail.Columns["HasLadder"] != null) this.GridViewDetail.Columns["HasLadder"].Visible = false;
            if (this.GridViewDetail.Columns["LadderFoc"] != null) this.GridViewDetail.Columns["LadderFoc"].Visible = false;
            SetNum("MinCharges", "Min. Charges", 85, "n2");
            SetNum("UnitPrice", "Unit Price", 80, "n4");
            SetNum("FOCQty", "FOC Qty", 70, "n0");
            SetNum("RebatePct", "Rebate (%)", 75, "n2");
            SetCol("LastReadDate", "Last Read Date", 95);
            SetNum("LastReading", "Last Reading", 95, "n0");
            SetNum("CurrentReading", "Manual Reading", 130, "n0");
            SetNum("MeterUsage", "Meter Usage", 90, "n0");
            SetNum("TotalCharges", "Total Charges", 90, "n2");
            SetNum("FetchedReading", "API Reading", 95, "n0");
            SetCol("Source", "Source", 70);
            SetCol("LastInvNo", "Last Invoice No", 105);
            SetCol("LastInvDate", "Last Invoice Date", 100);
            GridColumn dcol = this.GridViewDetail.Columns["LastReadDate"];
            if (dcol != null)
            { dcol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime; dcol.DisplayFormat.FormatString = "dd/MM/yyyy"; }
            dcol = this.GridViewDetail.Columns["LastInvDate"];
            if (dcol != null)
            { dcol.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime; dcol.DisplayFormat.FormatString = "dd/MM/yyyy"; }

            // Total Charges gets the same green money tint as the main grid.
            GridColumn chg = this.GridViewDetail.Columns["TotalCharges"];
            if (chg != null)
            {
                chg.AppearanceCell.BackColor = Color.FromArgb(223, 240, 216);
                chg.AppearanceCell.Options.UseBackColor = true;
                chg.AppearanceHeader.BackColor = Color.FromArgb(198, 230, 190);
                chg.AppearanceHeader.ForeColor = Color.FromArgb(27, 94, 32);
                chg.AppearanceHeader.Options.UseBackColor = true;
                chg.AppearanceHeader.Options.UseForeColor = true;
                chg.AppearanceHeader.FontStyleDelta = FontStyle.Bold;
                chg.AppearanceHeader.Options.UseFont = true;
            }
            // Computation-only column stays hidden.
            if (this.GridViewDetail.Columns["UseMin"] != null) this.GridViewDetail.Columns["UseMin"].Visible = false;
            // Ladder rows: the raw Unit Price / FOC Qty do NOT bill — display the ladder's
            // numbers in their place (same idea as the contract's meter grid).
            this.GridViewDetail.CustomColumnDisplayText +=
                new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(Detail_ColumnDisplayText);
            // Recompute on commit (cell leave / checkbox toggle)...
            this.GridViewDetail.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(Detail_CellValueChanged);
            // ...AND live while TYPING in the Manual Reading cell — usage + charges follow every keystroke.
            this.GridViewDetail.ShownEditor += new EventHandler(Detail_ShownEditor);

            // Current Reading is editable here so the user can key in readings manually for this
            // contract (typed values are saved as MANUAL on OK).
            GridColumn cur = this.GridViewDetail.Columns["CurrentReading"];
            if (cur != null)
            {
                cur.OptionsColumn.AllowEdit = true; cur.OptionsColumn.ReadOnly = false;
                // Paint the editable column yellow so users see at a glance which one they key into —
                // header included, a shade stronger. The hint below says "the yellow column", and
                // that instruction only works if the column is yellow all the way up to its title.
                cur.AppearanceCell.BackColor = Color.FromArgb(255, 249, 196);
                cur.AppearanceCell.Options.UseBackColor = true;
                cur.AppearanceHeader.BackColor = Color.FromArgb(255, 236, 150);
                cur.AppearanceHeader.ForeColor = Color.FromArgb(102, 60, 0);
                cur.AppearanceHeader.Options.UseBackColor = true;
                cur.AppearanceHeader.Options.UseForeColor = true;
                cur.AppearanceHeader.FontStyleDelta = FontStyle.Bold;
                cur.AppearanceHeader.Options.UseFont = true;
            }
            this.LblHint.Text = "Type the Manual Reading (yellow column) for this machine, or tick \'Use API Reading\' to use the API value instead.";

            GridColumn acc = this.GridViewDetail.Columns["AcceptFetched"];
            if (acc != null)
            {
                acc.Caption = "Use API Reading";
                acc.Width = 150;
                acc.ColumnEdit = this.RepoCheck;
                acc.OptionsColumn.AllowEdit = true;
                acc.OptionsColumn.ReadOnly = false;
            }
        }

        // With a multi-price ladder the meter row's raw Unit Price / FOC Qty are ignored by the
        // engine — show "tiered" and the ladder's free band instead, so the grid never contradicts
        // the computed Total Charges.
        private void Detail_ColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.ListSourceRowIndex < 0) return;
            System.Data.DataTable src = this.GridDetail.DataSource as System.Data.DataTable;
            if (src == null || e.ListSourceRowIndex >= src.Rows.Count) return;
            if (!src.Columns.Contains("HasLadder")) return;
            System.Data.DataRow d = src.Rows[e.ListSourceRowIndex];
            if (d["HasLadder"] == DBNull.Value || !Convert.ToBoolean(d["HasLadder"])) return;
            if (e.Column.FieldName == "UnitPrice") e.DisplayText = "tiered";
            else if (e.Column.FieldName == "FOCQty")
            {
                decimal lf = d["LadderFoc"] == DBNull.Value ? 0m : Convert.ToDecimal(d["LadderFoc"]);
                e.DisplayText = lf.ToString("n0");
            }
        }

        private void SetCol(string field, string caption, int width)
        {
            GridColumn c = this.GridViewDetail.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width;
            c.OptionsColumn.AllowEdit = false; c.OptionsColumn.ReadOnly = true;
        }

        private void SetNum(string field, string caption, int width, string fmt)
        {
            SetCol(field, caption, width);
            GridColumn c = this.GridViewDetail.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c.DisplayFormat.FormatString = fmt;
        }

        // Conflict rows (saved manual differs from fetched) are highlighted light red.
        private void GridViewDetail_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            object cf = this.GridViewDetail.GetRowCellValue(e.RowHandle, "HasConflict");
            if (cf != null && cf != DBNull.Value && Convert.ToBoolean(cf))
            {
                e.Appearance.BackColor = Color.FromArgb(255, 224, 224);
                e.Appearance.Options.UseBackColor = true;
            }
        }

        private void BtnAcceptAll_Click(object sender, EventArgs e)
        {
            this.GridViewDetail.CloseEditor();
            if (_dt == null) return;
            foreach (DataRow d in _dt.Rows)
                if (Convert.ToDecimal(d["FetchedReading"] == DBNull.Value ? 0 : d["FetchedReading"]) > 0m)
                    d["AcceptFetched"] = true;
            this.GridDetail.RefreshDataSource();
        }

        private void Detail_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName != "CurrentReading" && e.Column.FieldName != "AcceptFetched") return;
            DataRow d = this.GridViewDetail.GetDataRow(e.RowHandle);
            if (d == null) return;
            RecalcDetail(d);
        }

        // While the Manual Reading editor is open, EVERY KEYSTROKE recomputes usage/charges of the
        // row immediately. NOTE: an in-place editor only updates .Text while typing (.EditValue lags
        // until the cell is committed), so the per-keystroke hook must be TextChanged.
        private void Detail_ShownEditor(object sender, EventArgs e)
        {
            if (this.GridViewDetail.FocusedColumn == null ||
                this.GridViewDetail.FocusedColumn.FieldName != "CurrentReading") return;
            DevExpress.XtraEditors.BaseEdit ed = this.GridViewDetail.ActiveEditor;
            if (ed == null) return;
            // Editors are pooled/reused — remove first so repeated ShownEditor never stacks handlers.
            ed.TextChanged -= new EventHandler(Detail_LiveTyping);
            ed.TextChanged += new EventHandler(Detail_LiveTyping);
        }

        private void Detail_LiveTyping(object sender, EventArgs e)
        {
            DevExpress.XtraEditors.BaseEdit ed = this.GridViewDetail.ActiveEditor;
            if (ed == null) return;
            int rh = this.GridViewDetail.FocusedRowHandle;
            DataRow d = this.GridViewDetail.GetDataRow(rh);
            if (d == null) return;
            string txt = (ed.Text ?? "").Replace(",", "").Trim();
            decimal typed;
            if (txt.Length == 0) typed = 0m;
            else if (!decimal.TryParse(txt, out typed)) return;
            ComputeUsageCharge(d, typed);
            this.GridViewDetail.InvalidateRow(rh);   // repaint usage/charges while the editor stays open
        }

        // Mirrors the main grid's Recalc: effective reading = API value when accepted, else the
        // manual one. Charge comes from the ONE billing engine (ComputeCharge: NET FOC + rebate,
        // multi-price ladder incl. per-meter overrides, FOC-reset accrual, min floor) — a private
        // flat-rate formula here used to preview ladder meters wrongly.
        private void RecalcDetail(DataRow d)
        {
            decimal fv = DV(d["FetchedReading"]), cv = DV(d["CurrentReading"]);
            bool accept = d["AcceptFetched"] != DBNull.Value && Convert.ToBoolean(d["AcceptFetched"]);
            ComputeUsageCharge(d, (accept && fv > 0m) ? fv : cv);
        }

        private void ComputeUsageCharge(DataRow d, decimal cur)
        {
            ServiceContractPhotocopier.Classes.MeterBillLine ln = new ServiceContractPhotocopier.Classes.MeterBillLine();
            ln.Last = DV(d["LastReading"]);
            ln.Current = cur;
            ln.Rate = DV(d["UnitPrice"]);
            ln.MinCharges = DV(d["MinCharges"]);
            ln.Foc = DV(d["FOCQty"]);
            ln.RebatePct = DV(d["RebatePct"]);
            ln.MultiPriceCode = d.Table.Columns.Contains("MultiPriceCode") && d["MultiPriceCode"] != DBNull.Value
                ? Convert.ToString(d["MultiPriceCode"]) : "";
            ln.FocResetCount = d.Table.Columns.Contains("FocResetCount") && d["FocResetCount"] != DBNull.Value
                ? Convert.ToInt32(d["FocResetCount"]) : 1;
            ln.IsFlat = d.Table.Columns.Contains("IsFlat") && d["IsFlat"] != DBNull.Value && Convert.ToBoolean(d["IsFlat"]);
            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, _ladders);
            // Same UseMin override the generate loop applies (meter flagged "always bill the minimum").
            bool useMin = d["UseMin"] != DBNull.Value && Convert.ToBoolean(d["UseMin"]);
            d["MeterUsage"] = ln.Usage;
            d["TotalCharges"] = useMin ? ln.MinCharges : ln.Charge;
        }

        private static decimal DV(object v)
        { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            this.GridViewDetail.CloseEditor();
            this.GridViewDetail.UpdateCurrentRow();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
