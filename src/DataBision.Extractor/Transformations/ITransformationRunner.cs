namespace DataBision.Extractor.Transformations;

public interface ITransformationRunner
{
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshStgAsync(
        string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshMartAsync(
        string companyId, CancellationToken ct = default);

    Task<(IReadOnlyList<(string Object, int RowsAffected)> Stg,
          IReadOnlyList<(string Object, int RowsAffected)> Mart)> RefreshAllAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_all_processes(@company_id) to populate process-dashboard MART tables.
    /// Returns empty list if the function does not exist yet (migration not applied).
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshProcessMartAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_sales(@company_id) to populate sales MART tables:
    /// sales_period_kpi, top_customers, top_items, top_salespersons, open_sales_orders.
    /// Requires migration 20260628000002_AddSalesMartRefreshFunctions applied to Supabase.
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshSalesMartAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_purchases(@company_id) to populate purchase MART tables:
    /// purchase_period_kpi, top_suppliers, top_purchase_items, open_purchase_orders.
    /// Requires migration 20260629000002_AddPurchaseMartRefreshFunctions applied to Supabase.
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshPurchasesMartAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_inventory(@company_id) to populate inventory MART tables:
    /// inventory_snapshot, inventory_movement_kpi, slow_moving_items, warehouse_stock.
    /// Requires migration 20260630000002_AddInventoryMartRefreshFunctions applied to Supabase.
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshInventoryMartAsync(
        string companyId, CancellationToken ct = default);

    /// <summary>
    /// Calls mart.refresh_finance(@company_id) to populate finance MART tables:
    /// ar_aging, ap_aging, finance_period_kpi, finance_summary.
    /// Requires migration 20260701000002_AddFinanceMartRefreshFunctions applied to Supabase.
    /// </summary>
    Task<IReadOnlyList<(string Object, int RowsAffected)>> RefreshFinanceMartAsync(
        string companyId, CancellationToken ct = default);
}
