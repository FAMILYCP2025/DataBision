namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>
/// SAP B1 OACT — Chart of Accounts replica. No UpdateDate in SL → full-refresh only.
/// SL endpoint: ChartOfAccounts. Field names to validate against SL v1000290 before first run.
/// </summary>
public sealed class SapOactRow : IIngestRow
{
    public string? Code { get; set; }           // AccountCode (PK)
    public string? Name { get; set; }           // AccountName
    public string? FatherNum { get; set; }      // Parent account code
    public string? Levels { get; set; }         // Level in account hierarchy
    public string? GroupMask { get; set; }      // Group classification mask
    public string? AccountType { get; set; }    // SL enum: act / oexp / oinc / other
    public string? Postable { get; set; }       // Y/N (mapped from tYES/tNO)
    public string? Frozen { get; set; }         // Y/N
    public string? ValidFor { get; set; }       // Y/N
    public string? CashAccount { get; set; }    // Y/N
    public string? ControlAccount { get; set; } // Y/N
    public string? Currency { get; set; }       // ISO 4217 or null
    public string? FormatCode { get; set; }     // SAP format code (prefix-based classification hint)
    public string? ExternalCode { get; set; }   // External system code

    // Technical
    public string IngestionMode { get; set; }   = string.Empty;
    public string ExtractionRunId { get; set; } = string.Empty;
    public string BatchId { get; set; }         = string.Empty;
    public DateTime ExtractedAtUtc { get; set; }
    public string? SourceHashHex { get; set; }

    public IDictionary<string, object?> ToColumns() => new Dictionary<string, object?>
    {
        [nameof(Code)]            = Code,
        [nameof(Name)]            = Name,
        [nameof(FatherNum)]       = FatherNum,
        [nameof(Levels)]          = Levels,
        [nameof(GroupMask)]       = GroupMask,
        [nameof(AccountType)]     = AccountType,
        [nameof(Postable)]        = Postable,
        [nameof(Frozen)]          = Frozen,
        [nameof(ValidFor)]        = ValidFor,
        [nameof(CashAccount)]     = CashAccount,
        [nameof(ControlAccount)]  = ControlAccount,
        [nameof(Currency)]        = Currency,
        [nameof(FormatCode)]      = FormatCode,
        [nameof(ExternalCode)]    = ExternalCode,
        [nameof(IngestionMode)]   = IngestionMode,
        [nameof(ExtractionRunId)] = ExtractionRunId,
        [nameof(BatchId)]         = BatchId,
        [nameof(ExtractedAtUtc)]  = ExtractedAtUtc,
        [nameof(SourceHashHex)]   = SourceHashHex,
    };
}
