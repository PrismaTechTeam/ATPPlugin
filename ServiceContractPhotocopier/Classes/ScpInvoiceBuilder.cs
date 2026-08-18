using System;
using System.Collections.Generic;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One billable meter line for the Meter Reading billing run (one machine + one colour role).
    /// </summary>
    public class MeterBillLine
    {
        public long ItemKey;
        public long ContractKey;
        public long ItemMeterKey;
        public string ContractNo = "";
        public string ItemNo = "";
        public string DebtorCode = "";
        public string SerialNumber = "";
        public string MeterTypeCode = "";
        public string MeterTypeName = "";
        public string ACItemCode = "";
        public string ItemName = "";      // service item no (label)
        public string ItemDesc = "";      // service item description (model/location text on the line)
        public string ColorLabel = "";   // "Black" or "Colour"
        public decimal Last;
        public decimal Current;
        public decimal Usage;
        public decimal Rate;
        public decimal MinCharges;
        public decimal Foc;
        public decimal RebatePct;
        public decimal Charge;
        public bool UseMin;
        public bool IsFlat;   // flat/rental meter: bill qty 1 x rate regardless of readings
        // --- strategy pipeline (P0 plumbing; wired progressively in P5a-P5c) ---
        public string StrategyCode = "";   // contract's strategy in force for this billing run
        public string StrategyNote = "";   // human-readable outcome ("WAIVED: ...", "NET: ...")
        public bool NetBilling;            // FOC-REBATE strategy with NetBilling='Y'
        public bool WaivedRental;          // WAIVE-TARGET met -> rental billed 0 this period
        public bool IsRental;              // flat AND a rental (RA-*) type — distinguishes from MIN-*
        public decimal BillCopies;         // the NET qty the invoice line bills (usage - FOC)
        public string MultiPriceCode = ""; // tiered-pricing ladder code (per-meter override or meter type)
        public decimal EffUnitPrice;       // the resolved per-copy price (multi-price tier, or flat rate)
        public int FocResetCount = 1;      // FOC allowance multiplier = reset periods in this billed span
                                           // (1 = monthly reset on a monthly bill; 4 ≈ weekly reset billed monthly)
        public bool IsCommittedMin;        // a "MIN ..." committed-minimum meter — bills the TOP-UP to the
                                           // committed amount over the item's print charges, always shown
        public decimal CommittedAmount;    // the committed minimum (the MIN meter's Minimum Charges)
        public decimal PrintedAmount;      // the item's actual print (BK/CL) charges the top-up was measured against
        public bool AlwaysBill;            // keep this line on the invoice even when its charge is 0 (transparency)
        public DateTime? RentalStartDate;  // n/N anchor
        public int RentalMonths;           // N (0 = open-ended, no n/N text)
        public char RentalBasis = 'A';     // A accrual / P prepayment
        public string MachineStatus = "";  // API fetch status (ONLINE/OFFLINE/"") — drives the
                                           // advanced per-status invoice number format
        public string TrackingId = "";     // offline reading report id ("MR-yymmdd-nnn") — the job's
                                           // distinct ids become the invoice Reference No
        public bool IsGroupItem;           // line belongs to the contract's GROUP "machine" — its
                                           // MIN/WAIVE sums span the WHOLE fleet, not one machine
        public string ModelCode = "";      // the machine's stock item (zSCP2_Item.ItemCode) — the bucket
                                           // when a contract groups lines by model
        public string LineGroupCode = "";  // the "HEAVY DUTY" / "MEDIUM DUTY" word printed on the line
                                           // (zSCP2_Item.LineGroupCode) -- a description, never a split
        public string MergeGroupCode = ""; // which LINE this machine prints on when the contract merges
                                           // (zSCP2_Item.MergeGroupCode); '' = follow the line mode
        /// <summary>The contract has a Billing Format, so it bills the way the customer's own
        /// invoices do: rebate deducted as copies, cents rounded half away from zero, and a line
        /// worth 0.00 still printed. Off = the engine as it behaved before, so an existing contract's
        /// money does not move until someone picks a format for it.</summary>
        public bool NewMoneyRules;
        /// <summary>Copies removed by the rebate (new rules only) — printed as "Meter Rebate Qty (3%)".</summary>
        public decimal RebateQty;
        // --- Rental-Waive contra meter (master-style; the engine decides firing at Generate) ---
        public bool IsWaiveMeter;
        public int WaiveFirstNMonths;      // 0 = no window condition
        public decimal WaiveTargetAmount;  // 0 = no usage condition (0 & 0 = ALWAYS waive)
        public decimal WaivePartialThreshold;  // partial band (RM, demo 28/07 #24): charges reach RM X...
        public decimal WaivePartialAmount;     // ...-> waive RM Y off the rental (0/0 = no partial band)
        public string WaiveScope = "BKCL";
        public DateTime? EffStartDate;     // effective service start (item, else contract) — RENTAL-FREE-N anchor fallback
        public DateTime? LastDate;
        /// <summary>API LastAuditDate of the CURRENT reading — used as the MeterTrans date so the
        /// next period's "Last Read Date" is the real meter-read date (falls back to now if absent).</summary>
        public DateTime? AuditDate;
        // #16 "billing period follows contract date": when set, the invoice DISPLAYS these contract-
        // cycle dates instead of LastDate/AuditDate (Previous = PeriodStart, Current = PeriodEnd).
        // DISPLAY ONLY — stamps, staging and the reading log always keep the real audit dates.
        public DateTime? PeriodStart;
        public DateTime? PeriodEnd;
    }

    /// <summary>
    /// Shared meter-billing math + AutoCount invoice construction. Mirrors the proven
    /// MeterTypeTransactionEntry_Form pipeline (RecalcRow + BuildInvoiceDocument) so the new
    /// Meter Reading Integration flow produces identical invoices.
    /// </summary>
    public static class ScpInvoiceBuilder
    {
        /// <summary>THE single meter-charge engine (used by BOTH the grid preview and the invoice, so they
        /// can never diverge). NET convention (user decision 2026-07-22):
        ///   billable = max(0, usage - FOC)                         (FOC copies really deducted)
        ///   unitprice = flat-per-tier multi-price by usage, else the flat ChargesRate
        ///   charge = round(billable x unitprice, 2) x (1 - rebate%)  (rebate really deducted)
        ///   charge floored at MinCharges (committed minimum still bills when FOC covers all usage)
        /// Flat/rental meters bill qty 1 x rate (FOC Qty = free-rental months -> RM0 this period).
        /// Pass the pre-loaded multi-price ladders (null = flat rate only).</summary>
        /// <summary>
        /// Feedback #6 "Group Rental with same unit price and QTY": collapse a job's RENTAL lines so
        /// there is one invoice row PER METER TYPE carrying the number of units, instead of one row
        /// per machine. Their own invoice reads
        /// <c>RA-32 UNIT ... MEDIUM HEAVY DUTY "55 cpm"   32 UNIT   908.20   29,062.40</c>.
        ///
        /// <paramref name="groupQty"/> maps each surviving "leader" line to its unit count;
        /// <paramref name="folded"/> lists the lines whose unit was counted there and which must not
        /// print a row of their own.
        ///
        /// Only merges what is genuinely identical: same meter type, same per-unit charge, same
        /// department/project, and no per-machine story to tell. Anything carrying a strategy note
        /// (a waived or committed-minimum rental explains ITSELF on the line) stays separate —
        /// merging those would throw the explanation away.
        /// </summary>
        /// Superseded by <see cref="ScpInvoiceLayout.Fold"/>, which folds meter lines as well as
        /// rentals and takes its rules from the contract's Billing Format. The legacy predicate and
        /// key live on there verbatim for contracts that have not been given a format.
        public static void ComputeCharge(MeterBillLine ln,
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<decimal[]>> ladders)
        {
            if (ln.IsFlat)
            {
                ln.Usage = 0m; ln.BillCopies = 0m;
                if (ln.Foc > 0m) { ln.Charge = 0m; ln.UseMin = false; ln.EffUnitPrice = 0m; return; }
                decimal flat = ln.Rate;
                if (flat < ln.MinCharges) flat = ln.MinCharges;
                ln.UseMin = (ln.Rate == 0m && ln.MinCharges > 0m);
                ln.EffUnitPrice = ln.Rate;
                ln.Charge = flat;
                return;
            }

            decimal usage = ln.Current - ln.Last;
            if (usage < 0m) usage = 0m;
            ln.Usage = usage;

            // Billed copies + effective per-copy price come from ONE of two mechanisms:
            //   (a) multi-price ladder present -> MARGINAL charge (its 0.00 first band IS the FOC allowance);
            //       billed = usage - freeCopies, effective price = grossCharge / billed (blended).
            //   (b) no ladder -> flat rate with the FOCQty column deducted (NET).
            // FOC reset accrual: the free allowance refreshes every reset period, so a bill spanning N
            // reset periods grants N x the base allowance (FocResetCount; 1 = the simple monthly case).
            int resetN = ln.FocResetCount < 1 ? 1 : ln.FocResetCount;
            decimal billed, effUnit;
            if (ScpMultiPrice.HasLadder(ladders, ln.MultiPriceCode))
            {
                decimal freeCopies;
                // Scale the ladder's boundaries (its FOC bands + tier breaks are "per reset period").
                decimal gross = ScpMultiPrice.MarginalCharge(ladders, ln.MultiPriceCode, usage, resetN, out freeCopies);
                billed = usage - freeCopies;
                if (billed < 0m) billed = 0m;
                effUnit = billed > 0m ? Math.Round(gross / billed, 6) : 0m;
            }
            else
            {
                billed = usage - ln.Foc * resetN;
                if (billed < 0m) billed = 0m;
                effUnit = ln.Rate;
            }
            // Rebate. The customer's invoices deduct it as COPIES, not as a discount on the amount:
            // Kastam's 4WE04767 goes 5,232 gross, less 500 FOC = 4,732, less floor(4,732 x 3%) = 141,
            // and bills qty 4,591. Tangkak's MR2607.1416 bills qty 1,882 at 53.64 where the amount
            // form gives 1,940 at 53.63. Pontian proves the flooring happens per machine and is then
            // summed -- its merged line prints 1,522, where 2% of the merged 76,244 would be 1,524.
            //
            // Only contracts on the new engine get this; everything else keeps the amount form.
            decimal charge;
            if (ln.RebatePct > 0m && ln.NewMoneyRules)
            {
                ln.RebateQty = Math.Floor(billed * ln.RebatePct / 100m);
                if (ln.RebateQty < 0m) ln.RebateQty = 0m;
                billed -= ln.RebateQty;
                if (billed < 0m) billed = 0m;
                charge = Round2(billed * effUnit, ln.NewMoneyRules);
            }
            else
            {
                ln.RebateQty = 0m;
                decimal sub = Round2(billed * effUnit, ln.NewMoneyRules);
                charge = ln.RebatePct > 0m ? Round2(sub * (1m - ln.RebatePct / 100m), ln.NewMoneyRules) : sub;
            }
            ln.BillCopies = billed;
            ln.EffUnitPrice = effUnit;

            // Minimum-charge floor. A committed minimum still bills even when the allowance covers all usage.
            if (charge < ln.MinCharges) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
            else { ln.Charge = charge; ln.UseMin = false; }
        }

        /// <summary>
        /// Round to cents. AutoCount rounds half away from zero unless the book asks for banker's
        /// rounding (DecimalSetting), and the customer's invoices agree with it: Pasir Gudang's
        /// colour line is 5,693 x 0.285 = 1,622.505 and prints 1,622.51, where .NET's default
        /// ToEven gives 1,622.50. Kept opt-in so an existing contract's cents do not move underneath
        /// anyone mid-month.
        /// </summary>
        private static decimal Round2(decimal v, bool newMoneyRules)
        {
            return newMoneyRules ? Math.Round(v, 2, MidpointRounding.AwayFromZero) : Math.Round(v, 2);
        }

        /// <summary>
        /// Builds (but does NOT save) an Invoice document from the given billable lines.
        /// LEGACY FORMAT — mirrors how the customer's V8 book created meter invoices (verified against
        /// v8_atp_main salesinvoice/salesinvoiceitem, e.g. MR2603.0691):
        ///   • header Description = "Billing- [ref]"; Remark1 = the CSSI/contract ref.
        ///   • ONE detail line per meter: Description = "{item desc} S/N:{serial}- {meter name}",
        ///     Qty = billable copies, UnitPrice = rate; the reading breakdown lives in
        ///     FurtherDescription as the legacy 16-line structured block (legend + 15 values).
        ///   • Minimum-charge lines: Qty 1 × the minimum, FurtherDescription "*** Minimum Charges ***".
        /// </summary>
        public static AutoCount.Invoicing.Sales.Invoice.Invoice BuildInvoice(
            DBSetting db, string debtorCode, string refDocNo, string description,
            DateTime docDate, DateTime readingDate, List<MeterBillLine> lines)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(UserSession.CurrentUserSession, db);

            AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();
            if (doc == null)
                throw new InvalidOperationException("Failed to create a new invoice document.");

            // Meter invoices draw their number from the book's own Document Numbering Format (native
            // AutoCount numbering, per-month running set -> MR2607.0001 style). The format NAME is
            // configurable in Service Option; it is only applied when it actually exists, so a book
            // without it falls back to the IV default numbering.
            try
            {
                string fmtName = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_INVOICE_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_INVOICE_DOCNO_FORMAT).Trim();
                // ADVANCED numbering: ONLINE machines -> one format, OFFLINE -> another (a mixed
                // invoice counts as ONLINE); lines with no API status keep the default format.
                if (ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                        ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ADVANCED, false))
                {
                    bool anyOnline = false, anyOffline = false;
                    foreach (MeterBillLine l0 in lines)
                    {
                        string ms = (l0.MachineStatus ?? "").Trim().ToUpperInvariant();
                        if (ms.StartsWith("ONLINE")) anyOnline = true;
                        else if (ms.StartsWith("OFFLINE")) anyOffline = true;
                    }
                    string adv = anyOnline
                        ? ServiceContractPhotocopier.Data.PumsConfig.Get(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ONLINE, "").Trim()
                        : (anyOffline
                            ? ServiceContractPhotocopier.Data.PumsConfig.Get(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_OFFLINE, "").Trim()
                            : "");
                    if (adv.Length > 0) fmtName = adv;
                }
                if (fmtName.Length > 0)
                {
                    System.Data.DataTable fmt = db.GetDataTable(
                        "SELECT TOP 1 Name FROM dbo.DocNoFormat WHERE DocType='IV' AND Name='" + fmtName.Replace("'", "''") + "'", false);
                    if (fmt.Rows.Count > 0) doc.DocNoFormatName = fmtName;
                }
            }
            catch { }

            doc.DebtorCode = debtorCode;
            doc.DocDate = docDate;
            doc.Description = description;
            // THREE different fields, and only one of them is the box labelled "Ref" on AutoCount's
            // invoice screen — that one is IV.Ref. We were writing RefDocNo (IV.RefDocNo) and Remark1,
            // both of which held the offline tracking id correctly and neither of which the operator
            // can see there. Hence "the Ref is still empty" on an invoice whose id was stamped fine.
            doc.Ref = refDocNo ?? "";
            doc.RefDocNo = refDocNo ?? "";
            doc.Remark1 = refDocNo ?? "";
            if (doc.DetailCount > 0) doc.ClearDetails();

            // Each meter line's invoice detail carries its CONTRACT's Department + Project so the invoice
            // is analysed exactly like the contract it bills. Loaded once per distinct contract.
            System.Collections.Generic.Dictionary<long, string[]> contractDeptProj = LoadContractDeptProj(db, lines);

            bool legacyDataBlock = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_LEGACY_DATA_BLOCK,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INVOICE_LEGACY_DATA_BLOCK);

            // Invoice line description source (user decision 2026-07-27): DEFAULT = the AutoCount stock
            // item's own description (master convention — "BK COPY + PRINT A4&A3"); the Plugin Option
            // can switch back to the meter type name. One lookup per distinct item code.
            bool descFromItem = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_DESC_FROM_ITEM,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INVOICE_DESC_FROM_ITEM);
            System.Collections.Generic.Dictionary<string, string> itemDescByCode =
                new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (descFromItem)
            {
                System.Collections.Generic.List<string> codes = new System.Collections.Generic.List<string>();
                foreach (MeterBillLine l0 in lines)
                    if (!string.IsNullOrEmpty(l0.ACItemCode) && !codes.Contains(l0.ACItemCode)) codes.Add(l0.ACItemCode);
                if (codes.Count > 0)
                {
                    System.Text.StringBuilder inList = new System.Text.StringBuilder();
                    foreach (string c in codes)
                    {
                        if (inList.Length > 0) inList.Append(",");
                        inList.Append("N'").Append(c.Replace("'", "''")).Append("'");
                    }
                    try
                    {
                        System.Data.DataTable it = db.GetDataTable(
                            "SELECT ItemCode, ISNULL(Description,'') AS Description FROM dbo.Item WHERE ItemCode IN (" + inList + ")", false);
                        foreach (System.Data.DataRow r in it.Rows)
                            itemDescByCode[Convert.ToString(r["ItemCode"])] = Convert.ToString(r["Description"]).Trim();
                    }
                    catch { }
                }
            }

            // How many rows this invoice prints. Each contract's RentalLineMode / MeterLineMode
            // decides whether machines collapse together; a contract with no Billing Format keeps
            // exactly the legacy behaviour (rentals fold on the old rule, usage never folds).
            // Merging is presentation only — meter stamps, reading history and any later CN still
            // see the individual machines.
            System.Collections.Generic.List<ScpFoldedLine> rows = ScpInvoiceLayout.Fold(db, lines);

            foreach (ScpFoldedLine row in rows)
            {
                MeterBillLine ln = row.Leader;

                string lineDept = "", lineProj = "";
                string[] dp;
                if (ln.ContractKey > 0 && contractDeptProj.TryGetValue(ln.ContractKey, out dp))
                { lineDept = dp[0]; lineProj = dp[1]; }

                // ComputeCharge sets UseMin authoritatively (charge was floored to the minimum).
                bool minBilled = ln.UseMin;
                string block;
                if (ln.IsCommittedMin)
                    // Committed-minimum meter: show the committed amount, the item's actual print charges,
                    // and the top-up billed — so the customer sees WHY the amount is what it is (transparency).
                    block = "*** MINIMUM COMMITTED PRINT CHARGES ***\r\n" +
                            "Committed Minimum : " + ln.CommittedAmount.ToString("#,##0.00") + "\r\n" +
                            "Print Charges     : " + ln.PrintedAmount.ToString("#,##0.00") + "\r\n" +
                            "Top-Up Billed     : " + ln.Charge.ToString("#,##0.00");
                else if (minBilled)
                    block = "*** Minimum Charges ***";
                else
                    // The 17-field block is CODED for the legacy V8 design, which reads those lines
                    // by position. On any other layout it prints as a wall of raw numbers, and the
                    // human-readable reading rows below the charge already say the same thing — so
                    // it is off unless a design that parses it is in use.
                    block = legacyDataBlock ? ComposeBreakdown(ln, readingDate) : "";

                // Charge row (NET convention): Qty = billed copies (usage - FOC), UnitPrice = the resolved
                // per-copy price (multi-price tier or flat rate), Rebate % as a line discount — so the line
                // total = ComputeCharge's NET charge and the invoice matches the grid exactly. Minimum-floor
                // and flat/rental lines bill 1 x the (already-final) charge.
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(ln.ACItemCode)) dtl.ItemCode = ln.ACItemCode;
                string itemMasterDesc;
                // The meter's own wording, then which machines the line covers — see
                // ComposeFoldedDescription. Without the second part a per-machine layout prints
                // several identical lines and the customer cannot tell them apart.
                dtl.Description = ComposeFoldedDescription(row,
                    descFromItem && !string.IsNullOrEmpty(ln.ACItemCode)
                        && itemDescByCode.TryGetValue(ln.ACItemCode, out itemMasterDesc)
                        && itemMasterDesc.Length > 0
                    ? itemMasterDesc                    // master convention: the stock item's description
                    : ComposeLineDescription(ln));      // fallback / option OFF: meter type name
                if (minBilled || ln.IsFlat || ln.BillCopies <= 0m)
                {
                    // Grouped rental: Qty = how many machines share this row, UnitPrice stays the
                    // PER-UNIT rental, so the line reads "3 UNIT x 300.00" and totals correctly.
                    dtl.Qty = row.Units > 1m ? row.Units : 1m;
                    dtl.UnitPrice = ln.Charge;
                }
                else
                {
                    // Merged usage: Qty is the members' billable copies added up, priced once at the
                    // rate they share. Rounding the row once is what the customer's own invoices do —
                    // HSI prints 3,048.36 where the per-machine charges add to 3,048.37.
                    dtl.Qty = row.IsMerged ? row.BillCopies : ln.BillCopies;
                    dtl.UnitPrice = row.IsMerged ? row.PrintUnitPrice : ln.EffUnitPrice;
                    // Under the new rules the rebate is already out of BillCopies as copies, so a
                    // line discount here would take it a second time.
                    if (ln.RebatePct > 0m && !ln.NewMoneyRules)
                        dtl.Discount = ln.RebatePct.ToString("0.##") + "%";
                    // Each meter keeps its own rate, so a merged usage line is worth what its
                    // machines are worth -- stated, not recomputed from a rate that cannot represent
                    // several. Qty x UnitPrice would round to 2,651.27 where the machines cost
                    // 2,651.24.  NOTE: needs confirming against a generated invoice that AutoCount
                    // keeps this and does not recompute it from Qty x UnitPrice on save.
                    if (row.IsMerged) dtl.SubTotal = row.PrintAmount;
                }
                // AutoCount's native FOC Qty column carries the free copies actually APPLIED to this
                // bill (user request): the ladder's free band or the meter's Free Qty allowance.
                // Flat lines (rental / waive / MIN) have no copies to give free.
                if (!ln.IsFlat)
                {
                    decimal focCol = row.IsMerged ? row.FocApplied
                                                  : (ln.Usage - ln.BillCopies - ln.RebateQty);
                    if (focCol < 0m) focCol = 0m;
                    if (focCol > 0m) dtl.FOCQty = focCol;
                }
                dtl.FurtherDescription = block;
                // Department + Project = the billed contract's (per line, since a grouped invoice can
                // span several contracts). Empty contract values leave the detail's defaults untouched.
                if (!string.IsNullOrEmpty(lineDept)) dtl.DeptNo = lineDept;
                if (!string.IsNullOrEmpty(lineProj)) dtl.ProjNo = lineProj;

                // Strategy outcome ("RENTAL FREE month 1/1 (strategy)", "WAIVED: charges >= target",
                // "PARTIAL WAIVE 90%: ...") — its own text row directly under the charge row, so the
                // CUSTOMER sees why a rental is 0.00 or reduced (user transparency rule).
                if (!string.IsNullOrEmpty(ln.StrategyNote))
                    AddTextRow(doc, ln.StrategyNote, block);

                // Reading text rows — exactly the master's content: Current / Previous / (FOC when >0)
                // / Usage, each carrying the same block, then a blank separator row. Dates dd/MM/yyyy,
                // readings plain digits (no thousands separators — the master prints "126699").
                // A committed-minimum meter has no reading, so it skips these (its committed/print/top-up
                // breakdown is already on the charge row's description).
                if (!ln.IsCommittedMin)
                {
                // #16: contract-period mode prints the period as ONE range line — user-specified
                // format "(start - end)" — and the reading rows drop their dates (the customer must
                // never see cross-month reading dates). Normal mode is unchanged.
                bool periodMode = ln.PeriodStart.HasValue && ln.PeriodEnd.HasValue;
                if (periodMode)
                    AddTextRow(doc, "Billing Period (" + ln.PeriodStart.Value.ToString("dd/MM/yyyy") +
                                    " - " + ln.PeriodEnd.Value.ToString("dd/MM/yyyy") + ")", block);
                // A merged row shows the group's summed readings — no machine has these numbers, and
                // that is exactly what the customer's invoices print (Rompin's AMR2607.0087 shows
                // 349,707, its four machines added up). Where the members were read on different
                // days the row prints the span rather than picking one machine's date and implying
                // the others were read then too.
                DateTime curDate = (row.IsMerged ? row.CurDate : ln.AuditDate) ?? readingDate;
                string curDateStr = DateSpan(row, true, curDate);
                string lastDateStr = DateSpan(row, false, ln.LastDate ?? DateTime.MinValue);
                decimal showCurrent = row.IsMerged ? row.Current : ln.Current;
                decimal showLast = row.IsMerged ? row.Last : ln.Last;

                AddTextRow(doc, periodMode
                    ? "Current Meter Reading : " + Num(showCurrent)
                    : "Current Meter Reading (" + curDateStr + ") : " + Num(showCurrent), block);

                AddTextRow(doc, periodMode
                    ? "Previous Meter Reading : " + Num(showLast)
                    : "Previous Meter Reading (" + lastDateStr + ") : " + Num(showLast), block);

                // Which machines are on this row. A single-machine row already says so on the charge
                // line, so only a merged one needs the list — Pontian prints exactly this, all six
                // serials against one BK line of 74,722.
                if (row.IsMerged)
                    AddTextRow(doc, "S/N : " + SerialList(row), block);

                // FOC actually APPLIED to this bill: for a ladder meter that is the ladder's own free
                // band (usage − billed copies) — its FOCQty column is ignored by the engine, so printing
                // the raw column here used to show a FOC that was never deducted. Flat (rental) lines
                // keep the column value (= free months).
                decimal focApplied = row.IsMerged ? row.FocApplied
                                                  : (ln.IsFlat ? ln.Foc : (ln.Usage - ln.BillCopies - ln.RebateQty));
                if (focApplied < 0m) focApplied = 0m;
                if (focApplied > 0m)
                    AddTextRow(doc, "Meter FOC Qty : " + Num(focApplied), block);

                // The copies the rebate removed, shown the way Kastam and Tangkak show them
                // ("Meter Rebate Qty (3%) : 141") so the arithmetic on the line is followable.
                decimal rebQty = row.IsMerged ? row.RebateQty : ln.RebateQty;
                if (rebQty > 0m)
                    AddTextRow(doc, "Meter Rebate Qty (" + ln.RebatePct.ToString("0.##") + "%) : " + Num(rebQty), block);

                AddTextRow(doc, "Meter Charges Usage : " + Num(row.IsMerged ? row.BillCopies : ln.BillCopies), block);
                }   // end reading rows (skipped for committed-minimum meters)

                // Blank separator between meters (the master prints one). Like every other text row it
                // is excluded from the subtotal, which is also what keeps its empty Description away
                // from the e-Invoice payload — see AddTextRow.
                AddTextRow(doc, "", "");
            }
            return doc;
        }

        /// <summary>
        /// A description-only detail row: the reading breakdown, the strategy note, the blank separator
        /// between meters. Carries no money and no account.
        ///
        /// <para><b>AddToSubTotal = false is what makes these rows legal for LHDN.</b>
        /// AutoCount submits every detail row it finds with
        /// <c>(DtlType = 'N' OR 'D' OR 'V') AND AddToSubTotal = 'T'</c>
        /// (<c>JsonInvoiceHelper.cs:682</c>), then throws <c>DetailDescIsEmpty</c> on a blank description
        /// (<c>:695</c>) and <c>DetailMissingClassification</c> on a row with no classification code
        /// (<c>:712</c>). <c>AddDetail()</c> stamps every new row <c>DtlType='N'</c>,
        /// <c>AddToSubTotal='T'</c> (<c>InvoicingDocument.cs:2289-2291</c>), so the blank separator alone
        /// made EVERY meter invoice fail MyInvois validation, and the reading rows would have been
        /// submitted as RM0 line items. Clearing the flag drops them from that query while leaving the
        /// printed document byte-for-byte identical — these rows have no Qty or UnitPrice, so they
        /// contributed nothing to the subtotal in the first place.</para>
        /// </summary>
        /// <summary>The reading date to print. One machine, or several read on the same day, gives a
        /// single date; a merged row whose members were read over several days prints the span, so
        /// the invoice never claims a reading was taken on a day it was not.</summary>
        private static string DateSpan(ScpFoldedLine row, bool current, DateTime fallback)
        {
            if (!row.IsMerged)
                return fallback == DateTime.MinValue ? "" : fallback.ToString("dd/MM/yyyy");

            DateTime? lo = null, hi = null;
            foreach (MeterBillLine m in row.Members)
            {
                DateTime? d = current ? m.AuditDate : m.LastDate;
                if (!d.HasValue) continue;
                if (!lo.HasValue || d.Value < lo.Value) lo = d;
                if (!hi.HasValue || d.Value > hi.Value) hi = d;
            }
            if (!lo.HasValue) return fallback == DateTime.MinValue ? "" : fallback.ToString("dd/MM/yyyy");
            return lo.Value.Date == hi.Value.Date
                ? lo.Value.ToString("dd/MM/yyyy")
                : lo.Value.ToString("dd/MM") + "-" + hi.Value.ToString("dd/MM/yyyy");
        }

        /// <summary>The serials on a merged row, in the order the machines were billed.</summary>
        private static string SerialList(ScpFoldedLine row)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (MeterBillLine m in row.Members)
            {
                string s = (m.SerialNumber ?? "").Trim();
                if (s.Length == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(s);
            }
            return sb.ToString();
        }

        private static void AddTextRow(AutoCount.Invoicing.Sales.Invoice.Invoice doc,
            string description, string furtherDescription)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceDetail d = doc.AddDetail();
            d.Description = description;
            if (!string.IsNullOrEmpty(furtherDescription)) d.FurtherDescription = furtherDescription;
            d.AccNo = null;
            d.AddToSubTotal = false;
        }

        // Contract Department + Project for every distinct ContractKey referenced by the lines.
        // Returns { ContractKey -> [DeptNo, ProjNo] }; contracts with none map to empty strings.
        private static System.Collections.Generic.Dictionary<long, string[]> LoadContractDeptProj(
            DBSetting db, List<MeterBillLine> lines)
        {
            System.Collections.Generic.Dictionary<long, string[]> map =
                new System.Collections.Generic.Dictionary<long, string[]>();
            System.Collections.Generic.HashSet<long> keys = new System.Collections.Generic.HashSet<long>();
            foreach (MeterBillLine ln in lines) if (ln.ContractKey > 0) keys.Add(ln.ContractKey);
            if (keys.Count == 0) return map;
            try
            {
                string inList = string.Join(",", new List<long>(keys).ConvertAll(k => k.ToString()).ToArray());
                System.Data.DataTable dt = db.GetDataTable(
                    "SELECT ContractKey, ISNULL(DeptNo,'') AS DeptNo, ISNULL(ProjNo,'') AS ProjNo " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (System.Data.DataRow r in dt.Rows)
                    map[Convert.ToInt64(r["ContractKey"])] = new string[]
                    { Convert.ToString(r["DeptNo"]), Convert.ToString(r["ProjNo"]) };
            }
            catch { }
            return map;
        }

        // Master convention (verified against v8_atp_main salesinvoiceitem): the line description is
        // the METER TYPE's description ONLY — e.g. "BK COPY + PRINT A4&A3". Machine-specific texts like
        // "PUSPEN ... S/N:4NL20240- BK COPY+PRINT A4&A3" exist in the master only because the customer
        // created per-machine meter types carrying that full text as the meter description; the item's
        // own description is never prefixed by the billing code.
        private static string ComposeLineDescription(MeterBillLine ln)
        {
            if (!string.IsNullOrEmpty(ln.MeterTypeName)) return ln.MeterTypeName.Trim();
            if (!string.IsNullOrEmpty(ln.MeterTypeCode)) return ln.MeterTypeCode.Trim();
            return ln.ItemName;
        }

        /// <summary>
        /// What a line says about the machines it covers — the customer's own proposal, and what
        /// their invoices already print:
        /// <code>
        ///   MONTHLY RENTAL (11/36)              BK COPY + PRINT A4&amp;A3
        ///   MODEL:iR-ADV 4545i  S/N:YAJ01479    MODEL:iR-ADV 4545i  3 UNIT
        /// </code>
        ///
        /// <para>This is what makes a per-machine layout readable. Without it, three machines of the
        /// same model on the same duty produce three lines reading "MONTHLY RENTAL — MEDIUM DUTY",
        /// and nothing on the invoice says which machine each one is for.</para>
        ///
        /// <para>The three shapes are deliberately different. One machine names it. A row merged by
        /// model names the model and counts the units, because that is what the merge key was. A row
        /// merged ACROSS models lists the models, because the count alone would hide that they are
        /// not all the same thing.</para>
        ///
        /// <para>Opt-in with the rest: a contract with no Billing Format keeps the bare meter-type
        /// description it has always had.</para>
        /// </summary>
        public static string ComposeFoldedDescription(ScpFoldedLine row, string baseText)
        {
            if (row == null || row.Leader == null) return baseText;
            MeterBillLine ln = row.Leader;
            if (!ln.NewMoneyRules) return baseText;

            string head = (baseText ?? "").Trim();
            // The instalment counter belongs to the rental sentence, not to the machine list.
            if (ln.IsRental && ln.RentalMonths > 0)
                head += " (" + ScpInvoiceLayout.RentalMonthNo(ln) + "/" + ln.RentalMonths + ")";

            System.Text.StringBuilder tail = new System.Text.StringBuilder();
            List<string> models = new List<string>();
            foreach (MeterBillLine m in row.Members)
            {
                string mc = (m.ModelCode ?? "").Trim();
                if (mc.Length > 0 && !models.Contains(mc)) models.Add(mc);
            }
            if (models.Count > 0)
                tail.Append("MODEL:").Append(string.Join(", ", models.ToArray()));

            if (!row.IsMerged)
            {
                string sn = (ln.SerialNumber ?? "").Trim();
                if (sn.Length > 0)
                {
                    if (tail.Length > 0) tail.Append("  ");
                    tail.Append("S/N:").Append(sn);
                }
            }
            else
            {
                if (tail.Length > 0) tail.Append("  ");
                tail.Append(row.Units).Append(" UNIT");
            }
            return tail.Length == 0 ? head : head + "\r\n" + tail;
        }

        // The legacy 16-line More Description block (legend + 15 values), copied verbatim from the
        // customer's V8 meter invoices.
        private static string ComposeBreakdown(MeterBillLine ln, DateTime readingDate)
        {
            // #16: contract-period mode swaps the DISPLAYED dates in fields 7/13/14/15; the ACTUAL
            // audit dates are always appended as fields 16/17 so report designs can print either.
            DateTime actualCur = ln.AuditDate ?? readingDate;
            DateTime cur = ln.PeriodEnd ?? actualCur;
            System.Globalization.CultureInfo en = new System.Globalization.CultureInfo("en-US");
            DateTime? lastDate = ln.PeriodStart ?? ln.LastDate;
            string lastLong = lastDate.HasValue ? LongDate(lastDate.Value, en) : "";
            string lastShort = lastDate.HasValue ? lastDate.Value.ToString("yyyyMMdd") : "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("1. Mt Type; 2. Mt Name; 3. Min Chrg; 4. Chrg Rate; 5. FOC Qty; 6. Rebate %; " +
                          "7. Last Reading Date; 8. Last Reading Mt; 9. Mt Usage; 10. Total Chrg; 11. Multi Price; " +
                          "12. Rebate Qty; 13. Current Reading Date; 14. Last Reading Short Date; 15. Current Reading Short Date; " +
                          "16. Actual Last Audit; 17. Actual Current Audit");
            // FOC as APPLIED (ladder free band / reset-scaled column), consistent with the FOC text row.
            decimal focShown = ln.IsFlat ? ln.Foc : (ln.Usage - ln.BillCopies);
            if (focShown < 0m) focShown = 0m;
            sb.AppendLine(ln.MeterTypeCode);
            sb.AppendLine(ComposeLineDescription(ln));
            sb.AppendLine(Num(ln.MinCharges));
            sb.AppendLine(Num(ln.Rate));
            sb.AppendLine(Num(focShown));
            sb.AppendLine(Num(ln.RebatePct));
            sb.AppendLine(lastLong);
            sb.AppendLine(Num(ln.Last));
            sb.AppendLine(Num(ln.Usage));
            sb.AppendLine(Num(ln.Charge));
            // multi price: per-meter override ladders are keyed '#<meterkey>' internally — print
            // the customer-friendly "(Custom)" instead of the raw key.
            string mpShown = ln.MultiPriceCode ?? "";
            if (mpShown.StartsWith("#")) mpShown = "(Custom)";
            sb.AppendLine(mpShown);
            sb.AppendLine("0");                      // rebate qty
            sb.AppendLine(LongDate(cur, en));
            sb.AppendLine(lastShort);
            sb.AppendLine(cur.ToString("yyyyMMdd"));
            // 16/17: the ACTUAL audit dates (same as 14/15 unless contract-period mode swapped them).
            sb.AppendLine(ln.LastDate.HasValue ? ln.LastDate.Value.ToString("yyyyMMdd") : "");
            sb.Append(actualCur.ToString("yyyyMMdd"));
            return sb.ToString();
        }

        private static string Num(decimal v) { return v.ToString("0.######"); }

        // Legacy long stamp: "24 Feb 2026 5:10:06 pm" (lowercase am/pm).
        private static string LongDate(DateTime d, System.Globalization.CultureInfo en)
        {
            return d.ToString("d MMM yyyy h:mm:ss", en) + " " + d.ToString("tt", en).ToLowerInvariant();
        }
    }
}
