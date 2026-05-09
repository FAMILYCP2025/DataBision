using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataBision.Infrastructure.Data;

public static class DatabaseExtensions
{
    /// <summary>
    /// Registers AppDbContext with SQLite (when connection string starts with "Data Source=")
    /// or SQL Server otherwise. This allows SQLite in dev without changing production config.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        if (IsSqlite(connectionString))
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        else
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

        return services;
    }

    public static bool IsSqlite(string connectionString)
        => connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
        || connectionString.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase);
}
