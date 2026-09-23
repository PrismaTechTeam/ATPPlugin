using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;

/// <summary>
/// What a credit note gives back, checked against what the invoice actually charged.
///
/// <para>A correction credits copies. The question is what those copies were WORTH, and the old
/// answer -- copies x the rate in the meter's rate column -- is right only while the line is a
/// plain multiplication. A ladder charges the band; a free allowance charges nothing; a line
/// sitting on its minimum does not get cheaper at all. On the customer's own book a quarter of the
/// correctable invoice lines are one of those.</para>
///
/// <para>So this walks every billed line the book has logged and asks two things of each:</para>
/// <list type="number">
/// <item><description>Re-run the engine over the reading that WAS billed. It has to reproduce the
/// amount that was billed, or nothing downstream can be trusted.</description></item>
/// <item><description>Take some copies off and compare the two answers: the multiplication, and
/// what the line actually stops costing.</description></item>
/// </list>
/// </summary>
internal static class CnCredit
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

    private static int _fail;

    private static int Main(string[] args)
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
        try { return Run(args); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args)
    {
        string book = args.Length > 0 ? args[0] : "AED_ATPTEST";
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        Console.WriteLine();
        Console.WriteLine("   book   " + book);

        Dictionary<string, List<decimal[]>> ladders = ScpMultiPrice.LoadLadders(db);

        // Every billed line a correction could reach: metered (not flat), with money on it.
        DataTable t = db.GetDataTable(
            "SELECT l.LogKey, l.DocNo, l.ItemMeterKey, i.SerialNumber, m.MeterTypeCode, " +
            "       ISNULL(l.LastReading,0) AS LastReading, l.Reading, ISNULL(l.[Usage],0) AS Usage, " +
            "       ISNULL(l.UnitPrice,0) AS LoggedRate, ISNULL(l.MinCharges,0) AS MinCharges, " +
            "       ISNULL(l.FOCQty,0) AS FOCQty, ISNULL(m.FOCQty,0) AS CurrentFoc, ISNULL(l.RebatePct,0) AS RebatePct, l.Charge, " +
            "       ISNULL(l.PeriodYear,0) AS PeriodYear, ISNULL(l.PeriodMonth,0) AS PeriodMonth, " +
            "       ISNULL(c.FOCResetUnit,'M') AS FOCResetUnit, ISNULL(c.FOCResetN,0) AS FOCResetN, " +
            "       CASE WHEN pm.ItemMeterKey IS NOT NULL " +
            "            THEN '#' + CAST(m.ItemMeterKey AS varchar(20)) " +
            "            ELSE ISNULL(NULLIF(m.MeterMultiPriceCode,''), ISNULL(mt.MeterMultiPriceCode,'')) END AS MultiPriceCode " +
            "  FROM dbo.zSCP2_MeterReadingLog l " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = l.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            "  LEFT JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
            "  LEFT JOIN dbo.zSCP_MeterType mt ON mt.MeterTypeCode = m.MeterTypeCode " +
            "  LEFT JOIN (SELECT DISTINCT ItemMeterKey FROM dbo.zSCP2_ItemMeterPrice) pm " +
            "         ON pm.ItemMeterKey = m.ItemMeterKey " +
            " WHERE l.Source = 'INVOICE' AND l.Charge > 0 AND ISNULL(mt.IsFlatCharge,'N') = 'N' " +
            " ORDER BY l.LogKey", false);

        Console.WriteLine("   lines  " + t.Rows.Count + " billed metered lines in the log");
        Console.WriteLine();

        int derivable = 0, repriced = 0, differs = 0;
        decimal worstGap = 0m; string worstLine = "";
        decimal shortChanged = 0m, overCredited = 0m;

        foreach (DataColumn c in t.Columns) c.ReadOnly = false;
        foreach (DataRow r in t.Rows) Settle(r, ladders);

        foreach (DataRow r in t.Rows)
        {
            decimal billedCharge = Dec(r["Charge"]);
            decimal reading = Dec(r["Reading"]);
            decimal last = Dec(r["LastReading"]);
            decimal eff;

            // (1) the engine has to reproduce the invoice from the log alone
            decimal rebuilt = ChargeAt(r, ladders, reading, out eff);
            if (Math.Abs(rebuilt - billedCharge) > 0.01m) { repriced++; continue; }
            derivable++;

            // (2) give a tenth of the usage back and compare the two answers
            decimal usage = reading - last;
            if (usage <= 0m) continue;
            decimal give = Math.Floor(usage / 10m);
            if (give <= 0m) continue;
            decimal corrected = reading - give;

            decimal ignored;
            decimal after = ChargeAt(r, ladders, corrected, out ignored);
            decimal truth = billedCharge - after;
            if (truth < 0m) truth = 0m;
            decimal oldWay = Math.Round(give * Dec(r["LoggedRate"]), 2);

            decimal gap = Math.Round(oldWay - truth, 2);
            if (Math.Abs(gap) > 0.01m)
            {
                differs++;
                if (gap > 0m) overCredited += gap; else shortChanged += -gap;
                if (Math.Abs(gap) > Math.Abs(worstGap))
                {
                    worstGap = gap;
                    worstLine = Convert.ToString(r["DocNo"]) + "  " + Convert.ToString(r["SerialNumber"]) +
                                " " + Convert.ToString(r["MeterTypeCode"]) +
                                "   billed " + billedCharge.ToString("n2") +
                                ", give back " + give.ToString("n0") + " copies:" +
                                "  old " + oldWay.ToString("n2") + "   true " + truth.ToString("n2");
                }
            }
        }

        Say("lines the engine reproduces", derivable.ToString());
        Say("lines re-priced since (must be keyed)", repriced.ToString());
        Say("of the reproducible, credit differs", differs.ToString());
        Say("  old way gave back TOO MUCH, total", overCredited.ToString("n2"));
        Say("  old way gave back TOO LITTLE, total", shortChanged.ToString("n2"));
        Console.WriteLine();
        if (worstLine.Length > 0) Console.WriteLine("   worst   " + worstLine);
        Console.WriteLine();

        // ---------------------------------------------------------------- the story, as a test
        Line("the ladder line from the story");
        DataRow story = null;
        foreach (DataRow r in t.Rows)
            if (Convert.ToInt64(r["ItemMeterKey"]) == 7163 && Convert.ToString(r["DocNo"]) == "MR2608.0795")
            { story = r; break; }

        if (story == null)
        {
            Say("not in this book", "skipped");
        }
        else
        {
            decimal eff2;
            decimal rebuilt = ChargeAt(story, ladders, Dec(story["Reading"]), out eff2);
            Check("the engine reproduces the invoice", Math.Abs(rebuilt - Dec(story["Charge"])) <= 0.01m);
            Say("billed", Dec(story["Usage"]).ToString("n0") + " copies = " + Dec(story["Charge"]).ToString("n2"));
            Say("rate in the meter's rate column", Dec(story["LoggedRate"]).ToString("n4"));
            Say("rate the invoice actually charged", eff2.ToString("n4"));

            decimal ignored;
            decimal after = ChargeAt(story, ladders, Dec(story["Reading"]) - 200m, out ignored);
            decimal truth = Dec(story["Charge"]) - after;
            decimal oldWay = Math.Round(200m * Dec(story["LoggedRate"]), 2);
            Say("correct 800 -> 600, old way credits", oldWay.ToString("n2"));
            Say("                  and the truth is", truth.ToString("n2"));
            Check("the old way short-changed the customer", truth > oldWay);
            Check("600 copies costs 350.00", after == 350.00m);
            Check("so the credit is 140.00", truth == 140.00m);
        }

        // ---------------------------------------------------------------- the minimum floor
        Line("a line sitting on its minimum");
        MeterBillLine mn = new MeterBillLine();
        mn.Last = 0m; mn.Current = 5000m; mn.Rate = 0.0285m; mn.MinCharges = 300m;
        ScpInvoiceBuilder.ComputeCharge(mn, ladders);
        Check("5,000 copies at 0.0285 bills the 300.00 floor", mn.Charge == 300m && mn.UseMin);
        MeterBillLine mn2 = new MeterBillLine();
        mn2.Last = 0m; mn2.Current = 3000m; mn2.Rate = 0.0285m; mn2.MinCharges = 300m;
        ScpInvoiceBuilder.ComputeCharge(mn2, ladders);
        Check("3,000 copies bills the same 300.00", mn2.Charge == 300m);
        Say("old way would credit", Math.Round(2000m * 0.0285m, 2).ToString("n2"));
        Say("the line stops costing", (mn.Charge - mn2.Charge).ToString("n2"));
        Check("nothing to give back", mn.Charge - mn2.Charge == 0m);

        // ---------------------------------------------------------------- copies falling back into FOC
        Line("copies falling back inside the free allowance");
        MeterBillLine f1 = new MeterBillLine();
        f1.Last = 0m; f1.Current = 600m; f1.Rate = 0.10m; f1.Foc = 500m;
        ScpInvoiceBuilder.ComputeCharge(f1, ladders);
        Check("600 copies with 500 free bills 100 x 0.10", f1.Charge == 10.00m);
        MeterBillLine f2 = new MeterBillLine();
        f2.Last = 0m; f2.Current = 400m; f2.Rate = 0.10m; f2.Foc = 500m;
        ScpInvoiceBuilder.ComputeCharge(f2, ladders);
        Check("400 copies bills nothing", f2.Charge == 0m);
        Say("old way would credit", Math.Round(200m * 0.10m, 2).ToString("n2"));
        Say("the line stops costing", (f1.Charge - f2.Charge).ToString("n2"));
        Check("only the charged copies come back", f1.Charge - f2.Charge == 10.00m);

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    /// <summary>As MeterCN_Form does it (ATP-3): a ladder line billed before its free band became the
    /// meter's Free Qty logged the Free Qty column the ladder then ignored. When the logged figure does
    /// not rebuild the invoice, today's Free Qty is tried, and kept only if it reproduces it.</summary>
    private static void Settle(DataRow r, Dictionary<string, List<decimal[]>> ladders)
    {
        decimal eff;
        if (Math.Abs(ChargeAt(r, ladders, Dec(r["Reading"]), out eff) - Dec(r["Charge"])) <= 0.01m) return;
        if (Convert.ToString(r["MultiPriceCode"]).Length == 0 || Dec(r["CurrentFoc"]) == Dec(r["FOCQty"])) return;
        object logged = r["FOCQty"];
        r["FOCQty"] = Dec(r["CurrentFoc"]);
        if (Math.Abs(ChargeAt(r, ladders, Dec(r["Reading"]), out eff) - Dec(r["Charge"])) > 0.01m) r["FOCQty"] = logged;
    }

    private static decimal ChargeAt(DataRow r, Dictionary<string, List<decimal[]>> ladders,
                                    decimal reading, out decimal effUnitPrice)
    {
        MeterBillLine ln = new MeterBillLine();
        ln.IsFlat = false;
        ln.Last = Dec(r["LastReading"]);
        ln.Current = reading;
        ln.Rate = Dec(r["LoggedRate"]);
        ln.MinCharges = Dec(r["MinCharges"]);
        ln.Foc = Dec(r["FOCQty"]);
        ln.RebatePct = Dec(r["RebatePct"]);
        ln.MultiPriceCode = Convert.ToString(r["MultiPriceCode"]);
        int y = Convert.ToInt32(r["PeriodYear"]), mo = Convert.ToInt32(r["PeriodMonth"]);
        int days = (y > 0 && mo >= 1 && mo <= 12) ? DateTime.DaysInMonth(y, mo) : 30;
        ln.FocResetCount = ScpMultiPrice.FocResetCount(
            Convert.ToString(r["FOCResetUnit"]), Convert.ToInt32(r["FOCResetN"]), days);
        ScpInvoiceBuilder.ComputeCharge(ln, ladders);
        effUnitPrice = ln.EffUnitPrice;
        return ln.Charge;
    }

    private static decimal Dec(object v)
    {
        return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    }

    private static void Line(string title)
    {
        Console.WriteLine();
        Console.WriteLine("== " + title + " " + new string('=', Math.Max(3, 60 - title.Length)));
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(38) + value);
    }

    private static void Check(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what);
    }
}
