using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier.MeterReading.OperationForms
{
    /// <summary>
    /// What Generate is about to create, before it creates it: every invoice, its lines, and the
    /// machines behind each line.
    ///
    /// <para>It replaces a Yes/No box that could only say a number. With formats folding several
    /// machines onto one line, the number stopped being enough — "3 invoices" says nothing about
    /// whether the five machines meant to share a line actually did.</para>
    ///
    /// <para>The rows come from <see cref="ScpInvoiceLayout.Fold"/>, the same call
    /// <see cref="ScpInvoiceBuilder.BuildInvoice"/> makes, so this cannot drift from what is
    /// produced. Quantities, prices and amounts are the folded row's own, including the
    /// round-once-per-row rule.</para>
    ///
    /// <para>A contract is BLOCKED, not warned, when a meter read backwards. That is normally a
    /// replaced meter, but it is also exactly what a machine looks like after it inherits a summed
    /// reading in migration — and billing it would clamp usage to zero and issue a silent RM0
    /// invoice that nobody notices until the customer asks why.</para>
    /// </summary>
    public partial class MeterInvoicePreview_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly List<MeterInvoiceGenerator.InvoiceJob> _jobs;
        private readonly string _periodName;
        private readonly string _note;
        private DataTable _dt;

        /// <summary>The jobs the user chose to create — blocked ones removed.</summary>
        public List<MeterInvoiceGenerator.InvoiceJob> Approved =
            new List<MeterInvoiceGenerator.InvoiceJob>();

        public MeterInvoicePreview_Form() { InitializeComponent(); }

        public MeterInvoicePreview_Form(DBSetting db, List<MeterInvoiceGenerator.InvoiceJob> jobs,
            string periodName, string note) : this()
        {
            _db = db;
            _jobs = jobs ?? new List<MeterInvoiceGenerator.InvoiceJob>();
            _periodName = periodName ?? "";
            _note = note ?? "";
            this.Load += new EventHandler(OnFormLoad);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            BuildGrid();
            Populate();
        }

        private void BuildGrid()
        {
            _dt = new DataTable();
            _dt.Columns.Add("Level", typeof(int));      // 0 invoice, 1 line, 2 machine, 3 warning
            _dt.Columns.Add("What", typeof(string));
            _dt.Columns.Add("Qty", typeof(decimal));
            _dt.Columns.Add("UnitPrice", typeof(decimal));
            _dt.Columns.Add("Amount", typeof(decimal));
            _dt.Columns.Add("Blocked", typeof(bool));

            GridViewPreview.OptionsBehavior.AutoPopulateColumns = false;
            GridViewPreview.OptionsBehavior.Editable = false;
            GridViewPreview.OptionsView.ShowGroupPanel = false;
            GridViewPreview.OptionsView.ShowIndicator = false;
            GridViewPreview.OptionsSelection.EnableAppearanceFocusedCell = false;
            GridViewPreview.Columns.Clear();

            DevExpress.XtraGrid.Columns.GridColumn cWhat = GridViewPreview.Columns.AddVisible("What");
            cWhat.Caption = "What will be created";
            cWhat.Width = 560;
            DevExpress.XtraGrid.Columns.GridColumn cQty = GridViewPreview.Columns.AddVisible("Qty");
            cQty.Caption = "Qty";
            cQty.Width = 90;
            cQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cQty.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn cPrice = GridViewPreview.Columns.AddVisible("UnitPrice");
            cPrice.Caption = "Unit Price";
            cPrice.Width = 90;
            cPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cPrice.DisplayFormat.FormatString = "#,##0.####";
            DevExpress.XtraGrid.Columns.GridColumn cAmt = GridViewPreview.Columns.AddVisible("Amount");
            cAmt.Caption = "Amount";
            cAmt.Width = 100;
            cAmt.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            cAmt.DisplayFormat.FormatString = "#,##0.00";

            GridViewPreview.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(View_RowCellStyle);
            // Money columns are left unset on rows that have no money of their own (the invoice
            // header carries only a total; machine and warning rows carry none), so they render
            // empty without a display-text handler deciding it a second time.
        }

        private void Populate()
        {
            int ready = 0, blocked = 0;
            decimal grand = 0m;
            Approved.Clear();

            foreach (MeterInvoiceGenerator.InvoiceJob job in _jobs)
            {
                List<ScpFoldedLine> rows = ScpInvoiceLayout.Fold(_db, job.Lines);

                // Decide the whole invoice first — a customer bills as a whole, so one bad reading
                // holds back its invoice rather than producing a half-right one.
                List<string> reasons = new List<string>();
                foreach (ScpFoldedLine row in rows)
                {
                    MeterBillLine bad = row.BackwardsMember();
                    if (bad != null)
                        reasons.Add("reading went backwards on " + Label(bad) +
                                    " (" + Num(bad.Last) + " → " + Num(bad.Current) + ")");
                }
                bool isBlocked = reasons.Count > 0;

                decimal total = 0m;
                foreach (ScpFoldedLine row in rows) total += row.PrintAmount;
                if (isBlocked) blocked++; else { ready++; grand += total; Approved.Add(job); }

                DataRow h = _dt.NewRow();
                h["Level"] = 0;
                h["What"] = (isBlocked ? "⚠  " : "") + job.Label + "  —  " + rows.Count +
                            (rows.Count == 1 ? " line" : " lines");
                h["Amount"] = total;
                h["Blocked"] = isBlocked;
                _dt.Rows.Add(h);

                foreach (ScpFoldedLine row in rows)
                {
                    DataRow l = _dt.NewRow();
                    l["Level"] = 1;
                    l["What"] = "      " + DescribeRow(row);
                    l["Qty"] = row.PrintQty;
                    l["UnitPrice"] = row.PrintUnitPrice;
                    l["Amount"] = row.PrintAmount;
                    l["Blocked"] = isBlocked;
                    _dt.Rows.Add(l);

                    // Only a merged row needs its machines spelled out; a single-machine row already
                    // names its machine on the line itself.
                    if (!row.IsMerged) continue;
                    foreach (MeterBillLine m in row.Members)
                    {
                        DataRow g = _dt.NewRow();
                        g["Level"] = 2;
                        g["What"] = "            " + Label(m) +
                                    (m.IsFlat ? "" : "   " + Num(m.Last) + " → " + Num(m.Current) +
                                                     " = " + Num(m.Usage));
                        g["Blocked"] = m.Current < m.Last;
                        _dt.Rows.Add(g);
                    }
                }

                foreach (string reason in reasons)
                {
                    DataRow w = _dt.NewRow();
                    w["Level"] = 3;
                    w["What"] = "      ⚠  Blocked: " + reason;
                    w["Blocked"] = true;
                    _dt.Rows.Add(w);
                }
            }

            GridPreview.DataSource = _dt;

            LblHeader.Text = _periodName + "  ·  " + ready + (ready == 1 ? " invoice" : " invoices") + " ready" +
                             (blocked > 0 ? "  ·  " + blocked + " blocked" : "") +
                             "  ·  total " + grand.ToString("#,##0.00");
            LblNote.Text = blocked > 0
                ? "Blocked customers are skipped — fix the reading and Generate again. " + _note
                : _note;
            BtnCreate.Text = ready == 0
                ? "Nothing to create"
                : "Create " + ready + (ready == 1 ? " invoice" : " invoices") + " (not yet submitted to LHDN)";
            BtnCreate.Enabled = ready > 0;
        }

        /// <summary>The line as the invoice will describe it, plus how many machines it covers.</summary>
        private string DescribeRow(ScpFoldedLine row)
        {
            MeterBillLine ln = row.Leader;
            string what = ln.IsRental || ln.IsFlat
                ? (string.IsNullOrEmpty(ln.MeterTypeName) ? "RENTAL" : ln.MeterTypeName)
                : (string.IsNullOrEmpty(ln.MeterTypeName) ? ln.ColorLabel : ln.MeterTypeName);
            if (!string.IsNullOrEmpty(ln.LineGroupCode)) what += " — " + ln.LineGroupCode;
            if (row.IsMerged) what += "  (" + row.Units + " machines)";
            else what += "  " + Label(ln);
            if (ln.IsFlat && ln.Charge == 0m) what += "  · FOC";
            if (!string.IsNullOrEmpty(ln.StrategyNote)) what += "  · " + ln.StrategyNote;
            return what;
        }

        private static string Label(MeterBillLine m)
        {
            string s = (m.SerialNumber ?? "").Trim();
            string n = (m.ItemName ?? "").Trim();
            if (s.Length > 0 && n.Length > 0) return n + " " + s;
            return s.Length > 0 ? s : n;
        }

        private static string Num(decimal d) { return d.ToString("#,##0.##"); }

        private void View_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            object lv = GridViewPreview.GetRowCellValue(e.RowHandle, "Level");
            object bl = GridViewPreview.GetRowCellValue(e.RowHandle, "Blocked");
            int level = lv == null || lv == DBNull.Value ? 0 : Convert.ToInt32(lv);
            bool blocked = bl != null && bl != DBNull.Value && Convert.ToBoolean(bl);

            if (level == 0)
            {
                e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;   // not new Font(): RowCellStyle fires per cell per paint
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(238, 242, 248);
            }
            else if (level == 2)
            {
                e.Appearance.ForeColor = System.Drawing.Color.DimGray;
            }
            if (blocked)
            {
                e.Appearance.ForeColor = System.Drawing.Color.Firebrick;
                if (level == 3)
                    e.Appearance.FontStyleDelta = System.Drawing.FontStyle.Bold;   // not new Font(): RowCellStyle fires per cell per paint
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
