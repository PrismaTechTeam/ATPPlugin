using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Strategy Maintenance — the marketing/billing strategy TEMPLATE library (zSCP2_Strategy +
    /// zSCP2_StrategyRule). A strategy = code + description + a composed list of rule lines (built with the
    /// shared <see cref="ServiceContractPhotocopier.Classes.CommonForms.StrategyRulesEditorControl"/>).
    /// These are reusable TEMPLATES; a contract seeds its own editable copy from one. Master mode has no
    /// per-rule service-item binding (templates have no items).
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Strategy Maintenance",
    ParentMenuCaption = "General Setup", MenuOrder = 150, ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_STRATEGY,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_STRATEGY)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class StrategyLst_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private long _selectedKey = 0;
        private bool _isNewRow = false;

        /// <summary>The code saved by the most recent Save — lets callers (contract "+") preselect it.</summary>
        public string SavedCode = "";

        public StrategyLst_Form() { InitializeComponent(); }
        public StrategyLst_Form(UserSession userSession) : this()
        { if (userSession != null) _dbSetting = userSession.DBSetting; this.Load += new EventHandler(OnFormLoad); }
        public StrategyLst_Form(DBSetting dbSetting) : this()
        { _dbSetting = dbSetting; this.Load += new EventHandler(OnFormLoad); }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_dbSetting == null) return;
            ApplyButtonIcons();
            RulesEditor.SetServiceItems(null);   // master mode: templates have no service items
            LoadGrid();
            GridViewST.FocusedRowChanged += delegate { PopulateDetail(); };
            SetReadOnly(true);
            SetEditMode(false);
            PopulateDetail();
        }

        private void ApplyButtonIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new System.Drawing.SizeF(dpi, dpi));
                BtnNew.ImageOptions.Image = img.GetLargeImage_New();
                BtnEdit.ImageOptions.Image = img.GetLargeImage_Edit();
                BtnCopyNew.ImageOptions.Image = img.GetLargeImage_CopyTo2();
                BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                BtnCancel.ImageOptions.Image = img.GetLargeImage_Cancel();
                BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete2();
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

        private void SetReadOnly(bool ro)
        {
            TxtCode.Properties.ReadOnly = true;
            TxtDesc.Properties.ReadOnly = ro;
            TxtRemark.Properties.ReadOnly = ro;
            ChkInactive.Properties.ReadOnly = ro;
            RulesEditor.SetEditable(!ro);
        }

        private void SetEditMode(bool editing)
        {
            BtnNew.Enabled = !editing;
            BtnEdit.Enabled = !editing;
            BtnCopyNew.Enabled = !editing;
            BtnDelete.Enabled = !editing;
            BtnRefresh.Enabled = !editing;
            BtnSave.Enabled = editing;
            BtnCancel.Enabled = editing;
            GridST.Enabled = !editing;
        }

        private void LoadGrid()
        {
            try
            {
                DataTable dt = _dbSetting.GetDataTable(
                    "SELECT s.StrategyKey, s.StrategyCode, s.[Description], s.Remark, s.Inactive, " +
                    "(SELECT COUNT(*) FROM dbo.zSCP2_StrategyRule r WHERE r.StrategyKey = s.StrategyKey) AS RuleCount " +
                    "FROM dbo.zSCP2_Strategy s ORDER BY s.StrategyCode", false);
                dt.Columns.Add("Rules", typeof(string));
                foreach (DataRow r in dt.Rows)
                {
                    int rc = r["RuleCount"] == DBNull.Value ? 0 : Convert.ToInt32(r["RuleCount"]);
                    r["Rules"] = rc == 0 ? "(no rules)" : rc + " rule" + (rc == 1 ? "" : "s");
                }
                GridST.DataSource = dt;
                ConfigureGridColumns();
            }
            catch (Exception ex) { XtraMessageBox.Show("Load failed:\r\n" + ex.Message, "Error"); }
        }

        private void ConfigureGridColumns()
        {
            foreach (string hide in new string[] { "StrategyKey", "RuleCount" })
                if (GridViewST.Columns[hide] != null) GridViewST.Columns[hide].Visible = false;
            SetCap("StrategyCode", "Strategy Code", 160, 0);
            SetCap("Description", "Description", 320, 1);
            SetCap("Rules", "Rules", 120, 2);
            SetCap("Remark", "Remark", 220, 3);
            SetCap("Inactive", "Inactive", 60, 4);
        }

        private void SetCap(string field, string caption, int width, int idx)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = GridViewST.Columns[field];
            if (c == null) return;
            c.Caption = caption; c.Width = width; c.Visible = true; c.VisibleIndex = idx;
        }

        // Load a strategy's master rule lines into a StrategyRule list (for the builder control).
        private List<StrategyRule> LoadMasterRules(long strategyKey)
        {
            List<StrategyRule> list = new List<StrategyRule>();
            if (strategyKey <= 0) return list;
            try
            {
                DataTable dt = _dbSetting.GetDataTable(
                    "SELECT Seq, RuleKind, Scope, TargetAmount, PartialPct, FreeMonths, CommitAmount, " +
                    "FocCopies, RebatePct, NetBilling, LimitScope, LimitQty " +
                    "FROM dbo.zSCP2_StrategyRule WHERE StrategyKey = " + strategyKey + " ORDER BY Seq", false);
                foreach (DataRow r in dt.Rows)
                {
                    StrategyRule x = new StrategyRule();
                    x.Seq = r["Seq"] == DBNull.Value ? 0 : Convert.ToInt32(r["Seq"]);
                    x.Kind = V(r, "RuleKind");
                    x.Scope = V(r, "Scope");
                    x.TargetAmount = Dec(r, "TargetAmount");
                    x.PartialPct = Dec(r, "PartialPct");
                    x.FreeMonths = r["FreeMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["FreeMonths"]);
                    x.CommitAmount = Dec(r, "CommitAmount");
                    x.FocCopies = Dec(r, "FocCopies");
                    x.RebatePct = Dec(r, "RebatePct");
                    x.NetBilling = V(r, "NetBilling") == "Y";
                    x.LimitScope = V(r, "LimitScope") == "G" ? 'G' : 'S';
                    x.LimitQty = Dec(r, "LimitQty");
                    list.Add(x);
                }
            }
            catch (Exception ex) { XtraMessageBox.Show("Load rules failed:\r\n" + ex.Message, "Error"); }
            return list;
        }

        private void PopulateDetail()
        {
            int rh = GridViewST.FocusedRowHandle;
            DataRow row = rh < 0 ? null : GridViewST.GetDataRow(rh);
            if (row == null) { ClearDetail(); return; }
            _selectedKey = Convert.ToInt64(row["StrategyKey"]);
            _isNewRow = false;
            TxtCode.Text = V(row, "StrategyCode");
            TxtDesc.Text = V(row, "Description");
            TxtRemark.Text = V(row, "Remark");
            ChkInactive.Checked = V(row, "Inactive") == "Y";
            RulesEditor.LoadRules(LoadMasterRules(_selectedKey));
            SetReadOnly(true);
            SetEditMode(false);
        }

        private void ClearDetail()
        {
            _selectedKey = 0; _isNewRow = false;
            TxtCode.Text = ""; TxtDesc.Text = ""; TxtRemark.Text = "";
            ChkInactive.Checked = false;
            RulesEditor.LoadRules(new List<StrategyRule>());
            SetReadOnly(true);
        }

        private void OnNew(object sender, EventArgs e)
        {
            ClearDetail(); _isNewRow = true;
            SetReadOnly(false); TxtCode.Properties.ReadOnly = false;
            SetEditMode(true); TxtCode.Focus();
        }

        // Copy to New: header + all rules, minus the Strategy Code.
        private void OnCopyToNew(object sender, EventArgs e)
        {
            int rh = GridViewST.FocusedRowHandle;
            if (rh < 0) return;
            DataRow row = GridViewST.GetDataRow(rh);
            if (row == null) return;
            PopulateDetail();      // loads header + rules of the source into the editor
            _selectedKey = 0; _isNewRow = true;
            TxtCode.Text = "";
            SetReadOnly(false); TxtCode.Properties.ReadOnly = false;
            SetEditMode(true); TxtCode.Focus();
        }

        private void OnEdit(object sender, EventArgs e)
        { if (_selectedKey == 0) return; SetReadOnly(false); SetEditMode(true); TxtDesc.Focus(); }

        private void OnRefresh(object sender, EventArgs e) { LoadGrid(); PopulateDetail(); SetEditMode(false); }

        private void OnSave(object sender, EventArgs e)
        {
            string code = (TxtCode.Text ?? "").Trim();
            if (code.Length == 0)
            { XtraMessageBox.Show("Strategy Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            string err;
            if (!RulesEditor.ValidateRules(out err))
            { XtraMessageBox.Show(err, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            List<StrategyRule> rules = RulesEditor.GetRules();
            try
            {
                using (SqlConnection cn = new SqlConnection(_dbSetting.ConnectionString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction("SaveStrategy"))
                    {
                        try
                        {
                            long key = _selectedKey;
                            string by = ScpContractAudit.CurrentUser();
                            if (_isNewRow || key == 0)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO dbo.zSCP2_Strategy (StrategyCode, [Description], StrategyType, Remark, Inactive, Created, CreatedBy) " +
                                    "VALUES (@code, @desc, '', @rm, @ia, GETDATE(), @by); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@code", code);
                                    cmd.Parameters.AddWithValue("@desc", TxtDesc.Text ?? "");
                                    cmd.Parameters.AddWithValue("@rm", TxtRemark.Text ?? "");
                                    cmd.Parameters.AddWithValue("@ia", ChkInactive.Checked ? "Y" : "N");
                                    cmd.Parameters.AddWithValue("@by", by);
                                    key = Convert.ToInt64(cmd.ExecuteScalar());
                                }
                            }
                            else
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "UPDATE dbo.zSCP2_Strategy SET StrategyCode=@code, [Description]=@desc, Remark=@rm, " +
                                    "Inactive=@ia, Modified=GETDATE(), ModifiedBy=@by, LastModified=GETDATE() WHERE StrategyKey=@key", cn, tx))
                                {
                                    cmd.Parameters.AddWithValue("@code", code);
                                    cmd.Parameters.AddWithValue("@desc", TxtDesc.Text ?? "");
                                    cmd.Parameters.AddWithValue("@rm", TxtRemark.Text ?? "");
                                    cmd.Parameters.AddWithValue("@ia", ChkInactive.Checked ? "Y" : "N");
                                    cmd.Parameters.AddWithValue("@by", by);
                                    cmd.Parameters.AddWithValue("@key", key);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand del = new SqlCommand("DELETE FROM dbo.zSCP2_StrategyRule WHERE StrategyKey=@key", cn, tx))
                            { del.Parameters.AddWithValue("@key", key); del.ExecuteNonQuery(); }

                            foreach (StrategyRule r in rules)
                            {
                                using (SqlCommand ins = new SqlCommand(
                                    "INSERT INTO dbo.zSCP2_StrategyRule (StrategyKey, Seq, RuleKind, Scope, TargetAmount, PartialPct, " +
                                    "FreeMonths, CommitAmount, FocCopies, RebatePct, NetBilling, LimitScope, LimitQty) " +
                                    "VALUES (@key,@seq,@kind,@scope,@tgt,@pp,@fm,@cm,@fc,@rb,@net,@ls,@lq)", cn, tx))
                                {
                                    ins.Parameters.AddWithValue("@key", key);
                                    ins.Parameters.AddWithValue("@seq", r.Seq);
                                    ins.Parameters.AddWithValue("@kind", r.Kind);
                                    ins.Parameters.AddWithValue("@scope", r.Scope);
                                    ins.Parameters.AddWithValue("@tgt", r.TargetAmount);
                                    ins.Parameters.AddWithValue("@pp", r.PartialPct);
                                    ins.Parameters.AddWithValue("@fm", r.FreeMonths);
                                    ins.Parameters.AddWithValue("@cm", r.CommitAmount);
                                    ins.Parameters.AddWithValue("@fc", r.FocCopies);
                                    ins.Parameters.AddWithValue("@rb", r.RebatePct);
                                    ins.Parameters.AddWithValue("@net", r.NetBilling ? "Y" : "N");
                                    ins.Parameters.AddWithValue("@ls", r.LimitScope == 'G' ? "G" : "S");
                                    ins.Parameters.AddWithValue("@lq", r.LimitQty);
                                    ins.ExecuteNonQuery();
                                }
                            }
                            tx.Commit();
                            _selectedKey = key;
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                SavedCode = code;
                _isNewRow = false;
                LoadGrid();
                SelectByKey(_selectedKey);
                SetReadOnly(true);
                SetEditMode(false);
            }
            catch (Exception ex) { XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error"); }
        }

        private void SelectByKey(long key)
        {
            for (int i = 0; i < GridViewST.RowCount; i++)
            {
                DataRow r = GridViewST.GetDataRow(i);
                if (r != null && Convert.ToInt64(r["StrategyKey"]) == key)
                { GridViewST.FocusedRowHandle = i; return; }
            }
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selectedKey == 0) return;
            string code = TxtCode.Text.Trim();
            try
            {
                object used = _dbSetting.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.zSCP2_Contract WHERE StrategyCode = N'" + SQLString(code) + "'");
                int cnt = used == null || used == DBNull.Value ? 0 : Convert.ToInt32(used);
                if (cnt > 0)
                {
                    XtraMessageBox.Show("Cannot delete: strategy '" + code + "' is attached to " + cnt +
                        " contract(s). Detach it from those contracts first (or mark the strategy Inactive).",
                        "Delete Strategy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (XtraMessageBox.Show("Delete strategy '" + code + "' and all its rules?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                // Rule lines cascade-delete via FK (ON DELETE CASCADE).
                _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Strategy WHERE StrategyKey=" + _selectedKey);
                LoadGrid();
                PopulateDetail();
            }
            catch (Exception ex) { XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error"); }
        }

        private void OnCancel(object sender, EventArgs e) { PopulateDetail(); SetEditMode(false); }
        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private static string V(DataRow r, string c) { return r[c] == DBNull.Value ? "" : r[c].ToString(); }
        private static decimal Dec(DataRow r, string c) { return r[c] == DBNull.Value ? 0m : Convert.ToDecimal(r[c]); }
    }
}
