using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

// What the Meter Invoice Run would show for one day, straight from the data layer, then the
// form itself opened off-screen: the day strip, the summary line, the invoice grid, no overlaps.
// Read-only apart from nothing: it keys no reading and generates nothing.
internal static class InvoiceRunCheck
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
        int year = args.Length > 1 ? int.Parse(args[1]) : 2026;
        int month = args.Length > 2 ? int.Parse(args[2]) : 9;
        int day = args.Length > 3 ? int.Parse(args[3]) : 1;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        Console.WriteLine();
        Console.WriteLine("   book " + book + "   period " + year + "/" + month + "   day " + day);

        // ---------------------------------------------------------------- the data layer
        Dictionary<string, List<decimal[]>> ladders;
        DataTable rows = ServiceContractPhotocopier.Classes.ScpInvoiceRun.LoadRows(db, year, month, day, false, "", out ladders);
        string grp = ServiceContractPhotocopier.Classes.ScpInvoiceRun.GroupingMode(db);
        List<ServiceContractPhotocopier.Classes.InvoiceRunItem> items =
            ServiceContractPhotocopier.Classes.ScpInvoiceRun.BuildItems(rows, grp);

        int ready = 0, waiting = 0, invoiced = 0;
        foreach (ServiceContractPhotocopier.Classes.InvoiceRunItem it in items)
        {
            if (it.Status == 0) ready++; else if (it.Status == 1) waiting++; else invoiced++;
        }
        Console.WriteLine("   rows " + rows.Rows.Count + "   invoices " + items.Count +
                          "   ready " + ready + "   waiting " + waiting + "   invoiced " + invoiced + "   grouping " + grp);
        Console.WriteLine();

        string[] show = new string[] { "DEMO-KST", "SC 000000018", "DEMO-PG", "DEMO-3G", "DEMO-TGK", "DEMO-KKR" };
        Dictionary<string, int> perContract = new Dictionary<string, int>();
        foreach (ServiceContractPhotocopier.Classes.InvoiceRunItem it in items)
        {
            int n; perContract.TryGetValue(it.ContractNo, out n); perContract[it.ContractNo] = n + 1;
            if (Array.IndexOf(show, it.ContractNo) < 0) continue;
            Console.WriteLine("   " + it.ContractNo.PadRight(14) + (it.Kind + "  " + it.Detail).PadRight(40) +
                              ("m " + it.Machines.Count).PadRight(6) +
                              (it.MetersTotal == 0 ? "   -   " : (it.MetersRead + "/" + it.MetersTotal).PadLeft(7)) +
                              it.Amount.ToString("n2").PadLeft(12) + "   " + it.StatusText);
        }
        Console.WriteLine();

        // What the seeds promise, checked against what the layer says.
        Expect("DEMO-KST makes 2 invoices", Count(perContract, "DEMO-KST") == 2);
        Expect("DEMO-3G makes 3 invoices (one per branch)", Count(perContract, "DEMO-3G") == 3);
        Expect("DEMO-TGK makes 9 invoices (one per machine, rent apart)", Count(perContract, "DEMO-TGK") == 9);
        Expect("DEMO-KKR makes 3 invoices", Count(perContract, "DEMO-KKR") == 3);
        ServiceContractPhotocopier.Classes.InvoiceRunItem kstMeter = Find(items, "DEMO-KST", false);
        ServiceContractPhotocopier.Classes.InvoiceRunItem kstRent = Find(items, "DEMO-KST", true);
        Expect("DEMO-KST rental invoice is Ready", kstRent != null && kstRent.Status == 0);
        Expect("DEMO-KST rental invoice has no readings to wait for", kstRent != null && kstRent.MetersTotal == 0);
        Expect("DEMO-KST meter invoice is Waiting", kstMeter != null && kstMeter.Status == 1);
        Expect("DEMO-KST meter invoice counts 37 meters", kstMeter != null && kstMeter.MetersTotal == 37);
        ServiceContractPhotocopier.Classes.InvoiceRunItem pgRent = Find(items, "DEMO-PG", true);
        Expect("DEMO-PG rental invoice is Invoiced", pgRent != null && pgRent.Status == 2 && pgRent.InvoicedDocNo.Length > 0);
        foreach (ServiceContractPhotocopier.Classes.InvoiceRunItem it in items)
        {
            if (it.Status == 0 && it.Missing != 0) Fail("a Ready invoice with readings missing: " + it.ContractNo);
            if (it.Status == 1 && it.Missing <= 0) Fail("a Waiting invoice with nothing missing: " + it.ContractNo);
            foreach (DataRow r in it.Rows)
                if (Convert.ToString(r["RunJobKey"]) != it.JobKey) Fail("row stamped with another invoice: " + it.ContractNo);
        }

        // Last reading: the initial reading stands in when nothing was ever billed (same rule as the list screen).
        bool sawKjr = false;
        foreach (DataRow r in rows.Rows)
        {
            if (Convert.ToString(r["ServiceItemNo"]) == "DEMO-KJR-018" && Convert.ToString(r["Role"]) == "CL")
            {
                sawKjr = true;
                decimal last = r["LastReading"] == DBNull.Value ? -1m : Convert.ToDecimal(r["LastReading"]);
                Expect("DEMO-KJR-018 CL last reading is its initial reading 2,710 (never billed)", last == 2710m);
            }
        }
        if (!sawKjr) Fail("DEMO-KJR-018 CL not in the day's rows");

        // ---------------------------------------------------------------- the screen
        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type t = plug.GetType("ServiceContractPhotocopier.MeterReading.OperationForms.InvoiceRun_Form", true);
        Form w = (Form)Activator.CreateInstance(t, new object[] { db });
        try
        {
            w.StartPosition = FormStartPosition.Manual;
            w.Location = new Point(-4000, -4000);
            w.ShowInTaskbar = false;
            w.Show();
            Application.DoEvents();

            Control top = Find(w, "PanelFilter");
            Control box = Find(w, "GrpFilter");
            Control days = Find(w, "PanelDays");
            // The form opens on the nearest day to today; put it on the day under test.
            foreach (Control c in days.Controls)
                if (c.GetType().Name == "CheckButton" && c.Text == day.ToString())
                    c.GetType().GetProperty("Checked").SetValue(c, true, null);
            Application.DoEvents();
            Control summary = Find(w, "LblSummary");
            Control grid = Find(w, "GridInvoices");
            if (top == null || box == null || days == null || summary == null || grid == null)
            { Console.WriteLine("   a control is missing"); return 2; }

            int dayButtons = 0;
            foreach (Control c in days.Controls) if (c.GetType().Name == "CheckButton") dayButtons++;
            Console.WriteLine("   day buttons   " + dayButtons + "   strip " + days.Width + " x " + days.Height + "   top panel " + top.Width + " x " + top.Height);
            Console.WriteLine("   summary       " + summary.Text.Trim());
            // Read the view through reflection so the harness needs no DevExpress grid references.
            object gv = grid.GetType().GetProperty("MainView").GetValue(grid, null);
            int dataRowCount = (int)gv.GetType().GetProperty("DataRowCount").GetValue(gv, null);
            int rowCount = (int)gv.GetType().GetProperty("RowCount").GetValue(gv, null);
            Console.WriteLine("   grid rows     " + dataRowCount + " data rows, " + rowCount + " rows shown (groups included)");

            Expect("day strip has a button per billing day in the book", dayButtons > 0);
            Expect("the summary line names the day", summary.Text.Contains(Ordinal(day) + ":"));
            Expect("the grid shows every invoice", dataRowCount == items.Count);
            Expect("the summary sits inside Filter Options, under the strip", summary.Bottom <= box.Height && summary.Top >= days.Bottom);
            Expect("the day strip sits inside Filter Options", days.Bottom <= box.Height);
            Expect("Filter Options fits the panel", box.Bottom <= top.Height);
            Expect("nothing overlaps in Filter Options", NoOverlap(box));
            Expect("nothing overlaps in the panel", NoOverlap(top));
            Control fetchHere = null;
            foreach (Control c in top.Controls) if (c.Name == "BtnFetch") fetchHere = c;
            Expect("the Invoices view has its own Fetch button", fetchHere != null && fetchHere.Visible);
            Control head = Find(w, "PanelDetailHead");
            Expect("nothing overlaps in the detail head", head != null && NoOverlap(head));

            // The Meters view: the list screen hosted inside, with the invoice view hidden.
            Control viewMeters = Find(w, "BtnViewMeters");
            Control viewInvoices = Find(w, "BtnViewInvoices");
            Control host = Find(w, "PanelMeters");
            Control split = Find(w, "SplitMain");
            if (viewMeters != null && host != null)
            {
                viewMeters.GetType().GetProperty("Checked").SetValue(viewMeters, true, null);
                Application.DoEvents();
                bool hosted = false;
                foreach (Control c in host.Controls) if (c.GetType().Name == "MeterReadingIntegration_Form") hosted = true;
                Expect("Meters view hosts the Meter Reading Integration screen", hosted);
                Expect("Meters view hides the invoice list", split != null && !split.Visible && host.Visible);
                Control gen = Find(host, "BtnGenerateInvoice");
                Expect("the hosted list's Generate Invoice button is hidden", gen != null && !gen.Visible);
                Control selAll = Find(host, "BtnSelfManualKeyIn");
                Expect("the hosted list's Select All button is hidden", selAll != null && !selAll.Visible);
                Control fetch = Find(host, "BtnFetch");
                Expect("Fetch is still there", fetch != null && fetch.Visible);
                // Setting slid left into the hole: it now sits where Select All used to (x 818), not at 1126.
                Control refreshBtn = Find(host, "BtnRefresh");
                Control settingBtn = null;
                if (refreshBtn != null)
                    foreach (Control c in refreshBtn.Parent.Controls)
                        if (c.Text == "Setting" && c.GetType().Name == "SimpleButton") settingBtn = c;
                Expect("the hosted toolbar closed the gap (Setting moved left)", settingBtn != null && selAll != null && settingBtn.Left <= selAll.Left + 4);
                // The tick column is hidden on the hosted grid(s).
                int shownSel = 0, grids = 0;
                foreach (Control g in AllControls(host))
                {
                    if (g.GetType().Name != "GridControl") continue;
                    grids++;
                    object mv = g.GetType().GetProperty("MainView").GetValue(g, null);
                    object cols = mv.GetType().GetProperty("Columns").GetValue(mv, null);
                    object col = cols.GetType().GetMethod("get_Item", new Type[] { typeof(string) }).Invoke(cols, new object[] { "SelCssi" });
                    if (col != null && (bool)col.GetType().GetProperty("Visible").GetValue(col, null)) shownSel++;
                }
                Expect("the hosted grids hide the Select column (" + grids + " grids)", grids > 0 && shownSel == 0);
                viewInvoices.GetType().GetProperty("Checked").SetValue(viewInvoices, true, null);
                Application.DoEvents();
                Expect("back on Invoices the list is visible again", split != null && split.Visible && !host.Visible);
            }
            else Fail("view switch controls missing");
        }
        finally { w.Close(); w.Dispose(); }

        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "   ALL OK" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    private static int Count(Dictionary<string, int> d, string k) { int n; return d.TryGetValue(k, out n) ? n : 0; }

    private static ServiceContractPhotocopier.Classes.InvoiceRunItem Find(
        List<ServiceContractPhotocopier.Classes.InvoiceRunItem> items, string contractNo, bool rentalSide)
    {
        foreach (ServiceContractPhotocopier.Classes.InvoiceRunItem it in items)
            if (it.ContractNo == contractNo && it.RentalSide == rentalSide) return it;
        return null;
    }

    private static void Expect(string what, bool ok)
    {
        Console.WriteLine("   " + (ok ? "ok    " : "FAIL  ") + what);
        if (!ok) _fail++;
    }
    private static void Fail(string what) { Console.WriteLine("   FAIL  " + what); _fail++; }

    private static string Ordinal(int d)
    {
        if (d % 100 >= 11 && d % 100 <= 13) return d + "th";
        switch (d % 10) { case 1: return d + "st"; case 2: return d + "nd"; case 3: return d + "rd"; default: return d + "th"; }
    }

    private static List<Control> AllControls(Control root)
    {
        List<Control> all = new List<Control>();
        foreach (Control c in root.Controls) { all.Add(c); all.AddRange(AllControls(c)); }
        return all;
    }

    private static Control Find(Control root, string name)
    {
        if (root.Name == name) return root;
        foreach (Control c in root.Controls) { Control f = Find(c, name); if (f != null) return f; }
        return null;
    }

    private static bool NoOverlap(Control parent)
    {
        bool ok = true;
        List<Control> kids = new List<Control>();
        foreach (Control c in parent.Controls) if (c.Visible) kids.Add(c);
        for (int i = 0; i < kids.Count; i++)
            for (int j = i + 1; j < kids.Count; j++)
            {
                Rectangle a = kids[i].Bounds, b = kids[j].Bounds;
                a.Inflate(-1, -1); b.Inflate(-1, -1);
                if (a.IntersectsWith(b))
                {
                    Console.WriteLine("         overlap: " + kids[i].Name + " " + kids[i].Bounds + "  vs  " + kids[j].Name + " " + kids[j].Bounds);
                    ok = false;
                }
            }
        return ok;
    }
}
