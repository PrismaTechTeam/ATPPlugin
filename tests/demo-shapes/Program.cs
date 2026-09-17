using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Folds the twelve DEMO-* contracts through the real engine and prints the invoice each one produces.
//
// Seeding the contracts proves the DATA exists. This proves the eleven modes in BILLING-MODE-LIST.md
// actually come out of the engine as documented -- one line or seven, per model or across it, one
// invoice or two -- which is the only claim that matters.
//
// It builds its lines the same way SampleInvoice_Form does: the contract's own prices, invented
// readings, then ComputeCharge and Fold. So a mismatch here is a mismatch the user would have seen
// on the Sample Invoice screen.
static class DemoShapes
{
    const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

    static int Main(string[] args)
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
        try { Run(args.Length > 0 ? args[0] : "DEMO-%"); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run(string like)
    {
        var db = new AutoCount.Data.DBSetting(AutoCount.Data.DBServerType.SQL2000,
            "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        DataTable contracts = db.GetDataTable(
            "SELECT ContractKey, ContractNo, ISNULL(BillingFormatCode,'') AS Fmt, " +
            "ISNULL(UseNewLayout,'N') AS NewLayout, " +
            "ISNULL(RentalLineMode,'A') AS RLM, ISNULL(MeterLineMode,'S') AS MLM, " +
            "ISNULL(RentalSeparateInvoice,'N') AS RSep, ISNULL(BillingMode,'G') AS BM, " +
            "ISNULL([Description],'') AS Descr " +
            "FROM dbo.zSCP2_Contract WHERE ContractNo LIKE N'" + like.Replace("'","''") + "' ORDER BY ContractNo", false);

        bool legacyFold = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.LegacyRentalFold(db);
        // Every ladder in the book, master list and per-meter alike. Priced without it, a tiered meter
        // reads as its flat rate -- nothing at all when the ladder IS the price.
        _ladders = ServiceContractPhotocopier.Classes.ScpMultiPrice.LoadLadders(db);
        foreach (DataRow c in contracts.Rows)
        {
            long ck = Convert.ToInt64(c["ContractKey"]);
            string no = Convert.ToString(c["ContractNo"]);
            // The new rules carry their own switch; a contract on them names no format at all and
            // takes its shape from how its machines are grouped.
            bool newLayout = Convert.ToString(c["NewLayout"]).Trim().ToUpperInvariant() == "Y";
            bool hasFormat = newLayout || Convert.ToString(c["Fmt"]).Trim().Length > 0;
            char rlm = Convert.ToString(c["RLM"])[0];
            char mlm = Convert.ToString(c["MLM"])[0];
            // On the new layout there is no mode: the machines' groups are the whole answer.
            if (newLayout)
            {
                rlm = ServiceContractPhotocopier.Classes.ScpBillingFormat.LINE_BY_GROUP;
                mlm = ServiceContractPhotocopier.Classes.ScpBillingFormat.LINE_BY_GROUP;
            }
            bool rentalSep = Convert.ToString(c["RSep"]) == "Y";
            bool perMachine = Convert.ToString(c["BM"]) == "S";

            var lines = BuildLines(db, ck, hasFormat, no);
            // The agreed price for a merged line, stamped onto every machine on it -- the same two
            // passes MeterReadingIntegration_Form makes before it folds. Without them the harness
            // priced every machine at its own rate and could never have caught the loader whose key
            // format had drifted, which is exactly the bug that hid here for weeks.
            ApplyAgreedPrices(db, ck, lines);
            // The group's own tiers, priced over the group's copies -- the pass the billing run makes
            // between pricing the machines and reading their charges.
            var termsByCt = new Dictionary<long,
                Dictionary<string, ServiceContractPhotocopier.Classes.ScpLineTerms>>();
            termsByCt[ck] = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadTerms(db, ck);
            ServiceContractPhotocopier.Classes.ScpGroupLadder.Apply(lines, termsByCt, _ladders);
            // Waive, then minimum -- the order the billing run uses. Both read charges that have to
            // exist first, so they run once every copy line is priced.
            ServiceContractPhotocopier.Classes.ScpWaiveMeter.Apply(lines, 2026, 7);
            ServiceContractPhotocopier.Classes.ScpCommittedMin.ApplyMeterMinimums(lines);

            // Group into invoices first, then fold each one -- folding across an invoice split would
            // merge lines that never meet on paper. Same order as SampleInvoice_Form.
            var order = new List<string>();
            var byInv = new Dictionary<string, List<ServiceContractPhotocopier.Classes.MeterBillLine>>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in lines)
            {
                bool rentSide = l.IsRental || l.IsWaiveMeter;
                // A bill group is a separate invoice before anything else splits -- the run keys on
                // it (MeterReadingIntegration_Form), so the harness must too or it reports one
                // invoice where the customer gets two.
                string bg = (l.BillGroupCode ?? "").Trim();
                string inv = perMachine
                    ? (rentalSep && rentSide ? l.ItemName + " (rental)" : l.ItemName)
                    : (rentalSep && rentSide ? "rental" : "main");
                if (bg.Length > 0 && !perMachine) inv = bg + (rentalSep && rentSide ? " (rental)" : "");   // per machine outranks a bill group
                if (!byInv.ContainsKey(inv)) { order.Add(inv); byInv[inv] = new List<ServiceContractPhotocopier.Classes.MeterBillLine>(); }
                byInv[inv].Add(l);
            }

            int totalLines = 0;
            var per = new List<string>();
            foreach (string inv in order)
            {
                var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.FoldWith(
                    byInv[inv], rlm, mlm, hasFormat, legacyFold);
                totalLines += rows.Count;
                per.Add(rows.Count.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("=== " + no + "   " +
                              (newLayout ? "grouped by the machines"
                                         : (hasFormat ? "format " + Convert.ToString(c["Fmt"]) : "(legacy)")) +
                              "   rental " + rlm + " / meters " + mlm +
                              (rentalSep ? "   rental on its own invoice" : "") +
                              (perMachine ? "   one invoice per machine" : ""));
            Console.WriteLine("    " + Convert.ToString(c["Descr"]));
            Console.WriteLine("    -> " + order.Count + " invoice(s), " + totalLines +
                              " line(s) total  [" + string.Join(" + ", per.ToArray()) + "]");

            int fRental = 0, fBk = 0, fCl = 0;
            foreach (string inv in order)
                foreach (var row in ServiceContractPhotocopier.Classes.ScpInvoiceLayout.FoldWith(
                             byInv[inv], rlm, mlm, hasFormat, legacyFold))
                {
                    if (row.Leader.IsWaiveMeter || row.Leader.IsCommittedMin) continue;
                    if (row.Leader.IsRental || row.Leader.IsFlat) fRental++;
                    else if (row.Leader.ColorLabel == "Colour") fCl++;
                    else fBk++;
                }
            ScreenCheck.Report(lines, fRental, fBk, fCl);

            foreach (string inv in order)
            {
                var rows = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.FoldWith(
                    byInv[inv], rlm, mlm, hasFormat, legacyFold);
                Console.WriteLine("    " + inv + ":");
                foreach (var row in rows)
                {
                    string kind = row.Leader.IsRental ? "RENTAL"
                                : row.Leader.IsWaiveMeter ? "WAIVE"
                                : row.Leader.IsCommittedMin ? "COMMIT"
                                : (row.Leader.ColorLabel == "Colour" ? "CL" : "BK");
                    Console.WriteLine(string.Format(
                        "       {0,-6} {1,2} machine(s)  qty {2,9:n0}  @ {3,-9} = {4,10:n2}   {5}",
                        kind, row.Members.Count, row.PrintQty, row.PrintUnitPrice, row.PrintAmount,
                        Models(row)));

                    // What the line will actually SAY -- composed by the same method the posted
                    // invoice and the Sample Invoice preview both call, so this is the printed
                    // wording and not a summary of it.
                    string desc = ServiceContractPhotocopier.Classes.ScpInvoiceBuilder
                        .ComposeFoldedDescription(row, kind == "RENTAL" ? "MONTHLY RENTAL"
                            : kind == "WAIVE" ? "RENTAL WAIVE"
                            : kind == "COMMIT" ? "MINIMUM COMMITTED PRINT CHARGES"
                            : kind == "CL" ? "COLOUR COPY + PRINT A4 & A3" : "BLACK COPY + PRINT A4 & A3");
                    foreach (string dl in desc.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                        Console.WriteLine("              | " + dl);
                    // The sentence a minimum or a waive prints under itself. It is the whole point of
                    // those two lines -- the figure alone never says why it is what it is.
                    string note = row.IsMerged && row.Leader.IsCommittedMin
                        ? ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.MergedMinimumNote(row)
                        : (row.IsMerged && row.Leader.IsWaiveMeter
                            ? ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.MergedWaiveNote(row)
                            : row.Leader.StrategyNote);
                    if (!string.IsNullOrEmpty(note))
                        Console.WriteLine("              | " + note);
                    // The breakdown rows the invoice prints underneath -- readings for a meter line,
                    // and for a merged minimum what each machine owed, printed and is short.
                    foreach (string br in ServiceContractPhotocopier.Classes.ScpInvoiceBuilder
                                             .ComposeReadingRows(row, new DateTime(2026, 7, 31)))
                    {
                        Console.WriteLine("              |   " + br);
                        Check(br, "breakdown");
                    }
                    Check(desc, "description");
                    if (!string.IsNullOrEmpty(note)) Check(note, "note");
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine(_tooLong == 0
            ? "Every description fits the 100-character column."
            : "!! " + _tooLong + " line(s) would be REJECTED by AutoCount -- Generate would fail.");
        Console.WriteLine("Contracts folded. Compare each shape against BILLING-SCENARIOS.md.");
    }

    static int _tooLong;

    /// <summary>One line that will land in IVDTL.Description, measured against what that column
    /// takes. AutoCount throws rather than truncating, so a single long line stops a whole
    /// billing run -- better to hear it here than from a user pressing Generate.</summary>
    static void Check(string text, string what)
    {
        string[] parts = (text ?? "").Split(new char[] { '\n' });
        foreach (string one in parts)
            if (one.TrimEnd().Length > 100)
            {
                _tooLong++;
                Console.WriteLine("              !! " + what + " is " + one.TrimEnd().Length +
                                  " chars, the column takes 100: " + one.TrimEnd());
            }
    }

    static string Models(ServiceContractPhotocopier.Classes.ScpFoldedLine row)
    {
        var seen = new List<string>();
        foreach (var m in row.Members)
        {
            string s = (m.ModelCode ?? "").Trim();
            if (s.Length > 0 && !seen.Contains(s)) seen.Add(s);
        }
        return string.Join(", ", seen.ToArray());
    }

    static List<ServiceContractPhotocopier.Classes.ScpFoldedLine> Unfolded(
        List<ServiceContractPhotocopier.Classes.MeterBillLine> lines)
    {
        var rows = new List<ServiceContractPhotocopier.Classes.ScpFoldedLine>();
        foreach (var l in lines) rows.Add(new ServiceContractPhotocopier.Classes.ScpFoldedLine(l));
        return rows;
    }

    static System.Collections.Generic.Dictionary<string,
        System.Collections.Generic.List<decimal[]>> _ladders;

    /// <summary>Stamps the contract's agreed line prices onto the machines, rental side and copies
    /// side, before anything is folded. A priced group arrives at the fold already agreeing, which is
    /// what lets it come out as ONE row -- machines at different rates cannot share a price cell.</summary>
    static void ApplyAgreedPrices(AutoCount.Data.DBSetting db, long ck,
                                  List<ServiceContractPhotocopier.Classes.MeterBillLine> lines)
    {
        List<long> keys = new List<long>(); keys.Add(ck);
        var prices = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadForContracts(db, keys);
        var modes  = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.LoadRentalModes(db, keys);
        var terms  = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.LoadTerms(db, ck);
        char mode  = modes.ContainsKey(ck) ? modes[ck] : 'S';

        foreach (ServiceContractPhotocopier.Classes.MeterBillLine l in lines)
        {
            if (l.IsWaiveMeter || l.IsCommittedMin) continue;
            if (l.IsRental || l.IsFlat)
            {
                decimal? p = ServiceContractPhotocopier.Classes.ScpRentalGroupPrice.PriceFor(
                    prices, ck, mode, l.ModelCode, l.MergeGroupCode);
                if (!p.HasValue) continue;
                // The agreed figure IS the rental: a minimum left on the meter would outrank it,
                // because a flat charge bills the greater of the two.
                l.Rate = p.Value; l.MinCharges = 0m;
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
                continue;
            }
            string grp = (l.MergeGroupCodeMeter ?? "").Trim();
            if (grp.Length == 0) continue;                       // its own row, its own rate
            if ((l.MultiPriceCode ?? "").Trim().Length > 0) continue;   // a ladder outranks a flat figure
            ServiceContractPhotocopier.Classes.ScpLineTerms t;
            if (!terms.TryGetValue(ServiceContractPhotocopier.Classes.ScpLineTerms.Key(
                    ServiceContractPhotocopier.Classes.ScpLineTerms.SIDE_METER, grp), out t)) continue;
            decimal agreed = l.ColorLabel == "Colour" ? t.ClPrice : t.BkPrice;
            if (agreed <= 0m) continue;
            l.Rate = agreed;
            ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
        }
    }

    static List<ServiceContractPhotocopier.Classes.MeterBillLine> BuildLines(
        AutoCount.Data.DBSetting db, long ck, bool hasFormat, string contractNo)
    {
        // What the machine line names, read live from the contract's format the way Generate reads it.
        char machineLine = ServiceContractPhotocopier.Classes.ScpBillingFormat.MACHINE_LINE_BOTH;
        DataTable ft = db.GetDataTable(
            "SELECT ISNULL(bf.MachineLineShows,'B') AS MachineLineShows FROM dbo.zSCP2_Contract c " +
            "LEFT JOIN dbo.zSCP2_BillingFormat bf ON bf.FormatCode = c.BillingFormatCode " +
            "WHERE c.ContractKey = " + ck, false);
        if (ft.Rows.Count > 0)
        {
            string mls = Convert.ToString(ft.Rows[0]["MachineLineShows"]).Trim();
            if (mls.Length > 0) machineLine = char.ToUpperInvariant(mls[0]);
        }

        var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
        DataTable t = db.GetDataTable(
            "SELECT i.ItemKey, i.ServiceItemNo, ISNULL(i.ItemCode,'') AS Model, " +
            "ISNULL(i.SerialNumber,'') AS Serial, ISNULL(i.MergeGroupCode,'') AS Grp, " +
            "ISNULL(i.MergeGroupCodeMeter,'') AS GrpM, " +
            "ISNULL(i.LineGroupCode,'') AS Lbl, ISNULL(i.BillGroupCode,'') AS BillGrp, " +
            "m.MeterTypeCode, ISNULL(m.MeterRole,'') AS Role, ISNULL(m.[Description],'') AS Descr, " +
            "ISNULL(m.ChargesRate,0) AS Rate, ISNULL(m.MinimumCharges,0) AS MinChg, " +
            "ISNULL(m.FOCQty,0) AS Foc, ISNULL(m.RebateQtyInPercent,0) AS Reb, " +
            "CASE WHEN EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeterPrice pm WHERE pm.ItemMeterKey = m.ItemMeterKey) " +
            "     THEN '#' + CAST(m.ItemMeterKey AS varchar(20)) " +
            "     ELSE ISNULL(m.MeterMultiPriceCode,'') END AS Ladder, ISNULL(m.WaiveScope,'BKCL') AS WScope, " +
            "ISNULL(m.WaiveTargetAmount,0) AS WTarget, ISNULL(m.WaiveFirstNMonths,0) AS WFirstN, " +
            "ISNULL(m.InitialReading,0) AS Init, " +
            "ISNULL(m.WaivePartialThreshold,0) AS WPartT, ISNULL(m.WaivePartialAmount,0) AS WPartA, " +
            "ISNULL(m.CommitScope,'S') AS CScope " +
            "FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey " +
            "WHERE i.ContractKey = " + ck + " AND ISNULL(i.Inactive,'N') <> 'Y' " +
            "ORDER BY i.Pos, i.ItemKey, m.ItemMeterKey", false);

        bool showModel = true, showSerial = true, showUnits = true;
        try
        {
            DataTable sm = db.GetDataTable(
                "SELECT ISNULL(ShowModelOnLine,'Y') AS M, ISNULL(ShowSerialOnLine,'Y') AS S, " +
                "       ISNULL(ShowUnitsOnLine,'Y') AS U " +
                "  FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck, false);
            if (sm.Rows.Count > 0)
            {
                showModel = Convert.ToString(sm.Rows[0]["M"]).Trim().ToUpperInvariant() != "N";
                showSerial = Convert.ToString(sm.Rows[0]["S"]).Trim().ToUpperInvariant() != "N";
                showUnits = Convert.ToString(sm.Rows[0]["U"]).Trim().ToUpperInvariant() != "N";
            }
        }
        catch { }

        DateTime period = new DateTime(2026, 7, 31);
        long lastItem = -1; int seed = 0;
        foreach (DataRow r in t.Rows)
        {
            long ik = Convert.ToInt64(r["ItemKey"]);
            if (ik != lastItem) { seed++; lastItem = ik; }
            string type = Convert.ToString(r["MeterTypeCode"]).Trim();
            string role = Convert.ToString(r["Role"]).Trim().ToUpperInvariant();
            if (type.Length == 0) continue;

            var l = new ServiceContractPhotocopier.Classes.MeterBillLine();
            l.ContractKey = ck; l.ItemKey = ik;
            l.ItemName = Convert.ToString(r["ServiceItemNo"]);
            l.SerialNumber = Convert.ToString(r["Serial"]);
            l.ModelCode = Convert.ToString(r["Model"]);
            l.MergeGroupCode = Convert.ToString(r["Grp"]);
            l.MergeGroupCodeMeter = Convert.ToString(r["GrpM"]);
            l.LineGroupCode = Convert.ToString(r["Lbl"]);
            l.BillGroupCode = Convert.ToString(r["BillGrp"]).Trim();
            l.MeterTypeCode = type; l.ACItemCode = type;
            l.MeterTypeName = Convert.ToString(r["Descr"]);
            l.NewMoneyRules = hasFormat;
            l.MachineLineShows = machineLine;
            // The two ticks, read from the contract like everything else on the line.
            l.ShowModel = showModel;
            l.ShowSerial = showSerial;
            l.ShowUnits = showUnits;
            l.AuditDate = period; l.LastDate = period.AddMonths(-1); l.PeriodEnd = period;
            l.MinCharges = Convert.ToDecimal(r["MinChg"]);
            decimal openAt = Convert.ToDecimal(r["Init"]);
            l.Rate = Convert.ToDecimal(r["Rate"]);
            l.Foc = Convert.ToDecimal(r["Foc"]);
            l.RebatePct = Convert.ToDecimal(r["Reb"]);
            l.MultiPriceCode = Convert.ToString(r["Ladder"]);
            l.WaiveScope = Convert.ToString(r["WScope"]);
            l.CommitScope = Convert.ToString(r["CScope"]);
            // The waive's own terms, on the line. Without them ScpWaiveMeter reads "no window, no
            // target" and fires every waive every month -- which is what the harness used to report.
            l.WaiveTargetAmount = Convert.ToDecimal(r["WTarget"]);
            l.WaiveFirstNMonths = Convert.ToInt32(r["WFirstN"]);
            l.WaivePartialThreshold = Convert.ToDecimal(r["WPartT"]);
            l.WaivePartialAmount = Convert.ToDecimal(r["WPartA"]);

            bool isWaive = ServiceContractPhotocopier.Classes.ScpStrategy.IsWaiveRole(role, false);
            bool isCommit = ServiceContractPhotocopier.Classes.ScpStrategy.IsCommittedMinRole(role, type, l.MinCharges);
            bool isRental = !isWaive && !isCommit &&
                ServiceContractPhotocopier.Classes.ScpStrategy.IsRentalRole(role, type);

            if (isRental || isWaive || isCommit)
            {
                l.IsFlat = true; l.IsRental = isRental; l.IsWaiveMeter = isWaive;
                l.RentalMonths = 36; l.RentalStartDate = period.AddMonths(-12);
                if (isCommit)
                {
                    l.IsCommittedMin = true; l.CommittedAmount = l.MinCharges;
                    l.AlwaysBill = true; l.Charge = 0m;
                }
                else if (isWaive)
                {
                    // Priced 0 until ScpWaiveMeter has its say -- it runs after every copy line is
                    // priced, because whether a waive fires is decided by what those copies came to.
                    l.Charge = 0m;
                    l.AlwaysBill = true;
                    decimal amt = Math.Abs(l.MinCharges != 0m ? l.MinCharges : l.Rate);
                    decimal at = Convert.ToDecimal(r["WTarget"]);
                    int n = Convert.ToInt32(r["WFirstN"]);
                    string terms = n > 0
                        ? "free for the first " + n + " month" + (n == 1 ? "" : "s")
                        : (at > 0m ? "when this month's copies reach " + at.ToString("n2") : "every month");
                    l.StrategyNote = "RENTAL WAIVE " + amt.ToString("n2") + " · " + terms;
                }
                else
                {
                    ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
                }
            }
            else
            {
                l.ColorLabel = role == "CL" ? "Colour" : (role == "BK" ? "Black" : "Usage");
                // The payload the user pastes into TEST Fetch, if one exists for this contract --
                // so what the harness reports and what Generate produces come off the SAME numbers.
                // Without it the two disagree for no reason anybody can see, and the harness stops
                // being evidence about the invoice sitting on the screen.
                decimal[] fed = Fed(contractNo, l.ItemName, role, openAt);
                if (fed != null) { l.Last = fed[0]; l.Current = fed[1]; }
                else
                {
                    decimal usage = role == "CL" ? 900 + seed * 137 : 4200 + seed * 613;
                    l.Last = 100000 + seed * 1000;
                    l.Current = l.Last + usage;
                }
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, _ladders);
            }
            lines.Add(l);
        }
        return lines;
    }

    // ---- the pasted payload, read once per contract ------------------------------------------
    static string _fedFor;
    static Dictionary<string, decimal[]> _fed;   // "<machine>|BK" -> { totalBK, totalCL }

    /// <summary>The reading this machine's meter should carry, taken from
    /// tests\meter-json\&lt;contract&gt;.json when that file exists. Returns { last, current };
    /// null when there is no payload, and the harness falls back to its invented usage.</summary>
    static decimal[] Fed(string contractNo, string machine, string role, decimal openAt)
    {
        if (_fedFor != contractNo)
        {
            _fedFor = contractNo;
            _fed = null;
            string path = Path.Combine(
                Path.GetDirectoryName(typeof(DemoShapes).Assembly.Location) ?? ".",
                Path.Combine("..", Path.Combine("meter-json", contractNo + ".json")));
            if (File.Exists(path))
            {
                _fed = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
                foreach (Newtonsoft.Json.Linq.JObject o in
                         Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(path)))
                {
                    string code = (string)o["Code"];
                    if (string.IsNullOrEmpty(code)) continue;
                    _fed[code] = new decimal[] { (decimal)o["TotalBK"], (decimal)o["TotalCL"] };
                }
            }
        }
        if (_fed == null) return null;
        decimal[] totals;
        if (!_fed.TryGetValue(machine, out totals)) return null;
        decimal current = role == "CL" ? totals[1] : totals[0];
        if (current <= 0m) return null;                  // no counter of that colour on this machine
        return new decimal[] { openAt, current };
    }
}
