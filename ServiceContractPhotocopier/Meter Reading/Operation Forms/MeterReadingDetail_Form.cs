using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// Per-contract meter detail / override dialog. Opened by double-clicking a contract in the
    /// Meter Reading Integration grid. Shows every meter of the contract with its saved/manual
    /// Current reading beside the freshly-Fetched (API) value, and lets the user tick
    /// "Accept fetched" to override (used to resolve a manual-vs-API conflict). The bound DataTable
    /// is edited in place; the caller reads back the AcceptFetched flags after ShowDialog returns OK.
    /// </summary>
    public partial class MeterReadingDetail_Form : XtraForm
    {
        private DataTable _dt;

        public MeterReadingDetail_Form()
        {
            InitializeComponent();
        }

        public MeterReadingDetail_Form(string contractNo, string customer, DataTable dt) : this()
        {
            _dt = dt;
            this.LblTitle.Text = "Contract  " + contractNo + "      " + customer;
            this.GridDetail.DataSource = _dt;
            ConfigureGrid();
        }

        private void ConfigureGrid()
        {
            // Flat, solid-green header so the white title/hint stay high-contrast (the default
            // PanelControl gradient washed the text out near the top).
            this.PanelTop.LookAndFeel.UseDefaultLookAndFeel = false;
            this.PanelTop.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            this.PanelTop.Appearance.BackColor = Color.FromArgb(27, 94, 32);
            this.PanelTop.Appearance.Options.UseBackColor = true;
            this.LblTitle.Appearance.ForeColor = Color.White;
            this.LblTitle.Appearance.Options.UseForeColor = true;
            this.LblHint.Appearance.ForeColor = Color.FromArgb(220, 237, 220);
            this.LblHint.Appearance.Font = new Font("Segoe UI", 8.5F);
            this.LblHint.Appearance.Options.UseForeColor = true;
            this.LblHint.Appearance.Options.UseFont = true;

            this.GridViewDetail.OptionsBehavior.Editable = true;
            this.GridViewDetail.OptionsView.ColumnAutoWidth = false;
            this.GridViewDetail.OptionsView.ShowGroupPanel = false;
            this.GridViewDetail.RowStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(GridViewDetail_RowStyle);

            if (this.GridViewDetail.Columns["ItemMeterKey"] != null) this.GridViewDetail.Columns["ItemMeterKey"].Visible = false;
            if (this.GridViewDetail.Columns["HasConflict"] != null) this.GridViewDetail.Columns["HasConflict"].Visible = false;

            SetCol("ServiceItemNo", "Service Item No", 120);
            SetCol("SerialNo", "Serial", 100);
            SetCol("MeterType", "Meter Type", 120);
            SetCol("Role", "Meter", 100);
            SetNum("LastReading", "Last Reading", 95, "n0");
            SetNum("CurrentReading", "Manual Reading", 150, "n0");
            SetNum("FetchedReading", "API Reading", 110, "n0");
            SetCol("Source", "Source", 75);

            // Current Reading is editable here so the user can key in readings manually for this
            // contract (typed values are saved as MANUAL on OK).
            GridColumn cur = this.GridViewDetail.Columns["CurrentReading"];
            if (cur != null)
            {
                cur.OptionsColumn.AllowEdit = true; cur.OptionsColumn.ReadOnly = false;
                // Paint the editable column yellow so users see at a glance which one they key into.
                cur.AppearanceCell.BackColor = Color.FromArgb(255, 249, 196);
                cur.AppearanceCell.Options.UseBackColor = true;
            }
            this.LblHint.Text = "Type the Manual Reading (yellow column) for this machine, or tick \'Use API Reading\' to use the API value instead.";

            GridColumn acc = this.GridViewDetail.Columns["AcceptFetched"];
            if (acc != null)
            {
                acc.Caption = "Use API Reading";
                acc.Width = 150;
                acc.ColumnEdit = this.RepoCheck;
                acc.OptionsColumn.AllowEdit = true;
                acc.OptionsColumn.ReadOnly = false;
            }
        }

        private void SetCol(string field, string caption, int width)
        {
            GridColumn c = this.GridViewDetail.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width;
            c.OptionsColumn.AllowEdit = false; c.OptionsColumn.ReadOnly = true;
        }

        private void SetNum(string field, string caption, int width, string fmt)
        {
            SetCol(field, caption, width);
            GridColumn c = this.GridViewDetail.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c.DisplayFormat.FormatString = fmt;
        }

        // Conflict rows (saved manual differs from fetched) are highlighted light red.
        private void GridViewDetail_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return;
            object cf = this.GridViewDetail.GetRowCellValue(e.RowHandle, "HasConflict");
            if (cf != null && cf != DBNull.Value && Convert.ToBoolean(cf))
            {
                e.Appearance.BackColor = Color.FromArgb(255, 224, 224);
                e.Appearance.Options.UseBackColor = true;
            }
        }

        private void BtnAcceptAll_Click(object sender, EventArgs e)
        {
            this.GridViewDetail.CloseEditor();
            if (_dt == null) return;
            foreach (DataRow d in _dt.Rows)
                if (Convert.ToDecimal(d["FetchedReading"] == DBNull.Value ? 0 : d["FetchedReading"]) > 0m)
                    d["AcceptFetched"] = true;
            this.GridDetail.RefreshDataSource();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            this.GridViewDetail.CloseEditor();
            this.GridViewDetail.UpdateCurrentRow();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
