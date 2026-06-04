using DataBision.Extractor.Extraction;

namespace DataBision.Extractor.Scheduling;

/// <summary>
/// Per-object result within a scheduled cycle.
/// </summary>
public sealed class ScheduledExtractionSummary
{
    public string SapObject { get; init; } = string.Empty;
    public bool Success { get; init; }
    public int RowsExtracted { get; init; }
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsSkipped { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Error { get; init; }

    public static ScheduledExtractionSummary FromResult(ExtractionResult r) => new()
    {
        SapObject    = r.SapObject,
        Success      = r.Success,
        RowsExtracted = r.RowsExtracted,
        RowsInserted = r.RowsInserted,
        RowsUpdated  = r.RowsUpdated,
        RowsSkipped  = r.RowsSkipped,
        Duration     = r.Duration,
        Error        = r.Error
    };

    public static ScheduledExtractionSummary Failed(string sapObject, string error) => new()
    {
        SapObject = sapObject,
        Success   = false,
        Error     = error
    };
}
