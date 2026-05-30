namespace DataBision.Application.DTOs.Ingest;

public sealed class CheckpointDto
{
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string SapObject { get; init; } = string.Empty;
    public string? WatermarkDate { get; init; }
    public string? WatermarkTs { get; init; }
    public DateTime? LastSuccessfulRunUtc { get; init; }
    public long TotalRowsIngested { get; init; }
}
