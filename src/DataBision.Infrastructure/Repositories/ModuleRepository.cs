using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class ModuleRepository(AppDbContext db) : IModuleRepository
{
    public async Task<IEnumerable<Module>> GetAllAsync()
        => await db.Modules.OrderBy(m => m.SortOrder).ToListAsync();

    public Task<Module?> GetByIdAsync(int id)
        => db.Modules.FirstOrDefaultAsync(m => m.Id == id);

    public Task<Module?> GetBySlugAsync(string slug)
        => db.Modules.FirstOrDefaultAsync(m => m.Slug == slug);

    public async Task<IEnumerable<Report>> GetActiveReportsAsync(int moduleId, int companyId)
        => await db.Reports
            .Where(r => r.ModuleId == moduleId && r.CompanyId == companyId && r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();

    public Task<int> GetActiveReportCountAsync(int moduleId, int companyId)
        => db.Reports.CountAsync(r => r.ModuleId == moduleId && r.CompanyId == companyId && r.IsActive);

    public async Task<Dictionary<string, int>> GetModuleSlugMapAsync()
        => await db.Modules.ToDictionaryAsync(m => m.Slug, m => m.Id);
}
