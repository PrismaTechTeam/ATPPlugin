using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

// Meters & Pricing, "What the invoice prints": a bill group is its own invoice, so the lines are shown
// under each bill group's invoice with that group's machines.
//   run.ps1   -> builds and runs; prints ok / FAIL per check
internal static class PricingBillGroupCheck
{
    private const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    private const string PLUGIN = @"C:\Dev\Plugin\ATP\ServiceContractPhotocopier\bin\Debug\ServiceContractPhotocopier.dll";
    private static int _fail;

    [STAThread]
    private static int Main()
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
        if (Environment.GetCommandLineArgs().Length > 1) { try { Shot(Environment.GetCommandLineArgs()[1], Environment.GetCommandLineArgs().Length > 2 ? Environment.GetCommandLineArgs()[2] : "ONE"); } catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; } return 0; }
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine();
        Console.WriteLine(_fail == 0 ? "   ALL OK" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    private static void Expect(string what, bool ok)
    {
        Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
        if (!ok) _fail++;
    }

    private sealed class Kit
    {
        public Assembly Plug;
        public Type Item, ItemForm, Terms, Form;
        public DBSetting Db;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        Kit k = new Kit();
        k.Plug = Assembly.LoadFrom(PLUGIN);
        k.Item = k.Plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.ItemEditData", true);
        k.ItemForm = k.Plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Item_Form", true);
        k.Terms = k.Plug.GetType("ServiceContractPhotocopier.Classes.ScpLineTerms", true);
        k.Form = k.Plug.GetType("ServiceContractPhotocopier.RentalGroupPrice_Form", true);
        k.Db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        // The screenshot: three new machines, no item numbers, nothing merged, two bill groups.
        Form f;
        DataTable one = Lines(k, "ONE", new string[][] {
            new string[] { "000145", "HQOFFICE", "", "" }, new string[] { "448571", "HQOFFICE", "", "" }, new string[] { "885701", "IOI", "", "" } }, out f);
        f.Close();
        Expect("no line claims to print on two invoices", Count(one, "Invoice LIKE '*separate invoices*'") == 0);
        Expect("two invoices: Invoice HQOFFICE and Invoice IOI (" + Distinct(one, "Invoice") + ")", Distinct(one, "Invoice") == "Invoice HQOFFICE, Invoice IOI");
        Expect("machines not merged are a line each: 3 rental, 3 black, 3 colour, one machine per row (" + one.Rows.Count + " rows)",
               Count(one, "Field = 'OwnRate'") == 3 && Count(one, "Field = 'OwnBk'") == 3 && Count(one, "Field = 'OwnCl'") == 3 && Count(one, "Units <> 1") == 0);
        Expect("HQOFFICE carries two machines' lines, IOI one", Count(one, "Invoice = 'Invoice HQOFFICE'") == 6 && Count(one, "Invoice = 'Invoice IOI'") == 3);
        // Black and colour of one line share its key (the machines under it are listed once); rental is its own.
        Expect("six line keys: a rental line and a copies line per machine (" + Distinct(one, "LineKey").Split(',').Length + ")",
               Distinct(one, "LineKey").Split(',').Length == 6);

        // All three merged: the merged line prints on both invoices, each with its own machines.
        DataTable merged = Lines(k, "ONE", new string[][] {
            new string[] { "000145", "HQOFFICE", "RA", "MB" }, new string[] { "448571", "HQOFFICE", "RA", "MB" }, new string[] { "885701", "IOI", "RA", "MB" } }, out f);
        Expect("merged rental: one row under HQOFFICE with 2 machines, one under IOI with 1",
               Count(merged, "Field = 'OwnRate' AND Invoice = 'Invoice HQOFFICE' AND Units = 2") == 1 &&
               Count(merged, "Field = 'OwnRate' AND Invoice = 'Invoice IOI' AND Units = 1") == 1);
        Expect("merged copies: black and colour rows under each invoice", Count(merged, "Field = 'OwnBk'") == 2 && Count(merged, "Field = 'OwnCl'") == 2);
        // A price typed on one of the merged rows is the line's price: the other invoice's row takes it.
        DataRow hq = merged.Select("Field = 'OwnRate' AND Invoice = 'Invoice HQOFFICE'")[0];
        hq["UnitPrice"] = 120m;
        k.Form.GetMethod("SyncLineRows", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { hq, "UnitPrice" });
        DataRow ioi = merged.Select("Field = 'OwnRate' AND Invoice = 'Invoice IOI'")[0];
        Expect("a price set on the HQOFFICE row is on the IOI row too (" + ioi["UnitPrice"] + ")", Convert.ToDecimal(ioi["UnitPrice"]) == 120m);
        f.Close();

        // Rental apart inside bill groups: four invoices.
        DataTable rs = Lines(k, "RS", new string[][] {
            new string[] { "000145", "HQOFFICE", "", "" }, new string[] { "885701", "IOI", "", "" } }, out f);
        f.Close();
        Expect("rental apart: HQOFFICE and IOI each a rental and a meter invoice (" + Distinct(rs, "Invoice") + ")",
               Distinct(rs, "Invoice") == "Invoice HQOFFICE - meters, Invoice HQOFFICE - rental, Invoice IOI - meters, Invoice IOI - rental");

        // One invoice per machine outranks the bill group: three machines, three invoices.
        DataTable pm = Lines(k, "PM", new string[][] {
            new string[] { "000145", "HQOFFICE", "", "" }, new string[] { "448571", "HQOFFICE", "", "" }, new string[] { "885701", "IOI", "", "" } }, out f);
        f.Close();
        Expect("per machine: an invoice each, not per bill group (" + Distinct(pm, "Invoice") + ")", Distinct(pm, "Invoice") == "000145, 448571, 885701");
        DataTable pms = Lines(k, "PMS", new string[][] {
            new string[] { "000145", "HQOFFICE", "", "" }, new string[] { "885701", "IOI", "", "" } }, out f);
        f.Close();
        Expect("per machine, rental apart: two invoices per machine (" + Distinct(pms, "Invoice") + ")",
               Distinct(pms, "Invoice") == "000145 - meters, 000145 - rental, 885701 - meters, 885701 - rental");

        // Hide options: the four groups fold away and the invoice list takes their room; Show puts it back.
        DataTable fold = Lines(k, "ONE", new string[][] { new string[] { "000145", "HQOFFICE", "", "" } }, out f);
        Control grid = f.Controls["GridLines"];
        Control ticked = f.Controls["GrpTicked"];
        Control toggle = f.Controls["BtnToggleOptions"];
        int top0 = grid.Top, h0 = grid.Height;
        MethodInfo click = k.Form.GetMethod("BtnToggleOptions_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        bool wasHidden = !ticked.Visible;
        if (wasHidden) { click.Invoke(f, new object[] { null, EventArgs.Empty }); top0 = grid.Top; h0 = grid.Height; }
        click.Invoke(f, new object[] { null, EventArgs.Empty });
        Expect("Hide options: groups gone, the invoice list starts where they were and is taller (" + grid.Top + ", " + grid.Height + ")",
               !ticked.Visible && grid.Top == ticked.Top && grid.Bottom == top0 + h0 && toggle.Text.Contains("Show"));
        Control sample = f.Controls["BtnSample"];
        object sampleIcon = sample.GetType().GetProperty("ImageOptions").GetValue(sample, null);
        Control mergeBtn = f.Controls["GrpTicked"].Controls["BtnMerge"];
        object mergeIcon = mergeBtn.GetType().GetProperty("ImageOptions").GetValue(mergeBtn, null);
        Expect("the buttons carry icons (View Sample Invoice, Merge rental)",
               sampleIcon.GetType().GetProperty("SvgImage").GetValue(sampleIcon, null) != null &&
               mergeIcon.GetType().GetProperty("SvgImage").GetValue(mergeIcon, null) != null);
        Expect("Hide options hides View Sample Invoice too; the toggle moves to the left (" + toggle.Left + ")", !sample.Visible && toggle.Left == sample.Left);
        click.Invoke(f, new object[] { null, EventArgs.Empty });
        Expect("Show options: back as it was (" + grid.Top + ", " + grid.Height + ")", ticked.Visible && grid.Top == top0 && grid.Height == h0 && toggle.Text.Contains("Hide")
               && sample.Visible && toggle.Left > sample.Right);
        if (wasHidden) click.Invoke(f, new object[] { null, EventArgs.Empty });   // the user's own choice back
        f.Close();
        if (fold == null) Expect("form built", false);

        // No bill group: one invoice, as before.
        DataTable none = Lines(k, "ONE", new string[][] {
            new string[] { "000145", "", "", "" }, new string[] { "885701", "", "", "" } }, out f);
        f.Close();
        Expect("no bill group: The invoice (" + Distinct(none, "Invoice") + ")", Distinct(none, "Invoice") == "The invoice");
    }

    /// <summary>The screenshot's three machines, on screen, saved to a PNG -- for a look, not a check.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Shot(string png, string split)
    {
        Kit k = new Kit();
        k.Plug = Assembly.LoadFrom(PLUGIN);
        k.Item = k.Plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.ItemEditData", true);
        k.ItemForm = k.Plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Item_Form", true);
        k.Terms = k.Plug.GetType("ServiceContractPhotocopier.Classes.ScpLineTerms", true);
        k.Form = k.Plug.GetType("ServiceContractPhotocopier.RentalGroupPrice_Form", true);
        k.Db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);
        Form f;
        Lines(k, split, new string[][] {
            new string[] { "000145", "HQOFFICE", "", "" }, new string[] { "448571", "HQOFFICE", "", "" }, new string[] { "885701", "IOI", "", "" } }, out f);
        f.Location = new Point(20, 20);
        MethodInfo toggle = k.Form.GetMethod("BtnToggleOptions_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        bool fold = Environment.GetCommandLineArgs().Length > 3 && Environment.GetCommandLineArgs()[3] == "hide";
        if (fold) toggle.Invoke(f, new object[] { null, EventArgs.Empty });
        f.TopMost = true;
        f.Activate();
        DateTime end = DateTime.Now.AddMilliseconds(2500);
        while (DateTime.Now < end) { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
        using (Bitmap b = new Bitmap(f.Width, f.Height))
        using (Graphics g = Graphics.FromImage(b))
        {
            g.CopyFromScreen(f.Location, Point.Empty, f.Size);
            b.Save(png, System.Drawing.Imaging.ImageFormat.Png);
        }
        if (fold) toggle.Invoke(f, new object[] { null, EventArgs.Empty });
        f.Close();
    }

    private static DataTable Lines(Kit k, string split, string[][] machines, out Form form)
    {
        IList items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(k.Item));
        foreach (string[] m in machines)
        {
            object d = Activator.CreateInstance(k.Item);
            k.Item.GetField("SerialNumber").SetValue(d, m[0]);
            k.Item.GetField("ServiceItemNo").SetValue(d, "");
            k.Item.GetField("ItemCode").SetValue(d, "IR-ADV");
            k.Item.GetField("BillGroupCode").SetValue(d, m[1]);
            k.Item.GetField("MergeGroupCode").SetValue(d, m[2]);
            k.Item.GetField("MergeGroupCodeMeter").SetValue(d, m[3]);
            DataTable meters = (DataTable)k.ItemForm.GetMethod("CreateMetersTable", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            foreach (string role in new string[] { "RENTAL", "BK", "CL" })
            {
                DataRow r = meters.NewRow();
                r["MeterTypeCode"] = role; r["MeterRole"] = role; r["MinimumCharges"] = 0m; r["ChargesRate"] = 0m;
                r["MeterMultiPriceCode"] = ""; r["CustomTiers"] = "";
                meters.Rows.Add(r);
            }
            k.Item.GetField("Meters").SetValue(d, meters);
            items.Add(d);
        }
        object terms = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), k.Terms), new object[] { StringComparer.OrdinalIgnoreCase });
        form = (Form)Activator.CreateInstance(k.Form, new object[] { items, terms, split });
        k.Form.GetField("Db").SetValue(form, k.Db);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-4000, -4000);
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();
        return (DataTable)k.Form.GetField("_dtLines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
    }

    private static int Count(DataTable t, string filter) { return t.Select(filter).Length; }

    private static string Distinct(DataTable t, string col)
    {
        List<string> v = new List<string>();
        foreach (DataRow r in t.Rows) { string s = Convert.ToString(r[col]); if (!v.Contains(s)) v.Add(s); }
        v.Sort(StringComparer.Ordinal);
        return string.Join(", ", v.ToArray());
    }
}
