using DataBision.Extractor.DataBision;
using DataBision.Extractor.Extraction;
using DataBision.Extractor.Extraction.Jobs;
using DataBision.Extractor.Options;
using DataBision.Extractor.Scheduling;
using DataBision.Extractor.ServiceLayer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ── Configuration ─────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// ── DI Container ──────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddLogging(b => b
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

var slOptions  = config.GetSection(SapServiceLayerOptions.Section).Get<SapServiceLayerOptions>()  ?? new SapServiceLayerOptions();
var apiOptions = config.GetSection(DataBisionApiOptions.Section).Get<DataBisionApiOptions>()       ?? new DataBisionApiOptions();
var extOptions = config.GetSection(ExtractorOptions.Section).Get<ExtractorOptions>()               ?? new ExtractorOptions();

services.AddSingleton(slOptions);
services.AddSingleton(apiOptions);
services.AddSingleton(extOptions);
services.AddSingleton<IServiceLayerClient, ServiceLayerClient>();
services.AddSingleton<IDataBisionIngestClient, DataBisionIngestClient>();
services.AddSingleton<OslpExtractorJob>();
services.AddSingleton<OcrdExtractorJob>();
services.AddSingleton<OitmExtractorJob>();
services.AddSingleton<OinvExtractorJob>();
services.AddSingleton<Inv1ExtractorJob>();
services.AddSingleton<OrinExtractorJob>();
services.AddSingleton<Rin1ExtractorJob>();
services.AddSingleton<ExtractorRunner>(sp => new ExtractorRunner(
    new IExtractorJob[]
    {
        sp.GetRequiredService<OslpExtractorJob>(),
        sp.GetRequiredService<OcrdExtractorJob>(),
        sp.GetRequiredService<OitmExtractorJob>(),
        sp.GetRequiredService<OinvExtractorJob>(),
        sp.GetRequiredService<Inv1ExtractorJob>(),
        sp.GetRequiredService<OrinExtractorJob>(),
        sp.GetRequiredService<Rin1ExtractorJob>(),
    },
    sp.GetRequiredService<ExtractorOptions>(),
    sp.GetRequiredService<ILogger<ExtractorRunner>>()));
services.AddSingleton<ExtractorScheduler>();

var sp  = services.BuildServiceProvider();
var log = sp.GetRequiredService<ILogger<Program>>();

// ── --help ─────────────────────────────────────────────────────────────────────
if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
        DataBision SAP Extractor

        Usage:
          DataBision.Extractor [options]

        Options:
          --help, -h                     Show this help
          --validate                     Test Service Layer login + GET OSLP top 5 + logout
          --dry-run                      Show resolved configuration (no connections, no data)
          --object <name>                Extract single object: OSLP|OCRD|OITM|OINV|INV1|ORIN|RIN1|ALL
          --object <name> --send         Extract and send to DataBision Ingest API
          --object <name> --dry-run      Validate object name and show planned extraction
          --run-once [--send]            Extract Extractor.Objects list once and exit
          --schedule [--send]            Run Extractor.Objects in a scheduled loop (Ctrl+C to stop)
          --interval-minutes N           Override Extractor.IntervalMinutes (default 30)
          --max-cycles N                 Stop after N cycles (useful for testing)

        Note: ALL includes OSLP/OCRD/OITM/OINV only. INV1/ORIN/RIN1 require explicit --object.

        Examples:
          dotnet run -- --validate
          dotnet run -- --dry-run
          dotnet run -- --object OCRD --send
          dotnet run -- --run-once --send
          dotnet run -- --schedule --interval-minutes 30 --send
          dotnet run -- --schedule --interval-minutes 1 --max-cycles 1 --send
        """);
    return 0;
}

// ── Startup log ───────────────────────────────────────────────────────────────
log.LogInformation("DataBision Extractor starting...");

if (args.Length == 0)
{
    log.LogError("No action specified. Use --help for usage.");
    return 1;
}

// ── Parse common flags ────────────────────────────────────────────────────────
bool isDryRun  = args.Contains("--dry-run");
bool isSend    = args.Contains("--send");
bool isRunOnce = args.Contains("--run-once");
bool isSchedule = args.Contains("--schedule");

string? objectArg = null;
var objIdx = Array.IndexOf(args, "--object");
if (objIdx >= 0 && objIdx + 1 < args.Length)
    objectArg = args[objIdx + 1];

int intervalMinutes = extOptions.IntervalMinutes;
var intIdx = Array.IndexOf(args, "--interval-minutes");
if (intIdx >= 0 && intIdx + 1 < args.Length && int.TryParse(args[intIdx + 1], out var parsedInterval))
    intervalMinutes = parsedInterval;

int? maxCycles = extOptions.MaxCycles;
var maxIdx = Array.IndexOf(args, "--max-cycles");
if (maxIdx >= 0 && maxIdx + 1 < args.Length && int.TryParse(args[maxIdx + 1], out var parsedMax))
    maxCycles = parsedMax;

// ── --dry-run alone: show config ──────────────────────────────────────────────
if (isDryRun && !args.Contains("--validate") && objectArg is null && !isRunOnce && !isSchedule)
{
    log.LogInformation("=== DRY-RUN: configuration check ===");
    var slValid  = ValidateSlSilent(slOptions);
    var apiValid = ValidateApiSilent(apiOptions);
    var extValid = ValidateExtSilent(extOptions);

    log.LogInformation("SapServiceLayer.BaseUrl:    {Url}",  MaskUrl(slOptions.BaseUrl));
    log.LogInformation("SapServiceLayer.CompanyDB:  {Db}",   slOptions.CompanyDB.Length > 0 ? "[set]" : "[MISSING]");
    log.LogInformation("SapServiceLayer.UserName:   {U}",    slOptions.UserName.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("SapServiceLayer.Password:   {P}",    slOptions.Password.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("DataBisionApi.BaseUrl:       {Url}", apiOptions.BaseUrl);
    log.LogInformation("DataBisionApi.ApiKey:        {K}",   apiOptions.ApiKey.Length > 0   ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.TenantId:          {T}",   extOptions.TenantId.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.CompanyId:         {C}",   extOptions.CompanyId.Length > 0 ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.Mode:              {M}",   extOptions.Mode);
    log.LogInformation("Extractor.PageSize:          {Ps}",  extOptions.PageSize);
    log.LogInformation("Extractor.Objects:           {Obj}", string.Join(", ", extOptions.Objects));
    log.LogInformation("Extractor.IntervalMinutes:   {I}",   extOptions.IntervalMinutes);

    if (!slValid || !apiValid || !extValid)
    {
        log.LogError("Configuration incomplete — fill in appsettings.Development.json.");
        return 2;
    }
    log.LogInformation("=== DRY-RUN: configuration OK — no connections made ===");
    return 0;
}

// ── Full config validation (all non-dry-run modes) ────────────────────────────
try
{
    slOptions.Validate();
    log.LogInformation("SapServiceLayer: {Url} / DB={Db}", MaskUrl(slOptions.BaseUrl), slOptions.CompanyDB);
    apiOptions.Validate();
    extOptions.Validate();
}
catch (InvalidOperationException ex)
{
    log.LogError("Configuration error: {Message}", ex.Message);
    return 2;
}

// ── --validate ────────────────────────────────────────────────────────────────
if (args.Contains("--validate"))
{
    log.LogInformation("=== Sprint 3A: Service Layer Validation ===");
    var client = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(slOptions.TimeoutSeconds * 2));

    try
    {
        await client.LoginAsync(cts.Token);
        log.LogInformation("[P-01] PASS — Login successful");

        var oslpRows = await client.GetAsync("SalesPersons",
            "$top=5&$select=SalesEmployeeCode,SalesEmployeeName", cts.Token);
        log.LogInformation("[P-06] PASS — OSLP rows received: {Count}", oslpRows.Count);

        await client.LogoutAsync(cts.Token);
        log.LogInformation("[P-04] PASS — Logout completed");
        log.LogInformation("=== Validation: ALL PASS ===");
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== Validation: FAIL — {Message}", ex.Message);
        return 3;
    }
}

// ── --run-once ────────────────────────────────────────────────────────────────
if (isRunOnce)
{
    log.LogInformation("=== Run-once: {Objects} (send={Send}) ===",
        string.Join(", ", extOptions.Objects), isSend);

    var scheduler = sp.GetRequiredService<ExtractorScheduler>();
    var slClient  = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

    await slClient.LoginAsync(cts.Token);
    try
    {
        var summaries = await scheduler.RunCycleAsync(isSend, cts.Token);
        scheduler.LogCycleSummary(1, summaries);
        return summaries.Any(s => !s.Success) ? 4 : 0;
    }
    finally
    {
        await slClient.LogoutAsync();
    }
}

// ── --schedule ────────────────────────────────────────────────────────────────
if (isSchedule)
{
    log.LogInformation("=== Schedule mode: objects={Obj}, interval={Min}min, maxCycles={Max}, send={Send} ===",
        string.Join(", ", extOptions.Objects), intervalMinutes,
        maxCycles?.ToString() ?? "unlimited", isSend);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        log.LogInformation("=== Ctrl+C received — stopping after current cycle ===");
        cts.Cancel();
    };

    var scheduler = sp.GetRequiredService<ExtractorScheduler>();
    var slClient  = sp.GetRequiredService<IServiceLayerClient>();
    int cycleCount = 0;
    int exitCode   = 0;

    while (!cts.IsCancellationRequested)
    {
        if (maxCycles.HasValue && cycleCount >= maxCycles.Value) break;

        cycleCount++;
        log.LogInformation("=== Cycle {N} starting at {Time} ===",
            cycleCount, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

        try
        {
            await slClient.LoginAsync(cts.Token);
            try
            {
                var summaries = await scheduler.RunCycleAsync(isSend, cts.Token);
                scheduler.LogCycleSummary(cycleCount, summaries);
                if (summaries.Any(s => !s.Success)) exitCode = 4;
            }
            finally
            {
                await slClient.LogoutAsync(CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            log.LogInformation("=== Cycle {N} cancelled ===", cycleCount);
            break;
        }
        catch (Exception ex)
        {
            log.LogError("=== Cycle {N} failed — {Msg} ===", cycleCount, ex.Message);
            exitCode = 4;
        }

        if (maxCycles.HasValue && cycleCount >= maxCycles.Value) break;
        if (cts.IsCancellationRequested) break;

        log.LogInformation("=== Cycle {N} complete. Next in {Min} min (Ctrl+C to stop) ===",
            cycleCount, intervalMinutes);

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cts.Token);
        }
        catch (OperationCanceledException) { break; }
    }

    log.LogInformation("=== Scheduler stopped after {N} cycle(s) ===", cycleCount);
    return exitCode;
}

// ── --object ─────────────────────────────────────────────────────────────────
if (objectArg is not null)
{
    if (!ExtractorRunner.IsSupported(objectArg))
    {
        log.LogError("Unknown object '{Obj}'. Supported: OSLP, OCRD, OITM, OINV, INV1, ORIN, RIN1, ALL", objectArg);
        return 1;
    }

    if (isDryRun)
    {
        log.LogInformation("=== DRY-RUN: --object {Obj} ===", objectArg.ToUpperInvariant());
        log.LogInformation("TenantId:   {T}", extOptions.TenantId);
        log.LogInformation("CompanyId:  {C}", extOptions.CompanyId);
        log.LogInformation("Mode:       {M}", extOptions.Mode);
        log.LogInformation("PageSize:   {Ps}", extOptions.PageSize);
        log.LogInformation("Extraction of {Obj}: NOT STARTED (dry-run)", objectArg.ToUpperInvariant());
        return 0;
    }

    log.LogInformation("=== Extraction: {Obj} (send={Send}) ===", objectArg.ToUpperInvariant(), isSend);
    var runner   = sp.GetRequiredService<ExtractorRunner>();
    var slClient = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

    try
    {
        await slClient.LoginAsync(cts.Token);
        var results = await runner.RunAsync(objectArg, dryRun: false, send: isSend, cts.Token);
        return results.Any(r => !r.Success) ? 4 : 0;
    }
    finally
    {
        await slClient.LogoutAsync();
    }
}

log.LogWarning("No recognized action. Use --help for usage.");
return 1;

// ── Helpers ───────────────────────────────────────────────────────────────────

static string MaskUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url)) return "(not set)";
    try { var uri = new Uri(url); return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}"; }
    catch { return "(invalid url)"; }
}

static bool ValidateSlSilent(SapServiceLayerOptions o)
    => !string.IsNullOrWhiteSpace(o.BaseUrl)
    && !string.IsNullOrWhiteSpace(o.CompanyDB)
    && !string.IsNullOrWhiteSpace(o.UserName)
    && !string.IsNullOrWhiteSpace(o.Password);

static bool ValidateApiSilent(DataBisionApiOptions o)
    => !string.IsNullOrWhiteSpace(o.BaseUrl)
    && !string.IsNullOrWhiteSpace(o.ApiKey);

static bool ValidateExtSilent(ExtractorOptions o)
    => !string.IsNullOrWhiteSpace(o.TenantId)
    && !string.IsNullOrWhiteSpace(o.CompanyId);
