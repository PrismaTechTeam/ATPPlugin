// ATPUiTest — pattern-only UI test harness for the ATP plugin (AutoCount via ATPShadowMain).
//
// DESIGN RULES (the whole point of this harness):
//   * NEVER touches the mouse  (no FlaUI Mouse class, no click coordinates).
//   * NEVER sends keyboard input (no FlaUI Keyboard class, no SendKeys).
//   * NEVER requires the AutoCount window to be focused / foreground.
//   All interaction is via UI Automation PATTERNS, which are delivered cross-process
//   by COM straight to the control regardless of focus:
//     - InvokePattern / LegacyIAccessible.DoDefaultAction  -> press buttons & menu items
//     - ValuePattern.SetValue                              -> type into edits
//     - ExpandCollapsePattern                              -> open menus / combos
//     - SelectionItemPattern                               -> pick tabs / list rows
//     - WindowPattern.Close                                -> close forms (the reliable
//       replacement for Alt+F4 / F2, which need focus and used to get stuck)
//
// Usage:
//   ATPUiTest.exe             -> run all scenarios
//   ATPUiTest.exe S1 S3       -> run only the named scenarios
//   ATPUiTest.exe --list      -> list scenarios
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using Application = FlaUI.Core.Application;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

/// <summary>Raw Win32 window discovery + close. UIA sweeps (GetAllTopLevelWindows /
/// FindFirstDescendant across windows) BLOCK when any window's UI thread sits in a modal or
/// menu loop — EnumWindows / GetWindowText / PostMessage(WM_CLOSE) never do. So: discover and
/// close windows via Win32, and only enter UIA for elements INSIDE a known-good hwnd.</summary>
internal static class Win32
{
    private delegate bool EnumProc(IntPtr h, IntPtr lp);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc cb, IntPtr lp);
    [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    private const uint WM_CLOSE = 0x0010;

    public record Win(IntPtr Hwnd, string Title, bool Enabled);

    public static List<Win> List(int pid)
    {
        List<Win> r = new();
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint p);
            if (p == pid && IsWindowVisible(h))
            {
                StringBuilder sb = new(512);
                GetWindowText(h, sb, 512);
                r.Add(new Win(h, sb.ToString(), IsWindowEnabled(h)));
            }
            return true;
        }, IntPtr.Zero);
        return r;
    }

    public static Win Find(int pid, string titlePart, bool exact) =>
        List(pid).FirstOrDefault(w => exact
            ? string.Equals(w.Title.Trim(), titlePart, StringComparison.OrdinalIgnoreCase)
            : w.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase));

    public static void Close(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero && IsWindow(hwnd)) PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    public static bool Alive(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindow(hwnd) && IsWindowVisible(hwnd);
}

internal static class Program
{
    private static Application _app;
    private static UIA3Automation _ui;
    private static int _pid;
    private static readonly List<(string Id, string Name, Func<string> Run)> Scenarios = new();
    private static readonly List<(string Id, bool Pass, string Note)> Results = new();

    private static int Main(string[] args)
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        ThreadPool.SetMinThreads(128, 128);   // abandoned (modal-blocked) UIA calls must not starve the pool
        RegisterScenarios();
        if (args.Contains("--list"))
        {
            foreach ((string id, string name, _) in Scenarios) Console.WriteLine($"{id}  {name}");
            return 0;
        }

        Process p = Process.GetProcessesByName("ATPShadowMain").FirstOrDefault();
        if (p == null)
        {
            Console.WriteLine("[FATAL] ATPShadowMain.exe is not running. Start it first (full AutoCount).");
            return 2;
        }
        _pid = p.Id;
        _app = Application.Attach(p.Id);
        using UIA3Automation ui = new UIA3Automation();
        _ui = ui;
        // Fail fast instead of the 60s+ COM default when a provider thread is modal-blocked.
        try { ui.ConnectionTimeout = TimeSpan.FromSeconds(5); } catch { }
        try { ui.TransactionTimeout = TimeSpan.FromSeconds(5); } catch { }
        Console.WriteLine($"[attach] ATPShadowMain pid={p.Id}");

        Cleanup();   // Win32-based: dismiss/close anything a previous (failed) run left open

        string[] wanted = args.Where(a => !a.StartsWith("--")).ToArray();
        foreach ((string id, string name, Func<string> run) in Scenarios)
        {
            if (wanted.Length > 0 && !wanted.Contains(id, StringComparer.OrdinalIgnoreCase)) continue;
            Console.WriteLine($"\n=== {id}  {name} ===");
            try
            {
                // Per-scenario watchdog: a wedged scenario FAILS, it never hangs the runner.
                Task<string> t = Task.Run(run);
                t.ContinueWith(x => { _ = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                if (!t.Wait(TimeSpan.FromSeconds(120)))
                    throw new Exception("scenario watchdog: exceeded 120s");
                string note = t.Result;
                Results.Add((id, true, note));
                Console.WriteLine($"[PASS] {id}  {note}");
            }
            catch (Exception ex)
            {
                Exception real = ex is AggregateException ag ? ag.InnerException ?? ex : ex;
                Results.Add((id, false, real.Message));
                Console.WriteLine($"[FAIL] {id}  {real.Message}");
                Cleanup();   // reset app state so the NEXT scenario starts clean
            }
        }

        Console.WriteLine("\n================ SUMMARY ================");
        foreach ((string id, bool pass, string note) in Results)
            Console.WriteLine($"{(pass ? "PASS" : "FAIL")}  {id}  {note}");
        int failed = Results.Count(r => !r.Pass);
        Console.WriteLine($"{Results.Count - failed}/{Results.Count} passed");
        return failed == 0 ? 0 : 1;
    }

    // ================= scenario registry =================

    private static void RegisterScenarios()
    {
        Scenarios.Add(("S1", "Attach + find the AutoCount main window", S1_FindMainWindow));
        Scenarios.Add(("S2", "Open Service & Contract menu -> Maintain Service Contract, then close it", S2_OpenContractList));
        Scenarios.Add(("S3", "B1 validation: New contract -> Save empty -> 'Contract No is required.'", S3_ContractValidation));
    }

    // S1 — prove we can see the app without focusing it.
    private static string S1_FindMainWindow()
    {
        Window main = MainWindow();
        return $"main window: \"{main.Title}\"";
    }

    // S2 — prove menus open via patterns (no mouse) and forms close via WindowPattern (no Alt+F4).
    private static string S2_OpenContractList()
    {
        OpenPluginMenu("Maintain Service Contract");
        Window w = WaitWindow("Maintain Service Contract", 20);
        CloseWindow(w);
        return "opened + closed via WindowPattern (focus-free)";
    }

    // S3 — E2E guide B1: the empty-save validation fires and reads back word-for-word.
    private static string S3_ContractValidation()
    {
        OpenPluginMenu("Maintain Service Contract");
        Console.WriteLine("  [step] menu opened");
        Window list = WaitWindow("Maintain Service Contract", 20);
        Console.WriteLine("  [step] list form found");
        try
        {
            Press(FindInWindow(list, "New", ControlType.Button, 10));
            Console.WriteLine("  [step] New pressed");
            Window editor = WaitWindow("Service Contract", 20, exact: true);   // NOT the "Maintain …" list
            Console.WriteLine("  [step] editor open");
            try
            {
                // Ribbon "Save" — DevExpress ribbon buttons expose Invoke/DoDefaultAction.
                Press(FindInWindow(editor, "Save", ControlType.Button, 10));
                Console.WriteLine("  [step] Save pressed");
                Window box = WaitWindow("Validation", 10);
                Console.WriteLine("  [step] validation box found");
                string msg = AllText(box);
                Press(FindInWindow(box, "OK", ControlType.Button, 5));
                if (!msg.Contains("Contract No is required"))
                    throw new Exception($"unexpected validation text: \"{msg}\"");
                return "\"Contract No is required.\" verified";
            }
            finally
            {
                Window ed = FindWindow("Service Contract", exact: true);
                CloseWindow(ed);
                DismissIfPresent("Unsaved Changes", "Yes");
            }
        }
        finally { DismissIfPresent("Maintain Service Contract", null); }
    }

    // ================= pattern-only helpers =================

    /// <summary>Run a UIA call on a background task with a hard timeout. UIA pattern calls and
    /// even property/descendant reads BLOCK when the target window's UI thread is held by a
    /// modal dialog — this wrapper makes every such call abandonable so the harness can never
    /// hang (the #1 way the interactive MCP got stuck).</summary>
    private static void SafeCall(Action act, int seconds = 3)
    {
        Task t = Task.Run(act);
        t.ContinueWith(x => { _ = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        t.Wait(TimeSpan.FromSeconds(seconds));
    }

    private static T SafeGet<T>(Func<T> get, int seconds = 3, T fallback = default)
    {
        Task<T> t = Task.Run(get);
        t.ContinueWith(x => { _ = x.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        return t.Wait(TimeSpan.FromSeconds(seconds)) && t.Status == TaskStatus.RanToCompletion ? t.Result : fallback;
    }

    /// <summary>An ENABLED AutoCount shell window that actually carries the "Service &amp; Contract"
    /// menu (the main shell and SingleInstanceThreadForm module windows both merge it; disabled
    /// windows are modal-blocked and must never be used). Candidates come from WIN32 (with the
    /// enabled flag read there — free and truthful); only the menu probe enters UIA.</summary>
    private static Window MainWindow()
    {
        Window w = Retry.WhileNull(() =>
            {
                foreach (Win32.Win c in Win32.List(_pid))
                {
                    if (!c.Enabled || !c.Title.Contains("AutoCount", StringComparison.OrdinalIgnoreCase)) continue;
                    Window x = SafeGet(() => _ui.FromHandle(c.Hwnd).AsWindow(), 3);
                    if (x == null) continue;
                    bool hasMenu = SafeGet(() => x.FindFirstDescendant(cf => cf.ByName("Service & Contract")) != null, 3);
                    if (hasMenu) return x;
                }
                return null;
            },
            timeout: TimeSpan.FromSeconds(15), interval: TimeSpan.FromMilliseconds(400)).Result;
        return w ?? throw new Exception("no ENABLED AutoCount window with the 'Service & Contract' menu found");
    }

    /// <summary>Open "Service &amp; Contract" -&gt; &lt;item&gt; on the main menu bar, patterns only.
    /// DevExpress sub-menus pop up as separate top-level windows of the process, so after
    /// expanding the root item the target is searched across ALL the process's windows.</summary>
    private static void OpenPluginMenu(string itemName)
    {
        // DevExpress bar menus TOGGLE: pressing the root right after a previous menu action can
        // close a lingering (invisible) menu loop instead of opening the popup. Retry a few times,
        // re-finding the root fresh each attempt (elements go stale across popup cycles).
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Window main = MainWindow();
            AutomationElement root = Retry.WhileNull(() =>
                    SafeGet(() => main.FindFirstDescendant(cf => cf.ByName("Service & Contract")), 3),
                TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(400)).Result
                ?? throw new Exception("menu 'Service & Contract' not found on the main window");
            Press(root);                   // expand the root menu (Invoke/DoDefaultAction opens it)
            AutomationElement item = FindAnywhere(itemName, attempt == 1 ? 5 : 8);
            if (item != null) { Press(item); return; }
            Thread.Sleep(500);
        }
        throw new Exception($"menu item '{itemName}' did not appear after opening the menu (3 attempts)");
    }

    /// <summary>Press an element without the mouse: Invoke -&gt; ExpandCollapse -&gt; SelectionItem
    /// -&gt; LegacyIAccessible.DoDefaultAction, first supported wins.
    /// The pattern call runs on a BACKGROUND task with a short wait: when a button's handler
    /// opens a MODAL dialog, the UIA call does not return until the dialog closes (it times
    /// out as "UIA Timeout") even though the press landed. Scenarios always verify the press
    /// by waiting for its expected window, so a hung call is safe to abandon.</summary>
    private static void Press(AutomationElement el)
    {
        if (el == null) throw new Exception("Press(null)");
        Action act;
        if (SafeGet(() => el.Patterns.Invoke.IsSupported, 2)) act = () => el.Patterns.Invoke.Pattern.Invoke();
        else if (SafeGet(() => el.Patterns.ExpandCollapse.IsSupported, 2)) act = () => el.Patterns.ExpandCollapse.Pattern.Expand();
        else if (SafeGet(() => el.Patterns.SelectionItem.IsSupported, 2)) act = () => el.Patterns.SelectionItem.Pattern.Select();
        else if (SafeGet(() => el.Patterns.LegacyIAccessible.IsSupported, 2)) act = () => el.Patterns.LegacyIAccessible.Pattern.DoDefaultAction();
        else throw new Exception($"element '{SafeGet(() => el.Name, 2, "?")}' supports no press-like pattern");

        SafeCall(act);   // modal-opening presses never return — move on and verify
    }

    /// <summary>Set an edit's text via ValuePattern — no keyboard, no focus.</summary>
    private static void SetText(AutomationElement el, string value)
    {
        if (el == null) throw new Exception("SetText(null)");
        if (el.Patterns.Value.IsSupported) { SafeCall(() => el.Patterns.Value.Pattern.SetValue(value)); return; }
        if (el.Patterns.LegacyIAccessible.IsSupported) { SafeCall(() => el.Patterns.LegacyIAccessible.Pattern.SetValue(value)); return; }
        throw new Exception($"element '{el.Name}' has no Value pattern");
    }

    /// <summary>Close a form the focus-free way: WM_CLOSE via Win32 — identical to the user
    /// clicking X, never needs focus, and cannot block (unlike WindowPattern.Close on a
    /// modal-blocked window, and unlike Alt+F4/F2 which need focus).</summary>
    private static void CloseWindow(Window w)
    {
        if (w == null) return;
        IntPtr h = SafeGet(() => new IntPtr(w.Properties.NativeWindowHandle.ValueOrDefault), 2);
        if (h != IntPtr.Zero) Win32.Close(h);
    }

    /// <summary>Wait for a window whose title matches — discovery is PURE WIN32 (EnumWindows),
    /// which can never block; only the final element wrap enters UIA. exact=true matches the
    /// whole title ("Service Contract" must NOT match "Maintain Service Contract").</summary>
    private static Window WaitWindow(string titlePart, int seconds, bool exact = false)
    {
        Window w = Retry.WhileNull(() => FindWindow(titlePart, exact),
            TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(400)).Result;
        return w ?? throw new Exception($"window '{(exact ? titlePart : "*" + titlePart + "*")}' did not appear within {seconds}s");
    }

    private static Window FindWindow(string titlePart, bool exact = false)
    {
        Win32.Win h = Win32.Find(_pid, titlePart, exact);
        if (h == null) return null;
        return SafeGet(() => _ui.FromHandle(h.Hwnd).AsWindow(), 3);
    }

    /// <summary>Reset the app to a neutral state: dismiss dialogs (UIA press on the dialog
    /// itself — it always pumps), then WM_CLOSE stray forms deepest-first. Win32-driven, so it
    /// works even when everything is modal-blocked. Loops until stable (max 5 rounds).</summary>
    private static void Cleanup()
    {
        for (int round = 0; round < 5; round++)
        {
            bool acted = false;
            // 1) message boxes first — they block everything else
            foreach ((string title, string button) in new[] { ("Validation", "OK"), ("Unsaved Changes", "Yes"), ("Saved", "OK"), ("Error", "OK") })
            {
                Win32.Win box = Win32.Find(_pid, title, exact: true);
                if (box != null)
                {
                    Window w = SafeGet(() => _ui.FromHandle(box.Hwnd).AsWindow(), 3);
                    AutomationElement b = w == null ? null :
                        SafeGet(() => w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(button))), 3);
                    if (b != null) Press(b); else Win32.Close(box.Hwnd);
                    acted = true;
                }
            }
            // 2) stray plugin forms (modal editor before its owner list)
            foreach (string title in new[] { "Service Contract", "Service Item" })
            {
                Win32.Win f = Win32.Find(_pid, title, exact: true);
                if (f != null) { Win32.Close(f.Hwnd); acted = true; }
            }
            Win32.Win list = Win32.Find(_pid, "Maintain Service Contract", exact: false);
            if (list != null) { Win32.Close(list.Hwnd); acted = true; }
            if (!acted) return;
            Thread.Sleep(600);
        }
    }

    /// <summary>Find a named element of a type inside a window, with retry (every probe SafeGet-capped).</summary>
    private static AutomationElement FindInWindow(Window w, string name, ControlType type, int seconds)
    {
        AutomationElement el = Retry.WhileNull(() =>
                SafeGet(() => w.FindFirstDescendant(cf => cf.ByControlType(type).And(cf.ByName(name))), 3),
            TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(300)).Result;
        // DevExpress often exposes ribbon buttons with trailing hotkey text — relax to StartsWith.
        el ??= Retry.WhileNull(() =>
                SafeGet(() => w.FindAllDescendants(cf => cf.ByControlType(type))
                               .FirstOrDefault(x => x.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)), 4),
            TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(300)).Result;
        return el ?? throw new Exception($"'{name}' ({type}) not found in '{SafeGet(() => w.Title, 2, "?")}'");
    }

    /// <summary>Search every visible window of the process (menus pop up as their own windows,
    /// usually with an EMPTY title — so popups are searched FIRST). Win32 enumeration + per-window
    /// SafeGet-capped UIA descendant search.</summary>
    private static AutomationElement FindAnywhere(string name, int seconds)
    {
        return Retry.WhileNull(() =>
        {
            foreach (Win32.Win c in Win32.List(_pid).OrderBy(x => x.Title.Length))   // popups (no title) first
            {
                Window t = SafeGet(() => _ui.FromHandle(c.Hwnd).AsWindow(), 2);
                if (t == null) continue;
                AutomationElement el = SafeGet(() => t.FindFirstDescendant(cf => cf.ByName(name)), 3);
                if (el != null) return el;
            }
            return null;
        }, TimeSpan.FromSeconds(seconds), TimeSpan.FromMilliseconds(300)).Result;
    }

    /// <summary>All text content of a dialog (for asserting validation messages).</summary>
    private static string AllText(Window w)
    {
        return SafeGet(() => string.Join(" | ",
            w.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
             .Select(t => t.Name).Where(s => !string.IsNullOrWhiteSpace(s))), 5, "");
    }

    /// <summary>If a window with this title is open, press a button (or just close it). Never throws.</summary>
    private static void DismissIfPresent(string titlePart, string buttonName)
    {
        try
        {
            Window w = FindWindow(titlePart);
            if (w == null) return;
            if (buttonName != null)
            {
                AutomationElement b = w.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(buttonName)));
                if (b != null) { Press(b); return; }
            }
            CloseWindow(w);
        }
        catch { }
    }
}
