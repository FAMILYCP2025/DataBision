using DataBision.Api.Security;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

// Security enforced by CompanyContextResolver in each action.
// [AllowAnonymous] lets ASP.NET populate User claims when JWT is present
// without blocking the request at the pipeline level — the resolver handles 401/403.
[ApiController]
[Route("api/client/dashboard")]
[AllowAnonymous]
public sealed class ClientDashboardController(
    IDashboardService dashboard,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var result = await dashboard.GetSummaryAsync(companyId!, ct);
        if (result is null)
            return Ok(new { data = (object?)null });

        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/sales-daily?days=30
    [HttpGet("sales-daily")]
    public async Task<IActionResult> GetSalesDaily(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        if (days < 1 || days > 365)
            return BadRequest(new { error = "invalid_days", message = "days must be between 1 and 365." });

        var result = await dashboard.GetSalesDailyAsync(companyId!, days, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/sales-monthly?months=12
    [HttpGet("sales-monthly")]
    public async Task<IActionResult> GetSalesMonthly(
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        if (months < 1 || months > 36)
            return BadRequest(new { error = "invalid_months", message = "months must be between 1 and 36." });

        var result = await dashboard.GetSalesMonthlyAsync(companyId!, months, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/dashboard/top-customers?limit=10&offset=0&sortBy=netSalesAmount&sortDir=desc
    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomers(
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });
        if (offset < 0)
            return BadRequest(new { error = "invalid_offset", message = "offset must be >= 0." });
        if (sortBy is not null && !CustomerSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", CustomerSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new DataBision.Application.DTOs.Dashboard.PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await dashboard.GetTopCustomersAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // GET /api/client/dashboard/top-items?limit=10&offset=0&sortBy=grossSalesAmount&sortDir=desc
    [HttpGet("top-items")]
    public async Task<IActionResult> GetTopItems(
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });
        if (offset < 0)
            return BadRequest(new { error = "invalid_offset", message = "offset must be >= 0." });
        if (sortBy is not null && !ItemSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", ItemSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new DataBision.Application.DTOs.Dashboard.PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await dashboard.GetTopItemsAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // GET /api/client/dashboard/salespersons?limit=20&offset=0&sortBy=netSalesAmount&sortDir=desc
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });
        if (offset < 0)
            return BadRequest(new { error = "invalid_offset", message = "offset must be >= 0." });
        if (sortBy is not null && !SalespersonSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", SalespersonSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new DataBision.Application.DTOs.Dashboard.PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await dashboard.GetSalespersonsAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // ── Sort allowlists ────────────────────────────────────────────────────────

    internal static readonly HashSet<string> CustomerSortFields =
        ["netSalesAmount", "salesAmount", "invoiceCount", "lastInvoiceDate", "cardCode"];

    internal static readonly HashSet<string> ItemSortFields =
        ["grossSalesAmount", "quantitySold", "invoiceCount", "itemCode"];

    internal static readonly HashSet<string> SalespersonSortFields =
        ["netSalesAmount", "salesAmount", "invoiceCount", "salesPersonCode"];

    private static bool IsValidSortDir(string? sortDir) =>
        sortDir is null || sortDir == "asc" || sortDir == "desc";
}
