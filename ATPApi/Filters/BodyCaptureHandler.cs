using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ATPApi.Filters
{
    public class BodyCaptureHandler : DelegatingHandler
    {
        public const string PropertyKey = "ATP.RawRequestBody";

        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content != null)
            {
                string body = await request.Content.ReadAsStringAsync();
                request.Properties[PropertyKey] = body ?? string.Empty;
            }
            else
            {
                request.Properties[PropertyKey] = string.Empty;
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
