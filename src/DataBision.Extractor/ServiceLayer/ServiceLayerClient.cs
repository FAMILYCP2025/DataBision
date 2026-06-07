using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataBision.Extractor.Options;
using DataBision.Extractor.Resilience;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.ServiceLayer;

public sealed class ServiceLayerClient : IServiceLayerClient, IDisposable
{
    private readonly SapServiceLayerOptions _options;
    private readonly ILogger<ServiceLayerClient> _log;
    private readonly HttpClient _http;
    private ServiceLayerSession? _session;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsLoggedIn => _session is not null && !_session.IsExpired;

    public ServiceLayerClient(SapServiceLayerOptions options, ILogger<ServiceLayerClient> log)
    {
        _options = options;
        _log = log;

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = false // we manage cookies manually via headers
        };

        if (options.IgnoreSslCertificateErrors)
        {
            // Lambda is used instead of DangerousAcceptAnyServerCertificateValidator
            // to ensure consistent behavior across all .NET 8 runtime environments.
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
        _http.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task LoginAsync(CancellationToken ct = default)
        => RetryHelper.ExecuteAsync(LoginOnceAsync, "SL.Login", _log, ct);

    private async Task LoginOnceAsync(CancellationToken ct)
    {
        _log.LogInformation("Service Layer login — CompanyDB: {CompanyDB}, User: {User}",
            _options.CompanyDB, _options.UserName);

        // SAP B1 Service Layer rejects "application/json; charset=utf-8".
        // Must send Content-Type: application/json with no charset parameter.
        var payloadJson = JsonSerializer.Serialize(new
        {
            CompanyDB = _options.CompanyDB,
            UserName  = _options.UserName,
            Password  = _options.Password
        });
        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _http.PostAsync("Login", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Service Layer login failed. HTTP {(int)response.StatusCode}: {body}");
        }

        var auth = await response.Content.ReadFromJsonAsync<ServiceLayerAuthResponse>(JsonOpts, ct)
                   ?? throw new InvalidOperationException("Login response was empty.");

        // Extract B1SESSION cookie from Set-Cookie header
        var sessionId = auth.SessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            // Fall back: try to read from Set-Cookie header
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var c in cookies)
                {
                    if (c.StartsWith("B1SESSION=", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionId = c.Split(';')[0]["B1SESSION=".Length..];
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new InvalidOperationException("Login succeeded but no SessionId found in response.");

        var timeout = auth.SessionTimeout > 0 ? auth.SessionTimeout : 30;
        _session = new ServiceLayerSession(sessionId, timeout);

        _log.LogInformation("Login successful. SessionTimeout: {Timeout} min. SL Version: {Version}",
            _session.TimeoutMinutes, auth.Version);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (_session is null) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "Logout");
            request.Headers.Add("Cookie", _session.Cookie);
            using var response = await _http.SendAsync(request, ct);
            _log.LogInformation("Logout completed. HTTP {Status}", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Logout failed (non-critical): {Message}", ex.Message);
        }
        finally
        {
            _session = null;
        }
    }

    public Task<JsonArray> GetAsync(string entity, string query, CancellationToken ct = default)
        => RetryHelper.ExecuteAsync(c => GetOnceAsync(entity, query, c), $"SL.Get({entity})", _log, ct);

    private async Task<JsonArray> GetOnceAsync(string entity, string query, CancellationToken ct)
    {
        if (_session is null || _session.IsExpired)
            throw new InvalidOperationException("Not logged in. Call LoginAsync first.");

        if (_session.IsNearExpiry)
        {
            _log.LogInformation("Session near expiry — renewing before request.");
            await LoginAsync(ct);
        }

        var url = string.IsNullOrWhiteSpace(query)
            ? entity
            : $"{entity}?{query.TrimStart('?')}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", _session.Cookie);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var response = await _http.SendAsync(request, ct);
        sw.Stop();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _log.LogWarning("HTTP 401 — session expired. Re-logging in and retrying.");
            _session = null;
            await LoginAsync(ct);
            return await GetAsync(entity, query, ct); // retry once
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"GET {entity} failed. HTTP {(int)response.StatusCode}: {body}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        _log.LogDebug("GET {Entity} completed in {Ms} ms", entity, sw.ElapsedMilliseconds);

        var doc = JsonNode.Parse(json);
        if (doc is JsonObject obj && obj["value"] is JsonArray arr)
            return arr;

        // Some SL versions return a direct array
        if (doc is JsonArray directArr)
            return directArr;

        _log.LogWarning("Unexpected response structure from {Entity}. Raw: {Json}", entity,
            json.Length > 500 ? json[..500] + "..." : json);
        return [];
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
