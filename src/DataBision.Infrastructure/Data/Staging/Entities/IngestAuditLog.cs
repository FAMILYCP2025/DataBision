using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBision.Infrastructure.Data.Staging.Entities;

[Table("ingest_audit_log", Schema = "audit")]
public sealed class IngestAuditLog
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(36)]
    public string RunId { get; set; } = string.Empty;

    [Required, MaxLength(36)]
    public string TenantId { get; set; } = string.Empty;

    [Required, MaxLength(36)]
    public string CompanyId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string SapObject { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Detail { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
