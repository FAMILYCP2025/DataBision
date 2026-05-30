namespace DataBision.Application.DTOs.Ingest;

public sealed class IngestBatchResponse
{
    public string RunId { get; init; } = string.Empty;
    public string SapObject { get; init; } = string.Empty;
    public int RowsReceived { get; init; }
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsSkipped { get; init; }
    public DateTime ProcessedAtUtc { get; init; }
    public string? WatermarkDate { get; init; }
    public string? WatermarkTs { get; init; }
}
