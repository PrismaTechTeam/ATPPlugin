using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Linq;
using ATPApi.AutoCount;
using ATPApi.Filters;
using ATPApi.Logging;

namespace ATPApi
{
    public static class Program
    {
        private const string AUTOCOUNT_DIR = @"C:\Program Files (x86)\AutoCount\Accounting 2.2";

        [STAThread]
        public static int Main(string[] args)
        {
            // Register the AutoCount DLL resolver before any AutoCount type is touched.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAutoCountDir;

            try
            {
                return Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Fatal: " + ex);
                return 1;
            }
        }

        private static Assembly ResolveFromAutoCountDir(object sender, ResolveEventArgs args)
        {
            string dllName = new AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(AUTOCOUNT_DIR, dllName);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run()
        {
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

                using (WebApp.Start<Startup>(baseUrl))
                {
                    log.LogInformation("ATPApi listening on {BaseUrl}", baseUrl);
                    Thread.Sleep(Timeout.Infinite);
                }

                return 0;
            }
            catch (Exception ex)
            {
                if (log != null) log.LogCritical(ex, "Fatal startup error");
                else Console.Error.WriteLine("Fatal (pre-logger): " + ex);
                return 1;
            }
            finally
            {
                LoggingBootstrap.Shutdown();
            }
        }
    }
}
