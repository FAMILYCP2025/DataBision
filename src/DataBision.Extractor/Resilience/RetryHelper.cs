using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Resilience;

/// <summary>
/// Lightweight retry helper without Polly. Retries transient network and timeout errors only.
/// Does NOT retry HTTP 400 (bad query), 401 (auth), or 422 (validation) — those are not transient.
/// </summary>
public static class RetryHelper
{
    private static readonly int[] BackoffSeconds = [1, 2, 4];

    /// <summary>
    /// Executes an async operation with exponential backoff retry.
    /// Retries only on <see cref="HttpRequestException"/> and non-cancellation <see cref="TaskCanceledException"/>.
    /// All other exceptions propagate immediately.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        string operationName,
        ILogger logger,
        CancellationToken ct,
        int maxAttempts = 3)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action(ct);
            }
            catch (Exception ex) when (IsTransient(ex, ct) && attempt < maxAttempts)
            {
                var delay = BackoffSeconds[Math.Min(attempt - 1, BackoffSeconds.Length - 1)];
                logger.LogWarning("{Op}: attempt {A}/{Max} failed — retrying in {D}s. Error: {Msg}",
                    operationName, attempt, maxAttempts, delay, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }

        // Final attempt — let exception propagate to caller
        return await action(ct);
    }

    /// <summary>Void overload.</summary>
    public static Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        string operationName,
        ILogger logger,
        CancellationToken ct,
        int maxAttempts = 3)
        => ExecuteAsync<bool>(
            async c => { await action(c); return true; },
            operationName, logger, ct, maxAttempts);

    private static bool IsTransient(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException
        || (ex is TaskCanceledException tce && !ct.IsCancellationRequested && tce.InnerException is TimeoutException);
}
