using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/purchasing")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientBiPurchasingController(
    IProcessDashboardService svc,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/purchasing/executive?days=30
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutive(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (days < 1 || days > 365)
            return this.BadRequestError("invalid_days", "days must be between 1 and 365.");

        var result = await svc.GetPurchasingExecutiveAsync(ctx.CompanyId!, days, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/purchasing/suppliers?limit=50&offset=0
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetPurchasingSuppliersAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/purchasing/receiving?limit=50&offset=0
    [HttpGet("receiving")]
    public async Task<IActionResult> GetReceiving(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetPurchasingReceivingAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }
}
