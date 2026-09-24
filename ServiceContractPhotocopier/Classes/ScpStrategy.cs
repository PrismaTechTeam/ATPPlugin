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

        /// <summary>The five meter types the new way needs, seeded by
        /// 04_Seed_zSCP_MeterType_Standard.sql. The old book invented a meter type per machine and per
        /// rate because Master Accounting had nowhere else to put them; here the rate lives on the
        /// machine's meter and the wording comes from the billing format, so a type only says WHICH
        /// counter this is, and COMMIT / WAIVE only say which KIND of deal this is -- the amount and
        /// the terms are on the machine's meter row. The machine form's ticks look these up by code.</summary>
        public const string METER_TYPE_RENTAL = "RENTAL";
        public const string METER_TYPE_BK = "BK";
        public const string METER_TYPE_CL = "CL";
        public const string METER_TYPE_COMMIT = "COMMIT";
        public const string METER_TYPE_WAIVE = "WAIVE";

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

        /// <summary>How many months a term runs, counting both ends: 01/08/2026 to 31/07/2029 is 36.
        /// 0 when either date is missing or the end falls before the start (an open-ended deal).</summary>
        public static int TermMonths(object start, object end)
        {
            if (start == null || start == DBNull.Value || end == null || end == DBNull.Value) return 0;
            DateTime s = Convert.ToDateTime(start);
            DateTime e = Convert.ToDateTime(end);
            int n = (e.Year - s.Year) * 12 + (e.Month - s.Month) + 1;
            return n > 0 ? n : 0;
        }

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

        /// <summary>
        /// Feedback ATP-10: the counter a rental billed IN ADVANCE prints -- which of its months the
        /// bill pays for, and which calendar months those are: "(2/36) NOV 2026". A machine that joined
        /// after the bill that should have carried its first month pays two on its first bill:
        /// "(1-2/36) DEC 2026 - JAN 2027". An open-ended rental has no N and prints only the month.
        /// </summary>
        public static string PrepaidRentalCounter(DateTime rentalStart, int rentalMonths, int fromN, int toN)
        {
            if (fromN < 1) fromN = 1;
            if (toN < fromN) toN = fromN;
            // A term extended past the rental's N keeps billing, and reads N/N, as it does with the
            // month's copies; the calendar month named is always the true one.
            int a = rentalMonths > 0 && fromN > rentalMonths ? rentalMonths : fromN;
            int b = rentalMonths > 0 && toN > rentalMonths ? rentalMonths : toN;
            string frac = a == b ? a.ToString(System.Globalization.CultureInfo.InvariantCulture) : a + "-" + b;
            string first = RentalMonthLabel(rentalStart, fromN);
            string months = fromN == toN ? first : first + " - " + RentalMonthLabel(rentalStart, toN);
            return rentalMonths > 0 ? "(" + frac + "/" + rentalMonths + ") " + months : months;
        }

        /// <summary>The calendar month of rental month n: month 1 is the rental's start month.</summary>
        public static string RentalMonthLabel(DateTime rentalStart, int n)
        {
            DateTime m = new DateTime(rentalStart.Year, rentalStart.Month, 1).AddMonths(n - 1);
            return m.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
        }

        /// <summary>The meter type's name with the prepaid counter in its blank slot, the way
        /// <see cref="ComposeRentalPeriodText"/> puts "(n/N)" there: "MONTHLY RENTAL ( /60)" becomes
        /// "MONTHLY RENTAL (2/60) NOV 2026".</summary>
        public static string ComposePrepaidRentalText(string meterTypeName, DateTime rentalStart,
            int rentalMonths, int fromN, int toN)
        {
            string name = meterTypeName ?? "";
            string counter = PrepaidRentalCounter(rentalStart, rentalMonths, fromN, toN);
            Match m = SlotParen.Match(name);
            if (m.Success) return name.Substring(0, m.Index) + counter + name.Substring(m.Index + m.Length);
            m = SlotXx.Match(name);
            if (m.Success) return name.Substring(0, m.Index) + counter + name.Substring(m.Index + m.Length);
            return name + " " + counter;
        }

        /// <summary><see cref="IsRentalRole"/> as SQL over a zSCP2_ItemMeter alias -- for the queries
        /// that have to ask it of every meter at once.</summary>
        public static string RentalRoleSql(string alias)
        {
            string role = "UPPER(LTRIM(RTRIM(ISNULL(" + alias + ".MeterRole,''))))";
            string code = "UPPER(LTRIM(RTRIM(ISNULL(" + alias + ".MeterTypeCode,''))))";
            return "(" + role + " = 'RENTAL' OR (" + role + " IN ('','NA') AND " + code + " <> '' AND (" +
                   code + " LIKE 'RA%' OR " + code + " LIKE '%.RA%' OR " + code + " LIKE '%-RA%' OR " +
                   code + " LIKE '% RA%' OR " + code + " LIKE '%RENTAL%')))";
        }

        private static decimal AsDec(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }

        /// <summary>Is this meter a RENTAL? The role answers it; the code convention is the fallback
        /// for rows old enough to predate the role column.</summary>
        /// <remarks>
        /// Asked in one place because it used to be asked in several and they disagreed. The engine
        /// tested the CODE alone, so a flat type whose code is not rental-shaped but whose role IS
        /// RENTAL — the customer's own book has one, `~RA-SV-R6` — was priced as a rental by every
        /// screen and then skipped by the group price, the free-rental countdown, the waive target,
        /// the rental-separate invoice and the (n/N) counter.
        ///
        /// <para>The code half is gated on an unset role so a row explicitly marked COMMIT or WAIVE is
        /// never mistaken for rent just because its type happens to be typed "RENTAL".</para>
        /// </remarks>
        public static bool IsRentalRole(string meterRole, string meterTypeCode)
        {
            string r = (meterRole ?? "").Trim().ToUpperInvariant();
            if (r == "RENTAL") return true;
            if (r.Length > 0 && r != "NA") return false;
            return IsRentalMeterCode(meterTypeCode);
        }

        /// <summary>Is this meter a rental WAIVE — the contra that gives the rent back? Role first,
        /// the meter type's own flag as the OR that keeps the legacy "(W)" family working.</summary>
        public static bool IsWaiveRole(string meterRole, bool typeIsRentalWaive)
        {
            return (meterRole ?? "").Trim().ToUpperInvariant() == "WAIVE" || typeIsRentalWaive;
        }

        /// <summary>Is this meter a committed minimum? Role first, the legacy "MIN..." type name as
        /// the fallback — and it needs an amount, because a minimum of nothing is not one.</summary>
        public static bool IsCommittedMinRole(string meterRole, string meterTypeCode, decimal minimumCharges)
        {
            if (minimumCharges <= 0m) return false;
            if ((meterRole ?? "").Trim().ToUpperInvariant() == "COMMIT") return true;
            return IsCommittedMinMeterCode(meterTypeCode);
        }

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

        /// <summary>Normalizes a Bill Group code (#6 split billing): trim, uppercase, A-Z/0-9/dash
        /// only, max 20 chars. The charset matters — the code is embedded in the invoice job key
        /// ("C{ck}_G{code}[_R]") where underscore is the separator, so a code like "1_R" must never
        /// be able to forge the rental-separate suffix.</summary>
        public static string SanitizeBillGroup(string raw)
        {
            if (raw == null) return "";
            string s = raw.Trim().ToUpperInvariant();
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-') sb.Append(c);
            string r = sb.ToString();
            return r.Length > 20 ? r.Substring(0, 20) : r;
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
