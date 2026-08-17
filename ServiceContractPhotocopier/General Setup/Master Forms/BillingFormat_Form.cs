using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.GeneralSetup.MasterForms
{
    /// <summary>
    /// Create and name an invoice layout, and see an invoice in it before saving.
    ///
    /// <para>Three questions decide every layout the customer uses — how many invoices, how the
    /// rental lines collapse, how the black and colour lines collapse. Rather than ask them on
    /// every contract, they are answered once here under a name the user chooses, and contracts
    /// point at it.</para>
    ///
    /// <para>The preview is the reason this screen exists rather than three combo boxes on the
    /// contract. "Rental per model" and "rental one line" are indistinguishable as words and
    /// obvious as invoices, so the sample redraws on every click, using a fixed six-machine fleet
    /// modelled on Pasir Gudang — four models, two rates, one machine with no rental — because that
    /// is the smallest fleet on which all three choices visibly differ.</para>
    /// </summary>
    public partial class BillingFormat_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly string _editCode;      // "" = creating a new one
        private bool _loading;

        /// <summary>The code that was saved, so the list can re-select it.</summary>
        public string SavedCode = "";

        public BillingFormat_Form() { InitializeComponent(); }

        public BillingFormat_Form(DBSetting db, string editCode) : this()
        {
            _db = db;
            _editCode = (editCode ?? "").Trim();
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            BuildPreviewGrid();
            _loading = true;
            try
            {
                if (_editCode.Length > 0) LoadExisting();
                else
                {
                    RdoOneInvoice.Checked = true;
                    RdoRentalOneLine.Checked = true;
                    RdoMeterOneLine.Checked = true;
                }
            }
            finally { _loading = false; }
            WireChangeEvents();
            RefreshPreview();
            this.Text = _editCode.Length > 0 ? "Billing Format — " + _editCode : "Billing Format — new";
        }

        private void LoadExisting()
        {
            DataTable t = _db.GetDataTable(
                "SELECT FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, " +
                "ISNULL(Remark,'') AS Remark, Inactive FROM dbo.zSCP2_BillingFormat " +
                "WHERE FormatCode = N'" + _editCode.Replace("'", "''") + "'", false);
            if (t.Rows.Count == 0) return;
            DataRow r = t.Rows[0];
            TxtCode.Text = Convert.ToString(r["FormatCode"]);
            TxtName.Text = Convert.ToString(r["FormatName"]);
            TxtRemark.Text = Convert.ToString(r["Remark"]);
            ChkInactive.Checked = Convert.ToString(r["Inactive"]) == "Y";
            TxtCode.Properties.ReadOnly = true;      // the code is what contracts point at

            string split = Convert.ToString(r["InvoiceSplit"]).Trim();
            RdoOneInvoice.Checked = split == ScpBillingFormat.SPLIT_ONE;
            RdoRentalSeparate.Checked = split == ScpBillingFormat.SPLIT_RENTAL_SEPARATE;
            RdoPerMachine.Checked = split == ScpBillingFormat.SPLIT_PER_MACHINE;
            RdoPerMachineSeparate.Checked = split == ScpBillingFormat.SPLIT_PER_MACHINE_SEPARATE;

            char rl = ModeChar(r["RentalLineMode"], ScpBillingFormat.LINE_ACROSS_MODEL);
            RdoRentalOneLine.Checked = rl == ScpBillingFormat.LINE_ACROSS_MODEL;
            RdoRentalPerModel.Checked = rl == ScpBillingFormat.LINE_SAME_MODEL;
            RdoRentalPerMachine.Checked = rl == ScpBillingFormat.LINE_PER_MACHINE;

            char ml = ModeChar(r["MeterLineMode"], ScpBillingFormat.LINE_PER_MACHINE);
            RdoMeterOneLine.Checked = ml == ScpBillingFormat.LINE_ACROSS_MODEL;
            RdoMeterPerModel.Checked = ml == ScpBillingFormat.LINE_SAME_MODEL;
            RdoMeterPerMachine.Checked = ml == ScpBillingFormat.LINE_PER_MACHINE;
        }

        private static char ModeChar(object o, char fallback)
        {
            string s = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            return s.Length == 0 ? fallback : char.ToUpperInvariant(s[0]);
        }

        private void WireChangeEvents()
        {
            EventHandler h = new EventHandler(Choice_Changed);
            RdoOneInvoice.CheckedChanged += h;
            RdoRentalSeparate.CheckedChanged += h;
            RdoPerMachine.CheckedChanged += h;
            RdoPerMachineSeparate.CheckedChanged += h;
            RdoRentalOneLine.CheckedChanged += h;
            RdoRentalPerModel.CheckedChanged += h;
            RdoRentalPerMachine.CheckedChanged += h;
            RdoMeterOneLine.CheckedChanged += h;
            RdoMeterPerModel.CheckedChanged += h;
            RdoMeterPerMachine.CheckedChanged += h;
        }

        private void Choice_Changed(object sender, EventArgs e)
        {
            if (_loading) return;
            RefreshPreview();
        }

        // ===== the format currently on screen =====

        private string CurrentSplit()
        {
            if (RdoRentalSeparate.Checked) return ScpBillingFormat.SPLIT_RENTAL_SEPARATE;
            if (RdoPerMachine.Checked) return ScpBillingFormat.SPLIT_PER_MACHINE;
            if (RdoPerMachineSeparate.Checked) return ScpBillingFormat.SPLIT_PER_MACHINE_SEPARATE;
            return ScpBillingFormat.SPLIT_ONE;
        }

        private char CurrentRentalMode()
        {
            if (RdoRentalPerModel.Checked) return ScpBillingFormat.LINE_SAME_MODEL;
            if (RdoRentalPerMachine.Checked) return ScpBillingFormat.LINE_PER_MACHINE;
            return ScpBillingFormat.LINE_ACROSS_MODEL;
        }

        private char CurrentMeterMode()
        {
            if (RdoMeterPerModel.Checked) return ScpBillingFormat.LINE_SAME_MODEL;
            if (RdoMeterPerMachine.Checked) return ScpBillingFormat.LINE_PER_MACHINE;
            return ScpBillingFormat.LINE_ACROSS_MODEL;
        }

        // ===== preview =====

        private DataTable _dtPreview;

        private void BuildPreviewGrid()
        {
            _dtPreview = new DataTable();
            _dtPreview.Columns.Add("Level", typeof(int));
            _dtPreview.Columns.Add("What", typeof(string));
            _dtPreview.Columns.Add("Qty", typeof(decimal));
            _dtPreview.Columns.Add("UnitPrice", typeof(decimal));
            _dtPreview.Columns.Add("Amount", typeof(decimal));

            GridViewPreview.OptionsBehavior.AutoPopulateColumns = false;
            GridViewPreview.OptionsBehavior.Editable = false;
            GridViewPreview.OptionsView.ShowGroupPanel = false;
            GridViewPreview.OptionsView.ShowIndicator = false;
            GridViewPreview.OptionsSelection.EnableAppearanceFocusedCell = false;
            GridViewPreview.Columns.Clear();

            DevExpress.XtraGrid.Columns.GridColumn c1 = GridViewPreview.Columns.AddVisible("What");
            c1.Caption = "The invoice would look like this";
            c1.Width = 380;
            DevExpress.XtraGrid.Columns.GridColumn c2 = GridViewPreview.Columns.AddVisible("Qty");
            c2.Caption = "Qty";
            c2.Width = 80;
            c2.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c2.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn c3 = GridViewPreview.Columns.AddVisible("UnitPrice");
            c3.Caption = "Unit Price";
            c3.Width = 80;
            c3.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c3.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn c4 = GridViewPreview.Columns.AddVisible("Amount");
            c4.Caption = "Amount";
            c4.Width = 90;
            c4.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            c4.DisplayFormat.FormatString = "#,##0.00";

            GridViewPreview.RowCellStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(Preview_RowCellStyle);
        }

        /// <summary>
        /// The sample fleet. Modelled on Pasir Gudang because it is the smallest real fleet on which
        /// all three choices produce visibly different invoices: four models, two black rates so
        /// "per model" and "one line" cannot look alike, colour on one machine only, and a machine
        /// with no rental meter so the unit count is not simply the machine count.
        /// </summary>
        private static List<MeterBillLine> SampleFleet()
        {
            List<MeterBillLine> lines = new List<MeterBillLine>();
            AddMachine(lines, "iR-ADV 8505", "SWD00508", "HEAVY DUTY", 500m, 0.019m, 534659m, 594908m, 0m, 0m);
            AddMachine(lines, "iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", 300m, 0.0285m, 326760m, 355403m, 41172m, 46865m);
            AddMachine(lines, "iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY", 300m, 0.0285m, 58218m, 68258m, 0m, 0m);
            AddMachine(lines, "iR-ADV 4545i", "UMV05259", "MEDIUM DUTY", 300m, 0.0285m, 38423m, 42472m, 0m, 0m);
            AddMachine(lines, "iR-ADV 4545i", "UPB00820", "MEDIUM DUTY", 300m, 0.0285m, 72143m, 79314m, 0m, 0m);
            AddMachine(lines, "iR-ADV C4535i", "UNV01325", "MEDIUM DUTY", 0m, 0.0285m, 21947m, 24904m, 0m, 0m);
            return lines;
        }

        private static void AddMachine(List<MeterBillLine> lines, string model, string serial, string label,
            decimal rental, decimal bkRate, decimal bkPrev, decimal bkCur, decimal clPrev, decimal clCur)
        {
            if (rental > 0m)
            {
                MeterBillLine r = NewLine(model, serial, label);
                r.IsFlat = true; r.IsRental = true;
                r.MeterTypeCode = "RENTAL"; r.MeterTypeName = "MONTHLY RENTAL"; r.ACItemCode = "RENTAL";
                r.Charge = rental; r.EffUnitPrice = rental;
                r.RentalMonths = 36; r.RentalStartDate = new DateTime(2025, 7, 1);
                lines.Add(r);
            }
            lines.Add(Meter(model, serial, label, "BK", "BK COPY + PRINT", bkRate, bkPrev, bkCur));
            if (clCur > clPrev)
                lines.Add(Meter(model, serial, label, "CL", "COLOUR COPY + PRINT", 0.285m, clPrev, clCur));
        }

        private static MeterBillLine Meter(string model, string serial, string label, string role,
            string name, decimal rate, decimal prev, decimal cur)
        {
            MeterBillLine m = NewLine(model, serial, label);
            m.MeterTypeCode = role; m.MeterTypeName = name; m.ACItemCode = role; m.ColorLabel = role;
            m.Rate = rate; m.EffUnitPrice = rate;
            m.Last = prev; m.Current = cur;
            m.Usage = cur - prev; m.BillCopies = m.Usage;
            m.Charge = Math.Round(m.BillCopies * rate, 2, MidpointRounding.AwayFromZero);
            return m;
        }

        private static MeterBillLine NewLine(string model, string serial, string label)
        {
            MeterBillLine l = new MeterBillLine();
            l.ModelCode = model; l.SerialNumber = serial; l.LineGroupCode = label;
            l.ItemName = ""; l.AuditDate = new DateTime(2026, 7, 24); l.LastDate = new DateTime(2026, 6, 24);
            return l;
        }

        private void RefreshPreview()
        {
            _dtPreview.Rows.Clear();
            string split = CurrentSplit();
            char rentalMode = CurrentRentalMode();
            char meterMode = CurrentMeterMode();

            List<MeterBillLine> fleet = SampleFleet();
            // Split into the invoices this format produces, then fold each one's lines.
            List<KeyValuePair<string, List<MeterBillLine>>> invoices = SplitIntoInvoices(fleet, split);

            decimal grand = 0m;
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
                    l["What"] = "      " + Describe(row);
                    l["Qty"] = row.PrintQty;
                    l["UnitPrice"] = row.PrintUnitPrice;
                    l["Amount"] = row.PrintAmount;
                    _dtPreview.Rows.Add(l);
                }
            }
            GridPreview.DataSource = _dtPreview;
            GridViewPreview.BestFitColumns();

            LblPreviewNote.Text = "Sample: 6 machines, 4 models, one with no rental meter.   " +
                                  invoices.Count + (invoices.Count == 1 ? " invoice" : " invoices") +
                                  ", " + grand.ToString("#,##0.00") + " in total.";
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
                string key = perMachine ? "Machine " + ln.SerialNumber : "Invoice";
                if (rentalApart && ln.IsRental) key += " — RENTAL";
                else if (rentalApart) key += " — METER";
                if (!buckets.ContainsKey(key)) { buckets[key] = new List<MeterBillLine>(); order.Add(key); }
                buckets[key].Add(ln);
            }
            foreach (string k in order)
                outp.Add(new KeyValuePair<string, List<MeterBillLine>>(k, buckets[k]));
            return outp;
        }

        private static string Describe(ScpFoldedLine row)
        {
            MeterBillLine ln = row.Leader;
            string what = string.IsNullOrEmpty(ln.MeterTypeName) ? ln.MeterTypeCode : ln.MeterTypeName;
            if (!string.IsNullOrEmpty(ln.LineGroupCode)) what += " — " + ln.LineGroupCode;
            if (row.IsMerged) what += "   (" + row.Units + " machines: " + Models(row) + ")";
            else what += "   " + ln.ModelCode + " " + ln.SerialNumber;
            return what;
        }

        private static string Models(ScpFoldedLine row)
        {
            List<string> seen = new List<string>();
            foreach (MeterBillLine m in row.Members)
                if (!seen.Contains(m.ModelCode)) seen.Add(m.ModelCode);
            return string.Join(", ", seen.ToArray());
        }

        private void Preview_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            object lv = GridViewPreview.GetRowCellValue(e.RowHandle, "Level");
            if (lv != null && lv != DBNull.Value && Convert.ToInt32(lv) == 0)
            {
                e.Appearance.Font = new System.Drawing.Font(e.Appearance.Font, System.Drawing.FontStyle.Bold);
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(238, 242, 248);
            }
        }

        // ===== save =====

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string code = TxtCode.Text.Trim().ToUpperInvariant();
            string name = TxtName.Text.Trim();
            if (code.Length == 0) { Warn("Give the format a code — contracts point at it.", TxtCode); return; }
            if (name.Length == 0) { Warn("Give the format a name people will recognise.", TxtName); return; }

            try
            {
                if (_editCode.Length == 0)
                {
                    object dup = _db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_BillingFormat " +
                        "WHERE FormatCode = N'" + code.Replace("'", "''") + "'");
                    if (dup != null && dup != DBNull.Value && Convert.ToInt32(dup) > 0)
                    { Warn("A format with code '" + code + "' already exists.", TxtCode); return; }

                    _db.ExecuteNonQuery(
                        "INSERT INTO dbo.zSCP2_BillingFormat " +
                        "(FormatCode, FormatName, InvoiceSplit, RentalLineMode, MeterLineMode, Remark, Inactive) " +
                        "VALUES (" + Q(code) + "," + Q(name) + "," + Q(CurrentSplit()) + "," +
                        Q(CurrentRentalMode().ToString()) + "," + Q(CurrentMeterMode().ToString()) + "," +
                        Q(TxtRemark.Text.Trim()) + ",'" + (ChkInactive.Checked ? 'Y' : 'N') + "')");
                }
                else
                {
                    _db.ExecuteNonQuery(
                        "UPDATE dbo.zSCP2_BillingFormat SET FormatName=" + Q(name) +
                        ", InvoiceSplit=" + Q(CurrentSplit()) +
                        ", RentalLineMode=" + Q(CurrentRentalMode().ToString()) +
                        ", MeterLineMode=" + Q(CurrentMeterMode().ToString()) +
                        ", Remark=" + Q(TxtRemark.Text.Trim()) +
                        ", Inactive='" + (ChkInactive.Checked ? 'Y' : 'N') + "'" +
                        ", LastModified=GETDATE() WHERE FormatCode=" + Q(_editCode));

                    // Contracts store their own copy of the three answers, so an edit here has to be
                    // pushed to them explicitly -- and only when the user says so, because it changes
                    // how those contracts bill next month.
                    int used = ScpBillingFormat.UsedByContractCount(_db, _editCode);
                    if (used > 0 && XtraMessageBox.Show(this,
                            used + " contract(s) use this format.\r\n\r\n" +
                            "Apply the change to them as well?\r\n" +
                            "No leaves them billing the way they do now.",
                            "Billing Format", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ScpBillingFormat f = ScpBillingFormat.Load(_db, _editCode);
                        if (f != null)
                            _db.ExecuteNonQuery(
                                "UPDATE dbo.zSCP2_Contract SET BillingMode='" + f.BillingMode +
                                "', RentalSeparateInvoice='" + (f.RentalSeparateInvoice ? 'Y' : 'N') +
                                "', RentalLineMode='" + f.RentalLineMode +
                                "', MeterLineMode='" + f.MeterLineMode +
                                "' WHERE BillingFormatCode=" + Q(_editCode));
                    }
                }
                SavedCode = code;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Save failed:\r\n" + ex.Message, "Billing Format",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Q(string s) { return "N'" + (s ?? "").Replace("'", "''") + "'"; }

        private void Warn(string msg, Control focus)
        {
            XtraMessageBox.Show(msg, "Billing Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (focus != null) focus.Focus();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
