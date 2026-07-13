using System;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;

namespace ServiceContractPhotocopier.Classes.CommonForms
{
    /// <summary>
    /// Reusable "Advance Search" picker (mirrors AutoCount's advance-search dialog): a full grid of the
    /// data with a built-in Find panel that searches across EVERY column ("search everything"), a
    /// per-column auto-filter row, click-to-sort headers, and a record count. Returns the selected
    /// row's key value. Use via the static <see cref="Pick"/> helper.
    /// </summary>
    public partial class AdvanceSearch_Form : XtraForm
    {
        private readonly string _keyField;

        public object SelectedKey { get; private set; }

        public AdvanceSearch_Form()
        {
            InitializeComponent();
        }

        public AdvanceSearch_Form(string title, DataTable data, string keyField,
            string[] fields, string[] captions, int[] widths) : this()
        {
            _keyField = keyField;
            this.Text = title;
            Grid.DataSource = data;
            View.PopulateColumns();

            // Show only the requested columns, in order, with captions/widths; hide everything else.
            foreach (GridColumn c in View.Columns) c.Visible = false;
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    GridColumn c = View.Columns[fields[i]];
                    if (c == null) continue;
                    c.Visible = true;
                    c.VisibleIndex = i;
                    if (captions != null && i < captions.Length) c.Caption = captions[i];
                    if (widths != null && i < widths.Length) c.Width = widths[i];
                }
            }
            View.BestFitColumns();
            UpdateCount();
            View.DoubleClick += new EventHandler(View_DoubleClick);
            View.KeyDown += new KeyEventHandler(View_KeyDown);
            Grid.KeyDown += new KeyEventHandler(View_KeyDown);
        }

        private void UpdateCount()
        {
            LblCount.Text = View.RowCount + " record(s)";
        }

        private void View_DoubleClick(object sender, EventArgs e) { Accept(); }

        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { Accept(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { this.DialogResult = DialogResult.Cancel; this.Close(); }
        }

        private void BtnOk_Click(object sender, EventArgs e) { Accept(); }
        private void BtnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }

        private void Accept()
        {
            int rh = View.FocusedRowHandle;
            if (rh < 0) { XtraMessageBox.Show("Select a row.", this.Text); return; }
            DataRowView drv = View.GetRow(rh) as DataRowView;
            if (drv == null || !drv.Row.Table.Columns.Contains(_keyField)) return;
            SelectedKey = drv.Row[_keyField];
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Shows the advance-search dialog and returns the selected key value (null if cancelled).</summary>
        public static object Pick(IWin32Window owner, string title, DataTable data, string keyField,
            string[] fields, string[] captions, int[] widths)
        {
            if (data == null || data.Rows.Count == 0)
            {
                XtraMessageBox.Show("No records found.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            using (AdvanceSearch_Form f = new AdvanceSearch_Form(title, data, keyField, fields, captions, widths))
            {
                return f.ShowDialog(owner) == DialogResult.OK ? f.SelectedKey : null;
            }
        }
    }
}
