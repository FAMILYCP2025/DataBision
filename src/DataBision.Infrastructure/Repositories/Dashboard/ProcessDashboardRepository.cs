using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class ProcessDashboardRepository(string connectionString) : IProcessDashboardRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    // ── Sort helpers ─────────────────────────────────────────────────────────

    private static readonly IReadOnlyDictionary<string, string> CustomerSortCols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["netSales"]         = "net_sales",
            ["grossSales"]       = "gross_sales",
            ["invoiceCount"]     = "invoice_count",
            ["lastInvoiceDate"]  = "last_invoice_date",
            ["cardCode"]         = "card_code",
        };

    private static readonly IReadOnlyDictionary<string, string> ItemSortCols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["grossSales"]  = "gross_sales",
            ["qtySold"]     = "quantity_sold",
            ["invoiceCount"] = "invoice_count",
            ["itemCode"]    = "item_code",
        };

    private static readonly IReadOnlyDictionary<string, string> ArAgingSortCols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["overdueAmount"] = "overdue_amount",
            ["balanceDue"]    = "balance_due",
            ["cardCode"]      = "card_code",
        };

    private static readonly IReadOnlyDictionary<string, string> RotationSortCols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["qtySold30d"]       = "qty_sold_30d",
            ["qtySold90d"]       = "qty_sold_90d",
            ["coverageDays"]     = "coverage_days",
            ["rotationStatus"]   = "rotation_status",
            ["itemCode"]         = "item_code",
        };

    private static string ResolveCol(string? sortBy, IReadOnlyDictionary<string, string> map, string def)
        => (sortBy is not null && map.TryGetValue(sortBy, out var c)) ? c : def;

    private static string ResolveDir(string? sortDir)
        => sortDir?.ToLowerInvariant() == "asc" ? "ASC" : "DESC";

    // ── SALES ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SalesCustomerDashboardDto>> GetSalesCustomersAsync(
        string companyId, PaginationOptions p, NativeBiFilterDto? filters = null, CancellationToken ct = default)
    {
        var col = ResolveCol(p.SortBy, CustomerSortCols, "net_sales");
        var dir = ResolveDir(p.SortDir);
        var sql = $"""
            SELECT
                card_code           AS "CardCode",
                card_name           AS "CardName",
                card_type           AS "CardType",
                salesperson_name    AS "SalespersonName",
                gross_sales         AS "GrossSales",
                credit_memos        AS "CreditMemos",
                net_sales           AS "NetSales",
                invoice_count       AS "InvoiceCount",
                avg_ticket          AS "AvgTicket",
                last_invoice_date   AS "LastInvoiceDate",
                is_active           AS "IsActive"
            FROM mart.sales_customer_dashboard
            WHERE company_id = @company_id
              AND (@salesperson IS NULL OR salesperson_name = ANY(string_to_array(@salesperson, ',')))
            ORDER BY {col} {dir}
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesCustomerDashboardRow>(
            new CommandDefinition(sql, new
            {
                company_id  = companyId,
                limit       = p.Limit,
                offset      = p.Offset,
                salesperson = string.IsNullOrWhiteSpace(filters?.SalespersonCodes) ? null : filters!.SalespersonCodes,
            }, cancellationToken: ct));
        return rows.Select(r => new SalesCustomerDashboardDto
        {
            CardCode         = r.CardCode,
            CardName         = r.CardName,
            CardType         = r.CardType,
            SalespersonName  = r.SalespersonName,
            GrossSales       = r.GrossSales,
            CreditMemos      = r.CreditMemos,
            NetSales         = r.NetSales,
            InvoiceCount     = r.InvoiceCount,
            AvgTicket        = r.AvgTicket,
            LastInvoiceDate  = r.LastInvoiceDate.HasValue ? DateOnly.FromDateTime(r.LastInvoiceDate.Value) : null,
            IsActive         = r.IsActive,
        }).ToList();
    }

    public async Task<IReadOnlyList<SalesItemDashboardDto>> GetSalesItemsAsync(
        string companyId, PaginationOptions p, NativeBiFilterDto? filters = null, CancellationToken ct = default)
    {
        var col = ResolveCol(p.SortBy, ItemSortCols, "gross_sales");
        var dir = ResolveDir(p.SortDir);
        var (dateFrom, dateTo) = filters?.EffectiveDateRange() ?? (null, null);
        var sql = $"""
            SELECT
                item_code           AS "ItemCode",
                item_name           AS "ItemName",
                item_group_code     AS "ItemGroupCode",
                quantity_sold       AS "QuantitySold",
                gross_sales         AS "GrossSales",
                gross_margin_pct    AS "GrossMarginPct",
                invoice_count       AS "InvoiceCount",
                last_sale_date      AS "LastSaleDate"
            FROM mart.sales_item_dashboard
            WHERE company_id = @company_id
              AND (@date_from IS NULL OR last_sale_date >= @date_from::date)
              AND (@date_to   IS NULL OR last_sale_date <= @date_to::date)
              AND (@item_group IS NULL OR item_group_code = ANY(string_to_array(@item_group, ',')))
            ORDER BY {col} {dir}
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesItemDashboardRow>(
            new CommandDefinition(sql, new
            {
                company_id  = companyId,
                limit       = p.Limit,
                offset      = p.Offset,
                date_from   = dateFrom.HasValue ? (object)dateFrom.Value.ToString("yyyy-MM-dd") : null,
                date_to     = dateTo.HasValue ? (object)dateTo.Value.ToString("yyyy-MM-dd") : null,
                item_group  = string.IsNullOrWhiteSpace(filters?.ItemGroupCodes) ? null : filters!.ItemGroupCodes,
            }, cancellationToken: ct));
        return rows.Select(r => new SalesItemDashboardDto
        {
            ItemCode       = r.ItemCode,
            ItemName       = r.ItemName,
            ItemGroupCode  = r.ItemGroupCode,
            QuantitySold   = r.QuantitySold,
            GrossSales     = r.GrossSales,
            GrossMarginPct = r.GrossMarginPct,
            InvoiceCount   = r.InvoiceCount,
            LastSaleDate   = r.LastSaleDate.HasValue ? DateOnly.FromDateTime(r.LastSaleDate.Value) : null,
        }).ToList();
    }

    public async Task<IReadOnlyList<SalesItemGroupSummaryDto>> GetSalesItemGroupSummaryAsync(
        string companyId, NativeBiFilterDto? filters = null, CancellationToken ct = default)
    {
        var (dateFrom, dateTo) = filters?.EffectiveDateRange() ?? (null, null);

        const string sql = """
            SELECT
                item_group_code                              AS "ItemGroupCode",
                item_group_code                              AS "ItemGroupName",
                SUM(gross_sales)                             AS "GrossSales",
                SUM(gross_sales - COALESCE(credit_memos, 0)) AS "NetSales",
                SUM(invoice_count)                           AS "InvoiceCount",
                COUNT(DISTINCT item_code)                    AS "SkuCount",
                CASE WHEN SUM(gross_sales) > 0
                     THEN AVG(gross_margin_pct)
                     ELSE 0 END                              AS "GrossMarginPct"
            FROM mart.sales_item_dashboard
            WHERE company_id = @company_id
              AND (@date_from IS NULL OR last_sale_date >= @date_from::date)
              AND (@date_to   IS NULL OR last_sale_date <= @date_to::date)
              AND (@item_group IS NULL OR item_group_code = ANY(string_to_array(@item_group, ',')))
            GROUP BY item_group_code
            ORDER BY SUM(gross_sales) DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesItemGroupSummaryDto>(
            new CommandDefinition(sql, new
            {
                company_id  = companyId,
                date_from   = dateFrom.HasValue ? (object)dateFrom.Value.ToString("yyyy-MM-dd") : null,
                date_to     = dateTo.HasValue ? (object)dateTo.Value.ToString("yyyy-MM-dd") : null,
                item_group  = string.IsNullOrWhiteSpace(filters?.ItemGroupCodes) ? null : filters!.ItemGroupCodes,
            }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SalesWarehouseSummaryDto>> GetSalesWarehouseSummaryAsync(
        string companyId, NativeBiFilterDto? filters = null, CancellationToken ct = default)
    {
        var (dateFrom, dateTo) = filters?.EffectiveDateRange() ?? (null, null);

        const string sql = """
            SELECT
                warehouse_code         AS "WarehouseCode",
                MAX(warehouse_name)    AS "WarehouseName",
                SUM(gross_sales_amount)  AS "GrossSales",
                SUM(net_sales_amount)    AS "NetSales",
                COUNT(DISTINCT CONCAT(invoice_doc_num::text, '_', invoice_line_num::text)) AS "InvoiceCount",
                COUNT(DISTINCT item_code)  AS "SkuCount"
            FROM mart.sales_item_dashboard
            WHERE company_id = @companyId
              AND warehouse_code IS NOT NULL
              AND warehouse_code <> ''
              AND (@dateFrom IS NULL OR invoice_date >= @dateFrom::date)
              AND (@dateTo   IS NULL OR invoice_date <= @dateTo::date)
              AND (@itemGroupCodes IS NULL OR item_group_code = ANY(string_to_array(@itemGroupCodes, ',')))
            GROUP BY warehouse_code
            ORDER BY SUM(net_sales_amount) DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesWarehouseSummaryDto>(
            new CommandDefinition(sql, new
            {
                companyId,
                dateFrom       = dateFrom.HasValue ? (object)dateFrom.Value.ToString("yyyy-MM-dd") : null,
                dateTo         = dateTo.HasValue ? (object)dateTo.Value.ToString("yyyy-MM-dd") : null,
                itemGroupCodes = string.IsNullOrWhiteSpace(filters?.ItemGroupCodes) ? null : filters!.ItemGroupCodes,
            }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SalesFulfillmentDto>> GetSalesFulfillmentAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_date         AS "PeriodDate",
                orders_count        AS "OrdersCount",
                orders_amount       AS "OrdersAmount",
                delivered_count     AS "DeliveredCount",
                delivered_amount    AS "DeliveredAmount",
                fill_rate_pct       AS "FillRatePct",
                pending_orders      AS "PendingOrders"
            FROM mart.sales_fulfillment_dashboard
            WHERE company_id = @company_id
              AND period_date >= CURRENT_DATE - @days
            ORDER BY period_date DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<SalesFulfillmentRow>(
            new CommandDefinition(sql, new { company_id = companyId, days }, cancellationToken: ct));
        return rows.Select(r => new SalesFulfillmentDto
        {
            PeriodDate      = DateOnly.FromDateTime(r.PeriodDate),
            OrdersCount     = r.OrdersCount,
            OrdersAmount    = r.OrdersAmount,
            DeliveredCount  = r.DeliveredCount,
            DeliveredAmount = r.DeliveredAmount,
            FillRatePct     = r.FillRatePct,
            PendingOrders   = r.PendingOrders,
        }).ToList();
    }

    // ── FINANCE ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FinanceExecutiveDto>> GetFinanceExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_date             AS "PeriodDate",
                ar_total                AS "ArTotal",
                ar_overdue              AS "ArOverdue",
                ar_overdue_pct          AS "ArOverduePct",
                ap_total                AS "ApTotal",
                ap_overdue              AS "ApOverdue",
                new_invoices_count      AS "NewInvoicesCount",
                new_invoices_amount     AS "NewInvoicesAmount"
            FROM mart.finance_executive_daily
            WHERE company_id = @company_id
              AND period_date >= CURRENT_DATE - @days
            ORDER BY period_date DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<FinanceExecutiveRow>(
            new CommandDefinition(sql, new { company_id = companyId, days }, cancellationToken: ct));
        return rows.Select(r => new FinanceExecutiveDto
        {
            PeriodDate         = DateOnly.FromDateTime(r.PeriodDate),
            ArTotal            = r.ArTotal,
            ArOverdue          = r.ArOverdue,
            ArOverduePct       = r.ArOverduePct,
            ApTotal            = r.ApTotal,
            ApOverdue          = r.ApOverdue,
            NewInvoicesCount   = r.NewInvoicesCount,
            NewInvoicesAmount  = r.NewInvoicesAmount,
        }).ToList();
    }

    public async Task<IReadOnlyList<FinanceArAgingDto>> GetFinanceArAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var col = ResolveCol(p.SortBy, ArAgingSortCols, "overdue_amount");
        var dir = ResolveDir(p.SortDir);
        var sql = $"""
            SELECT
                card_code           AS "CardCode",
                card_name           AS "CardName",
                invoice_count       AS "InvoiceCount",
                total_amount        AS "TotalAmount",
                balance_due         AS "BalanceDue",
                overdue_amount      AS "OverdueAmount",
                aging_0_30          AS "Aging0To30",
                aging_31_60         AS "Aging31To60",
                aging_61_90         AS "Aging61To90",
                aging_90_plus       AS "Aging90Plus",
                last_invoice_date   AS "LastInvoiceDate",
                oldest_overdue_date AS "OldestOverdueDate"
            FROM mart.finance_ar_aging_dashboard
            WHERE company_id = @company_id
            ORDER BY {col} {dir}
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<FinanceArAgingRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new FinanceArAgingDto
        {
            CardCode           = r.CardCode,
            CardName           = r.CardName,
            InvoiceCount       = r.InvoiceCount,
            TotalAmount        = r.TotalAmount,
            BalanceDue         = r.BalanceDue,
            OverdueAmount      = r.OverdueAmount,
            Aging0To30         = r.Aging0To30,
            Aging31To60        = r.Aging31To60,
            Aging61To90        = r.Aging61To90,
            Aging90Plus        = r.Aging90Plus,
            LastInvoiceDate    = r.LastInvoiceDate.HasValue ? DateOnly.FromDateTime(r.LastInvoiceDate.Value) : null,
            OldestOverdueDate  = r.OldestOverdueDate.HasValue ? DateOnly.FromDateTime(r.OldestOverdueDate.Value) : null,
        }).ToList();
    }

    public async Task<IReadOnlyList<FinanceApAgingDto>> GetFinanceApAgingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                supplier_code   AS "SupplierCode",
                supplier_name   AS "SupplierName",
                invoice_count   AS "InvoiceCount",
                balance_due     AS "BalanceDue",
                overdue_amount  AS "OverdueAmount",
                aging_0_30      AS "Aging0To30",
                aging_31_60     AS "Aging31To60",
                aging_61_90     AS "Aging61To90",
                aging_90_plus   AS "Aging90Plus"
            FROM mart.finance_ap_aging_dashboard
            WHERE company_id = @company_id
            ORDER BY overdue_amount DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<FinanceApAgingDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.ToList();
    }

    // ── INVENTORY ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<InventoryRotationDto>> GetInventoryRotationAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        var col = ResolveCol(p.SortBy, RotationSortCols, "qty_sold_30d");
        var dir = ResolveDir(p.SortDir);
        var sql = $"""
            SELECT
                item_code               AS "ItemCode",
                item_name               AS "ItemName",
                item_group_code         AS "ItemGroupCode",
                qty_sold_30d            AS "QtySold30d",
                qty_sold_90d            AS "QtySold90d",
                last_sale_date          AS "LastSaleDate",
                avg_daily_sales_qty     AS "AvgDailySalesQty",
                on_hand_qty             AS "OnHandQty",
                coverage_days           AS "CoverageDays",
                rotation_status         AS "RotationStatus"
            FROM mart.inventory_rotation_dashboard
            WHERE company_id = @company_id
            ORDER BY {col} {dir}
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<InventoryRotationRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new InventoryRotationDto
        {
            ItemCode           = r.ItemCode,
            ItemName           = r.ItemName,
            ItemGroupCode      = r.ItemGroupCode,
            QtySold30d         = r.QtySold30d,
            QtySold90d         = r.QtySold90d,
            LastSaleDate       = r.LastSaleDate.HasValue ? DateOnly.FromDateTime(r.LastSaleDate.Value) : null,
            AvgDailySalesQty   = r.AvgDailySalesQty,
            OnHandQty          = r.OnHandQty,
            CoverageDays       = r.CoverageDays,
            RotationStatus     = r.RotationStatus,
        }).ToList();
    }

    public async Task<IReadOnlyList<InventoryStockDto>> GetInventoryStockAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                warehouse_code  AS "WarehouseCode",
                item_code       AS "ItemCode",
                item_name       AS "ItemName",
                item_group_code AS "ItemGroupCode",
                on_hand_qty     AS "OnHandQty",
                available_qty   AS "AvailableQty",
                stock_value     AS "StockValue",
                is_stockout     AS "IsStockout"
            FROM mart.inventory_stock_dashboard
            WHERE company_id = @company_id
            ORDER BY is_stockout DESC, available_qty ASC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<InventoryStockDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<InventoryWarehouseDto>> GetInventoryWarehousesAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                warehouse_code      AS "WarehouseCode",
                warehouse_name      AS "WarehouseName",
                transfer_in_count   AS "TransferInCount",
                transfer_in_qty     AS "TransferInQty",
                transfer_out_count  AS "TransferOutCount",
                transfer_out_qty    AS "TransferOutQty",
                last_transfer_date  AS "LastTransferDate"
            FROM mart.inventory_warehouse_dashboard
            WHERE company_id = @company_id
            ORDER BY warehouse_code;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<InventoryWarehouseRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
        return rows.Select(r => new InventoryWarehouseDto
        {
            WarehouseCode   = r.WarehouseCode,
            WarehouseName   = r.WarehouseName,
            TransferInCount  = r.TransferInCount,
            TransferInQty    = r.TransferInQty,
            TransferOutCount = r.TransferOutCount,
            TransferOutQty   = r.TransferOutQty,
            LastTransferDate = r.LastTransferDate.HasValue ? DateOnly.FromDateTime(r.LastTransferDate.Value) : null,
        }).ToList();
    }

    // ── PURCHASING ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PurchasingExecutiveDto>> GetPurchasingExecutiveAsync(
        string companyId, int days, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                purchase_date       AS "PurchaseDate",
                po_count            AS "PoCount",
                po_amount           AS "PoAmount",
                received_count      AS "ReceivedCount",
                received_amount     AS "ReceivedAmount",
                active_suppliers    AS "ActiveSuppliers"
            FROM mart.purchase_executive_daily
            WHERE company_id = @company_id
              AND purchase_date >= CURRENT_DATE - @days
            ORDER BY purchase_date DESC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<PurchasingExecutiveRow>(
            new CommandDefinition(sql, new { company_id = companyId, days }, cancellationToken: ct));
        return rows.Select(r => new PurchasingExecutiveDto
        {
            PurchaseDate    = DateOnly.FromDateTime(r.PurchaseDate),
            PoCount         = r.PoCount,
            PoAmount        = r.PoAmount,
            ReceivedCount   = r.ReceivedCount,
            ReceivedAmount  = r.ReceivedAmount,
            ActiveSuppliers = r.ActiveSuppliers,
        }).ToList();
    }

    public async Task<IReadOnlyList<PurchasingSupplierDto>> GetPurchasingSuppliersAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                supplier_code   AS "SupplierCode",
                supplier_name   AS "SupplierName",
                po_count        AS "PoCount",
                po_amount       AS "PoAmount",
                received_amount AS "ReceivedAmount",
                avg_po_amount   AS "AvgPoAmount",
                last_po_date    AS "LastPoDate"
            FROM mart.purchase_supplier_dashboard
            WHERE company_id = @company_id
            ORDER BY po_amount DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<PurchasingSupplierRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new PurchasingSupplierDto
        {
            SupplierCode    = r.SupplierCode,
            SupplierName    = r.SupplierName,
            PoCount         = r.PoCount,
            PoAmount        = r.PoAmount,
            ReceivedAmount  = r.ReceivedAmount,
            AvgPoAmount     = r.AvgPoAmount,
            LastPoDate      = r.LastPoDate.HasValue ? DateOnly.FromDateTime(r.LastPoDate.Value) : null,
        }).ToList();
    }

    public async Task<IReadOnlyList<PurchasingReceivingDto>> GetPurchasingReceivingAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                supplier_code   AS "SupplierCode",
                supplier_name   AS "SupplierName",
                gr_count        AS "GrCount",
                gr_amount       AS "GrAmount",
                last_gr_date    AS "LastGrDate"
            FROM mart.purchase_receiving_dashboard
            WHERE company_id = @company_id
            ORDER BY gr_amount DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<PurchasingReceivingRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new PurchasingReceivingDto
        {
            SupplierCode = r.SupplierCode,
            SupplierName = r.SupplierName,
            GrCount      = r.GrCount,
            GrAmount     = r.GrAmount,
            LastGrDate   = r.LastGrDate.HasValue ? DateOnly.FromDateTime(r.LastGrDate.Value) : null,
        }).ToList();
    }

    // ── OPERATIONS ───────────────────────────────────────────────────────────

    public async Task<OperationHealthDto?> GetPipelineHealthAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                last_extractor_run_utc  AS "LastExtractorRunUtc",
                last_transform_run_utc  AS "LastTransformRunUtc",
                extractor_status        AS "ExtractorStatus",
                transform_status        AS "TransformStatus",
                active_alerts           AS "ActiveAlerts",
                dq_errors_unresolved    AS "DqErrorsUnresolved",
                objects_extracted       AS "ObjectsExtracted",
                health_score            AS "HealthScore",
                updated_at_utc          AS "UpdatedAtUtc"
            FROM ops.pipeline_health
            WHERE company_id = @company_id;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<OperationHealthRow>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
        if (row is null) return null;
        return new OperationHealthDto
        {
            LastExtractorRunUtc = row.LastExtractorRunUtc?.ToString("o"),
            LastTransformRunUtc = row.LastTransformRunUtc?.ToString("o"),
            ExtractorStatus     = row.ExtractorStatus,
            TransformStatus     = row.TransformStatus,
            ActiveAlerts        = row.ActiveAlerts,
            DqErrorsUnresolved  = row.DqErrorsUnresolved,
            ObjectsExtracted    = row.ObjectsExtracted,
            HealthScore         = row.HealthScore,
            UpdatedAtUtc        = row.UpdatedAtUtc?.ToString("o"),
        };
    }

    public async Task<IReadOnlyList<OperationAlertDto>> GetActiveAlertsAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id                  AS "Id",
                rule_code           AS "RuleCode",
                severity            AS "Severity",
                triggered_value     AS "TriggeredValue",
                message             AS "Message",
                triggered_at_utc    AS "TriggeredAtUtc",
                is_resolved         AS "IsResolved"
            FROM ops.alert_event
            WHERE company_id = @company_id
              AND is_resolved = FALSE
            ORDER BY triggered_at_utc DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<OperationAlertRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new OperationAlertDto
        {
            Id              = r.Id,
            RuleCode        = r.RuleCode,
            Severity        = r.Severity,
            TriggeredValue  = r.TriggeredValue,
            Message         = r.Message,
            TriggeredAtUtc  = r.TriggeredAtUtc.ToString("o"),
            IsResolved      = r.IsResolved,
        }).ToList();
    }

    public async Task<IReadOnlyList<OperationDataQualityDto>> GetDataQualityIssuesAsync(
        string companyId, PaginationOptions p, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                id                  AS "Id",
                sap_object          AS "SapObject",
                issue_type          AS "IssueType",
                severity            AS "Severity",
                description         AS "Description",
                affected_rows       AS "AffectedRows",
                sample_key          AS "SampleKey",
                detected_at_utc     AS "DetectedAtUtc",
                is_resolved         AS "IsResolved"
            FROM ops.data_quality_issue
            WHERE company_id = @company_id
              AND is_resolved = FALSE
            ORDER BY detected_at_utc DESC
            LIMIT @limit OFFSET @offset;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<OperationDataQualityRow>(
            new CommandDefinition(sql, new { company_id = companyId, limit = p.Limit, offset = p.Offset }, cancellationToken: ct));
        return rows.Select(r => new OperationDataQualityDto
        {
            Id             = r.Id,
            SapObject      = r.SapObject,
            IssueType      = r.IssueType,
            Severity       = r.Severity,
            Description    = r.Description,
            AffectedRows   = r.AffectedRows,
            SampleKey      = r.SampleKey,
            DetectedAtUtc  = r.DetectedAtUtc.ToString("o"),
            IsResolved     = r.IsResolved,
        }).ToList();
    }

    // ── FINANCE ACCOUNTING ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<IncomeStatementPeriodDto>> GetIncomeStatementAsync(
        string companyId, int? year, int? month, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year    AS "PeriodYear",
                period_month   AS "PeriodMonth",
                statement_line AS "StatementLine",
                ROUND(COALESCE(amount, 0), 4) AS "Amount"
            FROM mart.income_statement_summary
            WHERE company_id = @company_id
              AND (@year  IS NULL OR period_year  = @year)
              AND (@month IS NULL OR period_month = @month)
            ORDER BY period_year, period_month, statement_line;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<IncomeStatementRawRow>(
            new CommandDefinition(sql, new { company_id = companyId, year, month }, cancellationToken: ct));

        return rows
            .GroupBy(r => (r.PeriodYear, r.PeriodMonth))
            .Select(g =>
            {
                var rev    = g.FirstOrDefault(r => r.StatementLine == "revenue")?.Amount ?? 0;
                var cogs   = g.FirstOrDefault(r => r.StatementLine == "cogs")?.Amount ?? 0;
                var opex   = g.FirstOrDefault(r => r.StatementLine == "opex")?.Amount ?? 0;
                var fin    = g.FirstOrDefault(r => r.StatementLine == "financial")?.Amount ?? 0;
                var tax    = g.FirstOrDefault(r => r.StatementLine == "tax")?.Amount ?? 0;
                var gp     = rev - cogs;
                var oi     = gp - opex;
                var ni     = oi - fin - tax;

                return new IncomeStatementPeriodDto
                {
                    PeriodYear       = g.Key.PeriodYear,
                    PeriodMonth      = g.Key.PeriodMonth,
                    Revenue          = rev,
                    Cogs             = cogs,
                    GrossProfit      = gp,
                    GrossProfitPct   = rev != 0 ? Math.Round(gp  / rev * 100, 2) : 0,
                    Opex             = opex,
                    OperatingIncome  = oi,
                    OperatingPct     = rev != 0 ? Math.Round(oi  / rev * 100, 2) : 0,
                    Financial        = fin,
                    Tax              = tax,
                    NetIncome        = ni,
                    NetPct           = rev != 0 ? Math.Round(ni  / rev * 100, 2) : 0,
                    Lines            = g.Select(r => new IncomeStatementLineDto
                    {
                        StatementLine = r.StatementLine,
                        Amount        = r.Amount,
                        PctOfRevenue  = rev != 0 ? Math.Round(r.Amount / rev * 100, 2) : 0,
                    }).ToList(),
                };
            })
            .OrderBy(d => d.PeriodYear).ThenBy(d => d.PeriodMonth)
            .ToList();
    }

    public async Task<IReadOnlyList<BalanceSheetSnapshotDto>> GetBalanceSheetAsync(
        string companyId, string? snapshotDate, CancellationToken ct = default)
    {
        const string latestDateSql = """
            SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = @company_id;
            """;
        const string rowsSql = """
            SELECT
                snapshot_date::TEXT AS "SnapshotDate",
                category            AS "Category",
                sub_category        AS "SubCategory",
                ROUND(COALESCE(amount, 0), 4) AS "Amount"
            FROM mart.balance_sheet_summary
            WHERE company_id   = @company_id
              AND snapshot_date = @snapshot_date::DATE
            ORDER BY category, sub_category;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var effectiveDate = snapshotDate
            ?? (await conn.QueryFirstOrDefaultAsync<string?>(
                new CommandDefinition(latestDateSql, new { company_id = companyId }, cancellationToken: ct)));

        if (effectiveDate is null) return [];

        var rows = await conn.QueryAsync<BalanceSheetRawRow>(
            new CommandDefinition(rowsSql, new { company_id = companyId, snapshot_date = effectiveDate }, cancellationToken: ct));

        var list = rows.ToList();
        if (list.Count == 0) return [];

        var entries = list.Select(r => new BalanceSheetEntryDto
        {
            Category    = r.Category,
            SubCategory = r.SubCategory,
            Amount      = r.Amount,
        }).ToList();

        var totalAssets = list
            .Where(r => r.Category is "current_assets" or "non_current_assets")
            .Sum(r => r.Amount);
        var totalLiab = list
            .Where(r => r.Category is "current_liabilities" or "non_current_liabilities")
            .Sum(r => r.Amount);
        var totalEquity = list
            .Where(r => r.Category == "equity")
            .Sum(r => r.Amount);

        return [new BalanceSheetSnapshotDto
        {
            SnapshotDate      = effectiveDate,
            TotalAssets       = totalAssets,
            TotalLiabilities  = totalLiab,
            TotalEquity       = totalEquity,
            Imbalance         = totalAssets - totalLiab - totalEquity,
            Entries           = entries,
        }];
    }

    public async Task<IReadOnlyList<EbitdaPeriodDto>> GetEbitdaAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year        AS "PeriodYear",
                period_month       AS "PeriodMonth",
                ROUND(revenue,       4) AS "Revenue",
                ROUND(cogs,          4) AS "Cogs",
                ROUND(gross_profit,  4) AS "GrossProfit",
                ROUND(opex,          4) AS "Opex",
                ROUND(ebitda,        4) AS "Ebitda",
                ROUND(depreciation,  4) AS "Depreciation",
                ROUND(amortization,  4) AS "Amortization",
                ROUND(financial_result, 4) AS "FinancialResult",
                ROUND(tax_result,    4) AS "TaxResult",
                ROUND(net_income,    4) AS "NetIncome"
            FROM mart.ebitda_summary
            WHERE company_id = @company_id
            ORDER BY period_year DESC, period_month DESC
            LIMIT @months;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<EbitdaRawRow>(
            new CommandDefinition(sql, new { company_id = companyId, months }, cancellationToken: ct));

        return rows.Select(r => new EbitdaPeriodDto
        {
            PeriodYear      = r.PeriodYear,
            PeriodMonth     = r.PeriodMonth,
            Revenue         = r.Revenue,
            Cogs            = r.Cogs,
            GrossProfit     = r.GrossProfit,
            Opex            = r.Opex,
            Ebitda          = r.Ebitda,
            Depreciation    = r.Depreciation,
            Amortization    = r.Amortization,
            FinancialResult = r.FinancialResult,
            TaxResult       = r.TaxResult,
            NetIncome       = r.NetIncome,
            EbitdaMargin    = r.Revenue != 0 ? Math.Round(r.Ebitda    / r.Revenue * 100, 2) : 0,
            NetMargin       = r.Revenue != 0 ? Math.Round(r.NetIncome / r.Revenue * 100, 2) : 0,
        }).OrderBy(d => d.PeriodYear).ThenBy(d => d.PeriodMonth).ToList();
    }

    public async Task<IReadOnlyList<ChartOfAccountEntryDto>> GetChartOfAccountsAsync(
        string companyId, bool postableOnly, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                ga.code             AS "Code",
                ga.name             AS "Name",
                ga.father_num       AS "FatherNum",
                ga.level            AS "Level",
                ga.account_type     AS "AccountType",
                ga.statement_line   AS "StatementLine",
                ga.postable         AS "Postable",
                ROUND(COALESCE(SUM(ab.debit_sum - ab.credit_sum), 0), 4) AS "Balance"
            FROM mart.gl_accounts ga
            LEFT JOIN mart.account_balances ab
                   ON ab.company_id = ga.company_id AND ab.code = ga.code
            WHERE ga.company_id = @company_id
              AND (@postable_only = FALSE OR ga.postable = TRUE)
            GROUP BY ga.code, ga.name, ga.father_num, ga.level,
                     ga.account_type, ga.statement_line, ga.postable
            ORDER BY ga.code;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<ChartOfAccountEntryDto>(
            new CommandDefinition(sql, new { company_id = companyId, postable_only = postableOnly }, cancellationToken: ct));
        return rows.ToList();
    }

    // ── FINANCE VALIDATION (Sprint 14C) ──────────────────────────────────────

    public async Task<FinanceValidationSummaryDto> GetFinanceValidationsAsync(
        string companyId, CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        // 1. Last period available in income statement
        var lastPeriod = await conn.QueryFirstOrDefaultAsync<(int? year, int? month)>(
            new CommandDefinition(
                """
                SELECT MAX(period_year) AS year, MAX(period_month) AS month
                FROM mart.income_statement_summary
                WHERE company_id = @company_id
                """,
                new { company_id = companyId }, cancellationToken: ct));

        // 2. Unclassified postable accounts in mart.gl_accounts
        var unclassified = await conn.QueryFirstOrDefaultAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)::INT FROM mart.gl_accounts
                WHERE company_id = @company_id
                  AND postable = TRUE
                  AND (statement_line IS NULL OR statement_line = 'unclassified')
                """,
                new { company_id = companyId }, cancellationToken: ct));

        // 3. Balance imbalance from latest snapshot
        var balRow = await conn.QueryFirstOrDefaultAsync<BalanceCheckRow>(
            new CommandDefinition(
                """
                SELECT
                    snapshot_date::TEXT AS snapshot_date,
                    SUM(CASE WHEN category IN ('current_assets','non_current_assets')       THEN amount ELSE 0 END) AS total_assets,
                    SUM(CASE WHEN category IN ('current_liabilities','non_current_liabilities') THEN amount ELSE 0 END) AS total_liabilities,
                    SUM(CASE WHEN category = 'equity' THEN amount ELSE 0 END) AS total_equity
                FROM mart.balance_sheet_summary
                WHERE company_id  = @company_id
                  AND snapshot_date = (SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = @company_id)
                GROUP BY snapshot_date
                """,
                new { company_id = companyId }, cancellationToken: ct));

        // 4. Orphan journal lines (jdt1 rows with no matching ojdt)
        var orphanLines = await conn.QueryFirstOrDefaultAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)::INT
                FROM stg.sap_jdt1 j
                WHERE j.company_id = @company_id
                  AND NOT EXISTS (
                      SELECT 1 FROM stg.sap_ojdt o
                      WHERE o.company_id = j.company_id
                        AND o."TransId"  = j."TransId"
                  )
                """,
                new { company_id = companyId }, cancellationToken: ct));

        // 5. Negative revenue periods
        var negativeRevenue = await conn.QueryFirstOrDefaultAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)::INT
                FROM mart.income_statement_summary
                WHERE company_id   = @company_id
                  AND statement_line = 'revenue'
                  AND amount < 0
                """,
                new { company_id = companyId }, cancellationToken: ct));

        // 6. Income statement rows total (to detect no-data state)
        var incomeRows = await conn.QueryFirstOrDefaultAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*)::INT FROM mart.income_statement_summary WHERE company_id = @company_id",
                new { company_id = companyId }, cancellationToken: ct));

        // ── Build issues list ─────────────────────────────────────────────────

        var issues = new List<FinanceValidationIssueDto>();

        if (incomeRows == 0)
        {
            issues.Add(new FinanceValidationIssueDto
            {
                Severity    = "critical",
                IssueType   = "no_data",
                Title       = "Sin datos contables",
                Description = "No hay períodos en mart.income_statement_summary. Verifique que OACT/OJDT fueron extraídos y que se ejecutó refresh_accounting_all.",
                Count       = 0,
            });
        }

        if (unclassified > 0)
        {
            issues.Add(new FinanceValidationIssueDto
            {
                Severity    = unclassified > 10 ? "critical" : "warning",
                IssueType   = "unclassified_accounts",
                Title       = $"{unclassified} cuentas sin clasificar",
                Description = "Cuentas postables en mart.gl_accounts con statement_line = 'unclassified' o NULL. Clasifíquelas en SuperAdmin → Native BI → Clasificación Contable.",
                Count       = unclassified,
            });
        }

        if (balRow is not null)
        {
            var imbalance = Math.Abs(balRow.TotalAssets - balRow.TotalLiabilities - balRow.TotalEquity);
            if (imbalance > 0.01m)
            {
                issues.Add(new FinanceValidationIssueDto
                {
                    Severity    = imbalance > 1000 ? "critical" : "warning",
                    IssueType   = "balance_imbalance",
                    Title       = $"Balance desbalanceado: {imbalance:N2}",
                    Description = "Activos ≠ Pasivos + Patrimonio. Verifique clasificación de cuentas de balance y re-ejecute el ETL.",
                    Count       = 1,
                    Period      = balRow.SnapshotDate,
                });
            }
        }

        if (orphanLines > 0)
        {
            issues.Add(new FinanceValidationIssueDto
            {
                Severity    = orphanLines > 100 ? "warning" : "info",
                IssueType   = "orphan_lines",
                Title       = $"{orphanLines} líneas de diario huérfanas",
                Description = "Líneas en stg.sap_jdt1 sin cabecera en stg.sap_ojdt. Puede indicar extracción incompleta.",
                Count       = orphanLines,
            });
        }

        if (negativeRevenue > 0)
        {
            issues.Add(new FinanceValidationIssueDto
            {
                Severity    = "warning",
                IssueType   = "negative_revenue",
                Title       = $"{negativeRevenue} período(s) con revenue negativo",
                Description = "El importe de 'revenue' es negativo en algunos períodos. Verifique el signo de las cuentas de ingreso.",
                Count       = negativeRevenue,
            });
        }

        // ── Health score (100 - deductions) ──────────────────────────────────

        var score = 100;
        if (incomeRows == 0)         score -= 50;
        if (unclassified > 10)       score -= 30;
        else if (unclassified > 0)   score -= 10;
        var bal = balRow is not null ? Math.Abs(balRow.TotalAssets - balRow.TotalLiabilities - balRow.TotalEquity) : 0m;
        if (bal > 1000)              score -= 20;
        else if (bal > 0.01m)        score -= 5;
        if (orphanLines > 100)       score -= 10;
        if (negativeRevenue > 0)     score -= 5;
        score = Math.Max(0, score);

        var critical = issues.Count(i => i.Severity == "critical");
        var warning  = issues.Count(i => i.Severity == "warning");
        var info     = issues.Count(i => i.Severity == "info");

        var healthStatus = critical > 0 ? "critical" : warning > 0 ? "warning" : "ok";

        FinanceReconciliationDto? reconciliation = null;
        if (balRow is not null)
        {
            var imb = balRow.TotalAssets - balRow.TotalLiabilities - balRow.TotalEquity;
            reconciliation = new FinanceReconciliationDto
            {
                SnapshotDate     = balRow.SnapshotDate,
                TotalAssets      = balRow.TotalAssets,
                TotalLiabilities = balRow.TotalLiabilities,
                TotalEquity      = balRow.TotalEquity,
                Imbalance        = Math.Abs(imb),
                IsBalanced       = Math.Abs(imb) <= 0.01m,
            };
        }

        return new FinanceValidationSummaryDto
        {
            HealthScore          = score,
            HealthStatus         = healthStatus,
            CriticalIssues       = critical,
            WarningIssues        = warning,
            InfoIssues           = info,
            LastPeriodValidated  = lastPeriod.year.HasValue ? $"{lastPeriod.year:D4}-{lastPeriod.month:D2}" : null,
            BalanceImbalance     = reconciliation?.Imbalance ?? 0,
            UnclassifiedAccounts = unclassified,
            OrphanJournalLines   = orphanLines,
            Issues               = issues,
            Reconciliation       = reconciliation,
        };
    }

    // ── FINANCE READINESS (Sprint 14E) ───────────────────────────────────────

    public async Task<FinanceReadinessDto> GetFinanceReadinessAsync(
        string companyId, CancellationToken ct = default)
    {
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        static Task<int> Count(Npgsql.NpgsqlConnection c, string sql, string cid, CancellationToken token)
            => c.QueryFirstOrDefaultAsync<int>(new Dapper.CommandDefinition(sql, new { company_id = cid }, cancellationToken: token));

        // Parallel counts from each layer
        var rawOact = await Count(conn,
            "SELECT COUNT(*)::INT FROM raw.sap_oact   WHERE company_id = @company_id", companyId, ct);
        var rawOjdt = await Count(conn,
            "SELECT COUNT(*)::INT FROM raw.sap_ojdt   WHERE company_id = @company_id", companyId, ct);
        var rawJdt1 = await Count(conn,
            "SELECT COUNT(*)::INT FROM raw.sap_jdt1   WHERE company_id = @company_id", companyId, ct);
        var stgOact = await Count(conn,
            "SELECT COUNT(*)::INT FROM stg.sap_oact   WHERE company_id = @company_id", companyId, ct);
        var stgOjdt = await Count(conn,
            "SELECT COUNT(*)::INT FROM stg.sap_ojdt   WHERE company_id = @company_id", companyId, ct);
        var stgJdt1 = await Count(conn,
            "SELECT COUNT(*)::INT FROM stg.sap_jdt1   WHERE company_id = @company_id", companyId, ct);
        var martGl = await Count(conn,
            "SELECT COUNT(*)::INT FROM mart.gl_accounts WHERE company_id = @company_id", companyId, ct);
        var martIs = await Count(conn,
            "SELECT COUNT(*)::INT FROM mart.income_statement_summary WHERE company_id = @company_id", companyId, ct);
        var martBs = await Count(conn,
            "SELECT COUNT(*)::INT FROM mart.balance_sheet_summary    WHERE company_id = @company_id", companyId, ct);
        var martEb = await Count(conn,
            "SELECT COUNT(*)::INT FROM mart.ebitda_summary           WHERE company_id = @company_id", companyId, ct);
        var clsRules = await Count(conn,
            "SELECT COUNT(*)::INT FROM cfg.account_classification_rules WHERE company_id = @company_id", companyId, ct);
        var unclassified = await Count(conn,
            "SELECT COUNT(*)::INT FROM mart.gl_accounts WHERE company_id = @company_id AND postable = TRUE AND (statement_line IS NULL OR statement_line = 'unclassified')", companyId, ct);

        // Determine readiness
        var blocking = new List<string>();
        var warnings = new List<string>();

        if (rawOact == 0)    blocking.Add("OACT no extraído — ejecutar extractor con --object OACT");
        if (rawOjdt == 0)    blocking.Add("OJDT no extraído — ejecutar extractor con --object OJDT");
        if (rawJdt1 == 0)    blocking.Add("JDT1 no extraído — ejecutar extractor con --object JDT1");
        if (martGl   == 0)   blocking.Add("mart.gl_accounts vacía — ejecutar mart.refresh_accounting_all()");
        if (martIs   == 0)   blocking.Add("mart.income_statement_summary vacía — ejecutar mart.refresh_accounting_all()");
        if (clsRules == 0)   warnings.Add("Sin reglas de clasificación contable — configure en SuperAdmin → Native BI");
        if (unclassified > 0) warnings.Add($"{unclassified} cuenta(s) postable(s) sin clasificar — revisar con contador");
        if (martBs   == 0 && rawOact > 0) warnings.Add("mart.balance_sheet_summary vacía — re-ejecutar refresh_accounting_all()");

        var status = blocking.Count > 0 ? "blocked" : warnings.Count > 0 ? "warning" : "ready";

        return new FinanceReadinessDto
        {
            RawOactCount         = rawOact,
            RawOjdtCount         = rawOjdt,
            RawJdt1Count         = rawJdt1,
            StgOactCount         = stgOact,
            StgOjdtCount         = stgOjdt,
            StgJdt1Count         = stgJdt1,
            MartGlAccounts       = martGl,
            MartIncomeStatement  = martIs,
            MartBalanceSheet     = martBs,
            MartEbitda           = martEb,
            ClassificationRules  = clsRules,
            UnclassifiedPostable = unclassified,
            ReadinessStatus      = status,
            BlockingReasons      = blocking,
            Warnings             = warnings,
        };
    }

    // ── Private row types (Dapper mapping targets) ────────────────────────────

    private sealed class SalesCustomerDashboardRow
    {
        public string CardCode { get; init; } = "";
        public string? CardName { get; init; }
        public string? CardType { get; init; }
        public string? SalespersonName { get; init; }
        public decimal GrossSales { get; init; }
        public decimal CreditMemos { get; init; }
        public decimal NetSales { get; init; }
        public int InvoiceCount { get; init; }
        public decimal AvgTicket { get; init; }
        public DateTime? LastInvoiceDate { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class SalesItemDashboardRow
    {
        public string ItemCode { get; init; } = "";
        public string? ItemName { get; init; }
        public string? ItemGroupCode { get; init; }
        public decimal QuantitySold { get; init; }
        public decimal GrossSales { get; init; }
        public decimal? GrossMarginPct { get; init; }
        public int InvoiceCount { get; init; }
        public DateTime? LastSaleDate { get; init; }
    }

    private sealed class SalesFulfillmentRow
    {
        public DateTime PeriodDate { get; init; }
        public int OrdersCount { get; init; }
        public decimal OrdersAmount { get; init; }
        public int DeliveredCount { get; init; }
        public decimal DeliveredAmount { get; init; }
        public decimal? FillRatePct { get; init; }
        public int PendingOrders { get; init; }
    }

    private sealed class FinanceExecutiveRow
    {
        public DateTime PeriodDate { get; init; }
        public decimal ArTotal { get; init; }
        public decimal ArOverdue { get; init; }
        public decimal ArOverduePct { get; init; }
        public decimal? ApTotal { get; init; }
        public decimal? ApOverdue { get; init; }
        public int NewInvoicesCount { get; init; }
        public decimal NewInvoicesAmount { get; init; }
    }

    private sealed class FinanceArAgingRow
    {
        public string CardCode { get; init; } = "";
        public string? CardName { get; init; }
        public int InvoiceCount { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal BalanceDue { get; init; }
        public decimal OverdueAmount { get; init; }
        public decimal Aging0To30 { get; init; }
        public decimal Aging31To60 { get; init; }
        public decimal Aging61To90 { get; init; }
        public decimal Aging90Plus { get; init; }
        public DateTime? LastInvoiceDate { get; init; }
        public DateTime? OldestOverdueDate { get; init; }
    }

    private sealed class InventoryRotationRow
    {
        public string ItemCode { get; init; } = "";
        public string? ItemName { get; init; }
        public string? ItemGroupCode { get; init; }
        public decimal QtySold30d { get; init; }
        public decimal QtySold90d { get; init; }
        public DateTime? LastSaleDate { get; init; }
        public decimal AvgDailySalesQty { get; init; }
        public decimal? OnHandQty { get; init; }
        public decimal? CoverageDays { get; init; }
        public string RotationStatus { get; init; } = "";
    }

    private sealed class InventoryWarehouseRow
    {
        public string WarehouseCode { get; init; } = "";
        public string? WarehouseName { get; init; }
        public int TransferInCount { get; init; }
        public decimal TransferInQty { get; init; }
        public int TransferOutCount { get; init; }
        public decimal TransferOutQty { get; init; }
        public DateTime? LastTransferDate { get; init; }
    }

    private sealed class PurchasingExecutiveRow
    {
        public DateTime PurchaseDate { get; init; }
        public int PoCount { get; init; }
        public decimal PoAmount { get; init; }
        public int ReceivedCount { get; init; }
        public decimal ReceivedAmount { get; init; }
        public int ActiveSuppliers { get; init; }
    }

    private sealed class PurchasingSupplierRow
    {
        public string SupplierCode { get; init; } = "";
        public string? SupplierName { get; init; }
        public int PoCount { get; init; }
        public decimal PoAmount { get; init; }
        public decimal ReceivedAmount { get; init; }
        public decimal AvgPoAmount { get; init; }
        public DateTime? LastPoDate { get; init; }
    }

    private sealed class PurchasingReceivingRow
    {
        public string SupplierCode { get; init; } = "";
        public string? SupplierName { get; init; }
        public int GrCount { get; init; }
        public decimal GrAmount { get; init; }
        public DateTime? LastGrDate { get; init; }
    }

    private sealed class OperationHealthRow
    {
        public DateTime? LastExtractorRunUtc { get; init; }
        public DateTime? LastTransformRunUtc { get; init; }
        public string ExtractorStatus { get; init; } = "";
        public string TransformStatus { get; init; } = "";
        public int ActiveAlerts { get; init; }
        public int DqErrorsUnresolved { get; init; }
        public int ObjectsExtracted { get; init; }
        public int HealthScore { get; init; }
        public DateTime? UpdatedAtUtc { get; init; }
    }

    private sealed class OperationAlertRow
    {
        public long Id { get; init; }
        public string RuleCode { get; init; } = "";
        public string Severity { get; init; } = "";
        public string? TriggeredValue { get; init; }
        public string? Message { get; init; }
        public DateTime TriggeredAtUtc { get; init; }
        public bool IsResolved { get; init; }
    }

    private sealed class OperationDataQualityRow
    {
        public long Id { get; init; }
        public string SapObject { get; init; } = "";
        public string IssueType { get; init; } = "";
        public string Severity { get; init; } = "";
        public string Description { get; init; } = "";
        public int AffectedRows { get; init; }
        public string? SampleKey { get; init; }
        public DateTime DetectedAtUtc { get; init; }
        public bool IsResolved { get; init; }
    }

    // Accounting row types

    private sealed class IncomeStatementRawRow
    {
        public int     PeriodYear     { get; init; }
        public int     PeriodMonth    { get; init; }
        public string  StatementLine  { get; init; } = "";
        public decimal Amount         { get; init; }
    }

    private sealed class BalanceSheetRawRow
    {
        public string  SnapshotDate { get; init; } = "";
        public string  Category     { get; init; } = "";
        public string  SubCategory  { get; init; } = "";
        public decimal Amount       { get; init; }
    }

    private sealed class EbitdaRawRow
    {
        public int     PeriodYear       { get; init; }
        public int     PeriodMonth      { get; init; }
        public decimal Revenue          { get; init; }
        public decimal Cogs             { get; init; }
        public decimal GrossProfit      { get; init; }
        public decimal Opex             { get; init; }
        public decimal Ebitda           { get; init; }
        public decimal Depreciation     { get; init; }
        public decimal Amortization     { get; init; }
        public decimal FinancialResult  { get; init; }
        public decimal TaxResult        { get; init; }
        public decimal NetIncome        { get; init; }
    }

    private sealed class BalanceCheckRow
    {
        public string?  SnapshotDate     { get; init; }
        public decimal  TotalAssets      { get; init; }
        public decimal  TotalLiabilities { get; init; }
        public decimal  TotalEquity      { get; init; }
    }
}
