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
    /// Webhook 2 — POST /api/stocktransfer.
    /// Receives one Stock Transfer Request per call from PUMS and stores it with a
    /// New / Update status. History is preserved.
    /// </summary>
    [ApiKeyAuth]
    [RoutePrefix("api/stocktransfer")]
    public class StockTransferController : ApiController
    {
        private readonly ILogger _log;

        public StockTransferController(ILogger<StockTransferController> log)
        {
            _log = log;
        }

        [HttpPost, Route("")]
        public HttpResponseMessage Post([FromBody] StockTransferRequest req)
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

            if (string.IsNullOrWhiteSpace(req.RequestId))
                return Fail(HttpStatusCode.BadRequest, req.RequestId, "INVALID_PAYLOAD", "Field 'RequestId' is required.", incomingBody);
            if (req.DocumentDateTime == null)
                return Fail(HttpStatusCode.BadRequest, req.RequestId, "INVALID_PAYLOAD", "Field 'DocumentDateTime' is required.", incomingBody);
            if (req.Qty == null)
                return Fail(HttpStatusCode.BadRequest, req.RequestId, "INVALID_PAYLOAD", "Field 'qty' is required.", incomingBody);

            string rawJson = ATPApi.Repositories.JsonFormat.Pretty(incomingBody ?? JsonConvert.SerializeObject(req));
            try
            {
                PumsRepository.UpsertOutcome outcome = PumsRepository.UpsertStockTransfer(req, rawJson);
                string status = outcome.ToString();

                _log.LogInformation("Stock Transfer accepted: id={Id} status={Status}", req.RequestId, status);
                string message = outcome == PumsRepository.UpsertOutcome.Ignored
                                 ? "Stock Transfer duplicate ignored (FLAG_CONTROL=false)."
                                 : "Stock Transfer task received.";
                PumsLogWriter.Write("Information", "WebhookStockTransfer", req.RequestId,
                    message + " (status=" + status + ")", rawJson, status);

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success    = true,
                    requestId  = req.RequestId,
                    message    = message,
                    status     = status
                });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Stock Transfer failed for id={Id}", req.RequestId);
                PumsLogWriter.Write("Error", "WebhookStockTransfer", req.RequestId,
                    ex.Message, rawJson, ex.ToString());
                return Fail(HttpStatusCode.InternalServerError, req.RequestId, "INTERNAL_ERROR", ex.Message, rawJson);
            }
        }

        private HttpResponseMessage Fail(HttpStatusCode code, string id, string errorCode, string reason, string rawJson)
        {
            string prettyPayload = ATPApi.Repositories.JsonFormat.Pretty(rawJson);
            try
            {
                PumsLogWriter.Write("Error", "WebhookStockTransfer", id,
                    errorCode + ": " + reason, prettyPayload, errorCode);
            }
            catch (Exception logEx)
            {
                _log.LogWarning(logEx, "Failed to write error to PumsLog for id={Id}", id);
            }

            return Request.CreateResponse(code, new
            {
                success    = false,
                requestId  = id,
                errorCode  = errorCode,
                reason     = reason
            });
        }
    }
}
