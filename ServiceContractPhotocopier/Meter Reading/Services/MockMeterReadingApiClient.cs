using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AutoCount.Data;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// MOCK client — used before the real PUMS API exists. Reads the actual machine serial numbers
    /// from zSCP2_Item and returns DETERMINISTIC cumulative meter totals for each (stable per serial +
    /// month) so the whole billing flow is testable end-to-end. Online = current month snapshot;
    /// Offline = the requested month. No HTTP, no external dependency.
    /// </summary>
    public class MockMeterReadingApiClient : IMeterReadingApiClient
    {
        private readonly DBSetting _db;

        public MockMeterReadingApiClient(DBSetting db)
        {
            _db = db;
        }

        public List<MeterReadingDto> GetOnline()
        {
            return Build(DateTime.Today.Month, false);
        }

        public List<MeterReadingDto> GetOffline(int month)
        {
            return Build(month, true);
        }

        private List<MeterReadingDto> Build(int month, bool offline)
        {
            List<MeterReadingDto> result = new List<MeterReadingDto>();
            int safeMonth = (month >= 1 && month <= 12) ? month : DateTime.Today.Month;

            using (SqlConnection conn = new SqlConnection(_db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT i.SerialNumber, i.MachineName FROM dbo.zSCP2_Item i " +
                "WHERE i.Inactive = 'N' AND LTRIM(RTRIM(i.SerialNumber)) <> '' " +
                "ORDER BY i.SerialNumber", conn))
            {
                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string serial = r["SerialNumber"] as string ?? string.Empty;
                        string machine = r["MachineName"] as string ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(serial)) continue;

                        int h = DeterministicHash(serial);
                        // Cumulative totals that grow with the month so consecutive billings show usage.
                        decimal totalBk = 100000m + (h % 50000) + (safeMonth * 1500m);
                        decimal totalCl = 40000m + (DeterministicHash(serial + "#CL") % 30000) + (safeMonth * 800m);

                        MeterReadingDto dto = new MeterReadingDto();
                        dto.Code = machine;
                        dto.SerialNumber = serial;
                        dto.TotalBK = totalBk;
                        dto.TotalCL = totalCl;
                        dto.LastAuditDate = new DateTime(DateTime.Today.Year, safeMonth,
                            Math.Min(DateTime.DaysInMonth(DateTime.Today.Year, safeMonth), 15), 12, 0, 0);
                        dto.TrackingId = offline ? ("MR-MOCK-" + safeMonth.ToString("00") + "-" + (h % 1000).ToString("000")) : null;
                        result.Add(dto);
                    }
                }
            }
            return result;
        }

        /// <summary>Stable hash independent of .NET's randomized String.GetHashCode (positive int).</summary>
        private static int DeterministicHash(string s)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in s) hash = hash * 31 + c;
                return hash & 0x7FFFFFFF;
            }
        }
    }
}
