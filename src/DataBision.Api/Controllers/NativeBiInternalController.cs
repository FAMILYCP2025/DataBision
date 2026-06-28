using DataBision.Api.Filters;
using DataBision.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

/// <summary>
/// Internal endpoints consumed by the DataBision Extractor process.
/// Authentication: X-DataBision-ApiKey header (ApiKeyAuthFilter).
/// These endpoints are NOT for the Admin UI or portal — they serve machine-to-machine calls only.
/// SECURITY: ResolveConnectionProfile returns the decrypted SAP password.
///           Endpoint must be served over HTTPS only. The response must never be logged.
/// </summary>
[ApiController]
[Route("api/internal/native-bi")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public sealed class NativeBiInternalController(
    INativeBiConnectionProfileService profiles,
    ILogger<NativeBiInternalController> log) : ControllerBase
{
    /// <summary>
    /// Resolves SAP Service Layer credentials for a connection profile.
    /// The companyId query param must match the analytics company ID bound to the API key.
    /// Either profileName or profileId must be provided.
    /// </summary>
    [HttpGet("connection-profile/resolve")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResolveConnectionProfile(
        [FromQuery] string? companyId,
        [FromQuery] string? profileName,
        [FromQuery] int? profileId,
        CancellationToken ct)
    {
        var isProduction = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .Equals("Production", StringComparison.OrdinalIgnoreCase);
        if (isProduction && !HttpContext.Request.IsHttps)
            return StatusCode(403, new { error = "https_required", message = "SAP credentials can only be resolved over HTTPS." });

        var keyCompanyId = HttpContext.Items[ApiKeyAuthFilter.CompanyIdItemKey] as string;

        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "company_id_required", message = "companyId query parameter is required." });

        if (!string.Equals(companyId, keyCompanyId, StringComparison.Ordinal))
            return StatusCode(403, new { error = "company_mismatch", message = "companyId does not match the identity bound to the API key." });

        if (profileId is null && string.IsNullOrWhiteSpace(profileName))
            return BadRequest(new { error = "profile_required", message = "Either profileName or profileId query parameter is required." });

        log.LogInformation("Extractor resolved profile: company={Company} profileName={Name} profileId={Id}",
            companyId, profileName, profileId);

        var (result, error) = await profiles.ResolveForExtractorAsync(companyId, profileName, profileId, ct);

        return error switch
        {
            "company_not_found"        => NotFound(new { error, message = "Analytics company not found in AppDB. Ensure AnalyticsCompanyId is set on the Company." }),
            "profile_not_found"        => NotFound(new { error, message = "Connection profile not found." }),
            "profile_inactive"         => BadRequest(new { error, message = "The connection profile is not active." }),
            "secret_resolution_failed" => StatusCode(500, new { error, message = "Failed to resolve SAP credentials. Check SecretRef configuration on the API server." }),
            not null                   => StatusCode(500, new { error, message = "An unexpected error occurred resolving the profile." }),
            _                          => Ok(new { data = result })
        };
    }
}
