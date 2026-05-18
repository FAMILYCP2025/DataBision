using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class ReportRepository(AppDbContext db) : IReportRepository
{
    public async Task<IEnumerable<Report>> GetReportsAsync(int companyId, int moduleId)
    {
        return await db.Reports
            .Where(r => r.CompanyId == companyId && r.ModuleId == moduleId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();
    }

    public async Task<Report?> GetReportAsync(int reportId, int companyId)
    {
        return await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId && r.CompanyId == companyId);
    }

    public async Task<Report> AddAsync(Report report)
    {
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    public async Task UpdateAsync(Report report)
    {
        report.UpdatedAt = DateTime.UtcNow;
        db.Reports.Update(report);
        await db.SaveChangesAsync();
    }
}
