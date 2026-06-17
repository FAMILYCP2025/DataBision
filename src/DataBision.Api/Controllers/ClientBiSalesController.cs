using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/sales")]
[AllowAnonymous]
public sealed class ClientBiSalesController(
    IProcessDashboardService svc,
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
}
