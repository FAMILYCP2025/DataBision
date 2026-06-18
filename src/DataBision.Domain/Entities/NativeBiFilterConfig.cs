namespace DataBision.Domain.Entities;

public class NativeBiFilterConfig
{
    public int CompanyId { get; set; }
    public string FilterKey { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsAdvanced { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public string? DefaultValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
