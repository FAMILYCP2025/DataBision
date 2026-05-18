namespace DataBision.Application.DTOs.Reports;

public record ReportEmbedConfigDto(
    int Id,
    int ModuleId,
    string Name,
    string? Description,
    string EmbedUrl,
    string WorkspaceId,
    string ReportId,
    string DatasetId,
    bool IsConfigured);
