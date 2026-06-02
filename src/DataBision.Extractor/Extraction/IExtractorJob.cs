namespace DataBision.Extractor.Extraction;

/// <summary>
/// One extraction job for a single SAP object (OSLP, OCRD, OITM, OINV, etc.).
/// Implemented per-object in Sprint 3C.
/// </summary>
public interface IExtractorJob
{
    string SapObject { get; }
    Task<ExtractionResult> RunAsync(bool dryRun, CancellationToken ct = default);
}
