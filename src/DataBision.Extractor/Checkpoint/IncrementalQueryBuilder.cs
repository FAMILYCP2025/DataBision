using DataBision.Extractor.DataBision;

namespace DataBision.Extractor.Checkpoint;

/// <summary>
/// Builds SAP B1 Service Layer OData $filter clauses for incremental extraction.
/// Uses the checkpoint watermarkDate minus a lookback window to avoid missing
/// records that were updated slightly before the last run's watermark.
///
/// Design decision — lookback is expressed in days, not minutes:
/// SAP SL only exposes UpdateDate (no UpdateTS in most endpoints on SL 1000290).
/// Converting LookbackMinutes to days (ceiling, min 1) is conservative and safe.
/// </summary>
public static class IncrementalQueryBuilder
{
    /// <summary>
    /// Builds a $filter string for UpdateDate-based incremental extraction.
    /// Returns null if no checkpoint exists (triggers full limited extraction instead).
    /// </summary>
    public static (string? filter, DateTime? effectiveFrom) Build(
        ExtractorCheckpoint? checkpoint, int lookbackMinutes)
    {
        if (checkpoint?.WatermarkDate is null)
            return (null, null);

        if (!DateTime.TryParse(checkpoint.WatermarkDate, out var wmDate))
            return (null, null);

        // Convert LookbackMinutes to days — SAP SL filters by date, not datetime.
        // Minimum 1 day to catch same-day updates that may have been cut off.
        var lookbackDays = lookbackMinutes > 0
            ? Math.Max(1, (int)Math.Ceiling(lookbackMinutes / 1440.0))
            : 1;

        var effectiveFrom = wmDate.Date.AddDays(-lookbackDays);

        // SAP B1 SL OData date filter — single-quoted date string (confirmed working format)
        var filter = $"UpdateDate ge '{effectiveFrom:yyyy-MM-dd}'";
        return (filter, effectiveFrom);
    }
}
