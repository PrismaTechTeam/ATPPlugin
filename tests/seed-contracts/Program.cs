using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Builds the twelve demo contracts DEMO-01 .. DEMO-12 in AED_ATPTEST -- one per billing-format
// preset, plus a legacy control with no format at all -- and the copier models they need.
//
// The point is that the twelve print DIFFERENTLY. A contract only proves its mode if the machines
// under it can tell the modes apart, so each set is shaped for the mode it demonstrates:
//   merge ACROSS model  needs several models SHARING one price/rate, or nothing merges
//   merge BY model      needs two models and more than one machine on one of them
//   per machine         needs enough machines that the row count is obviously different
// DEMO-05 carries two BK rates on purpose: two black lines is the Pasir Gudang shape and it is the
// only honest way to print it -- one row is one 'qty x price = amount', and two prices cannot share
// a row.
//
// Re-runnable: it deletes ONLY the DEMO-* contracts (and their machines/meters, by cascade) and
// rebuilds them. Stock items that already exist are left exactly as they are.
static class SeedContracts
{
    const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";

    static string _conn = "";
    static string _debtor = "";
    static int _itemsMade = 0, _itemsKept = 0;
    static int _ctMade = 0, _machMade = 0, _meterMade = 0;
    static readonly Dictionary<string, string> _meterDesc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    static readonly DateTime START = new DateTime(2026, 1, 1);
    static readonly DateTime EXPIRY = new DateTime(2028, 12, 31);

    static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
        {
            string n = new AssemblyName(a.Name).Name;
            string p = Path.Combine(AC, n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine();
        Console.WriteLine("Models   : " + _itemsMade + " created, " + _itemsKept + " already there.");
        Console.WriteLine("Contracts: " + _ctMade + " rebuilt, " + _machMade + " machines, " + _meterMade + " meters.");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        AutoCount.Data.DBSetting db = new AutoCount.Data.DBSetting(
            AutoCount.Data.DBServerType.SQL2000,
            "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        AutoCount.Authentication.UserSession ses = new AutoCount.Authentication.UserSession(db);
        if (!AutoCount.Authentication.UserSession.CurrentUserSession.Login("ADMIN", "ADMIN"))
            throw new Exception("AutoCount login failed.");
        _conn = db.ConnectionString;
        Console.WriteLine("Logged in to AED_ATPTEST.");

        Console.WriteLine();
        Console.WriteLine("== Part 1: copier models ==");
        SeedModels(db, ses);

        Console.WriteLine();
        Console.WriteLine("== Part 2: demo contracts ==");
        CheckMeterTypes();
        _debtor = PickDebtor();
        Console.WriteLine("  debtor " + _debtor);
        Wipe();
        BuildAll();
    }

    // ------------------------------------------------------------------ Part 1: models

    static void SeedModels(AutoCount.Data.DBSetting db, AutoCount.Authentication.UserSession ses)
    {
        // Twelve more, chosen so every scenario below has a model of the right character to hand:
        // light/medium/heavy colour for the by-model splits, mono for the machines that carry no
        // colour meter, and a production press for the single-unit line.
        Model(db, ses, "iR-ADV C3560i", "COPIER iR-ADV C3560i - COLOUR A3 LIGHT DUTY");
        Model(db, ses, "iR-ADV DX C3940i", "COPIER iR-ADV DX C3940i - COLOUR A3 MEDIUM DUTY");
        Model(db, ses, "imageFORCE C7165", "COPIER imageFORCE C7165 - COLOUR A3 HEAVY DUTY");
        Model(db, ses, "iR-ADV DX 6980i", "COPIER iR-ADV DX 6980i - MONO A3 HEAVY DUTY");
        Model(db, ses, "iR-ADV DX C5760i", "COPIER iR-ADV DX C5760i - COLOUR A3 HEAVY DUTY");
        Model(db, ses, "iR-ADV 6580i", "COPIER iR-ADV 6580i - MONO A3 HEAVY DUTY");
        Model(db, ses, "iR-ADV DX 4960i", "COPIER iR-ADV DX 4960i - MONO A3 MEDIUM DUTY");
        Model(db, ses, "iR-ADV DX C5880i", "COPIER iR-ADV DX C5880i - COLOUR A3 HEAVY DUTY");
        Model(db, ses, "imageFORCE 6170", "COPIER imageFORCE 6170 - MONO A3 HEAVY DUTY");
        Model(db, ses, "imagePRESS C910", "COPIER imagePRESS C910 - COLOUR A3 PRODUCTION");
        Model(db, ses, "iR-ADV C7565i", "COPIER iR-ADV C7565i - COLOUR A3 HEAVY DUTY");
        Model(db, ses, "iR-ADV 2745i", "COPIER iR-ADV 2745i - MONO A4 LIGHT DUTY");
    }

    // Same shape as the book's existing copiers: group C001, type H001, one UNIT, serialised, not
    // stock-controlled -- a rented machine is tracked by its contract, not by a stock balance.
    static void Model(AutoCount.Data.DBSetting db, AutoCount.Authentication.UserSession ses,
        string code, string desc)
    {
        object exists = db.ExecuteScalar(
            "SELECT ItemCode FROM dbo.Item WHERE ItemCode = N'" + code.Replace("'", "''") + "'");
        if (exists != null && exists != DBNull.Value)
        {
            Console.WriteLine("  kept   " + code);
            _itemsKept++;
            return;
        }

        AutoCount.Stock.Item.ItemDataAccess da = AutoCount.Stock.Item.ItemDataAccess.Create(ses, db);
        AutoCount.Stock.Item.ItemEntity it = da.NewItem();
        it.ItemCode = code;
        it.Description = desc;
        it.ItemGroup = "C001";
        it.ItemType = "H001";
        // NewItem() already comes with one blank UOM row -- name THAT one rather than adding a
        // second, or the item ships with a stray '' UOM next to its real one.
        if (it.UomCount > 0) it.GetUom(0).Uom = "UNIT";
        else it.NewUom("UNIT", 1m);
        it.BaseUom = "UNIT";
        it.SalesUom = "UNIT";
        it.PurchaseUom = "UNIT";
        it.ReportUom = "UNIT";
        it.StockControl = false;
        it.HasSerialNo = true;
        it.IsActive = true;
        da.SaveData(it, "ADMIN");
        Console.WriteLine("  new    " + code + "   " + desc);
        _itemsMade++;
    }

    // ------------------------------------------------------------------ small model of the data

    class Meter
    {
        public string Type = "";      // zSCP_MeterType.MeterTypeCode
        public string Role = "NA";    // RENTAL / BK / CL / COMMIT / WAIVE -- blank blocks the save
        public decimal Rate = 0m;     // ChargesRate: rental money per month, or price per copy
        public decimal Min = 0m;      // MinimumCharges: the committed amount, or the NEGATIVE waive
        public decimal Initial = 0m;
        public string CommitScope = "S";
        public string WaiveScope = "BKCL";
        public int WaiveFirstN = 0;
        public decimal WaiveTarget = 0m, WaivePartialThreshold = 0m, WaivePartialAmount = 0m;
        public int RentalMonths = 0;
        public string RentalBasis = "A";
    }

    class Mach
    {
        public string Model = "";
        public string Serial = "";
        public string MergeGroup = "";
        public string MachineMode = "";
        public List<Meter> Meters = new List<Meter>();

        public Mach Rent(decimal monthly)
        {
            Meter m = new Meter();
            m.Type = "RENTAL"; m.Role = "RENTAL"; m.Rate = monthly;
            m.RentalMonths = 36; m.RentalBasis = "A";
            Meters.Add(m); return this;
        }
        public Mach Bk(decimal rate, decimal initial)
        {
            Meter m = new Meter();
            m.Type = "BK"; m.Role = "BK"; m.Rate = rate; m.Initial = initial;
            Meters.Add(m); return this;
        }
        public Mach Cl(decimal rate, decimal initial)
        {
            Meter m = new Meter();
            m.Type = "CL"; m.Role = "CL"; m.Rate = rate; m.Initial = initial;
            Meters.Add(m); return this;
        }
        // The committed amount lives in MinimumCharges -- that is what the engine reads, and a
        // minimum of nothing is not one.
        public Mach Commit(decimal amount, string scope)
        {
            Meter m = new Meter();
            m.Type = "COMMIT"; m.Role = "COMMIT"; m.Min = amount; m.CommitScope = scope;
            Meters.Add(m); return this;
        }
        // The contra is stored NEGATIVE -- it takes the rent away.
        public Mach Waive(decimal amount, int firstN, decimal target, string scope)
        {
            Meter m = new Meter();
            m.Type = "WAIVE"; m.Role = "WAIVE"; m.Min = -Math.Abs(amount);
            m.WaiveFirstN = firstN; m.WaiveTarget = target; m.WaiveScope = scope;
            Meters.Add(m); return this;
        }
        public Mach Group(string g) { MergeGroup = g; return this; }
        public Mach Online() { MachineMode = "ONLINE"; return this; }
        public Mach Offline() { MachineMode = "OFFLINE"; return this; }
    }

    class Ct
    {
        public string No = "";
        public string Format = "";        // '' = the legacy control
        public string Desc = "";
        public string Prints = "";        // what the invoice should come out as, for the console
        public List<Mach> Machines = new List<Mach>();
    }

    static Mach M(string model, string serial) { Mach m = new Mach(); m.Model = model; m.Serial = serial; return m; }

    // ------------------------------------------------------------------ the twelve

    static List<Ct> Plan()
    {
        List<Ct> all = new List<Ct>();

        // -- Mode 1 -- one invoice, rental merged ignoring model, BK+CL merged ignoring model.
        // Five machines over three models all at 450 and all at one rate, so everything folds.
        Ct c1 = New("DEMO-01", "1INV-ALL",
            "Mode 1 - one invoice; rental one line across models; BK and CL one line each",
            "1 INV: rental 5 UNIT x 450.00, one BK line, one CL line = 3 lines");
        c1.Machines.Add(M("iR-ADV C3560i", "DEMO01-001").Rent(450m).Bk(0.0250m, 118400m).Cl(0.2500m, 41200m).Online());
        c1.Machines.Add(M("iR-ADV C3560i", "DEMO01-002").Rent(450m).Bk(0.0250m, 96350m).Cl(0.2500m, 33780m).Online());
        c1.Machines.Add(M("iR-ADV DX C3940i", "DEMO01-003").Rent(450m).Bk(0.0250m, 205110m).Cl(0.2500m, 62940m));
        c1.Machines.Add(M("iR-ADV DX C3940i", "DEMO01-004").Rent(450m).Bk(0.0250m, 187620m).Cl(0.2500m, 58015m));
        c1.Machines.Add(M("imageFORCE C7165", "DEMO01-005").Rent(450m).Bk(0.0250m, 342800m).Cl(0.2500m, 129440m).Offline());
        all.Add(c1);

        // -- Mode 2 -- the JPJ shape. Three models at three different rentals, so rental splits
        // three ways; every black meter shares 0.0285, so the black line stays ONE across all three.
        Ct c2 = New("DEMO-02", "1INV-RMODEL",
            "Mode 2 - one invoice; rental one line per model; BK and CL one line each",
            "1 INV: 3 rental lines (1 / 2 / 3 UNIT), one BK line across 3 models, one CL line = 5 lines");
        c2.Machines.Add(M("iR-ADV DX 6980i", "DEMO02-001").Rent(1287.25m).Bk(0.0285m, 1204380m));
        c2.Machines.Add(M("iR-ADV DX C5760i", "DEMO02-002").Rent(655.50m).Bk(0.0285m, 388120m).Cl(0.2850m, 96470m));
        c2.Machines.Add(M("iR-ADV DX C5760i", "DEMO02-003").Rent(655.50m).Bk(0.0285m, 355940m).Cl(0.2850m, 88210m));
        c2.Machines.Add(M("iR-ADV C3560i", "DEMO02-004").Rent(476.90m).Bk(0.0285m, 142650m).Cl(0.2850m, 39880m));
        c2.Machines.Add(M("iR-ADV C3560i", "DEMO02-005").Rent(476.90m).Bk(0.0285m, 133200m).Cl(0.2850m, 36540m));
        c2.Machines.Add(M("iR-ADV C3560i", "DEMO02-006").Rent(476.90m).Bk(0.0285m, 151070m).Cl(0.2850m, 44120m));
        all.Add(c2);

        // -- Mode 3 -- one invoice, one rental line, but every machine's usage is read separately.
        // The per-copy prices differ on purpose: nothing here could have merged anyway.
        Ct c3 = New("DEMO-03", "1INV-MMACHINE",
            "Mode 3 - one invoice; rental one line across models; BK and CL per machine",
            "1 INV: rental 4 UNIT x 520.00, then BK+CL for machines 1-2 and BK for 3-4 = 7 lines");
        c3.Machines.Add(M("iR-ADV DX C3940i", "DEMO03-001").Rent(520m).Bk(0.0240m, 88420m).Cl(0.2400m, 27310m));
        c3.Machines.Add(M("iR-ADV DX C3940i", "DEMO03-002").Rent(520m).Bk(0.0250m, 74180m).Cl(0.2500m, 22940m));
        c3.Machines.Add(M("iR-ADV DX 4960i", "DEMO03-003").Rent(520m).Bk(0.0220m, 512300m));
        c3.Machines.Add(M("iR-ADV DX 4960i", "DEMO03-004").Rent(520m).Bk(0.0230m, 466150m));
        all.Add(c3);

        // -- Mode 4 -- the MBJB shape. Every black meter is 0.0200 and every colour meter 0.2800, so
        // the rate cannot be what splits the meter lines: the MODEL is.
        Ct c4 = New("DEMO-04", "1INV-BYMODEL",
            "Mode 4 - one invoice; rental one line per model; BK and CL one line per model",
            "1 INV: 3 rental lines + 3 BK lines + 3 CL lines = 9 lines, all BK at one 0.0200 rate");
        c4.Machines.Add(M("iR-ADV DX C5880i", "DEMO04-001").Rent(880m).Bk(0.0200m, 244300m).Cl(0.2800m, 71250m));
        c4.Machines.Add(M("iR-ADV DX C5880i", "DEMO04-002").Rent(880m).Bk(0.0200m, 231770m).Cl(0.2800m, 65480m));
        c4.Machines.Add(M("iR-ADV DX C5880i", "DEMO04-003").Rent(880m).Bk(0.0200m, 259010m).Cl(0.2800m, 77930m));
        c4.Machines.Add(M("iR-ADV C7565i", "DEMO04-004").Rent(720m).Bk(0.0200m, 174220m).Cl(0.2800m, 52880m));
        c4.Machines.Add(M("iR-ADV C7565i", "DEMO04-005").Rent(720m).Bk(0.0200m, 168940m).Cl(0.2800m, 48170m));
        c4.Machines.Add(M("imagePRESS C910", "DEMO04-006").Rent(1150m).Bk(0.0200m, 402660m).Cl(0.2800m, 188420m));
        all.Add(c4);

        // -- Mode 5 -- the Pasir Gudang shape, and the contract that proves TWO black lines are
        // correct. One heavy machine is priced 0.0190 and everything else 0.0285: a row is one
        // 'qty x price = amount', so two prices are two rows, however hard the mode merges.
        // MergeGroupCode carries the old duty labels; here they agree with what the money already says.
        Ct c5 = New("DEMO-05", "2INV-ALL",
            "Mode 5 - rental invoice + meter invoice; rental one line across models; BK and CL one line each",
            "INV1: 3 rental lines (1200 / 620 x4 / 480). INV2: TWO BK lines (0.0190 x1, 0.0285 x5 over 3 models) + 1 CL line");
        c5.Machines.Add(M("iR-ADV 6580i", "DEMO05-001").Group("HEAVY").Rent(1200m).Bk(0.0190m, 1806420m));
        c5.Machines.Add(M("iR-ADV DX 4960i", "DEMO05-002").Group("MEDIUM").Rent(620m).Bk(0.0285m, 604180m));
        c5.Machines.Add(M("iR-ADV DX 4960i", "DEMO05-003").Group("MEDIUM").Rent(620m).Bk(0.0285m, 588700m));
        c5.Machines.Add(M("iR-ADV DX 4960i", "DEMO05-004").Group("MEDIUM").Rent(620m).Bk(0.0285m, 561330m));
        c5.Machines.Add(M("iR-ADV DX C5760i", "DEMO05-005").Group("MEDIUM").Rent(620m).Bk(0.0285m, 318640m).Cl(0.2850m, 92110m));
        c5.Machines.Add(M("iR-ADV DX C3940i", "DEMO05-006").Group("MEDIUM").Rent(480m).Bk(0.0285m, 205480m).Cl(0.2850m, 60350m));
        all.Add(c5);

        // -- Mode 6 -- the ROMPIN shape, and where the rental waive lives. Two models, two rentals,
        // one shared meter rate; machine 5 has the rent given back for its first three months.
        Ct c6 = New("DEMO-06", "2INV-RMODEL",
            "Mode 6 - rental invoice + meter invoice; rental one line per model; BK and CL one line each",
            "INV1: 2 rental lines (3 UNIT x 495.00, 2 UNIT x 815.00) + a WAIVE contra for DEMO06-005. INV2: 1 BK + 1 CL");
        c6.Machines.Add(M("iR-ADV C3560i", "DEMO06-001").Rent(495m).Bk(0.0250m, 121400m).Cl(0.2500m, 35620m));
        c6.Machines.Add(M("iR-ADV C3560i", "DEMO06-002").Rent(495m).Bk(0.0250m, 114870m).Cl(0.2500m, 31940m));
        c6.Machines.Add(M("iR-ADV C3560i", "DEMO06-003").Rent(495m).Bk(0.0250m, 132250m).Cl(0.2500m, 40110m));
        c6.Machines.Add(M("iR-ADV DX C5760i", "DEMO06-004").Rent(815m).Bk(0.0250m, 397520m).Cl(0.2500m, 101880m));
        c6.Machines.Add(M("iR-ADV DX C5760i", "DEMO06-005").Rent(815m).Bk(0.0250m, 374060m).Cl(0.2500m, 96430m)
                          .Waive(815m, 3, 2445m, "BKCL"));
        all.Add(c6);

        // -- Mode 7 -- the F0019 shape. Five machines over three models on ONE agreed rental price,
        // so the rental invoice is a single line; the meter invoice reads every machine on its own.
        Ct c7 = New("DEMO-07", "2INV-MMACHINE",
            "Mode 7 - rental invoice + meter invoice; rental one line across models; BK and CL per machine",
            "INV1: rental 5 UNIT x 385.00 = 1 line. INV2: BK+CL per machine = 10 lines");
        c7.Machines.Add(M("iR-ADV C3560i", "DEMO07-001").Rent(385m).Bk(0.0260m, 92140m).Cl(0.2600m, 26480m));
        c7.Machines.Add(M("iR-ADV DX C3940i", "DEMO07-002").Rent(385m).Bk(0.0260m, 143870m).Cl(0.2600m, 41250m));
        c7.Machines.Add(M("iR-ADV DX C3940i", "DEMO07-003").Rent(385m).Bk(0.0260m, 156320m).Cl(0.2600m, 44980m));
        c7.Machines.Add(M("iR-ADV C5335", "DEMO07-004").Rent(385m).Bk(0.0260m, 210450m).Cl(0.2600m, 63710m));
        c7.Machines.Add(M("iR-ADV C5665", "DEMO07-005").Rent(385m).Bk(0.0260m, 188930m).Cl(0.2600m, 57120m));
        all.Add(c7);

        // -- Mode 8 -- the KEJORA / MARA shape, and where the committed minimum lives. The COMMIT
        // meter tops up one machine to 500.00 and always prints on its own line: merging it would
        // throw away the explanation.
        Ct c8 = New("DEMO-08", "2INV-RMODEL-MM",
            "Mode 8 - rental invoice + meter invoice; rental one line per model; BK and CL per machine",
            "INV1: 2 rental lines (2 UNIT x 950.00, 3 UNIT x 610.00). INV2: BK+CL per machine = 10 lines + a COMMIT top-up for DEMO08-001");
        c8.Machines.Add(M("imageFORCE C5150", "DEMO08-001").Rent(950m).Bk(0.0300m, 268410m).Cl(0.3000m, 82340m)
                          .Commit(500m, "S"));
        c8.Machines.Add(M("imageFORCE C5150", "DEMO08-002").Rent(950m).Bk(0.0300m, 247180m).Cl(0.3000m, 75920m));
        c8.Machines.Add(M("iR-ADV DX C3922i", "DEMO08-003").Rent(610m).Bk(0.0280m, 118640m).Cl(0.2800m, 34870m));
        c8.Machines.Add(M("iR-ADV DX C3922i", "DEMO08-004").Rent(610m).Bk(0.0280m, 126950m).Cl(0.2800m, 38210m));
        c8.Machines.Add(M("iR-ADV DX C3922i", "DEMO08-005").Rent(610m).Bk(0.0280m, 109730m).Cl(0.2800m, 31460m));
        all.Add(c8);

        // -- Mode 9 -- the KASTAM shape. Nothing merges at all, and the four different rentals make
        // that visible even to a mode that wanted to.
        Ct c9 = New("DEMO-09", "2INV-EACH",
            "Mode 9 - rental invoice + meter invoice; rental per machine; BK and CL per machine",
            "INV1: 4 rental lines. INV2: 5 meter lines (BK on all four, CL only on the colour one)");
        c9.Machines.Add(M("iR-ADV DX 4951i", "DEMO09-001").Rent(705m).Bk(0.0210m, 733640m));
        c9.Machines.Add(M("iR-ADV DX 4951i", "DEMO09-002").Rent(690m).Bk(0.0210m, 688210m));
        c9.Machines.Add(M("iR-ADV 2745i", "DEMO09-003").Rent(250m).Bk(0.0300m, 84920m));
        c9.Machines.Add(M("iR-ADV C1234", "DEMO09-004").Rent(430m).Bk(0.0250m, 121780m).Cl(0.2500m, 37450m));
        all.Add(c9);

        // -- Mode 10 -- the KENSINGTON shape, the mirror of mode 3: every rental named separately,
        // every meter folded. Five different rentals, one shared 0.0220 / 0.2200.
        Ct c10 = New("DEMO-10", "2INV-RMACHINE",
            "Mode 10 - rental invoice + meter invoice; rental per machine; BK and CL one line each",
            "INV1: 5 rental lines, all different money. INV2: 1 BK line + 1 CL line across 5 machines / 5 models");
        c10.Machines.Add(M("iR-ADV C3560i", "DEMO10-001").Rent(512m).Bk(0.0220m, 96140m).Cl(0.2200m, 28470m));
        c10.Machines.Add(M("iR-ADV DX C3940i", "DEMO10-002").Rent(545m).Bk(0.0220m, 137850m).Cl(0.2200m, 39620m));
        c10.Machines.Add(M("imageFORCE C7165", "DEMO10-003").Rent(1180m).Bk(0.0220m, 388270m).Cl(0.2200m, 142310m));
        c10.Machines.Add(M("iR-ADV C5665", "DEMO10-004").Rent(600m).Bk(0.0220m, 204930m).Cl(0.2200m, 61840m));
        c10.Machines.Add(M("iR-ADV C5335", "DEMO10-005").Rent(575m).Bk(0.0220m, 176520m).Cl(0.2200m, 53190m));
        all.Add(c10);

        // -- Mode 11 -- the TANGKAK shape: every machine is its own pair of invoices, and the machine
        // with no rental meter simply produces no rental invoice. Three machines, five invoices.
        Ct c11 = New("DEMO-11", "PERMACHINE",
            "Mode 11 - one rental invoice and one meter invoice per machine",
            "5 invoices from 3 machines: 2 rental (DEMO11-003 has no rental meter) + 3 meter");
        c11.Machines.Add(M("imageFORCE 6170", "DEMO11-001").Rent(990m).Bk(0.0195m, 1442800m));
        c11.Machines.Add(M("iR-ADV DX 4960i", "DEMO11-002").Rent(640m).Bk(0.0250m, 526170m));
        c11.Machines.Add(M("iR-ADV DX C3940i", "DEMO11-003").Bk(0.0270m, 148390m).Cl(0.2700m, 42260m));
        all.Add(c11);

        // -- the control -- the same five machines as DEMO-01 with no format at all. Legacy folds
        // identical flat rentals and never merges usage, so the two invoices sit side by side:
        // DEMO-01 prints 3 lines, DEMO-12 prints 11 from exactly the same machines.
        Ct c12 = New("DEMO-12", "",
            "Legacy control - no billing format; same five machines as DEMO-01",
            "1 INV: rental 5 UNIT x 450.00 folded by the old rule, then BK+CL per machine = 11 lines");
        c12.Machines.Add(M("iR-ADV C3560i", "DEMO12-001").Rent(450m).Bk(0.0250m, 118400m).Cl(0.2500m, 41200m));
        c12.Machines.Add(M("iR-ADV C3560i", "DEMO12-002").Rent(450m).Bk(0.0250m, 96350m).Cl(0.2500m, 33780m));
        c12.Machines.Add(M("iR-ADV DX C3940i", "DEMO12-003").Rent(450m).Bk(0.0250m, 205110m).Cl(0.2500m, 62940m));
        c12.Machines.Add(M("iR-ADV DX C3940i", "DEMO12-004").Rent(450m).Bk(0.0250m, 187620m).Cl(0.2500m, 58015m));
        c12.Machines.Add(M("imageFORCE C7165", "DEMO12-005").Rent(450m).Bk(0.0250m, 342800m).Cl(0.2500m, 129440m));
        all.Add(c12);

        return all;
    }

    static Ct New(string no, string format, string desc, string prints)
    {
        Ct c = new Ct();
        c.No = no; c.Format = format; c.Desc = desc; c.Prints = prints;
        return c;
    }

    // ------------------------------------------------------------------ writing it

    static void CheckMeterTypes()
    {
        string[] want = new string[] { "RENTAL", "BK", "CL", "COMMIT", "WAIVE" };
        using (SqlConnection cn = new SqlConnection(_conn))
        {
            cn.Open();
            foreach (string t in want)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT [Description] FROM dbo.zSCP_MeterType WHERE MeterTypeCode=@c", cn))
                {
                    cmd.Parameters.AddWithValue("@c", t);
                    object d = cmd.ExecuteScalar();
                    if (d == null || d == DBNull.Value)
                        throw new Exception("Meter type '" + t + "' is missing from zSCP_MeterType.");
                    _meterDesc[t] = Convert.ToString(d);
                }
            }
        }
        Console.WriteLine("  meter types RENTAL / BK / CL / COMMIT / WAIVE all present");
    }

    static string PickDebtor()
    {
        using (SqlConnection cn = new SqlConnection(_conn))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TOP 1 AccNo FROM dbo.Debtor WHERE AccNo='3000-A0002'", cn))
            {
                object a = cmd.ExecuteScalar();
                if (a != null && a != DBNull.Value) return Convert.ToString(a);
            }
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 AccNo FROM dbo.Debtor ORDER BY AccNo", cn))
            {
                object a = cmd.ExecuteScalar();
                if (a == null || a == DBNull.Value) throw new Exception("No debtor in the book.");
                return Convert.ToString(a);
            }
        }
    }

    // Only the DEMO-* rows go, and nothing else. The machines and meters follow by cascade, but the
    // explicit deletes keep it obvious what the tool touches.
    static void Wipe()
    {
        using (SqlConnection cn = new SqlConnection(_conn))
        {
            cn.Open();
            int meters = Exec(cn, "DELETE FROM dbo.zSCP2_ItemMeter WHERE ItemKey IN " +
                "(SELECT ItemKey FROM dbo.zSCP2_Item WHERE ServiceItemNo LIKE 'DEMO-%')");
            int machines = Exec(cn, "DELETE FROM dbo.zSCP2_Item WHERE ServiceItemNo LIKE 'DEMO-%'");
            int cts = Exec(cn, "DELETE FROM dbo.zSCP2_Contract WHERE ContractNo LIKE 'DEMO-%'");
            Console.WriteLine("  cleared " + cts + " contracts, " + machines + " machines, " + meters + " meters");
        }
    }

    static int Exec(SqlConnection cn, string sql)
    {
        using (SqlCommand cmd = new SqlCommand(sql, cn)) return cmd.ExecuteNonQuery();
    }

    static void BuildAll()
    {
        List<Ct> plan = Plan();
        using (SqlConnection cn = new SqlConnection(_conn))
        {
            cn.Open();
            foreach (Ct c in plan) Build(cn, c);
        }
    }

    static void Build(SqlConnection cn, Ct c)
    {
        // The engine reads the CONTRACT, not the preset: BillingFormatCode only says "this contract
        // has been given a format". So the preset's four answers get stamped onto the contract the
        // same way ScpBillingFormat.ApplyTo does it.
        string split = "ONE";
        char rMode = 'A';
        char mMode = 'S';
        string fname = "(legacy - no format)";
        if (c.Format.Length > 0)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT FormatName, InvoiceSplit, RentalLineMode, MeterLineMode " +
                "FROM dbo.zSCP2_BillingFormat WHERE FormatCode=@f", cn))
            {
                cmd.Parameters.AddWithValue("@f", c.Format);
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) throw new Exception("Billing format '" + c.Format + "' not found.");
                    fname = Convert.ToString(rd["FormatName"]);
                    split = Convert.ToString(rd["InvoiceSplit"]).Trim();
                    rMode = Convert.ToString(rd["RentalLineMode"]).Trim()[0];
                    mMode = Convert.ToString(rd["MeterLineMode"]).Trim()[0];
                }
            }
        }
        char billingMode = split == "PM" || split == "PMS" ? 'S' : 'G';
        string rentalSep = split == "RS" || split == "PMS" ? "Y" : "N";

        long ck = 0;
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.zSCP2_Contract " +
            "(ContractNo, DebtorCode, [Description], BillingFormatCode, RentalLineMode, MeterLineMode, " +
            " BillingMode, BillingDay, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
            " RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy, Created, Modified, LastModified) " +
            "VALUES (@no, @deb, @desc, @fmt, @rm, @mm, @bm, 1, @sd, @sd, @ed, @rsep, 'N', 'ADMIN', 'ADMIN', " +
            " GETDATE(), GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
        {
            cmd.Parameters.AddWithValue("@no", c.No);
            cmd.Parameters.AddWithValue("@deb", _debtor);
            cmd.Parameters.AddWithValue("@desc", c.Desc);
            cmd.Parameters.AddWithValue("@fmt", c.Format);
            cmd.Parameters.AddWithValue("@rm", rMode.ToString());
            cmd.Parameters.AddWithValue("@mm", mMode.ToString());
            cmd.Parameters.AddWithValue("@bm", billingMode.ToString());
            cmd.Parameters.AddWithValue("@sd", START);
            cmd.Parameters.AddWithValue("@ed", EXPIRY);
            cmd.Parameters.AddWithValue("@rsep", rentalSep);
            ck = Convert.ToInt64(cmd.ExecuteScalar());
        }
        _ctMade++;

        int pos = 0;
        int meters = 0;
        foreach (Mach m in c.Machines)
        {
            pos++;
            long ik = InsertMachine(cn, ck, c, m, pos);
            foreach (Meter mt in m.Meters) { InsertMeter(cn, ik, m, mt); meters++; }
            _machMade++;
        }
        _meterMade += meters;

        Console.WriteLine();
        Console.WriteLine("  " + c.No + "  " + (c.Format.Length > 0 ? c.Format : "(none)") +
                          "   split=" + split + " rental=" + rMode + " meter=" + mMode +
                          "   " + c.Machines.Count + " machines / " + meters + " meters");
        Console.WriteLine("        " + fname);
        Console.WriteLine("        prints -> " + c.Prints);
    }

    static long InsertMachine(SqlConnection cn, long ck, Ct c, Mach m, int pos)
    {
        string sino = c.No + "-" + pos.ToString("000", CultureInfo.InvariantCulture);
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.zSCP2_Item " +
            "(ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, " +
            " MergeGroupCode, Inactive, IsGroupItem, MachineMode, " +
            " ServiceStartDate, ServiceExpiryDate, LastModified) " +
            "VALUES (@ck, @sino, @code, @sn, @desc, @pos, @grp, 'N', 'N', @mmode, @sd, @ed, GETDATE()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
        {
            cmd.Parameters.AddWithValue("@ck", ck);
            cmd.Parameters.AddWithValue("@sino", sino);
            cmd.Parameters.AddWithValue("@code", m.Model);
            cmd.Parameters.AddWithValue("@sn", m.Serial);
            cmd.Parameters.AddWithValue("@desc", m.Model + " / " + m.Serial);
            cmd.Parameters.AddWithValue("@pos", pos);
            cmd.Parameters.AddWithValue("@grp", m.MergeGroup);
            cmd.Parameters.AddWithValue("@mmode", m.MachineMode);
            cmd.Parameters.AddWithValue("@sd", START);
            cmd.Parameters.AddWithValue("@ed", EXPIRY);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }
    }

    static void InsertMeter(SqlConnection cn, long ik, Mach mach, Meter mt)
    {
        string desc = _meterDesc.ContainsKey(mt.Type) ? _meterDesc[mt.Type] : mt.Type;
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.zSCP2_ItemMeter " +
            "(ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, " +
            " MinimumCharges, ChargesRate, MeterMultiPriceCode, RebateQtyInPercent, FOCQty, InitialReading, " +
            " RentalStartDate, RentalMonths, RentalBasis, " +
            " WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, " +
            " WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified) " +
            "VALUES (@ik, @type, @desc, @role, '', @min, @rate, '', 0, 0, @init, " +
            " @rsd, @rm, @rb, @wn, @wt, 100, @ws, @wpt, @wpa, @cs, GETDATE())", cn))
        {
            cmd.Parameters.AddWithValue("@ik", ik);
            cmd.Parameters.AddWithValue("@type", mt.Type);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.Parameters.AddWithValue("@role", mt.Role);
            cmd.Parameters.AddWithValue("@min", mt.Min);
            cmd.Parameters.AddWithValue("@rate", mt.Rate);
            cmd.Parameters.AddWithValue("@init", mt.Initial);
            cmd.Parameters.AddWithValue("@rsd", mt.RentalMonths > 0 ? (object)START : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@rm", mt.RentalMonths);
            cmd.Parameters.AddWithValue("@rb", mt.RentalBasis);
            cmd.Parameters.AddWithValue("@wn", mt.WaiveFirstN);
            cmd.Parameters.AddWithValue("@wt", mt.WaiveTarget);
            cmd.Parameters.AddWithValue("@ws", mt.WaiveScope);
            cmd.Parameters.AddWithValue("@wpt", mt.WaivePartialThreshold);
            cmd.Parameters.AddWithValue("@wpa", mt.WaivePartialAmount);
            cmd.Parameters.AddWithValue("@cs", mt.CommitScope);
            cmd.ExecuteNonQuery();
        }
    }
}
