using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Transfer Machine to DO: pick the machines that are going out, and one delivery order is
    /// made for them.
    ///
    /// It replaced a confirmation box that listed the machines the user had already selected in
    /// the contract grid. Selecting there and confirming here were two different acts of picking,
    /// and the second could only say yes or no to the first -- a machine that could not go out
    /// (no serial, already delivered, out of stock) could only be dropped from the list with a
    /// note, never swapped for another without closing the box and starting again.
    ///
    /// So the picking happens here: every machine of the contract, each with whether it can go and
    /// why not, and a tick box on the ones that can. Nothing is checked for a machine that cannot
    /// go, and nothing can be.
    /// </summary>
    public partial class zSCP2_TransferToDO_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly string _debtorCode;
        private readonly string _debtorName;
        private readonly string _contractNo;
        private readonly string _deptNo;
        private readonly string _projNo;
        private readonly List<Classes.ScpContractDO.Candidate> _machines;
        private DataTable _rows;

        /// <summary>The DO that was made, once one has been. Empty while nothing has been created.</summary>
        public string CreatedDocNo = "";
        public long CreatedDocKey;
        /// <summary>The user answered Yes to "Open it now?" after the DO was made.</summary>
        public bool OpenCreated;

        public zSCP2_TransferToDO_Form(DBSetting db, string debtorCode, string debtorName, string contractNo,
            string deptNo, string projNo, List<Classes.ScpContractDO.Candidate> machines)
        {
            InitializeComponent();
            _db = db;
            _debtorCode = debtorCode ?? "";
            _debtorName = debtorName ?? "";
            _contractNo = contractNo ?? "";
            _deptNo = deptNo ?? "";
            _projNo = projNo ?? "";
            _machines = machines ?? new List<Classes.ScpContractDO.Candidate>();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            LblCustomer.Text = _debtorCode + (_debtorName.Length > 0 ? "  -  " + _debtorName : "");
            DateDoc.EditValue = DateTime.Today;

            _rows = new DataTable();
            _rows.Columns.Add("Sel", typeof(bool));
            _rows.Columns.Add("ServiceItemNo", typeof(string));
            _rows.Columns.Add("ItemCode", typeof(string));
            _rows.Columns.Add("SerialNo", typeof(string));
            _rows.Columns.Add("Location", typeof(string));
            _rows.Columns.Add("Status", typeof(string));
            _rows.Columns.Add("CanGo", typeof(bool));
            _rows.Columns.Add("ItemKey", typeof(long));

            List<Classes.ScpContractDO.Check> checks;
            try { checks = Classes.ScpContractDO.CheckAvailable(_db, _machines); }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, "The stock could not be checked:\r\n\r\n" + ex.Message,
                    "Transfer Machine to DO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            foreach (Classes.ScpContractDO.Check c in checks)
            {
                DataRow r = _rows.NewRow();
                // Ready to go is ticked to begin with: the user opened this to send machines out,
                // and the common case is all of them.
                r["Sel"] = c.Ok;
                r["ServiceItemNo"] = c.Machine.ServiceItemNo ?? "";
                r["ItemCode"] = c.Machine.ItemCode ?? "";
                r["SerialNo"] = c.Machine.SerialNo ?? "";
                r["Location"] = c.Machine.Location ?? "";
                r["Status"] = c.Ok ? "Ready" : c.Reason;
                r["CanGo"] = c.Ok;
                r["ItemKey"] = c.Machine.ItemKey;
                _rows.Rows.Add(r);
            }

            GridMachines.DataSource = _rows;
            GridViewMachines.BestFitColumns();
            ActiveControl = GridMachines;   // the ticks are the job here, not the date
            UpdateCount();
        }

        /// <summary>How many are ticked, and whether there is anything to create.</summary>
        private void UpdateCount()
        {
            int n = 0;
            int ready = 0;
            if (_rows != null)
            {
                foreach (DataRow r in _rows.Rows)
                {
                    if (r["CanGo"] != DBNull.Value && Convert.ToBoolean(r["CanGo"])) ready++;
                    if (r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"])) n++;
                }
            }
            LblCount.Text = n + " machine(s) selected" + (ready < (_rows == null ? 0 : _rows.Rows.Count)
                ? "   (" + ready + " of " + _rows.Rows.Count + " can go out)"
                : "");
            BtnCreate.Enabled = n > 0;
        }

        /// <summary>A machine that cannot go out cannot be ticked -- the Status column says why.</summary>
        private void GridViewMachines_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GridViewMachines.FocusedColumn != ColSel) return;
            object v = GridViewMachines.GetFocusedRowCellValue("CanGo");
            if (v == null || v == DBNull.Value || !Convert.ToBoolean(v)) e.Cancel = true;
        }

        private void GridViewMachines_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column != ColSel) return;
            UpdateCount();
        }

        // A machine that cannot go out is dimmed, and its reason is in the warning colour -- the
        // same amber the contract grid uses for "No DO yet", so the two screens agree.
        private void GridViewMachines_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            object v = GridViewMachines.GetRowCellValue(e.RowHandle, "CanGo");
            if (v != null && v != DBNull.Value && Convert.ToBoolean(v)) return;

            e.Appearance.ForeColor = System.Drawing.Color.FromArgb(150, 80, 0);
            e.Appearance.Options.UseForeColor = true;
            if (e.Column == ColStatus)
            {
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 236, 179);
                e.Appearance.Options.UseBackColor = true;
            }
        }

        private void BtnAll_Click(object sender, EventArgs e)
        {
            SetAll(true);
        }

        private void BtnNone_Click(object sender, EventArgs e)
        {
            SetAll(false);
        }

        private void SetAll(bool on)
        {
            if (_rows == null) return;
            GridViewMachines.CloseEditor();
            foreach (DataRow r in _rows.Rows)
            {
                bool canGo = r["CanGo"] != DBNull.Value && Convert.ToBoolean(r["CanGo"]);
                r["Sel"] = on && canGo;   // "Select all" means all that CAN go
            }
            GridViewMachines.RefreshData();
            UpdateCount();
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            GridViewMachines.CloseEditor();
            GridViewMachines.UpdateCurrentRow();

            List<Classes.ScpContractDO.Candidate> going = new List<Classes.ScpContractDO.Candidate>();
            foreach (DataRow r in _rows.Rows)
            {
                if (r["Sel"] == DBNull.Value || !Convert.ToBoolean(r["Sel"])) continue;
                long itemKey = r["ItemKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ItemKey"]);
                foreach (Classes.ScpContractDO.Candidate c in _machines)
                {
                    if (c.ItemKey != itemKey) continue;
                    going.Add(c);
                    break;
                }
            }
            if (going.Count == 0)
            {
                XtraMessageBox.Show(this, "Tick the machines that are going out.", "Transfer Machine to DO",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime docDate = DateDoc.EditValue == null || DateDoc.EditValue == DBNull.Value
                ? DateTime.Today
                : Convert.ToDateTime(DateDoc.EditValue).Date;

            string docNo;
            long docKey;
            try
            {
                Cursor = Cursors.WaitCursor;
                // The stock is checked again in here, at the moment the DO is written: a serial
                // delivered by someone else while this dialog was open is caught there, not here.
                docNo = Classes.ScpContractDO.CreateDO(_db, _debtorCode, _contractNo, _deptNo, _projNo,
                    docDate, going, out docKey);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                XtraMessageBox.Show(this, "The DO was not made:\r\n\r\n" + ex.Message, "Transfer Machine to DO",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally { Cursor = Cursors.Default; }

            CreatedDocNo = docNo;
            CreatedDocKey = docKey;

            // Offer the document rather than only naming it: the next thing anyone does with a
            // fresh DO is look at it or print it. It is opened by the contract once this dialog
            // has closed, so the machines' DO column already shows it behind the DO screen.
            OpenCreated = XtraMessageBox.Show(this,
                docNo + " created for " + going.Count + " machine(s).\r\n\r\nOpen it now?",
                "Transfer Machine to DO", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes;

            DialogResult = DialogResult.OK;
            Close();
        }

        // A tick counts the moment it is clicked, not when the cell is left: the count and the
        // Create DO button follow the box.
        private void RepoSel_EditValueChanged(object sender, EventArgs e)
        {
            GridViewMachines.PostEditor();
        }
    }
}
