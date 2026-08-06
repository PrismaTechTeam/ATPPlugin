using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Demo 28/07 #2(b): "View Setting" — a plain tick list of the meter grid's optional columns.
    /// The column chooser can do the same job, but only if you already know to right-click the
    /// header; this is the door the operator can find. Only columns the user genuinely owns are
    /// listed — the tab-driven key-in columns and the internal data drivers are not offered,
    /// because the grid re-forces those on every tab switch and a choice here would not stick.
    /// The choice itself persists through the grid's native AutoCount layout (saved per user when
    /// the meter form closes) — this dialog only flips Visible.
    /// </summary>
    public partial class MeterViewSetting_Form : XtraForm
    {
        private readonly List<GridColumn> _cols;

        public MeterViewSetting_Form()
        {
            InitializeComponent();
        }

        public MeterViewSetting_Form(List<GridColumn> cols) : this()
        {
            _cols = cols;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_cols == null) return;
            ListCols.BeginUpdate();
            for (int i = 0; i < _cols.Count; i++)
            {
                GridColumn c = _cols[i];
                string caption = string.IsNullOrEmpty(c.Caption) ? c.FieldName : c.Caption;
                ListCols.Items.Add(new DevExpress.XtraEditors.Controls.CheckedListBoxItem(c, c.Visible));
                ListCols.Items[ListCols.Items.Count - 1].Description = caption;
            }
            ListCols.EndUpdate();
        }

        private void BtnShowAll_Click(object sender, EventArgs e)
        {
            ListCols.CheckAll();
        }

        private void BtnHideAll_Click(object sender, EventArgs e)
        {
            ListCols.UnCheckAll();
        }

        /// <summary>
        /// Called by the caller after an OK result: pushes the ticks onto the grid columns.
        /// </summary>
        public void ApplySelection()
        {
            for (int i = 0; i < ListCols.Items.Count; i++)
            {
                GridColumn c = ListCols.Items[i].Value as GridColumn;
                if (c == null) continue;
                bool want = ListCols.Items[i].CheckState == CheckState.Checked;
                if (c.Visible != want) c.Visible = want;
            }
        }
    }
}
