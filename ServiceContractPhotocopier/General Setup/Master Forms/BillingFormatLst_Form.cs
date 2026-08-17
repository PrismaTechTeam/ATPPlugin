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
    /// The bottom grid, where Multi Pricing has its rate ladder, is a sample invoice.</para>
    ///
    /// <para>The sample is the reason the three questions get a screen rather than three combo boxes
    /// on the contract: "rental per model" and "rental one line" are indistinguishable as words and
    /// obvious as invoices. It redraws on every click, folded through <see cref="ScpInvoiceLayout"/>
    /// — the same call the engine makes — so a preview cannot promise what Generate will not do.</para>
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
            BuildColumns();
            GridViewFormats.FocusedRowChanged +=
                new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(OnFocusedRowChanged);
            RgInvoices.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            RgRental.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            RgMeter.SelectedIndexChanged += new EventHandler(OnChoiceChanged);
            this.KeyDown += new KeyEventHandler(OnFormKeyDown);
            LoadData("");
            SetEditMode(false);
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 && !_editMode) { this.Close(); e.Handled = true; }
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

            _dtPreview = new DataTable();
            _dtPreview.Columns.Add("Level", typeof(int));
            _dtPreview.Columns.Add("What", typeof(string));
            _dtPreview.Columns.Add("Qty", typeof(decimal));
            _dtPreview.Columns.Add("UnitPrice", typeof(decimal));
            _dtPreview.Columns.Add("Amount", typeof(decimal));

            GridViewPreview.OptionsBehavior.AutoPopulateColumns = false;
            GridViewPreview.Columns.Clear();
            AddCol(GridViewPreview, "What", "Line", 600);
            DevExpress.XtraGrid.Columns.GridColumn cq = AddCol(GridViewPreview, "Qty", "Quantity", 110);
            cq.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cq.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn cp = AddCol(GridViewPreview, "UnitPrice", "Unit Price", 110);
            cp.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cp.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn ca = AddCol(GridViewPreview, "Amount", "Amount", 120);
            ca.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            ca.DisplayFormat.FormatString = "#,##0.00";
            GridViewPreview.RowCellStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(OnPreviewRowStyle);
            GridPreview.DataSource = _dtPreview;
        }

        private static DevExpress.XtraGrid.Columns.GridColumn AddCol(
            DevExpress.XtraGrid.Views.Grid.GridView gv, string field, string caption, int width)
        {
            DevExpress.XtraGrid.Columns.GridColumn c = gv.Columns.AddVisible(field);
            c.Caption = caption;
            c.Width = width;
            return c;
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
                    RgInvoices.EditValue = ScpBillingFormat.SPLIT_ONE;
                    RgRental.EditValue = "A";
                    RgMeter.EditValue = "S";
                }
                else
                {
                    _selectedCode = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "FormatCode"));
                    TxtCode.Text = _selectedCode;
                    TxtName.Text = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "FormatName"));
                    TxtRemark.Text = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "Remark"));
                    ChkInactive.Checked = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "Inactive")) == "Y";
                    RgInvoices.EditValue = Convert.ToString(GridViewFormats.GetRowCellValue(rh, "InvoiceSplit")).Trim();
                    RgRental.EditValue = FirstChar(GridViewFormats.GetRowCellValue(rh, "RentalLineMode"), 'A').ToString();
                    RgMeter.EditValue = FirstChar(GridViewFormats.GetRowCellValue(rh, "MeterLineMode"), 'S').ToString();
                }
            }
            finally { _loading = false; }
            RefreshPreview();
        }

        private void SetEditMode(bool on)
        {
            _editMode = on;
            GridFormats.Enabled = !on;
            TxtCode.Properties.ReadOnly = !on || !_isNew;   // the code is what contracts point at
            TxtName.Properties.ReadOnly = !on;
            TxtRemark.Properties.ReadOnly = !on;
            ChkInactive.Properties.ReadOnly = !on;
            RgInvoices.Properties.ReadOnly = !on;
            RgRental.Properties.ReadOnly = !on;
            RgMeter.Properties.ReadOnly = !on;
            BtnNew.Enabled = !on;
            BtnEdit.Enabled = !on && _selectedCode.Length > 0;
            BtnCopyNew.Enabled = !on && _selectedCode.Length > 0;
            BtnDelete.Enabled = !on && _selectedCode.Length > 0;
            BtnRefresh.Enabled = !on;
            BtnSave.Enabled = on;
            BtnCancel.Enabled = on;
            LblStatus.Text = on
                ? (_isNew ? "New billing format — Save to keep it." : "Editing " + _selectedCode + " — Save to keep the change.")
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
                RgInvoices.EditValue = ScpBillingFormat.SPLIT_ONE;
                RgRental.EditValue = "A";
                RgMeter.EditValue = "S";
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

        // ===== the format currently on screen =====

        private string CurrentSplit()
        {
            string v = RgInvoices.EditValue == null ? "" : Convert.ToString(RgInvoices.EditValue);
            return v.Length == 0 ? ScpBillingFormat.SPLIT_ONE : v;
        }

        private string CurrentRental()
        {
            string v = RgRental.EditValue == null ? "" : Convert.ToString(RgRental.EditValue);
            return v.Length == 0 ? "A" : v;
        }

        private string CurrentMeter()
        {
            string v = RgMeter.EditValue == null ? "" : Convert.ToString(RgMeter.EditValue);
            return v.Length == 0 ? "S" : v;
        }

        private void OnChoiceChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            RefreshPreview();
        }

        // ===== sample invoice =====

        /// <summary>
        /// The sample fleet, modelled on Pasir Gudang: four models, two black rates, colour on one
        /// machine, and one machine with no rental meter. That is the smallest real fleet on which
        /// all three choices produce visibly different invoices — with a simpler one, "per model"
        /// and "one line" would draw the same thing and the preview would teach nothing.
        /// </summary>
        private static List<MeterBillLine> SampleFleet()
        {
            List<MeterBillLine> lines = new List<MeterBillLine>();
            AddSample(lines, "iR-ADV 8505", "SWD00508", "HEAVY DUTY", 500m, 0.019m, 534659m, 594908m, 0m, 0m);
            AddSample(lines, "iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", 300m, 0.0285m, 326760m, 355403m, 41172m, 46865m);
            AddSample(lines, "iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY", 300m, 0.0285m, 58218m, 68258m, 0m, 0m);
            AddSample(lines, "iR-ADV 4545i", "UMV05259", "MEDIUM DUTY", 300m, 0.0285m, 38423m, 42472m, 0m, 0m);
            AddSample(lines, "iR-ADV 4545i", "UPB00820", "MEDIUM DUTY", 300m, 0.0285m, 72143m, 79314m, 0m, 0m);
            AddSample(lines, "iR-ADV C4535i", "UNV01325", "MEDIUM DUTY", 0m, 0.0285m, 21947m, 24904m, 0m, 0m);
            return lines;
        }

        private static void AddSample(List<MeterBillLine> lines, string model, string serial, string label,
            decimal rental, decimal bkRate, decimal bkPrev, decimal bkCur, decimal clPrev, decimal clCur)
        {
            if (rental > 0m)
            {
                MeterBillLine r = NewSample(model, serial, label);
                r.IsFlat = true; r.IsRental = true;
                r.MeterTypeCode = "RENTAL"; r.MeterTypeName = "MONTHLY RENTAL"; r.ACItemCode = "RENTAL";
                r.Charge = rental; r.EffUnitPrice = rental;
                r.RentalMonths = 36; r.RentalStartDate = new DateTime(2025, 7, 1);
                lines.Add(r);
            }
            lines.Add(SampleMeter(model, serial, label, "BK", "BK COPY + PRINT", bkRate, bkPrev, bkCur));
            if (clCur > clPrev)
                lines.Add(SampleMeter(model, serial, label, "CL", "COLOUR COPY + PRINT", 0.285m, clPrev, clCur));
        }

        private static MeterBillLine SampleMeter(string model, string serial, string label, string role,
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
            return l;
        }

        private void RefreshPreview()
        {
            if (_dtPreview == null) return;
            _dtPreview.Rows.Clear();
            string split = CurrentSplit();
            char rentalMode = CurrentRental()[0];
            char meterMode = CurrentMeter()[0];

            decimal grand = 0m;
            List<KeyValuePair<string, List<MeterBillLine>>> invoices = SplitIntoInvoices(SampleFleet(), split);
            foreach (KeyValuePair<string, List<MeterBillLine>> inv in invoices)
            {
                List<ScpFoldedLine> rows = ScpInvoiceLayout.FoldWith(inv.Value, rentalMode, meterMode);
                decimal total = 0m;
                foreach (ScpFoldedLine row in rows) total += row.PrintAmount;
                grand += total;

                DataRow h = _dtPreview.NewRow();
                h["Level"] = 0;
                h["What"] = inv.Key + "   —   " + rows.Count + (rows.Count == 1 ? " line" : " lines");
                h["Amount"] = total;
                _dtPreview.Rows.Add(h);

                foreach (ScpFoldedLine row in rows)
                {
                    DataRow l = _dtPreview.NewRow();
                    l["Level"] = 1;
                    l["What"] = "        " + DescribeRow(row);
                    l["Qty"] = row.PrintQty;
                    l["UnitPrice"] = row.PrintUnitPrice;
                    l["Amount"] = row.PrintAmount;
                    _dtPreview.Rows.Add(l);
                }
            }
            LblPreviewNote.Text = "6 machines, 4 models, one with no rental meter   →   " +
                invoices.Count + (invoices.Count == 1 ? " invoice" : " invoices") +
                ", " + grand.ToString("#,##0.00") + " in total";
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
            foreach (MeterBillLine ln in fleet)
            {
                string key = perMachine ? "INVOICE  " + ln.SerialNumber : "INVOICE";
                if (rentalApart) key += ln.IsRental ? "  (rental)" : "  (meter)";
                if (!buckets.ContainsKey(key)) { buckets[key] = new List<MeterBillLine>(); order.Add(key); }
                buckets[key].Add(ln);
            }
            foreach (string k in order)
                outp.Add(new KeyValuePair<string, List<MeterBillLine>>(k, buckets[k]));
            return outp;
        }

        private static string DescribeRow(ScpFoldedLine row)
        {
            MeterBillLine ln = row.Leader;
            string what = string.IsNullOrEmpty(ln.MeterTypeName) ? ln.MeterTypeCode : ln.MeterTypeName;
            if (!string.IsNullOrEmpty(ln.LineGroupCode)) what += " — " + ln.LineGroupCode;
            if (row.IsMerged) what += "   (" + row.Units + " machines: " + Models(row) + ")";
            else what += "   " + ln.ModelCode + "  " + ln.SerialNumber;
            return what;
        }

        private static string Models(ScpFoldedLine row)
        {
            List<string> seen = new List<string>();
            foreach (MeterBillLine m in row.Members)
                if (!seen.Contains(m.ModelCode)) seen.Add(m.ModelCode);
            return string.Join(", ", seen.ToArray());
        }

        private void OnPreviewRowStyle(object sender,
            DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            object lv = GridViewPreview.GetRowCellValue(e.RowHandle, "Level");
            if (lv == null || lv == DBNull.Value || Convert.ToInt32(lv) != 0) return;
            e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
            e.Appearance.BackColor = System.Drawing.Color.FromArgb(238, 242, 248);
        }
    }
}
