using System.Text.Json;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;

namespace DataBision.Application.Services;

public class AuditService(IAuditRepository repository) : IAuditService
{
    public Task<IEnumerable<AuditLog>> GetPagedAsync(int? companyId, int page, int pageSize)
        => repository.GetPagedAsync(companyId, page, pageSize);

    public async Task LogAsync(
        string action,
        int? userId = null,
        int? companyId = null,
        string? resourceType = null,
        string? resourceId = null,
        object? metadata = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        await repository.InsertAsync(new AuditLog
        {
            Action = action,
            UserId = userId,
            CompanyId = companyId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
    }
}
