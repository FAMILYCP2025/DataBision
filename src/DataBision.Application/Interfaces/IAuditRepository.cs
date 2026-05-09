using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IAuditRepository
{
    Task InsertAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetPagedAsync(int? companyId, int page, int pageSize);
}
