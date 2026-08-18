using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Which counters every machine on the contract has — rent, black, colour — and what each one
    /// charges, for the whole fleet at once.
    ///
    /// <para>The machine grid can tick one machine at a time, and for thirty machines that is thirty
    /// clicks and one of them missed. Worse, clicking a tick collapses a multi-row selection to the
    /// row clicked, so "select the fleet, then tick" cannot work in the grid at all. Here the ticking
    /// and the doing are separate: tick the machines, then press what they need.</para>
    ///
    /// <para>Rates work the same way, and for a reason beyond convenience: a merged line is only
    /// possible when its machines agree on price, so "give these six the same rate" is not a
    /// shortcut, it is how you decide the invoice prints one black line instead of six.</para>
    ///
    /// <para>Removing takes the meter and, once saved, its whole reading history — so it says how
    /// many machines that is before it does it. A machine already the way it is asked to be is not
    /// touched.</para>
    ///
    /// <para>Changes are written back into the contract editor on OK only, and the contract still has
    /// to be saved.</para>
    /// </summary>
    public partial class MachineMeters_Form : XtraForm
    {
        private readonly List<ItemEditData> _items;
        private readonly AutoCount.Data.DBSetting _db;
        private DataTable _dt;
        private bool _changed;

        public MachineMeters_Form()
        {
            InitializeComponent();
        }

        public MachineMeters_Form(AutoCount.Data.DBSetting db, List<ItemEditData> items) : this()
        {
            _db = db;
            _items = items ?? new List<ItemEditData>();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _dt = new DataTable();
            _dt.Columns.Add("Idx", typeof(int));
            _dt.Columns.Add("Sel", typeof(bool));
            _dt.Columns.Add("ServiceItemNo", typeof(string));
            _dt.Columns.Add("SerialNumber", typeof(string));
            _dt.Columns.Add("ItemCode", typeof(string));
            _dt.Columns.Add("HasRental", typeof(bool));
            _dt.Columns.Add("RentalRate", typeof(decimal));
            _dt.Columns.Add("HasBK", typeof(bool));
            _dt.Columns.Add("BKRate", typeof(decimal));
            _dt.Columns.Add("HasCL", typeof(bool));
            _dt.Columns.Add("CLRate", typeof(decimal));

            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d == null || d.IsGroupItem) continue;
                if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
                DataRow r = _dt.NewRow();
                r["Idx"] = i;
                r["Sel"] = true;                 // the usual case is "all of them"
                r["ServiceItemNo"] = string.IsNullOrWhiteSpace(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber ?? "";
                r["ItemCode"] = d.ItemCode ?? "";
                LoadRole(r, d, "RENTAL", "HasRental", "RentalRate");
                LoadRole(r, d, "BK", "HasBK", "BKRate");
                LoadRole(r, d, "CL", "HasCL", "CLRate");
                _dt.Rows.Add(r);
            }
            GridMachines.DataSource = _dt;
            GridViewMachines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMachines_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);
            RefreshSummary();
        }

        private static void LoadRole(DataRow r, ItemEditData d, string role, string hasField, string rateField)
        {
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            r[hasField] = m != null;
            r[rateField] = m == null ? 0m : RateOf(m);
        }

        /// <summary>A meter's price. A flat meter carries it in whichever of the two columns was
        /// filled in, which is why the old book has rentals in both.</summary>
        private static decimal RateOf(DataRow m)
        {
            decimal rate = m.Table.Columns.Contains("ChargesRate") && m["ChargesRate"] != DBNull.Value
                ? Convert.ToDecimal(m["ChargesRate"]) : 0m;
            decimal min = m.Table.Columns.Contains("MinimumCharges") && m["MinimumCharges"] != DBNull.Value
                ? Convert.ToDecimal(m["MinimumCharges"]) : 0m;
            return rate > 0m ? rate : min;
        }

        private void GridViewMachines_CellValueChanged(object sender,
            DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != null && e.Column.FieldName == "Sel") { RefreshSummary(); return; }
            _changed = true;
            RefreshSummary();
        }

        private List<DataRow> Ticked()
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            List<DataRow> list = new List<DataRow>();
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) list.Add(r);
            }
            return list;
        }

        // The six add/remove buttons all land here: one counter, on or off, for every ticked machine.
        private void Apply(string field, bool want)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }
            int changed = 0;
            foreach (DataRow r in ticked)
            {
                bool now = r[field] != DBNull.Value && Convert.ToBoolean(r[field]);
                if (now == want) continue;
                r[field] = want;
                changed++;
            }
            if (changed > 0) _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            if (changed == 0) LblSummary.Text = "Nothing to change — those machines are already like that.";
        }

        private void NeedTicks()
        {
            XtraMessageBox.Show("Tick the machines first.", "Meters",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAddRental_Click(object sender, EventArgs e) { Apply("HasRental", true); }
        private void BtnDelRental_Click(object sender, EventArgs e) { Apply("HasRental", false); }
        private void BtnAddBK_Click(object sender, EventArgs e) { Apply("HasBK", true); }
        private void BtnDelBK_Click(object sender, EventArgs e) { Apply("HasBK", false); }
        private void BtnAddCL_Click(object sender, EventArgs e) { Apply("HasCL", true); }
        private void BtnDelCL_Click(object sender, EventArgs e) { Apply("HasCL", false); }

        // ---- one rate, many machines ----
        // Same model, same deal: type the figure once. This is also what makes a merged line
        // possible -- machines that disagree on price cannot share a printed line.
        private void SetRate(string hasField, string rateField, string word)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0) { NeedTicks(); return; }
            decimal rate = TxtRate.Value;
            if (rate < 0m) return;

            int done = 0, skipped = 0;
            foreach (DataRow r in ticked)
            {
                bool has = r[hasField] != DBNull.Value && Convert.ToBoolean(r[hasField]);
                if (!has) { skipped++; continue; }      // no such counter on this machine
                r[rateField] = rate;
                done++;
            }
            if (done > 0) _changed = true;
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = done + " machine" + (done == 1 ? "" : "s") + " now charge " +
                rate.ToString("n4").TrimEnd('0').TrimEnd('.') + " for " + word +
                (skipped > 0 ? "   ·   " + skipped + " skipped (no " + word + " meter)" : "");
        }

        private void BtnRateRental_Click(object sender, EventArgs e) { SetRate("HasRental", "RentalRate", "rental"); }
        private void BtnRateBK_Click(object sender, EventArgs e) { SetRate("HasBK", "BKRate", "black"); }
        private void BtnRateCL_Click(object sender, EventArgs e) { SetRate("HasCL", "CLRate", "colour"); }

        // ---- selecting ----
        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            bool anyOff = false;
            foreach (DataRow r in _dt.Rows)
                if (r.RowState != DataRowState.Deleted && (r["Sel"] == DBNull.Value || !Convert.ToBoolean(r["Sel"])))
                    anyOff = true;
            foreach (DataRow r in _dt.Rows)
                if (r.RowState != DataRowState.Deleted) r["Sel"] = anyOff;
            GridViewMachines.RefreshData();
            RefreshSummary();
        }

        /// <summary>Tick every machine of the same model as the focused row — the usual way a rate is
        /// decided, since one model is one deal.</summary>
        private void BtnSameModel_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            DataRowView drv = GridViewMachines.GetRow(GridViewMachines.FocusedRowHandle) as DataRowView;
            if (drv == null)
            {
                XtraMessageBox.Show("Click a machine of the model you want first.", "Meters",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string model = Convert.ToString(drv.Row["ItemCode"]).Trim();
            int n = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                bool same = string.Equals(Convert.ToString(r["ItemCode"]).Trim(), model,
                    StringComparison.OrdinalIgnoreCase);
                r["Sel"] = same;
                if (same) n++;
            }
            GridViewMachines.RefreshData();
            RefreshSummary();
            LblSummary.Text = n + " machine" + (n == 1 ? "" : "s") + " of " +
                (model.Length == 0 ? "(no model)" : model) + " ticked.";
        }

        private void RefreshSummary()
        {
            int machines = 0, sel = 0, rent = 0, bk = 0, cl = 0, none = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                machines++;
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) sel++;
                bool hr = r["HasRental"] != DBNull.Value && Convert.ToBoolean(r["HasRental"]);
                bool hb = r["HasBK"] != DBNull.Value && Convert.ToBoolean(r["HasBK"]);
                bool hc = r["HasCL"] != DBNull.Value && Convert.ToBoolean(r["HasCL"]);
                if (hr) rent++;
                if (hb) bk++;
                if (hc) cl++;
                if (!hr && !hb && !hc) none++;
            }
            LblSummary.Text = machines + " machine" + (machines == 1 ? "" : "s") + ", " + sel + " ticked" +
                "   ·   " + rent + " rental, " + bk + " black, " + cl + " colour" +
                (none > 0 ? "   ·   " + none + " with no meter at all" : "");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();
            if (!Harvest()) return;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Applies the grid to the contract's machines. False = the user backed out of the
        /// warning, so nothing at all is applied.</summary>
        private bool Harvest()
        {
            // Count the removals that cost reading history before touching anything, so the warning
            // states a real number and the user can still say no.
            int losing = 0;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                ItemEditData d = _items[Convert.ToInt32(r["Idx"])];
                if (d.ItemKey <= 0) continue;
                losing += LosesHistory(d, "RENTAL", Convert.ToBoolean(r["HasRental"]));
                losing += LosesHistory(d, "BK", Convert.ToBoolean(r["HasBK"]));
                losing += LosesHistory(d, "CL", Convert.ToBoolean(r["HasCL"]));
            }
            if (losing > 0 &&
                XtraMessageBox.Show(
                    "This removes " + losing + " meter" + (losing == 1 ? "" : "s") + "." +
                    Environment.NewLine + Environment.NewLine +
                    "When you save the contract, each one AND its reading history (all readings + " +
                    "billing log for that counter) are permanently deleted." + Environment.NewLine +
                    Environment.NewLine + "Continue?",
                    "Meters", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return false;

            bool typeMissing = false;
            foreach (DataRow r in _dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                ItemEditData d = _items[Convert.ToInt32(r["Idx"])];
                if (!Set(d, "RENTAL", Convert.ToBoolean(r["HasRental"]), Dec(r["RentalRate"]))) typeMissing = true;
                if (!Set(d, "BK", Convert.ToBoolean(r["HasBK"]), Dec(r["BKRate"]))) typeMissing = true;
                if (!Set(d, "CL", Convert.ToBoolean(r["HasCL"]), Dec(r["CLRate"]))) typeMissing = true;
            }
            if (typeMissing)
                XtraMessageBox.Show(
                    "This book is missing one of the standard RENTAL / BK / CL meter types, so those " +
                    "meters are waiting for one." + Environment.NewLine + Environment.NewLine +
                    "Open a machine and pick a meter type on its new meter row.",
                    "Meters", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        private static decimal Dec(object v)
        {
            return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
        }

        private static int LosesHistory(ItemEditData d, string role, bool want)
        {
            if (want) return 0;
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            return (m != null && m.RowState != DataRowState.Added) ? 1 : 0;
        }

        /// <summary>Puts one counter on or off a machine, and prices it. False = the standard type is
        /// missing.</summary>
        private bool Set(ItemEditData d, string role, bool want, decimal rate)
        {
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            bool ok = true;
            if (!want)
            {
                if (m != null) m.Delete();
                return true;
            }
            if (m == null)
            {
                ok = zSCP2_Item_Form.AddStandardMeter(_db, d.Meters, role);
                m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
                if (m == null) return ok;
            }
            // Only write a price the user actually changed. A rental that carries its amount in
            // MinimumCharges keeps it there untouched unless a new figure is given, so an old
            // contract is not quietly re-shaped by opening this screen.
            if (rate != RateOf(m))
            {
                m["ChargesRate"] = rate;
                if (m.Table.Columns.Contains("MinimumCharges")) m["MinimumCharges"] = 0m;
            }
            return ok;
        }

        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            DialogResult r = XtraMessageBox.Show(
                "You changed the meters but did not press OK." + Environment.NewLine + Environment.NewLine +
                "Apply them to the contract?" + Environment.NewLine +
                "(Remember to SAVE the contract afterwards.)",
                "Meters", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)
            {
                GridViewMachines.CloseEditor();
                GridViewMachines.UpdateCurrentRow();
                if (!Harvest()) { e.Cancel = true; return; }
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
