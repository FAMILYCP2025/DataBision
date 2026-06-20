using System.Text.Json.Nodes;

namespace DataBision.Extractor.ServiceLayer;

public interface IServiceLayerClient
{
    Task LoginAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<JsonArray> GetAsync(string entity, string query, CancellationToken ct = default);

    /// <summary>Fetches a single page and returns rows + optional @odata.nextLink. No client-level retry.</summary>
    Task<ServiceLayerPage> GetPageAsync(string entity, string query, CancellationToken ct = default);

    /// <summary>
    /// Fetches a single entity by key (e.g. "JournalEntries(8)") and returns it as a JsonObject.
    /// Returns null on HTTP error or unexpected response format.
    /// </summary>
    Task<JsonObject?> GetObjectAsync(string entityWithKey, CancellationToken ct = default);

    /// <summary>
    /// Fetches raw response body for non-JSON endpoints (e.g. "$metadata" which returns EDMX XML).
    /// </summary>
    Task<string> GetRawStringAsync(string path, CancellationToken ct = default);

    bool IsLoggedIn { get; }
}
