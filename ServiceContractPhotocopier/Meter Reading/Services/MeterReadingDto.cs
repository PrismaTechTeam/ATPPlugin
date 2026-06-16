using System;
using Newtonsoft.Json;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// One machine's latest meter snapshot returned by the PUMS meter-reading API.
    /// Shape matches the API spec exactly (API 1 online / API 2 offline). TotalBK / TotalCL are
    /// CUMULATIVE lifetime totals per machine, keyed by SerialNumber. TrackingId is offline-only.
    /// </summary>
    public class MeterReadingDto
    {
        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("SerialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("TotalBK")]
        public decimal TotalBK { get; set; }

        [JsonProperty("TotalCL")]
        public decimal TotalCL { get; set; }

        [JsonProperty("LastAuditDate")]
        public DateTime? LastAuditDate { get; set; }

        [JsonProperty("TrackingId")]
        public string TrackingId { get; set; }

        /// <summary>
        /// Which endpoint produced this reading (Online = API 1, Offline = API 2). Set by the client,
        /// not part of the wire JSON — it tells the UI the machine's type. Defaults to Online.
        /// </summary>
        [JsonIgnore]
        public MachineStatus Status { get; set; }
    }
}
