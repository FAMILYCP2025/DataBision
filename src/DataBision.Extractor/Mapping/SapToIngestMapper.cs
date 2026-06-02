namespace DataBision.Extractor.Mapping;

/// <summary>
/// Maps SAP B1 Service Layer JSON responses to DataBision Ingest API DTOs.
/// Sprint 3B: skeleton — all methods throw NotImplementedException.
/// Sprint 3C: field-by-field mapping implemented per SAP object,
///            using exact field names confirmed during Sprint 3A validation.
/// </summary>
public static class SapToIngestMapper
{
    // ── OSLP ──────────────────────────────────────────────────────────────────
    // SL fields confirmed in Sprint 3A: SalesEmployeeCode (int), SalesEmployeeName
    // UpdateDate NOT available — full refresh strategy, no watermark
    public static object MapOslpRow(System.Text.Json.Nodes.JsonNode row)
        => throw new NotImplementedException("OSLP mapper — Sprint 3C");

    // ── OCRD ──────────────────────────────────────────────────────────────────
    // SL fields: CardCode, CardName, CardType, UpdateDate — confirm names in Sprint 3C
    public static object MapOcrdRow(System.Text.Json.Nodes.JsonNode row)
        => throw new NotImplementedException("OCRD mapper — Sprint 3C");

    // ── OITM ──────────────────────────────────────────────────────────────────
    // SL fields: ItemCode, ItemName, ItemsGroupCode (type TBD) — confirm in Sprint 3C
    public static object MapOitmRow(System.Text.Json.Nodes.JsonNode row)
        => throw new NotImplementedException("OITM mapper — Sprint 3C");

    // ── OINV ──────────────────────────────────────────────────────────────────
    // SL fields: DocEntry, DocNum, CardCode, UpdateDate, SalesPersonCode (TBC) — confirm Sprint 3C
    public static object MapOinvRow(System.Text.Json.Nodes.JsonNode row)
        => throw new NotImplementedException("OINV mapper — Sprint 3C");
}
