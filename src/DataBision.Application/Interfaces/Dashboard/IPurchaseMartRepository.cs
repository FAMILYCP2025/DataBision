using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

/// <summary>
/// Reads from Sprint 4 Purchase MART tables:
///   mart.purchase_period_kpi, mart.top_suppliers, mart.top_purchase_items,
///   mart.open_purchase_orders
/// All methods filter by company_id.
/// </summary>
public interface IPurchaseMartRepository
{
    Task<PurchaseMartKpiSummaryDto?> GetKpiSummaryAsync(string companyId, CancellationToken ct = default);

    Task<IReadOnlyList<PurchasePeriodKpiDto>> GetByPeriodAsync(
        string companyId, int months, CancellationToken ct = default);

    Task<IReadOnlyList<TopSupplierMartDto>> GetTopSuppliersAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<TopPurchaseItemMartDto>> GetTopItemsAsync(
        string companyId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<OpenPurchaseOrderMartDto>> GetOpenOrdersAsync(
        string companyId, bool overdueOnly, CancellationToken ct = default);
}
