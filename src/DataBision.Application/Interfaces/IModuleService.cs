using DataBision.Application.DTOs.Modules;

namespace DataBision.Application.Interfaces;

public interface IModuleService
{
    Task<IEnumerable<ModuleDto>> GetAccessibleAsync(int userId, int? companyId, bool isSuperAdmin);
    Task<(IEnumerable<ReportSummaryDto>? reports, string? error)> GetAccessibleReportsAsync(
        string moduleSlug, int userId, int companyId);
}
