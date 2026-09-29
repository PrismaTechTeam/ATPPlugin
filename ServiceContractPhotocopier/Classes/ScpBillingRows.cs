using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using AutoCount.Data;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// The rows that are about to be billed: one row per counter, carrying everything the money
    /// decision needs -- the machine, the readings, the prices, the contract terms.
    ///
    /// <para>This used to live inside the Meter Reading Integration screen, which was fine while
    /// that screen was the only thing that billed anything. It is not any more: inter-billing bills
    /// one contract at a time, and it must get the SAME rows, or the two screens would answer
    /// differently about the same money.</para>
    ///
    /// <para>The two callers differ in one place only -- WHICH rows. That screen asks by billing
    /// day, tab and search box; inter-billing asks for one contract. So the query takes its four
    /// filter clauses as arguments and everything after the WHERE is shared, unchanged.</para>
    /// </summary>
    public static class ScpBillingRows
    {
        /// <summary>
        /// An invoice DELETED (or cancelled) in AutoCount releases its meters for re-billing: the billed
        /// readings it wrote are removed (they would freeze Last Reading at the billed value) and the
        /// billing-period stamps are cleared, so the month is to bill again. Only rows whose DocKey came
        /// from this plugin's generator are touched. Best-effort: a load is never blocked by it.
        /// </summary>
        public static void ReconcileDeletedInvoices(DBSetting db)
        {
            if (db == null) return;
            try
            {
                // Audit BEFORE the delete: the billed readings about to be un-billed are appended to the
                // immutable log as INVOICE-DELETED — stamped with WHO ran the reconcile and carrying the
                // original invoice's baseline/usage over from its INVOICE log row.
                string who = "";
                try { who = (AutoCount.Authentication.UserSession.CurrentUserSession.LoginUserID ?? "").Replace("'", "''"); }
                catch { }
                db.ExecuteNonQuery(
                    "INSERT INTO dbo.zSCP2_MeterReadingLog (ItemMeterKey, PeriodYear, PeriodMonth, Reading, ReadingDate, Source, DocNo, CreatedBy, LastReading, [Usage], UnitPrice, MinCharges, FOCQty, RebatePct, Charge) " +
                    "SELECT t.ServiceItemMeterTypeKey, ISNULL(me.PeriodYear,0), ISNULL(me.PeriodMonth,0), " +
                    "t.MeterTransReading, t.MeterTransDate, 'INVOICE-DELETED', ISNULL(me.InvoicedDocNo,''), N'" + who + "', " +
                    "orig.LastReading, orig.[Usage], orig.UnitPrice, orig.MinCharges, orig.FOCQty, orig.RebatePct, orig.Charge " +
                    "FROM dbo.zSCP_MeterTrans t " +
                    "LEFT JOIN dbo.zSCP2_MeterEntry me ON me.InvoicedDocKey = t.SalesInvoiceDocKey AND me.ItemMeterKey = t.ServiceItemMeterTypeKey " +
                    "OUTER APPLY (SELECT TOP 1 l.LastReading, l.[Usage], l.UnitPrice, l.MinCharges, l.FOCQty, l.RebatePct, l.Charge FROM dbo.zSCP2_MeterReadingLog l " +
                    "  WHERE l.ItemMeterKey = t.ServiceItemMeterTypeKey AND l.Source = 'INVOICE' " +
                    "  AND l.DocNo = ISNULL(me.InvoicedDocNo,'') ORDER BY l.LogKey DESC) orig " +
                    "WHERE t.SalesInvoiceDocKey IS NOT NULL " +
                    "AND t.SalesInvoiceDocKey IN (SELECT me2.InvoicedDocKey FROM dbo.zSCP2_MeterEntry me2 WHERE me2.InvoicedDocKey IS NOT NULL) " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.SalesInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                db.ExecuteNonQuery(
                    "DELETE t FROM dbo.zSCP_MeterTrans t " +
                    "WHERE t.SalesInvoiceDocKey IS NOT NULL " +
                    "AND t.SalesInvoiceDocKey IN (SELECT me.InvoicedDocKey FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocKey IS NOT NULL) " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = t.SalesInvoiceDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                db.ExecuteNonQuery(
                    "UPDATE me SET InvoicedDocKey=NULL, InvoicedDocNo='', InvoicedAt=NULL " +
                    "FROM dbo.zSCP2_MeterEntry me " +
                    "WHERE me.InvoicedDocKey IS NOT NULL " +
                    "AND NOT EXISTS (SELECT 1 FROM dbo.IV iv WHERE iv.DocKey = me.InvoicedDocKey AND ISNULL(iv.Cancelled,'F') <> 'T')");
                // #10: reading overrides whose CN was deleted/cancelled roll back the same way.
                ScpCreditNoteBuilder.ReconcileDeletedCreditNotes(db);
            }
            catch { }   // reconciliation is best-effort; the load itself must never be blocked
        }

        /// <summary>The row shape: 58 columns, and every one of them is read by the job builder.</summary>
        public static DataTable NewTable()
        {
        DataTable dt = new DataTable();
        // ---- visible: item columns (these merge), then meter columns ----
        dt.Columns.Add("SelCssi", typeof(bool));   // ONE merged checkbox per CSSI — ticks/unticks the whole machine (both meters)
        dt.Columns.Add("ContractNo", typeof(string));
        dt.Columns.Add("ServiceItemNo", typeof(string));
        dt.Columns.Add("SerialNo", typeof(string));
        dt.Columns.Add("ItemDesc", typeof(string));        // item description (invoice line text; hidden)
        dt.Columns.Add("MachineStatus", typeof(string));   // ONLINE / OFFLINE (set on fetch, per item)
        dt.Columns.Add("Customer", typeof(string));
        dt.Columns.Add("Mode", typeof(string));
        dt.Columns.Add("BillingDay", typeof(int));   // effective billing day = COALESCE(item override, contract day)
        dt.Columns.Add("MeterType", typeof(string));
        dt.Columns.Add("MeterTypeName", typeof(string));
        dt.Columns.Add("MinCharges", typeof(decimal));
        dt.Columns.Add("UnitPrice", typeof(decimal));
        dt.Columns.Add("FOCQty", typeof(decimal));
        dt.Columns.Add("MultiPriceCode", typeof(string));   // tiered-pricing ladder code (hidden; drives the tier price)
        dt.Columns.Add("FOCResetUnit", typeof(string));     // contract FOC reset period M/W/D (hidden)
        dt.Columns.Add("FOCResetN", typeof(int));           // N for 'D' (hidden)
        dt.Columns.Add("RebatePct", typeof(decimal));
        dt.Columns.Add("LastReadDate", typeof(DateTime));
        dt.Columns.Add("LastAuditDate", typeof(DateTime));   // API audit date of the CURRENT fetched reading
        dt.Columns.Add("LastFetchDate", typeof(DateTime));   // when the reading was last fetched/saved into staging
        dt.Columns.Add("DateEdited", typeof(bool));          // the Last Audit Date was typed by a person, not stamped (hidden)
        dt.Columns.Add("LastReading", typeof(decimal));
        dt.Columns.Add("CurrentReading", typeof(decimal));
        dt.Columns.Add("MeterUsage", typeof(decimal));
        dt.Columns.Add("TotalCharges", typeof(decimal));
        dt.Columns.Add("LastInvNo", typeof(string));      // last invoice generated for this meter (any period)
        dt.Columns.Add("LastInvDate", typeof(DateTime));  // its date — GREEN when it falls in the selected billing period (= already invoiced now)
        dt.Columns.Add("InvTotal", typeof(decimal));      // that invoice's NetTotal (shown on the Invoiced tab only)
        dt.Columns.Add("UseMin", typeof(bool));
        dt.Columns.Add("Sel", typeof(bool));
        dt.Columns.Add("Status", typeof(string));
        // ---- hidden keys ----
        dt.Columns.Add("ItemKey", typeof(long));
        dt.Columns.Add("ContractKey", typeof(long));
        dt.Columns.Add("DebtorCode", typeof(string));
        dt.Columns.Add("BillingMode", typeof(string));
        dt.Columns.Add("ItemMeterKey", typeof(long));
        dt.Columns.Add("ACItemCode", typeof(string));
        dt.Columns.Add("MachineLineShows", typeof(string));
        dt.Columns.Add("ShowModel", typeof(bool));    // name the model under the charge
        dt.Columns.Add("ShowSerial", typeof(bool));   // name the machines under it
        dt.Columns.Add("ShowUnits", typeof(bool));    // say how many machines the line covers
        dt.Columns.Add("MergeGroupCodeMeter", typeof(string));
        dt.Columns.Add("Role", typeof(string));
        dt.Columns.Add("Shade", typeof(int));   // 0/1 per-item zebra shade (hidden)
        dt.Columns.Add("EntrySource", typeof(string));    // where CurrentReading came from: MANUAL/ONLINE/OFFLINE/'' (hidden)
        dt.Columns.Add("TrackingId", typeof(string));     // offline report id ("MR-yymmdd-nnn"); '' = none (hidden)
        dt.Columns.Add("FetchedReading", typeof(decimal)); // last value the API returned for this meter (hidden)
        dt.Columns.Add("IsFlat", typeof(bool));            // flat/rental meter: auto-billed qty 1 x price, no reading
        dt.Columns.Add("IsWaive", typeof(bool));           // Rental-Waive contra meter (engine decides firing)
        dt.Columns.Add("IsGroupItem", typeof(bool));       // the contract's GROUP machine (fleet-total deals)
        dt.Columns.Add("MachineMode", typeof(string));     // DEFINED online/offline ('' = use fetch status)
        dt.Columns.Add("BillGroupCode", typeof(string));   // #6 bill-group split ('' = none)
        dt.Columns.Add("LineGroupCode", typeof(string));   // the word printed on the line ("HEAVY DUTY")
        dt.Columns.Add("MergeGroupCode", typeof(string));  // which LINE it merges onto ('' = follow the mode)
        dt.Columns.Add("ModelCode", typeof(string));       // the machine's model — bucket when grouping by model
        dt.Columns.Add("NewMoneyRules", typeof(bool));     // contract has a Billing Format -> bills like the
                                                          // customer's own invoices (rebate as copies, etc.)
        dt.Columns.Add("WaiveFirstNMonths", typeof(int));
        dt.Columns.Add("WaiveTargetAmount", typeof(decimal));
        dt.Columns.Add("WaivePartialThreshold", typeof(decimal));
        dt.Columns.Add("WaivePartialAmount", typeof(decimal));
        dt.Columns.Add("WaiveScope", typeof(string));
        dt.Columns.Add("CommitScope", typeof(string));    // S machine / G merge group / C contract
        dt.Columns.Add("StrategyCode", typeof(string));    // contract's strategy in force (hidden; stamped at generate)
        dt.Columns.Add("RentSep", typeof(bool));           // contract flag: rental billed on its own invoice (hidden)
        dt.Columns.Add("TierIncremental", typeof(bool));   // ATP-3: the contract prices tiers band by band (hidden)
        dt.Columns.Add("PeriodByContract", typeof(bool));  // #16: invoice display dates follow the contract cycle (hidden)
        dt.Columns.Add("RentalBillingDay", typeof(int));   // rental-separate invoice's own day; 0 = follow meter date (hidden)
        /// <summary>The day of the month THIS row is due on -- not the contract's, the row's.
        /// A rental billing apart follows the contract's Rental invoice day; everything else
        /// follows the billing due day, or the machine's own override. Two rows of one machine
        /// can be due on different days, which is the whole point: the rent on the 1st, the
        /// copies on the 7th.</summary>
        dt.Columns.Add("DueDay", typeof(int));
        /// <summary>Its day has gone and it still has not been billed. Set by the screen, which
        /// is the only thing that knows what day is being worked.</summary>
        dt.Columns.Add("Late", typeof(bool));
        dt.Columns.Add("RentalStartDate", typeof(DateTime)); // rental period anchor (hidden; n/N)
        dt.Columns.Add("RentalMonths", typeof(int));       // rental total periods N (hidden)
        dt.Columns.Add("RentalBasis", typeof(string));     // A accrual / P prepayment -- the CONTRACT's (ATP-10; hidden)
        dt.Columns.Add("RentalFromN", typeof(int));        // ATP-10: first rental month this bill pays for in advance (0 = accrual; hidden)
        dt.Columns.Add("RentalToN", typeof(int));          // ATP-10: last one (hidden)
        dt.Columns.Add("RentalFor", typeof(string));       // "2/36 · OCT 2026": the rental months this row bills (shown)
        dt.Columns.Add("RentalMonthsDue", typeof(int));    // ATP-10: rental months on this bill -- 1, or 2 for a machine that joined late (hidden)
        dt.Columns.Add("LastRentalPeriod", typeof(int));   // ATP-10: YYYYMM this meter was last billed before this month, 0 = never (hidden)
        dt.Columns.Add("PrevHandled", typeof(bool));       // ATP-10: the contract's previous month is billed or skipped (hidden)
        dt.Columns.Add("EffExpiry", typeof(DateTime));     // ATP-10: the machine's effective end -- its last month bills no rental in advance (hidden)
        dt.Columns.Add("BilledNow", typeof(bool));         // ATP-10: this meter's month is already invoiced (hidden)
        dt.Columns.Add("NeedManual", typeof(bool));        // SNAPSHOT tab membership: set at load/fetch, NOT live —
                                                           // keying a reading must not make the row vanish mid-work (hidden)
        dt.Columns.Add("Locked", typeof(bool));            // billing-day auto-fetch snapshot lock — reading frozen (hidden)
        dt.Columns.Add("HasConflict", typeof(bool));       // saved-manual value differs from a fresh API value (hidden)
        dt.Columns.Add("IsExpired", typeof(bool));         // effective expiry BEFORE the billing month (row tinted; Generate warns)
        dt.Columns.Add("NotStarted", typeof(bool));        // the contract has not begun yet (start date after today) - off the run's working tabs
        dt.Columns.Add("EffStart", typeof(DateTime));      // effective service start (RENTAL-FREE-N month anchor; hidden)
        dt.Columns.Add("ContractStart", typeof(DateTime)); // CONTRACT service start (#16 period anchor - one period per invoice; hidden)
        dt.Columns.Add("InvoicedDocNo", typeof(string));   // non-empty = this meter+period is already invoiced (hidden)
        return dt;
        }

        /// <summary>
        /// Which day of the month a row is due on, as SQL.
        ///
        /// <para>The screen filters on it and the row carries it, and both have to agree, so
        /// there is one copy of the rule and it lives here. A rental -- and the waive that rides
        /// the same invoice -- follows Rental invoice day, but ONLY when the rental bills apart:
        /// on one shared invoice there is one document and it cannot carry two dates. Anything
        /// else, and 0, means "follow the meters".</para>
        /// </summary>
        public const string RowDueDaySql =
            "(CASE WHEN (UPPER(ISNULL(m.MeterRole,'')) IN ('RENTAL','WAIVE') " +
            "            OR UPPER(ISNULL(m.MeterTypeCode,'')) LIKE 'RA%' " +
            "            OR UPPER(ISNULL(m.MeterTypeCode,'')) LIKE '%RENTAL%') " +
            "           AND ISNULL(c.RentalSeparateInvoice,'N') = 'Y' " +
            "           AND ISNULL(c.RentalBillingDay,0) BETWEEN 1 AND 28 " +
            "      THEN c.RentalBillingDay " +
            "      ELSE COALESCE(i.BillingDayOverride, c.BillingDay) END)";

        /// <summary>The query. Everything after WHERE is the same for every caller; the four filter
        /// clauses are what differ. Pass "1=1" for the inactive one and "" for the rest to mean
        /// "no filter".</summary>
        public static string Sql(int year, int month, string inactiveFilter, string dayFilter,
                                string searchFilter, string expiryFilter)
        {
            string sql =
                // Per-meter machine serial: a multi-machine CSSI assigns meters to provided units
                // (m.MachineSerialNo); '' falls back to the CSSI's own header machine serial.
                "SELECT i.ItemKey, c.ContractKey, c.ContractNo, i.ServiceItemNo, " +
                "ISNULL(i.Description,'') AS ItemDesc, " +
                "COALESCE(NULLIF(m.MachineSerialNo,''), i.SerialNumber) AS SerialNumber, " +
                "c.DebtorCode, ISNULL(d.CompanyName,'') AS DebtorName, c.BillingMode, " +
                "ISNULL(c.StrategyCode,'') AS StrategyCode, ISNULL(c.RentalSeparateInvoice,'N') AS RentSep, " +
                "ISNULL(c.PeriodFollowContract,'N') AS PeriodByContract, c.ServiceStartDate AS ContractStart, " +
                // ATP-3: the meter's own tier rule (moved off the contract 26/9).
                "ISNULL(m.TierMode,'T') AS TierMode, " +
                // What the machine line names -- model, duty label, or both. From the contract's
                // format; 'B' for a contract that has none, which is what it printed before.
                // The contract carries its own answer now; the format is only a fallback for
                // the contracts that have not been moved across yet.
                "ISNULL(NULLIF(c.MachineLineShows,''), ISNULL(bf.MachineLineShows,'B')) AS MachineLineShows, " +
                // The two ticks. Contract-level only -- a Billing Format is a house style and these
                // are about one customer's fleet.
                "CASE WHEN UPPER(ISNULL(c.ShowModelOnLine,'Y'))  = 'N' THEN 0 ELSE 1 END AS ShowModel, " +
                "CASE WHEN UPPER(ISNULL(c.ShowSerialOnLine,'Y')) = 'N' THEN 0 ELSE 1 END AS ShowSerial, " +
                "CASE WHEN UPPER(ISNULL(c.ShowUnitsOnLine,'Y'))  = 'N' THEN 0 ELSE 1 END AS ShowUnits, " +
                "ISNULL(c.RentalBillingDay,0) AS RentalBillingDay, " +
                "ISNULL(c.FOCResetUnit,'M') AS FOCResetUnit, ISNULL(c.FOCResetN,0) AS FOCResetN, " +
                "COALESCE(i.BillingDayOverride, c.BillingDay) AS EffBillingDay, " +
                // The row's own due day, worked out in SQL so the screen can filter on it
                // before a single row is fetched. A WAIVE rides the rental invoice, so it
                // keeps the rental's day too.
                RowDueDaySql + " AS DueDay, " +
                "ISNULL(i.IsGroupItem,'N') AS IsGroupItem, ISNULL(i.MachineMode,'') AS MachineMode, " +
                // BillGroupCode picks the INVOICE; LineGroupCode picks the LINE and is the word
                // ("HEAVY DUTY") the line prints. ItemCode is the machine's model — the bucket
                // when a contract groups its lines by model (v11 / v14).
                "ISNULL(i.BillGroupCode,'') AS BillGroupCode, ISNULL(i.LineGroupCode,'') AS LineGroupCode, " +
                "ISNULL(i.MergeGroupCodeMeter,'') AS MergeGroupCodeMeter, " +
                // MergeGroupCode says which LINE this machine prints on when the contract merges
                // -- a hand-made group beats the line mode's own bucket (v12).
                "ISNULL(i.MergeGroupCode,'') AS MergeGroupCode, " +
                "ISNULL(i.ItemCode,'') AS ModelCode, ISNULL(c.BillingFormatCode,'') AS BillingFormatCode, " +
                "ISNULL(c.UseNewLayout,'N') AS UseNewLayout, " +
                "m.ItemMeterKey, m.MeterRole, m.MeterTypeCode, ISNULL(mt.Description,'') AS MeterTypeName, " +
                // Invoice line Item Code = the meter type's stock code (master convention: metertype.stockcode
                // goes on the charge row); ACItemCode is an explicit override when set.
                "ISNULL(NULLIF(mt.ACItemCode,''), ISNULL(mt.StockCode,'')) AS ACItemCode, ISNULL(m.MinimumCharges,0) AS MinCharges, " +
                "ISNULL(m.ChargesRate,0) AS UnitPrice, ISNULL(m.FOCQty,0) AS FOCQty, " +
                // Per-meter tier override (zSCP2_ItemMeterPrice) beats the scheme code; its ladder is
                // keyed '#<ItemMeterKey>' in the loaded ladder map (ScpMultiPrice.MeterLadderKey).
                // Joined (pm), NOT a correlated EXISTS — the correlated form ballooned this query's
                // memory grant and stalled it on RESOURCE_SEMAPHORE when the server was memory-squeezed.
                "CASE WHEN pm.ItemMeterKey IS NOT NULL " +
                "     THEN '#' + CAST(m.ItemMeterKey AS varchar(20)) " +
                "     ELSE ISNULL(NULLIF(m.MeterMultiPriceCode,''), ISNULL(mt.MeterMultiPriceCode,'')) END AS MultiPriceCode, " +
                "ISNULL(m.RebateQtyInPercent,0) AS RebatePct, ISNULL(m.InitialReading,0) AS InitReading, " +
                "ISNULL(mt.IsFlatCharge,'N') AS IsFlatCharge, ISNULL(mt.IsRentalWaive,'N') AS IsRentalWaive, " +
                // WaiveScope says WHICH charges count (black / colour / both); CommitScope says
                // WHOSE (this machine / its merge group / the whole contract).
                "ISNULL(m.CommitScope,'S') AS CommitScope, " +
                "ISNULL(m.WaiveFirstNMonths,0) AS WaiveFirstNMonths, ISNULL(m.WaiveTargetAmount,0) AS WaiveTargetAmount, " +
                "ISNULL(m.WaivePartialThreshold,0) AS WaivePartialThreshold, " +
                "ISNULL(m.WaivePartialAmount,0) AS WaivePartialAmount, ISNULL(m.WaiveScope,'BKCL') AS WaiveScope, " +
                // The CONTRACT says whether its rental is billed with the month's copies or a month
                // ahead (feedback ATP-10); the old per-machine field is kept in step by the contract.
                "m.RentalStartDate, ISNULL(m.RentalMonths,0) AS RentalMonths, ISNULL(c.RentalBasis,'A') AS RentalBasis, " +
                // ...and, for a contract billing in advance: the last month this meter was billed, whether
                // the contract's previous month is billed or skipped, and whether this month is billed.
                "ISNULL(lrb.P, 0) AS LastRentBilledP, CASE WHEN ph.ContractKey IS NULL THEN 0 ELSE 1 END AS PrevHandled, " +
                "CASE WHEN ISNULL(c.RentalBasis,'A') = 'P' AND EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry xb " +
                "  WHERE xb.ItemMeterKey = m.ItemMeterKey AND xb.PeriodYear = " + year + " AND xb.PeriodMonth = " + month +
                "    AND ISNULL(xb.InvoicedDocNo,'') <> '') THEN 1 ELSE 0 END AS BilledNow, " +
                "COALESCE(i.ServiceExpiryDate, c.ServiceExpiryDate) AS EffExpiry, " +
                "COALESCE(i.ServiceStartDate, c.ServiceStartDate) AS EffStart, " +
                "lr.LastReading, lr.LastDate, " +
                "ISNULL(li.LastInvNo,'') AS LastInvNo, li.LastInvAt, ISNULL(li.LastInvTotal,0) AS LastInvTotal " +
                "FROM dbo.zSCP2_ItemMeter m " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                "LEFT JOIN dbo.Debtor d ON d.AccNo = c.DebtorCode " +
                "LEFT JOIN dbo.zSCP2_BillingFormat bf ON bf.FormatCode = c.BillingFormatCode " +
                "LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
                "LEFT JOIN (SELECT DISTINCT ItemMeterKey FROM dbo.zSCP2_ItemMeterPrice) pm ON pm.ItemMeterKey = m.ItemMeterKey " +
                // Latest reading per meter in ONE pass over zSCP_MeterTrans (window function),
                // instead of a correlated TOP-1 OUTER APPLY per row (which timed out on 145k rows).
                "LEFT JOIN (SELECT z.ServiceItemMeterTypeKey, z.MeterTransReading AS LastReading, z.MeterTransDate AS LastDate " +
                "  FROM (SELECT t.ServiceItemMeterTypeKey, t.MeterTransReading, t.MeterTransDate, " +
                // A row BILLED in the selected period ALWAYS becomes the baseline (Last Reading +
                // Last Read Date follow the just-billed reading), even if stray legacy rows carry
                // out-of-order dates — then normal latest-date ordering.
                "        ROW_NUMBER() OVER (PARTITION BY t.ServiceItemMeterTypeKey ORDER BY " +
                "          CASE WHEN t.SalesInvoiceDocKey IS NOT NULL AND t.MeterTransDate >= DATEFROMPARTS(" + year + "," + month + ",1) THEN 1 ELSE 0 END DESC, " +
                "          t.MeterTransDate DESC, t.MeterTransKey DESC) AS rn " +
                // Last reading baseline = latest MeterTrans STRICTLY BEFORE the selected billing
                // month, PLUS any BILLED reading inside the month (SalesInvoiceDocKey set): once a
                // meter is invoiced, its Last Reading moves to the billed value so the remaining
                // usage shows 0 — and reverts automatically if that invoice is later deleted
                // (ReconcileDeletedInvoices removes the billed row).
                "        FROM dbo.zSCP_MeterTrans t WHERE t.MeterTransDate < DATEFROMPARTS(" + year + "," + month + ",1) " +
                "           OR (t.SalesInvoiceDocKey IS NOT NULL AND t.MeterTransDate < DATEADD(MONTH, 1, DATEFROMPARTS(" + year + "," + month + ",1)))) z WHERE z.rn = 1) lr ON lr.ServiceItemMeterTypeKey = m.ItemMeterKey " +
                // Last invoice ever generated for this meter (from the zSCP2_MeterEntry stamps) —
                // lets the operator tell "0 usage because ALREADY INVOICED" apart from "no reading".
                "LEFT JOIN (SELECT z2.ItemMeterKey, z2.InvoicedDocNo AS LastInvNo, z2.InvoicedAt AS LastInvAt, " +
                "         ivh.NetTotal AS LastInvTotal " +
                "  FROM (SELECT me.ItemMeterKey, me.InvoicedDocNo, me.InvoicedAt, me.InvoicedDocKey, " +
                "        ROW_NUMBER() OVER (PARTITION BY me.ItemMeterKey ORDER BY me.InvoicedAt DESC) AS rn " +
                "        FROM dbo.zSCP2_MeterEntry me WHERE me.InvoicedDocKey IS NOT NULL) z2 " +
                "  LEFT JOIN dbo.IV ivh ON ivh.DocKey = z2.InvoicedDocKey " +
                "  WHERE z2.rn = 1) li " +
                "ON li.ItemMeterKey = m.ItemMeterKey " +
                // ATP-10: contracts billing their rental in advance only -- nothing else pays for this.
                "LEFT JOIN (SELECT e3.ItemMeterKey, MAX(e3.PeriodYear * 100 + e3.PeriodMonth) AS P " +
                "  FROM dbo.zSCP2_MeterEntry e3 " +
                "  JOIN dbo.zSCP2_ItemMeter m3 ON m3.ItemMeterKey = e3.ItemMeterKey " +
                "  JOIN dbo.zSCP2_Item i3 ON i3.ItemKey = m3.ItemKey " +
                "  JOIN dbo.zSCP2_Contract c3 ON c3.ContractKey = i3.ContractKey AND ISNULL(c3.RentalBasis,'A') = 'P' " +
                "  WHERE ISNULL(e3.InvoicedDocNo,'') <> '' AND e3.PeriodYear * 100 + e3.PeriodMonth < " + (year * 100 + month) + " " +
                "  GROUP BY e3.ItemMeterKey) lrb ON lrb.ItemMeterKey = m.ItemMeterKey " +
                "LEFT JOIN (SELECT DISTINCT i5.ContractKey FROM dbo.zSCP2_MeterEntry e5 " +
                "  JOIN dbo.zSCP2_ItemMeter m5 ON m5.ItemMeterKey = e5.ItemMeterKey " +
                "  JOIN dbo.zSCP2_Item i5 ON i5.ItemKey = m5.ItemKey " +
                "  JOIN dbo.zSCP2_Contract c5 ON c5.ContractKey = i5.ContractKey AND ISNULL(c5.RentalBasis,'A') = 'P' " +
                "  WHERE e5.PeriodYear = " + (month == 1 ? year - 1 : year) + " AND e5.PeriodMonth = " + (month == 1 ? 12 : month - 1) +
                "    AND ISNULL(e5.InvoicedDocNo,'') <> '' " +
                "  UNION SELECT sk.ContractKey FROM dbo.zSCP2_ContractPeriodSkip sk " +
                "  WHERE sk.UndoneAt IS NULL AND sk.PeriodYear = " + (month == 1 ? year - 1 : year) +
                "    AND sk.PeriodMonth = " + (month == 1 ? 12 : month - 1) + ") ph ON ph.ContractKey = c.ContractKey " +
                // ALL meters of the item are shown — BK/CL usage meters AND the non-reading ones
                // (rental RA-*, minimum MIN-*, fax, etc., role NA). Previously only BK/CL showed,
                // which made the item's other meters look "missing".
                "WHERE " + inactiveFilter + " " + dayFilter + searchFilter + expiryFilter +
                "ORDER BY c.DebtorCode, c.ContractNo, i.ServiceItemNo, m.MeterRole " +   // customers cluster together
                // Memory-squeeze armour. On this dev box Windows pressure shrank SQL's query-workspace
                // pool to ~18MB and the PARALLEL plan's REQUIRED grant (~50MB, scales with DOP) could
                // never fit -> permanent RESOURCE_SEMAPHORE queue -> timeout. MAXDOP 1 cuts the required
                // minimum to a few MB (fits any pool); MAX_GRANT_PERCENT makes oversized asks spill to
                // tempdb instead of queuing. ~4k result rows — serial is fast anyway.
                "OPTION (MAXDOP 1, MAX_GRANT_PERCENT = 20)";
            return sql;
        }

        /// <summary>Runs the query and maps it into <see cref="NewTable"/> rows.</summary>
        public static DataTable Load(DBSetting db, int year, int month,
                                    string inactiveFilter, string dayFilter,
                                    string searchFilter, string expiryFilter,
                                    out Dictionary<string, List<decimal[]>> ladders)
        {
            return Load(db, year, month, inactiveFilter, dayFilter, searchFilter, expiryFilter, null, out ladders);
        }

        /// <summary>The same, with the rental basis given rather than read ("A" / "P"; null = each
        /// contract's own). Only Calculation Test does this: it bills the setting on the contract
        /// screen, saved or not.</summary>
        public static DataTable Load(DBSetting db, int year, int month,
                                    string inactiveFilter, string dayFilter,
                                    string searchFilter, string expiryFilter, string rentalBasisOverride,
                                    out Dictionary<string, List<decimal[]>> ladders)
        {
            DataTable src = Query(db, Sql(year, month, inactiveFilter, dayFilter,
                                          searchFilter, expiryFilter), 180);
            DataTable t = NewTable();
            ladders = ScpMultiPrice.LoadLadders(db);   // tiered pricing
            Dictionary<long, int> shadeByContract = new Dictionary<long, int>();
            foreach (DataRow r in src.Rows)
            {
                DataRow g = t.NewRow();
                long ck = D64(r["ContractKey"]);
                int shade;
                if (!shadeByContract.TryGetValue(ck, out shade))
                {
                    shade = shadeByContract.Count % 2;   // alternate per contract → clean colour bands
                    shadeByContract[ck] = shade;
                }
                g["Shade"] = shade;
                g["SelCssi"] = false;
                g["ContractNo"] = S(r["ContractNo"]);
                g["ServiceItemNo"] = S(r["ServiceItemNo"]);
                g["SerialNo"] = S(r["SerialNumber"]);
                g["ItemDesc"] = S(r["ItemDesc"]);
                g["Customer"] = S(r["DebtorCode"]) + " - " + S(r["DebtorName"]);
                g["Mode"] = S(r["BillingMode"]) == "S" ? "Separate" : "Group";
                if (r["EffBillingDay"] != DBNull.Value) g["BillingDay"] = Convert.ToInt32(r["EffBillingDay"]);
                g["MeterType"] = S(r["MeterTypeCode"]);
                g["MeterTypeName"] = S(r["MeterTypeName"]);
                g["MinCharges"] = Dec(r["MinCharges"]);
                g["UnitPrice"] = Dec(r["UnitPrice"]);
                g["FOCQty"] = Dec(r["FOCQty"]);
                g["MultiPriceCode"] = S(r["MultiPriceCode"]);
                g["FOCResetUnit"] = S(r["FOCResetUnit"]);
                g["FOCResetN"] = r["FOCResetN"] == DBNull.Value ? 0 : Convert.ToInt32(r["FOCResetN"]);
                g["RebatePct"] = Dec(r["RebatePct"]);
                if (r["LastDate"] != DBNull.Value) g["LastReadDate"] = Convert.ToDateTime(r["LastDate"]);
                g["LastReading"] = (r["LastReading"] == DBNull.Value) ? Dec(r["InitReading"]) : Dec(r["LastReading"]);
                g["CurrentReading"] = 0m;
                g["MeterUsage"] = 0m;
                g["TotalCharges"] = 0m;
                // "Use Min" means "this flat meter bills its minimum". A committed minimum does
                // NOT -- it bills the shortfall, worked out at Generate -- so it must not claim to.
                g["UseMin"] = Dec(r["UnitPrice"]) == 0m && Dec(r["MinCharges"]) > 0m &&
                              !ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinRole(
                                  S(r["MeterRole"]), S(r["MeterTypeCode"]), Dec(r["MinCharges"]));
                g["LastInvNo"] = S(r["LastInvNo"]);
                if (r["LastInvAt"] != DBNull.Value) g["LastInvDate"] = Convert.ToDateTime(r["LastInvAt"]);
                g["InvTotal"] = Dec(r["LastInvTotal"]);
                g["Sel"] = false;
                g["Status"] = "";
                g["ItemKey"] = D64(r["ItemKey"]);
                g["ContractKey"] = D64(r["ContractKey"]);
                g["DebtorCode"] = S(r["DebtorCode"]);
                g["BillingMode"] = S(r["BillingMode"]);
                g["ItemMeterKey"] = D64(r["ItemMeterKey"]);
                g["ACItemCode"] = S(r["ACItemCode"]);
            g["MachineLineShows"] = S(r["MachineLineShows"]);
                g["ShowModel"] = r["ShowModel"] == DBNull.Value || Convert.ToInt32(r["ShowModel"]) != 0;
                g["ShowSerial"] = r["ShowSerial"] == DBNull.Value || Convert.ToInt32(r["ShowSerial"]) != 0;
                g["ShowUnits"] = r["ShowUnits"] == DBNull.Value || Convert.ToInt32(r["ShowUnits"]) != 0;
            g["MergeGroupCodeMeter"] = S(r["MergeGroupCodeMeter"]);
                g["Role"] = S(r["MeterRole"]);
                g["EntrySource"] = "";
                g["FetchedReading"] = 0m;
                g["HasConflict"] = false;
                // A waive meter is flat BY DEFINITION (it has no reading) even if the type's
                // rental flag was left unticked in the master. Recognised by ROLE first, with the
                // type's own flag as the OR that keeps the legacy (W) family working untouched.
                g["IsWaive"] = ServiceContractPhotocopier.Classes.ScpStrategy.IsWaiveRole(
                    S(r["MeterRole"]), S(r["IsRentalWaive"]) == "Y");
                g["IsGroupItem"] = S(r["IsGroupItem"]) == "Y";
                g["MachineMode"] = S(r["MachineMode"]);
                g["BillGroupCode"] = ServiceContractPhotocopier.Classes.ScpStrategy.SanitizeBillGroup(S(r["BillGroupCode"]));
                g["LineGroupCode"] = S(r["LineGroupCode"]);
                g["MergeGroupCode"] = S(r["MergeGroupCode"]);
                g["ModelCode"] = S(r["ModelCode"]);
                // The same test ScpInvoiceLayout.LoadLayouts makes. This asked "does the contract
                // NAME a Billing Format", which was true of every contract while a format was the
                // only way off the legacy path. Contracts carry UseNewLayout now and no format name
                // at all, so it answered NO for all of them -- and ComposeFoldedDescription returns
                // on its first line when NewMoneyRules is false, which is why the model and the
                // serial never reached a generated invoice while the preview showed them.
                g["NewMoneyRules"] = S(r["UseNewLayout"]).Trim().ToUpperInvariant() == "Y"
                                  || S(r["BillingFormatCode"]).Length > 0;
                g["IsFlat"] = S(r["IsFlatCharge"]) == "Y" || S(r["IsRentalWaive"]) == "Y";
                g["WaiveFirstNMonths"] = r["WaiveFirstNMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["WaiveFirstNMonths"]);
                g["WaiveTargetAmount"] = Dec(r["WaiveTargetAmount"]);
                g["WaivePartialThreshold"] = Dec(r["WaivePartialThreshold"]);
                g["WaivePartialAmount"] = Dec(r["WaivePartialAmount"]);
                g["WaiveScope"] = S(r["WaiveScope"]);
                g["CommitScope"] = S(r["CommitScope"]);
                // Expired = effective expiry BEFORE the billing month's 1st (still billable IN its
                // final month — this only flags machines whose last billable month is already over).
                g["IsExpired"] = r["EffExpiry"] != DBNull.Value &&
                    Convert.ToDateTime(r["EffExpiry"]).Date < new DateTime(year, month, 1);
                // Not started = the CONTRACT's service start is still in the future, measured
                // against TODAY and not against the billing month: a deal signed today to start
                // on 1 Nov has nothing to read and nothing to bill, and sitting in the run only
                // gets it billed by accident (feedback ATP-11). The row is still loaded -- the
                // screen decides what to do with it.
                g["NotStarted"] = r["ContractStart"] != DBNull.Value &&
                    Convert.ToDateTime(r["ContractStart"]).Date > DateTime.Today;
                if (r["EffStart"] != DBNull.Value) g["EffStart"] = Convert.ToDateTime(r["EffStart"]);
                if (r["ContractStart"] != DBNull.Value) g["ContractStart"] = Convert.ToDateTime(r["ContractStart"]);
                g["StrategyCode"] = S(r["StrategyCode"]);
                g["RentSep"] = S(r["RentSep"]) == "Y";
                g["PeriodByContract"] = S(r["PeriodByContract"]) == "Y";
                g["TierIncremental"] = S(r["TierMode"]).Trim().ToUpperInvariant() == "I";
                g["RentalBillingDay"] = r["RentalBillingDay"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalBillingDay"]);
                g["DueDay"] = r.Table.Columns.Contains("DueDay") && r["DueDay"] != DBNull.Value
                    ? Convert.ToInt32(r["DueDay"]) : 0;
                g["Late"] = false;
                // (n/N) on the rental line. The machine's own rental dates when it has them --
                // otherwise the deal itself: a rental keyed with nothing but a contract from
                // 01/08/2026 to 31/07/2029 still prints "(2/36)", which is what the customer counts.
                if (r["RentalStartDate"] != DBNull.Value) g["RentalStartDate"] = Convert.ToDateTime(r["RentalStartDate"]);
                else if (r["EffStart"] != DBNull.Value) g["RentalStartDate"] = Convert.ToDateTime(r["EffStart"]);
                int rentalMonths = r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]);
                if (rentalMonths <= 0)
                    rentalMonths = ServiceContractPhotocopier.Classes.ScpStrategy.TermMonths(r["EffStart"], r["EffExpiry"]);
                g["RentalMonths"] = rentalMonths;
                g["RentalBasis"] = S(r["RentalBasis"]) == "P" ? "P" : "A";
                if (rentalBasisOverride == "A" || rentalBasisOverride == "P") g["RentalBasis"] = rentalBasisOverride;
                g["RentalFromN"] = 0;
                g["RentalToN"] = 0;
                g["RentalMonthsDue"] = 1;
                g["LastRentalPeriod"] = r["LastRentBilledP"] == DBNull.Value ? 0 : Convert.ToInt32(r["LastRentBilledP"]);
                g["PrevHandled"] = r["PrevHandled"] != DBNull.Value && Convert.ToInt32(r["PrevHandled"]) == 1;
                if (r["EffExpiry"] != DBNull.Value) g["EffExpiry"] = Convert.ToDateTime(r["EffExpiry"]);
                g["BilledNow"] = r["BilledNow"] != DBNull.Value && Convert.ToInt32(r["BilledNow"]) == 1;
                t.Rows.Add(g);
            }
            ApplyRentalBasis(t, year, month);
            return t;
        }

        /// <summary>
        /// Feedback ATP-10, a contract that bills its rental IN ADVANCE: each bill carries that month's
        /// copies and NEXT month's rental. So, for such a contract only --
        /// <list type="bullet">
        /// <item>a rental row pays for next month ("(2/36) NOV 2026") -- two months only for a machine
        /// that joined after the bill that should have carried its first month -- and nothing in the
        /// machine's last month (it was paid the month before);</item>
        /// <item>in the month before the start the contract bills its first month's rental alone: its
        /// counters, and a committed minimum, are not due until it starts;</item>
        /// <item>a waive stays with the rental it gives back, and goes where no rental is due.</item>
        /// </list>
        /// Rows that are not due are taken off the table, so every screen and the invoice agree that
        /// they are not there. A row already invoiced this month always stays. A contract billed with
        /// its month's copies -- every contract before ATP-10 -- is not touched.
        /// </summary>
        public static void ApplyRentalBasis(DataTable t, int year, int month)
        {
            if (t == null || t.Rows.Count == 0 || !t.Columns.Contains("RentalToN")) return;
            int period = year * 100 + month;
            List<DataRow> drop = new List<DataRow>();
            HashSet<long> rentDue = new HashSet<long>();
            foreach (DataRow r in t.Rows)
            {
                if (S(r["RentalBasis"]) != "P" || !IsRentRow(r)) continue;
                // Already invoiced this month, or no start to count from: it stays as it is -- and so
                // does its waive.
                if ((r["BilledNow"] != DBNull.Value && Convert.ToBoolean(r["BilledNow"])) || r["RentalStartDate"] == DBNull.Value)
                {
                    rentDue.Add(D64(r["ItemKey"]));
                    continue;
                }
                DateTime start = Convert.ToDateTime(r["RentalStartDate"]);
                // The rental month this bill pays for is fixed by the calendar -- next month's. It is
                // never worked out from what happens to be billed already: months billed together,
                // out of order, or after a deleted invoice must each still pay for their own month.
                int n = ScpStrategy.RentalPeriodN(start, 'P', year, month);
                // The machine's last month pays for nothing: its rental went out the month before.
                bool lastMonth = false;
                if (r["EffExpiry"] != DBNull.Value)
                {
                    DateTime end = Convert.ToDateTime(r["EffExpiry"]);
                    lastMonth = period >= end.Year * 100 + end.Month;
                }
                if (n < 1 || lastMonth)
                {
                    drop.Add(r);
                    continue;
                }
                // The one exception: a machine that joined after the bill that should have carried
                // its first month -- never billed itself, while the contract's previous month was
                // billed (or skipped) without it. That month goes on this bill with next month's,
                // and never more than the two.
                int due = 1;
                bool billedBefore = r["LastRentalPeriod"] != DBNull.Value && Convert.ToInt32(r["LastRentalPeriod"]) > 0;
                bool prevHandled = r["PrevHandled"] != DBNull.Value && Convert.ToBoolean(r["PrevHandled"]);
                if (!billedBefore && prevHandled && n >= 2) due = 2;
                r["RentalFromN"] = n - due + 1;
                r["RentalToN"] = n;
                r["RentalMonthsDue"] = due;
                // Due before the contract starts is the point: it is on the working tabs.
                r["NotStarted"] = false;
                rentDue.Add(D64(r["ItemKey"]));
            }
            foreach (DataRow r in t.Rows)
            {
                if (S(r["RentalBasis"]) != "P" || IsRentRow(r) || drop.Contains(r)) continue;
                if (r["BilledNow"] != DBNull.Value && Convert.ToBoolean(r["BilledNow"])) continue;
                if (IsWaiveRow(r))
                {
                    // The waive goes with its rent -- and, like the rent, it is due before the contract
                    // starts. It kept NotStarted, so the Meter Invoice Run left it off the invoice and
                    // a "first 2 months free" machine was billed its first month (29/9, #55: MR2609.0844
                    // charged 300.00 for 1/36 OCT 2026). A waive by target has no copies to reach yet
                    // and still does not fire.
                    if (!rentDue.Contains(D64(r["ItemKey"]))) drop.Add(r);
                    else r["NotStarted"] = false;
                    continue;
                }
                if (r["ContractStart"] == DBNull.Value) continue;
                DateTime cs = Convert.ToDateTime(r["ContractStart"]);
                if (period < cs.Year * 100 + cs.Month) drop.Add(r);
            }
            foreach (DataRow r in drop) t.Rows.Remove(r);
        }

        /// <summary>A machine's rental line -- not its waive, not a committed minimum.</summary>
        private static bool IsRentRow(DataRow r)
        {
            bool flat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            return flat && !IsWaiveRow(r) && !IsCommitRow(r)
                && ScpStrategy.IsRentalRole(S(r["Role"]), S(r["MeterType"]));
        }

        private static bool IsWaiveRow(DataRow r)
        {
            if (r.Table.Columns.Contains("IsWaive") && r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) return true;
            return S(r["Role"]).Trim().ToUpperInvariant() == "WAIVE";
        }

        /// <summary>Every billable row of ONE contract for one period, priced -- what a screen that
        /// bills a single contract needs, without any of the billing-day filtering that decides
        /// which contracts are DUE this month.</summary>
        public static DataTable ForContract(DBSetting db, long contractKey, int year, int month,
                                            out Dictionary<string, List<decimal[]>> ladders)
        {
            return ForContract(db, contractKey, year, month, null, out ladders);
        }

        /// <summary>The same, billing the rental basis given ("A" / "P", null = the saved one).</summary>
        public static DataTable ForContract(DBSetting db, long contractKey, int year, int month,
                                            string rentalBasisOverride,
                                            out Dictionary<string, List<decimal[]>> ladders)
        {
            DataTable t = Load(db, year, month, "1=1", "",
                               " AND c.ContractKey = " + contractKey + " ", "", rentalBasisOverride, out ladders);
            // Same order as the screen: the staged readings, then the flat charges that have
            // none, then the agreed prices. Any of them missing bills the wrong money.
            PrefillFromStaging(db, t, month, year, ladders);
            AutoFillFlatMeters(db, t, year, month, ladders);
            ApplyRentalGroupPrices(db, t);
            ApplyMeterLinePrices(db, t);
            return t;
        }

        public static void ApplyRentalGroupPrices(DBSetting db, DataTable rows)
        {
        if (rows == null || rows.Rows.Count == 0) return;
        System.Collections.Generic.List<long> keys = new System.Collections.Generic.List<long>();
        foreach (DataRow r in rows.Rows)
        {
            long ck = r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]);
            if (ck > 0 && !keys.Contains(ck)) keys.Add(ck);
        }
        if (keys.Count == 0) return;

        System.Collections.Generic.Dictionary<long, System.Collections.Generic.Dictionary<string, decimal>> prices =
            ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadForContracts(db, keys);
        if (prices.Count == 0) return;
        System.Collections.Generic.Dictionary<long, char> modes =
            ServiceContractPhotocopier.Classes.ScpInvoiceLayout.LoadRentalModes(db, keys);
        ApplyRentalGroupPrices(rows, prices, modes);
        }

        /// <summary>The same, with the prices handed in rather than read -- for a screen testing
        /// agreed prices that have not been saved yet (Calculation Test).</summary>
        public static void ApplyRentalGroupPrices(DataTable rows,
            System.Collections.Generic.Dictionary<long, System.Collections.Generic.Dictionary<string, decimal>> prices,
            System.Collections.Generic.Dictionary<long, char> modes)
        {
        if (rows == null || prices == null || modes == null || prices.Count == 0) return;
        foreach (DataRow r in rows.Rows)
        {
            if (r["IsFlat"] == DBNull.Value || !Convert.ToBoolean(r["IsFlat"])) continue;
            if (r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"])) continue;
            if (r["NewMoneyRules"] == DBNull.Value || !Convert.ToBoolean(r["NewMoneyRules"])) continue;
            if (!ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalRole(S(r["Role"]), S(r["MeterType"]))) continue;
            long ck = r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]);
            char mode;
            if (!modes.TryGetValue(ck, out mode)) continue;
            decimal? p = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.PriceFor(
                prices, ck, mode, S(r["ModelCode"]), S(r["MergeGroupCode"]));
            if (!p.HasValue) continue;
            r["UnitPrice"] = p.Value;
            // The group price IS the rental. A minimum left on the meter would outrank it
            // (a flat charge is the greater of the two) and quietly bill the old number.
            r["MinCharges"] = 0m;
            r["UseMin"] = false;
        }
        }

        public static void ApplyMeterLinePrices(DBSetting db, DataTable rows)
        {
        if (rows == null || rows.Rows.Count == 0) return;
        System.Collections.Generic.List<long> keys = new System.Collections.Generic.List<long>();
        foreach (DataRow r in rows.Rows)
        {
            long ck = r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]);
            if (ck > 0 && !keys.Contains(ck)) keys.Add(ck);
        }
        if (keys.Count == 0) return;

        System.Collections.Generic.Dictionary<long,
            System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpLineTerms>> byContract =
            new System.Collections.Generic.Dictionary<long,
                System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpLineTerms>>();
        foreach (long ck in keys)
            byContract[ck] = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadTerms(db, ck);
        ApplyMeterLinePrices(rows, byContract);
        }

        /// <summary>The same, with the terms handed in rather than read -- for a screen testing
        /// agreed prices that have not been saved yet (Calculation Test).</summary>
        public static void ApplyMeterLinePrices(DataTable rows,
            System.Collections.Generic.Dictionary<long,
                System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpLineTerms>> byContract)
        {
        if (rows == null || byContract == null) return;
        foreach (DataRow r in rows.Rows)
        {
            if (r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"])) continue;
            if (r["NewMoneyRules"] == DBNull.Value || !Convert.ToBoolean(r["NewMoneyRules"])) continue;
            string role = S(r["Role"]).Trim().ToUpperInvariant();
            if (role != "BK" && role != "CL") continue;

            long ck = r["ContractKey"] == DBNull.Value ? 0L : Convert.ToInt64(r["ContractKey"]);
            System.Collections.Generic.Dictionary<string, ServiceContractPhotocopier.Classes.ScpLineTerms> terms;
            if (!byContract.TryGetValue(ck, out terms) || terms.Count == 0) continue;

            string grp = S(r["MergeGroupCodeMeter"]).Trim();
            if (grp.Length == 0) continue;      // its own line -- its own rate, nothing to agree

            ServiceContractPhotocopier.Classes.ScpLineTerms t;
            if (!terms.TryGetValue(
                    ServiceContractPhotocopier.Classes.ScpLineTerms.Key(
                        ServiceContractPhotocopier.Classes.ScpLineTerms.SIDE_METER, grp), out t)) continue;

            decimal agreed = role == "BK" ? t.BkPrice : t.ClPrice;
            if (agreed <= 0m) continue;

            // A ladder prices the copies by band and outranks a flat figure; leave it alone.
            if (S(r["MultiPriceCode"]).Trim().Length > 0) continue;

            r["UnitPrice"] = agreed;
        }
        }


        /// <summary>How invoices are grouped, as the book is configured. Read from the setting
        /// rather than from a screen, so every screen that bills agrees about it.</summary>
        public static string GroupingMode(DBSetting db)
        {
            try
            {
                string m = ServiceContractPhotocopier.Data.PumsConfig.Get(db,
                    ServiceContractPhotocopier.Data.PumsConfig.KEY_METER_GROUPING,
                    ServiceContractPhotocopier.Data.PumsConfig.DEFAULT_METER_GROUPING).Trim().ToUpperInvariant();
                if (m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_DEBTOR ||
                    m == ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_MACHINE) return m;
            }
            catch { }
            return ServiceContractPhotocopier.Data.PumsConfig.METER_GROUPING_FOLLOW;
        }


        /// <summary>
        /// Puts the readings already staged for the period onto the rows.
        ///
        /// <para>The query above brings the machines and their prices; the READING is a separate
        /// fact, staged when somebody keyed or fetched it, and it arrives here. Leaving this out
        /// produced a contract that billed its rentals and nothing else -- every usage line at
        /// zero, silently, because a counter with no current reading has no usage.</para>
        /// </summary>
        public static void PrefillFromStaging(DBSetting db, DataTable rows, int month, int year,
                                              Dictionary<string, List<decimal[]>> ladders)
        {
            if (rows == null) return;
            Dictionary<long, DataRow> byMeter = new Dictionary<long, DataRow>();
            string sql = "SELECT ItemMeterKey, CurrentReading, ReadingDate, Source, InvoicedDocNo, InvoicedAt, LockedAt, LastModified, " +
                         "ISNULL(ReadingDateEdited,'N') AS ReadingDateEdited, " +
                         "ISNULL(TrackingId,'') AS TrackingId " +
                         "FROM dbo.zSCP2_MeterEntry WHERE PeriodYear=" + year + " AND PeriodMonth=" + month;
            DataTable saved = Query(db, sql, 60);
            foreach (DataRow s in saved.Rows) byMeter[D64(s["ItemMeterKey"])] = s;
            if (byMeter.Count == 0) return;

            foreach (DataRow r in rows.Rows)
            {
                DataRow s;
                if (!byMeter.TryGetValue(D64(r["ItemMeterKey"]), out s)) continue;
                string src = S(s["Source"]).Trim().ToUpperInvariant();
                r["CurrentReading"] = Dec(s["CurrentReading"]);
                r["EntrySource"] = src;
                r["TrackingId"] = S(s["TrackingId"]).Trim();
                if (s["ReadingDate"] != DBNull.Value) r["LastAuditDate"] = Convert.ToDateTime(s["ReadingDate"]);
                // Typed by a person (feedback ATP-5), so a later fetch leaves it alone while the
                // reading it belongs to is unchanged.
                r["DateEdited"] = S(s["ReadingDateEdited"]).Trim().ToUpperInvariant() == "Y";
                if (s["LastModified"] != DBNull.Value) r["LastFetchDate"] = Convert.ToDateTime(s["LastModified"]);
                Recalc(r, ladders, year, month);   // restored but NOT auto-selected — user picks rows before generating
                string dt = (s["ReadingDate"] != DBNull.Value) ? "  " + Convert.ToDateTime(s["ReadingDate"]).ToString("dd/MM/yyyy") : "";
                if (src == "ONLINE") { r["MachineStatus"] = "ONLINE"; r["Status"] = "Online (saved)" + dt; }
                else if (src == "OFFLINE") { r["MachineStatus"] = "OFFLINE"; r["Status"] = "Offline (saved)" + dt; }
                else { r["Status"] = "Manual (saved)" + dt; }

                // Billing-day auto-fetch snapshot: frozen — no fetch/manual override until invoiced.
                if (s["LockedAt"] != DBNull.Value)
                {
                    r["Locked"] = true;
                    r["Status"] = "🔒 AUTO-FETCHED (locked " +
                        Convert.ToDateTime(s["LockedAt"]).ToString("dd/MM/yyyy HH:mm") + ")" + dt;
                }

                // Billing-period guard: already invoiced for this period -> show it and lock out of Generate.
                string invNo = S(s["InvoicedDocNo"]).Trim();
                if (invNo.Length > 0)
                {
                    r["InvoicedDocNo"] = invNo;
                    r["LastInvNo"] = invNo;
                    if (s["InvoicedAt"] != DBNull.Value) r["LastInvDate"] = Convert.ToDateTime(s["InvoicedAt"]);
                    string invAt = (s["InvoicedAt"] != DBNull.Value) ? "  " + Convert.ToDateTime(s["InvoicedAt"]).ToString("dd/MM/yyyy") : "";
                    r["Status"] = "INVOICED " + invNo + invAt;
                }
            }
            // SyncSelCssi() is the grid's own selection housekeeping and stays with the grid.
                }

        /// <summary>A flat charge has nothing to read, so it is filled in for itself -- otherwise
        /// a rental would sit there waiting for a reading that will never come.</summary>
        public static void AutoFillFlatMeters(DBSetting db, DataTable rows, int year, int month,
                                              Dictionary<string, List<decimal[]>> ladders)
        {
            if (rows == null) return;
            foreach (DataRow r in rows.Rows)
            {
                if (r["IsFlat"] == DBNull.Value || !Convert.ToBoolean(r["IsFlat"])) continue;
                if (S(r["InvoicedDocNo"]).Trim().Length > 0) continue;   // billed this period already
                if (r.Table.Columns.Contains("IsWaive") && r["IsWaive"] != DBNull.Value && Convert.ToBoolean(r["IsWaive"]))
                {
                    // Waive contra: whether (and how much) it fires is decided at Generate from its
                    // Waive Configuration — the preview must show 0.00, never a positive amount.
                    r["CurrentReading"] = 0m;
                    r["MeterUsage"] = 0m;
                    r["TotalCharges"] = 0m;
                    r["EntrySource"] = "WAIVE-AUTO";
                    continue;
                }
                if (r["Locked"] != DBNull.Value && Convert.ToBoolean(r["Locked"])) continue;
                decimal rate = Dec(r["UnitPrice"]);
                decimal min = Dec(r["MinCharges"]);
                decimal foc = Dec(r["FOCQty"]);        // rental FOC Qty = free-rental months remaining
                decimal charge;
                int rentMonths = r.Table.Columns.Contains("RentalMonthsDue") && r["RentalMonthsDue"] != DBNull.Value
                    ? Convert.ToInt32(r["RentalMonthsDue"]) : 1;
                if (rentMonths > 1 && !IsCommitRow(r))
                {
                    // ATP-10: a machine that joined a prepaid contract after the bill that should have
                    // carried its first month pays two months here. Free months count one by one.
                    decimal one = rate < min ? min : rate;
                    int free = foc > 0m ? (int)Math.Min(Math.Floor(foc), rentMonths) : 0;
                    charge = one * (rentMonths - free);
                    r["EntrySource"] = charge == 0m ? "RENTAL FREE" : "RENTAL";
                }
                else if (foc > 0m)
                {
                    // Free-rental month: RM0 this period; the FOC-months counter is decremented at
                    // Generate (per the countdown model), then normal rental resumes at FOC = 0.
                    charge = 0m;
                    r["EntrySource"] = "RENTAL FREE";
                }
                else if (IsCommitRow(r))
                {
                    // A committed minimum bills the SHORTFALL, not the committed amount -- and what
                    // the shortfall is cannot be known until the print charges are in, at Generate.
                    // Previewing the full amount was the screen saying 300.00 where the invoice said
                    // 80.00, which reads as the software being wrong about the money.
                    charge = 0m;
                    r["EntrySource"] = "MIN-AUTO";
                }
                else
                {
                    charge = rate;                     // the flat rental amount (rate, or Min Charges)
                    if (charge < min) charge = min;
                    r["EntrySource"] = "RENTAL";
                }
                // Rental has NO meter reading — reading + usage stay 0 (still shown), the charge is the
                // flat rental / minimum payment. It bills as long as that amount > 0.
                r["CurrentReading"] = 0m;
                r["MeterUsage"] = 0m;
                r["TotalCharges"] = charge;
                // Rental period n/N ("MONTHLY RENTAL (3/12)") — replaces the blank slot in the meter
                // type name so the grid AND the invoice line both show the current period.
                // A rental billed in advance (ATP-10) names the months it pays for: "(2/36) NOV 2026".
                int toN = r.Table.Columns.Contains("RentalToN") && r["RentalToN"] != DBNull.Value
                    ? Convert.ToInt32(r["RentalToN"]) : 0;
                if (toN > 0 && r["RentalStartDate"] != DBNull.Value)
                    r["MeterTypeName"] = ServiceContractPhotocopier.Classes.ScpStrategy.ComposePrepaidRentalText(
                        S(r["MeterTypeName"]), Convert.ToDateTime(r["RentalStartDate"]),
                        Convert.ToInt32(r["RentalMonths"]), Convert.ToInt32(r["RentalFromN"]), toN);
                else if (r["RentalStartDate"] != DBNull.Value && Convert.ToInt32(r["RentalMonths"]) > 0)
                    r["MeterTypeName"] = ServiceContractPhotocopier.Classes.ScpStrategy.ComposeRentalPeriodText(
                        S(r["MeterTypeName"]), Convert.ToDateTime(r["RentalStartDate"]),
                        Convert.ToInt32(r["RentalMonths"]), 'A', year, month);
                // The same months in the run's own column, so the cycle shows on the screen and not
                // only on the invoice line (user, 28/9).
                if (IsRentRow(r) && r.Table.Columns.Contains("RentalFor"))
                    r["RentalFor"] = ServiceContractPhotocopier.Classes.ScpStrategy.RentalForWords(
                        r["RentalStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["RentalStartDate"]),
                        r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]),
                        r["RentalFromN"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalFromN"]),
                        toN, year, month);
            }

            // A rental already billed this period (or locked) is not recomposed above, but it still
            // says which of its months it billed -- worked out the same way from its start and its
            // contract's basis. (An old bill that caught up two months reads as its one month here;
            // the invoice line has both.)
            foreach (DataRow r in rows.Rows)
            {
                if (!IsRentRow(r) || !r.Table.Columns.Contains("RentalFor") || S(r["RentalFor"]).Length > 0) continue;
                DateTime? rs = r["RentalStartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["RentalStartDate"]);
                int nn = rs.HasValue
                    ? ServiceContractPhotocopier.Classes.ScpStrategy.RentalPeriodN(rs.Value, S(r["RentalBasis"]) == "P" ? 'P' : 'A', year, month)
                    : 0;
                r["RentalFor"] = ServiceContractPhotocopier.Classes.ScpStrategy.RentalForWords(
                    rs, r["RentalMonths"] == DBNull.Value ? 0 : Convert.ToInt32(r["RentalMonths"]), nn, nn, year, month);
            }
                }


        /// <summary>Recomputes one row: usage from the readings, then what it charges. The
        /// ladders come in as an argument because a rate can be a tier band rather than a
        /// number.</summary>
        public static void Recalc(DataRow r, Dictionary<string, List<decimal[]>> ladders,
                                  int year, int month)
        {
            MeterBillLine ln = new MeterBillLine();
            ln.IsFlat = r["IsFlat"] != DBNull.Value && Convert.ToBoolean(r["IsFlat"]);
            ln.Current = Dec(r["CurrentReading"]);
            ln.Last = Dec(r["LastReading"]);
            ln.Rate = Dec(r["UnitPrice"]);
            ln.MinCharges = Dec(r["MinCharges"]);
            ln.Foc = Dec(r["FOCQty"]);
            ln.RebatePct = Dec(r["RebatePct"]);
            ln.MultiPriceCode = S(r["MultiPriceCode"]);
            ln.FocResetCount = ServiceContractPhotocopier.Classes.ScpInvoiceJobs.FocResetCountFor(r, year, month);
            // As Generate sets it: it decides whether a rebate comes off as copies or off the amount,
            // and how the cents round. Left unset the list priced a 5% rebate on the old rule and
            // showed 3,709.94 for a line the invoice bills at 3,710.00.
            ln.NewMoneyRules = r.Table.Columns.Contains("NewMoneyRules") && r["NewMoneyRules"] != DBNull.Value
                               && Convert.ToBoolean(r["NewMoneyRules"]);
            ln.TierIncremental = r.Table.Columns.Contains("TierIncremental") && r["TierIncremental"] != DBNull.Value
                                 && Convert.ToBoolean(r["TierIncremental"]);
            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(ln, ladders);
            // Honour the user's "Use Min." tick (force the minimum charge) without clobbering their input.
            if (r["UseMin"] != DBNull.Value && Convert.ToBoolean(r["UseMin"])) ln.Charge = ln.MinCharges;
            r["MeterUsage"] = ln.Usage;
            r["TotalCharges"] = ln.Charge;
                }

        /// <summary>Is this row a committed minimum rather than something that is read?</summary>
        public static bool IsCommitRow(DataRow r)
        {
            return ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinRole(
                r.Table.Columns.Contains("Role") ? S(r["Role"]) : "",
                S(r["MeterType"]),
                r.Table.Columns.Contains("MinCharges") ? Dec(r["MinCharges"]) : 0m);
                }

        private static DataTable Query(DBSetting db, string sql, int seconds)
        {
            DataTable t = new DataTable();
            using (SqlConnection cn = new SqlConnection(db.ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandTimeout = seconds;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(t);
                }
            }
            return t;
        }

        private static string S(object o) { return (o == null || o == DBNull.Value) ? "" : o.ToString(); }
        private static decimal Dec(object o) { decimal d; return (o != null && o != DBNull.Value && decimal.TryParse(o.ToString(), out d)) ? d : 0m; }
        private static long D64(object o) { long l; return (o != null && o != DBNull.Value && long.TryParse(o.ToString(), out l)) ? l : 0L; }
    }
}
