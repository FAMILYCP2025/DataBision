using DataBision.Api.Filters;
using DataBision.Application.Interfaces.Ingest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
public sealed class IngestCheckpointController(IIngestCheckpointRepository checkpointRepo) : ControllerBase
{
    /// <summary>
    /// GET /api/ingest/checkpoint/{companyId}/{sourceObject}
    /// The companyId in the URL must match the API key's companyId; mismatch returns 403.
    /// </summary>
    [HttpGet("checkpoint/{companyId}/{sourceObject}")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<IActionResult> GetCheckpoint(string companyId, string sourceObject, CancellationToken ct)
    {
        var tenantId = HttpContext.Items[ApiKeyAuthFilter.TenantIdItemKey] as string ?? string.Empty;
        var apiKeyCompanyId = HttpContext.Items[ApiKeyAuthFilter.CompanyIdItemKey] as string ?? string.Empty;

        if (!string.Equals(companyId, apiKeyCompanyId, StringComparison.Ordinal))
        {
            return StatusCode(403, new
            {
                error = "company_id_mismatch",
                message = "URL companyId does not match the company identity bound to the API key."
            });
        }

        var checkpoint = await checkpointRepo.GetAsync(tenantId, companyId, sourceObject, ct);

        return Ok(new { data = checkpoint });
    }
}
