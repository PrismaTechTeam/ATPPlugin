using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.ServiceAppointment.OperationForms
{
    /// <summary>
    /// Meter Type Transaction Entry — records current meter readings for a service item's
    /// meter types and optionally generates a Sales Invoice.
    /// Matches V8 UI: header (Service Tag, Reading Date, Stock Code, Debtor Code, Department,
    /// Job, Location), Sales Invoice Settings group, and a 13-column grid.
    /// </summary>
    // RETIRED (module v2): meter billing now runs from Meter Reading Integration. Menu entry removed.
    // [AutoCount.PlugIn.MenuItem("Meter Type Transaction Entry",
    //     MenuOrder = 430,
    //     OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_METER_TYPE_TRANS,
    //     VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_METER_TYPE_TRANS)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class MeterTypeTransactionEntry_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dtGrid;                   // grid data source
        private long _currentServiceItemKey = 0;     // selected service item
        private string _currentUserCode = "ADMIN";
        private bool _suppressEvents = false;

        // ───────────────────── Constructors ─────────────────────
        public MeterTypeTransactionEntry_Form() { InitializeComponent(); }

        public MeterTypeTransactionEntry_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public MeterTypeTransactionEntry_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        // ───────────────────── Form Load ─────────────────────
        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            _suppressEvents = true;
            try
            {
                _currentUserCode = ResolveCurrentUserCode();
                _dtGrid = NewGridTable();
                GridMeter.DataSource = _dtGrid;
                LoadLookups();

                DtReadingDate.DateTime = DateTime.Now;
                DtInvoiceDate.DateTime = DateTime.Today;
                CmbDescription.Text = "";

                UpdateStatusBar();

                // wire grid events after initial setup
                GridViewMeter.CellValueChanged += GridViewMeter_CellValueChanged;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private string ResolveCurrentUserCode()
        {
            try
            {
                UserSession s = UserSession.CurrentUserSession;
                if (s != null && !string.IsNullOrEmpty(s.LoginUserID)) return s.LoginUserID;
            }
            catch { }
            return "ADMIN";
        }

        // ───────────────────── Grid DataTable ─────────────────────
        private DataTable NewGridTable()
        {
            DataTable dt = new DataTable("MeterGrid");
            dt.Columns.Add("ServiceItemMeterTypeKey", typeof(long));
            dt.Columns.Add("MeterTypeCode", typeof(string));
            dt.Columns.Add("MeterTypeName", typeof(string));
            dt.Columns.Add("MinCharges", typeof(decimal));
            dt.Columns.Add("UnitPrice", typeof(decimal));
            dt.Columns.Add("FOCQty", typeof(decimal));
            dt.Columns.Add("RebateQtyPercent", typeof(decimal));
            dt.Columns.Add("LastReadDate", typeof(DateTime));
            dt.Columns.Add("LastReading", typeof(decimal));
            dt.Columns.Add("MeterUsage", typeof(decimal));
            dt.Columns.Add("TotalCharges", typeof(decimal));
            dt.Columns.Add("Selected", typeof(bool));
            dt.Columns.Add("CurrentReading", typeof(decimal));
            dt.Columns.Add("UseMinCharges", typeof(bool));
            dt.Columns.Add("ACItemCode", typeof(string));
            // defaults
            dt.Columns["MinCharges"].DefaultValue = 0m;
            dt.Columns["UnitPrice"].DefaultValue = 0m;
            dt.Columns["FOCQty"].DefaultValue = 0m;
            dt.Columns["RebateQtyPercent"].DefaultValue = 0m;
            dt.Columns["LastReading"].DefaultValue = 0m;
            dt.Columns["MeterUsage"].DefaultValue = 0m;
            dt.Columns["TotalCharges"].DefaultValue = 0m;
            dt.Columns["Selected"].DefaultValue = false;
            dt.Columns["CurrentReading"].DefaultValue = 0m;
            dt.Columns["UseMinCharges"].DefaultValue = false;
            dt.Columns["ACItemCode"].DefaultValue = "";
            return dt;
        }

        // ───────────────────── Load Lookups ─────────────────────
        private void LoadLookups()
        {
            // Service Tag — ServiceItem list
            try
            {
                DataTable dtSI = _dbSetting.GetDataTable(
                    "SELECT ServiceItemCode, StockCode, ISNULL(DebtorCode,'') AS DebtorCode, " +
                    "ISNULL([Description],'') AS [Description] " +
                    "FROM [dbo].[zSCP_ServiceItem] ORDER BY ServiceItemCode", false);
                LkServiceTag.Properties.DataSource = dtSI;
                LkServiceTag.Properties.DisplayMember = "ServiceItemCode";
                LkServiceTag.Properties.ValueMember = "ServiceItemCode";
                EnsureDropdownButton(LkServiceTag);
                GridView vSI = LkServiceTag.Properties.View;
                vSI.Columns.Clear();
                vSI.OptionsView.ShowGroupPanel = false;
                vSI.Columns.AddField("ServiceItemCode").VisibleIndex = 0;
                vSI.Columns.AddField("StockCode").VisibleIndex = 1;
                vSI.Columns.AddField("DebtorCode").VisibleIndex = 2;
                vSI.Columns.AddField("Description").VisibleIndex = 3;
            }
            catch { }

            // Stock Code — Item table (read-only display, auto-filled from service item)
            try
            {
                DataTable dtItem = _dbSetting.GetDataTable(
                    "SELECT ItemCode, [Description] FROM [dbo].[Item] ORDER BY ItemCode", false);
                LkStockCode.Properties.DataSource = dtItem;
                LkStockCode.Properties.DisplayMember = "ItemCode";
                LkStockCode.Properties.ValueMember = "ItemCode";
                LkStockCode.Properties.PopupFormWidth = 520;
                EnsureDropdownButton(LkStockCode);
                GridView vI = LkStockCode.Properties.View;
                vI.Columns.Clear();
                vI.OptionsView.ShowGroupPanel = false;
                vI.Columns.AddField("ItemCode").VisibleIndex = 0;
                vI.Columns.AddField("Description").VisibleIndex = 1;
                LkStockCode.EditValueChanged += LkStockCode_EditValueChanged;
            }
            catch { }

            // Debtor Code
            try
            {
                DataTable dtDeb = _dbSetting.GetDataTable(
                    "SELECT AccNo AS DebtorCode, CompanyName FROM [dbo].[Debtor] ORDER BY AccNo", false);
                LkDebtorCode.Properties.DataSource = dtDeb;
                LkDebtorCode.Properties.DisplayMember = "DebtorCode";
                LkDebtorCode.Properties.ValueMember = "DebtorCode";
                LkDebtorCode.Properties.PopupFormWidth = 520;
                EnsureDropdownButton(LkDebtorCode);
                GridView vD = LkDebtorCode.Properties.View;
                vD.Columns.Clear();
                vD.OptionsView.ShowGroupPanel = false;
                vD.Columns.AddField("DebtorCode").VisibleIndex = 0;
                vD.Columns.AddField("CompanyName").VisibleIndex = 1;
                LkDebtorCode.EditValueChanged += LkDebtorCode_EditValueChanged;
            }
            catch { }

            // Department
            try
            {
                DataTable dtDept = _dbSetting.GetDataTable(
                    "SELECT DeptNo, ISNULL([Description],'') AS [Description] FROM [dbo].[Dept] WHERE IsActive='T' OR IsActive IS NULL ORDER BY DeptNo", false);
                LkDepartment.Properties.DataSource = dtDept;
                LkDepartment.Properties.DisplayMember = "DeptNo";
                LkDepartment.Properties.ValueMember = "DeptNo";
                LkDepartment.Properties.PopupFormWidth = 420;
                EnsureDropdownButton(LkDepartment);
                GridView vDp = LkDepartment.Properties.View;
                vDp.Columns.Clear();
                vDp.OptionsView.ShowGroupPanel = false;
                vDp.Columns.AddField("DeptNo").VisibleIndex = 0;
                vDp.Columns.AddField("Description").VisibleIndex = 1;
                LkDepartment.EditValueChanged += LkDepartment_EditValueChanged;
            }
            catch { }

            // Job (Project table in AC 2.x)
            try
            {
                DataTable dtJob;
                try
                {
                    dtJob = _dbSetting.GetDataTable(
                        "SELECT ProjNo AS JobNo, ISNULL([Description],'') AS [Description] FROM [dbo].[Project] WHERE IsActive='T' OR IsActive IS NULL ORDER BY ProjNo", false);
                }
                catch
                {
                    dtJob = _dbSetting.GetDataTable(
                        "SELECT JobNo, ISNULL([Description],'') AS [Description] FROM [dbo].[Job] ORDER BY JobNo", false);
                }
                LkJob.Properties.DataSource = dtJob;
                LkJob.Properties.DisplayMember = "JobNo";
                LkJob.Properties.ValueMember = "JobNo";
                LkJob.Properties.PopupFormWidth = 420;
                EnsureDropdownButton(LkJob);
                GridView vJ = LkJob.Properties.View;
                vJ.Columns.Clear();
                vJ.OptionsView.ShowGroupPanel = false;
                vJ.Columns.AddField("JobNo").VisibleIndex = 0;
                vJ.Columns.AddField("Description").VisibleIndex = 1;
                LkJob.EditValueChanged += LkJob_EditValueChanged;
            }
            catch
            {
                EnsureDropdownButton(LkJob);
            }

            // Location
            try
            {
                DataTable dtLoc = _dbSetting.GetDataTable(
                    "SELECT Location, ISNULL([Description],'') AS [Description] FROM [dbo].[Location] ORDER BY Location", false);
                LkLocation.Properties.DataSource = dtLoc;
                LkLocation.Properties.DisplayMember = "Location";
                LkLocation.Properties.ValueMember = "Location";
                LkLocation.Properties.PopupFormWidth = 420;
                EnsureDropdownButton(LkLocation);
                GridView vL = LkLocation.Properties.View;
                vL.Columns.Clear();
                vL.OptionsView.ShowGroupPanel = false;
                vL.Columns.AddField("Location").VisibleIndex = 0;
                vL.Columns.AddField("Description").VisibleIndex = 1;
                LkLocation.EditValueChanged += LkLocation_EditValueChanged;
            }
            catch { }

            // Invoice No Format — list AC's DocNoFormat entries for AR Invoice (DocType "IV").
            // Picks the one flagged IsDefault='T' on first load. Ellipsis button opens AC's
            // standard Document No Format Maintenance form (FormDocumentNoMaintenance).
            LoadInvoiceNoFormats();
        }

        private void LoadInvoiceNoFormats()
        {
            try
            {
                DataTable dtFmt = _dbSetting.GetDataTable(
                    "SELECT [Name], ISNULL([IsDefault],'F') AS IsDefault " +
                    "FROM [dbo].[DocNoFormat] WHERE DocType='IV' ORDER BY [Name]", false);

                CmbInvoiceNoFmt.Properties.Items.Clear();
                string defaultName = null;
                foreach (DataRow r in dtFmt.Rows)
                {
                    string name = r["Name"].ToString();
                    CmbInvoiceNoFmt.Properties.Items.Add(name);
                    if (string.Equals(r["IsDefault"].ToString(), "T", StringComparison.OrdinalIgnoreCase))
                        defaultName = name;
                }

                if (!string.IsNullOrEmpty(defaultName))
                    CmbInvoiceNoFmt.EditValue = defaultName;
                else if (CmbInvoiceNoFmt.Properties.Items.Count > 0)
                    CmbInvoiceNoFmt.SelectedIndex = 0;
            }
            catch { }
        }

        // Ellipsis on CmbInvoiceNoFmt opens AC's standard Doc-No-Format Maintenance.
        private void CmbInvoiceNoFmt_ButtonClick(object sender,
            DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            if (e.Button.Kind != DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis) return;
            try
            {
                UserSession session = UserSession.CurrentUserSession;
                if (session == null) return;
                using (AutoCount.GeneralMaint.DocumentNoFormat.FormDocumentNoMaintenance frm =
                    new AutoCount.GeneralMaint.DocumentNoFormat.FormDocumentNoMaintenance(session))
                {
                    frm.ShowDialog(this);
                }
                LoadInvoiceNoFormats();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Failed to open Document No Format Maintenance:\r\n" + ex.Message,
                    "Document No Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ───────────────────── Service Tag changed ─────────────────────
        private void LkServiceTag_EditValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            _suppressEvents = true;
            try
            {
                string serviceItemCode = (LkServiceTag.EditValue ?? "").ToString().Trim();
                if (string.IsNullOrEmpty(serviceItemCode))
                {
                    ClearHeaderAndGrid();
                    return;
                }

                // Load service item header
                string sql = "SELECT si.ServiceItemKey, si.ServiceItemCode, si.StockCode, " +
                    "ISNULL(si.DebtorCode,'') AS DebtorCode, ISNULL(si.DepartmentCode,'') AS DepartmentCode, " +
                    "ISNULL(si.JobCode,'') AS JobCode, ISNULL(si.StockLocationCode,'') AS StockLocationCode, " +
                    "ISNULL(i.[Description],'') AS StockDesc, ISNULL(d.CompanyName,'') AS DebtorName " +
                    "FROM [dbo].[zSCP_ServiceItem] si " +
                    "LEFT JOIN [dbo].[Item] i ON i.ItemCode = si.StockCode " +
                    "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = si.DebtorCode " +
                    "WHERE si.ServiceItemCode = N'" + SQLString(serviceItemCode) + "'";
                DataTable dtSI = _dbSetting.GetDataTable(sql, false);
                if (dtSI.Rows.Count == 0)
                {
                    ClearHeaderAndGrid();
                    return;
                }

                DataRow r = dtSI.Rows[0];
                _currentServiceItemKey = Convert.ToInt64(r["ServiceItemKey"]);

                // Populate header fields
                LkStockCode.EditValue = r["StockCode"].ToString();
                LblStockCodeDesc.Text = r["StockDesc"].ToString();
                LkDebtorCode.EditValue = r["DebtorCode"].ToString();
                LblDebtorCodeDesc.Text = r["DebtorName"].ToString();
                LkDepartment.EditValue = r["DepartmentCode"].ToString();
                LblDeptDesc.Text = LookupDescription(LkDepartment);
                LkJob.EditValue = r["JobCode"].ToString();
                LblJobDesc.Text = LookupDescription(LkJob);
                LkLocation.EditValue = r["StockLocationCode"].ToString();
                LblLocationDesc.Text = LookupDescription(LkLocation);

                // Update description
                CmbDescription.Text = "Billing- [" + serviceItemCode + "]";

                // Load meter types for this service item
                LoadMeterGrid(_currentServiceItemKey);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Error loading service item:\r\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void ClearHeaderAndGrid()
        {
            _currentServiceItemKey = 0;
            LkStockCode.EditValue = null;
            LblStockCodeDesc.Text = "";
            LkDebtorCode.EditValue = null;
            LblDebtorCodeDesc.Text = "";
            LkDepartment.EditValue = null;
            LblDeptDesc.Text = "";
            LkJob.EditValue = null;
            LblJobDesc.Text = "";
            LkLocation.EditValue = null;
            LblLocationDesc.Text = "";
            CmbDescription.Text = "";
            _dtGrid.Rows.Clear();
            UpdateStatusBar();
        }

        // ───────────────────── Load Meter Grid ─────────────────────
        private void LoadMeterGrid(long serviceItemKey)
        {
            _dtGrid.Rows.Clear();

            // Get all meter types for this service item, with last reading info
            string sql =
                "SELECT simt.ServiceItemMeterTypeKey, simt.MeterTypeCode, " +
                "ISNULL(mt.[Description],'') AS MeterTypeName, " +
                "simt.MinimumCharges, simt.ChargesRate, simt.FOCQty, simt.RebateQtyInPercent, " +
                "simt.InitialReading, ISNULL(mt.ACItemCode,'') AS ACItemCode, " +
                "lr.LastTransDate, lr.LastReading " +
                "FROM [dbo].[zSCP_ServiceItemMeterType] simt " +
                "INNER JOIN [dbo].[zSCP_MeterType] mt ON mt.MeterTypeCode = simt.MeterTypeCode " +
                "OUTER APPLY ( " +
                "   SELECT TOP 1 t.MeterTransDate AS LastTransDate, t.MeterTransReading AS LastReading " +
                "   FROM [dbo].[zSCP_MeterTrans] t " +
                "   WHERE t.ServiceItemMeterTypeKey = simt.ServiceItemMeterTypeKey " +
                "   ORDER BY t.MeterTransDate DESC, t.MeterTransKey DESC " +
                ") lr " +
                "WHERE simt.ServiceItemKey = " + serviceItemKey +
                " ORDER BY simt.MeterTypeCode";

            DataTable dtMT = _dbSetting.GetDataTable(sql, false);

            foreach (DataRow src in dtMT.Rows)
            {
                DataRow row = _dtGrid.NewRow();
                row["ServiceItemMeterTypeKey"] = src["ServiceItemMeterTypeKey"];
                row["MeterTypeCode"] = src["MeterTypeCode"].ToString();
                row["MeterTypeName"] = src["MeterTypeName"].ToString();
                row["MinCharges"] = Convert.ToDecimal(src["MinimumCharges"]);
                row["UnitPrice"] = Convert.ToDecimal(src["ChargesRate"]);
                row["FOCQty"] = Convert.ToDecimal(src["FOCQty"]);
                row["RebateQtyPercent"] = Convert.ToDecimal(src["RebateQtyInPercent"]);

                if (src["LastTransDate"] != DBNull.Value)
                    row["LastReadDate"] = Convert.ToDateTime(src["LastTransDate"]);
                decimal lastReading = src["LastReading"] != DBNull.Value
                    ? Convert.ToDecimal(src["LastReading"])
                    : Convert.ToDecimal(src["InitialReading"]);
                row["LastReading"] = lastReading;

                row["CurrentReading"] = 0m;
                row["MeterUsage"] = 0m;
                row["TotalCharges"] = 0m;
                row["Selected"] = false;
                // UseMinCharges: default true if MinCharges > 0 and ChargesRate == 0 (rental type)
                decimal minC = Convert.ToDecimal(src["MinimumCharges"]);
                decimal rate = Convert.ToDecimal(src["ChargesRate"]);
                row["UseMinCharges"] = (minC > 0m && rate == 0m);
                row["ACItemCode"] = src["ACItemCode"].ToString();

                _dtGrid.Rows.Add(row);
            }
            _dtGrid.AcceptChanges();
            UpdateStatusBar();
        }

        // ───────────────────── Grid cell changed — recalculate ─────────────────────
        private void GridViewMeter_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (_suppressEvents) return;
            if (e.Column.FieldName == "CurrentReading" || e.Column.FieldName == "UseMinCharges")
            {
                RecalcRow(e.RowHandle);
            }
            UpdateStatusBar();
        }

        private void RecalcRow(int rowHandle)
        {
            if (rowHandle < 0) return;
            decimal currentReading = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "CurrentReading") ?? 0m);
            decimal lastReading = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "LastReading") ?? 0m);
            decimal unitPrice = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "UnitPrice") ?? 0m);
            decimal focQty = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "FOCQty") ?? 0m);
            decimal rebatePercent = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "RebateQtyPercent") ?? 0m);
            decimal minCharges = Convert.ToDecimal(GridViewMeter.GetRowCellValue(rowHandle, "MinCharges") ?? 0m);
            bool useMin = Convert.ToBoolean(GridViewMeter.GetRowCellValue(rowHandle, "UseMinCharges") ?? false);

            // Meter Usage = Current - Last (min 0)
            decimal usage = currentReading - lastReading;
            if (usage < 0m) usage = 0m;

            // Billable usage = max(usage - FOC, 0)
            decimal billable = usage - focQty;
            if (billable < 0m) billable = 0m;

            // Apply rebate
            if (rebatePercent > 0m)
                billable = billable * (1m - rebatePercent / 100m);

            // Total charges
            decimal total;
            if (useMin)
            {
                total = minCharges;
            }
            else
            {
                total = billable * unitPrice;
                if (total < minCharges)
                    total = minCharges;
            }

            GridViewMeter.SetRowCellValue(rowHandle, "MeterUsage", usage);
            GridViewMeter.SetRowCellValue(rowHandle, "TotalCharges", total);
        }

        // ───────────────────── Selection buttons ─────────────────────
        private void OnTickSelection(object sender, EventArgs e)
        {
            int rh = GridViewMeter.FocusedRowHandle;
            if (rh < 0) return;
            bool current = Convert.ToBoolean(GridViewMeter.GetRowCellValue(rh, "Selected") ?? false);
            GridViewMeter.SetRowCellValue(rh, "Selected", !current);
        }

        private void OnSelectAll(object sender, EventArgs e)
        {
            for (int i = 0; i < GridViewMeter.RowCount; i++)
            {
                GridViewMeter.SetRowCellValue(i, "Selected", true);
            }
        }

        private void OnDeselectAll(object sender, EventArgs e)
        {
            for (int i = 0; i < GridViewMeter.RowCount; i++)
            {
                GridViewMeter.SetRowCellValue(i, "Selected", false);
            }
        }

        // ───────────────────── Fetch Reading (demo) ─────────────────────
        // Confirmation -> random 5-digit reading written into every visible row's
        // CurrentReading. Tick the unlabeled checkbox next to the button to flip
        // into demo-failure mode (Contract No not found).
        private static readonly Random _fetchRng = new Random();

        private void OnFetchReading(object sender, EventArgs e)
        {
            if (this.ChkSimulateFailure.Checked)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "Unable to fetch:\r\nReason: Contract No not found.\r\nPlease use key in manual reading.",
                    "Fetch Reading",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            if (GridViewMeter.RowCount == 0)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "No meter rows to fetch.",
                    "Fetch Reading",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }

            System.Windows.Forms.DialogResult ans = DevExpress.XtraEditors.XtraMessageBox.Show(
                "Fetch the latest reading from the device for " + GridViewMeter.RowCount + " row(s)?",
                "Fetch Reading",
                System.Windows.Forms.MessageBoxButtons.OKCancel,
                System.Windows.Forms.MessageBoxIcon.Question);
            if (ans != System.Windows.Forms.DialogResult.OK) return;

            for (int i = 0; i < GridViewMeter.RowCount; i++)
            {
                int reading = _fetchRng.Next(10000, 100000); // 5-digit
                GridViewMeter.SetRowCellValue(i, "CurrentReading", (decimal)reading);
            }
        }

        // ───────────────────── Save & Generate Invoice ─────────────────────
        private void OnSaveAndGenerateInvoice(object sender, EventArgs e)
        {
            if (_currentServiceItemKey == 0)
            {
                XtraMessageBox.Show("Please select a Service Tag first.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string debtorCode = (LkDebtorCode.EditValue ?? "").ToString().Trim();
            if (string.IsNullOrEmpty(debtorCode))
            {
                XtraMessageBox.Show("Debtor Code is required to generate an invoice.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Commit any pending grid edits
            GridViewMeter.CloseEditor();
            GridViewMeter.UpdateCurrentRow();

            // Check at least one row is selected with CurrentReading > 0
            int selectedCount = 0;
            for (int i = 0; i < _dtGrid.Rows.Count; i++)
            {
                DataRow row = _dtGrid.Rows[i];
                if (Convert.ToBoolean(row["Selected"]))
                {
                    decimal cr = Convert.ToDecimal(row["CurrentReading"]);
                    if (cr <= 0m)
                    {
                        XtraMessageBox.Show("Row " + (i + 1) + " (" + row["MeterTypeCode"] + ") is selected but has no current reading.",
                            "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    selectedCount++;
                }
            }

            if (selectedCount == 0)
            {
                XtraMessageBox.Show("Please select at least one meter type row to save.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // ── Step 1: Build invoice (NOT saved yet) and open AC Invoice form ──
                // User reviews and saves from the AutoCount Invoice Entry form.
                // If user cancels / closes without saving → nothing is saved.
                AutoCount.Invoicing.Sales.Invoice.Invoice doc = BuildInvoiceDocument(debtorCode);

                AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry invoiceForm =
                    new AutoCount.Invoicing.Sales.Invoice.FormInvoiceEntry(doc);
                invoiceForm.ShowDialog(this);

                // Authoritative saved-check: AC transitions DocumentState to View only after a
                // successful Save. DocKey alone is unreliable — AddNew pre-allocates it, so a
                // cancelled invoice still has DocKey>0 and DocNo stays as the placeholder
                // AppConst.NewDocumentNo ("<<New>>"). Match what FormInvoiceEntry itself checks.
                bool invoiceSaved =
                    doc.DocumentState == AutoCount.Invoicing.InvoicingDocumentState.View
                    && doc.DocNo != AutoCount.Const.AppConst.NewDocumentNo;

                if (!invoiceSaved)
                {
                    XtraMessageBox.Show(
                        "Invoice was not saved. Meter readings were NOT recorded.",
                        "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                long invoiceDocKey = doc.DocKey;

                // ── Step 2: Invoice saved → now save meter readings and link to invoice ──
                DateTime readingDate = DtReadingDate.DateTime;
                using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("MeterTrans"))
                    {
                        try
                        {
                            foreach (DataRow row in _dtGrid.Rows)
                            {
                                if (!Convert.ToBoolean(row["Selected"])) continue;

                                long simtKey = Convert.ToInt64(row["ServiceItemMeterTypeKey"]);
                                string mtCode = row["MeterTypeCode"].ToString();
                                decimal currentReading = Convert.ToDecimal(row["CurrentReading"]);

                                SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO [dbo].[zSCP_MeterTrans] " +
                                    "(ServiceItemMeterTypeKey, ServiceItemKey, MeterTypeCode, MeterTransDate, " +
                                    "MeterTransReading, SalesInvoiceDocKey, Remark) " +
                                    "VALUES (@simtKey, @siKey, @mtCode, @transDate, @reading, @docKey, @remark)", cn, tx);
                                cmd.Parameters.AddWithValue("@simtKey", simtKey);
                                cmd.Parameters.AddWithValue("@siKey", _currentServiceItemKey);
                                cmd.Parameters.AddWithValue("@mtCode", mtCode);
                                cmd.Parameters.AddWithValue("@transDate", readingDate);
                                cmd.Parameters.AddWithValue("@reading", currentReading);
                                cmd.Parameters.AddWithValue("@docKey", invoiceDocKey);
                                cmd.Parameters.AddWithValue("@remark", "Meter reading - Invoice " + doc.DocNo);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                XtraMessageBox.Show(
                    selectedCount + " meter reading(s) saved.\r\nInvoice: " + doc.DocNo,
                    "Meter Type Transaction Entry",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reload grid to update last reading values
                LoadMeterGrid(_currentServiceItemKey);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Failed:\r\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Builds a pre-filled Invoice document from the selected meter grid rows.
        /// Shared by both OpenInvoiceForm (UI) and SaveInvoiceDirect (fallback).
        /// </summary>
        private AutoCount.Invoicing.Sales.Invoice.Invoice BuildInvoiceDocument(string debtorCode)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                    UserSession.CurrentUserSession, _dbSetting);

            AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();
            if (doc == null)
                throw new InvalidOperationException("Failed to create a new invoice document.");

            doc.DebtorCode = debtorCode;
            doc.DocDate = DtInvoiceDate.DateTime;
            doc.Description = CmbDescription.Text;
            doc.RefDocNo = (LkServiceTag.EditValue ?? "").ToString();

            // Pin the running-number format AC will use when generating DocNo.
            string fmtName = (CmbInvoiceNoFmt.EditValue ?? CmbInvoiceNoFmt.Text ?? "").ToString().Trim();
            if (!string.IsNullOrEmpty(fmtName))
                doc.DocNoFormatName = fmtName;

            if (doc.DetailCount > 0) doc.ClearDetails();

            string deptCode = (LkDepartment.EditValue ?? "").ToString();
            string jobCode = (LkJob.EditValue ?? "").ToString();
            string locationCode = (LkLocation.EditValue ?? "").ToString();
            string readingDateStr = DtReadingDate.DateTime.ToString("dd/MM/yyyy");

            foreach (DataRow row in _dtGrid.Rows)
            {
                if (!Convert.ToBoolean(row["Selected"])) continue;

                decimal totalCharges = Convert.ToDecimal(row["TotalCharges"]);
                decimal meterUsage = Convert.ToDecimal(row["MeterUsage"]);
                string mtName = row["MeterTypeName"].ToString();
                decimal unitPrice = Convert.ToDecimal(row["UnitPrice"]);
                bool useMin = Convert.ToBoolean(row["UseMinCharges"]);
                decimal lastReading = Convert.ToDecimal(row["LastReading"]);
                decimal currentReading = Convert.ToDecimal(row["CurrentReading"]);
                DateTime lastReadDate = row["LastReadDate"] != DBNull.Value
                    ? Convert.ToDateTime(row["LastReadDate"]) : DateTime.MinValue;
                string lastReadDateStr = lastReadDate > DateTime.MinValue
                    ? lastReadDate.ToString("dd/MM/yyyy") : "N/A";

                // ── Billing line ──
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                string acItemCode = row["ACItemCode"].ToString();
                if (!string.IsNullOrEmpty(acItemCode))
                    dtl.ItemCode = acItemCode;
                dtl.Description = mtName;

                if (useMin || unitPrice == 0m)
                { dtl.Qty = 1m; dtl.UnitPrice = totalCharges; }
                else
                { dtl.Qty = meterUsage > 0m ? meterUsage : 1m; dtl.UnitPrice = unitPrice; }

                if (!string.IsNullOrEmpty(deptCode)) dtl.DeptNo = deptCode;
                if (!string.IsNullOrEmpty(jobCode)) dtl.ProjNo = jobCode;
                if (!string.IsNullOrEmpty(locationCode)) dtl.Location = locationCode;

                // ── Description-only rows (V8 pattern) ──
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtlCur = doc.AddDetail();
                dtlCur.Description = "Current Meter Reading (" + readingDateStr + ") : " + currentReading.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtlPrev = doc.AddDetail();
                dtlPrev.Description = "Previous Meter Reading (" + lastReadDateStr + ") : " + lastReading.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtlUsage = doc.AddDetail();
                dtlUsage.Description = "Meter Charges Usage : " + meterUsage.ToString("n0");

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtlBlank = doc.AddDetail();
                dtlBlank.Description = "";
            }
            return doc;
        }


        // ───────────────────── Exit ─────────────────────
        private void OnExit(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F2)
            {
                OnExit(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ───────────────────── Lookup description helpers ─────────────────────
        private void LkStockCode_EditValueChanged(object sender, EventArgs e)
        {
            LblStockCodeDesc.Text = LookupDescription(LkStockCode);
        }

        private void LkDebtorCode_EditValueChanged(object sender, EventArgs e)
        {
            LblDebtorCodeDesc.Text = LookupDescription(LkDebtorCode);
        }

        private void LkDepartment_EditValueChanged(object sender, EventArgs e)
        {
            LblDeptDesc.Text = LookupDescription(LkDepartment);
        }

        private void LkJob_EditValueChanged(object sender, EventArgs e)
        {
            LblJobDesc.Text = LookupDescription(LkJob);
        }

        private void LkLocation_EditValueChanged(object sender, EventArgs e)
        {
            LblLocationDesc.Text = LookupDescription(LkLocation);
        }

        // ───────────────────── Status bar ─────────────────────
        private void UpdateStatusBar()
        {
            int rowCount = _dtGrid != null ? _dtGrid.Rows.Count : 0;
            LblRowCount.Text = rowCount.ToString();

            decimal total = 0m;
            if (_dtGrid != null)
            {
                foreach (DataRow r in _dtGrid.Rows)
                {
                    if (Convert.ToBoolean(r["Selected"]))
                        total += Convert.ToDecimal(r["TotalCharges"]);
                }
            }
            LblTotal.Text = total.ToString("n2");
        }

        // ───────────────────── Shared helpers ─────────────────────

        /// <summary>
        /// Gets the description column value from a SearchLookUpEdit's current row.
        /// Looks for "Description", "CompanyName", or the second visible column.
        /// </summary>
        private static string LookupDescription(SearchLookUpEdit lk)
        {
            if (lk.EditValue == null || lk.EditValue == DBNull.Value) return "";
            DataTable dt = lk.Properties.DataSource as DataTable;
            if (dt == null) return "";
            string valMember = lk.Properties.ValueMember;
            string val = lk.EditValue.ToString();
            DataRow[] rows = dt.Select("[" + valMember + "] = '" + val.Replace("'", "''") + "'");
            if (rows.Length == 0) return "";
            // Try known description columns
            if (dt.Columns.Contains("Description")) return rows[0]["Description"].ToString();
            if (dt.Columns.Contains("CompanyName")) return rows[0]["CompanyName"].ToString();
            // fallback: second column
            if (dt.Columns.Count >= 2) return rows[0][1].ToString();
            return "";
        }

        /// <summary>
        /// Ensures a SearchLookUpEdit has at least a Combo + Ellipsis button.
        /// </summary>
        private static void EnsureDropdownButton(SearchLookUpEdit lk)
        {
            bool hasCombo = false;
            foreach (DevExpress.XtraEditors.Controls.EditorButton b in lk.Properties.Buttons)
            {
                if (b.Kind == DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) hasCombo = true;
            }
            if (!hasCombo)
                lk.Properties.Buttons.Add(new DevExpress.XtraEditors.Controls.EditorButton(
                    DevExpress.XtraEditors.Controls.ButtonPredefines.Combo));
        }
    }
}
