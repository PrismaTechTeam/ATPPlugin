using System;
using System.Collections.Generic;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// A committed minimum is a floor under print charges, not a charge. It bills only the shortfall,
    /// and it prints even when the shortfall is nothing, so the customer can see the arithmetic.
    ///
    /// <para>Two questions decide what it is measured against, and they are genuinely separate:
    /// <b>WaiveScope</b> says WHICH charges count — black only, colour only, or both — and
    /// <b>CommitScope</b> says WHOSE: this machine, this machine's merge group, or the whole
    /// contract. "RM 3,000 a month across these six machines, colour only" needs an answer to each.</para>
    ///
    /// <para>A pooled minimum is expressed by putting ONE committed meter on ONE machine of the group
    /// and scoping it to the group. The other machines carry none. That prints one line and tops up
    /// once; six meters at RM 3,000 would top up six times.</para>
    ///
    /// <para>This lives apart from the Meter Reading form so it can be tested. It is arithmetic on a
    /// list of lines — nothing in it needs a screen.</para>
    /// </summary>
    public static class ScpCommittedMin
    {
        /// <summary>The bucket a machine's print charges fall into for a group-scope minimum. A
        /// machine with no merge group is its own group, so "this group" on an ungrouped machine
        /// means that machine — never the whole contract by accident.</summary>
        public static string GroupKey(MeterBillLine l)
        {
            string g = (l.MergeGroupCode ?? "").Trim();
            return g.Length > 0
                ? l.ContractKey + "|#" + g.ToUpperInvariant()
                : l.ContractKey + "|@" + l.ItemKey;
        }

        /// <summary>What the invoice calls the set a minimum is measured over.</summary>
        public static string GroupWords(MeterBillLine l)
        {
            string g = (l.MergeGroupCode ?? "").Trim();
            return g.Length > 0 ? "group " + g.ToUpperInvariant() : "this machine";
        }

        /// <summary>The print charges of a run, bucketed the three ways a minimum can be measured.
        /// Built once and handed back, because the COMMIT-MIN strategy rule needs the same sums and
        /// summing them twice is how the two paths drift apart.</summary>
        public class PrintedSums
        {
            public readonly Dictionary<long, decimal> BkByItem = new Dictionary<long, decimal>();
            public readonly Dictionary<long, decimal> ClByItem = new Dictionary<long, decimal>();
            public readonly Dictionary<long, decimal> BkByContract = new Dictionary<long, decimal>();
            public readonly Dictionary<long, decimal> ClByContract = new Dictionary<long, decimal>();
            public readonly Dictionary<string, decimal> BkByGroup =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, decimal> ClByGroup =
                new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            /// <summary>One machine's printed charges under a black / colour / both scope.</summary>
            public decimal ForItem(long itemKey, string scope)
            {
                decimal bk, cl;
                BkByItem.TryGetValue(itemKey, out bk);
                ClByItem.TryGetValue(itemKey, out cl);
                string s = (scope ?? "").Trim().ToUpperInvariant();
                return s == "BK" ? bk : (s == "CL" ? cl : bk + cl);
            }
        }

        /// <summary>
        /// Rewrites every committed-minimum line in <paramref name="allLines"/> to its top-up, and
        /// returns the items that carry one — the COMMIT-MIN strategy rule must not top those up a
        /// second time.
        /// </summary>
        /// <remarks>
        /// The print charges are summed across ALL the lines given, not per invoice job, on purpose:
        /// a rental-separate or Bill Group split spreads one machine's lines over several jobs, and a
        /// per-job sum would read zero on the job that has no usage and bill the whole minimum there.
        /// </remarks>
        public static HashSet<long> ApplyMeterMinimums(IEnumerable<MeterBillLine> allLines)
        {
            PrintedSums ignored;
            return ApplyMeterMinimums(allLines, out ignored);
        }

        public static HashSet<long> ApplyMeterMinimums(IEnumerable<MeterBillLine> allLines,
            out PrintedSums printed)
        {
            printed = new PrintedSums();
            HashSet<long> minMeterItems = new HashSet<long>();
            if (allLines == null) return minMeterItems;

            // Sum the actual PRINT charges (non-flat BK/CL usage meters), split by colour so a
            // minimum can count black only, colour only or both, and bucketed three ways so it can be
            // measured against one machine, its group, or the contract.
            Dictionary<long, decimal> bkByItem = printed.BkByItem;
            Dictionary<long, decimal> clByItem = printed.ClByItem;
            Dictionary<long, decimal> bkByContract = printed.BkByContract;
            Dictionary<long, decimal> clByContract = printed.ClByContract;
            Dictionary<string, decimal> bkByGroup = printed.BkByGroup;
            Dictionary<string, decimal> clByGroup = printed.ClByGroup;

            foreach (MeterBillLine l in allLines)
            {
                if (l.IsFlat || l.ItemKey <= 0) continue;
                if (l.ColorLabel != "Black" && l.ColorLabel != "Colour") continue;
                bool black = l.ColorLabel == "Black";

                Dictionary<long, decimal> byItem = black ? bkByItem : clByItem;
                decimal t;
                byItem.TryGetValue(l.ItemKey, out t);
                byItem[l.ItemKey] = t + l.Charge;

                Dictionary<long, decimal> byContract = black ? bkByContract : clByContract;
                decimal tc;
                byContract.TryGetValue(l.ContractKey, out tc);
                byContract[l.ContractKey] = tc + l.Charge;

                string gk = GroupKey(l);
                Dictionary<string, decimal> byGroup = black ? bkByGroup : clByGroup;
                decimal tg;
                byGroup.TryGetValue(gk, out tg);
                byGroup[gk] = tg + l.Charge;
            }

            foreach (MeterBillLine l in allLines)
            {
                if (!l.IsCommittedMin) continue;
                minMeterItems.Add(l.ItemKey);

                decimal pBk = 0m, pCl = 0m;
                string who = (l.CommitScope ?? "S").Trim().ToUpperInvariant();
                if (l.IsGroupItem) who = "C";     // the fleet machine has always meant the whole contract
                if (who == "C")
                {
                    bkByContract.TryGetValue(l.ContractKey, out pBk);
                    clByContract.TryGetValue(l.ContractKey, out pCl);
                }
                else if (who == "G")
                {
                    string gk = GroupKey(l);
                    bkByGroup.TryGetValue(gk, out pBk);
                    clByGroup.TryGetValue(gk, out pCl);
                }
                else
                {
                    bkByItem.TryGetValue(l.ItemKey, out pBk);
                    clByItem.TryGetValue(l.ItemKey, out pCl);
                }

                string cscope = (l.WaiveScope ?? "BKCL").Trim().ToUpperInvariant();
                decimal charged = cscope == "BK" ? pBk : (cscope == "CL" ? pCl : pBk + pCl);
                decimal topUp = l.CommittedAmount - charged;
                if (topUp < 0m) topUp = 0m;

                string whoWords = who == "C" ? " (whole contract)"
                                : (who == "G" ? " (" + GroupWords(l) + ")" : "");
                l.PrintedAmount = charged;
                l.Charge = topUp;
                l.Foc = 0m;
                l.UseMin = false;
                l.StrategyNote = "COMMITTED MIN " + l.CommittedAmount.ToString("0.00") + whoWords +
                                 ": printed " + charged.ToString("0.00") +
                                 " -> top-up " + topUp.ToString("0.00");
            }
            return minMeterItems;
        }

        /// <summary>
        /// Two committed minimums measured against the same set would each top it up, and the
        /// customer would pay the shortfall twice. Returns the offending sets, or an empty list.
        /// </summary>
        /// <remarks>
        /// Checked on the contract screen rather than in the database, because the group a machine
        /// belongs to lives on a different table from the meter that names the group.
        /// </remarks>
        public static List<string> FindDoubleCounted(IEnumerable<MeterBillLine> allLines)
        {
            List<string> problems = new List<string>();
            if (allLines == null) return problems;
            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (MeterBillLine l in allLines)
            {
                if (!l.IsCommittedMin) continue;
                string who = (l.CommitScope ?? "S").Trim().ToUpperInvariant();
                if (l.IsGroupItem) who = "C";
                if (who != "G" && who != "C") continue;      // per-machine minimums cannot collide
                string key = who == "C" ? "C|" + l.ContractKey : "G|" + GroupKey(l);
                int n;
                seen.TryGetValue(key, out n);
                seen[key] = n + 1;
                words[key] = who == "C" ? "the whole contract" : GroupWords(l);
            }
            foreach (KeyValuePair<string, int> kv in seen)
                if (kv.Value > 1)
                    problems.Add(kv.Value + " committed minimums are measured against " + words[kv.Key] +
                                 " — each would top it up, so the shortfall would be billed " +
                                 kv.Value + " times. Keep one.");
            return problems;
        }
    }
}
