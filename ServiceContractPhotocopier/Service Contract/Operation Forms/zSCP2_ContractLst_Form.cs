using System;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// List form for the combined Service Contract module v2 (zSCP2_*). Single entry point that
    /// replaces the old separate "Maintain Service Contract" + "Maintain Service Item" menus.
    /// </summary>
    // SingleInstanceThreadForm(..., mergeMainMenu: true) merges AutoCount's main menu bar (File,
    // G/L, A/R, ... Service & Contract) into this window - the native "navbar" look.
    [AutoCount.PlugIn.MenuItem("Maintain Service Contract", MenuOrder = 200, ShowAsDialog = false)]
    [AutoCount.Application.SingleInstanceThreadForm(FormWindowState.Maximized, true)]
    public partial class zSCP2_ContractLst_Form : XtraForm
    {
        protected DBSetting _dbSetting;   // protected: shared with the "Maintain Service Item" alias subclass

        public zSCP2_ContractLst_Form() { InitializeComponent(); ApplyAutoCountToolbarImages(); }

        // Native AutoCount toolbar look: the official large icons (same ones Stock Item / Address
        // Maintenance use), image left + caption right, default boxed button style.
        private void ApplyAutoCountToolbarImages()
        {
            try
            {
                // GetAutoCountImage expects DPI-based dimensions (96/120/144...). Native AutoCount
                // forms use AutoScaleMode=Dpi so their AutoScaleDimensions ARE the DPI; ours is
                // Font-based (7,14) which would shrink the icons to ~1% — pass the real DPI instead.
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                this.BtnNew.ImageOptions.Image = img.GetLargeImage_New();
                this.BtnEdit.ImageOptions.Image = img.GetLargeImage_Edit();
                this.BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete2();
                this.BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                this.BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }   // icons are cosmetic — never block the form over an image lookup
        }

        public zSCP2_ContractLst_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public zSCP2_ContractLst_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            LoadGrid();
            GridView.DoubleClick += delegate { OnEdit(null, null); };
        }

        // Virtual so the "Maintain Service Item" alias can present the SAME data at service-item
        // level (one row per CSSI) while every CRUD handler below keeps working off row["ContractKey"].
        protected virtual void LoadGrid()
        {
            try
            {
                Grid.DataSource = _dbSetting.GetDataTable(
                    "SELECT ContractKey, ContractNo, ContractTypeCode, DebtorCode, DebtorName, ContractDate, " +
                    "ServiceStartDate, ServiceExpiryDate, ContractValue, BillingDay, BillOnMonthEnd, BillingMode, " +
                    "Agent, Area, DeptNo, ProjNo, ReferenceNo, Description, ItemCount, Inactive " +
                    "FROM [dbo].[zvSCP2_ContractList] ORDER BY ContractNo", false);
                EnsureContractDateColumns();
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        // Contract Start / Contract Expiry columns with a traffic-light Expiry (red = expired,
        // amber = expiring within 30 days, green = active), plus a renewal-reminder count in the
        // green header. Runs after every load; column creation is once-only.
        private System.Drawing.Font _expiryBold;
        private void EnsureContractDateColumns()
        {
            if (GridView.Columns.ColumnByFieldName("ServiceStartDate") == null)
            {
                DevExpress.XtraGrid.Columns.GridColumn cs = GridView.Columns.AddVisible("ServiceStartDate");
                cs.Caption = "Contract Start";
                cs.Width = 95;
                DevExpress.XtraGrid.Columns.GridColumn cx =
                    GridView.Columns.ColumnByFieldName("ContractDate");
                cs.VisibleIndex = cx != null ? cx.VisibleIndex + 1 : 5;
            }
            if (GridView.Columns.ColumnByFieldName("ServiceExpiryDate") == null)
            {
                DevExpress.XtraGrid.Columns.GridColumn ce = GridView.Columns.AddVisible("ServiceExpiryDate");
                ce.Caption = "Contract Expiry";
                ce.Width = 100;
                DevExpress.XtraGrid.Columns.GridColumn cs2 =
                    GridView.Columns.ColumnByFieldName("ServiceStartDate");
                ce.VisibleIndex = cs2 != null ? cs2.VisibleIndex + 1 : 6;
            }
            GridView.RowCellStyle -= ContractList_ExpiryCellStyle;   // avoid double-subscribe on Refresh
            GridView.RowCellStyle += ContractList_ExpiryCellStyle;

            // Renewal reminder: how many ACTIVE contracts expire within the next 30 days.
            try
            {
                int soon = 0;
                DataTable src = Grid.DataSource as DataTable;
                if (src != null && src.Columns.Contains("ServiceExpiryDate"))
                    foreach (DataRow r in src.Rows)
                    {
                        if (Convert.ToString(r["Inactive"]) == "Y") continue;
                        if (r["ServiceExpiryDate"] == DBNull.Value) continue;
                        DateTime xp = Convert.ToDateTime(r["ServiceExpiryDate"]).Date;
                        if (xp >= DateTime.Today && xp <= DateTime.Today.AddDays(30)) soon++;
                    }
                PanelHeaderTop.Hint = soon > 0
                    ? "⚠  " + soon + " contract(s) expiring within 30 days — check the amber Contract Expiry dates below."
                    : "In this window, you can create, modify, or delete service contracts and their service items.";
            }
            catch { }
        }

        private void ContractList_ExpiryCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "ServiceExpiryDate") return;
            object v = GridView.GetRowCellValue(e.RowHandle, e.Column);
            if (v == null || v == DBNull.Value) return;
            DateTime expiry = Convert.ToDateTime(v).Date;
            if (expiry < DateTime.Today) e.Appearance.ForeColor = System.Drawing.Color.Firebrick;
            else if (expiry <= DateTime.Today.AddDays(30)) e.Appearance.ForeColor = System.Drawing.Color.DarkOrange;
            else e.Appearance.ForeColor = System.Drawing.Color.ForestGreen;
            if (_expiryBold == null) _expiryBold = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.Font = _expiryBold;
        }

        private DataRow GetSelectedRow()
        {
            int rh = GridView.FocusedRowHandle;
            return rh < 0 ? null : GridView.GetDataRow(rh);
        }

        // Contract editors open NON-MODAL (user request): the list, other contracts and the rest
        // of AutoCount stay usable while a contract is being edited. The list refreshes whenever
        // an editor closes; an already-open contract is ACTIVATED instead of opened twice (two
        // editors on the same contract would silently overwrite each other's save).
        private readonly System.Collections.Generic.Dictionary<long, zSCP2_Contract_Form> _openEditors =
            new System.Collections.Generic.Dictionary<long, zSCP2_Contract_Form>();

        // Virtual so the "Maintain Service Item" alias can open the Service Item editor instead.
        protected virtual void OnNew(object sender, EventArgs e)
        {
            zSCP2_Contract_Form f = new zSCP2_Contract_Form(_dbSetting);
            f.FormClosed += delegate { LoadGrid(); };
            f.Show(this);
        }

        // Virtual so the "Maintain Service Item" alias can open the Service Item editor instead of the
        // contract editor (the list is item-level; editing a row must edit THAT item).
        protected virtual void OnEdit(object sender, EventArgs e)
        {
            DataRow row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ContractKey"]);
            zSCP2_Contract_Form open;
            if (_openEditors.TryGetValue(key, out open) && !open.IsDisposed)
            {
                if (open.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                    open.WindowState = System.Windows.Forms.FormWindowState.Normal;
                open.Activate();
                return;
            }
            zSCP2_Contract_Form f = new zSCP2_Contract_Form(_dbSetting, key);
            _openEditors[key] = f;
            f.FormClosed += delegate { _openEditors.Remove(key); LoadGrid(); };
            f.Show(this);
        }

        // Virtual for the same reason: on the item list, Delete must remove only the selected ITEM,
        // not its whole contract and every sibling item.
        protected virtual void OnDelete(object sender, EventArgs e)
        {
            DataRow row = GetSelectedRow();
            if (row == null) return;
            long key = Convert.ToInt64(row["ContractKey"]);
            string code = row["ContractNo"].ToString();
            if (XtraMessageBox.Show("Delete contract '" + code + "' and all its machines / meter config?\r\n" +
                "ALL meter reading history of its machines is permanently deleted too.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                // FK cascade removes zSCP2_Item -> zSCP2_ItemMeter automatically.
                _dbSetting.ExecuteNonQuery("DELETE FROM [dbo].[zSCP2_Contract] WHERE ContractKey=" + key);
                LoadGrid();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }
    }
}
