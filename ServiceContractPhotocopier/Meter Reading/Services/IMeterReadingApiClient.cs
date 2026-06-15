using System.Collections.Generic;

namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// Client for the PUMS meter-reading API. Implementations: a local MOCK (for development before
    /// the real API is live) and a LIVE HTTP client. Both are BLOCKING — call them off the UI thread.
    /// Selected at runtime by <see cref="MeterReadingApiClientFactory"/> from PumsConfig.
    /// </summary>
    public interface IMeterReadingApiClient
    {
        /// <summary>API 1 — GET /api/meter-reading/online. Latest reading for every ONLINE machine.</summary>
        List<MeterReadingDto> GetOnline();

        /// <summary>API 2 — GET /api/meter-reading/offline?month=N. OFFLINE machines with a qualifying task that month.</summary>
        List<MeterReadingDto> GetOffline(int month);
    }
}
