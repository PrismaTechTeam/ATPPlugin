using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DevExpress.XtraReports.UI;

namespace ServiceContractPhotocopier.Classes
{
    /// <summary>
    /// Repoints a report's script references at the assemblies that are actually loaded.
    ///
    /// Every AutoCount report carries a small script (it assigns __report in Report_BeforePrint), and
    /// AutoCount builds that script's assembly references with PathHelper.GetStartupFile(...) — i.e.
    /// the EXE's own folder. When the runtime is hosted from anywhere other than the Accounting
    /// install folder, those paths name files that are not there, the script will not compile, and
    /// the preview shows only "There are errors in scripts" — no page, no reason.
    ///
    /// Pointing each reference at the loaded assembly's real location fixes that, and is a no-op
    /// under a normal install where the two are the same file. It also removes a subtler trap: a
    /// stale COPY of AutoCount.UI.dll sitting next to the host would compile against different types
    /// than the ones in memory.
    /// </summary>
    public static class ScpReportScripts
    {
        public static void FixReferences(XtraReport report)
        {
            if (report == null) return;
            try
            {
                string[] refs = report.ScriptReferences;
                if (refs == null || refs.Length == 0) return;

                List<string> fixedRefs = new List<string>();
                for (int i = 0; i < refs.Length; i++)
                {
                    string r = refs[i];
                    if (string.IsNullOrEmpty(r)) continue;

                    string resolved = Locate(r);
                    // A reference that cannot be resolved is dropped rather than passed through: a
                    // path to a file that does not exist fails the whole compile, taking the report
                    // with it. Without it the script may still compile; with it, never.
                    if (resolved.Length == 0) continue;
                    if (!fixedRefs.Contains(resolved)) fixedRefs.Add(resolved);
                }
                report.ScriptReferences = fixedRefs.ToArray();
            }
            catch { }   // never let reference tidying be the thing that stops a report opening
        }

        /// <summary>The loaded assembly's location if we have it, else the original path when the
        /// file really is there, else nothing.</summary>
        private static string Locate(string reference)
        {
            string name;
            try { name = Path.GetFileNameWithoutExtension(reference); }
            catch { return ""; }
            if (string.IsNullOrEmpty(name)) return "";

            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < loaded.Length; i++)
            {
                Assembly a = loaded[i];
                if (string.Compare(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase) != 0) continue;
                string loc;
                try { loc = a.IsDynamic ? "" : a.Location; }
                catch { loc = ""; }
                if (loc.Length > 0 && File.Exists(loc)) return loc;
                break;
            }

            try { if (File.Exists(reference)) return reference; }
            catch { }

            // Last resort: the folder the AutoCount runtime itself was loaded from. Under a hosted
            // runtime this is the install folder, which is where the rest of the DLLs live.
            try
            {
                string dir = Path.GetDirectoryName(typeof(AutoCount.Data.DBSetting).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    string candidate = Path.Combine(dir, name + ".dll");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch { }
            return "";
        }
    }
}
