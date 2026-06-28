using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DataBision.Infrastructure.Repositories;

/// <summary>
/// Executes a short smoke-test against SAP B1 Service Layer:
///   1. POST Login
///   2. GET ChartOfAccounts?$top=1
///   3. GET JournalEntries?$top=1  (optional — may not exist in all SL versions)
///   4. POST Logout
/// Never logs B1SESSION, password, or cookie values.
/// </summary>
public sealed class NativeBiSapConnectionTester(
    ISecretRefResolver secretRefResolver,
    ILogger<NativeBiSapConnectionTester> log) : INativeBiSapConnectionTester
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<TestNativeBiConnectionProfileResult> TestAsync(
        NativeBiConnectionProfile profile, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var checkedAt = DateTime.UtcNow;

        string password;
        try
        {
            password = secretRefResolver.Resolve(profile.SecretRef);
        }
        catch (Exception ex)
        {
            log.LogWarning("TestConnection: SecretRef resolution failed for profile {Id}: {Msg}", profile.Id, ex.Message);
            return Fail(sw, checkedAt, profile, $"SecretRef error: {ex.Message}");
        }

        if (profile.IgnoreSslErrors)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                log.LogError(
                    "SECURITY BLOCK: Profile {Id} has IgnoreSslErrors=true but ASPNETCORE_ENVIRONMENT=Production. " +
                    "TestConnection refused.", profile.Id);
                return Fail(sw, checkedAt, profile,
                    "Conexión rechazada: IgnoreSslErrors=true no está permitido en entornos Production. " +
                    "Desactive IgnoreSslErrors en el perfil o use un ambiente DEV/TST.");
            }
        }

        var handler = new HttpClientHandler { UseCookies = false };
        if (profile.IgnoreSslErrors)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(profile.ServiceLayerBaseUrl.TrimEnd('/') + "/"),
            Timeout     = TimeSpan.FromSeconds(profile.TimeoutSeconds)
        };
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        string? sessionCookie = null;
        bool loginOk          = false;
        bool coaOk            = false;
        bool jeOk             = false;

        // 1. Login
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                CompanyDB = profile.CompanyDb,
                UserName  = profile.SapUserName,
                Password  = password
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var loginResp = await http.PostAsync("Login", content, ct);
            if (!loginResp.IsSuccessStatusCode)
            {
                var body = await loginResp.Content.ReadAsStringAsync(ct);
                log.LogWarning("TestConnection: Login HTTP {Code} for profile {Id}", (int)loginResp.StatusCode, profile.Id);
                return Fail(sw, checkedAt, profile,
                    $"Login failed (HTTP {(int)loginResp.StatusCode}). Check CompanyDB, user, and password.");
            }

            // Extract B1SESSION from Set-Cookie
            if (loginResp.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var c in cookies)
                {
                    if (c.StartsWith("B1SESSION=", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionCookie = "B1SESSION=" + c.Split(';')[0]["B1SESSION=".Length..];
                        break;
                    }
                }
            }

            if (sessionCookie is null)
            {
                var authBody = await loginResp.Content.ReadAsStringAsync(ct);
                var authNode = JsonNode.Parse(authBody);
                var sessionId = authNode?["SessionId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(sessionId))
                    sessionCookie = $"B1SESSION={sessionId}";
            }

            loginOk = sessionCookie is not null;
            if (!loginOk)
                return Fail(sw, checkedAt, profile, "Login succeeded (HTTP 200) but no B1SESSION found in response.");
        }
        catch (TaskCanceledException)
        {
            return Fail(sw, checkedAt, profile, $"Login timed out after {profile.TimeoutSeconds}s.");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "TestConnection: Login exception for profile {Id}", profile.Id);
            return Fail(sw, checkedAt, profile, $"Login error: {ex.Message}");
        }

        // 2. Chart of Accounts
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "ChartOfAccounts?$top=1&$select=Code");
            req.Headers.Add("Cookie", sessionCookie);
            using var resp = await http.SendAsync(req, ct);
            coaOk = resp.IsSuccessStatusCode;
            if (!coaOk)
                log.LogWarning("TestConnection: ChartOfAccounts HTTP {Code} for profile {Id}", (int)resp.StatusCode, profile.Id);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "TestConnection: ChartOfAccounts exception for profile {Id}", profile.Id);
        }

        // 3. JournalEntries (optional — some SL versions don't expose this directly)
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "JournalEntries?$top=1&$select=JdtNum");
            req.Headers.Add("Cookie", sessionCookie);
            using var resp = await http.SendAsync(req, ct);
            jeOk = resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            log.LogDebug("TestConnection: JournalEntries not available for profile {Id}: {Msg}", profile.Id, ex.Message);
        }

        // 4. Logout (best-effort)
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "Logout");
            req.Headers.Add("Cookie", sessionCookie);
            await http.SendAsync(req, ct);
        }
        catch { /* logout is non-critical */ }

        sw.Stop();
        log.LogInformation("TestConnection: profile {Id} — login={Login} coa={Coa} je={Je} in {Ms}ms",
            profile.Id, loginOk, coaOk, jeOk, sw.ElapsedMilliseconds);

        var success = loginOk && coaOk;
        return new TestNativeBiConnectionProfileResult
        {
            Success                   = success,
            LatencyMs                 = sw.ElapsedMilliseconds,
            CheckedAt                 = checkedAt,
            ServiceLayerBaseUrlMasked = MaskUrl(profile.ServiceLayerBaseUrl),
            CompanyDb                 = profile.CompanyDb,
            Message                   = success
                ? $"Conexión exitosa en {sw.ElapsedMilliseconds}ms. JournalEntries: {(jeOk ? "OK" : "no disponible")}."
                : "Conexión establecida pero ChartOfAccounts falló. Verificar permisos del usuario SAP.",
            Capabilities = new TestCapabilities
            {
                LoginOk           = loginOk,
                ChartOfAccountsOk = coaOk,
                JournalEntriesOk  = jeOk
            }
        };
    }

    private static TestNativeBiConnectionProfileResult Fail(
        Stopwatch sw, DateTime checkedAt, NativeBiConnectionProfile profile, string message)
    {
        sw.Stop();
        return new TestNativeBiConnectionProfileResult
        {
            Success                   = false,
            LatencyMs                 = sw.ElapsedMilliseconds,
            CheckedAt                 = checkedAt,
            ServiceLayerBaseUrlMasked = MaskUrl(profile.ServiceLayerBaseUrl),
            CompanyDb                 = profile.CompanyDb,
            Message                   = message,
            Capabilities              = new TestCapabilities()
        };
    }

    private static string MaskUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "(not set)";
        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        catch { return "(invalid url)"; }
    }
}
