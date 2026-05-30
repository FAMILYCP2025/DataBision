using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBision.Infrastructure.Data.Staging.Entities;

[Table("source_object_config", Schema = "ctl")]
public sealed class SourceObjectConfig
{
    [Key, MaxLength(50)]
    public string SourceObject { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    [MaxLength(30)]
    public string ExtractionMode { get; set; } = "INCREMENTAL";

    public int FrequencyMinutes { get; set; } = 60;

    public int LookbackNormalDays { get; set; } = 3;
    public int LookbackNightlyDays { get; set; } = 7;
    public int LookbackMonthCloseDays { get; set; } = 35;

    [MaxLength(10)]
    public string? InitialLoadFromDate { get; set; }

    public bool SupportsUpdateTs { get; set; } = false;
    public bool SupportsCreateTs { get; set; } = false;

    [MaxLength(50)]
    public string? HeaderTable { get; set; }

    [MaxLength(50)]
    public string? LineTable { get; set; }

    [MaxLength(200)]
    public string? PrimaryKey { get; set; }

    [MaxLength(200)]
    public string? NaturalKey { get; set; }

    public bool IsMasterData { get; set; } = false;

    public int PageSize { get; set; } = 500;
    public int MaxPerRun { get; set; } = 10000;
    public int RetentionDaysRaw { get; set; } = 90;

    [MaxLength(254)]
    public string? AlertEmail { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
}
