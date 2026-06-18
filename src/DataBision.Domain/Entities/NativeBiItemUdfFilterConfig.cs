namespace DataBision.Domain.Entities;

public class NativeBiItemUdfFilterConfig
{
    public int CompanyId { get; set; }
    public string UdfFieldName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsMultiSelect { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
