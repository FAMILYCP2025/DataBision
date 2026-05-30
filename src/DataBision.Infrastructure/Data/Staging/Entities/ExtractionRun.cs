using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBision.Infrastructure.Data.Staging.Entities;

[Table("extraction_run", Schema = "ctl")]
public sealed class ExtractionRun
{
    [Key, MaxLength(36)]
    public string RunId { get; set; } = string.Empty;

    [Required, MaxLength(36)]
    public string TenantId { get; set; } = string.Empty;

    [Required, MaxLength(36)]
    public string CompanyId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string SapObject { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string IngestionMode { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "RUNNING";

    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public int RowsReceived { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsSkipped { get; set; }

    [MaxLength(10)]
    public string? WatermarkDate { get; set; }

    [MaxLength(6)]
    public string? WatermarkTs { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}
