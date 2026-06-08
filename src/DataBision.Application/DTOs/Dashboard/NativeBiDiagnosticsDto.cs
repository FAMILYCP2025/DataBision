namespace DataBision.Application.DTOs.Dashboard;

public sealed class NativeBiDiagnosticsDto
{
    public string CompanyId { get; init; } = string.Empty;
    public string Status { get; init; } = "unknown"; // ok | warning | error | unknown
    public IReadOnlyList<DiagnosticCheckDto> Checks { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; }
}

public sealed class DiagnosticCheckDto
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = "unknown"; // ok | warning | error | unknown
    public string? Detail { get; init; }
}

public sealed class NativeBiTableCountsDto
{
    public string CompanyId { get; init; } = string.Empty;
    public IReadOnlyList<TableCountDto> Tables { get; init; } = [];
    public DateTime GeneratedAtUtc { get; init; }
}

public sealed class TableCountDto
{
    public string Schema { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public long RowCount { get; init; }
    public DateTime? TransformedAtUtc { get; init; }
}
