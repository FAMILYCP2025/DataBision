namespace DataBision.Shared.Constants;

public static class SapObjects
{
    // Master data
    public const string BusinessPartners = "OCRD";
    public const string Items = "OITM";
    public const string ItemWarehouses = "OITW";
    public const string PriceLists = "OPLN";
    public const string SpecialPrices = "SPP1";
    public const string Warehouses = "OWHS";
    public const string SalesPeople = "OSLP";

    // Documents — Sales
    public const string SalesOrders = "ORDR";
    public const string SalesOrderLines = "RDR1";
    public const string Deliveries = "ODLN";
    public const string DeliveryLines = "DLN1";
    public const string ARInvoices = "OINV";
    public const string ARInvoiceLines = "INV1";
    public const string ARCreditNotes = "ORIN";
    public const string ARCreditNoteLines = "RIN1";

    // Documents — Purchasing
    public const string PurchaseOrders = "OPOR";
    public const string PurchaseOrderLines = "POR1";
    public const string APInvoices = "OPCH";
    public const string APInvoiceLines = "PCH1";

    // Inventory
    public const string InventoryTransactions = "OITL";
    public const string StockCounting = "OIST";

    // Financial
    public const string JournalEntries = "OJDT";
    public const string JournalEntryLines = "JDT1";
    public const string ChartOfAccounts = "OACT";

    // All known SAP B1 objects used in DataBision
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        BusinessPartners, Items, ItemWarehouses, PriceLists, SpecialPrices,
        Warehouses, SalesPeople,
        SalesOrders, SalesOrderLines, Deliveries, DeliveryLines,
        ARInvoices, ARInvoiceLines, ARCreditNotes, ARCreditNoteLines,
        PurchaseOrders, PurchaseOrderLines, APInvoices, APInvoiceLines,
        InventoryTransactions, StockCounting,
        JournalEntries, JournalEntryLines, ChartOfAccounts
    };

    public static bool IsValid(string? sapObject) =>
        sapObject is not null && All.Contains(sapObject);
}
