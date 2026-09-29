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
using ServiceContractPhotocopier.MeterReading.OperationForms;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Inter-Billing: HQ reads the meters, this book bills.
    ///
    /// <para>One board. HQ's contracts are listed with where each one stands this period -- not
    /// taken, changed at HQ, unpriced, waiting for HQ's reading, ready, invoiced. HQ's readings are
    /// read from HQ's book as the board loads; nothing is fetched or brought over by hand. Generate
    /// writes the readings it billed into this book, stamped INTERBILL, before the invoice is
    /// saved, so every invoice keeps its own meter record.</para>
    ///
    /// <para>Separate from the Meter Invoice Run: a contract that shares HQ's machines is billed here
    /// and nowhere else. Money comes from the same engine as every other screen.</para>
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Inter-Billing", MenuOrder = 240, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class InterBillBoard_Form : XtraForm
    {
        private DBSetting _db;
        private UserSession _userSession;
        private string _userId = "ADMIN";
        private ScpInterBillBook _book;
        private string _cs = "";
        private List<IbContractRow> _rows = new List<IbContractRow>();
        private DataTable _dtContracts;
        private IbContractRow _current;
        private bool _loading;
        private DataTable _pickTable;
        private string _pickBefore = "";
        private List<IbInvoiceRow> _invoices = new List<IbInvoiceRow>();
        private IbInvoiceRow _pickedInvoice;      // an invoice picked under a contract row, in either list
        private bool _invoiceView = true;
        private int _chipInvoicedLeft;
        private int _chipNotTakenLeft;
        private Control[] _toolbar;
        private int[] _toolbarLefts;
        private int _filterPanelHeight;
        private bool _suppress;

        private static readonly Color READY_BACK = Color.FromArgb(227, 242, 228);
        private static readonly Color WAIT_BACK = Color.FromArgb(255, 243, 207);
        private static readonly Color BAD_BACK = Color.FromArgb(251, 228, 225);
        private static readonly Color DONE_FORE = Color.FromArgb(108, 116, 112);
        private static readonly Color HQ_BACK = Color.FromArgb(227, 236, 248);

        public InterBillBoard_Form()
        {
            InitializeComponent();
            try { this.PanelHeaderTop.HintCtrl.Visible = false; } catch { }
            // The contract list carries ten columns. Widened here, after the layout exists, so the
            // right side's anchored buttons move with it instead of being measured against it.
            this.SplitMain.SplitterPosition = 760;
            ApplyButtonIcons();
            InitDefaults();
        }

        public InterBillBoard_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null)
            {
                _db = userSession.DBSetting;
                try { _userId = userSession.LoginUserID; } catch { }
            }
            Boot();
        }

        public InterBillBoard_Form(DBSetting dbSetting) : this()
        {
            _db = dbSetting;
            Boot();
        }

        // ───────────────────────────── set-up ─────────────────────────────

        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new SizeF(dpi, dpi));
                SetIcon(this.BtnRefresh, img.GetLargeImage_Refresh());
                SetIcon(this.BtnGenerateReady, img.GetLargeImage_New());
                SetIcon(this.BtnGenerateThis, img.GetLargeImage_New());
                SetIcon(this.BtnOpenContract, img.GetLargeImage_Inquiry());
            }
            catch { }
            try
            {
                DevExpress.Utils.Svg.SvgImage svg = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/setup/properties.svg");
                if (svg != null)
                {
                    this.BtnSetup.ImageOptions.SvgImage = svg;
                    this.BtnSetup.ImageOptions.SvgImageSize = new Size(24, 24);
                    this.BtnSetup.ImageOptions.ImageToTextIndent = 6;
                    this.BtnSetup.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
                    this.BtnSetup.ImageOptions.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
                }
            }
            catch { }
        }

        private static void SetIcon(SimpleButton b, Image img)
        {
            if (b == null || img == null) return;
            b.ImageOptions.Image = img;
            b.ImageOptions.ImageToTextIndent = 6;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
        }

        private void InitDefaults()
        {
            this.LblUpTo.Text = ScpBillingSequence.Name(UpTo());
            _chipInvoicedLeft = this.ChipInvoiced.Left;
            _chipNotTakenLeft = this.ChipNotTaken.Left;
            _toolbar = new Control[] { this.BtnRefresh, this.BtnGenerateReady, this.BtnSetup, this.LblShow,
                this.ChipAll, this.ChipReady, this.ChipWaiting, this.ChipChanged, this.ChipUnpriced, this.ChipNoItem, this.ChipNotTaken, this.ChipInvoiced };
            _toolbarLefts = new int[_toolbar.Length];
            for (int i = 0; i < _toolbar.Length; i++) _toolbarLefts[i] = _toolbar[i].Left;
            _filterPanelHeight = this.PanelFilter.Height;
            this.BtnViewInvoices.CheckedChanged += new EventHandler(View_Changed);
            this.BtnViewContracts.CheckedChanged += new EventHandler(View_Changed);
            this.GridViewInvoices.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewInvoices_FocusedRowChanged);
            this.GridViewInvoices.RowStyle += new RowStyleEventHandler(GridViewInvoices_RowStyle);
            this.GridViewInvoices.DoubleClick += new EventHandler(GridViewInvoices_DoubleClick);
            // Events on a pattern view reach every contract's expanded copy of it. Both lists use the same ones.
            foreach (GridView lines in new GridView[] { this.GridViewContractInvoices, this.GridViewInvoiceLines })
            {
                lines.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(DetailInvoice_FocusedRowChanged);
                lines.RowStyle += new RowStyleEventHandler(DetailInvoice_RowStyle);
                lines.DoubleClick += new EventHandler(DetailInvoice_DoubleClick);
            }
            this.GridContracts.FocusedViewChanged += new DevExpress.XtraGrid.ViewFocusEventHandler(Grid_FocusedViewChanged);
            this.GridInvoices.FocusedViewChanged += new DevExpress.XtraGrid.ViewFocusEventHandler(Grid_FocusedViewChanged);
            this.GridViewContracts.MasterRowEmpty += new MasterRowEmptyEventHandler(GridViewContracts_MasterRowEmpty);
            this.ChkShowEnded.CheckedChanged += new EventHandler(Period_Changed);
            this.PceCustomers.QueryPopUp += new System.ComponentModel.CancelEventHandler(PceCustomers_QueryPopUp);
            this.PceCustomers.Popup += new EventHandler(PceCustomers_Popup);
            this.PceCustomers.QueryResultValue += new DevExpress.XtraEditors.Controls.QueryResultValueEventHandler(PceCustomers_QueryResultValue);
            this.PceCustomers.Closed += new DevExpress.XtraEditors.Controls.ClosedEventHandler(PceCustomers_Closed);
            this.TxtCustomerSearch.EditValueChanged += new EventHandler(PickFilter_Changed);
            this.TxtCustomerSearch.KeyDown += new KeyEventHandler(TxtCustomerSearch_KeyDown);
            this.ChkTickedOnly.CheckedChanged += new EventHandler(PickFilter_Changed);
            this.BtnTickShown.Click += new EventHandler(BtnTickShown_Click);
            this.BtnUntickShown.Click += new EventHandler(BtnUntickShown_Click);
            this.BtnPickClear.Click += new EventHandler(BtnPickClear_Click);
            this.BtnPickOk.Click += new EventHandler(BtnPickOk_Click);
            this.GridViewCustomerPick.RowCellClick += new RowCellClickEventHandler(GridViewCustomerPick_RowCellClick);
            this.GridViewCustomerPick.KeyDown += new KeyEventHandler(GridViewCustomerPick_KeyDown);
            this.BtnRefresh.Click += new EventHandler(BtnRefresh_Click);
            this.BtnGenerateReady.Click += new EventHandler(BtnGenerateReady_Click);
            this.BtnGenerateThis.Click += new EventHandler(BtnGenerateThis_Click);
            this.BtnSetup.Click += new EventHandler(BtnSetup_Click);
            this.BtnOpenContract.Click += new EventHandler(BtnOpenContract_Click);
            this.BtnApply.Click += new EventHandler(BtnApply_Click);
            this.BtnIgnore.Click += new EventHandler(BtnIgnore_Click);
            this.BtnTake.Click += new EventHandler(BtnTake_Click);
            this.BtnSkipMonth.Click += new EventHandler(BtnSkipMonth_Click);
            this.BtnUndoSkip.Click += new EventHandler(BtnUndoSkip_Click);
            this.BtnBillFrom.Click += new EventHandler(BtnBillFrom_Click);
            foreach (CheckButton chip in Chips()) chip.CheckedChanged += new EventHandler(Chip_Changed);

            this.GridViewContracts.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewContracts_FocusedRowChanged);
            this.GridViewContracts.RowStyle += new RowStyleEventHandler(GridViewContracts_RowStyle);
            this.GridViewContracts.RowCellStyle += new RowCellStyleEventHandler(GridViewContracts_RowCellStyle);
            this.GridViewContracts.DoubleClick += new EventHandler(GridViewContracts_DoubleClick);
            this.GridViewMachines.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(GridViewMachines_FocusedRowChanged);
            this.GridViewMachines.RowStyle += new RowStyleEventHandler(GridViewMachines_RowStyle);
            this.GridViewReadings.RowCellStyle += new RowCellStyleEventHandler(GridViewReadings_RowCellStyle);
            this.GridViewReadings.ShowingEditor += new System.ComponentModel.CancelEventHandler(GridViewReadings_ShowingEditor);
            this.GridViewReadings.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewReadings_CellValueChanged);

            this.BtnGenerateReady.Enabled = false;
            this.BtnGenerateThis.Enabled = false;
            this.BtnOpenContract.Enabled = false;
            ShowActionPanel(false, null);
        }

        private CheckButton[] Chips()
        {
            return new CheckButton[] { ChipAll, ChipReady, ChipWaiting, ChipChanged, ChipUnpriced, ChipNoItem, ChipNotTaken, ChipInvoiced };
        }

        private void Boot()
        {
            if (_db == null) return;
            LoadCustomers();
            LoadBooks();
            FillCustomerFilter();
            LoadView();
            ScpBillingRows.ReconcileDeletedInvoices(_db);   // a deleted invoice gives its month back
            LoadData();
        }

        // ───────────────────────────── the customer filter ─────────────────────────────

        /// <summary>The picker: HQ's customers -- one row per debtor on HQ's contracts, read from HQ's
        /// book. Ticks come from this user's saved filter. Nothing ticked = every customer.</summary>
        private void FillCustomerFilter()
        {
            _suppress = true;
            try
            {
                DataTable t = new DataTable("HqCustomers");
                t.Columns.Add("Ticked", typeof(bool));
                t.Columns.Add("Code", typeof(string));
                t.Columns.Add("Name", typeof(string));
                if (_cs.Length > 0)
                {
                    HashSet<string> saved = ScpInterBillBoard.LoadCustomerFilter(_db, _userId);
                    List<string[]> hqCustomers = null;
                    try { hqCustomers = ScpInterBillBoard.Customers(_cs, this.ChkShowEnded.Checked); }
                    catch (Exception ex) { this.LblStatus.Text = ex.Message; }
                    if (hqCustomers != null)
                    {
                        int matched = 0;
                        foreach (string[] cu in hqCustomers)
                        {
                            bool on = saved != null && saved.Contains(cu[0]);
                            if (on) matched++;
                            t.Rows.Add(on, cu[0], cu[1].Length > 0 ? cu[1] : cu[0]);
                        }
                        // A saved filter that names none of HQ's customers (HQ changed, or it was saved
                        // against another book) would show an empty board for no visible reason: start over.
                        if (saved != null && matched == 0)
                        {
                            try { ScpInterBillBoard.SaveCustomerFilter(_db, _userId, null); } catch { }
                        }
                    }
                }
                t.AcceptChanges();
                _pickTable = t;
                this.GridCustomerPick.DataSource = new DataView(t, "", "Name, Code", DataViewRowState.CurrentRows);
                ApplyPickFilter();
                this.PceCustomers.EditValue = PickSummary();
            }
            finally { _suppress = false; }
        }

        /// <summary>What is ticked. Null = nothing ticked (or everything), i.e. no filter.</summary>
        private HashSet<string> CustomerFilter()
        {
            if (_pickTable == null) return null;
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _pickTable.Rows)
                if (IsTicked(r)) set.Add(Convert.ToString(r["Code"]));
            if (set.Count == 0 || set.Count == _pickTable.Rows.Count) return null;
            return set;
        }

        private static bool IsTicked(DataRow r)
        {
            return r["Ticked"] is bool && (bool)r["Ticked"];
        }

        private static string FilterKey(HashSet<string> set)
        {
            if (set == null) return "";
            List<string> l = new List<string>(set);
            l.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("|", l.ToArray());
        }

        /// <summary>The box text: null shows "All HQ customers".</summary>
        private object PickSummary()
        {
            HashSet<string> f = CustomerFilter();
            if (f == null) return null;
            List<string> names = new List<string>();
            foreach (DataRow r in _pickTable.Rows)
                if (IsTicked(r) && names.Count < 3) names.Add(Convert.ToString(r["Name"]));
            string s = f.Count + " of " + _pickTable.Rows.Count + " HQ customers  ·  " + string.Join(", ", names.ToArray());
            if (f.Count > names.Count) s += ", ...";
            return s;
        }

        /// <summary>The search box and "Ticked only" narrow the picker's own list. Every word must
        /// appear in the name or the code.</summary>
        private void ApplyPickFilter()
        {
            DataView v = this.GridCustomerPick.DataSource as DataView;
            if (v == null) return;
            List<string> parts = new List<string>();
            string q = Convert.ToString(this.TxtCustomerSearch.EditValue).Trim();
            foreach (string word in q.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string w = LikeEscape(word);
                parts.Add("(Name LIKE '%" + w + "%' OR Code LIKE '%" + w + "%')");
            }
            if (this.ChkTickedOnly.Checked) parts.Add("Ticked = true");
            v.RowFilter = string.Join(" AND ", parts.ToArray());
            UpdatePickCount();
        }

        private static string LikeEscape(string s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char ch in s)
            {
                if (ch == '[' || ch == ']' || ch == '%' || ch == '*') sb.Append('[').Append(ch).Append(']');
                else if (ch == '\'') sb.Append("''");
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        private void UpdatePickCount()
        {
            int ticked = 0;
            if (_pickTable != null)
                foreach (DataRow r in _pickTable.Rows) if (IsTicked(r)) ticked++;
            DataView v = this.GridCustomerPick.DataSource as DataView;
            int shown = v == null ? 0 : v.Count;
            this.LblPickCount.Text = (ticked == 0 ? "None ticked = all" : ticked + " ticked") + "  ·  " + shown + " shown";
        }

        private void SetTicked(bool on, bool shownOnly)
        {
            if (_pickTable == null) return;
            List<DataRow> rows = new List<DataRow>();
            DataView v = this.GridCustomerPick.DataSource as DataView;
            if (shownOnly && v != null) { foreach (DataRowView rv in v) rows.Add(rv.Row); }
            else { foreach (DataRow r in _pickTable.Rows) rows.Add(r); }
            foreach (DataRow r in rows) r["Ticked"] = on;
            UpdatePickCount();
        }

        private void ToggleRow(int rowHandle)
        {
            DataRow r = this.GridViewCustomerPick.GetDataRow(rowHandle);
            if (r == null) return;
            r["Ticked"] = !IsTicked(r);
            UpdatePickCount();
        }

        private void PceCustomers_QueryPopUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _pickBefore = FilterKey(CustomerFilter());
        }

        private void PceCustomers_Popup(object sender, EventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                this.TxtCustomerSearch.Focus();
                this.TxtCustomerSearch.SelectAll();
            }));
        }

        private void PceCustomers_QueryResultValue(object sender, DevExpress.XtraEditors.Controls.QueryResultValueEventArgs e)
        {
            e.Value = PickSummary();
        }

        /// <summary>Closing the picker saves the ticks for this user and reads the board again --
        /// only when the ticks changed.</summary>
        private void PceCustomers_Closed(object sender, DevExpress.XtraEditors.Controls.ClosedEventArgs e)
        {
            if (_suppress || _db == null) return;
            HashSet<string> f = CustomerFilter();
            _suppress = true;
            try { this.PceCustomers.EditValue = PickSummary(); }
            finally { _suppress = false; }
            if (FilterKey(f) == _pickBefore) return;
            _pickBefore = FilterKey(f);
            try { ScpInterBillBoard.SaveCustomerFilter(_db, _userId, f); } catch { }
            LoadData();
        }

        private void PickFilter_Changed(object sender, EventArgs e) { ApplyPickFilter(); }
        private void BtnTickShown_Click(object sender, EventArgs e) { SetTicked(true, true); }
        private void BtnUntickShown_Click(object sender, EventArgs e) { SetTicked(false, true); }
        private void BtnPickClear_Click(object sender, EventArgs e) { SetTicked(false, false); }
        private void BtnPickOk_Click(object sender, EventArgs e) { this.PceCustomers.ClosePopup(); }

        private void GridViewCustomerPick_RowCellClick(object sender, RowCellClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left) ToggleRow(e.RowHandle);
        }

        private void GridViewCustomerPick_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) { ToggleRow(this.GridViewCustomerPick.FocusedRowHandle); e.Handled = true; }
        }

        /// <summary>Down moves from the search box into the list; Space there ticks.</summary>
        private void TxtCustomerSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Down) return;
            this.GridCustomerPick.Focus();
            e.Handled = true;
        }

        private void LoadCustomers()
        {
            try
            {
                DataTable t = _db.GetDataTable("SELECT AccNo, ISNULL(CompanyName,'') AS CompanyName FROM dbo.Debtor ORDER BY AccNo", false);
                this.SlkCustomer.Properties.DataSource = t;
                this.SlkCustomer.Properties.ValueMember = "AccNo";
                this.SlkCustomer.Properties.DisplayMember = "CompanyName";
                this.SlkCustomerView.PopulateColumns(t);
                if (this.SlkCustomerView.Columns["AccNo"] != null) { this.SlkCustomerView.Columns["AccNo"].Caption = "Code"; this.SlkCustomerView.Columns["AccNo"].Width = 90; }
                if (this.SlkCustomerView.Columns["CompanyName"] != null) { this.SlkCustomerView.Columns["CompanyName"].Caption = "Company Name"; this.SlkCustomerView.Columns["CompanyName"].Width = 260; }
            }
            catch { }
        }

        /// <summary>HQ is set once in Inter-Billing Setup; nobody picks it here.</summary>
        private void LoadBooks()
        {
            SelectBook(ScpInterBillBooks.HqBook(_db));
        }

        private void SelectBook(ScpInterBillBook b)
        {
            _book = b;
            _cs = "";
            if (_book == null) { SetConn("Not set  ·  Setup", false); return; }
            if (!_book.IsUsable) { SetConn(HqName() + "  ·  Not tested  ·  Setup", false); return; }
            try { _cs = ScpInterBillBooks.ConnectionStringOf(_db, _book); SetConn(HqName() + "  ·  Connected", true); }
            catch (Exception ex) { SetConn(HqName() + "  ·  Not reachable", false); this.LblStatus.Text = ex.Message; }
            this.ColMHq.Caption = _book.Alias.Length > 0 ? _book.Alias : "HQ";
        }

        private string HqName()
        {
            if (_book == null) return "";
            return _book.RemoteCompany.Length > 0 ? _book.RemoteCompany : _book.DatabaseName;
        }

        private void SetConn(string text, bool ok)
        {
            this.LblConn.Text = text;
            this.LblConn.Appearance.ForeColor = ok ? Color.FromArgb(27, 94, 32) : Color.FromArgb(198, 40, 40);
        }

        /// <summary>The board bills up to this month. Each contract is at its own month, up to here.</summary>
        private static int UpTo() { return ScpBillingSequence.Period(DateTime.Today); }

        // ───────────────────────────── load ─────────────────────────────

        private void LoadData()
        {
            if (_db == null || _loading) return;
            _loading = true;
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            string keep = _current == null ? "" : KeyOf(_current);
            HashSet<string> expanded = ExpandedKeys(this.GridViewContracts);
            HashSet<string> expandedInvoices = ExpandedKeys(this.GridViewInvoices);
            try
            {
                _rows = new List<IbContractRow>();
                _invoices = new List<IbInvoiceRow>();
                if (_book != null && _book.IsUsable && _cs.Length > 0)
                {
                    try
                    {
                        _rows = ScpInterBillBoard.Load(_db, _book, _cs, ScpBillingSequence.YearOf(UpTo()), ScpBillingSequence.MonthOf(UpTo()), this.ChkShowEnded.Checked,
                                                       ScpInterBillBoard.LoadCustomerFilter(_db, _userId));
                        _invoices = ScpInterBillBoard.Invoices(_db, _rows, ScpBillingSequence.YearOf(UpTo()), ScpBillingSequence.MonthOf(UpTo()));
                        SetConn(HqName() + "  ·  Connected", true);
                    }
                    catch (Exception ex)
                    {
                        SetConn(HqName() + "  ·  Not reachable", false);
                        this.LblStatus.Text = ex.Message;
                    }
                }
                BindContracts();
                BindInvoices();
                UpdateChips();
                ApplyFilter();
                this.LblUpTo.Text = ScpBillingSequence.Name(UpTo());
                this.GridViewContracts.ViewCaption = "HQ contracts — up to " + ScpBillingSequence.Name(UpTo());
                HashSet<string> onlyCustomers = CustomerFilter();
                this.GridViewInvoices.ViewCaption = "Invoices — up to " + ScpBillingSequence.Name(UpTo()) +
                    (onlyCustomers == null ? "" : "  ·  " + onlyCustomers.Count + (onlyCustomers.Count == 1 ? " HQ customer only" : " HQ customers only"));
            }
            finally
            {
                this.Cursor = old;
                _loading = false;
            }
            RestoreExpanded(this.GridViewContracts, expanded);
            RestoreExpanded(this.GridViewInvoices, expandedInvoices);
            if (_invoiceView) FocusInvoiceContract(keep); else FocusKey(keep);
        }

        private static string KeyOf(IbContractRow r) { return r.HqContractNo + "|" + r.LocalKey; }

        private void BindContracts()
        {
            DataTable t = new DataTable();
            t.Columns.Add("Idx", typeof(int));
            t.Columns.Add("HqContractNo", typeof(string));
            t.Columns.Add("LocalNo", typeof(string));
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("PeriodText", typeof(string));
            t.Columns.Add("Progress", typeof(string));
            t.Columns.Add("Due", typeof(int));
            t.Columns.Add("InvoiceProgress", typeof(string));
            t.Columns.Add("Machines", typeof(int));
            t.Columns.Add("Readings", typeof(string));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("StatusText", typeof(string));
            t.Columns.Add("Status", typeof(int));
            for (int i = 0; i < _rows.Count; i++)
            {
                IbContractRow r = _rows[i];
                DataRow d = t.NewRow();
                d["Idx"] = i;
                d["HqContractNo"] = r.HqContractNo;
                d["LocalNo"] = r.Taken ? r.LocalNo : "—";
                d["Customer"] = r.Taken ? r.Customer : "—";
                d["PeriodText"] = r.Taken && r.Period > 0 ? ScpBillingSequence.Name(r.Period) : "—";
                d["Progress"] = r.Taken && r.Sequence != null ? r.Sequence.ProgressText(ScpInterBillBoard.OpenPeriod(r)) : "—";
                if (r.Taken && r.Due > 0) d["Due"] = r.Due; else d["Due"] = DBNull.Value;
                int invTotal = 0, invDone = 0;
                foreach (IbInvoiceRow v in _invoices)
                    if (v.Contract == r) { invTotal++; if (v.Status == ScpInterBillBoard.INVOICED) invDone++; }
                d["InvoiceProgress"] = invTotal > 0 ? invDone + "/" + invTotal : "—";
                d["Machines"] = r.Machines;
                d["Readings"] = r.Taken && r.MetersTotal > 0 ? r.MetersRead + " / " + r.MetersTotal : "—";
                if (r.Taken && r.Open) d["Amount"] = r.Amount; else d["Amount"] = DBNull.Value;
                d["StatusText"] = r.StatusText;
                d["Status"] = r.Status;
                t.Rows.Add(d);
            }
            t.TableName = "Contracts";
            _dtContracts = t;

            DataTable iv = NewInvoiceTable("ContractInvoices");
            iv.Columns.Add("ContractIdx", typeof(int));
            for (int i = 0; i < _invoices.Count; i++)
            {
                int ci = _rows.IndexOf(_invoices[i].Contract);
                if (ci < 0) continue;
                DataRow d = AddInvoiceRow(iv, i, _invoices[i]);
                d["ContractIdx"] = ci;
            }
            DataSet ds = new DataSet("Board");
            ds.Tables.Add(t);
            ds.Tables.Add(iv);
            ds.Relations.Add("Invoices", t.Columns["Idx"], iv.Columns["ContractIdx"], false);
            this.GridContracts.DataMember = "Contracts";
            this.GridContracts.DataSource = ds;
        }

        private void UpdateChips()
        {
            if (_invoiceView) { UpdateInvoiceChips(); return; }
            int all = _rows.Count, ready = 0, waiting = 0, changed = 0, unpriced = 0, noItem = 0, notTaken = 0, invoiced = 0;
            decimal readyAmt = 0m;
            foreach (IbContractRow r in _rows)
            {
                if (r.Status == ScpInterBillBoard.READY) { ready++; readyAmt += r.Amount; }
                else if (r.Status == ScpInterBillBoard.WAITING) waiting++;
                else if (r.Status == ScpInterBillBoard.CHANGED) changed++;
                else if (r.Status == ScpInterBillBoard.UNPRICED) unpriced++;
                else if (r.Status == ScpInterBillBoard.NO_ITEM) noItem++;
                else if (r.Status == ScpInterBillBoard.NOT_TAKEN || r.Status == ScpInterBillBoard.ENDED) notTaken++;
                else if (r.Status == ScpInterBillBoard.INVOICED) invoiced++;
            }
            this.ChipAll.Text = "All (" + all + ")";
            this.ChipReady.Text = "Ready (" + ready + ")";
            this.ChipWaiting.Text = "Waiting HQ reading (" + waiting + ")";
            this.ChipChanged.Text = "Changed at HQ (" + changed + ")";
            this.ChipUnpriced.Text = "Unpriced (" + unpriced + ")";
            this.ChipNoItem.Text = "No item code (" + noItem + ")";
            this.ChipNotTaken.Text = "Not taken (" + notTaken + ")";
            this.ChipInvoiced.Text = "Up to date (" + invoiced + ")";
            this.BtnGenerateReady.Text = "Generate all ready (" + ready + ")";
            this.BtnGenerateReady.Enabled = ready > 0;
            this.LblStatus.Text = "Up to " + ScpBillingSequence.Name(UpTo()) + "     ready: RM " + readyAmt.ToString("n2") +
                (_book != null ? "     HQ: " + _book.DatabaseName + (_book.RemoteCompany.Length > 0 ? " · " + _book.RemoteCompany : "") : "");
        }

        private void ApplyFilter()
        {
            string f = "";
            if (this.ChipReady.Checked) f = "[Status] = " + ScpInterBillBoard.READY;
            else if (this.ChipWaiting.Checked) f = "[Status] = " + ScpInterBillBoard.WAITING;
            else if (this.ChipChanged.Checked) f = "[Status] = " + ScpInterBillBoard.CHANGED;
            else if (this.ChipUnpriced.Checked) f = "[Status] = " + ScpInterBillBoard.UNPRICED;
            else if (this.ChipNoItem.Checked) f = "[Status] = " + ScpInterBillBoard.NO_ITEM;
            else if (this.ChipNotTaken.Checked) f = "[Status] = " + ScpInterBillBoard.NOT_TAKEN + " OR [Status] = " + ScpInterBillBoard.ENDED;
            else if (this.ChipInvoiced.Checked) f = "[Status] = " + ScpInterBillBoard.INVOICED;
            this.GridViewContracts.ActiveFilterString = f;
            // The invoice list is filtered where it is built: a contract row stays when one of its
            // invoices matches, and only the invoices that match are under it.
            if (!_loading)
            {
                HashSet<string> ex = ExpandedKeys(this.GridViewInvoices);
                BindInvoices();
                RestoreExpanded(this.GridViewInvoices, ex);
            }
        }

        private void FocusKey(string key)
        {
            int target = -1;
            for (int h = 0; h < this.GridViewContracts.RowCount; h++)
            {
                IbContractRow r = RowAt(h);
                if (r != null && KeyOf(r) == key) { target = h; break; }
            }
            if (target < 0 && this.GridViewContracts.RowCount > 0) target = 0;
            if (target >= 0) this.GridViewContracts.FocusedRowHandle = target;
            ShowDetail(target >= 0 ? RowAt(target) : null);
        }

        private IbContractRow RowAt(int handle)
        {
            if (handle < 0) return null;
            DataRow d = this.GridViewContracts.GetDataRow(handle);
            if (d == null) return null;
            int i = Convert.ToInt32(d["Idx"]);
            return i >= 0 && i < _rows.Count ? _rows[i] : null;
        }

        // ───────────────────────────── the invoice view ─────────────────────────────

        private string ViewConfigKey()
        {
            return "INTERBILL_VIEW_" + ((_userId ?? "").Trim().Length > 0 ? _userId.Trim().ToUpperInvariant() : "ADMIN");
        }

        /// <summary>The view this user left the board in; the invoice list the first time.</summary>
        private void LoadView()
        {
            string v = "";
            try { v = ServiceContractPhotocopier.Data.PumsConfig.Get(_db, ViewConfigKey(), ""); } catch { }
            _invoiceView = !string.Equals((v ?? "").Trim(), "CONTRACTS", StringComparison.OrdinalIgnoreCase);
            _suppress = true;
            try
            {
                this.BtnViewInvoices.Checked = _invoiceView;
                this.BtnViewContracts.Checked = !_invoiceView;
            }
            finally { _suppress = false; }
            ApplyViewVisibility();
        }

        private void View_Changed(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (_suppress || b == null || !b.Checked) return;
            _invoiceView = b == this.BtnViewInvoices;
            try { ServiceContractPhotocopier.Data.PumsConfig.Set(_db, ViewConfigKey(), _invoiceView ? "INVOICES" : "CONTRACTS"); } catch { }
            IbContractRow was = _current;
            ApplyViewVisibility();
            UpdateChips();
            ApplyFilter();
            if (_invoiceView) FocusInvoiceContract(was == null ? "" : KeyOf(was));
            else FocusKey(was == null ? "" : KeyOf(was));
        }

        private void ApplyViewVisibility()
        {
            this.GridInvoices.Visible = _invoiceView;
            this.GridContracts.Visible = !_invoiceView;
            // "Not taken" is a contract, never an invoice.
            this.ChipNotTaken.Visible = !_invoiceView;
            if (_invoiceView && this.ChipNotTaken.Checked) this.ChipAll.Checked = true;

            // The invoice list is a checklist: no Filter Options box. HQ is on the status bar, the month
            // on the list's caption, and a customer filter, when there is one, is named on the caption too.
            // The buttons and chips move up to where the box was.
            this.GrpFilter.Visible = !_invoiceView;
            int dx = _invoiceView ? this.GrpFilter.Left - _toolbarLefts[0] : 0;
            for (int i = 0; i < _toolbar.Length; i++) _toolbar[i].Left = _toolbarLefts[i] + dx;
            this.ChipInvoiced.Left = (_invoiceView ? _chipNotTakenLeft : _chipInvoicedLeft) + dx;
            int bottom = 0;
            foreach (Control ctl in _toolbar) if (ctl.Bottom > bottom) bottom = ctl.Bottom;
            this.PanelFilter.Height = _invoiceView ? bottom + 8 : _filterPanelHeight;
        }

        private static string InvoiceKeyOf(IbInvoiceRow v)
        {
            return v.Contract.LocalKey + "|" + v.Period + "|" + v.JobKey + (v.Skipped ? "|S" : "");
        }

        /// <summary>The invoice list: one row per contract that has invoices this month, its invoices under
        /// it. Only what the chip asks for is put in.</summary>
        private void BindInvoices()
        {
            DataTable m = new DataTable("InvContracts");
            m.Columns.Add("Idx", typeof(int));
            m.Columns.Add("Customer", typeof(string));
            m.Columns.Add("ContractNo", typeof(string));
            m.Columns.Add("PeriodText", typeof(string));
            m.Columns.Add("InvoiceProgress", typeof(string));
            m.Columns.Add("Readings", typeof(string));
            m.Columns.Add("Amount", typeof(decimal));
            m.Columns.Add("StatusText", typeof(string));
            m.Columns.Add("Status", typeof(int));
            DataTable lines = NewInvoiceTable("InvLines");
            lines.Columns.Add("ContractIdx", typeof(int));
            int wanted = ChipStatus();
            for (int ci = 0; ci < _rows.Count; ci++)
            {
                IbContractRow r = _rows[ci];
                List<IbInvoiceRow> mine = new List<IbInvoiceRow>();
                foreach (IbInvoiceRow v in _invoices) if (v.Contract == r) mine.Add(v);
                if (mine.Count == 0) continue;
                int done = 0, read = 0, total = 0;
                decimal amount = 0m;
                bool any = false;
                foreach (IbInvoiceRow v in mine)
                {
                    if (v.Status == ScpInterBillBoard.INVOICED) done++;
                    read += v.MetersRead;
                    total += v.MetersTotal;
                    if (!v.Skipped) amount += v.Amount;
                    if (wanted >= 0 && v.Status != wanted) continue;
                    DataRow line = AddInvoiceRow(lines, _invoices.IndexOf(v), v);
                    line["ContractIdx"] = ci;
                    any = true;
                }
                if (!any) continue;
                int status;
                string text;
                SummaryOf(mine, out status, out text);
                DataRow d = m.NewRow();
                d["Idx"] = ci;
                d["Customer"] = r.Customer;
                d["ContractNo"] = r.LocalNo;
                d["PeriodText"] = ScpBillingSequence.Name(mine[0].Period);
                d["InvoiceProgress"] = done + "/" + mine.Count;
                d["Readings"] = total > 0 ? read + " / " + total : "—";
                d["Amount"] = amount;
                d["StatusText"] = text;
                d["Status"] = status;
                m.Rows.Add(d);
            }
            DataSet ds = new DataSet("InvoiceList");
            ds.Tables.Add(m);
            ds.Tables.Add(lines);
            ds.Relations.Add("Invoices", m.Columns["Idx"], lines.Columns["ContractIdx"], false);
            this.GridInvoices.DataMember = "InvContracts";
            this.GridInvoices.DataSource = ds;
        }

        /// <summary>A contract's row says where its invoices stand as one: all made, some ready to go, or
        /// what the rest wait for.</summary>
        private static void SummaryOf(List<IbInvoiceRow> mine, out int status, out string text)
        {
            int done = 0, ready = 0;
            IbInvoiceRow firstOpen = null;
            List<string> docs = new List<string>();
            foreach (IbInvoiceRow v in mine)
            {
                if (v.Status == ScpInterBillBoard.INVOICED)
                {
                    done++;
                    if (v.DocNo.Length > 0 && !docs.Contains(v.DocNo)) docs.Add(v.DocNo);
                    continue;
                }
                if (v.Status == ScpInterBillBoard.READY) ready++;
                if (firstOpen == null) firstOpen = v;
            }
            if (done == mine.Count)
            {
                status = ScpInterBillBoard.INVOICED;
                text = mine.Count == 1 ? mine[0].StatusText : "Invoiced · " + string.Join(", ", docs.ToArray());
            }
            else if (ready > 0)
            {
                status = ScpInterBillBoard.READY;
                text = ready == mine.Count ? "Ready" : "Ready " + ready + " of " + mine.Count;
            }
            else
            {
                status = firstOpen.Status;
                text = firstOpen.StatusText;
            }
        }

        /// <summary>The status the chip picks; -1 = All.</summary>
        private int ChipStatus()
        {
            if (this.ChipReady.Checked) return ScpInterBillBoard.READY;
            if (this.ChipWaiting.Checked) return ScpInterBillBoard.WAITING;
            if (this.ChipChanged.Checked) return ScpInterBillBoard.CHANGED;
            if (this.ChipUnpriced.Checked) return ScpInterBillBoard.UNPRICED;
            if (this.ChipNoItem.Checked) return ScpInterBillBoard.NO_ITEM;
            if (this.ChipInvoiced.Checked) return ScpInterBillBoard.INVOICED;
            return -1;
        }

        /// <summary>The row shape of an invoice, in the flat list and under a contract.</summary>
        private static DataTable NewInvoiceTable(string name)
        {
            DataTable t = new DataTable(name);
            t.Columns.Add("Idx", typeof(int));
            t.Columns.Add("Customer", typeof(string));
            t.Columns.Add("ContractNo", typeof(string));
            t.Columns.Add("PeriodText", typeof(string));
            t.Columns.Add("Kind", typeof(string));
            t.Columns.Add("Detail", typeof(string));
            t.Columns.Add("Machines", typeof(int));
            t.Columns.Add("Readings", typeof(string));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("StatusText", typeof(string));
            t.Columns.Add("Status", typeof(int));
            return t;
        }

        private static DataRow AddInvoiceRow(DataTable t, int idx, IbInvoiceRow v)
        {
            DataRow d = t.NewRow();
            d["Idx"] = idx;
            d["Customer"] = v.Contract.Customer;
            d["ContractNo"] = v.Contract.LocalNo;
            d["PeriodText"] = ScpBillingSequence.Name(v.Period);
            d["Kind"] = v.Kind;
            d["Detail"] = v.Detail;
            if (v.Skipped) d["Machines"] = DBNull.Value; else d["Machines"] = v.Machines;
            d["Readings"] = v.MetersTotal > 0 ? v.MetersRead + " / " + v.MetersTotal : "—";
            if (v.Skipped) d["Amount"] = DBNull.Value; else d["Amount"] = v.Amount;
            d["StatusText"] = v.StatusText;
            d["Status"] = v.Status;
            t.Rows.Add(d);
            return d;
        }

        // ───────────────────────────── invoices under a contract ─────────────────────────────

        private IbInvoiceRow InvoiceOfDetail(GridView view, int handle)
        {
            if (view == null || handle < 0) return null;
            DataRow d = view.GetDataRow(handle);
            if (d == null) return null;
            int i = Convert.ToInt32(d["Idx"]);
            return i >= 0 && i < _invoices.Count ? _invoices[i] : null;
        }

        /// <summary>An invoice picked under a contract: the right side shows that invoice, and "Generate
        /// this invoice" makes that one only.</summary>
        private void ShowPickedInvoice(IbInvoiceRow v)
        {
            _pickedInvoice = v;
            if (v == null) return;
            ShowDetail(v.Contract, v.Skipped ? "" : v.JobKey);
            this.LblDetailSub.Text = v.Contract.Customer + "  ·  " + v.Kind;
            SetGenerateThis(v.Status == ScpInterBillBoard.READY ? 1 : 0);
        }

        /// <summary>No invoices under a contract (not taken, nothing this month): no expand button.</summary>
        private void GridViewContracts_MasterRowEmpty(object sender, MasterRowEmptyEventArgs e)
        {
            IbContractRow r = RowAt(e.RowHandle);
            bool any = false;
            if (r != null) foreach (IbInvoiceRow v in _invoices) if (v.Contract == r) { any = true; break; }
            e.IsEmpty = !any;
        }

        /// <summary>Focus moving between a contract row and an invoice under it, in whichever list shows.</summary>
        private void Grid_FocusedViewChanged(object sender, DevExpress.XtraGrid.ViewFocusEventArgs e)
        {
            DevExpress.XtraGrid.GridControl g = sender as DevExpress.XtraGrid.GridControl;
            if (_loading || g == null || (g == this.GridInvoices) != _invoiceView) return;
            if (e.View == g.MainView)
            {
                _pickedInvoice = null;
                if (_invoiceView) ShowDetail(ContractOfMaster(this.GridViewInvoices, this.GridViewInvoices.FocusedRowHandle));
                else ShowDetail(RowAt(this.GridViewContracts.FocusedRowHandle));
                return;
            }
            GridView view = e.View as GridView;
            if (view != null) ShowPickedInvoice(InvoiceOfDetail(view, view.FocusedRowHandle));
        }

        private void DetailInvoice_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading) return;
            GridView view = sender as GridView;
            if (view == null || view.GridControl.FocusedView != view) return;
            if ((view.GridControl == this.GridInvoices) != _invoiceView) return;
            ShowPickedInvoice(InvoiceOfDetail(view, e.FocusedRowHandle));
        }

        private void DetailInvoice_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            IbInvoiceRow v = InvoiceOfDetail(sender as GridView, e.RowHandle);
            if (v != null) StyleRow(e, v.Status);
        }

        /// <summary>Double-click an invoice: the invoice from its Status when it is made, the contract otherwise.</summary>
        private void DetailInvoice_DoubleClick(object sender, EventArgs e)
        {
            GridView view = sender as GridView;
            if (view == null) return;
            Point pt = view.GridControl.PointToClient(Control.MousePosition);
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = view.CalcHitInfo(pt);
            if (hit == null || !hit.InRowCell) return;
            IbInvoiceRow v = InvoiceOfDetail(view, hit.RowHandle);
            if (v == null) return;
            if (hit.Column != null && hit.Column.FieldName == "StatusText" && v.DocNo.Length > 0 && v.DocNo.IndexOf(' ') < 0) { OpenInvoice(v.DocNo); return; }
            OpenContract(v.Contract);
        }

        private void UpdateInvoiceChips()
        {
            int all = _invoices.Count, ready = 0, waiting = 0, changed = 0, unpriced = 0, noItem = 0, invoiced = 0;
            decimal readyAmt = 0m;
            foreach (IbInvoiceRow v in _invoices)
            {
                if (v.Status == ScpInterBillBoard.READY) { ready++; readyAmt += v.Amount; }
                else if (v.Status == ScpInterBillBoard.WAITING) waiting++;
                else if (v.Status == ScpInterBillBoard.CHANGED) changed++;
                else if (v.Status == ScpInterBillBoard.UNPRICED) unpriced++;
                else if (v.Status == ScpInterBillBoard.NO_ITEM) noItem++;
                else if (v.Status == ScpInterBillBoard.INVOICED) invoiced++;
            }
            this.ChipAll.Text = "All (" + all + ")";
            this.ChipReady.Text = "Ready (" + ready + ")";
            this.ChipWaiting.Text = "Waiting HQ reading (" + waiting + ")";
            this.ChipChanged.Text = "Changed at HQ (" + changed + ")";
            this.ChipUnpriced.Text = "Unpriced (" + unpriced + ")";
            this.ChipNoItem.Text = "No item code (" + noItem + ")";
            this.ChipInvoiced.Text = "Invoiced (" + invoiced + ")";
            this.BtnGenerateReady.Text = "Generate all ready (" + ready + ")";
            this.BtnGenerateReady.Enabled = ready > 0;
            this.LblStatus.Text = "Up to " + ScpBillingSequence.Name(UpTo()) + "     " + invoiced + " of " + all + " invoiced" +
                "     ready: RM " + readyAmt.ToString("n2") +
                (_book != null ? "     HQ: " + _book.DatabaseName + (_book.RemoteCompany.Length > 0 ? " · " + _book.RemoteCompany : "") : "");
        }

        private IbContractRow ContractOfMaster(GridView view, int handle)
        {
            if (view == null || handle < 0) return null;
            DataRow d = view.GetDataRow(handle);
            if (d == null) return null;
            int i = Convert.ToInt32(d["Idx"]);
            return i >= 0 && i < _rows.Count ? _rows[i] : null;
        }

        private HashSet<string> ExpandedKeys(GridView view)
        {
            HashSet<string> keys = new HashSet<string>();
            for (int h = 0; h < view.RowCount; h++)
            {
                IbContractRow r = ContractOfMaster(view, h);
                if (r != null && view.GetMasterRowExpanded(h)) keys.Add(KeyOf(r));
            }
            return keys;
        }

        private void RestoreExpanded(GridView view, HashSet<string> keys)
        {
            if (keys == null || keys.Count == 0) return;
            for (int h = 0; h < view.RowCount; h++)
            {
                IbContractRow r = ContractOfMaster(view, h);
                if (r != null && keys.Contains(KeyOf(r))) view.SetMasterRowExpanded(h, true);
            }
        }

        private List<IbInvoiceRow> ReadyInvoicesOf(IbContractRow r)
        {
            List<IbInvoiceRow> list = new List<IbInvoiceRow>();
            if (r != null) foreach (IbInvoiceRow v in _invoices) if (v.Contract == r && v.Status == ScpInterBillBoard.READY) list.Add(v);
            return list;
        }

        /// <summary>The big button makes what is picked: one invoice, or a contract's ready ones.</summary>
        private void SetGenerateThis(int ready)
        {
            this.BtnGenerateThis.Enabled = ready > 0;
            this.BtnGenerateThis.Text = ready > 1 ? "Generate " + ready + " invoices" : "Generate this invoice";
        }

        private void FocusInvoiceContract(string key)
        {
            int target = -1;
            for (int h = 0; h < this.GridViewInvoices.RowCount; h++)
            {
                IbContractRow r = ContractOfMaster(this.GridViewInvoices, h);
                if (r != null && KeyOf(r) == key) { target = h; break; }
            }
            if (target < 0 && this.GridViewInvoices.RowCount > 0) target = 0;
            if (target >= 0) this.GridViewInvoices.FocusedRowHandle = target;
            _pickedInvoice = null;
            ShowDetail(target >= 0 ? ContractOfMaster(this.GridViewInvoices, target) : null);
        }

        private void GridViewInvoices_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading || !_invoiceView) return;
            _pickedInvoice = null;
            ShowDetail(ContractOfMaster(this.GridViewInvoices, e.FocusedRowHandle));
        }

        /// <summary>Double-click a contract row: its invoice from Status when there is exactly one made, the contract otherwise.</summary>
        private void GridViewInvoices_DoubleClick(object sender, EventArgs e)
        {
            Point pt = this.GridInvoices.PointToClient(Control.MousePosition);
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = this.GridViewInvoices.CalcHitInfo(pt);
            if (hit == null || !hit.InRowCell) return;
            IbContractRow r = ContractOfMaster(this.GridViewInvoices, hit.RowHandle);
            if (r == null) return;
            if (hit.Column == this.ColIvStatus)
            {
                List<string> docs = new List<string>();
                foreach (IbInvoiceRow v in _invoices)
                    if (v.Contract == r && v.DocNo.Length > 0 && v.DocNo.IndexOf(' ') < 0 && !docs.Contains(v.DocNo)) docs.Add(v.DocNo);
                if (docs.Count == 1) { OpenInvoice(docs[0]); return; }
            }
            OpenContract(r);
        }

        // ───────────────────────────── the right side ─────────────────────────────

        private void ShowDetail(IbContractRow r) { ShowDetail(r, null); }

        /// <summary>The right side for a contract; with <paramref name="jobKey"/>, only the readings that
        /// go on that one invoice.</summary>
        private void ShowDetail(IbContractRow r, string jobKey)
        {
            _current = r;
            if (r == null)
            {
                this.LblDetailTitle.Text = _book == null ? "No HQ book" : "";
                this.LblDetailSub.Text = "";
                this.GridMachines.DataSource = null;
                this.GridReadings.DataSource = null;
                SetGenerateThis(0);
                this.BtnOpenContract.Enabled = false;
                ShowActionPanel(false, null);
                UpdateSequenceStrip(null);
                return;
            }
            this.LblDetailTitle.Text = r.Taken ? r.LocalNo : r.HqContractNo;
            this.LblDetailSub.Text = r.Taken ? (r.Customer + "  ·  HQ " + r.HqContractNo) : (r.StatusText + (r.Remote != null && r.Remote.Description.Length > 0 ? "  ·  " + r.Remote.Description : ""));
            if (r.Error.Length > 0) this.LblDetailSub.Text = r.Error;
            SetGenerateThis(ReadyInvoicesOf(r).Count);
            this.BtnOpenContract.Enabled = r.Taken;
            UpdateSequenceStrip(r);
            this.ColMMine.Caption = r.Taken ? r.LocalNo : "Opening reading";
            this.ColMStatus.Caption = r.Taken ? "Status" : "";
            if (!r.Taken) { this.ColMMine.FieldName = "StatusText"; this.ColMStatus.Visible = false; }
            else { this.ColMMine.FieldName = "Mine"; if (!this.ColMStatus.Visible) { this.ColMStatus.Visible = true; this.ColMStatus.VisibleIndex = 4; } }

            DataTable mt = new DataTable();
            mt.Columns.Add("Idx", typeof(int));
            mt.Columns.Add("Serial", typeof(string));
            mt.Columns.Add("Model", typeof(string));
            mt.Columns.Add("AtHq", typeof(string));
            mt.Columns.Add("Mine", typeof(string));
            mt.Columns.Add("StatusText", typeof(string));
            mt.Columns.Add("HasChange", typeof(bool));
            for (int i = 0; i < r.MachineRows.Count; i++)
            {
                IbMachineRow m = r.MachineRows[i];
                DataRow d = mt.NewRow();
                d["Idx"] = i; d["Serial"] = m.Serial; d["Model"] = m.Model; d["AtHq"] = m.AtHq;
                d["Mine"] = m.Mine; d["StatusText"] = m.StatusText; d["HasChange"] = m.Change != null;
                mt.Rows.Add(d);
            }
            this.GridMachines.DataSource = mt;
            this.GridViewMachines.ViewCaption = "Machines";

            DataTable rt = new DataTable();
            rt.Columns.Add("Idx", typeof(int));
            rt.Columns.Add("ServiceItemNo", typeof(string));
            rt.Columns.Add("Serial", typeof(string));
            rt.Columns.Add("Meter", typeof(string));
            rt.Columns.Add("Last", typeof(decimal));
            rt.Columns.Add("Current", typeof(decimal));
            rt.Columns.Add("Copies", typeof(decimal));
            rt.Columns.Add("Amount", typeof(decimal));
            rt.Columns.Add("AtHq", typeof(string));
            rt.Columns.Add("Saved", typeof(string));
            for (int i = 0; i < r.Readings.Count; i++)
            {
                IbReadingRow x = r.Readings[i];
                if (jobKey != null && x.Row != null && x.Row.Table.Columns.Contains(ScpInvoiceRun.COL_JOBKEY) &&
                    Convert.ToString(x.Row[ScpInvoiceRun.COL_JOBKEY]) != jobKey) continue;
                DataRow d = rt.NewRow();
                d["Idx"] = i; d["ServiceItemNo"] = x.ServiceItemNo; d["Serial"] = x.Serial; d["Meter"] = x.Meter;
                if (!x.IsCharge) d["Last"] = x.Last;
                if (x.HasReading) { d["Current"] = x.Current; d["Copies"] = x.Copies; d["Amount"] = x.Amount; }
                else if (x.IsCharge && x.AmountShown) d["Amount"] = x.Amount;   // the rent, a minimum, a waive: no counter, just the money
                d["AtHq"] = x.AtHq;
                d["Saved"] = x.Saved.Length > 0 ? x.Saved : "—";
                rt.Rows.Add(d);
            }
            this.GridReadings.DataSource = rt;
            this.GridViewReadings.ViewCaption = r.Period > 0 ? "Readings — " + ScpBillingSequence.Name(r.Period) : "Readings";

            bool canTake = !r.Taken && r.Status == ScpInterBillBoard.NOT_TAKEN;
            ShowActionPanel(canTake, r);
            if (!canTake) UpdateChangeButtons();
        }

        /// <summary>The line under the title: how many months are billed, the month being billed, how
        /// many are due -- and the three things that change the run of months.</summary>
        private void UpdateSequenceStrip(IbContractRow r)
        {
            ScpContractSequence s = r == null ? null : r.Sequence;
            bool taken = r != null && r.Taken && s != null;
            int open = taken ? ScpInterBillBoard.OpenPeriod(r) : 0;
            this.BtnBillFrom.Enabled = taken;
            bool canSkip = taken && r.Open && r.Period > 0 && r.Period <= UpTo();
            this.BtnSkipMonth.Enabled = canSkip;
            this.BtnSkipMonth.Text = canSkip ? "Skip " + ScpBillingSequence.Name(r.Period) : "Skip month";
            int undo = taken ? s.SkipBefore(open) : 0;
            this.BtnUndoSkip.Enabled = undo > 0;
            this.BtnUndoSkip.Tag = undo;
            this.BtnUndoSkip.Text = undo > 0 ? "Undo skip " + ScpBillingSequence.Name(undo) : "Undo skip";
            if (!taken) { this.LblSequence.Text = ""; return; }
            string text = "Billed " + s.ProgressText(open);
            if (r.Open) text += "  ·  " + ScpBillingSequence.Name(r.Period) + (r.Due > 1 ? "  ·  " + r.Due + " due" : "");
            else if (s.Complete) text += "  ·  complete";
            else text += "  ·  next " + ScpBillingSequence.Name(s.Next);
            this.LblSequence.Text = text;
        }

        /// <summary>The strip under Machines holds either Take (a contract not taken yet) or the two
        /// answers to a change (a contract already taken).</summary>
        private void ShowActionPanel(bool take, IbContractRow r)
        {
            this.LblCustomer.Visible = take;
            this.SlkCustomer.Visible = take;
            this.BtnTake.Visible = take;
            this.BtnApply.Visible = !take;
            this.BtnIgnore.Visible = !take;
            if (!take) { this.BtnApply.Enabled = false; this.BtnIgnore.Enabled = false; this.BtnApply.Text = "—"; }
        }

        private IbMachineRow FocusedMachine()
        {
            if (_current == null) return null;
            DataRow d = this.GridViewMachines.GetFocusedDataRow();
            if (d == null) return null;
            int i = Convert.ToInt32(d["Idx"]);
            return i >= 0 && i < _current.MachineRows.Count ? _current.MachineRows[i] : null;
        }

        private void UpdateChangeButtons()
        {
            IbMachineRow m = FocusedMachine();
            IbChange ch = m == null ? null : m.Change;
            if (ch == null && _current != null)
            {
                // Nothing focused with a change: offer the first one, so the answer is one click away.
                foreach (IbMachineRow mr in _current.MachineRows) if (mr.Change != null) { ch = mr.Change; break; }
            }
            this.BtnApply.Enabled = ch != null;
            this.BtnApply.Text = ch == null ? "—" : ch.ApplyCaption;
            this.BtnIgnore.Enabled = ch != null && ch.IgnoreCaption.Length > 0;
            this.BtnIgnore.Text = ch != null && ch.IgnoreCaption.Length > 0 ? ch.IgnoreCaption : "Ignore";
            this.BtnApply.Tag = ch;
        }

        // ───────────────────────────── events ─────────────────────────────

        private void Period_Changed(object sender, EventArgs e)
        {
            if (_suppress) return;
            if (sender == this.ChkShowEnded) FillCustomerFilter();
            LoadData();
        }
        private void BtnRefresh_Click(object sender, EventArgs e) { ScpBillingRows.ReconcileDeletedInvoices(_db); LoadData(); }

        private void Chip_Changed(object sender, EventArgs e)
        {
            CheckButton b = sender as CheckButton;
            if (b == null || !b.Checked) return;
            ApplyFilter();
            if (_invoiceView) FocusInvoiceContract(_current == null ? "" : KeyOf(_current));
            else FocusKey(_current == null ? "" : KeyOf(_current));
        }

        private void GridViewContracts_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_loading || _invoiceView) return;   // the hidden list moving must not replace the invoice on the right
            _pickedInvoice = null;
            ShowDetail(RowAt(this.GridViewContracts.FocusedRowHandle));
        }

        private void GridViewMachines_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_current != null && _current.Taken) UpdateChangeButtons();
        }

        private void GridViewContracts_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            IbContractRow r = RowAt(e.RowHandle);
            if (r == null) return;
            StyleRow(e, r.Status);
        }

        private void GridViewInvoices_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            DataRow d = this.GridViewInvoices.GetDataRow(e.RowHandle);
            if (d == null || d["Status"] == DBNull.Value) return;
            StyleRow(e, Convert.ToInt32(d["Status"]));
        }

        private static void StyleRow(RowStyleEventArgs e, int status)
        {
            Color back = Color.Empty;
            if (status == ScpInterBillBoard.READY) back = READY_BACK;
            else if (status == ScpInterBillBoard.WAITING || status == ScpInterBillBoard.CHANGED) back = WAIT_BACK;
            else if (status == ScpInterBillBoard.UNPRICED || status == ScpInterBillBoard.NO_ITEM || status == ScpInterBillBoard.ERROR) back = BAD_BACK;
            if (back != Color.Empty)
            {
                e.Appearance.BackColor = back; e.Appearance.BackColor2 = back;
                e.Appearance.Options.UseBackColor = true;
            }
            else
            {
                e.Appearance.ForeColor = DONE_FORE;
                e.Appearance.Options.UseForeColor = true;
            }
        }

        /// <summary>Two or more months to bill is a contract falling behind: the number says so.</summary>
        private void GridViewContracts_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            if (e.Column != this.ColDue || e.RowHandle < 0) return;
            IbContractRow r = RowAt(e.RowHandle);
            if (r == null || r.Due < 2) return;
            e.Appearance.ForeColor = Color.FromArgb(198, 40, 40);
            e.Appearance.FontStyleDelta = FontStyle.Bold;
            e.Appearance.Options.UseForeColor = true;
        }

        private void GridViewMachines_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            DataRow d = this.GridViewMachines.GetDataRow(e.RowHandle);
            if (d == null || d["HasChange"] == DBNull.Value || !Convert.ToBoolean(d["HasChange"])) return;
            e.Appearance.BackColor = WAIT_BACK; e.Appearance.BackColor2 = WAIT_BACK;
            e.Appearance.Options.UseBackColor = true;
        }

        private IbReadingRow ReadingAt(int handle)
        {
            if (_current == null || handle < 0) return null;
            DataRow d = this.GridViewReadings.GetDataRow(handle);
            if (d == null) return null;
            int i = Convert.ToInt32(d["Idx"]);
            return i >= 0 && i < _current.Readings.Count ? _current.Readings[i] : null;
        }

        private void GridViewReadings_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            IbReadingRow x = ReadingAt(e.RowHandle);
            if (x == null) return;
            if (x.IsCharge)
            {
                // A charge, not a counter: nothing to key, so nothing is marked missing.
                if (e.Column == this.ColRAtHq)
                {
                    e.Appearance.ForeColor = Color.FromArgb(23, 69, 122);
                    e.Appearance.Options.UseForeColor = true;
                }
                return;
            }
            if (e.Column == this.ColRCurrent)
            {
                if (!x.HasReading)
                {
                    e.Appearance.BackColor = Color.FromArgb(255, 213, 79);
                    e.Appearance.Options.UseBackColor = true;
                }
                else if (x.FromHq)
                {
                    e.Appearance.BackColor = HQ_BACK;
                    e.Appearance.ForeColor = Color.FromArgb(23, 69, 122);
                    e.Appearance.Options.UseBackColor = true;
                    e.Appearance.Options.UseForeColor = true;
                }
            }
            else if (e.Column == this.ColRAtHq && !x.HasReading)
            {
                e.Appearance.ForeColor = Color.FromArgb(122, 83, 0);
                e.Appearance.FontStyleDelta = FontStyle.Bold;
                e.Appearance.Options.UseForeColor = true;
            }
            else if (e.Column == this.ColRSaved && x.Invoiced)
            {
                e.Appearance.ForeColor = Color.FromArgb(30, 107, 42);
                e.Appearance.FontStyleDelta = FontStyle.Bold;
                e.Appearance.Options.UseForeColor = true;
            }
        }

        /// <summary>Only a counter of this book's own is keyed here; HQ's are read from HQ.</summary>
        private void GridViewReadings_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IbReadingRow x = ReadingAt(this.GridViewReadings.FocusedRowHandle);
            if (this.GridViewReadings.FocusedColumn != this.ColRCurrent || x == null || x.IsCharge || x.FromHq || x.Invoiced) e.Cancel = true;
        }

        private void GridViewReadings_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != this.ColRCurrent) return;
            IbReadingRow x = ReadingAt(e.RowHandle);
            if (x == null || x.IsCharge || x.FromHq || x.Invoiced || _current == null || _current.Period <= 0) return;
            decimal v = 0m;
            if (e.Value != null && e.Value != DBNull.Value) decimal.TryParse(Convert.ToString(e.Value), out v);
            try { ScpInvoiceRun.SaveReading(_db, x.LocalMeterKey, ScpBillingSequence.YearOf(_current.Period), ScpBillingSequence.MonthOf(_current.Period), v); }
            catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            LoadData();
        }

        private void GridViewContracts_DoubleClick(object sender, EventArgs e)
        {
            Point pt = this.GridContracts.PointToClient(Control.MousePosition);
            DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo hit = this.GridViewContracts.CalcHitInfo(pt);
            if (hit == null || !hit.InRowCell) return;
            IbContractRow r = RowAt(hit.RowHandle);
            if (r == null) return;
            if (hit.Column == this.ColStatus && r.InvoicedDocs.Count > 0) { OpenInvoice(r.InvoicedDocs[0]); return; }
            if (r.Taken) OpenContract(r);
        }

        private void BtnOpenContract_Click(object sender, EventArgs e) { if (_current != null && _current.Taken) OpenContract(_current); }

        private void OpenContract(IbContractRow r)
        {
            try
            {
                zSCP2_Contract_Form f = new zSCP2_Contract_Form(_db, r.LocalKey);
                f.FormClosed += delegate { LoadData(); };
                f.Show();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not open " + r.LocalNo + ":\r\n" + ex.Message, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenInvoice(string docNo)
        {
            try
            {
                object k = _db.ExecuteScalar("SELECT DocKey FROM dbo.IV WHERE DocNo = N'" + docNo.Replace("'", "''") + "'");
                if (k == null || k == DBNull.Value) { XtraMessageBox.Show("Invoice " + docNo + " not found.", "Open Invoice"); return; }
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                    AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(AutoCount.Authentication.UserSession.CurrentUserSession, _db);
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.Edit(Convert.ToInt64(k));
                if (doc == null) return;
                using (AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry f = new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc))
                    f.ShowDialog(this);
                LoadData();
            }
            catch (Exception ex) { XtraMessageBox.Show("Could not open the invoice:\r\n" + ex.Message, "Open Invoice"); }
        }

        private void BtnSetup_Click(object sender, EventArgs e)
        {
            try
            {
                using (ServiceContractPhotocopier.GeneralSetup.MasterForms.InterBillSetup_Form f =
                           new ServiceContractPhotocopier.GeneralSetup.MasterForms.InterBillSetup_Form(_db))
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
            }
            catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Inter-Billing Setup"); }
            LoadBooks();
            FillCustomerFilter();
            LoadData();
        }

        // ───────────────────────────── answers ─────────────────────────────

        private void BtnApply_Click(object sender, EventArgs e)
        {
            IbChange ch = this.BtnApply.Tag as IbChange;
            if (ch == null || _current == null) return;
            if (ch.Kind == IbChange.MACHINE_GONE &&
                XtraMessageBox.Show(ch.ApplyCaption + " on " + _current.LocalNo + "?", "Inter-Billing",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            string err = ScpInterBillBoard.Apply(_db, _book, _cs, _current, ch);
            if (err.Length > 0) XtraMessageBox.Show(err, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadData();
        }

        private void BtnIgnore_Click(object sender, EventArgs e)
        {
            IbChange ch = this.BtnApply.Tag as IbChange;
            if (ch == null || _current == null) return;
            string err = ScpInterBillBoard.Ignore(_db, _book, _current, ch);
            if (err.Length > 0) XtraMessageBox.Show(err, "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadData();
        }

        private void BtnTake_Click(object sender, EventArgs e)
        {
            if (_current == null || _current.Taken || _current.Remote == null) return;
            string debtor = this.SlkCustomer.EditValue == null ? "" : Convert.ToString(this.SlkCustomer.EditValue).Trim();
            if (debtor.Length == 0) { this.SlkCustomer.Focus(); this.SlkCustomer.ShowPopup(); return; }
            List<string> clashes = ScpInterBillTake.NumberClashes(_db, _current.Remote.ContractNo, _current.HqMachines);
            if (clashes.Count > 0)
            {
                XtraMessageBox.Show("Already used in this book:\r\n\r\n" + string.Join("\r\n", clashes.ToArray()),
                    "Take contract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            ScpTakeResult res;
            try { res = ScpInterBillBoard.Take(_db, _book, _cs, _current, debtor, _userId); }
            finally { this.Cursor = old; }
            if (!res.Ok) { XtraMessageBox.Show(res.Error, "Take contract", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (res.Notes.Count > 0)
                XtraMessageBox.Show("Taken as " + res.ContractNo + ", except:\r\n\r\n" + string.Join("\r\n", res.Notes.ToArray()),
                    "Take contract", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _current = null;
            this.SlkCustomer.EditValue = null;
            LoadData();
            FocusKey(res.ContractNo.Length > 0 ? FindKeyByLocal(res.ContractKey) : "");
        }

        private string FindKeyByLocal(long localKey)
        {
            foreach (IbContractRow r in _rows) if (r.LocalKey == localKey) return KeyOf(r);
            return "";
        }

        // ───────────────────────────── the months ─────────────────────────────

        /// <summary>A month that is not billed on purpose is recorded with a reason; the contract moves on.</summary>
        private void BtnSkipMonth_Click(object sender, EventArgs e)
        {
            IbContractRow r = _current;
            if (r == null || !r.Taken || !r.Open || r.Period <= 0) return;
            string name = ScpBillingSequence.Name(r.Period);
            string answer = XtraInputBox.Show(this, "Reason:", "Skip " + name + "  ·  " + r.LocalNo, "");
            string reason = (answer ?? "").Trim();
            if (reason.Length == 0) return;
            try { ScpBillingSequence.Skip(_db, r.LocalKey, r.Period, reason, _userId); }
            catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Skip " + name, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            LoadData();
        }

        private void BtnUndoSkip_Click(object sender, EventArgs e)
        {
            IbContractRow r = _current;
            int period = this.BtnUndoSkip.Tag is int ? (int)this.BtnUndoSkip.Tag : 0;
            if (r == null || !r.Taken || period <= 0) return;
            try { ScpBillingSequence.UndoSkip(_db, r.LocalKey, period, _userId); }
            catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Undo skip", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            LoadData();
        }

        /// <summary>The first month this book bills the contract.
        /// <para>Earlier: the months before come in to be billed (asked first when later months are
        /// already invoiced). Later: the months in between that are not billed would be dropped, so they
        /// are skipped with a reason like any other skip -- on record, and each can be undone.</para></summary>
        private void BtnBillFrom_Click(object sender, EventArgs e)
        {
            IbContractRow r = _current;
            if (r == null || !r.Taken || r.Sequence == null) return;
            ScpContractSequence s = r.Sequence;
            int last = UpTo();
            int first = s.StartPeriod > 0 && s.StartPeriod <= last ? s.StartPeriod : ScpBillingSequence.AddMonths(last, -24);
            List<int> periods = new List<int>();
            ComboBoxEdit cmb = new ComboBoxEdit();
            cmb.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            for (int p = last; p >= first && periods.Count < 240; p = ScpBillingSequence.AddMonths(p, -1))
            {
                periods.Add(p);
                cmb.Properties.Items.Add(ScpBillingSequence.Name(p));
            }
            XtraInputBoxArgs args = new XtraInputBoxArgs();
            args.Caption = "Bill from  ·  " + r.LocalNo;
            args.Prompt = "First month billed here:";
            args.Editor = cmb;
            args.DefaultResponse = ScpBillingSequence.Name(s.First);
            object res = XtraInputBox.Show(args);
            if (res == null) return;
            int idx = cmb.Properties.Items.IndexOf(Convert.ToString(res));
            if (idx < 0 || idx >= periods.Count) return;
            int target = periods[idx];
            try
            {
                if (target < s.First)
                {
                    int billedAfter = 0;
                    foreach (int p in s.Billed) if (p >= s.First && (billedAfter == 0 || p < billedAfter)) billedAfter = p;
                    if (billedAfter > 0 && XtraMessageBox.Show(
                            "Bill " + ScpBillingSequence.Name(target) + " – " + ScpBillingSequence.Name(ScpBillingSequence.AddMonths(s.First, -1)) +
                            " too?\r\n" + ScpBillingSequence.Name(billedAfter) + " is already invoiced.",
                            "Bill from  ·  " + r.LocalNo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;
                    ScpBillingSequence.SetBillFrom(_db, r.LocalKey, target);
                }
                else if (target > s.First)
                {
                    List<int> drop = new List<int>();
                    for (int p = s.First; p < target; p = ScpBillingSequence.AddMonths(p, 1)) if (!s.IsHandled(p)) drop.Add(p);
                    if (drop.Count == 0) return;
                    string span = ScpBillingSequence.Name(drop[0]) + (drop.Count > 1 ? " – " + ScpBillingSequence.Name(drop[drop.Count - 1]) : "");
                    string answer = XtraInputBox.Show(this, "Reason:", "Skip " + span + "  ·  " + r.LocalNo, "");
                    string reason = (answer ?? "").Trim();
                    if (reason.Length == 0) return;
                    foreach (int p in drop)
                        ScpBillingSequence.Skip(_db, r.LocalKey, p, "Bill from " + ScpBillingSequence.Name(target) + ": " + reason, _userId);
                }
                else return;
            }
            catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Bill from", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            LoadData();
        }

        // ───────────────────────────── generate ─────────────────────────────

        private void BtnGenerateReady_Click(object sender, EventArgs e)
        {
            // Read both books again first: another PC may have billed, or HQ may have changed a
            // reading, since the board was loaded.
            ScpBillingRows.ReconcileDeletedInvoices(_db);
            LoadData();
            if (_invoiceView)
            {
                List<IbInvoiceRow> readyInvoices = new List<IbInvoiceRow>();
                foreach (IbInvoiceRow v in _invoices) if (v.Status == ScpInterBillBoard.READY) readyInvoices.Add(v);
                if (readyInvoices.Count == 0) { XtraMessageBox.Show("Nothing is ready.", "Inter-Billing"); return; }
                GenerateInvoices(readyInvoices);
                return;
            }
            List<IbContractRow> ready = new List<IbContractRow>();
            foreach (IbContractRow r in _rows) if (r.Status == ScpInterBillBoard.READY) ready.Add(r);
            if (ready.Count == 0) { XtraMessageBox.Show("Nothing is ready.", "Inter-Billing"); return; }
            Generate(ready);
        }

        private void BtnGenerateThis_Click(object sender, EventArgs e)
        {
            DevExpress.XtraGrid.GridControl g = _invoiceView ? this.GridInvoices : this.GridContracts;
            if (_pickedInvoice != null && g.FocusedView != g.MainView) { GenerateThisInvoice(_pickedInvoice); return; }
            if (_current == null) return;
            string key = KeyOf(_current);
            ScpBillingRows.ReconcileDeletedInvoices(_db);
            LoadData();
            IbContractRow fresh = null;
            foreach (IbContractRow r in _rows) if (KeyOf(r) == key) fresh = r;
            List<IbInvoiceRow> ready = ReadyInvoicesOf(fresh);
            if (ready.Count == 0)
            {
                XtraMessageBox.Show((fresh == null ? "That contract" : fresh.LocalNo) + " has nothing ready" +
                    (fresh == null ? "." : ": " + fresh.StatusText), "Inter-Billing");
                return;
            }
            GenerateInvoices(ready);
        }

        private void GenerateThisInvoice(IbInvoiceRow picked)
        {
            if (picked == null) return;
            string key = InvoiceKeyOf(picked);
            ScpBillingRows.ReconcileDeletedInvoices(_db);
            LoadData();
            IbInvoiceRow fresh = null;
            foreach (IbInvoiceRow v in _invoices) if (InvoiceKeyOf(v) == key) fresh = v;
            if (fresh == null || fresh.Status != ScpInterBillBoard.READY)
            {
                XtraMessageBox.Show("That invoice is no longer ready" + (fresh == null ? "." : ": " + fresh.StatusText), "Inter-Billing");
                return;
            }
            List<IbInvoiceRow> one = new List<IbInvoiceRow>();
            one.Add(fresh);
            GenerateInvoices(one);
        }

        /// <summary>The invoices picked, one run per month, oldest first. Only those invoices are built:
        /// a contract's other invoices stay as they are.</summary>
        private void GenerateInvoices(List<IbInvoiceRow> chosen)
        {
            if (_db == null || chosen == null || chosen.Count == 0) return;
            SortedDictionary<int, List<IbInvoiceRow>> byMonth = new SortedDictionary<int, List<IbInvoiceRow>>();
            foreach (IbInvoiceRow v in chosen)
            {
                if (v.Period <= 0 || v.Skipped) continue;
                List<IbInvoiceRow> l;
                if (!byMonth.TryGetValue(v.Period, out l)) { l = new List<IbInvoiceRow>(); byMonth[v.Period] = l; }
                l.Add(v);
            }
            foreach (KeyValuePair<int, List<IbInvoiceRow>> kv in byMonth)
            {
                List<IbContractRow> contracts = new List<IbContractRow>();
                HashSet<string> keys = new HashSet<string>();
                foreach (IbInvoiceRow v in kv.Value)
                {
                    if (!contracts.Contains(v.Contract)) contracts.Add(v.Contract);
                    keys.Add(v.JobKey);
                }
                if (!GenerateMonth(kv.Key, contracts, keys)) break;
            }
            LoadData();
        }

        /// <summary>One run per month, oldest first. Each contract bills its own month, so the ready
        /// contracts can span months; the invoices of one month are previewed and saved together.</summary>
        private void Generate(List<IbContractRow> contracts)
        {
            if (_db == null || contracts == null || contracts.Count == 0) return;
            SortedDictionary<int, List<IbContractRow>> byMonth = new SortedDictionary<int, List<IbContractRow>>();
            foreach (IbContractRow c in contracts)
            {
                if (c.Period <= 0) continue;
                List<IbContractRow> l;
                if (!byMonth.TryGetValue(c.Period, out l)) { l = new List<IbContractRow>(); byMonth[c.Period] = l; }
                l.Add(c);
            }
            foreach (KeyValuePair<int, List<IbContractRow>> kv in byMonth)
                if (!GenerateMonth(kv.Key, kv.Value)) break;
            LoadData();
        }

        /// <summary>Jobs from the rows the board priced with HQ's readings, the usual preview, then the
        /// readings of every approved contract are written into this book and the invoices are saved.
        /// False = stopped (preview closed, or the readings could not be saved).</summary>
        private bool GenerateMonth(int period, List<IbContractRow> contracts) { return GenerateMonth(period, contracts, null); }

        private bool GenerateMonth(int period, List<IbContractRow> contracts, HashSet<string> onlyJobKeys)
        {
            int y = ScpBillingSequence.YearOf(period), m = ScpBillingSequence.MonthOf(period);
            string name = ScpBillingSequence.Name(period);
            Dictionary<long, string> snapshots;
            List<string> skipped;
            List<MeterInvoiceGenerator.InvoiceJob> jobs = ScpInterBillBoard.BuildJobs(_db, contracts, y, m, onlyJobKeys, out snapshots, out skipped);
            if (jobs.Count == 0)
            {
                XtraMessageBox.Show(name + ": nothing to generate." + (skipped.Count > 0 ? "\r\n\r\n" + string.Join("\r\n", skipped.ToArray()) : ""),
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            string note = "HQ readings are saved into this book with each invoice." +
                          (skipped.Count > 0 ? "  Left out: " + string.Join("; ", skipped.ToArray()) : "");
            using (MeterInvoicePreview_Form pv = new MeterInvoicePreview_Form(_db, jobs, name, note))
            {
                if (pv.ShowDialog(this) != DialogResult.OK) return false;
                jobs = pv.Approved;
            }
            if (jobs.Count == 0) return false;

            HashSet<long> approved = new HashSet<long>();
            HashSet<long> approvedMeters = new HashSet<long>();
            foreach (MeterInvoiceGenerator.InvoiceJob j in jobs)
                if (j.Lines != null)
                    foreach (MeterBillLine ln in j.Lines) { approved.Add(ln.ContractKey); if (ln.ItemMeterKey > 0) approvedMeters.Add(ln.ItemMeterKey); }
            try
            {
                foreach (IbContractRow c in contracts)
                    if (approved.Contains(c.LocalKey)) ScpInterBillBoard.SaveHqReadings(_db, c, y, m, approvedMeters);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("The HQ readings could not be saved, so nothing was generated:\r\n" + ex.Message,
                    "Inter-Billing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            using (MeterInvoiceGenerateProgress_Form dlg = new MeterInvoiceGenerateProgress_Form(_db, jobs, DateTime.Today, DateTime.Now, y, m, snapshots))
            {
                dlg.ShowDialog(this);
            }
            return true;
        }
    }
}
