namespace DataBision.Extractor.Options;

public sealed class ExtractorOptions
{
    public const string Section = "Extractor";

    // ── Identity ──────────────────────────────────────────────────────────────
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;

    // ── Extraction ────────────────────────────────────────────────────────────
    public string Mode { get; init; } = "INCREMENTAL";
    public int PageSize { get; init; } = 100;

    /// <summary>Minutes to look back from the watermark when building the incremental filter.</summary>
    public int LookbackMinutes { get; init; } = 10;

    // ── Scheduler ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Objects executed by --run-once and --schedule.
    /// INV1/RIN1/OSLP are excluded by default — use explicit --object for those.
    /// Value comes from appsettings.json; empty array as default avoids binding duplication.
    /// </summary>
    public string[] Objects { get; init; } = [];

    /// <summary>Minutes between cycles in --schedule mode.</summary>
    public int IntervalMinutes { get; init; } = 30;

    /// <summary>Optional cap on cycles for testing. Null = unlimited.</summary>
    public int? MaxCycles { get; init; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
            throw new InvalidOperationException("Extractor:TenantId is required.");
        if (string.IsNullOrWhiteSpace(CompanyId))
            throw new InvalidOperationException("Extractor:CompanyId is required.");
    }
}
