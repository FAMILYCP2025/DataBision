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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int GetInt(JsonNode? node, string field)
    {
        try { return node?[field]?.GetValue<int>() ?? 0; }
        catch { return 0; }
    }

    private static string? GetStr(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null) return null;
        // Works for String, Number, Boolean nodes
        return v.ToString();
    }

    private static decimal? GetDec(JsonNode? node, string field)
    {
        try { return node?[field]?.GetValue<decimal>(); }
        catch { return null; }
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
}

/// <summary>
/// Common metadata passed to every mapper to populate technical row fields.
/// </summary>
public sealed record MappingContext(
    string RunId,
    string BatchId,
    DateTime ExtractedAtUtc,
    string IngestionMode = "INCREMENTAL");
