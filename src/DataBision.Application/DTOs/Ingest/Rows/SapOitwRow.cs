namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OITW — Item-Warehouse stock levels. Composite key: ItemCode + WhsCode.</summary>
public sealed class SapOitwRow : IIngestRow
{
    public string? ItemCode { get; set; }
    public string? WhsCode { get; set; }
    public decimal? OnHand { get; set; }
    public decimal? IsCommited { get; set; }
    public decimal? OnOrder { get; set; }

    // OITW has no UpdateDate in SAP B1 SL — watermark uses extraction timestamp.
    public string IngestionMode { get; set; } = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(ItemCode)] = ItemCode,
        [nameof(WhsCode)] = WhsCode,
        [nameof(OnHand)] = OnHand,
        [nameof(IsCommited)] = IsCommited,
        [nameof(OnOrder)] = OnOrder,
    };
}
