using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface INativeBiAdminConfigService
{
    Task<IReadOnlyList<NativeBiFilterConfigDto>> GetFiltersAsync(int companyId, CancellationToken ct = default);
    Task<NativeBiFilterConfigDto> UpsertFilterAsync(int companyId, string filterKey, UpsertNativeBiFilterConfigDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<NativeBiItemUdfFilterConfigDto>> GetItemUdfFiltersAsync(int companyId, CancellationToken ct = default);
    Task<NativeBiItemUdfFilterConfigDto> UpsertItemUdfFilterAsync(int companyId, string udfFieldName, UpsertNativeBiItemUdfFilterConfigDto dto, CancellationToken ct = default);

    Task<IReadOnlyList<NativeBiDimensionConfigDto>> GetDimensionsAsync(int companyId, CancellationToken ct = default);
    Task<NativeBiDimensionConfigDto> UpsertDimensionAsync(int companyId, int dimensionNumber, UpsertNativeBiDimensionConfigDto dto, CancellationToken ct = default);
}
