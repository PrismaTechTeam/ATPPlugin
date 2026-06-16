using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// LIVE client — calls the real PUMS meter-reading HTTP endpoints with the X-API-Key header.
    /// Synchronous (blocks the calling thread); the Meter Reading Integration form calls it from a
    /// background task. Switching from MOCK to LIVE is purely config: set METER_API_MODE=LIVE and
    /// point METER_API_BASE_URL at the real host — no code change.
    /// </summary>
    public class LiveMeterReadingApiClient : IMeterReadingApiClient
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly int _timeoutMs;

        public LiveMeterReadingApiClient(string baseUrl, string apiKey, int timeoutMs)
        {
            _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
            _apiKey = apiKey ?? string.Empty;
            _timeoutMs = timeoutMs > 0 ? timeoutMs : 15000;
        }

        public List<MeterReadingDto> GetReadings(MachineStatus status, int month)
        {
            string path = status == MachineStatus.Offline
                ? "/api/meter-reading/offline?month=" + month
                : "/api/meter-reading/online";
            List<MeterReadingDto> list = GetArray(path);
            foreach (MeterReadingDto d in list) d.Status = status;   // tag with the producing endpoint
            return list;
        }

        public List<MeterReadingDto> GetOnline()
        {
            return GetReadings(MachineStatus.Online, System.DateTime.Today.Month);
        }

        public List<MeterReadingDto> GetOffline(int month)
        {
            return GetReadings(MachineStatus.Offline, month);
        }

        private List<MeterReadingDto> GetArray(string relativePath)
        {
            string url = _baseUrl + relativePath;
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);
                if (!string.IsNullOrEmpty(_apiKey))
                    client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

                HttpResponseMessage resp = client.GetAsync(url).GetAwaiter().GetResult();
                string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode)
                    throw new MeterReadingApiException(
                        "Meter API GET " + url + " returned " + (int)resp.StatusCode + " " + resp.StatusCode + ".", body);

                List<MeterReadingDto> list = JsonConvert.DeserializeObject<List<MeterReadingDto>>(body);
                return list ?? new List<MeterReadingDto>();
            }
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
