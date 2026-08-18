using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using ServiceContractPhotocopier.Classes;
using ServiceContractPhotocopier.ServiceContract.OperationForms;

namespace ServiceContractPhotocopier
{
    /// <summary>
    /// What this contract's invoice will look like — before a single reading exists.
    ///
    /// <para>The settings that decide an invoice's shape are spread over three screens and none of
    /// them shows the result: the Billing Format says how lines merge, the machines carry the prices,
    /// and Lines &amp; Price can override the rental. Whether that adds up to one line or seven is
    /// only discoverable today by generating a real invoice, which needs a month of readings first.
    /// This answers the question at the moment the contract is being set up, which is when it is
    /// actually being asked.</para>
    ///
    /// <para><b>It is the same engine.</b> The lines are real <see cref="MeterBillLine"/>s put through
    /// <see cref="ScpInvoiceLayout.FoldWith"/>, <see cref="ScpInvoiceBuilder.ComputeCharge"/> and
    /// <see cref="ScpInvoiceBuilder.ComposeFoldedDescription"/> — the same three the billing run uses.
    /// A preview that computed its own answer would eventually promise something Generate does not
    /// produce, and would be worse than no preview at all.</para>
    ///
    /// <para>Only the READINGS are invented: every machine is given a month of usage, so the copies
    /// and the money are illustrative while the SHAPE — how many invoices, how many lines, what each
    /// line says and which machines it covers — is exactly what will come out.</para>
    ///
    /// <para>It renders as a printed document rather than a grid of rows, because a grid answers
    /// "how many lines" and the question actually being asked is "does this read like an invoice" —
    /// whether the wording, the units and the money on the page make sense to the person paying it.
    /// One sheet per invoice, and the printing system brings zoom, print and PDF export with it.</para>
    /// </summary>
    public partial class SampleInvoice_Form : XtraForm
    {
        private readonly AutoCount.Data.DBSetting _db;
        private readonly List<ItemEditData> _items;
        private readonly string _contractNo;
        private readonly string _debtor;
        private readonly char _rentalMode;
        private readonly char _meterMode;
        private readonly bool _hasFormat;
        private readonly string _formatName;
        private readonly bool _rentalSeparate;
        private readonly bool _perMachine;
        private readonly Dictionary<string, decimal> _groupPrices;
        /// <summary>Whether this book still folds rentals the old way — what a contract with no
        /// Billing Format is actually billed under.</summary>
        private bool _legacyRentalFold;
        private string _footer = "";

        public SampleInvoice_Form()
        {
            InitializeComponent();
        }

        public SampleInvoice_Form(AutoCount.Data.DBSetting db, List<ItemEditData> items,
            string contractNo, string debtor, string formatName, bool hasFormat,
            char rentalMode, char meterMode, bool rentalSeparate, bool perMachine,
            Dictionary<string, decimal> groupPrices) : this()
        {
            _db = db;
            _items = items ?? new List<ItemEditData>();
            _contractNo = contractNo ?? "";
            _debtor = debtor ?? "";
            _formatName = formatName ?? "";
            _hasFormat = hasFormat;
            _rentalMode = rentalMode;
            _meterMode = meterMode;
            _rentalSeparate = rentalSeparate;
            _perMachine = perMachine;
            _groupPrices = groupPrices ?? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            _legacyRentalFold = ScpInvoiceLayout.LegacyRentalFold(_db);
            List<SampleInvoiceDoc> docs = Build();
            ScpSampleInvoiceReport rpt = ScpSampleInvoiceReport.Create(docs, _footer);
            rpt.CreateDocument();
            PrintPreview.PrintingSystem = rpt.PrintingSystem;
        }

        // ---------- the sample fleet ----------

        /// <summary>Turns the contract's machines into the bill lines a month would produce. The
        /// prices are the contract's own; only the meter readings are invented.</summary>
        private List<MeterBillLine> BuildLines()
        {
            List<MeterBillLine> lines = new List<MeterBillLine>();
            DateTime period = DateTime.Today;
            int seed = 0;

            foreach (ItemEditData d in _items)
            {
                if (d == null || d.IsGroupItem || d.Inactive || d.Meters == null) continue;
                seed++;
                foreach (DataRow mr in d.Meters.Rows)
                {
                    if (mr.RowState == DataRowState.Deleted) continue;
                    string type = Str(mr, "MeterTypeCode").Trim();
                    if (type.Length == 0) continue;
                    string role = Str(mr, "MeterRole").Trim().ToUpperInvariant();

                    MeterBillLine l = new MeterBillLine();
                    l.ContractKey = 1;
                    l.ContractNo = _contractNo;
                    l.ItemKey = seed;
                    l.ItemName = string.IsNullOrEmpty(d.ServiceItemNo) ? "<NEW>" : d.ServiceItemNo;
                    l.SerialNumber = d.SerialNumber ?? "";
                    l.ModelCode = d.ItemCode ?? "";
                    l.MergeGroupCode = d.MergeGroupCode ?? "";
                    l.LineGroupCode = d.LineGroupCode ?? "";
                    l.MeterTypeCode = type;
                    l.MeterTypeName = Str(mr, "Description");
                    l.ACItemCode = type;
                    l.NewMoneyRules = _hasFormat;
                    l.AuditDate = period;
                    l.LastDate = period.AddMonths(-1);
                    l.PeriodEnd = period;
                    l.MinCharges = Dec(mr, "MinimumCharges");
                    l.Rate = Dec(mr, "ChargesRate");
                    l.Foc = Dec(mr, "FOCQty");
                    l.RebatePct = Dec(mr, "RebateQtyInPercent");
                    l.MultiPriceCode = Str(mr, "MeterMultiPriceCode");
                    l.WaiveScope = Str(mr, "WaiveScope");
                    l.CommitScope = Str(mr, "CommitScope");

                    bool isWaive = ScpStrategy.IsWaiveRole(role, false);
                    bool isCommit = ScpStrategy.IsCommittedMinRole(role, type, l.MinCharges);
                    bool isRental = !isWaive && !isCommit && ScpStrategy.IsRentalRole(role, type);

                    if (isRental || isWaive || isCommit)
                    {
                        l.IsFlat = true;
                        l.IsRental = isRental;
                        l.IsWaiveMeter = isWaive;
                        l.RentalMonths = 36;
                        l.RentalStartDate = period.AddMonths(-12);
                        if (isCommit)
                        {
                            // A committed minimum bills the shortfall, and the shortfall is not known
                            // until the copies are in. The line is shown for its shape, priced 0.
                            l.IsCommittedMin = true;
                            l.CommittedAmount = l.MinCharges;
                            l.AlwaysBill = true;
                            l.Charge = 0m;
                            l.StrategyNote = "COMMITTED MIN " + l.MinCharges.ToString("n2") +
                                             " — bills the shortfall, worked out at Generate";
                        }
                        else if (isWaive)
                        {
                            l.Charge = -Math.Abs(l.MinCharges != 0m ? l.MinCharges : l.Rate);
                            l.StrategyNote = "RENTAL WAIVE — fires on its own terms at Generate";
                        }
                        else
                        {
                            ScpInvoiceBuilder.ComputeCharge(l, null);
                        }
                    }
                    else
                    {
                        // A month of copies. Varied per machine so a merged line visibly sums them.
                        decimal usage = role == "CL" ? 900 + seed * 137 : 4200 + seed * 613;
                        l.ColorLabel = role == "CL" ? "Colour" : (role == "BK" ? "Black" : "Usage");
                        l.Last = 100000 + seed * 1000;
                        l.Current = l.Last + usage;
                        ScpInvoiceBuilder.ComputeCharge(l, null);
                    }
                    lines.Add(l);
                }
            }
            ApplyGroupPrices(lines);
            return lines;
        }

        /// <summary>The contract's agreed rental price, stamped onto every member of its group before
        /// anything is folded — the same order the billing run uses, and what makes a priced group
        /// merge into one line at all.</summary>
        private void ApplyGroupPrices(List<MeterBillLine> lines)
        {
            if (_groupPrices.Count == 0 || _rentalMode == ScpBillingFormat.LINE_PER_MACHINE) return;
            foreach (MeterBillLine l in lines)
            {
                if (!l.IsRental) continue;
                string key = ScpRentalGroupPrice.GroupKeyFor(_rentalMode, l.ModelCode, l.MergeGroupCode);
                decimal price;
                if (!_groupPrices.TryGetValue(key, out price) || price <= 0m) continue;
                l.Rate = price;
                l.MinCharges = 0m;
                ScpInvoiceBuilder.ComputeCharge(l, null);
            }
        }

        // ---------- the invoices ----------

        /// <summary>Which invoice a line lands on. The two split flags are the contract's own, so a
        /// preview shows the same number of invoices Generate will create.</summary>
        private string InvoiceOf(MeterBillLine l)
        {
            string who = _perMachine ? l.ItemName : "this contract";
            string what = _rentalSeparate && (l.IsRental || l.IsWaiveMeter) ? "rental" : "";
            if (!_perMachine && what.Length == 0) return "Invoice 1 — " + _debtor;
            if (!_perMachine) return "Invoice — rental";
            return what.Length > 0 ? "Invoice — " + who + " (rental)" : "Invoice — " + who;
        }

        private List<SampleInvoiceDoc> Build()
        {
            List<SampleInvoiceDoc> docs = new List<SampleInvoiceDoc>();
            List<MeterBillLine> lines = BuildLines();
            if (lines.Count == 0)
            {
                LblHeader.Text = "This contract has no machines with meters yet — nothing to bill.";
                return docs;
            }

            // Group into invoices first, then fold each one on its own, exactly as the billing run
            // does: folding across an invoice split would merge lines that never meet on paper.
            List<string> order = new List<string>();
            Dictionary<string, List<MeterBillLine>> byInvoice =
                new Dictionary<string, List<MeterBillLine>>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine l in lines)
            {
                string inv = InvoiceOf(l);
                if (!byInvoice.ContainsKey(inv)) { order.Add(inv); byInvoice[inv] = new List<MeterBillLine>(); }
                byInvoice[inv].Add(l);
            }

            decimal grand = 0m;
            int totalLines = 0;
            foreach (string inv in order)
            {
                List<ScpFoldedLine> rows = ScpInvoiceLayout.FoldWith(
                    byInvoice[inv], _rentalMode, _meterMode, _hasFormat, _legacyRentalFold);

                SampleInvoiceDoc doc = new SampleInvoiceDoc();
                doc.Title = "TAX INVOICE";
                doc.DebtorCode = _debtor;
                doc.ContractNo = _contractNo;
                doc.Note = inv;
                docs.Add(doc);

                foreach (ScpFoldedLine row in rows)
                {
                    totalLines++;
                    // The description is composed by the SAME method the invoice builder uses, so the
                    // MODEL: / S/N: / n UNIT block and the (n/N) counter read exactly as they will
                    // print. Its second line is the sub-description on paper.
                    string composed = ScpInvoiceBuilder.ComposeFoldedDescription(row, DescriptionOf(row.Leader));
                    string head = composed, sub = "";
                    int br = composed.IndexOf("\r\n", StringComparison.Ordinal);
                    if (br >= 0) { head = composed.Substring(0, br); sub = composed.Substring(br + 2); }

                    SampleInvoiceLine sl = new SampleInvoiceLine();
                    sl.Description = head;
                    sl.SubDescription = sub;
                    sl.Covers = Covers(row);
                    sl.Note = row.Leader.StrategyNote ?? "";
                    sl.Qty = row.PrintQty;
                    sl.UnitPrice = row.PrintUnitPrice;
                    sl.Amount = row.PrintAmount;
                    // A rental, a waive and a committed minimum have no copies to print.
                    sl.ShowQty = !(row.Leader.IsFlat || row.Leader.IsCommittedMin || row.Leader.IsWaiveMeter);
                    doc.Lines.Add(sl);
                    grand += row.PrintAmount;
                }
            }

            LblHeader.Text =
                (_hasFormat ? "Format:  " + _formatName : "No Billing Format — billing the legacy way") +
                "        " + order.Count + (order.Count == 1 ? " invoice" : " invoices") +
                ",  " + totalLines + (totalLines == 1 ? " line" : " lines") +
                ",  " + grand.ToString("n2") + " a month";
            _footer =
                "The SHAPE is real — how many invoices, how many lines, what each line says and which " +
                "machines it covers all come from this contract's own settings, through the same engine " +
                "Generate uses. Only the meter READINGS are invented, so the copies and the money are " +
                "illustrative. A committed minimum prints 0.00 because its charge is the shortfall, " +
                "which cannot be known until the copies are in.";
            return docs;
        }

        private static string DescriptionOf(MeterBillLine l)
        {
            string t = (l.MeterTypeName ?? "").Trim();
            if (t.Length > 0) return t;
            if (l.IsRental) return "MONTHLY RENTAL";
            if (l.IsCommittedMin) return "MINIMUM COMMITTED PRINT CHARGES";
            if (l.IsWaiveMeter) return "RENTAL WAIVE";
            return (l.ColorLabel ?? "").Length > 0 ? l.ColorLabel.ToUpperInvariant() + " COPIES" : l.MeterTypeCode;
        }

        /// <summary>Which machines a printed line actually covers — the question the shape is being
        /// checked for.</summary>
        private static string Covers(ScpFoldedLine row)
        {
            if (!row.IsMerged) return row.Leader.ItemName + "  " + row.Leader.SerialNumber;
            List<string> names = new List<string>();
            foreach (MeterBillLine m in row.Members)
            {
                string s = (m.SerialNumber ?? "").Trim();
                names.Add(s.Length > 0 ? s : m.ItemName);
            }
            return row.Members.Count + " machines:  " + string.Join(", ", names.ToArray());
        }

        private static string Str(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToString(r[col]) : "";
        }

        private static decimal Dec(DataRow r, string col)
        {
            return r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDecimal(r[col]) : 0m;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
