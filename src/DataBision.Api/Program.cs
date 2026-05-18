using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using DataBision.Application.Interfaces;
using DataBision.Application.Options;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using DataBision.Application.Services;
using DataBision.Infrastructure.Azure;
using DataBision.Infrastructure.Data;
using DataBision.Infrastructure.PowerBI;
using DataBision.Infrastructure.Repositories;
using DataBision.Infrastructure.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBrandingService, BlobStorageService>();
builder.Services.AddScoped<IPowerBIService, PowerBIService>();
builder.Services.Configure<PowerBISettingsOptions>(
    builder.Configuration.GetSection(PowerBISettingsOptions.SectionName));
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<DataBision.Application.Validators.LoginRequestValidator>();

// Disable JWT claim name mapping so "role" stays "role" (not remapped to ClaimTypes.Role URI)
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// JWT Authentication (RS256)
var publicKeyPem = builder.Configuration["Jwt:PublicKey"];
if (!string.IsNullOrEmpty(publicKeyPem))
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(publicKeyPem);

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
              .WithOrigins("https://*.databision.app")
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
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();
