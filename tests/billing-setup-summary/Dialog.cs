using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
/// Does the Minimum / waive dialog fit inside its own box?
///
/// <para>It has been cut off three times: a group measured against a control that was no longer the
/// last one, a group measured in the wrong coordinate system, and a form that never grew when the
/// group did. Each time the missing part was a real deal the user could no longer set -- the free
/// window, the consolation band, "count only" -- and each time it was found by a person opening the
/// screen, because nothing here was ever measured.</para>
///
/// <para>So: build the dialog the way the contract screen builds it, run its Load, and check that
/// every control that says it is visible actually sits inside the box, and every box inside the
/// form. No window is shown.</para>
/// </summary>
internal static class DialogFit
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
        try { return Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
    }

    private static int Run()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type t = plug.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.LineTerms_Form", false)
                 ?? plug.GetType("ServiceContractPhotocopier.LineTerms_Form", true);

        // The four shapes the contract screen opens it in.
        Check(t, "a rental line, 3 machines, free months set",   3, true,  true,  true,  12);
        Check(t, "a rental line, 3 machines, nothing set",       3, true,  true,  true,   0);
        Check(t, "a rental line of ONE machine",                 1, true,  true,  true,  12);
        Check(t, "a meter line - minimum only, no waive",        3, true,  false, false,  0);

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    private static void Check(Type t, string what, int machines,
                              bool allowMin, bool allowWaive, bool allowUsageWaive, int firstN)
    {
        Type mm = t.Assembly.GetType("ServiceContractPhotocopier.ServiceContract.OperationForms.LineTerms_Form+MachineMin", false)
                  ?? t.GetNestedType("MachineMin", BindingFlags.Public | BindingFlags.NonPublic);
        Type listType = typeof(List<>).MakeGenericType(mm);
        object onLine = Activator.CreateInstance(listType);
        MethodInfo add = listType.GetMethod("Add");
        for (int i = 0; i < machines; i++)
            add.Invoke(onLine, new object[] { Activator.CreateInstance(mm) });

        object dlg = Activator.CreateInstance(t, new object[]
        {
            "RA3", machines, 0m, 0m, firstN > 0 ? 900m : 0m,
            allowMin, allowWaive, "", false, onLine, false, firstN,
            0m, 0m, "BKCL", allowUsageWaive, 300m,
            "BKCL"   // minCount: the minimum counts black and colour (ATP-4 added the argument)
        });
        try
        {
            // SHOWN, off the side of the screen. Control.Visible answers "no" about everything on a
            // form that was never shown, whatever the code set -- so a check that reads it from an
            // unshown form passes on a dialog with its bottom third missing, which is exactly what
            // this file exists to catch.
            Form f = (Form)dlg;
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new System.Drawing.Point(-4000, -4000);
            f.ShowInTaskbar = false;
            f.Show();
            Application.DoEvents();

            Console.WriteLine();
            Console.WriteLine("   " + what);
            int bad = 0;
            foreach (Control g in f.Controls)
            {
                if (!(g is DevExpress.XtraEditors.GroupControl) || !g.Visible) continue;
                foreach (Control c in g.Controls)
                {
                    if (!c.Visible) continue;
                    if (c.Bottom <= g.ClientSize.Height && c.Right <= g.ClientSize.Width) continue;
                    bad++;
                    Console.WriteLine("        cut off: " + c.Name.PadRight(16) +
                                      " bottom " + c.Bottom + " / box " + g.ClientSize.Height);
                }
                if (g.Bottom > f.ClientSize.Height)
                {
                    bad++;
                    Console.WriteLine("        box past the window: " + g.Name +
                                      " bottom " + g.Bottom + " / form " + f.ClientSize.Height);
                }
            }
            // OK must not sit on top of the boxes either.
            Control ok = f.Controls["BtnOK"];
            foreach (Control g in f.Controls)
                if (g is DevExpress.XtraEditors.GroupControl && g.Visible && ok != null && ok.Top < g.Bottom)
                {
                    bad++;
                    Console.WriteLine("        OK overlaps " + g.Name + ": OK top " + ok.Top +
                                      " / box bottom " + g.Bottom);
                }
            foreach (Control g in f.Controls)
                if (g is DevExpress.XtraEditors.GroupControl && g.Visible)
                {
                    int low = 0; string last = "";
                    foreach (Control c in g.Controls)
                        if (c.Visible && c.Bottom > low) { low = c.Bottom; last = c.Name; }
                    Console.WriteLine("        " + g.Name.PadRight(9) + " top " + g.Top +
                                      "  height " + g.Height + "  room " + g.ClientSize.Height +
                                      "  lowest control " + last + " at " + low);
                }
            foreach (string nm in new string[] { "LblCount", "CmbCount", "LblWaiveD", "ChkPartial" })
            {
                Control[] hit = f.Controls.Find(nm, true);
                if (hit.Length > 0)
                    Console.WriteLine("          " + nm.PadRight(12) + " top " + hit[0].Top +
                                      " bottom " + hit[0].Bottom + " visible " + hit[0].Visible);
            }
            Console.WriteLine("        form " + f.ClientSize.Height + "   OK at " +
                              (f.Controls["BtnOK"] == null ? -1 : f.Controls["BtnOK"].Top));
            if (bad == 0) Console.WriteLine("   ok   everything fits");
            else _fail++;
        }
        finally { ((Form)dlg).Hide(); ((IDisposable)dlg).Dispose(); }
    }
}
