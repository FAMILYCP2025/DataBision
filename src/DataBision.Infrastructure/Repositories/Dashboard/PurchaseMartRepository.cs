using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

/// <summary>
/// Reads from Sprint 4 Purchase MART tables using Dapper + Npgsql.
/// All queries filter by company_id — never cross-tenant data.
/// Column aliases use "QuotedName" format to match positional record DTOs.
/// </summary>
public sealed class PurchaseMartRepository(string connectionString) : IPurchaseMartRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    // ── Summary KPI ──────────────────────────────────────────────────────────

    public async Task<PurchaseMartKpiSummaryDto?> GetKpiSummaryAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            WITH ltm AS (
                SELECT
                    SUM(gross_purchases)    AS gross_ltm,
                    SUM(invoice_count)      AS inv_count_ltm,
                    MAX(active_suppliers)   AS active_sup_ltm
                FROM mart.purchase_period_kpi
                WHERE company_id = @company_id
                  AND (period_year * 100 + period_month) >
                      (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '12 months')::INT * 100 +
                       EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '12 months')::INT)
            ),
            prev_ltm AS (
                SELECT SUM(gross_purchases) AS gross_prev
                FROM mart.purchase_period_kpi
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
                    COUNT(*)                                     AS open_count,
                    COALESCE(SUM(open_amount), 0)               AS open_amount,
                    SUM(CASE WHEN is_overdue THEN 1 ELSE 0 END) AS overdue_count
                FROM mart.open_purchase_orders
                WHERE company_id = @company_id
            )
            SELECT
                COALESCE(l.gross_ltm, 0)                        AS "GrossPurchasesLtm",
                COALESCE(p.gross_prev, 0)                       AS "GrossPurchasesPrevLtm",
                CASE WHEN COALESCE(p.gross_prev, 0) > 0
                     THEN ROUND(((COALESCE(l.gross_ltm,0) - COALESCE(p.gross_prev,0))
                                / p.gross_prev) * 100, 2)
                     ELSE 0 END                                 AS "GrowthPct",
                CASE WHEN COALESCE(l.inv_count_ltm, 0) > 0
                     THEN COALESCE(l.gross_ltm,0) / l.inv_count_ltm
                     ELSE 0 END                                 AS "AvgTicketLtm",
                COALESCE(l.active_sup_ltm, 0)                  AS "ActiveSuppliersLtm",
                COALESCE(o.open_count, 0)::INT                  AS "OpenOrdersCount",
                COALESCE(o.open_amount, 0)                      AS "OpenOrdersAmount",
                COALESCE(o.overdue_count, 0)::INT               AS "OverdueOrdersCount"
            FROM ltm l
            CROSS JOIN prev_ltm p
            CROSS JOIN orders o;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<PurchaseMartKpiSummaryDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    // ── Period trend ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PurchasePeriodKpiDto>> GetByPeriodAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year         AS "Year",
                period_month        AS "Month",
                gross_purchases     AS "GrossPurchases",
                credit_memo_amount  AS "CreditMemoAmount",
                net_purchases       AS "NetPurchases",
                invoice_count       AS "InvoiceCount",
                credit_memo_count   AS "CreditMemoCount",
                active_suppliers    AS "ActiveSuppliers",
                avg_ticket          AS "AvgTicket"
            FROM mart.purchase_period_kpi
            WHERE company_id = @company_id
              AND (period_year * 100 + period_month) >
                  (EXTRACT(YEAR  FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT * 100 +
                   EXTRACT(MONTH FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT)
            ORDER BY period_year ASC, period_month ASC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<PurchasePeriodKpiDto>(
            new CommandDefinition(sql, new { company_id = companyId, months }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Top suppliers ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopSupplierMartDto>> GetTopSuppliersAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                card_code           AS "CardCode",
                card_name           AS "CardName",
                gross_purchases     AS "GrossPurchases",
                credit_memo_amount  AS "CreditMemoAmount",
                net_purchases       AS "NetPurchases",
                invoice_count       AS "InvoiceCount",
                last_invoice_date   AS "LastInvoiceDate",
                dpo_days            AS "DpoDays"
            FROM mart.top_suppliers
            WHERE company_id = @company_id
            ORDER BY net_purchases DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TopSupplierMartDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Top purchase items ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TopPurchaseItemMartDto>> GetTopItemsAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_code           AS "ItemCode",
                item_name           AS "ItemName",
                item_group_name     AS "ItemGroupName",
                gross_purchases     AS "GrossPurchases",
                quantity_purchased  AS "QuantityPurchased",
                invoice_count       AS "InvoiceCount",
                avg_unit_price      AS "AvgUnitPrice"
            FROM mart.top_purchase_items
            WHERE company_id = @company_id
            ORDER BY gross_purchases DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TopPurchaseItemMartDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    // ── Open purchase orders pipeline ─────────────────────────────────────────

    public async Task<IReadOnlyList<OpenPurchaseOrderMartDto>> GetOpenOrdersAsync(
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
                is_overdue          AS "IsOverdue"
            FROM mart.open_purchase_orders
            WHERE company_id = @company_id
              AND (@overdue_only = FALSE OR is_overdue = TRUE)
            ORDER BY is_overdue DESC, doc_due_date ASC
            LIMIT 500;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<OpenPurchaseOrderMartDto>(
            new CommandDefinition(sql,
                new { company_id = companyId, overdue_only = overdueOnly },
                cancellationToken: ct));
        return rows.AsList();
    }
}
