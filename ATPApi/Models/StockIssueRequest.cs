using System;

namespace ATPApi.Models
{
    /// <summary>
    /// PUMS → AutoCount Stock Issue Request payload (Webhook 1).
    /// Property casing matches the spec exactly so default JSON binding maps each field.
    /// </summary>
    public class StockIssueRequest
    {
        public string StockIssueId   { get; set; }
        public DateTime? IssueDateTime { get; set; }
        public string StockIssueNo   { get; set; }
        public string ReferenceNo    { get; set; }
        public string Description    { get; set; }
        public string Department     { get; set; }
        public string Job            { get; set; }
        public string Technician     { get; set; }
        public string Location       { get; set; }
        public string ItemCode       { get; set; }
        public decimal? Quantity     { get; set; }
        public string UOM            { get; set; }
    }
}
