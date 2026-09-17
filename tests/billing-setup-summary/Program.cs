using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

/// <summary>
/// What the Meters &amp; Pricing screen says at the bottom, read off the real screen.
///
/// <para>That line answers "how many invoices does this contract send", and it is the one number
/// on the screen a person checks against the Bill group column two inches above it. It used to be
/// a fixed phrase per split option -- "1 invoice" over three bill groups -- so the screen
/// contradicted itself and nothing caught it, because no test opens this dialog.</para>
///
/// <para>The dialog is built the way the contract editor builds it, its Load is run, and the label
/// is read. Nothing is shown and nothing is saved.</para>
/// </summary>
internal static class BillingSetupSummary
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
        string contractNo = args.Length > 1 ? args[1] : "DEMO-3G";
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);
        Console.WriteLine();
        Console.WriteLine("   book       " + book + "   contract " + contractNo);

        DataTable c = db.GetDataTable(
            "SELECT ContractKey, ISNULL(RentalSeparateInvoice,'N') AS RSep, ISNULL(BillingMode,'G') AS BM " +
            "  FROM dbo.zSCP2_Contract WHERE ContractNo = N'" + contractNo.Replace("'", "''") + "'", false);
        if (c.Rows.Count == 0) { Console.WriteLine("   no such contract"); return 2; }
        long ck = Convert.ToInt64(c.Rows[0]["ContractKey"]);

        int groups = Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(DISTINCT BillGroupCode) FROM dbo.zSCP2_Item " +
            " WHERE ContractKey = " + ck + " AND ISNULL(BillGroupCode,'') <> ''"));
        int ungrouped = Convert.ToInt32(db.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.zSCP2_Item " +
            " WHERE ContractKey = " + ck + " AND ISNULL(BillGroupCode,'') = ''"));
        int expected = groups + (ungrouped > 0 ? 1 : 0);
        if (expected < 1) expected = 1;
        Say("bill groups on the machines", groups + (ungrouped > 0 ? " (+ " + ungrouped + " ungrouped)" : ""));
        Say("so the contract sends", expected + (expected == 1 ? " invoice" : " invoices"));

        // The items, loaded the way the contract editor loads them.
        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type contractForm = plug.GetType(
            "ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form", true);
        MethodInfo loadOne = contractForm.GetMethod("LoadOneItem",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (loadOne == null) { Console.WriteLine("   LoadOneItem not found"); return 2; }

        Type itemEdit = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.ItemEditData", false)
                        ?? loadOne.ReturnType;
        Type listOfItems = typeof(List<>).MakeGenericType(itemEdit);
        object items = Activator.CreateInstance(listOfItems);
        MethodInfo add = listOfItems.GetMethod("Add");

        DataTable it = db.GetDataTable(
            "SELECT ItemKey FROM dbo.zSCP2_Item WHERE ContractKey = " + ck + " ORDER BY Pos, ItemKey", false);
        foreach (DataRow r in it.Rows)
            add.Invoke(items, new object[] { loadOne.Invoke(null, new object[] { db, Convert.ToInt64(r["ItemKey"]) }) });
        Say("machines loaded", it.Rows.Count.ToString());

        Type termsType = plug.GetType("ServiceContractPhotocopier.Classes.ScpLineTerms", true);
        Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), termsType);
        object terms = Activator.CreateInstance(dictType,
            new object[] { StringComparer.OrdinalIgnoreCase });

        // The two ticks under "Under each charge, print", read off the screen after Load. They are
        // handed in the way the contract editor hands them in, so what comes back is what a person
        // would see.
        bool dbModel = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(ShowModelOnLine,'Y') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)).Trim() != "N";
        bool dbSerial = Convert.ToString(db.ExecuteScalar(
            "SELECT ISNULL(ShowSerialOnLine,'Y') FROM dbo.zSCP2_Contract WHERE ContractKey = " + ck)).Trim() != "N";
        Say("contract says model / serial", (dbModel ? "Y" : "N") + " / " + (dbSerial ? "Y" : "N"));
        Ticks(plug, items, terms, db, dbModel, dbSerial);

        Terms(plug, items, terms, db);

        // Each of the four choices, and what the footer says about it.
        Check("one invoice for everything", "ONE", expected, plug, items, terms, db);
        Check("a rental invoice and a meter invoice", "RS", expected * 2, plug, items, terms, db);
        Check("one invoice per machine", "PM", it.Rows.Count, plug, items, terms, db);
        Check("per machine, rental apart", "PMS", it.Rows.Count * 2, plug, items, terms, db);

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    /// <summary>Opens the dialog the way the contract editor does and reads the two tick boxes
    /// back. A checkbox that shows the opposite of what is stored is a lie the user acts on.</summary>
    private static void Ticks(Assembly plug, object items, object terms, DBSetting db,
                              bool wantModel, bool wantSerial)
    {
        Type dlgType = plug.GetType("ServiceContractPhotocopier.RentalGroupPrice_Form", true);
        object dlg = Activator.CreateInstance(dlgType, new object[] { items, terms, "ONE" });
        try
        {
            FieldInfo dbF = dlgType.GetField("Db", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (dbF != null) dbF.SetValue(dlg, db);
            dlgType.GetField("ResultShowModel", BindingFlags.Instance | BindingFlags.Public).SetValue(dlg, wantModel);
            dlgType.GetField("ResultShowSerial", BindingFlags.Instance | BindingFlags.Public).SetValue(dlg, wantSerial);
            ((Control)dlg).CreateControl();
            MethodInfo onLoad = dlgType.GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (onLoad != null) onLoad.Invoke(dlg, new object[] { EventArgs.Empty });

            bool gotModel = Box(dlgType, dlg, "ChkShowModel");
            bool gotSerial = Box(dlgType, dlg, "ChkShowSerial");
            Console.WriteLine();
            Ok("the model box shows what is stored", gotModel == wantModel);
            Ok("the serial box shows what is stored", gotSerial == wantSerial);
            Console.WriteLine("        screen: model " + (gotModel ? "ticked" : "clear") +
                              ", serial " + (gotSerial ? "ticked" : "clear"));
        }
        finally { ((IDisposable)dlg).Dispose(); }
    }

    /// <summary>
    /// The Minimum / waive column, read off the built grid.
    ///
    /// <para>That sentence is the only place a deal shows without opening a dialog, and it used to
    /// be written from the usage target alone. A rental that is free for the first twelve months
    /// came out as "reach 0.00 -> waive 900.00" -- a condition nobody set, and one that zero copies
    /// meet every month. A waive with no condition at all, which the engine really does fire every
    /// month, read the same way.</para>
    ///
    /// <para>Each deal is put on the machines in memory and the dialog is built over them. Nothing
    /// is written to the book.</para>
    /// </summary>
    private static void Terms(Assembly plug, object items, object terms, DBSetting db)
    {
        Console.WriteLine();
        // A contract may already carry waives of its own -- PASIR GUDANG has three. A second one on
        // the same line makes it "own waive each", which is a different sentence and not the one
        // under test, so the contract's own are put aside for the duration and handed back after.
        List<DataRow> hidden = new List<DataRow>();
        foreach (object it in (System.Collections.IList)items)
        {
            DataTable mt = (DataTable)Get(it, "Meters");
            if (mt == null) continue;
            foreach (DataRow mr in mt.Rows)
            {
                if (mr.RowState == DataRowState.Deleted) continue;
                if (Convert.ToString(mr["MeterRole"]).Trim().ToUpperInvariant() != "WAIVE") continue;
                mr["MeterRole"] = "SET-ASIDE";
                hidden.Add(mr);
            }
        }
        try
        {
        Deal(plug, items, terms, db, "ONE", "free for the first 12 months", 12, 0m,
             "free for the first 12 months (900.00 a month)");
        Deal(plug, items, terms, db, "ONE", "reach 5,000 in copies", 0, 5000m,
             "reach 5,000.00 -> waive 900.00");
        Deal(plug, items, terms, db, "ONE", "both, in that order", 12, 5000m,
             "free for the first 12 months, then reach 5,000.00 -> waive 900.00");
        Deal(plug, items, terms, db, "ONE", "no condition at all", 0, 0m,
             "every month -> waive 900.00");

        // The rental on an invoice of its own. Only a waive that counts copies is in doubt there;
        // a free window reads a calendar and is not.
        Deal(plug, items, terms, db, "RS", "rental apart, free months - no warning", 12, 0m,
             "free for the first 12 months (900.00 a month)");
        Deal(plug, items, terms, db, "RS", "rental apart, a copy target - warned", 0, 5000m,
             "! rental is on its own invoice - reach 5,000.00 -> waive 900.00");
        }
        finally
        {
            foreach (DataRow mr in hidden) mr["MeterRole"] = "WAIVE";
        }
    }

    /// <summary>Puts one waive on the first machine, builds the dialog, and reads the sentence the
    /// rental line ends up carrying.</summary>
    private static void Deal(Assembly plug, object items, object terms, DBSetting db,
                             string split, string what, int firstN, decimal target, string want)
    {
        // The waive has to sit on a machine that HAS a rental -- it is the credit against one, and
        // the screen only offers a rental line to machines that carry a rental meter. A contract
        // with none (KENSINGTON has eighteen machines and no rental at all) has nothing to test.
        object first = null;
        DataTable meters = null;
        foreach (object it in (System.Collections.IList)items)
        {
            DataTable mt = (DataTable)Get(it, "Meters");
            if (mt == null) continue;
            foreach (DataRow mr in mt.Rows)
            {
                if (mr.RowState == DataRowState.Deleted) continue;
                if (Convert.ToString(mr["MeterRole"]).Trim().ToUpperInvariant() != "RENTAL") continue;
                first = it; meters = mt; break;
            }
            if (first != null) break;
        }
        if (meters == null)
        {
            Console.WriteLine("   --   " + what + "   (no machine here carries a rental)");
            return;
        }
        DataRow w = meters.NewRow();
        Set(w, "MeterTypeCode", "RA");
        Set(w, "MeterRole", "WAIVE");
        Set(w, "WaiveFirstNMonths", firstN);
        Set(w, "WaiveTargetAmount", target);
        Set(w, "MinimumCharges", 900m);          // the money that comes off
        Set(w, "CommitScope", "G");              // the whole line, not this one machine
        Set(w, "WaiveScope", "G");
        meters.Rows.Add(w);
        try
        {
            Type dlgType = plug.GetType("ServiceContractPhotocopier.RentalGroupPrice_Form", true);
            object dlg = Activator.CreateInstance(dlgType, new object[] { items, terms, split });
            try
            {
                FieldInfo dbF = dlgType.GetField("Db", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dbF != null) dbF.SetValue(dlg, db);
                ((Control)dlg).CreateControl();
                MethodInfo onLoad = dlgType.GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (onLoad != null) onLoad.Invoke(dlg, new object[] { EventArgs.Empty });

                FieldInfo f = dlgType.GetField("_dtLines", BindingFlags.Instance | BindingFlags.NonPublic);
                DataTable lines = f == null ? null : (DataTable)f.GetValue(dlg);
                string said = "";
                if (lines != null)
                    foreach (DataRow r in lines.Rows)
                    {
                        string t = Convert.ToString(r["Terms"]).Trim();
                        if (t.Length > 0 && said.Length == 0) said = t;
                    }
                Ok(what, said == want);
                Console.WriteLine("        says   " + (said.Length == 0 ? "<nothing>" : said));
                if (said != want) Console.WriteLine("        wanted " + want);
            }
            finally { ((IDisposable)dlg).Dispose(); }
        }
        finally { meters.Rows.Remove(w); }
    }

    private static object Get(object o, string name)
    {
        Type t = o.GetType();
        PropertyInfo p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null) return p.GetValue(o, null);
        FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f == null ? null : f.GetValue(o);
    }

    private static void Set(DataRow r, string col, object v)
    {
        if (r.Table.Columns.Contains(col)) r[col] = v;
    }

    private static void Ok(string what, bool ok)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what);
    }

    private static bool Box(Type t, object o, string name)
    {
        FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return false;
        object ed = f.GetValue(o);
        PropertyInfo p = ed.GetType().GetProperty("Checked");
        return p != null && Convert.ToBoolean(p.GetValue(ed, null));
    }

    private static void Check(string what, string split, int wantCount,
                              Assembly plug, object items, object terms, DBSetting db)
    {
        Type dlgType = plug.GetType("ServiceContractPhotocopier.RentalGroupPrice_Form", true);
        object dlg = Activator.CreateInstance(dlgType, new object[] { items, terms, split });
        try
        {
            PropertyInfo dbProp = dlgType.GetProperty("Db");
            if (dbProp != null && dbProp.CanWrite) dbProp.SetValue(dlg, db, null);
            else
            {
                FieldInfo dbField = dlgType.GetField("Db",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dbField != null) dbField.SetValue(dlg, db);
            }

            // CreateControl does not reliably raise Load, so OnLoad is invoked outright -- the same
            // trick smoke-forms uses, for the same reason.
            ((Control)dlg).CreateControl();
            MethodInfo onLoad = dlgType.GetMethod("OnLoad",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (onLoad != null) onLoad.Invoke(dlg, new object[] { EventArgs.Empty });

            FieldInfo lbl = dlgType.GetField("LblSummary",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            string text = lbl == null ? "" : Convert.ToString(((Control)lbl.GetValue(dlg)).Text);

            string head = text.Length == 0 ? "" : text.Split('·')[0].Trim();
            bool ok = head.StartsWith(wantCount.ToString() + " invoice", StringComparison.OrdinalIgnoreCase);
            if (!ok) _fail++;
            Console.WriteLine();
            Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what + "   (expected " + wantCount + ")");
            Console.WriteLine("        " + (text.Length == 0 ? "<label not found>" : text));
        }
        finally { ((IDisposable)dlg).Dispose(); }
    }

    private static void Say(string what, string value)
    {
        Console.WriteLine("   " + what.PadRight(30) + value);
    }
}
