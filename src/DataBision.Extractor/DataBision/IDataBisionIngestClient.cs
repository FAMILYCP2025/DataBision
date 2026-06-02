namespace DataBision.Extractor.DataBision;

/// <summary>
/// Sends extracted SAP data to the DataBision Ingest API.
/// Implemented in Sprint 3D — skeleton only in Sprint 3B.
/// </summary>
public interface IDataBisionIngestClient
{
    Task<IngestResponse> SendAsync<T>(string endpoint, IngestBatch<T> batch, CancellationToken ct = default)
        where T : class;
}
