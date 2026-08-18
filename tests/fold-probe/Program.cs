using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Exercises ScpInvoiceLayout.Fold against the real Pasir Gudang, JPJ and MBJB numbers.
// A temp contract is created in AED_ATPTEST, pointed at a seeded Billing Format, folded, and
// deleted again.
static class FoldProbe
{
    const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    static int _fail = 0;

    static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
        {
            string n = new AssemblyName(a.Name).Name;
            if (n == "ServiceContractPhotocopier") return Assembly.LoadFrom(PLUGIN);
            string p = Path.Combine(AC, n + ".dll");
            if (File.Exists(p)) return Assembly.LoadFrom(p);
            p = Path.Combine(Path.GetDirectoryName(PLUGIN), n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine(_fail == 0 ? "\nALL CHECKS PASSED" : "\n" + _fail + " CHECK(S) FAILED");
        return _fail == 0 ? 0 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        var db = new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000,
            "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        long ck = NewContract(db, "ZZPROBE-PG", "2INV-RMODEL");   // rental per model, meters across model
        try
        {
            // Pasir Gudang MR2607.1106 / 1107 — six machines, four models, two rates.
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            // rentals: all FOC (0.00); 8505 alone, C5550i alone, three 4545i together
            lines.Add(Rental(ck, "iR-ADV 8505", "SWD00508", "HEAVY DUTY"));
            lines.Add(Rental(ck, "iR-ADV C5550i", "2JD01705", "MEDIUM DUTY"));
            lines.Add(Rental(ck, "iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY"));
            lines.Add(Rental(ck, "iR-ADV 4545i", "UMV05259", "MEDIUM DUTY"));
            lines.Add(Rental(ck, "iR-ADV 4545i", "UPB00820", "MEDIUM DUTY"));
            // BK: the 8505 at 0.019, everyone else at 0.0285
            lines.Add(Bk(ck, "iR-ADV 8505", "SWD00508", "HEAVY DUTY", 0.019m, 534659m, 594908m));
            lines.Add(Bk(ck, "iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", 0.0285m, 326760m, 355403m));
            lines.Add(Bk(ck, "iR-ADV 4545i", "YAJ01479", "MEDIUM DUTY", 0.0285m, 58218m, 68258m));
            lines.Add(Bk(ck, "iR-ADV 4545i", "UMV05259", "MEDIUM DUTY", 0.0285m, 38423m, 42472m));
            lines.Add(Bk(ck, "iR-ADV 4545i", "UPB00820", "MEDIUM DUTY", 0.0285m, 72143m, 79314m));
            lines.Add(Bk(ck, "iR-ADV C4535i", "UNV01325", "MEDIUM DUTY", 0.0285m, 21947m, 24904m));
            // CL: only the C5550i has colour usage
            lines.Add(Cl(ck, "iR-ADV C5550i", "2JD01705", "MEDIUM DUTY", 0.285m, 41172m, 46865m));

            var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Console.WriteLine("=== PASIR GUDANG (format 2INV-RMODEL: rental per model, meters across model) ===");
            Dump(rows);
            // MERGE MEANS ONE LINE. A merged row is not kept apart by its machines' prices --
            // it carries a price of its own, the way the legacy ".C" combine meter did.
            //
            // This is a DELIBERATE departure from Pasir Gudang's issued MR2607.1106, which prints
            // two black lines (60,249 at 0.019 and 52,860 at 0.0285) because Jean built two combine
            // meters. Under "merge, ignoring model" the six machines are one black line. The money
            // is preserved: the row prices itself at the rate that reproduces what the machines
            // actually cost.
            // Format 2INV-RMODEL is rental M (by model) and meter A (ignoring model), so:
            //   rental -> 3 rows, split by MODEL   1 x 8505, 1 x C5550i, 3 x 4545i
            //   meter  -> 1 BK over all six machines, 1 CL
            Check("rows", rows.Count, 5);
            var rentals = Where(rows, true);
            var meters = Where(rows, false);
            Check("rental rows (by model)", rentals.Count, 3);
            Check("rental units 8505", UnitsOf(rentals, "iR-ADV 8505"), 1m);
            Check("rental units C5550i", UnitsOf(rentals, "iR-ADV C5550i"), 1m);
            Check("rental units 4545i", UnitsOf(rentals, "iR-ADV 4545i"), 3m);
            Check("meter rows (1 BK + 1 CL)", meters.Count, 2);
            Check("BK qty (all six machines)", meters[0].BillCopies, 113109m);
            Check("BK machines", meters[0].Members.Count, 6);
            Check("CL qty", meters[1].BillCopies, 5693m);

            // One line cannot reproduce mixed rates to the cent. 60,249 at 0.019 plus 52,860 at
            // 0.0285 is 2,651.24; one row of 113,109 at the rate that averages them comes to
            // 2,651.27. Three cents is the price of the merge, and it is why the merged line's rate
            // is a contract decision rather than something derived -- once it is set, the amount is
            // simply quantity x that rate and nothing has drifted.
            decimal bkMoney = 0m;
            foreach (var m in meters[0].Members) bkMoney += m.Charge;
            Console.WriteLine(string.Format(
                "       merged BK: {0:n0} copies, machines cost {1:n2}, row states {2:n2}   " +
                "(rate x qty would give {3:n2})",
                meters[0].PrintQty, bkMoney, meters[0].PrintAmount,
                Math.Round(meters[0].PrintQty * meters[0].PrintUnitPrice, 2)));
            Check("BK money is exactly what the machines cost", meters[0].PrintAmount, Math.Round(bkMoney, 2));
        }
        finally { DropContract(db, ck); }

        long ck2 = NewContract(db, "ZZPROBE-JPJ", "1INV-RMODEL");  // rental per model, meters across model
        try
        {
            // JPJ MR2607.0227 — twelve machines, three models, ONE BK rate. The duty labels must not
            // split the BK line.
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            string[] m5 = { "4LS03017", "4LS03018", "4LS03019", "4LS03033", "4LS03010" };
            string[] m6 = { "4MP02932", "4MP02938", "4MP02939", "4MP02582", "4MP02949", "4MP02580" };
            lines.Add(Rental(ck2, "iR-ADV DX 8986", "28B01247", "HEAVY DUTY"));
            foreach (string s in m5) lines.Add(Rental(ck2, "IR ADV DX C3935I", s, "MEDIUM DUTY"));
            foreach (string s in m6) lines.Add(Rental(ck2, "IR ADV DX C3926I", s, "LIGHT DUTY"));
            lines.Add(Bk(ck2, "iR-ADV DX 8986", "28B01247", "HEAVY DUTY", 0.0285m, 532899m, 564973m));
            foreach (string s in m5) lines.Add(Bk(ck2, "IR ADV DX C3935I", s, "MEDIUM DUTY", 0.0285m, 100m, 200m));
            foreach (string s in m6) lines.Add(Bk(ck2, "IR ADV DX C3926I", s, "LIGHT DUTY", 0.0285m, 100m, 200m));

            var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Console.WriteLine("\n=== JPJ (labels HEAVY/MEDIUM/LIGHT, one BK rate) ===");
            Dump(rows);
            Check("JPJ rental rows", Where(rows, true).Count, 3);
            Check("JPJ rental units MEDIUM", UnitsOf(Where(rows, true), "IR ADV DX C3935I"), 5m);
            Check("JPJ rental units LIGHT", UnitsOf(Where(rows, true), "IR ADV DX C3926I"), 6m);
            Check("JPJ BK rows (labels must NOT split)", Where(rows, false).Count, 1);
            Check("JPJ BK machines", Where(rows, false)[0].Members.Count, 12);
        }
        finally { DropContract(db, ck2); }

        long ck3 = NewContract(db, "ZZPROBE-MBJB", "1INV-BYMODEL"); // both per model
        try
        {
            // MBJB — same rate everywhere, so only the MODEL can split the meter lines.
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            for (int i = 0; i < 2; i++) lines.Add(Bk(ck3, "iR-ADV 8105i", "A" + i, "", 0.020m, 0m, 100m));
            for (int i = 0; i < 3; i++) lines.Add(Bk(ck3, "iR-ADV C5170", "B" + i, "", 0.020m, 0m, 100m));
            for (int i = 0; i < 36; i++) lines.Add(Bk(ck3, "iR-ADV C5160", "C" + i, "", 0.020m, 0m, 100m));
            for (int i = 0; i < 11; i++) lines.Add(Bk(ck3, "iR-ADV C5150", "D" + i, "", 0.020m, 0m, 100m));

            var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Console.WriteLine("\n=== MBJB (52 machines, one rate, grouped by model) ===");
            Dump(rows);
            Check("MBJB BK rows", rows.Count, 4);
            Check("MBJB C5160 machines", MembersOfModel(rows, "iR-ADV C5160"), 36);
        }
        finally { DropContract(db, ck3); }

        MoneyChecks();
        ApplyFormatChecks(db);

        // A contract with NO format must behave exactly as before: rentals fold on the old rule,
        // usage never folds.
        long ck4 = NewContract(db, "ZZPROBE-LEGACY", "");
        try
        {
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            for (int i = 0; i < 3; i++) lines.Add(RentalPaid(ck4, "iR-ADV 4545i", "L" + i, 300m));
            for (int i = 0; i < 3; i++) lines.Add(Bk(ck4, "iR-ADV 4545i", "L" + i, "", 0.03m, 0m, 1000m));
            var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Console.WriteLine("\n=== LEGACY (no Billing Format) ===");
            Dump(rows);
            Check("legacy rental rows (folds, as today)", Where(rows, true).Count, 1);
            Check("legacy rental units", Where(rows, true)[0].Members.Count, 3);
            Check("legacy BK rows (never folds)", Where(rows, false).Count, 3);
        }
        finally { DropContract(db, ck4); }

        // ---- the group owns the rental price ----
        // Three machines the salesman priced one at a time -- 250, 300, 475 -- on a contract that
        // merges all rental into ONE line. Until someone prices the group, the merged line can only
        // average them. Once the contract states 350, the machines bill at 350 and the line is
        // exactly 3 x 350, with nothing to reconcile.
        long ck5 = NewContract(db, "ZZPROBE-RGP", "2INV-ALL");   // rental A: merge, ignoring model
        try
        {
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + ck5);
            db.ExecuteNonQuery("INSERT INTO dbo.zSCP2_ContractRentalPrice (ContractKey, GroupCode, UnitPrice) " +
                               "VALUES (" + ck5 + ", N'', 350)");

            var prices = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadForContracts(
                db, new List<long> { ck5 });
            var modes = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.LoadRentalModes(
                db, new List<long> { ck5 });
            Console.WriteLine(Environment.NewLine + "=== RENTAL GROUP PRICE (merge ignoring model, contract says 350) ===");
            Check("mode read back", modes[ck5].ToString(), "A");
            decimal? p = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.PriceFor(
                prices, ck5, 'A', "iR-ADV 4545i", "");
            Check("price for any model under mode A", p.HasValue ? p.Value : -1m, 350m);
            decimal? pm = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.PriceFor(
                prices, ck5, 'M', "iR-ADV 4545i", "");
            Check("mode M looks up BY MODEL, so this row does not answer", pm.HasValue, false);
            decimal? ps = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.PriceFor(
                prices, ck5, 'S', "", "");
            Check("no merge means no group price", ps.HasValue, false);

            // What the engine does with it: every rental meter in the group bills at the group price.
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            lines.Add(RentalPaid(ck5, "iR-ADV 4545i", "G1", 350m));
            lines.Add(RentalPaid(ck5, "iR-ADV C5550i", "G2", 350m));
            lines.Add(RentalPaid(ck5, "iR-ADV 8505", "G3", 350m));
            var rgrows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Dump(rgrows);
            var rg = Where(rgrows, true);
            Check("one rental line for the group", rg.Count, 1);
            Check("units", rg[0].PrintQty, 3m);
            Check("the group price is the printed price", rg[0].PrintUnitPrice, 350m);
            Check("amount is units x the group price, exactly", rg[0].PrintAmount, 1050m);
        }
        finally
        {
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + ck5);
            DropContract(db, ck5);
        }

        // ---- "these two models together, that one apart" ----
        // The shape no line mode can express: a C5335 and a C5665 on ONE rental line of 2 UNIT, and
        // four C1234 on a line of their own. "Merge by model" would give three lines; "merge,
        // ignoring model" would give one. Grouping the two says exactly what is meant.
        long ck6 = NewContract(db, "ZZPROBE-MIX", "1INV-BYMODEL");   // rental M, meter M
        try
        {
            var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
            var a1 = RentalPaid(ck6, "MODEL C5335", "M1", 300m); a1.MergeGroupCode = "PAIR";
            var a2 = RentalPaid(ck6, "MODEL C5665", "M2", 300m); a2.MergeGroupCode = "PAIR";
            lines.Add(a1); lines.Add(a2);
            for (int i = 0; i < 4; i++) lines.Add(RentalPaid(ck6, "MODEL C1234", "N" + i, 250m));
            // ...and their black meters follow the same grouping, at their own rates.
            var b1 = Bk(ck6, "MODEL C5335", "M1", "", 0.03m, 0m, 1000m); b1.MergeGroupCode = "PAIR";
            var b2 = Bk(ck6, "MODEL C5665", "M2", "", 0.03m, 0m, 2000m); b2.MergeGroupCode = "PAIR";
            lines.Add(b1); lines.Add(b2);
            for (int i = 0; i < 4; i++) lines.Add(Bk(ck6, "MODEL C1234", "N" + i, "", 0.03m, 0m, 500m));

            var mixrows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Console.WriteLine(Environment.NewLine + "=== MERGE TWO MODELS, KEEP THE THIRD APART (mode M) ===");
            Dump(mixrows);
            var mr = Where(mixrows, true);
            var mm = Where(mixrows, false);
            Check("rental lines (grouped pair + the C1234s)", mr.Count, 2);
            Check("the pair is 2 UNIT across two models", UnitsOf(mr, "MODEL C5335"), 2m);
            Check("the C1234s stay their own line of 4", UnitsOf(mr, "MODEL C1234"), 4m);
            Check("black follows the same grouping", mm.Count, 2);
            Check("the pair's black line covers both", mm[0].Members.Count + mm[1].Members.Count, 6);

            // Ungrouped, the same fleet is three lines -- one per model. The group is doing the work.
            a1.MergeGroupCode = ""; a2.MergeGroupCode = ""; b1.MergeGroupCode = ""; b2.MergeGroupCode = "";
            var plain = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.Fold(db, lines);
            Check("without the group it is one line per model again", Where(plain, true).Count, 3);
        }
        finally { DropContract(db, ck6); }
    }

    // Picking a format on a contract must land on the four columns the engine reads. This is the
    // "create a new contract the new way" path, checked at the data level.
    static void ApplyFormatChecks(AutoCount.Data.DBSetting db)
    {
        Console.WriteLine("\n=== APPLY FORMAT (what the contract screen writes) ===");
        string[,] cases = {
            // format          expected BillingMode / RentalSeparate / RentalLineMode / MeterLineMode
            { "2INV-RMODEL",  "G", "Y", "M", "A" },   // Pasir Gudang, Rompin, IPG Afzan
            { "1INV-BYMODEL", "G", "N", "M", "M" },   // MBJB
            { "PERMACHINE",   "S", "Y", "S", "S" },   // Tangkak
            { "2INV-EACH",    "G", "Y", "S", "S" },   // Kastam
        };
        for (int i = 0; i < cases.GetLength(0); i++)
        {
            string code = cases[i, 0];
            long ck = NewContract(db, "ZZPROBE-APPLY", "");
            try
            {
                var f = ServiceContractPhotocopier.Classes.ScpBillingFormat.Load(db, code);
                f.ApplyTo(db, ck);
                var t = db.GetDataTable("SELECT BillingFormatCode, BillingMode, RentalSeparateInvoice, " +
                                        "RentalLineMode, MeterLineMode FROM dbo.zSCP2_Contract " +
                                        "WHERE ContractKey = " + ck, false);
                var r = t.Rows[0];
                Check(code + " code", Convert.ToString(r["BillingFormatCode"]), code);
                Check(code + " BillingMode", Convert.ToString(r["BillingMode"]), cases[i, 1]);
                Check(code + " RentalSeparate", Convert.ToString(r["RentalSeparateInvoice"]), cases[i, 2]);
                Check(code + " RentalLineMode", Convert.ToString(r["RentalLineMode"]), cases[i, 3]);
                Check(code + " MeterLineMode", Convert.ToString(r["MeterLineMode"]), cases[i, 4]);
                Console.WriteLine("       \"" + f.Summary() + "\"");
            }
            finally { DropContract(db, ck); }
        }
    }

    // ComputeCharge against the issued invoices. No DB needed — these are pure arithmetic.
    static void MoneyChecks()
    {
        Console.WriteLine("\n=== MONEY (ComputeCharge vs the issued invoices) ===");

        // Tangkak MR2607.1417 line 2, machine 4WE00869: BK 111,651 - 101,028 = 10,623 gross,
        // FOC 5,000 -> 5,623, rebate floor(5,623 x 3%) = 168 -> bills qty 5,455 at 0.0285 = 155.47.
        var bk = Usage(10623m, 0.0285m, foc: 5000m, rebate: 3m, newRules: true);
        Check("Tangkak 4WE00869 rebate qty", bk.RebateQty, 168m);
        Check("Tangkak 4WE00869 billed qty", bk.BillCopies, 5455m);
        Check("Tangkak 4WE00869 charge", bk.Charge, 155.47m);

        // Same line, colour: 11,794 - 10,136 = 1,658 gross, FOC 500 -> 1,158,
        // rebate floor(1,158 x 3%) = 34 -> 1,124 at 0.285 = 320.34.
        var cl = Usage(1658m, 0.285m, foc: 500m, rebate: 3m, newRules: true);
        Check("Tangkak 4WE00869 CL rebate qty", cl.RebateQty, 34m);
        Check("Tangkak 4WE00869 CL billed qty", cl.BillCopies, 1124m);
        Check("Tangkak 4WE00869 CL charge", cl.Charge, 320.34m);

        // Tangkak MR2607.1416, machine 2GS03078: 32,220 - 30,280 = 1,940, no FOC,
        // rebate floor(1,940 x 3%) = 58 -> 1,882 at 0.0285 = 53.64.
        var t16 = Usage(1940m, 0.0285m, foc: 0m, rebate: 3m, newRules: true);
        Check("Tangkak 2GS03078 rebate qty", t16.RebateQty, 58m);
        Check("Tangkak 2GS03078 billed qty", t16.BillCopies, 1882m);
        Check("Tangkak 2GS03078 charge", t16.Charge, 53.64m);

        // The same meter under the OLD rules is what the engine bills today: qty 1,940 and 53.63.
        var t16old = Usage(1940m, 0.0285m, foc: 0m, rebate: 3m, newRules: false);
        Check("Tangkak 2GS03078 OLD billed qty", t16old.BillCopies, 1940m);
        Check("Tangkak 2GS03078 OLD charge (today)", t16old.Charge, 53.63m);

        // Pasir Gudang MR2607.1106 colour: 5,693 x 0.285 = 1,622.505, printed 1,622.51.
        // Banker's rounding gives 1,622.50.
        var pg = Usage(5693m, 0.285m, foc: 0m, rebate: 0m, newRules: true);
        Check("Pasir Gudang CL charge (away from zero)", pg.Charge, 1622.51m);
        var pgOld = Usage(5693m, 0.285m, foc: 0m, rebate: 0m, newRules: false);
        Check("Pasir Gudang CL charge OLD (to even)", pgOld.Charge, 1622.50m);

        // Rompin: colour read backwards (5,834 now, 5,920 before). Clamp to 0, still a line.
        var neg = Usage(-86m, 0.30m, foc: 0m, rebate: 0m, newRules: true);
        Check("Rompin CL negative usage clamped", neg.Usage, 0m);
        Check("Rompin CL negative charge", neg.Charge, 0m);

        // Tangkak MR2607.1413: 18,331 - 14,829 = 3,502, all inside the 5,000 FOC -> 0.00 invoice.
        var z = Usage(3502m, 0.029m, foc: 5000m, rebate: 0m, newRules: true);
        Check("Tangkak FOC-covered billed qty", z.BillCopies, 0m);
        Check("Tangkak FOC-covered charge", z.Charge, 0m);
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine Usage(
        decimal usage, decimal rate, decimal foc, decimal rebate, bool newRules)
    {
        var l = new ServiceContractPhotocopier.Classes.MeterBillLine();
        l.Last = 0m; l.Current = usage; l.Rate = rate; l.Foc = foc; l.RebatePct = rebate;
        l.NewMoneyRules = newRules; l.FocResetCount = 1;
        ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, null);
        return l;
    }

    // ----- helpers -----

    static List<ServiceContractPhotocopier.Classes.ScpFoldedLine> Where(
        List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows, bool rental)
    {
        var o = new List<ServiceContractPhotocopier.Classes.ScpFoldedLine>();
        foreach (var r in rows) if (r.Leader.IsRental == rental) o.Add(r);
        return o;
    }

    static decimal UnitsOf(List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows, string model)
    {
        foreach (var r in rows) if (r.Leader.ModelCode == model) return r.Units;
        return -1m;
    }

    static decimal QtyAt(List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows, decimal price)
    {
        foreach (var r in rows) if (r.Leader.EffUnitPrice == price) return r.BillCopies;
        return -1m;
    }

    static int MembersAt(List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows, decimal price)
    {
        foreach (var r in rows) if (r.Leader.EffUnitPrice == price) return r.Members.Count;
        return -1;
    }

    static int MembersOfModel(List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows, string model)
    {
        foreach (var r in rows) if (r.Leader.ModelCode == model) return r.Members.Count;
        return -1;
    }

    static void Dump(List<ServiceContractPhotocopier.Classes.ScpFoldedLine> rows)
    {
        foreach (var r in rows)
            Console.WriteLine(string.Format("  {0,-8} {1,-18} {2,-13} units={3,-3} qty={4,-9} price={5,-8} cur={6,-9} prev={7}",
                r.Leader.IsRental ? "RENTAL" : r.Leader.ColorLabel, r.Leader.ModelCode,
                r.Leader.LineGroupCode, r.Units, r.BillCopies, r.Leader.EffUnitPrice, r.Current, r.Last));
    }

    static void Check(string what, object got, object want)
    {
        // Compare decimals by value — 0 and 0.00 are the same amount, and scale is not the point here.
        bool ok = got is decimal && want is decimal
            ? (decimal)got == (decimal)want
            : got.ToString() == want.ToString();
        if (!ok) _fail++;
        Console.WriteLine((ok ? "  ok   " : "  FAIL ") + what + " = " + got + (ok ? "" : "  (expected " + want + ")"));
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine Base(long ck, string model, string serial, string label)
    {
        var l = new ServiceContractPhotocopier.Classes.MeterBillLine();
        l.ContractKey = ck; l.ModelCode = model; l.SerialNumber = serial; l.LineGroupCode = label;
        l.AuditDate = new DateTime(2026, 7, 24); l.LastDate = new DateTime(2026, 6, 24);
        return l;
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine Rental(long ck, string model, string serial, string label)
    {
        var l = Base(ck, model, serial, label);
        l.IsFlat = true; l.IsRental = true; l.MeterTypeCode = "RENTAL"; l.ACItemCode = "RENTAL";
        l.Charge = 0m; l.EffUnitPrice = 0m;   // FOC, like Pasir Gudang's rental invoice
        l.RentalMonths = 36; l.RentalStartDate = new DateTime(2025, 7, 1);
        return l;
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine RentalPaid(long ck, string model, string serial, decimal amt)
    {
        var l = Rental(ck, model, serial, "");
        l.Charge = amt; l.EffUnitPrice = amt;
        return l;
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine Meter(long ck, string model, string serial,
        string label, string role, decimal rate, decimal prev, decimal cur)
    {
        var l = Base(ck, model, serial, label);
        l.MeterTypeCode = role; l.ACItemCode = role; l.ColorLabel = role;
        l.Rate = rate; l.EffUnitPrice = rate;
        l.Last = prev; l.Current = cur;
        l.Usage = cur - prev; l.BillCopies = l.Usage; l.Charge = Math.Round(l.BillCopies * rate, 2);
        return l;
    }

    static ServiceContractPhotocopier.Classes.MeterBillLine Bk(long ck, string model, string serial,
        string label, decimal rate, decimal prev, decimal cur)
    { return Meter(ck, model, serial, label, "BK", rate, prev, cur); }

    static ServiceContractPhotocopier.Classes.MeterBillLine Cl(long ck, string model, string serial,
        string label, decimal rate, decimal prev, decimal cur)
    { return Meter(ck, model, serial, label, "CL", rate, prev, cur); }

    static long NewContract(AutoCount.Data.DBSetting db, string no, string formatCode)
    {
        db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Contract WHERE ContractNo = N'" + no + "'");
        string r = "A", m = "S";
        if (formatCode.Length > 0)
        {
            var t = db.GetDataTable("SELECT RentalLineMode, MeterLineMode FROM dbo.zSCP2_BillingFormat " +
                                    "WHERE FormatCode = N'" + formatCode + "'", false);
            r = Convert.ToString(t.Rows[0]["RentalLineMode"]);
            m = Convert.ToString(t.Rows[0]["MeterLineMode"]);
        }
        db.ExecuteNonQuery(
            "INSERT INTO dbo.zSCP2_Contract (ContractNo, DebtorCode, BillingFormatCode, RentalLineMode, MeterLineMode) " +
            "VALUES (N'" + no + "', N'ZZPROBE', N'" + formatCode + "', '" + r + "', '" + m + "')");
        return Convert.ToInt64(db.ExecuteScalar(
            "SELECT ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'" + no + "'"));
    }

    static void DropContract(AutoCount.Data.DBSetting db, long ck)
    {
        try { db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck); } catch { }
    }
}
