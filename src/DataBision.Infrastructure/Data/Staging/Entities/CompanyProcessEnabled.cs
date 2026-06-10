using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataBision.Infrastructure.Data.Staging.Entities;

[Table("company_process_enabled", Schema = "cfg")]
public sealed class CompanyProcessEnabled
{
    [Required, MaxLength(100)]
    public string CompanyId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ProcessCode { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime EnabledAtUtc { get; set; } = DateTime.UtcNow;
}
