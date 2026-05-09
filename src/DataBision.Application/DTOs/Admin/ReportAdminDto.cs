namespace DataBision.Application.DTOs.Admin;

public class ReportAdminDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateReportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateReportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateReportStatusDto
{
    public bool IsActive { get; set; }
}
