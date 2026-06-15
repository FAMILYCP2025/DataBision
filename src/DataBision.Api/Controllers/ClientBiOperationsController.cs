using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/operations")]
[AllowAnonymous]
public sealed class ClientBiOperationsController(
    IProcessDashboardService svc,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/operations/pipeline-health
    [HttpGet("pipeline-health")]
    public async Task<IActionResult> GetPipelineHealth(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetPipelineHealthAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/operations/alerts?limit=20&offset=0
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");

        var result = await svc.GetActiveAlertsAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/operations/data-quality?limit=20&offset=0
    [HttpGet("data-quality")]
    public async Task<IActionResult> GetDataQuality(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");

        var result = await svc.GetDataQualityIssuesAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }
}
