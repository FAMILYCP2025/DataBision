using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBision.Infrastructure.Data.Staging.Entities;

[Table("ingest_checkpoint", Schema = "ctl")]
public sealed class IngestCheckpoint
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(36)]
    public string TenantId { get; set; } = string.Empty;

    [Required, MaxLength(36)]
    public string CompanyId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string SapObject { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? WatermarkDate { get; set; }

    [MaxLength(6)]
    public string? WatermarkTs { get; set; }

    public DateTime? LastSuccessfulRunUtc { get; set; }

    public long TotalRowsIngested { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
