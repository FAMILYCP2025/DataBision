using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

/// <summary>
/// Returns distinct filter option values from the MART database for use in client-side filter selects.
/// Each endpoint is resilient: if the underlying MART table/column doesn't exist yet it returns an empty array.
/// </summary>
[ApiController]
[Route("api/client/bi/filters")]
[AllowAnonymous]
public sealed class ClientBiFilterOptionsController(
    IFilterOptionsService svc,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/filters/item-groups
    [HttpGet("item-groups")]
    public async Task<IActionResult> GetItemGroups(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await svc.GetItemGroupsAsync(ctx.CompanyId!, ct));
    }

    // GET /api/client/bi/filters/customer-groups
    [HttpGet("customer-groups")]
    public async Task<IActionResult> GetCustomerGroups(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await svc.GetCustomerGroupsAsync(ctx.CompanyId!, ct));
    }

    // GET /api/client/bi/filters/supplier-groups
    [HttpGet("supplier-groups")]
    public async Task<IActionResult> GetSupplierGroups(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await svc.GetSupplierGroupsAsync(ctx.CompanyId!, ct));
    }

    // GET /api/client/bi/filters/warehouses
    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await svc.GetWarehousesAsync(ctx.CompanyId!, ct));
    }

    // GET /api/client/bi/filters/salespersons
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await svc.GetSalespersonsAsync(ctx.CompanyId!, ct));
    }
}
