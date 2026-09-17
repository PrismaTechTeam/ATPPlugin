using System;
using System.Collections.Generic;
using System.Data;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// The rental price of a merged rental line, owned by the contract instead of by the machines.
    ///
    /// <para>A merged rental line is a deal: "3 UNIT ... MONTHLY RENTAL (1/36)" is one price agreed
    /// for a group of machines. Left on the machines, that price is only whatever the members happen
    /// to agree on, and the fourth machine added next month arrives at its own number. Held here,
    /// the group owns it -- every rental meter in the group bills at this price, and a machine that
    /// joins later joins at the group's price.</para>
    ///
    /// <para>This is rental only, and deliberately. Black and colour are not a deal: each meter has
    /// its own rate and produces its own charge, and a merged usage line is worth the sum of its
    /// machines (see <see cref="ScpFoldedLine.PrintAmount"/>).</para>
    ///
    /// <para>No row means no override. A contract nobody has priced bills exactly as before.</para>
    /// </summary>
    /// <summary>What one printed line charges, when the line has been given an agreed figure rather
    /// than letting each machine bill its own. Rental and copies are separate lines, so a line row
    /// carries a price for whichever charge it is.
    ///
    /// <para>Zero means "no agreement" -- the machines keep their own rates and, if those rates
    /// differ, the line comes apart into one line per rate. That is not a fault: a printed line is
    /// one Qty x Unit Price, and two prices cannot share the cell.</para></summary>
    public class ScpLineTerms
    {
        public const string SIDE_RENTAL = "R";
        public const string SIDE_METER = "M";

        public string GroupCode = "";
        public string Side = SIDE_RENTAL;
        public decimal UnitPrice;      // rental: a month for ONE machine
        public decimal BkPrice;        // copies: per black copy
        public decimal ClPrice;        // copies: per colour copy

        /// <summary>Tier pricing agreed over the whole group, black and colour. Either a master
        /// ladder code from the price list, or this group's own "boundary|price;..." bands.
        ///
        /// <para>A ladder outranks the flat figure beside it: where there are bands there is no
        /// single rate to agree. See <see cref="ScpGroupLadder"/> for what it does to the money —
        /// the group's copies climb it ONCE, which is the whole reason a customer asks for it.</para>
        /// </summary>
        public string LadderBk = "";
        public string LadderCl = "";

        public bool IsEmpty
        {
            get
            {
                return UnitPrice <= 0m && BkPrice <= 0m && ClPrice <= 0m
                    && (LadderBk ?? "").Trim().Length == 0 && (LadderCl ?? "").Trim().Length == 0;
            }
        }

        public static string Key(string side, string groupCode)
        {
            return (side ?? SIDE_RENTAL).Trim().ToUpperInvariant() + "|" +
                   (groupCode ?? "").Trim().ToUpperInvariant();
        }
    }

    public static class ScpRentalGroupPrice
    {
        /// <summary>Which line a machine falls on, under a merging line mode.</summary>
        /// <remarks>
        /// A hand-made group wins over the mode's own bucket, which is what lets a contract say
        /// "these two models together, that one apart" -- something no mode can express. Under
        /// "merge by model" the group MERGES models; under "merge, ignoring model" it SPLITS a set
        /// off the single line. Same rule, both readings.
        ///
        /// <para>The '#' is not decoration: without it a group named "C5335" and the model C5335
        /// would be the same bucket.</para>
        /// </remarks>
        public static string GroupKeyFor(char mode, string modelCode, string mergeGroupCode)
        {
            string g = (mergeGroupCode ?? "").Trim();
            if (g.Length > 0) return "#" + g.ToUpperInvariant();
            if (mode == ScpBillingFormat.LINE_SAME_MODEL) return (modelCode ?? "").Trim();
            return "";
        }

        /// <summary>Normalises a group name the user typed: trimmed, upper case, at most 40 chars.
        /// Case is not a distinction -- "office" and "OFFICE" are one group, and anything else would
        /// silently print two lines.</summary>
        public static string Sanitize(string raw)
        {
            if (raw == null) return "";
            string s = raw.Trim().ToUpperInvariant();
            return s.Length > 40 ? s.Substring(0, 40) : s;
        }

        /// <summary>Every line's agreed terms for one contract, keyed "SIDE|GROUPCODE". Never throws:
        /// an older book without the columns simply has no agreements.</summary>
        public static Dictionary<string, ScpLineTerms> LoadTerms(DBSetting db, long contractKey)
        {
            Dictionary<string, ScpLineTerms> map =
                new Dictionary<string, ScpLineTerms>(StringComparer.OrdinalIgnoreCase);
            if (db == null || contractKey <= 0) return map;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT ISNULL(Side,'R') AS Side, GroupCode, ISNULL(UnitPrice,0) AS UnitPrice, " +
                    "ISNULL(BkPrice,0) AS BkPrice, ISNULL(ClPrice,0) AS ClPrice, " +
                    "ISNULL(LadderBk,'') AS LadderBk, ISNULL(LadderCl,'') AS LadderCl " +
                    "FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + contractKey, false);
                foreach (DataRow r in t.Rows)
                {
                    ScpLineTerms x = new ScpLineTerms();
                    x.Side = Convert.ToString(r["Side"]).Trim().ToUpperInvariant();
                    x.GroupCode = Convert.ToString(r["GroupCode"]).Trim();
                    x.UnitPrice = Convert.ToDecimal(r["UnitPrice"]);
                    x.BkPrice = Convert.ToDecimal(r["BkPrice"]);
                    x.ClPrice = Convert.ToDecimal(r["ClPrice"]);
                    x.LadderBk = Convert.ToString(r["LadderBk"]).Trim();
                    x.LadderCl = Convert.ToString(r["LadderCl"]).Trim();
                    map[ScpLineTerms.Key(x.Side, x.GroupCode)] = x;
                }
            }
            catch { }
            return map;
        }

        /// <summary>Prices for one contract, keyed by GroupCode ('' = the whole contract).
        /// Never throws: an older book without the table simply has no overrides.</summary>
        public static Dictionary<string, decimal> LoadForContract(DBSetting db, long contractKey)
        {
            Dictionary<string, decimal> map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (db == null || contractKey <= 0) return map;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT GroupCode, UnitPrice FROM dbo.zSCP2_ContractRentalPrice " +
                    "WHERE ContractKey = " + contractKey + " AND ISNULL(Side,'R') = 'R'", false);
                foreach (DataRow r in t.Rows)
                    map[Convert.ToString(r["GroupCode"]).Trim()] =
                        r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
            }
            catch { }
            return map;
        }

        /// <summary>Prices for many contracts in one query -- the billing run's form.
        /// Outer key = ContractKey, inner = GroupCode ('' = the whole contract).</summary>
        public static Dictionary<long, Dictionary<string, decimal>> LoadForContracts(
            DBSetting db, IEnumerable<long> contractKeys)
        {
            Dictionary<long, Dictionary<string, decimal>> map =
                new Dictionary<long, Dictionary<string, decimal>>();
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
                    // Rental rows only: the copies now keep their agreed rates in the same table,
                    // on the M side, and a rental price map that swallowed them would price a rental
                    // line at a rate per copy.
                    "SELECT ContractKey, GroupCode, UnitPrice FROM dbo.zSCP2_ContractRentalPrice " +
                    "WHERE ContractKey IN (" + inList + ") AND ISNULL(Side,'R') = 'R'", false);
                foreach (DataRow r in t.Rows)
                {
                    long ck = Convert.ToInt64(r["ContractKey"]);
                    Dictionary<string, decimal> inner;
                    if (!map.TryGetValue(ck, out inner))
                    {
                        inner = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        map[ck] = inner;
                    }
                    // Stored under BOTH names GroupKeyFor can ask for, because the column serves two
                    // different things and the caller cannot tell them apart from here.
                    //
                    // Under "per model" the GroupCode column holds a MODEL, and GroupKeyFor returns the
                    // model as-is. Under a hand-made group it holds a GROUP, and GroupKeyFor returns it
                    // prefixed with '#' -- that prefix arrived with the rule "a group always wins over
                    // the mode", and this loader was never taught it. The lookup therefore missed every
                    // hand-made group, so an agreed rental price on a merged line was loaded, ignored,
                    // and every machine quietly billed its own rate instead.
                    string gc = Convert.ToString(r["GroupCode"]).Trim();
                    decimal up = r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                    inner[gc] = up;
                    if (gc.Length > 0) inner["#" + gc.ToUpperInvariant()] = up;
                }
            }
            catch { }   // older book without the table -> nothing is overridden
            return map;
        }

        /// <summary>The price this machine's rental should bill at, or null when the group has not
        /// been priced. A zero price is NOT an override -- a group priced at nothing is a group
        /// nobody has filled in yet, and the machines keep their own rates.</summary>
        public static decimal? PriceFor(Dictionary<long, Dictionary<string, decimal>> map,
            long contractKey, char rentalMode, string modelCode, string mergeGroupCode)
        {
            if (map == null) return null;
            if (rentalMode == ScpBillingFormat.LINE_PER_MACHINE) return null;   // no group, no group price
            Dictionary<string, decimal> inner;
            if (!map.TryGetValue(contractKey, out inner)) return null;
            decimal p;
            if (!inner.TryGetValue(GroupKeyFor(rentalMode, modelCode, mergeGroupCode), out p)) return null;
            return p > 0m ? (decimal?)p : null;
        }
    }
}
