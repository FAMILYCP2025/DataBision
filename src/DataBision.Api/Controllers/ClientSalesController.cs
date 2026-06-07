using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/sales")]
[AllowAnonymous] // TODO Sprint-6E: enforce JWT company_id claim validation
public sealed class ClientSalesController(ISalesService sales) : ControllerBase
{
    // GET /api/client/sales/overview?companyId=...&dateFrom=2026-01-01&dateTo=2026-12-31
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string companyId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null)
            return BadRequest(new { error = "invalid_date_from", message = "dateFrom must be a valid date (YYYY-MM-DD)." });
        if (to is null)
            return BadRequest(new { error = "invalid_date_to", message = "dateTo must be a valid date (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetOverviewAsync(companyId, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/daily?companyId=...&dateFrom=...&dateTo=...
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(
        [FromQuery] string companyId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null || to is null)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom and dateTo must be valid dates (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetDailyAsync(companyId, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/monthly?companyId=...&dateFrom=...&dateTo=...
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly(
        [FromQuery] string companyId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        var (from, to) = ParseDateRange(dateFrom, dateTo);
        if (from is null || to is null)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom and dateTo must be valid dates (YYYY-MM-DD)." });
        if (from > to)
            return BadRequest(new { error = "invalid_date_range", message = "dateFrom cannot be after dateTo." });

        var result = await sales.GetMonthlyAsync(companyId, from.Value, to.Value, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/customers?companyId=...&limit=50
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string companyId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await sales.GetCustomersAsync(companyId, limit, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/items?companyId=...&limit=50
    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] string companyId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await sales.GetItemsAsync(companyId, limit, ct);
        return Ok(new { data = result });
    }

    // GET /api/client/sales/salespersons?companyId=...&limit=50
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons(
        [FromQuery] string companyId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return BadRequest(new { error = "missing_company_id", message = "companyId is required." });

        if (limit < 1 || limit > 100)
            return BadRequest(new { error = "invalid_limit", message = "limit must be between 1 and 100." });

        var result = await sales.GetSalespersonsAsync(companyId, limit, ct);
        return Ok(new { data = result });
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
}
