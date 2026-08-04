using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// TEST-ONLY client: serves readings parsed from a pasted JSON instead of calling the API.
    /// Reached through the hidden "TEST Fetch (JSON)" button on the Meter Reading Integration form
    /// (Ctrl+Shift+T reveals it) — the REAL fetch pipeline (serial matching, conflict detection,
    /// staging) runs unchanged on the fake data.
    /// Accepted JSON shapes:
    ///   [ { ... }, { ... } ]                       all entries served as ONLINE
    ///   { "Online": [ ... ], "Offline": [ ... ] }  split per endpoint
    /// Entry shape = the live API DTO: Code, SerialNumber, TotalBK, TotalCL, LastAuditDate,
    /// TrackingId. A missing LastAuditDate is auto-filled with day 1 of the requested period so the
    /// entry always qualifies for the selected billing month.
    /// </summary>
    public class JsonPasteMeterReadingApiClient : IMeterReadingApiClient
    {
        private readonly string _json;

        public JsonPasteMeterReadingApiClient(string json)
        {
            _json = json ?? "";
        }

        public List<MeterReadingDto> GetReadings(MachineStatus status, int month)
        {
            return GetReadings(status, DateTime.Today.Year, month);
        }

        public List<MeterReadingDto> GetReadings(MachineStatus status, int year, int month)
        {
            List<MeterReadingDto> list = new List<MeterReadingDto>();
            string body = _json.Trim();
            if (body.Length == 0) return list;
            if (body.StartsWith("["))
            {
                // Flat array: everything is ONLINE; the offline endpoint has no data.
                if (status == MachineStatus.Online)
                    list = JsonConvert.DeserializeObject<List<MeterReadingDto>>(body) ?? new List<MeterReadingDto>();
            }
            else
            {
                JObject root = JObject.Parse(body);
                JToken arr = root[status == MachineStatus.Online ? "Online" : "Offline"];
                if (arr != null)
                    list = arr.ToObject<List<MeterReadingDto>>() ?? new List<MeterReadingDto>();
            }
            foreach (MeterReadingDto d in list)
            {
                d.Status = status;
                if (!d.LastAuditDate.HasValue) d.LastAuditDate = new DateTime(year, month, 1);
            }
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
    }
}
