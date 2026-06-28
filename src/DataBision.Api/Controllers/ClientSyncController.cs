using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/sync")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientSyncController(
    ISyncStatusService sync,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/sync/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await sync.GetStatusAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/sync/objects
    [HttpGet("objects")]
    public async Task<IActionResult> GetObjects(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await sync.GetObjectsAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/sync/transform-status
    [HttpGet("transform-status")]
    public async Task<IActionResult> GetTransformStatus(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await sync.GetTransformStatusAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }
}
