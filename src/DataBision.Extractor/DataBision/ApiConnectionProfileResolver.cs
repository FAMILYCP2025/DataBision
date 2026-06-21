using System.Text.Json;
using DataBision.Extractor.Options;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.DataBision;

/// <summary>
/// Fetches SAP Service Layer credentials from the DataBision API internal endpoint.
/// Called at startup when --profile or --profile-id is specified.
/// SECURITY: The resolved SAP password is held only in memory and never written to disk or logs.
/// </summary>
public sealed class ApiConnectionProfileResolver : IConnectionProfileResolver, IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiConnectionProfileResolver> _log;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiConnectionProfileResolver(DataBisionApiOptions options, ILogger<ApiConnectionProfileResolver> log)
    {
        _log = log;
        _http = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout     = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Add("X-DataBision-ApiKey", options.ApiKey);
    }

    public async Task<(SapServiceLayerOptions SlOptions, int FetchConcurrency)?> ResolveAsync(
        string analyticsCompanyId,
        string? profileName,
        int? profileId,
        CancellationToken ct = default)
    {
        var queryParams = $"companyId={Uri.EscapeDataString(analyticsCompanyId)}";
        if (!string.IsNullOrWhiteSpace(profileName))
            queryParams += $"&profileName={Uri.EscapeDataString(profileName)}";
        if (profileId.HasValue)
            queryParams += $"&profileId={profileId.Value}";

        var url = $"api/internal/native-bi/connection-profile/resolve?{queryParams}";
        _log.LogInformation("Resolving SAP credentials from API: company={Company} profile={Profile}",
            analyticsCompanyId, profileName ?? profileId?.ToString() ?? "(first active)");

        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                _log.LogError("Profile resolve failed: HTTP {Code} — {Body}", (int)response.StatusCode, errBody);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var doc  = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);
            if (!doc.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
            {
                _log.LogError("Profile resolve: unexpected response shape (no 'data' field).");
                return null;
            }

            var baseUrl      = data.TryGetProperty("serviceLayerBaseUrl", out var bu)  ? bu.GetString()   : null;
            var companyDb    = data.TryGetProperty("companyDb",           out var db)  ? db.GetString()   : null;
            var sapUser      = data.TryGetProperty("sapUserName",         out var u)   ? u.GetString()    : null;
            var sapPass      = data.TryGetProperty("sapPassword",         out var p)   ? p.GetString()    : null;
            var ignoreSsl    = data.TryGetProperty("ignoreSslErrors",     out var ssl) && ssl.GetBoolean();
            var timeout      = data.TryGetProperty("timeoutSeconds",      out var t)   ? t.GetInt32()     : 60;
            var fetchConc    = data.TryGetProperty("fetchConcurrency",    out var fc)  ? fc.GetInt32()    : 0;
            var profileIdR   = data.TryGetProperty("profileId",           out var pid) ? pid.GetInt32()   : 0;
            var profileNameR = data.TryGetProperty("profileName",         out var pn)  ? pn.GetString()   : null;

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(companyDb) ||
                string.IsNullOrWhiteSpace(sapUser)  || string.IsNullOrWhiteSpace(sapPass))
            {
                _log.LogError("Profile resolve: response missing required credential fields.");
                return null;
            }

            _log.LogInformation("Profile resolved: id={Id} name={Name} db={Db} concurrency={Conc}",
                profileIdR, profileNameR, companyDb, fetchConc);

            var slOptions = new SapServiceLayerOptions
            {
                BaseUrl                    = baseUrl,
                CompanyDB                  = companyDb,
                UserName                   = sapUser,
                Password                   = sapPass,
                IgnoreSslCertificateErrors = ignoreSsl,
                TimeoutSeconds             = timeout,
            };

            return (slOptions, fetchConc);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Profile resolve: request to DataBision API failed — {Message}", ex.Message);
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}
