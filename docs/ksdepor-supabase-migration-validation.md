# KSDEPOR — Validación de Migraciones Supabase DEV/TST

**Sprint:** 8E  
**Fecha:** 2026-06-10  
**Entorno:** Supabase DEV/TST (appsettings.Development.json — API project)

---

## Comando ejecutado

```powershell
dotnet ef database update `
  --context StagingDbContext `
  --project src/DataBision.Infrastructure `
  --startup-project src/DataBision.Api
```

- **ASPNETCORE_ENVIRONMENT:** Development (default dotnet ef)  
- **DB target:** Supabase DEV/TST (StagingConnection de appsettings.Development.json de API)  
- **NO aplica a producción**

---

## Resultado

```
Done.
```

**Migraciones aplicadas en este run:**
| Migration | Estado anterior | Estado final |
|-----------|----------------|-------------|
| 20260610183821_AddCfgSchema | Pending | Applied ✅ |
| 20260610202144_AddMartProcessSchemas | Pending | Applied ✅ |
| 20260610202613_AddOpsSchema | Pending | Applied ✅ |

**Historial completo en `ctl.__EFMigrationsHistory`:**
| Migration | Estado |
|-----------|--------|
| 20260530221734_InitialStagingSchemaPostgres | Applied (anterior) |
| 20260607020740_AddStgSchema | Applied (anterior) |
| 20260607030000_AddMartSchema | Applied (anterior) |
| 20260610183821_AddCfgSchema | Applied ✅ 2026-06-10 |
| 20260610202144_AddMartProcessSchemas | Applied ✅ 2026-06-10 |
| 20260610202613_AddOpsSchema | Applied ✅ 2026-06-10 |

---

## Schemas creados

Los schemas fueron creados por las migraciones. Queries de confirmación manuales:

```sql
select schema_name
from information_schema.schemata
where schema_name in ('ctl','raw','stg','mart','cfg','ops')
order by schema_name;
-- Esperado: cfg, ctl, mart, ops, raw, stg  (6 rows)
```

---

## Tablas y conteos esperados

### cfg (AddCfgSchema)

```sql
select count(*) from cfg.process;              -- esperado: 4 (SALES, PURCHASING, INVENTORY, FINANCE)
select count(*) from cfg.dashboard;            -- esperado: 12+ (3 por proceso)
select count(*) from cfg.kpi;                  -- esperado: 20+
select count(*) from cfg.sap_object_catalog;   -- esperado: 7+ (OINV,ORIN,INV1,RIN1,OCRD,OITM,OSLP)
select count(*) from cfg.company_process_enabled; -- esperado: 0 (no tenant seeded aún)
```

### mart (AddMartProcessSchemas — nuevas tablas)

```sql
select count(*) from mart.sales_customer_dashboard;     -- 0 (requiere refresh)
select count(*) from mart.sales_item_dashboard;         -- 0 (requiere refresh)
select count(*) from mart.sales_fulfillment_dashboard;  -- 0 (requiere ORDR/ODLN)
select count(*) from mart.purchase_executive_daily;     -- 0 (requiere OPOR)
select count(*) from mart.purchase_supplier_dashboard;  -- 0 (requiere OPOR)
select count(*) from mart.purchase_receiving_dashboard; -- 0 (requiere OPDN)
select count(*) from mart.inventory_stock_dashboard;    -- 0 (requiere OITW)
select count(*) from mart.inventory_rotation_dashboard; -- 0 (requiere refresh)
select count(*) from mart.inventory_warehouse_dashboard;-- 0 (requiere OWTR)
select count(*) from mart.finance_ar_aging_dashboard;   -- 0 (requiere refresh con OINV)
select count(*) from mart.finance_ap_aging_dashboard;   -- 0 (requiere OPCH)
select count(*) from mart.finance_executive_daily;      -- 0 (requiere refresh)
```

### ops (AddOpsSchema)

```sql
select count(*) from ops.alert_rule;    -- esperado: 8
select count(*) from ops.extractor_run; -- 0 (no hay runs aún)
select count(*) from ops.pipeline_health; -- 0 (no hay empresa aún)

select rule_code, severity from ops.alert_rule order by id;
-- EXTRACTOR_NOT_RUN_RECENTLY | ERROR
-- MART_EMPTY                 | WARNING
-- STG_EMPTY                  | WARNING
-- SALES_DROP_DAILY           | WARNING
-- STOCKOUT_ITEMS             | WARNING
-- AR_OVERDUE_HIGH            | WARNING
-- DATA_QUALITY_ERRORS        | WARNING
-- TRANSFORM_FAILED           | ERROR
```

### Tablas cfg/mart/ops completas

```sql
select table_schema, table_name
from information_schema.tables
where table_schema in ('cfg','mart','ops')
order by table_schema, table_name;
```

---

## Validación desde extractor (--validate-staging)

Se implementó el comando `--validate-staging` en el Extractor:

```bash
dotnet run --project src/DataBision.Extractor -- --validate-staging
```

**Requisito:** `Staging:ConnectionString` debe estar configurado en `appsettings.Development.json` del extractor.

El template está en `src/DataBision.Extractor/appsettings.Development.template.json`:
```json
"Staging": {
  "ConnectionString": "Host=HOST;Port=5432;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require;"
}
```

Agregar esta sección al `appsettings.Development.json` del extractor para poder ejecutar `--validate-staging` y verificar conteos directamente.

---

## Riesgos y rollback

**Rollback lógico** (si algo falla post-migración):

```sql
-- Revertir AddOpsSchema:
DROP FUNCTION IF EXISTS ops.log_data_quality_issue(TEXT,TEXT,TEXT,TEXT,TEXT,INTEGER,TEXT);
DROP FUNCTION IF EXISTS ops.evaluate_alert_rules(TEXT);
DROP FUNCTION IF EXISTS ops.refresh_pipeline_health(TEXT);
DROP TABLE IF EXISTS ops.pipeline_health;
DROP TABLE IF EXISTS ops.alert_event;
DROP TABLE IF EXISTS ops.alert_rule;
DROP TABLE IF EXISTS ops.data_quality_issue;
DROP TABLE IF EXISTS ops.transform_run;
DROP TABLE IF EXISTS ops.extractor_page_log;
DROP TABLE IF EXISTS ops.extractor_run;
DROP SCHEMA IF EXISTS ops;
-- Luego eliminar registro de ctl.__EFMigrationsHistory
DELETE FROM ctl."__EFMigrationsHistory" WHERE migration_id = '20260610202613_AddOpsSchema';

-- Revertir AddMartProcessSchemas: similar con tablas mart.* nuevas
-- Revertir AddCfgSchema: DROP SCHEMA IF EXISTS cfg CASCADE;
```

**Riesgos identificados:**
- `mart.refresh_all_processes` llama a `mart.refresh_finance_process` que consulta `mart.finance_executive_daily` — si la tabla no existe, la función es defensiva y continúa.
- `ops.evaluate_alert_rules` consulta `mart.finance_executive_daily` defensivamente (`IF EXISTS`).
- Las tablas MART nuevas son vacías hasta ejecutar el primer refresh. Esto es esperado.

---

## Confirmación final

- Conexión a SAP HANA: NO usada en este sprint.
- Tablas modificadas en SAP: NINGUNA.
- Solo Supabase PostgreSQL DEV/TST fue modificado.
- Producción: SIN TOCAR.
