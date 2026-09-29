using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;
using ServiceContractPhotocopier.Classes;

/// <summary>
/// Drives the inter-billing module through the REAL classes the screens call, against two real
/// account books, and prints what each step did.
///
/// <para>Book B is the one this plug-in runs in (AED_ATPTEST). Book A is a stand-in parent
/// (AED_ATPBOOKA) holding one contract with three machines, nine counters and a month of readings.
/// Nothing here reimplements the module: every line goes through ScpInterBillBooks,
/// ScpInterBillReader, ScpInterBillTake, ScpInterBillLinks and ScpInterBillGuard, so a green run
/// says the screens will do the same.</para>
///
/// <para>Every run resets both books first, so it can be run as often as you like and the counts it
/// checks always mean the same thing. Pass "clean" to reset and stop.</para>
/// </summary>
internal static class Program
{
    private const string Server = "localhost,1433";
    /// <summary>The BILLING book -- the one that takes contracts and issues the bill. Named on
    /// the command line so a clean can be pointed at whichever book has been used for testing.</summary>
    private static string BookB = "AED_ASNDUMMY";
    private const string User = "sa";
    private const string Pass = "rs6663";

    /// <summary>The PARENT book -- the one that owns the machines and takes the readings.
    ///
    /// <para>A REAL account book: created in AutoCount, with this plug-in installed and its
    /// migrations run. There was briefly a hand-built nine-table stand-in as well; it was dropped,
    /// because a fixture that is not an account book cannot say anything about how the module
    /// behaves against one, and two ways to run the same suite is one too many.</para>
    ///
    /// <para>Named on the command line so a different parent book can be pointed at.</para></summary>
    private static string BookA = "AED_ATPTEST";

    /// <summary>The parent book's seeded contract every run takes (seed-parent-book.sql).</summary>
    private const string FixtureNo = "HQ-2026-001";

    private static int _fail;

    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

    private static int Main(string[] args)
    {
        // AutoCount lives in Program Files, not beside this exe. The resolver has to be in place
        // before the JIT touches an AutoCount type, which is why the work is in Run() and Run() is
        // never inlined.
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
        bool clean = args.Length > 0 && args[0] == "clean";
        if (args.Length > 1 && args[1].Trim().Length > 0) BookA = args[1].Trim();
        if (args.Length > 2 && args[2].Trim().Length > 0) BookB = args[2].Trim();
        Console.WriteLine();
        Console.WriteLine("   billing book (this one)   " + BookB);
        Console.WriteLine("   machine book (theirs)     " + BookA);

        DBSetting db = new DBSetting(DBServerType.SQL2000, Server, User, Pass, BookB, false);

        Line("book identity");
        Guid mine = ScpBookIdentity.Local(db);
        Say("this book", mine.ToString().ToUpperInvariant());
        Check("this book has an identity", mine != Guid.Empty);

        // Every run resets first, and that is not tidiness.
        //
        // The numbers below are written down -- 3 machines, 9 counters, 6 opening readings -- and
        // that is what makes a failure readable. But the run ENDS by taking a fourth machine, so a
        // second run met four of everything and reported eight failures that were all the same
        // thing: the fixture had moved. A suite whose result depends on how many times it has been
        // run before is a suite nobody trusts.
        //
        // So there is one path, always from nothing, and the constants always mean what they say.
        Wipe(db);
        if (clean) return 0;

        // ---------------------------------------------------------------- the password store
        Line("encrypt / decrypt");
        string enc = ScpSecret.Protect(Pass);
        Check("ciphertext is not the password", enc != Pass && enc.StartsWith("enc1:"));
        Check("it comes back", ScpSecret.Unprotect(enc) == Pass);
        Check("twice is different (random IV)", ScpSecret.Protect(Pass) != enc);
        Check("plain text passes through", ScpSecret.Unprotect("typed by hand") == "typed by hand");
        Check("rubbish does not throw", ScpSecret.Unprotect("enc1:not-base64!!") == "");

        // ---------------------------------------------------------------- the other book
        Line("connect to the other book");
        ScpInterBillBook book = FindOrMake(db, "HQ (" + BookA + ")");
        book.ServerName = Server;
        book.DatabaseName = BookA;
        book.DbUser = User;
        book.Password = Pass;
        string msg;
        bool ok = ScpInterBillBooks.Test(db, book, out msg);
        Say("test", msg.Replace(Environment.NewLine, " "));
        Check("reached it", ok);
        Check("learned who they are", book.RemoteBookId != Guid.Empty);
        Check("and it is not us", book.RemoteBookId != mine);
        ScpInterBillBooks.Save(db, book);

        ScpInterBillBook reloaded = ScpInterBillBooks.Load(db, book.BookKey);
        Check("saved and reloaded", reloaded != null && reloaded.RemoteBookId == book.RemoteBookId);
        Check("password is not read back", reloaded != null && reloaded.Password == "");
        Check("but it is known to be there", reloaded != null && reloaded.HasPassword);
        Check("and it still opens the book",
              ScpInterBillBooks.PasswordOf(db, book.BookKey) == Pass);

        // Pointing it at ITSELF must be refused: a book that could take its own contracts would
        // let somebody bill one machine to one customer twice.
        ScpInterBillBook self = new ScpInterBillBook();
        self.Alias = "(self test)";
        self.ServerName = Server; self.DatabaseName = BookB;
        self.DbUser = User; self.Password = Pass;
        string selfMsg;
        bool selfOk = ScpInterBillBooks.Test(db, self, out selfMsg);
        Check("linking a book to itself is refused", !selfOk);
        Say("said", selfMsg.Replace(Environment.NewLine, " "));

        // ---------------------------------------------------------------- what they have
        Line("their contracts");
        string cs = ScpInterBillBooks.ConnectionStringOf(db, reloaded);
        // Asked for BY NUMBER, through the search the screen uses.
        //
        // It used to take whichever contract came back first, which was fine against a fixture with
        // one contract on it and meaningless against a real parent book: this one has 3,104, so
        // "the first" was an alphabetical accident and the run took a stranger's contract and then
        // failed eleven checks about a fixture it had never opened.
        const string FIXTURE = "HQ-2026-001";
        List<ScpRemoteContract> theirs = ScpInterBillReader.Contracts(cs, false, FIXTURE);
        Check("found their contract by searching for it", theirs.Count > 0);

        ScpRemoteContract src = null;
        foreach (ScpRemoteContract c in theirs)
        {
            Say(c.ContractNo, c.MachineCount + " machines, billed to " +
                (c.DebtorName.Length > 0 ? c.DebtorName : c.DebtorCode));
            if (c.ContractNo == FIXTURE) src = c;
        }
        Check("and it is the one asked for", src != null);
        if (src == null)
        {
            Console.WriteLine();
            Console.WriteLine("   " + FIXTURE + " is not in " + BookA + " -- run seed-parent-book.sql");
            Console.WriteLine("   against that book first.");
            return 1;
        }

        List<ScpRemoteMachine> machines = ScpInterBillReader.Machines(cs, src.ContractKey);
        int counters = 0;
        foreach (ScpRemoteMachine m in machines) counters += m.Meters.Count;
        Check("read their machines", machines.Count == 3);
        Check("with their counters", counters == 9);

        // ---------------------------------------------------------------- take it
        Line("take it");
        Check("nothing of theirs is taken yet",
              !ScpInterBillLinks.TakenSourceKeys(
                   db, reloaded.RemoteBookId, ScpInterBillLink.CONTRACT).Contains(src.ContractKey));

        // 30% is the figure in the brief: their 1.30 becomes 1.69.
        const decimal MARGIN = 30m;
        string debtor = FirstDebtor(db);
        ScpTakeResult res = ScpInterBillTake.TakeContract(
            db, reloaded, cs, src, machines, debtor, "", src.Description,
            src.StartDate, src.ExpiryDate, "ADMIN", MARGIN);
        Say("made", res.ContractNo + "  " + res.Machines + " machines, " + res.Meters + " counters");
        if (res.Error.Length > 0) Say("error", res.Error);
        Check("the take succeeded", res.Ok);
        Check("every machine came", res.Machines == machines.Count);
        Check("every counter came", res.Meters == counters);
        long localKey = res.ContractKey;

        // Their numbers, kept. Two companies talking about one machine have to be able to say the
        // same thing about it, so nothing here is renumbered.
        Check("the contract keeps THEIR number", res.ContractNo == src.ContractNo);
        int renamed = 0;
        foreach (ScpRemoteMachine m in machines)
        {
            decimal sameNo = Scalar(db,
                "SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = " + localKey +
                " AND ServiceItemNo = N'" + m.ServiceItemNo.Replace("'", "''") + "'");
            if (sameNo != 1m) renamed++;
        }
        Check("every machine keeps THEIR service item number", renamed == 0);

        // ...and how they bill it. Not because this book must bill the same way, but because a
        // hardcoded default here is the module inventing an answer nobody gave it -- which it did,
        // and which turned a contract they bill per machine into one invoice without saying so.
        Check("it starts on THEIR invoice split",
              Str(db, "SELECT BillingMode FROM dbo.zSCP2_Contract WHERE ContractKey = " + localKey)
                  == src.BillingMode);
        Check("and THEIR billing day",
              Str(db, "SELECT CAST(BillingDay AS VARCHAR) FROM dbo.zSCP2_Contract WHERE ContractKey = " + localKey)
                  == src.BillingDay.ToString());
        Say("they bill", src.BillingMode == "S" ? "one invoice per machine" : "one invoice per contract");

        // ...and taking it a second time is refused rather than half-done, because those numbers are
        // unique in this book.
        Check("taking it twice is refused up front",
              ScpInterBillTake.NumberClashes(db, src.ContractNo, machines).Count > 0);

        // ---------------------------------------------------------------- what it made
        Line("what it made");
        Check("the contract is linked", ScpInterBillGuard.ContractIsSourced(db, localKey));
        Say("source book", ScpInterBillGuard.SourceName(db, localKey));

        Check("its customer is OURS, not theirs",
              !string.Equals(DebtorOf(db, localKey), src.DebtorCode, StringComparison.OrdinalIgnoreCase));

        // ---------------------------------------------------------------- the margin
        Line("their price plus the margin");

        // The arithmetic on its own first, including the figure from the brief.
        Check("1.30 at 30% is 1.69", ScpInterBillTake.WithMargin(1.30m, 30m, true) == 1.69m);
        Check("a rental rounds to cents", ScpInterBillTake.WithMargin(380m, 30m, true) == 494.00m);
        Check("a copy rate keeps its places",
              ScpInterBillTake.WithMargin(0.0180m, 30m, false) == 0.0234m);
        // 0.0285 + 30% is 0.03705 -- FIVE places. Rounded to four it stores 0.0371, and the
        // hospital that prints 32,852 black copies a month is billed RM1.64 nobody agreed to.
        // The column holds six, so six is what is kept.
        Check("a rate that needs five places keeps them",
              ScpInterBillTake.WithMargin(0.0285m, 30m, false) == 0.03705m);
        Check("and six", ScpInterBillTake.WithMargin(0.000135m, 30m, false) == 0.000176m);
        Check("margin 0 leaves it alone", ScpInterBillTake.WithMargin(0.0180m, 0m, false) == 0.0180m);
        // A margin of nobody-is-adding-anything must be a copy, to the last place they quoted.
        Check("margin 0 copies six places exactly",
              ScpInterBillTake.WithMargin(0.123456m, 0m, false) == 0.123456m);
        // Nothing priced over there must stay nothing priced here -- adding a margin to zero is how a
        // system invents a price, which is the one thing this must never do.
        Check("nothing from nothing", ScpInterBillTake.WithMargin(0m, 30m, false) == 0m);

        // ...then what actually landed on the contract, counter by counter, against their book.
        int wrong = 0;
        int checkedMeters = 0;
        foreach (ScpInterBillLink ml in ScpInterBillLinks.ForContract(db, localKey))
        {
            if (ml.EntityType != ScpInterBillLink.METER) continue;
            decimal theirRate = 0m, theirMin = 0m;
            foreach (ScpRemoteMachine m in machines)
                foreach (ScpRemoteMeter t in m.Meters)
                    if (t.ItemMeterKey == ml.SourceKey)
                    {
                        theirRate = t.ChargesRate;
                        theirMin = t.MinimumCharges;
                        bool flat = t.MeterRole.Trim().ToUpperInvariant() == "RENTAL";
                        decimal want = ScpInterBillTake.WithMargin(theirRate, MARGIN, flat);
                        decimal wantMin = ScpInterBillTake.WithMargin(theirMin, MARGIN, true);
                        decimal got = Scalar(db,
                            "SELECT ISNULL(ChargesRate,0) FROM dbo.zSCP2_ItemMeter WHERE ItemMeterKey = " + ml.LocalKey);
                        decimal gotMin = Scalar(db,
                            "SELECT ISNULL(MinimumCharges,0) FROM dbo.zSCP2_ItemMeter WHERE ItemMeterKey = " + ml.LocalKey);
                        checkedMeters++;
                        if (got != want || gotMin != wantMin) wrong++;
                    }
        }
        Say("counters checked", checkedMeters.ToString());
        Check("every counter carries their price plus 30%", checkedMeters > 0 && wrong == 0);

        // And the link remembers what their price WAS, or a rise over there could never be noticed.
        int withRate = 0;
        foreach (ScpInterBillLink ml in ScpInterBillLinks.ForContract(db, localKey))
            if (ml.EntityType == ScpInterBillLink.METER &&
                ScpInterBillSnapshot.Get(ScpInterBillSnapshot.Parse(ml.TakenSnapshot), "RATE").Length > 0)
                withRate++;
        Check("each link remembers their rate", withRate == counters);

        decimal openings = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_ItemMeter m JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND m.InitialReading > 0");
        Check("opening readings DID come across", openings == 6m);

        // Their layout comes across as the starting point. Blank was never neutral: it means "every
        // machine on its own line", so a fleet the other book prints as three lines used to arrive
        // as twenty-one and somebody had to re-type a layout that already existed.
        int theirGrouped = 0;
        foreach (ScpRemoteMachine m in machines)
            if (m.MergeGroupCode.Trim().Length > 0 || m.MergeGroupCodeMeter.Trim().Length > 0)
                theirGrouped++;
        decimal grouped = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_Item " +
            " WHERE ContractKey = " + localKey +
            "   AND (ISNULL(MergeGroupCode,'') <> '' OR ISNULL(MergeGroupCodeMeter,'') <> '')");
        Say("machines they had grouped", theirGrouped.ToString());
        Check("their grouping came across", theirGrouped > 0 && grouped == (decimal)theirGrouped);

        // And the price the grouping was FOR. A group without it folds seven machines onto one line
        // and then bills whatever the first machine's own counter happens to say.
        List<ScpRemoteLinePrice> theirLines =
            ScpInterBillReader.LinePrices(cs, src.ContractKey);
        int wantLines = 0;
        foreach (ScpRemoteLinePrice p in theirLines)
        {
            string g = p.GroupCode.Trim();
            if (g.Length == 0) continue;
            foreach (ScpRemoteMachine m in machines)
                if (string.Equals(m.MergeGroupCode.Trim(), g, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.MergeGroupCodeMeter.Trim(), g, StringComparison.OrdinalIgnoreCase))
                { wantLines++; break; }
        }
        decimal gotLines = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_ContractRentalPrice WHERE ContractKey = " + localKey);
        Say("agreed lines they had", wantLines.ToString());
        Check("their agreed line prices came across", gotLines == (decimal)wantLines);

        // Through the margin, like every other price -- otherwise the line bills their figure and
        // the whole contract earns nothing.
        int lineWrong = 0;
        foreach (ScpRemoteLinePrice p in theirLines)
        {
            string g = p.GroupCode.Trim();
            if (g.Length == 0 || p.UnitPrice <= 0m) continue;
            decimal got = Scalar(db,
                "SELECT ISNULL(MAX(UnitPrice),0) FROM dbo.zSCP2_ContractRentalPrice " +
                " WHERE ContractKey = " + localKey + " AND GroupCode = N'" + g.Replace("'", "''") + "'");
            if (got != 0m && got != ScpInterBillTake.WithMargin(p.UnitPrice, MARGIN, true)) lineWrong++;
        }
        Check("and through the margin", lineWrong == 0);

        // A ladder moves its PRICES and keeps its boundaries -- a band is a count of copies.
        Check("a ladder keeps its boundaries and lifts its prices",
              ScpInterBillTake.LadderWithMargin("3000|0.025;99999999|0.02", 30m)
                  == "3000|0.0325;99999999|0.026");
        Check("no margin leaves a ladder alone",
              ScpInterBillTake.LadderWithMargin("3000|0.025", 0m) == "3000|0.025");

        List<ScpInterBillLink> links = ScpInterBillLinks.ForContract(db, localKey);
        int items = 0, meters = 0;
        foreach (ScpInterBillLink l in links)
        {
            if (l.EntityType == ScpInterBillLink.ITEM) items++;
            else if (l.EntityType == ScpInterBillLink.METER) meters++;
        }
        Check("every machine is linked", items == machines.Count);
        Check("every counter is linked", meters == counters);

        // ---------------------------------------------------------------- the guard
        Line("the counters cannot be removed");
        long anItem = (long)Scalar(db,
            "SELECT TOP 1 ItemKey FROM dbo.zSCP2_Item WHERE ContractKey = " + localKey + " ORDER BY Pos");
        HashSet<string> owned = ScpInterBillGuard.OwnedMeterTags(db, anItem);
        Check("the machine's counters are known to be theirs", owned.Count == 3);

        DataTable onScreen = new DataTable();
        onScreen.Columns.Add("MeterTypeCode", typeof(string));
        onScreen.Columns.Add("MachineSerialNo", typeof(string));
        onScreen.Rows.Add("RENTAL", "");
        onScreen.Rows.Add("BK", "");
        // CL has been unticked.
        List<string> vanished = ScpInterBillGuard.VanishedOwnedMeters(owned, onScreen);
        Check("removing one is caught", vanished.Count == 1);
        Say("it says", vanished.Count > 0 ? vanished[0] : "(nothing)");

        onScreen.Rows.Add("CL", "");
        Check("leaving them alone is not caught",
              ScpInterBillGuard.VanishedOwnedMeters(owned, onScreen).Count == 0);

        // A counter of this book's OWN on the same machine is not guarded -- adding is allowed.
        Check("an unrelated counter is not theirs",
              !owned.Contains(ScpInterBillGuard.Tag("A0 PLOTTER", "")));

        // ---------------------------------------------------------------- their readings
        Line("their readings");
        List<ScpInterBillLink> meterLinks = ScpInterBillLinks.OfType(
            db, reloaded.RemoteBookId, ScpInterBillLink.METER, false);
        List<long> sourceKeys = new List<long>();
        Dictionary<long, long> localBySource = new Dictionary<long, long>();
        foreach (ScpInterBillLink l in meterLinks)
        {
            sourceKeys.Add(l.SourceKey);
            localBySource[l.SourceKey] = l.LocalKey;
        }
        List<ScpRemoteReading> readings = ScpInterBillReader.Readings(cs, sourceKeys, 2026, 9);
        Check("their September readings are there", readings.Count == 6);

        int wrote = 0;
        foreach (ScpRemoteReading x in readings)
        {
            long lk;
            if (!localBySource.TryGetValue(x.ItemMeterKey, out lk)) continue;
            WriteEntry(db, lk, 2026, 9, x.CurrentReading, x.ReadingDate);
            wrote++;
        }
        Check("brought every one over", wrote == 6);

        decimal here = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 " +
            "   AND e.Source = 'INTERBILL'");
        Check("and they say where they came from", here == 6m);

        decimal same = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 " +
            "   AND e.CurrentReading > m.InitialReading");
        Check("every reading is past its opening", same == 6m);

        // Bringing them a second time must not double anything.
        foreach (ScpRemoteReading x in readings)
        {
            long lk;
            if (localBySource.TryGetValue(x.ItemMeterKey, out lk))
                WriteEntry(db, lk, 2026, 9, x.CurrentReading, x.ReadingDate);
        }
        decimal stillSix = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9");
        Check("bringing them twice makes no second row", stillSix == 6m);

        // ---------------------------------------------------------------- the money
        //
        // The whole chain, end to end: their prices, plus the margin, against their readings, folded
        // by the same code the Meter Reading Integration screen uses. If this number is right then
        // every step before it was right, and the two screens cannot disagree -- there is only one
        // of them now.
        Line("what it will bill");

        Dictionary<string, List<decimal[]>> ladders;
        DataTable billRows = ScpBillingRows.ForContract(db, localKey, 2026, 9, out ladders);
        List<DataRow> allRows = new List<DataRow>();
        foreach (DataRow br in billRows.Rows) { br["Sel"] = true; allRows.Add(br); }
        Check("the contract's counters came back as rows", billRows.Rows.Count > 0);

        int inv;
        Dictionary<long, string> snaps;
        string bt, bm;
        Dictionary<string, MeterInvoiceGenerator.InvoiceJob> billJobs = ScpInvoiceJobs.Build(
            db, billRows, allRows, ladders, 2026, 9, ScpBillingRows.GroupingMode(db),
            out inv, out snaps, out bt, out bm);
        if (bm.Length > 0) Say("refused", bm.Replace(Environment.NewLine, " "));
        Check("the jobs were built", billJobs != null && billJobs.Count > 0);

        decimal billed = 0m;
        if (billJobs != null)
            foreach (MeterInvoiceGenerator.InvoiceJob jb in billJobs.Values)
                foreach (MeterBillLine bl in jb.Lines) billed += bl.Charge;

        // 3 machines, each: rental 380 + 30%  =  494.00
        //                   black  8,400 x 0.0180 + 30%  =  8,400 x 0.0234  =  196.56
        //                   colour 1,250 x 0.1800 + 30%  =  1,250 x 0.2340  =  292.50
        //                                                                      ------
        //                                                                      983.06   x3 = 2,949.18
        decimal expected = 2949.18m;
        Say("it bills", billed.ToString("#,##0.00") + "   (expected " + expected.ToString("#,##0.00") + ")");
        Check("their price plus 30%, against their readings", billed == expected);

        // The money is checked HERE, before the next test deliberately marks the colour
        // readings invoiced. Run it after that and the colour lines bill nothing, and the
        // failure looks like a pricing bug instead of a test standing on its own foot.
        // An invoiced reading is not overwritten -- it has been sent to a customer.
        db.ExecuteNonQuery(
            "UPDATE e SET e.Invoiced='Y', e.CurrentReading = 1 " +
            "  FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 " +
            "   AND m.MeterTypeCode = 'CL'");
        foreach (ScpRemoteReading x in readings)
        {
            long lk;
            if (localBySource.TryGetValue(x.ItemMeterKey, out lk))
                WriteEntry(db, lk, 2026, 9, x.CurrentReading, x.ReadingDate);
        }
        decimal held = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_MeterEntry e " +
            "  JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND e.PeriodYear = 2026 AND e.PeriodMonth = 9 " +
            "   AND e.Invoiced = 'Y' AND e.CurrentReading = 1");
        Check("an invoiced reading is left alone", held == 3m);

        // ---------------------------------------------------------------- what they changed
        Line("what they changed");
        ScpInterBillLink contractLink = ScpInterBillLinks.ContractLink(db, localKey);
        ScpRemoteContract now = ScpInterBillReader.Contract(cs, src.ContractKey);
        Check("nothing has changed yet",
              ScpInterBillSnapshot.Differences(contractLink.TakenSnapshot,
                                               ScpInterBillTake.ContractSnapshot(now)).Count == 0);

        // Their fourth machine.
        long newItem = AddMachineOverThere(cs, src.ContractKey, FIXTURE);
        List<ScpRemoteMachine> after = ScpInterBillReader.Machines(cs, src.ContractKey);
        Check("they now have four", after.Count == 4);

        ScpRemoteMachine added = null;
        foreach (ScpRemoteMachine m in after) if (m.ItemKey == newItem) added = m;
        Check("and this book has not got it",
              added != null && ScpInterBillLinks.Of(db, ScpInterBillLink.ITEM, 0) == null);

        ScpTakeResult more = ScpInterBillTake.AddMachine(
            db, reloaded, cs, localKey, added, reloaded.MarginPercent);
        Say("added", more.Error.Length > 0 ? more.Error : more.Machines + " machine, " + more.Meters + " counters");
        Check("taking the extra machine works", more.Error.Length == 0 && more.Machines == 1);

        decimal fourHere = Scalar(db,
            "SELECT COUNT(*) FROM dbo.zSCP2_Item WHERE ContractKey = " + localKey);
        Check("this book now has four too", fourHere == 4m);

        decimal addedRental = Scalar(db,
            "SELECT ISNULL(MAX(m.ChargesRate),0) FROM dbo.zSCP2_ItemMeter m " +
            "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
            " WHERE i.ContractKey = " + localKey + " AND i.SerialNumber = 'HQA-004' " +
            "   AND m.MeterTypeCode = 'RENTAL'");
        // The extra machine is priced by the CONNECTION's margin, which the fixture leaves at 0 --
        // so it lands at their 380 exactly. Different figure, same rule.
        Check("the extra machine is priced too", addedRental > 0m);
        Say("its rental", addedRental.ToString("#,##0.00") + "  (connection margin " +
            reloaded.MarginPercent.ToString("0.##") + "%)");

        Line(_fail == 0 ? "all good" : _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// The connection to use, reusing whatever is already configured for that database.
    ///
    /// <para>It used to insist on its own alias, so a run beside a connection somebody had set up by
    /// hand left two rows pointing at the same book -- one of them the run's litter. Matching on the
    /// DATABASE finds theirs and leaves the screen with one row.</para>
    /// </summary>
    private static ScpInterBillBook FindOrMake(DBSetting db, string alias)
    {
        foreach (ScpInterBillBook b in ScpInterBillBooks.LoadAll(db))
            if (string.Equals(b.DatabaseName, BookA, StringComparison.OrdinalIgnoreCase)) return b;
        foreach (ScpInterBillBook b in ScpInterBillBooks.LoadAll(db))
            if (string.Equals(b.Alias, alias, StringComparison.OrdinalIgnoreCase)) return b;
        ScpInterBillBook n = new ScpInterBillBook();
        n.Alias = alias;
        return n;
    }

    private static string FirstDebtor(DBSetting db)
    {
        object o = db.ExecuteScalar("SELECT TOP 1 AccNo FROM dbo.Debtor ORDER BY NEWID()");
        return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
    }

    private static string DebtorOf(DBSetting db, long contractKey)
    {
        object o = db.ExecuteScalar(
            "SELECT DebtorCode FROM dbo.zSCP2_Contract WHERE ContractKey = " + contractKey);
        return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
    }


    private static string Str(DBSetting db, string sql)
    {
        object o = db.ExecuteScalar(sql);
        return o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
    }
    private static decimal Scalar(DBSetting db, string sql)
    {
        object o = db.ExecuteScalar(sql);
        return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
    }

    /// <summary>The same write the screen makes, so the harness proves the screen's path.</summary>
    private static void WriteEntry(DBSetting db, long meterKey, int year, int month,
                                   decimal reading, DateTime? on)
    {
        using (SqlConnection cn = new SqlConnection(db.ConnectionString))
        {
            cn.Open();
            using (SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.zSCP2_MeterEntry SET CurrentReading=@v, ReadingDate=@d, Source='INTERBILL', " +
                " LastModified=GETDATE() " +
                " WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m AND ISNULL(Invoiced,'N')<>'Y'; " +
                "IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dbo.zSCP2_MeterEntry " +
                "     WHERE ItemMeterKey=@k AND PeriodYear=@y AND PeriodMonth=@m) " +
                "INSERT INTO dbo.zSCP2_MeterEntry " +
                " (ItemMeterKey, PeriodYear, PeriodMonth, CurrentReading, ReadingDate, Source, " +
                "  Invoiced, LastModified) " +
                "VALUES (@k, @y, @m, @v, @d, 'INTERBILL', 'N', GETDATE())", cn))
            {
                cmd.Parameters.AddWithValue("@k", meterKey);
                cmd.Parameters.AddWithValue("@y", year);
                cmd.Parameters.AddWithValue("@m", month);
                cmd.Parameters.AddWithValue("@v", reading);
                cmd.Parameters.AddWithValue("@d", on.HasValue ? (object)on.Value : DateTime.Today);
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Puts a fourth machine on their contract, the way their own screens would.
    ///
    /// <para>This is the only place the run WRITES into the other book, and it refuses to write into
    /// anything but the fixture. It once ran against a contract picked by accident and put a test
    /// machine onto a stranger's contract in a book with three thousand of them -- so the contract
    /// number is checked here, at the last moment, against the row about to be touched.</para>
    /// </summary>
    private static long AddMachineOverThere(string cs, long contractKey, string expectContractNo)
    {
        using (SqlConnection cn = new SqlConnection(cs))
        {
            cn.Open();

            using (SqlCommand guard = new SqlCommand(
                "SELECT ContractNo FROM dbo.zSCP2_Contract WHERE ContractKey = @ck", cn))
            {
                guard.Parameters.AddWithValue("@ck", contractKey);
                object o = guard.ExecuteScalar();
                string actual = o == null || o == DBNull.Value ? "" : Convert.ToString(o).Trim();
                if (actual != expectContractNo)
                {
                    throw new Exception(
                        "refusing to write into their book: expected contract " + expectContractNo +
                        " but key " + contractKey + " is " +
                        (actual.Length == 0 ? "(not there)" : actual));
                }
            }

            long ik;
            using (SqlCommand cmd = new SqlCommand(
                "IF EXISTS (SELECT 1 FROM dbo.zSCP2_Item WHERE ContractKey=@ck AND SerialNumber='HQA-004') " +
                "  SELECT ItemKey FROM dbo.zSCP2_Item WHERE ContractKey=@ck AND SerialNumber='HQA-004' " +
                "ELSE BEGIN " +
                "  INSERT INTO dbo.zSCP2_Item (ContractKey, ServiceItemNo, ItemCode, SerialNumber, " +
                "   [Description], Pos, Inactive, IsGroupItem, MachineMode, ServiceStartDate, " +
                "   ServiceExpiryDate, LastModified) " +
                "  VALUES (@ck, 'HQ-2026-001-004', 'iR-ADV C3560i', 'HQA-004', " +
                "   'iR-ADV C3560i / HQA-004', 4, 'N', 'N', 'ONLINE', '2026-01-01', '2028-12-31', GETDATE()); " +
                "  SELECT CAST(SCOPE_IDENTITY() AS bigint); END", cn))
            {
                cmd.Parameters.AddWithValue("@ck", contractKey);
                ik = Convert.ToInt64(cmd.ExecuteScalar());
            }
            using (SqlCommand cmd = new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM dbo.zSCP2_ItemMeter WHERE ItemKey=@ik) " +
                "INSERT INTO dbo.zSCP2_ItemMeter (ItemKey, MeterTypeCode, [Description], MeterRole, " +
                " MachineSerialNo, MinimumCharges, ChargesRate, MeterMultiPriceCode, " +
                " RebateQtyInPercent, FOCQty, InitialReading, RentalMonths, RentalBasis, " +
                " WaiveFirstNMonths, WaiveTargetAmount, WaivePartialPct, WaiveScope, " +
                " WaivePartialThreshold, WaivePartialAmount, CommitScope, LastModified) " +
                "VALUES (@ik,'RENTAL','MONTHLY RENTAL','RENTAL','',0,380,'',0,0,0,0,'A',0,0,100,'S',0,0,'S',GETDATE()), " +
                "       (@ik,'BK','BLACK COPY','BK','',0,0.018,'',0,0,50000,0,'A',0,0,100,'S',0,0,'S',GETDATE())", cn))
            {
                cmd.Parameters.AddWithValue("@ik", ik);
                cmd.ExecuteNonQuery();
            }
            return ik;
        }
    }

    /// <summary>Puts the billing book back as it was, so the run can be repeated from nothing --
    /// removing ONLY what a run makes: the contract taken from the fixture over this run's own
    /// connection, and the fourth machine it adds to the fixture over there.
    ///
    /// <para>It used to clear every link in the book, delete every contract any link pointed at, and
    /// send its fixture reset to every configured connection. On 28/9 that removed a contract the
    /// user had taken from another company's book for their own testing (CSSI-000004 in
    /// AED_ASNDUMMY) and sent a DELETE to that company's book (it matched nothing there). So the
    /// contracts are found by the fixture's number AND this run's connection, and "over there" is
    /// only ever <see cref="BookA"/>.</para></summary>
    private static void Wipe(DBSetting db)
    {
        Line("clean");
        ScpInterBillBook own = null;
        foreach (ScpInterBillBook b in ScpInterBillBooks.LoadAll(db))
            if (string.Equals(b.DatabaseName, BookA, StringComparison.OrdinalIgnoreCase)) own = b;
        if (own == null || own.RemoteBookId == Guid.Empty)
        {
            Say("nothing to clean", "no connection to " + BookA + " yet");
            return;
        }

        DataTable t = db.GetDataTable(
            "SELECT DISTINCT RootContractKey FROM dbo.zSCP2_InterBillLink " +
            " WHERE EntityType = 'CONTRACT' AND RootContractKey > 0 " +
            "   AND SourceBookId = '" + own.RemoteBookId + "' AND SourceRef = N'" + FixtureNo + "'", false);
        foreach (DataRow r in t.Rows)
        {
            long ck = Convert.ToInt64(r["RootContractKey"]);
            db.ExecuteNonQuery(
                "DELETE e FROM dbo.zSCP2_MeterEntry e " +
                " JOIN dbo.zSCP2_ItemMeter m ON m.ItemMeterKey = e.ItemMeterKey " +
                " JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + ck);
            db.ExecuteNonQuery(
                "DELETE m FROM dbo.zSCP2_ItemMeter m " +
                " JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey WHERE i.ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Item WHERE ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck);
            db.ExecuteNonQuery("DELETE FROM dbo.zSCP2_InterBillLink WHERE RootContractKey = " + ck);
            Say("removed", "contract " + ck + " (taken from " + FixtureNo + ") and its links");
        }

        // Anything beyond the seeded three goes too -- the fourth machine this run adds over there,
        // and any further one added by hand to demonstrate the "what they changed" list. The counts
        // in this file are written down, so the parent has to start every run at exactly three.
        try
        {
            using (SqlConnection cn = new SqlConnection(ScpInterBillBooks.ConnectionStringOf(db, own)))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    // Scoped to the seeded contract by NUMBER, not just by serial, and only ever sent
                    // to this run's own parent book.
                    "DELETE m FROM dbo.zSCP2_ItemMeter m " +
                    "  JOIN dbo.zSCP2_Item i ON i.ItemKey = m.ItemKey " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE c.ContractNo = @no " +
                    "   AND i.SerialNumber NOT IN ('HQA-001','HQA-002','HQA-003'); " +
                    "DELETE i FROM dbo.zSCP2_Item i " +
                    "  JOIN dbo.zSCP2_Contract c ON c.ContractKey = i.ContractKey " +
                    " WHERE c.ContractNo = @no " +
                    "   AND i.SerialNumber NOT IN ('HQA-001','HQA-002','HQA-003');", cn))
                {
                    cmd.Parameters.AddWithValue("@no", FixtureNo);
                    cmd.ExecuteNonQuery();
                }
            }
            Say("over there", "reset to the seeded three machines in " + own.Alias);
        }
        catch (Exception ex) { Say("over there", "left alone: " + ex.Message); }
        // The CONNECTION stays, and so does every other link and contract in this book: other
        // connections, and contracts somebody took by hand, are not this run's to touch.
        Say("links", "this run's cleared -- everything else is left alone");
    }

    // ---------------------------------------------------------------- output

    private static void Line(string title)
    {
        Console.WriteLine();
        Console.WriteLine("== " + title + " " + new string('=', Math.Max(2, 60 - title.Length)));
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(34) + value);
    }

    private static void Check(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine((ok ? "   ok   " : "   FAIL ") + what);
    }
}
