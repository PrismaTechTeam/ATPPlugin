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
        /// <summary>Copies the rebate removed, floored per machine then added up — which is how the
        /// customer's own merged lines read (Pontian prints 1,522, where 2% of the merged 76,244
        /// would have been 1,524).</summary>
        public decimal RebateQty;
        /// <summary>Newest and oldest reading dates in the group. Equal on an unmerged line; a merged
        /// line prints the range rather than inventing a single date the readings never shared.</summary>
        public DateTime? CurDate, PrevDate;

        /// <summary>The quantity this row prints — machines for a rental, copies for usage.</summary>
        public decimal PrintQty
        {
            get
            {
                if (Leader.UseMin || Leader.IsFlat || Leader.BillCopies <= 0m)
                    return Units > 1m ? Units : 1m;
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
                bool flat = Leader.UseMin || Leader.IsFlat || Leader.BillCopies <= 0m;
                if (!IsMerged) return flat ? Leader.Charge : Leader.EffUnitPrice;

                decimal money = 0m;
                bool uniform = true;
                decimal first = flat ? Leader.Charge : Leader.EffUnitPrice;
                foreach (MeterBillLine m in Members)
                {
                    money += m.Charge;
                    decimal p = flat ? m.Charge : m.EffUnitPrice;
                    if (p != first) uniform = false;
                }
                if (uniform) return first;
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
        /// <para>Since a merged line is now always one rate, its sum equals qty x rate anyway, bar
        /// that same rounding half. The reader's arithmetic checks out and the books still hold.</para>
        /// </remarks>
        public decimal PrintAmount
        {
            get
            {
                if (Members == null || Members.Count == 0)
                    return Math.Round(PrintQty * PrintUnitPrice, 2, MidpointRounding.AwayFromZero);
                decimal money = 0m;
                foreach (MeterBillLine m in Members) money += m.Charge;
                return Math.Round(money, 2, MidpointRounding.AwayFromZero);
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
            List<ScpFoldedLine> result = new List<ScpFoldedLine>();
            if (lines == null || lines.Count == 0) return result;
            ContractLayout lay = new ContractLayout();
            lay.HasFormat = true;
            lay.RentalMode = rentalMode;
            lay.MeterMode = meterMode;

            Dictionary<string, ScpFoldedLine> byKey =
                new Dictionary<string, ScpFoldedLine>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine ln in lines)
            {
                string key = FoldKey(ln, lay, true);
                ScpFoldedLine grp;
                if (key != null && byKey.TryGetValue(key, out grp)) { grp.Members.Add(ln); continue; }
                grp = new ScpFoldedLine(ln);
                if (key != null) byKey[key] = grp;
                result.Add(grp);
            }
            foreach (ScpFoldedLine g in result) Aggregate(g);
            return result;
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
            return result;
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
            // A committed minimum or a waive explains ONE machine on its own line; merging would
            // throw the explanation away.
            if (ln.IsCommittedMin || ln.IsWaiveMeter) return null;

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
            m += "|" + ScpRentalGroupPrice.GroupKeyFor(lay.MeterMode, ln.ModelCode, ln.MergeGroupCode);
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
            g.RebateQty = 0m; g.CurDate = null; g.PrevDate = null;
            foreach (MeterBillLine m in g.Members)
            {
                g.BillCopies += m.BillCopies;
                g.RebateQty += m.RebateQty;
                g.Current += m.Current;
                g.Last += m.Last;
                g.Usage += m.Usage;
                // Copies the ALLOWANCE took, which is not everything missing from BillCopies — the
                // rebate took its own share and is reported on its own row.
                decimal foc = m.IsFlat ? m.Foc : (m.Usage - m.BillCopies - m.RebateQty);
                if (foc < 0m) foc = 0m;
                g.FocApplied += foc;

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
                    "ISNULL(RentalLineMode,'A') AS RentalLineMode, ISNULL(MeterLineMode,'S') AS MeterLineMode " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                {
                    ContractLayout lay = new ContractLayout();
                    lay.HasFormat = Convert.ToString(r["BillingFormatCode"]).Trim().Length > 0;
                    lay.RentalMode = FirstChar(r["RentalLineMode"], ScpBillingFormat.LINE_ACROSS_MODEL);
                    lay.MeterMode = FirstChar(r["MeterLineMode"], ScpBillingFormat.LINE_PER_MACHINE);
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
                    "ISNULL(RentalLineMode,'A') AS RentalLineMode " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                    map[Convert.ToInt64(r["ContractKey"])] =
                        Convert.ToString(r["BillingFormatCode"]).Trim().Length == 0
                            ? ScpBillingFormat.LINE_PER_MACHINE
                            : FirstChar(r["RentalLineMode"], ScpBillingFormat.LINE_ACROSS_MODEL);
            }
            catch { }   // older book without the v14 columns -> no contract has groups
            return map;
        }

        /// <summary>The global that RentalLineMode replaces. Still honoured for contracts that have
        /// not been given a format, so nothing changes underneath a book mid-migration.</summary>
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
