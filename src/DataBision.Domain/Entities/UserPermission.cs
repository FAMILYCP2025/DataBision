namespace DataBision.Domain.Entities;

public class UserPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int ModuleId { get; set; }
    public int? ReportId { get; set; }
    public bool CanView { get; set; } = true;
    public int GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public Module Module { get; set; } = null!;
    public Report? Report { get; set; }
}
