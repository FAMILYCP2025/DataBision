namespace DataBision.Domain.Entities;

public class CompanyBranding
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string PrimaryColor { get; set; } = "#2563EB";
    public string SecondaryColor { get; set; } = "#64748B";
    public string AccentColor { get; set; } = "#0EA5E9";
    public string BackgroundColor { get; set; } = "#F8FAFC";
    public string SidebarColor { get; set; } = "#0F172A";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CompanyDisplayName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
}
