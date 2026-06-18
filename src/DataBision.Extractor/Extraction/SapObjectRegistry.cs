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

        // PURCHASING (Sprint 8J)
        public const string OPOR = "OPOR";   // Purchase Orders       → mart.purchase_executive_daily
        public const string OPDN = "OPDN";   // Goods Receipts PO     → mart.purchase_receiving_dashboard
        public const string OPCH = "OPCH";   // AP Invoices           → mart.finance_ap_aging_dashboard

        // INVENTORY (Sprint 8J)
        // OITW: moved to Prepared — ItemWarehouseInfoCollection not exposed as top-level entity in SL v1000290
        public const string OWTR = "OWTR";   // Stock Transfers       → mart.inventory_warehouse_dashboard

        // SALES FULFILLMENT (Sprint 8J)
        public const string ORDR = "ORDR";   // Sales Orders          → mart.sales_fulfillment_dashboard
        public const string ODLN = "ODLN";   // Deliveries            → mart.sales_fulfillment_dashboard

        public static readonly IReadOnlyList<string> All =
            [OINV, ORIN, INV1, RIN1, OCRD, OITM, OSLP, OPOR, OPDN, OPCH, OWTR, ORDR, ODLN];
    }

    // ── Prepared (inactive — line objects and warehouses, not yet wired) ────────

    public static class Prepared
    {
        // Lines — activate when volume is known
        public const string RDR1 = "RDR1";   // Sales Order Lines
        public const string DLN1 = "DLN1";   // Delivery Lines
        public const string POR1 = "POR1";   // Purchase Order Lines
        public const string PDN1 = "PDN1";   // Goods Receipt Lines
        public const string PCH1 = "PCH1";   // AP Invoice Lines
        public const string WTR1 = "WTR1";   // Stock Transfer Lines

        // Master data + inventory per warehouse
        public const string OITW = "OITW";   // Item Warehouse Info — no top-level SL entity in v1000290
        public const string OWHS = "OWHS";   // Warehouses            → mart.inventory_warehouse_dashboard

        // Accounting — Sprint 13A; explicit CLI only, NOT in AllObjects (no accidental full-refresh)
        public const string OACT = "OACT";   // Chart of Accounts    → mart.gl_accounts (full-refresh)
        public const string OJDT = "OJDT";   // Journal Entries      → mart.account_balances (incremental by ReferenceDate)
        // JDT1 lines are embedded in OJDT via $expand — no separate Prepared entry needed

        public static readonly IReadOnlyList<string> All =
            [RDR1, DLN1, POR1, PDN1, PCH1, WTR1, OWHS];
    }

    // ── Endpoint mapping ──────────────────────────────────────────────────────
    // Maps SAP object code → Service Layer entity name

    public static readonly IReadOnlyDictionary<string, string> Endpoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Active.OINV]     = "Invoices",
            [Active.ORIN]     = "CreditNotes",
            [Active.OCRD]     = "BusinessPartners",
            [Active.OITM]     = "Items",
            [Active.OSLP]     = "SalesPersons",
            [Active.OPOR]     = "PurchaseOrders",
            [Active.OPDN]     = "PurchaseDeliveryNotes",
            [Active.OPCH]     = "PurchaseInvoices",
            [Prepared.OITW]   = "ItemWarehouseInfoCollection",
            [Active.OWTR]     = "StockTransfers",
            [Active.ORDR]     = "Orders",
            [Active.ODLN]     = "DeliveryNotes",
            [Prepared.OWHS]   = "Warehouses",
            [Prepared.OACT]   = "ChartOfAccounts",
            [Prepared.OJDT]   = "JournalEntries",
        };
}
