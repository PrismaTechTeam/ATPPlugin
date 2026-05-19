using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace ATPApi.Logging
{
    /// <summary>
    /// Centralized logging setup. Builds a Serilog pipeline (console + file + optional Loki)
    /// and exposes it as an <see cref="ILoggerFactory"/> so the rest of the app uses the
    /// standard Microsoft.Extensions.Logging <see cref="ILogger{T}"/> abstraction.
    /// </summary>
    public static class LoggingBootstrap
    {
        private const string AppName = "ATPApi";

        public static ILoggerFactory LoggerFactory { get; private set; }

        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
        public static Microsoft.Extensions.Logging.ILogger CreateLogger(string category)
            => LoggerFactory.CreateLogger(category);

        public static void Initialize(JObject root)
        {
            var logging = (JObject)root["Logging"] ?? new JObject();

            var minLevelStr = (string)logging["MinimumLevel"] ?? "Information";
            var minLevel = Enum.TryParse<LogEventLevel>(minLevelStr, true, out var lvl)
                ? lvl : LogEventLevel.Information;

            var filePath = (string)logging["FilePath"] ?? "logs/atpapi-.log";
            if (!Path.IsPathRooted(filePath))
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            var retainDays   = (int?)logging["FileRollingDays"] ?? 14;
            var environment  = (string)root["Environment"] ?? "dev";
            var logServerUrl = (string)root["logServerUrl"];

            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SERILOG ERROR: " + msg);
                Console.ResetColor();
            });

            var cfg = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("app_name", AppName)
                .Enrich.WithProperty("env", environment)
                .WriteTo.Async(a => a.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"))
                .WriteTo.Async(a => a.File(
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainDays,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

            if (!string.IsNullOrWhiteSpace(logServerUrl))
            {
                cfg.WriteTo.Async(a => a.GrafanaLoki(
                    logServerUrl,
                    textFormatter: new LokiJsonTextFormatter(),
                    labels: new[]
                    {
                        new LokiLabel { Key = "app_name", Value = AppName },
                        new LokiLabel { Key = "env",      Value = environment }
                    }));
            }

            Log.Logger = cfg.CreateLogger();
            LoggerFactory = new Microsoft.Extensions.Logging.LoggerFactory()
                .AddSerilog(Log.Logger, dispose: true);
        }

        public static void Shutdown() => Log.CloseAndFlush();
    }
}
