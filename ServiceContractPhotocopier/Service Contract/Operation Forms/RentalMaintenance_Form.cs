using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// Rental Maintenance — the dedicated home for RENTAL (flat-charge) meters. The DATA stays where
    /// it always was (zSCP2_ItemMeter rows whose meter type IsFlatCharge='Y'); this module only
    /// changes the maintenance method: one grid of every rental across all machines, inline edit of
    /// the amount / free months / start / total months / basis, and an "Assign Rental" flow to put a
    /// rental on a machine without opening its meter config. Every edit is written to the contract
    /// change audit (source RENTAL).
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Rental Maintenance",
    MenuOrder = 220, ShowAsDialog = false,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_RENTAL_MAINT,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_RENTAL_MAINT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class RentalMaintenance_Form : XtraForm
    {
        private DBSetting _db;
        private DataTable _dt;
        // Original values per ItemMeterKey for the audit diff (field -> value).
        private readonly Dictionary<long, Dictionary<string, string>> _orig = new Dictionary<long, Dictionary<string, string>>();
        private static readonly string[] EditCols = new string[]
        { "MonthlyAmount", "MinimumCharges", "FreeMonthsLeft", "RentalStartDate", "RentalMonths", "RentalBasis", "Description" };

        public RentalMaintenance_Form() { InitializeComponent(); }
        public RentalMaintenance_Form(UserSession userSession) : this()
        { if (userSession != null) _db = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public RentalMaintenance_Form(DBSetting dbSetting) : this()
        { _db = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_db == null) return;
            ApplyButtonIcons();
            ConfigureGrid();
            LoadGrid();
        }

        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                BtnAssign.ImageOptions.Image = img.GetLargeImage_New();
                BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                try
                {
                    System.Drawing.Image door = DevExpress.Images.ImageResourceCache.Default.GetImage("images/xaf/action_exit_32x32.png");
                    BtnExit.ImageOptions.Image = door ?? img.GetLargeImage_Close();
                }
                catch { BtnExit.ImageOptions.Image = img.GetLargeImage_Close(); }
            }
            catch { }
        }

        private void ConfigureGrid()
        {
            GridViewRent.OptionsBehavior.Editable = true;
            GridViewRent.OptionsView.ShowGroupPanel = false;
            GridViewRent.OptionsView.ShowAutoFilterRow = true;
            GridViewRent.OptionsView.ColumnAutoWidth = false;

            RepoBasis.Items.Clear();
            RepoBasis.Items.AddRange(new object[] { "A", "P" });
            RepoBasis.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            RepoStartDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            RepoStartDate.DisplayFormat.FormatString = "dd/MM/yyyy";
            RepoStartDate.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            RepoStartDate.EditFormat.FormatString = "dd/MM/yyyy";
            RepoStartDate.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;

            GridViewRent.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewRent_CellValueChanged);
            GridViewRent.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewRent_RowCellStyle);
        }

        private void LoadGrid()
        {
            try
            {
                _dt = _db.GetDataTable(
                    "SELECT m.ItemMeterKey, ISNULL(c.ContractKey,0) AS ContractKey, ISNULL(c.ContractNo,'') AS ContractNo, " +
                    "ISNULL(c.DebtorCode,'') AS DebtorCode, ISNULL(d.CompanyName,'') AS Customer, " +
                    "ISNULL(c.StrategyCode,'') AS StrategyCode, ISNULL(c.RentalSeparateInvoice,'N') AS RentSep, " +
                    "i.ItemKey, i.ServiceItemNo, ISNULL(i.SerialNumber,'') AS SerialNumber, " +
                    "m.MeterTypeCode, ISNULL(m.[Description],'') AS [Description], " +
                    "ISNULL(m.ChargesRate,0) AS MonthlyAmount, ISNULL(m.MinimumCharges,0) AS MinimumCharges, " +
                    "ISNULL(m.FOCQty,0) AS FreeMonthsLeft, m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, " +
                    "ISNULL(m.RentalBasis,'A') AS RentalBasis, " +
                    "ISNULL(li.LastInvNo,'') AS LastInvNo, li.LastInvAt " +
                    "FROM dbo.zSCP2_ItemMeter m " +
                    "JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode AND ISNULL(mt.IsFlatCharge,'N') = 'Y' " +
                    "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "LEFT JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN (SELECT z2.ItemMeterKey, z2.InvoicedDocNo AS LastInvNo, z2.InvoicedAt AS LastInvAt " +
                    "  FROM (SELECT me.ItemMeterKey, me.InvoicedDocNo, me.InvoicedAt, " +
                    "        ROW_NUMBER() OVER (PARTITION BY me.ItemMeterKey ORDER BY me.InvoicedAt DESC) AS rn " +
                    "        FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocNo IS NOT NULL AND me.InvoicedDocNo <> '') z2 " +
                    "  WHERE z2.rn = 1) li ON li.ItemMeterKey = m.ItemMeterKey " +
                    "ORDER BY c.DebtorCode, c.ContractNo, i.ServiceItemNo, m.MeterTypeCode", false);

                // Current period n/N (computed display column; red when overdue via RowCellStyle).
                _dt.Columns.Add("PeriodNow", typeof(string));
                DateTime today = DateTime.Today;
                foreach (DataRow r in _dt.Rows)
                {
                    if (r["RentalStartDate"] != DBNull.Value && Convert.ToInt32(r["RentalMonths"]) > 0)
                    {
                        char basis = (Convert.ToString(r["RentalBasis"]) == "P") ? 'P' : 'A';
                        int n = ScpStrategy.RentalPeriodN(Convert.ToDateTime(r["RentalStartDate"]), basis, today.Year, today.Month);
                        r["PeriodNow"] = n + "/" + Convert.ToInt32(r["RentalMonths"]);
                    }
                    else r["PeriodNow"] = "";
                }
                _dt.AcceptChanges();
                SnapshotOriginals();
                GridRent.DataSource = _dt;
                ConfigureColumns();
                LblCount.Text = _dt.Rows.Count + " rental meter(s)";
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void SnapshotOriginals()
        {
            _orig.Clear();
            foreach (DataRow r in _dt.Rows)
            {
                Dictionary<string, string> vals = new Dictionary<string, string>();
                foreach (string c in EditCols) vals[c] = ScpContractAudit.Normalise(r[c]);
                _orig[Convert.ToInt64(r["ItemMeterKey"])] = vals;
            }
        }

        private void ConfigureColumns()
        {
            GridViewRent.PopulateColumns();
            foreach (string hide in new string[] { "ItemMeterKey", "ContractKey", "ItemKey", "RentSep" })
                if (GridViewRent.Columns[hide] != null) GridViewRent.Columns[hide].Visible = false;
            // read-only identity columns
            foreach (string ro in new string[] { "ContractNo", "DebtorCode", "Customer", "StrategyCode",
                "ServiceItemNo", "SerialNumber", "MeterTypeCode", "PeriodNow", "LastInvNo", "LastInvAt" })
                if (GridViewRent.Columns[ro] != null) GridViewRent.Columns[ro].OptionsColumn.AllowEdit = false;
            Cap("ContractNo", "Contract", 100, 0);
            Cap("DebtorCode", "Debtor", 90, 1);
            Cap("Customer", "Customer", 200, 2);
            Cap("ServiceItemNo", "Service Item No", 120, 3);
            Cap("SerialNumber", "Serial", 100, 4);
            Cap("MeterTypeCode", "Rental Type", 140, 5);
            Cap("Description", "Description", 200, 6);
            Cap("MonthlyAmount", "Monthly Amount", 100, 7);
            Cap("MinimumCharges", "Min Charges", 90, 8);
            Cap("FreeMonthsLeft", "Free Months Left", 100, 9);
            Cap("RentalStartDate", "Rental Start", 95, 10);
            Cap("RentalMonths", "Months (N)", 80, 11);
            Cap("RentalBasis", "Basis (A/P)", 80, 12);
            Cap("PeriodNow", "Period Now", 80, 13);
            Cap("StrategyCode", "Strategy", 90, 14);
            Cap("LastInvNo", "Last Invoice", 110, 15);
            Cap("LastInvAt", "Last Inv At", 110, 16);
            if (GridViewRent.Columns["MonthlyAmount"] != null) GridViewRent.Columns["MonthlyAmount"].ColumnEdit = null;
            if (GridViewRent.Columns["RentalStartDate"] != null) GridViewRent.Columns["RentalStartDate"].ColumnEdit = RepoStartDate;
            if (GridViewRent.Columns["RentalBasis"] != null) GridViewRent.Columns["RentalBasis"].ColumnEdit = RepoBasis;
            foreach (string n2 in new string[] { "MonthlyAmount", "MinimumCharges" })
                if (GridViewRent.Columns[n2] != null)
                {
                    GridViewRent.Columns[n2].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    GridViewRent.Columns[n2].DisplayFormat.FormatString = "n2";
                }
            if (GridViewRent.Columns["LastInvAt"] != null)
            {
                GridViewRent.Columns["LastInvAt"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                GridViewRent.Columns["LastInvAt"].DisplayFormat.FormatString = "dd/MM/yyyy";
            }
        }

        private void Cap(string field, string caption, int width, int idx)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewRent.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = idx;
        }

        private void GridViewRent_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            LblCount.Text = "unsaved changes — press Save";
        }

        // Overdue rentals (period n > N) paint the Period Now cell red.
        private void GridViewRent_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "PeriodNow") return;
            string v = Convert.ToString(GridViewRent.GetRowCellValue(e.RowHandle, "PeriodNow"));
            int slash = v.IndexOf('/');
            if (slash <= 0) return;
            int n, total;
            if (int.TryParse(v.Substring(0, slash), out n) && int.TryParse(v.Substring(slash + 1), out total) && n > total)
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(198, 40, 40);
                e.Appearance.Options.UseForeColor = true;
            }
        }

        // Save: per dirty row an in-place UPDATE on zSCP2_ItemMeter (NEVER delete/reinsert — the row
        // is the CASCADE parent of the machine's whole reading history) + one audit row per changed
        // field (source RENTAL) in the same transaction.
        private void OnSave(object sender, EventArgs e)
        {
            GridViewRent.PostEditor();
            GridViewRent.CloseEditor();
            if (_dt == null) return;
            int rows = 0, fields = 0;
            try
            {
                using (SqlConnection cn = new SqlConnection(_db.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("RentalMaint"))
                    {
                        try
                        {
                            foreach (DataRow r in _dt.Rows)
                            {
                                if (r.RowState != DataRowState.Modified) continue;
                                long mk = Convert.ToInt64(r["ItemMeterKey"]);
                                Dictionary<string, string> before;
                                if (!_orig.TryGetValue(mk, out before)) before = new Dictionary<string, string>();

                                using (SqlCommand up = new SqlCommand(
                                    "UPDATE dbo.zSCP2_ItemMeter SET ChargesRate=@rate, MinimumCharges=@min, FOCQty=@foc, " +
                                    "RentalStartDate=@rsd, RentalMonths=@rm, RentalBasis=@rb, [Description]=@desc, " +
                                    "LastModified=GETDATE() WHERE ItemMeterKey=@mk", cn, tx))
                                {
                                    up.Parameters.AddWithValue("@rate", Dec(r["MonthlyAmount"]));
                                    up.Parameters.AddWithValue("@min", Dec(r["MinimumCharges"]));
                                    up.Parameters.AddWithValue("@foc", Dec(r["FreeMonthsLeft"]));
                                    up.Parameters.AddWithValue("@rsd", r["RentalStartDate"] == DBNull.Value ? (object)DBNull.Value : r["RentalStartDate"]);
                                    up.Parameters.AddWithValue("@rm", r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]));
                                    up.Parameters.AddWithValue("@rb", Convert.ToString(r["RentalBasis"]) == "P" ? "P" : "A");
                                    up.Parameters.AddWithValue("@desc", Convert.ToString(r["Description"]) ?? "");
                                    up.Parameters.AddWithValue("@mk", mk);
                                    up.ExecuteNonQuery();
                                }

                                Guid changeSet = Guid.NewGuid();
                                long ck = Convert.ToInt64(r["ContractKey"]);
                                string cno = Convert.ToString(r["ContractNo"]);
                                long ik = Convert.ToInt64(r["ItemKey"]);
                                foreach (string col in EditCols)
                                {
                                    string oldV;
                                    before.TryGetValue(col, out oldV);
                                    string newV = ScpContractAudit.Normalise(r[col]);
                                    if ((oldV ?? "") == (newV ?? "")) continue;
                                    ScpContractAudit.WriteRow(cn, tx, ck, cno, ik, mk, changeSet,
                                        ScpContractAudit.SOURCE_RENTAL, "RENTAL." + col, oldV, newV);
                                    fields++;
                                }
                                rows++;
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); return; }
            LoadGrid();
            XtraMessageBox.Show(rows + " rental meter(s) saved (" + fields + " field change(s) audited).", "Saved",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnAssign(object sender, EventArgs e)
        {
            using (RentalAssign_Form f = new RentalAssign_Form(_db))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private static decimal Dec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }
    }
}
