using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AutoCount.Data;

// The contract list's audit columns, and the machine list's hidden date columns — both grids shown
// off-screen and read, because a column's Visible answers nothing on a form that never opened.
internal static class Cols
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
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine(_fail == 0 ? "   ALL OK" : "   " + _fail + " FAILED");
        return _fail == 0 ? 0 : 1;
    }

    private static void Expect(string what, bool ok)
    {
        Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
        if (!ok) _fail++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);
        Assembly plug = Assembly.LoadFrom(PLUGIN);

        Type tl = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_ContractLst_Form", true);
        Form list = (Form)Activator.CreateInstance(tl, new object[] { db });
        Show(list);
        try
        {
            object view = Field(list, "GridView");
            Expect("the list never scrolls sideways (ColumnAutoWidth)", AutoWidth(view));
            foreach (string[] c in new string[][] {
                new string[] { "CreatedBy", "Created By" }, new string[] { "CreatedDate", "Created" },
                new string[] { "ModifiedBy", "Modified By" }, new string[] { "LastModifiedDate", "Last Modified" } })
            {
                object col = Column(view, c[0]);
                Expect("the list shows " + c[1], col != null && (bool)Prop(col, "Visible"));
            }
        }
        finally { list.Close(); }

        Type tc = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_Contract_Form", true);
        long ck = Convert.ToInt64(db.ExecuteScalar("SELECT ContractKey FROM dbo.zSCP2_Contract WHERE ContractNo = N'SC 000000032'"));
        Form contract = (Form)Activator.CreateInstance(tc, new object[] { db, ck });
        Show(contract);
        try
        {
            object view = Field(contract, "GridViewItems");
            Expect("the machine list never scrolls sideways (ColumnAutoWidth)", AutoWidth(view));
            foreach (string[] c in new string[][] {
                new string[] { "BillingDay", "Billing Day" }, new string[] { "ServiceStart", "Service Start" },
                new string[] { "Expiry", "Expiry" } })
            {
                object col = Column(view, c[0]);
                bool hidden = col != null && !(bool)Prop(col, "Visible");
                bool inChooser = col != null && (bool)Prop(Prop(col, "OptionsColumn"), "ShowInCustomizationForm");
                Expect(c[1] + " starts hidden, still in the column chooser", hidden && inChooser);
            }
            // A rental row keeps no reading, no rebate and no free quantity of its own; a counter keeps all three.
            object cfg = Field(contract, "GridViewMeterCfg");
            System.Reflection.MethodInfo locked = contract.GetType().GetMethod("MeterCfgRentalLocked", BindingFlags.Instance | BindingFlags.NonPublic);
            int rows = (int)Prop(cfg, "RowCount");
            int rentalRow = -1, meterRow = -1;
            for (int h = 0; h < rows; h++)
            {
                string role = Convert.ToString(CellValue(cfg, h, "MeterRole")).Trim().ToUpperInvariant();
                if (role == "RENTAL" && rentalRow < 0) rentalRow = h;
                if (role == "BK" && meterRow < 0) meterRow = h;
            }
            Expect("the machine's counters are listed (rental row " + rentalRow + ", BK row " + meterRow + ")", rentalRow >= 0 && meterRow >= 0);
            foreach (string field in new string[] { "RebateQtyInPercent", "FOCQty", "InitialReading" })
            {
                bool shutOnRental = (bool)locked.Invoke(contract, new object[] { rentalRow, field });
                bool openOnMeter = !(bool)locked.Invoke(contract, new object[] { meterRow, field });
                Expect(field + ": shut on the rental, open on the black counter", shutOnRental && openOnMeter);
            }

            object serial = Column(view, "SerialNumber");
            if (serial == null) serial = Column(view, "MachineSerial");
            Expect("the columns people key in are still there (Machine Serial)", serial != null && (bool)Prop(serial, "Visible"));
        }
        finally { contract.Close(); }
    }

    private static void Show(Form f)
    {
        f.StartPosition = FormStartPosition.Manual;
        f.Location = new Point(-4000, -4000);
        f.ShowInTaskbar = false;
        f.Show();
        for (int i = 0; i < 60; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(50); }
    }

    private static object Field(object o, string name)
    {
        return o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    }

    private static object Prop(object o, string name)
    {
        System.Reflection.PropertyInfo pi = null;
        for (Type ty = o.GetType(); ty != null && pi == null; ty = ty.BaseType)
            pi = ty.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        return pi.GetValue(o, null);
    }

    private static bool AutoWidth(object view)
    {
        object options = Prop(view, "OptionsView");
        return (bool)Prop(options, "ColumnAutoWidth");
    }

    private static object CellValue(object view, int rowHandle, string field)
    {
        return view.GetType().GetMethod("GetRowCellValue", new Type[] { typeof(int), typeof(string) })
                   .Invoke(view, new object[] { rowHandle, field });
    }

    private static object Column(object view, string field)
    {
        object cols = Prop(view, "Columns");
        return cols.GetType().GetMethod("ColumnByFieldName", new Type[] { typeof(string) }).Invoke(cols, new object[] { field });
    }
}
