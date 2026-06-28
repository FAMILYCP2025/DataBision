# Runbook — Migrations de Supabase Staging (StagingDbContext)

**DataBision · Junio 2026**
**Versión:** 1.0
**Aplica a:** DataBision.Infrastructure → StagingDbContext (PostgreSQL/Supabase)

---

## 1. Por qué las migrations de Supabase son manuales

Supabase expone dos endpoints de conexión:

| Endpoint | Puerto | Uso |
|---|---|---|
| Transaction pooler (PgBouncer) | 6543 | Operación normal — alta disponibilidad |
| Direct connection | 5432 | Migrations y operaciones DDL |

EF Core aplica migrations dentro de una **transaction explícita**. PgBouncer en modo transaction pooling **no mantiene estado de transacción entre comandos** — el primer `BEGIN` de EF Core abre una transacción que el pooler no puede trackear hasta su `COMMIT`, lo que causa el error:

```
Npgsql.NpgsqlException: ERROR: prepared statement "..." does not exist
```

Por esta razón, las migrations de `StagingDbContext` están **excluidas del startup automático de la API** (ver comentario en `Program.cs`) y deben ejecutarse manualmente, **apuntando al port 5432 directamente**.

El `AppDbContext` (SQLite, en el servidor) sí corre migrations al startup porque no pasa por PgBouncer.

---

## 2. Cuándo ejecutar este runbook

| Situación | Acción |
|---|---|
| Primer deploy de un nuevo cliente | Ejecutar migrations completas (todas) |
| Upgrade de versión con nuevas migrations | Ejecutar solo las nuevas (`dotnet ef migrations list` para identificarlas) |
| Rollback de una migration fallida | Ejecutar rollback a la migration anterior |
| Verificar estado actual de migrations | `dotnet ef migrations list --context StagingDbContext` |

---

## 3. Prerequisitos

1. **.NET 8 SDK** instalado en la máquina que ejecuta el comando:
   ```powershell
   dotnet --version  # debe retornar 8.x.x
   ```

2. **Código fuente** de DataBision accesible (el comando usa los archivos compilados):
   ```
   src/DataBision.Infrastructure/
   src/DataBision.Api/
   ```

3. **Connection string de Supabase con port 5432** (direct connection, no PgBouncer):
   ```
   Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<password>
   ```
   > El port 6543 (PgBouncer) fallará. Usar 5432.

4. Variable de entorno disponible antes de ejecutar:
   ```powershell
   $env:ConnectionStrings__StagingConnection = "Host=...;Port=5432;..."
   ```

---

## 4. Comando para aplicar migrations

### Windows (PowerShell)

```powershell
$env:ConnectionStrings__StagingConnection = "Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require"

dotnet ef database update `
    --project src/DataBision.Infrastructure `
    --startup-project src/DataBision.Api `
    --context StagingDbContext
```

### Linux / macOS (bash)

```bash
export ConnectionStrings__StagingConnection="Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<password>;SSL Mode=Require"

dotnet ef database update \
    --project src/DataBision.Infrastructure \
    --startup-project src/DataBision.Api \
    --context StagingDbContext
```

### Salida esperada (éxito)

```
Build started...
Build succeeded.
Applying migration '20260530221734_InitialStagingSchemaPostgres'.
...
Applying migration '20260620000001_Sprint20StaleDataDeleteInsertFix'.
Done.
```

---

## 5. Verificación post-migration

Una vez aplicadas las migrations, verificar desde el extractor instalado:

```bash
dotnet DataBision.Extractor.dll --validate-staging
```

**Salida esperada:**

```
[STG-01] Schema raw: OK (tablas: raw.oact, raw.ojdt, raw.jdt1, ...)
[STG-02] Schema stg: OK (tablas: stg.accounts, stg.journal_entries, ...)
[STG-03] Schema mart: OK (tablas: mart.accounting_summary, ...)
[STG-04] Schema cfg: OK (tablas: cfg.account_classification_rules)
[STG-05] Schema ops: OK (tablas: ops.extractor_run, ops.transform_run)
=== --validate-staging: DONE ===
```

Si algún schema aparece como `MISSING` o `ERROR`, la migration correspondiente no se aplicó correctamente. Ver sección 6 (Rollback).

---

## 6. Rollback

### Listar migrations disponibles

```powershell
dotnet ef migrations list `
    --project src/DataBision.Infrastructure `
    --startup-project src/DataBision.Api `
    --context StagingDbContext
```

### Rollback a una migration específica

```powershell
$env:ConnectionStrings__StagingConnection = "Host=...;Port=5432;..."

dotnet ef database update 20260617130000_AddAccountingMartFunctions `
    --project src/DataBision.Infrastructure `
    --startup-project src/DataBision.Api `
    --context StagingDbContext
```

Reemplazar `20260617130000_AddAccountingMartFunctions` con el nombre de la migration a la que se quiere volver.

> **Advertencia:** El rollback ejecuta el método `Down()` de las migrations posteriores. Verificar que el método `Down()` de la migration fallida no deja datos inconsistentes antes de ejecutar.

---

## 7. Migrations existentes

Las migrations están en `src/DataBision.Infrastructure/Data/Staging/Migrations/` y se aplican en este orden:

| # | Timestamp | Nombre | Descripción |
|---|---|---|---|
| 1 | 20260530221734 | InitialStagingSchemaPostgres | Schema inicial: schema `raw` con tablas base OACT/OJDT/JDT1 |
| 2 | 20260607020740 | AddStgSchema | Schema `stg`: tablas de staging transformadas (accounts, journal_entries) |
| 3 | 20260607030000 | AddMartSchema | Schema `mart`: tablas analíticas agregadas (accounting_summary) |
| 4 | 20260610183821 | AddCfgSchema | Schema `cfg`: reglas de clasificación de cuentas |
| 5 | 20260610202144 | AddMartProcessSchemas | Schema `mart`: tablas de procesos (sales, inventory, purchasing, operations) |
| 6 | 20260610202613 | AddOpsSchema | Schema `ops`: tablas de telemetría (extractor_run, transform_run) |
| 7 | 20260615182500 | FixMartProcessFunctions | Corrección de funciones SQL en schema `mart` (procesos) |
| 8 | 20260615182725 | FixMartProcessColumnNames | Renombre de columnas en tablas de procesos para consistencia |
| 9 | 20260615210000 | AddPurchaseFulfillmentSchemas | Tablas de fulfillment y cumplimiento de compras |
| 10 | 20260615210100 | FixStgRefreshAll | Corrección de función `stg.refresh_all` |
| 11 | 20260615210200 | FixMartProcessFunctions | Segunda corrección de funciones mart (procesos) |
| 12 | 20260615210300 | FixInventoryStockGroupBy | Corrección de agrupación en inventario (GROUP BY) |
| 13 | 20260616010000 | DeduplicateOpsAlerts | Deduplicación de registros en `ops` — índices únicos |
| 14 | 20260617120000 | AddAccountingSchema | Schema contable completo: cuentas, movimientos, clasificación PCGE |
| 15 | 20260617130000 | AddAccountingMartFunctions | Funciones MART para refresh contable (refresh_accounting_all) |
| 16 | 20260620000000 | Sprint19AccountingMartPcgeFixes | Correcciones de clasificación PCGE en MART contable |
| 17 | 20260620000001 | Sprint20StaleDataDeleteInsertFix | Fix de DELETE+INSERT en refresh contable para datos stale |

**Migration más reciente:** `20260620000001_Sprint20StaleDataDeleteInsertFix`

---

## Troubleshooting

### Error: "prepared statement does not exist"
Estás usando el port 6543 (PgBouncer). Cambiar a port 5432.

### Error: "SSL connection is required"
Agregar `SSL Mode=Require` a la connection string.

### Error: "permission denied for schema raw"
El usuario PostgreSQL no tiene permisos sobre los schemas. Ejecutar como el usuario `postgres` de Supabase, o otorgar permisos:
```sql
GRANT ALL ON SCHEMA raw TO <usuario>;
GRANT ALL ON SCHEMA stg TO <usuario>;
GRANT ALL ON SCHEMA mart TO <usuario>;
GRANT ALL ON SCHEMA cfg TO <usuario>;
GRANT ALL ON SCHEMA ops TO <usuario>;
```

### Error: migration ya aplicada
Si EF Core reporta que la migration ya está aplicada pero `--validate-staging` falla, las tablas de la migration pudieron haberse creado parcialmente. Revisar la tabla `__EFMigrationsHistory` en Supabase y eliminar la entrada correspondiente si necesitas re-aplicar.
