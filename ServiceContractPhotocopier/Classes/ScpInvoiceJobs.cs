using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Turns the rows to be billed into invoice jobs. This is where every figure of money on an
    /// invoice is decided, and it is the only place it is decided.
    ///
    /// <para>Group FOC pools, rental free months, waive targets, committed minimums, group tier
    /// pricing, the fold into printed lines -- all of it, once. It moved out of the Meter Reading
    /// Integration screen when inter-billing needed to bill a contract too: two screens, one set
    /// of answers. A second copy of this would drift from the first the day a rule changed, and
    /// the drift would show up as two different invoices for the same month.</para>
    ///
    /// <para>It shows nothing and asks nothing. Where it must refuse it returns null and says why
    /// in blockMessage, leaving the caller to put that in front of somebody. That is what lets
    /// more than one screen call it.</para>
    /// </summary>
    public static class ScpInvoiceJobs
    {
public static Dictionary<string, MeterInvoiceGenerator.InvoiceJob> Build(
            DBSetting db, DataTable allRows, List<DataRow> visibleRows,
            Dictionary<string, List<decimal[]>> ladders,
            int genYear, int genMonth, string grpMode,
            out int alreadyInvoiced, out Dictionary<long, string> runSnapshots,
            out string blockTitle, out string blockMessage)
        {
            Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs =
                new Dictionary<string, MeterInvoiceGenerator.InvoiceJob>();
            alreadyInvoiced = 0;
            runSnapshots = null;
            blockTitle = "";
            blockMessage = "";
            foreach (DataRow r in visibleRows)
            {
                if (!(r["Sel"] != DBNull.Value && Convert.ToBoolean(r["Sel"]))) continue;
                // Billing-period guard: this meter + period already has an invoice — never bill twice.
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) { alreadyInvoiced++; continue; }
                // Usage meters need a reading > 0 to bill; flat/rental meters bill on their flat amount
                // even with reading 0 (that is the whole point — no meter to read).
                bool rowFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                if (!rowFlat && Dec(r["CurrentReading"]) <= 0m) continue;
                // Invoice grouping (SETTING, 2026-08-04 — closes ISSUES.md D-2):
                //   FOLLOW  = the contract's own Billing Mode decides: G -> ONE invoice per CONTRACT
                //             ("Group Services into One Invoice"), S -> one per machine.
                //   DEBTOR  = legacy override: ONE invoice per CUSTOMER across all their contracts.
                //   MACHINE = force one invoice per machine.
                long contractKey = D64(r["ContractKey"]);
                long itemKey = D64(r["ItemKey"]);
                string mode;
                if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR) mode = "G";
                else if (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) mode = "S";
                else mode = S(r["BillingMode"]).Trim().ToUpperInvariant() == "S" ? "S" : "G";
                string groupKey = mode == "S"
                    ? ("C" + contractKey + "_I" + itemKey)
                    : (grpMode == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR
                        ? ("D" + S(r["DebtorCode"]))
                        : ("C" + contractKey));
                // Demo 28/07 #6: an explicit Bill Group on the machine OVERRIDES "one invoice for
                // everything" (and the debtor setting): same contract + same code = ONE invoice. It
                // does NOT override one invoice per machine -- per machine is the widest split, and a
                // bill group cannot put two machines back on one paper (user, 15/09). Bill Group splits
                // INVOICES only — never the deal scope (strategy passes stay contract-wide). Fleet group
                // machines (IsGroupItem) never carry a code -> legacy key, so their fleet-total MIN/WAIVE
                // lines stay on the invoice that has the prints.
                string billGroup = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]));
                bool rowIsGroupItem = r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
                if (rowIsGroupItem) billGroup = "";
                // Contract flag "Rental separate invoice": flat (rental/min) lines split into their own
                // job — one extra invoice per group ("Rental- [ref]") instead of riding the meter invoice.
                bool rentSep = r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]);
                // Only actual RENTAL meters split onto the rental-separate invoice — a committed-minimum
                // ("MIN ...") meter is a print charge and must stay on the meter invoice with BK/CL.
                //
                // A WAIVE goes with them. It is the contra that gives the rent back, and its meter
                // type is called WAIVE, so the rental test does not recognise it: HOSPITAL PASIR
                // GUDANG billed the rent of 1,500.00 on one invoice and credited it on the OTHER,
                // leaving a rental invoice that asked for money the customer had been promised free
                // and a meter invoice 1,500.00 light. Both totals were wrong; only their sum was right.
                bool rowIsWaive = r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]);
                bool rentalJob = rowFlat && rentSep &&
                                 (ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalMeterCode(S(r["MeterType"]))
                                  || rowIsWaive);
                string refNo = mode == "S" ? S(r["ServiceItemNo"]) : S(r["ContractNo"]);
                if (billGroup.Length > 0 && mode != "S") { groupKey = "C" + contractKey + "_G" + billGroup; refNo = S(r["ContractNo"]); }
                if (rentalJob) groupKey += "_R";   // rental suffix composes: 2 groups x rentSep = 4 jobs

                MeterBillLine ln = new MeterBillLine();
                ln.ItemKey = itemKey;
                ln.ContractKey = contractKey;
                ln.ItemMeterKey = D64(r["ItemMeterKey"]);
                ln.ContractNo = S(r["ContractNo"]);
                ln.ItemNo = S(r["ServiceItemNo"]);
                ln.DebtorCode = S(r["DebtorCode"]);
                ln.SerialNumber = S(r["SerialNo"]);
                ln.ItemName = S(r["ServiceItemNo"]);
                ln.ItemDesc = S(r["ItemDesc"]);
                ln.MeterTypeCode = S(r["MeterType"]);
                ln.MeterTypeName = S(r["MeterTypeName"]);
                ln.ACItemCode = S(r["ACItemCode"]);
                // Role-aware label: NA meters (scan/plotter/other usage) must NOT masquerade as "Black" —
                // that folded them into committed-min print sums and BK-scoped strategy passes.
                string roleUp = S(r["Role"]).Trim().ToUpperInvariant();
                ln.ColorLabel = roleUp == "CL" ? "Colour" : (roleUp == "BK" ? "Black" : "Usage");
                ln.Last = Dec(r["LastReading"]);
                ln.Current = Dec(r["CurrentReading"]);
                ln.Usage = Dec(r["MeterUsage"]);
                ln.Rate = Dec(r["UnitPrice"]);
                ln.MinCharges = Dec(r["MinCharges"]);
                ln.Foc = Dec(r["FOCQty"]);
                ln.RebatePct = Dec(r["RebatePct"]);
                ln.MultiPriceCode = S(r["MultiPriceCode"]);
                ln.FocResetCount = FocResetCountFor(r, genYear, genMonth);
                ln.IsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
                // Charge via the SAME engine the grid used, so the invoice matches the preview exactly
                // (NET: FOC copies + rebate deducted, multi-price tier, min floor). Sets BillCopies / EffUnitPrice.
                // Must be set BEFORE ComputeCharge — it decides how the rebate is taken and how the
                // cents are rounded.
                ln.NewMoneyRules = r["NewMoneyRules"] != DBNull.Value && Convert.ToBoolean(r["NewMoneyRules"]);
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, ladders);
                if (r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"])) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
                ln.IsRental = ln.IsFlat && ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalRole(
                    S(r["Role"]), S(r["MeterType"]));
                // The machine's DEFINED mode beats the live fetch status (deterministic numbering).
                string mmode = S(r["MachineMode"]).Trim().ToUpperInvariant();
                ln.MachineStatus = mmode.Length > 0 ? mmode : S(r["MachineStatus"]);
                ln.TrackingId = S(r["TrackingId"]).Trim();
                ln.IsGroupItem = r["IsGroupItem"] != DBNull.Value && Convert.ToBoolean(r["IsGroupItem"]);
                // Which printed line this machine belongs on, when its contract folds lines together.
                ln.ModelCode = S(r["ModelCode"]);
                ln.LineGroupCode = S(r["LineGroupCode"]);
                ln.ShowModel = r["ShowModel"] == DBNull.Value || Convert.ToBoolean(r["ShowModel"]);
                ln.ShowSerial = r["ShowSerial"] == DBNull.Value || Convert.ToBoolean(r["ShowSerial"]);
                ln.ShowUnits = r["ShowUnits"] == DBNull.Value || Convert.ToBoolean(r["ShowUnits"]);
                string mls = S(r["MachineLineShows"]).Trim();
                ln.MachineLineShows = mls.Length > 0 ? char.ToUpperInvariant(mls[0])
                    : ServiceContractPhotocopier.Classes.ScpBillingFormat.MACHINE_LINE_BOTH;
                ln.MergeGroupCode = S(r["MergeGroupCode"]);
                ln.MergeGroupCodeMeter = S(r["MergeGroupCodeMeter"]);
                ln.IsWaiveMeter = rowIsWaive;
                // Scope is shared: waive meters use it for the target sum, MIN meters for the
                // committed-minimum printed sum (BK only / CL only / both).
                string wsc = S(r["WaiveScope"]).Trim().ToUpperInvariant();
                ln.WaiveScope = wsc == "BK" || wsc == "CL" ? wsc : "BKCL";
                if (ln.IsWaiveMeter)
                {
                    ln.WaiveFirstNMonths = r["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["WaiveFirstNMonths"]);
                    ln.WaiveTargetAmount = Dec(r["WaiveTargetAmount"]);
                    ln.WaivePartialThreshold = Dec(r["WaivePartialThreshold"]);
                    ln.WaivePartialAmount = Dec(r["WaivePartialAmount"]);
                }
                // Committed-minimum meter: bill the TOP-UP to the committed amount over the print
                // charges it is measured against (computed in ApplyCommittedMin), and always show it
                // even at zero, so the customer sees the arithmetic.
                //
                // Recognised by its ROLE. It used to be recognised by its meter type being NAMED
                // "MIN something", which is why the old book has forty-odd of them -- one per amount.
                // The code prefix stays as an OR so every existing machine keeps billing exactly as
                // it did; without it, a type called COMMIT would have shown the committed-minimum UI
                // and then quietly billed the whole minimum every month as an ordinary flat meter.
                if (ln.IsFlat && ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinRole(
                        S(r["Role"]), ln.MeterTypeCode, ln.MinCharges))
                {
                    ln.IsCommittedMin = true;
                    ln.CommittedAmount = ln.MinCharges;
                    ln.AlwaysBill = true;
                }

                // Whose charges a term is measured over -- read for EVERY flat meter, not only the
                // committed minimum. A waive asks the same question ("print this much and the rent is
                // on us"), and reading the column only for one of them is how a line-scoped waive gets
                // stored, displayed, and then quietly measured per machine anyway.
                if (ln.IsFlat)
                {
                    ln.CommitScope = S(r["CommitScope"]).Trim().ToUpperInvariant();
                    if (ln.CommitScope != "G" && ln.CommitScope != "C") ln.CommitScope = "S";
                }
                ln.StrategyCode = S(r["StrategyCode"]);
                if (r["RentalStartDate"] != DBNull.Value) ln.RentalStartDate = Convert.ToDateTime(r["RentalStartDate"]);
                ln.RentalMonths = r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]);
                ln.RentalBasis = S(r["RentalBasis"]) == "P" ? 'P' : 'A';
                if (r["EffStart"] != DBNull.Value) ln.EffStartDate = Convert.ToDateTime(r["EffStart"]);
                if (S(r["EntrySource"]) == "RENTAL FREE") { ln.StrategyNote = "RENTAL FREE - FOC month"; ln.AlwaysBill = true; }
                if (r["LastReadDate"] != DBNull.Value) ln.LastDate = Convert.ToDateTime(r["LastReadDate"]);
                if (r["LastAuditDate"] != DBNull.Value) ln.AuditDate = Convert.ToDateTime(r["LastAuditDate"]);
                // #16: contract-cycle billing period for the invoice DISPLAY (flag on the contract).
                // Anchor day = the CONTRACT's service start day (contract-level on purpose: one
                // invoice shows ONE period even when items carry their own start-date overrides);
                // fallback billing day, then 1. The period is the cycle window that ENDS in the
                // billed month — the one this month's reading actually measured (user-verified):
                //   day-1 contract, July run  -> 01/07 - 31/07 (the billed month itself)
                //   day-25 contract, Aug run  -> 25/07 - 24/08
                if (r["PeriodByContract"] != DBNull.Value && Convert.ToBoolean(r["PeriodByContract"]))
                {
                    int anchorDay = r["ContractStart"] != DBNull.Value ? Convert.ToDateTime(r["ContractStart"]).Day
                        : (r["BillingDay"] != DBNull.Value && Convert.ToInt32(r["BillingDay"]) >= 1 ? Convert.ToInt32(r["BillingDay"]) : 1);
                    if (anchorDay <= 1)
                    {
                        ln.PeriodStart = new DateTime(genYear, genMonth, 1);
                        ln.PeriodEnd = new DateTime(genYear, genMonth, DateTime.DaysInMonth(genYear, genMonth));
                    }
                    else
                    {
                        int dimB = DateTime.DaysInMonth(genYear, genMonth);
                        ln.PeriodEnd = new DateTime(genYear, genMonth, (anchorDay - 1) > dimB ? dimB : (anchorDay - 1));
                        int py = genYear, pm = genMonth - 1;
                        if (pm < 1) { pm = 12; py--; }
                        int dimP = DateTime.DaysInMonth(py, pm);
                        ln.PeriodStart = new DateTime(py, pm, anchorDay > dimP ? dimP : anchorDay);
                    }
                }

                MeterInvoiceGenerator.InvoiceJob job;
                if (!jobs.TryGetValue(groupKey, out job))
                {
                    job = new MeterInvoiceGenerator.InvoiceJob();
                    job.DebtorCode = ln.DebtorCode;
                    job.RefDocNo = refNo;
                    // Invoice date = the BILLED PERIOD's billing day (row's effective day, clamped),
                    // so a May run dated 28/05 lands in the MR2605.* number series — never "today".
                    int effDay = r["BillingDay"] == DBNull.Value ? 28 : Convert.ToInt32(r["BillingDay"]);
                    int dim = DateTime.DaysInMonth(genYear, genMonth);
                    job.DocDate = new DateTime(genYear, genMonth, effDay > dim ? dim : (effDay < 1 ? 1 : effDay));
                    // Demo 28/07 #18: the rental-separate job gets its OWN invoice day when the
                    // contract sets one ("rental 是跟头标的,meter 是跟尾标的"): accrual rentals date
                    // in the billing month, prepayment rentals in the NEXT month (June run -> July 1).
                    if (rentalJob)
                    {
                        int rentDay = r.Table.Columns.Contains("RentalBillingDay") && r["RentalBillingDay"] != DBNull.Value
                            ? Convert.ToInt32(r["RentalBillingDay"]) : 0;
                        if (rentDay >= 1 && rentDay <= 28)
                        {
                            int ry = genYear, rm = genMonth;
                            if (S(r["RentalBasis"]) == "P") { rm++; if (rm > 12) { rm = 1; ry++; } }
                            int rdim = DateTime.DaysInMonth(ry, rm);
                            job.DocDate = new DateTime(ry, rm, rentDay > rdim ? rdim : rentDay);
                        }
                    }
                    // Legacy header text (verified against the customer's V8 meter invoices);
                    // rental-only invoices get their own header so the two are distinguishable.
                    job.Description = (rentalJob ? "Rental- [" : "Billing- [") + refNo
                        + (billGroup.Length > 0 ? " / " + billGroup : "") + "]";
                    job.Label = refNo + (billGroup.Length > 0 ? " / " + billGroup : "") + (rentalJob ? " (rental)" : "");
                    job.Lines = new List<MeterBillLine>();
                    jobs[groupKey] = job;
                }
                job.Lines.Add(ln);
            }

            // Strategy passes over the whole run (cross-machine — cannot live in the per-row grid preview).
            // ALL rule kinds act LIVE here — no "Apply to Meters" push step exists any more:
            //   1) GROUP FOC LIMIT   — pools a shared free-copy quota across the group's machines,
            //   2) RENTAL-FREE-N     — stateless: billing month number vs the rental's start month,
            //   3) WAIVE-TARGET      — waives rental when the (net-of-pool) usage charges reach the target,
            //   4) COMMITTED MIN     — MIN meters AND COMMIT-MIN rules top up the item's print charges.
            // A failure here ABORTS the run: silently billing WITHOUT the strategy would produce a wrong
            // invoice with no error — worse than no invoice.
            Dictionary<long, StrategyDef> runStrats;
            // runSnapshots is an out parameter of this method.
            try
            {
                HashSet<long> runCks = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb0 in jobs.Values)
                    foreach (MeterBillLine l0 in jb0.Lines)
                        if (l0.ContractKey > 0) runCks.Add(l0.ContractKey);
                runStrats = ServiceContractPhotocopier.Classes.ScpStrategy.LoadForContracts(db, runCks);
                runSnapshots = BuildContractSnapshots(runCks, runStrats, genYear, genMonth, allRows);
                // A group that agreed TIER pricing is priced over its copies added up, before
                // anything reads a charge -- the minimum measures charges and the waive is decided by
                // them, so both must see the price the group actually agreed.
                System.Collections.Generic.List<MeterBillLine> everyLineL =
                    new System.Collections.Generic.List<MeterBillLine>();
                foreach (MeterInvoiceGenerator.InvoiceJob jbl in jobs.Values)
                    foreach (MeterBillLine ll in jbl.Lines) everyLineL.Add(ll);
                System.Collections.Generic.Dictionary<long,
                    System.Collections.Generic.Dictionary<string,
                        ServiceContractPhotocopier.Classes.ScpLineTerms>> termsL =
                    new System.Collections.Generic.Dictionary<long,
                        System.Collections.Generic.Dictionary<string,
                            ServiceContractPhotocopier.Classes.ScpLineTerms>>();
                foreach (long ckl in runCks)
                    termsL[ckl] = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadTerms(db, ckl);
                ServiceContractPhotocopier.Classes.ScpGroupLadder.Apply(everyLineL, termsL, ladders);

                ApplyGroupLimit(jobs, runStrats, ladders);
                ApplyRentalFreeN(jobs, runStrats, genYear, genMonth);
                // The waive rules live in ScpWaiveMeter, not here: the preview and the shape harness
                // have to reach the same verdict this run does, and a rule kept inside a form is a rule
                // only the form can apply -- the preview showed a credit the invoice would not give.
                List<MeterBillLine> everyLineW = new List<MeterBillLine>();
                foreach (MeterInvoiceGenerator.InvoiceJob jbw in jobs.Values)
                    foreach (MeterBillLine lw in jbw.Lines) everyLineW.Add(lw);
                ServiceContractPhotocopier.Classes.ScpWaiveMeter.Apply(everyLineW, genYear, genMonth);
                // A free-months meter made its rental free instead of crediting it; its own row
                // has nothing to say and does not go on the paper.
                foreach (MeterInvoiceGenerator.InvoiceJob jbw2 in jobs.Values)
                    jbw2.Lines.RemoveAll(delegate(MeterBillLine lx) { return lx.Suppressed; });
                ApplyWaiveTarget(jobs, runStrats);
                // Last gate before the money is worked out. The contract screen checks this too, but
                // a clash can arrive from a migration, a direct edit, or a contract saved by an older
                // build -- and by the time it reaches here it is about to be billed to somebody.
                List<ServiceContractPhotocopier.Classes.MeterBillLine> everyLine =
                    new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb2 in jobs.Values)
                    foreach (MeterBillLine l2 in jb2.Lines) everyLine.Add(l2);
                List<string> twice =
                    ServiceContractPhotocopier.Classes.ScpCommittedMin.FindDoubleCounted(everyLine);
                if (twice.Count > 0)
                {
                    blockTitle = "Minimum counted twice";
                    blockMessage =
                        "These minimums are measured over the same charges, so a shortfall would be " +
                        "billed more than once:" + "\r\n" + "\r\n" +
                        string.Join("\r\n", twice.ToArray()) + "\r\n" + "\r\n" +
                        "Open the contract's Billing Setup and keep ONE of them." + "\r\n" +
                        "Nothing was generated.";
                    return null;
                }

                ApplyCommittedMin(jobs, runStrats);
            }
            catch (Exception stratEx)
            {
                blockTitle = "Generate Invoice";
                blockMessage = "Strategy evaluation failed — generation ABORTED, no invoice was created.\r\n\r\n" +
                    stratEx.Message + "\r\n\r\nFix the cause (or clear the contract's strategy rules) and Generate again.";
                return null;
            }

            // Offline readings reference their monthly report by TrackingId — when any line in a job
            // has one, the invoice's Reference No becomes the job's DISTINCT ids, comma-joined in line
            // order ("MR-260724-041, MR-260724-052"). IV.RefDocNo is nvarchar(30): only as many WHOLE
            // ids as fit are joined — an id is never cut in half. No ids -> the usual CSSI/contract ref.
            // Demo 28/07 #4: only BK/CL rows are staged with a TrackingId, so a rental-separate
            // ("_R") or flat-only job carried none even when the SAME machine's offline report id
            // was sitting on its usage rows — every line falls back to its MACHINE's id.
            Dictionary<long, string> tidByItem = new Dictionary<long, string>();
            foreach (DataRow tidRow in allRows.Rows)
            {
                string tidCell = S(tidRow["TrackingId"]).Trim();
                if (tidCell.Length == 0) continue;
                long tidItem = D64(tidRow["ItemKey"]);
                if (!tidByItem.ContainsKey(tidItem)) tidByItem[tidItem] = tidCell;
            }
            foreach (MeterInvoiceGenerator.InvoiceJob jbT in jobs.Values)
            {
                List<string> tids = new List<string>();
                foreach (MeterBillLine lT in jbT.Lines)
                {
                    string tid = (lT.TrackingId ?? "").Trim();
                    if (tid.Length == 0) tidByItem.TryGetValue(lT.ItemKey, out tid);
                    tid = (tid ?? "").Trim();
                    if (tid.Length > 0 && !tids.Contains(tid)) tids.Add(tid);
                }
                if (tids.Count == 0) continue;
                string joined = "";
                foreach (string tid in tids)
                {
                    string next = joined.Length == 0 ? tid : joined + ", " + tid;
                    if (next.Length > 30) break;
                    joined = next;
                }
                if (joined.Length > 0) jbT.RefDocNo = joined;
            }

            // Progress dialog shows WHICH MACHINE(S) are being billed — service item nos, not the contract.
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
            {
                List<string> nos = new List<string>();
                foreach (MeterBillLine l2 in jb.Lines)
                    if (!string.IsNullOrEmpty(l2.ItemNo) && !nos.Contains(l2.ItemNo)) nos.Add(l2.ItemNo);
                string lbl = string.Join(", ", nos.ToArray());
                if (lbl.Length > 70) lbl = lbl.Substring(0, 67) + "…(" + nos.Count + " items)";
                if (lbl.Length > 0) jb.Label = lbl;
            }

            return jobs;
        }

private static void ApplyGroupLimit(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats, Dictionary<string, List<decimal[]>> ladders)
        {
            {
                HashSet<long> cks = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (!l.IsFlat && l.ContractKey > 0) cks.Add(l.ContractKey);
                if (cks.Count == 0 || strats == null || strats.Count == 0) return;

                foreach (long ck in cks)
                {
                    StrategyDef sd;
                    if (!strats.TryGetValue(ck, out sd)) continue;
                    foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_LIMIT))
                    {
                        // LIMIT is ALWAYS group-pooled now (the builder forces 'G'); legacy 'S' rows are
                        // pooled too rather than silently doing nothing.
                        if (rule.LimitQty <= 0m) continue;
                        // Gather this contract's covered usage lines (scope + item set), deterministic order.
                        // Only BK/CL print meters join the pool ("BK+CL usage" scope means exactly that —
                        // NA meters like scan/plotter stay out). Ladder meters stay out too: their FOC lives
                        // in the ladder's own 0.00 band and ComputeCharge ignores ln.Foc for them, so a pool
                        // share allocated there would be silently DISCARDED (the group would lose free copies).
                        List<MeterBillLine> members = new List<MeterBillLine>();
                        foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                            foreach (MeterBillLine l in jb.Lines)
                            {
                                if (l.IsFlat || l.ContractKey != ck) continue;
                                if (l.ColorLabel != "Black" && l.ColorLabel != "Colour") continue;
                                if (ServiceContractPhotocopier.Classes.ScpMultiPrice.HasLadder(ladders, l.MultiPriceCode)) continue;
                                if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                                if (rule.Scope == "BK" && l.ColorLabel != "Black") continue;
                                if (rule.Scope == "CL" && l.ColorLabel != "Colour") continue;
                                members.Add(l);
                            }
                        members.Sort(delegate (MeterBillLine a, MeterBillLine b)
                        {
                            int c = a.ItemKey.CompareTo(b.ItemKey);
                            return c != 0 ? c : a.ItemMeterKey.CompareTo(b.ItemMeterKey);
                        });

                        if (members.Count == 0) continue;
                        // Total RAW usage of the group; the pool frees min(LimitQty, total usage).
                        decimal totalUsage = 0m;
                        foreach (MeterBillLine l in members) totalUsage += l.Usage;
                        if (totalUsage <= 0m) continue;
                        decimal poolUsed = Math.Min(rule.LimitQty, totalUsage);

                        // Proportional share per machine (whole copies; remainder to the last so the shares
                        // sum EXACTLY to poolUsed). The share REPLACES the machine's per-meter FOC.
                        decimal allocated = 0m;
                        for (int i = 0; i < members.Count; i++)
                        {
                            MeterBillLine l = members[i];
                            decimal share = (i == members.Count - 1)
                                ? poolUsed - allocated
                                : Math.Round(poolUsed * l.Usage / totalUsage, 0, MidpointRounding.AwayFromZero);
                            if (share < 0m) share = 0m;
                            if (share > l.Usage) share = l.Usage;   // never free more than this machine printed
                            allocated += share;
                            l.Foc = share;   // REPLACE per-meter FOC with the group pool share
                            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, ladders);
                            l.StrategyNote = "GROUP FOC: " + share.ToString("0") + " free (pool " +
                                             poolUsed.ToString("0") + "/" + totalUsage.ToString("0") +
                                             " of " + rule.LimitQty.ToString("0") + ")";
                        }
                    }
                }
            }
        }

private static void ApplyRentalFreeN(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats, int genYear, int genMonth)
        {
            if (strats == null || strats.Count == 0) return;
            HashSet<long> waiveOwners = ItemsWithWaiveMeters(jobs);
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                {
                    if (!l.IsRental || l.ContractKey <= 0 || l.Charge <= 0m) continue;
                    // A machine with a WAIVE METER owns its rental deal there — the strategy rule
                    // must not zero the rent too (double waive). Waive lines themselves never apply.
                    if (l.IsWaiveMeter || waiveOwners.Contains(l.ItemKey)) continue;
                    StrategyDef sd;
                    if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                    foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_RENTAL_FREE_N))
                    {
                        if (rule.FreeMonths <= 0) continue;
                        if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                        DateTime? anchor = l.RentalStartDate ?? l.EffStartDate;
                        if (!anchor.HasValue)
                        {
                            l.StrategyNote = "RENTAL FREE rule skipped - no rental start / service start date to count from";
                            continue;
                        }
                        int monthNo = (genYear * 12 + genMonth) - (anchor.Value.Year * 12 + anchor.Value.Month) + 1;
                        if (monthNo >= 1 && monthNo <= rule.FreeMonths)
                        {
                            l.Charge = 0m;
                            l.UseMin = false;
                            l.StrategyNote = "RENTAL FREE month " + monthNo + "/" + rule.FreeMonths + " (strategy)";
                            // User rule: every meter SHOWS on the invoice — a freed rental prints as a
                            // 0.00 line with this note, never silently vanishes into a NO-CHARGE stamp.
                            l.AlwaysBill = true;
                        }
                        break;   // one free-N rule per rental line
                    }
                }
        }

private static void ApplyWaiveTarget(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats)
        {
            {
                HashSet<long> cks = new HashSet<long>();
                List<MeterBillLine> usage = new List<MeterBillLine>();   // all non-flat (BK/CL) usage lines
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (l.ContractKey > 0) cks.Add(l.ContractKey);
                        if (!l.IsFlat && l.ContractKey > 0) usage.Add(l);
                    }
                if (cks.Count == 0 || strats == null || strats.Count == 0) return;
                HashSet<long> waiveOwners = ItemsWithWaiveMeters(jobs);

                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (!l.IsRental || l.Foc > 0m || l.Charge <= 0m) continue;
                        // Machines with a WAIVE METER: the deal lives there (see ApplyWaiveMeters).
                        if (l.IsWaiveMeter || waiveOwners.Contains(l.ItemKey)) continue;
                        StrategyDef sd;
                        if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                        // The strategy is a bundle of rules — pick the WAIVE-TARGET rule that applies to THIS
                        // rental line: an item-specific rule (its ServiceItemKeys set contains this item) wins
                        // over a contract-wide rule (empty ServiceItemKeys = all items).
                        StrategyRule wr = null;
                        foreach (StrategyRule cand in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_WAIVE_TARGET))
                        {
                            if (cand.ServiceItemKeys.Count > 0 && cand.ServiceItemKeys.Contains(l.ItemKey)) { wr = cand; break; }
                            if (cand.ServiceItemKeys.Count == 0 && wr == null) wr = cand;
                        }
                        if (wr == null || wr.TargetAmount <= 0m) continue;
                        // Target total = usage charges of the SAME contract, filtered by the rule's Service
                        // Item set (empty = all) and its Scope ("BK"=Black only / "CL"=Colour only / else both).
                        decimal total = 0m;
                        foreach (MeterBillLine u in usage)
                        {
                            if (u.ContractKey != l.ContractKey) continue;
                            // Only BK/CL PRINT charges count toward the target — the scope choices are
                            // "Black / Colour / BK+CL usage", so NA meters (scan/plotter) never count.
                            if (u.ColorLabel != "Black" && u.ColorLabel != "Colour") continue;
                            if (wr.ServiceItemKeys.Count > 0 && !wr.ServiceItemKeys.Contains(u.ItemKey)) continue;
                            if (wr.Scope == "BK" && u.ColorLabel != "Black") continue;
                            if (wr.Scope == "CL" && u.ColorLabel != "Colour") continue;
                            total += u.Charge;
                        }
                        if (total >= wr.TargetAmount)
                        {
                            l.Charge = 0m;
                            l.WaivedRental = true;
                            l.StrategyNote = "WAIVED: meter charges " + total.ToString("0.00") +
                                             " >= target " + wr.TargetAmount.ToString("0.00");
                            l.AlwaysBill = true;   // waived = still printed at 0.00 with the note (user rule)
                        }
                        else if (wr.PartialPct > 0m && total >= wr.TargetAmount * wr.PartialPct / 100m)
                        {
                            l.Charge = Math.Round(l.Charge * (1m - wr.PartialPct / 100m), 2);
                            l.StrategyNote = "PARTIAL WAIVE " + wr.PartialPct.ToString("0.##") + "%: meter charges " +
                                             total.ToString("0.00") + " >= " +
                                             (wr.TargetAmount * wr.PartialPct / 100m).ToString("0.00") +
                                             " (" + wr.PartialPct.ToString("0.##") + "% of target " + wr.TargetAmount.ToString("0.00") + ")";
                        }
                    }
            }
        }

private static void ApplyCommittedMin(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs,
            Dictionary<long, StrategyDef> strats)
        {
            {
                // The arithmetic lives in ScpCommittedMin so it can be tested without a screen. It
                // sums across ALL jobs on purpose: a rental-separate or Bill Group split spreads one
                // machine's lines over several jobs, and a per-job sum would read zero on the job
                // with no usage and bill the whole minimum there.
                List<MeterBillLine> everything = new List<MeterBillLine>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines) everything.Add(l);

                // Items already carrying a committed meter — the COMMIT-MIN rule must not top those
                // up a second time.
                ServiceContractPhotocopier.Classes.ScpCommittedMin.PrintedSums printed;
                HashSet<long> minMeterItems =
                    ServiceContractPhotocopier.Classes.ScpCommittedMin.ApplyMeterMinimums(everything, out printed);

                // COMMIT-MIN RULES act LIVE too (no meter push): for every covered item WITHOUT a MIN
                // meter, a top-up line is synthesized when its scoped print charges fall short.
                if (strats == null || strats.Count == 0) return;
                // ONE top-up per item per RUN (not per job): with rental-separate ("_R") or Bill Group
                // splits an item's lines can span jobs — the old per-job dedup synthesized a SECOND
                // full-amount top-up on the rental-only job (printed=0 there). The top-up prints once,
                // on the job carrying the item's usage charges, and counts printed across ALL jobs.
                Dictionary<long, MeterInvoiceGenerator.InvoiceJob> hostByItem =
                    new Dictionary<long, MeterInvoiceGenerator.InvoiceJob>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (l.ItemKey > 0 && !l.IsFlat && !hostByItem.ContainsKey(l.ItemKey))
                            hostByItem[l.ItemKey] = jb;
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                    foreach (MeterBillLine l in jb.Lines)
                        if (l.ItemKey > 0 && !hostByItem.ContainsKey(l.ItemKey))
                            hostByItem[l.ItemKey] = jb;   // flat-only item: any job carrying it
                HashSet<long> doneItems = new HashSet<long>();
                foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                {
                    List<MeterBillLine> extra = new List<MeterBillLine>();
                    foreach (MeterBillLine l in jb.Lines)
                    {
                        if (l.ItemKey <= 0 || l.ContractKey <= 0) continue;
                        if (hostByItem[l.ItemKey] != jb) continue;   // synthesize only on the host job
                        if (!doneItems.Add(l.ItemKey)) continue;
                        if (minMeterItems.Contains(l.ItemKey)) continue;   // MIN meter already rules this item
                        StrategyDef sd;
                        if (!strats.TryGetValue(l.ContractKey, out sd)) continue;
                        foreach (StrategyRule rule in sd.RulesOfKind(ServiceContractPhotocopier.Classes.ScpStrategy.TYPE_COMMIT_MIN))
                        {
                            if (rule.CommitAmount <= 0m) continue;
                            if (rule.ServiceItemKeys.Count > 0 && !rule.ServiceItemKeys.Contains(l.ItemKey)) continue;
                            // Scoped print charges of THIS item across ALL jobs (run-wide buckets built
                            // above — identical filter to the old inner loop, but complete after splits).
                            decimal ruleprinted = printed.ForItem(l.ItemKey, rule.Scope);
                            decimal topUp = rule.CommitAmount - ruleprinted;
                            if (topUp < 0m) topUp = 0m;
                            MeterBillLine minLn = new MeterBillLine();
                            minLn.ItemKey = l.ItemKey;
                            minLn.ContractKey = l.ContractKey;
                            minLn.ContractNo = l.ContractNo;
                            minLn.ItemNo = l.ItemNo;
                            minLn.ItemName = l.ItemName;
                            minLn.ItemDesc = l.ItemDesc;
                            minLn.DebtorCode = l.DebtorCode;
                            minLn.SerialNumber = l.SerialNumber;
                            minLn.MeterTypeCode = "COMMIT-MIN";
                            minLn.MeterTypeName = "MINIMUM COMMITTED PRINT CHARGES";
                            minLn.IsFlat = true;
                            minLn.IsCommittedMin = true;
                            minLn.AlwaysBill = true;   // transparency: shown even at RM 0
                            minLn.CommittedAmount = rule.CommitAmount;
                            minLn.PrintedAmount = ruleprinted;
                            minLn.Charge = topUp;
                            minLn.StrategyCode = l.StrategyCode;
                            minLn.StrategyNote = "COMMITTED MIN (rule) " + rule.CommitAmount.ToString("0.00") +
                                                 ": printed " + ruleprinted.ToString("0.00") + " -> top-up " + topUp.ToString("0.00");
                            extra.Add(minLn);
                            break;   // one committed-min line per item
                        }
                    }
                    foreach (MeterBillLine x in extra) jb.Lines.Add(x);
                }
            }
        }

private static Dictionary<long, string> BuildContractSnapshots(HashSet<long> cks,
            Dictionary<long, StrategyDef> strats, int genYear, int genMonth,
            DataTable allRows)
        {
            Dictionary<long, string> map = new Dictionary<long, string>();
            // Header flags from the loaded grid rows (first row per contract carries them).
            Dictionary<long, string> header = new Dictionary<long, string>();
            foreach (DataRow r in allRows.Rows)
            {
                long ck = D64(r["ContractKey"]);
                if (ck <= 0 || header.ContainsKey(ck)) continue;
                header[ck] = "Contract " + S(r["ContractNo"]) + "  |  Customer " + S(r["DebtorCode"]) +
                    "  |  Strategy '" + S(r["StrategyCode"]) + "'  |  FOC Reset " + S(r["FOCResetUnit"]) +
                    (S(r["FOCResetUnit"]) == "D" ? "/" + S(r["FOCResetN"]) : "") +
                    "  |  RentalSeparate " + (r["RentSep"] != DBNull.Value && Convert.ToBoolean(r["RentSep"]) ? "Y" : "N") +
                    "  |  PeriodFollowContract " + (r["PeriodByContract"] != DBNull.Value && Convert.ToBoolean(r["PeriodByContract"]) ? "Y" : "N");
            }
            foreach (long ck in cks)
            {
                StrategyDef sd;
                strats.TryGetValue(ck, out sd);
                string head;
                if (!header.TryGetValue(ck, out head)) head = "Contract key " + ck;
                map[ck] = "Billing period " + genYear + "-" + genMonth.ToString("00") + "\r\n" + head + "\r\n" +
                          ServiceContractPhotocopier.Classes.ScpStrategy.SerializeRules(sd);
            }
            return map;
        }


        /// <summary>How many times a contract's free quantity resets inside one billing period.
        /// The period is passed in rather than read off a screen -- more than one screen bills now.</summary>
        public static int FocResetCountFor(DataRow r, int year, int month)
        {
            string unit = r.Table.Columns.Contains("FOCResetUnit") ? S(r["FOCResetUnit"]) : "M";
            int n = r.Table.Columns.Contains("FOCResetN") && r["FOCResetN"] != DBNull.Value ? Convert.ToInt32(r["FOCResetN"]) : 0;
            int periodDays = System.DateTime.DaysInMonth(year, month);
            return ServiceContractPhotocopier.Classes.ScpMultiPrice.FocResetCount(unit, n, periodDays);
        }

private static HashSet<long> ItemsWithWaiveMeters(Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs)
        {
            HashSet<long> set = new HashSet<long>();
            foreach (MeterInvoiceGenerator.InvoiceJob jb in jobs.Values)
                foreach (MeterBillLine l in jb.Lines)
                    if (l.IsWaiveMeter) set.Add(l.ItemKey);
            return set;
        }

        // The same three converters the screen uses. They travel with the code that needs them.
        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal Dec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
    }
}
