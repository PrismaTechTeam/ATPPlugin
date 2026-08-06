using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using AutoCount.Data;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// Demo 28/07 #10: correct a wrongly-billed meter invoice with a REAL Sales Credit Note.
    /// The user keys the CORRECT reading per meter; Credit Copies = Billed - Correct, Credit
    /// Amount = copies x the invoice's effective rate (editable override). Generate creates the
    /// Sales CN (optionally knocked off against the invoice) and overrides the machine's LAST
    /// METER READING so later months bill from the corrected value.
    /// </summary>
    public partial class MeterCN_Form : XtraForm
    {
        private readonly DBSetting _db;
        private readonly long _invoiceDocKey;
        private readonly string _invoiceDocNo;
        private string _debtorCode = "";
        private DataTable _dt;
        private bool _recalc;   // guard against re-entrant CellValueChanged during recompute

        public MeterCN_Form()
        {
            InitializeComponent();
        }

        public MeterCN_Form(DBSetting db, long invoiceDocKey, string invoiceDocNo) : this()
        {
            _db = db;
            _invoiceDocKey = invoiceDocKey;
            _invoiceDocNo = invoiceDocNo ?? "";
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            DataTable hdr = _db.GetDataTable(
                "SELECT iv.DocNo, iv.DocDate, iv.DebtorCode, ISNULL(iv.Cancelled,'F') AS Cancelled, " +
                "ISNULL(d.CompanyName,'') AS CompanyName " +
                "FROM dbo.IV iv LEFT JOIN dbo.Debtor d ON d.AccNo = iv.DebtorCode " +
                "WHERE iv.DocKey = " + _invoiceDocKey, false);
            if (hdr.Rows.Count == 0)
            {
                XtraMessageBox.Show("Invoice not found.", "Correct with CN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }
            DataRow h = hdr.Rows[0];
            if (Convert.ToString(h["Cancelled"]) == "T")
            {
                XtraMessageBox.Show("This invoice is cancelled - nothing to correct.", "Correct with CN",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }
            _debtorCode = Convert.ToString(h["DebtorCode"]);
            this.LblInvoiceVal.Text = Convert.ToString(h["DocNo"]) + "    " +
                (h["DocDate"] == DBNull.Value ? "" : Convert.ToDateTime(h["DocDate"]).ToString("dd/MM/yyyy")) +
                "    " + _debtorCode + " — " + Convert.ToString(h["CompanyName"]);
            this.DtCNDate.EditValue = DateTime.Today;

            // Existing live CNs against this invoice — a new CN overrides again (latest wins).
            try
            {
                DataTable cns = _db.GetDataTable(
                    "SELECT DocNo FROM dbo.CN WHERE OurInvoiceNo = '" + _invoiceDocNo.Replace("'", "''") + "' " +
                    "AND ISNULL(Cancelled,'F') <> 'T' ORDER BY DocDate, DocNo", false);
                if (cns.Rows.Count > 0)
                {
                    List<string> nos = new List<string>();
                    foreach (DataRow r in cns.Rows) nos.Add(Convert.ToString(r["DocNo"]));
                    this.LblExistingCN.Text = "Already corrected by: " + string.Join(", ", nos.ToArray()) +
                        " — a new CN overrides again (latest wins).";
                    this.LblExistingCN.Visible = true;
                }
            }
            catch { }

            // The invoice's billed meter lines (flat/rental excluded — no reading to correct).
            _dt = _db.GetDataTable(
                "SELECT t.MeterTransKey, t.ServiceItemMeterTypeKey AS ItemMeterKey, t.ServiceItemKey AS ItemKey, " +
                "t.MeterTypeCode, t.MeterTransDate, t.MeterTransReading AS BilledReading, " +
                "i.ServiceItemNo, COALESCE(NULLIF(m.MachineSerialNo,''), i.SerialNumber) AS Serial, " +
                "ISNULL(mt.Description,'') AS MeterTypeName, " +
                "ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS ACItemCode, " +
                "ISNULL(c.DeptNo,'') AS DeptNo, ISNULL(c.ProjNo,'') AS ProjNo, " +
                "ISNULL(me.PeriodYear, YEAR(t.MeterTransDate)) AS PeriodYear, " +
                "ISNULL(me.PeriodMonth, MONTH(t.MeterTransDate)) AS PeriodMonth, " +
                "lg.LastReading, lg.[Usage] AS BilledUsage, lg.UnitPrice AS BilledRate, lg.Charge AS BilledCharge, " +
                "ISNULL(m.ChargesRate,0) AS CurrentRate, pc.PrevCorrectReading " +
                "FROM dbo.zSCP_MeterTrans t " +
                "JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "LEFT JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = t.MeterTypeCode " +
                // TOP 1 per meter: an invoice spanning two stamped periods must not duplicate the line.
                "OUTER APPLY (SELECT TOP 1 e.PeriodYear, e.PeriodMonth FROM dbo.zSCP2_MeterEntry e " +
                "  WHERE e.InvoicedDocKey = t.SalesInvoiceDocKey AND e.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                "  ORDER BY e.PeriodYear DESC, e.PeriodMonth DESC) me " +
                "OUTER APPLY (SELECT TOP 1 l.LastReading, l.[Usage], l.UnitPrice, l.Charge " +
                "  FROM dbo.zSCP2_MeterReadingLog l " +
                "  WHERE l.ItemMeterKey = t.ServiceItemMeterTypeKey AND l.Source='INVOICE' " +
                "    AND l.DocNo='" + _invoiceDocNo.Replace("'", "''") + "' ORDER BY l.LogKey DESC) lg " +
                // A LATER CN's base = the machine's CURRENT corrected reading (the newest correction
                // on ANY invoice of this meter), never the raw billed value - otherwise a second CN
                // would re-credit copies an earlier CN already credited. Not scoped to this invoice:
                // the agreed scenario corrects a LATER invoice first and then an EARLIER one, and
                // that second CN must start from where the first one left the meter, not from the
                // older invoice's own billed figure.
                "OUTER APPLY (SELECT TOP 1 p.MeterTransReading AS PrevCorrectReading FROM dbo.zSCP_MeterTrans p " +
                "  WHERE p.CNDocKey IS NOT NULL " +
                "    AND p.ServiceItemMeterTypeKey = t.ServiceItemMeterTypeKey ORDER BY p.MeterTransKey DESC) pc " +
                "WHERE t.SalesInvoiceDocKey = " + _invoiceDocKey + " AND t.CNDocKey IS NULL " +
                "AND ISNULL(mt.IsFlatCharge,'N') = 'N' " +
                "ORDER BY i.ServiceItemNo, t.MeterTypeCode", false);
            if (_dt.Rows.Count == 0)
            {
                XtraMessageBox.Show("No correctable meter lines on this invoice " +
                    "(rental / flat charges are corrected with a plain AutoCount credit note).",
                    "Correct with CN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }

            // Working columns: rate resolution + editable correction values. BaseReading = the value
            // the credit is measured FROM: the previous CN's corrected reading when one exists (a
            // second CN must not re-credit copies the first one already credited), else the billed.
            _dt.Columns.Add("Rate", typeof(decimal));
            _dt.Columns.Add("BaseReading", typeof(decimal));
            _dt.Columns.Add("CorrectReading", typeof(decimal));
            _dt.Columns.Add("CreditCopies", typeof(decimal));
            _dt.Columns.Add("CreditAmount", typeof(decimal));
            _dt.Columns.Add("AmountOverridden", typeof(bool));
            foreach (DataRow r in _dt.Rows)
            {
                r["Rate"] = r["BilledRate"] == DBNull.Value ? Dec(r["CurrentRate"]) : Dec(r["BilledRate"]);
                r["BaseReading"] = r["PrevCorrectReading"] == DBNull.Value
                    ? Dec(r["BilledReading"]) : Dec(r["PrevCorrectReading"]);
                r["CorrectReading"] = Dec(r["BaseReading"]);   // credit 0 until edited
                r["CreditCopies"] = 0m;
                r["CreditAmount"] = 0m;
                r["AmountOverridden"] = false;
            }
            this.GridLines.DataSource = _dt;
            this.GridViewLines.CellValueChanged +=
                new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(GridViewLines_CellValueChanged);
            this.GridViewLines.RowCellStyle +=
                new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(GridViewLines_RowCellStyle);
            RefreshTotal();
        }

        private static decimal Dec(object v)
        {
            return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
        }

        private void GridViewLines_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            if (_recalc || e.Column == null || e.RowHandle < 0) return;
            DataRow r = this.GridViewLines.GetDataRow(e.RowHandle);
            if (r == null) return;
            _recalc = true;
            try
            {
                if (e.Column.FieldName == "CorrectReading")
                {
                    decimal copies = Dec(r["BaseReading"]) - Dec(r["CorrectReading"]);
                    r["CreditCopies"] = copies;
                    r["CreditAmount"] = copies > 0m ? Math.Round(copies * Dec(r["Rate"]), 2) : 0m;
                    r["AmountOverridden"] = false;
                }
                else if (e.Column.FieldName == "CreditAmount")
                {
                    r["AmountOverridden"] = true;
                }
            }
            finally { _recalc = false; }
            RefreshTotal();
        }

        // Negative credit (correct reading HIGHER than billed) = red — blocked at Generate.
        private void GridViewLines_RowCellStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowCellStyleEventArgs e)
        {
            if (e.Column == null || e.Column.FieldName != "CreditCopies") return;
            DataRow r = this.GridViewLines.GetDataRow(e.RowHandle);
            if (r != null && Dec(r["CreditCopies"]) < 0m)
            {
                e.Appearance.ForeColor = System.Drawing.Color.White;
                e.Appearance.BackColor = System.Drawing.Color.FromArgb(198, 40, 40);
            }
        }

        private void RefreshTotal()
        {
            decimal total = 0m;
            foreach (DataRow r in _dt.Rows)
                if (Dec(r["CreditCopies"]) > 0m) total += Dec(r["CreditAmount"]);
            this.LblTotal.Text = "Credit total:  " + total.ToString("n2");
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            this.GridViewLines.PostEditor();
            this.GridViewLines.UpdateCurrentRow();

            // Pre-pass: every hard block surfaces BEFORE any confirmation prompt.
            foreach (DataRow r in _dt.Rows)
            {
                if (Dec(r["CreditCopies"]) < 0m)
                {
                    XtraMessageBox.Show("Machine " + Convert.ToString(r["ServiceItemNo"]) + " / " +
                        Convert.ToString(r["MeterTypeCode"]) + ": the correct reading is HIGHER than the billed " +
                        "reading - that is extra usage, issue a supplementary invoice instead of a CN.",
                        "Correct with CN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (Dec(r["CreditCopies"]) > 0m && Dec(r["CreditAmount"]) < 0m)
                {
                    XtraMessageBox.Show("Machine " + Convert.ToString(r["ServiceItemNo"]) + " / " +
                        Convert.ToString(r["MeterTypeCode"]) + ": the credit amount cannot be negative.",
                        "Correct with CN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            List<CnCorrectionLine> lines = new List<CnCorrectionLine>();
            decimal total = 0m;
            foreach (DataRow r in _dt.Rows)
            {
                decimal copies = Dec(r["CreditCopies"]);
                if (copies <= 0m) continue;
                if (r["BilledUsage"] != DBNull.Value && copies > Dec(r["BilledUsage"]))
                {
                    if (XtraMessageBox.Show("Machine " + Convert.ToString(r["ServiceItemNo"]) + " / " +
                            Convert.ToString(r["MeterTypeCode"]) + ": crediting " + copies.ToString("0.##") +
                            " copies is MORE than this invoice billed (" + Dec(r["BilledUsage"]).ToString("0.##") +
                            ").\r\nContinue anyway?", "Correct with CN",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;
                }
                CnCorrectionLine l = new CnCorrectionLine();
                l.ItemMeterKey = Convert.ToInt64(r["ItemMeterKey"]);
                l.ItemKey = Convert.ToInt64(r["ItemKey"]);
                l.MeterTypeCode = Convert.ToString(r["MeterTypeCode"]);
                l.MeterTypeName = Convert.ToString(r["MeterTypeName"]);
                l.ACItemCode = Convert.ToString(r["ACItemCode"]);
                l.ServiceItemNo = Convert.ToString(r["ServiceItemNo"]);
                l.Serial = Convert.ToString(r["Serial"]);
                l.DeptNo = Convert.ToString(r["DeptNo"]);
                l.ProjNo = Convert.ToString(r["ProjNo"]);
                // The credit is measured from the BASE (previous CN's corrected reading when one
                // exists) — the CN description and log then show "base -> correct" truthfully.
                l.BilledReading = Dec(r["BaseReading"]);
                l.BilledUsage = Dec(r["BilledUsage"]);
                l.CorrectReading = Dec(r["CorrectReading"]);
                l.CreditCopies = copies;
                l.Rate = Dec(r["Rate"]);
                l.CreditAmount = Dec(r["CreditAmount"]);
                l.AmountOverridden = r["AmountOverridden"] != DBNull.Value && Convert.ToBoolean(r["AmountOverridden"]);
                l.BilledTransDate = Convert.ToDateTime(r["MeterTransDate"]);
                l.PeriodYear = Convert.ToInt32(r["PeriodYear"]);
                l.PeriodMonth = Convert.ToInt32(r["PeriodMonth"]);
                lines.Add(l);
                total += l.CreditAmount;
            }
            if (lines.Count == 0)
            {
                XtraMessageBox.Show("Nothing to credit - key a lower Correct Reading on at least one meter.",
                    "Correct with CN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime cnDate = this.DtCNDate.EditValue is DateTime ? (DateTime)this.DtCNDate.EditValue : DateTime.Today;
            if (XtraMessageBox.Show(
                    "Create a Sales Credit Note against " + _invoiceDocNo + "?\r\n\r\n" +
                    "   • " + lines.Count + " meter line(s), credit total " + total.ToString("n2") + "\r\n" +
                    "   • Each machine's LAST METER READING becomes the corrected value\r\n" +
                    "     (future billing starts from it; the latest CN always wins)\r\n" +
                    (this.ChkKnockOff.Checked ? "   • The CN will be knocked off against this invoice\r\n" : "") +
                    "\r\nContinue?",
                    "Correct with CN", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                long cnDocKey;
                string koWarn;
                string cnNo = ScpCreditNoteBuilder.CreateCorrectionCN(_db, _debtorCode, _invoiceDocKey,
                    _invoiceDocNo, cnDate, this.TxtReason.Text.Trim(), this.ChkKnockOff.Checked,
                    lines, out cnDocKey, out koWarn);
                DialogResult open = XtraMessageBox.Show("Credit Note " + cnNo + " created." +
                    (koWarn.Length > 0 ? "\r\n\r\n⚠ " + koWarn : "") +
                    "\r\n\r\nOpen it in the Credit Note module now?",
                    "Correct with CN", MessageBoxButtons.YesNo,
                    koWarn.Length > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                if (open == DialogResult.Yes) OpenCreditNote(cnDocKey);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Credit note failed:\r\n" + ex.Message, "Correct with CN",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Opens the created CN in AutoCount's own Credit Note entry form (amend/delete there —
        // the reconcile picks up whatever the user does to it).
        private void OpenCreditNote(long cnDocKey)
        {
            try
            {
                AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand cmd =
                    AutoCount.Invoicing.Sales.CreditNote.CreditNoteCommand.Create(
                        AutoCount.Authentication.UserSession.CurrentUserSession, _db);
                AutoCount.Invoicing.Sales.CreditNote.CreditNote doc = cmd.Edit(cnDocKey);
                if (doc == null)
                {
                    XtraMessageBox.Show("Credit note not found.", "Correct with CN");
                    return;
                }
                using (AutoCount.Invoicing.Sales.CreditNote.FormCreditNoteEntry f =
                    new AutoCount.Invoicing.Sales.CreditNote.FormCreditNoteEntry(doc))
                {
                    f.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Open credit note failed:\r\n" + ex.Message, "Correct with CN",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
