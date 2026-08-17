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

        /// <summary>What the row comes to. Deliberately Qty x UnitPrice rather than a sum of the
        /// members' charges, because that is what the invoice line itself computes — a merged row is
        /// rounded once, which is why HSI prints 3,048.36 where the per-machine charges add to
        /// 3,048.37. Preview and invoice therefore cannot disagree.</summary>
        public decimal PrintAmount
        {
            get { return Math.Round(PrintQty * PrintUnitPrice, 2, MidpointRounding.AwayFromZero); }
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
        /// <para>The unit price is always part of the key. Two machines at different money can never
        /// share a Qty x UnitPrice row without the arithmetic on the invoice ceasing to be true.</para>
        /// </summary>
        private static string FoldKey(MeterBillLine ln, ContractLayout lay, bool legacyRentalFold)
        {
            if (ln == null) return null;
            // A committed minimum or a waive explains ONE machine on its own line; merging would
            // throw the explanation away. Ladder lines carry a blended per-copy price that only
            // means anything for the machine it was computed from.
            if (ln.IsCommittedMin || ln.IsWaiveMeter || !string.IsNullOrEmpty(ln.MultiPriceCode))
                return null;

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

                // A merged row is ONE line. Merge means merge: the machines' individual prices do
                // not keep it apart, because the merged line carries a price of its own -- the same
                // idea the legacy ".C" combine meter had, one meter with one rate standing for the
                // whole group. Where the members' prices differ the row prices itself at the rate
                // that reproduces their total (see ScpFoldedLine.PrintUnitPrice), until a contract
                // sets one explicitly.
                //
                // The instalment counter stays in: "3 UNIT ... (13/36)" is a claim about all three
                // machines, and a machine that joined later is genuinely on a different month.
                string k = "R|" + ln.ContractKey + "|" + (ln.MeterTypeCode ?? "") + "|" +
                           (ln.ACItemCode ?? "") + "|" +
                           ln.RentalMonths + "/" + RentalMonthNo(ln) + "|" + (ln.StrategyNote ?? "");
                if (lay.RentalMode == ScpBillingFormat.LINE_SAME_MODEL) k += "|" + (ln.ModelCode ?? "");
                return k;
            }

            // ----- BK / CL -----
            if (!lay.HasFormat) return null;                                  // legacy never merged usage
            if (lay.MeterMode == ScpBillingFormat.LINE_PER_MACHINE) return null;

            // Neither the price nor the rebate splits a merged row. Merge means one line: the row
            // sums its members' copies and prices itself at the rate that reproduces their money.
            string m = "M|" + ln.ContractKey + "|" + (ln.ColorLabel ?? "") + "|" + (ln.MeterTypeCode ?? "") + "|" +
                       (ln.ACItemCode ?? "") + "|" + (ln.StrategyNote ?? "");
            // The duty label is NEVER part of a key, in any mode. It is a description -- the word the
            // line prints -- and nothing more. What actually separates rows is the price (always,
            // because a row is one Qty x UnitPrice) and, under "same model", the model.
            //
            // The real invoices confirm the label is never needed: JPJ tags twelve machines HEAVY /
            // MEDIUM / LIGHT and prints ONE black line of 80,720 because they share a rate; its
            // rental splits three ways on price. MBJB prints "MEDIUM HEAVY DUTY" on both its C5160
            // and C5150 rows and they stay apart on model. Pasir Gudang's two MEDIUM DUTY rental
            // lines are two different models.
            if (lay.MeterMode == ScpBillingFormat.LINE_SAME_MODEL) m += "|" + (ln.ModelCode ?? "");
            return m;
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
