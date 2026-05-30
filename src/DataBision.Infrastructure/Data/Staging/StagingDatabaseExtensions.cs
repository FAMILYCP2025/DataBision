using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataBision.Infrastructure.Data.Staging;

public static class StagingDatabaseExtensions
{
    /// <summary>
    /// Registers StagingDbContext (SQL Server only — no SQLite fallback).
    /// Call only when a StagingConnectionString is configured.
    /// </summary>
    public static IServiceCollection AddStagingDatabase(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StagingDbContext>(o =>
            o.UseSqlServer(connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "ctl")));

        return services;
    }
}
