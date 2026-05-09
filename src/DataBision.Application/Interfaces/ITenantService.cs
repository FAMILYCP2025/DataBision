using DataBision.Application.DTOs.Tenant;
using DataBision.Domain.Entities;

namespace DataBision.Application.Interfaces;

public interface ITenantService
{
    Task<TenantConfigDto?> GetConfigBySlugAsync(string slug);
    Task<Company?> GetCompanyBySlugAsync(string slug);
}
