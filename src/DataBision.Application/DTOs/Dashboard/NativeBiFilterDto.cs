namespace DataBision.Application.DTOs.Dashboard;

/// <summary>
/// Optional filter parameters accepted by all Native BI endpoints.
/// Controllers bind these as [FromQuery] and pass them through to the service layer.
/// Repository implementations apply only the filters they support; unrecognized
/// filters are silently ignored to maintain backward compatibility.
/// </summary>
public sealed record NativeBiFilterDto
{
    // ── Date range ─────────────────────────────────────────────────────────────

    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo   { get; init; }

    /// <summary>Four-digit year (e.g. 2026). Expands to Jan 1–Dec 31 when DateFrom/DateTo absent.</summary>
    public int? Year  { get; init; }

    /// <summary>Month 1–12. Requires Year to be set to be meaningful.</summary>
    public int? Month { get; init; }

    // ── Sales ──────────────────────────────────────────────────────────────────

    /// <summary>gross | net | both. Default: gross (uses gross_sales column).</summary>
    public string? SalesType { get; init; }

    /// <summary>Comma-separated salesperson codes.</summary>
    public string? SalespersonCodes { get; init; }

    /// <summary>Comma-separated customer group codes.</summary>
    public string? CustomerGroupCodes { get; init; }

    // ── Items / Inventory ──────────────────────────────────────────────────────

    /// <summary>Comma-separated item group codes.</summary>
    public string? ItemGroupCodes { get; init; }

    // ── Purchasing / Inventory ─────────────────────────────────────────────────

    /// <summary>Comma-separated supplier group codes.</summary>
    public string? SupplierGroupCodes { get; init; }

    /// <summary>Comma-separated warehouse codes.</summary>
    public string? WarehouseCodes { get; init; }

    public string? WarehouseLocations { get; init; }

    // ── Finance ────────────────────────────────────────────────────────────────

    public string? AccountCodes  { get; init; }
    public string? AccountLevel  { get; init; }

    // ── SAP Dimensions (1–5) ───────────────────────────────────────────────────

    public string? Dimension1 { get; init; }
    public string? Dimension2 { get; init; }
    public string? Dimension3 { get; init; }
    public string? Dimension4 { get; init; }
    public string? Dimension5 { get; init; }

    // ── Operations ─────────────────────────────────────────────────────────────

    public string? Severity    { get; init; }
    public string? ProcessCode { get; init; }
    public string? ObjectCode  { get; init; }

    // ── Diagnostics ────────────────────────────────────────────────────────────

    public string? Schema      { get; init; }
    public string? TableFilter { get; init; }

    // ── Item UDFs (tenant-configured, up to 6) ─────────────────────────────────

    public string? Udf1 { get; init; }
    public string? Udf2 { get; init; }
    public string? Udf3 { get; init; }
    public string? Udf4 { get; init; }
    public string? Udf5 { get; init; }
    public string? Udf6 { get; init; }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Resolve the effective date range from DateFrom/DateTo, or from Year+Month.</summary>
    public (DateTime? From, DateTime? To) EffectiveDateRange()
    {
        if (DateFrom.HasValue || DateTo.HasValue)
            return (DateFrom?.ToDateTime(TimeOnly.MinValue), DateTo?.ToDateTime(TimeOnly.MaxValue));

        if (Year.HasValue)
        {
            var y = Year.Value;
            if (Month is >= 1 and <= 12)
            {
                var lastDay = DateTime.DaysInMonth(y, Month.Value);
                return (new DateTime(y, Month.Value, 1), new DateTime(y, Month.Value, lastDay, 23, 59, 59));
            }
            return (new DateTime(y, 1, 1), new DateTime(y, 12, 31, 23, 59, 59));
        }

        return (null, null);
    }

    /// <summary>Split a comma-separated filter value into a trimmed, non-empty array.</summary>
    public static string[] SplitCodes(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
