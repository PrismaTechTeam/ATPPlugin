using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
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
            // Follow appsettings.json "AutoCountInstallPath" 100% — no registry probing, no folder
            // scanning. The configured path is authoritative; if it's wrong the service fails fast
            // (clear signal to fix the setting) rather than silently using a different AutoCount.
            try
            {
                string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(cfg))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(
                        File.ReadAllText(cfg), "\"AutoCountInstallPath\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success)
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "\\").Trim();
                        if (p.Length > 0) return p;
                    }
                }
            }
            catch { /* fall through to the default */ }

            // Default only when the setting is absent/empty.
            return @"C:\Program Files\AutoCount\Accounting 2.2";
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
