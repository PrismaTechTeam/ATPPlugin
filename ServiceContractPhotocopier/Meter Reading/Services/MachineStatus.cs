namespace ServiceContractPhotocopier.MeterReading.Services
{
    /// <summary>
    /// The two kinds of machine a service item can be, and the two PUMS meter-reading endpoints
    /// behind a single <see cref="IMeterReadingApiClient"/>:
    ///   • Online  — machine reports live; read from API 1 (/api/meter-reading/online).
    ///   • Offline — machine is audited manually; read from API 2 (/api/meter-reading/offline?month=N).
    /// A reading's Status is set to whichever endpoint produced it, so the UI can show the type
    /// without storing a flag on the item (the API is the source of truth).
    /// </summary>
    public enum MachineStatus
    {
        Online = 0,
        Offline = 1
    }
}
