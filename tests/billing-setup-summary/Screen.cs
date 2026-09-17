using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AutoCount.Data;

/// <summary>
/// Does the Meter Reading screen's filter box hold what is in it?
///
/// <para>The day strip grew a second and third row the day rentals got their own billing day, and
/// the box did not grow with it: the Month line ended up underneath the days and Filter and Reset
/// were pushed out of the box altogether. Nothing in a build catches that -- the form compiles
/// perfectly with its controls stacked on top of each other.</para>
///
/// <para>So the screen is built the way AutoCount builds it, shown off the side of the display, and
/// measured: every control inside its box, every box inside the panel, and no two controls sitting
/// on the same pixels. Shown, because Control.Visible answers "no" about everything on a form that
/// was never displayed -- and a check that reads it from an unshown form passes on a screen with
/// its bottom row missing.</para>
/// </summary>
internal static class ScreenFit
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

    private static int Run(string[] args)
    {
        string book = args.Length > 0 ? args[0] : "AED_ATPTEST";
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        DBSetting db = new DBSetting(DBServerType.SQL2000, "localhost,1433", "sa", "rs6663", book, false);

        Assembly plug = Assembly.LoadFrom(PLUGIN);
        Type t = plug.GetType("ServiceContractPhotocopier.MeterReading.OperationForms.MeterReadingIntegration_Form", true);
        object f = Activator.CreateInstance(t, new object[] { db });

        Console.WriteLine();
        Console.WriteLine("   book   " + book);
        try
        {
            Form w = (Form)f;
            w.StartPosition = FormStartPosition.Manual;
            w.Location = new Point(-4000, -4000);
            w.ShowInTaskbar = false;
            w.Show();
            Application.DoEvents();

            Control panel = Find(w, "PanelFilter");
            Control box = Find(w, "GrpFilter");
            Control days = Find(w, "_pnlDays");
            if (panel == null || box == null) { Console.WriteLine("   filter panel not found"); return 2; }

            int dayButtons = 0;
            if (days != null)
                foreach (Control c in days.Controls)
                    if (c is DevExpress.XtraEditors.CheckButton) dayButtons++;

            Console.WriteLine("   day buttons                " + dayButtons +
                              (days == null ? "" : "   strip " + days.Width + " x " + days.Height));
            Console.WriteLine("   filter box                 " + box.Width + " x " + box.Height +
                              "   panel " + panel.Width + " x " + panel.Height);
            Console.WriteLine();

            // 1. every control inside the box it lives in
            Inside(box, "the filter box");
            if (days != null) Inside(days, "the day strip");
            Ok("the filter box fits the panel",
               box.Right <= panel.Width && box.Bottom <= panel.Height,
               "box bottom " + box.Bottom + " / panel " + panel.Height);

            // 2. nothing sitting on top of anything else
            NoOverlap(box, "the filter box");
            NoOverlap(panel, "the panel");
        }
        finally { ((Form)f).Hide(); ((IDisposable)f).Dispose(); }

        Console.WriteLine();
        Console.WriteLine(_fail == 0
            ? "== all good ===================================================="
            : "== " + _fail + " FAILED ==========================================");
        Console.WriteLine();
        return _fail == 0 ? 0 : 1;
    }

    private static void Inside(Control parent, string what)
    {
        int bad = 0;
        foreach (Control c in parent.Controls)
        {
            if (!c.Visible) continue;
            if (c.Bottom <= parent.ClientSize.Height && c.Right <= parent.ClientSize.Width) continue;
            bad++;
            Console.WriteLine("        outside: " + Name(c) + "  at " + c.Top + ".." + c.Bottom +
                              " / " + parent.ClientSize.Height + " tall");
        }
        Ok("everything sits inside " + what, bad == 0, "");
    }

    private static void NoOverlap(Control parent, string what)
    {
        List<Control> vis = new List<Control>();
        foreach (Control c in parent.Controls)
            if (c.Visible && c.Width > 0 && c.Height > 0) vis.Add(c);
        int bad = 0;
        for (int i = 0; i < vis.Count; i++)
            for (int j = i + 1; j < vis.Count; j++)
            {
                Rectangle a = vis[i].Bounds, b = vis[j].Bounds;
                a.Inflate(-1, -1);            // touching edges is not overlapping
                if (!a.IntersectsWith(b)) continue;
                bad++;
                if (bad <= 6)
                    Console.WriteLine("        overlap: " + Name(vis[i]) + " " + vis[i].Bounds +
                                      "   and   " + Name(vis[j]) + " " + vis[j].Bounds);
            }
        Ok("nothing overlaps in " + what, bad == 0, bad + " pair(s)");
    }

    private static string Name(Control c)
    {
        string n = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name;
        string txt = (c.Text ?? "").Trim();
        if (txt.Length > 22) txt = txt.Substring(0, 22);
        return txt.Length > 0 ? n + " (\"" + txt + "\")" : n;
    }

    private static Control Find(Control root, string name)
    {
        foreach (Control c in root.Controls)
        {
            if (c.Name == name) return c;
            Control hit = Find(c, name);
            if (hit != null) return hit;
        }
        // private fields are not named controls unless the code set Name; look there too
        FieldInfo fi = root.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return fi == null ? null : fi.GetValue(root) as Control;
    }

    private static void Ok(string what, bool ok, string detail)
    {
        if (!ok) _fail++;
        Console.WriteLine("   " + (ok ? "ok  " : "FAIL") + " " + what +
                          (ok || detail.Length == 0 ? "" : "   (" + detail + ")"));
    }
}
