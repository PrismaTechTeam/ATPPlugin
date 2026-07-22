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
        /// <summary>All tier ladders keyed by MeterMultiPriceCode. Each value is the ascending
        /// [boundary, unitPrice] rows. Load once per billing run (a per-row SELECT would be too slow).</summary>
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
            return map;
        }

        /// <summary>MARGINAL charge for a usage quantity over the tier ladder: each band (prevBound..bound]
        /// is priced at that band's UnitPrice. Returns the gross charge and, via <paramref name="freeCopies"/>,
        /// how many copies fell in bands priced 0.00 (the FOC allowance) — the caller uses that to show the
        /// billed (non-free) quantity on the invoice. Beyond the last boundary the last band's price applies.</summary>
        public static decimal MarginalCharge(Dictionary<string, List<decimal[]>> ladders,
            string code, decimal usage, out decimal freeCopies)
        {
            freeCopies = 0m;
            List<decimal[]> tiers;
            if (usage <= 0m || ladders == null || string.IsNullOrEmpty(code)
                || !ladders.TryGetValue(code, out tiers) || tiers.Count == 0) return 0m;

            decimal charge = 0m, remaining = usage, prevBound = 0m, lastPrice = 0m;
            foreach (decimal[] t in tiers)   // ascending by boundary
            {
                decimal bound = t[0], price = t[1];
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

        /// <summary>True when a usable ladder exists for the code.</summary>
        public static bool HasLadder(Dictionary<string, List<decimal[]>> ladders, string code)
        {
            if (ladders == null || string.IsNullOrEmpty(code)) return false;
            List<decimal[]> tiers;
            return ladders.TryGetValue(code, out tiers) && tiers.Count > 0;
        }

        private static decimal AsDec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }
    }
}
