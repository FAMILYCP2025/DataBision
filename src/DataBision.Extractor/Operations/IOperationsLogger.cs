namespace DataBision.Extractor.Operations;

public interface IOperationsLogger
{
    Task<long> StartExtractorRunAsync(
        string companyId, string sapObject, string mode,
        int pageSize, int maxPages, CancellationToken ct = default);

    Task CompleteExtractorRunAsync(
        long runId, string status,
        int pagesFetched, int rowsExtracted, int rowsInserted, int rowsUpdated,
        bool hitMaxPages, string? lastError, string? watermarkDate,
        CancellationToken ct = default);

    Task LogExtractorPageAsync(
        long runId, string sapObject,
        int pageNumber, int skipOffset, int topCount, int rowsReceived,
        long elapsedMs, string status, string? errorCode, string? errorMessage,
        CancellationToken ct = default);

    Task<long> StartTransformRunAsync(
        string companyId, string transformType, CancellationToken ct = default);

    Task CompleteTransformRunAsync(
        long runId, string status, int objectsRefreshed,
        string? lastError, CancellationToken ct = default);

    Task RefreshPipelineHealthAsync(string companyId, CancellationToken ct = default);

    Task EvaluateAlertRulesAsync(string companyId, CancellationToken ct = default);
}
