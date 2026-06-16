namespace DataBision.Extractor.Extraction;

public sealed class ExtractionResult
{
    public string SapObject { get; init; } = string.Empty;
    public bool Success { get; init; }
    public int RowsExtracted { get; init; }
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsSkipped { get; init; }
    public TimeSpan Duration { get; init; }
    public string? WatermarkDate { get; init; }
    public string? WatermarkTs { get; init; }
    public string? Error { get; init; }
    public bool IsDryRun { get; init; }
    public int PagesFetched { get; init; }
    public bool HitMaxPages { get; init; }

    public static ExtractionResult NotImplemented(string sapObject) => new()
    {
        SapObject = sapObject,
        Success = false,
        Error = $"{sapObject} extraction not implemented until Sprint 3C."
    };

    public static ExtractionResult DryRun(string sapObject) => new()
    {
        SapObject = sapObject,
        Success = true,
        IsDryRun = true,
        Error = null
    };
}
