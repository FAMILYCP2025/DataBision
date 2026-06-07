using DataBision.Extractor.DataBision;
using DataBision.Extractor.Extraction;
using DataBision.Extractor.Extraction.Jobs;
using DataBision.Extractor.Options;
using DataBision.Extractor.Scheduling;
using DataBision.Extractor.Service;
using DataBision.Extractor.ServiceLayer;
using DataBision.Extractor.Transformations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

// ── --service mode: run as Windows Service via Generic Host ───────────────────
if (args.Contains("--service"))
{
    var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService(o => o.ServiceName = "DataBisionExtractor")
        .UseSerilog((ctx, logCfg) => ConfigureSerilog(logCfg, isService: true))
        .ConfigureServices((ctx, services) =>
        {
            var extOptions = ctx.Configuration.GetSection(ExtractorOptions.Section)
                                 .Get<ExtractorOptions>() ?? new ExtractorOptions();
            var slOptions  = ctx.Configuration.GetSection(SapServiceLayerOptions.Section)
                                 .Get<SapServiceLayerOptions>() ?? new SapServiceLayerOptions();
            var apiOptions = ctx.Configuration.GetSection(DataBisionApiOptions.Section)
                                 .Get<DataBisionApiOptions>() ?? new DataBisionApiOptions();

            services.AddSingleton(extOptions);
            services.AddSingleton(slOptions);
            services.AddSingleton(apiOptions);
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
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExtractorRunner>>()));
            services.AddSingleton<ExtractorScheduler>();
            services.AddHostedService<ExtractorWorkerService>();
        })
        .Build();

    await host.RunAsync();
    return 0;
}

// ── CLI mode: manual DI + Serilog ─────────────────────────────────────────────
Log.Logger = ConfigureSerilog(new LoggerConfiguration(), isService: false).CreateLogger();

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

services.AddLogging(b => b
    .ClearProviders()
    .AddSerilog(Log.Logger));

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
          --dry-run                      Show resolved configuration (no connections)
          --object <name>                Extract single object: OSLP|OCRD|OITM|OINV|INV1|ORIN|RIN1|ALL
          --object <name> --send         Extract and send to DataBision Ingest API
          --run-once [--send]            Extract Extractor.Objects list once and exit
          --schedule [--send]            Run Extractor.Objects in a loop (Ctrl+C to stop)
          --interval-minutes N           Override Extractor.IntervalMinutes (default 30)
          --max-cycles N                 Stop after N cycles (useful for testing)
          --transform [--company C]      Refresh all STG tables from RAW (requires Staging:ConnectionString)
          --service                      Run as Windows Service (uses Extractor.SendEnabled from config)

        Examples:
          dotnet run -- --validate
          dotnet run -- --object OCRD --send
          dotnet run -- --run-once --send
          dotnet run -- --schedule --interval-minutes 30 --send
          dotnet run -- --schedule --interval-minutes 1 --max-cycles 1 --send
          dotnet DataBision.Extractor.exe --service
        """);
    return 0;
}

log.LogInformation("DataBision Extractor starting...");

if (args.Length == 0)
{
    log.LogError("No action specified. Use --help for usage.");
    return 1;
}

// ── Parse common flags ────────────────────────────────────────────────────────
bool isDryRun   = args.Contains("--dry-run");
bool isSend     = args.Contains("--send");
bool isRunOnce  = args.Contains("--run-once");
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

// ── --dry-run alone ───────────────────────────────────────────────────────────
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
    log.LogInformation("Extractor.Objects:           {Obj}", string.Join(", ", extOptions.Objects));
    log.LogInformation("Extractor.IntervalMinutes:   {I}",   extOptions.IntervalMinutes);
    log.LogInformation("Extractor.SendEnabled:       {S}",   extOptions.SendEnabled);

    if (!slValid || !apiValid || !extValid)
    {
        log.LogError("Configuration incomplete — fill in appsettings.Development.json.");
        return 2;
    }
    log.LogInformation("=== DRY-RUN: configuration OK ===");
    return 0;
}

// ── Full config validation ────────────────────────────────────────────────────
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
    var client = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(slOptions.TimeoutSeconds * 2));
    try
    {
        await client.LoginAsync(cts.Token);
        log.LogInformation("[P-01] PASS — Login successful");
        var rows = await client.GetAsync("SalesPersons", "$top=5&$select=SalesEmployeeCode,SalesEmployeeName", cts.Token);
        log.LogInformation("[P-06] PASS — OSLP rows received: {Count}", rows.Count);
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
    finally { await slClient.LogoutAsync(); }
}

// ── --schedule ────────────────────────────────────────────────────────────────
if (isSchedule)
{
    log.LogInformation("=== Schedule: objects={Obj}, interval={Min}min, maxCycles={Max}, send={Send} ===",
        string.Join(", ", extOptions.Objects), intervalMinutes,
        maxCycles?.ToString() ?? "unlimited", isSend);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

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
            finally { await slClient.LogoutAsync(CancellationToken.None); }
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { log.LogError("=== Cycle {N} failed — {Msg}", cycleCount, ex.Message); exitCode = 4; }

        if (maxCycles.HasValue && cycleCount >= maxCycles.Value) break;
        if (cts.IsCancellationRequested) break;
        log.LogInformation("=== Cycle {N} complete. Next in {Min} min (Ctrl+C to stop) ===", cycleCount, intervalMinutes);
        try { await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cts.Token); }
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
    finally { await slClient.LogoutAsync(); }
}

// ── --transform ───────────────────────────────────────────────────────────────
if (args.Contains("--transform"))
{
    var stagingOptions = config.GetSection(StagingOptions.Section).Get<StagingOptions>()
                         ?? new StagingOptions();
    try { stagingOptions.Validate(); }
    catch (InvalidOperationException ex)
    {
        log.LogError("Configuration error: {Message}", ex.Message);
        return 2;
    }

    string? companyArg = null;
    var companyIdx = Array.IndexOf(args, "--company");
    if (companyIdx >= 0 && companyIdx + 1 < args.Length)
        companyArg = args[companyIdx + 1];

    var companyId = !string.IsNullOrWhiteSpace(companyArg)
        ? companyArg
        : extOptions.CompanyId;

    if (string.IsNullOrWhiteSpace(companyId))
    {
        log.LogError("No company specified. Use --company <id> or set Extractor:CompanyId in config.");
        return 2;
    }

    log.LogInformation("=== STG Transform: company={CompanyId} ===", companyId);
    var runner = new TransformationRunner(stagingOptions.ConnectionString,
        sp.GetRequiredService<ILogger<TransformationRunner>>());
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    try
    {
        var results = await runner.RefreshAllAsync(companyId, cts.Token);
        log.LogInformation("=== STG Transform: DONE — {Count} object(s) ===", results.Count);
        foreach (var (obj, rows) in results)
            log.LogInformation("  {Object}: {Rows} row(s)", obj, rows);
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== STG Transform: FAILED — {Message}", ex.Message);
        return 5;
    }
}

log.LogWarning("No recognized action. Use --help for usage.");
return 1;

// ── Serilog factory ───────────────────────────────────────────────────────────

static LoggerConfiguration ConfigureSerilog(LoggerConfiguration cfg, bool isService)
{
    cfg = cfg
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.File(
            path: "logs/databision-extractor-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            retainedFileCountLimit: 30);

    if (!isService)
        cfg = cfg.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

    return cfg;
}

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
