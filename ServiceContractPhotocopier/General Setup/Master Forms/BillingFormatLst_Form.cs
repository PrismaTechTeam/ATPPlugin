using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Authentication;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// The invoice layouts this company bills under: how many invoices a contract produces, and how
    /// its lines are grouped. Eleven are seeded from the customer's own July-2026 invoices as
    /// starting points; the names are theirs to change and new ones are theirs to add.
    ///
    /// <para>Same shape as Meter Multi Pricing next door — green header, New / Edit / Copy to New /
    /// Save / Cancel / Delete / Refresh / Exit toolbar, list on top, detail below, no popup editor.
    /// Where Multi Pricing puts its rate ladder, this puts a sample invoice.</para>
    ///
    /// <para>The sample is why the three questions get a screen rather than three combo boxes on the
    /// contract: "rental per model" and "rental one line" are indistinguishable as words and obvious
    /// as invoices. It redraws on every click, folded through <see cref="ScpInvoiceLayout"/> — the
    /// same call the engine makes — so it cannot promise what Generate will not do.</para>
    /// </summary>
    [AutoCount.PlugIn.MenuItem("Billing Format",
    ParentMenuCaption = "General Setup", MenuOrder = 135, ParentMenuOrder = 600,
    OpenAccessRight = AccessRightsConsts.CMD_OPEN_SCP_SETUP_BILLING_FORMAT,
    VisibleAccessRight = AccessRightsConsts.CMD_SHOW_SCP_SETUP_BILLING_FORMAT)]
    [AutoCount.Application.SingleInstanceThreadForm(System.Windows.Forms.FormWindowState.Maximized, true)]
    public partial class BillingFormatLst_Form : XtraForm
    {
        private DBSetting _dbSetting;
        private DataTable _dt;
        private DataTable _dtPreview;
        private string _selectedCode = "";
        private bool _isNew;
        private bool _editMode;
        private bool _loading;
        private bool _dirty;

        public BillingFormatLst_Form() { InitializeComponent(); }

        public BillingFormatLst_Form(UserSession userSession) : this()
        {
            if (userSession != null) _dbSetting = userSession.DBSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        public BillingFormatLst_Form(DBSetting dbSetting) : this()
        {
            _dbSetting = dbSetting;
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            ApplyButtonIcons();
            if (_dbSetting == null) return;
            BuildColumns();
            GridViewFormats.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(OnFocusedRowChanged);
            RgInvoices.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            RgRental.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            RgMeter.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            TxtName.EditValueChanged += new EventHandler(OnFieldEdited);
            TxtRemark.EditValueChanged += new EventHandler(OnFieldEdited);
            ChkInactive.CheckedChanged += new EventHandler(OnFieldEdited);
            this.KeyDown += new KeyEventHandler(OnFormKeyDown);
            LoadData("");
            SetEditMode(false);
        }

        /// <summary>The native AutoCount toolbar glyphs, same call every sibling makes. Cosmetic, so
        /// a failure here must never stop the form opening.
        /// <para>The size argument is DPI, not AutoScaleDimensions — passing (7,15) by mistake asks
        /// for icons about 1% of full size.</para></summary>
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
                    System.Drawing.Image door =
                        DevExpress.Images.ImageResourceCache.Default.GetImage("images/xaf/action_exit_32x32.png");
                    BtnExit.ImageOptions.Image = door ?? img.GetLargeImage_Close();
                }
                catch { BtnExit.ImageOptions.Image = img.GetLargeImage_Close(); }
            }
            catch { }
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 && !_editMode) { this.Close(); e.Handled = true; }
        }

        private void OnFieldEdited(object sender, EventArgs e)
        {
            if (!_loading && _editMode) _dirty = true;
        }

        /// <summary>Never lose a half-typed format to the window's X — the same rule the contract and
        /// service-item editors follow.</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_editMode && _dirty &&
                XtraMessageBox.Show(this, "You have unsaved changes. Discard them and close?",
                    "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            { e.Cancel = true; return; }
            base.OnFormClosing(e);
        }

        private void BuildColumns()
        {
            GridViewFormats.OptionsBehavior.AutoPopulateColumns = false;
            GridViewFormats.Columns.Clear();
            AddCol(GridViewFormats, "FormatCode", "Format Code", 150);
            AddCol(GridViewFormats, "FormatName", "Name", 300);
            AddCol(GridViewFormats, "Invoices", "Invoices", 150);
            AddCol(GridViewFormats, "Rental", "Rental lines", 130);
            AddCol(GridViewFormats, "Meters", "BK & CL lines", 135);
            AddCol(GridViewFormats, "UsedBy", "Contracts", 80);
            AddCol(GridViewFormats, "Inactive", "Inactive", 70);

            BuildPreviewColumns();
        }

        private static DevExpress.XtraGrid.Columns.GridColumn AddCol(
            DevExpress.XtraGrid.Views.Grid.GridView gv, string field, string caption, int width)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = gv.Columns.AddVisible(field);
            c.Caption = caption;
            c.Width = width;
            return c;
        }

        private static void RightAlign(DevExpress.XtraGrid.Columns.GridColumn c)
        {
            c.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            c.AppearanceCell.Options.UseTextOptions = true;
            c.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            c.AppearanceHeader.Options.UseTextOptions = true;
        }

        // ===== list =====

        private void LoadData(string selectCode)
        {
            try
            {
                _dt = _dbSetting.GetDataTable(
                    "SELECT f.FormatCode, f.FormatName, f.InvoiceSplit, f.RentalLineMode, f.MeterLineMode, " +
                    "ISNULL(f.Remark,'') AS Remark, f.Inactive, " +
                    "(SELECT COUNT(*) FROM dbo.zSCP2_Contract c WHERE c.BillingFormatCode = f.FormatCode) AS UsedBy " +
                    "FROM dbo.zSCP2_BillingFormat f ORDER BY f.FormatCode", false);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Could not load billing formats:\r\n" + ex.Message,
                    "Billing Format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // The three answers in words, so the codes never have to be decoded by eye.
            _dt.Columns.Add("Invoices", typeof(string));
            _dt.Columns.Add("Rental", typeof(string));
            _dt.Columns.Add("Meters", typeof(string));
            foreach (DataRow r in _dt.Rows)
            {
                r["Invoices"] = ScpBillingFormat.DescribeSplit(Convert.ToString(r["InvoiceSplit"]));
                r["Rental"] = ScpBillingFormat.DescribeLineMode(FirstChar(r["RentalLineMode"], 'A'));
                r["Meters"] = ScpBillingFormat.DescribeLineMode(FirstChar(r["MeterLineMode"], 'S'));
            }
            GridFormats.DataSource = _dt;

            int pick = 0;
            if (!string.IsNullOrEmpty(selectCode))
                for (int i = 0; i < GridViewFormats.RowCount; i++)
                    if (string.Equals(Convert.ToString(GridViewFormats.GetRowCellValue(i, "FormatCode")),
                                      selectCode, StringComparison.OrdinalIgnoreCase))
                    { pick = i; break; }
            if (GridViewFormats.RowCount > 0) GridViewFormats.FocusedRowHandle = pick;
            ShowFocusedRow();
        }

        private static char FirstChar(object o, char fallback)
        {
            string s = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            return s.Length == 0 ? fallback : char.ToUpperInvariant(s[0]);
        }

        private void OnFocusedRowChanged(object sender,
            DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (_editMode) return;
            ShowFocusedRow();
        }

        /// <summary>Put a stored code into a radio group, falling back when it matches no option.
        /// <para>DevExpress compares item values with <c>Equals</c> — ordinal, case-sensitive, no
        /// trim — so a stray <c>'one'</c> or a padded <c>nchar</c> selects nothing, leaves the group
        /// blank, and would then be written straight back to the database on the next save.</para></summary>
        private static void SetRadio(RadioGroup rg, string value, string fallback)
        {
            string v = (value ?? "").Trim().ToUpperInvariant();
            if (rg.Properties.Items.GetItemIndexByValue(v) < 0) v = fallback;
            rg.EditValue = v;
        }

        /// <summary>Read back from the selected ITEM, never from EditValue, so only one of the
        /// offered values can ever reach the database.</summary>
        private static string ReadRadio(RadioGroup rg, string fallback)
        {
            int i = rg.SelectedIndex;
            return i < 0 ? fallback : Convert.ToString(rg.Properties.Items[i].Value);
        }

        private void ShowFocusedRow()
        {
            int rh = GridViewFormats.FocusedRowHandle;
            _loading = true;
            try
            {
                if (rh < 0)
                {
                    _selectedCode = "";
                    TxtCode.Text = ""; TxtName.Text = ""; TxtRemark.Text = "";
                    ChkInactive.Checked = false;
                    SetRadio(RgInvoices, ScpBillingFormat.SPLIT_ONE, ScpBillingFormat.SPLIT_ONE);
                    SetRadio(RgRental, "A", "A");
                    SetRadio(RgMeter, "S", "S");
                }
                else
                {
                    _selectedCode = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "FormatCode"));
                    TxtCode.Text = _selectedCode;
                    TxtName.Text = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "FormatName"));
                    TxtRemark.Text = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "Remark"));
                    ChkInactive.Checked = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "Inactive")) == "Y";
                    SetRadio(RgInvoices, Convert.ToString(GridViewFormats.GetRowCellValue(rh, "InvoiceSplit")),
                             ScpBillingFormat.SPLIT_ONE);
                    SetRadio(RgRental, FirstChar(GridViewFormats.GetRowCellValue(rh, "RentalLineMode"), 'A').ToString(), "A");
                    SetRadio(RgMeter, FirstChar(GridViewFormats.GetRowCellValue(rh, "MeterLineMode"), 'S').ToString(), "S");
                }
            }
            finally { _loading = false; }
            RefreshPreview();
        }

        /// <summary>ReadOnly on a RadioGroup does block input, but its only visual cue is the default
        /// grey background — which the transparent Appearance here overrides. Grey the text instead,
        /// or the group looks live and silently swallows clicks.</summary>
        private static void SetRadioReadOnly(RadioGroup rg, bool ro)
        {
            rg.Properties.ReadOnly = ro;
            rg.Properties.Appearance.ForeColor = ro
                ? System.Drawing.SystemColors.GrayText
                : System.Drawing.Color.Empty;
        }

        private void SetEditMode(bool on)
        {
            _editMode = on;
            if (!on) _dirty = false;
            GridFormats.Enabled = !on;
            TxtCode.Properties.ReadOnly = !on || !_isNew;   // the code is what contracts point at
            TxtName.Properties.ReadOnly = !on;
            TxtRemark.Properties.ReadOnly = !on;
            ChkInactive.Properties.ReadOnly = !on;
            SetRadioReadOnly(RgInvoices, !on);
            SetRadioReadOnly(RgRental, !on);
            SetRadioReadOnly(RgMeter, !on);
            BtnNew.Enabled = !on;
            BtnEdit.Enabled = !on && _selectedCode.Length > 0;
            BtnCopyNew.Enabled = !on && _selectedCode.Length > 0;
            BtnDelete.Enabled = !on && _selectedCode.Length > 0;
            BtnRefresh.Enabled = !on;
            BtnExit.Enabled = !on;
            BtnSave.Enabled = on;
            BtnCancel.Enabled = on;
            LblStatus.Text = on
                ? (_isNew ? "New billing format — Save to keep it."
                          : "Editing " + _selectedCode + " — Save to keep the change.")
                : "Billing Format";
        }

        // ===== toolbar =====

        private void OnNew(object sender, EventArgs e)
        {
            _isNew = true;
            _loading = true;
            try
            {
                _selectedCode = "";
                TxtCode.Text = ""; TxtName.Text = ""; TxtRemark.Text = "";
                ChkInactive.Checked = false;
                SetRadio(RgInvoices, ScpBillingFormat.SPLIT_ONE, ScpBillingFormat.SPLIT_ONE);
                SetRadio(RgRental, "A", "A");
                SetRadio(RgMeter, "S", "S");
            }
            finally { _loading = false; }
            RefreshPreview();
            SetEditMode(true);
            TxtCode.Focus();
        }

        private void OnEdit(object sender, EventArgs e)
        {
            if (_selectedCode.Length == 0) return;
            _isNew = false;
            SetEditMode(true);
            TxtName.Focus();
        }

        // Most new layouts are a small change to one that already exists.
        private void OnCopyToNew(object sender, EventArgs e)
        {
            if (_selectedCode.Length == 0) return;
            _isNew = true;
            _loading = true;
            try
            {
                TxtCode.Text = "";
                TxtName.Text = Truncate(TxtName.Text + " (copy)", 100);
            }
            finally { _loading = false; }
            SetEditMode(true);
            TxtCode.Focus();
        }

        private static string Truncate(string s, int n)
        { return s != null && s.Length > n ? s.Substring(0, n) : s; }

        private void OnSave(object sender, EventArgs e)
        {
            string code = TxtCode.Text.Trim().ToUpperInvariant();
            string name = TxtName.Text.Trim();
            if (code.Length == 0) { Warn("Give the format a code — contracts point at it.", TxtCode); return; }
            if (name.Length == 0) { Warn("Give the format a name people will recognise.", TxtName); return; }

            try
            {
                if (_isNew)
                {
                    object dup = _dbSetting.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_BillingFormat " +
                        "WHERE FormatCode = " + Q(code));
                    if (dup != null && dup != DBNull.Value && Convert.ToInt32(dup) > 0)
                    { Warn("A format with code '" + code + "' already exists.", TxtCode); return; }

                    _dbSetting.ExecuteNonQuery(
                        "INSERT INTO dbo.zSCP2_BillingFormat " +
                        "(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark, Inactive) " +
                        "VALUES (" + Q(code) + "," + Q(name) + "," + Q(CurrentSplit()) + "," +
                        Q(CurrentRental()) + "," + Q(CurrentMeter()) + "," + Q(TxtRemark.Text.Trim()) +
                        ",'" + (ChkInactive.Checked ? 'Y' : 'N') + "')");
                }
                else
                {
                    _dbSetting.ExecuteNonQuery(
                        "UPDATE dbo.zSCP2_BillingFormat SET FormatName=" + Q(name) +
                        ", InvoiceSplit=" + Q(CurrentSplit()) +
                        ", RentalLineMode=" + Q(CurrentRental()) +
                        ", MeterLineMode=" + Q(CurrentMeter()) +
                        ", Remark=" + Q(TxtRemark.Text.Trim()) +
                        ", Inactive='" + (ChkInactive.Checked ? 'Y' : 'N') + "'" +
                        ", LastModified=GETDATE() WHERE FormatCode=" + Q(code));

                    // Contracts keep their own copy of the three answers, so an edit here has to be
                    // pushed to them on purpose — it changes how those customers bill next month.
                    int used = ScpBillingFormat.UsedByContractCount(_dbSetting, code);
                    if (used > 0 && XtraMessageBox.Show(this,
                            used + " contract(s) use this format.\r\n\r\n" +
                            "Apply the change to them as well?\r\n" +
                            "No leaves them billing the way they do now.",
                            "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ScpBillingFormat f = ScpBillingFormat.Load(_dbSetting, code);
                        if (f != null)
                            _dbSetting.ExecuteNonQuery(
                                "UPDATE dbo.zSCP2_Contract SET BillingMode='" + f.BillingMode +
                                "', RentalSeparateInvoice='" + (f.RentalSeparateInvoice ? 'Y' : 'N') +
                                "', RentalLineMode='" + f.RentalLineMode +
                                "', MeterLineMode='" + f.MeterLineMode +
                                "' WHERE BillingFormatCode=" + Q(code));
                    }
                }
                _isNew = false;
                SetEditMode(false);
                LoadData(code);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Billing Format",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            _isNew = false;
            SetEditMode(false);
            ShowFocusedRow();
        }

        private void OnDelete(object sender, EventArgs e)
        {
            if (_selectedCode.Length == 0) return;
            int used = ScpBillingFormat.UsedByContractCount(_dbSetting, _selectedCode);
            if (used > 0)
            {
                // Deleting would leave those contracts pointing at nothing. Retiring keeps their
                // history readable and stops the format being chosen again.
                if (XtraMessageBox.Show(this,
                        used + " contract(s) use '" + _selectedCode + "', so it cannot be deleted.\r\n\r\n" +
                        "Mark it Inactive instead? Those contracts keep billing as they do now, and the " +
                        "format stops appearing when someone picks one.",
                        "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                _dbSetting.ExecuteNonQuery("UPDATE dbo.zSCP2_BillingFormat SET Inactive='Y', " +
                    "LastModified=GETDATE() WHERE FormatCode = " + Q(_selectedCode));
                LoadData(_selectedCode);
                return;
            }
            if (XtraMessageBox.Show(this, "Delete billing format '" + _selectedCode + "'?",
                    "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _dbSetting.ExecuteNonQuery("DELETE FROM dbo.zSCP2_BillingFormat WHERE FormatCode = " + Q(_selectedCode));
            LoadData("");
        }

        private void OnRefresh(object sender, EventArgs e) { LoadData(_selectedCode); }

        private void OnExit(object sender, EventArgs e) { this.Close(); }

        private void Warn(string msg, Control focus)
        {
            XtraMessageBox.Show(msg, "Billing Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (focus != null) focus.Focus();
        }

        private static string Q(string s) { return "N'" + (s ?? "").Replace("'", "''") + "'"; }

        private string CurrentSplit() { return ReadRadio(RgInvoices, ScpBillingFormat.SPLIT_ONE); }
        private string CurrentRental() { return ReadRadio(RgRental, "A"); }
        private string CurrentMeter() { return ReadRadio(RgMeter, "S"); }

        private void OnChoiceChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            _dirty = _editMode;
            RefreshPreview();
        }

        // ===== sample invoice =====

        private void BuildPreviewColumns()
        {
            _dtPreview = new DataTable();
            _dtPreview.Columns.Add("InvKey", typeof(int));
            _dtPreview.Columns.Add("Invoice", typeof(string));
            _dtPreview.Columns.Add("LineNo", typeof(int));
            _dtPreview.Columns.Add("Code", typeof(string));
            _dtPreview.Columns.Add("Descr", typeof(string));
            _dtPreview.Columns.Add("Machines", typeof(string));
            _dtPreview.Columns.Add("Qty", typeof(decimal));
            _dtPreview.Columns.Add("UOM", typeof(string));
            _dtPreview.Columns.Add("UnitPrice", typeof(decimal));
            _dtPreview.Columns.Add("Amount", typeof(decimal));
            _dtPreview.Columns.Add("IsFoc", typeof(bool));

            GridViewPreview.OptionsBehavior.AutoPopulateColumns = false;
            GridViewPreview.Columns.Clear();

            // Grouped by invoice: DevExpress paints group rows in the skin's own style, gives the
            // per-invoice total for free, and lets the PM / PMS layouts collapse to one row each —
            // so the number of rows on screen IS the answer to "how many invoices?".
            DevExpress.XtraGrid.Columns.GridColumn cInv = GridViewPreview.Columns.AddVisible("Invoice");
            cInv.Visible = false;
            cInv.GroupIndex = 0;
            GridViewPreview.GroupFormat = "{1}";

            DevExpress.XtraGrid.Columns.GridColumn cNo = AddCol(GridViewPreview, "LineNo", "", 34);
            cNo.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cNo.DisplayFormat.FormatString = "0'.'";
            cNo.SortIndex = 1;
            RightAlign(cNo);
            AddCol(GridViewPreview, "Code", "Item", 74);
            AddCol(GridViewPreview, "Descr", "Description", 430);
            AddCol(GridViewPreview, "Machines", "Serial numbers", 230);
            DevExpress.XtraGrid.Columns.GridColumn cQty = AddCol(GridViewPreview, "Qty", "Quantity", 92);
            cQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cQty.DisplayFormat.FormatString = "#,##0.####";
            RightAlign(cQty);
            AddCol(GridViewPreview, "UOM", "", 46);
            DevExpress.XtraGrid.Columns.GridColumn cPr = AddCol(GridViewPreview, "UnitPrice", "Unit Price", 92);
            cPr.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cPr.DisplayFormat.FormatString = "#,##0.00##";
            RightAlign(cPr);
            DevExpress.XtraGrid.Columns.GridColumn cAmt = AddCol(GridViewPreview, "Amount", "Amount", 104);
            cAmt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cAmt.DisplayFormat.FormatString = "#,##0.00";
            RightAlign(cAmt);

            // Which machines a line covers is supporting detail, so it is dimmed — derived from the
            // skin's own row colours rather than a hardcoded grey that would be invisible on a dark one.
            System.Drawing.Color fore = GridViewPreview.PaintAppearance.Row.ForeColor;
            System.Drawing.Color back = GridViewPreview.PaintAppearance.Row.BackColor;
            GridViewPreview.Columns["Machines"].AppearanceCell.ForeColor = System.Drawing.Color.FromArgb(
                (fore.R + back.R) / 2, (fore.G + back.G) / 2, (fore.B + back.B) / 2);
            GridViewPreview.Columns["Machines"].AppearanceCell.Options.UseForeColor = true;

            // Per-invoice total, on the group row, under the Amount column.
            DevExpress.XtraGrid.GridGroupSummaryItem gs = new DevExpress.XtraGrid.GridGroupSummaryItem();
            gs.SummaryType = DevExpress.Data.SummaryItemType.Sum;
            gs.FieldName = "Amount";
            gs.DisplayFormat = "{0:n2}";
            gs.ShowInGroupColumnFooter = cAmt;
            GridViewPreview.GroupSummary.Add(gs);

            // Grand total never scrolls away.
            cAmt.Summary.Add(DevExpress.Data.SummaryItemType.Sum, "Amount", "{0:n2}");
            cQty.Summary.Add(DevExpress.Data.SummaryItemType.Sum, "Qty", "{0:n0}");

            GridViewPreview.CustomColumnDisplayText +=
                new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(OnPreviewDisplayText);
            GridPreview.DataSource = _dtPreview;
        }

        /// <summary>A rental worth nothing prints the word FOC, exactly as the customer's own invoice
        /// does. A METER line that billed no copies is not FOC and still shows 0.00.</summary>
        private void OnPreviewDisplayText(object sender,
            DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "Amount") return;
            if (e.ListSourceRowIndex < 0 || e.ListSourceRowIndex >= _dtPreview.Rows.Count) return;
            DataRow r = _dtPreview.Rows[e.ListSourceRowIndex];
            if (r["IsFoc"] != DBNull.Value && Convert.ToBoolean(r["IsFoc"])) e.DisplayText = "FOC";
        }

        /// <summary>
        /// The sample fleet: six machines, four models, two black rates, colour on two different
        /// models, one machine with no rental and one rented free.
        ///
        /// <para>Every part of that is load-bearing. The 0.019 machine is what stops "one line for
        /// all" being read as "always exactly one line" — it still yields two black lines because
        /// the rates differ, which is Pasir Gudang's real invoice. The three identical 4545i are
        /// what make "per model" different from "per machine". Colour on two models is what makes
        /// the Black &amp; Colour question visible at all. The machine with no rental is what makes
        /// the machine count differ from the invoice count, which is the whole point of the
        /// per-machine layouts. And the free rental is what exercises FOC.</para>
        ///
        /// <para>Emitted rentals-first, then black, then colour: the fold preserves first-appearance
        /// order, so interleaving them per machine would scatter the rental lines through the invoice
        /// and make "rental per model" impossible to see.</para>
        /// </summary>
        private static List<MeterBillLine> SampleFleet()
        {
            List<MeterBillLine> lines = new List<MeterBillLine>();
            // Rentals, all at the same price on purpose. A machine rented at a different price --
            // or rented free -- cannot share a row with the others, because one row is one
            // Qty x UnitPrice and two prices have no single answer. Giving the sample a rental that
            // differed made "One line for all machines" produce two lines, which is arithmetically
            // right and pedagogically useless: the option has to be seen doing what it says.
            lines.Add(Rent("iR-ADV 8505", "SWD00508", "HEAVY DUTY", 300m));
            lines.Add(Rent("iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", 300m));
            lines.Add(Rent("iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY", 300m));
            lines.Add(Rent("iR-ADV 4545i", "UMV05259", "MEDIUM DUTY", 300m));
            lines.Add(Rent("iR-ADV 4545i", "UPB00820", "MEDIUM DUTY", 300m));
            // The 8505 on its own rate, exactly as Pasir Gudang bills it. "Merge, ignoring model"
            // still gives TWO black lines here -- 60,249 at 0.019 and 52,860 across five machines at
            // 0.0285 -- because a row is one Qty x UnitPrice and two rates have no single answer.
            // That is the rule the spec states ("出几行由资料决定") and the invoice the customer
            // actually sends, so the sample shows it rather than hiding it behind a tidier fiction.
            lines.Add(Meter("iR-ADV 8505", "SWD00508", "HEAVY DUTY", "BK", "BK COPY + PRINT", 0.019m, 534659m, 594908m));
            lines.Add(Meter("iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", "BK", "BK COPY + PRINT", 0.0285m, 326760m, 355403m));
            lines.Add(Meter("iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY", "BK", "BK COPY + PRINT", 0.0285m, 58218m, 68258m));
            lines.Add(Meter("iR-ADV 4545i", "UMV05259", "MEDIUM DUTY", "BK", "BK COPY + PRINT", 0.0285m, 38423m, 42472m));
            lines.Add(Meter("iR-ADV 4545i", "UPB00820", "MEDIUM DUTY", "BK", "BK COPY + PRINT", 0.0285m, 72143m, 79314m));
            lines.Add(Meter("iR-ADV C4535i", "UNV01325", "MEDIUM DUTY", "BK", "BK COPY + PRINT", 0.0285m, 21947m, 24904m));
            // colour — on two different models, so it obeys the model rule visibly
            lines.Add(Meter("iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", "CL", "COLOUR COPY + PRINT", 0.285m, 41172m, 46865m));
            lines.Add(Meter("iR-ADV C4535i", "UNV01325", "MEDIUM DUTY", "CL", "COLOUR COPY + PRINT", 0.285m, 12880m, 14000m));
            return lines;
        }

        private static MeterBillLine Rent(string model, string serial, string label, decimal amount)
        {
            MeterBillLine l = NewSample(model, serial, label);
            l.IsFlat = true; l.IsRental = true;
            l.MeterTypeCode = "RENTAL"; l.MeterTypeName = "MONTHLY RENTAL"; l.ACItemCode = "RENTAL";
            l.Charge = amount; l.EffUnitPrice = amount;
            l.RentalMonths = 36; l.RentalStartDate = new DateTime(2025, 7, 1);
            return l;
        }

        private static MeterBillLine Meter(string model, string serial, string label, string role,
            string name, decimal rate, decimal prev, decimal cur)
        {
            MeterBillLine m = NewSample(model, serial, label);
            m.MeterTypeCode = role; m.MeterTypeName = name; m.ACItemCode = role; m.ColorLabel = role;
            m.Rate = rate; m.EffUnitPrice = rate;
            m.Last = prev; m.Current = cur;
            m.Usage = cur - prev; m.BillCopies = m.Usage;
            m.Charge = Math.Round(m.BillCopies * rate, 2, MidpointRounding.AwayFromZero);
            return m;
        }

        private static MeterBillLine NewSample(string model, string serial, string label)
        {
            MeterBillLine l = new MeterBillLine();
            l.ModelCode = model; l.SerialNumber = serial; l.LineGroupCode = label;
            l.AuditDate = new DateTime(2026, 7, 24); l.LastDate = new DateTime(2026, 6, 24);
            // The sample is a contract that HAS a format — that is the whole subject of this screen —
            // so it must be described the way such a contract's invoice is described.
            l.NewMoneyRules = true;
            return l;
        }

        private void RefreshPreview()
        {
            if (_dtPreview == null) return;
            string split = CurrentSplit();
            char rentalMode = CurrentRental()[0];
            char meterMode = CurrentMeter()[0];

            List<MeterBillLine> fleet = SampleFleet();
            List<KeyValuePair<string, List<MeterBillLine>>> invoices = SplitIntoInvoices(fleet, split);
            int pad = invoices.Count.ToString().Length;

            decimal grand = 0m;
            int rental = 0, black = 0, colour = 0;

            GridViewPreview.BeginUpdate();
            try
            {
                _dtPreview.BeginLoadData();
                try
                {
                    _dtPreview.Rows.Clear();
                    for (int inv = 0; inv < invoices.Count; inv++)
                    {
                        List<ScpFoldedLine> rows =
                            ScpInvoiceLayout.FoldWith(invoices[inv].Value, rentalMode, meterMode);
                        // The ordinal is zero-padded because groups sort on the caption STRING —
                        // "INVOICE 10" would otherwise come before "INVOICE 2".
                        string caption = "INVOICE " + (inv + 1).ToString().PadLeft(pad, '0') +
                                         " of " + invoices.Count + "   ·   " + invoices[inv].Key +
                                         "   ·   " + rows.Count + (rows.Count == 1 ? " line" : " lines");
                        int no = 0;
                        foreach (ScpFoldedLine row in rows)
                        {
                            no++;
                            grand += row.PrintAmount;
                            if (row.Leader.IsRental) rental++;
                            else if (row.Leader.ColorLabel == "CL") colour++;
                            else black++;

                            DataRow r = _dtPreview.NewRow();
                            r["InvKey"] = inv;
                            r["Invoice"] = caption;
                            r["LineNo"] = no;
                            r["Code"] = row.Leader.MeterTypeCode;
                            r["Descr"] = Describe(row);
                            r["Machines"] = Machines(row, fleet);
                            r["Qty"] = row.PrintQty;
                            r["UOM"] = row.Leader.IsFlat ? "UNIT" : "PCS";
                            r["UnitPrice"] = row.PrintUnitPrice;
                            r["Amount"] = row.PrintAmount;
                            r["IsFoc"] = row.Leader.IsFlat && row.PrintAmount == 0m;
                            _dtPreview.Rows.Add(r);
                        }
                    }
                }
                finally { _dtPreview.EndLoadData(); }
            }
            finally { GridViewPreview.EndUpdate(); }

            // Two invoices or fewer read as a whole document; more, and the collapsed group rows ARE
            // the answer, with the first opened as a worked example.
            if (invoices.Count <= 2) GridViewPreview.ExpandAllGroups();
            else { GridViewPreview.CollapseAllGroups(); GridViewPreview.SetRowExpanded(-1, true); }
            GridViewPreview.TopRowIndex = 0;

            LblPreviewNote.Text = SummaryLine(fleet, invoices.Count, rental, black, colour, grand);
        }

        /// <summary>What changes when a radio is clicked — line counts — not the fleet, which never
        /// changes, and not the total, which is identical on every layout because folding is
        /// presentation only. Saying so is the point: it is the thing a biller is anxious about.</summary>
        private string SummaryLine(List<MeterBillLine> fleet, int invoices, int rental, int black,
            int colour, decimal grand)
        {
            int machines = 0;
            List<string> seen = new List<string>();
            foreach (MeterBillLine l in fleet)
                if (!seen.Contains(l.SerialNumber)) { seen.Add(l.SerialNumber); machines++; }
            int lines = rental + black + colour;

            string s = machines + " machines  →  " + invoices + (invoices == 1 ? " invoice" : " invoices") +
                       ", " + lines + " lines  (" + rental + " rental · " + black + " black · " +
                       colour + " colour)  ·  RM" + grand.ToString("#,##0.00") + ", the same on every layout";

            // The one thing that surprises people: "one line for all" can still produce two, when the
            // machines are not on the same rate.
            if (CurrentMeter()[0] == ScpBillingFormat.LINE_ACROSS_MODEL && black > 1)
                s += "  ·  " + black + " black lines: merging ignores the model, but never the rate — 0.019 and 0.0285 cannot share a row";
            return s;
        }

        /// <summary>Which machines land on which invoice, for this split.</summary>
        private static List<KeyValuePair<string, List<MeterBillLine>>> SplitIntoInvoices(
            List<MeterBillLine> fleet, string split)
        {
            List<KeyValuePair<string, List<MeterBillLine>>> outp =
                new List<KeyValuePair<string, List<MeterBillLine>>>();
            bool perMachine = split == ScpBillingFormat.SPLIT_PER_MACHINE
                           || split == ScpBillingFormat.SPLIT_PER_MACHINE_SEPARATE;
            bool rentalApart = split == ScpBillingFormat.SPLIT_RENTAL_SEPARATE
                            || split == ScpBillingFormat.SPLIT_PER_MACHINE_SEPARATE;

            List<string> order = new List<string>();
            Dictionary<string, List<MeterBillLine>> buckets = new Dictionary<string, List<MeterBillLine>>();
            // Two passes when rental is apart, so all the rental invoices come out as one run and the
            // meter invoices as another — which is how the real numbering runs.
            for (int pass = 0; pass < (rentalApart ? 2 : 1); pass++)
            {
                foreach (MeterBillLine ln in fleet)
                {
                    if (rentalApart && (pass == 0) != ln.IsRental) continue;
                    string key = perMachine
                        ? ln.SerialNumber + "  " + ln.ModelCode
                        : "everything on one invoice";
                    if (rentalApart) key = (ln.IsRental ? "rental" : "meters") + (perMachine ? "  ·  " + key : "");
                    if (!buckets.ContainsKey(key)) { buckets[key] = new List<MeterBillLine>(); order.Add(key); }
                    buckets[key].Add(ln);
                }
            }
            foreach (string k in order)
                outp.Add(new KeyValuePair<string, List<MeterBillLine>>(k, buckets[k]));
            return outp;
        }

        /// <summary>
        /// The description the invoice will carry, composed by the engine's own
        /// <see cref="ScpInvoiceBuilder.ComposeFoldedDescription"/> so the preview cannot word a line
        /// differently from the document.
        ///
        /// <para>The duty label prints only when every machine on the row carries it — a row that
        /// merged a HEAVY machine with MEDIUM ones is neither, and saying "HEAVY DUTY" because the
        /// first member happened to be one would misdescribe what the row covers.</para>
        /// </summary>
        private static string Describe(ScpFoldedLine row)
        {
            MeterBillLine ln = row.Leader;
            string what = string.IsNullOrEmpty(ln.MeterTypeName) ? ln.MeterTypeCode : ln.MeterTypeName;
            string label = ln.LineGroupCode ?? "";
            foreach (MeterBillLine m in row.Members)
                if (!string.Equals(m.LineGroupCode ?? "", label, StringComparison.OrdinalIgnoreCase))
                { label = ""; break; }
            if (label.Length > 0) what += "  —  " + label;
            // One grid row per invoice line, so the second line of the description follows the first
            // rather than doubling the row height.
            return ScpInvoiceBuilder.ComposeFoldedDescription(row, what).Replace("\r\n", "   ");
        }

        /// <summary>
        /// When the merge key IS the model, lead with the model. When the merge deliberately ignores
        /// the model, lead with the count and list the models. That asymmetry is what lets someone
        /// tell "across model" from "same model" even where both happen to yield the same row count.
        /// </summary>
        private static string Machines(ScpFoldedLine row, List<MeterBillLine> fleet)
        {
            // An unmerged row names its own machine in the description now, so repeating it here
            // would be noise. Only the "no rental" fact is worth adding.
            if (!row.IsMerged)
                return HasRental(fleet, row.Leader.SerialNumber) ? "" : "(this machine has no rental)";

            List<string> serials = new List<string>();
            foreach (MeterBillLine m in row.Members) serials.Add(m.SerialNumber);
            return string.Join(", ", serials.ToArray());
        }

        private static bool HasRental(List<MeterBillLine> fleet, string serial)
        {
            foreach (MeterBillLine l in fleet)
                if (l.IsRental && l.SerialNumber == serial) return true;
            return false;
        }
    }
}
