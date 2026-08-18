using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Renders DEMO-05's sample invoice through the real report and exports it to PDF, so the document
// can be looked at rather than assumed. A preview nobody has ever seen rendered is a preview nobody
// knows renders.
static class RenderCheck
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
        try { Run(args.Length > 0 ? args[0] : "DEMO-05"); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run(string contractNo)
    {
        var docs = new List<ServiceContractPhotocopier.Classes.SampleInvoiceDoc>();

        var d1 = new ServiceContractPhotocopier.Classes.SampleInvoiceDoc();
        d1.Title = "TAX INVOICE";
        d1.DebtorCode = "3000-A0002";
        d1.DebtorName = "DEMO CUSTOMER SDN BHD";
        d1.ContractNo = contractNo;
        d1.Note = "rental";
        d1.Lines.Add(Line("MONTHLY RENTAL (13/36)", "MODEL:iR-ADV 6580i  S/N:DEMO05-001",
            "DEMO-05-001  DEMO05-001", "", 1, 1200m, 1200m, false));
        d1.Lines.Add(Line("MONTHLY RENTAL (13/36)", "MODEL:iR-ADV DX 4960i, iR-ADV DX C5760i  4 UNIT",
            "4 machines:  DEMO05-002, DEMO05-003, DEMO05-004, DEMO05-005", "", 4, 620m, 2480m, false));

        var d2 = new ServiceContractPhotocopier.Classes.SampleInvoiceDoc();
        d2.Title = "TAX INVOICE";
        d2.DebtorCode = "3000-A0002";
        d2.DebtorName = "DEMO CUSTOMER SDN BHD";
        d2.ContractNo = contractNo;
        d2.Note = "meters";
        d2.Lines.Add(Line("BLACK COPY + PRINT A4 & A3", "MODEL:iR-ADV 6580i  S/N:DEMO05-001",
            "DEMO-05-001  DEMO05-001", "", 4813, 0.019m, 91.45m, true));
        d2.Lines.Add(Line("BLACK COPY + PRINT A4 & A3", "MODEL:iR-ADV DX 4960i, iR-ADV DX C5760i  5 UNIT",
            "5 machines:  DEMO05-002, DEMO05-003, DEMO05-004, DEMO05-005, DEMO05-006", "",
            33260, 0.0285m, 947.90m, true));
        d2.Lines.Add(Line("MINIMUM COMMITTED PRINT CHARGES", "",
            "DEMO-05-002  DEMO05-002",
            "COMMITTED MIN 500.00 — bills the shortfall, worked out at Generate", 1, 0m, 0m, false));

        docs.Add(d1);
        docs.Add(d2);

        var rpt = ServiceContractPhotocopier.Classes.ScpSampleInvoiceReport.Create(docs,
            "The SHAPE is real. Only the meter READINGS are invented.");
        rpt.CreateDocument();
        var ps = rpt.PrintingSystem;

        string outPdf = Path.Combine(Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().Location), "sample-invoice.pdf");
        ps.ExportToPdf(outPdf);
        var imgOpt = new DevExpress.XtraPrinting.ImageExportOptions();
        imgOpt.Format = System.Drawing.Imaging.ImageFormat.Png;
        imgOpt.Resolution = 110;
        imgOpt.ExportMode = DevExpress.XtraPrinting.ImageExportMode.SingleFilePageByPage;
        imgOpt.PageRange = "1";
        ps.ExportToImage(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outPdf), "page.png"), imgOpt);

        Console.WriteLine("pages   : " + ps.Document.Pages.Count);
        Console.WriteLine("bricks  : " + CountBricks(ps));
        Console.WriteLine("exported: " + outPdf + "   (" + new FileInfo(outPdf).Length + " bytes)");
        if (ps.Document.Pages.Count != 2)
            Console.WriteLine("!! expected 2 pages — one invoice per sheet");
        else
            Console.WriteLine("ok      : one invoice per sheet");
    }

    static int CountBricks(DevExpress.XtraPrinting.PrintingSystemBase ps)
    {
        int n = 0;
        foreach (DevExpress.XtraPrinting.Page pg in ps.Document.Pages)
            n += pg.InnerBricks.Count;
        return n;
    }

    static ServiceContractPhotocopier.Classes.SampleInvoiceLine Line(string desc, string sub,
        string covers, string note, decimal qty, decimal price, decimal amount, bool showQty)
    {
        var l = new ServiceContractPhotocopier.Classes.SampleInvoiceLine();
        l.Description = desc; l.SubDescription = sub; l.Covers = covers; l.Note = note;
        l.Qty = qty; l.UnitPrice = price; l.Amount = amount; l.ShowQty = showQty;
        return l;
    }
}
