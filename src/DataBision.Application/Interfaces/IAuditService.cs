using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        int? userId = null,
        int? companyId = null,
        string? resourceType = null,
        string? resourceId = null,
        object? metadata = null,
        string? ipAddress = null,
        string? userAgent = null);

    Task<IEnumerable<AuditLog>> GetPagedAsync(int? companyId, int page, int pageSize);
}
