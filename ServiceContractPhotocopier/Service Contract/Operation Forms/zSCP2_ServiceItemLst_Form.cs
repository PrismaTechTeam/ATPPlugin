using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// "Maintain Service Item" — the SAME module as "Maintain Service Contract" (same editor, same
    /// CRUD), but the list shows ONE ROW PER SERVICE ITEM (CSSI) instead of one per contract.
    /// Every row carries its ContractKey/ContractNo, so the inherited New / Edit / Delete /
    /// double-click handlers work unchanged (they open the contract editor for that machine).
    /// Pure subclass — no UI of its own (hence no Designer/resx triple).
    /// </summary>
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    [AutoCount.PlugIn.MenuItem("Maintain Service Item", MenuOrder = 210, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public class zSCP2_ServiceItemLst_Form : zSCP2_ContractLst_Form
    {
        private SimpleButton _btnCopyNew;

        public zSCP2_ServiceItemLst_Form() : base() { Retitle(); }
        public zSCP2_ServiceItemLst_Form(UserSession userSession) : base(userSession) { Retitle(); }
        public zSCP2_ServiceItemLst_Form(DBSetting dbSetting) : base(dbSetting) { Retitle(); }

        private void Retitle()
        {
            this.Text = "Maintain Service Item";
            this.PanelHeaderTop.Header = "Maintain Service Item";

            // "Copy to New" — duplicate the selected machine's configuration into a new one
            // (unique info excluded: item no, serial, provided-item serials, initial readings).
            _btnCopyNew = new SimpleButton();
            _btnCopyNew.Text = "Copy to New";
            _btnCopyNew.Location = new System.Drawing.Point(458, 6);
            _btnCopyNew.Size = new System.Drawing.Size(110, 50);
            _btnCopyNew.Click += new EventHandler(OnCopyToNew);
            this.PanelToolbar.Controls.Add(_btnCopyNew);
            _btnCopyNew.BringToFront();
        }

        // New from the ITEM list opens the Service Item editor directly (not the contract editor).
        // On Save the item gets its OWN new contract (legacy model: one contract per CSSI), using the
        // customer picked in the dialog; effective billing day = item override (or the config default).
        protected override void OnNew(object sender, EventArgs e)
        {
            OpenNewItemDialog(new ItemEditData());
        }

        // "Copy to New": preload the dialog from the selected machine, minus its unique identity.
        private void OnCopyToNew(object sender, EventArgs e)
        {
            DataRow row = GridView.FocusedRowHandle < 0 ? null : GridView.GetDataRow(GridView.FocusedRowHandle);
            if (row == null) return;
            try { OpenNewItemDialog(LoadItemAsTemplate(Convert.ToInt64(row["ItemKey"]))); }
            catch (Exception ex) { XtraMessageBox.Show("Copy failed:\r\n" + ex.Message, "Error"); }
        }

        private void OpenNewItemDialog(ItemEditData d)
        {
            int defaultDay = PumsConfig.GetInt(_dbSetting, PumsConfig.KEY_DEFAULT_BILLING_DAY, PumsConfig.DEFAULT_BILLING_DAY_VALUE);
            using (zSCP2_Item_Form f = new zSCP2_Item_Form(_dbSetting, d, defaultDay, true))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    if (f.SelectedContractKey > 0)
                        CreateItemUnderContract(d, f.SelectedContractKey);      // attach to the picked contract
                    else
                        CreateItemWithOwnContract(d, f.SelectedDebtorCode, defaultDay);
                    LoadGrid();
                }
                catch (Exception ex)
                { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void CreateItemWithOwnContract(ItemEditData d, string debtorCode, int defaultDay)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("NewServiceItem"))
                {
                    try
                    {
                        // Auto contract number from the plugin's Document Numbering Format.
                        string contractNo = Classes.ScpDocNo.Next(_dbSetting, Classes.ScpDocNo.DOCTYPE_CONTRACT);

                        int contractDay = d.BillingDayOverride.HasValue ? d.BillingDayOverride.Value : defaultDay;
                        long contractKey;
                        using (SqlCommand cmd = new SqlCommand(
                            "INSERT INTO [dbo].[zSCP2_Contract] " +
                            "(ContractNo, ContractTypeCode, DebtorCode, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
                            " ContractValue, BillingDay, BillingMode, Address1, Attention, Phone, TermCode, AreaCode, StaffCode, " +
                            " Description, Remark1, Remark2, Note, Inactive, Created, LastModified) " +
                            "VALUES (@no,'',@debtor,GETDATE(),NULL,NULL,0,@bday,'G','','','','','','', " +
                            "@desc,'','','','N',GETDATE(),GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@no", contractNo);
                            cmd.Parameters.AddWithValue("@debtor", debtorCode ?? "");
                            cmd.Parameters.AddWithValue("@bday", (byte)Math.Max(1, Math.Min(31, contractDay)));
                            cmd.Parameters.AddWithValue("@desc", d.ServiceItemNo ?? "");
                            contractKey = Convert.ToInt64(cmd.ExecuteScalar());
                        }

                        InsertItemTree(cn, tx, d, contractKey);

                        tx.Commit();
                        XtraMessageBox.Show("Service item saved (contract " + contractNo + " created for it).",
                            "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Build an ItemEditData copy of an existing machine EXCLUDING its unique identity:
        // Service Item No (auto-picked in the dialog), Serial Number, provided-item serials,
        // and initial readings (start at 0 for the new machine).
        private ItemEditData LoadItemAsTemplate(long itemKey)
        {
            ItemEditData d = new ItemEditData();
            DataTable it = _dbSetting.GetDataTable("SELECT * FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + itemKey, false);
            if (it.Rows.Count > 0)
            {
                DataRow r = it.Rows[0];
                d.Description = r["Description"] as string ?? "";
                d.BillingDayOverride = r["BillingDayOverride"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillingDayOverride"]);
                d.DepartmentCode = r["DepartmentCode"] as string ?? "";
                d.JobCode = r["JobCode"] as string ?? "";
                d.StockLocationCode = r["StockLocationCode"] as string ?? "";
            }

            d.ItemCodes = zSCP2_Item_Form.CreateItemCodesTable();
            DataTable ic = _dbSetting.GetDataTable(
                "SELECT ItemCode, Description, Qty FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=" + itemKey + " ORDER BY Pos", false);
            foreach (DataRow s in ic.Rows)
            {
                DataRow nr = d.ItemCodes.NewRow();
                nr["ItemCode"] = s["ItemCode"];
                nr["Description"] = s["Description"];
                nr["Qty"] = s["Qty"];
                nr["SerialNumber"] = "";
                d.ItemCodes.Rows.Add(nr);
            }
            d.ItemCodes.AcceptChanges();

            d.Meters = zSCP2_Item_Form.CreateMetersTable();
            DataTable mt = _dbSetting.GetDataTable(
                "SELECT MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                "RebateQtyInPercent, FOCQty FROM [dbo].[zSCP2_ItemMeter] WHERE ItemKey=" + itemKey + " ORDER BY ItemMeterKey", false);
            foreach (DataRow s in mt.Rows)
            {
                DataRow nr = d.Meters.NewRow();
                nr["MeterTypeCode"] = s["MeterTypeCode"];
                nr["MeterRole"] = s["MeterRole"];
                nr["MinimumCharges"] = s["MinimumCharges"];
                nr["ChargesRate"] = s["ChargesRate"];
                nr["MeterMultiPriceCode"] = s["MeterMultiPriceCode"];
                nr["RebateQtyInPercent"] = s["RebateQtyInPercent"];
                nr["FOCQty"] = s["FOCQty"];
                nr["InitialReading"] = 0m;
                d.Meters.Rows.Add(nr);
            }
            d.Meters.AcceptChanges();
            return d;
        }

        // Attach the new item (+ its item codes + meters) to an EXISTING contract.
        private void CreateItemUnderContract(ItemEditData d, long contractKey)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction("NewServiceItem"))
                {
                    try
                    {
                        InsertItemTree(cn, tx, d, contractKey);
                        tx.Commit();
                        XtraMessageBox.Show("Service item saved under the selected contract.",
                            "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Item + its child rows, inside the caller's transaction. Same SQL as the contract editor.
        private void InsertItemTree(SqlConnection cn, SqlTransaction tx, ItemEditData d, long contractKey)
        {
            long itemKey;
            using (SqlCommand cmd = new SqlCommand(
                "INSERT INTO [dbo].[zSCP2_Item] " +
                "(ContractKey, ServiceItemNo, SerialNumber, Description, BillingDayOverride, " +
                " DepartmentCode, JobCode, StockLocationCode, Pos, Inactive, LastModified) " +
                "VALUES (@ck,@no,@serial,@desc,@bday,@dept,@job,@loc,0,@inact,GETDATE()); " +
                "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
            {
                cmd.Parameters.AddWithValue("@ck", contractKey);
                cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                cmd.Parameters.AddWithValue("@loc", d.StockLocationCode ?? "");
                cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                itemKey = Convert.ToInt64(cmd.ExecuteScalar());
            }

            if (d.ItemCodes != null)
            {
                int pos = 0;
                foreach (DataRow r in d.ItemCodes.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string code = r["ItemCode"] == null ? "" : r["ItemCode"].ToString().Trim();
                    if (code.Length == 0) continue;
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[zSCP2_ItemCode] (ItemKey, ItemCode, Description, Qty, SerialNumber, Pos, LastModified) " +
                        "VALUES (@ik,@code,@desc,@qty,@serial,@pos,GETDATE());", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ik", itemKey);
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@desc", r["Description"] == null ? "" : r["Description"].ToString());
                        cmd.Parameters.AddWithValue("@qty", Dec(r["Qty"]));
                        cmd.Parameters.AddWithValue("@serial", r["SerialNumber"] == null ? "" : r["SerialNumber"].ToString());
                        cmd.Parameters.AddWithValue("@pos", pos++);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (d.Meters != null)
            {
                foreach (DataRow r in d.Meters.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string code = r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString().Trim();
                    if (code.Length == 0) continue;
                    string role = r["MeterRole"] == null ? "NA" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    if (role != "BK" && role != "CL") role = "NA";
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[zSCP2_ItemMeter] " +
                        "(ItemKey, MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                        " RebateQtyInPercent, FOCQty, InitialReading, LastModified) " +
                        "VALUES (@ik,@code,@role,@min,@rate,@multi,@rebate,@foc,@init,GETDATE());", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ik", itemKey);
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.Parameters.AddWithValue("@min", Dec(r["MinimumCharges"]));
                        cmd.Parameters.AddWithValue("@rate", Dec(r["ChargesRate"]));
                        cmd.Parameters.AddWithValue("@multi", r["MeterMultiPriceCode"] == null ? "" : r["MeterMultiPriceCode"].ToString());
                        cmd.Parameters.AddWithValue("@rebate", Dec(r["RebateQtyInPercent"]));
                        cmd.Parameters.AddWithValue("@foc", Dec(r["FOCQty"]));
                        cmd.Parameters.AddWithValue("@init", Dec(r["InitialReading"]));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static decimal Dec(object v)
        {
            return (v == null || v == DBNull.Value) ? 0m : Convert.ToDecimal(v);
        }

        // One row per CSSI. ContractKey/ContractNo stay in the table so the base CRUD works as-is.
        protected override void LoadGrid()
        {
            try
            {
                DataTable dt = _dbSetting.GetDataTable(
                    "SELECT i.ItemKey, i.ServiceItemNo, i.SerialNumber, c.ContractNo, c.DebtorCode, " +
                    "ISNULL(d.CompanyName,'') AS DebtorName, " +
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS BillingDay, c.BillingMode, " +
                    "bk.MeterTypeCode AS BlackMeter, cl.MeterTypeCode AS ColourMeter, " +
                    "i.ServiceExpiryDate, i.Inactive, c.ContractKey " +
                    "FROM [dbo].[zSCP2_Item] i " +
                    "JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = c.DebtorCode " +
                    "LEFT JOIN [dbo].[zSCP2_ItemMeter] bk ON bk.ItemKey = i.ItemKey AND bk.MeterRole = 'BK' " +
                    "LEFT JOIN [dbo].[zSCP2_ItemMeter] cl ON cl.ItemKey = i.ItemKey AND cl.MeterRole = 'CL' " +
                    "ORDER BY i.ServiceItemNo", false);
                Grid.DataSource = dt;

                // The designer's columns are contract-level; rebuild them for the item-level table.
                GridView.Columns.Clear();
                GridView.PopulateColumns();
                Cfg("ServiceItemNo", "Service Item No", 130, 0);
                Cfg("SerialNumber", "Serial Number", 110, 1);
                Cfg("ContractNo", "Contract No.", 100, 2);
                Cfg("DebtorCode", "Customer Code", 100, 3);
                Cfg("DebtorName", "Customer Name", 230, 4);
                Cfg("BillingDay", "Billing Day", 75, 5);
                Cfg("BillingMode", "Billing Mode", 80, 6);
                Cfg("BlackMeter", "Black Meter", 110, 7);
                Cfg("ColourMeter", "Colour Meter", 110, 8);
                GridColumn exp = GridView.Columns["ServiceExpiryDate"];
                if (exp != null)
                {
                    exp.Caption = "Expiry Date"; exp.Width = 95; exp.VisibleIndex = 9;
                    exp.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    exp.DisplayFormat.FormatString = "dd/MM/yyyy";
                }
                Cfg("Inactive", "Inactive", 60, 10);
                GridColumn ck = GridView.Columns["ContractKey"];
                if (ck != null) ck.Visible = false;
                GridColumn ik = GridView.Columns["ItemKey"];
                if (ik != null) ik.Visible = false;

                GridView.OptionsBehavior.Editable = false;
                GridView.RowCellStyle -= GridView_ExpiryCellStyle;   // avoid double-subscribe on Refresh
                GridView.RowCellStyle += GridView_ExpiryCellStyle;
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void Cfg(string field, string caption, int width, int visibleIndex)
        {
            GridColumn c = GridView.Columns[field];
            if (c == null) return;
            if (caption != null) c.Caption = caption;
            if (width > 0) c.Width = width;
            if (visibleIndex >= 0) c.VisibleIndex = visibleIndex;
        }

        // RowCellStyle fires per visible cell on every repaint/scroll, so the bold font is cached
        // once (a new Font() per cell was the main list-scroll lag). Also short-circuit fast.
        private System.Drawing.Font _boldFont;
        private static readonly System.Drawing.Color _expiredRed = System.Drawing.Color.FromArgb(198, 40, 40);
        private static readonly System.Drawing.Color _activeGreen = System.Drawing.Color.FromArgb(46, 125, 50);

        // Expiry Date: RED bold when expired, GREEN bold when still active (same as the editor grid).
        private void GridView_ExpiryCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "ServiceExpiryDate") return;
            object v = GridView.GetRowCellValue(e.RowHandle, e.Column);
            if (v == null || v == DBNull.Value) return;
            DateTime expiry = Convert.ToDateTime(v);
            e.Appearance.ForeColor = expiry.Date < DateTime.Today ? _expiredRed : _activeGreen;
            if (_boldFont == null)
                _boldFont = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.Font = _boldFont;
        }
    }
}
