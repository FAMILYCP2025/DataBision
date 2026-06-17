namespace DataBision.Application.DTOs.Admin;

public record CompanyDto(
    int Id,
    string Name,
    string Slug,
    string Status,
    string PlanName,
    int UserLimit,
    int CurrentUsers,
    DateTime CreatedAt,
    string? AnalyticsCompanyId);

public record CreateCompanyDto(
    string Name,
    string Slug,
    string PlanName = "Basic",
    int UserLimit = 10,
    string? AnalyticsCompanyId = null);

public record UpdateCompanyDto(
    string Name,
    string Status,
    string PlanName,
    int UserLimit,
    string? AnalyticsCompanyId = null);
