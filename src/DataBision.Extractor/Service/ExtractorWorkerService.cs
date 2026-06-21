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

        log.LogInformation(
            "ExtractorWorkerService started. Objects={Obj}, Interval={Min}min, Send={Send}, " +
            "MartRefreshAfterExtraction={Mart}, ProcessMartRefresh={Proc}",
            string.Join(", ", options.Objects), options.IntervalMinutes, options.SendEnabled,
            options.RunMartRefreshAfterExtraction, options.RunProcessMartRefreshAfterExtraction);

        if ((options.RunMartRefreshAfterExtraction || options.RunProcessMartRefreshAfterExtraction) && transformRunner is null)
        {
            log.LogWarning("RunMartRefreshAfterExtraction/RunProcessMartRefreshAfterExtraction=true but no ITransformationRunner injected " +
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

            // Finance MART refresh after successful extraction
            bool martRefreshSucceeded = false;
            if (extractionSucceeded && options.RunMartRefreshAfterExtraction && transformRunner is not null)
            {
                log.LogInformation("=== Finance MART refresh after cycle {N} — company={CompanyId} ===",
                    cycleCount, martCompanyId);
                try
                {
                    using var martCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    martCts.CancelAfter(TimeSpan.FromMinutes(10));
                    var martResults = await transformRunner.RefreshMartAsync(martCompanyId, martCts.Token);
                    log.LogInformation("=== Finance MART refresh done — {Count} object(s) refreshed ===", martResults.Count);
                    martRefreshSucceeded = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Finance MART refresh after cycle {N} failed — {Msg}. Extraction data is intact.",
                        cycleCount, ex.Message);
                }
            }

            // Process-dashboard MART refresh (only if finance MART succeeded)
            if (martRefreshSucceeded && options.RunProcessMartRefreshAfterExtraction && transformRunner is not null)
            {
                log.LogInformation("=== Process MART refresh after cycle {N} — company={CompanyId} ===",
                    cycleCount, martCompanyId);
                try
                {
                    using var procCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    procCts.CancelAfter(TimeSpan.FromMinutes(10));
                    var procResults = await transformRunner.RefreshProcessMartAsync(martCompanyId, procCts.Token);
                    log.LogInformation("=== Process MART refresh done — {Count} object(s) refreshed ===", procResults.Count);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Process MART refresh after cycle {N} failed — {Msg}. Finance MART data is intact.",
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
