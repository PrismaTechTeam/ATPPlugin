using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Multiple-pricing (tiered unit price) evaluator. A tier ladder is a code -> ascending list of
    /// (MeterReading boundary, UnitPrice) rows, where each boundary is the UPPER edge of that band.
    ///
    /// <para><b>Semantics = THRESHOLD.</b> The month's copies decide WHICH band applies, and then the
    /// whole bill is at that band's rate. Print 12,000 against 10,000 -> 0.025 -> above -> 0.020, and
    /// every billed copy is 0.020. This is the deal as it is struck and as the customer reads it:
    /// "print more and the rate comes down", one rate, one line, a figure they can multiply.</para>
    ///
    /// <para>It is not incremental. Slicing the copies and charging each slice its own band's rate
    /// leaves the line with no single price — an invoice then has to print a blended figure nobody
    /// agreed to (15,000 copies coming to 335.00 reads as 0.0223), or split one charge into a row per
    /// band. Both describe arithmetic the customer never signed.</para>
    ///
    /// <para><b>The 0.00 band is different, and stays an allowance.</b> "First 2,500 free, then 0.025"
    /// is how every real ladder in the book writes its free copies: those copies come off the top and
    /// the rest is billed. 97 of the 99 ladders in the customer's book are exactly this shape — one
    /// free band and one price — for which threshold and incremental are the same number anyway.</para>
    ///
    /// <para>Never throws; no ladder -> flat rate.</para>
    /// </summary>
    public static class ScpMultiPrice
    {
        /// <summary>The bands written as text: "boundary|price;boundary|price;...". Ascending, and a
        /// boundary of nothing is not a band. This is how a ladder travels when it is not a row in a
        /// table — a meter's own override, or a group's agreed tiers.</summary>
        public static List<decimal[]> ParseTiers(string csv)
        {
            List<decimal[]> rows = new List<decimal[]>();
            if (string.IsNullOrEmpty(csv)) return rows;
            foreach (string part in csv.Split(';'))
            {
                string[] ab = part.Split('|');
                if (ab.Length != 2) continue;
                decimal mr, up;
                if (decimal.TryParse(ab[0], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out mr) &&
                    decimal.TryParse(ab[1], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out up) && mr > 0m)
                    rows.Add(new decimal[] { mr, up });
            }
            return rows;
        }

        /// <summary>
        /// What a ladder reads as on a screen, in the words of the deal rather than the words of the
        /// data: <c>3,000 free, then 0.0250 \u2192 0.0200</c>.
        ///
        /// <para>It used to say "3 bands". A band is our word for a row in a table; nobody agreeing a
        /// contract has ever said it. What the customer wants to see is the free copies and what the
        /// price comes down to, which is the whole of what they signed.</para>
        /// </summary>
        public static string Describe(List<decimal[]> tiers)
        {
            if (tiers == null || tiers.Count == 0) return "";
            decimal free = LadderFreeCopies(tiers);

            // The prices actually charged, in the order the copies meet them. Repeats collapse: a
            // ladder written as three rows at one price is one price.
            List<decimal> prices = new List<decimal>();
            foreach (decimal[] t in tiers)
                if (t[1] > 0m && !prices.Contains(t[1])) prices.Add(t[1]);

            string rate = "";
            if (prices.Count == 1) rate = prices[0].ToString("0.0000");
            else if (prices.Count > 1)
                rate = prices[0].ToString("0.0000") + " \u2192 " +
                       prices[prices.Count - 1].ToString("0.0000");

            if (free > 0m && rate.Length > 0)
                return free.ToString("#,##0") + " free, then " + rate;
            if (free > 0m) return free.ToString("#,##0") + " free";
            return rate.Length > 0 ? rate + " by volume" : "";
        }

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

        /// <summary>
        /// What a usage costs on a tier ladder, and at what rate.
        ///
        /// <para>The copies decide the band; the band's rate then applies to every billed copy.
        /// <paramref name="freeCopies"/> comes back holding the copies that fell in 0.00-priced bands
        /// — the allowance, which comes off before anything is charged — and
        /// <paramref name="rate"/> the price the line prints. Beyond the last boundary the last
        /// band's rate applies.</para>
        /// </summary>
        public static decimal LadderCharge(Dictionary<string, List<decimal[]>> ladders,
            string code, decimal usage, int boundaryScale, out decimal freeCopies, out decimal rate)
        {
            freeCopies = 0m;
            rate = 0m;
            List<decimal[]> tiers;
            if (usage <= 0m || ladders == null || string.IsNullOrEmpty(code)
                || !ladders.TryGetValue(code, out tiers) || tiers.Count == 0) return 0m;
            if (boundaryScale < 1) boundaryScale = 1;   // FOC reset accrual: bands are per reset period

            // The allowance first: copies sitting in a 0.00 band are free wherever that band is.
            decimal remaining = usage, prevBound = 0m;
            foreach (decimal[] t in tiers)
            {
                decimal bound = t[0] * boundaryScale;
                decimal width = bound - prevBound;
                if (width < 0m) width = 0m;
                decimal take = remaining < width ? remaining : width;
                if (take > 0m && t[1] == 0m) freeCopies += take;
                remaining -= take > 0m ? take : 0m;
                prevBound = bound;
                if (remaining <= 0m) break;
            }

            // Then the rate: the band the month's copies REACHED. Not the band each slice sits in --
            // the customer prints 12,000 and expects the 12,000 rate on the lot.
            rate = PriceAt(tiers, usage, boundaryScale);
            decimal billed = usage - freeCopies;
            if (billed < 0m) billed = 0m;
            return billed * rate;
        }

        /// <summary>The ladder behind a code, or null.</summary>
        public static List<decimal[]> Tiers(Dictionary<string, List<decimal[]>> ladders, string code)
        {
            List<decimal[]> tiers;
            if (ladders == null || string.IsNullOrEmpty(code) || !ladders.TryGetValue(code, out tiers)) return null;
            return tiers;
        }

        /// <summary>
        /// Feedback ATP-3, band by band: lay BILLABLE copies -- what is left after the free copies --
        /// over the ladder's priced bands, each band's share at its own rate. A band's boundary is its
        /// upper edge counted in billable copies (scaled per reset period, like the threshold bands);
        /// the last band runs on without end. 0.00 bands are skipped: they are the old free allowance,
        /// already taken off before this is called, so a ladder converted to Free Qty and one that
        /// still carries its 0.00 band split the same copies the same way.
        /// Returns {copies, rate, amount} for each band that received copies; amounts rounded per band.
        /// </summary>
        public static List<decimal[]> Slices(List<decimal[]> tiers, decimal billable, int boundaryScale, bool awayFromZero)
        {
            List<decimal[]> result = new List<decimal[]>();
            if (tiers == null || tiers.Count == 0 || billable <= 0m) return result;
            if (boundaryScale < 1) boundaryScale = 1;
            List<decimal[]> priced = new List<decimal[]>();
            foreach (decimal[] t in tiers) if (t[1] != 0m) priced.Add(t);
            priced.Sort(delegate (decimal[] a, decimal[] b) { return a[0].CompareTo(b[0]); });
            if (priced.Count == 0) return result;
            decimal remaining = billable, prev = 0m;
            for (int i = 0; i < priced.Count && remaining > 0m; i++)
            {
                bool last = i == priced.Count - 1;
                decimal take = remaining;
                if (!last)
                {
                    decimal width = priced[i][0] * boundaryScale - prev;
                    if (width < 0m) width = 0m;
                    if (width < take) take = width;
                    prev = priced[i][0] * boundaryScale;
                }
                if (take <= 0m) continue;
                decimal amt = awayFromZero
                    ? Math.Round(take * priced[i][1], 2, MidpointRounding.AwayFromZero)
                    : Math.Round(take * priced[i][1], 2);
                result.Add(new decimal[] { take, priced[i][1], amt });
                remaining -= take;
            }
            return result;
        }

        /// <summary>Feedback ATP-3: why these bands cannot be saved, or "" when they can. The first band
        /// may not be 0.00 -- free copies are the meter's Free Qty, set on the contract's meter grid,
        /// not a band of the ladder.</summary>
        public static string WhyTiersWrong(List<decimal[]> tiers)
        {
            if (tiers == null || tiers.Count == 0) return "";
            List<decimal[]> sorted = new List<decimal[]>(tiers);
            sorted.Sort(delegate (decimal[] a, decimal[] b) { return a[0].CompareTo(b[0]); });
            if (sorted[0][1] == 0m)
                return "The first tier cannot be 0.00. Free copies are the meter's Free Qty, on the contract's " +
                       "meter grid -- give the meter its Free Qty there and start the tiers at the first paid rate.";
            return "";
        }

        /// <summary>The rate a given quantity earns: the band it lands in, or the last band's rate
        /// once it is past the ladder's top boundary.</summary>
        public static decimal PriceAt(List<decimal[]> tiers, decimal usage, int boundaryScale)
        {
            if (tiers == null || tiers.Count == 0) return 0m;
            if (boundaryScale < 1) boundaryScale = 1;
            decimal last = 0m;
            foreach (decimal[] t in tiers)   // ascending by boundary
            {
                last = t[1];
                if (usage <= t[0] * boundaryScale) return t[1];
            }
            return last;
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
