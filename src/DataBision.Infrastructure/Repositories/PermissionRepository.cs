using DataBision.Application.Interfaces;
using DataBision.Domain.Entities;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Repositories;

public class PermissionRepository(AppDbContext db) : IPermissionRepository
{
    public Task<Report?> GetReportAsync(int reportId)
        => db.Reports.FindAsync(reportId).AsTask();

    public Task<bool> UserBelongsToCompanyAsync(int userId, int companyId)
        => db.UserCompanies.AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);

    public Task<bool> HasPermissionAsync(int userId, int companyId, int moduleId, int reportId)
        => db.UserPermissions.AnyAsync(p =>
            p.UserId == userId &&
            p.CompanyId == companyId &&
            p.ModuleId == moduleId &&
            p.CanView &&
            p.ReportId == reportId);

    public Task<bool> IsCompanyAdminAsync(int userId, int companyId)
        => db.UserCompanies
            .Where(uc => uc.UserId == userId && uc.CompanyId == companyId)
            .Join(db.Users, uc => uc.UserId, u => u.Id, (uc, u) => u)
            .AnyAsync(u => u.Role == Domain.Enums.UserRole.CompanyAdmin);

    public Task<bool> HasModulePermissionAsync(int userId, int companyId, int moduleId)
        => db.UserPermissions.AnyAsync(p =>
            p.UserId == userId &&
            p.CompanyId == companyId &&
            p.ModuleId == moduleId &&
            p.ReportId != null &&
            p.CanView);

    public async Task UpsertPermissionsAsync(int companyId, int grantedBy, IEnumerable<PermissionUpdateDto> updates)
    {
        foreach (var u in updates)
        {
            var existing = await db.UserPermissions.FirstOrDefaultAsync(p =>
                p.UserId == u.UserId &&
                p.CompanyId == companyId &&
                p.ModuleId == u.ModuleId &&
                p.ReportId == u.ReportId);

            if (existing is null)
            {
                db.UserPermissions.Add(new UserPermission
                {
                    UserId = u.UserId,
                    CompanyId = companyId,
                    ModuleId = u.ModuleId,
                    ReportId = u.ReportId,
                    CanView = u.CanView,
                    GrantedBy = grantedBy
                });
            }
            else
            {
                existing.CanView = u.CanView;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task ReplaceUserPermissionsAsync(int companyId, int targetUserId, int grantedBy, IEnumerable<PermissionUpdateDto> updates)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            // Eliminar todos los permisos actuales del usuario en esta compañía
            var existing = await db.UserPermissions
                .Where(p => p.UserId == targetUserId && p.CompanyId == companyId)
                .ToListAsync();

            db.UserPermissions.RemoveRange(existing);
            await db.SaveChangesAsync();

            // Permisos válidos: deben ser para el usuario objetivo, tener ReportId concreto
            // (un row con ReportId=null es ambiguo y no concede nada en HasPermissionAsync /
            // HasModulePermissionAsync — se descarta para evitar filas muertas).
            foreach (var u in updates)
            {
                if (u.UserId != targetUserId) continue;
                if (!u.ReportId.HasValue) continue;

                db.UserPermissions.Add(new UserPermission
                {
                    UserId = u.UserId,
                    CompanyId = companyId,
                    ModuleId = u.ModuleId,
                    ReportId = u.ReportId,
                    CanView = u.CanView,
                    GrantedBy = grantedBy
                });
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<UserPermission>> GetForCompanyAsync(int companyId)
        => await db.UserPermissions
            .Where(p => p.CompanyId == companyId)
            .ToListAsync();
}
