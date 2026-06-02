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

var slOptions = config.GetSection(SapServiceLayerOptions.Section).Get<SapServiceLayerOptions>()
    ?? new SapServiceLayerOptions();
var apiOptions = config.GetSection(DataBisionApiOptions.Section).Get<DataBisionApiOptions>()
    ?? new DataBisionApiOptions();
var extOptions = config.GetSection(ExtractorOptions.Section).Get<ExtractorOptions>()
    ?? new ExtractorOptions();

services.AddSingleton(slOptions);
services.AddSingleton(apiOptions);
services.AddSingleton(extOptions);
services.AddSingleton<IServiceLayerClient, ServiceLayerClient>();

var sp = services.BuildServiceProvider();
var log = sp.GetRequiredService<ILogger<Program>>();

// ── Args handling ──────────────────────────────────────────────────────────────
// args is the implicit top-level parameter for command-line arguments
if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
        DataBision SAP Extractor — Sprint 3A (Service Layer Validation)

        Usage:
          DataBision.Extractor [options]

        Options:
          --help, -h        Show this help
          --validate        Test login + GET OSLP top 5 + logout (Sprint 3A validation)
          --object <name>   Extract specific object: OSLP, OCRD, OITM, OINV, ALL
          --dry-run         Validate without sending to Ingest API
        """);
    return 0;
}

// ── Validate configuration ─────────────────────────────────────────────────────
log.LogInformation("DataBision Extractor starting...");

if (!args.Contains("--validate") && !args.Contains("--object") && args.Length == 0)
{
    log.LogError("No action specified. Use --help for usage.");
    return 1;
}

try
{
    slOptions.Validate();
    log.LogInformation("Configuration: BaseUrl={BaseUrl}, CompanyDB={CompanyDB}",
        MaskUrl(slOptions.BaseUrl), slOptions.CompanyDB);
}
catch (InvalidOperationException ex)
{
    log.LogError("Configuration error: {Message}", ex.Message);
    log.LogError("Copy appsettings.Development.template.json to appsettings.Development.json and fill in credentials.");
    return 2;
}

// ── Sprint 3A: Validation mode ─────────────────────────────────────────────────
if (args.Contains("--validate"))
{
    log.LogInformation("=== Sprint 3A: Service Layer Validation ===");
    var client = sp.GetRequiredService<IServiceLayerClient>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(slOptions.TimeoutSeconds * 2));

    try
    {
        // P-01: Login
        log.LogInformation("[P-01] Testing login...");
        await client.LoginAsync(cts.Token);
        log.LogInformation("[P-01] PASS — Login successful");

        // P-06: GET OSLP top 5
        // Note: SalesPersons in this SL version does not expose UpdateDate — using minimal select.
        log.LogInformation("[P-06] Testing GET SalesPersons top 5...");
        var oslpRows = await client.GetAsync("SalesPersons",
            "$top=5&$select=SalesEmployeeCode,SalesEmployeeName", cts.Token);
        log.LogInformation("[P-06] PASS — OSLP rows received: {Count}", oslpRows.Count);
        foreach (var row in oslpRows)
            log.LogInformation("  OSLP: {Row}", row?.ToJsonString());

        // P-04: Logout
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

log.LogWarning("No recognized action. Use --help for usage.");
return 1;

static string MaskUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url)) return "(not set)";
    try
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}";
    }
    catch
    {
        return "(invalid url)";
    }
}
