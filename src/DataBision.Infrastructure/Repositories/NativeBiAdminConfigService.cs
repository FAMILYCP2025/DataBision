using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class NativeBiAdminConfigService(AppDbContext db) : INativeBiAdminConfigService
{
    // ── Filters ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NativeBiFilterConfigDto>> GetFiltersAsync(int companyId, CancellationToken ct = default)
    {
        return await db.NativeBiFilterConfigs
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.FilterKey)
            .Select(x => new NativeBiFilterConfigDto
            {
                CompanyId    = x.CompanyId,
                FilterKey    = x.FilterKey,
                Label        = x.Label,
                IsEnabled    = x.IsEnabled,
                IsAdvanced   = x.IsAdvanced,
                DisplayOrder = x.DisplayOrder,
                DefaultValue = x.DefaultValue
            })
            .ToListAsync(ct);
    }

    public async Task<NativeBiFilterConfigDto> UpsertFilterAsync(
        int companyId, string filterKey, UpsertNativeBiFilterConfigDto dto, CancellationToken ct = default)
    {
        var entity = await db.NativeBiFilterConfigs
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.FilterKey == filterKey, ct);

        if (entity is null)
        {
            entity = new NativeBiFilterConfig
            {
                CompanyId = companyId,
                FilterKey = filterKey,
                CreatedAt = DateTime.UtcNow
            };
            db.NativeBiFilterConfigs.Add(entity);
        }

        entity.Label        = dto.Label;
        entity.IsEnabled    = dto.IsEnabled;
        entity.IsAdvanced   = dto.IsAdvanced;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.DefaultValue = dto.DefaultValue;
        entity.UpdatedAt    = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new NativeBiFilterConfigDto
        {
            CompanyId    = entity.CompanyId,
            FilterKey    = entity.FilterKey,
            Label        = entity.Label,
            IsEnabled    = entity.IsEnabled,
            IsAdvanced   = entity.IsAdvanced,
            DisplayOrder = entity.DisplayOrder,
            DefaultValue = entity.DefaultValue
        };
    }

    // ── Item UDF filters ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NativeBiItemUdfFilterConfigDto>> GetItemUdfFiltersAsync(int companyId, CancellationToken ct = default)
    {
        return await db.NativeBiItemUdfFilterConfigs
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.UdfFieldName)
            .Select(x => new NativeBiItemUdfFilterConfigDto
            {
                CompanyId    = x.CompanyId,
                UdfFieldName = x.UdfFieldName,
                Label        = x.Label,
                IsEnabled    = x.IsEnabled,
                IsMultiSelect = x.IsMultiSelect,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync(ct);
    }

    public async Task<NativeBiItemUdfFilterConfigDto> UpsertItemUdfFilterAsync(
        int companyId, string udfFieldName, UpsertNativeBiItemUdfFilterConfigDto dto, CancellationToken ct = default)
    {
        var entity = await db.NativeBiItemUdfFilterConfigs
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UdfFieldName == udfFieldName, ct);

        if (entity is null)
        {
            entity = new NativeBiItemUdfFilterConfig
            {
                CompanyId    = companyId,
                UdfFieldName = udfFieldName,
                CreatedAt    = DateTime.UtcNow
            };
            db.NativeBiItemUdfFilterConfigs.Add(entity);
        }

        entity.Label        = dto.Label;
        entity.IsEnabled    = dto.IsEnabled;
        entity.IsMultiSelect = dto.IsMultiSelect;
        entity.DisplayOrder = dto.DisplayOrder;
        entity.UpdatedAt    = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new NativeBiItemUdfFilterConfigDto
        {
            CompanyId    = entity.CompanyId,
            UdfFieldName = entity.UdfFieldName,
            Label        = entity.Label,
            IsEnabled    = entity.IsEnabled,
            IsMultiSelect = entity.IsMultiSelect,
            DisplayOrder = entity.DisplayOrder
        };
    }

    // ── Dimensions ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NativeBiDimensionConfigDto>> GetDimensionsAsync(int companyId, CancellationToken ct = default)
    {
        return await db.NativeBiDimensionConfigs
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.DimensionNumber)
            .Select(x => new NativeBiDimensionConfigDto
            {
                CompanyId       = x.CompanyId,
                DimensionNumber = x.DimensionNumber,
                Label           = x.Label,
                IsEnabled       = x.IsEnabled
            })
            .ToListAsync(ct);
    }

    public async Task<NativeBiDimensionConfigDto> UpsertDimensionAsync(
        int companyId, int dimensionNumber, UpsertNativeBiDimensionConfigDto dto, CancellationToken ct = default)
    {
        if (dimensionNumber < 1 || dimensionNumber > 5)
            throw new ArgumentOutOfRangeException(nameof(dimensionNumber), "Dimension number must be 1–5.");

        var entity = await db.NativeBiDimensionConfigs
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.DimensionNumber == dimensionNumber, ct);

        if (entity is null)
        {
            entity = new NativeBiDimensionConfig
            {
                CompanyId       = companyId,
                DimensionNumber = dimensionNumber,
                CreatedAt       = DateTime.UtcNow
            };
            db.NativeBiDimensionConfigs.Add(entity);
        }

        entity.Label     = dto.Label;
        entity.IsEnabled = dto.IsEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new NativeBiDimensionConfigDto
        {
            CompanyId       = entity.CompanyId,
            DimensionNumber = entity.DimensionNumber,
            Label           = entity.Label,
            IsEnabled       = entity.IsEnabled
        };
    }
}
