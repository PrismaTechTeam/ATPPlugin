using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

/// <summary>
/// Builds the inter-billing screens once, off-screen, against the real book — and reports what
/// throws.
///
/// <para>A WinForms fault does not show up in a compile and does not show up in the engine harness:
/// a designer file the parser accepts but the runtime chokes on, a null column name, a lookup bound
/// before its grid exists. All of it waits until somebody clicks the menu item. This clicks it
/// first.</para>
///
/// <para>Nothing is shown. <c>CreateControl()</c> forces the handle, which is what raises
/// <c>Load</c> — so every line of OnFormLoad runs, the grids bind, and the forms are disposed. If
/// this is silent, the menu item opens.</para>
/// </summary>
internal static class SmokeForms
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
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        Console.WriteLine();
        Console.WriteLine("   book   " + book);
        Console.WriteLine();

        _expectInTitle = "Inter-Billing Setup";
        Build("Inter-Billing Setup", delegate
        {
            return new ServiceContractPhotocopier.GeneralSetup.MasterForms.InterBillSetup_Form(db);
        });

        _expectInTitle = "Inter-Billing";
        Build("Inter-Billing", delegate
        {
            return new ServiceContractPhotocopier.ServiceContract.OperationForms.InterBilling_Form(db);
        });

        // The contract editor is not new, but the inter-billing work reaches into it: the window
        // title, the borrowed-counter cache the panel builds on bind, and the save gate. Both kinds
        // of contract are opened -- one taken from another book, one of this book's own -- because
        // the interesting risk is a screen that now works only for the case it was written for.
        long borrowed = FirstContract(db,
            "SELECT TOP 1 LocalKey FROM dbo.zSCP2_InterBillLink WHERE EntityType = 'CONTRACT' AND LocalKey > 0");
        long plain = FirstContract(db,
            "SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractKey NOT IN " +
            "(SELECT LocalKey FROM dbo.zSCP2_InterBillLink WHERE EntityType = 'CONTRACT') ORDER BY ContractKey");

        if (borrowed > 0)
        {
            _expectInTitle = "machines from";
            Build("Service Contract (taken from another book)", delegate
            {
                return new ServiceContractPhotocopier.ServiceContract.OperationForms
                    .zSCP2_Contract_Form(db, borrowed);
            });
        }
        else
            Console.WriteLine("   --   no inter-billed contract in this book to open");

        if (plain > 0)
        {
            _expectInTitle = "EDIT";
            Build("Service Contract (this book's own)", delegate
            {
                return new ServiceContractPhotocopier.ServiceContract.OperationForms
                    .zSCP2_Contract_Form(db, plain);
            });
        }

        // "Correct with CN". Its whole job is one long query joining the meter trail, the billing
        // log, the tier tables and three OUTER APPLYs -- exactly the kind of thing a compiler is
        // happy with and SQL Server is not. Opened against a REAL invoice of this book so the
        // query runs and the grid binds.
        long invKey = 0; string invNo = "";
        try
        {
            System.Data.DataTable iv = db.GetDataTable(
                "SELECT TOP 1 iv.DocKey, iv.DocNo FROM dbo.IV iv " +
                " JOIN dbo.zSCP_MeterTrans t ON t.SalesInvoiceDocKey = iv.DocKey " +
                " WHERE ISNULL(iv.Cancelled,'F') <> 'T' ORDER BY iv.DocKey DESC", false);
            if (iv.Rows.Count > 0)
            {
                invKey = Convert.ToInt64(iv.Rows[0]["DocKey"]);
                invNo = Convert.ToString(iv.Rows[0]["DocNo"]);
            }
        }
        catch { }

        if (invKey > 0)
        {
            _expectInTitle = "Credit Note";
            Build("Correct with CN  (" + invNo + ")", delegate
            {
                return new ServiceContractPhotocopier.MeterCN_Form(db, invKey, invNo);
            });
        }
        else
            Console.WriteLine("   --   no meter invoice in this book to correct");

        // The database picker reads a live server list. It is the one screen here that talks to
        // something other than this book, so it is worth opening for real rather than trusting it.
        _expectInTitle = "Choose a database";
        Build("Choose a database", delegate
        {
            System.Data.SqlClient.SqlConnectionStringBuilder b =
                new System.Data.SqlClient.SqlConnectionStringBuilder();
            b.DataSource = "localhost,1433";
            b.InitialCatalog = "master";
            b.UserID = "sa";
            b.Password = "rs6663";
            b.ConnectTimeout = 15;
            return new ServiceContractPhotocopier.GeneralSetup.MasterForms
                .PickDatabase_Form(b.ConnectionString);
        });

        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "   every screen builds and loads" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    private static long FirstContract(DBSetting db, string sql)
    {
        try
        {
            object o = db.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0L : Convert.ToInt64(o);
        }
        catch { return 0L; }
    }

    private delegate DevExpress.XtraEditors.XtraForm Maker();

    /// <summary>Something the window title MUST say once the form has loaded. It is the cheapest
    /// proof that Load actually ran and got to the end -- a form that throws half way through leaves
    /// the designer's caption in place and otherwise looks fine.</summary>
    private static string _expectInTitle = "";

    private static string Q(string s) { return "\"" + s + "\""; }

    private static void Build(string what, Maker make)
    {
        DevExpress.XtraEditors.XtraForm f = null;
        try
        {
            f = make();

            // CreateControl() makes the handle. It does NOT reliably raise Load -- which is the
            // whole point of the exercise, because everything these screens do on opening is in
            // their Load handler. The first version of this file stopped here and reported "ok" for
            // a form whose Load had never run: a test that passes without testing anything.
            //
            // OnLoad is raised directly instead. It is the same event the menu item raises, it runs
            // every handler, and it needs no window on screen.
            f.CreateControl();
            MethodInfo onLoad = typeof(Form).GetMethod(
                "OnLoad", BindingFlags.Instance | BindingFlags.NonPublic);
            onLoad.Invoke(f, new object[] { EventArgs.Empty });
            Application.DoEvents();

            Console.WriteLine("   ok   " + what + "  (" + f.Text + ")");
            if (_expectInTitle.Length > 0 && f.Text.IndexOf(_expectInTitle, StringComparison.Ordinal) < 0)
            {
                _fail++;
                Console.WriteLine("   FAIL the title should contain " + Q(_expectInTitle) +
                                  " -- the load path did not finish");
            }
        }
        catch (Exception ex)
        {
            _fail++;
            Console.WriteLine("   FAIL " + what);
            Console.WriteLine("        " + ex.GetType().Name + ": " + ex.Message);
            Exception inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 3)
            {
                Console.WriteLine("        inner: " + inner.GetType().Name + ": " + inner.Message);
                inner = inner.InnerException;
                depth++;
            }
            string st = ex.StackTrace ?? "";
            foreach (string line in st.Split('\n'))
            {
                if (line.IndexOf("ServiceContractPhotocopier", StringComparison.Ordinal) >= 0)
                {
                    Console.WriteLine("        at" + line.Trim().Substring(2));
                    break;
                }
            }
        }
        finally
        {
            if (f != null) { try { f.Dispose(); } catch { } }
        }
    }
}
