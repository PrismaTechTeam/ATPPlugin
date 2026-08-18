using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Which machines print as ONE line, and what that line costs.
    ///
    /// <para>The three line modes answer the question in general — merge everything, merge by model,
    /// do not merge — and none of them can say "a C5335 and a C5665 together, four C1234 apart".
    /// Ticking machines and merging them says exactly that: the group replaces whatever bucket the
    /// mode would have used, so under "merge by model" it merges models together and under "merge,
    /// ignoring model" it splits a set off the single line. It never turns merging on — a contract
    /// set to one line per machine stays one line per machine.</para>
    ///
    /// <para>A merged rental line is also a deal, so the line carries a price: every rental meter on
    /// it bills at that figure, whatever each machine's own meter says, and a machine added to the
    /// line later joins at it. Leave a line at 0 and its machines keep their own rates.</para>
    ///
    /// <para>Rental only for the price. Black and colour take the same grouping — a machine that
    /// prints with a group prints with it on every line — but each meter keeps its own rate and the
    /// merged usage line is worth the sum of its machines.</para>
    ///
    /// <para>Everything is written back into the contract editor on OK only, and the contract still
    /// has to be saved.</para>
    /// </summary>
    public partial class RentalGroupPrice_Form : XtraForm
    {
        private readonly List<ItemEditData> _items;
        private readonly char _rentalMode;
        private readonly char _meterMode;
        private readonly Dictionary<string, decimal> _prices;   // GroupCode -> price
        private DataTable _dtMachines;
        private DataTable _dtLines;
        private bool _changed;

        /// <summary>The prices as edited, keyed by group code ('' = the whole contract, '#NAME' = a
        /// hand-made group, otherwise a model). A line left at 0 is absent.</summary>
        public Dictionary<string, decimal> Result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        /// <summary>True when a rental line can carry a price at all — when the contract merges its
        /// rental lines. Grouping still matters for black and colour when it does not.</summary>
        private bool Priceable
        {
            get { return _rentalMode != ServiceContractPhotocopier.Classes.ScpBillingFormat.LINE_PER_MACHINE; }
        }

        /// <summary>The mode whose buckets the lines are counted from: the rental one when rental
        /// merges, otherwise the meter one.</summary>
        private char GroupingMode
        {
            get { return Priceable ? _rentalMode : _meterMode; }
        }

        public RentalGroupPrice_Form()
        {
            InitializeComponent();
        }

        public RentalGroupPrice_Form(List<ItemEditData> items, char rentalMode, char meterMode,
            Dictionary<string, decimal> current) : this()
        {
            _items = items ?? new List<ItemEditData>();
            _rentalMode = rentalMode;
            _meterMode = meterMode;
            _prices = current ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _dtMachines = new DataTable();
            _dtMachines.Columns.Add("Idx", typeof(int));
            _dtMachines.Columns.Add("Sel", typeof(bool));
            _dtMachines.Columns.Add("ServiceItemNo", typeof(string));
            _dtMachines.Columns.Add("SerialNumber", typeof(string));
            _dtMachines.Columns.Add("ItemCode", typeof(string));
            _dtMachines.Columns.Add("MergeGroup", typeof(string));   // the machine's own group ('' = none)
            _dtMachines.Columns.Add("PrintsOn", typeof(string));     // the line it lands on
            _dtMachines.Columns.Add("OwnRate", typeof(decimal));

            _dtLines = new DataTable();
            _dtLines.Columns.Add("GroupKey", typeof(string));
            _dtLines.Columns.Add("LineName", typeof(string));
            _dtLines.Columns.Add("Units", typeof(int));
            _dtLines.Columns.Add("OnMachines", typeof(string));
            _dtLines.Columns.Add("UnitPrice", typeof(decimal));
            _dtLines.Columns.Add("Monthly", typeof(decimal));

            BuildMachines();
            this.GridMachines.DataSource = _dtMachines;
            this.GridLines.DataSource = _dtLines;
            this.GridViewLines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewLines_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);

            this.ColUnitPrice.OptionsColumn.AllowEdit = Priceable;
            this.ColUnitPrice.OptionsColumn.ReadOnly = !Priceable;
            this.BtnFromMachines.Enabled = Priceable;
            this.BtnClear.Enabled = Priceable;
            this.LblHint.Text = HintText();

            RebuildLines();
        }

        private string HintText()
        {
            string modeWords =
                GroupingMode == ServiceContractPhotocopier.Classes.ScpBillingFormat.LINE_SAME_MODEL
                    ? "This contract prints one line per model. "
                    : "This contract merges its lines regardless of model. ";
            string grouping = "Tick the machines that belong on ONE line and press Merge — the group " +
                "beats the model either way, so you can put a C5335 and a C5665 on one line and keep four " +
                "C1234 on another.";
            string pricing = Priceable
                ? " Then type what each rental line costs a month for ONE machine; a line left at 0 keeps " +
                  "each machine's own meter rate."
                : " Rental prints one line per machine here, so there is nothing to price — the grouping " +
                  "still decides the black and colour lines.";
            return modeWords + grouping + pricing;
        }

        private void BuildMachines()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d == null || d.IsGroupItem) continue;
                DataRow r = _dtMachines.NewRow();
                r["Idx"] = i;
                r["Sel"] = false;
                r["ServiceItemNo"] = string.IsNullOrWhiteSpace(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber ?? "";
                r["ItemCode"] = d.ItemCode ?? "";
                r["MergeGroup"] = d.MergeGroupCode ?? "";
                decimal rate;
                r["OwnRate"] = RentalRateOf(d, out rate) ? rate : 0m;
                _dtMachines.Rows.Add(r);
            }
        }

        /// <summary>The name of the line a machine lands on, in the words the invoice would use.</summary>
        private string LineNameOf(string groupKey, string modelCode)
        {
            if (groupKey.StartsWith("#")) return groupKey.Substring(1);
            if (groupKey.Length == 0) return "All machines";
            return modelCode;
        }

        // Recount the lines off the machines as they now stand, keeping any price already typed for
        // a line that still exists. Regrouping can retire a line; its price goes with it, because
        // there is no longer anything it was the price of.
        private void RebuildLines()
        {
            Dictionary<string, decimal> keep = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                decimal v = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (v > 0m) keep[Convert.ToString(r["GroupKey"])] = v;
            }
            foreach (KeyValuePair<string, decimal> kv in _prices)
                if (!keep.ContainsKey(kv.Key) && kv.Value > 0m) keep[kv.Key] = kv.Value;

            _dtLines.Rows.Clear();
            List<string> order = new List<string>();
            Dictionary<string, int> units = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<decimal>> rates = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow m in _dtMachines.Rows)
            {
                if (m.RowState == DataRowState.Deleted) continue;
                string model = Convert.ToString(m["ItemCode"]);
                string grp = Convert.ToString(m["MergeGroup"]);
                string key = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.GroupKeyFor(
                    GroupingMode, model, grp);
                m["PrintsOn"] = LineNameOf(key, model);
                decimal own = m["OwnRate"] == DBNull.Value ? 0m : Convert.ToDecimal(m["OwnRate"]);
                if (own <= 0m && Priceable) { /* still counts as a machine on the line */ }
                if (!units.ContainsKey(key))
                {
                    order.Add(key); units[key] = 0; rates[key] = new List<decimal>();
                    names[key] = LineNameOf(key, model);
                }
                units[key] = units[key] + 1;
                if (own > 0m) rates[key].Add(own);
            }

            order.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string key in order)
            {
                DataRow r = _dtLines.NewRow();
                r["GroupKey"] = key;
                r["LineName"] = names[key];
                r["Units"] = units[key];
                r["OnMachines"] = DescribeRates(rates[key]);
                decimal price;
                r["UnitPrice"] = keep.TryGetValue(key, out price) ? price : 0m;
                r["Monthly"] = Convert.ToDecimal(r["UnitPrice"]) * units[key];
                _dtLines.Rows.Add(r);
            }
            GridViewMachines.RefreshData();
            GridViewLines.RefreshData();
            RefreshSummary();
        }

        /// <summary>This machine's monthly rental as its own meter states it. False when the machine
        /// has no rental meter at all.</summary>
        private static bool RentalRateOf(ItemEditData d, out decimal rate)
        {
            rate = 0m;
            if (d == null || d.Meters == null) return false;
            foreach (DataRow mr in d.Meters.Rows)
            {
                if (mr.RowState == DataRowState.Deleted) continue;
                string type = mr.Table.Columns.Contains("MeterTypeCode") ? Convert.ToString(mr["MeterTypeCode"]).Trim() : "";
                string role = mr.Table.Columns.Contains("MeterRole") ? Convert.ToString(mr["MeterRole"]).Trim() : "";
                if (string.Equals(role, "WAIVE", StringComparison.OrdinalIgnoreCase)) continue;
                bool isRental = string.Equals(role, "RENTAL", StringComparison.OrdinalIgnoreCase) ||
                    (type.Length > 0 && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(type));
                if (!isRental) continue;
                decimal r = mr.Table.Columns.Contains("ChargesRate") && mr["ChargesRate"] != DBNull.Value
                    ? Convert.ToDecimal(mr["ChargesRate"]) : 0m;
                decimal m = mr.Table.Columns.Contains("MinimumCharges") && mr["MinimumCharges"] != DBNull.Value
                    ? Convert.ToDecimal(mr["MinimumCharges"]) : 0m;
                rate = r > 0m ? r : m;   // the flat charge is whichever column carries it
                return true;
            }
            return false;
        }

        // "300.00" when they agree, "250.00 - 475.00 (mixed)" when they do not -- which is exactly
        // the case a line price is for.
        private static string DescribeRates(List<decimal> rates)
        {
            if (rates == null || rates.Count == 0) return "";
            decimal lo = rates[0], hi = rates[0];
            for (int i = 1; i < rates.Count; i++)
            {
                if (rates[i] < lo) lo = rates[i];
                if (rates[i] > hi) hi = rates[i];
            }
            if (lo == hi) return lo.ToString("n2");
            return lo.ToString("n2") + " - " + hi.ToString("n2") + " (mixed)";
        }

        // ---- grouping ----

        private List<DataRow> TickedMachines()
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            List<DataRow> list = new List<DataRow>();
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) list.Add(r);
            }
            return list;
        }

        private void BtnMerge_Click(object sender, EventArgs e)
        {
            List<DataRow> ticked = TickedMachines();
            if (ticked.Count < 2)
            {
                XtraMessageBox.Show("Tick at least two machines — a line of one is what not merging already does.",
                    "Merge into one line", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Default the name to a group already in play, so merging a third machine into an
            // existing line is a matter of ticking it and pressing OK.
            string suggested = "";
            foreach (DataRow r in ticked)
            {
                string g = Convert.ToString(r["MergeGroup"]).Trim();
                if (g.Length > 0) { suggested = g; break; }
            }
            if (suggested.Length == 0) suggested = "GROUP 1";
            string name = Convert.ToString(XtraInputBox.Show(
                "Name this line. It is only how you recognise the group here — the invoice prints the " +
                "line's own description.", "Merge into one line", suggested));
            name = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.Sanitize(name);
            if (name.Length == 0) return;
            foreach (DataRow r in ticked) r["MergeGroup"] = name;
            _changed = true;
            RebuildLines();
        }

        private void BtnUngroup_Click(object sender, EventArgs e)
        {
            List<DataRow> ticked = TickedMachines();
            if (ticked.Count == 0) return;
            foreach (DataRow r in ticked) r["MergeGroup"] = "";
            _changed = true;
            RebuildLines();
        }

        // ---- pricing ----

        private void GridViewLines_CellValueChanged(object sender,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "UnitPrice") return;
            decimal price = e.Value == null || e.Value == DBNull.Value ? 0m : Convert.ToDecimal(e.Value);
            if (price < 0m) { price = 0m; GridViewLines.SetRowCellValue(e.RowHandle, "UnitPrice", 0m); }
            object u = GridViewLines.GetRowCellValue(e.RowHandle, "Units");
            int units = u == null || u == DBNull.Value ? 0 : Convert.ToInt32(u);
            GridViewLines.SetRowCellValue(e.RowHandle, "Monthly", price * units);
            _changed = true;
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            int priced = 0, machines = 0;
            decimal money = 0m;
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                int units = r["Units"] == DBNull.Value ? 0 : Convert.ToInt32(r["Units"]);
                decimal price = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                machines += units;
                if (price <= 0m) continue;
                priced++;
                money += price * units;
            }
            int lines = _dtLines.Rows.Count;
            if (lines == 0) { LblSummary.Text = "No machines on this contract yet."; return; }
            string t = lines + " line" + (lines == 1 ? "" : "s") + " - " + machines + " machine" +
                       (machines == 1 ? "" : "s");
            if (Priceable)
            {
                t += " - " + priced + " priced here";
                if (priced > 0) t += " - " + money.ToString("n2") + " a month";
                if (priced < lines) t += "   (the rest keep their own meter rates)";
            }
            LblSummary.Text = t;
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            GridViewLines.CloseEditor();
            GridViewLines.UpdateCurrentRow();
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                r["UnitPrice"] = 0m;
                r["Monthly"] = 0m;
            }
            _changed = true;
            GridViewLines.RefreshData();
            RefreshSummary();
        }

        // Seed every line from what its machines already charge, so a contract only being moved onto
        // the new rules starts from its real numbers. Lines whose machines disagree are left alone --
        // there is no honest number to fill in.
        private void BtnFromMachines_Click(object sender, EventArgs e)
        {
            GridViewLines.CloseEditor();
            GridViewLines.UpdateCurrentRow();
            int filled = 0, skipped = 0;
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string shown = Convert.ToString(r["OnMachines"]);
                decimal v;
                if (shown.IndexOf("mixed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    !decimal.TryParse(shown, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture, out v) || v <= 0m)
                { skipped++; continue; }
                r["UnitPrice"] = v;
                r["Monthly"] = v * (r["Units"] == DBNull.Value ? 0 : Convert.ToInt32(r["Units"]));
                filled++;
            }
            _changed = true;
            GridViewLines.RefreshData();
            RefreshSummary();
            if (skipped > 0)
                XtraMessageBox.Show(filled + " line(s) filled in. " + skipped + " left blank because their " +
                    "machines charge different amounts - type the price the line has agreed.",
                    "Take from machines", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---- apply ----

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            GridViewLines.CloseEditor();
            GridViewLines.UpdateCurrentRow();
            Harvest();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Writes the grouping back into the contract editor's machines and collects the
        /// prices.</summary>
        private void Harvest()
        {
            foreach (DataRow r in _dtMachines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                int idx = Convert.ToInt32(r["Idx"]);
                if (idx < 0 || idx >= _items.Count) continue;
                _items[idx].MergeGroupCode = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.Sanitize(
                    Convert.ToString(r["MergeGroup"]));
            }
            Result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (!Priceable) return;   // nothing on this contract is a priced rental line
            foreach (DataRow r in _dtLines.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                decimal price = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (price <= 0m) continue;   // not priced -- the machines keep their own rates
                Result[Convert.ToString(r["GroupKey"])] = price;
            }
        }

        // Grouping machines and then closing the window would lose it silently.
        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            DialogResult r = XtraMessageBox.Show(
                "You changed the lines but did not press OK." + Environment.NewLine + Environment.NewLine +
                "Apply them to the contract?" + Environment.NewLine +
                "(Remember to SAVE the contract afterwards.)",
                "Lines & Rental Price", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)
            {
                GridViewMachines.CloseEditor();
                GridViewMachines.UpdateCurrentRow();
                GridViewLines.CloseEditor();
                GridViewLines.UpdateCurrentRow();
                Harvest();
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
