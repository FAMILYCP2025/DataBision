using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;

namespace DataBision.Application.Services;

public class PermissionService(IPermissionRepository repository) : IPermissionService
{
    public async Task<bool> CanViewReportAsync(int userId, int reportId, int companyId)
    {
        var report = await repository.GetReportAsync(reportId);
        if (report is null || !report.IsActive || report.CompanyId != companyId)
            return false;

        var belongs = await repository.UserBelongsToCompanyAsync(userId, companyId);
        if (!belongs) return false;

        return await repository.HasPermissionAsync(userId, companyId, report.ModuleId, reportId);
    }

    public async Task<bool> CanViewModuleAsync(int userId, int moduleId, int companyId)
    {
        var belongs = await repository.UserBelongsToCompanyAsync(userId, companyId);
        if (!belongs) return false;

        return await repository.HasModulePermissionAsync(userId, companyId, moduleId);
    }

    public async Task<bool> IsCompanyAdminAsync(int userId, int companyId)
        => await repository.IsCompanyAdminAsync(userId, companyId);

    public Task<bool> UserBelongsToCompanyAsync(int userId, int companyId)
        => repository.UserBelongsToCompanyAsync(userId, companyId);

    public async Task UpdatePermissionsBatchAsync(int companyId, int grantedBy, IEnumerable<PermissionUpdateDto> updates)
    {
        await repository.UpsertPermissionsAsync(companyId, grantedBy, updates);
    }

    public async Task ReplaceUserPermissionsAsync(int companyId, int targetUserId, int grantedBy, IEnumerable<PermissionUpdateDto> updates)
    {
        await repository.ReplaceUserPermissionsAsync(companyId, targetUserId, grantedBy, updates);
    }

    public async Task<IEnumerable<PermissionDto>> GetForCompanyAsync(int companyId)
    {
        var list = await repository.GetForCompanyAsync(companyId);
        return list.Select(p => new PermissionDto(p.Id, p.UserId, p.CompanyId, p.ModuleId, p.ReportId, p.CanView, p.GrantedAt));
    }
}

