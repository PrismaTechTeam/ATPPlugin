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

        int bricks = 0;
        foreach (DevExpress.XtraPrinting.Page pg in ps.Document.Pages) bricks += pg.InnerBricks.Count;

        Console.WriteLine("RESULT              : OK");
        Console.WriteLine("layout resolved     : " + (layout.Length > 0 ? layout : "(none)"));
        Console.WriteLine("pages               : " + ps.Document.Pages.Count);
        Console.WriteLine("bricks              : " + bricks);
        Console.WriteLine("pdf                 : " + outPdf + "  (" + new FileInfo(outPdf).Length + " bytes)");
        foreach (string f in Directory.GetFiles(dir, "real-page*.png"))
            Console.WriteLine("png                 : " + f + "  (" + new FileInfo(f).Length + " bytes)");

        long ivAfter = Count(db, "IV");
        long dtlAfter = Count(db, "IVDTL");
        Console.WriteLine("IV rows after       : " + ivAfter + "   (IVDTL " + dtlAfter + ")");
        Console.WriteLine(ivAfter == ivBefore && dtlAfter == dtlBefore
            ? "book untouched      : yes"
            : "!! BOOK CHANGED     : an invoice was written");

        if (bricks < 20) Console.WriteLine("!! suspiciously few bricks -- the page may be blank");
        return (ivAfter == ivBefore && dtlAfter == dtlBefore && bricks >= 20) ? 0 : 1;
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
