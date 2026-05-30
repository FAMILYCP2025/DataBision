using DataBision.Application.DTOs.Ingest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBision.Api.Filters;

/// <summary>
/// Validates X-DataBision-ApiKey header for ingest endpoints.
/// Config key format: <c>Ingest:ApiKeys:{key}</c> = <c>"{tenantId}:{companyId}"</c>.
///
/// Behaviour:
/// • 401 when the header is missing/empty/not registered.
/// • Resolves TenantId/CompanyId from the key and stores them in <see cref="HttpContext.Items"/>.
/// • If the request body implements <see cref="IIngestBatchRequest"/>:
///     – body has both TenantId AND CompanyId populated → must match the key, otherwise 403.
///     – body has both empty → filled from the key (DEV/MVP convenience).
///     – partial population → 403 (don't paper over extractor mis-configuration).
/// </summary>
public sealed class ApiKeyAuthFilter(IConfiguration configuration) : IAsyncActionFilter
{
    public const string HeaderName = "X-DataBision-ApiKey";
    public const string TenantIdItemKey = "IngestTenantId";
    public const string CompanyIdItemKey = "IngestCompanyId";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Header present?
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            context.Result = Unauthorized("missing_api_key", $"{HeaderName} header is required.");
            return;
        }

        var providedKey = keyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            context.Result = Unauthorized("missing_api_key", $"{HeaderName} header is required.");
            return;
        }

        // 2. Key registered?
        var identity = configuration[$"Ingest:ApiKeys:{providedKey}"];
        if (string.IsNullOrWhiteSpace(identity))
        {
            context.Result = Unauthorized("invalid_api_key", "The provided API key is not valid.");
            return;
        }

        // 3. Key value well-formed?
        var parts = identity.Split(':', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            // Surface as 500 — this is an operator misconfiguration, not a caller error.
            context.Result = new ObjectResult(new
            {
                error = "api_key_misconfigured",
                message = "The API key entry is not in 'tenantId:companyId' format."
            })
            { StatusCode = 500 };
            return;
        }

        var keyTenantId = parts[0];
        var keyCompanyId = parts[1];

        context.HttpContext.Items[TenantIdItemKey] = keyTenantId;
        context.HttpContext.Items[CompanyIdItemKey] = keyCompanyId;

        // 4. Validate batch body against the key identity (if applicable).
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is not IIngestBatchRequest batch) continue;

            var hasTenant = !string.IsNullOrWhiteSpace(batch.TenantId);
            var hasCompany = !string.IsNullOrWhiteSpace(batch.CompanyId);

            if (!hasTenant && !hasCompany)
            {
                // DEV/MVP: empty body — fill from the API key.
                batch.TenantId = keyTenantId;
                batch.CompanyId = keyCompanyId;
                continue;
            }

            if (hasTenant != hasCompany)
            {
                context.Result = Forbidden(
                    "tenant_company_partial",
                    "Body must provide both TenantId and CompanyId, or neither.");
                return;
            }

            if (!string.Equals(batch.TenantId, keyTenantId, StringComparison.Ordinal)
                || !string.Equals(batch.CompanyId, keyCompanyId, StringComparison.Ordinal))
            {
                context.Result = Forbidden(
                    "tenant_company_mismatch",
                    "TenantId/CompanyId in body do not match the identity bound to the API key.");
                return;
            }
        }

        await next();
    }

    private static UnauthorizedObjectResult Unauthorized(string error, string message) =>
        new(new { error, message });

    private static ObjectResult Forbidden(string error, string message) =>
        new(new { error, message }) { StatusCode = 403 };
}
