# ADR-001 — Motor de Base de Datos: Azure SQL → Supabase PostgreSQL

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

DataBision necesita una base de datos intermedia para almacenar los datos extraídos de SAP B1. El diseño original usaba Azure SQL (SQL Server) en una instancia por tenant. Esta decisión fue tomada en el contexto de una arquitectura Enterprise con Power BI Embedded, donde Azure SQL + Azure Data Factory + Power BI era el stack natural de Microsoft.

El cambio estratégico hacia un producto SaaS con planes desde USD 350/mes hace que el costo de Azure SQL sea prohibitivo para los planes iniciales.

---

## Opciones Evaluadas

### Opción A — Azure SQL (diseño original)
- **Costo mínimo:** USD 15-35/mes por tenant (Basic a Standard S1)
- **Multi-tenancy:** una DB por tenant (aislamiento máximo)
- **Ventajas:** integración nativa con Power BI, Azure AD, Azure Data Factory
- **Desventajas:** costo alto para plan USD 350, setup complejo (Azure SQL Server lógico + DB por tenant + Key Vault), T-SQL MERGE (no portátil)

### Opción B — Supabase PostgreSQL (decisión tomada)
- **Costo mínimo:** USD 0 (Free) / USD 25/mes (Pro)
- **Multi-tenancy:** instancia compartida + `company_id` por fila
- **Ventajas:** costo bajo, setup 5 minutos, PostgreSQL estándar (portátil), PgBouncer incluido, backups automáticos
- **Desventajas:** límite conexiones en Free tier (10), sin aislamiento físico entre tenants

### Opción C — Railway / Neon PostgreSQL
- Similar a Supabase pero sin el dashboard y las extras
- No ofrece ventaja significativa sobre Supabase para el MVP

---

## Decisión

**Opción B — Supabase PostgreSQL.**

Supabase Pro (USD 25/mes) es suficiente para 1–15 clientes. El costo por tenant baja de USD 35 (Azure SQL) a ~USD 3 (porción de Supabase Pro). Esto hace viable el plan Starter a USD 350/mes con 77%+ de margen bruto.

---

## Consecuencias

### Cambios en el código

| Archivo | Cambio |
|---|---|
| `Infrastructure.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `StagingDbContext.cs` | `UseSqlServer` → `UseNpgsql` |
| `StagingDatabaseExtensions.cs` | `UseSqlServer` → `UseNpgsql` |
| `AppDbContext.cs` (path producción) | `UseSqlServer` → `UseNpgsql` |
| `SapRawRepository.cs` | 7 T-SQL MERGE → 7 `INSERT ON CONFLICT DO UPDATE` PostgreSQL |
| Migrations | Eliminar y regenerar para PostgreSQL |

### Cambios en el schema

| SQL Server | PostgreSQL |
|---|---|
| `NVARCHAR(n)` | `VARCHAR(n)` |
| `BIGINT IDENTITY(1,1)` | `BIGSERIAL` |
| `BIT` | `BOOLEAN` |
| `DATETIME2` | `TIMESTAMPTZ` |
| `GETUTCDATE()` | `NOW()` |
| `ISNULL(x, y)` | `COALESCE(x, y)` |
| `MERGE ... OUTPUT $action` | `INSERT ON CONFLICT ... RETURNING (xmax = 0)::int` |
| `[schema].[table]` (brackets) | `"schema"."table"` (comillas dobles) |

### Multi-tenancy

El modelo original (una DB por tenant) se reemplaza por una instancia compartida con `company_id` en cada tabla. Ver ADR-003 para el análisis de este cambio.

### Mitigación de riesgos

- Usar conexión directa (puerto 5432) para EF migrations, pooler (puerto 6543) para runtime
- Plan Pro desde el primer cliente en producción (evitar pausa del Free tier)
- Monitorear conexiones activas; alertar si > 10 concurrentes

### Ruta de migración futura

Si un cliente requiere aislamiento contractual (compliance, bancario) o el volumen supera Supabase Pro:
1. `pg_dump` de Supabase → `pg_restore` en Azure Database for PostgreSQL
2. Cambiar connection string en variables de entorno
3. **Sin cambios en código .NET** (Npgsql funciona con cualquier Postgres)

Azure SQL sigue siendo la opción Enterprise documentada en `azure-sql-staging-design.md` para clientes con:
- Contratos Azure Enterprise existentes
- Requerimientos de residencia de datos en Azure
- Integración con Azure Synapse / Fabric

---

## Documentos Afectados

- `databision-product-architecture.md` — SUPERSEDED en sección de DB
- `azure-sql-staging-design.md` — Mantener como referencia Enterprise
- `two-client-production-roadmap.md` — Actualizar referencias de Azure SQL
- `supabase-postgres-mvp-architecture.md` — Es la especificación técnica de esta decisión
