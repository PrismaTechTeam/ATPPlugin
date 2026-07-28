using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>One composable rule line of a strategy (a row of zSCP2_StrategyRule). A strategy is a
    /// header plus a list of these — the "builder". Each line is ONE primitive (Kind) applied to a
    /// Scope of meters, with its own parameters; a strategy may mix kinds and repeat a kind.</summary>
    public class StrategyRule
    {
        public int Seq;
        public string Kind = "";          // WAIVE-TARGET / RENTAL-FREE-N / COMMIT-MIN / FOC-REBATE / INITIAL-METER / LIMIT
        public string Scope = "";         // "" = all meters / "BK" / "CL" = usage role / "RENTAL" = flat/rental meters
        public readonly List<long> ServiceItemKeys = new List<long>();   // empty = all service items; else the bound zSCP2_Item.ItemKey set
        public decimal TargetAmount;      // WAIVE-TARGET
        public decimal PartialPct;        // WAIVE-TARGET: reach X% of target -> waive X% of rental (0 = all-or-nothing)
        public int FreeMonths;            // RENTAL-FREE-N
        public decimal CommitAmount;      // COMMIT-MIN
        public decimal FocCopies;         // FOC-REBATE
        public decimal RebatePct;         // FOC-REBATE
        public bool NetBilling;           // FOC-REBATE: bill NET=(usage-FOC)x(1-rebate%) instead of raw
        public char LimitScope = 'S';     // LIMIT: S single machine / G pooled group (.C convention)
        public decimal LimitQty;          // LIMIT
    }

    /// <summary>One strategy = a header (Code/Description) + a freely-composed list of rule lines.
    /// Attached to a contract by soft code (zSCP2_Contract.StrategyCode, no FK).</summary>
    public class StrategyDef
    {
        public string Code = "";
        public string Description = "";
        public readonly List<StrategyRule> Rules = new List<StrategyRule>();

        public StrategyRule FirstRuleOfKind(string kind)
        {
            foreach (StrategyRule r in Rules) if (r.Kind == kind) return r;
            return null;
        }
        public IEnumerable<StrategyRule> RulesOfKind(string kind)
        {
            foreach (StrategyRule r in Rules) if (r.Kind == kind) yield return r;
        }
        public bool HasKind(string kind) { return FirstRuleOfKind(kind) != null; }
    }

    /// <summary>
    /// Strategy loading + rental-period helpers shared by the contract form, Rental Maintenance and
    /// the Meter Reading billing pipeline.
    /// </summary>
    public static class ScpStrategy
    {
        public const string TYPE_WAIVE_TARGET = "WAIVE-TARGET";
        public const string TYPE_RENTAL_FREE_N = "RENTAL-FREE-N";
        public const string TYPE_COMMIT_MIN = "COMMIT-MIN";
        public const string TYPE_FOC_REBATE = "FOC-REBATE";
        public const string TYPE_INITIAL_METER = "INITIAL-METER";
        public const string TYPE_LIMIT = "LIMIT";

        /// <summary>Effective strategy (the contract's OWN rule lines) per contract for the given keys.
        /// Reads zSCP2_ContractStrategyRule — the per-contract copy, edited on the contract, decoupled from
        /// the master template once seeded. Contracts with no rules simply have no entry.
        /// THROWS on DB failure — swallowing here made billing silently run WITHOUT the strategy
        /// (a wrong invoice with no error); callers surface the failure and abort instead.</summary>
        public static Dictionary<long, StrategyDef> LoadForContracts(DBSetting db, IEnumerable<long> contractKeys)
        {
            Dictionary<long, StrategyDef> map = new Dictionary<long, StrategyDef>();
            List<string> keys = new List<string>();
            foreach (long k in contractKeys) if (k > 0 && !keys.Contains(k.ToString())) keys.Add(k.ToString());
            if (keys.Count == 0) return map;
            DataTable dt = db.GetDataTable(
                "SELECT ContractKey, Seq, RuleKind, Scope, ServiceItemKeys, TargetAmount, PartialPct, FreeMonths, " +
                "CommitAmount, FocCopies, RebatePct, NetBilling, LimitScope, LimitQty, SeededFromCode " +
                "FROM dbo.zSCP2_ContractStrategyRule WHERE ContractKey IN (" + string.Join(",", keys.ToArray()) + ") " +
                "ORDER BY ContractKey, Seq", false);
            foreach (DataRow r in dt.Rows)
            {
                long ck = Convert.ToInt64(r["ContractKey"]);
                StrategyDef d;
                if (!map.TryGetValue(ck, out d))
                {
                    d = new StrategyDef();
                    d.Code = Convert.ToString(r["SeededFromCode"]);
                    map[ck] = d;
                }
                d.Rules.Add(ReadRule(r));
            }
            return map;
        }

        /// <summary>The contract's own strategy rule lines (for the builder).
        /// THROWS on DB failure — returning an empty list on error let the Strategy tab render empty
        /// and the next Save delete-reinsert WIPE the contract's real rules. Callers catch, warn and
        /// lock the editor instead.</summary>
        public static List<StrategyRule> LoadContractRules(DBSetting db, long contractKey)
        {
            List<StrategyRule> list = new List<StrategyRule>();
            if (contractKey <= 0) return list;
            DataTable dt = db.GetDataTable(
                "SELECT Seq, RuleKind, Scope, ServiceItemKeys, TargetAmount, PartialPct, FreeMonths, CommitAmount, " +
                "FocCopies, RebatePct, NetBilling, LimitScope, LimitQty " +
                "FROM dbo.zSCP2_ContractStrategyRule WHERE ContractKey = " + contractKey + " ORDER BY Seq", false);
            foreach (DataRow r in dt.Rows) list.Add(ReadRule(r));
            return list;
        }

        /// <summary>Loads a single strategy (with rules) by code, or null if not found / inactive.
        /// THROWS on DB failure (a DB error used to masquerade as "no rules to copy").</summary>
        public static StrategyDef LoadByCode(DBSetting db, string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            DataTable hd = db.GetDataTable(
                "SELECT StrategyKey, StrategyCode, [Description] FROM dbo.zSCP2_Strategy " +
                "WHERE StrategyCode = N'" + code.Replace("'", "''") + "' AND Inactive = 'N'", false);
            if (hd.Rows.Count == 0) return null;
            long sk = Convert.ToInt64(hd.Rows[0]["StrategyKey"]);
            StrategyDef d = new StrategyDef();
            d.Code = Convert.ToString(hd.Rows[0]["StrategyCode"]);
            d.Description = Convert.ToString(hd.Rows[0]["Description"]);
            DataTable rl = db.GetDataTable(
                "SELECT StrategyKey, Seq, RuleKind, Scope, TargetAmount, PartialPct, FreeMonths, " +
                "CommitAmount, FocCopies, RebatePct, NetBilling, LimitScope, LimitQty " +
                "FROM dbo.zSCP2_StrategyRule WHERE StrategyKey = " + sk + " ORDER BY Seq", false);
            foreach (DataRow r in rl.Rows) d.Rules.Add(ReadRule(r));
            return d;
        }

        /// <summary>Human-readable one-line-per-rule serialization for the per-invoice contract
        /// snapshot (zSCP2_ContractSnapshot) — proves what the deal was when the invoice was made.</summary>
        public static string SerializeRules(StrategyDef def)
        {
            if (def == null || def.Rules.Count == 0) return "(no strategy rules)";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (StrategyRule x in def.Rules)
            {
                sb.Append("Rule ").Append(x.Seq).Append(": ").Append(x.Kind);
                if (!string.IsNullOrEmpty(x.Scope)) sb.Append("  scope=").Append(x.Scope);
                sb.Append("  items=").Append(x.ServiceItemKeys.Count == 0 ? "ALL" : JoinItemKeys(x.ServiceItemKeys));
                if (x.Kind == TYPE_WAIVE_TARGET)
                    sb.Append("  target=").Append(x.TargetAmount.ToString("0.00")).Append("  partial%=").Append(x.PartialPct.ToString("0.##"));
                else if (x.Kind == TYPE_RENTAL_FREE_N) sb.Append("  freeMonths=").Append(x.FreeMonths);
                else if (x.Kind == TYPE_COMMIT_MIN) sb.Append("  committed=").Append(x.CommitAmount.ToString("0.00"));
                else if (x.Kind == TYPE_LIMIT) sb.Append("  limitQty=").Append(x.LimitQty.ToString("0.##"));
                else if (x.Kind == TYPE_FOC_REBATE)
                    sb.Append("  foc=").Append(x.FocCopies.ToString("0.##")).Append("  rebate%=").Append(x.RebatePct.ToString("0.##"));
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd();
        }

        private static StrategyRule ReadRule(DataRow r)
        {
            StrategyRule x = new StrategyRule();
            x.Seq = r["Seq"] == DBNull.Value ? 0 : Convert.ToInt32(r["Seq"]);
            x.Kind = Convert.ToString(r["RuleKind"]);
            x.Scope = Convert.ToString(r["Scope"]);
            if (r.Table.Columns.Contains("ServiceItemKeys") && r["ServiceItemKeys"] != DBNull.Value)
                ParseItemKeys(Convert.ToString(r["ServiceItemKeys"]), x.ServiceItemKeys);
            x.TargetAmount = AsDec(r["TargetAmount"]);
            x.PartialPct = AsDec(r["PartialPct"]);
            x.FreeMonths = r["FreeMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["FreeMonths"]);
            x.CommitAmount = AsDec(r["CommitAmount"]);
            x.FocCopies = AsDec(r["FocCopies"]);
            x.RebatePct = AsDec(r["RebatePct"]);
            x.NetBilling = Convert.ToString(r["NetBilling"]) == "Y";
            x.LimitScope = Convert.ToString(r["LimitScope"]) == "G" ? 'G' : 'S';
            x.LimitQty = AsDec(r["LimitQty"]);
            return x;
        }

        /// <summary>The rental period number n for the billing period (year, month): months between the
        /// rental start and the period + 1; prepayment basis ('P') counts one period ahead.</summary>
        public static int RentalPeriodN(DateTime rentalStart, char basis, int periodYear, int periodMonth)
        {
            int n = (periodYear - rentalStart.Year) * 12 + (periodMonth - rentalStart.Month) + 1;
            if (basis == 'P' || basis == 'p') n += 1;
            return n;
        }

        // Blank period slots seen in the master's rental meter type names: "( /60)", "(/12)",
        // "(  /60)", "XX/202X", "MM/YYYY" style tokens.
        private static readonly Regex SlotParen = new Regex(@"\(\s*(XX|xx)?\s*/\s*(\d+)\s*\)", RegexOptions.Compiled);
        private static readonly Regex SlotXx = new Regex(@"\b(XX|xx)\s*/\s*20XX\b|\bMM/YYYY\b|\bXX/202X\b", RegexOptions.Compiled);

        /// <summary>Injects "n/N" into a rental meter description: replaces the first blank period
        /// slot ("( /60)" -> "(3/60)"; "XX/202X" -> "3/12"); appends " (n/N)" when no slot exists.
        /// Months &lt;= 0 returns the name unchanged (open-ended rental).</summary>
        public static string ComposeRentalPeriodText(string meterTypeName, DateTime? rentalStart,
            int rentalMonths, char basis, int periodYear, int periodMonth)
        {
            string name = meterTypeName ?? "";
            if (!rentalStart.HasValue || rentalMonths <= 0) return name;
            int n = RentalPeriodN(rentalStart.Value, basis, periodYear, periodMonth);
            string frac = n + "/" + rentalMonths;

            Match m = SlotParen.Match(name);
            if (m.Success) return name.Substring(0, m.Index) + "(" + frac + ")" + name.Substring(m.Index + m.Length);
            m = SlotXx.Match(name);
            if (m.Success) return name.Substring(0, m.Index) + frac + name.Substring(m.Index + m.Length);
            return name + " (" + frac + ")";
        }

        private static decimal AsDec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }

        /// <summary>True if a (flat) meter type code is a RENTAL — not just codes that START with "RA"
        /// but the real-world convention where the rental code is prefixed, e.g. "01.RA.2JC10897",
        /// "01.RA-1 UNIT", "01.1 UNIT RENTAL". Mirrors the v1.5.0 flat-mark pattern. Callers should also
        /// require IsFlatCharge='Y' so committed-MIN flat meters are excluded.</summary>
        public static bool IsRentalMeterCode(string meterTypeCode)
        {
            string c = (meterTypeCode ?? "").Trim().ToUpperInvariant();
            if (c.Length == 0) return false;
            return c.StartsWith("RA") || c.Contains(".RA") || c.Contains("-RA") || c.Contains(" RA") || c.Contains("RENTAL");
        }

        /// <summary>True if a (flat) meter type code is a COMMITTED-MINIMUM meter — the master convention
        /// puts the minimum committed print charge on its own meter, e.g. "MIN 1764-12MTH" (rate 0, minimum
        /// = the committed amount). Such a meter tops the item's print charges up to the committed minimum,
        /// rather than billing the full minimum every month. Callers also require IsFlatCharge='Y'.</summary>
        public static bool IsCommittedMinMeterCode(string meterTypeCode)
        {
            string c = (meterTypeCode ?? "").Trim().ToUpperInvariant();
            return c.StartsWith("MIN");
        }

        /// <summary>Parse a comma-separated ItemKey list into the target list (cleared first).</summary>
        public static void ParseItemKeys(string csv, List<long> into)
        {
            into.Clear();
            if (string.IsNullOrEmpty(csv)) return;
            foreach (string part in csv.Split(','))
            {
                long k;
                string p = part.Trim();
                if (p.Length > 0 && long.TryParse(p, out k) && k > 0 && !into.Contains(k)) into.Add(k);
            }
        }

        /// <summary>Serialise an ItemKey list to a comma-separated string ('' = all items).</summary>
        public static string JoinItemKeys(IEnumerable<long> keys)
        {
            if (keys == null) return "";
            List<string> parts = new List<string>();
            foreach (long k in keys) if (k > 0) parts.Add(k.ToString());
            return string.Join(",", parts.ToArray());
        }
    }
}
