using DataBision.Api.Security;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DataBision.Api.Controllers;

[ApiController]
[Route("api/client/bi/finance")]
[AllowAnonymous]
[EnableRateLimiting("api")]
public sealed class ClientBiFinanceController(
    IProcessDashboardService svc,
    IFinanceMartRepository? financeMart,
    IConfiguration config) : ControllerBase
{
    // GET /api/client/bi/finance/executive?days=30
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutive(
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (days < 1 || days > 365)
            return this.BadRequestError("invalid_days", "days must be between 1 and 365.");

        var result = await svc.GetFinanceExecutiveAsync(ctx.CompanyId!, days, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/ar-aging?limit=50&offset=0&sortBy=overdueAmount&sortDir=desc
    [HttpGet("ar-aging")]
    public async Task<IActionResult> GetArAging(
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

        var result = await svc.GetFinanceArAgingAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset, sortBy, sortDir), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/finance/ap-aging?limit=50&offset=0
    [HttpGet("ap-aging")]
    public async Task<IActionResult> GetApAging(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (limit < 1 || limit > 200)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 200.");

        var result = await svc.GetFinanceApAgingAsync(ctx.CompanyId!,
            new PaginationOptions(limit, offset), ct);
        return this.OkPaged(result.Data, result.Meta);
    }

    // GET /api/client/bi/finance/income-statement?year=2024&month=6&months=12
    // Returns P&L periods from mart.income_statement_summary.
    // Empty list when MART data not yet populated — frontend shows FinancialDataPending.
    [HttpGet("income-statement")]
    public async Task<IActionResult> GetIncomeStatement(
        [FromQuery] int? year   = null,
        [FromQuery] int? month  = null,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetIncomeStatementAsync(ctx.CompanyId!, year, month, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/balance-sheet?snapshotDate=2024-06-30
    // Returns balance sheet snapshot from mart.balance_sheet_summary.
    // If snapshotDate omitted, returns the most recent snapshot date.
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet(
        [FromQuery] string? snapshotDate = null,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetBalanceSheetAsync(ctx.CompanyId!, snapshotDate, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/ebitda?months=12
    // Returns EBITDA trend from mart.ebitda_summary (last N months, oldest→newest).
    [HttpGet("ebitda")]
    public async Task<IActionResult> GetEbitda(
        [FromQuery] int months = 12,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        if (months < 1 || months > 60)
            return this.BadRequestError("invalid_months", "months must be between 1 and 60.");

        var result = await svc.GetEbitdaAsync(ctx.CompanyId!, months, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/chart-of-accounts?postableOnly=false
    // Returns chart of accounts with balances from mart.gl_accounts + mart.account_balances.
    [HttpGet("chart-of-accounts")]
    public async Task<IActionResult> GetChartOfAccounts(
        [FromQuery] bool postableOnly = false,
        CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetChartOfAccountsAsync(ctx.CompanyId!, postableOnly, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/validations
    // Returns financial health score, issues, and balance reconciliation.
    [HttpGet("validations")]
    public async Task<IActionResult> GetValidations(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetFinanceValidationsAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/readiness
    // Returns layer-by-layer row counts and readiness status (blocked/warning/ready).
    [HttpGet("readiness")]
    public async Task<IActionResult> GetReadiness(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetFinanceReadinessAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // GET /api/client/bi/finance/refresh-status
    // Returns last OACT/OJDT extraction run and last MART transform run timestamps and status.
    // Intended for the dashboard refresh-log widget — read-only, no refresh trigger.
    [HttpGet("refresh-status")]
    public async Task<IActionResult> GetRefreshStatus(CancellationToken ct = default)
    {
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;

        var result = await svc.GetFinanceRefreshStatusAsync(ctx.CompanyId!, ct);
        return this.OkData(result);
    }

    // ── Sprint 6 — Finance MART endpoints ────────────────────────────────────

    // GET /api/client/bi/finance/mart/summary
    [HttpGet("mart/summary")]
    public async Task<IActionResult> GetMartSummary(CancellationToken ct)
    {
        if (financeMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        var result = await financeMart.GetSummaryAsync(ctx.CompanyId!, ct);
        return result is null
            ? this.OkData(new { hasData = false })
            : this.OkData(result);
    }

    // GET /api/client/bi/finance/mart/ar-aging?limit=50
    [HttpGet("mart/ar-aging")]
    public async Task<IActionResult> GetMartArAging(
        [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (financeMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 500)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 500.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await financeMart.GetArAgingAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/finance/mart/ap-aging?limit=50
    [HttpGet("mart/ap-aging")]
    public async Task<IActionResult> GetMartApAging(
        [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (financeMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (limit < 1 || limit > 500)
            return this.BadRequestError("invalid_limit", "limit must be between 1 and 500.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await financeMart.GetApAgingAsync(ctx.CompanyId!, limit, ct));
    }

    // GET /api/client/bi/finance/mart/period-kpi?months=12
    [HttpGet("mart/period-kpi")]
    public async Task<IActionResult> GetMartPeriodKpi(
        [FromQuery] int months = 12, CancellationToken ct = default)
    {
        if (financeMart is null)
            return this.BadRequestError("staging_not_configured", "Staging connection is not configured.");
        if (months < 1 || months > 36)
            return this.BadRequestError("invalid_months", "months must be between 1 and 36.");
        var ctx = CompanyContextResolver.TryResolve(HttpContext, config);
        if (!ctx.IsSuccess) return ctx.Error!;
        return this.OkData(await financeMart.GetPeriodKpiAsync(ctx.CompanyId!, months, ct));
    }
}
