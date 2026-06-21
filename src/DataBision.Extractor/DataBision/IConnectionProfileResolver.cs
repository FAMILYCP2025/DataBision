using DataBision.Extractor.Options;

namespace DataBision.Extractor.DataBision;

public interface IConnectionProfileResolver
{
    /// <summary>
    /// Fetches SAP SL credentials from the DataBision API internal endpoint.
    /// Returns the resolved SL options and the profile's FetchConcurrency setting.
    /// Returns null if the resolution fails (caller should abort startup).
    /// </summary>
    Task<(SapServiceLayerOptions SlOptions, int FetchConcurrency)?> ResolveAsync(
        string analyticsCompanyId,
        string? profileName,
        int? profileId,
        CancellationToken ct = default);
}
