using System;
using System.Collections.Generic;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Tier pricing agreed over a GROUP of machines rather than over one.
    ///
    /// <para><b>Why it cannot be done by merging alone.</b> Two machines on the same ladder already
    /// print on one line — but each of them climbed that ladder on its own copies, and the line just
    /// adds the two charges up. Merging decides how many LINES print; it has never decided what
    /// anything costs. So five thousand copies from each of two machines pays the first band twice,
    /// which is the opposite of what a customer means by "we'll do ten thousand between us, so give
    /// us the ten-thousand rate".</para>
    ///
    /// <para><b>What this does.</b> The group's copies are added up and climb ONE ladder, once. The
    /// resulting blended rate goes back on every machine in the group, and the free copies (the
    /// machines' Free Qty, pooled -- ATP-3) are shared out in proportion to what each printed — so the machines' own charges still add up to the
    /// line, and the reading log still says what each machine cost.</para>
    ///
    /// <para><b>What it does not do.</b> It never touches a machine that carries a ladder of its own.
    /// An own ladder IS the statement that this machine is priced unlike the others, so it keeps its
    /// ladder and keeps its own line — and it is left out of the group's total as well, because its
    /// copies were never part of the deal the group struck.</para>
    ///
    /// <para>Nor does it change what a shared ladder CODE has always done. Two machines pointing at
    /// the same master ladder still climb it one at a time; a contract that wants the combined volume
    /// says so by agreeing a group ladder here. Making the code mean something new would have moved
    /// money on contracts nobody touched.</para>
    /// </summary>
    public static class ScpGroupLadder
    {
        /// <summary>The ladder-map key a group's tiers live under. Shaped so it can never collide
        /// with a master ladder code (which has no '|') or a per-meter override ("#key").</summary>
        public static string KeyFor(long contractKey, string groupCode, bool colour)
        {
            return "G|" + contractKey + "|" + (groupCode ?? "").Trim().ToUpperInvariant() +
                   "|" + (colour ? "CL" : "BK");
        }

        /// <summary>The bands a stored ladder means: either a master code from the price list, or
        /// this group's own "boundary|price;..." text.</summary>
        public static List<decimal[]> Resolve(Dictionary<string, List<decimal[]>> ladders, string stored)
        {
            string t = (stored ?? "").Trim();
            if (t.Length == 0) return null;
            if (t.IndexOf('|') >= 0)
            {
                List<decimal[]> own = ScpMultiPrice.ParseTiers(t);
                return own.Count > 0 ? own : null;
            }
            List<decimal[]> byCode;
            if (ladders != null && ladders.TryGetValue(t, out byCode) && byCode.Count > 0) return byCode;
            return null;
        }

        /// <summary>
        /// Price every copy group that has agreed a ladder, over the group's copies added up.
        ///
        /// <para>Runs after the per-machine charges exist and before anything that reads them — a
        /// minimum measures charges, and a waive is decided by them, so both must see the group's
        /// price and not the price the machines would have paid alone.</para>
        /// </summary>
        public static void Apply(List<MeterBillLine> lines,
            Dictionary<long, Dictionary<string, ScpLineTerms>> termsByContract,
            Dictionary<string, List<decimal[]>> ladders)
        {
            if (lines == null || lines.Count == 0 || termsByContract == null) return;

            Dictionary<string, List<MeterBillLine>> groups =
                new Dictionary<string, List<MeterBillLine>>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine ln in lines)
            {
                if (ln == null || ln.IsFlat) continue;
                if (!ln.NewMoneyRules) continue;            // the old rules never had group prices
                if (ln.ColorLabel != "Black" && ln.ColorLabel != "Colour") continue;
                // A machine priced unlike the others keeps its own ladder, its own line, and its
                // copies stay out of the group's total.
                if ((ln.MultiPriceCode ?? "").Trim().Length > 0) continue;
                string grp = (ln.MergeGroupCodeMeter ?? "").Trim();
                if (grp.Length == 0) continue;              // on its own line: nothing to share with
                string key = KeyFor(ln.ContractKey, grp, ln.ColorLabel == "Colour");
                List<MeterBillLine> set;
                if (!groups.TryGetValue(key, out set)) { set = new List<MeterBillLine>(); groups[key] = set; }
                set.Add(ln);
            }

            foreach (KeyValuePair<string, List<MeterBillLine>> kv in groups)
            {
                List<MeterBillLine> set = kv.Value;
                MeterBillLine lead = set[0];
                Dictionary<string, ScpLineTerms> terms;
                if (!termsByContract.TryGetValue(lead.ContractKey, out terms) || terms == null)
                {
                    // A preview builds its lines from a contract that may not be saved yet, so they
                    // carry no key to look up. One contract's terms in the map is that contract's.
                    if (termsByContract.Count != 1) continue;
                    terms = null;
                    foreach (KeyValuePair<long, Dictionary<string, ScpLineTerms>> only in termsByContract)
                        terms = only.Value;
                    if (terms == null) continue;
                }
                ScpLineTerms t;
                if (!terms.TryGetValue(ScpLineTerms.Key(ScpLineTerms.SIDE_METER,
                        (lead.MergeGroupCodeMeter ?? "").Trim()), out t) || t == null) continue;

                bool colour = lead.ColorLabel == "Colour";
                List<decimal[]> tiers = Resolve(ladders, colour ? t.LadderCl : t.LadderBk);
                if (tiers == null) continue;

                // The ladder is registered under the group's own key and stamped on every machine in
                // it. That does two jobs at once: the charge below is worked out from it, and the fold
                // keys lines on their ladder — so the group prints as ONE line without being told to.
                if (ladders != null) ladders[kv.Key] = tiers;
                Price(set, ladders, kv.Key);
            }
        }

        /// <summary>One group's copies over one ladder, then shared back out.</summary>
        private static void Price(List<MeterBillLine> set,
            Dictionary<string, List<decimal[]>> ladders, string key)
        {
            decimal groupUsage = 0m;
            foreach (MeterBillLine m in set)
            {
                decimal u = m.Current - m.Last;
                if (u < 0m) u = 0m;
                m.Usage = u;
                groupUsage += u;
            }

            // The free copies refresh every reset period, and every machine on one contract bills over
            // the same span, so the leader's count speaks for the group.
            int resetN = set[0].FocResetCount < 1 ? 1 : set[0].FocResetCount;
            decimal freeCopies, effUnit;
            decimal gross = ScpMultiPrice.LadderCharge(ladders, key, groupUsage, resetN,
                                                       out freeCopies, out effUnit);
            // ATP-3: the group's free copies are its machines' Free Qty, added up (plus a 0.00 band an
            // old ladder may still carry). The group is priced as one, so its allowance is one pool.
            decimal ownFree = 0m;
            foreach (MeterBillLine m in set) ownFree += m.Foc * resetN;
            if (ownFree > 0m)
            {
                freeCopies += ownFree;
                if (freeCopies > groupUsage) freeCopies = groupUsage;
                gross = (groupUsage - freeCopies) * effUnit;
            }
            decimal billedGroup = groupUsage - freeCopies;
            if (billedGroup < 0m) billedGroup = 0m;

            // The free copies are the GROUP's, so they are shared in proportion to what each machine
            // printed, and the last machine takes whatever the rounding left. Sharing this way is what
            // keeps the machines' billed copies adding up to the line's.
            decimal grossR = Math.Round(gross, 2, MidpointRounding.AwayFromZero);

            decimal given = 0m;
            decimal givenFree = 0m;
            for (int i = 0; i < set.Count; i++)
            {
                MeterBillLine m = set[i];
                m.MultiPriceCode = key;
                decimal mine = i == set.Count - 1
                    ? freeCopies - givenFree
                    : (groupUsage > 0m ? Math.Floor(freeCopies * m.Usage / groupUsage) : 0m);
                if (mine < 0m) mine = 0m;
                if (mine > m.Usage) mine = m.Usage;
                givenFree += mine;

                decimal billed = m.Usage - mine;
                if (billed < 0m) billed = 0m;

                // The rebate stays a per-machine deduction in COPIES, floored on each and then added
                // up — the same arithmetic the customer's own merged lines show.
                if (m.RebatePct > 0m)
                {
                    m.RebateQty = Math.Floor(billed * m.RebatePct / 100m);
                    if (m.RebateQty < 0m) m.RebateQty = 0m;
                    billed -= m.RebateQty;
                    if (billed < 0m) billed = 0m;
                }
                else m.RebateQty = 0m;

                m.BillCopies = billed;
                m.EffUnitPrice = effUnit;

                // The group agreed ONE figure, so the machines must add up to it. Pricing each of
                // them off the blended rate and rounding separately does not: 5,000 copies at
                // 0.022333 is 111.665 three times, which rounds to 335.01 against a deal worth
                // 335.00. The last machine takes what is left, the way any allocation of one amount
                // over several rows has to.
                decimal charge = i == set.Count - 1
                    ? grossR - given
                    : Math.Round(billed * effUnit, 2, MidpointRounding.AwayFromZero);
                if (charge < 0m) charge = 0m;
                given += charge;
                if (charge < m.MinCharges) { m.Charge = m.MinCharges; m.UseMin = true; }
                else { m.Charge = charge; m.UseMin = false; }
            }

            // ATP-3, band by band: the group's billable copies over the priced bands as ONE count,
            // and the resulting amount shared back by copies the way the threshold amount is. The
            // bands are the group's -- the same list on every machine -- and print once.
            if (set[0].TierIncremental)
            {
                decimal totalBilled = 0m;
                foreach (MeterBillLine m in set) totalBilled += m.BillCopies;
                List<decimal[]> bands = ScpMultiPrice.Slices(ScpMultiPrice.Tiers(ladders, key), totalBilled, resetN, true);
                decimal groupCharge = 0m;
                foreach (decimal[] b in bands) groupCharge += b[2];
                decimal blended = bands.Count == 1 ? bands[0][1]
                    : (bands.Count > 1 ? Math.Round(groupCharge / totalBilled, 6, MidpointRounding.AwayFromZero) : effUnit);
                decimal shared = 0m;
                for (int i = 0; i < set.Count; i++)
                {
                    MeterBillLine m = set[i];
                    decimal part = i == set.Count - 1
                        ? groupCharge - shared
                        : (totalBilled > 0m ? Math.Round(groupCharge * m.BillCopies / totalBilled, 2, MidpointRounding.AwayFromZero) : 0m);
                    if (part < 0m) part = 0m;
                    shared += part;
                    m.EffUnitPrice = blended;
                    m.TierBands = bands.Count >= 2 ? bands : null;
                    m.TierBandsAreGroup = bands.Count >= 2;
                    m.TierGroupSize = set.Count;
                    if (part < m.MinCharges) { m.Charge = m.MinCharges; m.UseMin = true; }
                    else { m.Charge = part; m.UseMin = false; }
                }
            }
        }
    }
}
