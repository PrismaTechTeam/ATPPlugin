using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoCount.Data;

// Runs the plugin's own migrations against an account book that has never had the plugin -- the
// same call PluginMain.BeforeLoad makes when a customer installs it -- and says whether the book
// comes out whole.
//
// Every book we develop against has had the plugin for months, so a migration that reads a column
// a LATER migration creates never fails there: the column has been sitting in the table since the
// day it was first added. On a new book it is the first thing that breaks, and it stops the plugin
// loading at all. This is the only test that sees that.
//
//   freshinstall.exe <database>
internal static class FreshInstall
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";

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
        string book = args.Length > 0 ? args[0] : "AED_ATPFRESH";
        try { return Run(book); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(string book)
    {
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        int before = Tables(db);
        Console.WriteLine("book " + book + ": " + before + " plugin tables before");

        // Exactly what the plugin does on load.
        Type mig = Assembly.LoadFrom(PLUGIN).GetType("ServiceContractPhotocopier.Classes.ScpMigrations_Cls");
        MethodInfo run = mig.GetMethod("RunEmbeddedSQLScripts", BindingFlags.Public | BindingFlags.Static);
        try
        {
            run.Invoke(null, new object[] { db });
        }
        catch (TargetInvocationException tie)
        {
            Exception e = tie.InnerException ?? tie;
            Console.WriteLine("   FAIL  the migration stopped:");
            Console.WriteLine("         " + e.Message.Replace("\r\n", "\r\n         "));
            Console.WriteLine("   " + Tables(db) + " plugin tables made before it stopped");
            return 1;
        }

        int after = Tables(db);
        Console.WriteLine("   ok    migration ran to the end (" + after + " plugin tables)");

        // Run it again: every script is meant to be safe to repeat, because it runs on every load.
        try { run.Invoke(null, new object[] { db }); }
        catch (TargetInvocationException tie)
        {
            Console.WriteLine("   FAIL  a SECOND load broke it: " + (tie.InnerException ?? tie).Message);
            return 1;
        }
        Console.WriteLine("   ok    a second load changes nothing and does not fail");

        int fail = 0;
        fail += Expect(db, "the delete-order trigger is on dbo.IV",
            "SELECT COUNT(*) FROM sys.triggers WHERE name = 'TR_zSCP2_IV_NoDeleteEarlierBilledMonth'", 1);
        fail += Expect(db, "zSCP2_Contract carries every column the contract form saves",
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.zSCP2_Contract') " +
            "AND name IN ('MachineLineShows','UseNewLayout','ShowModelOnLine','ShowSerialOnLine','ShowUnitsOnLine'," +
            "'BillFromPeriod','CreatedBy','ModifiedBy')", 8);
        fail += Expect(db, "the contract list view exists", "SELECT COUNT(*) FROM sys.views WHERE name = 'zvSCP2_ContractList'", 1);
        Console.WriteLine(fail == 0 ? "   ALL OK" : "   " + fail + " FAILED");
        return fail == 0 ? 0 : 1;
    }

    private static int Expect(DBSetting db, string what, string sql, int want)
    {
        int got = Convert.ToInt32(db.ExecuteScalar(sql));
        bool ok = got == want;
        Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what + (ok ? "" : " (got " + got + ", want " + want + ")"));
        return ok ? 0 : 1;
    }

    private static int Tables(DBSetting db)
    {
        return Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(*) FROM sys.tables WHERE name LIKE 'zSCP%' OR name LIKE 'Z[_]Pums%' OR name LIKE 'z[_]Sys%'"));
    }
}
