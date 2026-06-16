using DataBision.Extractor.DataBision;
using DataBision.Extractor.Extraction;
using DataBision.Extractor.Extraction.Jobs;
using DataBision.Extractor.Operations;
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

            var svcStagingOpts = ctx.Configuration.GetSection(StagingOptions.Section).Get<StagingOptions>() ?? new StagingOptions();
            IOperationsLogger? svcOpsLogger = null;
            if (!string.IsNullOrWhiteSpace(svcStagingOpts.ConnectionString))
            {
                var svcOpsLog = ctx.Configuration.GetSection("Logging").GetValue<string>("LogLevel:Default");
                svcOpsLogger = new OperationsLogger(svcStagingOpts.ConnectionString,
                    Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddSerilog())
                        .CreateLogger<OperationsLogger>());
            }

            services.AddSingleton(extOptions);
            services.AddSingleton(slOptions);
            services.AddSingleton(apiOptions);
            services.AddSingleton<IServiceLayerClient, ServiceLayerClient>();
            services.AddSingleton<IDataBisionIngestClient, DataBisionIngestClient>();
            services.AddSingleton<ServiceLayerPaginator>();
            if (svcOpsLogger is not null) services.AddSingleton<IOperationsLogger>(svcOpsLogger);
            services.AddSingleton<OslpExtractorJob>();
            services.AddSingleton<OcrdExtractorJob>();
            services.AddSingleton<OitmExtractorJob>();
            services.AddSingleton<OinvExtractorJob>();
            services.AddSingleton<Inv1ExtractorJob>();
            services.AddSingleton<OrinExtractorJob>();
            services.AddSingleton<Rin1ExtractorJob>();
            services.AddSingleton<OporExtractorJob>();
            services.AddSingleton<OpdnExtractorJob>();
            services.AddSingleton<OpchExtractorJob>();
            services.AddSingleton<OrdrExtractorJob>();
            services.AddSingleton<OdlnExtractorJob>();
            services.AddSingleton<OwtrExtractorJob>();
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
                    sp.GetRequiredService<OporExtractorJob>(),
                    sp.GetRequiredService<OpdnExtractorJob>(),
                    sp.GetRequiredService<OpchExtractorJob>(),
                    sp.GetRequiredService<OrdrExtractorJob>(),
                    sp.GetRequiredService<OdlnExtractorJob>(),
                    sp.GetRequiredService<OwtrExtractorJob>(),
                },
                sp.GetRequiredService<ExtractorOptions>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExtractorRunner>>(),
                svcOpsLogger,
                sp.GetRequiredService<ServiceLayerPaginator>()));
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

// CLI overrides for PageSize / MaxPages — parsed before DI so all jobs receive overridden values
{
    var pgIdx = Array.IndexOf(args, "--page-size");
    var mpIdx = Array.IndexOf(args, "--max-pages");
    int? cliPg = pgIdx >= 0 && pgIdx + 1 < args.Length && int.TryParse(args[pgIdx + 1], out var pg) && pg > 0 ? pg : null;
    int? cliMp = mpIdx >= 0 && mpIdx + 1 < args.Length && int.TryParse(args[mpIdx + 1], out var mp) && mp > 0 ? mp : null;
    if (cliPg.HasValue || cliMp.HasValue)
    {
        extOptions = new ExtractorOptions
        {
            TenantId        = extOptions.TenantId,
            CompanyId       = extOptions.CompanyId,
            Mode            = extOptions.Mode,
            PageSize        = cliPg ?? extOptions.PageSize,
            MaxPages        = cliMp ?? extOptions.MaxPages,
            LookbackMinutes = extOptions.LookbackMinutes,
            Objects         = extOptions.Objects,
            SendEnabled     = extOptions.SendEnabled,
            IntervalMinutes = extOptions.IntervalMinutes,
            MaxCycles       = extOptions.MaxCycles,
        };
    }
}

// Resolve OPS logger before DI build so it can be injected into ExtractorRunner
var stagingOptsEarly = config.GetSection(StagingOptions.Section).Get<StagingOptions>() ?? new StagingOptions();
IOperationsLogger? opsLogger = null;
if (!string.IsNullOrWhiteSpace(stagingOptsEarly.ConnectionString))
{
    opsLogger = new OperationsLogger(stagingOptsEarly.ConnectionString,
        LoggerFactory.Create(b => b.AddSerilog()).CreateLogger<OperationsLogger>());
}

services.AddSingleton(slOptions);
services.AddSingleton(apiOptions);
services.AddSingleton(extOptions);
services.AddSingleton<IServiceLayerClient, ServiceLayerClient>();
services.AddSingleton<IDataBisionIngestClient, DataBisionIngestClient>();
services.AddSingleton<ServiceLayerPaginator>();
if (opsLogger is not null) services.AddSingleton<IOperationsLogger>(opsLogger);
services.AddSingleton<OslpExtractorJob>();
services.AddSingleton<OcrdExtractorJob>();
services.AddSingleton<OitmExtractorJob>();
services.AddSingleton<OinvExtractorJob>();
services.AddSingleton<Inv1ExtractorJob>();
services.AddSingleton<OrinExtractorJob>();
services.AddSingleton<Rin1ExtractorJob>();
services.AddSingleton<OporExtractorJob>();
services.AddSingleton<OpdnExtractorJob>();
services.AddSingleton<OpchExtractorJob>();
services.AddSingleton<OrdrExtractorJob>();
services.AddSingleton<OdlnExtractorJob>();
services.AddSingleton<OwtrExtractorJob>();
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
        sp.GetRequiredService<OporExtractorJob>(),
        sp.GetRequiredService<OpdnExtractorJob>(),
        sp.GetRequiredService<OpchExtractorJob>(),
        sp.GetRequiredService<OrdrExtractorJob>(),
        sp.GetRequiredService<OdlnExtractorJob>(),
        sp.GetRequiredService<OwtrExtractorJob>(),
    },
    sp.GetRequiredService<ExtractorOptions>(),
    sp.GetRequiredService<ILogger<ExtractorRunner>>(),
    opsLogger,
    sp.GetRequiredService<ServiceLayerPaginator>()));
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
          --validate-staging             Validate Supabase schemas and table counts (no SAP required)
          --dry-run                      Show resolved configuration (no connections)
          --object <name>                Extract single object: OSLP|OCRD|OITM|OINV|INV1|ORIN|RIN1|OPOR|OPDN|OPCH|ORDR|ODLN|OWTR|ALL
          --object <name> --send         Extract and send to DataBision Ingest API
          --page-size N                  Override Extractor.PageSize for this run (default from config)
          --max-pages N                  Override Extractor.MaxPages for this run (default from config)
          --run-once [--send]            Extract Extractor.Objects list once and exit
          --schedule [--send]            Run Extractor.Objects in a loop (Ctrl+C to stop)
          --interval-minutes N           Override Extractor.IntervalMinutes (default 30)
          --max-cycles N                 Stop after N cycles (useful for testing)
          --transform [--company C]      Refresh STG tables from RAW (requires Staging:ConnectionString)
          --transform --include-mart     Refresh STG then MART
          --transform-mart [--company C] Refresh MART only (STG must already be populated)
          --validate-ops [--company C]   Query ops.extractor_run and ops.transform_run summary
          --service                      Run as Windows Service (uses Extractor.SendEnabled from config)

        Examples:
          dotnet run -- --validate
          dotnet run -- --validate-staging
          dotnet run -- --object OCRD --send
          dotnet run -- --object OINV --send --page-size 20 --max-pages 2
          dotnet run -- --run-once --send
          dotnet run -- --schedule --interval-minutes 30 --send
          dotnet run -- --schedule --interval-minutes 1 --max-cycles 1 --send
          dotnet run -- --transform --company company-dev-001
          dotnet run -- --transform --include-mart --company company-dev-001
          dotnet run -- --transform-mart --company company-dev-001
          dotnet run -- --validate-ops --company company-dev-001
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

// ── --validate-staging ─────────────────────────────────────────────────────────
if (args.Contains("--validate-staging"))
{
    var stagingOpts = config.GetSection(StagingOptions.Section).Get<StagingOptions>() ?? new StagingOptions();
    try { stagingOpts.Validate(); }
    catch (InvalidOperationException ex)
    {
        log.LogError("Staging config error: {Message}", ex.Message);
        return 2;
    }

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try
    {
        await using var conn = new Npgsql.NpgsqlConnection(stagingOpts.ConnectionString);
        await conn.OpenAsync(cts.Token);
        log.LogInformation("[VS-01] PASS — Supabase connection open");

        async Task<long> Scalar(string sql)
        {
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            var r = await cmd.ExecuteScalarAsync(cts.Token);
            return r is long l ? l : r is int i ? i : 0L;
        }

        // Schemas — reader closed before any subsequent commands
        var schemas = new List<string>();
        {
            await using var schemaCmd = new Npgsql.NpgsqlCommand(
                "SELECT schema_name FROM information_schema.schemata WHERE schema_name IN ('ctl','raw','stg','mart','cfg','ops') ORDER BY schema_name;", conn);
            await using var sr = await schemaCmd.ExecuteReaderAsync(cts.Token);
            while (await sr.ReadAsync(cts.Token)) schemas.Add(sr.GetString(0));
        }
        log.LogInformation("[VS-02] Schemas present: {Schemas}", string.Join(", ", schemas));

        // cfg counts
        var cfgProcess   = await Scalar("SELECT COUNT(*) FROM cfg.process;");
        var cfgDashboard = await Scalar("SELECT COUNT(*) FROM cfg.dashboard;");
        var cfgCpe       = await Scalar("SELECT COUNT(*) FROM cfg.company_process_enabled;");
        log.LogInformation("[VS-03] cfg.process={Proc}, cfg.dashboard={Dash}, cfg.company_process_enabled={Cpe}",
            cfgProcess, cfgDashboard, cfgCpe);

        // ops.alert_rule
        var alertRules = await Scalar("SELECT COUNT(*) FROM ops.alert_rule;");
        log.LogInformation("[VS-04] ops.alert_rule={Rules} (expected 8)", alertRules);

        // table list cfg/mart/ops — reader closed before return
        var tables = new List<string>();
        {
            await using var tabCmd = new Npgsql.NpgsqlCommand(
                "SELECT table_schema || '.' || table_name FROM information_schema.tables WHERE table_schema IN ('cfg','mart','ops') ORDER BY table_schema, table_name;", conn);
            await using var tr2 = await tabCmd.ExecuteReaderAsync(cts.Token);
            while (await tr2.ReadAsync(cts.Token)) tables.Add(tr2.GetString(0));
        }
        log.LogInformation("[VS-05] Tables ({Count}): {Tables}", tables.Count, string.Join(", ", tables));

        log.LogInformation("=== --validate-staging: ALL PASS ===");
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== --validate-staging: FAIL — {Message}", ex.Message);
        return 3;
    }
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
        log.LogError("Unknown object '{Obj}'. Supported: OSLP, OCRD, OITM, OINV, INV1, ORIN, RIN1, OPOR, OPDN, OPCH, ORDR, ODLN, OWTR, ALL", objectArg);
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

// ── --validate-ops ────────────────────────────────────────────────────────────
if (args.Contains("--validate-ops"))
{
    var voStagingOpts = config.GetSection(StagingOptions.Section).Get<StagingOptions>() ?? new StagingOptions();
    try { voStagingOpts.Validate(); }
    catch (InvalidOperationException ex)
    {
        log.LogError("Staging config error: {Message}", ex.Message);
        return 2;
    }

    string? voCompanyArg = null;
    var voCompIdx = Array.IndexOf(args, "--company");
    if (voCompIdx >= 0 && voCompIdx + 1 < args.Length) voCompanyArg = args[voCompIdx + 1];
    var voCompanyId = !string.IsNullOrWhiteSpace(voCompanyArg) ? voCompanyArg : extOptions.CompanyId;

    using var voCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try
    {
        await using var conn = new Npgsql.NpgsqlConnection(voStagingOpts.ConnectionString);
        await conn.OpenAsync(voCts.Token);
        log.LogInformation("[OPS-01] Connection open");

        async Task<long> Scalar(string sql, string? p = null)
        {
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            if (p is not null) cmd.Parameters.AddWithValue("p", p);
            var r = await cmd.ExecuteScalarAsync(voCts.Token);
            return r is long l ? l : r is int i ? i : 0L;
        }

        var totalRuns   = await Scalar(string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT COUNT(*) FROM ops.extractor_run"
            : "SELECT COUNT(*) FROM ops.extractor_run WHERE company_id=@p", voCompanyId ?? null);
        var failedRuns  = await Scalar(string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT COUNT(*) FROM ops.extractor_run WHERE status='ERROR'"
            : "SELECT COUNT(*) FROM ops.extractor_run WHERE company_id=@p AND status='ERROR'", voCompanyId ?? null);
        var totalPages  = await Scalar(string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT COUNT(*) FROM ops.extractor_page_log"
            : "SELECT COUNT(*) FROM ops.extractor_page_log WHERE run_id IN (SELECT run_id FROM ops.extractor_run WHERE company_id=@p)", voCompanyId ?? null);
        var totalTrans  = await Scalar(string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT COUNT(*) FROM ops.transform_run"
            : "SELECT COUNT(*) FROM ops.transform_run WHERE company_id=@p", voCompanyId ?? null);
        var alertEvents = await Scalar(string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT COUNT(*) FROM ops.alert_event"
            : "SELECT COUNT(*) FROM ops.alert_event WHERE company_id=@p", voCompanyId ?? null);

        log.LogInformation("[OPS-02] extractor_run: total={Total}, errors={Err}", totalRuns, failedRuns);
        log.LogInformation("[OPS-03] extractor_page_log: {Pages} pages logged", totalPages);
        log.LogInformation("[OPS-04] transform_run: {Trans} runs", totalTrans);
        log.LogInformation("[OPS-05] alert_event: {Alerts} events fired", alertEvents);

        // Show last 5 extractor runs
        var recentSql = string.IsNullOrWhiteSpace(voCompanyId)
            ? "SELECT sap_object, status, pages_fetched, rows_extracted, started_at_utc FROM ops.extractor_run ORDER BY started_at_utc DESC LIMIT 5"
            : "SELECT sap_object, status, pages_fetched, rows_extracted, started_at_utc FROM ops.extractor_run WHERE company_id=@p ORDER BY started_at_utc DESC LIMIT 5";
        await using var recCmd = new Npgsql.NpgsqlCommand(recentSql, conn);
        if (!string.IsNullOrWhiteSpace(voCompanyId)) recCmd.Parameters.AddWithValue("p", voCompanyId);
        await using var recRdr = await recCmd.ExecuteReaderAsync(voCts.Token);
        while (await recRdr.ReadAsync(voCts.Token))
        {
            log.LogInformation("  run: obj={Obj} status={Status} pages={Pages} rows={Rows} at={At}",
                recRdr.GetString(0), recRdr.GetString(1),
                recRdr.IsDBNull(2) ? 0 : recRdr.GetInt32(2),
                recRdr.IsDBNull(3) ? 0 : recRdr.GetInt32(3),
                recRdr.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:ss"));
        }

        log.LogInformation("=== --validate-ops: DONE ===");
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== --validate-ops: FAIL — {Message}", ex.Message);
        return 3;
    }
}

// ── --transform / --transform-mart ───────────────────────────────────────────
bool isTransform     = args.Contains("--transform");
bool isTransformMart = args.Contains("--transform-mart");
bool includeMart     = args.Contains("--include-mart");

if (isTransform || isTransformMart)
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

    var transformRunner = new TransformationRunner(stagingOptions.ConnectionString,
        sp.GetRequiredService<ILogger<TransformationRunner>>(), opsLogger);
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    try
    {
        if (isTransformMart)
        {
            // MART only (base + process dashboards)
            log.LogInformation("=== MART Transform: company={CompanyId} ===", companyId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = await transformRunner.RefreshMartAsync(companyId, cts.Token);
            sw.Stop();
            log.LogInformation("=== MART base done — {Count} object(s) in {Ms}ms ===",
                results.Count, sw.ElapsedMilliseconds);

            var swProc = System.Diagnostics.Stopwatch.StartNew();
            var procResults = await transformRunner.RefreshProcessMartAsync(companyId, cts.Token);
            swProc.Stop();
            log.LogInformation("=== MART process-dashboards done — {Count} object(s) in {Ms}ms ===",
                procResults.Count, swProc.ElapsedMilliseconds);
            log.LogInformation("=== MART Transform: DONE (base {BaseMs}ms + processes {ProcMs}ms) ===",
                sw.ElapsedMilliseconds, swProc.ElapsedMilliseconds);
        }
        else if (includeMart)
        {
            // STG then MART base then MART process dashboards
            log.LogInformation("=== STG+MART Transform: company={CompanyId} ===", companyId);
            var swStg = System.Diagnostics.Stopwatch.StartNew();
            var stgResults = await transformRunner.RefreshStgAsync(companyId, cts.Token);
            swStg.Stop();
            log.LogInformation("=== STG done — {Count} object(s) in {Ms}ms ===",
                stgResults.Count, swStg.ElapsedMilliseconds);

            var swMart = System.Diagnostics.Stopwatch.StartNew();
            var martResults = await transformRunner.RefreshMartAsync(companyId, cts.Token);
            swMart.Stop();
            log.LogInformation("=== MART base done — {Count} object(s) in {Ms}ms ===",
                martResults.Count, swMart.ElapsedMilliseconds);

            var swProc = System.Diagnostics.Stopwatch.StartNew();
            var procResults = await transformRunner.RefreshProcessMartAsync(companyId, cts.Token);
            swProc.Stop();
            log.LogInformation("=== MART process-dashboards done — {Count} object(s) in {Ms}ms ===",
                procResults.Count, swProc.ElapsedMilliseconds);
            log.LogInformation("=== STG+MART Transform: COMPLETE (STG {StgMs}ms + MART {MartMs}ms + proc {ProcMs}ms) ===",
                swStg.ElapsedMilliseconds, swMart.ElapsedMilliseconds, swProc.ElapsedMilliseconds);
        }
        else
        {
            // STG only
            log.LogInformation("=== STG Transform: company={CompanyId} ===", companyId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = await transformRunner.RefreshStgAsync(companyId, cts.Token);
            sw.Stop();
            log.LogInformation("=== STG Transform: DONE — {Count} object(s) in {Ms}ms ===",
                results.Count, sw.ElapsedMilliseconds);
        }
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== Transform: FAILED — {Message}", ex.Message);
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
