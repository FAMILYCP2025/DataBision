namespace DataBision.Shared.Constants;

public static class IngestionModes
{
    public const string DedicatedHana = "DEDICATED_HANA";
    public const string ServiceLayerQueue = "SERVICE_LAYER_QUEUE";
    public const string ServiceLayerDirect = "SERVICE_LAYER_DIRECT";
    public const string CsvInitialLoad = "CSV_INITIAL_LOAD";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        DedicatedHana,
        ServiceLayerQueue,
        ServiceLayerDirect,
        CsvInitialLoad
    };

    public static bool IsValid(string? mode) =>
        mode is not null && All.Contains(mode);
}
