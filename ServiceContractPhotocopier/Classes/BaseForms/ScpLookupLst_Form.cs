using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using static VTACPluginBase.Classes.Helpers.GeneralHelper;

namespace ServiceContractPhotocopier.Classes.BaseForms
{
    /// <summary>
    /// Standardized list+inline-edit form for all zSCP_LK_* lookup tables — the SAME family look as
    /// Maintain Service Contract / Meter Type / Meter Multi Pricing:
    ///   - Native AutoCount green PanelHeader (hint hidden)
    ///   - Top toolbar of 86x50 icon buttons: New | Edit | Save | Cancel | Delete | Refresh | Exit (F2)
    ///   - Grid filling most of the form (columns: Code | Description | Inactive)
    ///   - Bottom edit panel (Code + Inactive checkbox on line 1, Description on line 2)
    ///   - Status bar at very bottom
    /// No separate popup editor dialog — editing is inline. F-keys still work (F5..F9, F2).
    /// </summary>
    public class ScpLookupLst_Form : XtraForm
    {
        protected GridControl GridCtl;
        protected GridView GridVw;
        protected SimpleButton BtnAdd, BtnEdit, BtnSave, BtnCancel, BtnDelete, BtnRefresh, BtnExit;
        protected AutoCount.Controls.PanelHeader PanelHeaderTop;
        protected PanelControl PanelEdit;
        protected PanelControl PanelToolbar;
        protected PanelControl PanelStatus;
        protected LabelControl LblCode, LblDesc, LblStatus;
        protected TextEdit TxtCode, TxtDesc;
        protected CheckEdit ChkInactive;

        protected DBSetting _dbSetting;
        private long _selectedKey = 0;
        private bool _isNewRow = false;
        private bool _editMode = false;

        protected virtual string TableName   { get { return "zSCP_LK_Unknown"; } }
        protected virtual string ViewName    { get { return null; } }
        protected virtual string KeyColumn   { get { return "Code"; } }
        protected virtual string KeyField    { get { return "Key"; } }
        protected virtual string FormCaption { get { return "Lookup"; } }
        protected virtual string StatusText  { get { return "Lookup"; } }

        public ScpLookupLst_Form() { InitBaseLayout(); }

        public ScpLookupLst_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += delegate { LoadData(); SetEditMode(false); };
        }

        private void InitBaseLayout()
        {
            this.Text = FormCaption;
            this.ClientSize = new Size(920, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(750, 540);

            // Native AutoCount green header (hint hidden), same as every other maintenance module.
            PanelHeaderTop = new AutoCount.Controls.PanelHeader();
            PanelHeaderTop.Dock = DockStyle.Top;
            PanelHeaderTop.Header = FormCaption;
            PanelHeaderTop.Hint = "";
            PanelHeaderTop.Size = new Size(920, 56);

            // Top toolbar — 86x50 icon buttons, identical to the Maintain Service Contract list.
            PanelToolbar = new PanelControl();
            PanelToolbar.Dock = DockStyle.Top;
            PanelToolbar.Size = new Size(920, 62);

            BtnAdd     = MakeToolBtn("New",       8, delegate { OnAdd(); });
            BtnEdit    = MakeToolBtn("Edit",     98, delegate { OnEditBtn(); });
            BtnSave    = MakeToolBtn("Save",    188, delegate { OnSave(); });
            BtnCancel  = MakeToolBtn("Cancel",  278, delegate { OnCancelEdit(); });
            BtnDelete  = MakeToolBtn("Delete",  368, delegate { OnDelete(); });
            BtnRefresh = MakeToolBtn("Refresh", 458, delegate { LoadData(); });
            BtnRefresh.Width = 92;
            BtnExit    = MakeToolBtn("Exit (F2)", 556, delegate { this.Close(); });
            BtnExit.Width = 92;
            PanelToolbar.Controls.Add(BtnAdd);
            PanelToolbar.Controls.Add(BtnEdit);
            PanelToolbar.Controls.Add(BtnSave);
            PanelToolbar.Controls.Add(BtnCancel);
            PanelToolbar.Controls.Add(BtnDelete);
            PanelToolbar.Controls.Add(BtnRefresh);
            PanelToolbar.Controls.Add(BtnExit);
            ApplyToolbarIcons();

            // Grid — fills center
            GridCtl = new GridControl();
            GridCtl.Location = new Point(14, 128);
            GridCtl.Size = new Size(892, 366);
            GridCtl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            GridVw = new GridView(GridCtl);
            GridVw.OptionsBehavior.Editable = false;
            GridVw.OptionsView.ShowGroupPanel = false;
            GridVw.OptionsView.ColumnAutoWidth = true;
            GridCtl.MainView = GridVw;
            GridCtl.ViewCollection.Add(GridVw);
            // Columns set by subclass or default
            var colCode = new GridColumn(); colCode.FieldName = "Col_Code"; colCode.Caption = "Code"; colCode.Visible = true; colCode.Width = 150; colCode.VisibleIndex = 0;
            var colDesc = new GridColumn(); colDesc.FieldName = "Col_Desc"; colDesc.Caption = "Description"; colDesc.Visible = true; colDesc.Width = 500; colDesc.VisibleIndex = 1;
            var colInac = new GridColumn(); colInac.FieldName = "Inactive"; colInac.Caption = "Inactive"; colInac.Visible = true; colInac.Width = 70; colInac.VisibleIndex = 2;
            GridVw.Columns.Add(colCode);
            GridVw.Columns.Add(colDesc);
            GridVw.Columns.Add(colInac);
            GridVw.FocusedRowChanged += delegate { OnGridRowChanged(); };

            // Bottom edit panel
            PanelEdit = new PanelControl();
            PanelEdit.Location = new Point(14, 502);
            PanelEdit.Size = new Size(892, 65);
            PanelEdit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PanelEdit.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            LblCode = new LabelControl();
            LblCode.Text = "Code :";
            LblCode.Location = new Point(6, 8);

            TxtCode = new TextEdit();
            TxtCode.Location = new Point(140, 5);
            TxtCode.Width = 180;

            ChkInactive = new CheckEdit();
            ChkInactive.Properties.Caption = "Inactive";
            ChkInactive.Location = new Point(340, 5);

            LblDesc = new LabelControl();
            LblDesc.Text = "Description :";
            LblDesc.Location = new Point(6, 36);

            TxtDesc = new TextEdit();
            TxtDesc.Location = new Point(140, 33);
            TxtDesc.Width = 740;
            TxtDesc.Anchor = AnchorStyles.Left | AnchorStyles.Right;

            PanelEdit.Controls.Add(LblCode);
            PanelEdit.Controls.Add(TxtCode);
            PanelEdit.Controls.Add(ChkInactive);
            PanelEdit.Controls.Add(LblDesc);
            PanelEdit.Controls.Add(TxtDesc);

            // Status bar
            PanelStatus = new PanelControl();
            PanelStatus.Dock = DockStyle.Bottom;
            PanelStatus.Height = 24;
            LblStatus = new LabelControl();
            LblStatus.Text = StatusText;
            LblStatus.Location = new Point(6, 5);
            PanelStatus.Controls.Add(LblStatus);

            this.Controls.Add(GridCtl);
            this.Controls.Add(PanelEdit);
            this.Controls.Add(PanelToolbar);
            this.Controls.Add(PanelHeaderTop);
            this.Controls.Add(PanelStatus);

            // F-key shortcuts
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
        }

        private SimpleButton MakeToolBtn(string text, int x, EventHandler onClick)
        {
            var b = new SimpleButton();
            b.Text = text;
            b.Location = new Point(x, 6);
            b.Width = 86;
            b.Height = 50;
            b.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            b.Click += onClick;
            return b;
        }

        // Same AutoCount large icons as the Maintain Service Contract / Meter Type toolbars.
        private void ApplyToolbarIcons()
        {
            try
            {
                float dpi = 96f;
                try { dpi = this.DeviceDpi; } catch { }
                AutoCount.Images.IAutoCountImage img =
                    AutoCount.Images.ImageHelper.GetAutoCountImage(new SizeF(dpi, dpi));
                BtnAdd.ImageOptions.Image = img.GetLargeImage_New();
                BtnEdit.ImageOptions.Image = img.GetLargeImage_Edit();
                BtnSave.ImageOptions.Image = img.GetLargeImage_Save();
                BtnCancel.ImageOptions.Image = img.GetLargeImage_Cancel();
                BtnDelete.ImageOptions.Image = img.GetLargeImage_Delete2();
                BtnRefresh.ImageOptions.Image = img.GetLargeImage_Refresh();
                BtnExit.ImageOptions.Image = img.GetLargeImage_Close();
            }
            catch { }   // icons are cosmetic — never block the form
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2) { this.Close(); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { OnAdd(); e.Handled = true; }
            else if (e.KeyCode == Keys.F6) { OnEditBtn(); e.Handled = true; }
            else if (e.KeyCode == Keys.F7) { OnSave(); e.Handled = true; }
            else if (e.KeyCode == Keys.F8) { OnCancelEdit(); e.Handled = true; }
            else if (e.KeyCode == Keys.F9) { OnDelete(); e.Handled = true; }
        }

        protected virtual void LoadData()
        {
            if (_dbSetting == null) return;
            try
            {
                string source = string.IsNullOrEmpty(ViewName) ? TableName : ViewName;
                string sql = "SELECT * FROM [dbo].[" + source + "] ORDER BY [" + KeyColumn + "]";
                var dt = _dbSetting.GetDataTable(sql, false);
                if (dt == null) dt = new DataTable();

                // Point grid columns at the actual table columns (idempotent — look up by FieldName or Caption).
                var colCode = FindGridColumn("Col_Code", "Code");
                var colDesc = FindGridColumn("Col_Desc", "Description");
                if (colCode != null && dt.Columns.Contains(KeyColumn))
                {
                    colCode.FieldName = KeyColumn;
                    colCode.Caption  = KeyColumn.Replace("Code", " Code").Replace("  ", " ").Trim();
                }
                if (colDesc != null && dt.Columns.Contains("Description"))
                {
                    colDesc.FieldName = "Description";
                }
                GridCtl.DataSource = dt;
                if (LblCode   != null) LblCode.Text   = KeyColumn.Replace("Code", " Code").Replace("  ", " ").Trim() + " :";
                if (LblStatus != null) LblStatus.Text = StatusText;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Failed to load " + TableName + ":\r\n" + ex.Message,
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private GridColumn FindGridColumn(params string[] fieldNamesOrCaptions)
        {
            if (GridVw == null) return null;
            foreach (var key in fieldNamesOrCaptions)
            {
                foreach (GridColumn col in GridVw.Columns)
                {
                    if (col == null) continue;
                    if (string.Equals(col.FieldName, key, StringComparison.OrdinalIgnoreCase)) return col;
                    if (string.Equals(col.Caption,   key, StringComparison.OrdinalIgnoreCase)) return col;
                    if (string.Equals(col.Name,      key, StringComparison.OrdinalIgnoreCase)) return col;
                }
            }
            return null;
        }

        private void OnGridRowChanged()
        {
            int rh = GridVw.FocusedRowHandle;
            if (rh < 0) { ClearEdit(); return; }
            var row = GridVw.GetDataRow(rh);
            if (row == null) { ClearEdit(); return; }
            _selectedKey = row.Table.Columns.Contains(KeyField)
                ? Convert.ToInt64(row[KeyField])
                : 0;
            _isNewRow = false;
            TxtCode.Text = row.Table.Columns.Contains(KeyColumn) ? row[KeyColumn].ToString() : "";
            TxtDesc.Text = row.Table.Columns.Contains("Description") && row["Description"] != DBNull.Value ? row["Description"].ToString() : "";
            ChkInactive.Checked = row.Table.Columns.Contains("Inactive") && row["Inactive"] != DBNull.Value && row["Inactive"].ToString() == "Y";
        }

        private void ClearEdit()
        {
            _selectedKey = 0;
            _isNewRow = false;
            TxtCode.Text = "";
            TxtDesc.Text = "";
            ChkInactive.Checked = false;
        }

        private void SetEditMode(bool editing)
        {
            _editMode = editing;
            TxtCode.Properties.ReadOnly = !editing || !_isNewRow;
            TxtDesc.Properties.ReadOnly = !editing;
            ChkInactive.Properties.ReadOnly = !editing;
            BtnSave.Enabled = editing;
            BtnCancel.Enabled = editing;
            BtnAdd.Enabled = !editing;
            BtnEdit.Enabled = !editing;
            BtnDelete.Enabled = !editing;
        }

        private void OnAdd()
        {
            ClearEdit();
            _isNewRow = true;
            SetEditMode(true);
            TxtCode.Focus();
        }

        private void OnEditBtn()
        {
            if (string.IsNullOrEmpty(TxtCode.Text)) return;
            _isNewRow = false;
            SetEditMode(true);
            TxtDesc.Focus();
        }

        private void OnCancelEdit()
        {
            OnGridRowChanged();
            SetEditMode(false);
        }

        private void OnSave()
        {
            if (string.IsNullOrWhiteSpace(TxtCode.Text))
            {
                XtraMessageBox.Show("Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string code = SQLString(TxtCode.Text.Trim());
                string desc = SQLString(TxtDesc.Text ?? "");
                string inac = ChkInactive.Checked ? "Y" : "N";

                if (_isNewRow)
                {
                    _dbSetting.ExecuteNonQuery(
                        "INSERT INTO [dbo].[" + TableName + "] ([" + KeyColumn + "], [Description], [Inactive]) " +
                        "VALUES (N'" + code + "', N'" + desc + "', '" + inac + "')");
                }
                else
                {
                    _dbSetting.ExecuteNonQuery(
                        "UPDATE [dbo].[" + TableName + "] SET [Description] = N'" + desc + "', " +
                        "[Inactive] = '" + inac + "', [LastModified] = GETDATE() " +
                        "WHERE [" + KeyColumn + "] = N'" + code + "'");
                }

                SetEditMode(false);
                LoadData();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected virtual void OnDelete()
        {
            if (string.IsNullOrEmpty(TxtCode.Text)) return;
            if (XtraMessageBox.Show("Delete '" + TxtCode.Text + "'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                _dbSetting.ExecuteNonQuery(
                    "DELETE FROM [dbo].[" + TableName + "] WHERE [" + KeyColumn + "] = N'" + SQLString(TxtCode.Text.Trim()) + "'");
                ClearEdit();
                SetEditMode(false);
                LoadData();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Delete failed:\r\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected virtual void OpenEditor(DataRow existingRow) { }
    }
}
