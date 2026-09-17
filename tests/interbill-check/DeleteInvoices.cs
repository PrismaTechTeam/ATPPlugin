using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;

/// <summary>
/// Deletes A/R invoices from a book, through AutoCount's own command rather than through SQL.
///
/// <para>An invoice is not one row. It is the document, its details, the debtor's ledger, the
/// stock movement and the audit trail, and AutoCount writes all of it from one place. Deleting the
/// row by hand leaves the rest behind, and the book only tells you months later, in a figure
/// nobody can explain. <c>InvoiceCommand.Delete</c> takes it all out the way AutoCount put it in.
/// </para>
///
/// <para>It exists for demo books: a test invoice generated while proving the module works is
/// still a real document, and it sits in the A/R list of the book somebody is about to record.
/// </para>
///
///   delete-invoices.ps1 -Database AED_ASNDUMMY -DocNo I-000005
///   delete-invoices.ps1 -Database AED_ASNDUMMY -All
/// </summary>
internal static class DeleteInvoices
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

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
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex.Message); return 2; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string[] args)
    {
        string book = args.Length > 0 ? args[0] : "";
        string which = args.Length > 1 ? args[1] : "";
        if (book.Length == 0 || which.Length == 0)
        {
            Console.WriteLine("usage: deleteinvoices <book> <DocNo | ALL>");
            return 2;
        }

        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        AutoCount.Authentication.UserSession ses = new AutoCount.Authentication.UserSession(db);
        if (!AutoCount.Authentication.UserSession.CurrentUserSession.Login("ADMIN", "ADMIN"))
        {
            Console.WriteLine("   could not log in to " + book);
            return 1;
        }

        string where = string.Equals(which, "ALL", StringComparison.OrdinalIgnoreCase)
            ? "1=1"
            : "DocNo = N'" + which.Replace("'", "''") + "'";
        DataTable t = db.GetDataTable(
            "SELECT DocKey, DocNo, DebtorCode, NetTotal FROM dbo.ARInvoice WHERE " + where +
            " ORDER BY DocKey", false);

        Console.WriteLine();
        Console.WriteLine("   book   " + book);
        Console.WriteLine();
        if (t.Rows.Count == 0)
        {
            Console.WriteLine("   nothing to delete");
            Console.WriteLine();
            return 0;
        }

        AutoCount.Invoicing.Sales.Invoice.InvoiceCommand cmd =
            AutoCount.Invoicing.Sales.Invoice.InvoiceCommand.Create(
                AutoCount.Authentication.UserSession.CurrentUserSession, db);

        int gone = 0, failed = 0;
        foreach (DataRow r in t.Rows)
        {
            long key = Convert.ToInt64(r["DocKey"]);
            string no = Convert.ToString(r["DocNo"]);
            try
            {
                // Delete(long) reported "record not found" for a DocKey that plainly exists, so the
                // document is named the way a person names it. The key is still read, to fail loudly
                // if the two ever disagree.
                if (key <= 0) throw new ApplicationException("no DocKey");
                cmd.Delete(no);
                gone++;
                Console.WriteLine("   ok   deleted " + no + "  " +
                                  Convert.ToDecimal(r["NetTotal"]).ToString("n2") +
                                  "  (" + Convert.ToString(r["DebtorCode"]) + ")");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine("   --   " + no + " refused: " + ex.Message);
            }
        }
        Console.WriteLine();
        Console.WriteLine("   " + gone + " deleted, " + failed + " refused");
        Console.WriteLine();
        return failed > 0 ? 1 : 0;
    }
}
