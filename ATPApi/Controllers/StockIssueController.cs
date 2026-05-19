using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ATPApi.Filters;
using ATPApi.Models;
using ATPApi.Repositories;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ATPApi.Controllers
{
    /// <summary>
    /// Webhook 1 — POST /api/stockissue.
    /// Receives one Stock Issue Request per call from PUMS and stores it with a
    /// New / Update status. History is preserved (old rows are not overwritten).
    /// </summary>
    [ApiKeyAuth]
    [RoutePrefix("api/stockissue")]
    public class StockIssueController : ApiController
    {
        private readonly ILogger _log;

        public StockIssueController(ILogger<StockIssueController> log)
        {
            _log = log;
        }

        [HttpPost, Route("")]
        public HttpResponseMessage Post([FromBody] StockIssueRequest req)
        {
            string incomingBody = null;
            object bodyObj;
            if (Request.Properties.TryGetValue(ATPApi.Filters.BodyCaptureHandler.PropertyKey, out bodyObj))
                incomingBody = bodyObj as string;
            if (string.IsNullOrEmpty(incomingBody))
            {
                try { incomingBody = Request.Content.ReadAsStringAsync().Result; } catch { }
            }

            if (req == null)
                return Fail(HttpStatusCode.BadRequest, null, "INVALID_PAYLOAD", "Request body is required or could not be parsed.", incomingBody);

            // Required-field validation per the spec
            if (string.IsNullOrWhiteSpace(req.StockIssueId))
                return Fail(HttpStatusCode.BadRequest, req.StockIssueId, "INVALID_PAYLOAD", "Field 'StockIssueId' is required.", incomingBody);
            if (req.IssueDateTime == null)
                return Fail(HttpStatusCode.BadRequest, req.StockIssueId, "INVALID_PAYLOAD", "Field 'IssueDateTime' is required.", incomingBody);
            if (string.IsNullOrWhiteSpace(req.StockIssueNo))
                return Fail(HttpStatusCode.BadRequest, req.StockIssueId, "INVALID_PAYLOAD", "Field 'StockIssueNo' is required.", incomingBody);
            if (req.Quantity == null)
                return Fail(HttpStatusCode.BadRequest, req.StockIssueId, "INVALID_PAYLOAD", "Field 'Quantity' is required.", incomingBody);

            string rawJson = ATPApi.Repositories.JsonFormat.Pretty(incomingBody ?? JsonConvert.SerializeObject(req));
            try
            {
                PumsRepository.UpsertOutcome outcome = PumsRepository.UpsertStockIssue(req, rawJson);
                string status = outcome.ToString();

                _log.LogInformation("Stock Issue accepted: id={Id} status={Status}", req.StockIssueId, status);
                string message = outcome == PumsRepository.UpsertOutcome.Ignored
                                 ? "Stock Issue duplicate ignored (FLAG_CONTROL=false)."
                                 : "Stock Issue task received.";
                PumsLogWriter.Write("Information", "WebhookStockIssue", req.StockIssueId,
                    message + " (status=" + status + ")", rawJson, status);

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success      = true,
                    stockIssueId = req.StockIssueId,
                    message      = message,
                    status       = status
                });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Stock Issue failed for id={Id}", req.StockIssueId);
                PumsLogWriter.Write("Error", "WebhookStockIssue", req.StockIssueId,
                    ex.Message, rawJson, ex.ToString());
                return Fail(HttpStatusCode.InternalServerError, req.StockIssueId, "INTERNAL_ERROR", ex.Message, rawJson);
            }
        }

        private HttpResponseMessage Fail(HttpStatusCode code, string id, string errorCode, string reason, string rawJson)
        {
            string prettyPayload = ATPApi.Repositories.JsonFormat.Pretty(rawJson);
            try
            {
                PumsLogWriter.Write("Error", "WebhookStockIssue", id,
                    errorCode + ": " + reason, prettyPayload, errorCode);
            }
            catch (Exception logEx)
            {
                _log.LogWarning(logEx, "Failed to write error to PumsLog for id={Id}", id);
            }

            return Request.CreateResponse(code, new
            {
                success      = false,
                stockIssueId = id,
                errorCode    = errorCode,
                reason       = reason
            });
        }
    }
}
