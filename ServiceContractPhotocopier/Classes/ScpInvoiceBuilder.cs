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
        /// <summary>Which INVOICE this machine is billed on — the branch or department the
        /// customer settles separately. A bill group splits invoices and nothing else: the deal,
        /// its prices and its terms stay contract-wide.</summary>
        public string BillGroupCode = "";

        public bool AlwaysBill;            // keep this line on the invoice even when its charge is 0 (transparency)
        public DateTime? RentalStartDate;  // n/N anchor
        public int RentalMonths;           // N (0 = open-ended, no n/N text)
        public char RentalBasis = 'A';     // A accrual / P prepayment
        /// <summary>Feedback ATP-10: the rental months this line bills -- 1, or 2 when a machine joined
        /// a prepaid contract after the bill that should have carried its first month.</summary>
        public int RentalMonthsBilled = 1;
        /// <summary>ATP-10: the prepaid rental months this line pays for (0 = accrual).</summary>
        public int RentalFromN, RentalToN;
        /// <summary>ATP-10: free-rental months a two-month line used up (counted down at Generate).</summary>
        public int RentalFreeUsed;
        public string MachineStatus = "";  // API fetch status (ONLINE/OFFLINE/"") — drives the
                                           // advanced per-status invoice number format
        public string TrackingId = "";     // offline reading report id ("MR-yymmdd-nnn") — the job's
                                           // distinct ids become the invoice Reference No
        public bool IsGroupItem;           // line belongs to the contract's GROUP "machine" — its
                                           // MIN/WAIVE sums span the WHOLE fleet, not one machine
        public string ModelCode = "";      // the machine's stock item (zSCP2_Item.ItemCode) — the bucket
                                           // when a contract groups lines by model
        /// <summary>What the machine line under this charge names -- the model, the duty label, or
        /// both. From the contract's Billing Format; 'B' when it has none, which is what every
        /// contract printed before the setting existed.</summary>
        public char MachineLineShows = ScpBillingFormat.MACHINE_LINE_BOTH;

        /// <summary>Does the line name the MODEL under the charge, and does it name the SERIALS?
        ///
        /// <para>Two questions, because they have different answers. A fleet line covering
        /// thirty-six machines wants its model named and does not want thirty-six serial numbers
        /// printed underneath it -- and the single three-valued setting these replace could not say
        /// that. Default to true so a contract nobody has answered for prints what it always
        /// printed.</para></summary>
        public bool ShowModel = true;
        public bool ShowSerial = true;

        /// <summary>Does the line say how many machines it covers -- "(2 UNIT)"? Only where the
        /// Qty column does not already say it: a rental bills machines, a meter bills copies.</summary>
        public bool ShowUnits = true;

        /// <summary>Which line this machine's BLACK and COLOUR print on. Separate from the rental's
        /// group because the two questions have different answers: one agreed rental across the
        /// fleet while every machine still bills its own copies is the commonest deal here.</summary>
        public string MergeGroupCodeMeter = "";

        public string LineGroupCode = "";  // the "HEAVY DUTY" / "MEDIUM DUTY" word printed on the line
                                           // (zSCP2_Item.LineGroupCode) -- a description, never a split
        /// <summary>Whose print charges a committed minimum is measured against: 'S' this machine,
        /// 'G' its merge group, 'C' the whole contract. Ignored unless IsCommittedMin.</summary>
        public string CommitScope = "S";
        public string MergeGroupCode = ""; // which LINE this machine prints on when the contract merges
                                           // (zSCP2_Item.MergeGroupCode); '' = follow the line mode
        /// <summary>The contract has a Billing Format, so it bills the way the customer's own
        /// invoices do: rebate deducted as copies, cents rounded half away from zero, and a line
        /// worth 0.00 still printed. Off = the engine as it behaved before, so an existing contract's
        /// money does not move until someone picks a format for it.</summary>
        public bool NewMoneyRules;

        /// <summary>Feedback ATP-3: the contract prices its tiers BAND BY BAND -- each band's copies at
        /// that band's rate -- instead of the whole month at the band it reached. From the contract's
        /// TierMode ('I'); off = the threshold rule every contract had before.</summary>
        public bool TierIncremental;
        /// <summary>Band by band: the bands this line was charged in, {copies, rate, amount} each,
        /// ascending, amounts rounded per band (before any old-rule rebate discount, which the
        /// invoice prints per band). Null when the line was not split.</summary>
        public System.Collections.Generic.List<decimal[]> TierBands;
        /// <summary>True when <see cref="TierBands"/> are the GROUP's pooled bands, the same list on
        /// every machine of a group ladder -- printed once for the group, never added up.</summary>
        public bool TierBandsAreGroup;
        /// <summary>How many machines share <see cref="TierBands"/> when they are a group's (ATP-3).
        /// The bands are the whole group's copies, so they may only print on a row that holds all of
        /// them; a row holding fewer prints its machines' share as one line.</summary>
        public int TierGroupSize;
        /// <summary>Copies removed by the rebate (new rules only) — printed as "Meter Rebate Qty (3%)".</summary>
        public decimal RebateQty;
        // --- Rental-Waive contra meter (master-style; the engine decides firing at Generate) ---
        public bool IsWaiveMeter;
        /// <summary>This rental is FREE this month, because a free-months deal covers it — not
        /// credited back afterwards. FOC and a waive are two different things and the customer
        /// can tell them apart: a free month prints ONE line with nothing to pay; a waive prints
        /// the rent and then a credit against it.</summary>
        public bool FreeThisMonth;
        /// <summary>Kept out of the invoice altogether. A free-months waive meter has nothing to
        /// contra: it made the rental free instead, and its own row would be a second line saying
        /// so.</summary>
        public bool Suppressed;
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
                if (ln.RentalMonthsBilled > 1)
                {
                    // ATP-10: a machine that joined a prepaid contract after the bill that should have
                    // carried its first month pays for two months on this one. Free-rental months
                    // count month by month: one left means one of the two is free.
                    int months = ln.RentalMonthsBilled;
                    int free = ln.Foc > 0m ? (int)Math.Min(Math.Floor(ln.Foc), months) : 0;
                    decimal one = ln.Rate < ln.MinCharges ? ln.MinCharges : ln.Rate;
                    ln.RentalFreeUsed = free;
                    ln.UseMin = false;
                    ln.EffUnitPrice = one;
                    ln.Charge = one * (months - free);
                    return;
                }
                if (ln.Foc > 0m)
                {
                    // One free month used. On a contract that prints its 0.00 rent (the new layout) the
                    // line is saved on the invoice, and only RentalFreeUsed counts the months down there
                    // (MeterInvoiceGenerator) -- without it the rent stayed free for good (29/9: MR2609.0841
                    // printed "RENTAL FREE - FOC month" and left 2 free months at 2).
                    ln.RentalFreeUsed = 1;
                    ln.Charge = 0m; ln.UseMin = false; ln.EffUnitPrice = 0m;
                    return;
                }
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

            // Billed copies + effective per-copy price:
            //   (a) multi-price ladder -> the band the month REACHED prices the lot (threshold), or,
            //       on a contract that asks for it, each band's copies at its own rate (ATP-3).
            //   (b) no ladder -> flat rate.
            // Either way the free copies are the meter's Free Qty (ATP-3: FOC lives there and nowhere
            // else; a 0.00 band left in an old ladder still counts as the allowance it always was).
            // FOC reset accrual: the free allowance refreshes every reset period, so a bill spanning N
            // reset periods grants N x the base allowance (FocResetCount; 1 = the simple monthly case).
            int resetN = ln.FocResetCount < 1 ? 1 : ln.FocResetCount;
            decimal billed, effUnit;
            ln.TierBands = null;
            ln.TierBandsAreGroup = false;
            bool bandByBand = false;
            if (ScpMultiPrice.HasLadder(ladders, ln.MultiPriceCode))
            {
                decimal freeCopies, bandRate;
                // Scale the ladder's boundaries (its FOC bands + tier breaks are "per reset period").
                ScpMultiPrice.LadderCharge(ladders, ln.MultiPriceCode, usage, resetN,
                                           out freeCopies, out bandRate);
                billed = usage - freeCopies - ln.Foc * resetN;
                if (billed < 0m) billed = 0m;
                // The band's own rate, not a figure derived from the total. A ladder line prints a
                // price the customer agreed to and can multiply back to the amount.
                effUnit = bandRate;
                bandByBand = ln.TierIncremental;
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
            if (bandByBand)
            {
                // ATP-3, band by band: the copies left after the free copies (and a rebate taken as
                // copies) are laid over the priced bands, each band's share at its own rate --
                // 1,548 over "1,000 at 0.024, then 0.020" is 24.00 + 10.96 = 34.96. One invoice row
                // per band, so every row multiplies out.
                List<decimal[]> tiers = ScpMultiPrice.Tiers(ladders, ln.MultiPriceCode);
                ln.TierBands = ScpMultiPrice.Slices(tiers, billed, resetN, ln.NewMoneyRules);
                decimal sum = 0m;
                foreach (decimal[] b in ln.TierBands)
                    sum += (ln.RebatePct > 0m && !ln.NewMoneyRules)
                        ? Round2(b[2] * (1m - ln.RebatePct / 100m), ln.NewMoneyRules)   // the discount each row prints
                        : b[2];
                charge = sum;
                // One band: that band's own rate, so the single row multiplies out as any other. Two or
                // more: what the line would print as one rate -- a display figure only; the rows carry
                // the money.
                if (ln.TierBands.Count == 1) effUnit = ln.TierBands[0][1];
                else if (ln.TierBands.Count > 1) effUnit = Math.Round(sum / billed, 6, MidpointRounding.AwayFromZero);
                if (ln.TierBands.Count < 2) ln.TierBands = null;   // one band: an ordinary line
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
                string itemMasterDesc;
                // The meter's own wording, then which machines the line covers — see
                // ComposeFoldedDescription. Without the second part a per-machine layout prints
                // several identical lines and the customer cannot tell them apart.
                string composed = ComposeFoldedDescription(row,
                    descFromItem && !string.IsNullOrEmpty(ln.ACItemCode)
                        && itemDescByCode.TryGetValue(ln.ACItemCode, out itemMasterDesc)
                        && itemMasterDesc.Length > 0
                    ? itemMasterDesc                    // master convention: the stock item's description
                    : ComposeLineDescription(ln));      // fallback / option OFF: meter type name
                // IVDTL.Description is nvarchar(100) and AutoCount rejects anything longer outright --
                // Generate dies with "The value violates the MaxLength limit of this column" and no
                // invoice is written at all. A merged row is exactly what overflows it: the charge
                // name, then the models, then a serial for every machine on the row.
                //
                // The extra lines were only ever newlines inside one field, and the document already
                // prints text-only rows underneath a charge. So the first line stays the charge's own
                // description and the rest become those rows -- nothing is lost, nothing is truncated,
                // and no number of machines can break the run.
                string[] descLines = composed.Split(new string[] { BREAK }, StringSplitOptions.None);
                List<string> extraDescRows = new List<string>();
                for (int di = 1; di < descLines.Length; di++)
                    if (descLines[di].Trim().Length > 0) extraDescRows.Add(descLines[di]);

                // ---- the charge row -----------------------------------------------------------
                //
                // A ladder on THRESHOLD (every contract's rule before ATP-3) prices the lot at the
                // band reached, so the row has one agreed rate. A contract priced BAND BY BAND
                // charges each slice at its own rate; that line has no single price, and printing
                // charge-over-copies would show a rate that appears nowhere in the deal -- so there
                // each band becomes its own row (below), and Qty x Unit Price = Amount holds on
                // every one of them.
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(ln.ACItemCode)) dtl.ItemCode = ln.ACItemCode;
                dtl.Description = Fit(descLines[0]);
                // What the row prints is ScpFoldedLine's decision, in ONE place. This used to be a
                // second copy of the same three cases, and the copies drifted: a merged WAIVE is
                // flat, so it fell into the rental case and printed "3 machines x the leader's -450"
                // = -1,350 on a row whose own note said 900.00 came off -- crediting the customer
                // 450 for a machine that never reached its target. The preview, which reads these
                // properties, was right; only the posted document was wrong, which is the worst
                // place for the two to disagree.
                //
                //   merged minimum / waive : a SUM of separate amounts -> qty 1, price = the sum
                //   flat (rental, min)     : qty = machines sharing the row, price = per-unit
                //   usage                  : qty = billable copies, priced once at the shared rate
                dtl.Qty = row.PrintQty;
                dtl.UnitPrice = row.PrintUnitPrice;
                if (!(row.IsMerged && (ln.IsCommittedMin || ln.IsWaiveMeter))
                    && !(minBilled || ln.IsFlat || ln.BillCopies <= 0m))
                {
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

                // ATP-3, band by band: the row above becomes Tier 1, and each further band gets its
                // own row -- same item, its own copies and rate -- so every row multiplies out and the
                // rows add up to the charge. The FOC column stays on Tier 1; the text rows that follow
                // (strategy note, serials, readings) come after the last band.
                List<decimal[]> bands = row.Bands;
                if (bands != null && !minBilled)
                {
                    bool oldDiscount = ln.RebatePct > 0m && !ln.NewMoneyRules;
                    for (int bi = 0; bi < bands.Count; bi++)
                    {
                        AutoCount.Invoicing.Sales.Invoice.InvoiceDetail bd = bi == 0 ? dtl : doc.AddDetail();
                        if (bi > 0)
                        {
                            // The item and the analysis codes, but not the legacy data block: that is the
                            // machine's readings, and it belongs to the line once, on Tier 1.
                            if (!string.IsNullOrEmpty(ln.ACItemCode)) bd.ItemCode = ln.ACItemCode;
                            if (!string.IsNullOrEmpty(lineDept)) bd.DeptNo = lineDept;
                            if (!string.IsNullOrEmpty(lineProj)) bd.ProjNo = lineProj;
                        }
                        bd.Description = Fit(descLines[0] + "  (Tier " + (bi + 1) + ")");
                        bd.Qty = bands[bi][0];
                        bd.UnitPrice = bands[bi][1];
                        if (oldDiscount) bd.Discount = ln.RebatePct.ToString("0.##") + "%";
                        else bd.SubTotal = bands[bi][2];
                    }
                }
                // A machine whose group was priced band by band but whose row does not hold the whole
                // group (another meter type, another invoice) prints its share as one line. Its rate is
                // the group's blended figure, which does not multiply back to the share exactly, so the
                // amount is stated -- the machine's charge, as the reading log keeps it.
                else if (bands == null && ln.TierBandsAreGroup && !row.IsMerged && !minBilled && ln.BillCopies > 0m)
                    dtl.SubTotal = row.PrintAmount;

                // Strategy outcome ("RENTAL FREE month 1/1 (strategy)", "WAIVED: charges >= target",
                // "PARTIAL WAIVE 90%: ...") — its own text row directly under the charge row, so the
                // CUSTOMER sees why a rental is 0.00 or reduced (user transparency rule).
                foreach (string extraRow in extraDescRows)
                    AddTextRow(doc, extraRow, block);


                string note = row.IsMerged && ln.IsCommittedMin ? MergedMinimumNote(row)
                            : (row.IsMerged && ln.IsWaiveMeter ? MergedWaiveNote(row) : ln.StrategyNote);
                if (!string.IsNullOrEmpty(note))
                    AddTextRow(doc, note, block);

                // Reading text rows -- Current / Previous / (S/N when merged) / (FOC) / (rebate)
                // / Usage, each carrying the same More Description block, composed by the method the
                // Sample Invoice preview also calls so the two cannot drift.
                foreach (string readingRow in ComposeReadingRows(row, readingDate))
                    AddTextRow(doc, readingRow, block);

                // Blank separator between meters (the master prints one). Like every other text row it
                // is excluded from the subtotal, which is also what keeps its empty Description away
                // from the e-Invoice payload — see AddTextRow.
                AddTextRow(doc, "", "");
            }
            return doc;
        }

        /// <summary>The sentence a MERGED waive prints. Each member was judged on its own terms, so
        /// the row says how many of them actually fired — a row reading −450 across three machines is
        /// otherwise indistinguishable from one where all three were waived at 150 each.</summary>
        public static string MergedWaiveNote(ScpFoldedLine row)
        {
            int fired = 0;
            decimal off = 0m;
            foreach (MeterBillLine m in row.Members)
                if (m.Charge < 0m) { fired++; off += -m.Charge; }
            return fired == 0
                ? "RENTAL WAIVE · none of the " + row.Units.ToString("0") + " machines reached its target"
                : "RENTAL WAIVE · " + fired + " of " + row.Units.ToString("0") +
                  " machines reached its target · " + off.ToString("n2") + " off the rental";
        }

        /// <summary>How a machine is named in a breakdown row: its service item number and serial
        /// where both exist, otherwise whichever it has.</summary>
        private static string NameOf(MeterBillLine m)
        {
            string n = (m.ItemName ?? "").Trim();
            string sn = (m.SerialNumber ?? "").Trim();
            if (n.Length > 0 && sn.Length > 0) return n + " " + sn;
            return n.Length > 0 ? n : sn;
        }

        /// <summary>The sentence a MERGED minimum prints. Each member kept its own arithmetic — its own
        /// floor against its own copies — and this row is their shortfalls added up.
        ///
        /// <para>It cannot reuse a member's sentence: three machines with floors of 350, 400 and 450
        /// have no single "MINIMUM 350.00" to quote, and quoting one would be wrong about the other two.
        /// It states what the row IS instead: how many machines, what their copies came to, what the
        /// shortfalls came to. A machine that met its own floor contributes nothing and is simply not
        /// part of the shortfall — which is why the total is never "floors minus copies".</para></summary>
        public static string MergedMinimumNote(ScpFoldedLine row)
        {
            decimal floors = 0m, printed = 0m, shortfall = 0m;
            int shortCount = 0;
            foreach (MeterBillLine m in row.Members)
            {
                floors += m.CommittedAmount;
                printed += m.PrintedAmount;
                shortfall += m.Charge;
                if (m.Charge > 0m) shortCount++;
            }
            // Written short on purpose: this lands in a 100-character column, and the machine count
            // and every figure have to survive.
            string colourWord = row.Leader == null ? "" : ScpCommittedMin.ScopeWord(row.Leader.WaiveScope);
            return "MIN " + (colourWord.Length > 0 ? colourWord + " " : "") + "per machine · floors " + floors.ToString("n2") +
                   " · copies " + printed.ToString("n2") +
                   (shortfall > 0m
                        ? " · " + shortCount + " of " + row.Units.ToString("0") + " short " + shortfall.ToString("n2")
                        : " · all over their minimum");
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
            return SerialsOf(row.Members);
        }

        private static string SerialsOf(List<MeterBillLine> machines)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (MeterBillLine m in machines)
            {
                string s = (m.SerialNumber ?? "").Trim();
                if (s.Length == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(s);
            }
            return sb.ToString();
        }

        /// <summary>The most this column takes. AutoCount types it d_ItemDescription = nvarchar(100)
        /// and throws rather than truncating, so a single long line kills a whole billing run.</summary>
        private const int DESC_MAX = 100;

        /// <summary>A description trimmed to what the column accepts, with an ellipsis so a reader can
        /// tell it was cut. Everything composed here is written to fit; this is the backstop for what
        /// comes from the book itself -- an item description or a duty label nobody here controls.</summary>
        private static string Fit(string text)
        {
            string t = text ?? "";
            return t.Length <= DESC_MAX ? t : t.Substring(0, DESC_MAX - 1) + "\u2026";
        }

        private static void AddTextRow(AutoCount.Invoicing.Sales.Invoice.Invoice doc,
            string description, string furtherDescription)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceDetail d = doc.AddDetail();
            d.Description = Fit(description);
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
        /// The lines a meter charge prints UNDER itself -- the reading breakdown the customer's own
        /// invoices carry:
        /// <code>
        ///   BK COPY + PRINT A4&amp;A3                          10,929      0.0250      273.23
        ///   Current Meter Reading (04/08/2026) : 140608
        ///   Previous Meter Reading (19/03/2026) : 129679
        ///   Meter Charges Usage : 10929
        /// </code>
        ///
        /// <para>Without them a meter line is a number with nothing behind it, and the customer has
        /// no way to check the usage they are being billed for. This is the single most recognisable
        /// thing about a meter invoice from this business, so it lives in one method that both the
        /// posted document and the Sample Invoice preview call -- a preview that composed its own
        /// version would drift from the article the moment either changed.</para>
        ///
        /// <para>Returned in print order, ready to become one description-only row each.</para>
        ///
        /// <para>A line with no meter behind it -- a rental, a waive, a committed minimum -- returns
        /// nothing, because it has no reading to report. The old system printed the rows anyway, all
        /// zeros (doc 2402289 carries "Current Meter Reading (07/07/2026) : 0" under RA-24MTH_EB2B),
        /// and on a merged rental that also meant the serials appeared twice: once on the description
        /// and again as the block's own S/N row. Zeros the customer has to learn to ignore are not
        /// fidelity to the article, they are a habit the article had. The free months are real
        /// information, so a rental that has them still says so.</para>
        /// </summary>
        public static List<string> ComposeReadingRows(ScpFoldedLine row, DateTime readingDate)
        {
            List<string> rows = new List<string>();
            if (row == null || row.Leader == null) return rows;
            MeterBillLine ln = row.Leader;
            if (ln.IsWaiveMeter)
            {
                // Which machines earned the credit and which did not. Without it a merged contra is a
                // single negative figure covering several separate deals, and nobody can check it.
                if (!row.IsMerged) return rows;
                foreach (MeterBillLine m in row.Members)
                    rows.Add(NameOf(m) + " : " +
                             (m.Charge < 0m
                                ? "waived " + (-m.Charge).ToString("n2")
                                : "target not reached, rental charged"));
                return rows;
            }
            if (ln.IsCommittedMin)
            {
                // A minimum on ONE machine explains itself on the line above. A merged one cannot --
                // its figure is several machines' shortfalls added up, and a customer asked to pay it
                // is entitled to see which machine contributed what. One row each, the same shape the
                // meter rows use: what was owed, what was printed, what is left to pay.
                if (!row.IsMerged) return rows;
                foreach (MeterBillLine m in row.Members)
                    rows.Add(NameOf(m) + " : minimum " + m.CommittedAmount.ToString("n2") +
                             ", copies " + m.PrintedAmount.ToString("n2") +
                             (m.Charge > 0m ? ", short " + m.Charge.ToString("n2")
                                            : ", over the minimum"));
                return rows;
            }
            if (ln.IsFlat)
            {
                decimal freeMonths = row.IsMerged ? row.FocAllowed : ln.Foc;
                if (freeMonths > 0m) rows.Add("Meter FOC Qty : " + Num(freeMonths));
                return rows;
            }

            // #16: contract-period mode prints the period as ONE range line -- user-specified format
            // "(start - end)" -- and the reading rows drop their dates (the customer must never see
            // cross-month reading dates). Normal mode is unchanged.
            bool periodMode = ln.PeriodStart.HasValue && ln.PeriodEnd.HasValue;
            if (periodMode)
                rows.Add("Billing Period (" + ln.PeriodStart.Value.ToString("dd/MM/yyyy") +
                         " - " + ln.PeriodEnd.Value.ToString("dd/MM/yyyy") + ")");

            // A merged row shows the group's summed readings -- no machine has these numbers, and that
            // is exactly what the customer's invoices print (Rompin's AMR2607.0087 shows 349,707, its
            // four machines added up). Where the members were read on different days the row prints
            // the span rather than picking one machine's date and implying the others were read then.
            DateTime curDate = (row.IsMerged ? row.CurDate : ln.AuditDate) ?? readingDate;
            string curDateStr = DateSpan(row, true, curDate);
            string lastDateStr = DateSpan(row, false, ln.LastDate ?? DateTime.MinValue);
            decimal showCurrent = row.IsMerged ? row.Current : ln.Current;
            decimal showLast = row.IsMerged ? row.Last : ln.Last;

            rows.Add(periodMode
                ? "Current Meter Reading : " + Num(showCurrent)
                : "Current Meter Reading (" + curDateStr + ") : " + Num(showCurrent));

            rows.Add(periodMode || lastDateStr.Length == 0
                ? "Previous Meter Reading : " + Num(showLast)
                : "Previous Meter Reading (" + lastDateStr + ") : " + Num(showLast));

            // Which machines are on this row. A single-machine row already says so on the charge
            // line, so only a merged one needs the list -- Pontian prints exactly this, all six
            // serials against one BK line of 74,722.
            //
            // The SAME tick governs it. There are two serial blocks on a meter line -- this one,
            // inside the reading breakdown, and the one under the model -- and turning the tick off
            // silenced only the second, so a thirty-six machine line still printed thirty-six
            // serials three lines further down. One question, one answer, both places.
            if (row.IsMerged && ln.ShowSerial)
                rows.Add("S/N : " + SerialList(row));

            // A ladder meter's allowance is its Free Qty, like any meter's (ATP-3). Only a meter with no
            // Free Qty -- an old ladder still carrying its 0.00 band -- prints what it actually took.
            // The ALLOWANCE, not what this month happened to use. A machine allowed 5,000 free
            // copies that printed 4,813 was printing "Meter FOC Qty : 4813" -- which reads as if
            // the allowance were 4,813, and would say something different every month. The
            // customer's own invoice prints 5000, because that is the term of the deal.
            //
            // A meter with no Free Qty of its own takes its free copies from an old 0.00 band, so
            // there the applied figure IS the allowance and is still what prints.
            decimal focShow = row.IsMerged
                ? row.FocAllowed
                : (ln.Foc > 0m ? ln.Foc : ln.Usage - ln.BillCopies - ln.RebateQty);
            if (focShow < 0m) focShow = 0m;
            if (focShow > 0m)
                rows.Add("Meter FOC Qty : " + Num(focShow));

            // The copies the rebate removed, shown the way Kastam and Tangkak show them
            // ("Meter Rebate Qty (3%) : 141") so the arithmetic on the line is followable.
            decimal rebQty = row.IsMerged ? row.RebateQty : ln.RebateQty;
            if (rebQty > 0m)
                rows.Add("Meter Rebate Qty (" + ln.RebatePct.ToString("0.##") + "%) : " + Num(rebQty));

            rows.Add("Meter Charges Usage : " + Num(row.IsMerged ? row.BillCopies : ln.BillCopies));
            return rows;
        }

        /// <summary>
        /// What a line says about the machines it covers — the customer's own proposal, and what
        /// their invoices already print:
        /// <code>
        ///   MONTHLY RENTAL (11/36)              BK COPY + PRINT A4&amp;A3
        ///   MODEL:iR-ADV 4545i  S/N:YAJ01479    MODEL:iR-ADV 4545i  (3 UNIT)
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
        /// <summary>A line break inside one printed description. Spelled out here because the escape
        /// is easy to lose in an edit and a literal backslash-r on an invoice is not a typo anyone
        /// spots until a customer does.</summary>
        private static readonly string BREAK = "\r\n";

        public static string ComposeFoldedDescription(ScpFoldedLine row, string baseText)
        {
            if (row == null || row.Leader == null) return baseText;
            MeterBillLine ln = row.Leader;
            if (!ln.NewMoneyRules)
            {
                // A rental billed in advance names the months it pays for on every invoice (ATP-10) --
                // a first bill reading just "MONTHLY RENTAL" does not say it is next month's.
                string ahead = ln.IsRental && ln.RentalBasis == 'P' ? ScpInvoiceLayout.RentalCounterText(ln) : "";
                if (ahead.Length > 0 && (baseText ?? "").IndexOf(ahead.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                    return (baseText ?? "").TrimEnd() + ahead;
                return baseText;
            }

            string head = (baseText ?? "").Trim();
            // The instalment counter belongs to the rental sentence, not to the machine list.
            if (ln.IsRental)
            {
                string counter = ScpInvoiceLayout.RentalCounterText(ln);
                // A prepaid line whose description already names its months does not say them twice.
                if (!(ln.RentalBasis == 'P' && counter.Length > 0
                      && head.IndexOf(counter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                    head += counter;
            }

            // A rental the deal gives free this month says so ON THE LINE. The amount column
            // prints 0.00 -- writing the word "FOC" there is the report layout's decision, and
            // the book has no layout of its own -- but the description is ours, and a customer
            // reading "MONTHLY RENTAL (3/36) - FOC" over a zero needs nothing explained.
            if (ln.FreeThisMonth) head += " - FOC";

            // The duty labels the machines carry. The old system had them baked into the item code --
            // MEDIUM DUTY "30-50 ppm" - IRADX4935I was an item, and a customer with three duty classes
            // needed three item codes -- so the words are familiar to the customer and belong on the
            // line. Where they go depends on what the format says the machine line names.
            char shows = ln.MachineLineShows;
            string duty = LineLabels(row);
            // The duty words ride on the charge name, the way the customer's own invoices print
            // them -- but only while the two together still fit the 100-character column. A row
            // covering three duty classes runs to 107 and would be cut off mid-word; there the
            // words take a line of their own, which the document prints as its own row underneath.
            // Nothing is lost either way, and the common case still reads as one sentence.
            string dutyOwnLine = "";
            bool dutyAlreadySaid = false;
            if (duty.Length > 0 && shows == ScpBillingFormat.MACHINE_LINE_BOTH)
            {
                if (head.Length + 2 + duty.Length <= DESC_MAX) head += "  " + duty;
                else dutyOwnLine = duty;
                dutyAlreadySaid = true;
            }

            // "Label only" means the line names the CLASS of machine and not the machine: no model
            // and no serial. That is how this business printed before ATP -- MEDIUM HEAVY DUTY
            // ''45cpm'' - MONTHLY RENTAL - names neither, and of 37,179 rental lines in the old book
            // 247 carry a serial and of 114,361 meter lines, twelve do. The serial line is ours, not
            // theirs.
            //
            // A row with no label falls back to the model. Choosing "label only" and then meeting a
            // machine nobody labelled would otherwise print a line that says nothing at all about
            // what is being billed -- and a line gone blank is indistinguishable from a bug.
            bool labelOnly = shows == ScpBillingFormat.MACHINE_LINE_LABEL && duty.Length > 0;

            // The two ticks. "Label only" still means neither, so an old contract prints exactly what
            // it printed; a contract that has answered the ticks answers for itself.
            //
            // Nothing at all is not an option. A line that names neither its class, its model nor
            // its machines says only "MONTHLY RENTAL" over a number, and the customer has no way to
            // ask about it -- so when the ticks would leave it blank, the model still prints. It is
            // the same reason "label only" falls back to the model on an unlabelled machine: a line
            // gone blank is indistinguishable from a bug.
            //
            // "Already said" matters. When the format puts the duty words on the charge sentence,
            // repeating them underneath printed the class TWICE on every line -- MONTHLY RENTAL
            // (13/36) HEAVY DUTY "105 cpm" over HEAVY DUTY "105 cpm" (2 UNIT). The fallback is for
            // a line that would otherwise say nothing, not for one that has already said it.
            bool haveSpareDuty = duty.Length > 0 && !dutyAlreadySaid;
            // Blank means the line names NOTHING -- no class anywhere (charge sentence included),
            // no serials. A class already on the charge sentence is still a name, so turning both
            // ticks off there leaves the unit count and nothing more, which is what was asked for.
            bool wouldSayNothing = !ln.ShowSerial && duty.Length == 0;
            bool wantModel = !labelOnly && (ln.ShowModel || wouldSayNothing);
            bool wantSerial = !labelOnly && ln.ShowSerial;

            System.Text.StringBuilder tail = new System.Text.StringBuilder();
            if (labelOnly)
            {
                tail.Append(duty);
            }
            else if (!wantModel && haveSpareDuty)
            {
                // Model turned off and the class has not been named yet -- name it here.
                tail.Append(duty);
            }
            else if (!wantModel)
            {
                // Model off, class already on the charge sentence: the unit count alone follows.
            }
            else
            {
                List<string> models = new List<string>();
                foreach (MeterBillLine m in (row.Covers != null && !row.IsMerged ? row.Covers : row.Members))
                {
                    string mc = (m.ModelCode ?? "").Trim();
                    if (mc.Length > 0 && !models.Contains(mc)) models.Add(mc);
                }
                if (models.Count > 0)
                    tail.Append("Model: ").Append(string.Join(", ", models.ToArray()));
            }

            if (!row.IsMerged && row.Covers != null)
            {
                // A group-scoped term is one meter on one machine, but it is ABOUT the whole group --
                // DEMO-19's single waive takes the rental off all three. Naming only the machine the
                // meter sits on reads as a credit for that machine, and the customer asks about the
                // other two. So it names them the way the rental line above it does.
                // Same rule as the merged row below: say it only where the Qty column does not.
                if (ln.ShowUnits && row.PrintQty != row.Covers.Count)
                {
                    if (tail.Length > 0) tail.Append("  ");
                    tail.Append("(").Append(row.Covers.Count).Append(" UNIT)");
                }
                if (wantSerial)
                {
                    string cs = SerialsOf(row.Covers);
                    if (cs.Length > 0) tail.Append(BREAK).Append("S/N: ").Append(cs);
                }
            }
            else if (!row.IsMerged)
            {
                // A row that stands for ONE machine names it, whatever the format says about models.
                // "Label only" drops the serial from a merged row because that row is about a group
                // and the count already describes it -- but a per-machine line IS about that machine,
                // and without the serial the customer cannot tell which of their five it is.
                string sn = (ln.SerialNumber ?? "").Trim();
                if (sn.Length > 0)
                {
                    if (tail.Length > 0) tail.Append("    ");
                    tail.Append("S/N: ").Append(sn);
                }
            }
            else
            {
                // The unit count, unless the Qty column already IS it.
                //
                // A merged RENTAL bills one row of N machines, so Qty prints N and UOM prints UNIT
                // -- and "(2 UNIT)" underneath said the same thing a third time. A merged METER row
                // bills COPIES, so its Qty is 69,393 and the machine count appears nowhere else;
                // there it is the only thing on the line that answers "how many machines".
                if (ln.ShowUnits && row.PrintQty != row.Units)
                {
                    if (tail.Length > 0) tail.Append("  ");
                    tail.Append("(").Append(row.Units).Append(" UNIT)");
                }

                // Which machines those units ARE. A meter line already lists them under its readings
                // ("S/N : ..." in the breakdown block), but a flat line has no breakdown block at all
                // -- so a merged rental printed "5 UNIT" and never said which five, and the customer
                // could not tell one 5-unit rental line from another. Its own line, because a list of
                // serials run onto the end of the model list reads as one more model.
                if (ln.IsFlat && wantSerial)
                {
                    string serials = SerialList(row);
                    if (serials.Length > 0) tail.Append(BREAK).Append("S/N: ").Append(serials);
                }
            }
            if (dutyOwnLine.Length > 0) head += BREAK + dutyOwnLine;
            return tail.Length == 0 ? head : head + BREAK + tail;
        }

        /// <summary>The duty labels on a printed row, each one once, in the order the machines appear:
        /// <c>MEDIUM DUTY "30-50 ppm", HEAVY DUTY "65 ppm"</c>.
        ///
        /// <para>The label is a DESCRIPTION and never a reason to split a line, so a row can hold
        /// machines whose labels differ -- JPJ merges twelve machines tagged HEAVY / MEDIUM / LIGHT
        /// onto one black line of 80,720 because they share a rate. This first printed nothing at all
        /// in that case, on the argument that no single word was true of the row. That was wrong twice
        /// over: it threw away information the customer is used to reading, and a line that has gone
        /// silent is indistinguishable from a label nobody filled in.</para>
        ///
        /// <para>The MODEL list one line below had the right answer all along -- four models on a row
        /// print as four models -- so the labels follow it: list what is there.</para></summary>
        private static string LineLabels(ScpFoldedLine row)
        {
            if (row == null || row.Leader == null) return "";
            List<string> labels = new List<string>();
            foreach (MeterBillLine m in row.Members)
            {
                string label = (m.LineGroupCode ?? "").Trim();
                if (label.Length == 0) continue;
                bool seen = false;
                for (int i = 0; i < labels.Count; i++)
                    if (string.Equals(labels[i], label, StringComparison.OrdinalIgnoreCase)) seen = true;
                if (!seen) labels.Add(label);
            }
            return labels.Count == 0 ? "" : string.Join(", ", labels.ToArray());
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
