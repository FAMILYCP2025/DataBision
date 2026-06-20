using System.Globalization;
using System.Text.Json.Nodes;
using DataBision.Application.DTOs.Ingest.Rows;

namespace DataBision.Extractor.Mapping;

/// <summary>
/// Maps SAP B1 Service Layer JSON responses to DataBision Application DTOs.
/// Field names are the exact names returned by SAP SL 1000290.
/// Hash/TSNorm computation is handled server-side by IngestService.
/// </summary>
public static class SapToIngestMapper
{
    // ── OSLP ──────────────────────────────────────────────────────────────────
    // SL fields confirmed Sprint 3A/3C: SalesEmployeeCode (int), SalesEmployeeName
    // UpdateDate NOT available in SalesPersons on SL 1000290 → full-refresh, no watermark
    public static SapOslpRow MapOslpRow(JsonNode row, MappingContext ctx) => new()
    {
        SlpCode         = GetInt(row, "SalesEmployeeCode"),
        SlpName         = GetStr(row, "SalesEmployeeName"),
        // No CreateDate/UpdateDate from SL for SalesPersons — leave null
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OCRD ──────────────────────────────────────────────────────────────────
    // SL fields confirmed Sprint 3C: CardCode, CardName, CardType, GroupCode,
    // FederalTaxID, CurrentAccountBalance, UpdateDate
    // CardType: SL returns enum ("cSupplier") but DB column is VARCHAR(1) → map to SAP char
    public static SapOcrdRow MapOcrdRow(JsonNode row, MappingContext ctx) => new()
    {
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        CardType        = MapCardType(GetStr(row, "CardType")),
        GroupCode       = GetStr(row, "GroupCode"),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        LicTradNum      = GetStr(row, "FederalTaxID"),
        Balance         = GetDec(row, "CurrentAccountBalance"),
        VatLiable       = MapYesNo(GetStr(row, "VatLiable")),
        FrozenFor       = MapYesNo(GetStr(row, "FrozenFor")),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OITM ──────────────────────────────────────────────────────────────────
    // SL fields confirmed Sprint 3C: ItemCode, ItemName, ItemsGroupCode (int Number), UpdateDate
    public static SapOitmRow MapOitmRow(JsonNode row, MappingContext ctx) => new()
    {
        ItemCode        = GetStr(row, "ItemCode"),
        ItemName        = GetStr(row, "ItemName"),
        ItmsGrpCod      = GetStr(row, "ItemsGroupCode"),   // SL returns Number; DTO is string?
        OnHand          = GetDec(row, "QuantityOnStock"),   // SL field name may vary
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OINV ──────────────────────────────────────────────────────────────────
    // SL fields confirmed Sprint 3C: DocEntry, DocNum, CardCode, DocDate, DocTotal, UpdateDate
    // DocStatus/DocType/Cancelled: SL returns enums, DB columns are VARCHAR(1) → map to SAP char
    public static SapOinvRow MapOinvRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        TaxDate         = GetDate(row, "TaxDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── INV1 ──────────────────────────────────────────────────────────────────
    // Lines extracted from Invoices.DocumentLines.
    // docEntry is taken from the parent document (passed explicitly).
    // SL field name for description is "ItemDescription" (not SAP OCRD "Dscription").
    // "UnitPrice" → Price; "WarehouseCode" → WhsCode; "DiscountPercent" → DiscPrcnt.
    public static SapInv1Row MapInv1Row(int docEntry, JsonNode line, MappingContext ctx) => new()
    {
        DocEntry        = docEntry,
        LineNum         = GetInt(line, "LineNum"),
        ItemCode        = GetStr(line, "ItemCode"),
        Dscription      = GetStrAny(line, "ItemDescription", "Dscription"),
        Quantity        = GetDec(line, "Quantity"),
        Price           = GetDecAny(line, "UnitPrice", "Price"),
        LineTotal       = GetDec(line, "LineTotal"),
        Currency        = GetStrAny(line, "Currency", "DocumentCurrency"),
        SlpCode         = GetStr(line, "SalesPersonCode"),
        WhsCode         = GetStrAny(line, "WarehouseCode", "WhsCode"),
        UomCode         = GetStrAny(line, "UoMCode", "UomCode"),
        DiscPrcnt       = GetDecAny(line, "DiscountPercent", "DiscPrcnt"),
        GrossBuyPr      = GetDecAny(line, "GrossPrice", "GrossBuyPr"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── ORIN ──────────────────────────────────────────────────────────────────
    // Same structure as OINV but for CreditNotes endpoint.
    public static SapOrinRow MapOrinRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        TaxDate         = GetDate(row, "TaxDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── RIN1 ──────────────────────────────────────────────────────────────────
    // Lines extracted from CreditNotes.DocumentLines.
    // Extra fields: BaseRef, BaseEntry, BaseLine, BaseType (back-reference to source document).
    public static SapRin1Row MapRin1Row(int docEntry, JsonNode line, MappingContext ctx) => new()
    {
        DocEntry        = docEntry,
        LineNum         = GetInt(line, "LineNum"),
        ItemCode        = GetStr(line, "ItemCode"),
        Dscription      = GetStrAny(line, "ItemDescription", "Dscription"),
        Quantity        = GetDec(line, "Quantity"),
        Price           = GetDecAny(line, "UnitPrice", "Price"),
        LineTotal       = GetDec(line, "LineTotal"),
        Currency        = GetStrAny(line, "Currency", "DocumentCurrency"),
        SlpCode         = GetStr(line, "SalesPersonCode"),
        WhsCode         = GetStrAny(line, "WarehouseCode", "WhsCode"),
        UomCode         = GetStrAny(line, "UoMCode", "UomCode"),
        DiscPrcnt       = GetDecAny(line, "DiscountPercent", "DiscPrcnt"),
        BaseRef         = GetStr(line, "BaseRef"),
        BaseEntry       = GetIntNullable(line, "BaseEntry"),
        BaseLine        = GetIntNullable(line, "BaseLine"),
        BaseType        = GetStr(line, "BaseType"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── Helpers (public entry-point for jobs that need raw int) ────────────────

    /// <summary>Called by line jobs to extract DocEntry from a parent document node.</summary>
    public static int GetIntPublic(JsonNode? node, string field) => GetInt(node, field);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int GetInt(JsonNode? node, string field)
    {
        try { return node?[field]?.GetValue<int>() ?? 0; }
        catch { return 0; }
    }

    private static int GetIntAny(JsonNode? node, params string[] fields)
    {
        foreach (var f in fields)
        {
            try
            {
                var v = node?[f];
                if (v is not null) return v.GetValue<int>();
            }
            catch { /* try next */ }
        }
        return 0;
    }

    private static int? GetIntNullable(JsonNode? node, string field)
    {
        try { return node?[field]?.GetValue<int>(); }
        catch { return null; }
    }

    private static string? GetStr(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null) return null;
        // Works for String, Number, Boolean nodes
        return v.ToString();
    }

    /// <summary>Tries multiple field names — returns value from the first one found.</summary>
    private static string? GetStrAny(JsonNode? node, params string[] fields)
    {
        foreach (var f in fields)
        {
            var v = node?[f];
            if (v is not null) return v.ToString();
        }
        return null;
    }

    private static decimal? GetDec(JsonNode? node, string field)
    {
        try { return node?[field]?.GetValue<decimal>(); }
        catch { return null; }
    }

    private static decimal? GetDecAny(JsonNode? node, params string[] fields)
    {
        foreach (var f in fields)
        {
            try
            {
                var v = node?[f];
                if (v is not null) return v.GetValue<decimal>();
            }
            catch { /* try next */ }
        }
        return null;
    }

    private static DateTime? GetDate(JsonNode? node, string field)
    {
        var s = node?[field]?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out var d)
            ? DateTime.SpecifyKind(d, DateTimeKind.Utc)
            : null;
    }

    // ── SAP SL enum → single-char SAP code ───────────────────────────────────
    // SL OData returns full enum names; raw.* columns are VARCHAR(1).

    private static string? MapCardType(string? v) => v switch
    {
        "cCustomer" => "C",
        "cSupplier" => "S",
        "cLead"     => "L",
        null        => null,
        _           => v.Length == 1 ? v : v[..1]   // safe fallback
    };

    // tYES / tNO / tNO → Y / N
    private static string? MapYesNo(string? v) => v switch
    {
        "tYES" or "tYes" => "Y",
        "tNO"  or "tNo"  => "N",
        null             => null,
        _                => v.Length == 1 ? v : v[..1]
    };

    // bost_Open / bost_Close → O / C
    private static string? MapDocStatus(string? v) => v switch
    {
        "bost_Open"  => "O",
        "bost_Close" => "C",
        null         => null,
        _            => v.Length == 1 ? v : v[..1]
    };

    // dDocument_Items / dDocument_Service → I / S
    private static string? MapDocType(string? v) => v switch
    {
        "dDocument_Items"   => "I",
        "dDocument_Service" => "S",
        null                => null,
        _                   => v.Length == 1 ? v : v[..1]
    };

    // ── OPOR — Purchase Orders ─────────────────────────────────────────────────
    public static SapOporRow MapOporRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OPDN — Purchase Delivery Notes (Goods Receipts) ───────────────────────
    public static SapOpdnRow MapOpdnRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OPCH — Purchase Invoices ───────────────────────────────────────────────
    public static SapOpchRow MapOpchRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OITW — Item Warehouse Levels ──────────────────────────────────────────
    // SL field: WarehouseCode (not WhsCode); InStock (not OnHand); Committed (not IsCommited)
    public static SapOitwRow MapOitwRow(JsonNode row, MappingContext ctx) => new()
    {
        ItemCode        = GetStr(row, "ItemCode"),
        WhsCode         = GetStrAny(row, "WarehouseCode", "WhsCode"),
        OnHand          = GetDecAny(row, "InStock", "OnHand"),
        IsCommited      = GetDecAny(row, "Committed", "IsCommited"),
        OnOrder         = GetDec(row, "OnOrder"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── ORDR — Sales Orders ───────────────────────────────────────────────────
    public static SapOrdrRow MapOrdrRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── ODLN — Delivery Notes ─────────────────────────────────────────────────
    public static SapOdlnRow MapOdlnRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        DocDueDate      = GetDate(row, "DocDueDate"),
        CardCode        = GetStr(row, "CardCode"),
        CardName        = GetStr(row, "CardName"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocTotalSy      = GetDec(row, "DocTotalSy"),
        VatSum          = GetDec(row, "VatSum"),
        DocCur          = GetStr(row, "DocCur"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        SlpCode         = GetStr(row, "SalesPersonCode"),
        ObjType         = GetStr(row, "ObjType"),
        DocType         = MapDocType(GetStr(row, "DocType")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OWTR — Stock Transfers ────────────────────────────────────────────────
    public static SapOwtrRow MapOwtrRow(JsonNode row, MappingContext ctx) => new()
    {
        DocEntry        = GetInt(row, "DocEntry"),
        DocNum          = GetInt(row, "DocNum"),
        DocDate         = GetDate(row, "DocDate"),
        FromWarehouse   = GetStrAny(row, "FromWarehouse", "FromWarehouseCode"),
        ToWarehouse     = GetStrAny(row, "ToWarehouse", "ToWarehouseCode"),
        DocTotal        = GetDec(row, "DocTotal"),
        DocStatus       = MapDocStatus(GetStr(row, "DocStatus")),
        Cancelled       = MapYesNo(GetStr(row, "Cancelled")),
        Comments        = GetStr(row, "Comments"),
        CreateDate      = GetDate(row, "CreateDate"),
        CreateTS        = GetStr(row, "CreateTS"),
        UpdateDate      = GetDate(row, "UpdateDate"),
        UpdateTS        = GetStr(row, "UpdateTS"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OACT — Chart of Accounts ──────────────────────────────────────────────
    // Full-refresh only — SL ChartOfAccounts entity has no UpdateDate field.
    // Field names to validate against SL v1000290 before first live run.
    public static SapOactRow MapOactRow(JsonNode row, MappingContext ctx) => new()
    {
        Code            = GetStr(row, "Code"),
        Name            = GetStr(row, "Name"),
        FatherNum       = null,   // Father/FatherNum not exposed via $select in SL v1000290
        Levels          = GetStrAny(row, "Levels", "Level"),
        GroupMask       = GetStr(row, "GroupMask"),
        AccountType     = GetStr(row, "AccountType"),
        Postable        = MapYesNo(GetStr(row, "Postable")),
        Frozen          = MapYesNo(GetStr(row, "Frozen")),
        ValidFor        = MapYesNo(GetStr(row, "ValidFor")),
        CashAccount     = MapYesNo(GetStr(row, "CashAccount")),
        ControlAccount  = MapYesNo(GetStr(row, "ControlAccount")),
        Currency        = GetStr(row, "Currency"),
        FormatCode      = GetStr(row, "FormatCode"),
        ExternalCode    = GetStr(row, "ExternalCode"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── OJDT — Journal Entry headers ──────────────────────────────────────────
    // Incremental by ReferenceDate (not UpdateDate — not exposed in SL JournalEntries entity).
    // Lines (JDT1) are embedded via $expand=JournalEntryLines and sent separately.
    // Field names to validate against SL v1000290.
    public static SapOjdtRow MapOjdtRow(JsonNode row, MappingContext ctx) => new()
    {
        TransId         = GetInt(row, "JdtNum"),           // SL OData key; internal TransId not exposed
        JdtNum          = GetIntNullable(row, "JdtNum"),
        RefDate         = GetDate(row, "ReferenceDate"),
        DueDate         = GetDate(row, "DueDate"),
        TaxDate         = GetDate(row, "TaxDate"),
        Memo            = GetStr(row, "Memo"),
        TransType       = GetStr(row, "TransactionCode"),
        BaseRef         = GetStr(row, "BaseRef"),
        UserRef         = GetStrAny(row, "Ref1", "UserRef"),
        CreatedBy       = GetStr(row, "CreatedBy"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };

    // ── JDT1 — Journal Entry lines ────────────────────────────────────────────
    // Extracted from $expand=JournalEntryLines on JournalEntries entity.
    // transId is the parent JdtNum (SL key), passed explicitly.
    // Field names to validate against SL v1000290.
    public static SapJdt1Row MapJdt1Row(int transId, JsonNode line, MappingContext ctx) => new()
    {
        TransId         = transId,
        LineId          = GetIntAny(line, "Line_ID", "LineId", "LineNum"),
        Account         = GetStrAny(line, "AccountCode", "Account"),
        Debit           = GetDec(line, "Debit"),
        Credit          = GetDec(line, "Credit"),
        FcDebit         = GetDecAny(line, "FCDebit", "FcDebit"),
        FcCredit        = GetDecAny(line, "FCCredit", "FcCredit"),
        SysDebit        = GetDecAny(line, "SystemDebit", "SysDebit"),
        SysCredit       = GetDecAny(line, "SystemCredit", "SysCredit"),
        ShortName       = GetStrAny(line, "ShortName", "BPCode"),
        ContraAct       = GetStrAny(line, "ContraAccount", "ContraAct"),
        LineMemo        = GetStr(line, "LineMemo"),
        RefDate         = GetDate(line, "ReferenceDate1"),
        ProfitCode      = GetStrAny(line, "CostingCode", "ProfitCode"),
        OcrCode         = GetStrAny(line, "CostingCode2", "OcrCode"),
        OcrCode2        = GetStrAny(line, "CostingCode3", "OcrCode2"),
        OcrCode3        = GetStrAny(line, "CostingCode4", "OcrCode3"),
        OcrCode4        = GetStrAny(line, "CostingCode5", "OcrCode4"),
        OcrCode5        = GetStr(line, "OcrCode5"),
        ProjectCode     = GetStr(line, "ProjectCode"),
        IngestionMode   = ctx.IngestionMode,
        ExtractionRunId = ctx.RunId,
        BatchId         = ctx.BatchId,
        ExtractedAtUtc  = ctx.ExtractedAtUtc,
    };
}

/// <summary>
/// Common metadata passed to every mapper to populate technical row fields.
/// </summary>
public sealed record MappingContext(
    string RunId,
    string BatchId,
    DateTime ExtractedAtUtc,
    string IngestionMode = "INCREMENTAL");
