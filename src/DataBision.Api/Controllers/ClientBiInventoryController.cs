using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/inventory")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientBiInventoryController(
    IProcessDashboardService svc,
    IInventoryMartRepository? inventoryMart,
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

    // ── Sprint 5 — Inventory MART endpoints ──────────────────────────────────

    // GET /api/client/bi/inventory/mart/kpi
    [HttpGet("mart/kpi")]
    public async Task<IActionResult> GetMartKpi(CancellationToken ct)
    {
        if (inventoryMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        var result = await inventoryMart.GetKpiSummaryAsync(ctx.CompanyId!, ct);
        return result is null
            ? this.OkData(new { hasData = false })
            : this.OkData(result);
    }

    // GET /api/client/bi/inventory/mart/snapshot?limit=50
    [HttpGet("mart/snapshot")]
    public async Task<IActionResult> GetMartSnapshot(
        [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (inventoryMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 500)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 500.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await inventoryMart.GetSnapshotAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/inventory/mart/movement?months=12
    [HttpGet("mart/movement")]
    public async Task<IActionResult> GetMartMovement(
        [FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (inventoryMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (months < 1 || months > 36)
            return this.BadRequestError("invalid_months", "months must be between 1 and 36.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await inventoryMart.GetMovementByPeriodAsync(ctx.CompanyId!, months, ct));
    }

    // GET /api/client/bi/inventory/mart/slow-moving?minDays=90
    [HttpGet("mart/slow-moving")]
    public async Task<IActionResult> GetMartSlowMoving(
        [FromQuery] int minDays = 90, CancellationToken ct = default)
    {
        if (inventoryMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (minDays < 1 || minDays > 365)
            return this.BadRequestError("invalid_min_days", "minDays must be between 1 and 365.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await inventoryMart.GetSlowMovingItemsAsync(ctx.CompanyId!, minDays, ct));
    }

    // GET /api/client/bi/inventory/mart/warehouses-mart
    [HttpGet("mart/warehouses")]
    public async Task<IActionResult> GetMartWarehouses(CancellationToken ct)
    {
        if (inventoryMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await inventoryMart.GetWarehouseStockAsync(ctx.CompanyId!, ct));
    }
}
