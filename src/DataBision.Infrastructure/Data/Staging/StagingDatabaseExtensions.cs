using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataBision.Infrastructure.Data.Staging;

public static class StagingDatabaseExtensions
{
    /// <summary>
    /// Registers StagingDbContext targeting Supabase PostgreSQL.
    /// Call only when StagingConnectionString is configured.
    /// Port 6543 (PgBouncer) for runtime; port 5432 (direct) for dotnet ef migrations.
    /// </summary>
    public static IServiceCollection AddStagingDatabase(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StagingDbContext>(o =>
            o.UseNpgsql(connectionString,
                pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "ctl"))
             .UseSnakeCaseNamingConvention());

        return services;
    }
}
