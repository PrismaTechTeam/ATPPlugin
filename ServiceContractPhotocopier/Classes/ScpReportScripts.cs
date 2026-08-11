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
        /// <summary>
        /// Make a saved design renderable: resolvable script references, and no duplicate handler
        /// that would stop the script compiling. Call this before CreateDocument on anything loaded
        /// out of dbo.Report.
        /// </summary>
        public static void Prepare(XtraReport report)
        {
            RemoveDuplicateMethods(report);
            FixReferences(report);
        }

        /// <summary>
        /// Drop a second definition of a method the script already has.
        ///
        /// Every AutoCount report carries a Report_BeforePrint; paste a replacement BELOW the
        /// original instead of over it and the design now declares it twice. That is CS0111, the
        /// script does not compile, and the report renders as "There are errors in scripts" — no
        /// page, and nothing that names the design or the cause.
        ///
        /// The first definition wins, which is the one AutoCount put there. Done in memory only:
        /// the saved design is not rewritten behind the operator's back.
        /// </summary>
        public static void RemoveDuplicateMethods(XtraReport report)
        {
            if (report == null) return;
            try
            {
                string src = report.ScriptsSource;
                if (string.IsNullOrEmpty(src) || src.IndexOf(" void ", StringComparison.Ordinal) < 0) return;

                List<string> seen = new List<string>();
                string result = src;
                int searchFrom = 0;
                while (true)
                {
                    int declStart, bodyEnd;
                    string name = NextMethod(result, searchFrom, out declStart, out bodyEnd);
                    if (name == null) break;
                    if (seen.Contains(name))
                    {
                        result = result.Remove(declStart, bodyEnd - declStart);
                        searchFrom = declStart;   // same position now holds whatever followed
                    }
                    else
                    {
                        seen.Add(name);
                        searchFrom = bodyEnd;
                    }
                }
                if (result != src) report.ScriptsSource = result;
            }
            catch { }   // a script we cannot parse is left exactly as the operator wrote it
        }

        /// <summary>
        /// The next `void Name(...) { ... }` at or after <paramref name="from"/>, with the span it
        /// occupies. Deliberately narrow: only methods returning void are considered, which is every
        /// report event handler and nothing that could be confused with a call or a control flow
        /// statement.
        /// </summary>
        private static string NextMethod(string src, int from, out int declStart, out int bodyEnd)
        {
            declStart = -1; bodyEnd = -1;
            int i = src.IndexOf(" void ", from, StringComparison.Ordinal);
            if (i < 0) return null;

            int nameStart = i + " void ".Length;
            int p = nameStart;
            while (p < src.Length && (char.IsLetterOrDigit(src[p]) || src[p] == '_')) p++;
            if (p == nameStart) { declStart = -1; return NextMethod(src, i + 5, out declStart, out bodyEnd); }
            string name = src.Substring(nameStart, p - nameStart);

            int paren = src.IndexOf('(', p);
            if (paren < 0) return null;
            int close = src.IndexOf(')', paren);
            if (close < 0) return null;
            int brace = src.IndexOf('{', close);
            if (brace < 0) return null;
            // Anything other than whitespace between ) and { means this was not a declaration.
            for (int k = close + 1; k < brace; k++)
                if (!char.IsWhiteSpace(src[k])) return NextMethod(src, brace, out declStart, out bodyEnd);

            int depth = 0, q = brace;
            while (q < src.Length)
            {
                if (src[q] == '{') depth++;
                else if (src[q] == '}') { depth--; if (depth == 0) break; }
                q++;
            }
            if (depth != 0) return null;

            // Take the whole line the declaration starts on, so removing it leaves no stray modifiers.
            int lineStart = src.LastIndexOf('\n', i) + 1;
            declStart = lineStart;
            bodyEnd = Math.Min(src.Length, q + 1);
            while (bodyEnd < src.Length && (src[bodyEnd] == '\r' || src[bodyEnd] == '\n')) bodyEnd++;
            return name;
        }

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

        /// <summary>
        /// Turn a report-rendering failure into something the operator can act on. A script that will
        /// not compile surfaces as a wall of C# compiler output naming a class nobody has heard of;
        /// what the reader needs is WHICH design is broken and WHERE to fix it.
        /// </summary>
        public static Exception Explain(Exception ex, string layoutName)
        {
            string raw = ex == null ? "" : (ex.Message ?? "");
            string design = string.IsNullOrEmpty(layoutName) ? "the report design" : "report design '" + layoutName + "'";

            if (raw.IndexOf("CS0111", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("already defines a member", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Exception(
                    "The script in " + design + " declares Report_BeforePrint twice, so it will not " +
                    "compile and no page can be produced.\r\n\r\n" +
                    "Open that design (Report Design > Design > Scripts tab) and delete the duplicate — " +
                    "keep ONE copy of the method.\r\n\r\n" + raw, ex);

            if (raw.IndexOf("errors in script", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Exception(
                    "The script in " + design + " will not compile, so no page can be produced.\r\n\r\n" +
                    "Open that design (Report Design > Design > Scripts tab) and fix the error below, " +
                    "or clear the script entirely if the design does not need one.\r\n\r\n" + raw, ex);

            return new Exception("Could not render " + design + ": " + raw, ex);
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
