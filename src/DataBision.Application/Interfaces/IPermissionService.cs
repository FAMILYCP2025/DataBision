using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> CanViewReportAsync(int userId, int reportId, int companyId);
    Task<bool> CanViewModuleAsync(int userId, int moduleId, int companyId);
    Task<bool> IsCompanyAdminAsync(int userId, int companyId);
    Task<bool> UserBelongsToCompanyAsync(int userId, int companyId);
    Task UpdatePermissionsBatchAsync(int companyId, int grantedBy, IEnumerable<PermissionUpdateDto> updates);
    Task<PermissionsChangeResult> ReplaceUserPermissionsAsync(int companyId, int targetUserId, int grantedBy, IEnumerable<PermissionUpdateDto> updates);
    Task<IEnumerable<PermissionDto>> GetForCompanyAsync(int companyId);
}

public record PermissionUpdateDto(int UserId, int ModuleId, int? ReportId, bool CanView);

public record PermissionChange(int ModuleId, int? ReportId);

public record PermissionsChangeResult(IReadOnlyList<PermissionChange> Added, IReadOnlyList<PermissionChange> Removed);
