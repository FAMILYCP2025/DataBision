using DataBision.Domain.Entities;
using DataBision.Domain.Enums;
using DataBision.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataBision.Infrastructure.Seed;

public class DatabaseSeeder(AppDbContext db, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync()
    {
        await SeedModulesAsync();
        await SeedSuperAdminAsync();
        await SeedDemoCompanyAsync();
    }

    // ── Modules ───────────────────────────────────────────────────────────────

    private async Task SeedModulesAsync()
    {
        if (await db.Modules.AnyAsync()) return;

        db.Modules.AddRange(
            new Module { Name = "Comercial",    Slug = "comercial",    Icon = "trending-up",  SortOrder = 1 },
            new Module { Name = "Facturación",  Slug = "facturacion",  Icon = "file-text",    SortOrder = 2 },
            new Module { Name = "Canales",      Slug = "canales",      Icon = "git-branch",   SortOrder = 3 },
            new Module { Name = "Inventario",   Slug = "inventario",   Icon = "package",      SortOrder = 4 },
            new Module { Name = "Finanzas",     Slug = "finanzas",     Icon = "dollar-sign",  SortOrder = 5 },
            new Module { Name = "Flujo Venta",  Slug = "flujo-venta",  Icon = "bar-chart-2",  SortOrder = 6 }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Modules seeded (6).");
    }

    // ── SuperAdmin ────────────────────────────────────────────────────────────

    private async Task SeedSuperAdminAsync()
    {
        const string adminEmail = "admin@databision.app";
        if (await db.Users.AnyAsync(u => u.Email == adminEmail)) return;

        db.Users.Add(new User
        {
            Email       = adminEmail,
            PasswordHash= BCrypt.Net.BCrypt.HashPassword("Admin@DataBision2026!"),
            FirstName   = "Super",
            LastName    = "Admin",
            Role        = UserRole.SuperAdmin,
            IsActive    = true
        });

        await db.SaveChangesAsync();
        logger.LogInformation("SuperAdmin seeded.");
    }

    // ── Demo company + users + reports + permissions ───────────────────────────

    private async Task SeedDemoCompanyAsync()
    {
        // Idempotent: skip if demo company exists
        if (await db.Companies.AnyAsync(c => c.Slug == "demo")) return;

        // 1. Company
        var company = new Company
        {
            Name   = "Demo Company",
            Slug   = "demo",
            Status = CompanyStatus.Active
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // 2. Users
        var admin = new User
        {
            Email        = "admin@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@Admin2026!"),
            FirstName    = "Company",
            LastName     = "Admin",
            Role         = UserRole.CompanyAdmin,
            IsActive     = true
        };
        var viewer = new User
        {
            Email        = "viewer@demo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@Viewer2026!"),
            FirstName    = "Viewer",
            LastName     = "User",
            Role         = UserRole.Viewer,
            IsActive     = true
        };
        db.Users.AddRange(admin, viewer);
        await db.SaveChangesAsync();

        // 3. Link users to company
        db.UserCompanies.AddRange(
            new UserCompany { UserId = admin.Id,  CompanyId = company.Id },
            new UserCompany { UserId = viewer.Id, CompanyId = company.Id }
        );
        await db.SaveChangesAsync();

        // 4. Load modules by slug for stable FK references
        var modules = await db.Modules.ToDictionaryAsync(m => m.Slug);

        // 5. Reports per module (2–3 each)
        var now = DateTime.UtcNow;
        var reports = new List<Report>
        {
            // Comercial
            new() { ModuleId = modules["comercial"].Id,   CompanyId = company.Id, Name = "Resumen de Ventas",          Description = "KPIs principales del equipo comercial",              SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-2), WorkspaceId = "ws-1", ReportId = "rep-1", DatasetId = "ds-1", EmbedUrl = "" },
            new() { ModuleId = modules["comercial"].Id,   CompanyId = company.Id, Name = "Pipeline de Oportunidades",  Description = "Estado actual del pipeline por etapa",               SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-1), WorkspaceId = "ws-1", ReportId = "rep-2", DatasetId = "ds-1", EmbedUrl = "" },
            new() { ModuleId = modules["comercial"].Id,   CompanyId = company.Id, Name = "Análisis por Vendedor",      Description = "Performance individual por representante",           SortOrder = 3, IsActive = true, CreatedAt = now.AddDays(-3), WorkspaceId = "ws-1", ReportId = "rep-3", DatasetId = "ds-1", EmbedUrl = "" },
            // Facturación
            new() { ModuleId = modules["facturacion"].Id, CompanyId = company.Id, Name = "Cuentas por Cobrar",         Description = "Antigüedad de cartera y vencimientos",               SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-1), WorkspaceId = "ws-1", ReportId = "rep-4", DatasetId = "ds-2", EmbedUrl = "" },
            new() { ModuleId = modules["facturacion"].Id, CompanyId = company.Id, Name = "Facturación Mensual",        Description = "Evolución de facturación por mes",                  SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-4), WorkspaceId = "ws-1", ReportId = "rep-5", DatasetId = "ds-2", EmbedUrl = "" },
            // Canales
            new() { ModuleId = modules["canales"].Id,     CompanyId = company.Id, Name = "Ventas por Canal",           Description = "Comparativo de desempeño por canal de distribución",SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-2), WorkspaceId = "ws-1", ReportId = "rep-6", DatasetId = "ds-3", EmbedUrl = "" },
            new() { ModuleId = modules["canales"].Id,     CompanyId = company.Id, Name = "Cobertura Geográfica",       Description = "Distribución territorial de clientes y ventas",     SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-5), WorkspaceId = "ws-1", ReportId = "rep-7", DatasetId = "ds-3", EmbedUrl = "" },
            new() { ModuleId = modules["canales"].Id,     CompanyId = company.Id, Name = "Sell-out por Punto de Venta",Description = "Venta al consumidor final por PDV",                 SortOrder = 3, IsActive = true, CreatedAt = now.AddDays(-6), WorkspaceId = "ws-1", ReportId = "rep-8", DatasetId = "ds-3", EmbedUrl = "" },
            // Inventario
            new() { ModuleId = modules["inventario"].Id,  CompanyId = company.Id, Name = "Stock Actual",               Description = "Inventario disponible por SKU y almacén",           SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-1), WorkspaceId = "ws-1", ReportId = "rep-9", DatasetId = "ds-4", EmbedUrl = "" },
            new() { ModuleId = modules["inventario"].Id,  CompanyId = company.Id, Name = "Movimientos de Inventario",  Description = "Entradas, salidas y transferencias",                SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-2), WorkspaceId = "ws-1", ReportId = "rep-10", DatasetId = "ds-4", EmbedUrl = "" },
            // Finanzas
            new() { ModuleId = modules["finanzas"].Id,    CompanyId = company.Id, Name = "Estado de Resultados",       Description = "P&L mensual y acumulado",                           SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-1), WorkspaceId = "ws-1", ReportId = "rep-11", DatasetId = "ds-5", EmbedUrl = "" },
            new() { ModuleId = modules["finanzas"].Id,    CompanyId = company.Id, Name = "Flujo de Caja",              Description = "Proyección y control de liquidez",                  SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-2), WorkspaceId = "ws-1", ReportId = "rep-12", DatasetId = "ds-5", EmbedUrl = "" },
            new() { ModuleId = modules["finanzas"].Id,    CompanyId = company.Id, Name = "Balance General",            Description = "Activos, pasivos y patrimonio",                     SortOrder = 3, IsActive = true, CreatedAt = now.AddDays(-4), WorkspaceId = "ws-1", ReportId = "rep-13", DatasetId = "ds-5", EmbedUrl = "" },
            // Flujo Venta
            new() { ModuleId = modules["flujo-venta"].Id, CompanyId = company.Id, Name = "Embudo de Conversión",       Description = "Tasas de conversión por etapa del proceso",         SortOrder = 1, IsActive = true, CreatedAt = now.AddDays(-1), WorkspaceId = "ws-1", ReportId = "rep-14", DatasetId = "ds-6", EmbedUrl = "" },
            new() { ModuleId = modules["flujo-venta"].Id, CompanyId = company.Id, Name = "Tiempos de Ciclo",           Description = "Duración promedio en cada etapa de venta",          SortOrder = 2, IsActive = true, CreatedAt = now.AddDays(-3), WorkspaceId = "ws-1", ReportId = "rep-15", DatasetId = "ds-6", EmbedUrl = "" },
        };
        db.Reports.AddRange(reports);
        await db.SaveChangesAsync();

        // 6. Viewer permissions: report-level only. UserPermission is the single source of truth
        //    and ReportId=null no longer grants anything (see PermissionRepository.HasPermissionAsync /
        //    HasModulePermissionAsync). CompanyAdmin has no rows — bypassed by role check in ModuleService.
        var comercialModule    = modules["comercial"];
        var facturacionModule  = modules["facturacion"];
        var resumenVentas      = reports.First(r => r.Name == "Resumen de Ventas");
        var cxcReport          = reports.First(r => r.Name == "Cuentas por Cobrar");

        db.UserPermissions.AddRange(
            // Viewer: one specific Comercial report
            new UserPermission { UserId = viewer.Id, CompanyId = company.Id, ModuleId = comercialModule.Id,   ReportId = resumenVentas.Id, CanView = true, GrantedBy = admin.Id },
            // Viewer: one specific Facturacion report
            new UserPermission { UserId = viewer.Id, CompanyId = company.Id, ModuleId = facturacionModule.Id, ReportId = cxcReport.Id,     CanView = true, GrantedBy = admin.Id }
        );
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Demo company seeded: company={Slug}, admin={AdminEmail}, viewer={ViewerEmail}, reports={Count}",
            company.Slug, admin.Email, viewer.Email, reports.Count);
    }
}

