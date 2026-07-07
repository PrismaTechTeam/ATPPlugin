using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// LIVE client — calls the real PUMS meter-reading endpoints (atgroup.asia PHP API):
    ///   Online : GET {base}/api/meter-reading-online.php?token={token}
    ///   Offline: GET {base}/api/meter-reading-offline.php?month=YYYY-MM&amp;token={token}
    /// Auth is a ?token= query parameter (NOT a header). Both return a JSON array of MeterReadingDto
    /// (Code, SerialNumber, TotalBK, TotalCL, LastAuditDate; offline also has TrackingId).
    /// Synchronous (blocks the caller); the Meter Reading Integration form calls it from a background
    /// task. Switching MOCK -> LIVE is config-only: METER_API_MODE=LIVE,
    /// METER_API_BASE_URL=https://atgroup.asia, METER_API_KEY=&lt;token&gt;.
    /// </summary>
    public class LiveMeterReadingApiClient : IMeterReadingApiClient
    {
        private const string ONLINE_PATH  = "/api/meter-reading-online.php";
        private const string OFFLINE_PATH = "/api/meter-reading-offline.php";
        private const int MAX_ATTEMPTS   = 3;      // retry transient connection failures (endpoint is fast)
        private const int RETRY_DELAY_MS = 700;

        private readonly string _baseUrl;
        private readonly string _token;
        private readonly int _timeoutMs;

        public LiveMeterReadingApiClient(string baseUrl, string token, int timeoutMs)
        {
            _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
            _token = token ?? string.Empty;
            _timeoutMs = timeoutMs > 0 ? timeoutMs : 15000;
        }

        public List<MeterReadingDto> GetReadings(MachineStatus status, int month)
        {
            string url;
            if (status == MachineStatus.Offline)
            {
                // The offline endpoint keys by billing month as YYYY-MM. The form works within the
                // current year (it derives the year from DateTime.Today everywhere), so pair the
                // selected month with the current year.
                string yearMonth = string.Format("{0:0000}-{1:00}", DateTime.Today.Year, month);
                url = _baseUrl + OFFLINE_PATH + "?month=" + Uri.EscapeDataString(yearMonth) + TokenSuffix("&");
            }
            else
            {
                url = _baseUrl + ONLINE_PATH + TokenSuffix("?");
            }

            List<MeterReadingDto> list = GetArray(url);
            foreach (MeterReadingDto d in list) d.Status = status;   // tag with the producing endpoint
            return list;
        }

        public List<MeterReadingDto> GetOnline()
        {
            return GetReadings(MachineStatus.Online, DateTime.Today.Month);
        }

        public List<MeterReadingDto> GetOffline(int month)
        {
            return GetReadings(MachineStatus.Offline, month);
        }

        private string TokenSuffix(string separator)
        {
            return string.IsNullOrEmpty(_token) ? string.Empty : separator + "token=" + Uri.EscapeDataString(_token);
        }

        private List<MeterReadingDto> GetArray(string url)
        {
            // The API is HTTPS; ensure TLS 1.2 is enabled (older .NET Framework defaults can omit it).
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

            // The endpoints are fast (<1s) but occasionally drop the connection (transient server
            // hiccup). Retry TRANSPORT failures a few times; never retry a real HTTP status error.
            Exception last = null;
            for (int i = 1; i <= MAX_ATTEMPTS; i++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);

                        HttpResponseMessage resp = client.GetAsync(url).GetAwaiter().GetResult();
                        string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!resp.IsSuccessStatusCode)
                            throw new MeterReadingApiException(
                                "Meter API GET " + Redact(url) + " returned " + (int)resp.StatusCode + " " + resp.StatusCode + ".", body);

                        List<MeterReadingDto> list = JsonConvert.DeserializeObject<List<MeterReadingDto>>(body);
                        return list ?? new List<MeterReadingDto>();
                    }
                }
                catch (MeterReadingApiException) { throw; }        // explicit HTTP status — not transient
                catch (Exception ex)
                {
                    last = ex;                                      // connection/transport blip — retry
                    if (i < MAX_ATTEMPTS) System.Threading.Thread.Sleep(RETRY_DELAY_MS);
                }
            }
            throw new MeterReadingApiException(
                "Meter API GET " + Redact(url) + " failed after " + MAX_ATTEMPTS + " attempts (transient connection error): " +
                (last != null ? last.Message : "unknown error"), "");
        }

        // Keep the token out of exception messages / logs.
        private string Redact(string url)
        {
            return string.IsNullOrEmpty(_token) ? url : url.Replace(_token, "***");
        }
    }

    /// <summary>Raised when the meter API returns a non-success status; carries the raw body for logging.</summary>
    public class MeterReadingApiException : Exception
    {
        public string ResponseBody { get; private set; }

        public MeterReadingApiException(string message, string responseBody) : base(message)
        {
            ResponseBody = responseBody;
        }
    }
}
