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
            "ISNULL(RentalLineMode,'A') AS RLM, ISNULL(MeterLineMode,'S') AS MLM, " +
            "ISNULL(RentalSeparateInvoice,'N') AS RSep, ISNULL(BillingMode,'G') AS BM, " +
            "ISNULL([Description],'') AS Descr " +
            "FROM dbo.zSCP2_Contract WHERE ContractNo LIKE N'" + like.Replace("'","''") + "' ORDER BY ContractNo", false);

        bool legacyFold = ServiceContractPhotocopier.Classes.ScpInvoiceLayout.LegacyRentalFold(db);
        foreach (DataRow c in contracts.Rows)
        {
            long ck = Convert.ToInt64(c["ContractKey"]);
            string no = Convert.ToString(c["ContractNo"]);
            bool hasFormat = Convert.ToString(c["Fmt"]).Trim().Length > 0;
            char rlm = Convert.ToString(c["RLM"])[0];
            char mlm = Convert.ToString(c["MLM"])[0];
            bool rentalSep = Convert.ToString(c["RSep"]) == "Y";
            bool perMachine = Convert.ToString(c["BM"]) == "S";

            var lines = BuildLines(db, ck, hasFormat);

            // Group into invoices first, then fold each one -- folding across an invoice split would
            // merge lines that never meet on paper. Same order as SampleInvoice_Form.
            var order = new List<string>();
            var byInv = new Dictionary<string, List<ServiceContractPhotocopier.Classes.MeterBillLine>>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in lines)
            {
                bool rentSide = l.IsRental || l.IsWaiveMeter;
                string inv = perMachine
                    ? (rentalSep && rentSide ? l.ItemName + " (rental)" : l.ItemName)
                    : (rentalSep && rentSide ? "rental" : "main");
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
            Console.WriteLine("=== " + no + "   format " + (hasFormat ? Convert.ToString(c["Fmt"]) : "(legacy)") +
                              "   rental " + rlm + " / meters " + mlm +
                              (rentalSep ? "   rental on its own invoice" : "") +
                              (perMachine ? "   one invoice per machine" : ""));
            Console.WriteLine("    " + Convert.ToString(c["Descr"]));
            Console.WriteLine("    -> " + order.Count + " invoice(s), " + totalLines +
                              " line(s) total  [" + string.Join(" + ", per.ToArray()) + "]");

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
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine("Twelve contracts folded. Compare each shape against BILLING-MODE-LIST.md.");
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

    static List<ServiceContractPhotocopier.Classes.MeterBillLine> BuildLines(
        AutoCount.Data.DBSetting db, long ck, bool hasFormat)
    {
        var lines = new List<ServiceContractPhotocopier.Classes.MeterBillLine>();
        DataTable t = db.GetDataTable(
            "SELECT i.ItemKey, i.ServiceItemNo, ISNULL(i.ItemCode,'') AS Model, " +
            "ISNULL(i.SerialNumber,'') AS Serial, ISNULL(i.MergeGroupCode,'') AS Grp, " +
            "m.MeterTypeCode, ISNULL(m.MeterRole,'') AS Role, ISNULL(m.[Description],'') AS Descr, " +
            "ISNULL(m.ChargesRate,0) AS Rate, ISNULL(m.MinimumCharges,0) AS MinChg, " +
            "ISNULL(m.FOCQty,0) AS Foc, ISNULL(m.RebateQtyInPercent,0) AS Reb, " +
            "ISNULL(m.MeterMultiPriceCode,'') AS Ladder, ISNULL(m.WaiveScope,'BKCL') AS WScope, " +
            "ISNULL(m.CommitScope,'S') AS CScope " +
            "FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_ItemMeter m ON m.ItemKey = i.ItemKey " +
            "WHERE i.ContractKey = " + ck + " AND ISNULL(i.Inactive,'N') <> 'Y' " +
            "ORDER BY i.Pos, i.ItemKey, m.ItemMeterKey", false);

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
            l.MeterTypeCode = type; l.ACItemCode = type;
            l.MeterTypeName = Convert.ToString(r["Descr"]);
            l.NewMoneyRules = hasFormat;
            l.AuditDate = period; l.LastDate = period.AddMonths(-1); l.PeriodEnd = period;
            l.MinCharges = Convert.ToDecimal(r["MinChg"]);
            l.Rate = Convert.ToDecimal(r["Rate"]);
            l.Foc = Convert.ToDecimal(r["Foc"]);
            l.RebatePct = Convert.ToDecimal(r["Reb"]);
            l.MultiPriceCode = Convert.ToString(r["Ladder"]);
            l.WaiveScope = Convert.ToString(r["WScope"]);
            l.CommitScope = Convert.ToString(r["CScope"]);

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
                    l.Charge = -Math.Abs(l.MinCharges != 0m ? l.MinCharges : l.Rate);
                }
                else
                {
                    ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, null);
                }
            }
            else
            {
                decimal usage = role == "CL" ? 900 + seed * 137 : 4200 + seed * 613;
                l.ColorLabel = role == "CL" ? "Colour" : (role == "BK" ? "Black" : "Usage");
                l.Last = 100000 + seed * 1000;
                l.Current = l.Last + usage;
                ServiceContractPhotocopier.Classes.ScpInvoiceBuilder.ComputeCharge(l, null);
            }
            lines.Add(l);
        }
        return lines;
    }
}
