using AutoCount.Data;
using ServiceContractPhotocopier.Data;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// Picks the meter-reading client based on PumsConfig (METER_API_MODE). Default MOCK.
    /// The Meter Reading Integration form should always obtain its client through this factory so
    /// flipping MOCK -> LIVE is a config change with zero code edits.
    /// </summary>
    public static class MeterReadingApiClientFactory
    {
        public static IMeterReadingApiClient Create(DBSetting db)
        {
            string mode = PumsConfig.Get(db, PumsConfig.KEY_METER_API_MODE, PumsConfig.METER_API_MODE_MOCK);
            if (string.Equals(mode, PumsConfig.METER_API_MODE_LIVE, System.StringComparison.OrdinalIgnoreCase))
            {
                string baseUrl = PumsConfig.Get(db, PumsConfig.KEY_METER_API_BASE_URL, PumsConfig.DEFAULT_METER_API_BASE_URL);
                string apiKey = PumsConfig.Get(db, PumsConfig.KEY_METER_API_KEY, "");
                int timeout = PumsConfig.GetInt(db, PumsConfig.KEY_METER_API_TIMEOUT_MS, PumsConfig.DEFAULT_METER_API_TIMEOUT_MS);
                return new LiveMeterReadingApiClient(baseUrl, apiKey, timeout);
            }
            return new MockMeterReadingApiClient(db);
        }
    }
}
