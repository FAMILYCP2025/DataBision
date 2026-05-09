using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IModuleRepository
{
    Task<IEnumerable<Module>> GetAllAsync();
    Task<Module?> GetByIdAsync(int id);
    Task<Module?> GetBySlugAsync(string slug);
    Task<IEnumerable<Report>> GetActiveReportsAsync(int moduleId, int companyId);
    Task<int> GetActiveReportCountAsync(int moduleId, int companyId);
    Task<Dictionary<string, int>> GetModuleSlugMapAsync();
}
