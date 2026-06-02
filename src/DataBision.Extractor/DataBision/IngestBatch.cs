namespace DataBision.Extractor.DataBision;

/// <summary>
/// Payload sent to the DataBision Ingest API for one batch of rows.
/// Mirrors the IngestBatchRequest shape on the API side.
/// </summary>
public sealed class IngestBatch<T> where T : class
{
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string SapObject { get; init; } = string.Empty;
    public string ExtractionRunId { get; init; } = string.Empty;
    public string BatchId { get; init; } = string.Empty;
    public string IngestionMode { get; init; } = "INCREMENTAL";
    public IReadOnlyList<T> Rows { get; init; } = [];
}

/// <summary>
/// Response from the DataBision Ingest API after a batch is processed.
/// </summary>
public sealed class IngestResponse
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsSkipped { get; init; }
    public string? Error { get; init; }
}
