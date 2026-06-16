namespace DataBision.Application.Interfaces;

/// <summary>
/// Maps a tenant company identifier (slug from JWT or numeric app ID) to the analytics
/// company_id stored in the MART/staging database.
///
/// DEV/demo: reads mapping from NativeBi:CompanySlugMap config.
/// Production path: replace with a DB-backed lookup on Company.AnalyticsCompanyId
/// (a column to be added to the companies table in a future migration).
/// </summary>
public interface IAnalyticsCompanyResolver
{
    string Resolve(string companyIdentifier);
}
