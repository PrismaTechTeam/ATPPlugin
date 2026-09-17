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
/// Installs this plug-in into an account book that is not the dev one, and brings it up to the state
/// a book reaches after AutoCount has opened it once.
///
/// <para>Inter-billing needs a SECOND real account book, and a second book is not something the dev
/// launcher can reach: it installs into whatever App.config names, and App.config is the developer's
/// own book. Rather than repointing that (and quietly changing where every later run goes), this
/// does the same three things the launcher and the plug-in do between them, against a book named on
/// the command line.</para>
///
/// <list type="number">
///   <item>register the package -- <c>PlugInManager.InstallPackage</c>, exactly as ATPShadowMain
///         does, so AutoCount lists the plug-in and shows its menu;</item>
///   <item>run the schema migrations -- <c>ScpMigrations_Cls.RunEmbeddedSQLScripts</c>, the same
///         call <c>PluginMain.BeforeLoad</c> makes, which is what creates the zSCP2 tables and this
///         book's permanent identity;</item>
///   <item>grant the plug-in's access rights to a user, because declaring a right gives it to
///         nobody and a book whose menus are all hidden looks like a failed install.</item>
/// </list>
///
/// <para>Everything here is idempotent. Running it twice re-registers the same package and re-runs
/// migrations that are all guarded.</para>
/// </summary>
internal static class InstallIntoBook
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    private const string APP =
        @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\ServiceContractPhotocopier.app";

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
        if (args.Length < 1)
        {
            Console.WriteLine("usage: installintobook <database> [server] [sql user] [sql password] [grant to]");
            return 1;
        }
        string dbName = args[0];
        string server = args.Length > 1 ? args[1] : "localhost,1433";
        string user = args.Length > 2 ? args[2] : "sa";
        string pass = args.Length > 3 ? args[3] : "rs6663";
        string grantTo = args.Length > 4 ? args[4] : "ADMIN";

        DBSetting db = new DBSetting(DBServerType.SQL2000, server, user, pass, dbName, false);

        Console.WriteLine();
        Console.WriteLine("   book      " + dbName + " on " + server);
        Console.WriteLine("   package   " + APP + "  (exists=" + File.Exists(APP) + ")");
        Console.WriteLine();

        // -------------------------------------------------------------- 1. register the package
        // The licence and OEM globals have to be up before PlugInManager will talk to a book; this is
        // the same call ATPShadowMain makes for the same reason.
        AutoCount.MainEntry.MainStartup.Default.SetDefaultOEM();
        AutoCount.PlugIn.PlugInManager.InstallPackage(db, APP, true);
        Console.WriteLine("   ok   package registered");

        // -------------------------------------------------------------- 2. the schema
        ScpMigrations_Cls.RunEmbeddedSQLScripts(db);
        Console.WriteLine("   ok   migrations run");

        Guid id = ScpBookIdentity.Local(db);
        Console.WriteLine("   ok   book identity " + id.ToString().ToUpperInvariant());
        if (id == Guid.Empty)
        {
            Console.WriteLine("   FAIL this book has no identity -- nothing can be linked to it");
            return 1;
        }

        // -------------------------------------------------------------- 3. the rights
        // Declaring a right only makes it appear in the Access Rights screen; dbo.AccessRight is a
        // GRANT table, and a row is what lets somebody see the menu item. A brand-new book has no
        // rows at all, so without this every one of the plug-in's screens is invisible -- which reads
        // as "the install failed" rather than "you have not been given it".
        int granted = GrantAll(db, grantTo);
        Console.WriteLine("   ok   " + granted + " access rights granted to " + grantTo);

        Console.WriteLine();
        Console.WriteLine("   tables " + Scalar(db,
            "SELECT COUNT(*) FROM sys.tables WHERE name LIKE 'zSCP%'") + " zSCP* now in the book");
        Console.WriteLine();
        return 0;
    }

    /// <summary>Every right the plug-in declares, read off <see cref="AccessRightsConsts"/> itself so
    /// the list cannot drift from the one the menus check.</summary>
    private static int GrantAll(DBSetting db, string userId)
    {
        List<string> cmds = new List<string>();
        FieldInfo[] fields = typeof(AccessRightsConsts).GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        foreach (FieldInfo f in fields)
        {
            if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
            string v = Convert.ToString(f.GetRawConstantValue());
            if (v != null && v.StartsWith("CMD_", StringComparison.Ordinal)) cmds.Add(v);
        }

        int n = 0;
        using (SqlConnection cn = new SqlConnection(db.ConnectionString))
        {
            cn.Open();
            foreach (string cmd in cmds)
            {
                using (SqlCommand c = new SqlCommand(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.AccessRight WHERE CmdID=@c AND UserID=@u) " +
                    "INSERT INTO dbo.AccessRight (CmdID, UserID) VALUES (@c, @u)", cn))
                {
                    c.Parameters.AddWithValue("@c", cmd);
                    c.Parameters.AddWithValue("@u", userId);
                    try { c.ExecuteNonQuery(); n++; } catch { }
                }
            }
        }
        return n;
    }

    private static string Scalar(DBSetting db, string sql)
    {
        object o = db.ExecuteScalar(sql);
        return o == null || o == DBNull.Value ? "0" : Convert.ToString(o);
    }
}
