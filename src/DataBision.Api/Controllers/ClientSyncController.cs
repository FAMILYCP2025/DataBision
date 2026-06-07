using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/sync")]
[AllowAnonymous] // TODO Sprint-6E: enforce JWT company_id claim validation
public sealed class ClientSyncController(ISyncStatusService sync) : ControllerBase
{
    // GET /api/client/sync/status?companyId=company-dev-001
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromQuery] string companyId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var result = await sync.GetStatusAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sync/objects?companyId=company-dev-001
    [HttpGet("objects")]
    public async Task<IActionResult> GetObjects(
        [FromQuery] string companyId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var result = await sync.GetObjectsAsync(companyId, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sync/transform-status?companyId=company-dev-001
    [HttpGet("transform-status")]
    public async Task<IActionResult> GetTransformStatus(
        [FromQuery] string companyId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var result = await sync.GetTransformStatusAsync(companyId, ct);
        return Ok(new { data = result });
    }
}
