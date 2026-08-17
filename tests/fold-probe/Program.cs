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
            Check("rows", rows.Count, 6);              // 3 rental + 2 BK + 1 CL
            var rentals = Where(rows, true);
            var meters = Where(rows, false);
            Check("rental rows", rentals.Count, 3);
            Check("rental units 8505", UnitsOf(rentals, "iR-ADV 8505"), 1m);
            Check("rental units C5550i", UnitsOf(rentals, "iR-ADV C5550i"), 1m);
            Check("rental units 4545i", UnitsOf(rentals, "iR-ADV 4545i"), 3m);
            Check("meter rows", meters.Count, 3);
            Check("BK heavy qty", QtyAt(meters, 0.019m), 60249m);
            Check("BK medium qty", QtyAt(meters, 0.0285m), 52860m);
            Check("CL qty", QtyAt(meters, 0.285m), 5693m);
            Check("BK medium machines", MembersAt(meters, 0.0285m), 5);
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
        bool ok = got.ToString() == want.ToString();
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
