using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Calculation Test -- key in a month's readings on one contract and see what it would bill.
    ///
    /// <para>The money is worked out by the code that bills it, not by a copy of the formula:
    /// the rows come from <see cref="ScpBillingRows.ForContract"/> and the lines from
    /// <see cref="ScpInvoiceJobs.Build"/>, the same two calls Inter-Billing makes before it
    /// generates. So FOC, tier price, rebate, minimum charge, committed minimums, waives, group
    /// tiers and the rental split all come out here exactly as they would on the invoice. A test
    /// that ran its own arithmetic would only prove that the test agrees with itself.</para>
    ///
    /// <para>Nothing is written. The readings and prices typed here live in this window; the
    /// engine is handed a copy of the rows and the ladders, and Build only reads.</para>
    /// </summary>
    public partial class CalculationTest_Form : XtraForm
    {
        private const string FIELD_INITIAL = "Initial";
        private const string FIELD_CURRENT = "Current";
        private const string FIELD_RATE = "Rate";
        private const string FIELD_FOC = "Foc";
        private const string FIELD_REBATE = "Rebate";
        private const string FIELD_MIN = "Min";

        private readonly DBSetting _db;
        private readonly long _contractKey;
        private int _year;
        private int _month;
        private bool _loading;

        /// <summary>The month's rows as billing loads them. Never handed to the engine itself --
        /// every calculation works on a copy, so what was typed can always be taken back.</summary>
        private DataTable _rows;
        private Dictionary<string, List<decimal[]>> _ladders;

        private readonly DataTable _view = NewView();
        private readonly DataTable _tiers = NewTiers();
        private readonly DataTable _pay = NewPay();

        /// <summary>What the user has typed, as "meterKey|field". A value nobody typed follows the
        /// contract, so changing the month re-reads it; a typed one is kept.</summary>
        private readonly HashSet<string> _typed = new HashSet<string>();
        /// <summary>Bands typed for one meter. Present and empty = the meter tested at a flat price.</summary>
        private readonly Dictionary<long, List<decimal[]>> _tierOverride = new Dictionary<long, List<decimal[]>>();

        // The last calculation, for the working pane.
        private readonly Dictionary<long, DataRow> _calcRows = new Dictionary<long, DataRow>();
        private Dictionary<string, List<decimal[]>> _calcLadders;
        private readonly Dictionary<long, MeterBillLine> _billed = new Dictionary<long, MeterBillLine>();
        private readonly Dictionary<long, MeterBillLine> _alone = new Dictionary<long, MeterBillLine>();
        private readonly Dictionary<long, string> _invoiceOf = new Dictionary<long, string>();
        private long _tierKey;

        public CalculationTest_Form()
        {
            InitializeComponent();
        }

        public CalculationTest_Form(DBSetting db, long contractKey) : this()
        {
            _db = db;
            _contractKey = contractKey;
            _year = DateTime.Today.Year;
            _month = DateTime.Today.Month;

            GridMeters.DataSource = _view;
            GridTiers.DataSource = _tiers;
            GridPay.DataSource = _pay;

            _loading = true;
            CultureInfo en = new CultureInfo("en-US");
            for (int m = 1; m <= 12; m++) CmbMonth.Properties.Items.Add(en.DateTimeFormat.GetMonthName(m));
            CmbMonth.SelectedIndex = _month - 1;
            SpnYear.EditValue = (decimal)_year;
            _loading = false;

            LblTitle.Text = "Calculation Test - " + ContractTitle();
            SetIcon();
            Wire();
            LoadMonth();
        }

        private void Wire()
        {
            CmbMonth.SelectedIndexChanged += new EventHandler(Month_Changed);
            SpnYear.EditValueChanged += new EventHandler(Month_Changed);
            BtnReset.Click += new EventHandler(BtnReset_Click);
            BtnClose.Click += new EventHandler(BtnClose_Click);
            GridViewMeters.ShowingEditor += new System.ComponentModel.CancelEventHandler(Meters_ShowingEditor);
            GridViewMeters.CellValueChanged += new CellValueChangedEventHandler(Meters_CellValueChanged);
            GridViewMeters.FocusedRowChanged += new FocusedRowChangedEventHandler(Meters_FocusedRowChanged);
            GridViewMeters.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(Meters_RowCellStyle);
            GridViewTiers.CellValueChanged += new CellValueChangedEventHandler(Tiers_Changed);
            GridViewTiers.RowDeleted += new DevExpress.Data.RowDeletedEventHandler(Tiers_RowDeleted);
            GridViewTiers.KeyDown += new KeyEventHandler(Tiers_KeyDown);
        }

        private void SetIcon()
        {
            try
            {
                DevExpress.Utils.Svg.SvgImage svg =
                    DevExpress.Images.ImageResourceCache.Default.GetSvgImage("svgimages/icon%20builder/business_calculator.svg");
                if (svg != null) this.IconOptions.SvgImage = svg;
            }
            catch { }   // an icon is cosmetic
        }

        private string ContractTitle()
        {
            try
            {
                DataTable t = _db.GetDataTable(
                    "SELECT c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS CompanyName " +
                    "FROM dbo.zSCP2_Contract c LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "WHERE c.ContractKey = " + _contractKey, false);
                if (t.Rows.Count == 0) return "";
                DataRow r = t.Rows[0];
                string who = S(r["CompanyName"]).Trim();
                return S(r["ContractNo"]).Trim() + "  (" + S(r["DebtorCode"]).Trim() +
                       (who.Length > 0 ? " - " + who : "") + ")";
            }
            catch { return ""; }
        }

        // ------------------------------------------------------------------ tables

        private static DataTable NewView()
        {
            DataTable t = new DataTable("Meters");
            t.Columns.Add("ItemMeterKey", typeof(long));
            t.Columns.Add("ServiceItemNo", typeof(string));
            t.Columns.Add("SerialNo", typeof(string));
            t.Columns.Add("Meter", typeof(string));
            t.Columns.Add("MeterTypeName", typeof(string));
            t.Columns.Add("Kind", typeof(string));
            t.Columns.Add("IsFlat", typeof(bool));
            t.Columns.Add(FIELD_INITIAL, typeof(decimal));
            t.Columns.Add(FIELD_CURRENT, typeof(decimal));
            t.Columns.Add("Copies", typeof(decimal));
            t.Columns.Add(FIELD_RATE, typeof(decimal));
            t.Columns.Add("TierText", typeof(string));
            t.Columns.Add(FIELD_FOC, typeof(decimal));
            t.Columns.Add(FIELD_REBATE, typeof(decimal));
            t.Columns.Add(FIELD_MIN, typeof(decimal));
            t.Columns.Add("Billed", typeof(decimal));
            t.Columns.Add("Unit", typeof(decimal));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("Note", typeof(string));
            t.Columns.Add("Invoice", typeof(string));
            return t;
        }

        private static DataTable NewTiers()
        {
            DataTable t = new DataTable("Tiers");
            t.Columns.Add("UpTo", typeof(decimal));
            t.Columns.Add("Price", typeof(decimal));
            return t;
        }

        private static DataTable NewPay()
        {
            DataTable t = new DataTable("Pay");
            t.Columns.Add("Invoice", typeof(string));
            t.Columns.Add("Amount", typeof(decimal));
            return t;
        }

        // ------------------------------------------------------------------ loading

        /// <summary>Reads the month's rows the way billing does, then lays them out with anything the
        /// user has typed kept in place.</summary>
        private void LoadMonth()
        {
            Cursor old = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                Dictionary<string, List<decimal[]>> ladders;
                _rows = ScpBillingRows.ForContract(_db, _contractKey, _year, _month, out ladders);
                _ladders = ladders ?? new Dictionary<string, List<decimal[]>>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _rows = null;
                ShowProblem("The contract could not be read: " + ex.Message);
                this.Cursor = old;
                return;
            }

            Dictionary<long, DataRow> before = new Dictionary<long, DataRow>();
            DataTable previous = _view.Copy();
            foreach (DataRow p in previous.Rows) before[D64(p["ItemMeterKey"])] = p;

            _loading = true;
            try
            {
                _view.Rows.Clear();
                foreach (DataRow r in _rows.Rows)
                {
                    long key = D64(r["ItemMeterKey"]);
                    bool flat = Bool(r["IsFlat"]);
                    DataRow v = _view.NewRow();
                    v["ItemMeterKey"] = key;
                    v["ServiceItemNo"] = Bool(r["IsGroupItem"]) ? "(whole contract)" : S(r["ServiceItemNo"]);
                    v["SerialNo"] = S(r["SerialNo"]);
                    v["Meter"] = S(r["MeterType"]);
                    v["MeterTypeName"] = S(r["MeterTypeName"]);
                    v["Kind"] = KindOf(r);
                    v["IsFlat"] = flat;
                    v[FIELD_INITIAL] = flat ? 0m : Dec(r["LastReading"]);
                    v[FIELD_CURRENT] = flat ? 0m : Dec(r["CurrentReading"]);
                    v[FIELD_RATE] = Dec(r["UnitPrice"]);
                    v[FIELD_FOC] = Dec(r["FOCQty"]);
                    v[FIELD_REBATE] = Dec(r["RebatePct"]);
                    v[FIELD_MIN] = Dec(r["MinCharges"]);

                    DataRow p;
                    if (before.TryGetValue(key, out p))
                    {
                        foreach (string f in new string[] { FIELD_INITIAL, FIELD_CURRENT, FIELD_RATE, FIELD_FOC, FIELD_REBATE, FIELD_MIN })
                            if (_typed.Contains(key + "|" + f)) v[f] = p[f];
                    }
                    v["TierText"] = flat ? "" : ScpMultiPrice.Describe(TiersOf(key, S(r["MultiPriceCode"])));
                    _view.Rows.Add(v);
                }
            }
            finally { _loading = false; }

            this.Cursor = old;
            Recalculate();
            LoadTierGrid();
        }

        private string KindOf(DataRow r)
        {
            if (!Bool(r["IsFlat"])) return "Copies";
            if (Bool(r["IsWaive"])) return "Waive";
            if (ScpBillingRows.IsCommitRow(r)) return "Committed minimum";
            if (ScpStrategy.IsRentalRole(S(r["Role"]), S(r["MeterType"]))) return "Rental";
            return "Flat charge";
        }

        /// <summary>The bands a meter is priced on in this test: typed ones first, else the contract's.</summary>
        private List<decimal[]> TiersOf(long key, string multiPriceCode)
        {
            List<decimal[]> bands;
            if (_tierOverride.TryGetValue(key, out bands)) return bands;
            if (_ladders != null && !string.IsNullOrEmpty(multiPriceCode) && _ladders.TryGetValue(multiPriceCode, out bands))
                return bands;
            return new List<decimal[]>();
        }

        // ------------------------------------------------------------------ the calculation

        /// <summary>
        /// Hands the engine a copy of the month's rows with the typed readings and prices in them,
        /// and shows what it bills.
        /// </summary>
        private void Recalculate()
        {
            _billed.Clear();
            _alone.Clear();
            _invoiceOf.Clear();
            _calcRows.Clear();
            _pay.Rows.Clear();
            if (_rows == null) return;

            DataTable rows = _rows.Copy();
            Dictionary<string, List<decimal[]>> ladders =
                new Dictionary<string, List<decimal[]>>(_ladders, StringComparer.OrdinalIgnoreCase);
            Dictionary<long, DataRow> viewOf = new Dictionary<long, DataRow>();
            foreach (DataRow v in _view.Rows) viewOf[D64(v["ItemMeterKey"])] = v;

            List<DataRow> all = new List<DataRow>();
            foreach (DataRow r in rows.Rows)
            {
                long key = D64(r["ItemMeterKey"]);
                DataRow v;
                if (!viewOf.TryGetValue(key, out v)) continue;
                bool flat = Bool(r["IsFlat"]);
                if (!flat)
                {
                    r["LastReading"] = Dec(v[FIELD_INITIAL]);
                    r["CurrentReading"] = Dec(v[FIELD_CURRENT]);
                }
                r["UnitPrice"] = Dec(v[FIELD_RATE]);
                r["FOCQty"] = Dec(v[FIELD_FOC]);
                r["RebatePct"] = Dec(v[FIELD_REBATE]);
                r["MinCharges"] = Dec(v[FIELD_MIN]);

                List<decimal[]> bands;
                if (!flat && _tierOverride.TryGetValue(key, out bands))
                {
                    // Under a key of its own: a scheme code is shared by every meter on it, and a
                    // test of one meter must not reprice the others.
                    string code = bands.Count > 0 ? "#TEST" + key : "";
                    if (bands.Count > 0) ladders[code] = bands;
                    r["MultiPriceCode"] = code;
                }

                // "Bills its minimum" is decided when the rows load, from the price and the minimum.
                // Asked again only where the user changed one of them: the loaded answer can also come
                // from an agreed line price, which this screen does not second-guess.
                if (_typed.Contains(key + "|" + FIELD_RATE) || _typed.Contains(key + "|" + FIELD_MIN))
                    r["UseMin"] = Dec(r["UnitPrice"]) == 0m && Dec(r["MinCharges"]) > 0m &&
                                  !ScpStrategy.IsCommittedMinRole(S(r["Role"]), S(r["MeterType"]), Dec(r["MinCharges"]));
                // A rental's FOC is its free months left: 0.00 this month while there are any.
                if (flat && _typed.Contains(key + "|" + FIELD_FOC) && !Bool(r["IsWaive"]) && !ScpBillingRows.IsCommitRow(r))
                    r["EntrySource"] = Dec(r["FOCQty"]) > 0m ? "RENTAL FREE" : "RENTAL";

                // A month already invoiced is still worked out: nothing is written, and the question
                // is what the numbers come to, not whether the month is open.
                r["InvoicedDocNo"] = "";
                r["Sel"] = true;
                all.Add(r);
                _calcRows[key] = r;
            }
            _calcLadders = ladders;

            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs = null;
            string blockTitle = "", blockMessage = "";
            try
            {
                int alreadyInvoiced;
                Dictionary<long, string> snapshots;
                jobs = ScpInvoiceJobs.Build(_db, rows, all, ladders, _year, _month,
                    ScpBillingRows.GroupingMode(_db),
                    out alreadyInvoiced, out snapshots, out blockTitle, out blockMessage);
            }
            catch (Exception ex)
            {
                jobs = null;
                blockTitle = "Calculation Test";
                blockMessage = ex.Message;
            }

            // Each line on its own first -- the first thing Build does to it, before any term that
            // spans the contract. The working pane shows both, so a line a term changed says so.
            foreach (DataRow r in all) _alone[D64(r["ItemMeterKey"])] = PriceAlone(r, ladders, true);

            decimal total = 0m;
            if (jobs != null)
            {
                List<MeterInvoiceGenerator.InvoiceJob> ordered = new List<MeterInvoiceGenerator.InvoiceJob>(jobs.Values);
                ordered.Sort(delegate(MeterInvoiceGenerator.InvoiceJob a, MeterInvoiceGenerator.InvoiceJob b)
                {
                    return string.Compare(a.Description, b.Description, StringComparison.OrdinalIgnoreCase);
                });
                foreach (MeterInvoiceGenerator.InvoiceJob job in ordered)
                {
                    decimal sum = 0m;
                    foreach (MeterBillLine ln in job.Lines)
                    {
                        _billed[ln.ItemMeterKey] = ln;
                        _invoiceOf[ln.ItemMeterKey] = job.Description;
                        sum += ln.Charge;
                    }
                    DataRow p = _pay.NewRow();
                    p["Invoice"] = job.Description;
                    p["Amount"] = sum;
                    _pay.Rows.Add(p);
                    total += sum;
                }
            }

            _loading = true;
            try
            {
                foreach (DataRow v in _view.Rows) ShowResult(v);
            }
            finally { _loading = false; }

            LblTotal.Text = "Total  " + total.ToString("n2");
            if (jobs == null) ShowProblem(blockTitle + ": " + blockMessage);
            else if (jobs.Count == 0) ShowNote("Nothing bills yet - key in a current reading.");
            else ShowNote(jobs.Count == 1
                ? "Worked out by the billing engine. This contract bills on one invoice."
                : "Worked out by the billing engine. This contract bills on " + jobs.Count + " invoices.");
            ShowWorking();
        }

        /// <summary>One line priced by itself, the way Build prices it before any contract-wide term.
        /// <paramref name="withMin"/> false leaves the minimum out, to show what it changed.</summary>
        private MeterBillLine PriceAlone(DataRow r, Dictionary<string, List<decimal[]>> ladders, bool withMin)
        {
            MeterBillLine ln = new MeterBillLine();
            ln.IsFlat = Bool(r["IsFlat"]);
            ln.Last = Dec(r["LastReading"]);
            ln.Current = Dec(r["CurrentReading"]);
            ln.Rate = Dec(r["UnitPrice"]);
            ln.MinCharges = withMin ? Dec(r["MinCharges"]) : 0m;
            ln.Foc = Dec(r["FOCQty"]);
            ln.RebatePct = Dec(r["RebatePct"]);
            ln.MultiPriceCode = S(r["MultiPriceCode"]);
            ln.FocResetCount = ScpInvoiceJobs.FocResetCountFor(r, _year, _month);
            ln.NewMoneyRules = Bool(r["NewMoneyRules"]);
            ScpInvoiceBuilder.ComputeCharge(ln, ladders);
            if (withMin && Bool(r["UseMin"])) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
            return ln;
        }

        private void ShowResult(DataRow v)
        {
            long key = D64(v["ItemMeterKey"]);
            bool flat = Bool(v["IsFlat"]);
            MeterBillLine b;
            bool onInvoice = _billed.TryGetValue(key, out b);
            MeterBillLine a;
            _alone.TryGetValue(key, out a);

            decimal copies = Dec(v[FIELD_CURRENT]) - Dec(v[FIELD_INITIAL]);
            v["Copies"] = flat ? (object)DBNull.Value : (copies < 0m ? 0m : copies);
            if (onInvoice)
            {
                v["Billed"] = flat ? (object)DBNull.Value : b.BillCopies;
                v["Unit"] = flat ? (object)DBNull.Value : b.EffUnitPrice;
                v["Amount"] = b.Charge;
                v["Invoice"] = _invoiceOf[key];
            }
            else
            {
                v["Billed"] = DBNull.Value;
                v["Unit"] = DBNull.Value;
                v["Amount"] = DBNull.Value;
                v["Invoice"] = "";
            }
            v["Note"] = NoteFor(v, a, onInvoice ? b : null);
        }

        private string NoteFor(DataRow v, MeterBillLine alone, MeterBillLine billed)
        {
            bool flat = Bool(v["IsFlat"]);
            if (billed == null)
            {
                if (!flat && Dec(v[FIELD_CURRENT]) <= 0m) return "No current reading - not billed";
                if (Convert.ToString(v["Kind"]) == "Waive") return "Rental free this month instead";
                return "Not billed this month";
            }
            if (!flat && Dec(v[FIELD_CURRENT]) < Dec(v[FIELD_INITIAL])) return "Current is below initial - 0 copies";
            if (!string.IsNullOrEmpty(billed.StrategyNote)) return billed.StrategyNote;
            if (alone != null && billed.Charge != alone.Charge) return "Changed by the contract's group terms";
            if (billed.UseMin) return "Minimum charge applies";
            if (!flat && ScpMultiPrice.HasLadder(_calcLadders, billed.MultiPriceCode)) return "Tier price";
            if (!flat && billed.Foc > 0m) return "Less FOC";
            return "";
        }

        // ------------------------------------------------------------------ the working

        /// <summary>The focused line, worked out in words and numbers, step by step.</summary>
        private void ShowWorking()
        {
            DataRow v = FocusedView();
            if (v == null) { MemoWorking.Text = ""; return; }
            long key = D64(v["ItemMeterKey"]);
            DataRow r;
            if (!_calcRows.TryGetValue(key, out r)) { MemoWorking.Text = ""; return; }
            MeterBillLine a;
            _alone.TryGetValue(key, out a);
            MeterBillLine b;
            bool onInvoice = _billed.TryGetValue(key, out b);

            StringBuilder sb = new StringBuilder();
            string who = S(v["ServiceItemNo"]) + (S(v["SerialNo"]).Length > 0 ? "  S/N " + S(v["SerialNo"]) : "");
            sb.AppendLine(who + "  -  " + S(v["Meter"]) + " " + S(v["MeterTypeName"]));
            sb.AppendLine();
            if (Bool(r["IsFlat"])) WorkFlat(sb, v, r, a, b);
            else WorkCopies(sb, r, a, b);
            sb.AppendLine();
            if (onInvoice)
            {
                sb.AppendLine(Step("Bills", b.Charge.ToString("n2") + "   on " + _invoiceOf[key]));
            }
            else
            {
                sb.AppendLine(Step("Bills", "nothing - " + S(v["Note"]).ToLowerInvariant()));
            }
            MemoWorking.Text = sb.ToString();
        }

        private void WorkCopies(StringBuilder sb, DataRow r, MeterBillLine a, MeterBillLine b)
        {
            decimal last = Dec(r["LastReading"]);
            decimal current = Dec(r["CurrentReading"]);
            if (current <= 0m)
            {
                sb.AppendLine("No current reading yet. Billing leaves a meter without one out, so key one in.");
                return;
            }
            decimal usage = current - last;
            if (usage < 0m)
            {
                sb.AppendLine(Step("Copies", "current " + N0(current) + " is below initial " + N0(last) + " -> counted as 0"));
                usage = 0m;
            }
            else
            {
                sb.AppendLine(Step("Copies", "current " + N0(current) + " - initial " + N0(last) + " = " + N0(usage)));
            }
            if (a == null) return;

            int resetN = ScpInvoiceJobs.FocResetCountFor(r, _year, _month);
            string code = S(r["MultiPriceCode"]);
            decimal toBill;
            if (ScpMultiPrice.HasLadder(_calcLadders, code))
            {
                List<decimal[]> bands = _calcLadders[code];
                decimal free, rate;
                ScpMultiPrice.LadderCharge(_calcLadders, code, usage, resetN, out free, out rate);
                sb.AppendLine(Step("Tier price", ScpMultiPrice.Describe(bands)));
                // The ladder replaces both: its 0.00 band IS the free allowance, and its bands the price.
                List<string> unused = new List<string>();
                if (Dec(r["UnitPrice"]) != 0m) unused.Add("Price " + P(Dec(r["UnitPrice"])));
                if (Dec(r["FOCQty"]) != 0m) unused.Add("FOC " + N0(Dec(r["FOCQty"])));
                if (unused.Count > 0)
                    sb.AppendLine(Step("", "(the tier price replaces " + string.Join(" and ", unused.ToArray()) + ")"));
                sb.AppendLine(Step("", BandWords(bands, usage, resetN) + " -> " + P(rate) + " a copy"));
                if (resetN > 1)
                    sb.AppendLine(Step("", "(each band x " + resetN + ": the allowance resets " + resetN + " times this month)"));
                toBill = usage - free;
                if (toBill < 0m) toBill = 0m;
                sb.AppendLine(Step("Free copies", free > 0m
                    ? "the 0.00 band covers " + N0(free) + " -> " + N0(usage) + " - " + N0(free) + " = " + N0(toBill) + " to bill"
                    : "none -> " + N0(toBill) + " to bill"));
            }
            else
            {
                decimal foc = Dec(r["FOCQty"]) * resetN;
                toBill = usage - foc;
                if (toBill < 0m) toBill = 0m;
                sb.AppendLine(Step("Price", P(Dec(r["UnitPrice"])) + " a copy"));
                if (foc > 0m)
                    sb.AppendLine(Step("FOC", N0(foc) + " free" +
                        (resetN > 1 ? " (" + N0(Dec(r["FOCQty"])) + " x " + resetN + " resets)" : "") +
                        " -> " + N0(usage) + " - " + N0(foc) + " = " + N0(toBill) + " to bill"));
                else
                    sb.AppendLine(Step("FOC", "none -> " + N0(toBill) + " to bill"));
            }

            decimal pct = Dec(r["RebatePct"]);
            bool newRules = Bool(r["NewMoneyRules"]);
            MeterBillLine noMin = PriceAlone(r, _calcLadders, false);
            if (pct > 0m && newRules)
            {
                sb.AppendLine(Step("Rebate " + pct.ToString("0.##") + "%", "floor(" + N0(toBill) + " x " + pct.ToString("0.##") + "%) = " +
                    N0(noMin.RebateQty) + " copies off -> " + N0(noMin.BillCopies) + " to bill"));
                sb.AppendLine(Step("Amount", N0(noMin.BillCopies) + " x " + P(noMin.EffUnitPrice) + " = " + noMin.Charge.ToString("n2")));
            }
            else if (pct > 0m)
            {
                decimal sub = Math.Round(noMin.BillCopies * noMin.EffUnitPrice, 2);
                sb.AppendLine(Step("Amount", N0(noMin.BillCopies) + " x " + P(noMin.EffUnitPrice) + " = " + sub.ToString("n2")));
                sb.AppendLine(Step("Rebate " + pct.ToString("0.##") + "%", "taken off the amount -> " + noMin.Charge.ToString("n2") +
                    "   (no billing format on this contract)"));
            }
            else
            {
                sb.AppendLine(Step("Amount", N0(noMin.BillCopies) + " x " + P(noMin.EffUnitPrice) + " = " + noMin.Charge.ToString("n2")));
            }

            decimal min = Dec(r["MinCharges"]);
            if (Bool(r["UseMin"]))
                sb.AppendLine(Step("Min. charge", "the price is 0.00, so the meter bills its minimum " + min.ToString("n2")));
            else if (min > 0m && noMin.Charge < min)
                sb.AppendLine(Step("Min. charge", min.ToString("n2") + " is more than " + noMin.Charge.ToString("n2") + " -> " + min.ToString("n2")));
            else if (min > 0m)
                sb.AppendLine(Step("Min. charge", min.ToString("n2") + " is not more than " + noMin.Charge.ToString("n2") + " -> no change"));
            else
                sb.AppendLine(Step("Min. charge", "none"));

            if (b != null && b.Charge != a.Charge)
                sb.AppendLine(Step("Contract terms", (string.IsNullOrEmpty(b.StrategyNote)
                    ? "the group's terms price this meter with the others" : b.StrategyNote) +
                    " -> " + b.Charge.ToString("n2")));
        }

        private void WorkFlat(StringBuilder sb, DataRow v, DataRow r, MeterBillLine a, MeterBillLine b)
        {
            string kind = S(v["Kind"]);
            decimal rate = Dec(r["UnitPrice"]);
            decimal min = Dec(r["MinCharges"]);
            decimal foc = Dec(r["FOCQty"]);
            if (kind == "Waive")
            {
                decimal amount = min != 0m ? Math.Abs(min) : Math.Abs(rate);
                sb.AppendLine(Step("Waive", amount.ToString("n2") + " off the rental when its terms are met"));
                if (b != null && !string.IsNullOrEmpty(b.StrategyNote)) sb.AppendLine(Step("This month", b.StrategyNote));
                return;
            }
            if (kind == "Committed minimum")
            {
                sb.AppendLine(Step("Committed", min.ToString("n2") + " a month, measured against the copies it covers"));
                if (b != null && b.IsCommittedMin)
                    sb.AppendLine(Step("", "the copies came to " + b.PrintedAmount.ToString("n2") + " -> top-up " +
                        b.Charge.ToString("n2")));
                if (b != null && !string.IsNullOrEmpty(b.StrategyNote)) sb.AppendLine(Step("This month", b.StrategyNote));
                return;
            }
            sb.AppendLine(Step(kind, rate.ToString("n2") + " a month"));
            if (foc > 0m)
                sb.AppendLine(Step("FOC", N0(foc) + " free month" + (foc == 1m ? "" : "s") + " left -> nothing to pay this month"));
            else if (Bool(r["UseMin"]))
                sb.AppendLine(Step("Min. charge", "the price is 0.00, so it bills its minimum " + min.ToString("n2")));
            else if (min > rate)
                sb.AppendLine(Step("Min. charge", min.ToString("n2") + " is more than " + rate.ToString("n2") + " -> " + min.ToString("n2")));
            if (b != null && a != null && b.Charge != a.Charge)
                sb.AppendLine(Step("Contract terms", (string.IsNullOrEmpty(b.StrategyNote)
                    ? "changed by the contract's terms" : b.StrategyNote) + " -> " + b.Charge.ToString("n2")));
        }

        /// <summary>Which band the copies reached, in words: "2,500 copies reach the band up to 5,000".</summary>
        private static string BandWords(List<decimal[]> bands, decimal usage, int resetN)
        {
            if (resetN < 1) resetN = 1;
            decimal lastBound = 0m;
            foreach (decimal[] t in bands)
            {
                lastBound = t[0] * resetN;
                if (usage <= lastBound) return N0(usage) + " copies reach the band up to " + N0(lastBound);
            }
            return N0(usage) + " copies are past the top band (" + N0(lastBound) + ")";
        }

        private static string Step(string label, string text)
        {
            return (label + ":").PadRight(16) + text;
        }

        // ------------------------------------------------------------------ tier grid

        private void LoadTierGrid()
        {
            DataRow v = FocusedView();
            _loading = true;
            try
            {
                _tiers.Rows.Clear();
                _tierKey = 0L;
                if (v == null || Bool(v["IsFlat"]))
                {
                    GroupTiers.Text = "Tier Price";
                    GridTiers.Enabled = false;
                    return;
                }
                _tierKey = D64(v["ItemMeterKey"]);
                string code = "";
                if (_rows != null)
                    foreach (DataRow x in _rows.Rows)
                        if (D64(x["ItemMeterKey"]) == _tierKey) { code = S(x["MultiPriceCode"]); break; }
                foreach (decimal[] t in TiersOf(_tierKey, code))
                {
                    DataRow tr = _tiers.NewRow();
                    tr["UpTo"] = t[0];
                    tr["Price"] = t[1];
                    _tiers.Rows.Add(tr);
                }
                GroupTiers.Text = "Tier Price - " + S(v["ServiceItemNo"]) + " " + S(v["Meter"]);
                GridTiers.Enabled = true;
            }
            finally { _loading = false; }
        }

        /// <summary>The bands as typed, for the focused meter. None left = that meter at a flat price.</summary>
        private void TiersEdited()
        {
            if (_loading || _tierKey == 0L) return;
            List<decimal[]> bands = new List<decimal[]>();
            foreach (DataRow t in _tiers.Rows)
            {
                if (t.RowState == DataRowState.Deleted || t.RowState == DataRowState.Detached) continue;
                decimal upTo = Dec(t["UpTo"]);
                if (upTo <= 0m) continue;
                bands.Add(new decimal[] { upTo, Dec(t["Price"]) });
            }
            bands.Sort(delegate(decimal[] x, decimal[] y) { return x[0].CompareTo(y[0]); });
            _tierOverride[_tierKey] = bands;
            foreach (DataRow v in _view.Rows)
                if (D64(v["ItemMeterKey"]) == _tierKey)
                {
                    _loading = true;
                    try { v["TierText"] = ScpMultiPrice.Describe(bands); }
                    finally { _loading = false; }
                }
            Recalculate();
        }

        private void Tiers_Changed(object sender, CellValueChangedEventArgs e) { TiersEdited(); }

        private void Tiers_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e) { TiersEdited(); }

        private void Tiers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete || GridViewTiers.IsEditing) return;
            if (GridViewTiers.FocusedRowHandle < 0) return;
            GridViewTiers.DeleteRow(GridViewTiers.FocusedRowHandle);
            e.Handled = true;
        }

        // ------------------------------------------------------------------ main grid events

        private void Meters_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataRow v = FocusedView();
            if (v == null) { e.Cancel = true; return; }
            string f = GridViewMeters.FocusedColumn == null ? "" : GridViewMeters.FocusedColumn.FieldName;
            // A flat charge has no meter to read and nothing to rebate.
            if (Bool(v["IsFlat"]) && (f == FIELD_INITIAL || f == FIELD_CURRENT || f == FIELD_REBATE))
                e.Cancel = true;
        }

        private void Meters_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (_loading || e.Column == null) return;
            DataRow v = GridViewMeters.GetDataRow(e.RowHandle);
            if (v == null) return;
            if (v[e.Column.FieldName] == DBNull.Value) v[e.Column.FieldName] = 0m;
            _typed.Add(D64(v["ItemMeterKey"]) + "|" + e.Column.FieldName);
            Recalculate();
        }

        private void Meters_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            LoadTierGrid();
            ShowWorking();
        }

        private void Meters_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null) return;
            string f = e.Column.FieldName;
            DataRow v = GridViewMeters.GetDataRow(e.RowHandle);
            if (v == null) return;
            bool flat = Bool(v["IsFlat"]);
            // Nothing to key in on a flat charge; and on a tiered meter the bands decide the price
            // and the free copies, so Price and FOC are not used. Grey, not the input colour.
            bool unused = flat
                ? (f == FIELD_INITIAL || f == FIELD_CURRENT || f == FIELD_REBATE)
                : ((f == FIELD_RATE || f == FIELD_FOC) && S(v["TierText"]).Length > 0);
            if (!unused) return;
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(170, 170, 170);
        }

        private void Month_Changed(object sender, EventArgs e)
        {
            if (_loading) return;
            _month = CmbMonth.SelectedIndex + 1;
            _year = Convert.ToInt32(SpnYear.Value);
            if (_month < 1) _month = 1;
            LoadMonth();
        }

        /// <summary>Prices back to what the contract says. The readings stay: they are the test.</summary>
        private void BtnReset_Click(object sender, EventArgs e)
        {
            GridViewMeters.CloseEditor();
            List<string> keep = new List<string>();
            foreach (string t in _typed)
                if (t.EndsWith("|" + FIELD_INITIAL) || t.EndsWith("|" + FIELD_CURRENT)) keep.Add(t);
            _typed.Clear();
            foreach (string t in keep) _typed.Add(t);
            _tierOverride.Clear();
            LoadMonth();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ------------------------------------------------------------------ helpers

        private DataRow FocusedView()
        {
            if (GridViewMeters.FocusedRowHandle < 0) return null;
            return GridViewMeters.GetDataRow(GridViewMeters.FocusedRowHandle);
        }

        private void ShowProblem(string text)
        {
            LblFoot.Appearance.ForeColor = System.Drawing.Color.FromArgb(192, 0, 0);
            LblFoot.Text = text;
        }

        private void ShowNote(string text)
        {
            LblFoot.Appearance.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            LblFoot.Text = text;
        }

        private static string N0(decimal d) { return d.ToString("#,##0"); }
        private static string P(decimal d) { return d.ToString("#,##0.00####"); }
        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal Dec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
        private static bool Bool(object o) { return o != null && o != DBNull.Value && Convert.ToBoolean(o); }
    }
}
