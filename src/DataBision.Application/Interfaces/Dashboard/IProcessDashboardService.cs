using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Application.Interfaces.Dashboard;

public interface IProcessDashboardService
{
    // SALES
    Task<PagedResultDto<SalesCustomerDashboardDto>> GetSalesCustomersAsync(string companyId, PaginationOptions p, NativeBiFilterDto? filters = null, CancellationToken ct = default);
    Task<PagedResultDto<SalesItemDashboardDto>> GetSalesItemsAsync(string companyId, PaginationOptions p, NativeBiFilterDto? filters = null, CancellationToken ct = default);
    Task<IReadOnlyList<SalesFulfillmentDto>> GetSalesFulfillmentAsync(string companyId, int days, CancellationToken ct = default);
    Task<IReadOnlyList<SalesItemGroupSummaryDto>> GetSalesItemGroupSummaryAsync(string companyId, NativeBiFilterDto? filters = null, CancellationToken ct = default);
    Task<IReadOnlyList<SalesWarehouseSummaryDto>> GetSalesWarehouseSummaryAsync(string companyId, NativeBiFilterDto? filters = null, CancellationToken ct = default);

    // FINANCE
    Task<IReadOnlyList<FinanceExecutiveDto>> GetFinanceExecutiveAsync(string companyId, int days, CancellationToken ct = default);
    Task<PagedResultDto<FinanceArAgingDto>> GetFinanceArAgingAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
    Task<PagedResultDto<FinanceApAgingDto>> GetFinanceApAgingAsync(string companyId, PaginationOptions p, CancellationToken ct = default);

    // INVENTORY
    Task<PagedResultDto<InventoryRotationDto>> GetInventoryRotationAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
    Task<PagedResultDto<InventoryStockDto>> GetInventoryStockAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryWarehouseDto>> GetInventoryWarehousesAsync(string companyId, CancellationToken ct = default);

    // PURCHASING
    Task<IReadOnlyList<PurchasingExecutiveDto>> GetPurchasingExecutiveAsync(string companyId, int days, CancellationToken ct = default);
    Task<PagedResultDto<PurchasingSupplierDto>> GetPurchasingSuppliersAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
    Task<PagedResultDto<PurchasingReceivingDto>> GetPurchasingReceivingAsync(string companyId, PaginationOptions p, CancellationToken ct = default);

    // OPERATIONS
    Task<OperationHealthDto?> GetPipelineHealthAsync(string companyId, CancellationToken ct = default);
    Task<PagedResultDto<OperationAlertDto>> GetActiveAlertsAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
    Task<PagedResultDto<OperationDataQualityDto>> GetDataQualityIssuesAsync(string companyId, PaginationOptions p, CancellationToken ct = default);
}
