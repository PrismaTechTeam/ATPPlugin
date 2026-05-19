using System.IO;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json.Serialization;
using Owin;
using Swashbuckle.Application;
using ATPApi.Logging;
using ATPApi.Filters;

namespace ATPApi
{
    public class Startup
    {
        /// <summary>
        /// Set by <c>Program.Main</c> before <c>WebApp.Start</c> from appsettings <c>Cors.AllowedOrigin</c>.
        /// </summary>
        public static string CorsOrigin = "*";

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            // CORS — the HTML clones live on a different origin (http://localhost:8765)
            // so the browser preflights every POST. Allow the configured origin with
            // any header (so X-API-Key passes) and any method.
            config.EnableCors(new EnableCorsAttribute(CorsOrigin, "*", "*"));

            config.DependencyResolver = new LoggerDependencyResolver();
            config.Services.Add(typeof(System.Web.Http.ExceptionHandling.IExceptionLogger),
                new GlobalExceptionLogger());

            config.MessageHandlers.Add(new BodyCaptureHandler());

            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            var json = config.Formatters.JsonFormatter;
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            var xmlDoc = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ATPApi.xml");
            config
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "ATP API");
                    if (File.Exists(xmlDoc)) c.IncludeXmlComments(xmlDoc);
                })
                .EnableSwaggerUi();

            app.UseWebApi(config);
        }
    }
}
