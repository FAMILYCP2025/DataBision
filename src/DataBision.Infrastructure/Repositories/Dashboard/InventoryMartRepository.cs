using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class InventoryMartRepository(string connectionString) : IInventoryMartRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<InventoryMartKpiSummaryDto?> GetKpiSummaryAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            WITH snap AS (
                SELECT
                    COUNT(*)         AS total_items,
                    SUM(stock_value) AS total_stock_value
                FROM mart.inventory_snapshot
                WHERE company_id = @company_id
            ),
            slow AS (
                SELECT
                    COUNT(*)         AS slow_count,
                    SUM(stock_value) AS slow_value
                FROM mart.slow_moving_items
                WHERE company_id = @company_id
            ),
            below AS (
                SELECT SUM(items_below_min) AS below_min_total
                FROM mart.warehouse_stock
                WHERE company_id = @company_id
            ),
            wh AS (
                SELECT COUNT(*) AS warehouse_count
                FROM mart.warehouse_stock
                WHERE company_id = @company_id
            )
            SELECT
                COALESCE(s.total_stock_value, 0)    AS "TotalStockValue",
                COALESCE(s.total_items, 0)::INT     AS "TotalItems",
                COALESCE(sl.slow_count, 0)::INT     AS "SlowMovingItemsCount",
                COALESCE(sl.slow_value, 0)          AS "SlowMovingStockValue",
                COALESCE(b.below_min_total, 0)::INT AS "ItemsBelowMin",
                COALESCE(w.warehouse_count, 0)::INT AS "WarehouseCount"
            FROM snap s
            CROSS JOIN slow sl
            CROSS JOIN below b
            CROSS JOIN wh w;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<InventoryMartKpiSummaryDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<InventorySnapshotItemDto>> GetSnapshotAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_code       AS "ItemCode",
                item_name       AS "ItemName",
                item_group_name AS "ItemGroupName",
                on_hand         AS "OnHand",
                committed       AS "Committed",
                ordered         AS "Ordered",
                available       AS "Available",
                avg_price       AS "AvgPrice",
                stock_value     AS "StockValue"
            FROM mart.inventory_snapshot
            WHERE company_id = @company_id
            ORDER BY stock_value DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<InventorySnapshotItemDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<InventoryMovementKpiDto>> GetMovementByPeriodAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year       AS "Year",
                period_month      AS "Month",
                inbound_qty       AS "InboundQty",
                outbound_qty      AS "OutboundQty",
                net_qty           AS "NetQty",
                inbound_value     AS "InboundValue",
                outbound_value    AS "OutboundValue",
                transaction_count AS "TransactionCount"
            FROM mart.inventory_movement_kpi
            WHERE company_id = @company_id
              AND (period_year * 100 + period_month) >
                  (EXTRACT(YEAR  FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT * 100 +
                   EXTRACT(MONTH FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT)
            ORDER BY period_year ASC, period_month ASC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<InventoryMovementKpiDto>(
            new CommandDefinition(sql, new { company_id = companyId, months }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<SlowMovingItemDto>> GetSlowMovingItemsAsync(
        string companyId, int minDays, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                item_code             AS "ItemCode",
                item_name             AS "ItemName",
                item_group_name       AS "ItemGroupName",
                on_hand               AS "OnHand",
                stock_value           AS "StockValue",
                last_movement_date    AS "LastMovementDate",
                days_without_movement AS "DaysWithoutMovement"
            FROM mart.slow_moving_items
            WHERE company_id = @company_id
              AND days_without_movement >= @min_days
            ORDER BY days_without_movement DESC
            LIMIT 500;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SlowMovingItemDto>(
            new CommandDefinition(sql, new { company_id = companyId, min_days = minDays }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<WarehouseStockDto>> GetWarehouseStockAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                warehouse_code    AS "WarehouseCode",
                warehouse_name    AS "WarehouseName",
                total_items       AS "TotalItems",
                total_on_hand     AS "TotalOnHand",
                total_stock_value AS "TotalStockValue",
                items_below_min   AS "ItemsBelowMin"
            FROM mart.warehouse_stock
            WHERE company_id = @company_id
            ORDER BY total_stock_value DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<WarehouseStockDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
        return rows.AsList();
    }
}
