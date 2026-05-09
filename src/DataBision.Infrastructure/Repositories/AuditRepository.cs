using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class AuditRepository(AppDbContext db) : IAuditRepository
{
    public async Task InsertAsync(AuditLog log)
    {
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetPagedAsync(int? companyId, int page, int pageSize)
        => await db.AuditLogs
            .Where(l => companyId == null || l.CompanyId == companyId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
}
