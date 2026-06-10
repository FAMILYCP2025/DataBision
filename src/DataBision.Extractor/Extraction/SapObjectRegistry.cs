namespace DataBision.Extractor.Extraction;

/// <summary>
/// Catalog of SAP B1 objects known to the extractor.
/// Active = currently extracted. Prepared = ready to activate, not yet wired.
/// </summary>
public static class SapObjectRegistry
{
    // ── Active (extracted today) ──────────────────────────────────────────────

    public static class Active
    {
        // SALES — headers
        public const string OINV = "OINV";   // AR Invoices
        public const string ORIN = "ORIN";   // AR Credit Memos

        // SALES — lines (via DocumentLines, not paginator)
        public const string INV1 = "INV1";   // AR Invoice Lines
        public const string RIN1 = "RIN1";   // AR Credit Memo Lines

        // SALES — master data
        public const string OCRD = "OCRD";   // Business Partners / Customers
        public const string OITM = "OITM";   // Items
        public const string OSLP = "OSLP";   // Sales Persons

        public static readonly IReadOnlyList<string> All = [OINV, ORIN, INV1, RIN1, OCRD, OITM, OSLP];
    }

    // ── Prepared (inactive — code ready, not yet activated) ──────────────────
    // Activate by adding the job and wiring in ExtractorRunner + Program.cs

    public static class Prepared
    {
        // SALES — fulfillment
        public const string ORDR = "ORDR";   // Sales Orders          → mart.sales_fulfillment_dashboard
        public const string RDR1 = "RDR1";   // Sales Order Lines
        public const string ODLN = "ODLN";   // Deliveries            → mart.sales_fulfillment_dashboard
        public const string DLN1 = "DLN1";   // Delivery Lines

        // PURCHASING
        public const string OPOR = "OPOR";   // Purchase Orders       → mart.purchase_executive_daily
        public const string POR1 = "POR1";   // Purchase Order Lines
        public const string OPDN = "OPDN";   // Goods Receipts PO     → mart.purchase_receiving_dashboard
        public const string PDN1 = "PDN1";   // Goods Receipt Lines
        public const string OPCH = "OPCH";   // AP Invoices           → mart.finance_ap_aging_dashboard
        public const string PCH1 = "PCH1";   // AP Invoice Lines

        // INVENTORY
        public const string OITW = "OITW";   // Item Warehouse Info   → mart.inventory_stock_dashboard
        public const string OWHS = "OWHS";   // Warehouses            → mart.inventory_warehouse_dashboard
        public const string OWTR = "OWTR";   // Stock Transfers       → mart.inventory_warehouse_dashboard
        public const string WTR1 = "WTR1";   // Stock Transfer Lines

        public static readonly IReadOnlyList<string> All =
            [ORDR, RDR1, ODLN, DLN1, OPOR, POR1, OPDN, PDN1, OPCH, PCH1, OITW, OWHS, OWTR, WTR1];
    }

    // ── Endpoint mapping ──────────────────────────────────────────────────────
    // Maps SAP object code → Service Layer entity name

    public static readonly IReadOnlyDictionary<string, string> Endpoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Active
            [Active.OINV] = "Invoices",
            [Active.ORIN] = "CreditNotes",
            [Active.OCRD] = "BusinessPartners",
            [Active.OITM] = "Items",
            [Active.OSLP] = "SalesPersons",
            // Prepared
            [Prepared.ORDR] = "Orders",
            [Prepared.ODLN] = "DeliveryNotes",
            [Prepared.OPOR] = "PurchaseOrders",
            [Prepared.OPDN] = "PurchaseDeliveryNotes",
            [Prepared.OPCH] = "PurchaseInvoices",
            [Prepared.OITW] = "ItemWarehouseInfoCollection",
            [Prepared.OWHS] = "Warehouses",
            [Prepared.OWTR] = "StockTransfers",
        };
}
