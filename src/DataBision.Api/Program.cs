using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using DataBision.Api.Filters;
using DataBision.Application.Interfaces;
using DataBision.Application.Interfaces.Dashboard;
using DataBision.Application.Interfaces.Ingest;
using DataBision.Application.Options;
using DataBision.Application.Services.Dashboard;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using DataBision.Application.Services;
using DataBision.Infrastructure.Azure;
using DataBision.Infrastructure.Data;
using DataBision.Infrastructure.Data.Staging;
using DataBision.Infrastructure.PowerBI;
using DataBision.Infrastructure.Repositories;
using DataBision.Infrastructure.Repositories.Dashboard;
using DataBision.Infrastructure.Repositories.Ingest;
using DataBision.Infrastructure.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction() && string.IsNullOrEmpty(builder.Configuration["Jwt:PublicKey"]))
    throw new InvalidOperationException(
        "Jwt:PublicKey is required in Production. Set the RSA public key PEM in configuration.");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Database — auto-detects SQLite ("Data Source=...") vs SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required. Check appsettings.Development.json.");
builder.Services.AddDatabase(connectionString);

// Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// ApiKeyAuthFilter: needed for ingest endpoints AND internal extractor endpoints (does not require staging)
builder.Services.AddScoped<ApiKeyAuthFilter>();

// Staging database — optional, guarded by connection string presence (PostgreSQL/Supabase in Sprint 1)
var stagingConnectionString = builder.Configuration.GetConnectionString("StagingConnection");
if (!string.IsNullOrWhiteSpace(stagingConnectionString))
{
    builder.Services.AddStagingDatabase(stagingConnectionString);
    builder.Services.AddScoped<ISapRawRepository>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<SapRawRepository>>();
        return new SapRawRepository(stagingConnectionString, logger);
    });
    builder.Services.AddScoped<IIngestCheckpointRepository>(_ =>
        new IngestCheckpointRepository(stagingConnectionString));
    builder.Services.AddScoped<IIngestService, IngestService>();

    // Native BI — Dashboard, Sales, Sync endpoints (read from mart.* / stg.* / ctl.*)
    builder.Services.AddScoped<IDashboardRepository>(_ =>
        new DashboardRepository(stagingConnectionString));
    builder.Services.AddScoped<ISyncStatusRepository>(_ =>
        new SyncStatusRepository(stagingConnectionString));
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<ISalesService, SalesService>();
    builder.Services.AddScoped<ISyncStatusService, SyncStatusService>();
    builder.Services.AddScoped<IDiagnosticsRepository>(_ =>
        new DiagnosticsRepository(stagingConnectionString));
    builder.Services.AddScoped<IDiagnosticsService, DiagnosticsService>();

    // Process catalog + process dashboards (cfg.* + mart new tables + ops.*)
    builder.Services.AddScoped<IProcessRepository>(_ =>
        new ProcessRepository(stagingConnectionString));
    builder.Services.AddScoped<IProcessDashboardRepository>(_ =>
        new ProcessDashboardRepository(stagingConnectionString));
    builder.Services.AddScoped<IProcessService, ProcessService>();
    builder.Services.AddScoped<IProcessDashboardService, ProcessDashboardService>();

    // Filter options (distinct values from MART for filter dropdowns)
    builder.Services.AddScoped<IFilterOptionsRepository>(_ =>
        new FilterOptionsRepository(stagingConnectionString));
    builder.Services.AddScoped<IFilterOptionsService, FilterOptionsService>();

    // Sprint 3 — Sales MART (mart.sales_period_kpi, top_customers, top_items, top_salespersons, open_sales_orders)
    builder.Services.AddScoped<ISalesMartRepository>(_ =>
        new SalesMartRepository(stagingConnectionString));

    // Sprint 4 — Purchases MART (mart.purchase_period_kpi, top_suppliers, top_purchase_items, open_purchase_orders)
    builder.Services.AddScoped<IPurchaseMartRepository>(_ =>
        new PurchaseMartRepository(stagingConnectionString));

    // Sprint 5 — Inventory MART (mart.inventory_snapshot, inventory_movement_kpi, slow_moving_items, warehouse_stock)
    builder.Services.AddScoped<IInventoryMartRepository>(_ =>
        new InventoryMartRepository(stagingConnectionString));

    // Sprint 6 — Finance MART (mart.finance_summary, ar_aging, ap_aging, finance_period_kpi)
    builder.Services.AddScoped<IFinanceMartRepository>(_ =>
        new FinanceMartRepository(stagingConnectionString));

    // Sprint 10 — MART Admin Refresh (triggers all 4 mart.refresh_* functions)
    builder.Services.AddScoped<IMartAdminRefreshService>(_ =>
        new MartAdminRefreshService(stagingConnectionString));

    // Account classification rules (cfg.account_classification_rules in staging DB)
    builder.Services.AddScoped<IAccountClassificationService>(_ =>
        new AccountClassificationRepository(stagingConnectionString));
}

// Analytics company ID resolver (maps app slug → staging company_id for Native BI queries)
builder.Services.AddScoped<IAnalyticsCompanyResolver, AnalyticsCompanyResolver>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBrandingService, BlobStorageService>();

// Power BI is an optional future add-on, not part of the Native BI MVP.
builder.Services.Configure<PowerBISettingsOptions>(
    builder.Configuration.GetSection(PowerBISettingsOptions.SectionName));
if (builder.Configuration.GetValue<bool>("PowerBI:Enabled"))
{
    builder.Services.AddScoped<IPowerBIService, PowerBIService>();
}

builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INativeBiAdminConfigService, NativeBiAdminConfigService>();
builder.Services.AddSingleton<ISecretRefResolver, DefaultSecretRefResolver>();
builder.Services.AddScoped<INativeBiConnectionProfileService, NativeBiConnectionProfileService>();
builder.Services.AddScoped<INativeBiSapConnectionTester, NativeBiSapConnectionTester>();

// Seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DataBision.Application.Validators.LoginRequestValidator>();

// Disable JWT claim name mapping so "role" stays "role" (not remapped to ClaimTypes.Role URI)
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// Resolves a PEM key from direct content, escaped-newline content, or a file path.
// Never logs resolved key material.
static string ResolvePemKey(string pemOrPath, string configKey)
{
    var candidate = pemOrPath.Replace("\\n", "\n");
    if (candidate.Contains("-----BEGIN", StringComparison.Ordinal))
        return candidate;
    if (File.Exists(candidate))
        return File.ReadAllText(candidate);
    throw new InvalidOperationException(
        $"Configuration key '{configKey}' must contain PEM content (starting with '-----BEGIN...') " +
        "or a valid file path to a PEM file.");
}

// JWT Authentication (RS256)
var publicKeyPem = builder.Configuration["Jwt:PublicKey"];
if (!string.IsNullOrEmpty(publicKeyPem))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(ResolvePemKey(publicKeyPem, "Jwt:PublicKey"));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "databision-api",
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                RoleClaimType = "role",
                NameClaimType = JwtRegisteredClaimNames.Sub
            };
        });
}
else
{
    Log.Warning("Jwt:PublicKey not configured — JWT auth disabled. Set Jwt:PublicKey to enable.");
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(opts => opts.AddPolicy("DataBision", policy =>
{
    if (builder.Environment.IsDevelopment())
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
              .WithOrigins("https://*.databision.com", "https://*.databision.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
}));

// Rate limiting: 5 login attempts per 15 min per IP
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(15);
        o.QueueLimit = 0;
    });
    opts.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 120;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Apply migrations + seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();

    // StagingDbContext (Supabase PostgreSQL) migrations are intentionally NOT run at startup.
    // PgBouncer transaction pooler (port 6543) does not support migration transactions.
    // Run migrations manually: dotnet ef database update --context StagingDbContext
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors("DataBision");
app.UseRateLimiter();
app.UseMiddleware<DataBision.Api.Middleware.TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", async (IConfiguration cfg) =>
{
    var stagingStatus = "not_configured";
    var stagingCs = cfg.GetConnectionString("StagingConnection");

    if (!string.IsNullOrWhiteSpace(stagingCs))
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(stagingCs);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await conn.OpenAsync(cts.Token);
            stagingStatus = "ok";
        }
        catch (Exception ex)
        {
            stagingStatus = $"error:{ex.GetType().Name}";
        }
    }

    var healthy = stagingStatus is "ok" or "not_configured";
    var payload = new
    {
        status    = healthy ? "ok" : "degraded",
        timestamp = DateTime.UtcNow,
        staging   = stagingStatus
    };

    return healthy ? Results.Ok(payload) : Results.Json(payload, statusCode: 503);
});

app.Run();
