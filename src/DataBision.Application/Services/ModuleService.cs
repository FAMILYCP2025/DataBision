using DataBision.Application.DTOs.Modules;
using DataBision.Application.Interfaces;

namespace DataBision.Application.Services;

public class ModuleService(IModuleRepository repository, IPermissionService permissions) : IModuleService
{
    public async Task<IEnumerable<ModuleDto>> GetAccessibleAsync(int userId, int? companyId, bool isSuperAdmin)
    {
        var all = await repository.GetAllAsync();

        // SuperAdmin: all modules, no report count (no company context)
        if (isSuperAdmin)
            return all.Select(m => new ModuleDto(m.Id, m.Name, m.Slug, m.Icon, 0));

        if (!companyId.HasValue)
            return [];

        var isCompanyAdmin = await permissions.IsCompanyAdminAsync(userId, companyId.Value);

        var accessible = new List<ModuleDto>();
        foreach (var m in all)
        {
            bool canView = isCompanyAdmin
                || await permissions.CanViewModuleAsync(userId, m.Id, companyId.Value);

            if (canView)
            {
                var reportCount = await repository.GetActiveReportCountAsync(m.Id, companyId.Value);
                accessible.Add(new ModuleDto(m.Id, m.Name, m.Slug, m.Icon, reportCount));
            }
        }
        return accessible;
    }

    public async Task<(IEnumerable<ReportSummaryDto>? reports, string? error)> GetAccessibleReportsAsync(
        string moduleSlug, int userId, int companyId)
    {
        var module = await repository.GetBySlugAsync(moduleSlug);
        if (module is null) return (null, "module_not_found");

        var reports = await repository.GetActiveReportsAsync(module.Id, companyId);

        var isCompanyAdmin = await permissions.IsCompanyAdminAsync(userId, companyId);

        var accessible = new List<ReportSummaryDto>();
        foreach (var r in reports)
        {
            bool canView = isCompanyAdmin
                || await permissions.CanViewReportAsync(userId, r.Id, companyId);

            if (canView)
                accessible.Add(new ReportSummaryDto(
                    r.Id, r.Name, r.Description,
                    r.CreatedAt.ToString("yyyy-MM-dd"),
                    r.EmbedUrl));
        }
        return (accessible, null);
    }
}

