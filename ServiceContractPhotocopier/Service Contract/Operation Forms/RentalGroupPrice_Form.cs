using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Prices the contract's rental GROUPS, once the format merges rental lines.
    ///
    /// <para>A merged rental line is a deal -- "3 UNIT ... MONTHLY RENTAL (1/36)" -- and a deal has
    /// one price. This is where that price is agreed. Every rental meter in the group then bills at
    /// it, whatever each machine's own meter says, and a machine added later joins at the group's
    /// price instead of arriving with a number of its own.</para>
    ///
    /// <para>Which groups exist follows the format: "merge by model" gives every model its own row,
    /// a merge that ignores model gives one row for the whole contract. Leave a price at 0 and that
    /// group is not priced -- its machines keep their own meter rates, exactly as before.</para>
    ///
    /// <para>Changes are written back into the contract editor on OK only, and the contract still
    /// has to be saved.</para>
    /// </summary>
    public partial class RentalGroupPrice_Form : XtraForm
    {
        private readonly List<ItemEditData> _items;
        private readonly char _rentalMode;
        private readonly Dictionary<string, decimal> _prices;   // ModelCode ('' = all) -> price
        private DataTable _dt;
        private bool _changed;

        /// <summary>The prices as edited, ready to store. Keyed by ModelCode; '' is the whole
        /// contract. A group left at 0 is absent -- there is nothing to store for it.</summary>
        public Dictionary<string, decimal> Result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        public RentalGroupPrice_Form()
        {
            InitializeComponent();
        }

        public RentalGroupPrice_Form(List<ItemEditData> items, char rentalMode,
            Dictionary<string, decimal> current) : this()
        {
            _items = items ?? new List<ItemEditData>();
            _rentalMode = rentalMode;
            _prices = current ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _dt = new DataTable();
            _dt.Columns.Add("GroupKey", typeof(string));    // ModelCode, '' = the whole contract
            _dt.Columns.Add("GroupName", typeof(string));
            _dt.Columns.Add("Units", typeof(int));
            _dt.Columns.Add("OnMachines", typeof(string));  // what the meters say today
            _dt.Columns.Add("UnitPrice", typeof(decimal));
            _dt.Columns.Add("Monthly", typeof(decimal));

            BuildGroups();
            this.GridGroups.DataSource = _dt;
            this.GridViewGroups.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewGroups_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);
            this.LblHint.Text = _rentalMode == ServiceContractPhotocopier.Classes.ScpBillingFormat.LINE_SAME_MODEL
                ? "This contract merges its rental lines BY MODEL, so every model is one line with one price. " +
                  "Type the monthly rental of ONE machine - the line prints units x that price. Leave a group at 0 " +
                  "and its machines keep their own meter rates."
                : "This contract merges all its rental into ONE line, so there is one price. Type the monthly " +
                  "rental of ONE machine - the line prints units x that price. Leave it at 0 and the machines " +
                  "keep their own meter rates.";
            RefreshSummary();
        }

        // The groups the fold engine will actually produce, counted off the contract's real machines.
        private void BuildGroups()
        {
            List<string> order = new List<string>();
            Dictionary<string, int> units = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<decimal>> rates = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d == null || d.IsGroupItem) continue;
                decimal rate;
                if (!RentalRateOf(d, out rate)) continue;   // no rental meter = not in a rental group
                string key = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.GroupKeyFor(_rentalMode, d.ItemCode);
                if (!units.ContainsKey(key)) { order.Add(key); units[key] = 0; rates[key] = new List<decimal>(); }
                units[key] = units[key] + 1;
                rates[key].Add(rate);
            }

            order.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string key in order)
            {
                DataRow r = _dt.NewRow();
                r["GroupKey"] = key;
                r["GroupName"] = key.Length == 0 ? "All machines" : key;
                r["Units"] = units[key];
                r["OnMachines"] = DescribeRates(rates[key]);
                decimal price;
                r["UnitPrice"] = _prices.TryGetValue(key, out price) ? price : 0m;
                r["Monthly"] = Convert.ToDecimal(r["UnitPrice"]) * units[key];
                _dt.Rows.Add(r);
            }
        }

        /// <summary>This machine's monthly rental as its own meter states it. False when the machine
        /// has no rental meter at all -- it is not part of any rental line.</summary>
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
        // the case a group price is for.
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

        private void GridViewGroups_CellValueChanged(object sender,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "UnitPrice") return;
            decimal price = e.Value == null || e.Value == DBNull.Value ? 0m : Convert.ToDecimal(e.Value);
            if (price < 0m) { price = 0m; GridViewGroups.SetRowCellValue(e.RowHandle, "UnitPrice", 0m); }
            object u = GridViewGroups.GetRowCellValue(e.RowHandle, "Units");
            int units = u == null || u == DBNull.Value ? 0 : Convert.ToInt32(u);
            GridViewGroups.SetRowCellValue(e.RowHandle, "Monthly", price * units);
            _changed = true;
            RefreshSummary();
        }

        // What the contract will bill in rental a month, at the prices on screen.
        private void RefreshSummary()
        {
            int priced = 0, machines = 0;
            decimal money = 0m;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                int units = r["Units"] == DBNull.Value ? 0 : Convert.ToInt32(r["Units"]);
                decimal price = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                machines += units;
                if (price <= 0m) continue;
                priced++;
                money += price * units;
            }
            int groups = _dt.Rows.Count;
            LblSummary.Text = groups == 0
                ? "No machine on this contract has a rental meter yet."
                : groups + " group" + (groups == 1 ? "" : "s") + " - " + machines + " machine" +
                  (machines == 1 ? "" : "s") + " - " + priced + " priced here" +
                  (priced > 0 ? " - " + money.ToString("n2") + " a month" : "") +
                  (priced < groups ? "   (the rest keep their own meter rates)" : "");
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            GridViewGroups.CloseEditor();
            GridViewGroups.UpdateCurrentRow();
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                r["UnitPrice"] = 0m;
                r["Monthly"] = 0m;
            }
            _changed = true;
            GridViewGroups.RefreshData();
            RefreshSummary();
        }

        // Seed every group from what its machines already charge, so a contract that is only being
        // moved onto the new rules starts from its real numbers instead of a blank column. Groups
        // whose machines disagree are left alone -- there is no honest number to fill in.
        private void BtnFromMachines_Click(object sender, EventArgs e)
        {
            GridViewGroups.CloseEditor();
            GridViewGroups.UpdateCurrentRow();
            int filled = 0, skipped = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string shown = Convert.ToString(r["OnMachines"]);
                if (shown.IndexOf("mixed", StringComparison.OrdinalIgnoreCase) >= 0) { skipped++; continue; }
                decimal v;
                if (!decimal.TryParse(shown, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture, out v) || v <= 0m) { skipped++; continue; }
                r["UnitPrice"] = v;
                r["Monthly"] = v * (r["Units"] == DBNull.Value ? 0 : Convert.ToInt32(r["Units"]));
                filled++;
            }
            _changed = true;
            GridViewGroups.RefreshData();
            RefreshSummary();
            if (skipped > 0)
                XtraMessageBox.Show(filled + " group(s) filled in. " + skipped + " left blank because their " +
                    "machines charge different amounts - type the price the group has agreed.",
                    "Take from machines", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewGroups.CloseEditor();
            GridViewGroups.UpdateCurrentRow();
            Harvest();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Harvest()
        {
            Result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                decimal price = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                if (price <= 0m) continue;   // not priced -- nothing to store
                Result[Convert.ToString(r["GroupKey"])] = price;
            }
        }

        // Typing prices and then closing the window would lose them silently.
        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            DialogResult r = XtraMessageBox.Show(
                "You changed the rental prices but did not press OK.\r\n\r\n" +
                "Apply them to the contract?\r\n" +
                "(Remember to SAVE the contract afterwards.)",
                "Rental price", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)
            {
                GridViewGroups.CloseEditor();
                GridViewGroups.UpdateCurrentRow();
                Harvest();
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
