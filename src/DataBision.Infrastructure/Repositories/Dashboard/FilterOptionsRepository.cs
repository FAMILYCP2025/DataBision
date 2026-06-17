using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

/// <summary>
/// Queries MART views/tables in Supabase to return distinct filter options.
/// Any query that targets a table or column that may not exist returns an empty list
/// rather than throwing, keeping the UI resilient for early/partial MART setups.
/// </summary>
public sealed class FilterOptionsRepository(string connectionString) : IFilterOptionsRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<IReadOnlyList<FilterOptionDto>> GetItemGroupsAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT
                item_group_code AS "Code",
                COALESCE(item_group_name, item_group_code) AS "Name"
            FROM mart.sales_item_dashboard
            WHERE company_id = @company_id
              AND item_group_code IS NOT NULL
              AND item_group_code <> ''
            ORDER BY "Name";
            """;
        return await SafeQuery(sql, new { company_id = companyId }, ct);
    }

    public async Task<IReadOnlyList<FilterOptionDto>> GetCustomerGroupsAsync(
        string companyId, CancellationToken ct = default)
    {
        // Customer groups sourced from dim_customers if available; may be empty in early setups
        const string sql = """
            SELECT DISTINCT
                customer_group_code AS "Code",
                COALESCE(customer_group_name, customer_group_code) AS "Name"
            FROM mart.dim_customers
            WHERE company_id = @company_id
              AND customer_group_code IS NOT NULL
              AND customer_group_code <> ''
            ORDER BY "Name";
            """;
        return await SafeQuery(sql, new { company_id = companyId }, ct);
    }

    public async Task<IReadOnlyList<FilterOptionDto>> GetSupplierGroupsAsync(
        string companyId, CancellationToken ct = default)
    {
        // Supplier groups sourced from dim_suppliers if available
        const string sql = """
            SELECT DISTINCT
                supplier_group_code AS "Code",
                COALESCE(supplier_group_name, supplier_group_code) AS "Name"
            FROM mart.dim_suppliers
            WHERE company_id = @company_id
              AND supplier_group_code IS NOT NULL
              AND supplier_group_code <> ''
            ORDER BY "Name";
            """;
        return await SafeQuery(sql, new { company_id = companyId }, ct);
    }

    public async Task<IReadOnlyList<FilterOptionDto>> GetWarehousesAsync(
        string companyId, CancellationToken ct = default)
    {
        // Warehouses from mart.inventory_warehouse (used by existing /warehouses endpoint)
        const string sql = """
            SELECT DISTINCT
                warehouse_code AS "Code",
                COALESCE(warehouse_name, warehouse_code) AS "Name"
            FROM mart.inventory_warehouse
            WHERE company_id = @company_id
              AND warehouse_code IS NOT NULL
              AND warehouse_code <> ''
            ORDER BY "Name";
            """;
        return await SafeQuery(sql, new { company_id = companyId }, ct);
    }

    public async Task<IReadOnlyList<FilterOptionDto>> GetSalespersonsAsync(
        string companyId, CancellationToken ct = default)
    {
        // Salespersons from mart.sales_customer_dashboard — use name as both code and label
        // until a dedicated dim_salespersons table is available
        const string sql = """
            SELECT DISTINCT
                COALESCE(salesperson_code, salesperson_name) AS "Code",
                salesperson_name AS "Name"
            FROM mart.sales_customer_dashboard
            WHERE company_id = @company_id
              AND salesperson_name IS NOT NULL
              AND salesperson_name <> ''
            ORDER BY "Name";
            """;
        return await SafeQuery(sql, new { company_id = companyId }, ct);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<FilterOptionDto>> SafeQuery(
        string sql, object param, CancellationToken ct)
    {
        try
        {
            await using var conn = OpenConnection();
            await conn.OpenAsync(ct);
            var rows = await conn.QueryAsync<FilterOptionRow>(
                new CommandDefinition(sql, param, cancellationToken: ct));
            return rows.Select(r => new FilterOptionDto(r.Code, r.Name)).ToList();
        }
        catch
        {
            // Table or column doesn't exist yet in MART — return empty list
            return [];
        }
    }

    private sealed record FilterOptionRow(string Code, string Name);
}
