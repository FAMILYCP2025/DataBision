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

    /// <summary>Safety cap: max pages per object per run. Default 500 (= 50k rows at PageSize 100).</summary>
    public int MaxPages { get; init; } = 500;

    /// <summary>Minutes to look back from the watermark when building the incremental filter.</summary>
    public int LookbackMinutes { get; init; } = 10;

    // ── Scheduler ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Objects executed by --run-once and --schedule.
    /// INV1/RIN1/OSLP are excluded by default — use explicit --object for those.
    /// Value comes from appsettings.json; empty array as default avoids binding duplication.
    /// </summary>
    public string[] Objects { get; init; } = [];

    /// <summary>
    /// When true, extracted rows are sent to the Ingest API.
    /// Default false — must be explicitly enabled in production config or via --send CLI flag.
    /// </summary>
    public bool SendEnabled { get; init; } = false;

    /// <summary>Minutes between cycles in --schedule mode.</summary>
    public int IntervalMinutes { get; init; } = 30;

    /// <summary>Optional cap on cycles for testing. Null = unlimited.</summary>
    public int? MaxCycles { get; init; } = null;

    // ── Accounting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Max concurrent SAP Service Layer GET requests when fetching individual
    /// JournalEntries(N) for JDT1 line extraction. Default 3.
    /// Increase for faster extraction; decrease if SL returns 429/session errors.
    /// </summary>
    public int JournalEntryLineFetchConcurrency { get; init; } = 3;

    // ── Connection profile (Sprint 22C) ───────────────────────────────────────
    // When set, the extractor will (in a future sprint) load SL credentials from
    // the DataBision API instead of appsettings. Currently parsed but not resolved.

    /// <summary>Name of the NativeBiConnectionProfile to load at startup (e.g. "produccion").</summary>
    public string? ProfileName { get; init; } = null;

    /// <summary>ID of the NativeBiConnectionProfile to load at startup.</summary>
    public int? ConnectionProfileId { get; init; } = null;

    // ── Post-extraction transform ─────────────────────────────────────────────

    /// <summary>
    /// When true, runs refresh_accounting_all after each extraction cycle completes.
    /// Requires Staging:ConnectionString to be set. Default false.
    /// </summary>
    public bool RunMartRefreshAfterExtraction { get; init; } = false;

    /// <summary>
    /// Company ID to use for MART refresh after extraction.
    /// Defaults to Extractor:CompanyId if not set.
    /// </summary>
    public string? MartRefreshCompanyId { get; init; } = null;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TenantId))
            throw new InvalidOperationException("Extractor:TenantId is required.");
        if (string.IsNullOrWhiteSpace(CompanyId))
            throw new InvalidOperationException("Extractor:CompanyId is required.");
    }
}
