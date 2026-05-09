using DataBision.Application.DTOs.Tenant;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;

namespace DataBision.Application.Services;

public class TenantService(ITenantRepository repository) : ITenantService
{
    public async Task<TenantConfigDto?> GetConfigBySlugAsync(string slug)
    {
        var company = await repository.GetCompanyWithBrandingAsync(slug);
        if (company is null) return null;

        var b = company.Branding;
        return new TenantConfigDto(
            b?.CompanyDisplayName ?? company.Name,
            b?.LogoUrl,
            b?.FaviconUrl,
            b?.PrimaryColor ?? "#2563EB",
            b?.SecondaryColor ?? "#64748B",
            b?.AccentColor ?? "#0EA5E9",
            b?.BackgroundColor ?? "#F8FAFC",
            b?.SidebarColor ?? "#0F172A");
    }

    public async Task<Company?> GetCompanyBySlugAsync(string slug)
        => await repository.GetCompanyWithBrandingAsync(slug);
}
