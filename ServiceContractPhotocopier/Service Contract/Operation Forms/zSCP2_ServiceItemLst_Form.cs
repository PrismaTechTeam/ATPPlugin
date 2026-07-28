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
        public zSCP2_ServiceItemLst_Form() : base() { Retitle(); }
        public zSCP2_ServiceItemLst_Form(UserSession userSession) : base(userSession) { Retitle(); }
        public zSCP2_ServiceItemLst_Form(DBSetting dbSetting) : base(dbSetting) { Retitle(); }

        private void Retitle()
        {
            this.Text = "Maintain Service Item";
            this.PanelHeaderTop.Header = "Maintain Service Item";

            // Toolbar is New / Edit / Delete / Refresh only — Exit and "Copy to New" removed on request
            // ("Copy From..." lives in the Service Item editor's ribbon; the window closes with its X).
            if (this.BtnExit != null) this.BtnExit.Visible = false;
        }

        // Edit from the ITEM list must open the SERVICE ITEM editor for the selected row — the base
        // implementation opens the contract editor, which is wrong for an item-level list.
        protected override void OnEdit(object sender, EventArgs e)
        {
            DataRow row = GridView.FocusedRowHandle < 0 ? null : GridView.GetDataRow(GridView.FocusedRowHandle);
            if (row == null) return;
            long itemKey = Convert.ToInt64(row["ItemKey"]);
            string contractNo = row["ContractNo"] == null ? "" : row["ContractNo"].ToString();
            int defaultDay = PumsConfig.GetInt(_dbSetting, PumsConfig.KEY_DEFAULT_BILLING_DAY, PumsConfig.DEFAULT_BILLING_DAY_VALUE);
            try
            {
                ItemEditData d = zSCP2_Contract_Form.LoadOneItem(_dbSetting, itemKey);
                // Embedded mode: the item already has a home, so the contract shows read-only.
                using (zSCP2_Item_Form f = new zSCP2_Item_Form(_dbSetting, d, defaultDay, contractNo))
                {
                    f.AllowOwnershipChange = true;   // ownership transfers are done from this list only
                    if (f.ShowDialog(this) != DialogResult.OK) return;
                    UpdateItemTree(d);
                    ShowSavedTick("Service item " + d.ServiceItemNo + " updated.");
                    LoadGrid();
                    FocusItemRow(itemKey);
                }
            }
            catch (Exception ex)
            { XtraMessageBox.Show("Edit failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // Delete from the ITEM list removes only the selected ITEM. The base deletes the whole contract
        // (and every sibling item with it) — never what you want from an item-level row.
        protected override void OnDelete(object sender, EventArgs e)
        {
            DataRow row = GridView.FocusedRowHandle < 0 ? null : GridView.GetDataRow(GridView.FocusedRowHandle);
            if (row == null) return;
            long itemKey = Convert.ToInt64(row["ItemKey"]);
            string no = row["ServiceItemNo"] == null ? "" : row["ServiceItemNo"].ToString();
            string serial = row.Table.Columns.Contains("SerialNumber") && row["SerialNumber"] != DBNull.Value
                ? row["SerialNumber"].ToString() : "";
            // Same two-stage flow as the contract editor's delete: an impact summary (with the real
            // reading count), then a typed confirmation — a machine's whole reading history dies here.
            int readings = 0;
            try
            {
                object o = _dbSetting.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry en " +
                    "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = en.ItemMeterKey WHERE m.ItemKey = " + itemKey);
                readings = o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { }
            if (XtraMessageBox.Show("Delete service item '" + no + "'" +
                    (serial.Length > 0 ? " (serial " + serial + ")" : "") + "?\r\n\r\n" +
                    "This PERMANENTLY deletes the machine, its meter configuration, its provided-item lines and " +
                    (readings > 0 ? "its " + readings + " meter reading record(s) (incl. the billing log)."
                                  : "its reading history.") +
                    "\r\nThe contract itself is kept.",
                    "Delete Service Item", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            string typed = DevExpress.XtraEditors.XtraInputBox.Show(
                "This cannot be undone. Type DELETE to confirm:", "Delete Service Item", "");
            if (typed == null || typed.Trim().ToUpperInvariant() != "DELETE") return;
            try
            {
                // Item-bound provided lines have no FK (multiple-cascade-path limitation) — removed in
                // the same transaction so they never linger as zombie lines on the contract.
                _dbSetting.ExecuteNonQuery(
                    "BEGIN TRY BEGIN TRAN; " +
                    "DELETE FROM [dbo].[zSCP2_ContractSparePart] WHERE ItemKey=" + itemKey + "; " +
                    "DELETE FROM [dbo].[zSCP2_Item] WHERE ItemKey=" + itemKey + "; " +
                    "COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH");
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
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
                    // Refresh the master grid and jump to the row we just created — reloading alone left
                    // the user parked on the old scroll position, so the new item looked like it vanished.
                    LoadGrid();
                    FocusItemRow(d.ItemKey);
                }
                catch (Exception ex)
                { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        // Save an EDITED item. Deliberately does NOT wipe-and-rebuild zSCP2_ItemMeter: that table is the
        // FK parent of zSCP_MeterTrans and zSCP2_MeterEntry with ON DELETE CASCADE, so deleting a meter
        // row destroys that meter's reading history. Meters are matched on their natural key
        // (MeterTypeCode + MeterRole) and updated in place; only meters the user actually removed are
        // deleted, and only those lose their readings.
        private void UpdateItemTree(ItemEditData d)
        {
            using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
            {
                cn.Open();
                using (SqlTransaction tx = cn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE [dbo].[zSCP2_Item] SET ServiceItemNo=@no, SerialNumber=@serial, Description=@desc, " +
                            "BillingDayOverride=@bday, DepartmentCode=@dept, JobCode=@job, Inactive=@inact, " +
                            "LastModified=GETDATE() WHERE ItemKey=@ik", cn, tx))
                        {
                            cmd.Parameters.AddWithValue("@no", d.ServiceItemNo ?? "");
                            cmd.Parameters.AddWithValue("@serial", d.SerialNumber ?? "");
                            cmd.Parameters.AddWithValue("@desc", d.Description ?? "");
                            cmd.Parameters.AddWithValue("@bday", (object)d.BillingDayOverride ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@dept", d.DepartmentCode ?? "");
                            cmd.Parameters.AddWithValue("@job", d.JobCode ?? "");
                            cmd.Parameters.AddWithValue("@inact", d.Inactive ? "Y" : "N");
                            cmd.Parameters.AddWithValue("@ik", d.ItemKey);
                            cmd.ExecuteNonQuery();
                        }

                        long contractKey = 0;
                        using (SqlCommand q = new SqlCommand("SELECT ContractKey FROM [dbo].[zSCP2_Item] WHERE ItemKey=@ik", cn, tx))
                        {
                            q.Parameters.AddWithValue("@ik", d.ItemKey);
                            object o = q.ExecuteScalar();
                            if (o != null && o != DBNull.Value) contractKey = Convert.ToInt64(o);
                        }

                        zSCP2_Item_Form.SaveMetersPreservingReadings(cn, tx, d, d.ItemKey);

                        // Item codes carry no history, so wipe-and-rebuild is safe here.
                        using (SqlCommand del = new SqlCommand("DELETE FROM [dbo].[zSCP2_ItemCode] WHERE ItemKey=@ik", cn, tx))
                        { del.Parameters.AddWithValue("@ik", d.ItemKey); del.ExecuteNonQuery(); }
                        InsertItemCodeRows(cn, tx, d, d.ItemKey);

                        zSCP2_Contract_Form.SaveItemSpareParts(cn, tx, d, d.ItemKey, contractKey);
                        // Extras hydrate in LoadOneItem/LoadExtras; if that failed, persisting the empty
                        // defaults would wipe the item's Grade/Note/PM/context overrides — skip instead.
                        if (d.LoadedCtxValid) zSCP2_Item_Form.PersistItemExtras(cn, tx, d, d.ItemKey);
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        /// <summary>Reload the grid and select/scroll to a given item — the post-save refresh contract.</summary>
        public void RefreshAndFocus(long itemKey)
        {
            LoadGrid();
            FocusItemRow(itemKey);
        }

        // Find the row carrying itemKey and make it the focused, visible row.
        private void FocusItemRow(long itemKey)
        {
            if (itemKey <= 0) return;
            try
            {
                for (int i = 0; i < GridView.RowCount; i++)
                {
                    int rh = GridView.GetVisibleRowHandle(i);
                    if (rh < 0) continue;
                    object v = GridView.GetRowCellValue(rh, "ItemKey");
                    if (v == null || v == DBNull.Value) continue;
                    if (Convert.ToInt64(v) != itemKey) continue;
                    GridView.FocusedRowHandle = rh;
                    GridView.MakeRowVisible(rh);
                    GridView.SelectRow(rh);
                    return;
                }
            }
            catch { }
        }

        // AutoCount's 444 images have no tick, so the success box borrows DevExpress's green "apply".
        private void ShowSavedTick(string text)
        {
            try
            {
                DevExpress.XtraEditors.XtraMessageBoxArgs a = new DevExpress.XtraEditors.XtraMessageBoxArgs();
                a.Owner = this;
                a.Caption = "Saved";
                a.Text = text;
                a.Buttons = new DialogResult[] { DialogResult.OK };
                a.ImageOptions.SvgImage = DevExpress.Images.ImageResourceCache.Default.GetSvgImage("devav/actions/apply.svg");
                a.ImageOptions.SvgImageSize = new System.Drawing.Size(32, 32);
                XtraMessageBox.Show(a);
            }
            catch { XtraMessageBox.Show(text, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information); }
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
                        ShowSavedTick("Service item " + d.ServiceItemNo + " saved (contract " + contractNo + " created for it).");
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
                "SELECT ItemMeterKey, MeterTypeCode, MeterRole, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
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
                nr["CustomTiers"] = zSCP2_Item_Form.LoadCustomTiersCsv(_dbSetting, Convert.ToInt64(s["ItemMeterKey"]));
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
                        ShowSavedTick("Service item " + d.ServiceItemNo + " saved under the selected contract.");
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Item + its child rows, inside the caller's transaction. Same SQL as the contract editor.
        // Rebuild an item's provided-item-code rows. Shared by the insert and update paths (item codes
        // carry no history, so wipe-and-rebuild is safe for them — unlike meters).
        private void InsertItemCodeRows(SqlConnection cn, SqlTransaction tx, ItemEditData d, long itemKey)
        {
            if (d.ItemCodes == null) return;
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

        private void InsertItemTree(SqlConnection cn, SqlTransaction tx, ItemEditData d, long contractKey)
        {
            // Reserve the REAL service item number now (only if it was an auto-preview) so the counter
            // is only consumed on an actual save — clicking Auto never burned a number.
            if (d.ServiceItemNoIsAuto)
                d.ServiceItemNo = Classes.ScpDocNo.Next(_dbSetting, Classes.ScpDocNo.DOCTYPE_SERVICE_ITEM);

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
            d.ItemKey = itemKey;   // hand the new key back so the list can reload and jump to this row

            InsertItemCodeRows(cn, tx, d, itemKey);

            if (d.Meters != null)
            {
                foreach (DataRow r in d.Meters.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string code = r["MeterTypeCode"] == null ? "" : r["MeterTypeCode"].ToString().Trim();
                    if (code.Length == 0) continue;
                    string role = r["MeterRole"] == null ? "" : r["MeterRole"].ToString().Trim().ToUpperInvariant();
                    zSCP2_Item_Form.RequireMeterRole(role, code);   // no silent "NA" default — empty role blocks the save
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[zSCP2_ItemMeter] " +
                        "(ItemKey, MeterTypeCode, MeterRole, MachineSerialNo, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                        " RebateQtyInPercent, FOCQty, InitialReading, LastModified) " +
                        "VALUES (@ik,@code,@role,@mser,@min,@rate,@multi,@rebate,@foc,@init,GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ik", itemKey);
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.Parameters.AddWithValue("@mser", r.Table.Columns.Contains("MachineSerialNo") && r["MachineSerialNo"] != DBNull.Value
                            ? ((string)r["MachineSerialNo"]).Trim() : "");
                        cmd.Parameters.AddWithValue("@min", Dec(r["MinimumCharges"]));
                        cmd.Parameters.AddWithValue("@rate", Dec(r["ChargesRate"]));
                        cmd.Parameters.AddWithValue("@multi", r["MeterMultiPriceCode"] == null ? "" : r["MeterMultiPriceCode"].ToString());
                        cmd.Parameters.AddWithValue("@rebate", Dec(r["RebateQtyInPercent"]));
                        cmd.Parameters.AddWithValue("@foc", Dec(r["FOCQty"]));
                        cmd.Parameters.AddWithValue("@init", Dec(r["InitialReading"]));
                        long newMeterKey = Convert.ToInt64(cmd.ExecuteScalar());
                        string tiersCsv = r.Table.Columns.Contains("CustomTiers") && r["CustomTiers"] != DBNull.Value
                            ? Convert.ToString(r["CustomTiers"]) : "";
                        zSCP2_Item_Form.SaveCustomTiers(cn, tx, newMeterKey, tiersCsv);
                    }
                }
            }

            // Item-bound spare parts (from the service item editor's grid).
            if (d.SpareParts != null)
            {
                int spPos = 0;
                foreach (DataRow r in d.SpareParts.Rows)
                {
                    if (r.RowState == DataRowState.Deleted) continue;
                    string code = r["ItemCode"] == DBNull.Value ? "" : Convert.ToString(r["ItemCode"]).Trim();
                    if (code.Length == 0 && Convert.ToString(r["Description"]).Trim().Length == 0) continue;
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[zSCP2_ContractSparePart] " +
                        "(ContractKey, ItemKey, ItemCode, Description, SerialNumber, Unlimited, UOM, Quantity, Discount, UnitPrice, " +
                        " TaxType, TaxInclusive, TaxRate, Pos, LastModified) " +
                        "VALUES (@ck,@ik,@code,@desc,@serial,@unl,@uom,@qty,@disc,@price,@ttype,@tinc,@trate,@pos,GETDATE());", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@ck", contractKey);
                        cmd.Parameters.AddWithValue("@ik", itemKey);
                        cmd.Parameters.AddWithValue("@code", code);
                        cmd.Parameters.AddWithValue("@desc", r["Description"] == DBNull.Value ? "" : Convert.ToString(r["Description"]));
                        cmd.Parameters.AddWithValue("@serial", r.Table.Columns.Contains("SerialNumber") && r["SerialNumber"] != DBNull.Value ? Convert.ToString(r["SerialNumber"]) : "");
                        cmd.Parameters.AddWithValue("@unl", Convert.ToBoolean(r["Unlimited"]) ? "Y" : "N");
                        cmd.Parameters.AddWithValue("@uom", r["UOM"] == DBNull.Value ? "" : Convert.ToString(r["UOM"]));
                        cmd.Parameters.AddWithValue("@qty", Dec(r["Quantity"]));
                        cmd.Parameters.AddWithValue("@disc", r["Discount"] == DBNull.Value ? "" : Convert.ToString(r["Discount"]));
                        cmd.Parameters.AddWithValue("@price", Dec(r["UnitPrice"]));
                        cmd.Parameters.AddWithValue("@ttype", r["TaxType"] == DBNull.Value ? "" : Convert.ToString(r["TaxType"]));
                        cmd.Parameters.AddWithValue("@tinc", Convert.ToBoolean(r["TaxInclusive"]) ? "Y" : "N");
                        cmd.Parameters.AddWithValue("@trate", Dec(r["TaxRate"]));
                        cmd.Parameters.AddWithValue("@pos", spPos++);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // Extended item columns (Item Code, Grade, More Header, Note, Remarks).
            zSCP2_Item_Form.PersistItemExtras(cn, tx, d, itemKey);
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
                // Context columns fall back to the parent contract when the item hasn't overridden them,
                // mirroring the editor's inherit rule so the grid shows the EFFECTIVE value.
                // LEFT JOIN: an item may be owned by a customer directly (no contract). Its effective
                // owner is COALESCE(contract debtor, OwnerDebtorCode).
                DataTable dt = _dbSetting.GetDataTable(
                    "SELECT i.ItemKey, i.ServiceItemNo, i.SerialNumber, ISNULL(c.ContractNo,'') AS ContractNo, " +
                    "ISNULL(COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')),'') AS DebtorCode, " +
                    "ISNULL(d.CompanyName,'') AS DebtorName, " +
                    "i.ItemCode, i.Description, i.GradeCode, i.ReferenceNo, " +
                    "COALESCE(NULLIF(i.ContractTypeCode,''), c.ContractTypeCode, '') AS ContractTypeCode, " +
                    "COALESCE(NULLIF(i.StaffCode,''), c.StaffCode, '') AS StaffCode, " +
                    "COALESCE(NULLIF(i.AreaCode,''), c.AreaCode, '') AS AreaCode, " +
                    "COALESCE(NULLIF(i.Phone,''), c.Phone, '') AS Phone, " +
                    "COALESCE(NULLIF(i.Attention,''), c.Attention, '') AS Attention, " +
                    "COALESCE(i.ServiceStartDate, c.ServiceStartDate) AS ServiceStartDate, " +
                    "i.DepartmentCode, i.JobCode AS ProjNo, " +
                    "COALESCE(i.BillingDayOverride, c.BillingDay) AS BillingDay, c.BillingMode, " +
                    "bk.MeterTypeCode AS BlackMeter, cl.MeterTypeCode AS ColourMeter, " +
                    "COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) AS ServiceExpiryDate, " +
                    // EFFECTIVE inactive: a machine whose CONTRACT is deactivated will not bill either —
                    // showing the item's own 'N' there used to look active while billing had stopped.
                    "CASE WHEN ISNULL(c.Inactive,'N')='Y' THEN 'Y (contract)' WHEN i.Inactive='Y' THEN 'Y' ELSE '' END AS Inactive, " +
                    "c.ContractKey " +
                    "FROM [dbo].[zSCP2_Item] i " +
                    "LEFT JOIN [dbo].[zSCP2_Contract] c ON c.ContractKey = i.ContractKey " +
                    "LEFT JOIN [dbo].[Debtor] d ON d.AccNo = COALESCE(NULLIF(c.DebtorCode,''), NULLIF(i.OwnerDebtorCode,'')) " +
                    // Grouped: a multi-machine item can carry several BK/CL meters — a plain join would
                    // multiply the item's row once per meter.
                    "LEFT JOIN (SELECT ItemKey, MIN(MeterTypeCode) AS MeterTypeCode FROM [dbo].[zSCP2_ItemMeter] WHERE MeterRole='BK' GROUP BY ItemKey) bk ON bk.ItemKey = i.ItemKey " +
                    "LEFT JOIN (SELECT ItemKey, MIN(MeterTypeCode) AS MeterTypeCode FROM [dbo].[zSCP2_ItemMeter] WHERE MeterRole='CL' GROUP BY ItemKey) cl ON cl.ItemKey = i.ItemKey " +
                    "WHERE ISNULL(i.IsGroupItem,'N') = 'N' " +   // group "machines" live on their contract's Group Deal tab
                    "ORDER BY i.ServiceItemNo", false);
                Grid.DataSource = dt;

                // The designer's columns are contract-level; rebuild them for the item-level table.
                GridView.Columns.Clear();
                GridView.PopulateColumns();
                Cfg("ServiceItemNo", "Service Item No", 130, 0);
                Cfg("SerialNumber", "Machine Serial", 110, 1);
                Cfg("ContractNo", "Contract No.", 100, 2);
                Cfg("DebtorCode", "Customer Code", 100, 3);
                Cfg("DebtorName", "Customer Name", 230, 4);
                Cfg("ItemCode", "Item Code", 110, 5);
                Cfg("Description", "Description", 180, 6);
                Cfg("GradeCode", "Grade", 70, 7);
                Cfg("ContractTypeCode", "Contract Type", 100, 8);
                Cfg("ReferenceNo", "Reference No", 110, 9);
                Cfg("StaffCode", "Agent", 80, 10);
                Cfg("BillingDay", "Billing Day", 75, 11);
                Cfg("BillingMode", "Billing Mode", 80, 12);
                Cfg("BlackMeter", "Black Meter", 110, 13);
                Cfg("ColourMeter", "Colour Meter", 110, 14);
                GridColumn sd = GridView.Columns["ServiceStartDate"];
                if (sd != null)
                {
                    sd.Caption = "Start Date"; sd.Width = 95; sd.VisibleIndex = 15;
                    sd.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    sd.DisplayFormat.FormatString = "dd/MM/yyyy";
                }
                GridColumn exp = GridView.Columns["ServiceExpiryDate"];
                if (exp != null)
                {
                    exp.Caption = "Expiry Date"; exp.Width = 95; exp.VisibleIndex = 16;
                    exp.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
                    exp.DisplayFormat.FormatString = "dd/MM/yyyy";
                }
                Cfg("AreaCode", "Area", 80, 17);
                Cfg("Attention", "Attention", 120, 18);
                Cfg("Phone", "Phone", 100, 19);
                Cfg("DepartmentCode", "Department", 90, 20);
                Cfg("ProjNo", "Project", 90, 21);
                Cfg("Inactive", "Inactive", 90, 22);
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
            // Traffic light: red = expired · amber = expiring within 30 days · green = active.
            if (expiry.Date < DateTime.Today) e.Appearance.ForeColor = _expiredRed;
            else if (expiry.Date <= DateTime.Today.AddDays(30)) e.Appearance.ForeColor = System.Drawing.Color.DarkOrange;
            else e.Appearance.ForeColor = _activeGreen;
            if (_boldFont == null)
                _boldFont = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.Font = _boldFont;
        }
    }
}
