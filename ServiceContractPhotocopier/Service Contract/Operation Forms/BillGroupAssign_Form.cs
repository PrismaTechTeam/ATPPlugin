using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// #6 Bill Group bulk assign: tick machines, give them a group name, and Generate bills each
    /// group as its own invoice. Friendlier than typing the code row by row on a 16-machine
    /// contract. Changes are written back into the contract editor's ItemEditData list on OK only.
    /// </summary>
    public partial class BillGroupAssign_Form : XtraForm
    {
        private readonly List<ItemEditData> _items;
        private DataTable _dt;
        private bool _changed;   // Assign/Clear happened — guard against closing without OK

        public BillGroupAssign_Form()
        {
            InitializeComponent();
        }

        public BillGroupAssign_Form(List<ItemEditData> items) : this()
        {
            _items = items;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _dt = new DataTable();
            _dt.Columns.Add("Idx", typeof(int));
            _dt.Columns.Add("Sel", typeof(bool));
            _dt.Columns.Add("ServiceItemNo", typeof(string));
            _dt.Columns.Add("SerialNumber", typeof(string));
            _dt.Columns.Add("ItemCode", typeof(string));
            _dt.Columns.Add("Description", typeof(string));
            _dt.Columns.Add("BillGroup", typeof(string));
            for (int i = 0; i < _items.Count; i++)
            {
                ItemEditData d = _items[i];
                if (d.IsGroupItem) continue;   // the fleet "group machine" never joins a bill group
                DataRow r = _dt.NewRow();
                r["Idx"] = i;
                r["Sel"] = false;
                r["ServiceItemNo"] = string.IsNullOrWhiteSpace(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                r["SerialNumber"] = d.SerialNumber ?? "";
                r["ItemCode"] = d.ItemCode ?? "";
                r["Description"] = d.Description ?? "";
                r["BillGroup"] = d.BillGroupCode ?? "";
                _dt.Rows.Add(r);
            }
            this.GridMachines.DataSource = _dt;
            this.GridViewMachines.DoubleClick += new EventHandler(GridViewMachines_DoubleClick);
            this.FormClosing += new FormClosingEventHandler(OnFormClosingGuard);
            RefreshGroupCombo();
            RefreshSummary();
        }

        // Assign changes only the list INSIDE this dialog; OK applies them to the contract. A user
        // who presses Assign and then closes the window would silently lose the grouping — catch it.
        private void OnFormClosingGuard(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK || !_changed) return;
            DialogResult r = XtraMessageBox.Show(
                "You assigned groups but did not press OK.\r\n\r\n" +
                "Apply the Bill Group changes to the contract?\r\n" +
                "(Remember to SAVE the contract afterwards.)",
                "Bill Group", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) { e.Cancel = true; return; }
            if (r == DialogResult.Yes)
            {
                ApplyToItems();
                this.DialogResult = DialogResult.OK;
            }
        }

        /// <summary>Writes the dialog's grouping back into the contract editor's item list.</summary>
        private void ApplyToItems()
        {
            this.GridViewMachines.PostEditor();
            foreach (DataRow r in _dt.Rows)
            {
                int idx = Convert.ToInt32(r["Idx"]);
                if (idx >= 0 && idx < _items.Count)
                    _items[idx].BillGroupCode = Convert.ToString(r["BillGroup"]).Trim();
            }
        }

        private void RefreshGroupCombo()
        {
            string keep = this.CmbGroup.Text;
            this.CmbGroup.Properties.Items.Clear();
            SortedSet<string> codes = new SortedSet<string>();
            foreach (DataRow r in _dt.Rows)
            {
                string g = Convert.ToString(r["BillGroup"]).Trim();
                if (g.Length > 0) codes.Add(g);
            }
            foreach (string c in codes) this.CmbGroup.Properties.Items.Add(c);
            this.CmbGroup.Text = keep;
        }

        /// <summary>Plain-words preview of what Generate will do with the current grouping.</summary>
        private void RefreshSummary()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            List<string> order = new List<string>();
            int ungrouped = 0;
            foreach (DataRow r in _dt.Rows)
            {
                string g = Convert.ToString(r["BillGroup"]).Trim();
                if (g.Length == 0) { ungrouped++; continue; }
                if (!counts.ContainsKey(g)) { counts[g] = 0; order.Add(g); }
                counts[g] = counts[g] + 1;
            }
            string text;
            if (counts.Count == 0)
            {
                text = "No groups - all " + _dt.Rows.Count + " machine(s) bill the normal way.";
            }
            else
            {
                order.Sort();
                List<string> parts = new List<string>();
                foreach (string g in order)
                    parts.Add(g + " (" + counts[g] + " machine" + (counts[g] == 1 ? "" : "s") + ")");
                int invoices = counts.Count + (ungrouped > 0 ? 1 : 0);
                text = "Generate splits this contract into " + invoices + " invoice(s): " + string.Join(", ", parts.ToArray());
                if (ungrouped > 0) text += " + " + ungrouped + " ungrouped machine(s) billed the normal way";
            }
            this.LblSummary.Text = text;
        }

        private void GridViewMachines_DoubleClick(object sender, EventArgs e)
        {
            int rh = this.GridViewMachines.FocusedRowHandle;
            if (rh < 0) return;
            object v = this.GridViewMachines.GetRowCellValue(rh, "Sel");
            bool cur = v != null && v != DBNull.Value && Convert.ToBoolean(v);
            this.GridViewMachines.SetRowCellValue(rh, "Sel", !cur);
        }

        private List<DataRow> TickedRows()
        {
            this.GridViewMachines.PostEditor();
            this.GridViewMachines.UpdateCurrentRow();
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow r in _dt.Rows)
                if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) rows.Add(r);
            return rows;
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            this.GridViewMachines.PostEditor();
            foreach (DataRow r in _dt.Rows) r["Sel"] = true;
        }

        private void BtnAssign_Click(object sender, EventArgs e)
        {
            string code = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(this.CmbGroup.Text);
            if (code.Length == 0)
            {
                XtraMessageBox.Show("Type a group name first (e.g. G1, FINANCE, LEVEL2) - or use \"Clear Ticked\" to ungroup.",
                    "Bill Group", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            List<DataRow> rows = TickedRows();
            if (rows.Count == 0)
            {
                XtraMessageBox.Show("Tick the machines first, then press \"Assign to Ticked\".",
                    "Bill Group", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (DataRow r in rows) { r["BillGroup"] = code; r["Sel"] = false; }
            _changed = true;
            this.CmbGroup.Text = "";
            RefreshGroupCombo();
            RefreshSummary();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            List<DataRow> rows = TickedRows();
            if (rows.Count == 0)
            {
                XtraMessageBox.Show("Tick the machines to remove from their group first.",
                    "Bill Group", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (DataRow r in rows) { r["BillGroup"] = ""; r["Sel"] = false; }
            _changed = true;
            RefreshGroupCombo();
            RefreshSummary();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // The other half of the trap: ticked machines + a typed group name, but Assign was never
            // pressed. OK almost certainly means "assign them" — do it rather than silently ignore.
            string pending = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(this.CmbGroup.Text);
            if (pending.Length > 0)
            {
                List<DataRow> rows = TickedRows();
                if (rows.Count > 0)
                {
                    foreach (DataRow r in rows) { r["BillGroup"] = pending; r["Sel"] = false; }
                    _changed = true;
                    RefreshSummary();
                }
            }
            ApplyToItems();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
