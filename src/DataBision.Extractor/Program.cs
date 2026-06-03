using DataBision.Extractor.DataBision;
using DataBision.Extractor.Extraction;
using DataBision.Extractor.Extraction.Jobs;
using DataBision.Extractor.Options;
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
services.AddSingleton<ExtractorRunner>(sp => new ExtractorRunner(
    new IExtractorJob[]
    {
        sp.GetRequiredService<OslpExtractorJob>(),
        sp.GetRequiredService<OcrdExtractorJob>(),
        sp.GetRequiredService<OitmExtractorJob>(),
        sp.GetRequiredService<OinvExtractorJob>(),
    },
    sp.GetRequiredService<ExtractorOptions>(),
    sp.GetRequiredService<ILogger<ExtractorRunner>>()));
// jobs above require IDataBisionIngestClient — registered above ✅

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
          --help, -h              Show this help
          --validate              Test Service Layer login + GET OSLP top 5 + logout
          --dry-run               Show resolved configuration (no connections, no data)
          --object <name>         Extract object: OSLP | OCRD | OITM | OINV | ALL
          --object <name> --send  Extract and send to DataBision Ingest API
          --object <name> --dry-run  Validate object name and show planned extraction

        Examples:
          dotnet run -- --validate
          dotnet run -- --dry-run
          dotnet run -- --object OINV
          dotnet run -- --object OSLP --send
          dotnet run -- --object ALL --send
          dotnet run -- --object ALL --dry-run
        """);
    return 0;
}

// ── Startup log ───────────────────────────────────────────────────────────────
log.LogInformation("DataBision Extractor starting...");

// ── Validate configuration ─────────────────────────────────────────────────────
if (args.Length == 0)
{
    log.LogError("No action specified. Use --help for usage.");
    return 1;
}

// Configuration validation — always run even for --dry-run (minus secret fields for dry-run)
bool isDryRun  = args.Contains("--dry-run");
bool isSend    = args.Contains("--send");
string? objectArg = null;
var objIdx = Array.IndexOf(args, "--object");
if (objIdx >= 0 && objIdx + 1 < args.Length)
    objectArg = args[objIdx + 1];

if (isDryRun && !args.Contains("--validate") && objectArg is null)
{
    // --dry-run alone: show config without connecting
    log.LogInformation("=== DRY-RUN: configuration check ===");
    var slValid  = ValidateSlSilent(slOptions);
    var apiValid = ValidateApiSilent(apiOptions);
    var extValid = ValidateExtSilent(extOptions);

    log.LogInformation("SapServiceLayer.BaseUrl:    {Url}",      MaskUrl(slOptions.BaseUrl));
    log.LogInformation("SapServiceLayer.CompanyDB:  {Db}",       slOptions.CompanyDB.Length > 0 ? "[set]" : "[MISSING]");
    log.LogInformation("SapServiceLayer.UserName:   {U}",        slOptions.UserName.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("SapServiceLayer.Password:   {P}",        slOptions.Password.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("SapServiceLayer.IgnoreSSL:  {Ssl}",      slOptions.IgnoreSslCertificateErrors);
    log.LogInformation("DataBisionApi.BaseUrl:       {Url}",     apiOptions.BaseUrl);
    log.LogInformation("DataBisionApi.ApiKey:        {K}",       apiOptions.ApiKey.Length > 0   ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.TenantId:          {T}",       extOptions.TenantId.Length > 0  ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.CompanyId:         {C}",       extOptions.CompanyId.Length > 0 ? "[set]" : "[MISSING]");
    log.LogInformation("Extractor.Mode:              {M}",       extOptions.Mode);
    log.LogInformation("Extractor.PageSize:          {Ps}",      extOptions.PageSize);
    log.LogInformation("Extractor.LookbackMinutes:   {Lm}",      extOptions.LookbackMinutes);

    if (!slValid || !apiValid || !extValid)
    {
        log.LogError("Configuration incomplete — fill in appsettings.Development.json.");
        return 2;
    }
    log.LogInformation("=== DRY-RUN: configuration OK — no connections made ===");
    return 0;
}

// For all other modes, full config validation is required
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
    log.LogError("Copy appsettings.Development.template.json → appsettings.Development.json and fill in credentials.");
    return 2;
}

// ── --validate ─────────────────────────────────────────────────────────────────
if (args.Contains("--validate"))
{
    log.LogInformation("=== Sprint 3A: Service Layer Validation ===");
    var client = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(slOptions.TimeoutSeconds * 2));

    try
    {
        log.LogInformation("[P-01] Testing login...");
        await client.LoginAsync(cts.Token);
        log.LogInformation("[P-01] PASS — Login successful");

        // Note: SalesPersons does not expose UpdateDate in SL 1000290 — field removed.
        log.LogInformation("[P-06] Testing GET SalesPersons top 5...");
        var oslpRows = await client.GetAsync("SalesPersons",
            "$top=5&$select=SalesEmployeeCode,SalesEmployeeName", cts.Token);
        log.LogInformation("[P-06] PASS — OSLP rows received: {Count}", oslpRows.Count);
        foreach (var row in oslpRows)
            log.LogInformation("  OSLP: {Row}", row?.ToJsonString());

        log.LogInformation("[P-04] Testing logout...");
        await client.LogoutAsync(cts.Token);
        log.LogInformation("[P-04] PASS — Logout completed");

        log.LogInformation("=== Sprint 3A Validation: ALL PASS ===");
        return 0;
    }
    catch (Exception ex)
    {
        log.LogError(ex, "=== Sprint 3A Validation: FAIL — {Message}", ex.Message);
        return 3;
    }
}

// ── --object ───────────────────────────────────────────────────────────────────
if (objectArg is not null)
{
    if (!ExtractorRunner.IsSupported(objectArg))
    {
        log.LogError("Unknown object '{Obj}'. Supported: OSLP, OCRD, OITM, OINV, ALL", objectArg);
        return 1;
    }

    if (isDryRun)
    {
        log.LogInformation("=== DRY-RUN: --object {Obj} ===", objectArg.ToUpperInvariant());
        log.LogInformation("TenantId:   {T}", extOptions.TenantId);
        log.LogInformation("CompanyId:  {C}", extOptions.CompanyId);
        log.LogInformation("Mode:       {M}", extOptions.Mode);
        log.LogInformation("PageSize:   {Ps}", extOptions.PageSize);
        log.LogInformation("Lookback:   {Lm} minutes", extOptions.LookbackMinutes);
        log.LogInformation("Extraction of {Obj}: NOT STARTED (dry-run — no connection made)", objectArg.ToUpperInvariant());
        log.LogInformation("=== DRY-RUN complete — Sprint 3C will implement real extraction ===");
        return 0;
    }

    // Real extraction
    log.LogInformation("=== Extraction: {Obj} (send={Send}) ===", objectArg.ToUpperInvariant(), isSend);
    var runner   = sp.GetRequiredService<ExtractorRunner>();
    var slClient = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

    try
    {
        await slClient.LoginAsync(cts.Token);
        var results = await runner.RunAsync(objectArg, dryRun: false, send: isSend, cts.Token);
        var anyFail = results.Any(r => !r.Success);
        return anyFail ? 4 : 0;
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
    try
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}";
    }
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
