namespace DataBision.Domain.Entities;

public class NativeBiDimensionConfig
{
    public int CompanyId { get; set; }
    public int DimensionNumber { get; set; }
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
