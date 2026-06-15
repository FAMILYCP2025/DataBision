using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/inventory")]
[AllowAnonymous]
public sealed class ClientBiInventoryController(
    IProcessDashboardService svc,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/inventory/rotation?limit=50&offset=0&sortBy=qtySold30d&sortDir=desc
    [HttpGet("rotation")]
    public async Task<IActionResult> GetRotation(
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

        var result = await svc.GetInventoryRotationAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset, sortBy, sortDir), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/inventory/stock?limit=50&offset=0
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetInventoryStockAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/inventory/warehouses
    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetInventoryWarehousesAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }
}
