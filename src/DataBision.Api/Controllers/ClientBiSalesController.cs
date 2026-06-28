using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/sales")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientBiSalesController(
    IProcessDashboardService svc,
    ISalesMartRepository? salesMart,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/sales/customers-dashboard?limit=50&offset=0&sortBy=netSales&sortDir=desc
    [HttpGet("customers-dashboard")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] NativeBiFilterDto? filters = null,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetSalesCustomersAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset, sortBy, sortDir), filters, ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/sales/items-dashboard?limit=50&offset=0&sortBy=grossSales&sortDir=desc
    [HttpGet("items-dashboard")]
    public async Task<IActionResult> GetItems(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        [FromQuery] NativeBiFilterDto? filters = null,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetSalesItemsAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset, sortBy, sortDir), filters, ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/sales/fulfillment?days=30
    [HttpGet("fulfillment")]
    public async Task<IActionResult> GetFulfillment(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (days < 1 || days > 365)
            return this.BadRequestError("invalid_days", "days must be between 1 and 365.");

        var result = await svc.GetSalesFulfillmentAsync(ctx.CompanyId!, days, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/sales/item-groups
    [HttpGet("item-groups")]
    public async Task<IActionResult> GetItemGroupSummary(
        [FromQuery] NativeBiFilterDto filters,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        return this.OkData(await svc.GetSalesItemGroupSummaryAsync(ctx.CompanyId!, filters, ct));
    }

    // GET /api/client/bi/sales/warehouses
    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouseSummary(
        [FromQuery] NativeBiFilterDto? filters,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var data = await svc.GetSalesWarehouseSummaryAsync(ctx.CompanyId!, filters, ct);
        return this.OkData(data);
    }

    // ── Sprint 3 — MART endpoints (mart.sales_period_kpi, top_customers, etc.) ──

    // GET /api/client/bi/sales/mart/kpi
    [HttpGet("mart/kpi")]
    public async Task<IActionResult> GetMartKpi(CancellationToken ct)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        var result = await salesMart.GetKpiSummaryAsync(ctx.CompanyId!, ct);
        return result is null
            ? this.OkData(new { hasData = false })
            : this.OkData(result);
    }

    // GET /api/client/bi/sales/mart/by-period?months=12
    [HttpGet("mart/by-period")]
    public async Task<IActionResult> GetMartByPeriod(
        [FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (months < 1 || months > 36)
            return this.BadRequestError("invalid_months", "months must be between 1 and 36.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await salesMart.GetByPeriodAsync(ctx.CompanyId!, months, ct));
    }

    // GET /api/client/bi/sales/mart/top-customers?limit=10
    [HttpGet("mart/top-customers")]
    public async Task<IActionResult> GetMartTopCustomers(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await salesMart.GetTopCustomersAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/sales/mart/top-items?limit=10
    [HttpGet("mart/top-items")]
    public async Task<IActionResult> GetMartTopItems(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await salesMart.GetTopItemsAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/sales/mart/top-salespersons
    [HttpGet("mart/top-salespersons")]
    public async Task<IActionResult> GetMartTopSalespersons(CancellationToken ct)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await salesMart.GetTopSalespersonsAsync(ctx.CompanyId!, ct));
    }

    // GET /api/client/bi/sales/mart/open-orders?overdueOnly=false
    [HttpGet("mart/open-orders")]
    public async Task<IActionResult> GetMartOpenOrders(
        [FromQuery] bool overdueOnly = false, CancellationToken ct = default)
    {
        if (salesMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await salesMart.GetOpenOrdersAsync(ctx.CompanyId!, overdueOnly, ct));
    }
}
