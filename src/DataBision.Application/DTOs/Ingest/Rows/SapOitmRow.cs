namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OITM — Item master data.</summary>
public sealed class SapOitmRow : IIngestRow
{
    // Business columns
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? FrgnName { get; set; }
    public string? ItmsGrpCod { get; set; }
    public string? CstGrpCode { get; set; }
    public string? InvntryUom { get; set; }
    public string? BuyUnitMsr { get; set; }
    public string? SalUnitMsr { get; set; }
    public string? ManSerNum { get; set; }
    public decimal? OnHand { get; set; }
    public decimal? IsCommited { get; set; }
    public decimal? OnOrder { get; set; }
    public decimal? AvgPrice { get; set; }
    public decimal? LastPurPrc { get; set; }
    public string? ItemType { get; set; }
    public string? SWW { get; set; }
    public string? Canceled { get; set; }

    // Watermark columns
    public DateTime? CreateDate { get; set; }
    public string? CreateTS { get; set; }
    public string? CreateTSNorm { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateTS { get; set; }
    public string? UpdateTSNorm { get; set; }

    // Technical fields
    public string IngestionMode { get; set; } = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(ItemCode)] = ItemCode,
        [nameof(ItemName)] = ItemName,
        [nameof(FrgnName)] = FrgnName,
        [nameof(ItmsGrpCod)] = ItmsGrpCod,
        [nameof(CstGrpCode)] = CstGrpCode,
        [nameof(InvntryUom)] = InvntryUom,
        [nameof(BuyUnitMsr)] = BuyUnitMsr,
        [nameof(SalUnitMsr)] = SalUnitMsr,
        [nameof(ManSerNum)] = ManSerNum,
        [nameof(OnHand)] = OnHand,
        [nameof(IsCommited)] = IsCommited,
        [nameof(OnOrder)] = OnOrder,
        [nameof(AvgPrice)] = AvgPrice,
        [nameof(LastPurPrc)] = LastPurPrc,
        [nameof(ItemType)] = ItemType,
        [nameof(SWW)] = SWW,
        [nameof(Canceled)] = Canceled,
        [nameof(CreateDate)] = CreateDate,
        [nameof(CreateTS)] = CreateTS,
        [nameof(CreateTSNorm)] = CreateTSNorm,
        [nameof(UpdateDate)] = UpdateDate,
        [nameof(UpdateTS)] = UpdateTS,
        [nameof(UpdateTSNorm)] = UpdateTSNorm,
        [nameof(IngestionMode)] = IngestionMode,
        [nameof(ExtractionRunId)] = ExtractionRunId,
        [nameof(BatchId)] = BatchId,
        [nameof(ExtractedAtUtc)] = ExtractedAtUtc,
        [nameof(SourceHashHex)] = SourceHashHex,
    };
}
