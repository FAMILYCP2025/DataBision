using DataBision.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DataBision.Application.Services;

/// <summary>
/// Resolves analytics company_id from the local DB first (Company.AnalyticsCompanyId),
/// then falls back to appsettings NativeBi:CompanySlugMap in Development environments.
/// Throws if no mapping is found in production.
/// </summary>
public sealed class AnalyticsCompanyResolver(
    ICompanyRepository companyRepository,
    IConfiguration config,
    IHostEnvironment env) : IAnalyticsCompanyResolver
{
    public async Task<string> ResolveAsync(string companyIdentifier, CancellationToken ct = default)
    {
        // 1. DB lookup — primary path for all environments
        var company = await companyRepository.GetBySlugAsync(companyIdentifier, ct);
        if (company?.AnalyticsCompanyId is { Length: > 0 } dbId)
            return dbId;

        // 2. Appsettings fallback — only in Development
        if (env.IsDevelopment())
        {
            var section = config.GetSection("NativeBi:CompanySlugMap");
            foreach (var child in section.GetChildren())
            {
                if (string.Equals(child.Key, companyIdentifier, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(child.Value))
                    return child.Value;
            }

            var defaultId = config["NativeBi:DefaultAnalyticsCompanyId"];
            if (!string.IsNullOrWhiteSpace(defaultId))
                return defaultId;
        }

        // 3. No mapping found
        throw new InvalidOperationException(
            $"analytics_company_id no configurado para la empresa '{companyIdentifier}'. " +
            "Configure el campo Analytics Company ID en el panel de administración.");
    }
}
