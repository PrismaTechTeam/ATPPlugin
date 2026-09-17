using System;
using System.Collections.Generic;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Whether a rental waive fires this month, and for how much.
    ///
    /// <para>This used to live inside the Meter Reading form, which meant only the Generate button
    /// could answer the question. The Sample Invoice and the shape harness had to guess, and a
    /// preview that guesses is worse than no preview — it showed a credit the invoice then did not
    /// give. It is arithmetic on a list of lines; nothing in it needs a screen.</para>
    /// </summary>
    public static class ScpWaiveMeter
    {
        // ═══ WAIVE METERS (master-style contra, engine-decided) ═══
        // A Rental-Waive meter (its own item code, e.g. RA-MONTH(W)-13MTH) bills NEGATIVE when its
        // per-meter Waive Configuration says so — replacing the master's manual monthly decision:
        //   window : FirstN > 0  -> fires only in months 1..N (anchor RentalStartDate, else the
        //            machine's effective service start)
        //   target : Target > 0  -> the MACHINE's scoped BK/CL charges must reach it; the partial %
        //            band gives a partial contra (amount x partial%)
        //   neither             -> ALWAYS fires (legacy master behavior)
        // Fired  -> Charge = -amount (AlwaysBill: the contra PRINTS with its note).
        // Silent -> Charge = 0, and the row still prints with the reason it did not fire. A deal the
        //           customer signed was measured this month; "nothing came off, and here is why" is
        //           an answer, a row that quietly vanishes is not -- and the committed minimum has
        //           printed its 0.00 the same way since it was written.
        public static void Apply(List<MeterBillLine> lines, int genYear, int genMonth)
        {
            // Every charge line in the RUN, not in one invoice: a waive on a machine whose rental was
            // split onto its own invoice is still decided by copies billed on the other one.
            List<MeterBillLine> usage = new List<MeterBillLine>();
            foreach (MeterBillLine l0 in lines)
                if (!l0.IsFlat) usage.Add(l0);

            foreach (MeterBillLine l in lines)
            {
                if (!l.IsWaiveMeter) continue;
                // The waive amount is stored NEGATIVE on the meter row (master convention, user
                // rule 2026-07-27) — the engine is sign-agnostic and the fired contra always
                // bills MINUS the magnitude, whatever sign was keyed.
                decimal amount = l.MinCharges != 0m ? Math.Abs(l.MinCharges) : Math.Abs(l.Rate);
                if (amount <= 0m) { l.Charge = 0m; l.UseMin = false; continue; }

                // SEQUENTIAL semantics (user decision): months 1..N = free window, ALWAYS waived;
                // AFTER the window the target condition (when set) takes over. Window-only = waive
                // stops after N months; target-only = by usage from day one; neither = always.
                bool fire = true;
                bool inWindow = false;
                string note = "RENTAL WAIVE";
                if (l.WaiveFirstNMonths > 0)
                {
                    DateTime? anchor = l.RentalStartDate ?? l.EffStartDate;
                    if (!anchor.HasValue) { l.Charge = 0m; l.UseMin = false; continue; }   // no date to count from
                    int monthNo = (genYear * 12 + genMonth) - (anchor.Value.Year * 12 + anchor.Value.Month) + 1;
                    if (monthNo >= 1 && monthNo <= l.WaiveFirstNMonths)
                    {
                        inWindow = true;
                        // FOC, not a waive: in the free window nothing is charged in the first
                        // place. The note is what the customer reads under the zero.
                        note = "FOC - free month " + monthNo + " of " + l.WaiveFirstNMonths;
                    }
                    else if (l.WaiveTargetAmount <= 0m)
                        fire = false;   // window over and no usage condition -> the waive retires
                }
                decimal waiveAmt = amount;
                string missed = l.WaiveFirstNMonths > 0
                    ? "RENTAL WAIVE " + amount.ToString("n2") + " - the free months are over"
                    : "";
                if (fire && !inWindow && l.WaiveTargetAmount > 0m)
                {
                    decimal total = 0m;
                    foreach (MeterBillLine u in usage)
                    {
                        // Whose copies count towards the target. Per MACHINE normally; the fleet
                        // machine counts the WHOLE contract; and a waive scoped 'G' counts the
                        // machines that share its rental line -- because the deal was struck over
                        // that line, not over one of the machines on it. Same scope column the
                        // committed minimum uses, for the same question.
                        if (!WaiveCounts(l, u)) continue;
                        if (u.ColorLabel != "Black" && u.ColorLabel != "Colour") continue;
                        if (l.WaiveScope == "BK" && u.ColorLabel != "Black") continue;
                        if (l.WaiveScope == "CL" && u.ColorLabel != "Colour") continue;
                        total += u.Charge;
                    }
                    if (total >= l.WaiveTargetAmount)
                        note += " - charges " + total.ToString("0.00") + " >= target " + l.WaiveTargetAmount.ToString("0.00");
                    // Partial band in RM (demo 28/07 #24): charges reach RM X -> waive RM Y
                    // (was a % of the target — the customer agrees deals in RM, not %).
                    else if (l.WaivePartialThreshold > 0m && l.WaivePartialAmount > 0m
                             && total >= l.WaivePartialThreshold)
                    {
                        waiveAmt = Math.Min(l.WaivePartialAmount, amount);
                        note += " - PARTIAL: charges " + total.ToString("0.00") +
                                " >= " + l.WaivePartialThreshold.ToString("0.00") +
                                " -> waive " + waiveAmt.ToString("0.00");
                    }
                    else
                    {
                        fire = false;
                        missed = "RENTAL WAIVE " + amount.ToString("n2") + " - charges " +
                                 total.ToString("0.00") + " short of target " +
                                 l.WaiveTargetAmount.ToString("0.00");
                    }
                }
                if (fire && inWindow)
                {
                    // FREE MONTHS ARE NOT A WAIVE. The deal is "you do not pay rent for the first N
                    // months", so the rental line itself is free -- one line, nothing to pay. Billing
                    // the rent and crediting it back on a second line is a different deal that happens
                    // to reach the same total, and HOSPITAL PASIR GUDANG's own invoice shows which one
                    // it is: three lines reading FOC, no contra anywhere.
                    //
                    // The meter row that carries the deal has nothing left to print, so it stays off.
                    l.Charge = 0m;
                    l.UseMin = false;
                    l.AlwaysBill = false;
                    l.Suppressed = true;
                    l.StrategyNote = note;
                    foreach (MeterBillLine r in lines)
                    {
                        if (!r.IsRental || r.IsWaiveMeter || !WaiveCounts(l, r)) continue;
                        r.Charge = 0m;
                        r.UseMin = false;
                        r.FreeThisMonth = true;
                        r.AlwaysBill = true;
                        r.StrategyNote = note;
                    }
                    continue;
                }
                if (fire)
                {
                    l.Charge = -Math.Round(waiveAmt, 2);   // the CONTRA line (negative)
                    l.UseMin = false;
                    l.AlwaysBill = true;                              // negative lines must print
                    l.StrategyNote = note;
                }
                else
                {
                    // Not this month. The row keeps its place and says so -- an empty 0.00 line
                    // labelled RENTAL WAIVE, with no sentence under it, is the one outcome that
                    // tells the reader nothing at all.
                    l.Charge = 0m;
                    l.UseMin = false;
                    l.AlwaysBill = true;
                    l.StrategyNote = missed;
                }
            }
        }

        private static bool WaiveCounts(MeterBillLine waive, MeterBillLine usageLine)
        {
            if (waive.IsGroupItem) return usageLine.ContractKey == waive.ContractKey;

            string grp = (waive.MergeGroupCode ?? "").Trim();
            bool byGroup = grp.Length > 0 &&
                string.Equals((waive.CommitScope ?? "").Trim(), "G", StringComparison.OrdinalIgnoreCase);
            if (byGroup)
            {
                return usageLine.ContractKey == waive.ContractKey &&
                       string.Equals((usageLine.MergeGroupCode ?? "").Trim(), grp,
                                     StringComparison.OrdinalIgnoreCase);
            }
            return usageLine.ItemKey == waive.ItemKey;
        }
    }
}
