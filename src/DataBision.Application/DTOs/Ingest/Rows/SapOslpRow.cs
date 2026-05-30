namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OSLP — Salesperson master data.</summary>
public sealed class SapOslpRow : IIngestRow
{
    // Business columns
    public int SlpCode { get; set; }
    public string? SlpName { get; set; }
    public decimal? Commission { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Telephone { get; set; }
    public string? Active { get; set; }
    public int? GroupCode { get; set; }

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
        [nameof(SlpCode)] = SlpCode,
        [nameof(SlpName)] = SlpName,
        [nameof(Commission)] = Commission,
        [nameof(Email)] = Email,
        [nameof(Mobile)] = Mobile,
        [nameof(Telephone)] = Telephone,
        [nameof(Active)] = Active,
        [nameof(GroupCode)] = GroupCode,
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
