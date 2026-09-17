using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

// The Inter-Billing board, end to end against two real books.
//
//   subsidiary (this book, bills)   AED_ATPTEST   -- the book ShadowMain opens
//   HQ (reads the meters)           AED_ASNDUMMY
//
// setup : connect this book to HQ, take HQ-2026-001 / 002 / 003 / 006 through the product's own Take,
//         then have HQ add a machine to 002 -- so each board state exists once.
// check : load the board and check every state, the jobs it builds, the reading write, the screen.
// clean : remove what setup made in this book and HQ-2026-002..006 extras in HQ.
internal static class InterBillBoardCheck
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    private const string Server = "localhost,1433";
    private const string User = "sa";
    private const string Pass = "rs6663";
    private static string Sub = "AED_ATPTEST";
    private static string Hq = "AED_ASNDUMMY";
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
        string mode = args.Length > 0 ? args[0] : "check";
        DBSetting db = new DBSetting(DBServerType.SQL2000, Server, User, Pass, Sub, false);
        Console.WriteLine();
        Console.WriteLine("   mode " + mode + "   subsidiary " + Sub + "   HQ " + Hq);
        if (mode == "clean") { Clean(db); return 0; }
        if (mode == "setup") { Setup(db); return _fail == 0 ? 0 : 1; }
        Check(db);
        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "   ALL OK" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    // ───────────────────────────── setup ─────────────────────────────

    private static ServiceContractPhotocopier.Classes.ScpInterBillBook Book(DBSetting db)
    {
        foreach (ServiceContractPhotocopier.Classes.ScpInterBillBook b in ServiceContractPhotocopier.Classes.ScpInterBillBooks.LoadAll(db))
            if (b.DatabaseName == Hq) return b;
        return null;
    }

    private static void Setup(DBSetting db)
    {
        Clean(db);
        ServiceContractPhotocopier.Classes.ScpInterBillBook book = Book(db);
        if (book == null) book = new ServiceContractPhotocopier.Classes.ScpInterBillBook();
        book.Alias = "HQ";
        book.ServerName = Server; book.DatabaseName = Hq; book.DbUser = User; book.Password = Pass;
        book.MarginPercent = 0m;
        string msg;
        bool ok = ServiceContractPhotocopier.Classes.ScpInterBillBooks.Test(db, book, out msg);
        Expect("HQ book reached (" + msg.Replace(Environment.NewLine, " ") + ")", ok);
        ServiceContractPhotocopier.Classes.ScpInterBillBooks.Save(db, book);
        book = Book(db);
        ServiceContractPhotocopier.Classes.ScpInterBillBooks.SetHqBook(db, book.BookKey);   // what ticking HQ in Setup does
        string cs = ServiceContractPhotocopier.Classes.ScpInterBillBooks.ConnectionStringOf(db, book);

        List<ServiceContractPhotocopier.Classes.IbContractRow> rows =
            ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false);
        string[][] takes = new string[][] {
            new string[] { "HQ-2026-007", "3000-00001" },
            new string[] { "HQ-2026-002", "3000-B0023" },
            new string[] { "HQ-2026-003", "3000-C0005" },
            new string[] { "HQ-2026-006", "3000-D0001" } };
        foreach (string[] t in takes)
        {
            ServiceContractPhotocopier.Classes.IbContractRow r = Find(rows, t[0], false);
            if (r == null) { Fail(t[0] + " is not on HQ's list"); continue; }
            ServiceContractPhotocopier.Classes.ScpTakeResult res =
                ServiceContractPhotocopier.Classes.ScpInterBillBoard.Take(db, book, cs, r, t[1], "ADMIN");
            Expect("took " + t[0] + " for " + t[1] + (res.Ok ? "" : " (" + res.Error + ")"), res.Ok);
        }

        // HQ puts a fifth machine on HQ-2026-002 after the take -- the one write into HQ's book, and
        // only into that contract, checked by number at the moment of writing.
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "DECLARE @ck BIGINT = (SELECT ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'HQ-2026-002'); " +
                "IF @ck IS NULL RAISERROR('HQ-2026-002 missing', 16, 1); " +
                "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_Item WHERE ContractKey = @ck AND SerialNumber = N'HQB-005') BEGIN " +
                " INSERT INTO dbo.zSCP2_Item (ContractKey, ServiceItemNo, ItemCode, SerialNumber, [Description], Pos, MergeGroupCode, MergeGroupCodeMeter, " +
                "  BillGroupCode, LineGroupCode, Inactive, IsGroupItem, MachineMode, ServiceStartDate, ServiceExpiryDate, LastModified) " +
                " VALUES (@ck, N'HQ-2026-002-005', N'iR-ADV DX 4745i', N'HQB-005', N'iR-ADV DX 4745i / HQB-005', 5, '', '', '', '', 'N', 'N', 'ONLINE', '2026-09-11', '2028-12-31', GETDATE()); " +
                " DECLARE @ik BIGINT = SCOPE_IDENTITY(); " +
                " INSERT INTO dbo.zSCP2_ItemMeter (ItemKey, MeterTypeCode, [Description], MeterRole, MachineSerialNo, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                "  RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis, WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, " +
                "  WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified) VALUES " +
                "  (@ik, N'RENTAL', N'MONTHLY RENTAL', N'RENTAL', '', 0, 520, '', 0, 0, 0, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()), " +
                "  (@ik, N'BK', N'BLACK COPY', N'BK', '', 0, 0.018, '', 0, 0, 31400, 0, 'A', 0, 0, 100, 'S', 0, 0, 'S', GETDATE()); END", cn))
                cmd.ExecuteNonQuery();
        }
        Say("HQ", "HQB-005 added to HQ-2026-002");
    }

    private static void Clean(DBSetting db)
    {
        string[] nos = new string[] { "HQ-2026-002", "HQ-2026-003", "HQ-2026-004", "HQ-2026-005", "HQ-2026-006", "HQ-2026-007" };
        foreach (string no in nos)
        {
            object o = db.ExecuteScalar("SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'" + no + "'");
            if (o == null || o == DBNull.Value) continue;
            long ck = Convert.ToInt64(o);
            // Refuse to remove anything that is not a contract taken from HQ.
            object linked = db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_InterBillLink WHERE EntityType='CONTRACT' AND LocalKey = " + ck);
            if (linked == null || Convert.ToInt32(linked) == 0) { Say("left", no + " (not a taken contract)"); continue; }
            object inv = db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                                          "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + ck + " AND e.InvoicedDocKey IS NOT NULL");
            if (inv != null && Convert.ToInt32(inv) > 0) { Say("left", no + " (it has invoices -- delete them first)"); continue; }
            db.ExecuteNonQuery("DELETE e FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE m FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Item WHERE ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_InterBillLink WHERE RootContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck);
            Say("removed", no);
        }
        db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_InterBillLink WHERE LocalKey = 0 OR RootContractKey NOT IN (SELECT ContractKey FROM dbo.zSCP2_Contract)");
        ServiceContractPhotocopier.Classes.ScpInterBillBook book = Book(db);
        if (book != null)
        {
            using (SqlConnection cn = new SqlConnection(ServiceContractPhotocopier.Classes.ScpInterBillBooks.ConnectionStringOf(db, book)))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE m FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE c.ContractNo = N'HQ-2026-002' AND i.SerialNumber = N'HQB-005'; " +
                    "DELETE i FROM dbo.zSCP2_Item i JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE c.ContractNo = N'HQ-2026-002' AND i.SerialNumber = N'HQB-005';", cn))
                    cmd.ExecuteNonQuery();
            }
            Say("HQ", "HQB-005 removed from HQ-2026-002");
        }
    }

    // ───────────────────────────── check ─────────────────────────────

    private static void Check(DBSetting db)
    {
        ServiceContractPhotocopier.Classes.ScpInterBillBook book = Book(db);
        if (book == null) { Fail("no HQ book in " + Sub + " -- run setup"); return; }
        string cs = ServiceContractPhotocopier.Classes.ScpInterBillBooks.ConnectionStringOf(db, book);
        List<ServiceContractPhotocopier.Classes.IbContractRow> rows =
            ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false);
        Console.WriteLine();
        foreach (ServiceContractPhotocopier.Classes.IbContractRow r in rows)
        {
            if (!r.HqContractNo.StartsWith("HQ-2026-")) continue;
            Console.WriteLine("   " + r.HqContractNo.PadRight(13) + (r.Taken ? r.LocalNo : "—").PadRight(13) +
                ("m " + r.Machines).PadRight(6) + (r.Taken ? (r.MetersRead + "/" + r.MetersTotal) : "").PadLeft(6) +
                (r.Taken ? r.Amount.ToString("n2") : "").PadLeft(12) + "   " + r.StatusText + (r.Error.Length > 0 ? "   ERR " + r.Error : ""));
        }
        Console.WriteLine();

        ServiceContractPhotocopier.Classes.IbContractRow c1 = Find(rows, "HQ-2026-007", true);
        ServiceContractPhotocopier.Classes.IbContractRow c0 = Find(rows, "HQ-2026-001", false);
        ServiceContractPhotocopier.Classes.IbContractRow c2 = Find(rows, "HQ-2026-002", true);
        ServiceContractPhotocopier.Classes.IbContractRow c3 = Find(rows, "HQ-2026-003", true);
        ServiceContractPhotocopier.Classes.IbContractRow c4 = Find(rows, "HQ-2026-004", false);
        ServiceContractPhotocopier.Classes.IbContractRow c5 = Find(rows, "HQ-2026-005", false);
        ServiceContractPhotocopier.Classes.IbContractRow c6 = Find(rows, "HQ-2026-006", true);

        Expect("HQ-2026-007 is Ready", c1 != null && c1.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.READY);
        Expect("HQ-2026-007 reads 6 of 6 from HQ", c1 != null && c1.MetersRead == 6 && c1.MetersTotal == 6);
        Expect("HQ-2026-001 is Not taken", c0 != null && c0.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.NOT_TAKEN);
        Expect("HQ-2026-002 is Changed at HQ", c2 != null && c2.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.CHANGED);
        bool added = false;
        if (c2 != null) foreach (ServiceContractPhotocopier.Classes.IbChange ch in c2.Changes)
            if (ch.Kind == ServiceContractPhotocopier.Classes.IbChange.MACHINE_ADDED && ch.Which == "HQB-005") added = true;
        Expect("HQ-2026-002 reports HQB-005 added, with 'Add HQB-005 to ...'", added);
        Expect("HQ-2026-003 is Unpriced (2)", c3 != null && c3.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.UNPRICED && c3.Unpriced == 2);
        Expect("HQ-2026-004 is Not taken", c4 != null && c4.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.NOT_TAKEN);
        Expect("HQ-2026-005 (ended) is hidden", c5 == null);
        Expect("HQ-2026-006 is Waiting HQ reading (2)", c6 != null && c6.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.WAITING && c6.Missing == 2);

        // Readings: HQ's figure, HQ's source and date, the local baseline as Last.
        if (c1 != null)
        {
            int pums = 0, keyed = 0; bool lastOk = true;
            foreach (ServiceContractPhotocopier.Classes.IbReadingRow x in c1.Readings)
            {
                if (x.AtHq == "PUMS · 13 Sep") pums++;
                if (x.AtHq == "Keyed · 12 Sep") keyed++;
                if (x.Last <= 0m || x.Copies <= 0m) lastOk = false;
            }
            Expect("HQ-2026-007 readings say 'PUMS · 13 Sep' (4) and 'Keyed · 12 Sep' (2)", pums == 4 && keyed == 2);
            Expect("HQ-2026-007 every counter has a baseline and copies", lastOk);
            decimal copies = 0m, amount = c1.Amount;
            foreach (ServiceContractPhotocopier.Classes.IbReadingRow x in c1.Readings) copies += x.Copies;
            Expect("HQ-2026-007 starts from HQ's August reading: copies 11,760 BK + 960 CL = 12,720 (" + copies.ToString("n0") + ")", copies == 12720m);
            Expect("HQ-2026-007 amount 1,140.00 + 211.68 + 192.00 = 1,543.68 (" + amount.ToString("n2") + ")", amount == 1543.68m);
        }
        if (c6 != null)
        {
            int noReading = 0;
            foreach (ServiceContractPhotocopier.Classes.IbReadingRow x in c6.Readings) if (x.AtHq == "No reading at HQ") noReading++;
            Expect("HQ-2026-006 shows 'No reading at HQ' on 2 counters", noReading == 2);
        }

        // Jobs: built for Ready only.
        Dictionary<long, string> snaps;
        List<string> skipped;
        List<ServiceContractPhotocopier.Classes.MeterInvoiceGenerator.InvoiceJob> jobs =
            ServiceContractPhotocopier.Classes.ScpInterBillBoard.BuildJobs(db, rows, 2026, 9, out snaps, out skipped);
        HashSet<long> jobContracts = new HashSet<long>();
        foreach (ServiceContractPhotocopier.Classes.MeterInvoiceGenerator.InvoiceJob j in jobs)
            foreach (ServiceContractPhotocopier.Classes.MeterBillLine ln in j.Lines) jobContracts.Add(ln.ContractKey);
        Say("jobs", jobs.Count + " jobs · skipped " + skipped.Count);
        Expect("jobs are built for HQ-2026-007", c1 != null && jobContracts.Contains(c1.LocalKey));
        Expect("no job for a contract that is not Ready", (c2 == null || !jobContracts.Contains(c2.LocalKey)) && (c3 == null || !jobContracts.Contains(c3.LocalKey)) && (c6 == null || !jobContracts.Contains(c6.LocalKey)));
        decimal lineCopies = 0m;
        foreach (ServiceContractPhotocopier.Classes.MeterInvoiceGenerator.InvoiceJob j in jobs)
            foreach (ServiceContractPhotocopier.Classes.MeterBillLine ln in j.Lines)
                if (c1 != null && ln.ContractKey == c1.LocalKey && !ln.IsFlat) lineCopies += ln.Usage;
        Expect("the jobs bill HQ's copies from the August baseline (12,720)", lineCopies == 12720m);

        // The reading write refuses, all or nothing, when a counter is locked in this book.
        if (c1 != null && c1.Readings.Count > 0)
        {
            long lockedKey = c1.Readings[0].LocalMeterKey;
            db.ExecuteNonQuery("INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, LastModified, LockedAt) " +
                               "VALUES (" + lockedKey + ", 2026, 9, 1, GETDATE(), 'ONLINE', 'N', GETDATE(), GETDATE())");
            bool threw = false;
            try { ServiceContractPhotocopier.Classes.ScpInterBillBoard.SaveHqReadings(db, c1, 2026, 9); }
            catch (InvalidOperationException) { threw = true; }
            object written = db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + c1.LocalKey + " AND e.Source = 'INTERBILL'");
            Expect("a locked counter stops the whole save, nothing written", threw && Convert.ToInt32(written) == 0);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_MeterEntry WHERE ItemMeterKey = " + lockedKey + " AND PeriodYear = 2026 AND PeriodMonth = 9 AND LockedAt IS NOT NULL");
        }

        // The reading write: into this book, stamped INTERBILL with HQ's date -- then taken back out.
        if (c1 != null)
        {
            int wrote = ServiceContractPhotocopier.Classes.ScpInterBillBoard.SaveHqReadings(db, c1, 2026, 9);
            object n = db.ExecuteScalar("SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + c1.LocalKey +
                " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 AND e.Source = 'INTERBILL' AND CONVERT(date, e.ReadingDate) IN ('2026-09-12','2026-09-13') AND e.InvoicedDocKey IS NULL");
            Expect("6 readings written into this book as INTERBILL with HQ's dates", wrote == 6 && Convert.ToInt32(n) == 6);
            db.ExecuteNonQuery("DELETE e FROM dbo.zSCP2_MeterEntry e JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                "JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + c1.LocalKey +
                " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 AND e.Source = 'INTERBILL' AND e.InvoicedDocKey IS NULL");
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_MeterReadingLog WHERE Source = 'INTERBILL' AND PeriodYear = 2026 AND PeriodMonth = 9 AND ItemMeterKey IN " +
                "(SELECT m.ItemMeterKey FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + c1.LocalKey + ")");
        }

        // The billing sequence: nobody picks the month; each contract is at its own next one.
        ServiceContractPhotocopier.Classes.IbContractRow s7 = Find(rows, "HQ-2026-007", true);
        if (s7 != null && s7.Sequence != null)
        {
            Expect("HQ-2026-007 bills Sep 2026, the month it was taken (" + s7.Period + ")", s7.Period == 202609 && s7.Open);
            Expect("HQ-2026-007 shows 8/36 billed before its first invoice here (" + s7.Sequence.ProgressText(202609) + ")", s7.Sequence.ProgressText(202609) == "8/36");
            Expect("HQ-2026-007 has 1 month due (" + s7.Due + ")", s7.Due == 1);

            // The invoice list: HQ-2026-007's September is one Ready invoice worth what the contract bills;
            // HQ-2026-006's waits for HQ's readings.
            List<ServiceContractPhotocopier.Classes.IbInvoiceRow> inv = ServiceContractPhotocopier.Classes.ScpInterBillBoard.Invoices(db, rows, 2026, 9);
            decimal inv7 = 0m; int n7 = 0, ready7 = 0, wait6 = 0;
            HashSet<string> keys7 = new HashSet<string>();
            foreach (ServiceContractPhotocopier.Classes.IbInvoiceRow v in inv)
            {
                if (v.Contract.HqContractNo == "HQ-2026-007") { n7++; inv7 += v.Amount; keys7.Add(v.JobKey); if (v.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.READY) ready7++; }
                if (v.Contract.HqContractNo == "HQ-2026-006" && v.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.WAITING) wait6++;
            }
            Expect("invoice list: HQ-2026-007 has " + n7 + " invoice(s), all Ready, RM " + inv7.ToString("n2"), n7 >= 1 && ready7 == n7 && inv7 == s7.Amount);
            Expect("invoice list: HQ-2026-006 waits for HQ readings (" + wait6 + ")", wait6 >= 1);
            List<string> keySkipped; Dictionary<long, string> keySnaps;
            List<ServiceContractPhotocopier.Classes.IbContractRow> just7 = new List<ServiceContractPhotocopier.Classes.IbContractRow>();
            just7.Add(s7);
            List<ServiceContractPhotocopier.Classes.MeterInvoiceGenerator.InvoiceJob> byKey =
                ServiceContractPhotocopier.Classes.ScpInterBillBoard.BuildJobs(db, just7, 2026, 9, keys7, out keySnaps, out keySkipped);
            Expect("generating HQ-2026-007's invoices by name builds " + byKey.Count + " job(s)", byKey.Count == n7);
            long k7 = s7.LocalKey;
            int from7 = ServiceContractPhotocopier.Classes.ScpBillingSequence.BillFromOf(db, k7);
            Expect("HQ-2026-007 stores Sep 2026 as its first month billed here (" + from7 + ")", from7 == 202609);

            // Billing starts a month earlier: August comes first and two months are due.
            ServiceContractPhotocopier.Classes.ScpBillingSequence.SetBillFrom(db, k7, 202608);
            ServiceContractPhotocopier.Classes.IbContractRow a = Find(ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false), "HQ-2026-007", true);
            Expect("bill from Aug 2026: Aug first, 2 due, 7/36 (" + a.Period + ", " + a.Due + ", " + a.Sequence.ProgressText(a.Period) + ")",
                   a.Period == 202608 && a.Due == 2 && a.Sequence.ProgressText(a.Period) == "7/36");
            List<string> monthSkipped; Dictionary<long, string> monthSnaps;
            List<ServiceContractPhotocopier.Classes.IbContractRow> one = new List<ServiceContractPhotocopier.Classes.IbContractRow>();
            one.Add(a);
            List<ServiceContractPhotocopier.Classes.MeterInvoiceGenerator.InvoiceJob> wrongMonth =
                ServiceContractPhotocopier.Classes.ScpInterBillBoard.BuildJobs(db, one, 2026, 9, out monthSnaps, out monthSkipped);
            Expect("no September invoice while August is open (" + string.Join("; ", monthSkipped.ToArray()) + ")", wrongMonth.Count == 0);

            // Skipping August moves on to September; undoing it puts August back.
            ServiceContractPhotocopier.Classes.ScpBillingSequence.Skip(db, k7, 202608, "harness", "ADMIN");
            a = Find(ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false), "HQ-2026-007", true);
            Expect("skip Aug: Sep, 1 due, 8/36, undo offered (" + a.Period + ", " + a.Due + ")",
                   a.Period == 202609 && a.Due == 1 && a.Sequence.ProgressText(a.Period) == "8/36" && a.Sequence.SkipBefore(a.Period) == 202608);
            ServiceContractPhotocopier.Classes.ScpBillingSequence.UndoSkip(db, k7, 202608, "ADMIN");
            a = Find(ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false), "HQ-2026-007", true);
            Expect("undo the skip: back to Aug (" + a.Period + ")", a.Period == 202608);
            ServiceContractPhotocopier.Classes.ScpBillingSequence.SetBillFrom(db, k7, from7);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_ContractPeriodSkip WHERE ContractKey = " + k7);

            // An older month left unfinished comes first, even with a finished month after it:
            // from Jul, Jul billed for the copies only, Aug billed in full -> Jul is the month.
            ServiceContractPhotocopier.Classes.ScpBillingSequence.SetBillFrom(db, k7, 202607);
            string stampMonth = "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, InvoicedDocNo, LastModified) " +
                "SELECT m.ItemMeterKey, 2026, {0}, 1, GETDATE(), 'INTERBILL', 'N', 'SEQTEST', GETDATE() FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "WHERE i.ContractKey = " + k7 + " AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e WHERE e.ItemMeterKey = m.ItemMeterKey AND e.PeriodYear = 2026 AND e.PeriodMonth = {0}) ";
            db.ExecuteNonQuery(string.Format(stampMonth, 7) + "AND UPPER(ISNULL(m.MeterRole,'')) IN ('BK','CL')");
            db.ExecuteNonQuery(string.Format(stampMonth, 8));
            a = Find(ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false), "HQ-2026-007", true);
            Expect("Jul billed in part, Aug in full: Jul is open, 2 due (" + a.Period + ", " + a.Due + ", " + a.StatusText + ")",
                   a.Period == 202607 && a.Open && a.Due == 2);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_MeterEntry WHERE InvoicedDocNo = 'SEQTEST'");
            ServiceContractPhotocopier.Classes.ScpBillingSequence.SetBillFrom(db, k7, from7);

            // September invoiced for the copies but not the rental: it stays on September.
            string stamp = "INSERT INTO dbo.zSCP2_MeterEntry (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, Invoiced, InvoicedDocNo, LastModified) " +
                "SELECT m.ItemMeterKey, 2026, 9, 1, GETDATE(), 'INTERBILL', 'N', 'SEQTEST', GETDATE() FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                "WHERE i.ContractKey = " + k7 + " AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry e WHERE e.ItemMeterKey = m.ItemMeterKey AND e.PeriodYear = 2026 AND e.PeriodMonth = 9) ";
            db.ExecuteNonQuery(stamp + "AND UPPER(ISNULL(m.MeterRole,'')) IN ('BK','CL')");
            a = Find(ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false), "HQ-2026-007", true);
            Expect("Sep invoiced for BK/CL only: still Sep, open (" + a.Period + ", " + a.StatusText + ")", a.Period == 202609 && a.Open && a.Due == 1);
            db.ExecuteNonQuery(stamp);
            List<ServiceContractPhotocopier.Classes.IbContractRow> afterSep = ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false);
            a = Find(afterSep, "HQ-2026-007", true);
            int done7 = 0, left7 = 0;
            foreach (ServiceContractPhotocopier.Classes.IbInvoiceRow v in ServiceContractPhotocopier.Classes.ScpInterBillBoard.Invoices(db, afterSep, 2026, 9))
            {
                if (v.Contract.HqContractNo != "HQ-2026-007") continue;
                if (v.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.INVOICED && v.StatusText.StartsWith("Invoiced · SEQTEST")) done7++; else left7++;
            }
            Expect("after September is invoiced the list still shows HQ-2026-007, as Invoiced (" + done7 + " done, " + left7 + " left)", done7 >= 1 && left7 == 0);
            Expect("Sep invoiced in full: up to date, 9/36, nothing due, next Oct (" + a.StatusText + ")",
                   !a.Open && a.Status == ServiceContractPhotocopier.Classes.ScpInterBillBoard.INVOICED && a.Due == 0 &&
                   a.Sequence.Next == 202610 && a.Sequence.ProgressText(ServiceContractPhotocopier.Classes.ScpInterBillBoard.OpenPeriod(a)) == "9/36");
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_MeterEntry WHERE InvoicedDocNo = 'SEQTEST'");
        }
        else Fail("HQ-2026-007 has no billing sequence");

        // The customer filter is HQ's customer, applied as the board is read: a contract left out is
        // not loaded at all.
        List<string[]> hqCustomers = ServiceContractPhotocopier.Classes.ScpInterBillBoard.Customers(cs, false);
        bool subListed = false;
        foreach (string[] cu in hqCustomers) if (cu[0] == "3000-SUB01") subListed = true;
        Expect("the checklist is HQ's customers (" + hqCustomers.Count + ", includes 3000-SUB01)", subListed && hqCustomers.Count >= 1);
        HashSet<string> only = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        only.Add("3000-SUB01");
        List<ServiceContractPhotocopier.Classes.IbContractRow> filtered =
            ServiceContractPhotocopier.Classes.ScpInterBillBoard.Load(db, book, cs, 2026, 9, false, only);
        int sub = 0, other = 0;
        foreach (ServiceContractPhotocopier.Classes.IbContractRow r in rows)
            if (r.Remote != null && r.Remote.DebtorCode == "3000-SUB01") sub++; else other++;
        bool onlySub = true;
        foreach (ServiceContractPhotocopier.Classes.IbContractRow r in filtered) if (r.Remote == null || r.Remote.DebtorCode != "3000-SUB01") onlySub = false;
        Expect("filter HQ customer 3000-SUB01: " + filtered.Count + " rows, all SUB01, none of the other " + other, filtered.Count == sub && onlySub);

        // Meter Invoice Run does not list a taken contract.
        Dictionary<string, List<decimal[]>> ladders;
        DataTable runRows = ServiceContractPhotocopier.Classes.ScpInvoiceRun.LoadRows(db, 2026, 9, 31, true, "HQ-2026-", out ladders);
        int takenListed = 0;
        foreach (DataRow rr in runRows.Rows)
        {
            string no = Convert.ToString(rr["ContractNo"]);
            if (no == "HQ-2026-002" || no == "HQ-2026-003" || no == "HQ-2026-006" || no == "HQ-2026-007") takenListed++;
        }
        Expect("Meter Invoice Run lists none of the taken HQ contracts", takenListed == 0);

        // The screen, with a saved filter for this user, then without.
        ServiceContractPhotocopier.Classes.ScpInterBillBoard.SaveCustomerFilter(db, "ADMIN", only);
        Assembly plugF = Assembly.LoadFrom(PLUGIN);
        Type tf = plugF.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.InterBillBoard_Form", true);
        Form wf = (Form)Activator.CreateInstance(tf, new object[] { db });
        try
        {
            wf.StartPosition = FormStartPosition.Manual; wf.Location = new Point(-4000, -4000); wf.ShowInTaskbar = false;
            wf.Show(); Application.DoEvents();
            Control g = FindCtl(wf, "GridContracts");
            object v = g.GetType().GetProperty("MainView").GetValue(g, null);
            int n = (int)v.GetType().GetProperty("DataRowCount").GetValue(v, null);
            object ds = g.GetType().GetProperty("DataSource").GetValue(g, null);
            int tableRows = ds is DataTable ? ((DataTable)ds).Rows.Count
                          : ds is DataSet && ((DataSet)ds).Tables.Contains("Contracts") ? ((DataSet)ds).Tables["Contracts"].Rows.Count : -1;
            string filterText = (string)v.GetType().GetProperty("ActiveFilterString").GetValue(v, null);
            Expect("saved filter: the screen's DataTable itself has " + sub + " rows (" + tableRows + "), no grid filter", tableRows == sub && n == sub && string.IsNullOrEmpty(filterText));
        }
        finally { wf.Close(); wf.Dispose(); }
        ServiceContractPhotocopier.Classes.ScpInterBillBoard.SaveCustomerFilter(db, "ADMIN", null);

        // The screen. It opens on the view this user last chose; the checks below start from the
        // invoice list, so the choice is put aside and handed back at the end.
        object viewWas = db.ExecuteScalar("SELECT ConfigValue FROM dbo.Z_PumsConfig WHERE ConfigKey = 'INTERBILL_VIEW_ADMIN'");
        db.ExecuteNonQuery("DELETE FROM dbo.Z_PumsConfig WHERE ConfigKey = 'INTERBILL_VIEW_ADMIN'");
        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type t = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.InterBillBoard_Form", true);
        Form w = (Form)Activator.CreateInstance(t, new object[] { db });
        try
        {
            w.StartPosition = FormStartPosition.Manual;
            w.Location = new Point(-4000, -4000);
            w.Size = new Size(1366, 768);   // it opens maximized; the smallest common screen
            w.ShowInTaskbar = false;
            w.Show();
            Application.DoEvents();
            Control grid = FindCtl(w, "GridContracts");
            object gv = grid.GetType().GetProperty("MainView").GetValue(grid, null);
            int shown = (int)gv.GetType().GetProperty("DataRowCount").GetValue(gv, null);
            Say("screen", shown + " contracts on the board");
            Expect("the board lists every HQ contract it loaded", shown == rows.Count);
            Expect("nothing overlaps in Filter Options", NoOverlap(FindCtl(w, "GrpFilter")));
            Expect("nothing overlaps in the filter panel", NoOverlap(FindCtl(w, "PanelFilter")));
            Expect("nothing overlaps in the detail head", NoOverlap(FindCtl(w, "PanelDetailHead")));
            Control invGrid = FindCtl(w, "GridInvoices");
            object ivView = invGrid.GetType().GetProperty("MainView").GetValue(invGrid, null);
            int ivShown = (int)ivView.GetType().GetProperty("DataRowCount").GetValue(ivView, null);
            HashSet<long> withInvoices = new HashSet<long>();
            foreach (ServiceContractPhotocopier.Classes.IbInvoiceRow v in ServiceContractPhotocopier.Classes.ScpInterBillBoard.Invoices(db, rows, 2026, 9)) withInvoices.Add(v.Contract.LocalKey);
            int ivExpected = withInvoices.Count;
            Say("screen", ivShown + " contracts with invoices on the invoice list");
            Expect("the invoice list has one row per contract with invoices (" + ivShown + " of " + ivExpected + ")", ivShown == ivExpected && ivShown > 0);
            MethodInfo ivCell = ivView.GetType().GetMethod("GetRowCellValue", new Type[] { typeof(int), typeof(string) });
            int iv7 = -1;
            for (int h = 0; h < ivShown; h++) if (Convert.ToString(ivCell.Invoke(ivView, new object[] { h, "ContractNo" })) == "HQ-2026-007") iv7 = h;
            bool iv7Kids = iv7 >= 0 && !(bool)ivView.GetType().GetMethod("IsMasterRowEmpty", new Type[] { typeof(int) }).Invoke(ivView, new object[] { iv7 });
            Expect("invoice list: HQ-2026-007 opens onto its invoices, Invoices " + (iv7 >= 0 ? Convert.ToString(ivCell.Invoke(ivView, new object[] { iv7, "InvoiceProgress" })) : "?"),
                   iv7Kids && iv7 >= 0 && Convert.ToString(ivCell.Invoke(ivView, new object[] { iv7, "InvoiceProgress" })) == "0/1");
            Expect("nothing overlaps in the view switch", NoOverlap(FindCtl(w, "PanelView")));
            // Contracts view: each contract row opens onto its invoices.
            object cView = grid.GetType().GetProperty("MainView").GetValue(grid, null);
            int h7 = -1, cRows = (int)cView.GetType().GetProperty("DataRowCount").GetValue(cView, null);
            MethodInfo cell = cView.GetType().GetMethod("GetRowCellValue", new Type[] { typeof(int), typeof(string) });
            for (int h = 0; h < cRows; h++) if (Convert.ToString(cell.Invoke(cView, new object[] { h, "HqContractNo" })) == "HQ-2026-007") h7 = h;
            bool hasKids = h7 >= 0 && !(bool)cView.GetType().GetMethod("IsMasterRowEmpty", new Type[] { typeof(int) }).Invoke(cView, new object[] { h7 });
            string prog7 = h7 >= 0 ? Convert.ToString(cell.Invoke(cView, new object[] { h7, "InvoiceProgress" })) : "";
            Expect("Contracts view: HQ-2026-007 opens onto its invoices, Invoices " + prog7, hasKids && prog7 == "0/1");
            int h1 = -1;
            for (int h = 0; h < cRows; h++) if (Convert.ToString(cell.Invoke(cView, new object[] { h, "HqContractNo" })) == "HQ-2026-001") h1 = h;
            bool empty1 = h1 >= 0 && (bool)cView.GetType().GetMethod("IsMasterRowEmpty", new Type[] { typeof(int) }).Invoke(cView, new object[] { h1 });
            Expect("Contracts view: HQ-2026-001 (not taken) has nothing to open", empty1);
            Control conn = FindCtl(w, "LblConn");
            Expect("it names HQ and says Connected (" + (conn == null ? "" : conn.Text) + ")", conn != null && conn.Text.EndsWith("Connected") && conn.Text.Length > "Connected".Length);
            Expect("there is no HQ picker on the board", FindCtl(w, "CboBook") == null);
            Expect("nothing overlaps in Filter Options with the customer list", NoOverlap(FindCtl(w, "GrpFilter")));
            Control pickGrid = FindCtl(w, "GridCustomerPick");
            object pickView = pickGrid.GetType().GetProperty("MainView").GetValue(pickGrid, null);
            int pickAll = (int)pickView.GetType().GetProperty("DataRowCount").GetValue(pickView, null);
            FindCtl(w, "TxtCustomerSearch").Text = "subsidiary sdn";
            Application.DoEvents();
            int pickHit = (int)pickView.GetType().GetProperty("DataRowCount").GetValue(pickView, null);
            Expect("the customer picker searches: 'subsidiary sdn' leaves 1 of " + pickAll + " (" + pickHit + ")", pickAll >= 2 && pickHit == 1);
            // The pop-up is not open here, and a hidden button ignores PerformClick: call its handler.
            w.GetType().GetMethod("BtnTickShown_Click", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(w, new object[] { null, EventArgs.Empty });
            Control pickCount = FindCtl(w, "LblPickCount");
            Expect("Tick shown ticks what the search shows (" + pickCount.Text + ")", pickCount.Text.StartsWith("1 ticked"));
            FindCtl(w, "TxtCustomerSearch").Text = "";
            Application.DoEvents();
            Expect("clearing the search shows every customer again, the tick kept", (int)pickView.GetType().GetProperty("DataRowCount").GetValue(pickView, null) == pickAll && pickCount.Text.StartsWith("1 ticked"));
            Control popup = FindCtl(w, "PopCustomers");
            Expect("the picker is hidden until the box is opened", popup != null && !popup.Visible);
            Control grp = FindCtl(w, "GrpFilter"); Control pnl = FindCtl(w, "PanelFilter");
            Control refresh = FindCtl(w, "BtnRefresh");
            Expect("Invoices view: no Filter Options, the buttons start where it was (" + refresh.Left + ")",
                   grp != null && !grp.Visible && refresh.Left == grp.Left && pnl.Height < 142);
            Control toContracts = FindCtl(w, "BtnViewContracts");
            toContracts.GetType().GetProperty("Checked").SetValue(toContracts, true, null);
            Application.DoEvents();
            Expect("Contracts view: Filter Options back, buttons back (" + refresh.Left + ")",
                   grp.Visible && refresh.Left == 510 && grp.Bottom <= pnl.Height && NoOverlap(pnl));
            if (viewWas == null || viewWas == DBNull.Value)
                db.ExecuteNonQuery("DELETE FROM dbo.Z_PumsConfig WHERE ConfigKey = 'INTERBILL_VIEW_ADMIN'");
            else
                ServiceContractPhotocopier.Data.PumsConfig.Set(db, "INTERBILL_VIEW_ADMIN", Convert.ToString(viewWas));
        }
        finally { w.Close(); w.Dispose(); }
    }

    // ───────────────────────────── helpers ─────────────────────────────

    private static ServiceContractPhotocopier.Classes.IbContractRow Find(List<ServiceContractPhotocopier.Classes.IbContractRow> rows, string hqNo, bool taken)
    {
        foreach (ServiceContractPhotocopier.Classes.IbContractRow r in rows)
            if (r.HqContractNo == hqNo && r.Taken == taken) return r;
        return null;
    }

    private static void Expect(string what, bool ok) { Console.WriteLine("   " + (ok ? "ok    " : "FAIL  ") + what); if (!ok) _fail++; }
    private static void Fail(string what) { Console.WriteLine("   FAIL  " + what); _fail++; }
    private static void Say(string k, string v) { Console.WriteLine("   " + k.PadRight(8) + v); }

    private static Control FindCtl(Control root, string name)
    {
        if (root.Name == name) return root;
        foreach (Control c in root.Controls) { Control f = FindCtl(c, name); if (f != null) return f; }
        return null;
    }

    private static bool NoOverlap(Control parent)
    {
        if (parent == null) return false;
        bool ok = true;
        List<Control> kids = new List<Control>();
        foreach (Control c in parent.Controls) if (c.Visible) kids.Add(c);
        for (int i = 0; i < kids.Count; i++)
            for (int j = i + 1; j < kids.Count; j++)
            {
                Rectangle a = kids[i].Bounds, b = kids[j].Bounds;
                a.Inflate(-1, -1); b.Inflate(-1, -1);
                if (a.IntersectsWith(b)) { Console.WriteLine("         overlap: " + kids[i].Name + " " + kids[i].Bounds + " vs " + kids[j].Name + " " + kids[j].Bounds); ok = false; }
            }
        return ok;
    }
}
