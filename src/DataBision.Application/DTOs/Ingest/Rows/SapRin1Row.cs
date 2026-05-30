namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 RIN1 — AR Credit Memo line. Watermarks inherited from header (ORIN).</summary>
public sealed class SapRin1Row : IIngestRow
{
    // Business columns
    public int DocEntry { get; set; }
    public int LineNum { get; set; }
    public string? ItemCode { get; set; }
    public string? Dscription { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? LineTotal { get; set; }
    public string? Currency { get; set; }
    public string? SlpCode { get; set; }
    public string? WhsCode { get; set; }
    public string? UomCode { get; set; }
    public decimal? DiscPrcnt { get; set; }
    public string? BaseRef { get; set; }
    public int? BaseEntry { get; set; }
    public int? BaseLine { get; set; }
    public string? BaseType { get; set; }

    // Technical fields
    public string IngestionMode { get; set; } = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(DocEntry)] = DocEntry,
        [nameof(LineNum)] = LineNum,
        [nameof(ItemCode)] = ItemCode,
        [nameof(Dscription)] = Dscription,
        [nameof(Quantity)] = Quantity,
        [nameof(Price)] = Price,
        [nameof(LineTotal)] = LineTotal,
        [nameof(Currency)] = Currency,
        [nameof(SlpCode)] = SlpCode,
        [nameof(WhsCode)] = WhsCode,
        [nameof(UomCode)] = UomCode,
        [nameof(DiscPrcnt)] = DiscPrcnt,
        [nameof(BaseRef)] = BaseRef,
        [nameof(BaseEntry)] = BaseEntry,
        [nameof(BaseLine)] = BaseLine,
        [nameof(BaseType)] = BaseType,
        [nameof(IngestionMode)] = IngestionMode,
        [nameof(ExtractionRunId)] = ExtractionRunId,
        [nameof(BatchId)] = BatchId,
        [nameof(ExtractedAtUtc)] = ExtractedAtUtc,
        [nameof(SourceHashHex)] = SourceHashHex,
    };
}
