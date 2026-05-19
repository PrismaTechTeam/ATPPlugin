using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ATPApi.Filters
{
    /// <summary>
    /// Validates the <c>X-API-Key</c> header against the configured key.
    /// Returns 401 with the standard {success:false, errorCode:"UNAUTHORIZED"} body on failure.
    /// The key is loaded from appsettings.json at startup via <see cref="SetKey"/>.
    /// </summary>
    public sealed class ApiKeyAuthAttribute : AuthorizationFilterAttribute
    {
        private const string HeaderName = "X-API-Key";
        private static string _configuredKey;

        public static void SetKey(string key)
        {
            _configuredKey = key ?? string.Empty;
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext == null) return;

            System.Collections.Generic.IEnumerable<string> values;
            string supplied = null;
            if (actionContext.Request.Headers.TryGetValues(HeaderName, out values))
            {
                foreach (string v in values) { supplied = v; break; }
            }

            if (string.IsNullOrEmpty(_configuredKey))
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { success = false, errorCode = "INTERNAL_ERROR", reason = "Server API key not configured." });
                return;
            }

            if (string.IsNullOrEmpty(supplied) || !string.Equals(supplied, _configuredKey, StringComparison.Ordinal))
            {
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized,
                    new { success = false, errorCode = "UNAUTHORIZED", reason = "Missing or invalid X-API-Key header." });
            }
        }
    }
}
