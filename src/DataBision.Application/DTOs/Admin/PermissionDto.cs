namespace DataBision.Application.DTOs.Admin;

public record PermissionDto(
    int Id,
    int UserId,
    int CompanyId,
    int ModuleId,
    int? ReportId,
    bool CanView,
    DateTime GrantedAt);
