using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One row as it will be printed: either a single machine's line, or several machines collapsed
    /// into one. Money is never recomputed here — every member arrives with its charge already
    /// settled by <see cref="ScpInvoiceBuilder.ComputeCharge"/> and the strategy passes, and folding
    /// only decides how many rows the customer sees and what quantity each carries.
    /// </summary>
    public class ScpFoldedLine
    {
        /// <summary>The line that prints. Its own fields still describe machine #1 of the group.</summary>
        public MeterBillLine Leader;
        /// <summary>Every machine on this row, Leader first. One entry = an unmerged line.</summary>
        public List<MeterBillLine> Members = new List<MeterBillLine>();

        public bool IsMerged { get { return Members.Count > 1; } }
        /// <summary>Machines on the row — the "3 UNIT" of a merged rental line.</summary>
        public decimal Units { get { return Members.Count; } }

        /// <summary>Sum of the members' billable copies — the merged line's Qty.</summary>
        public decimal BillCopies;
        /// <summary>Summed readings. No machine has these numbers; they are what the customer's own
        /// invoices print (Rompin's AMR2607.0087 shows 349,707, the four machines added up).</summary>
        public decimal Current, Last;
        public decimal FocApplied, Usage;
        /// <summary>The allowance the deal GIVES, which is what the invoice prints. Not the same
        /// number as <see cref="FocApplied"/>: a machine allowed 5,000 free copies that printed
        /// 4,813 used 4,813 of them, and a line reading "Meter FOC Qty : 4813" tells the customer
        /// their allowance is whatever they happened to print. A meter with no allowance of its
        /// own -- a ladder, whose free band is worked out from the bands -- contributes what it
        /// actually took, because that is the only free quantity it has.</summary>
        public decimal FocAllowed;
        /// <summary>Copies the rebate removed, floored per machine then added up — which is how the
        /// customer's own merged lines read (Pontian prints 1,522, where 2% of the merged 76,244
        /// would have been 1,524).</summary>
        public decimal RebateQty;

        /// <summary>What the members charge, added up. A merged usage row is Qty x one rate, so its
        /// money falls out of the quantity — but a merged MINIMUM is a sum of separate shortfalls that
        /// no single rate describes, and multiplying one member's top-up by the count would only be
        /// right when every machine happened to be short by the same amount.</summary>
        public decimal ChargeTotal;
        /// <summary>Newest and oldest reading dates in the group. Equal on an unmerged line; a merged
        /// line prints the range rather than inventing a single date the readings never shared.</summary>
        public DateTime? CurDate, PrevDate;

        /// <summary>The machines a GROUP-scoped term speaks for — one entry each — even though only
        /// one of them carries the meter.
        ///
        /// <para>A waive scoped to a rental line is ONE meter on ONE machine, and it takes the whole
        /// line's rental off. Printing that row with the serial of the machine the meter happens to
        /// sit on says the credit belongs to that machine, and the customer is left asking about the
        /// other two. The same is true of a minimum measured over a group. So the row carries the
        /// machines it covers, and prints them the way the rental line does.</para>
        ///
        /// <para>Null on every other row, and on a term that really is about one machine — those
        /// name their machine and nothing else. It never touches Qty or price: a group waive is
        /// still ONE amount, not one per machine.</para></summary>
        public List<MeterBillLine> Covers;

        /// <summary>The quantity this row prints — machines for a rental, copies for usage.</summary>
        public decimal PrintQty
        {
            get
            {
                // A merged minimum is a SUM of separate shortfalls, so it has no count and no rate.
                // Three machines short by 150, 200 and 250 owe 600; printing "3 x 200" is arithmetic
                // that happens to land on the same total and says something false about every one of
                // them -- and the moment the floors differ it lands somewhere else entirely.
                // A merged minimum or waive is a SUM of separate amounts -- no count, no rate.
                if (IsMerged && (Leader.IsCommittedMin || Leader.IsWaiveMeter)) return 1m;
                if (Leader.UseMin || Leader.IsFlat)
                    return Units > 1m ? Units : 1m;
                // A copy line with nothing left to bill prints nothing to bill. It used to fall
                // into the flat case and print the MACHINE COUNT: SINGLE ADVERTISING has two
                // machines, 557 copies between them and 1,000 free, and the black line read
                // "2 @ 0.00" -- which on a copy line says two copies, not two machines. Their
                // own invoice prints the line with no quantity at all and the allowance below it.
                return IsMerged ? BillCopies : Leader.BillCopies;
            }
        }

        /// <summary>
        /// The per-unit price this row prints.
        ///
        /// <para>A merged row has ONE price, because it is one line. When its machines were all on
        /// the same rate that is simply the rate. When they were not, the row prices itself at the
        /// rate that reproduces what those machines actually cost -- total charge over total
        /// quantity -- so the line's own arithmetic holds and the invoice still comes to the right
        /// money. A contract that wants a different figure sets one; that is a pricing decision, and
        /// it belongs to whoever signed the deal, not to a grouping rule.</para>
        /// </summary>
        public decimal PrintUnitPrice
        {
            get
            {
                if (IsMerged && (Leader.IsCommittedMin || Leader.IsWaiveMeter))
                    return PrintAmount;                                     // qty is 1; see PrintQty
                bool flat = Leader.UseMin || Leader.IsFlat || Leader.BillCopies <= 0m;
                if (!IsMerged) return flat ? Leader.Charge : Leader.EffUnitPrice;

                decimal first = flat ? Leader.Charge : Leader.EffUnitPrice;
                if (OneRate) return first;
                decimal money = 0m;
                foreach (MeterBillLine m in Members) money += m.Charge;
                decimal qty = PrintQty;
                return qty > 0m ? Math.Round(money / qty, 6, MidpointRounding.AwayFromZero) : first;
            }
        }

        /// <summary>
        /// What the row comes to.
        ///
        /// <para><b>Rental</b> is units times the per-unit price, because that is the deal: a group
        /// of machines rented at one agreed monthly figure.</para>
        ///
        /// <para><b>Black and colour</b> are the sum of what the machines actually cost. Each meter
        /// keeps its own rate and its own charge; merging decides how many LINES print, not what
        /// anything costs. Summing is also the only way to be exact when the rates differ — 60,249
        /// at 0.019 plus 52,860 at 0.0285 is 2,651.24, and no single rate on 113,109 copies lands
        /// there. The rate the row displays is derived from this total, not the other way round.</para>
        /// </summary>
        /// <summary>What the line is worth: the sum of what its machines were charged.</summary>
        /// <remarks>
        /// Never recomputed from the printed rate, even for a line of one. A charge is worked out per
        /// machine -- ladder, free copies, rebate, minimum, then rounded -- and stored in the reading
        /// log that way, so recomputing here would let the invoice and the log disagree by a cent on
        /// a half. 5,693 colour copies at 0.285 is 1,622.505: the machine was charged 1,622.50 and
        /// that is the number that must print.
        ///
        /// <para>The one exception is a MERGED row whose machines all share a rate. There the line
        /// prints a quantity and a price, and the customer multiplies them -- so the row is worth
        /// exactly that product. HOSPITAL SULTAN ISMAIL is the case: thirteen machines at 0.0285
        /// cost 3,048.37 added up one at a time, while the line reads "106,960 @ 0.0285" and comes
        /// to 3,048.36. Their own invoice prints 3,048.36. A cent that cannot be reproduced from
        /// the figures on the paper is a cent the customer phones about.</para>
        /// </remarks>
        public decimal PrintAmount
        {
            get
            {
                if (Members == null || Members.Count == 0)
                    return Math.Round(PrintQty * PrintUnitPrice, 2, MidpointRounding.AwayFromZero);
                // One rate across the row: the line must multiply out, because the reader will do it.
                if (IsMerged && OneRate
                    && !(Leader.IsCommittedMin || Leader.IsWaiveMeter))
                    return Math.Round(PrintQty * PrintUnitPrice, 2, MidpointRounding.AwayFromZero);
                decimal money = 0m;
                foreach (MeterBillLine m in Members) money += m.Charge;
                return Math.Round(money, 2, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Whether every machine on this row was charged at the same rate.
        ///
        /// <para>It decides both what the row prints as its price and how it reaches its total, and
        /// the two answers have to come from the same question or the line stops multiplying
        /// out.</para>
        /// </summary>
        private bool OneRate
        {
            get
            {
                if (Members == null || Members.Count < 2) return true;
                bool flat = Leader.UseMin || Leader.IsFlat || Leader.BillCopies <= 0m;
                decimal first = flat ? Leader.Charge : Leader.EffUnitPrice;
                foreach (MeterBillLine m in Members)
                    if ((flat ? m.Charge : m.EffUnitPrice) != first) return false;
                return true;
            }
        }

        /// <summary>A member whose meter read backwards — the reading went DOWN. Usually a replaced
        /// or reset meter, but after a migration it is the signature of a machine that inherited a
        /// summed reading, and billing it would silently produce a zero invoice.</summary>
        public MeterBillLine BackwardsMember()
        {
            foreach (MeterBillLine m in Members)
                if (!m.IsFlat && m.Current < m.Last) return m;
            return null;
        }

        public ScpFoldedLine(MeterBillLine leader)
        {
            Leader = leader;
            Members.Add(leader);
        }
    }

    /// <summary>
    /// Decides how many rows an invoice prints, given each contract's RentalLineMode / MeterLineMode.
    ///
    /// <para><b>Folding is presentation.</b> Charges, FOC, rebate, ladders, committed minimums and
    /// waives are all computed per machine before this runs and are not touched. Meter stamps,
    /// reading history and the Appendix A listing stay per machine too, so a later credit note or
    /// enquiry still sees the individual machines.</para>
    ///
    /// <para><b>Opt-in per contract.</b> A contract with no BillingFormatCode keeps the exact legacy
    /// behaviour — rentals fold on the old predicate and key, meter lines never fold — so an existing
    /// book bills identically until someone picks a format for that customer. That doubles as the
    /// parallel-run switch: formats can be assigned one customer at a time and compared against the
    /// invoices Master Accounting issued.</para>
    /// </summary>
    public static class ScpInvoiceLayout
    {
        private class ContractLayout
        {
            public char RentalMode = ScpBillingFormat.LINE_ACROSS_MODEL;
            public char MeterMode = ScpBillingFormat.LINE_PER_MACHINE;
            public bool HasFormat;      // false = legacy path, byte-identical to before
        }

        /// <summary>
        /// Fold with the modes given rather than looked up — for showing what a layout WOULD do to
        /// a sample fleet, before any contract has been given it. Same code path as the real fold,
        /// so a preview cannot promise something the engine will not produce.
        /// </summary>
        public static List<ScpFoldedLine> FoldWith(List<MeterBillLine> lines, char rentalMode, char meterMode)
        {
            return FoldWith(lines, rentalMode, meterMode, true, false);
        }

        /// <summary>
        /// As above, but able to ask for the LEGACY shape as well — a contract with no Billing Format
        /// does not simply stop merging, it merges rentals on the old predicate and never merges
        /// usage. A preview that showed "nothing merges" for such a contract would be describing an
        /// invoice the engine does not produce, which is the one thing a preview must never do.
        /// </summary>
        public static List<ScpFoldedLine> FoldWith(List<MeterBillLine> lines, char rentalMode,
            char meterMode, bool hasFormat, bool legacyRentalFold)
        {
            List<ScpFoldedLine> result = new List<ScpFoldedLine>();
            if (lines == null || lines.Count == 0) return result;
            ContractLayout lay = new ContractLayout();
            lay.HasFormat = hasFormat;
            lay.RentalMode = rentalMode;
            lay.MeterMode = meterMode;

            Dictionary<string, ScpFoldedLine> byKey =
                new Dictionary<string, ScpFoldedLine>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine ln in lines)
            {
                string key = FoldKey(ln, lay, legacyRentalFold);
                ScpFoldedLine grp;
                if (key != null && byKey.TryGetValue(key, out grp)) { grp.Members.Add(ln); continue; }
                grp = new ScpFoldedLine(ln);
                if (key != null) byKey[key] = grp;
                result.Add(grp);
            }
            foreach (ScpFoldedLine g in result) Aggregate(g);
            MarkCovers(result, lines);
            SortForPrint(result);
            return result;
        }

        /// <summary>Work out, for every group-scoped minimum and waive, which machines it is about.
        /// See <see cref="ScpFoldedLine.Covers"/>.</summary>
        private static void MarkCovers(List<ScpFoldedLine> rows, List<MeterBillLine> lines)
        {
            foreach (ScpFoldedLine g in rows)
            {
                MeterBillLine ln = g.Leader;
                if (!ln.IsCommittedMin && !ln.IsWaiveMeter) continue;
                string scope = (ln.CommitScope ?? "").Trim().ToUpperInvariant();
                if (scope != "G" && scope != "C") continue;   // 'S' really is about one machine

                // A waive credits the RENT, so its set is the machines sharing the rental line; a
                // minimum floors the COPIES, so its set is the machines sharing the copies. Same
                // question the engine asks when it decides whether the term fires.
                string grp = ((ln.IsWaiveMeter ? ln.MergeGroupCode : ln.MergeGroupCodeMeter) ?? "").Trim();
                if (scope == "G" && grp.Length == 0) continue;

                List<long> seen = new List<long>();
                List<MeterBillLine> covers = new List<MeterBillLine>();
                foreach (MeterBillLine m in lines)
                {
                    if (m.ContractKey != ln.ContractKey) continue;
                    if (m.IsCommittedMin || m.IsWaiveMeter) continue;   // a term is not a machine
                    if (scope == "G")
                    {
                        string mg = ((ln.IsWaiveMeter ? m.MergeGroupCode : m.MergeGroupCodeMeter) ?? "").Trim();
                        if (!string.Equals(mg, grp, StringComparison.OrdinalIgnoreCase)) continue;
                    }
                    if (m.ItemKey <= 0 || seen.Contains(m.ItemKey)) continue;
                    seen.Add(m.ItemKey);
                    covers.Add(m);
                }
                if (covers.Count > 1) g.Covers = covers;
            }
        }

        /// <summary>
        /// Collapse the lines of ONE invoice into the rows it will print, in print order:
        /// rentals first, then black, then colour, then the explanatory lines (committed minimum,
        /// waive) that always speak for a single machine.
        /// </summary>
        public static List<ScpFoldedLine> Fold(DBSetting db, List<MeterBillLine> lines)
        {
            List<ScpFoldedLine> result = new List<ScpFoldedLine>();
            if (lines == null || lines.Count == 0) return result;

            Dictionary<long, ContractLayout> layouts = LoadLayouts(db, lines);
            bool legacyRentalFold = LegacyRentalFoldEnabled(db);

            Dictionary<string, ScpFoldedLine> byKey = new Dictionary<string, ScpFoldedLine>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine ln in lines)
            {
                ContractLayout lay;
                if (!layouts.TryGetValue(ln.ContractKey, out lay)) lay = new ContractLayout();

                string key = FoldKey(ln, lay, legacyRentalFold);
                ScpFoldedLine grp;
                if (key != null && byKey.TryGetValue(key, out grp))
                {
                    grp.Members.Add(ln);
                }
                else
                {
                    grp = new ScpFoldedLine(ln);
                    if (key != null) byKey[key] = grp;
                    result.Add(grp);
                }
            }

            foreach (ScpFoldedLine g in result) Aggregate(g);
            MarkCovers(result, lines);
            SortForPrint(result);
            return result;
        }

        /// <summary>
        /// Put the rows in print order: every rental first, then the waives that reduce them, then
        /// the usage grouped by the machines it is for -- black and colour of the same machine (or the
        /// same model, when the format merges by model) next to each other -- and the committed
        /// minimum last, because it measures the print charges printed above it.
        ///
        /// <para>Both Fold methods DESCRIBED an order and neither produced one: the rows came out
        /// however the readings were collected, which is machine by machine, so a six-machine contract
        /// printed rental, BK, CL, rental, BK, CL down the page. The customer reads the rental total
        /// off one block and the usage off another; interleaved, neither block exists.</para>
        ///
        /// <para>But black-then-colour is only the order WITHIN a machine. Sorting all the black
        /// together and all the colour after it separates the two halves of one machine's usage by the
        /// whole width of the contract, and the customer checking a machine has to read the invoice
        /// twice. So the usage is grouped by the row's leading machine first -- which is the same
        /// machine for the BK row and the CL row of one model -- and only then by colour.</para>
        ///
        /// <para>Stable within a rank, so machines and models stay in the order the contract lists
        /// them: the order that made sense to whoever entered them. List.Sort is not stable, hence
        /// the explicit insertion sort.</para>
        /// </summary>
        public static void SortForPrint(List<ScpFoldedLine> rows)
        {
            if (rows == null || rows.Count < 2) return;

            int n = rows.Count;
            ScpFoldedLine[] src = rows.ToArray();
            int[] section = new int[n];
            int[] group = new int[n];
            int[] colour = new int[n];

            // Which machine each usage row leads with, in the order those machines first appear. A
            // model's BK row and its CL row lead with the same machine, so they land in one group.
            Dictionary<long, int> groupOf = new Dictionary<long, int>();
            for (int i = 0; i < n; i++)
            {
                section[i] = SectionOf(src[i]);
                colour[i] = ColourOf(src[i]);
                group[i] = 0;
                if (section[i] != SECTION_METER) continue;
                long machine = src[i].Leader == null ? 0L : src[i].Leader.ItemKey;
                int g;
                if (!groupOf.TryGetValue(machine, out g)) { g = groupOf.Count; groupOf[machine] = g; }
                group[i] = g;
            }

            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            // Insertion sort: stable, and a printed invoice is a handful of rows.
            for (int i = 1; i < n; i++)
            {
                int idx = order[i];
                int j = i - 1;
                while (j >= 0 && After(section, group, colour, order[j], idx)) { order[j + 1] = order[j]; j--; }
                order[j + 1] = idx;
            }

            rows.Clear();
            for (int i = 0; i < n; i++) rows.Add(src[order[i]]);
        }

        private const int SECTION_RENTAL = 0;
        private const int SECTION_WAIVE = 1;
        private const int SECTION_METER = 2;
        private const int SECTION_MINIMUM = 3;

        /// <summary>True when row <paramref name="a"/> must print after row <paramref name="b"/>.</summary>
        private static bool After(int[] section, int[] group, int[] colour, int a, int b)
        {
            if (section[a] != section[b]) return section[a] > section[b];
            if (group[a] != group[b]) return group[a] > group[b];
            return colour[a] > colour[b];
        }

        private static int SectionOf(ScpFoldedLine row)
        {
            MeterBillLine ln = row == null ? null : row.Leader;
            if (ln == null) return SECTION_METER;
            if (ln.IsCommittedMin) return SECTION_MINIMUM;
            if (ln.IsWaiveMeter) return SECTION_WAIVE;
            if (ln.IsRental) return SECTION_RENTAL;
            return SECTION_METER;
        }

        private static int ColourOf(ScpFoldedLine row)
        {
            MeterBillLine ln = row == null ? null : row.Leader;
            if (ln == null) return 2;
            // "Black"/"Colour" from the reading grid, "BK"/"CL" from the format preview.
            string c = (ln.ColorLabel ?? "").Trim();
            if (string.Equals(c, "Black", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c, "BK", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(c, "Colour", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c, "Color", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c, "CL", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }

        /// <summary>
        /// The identity of a printed row. Two lines share a row when this matches; null means the
        /// line never merges and always prints alone.
        ///
        /// <para><b>Price is part of it.</b> A printed line is one Qty x UnitPrice, so two machines at
        /// different money cannot share one without the arithmetic on the invoice ceasing to be true.
        /// Pasir Gudang prints TWO black lines -- 60,249 at 0.019 and 52,860 at 0.0285 -- and that is
        /// not a quirk of how the meters were built, it is the only honest way to print them. JPJ
        /// splits its twelve rentals into three lines the same way, on price, while its twelve black
        /// meters share one rate and print as a single line of 80,720.</para>
        ///
        /// <para>Rental gets one extra move: a contract can state the price of a group, and that
        /// price is written onto every member before anything is computed, so the members arrive here
        /// already agreeing. Setting the group price IS how rentals at different money are made into
        /// one line -- by agreeing what the line costs, not by averaging what it cost.</para>
        /// </summary>
        private static string FoldKey(MeterBillLine ln, ContractLayout lay, bool legacyRentalFold)
        {
            if (ln == null) return null;
            // A waive follows its RENTAL. Machines sharing a rental line print one rental row, so the
            // credits against that rental print one row too -- three machines each freeing their own
            // 450 read as one 1,350 off the line they are on, which is the line the customer is looking
            // at. The set is the RENTAL group, not the copies group: a waive is a credit against rent,
            // and the machines that share the rent are the ones whose credits belong together.
            //
            // A machine on no rental line keeps its own credit row, because it has no row to share.
            if (ln.IsWaiveMeter)
            {
                if (!lay.HasFormat) return null;
                string wg = (ln.MergeGroupCode ?? "").Trim();
                if (wg.Length == 0) return null;
                return "WV|" + ln.ContractKey + "|" + wg.ToUpperInvariant();
            }

            // A committed minimum follows its COPIES. When the machines it measures print as one row,
            // their top-ups print as one row too -- the customer sees "3 machines, this much printed,
            // this much short", which is the same shape whether the floor was agreed per machine or
            // for the group. Only the arithmetic differs, and that is settled long before the fold:
            // a per-machine floor tops each machine up against its own copies and the row prints the
            // sum; a group floor is ONE meter scoped 'G' and arrives here as a single line already.
            //
            // Ungrouped machines keep a line each, exactly as before -- there is no row for them to
            // share, and a merged explanation would name machines the invoice never showed.
            if (ln.IsCommittedMin)
            {
                if (!lay.HasFormat) return null;
                string cg = (ln.MergeGroupCodeMeter ?? "").Trim();
                if (cg.Length == 0) return null;
                return "MIN|" + ln.ContractKey + "|" + cg.ToUpperInvariant();
            }

            if (ln.IsRental || ln.IsFlat)
            {
                if (!lay.HasFormat)
                {
                    // Legacy: the old predicate and the old key, untouched.
                    if (!legacyRentalFold) return null;
                    if (!ln.IsFlat || !ln.IsRental || !string.IsNullOrEmpty(ln.StrategyNote) || ln.Charge <= 0m)
                        return null;
                    return "R|" + (ln.MeterTypeCode ?? "") + "|" + (ln.ACItemCode ?? "") + "|" +
                           ln.Charge.ToString("0.####") + "|" + ln.ContractKey;
                }
                if (lay.RentalMode == ScpBillingFormat.LINE_PER_MACHINE) return null;

                // On the new layout the machines decide: in a group, share that group's line; in no
                // group, print alone. No mode sits above it, so there is nothing that can disagree
                // with what the screen showed.
                if (lay.RentalMode == ScpBillingFormat.LINE_BY_GROUP &&
                    (ln.MergeGroupCode ?? "").Trim().Length == 0) return null;

                // Money splits the line. Three machines at 250, 300 and 475 are three deals, and
                // "3 UNIT at ..." can only name one figure. Agreeing a group price is what turns
                // them into one line: ApplyRentalGroupPrices stamps that figure onto every member
                // before anything is computed, so a priced group arrives here already agreeing and
                // folds on its own. JPJ's twelve rentals printing as three lines is this rule.
                //
                // The instalment counter stays in too: "3 UNIT ... (13/36)" is a claim about all
                // three machines, and one that joined later is genuinely on a different month.
                //
                // The bucket: a hand-made group when the machine has one, otherwise the model under
                // "merge by model" and everything together under "merge, ignoring model". That is
                // the one place a contract can say "these two models on one line, that one apart".
                // A flat line is keyed on the money it SETTLED at, not on its rate: the old book puts
                // a rental's amount in MinimumCharges as often as in ChargesRate, so keying the rate
                // would read 0 for both and merge two rentals at different money onto one line. The
                // legacy key has always used the charge for exactly this reason.
                return "R|" + ln.ContractKey + "|" + (ln.MeterTypeCode ?? "") + "|" +
                       (ln.ACItemCode ?? "") + "|P:" +
                       ln.Charge.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                       ln.RentalMonths + "/" + RentalMonthNo(ln) + "|" + (ln.StrategyNote ?? "") + "|" +
                       ScpRentalGroupPrice.GroupKeyFor(lay.RentalMode, ln.ModelCode, ln.MergeGroupCode);
            }

            // ----- BK / CL -----
            if (!lay.HasFormat) return null;                                  // legacy never merged usage
            if (lay.MeterMode == ScpBillingFormat.LINE_PER_MACHINE) return null;
            if (lay.MeterMode == ScpBillingFormat.LINE_BY_GROUP &&
                (ln.MergeGroupCodeMeter ?? "").Trim().Length == 0) return null;

            // Price splits the line; nothing else about the money does. Machines at one rate sum to
            // exactly qty x that rate, so the row a reader can check by hand is the row that prints.
            // Free copies and rebates come out of the QUANTITY, not the rate, so they never split it.
            string m = "M|" + ln.ContractKey + "|" + (ln.ColorLabel ?? "") + "|" + (ln.MeterTypeCode ?? "") + "|" +
                       (ln.ACItemCode ?? "") + "|" + PriceKey(ln) + "|" + (ln.StrategyNote ?? "");
            // The duty label is NEVER part of a key, in any mode. It is a description -- the word the
            // line prints -- and nothing more. What separates rows is the price and, under "same
            // model", the model.
            //
            // The real invoices confirm the label is never needed: JPJ tags twelve machines HEAVY /
            // MEDIUM / LIGHT and prints ONE black line of 80,720 because they share a rate; its
            // rental splits three ways on price. MBJB prints "MEDIUM HEAVY DUTY" on both its C5160
            // and C5150 rows and they stay apart on model. Pasir Gudang's two MEDIUM DUTY rental
            // lines are two different models.
            // Same bucket rule as the rental line: a machine put in a group prints with its group,
            // whatever the mode would otherwise have done. Grouping is about which machines belong
            // together, and that answer does not change between the rental line and the black one.
            // The copies group on their own column, so "rental together, copies apart" is sayable.
            m += "|" + ScpRentalGroupPrice.GroupKeyFor(lay.MeterMode, ln.ModelCode, ln.MergeGroupCodeMeter);
            return m;
        }

        /// <summary>What "the same price" means for merging.</summary>
        /// <remarks>
        /// A flat rate is the rate itself. A multi-price ladder is the SCHEME: two machines on the
        /// same ladder are on the same deal even though their blended per-copy prices differ, because
        /// the blend is only an artefact of how much each one printed. Each still computes its own
        /// charge through the ladder and the merged line is worth their sum -- the same rule every
        /// merged usage line follows.
        ///
        /// <para>A per-meter tier override is keyed '#&lt;ItemMeterKey&gt;', which is unique to one
        /// machine, so an overridden machine keeps its own line. That is right: an override IS the
        /// statement that this machine is priced unlike the others.</para>
        /// </remarks>
        private static string PriceKey(MeterBillLine ln)
        {
            string ladder = (ln.MultiPriceCode ?? "").Trim();
            if (ladder.Length > 0) return "L:" + ladder.ToUpperInvariant();
            return "P:" + ln.Rate.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Which instalment this rental is in — part of a rental row's identity, and the
        /// "(11/36)" the line prints.</summary>
        public static int RentalMonthNo(MeterBillLine ln)
        {
            if (!ln.RentalStartDate.HasValue || ln.RentalMonths <= 0) return 0;
            DateTime s = ln.RentalStartDate.Value;
            DateTime now = ln.PeriodEnd ?? ln.AuditDate ?? DateTime.Today;
            int n = ((now.Year - s.Year) * 12) + (now.Month - s.Month) + 1;
            if (n < 1) n = 1;
            if (n > ln.RentalMonths) n = ln.RentalMonths;
            return n;
        }

        /// <summary>Sum the members onto the row. Per-line rounding is preserved on each member's own
        /// Charge (the reading log keeps it); the printed row multiplies the summed quantity by the
        /// shared price once, which is what the issued invoices show — HSI prints 3,048.36 where the
        /// per-machine charges add to 3,048.37.</summary>
        private static void Aggregate(ScpFoldedLine g)
        {
            g.BillCopies = 0m; g.Current = 0m; g.Last = 0m; g.FocApplied = 0m; g.Usage = 0m;
            g.FocAllowed = 0m;
            g.RebateQty = 0m; g.CurDate = null; g.PrevDate = null; g.ChargeTotal = 0m;
            foreach (MeterBillLine m in g.Members)
            {
                g.BillCopies += m.BillCopies;
                g.RebateQty += m.RebateQty;
                g.ChargeTotal += m.Charge;
                g.Current += m.Current;
                g.Last += m.Last;
                g.Usage += m.Usage;
                // Copies the ALLOWANCE took, which is not everything missing from BillCopies — the
                // rebate took its own share and is reported on its own row.
                decimal foc = m.IsFlat ? m.Foc : (m.Usage - m.BillCopies - m.RebateQty);
                if (foc < 0m) foc = 0m;
                g.FocApplied += foc;
                g.FocAllowed += m.Foc > 0m ? m.Foc : foc;

                if (m.AuditDate.HasValue && (!g.CurDate.HasValue || m.AuditDate.Value > g.CurDate.Value))
                    g.CurDate = m.AuditDate;
                if (m.LastDate.HasValue && (!g.PrevDate.HasValue || m.LastDate.Value < g.PrevDate.Value))
                    g.PrevDate = m.LastDate;
            }
        }

        /// <summary>The contract-level modes for every contract the lines touch, in one query.</summary>
        private static Dictionary<long, ContractLayout> LoadLayouts(DBSetting db, List<MeterBillLine> lines)
        {
            Dictionary<long, ContractLayout> map = new Dictionary<long, ContractLayout>();
            if (db == null) return map;
            List<long> keys = new List<long>();
            foreach (MeterBillLine ln in lines)
                if (ln.ContractKey > 0 && !keys.Contains(ln.ContractKey)) keys.Add(ln.ContractKey);
            if (keys.Count == 0) return map;

            System.Text.StringBuilder inList = new System.Text.StringBuilder();
            foreach (long k in keys)
            {
                if (inList.Length > 0) inList.Append(",");
                inList.Append(k);
            }
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT ContractKey, ISNULL(BillingFormatCode,'') AS BillingFormatCode, " +
                    "ISNULL(UseNewLayout,'N') AS UseNewLayout, " +
                    "ISNULL(RentalLineMode,'A') AS RentalLineMode, ISNULL(MeterLineMode,'S') AS MeterLineMode " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                {
                    ContractLayout lay = new ContractLayout();
                    // The new rules have their own switch now. They used to ride on "does this
                    // contract name a format", which meant a label was deciding the arithmetic --
                    // clearing the name quietly moved the money back to the old rounding and the
                    // old rebate. UseNewLayout says it out loud, and a contract can be moved across
                    // one at a time, which is also how the parallel run against the old system works.
                    bool newLayout = Convert.ToString(r["UseNewLayout"]).Trim().ToUpperInvariant() == "Y";
                    lay.HasFormat = newLayout || Convert.ToString(r["BillingFormatCode"]).Trim().Length > 0;
                    if (newLayout)
                    {
                        lay.RentalMode = ScpBillingFormat.LINE_BY_GROUP;
                        lay.MeterMode = ScpBillingFormat.LINE_BY_GROUP;
                    }
                    else
                    {
                        lay.RentalMode = FirstChar(r["RentalLineMode"], ScpBillingFormat.LINE_ACROSS_MODEL);
                        lay.MeterMode = FirstChar(r["MeterLineMode"], ScpBillingFormat.LINE_PER_MACHINE);
                    }
                    map[Convert.ToInt64(r["ContractKey"])] = lay;
                }
            }
            catch { }   // older book without the v14 columns -> every contract stays legacy
            return map;
        }

        /// <summary>Each contract's rental line mode, for callers that only need to know whether a
        /// contract HAS rental groups (and which). A contract with no Billing Format reports
        /// 'S' -- no merge, no group -- so nothing that has not been moved onto the new rules can be
        /// treated as having a group price.</summary>
        public static Dictionary<long, char> LoadRentalModes(DBSetting db, IEnumerable<long> contractKeys)
        {
            Dictionary<long, char> map = new Dictionary<long, char>();
            if (db == null || contractKeys == null) return map;
            System.Text.StringBuilder inList = new System.Text.StringBuilder();
            List<long> seen = new List<long>();
            foreach (long k in contractKeys)
            {
                if (k <= 0 || seen.Contains(k)) continue;
                seen.Add(k);
                if (inList.Length > 0) inList.Append(",");
                inList.Append(k);
            }
            if (inList.Length == 0) return map;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT ContractKey, ISNULL(BillingFormatCode,'') AS BillingFormatCode, " +
                    "ISNULL(UseNewLayout,'N') AS UseNewLayout, " +
                    "ISNULL(RentalLineMode,'A') AS RentalLineMode " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                {
                    bool newLayout = Convert.ToString(r["UseNewLayout"]).Trim().ToUpperInvariant() == "Y";
                    map[Convert.ToInt64(r["ContractKey"])] = newLayout
                        ? ScpBillingFormat.LINE_BY_GROUP
                        : (Convert.ToString(r["BillingFormatCode"]).Trim().Length == 0
                            ? ScpBillingFormat.LINE_PER_MACHINE
                            : FirstChar(r["RentalLineMode"], ScpBillingFormat.LINE_ACROSS_MODEL));
                }
            }
            catch { }   // older book without the v14 columns -> no contract has groups
            return map;
        }

        /// <summary>The global that RentalLineMode replaces. Still honoured for contracts that have
        /// not been given a format, so nothing changes underneath a book mid-migration.</summary>
        /// <summary>Does this book still fold rentals the old way? The switch a contract with no
        /// Billing Format is billed under — public so a preview can ask the same question the run
        /// asks, instead of guessing.</summary>
        public static bool LegacyRentalFold(DBSetting db)
        {
            return LegacyRentalFoldEnabled(db);
        }

        private static bool LegacyRentalFoldEnabled(DBSetting db)
        {
            try
            {
                return ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_GROUP_RENTAL_BY_METER,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_GROUP_RENTAL_BY_METER);
            }
            catch { return ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_GROUP_RENTAL_BY_METER; }
        }

        private static char FirstChar(object o, char fallback)
        {
            string s = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
            return s.Length == 0 ? fallback : char.ToUpperInvariant(s[0]);
        }
    }
}
