using DataBision.Application.DTOs.Admin;

namespace DataBision.Application.Interfaces;

public interface INativeBiConnectionProfileService
{
    Task<IReadOnlyList<NativeBiConnectionProfileDto>> GetAllAsync(int companyId, CancellationToken ct = default);
    Task<NativeBiConnectionProfileDto?> GetByIdAsync(int companyId, int profileId, CancellationToken ct = default);
    Task<(NativeBiConnectionProfileDto? result, string? error)> CreateAsync(int companyId, CreateNativeBiConnectionProfileRequest request, CancellationToken ct = default);
    Task<(NativeBiConnectionProfileDto? result, string? error)> UpdateAsync(int companyId, int profileId, UpdateNativeBiConnectionProfileRequest request, CancellationToken ct = default);
    Task<string?> DeleteAsync(int companyId, int profileId, CancellationToken ct = default);
    Task<TestNativeBiConnectionProfileResult> TestAsync(int companyId, int profileId, CancellationToken ct = default);

    /// <summary>
    /// Resolves the full connection profile including the decrypted SAP password.
    /// Only for use by the internal extractor endpoint — never expose via admin or public APIs.
    /// </summary>
    Task<(ResolveNativeBiConnectionProfileResponse? result, string? error)> ResolveForExtractorAsync(
        string analyticsCompanyId,
        string? profileName,
        int? profileId,
        CancellationToken ct = default);
}
