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

    static int Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
        {
            string n = new AssemblyName(a.Name).Name;
            string p = Path.Combine(AC, n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        // "serials" refreshes the machines' serial numbers in place and touches nothing else, so a
        // demo contract that has been edited by hand keeps those edits. Without it the seeder does
        // what it always did: delete every DEMO contract and build all twelve again.
        bool serialsOnly = args != null && args.Length > 0 &&
            string.Equals(args[0], "serials", StringComparison.OrdinalIgnoreCase);
        try { if (serialsOnly) RefreshSerials(); else Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine();
        Console.WriteLine("Models   : " + _itemsMade + " created, " + _itemsKept + " already there.");
        Console.WriteLine("Contracts: " + _ctMade + " rebuilt, " + _machMade + " machines, " + _meterMade + " meters.");
        return 0;
    }

    /// <summary>Replaces the tag a demo machine was seeded with by the serial it should have had.
    /// Idempotent: once a row's serial no longer reads like a tag it is left alone.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void RefreshSerials()
    {
        // Same book, same way in: this mode skips Run(), so it opens its own connection.
        AutoCount.Data.DBSetting dbs = new AutoCount.Data.DBSetting(
            AutoCount.Data.DBServerType.SQL2000,
            "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);
        _conn = dbs.ConnectionString;

        int done = 0;
        Dictionary<string, string> seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (SqlConnection cn = new SqlConnection(_conn))
        {
            cn.Open();
            DataTable t = new DataTable();
            using (SqlCommand q = new SqlCommand(
                "SELECT ItemKey, ServiceItemNo, ItemCode, SerialNumber FROM dbo.zSCP2_Item " +
                "WHERE ServiceItemNo LIKE 'DEMO-%' ORDER BY ItemKey", cn))
            using (SqlDataAdapter a = new SqlDataAdapter(q)) a.Fill(t);

            foreach (DataRow r in t.Rows)
            {
                string tag = TagOf(Convert.ToString(r["ServiceItemNo"]));
                string sn = Serial(tag);
                if (seen.ContainsKey(sn))
                    Console.WriteLine("  !! " + sn + " already taken by " + seen[sn]);
                else seen[sn] = Convert.ToString(r["ServiceItemNo"]).Trim();
                using (SqlCommand u = new SqlCommand(
                    "UPDATE dbo.zSCP2_Item SET SerialNumber = @sn, [Description] = @desc, " +
                    "LineGroupCode = @label, LastModified = GETDATE() WHERE ItemKey = @ik", cn))
                {
                    u.Parameters.AddWithValue("@label", DutyLabel(Convert.ToString(r["ItemCode"])));
                    u.Parameters.AddWithValue("@sn", sn);
                    u.Parameters.AddWithValue("@desc", Convert.ToString(r["ItemCode"]).Trim() + " / " + sn);
                    u.Parameters.AddWithValue("@ik", Convert.ToInt64(r["ItemKey"]));
                    u.ExecuteNonQuery();
                }
                Console.WriteLine("  " + Convert.ToString(r["ServiceItemNo"]).PadRight(14) + " -> " + sn);
                done++;
            }
        }
        Console.WriteLine();
        Console.WriteLine(done + " machines given a factory-shaped serial, " + seen.Count + " distinct.");
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

        /// <summary>A ladder off the master list (zSCP_MeterMultiPrice), by code.</summary>
        public string MultiPriceCode = "";

        /// <summary>Or this meter's OWN ladder, written to zSCP2_ItemMeterPrice and keyed by the meter
        /// itself. Pairs of (upper boundary, unit price), ascending; the last boundary is the ceiling.
        /// A ladder is MARGINAL -- each slice of usage pays its own band -- so a first band priced 0.00
        /// is how a free-copy allowance is written.</summary>
        public List<decimal[]> Tiers;
    }

    class Mach
    {
        public string Model = "";
        public string Serial = "";
        public string MergeGroup = "";
        /// <summary>Which BRANCH this machine is billed under. A bill group is a separate invoice --
        /// the contract stays one contract, but each group's machines get their own paper.</summary>
        public string BillGroup = "";
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

        /// <summary>A waive measured over the whole RENTAL line rather than one machine: the machines
        /// sharing this one's rental group have their copies added up, and the whole line's rental comes
        /// off when they reach the target. One meter for the line -- one per machine would credit the
        /// rental once for each of them.</summary>
        public Mach GroupWaive(decimal amount, decimal target)
        {
            Waive(amount, 0, target, "BKCL");
            Meters[Meters.Count - 1].CommitScope = "G";
            return this;
        }
        /// <summary>Black on a ladder from the master list. Two machines on the SAME code still merge
        /// onto one printed line; a different code, or an own-ladder below, splits them -- the fold keys
        /// on the ladder rather than the rate, because with tiers there is no single rate to key on.</summary>
        public Mach BkLadder(string code, decimal initial)
        {
            Meter m = new Meter();
            m.Type = "BK"; m.Role = "BK"; m.Initial = initial; m.MultiPriceCode = code;
            Meters.Add(m); return this;
        }

        /// <summary>Black on a ladder of this meter's own. Pairs: boundary, price, boundary, price...
        /// Never merges with anything, because the ladder is keyed to this one meter.</summary>
        public Mach BkTiers(decimal initial, params decimal[] pairs)
        {
            Meter m = new Meter();
            m.Type = "BK"; m.Role = "BK"; m.Initial = initial;
            m.Tiers = new List<decimal[]>();
            for (int i = 0; i + 1 < pairs.Length; i += 2)
                m.Tiers.Add(new decimal[] { pairs[i], pairs[i + 1] });
            Meters.Add(m); return this;
        }

        public Mach Group(string g) { MergeGroup = g; return this; }
        public Mach Branch(string b) { BillGroup = b; MergeGroup = b; return this; }
        public Mach Online() { MachineMode = "ONLINE"; return this; }
        public Mach Offline() { MachineMode = "OFFLINE"; return this; }
    }

    class Ct
    {
        public string No = "";
        public string Format = "";        // '' = the legacy control

        /// <summary>Tier bands agreed for the contract's black line as a whole, "boundary|price;...".
        /// Written to zSCP2_ContractRentalPrice on the copies side, which is where the engine looks.
        /// </summary>
        public string GroupLadderBk = "";
        public string GroupLadderCl = "";
        public string Desc = "";
        public string Prints = "";        // what the invoice should come out as, for the console
        public List<Mach> Machines = new List<Mach>();
    }

    static Mach M(string model, string tag) { Mach m = new Mach(); m.Model = model; m.Serial = tag; return m; }

    // The second argument above is a TAG -- "DEMO01-003" -- so the scenarios below stay readable and
    // a machine keeps its identity across a re-seed. It is not the serial number. A serial number is
    // stamped on the machine by the factory and has nothing to do with the number the contract files
    // it under: this book's real ones read 0057231, 77878, A/UMW02197-110V against service items
    // called CSSI 260000014. Demo data that made the two the same taught the wrong thing about the
    // module, so the tag is folded into a Canon-shaped serial instead -- three characters then five
    // digits, YAJ01479 / 2JC10897 / 4NL20240 -- deterministically, so re-seeding gives every machine
    // back the same serial it had.
    static string Serial(string tag)
    {
        // FNV-1a and then an avalanche step. The plain hash alone is not enough here: the tags differ
        // only in their last character, and slicing an unmixed hash handed DEMO01-001, DEMO01-003 and
        // DEMO01-004 the same serial -- three machines on one contract sharing a serial number is
        // exactly the confusion this was meant to remove.
        uint h = 2166136261u;
        foreach (char ch in tag ?? "") { unchecked { h ^= ch; h *= 16777619u; } }
        unchecked
        {
            h ^= h >> 16; h *= 0x7feb352du;
            h ^= h >> 15; h *= 0x846ca68bu;
            h ^= h >> 16;
        }
        const string A = "ABCDEFGHJKLMNPRSTUVWXYZ";   // no I/O/Q, the way a real plate leaves them out
        uint n = h % 100000u;
        uint r = h / 100000u;
        char c2 = A[(int)(r % (uint)A.Length)]; r /= (uint)A.Length;
        char c1 = A[(int)(r % (uint)A.Length)]; r /= (uint)A.Length;
        char c0 = (r % 3u == 0u) ? (char)('2' + (r / 3u) % 8u) : A[(int)((r / 3u) % (uint)A.Length)];
        return new string(new char[] { c0, c1, c2 }) + n.ToString("00000", CultureInfo.InvariantCulture);
    }

    /// <summary>The words the printed line carries for a machine of this model -- its duty class, in
    /// the form this book already prints ("MEDIUM DUTY \"30-50 ppm\" - IRADX4935I", "HEAVY DUTY
    /// ''105cpm''"). It is a DESCRIPTION and never a reason to split a line: DEMO-01 merges five
    /// machines carrying two different labels onto one rental line, and DEMO-04 keeps two models
    /// carrying the SAME label on two lines. Both are worth being able to see.</summary>
    static string DutyLabel(string model)
    {
        switch ((model ?? "").Trim())
        {
            case "iR-ADV DX C3922i": return "LIGHT DUTY";
            case "iR-ADV 2745i":     return "LIGHT DUTY";
            case "iR-ADV C1234":     return "LIGHT DUTY";
            case "iR-ADV C3560i":    return "MEDIUM DUTY \"30-50 ppm\"";
            case "iR-ADV C5335":     return "MEDIUM DUTY \"30-50 ppm\"";
            case "iR-ADV DX C3940i": return "MEDIUM DUTY \"30-50 ppm\"";
            case "imageFORCE C5150": return "MEDIUM HEAVY DUTY \"50 ppm\"";
            case "iR-ADV DX C5760i": return "MEDIUM HEAVY DUTY \"57 ppm\"";
            case "iR-ADV DX 4951i":  return "MEDIUM HEAVY DUTY \"51 ppm\"";
            case "iR-ADV DX 4960i":  return "MEDIUM HEAVY DUTY \"60 ppm\"";
            case "iR-ADV C5665":     return "HEAVY DUTY \"65 ppm\"";
            case "iR-ADV 6580i":     return "HEAVY DUTY \"65 ppm\"";
            case "iR-ADV C7565i":    return "HEAVY DUTY \"65 ppm\"";
            case "imageFORCE C7165": return "HEAVY DUTY \"65 ppm\"";
            case "imageFORCE 6170":  return "HEAVY DUTY \"70 ppm\"";
            case "iR-ADV DX 6980i":  return "HEAVY DUTY \"80 ppm\"";
            case "iR-ADV DX C5880i": return "HEAVY DUTY \"80 ppm\"";
            case "imagePRESS C910":  return "HEAVY DUTY \"90 ppm\"";
        }
        return "";
    }

    /// <summary>The tag a demo machine was seeded under, recovered from the number the contract files
    /// it as: DEMO-01-003 was seeded as DEMO01-003. Reading it from here rather than from the serial
    /// column means the refresh can be run again after the rule changes, instead of only once.</summary>
    static string TagOf(string serviceItemNo)
    {
        string v = (serviceItemNo ?? "").Trim();
        int i = v.IndexOf('-');
        return i < 0 ? v : v.Substring(0, i) + v.Substring(i + 1);
    }

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

        // -- Mode 13 -- one invoice per machine with EVERYTHING that machine owes on it: its rental
        // and its own black and colour, on one page. The only split "S" without "rental apart", and
        // the one shape the other twelve never reach -- DEMO-11 also cuts per machine but sends the
        // rental to a second invoice, so no demo showed a machine's whole month on a single page.
        //
        // Three machines at three different rentals and three different rates, so nothing could merge
        // even if the split allowed it: each invoice is unmistakably one machine's own.
        Ct c13 = New("DEMO-13", "PERMACHINE-ONE",
            "Mode 13 - one invoice per machine; rental and BK+CL together on it",
            "3 invoices, 3 lines each: RENTAL + BK + CL for that one machine = 9 lines");
        c13.Machines.Add(M("iR-ADV C3560i", "DEMO13-001").Rent(495m).Bk(0.0250m, 128400m).Cl(0.2500m, 44100m));
        c13.Machines.Add(M("iR-ADV DX C5760i", "DEMO13-002").Rent(815m).Bk(0.0230m, 214750m).Cl(0.2300m, 68320m));
        c13.Machines.Add(M("imageFORCE C7165", "DEMO13-003").Rent(1180m).Bk(0.0210m, 392610m).Cl(0.2100m, 147880m));
        all.Add(c13);

        // -- Mode 14 -- the committed minimum, all three answers on one page. The invented readings
        // climb with the machine's position (black 4,813 then 5,426 then 6,039), so the rates below are
        // what decide whether a machine reaches its floor -- machine 2 is priced an order down on
        // purpose so it falls short and gets topped up.
        //
        //   001  120.33 + 259.25 = 379.58  vs a floor of 300  -> over it, nothing happens
        //   002   54.26 + 117.40 = 171.66  vs a floor of 300  -> short, a COMMIT line makes up 128.34
        //   003  150.98 + 327.75 = 478.73  no floor at all    -> the control
        Ct c14 = New("DEMO-14", "1INV-MMACHINE",
            "Mode 14 - committed minimum per machine; one over its floor, one short, one without",
            "1 INV: 1 rental line + BK/CL per machine + ONE commit top-up (DEMO14-002 only)");
        c14.Machines.Add(M("iR-ADV C3560i", "DEMO14-001").Rent(450m)
                          .Bk(0.0250m, 118400m).Cl(0.2500m, 41200m).Commit(300m, "S"));
        c14.Machines.Add(M("iR-ADV C3560i", "DEMO14-002").Rent(450m)
                          .Bk(0.0100m, 96350m).Cl(0.1000m, 33780m).Commit(300m, "S"));
        c14.Machines.Add(M("iR-ADV C3560i", "DEMO14-003").Rent(450m)
                          .Bk(0.0250m, 205110m).Cl(0.2500m, 62940m));
        all.Add(c14);

        // -- Mode 15 -- the rental waive, on ONE invoice because that is the only place it means
        // anything: the credit and the copies that earned it have to be on the same page.
        //
        //   001  copies come to 379.58, the deal says reach 300  -> fires, -495.00 off the rental
        //   002  copies come to 429.15, the deal says reach 900  -> silent, no line at all
        //   003  free for the first 24 months and this is month 13 -> fires on the window, -640.00
        Ct c15 = New("DEMO-15", "1INV-MMACHINE",
            "Mode 15 - rental waive on one invoice; by usage, not reached, and by free months",
            "1 INV: 1 rental line + BK/CL per machine + 2 waive contras (001 by target, 003 by window)");
        c15.Machines.Add(M("iR-ADV C3560i", "DEMO15-001").Rent(495m)
                          .Bk(0.0250m, 118400m).Cl(0.2500m, 41200m).Waive(495m, 0, 300m, "BKCL"));
        c15.Machines.Add(M("iR-ADV C3560i", "DEMO15-002").Rent(815m)
                          .Bk(0.0250m, 96350m).Cl(0.2500m, 33780m).Waive(815m, 0, 900m, "BKCL"));
        c15.Machines.Add(M("iR-ADV C3560i", "DEMO15-003").Rent(640m)
                          .Bk(0.0250m, 205110m).Cl(0.2500m, 62940m).Waive(640m, 24, 0m, "BKCL"));
        all.Add(c15);

        // -- Mode 16 -- tiered pricing, and what it does to the LINES. Everything here is in one group
        // and every rental is 450, so the fold is free to merge all five; what actually splits the black
        // lines is the ladder, because with tiers there is no single rate a line could print.
        //
        //   001 + 005  the same master ladder  -> ONE line for the two of them
        //   002        a ladder of its own     -> alone, whatever it shares with anyone
        //   003        a free allowance written as a first band at 0.00
        //   004        no ladder, a flat rate  -> alone again, on the rate
        // Colour is a flat 0.2500 on all five, so it comes out as the single line the group asked for.
        Ct c16 = New("DEMO-16", "1INV-ALL",
            "Mode 16 - tiered (multi) pricing; the ladder is what splits the line, not the rate",
            "1 INV: 1 rental line + 4 BK lines (2 machines share a master ladder) + 1 CL line = 6");
        c16.Machines.Add(M("iR-ADV C3560i", "DEMO16-001").Rent(450m)
                          .BkLadder("BK +P - 0.028 FOC300", 118400m).Cl(0.2500m, 41200m));
        c16.Machines.Add(M("iR-ADV C3560i", "DEMO16-002").Rent(450m)
                          .BkTiers(96350m, 2000m, 0.0300m, 5000m, 0.0250m, 100000000m, 0.0200m)
                          .Cl(0.2500m, 33780m));
        c16.Machines.Add(M("iR-ADV C3560i", "DEMO16-003").Rent(450m)
                          .BkTiers(205110m, 2500m, 0m, 100000000m, 0.0250m)
                          .Cl(0.2500m, 62940m));
        c16.Machines.Add(M("iR-ADV C3560i", "DEMO16-004").Rent(450m)
                          .Bk(0.0250m, 187620m).Cl(0.2500m, 58015m));
        c16.Machines.Add(M("iR-ADV C3560i", "DEMO16-005").Rent(450m)
                          .BkLadder("BK +P - 0.028 FOC300", 342800m).Cl(0.2500m, 129440m));
        all.Add(c16);

        // -- Mode 17 & 18 -- the two ways to floor a merged line, side by side. The user's own
        // worksheet: same three machines, same copies, SAME invoice shape -- one merged copies row and
        // ONE minimum row -- and only the arithmetic differs.
        //
        //   17  a floor on EACH machine   350 / 400 / 450, each measured against its own copies,
        //       the row printing the shortfalls added up
        //   18  ONE floor for the GROUP   1,500 measured against the copies added up
        //
        // Everything else is held identical on purpose, so a difference in the output can only come
        // from the thing being demonstrated. Copies are 4,813 / 5,426 / 6,039 black at 0.02 (the
        // invented readings climb with position), colour 1,037 / 1,174 / 1,311 at 0.20.
        Ct c17 = New("DEMO-17", "1INV-ALL",
            "Mode 17 - a minimum on EACH machine, copies merged onto one line",
            "1 INV: 1 rental + 1 BK + 1 CL + ONE minimum row carrying the three shortfalls added up");
        c17.Machines.Add(M("iR-ADV C3560i", "DEMO17-001").Rent(450m)
                          .Bk(0.0200m, 118400m).Cl(0.2000m, 41200m).Commit(350m, "S"));
        c17.Machines.Add(M("iR-ADV C3560i", "DEMO17-002").Rent(450m)
                          .Bk(0.0200m, 96350m).Cl(0.2000m, 33780m).Commit(400m, "S"));
        c17.Machines.Add(M("iR-ADV C3560i", "DEMO17-003").Rent(450m)
                          .Bk(0.0200m, 205110m).Cl(0.2000m, 62940m).Commit(450m, "S"));
        all.Add(c17);

        // The group floor lives on ONE machine and is scoped to the group; the other two carry none.
        // Six meters against seven, and that difference is the whole point -- one deal, one meter.
        Ct c18 = New("DEMO-18", "1INV-ALL",
            "Mode 18 - ONE minimum for the whole group, copies merged onto one line",
            "1 INV: 1 rental + 1 BK + 1 CL + ONE minimum row measured on the group total");
        c18.Machines.Add(M("iR-ADV C3560i", "DEMO18-001").Rent(450m)
                          .Bk(0.0200m, 118400m).Cl(0.2000m, 41200m).Commit(1500m, "G"));
        c18.Machines.Add(M("iR-ADV C3560i", "DEMO18-002").Rent(450m)
                          .Bk(0.0200m, 96350m).Cl(0.2000m, 33780m));
        c18.Machines.Add(M("iR-ADV C3560i", "DEMO18-003").Rent(450m)
                          .Bk(0.0200m, 205110m).Cl(0.2000m, 62940m));
        all.Add(c18);

        // -- Mode 19 & 20 -- the two ways to waive a rental, side by side. A waive is a credit against
        // the RENTAL decided by what the COPIES came to, so it only means anything when both land on
        // the same piece of paper: one invoice for everything.
        //
        //   19  ONE waive for the rental line   the three machines' copies added up against one target,
        //       and the whole line's rental comes off
        //   20  a waive on EACH machine         each measured against its own copies, so a machine that
        //       prints enough loses its rental while the others keep paying
        //
        // Same three machines, same rates, same targets where comparable -- only the scope differs.
        Ct c19 = New("DEMO-19", "1INV-ALL",
            "Mode 19 - ONE rental waive for the whole line, on the group's copies",
            "1 INV: rental + BK + CL, and one waive contra for the whole line when the copies reach 1,000");
        c19.Machines.Add(M("iR-ADV C3560i", "DEMO19-001").Rent(450m)
                          .Bk(0.0200m, 118400m).Cl(0.2000m, 41200m).GroupWaive(1350m, 1000m));
        c19.Machines.Add(M("iR-ADV C3560i", "DEMO19-002").Rent(450m)
                          .Bk(0.0200m, 96350m).Cl(0.2000m, 33780m));
        c19.Machines.Add(M("iR-ADV C3560i", "DEMO19-003").Rent(450m)
                          .Bk(0.0200m, 205110m).Cl(0.2000m, 62940m));
        all.Add(c19);

        // Targets set so the three machines answer differently on the same month -- the point of the
        // per-machine shape is that they are judged one at a time.
        Ct c20 = New("DEMO-20", "1INV-ALL",
            "Mode 20 - a rental waive on EACH machine, on that machine's own copies",
            "1 INV: rental + BK + CL, and a waive contra only for the machines that reached their target");
        c20.Machines.Add(M("iR-ADV C3560i", "DEMO20-001").Rent(450m)
                          .Bk(0.0200m, 118400m).Cl(0.2000m, 41200m).Waive(450m, 0, 350m, "BKCL"));
        c20.Machines.Add(M("iR-ADV C3560i", "DEMO20-002").Rent(450m)
                          .Bk(0.0200m, 96350m).Cl(0.2000m, 33780m).Waive(450m, 0, 350m, "BKCL"));
        c20.Machines.Add(M("iR-ADV C3560i", "DEMO20-003").Rent(450m)
                          .Bk(0.0200m, 205110m).Cl(0.2000m, 62940m).Waive(450m, 0, 350m, "BKCL"));
        all.Add(c20);

        // -- Mode 21 -- one machine, one invoice, and everything that machine's month contains on it:
        // the rental, the waive that can take the rental away, the floor under its copies, and the
        // copies themselves. This is the shape a customer means by "bill each machine separately" --
        // not five invoices for five things, ONE invoice per machine that answers for that machine.
        //
        // A rental waive is allowed here for the same reason it is allowed on a single invoice: the
        // machine's rental and the machine's copies are on the same piece of paper, so the credit and
        // the reason for it are printed together. Only sending the rental to an invoice of its own
        // breaks that, and this split does not.
        //
        // The floor and the waive are measured over the SAME copies but do different things -- the
        // floor tops the COPIES up, the waive credits the RENT -- and the waive is judged first, on
        // what the machine actually printed, before any top-up. So the three machines answer three
        // different ways on one contract:
        //
        //   001  copies 400  target 300, floor 300  -> waive FIRES, floor met      (COMMIT prints 0.00)
        //   002  copies 150  target 300, floor 300  -> waive silent, floor tops up 150
        //   003  copies 300  target 250, floor 400  -> waive FIRES *and* floor tops up 100
        Ct c21 = New("DEMO-21", "PERMACHINE-ONE",
            "Mode 21 - one invoice per machine carrying rental + waive + minimum + BK/CL",
            "3 invoices: RENTAL, WAIVE, COMMIT, BK, CL for that one machine (002 has no waive line)");
        c21.Machines.Add(M("iR-ADV C3560i", "DEMO21-001").Rent(450m)
                          .Bk(0.0200m, 118400m).Cl(0.2000m, 41200m)
                          .Waive(450m, 0, 300m, "BKCL").Commit(300m, "S"));
        c21.Machines.Add(M("iR-ADV C3560i", "DEMO21-002").Rent(450m)
                          .Bk(0.0200m, 96350m).Cl(0.2000m, 33780m)
                          .Waive(450m, 0, 300m, "BKCL").Commit(300m, "S"));
        c21.Machines.Add(M("iR-ADV C3560i", "DEMO21-003").Rent(450m)
                          .Bk(0.0200m, 205110m).Cl(0.2000m, 62940m)
                          .Waive(450m, 0, 250m, "BKCL").Commit(400m, "S"));
        all.Add(c21);

        // -- Mode 22 -- tier pricing agreed over the GROUP. Three machines print 5,000 black each.
        // Every one of them alone would pay the first band on its own copies; together they climb one
        // ladder once, and the 15,000 reaches the cheap end.
        //
        //   per machine   5,000 @ the 2,000/5,000/rest ladder  = 60.00 + 75.00        =    135.00 each
        //                                                                   three of them   405.00
        //   as a group   15,000 over the same ladder = 2,000@.030 + 3,000@.025 + 10,000@.020
        //                                            =  60.00 +  75.00 + 200.00      =    335.00
        //
        // Same machines, same copies, same bands -- 70.00 apart. That difference IS the feature.
        Ct c22 = New("DEMO-22", "1INV-ALL",
            "Mode 22 - tier pricing agreed over the whole group, not machine by machine",
            "1 INV: ONE black line of 15,000 climbing the group's ladder once = 335.00");
        c22.Machines.Add(M("iR-ADV C3560i", "DEMO22-001").Rent(450m)
                          .Bk(0.0300m, 118400m).Cl(0.2000m, 41200m));
        c22.Machines.Add(M("iR-ADV C3560i", "DEMO22-002").Rent(450m)
                          .Bk(0.0300m, 96350m).Cl(0.2000m, 33780m));
        c22.Machines.Add(M("iR-ADV C3560i", "DEMO22-003").Rent(450m)
                          .Bk(0.0300m, 205110m).Cl(0.2000m, 62940m));
        c22.GroupLadderBk = "2000|0.030;5000|0.025;100000000|0.020";
        all.Add(c22);

        // -- Mode 23 -- the other half of tier pricing: one invoice per machine, and every machine
        // priced on a ladder of its OWN. Nothing merges here and nothing is meant to -- a ladder that
        // belongs to one machine is the statement that this machine is priced unlike the others, and
        // the split gives each of them its own page to say it on.
        //
        // All three ways a machine can carry a ladder are on this one contract, so the three invoices
        // are a side-by-side of what each looks like when it prints:
        //
        //   001  its own bands, with a free first band  3,000 free, 10,000 @ .025, rest @ .020
        //   002  its own bands, no free band            5,000 @ .030, rest @ .022
        //   003  a ladder off the master price list     BK +P - 0.028 FOC300
        Ct c23 = New("DEMO-23", "PERMACHINE-ONE",
            "Mode 23 - one invoice per machine, each machine on its own tier ladder",
            "3 invoices, 3 lines each: RENTAL + a laddered BK + CL for that one machine");
        c23.Machines.Add(M("iR-ADV C3560i", "DEMO23-001").Rent(450m)
                          .BkTiers(118400m, 3000m, 0.0000m, 10000m, 0.0250m, 100000000m, 0.0200m)
                          .Cl(0.2000m, 41200m));
        c23.Machines.Add(M("iR-ADV DX C3940i", "DEMO23-002").Rent(450m)
                          .BkTiers(96350m, 5000m, 0.0300m, 100000000m, 0.0220m)
                          .Cl(0.2000m, 33780m));
        c23.Machines.Add(M("imageFORCE C7165", "DEMO23-003").Rent(450m)
                          .BkLadder("BK +P - 0.028 FOC300", 205110m)
                          .Cl(0.2000m, 62940m));
        all.Add(c23);

        // -- Mode 24 -- one contract, two branches, two invoices. The customer is one debtor with one
        // agreement, but each branch settles its own paper, so every machine carries the branch it is
        // billed under and the run makes one invoice per branch.
        //
        // The branches are also the merge groups, and they have to be: a printed line lives on one
        // invoice, so machines billed apart can never share one. Merging across them is refused on the
        // pricing screen -- this contract is what that refusal is about.
        Ct c24 = New("DEMO-24", "1INV-ALL",
            "Mode 24 - one contract, two branches, one invoice each",
            "2 INV: NORTH prints rental + BK + CL for its 2 machines; SOUTH the same for its 2");
        c24.Machines.Add(M("iR-ADV C3560i", "DEMO24-001").Branch("NORTH").Rent(450m)
                          .Bk(0.0250m, 118400m).Cl(0.2500m, 41200m));
        c24.Machines.Add(M("iR-ADV C3560i", "DEMO24-002").Branch("NORTH").Rent(450m)
                          .Bk(0.0250m, 96350m).Cl(0.2500m, 33780m));
        c24.Machines.Add(M("iR-ADV C3560i", "DEMO24-003").Branch("SOUTH").Rent(450m)
                          .Bk(0.0250m, 205110m).Cl(0.2500m, 62940m));
        c24.Machines.Add(M("iR-ADV C3560i", "DEMO24-004").Branch("SOUTH").Rent(450m)
                          .Bk(0.0250m, 187620m).Cl(0.2500m, 58015m));
        all.Add(c24);

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
            // Own-ladders first: they hang off the meters and would be orphaned by the delete below.
            Exec(cn, "DELETE FROM dbo.zSCP2_ItemMeterPrice WHERE ItemMeterKey IN " +
                "(SELECT ItemMeterKey FROM dbo.zSCP2_ItemMeter WHERE ItemKey IN " +
                "(SELECT ItemKey FROM dbo.zSCP2_Item WHERE ServiceItemNo LIKE 'DEMO-%'))");
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

    /// <summary>The twelve shapes, written out. They used to be looked up from a Billing Format
    /// preset; now the contract carries the answers itself and the grouping is stamped onto the
    /// machines, which is what the screen edits and what the engine folds on. The codes stay as the
    /// scenario's NAME -- they are how the demo contracts are talked about -- but nothing reads them
    /// at billing time any more.</summary>
    static void Shape(string code, out string split, out char rMode, out char mMode, out string fname)
    {
        switch (code)
        {
            case "1INV-ALL":       split = "ONE"; rMode = 'A'; mMode = 'A'; fname = "1 invoice, rental 1 line, BK+CL 1 line each"; return;
            case "1INV-RMODEL":    split = "ONE"; rMode = 'M'; mMode = 'A'; fname = "1 invoice, rental per model, BK+CL 1 line each"; return;
            case "1INV-MMACHINE":  split = "ONE"; rMode = 'A'; mMode = 'S'; fname = "1 invoice, rental 1 line, BK+CL per machine"; return;
            case "1INV-BYMODEL":   split = "ONE"; rMode = 'M'; mMode = 'M'; fname = "1 invoice, rental per model, BK+CL per model"; return;
            case "2INV-ALL":       split = "RS";  rMode = 'A'; mMode = 'A'; fname = "2 invoices, rental 1 line, BK+CL 1 line each"; return;
            case "2INV-RMODEL":    split = "RS";  rMode = 'M'; mMode = 'A'; fname = "2 invoices, rental per model, BK+CL 1 line each"; return;
            case "2INV-MMACHINE":  split = "RS";  rMode = 'A'; mMode = 'S'; fname = "2 invoices, rental 1 line, BK+CL per machine"; return;
            case "2INV-RMODEL-MM": split = "RS";  rMode = 'M'; mMode = 'S'; fname = "2 invoices, rental per model, BK+CL per machine"; return;
            case "2INV-EACH":      split = "RS";  rMode = 'S'; mMode = 'S'; fname = "2 invoices, rental per machine, BK+CL per machine"; return;
            case "2INV-RMACHINE":  split = "RS";  rMode = 'S'; mMode = 'A'; fname = "2 invoices, rental per machine, BK+CL 1 line each"; return;
            case "PERMACHINE":     split = "PMS"; rMode = 'S'; mMode = 'S'; fname = "One rental + one meter invoice per machine"; return;
            case "PERMACHINE-ONE": split = "PM";  rMode = 'S'; mMode = 'S'; fname = "One invoice per machine, rental and meters together"; return;
        }
        split = "ONE"; rMode = 'A'; mMode = 'S'; fname = "(legacy - the old rules, nothing grouped)";
    }

    /// <summary>The group a machine belongs to on one side, under the shape being demonstrated.
    /// This is the whole of the new model: no mode is stored anywhere, the machines carry the
    /// grouping and the engine folds on that. 'A' is everyone in one group, 'M' is a group per
    /// model, 'S' is nobody grouped -- and anything the three cannot say is reachable too, by
    /// ticking machines and merging them, which no mode could ever express.</summary>
    static string GroupFor(char mode, string model)
    {
        if (mode == 'A') return "ALL MACHINES";
        if (mode == 'M') return model.ToUpperInvariant();
        return "";
    }

    static void Build(SqlConnection cn, Ct c)
    {
        string split; char rMode; char mMode; string fname;
        Shape(c.Format, out split, out rMode, out mMode, out fname);
        bool newLayout = c.Format.Length > 0;
        char billingMode = split == "PM" || split == "PMS" ? 'S' : 'G';
        string rentalSep = split == "RS" || split == "PMS" ? "Y" : "N";

        long ck = 0;
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.zSCP2_Contract " +
            "(ContractNo, DebtorCode, [Description], BillingFormatCode, UseNewLayout, MachineLineShows, " +
            " RentalLineMode, MeterLineMode, " +
            " BillingMode, BillingDay, ContractDate, ServiceStartDate, ServiceExpiryDate, " +
            " RentalSeparateInvoice, Inactive, CreatedBy, ModifiedBy, Created, Modified, LastModified) " +
            "VALUES (@no, @deb, @desc, '', @new, 'L', @rm, @mm, @bm, 1, @sd, @sd, @ed, @rsep, 'N', 'ADMIN', 'ADMIN', " +
            " GETDATE(), GETDATE(), GETDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
        {
            cmd.Parameters.AddWithValue("@no", c.No);
            cmd.Parameters.AddWithValue("@deb", _debtor);
            cmd.Parameters.AddWithValue("@desc", c.Desc);
            cmd.Parameters.AddWithValue("@new", newLayout ? "Y" : "N");
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
            long ik = InsertMachine(cn, ck, c, m, pos, GroupFor(rMode, m.Model), GroupFor(mMode, m.Model));
            foreach (Meter mt in m.Meters) { InsertMeter(cn, ik, m, mt); meters++; }
            _machMade++;
        }
        _meterMade += meters;

        // Tier bands agreed for the copies of the whole group. They live where every agreed group
        // figure lives -- one row per side per group -- because that is the row the engine reads.
        if (c.GroupLadderBk.Length > 0 || c.GroupLadderCl.Length > 0)
        {
            string grp = GroupFor(mMode, c.Machines[0].Model);
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey=@ck AND Side='M' AND GroupCode=@g; " +
                "INSERT INTO dbo.zSCP2_ContractRentalPrice " +
                "(ContractKey, Side, GroupCode, UnitPrice, BkPrice, ClPrice, LadderBk, LadderCl, LastModified) " +
                "VALUES (@ck,'M',@g,0,0,0,@lb,@lc,GETDATE());", cn))
            {
                cmd.Parameters.AddWithValue("@ck", ck);
                cmd.Parameters.AddWithValue("@g", grp);
                cmd.Parameters.AddWithValue("@lb", c.GroupLadderBk);
                cmd.Parameters.AddWithValue("@lc", c.GroupLadderCl);
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine();
        Console.WriteLine("  " + c.No + "  " + (c.Format.Length > 0 ? c.Format : "(none)") +
                          "   split=" + split + " rental=" + rMode + " meter=" + mMode +
                          "   " + c.Machines.Count + " machines / " + meters + " meters");
        Console.WriteLine("        " + fname);
        Console.WriteLine("        prints -> " + c.Prints);
    }

    static long InsertMachine(SqlConnection cn, long ck, Ct c, Mach m, int pos,
                              string rentalGroup, string meterGroup)
    {
        // A group typed by hand on the screen beats the shape, exactly as it does in the engine.
        if ((m.MergeGroup ?? "").Trim().Length > 0) { rentalGroup = m.MergeGroup; meterGroup = m.MergeGroup; }
        string sino = c.No + "-" + pos.ToString("000", CultureInfo.InvariantCulture);
        using (SqlCommand cmd = new SqlCommand(
            "INSERT INTO dbo.zSCP2_Item " +
            "(ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, " +
            " MergeGroupCode, MergeGroupCodeMeter, BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode, " +
            " ServiceStartDate, ServiceExpiryDate, LastModified) " +
            "VALUES (@ck, @sino, @code, @sn, @desc, @pos, @grp, @grpm, @bg, @label, 'N', 'N', @mmode, @sd, @ed, GETDATE()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
        {
            cmd.Parameters.AddWithValue("@ck", ck);
            cmd.Parameters.AddWithValue("@sino", sino);
            cmd.Parameters.AddWithValue("@code", m.Model);
            string sn = Serial(m.Serial);
            cmd.Parameters.AddWithValue("@sn", sn);
            cmd.Parameters.AddWithValue("@desc", m.Model + " / " + sn);
            cmd.Parameters.AddWithValue("@pos", pos);
            cmd.Parameters.AddWithValue("@grp", rentalGroup ?? "");
            cmd.Parameters.AddWithValue("@grpm", meterGroup ?? "");
            cmd.Parameters.AddWithValue("@bg", (m.BillGroup ?? "").Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@label", DutyLabel(m.Model));
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
            "VALUES (@ik, @type, @desc, @role, '', @min, @rate, @mpc, 0, 0, @init, " +
            " @rsd, @rm, @rb, @wn, @wt, 100, @ws, @wpt, @wpa, @cs, GETDATE()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS bigint);", cn))
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
            cmd.Parameters.AddWithValue("@mpc", mt.MultiPriceCode);
            long imk = Convert.ToInt64(cmd.ExecuteScalar());

            if (mt.Tiers == null || mt.Tiers.Count == 0) return;
            // An own-ladder lives beside the meter, and the engine picks it up under the key
            // "#<ItemMeterKey>" without the meter naming a code at all.
            foreach (decimal[] t in mt.Tiers)
                using (SqlCommand tc = new SqlCommand(
                    "INSERT INTO dbo.zSCP2_ItemMeterPrice (ItemMeterKey, MeterReading, UnitPrice) " +
                    "VALUES (@k, @r, @p)", cn))
                {
                    tc.Parameters.AddWithValue("@k", imk);
                    tc.Parameters.AddWithValue("@r", t[0]);
                    tc.Parameters.AddWithValue("@p", t[1]);
                    tc.ExecuteNonQuery();
                }
        }
    }
}
