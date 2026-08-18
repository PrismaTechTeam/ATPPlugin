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
    public static class ScpRentalGroupPrice
    {
        /// <summary>Which group a machine's rental falls in, under the contract's rental line mode.
        /// "Merge by model" gives every model its own price; a merge that ignores model has one
        /// price for the whole contract, so the key is ''.</summary>
        public static string GroupKeyFor(char rentalMode, string modelCode)
        {
            if (rentalMode == ScpBillingFormat.LINE_SAME_MODEL) return (modelCode ?? "").Trim();
            return "";
        }

        /// <summary>Prices for one contract, keyed by ModelCode ('' = the whole contract).
        /// Never throws: an older book without the table simply has no overrides.</summary>
        public static Dictionary<string, decimal> LoadForContract(DBSetting db, long contractKey)
        {
            Dictionary<string, decimal> map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (db == null || contractKey <= 0) return map;
            try
            {
                DataTable t = db.GetDataTable(
                    "SELECT ModelCode, UnitPrice FROM dbo.zSCP2_ContractRentalPrice " +
                    "WHERE ContractKey = " + contractKey, false);
                foreach (DataRow r in t.Rows)
                    map[Convert.ToString(r["ModelCode"]).Trim()] =
                        r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
            }
            catch { }
            return map;
        }

        /// <summary>Prices for many contracts in one query -- the billing run's form.
        /// Outer key = ContractKey, inner = ModelCode ('' = the whole contract).</summary>
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
                    "SELECT ContractKey, ModelCode, UnitPrice FROM dbo.zSCP2_ContractRentalPrice " +
                    "WHERE ContractKey IN (" + inList + ")", false);
                foreach (DataRow r in t.Rows)
                {
                    long ck = Convert.ToInt64(r["ContractKey"]);
                    Dictionary<string, decimal> inner;
                    if (!map.TryGetValue(ck, out inner))
                    {
                        inner = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        map[ck] = inner;
                    }
                    inner[Convert.ToString(r["ModelCode"]).Trim()] =
                        r["UnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitPrice"]);
                }
            }
            catch { }   // older book without the table -> nothing is overridden
            return map;
        }

        /// <summary>The price this machine's rental should bill at, or null when the group has not
        /// been priced. A zero price is NOT an override -- a group priced at nothing is a group
        /// nobody has filled in yet, and the machines keep their own rates.</summary>
        public static decimal? PriceFor(Dictionary<long, Dictionary<string, decimal>> map,
            long contractKey, char rentalMode, string modelCode)
        {
            if (map == null) return null;
            if (rentalMode == ScpBillingFormat.LINE_PER_MACHINE) return null;   // no group, no group price
            Dictionary<string, decimal> inner;
            if (!map.TryGetValue(contractKey, out inner)) return null;
            decimal p;
            if (!inner.TryGetValue(GroupKeyFor(rentalMode, modelCode), out p)) return null;
            return p > 0m ? (decimal?)p : null;
        }
    }
}
