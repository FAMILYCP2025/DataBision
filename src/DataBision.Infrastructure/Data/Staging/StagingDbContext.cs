using DataBision.Infrastructure.Data.Staging.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataBision.Infrastructure.Data.Staging;

/// <summary>
/// Separate EF context for the staging database (Supabase PostgreSQL).
/// Used only for ctl/audit EF-managed tables. Raw table upserts use Dapper.
/// </summary>
public sealed class StagingDbContext(DbContextOptions<StagingDbContext> options) : DbContext(options)
{
    public DbSet<SourceObjectConfig> SourceObjectConfigs => Set<SourceObjectConfig>();
    public DbSet<ExtractionRun> ExtractionRuns => Set<ExtractionRun>();
    public DbSet<IngestCheckpoint> IngestCheckpoints => Set<IngestCheckpoint>();
    public DbSet<IngestAuditLog> IngestAuditLogs => Set<IngestAuditLog>();
    public DbSet<CompanyProcessEnabled> CompanyProcessesEnabled => Set<CompanyProcessEnabled>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StagingDbContext).Assembly,
            t => t.Namespace?.Contains("Staging") == true);

        // Unique index on checkpoint (tenant+company+object)
        modelBuilder.Entity<IngestCheckpoint>()
            .HasIndex(c => new { c.TenantId, c.CompanyId, c.SapObject })
            .IsUnique()
            .HasDatabaseName("UX_ingest_checkpoint_tenant_company_object");

        // Unique index on source_object_config (one row per SAP object)
        modelBuilder.Entity<SourceObjectConfig>()
            .HasIndex(c => c.SourceObject)
            .IsUnique()
            .HasDatabaseName("UX_source_object_config_object");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Guard: only configure when DI has not already configured (design-time / dotnet ef tool path).
        // At runtime, AddStagingDatabase() in StagingDatabaseExtensions provides the full config.
        if (!optionsBuilder.IsConfigured)
            optionsBuilder
                .UseNpgsql(b => b.MigrationsHistoryTable("__EFMigrationsHistory", "ctl"))
                .UseSnakeCaseNamingConvention();
    }
}
