namespace DataBision.Application.DTOs.Modules;

public record ModuleDto(int Id, string Name, string Slug, string? Icon, int ReportCount = 0);

public record ReportSummaryDto(int Id, string Name, string? Description, string? LastUpdated = null, string? EmbedUrl = null);
