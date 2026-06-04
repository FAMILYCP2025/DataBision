namespace DataBision.Extractor.DataBision;

/// <summary>
/// Sends extracted SAP data to the DataBision Ingest API and reads checkpoint state.
/// </summary>
public interface IDataBisionIngestClient
{
    Task<IngestResponse> SendAsync<T>(string endpoint, IngestBatch<T> batch, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Reads the last successful checkpoint for the given SAP object.
    /// Returns null if no checkpoint exists or the API is unreachable.
    /// </summary>
    Task<ExtractorCheckpoint?> GetCheckpointAsync(
        string companyId, string sapObject, CancellationToken ct = default);
}
