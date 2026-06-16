namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OPDN — Purchase Delivery Note (Goods Receipt) header.</summary>
public sealed class SapOpdnRow : IIngestRow
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime? DocDate { get; set; }
    public DateTime? DocDueDate { get; set; }
    public string? CardCode { get; set; }
    public string? CardName { get; set; }
    public decimal? DocTotal { get; set; }
    public decimal? DocTotalSy { get; set; }
    public decimal? VatSum { get; set; }
    public string? DocCur { get; set; }
    public string? DocStatus { get; set; }
    public string? Cancelled { get; set; }
    public string? SlpCode { get; set; }
    public string? ObjType { get; set; }
    public string? DocType { get; set; }
    public string? Comments { get; set; }

    public DateTime? CreateDate { get; set; }
    public string? CreateTS { get; set; }
    public string? CreateTSNorm { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateTS { get; set; }
    public string? UpdateTSNorm { get; set; }

    public string IngestionMode { get; set; } = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(DocEntry)] = DocEntry,
        [nameof(DocNum)] = DocNum,
        [nameof(DocDate)] = DocDate,
        [nameof(DocDueDate)] = DocDueDate,
        [nameof(CardCode)] = CardCode,
        [nameof(CardName)] = CardName,
        [nameof(DocTotal)] = DocTotal,
        [nameof(DocTotalSy)] = DocTotalSy,
        [nameof(VatSum)] = VatSum,
        [nameof(DocCur)] = DocCur,
        [nameof(DocStatus)] = DocStatus,
        [nameof(Cancelled)] = Cancelled,
        [nameof(SlpCode)] = SlpCode,
        [nameof(ObjType)] = ObjType,
        [nameof(DocType)] = DocType,
        [nameof(Comments)] = Comments,
        [nameof(CreateDate)] = CreateDate,
        [nameof(CreateTS)] = CreateTS,
        [nameof(CreateTSNorm)] = CreateTSNorm,
        [nameof(UpdateDate)] = UpdateDate,
        [nameof(UpdateTS)] = UpdateTS,
        [nameof(UpdateTSNorm)] = UpdateTSNorm,
    };
}
