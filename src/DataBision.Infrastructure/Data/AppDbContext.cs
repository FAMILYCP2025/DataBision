using DataBision.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyBranding> CompanyBrandings => Set<CompanyBranding>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    // EtlConfig excluded until Step 12 (ADF integration)
    public DbSet<NativeBiFilterConfig> NativeBiFilterConfigs => Set<NativeBiFilterConfig>();
    public DbSet<NativeBiItemUdfFilterConfig> NativeBiItemUdfFilterConfigs => Set<NativeBiItemUdfFilterConfig>();
    public DbSet<NativeBiDimensionConfig> NativeBiDimensionConfigs => Set<NativeBiDimensionConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
