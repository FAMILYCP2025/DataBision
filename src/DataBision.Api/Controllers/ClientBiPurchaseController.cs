using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/purchase")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientBiPurchaseController(
    IPurchaseMartRepository? purchaseMart,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/purchase/mart/kpi
    [HttpGet("mart/kpi")]
    public async Task<IActionResult> GetMartKpi(CancellationToken ct)
    {
        if (purchaseMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        var result = await purchaseMart.GetKpiSummaryAsync(ctx.CompanyId!, ct);
        return result is null
            ? this.OkData(new { hasData = false })
            : this.OkData(result);
    }

    // GET /api/client/bi/purchase/mart/by-period?months=12
    [HttpGet("mart/by-period")]
    public async Task<IActionResult> GetMartByPeriod(
        [FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (purchaseMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (months < 1 || months > 36)
            return this.BadRequestError("invalid_months", "months must be between 1 and 36.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await purchaseMart.GetByPeriodAsync(ctx.CompanyId!, months, ct));
    }

    // GET /api/client/bi/purchase/mart/top-suppliers?limit=10
    [HttpGet("mart/top-suppliers")]
    public async Task<IActionResult> GetMartTopSuppliers(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (purchaseMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await purchaseMart.GetTopSuppliersAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/purchase/mart/top-items?limit=10
    [HttpGet("mart/top-items")]
    public async Task<IActionResult> GetMartTopItems(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (purchaseMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 100)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 100.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await purchaseMart.GetTopItemsAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/purchase/mart/open-orders?overdueOnly=false
    [HttpGet("mart/open-orders")]
    public async Task<IActionResult> GetMartOpenOrders(
        [FromQuery] bool overdueOnly = false, CancellationToken ct = default)
    {
        if (purchaseMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await purchaseMart.GetOpenOrdersAsync(ctx.CompanyId!, overdueOnly, ct));
    }
}
