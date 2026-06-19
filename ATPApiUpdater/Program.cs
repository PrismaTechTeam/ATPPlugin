using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Threading;

namespace ATPApiUpdater
{
    /// <summary>
    /// Update driver for the ATPApi service. Runs elevated (UAC prompt via app.manifest).
    /// Steps: stop service -> zip backup of install dir -> download new ZIP -> verify SHA256 ->
    /// extract over install dir -> start service -> health check. On any failure after the backup
    /// snapshot is taken, rolls back by restoring the backup ZIP.
    ///
    /// Exit codes:
    ///   0  success
    ///   1  bad args
    ///   2  couldn't stop service
    ///   3  download or sha256 mismatch
    ///   4  extract failed
    ///   5  health check failed (rolled back OK)
    ///   6  rollback itself failed (install may be broken -- manual intervention)
    /// </summary>
    internal static class Program
    {
        private const string DEFAULT_SERVICE = "ATPApi";
        private const string DEFAULT_HEALTH  = "http://localhost:5007/api/ping";
        private const string SELF_EXE_NAME   = "ATPApiUpdater.exe";
        private static readonly string LogDir  = @"C:\ProgramData\ATPApi";
        private static readonly string LogFile = Path.Combine(LogDir, "updater.log");

        private sealed class Options
        {
            public string Url;
            public string Sha256;
            public string Version;
            public string ServiceName = DEFAULT_SERVICE;
            public string HealthUrl   = DEFAULT_HEALTH;
            public string InstallDir;
        }

        private static int Main(string[] args)
        {
            Directory.CreateDirectory(LogDir);

            var opts = ParseArgs(args);
            if (opts == null)
            {
                PrintUsage();
                return 1;
            }

            Log("============================================================");
            Log($"ATPApiUpdater starting  version={opts.Version ?? "(n/a)"}");
            Log($"  install-dir = {opts.InstallDir}");
            Log($"  url         = {opts.Url}");
            Log($"  sha256      = {opts.Sha256}");
            Log($"  service     = {opts.ServiceName}");
            Log($"  health      = {opts.HealthUrl}");

            string backupZip = null;
            string downloadedZip = null;
            try
            {
                StopService(opts.ServiceName, TimeSpan.FromSeconds(30));

                backupZip = CreateBackup(opts.InstallDir, opts.Version);
                Log($"Backup created: {backupZip}");

                downloadedZip = Download(opts.Url);
                Log($"Downloaded: {downloadedZip} ({new FileInfo(downloadedZip).Length:N0} bytes)");

                VerifySha256(downloadedZip, opts.Sha256);
                Log("SHA256 verified");

                ExtractOver(downloadedZip, opts.InstallDir, excludeLeafName: SELF_EXE_NAME);
                Log("Extract complete");

                StartService(opts.ServiceName, TimeSpan.FromSeconds(30));

                if (!HealthCheck(opts.HealthUrl, TimeSpan.FromSeconds(60)))
                    throw new UpdateException(5, "Health check failed (no 200 within 60s)");

                Log("=== Update SUCCESS ===");
                return 0;
            }
            catch (Exception ex)
            {
                int exitCode = (ex as UpdateException)?.ExitCode ?? 3;
                Log("ERROR: " + ex.Message);

                if (backupZip != null)
                {
                    Log("Rolling back from backup...");
                    try
                    {
                        StopService(opts.ServiceName, TimeSpan.FromSeconds(30));
                        ExtractOver(backupZip, opts.InstallDir, excludeLeafName: SELF_EXE_NAME);
                        StartService(opts.ServiceName, TimeSpan.FromSeconds(30));
                        Log("Rollback OK -- running previous version");
                        return exitCode == 0 ? 5 : exitCode;
                    }
                    catch (Exception rollEx)
                    {
                        Log("!!! ROLLBACK FAILED: " + rollEx.Message);
                        Log("!!! Install dir may be inconsistent -- see " + backupZip);
                        return 6;
                    }
                }
                return exitCode;
            }
            finally
            {
                try { if (downloadedZip != null && File.Exists(downloadedZip)) File.Delete(downloadedZip); } catch { }
            }
        }

        // ---- args -------------------------------------------------------------

        private static Options ParseArgs(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                Func<string> next = () => ++i < args.Length ? args[i] : null;
                switch (a)
                {
                    case "--url":         o.Url = next(); break;
                    case "--sha256":      o.Sha256 = next(); break;
                    case "--version":     o.Version = next(); break;
                    case "--service":     o.ServiceName = next(); break;
                    case "--health":      o.HealthUrl = next(); break;
                    case "--install-dir": o.InstallDir = next(); break;
                    default:
                        Console.Error.WriteLine($"Unknown arg: {a}");
                        return null;
                }
            }

            if (string.IsNullOrEmpty(o.Url) || string.IsNullOrEmpty(o.Sha256))
                return null;

            if (string.IsNullOrEmpty(o.InstallDir))
                o.InstallDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return o;
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: ATPApiUpdater.exe --url <zip-url> --sha256 <hex>\n" +
                "                         [--version <ver>] [--service <name>]\n" +
                "                         [--health <url>]  [--install-dir <path>]");
        }

        // ---- service control --------------------------------------------------

        private static void StopService(string name, TimeSpan timeout)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    try { var _ = sc.Status; }
                    catch (InvalidOperationException) { Log($"Service '{name}' not installed - skip stop"); return; }

                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        Log($"Stopping service '{name}' (currently {sc.Status})...");
                        if (sc.Status != ServiceControllerStatus.StopPending)
                            sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        Log("Service marked Stopped");
                    }
                    else
                    {
                        Log("Service already stopped");
                    }
                }
                // SCM reporting Stopped doesn't mean the hosting process has fully exited or
                // released DLL handles. Wait until the exe itself can be opened exclusively.
                WaitForExeUnlock(name, TimeSpan.FromSeconds(15));
            }
            catch (System.ComponentModel.Win32Exception wex)
            {
                throw new UpdateException(2, $"Stop service '{name}' failed: {wex.Message}");
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                throw new UpdateException(2, $"Timed out waiting for service '{name}' to stop");
            }
        }

        private static void WaitForExeUnlock(string serviceName, TimeSpan timeout)
        {
            var installDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var exePath = Path.Combine(installDir ?? "", serviceName + ".exe");
            if (!File.Exists(exePath)) return;

            var deadline = DateTime.UtcNow + timeout;
            int attempt = 0;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    foreach (var p in System.Diagnostics.Process.GetProcessesByName(serviceName))
                    {
                        try { p.WaitForExit(500); } catch { }
                        p.Dispose();
                    }

                    using (File.Open(exePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (attempt > 0) Log($"  exe released after {attempt} retries");
                        return;
                    }
                }
                catch (IOException)
                {
                    attempt++;
                    Thread.Sleep(500);
                }
            }
            throw new UpdateException(2,
                $"Service stopped but install dir still locked after {timeout.TotalSeconds}s - another process is holding a DLL handle");
        }

        private static void StartService(string name, TimeSpan timeout)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    try { var _ = sc.Status; }
                    catch (InvalidOperationException) { Log($"Service '{name}' not installed -- skip start"); return; }

                    if (sc.Status == ServiceControllerStatus.Running) { Log("Service already running"); return; }
                    Log($"Starting service '{name}'...");
                    if (sc.Status != ServiceControllerStatus.StartPending)
                        sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    Log("Service running");
                }
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                throw new Exception($"Timed out waiting for service '{name}' to start");
            }
        }

        // ---- backup / download / verify / extract -----------------------------

        private static string CreateBackup(string installDir, string version)
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var tag = string.IsNullOrEmpty(version) ? stamp : $"{version}-{stamp}";
            var backupRoot = Path.Combine(LogDir, "backup");
            Directory.CreateDirectory(backupRoot);
            var zipPath = Path.Combine(backupRoot, $"ATPApi-{tag}.zip");

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in EnumerateBackupFiles(installDir))
                {
                    var rel = file.Substring(installDir.Length).TrimStart('\\', '/');
                    try
                    {
                        zip.CreateEntryFromFile(file, rel, CompressionLevel.Fastest);
                    }
                    catch (IOException ex)
                    {
                        Log($"  skip (locked): {rel} -- {ex.Message}");
                    }
                }
            }
            return zipPath;
        }

        private static IEnumerable<string> EnumerateBackupFiles(string root)
        {
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var rel = f.Substring(root.Length).TrimStart('\\', '/');
                if (rel.StartsWith("logs\\", StringComparison.OrdinalIgnoreCase) ||
                    rel.StartsWith("logs/",  StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return f;
            }
        }

        private static string Download(string url)
        {
            var tmp = Path.Combine(Path.GetTempPath(), $"atpapi-{Guid.NewGuid():N}.zip");
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromMinutes(5);
                using (var resp = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    resp.EnsureSuccessStatusCode();
                    using (var src = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        src.CopyTo(dst);
                    }
                }
            }
            return tmp;
        }

        private static void VerifySha256(string path, string expected)
        {
            string actual;
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                actual = BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            }
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new UpdateException(3, $"SHA256 mismatch -- got {actual}, expected {expected}");
        }

        private static void ExtractOver(string zipPath, string installDir, string excludeLeafName)
        {
            Directory.CreateDirectory(installDir);
            using (var fs = File.OpenRead(zipPath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry
                    if (string.Equals(entry.Name, excludeLeafName, StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"  skip (self): {entry.FullName}");
                        continue;
                    }
                    var target = Path.Combine(installDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    ExtractOneWithRetry(entry, target);
                }
            }
        }

        private static void ExtractOneWithRetry(ZipArchiveEntry entry, string target)
        {
            const int maxAttempts = 8;
            var sleep = TimeSpan.FromMilliseconds(500);
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    entry.ExtractToFile(target, overwrite: true);
                    if (attempt > 1) Log($"  ok after {attempt} tries: {entry.FullName}");
                    return;
                }
                catch (IOException) when (attempt < maxAttempts - 1)
                {
                    Thread.Sleep(sleep);
                }
                catch (IOException) when (attempt == maxAttempts - 1)
                {
                    try
                    {
                        var stale = target + ".stale-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                        File.Move(target, stale);
                        Log($"  moved aside locked: {Path.GetFileName(target)} -> {Path.GetFileName(stale)}");
                    }
                    catch (Exception mvEx)
                    {
                        Log($"  rename-aside failed for {entry.FullName}: {mvEx.Message}");
                    }
                    Thread.Sleep(sleep);
                }
            }
        }

        // ---- health check -----------------------------------------------------

        private static bool HealthCheck(string url, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        var resp = http.GetAsync(url).GetAwaiter().GetResult();
                        if ((int)resp.StatusCode == 200)
                        {
                            Log($"Health check OK: {url}");
                            return true;
                        }
                        Log($"  health {url} -> {(int)resp.StatusCode}, retrying");
                    }
                    catch (Exception ex)
                    {
                        Log($"  health {url} -> {ex.GetType().Name}: {ex.Message}, retrying");
                    }
                    Thread.Sleep(2000);
                }
            }
            return false;
        }

        // ---- logging ----------------------------------------------------------

        private static readonly object _logLock = new object();
        private static void Log(string msg)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {msg}";
            Console.WriteLine(line);
            try
            {
                lock (_logLock)
                    File.AppendAllText(LogFile, line + Environment.NewLine);
            }
            catch { /* best effort */ }
        }

        private sealed class UpdateException : Exception
        {
            public int ExitCode { get; }
            public UpdateException(int exitCode, string message) : base(message) { ExitCode = exitCode; }
        }
    }
}
