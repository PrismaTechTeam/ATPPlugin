using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

/// <summary>
/// What "View Sample Invoice" actually prints, read off the form that prints it.
///
/// <para>The sample is the picture people check a price against before they sign anything, and it
/// has never had a test. It builds its own lines -- invented readings, the agreed prices as the
/// screen is holding them right now -- so it can disagree with the billing run without either of
/// them being obviously wrong.</para>
///
/// <para>The case this was written for: a merged rental line whose agreed price is "same as now",
/// i.e. no agreed price at all. There is nothing in the group price map, so every machine keeps its
/// own rate -- and the question is whether the folded line still prints one.</para>
/// </summary>
internal static class SampleInvoiceCheck
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
        string contractNo = args.Length > 1 ? args[1] : "DEMO-DUTY";
        // "agreed" = hand the sample the contract's agreed prices, the way the screen does.
        // "sameasnow" = hand it none, which is what every line reading "same as now" means.
        bool withPrices = !(args.Length > 2 && args[2].Equals("sameasnow", StringComparison.OrdinalIgnoreCase));

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);

        Console.WriteLine();
        Console.WriteLine("   book       " + book + "   contract " + contractNo);
        Console.WriteLine("   prices     " + (withPrices ? "the contract's agreed figures" : "NONE - every line \"same as now\""));

        object o = db.ExecuteScalar(
            "SELECT TOP 1 ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'" +
            contractNo.Replace("'", "''") + "'");
        if (o == null || o == DBNull.Value) { Console.WriteLine("   no such contract"); return 2; }
        long ck = Convert.ToInt64(o);

        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type contractForm = plug.GetType(
            "ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form", true);
        MethodInfo loadOne = contractForm.GetMethod("LoadOneItem",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Type itemEdit = loadOne.ReturnType;
        Type listOfItems = typeof(List<>).MakeGenericType(itemEdit);
        object items = Activator.CreateInstance(listOfItems);
        MethodInfo add = listOfItems.GetMethod("Add");
        DataTable it = db.GetDataTable(
            "SELECT ItemKey FROM dbo.zSCP2_Item WHERE ContractKey = " + ck + " ORDER BY Pos, ItemKey", false);
        foreach (DataRow r in it.Rows)
            add.Invoke(items, new object[] { loadOne.Invoke(null, new object[] { db, Convert.ToInt64(r["ItemKey"]) }) });

        // The agreed rental figures, keyed the way the fold engine keys a rental line.
        Dictionary<string, decimal> groupPrices =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (withPrices)
        {
            DataTable gp = db.GetDataTable(
                "SELECT GroupCode, UnitPrice FROM dbo.zSCP2_ContractRentalPrice " +
                " WHERE ContractKey = " + ck + " AND Side = 'R' AND ISNULL(UnitPrice,0) > 0", false);
            foreach (DataRow r in gp.Rows)
                groupPrices[Convert.ToString(r["GroupCode"]).Trim()] = Convert.ToDecimal(r["UnitPrice"]);
        }
        Say("agreed rental lines handed over", groupPrices.Count.ToString());

        Type sample = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.SampleInvoice_Form", false)
                      ?? plug.GetType("ServiceContractPhotocopier.SampleInvoice_Form", true);
        // The contract's own split flags, not a guess -- the preview's whole job is to show what
        // THIS contract will send, and a harness that hard-codes them proves nothing about it.
        bool rentalSeparate = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(RentalSeparateInvoice,'N') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)).Trim() == "Y";
        bool perMachine = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(BillingMode,'G') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)).Trim() == "S";
        string shows = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(NULLIF(MachineLineShows,''),'B') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)).Trim();
        Say("rental apart / per machine", (rentalSeparate ? "Y" : "N") + " / " + (perMachine ? "Y" : "N"));

        object f = Activator.CreateInstance(sample, new object[]
        {
            db, items, contractNo, "3000-A0074", "", true,
            'G', 'G', rentalSeparate, perMachine, groupPrices, "",
            shows.Length > 0 ? char.ToUpperInvariant(shows[0]) : 'B'
        });

        try
        {
            MethodInfo build = sample.GetMethod("Build",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            object docs = build.Invoke(f, null);

            Console.WriteLine();
            int blank = 0, rows = 0;
            foreach (object doc in (IEnumerable)docs)
            {
                Type dt = doc.GetType();
                object lines = Field(dt, doc, "Lines") ?? Prop(dt, doc, "Lines");
                Console.WriteLine("   invoice   " + Convert.ToString(Field(dt, doc, "Title") ?? Prop(dt, doc, "Title")));
                if (lines == null) continue;
                foreach (object row in (IEnumerable)lines)
                {
                    Type rt = row.GetType();
                    decimal price = Dec(Prop(rt, row, "UnitPrice") ?? Field(rt, row, "UnitPrice"));
                    decimal qty = Dec(Prop(rt, row, "Qty") ?? Field(rt, row, "Qty"));
                    decimal amt = Dec(Prop(rt, row, "Amount") ?? Field(rt, row, "Amount"));
                    string what = Convert.ToString(Prop(rt, row, "Description") ?? Field(rt, row, "Description"));
                    rows++;
                    if (price == 0m && amt != 0m) blank++;
                    Console.WriteLine("        " + what.PadRight(8) +
                                      " qty " + qty.ToString("n0").PadLeft(9) +
                                      " @ " + price.ToString("n4").PadLeft(12) +
                                      " = " + amt.ToString("n2").PadLeft(12) +
                                      (price == 0m && amt != 0m ? "   <-- price printed as ZERO" : ""));
                }
            }
            Console.WriteLine();
            Say("rows", rows.ToString());
            Check("no row prints an amount at a zero price", blank == 0);
        }
        finally { ((IDisposable)f).Dispose(); }

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    private static object Field(Type t, object o, string name)
    {
        FieldInfo fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fi == null ? null : fi.GetValue(o);
    }

    private static object Prop(Type t, object o, string name)
    {
        PropertyInfo pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return pi == null ? null : pi.GetValue(o, null);
    }

    private static decimal Dec(object v)
    {
        return v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(34) + value);
    }

    private static void Check(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what);
    }
}

