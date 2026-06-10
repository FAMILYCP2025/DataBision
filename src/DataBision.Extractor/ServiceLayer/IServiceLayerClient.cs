using System.Text.Json.Nodes;

namespace DataBision.Extractor.ServiceLayer;

public interface IServiceLayerClient
{
    Task LoginAsync(CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<JsonArray> GetAsync(string entity, string query, CancellationToken ct = default);

    /// <summary>Fetches a single page and returns rows + optional @odata.nextLink. No client-level retry.</summary>
    Task<ServiceLayerPage> GetPageAsync(string entity, string query, CancellationToken ct = default);

    bool IsLoggedIn { get; }
}
