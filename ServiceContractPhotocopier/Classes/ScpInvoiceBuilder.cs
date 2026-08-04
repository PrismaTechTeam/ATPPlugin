using System;
using System.Collections.Generic;
using AutoCount.Authentication;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// One billable meter line for the Meter Reading billing run (one machine + one colour role).
    /// </summary>
    public class MeterBillLine
    {
        public long ItemKey;
        public long ContractKey;
        public long ItemMeterKey;
        public string ContractNo = "";
        public string ItemNo = "";
        public string DebtorCode = "";
        public string SerialNumber = "";
        public string MeterTypeCode = "";
        public string MeterTypeName = "";
        public string ACItemCode = "";
        public string ItemName = "";      // service item no (label)
        public string ItemDesc = "";      // service item description (model/location text on the line)
        public string ColorLabel = "";   // "Black" or "Colour"
        public decimal Last;
        public decimal Current;
        public decimal Usage;
        public decimal Rate;
        public decimal MinCharges;
        public decimal Foc;
        public decimal RebatePct;
        public decimal Charge;
        public bool UseMin;
        public bool IsFlat;   // flat/rental meter: bill qty 1 x rate regardless of readings
        // --- strategy pipeline (P0 plumbing; wired progressively in P5a-P5c) ---
        public string StrategyCode = "";   // contract's strategy in force for this billing run
        public string StrategyNote = "";   // human-readable outcome ("WAIVED: ...", "NET: ...")
        public bool NetBilling;            // FOC-REBATE strategy with NetBilling='Y'
        public bool WaivedRental;          // WAIVE-TARGET met -> rental billed 0 this period
        public bool IsRental;              // flat AND a rental (RA-*) type — distinguishes from MIN-*
        public decimal BillCopies;         // the NET qty the invoice line bills (usage - FOC)
        public string MultiPriceCode = ""; // tiered-pricing ladder code (per-meter override or meter type)
        public decimal EffUnitPrice;       // the resolved per-copy price (multi-price tier, or flat rate)
        public int FocResetCount = 1;      // FOC allowance multiplier = reset periods in this billed span
                                           // (1 = monthly reset on a monthly bill; 4 ≈ weekly reset billed monthly)
        public bool IsCommittedMin;        // a "MIN ..." committed-minimum meter — bills the TOP-UP to the
                                           // committed amount over the item's print charges, always shown
        public decimal CommittedAmount;    // the committed minimum (the MIN meter's Minimum Charges)
        public decimal PrintedAmount;      // the item's actual print (BK/CL) charges the top-up was measured against
        public bool AlwaysBill;            // keep this line on the invoice even when its charge is 0 (transparency)
        public DateTime? RentalStartDate;  // n/N anchor
        public int RentalMonths;           // N (0 = open-ended, no n/N text)
        public char RentalBasis = 'A';     // A accrual / P prepayment
        public string MachineStatus = "";  // API fetch status (ONLINE/OFFLINE/"") — drives the
                                           // advanced per-status invoice number format
        public string TrackingId = "";     // offline reading report id ("MR-yymmdd-nnn") — the job's
                                           // distinct ids become the invoice Reference No
        public bool IsGroupItem;           // line belongs to the contract's GROUP "machine" — its
                                           // MIN/WAIVE sums span the WHOLE fleet, not one machine
        // --- Rental-Waive contra meter (master-style; the engine decides firing at Generate) ---
        public bool IsWaiveMeter;
        public int WaiveFirstNMonths;      // 0 = no window condition
        public decimal WaiveTargetAmount;  // 0 = no usage condition (0 & 0 = ALWAYS waive)
        public decimal WaivePartialThreshold;  // partial band (RM, demo 28/07 #24): charges reach RM X...
        public decimal WaivePartialAmount;     // ...-> waive RM Y off the rental (0/0 = no partial band)
        public string WaiveScope = "BKCL";
        public DateTime? EffStartDate;     // effective service start (item, else contract) — RENTAL-FREE-N anchor fallback
        public DateTime? LastDate;
        /// <summary>API LastAuditDate of the CURRENT reading — used as the MeterTrans date so the
        /// next period's "Last Read Date" is the real meter-read date (falls back to now if absent).</summary>
        public DateTime? AuditDate;
        // #16 "billing period follows contract date": when set, the invoice DISPLAYS these contract-
        // cycle dates instead of LastDate/AuditDate (Previous = PeriodStart, Current = PeriodEnd).
        // DISPLAY ONLY — stamps, staging and the reading log always keep the real audit dates.
        public DateTime? PeriodStart;
        public DateTime? PeriodEnd;
    }

    /// <summary>
    /// Shared meter-billing math + AutoCount invoice construction. Mirrors the proven
    /// MeterTypeTransactionEntry_Form pipeline (RecalcRow + BuildInvoiceDocument) so the new
    /// Meter Reading Integration flow produces identical invoices.
    /// </summary>
    public static class ScpInvoiceBuilder
    {
        /// <summary>THE single meter-charge engine (used by BOTH the grid preview and the invoice, so they
        /// can never diverge). NET convention (user decision 2026-07-22):
        ///   billable = max(0, usage - FOC)                         (FOC copies really deducted)
        ///   unitprice = flat-per-tier multi-price by usage, else the flat ChargesRate
        ///   charge = round(billable x unitprice, 2) x (1 - rebate%)  (rebate really deducted)
        ///   charge floored at MinCharges (committed minimum still bills when FOC covers all usage)
        /// Flat/rental meters bill qty 1 x rate (FOC Qty = free-rental months -> RM0 this period).
        /// Pass the pre-loaded multi-price ladders (null = flat rate only).</summary>
        public static void ComputeCharge(MeterBillLine ln,
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<decimal[]>> ladders)
        {
            if (ln.IsFlat)
            {
                ln.Usage = 0m; ln.BillCopies = 0m;
                if (ln.Foc > 0m) { ln.Charge = 0m; ln.UseMin = false; ln.EffUnitPrice = 0m; return; }
                decimal flat = ln.Rate;
                if (flat < ln.MinCharges) flat = ln.MinCharges;
                ln.UseMin = (ln.Rate == 0m && ln.MinCharges > 0m);
                ln.EffUnitPrice = ln.Rate;
                ln.Charge = flat;
                return;
            }

            decimal usage = ln.Current - ln.Last;
            if (usage < 0m) usage = 0m;
            ln.Usage = usage;

            // Billed copies + effective per-copy price come from ONE of two mechanisms:
            //   (a) multi-price ladder present -> MARGINAL charge (its 0.00 first band IS the FOC allowance);
            //       billed = usage - freeCopies, effective price = grossCharge / billed (blended).
            //   (b) no ladder -> flat rate with the FOCQty column deducted (NET).
            // FOC reset accrual: the free allowance refreshes every reset period, so a bill spanning N
            // reset periods grants N x the base allowance (FocResetCount; 1 = the simple monthly case).
            int resetN = ln.FocResetCount < 1 ? 1 : ln.FocResetCount;
            decimal billed, effUnit;
            if (ScpMultiPrice.HasLadder(ladders, ln.MultiPriceCode))
            {
                decimal freeCopies;
                // Scale the ladder's boundaries (its FOC bands + tier breaks are "per reset period").
                decimal gross = ScpMultiPrice.MarginalCharge(ladders, ln.MultiPriceCode, usage, resetN, out freeCopies);
                billed = usage - freeCopies;
                if (billed < 0m) billed = 0m;
                effUnit = billed > 0m ? Math.Round(gross / billed, 6) : 0m;
            }
            else
            {
                billed = usage - ln.Foc * resetN;
                if (billed < 0m) billed = 0m;
                effUnit = ln.Rate;
            }
            ln.BillCopies = billed;
            ln.EffUnitPrice = effUnit;

            decimal sub = Math.Round(billed * effUnit, 2);
            decimal charge = ln.RebatePct > 0m ? Math.Round(sub * (1m - ln.RebatePct / 100m), 2) : sub;

            // Minimum-charge floor. A committed minimum still bills even when the allowance covers all usage.
            if (charge < ln.MinCharges) { ln.Charge = ln.MinCharges; ln.UseMin = true; }
            else { ln.Charge = charge; ln.UseMin = false; }
        }

        /// <summary>
        /// Builds (but does NOT save) an Invoice document from the given billable lines.
        /// LEGACY FORMAT — mirrors how the customer's V8 book created meter invoices (verified against
        /// v8_atp_main salesinvoice/salesinvoiceitem, e.g. MR2603.0691):
        ///   • header Description = "Billing- [ref]"; Remark1 = the CSSI/contract ref.
        ///   • ONE detail line per meter: Description = "{item desc} S/N:{serial}- {meter name}",
        ///     Qty = billable copies, UnitPrice = rate; the reading breakdown lives in
        ///     FurtherDescription as the legacy 16-line structured block (legend + 15 values).
        ///   • Minimum-charge lines: Qty 1 × the minimum, FurtherDescription "*** Minimum Charges ***".
        /// </summary>
        public static AutoCount.Invoicing.Sales.Invoice.Invoice BuildInvoice(
            DBSetting db, string debtorCode, string refDocNo, string description,
            DateTime docDate, DateTime readingDate, List<MeterBillLine> lines)
        {
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
                AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(UserSession.CurrentUserSession, db);

            AutoCount.Invoicing.Sales.Invoice.Invoice doc = cmd.AddNew();
            if (doc == null)
                throw new InvalidOperationException("Failed to create a new invoice document.");

            // Meter invoices draw their number from the book's own Document Numbering Format (native
            // AutoCount numbering, per-month running set -> MR2607.0001 style). The format NAME is
            // configurable in Service Option; it is only applied when it actually exists, so a book
            // without it falls back to the IV default numbering.
            try
            {
                string fmtName = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_INVOICE_DOCNO_FORMAT,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_INVOICE_DOCNO_FORMAT).Trim();
                // ADVANCED numbering: ONLINE machines -> one format, OFFLINE -> another (a mixed
                // invoice counts as ONLINE); lines with no API status keep the default format.
                if (ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                        ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ADVANCED, false))
                {
                    bool anyOnline = false, anyOffline = false;
                    foreach (MeterBillLine l0 in lines)
                    {
                        string ms = (l0.MachineStatus ?? "").Trim().ToUpperInvariant();
                        if (ms.StartsWith("ONLINE")) anyOnline = true;
                        else if (ms.StartsWith("OFFLINE")) anyOffline = true;
                    }
                    string adv = anyOnline
                        ? ServiceContractPhotocopier.Data.PumsConfig.Get(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_ONLINE, "").Trim()
                        : (anyOffline
                            ? ServiceContractPhotocopier.Data.PumsConfig.Get(db, ServiceContractPhotocopier.Data.PumsConfig.KEY_INV_FORMAT_OFFLINE, "").Trim()
                            : "");
                    if (adv.Length > 0) fmtName = adv;
                }
                if (fmtName.Length > 0)
                {
                    System.Data.DataTable fmt = db.GetDataTable(
                        "SELECT TOP 1 Name FROM dbo.DocNoFormat WHERE DocType='IV' AND Name='" + fmtName.Replace("'", "''") + "'", false);
                    if (fmt.Rows.Count > 0) doc.DocNoFormatName = fmtName;
                }
            }
            catch { }

            doc.DebtorCode = debtorCode;
            doc.DocDate = docDate;
            doc.Description = description;
            doc.RefDocNo = refDocNo ?? "";
            doc.Remark1 = refDocNo ?? "";
            if (doc.DetailCount > 0) doc.ClearDetails();

            // Each meter line's invoice detail carries its CONTRACT's Department + Project so the invoice
            // is analysed exactly like the contract it bills. Loaded once per distinct contract.
            System.Collections.Generic.Dictionary<long, string[]> contractDeptProj = LoadContractDeptProj(db, lines);

            // Invoice line description source (user decision 2026-07-27): DEFAULT = the AutoCount stock
            // item's own description (master convention — "BK COPY + PRINT A4&A3"); the Plugin Option
            // can switch back to the meter type name. One lookup per distinct item code.
            bool descFromItem = ServiceContractPhotocopier.Data.PumsConfig.GetBool(db,
                ServiceContractPhotocopier.Data.PumsConfig.KEY_INVOICE_DESC_FROM_ITEM,
                ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_INVOICE_DESC_FROM_ITEM);
            System.Collections.Generic.Dictionary<string, string> itemDescByCode =
                new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (descFromItem)
            {
                System.Collections.Generic.List<string> codes = new System.Collections.Generic.List<string>();
                foreach (MeterBillLine l0 in lines)
                    if (!string.IsNullOrEmpty(l0.ACItemCode) && !codes.Contains(l0.ACItemCode)) codes.Add(l0.ACItemCode);
                if (codes.Count > 0)
                {
                    System.Text.StringBuilder inList = new System.Text.StringBuilder();
                    foreach (string c in codes)
                    {
                        if (inList.Length > 0) inList.Append(",");
                        inList.Append("N'").Append(c.Replace("'", "''")).Append("'");
                    }
                    try
                    {
                        System.Data.DataTable it = db.GetDataTable(
                            "SELECT ItemCode, ISNULL(Description,'') AS Description FROM dbo.Item WHERE ItemCode IN (" + inList + ")", false);
                        foreach (System.Data.DataRow r in it.Rows)
                            itemDescByCode[Convert.ToString(r["ItemCode"])] = Convert.ToString(r["Description"]).Trim();
                    }
                    catch { }
                }
            }

            foreach (MeterBillLine ln in lines)
            {
                string lineDept = "", lineProj = "";
                string[] dp;
                if (ln.ContractKey > 0 && contractDeptProj.TryGetValue(ln.ContractKey, out dp))
                { lineDept = dp[0]; lineProj = dp[1]; }

                // ComputeCharge sets UseMin authoritatively (charge was floored to the minimum).
                bool minBilled = ln.UseMin;
                string block;
                if (ln.IsCommittedMin)
                    // Committed-minimum meter: show the committed amount, the item's actual print charges,
                    // and the top-up billed — so the customer sees WHY the amount is what it is (transparency).
                    block = "*** MINIMUM COMMITTED PRINT CHARGES ***\r\n" +
                            "Committed Minimum : " + ln.CommittedAmount.ToString("#,##0.00") + "\r\n" +
                            "Print Charges     : " + ln.PrintedAmount.ToString("#,##0.00") + "\r\n" +
                            "Top-Up Billed     : " + ln.Charge.ToString("#,##0.00");
                else if (minBilled)
                    block = "*** Minimum Charges ***";
                else
                    block = ComposeBreakdown(ln, readingDate);

                // Charge row (NET convention): Qty = billed copies (usage - FOC), UnitPrice = the resolved
                // per-copy price (multi-price tier or flat rate), Rebate % as a line discount — so the line
                // total = ComputeCharge's NET charge and the invoice matches the grid exactly. Minimum-floor
                // and flat/rental lines bill 1 x the (already-final) charge.
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dtl = doc.AddDetail();
                if (!string.IsNullOrEmpty(ln.ACItemCode)) dtl.ItemCode = ln.ACItemCode;
                string itemMasterDesc;
                dtl.Description = descFromItem && !string.IsNullOrEmpty(ln.ACItemCode)
                        && itemDescByCode.TryGetValue(ln.ACItemCode, out itemMasterDesc)
                        && itemMasterDesc.Length > 0
                    ? itemMasterDesc                    // master convention: the stock item's description
                    : ComposeLineDescription(ln);       // fallback / option OFF: meter type name
                if (minBilled || ln.IsFlat || ln.BillCopies <= 0m)
                { dtl.Qty = 1m; dtl.UnitPrice = ln.Charge; }
                else
                {
                    dtl.Qty = ln.BillCopies;
                    dtl.UnitPrice = ln.EffUnitPrice;
                    if (ln.RebatePct > 0m) dtl.Discount = ln.RebatePct.ToString("0.##") + "%";
                }
                // AutoCount's native FOC Qty column carries the free copies actually APPLIED to this
                // bill (user request): the ladder's free band or the meter's Free Qty allowance.
                // Flat lines (rental / waive / MIN) have no copies to give free.
                if (!ln.IsFlat)
                {
                    decimal focCol = ln.Usage - ln.BillCopies;
                    if (focCol < 0m) focCol = 0m;
                    if (focCol > 0m) dtl.FOCQty = focCol;
                }
                dtl.FurtherDescription = block;
                // Department + Project = the billed contract's (per line, since a grouped invoice can
                // span several contracts). Empty contract values leave the detail's defaults untouched.
                if (!string.IsNullOrEmpty(lineDept)) dtl.DeptNo = lineDept;
                if (!string.IsNullOrEmpty(lineProj)) dtl.ProjNo = lineProj;

                // Strategy outcome ("RENTAL FREE month 1/1 (strategy)", "WAIVED: charges >= target",
                // "PARTIAL WAIVE 90%: ...") — its own text row directly under the charge row, so the
                // CUSTOMER sees why a rental is 0.00 or reduced (user transparency rule).
                if (!string.IsNullOrEmpty(ln.StrategyNote))
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dNote = doc.AddDetail();
                    dNote.Description = ln.StrategyNote;
                    dNote.FurtherDescription = block;
                    dNote.AccNo = null;
                }

                // Reading text rows — exactly the master's content: Current / Previous / (FOC when >0)
                // / Usage, each carrying the same block, then a blank separator row. Dates dd/MM/yyyy,
                // readings plain digits (no thousands separators — the master prints "126699").
                // A committed-minimum meter has no reading, so it skips these (its committed/print/top-up
                // breakdown is already on the charge row's description).
                if (!ln.IsCommittedMin)
                {
                // #16: contract-period mode swaps the DISPLAYED dates (Previous = period start,
                // Current = period end); readings themselves are always the real meter values.
                DateTime curDate = ln.PeriodEnd ?? (ln.AuditDate ?? readingDate);
                string curDateStr = curDate.ToString("dd/MM/yyyy");
                DateTime? lastDate = ln.PeriodStart ?? ln.LastDate;
                string lastDateStr = lastDate.HasValue ? lastDate.Value.ToString("dd/MM/yyyy") : "";

                // Text rows carry NO account (master: acccode empty on description-only rows) — AddDetail
                // pre-fills the default sales account, so it is explicitly cleared here.
                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dCur = doc.AddDetail();
                dCur.Description = "Current Meter Reading (" + curDateStr + ") : " + Num(ln.Current);
                dCur.FurtherDescription = block;
                dCur.AccNo = null;

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dPrev = doc.AddDetail();
                dPrev.Description = "Previous Meter Reading (" + lastDateStr + ") : " + Num(ln.Last);
                dPrev.FurtherDescription = block;
                dPrev.AccNo = null;

                // FOC actually APPLIED to this bill: for a ladder meter that is the ladder's own free
                // band (usage − billed copies) — its FOCQty column is ignored by the engine, so printing
                // the raw column here used to show a FOC that was never deducted. Flat (rental) lines
                // keep the column value (= free months).
                decimal focApplied = ln.IsFlat ? ln.Foc : (ln.Usage - ln.BillCopies);
                if (focApplied < 0m) focApplied = 0m;
                if (focApplied > 0m)
                {
                    AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dFoc = doc.AddDetail();
                    dFoc.Description = "Meter FOC Qty : " + Num(focApplied);
                    dFoc.FurtherDescription = block;
                    dFoc.AccNo = null;
                }

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dUse = doc.AddDetail();
                dUse.Description = "Meter Charges Usage : " + Num(ln.Usage);
                dUse.FurtherDescription = block;
                dUse.AccNo = null;
                }   // end reading rows (skipped for committed-minimum meters)

                AutoCount.Invoicing.Sales.Invoice.InvoiceDetail dBlank = doc.AddDetail();
                dBlank.Description = "";
                dBlank.AccNo = null;
            }
            return doc;
        }

        // Contract Department + Project for every distinct ContractKey referenced by the lines.
        // Returns { ContractKey -> [DeptNo, ProjNo] }; contracts with none map to empty strings.
        private static System.Collections.Generic.Dictionary<long, string[]> LoadContractDeptProj(
            DBSetting db, List<MeterBillLine> lines)
        {
            System.Collections.Generic.Dictionary<long, string[]> map =
                new System.Collections.Generic.Dictionary<long, string[]>();
            System.Collections.Generic.HashSet<long> keys = new System.Collections.Generic.HashSet<long>();
            foreach (MeterBillLine ln in lines) if (ln.ContractKey > 0) keys.Add(ln.ContractKey);
            if (keys.Count == 0) return map;
            try
            {
                string inList = string.Join(",", new List<long>(keys).ConvertAll(k => k.ToString()).ToArray());
                System.Data.DataTable dt = db.GetDataTable(
                    "SELECT ContractKey, ISNULL(DeptNo,'') AS DeptNo, ISNULL(ProjNo,'') AS ProjNo " +
                    "FROM dbo.zSCP2_Contract WHERE ContractKey IN (" + inList + ")", false);
                foreach (System.Data.DataRow r in dt.Rows)
                    map[Convert.ToInt64(r["ContractKey"])] = new string[]
                    { Convert.ToString(r["DeptNo"]), Convert.ToString(r["ProjNo"]) };
            }
            catch { }
            return map;
        }

        // Master convention (verified against v8_atp_main salesinvoiceitem): the line description is
        // the METER TYPE's description ONLY — e.g. "BK COPY + PRINT A4&A3". Machine-specific texts like
        // "PUSPEN ... S/N:4NL20240- BK COPY+PRINT A4&A3" exist in the master only because the customer
        // created per-machine meter types carrying that full text as the meter description; the item's
        // own description is never prefixed by the billing code.
        private static string ComposeLineDescription(MeterBillLine ln)
        {
            if (!string.IsNullOrEmpty(ln.MeterTypeName)) return ln.MeterTypeName.Trim();
            if (!string.IsNullOrEmpty(ln.MeterTypeCode)) return ln.MeterTypeCode.Trim();
            return ln.ItemName;
        }

        // The legacy 16-line More Description block (legend + 15 values), copied verbatim from the
        // customer's V8 meter invoices.
        private static string ComposeBreakdown(MeterBillLine ln, DateTime readingDate)
        {
            // #16: contract-period mode swaps the DISPLAYED dates in fields 7/13/14/15; the ACTUAL
            // audit dates are always appended as fields 16/17 so report designs can print either.
            DateTime actualCur = ln.AuditDate ?? readingDate;
            DateTime cur = ln.PeriodEnd ?? actualCur;
            System.Globalization.CultureInfo en = new System.Globalization.CultureInfo("en-US");
            DateTime? lastDate = ln.PeriodStart ?? ln.LastDate;
            string lastLong = lastDate.HasValue ? LongDate(lastDate.Value, en) : "";
            string lastShort = lastDate.HasValue ? lastDate.Value.ToString("yyyyMMdd") : "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("1. Mt Type; 2. Mt Name; 3. Min Chrg; 4. Chrg Rate; 5. FOC Qty; 6. Rebate %; " +
                          "7. Last Reading Date; 8. Last Reading Mt; 9. Mt Usage; 10. Total Chrg; 11. Multi Price; " +
                          "12. Rebate Qty; 13. Current Reading Date; 14. Last Reading Short Date; 15. Current Reading Short Date; " +
                          "16. Actual Last Audit; 17. Actual Current Audit");
            // FOC as APPLIED (ladder free band / reset-scaled column), consistent with the FOC text row.
            decimal focShown = ln.IsFlat ? ln.Foc : (ln.Usage - ln.BillCopies);
            if (focShown < 0m) focShown = 0m;
            sb.AppendLine(ln.MeterTypeCode);
            sb.AppendLine(ComposeLineDescription(ln));
            sb.AppendLine(Num(ln.MinCharges));
            sb.AppendLine(Num(ln.Rate));
            sb.AppendLine(Num(focShown));
            sb.AppendLine(Num(ln.RebatePct));
            sb.AppendLine(lastLong);
            sb.AppendLine(Num(ln.Last));
            sb.AppendLine(Num(ln.Usage));
            sb.AppendLine(Num(ln.Charge));
            // multi price: per-meter override ladders are keyed '#<meterkey>' internally — print
            // the customer-friendly "(Custom)" instead of the raw key.
            string mpShown = ln.MultiPriceCode ?? "";
            if (mpShown.StartsWith("#")) mpShown = "(Custom)";
            sb.AppendLine(mpShown);
            sb.AppendLine("0");                      // rebate qty
            sb.AppendLine(LongDate(cur, en));
            sb.AppendLine(lastShort);
            sb.AppendLine(cur.ToString("yyyyMMdd"));
            // 16/17: the ACTUAL audit dates (same as 14/15 unless contract-period mode swapped them).
            sb.AppendLine(ln.LastDate.HasValue ? ln.LastDate.Value.ToString("yyyyMMdd") : "");
            sb.Append(actualCur.ToString("yyyyMMdd"));
            return sb.ToString();
        }

        private static string Num(decimal v) { return v.ToString("0.######"); }

        // Legacy long stamp: "24 Feb 2026 5:10:06 pm" (lowercase am/pm).
        private static string LongDate(DateTime d, System.Globalization.CultureInfo en)
        {
            return d.ToString("d MMM yyyy h:mm:ss", en) + " " + d.ToString("tt", en).ToLowerInvariant();
        }
    }
}
