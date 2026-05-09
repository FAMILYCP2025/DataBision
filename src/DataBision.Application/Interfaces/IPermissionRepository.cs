using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IPermissionRepository
{
    Task<Report?> GetReportAsync(int reportId);
    Task<bool> UserBelongsToCompanyAsync(int userId, int companyId);
    Task<bool> HasPermissionAsync(int userId, int companyId, int moduleId, int reportId);
    Task<bool> IsCompanyAdminAsync(int userId, int companyId);
    Task<bool> HasModulePermissionAsync(int userId, int companyId, int moduleId);
    Task UpsertPermissionsAsync(int companyId, int grantedBy, IEnumerable<PermissionUpdateDto> updates);
    Task ReplaceUserPermissionsAsync(int companyId, int targetUserId, int grantedBy, IEnumerable<PermissionUpdateDto> updates);
    Task<IEnumerable<UserPermission>> GetForCompanyAsync(int companyId);
}
