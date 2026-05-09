using DataBision.Domain.Enums;

namespace DataBision.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
    public string PlanName { get; set; } = "Basic";
    public int UserLimit { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CompanyBranding? Branding { get; set; }
    public ICollection<UserCompany> UserCompanies { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<EtlConfig> EtlConfigs { get; set; } = [];
}
