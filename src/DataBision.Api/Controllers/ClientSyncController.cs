using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/sync")]
[AllowAnonymous]
public sealed class ClientSyncController(
    ISyncStatusService sync,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/sync/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await sync.GetStatusAsync(companyId!, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sync/objects
    [HttpGet("objects")]
    public async Task<IActionResult> GetObjects(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await sync.GetObjectsAsync(companyId!, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sync/transform-status
    [HttpGet("transform-status")]
    public async Task<IActionResult> GetTransformStatus(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await sync.GetTransformStatusAsync(companyId!, ct);
        return Ok(new { data = result });
    }
}
