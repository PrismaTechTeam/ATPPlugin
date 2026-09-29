using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// The month-end run, one row per invoice.
    ///
    /// <para>The Meter Reading Integration screen lists every counter in the book and is kept
    /// as that: the list. This screen lists the DOCUMENTS a day produces -- a contract's rental
    /// invoice, its meter invoice, a bill group's invoice, a machine's own -- and says of each
    /// whether it can go out (green), what it is waiting for (amber, with the count), or that it
    /// already went (grey, with the number). "Generate all ready" sends the green ones and needs
    /// nothing ticked; a tick is for sending some of them.</para>
    ///
    /// <para>Nothing about money or grouping is decided here. Rows come from
    /// <see cref="ScpBillingRows"/>, the invoice key from <see cref="ScpInvoiceRun"/>, which
    /// mirrors <see cref="ScpInvoiceJobs"/>, and generating goes through
    /// <see cref="ScpInvoiceJobs.Build"/> and the same preview and progress dialogs as the list
    /// screen. Keying a reading writes the same staging row the list screen writes.</para>
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Meter Invoice Run", MenuOrder = 445, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class InvoiceRun_Form : XtraForm
    {
        private DBSetting _db;
        private UserSession _userSession;

        private DataTable _rows;                                   // one row per counter, from ScpBillingRows
        private Dictionary<string, List<decimal[]>> _ladders;
        private string _grpMode = ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_FOLLOW;
        private List<InvoiceRunItem> _items = new List<InvoiceRunItem>();
        private Dictionary<string, InvoiceRunItem> _byKey = new Dictionary<string, InvoiceRunItem>();
        private DataTable _dtItems;                                // what the left grid shows
        private DataView _detail;                                  // what the right grid shows
        private InvoiceRunItem _current;

        private MeterReadingIntegration_Form _meters;              // the list screen, hosted as the Meters view
        private Timer _fetchWatch;                                 // ticks while the hosted Fetch runs; reloads here when it ends
        private List<int> _days = new List<int>();
        private List<CheckButton> _dayButtons = new List<CheckButton>();
        private bool _suppress;                                    // programmatic changes must not reload
        private bool _loading;

        /// <summary>Showing every billing date already gone by, instead of one chosen day.</summary>
        private bool _overdue;
        /// <summary>How many invoices the last overdue scan found; -1 until one has run. Counting
        /// them costs a load a month, so the number is remembered rather than asked for again.</summary>
        private int _overdueCount = -1;
        /// <summary>How far back "Show all overdue" looks. A book further behind than this has a
        /// bigger problem than a screen, and every month costs a load.</summary>
        private const int OVERDUE_MONTHS_BACK = 6;
        /// <summary>One entry per month loaded: its rows and its price ladders. The ordinary view
        /// holds exactly one; the overdue view holds a month each.</summary>
        private readonly Dictionary<string, MonthRows> _months = new Dictionary<string, MonthRows>();

        private sealed class MonthRows
        {
            public int Year;
            public int Month;
            public DataTable Rows;
            public Dictionary<string, List<decimal[]>> Ladders;
        }

        private static string MonthKey(int year, int month) { return year + "-" + month; }

        private static readonly Color READY_BACK = Color.FromArgb(227, 242, 228);
        private static readonly Color WAIT_BACK = Color.FromArgb(255, 243, 207);
        private static readonly Color LATE_BACK = Color.FromArgb(253, 232, 232);
        private static readonly Color LATE_FORE = Color.FromArgb(183, 28, 28);
        private static readonly Color DONE_FORE = Color.FromArgb(108, 116, 112);
        private static readonly Color WAIT_CELL = Color.FromArgb(255, 213, 79);

        public InvoiceRun_Form()
        {
            InitializeComponent();
            // Native AutoCount header (same as Meter Reading Integration) with the hint line hidden.
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            ApplyButtonIcons();
            InitDefaults();
        }

        /// <summary>The same AutoCount toolbar images the list screen wears, so the two screens
        /// read as one module.</summary>
        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                SetBtnAcIcon(this.BtnRefresh, img.GetLargeImage_Refresh());
                SetBtnAcIcon(this.BtnFetch, img.GetLargeImage_Inquiry());
                SetBtnAcIcon(this.BtnGenerateReady, img.GetLargeImage_New());
                SetBtnAcIcon(this.BtnGenerateSelected, img.GetLargeImage_Approve());
                SetBtnAcIcon(this.BtnPreview, img.GetLargeImage_New());
            }
            catch { }   // icons are cosmetic -- never block the form over an image lookup
            SetBtnSvgIcon(this.BtnSetting, "svgimages/setup/properties.svg");
        }

        private static void SetBtnAcIcon(SimpleButton btn, Image image)
        {
            if (btn == null || image == null) return;
            btn.ImageOptions.Image = image;
            btn.ImageOptions.ImageToTextIndent = 6;
            btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        private static void SetBtnSvgIcon(SimpleButton btn, string svgName)
        {
            if (btn == null) return;
            try
            {
                DevExpress.Utils.Svg.SvgImage img = DevExpress.Images.ImageResourceCache.Default.GetSvgImage(svgName);
                if (img == null) return;
                btn.ImageOptions.SvgImage = img;
                btn.ImageOptions.SvgImageSize = new Size(24, 24);
                btn.ImageOptions.ImageToTextIndent = 6;
                btn.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
                btn.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
            }
            catch { }
        }

        public InvoiceRun_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _db = userSession.DBSetting;
            Boot();
        }

        public InvoiceRun_Form(DBSetting dbSetting) : this()
        {
            _db = dbSetting;
            Boot();
        }

        // ───────────────────────────── set-up ─────────────────────────────

        private void InitDefaults()
        {
            this.CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                this.CmbMonth.Properties.Items.Add(new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            this.CmbYear.Properties.Items.Clear();
            for (int y = DateTime.Today.Year - 2; y <= DateTime.Today.Year + 1; y++)
                this.CmbYear.Properties.Items.Add(y.ToString());
            _suppress = true;
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            this.CmbYear.SelectedIndex = this.CmbYear.Properties.Items.IndexOf(DateTime.Today.Year.ToString());
            _suppress = false;

            this.CmbMonth.SelectedIndexChanged += new EventHandler(Period_Changed);
            this.CmbYear.SelectedIndexChanged += new EventHandler(Period_Changed);
            this.BtnRefresh.Click += new EventHandler(BtnRefresh_Click);
            this.BtnFetch.Click += new EventHandler(BtnFetch_Click);
            this.BtnSetting.Click += new EventHandler(BtnSetting_Click);
            this.BtnViewInvoices.CheckedChanged += new EventHandler(View_Changed);
            this.BtnViewMeters.CheckedChanged += new EventHandler(View_Changed);
            this.BtnGenerateReady.Click += new EventHandler(BtnGenerateReady_Click);
            this.BtnGenerateSelected.Click += new EventHandler(BtnGenerateSelected_Click);
            this.BtnPreview.Click += new EventHandler(BtnGenerateThis_Click);
            this.BtnHistory.Click += new EventHandler(BtnHistory_Click);
            this.BtnDeleteInvoice.Click += new EventHandler(BtnDeleteInvoice_Click);
            this.BtnTakeOver.Click += new EventHandler(BtnTakeOver_Click);
            // Feedback ATP-6 / ATP-9: the invoice date is the due date only by default.
            this.DtInvDate.EditValueChanged += new EventHandler(DtInvDate_EditValueChanged);
            this.TxtInvRef.EditValueChanged += new EventHandler(TxtInvRef_EditValueChanged);
            this.BtnInvDateFromReading.Click += new EventHandler(BtnInvDateFromReading_Click);
            this.GridViewInvoices.RowCellStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewInvoices_InvDateCellStyle);
            this.BtnTakeOver.ToolTip = "The machine could not be trusted this month -- it broke down, it was " +
                "swapped, it reported a stale counter. This makes the reading on the row yours: the number " +
                "and its date are kept, the date can then be corrected, and a later fetch that disagrees " +
                "raises a conflict instead of overwriting it.";
            this.GridViewReadings.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewReadings_FocusedRowChanged);
            this.BtnOverdue.CheckedChanged += new EventHandler(BtnOverdue_CheckedChanged);
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(InvoiceRun_KeyDown);
            this.GridViewInvoices.DoubleClick += new EventHandler(GridViewInvoices_DoubleClick);
            this.GridReadings.Leave += new EventHandler(GridReadings_Leave);
            this.GridViewReadings.CustomColumnDisplayText +=
                new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(GridViewReadings_CustomColumnDisplayText);
            this.ChipAll.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipReady.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipWaiting.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipInvoiced.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipKindAll.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipKindRental.CheckedChanged += new EventHandler(Chip_Changed);
            this.ChipKindMeter.CheckedChanged += new EventHandler(Chip_Changed);
            this.TxtSearch.KeyDown += new KeyEventHandler(TxtSearch_KeyDown);
            this.ChkMissingOnly.CheckedChanged += new EventHandler(ChkMissingOnly_Changed);

            this.GridViewInvoices.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewInvoices_FocusedRowChanged);
            this.GridViewInvoices.RowStyle += new RowStyleEventHandler(GridViewInvoices_RowStyle);
            this.GridViewInvoices.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewInvoices_CellValueChanged);
            this.GridViewInvoices.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewInvoices_ShowingEditor);
            // A tick is posted the moment it is clicked, not when focus leaves the row -- otherwise
            // "Generate selected" only wakes up after the clerk clicks somewhere else.
            this.RepoSel.EditValueChanged += new EventHandler(RepoSel_EditValueChanged);

            this.GridViewReadings.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewReadings_ShowingEditor);
            this.GridViewReadings.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewReadings_CellValueChanged);
            this.GridViewReadings.RowCellStyle += new RowCellStyleEventHandler(GridViewReadings_RowCellStyle);

            this.BtnPreview.Text = "Generate this invoice";
            this.BtnGenerateReady.Enabled = false;
            this.BtnGenerateSelected.Enabled = false;
            this.BtnPreview.Enabled = false;
            this.BtnHistory.Enabled = false;
            this.BtnDeleteInvoice.Enabled = false;
            this.BtnTakeOver.Enabled = false;
            this.DtInvDate.Enabled = false;
            this.BtnInvDateFromReading.Enabled = false;
        }

        private void Boot()
        {
            if (_db == null) return;
            try { _days = ScpInvoiceRun.DaysInUse(_db); }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not read the billing days:\r\n" + ex.Message, "Meter Invoice Run",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _days = new List<int>();
            }
            BuildDayStrip();
            PickDefaultDay();
            LoadData();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LayoutDayStrip();   // the only moment the panel and the buttons share one unit at any DPI
        }

        // ───────────────────────────── the day strip ─────────────────────────────

        private void BuildDayStrip()
        {
            this.PanelDays.Controls.Clear();
            _dayButtons.Clear();
            foreach (int d in _days)
            {
                CheckButton b = new CheckButton();
                b.Text = d.ToString();
                b.Tag = d;
                b.GroupIndex = 77;
                b.CheckedChanged += new EventHandler(DayButton_CheckedChanged);
                _dayButtons.Add(b);
                this.PanelDays.Controls.Add(b);
            }
            LayoutDayStrip();
        }

        /// <summary>Places the buttons, wrapping when a book bills on many days, and grows the
        /// strip and the panel above the grid to hold every row. Every size is taken from a real
        /// control, never from the designer, so 100% and 150% agree.</summary>
        private void LayoutDayStrip()
        {
            int n = _dayButtons.Count;
            int rowH = Math.Max(24, this.CmbMonth.Height + 4);
            int gap = Math.Max(2, rowH / 9);
            int w = Math.Max(28, rowH + 6);
            int avail = Math.Max(w, this.PanelDays.Width);
            int perRow = Math.Max(1, (avail + gap) / (w + gap));
            int rows = n == 0 ? 1 : (n + perRow - 1) / perRow;
            if (rows > 1) perRow = (n + rows - 1) / rows;   // spread evenly rather than leave a short last row
            for (int i = 0; i < n; i++)
            {
                CheckButton b = _dayButtons[i];
                b.Size = new Size(w, rowH);
                b.Location = new Point((i % perRow) * (w + gap), (i / perRow) * (rowH + gap));
            }
            int stripH = rows * rowH + (rows - 1) * gap;
            this.PanelDays.Height = Math.Max(rowH, stripH);
            this.BtnOverdue.Height = rowH;
            this.BtnOverdue.Left = this.PanelDays.Left;
            this.BtnOverdue.Top = this.PanelDays.Bottom + 6;
            this.LblSummary.Top = this.BtnOverdue.Bottom + 6;
            // Filter Options grows with the strip, and the panel above the grid grows with the box,
            // so a book that bills on thirty days pushes the grid down instead of hiding a row.
            int boxH = Math.Max(150, this.LblSummary.Bottom + 10);
            if (boxH != this.GrpFilter.Height) this.GrpFilter.Height = boxH;
            int wanted = Math.Max(164, this.GrpFilter.Bottom + 6);
            if (wanted != this.PanelFilter.Height) this.PanelFilter.Height = wanted;
        }

        private void PickDefaultDay()
        {
            if (_dayButtons.Count == 0) return;
            int today = DateTime.Today.Day;
            CheckButton pick = null;
            foreach (CheckButton b in _dayButtons)
                if (Convert.ToInt32(b.Tag) >= today) { pick = b; break; }
            if (pick == null) pick = _dayButtons[0];
            _suppress = true;
            pick.Checked = true;
            _suppress = false;
        }

        private int SelectedDay()
        {
            foreach (CheckButton b in _dayButtons) if (b.Checked) return Convert.ToInt32(b.Tag);
            return _days.Count > 0 ? _days[0] : 1;
        }

        private int SelectedMonth() { return this.CmbMonth.SelectedIndex < 0 ? DateTime.Today.Month : this.CmbMonth.SelectedIndex + 1; }

        private int SelectedYear()
        {
            int y;
            return int.TryParse(Convert.ToString(this.CmbYear.EditValue), out y) ? y : DateTime.Today.Year;
        }

        private static string Ordinal(int d)
        {
            if (d % 100 >= 11 && d % 100 <= 13) return d + "th";
            switch (d % 10) { case 1: return d + "st"; case 2: return d + "nd"; case 3: return d + "rd"; default: return d + "th"; }
        }

        private string PeriodName()
        {
            return new CultureInfo("en-US").DateTimeFormat.GetAbbreviatedMonthName(SelectedMonth()) + " " + SelectedYear();
        }

        // ───────────────────────────── load ─────────────────────────────

        private void LoadData()
        {
            if (_db == null || _loading) return;
            _loading = true;
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                // Keep the ticks across a reload: a clerk who ticked six invoices and keyed one
                // reading should not find the six unticked.
                HashSet<string> ticked = new HashSet<string>();
                if (_dtItems != null)
                    foreach (DataRow r in _dtItems.Rows)
                        if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) ticked.Add(Convert.ToString(r["JobKey"]));
                string currentKey = _current == null ? "" : _current.JobKey;

                _grpMode = ScpInvoiceRun.GroupingMode(_db);
                _months.Clear();
                if (_overdue) _items = LoadOverdueItems();
                else
                {
                    int y = SelectedYear(), m = SelectedMonth(), d = SelectedDay();
                    _rows = ScpInvoiceRun.LoadRows(_db, y, m, d, false, Convert.ToString(this.TxtSearch.EditValue), out _ladders);
                    Keep(y, m, _rows, _ladders);
                    _items = ScpInvoiceRun.BuildItems(_rows, _grpMode, y, m);
                }
                _byKey = new Dictionary<string, InvoiceRunItem>();
                foreach (InvoiceRunItem it in _items) _byKey[it.JobKey] = it;

                BindItems(ticked);
                ApplyFilter();
                UpdateSummary();

                _current = null;
                if (currentKey.Length > 0 && _byKey.ContainsKey(currentKey)) FocusItem(currentKey);
                else ShowDetail(FocusedItem());
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Meter Invoice Run", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = old;
                _loading = false;
            }
        }

        private void Keep(int year, int month, DataTable rows, Dictionary<string, List<decimal[]>> ladders)
        {
            MonthRows mr = new MonthRows();
            mr.Year = year;
            mr.Month = month;
            mr.Rows = rows;
            mr.Ladders = ladders;
            _months[MonthKey(year, month)] = mr;
        }

        /// <summary>Every invoice whose billing date has gone by and which is still not made, from
        /// this month back through OVERDUE_MONTHS_BACK. One load per month, not per day: inside a
        /// month the loader already carries the earlier days forward.</summary>
        private List<InvoiceRunItem> LoadOverdueItems()
        {
            List<InvoiceRunItem> all = new List<InvoiceRunItem>();
            string search = Convert.ToString(this.TxtSearch.EditValue);
            // Ask the book which months it is behind on before loading anything: one cheap query
            // names the months AND the contracts, and only those are loaded.
            foreach (ScpInvoiceRun.OverdueMonth om in ScpInvoiceRun.OverdueMonths(_db, OVERDUE_MONTHS_BACK))
            {
                if (om.Contracts.Count == 0) continue;
                Dictionary<string, List<decimal[]>> ladders;
                DataTable rows = ScpInvoiceRun.LoadRows(_db, om.Year, om.Month, 1, true, search,
                                                        om.Contracts, out ladders);
                if (rows == null || rows.Rows.Count == 0) continue;
                Keep(om.Year, om.Month, rows, ladders);
                foreach (InvoiceRunItem it in ScpInvoiceRun.BuildItems(rows, _grpMode, om.Year, om.Month))
                    if (it.Overdue) all.Add(it);
            }
            // Oldest debt first: that is the order it has to be worked off in anyway.
            all.Sort(delegate (InvoiceRunItem a, InvoiceRunItem b)
            {
                int c = a.Due.CompareTo(b.Due);
                return c != 0 ? c : string.Compare(a.Customer, b.Customer, StringComparison.CurrentCultureIgnoreCase);
            });
            return all;
        }

        /// <summary>Point the row tables at the focused invoice's OWN month, which in the overdue
        /// view is not the month the pickers show.</summary>
        private void UseMonthOf(InvoiceRunItem it)
        {
            if (it == null) return;
            MonthRows mr;
            if (!_months.TryGetValue(MonthKey(it.Year, it.Month), out mr)) return;
            _rows = mr.Rows;
            _ladders = mr.Ladders;
        }

        private void BtnOverdue_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            _overdue = this.BtnOverdue.Checked;
            this.CmbMonth.Enabled = !_overdue;
            this.CmbYear.Enabled = !_overdue;
            this.PanelDays.Enabled = !_overdue;
            this.BtnFetch.Enabled = !_overdue;   // Fetch reads the month that is showing
            LoadData();
        }

        // The invoice dates the operator moved (feedback ATP-6 / ATP-9), by period + invoice. Kept for
        // as long as the screen is open, so a Refresh or a Fetch does not quietly put them back.
        private readonly Dictionary<string, DateTime> _docDates = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, string> _invRefs = new Dictionary<string, string>();   // ATP-8

        private static string DocDateKey(InvoiceRunItem it)
        {
            return it.Year + "-" + it.Month + "|" + it.JobKey;
        }

        private void BindItems(HashSet<string> ticked)
        {
            foreach (InvoiceRunItem it0 in _items)
            {
                DateTime moved;
                it0.DocDateOverride = _docDates.TryGetValue(DocDateKey(it0), out moved) ? (DateTime?)moved : null;
                string typedRef;
                it0.RefOverride = _invRefs.TryGetValue(DocDateKey(it0), out typedRef) ? typedRef : "";
            }
            DataTable t = new DataTable();
            t.Columns.Add("Sel", typeof(bool));
            t.Columns.Add("JobKey", typeof(string));
            t.Columns.Add("ContractKey", typeof(long));
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("Kind", typeof(string));
            t.Columns.Add("Detail", typeof(string));
            t.Columns.Add("Machines", typeof(int));
            t.Columns.Add("Readings", typeof(string));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("StatusText", typeof(string));
            t.Columns.Add("Status", typeof(int));
            t.Columns.Add("RentalSide", typeof(bool));
            t.Columns.Add("Overdue", typeof(bool));
            t.Columns.Add("Due", typeof(DateTime));
            t.Columns.Add("InvDate", typeof(DateTime));
            t.Columns.Add("InvDateChanged", typeof(bool));
            foreach (InvoiceRunItem it in _items)
            {
                DataRow r = t.NewRow();
                r["Sel"] = ticked != null && ticked.Contains(it.JobKey);
                r["JobKey"] = it.JobKey;
                r["ContractKey"] = it.ContractKey;
                FillItemRow(r, it);
                t.Rows.Add(r);
            }
            _dtItems = t;
            this.GridInvoices.DataSource = _dtItems;
            this.GridViewInvoices.ExpandAllGroups();
        }

        private static void FillItemRow(DataRow r, InvoiceRunItem it)
        {
            r["Customer"] = it.Customer.Length > 0 ? it.Customer : it.DebtorCode;
            r["ContractNo"] = it.ContractNo;
            r["Kind"] = it.Kind;
            r["Detail"] = it.Detail;
            r["Machines"] = it.Machines.Count;
            r["Readings"] = it.MetersTotal == 0 ? "—" : (it.MetersRead + " / " + it.MetersTotal);
            r["Amount"] = it.Amount;
            r["StatusText"] = it.StatusText;
            r["Status"] = it.Status;
            r["RentalSide"] = it.RentalSide;
            r["Overdue"] = it.Overdue;
            if (it.Year > 0 && it.Month > 0) r["Due"] = it.Due;
            if (r.Table.Columns.Contains("InvDate"))
            {
                // An invoice already made carries its own date on the document; a date worked out here
                // could differ from it, so the column stays empty rather than say something untrue.
                if (it.Status == InvoiceRunItem.INVOICED) r["InvDate"] = DBNull.Value;
                else r["InvDate"] = ScpInvoiceRun.EffectiveDocDate(it);
                r["InvDateChanged"] = it.Status != InvoiceRunItem.INVOICED && it.DocDateOverride.HasValue;
            }
        }

        private DataRow ItemRow(string jobKey)
        {
            if (_dtItems == null) return null;
            foreach (DataRow r in _dtItems.Rows)
                if (Convert.ToString(r["JobKey"]) == jobKey) return r;
            return null;
        }

        // ───────────────────────────── filters, summary ─────────────────────────────

        private void ApplyFilter()
        {
            List<string> parts = new List<string>();
            if (this.ChipReady.Checked) parts.Add("[Status] = 0");
            else if (this.ChipWaiting.Checked) parts.Add("[Status] IN (1, 3, 4, 5)");   // waiting, unpriced, no item code, below last
            else if (this.ChipInvoiced.Checked) parts.Add("[Status] = 2");
            if (this.ChipKindRental.Checked) parts.Add("[Kind] = 'Rental'");
            else if (this.ChipKindMeter.Checked) parts.Add("[Kind] <> 'Rental'");
            this.GridViewInvoices.ActiveFilterString = string.Join(" AND ", parts.ToArray());
            this.GridViewInvoices.ExpandAllGroups();
        }

        private void UpdateSummary()
        {
            int total = 0, ready = 0, waiting = 0, invoiced = 0, selReady = 0, selOther = 0, late = 0;
            decimal readyAmt = 0m, lateAmt = 0m;
            foreach (InvoiceRunItem it in _items)
            {
                total++;
                if (it.Status == InvoiceRunItem.READY) { ready++; readyAmt += it.Amount; }
                else if (it.Status == InvoiceRunItem.INVOICED) invoiced++;
                else waiting++;   // waiting on readings, a price or an item code
                if (it.Overdue) { late++; lateAmt += it.Amount; }
            }
            // The overdue view counts them properly; the day view can only see its own day, so it
            // shows the number the last scan found rather than a smaller one that looks like a total.
            if (_overdue) _overdueCount = total;
            // Which month each line is for: worth a column only when the list spans months.
            if (this.ColDue.Visible != _overdue)
            {
                this.ColDue.Visible = _overdue;
                if (_overdue)
                {
                    this.ColDue.VisibleIndex = 3;
                    this.ColDue.SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;
                }
                else this.ColDue.SortOrder = DevExpress.Data.ColumnSortOrder.None;
            }
            this.BtnOverdue.Text = (_overdue ? "Showing all overdue" : "Show all overdue") +
                                   (_overdueCount >= 0 ? " (" + _overdueCount + ")" : "");
            if (_dtItems != null)
                foreach (DataRow r in _dtItems.Rows)
                    if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))
                    {
                        if (Convert.ToInt32(r["Status"]) == InvoiceRunItem.READY) selReady++; else selOther++;
                    }

            string day = Ordinal(SelectedDay());
            if (_overdue)
            {
                this.LblSummary.Text = "Overdue:  " + total + (total == 1 ? " invoice" : " invoices") +
                    "   ·   " + ready + " ready   ·   " + waiting + " waiting on readings   ·   RM " + lateAmt.ToString("n2");
                this.LblSummary.Appearance.ForeColor = total > 0 ? LATE_FORE : Color.FromArgb(30, 107, 42);
            }
            else
            {
                this.LblSummary.Text = day + ":  " + total + (total == 1 ? " invoice due" : " invoices due") +
                    "   ·   " + ready + " ready   ·   " + waiting + " waiting on readings   ·   " + invoiced + " invoiced" +
                    (late > 0 ? "   ·   " + late + " overdue" : "");
                this.LblSummary.Appearance.ForeColor = late > 0 ? LATE_FORE
                    : waiting > 0 ? Color.FromArgb(122, 83, 0) : Color.FromArgb(30, 107, 42);
            }

            this.ChipAll.Text = "All (" + total + ")";
            this.ChipReady.Text = "Ready (" + ready + ")";
            this.ChipWaiting.Text = "Waiting (" + waiting + ")";
            this.ChipInvoiced.Text = "Invoiced (" + invoiced + ")";

            this.BtnGenerateReady.Text = "Generate all ready (" + ready + ")";
            this.BtnGenerateReady.Enabled = ready > 0;
            this.BtnGenerateSelected.Text = "Generate selected (" + (selReady + selOther) + ")";
            this.BtnGenerateSelected.Enabled = selReady > 0;

            this.LblGrouping.Text = _grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR ? "Grouping: one invoice per customer  (Setting)"
                : _grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE ? "Grouping: one invoice per machine  (Setting)"
                : "Grouping: follow contract Billing Mode  (Setting)";
            this.GridViewInvoices.ViewCaption = _overdue
                ? "Overdue invoices  -  every billing date up to " + DateTime.Today.ToString("dd/MM/yyyy")
                : "Invoices due on the " + day + "  -  " + PeriodName();
            this.LblStatus.Text = PeriodName() + " · " + day + "     ready to send: RM " + readyAmt.ToString("n2") +
                (selOther > 0 ? "     (" + selOther + " ticked " + (selOther == 1 ? "invoice is" : "invoices are") + " not ready and will be left out)" : "");
        }

        // ───────────────────────────── the right side ─────────────────────────────

        private InvoiceRunItem FocusedItem()
        {
            int h = this.GridViewInvoices.FocusedRowHandle;
            if (h < 0) return null;
            DataRow r = this.GridViewInvoices.GetDataRow(h);
            if (r == null) return null;
            InvoiceRunItem it;
            return _byKey.TryGetValue(Convert.ToString(r["JobKey"]), out it) ? it : null;
        }

        private void FocusItem(string jobKey)
        {
            for (int h = 0; h < this.GridViewInvoices.RowCount; h++)
            {
                if (!this.GridViewInvoices.IsDataRow(h)) continue;
                DataRow r = this.GridViewInvoices.GetDataRow(h);
                if (r != null && Convert.ToString(r["JobKey"]) == jobKey)
                {
                    this.GridViewInvoices.FocusedRowHandle = h;
                    ShowDetail(FocusedItem());
                    return;
                }
            }
            ShowDetail(FocusedItem());
        }

        private void ShowDetail(InvoiceRunItem it)
        {
            _current = it;
            if (it == null)
            {
                this.LblDetailTitle.Text = _items.Count == 0 ? "Nothing due on the " + Ordinal(SelectedDay()) : "Pick an invoice on the left";
                this.LblDetailSub.Text = "";
                this.LblFactMachines.Text = ""; this.LblFactReadings.Text = ""; this.LblFactMissing.Text = ""; this.LblFactAmount.Text = "";
                this.LblDetailFoot.Text = "";
                this.GridReadings.DataSource = null;
                this.BtnPreview.Enabled = false;
                this.BtnHistory.Enabled = false;
                this.BtnDeleteInvoice.Enabled = false;
                this.BtnTakeOver.Enabled = false;
                _suppress = true;
                this.DtInvDate.EditValue = null;
                this.TxtInvRef.EditValue = null;
                _suppress = false;
                this.DtInvDate.Enabled = false;
                this.BtnInvDateFromReading.Enabled = false;
                this.TxtInvRef.Enabled = false;
                return;
            }
            this.LblDetailTitle.Text = it.ContractNo + "  ·  " + it.Kind + " invoice" +
                (it.MachineNo.Length > 0 ? "  ·  " + it.MachineNo : (it.BillGroup.Length > 0 ? "  ·  " + it.BillGroup : ""));
            this.LblDetailSub.Text = it.Customer.Length > 0 ? it.Customer : it.DebtorCode;
            RefreshFacts(it);

            // The row the user is on decides what the tick means: a waiting invoice opens on its
            // missing readings, a ready one on everything it will print.
            _suppress = true;
            this.ChkMissingOnly.Checked = it.Status == InvoiceRunItem.WAITING;
            _suppress = false;
            BindDetail(it);
            this.BtnPreview.Enabled = it.Status == InvoiceRunItem.READY;
            this.BtnHistory.Enabled = it.Rows.Count > 0;
            this.BtnDeleteInvoice.Enabled = it.Status == InvoiceRunItem.INVOICED && it.InvoicedDocNo.Trim().Length > 0;
            // The date this invoice will carry; an invoice already made keeps the date it was made with.
            _suppress = true;
            this.DtInvDate.EditValue = it.Status == InvoiceRunItem.INVOICED ? null : (object)ScpInvoiceRun.EffectiveDocDate(it);
            this.TxtInvRef.EditValue = it.Status == InvoiceRunItem.INVOICED ? null : (object)(it.RefOverride ?? "");
            _suppress = false;
            this.DtInvDate.Enabled = it.Status != InvoiceRunItem.INVOICED;
            this.BtnInvDateFromReading.Enabled = it.Status != InvoiceRunItem.INVOICED;
            this.TxtInvRef.Enabled = it.Status != InvoiceRunItem.INVOICED;

            // The other half of a contract that bills its rent apart, so the clerk sees both.
            string foot = "";
            string sibling = it.RentalSide ? it.JobKey.Substring(0, it.JobKey.Length - 2) : it.JobKey + "_R";
            InvoiceRunItem other;
            if (_byKey.TryGetValue(sibling, out other))
                foot = "This contract's " + other.Kind.ToLowerInvariant() + " invoice: " + other.StatusText.ToLowerInvariant() +
                       (other.Status == InvoiceRunItem.WAITING ? "" : " · " + other.Amount.ToString("n2")) + "  (listed above)";
            else if (it.Status == InvoiceRunItem.INVOICED) foot = "Already on " + it.InvoicedDocNo + ". Nothing left to do here.";
            else if (it.Status == InvoiceRunItem.WAITING) foot = "Key the missing readings below; when the last one is in, this invoice turns Ready.";
            else if (it.Status == InvoiceRunItem.UNPRICED) foot = it.Unpriced + (it.Unpriced == 1 ? " line has" : " lines have") +
                " to bill with no price -- no rate, minimum or tier. Set it on the contract (Meters & Pricing), then Refresh.";
            else if (it.Status == InvoiceRunItem.NO_ITEM) foot = "No item code for " + string.Join(", ", it.NoItemTypes.ToArray()) +
                ": give the meter type an item code in Meter Type Maintenance, then Refresh.";
            else if (it.Status == InvoiceRunItem.BACKWARD) foot = it.Backwards + (it.Backwards == 1 ? " reading is" : " readings are") +
                " below the last one -- a replaced or reset meter, or a typing slip. Correct it below; it cannot be billed as it is.";
            if (it.SameSerial > 0) foot += (foot.Length > 0 ? "  " : "") + "Another active machine has the same serial, so a fetch gives both the same readings -- check it is not billed twice.";
            else foot = "Everything this invoice needs is here. It goes out with Generate all ready.";
            this.LblDetailFoot.Text = foot;
        }

        private void RefreshFacts(InvoiceRunItem it)
        {
            this.LblFactMachines.Text = "Machines: " + it.Machines.Count;
            this.LblFactReadings.Text = it.MetersTotal == 0 ? "No meters to read" : "Readings: " + it.MetersRead + " / " + it.MetersTotal;
            this.LblFactMissing.Text = it.Missing > 0 ? "Missing: " + it.Missing : "";
            this.LblFactAmount.Text = (it.Status == InvoiceRunItem.WAITING ? "So far: " : "Amount: ") + it.Amount.ToString("n2");
        }

        private void BindDetail(InvoiceRunItem it)
        {
            if (it == null) { this.GridReadings.DataSource = null; return; }
            UseMonthOf(it);
            if (_rows == null) { this.GridReadings.DataSource = null; return; }
            string f = "[" + ScpInvoiceRun.COL_JOBKEY + "] = '" + it.JobKey.Replace("'", "''") + "'";
            if (this.ChkMissingOnly.Checked) f += " AND [" + ScpInvoiceRun.COL_NEEDS + "] = True";
            _detail = new DataView(_rows, f, "ServiceItemNo, Role", DataViewRowState.CurrentRows);
            this.GridReadings.DataSource = _detail;
        }

        // ───────────────────────────── events: top ─────────────────────────────

        private void Period_Changed(object sender, EventArgs e) { if (!_suppress) LoadData(); }
        private void DayButton_CheckedChanged(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (_suppress || b == null || !b.Checked) return;
            LoadData();
        }
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            // An invoice deleted in AutoCount itself gives its month back here too.
            ScpBillingRows.ReconcileDeletedInvoices(_db);
            LoadData();
        }

        // ───────────────────────────── deleting an invoice ─────────────────────────────

        /// <summary>Del deletes the invoice that is picked; Ctrl+Shift+Del deletes every invoice on
        /// the list. Both only while the invoice list is the view showing.</summary>
        private void InvoiceRun_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Shift+U: a TEST Fetch JSON for the picked invoice's contract, to copy. Testing only.
            if (e.Control && e.Shift && e.KeyCode == Keys.U)
            {
                e.Handled = true;
                ShowTestFetchJson();
                return;
            }
            // Ctrl+Shift+T: the Meters view's TEST Fetch (JSON). The view is a window inside this
            // one and hears keys only while focus is in it -- after clicking "Meters - fetch & key
            // in" the focus is on that button, here. Only this shortcut is passed on.
            if (e.Control && e.Shift && e.KeyCode == Keys.T && this.BtnViewMeters.Checked
                && _meters != null && !_meters.IsDisposed)
            {
                e.Handled = true;
                _meters.ToggleTestFetch();
                return;
            }
            if (!this.BtnViewInvoices.Checked || e.KeyCode != Keys.Delete) return;
            // Only from the invoice list itself (#77, 29/9): Del in the search box, the Reference No or
            // a reading cell is that box's own key -- it opened the delete prompt and was lost.
            if (!this.GridInvoices.ContainsFocus) return;
            if (e.Control && e.Shift) { e.Handled = true; DeleteAllListedInvoices(); return; }
            if (!e.Control && !e.Shift && !e.Alt) { e.Handled = true; DeleteSelectedInvoice(); }
        }

        private void BtnDeleteInvoice_Click(object sender, EventArgs e) { DeleteSelectedInvoice(); }

        // The button follows the row the operator is on: it is only for a reading that came off a
        // machine and can still be taken over.
        private void GridViewReadings_FocusedRowChanged(object sender,
            DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            DataRow r = this.GridViewReadings.GetFocusedDataRow();
            this.BtnTakeOver.Enabled = r != null && ScpInvoiceRun.WhyCannotTakeOver(r).Length == 0;
        }

        // "Key in myself": the machine was down (or swapped, or reporting a stale counter), so the
        // reading on the row becomes the operator's. The number and the date it arrived with are
        // kept -- nothing is billed differently by this click -- and the date can then be corrected
        // (feedback ATP-5). Typing the number again cannot do this: a grid raises nothing when the
        // value does not change, so the reading would stay the machine's.
        private void BtnTakeOver_Click(object sender, EventArgs e)
        {
            DataRow r = this.GridViewReadings.GetFocusedDataRow();
            if (r == null || _db == null) return;
            string no = ScpInvoiceRun.WhyCannotTakeOver(r);
            if (no.Length > 0)
            { XtraMessageBox.Show(no, "Key in myself", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            string what = Convert.ToString(r["ServiceItemNo"]).Trim() + "  " +
                          Convert.ToString(r["Role"]).Trim();
            decimal cur = r["CurrentReading"] == DBNull.Value ? 0m : Convert.ToDecimal(r["CurrentReading"]);
            if (XtraMessageBox.Show(
                    what + " reads " + cur.ToString("n0") + ", and that reading came from the machine.\r\n\r\n" +
                    "Take it over as your own? The number and its date stay as they are -- what changes is that " +
                    "the date becomes yours to correct, and the next fetch will raise a conflict instead of " +
                    "overwriting it.",
                    "Key in myself", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int y, m;
            WorkPeriod(out y, out m);
            long imk = r["ItemMeterKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemMeterKey"]);
            try
            {
                ScpInvoiceRun.TakeOverReading(_db, imk, y, m);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The reading was not taken over:\r\n" + ex.Message,
                    "Key in myself", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            r["EntrySource"] = "MANUAL";
            r["HasConflict"] = false;
            ShowStagedDate(r, imk, y, m);
            this.GridViewReadings.RefreshData();
            this.BtnTakeOver.Enabled = false;
            this.LblDetailFoot.Text = "That reading is yours now -- type over Last Audit Date if the machine was read on another day.";
        }

        /// <summary>Ctrl+Shift+U: the TEST Fetch JSON for the contract of the invoice picked on the
        /// left, shown to copy. Nothing is fetched or saved -- paste it into TEST Fetch
        /// (Ctrl+Shift+T on the Meters view).</summary>
        private void ShowTestFetchJson()
        {
            InvoiceRunItem it = _current;
            if (it == null || _rows == null)
            {
                XtraMessageBox.Show("Pick an invoice on the left first - the JSON is written for its contract.",
                    "TEST JSON", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // The rows of the month this invoice belongs to (the overdue view holds several months).
            DataTable rows = _rows;
            int y = it.Year > 0 ? it.Year : SelectedYear(), m = it.Month > 0 ? it.Month : SelectedMonth();
            if (it.Rows.Count > 0 && it.Rows[0].Table != null) rows = it.Rows[0].Table;
            string json = ScpInvoiceRun.TestFetchJson(rows, it.ContractKey, y, m);
            if (json.Length == 0)
            {
                XtraMessageBox.Show(it.ContractNo + " has no black or colour meter to read.", "TEST JSON",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (XtraForm dlg = new XtraForm())
            {
                dlg.Text = "TEST JSON - " + it.ContractNo;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new System.Drawing.Size(760, 420);
                dlg.MinimizeBox = false;

                LabelControl hint = new LabelControl();
                hint.AutoSizeMode = LabelAutoSizeMode.None;
                hint.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
                hint.Appearance.Options.UseTextOptions = true;
                hint.Location = new System.Drawing.Point(10, 8);
                hint.Size = new System.Drawing.Size(740, 34);
                hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                hint.Text = "Copy this into TEST Fetch (Ctrl+Shift+T on the Meters view). TotalBK / TotalCL are the " +
                    "machine's lifetime counters - here the last reading + 100; change them to test another month.";
                dlg.Controls.Add(hint);

                MemoEdit memo = new MemoEdit();
                memo.Location = new System.Drawing.Point(10, 46);
                memo.Size = new System.Drawing.Size(740, 326);
                memo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                memo.Properties.Appearance.Font = new System.Drawing.Font("Consolas", 9F);
                memo.Properties.WordWrap = false;
                memo.Properties.ScrollBars = ScrollBars.Both;
                memo.Text = json;
                dlg.Controls.Add(memo);

                SimpleButton bCopy = new SimpleButton();
                bCopy.Text = "Copy";
                bCopy.Size = new System.Drawing.Size(100, 26);
                bCopy.Location = new System.Drawing.Point(10, 382);
                bCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                bCopy.Click += delegate
                {
                    try { Clipboard.SetText(memo.Text); bCopy.Text = "Copied"; } catch { }
                };
                dlg.Controls.Add(bCopy);

                SimpleButton bClose = new SimpleButton();
                bClose.Text = "Close";
                bClose.Size = new System.Drawing.Size(90, 26);
                bClose.Location = new System.Drawing.Point(660, 382);
                bClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                bClose.DialogResult = DialogResult.Cancel;
                dlg.Controls.Add(bClose);
                dlg.CancelButton = bClose;
                dlg.ShowDialog(this);
            }
        }

        /// <summary>The invoice the picked row was billed on: deleted from AutoCount, its readings
        /// released, and the row goes back to Ready.</summary>
        private void DeleteSelectedInvoice()
        {
            InvoiceRunItem it = _current;
            if (it == null || it.Status != InvoiceRunItem.INVOICED || it.InvoicedDocNo.Trim().Length == 0)
            {
                XtraMessageBox.Show("Pick an invoice that has been made — this one is not billed yet.",
                    "Delete invoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string docNo = it.InvoicedDocNo.Trim();
            if (RefusedForLaterMonth(new string[] { docNo }, "Delete invoice")) return;
            if (XtraMessageBox.Show(
                    "Delete invoice " + docNo + " for " + (it.Customer.Length > 0 ? it.Customer : it.DebtorCode) + "?" +
                    Environment.NewLine + Environment.NewLine +
                    "Any payment, credit note or refund knocked off it is deleted first, and the meter " +
                    "readings it billed are released, so " + PeriodName() + " can be billed again." +
                    Environment.NewLine + "This cannot be undone.",
                    "Delete invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            List<string> one = new List<string>();
            one.Add(docNo);
            RunDelete(one, "Delete invoice");
        }

        /// <summary>Every invoice on the list as it stands — the month, the day and the filters that
        /// are showing, and nothing else.</summary>
        private void DeleteAllListedInvoices()
        {
            List<string> docs = new List<string>();
            foreach (InvoiceRunItem it in _items)
            {
                string no = it.InvoicedDocNo.Trim();
                if (it.Status == InvoiceRunItem.INVOICED && no.Length > 0 && !docs.Contains(no)) docs.Add(no);
            }
            if (docs.Count == 0)
            {
                XtraMessageBox.Show("Nothing on this list has been invoiced yet.", "Delete invoices",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (RefusedForLaterMonth(docs.ToArray(), "Delete invoices")) return;
            if (XtraMessageBox.Show(
                    "Delete all " + docs.Count + (docs.Count == 1 ? " invoice" : " invoices") +
                    " on this list (" + PeriodName() + ", the " + Ordinal(SelectedDay()) + ")?" +
                    Environment.NewLine + Environment.NewLine +
                    "Any payment, credit note or refund knocked off them is deleted first, and every " +
                    "reading they billed is released so the month can be run again." +
                    Environment.NewLine + "This cannot be undone.",
                    "Delete invoices", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            RunDelete(docs, "Delete invoices");
        }

        /// <summary>A month cannot come off while a later one is invoiced: said before the question is
        /// asked, so nobody confirms a delete that is going to be refused.</summary>
        private bool RefusedForLaterMonth(string[] docNos, string title)
        {
            try
            {
                List<string> wanted = new List<string>(docNos);
                DataTable invoices = ScpInvoiceDelete.ByDocNo(_db, wanted);
                if (invoices.Rows.Count == 0) return false;
                List<string> later = ScpInvoiceDelete.LaterInvoices(_db, invoices);
                if (later.Count == 0) return false;
                XtraMessageBox.Show(ScpInvoiceDelete.BlockedMessage(later), title,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            catch { return false; }   // the delete itself checks again
        }

        private void RunDelete(List<string> docNos, string title)
        {
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            ScpDeleteResult res;
            try
            {
                DataTable invoices = ScpInvoiceDelete.ByDocNo(_db, docNos);
                if (invoices.Rows.Count == 0)
                {
                    XtraMessageBox.Show("Those invoices are no longer in the book — the list is being read again.",
                        title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ScpBillingRows.ReconcileDeletedInvoices(_db);
                    LoadData();
                    return;
                }
                res = ScpInvoiceDelete.Delete(_db, invoices, false);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally { this.Cursor = old; }
            LoadData();
            XtraMessageBox.Show(res.Summary, title, MessageBoxButtons.OK,
                res.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        private void Chip_Changed(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (b == null || !b.Checked) return;
            ApplyFilter();
            ShowDetail(FocusedItem());
        }
        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; LoadData(); }
        }
        private void ChkMissingOnly_Changed(object sender, EventArgs e) { if (!_suppress) BindDetail(_current); }

        private void BtnSetting_Click(object sender, EventArgs e)
        {
            using (MeterReadingSetting_Form dlg = new MeterReadingSetting_Form(_db, SelectedYear(), SelectedMonth()))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
            }
            int keep = SelectedDay();
            try { _days = ScpInvoiceRun.DaysInUse(_db); } catch { }
            BuildDayStrip();
            _suppress = true;
            bool found = false;
            foreach (CheckButton b in _dayButtons) if (Convert.ToInt32(b.Tag) == keep) { b.Checked = true; found = true; }
            _suppress = false;
            if (!found) PickDefaultDay();
            LoadData();
        }

        // ───────────────────────────── the two views ─────────────────────────────

        /// <summary>Invoices or Meters. The Meters view IS the Meter Reading Integration screen,
        /// hosted inside this one untouched -- Fetch, manual key-in, the tabs, all of it -- so the
        /// clerk reads the meters there and comes back here to send what is ready.</summary>
        private void View_Changed(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (_suppress || b == null || !b.Checked) return;
            PostReadings();
            if (b == this.BtnViewMeters) ShowMetersView(); else ShowInvoicesView();
        }

        private void ShowMetersView()
        {
            if (!EnsureMeters()) return;
            SyncMetersPeriod();
            this.PanelFilter.Visible = false;
            this.SplitMain.Visible = false;
            this.PanelStatus.Visible = false;
            this.PanelMeters.Visible = true;
            this.LblViewHint.Text = "Fetch or key in readings here, then switch back to Invoices to generate.";
        }

        /// <summary>The hosted list exists (built on first use) -- true when it is there to use.</summary>
        private bool EnsureMeters()
        {
            if (_db == null) return false;
            if (_meters == null)
            {
                Cursor old = this.Cursor;
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    _meters = _userSession != null
                        ? new MeterReadingIntegration_Form(_userSession)
                        : new MeterReadingIntegration_Form(_db);
                    _meters.ShowSelectColumn = false;   // ticking is for the Invoices view; takes effect on its next load
                    _meters.AllowRowDoubleClickDetail = false;   // readings are keyed in the grid here, not in a dialog
                    _meters.TopLevel = false;
                    _meters.FormBorderStyle = FormBorderStyle.None;
                    _meters.WindowState = FormWindowState.Normal;
                    _meters.Dock = DockStyle.Fill;
                    // Its own green header would sit under ours; the rest of it is exactly what it is.
                    Control[] hdr = _meters.Controls.Find("PanelHeaderTop", true);
                    if (hdr.Length > 0) hdr[0].Visible = false;
                    // Generating is this screen's job, from the Invoices view. The list keeps its
                    // Generate logic untouched; only its button is out of sight here.
                    Control[] gen = _meters.Controls.Find("BtnGenerateInvoice", true);
                    if (gen.Length > 0) gen[0].Visible = false;
                    // Select All ticks rows for Generate; with neither shown here it would tick
                    // nothing anyone can see. Hidden too, and the buttons to its right slide left
                    // so the toolbar has no hole where the two used to be.
                    Control[] sel = _meters.Controls.Find("BtnSelfManualKeyIn", true);
                    if (sel.Length > 0) sel[0].Visible = false;
                    CloseToolbarGap(gen.Length > 0 ? gen[0] : null, sel.Length > 0 ? sel[0] : null);
                    // Reload once so the hidden tick column takes effect on every tab.
                    Control[] refresh = _meters.Controls.Find("BtnRefresh", true);
                    SimpleButton refreshBtn = refresh.Length > 0 ? refresh[0] as SimpleButton : null;
                    if (refreshBtn != null) refreshBtn.PerformClick();
                    this.PanelMeters.Controls.Add(_meters);
                    _meters.Show();
                }
                catch (Exception ex)
                {
                    this.Cursor = old;
                    _meters = null;
                    XtraMessageBox.Show("Could not open the meter list:\r\n" + ex.Message, "Meter Invoice Run", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _suppress = true; this.BtnViewInvoices.Checked = true; _suppress = false;
                    return false;
                }
                this.Cursor = old;
            }
            return true;
        }

        /// <summary>Fetch from the Invoices view: the list screen's own Fetch is pressed, and the
        /// Meters view comes to the front so its progress, and any conflicts it raises, are seen.
        /// When the fetch ends the invoice list is recounted, whichever view is in front.</summary>
        private void BtnFetch_Click(object sender, EventArgs e)
        {
            PostReadings();
            if (!EnsureMeters()) return;
            Control[] fb = _meters.Controls.Find("BtnFetch", true);
            SimpleButton fetch = fb.Length > 0 ? fb[0] as SimpleButton : null;
            if (fetch == null)
            {
                XtraMessageBox.Show("The meter list has no Fetch button to press.", "Fetch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _suppress = true;
            this.BtnViewMeters.Checked = true;   // View_Changed is suppressed; shown by hand below
            _suppress = false;
            ShowMetersView();
            this.LblViewHint.Text = "Fetching... switch back to Invoices when it is done - the list will already be recounted.";
            fetch.PerformClick();
            WatchFetch(fetch);
        }

        private void WatchFetch(SimpleButton fetch)
        {
            if (_fetchWatch == null)
            {
                _fetchWatch = new Timer();
                _fetchWatch.Interval = 1000;
                _fetchWatch.Tick += new EventHandler(FetchWatch_Tick);
            }
            _fetchWatch.Tag = fetch;
            _fetchWatch.Start();
        }

        private void FetchWatch_Tick(object sender, EventArgs e)
        {
            SimpleButton fetch = _fetchWatch.Tag as SimpleButton;
            if (fetch == null || fetch.IsDisposed) { _fetchWatch.Stop(); return; }
            if (!fetch.Enabled) return;   // the list disables its Fetch for the whole run
            _fetchWatch.Stop();
            this.LblViewHint.Text = "Fetch finished. Switch back to Invoices to generate.";
            if (this.PanelMeters.Visible) _fetchDone = true;   // recount when the clerk comes back
            else LoadData();
        }

        private bool _fetchDone;

        private void ShowInvoicesView()
        {
            this.PanelMeters.Visible = false;
            this.PanelFilter.Visible = true;
            this.SplitMain.Visible = true;
            this.PanelStatus.Visible = true;
            this.LblViewHint.Text = "Fetch or key in readings on the Meters view, then come back here to generate the invoices.";
            if (_meters != null) LoadData();   // readings fetched or keyed over there are counted here
            _fetchDone = false;
        }

        /// <summary>Slide every control on the hidden buttons' row left by the width they took, so
        /// Refresh - Fetch - Setting sit together instead of around two blanks.</summary>
        private static void CloseToolbarGap(Control gen, Control sel)
        {
            Control anchor = sel ?? gen;
            if (anchor == null || anchor.Parent == null) return;
            int gap = 8;
            int removed = 0;
            int firstLeft = int.MaxValue;
            if (sel != null) { removed += sel.Width + gap; firstLeft = Math.Min(firstLeft, sel.Left); }
            if (gen != null) { removed += gen.Width + gap; firstLeft = Math.Min(firstLeft, gen.Left); }
            List<Control> row = new List<Control>();
            // Visible is not asked: the hosted form is not shown yet when this runs, and every child
            // answers "no" until it is. The two hidden buttons are excluded by identity instead.
            foreach (Control c in anchor.Parent.Controls)
                if (c != gen && c != sel && Math.Abs(c.Top - anchor.Top) <= 16 && c.Left > firstLeft) row.Add(c);
            foreach (Control c in row) c.Left -= removed;
        }

        /// <summary>Point the hosted list at the month and day this screen is on. Best effort:
        /// the list's own controls are reached by name, and a miss leaves it where it was.</summary>
        private void SyncMetersPeriod()
        {
            if (_meters == null) return;
            try
            {
                Control[] cm = _meters.Controls.Find("CmbMonth", true);
                Control[] cd = _meters.Controls.Find("CmbDay", true);
                ComboBoxEdit month = cm.Length > 0 ? cm[0] as ComboBoxEdit : null;
                ComboBoxEdit dayCombo = cd.Length > 0 ? cd[0] as ComboBoxEdit : null;
                if (month != null && month.SelectedIndex != SelectedMonth() - 1) month.SelectedIndex = SelectedMonth() - 1;
                if (dayCombo != null)
                {
                    int idx = dayCombo.Properties.Items.IndexOf(SelectedDay());
                    if (idx >= 0 && dayCombo.SelectedIndex != idx) dayCombo.SelectedIndex = idx;
                }
            }
            catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { if (_fetchWatch != null) { _fetchWatch.Stop(); _fetchWatch.Dispose(); _fetchWatch = null; } } catch { }
            try { if (_meters != null) { _meters.Close(); _meters.Dispose(); _meters = null; } } catch { }
            base.OnFormClosed(e);
        }

        // ───────────────────────────── double-click: the contract, the invoice ─────────────────────────────

        /// <summary>The column decides where you land: the contract code opens the contract, the
        /// invoice number (an Invoiced row's Status) opens the invoice. Same rule as the list screen.</summary>
        private void GridViewInvoices_DoubleClick(object sender, EventArgs e)
        {
            Point pt = this.GridInvoices.PointToClient(Control.MousePosition);
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = this.GridViewInvoices.CalcHitInfo(pt);
            if (hit == null || !hit.InRowCell || hit.Column == null) return;
            DataRow r = this.GridViewInvoices.GetDataRow(hit.RowHandle);
            if (r == null) return;
            if (hit.Column == this.ColContract || hit.Column == this.ColKind || hit.Column == this.ColDetail)
            {
                OpenContractByKey(r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]), Convert.ToString(r["ContractNo"]));
                return;
            }
            if (hit.Column == this.ColStatus || hit.Column == this.ColAmount)
            {
                InvoiceRunItem it;
                if (_byKey.TryGetValue(Convert.ToString(r["JobKey"]), out it) && it.Status == InvoiceRunItem.INVOICED && it.InvoicedDocNo.Length > 0)
                    OpenInvoiceByDocNo(it.InvoicedDocNo);
            }
        }

        private void OpenContractByKey(long contractKey, string contractNo)
        {
            if (_db == null) return;
            if (contractKey <= 0)
            {
                XtraMessageBox.Show("This row is not linked to a contract" + (contractNo.Length > 0 ? " (" + contractNo + ")" : "") + ".",
                    "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form f =
                    new ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form(_db, contractKey);
                f.Show();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open contract " + contractNo + ":\r\n" + ex.Message, "Open Contract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenInvoiceByDocNo(string docNo)
        {
            try
            {
                object k = _db.ExecuteScalar("SELECT DocKey FROM dbo.IV WHERE DocNo = N'" + docNo.Replace("'", "''") + "'");
                if (k == null || k == DBNull.Value)
                { XtraMessageBox.Show("Invoice " + docNo + " not found - it may have been deleted.", "Open Invoice"); return; }
                long docKey = Convert.ToInt64(k);
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(AutoCount.Authentication.UserSession.CurrentUserSession, _db);
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(docKey);
                if (doc == null)
                { XtraMessageBox.Show("Invoice " + docNo + " could not be opened.", "Open Invoice"); return; }
                using (AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry f = new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc))
                {
                    f.ShowDialog(this);
                }
                LoadData();   // it may have been edited, cancelled or deleted in there
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Could not open the invoice:\r\n" + ex.Message, "Open Invoice"); }
        }

        // ───────────────────────────── history, auto-save, source ─────────────────────────────

        /// <summary>The same Reading History dialog the list screen opens from its Invoiced tab,
        /// for the machine under the cursor (or the invoice's first machine).</summary>
        private void BtnHistory_Click(object sender, EventArgs e)
        {
            if (_db == null || _current == null) return;
            DataRow r = this.GridViewReadings.GetFocusedDataRow();
            if (r == null && _current.Rows.Count > 0) r = _current.Rows[0];
            if (r == null) return;
            try
            {
                using (MeterReadingHistory_Form h = new MeterReadingHistory_Form(_db,
                    r["ItemKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemKey"]),
                    Convert.ToString(r["ServiceItemNo"]), Convert.ToString(r["SerialNo"]),
                    _current.Status == InvoiceRunItem.INVOICED ? _current.InvoicedDocNo : Convert.ToString(r["LastInvNo"])))
                {
                    h.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open the reading history:\r\n" + ex.Message, "Reading History", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Whatever is half-typed in the reading cell is committed -- and so saved -- the
        /// moment the grid is left, a view is switched, or the form closes. Nobody has to press Enter.</summary>
        private void PostReadings()
        {
            try
            {
                if (this.GridViewReadings.IsEditing) this.GridViewReadings.PostEditor();
                this.GridViewReadings.CloseEditor();
            }
            catch { }
        }

        private void GridReadings_Leave(object sender, EventArgs e) { PostReadings(); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            PostReadings();
            base.OnFormClosing(e);
        }

        /// <summary>Where a reading came from, in words: keyed here or on the list, fetched from the
        /// meter API (online or offline machine), or carried by an invoice.</summary>
        private void GridViewReadings_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column != this.ColRSource) return;
            string s = e.Value == null || e.Value == DBNull.Value ? "" : Convert.ToString(e.Value).Trim().ToUpperInvariant();
            if (s == "MANUAL") e.DisplayText = "Keyed";
            else if (s == "ONLINE") e.DisplayText = "Fetched (online)";
            else if (s == "OFFLINE") e.DisplayText = "Fetched (offline)";
            else if (s == "INVOICE") e.DisplayText = "From invoice";
            else if (s == "INTERBILL") e.DisplayText = "From other book";
            else if (s == "WAIVE-AUTO" || s == "RENTAL FREE") e.DisplayText = "auto";
            else if (s.Length == 0) e.DisplayText = "";
            else e.DisplayText = s;
        }

        // ───────────────────────────── events: the invoice grid ─────────────────────────────

        private void GridViewInvoices_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading) return;
            ShowDetail(FocusedItem());
        }

        private void GridViewInvoices_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Only the tick is editable, and an invoiced row has nothing left to tick.
            if (this.GridViewInvoices.FocusedColumn != this.ColSel) { e.Cancel = true; return; }
            DataRow r = this.GridViewInvoices.GetFocusedDataRow();
            if (r != null && Convert.ToInt32(r["Status"]) == InvoiceRunItem.INVOICED) e.Cancel = true;
        }

        private void RepoSel_EditValueChanged(object sender, EventArgs e)
        {
            this.GridViewInvoices.PostEditor();
            this.GridViewInvoices.CloseEditor();
        }

        private void GridViewInvoices_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == this.ColSel) UpdateSummary();
        }

        private void GridViewInvoices_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            DataRow r = this.GridViewInvoices.GetDataRow(e.RowHandle);
            if (r == null) return;
            int st = Convert.ToInt32(r["Status"]);
            if (r.Table.Columns.Contains("Overdue") && r["Overdue"] != DBNull.Value && Convert.ToBoolean(r["Overdue"]))
            {
                e.Appearance.BackColor = LATE_BACK;
                e.Appearance.BackColor2 = LATE_BACK;
                e.Appearance.ForeColor = LATE_FORE;
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            if (st == InvoiceRunItem.READY)
            {
                e.Appearance.BackColor = READY_BACK; e.Appearance.BackColor2 = READY_BACK;
                e.Appearance.Options.UseBackColor = true;
            }
            else if (st == InvoiceRunItem.WAITING || st == InvoiceRunItem.UNPRICED || st == InvoiceRunItem.NO_ITEM || st == InvoiceRunItem.BACKWARD)
            {
                e.Appearance.BackColor = WAIT_BACK; e.Appearance.BackColor2 = WAIT_BACK;
                e.Appearance.Options.UseBackColor = true;
            }
            else
            {
                e.Appearance.ForeColor = DONE_FORE;
                e.Appearance.Options.UseForeColor = true;
            }
        }

        // ───────────────────────────── events: the readings grid ─────────────────────────────

        private void GridViewReadings_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataRow r = this.GridViewReadings.GetFocusedDataRow();
            // The day a KEYED reading was taken is the operator's to correct (feedback ATP-5): the
            // machine was read on the 10th even if the reading reached the screen on the 23rd, and
            // that date is what the invoice prints and what next period counts from. Everything the
            // row is not allowed to change is named by WhyDateFixed, which the tooltip repeats.
            if (this.GridViewReadings.FocusedColumn == this.ColRLastAudit)
            {
                if (r == null || ScpInvoiceRun.WhyDateFixed(r).Length > 0) e.Cancel = true;
                return;
            }
            // PUMS's report no. is shown, never typed: the Reference No is the invoice's, typed once
            // under Invoice date (feedback ATP-8, 26/9).
            if (this.GridViewReadings.FocusedColumn == this.ColRRef)
            {
                e.Cancel = true;
                return;
            }
            if (this.GridViewReadings.FocusedColumn != this.ColRCurrent) { e.Cancel = true; return; }
            if (r == null || ScpInvoiceRun.IsInvoiced(r) || !ScpInvoiceRun.IsUsageMeter(r)) e.Cancel = true;
        }

        private void GridViewReadings_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column == this.ColRRef && e.RowHandle >= 0)
            {
                DataRow rr = this.GridViewReadings.GetDataRow(e.RowHandle);
                if (rr != null && ScpInvoiceRun.WhyRefFixed(rr).Length == 0)
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 249, 219);
                    e.Appearance.Options.UseBackColor = true;
                }
                return;
            }
            if (e.Column == this.ColRLastAudit && e.RowHandle >= 0)
            {
                // Same pale yellow the Current Reading uses: this cell can be typed in. Only the
                // rows that may be corrected get it, so the ones that may not look inert.
                DataRow dr = this.GridViewReadings.GetDataRow(e.RowHandle);
                if (dr != null && ScpInvoiceRun.WhyDateFixed(dr).Length == 0)
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 249, 219);
                    e.Appearance.Options.UseBackColor = true;
                }
                return;
            }
            if (e.Column != this.ColRCurrent || e.RowHandle < 0) return;
            DataRow r = this.GridViewReadings.GetDataRow(e.RowHandle);
            if (r == null) return;
            if (!ScpInvoiceRun.IsUsageMeter(r) || ScpInvoiceRun.IsInvoiced(r))
            {
                e.Appearance.BackColor = Color.FromArgb(235, 235, 235);
                e.Appearance.ForeColor = Color.DimGray;
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
                return;
            }
            if (!ScpInvoiceRun.HasReading(r))
            {
                e.Appearance.BackColor = WAIT_CELL;
                e.Appearance.ForeColor = Color.FromArgb(102, 60, 0);
                e.Appearance.FontStyleDelta = FontStyle.Bold;
                e.Appearance.Options.UseBackColor = true;
                e.Appearance.Options.UseForeColor = true;
            }
        }

        private void GridViewReadings_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (_db == null) return;
            if (e.Column == this.ColRLastAudit)
            {
                ReadingDateChanged(this.GridViewReadings.GetDataRow(e.RowHandle), e.Value, e.RowHandle);
                return;
            }
            if (e.Column == this.ColRRef)
            {
                ReadingRefChanged(this.GridViewReadings.GetDataRow(e.RowHandle), e.Value);
                return;
            }
            if (e.Column != this.ColRCurrent) return;
            DataRow r = this.GridViewReadings.GetDataRow(e.RowHandle);
            if (r == null) return;
            decimal v = 0m;
            if (e.Value != null && e.Value != DBNull.Value) decimal.TryParse(Convert.ToString(e.Value), out v);
            int y, m;
            WorkPeriod(out y, out m);
            long imk = r["ItemMeterKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemMeterKey"]);
            try
            {
                ScpInvoiceRun.SaveReading(_db, imk, y, m, v);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The reading was not saved:\r\n" + ex.Message, "Meter Invoice Run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try { ScpBillingRows.PrefillFromStaging(_db, _rows, m, y, _ladders); } catch { }
                return;
            }
            r["CurrentReading"] = v;
            r["EntrySource"] = v > 0m ? "MANUAL" : "";
            // Show the date the reading now carries, read back rather than assumed: the same value
            // saved again keeps a hand-set date, a different value takes a fresh stamp.
            ShowStagedDate(r, imk, y, m);
            ScpBillingRows.Recalc(r, _ladders, y, m);
            r[ScpInvoiceRun.COL_NEEDS] = ScpInvoiceRun.IsUsageMeter(r) && !ScpInvoiceRun.HasReading(r) && !ScpInvoiceRun.IsInvoiced(r);

            // A new reading moves the contract's terms too: the committed minimum's top-up, the waive.
            long ck = r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]);
            HashSet<long> thisContract = new HashSet<long>();
            thisContract.Add(ck);
            ScpInvoiceRun.PriceTerms(_db, _rows, _ladders, y, m, thisContract);

            // Every invoice of the contract is recounted, not only this row's: a waive can sit on
            // the rental invoice while the copies it depends on are on another.
            InvoiceRunItem it;
            _byKey.TryGetValue(Convert.ToString(r[ScpInvoiceRun.COL_JOBKEY]), out it);
            foreach (InvoiceRunItem other in _byKey.Values)
            {
                if (other.ContractKey != ck) continue;
                ScpInvoiceRun.Refresh(other);
                DataRow ir = ItemRow(other.JobKey);
                if (ir != null) FillItemRow(ir, other);
                if (_current == other) { RefreshFacts(other); this.BtnPreview.Enabled = other.Status == InvoiceRunItem.READY; }
            }
            this.GridViewReadings.RefreshData();
            if (it != null && it.Status == InvoiceRunItem.READY && this.ChkMissingOnly.Checked)
                this.LblDetailFoot.Text = "That was the last one. This invoice is Ready and goes out with Generate all ready.";
            UpdateSummary();
            this.GridViewInvoices.LayoutChanged();
        }

        /// <summary>The month a reading, its date or its reference belongs to: the month of the invoice
        /// shown, which in the overdue view is not the month the pickers show (#41, 29/9: an April
        /// reading keyed there was saved under the pickers' month while its row turned Ready).</summary>
        private void WorkPeriod(out int year, out int month)
        {
            year = _current != null && _current.Year > 0 ? _current.Year : SelectedYear();
            month = _current != null && _current.Month > 0 ? _current.Month : SelectedMonth();
        }

        /// <summary>Put the staged reading date (and whether a person set it) back on the row.</summary>
        private void ShowStagedDate(DataRow r, long itemMeterKey, int year, int month)
        {
            try
            {
                bool edited;
                string reference;
                DateTime? when = ScpInvoiceRun.StagedDate(_db, itemMeterKey, year, month, out edited, out reference);
                if (when.HasValue) r["LastAuditDate"] = when.Value; else r["LastAuditDate"] = DBNull.Value;
                if (r.Table.Columns.Contains("DateEdited")) r["DateEdited"] = edited;
                if (r.Table.Columns.Contains("TrackingId")) r["TrackingId"] = reference;
            }
            catch { }
        }

        // The day a keyed reading was taken (feedback ATP-5). The reading and its money do not
        // move; the date does, and with it the date the invoice prints and the date the meter
        // transaction carries -- which is next period's "Last Read Date".
        private void ReadingDateChanged(DataRow r, object value, int rowHandle)
        {
            if (r == null || _db == null) return;
            int y, m;
            WorkPeriod(out y, out m);
            long imk = r["ItemMeterKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemMeterKey"]);
            string no = ScpInvoiceRun.WhyDateFixed(r);
            if (no.Length > 0)
            {
                XtraMessageBox.Show(no, "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            if (value == null || value == DBNull.Value)
            {
                XtraMessageBox.Show("A reading has to carry the day it was taken -- pick a date.",
                    "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            DateTime when = Convert.ToDateTime(value);
            string wrong = ScpInvoiceRun.WhyDateWrong(r, when);
            if (wrong.Length > 0)
            {
                XtraMessageBox.Show(wrong, "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            try
            {
                ScpInvoiceRun.SaveReadingDate(_db, imk, y, m, when.Date);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The date was not saved:\r\n" + ex.Message,
                    "Reading date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            ShowStagedDate(r, imk, y, m);
            // The money is untouched -- usage is reading minus reading, and no charge is worked out
            // from a date -- so nothing is repriced here. What DOES follow the date is the invoice's
            // printed period, so the facts pane is refreshed.
            InvoiceRunItem it;
            if (_byKey.TryGetValue(Convert.ToString(r[ScpInvoiceRun.COL_JOBKEY]), out it) && _current == it)
                RefreshFacts(it);
            this.GridViewReadings.RefreshData();
        }

        // The reading's reference (feedback ATP-8). Written to every keyed meter of the machine
        // for the period -- one slip covers the black and the colour counter -- and shown on those
        // rows at once. At Generate it becomes the invoice's Ref exactly as a PUMS report id does.
        private void ReadingRefChanged(DataRow r, object value)
        {
            if (r == null || _db == null) return;
            int y, m;
            WorkPeriod(out y, out m);
            long imk = r["ItemMeterKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemMeterKey"]);
            long ik = r["ItemKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemKey"]);
            string refNo = value == null || value == DBNull.Value ? "" : Convert.ToString(value).Trim();
            string no = ScpInvoiceRun.WhyRefFixed(r);
            if (no.Length == 0 && refNo.Length > ScpInvoiceRun.REF_MAX)
                no = "A reference can be at most " + ScpInvoiceRun.REF_MAX + " characters -- that is all the invoice's Ref can hold.";
            if (no.Length > 0)
            {
                XtraMessageBox.Show(no, "Reference No", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            try
            {
                ScpInvoiceRun.SaveReadingRef(_db, ik, y, m, refNo);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The reference was not saved:\r\n" + ex.Message,
                    "Reference No", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowStagedDate(r, imk, y, m);
                this.GridViewReadings.RefreshData();
                return;
            }
            // The machine's other keyed meters took it too -- show it where it now is.
            foreach (DataRow o in _rows.Rows)
            {
                if (o["ItemKey"] == DBNull.Value || Convert.ToInt64(o["ItemKey"]) != ik) continue;
                if (ScpInvoiceRun.IsInvoiced(o)) continue;
                if (Convert.ToString(o["EntrySource"]).Trim().ToUpperInvariant() != "MANUAL") continue;
                o["TrackingId"] = refNo;
            }
            this.GridViewReadings.RefreshData();
        }

        // ─────────────────────── the invoice date (feedback ATP-6 / ATP-9) ───────────────────────

        // The due date is the default, not the rule: a machine that broke down on the 15th is billed
        // for the 1st-14th and the invoice is dated the 14th. Any day in the same month; the list
        // shows it (in blue when moved) and Generate uses it, for this invoice or a batch.
        private void DtInvDate_EditValueChanged(object sender, EventArgs e)
        {
            if (_suppress || _current == null) return;
            InvoiceRunItem it = _current;
            object v = this.DtInvDate.EditValue;
            if (v == null || v == DBNull.Value)
            {
                // Cleared: back to the default.
                SetDocDate(it, null);
                return;
            }
            DateTime when = Convert.ToDateTime(v).Date;
            string wrong = ScpInvoiceRun.WhyDocDateWrong(it, when);
            if (wrong.Length > 0)
            {
                XtraMessageBox.Show(wrong, "Invoice date", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suppress = true;
                this.DtInvDate.EditValue = ScpInvoiceRun.EffectiveDocDate(it);
                _suppress = false;
                return;
            }
            SetDocDate(it, when == ScpInvoiceRun.DefaultDocDate(it).Date ? (DateTime?)null : when);
        }

        private void SetDocDate(InvoiceRunItem it, DateTime? when)
        {
            it.DocDateOverride = when;
            string key = DocDateKey(it);
            if (when.HasValue) _docDates[key] = when.Value; else _docDates.Remove(key);
            _suppress = true;
            this.DtInvDate.EditValue = ScpInvoiceRun.EffectiveDocDate(it);
            _suppress = false;
            DataRow ir = ItemRow(it.JobKey);
            if (ir != null) FillItemRow(ir, it);
            this.GridViewInvoices.LayoutChanged();
            this.LblDetailFoot.Text = when.HasValue
                ? "This invoice will be dated " + when.Value.ToString("dd/MM/yyyy") + " instead of its due date " +
                  ScpInvoiceRun.DefaultDocDate(it).ToString("dd/MM/yyyy") + "."
                : "This invoice is dated its due date, " + ScpInvoiceRun.DefaultDocDate(it).ToString("dd/MM/yyyy") + ".";
        }

        // ─────────────────────── the invoice's Reference No (feedback ATP-8) ───────────────────────

        // One Reference No for the whole invoice, typed where its date is (user, 26/9: "reference no
        // is not one per meter, it is for the invoice"). Generate writes it to the invoice's Ref; left
        // empty, the Ref is what it always was -- PUMS's report no., else the machine or contract no.
        private void TxtInvRef_EditValueChanged(object sender, EventArgs e)
        {
            if (_suppress || _current == null) return;
            string typed = Convert.ToString(this.TxtInvRef.EditValue ?? "").Trim();
            if (typed.Length > ScpInvoiceRun.REF_MAX) typed = typed.Substring(0, ScpInvoiceRun.REF_MAX);
            _current.RefOverride = typed;
            string key = DocDateKey(_current);
            if (typed.Length > 0) _invRefs[key] = typed; else _invRefs.Remove(key);
        }

        private void BtnInvDateFromReading_Click(object sender, EventArgs e)
        {
            if (_current == null) return;
            DateTime? last = ScpInvoiceRun.LastReadingDate(_current);
            if (!last.HasValue)
            {
                XtraMessageBox.Show("None of this invoice's readings has a date yet -- key or fetch them first.",
                    "Invoice date", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Through the editor, so the same month check applies.
            this.DtInvDate.EditValue = last.Value;
        }

        private void GridViewInvoices_InvDateCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column != this.ColInvDate || e.RowHandle < 0) return;
            object moved = this.GridViewInvoices.GetRowCellValue(e.RowHandle, "InvDateChanged");
            if (moved != null && moved != DBNull.Value && Convert.ToBoolean(moved))
            {
                e.Appearance.ForeColor = Color.FromArgb(21, 101, 192);
                e.Appearance.FontStyleDelta = FontStyle.Bold;
                e.Appearance.Options.UseForeColor = true;
                e.Appearance.Options.UseFont = true;
            }
        }

        // ───────────────────────────── generate ─────────────────────────────

        private void BtnGenerateReady_Click(object sender, EventArgs e)
        {
            List<InvoiceRunItem> chosen = new List<InvoiceRunItem>();
            foreach (InvoiceRunItem it in _items) if (it.Status == InvoiceRunItem.READY) chosen.Add(it);
            Generate(chosen, "");
        }

        private void BtnGenerateSelected_Click(object sender, EventArgs e)
        {
            List<InvoiceRunItem> chosen = new List<InvoiceRunItem>();
            int leftOut = 0;
            if (_dtItems != null)
                foreach (DataRow r in _dtItems.Rows)
                {
                    if (r["Sel"] == DBNull.Value || !Convert.ToBoolean(r["Sel"])) continue;
                    InvoiceRunItem it;
                    if (!_byKey.TryGetValue(Convert.ToString(r["JobKey"]), out it)) continue;
                    if (it.Status == InvoiceRunItem.READY) chosen.Add(it); else leftOut++;
                }
            if (chosen.Count == 0)
            {
                XtraMessageBox.Show(leftOut > 0
                    ? "The ticked invoices are still waiting on readings (or already invoiced). Nothing can go out yet."
                    : "Tick the invoices to send, or use Generate all ready.", "Generate selected");
                return;
            }
            Generate(chosen, leftOut > 0 ? leftOut + " ticked invoice(s) are not ready and were left out." : "");
        }

        private void BtnGenerateThis_Click(object sender, EventArgs e)
        {
            if (_current == null || _current.Status != InvoiceRunItem.READY) return;
            List<InvoiceRunItem> chosen = new List<InvoiceRunItem>();
            chosen.Add(_current);
            Generate(chosen, "");
        }

        /// <summary>Send a set of invoices: mark their rows, hand them to the one place that
        /// turns rows into documents, show the same preview and progress the list screen shows.</summary>
        /// <summary>The overdue view holds invoices from several months, and a run belongs to one
        /// month: they go out a month at a time, oldest first, each with its own preview.</summary>
        private void Generate(List<InvoiceRunItem> chosen, string note)
        {
            if (_db == null || chosen == null || chosen.Count == 0) return;
            this.GridViewReadings.CloseEditor();
            this.GridViewInvoices.CloseEditor();

            if (RefusedForEarlierMonth(chosen)) return;

            List<string> order = new List<string>();
            Dictionary<string, List<InvoiceRunItem>> byMonth = new Dictionary<string, List<InvoiceRunItem>>();
            foreach (InvoiceRunItem it in chosen)
            {
                string k = MonthKey(it.Year, it.Month);
                if (!byMonth.ContainsKey(k)) { byMonth[k] = new List<InvoiceRunItem>(); order.Add(k); }
                byMonth[k].Add(it);
            }
            order.Sort(delegate (string a, string b)
            {
                MonthRows ma, mb;
                _months.TryGetValue(a, out ma);
                _months.TryGetValue(b, out mb);
                if (ma == null || mb == null) return 0;
                return (ma.Year * 100 + ma.Month).CompareTo(mb.Year * 100 + mb.Month);
            });
            foreach (string k in order)
            {
                MonthRows mr;
                if (!_months.TryGetValue(k, out mr)) continue;
                GenerateMonth(byMonth[k], mr, note);
            }
        }

        /// <summary>The months go out oldest first: said before anything is built, so nobody sits
        /// through a preview for a run that is going to be refused.</summary>
        private bool RefusedForEarlierMonth(List<InvoiceRunItem> chosen)
        {
            try
            {
                List<string> waiting = ScpInvoiceRun.EarlierUnbilledMonths(_db, chosen, OVERDUE_MONTHS_BACK);
                if (waiting.Count == 0) return false;
                XtraMessageBox.Show(ScpInvoiceRun.EarlierMonthsMessage(waiting), "Generate invoice",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            catch { return false; }   // never let the check itself stop a run
        }

        private void GenerateMonth(List<InvoiceRunItem> chosen, MonthRows month, string note)
        {
            if (month == null || month.Rows == null || chosen == null || chosen.Count == 0) return;
            _rows = month.Rows;
            _ladders = month.Ladders;

            HashSet<string> keys = new HashSet<string>();
            foreach (InvoiceRunItem it in chosen) keys.Add(it.JobKey);
            List<DataRow> all = new List<DataRow>();
            foreach (DataRow r in _rows.Rows)
            {
                r["Sel"] = keys.Contains(Convert.ToString(r[ScpInvoiceRun.COL_JOBKEY])) && !ScpInvoiceRun.IsInvoiced(r);
                all.Add(r);
            }

            int y = month.Year, m = month.Month;
            string monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(m) + " " + y;
            int alreadyInvoiced;
            Dictionary<long, string> runSnapshots;
            string blockTitle, blockMessage;
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs = ScpInvoiceJobs.Build(
                _db, _rows, all, _ladders, y, m, _grpMode,
                out alreadyInvoiced, out runSnapshots, out blockTitle, out blockMessage);
            if (jobs == null)
            {
                XtraMessageBox.Show(blockMessage, blockTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (jobs.Count == 0)
            {
                XtraMessageBox.Show("Nothing to generate for " + monthName + ".", "Meter Invoice Run");
                return;
            }
            // Feedback ATP-6 / ATP-9: an invoice whose date was moved goes out on that date. Every
            // moved date must find its invoice; one that does not stops the run rather than going out
            // quietly on the due date the operator changed.
            List<string> unplaced = new List<string>();
            foreach (InvoiceRunItem it in chosen)
            {
                if (!it.DocDateOverride.HasValue) continue;
                MeterInvoiceGenerator.InvoiceJob job;
                if (jobs.TryGetValue(it.JobKey, out job)) job.DocDate = it.DocDateOverride.Value;
                else unplaced.Add(it.ContractNo + " (" + it.DocDateOverride.Value.ToString("dd/MM/yyyy") + ")");
            }
            // Feedback ATP-8: the Reference No typed for an invoice is its Ref, whatever PUMS's report
            // numbers would have made it. One that cannot find its invoice stops the run, as a date does.
            foreach (InvoiceRunItem it in chosen)
            {
                if (string.IsNullOrEmpty(it.RefOverride)) continue;
                MeterInvoiceGenerator.InvoiceJob job;
                if (jobs.TryGetValue(it.JobKey, out job)) job.RefDocNo = it.RefOverride;
                else unplaced.Add(it.ContractNo + " (Reference No " + it.RefOverride + ")");
            }
            if (unplaced.Count > 0)
            {
                XtraMessageBox.Show("The invoice date set for " + string.Join(", ", unplaced.ToArray()) +
                    " could not be matched to its invoice, so nothing was generated. Refresh and set the date again.",
                    "Meter Invoice Run", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<MeterInvoiceGenerator.InvoiceJob> jobList = new List<MeterInvoiceGenerator.InvoiceJob>(jobs.Values);
            string intro = jobList.Count + (jobList.Count == 1 ? " invoice" : " invoices") + " for " + monthName +
                ". They are saved automatically — no clicking through each one." +
                (note.Length > 0 ? "  " + note : "");
            using (MeterInvoicePreview_Form pv = new MeterInvoicePreview_Form(_db, jobList, monthName, intro))
            {
                if (pv.ShowDialog(this) != DialogResult.OK) return;
                jobList = pv.Approved;
            }
            if (jobList.Count == 0) return;

            using (MeterInvoiceGenerateProgress_Form dlg =
                new MeterInvoiceGenerateProgress_Form(_db, jobList, DateTime.Today, DateTime.Now, y, m, runSnapshots))
            {
                dlg.ShowDialog(this);
            }
            LoadData();
        }
    }
}
