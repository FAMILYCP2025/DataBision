namespace DataBision.Application.Interfaces;

/// <summary>
/// Maps a tenant company identifier (slug from JWT or numeric app ID) to the analytics
/// company_id stored in the MART/staging database.
///
/// Production: looks up Company.AnalyticsCompanyId in Azure SQL by slug.
/// Dev fallback: reads mapping from NativeBi:CompanySlugMap config.
/// </summary>
public interface IAnalyticsCompanyResolver
{
    Task<string> ResolveAsync(string companyIdentifier, CancellationToken ct = default);
}
