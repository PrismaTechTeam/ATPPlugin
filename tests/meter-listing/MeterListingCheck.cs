using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;

// Summary Sales Invoice Meter Listing, checked against what was actually billed.
internal static class Guard
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    private static int _fail;
    private static DBSetting _db;

    [STAThread]
    private static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs a)
        {
            string n = new AssemblyName(a.Name).Name;
            if (n == "ServiceContractPhotocopier") return Assembly.LoadFrom(PLUGIN);
            string p = Path.Combine(AC, n + ".dll");
            if (File.Exists(p)) return Assembly.LoadFrom(p);
            p = Path.Combine(Path.GetDirectoryName(PLUGIN), n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { Run(); } catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine(_fail == 0 ? "   ALL OK" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    private static void Expect(string what, bool ok) { Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what); if (!ok) _fail++; }
    private static decimal D(object v) { return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v); }
    private static string S(object v) { return v == null || v == DBNull.Value ? "" : Convert.ToString(v); }
    private static bool Off(decimal a, decimal b) { return Math.Abs(a - b) > 0.01m; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        _db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);
        new AutoCount.Authentication.UserSession(_db);
        if (!AutoCount.Authentication.UserSession.CurrentUserSession.Login("ADMIN", "ADMIN")) { Console.WriteLine("login failed"); return; }

        int year = 2026, month = 9;
        DataTable t = ScpMeterListing.Build(_db, year, month, "", "", "");
        Console.WriteLine("Sep 2026: " + t.Rows.Count + " rows");
        Console.WriteLine();

        // ---- 1. every machine row's NET arithmetic ------------------------------------------
        // The sheet has to be readable on its own: NET is what is left after the free copies AND
        // the rebate, and that is the figure the rate multiplies.
        int machines = 0, netBad = 0, clamped = 0;
        foreach (DataRow r in t.Rows)
        {
            if (Convert.ToBoolean(r["IsCombineRow"])) continue;
            machines++;
            decimal rawBK = D(r["CurrentTotalBK"]) - D(r["PreviousTotalBK"]) - D(r["FOCBK"]) - D(r["FixedRebateBKQty"]);
            decimal rawCL = D(r["CurrentTotalCL"]) - D(r["PreviousTotalCL"]) - D(r["FOCCL"]) - D(r["FixedRebateCLQty"]);
            if (rawBK < 0 || rawCL < 0) clamped++;
            if (Off(D(r["NetBK"]), rawBK < 0 ? 0m : rawBK) || Off(D(r["NetCL"]), rawCL < 0 ? 0m : rawCL))
            {
                netBad++;
                Console.WriteLine("      " + S(r["CSSI"]) + ": NET BK " + D(r["NetBK"]) + " but " +
                    D(r["CurrentTotalBK"]) + " - " + D(r["PreviousTotalBK"]) + " - " + D(r["FOCBK"]) +
                    " - " + D(r["FixedRebateBKQty"]) + " = " + rawBK);
            }
        }
        Expect("NET = current - previous - FOC - rebate on all " + machines + " machine rows", netBad == 0);
        if (clamped > 0) Console.WriteLine("      note: " + clamped + " row(s) would go negative and are shown as 0");

        // ---- 2. the sheet multiplies out on its own ------------------------------------------
        // No outside figure is allowed in here: whatever a customer can read off the row has to
        // produce the charge printed beside it.
        int rateBad = 0, rebated = 0;
        foreach (DataRow r in t.Rows)
        {
            if (Convert.ToBoolean(r["IsCombineRow"])) continue;
            if (D(r["FixedRebateBKQty"]) > 0m || D(r["FixedRebateCLQty"]) > 0m) rebated++;
            decimal wantBK = D(r["NetBK"]) * D(r["RateBK"]);
            decimal wantCL = D(r["NetCL"]) * D(r["RateCL"]);
            if (!Off(wantBK, D(r["BKCharges"])) && !Off(wantCL, D(r["CLCharges"]))) continue;
            rateBad++;
            if (rateBad <= 8)
                Console.WriteLine("      " + S(r["CSSI"]).PadRight(16) +
                    "BK " + D(r["NetBK"]) + " x " + D(r["RateBK"]) + " = " + wantBK.ToString("n2") +
                    ", listed " + D(r["BKCharges"]).ToString("n2") +
                    "   |   CL " + D(r["NetCL"]) + " x " + D(r["RateCL"]) + " = " + wantCL.ToString("n2") +
                    ", listed " + D(r["CLCharges"]).ToString("n2"));
        }
        Expect("charge = NET x rate, straight off the sheet", rateBad == 0);
        if (rebated > 0)
            Console.WriteLine("      (" + rebated + " row(s) carry a rebate, printed as the copies it comes to)");

        // ---- 3. every contract closes with a COMBINE row -------------------------------------
        Dictionary<string, decimal> metersOf = new Dictionary<string, decimal>();
        Dictionary<string, DataRow> combineOf = new Dictionary<string, DataRow>();
        foreach (DataRow r in t.Rows)
        {
            string ck = S(r["ContractNo"]);
            if (Convert.ToBoolean(r["IsCombineRow"])) { combineOf[ck] = r; continue; }
            if (!metersOf.ContainsKey(ck)) metersOf[ck] = 0m;
            metersOf[ck] += D(r["BKCharges"]) + D(r["CLCharges"]);
        }
        int missing = 0;
        foreach (string ck in metersOf.Keys) if (!combineOf.ContainsKey(ck)) missing++;
        Expect("every contract closes with its .C COMBINE row (" + combineOf.Count + " of " + metersOf.Count + ")", missing == 0);

        // ---- 4. the COMBINE row is a real total ----------------------------------------------
        Dictionary<string, decimal[]> sumOf = new Dictionary<string, decimal[]>();
        foreach (DataRow r in t.Rows)
        {
            if (Convert.ToBoolean(r["IsCombineRow"])) continue;
            string ck = S(r["ContractNo"]);
            if (!sumOf.ContainsKey(ck)) sumOf[ck] = new decimal[10];
            decimal[] a = sumOf[ck];
            a[0] += D(r["FOCBK"]); a[1] += D(r["FOCCL"]);
            a[2] += D(r["FixedRebateBKQty"]); a[3] += D(r["FixedRebateCLQty"]);
            a[4] += D(r["NetBK"]); a[5] += D(r["NetCL"]);
            a[6] += D(r["CurrentTotalBK"]); a[7] += D(r["CurrentTotalCL"]);
            a[8] += D(r["PreviousTotalBK"]); a[9] += D(r["PreviousTotalCL"]);
        }
        int grandBad = 0, noInv = 0, totalBad = 0;
        foreach (KeyValuePair<string, DataRow> kv in combineOf)
        {
            decimal meters = metersOf.ContainsKey(kv.Key) ? metersOf[kv.Key] : 0m;
            decimal want = meters + D(kv.Value["RentalAmt"]);
            if (Off(want, D(kv.Value["GrandTotal"])))
            {
                grandBad++;
                Console.WriteLine("      " + kv.Key + ": meters " + meters.ToString("n2") + " + rental " +
                    D(kv.Value["RentalAmt"]).ToString("n2") + " = " + want.ToString("n2") +
                    " but grand total says " + D(kv.Value["GrandTotal"]).ToString("n2"));
            }
            if (S(kv.Value["InvNo"]).Trim().Length == 0) noInv++;

            decimal[] a2 = sumOf.ContainsKey(kv.Key) ? sumOf[kv.Key] : new decimal[10];
            if (Off(a2[6], D(kv.Value["CurrentTotalBK"])) || Off(a2[7], D(kv.Value["CurrentTotalCL"])) ||
                Off(a2[8], D(kv.Value["PreviousTotalBK"])) || Off(a2[9], D(kv.Value["PreviousTotalCL"])) ||
                Off(a2[0], D(kv.Value["FOCBK"])) || Off(a2[1], D(kv.Value["FOCCL"])) ||
                Off(a2[2], D(kv.Value["FixedRebateBKQty"])) || Off(a2[3], D(kv.Value["FixedRebateCLQty"])) ||
                Off(a2[4], D(kv.Value["NetBK"])) || Off(a2[5], D(kv.Value["NetCL"])) ||
                Off(meters, D(kv.Value["BKCharges"]) + D(kv.Value["CLCharges"])))
            {
                totalBad++;
                Console.WriteLine("      " + kv.Key + ": .C totals do not match the machines above it -- " +
                    "NET BK " + D(kv.Value["NetBK"]) + " vs " + a2[4] + ", charges " +
                    (D(kv.Value["BKCharges"]) + D(kv.Value["CLCharges"])).ToString("n2") + " vs " + meters.ToString("n2"));
            }
        }
        Expect("COMBINE grand total = meters + rental", grandBad == 0);
        Expect("every COMBINE row names the invoice it came from", noInv == 0);
        Expect("the .C row totals the machines above it (readings, FOC, rebate, NET, charges)", totalBad == 0);

        // The .C row must also subtract down its own line, or it is just numbers in a row.
        int lineBad = 0;
        foreach (KeyValuePair<string, DataRow> kv in combineOf)
        {
            DataRow c = kv.Value;
            decimal downBK = D(c["CurrentTotalBK"]) - D(c["PreviousTotalBK"]) - D(c["FOCBK"]) - D(c["FixedRebateBKQty"]);
            decimal downCL = D(c["CurrentTotalCL"]) - D(c["PreviousTotalCL"]) - D(c["FOCCL"]) - D(c["FixedRebateCLQty"]);
            if (Off(downBK, D(c["NetBK"])) || Off(downCL, D(c["NetCL"])))
            {
                lineBad++;
                Console.WriteLine("      " + kv.Key + ": " + D(c["CurrentTotalBK"]) + " - " + D(c["PreviousTotalBK"]) +
                    " - " + D(c["FOCBK"]) + " - " + D(c["FixedRebateBKQty"]) + " = " + downBK +
                    " but NET BK says " + D(c["NetBK"]));
            }
        }
        Expect("the .C row subtracts down its own line to NET", lineBad == 0);

        // ---- 5. against the invoices actually in the book -------------------------------------
        Console.WriteLine();
        Console.WriteLine("   contract              listing       invoiced   invoice(s)");
        int moneyBad = 0;
        foreach (KeyValuePair<string, DataRow> kv in combineOf)
        {
            decimal listed = D(kv.Value["GrandTotal"]);
            decimal billed = InvoicedTotal(kv.Key, year, month);
            bool ok = !Off(listed, billed);
            if (!ok) moneyBad++;
            Console.WriteLine("   " + (ok ? "ok   " : "FAIL ") + kv.Key.PadRight(16) +
                listed.ToString("n2").PadLeft(12) + billed.ToString("n2").PadLeft(15) +
                "   " + S(kv.Value["InvNo"]));
        }
        Expect("the listing adds up to what the invoices really charged", moneyBad == 0);
    }

    /// <summary>The rebate percent the invoice actually used for this machine, from the log.</summary>
    private static decimal RebateOf(string cssi, int year, int month)
    {
        object o = _db.ExecuteScalar(
            "SELECT ISNULL(MAX(g.RebatePct),0) FROM dbo.zSCP2_MeterReadingLog g " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = g.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE g.Source = 'INVOICE' AND g.PeriodYear = " + year + " AND g.PeriodMonth = " + month +
            "   AND i.ServiceItemNo = N'" + cssi.Replace("'", "''") + "' " +
            "   AND EXISTS (SELECT 1 FROM dbo.IV iv WHERE RTRIM(iv.DocNo) = RTRIM(g.DocNo) " +
            "                 AND ISNULL(iv.Cancelled,'F') <> 'T')");
        return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
    }

    /// <summary>What this contract's invoices for the period really came to, before tax.</summary>
    private static decimal InvoicedTotal(string contractNo, int year, int month)
    {
        object o = _db.ExecuteScalar(
            "SELECT ISNULL(SUM(x.Amt),0) FROM (" +
            "  SELECT DISTINCT iv.DocKey, iv.NetTotal AS Amt " +
            "    FROM dbo.IV iv " +
            "   WHERE iv.DocKey IN (SELECT DISTINCT e.InvoicedDocKey " +
            "                         FROM dbo.zSCP2_MeterEntry e " +
            "                         JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "                         JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            "                         JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
            "                        WHERE c.ContractNo = N'" + contractNo.Replace("'", "''") + "' " +
            "                          AND e.PeriodYear = " + year + " AND e.PeriodMonth = " + month + " " +
            "                          AND e.InvoicedDocKey IS NOT NULL)) x");
        return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
    }
}
