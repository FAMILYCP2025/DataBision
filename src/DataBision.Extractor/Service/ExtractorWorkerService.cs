using DataBision.Extractor.Options;
using DataBision.Extractor.Scheduling;
using DataBision.Extractor.ServiceLayer;
using DataBision.Extractor.Transformations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Service;

/// <summary>
/// BackgroundService that drives the scheduled extraction loop when running as a Windows Service.
/// Reutilizes ExtractorScheduler — login/logout are managed per cycle here, not inside the scheduler.
/// If ExtractorOptions.RunMartRefreshAfterExtraction is true, runs MART refresh after each successful cycle.
/// </summary>
public sealed class ExtractorWorkerService(
    ExtractorScheduler scheduler,
    IServiceLayerClient slClient,
    ExtractorOptions options,
    ILogger<ExtractorWorkerService> log,
    ITransformationRunner? transformRunner = null) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var martCompanyId = !string.IsNullOrWhiteSpace(options.MartRefreshCompanyId)
            ? options.MartRefreshCompanyId
            : options.CompanyId;

        log.LogInformation("ExtractorWorkerService started. Objects={Obj}, Interval={Min}min, Send={Send}, MartRefreshAfterExtraction={Mart}",
            string.Join(", ", options.Objects), options.IntervalMinutes, options.SendEnabled,
            options.RunMartRefreshAfterExtraction);

        if (options.RunMartRefreshAfterExtraction && transformRunner is null)
        {
            log.LogWarning("RunMartRefreshAfterExtraction=true but no ITransformationRunner injected " +
                           "(Staging:ConnectionString not set?). MART refresh will be skipped.");
        }

        int cycleCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.MaxCycles.HasValue && cycleCount >= options.MaxCycles.Value)
            {
                log.LogInformation("MaxCycles={Max} reached — stopping worker.", options.MaxCycles.Value);
                break;
            }

            cycleCount++;
            log.LogInformation("=== Worker cycle {N} starting at {Time} ===",
                cycleCount, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

            bool extractionSucceeded = false;

            try
            {
                await slClient.LoginAsync(stoppingToken);
                try
                {
                    var summaries = await scheduler.RunCycleAsync(options.SendEnabled, stoppingToken);
                    scheduler.LogCycleSummary(cycleCount, summaries);
                    extractionSucceeded = summaries.All(s => s.Success);
                }
                finally
                {
                    await slClient.LogoutAsync(CancellationToken.None);
                }
            }
            catch (OperationCanceledException)
            {
                log.LogInformation("Worker cycle {N} cancelled — stopping.", cycleCount);
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Worker cycle {N} failed — waiting for next cycle. Error: {Msg}",
                    cycleCount, ex.Message);
            }

            // MART refresh after successful extraction
            if (extractionSucceeded && options.RunMartRefreshAfterExtraction && transformRunner is not null)
            {
                log.LogInformation("=== MART refresh after cycle {N} — company={CompanyId} ===",
                    cycleCount, martCompanyId);
                try
                {
                    using var martCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    martCts.CancelAfter(TimeSpan.FromMinutes(10));
                    var martResults = await transformRunner.RefreshMartAsync(martCompanyId, martCts.Token);
                    log.LogInformation("=== MART refresh done — {Count} object(s) refreshed ===", martResults.Count);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "MART refresh after cycle {N} failed — {Msg}. Extraction data is intact.",
                        cycleCount, ex.Message);
                }
            }

            if (stoppingToken.IsCancellationRequested) break;
            if (options.MaxCycles.HasValue && cycleCount >= options.MaxCycles.Value) break;

            log.LogInformation("=== Worker cycle {N} complete. Next in {Min} min ===",
                cycleCount, options.IntervalMinutes);

            try { await Task.Delay(TimeSpan.FromMinutes(options.IntervalMinutes), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        log.LogInformation("ExtractorWorkerService stopped after {N} cycle(s).", cycleCount);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("ExtractorWorkerService stopping gracefully...");
        await base.StopAsync(cancellationToken);
    }
}
