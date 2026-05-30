namespace DataBision.Application.DTOs.Ingest.Rows;

/// <summary>SAP B1 OCRD — Business Partner / Customer (master data).</summary>
public sealed class SapOcrdRow : IIngestRow
{
    // Business columns
    public string? CardCode { get; set; }
    public string? CardName { get; set; }
    public string? CardType { get; set; }
    public string? GroupCode { get; set; }
    public string? CntctPrsn { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Fax { get; set; }
    public string? EMail { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? Currency { get; set; }
    public string? SlpCode { get; set; }
    public string? VatLiable { get; set; }
    public string? LicTradNum { get; set; }
    public string? FrozenFor { get; set; }
    public decimal? Balance { get; set; }
    public decimal? CreditLine { get; set; }

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
        [nameof(CardCode)] = CardCode,
        [nameof(CardName)] = CardName,
        [nameof(CardType)] = CardType,
        [nameof(GroupCode)] = GroupCode,
        [nameof(CntctPrsn)] = CntctPrsn,
        [nameof(Phone1)] = Phone1,
        [nameof(Phone2)] = Phone2,
        [nameof(Fax)] = Fax,
        [nameof(EMail)] = EMail,
        [nameof(Country)] = Country,
        [nameof(City)] = City,
        [nameof(ZipCode)] = ZipCode,
        [nameof(Currency)] = Currency,
        [nameof(SlpCode)] = SlpCode,
        [nameof(VatLiable)] = VatLiable,
        [nameof(LicTradNum)] = LicTradNum,
        [nameof(FrozenFor)] = FrozenFor,
        [nameof(Balance)] = Balance,
        [nameof(CreditLine)] = CreditLine,
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
