namespace DataBision.Application.DTOs.Admin;

public sealed class NativeBiClientFilterConfigDto
{
    public IReadOnlyList<NativeBiFilterConfigDto>        Filters         { get; set; } = [];
    public IReadOnlyList<NativeBiItemUdfFilterConfigDto> ItemUdfFilters  { get; set; } = [];
    public IReadOnlyList<NativeBiDimensionConfigDto>     Dimensions      { get; set; } = [];
}
