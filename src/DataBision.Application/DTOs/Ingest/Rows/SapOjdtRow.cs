namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>
/// SAP B1 OJDT — Journal Entry header. Incremental by ReferenceDate.
/// SL endpoint: JournalEntries. Lines (JDT1) are expanded inline and sent separately.
/// Field names to validate against SL v1000290.
/// </summary>
public sealed class SapOjdtRow : IIngestRow
{
    public int TransId { get; set; }            // Journal entry internal ID (PK)
    public int? JdtNum { get; set; }            // User-visible number
    public DateTime? RefDate { get; set; }      // ReferenceDate — watermark field
    public DateTime? DueDate { get; set; }
    public DateTime? TaxDate { get; set; }
    public string? Memo { get; set; }
    public string? TransType { get; set; }      // TransactionCode
    public string? BaseRef { get; set; }        // Base document reference
    public string? UserRef { get; set; }        // UserRef / user reference
    public string? CreatedBy { get; set; }      // Creator user code

    public string IngestionMode { get; set; }   = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; }         = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(TransId)]         = TransId,
        [nameof(JdtNum)]          = JdtNum,
        [nameof(RefDate)]         = RefDate,
        [nameof(DueDate)]         = DueDate,
        [nameof(TaxDate)]         = TaxDate,
        [nameof(Memo)]            = Memo,
        [nameof(TransType)]       = TransType,
        [nameof(BaseRef)]         = BaseRef,
        [nameof(UserRef)]         = UserRef,
        [nameof(CreatedBy)]       = CreatedBy,
        [nameof(IngestionMode)]   = IngestionMode,
        [nameof(ExtractionRunId)] = ExtractionRunId,
        [nameof(BatchId)]         = BatchId,
        [nameof(ExtractedAtUtc)]  = ExtractedAtUtc,
        [nameof(SourceHashHex)]   = SourceHashHex,
    };
}
