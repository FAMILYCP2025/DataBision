namespace DataBision.Application.DTOs.Dashboard;

public sealed class FinanceRefreshStatusDto
{
    public ExtractionRunSummary? LastOactExtraction { get; set; }
    public ExtractionRunSummary? LastOjdtExtraction { get; set; }
    public TransformRunSummary?  LastMartRefresh     { get; set; }

    /// <summary>MAX(refreshed_at) across mart accounting tables — reflects actual data freshness.</summary>
    public DateTimeOffset? LastDataRefreshedAt { get; set; }

    /// <summary>ok | warning | error | never_run</summary>
    public string OverallStatus { get; set; } = "never_run";

    public string StatusMessage { get; set; } = string.Empty;
}

public sealed class ExtractionRunSummary
{
    public DateTimeOffset  StartedAt     { get; set; }
    public DateTimeOffset? FinishedAt    { get; set; }
    public string          Status        { get; set; } = string.Empty;
    public int             RowsExtracted { get; set; }
    public string?         LastError     { get; set; }
}

public sealed class TransformRunSummary
{
    public DateTimeOffset  StartedAt        { get; set; }
    public DateTimeOffset? FinishedAt       { get; set; }
    public string          Status           { get; set; } = string.Empty;
    public int             ObjectsRefreshed { get; set; }
    public string?         LastError        { get; set; }
}
