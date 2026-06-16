using DataBision.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DataBision.Application.Services;

/// <summary>
/// DEV/demo implementation that resolves analytics company_id from appsettings.
/// NativeBi:CompanySlugMap maps app slug → staging company_id.
/// NativeBi:DefaultAnalyticsCompanyId is the fallback when no mapping exists.
/// If neither is configured, the original identifier passes through unchanged.
///
/// PRODUCTION NOTE: This class should be replaced by a DB lookup:
///   var company = await companyRepo.FindBySlugAsync(identifier);
///   return company?.AnalyticsCompanyId ?? identifier;
/// </summary>
public sealed class AnalyticsCompanyResolver(IConfiguration config) : IAnalyticsCompanyResolver
{
    public string Resolve(string companyIdentifier)
    {
        var section = config.GetSection("NativeBi:CompanySlugMap");
        foreach (var child in section.GetChildren())
        {
            if (string.Equals(child.Key, companyIdentifier, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(child.Value))
                return child.Value;
        }

        var defaultId = config["NativeBi:DefaultAnalyticsCompanyId"];
        return !string.IsNullOrWhiteSpace(defaultId) ? defaultId : companyIdentifier;
    }
}
