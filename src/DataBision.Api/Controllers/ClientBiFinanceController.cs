using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/finance")]
[AllowAnonymous]
public sealed class ClientBiFinanceController(
    IProcessDashboardService svc,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/finance/executive?days=30
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutive(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (days < 1 || days > 365)
            return this.BadRequestError("invalid_days", "days must be between 1 and 365.");

        var result = await svc.GetFinanceExecutiveAsync(ctx.CompanyId!, days, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/ar-aging?limit=50&offset=0&sortBy=overdueAmount&sortDir=desc
    [HttpGet("ar-aging")]
    public async Task<IActionResult> GetArAging(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetFinanceArAgingAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset, sortBy, sortDir), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/finance/ap-aging?limit=50&offset=0
    [HttpGet("ap-aging")]
    public async Task<IActionResult> GetApAging(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetFinanceApAgingAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }
}
