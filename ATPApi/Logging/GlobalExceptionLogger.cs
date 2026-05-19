using System.Web.Http.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace ATPApi.Logging
{
    /// <summary>
    /// Catches every unhandled exception thrown from a Web API action and routes it
    /// through ILogger so it lands in the file sink (and Loki, if enabled).
    /// Registered in <see cref="Startup"/>.
    /// </summary>
    public class GlobalExceptionLogger : ExceptionLogger
    {
        private readonly ILogger _log;

        public GlobalExceptionLogger()
        {
            _log = LoggingBootstrap.CreateLogger("ATPApi.UnhandledException");
        }

        public override void Log(ExceptionLoggerContext context)
        {
            var req = context.Request;
            _log.LogError(context.Exception,
                "Unhandled exception in {Method} {Url}",
                req?.Method?.Method, req?.RequestUri);
        }
    }
}
