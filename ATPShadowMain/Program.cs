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
    /// then opens a plugin form directly — bypassing the AutoCount Plug-in Manager and the
    /// regular login dialog. Use this for fast iteration during development.
    /// </summary>
    internal static class Program
    {
        private const string AUTOCOUNT_DIR = @"C:\Program Files (x86)\AutoCount\Accounting 2.2";
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
            Log("Run: creating Startup");
            var startup = new AutoCount.MainEntry.Startup();
            Log("Run: Startup created");

            var dbSetting = new AutoCount.Data.DBSetting(
                AutoCount.Data.DBServerType.SQL2000,
                ConfigurationManager.AppSettings["DBSetting.ServerName"],
                ConfigurationManager.AppSettings["DBSetting.User"],
                ConfigurationManager.AppSettings["DBSetting.Password"],
                ConfigurationManager.AppSettings["DBSetting.DBName"],
                false);

            var userSession = new AutoCount.Authentication.UserSession(dbSetting);

            Log("Run: SubProjectStartup");
            startup.SubProjectStartup(userSession);
            Log("Run: SubProjectStartup done");

            Log("Run: Login");
            var ok = AutoCount.Authentication.UserSession.CurrentUserSession.Login(
                    ConfigurationManager.AppSettings["AutocountLogin.UserID"],
                    ConfigurationManager.AppSettings["AutocountLogin.Password"]);
            Log("Run: Login result=" + ok);

            if (ok)
            {
                // ── Pick which form to open here. Edit this one line, F5, done. ──
                // Examples:
                //   Application.Run(new ServiceContractPhotocopier.ServiceContract.OperationForms.ServiceContractLst_Form(userSession.DBSetting));
                //   Application.Run(new ServiceContractPhotocopier.ServiceItem.MasterForms.ServiceItem_Form(userSession.DBSetting));
                //   Application.Run(new ServiceContractPhotocopier.ServiceNote.OperationForms.ServiceNoteLst_Form(userSession.DBSetting));
                //   Application.Run(new ServiceContractPhotocopier.StockRequest.OperationForms.StockRequestIntegration_Form(userSession.DBSetting));
                //   Application.Run(new ATPShadowMain.ShadowLauncherV2_Form(userSession.DBSetting));   // tabbed shell (has DX-eval popup quirk)
                Log("Run: opening form");
                Application.Run(new ServiceContractPhotocopier.MeterReading.OperationForms.MeterReadingIntegration_Form(userSession.DBSetting));
                Log("Run: form closed");
            }
            else
            {
                MessageBox.Show("AutoCount login failed. Check App.config credentials.",
                    "ATPShadowMain", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
