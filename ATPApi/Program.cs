using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Linq;
using Topshelf;
using ATPApi.AutoCount;
using ATPApi.Filters;
using ATPApi.Logging;

namespace ATPApi
{
    public static class Program
    {
        public const string SERVICE_NAME = "ATPApi";

        // Resolved once: where the AutoCount runtime DLLs live. Order: appsettings
        // "AutoCountInstallPath" → common install dirs → historical default. This makes the
        // service portable across machines (dev = Program Files, some servers = Program Files (x86)).
        private static string _autoCountDir;
        private static string AutoCountDir
        {
            get { return _autoCountDir ?? (_autoCountDir = ResolveAutoCountDir()); }
        }

        private static string ResolveAutoCountDir()
        {
            // 1. appsettings.json override — customer can pin the path explicitly.
            try
            {
                string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(cfg))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(
                        File.ReadAllText(cfg), "\"AutoCountInstallPath\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success)
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "\\");
                        if (Directory.Exists(p) && File.Exists(Path.Combine(p, "AutoCount.dll"))) return p;
                    }
                }
            }
            catch { /* keep trying */ }

            // 2. Registry — the AutoCount installer writes the install path here. Most reliable and
            //    version-independent. Check both 64- and 32-bit registry views.
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey k = baseKey.OpenSubKey(@"SOFTWARE\AutoCount\Accounting"))
                    {
                        string path = k == null ? null : k.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "AutoCount.dll"))) return path;
                    }
                }
                catch { /* keep trying */ }
            }

            // 3. Scan Program Files (and x86) for "Accounting <version>", newest first, skipping
            //    backup folders (e.g. "Accounting 2.2_bk") so a stale copy isn't picked.
            foreach (string root in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                try
                {
                    string acRoot = Path.Combine(root ?? "", "AutoCount");
                    if (!Directory.Exists(acRoot)) continue;
                    string latest = Directory.GetDirectories(acRoot, "Accounting *")
                        .Where(d => !LooksLikeBackup(Path.GetFileName(d)))
                        .Where(d => File.Exists(Path.Combine(d, "AutoCount.dll")))
                        .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (latest != null) return latest;
                }
                catch { /* keep trying */ }
            }

            // 4. Hardcoded fallback (matches the csproj HintPath / dev install).
            return @"C:\Program Files\AutoCount\Accounting 2.2";
        }

        private static bool LooksLikeBackup(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return false;
            string lower = folderName.ToLowerInvariant();
            return lower.Contains("_bk") || lower.Contains("_backup") || lower.Contains("_old")
                || lower.Contains("_archive") || lower.Contains(".bak");
        }

        [STAThread]
        public static int Main(string[] args)
        {
            // Register the AutoCount DLL resolver before any AutoCount type is touched.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAutoCountDir;

            // Topshelf hosts the OWIN API as a Windows Service and provides the
            // install / uninstall / start / stop verbs the updater + DEPLOYMENT use:
            //   ATPApi.exe install --localsystem --autostart
            //   ATPApi.exe start | stop | uninstall
            //   ATPApi.exe                (no args → runs interactively as a console)
            TopshelfExitCode rc = HostFactory.Run(x =>
            {
                x.Service<ApiHost>(s =>
                {
                    s.ConstructUsing(name => new ApiHost());
                    s.WhenStarted(h => h.Start());
                    s.WhenStopped(h => h.Stop());
                });
                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.EnableServiceRecovery(r => r.RestartService(1));
                x.SetServiceName(SERVICE_NAME);
                x.SetDisplayName("ATP Webhook API");
                x.SetDescription("ATP plugin webhook/API service (PUMS stock + meter integration for AutoCount).");
            });
            return (int)rc;
        }

        /// <summary>
        /// Topshelf service wrapper around the OWIN self-host. Start() boots the API
        /// (settings + logging + AutoCount session + migrations + WebApp), Stop() tears it down.
        /// </summary>
        private sealed class ApiHost
        {
            private IDisposable _webApp;

            public void Start()
            {
                int rc = Run(out _webApp);
                if (rc != 0 || _webApp == null)
                    throw new Exception("ATPApi failed to start (see logs).");
            }

            public void Stop()
            {
                try { if (_webApp != null) _webApp.Dispose(); } catch { }
                _webApp = null;
                LoggingBootstrap.Shutdown();
            }
        }

        private static Assembly ResolveFromAutoCountDir(object sender, ResolveEventArgs args)
        {
            string dllName = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(AutoCountDir, dllName);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(out IDisposable webApp)
        {
            webApp = null;
            ILogger log = null;
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var root = JObject.Parse(File.ReadAllText(settingsPath));

                LoggingBootstrap.Initialize(root);
                log = LoggingBootstrap.CreateLogger("ATPApi.Program");

                var baseUrl      = (string)root["BaseUrl"] ?? "http://localhost:5007";
                var profileName  = (string)root["DefaultProfile"] ?? "atplugin";
                var profile      = (JObject)root["Profiles"]?[profileName];
                var apiKey       = (string)root["ApiKey"];
                var corsOrigin   = (string)root["Cors"]?["AllowedOrigin"] ?? "*";

                ApiKeyAuthAttribute.SetKey(apiKey);
                Startup.CorsOrigin = corsOrigin;

                if (profile == null)
                    throw new InvalidOperationException($"Profile '{profileName}' not found in appsettings.json.");

                SessionManager.Initialize(
                    server:        (string)profile["Server"],
                    sqlUser:       (string)profile["SqlUser"],
                    sqlPassword:   (string)profile["SqlPassword"],
                    database:      (string)profile["Database"],
                    loginUser:     (string)profile["LoginUser"],
                    loginPassword: (string)profile["LoginPassword"],
                    profileName:   profileName);

                DbMigration.Run(SessionManager.DbSetting, log);

                // Start the OWIN host and hand the disposable back to the Topshelf wrapper,
                // which keeps it alive until the service stops (no blocking Sleep here).
                webApp = WebApp.Start<Startup>(baseUrl);
                log.LogInformation("ATPApi listening on {BaseUrl}", baseUrl);
                return 0;
            }
            catch (Exception ex)
            {
                if (log != null) log.LogCritical(ex, "Fatal startup error");
                else Console.Error.WriteLine("Fatal (pre-logger): " + ex);
                LoggingBootstrap.Shutdown();
                return 1;
            }
        }
    }
}
