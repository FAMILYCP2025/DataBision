namespace DataBision.Extractor.DataBision;

/// <summary>
/// Local DTO for checkpoint data returned by GET /api/ingest/checkpoint/{companyId}/{sapObject}.
/// Mirrors the server-side CheckpointDto fields that the Extractor actually needs.
/// </summary>
public sealed class ExtractorCheckpoint
{
    public string? WatermarkDate { get; init; }
    public string? WatermarkTs { get; init; }
    public DateTime? LastSuccessfulRunUtc { get; init; }
    public long TotalRowsIngested { get; init; }
}
