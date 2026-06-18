namespace DataBision.Application.DTOs.Admin;

// ── Filter configs ────────────────────────────────────────────────────────────

public sealed class NativeBiFilterConfigDto
{
    public int CompanyId { get; set; }
    public string FilterKey { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsAdvanced { get; set; }
    public int DisplayOrder { get; set; }
    public string? DefaultValue { get; set; }
}

public sealed class UpsertNativeBiFilterConfigDto
{
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsAdvanced { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public string? DefaultValue { get; set; }
}

// ── Item UDF filter configs ───────────────────────────────────────────────────

public sealed class NativeBiItemUdfFilterConfigDto
{
    public int CompanyId { get; set; }
    public string UdfFieldName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsMultiSelect { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class UpsertNativeBiItemUdfFilterConfigDto
{
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsMultiSelect { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}

// ── Dimension configs ─────────────────────────────────────────────────────────

public sealed class NativeBiDimensionConfigDto
{
    public int CompanyId { get; set; }
    public int DimensionNumber { get; set; }
    public string? Label { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class UpsertNativeBiDimensionConfigDto
{
    public string? Label { get; set; }
    public bool IsEnabled { get; set; } = false;
}
