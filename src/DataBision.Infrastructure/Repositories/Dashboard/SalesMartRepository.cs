using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

/// <summary>
/// Reads from Sprint 3 Sales MART tables using Dapper + Npgsql.
/// All queries filter by company_id — never cross-tenant data.
/// Column aliases use "QuotedName" format to match positional record DTOs.
/// </summary>
public sealed class SalesMartRepository(string connectionString) : ISalesMartRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    // ── Summary KPI ──────────────────────────────────────────────────────────

    public async Task<SalesMartKpiSummaryDto?> GetKpiSummaryAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            WITH ltm AS (
                SELECT
                    SUM(net_sales)          AS net_sales_ltm,
                    SUM(gross_sales)        AS gross_sales_ltm,
                    SUM(credit_memo_amount) AS credit_ltm,
                    SUM(invoice_count)      AS inv_count_ltm,
                    MAX(active_customers)   AS active_cust_ltm
                FROM mart.sales_period_kpi
                WHERE company_id = @company_id
                  AND (period_year * 100 + period_month) >
                      (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '12 months')::INT * 100 +
                       EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '12 months')::INT)
            ),
            prev_ltm AS (
                SELECT SUM(net_sales) AS net_sales_prev
                FROM mart.sales_period_kpi
                WHERE company_id = @company_id
                  AND (period_year * 100 + period_month) <=
                      (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '12 months')::INT * 100 +
                       EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '12 months')::INT)
                  AND (period_year * 100 + period_month) >
                      (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '24 months')::INT * 100 +
                       EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '24 months')::INT)
            ),
            orders AS (
                SELECT
                    COUNT(*)                                          AS open_count,
                    COALESCE(SUM(open_amount), 0)                    AS open_amount,
                    SUM(CASE WHEN is_overdue THEN 1 ELSE 0 END)      AS overdue_count
                FROM mart.open_sales_orders
                WHERE company_id = @company_id
            )
            SELECT
                COALESCE(l.net_sales_ltm, 0)                        AS "NetSalesLtm",
                COALESCE(p.net_sales_prev, 0)                       AS "NetSalesPrevLtm",
                CASE WHEN COALESCE(p.net_sales_prev, 0) > 0
                     THEN ROUND(((COALESCE(l.net_sales_ltm,0) - COALESCE(p.net_sales_prev,0))
                                / p.net_sales_prev) * 100, 2)
                     ELSE 0 END                                     AS "GrowthPct",
                CASE WHEN COALESCE(l.inv_count_ltm, 0) > 0
                     THEN COALESCE(l.gross_sales_ltm,0) / l.inv_count_ltm
                     ELSE 0 END                                     AS "AvgTicketLtm",
                CASE WHEN COALESCE(l.gross_sales_ltm, 0) > 0
                     THEN ROUND((COALESCE(l.credit_ltm,0) / l.gross_sales_ltm) * 100, 2)
                     ELSE 0 END                                     AS "ReturnRatePct",
                COALESCE(l.active_cust_ltm, 0)                     AS "ActiveCustomersLtm",
                COALESCE(o.open_count, 0)::INT                      AS "OpenOrdersCount",
                COALESCE(o.open_amount, 0)                          AS "OpenOrdersAmount",
                COALESCE(o.overdue_count, 0)::INT                   AS "OverdueOrdersCount"
            FROM ltm l
            CROSS JOIN prev_ltm p
            CROSS JOIN orders o;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SalesMartKpiSummaryDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    // ── Period trend ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesPeriodKpiDto>> GetByPeriodAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year         AS "Year",
                period_month        AS "Month",
                gross_sales         AS "GrossSales",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales           AS "NetSales",
                invoice_count       AS "InvoiceCount",
                credit_memo_count   AS "CreditMemoCount",
                active_customers    AS "ActiveCustomers",
                avg_ticket          AS "AvgTicket",
                return_rate_pct     AS "ReturnRatePct"
            FROM mart.sales_period_kpi
            WHERE company_id = @company_id
              AND (period_year * 100 + period_month) >
                  (EXTRACT(YEAR  FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT * 100 +
                   EXTRACT(MONTH FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT)
            ORDER BY period_year ASC, period_month ASC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesPeriodKpiDto>(
            new CommandDefinition(sql, new { company_id = companyId, months }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Top customers ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopCustomerMartDto>> GetTopCustomersAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                card_code           AS "CardCode",
                card_name           AS "CardName",
                gross_sales         AS "GrossSales",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales           AS "NetSales",
                invoice_count       AS "InvoiceCount",
                last_invoice_date   AS "LastInvoiceDate",
                dso_days            AS "DsoDays"
            FROM mart.top_customers
            WHERE company_id = @company_id
            ORDER BY net_sales DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TopCustomerMartDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Top items ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopItemMartDto>> GetTopItemsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_code           AS "ItemCode",
                item_name           AS "ItemName",
                item_group_name     AS "ItemGroupName",
                gross_sales         AS "GrossSales",
                credit_memo_amount  AS "CreditMemoAmount",
                net_sales           AS "NetSales",
                quantity_sold       AS "QuantitySold",
                invoice_count       AS "InvoiceCount",
                avg_unit_price      AS "AvgUnitPrice"
            FROM mart.top_items
            WHERE company_id = @company_id
            ORDER BY net_sales DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TopItemMartDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Top salespersons ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopSalespersonMartDto>> GetTopSalespersonsAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sales_person_code   AS "SalesPersonCode",
                sales_person_name   AS "SalesPersonName",
                net_sales           AS "NetSales",
                gross_sales         AS "GrossSales",
                invoice_count       AS "InvoiceCount",
                active_customers    AS "ActiveCustomers",
                avg_ticket          AS "AvgTicket",
                return_rate_pct     AS "ReturnRatePct"
            FROM mart.top_salespersons
            WHERE company_id = @company_id
            ORDER BY net_sales DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TopSalespersonMartDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Open orders pipeline ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<OpenSalesOrderMartDto>> GetOpenOrdersAsync(
        string companyId, bool overdueOnly, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                doc_num             AS "DocNum",
                card_code           AS "CardCode",
                card_name           AS "CardName",
                doc_date            AS "DocDate",
                doc_due_date        AS "DocDueDate",
                doc_total           AS "DocTotal",
                open_amount         AS "OpenAmount",
                days_open           AS "DaysOpen",
                is_overdue          AS "IsOverdue",
                sales_person_name   AS "SalesPersonName"
            FROM mart.open_sales_orders
            WHERE company_id = @company_id
              AND (@overdue_only = FALSE OR is_overdue = TRUE)
            ORDER BY is_overdue DESC, doc_due_date ASC
            LIMIT 500;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<OpenSalesOrderMartDto>(
            new CommandDefinition(sql,
                new { company_id = companyId, overdue_only = overdueOnly },
                cancellationToken: ct));
        return rows.AsList();
    }
}
