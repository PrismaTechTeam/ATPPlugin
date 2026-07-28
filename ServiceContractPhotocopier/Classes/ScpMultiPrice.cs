using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Multiple-pricing (tiered unit price) evaluator. A tier ladder is a code -> ascending list of
    /// (MeterReading boundary, UnitPrice) rows from zSCP_MeterMultiPriceItem, where each boundary is the
    /// UPPER edge of that band. Semantics = MARGINAL (incremental): each slice of usage is priced at its
    /// own band's rate. This is what the customer's real ladders need — they encode the free-copy allowance
    /// as a first band priced 0.00 (e.g. "FOC2.5K" = (2500 -> 0.00), (∞ -> 0.025) = first 2500 free, the
    /// rest at 0.025), so a usage of 3000 costs 2500x0 + 500x0.025 = RM12.50 (NOT 3000x0.025). Decreasing
    /// bands still give "print more, cheaper unit price". Never throws; no ladder -> flat rate.
    /// </summary>
    public static class ScpMultiPrice
    {
        /// <summary>Ladder-map key for a meter's own per-meter tier override (zSCP2_ItemMeterPrice).</summary>
        public static string MeterLadderKey(long itemMeterKey) { return "#" + itemMeterKey; }

        /// <summary>All tier ladders keyed by MeterMultiPriceCode, PLUS every per-meter override
        /// ladder (zSCP2_ItemMeterPrice) keyed "#&lt;ItemMeterKey&gt;" — a meter with an override uses
        /// that key as its MultiPriceCode so the billing engine needs no special casing. Each value
        /// is the ascending [boundary, unitPrice] rows. Load once per billing run.</summary>
        public static Dictionary<string, List<decimal[]>> LoadLadders(DBSetting db)
        {
            Dictionary<string, List<decimal[]>> map =
                new Dictionary<string, List<decimal[]>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable dt = db.GetDataTable(
                    "SELECT MeterMultiPriceCode, MeterReading, UnitPrice " +
                    "FROM dbo.zSCP_MeterMultiPriceItem ORDER BY MeterMultiPriceCode, MeterReading", false);
                foreach (DataRow r in dt.Rows)
                {
                    string code = Convert.ToString(r["MeterMultiPriceCode"]);
                    if (string.IsNullOrEmpty(code)) continue;
                    List<decimal[]> tiers;
                    if (!map.TryGetValue(code, out tiers)) { tiers = new List<decimal[]>(); map[code] = tiers; }
                    tiers.Add(new decimal[] { AsDec(r["MeterReading"]), AsDec(r["UnitPrice"]) });
                }
            }
            catch { }
            try
            {
                DataTable pm = db.GetDataTable(
                    "SELECT ItemMeterKey, MeterReading, UnitPrice " +
                    "FROM dbo.zSCP2_ItemMeterPrice ORDER BY ItemMeterKey, MeterReading", false);
                foreach (DataRow r in pm.Rows)
                {
                    string key = MeterLadderKey(Convert.ToInt64(r["ItemMeterKey"]));
                    List<decimal[]> tiers;
                    if (!map.TryGetValue(key, out tiers)) { tiers = new List<decimal[]>(); map[key] = tiers; }
                    tiers.Add(new decimal[] { AsDec(r["MeterReading"]), AsDec(r["UnitPrice"]) });
                }
            }
            catch { }   // pre-migration book: overrides simply absent
            return map;
        }

        /// <summary>MARGINAL charge for a usage quantity over the tier ladder: each band (prevBound..bound]
        /// is priced at that band's UnitPrice. Returns the gross charge and, via <paramref name="freeCopies"/>,
        /// how many copies fell in bands priced 0.00 (the FOC allowance) — the caller uses that to show the
        /// billed (non-free) quantity on the invoice. Beyond the last boundary the last band's price applies.</summary>
        public static decimal MarginalCharge(Dictionary<string, List<decimal[]>> ladders,
            string code, decimal usage, int boundaryScale, out decimal freeCopies)
        {
            freeCopies = 0m;
            List<decimal[]> tiers;
            if (usage <= 0m || ladders == null || string.IsNullOrEmpty(code)
                || !ladders.TryGetValue(code, out tiers) || tiers.Count == 0) return 0m;
            if (boundaryScale < 1) boundaryScale = 1;   // FOC reset accrual: bands are per reset period

            decimal charge = 0m, remaining = usage, prevBound = 0m, lastPrice = 0m;
            foreach (decimal[] t in tiers)   // ascending by boundary
            {
                decimal bound = t[0] * boundaryScale, price = t[1];
                lastPrice = price;
                decimal bandWidth = bound - prevBound;
                if (bandWidth < 0m) bandWidth = 0m;
                decimal take = remaining < bandWidth ? remaining : bandWidth;
                if (take > 0m)
                {
                    charge += take * price;
                    if (price == 0m) freeCopies += take;
                    remaining -= take;
                }
                prevBound = bound;
                if (remaining <= 0m) break;
            }
            if (remaining > 0m)   // usage exceeds the last boundary — price the tail at the last band's rate
            {
                charge += remaining * lastPrice;
                if (lastPrice == 0m) freeCopies += remaining;
            }
            return charge;
        }

        /// <summary>Total FREE copies a tier ladder grants: the summed width of its 0.00-priced
        /// bands (usually the first band, e.g. "FOC20K" -> 20,000). Used by the meter grids to
        /// DISPLAY the effective free quantity while a ladder locks the Free Qty cell.</summary>
        public static decimal LadderFreeCopies(List<decimal[]> tiers)
        {
            if (tiers == null || tiers.Count == 0) return 0m;
            List<decimal[]> sorted = new List<decimal[]>(tiers);
            sorted.Sort(delegate (decimal[] a, decimal[] b) { return a[0].CompareTo(b[0]); });
            decimal free = 0m, prev = 0m;
            foreach (decimal[] t in sorted)
            {
                decimal width = t[0] - prev;
                if (width > 0m && t[1] == 0m) free += width;
                prev = t[0];
            }
            return free;
        }

        /// <summary>True when a usable ladder exists for the code.</summary>
        public static bool HasLadder(Dictionary<string, List<decimal[]>> ladders, string code)
        {
            if (ladders == null || string.IsNullOrEmpty(code)) return false;
            List<decimal[]> tiers;
            return ladders.TryGetValue(code, out tiers) && tiers.Count > 0;
        }

        private static decimal AsDec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }

        /// <summary>How many FOC-reset periods fall in a billed span of <paramref name="periodDays"/> days.
        /// Unit 'M' (or default) = monthly reset = 1 per monthly bill; 'W' = weekly (7d); 'D' = every N days.
        /// Rounds to the nearest whole reset period, minimum 1 (e.g. ~30-day month + weekly -> 4; + 3-day -> 10).</summary>
        public static int FocResetCount(string unit, int n, int periodDays)
        {
            if (periodDays <= 0) periodDays = 30;
            int resetDays;
            if (unit == "W") resetDays = 7;
            else if (unit == "D") resetDays = n > 0 ? n : 30;
            else return 1;   // 'M' / empty -> monthly reset = one allowance per monthly bill
            int c = (int)Math.Round((decimal)periodDays / resetDays, MidpointRounding.AwayFromZero);
            return c < 1 ? 1 : c;
        }
    }
}
