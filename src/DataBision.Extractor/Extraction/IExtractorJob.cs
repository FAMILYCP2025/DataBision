namespace DataBision.Extractor.Extraction;

/// <summary>
/// One extraction job for a single SAP object (OSLP, OCRD, OITM, OINV, etc.).
/// </summary>
public interface IExtractorJob
{
    string SapObject { get; }

    /// <param name="dryRun">If true, return immediately without connecting.</param>
    /// <param name="send">If true, map extracted rows and send to DataBision Ingest API.</param>
    Task<ExtractionResult> RunAsync(bool dryRun, bool send, CancellationToken ct = default);
}
