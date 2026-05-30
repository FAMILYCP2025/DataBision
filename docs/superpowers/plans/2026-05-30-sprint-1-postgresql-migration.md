# Sprint 1 — PostgreSQL/Npgsql Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate the Staging/Ingest layer from SQL Server (MERGE, SqlConnection, Microsoft.Data.SqlClient) to Supabase PostgreSQL (INSERT ON CONFLICT, NpgsqlConnection, Npgsql) without breaking the existing 34 unit tests.

**Architecture:** StagingDbContext moves to `UseNpgsql` + snake_case naming conventions for ctl/audit EF-managed tables. SapRawRepository replaces all T-SQL MERGE statements with PostgreSQL `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` idiom. Raw tables (raw.sap_*) are created via manual DDL in the EF migration's `Up()`. AppDbContext (main portal DB) keeps SQLite in dev — the `DatabaseExtensions.cs` production path is updated to UseNpgsql as a **minimum forced change** due to NuGet package removal, not as a functional portal migration.

**Tech Stack:** .NET 8, EF Core 8, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.3, Dapper 2.1.35, Supabase PostgreSQL 15, PgBouncer (port 6543)

---

## Ejecución: Sprints y Checkpoints

```
Sprint 1A  Provider/packages/build/tests         ← checkpoint antes de commit
Sprint 1B  Migración PostgreSQL schema            ← checkpoint antes de commit + confirmar Supabase
Sprint 1C  SapRawRepository PostgreSQL            ← checkpoint antes de commit
Sprint 1D  Validación Supabase/E2E                ← checkpoint antes de commit
```

**Reglas de ejecución:**
- Checkpoint obligatorio después de cada sprint: mostrar `git diff --cached --name-only`, `git status --short`, resultado build/test
- No commitear sin aprobación explícita
- `dotnet ef database update` solo cuando usuario confirme que Supabase dev está creado y connection string configurado
- No hacer push

**Deuda técnica documentada:**
- `RETURNING (xmax = 0)::int` — válido para MVP; en hardening evaluar `OUTPUT` equivalente o batch insert con transacción explícita para mayor atomicidad
- AppDbContext producción usa UseNpgsql por fuerza — migrar AppDbContext migrations a PostgreSQL es Sprint separado (portal DB migration)

---

## Análisis de Impacto

### Archivos a modificar (6)

| Archivo | Cambio |
|---|---|
| `src/DataBision.Infrastructure/DataBision.Infrastructure.csproj` | Swap NuGet packages |
| `src/DataBision.Infrastructure/Data/Staging/StagingDbContext.cs` | `UseSqlServer` → `UseNpgsql` + naming convention |
| `src/DataBision.Infrastructure/Data/Staging/StagingDatabaseExtensions.cs` | `UseSqlServer` → `UseNpgsql` |
| `src/DataBision.Infrastructure/Data/DatabaseExtensions.cs` | `UseSqlServer` → `UseNpgsql` (forzado por NuGet removal) |
| `src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs` | 7 MERGE → INSERT ON CONFLICT, SqlConnection → NpgsqlConnection |
| `src/DataBision.Api/Program.cs` | Actualizar comentario staging DB |

### Archivos a eliminar (3)

```
src/DataBision.Infrastructure/Data/Staging/Migrations/20260518000001_InitialStagingSchema.cs
src/DataBision.Infrastructure/Data/Staging/Migrations/20260518000001_InitialStagingSchema.Designer.cs
src/DataBision.Infrastructure/Data/Staging/Migrations/StagingDbContextModelSnapshot.cs
```

### Archivos a crear (1)

```
src/DataBision.Infrastructure/Data/Staging/Migrations/[timestamp]_InitialStagingSchemaPostgres.cs  ← generado por EF
```

### NO cambian

- `DataBision.Application/**` — interfaces, services, DTOs son agnósticos del motor
- `DataBision.Shared/**` — hash y normalizer son puros
- `DataBision.Domain/**` — entidades sin deps de infraestructura
- `DataBision.Api/Controllers/**` — sin cambios
- `DataBision.Api/Filters/**` — sin cambios
- `tests/**` — todos usan mocks; pasan sin cambios

---

## Mapa de Equivalencias SQL Server → PostgreSQL

| SQL Server | PostgreSQL |
|---|---|
| `MERGE INTO tgt USING src ON (pk) WHEN MATCHED THEN UPDATE WHEN NOT MATCHED THEN INSERT` | `INSERT ... ON CONFLICT (pk) DO UPDATE SET ... WHERE <guard> RETURNING (xmax=0)::int AS is_insert` |
| `ISNULL(x, y)` | `COALESCE(x, y)` |
| `GETUTCDATE()` | `NOW()` |
| `DATETIME2` | `TIMESTAMPTZ` |
| `NVARCHAR(n)` | `VARCHAR(n)` |
| `DECIMAL(18,6)` | `NUMERIC(19,6)` |
| `BIT` | `BOOLEAN` |
| `BIGINT IDENTITY(1,1)` | `BIGSERIAL` |
| `[schema].[table]` | `"schema"."table"` |
| `SqlConnection` | `NpgsqlConnection` |
| `OUTPUT $action` → count inserted/updated | `RETURNING (xmax=0)::int` → count(1) inserted, count(0) updated |
| `@param` (SqlClient) | `@param` (Npgsql — mismo formato) |

### Conteo de resultados con RETURNING

```csharp
// ANTES (SQL Server OUTPUT $action)
var inserted = results.Count(a => a == "INSERT");
var updated  = results.Count(a => a == "UPDATE");

// DESPUÉS (PostgreSQL RETURNING xmax)
// xmax = 0  → fila recién insertada → is_insert = 1
// xmax != 0 → fila actualizada     → is_insert = 0
// sin fila  → hash guard falló     → skipped (no contado aquí)
var inserted = results.Count(x => x == 1);
var updated  = results.Count(x => x == 0);
// skipped se calcula upstream: rows.Count() - inserted - updated
```

---

## Task 1: Swap NuGet Packages

**Files:**
- Modify: `src/DataBision.Infrastructure/DataBision.Infrastructure.csproj`

**Estado esperado al finalizar:** `dotnet build` FALLA — es el comportamiento esperado porque los `using` de SqlClient siguen en el código.

- [ ] **Step 1: Abrir el csproj**

Contenido actual relevante:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.4" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
```

- [ ] **Step 2: Reemplazar los packages**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\DataBision.Application\DataBision.Application.csproj" />
    <ProjectReference Include="..\DataBision.Domain\DataBision.Domain.csproj" />
    <ProjectReference Include="..\DataBision.Shared\DataBision.Shared.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="EFCore.NamingConventions" Version="8.0.3" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Npgsql" Version="8.0.3" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Restaurar packages**

```
dotnet restore src/DataBision.Infrastructure/DataBision.Infrastructure.csproj
```

Esperado: `Restore completed in X.XXs`

- [ ] **Step 4: Verificar que el build falla**

```
dotnet build DataBision.sln
```

Esperado: varios errores sobre `SqlConnection`, `UseSqlServer`, `Microsoft.Data.SqlClient` — esto confirma que los packages fueron removidos. Continuar al Task 2.

---

## Task 2: Actualizar Providers — StagingDbContext, StagingDatabaseExtensions, DatabaseExtensions

**Files:**
- Modify: `src/DataBision.Infrastructure/Data/Staging/StagingDbContext.cs`
- Modify: `src/DataBision.Infrastructure/Data/Staging/StagingDatabaseExtensions.cs`
- Modify: `src/DataBision.Infrastructure/Data/DatabaseExtensions.cs`

**Estado esperado al finalizar:** `dotnet build` PASA. `dotnet test` 34/34.

### 2a — StagingDbContext.cs

- [ ] **Step 1: Reemplazar el archivo completo**

```csharp
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StagingDbContext).Assembly,
            t => t.Namespace?.Contains("Staging") == true);

        modelBuilder.Entity<IngestCheckpoint>()
            .HasIndex(c => new { c.TenantId, c.CompanyId, c.SapObject })
            .IsUnique()
            .HasDatabaseName("UX_ingest_checkpoint_tenant_company_object");

        modelBuilder.Entity<SourceObjectConfig>()
            .HasIndex(c => c.SourceObject)
            .IsUnique()
            .HasDatabaseName("UX_source_object_config_source_object");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(b =>
                b.MigrationsHistoryTable("__EFMigrationsHistory", "ctl"));
    }
}
```

### 2b — StagingDatabaseExtensions.cs

- [ ] **Step 2: Reemplazar el método AddStagingDatabase**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataBision.Infrastructure.Data.Staging;

public static class StagingDatabaseExtensions
{
    /// <summary>
    /// Registers StagingDbContext (Supabase PostgreSQL).
    /// Call only when StagingConnectionString is configured.
    /// Port 6543 (PgBouncer) for runtime; port 5432 for EF migrations.
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
```

### 2c — DatabaseExtensions.cs (forzado por NuGet removal)

- [ ] **Step 3: Leer el archivo actual**

Ejecutar:
```
cat src/DataBision.Infrastructure/Data/DatabaseExtensions.cs
```

El archivo actual usa `UseSqlServer` para la ruta de producción. Reemplazar ese bloque:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataBision.Infrastructure.Data;

namespace DataBision.Infrastructure.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services, string connectionString)
    {
        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            // SQLite — desarrollo local
            services.AddDbContext<AppDbContext>(o =>
                o.UseSqlite(connectionString));
        }
        else
        {
            // PostgreSQL — producción (Supabase)
            services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(connectionString));
        }

        return services;
    }
}
```

**Nota:** Las migraciones de AppDbContext (EF) no se tocan en este sprint. `AppDbContext` SQLite en dev sigue funcionando sin cambios.

- [ ] **Step 4: Verificar build**

```
dotnet build DataBision.sln
```

Esperado: `Build succeeded. 0 Error(s)` — warning pre-existente CS9113 sigue presente, eso es aceptable.

- [ ] **Step 5: Verificar tests**

```
dotnet test DataBision.sln --no-build
```

Esperado: `Passed! Failed: 0, Passed: 34`

- [ ] **Step 6: Commit**

```
git add src/DataBision.Infrastructure/DataBision.Infrastructure.csproj \
        src/DataBision.Infrastructure/Data/Staging/StagingDbContext.cs \
        src/DataBision.Infrastructure/Data/Staging/StagingDatabaseExtensions.cs \
        src/DataBision.Infrastructure/Data/DatabaseExtensions.cs
git commit -m "feat: swap sql server to npgsql provider — staging and app db contexts"
```

---

## Task 3: Eliminar Migraciones SQL Server y Crear Migración PostgreSQL

**Files:**
- Delete: `src/DataBision.Infrastructure/Data/Staging/Migrations/20260518000001_InitialStagingSchema.cs`
- Delete: `src/DataBision.Infrastructure/Data/Staging/Migrations/20260518000001_InitialStagingSchema.Designer.cs`
- Delete: `src/DataBision.Infrastructure/Data/Staging/Migrations/StagingDbContextModelSnapshot.cs`
- Create: nueva migración PostgreSQL (generada + completada manualmente)

**Prerequisito:** Tener la conexión a Supabase configurada localmente en `appsettings.Development.json`.

### Configuración local requerida

Crear (NO commitear) `src/DataBision.Api/appsettings.Development.json` con:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=databision_dev.db",
    "StagingConnection": "Host=db.YOUR_SUPABASE_ID.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;"
  },
  "Ingest": {
    "ApiKeys": {
      "dev-key-001": "tenant-dev:company-dev-001"
    }
  }
}
```

**Nota:** Para `dotnet ef database update` usar port 5432 (directo), no 6543 (PgBouncer). PgBouncer no soporta transacciones de migración.

### 3a — Eliminar migraciones antiguas

- [ ] **Step 1: Eliminar los 3 archivos de migración SQL Server**

```powershell
Remove-Item "src\DataBision.Infrastructure\Data\Staging\Migrations\20260518000001_InitialStagingSchema.cs"
Remove-Item "src\DataBision.Infrastructure\Data\Staging\Migrations\20260518000001_InitialStagingSchema.Designer.cs"
Remove-Item "src\DataBision.Infrastructure\Data\Staging\Migrations\StagingDbContextModelSnapshot.cs"
```

- [ ] **Step 2: Verificar que el directorio Migrations queda vacío**

```
ls src/DataBision.Infrastructure/Data/Staging/Migrations/
```

Esperado: directorio existe pero sin archivos .cs.

### 3b — Generar nueva migración PostgreSQL

- [ ] **Step 3: Generar la migración inicial PostgreSQL**

```
dotnet ef migrations add InitialStagingSchemaPostgres \
  --project src/DataBision.Infrastructure \
  --startup-project src/DataBision.Api \
  --context StagingDbContext
```

Esperado: 3 archivos nuevos creados en `Migrations/`. El `Up()` contiene el DDL generado por EF para las tablas ctl/audit (IngestCheckpoint, ExtractionRun, IngestAuditLog, SourceObjectConfig) con tipos PostgreSQL y snake_case.

- [ ] **Step 4: Abrir el archivo de migración generado y localizar el método Up()**

```
cat "src/DataBision.Infrastructure/Data/Staging/Migrations/*_InitialStagingSchemaPostgres.cs"
```

Verificar que el Up() ya contiene las tablas EF (ctl schema): `ingest_checkpoint`, `extraction_run`, `ingest_audit_log`, `source_object_config`.

### 3c — Agregar DDL de tablas raw al Up()

- [ ] **Step 5: Agregar los CREATE SCHEMA y CREATE TABLE para raw.sap_* al final del Up()**

Localizar el cierre de `migrationBuilder.CreateTable(...)` y agregar ANTES del cierre de `Up()`:

```csharp
// Schemas
migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS raw;");
migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS stg;");
migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS audit;");

// ── raw.sap_oinv — AR Invoice headers ──────────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_oinv" (
        company_id           TEXT          NOT NULL,
        "DocEntry"           INTEGER       NOT NULL,
        "DocNum"             INTEGER,
        "DocDate"            DATE,
        "DocDueDate"         DATE,
        "TaxDate"            DATE,
        "CardCode"           VARCHAR(15),
        "CardName"           VARCHAR(100),
        "DocTotal"           NUMERIC(19,6),
        "DocTotalSy"         NUMERIC(19,6),
        "VatSum"             NUMERIC(19,6),
        "PaidToDate"         NUMERIC(19,6),
        "DocCur"             VARCHAR(3),
        "DocStatus"          VARCHAR(1),
        "SlpCode"            VARCHAR(10),
        "Comments"           TEXT,
        "ObjType"            VARCHAR(20),
        "DocType"            VARCHAR(1),
        "Cancelled"          VARCHAR(1),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "DocEntry")
    );
    CREATE INDEX idx_sap_oinv_company_update ON "raw"."sap_oinv" (company_id, "UpdateDate");
    CREATE INDEX idx_sap_oinv_company_card   ON "raw"."sap_oinv" (company_id, "CardCode");
    """);

// ── raw.sap_inv1 — AR Invoice lines ────────────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_inv1" (
        company_id           TEXT          NOT NULL,
        "DocEntry"           INTEGER       NOT NULL,
        "LineNum"            INTEGER       NOT NULL,
        "ItemCode"           VARCHAR(20),
        "Dscription"         VARCHAR(100),
        "Quantity"           NUMERIC(19,6),
        "Price"              NUMERIC(19,6),
        "Currency"           VARCHAR(3),
        "LineTotal"          NUMERIC(19,6),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "DocEntry", "LineNum")
    );
    """);

// ── raw.sap_orin — AR Credit Memo headers ──────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_orin" (
        company_id           TEXT          NOT NULL,
        "DocEntry"           INTEGER       NOT NULL,
        "DocNum"             INTEGER,
        "DocDate"            DATE,
        "DocDueDate"         DATE,
        "TaxDate"            DATE,
        "CardCode"           VARCHAR(15),
        "CardName"           VARCHAR(100),
        "DocTotal"           NUMERIC(19,6),
        "DocTotalSy"         NUMERIC(19,6),
        "VatSum"             NUMERIC(19,6),
        "DocCur"             VARCHAR(3),
        "DocStatus"          VARCHAR(1),
        "SlpCode"            VARCHAR(10),
        "Comments"           TEXT,
        "ObjType"            VARCHAR(20),
        "DocType"            VARCHAR(1),
        "Cancelled"          VARCHAR(1),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "DocEntry")
    );
    CREATE INDEX idx_sap_orin_company_update ON "raw"."sap_orin" (company_id, "UpdateDate");
    """);

// ── raw.sap_rin1 — AR Credit Memo lines ────────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_rin1" (
        company_id           TEXT          NOT NULL,
        "DocEntry"           INTEGER       NOT NULL,
        "LineNum"            INTEGER       NOT NULL,
        "ItemCode"           VARCHAR(20),
        "Dscription"         VARCHAR(100),
        "Quantity"           NUMERIC(19,6),
        "Price"              NUMERIC(19,6),
        "Currency"           VARCHAR(3),
        "LineTotal"          NUMERIC(19,6),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "DocEntry", "LineNum")
    );
    """);

// ── raw.sap_ocrd — Business Partners (Customers) ───────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_ocrd" (
        company_id           TEXT          NOT NULL,
        "CardCode"           VARCHAR(15)   NOT NULL,
        "CardName"           VARCHAR(100),
        "CardType"           VARCHAR(1),
        "GroupCode"          VARCHAR(10),
        "CntctPrsn"          VARCHAR(90),
        "Phone1"             VARCHAR(20),
        "Phone2"             VARCHAR(20),
        "Currency"           VARCHAR(3),
        "SlpCode"            VARCHAR(10),
        "VatLiable"          VARCHAR(1),
        "LicTradNum"         VARCHAR(30),
        "FrozenFor"          VARCHAR(1),
        "Balance"            NUMERIC(19,6),
        "CreditLine"         NUMERIC(19,6),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "CardCode")
    );
    CREATE INDEX idx_sap_ocrd_company_update ON "raw"."sap_ocrd" (company_id, "UpdateDate");
    """);

// ── raw.sap_oitm — Items (Products) ────────────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_oitm" (
        company_id           TEXT          NOT NULL,
        "ItemCode"           VARCHAR(20)   NOT NULL,
        "ItemName"           VARCHAR(100),
        "ItmsGrpCod"         INTEGER,
        "OnHand"             NUMERIC(19,6),
        "IsCommited"         NUMERIC(19,6),
        "OnOrder"            NUMERIC(19,6),
        "MinLevel"           NUMERIC(19,6),
        "MaxLevel"           NUMERIC(19,6),
        "AvgPrice"           NUMERIC(19,6),
        "LastPurPrc"         NUMERIC(19,6),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "ItemCode")
    );
    CREATE INDEX idx_sap_oitm_company_update ON "raw"."sap_oitm" (company_id, "UpdateDate");
    """);

// ── raw.sap_oslp — Salespersons ─────────────────────────────────────────────
migrationBuilder.Sql("""
    CREATE TABLE IF NOT EXISTS "raw"."sap_oslp" (
        company_id           TEXT          NOT NULL,
        "SlpCode"            INTEGER       NOT NULL,
        "SlpName"            VARCHAR(50),
        "CreateDate"         DATE,
        "CreateTS"           VARCHAR(10),
        "CreateTSNorm"       CHAR(6),
        "UpdateDate"         DATE,
        "UpdateTS"           VARCHAR(10),
        "UpdateTSNorm"       CHAR(6),
        source_hash_hex      CHAR(64)      NOT NULL,
        extraction_run_id    TEXT,
        batch_id             TEXT,
        extracted_at_utc     TIMESTAMPTZ,
        ingestion_mode       VARCHAR(20),
        raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
        raw_updated_at_utc   TIMESTAMPTZ,
        PRIMARY KEY (company_id, "SlpCode")
    );
    """);
```

- [ ] **Step 6: Agregar DROP correspondiente al Down() para las tablas raw**

Al final del método `Down()`, ANTES del cierre:

```csharp
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oslp";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oitm";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_ocrd";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_rin1";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_orin";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_inv1";""");
migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oinv";""");
migrationBuilder.Sql("DROP SCHEMA IF EXISTS audit CASCADE;");
migrationBuilder.Sql("DROP SCHEMA IF EXISTS stg CASCADE;");
migrationBuilder.Sql("DROP SCHEMA IF EXISTS raw CASCADE;");
```

- [ ] **Step 7: Build para verificar que la migración compila**

```
dotnet build DataBision.sln
```

Esperado: `Build succeeded.`

- [ ] **Step 8: Aplicar migración a Supabase dev**

```
dotnet ef database update \
  --project src/DataBision.Infrastructure \
  --startup-project src/DataBision.Api \
  --context StagingDbContext
```

Esperado: `Applying migration 'InitialStagingSchemaPostgres'... Done.`

- [ ] **Step 9: Verificar schemas en Supabase Dashboard**

En el Supabase Dashboard → Table Editor:
- Schema `raw`: tablas sap_oinv, sap_inv1, sap_orin, sap_rin1, sap_ocrd, sap_oitm, sap_oslp
- Schema `ctl`: tablas ingest_checkpoint, extraction_run, ingest_audit_log, source_object_config, __EFMigrationsHistory
- Schema `audit`: tabla ingest_audit_log (si está configurada en ese schema)

- [ ] **Step 10: Commit**

```
git add src/DataBision.Infrastructure/Data/Staging/Migrations/
git commit -m "feat: replace sql server staging migrations with postgresql initial schema"
```

---

## Task 4: Reescribir SapRawRepository para PostgreSQL

**Files:**
- Modify: `src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs`

Esta es la tarea más extensa. El archivo tiene 7 métodos de upsert + 1 de query. Todos deben reescribirse.

**Patrón común INSERT ON CONFLICT:**
```sql
INSERT INTO "raw"."sap_xxx" (col1, col2, ..., source_hash_hex, raw_created_at_utc)
VALUES (@val1, @val2, ..., @source_hash_hex, NOW())
ON CONFLICT (company_id, "PkCol") DO UPDATE SET
    col2 = EXCLUDED.col2,
    ...
    source_hash_hex = EXCLUDED.source_hash_hex,
    raw_updated_at_utc = NOW()
WHERE
    "raw"."sap_xxx".source_hash_hex != EXCLUDED.source_hash_hex
    AND (
        EXCLUDED."UpdateDate" > "raw"."sap_xxx"."UpdateDate"
        OR (
            EXCLUDED."UpdateDate" = "raw"."sap_xxx"."UpdateDate"
            AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                >= COALESCE("raw"."sap_xxx"."UpdateTSNorm", '000000')
        )
    )
RETURNING (xmax = 0)::int AS is_insert;
```

### 4a — Encabezado del archivo

- [ ] **Step 1: Reemplazar los using y constructor**

```csharp
using Dapper;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Ingest;

public sealed class SapRawRepository(string connectionString, ILogger<SapRawRepository> _logger)
    : ISapRawRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    private static (int inserted, int updated) CountResults(IEnumerable<int> results)
    {
        int inserted = results.Count(x => x == 1);
        int updated  = results.Count(x => x == 0);
        return (inserted, updated);
    }
```

### 4b — UpsertSalesInvoicesAsync (OINV)

- [ ] **Step 2: Reescribir el método UpsertSalesInvoicesAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertSalesInvoicesAsync(
        string companyId, IEnumerable<SapOinvRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_oinv" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "PaidToDate",
                "DocCur", "DocStatus", "SlpCode", "Comments", "ObjType", "DocType", "Cancelled",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @PaidToDate,
                @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum"          = EXCLUDED."DocNum",
                "DocDate"         = EXCLUDED."DocDate",
                "DocDueDate"      = EXCLUDED."DocDueDate",
                "TaxDate"         = EXCLUDED."TaxDate",
                "CardCode"        = EXCLUDED."CardCode",
                "CardName"        = EXCLUDED."CardName",
                "DocTotal"        = EXCLUDED."DocTotal",
                "DocTotalSy"      = EXCLUDED."DocTotalSy",
                "VatSum"          = EXCLUDED."VatSum",
                "PaidToDate"      = EXCLUDED."PaidToDate",
                "DocCur"          = EXCLUDED."DocCur",
                "DocStatus"       = EXCLUDED."DocStatus",
                "SlpCode"         = EXCLUDED."SlpCode",
                "Comments"        = EXCLUDED."Comments",
                "ObjType"         = EXCLUDED."ObjType",
                "DocType"         = EXCLUDED."DocType",
                "Cancelled"       = EXCLUDED."Cancelled",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oinv".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oinv"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oinv"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oinv"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("DocEntry",          r.DocEntry);
            p.Add("DocNum",            r.DocNum);
            p.Add("DocDate",           r.DocDate);
            p.Add("DocDueDate",        r.DocDueDate);
            p.Add("TaxDate",           r.TaxDate);
            p.Add("CardCode",          r.CardCode);
            p.Add("CardName",          r.CardName);
            p.Add("DocTotal",          r.DocTotal);
            p.Add("DocTotalSy",        r.DocTotalSy);
            p.Add("VatSum",            r.VatSum);
            p.Add("PaidToDate",        r.PaidToDate);
            p.Add("DocCur",            r.DocCur);
            p.Add("DocStatus",         r.DocStatus);
            p.Add("SlpCode",           r.SlpCode);
            p.Add("Comments",          r.Comments);
            p.Add("ObjType",           r.ObjType);
            p.Add("DocType",           r.DocType);
            p.Add("Cancelled",         r.Cancelled);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4c — UpsertSalesInvoiceLinesAsync (INV1)

- [ ] **Step 3: Reescribir UpsertSalesInvoiceLinesAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertSalesInvoiceLinesAsync(
        string companyId, IEnumerable<SapInv1Row> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_inv1" (
                company_id, "DocEntry", "LineNum",
                "ItemCode", "Dscription", "Quantity", "Price", "Currency", "LineTotal",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @LineNum,
                @ItemCode, @Dscription, @Quantity, @Price, @Currency, @LineTotal,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry", "LineNum") DO UPDATE SET
                "ItemCode"        = EXCLUDED."ItemCode",
                "Dscription"      = EXCLUDED."Dscription",
                "Quantity"        = EXCLUDED."Quantity",
                "Price"           = EXCLUDED."Price",
                "Currency"        = EXCLUDED."Currency",
                "LineTotal"       = EXCLUDED."LineTotal",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_inv1".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_inv1"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_inv1"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_inv1"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("DocEntry",          r.DocEntry);
            p.Add("LineNum",           r.LineNum);
            p.Add("ItemCode",          r.ItemCode);
            p.Add("Dscription",        r.Dscription);
            p.Add("Quantity",          r.Quantity);
            p.Add("Price",             r.Price);
            p.Add("Currency",          r.Currency);
            p.Add("LineTotal",         r.LineTotal);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4d — UpsertCreditMemosAsync (ORIN)

- [ ] **Step 4: Reescribir UpsertCreditMemosAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertCreditMemosAsync(
        string companyId, IEnumerable<SapOrinRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_orin" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum",
                "DocCur", "DocStatus", "SlpCode", "Comments", "ObjType", "DocType", "Cancelled",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum,
                @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum"          = EXCLUDED."DocNum",
                "DocDate"         = EXCLUDED."DocDate",
                "DocDueDate"      = EXCLUDED."DocDueDate",
                "TaxDate"         = EXCLUDED."TaxDate",
                "CardCode"        = EXCLUDED."CardCode",
                "CardName"        = EXCLUDED."CardName",
                "DocTotal"        = EXCLUDED."DocTotal",
                "DocTotalSy"      = EXCLUDED."DocTotalSy",
                "VatSum"          = EXCLUDED."VatSum",
                "DocCur"          = EXCLUDED."DocCur",
                "DocStatus"       = EXCLUDED."DocStatus",
                "SlpCode"         = EXCLUDED."SlpCode",
                "Comments"        = EXCLUDED."Comments",
                "ObjType"         = EXCLUDED."ObjType",
                "DocType"         = EXCLUDED."DocType",
                "Cancelled"       = EXCLUDED."Cancelled",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_orin".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_orin"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_orin"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_orin"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("DocEntry",          r.DocEntry);
            p.Add("DocNum",            r.DocNum);
            p.Add("DocDate",           r.DocDate);
            p.Add("DocDueDate",        r.DocDueDate);
            p.Add("TaxDate",           r.TaxDate);
            p.Add("CardCode",          r.CardCode);
            p.Add("CardName",          r.CardName);
            p.Add("DocTotal",          r.DocTotal);
            p.Add("DocTotalSy",        r.DocTotalSy);
            p.Add("VatSum",            r.VatSum);
            p.Add("DocCur",            r.DocCur);
            p.Add("DocStatus",         r.DocStatus);
            p.Add("SlpCode",           r.SlpCode);
            p.Add("Comments",          r.Comments);
            p.Add("ObjType",           r.ObjType);
            p.Add("DocType",           r.DocType);
            p.Add("Cancelled",         r.Cancelled);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4e — UpsertCreditMemoLinesAsync (RIN1)

- [ ] **Step 5: Reescribir UpsertCreditMemoLinesAsync (idéntico a INV1 pero apunta a sap_rin1)**

```csharp
    public async Task<(int inserted, int updated)> UpsertCreditMemoLinesAsync(
        string companyId, IEnumerable<SapRin1Row> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_rin1" (
                company_id, "DocEntry", "LineNum",
                "ItemCode", "Dscription", "Quantity", "Price", "Currency", "LineTotal",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @LineNum,
                @ItemCode, @Dscription, @Quantity, @Price, @Currency, @LineTotal,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry", "LineNum") DO UPDATE SET
                "ItemCode"        = EXCLUDED."ItemCode",
                "Dscription"      = EXCLUDED."Dscription",
                "Quantity"        = EXCLUDED."Quantity",
                "Price"           = EXCLUDED."Price",
                "Currency"        = EXCLUDED."Currency",
                "LineTotal"       = EXCLUDED."LineTotal",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_rin1".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_rin1"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_rin1"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_rin1"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("DocEntry",          r.DocEntry);
            p.Add("LineNum",           r.LineNum);
            p.Add("ItemCode",          r.ItemCode);
            p.Add("Dscription",        r.Dscription);
            p.Add("Quantity",          r.Quantity);
            p.Add("Price",             r.Price);
            p.Add("Currency",          r.Currency);
            p.Add("LineTotal",         r.LineTotal);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4f — UpsertCustomersAsync (OCRD)

- [ ] **Step 6: Reescribir UpsertCustomersAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertCustomersAsync(
        string companyId, IEnumerable<SapOcrdRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_ocrd" (
                company_id, "CardCode", "CardName", "CardType", "GroupCode",
                "CntctPrsn", "Phone1", "Phone2", "Currency", "SlpCode",
                "VatLiable", "LicTradNum", "FrozenFor", "Balance", "CreditLine",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @CardCode, @CardName, @CardType, @GroupCode,
                @CntctPrsn, @Phone1, @Phone2, @Currency, @SlpCode,
                @VatLiable, @LicTradNum, @FrozenFor, @Balance, @CreditLine,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "CardCode") DO UPDATE SET
                "CardName"        = EXCLUDED."CardName",
                "CardType"        = EXCLUDED."CardType",
                "GroupCode"       = EXCLUDED."GroupCode",
                "CntctPrsn"       = EXCLUDED."CntctPrsn",
                "Phone1"          = EXCLUDED."Phone1",
                "Phone2"          = EXCLUDED."Phone2",
                "Currency"        = EXCLUDED."Currency",
                "SlpCode"         = EXCLUDED."SlpCode",
                "VatLiable"       = EXCLUDED."VatLiable",
                "LicTradNum"      = EXCLUDED."LicTradNum",
                "FrozenFor"       = EXCLUDED."FrozenFor",
                "Balance"         = EXCLUDED."Balance",
                "CreditLine"      = EXCLUDED."CreditLine",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_ocrd".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_ocrd"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_ocrd"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_ocrd"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("CardCode",          r.CardCode);
            p.Add("CardName",          r.CardName);
            p.Add("CardType",          r.CardType);
            p.Add("GroupCode",         r.GroupCode);
            p.Add("CntctPrsn",         r.CntctPrsn);
            p.Add("Phone1",            r.Phone1);
            p.Add("Phone2",            r.Phone2);
            p.Add("Currency",          r.Currency);
            p.Add("SlpCode",           r.SlpCode);
            p.Add("VatLiable",         r.VatLiable);
            p.Add("LicTradNum",        r.LicTradNum);
            p.Add("FrozenFor",         r.FrozenFor);
            p.Add("Balance",           r.Balance);
            p.Add("CreditLine",        r.CreditLine);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4g — UpsertItemsAsync (OITM)

- [ ] **Step 7: Reescribir UpsertItemsAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertItemsAsync(
        string companyId, IEnumerable<SapOitmRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_oitm" (
                company_id, "ItemCode", "ItemName", "ItmsGrpCod",
                "OnHand", "IsCommited", "OnOrder", "MinLevel", "MaxLevel",
                "AvgPrice", "LastPurPrc",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @ItemCode, @ItemName, @ItmsGrpCod,
                @OnHand, @IsCommited, @OnOrder, @MinLevel, @MaxLevel,
                @AvgPrice, @LastPurPrc,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "ItemCode") DO UPDATE SET
                "ItemName"        = EXCLUDED."ItemName",
                "ItmsGrpCod"      = EXCLUDED."ItmsGrpCod",
                "OnHand"          = EXCLUDED."OnHand",
                "IsCommited"      = EXCLUDED."IsCommited",
                "OnOrder"         = EXCLUDED."OnOrder",
                "MinLevel"        = EXCLUDED."MinLevel",
                "MaxLevel"        = EXCLUDED."MaxLevel",
                "AvgPrice"        = EXCLUDED."AvgPrice",
                "LastPurPrc"      = EXCLUDED."LastPurPrc",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oitm".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oitm"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oitm"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oitm"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("ItemCode",          r.ItemCode);
            p.Add("ItemName",          r.ItemName);
            p.Add("ItmsGrpCod",        r.ItmsGrpCod);
            p.Add("OnHand",            r.OnHand);
            p.Add("IsCommited",        r.IsCommited);
            p.Add("OnOrder",           r.OnOrder);
            p.Add("MinLevel",          r.MinLevel);
            p.Add("MaxLevel",          r.MaxLevel);
            p.Add("AvgPrice",          r.AvgPrice);
            p.Add("LastPurPrc",        r.LastPurPrc);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4h — UpsertSalespersonsAsync (OSLP)

- [ ] **Step 8: Reescribir UpsertSalespersonsAsync**

```csharp
    public async Task<(int inserted, int updated)> UpsertSalespersonsAsync(
        string companyId, IEnumerable<SapOslpRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_oslp" (
                company_id, "SlpCode", "SlpName",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @SlpCode, @SlpName,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "SlpCode") DO UPDATE SET
                "SlpName"         = EXCLUDED."SlpName",
                "CreateDate"      = EXCLUDED."CreateDate",
                "CreateTS"        = EXCLUDED."CreateTS",
                "CreateTSNorm"    = EXCLUDED."CreateTSNorm",
                "UpdateDate"      = EXCLUDED."UpdateDate",
                "UpdateTS"        = EXCLUDED."UpdateTS",
                "UpdateTSNorm"    = EXCLUDED."UpdateTSNorm",
                source_hash_hex   = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id          = EXCLUDED.batch_id,
                extracted_at_utc  = EXCLUDED.extracted_at_utc,
                ingestion_mode    = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oslp".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oslp"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oslp"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oslp"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("SlpCode",           r.SlpCode);
            p.Add("SlpName",           r.SlpName);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);

            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }

        return CountResults(allResults);
    }
```

### 4i — GetExistingCreditMemoDocEntriesAsync

- [ ] **Step 9: Reescribir GetExistingCreditMemoDocEntriesAsync**

```csharp
    public async Task<IReadOnlyList<int>> GetExistingCreditMemoDocEntriesAsync(
        string companyId, IEnumerable<int> docEntries, CancellationToken ct)
    {
        var entries = docEntries.ToList();
        if (entries.Count == 0) return [];

        const string sql = """
            SELECT "DocEntry"
            FROM "raw"."sap_orin"
            WHERE company_id = @company_id
              AND "DocEntry" = ANY(@doc_entries);
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);

        var result = await conn.QueryAsync<int>(sql, new
        {
            company_id  = companyId,
            doc_entries = entries.ToArray()
        });

        return result.ToList().AsReadOnly();
    }

} // end class
```

**Nota:** PostgreSQL usa `= ANY(@array)` en lugar del `IN (@list)` dinámico de SQL Server. Npgsql mapea `int[]` a un array PostgreSQL automáticamente con `= ANY`.

- [ ] **Step 10: Verificar build**

```
dotnet build DataBision.sln
```

Esperado: `Build succeeded. 0 Error(s)`

- [ ] **Step 11: Verificar tests**

```
dotnet test DataBision.sln --no-build
```

Esperado: `Passed! Failed: 0, Passed: 34` — los tests usan mocks, no BD real.

- [ ] **Step 12: Commit**

```
git add src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs
git commit -m "feat: rewrite SapRawRepository with postgresql insert on conflict upserts"
```

---

## Task 5: Tests de Integración y Validación Runtime

**Files:**
- Create: `tests/DataBision.Infrastructure.Tests/Ingest/SapRawRepositoryIntegrationTests.cs`

**Prerequisito:** Supabase dev configurado y migración aplicada (Task 3 Step 8).

**Nota:** Estos tests requieren `TEST_STAGING_CONNECTION` en el entorno. Se marcan con `[Trait("Category", "Integration")]` para excluirlos del CI por defecto.

### 5a — Proyecto de tests de integración

- [ ] **Step 1: Crear proyecto de tests de integración**

```
dotnet new xunit -n DataBision.Infrastructure.Tests \
  --output tests/DataBision.Infrastructure.Tests \
  --framework net8.0
```

Agregar al `DataBision.sln`:
```
dotnet sln add tests/DataBision.Infrastructure.Tests/DataBision.Infrastructure.Tests.csproj
```

Agregar NuGet al nuevo `.csproj`:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\DataBision.Infrastructure\DataBision.Infrastructure.csproj" />
  <ProjectReference Include="..\..\src\DataBision.Application\DataBision.Application.csproj" />
  <ProjectReference Include="..\..\src\DataBision.Shared\DataBision.Shared.csproj" />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
  <PackageReference Include="xunit" Version="2.7.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
</ItemGroup>
```

- [ ] **Step 2: Crear el archivo de tests**

```csharp
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Infrastructure.Repositories.Ingest;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataBision.Infrastructure.Tests.Ingest;

[Trait("Category", "Integration")]
public sealed class SapRawRepositoryIntegrationTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_STAGING_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set TEST_STAGING_CONNECTION env var to run integration tests.");

    private static SapRawRepository NewRepo() =>
        new(ConnectionString, NullLogger<SapRawRepository>.Instance);

    private static SapOinvRow SampleOinvRow(int docEntry = 9001) => new()
    {
        DocEntry      = docEntry,
        DocNum        = 5000 + docEntry,
        DocDate       = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        CardCode      = "C0001",
        CardName      = "Cliente Test SA",
        DocTotal      = 1_190_000m,
        DocTotalSy    = 1_190_000m,
        DocStatus     = "O",
        DocCur        = "CLP",
        UpdateDate    = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        UpdateTS      = "143052",
        UpdateTSNorm  = "143052",
        IngestionMode = "FULL",
        ExtractionRunId = Guid.NewGuid().ToString(),
        BatchId       = Guid.NewGuid().ToString(),
        ExtractedAtUtc = DateTime.UtcNow,
        SourceHashHex = new string('a', 64),
    };

    // Use a unique company_id per test run to avoid cross-test contamination
    private static string TestCompanyId => $"test-{Guid.NewGuid():N}";

    [Fact]
    public async Task UpsertOinv_NewRow_ReturnsInserted1Updated0()
    {
        var repo = NewRepo();
        var companyId = TestCompanyId;
        var row = SampleOinvRow();

        var (inserted, updated) = await repo.UpsertSalesInvoicesAsync(companyId, [row]);

        inserted.Should().Be(1);
        updated.Should().Be(0);
    }

    [Fact]
    public async Task UpsertOinv_SameRowTwice_SecondIsSkipped()
    {
        var repo = NewRepo();
        var companyId = TestCompanyId;
        var row = SampleOinvRow();

        await repo.UpsertSalesInvoicesAsync(companyId, [row]);
        var (inserted, updated) = await repo.UpsertSalesInvoicesAsync(companyId, [row]);

        inserted.Should().Be(0);
        updated.Should().Be(0);
    }

    [Fact]
    public async Task UpsertOinv_ModifiedHash_ReturnsUpdated1()
    {
        var repo = NewRepo();
        var companyId = TestCompanyId;
        var row = SampleOinvRow();

        await repo.UpsertSalesInvoicesAsync(companyId, [row]);

        // Cambiar hash y UpdateDate para que el guard lo acepte
        row.DocTotal = 2_000_000m;
        row.SourceHashHex = new string('b', 64);
        row.UpdateDate = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        var (inserted, updated) = await repo.UpsertSalesInvoicesAsync(companyId, [row]);

        inserted.Should().Be(0);
        updated.Should().Be(1);
    }

    [Fact]
    public async Task UpsertOinv_OlderUpdateDate_IsSkippedEvenIfHashDiffers()
    {
        var repo = NewRepo();
        var companyId = TestCompanyId;
        var row = SampleOinvRow();
        row.UpdateDate = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        await repo.UpsertSalesInvoicesAsync(companyId, [row]);

        // Intentar con fecha anterior — el guard temporal rechaza
        var staleRow = SampleOinvRow();
        staleRow.SourceHashHex = new string('c', 64);
        staleRow.UpdateDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

        var (inserted, updated) = await repo.UpsertSalesInvoicesAsync(companyId, [staleRow]);

        inserted.Should().Be(0);
        updated.Should().Be(0);
    }

    [Fact]
    public async Task GetExistingCreditMemoDocEntries_ReturnsOnlyExisting()
    {
        var repo = NewRepo();
        var companyId = TestCompanyId;

        // Insertar ORIN con DocEntry 8001
        var orinRow = new SapOrinRow
        {
            DocEntry = 8001, DocNum = 1001,
            DocDate = DateTime.UtcNow, CardCode = "C0001",
            DocTotal = 100_000m, DocCur = "CLP",
            UpdateDate = DateTime.UtcNow, UpdateTS = "100000", UpdateTSNorm = "100000",
            IngestionMode = "FULL", ExtractionRunId = Guid.NewGuid().ToString(),
            BatchId = Guid.NewGuid().ToString(), ExtractedAtUtc = DateTime.UtcNow,
            SourceHashHex = new string('d', 64),
        };

        await repo.UpsertCreditMemosAsync(companyId, [orinRow]);

        var existing = await repo.GetExistingCreditMemoDocEntriesAsync(companyId, [8001, 9999]);

        existing.Should().ContainSingle().Which.Should().Be(8001);
    }
}
```

- [ ] **Step 3: Ejecutar tests de integración**

```
$env:TEST_STAGING_CONNECTION = "Host=db.YOUR_ID.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;"

dotnet test tests/DataBision.Infrastructure.Tests \
  --filter "Category=Integration" \
  -v normal
```

Esperado: 5 tests, 5 passed.

- [ ] **Step 4: Verificar que los tests unitarios existentes siguen verdes**

```
dotnet test DataBision.sln --filter "Category!=Integration"
```

Esperado: 34/34 passed.

- [ ] **Step 5: Prueba E2E con la API**

Levantar la API con la conexión Supabase configurada:
```
dotnet run --project src/DataBision.Api
```

POST al endpoint de ingest:
```bash
curl -X POST http://localhost:5000/api/ingest/sap-b1/sales-invoices \
  -H "X-DataBision-ApiKey: dev-key-001" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant-dev",
    "companyId": "company-dev-001",
    "sapObject": "OINV",
    "extractionRunId": "run-001",
    "batchId": "batch-001",
    "ingestionMode": "INCREMENTAL",
    "rows": [{
      "DocEntry": 1001, "DocNum": 5001,
      "DocDate": "2026-01-15", "CardCode": "C0001", "CardName": "Test SA",
      "DocTotal": 1190000, "DocTotalSy": 1190000, "DocStatus": "O", "DocCur": "CLP",
      "UpdateDate": "2026-01-15", "UpdateTS": "143052",
      "ExtractionRunId": "run-001", "BatchId": "batch-001",
      "ExtractedAtUtc": "2026-01-15T14:30:52Z", "IngestionMode": "INCREMENTAL"
    }]
  }'
```

Primer POST esperado:
```json
{ "data": { "rowsInserted": 1, "rowsUpdated": 0, "rowsSkipped": 0 } }
```

Segundo POST (idéntico) esperado:
```json
{ "data": { "rowsInserted": 0, "rowsUpdated": 0, "rowsSkipped": 1 } }
```

- [ ] **Step 6: Verificar checkpoint en Supabase**

En Supabase Dashboard → SQL Editor:
```sql
SELECT * FROM ctl.ingest_checkpoint WHERE company_id = 'company-dev-001';
```

Esperado: una fila con `sap_object = 'OINV'`, `watermark_date` y `total_rows_ingested = 1`.

- [ ] **Step 7: Verificar que no hay secretos en el código**

```bash
grep -rn "Password=" src/ --include="*.cs" | grep -v "PLACEHOLDER\|YOUR_PASSWORD"
```

Esperado: sin resultados.

- [ ] **Step 8: Commit final Sprint 1**

```
git add tests/DataBision.Infrastructure.Tests/ DataBision.sln
git commit -m "test: add postgresql integration tests for SapRawRepository"

git tag sprint-1-complete
```

---

## Riesgos y Mitigaciones

| ID | Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|---|
| R1 | Entity configurations usan nombres SQL Server explícitos que rompen con snake_case convention | Media | Medio | Revisar cada `IEntityTypeConfiguration` en `Staging/Entities/` — si tienen `HasColumnName("PascalCase")`, ajustar o quitar la convención |
| R2 | `DateTime` sin `DateTimeKind.Utc` rechazado por Npgsql | Alta | Alto | Npgsql 8 requiere `DateTimeKind.Utc` para `TIMESTAMPTZ`. Agregar `.EnableLegacyTimestampBehavior()` en `UseNpgsql(...)` o asegurar que todos los `DateTime` tengan `Kind = Utc` |
| R3 | `NUMERIC(19,6)` vs `decimal` — precisión silenciosa | Baja | Bajo | PostgreSQL `NUMERIC` mapea a `decimal` en C# — sin cambios requeridos |
| R4 | Dapper `DynamicParameters` con arrays en Npgsql | Media | Alto | `GetExistingCreditMemoDocEntries` usa `= ANY(@array)`. Npgsql requiere que el array sea `int[]`, no `List<int>` — llamar `.ToArray()` antes de pasar |
| R5 | PgBouncer bloquea transacciones EF migrations | Alta | Alto | Usar port 5432 directo para `dotnet ef database update`, port 6543 solo en runtime |
| R6 | `ON CONFLICT` falla si el índice UNIQUE no coincide exactamente con la clave en el INSERT | Media | Alto | El PRIMARY KEY del DDL es la conflict target — verificar que el constraint existe en Supabase antes de ejecutar `database update` |
| R7 | `EFCore.NamingConventions` convierte índices a snake_case y choca con nombres hardcodeados en `OnModelCreating` | Baja | Medio | Los nombres de índices hardcodeados (`HasDatabaseName(...)`) no son afectados por la convención — solo los nombres de columnas y tablas autogenerados |
| R8 | Supabase connection limit (15 en Pro) agotado durante dev | Baja | Medio | Usar pooling en el connection string de runtime; EF usará una sola conexión por request |

---

## Criterios de Aceptación Sprint 1

- [ ] `dotnet build DataBision.sln` → **0 errores, 0 referencias a SqlServer/SqlClient**
- [ ] `dotnet test DataBision.sln --filter "Category!=Integration"` → **34/34 passed**
- [ ] `dotnet test --filter "Category=Integration"` → **5/5 passed** (requiere Supabase dev)
- [ ] `dotnet ef database update` aplica migración limpia en Supabase fresh
- [ ] POST `/api/ingest/sap-b1/sales-invoices` retorna `{ rowsInserted: 1 }` en primer ingest
- [ ] Segundo POST idéntico retorna `{ rowsInserted: 0, rowsUpdated: 0, rowsSkipped: 1 }`
- [ ] `ctl.ingest_checkpoint` tiene registro actualizado tras el ingest
- [ ] Cero referencias a `Microsoft.Data.SqlClient`, `UseSqlServer`, `SqlConnection` en `src/`
- [ ] Cero secretos en código o archivos commiteados

---

## Recomendación: Antes de Empezar

1. **Configurar Supabase dev** — crear instancia Pro en supabase.com, copiar connection string al `appsettings.Development.json` local (no commitear).

2. **Revisar entity configurations** — leer cada `IEntityTypeConfiguration` en `src/DataBision.Infrastructure/Data/Staging/Entities/` y verificar que no haya `HasColumnName()` explícitos que choquen con la convención snake_case. Si hay conflicto, será visible en el build tras Task 2.

3. **Verificar `DateTime` en DTOs** — los `ExtractedAtUtc` y fechas de watermark deben usar `DateTimeKind.Utc`. Si Npgsql rechaza fechas en runtime, agregar `.EnableLegacyTimestampBehavior()` al `UseNpgsql()` como workaround temporal.

---

*Plan v1 — 2026-05-30 — DataBision Sprint 1*  
*Arquitectura base: `docs/master-architecture.md` v4.0*
