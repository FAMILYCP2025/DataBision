namespace DataBision.Application.DTOs.Ingest;

/// <summary>
/// Marker interface for all SAP raw row DTOs submitted to the Ingest API.
/// Technical fields (SourceHashHex, ExtractionRunId, BatchId, ExtractedAtUtc, IngestionMode)
/// are part of the contract but excluded from the canonical hash by CanonicalHasher.
/// </summary>
public interface IIngestRow
{
    string IngestionMode { get; set; }
    string ExtractionRunId { get; set; }
    string BatchId { get; set; }
    DateTime ExtractedAtUtc { get; set; }
    string? SourceHashHex { get; set; }

    /// <summary>Returns all columns (business + technical) as a dictionary for hashing and upsert.</summary>
    IDictionary<string, object?> ToColumns();
}
