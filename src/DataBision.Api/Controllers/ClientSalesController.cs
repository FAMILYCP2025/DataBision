using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/sales")]
[AllowAnonymous]
public sealed class ClientSalesController(
    ISalesService sales,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/sales/overview?dateFrom=2026-01-01&dateTo=2026-06-30
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null)
            return BadRequest(new { error = "invalid_date_from", message = "dateFrom must be a valid date (YYYY-MM-DD)." });
        if (to is null)
            return BadRequest(new { error = "invalid_date_to", message = "dateTo must be a valid date (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetOverviewAsync(companyId!, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/daily?dateFrom=...&dateTo=...
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null || to is null)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom and dateTo must be valid dates (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetDailyAsync(companyId!, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/monthly?dateFrom=...&dateTo=...
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var (companyId, err) = CompanyContextResolver.TryResolve(HttpContext, config);
        if (err is not null) return err;

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null || to is null)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom and dateTo must be valid dates (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetMonthlyAsync(companyId!, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/customers?limit=50&offset=0&sortBy=netSalesAmount&sortDir=desc
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int limit = 50,
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
        if (sortBy is not null && !ClientDashboardController.CustomerSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", ClientDashboardController.CustomerSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await sales.GetCustomersAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // GET /api/client/sales/items?limit=50&offset=0
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] int limit = 50,
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
        if (sortBy is not null && !ClientDashboardController.ItemSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", ClientDashboardController.ItemSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await sales.GetItemsAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // GET /api/client/sales/salespersons?limit=50&offset=0
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons(
        [FromQuery] int limit = 50,
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
        if (sortBy is not null && !ClientDashboardController.SalespersonSortFields.Contains(sortBy))
            return BadRequest(new { error = "invalid_sort_by", message = $"sortBy must be one of: {string.Join(", ", ClientDashboardController.SalespersonSortFields)}." });
        if (!IsValidSortDir(sortDir))
            return BadRequest(new { error = "invalid_sort_dir", message = "sortDir must be 'asc' or 'desc'." });

        var pagination = new PaginationOptions(limit, offset, sortBy, sortDir);
        var result = await sales.GetSalespersonsAsync(companyId!, pagination, ct);
        return Ok(new { data = result.Data, meta = result.Meta });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DateTime? from, DateTime? to) ParseDateRange(string? dateFrom, string? dateTo)
    {
        var (defaultFrom, defaultTo) = SalesService.DefaultDateRange();

        DateTime? from = string.IsNullOrWhiteSpace(dateFrom) ? defaultFrom
            : DateTime.TryParseExact(dateFrom, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var f)
                ? f : null;

        DateTime? to = string.IsNullOrWhiteSpace(dateTo) ? defaultTo
            : DateTime.TryParseExact(dateTo, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var t)
                ? t : null;

        return (from, to);
    }

    private static bool IsValidSortDir(string? sortDir) =>
        sortDir is null || sortDir == "asc" || sortDir == "desc";
}
