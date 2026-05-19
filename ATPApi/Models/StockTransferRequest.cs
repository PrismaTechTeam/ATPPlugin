using System;
using Newtonsoft.Json;

namespace ATPApi.Models
{
    /// <summary>
    /// PUMS → AutoCount Stock Transfer Request payload (Webhook 2).
    /// Lowercase fields (qty, type, unit, approval) are mapped via Newtonsoft's
    /// JsonProperty since C# property names must be PascalCase.
    /// </summary>
    public class StockTransferRequest
    {
        public string RequestId         { get; set; }
        public DateTime? DocumentDateTime { get; set; }
        public string Technician        { get; set; }
        public string Part              { get; set; }

        [JsonProperty("qty")]
        public decimal? Qty             { get; set; }

        [JsonProperty("type")]
        public string Type              { get; set; }

        [JsonProperty("unit")]
        public string Unit              { get; set; }

        [JsonProperty("approval")]
        public string Approval          { get; set; }
    }
}
