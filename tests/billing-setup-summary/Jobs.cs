using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;

/// <summary>
/// How many invoices a contract really produces, down the path that produces them.
///
/// <para>The footer on Meters &amp; Pricing is a promise; this is the delivery. It runs the same two
/// calls both billing screens run -- <c>ScpBillingRows.ForContract</c> then
/// <c>ScpInvoiceJobs.Build</c> -- and counts the jobs. A job is an invoice.</para>
///
/// <para>Worth its own harness because the shapes harness answers a different question: that one
/// folds lines to check what the paper reads, and works out its own grouping on the way. This is
/// the code that decides which machine's charges land on which document.</para>
/// </summary>
internal static class BillingJobs
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

    private static int _fail;

    [STAThread]
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
        string contractNo = args.Length > 1 ? args[1] : "DEMO-3G";
        int year = args.Length > 2 ? int.Parse(args[2]) : 2026;
        int month = args.Length > 3 ? int.Parse(args[3]) : 9;

        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        Console.WriteLine();
        Console.WriteLine("   book       " + book + "   contract " + contractNo + "   period " + month + "/" + year);

        object o = db.ExecuteScalar(
            "SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'" +
            contractNo.Replace("'", "''") + "'");
        if (o == null || o == DBNull.Value) { Console.WriteLine("   no such contract"); return 2; }
        long ck = Convert.ToInt64(o);

        Dictionary<string, List<decimal[]>> ladders;
        DataTable rows = ScpBillingRows.ForContract(db, ck, year, month, out ladders);
        Say("rows the screen would show", rows.Rows.Count.ToString());

        // Select All, then nothing filtered out -- the state a person leaves the screen in before
        // pressing Generate. Build skips any row whose Sel is not ticked, so a harness that forgets
        // this one line reports "0 invoices" and blames the product.
        List<DataRow> visible = new List<DataRow>();
        foreach (DataRow r in rows.Rows) { r["Sel"] = true; visible.Add(r); }

        int alreadyInvoiced;
        Dictionary<long, string> snapshots;
        string blockTitle, blockMessage;
        // Keyed by the job key -- one entry per invoice.
        Dictionary<string, MeterInvoiceGenerator.InvoiceJob> jobs = ScpInvoiceJobs.Build(
            db, rows, visible, ladders, year, month, ScpBillingRows.GroupingMode(db),
            out alreadyInvoiced, out snapshots, out blockTitle, out blockMessage);

        if (jobs == null)
        {
            Console.WriteLine();
            Console.WriteLine("   REFUSED: " + blockTitle);
            Console.WriteLine("   " + blockMessage);
            Console.WriteLine();
            return 1;
        }

        Say("already invoiced", alreadyInvoiced.ToString());
        Console.WriteLine();
        decimal total = 0m, paper = 0m;
        int n = 0;
        foreach (KeyValuePair<string, MeterInvoiceGenerator.InvoiceJob> kv in jobs)
        {
            MeterInvoiceGenerator.InvoiceJob j = kv.Value;
            n++;
            decimal jt = 0m;
            foreach (MeterBillLine l in j.Lines) jt += l.Charge;
            total += jt;
            Console.WriteLine("   invoice " + n + "   " + j.Lines.Count + " line" +
                              (j.Lines.Count == 1 ? " " : "s") +
                              "   " + jt.ToString("n2").PadLeft(12) +
                              "   ref " + j.RefDocNo + "   " + j.Label);

            // What the PAPER says. The figures above are per machine, before anything is folded;
            // an invoice prints folded lines, and a folded line's total is not always the sum of
            // its parts -- one rate across the row means the line must multiply out. This is the
            // number to hold against the customer's PDF.
            List<ScpFoldedLine> folded = ScpInvoiceLayout.Fold(db, j.Lines);
            ScpInvoiceLayout.SortForPrint(folded);
            foreach (ScpFoldedLine row in folded)
            {
                paper += row.PrintAmount;
                Console.WriteLine("        " + row.Leader.MeterTypeCode.PadRight(8) +
                                  row.PrintQty.ToString("n0").PadLeft(10) +
                                  " @ " + row.PrintUnitPrice.ToString("n4").PadLeft(11) +
                                  " = " + row.PrintAmount.ToString("n2").PadLeft(11) +
                                  "   " + Short(ScpInvoiceBuilder.ComposeFoldedDescription(row, row.Leader.MeterTypeName)));
            }
        }
        Console.WriteLine();
        Say("invoices", jobs.Count.ToString());
        Say("total", total.ToString("n2"));
        Say("what the paper prints", paper.ToString("n2") +
            (paper == total ? "" : "   <- " + (paper - total).ToString("n2") + " against the per-machine sum"));

        int groups = Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(DISTINCT BillGroupCode) FROM dbo.zSCP2_Item " +
            " WHERE ContractKey = " + ck + " AND ISNULL(BillGroupCode,'') <> ''"));
        int ungrouped = Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.zSCP2_Item " +
            " WHERE ContractKey = " + ck + " AND ISNULL(BillGroupCode,'') = ''"));
        bool rentSep = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(RentalSeparateInvoice,'N') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)) == "Y";
        bool perMachine = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(BillingMode,'G') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)) == "S";
        int machines = Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = " + ck));

        // How many invoices SHOULD come out.
        //
        // One per bill group, plus one for the machines carrying no group. Rental-separate then
        // adds a second invoice per bucket -- but only for buckets that actually have a rental to
        // put on it. Doubling blindly was wrong the first time it met a real contract: Rompin's
        // library copier is on its own invoice and is not rented, so it makes one invoice, not two.
        int buckets = perMachine ? machines : groups + (ungrouped > 0 ? 1 : 0);
        if (buckets < 1) buckets = 1;
        int rentalBuckets = 0;
        if (rentSep)
            rentalBuckets = Convert.ToInt32(db.ExecuteScalar(
                perMachine
                ? "SELECT COUNT(DISTINCT i.ItemKey) FROM dbo.zSCP2_Item i " +
                  " JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey AND m.MeterRole = 'RENTAL' " +
                  " WHERE i.ContractKey = " + ck
                : "SELECT COUNT(DISTINCT ISNULL(i.BillGroupCode,'')) FROM dbo.zSCP2_Item i " +
                  " JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey AND m.MeterRole = 'RENTAL' " +
                  " WHERE i.ContractKey = " + ck));
        int want = buckets + rentalBuckets;

        Console.WriteLine();
        Check("the invoices match the bill groups (expected " + want + ")", jobs.Count == want);

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    /// <summary>A description short enough to sit beside the figures.</summary>
    private static string Short(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", " ").Replace("\n", "  ").Trim();
        while (s.IndexOf("   ") >= 0) s = s.Replace("   ", "  ");
        return s.Length <= 44 ? s : s.Substring(0, 44);
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(30) + value);
    }

    private static void Check(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what);
    }
}
