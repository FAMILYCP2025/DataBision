namespace DataBision.Application.DTOs.Dashboard;

public sealed class ProcessDto
{
    public string ProcessCode { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class DashboardItemDto
{
    public string DashboardCode { get; init; } = string.Empty;
    public string DashboardName { get; init; } = string.Empty;
    public string DashboardType { get; init; } = string.Empty;
    public string ProcessCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}
