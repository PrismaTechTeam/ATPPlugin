using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.MeterReading.Services;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    [AutoCount.PlugIn.MenuItem("Meter Reading Integration", MenuOrder = 450)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, false)]
    public partial class MeterReadingIntegration_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private UserSession _userSession;
        private DataTable _dtGrid;

        public MeterReadingIntegration_Form()
        {
            InitializeComponent();
            InitDefaults();
        }

        public MeterReadingIntegration_Form(UserSession userSession) : this()
        {
            _userSession = userSession;
            if (userSession != null) _dbSetting = userSession.DBSetting;
            LoadData();
        }

        public MeterReadingIntegration_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            LoadData();
        }

        private void InitDefaults()
        {
            this.LblToday.Text = DateTime.Today.ToString("dd/MM/yyyy (ddd)");
            this.ChkShowAll.Checked = true;

            this.CmbMonth.Properties.Items.Clear();
            for (int m = 1; m <= 12; m++)
                this.CmbMonth.Properties.Items.Add(new CultureInfo("en-US").DateTimeFormat.GetMonthName(m));
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            this.CmbDay.Properties.Items.Clear();
            for (int d = 1; d <= 31; d++) this.CmbDay.Properties.Items.Add(d);
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;

            this.GridViewMeter.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.GridViewMeter_CellValueChanged);
        }

        private int SelectedMonth()
        {
            int idx = this.CmbMonth.SelectedIndex;
            return (idx >= 0 && idx <= 11) ? idx + 1 : DateTime.Today.Month;
        }

        private int SelectedDay()
        {
            int idx = this.CmbDay.SelectedIndex;
            return (idx >= 0 && idx <= 30) ? idx + 1 : DateTime.Today.Day;
        }

        // ───────────────────── Load listing ─────────────────────

        private void LoadData()
        {
            if (_dbSetting == null) { _dtGrid = NewGridTable(); GridMeter.DataSource = _dtGrid; return; }

            try
            {
                int month = SelectedMonth();
                int year = DateTime.Today.Year;
                int target = SelectedDay();
                int monthEnd = DateTime.DaysInMonth(year, month);
                if (target > monthEnd) target = monthEnd;

                string dayFilter = this.ChkShowAll.Checked
                    ? ""
                    : " AND (CASE WHEN COALESCE(i.BillingDayOverride,c.BillingDay) > " + monthEnd +
                      " THEN " + monthEnd + " ELSE COALESCE(i.BillingDayOverride,c.BillingDay) END) = " + target + " ";

                string sql =
                    "SELECT i.ItemKey, i.ServiceItemNo AS ItemNo, i.SerialNumber, i.Description AS MachineName, " +
                    "c.ContractKey, c.ContractNo, c.DebtorCode, ISNULL(d.CompanyName,'') AS DebtorName, c.BillingMode, " +
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS EffDay, " +
                    "bk.ItemMeterKey AS BKKey, ISNULL(bk.MeterTypeCode,'') AS BKType, ISNULL(bkmt.ACItemCode,'') AS BKAC, " +
                    "ISNULL(bk.ChargesRate,0) AS BKRate, ISNULL(bk.MinimumCharges,0) AS BKMin, ISNULL(bk.FOCQty,0) AS BKFoc, " +
                    "ISNULL(bk.RebateQtyInPercent,0) AS BKRebate, ISNULL(bk.InitialReading,0) AS BKInit, " +
                    "bkl.LastReading AS BKLast, bkl.LastDate AS BKLastDate, " +
                    "cl.ItemMeterKey AS CLKey, ISNULL(cl.MeterTypeCode,'') AS CLType, ISNULL(clmt.ACItemCode,'') AS CLAC, " +
                    "ISNULL(cl.ChargesRate,0) AS CLRate, ISNULL(cl.MinimumCharges,0) AS CLMin, ISNULL(cl.FOCQty,0) AS CLFoc, " +
                    "ISNULL(cl.RebateQtyInPercent,0) AS CLRebate, ISNULL(cl.InitialReading,0) AS CLInit, " +
                    "cll.LastReading AS CLLast, cll.LastDate AS CLLastDate " +
                    "FROM dbo.zSCP2_Item i " +
                    "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN dbo.zSCP2_ItemMeter bk ON bk.ItemKey = i.ItemKey AND bk.MeterRole = 'BK' " +
                    "LEFT JOIN dbo.zSCP_MeterType bkmt ON bkmt.MeterTypeCode = bk.MeterTypeCode " +
                    "OUTER APPLY (SELECT TOP 1 t.MeterTransReading AS LastReading, t.MeterTransDate AS LastDate " +
                    "  FROM dbo.zSCP_MeterTrans t WHERE t.ServiceItemMeterTypeKey = bk.ItemMeterKey " +
                    "  ORDER BY t.MeterTransDate DESC, t.MeterTransKey DESC) bkl " +
                    "LEFT JOIN dbo.zSCP2_ItemMeter cl ON cl.ItemKey = i.ItemKey AND cl.MeterRole = 'CL' " +
                    "LEFT JOIN dbo.zSCP_MeterType clmt ON clmt.MeterTypeCode = cl.MeterTypeCode " +
                    "OUTER APPLY (SELECT TOP 1 t.MeterTransReading AS LastReading, t.MeterTransDate AS LastDate " +
                    "  FROM dbo.zSCP_MeterTrans t WHERE t.ServiceItemMeterTypeKey = cl.ItemMeterKey " +
                    "  ORDER BY t.MeterTransDate DESC, t.MeterTransKey DESC) cll " +
                    "WHERE i.Inactive='N' AND c.Inactive='N' " + dayFilter +
                    "ORDER BY c.ContractNo, i.Pos, i.ItemNo";

                DataTable src = _dbSetting.GetDataTable(sql, false);

                string search = (this.TxtSearch.EditValue ?? "").ToString().Trim().ToLowerInvariant();

                _dtGrid = NewGridTable();
                foreach (DataRow r in src.Rows)
                {
                    if (search.Length > 0)
                    {
                        string hay = (S(r["ItemNo"]) + " " + S(r["SerialNumber"]) + " " + S(r["MachineName"]) +
                                      " " + S(r["ContractNo"]) + " " + S(r["DebtorName"])).ToLowerInvariant();
                        if (!hay.Contains(search)) continue;
                    }

                    DataRow g = _dtGrid.NewRow();
                    g["Sel"] = false;
                    g["ItemKey"] = Convert.ToInt64(r["ItemKey"]);
                    g["ContractKey"] = Convert.ToInt64(r["ContractKey"]);
                    g["ContractNo"] = S(r["ContractNo"]);
                    g["ItemNo"] = S(r["ItemNo"]);
                    g["SerialNo"] = S(r["SerialNumber"]);
                    g["MachineName"] = S(r["MachineName"]);
                    g["DebtorCode"] = S(r["DebtorCode"]);
                    g["Customer"] = S(r["DebtorCode"]) + " - " + S(r["DebtorName"]);
                    g["BillingMode"] = S(r["BillingMode"]);
                    g["Mode"] = S(r["BillingMode"]) == "S" ? "Separate" : "Group";

                    g["BKKey"] = D64(r["BKKey"]);
                    g["BKType"] = S(r["BKType"]);
                    g["BKAC"] = S(r["BKAC"]);
                    g["BKRate"] = Dec(r["BKRate"]);
                    g["BKMin"] = Dec(r["BKMin"]);
                    g["BKFoc"] = Dec(r["BKFoc"]);
                    g["BKRebate"] = Dec(r["BKRebate"]);
                    g["BKLast"] = (r["BKLast"] == DBNull.Value) ? Dec(r["BKInit"]) : Dec(r["BKLast"]);
                    g["BKLastDate"] = r["BKLastDate"];
                    g["BKCurrent"] = 0m; g["BKUsage"] = 0m; g["BKCharge"] = 0m;

                    g["CLKey"] = D64(r["CLKey"]);
                    g["CLType"] = S(r["CLType"]);
                    g["CLAC"] = S(r["CLAC"]);
                    g["CLRate"] = Dec(r["CLRate"]);
                    g["CLMin"] = Dec(r["CLMin"]);
                    g["CLFoc"] = Dec(r["CLFoc"]);
                    g["CLRebate"] = Dec(r["CLRebate"]);
                    g["CLLast"] = (r["CLLast"] == DBNull.Value) ? Dec(r["CLInit"]) : Dec(r["CLLast"]);
                    g["CLLastDate"] = r["CLLastDate"];
                    g["CLCurrent"] = 0m; g["CLUsage"] = 0m; g["CLCharge"] = 0m;

                    g["Status"] = "";
                    _dtGrid.Rows.Add(g);
                }

                GridMeter.DataSource = _dtGrid;
                ConfigureGrid();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable NewGridTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Sel", typeof(bool));
            dt.Columns.Add("ItemKey", typeof(long));
            dt.Columns.Add("ContractKey", typeof(long));
            dt.Columns.Add("ContractNo", typeof(string));
            dt.Columns.Add("ItemNo", typeof(string));
            dt.Columns.Add("SerialNo", typeof(string));
            dt.Columns.Add("MachineName", typeof(string));
            dt.Columns.Add("DebtorCode", typeof(string));
            dt.Columns.Add("Customer", typeof(string));
            dt.Columns.Add("BillingMode", typeof(string));
            dt.Columns.Add("Mode", typeof(string));
            dt.Columns.Add("BKKey", typeof(long));
            dt.Columns.Add("BKType", typeof(string));
            dt.Columns.Add("BKAC", typeof(string));
            dt.Columns.Add("BKRate", typeof(decimal));
            dt.Columns.Add("BKMin", typeof(decimal));
            dt.Columns.Add("BKFoc", typeof(decimal));
            dt.Columns.Add("BKRebate", typeof(decimal));
            dt.Columns.Add("BKLast", typeof(decimal));
            dt.Columns.Add("BKLastDate", typeof(DateTime));
            dt.Columns.Add("BKCurrent", typeof(decimal));
            dt.Columns.Add("BKUsage", typeof(decimal));
            dt.Columns.Add("BKCharge", typeof(decimal));
            dt.Columns.Add("CLKey", typeof(long));
            dt.Columns.Add("CLType", typeof(string));
            dt.Columns.Add("CLAC", typeof(string));
            dt.Columns.Add("CLRate", typeof(decimal));
            dt.Columns.Add("CLMin", typeof(decimal));
            dt.Columns.Add("CLFoc", typeof(decimal));
            dt.Columns.Add("CLRebate", typeof(decimal));
            dt.Columns.Add("CLLast", typeof(decimal));
            dt.Columns.Add("CLLastDate", typeof(DateTime));
            dt.Columns.Add("CLCurrent", typeof(decimal));
            dt.Columns.Add("CLUsage", typeof(decimal));
            dt.Columns.Add("CLCharge", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));
            return dt;
        }

        private void ConfigureGrid()
        {
            GridViewMeter.OptionsBehavior.Editable = true;
            string[] hidden = new string[] {
                "ItemKey","ContractKey","DebtorCode","BillingMode","BKKey","BKAC","BKRate","BKMin","BKFoc","BKRebate",
                "BKLastDate","CLKey","CLAC","CLRate","CLMin","CLFoc","CLRebate","CLLastDate" };
            foreach (string h in hidden)
                if (GridViewMeter.Columns[h] != null) GridViewMeter.Columns[h].Visible = false;

            SetCol("Sel", "Bill?", 50, true);
            SetCol("ContractNo", "Contract", 110, false);
            SetCol("ItemNo", "Service Item No", 110, false);
            SetCol("SerialNo", "Serial", 120, false);
            SetCol("MachineName", "Service Item", 200, false);
            SetCol("Customer", "Customer", 220, false);
            SetCol("Mode", "Mode", 70, false);
            SetCol("BKType", "BK Meter", 110, false);
            SetNum("BKLast", "BK Last", 90, false);
            SetNum("BKCurrent", "BK Current", 95, true);
            SetNum("BKUsage", "BK Usage", 90, false);
            SetNum("BKCharge", "BK Charge", 95, false);
            SetCol("CLType", "CL Meter", 110, false);
            SetNum("CLLast", "CL Last", 90, false);
            SetNum("CLCurrent", "CL Current", 95, true);
            SetNum("CLUsage", "CL Usage", 90, false);
            SetNum("CLCharge", "CL Charge", 95, false);
            SetCol("Status", "Status", 130, false);
        }

        private void SetCol(string field, string caption, int width, bool editable)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewMeter.Columns[field];
            if (c == null) return;
            c.Caption = caption;
            c.Width = width;
            c.OptionsColumn.AllowEdit = editable;
            c.OptionsColumn.ReadOnly = !editable;
        }

        private void SetNum(string field, string caption, int width, bool editable)
        {
            SetCol(field, caption, width, editable);
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewMeter.Columns[field];
            if (c == null) return;
            c.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c.DisplayFormat.FormatString = "n0";
        }

        // ───────────────────── Buttons ─────────────────────

        private void RefreshTimer_Tick(object sender, EventArgs e) { }

        private void BtnRefresh_Click(object sender, EventArgs e) { LoadData(); }
        private void BtnFilter_Click(object sender, EventArgs e) { LoadData(); }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            this.TxtSearch.EditValue = null;
            this.ChkShowAll.Checked = true;
            this.CmbMonth.SelectedIndex = DateTime.Today.Month - 1;
            this.CmbDay.SelectedIndex = DateTime.Today.Day - 1;
            LoadData();
        }

        private void BtnFetch_Click(object sender, EventArgs e)
        {
            if (_dtGrid == null || _dtGrid.Rows.Count == 0)
            { XtraMessageBox.Show("Nothing to fetch — the list is empty.", "Fetch"); return; }

            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                IMeterReadingApiClient client = MeterReadingApiClientFactory.Create(_dbSetting);
                Dictionary<string, MeterReadingDto> bySerial = new Dictionary<string, MeterReadingDto>(StringComparer.OrdinalIgnoreCase);
                foreach (MeterReadingDto d in client.GetOnline())
                    if (!string.IsNullOrWhiteSpace(d.SerialNumber)) bySerial[d.SerialNumber.Trim()] = d;
                foreach (MeterReadingDto d in client.GetOffline(SelectedMonth()))
                    if (!string.IsNullOrWhiteSpace(d.SerialNumber) && !bySerial.ContainsKey(d.SerialNumber.Trim()))
                        bySerial[d.SerialNumber.Trim()] = d;

                int matched = 0;
                foreach (DataRow r in _dtGrid.Rows)
                {
                    string serial = S(r["SerialNo"]).Trim();
                    MeterReadingDto dto;
                    if (serial.Length > 0 && bySerial.TryGetValue(serial, out dto))
                    {
                        if (D64(r["BKKey"]) > 0)
                        {
                            r["BKCurrent"] = dto.TotalBK;
                            RecalcCell(r, "BK");
                        }
                        if (D64(r["CLKey"]) > 0)
                        {
                            r["CLCurrent"] = dto.TotalCL;
                            RecalcCell(r, "CL");
                        }
                        r["Sel"] = true;
                        r["Status"] = "Matched" + (dto.TrackingId != null ? " (offline)" : " (online)");
                        matched++;
                    }
                    else
                    {
                        r["Status"] = "No API data";
                    }
                }
                GridMeter.RefreshDataSource();
                XtraMessageBox.Show(matched + " of " + _dtGrid.Rows.Count + " machine(s) matched by serial number.",
                    "Fetch complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Fetch failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor.Current = Cursors.Default; }
        }

        private void RecalcCell(DataRow r, string role)
        {
            MeterBillLine ln = new MeterBillLine();
            ln.Last = Dec(r[role + "Last"]);
            ln.Current = Dec(r[role + "Current"]);
            ln.Rate = Dec(r[role + "Rate"]);
            ln.MinCharges = Dec(r[role + "Min"]);
            ln.Foc = Dec(r[role + "Foc"]);
            ln.RebatePct = Dec(r[role + "Rebate"]);
            ScpInvoiceBuilder.ComputeCharge(ln);
            r[role + "Usage"] = ln.Usage;
            r[role + "Charge"] = ln.Charge;
        }

        private void BtnSelfManualKeyIn_Click(object sender, EventArgs e)
        {
            XtraMessageBox.Show("BK Current and CL Current columns are editable — type the readings, then click Generate Invoice.\r\n" +
                "(Usage / charge recalculates when you leave the cell.)", "Manual Key In",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GridViewMeter_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (e.Column == null) return;
            if (e.Column.FieldName == "BKCurrent") RecalcRowHandle(e.RowHandle, "BK");
            else if (e.Column.FieldName == "CLCurrent") RecalcRowHandle(e.RowHandle, "CL");
        }

        private void RecalcRowHandle(int rowHandle, string role)
        {
            DataRow r = GridViewMeter.GetDataRow(rowHandle);
            if (r == null) return;
            RecalcCell(r, role);
        }

        private void BtnGenerateInvoice_Click(object sender, EventArgs e)
        {
            if (_dbSetting == null || _dtGrid == null) return;
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            // Build billable lines from selected rows; group key respects each contract's mode.
            Dictionary<string, List<MeterBillLine>> groups = new Dictionary<string, List<MeterBillLine>>();
            Dictionary<string, string> groupRef = new Dictionary<string, string>();
            Dictionary<string, string> groupDebtor = new Dictionary<string, string>();

            foreach (DataRow r in _dtGrid.Rows)
            {
                if (!Convert.ToBoolean(r["Sel"])) continue;
                string mode = S(r["BillingMode"]);
                long contractKey = D64(r["ContractKey"]);
                long itemKey = D64(r["ItemKey"]);
                string groupKey = mode == "S" ? ("C" + contractKey + "_I" + itemKey) : ("C" + contractKey);
                string refNo = mode == "S" ? S(r["ItemNo"]) : S(r["ContractNo"]);

                AddRoleLine(groups, groupRef, groupDebtor, groupKey, refNo, r, "BK", "Black");
                AddRoleLine(groups, groupRef, groupDebtor, groupKey, refNo, r, "CL", "Colour");
            }

            if (groups.Count == 0)
            { XtraMessageBox.Show("No selected rows with a billable reading.", "Generate Invoice"); return; }

            string monthName = new CultureInfo("en-US").DateTimeFormat.GetMonthName(SelectedMonth());
            int invoicesCreated = 0;
            List<string> docNos = new List<string>();

            foreach (KeyValuePair<string, List<MeterBillLine>> grp in groups)
            {
                string debtor = groupDebtor[grp.Key];
                string refNo = groupRef[grp.Key];
                string desc = "Meter Billing " + monthName + " - " + refNo;
                try
                {
                    AutoCount.Invoicing.Sales.Invoice.Invoice doc = ScpInvoiceBuilder.BuildInvoice(
                        _dbSetting, debtor, refNo, desc, DateTime.Today, DateTime.Now, grp.Value);

                    AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry invForm =
                        new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc);
                    invForm.ShowDialog(this);

                    bool saved = doc.DocumentState == AutoCount.Invoicing.InvoicingDocumentState.View
                                 && doc.DocNo != AutoCount.Const.AppConst.NewDocumentNo;
                    if (!saved) continue;

                    WriteMeterTrans(doc.DocKey, doc.DocNo, grp.Value);
                    invoicesCreated++;
                    docNos.Add(doc.DocNo);
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Invoice for " + refNo + " failed:\r\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            XtraMessageBox.Show(invoicesCreated + " invoice(s) created:\r\n" + string.Join(", ", docNos.ToArray()),
                "Generate Invoice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }

        private void AddRoleLine(Dictionary<string, List<MeterBillLine>> groups,
            Dictionary<string, string> groupRef, Dictionary<string, string> groupDebtor,
            string groupKey, string refNo, DataRow r, string role, string colorLabel)
        {
            long meterKey = D64(r[role + "Key"]);
            if (meterKey <= 0) return;
            decimal current = Dec(r[role + "Current"]);
            if (current <= 0m) return; // nothing keyed/fetched for this colour

            MeterBillLine ln = new MeterBillLine();
            ln.ItemKey = D64(r["ItemKey"]);
            ln.ContractKey = D64(r["ContractKey"]);
            ln.ItemMeterKey = meterKey;
            ln.ContractNo = S(r["ContractNo"]);
            ln.ItemNo = S(r["ItemNo"]);
            ln.DebtorCode = S(r["DebtorCode"]);
            ln.SerialNumber = S(r["SerialNo"]);
            ln.ItemName = S(r["MachineName"]);
            ln.MeterTypeCode = S(r[role + "Type"]);
            ln.MeterTypeName = S(r[role + "Type"]);
            ln.ACItemCode = S(r[role + "AC"]);
            ln.ColorLabel = colorLabel;
            ln.Last = Dec(r[role + "Last"]);
            ln.Current = current;
            ln.Rate = Dec(r[role + "Rate"]);
            ln.MinCharges = Dec(r[role + "Min"]);
            ln.Foc = Dec(r[role + "Foc"]);
            ln.RebatePct = Dec(r[role + "Rebate"]);
            if (r[role + "LastDate"] != DBNull.Value) ln.LastDate = Convert.ToDateTime(r[role + "LastDate"]);
            ScpInvoiceBuilder.ComputeCharge(ln);

            if (!groups.ContainsKey(groupKey))
            {
                groups[groupKey] = new List<MeterBillLine>();
                groupRef[groupKey] = refNo;
                groupDebtor[groupKey] = ln.DebtorCode;
            }
            groups[groupKey].Add(ln);
        }

        private void WriteMeterTrans(long invoiceDocKey, string docNo, List<MeterBillLine> lines)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("MeterTrans"))
                {
                    try
                    {
                        foreach (MeterBillLine ln in lines)
                        {
                            SqlCommand cmd = new SqlCommand(
                                "INSERT INTO [dbo].[zSCP_MeterTrans] " +
                                "(ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, MeterTransDate, " +
                                "MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                "VALUES (@simtKey, @siKey, @mtCode, @transDate, @reading, @docKey, @remark)", cn, tx);
                            cmd.Parameters.AddWithValue("@simtKey", ln.ItemMeterKey);
                            cmd.Parameters.AddWithValue("@siKey", ln.ItemKey);
                            cmd.Parameters.AddWithValue("@mtCode", ln.MeterTypeCode);
                            cmd.Parameters.AddWithValue("@transDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@reading", ln.Current);
                            cmd.Parameters.AddWithValue("@docKey", invoiceDocKey);
                            cmd.Parameters.AddWithValue("@remark", ln.ColorLabel + " meter - Invoice " + docNo);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // ---- helpers ----
        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal Dec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
    }
}
