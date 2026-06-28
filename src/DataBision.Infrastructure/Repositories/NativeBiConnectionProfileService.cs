using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using DataBision.Application.Services;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataBision.Infrastructure.Repositories;

public sealed class NativeBiConnectionProfileService(
    AppDbContext db,
    ISecretRefResolver secretRefResolver,
    INativeBiSapConnectionTester tester,
    ILogger<NativeBiConnectionProfileService> log) : INativeBiConnectionProfileService
{
    public async Task<IReadOnlyList<NativeBiConnectionProfileDto>> GetAllAsync(
        int companyId, CancellationToken ct = default)
    {
        return await db.NativeBiConnectionProfiles
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.EnvironmentName).ThenBy(p => p.ProfileName)
            .Select(p => ToDto(p))
            .ToListAsync(ct);
    }

    public async Task<NativeBiConnectionProfileDto?> GetByIdAsync(
        int companyId, int profileId, CancellationToken ct = default)
    {
        var p = await FindAsync(companyId, profileId, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<(NativeBiConnectionProfileDto? result, string? error)> CreateAsync(
        int companyId, CreateNativeBiConnectionProfileRequest request, CancellationToken ct = default)
    {
        var validationError = ValidateRequest(request.ProfileName, request.ServiceLayerBaseUrl,
            request.CompanyDb, request.SapUserName, request.SecretRef,
            request.TimeoutSeconds, request.FetchConcurrency);
        if (validationError is not null) return (null, validationError);

        var companyExists = await db.Companies.AnyAsync(c => c.Id == companyId, ct);
        if (!companyExists) return (null, "company_not_found");

        var nameConflict = await db.NativeBiConnectionProfiles
            .AnyAsync(p => p.CompanyId == companyId && p.ProfileName == request.ProfileName, ct);
        if (nameConflict) return (null, "profile_name_taken");

        var entity = new NativeBiConnectionProfile
        {
            CompanyId           = companyId,
            ProfileName         = request.ProfileName.Trim(),
            EnvironmentName     = request.EnvironmentName.Trim(),
            ServiceLayerBaseUrl = request.ServiceLayerBaseUrl.Trim(),
            CompanyDb           = request.CompanyDb.Trim(),
            SapUserName         = request.SapUserName.Trim(),
            SecretRef           = request.SecretRef.Trim(),
            IsActive            = request.IsActive,
            IgnoreSslErrors     = request.IgnoreSslErrors,
            TimeoutSeconds      = request.TimeoutSeconds,
            FetchConcurrency    = request.FetchConcurrency,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow
        };

        db.NativeBiConnectionProfiles.Add(entity);
        await db.SaveChangesAsync(ct);

        log.LogInformation("NativeBiConnectionProfile created: id={Id} company={CompanyId} profile={Name}",
            entity.Id, companyId, entity.ProfileName);

        return (ToDto(entity), null);
    }

    public async Task<(NativeBiConnectionProfileDto? result, string? error)> UpdateAsync(
        int companyId, int profileId, UpdateNativeBiConnectionProfileRequest request, CancellationToken ct = default)
    {
        var entity = await FindAsync(companyId, profileId, ct);
        if (entity is null) return (null, "profile_not_found");

        var secretRef = request.SecretRef is not null ? request.SecretRef : entity.SecretRef;

        var validationError = ValidateRequest(request.ProfileName, request.ServiceLayerBaseUrl,
            request.CompanyDb, request.SapUserName, secretRef,
            request.TimeoutSeconds, request.FetchConcurrency);
        if (validationError is not null) return (null, validationError);

        var nameConflict = await db.NativeBiConnectionProfiles
            .AnyAsync(p => p.CompanyId == companyId && p.ProfileName == request.ProfileName && p.Id != profileId, ct);
        if (nameConflict) return (null, "profile_name_taken");

        entity.ProfileName         = request.ProfileName.Trim();
        entity.EnvironmentName     = request.EnvironmentName.Trim();
        entity.ServiceLayerBaseUrl = request.ServiceLayerBaseUrl.Trim();
        entity.CompanyDb           = request.CompanyDb.Trim();
        entity.SapUserName         = request.SapUserName.Trim();
        if (request.SecretRef is not null) entity.SecretRef = request.SecretRef.Trim();
        entity.IsActive            = request.IsActive;
        entity.IgnoreSslErrors     = request.IgnoreSslErrors;
        entity.TimeoutSeconds      = request.TimeoutSeconds;
        entity.FetchConcurrency    = request.FetchConcurrency;
        entity.UpdatedAt           = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        log.LogInformation("NativeBiConnectionProfile updated: id={Id} company={CompanyId}", profileId, companyId);

        return (ToDto(entity), null);
    }

    public async Task<string?> DeleteAsync(int companyId, int profileId, CancellationToken ct = default)
    {
        var entity = await FindAsync(companyId, profileId, ct);
        if (entity is null) return "profile_not_found";

        db.NativeBiConnectionProfiles.Remove(entity);
        await db.SaveChangesAsync(ct);

        log.LogInformation("NativeBiConnectionProfile deleted: id={Id} company={CompanyId}", profileId, companyId);
        return null;
    }

    public async Task<TestNativeBiConnectionProfileResult> TestAsync(
        int companyId, int profileId, CancellationToken ct = default)
    {
        var entity = await FindAsync(companyId, profileId, ct);
        if (entity is null)
        {
            return new TestNativeBiConnectionProfileResult
            {
                Success    = false,
                CheckedAt  = DateTime.UtcNow,
                Message    = "Profile not found.",
                Capabilities = new TestCapabilities()
            };
        }

        return await tester.TestAsync(entity, ct);
    }

    public async Task<(ResolveNativeBiConnectionProfileResponse? result, string? error)> ResolveForExtractorAsync(
        string analyticsCompanyId,
        string? profileName,
        int? profileId,
        CancellationToken ct = default)
    {
        var company = await db.Companies
            .FirstOrDefaultAsync(c => c.AnalyticsCompanyId == analyticsCompanyId, ct);
        if (company is null) return (null, "company_not_found");

        NativeBiConnectionProfile? profile;
        if (profileId.HasValue)
        {
            profile = await db.NativeBiConnectionProfiles
                .FirstOrDefaultAsync(p => p.CompanyId == company.Id && p.Id == profileId.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(profileName))
        {
            profile = await db.NativeBiConnectionProfiles
                .FirstOrDefaultAsync(p => p.CompanyId == company.Id && p.ProfileName == profileName, ct);
        }
        else
        {
            profile = await db.NativeBiConnectionProfiles
                .Where(p => p.CompanyId == company.Id && p.IsActive)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (profile is null) return (null, "profile_not_found");
        if (!profile.IsActive) return (null, "profile_inactive");

        if (profile.IgnoreSslErrors)
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (string.Equals(envName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                log.LogError(
                    "SECURITY BLOCK: Profile {ProfileId} for company '{CompanyId}' has IgnoreSslErrors=true " +
                    "but ASPNETCORE_ENVIRONMENT=Production. Credential resolution refused.",
                    profile.Id, company.Id);
                return (null, "ignore_ssl_blocked_in_production");
            }
        }

        string sapPassword;
        try
        {
            sapPassword = secretRefResolver.Resolve(profile.SecretRef);
        }
        catch (Exception ex)
        {
            log.LogError("SecretRef resolution failed for profile {ProfileId}: {Message}", profile.Id, ex.Message);
            return (null, "secret_resolution_failed");
        }

        return (new ResolveNativeBiConnectionProfileResponse
        {
            ProfileId           = profile.Id,
            ProfileName         = profile.ProfileName,
            ServiceLayerBaseUrl = profile.ServiceLayerBaseUrl,
            CompanyDb           = profile.CompanyDb,
            SapUserName         = profile.SapUserName,
            SapPassword         = sapPassword,
            IgnoreSslErrors     = profile.IgnoreSslErrors,
            TimeoutSeconds      = profile.TimeoutSeconds,
            FetchConcurrency    = profile.FetchConcurrency,
        }, null);
    }

    private Task<NativeBiConnectionProfile?> FindAsync(int companyId, int profileId, CancellationToken ct)
        => db.NativeBiConnectionProfiles
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == profileId, ct);

    private NativeBiConnectionProfileDto ToDto(NativeBiConnectionProfile p) =>
        new()
        {
            Id                      = p.Id,
            CompanyId               = p.CompanyId,
            ProfileName             = p.ProfileName,
            EnvironmentName         = p.EnvironmentName,
            ServiceLayerBaseUrl     = p.ServiceLayerBaseUrl,
            CompanyDb               = p.CompanyDb,
            SapUserName             = p.SapUserName,
            SecretRefHint           = secretRefResolver.ToHint(p.SecretRef),
            IsActive                = p.IsActive,
            IgnoreSslErrors         = p.IgnoreSslErrors,
            TimeoutSeconds          = p.TimeoutSeconds,
            FetchConcurrency        = p.FetchConcurrency,
            CreatedAt               = p.CreatedAt,
            UpdatedAt               = p.UpdatedAt
        };

    private static string? ValidateRequest(string profileName, string baseUrl, string companyDb,
        string sapUser, string secretRef, int timeoutSeconds, int fetchConcurrency)
    {
        if (string.IsNullOrWhiteSpace(profileName))    return "profile_name_required";
        if (profileName.Length > 100)                  return "profile_name_too_long";
        if (string.IsNullOrWhiteSpace(baseUrl))        return "service_layer_base_url_required";
        if (string.IsNullOrWhiteSpace(companyDb))      return "company_db_required";
        if (string.IsNullOrWhiteSpace(sapUser))        return "sap_user_name_required";
        if (string.IsNullOrWhiteSpace(secretRef))      return "secret_ref_required";
        if (timeoutSeconds  < 10 || timeoutSeconds  > 300) return "timeout_seconds_out_of_range";
        if (fetchConcurrency < 1 || fetchConcurrency > 10) return "fetch_concurrency_out_of_range";
        return null;
    }
}
