# DataBision — Supabase PostgreSQL MVP Architecture

**Versión:** 1.0  
**Fecha:** 2026-05-28  
**Estado:** Aprobado para planificación — pendiente aprobación de refactor técnico

---

## 1. Decisión Estratégica

**Azure SQL → Supabase PostgreSQL para el MVP comercial.**

| Criterio | Azure SQL | Supabase PostgreSQL |
|---|---|---|
| Costo mínimo | USD 15–35/mes (Basic) | USD 0–25/mes (Free/Pro) |
| Setup time | Varios pasos Azure | 5 minutos en supabase.com |
| Connection string | Directo ODBC/ADO.NET | Supabase pooler o directo |
| PostgreSQL estándar | No (SQL Server) | Sí (Postgres 15) |
| Migración futura | — | Cualquier Postgres managed |
| Auth row-level security | No nativo | Sí (Supabase RLS) |
| API REST auto | No | Sí (PostgREST, opcional) |
| Adecuado para plan USD 300 | No viable con margen | Sí |

**Azure SQL queda documentado como opción Enterprise** en `docs/azure-sql-staging-design.md`.

---

## 2. Arquitectura de Flujo de Datos

```
SAP Business One (HANA / SQL)
         │
         │  Consultas selectivas (UpdateDate >= watermark)
         ▼
DataBision Extractor (Windows Service / .NET Worker)
         │
         │  HTTP POST JSON → X-DataBision-ApiKey
         ▼
DataBision Ingest API (ASP.NET Core 8)
         │  Calcula source_hash, valida, normaliza TSNorm
         │
         ▼
Supabase PostgreSQL
 ┌──────────────────────────────────────────────┐
 │  raw.*          tablas de réplica idempotente │
 │  stg.*          transformaciones limpias      │
 │  dim.*          dimensiones                   │
 │  fact.*         hechos calculados             │
 │  ctl.*          control de extracción         │
 │  audit.*        log de cambios                │
 └──────────────────────────────────────────────┘
         │
         │  DirectQuery / Import (conexión directa)
         ▼
Power BI Desktop / Service
         │  Import Mode: dataset refrescado hasta 8x/día
         ▼
Portal DataBision (React / Vite)
 cliente.databision.com
         │  Iframe con reporte autenticado OR enlace workspace
         ▼
Usuario Final del cliente
```

---

## 3. Por Qué Supabase (y No PostgreSQL Managed Directo)

Supabase no es solo PostgreSQL — añade:

1. **Connection Pooler (PgBouncer integrado):** Crítico para .NET porque cada HTTP request abre/cierra conexiones. PgBouncer reutiliza conexiones sin costo extra.

2. **Dashboard visual:** Permite verificar datos sin herramienta extra durante el desarrollo.

3. **Row Level Security:** Utilizable en fase futura para que cada empresa solo vea sus propios datos directamente.

4. **Backups automáticos diarios:** Incluido en el Plan Pro ($25/mes).

5. **REST API (PostgREST):** Opcional, pero puede usarse en fases futuras sin código extra.

**Connection string para .NET (vía pooler):**
```
Host=db.xxxxxxxxxxxx.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;
```

**Connection string directo (para migraciones EF):**
```
Host=db.xxxxxxxxxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;
```

> EF Core migrations deben ejecutarse via conexión directa (port 5432), no via pooler (6543). El pooler no soporta las transacciones de migración.

---

## 4. Schemas y Tablas (Sin Cambios Respecto al Diseño Actual)

Los schemas se mantienen idénticos. Solo cambia el motor de base de datos.

```sql
-- Schemas (mismos que el diseño SQL Server)
CREATE SCHEMA raw;   -- réplica de SAP, idempotente
CREATE SCHEMA stg;   -- datos limpios, transformados
CREATE SCHEMA dim;   -- dimensiones (clientes, items, vendedores)
CREATE SCHEMA fact;  -- hechos (ventas, créditos)
CREATE SCHEMA ctl;   -- control y checkpoints
CREATE SCHEMA audit; -- log de cambios
```

### Tablas raw MVP (sin cambios en estructura)

| Tabla | Objeto SAP | PK natural |
|---|---|---|
| `raw.sap_oinv` | Facturas | `(company_id, "DocEntry")` |
| `raw.sap_inv1` | Líneas factura | `(company_id, "DocEntry", "LineNum")` |
| `raw.sap_orin` | Notas de crédito | `(company_id, "DocEntry")` |
| `raw.sap_rin1` | Líneas nota crédito | `(company_id, "DocEntry", "LineNum")` |
| `raw.sap_ocrd` | Clientes | `(company_id, "CardCode")` |
| `raw.sap_oitm` | Items | `(company_id, "ItemCode")` |
| `raw.sap_oslp` | Vendedores | `(company_id, "SlpCode")` |

> **Convención de nomenclatura:** Las columnas SAP mantienen su casing original (PascalCase) entre comillas dobles. Las columnas técnicas son snake_case sin comillas. Esto facilita la lectura por parte del equipo SAP.

---

## 5. Cambios en el Código .NET

### 5.1. Paquetes NuGet (Infrastructure.csproj)

**Eliminar:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.4" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
```

**Agregar:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
<PackageReference Include="Npgsql" Version="8.0.3" />
```

> Mantener `Microsoft.EntityFrameworkCore.Sqlite` para AppDbContext en desarrollo local.

### 5.2. StagingDbContext.cs

```csharp
// ANTES
optionsBuilder.UseSqlServer(b => b.MigrationsHistoryTable("__EFMigrationsHistory", "ctl"));

// DESPUÉS
optionsBuilder.UseNpgsql(b => b.MigrationsHistoryTable("__EFMigrationsHistory", "ctl"));
```

### 5.3. StagingDatabaseExtensions.cs

```csharp
// ANTES
services.AddDbContext<StagingDbContext>(o => o.UseSqlServer(connectionString, ...));

// DESPUÉS
services.AddDbContext<StagingDbContext>(o => o.UseNpgsql(connectionString, ...));
```

### 5.4. AppDbContext / Program.cs (portal)

El portal puede continuar usando SQLite en desarrollo y migrar a Supabase/PostgreSQL en producción:

```csharp
// ANTES (producción)
options.UseSqlServer(defaultConnection);

// DESPUÉS (producción)
options.UseNpgsql(defaultConnection);
```

### 5.5. SapRawRepository.cs — Reescritura de MERGE

**Este es el cambio más significativo.** T-SQL MERGE no existe en PostgreSQL. Se reemplaza con `INSERT ... ON CONFLICT ... DO UPDATE`.

**Equivalencia:**

| T-SQL MERGE | PostgreSQL INSERT ON CONFLICT |
|---|---|
| `MERGE INTO tgt USING src ON (pk)` | `INSERT INTO table (...) VALUES (...)` |
| `WHEN NOT MATCHED THEN INSERT` | → parte del INSERT principal |
| `WHEN MATCHED AND condition THEN UPDATE` | `ON CONFLICT (pk) DO UPDATE SET ... WHERE condition` |
| `GETUTCDATE()` | `NOW()` |
| `ISNULL(x, y)` | `COALESCE(x, y)` |
| `OUTPUT $action` | `RETURNING (xmax = 0) AS is_insert` |
| `[schema].[table]` | `"schema"."table"` |
| `SqlConnection` | `NpgsqlConnection` |

**Patrón de INSERT ON CONFLICT para tablas con temporal guard:**

```sql
INSERT INTO "raw"."sap_oinv" (
    company_id, "DocEntry", "DocNum", ..., 
    source_hash_hex, ..., raw_created_at_utc
)
VALUES (
    @company_id, @DocEntry, @DocNum, ...,
    @source_hash_hex, ..., NOW()
)
ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
    "DocNum" = EXCLUDED."DocNum",
    ...,
    source_hash_hex = EXCLUDED.source_hash_hex,
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
```

**Conteo de INSERT vs UPDATE:**
```csharp
// xmax = 0 → INSERT (fila nueva)
// xmax != 0 → UPDATE (fila modificada)
// Sin RETURNING → conflicto pero condición WHERE false → sin cambio
var result = await conn.QueryAsync<int>(new CommandDefinition(sql, p, ct));
var insertCount = result.Count(x => x == 1);  // is_insert = 1
var updateCount = result.Count(x => x == 0);  // is_insert = 0
```

### 5.6. EF Migrations

Las migraciones SQL Server existentes deben **descartarse** y regenerarse:

```bash
# Eliminar migraciones antiguas
rm src/DataBision.Infrastructure/Data/Staging/Migrations/ -rf

# Regenerar (después del cambio de provider)
dotnet ef migrations add InitialStagingSchema \
  --project src/DataBision.Infrastructure \
  --startup-project src/DataBision.Api \
  --context StagingDbContext
```

El DDL de las tablas `raw.*` se mantiene en el `Up()` de la migración como SQL manual (igual que el diseño actual), pero adaptado a tipos PostgreSQL.

**Equivalencia de tipos:**

| SQL Server | PostgreSQL |
|---|---|
| `NVARCHAR(n)` | `VARCHAR(n)` o `TEXT` |
| `NVARCHAR(MAX)` | `TEXT` |
| `BIGINT IDENTITY(1,1)` | `BIGSERIAL` |
| `BIT` | `BOOLEAN` |
| `DATETIME2` | `TIMESTAMPTZ` |
| `DECIMAL(18,4)` | `NUMERIC(18,4)` |
| `CHAR(6)` | `CHAR(6)` |
| `INT` | `INTEGER` |

---

## 6. Multi-Tenant en Supabase

Cada cliente de DataBision comparte la misma instancia de Supabase (MVP). El aislamiento es por `company_id` en cada tabla.

**Consideraciones:**
- **Row Level Security (RLS):** No se activa en el MVP. La API ya filtra por `company_id`. RLS se activa en Fase 2 si se agrega acceso directo a la BD para clientes.
- **Schemas separados por cliente:** No necesario en MVP. Considerarlo en Plan Advanced si hay requerimiento regulatorio.
- **Instancias Supabase separadas:** Solo necesario si el cliente requiere aislamiento total (contrato Enterprise).

---

## 7. Configuración de Variables de Entorno

### appsettings.Development.json (local)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=databision_dev.db",
    "StagingConnection": "Host=db.xxxx.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Pooling=true;"
  },
  "Ingest": {
    "ApiKeys": {
      "dev-key-001": "tenant-dev:company-dev-001"
    }
  }
}
```

### Variables de entorno en producción (Azure App Service)
```
ConnectionStrings__StagingConnection=Host=db.xxxx.supabase.co;Port=6543;...
ConnectionStrings__DefaultConnection=Host=db.xxxx.supabase.co;Port=5432;...
Ingest__ApiKeys__prod-key-001=tenant-clienteA:company-clienteA
```

---

## 8. Migración Futura a Azure SQL o PostgreSQL Managed

Cuando el volumen lo justifique, migrar desde Supabase es directo:

1. `pg_dump` de Supabase → `pg_restore` en Azure Database for PostgreSQL
2. Cambiar connection string en variables de entorno
3. **Sin cambios en el código .NET** (Npgsql funciona igual con cualquier Postgres)

Si se migra a Azure SQL en el futuro (Plan Enterprise):
1. Reemplazar Npgsql por Microsoft.EntityFrameworkCore.SqlServer
2. Reescribir INSERT ON CONFLICT → T-SQL MERGE (código actual ya disponible como referencia)

---

## 9. Archivos a Modificar en el Refactor

| Archivo | Tipo de cambio |
|---|---|
| `src/DataBision.Infrastructure/DataBision.Infrastructure.csproj` | Swap NuGet: SqlServer → Npgsql |
| `src/DataBision.Infrastructure/Data/Staging/StagingDbContext.cs` | `UseSqlServer` → `UseNpgsql` |
| `src/DataBision.Infrastructure/Data/Staging/StagingDatabaseExtensions.cs` | `UseSqlServer` → `UseNpgsql` |
| `src/DataBision.Infrastructure/Data/AppDbContext.cs` | `UseSqlServer` → `UseNpgsql` (prod path) |
| `src/DataBision.Infrastructure/Repositories/Ingest/SapRawRepository.cs` | Reescritura MERGE → INSERT ON CONFLICT (7 métodos) |
| `src/DataBision.Infrastructure/Data/Staging/Migrations/*` | Eliminar y regenerar |
| `src/DataBision.Api/Program.cs` | Cambio menor: IServiceProvider de Npgsql si aplica |
| `src/DataBision.Api/appsettings.Development.template.json` | Actualizar StagingConnection format |

**NO cambian:**
- `DataBision.Domain` — ningún cambio
- `DataBision.Application` — ningún cambio (interfaces y DTOs agnósticos)
- `DataBision.Shared` — ningún cambio (hasher y normalizer son puros)
- `DataBision.Api/Controllers/*` — ningún cambio
- `DataBision.Api/Filters/*` — ningún cambio
- `DataBision.Api/Middleware/*` — ningún cambio
- Todos los tests — ningún cambio (mocks, no dependen del motor)

---

## 10. Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Límite conexiones Supabase Free (pool 10) | Alta en Free | Medio | Usar Plan Pro ($25, pool 15+) desde el día 1 |
| Supabase no disponible en región SA | Baja | Alto | Usar región US-East o EU-West; latencia ~80ms |
| Performance INSERT ON CONFLICT vs MERGE | Media | Bajo | Test con 1.000 rows: ambos < 100ms esperado |
| RLS no activo en MVP | — | Bajo | La API garantiza filtro por company_id |
| Backup / DR | — | Alto | Supabase Pro incluye backups 7 días; exportar pg_dump mensual |
