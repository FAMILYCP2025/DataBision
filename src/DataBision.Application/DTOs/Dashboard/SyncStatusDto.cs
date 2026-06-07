namespace DataBision.Application.DTOs.Dashboard;

public sealed class SyncStatusDto
{
    public string CompanyId { get; init; } = string.Empty;
    public string OverallStatus { get; init; } = "unknown";
    public DateTime? LastSyncAtUtc { get; init; }
    public DateTime? LastTransformAtUtc { get; init; }
    public IReadOnlyList<SyncObjectStatusDto> Objects { get; init; } = [];
    public DataFreshnessDto DataFreshness { get; init; } = new();
}

public sealed class SyncObjectStatusDto
{
    public string SapObject { get; init; } = string.Empty;
    public string? WatermarkDate { get; init; }
    public DateTime? LastSuccessfulRunUtc { get; init; }
    public long TotalRowsIngested { get; init; }
    public string Status { get; init; } = "unknown";
}

public sealed class DataFreshnessDto
{
    public DateTime? RawLastUpdatedAtUtc { get; init; }
    public DateTime? StgLastTransformedAtUtc { get; init; }
    public DateTime? MartLastTransformedAtUtc { get; init; }
}

public sealed class TransformStatusDto
{
    public string CompanyId { get; init; } = string.Empty;
    public DateTime? MartTransformedAtUtc { get; init; }
    public DateTime? StgTransformedAtUtc { get; init; }
    public IReadOnlyList<MartTableStatusDto> MartTables { get; init; } = [];
}

public sealed class MartTableStatusDto
{
    public string TableName { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public DateTime? TransformedAtUtc { get; init; }
}
