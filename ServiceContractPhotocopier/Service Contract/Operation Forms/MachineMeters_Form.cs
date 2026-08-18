using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Which counters every machine on the contract has — rent, black, colour — set for the whole
    /// fleet at once.
    ///
    /// <para>The machine grid can tick one machine at a time, and for thirty machines that is thirty
    /// clicks and one of them missed. Worse, clicking a tick collapses a multi-row selection to the
    /// row clicked, so "select the fleet, then tick" cannot work in the grid at all. Here the ticking
    /// and the doing are separate: tick the machines, then press what they need.</para>
    ///
    /// <para>Adding gives the standard meter with the price left at 0. Removing takes the meter and,
    /// once saved, its whole reading history — so it says how many machines that is before it does
    /// it. A machine already the way it is asked to be is not touched.</para>
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
            _dt.Columns.Add("HasBK", typeof(bool));
            _dt.Columns.Add("HasCL", typeof(bool));

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
                r["HasRental"] = zSCP2_Item_Form.FindMeterByRole(d.Meters, "RENTAL") != null;
                r["HasBK"] = zSCP2_Item_Form.FindMeterByRole(d.Meters, "BK") != null;
                r["HasCL"] = zSCP2_Item_Form.FindMeterByRole(d.Meters, "CL") != null;
                _dt.Rows.Add(r);
            }
            GridMachines.DataSource = _dt;
            GridViewMachines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewMachines_CellValueChanged);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);
            RefreshSummary();
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

        // The six buttons all land here: one counter, on or off, for every ticked machine.
        private void Apply(string field, bool want)
        {
            List<DataRow> ticked = Ticked();
            if (ticked.Count == 0)
            {
                XtraMessageBox.Show("Tick the machines first.", "Meters",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
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
            LblSummary.Text = changed == 0
                ? "Nothing to change — those machines are already like that."
                : LblSummary.Text;
        }

        private void BtnAddRental_Click(object sender, EventArgs e) { Apply("HasRental", true); }
        private void BtnDelRental_Click(object sender, EventArgs e) { Apply("HasRental", false); }
        private void BtnAddBK_Click(object sender, EventArgs e) { Apply("HasBK", true); }
        private void BtnDelBK_Click(object sender, EventArgs e) { Apply("HasBK", false); }
        private void BtnAddCL_Click(object sender, EventArgs e) { Apply("HasCL", true); }
        private void BtnDelCL_Click(object sender, EventArgs e) { Apply("HasCL", false); }

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
                if (!Set(d, "RENTAL", Convert.ToBoolean(r["HasRental"]))) typeMissing = true;
                if (!Set(d, "BK", Convert.ToBoolean(r["HasBK"]))) typeMissing = true;
                if (!Set(d, "CL", Convert.ToBoolean(r["HasCL"]))) typeMissing = true;
            }
            if (typeMissing)
                XtraMessageBox.Show(
                    "This book is missing one of the standard RENTAL / BK / CL meter types, so those " +
                    "meters are waiting for one." + Environment.NewLine + Environment.NewLine +
                    "Open a machine and pick a meter type on its new meter row.",
                    "Meters", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        private static int LosesHistory(ItemEditData d, string role, bool want)
        {
            if (want) return 0;
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            return (m != null && m.RowState != DataRowState.Added) ? 1 : 0;
        }

        /// <summary>Puts one counter on or off a machine. False = the standard type is missing.</summary>
        private bool Set(ItemEditData d, string role, bool want)
        {
            if (d.Meters == null) d.Meters = zSCP2_Item_Form.CreateMetersTable();
            DataRow m = zSCP2_Item_Form.FindMeterByRole(d.Meters, role);
            if (want == (m != null)) return true;          // already so
            if (want) return zSCP2_Item_Form.AddStandardMeter(_db, d.Meters, role);
            m.Delete();
            return true;
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
