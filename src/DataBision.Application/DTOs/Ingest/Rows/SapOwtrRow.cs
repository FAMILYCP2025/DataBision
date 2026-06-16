namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OWTR — Stock Transfer header.</summary>
public sealed class SapOwtrRow : IIngestRow
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public DateTime? DocDate { get; set; }
    public string? FromWarehouse { get; set; }
    public string? ToWarehouse { get; set; }
    public decimal? DocTotal { get; set; }
    public string? DocStatus { get; set; }
    public string? Cancelled { get; set; }
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
        [nameof(FromWarehouse)] = FromWarehouse,
        [nameof(ToWarehouse)] = ToWarehouse,
        [nameof(DocTotal)] = DocTotal,
        [nameof(DocStatus)] = DocStatus,
        [nameof(Cancelled)] = Cancelled,
        [nameof(Comments)] = Comments,
        [nameof(CreateDate)] = CreateDate,
        [nameof(CreateTS)] = CreateTS,
        [nameof(CreateTSNorm)] = CreateTSNorm,
        [nameof(UpdateDate)] = UpdateDate,
        [nameof(UpdateTS)] = UpdateTS,
        [nameof(UpdateTSNorm)] = UpdateTSNorm,
    };
}
