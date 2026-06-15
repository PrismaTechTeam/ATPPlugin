using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace ATPShadowMain
{
    /// <summary>
    /// Dev launcher for the Service Contract Photocopier plugin.
    /// Boots the AutoCount runtime, programmatically logs in via App.config credentials,
    /// then opens the full AutoCount main window (FormMain) — bypassing the regular login
    /// dialog. Installed plugins are loaded, so the Service Contract module menu appears and
    /// can be navigated normally.
    ///
    /// To instead open a single plugin form directly (faster iteration on one form), see the
    /// commented LAUNCH OPTION B block in Run().
    /// </summary>
    internal static class Program
    {
        private const string AUTOCOUNT_DIR = @"C:\Program Files\AutoCount\Accounting 2.2";
        private static readonly string LOG_PATH = Path.Combine(
            Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? ".",
            "shadowmain.log");

        private static void Log(string msg)
        {
            try { File.AppendAllText(LOG_PATH, $"{DateTime.Now:HH:mm:ss.fff}  {msg}\r\n"); } catch { }
        }

        [STAThread]
        static void Main()
        {
            try { File.Delete(LOG_PATH); } catch { }
            Log("Main start");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAutoCountDir;
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log("UNHANDLED: " + e.ExceptionObject);
            Application.ThreadException += (s, e) => Log("THREAD: " + e.Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Run();
                Log("Run returned");
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
                MessageBox.Show(ex.ToString(), "ATPShadowMain — fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Log("Main end");
        }

        private static Assembly ResolveFromAutoCountDir(object sender, ResolveEventArgs args)
        {
            var dllName = new AssemblyName(args.Name).Name + ".dll";
            var path = Path.Combine(AUTOCOUNT_DIR, dllName);
            var found = File.Exists(path);
            Log($"Resolve {args.Name} -> {(found ? "OK " + path : "MISS")}");
            return found ? Assembly.LoadFrom(path) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run()
        {
            // === PREVIEW (temporary): open the new combined Service Contract list directly ===
            {
                AutoCount.MainEntry.Startup vstartup = new AutoCount.MainEntry.Startup();
                AutoCount.Data.DBSetting vdb = new AutoCount.Data.DBSetting(
                    AutoCount.Data.DBServerType.SQL2000,
                    ConfigurationManager.AppSettings["DBSetting.ServerName"],
                    ConfigurationManager.AppSettings["DBSetting.User"],
                    ConfigurationManager.AppSettings["DBSetting.Password"],
                    ConfigurationManager.AppSettings["DBSetting.DBName"],
                    false);
                AutoCount.Authentication.UserSession vses = new AutoCount.Authentication.UserSession(vdb);
                vstartup.SubProjectStartup(vses, AutoCount.MainEntry.StartupPlugInOption.NoLoad);
                bool vok = AutoCount.Authentication.UserSession.CurrentUserSession.Login(
                    ConfigurationManager.AppSettings["AutocountLogin.UserID"],
                    ConfigurationManager.AppSettings["AutocountLogin.Password"]);
                Log("PREVIEW login=" + vok);
                if (vok)
                {
                    // Provision the plugin schema on the target book (idempotent — safe on every run).
                    try
                    {
                        ServiceContractPhotocopier.Classes.ScpMigrations_Cls.RunEmbeddedSQLScripts(vses.DBSetting);
                        Log("PREVIEW migrations done");
                    }
                    catch (Exception mex) { Log("PREVIEW migrations FAILED: " + mex); }

                    Application.Run(new ServiceContractPhotocopier.ServiceContract.OperationForms.zSCP2_ContractLst_Form(vses.DBSetting));
                }
                return;
            }

            // ── LAUNCH OPTION A (current): full AutoCount main window, no login dialog. ──
            // MainStartup.StartWithStartupInfo is AutoCount's OWN public entry for programmatic
            // (no-dialog) login — the same path Accounting.exe takes for SQL-server startup. It
            // runs the entire boot in order: SetDefaultOEM, DatabaseManagement/ActiveDatabaseInfo,
            // license + ModuleController, system extensions, LoadStandardPlugIn (so the Service
            // Contract menu appears), the actual user Login, then RunMainForm. The call BLOCKS
            // until the main window is closed.
            //
            // NOTE: hand-constructing `new FormMain(userSession)` ourselves does NOT work — it
            // NREs in FormMain_Load because the license/ModuleController global state above isn't
            // set up. Let MainStartup do the full sequence; that's what this public method is for.
            Log("Run: SetDefaultOEM");
            AutoCount.MainEntry.MainStartup.Default.SetDefaultOEM();

            AutoCount.MainEntry.StartupInfo startInfo = new AutoCount.MainEntry.StartupInfo();
            startInfo.StartupType  = AutoCount.MainEntry.StartupType.Normal;
            startInfo.ServerName   = ConfigurationManager.AppSettings["DBSetting.ServerName"];
            startInfo.SqlUser      = ConfigurationManager.AppSettings["DBSetting.User"];
            startInfo.SqlPassword  = ConfigurationManager.AppSettings["DBSetting.Password"];
            startInfo.DatabaseName = ConfigurationManager.AppSettings["DBSetting.DBName"];
            startInfo.UserId       = ConfigurationManager.AppSettings["AutocountLogin.UserID"];
            startInfo.Password     = ConfigurationManager.AppSettings["AutocountLogin.Password"];

            Log("Run: StartWithStartupInfo (full AutoCount main UI)");
            bool ok = AutoCount.MainEntry.MainStartup.Default.StartWithStartupInfo(startInfo);
            Log("Run: StartWithStartupInfo returned=" + ok);
            if (!ok)
                MessageBox.Show("AutoCount login/startup failed. Check App.config credentials.",
                    "ATPShadowMain", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // ── LAUNCH OPTION B: open ONE plugin form directly (fast single-form iteration). ──
            // Comment out OPTION A above and uncomment this block. It boots a minimal session
            // (NoLoad — plugins aren't needed to new-up a form via ProjectReference) and runs one
            // form directly. Switch which form opens on the Application.Run line.
            /*
            AutoCount.MainEntry.Startup startup = new AutoCount.MainEntry.Startup();
            AutoCount.Data.DBSetting dbSetting = new AutoCount.Data.DBSetting(
                AutoCount.Data.DBServerType.SQL2000,
                ConfigurationManager.AppSettings["DBSetting.ServerName"],
                ConfigurationManager.AppSettings["DBSetting.User"],
                ConfigurationManager.AppSettings["DBSetting.Password"],
                ConfigurationManager.AppSettings["DBSetting.DBName"],
                false);
            AutoCount.Authentication.UserSession userSession =
                new AutoCount.Authentication.UserSession(dbSetting);
            startup.SubProjectStartup(userSession, AutoCount.MainEntry.StartupPlugInOption.NoLoad);
            bool okB = AutoCount.Authentication.UserSession.CurrentUserSession.Login(
                ConfigurationManager.AppSettings["AutocountLogin.UserID"],
                ConfigurationManager.AppSettings["AutocountLogin.Password"]);
            if (okB)
            {
                //   Application.Run(new ServiceContractPhotocopier.ServiceContract.OperationForms.ServiceContractLst_Form(userSession.DBSetting));
                //   Application.Run(new ServiceContractPhotocopier.ServiceItem.MasterForms.ServiceItem_Form(userSession.DBSetting));
                //   Application.Run(new ServiceContractPhotocopier.StockRequest.OperationForms.StockRequestIntegration_Form(userSession.DBSetting));
                Application.Run(new ServiceContractPhotocopier.MeterReading.OperationForms.MeterReadingIntegration_Form(userSession.DBSetting));
            }
            else
            {
                MessageBox.Show("AutoCount login failed. Check App.config credentials.",
                    "ATPShadowMain", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            */
        }
    }
}
