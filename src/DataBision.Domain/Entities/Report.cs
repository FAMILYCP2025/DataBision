namespace DataBision.Domain.Entities;

public class Report
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string EmbedUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Module Module { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
