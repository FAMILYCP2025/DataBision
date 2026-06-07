using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

/// <summary>
/// Dapper-based read-only repository against mart.* tables on Supabase PostgreSQL.
/// Uses the same NpgsqlConnection pattern as IngestCheckpointRepository.
/// </summary>
public sealed class DashboardRepository(string connectionString) : IDashboardRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    // ── Summary ──────────────────────────────────────────────────────────────

    public async Task<DashboardSummaryDto?> GetSummaryAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                company_id              AS "CompanyId",
                gross_sales_amount      AS "GrossSalesAmount",
                credit_memo_amount      AS "CreditMemoAmount",
                net_sales_amount        AS "NetSalesAmount",
                invoice_count           AS "InvoiceCount",
                credit_memo_count       AS "CreditMemoCount",
                active_customers        AS "ActiveCustomers",
                active_items            AS "ActiveItems",
                avg_ticket_amount       AS "AvgTicketAmount",
                last_invoice_date       AS "LastInvoiceDate",
                last_credit_memo_date   AS "LastCreditMemoDate",
                last_sync_at_utc        AS "LastSyncAtUtc",
                transformed_at_utc      AS "TransformedAtUtc"
            FROM mart.sales_kpi_summary
            WHERE company_id = @company_id
            LIMIT 1;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<SummaryRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));

        if (row is null) return null;

        return new DashboardSummaryDto
        {
            CompanyId           = row.CompanyId,
            GrossSalesAmount    = row.GrossSalesAmount,
            CreditMemoAmount    = row.CreditMemoAmount,
            NetSalesAmount      = row.NetSalesAmount,
            InvoiceCount        = row.InvoiceCount,
            CreditMemoCount     = row.CreditMemoCount,
            ActiveCustomers     = row.ActiveCustomers,
            ActiveItems         = row.ActiveItems,
            AvgTicketAmount     = row.AvgTicketAmount,
            LastInvoiceDate     = row.LastInvoiceDate.HasValue
                ? DateOnly.FromDateTime(row.LastInvoiceDate.Value) : null,
            LastCreditMemoDate  = row.LastCreditMemoDate.HasValue
                ? DateOnly.FromDateTime(row.LastCreditMemoDate.Value) : null,
            LastSyncAtUtc       = row.LastSyncAtUtc,
            TransformedAtUtc    = row.TransformedAtUtc,
        };
    }

    // ── Daily — last N days ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyLastNDaysAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        return await QuerySalesDailyAsync(companyId, cutoff, DateTime.UtcNow.Date, ct);
    }

    // ── Daily — explicit date range ───────────────────────────────────────────

    public Task<IReadOnlyList<SalesDailyDto>> GetSalesDailyByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => QuerySalesDailyAsync(companyId, dateFrom, dateTo, ct);

    private async Task<IReadOnlyList<SalesDailyDto>> QuerySalesDailyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct)
    {
        const string sql = """
            SELECT
                sales_date          AS "SalesDate",
                gross_sales_amount  AS "GrossSalesAmount",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales_amount    AS "NetSalesAmount",
                invoice_count       AS "InvoiceCount",
                credit_memo_count   AS "CreditMemoCount",
                active_customers    AS "ActiveCustomers",
                avg_ticket_amount   AS "AvgTicketAmount"
            FROM mart.sales_daily
            WHERE company_id = @company_id
              AND sales_date >= @date_from
              AND sales_date <= @date_to
            ORDER BY sales_date DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<DailyRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, date_from = dateFrom, date_to = dateTo },
                cancellationToken: ct));

        return rows.Select(r => new SalesDailyDto
        {
            SalesDate          = DateOnly.FromDateTime(r.SalesDate),
            GrossSalesAmount   = r.GrossSalesAmount,
            CreditMemoAmount   = r.CreditMemoAmount,
            NetSalesAmount     = r.NetSalesAmount,
            InvoiceCount       = r.InvoiceCount,
            CreditMemoCount    = r.CreditMemoCount,
            ActiveCustomers    = r.ActiveCustomers,
            AvgTicketAmount    = r.AvgTicketAmount,
        }).ToList();
    }

    // ── Monthly — last N months ───────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyLastNMonthsAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        var cutoff = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            .AddMonths(-months);
        return await QuerySalesMonthlyAsync(companyId, cutoff, DateTime.UtcNow, ct);
    }

    // ── Monthly — explicit date range ─────────────────────────────────────────

    public Task<IReadOnlyList<SalesMonthlyDto>> GetSalesMonthlyByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        => QuerySalesMonthlyAsync(companyId, dateFrom, dateTo, ct);

    private async Task<IReadOnlyList<SalesMonthlyDto>> QuerySalesMonthlyAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct)
    {
        const string sql = """
            SELECT
                sales_month         AS "SalesMonth",
                gross_sales_amount  AS "GrossSalesAmount",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales_amount    AS "NetSalesAmount",
                invoice_count       AS "InvoiceCount",
                credit_memo_count   AS "CreditMemoCount",
                active_customers    AS "ActiveCustomers",
                avg_ticket_amount   AS "AvgTicketAmount"
            FROM mart.sales_monthly
            WHERE company_id = @company_id
              AND sales_month >= DATE_TRUNC('month', @date_from::date)
              AND sales_month <= DATE_TRUNC('month', @date_to::date)
            ORDER BY sales_month DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<MonthlyRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, date_from = dateFrom, date_to = dateTo },
                cancellationToken: ct));

        return rows.Select(r => new SalesMonthlyDto
        {
            SalesMonth         = DateOnly.FromDateTime(r.SalesMonth),
            GrossSalesAmount   = r.GrossSalesAmount,
            CreditMemoAmount   = r.CreditMemoAmount,
            NetSalesAmount     = r.NetSalesAmount,
            InvoiceCount       = r.InvoiceCount,
            CreditMemoCount    = r.CreditMemoCount,
            ActiveCustomers    = r.ActiveCustomers,
            AvgTicketAmount    = r.AvgTicketAmount,
        }).ToList();
    }

    // ── Aggregated overview (from sales_daily) ────────────────────────────────

    public async Task<SalesOverviewDto> GetSalesOverviewByRangeAsync(
        string companyId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COALESCE(SUM(gross_sales_amount), 0)  AS "GrossSalesAmount",
                COALESCE(SUM(credit_memo_amount), 0)  AS "CreditMemoAmount",
                COALESCE(SUM(net_sales_amount), 0)    AS "NetSalesAmount",
                COALESCE(SUM(invoice_count), 0)       AS "InvoiceCount",
                COALESCE(SUM(credit_memo_count), 0)   AS "CreditMemoCount",
                COALESCE(MAX(active_customers), 0)    AS "ActiveCustomers",
                CASE WHEN COALESCE(SUM(invoice_count), 0) > 0
                     THEN COALESCE(SUM(gross_sales_amount), 0) / SUM(invoice_count)
                     ELSE 0 END                       AS "AvgTicketAmount"
            FROM mart.sales_daily
            WHERE company_id = @company_id
              AND sales_date >= @date_from
              AND sales_date <= @date_to;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var row = await conn.QueryFirstAsync<OverviewRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, date_from = dateFrom, date_to = dateTo },
                cancellationToken: ct));

        return new SalesOverviewDto
        {
            GrossSalesAmount = row.GrossSalesAmount,
            CreditMemoAmount = row.CreditMemoAmount,
            NetSalesAmount   = row.NetSalesAmount,
            InvoiceCount     = row.InvoiceCount,
            CreditMemoCount  = row.CreditMemoCount,
            ActiveCustomers  = row.ActiveCustomers,
            AvgTicketAmount  = row.AvgTicketAmount,
            DateFrom         = DateOnly.FromDateTime(dateFrom),
            DateTo           = DateOnly.FromDateTime(dateTo),
        };
    }

    // ── Customers ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CustomerSalesDto>> GetCustomersAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                card_code               AS "CardCode",
                card_name               AS "CardName",
                sales_amount            AS "SalesAmount",
                credit_memo_amount      AS "CreditMemoAmount",
                net_sales_amount        AS "NetSalesAmount",
                invoice_count           AS "InvoiceCount",
                credit_memo_count       AS "CreditMemoCount",
                last_invoice_date       AS "LastInvoiceDate",
                first_invoice_date      AS "FirstInvoiceDate",
                avg_ticket_amount       AS "AvgTicketAmount"
            FROM mart.customer_sales
            WHERE company_id = @company_id
            ORDER BY net_sales_amount DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<CustomerRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, limit },
                cancellationToken: ct));

        return rows.Select(r => new CustomerSalesDto
        {
            CardCode         = r.CardCode,
            CardName         = r.CardName,
            SalesAmount      = r.SalesAmount,
            CreditMemoAmount = r.CreditMemoAmount,
            NetSalesAmount   = r.NetSalesAmount,
            InvoiceCount     = r.InvoiceCount,
            CreditMemoCount  = r.CreditMemoCount,
            LastInvoiceDate  = r.LastInvoiceDate.HasValue
                ? DateOnly.FromDateTime(r.LastInvoiceDate.Value) : null,
            FirstInvoiceDate = r.FirstInvoiceDate.HasValue
                ? DateOnly.FromDateTime(r.FirstInvoiceDate.Value) : null,
            AvgTicketAmount  = r.AvgTicketAmount,
        }).ToList();
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ItemSalesDto>> GetItemsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_code           AS "ItemCode",
                item_name           AS "ItemName",
                quantity_sold       AS "QuantitySold",
                gross_sales_amount  AS "GrossSalesAmount",
                line_count          AS "LineCount",
                invoice_count       AS "InvoiceCount",
                last_sale_date      AS "LastSaleDate"
            FROM mart.item_sales
            WHERE company_id = @company_id
            ORDER BY gross_sales_amount DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<ItemRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, limit },
                cancellationToken: ct));

        return rows.Select(r => new ItemSalesDto
        {
            ItemCode         = r.ItemCode,
            ItemName         = r.ItemName,
            QuantitySold     = r.QuantitySold,
            GrossSalesAmount = r.GrossSalesAmount,
            LineCount        = r.LineCount,
            InvoiceCount     = r.InvoiceCount,
            LastSaleDate     = r.LastSaleDate.HasValue
                ? DateOnly.FromDateTime(r.LastSaleDate.Value) : null,
        }).ToList();
    }

    // ── Salespersons ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalespersonSalesDto>> GetSalespersonsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sales_person_code   AS "SalesPersonCode",
                sales_person_name   AS "SalesPersonName",
                sales_amount        AS "SalesAmount",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales_amount    AS "NetSalesAmount",
                invoice_count       AS "InvoiceCount",
                credit_memo_count   AS "CreditMemoCount",
                active_customers    AS "ActiveCustomers",
                avg_ticket_amount   AS "AvgTicketAmount"
            FROM mart.salesperson_sales
            WHERE company_id = @company_id
            ORDER BY net_sales_amount DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<SalespersonRow>(
            new CommandDefinition(sql,
                new { company_id = companyId, limit },
                cancellationToken: ct));

        return rows.Select(r => new SalespersonSalesDto
        {
            SalesPersonCode  = r.SalesPersonCode,
            SalesPersonName  = r.SalesPersonName,
            SalesAmount      = r.SalesAmount,
            CreditMemoAmount = r.CreditMemoAmount,
            NetSalesAmount   = r.NetSalesAmount,
            InvoiceCount     = r.InvoiceCount,
            CreditMemoCount  = r.CreditMemoCount,
            ActiveCustomers  = r.ActiveCustomers,
            AvgTicketAmount  = r.AvgTicketAmount,
        }).ToList();
    }

    // ── Private row types (Dapper mapping) ────────────────────────────────────

    private sealed class SummaryRow
    {
        public string CompanyId { get; set; } = string.Empty;
        public decimal GrossSalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public int ActiveCustomers { get; set; }
        public int ActiveItems { get; set; }
        public decimal AvgTicketAmount { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public DateTime? LastCreditMemoDate { get; set; }
        public DateTime? LastSyncAtUtc { get; set; }
        public DateTime TransformedAtUtc { get; set; }
    }

    private sealed class DailyRow
    {
        public DateTime SalesDate { get; set; }
        public decimal GrossSalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AvgTicketAmount { get; set; }
    }

    private sealed class MonthlyRow
    {
        public DateTime SalesMonth { get; set; }
        public decimal GrossSalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AvgTicketAmount { get; set; }
    }

    private sealed class OverviewRow
    {
        public decimal GrossSalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AvgTicketAmount { get; set; }
    }

    private sealed class CustomerRow
    {
        public string CardCode { get; set; } = string.Empty;
        public string? CardName { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public DateTime? FirstInvoiceDate { get; set; }
        public decimal AvgTicketAmount { get; set; }
    }

    private sealed class ItemRow
    {
        public string ItemCode { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal GrossSalesAmount { get; set; }
        public int LineCount { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? LastSaleDate { get; set; }
    }

    private sealed class SalespersonRow
    {
        public string SalesPersonCode { get; set; } = string.Empty;
        public string? SalesPersonName { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal CreditMemoAmount { get; set; }
        public decimal NetSalesAmount { get; set; }
        public int InvoiceCount { get; set; }
        public int CreditMemoCount { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AvgTicketAmount { get; set; }
    }
}
