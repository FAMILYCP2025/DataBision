using DataBision.Extractor.Options;
using DataBision.Extractor.Scheduling;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataBision.Extractor.Service;

/// <summary>
/// BackgroundService that drives the scheduled extraction loop when running as a Windows Service.
/// Reutilizes ExtractorScheduler — login/logout are managed per cycle here, not inside the scheduler.
/// </summary>
public sealed class ExtractorWorkerService(
    ExtractorScheduler scheduler,
    IServiceLayerClient slClient,
    ExtractorOptions options,
    ILogger<ExtractorWorkerService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("ExtractorWorkerService started. Objects={Obj}, Interval={Min}min, Send={Send}",
            string.Join(", ", options.Objects), options.IntervalMinutes, options.SendEnabled);

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

            try
            {
                await slClient.LoginAsync(stoppingToken);
                try
                {
                    var summaries = await scheduler.RunCycleAsync(options.SendEnabled, stoppingToken);
                    scheduler.LogCycleSummary(cycleCount, summaries);
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
