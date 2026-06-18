namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>
/// SAP B1 JDT1 — Journal Entry line. Embedded in JournalEntries via $expand=JournalEntryLines.
/// Sent to a dedicated ingest endpoint separately from the OJDT header.
/// Field names to validate against SL v1000290.
/// </summary>
public sealed class SapJdt1Row : IIngestRow
{
    public int TransId { get; set; }             // Parent OJDT.TransId (FK)
    public int LineId { get; set; }              // Line number within journal entry
    public string? Account { get; set; }         // GL account code
    public decimal? Debit { get; set; }          // Local currency debit
    public decimal? Credit { get; set; }         // Local currency credit
    public decimal? FcDebit { get; set; }        // FCDebit — foreign currency
    public decimal? FcCredit { get; set; }       // FCCredit
    public decimal? SysDebit { get; set; }       // SystemDebit — system currency
    public decimal? SysCredit { get; set; }      // SystemCredit
    public string? ShortName { get; set; }       // Business partner short name
    public string? ContraAct { get; set; }       // ContraAccount
    public string? LineMemo { get; set; }        // Line-level memo
    public DateTime? RefDate { get; set; }       // Line-level reference date
    public string? ProfitCode { get; set; }      // Dimension 1 (ProfitCode / OcrCode)
    public string? OcrCode { get; set; }         // Dimension 2
    public string? OcrCode2 { get; set; }        // Dimension 3
    public string? OcrCode3 { get; set; }        // Dimension 4
    public string? OcrCode4 { get; set; }        // Dimension 5
    public string? OcrCode5 { get; set; }        // Dimension 6
    public string? ProjectCode { get; set; }     // Project code

    public string IngestionMode { get; set; }   = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; }         = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(TransId)]         = TransId,
        [nameof(LineId)]          = LineId,
        [nameof(Account)]         = Account,
        [nameof(Debit)]           = Debit,
        [nameof(Credit)]          = Credit,
        [nameof(FcDebit)]         = FcDebit,
        [nameof(FcCredit)]        = FcCredit,
        [nameof(SysDebit)]        = SysDebit,
        [nameof(SysCredit)]       = SysCredit,
        [nameof(ShortName)]       = ShortName,
        [nameof(ContraAct)]       = ContraAct,
        [nameof(LineMemo)]        = LineMemo,
        [nameof(RefDate)]         = RefDate,
        [nameof(ProfitCode)]      = ProfitCode,
        [nameof(OcrCode)]         = OcrCode,
        [nameof(OcrCode2)]        = OcrCode2,
        [nameof(OcrCode3)]        = OcrCode3,
        [nameof(OcrCode4)]        = OcrCode4,
        [nameof(OcrCode5)]        = OcrCode5,
        [nameof(ProjectCode)]     = ProjectCode,
        [nameof(IngestionMode)]   = IngestionMode,
        [nameof(ExtractionRunId)] = ExtractionRunId,
        [nameof(BatchId)]         = BatchId,
        [nameof(ExtractedAtUtc)]  = ExtractedAtUtc,
        [nameof(SourceHashHex)]   = SourceHashHex,
    };
}
