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
        /// <summary>
        /// Unified entry point: ONE call, the <paramref name="status"/> discriminator picks the endpoint.
        ///   • Online  → API 1, GET /api/meter-reading/online (latest reading for every online machine).
        ///   • Offline → API 2, GET /api/meter-reading/offline?month=N (offline machines audited that month).
        /// Every returned DTO has its <see cref="MeterReadingDto.Status"/> set to <paramref name="status"/>.
        /// This is how a single interface serves both machine types — call it once per status.
        /// </summary>
        List<MeterReadingDto> GetReadings(MachineStatus status, int month);

        /// <summary>Convenience: GetReadings(Online, currentMonth).</summary>
        List<MeterReadingDto> GetOnline();

        /// <summary>Convenience: GetReadings(Offline, month).</summary>
        List<MeterReadingDto> GetOffline(int month);
    }
}
