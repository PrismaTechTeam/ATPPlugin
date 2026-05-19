using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.Logging;

namespace ATPApi.Logging
{
    /// <summary>
    /// Minimal Web API <see cref="IDependencyResolver"/> that injects
    /// <see cref="ILogger{T}"/> into controllers. Add service injections here as needed.
    /// </summary>
    public class LoggerDependencyResolver : IDependencyResolver
    {
        public IDependencyScope BeginScope() => this;

        public object GetService(Type serviceType)
        {
            if (serviceType == null) return null;

            if (serviceType.IsGenericType &&
                serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
            {
                // ILogger<T> must be an actual ILogger<T> instance (not a bare ILogger),
                // otherwise WebAPI's controller activation throws a casting error.
                var category = serviceType.GetGenericArguments()[0];
                var loggerType = typeof(Logger<>).MakeGenericType(category);
                return Activator.CreateInstance(loggerType, LoggingBootstrap.LoggerFactory);
            }

            if (serviceType == typeof(ILoggerFactory))
                return LoggingBootstrap.LoggerFactory;

            // Let Web API construct controllers; we only inject loggers.
            if (typeof(System.Web.Http.Controllers.IHttpController).IsAssignableFrom(serviceType))
            {
                try
                {
                    var ctor = serviceType.GetConstructors()[0];
                    var parameters = ctor.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        args[i] = GetService(parameters[i].ParameterType);
                    return Activator.CreateInstance(serviceType, args);
                }
                catch { return null; }
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType) => Array.Empty<object>();

        public void Dispose() { }
    }
}
