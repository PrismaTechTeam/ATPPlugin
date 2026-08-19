using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Renders the sample invoices through AutoCount's REAL Sales Invoice layout -- the same design a
// printed invoice uses -- and proves the book is untouched while doing it.
//
// The claim being tested is not "a PDF came out". It is: the stock layout, chosen the way the print
// path chooses it, accepted a hand-built data source and drew a document from it, and the Invoice
// table has exactly as many rows afterwards as it had before.
static class RealLayoutCheck
{
    const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

    [STAThread]
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
        try { return Run(args.Length > 0 ? args[0] : ""); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(string templateName)
    {
        // AutoCount reads its built-in report designs from report.dat in the STARTUP folder. Hosted
        // outside Accounting.exe that is this harness's own bin, where no such file exists, and every
        // system layout silently disappears from the list. Inside the real plugin the two folders are
        // the same, so this line is a harness concern only.
        AutoCount.Application.PathHelper.SetStartupPath(AC);

        AutoCount.Data.DBSetting db = new AutoCount.Data.DBSetting(
            AutoCount.Data.DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        long ivBefore = Count(db, "IV");
        long dtlBefore = Count(db, "IVDTL");
        Console.WriteLine("IV rows before      : " + ivBefore + "   (IVDTL " + dtlBefore + ")");

        AutoCount.Authentication.UserSession us = new AutoCount.Authentication.UserSession(db);
        new AutoCount.MainEntry.Startup().SubProjectStartup(us, AutoCount.MainEntry.StartupPlugInOption.NoLoad);
        bool ok = AutoCount.Authentication.UserSession.CurrentUserSession.Login("ADMIN", "ADMIN");
        Console.WriteLine("login               : " + ok);
        if (!ok) { Console.WriteLine("!! cannot continue without a session"); return 3; }
        us = AutoCount.Authentication.UserSession.CurrentUserSession;

        List<ServiceContractPhotocopier.Classes.SampleInvoiceDoc> docs = Samples(db);

        string layout, error;
        DevExpress.XtraReports.UI.XtraReport xr =
            ServiceContractPhotocopier.Classes.ScpSampleInvoiceDocSource.Build(
                db, us, templateName, docs, out layout, out error);

        if (xr == null)
        {
            Console.WriteLine("RESULT              : FAILED");
            Console.WriteLine("reason              : " + error);
            Console.WriteLine("IV rows after       : " + Count(db, "IV"));
            return 1;
        }

        DevExpress.XtraPrinting.PrintingSystemBase ps = xr.PrintingSystem;
        string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string outPdf = Path.Combine(dir, "real-invoice.pdf");
        ps.ExportToPdf(outPdf);

        DevExpress.XtraPrinting.ImageExportOptions img = new DevExpress.XtraPrinting.ImageExportOptions();
        img.Format = DevExpress.Drawing.DXImageFormat.Png;
        img.Resolution = 110;
        img.ExportMode = DevExpress.XtraPrinting.ImageExportMode.SingleFilePageByPage;
        img.PageRange = "1";
        string outPng = Path.Combine(dir, "real-page.png");
        ps.ExportToImage(outPng, img);

        // and every page, so each sample invoice can actually be looked at
        DevExpress.XtraPrinting.ImageExportOptions all = new DevExpress.XtraPrinting.ImageExportOptions();
        all.Format = DevExpress.Drawing.DXImageFormat.Png;
        all.Resolution = 110;
        all.ExportMode = DevExpress.XtraPrinting.ImageExportMode.SingleFilePageByPage;
        ps.ExportToImage(Path.Combine(dir, "real-all.png"), all);

        // What the document actually SAYS. A page count proves paper was produced; only the text
        // proves the sample's own numbers reached it.
        string text = "";
        using (MemoryStream ms = new MemoryStream())
        {
            ps.ExportToText(ms, new DevExpress.XtraPrinting.TextExportOptions());
            text = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "real-page.txt"), text);
        string[] mustSay = { "MONTHLY RENTAL", "SAMPLE-RENTAL", "C.O.D.", "MS TESTER", "JALAN CONTOH", "1,200.00", "2,480.00", "3680",
                             "BLACK COPY", "947.90", "1039.35", "Y.ARCHITECTS",
                             "THREE THOUSAND SIX HUNDRED EIGHTY ONLY",
                             "ONE THOUSAND THIRTY NINE AND CENTS THIRTY FIVE ONLY" };
        List<string> missing = new List<string>();
        for (int i = 0; i < mustSay.Length; i++)
            if (text.IndexOf(mustSay[i], StringComparison.OrdinalIgnoreCase) < 0) missing.Add(mustSay[i]);

        Console.WriteLine("RESULT              : OK");
        Console.WriteLine("layout resolved     : " + (layout.Length > 0 ? layout : "(none)"));
        Console.WriteLine("pages               : " + ps.Document.Pages.Count + "   (invoices " + docs.Count + ")");
        // Page 1 must be the FIRST invoice handed in, not whichever sentinel key sorted lowest.
        // Detail lines, not the document number: the text export prints a repeating page header
        // only once, so invoice 2's "No." never appears in it.
        int atFirst = text.IndexOf("MONTHLY RENTAL", StringComparison.OrdinalIgnoreCase);
        int atSecond = text.IndexOf("BLACK COPY", StringComparison.OrdinalIgnoreCase);
        bool orderOk = atFirst >= 0 && atSecond > atFirst;
        Console.WriteLine("page 1 is invoice 1 : " + (orderOk ? "yes" : "NO -- the batch printed out of order"));

        Console.WriteLine("on the page         : " + (missing.Count == 0
            ? "all " + mustSay.Length + " expected values present"
            : "MISSING -> " + string.Join(", ", missing.ToArray())));
        Console.WriteLine("pdf                 : " + outPdf + "  (" + new FileInfo(outPdf).Length + " bytes)");
        foreach (string f in Directory.GetFiles(dir, "real-*.png"))
            Console.WriteLine("png                 : " + f + "  (" + new FileInfo(f).Length + " bytes)");

        long ivAfter = Count(db, "IV");
        long dtlAfter = Count(db, "IVDTL");
        Console.WriteLine("IV rows after       : " + ivAfter + "   (IVDTL " + dtlAfter + ")");
        Console.WriteLine(ivAfter == ivBefore && dtlAfter == dtlBefore
            ? "book untouched      : yes"
            : "!! BOOK CHANGED     : an invoice was written");


        // What the design actually asks for. Anything listed here that the sample leaves unset is a
        // field that will print blank -- which is the only way to know what still needs filling.
        Console.WriteLine();
        Console.WriteLine("---- fields this layout binds ----");
        List<string> bound = new List<string>();
        foreach (DevExpress.XtraReports.UI.Band b in xr.Bands) Collect(b, bound);
        bound.Sort();
        Console.WriteLine(string.Join(", ", bound.ToArray()));

        // The same layout, fed a REAL invoice from the book. Side by side with real-page.png this
        // says whether the sample is missing anything the printed article has.
        Control(us, layout, dir);

        return (ivAfter == ivBefore && dtlAfter == dtlBefore
                && missing.Count == 0 && orderOk && ps.Document.Pages.Count == docs.Count) ? 0 : 1;
    }

    /// <summary>Every data member and expression the design refers to, once each.</summary>
    static void Collect(DevExpress.XtraReports.UI.XRControl c, List<string> into)
    {
        foreach (DevExpress.XtraReports.UI.XRBinding bd in c.DataBindings)
            Add(into, bd.DataMember);
        foreach (DevExpress.XtraReports.UI.ExpressionBinding eb in c.ExpressionBindings)
            Add(into, eb.Expression);
        foreach (DevExpress.XtraReports.UI.XRControl k in c.Controls) Collect(k, into);
    }

    static void Add(List<string> into, string what)
    {
        string s = (what ?? "").Trim();
        if (s.Length == 0) return;
        foreach (System.Text.RegularExpressions.Match m in
            System.Text.RegularExpressions.Regex.Matches(s, @"\[([A-Za-z0-9_. ]+)\]"))
        {
            string f = m.Groups[1].Value.Trim();
            if (f.Length > 0 && !into.Contains(f)) into.Add(f);
        }
        if (s.IndexOf('[') < 0 && !into.Contains(s)) into.Add(s);
    }

    /// <summary>A genuine invoice from the book through the identical layout, as the control.</summary>
    static void Control(AutoCount.Authentication.UserSession us, string layout, string dir)
    {
        try
        {
            object o = us.DBSetting.ExecuteScalar(
                "SELECT TOP 1 DocKey FROM dbo.IV WHERE DocKey IN (SELECT DocKey FROM dbo.IVDTL) ORDER BY DocKey DESC");
            if (o == null) { Console.WriteLine("control             : no real invoice to compare against"); return; }
            long docKey = Convert.ToInt64(o);

            AutoCount.Invoicing.Sales.Invoice.InvoiceListingReport r =
                AutoCount.Invoicing.Sales.Invoice.InvoiceListingReport.Create(us);
            object ds = r.GetReportDataSource(docKey);
            AutoCount.Report.ReportTemplate t =
                AutoCount.Report.AutoCountReport.GetInstance().GetReport(layout, ds, us, true);
            DevExpress.XtraReports.UI.XtraReport cx = t.Report as DevExpress.XtraReports.UI.XtraReport;
            ServiceContractPhotocopier.Classes.ScpReportScripts.Prepare(cx);
            cx.DataSource = ds;
            cx.CreateDocument();

            DevExpress.XtraPrinting.ImageExportOptions img = new DevExpress.XtraPrinting.ImageExportOptions();
            img.Format = DevExpress.Drawing.DXImageFormat.Png;
            img.Resolution = 110;
            img.ExportMode = DevExpress.XtraPrinting.ImageExportMode.SingleFilePageByPage;
            img.PageRange = "1";
            string outPng = Path.Combine(dir, "real-control.png");
            cx.PrintingSystem.ExportToImage(outPng, img);
            Console.WriteLine("control (DocKey " + docKey + "): " + outPng);
        }
        catch (Exception ex) { Console.WriteLine("control             : failed -- " + ex.Message); }
    }

    static long Count(AutoCount.Data.DBSetting db, string table)
    {
        return Convert.ToInt64(db.ExecuteScalar("SELECT COUNT(*) FROM dbo." + table));
    }

    /// <summary>The same two invoices RenderCheck draws by hand, so the two previews can be compared
    /// side by side. The customer code is a real one from the book, so the bill-to block is real.</summary>
    static List<ServiceContractPhotocopier.Classes.SampleInvoiceDoc> Samples(AutoCount.Data.DBSetting db)
    {
        object o = db.ExecuteScalar("SELECT TOP 1 AccNo FROM dbo.Debtor ORDER BY AccNo");
        string code = o != null ? Convert.ToString(o).Trim() : "";
        Console.WriteLine("sample debtor       : " + code);

        List<ServiceContractPhotocopier.Classes.SampleInvoiceDoc> docs =
            new List<ServiceContractPhotocopier.Classes.SampleInvoiceDoc>();

        ServiceContractPhotocopier.Classes.SampleInvoiceDoc d1 =
            new ServiceContractPhotocopier.Classes.SampleInvoiceDoc();
        d1.Title = "TAX INVOICE";
        d1.DebtorCode = code;
        d1.DebtorName = "DEMO CUSTOMER SDN BHD";
        d1.ContractNo = "DEMO-05";
        d1.Note = "rental only";
        // the caller-supplied header fields, so the class is exercised on the path the form uses
        d1.DocNo = "SAMPLE-RENTAL";
        d1.DocDate = new DateTime(2026, 8, 1);
        d1.Terms = "C.O.D.";
        d1.Attention = "MS TESTER";
        d1.DebtorAddress = "LOT 1, JALAN CONTOH\r\nTAMAN PERINDUSTRIAN\r\n81100 JOHOR BAHRU\r\nJOHOR";
        d1.Lines.Add(Line("MONTHLY RENTAL (13/36)", "MODEL:iR-ADV 6580i  S/N:DEMO05-001",
            "DEMO-05-001  DEMO05-001", "", 1, 1200m, 1200m, false));
        d1.Lines.Add(Line("MONTHLY RENTAL (13/36)", "MODEL:iR-ADV DX 4960i, iR-ADV DX C5760i  4 UNIT",
            "4 machines:  DEMO05-002, DEMO05-003, DEMO05-004, DEMO05-005", "", 4, 620m, 2480m, false));
        docs.Add(d1);

        ServiceContractPhotocopier.Classes.SampleInvoiceDoc d2 =
            new ServiceContractPhotocopier.Classes.SampleInvoiceDoc();
        d2.Title = "TAX INVOICE";
        d2.DebtorCode = code;
        d2.DebtorName = "DEMO CUSTOMER SDN BHD";
        d2.ContractNo = "DEMO-05";
        d2.Note = "meters only";
        d2.Lines.Add(Line("BLACK COPY + PRINT A4 & A3", "MODEL:iR-ADV 6580i  S/N:DEMO05-001",
            "DEMO-05-001  DEMO05-001", "", 4813, 0.019m, 91.45m, true));
        d2.Lines.Add(Line("BLACK COPY + PRINT A4 & A3", "MODEL:iR-ADV DX 4960i, iR-ADV DX C5760i  5 UNIT",
            "5 machines:  DEMO05-002, DEMO05-003, DEMO05-004, DEMO05-005, DEMO05-006", "",
            33260, 0.0285m, 947.90m, true));
        d2.Lines.Add(Line("MINIMUM COMMITTED PRINT CHARGES", "", "DEMO-05-002  DEMO05-002",
            "COMMITTED MIN 500.00 -- bills the shortfall, worked out at Generate", 1, 0m, 0m, false));
        docs.Add(d2);

        return docs;
    }

    static ServiceContractPhotocopier.Classes.SampleInvoiceLine Line(string desc, string sub,
        string covers, string note, decimal qty, decimal price, decimal amount, bool showQty)
    {
        ServiceContractPhotocopier.Classes.SampleInvoiceLine l =
            new ServiceContractPhotocopier.Classes.SampleInvoiceLine();
        l.Description = desc; l.SubDescription = sub; l.Covers = covers; l.Note = note;
        l.Qty = qty; l.UnitPrice = price; l.Amount = amount; l.ShowQty = showQty;
        return l;
    }
}
