namespace DataBision.Application.DTOs.Dashboard;

public sealed class OperationHealthDto
{
    public string? LastExtractorRunUtc { get; init; }
    public string? LastTransformRunUtc { get; init; }
    public string ExtractorStatus { get; init; } = string.Empty;
    public string TransformStatus { get; init; } = string.Empty;
    public int ActiveAlerts { get; init; }
    public int DqErrorsUnresolved { get; init; }
    public int ObjectsExtracted { get; init; }
    public int HealthScore { get; init; }
    public string? UpdatedAtUtc { get; init; }
}

public sealed class OperationAlertDto
{
    public long Id { get; init; }
    public string RuleCode { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string? TriggeredValue { get; init; }
    public string? Message { get; init; }
    public string TriggeredAtUtc { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
}

public sealed class OperationDataQualityDto
{
    public long Id { get; init; }
    public string SapObject { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int AffectedRows { get; init; }
    public string? SampleKey { get; init; }
    public string DetectedAtUtc { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
}
