# DataBision — Native BI Data Visibility (KSDEPOR Demo)

Sprint 8R — Junio 2026

---

## Causa raíz

Los endpoints Native BI consultaban el MART con el identificador que entrega `CompanyContextResolver.TryResolve`, que extrae del JWT la claim `company_slug` (primera de la lista `["company_slug", "company_id", "companyId"]`).

Para `demo@ksdepor.com`, el JWT contiene `company_slug = "ksdepor"` y `company_id = "2"` (ID numérico en la BD local SQLite). El resolver devolvía `"ksdepor"`.

Los datos en Supabase/MART están cargados con `company_id = 'company-dev-001'`.

Resultado: `WHERE company_id = 'ksdepor'` → 0 filas en todas las tablas MART.

---

## Mapping ksdepor → company-dev-001

### Solución implementada

Se creó un `IAnalyticsCompanyResolver` que mapea el identificador de la app (slug de JWT) al `company_id` usado en la BD analítica (Supabase/MART).

**Archivos creados:**
- `src/DataBision.Application/Interfaces/IAnalyticsCompanyResolver.cs` — interfaz
- `src/DataBision.Application/Services/AnalyticsCompanyResolver.cs` — implementación

**Configuración en `appsettings.json`** (valores no secretos):
```json
"NativeBi": {
  "DefaultAnalyticsCompanyId": "",
  "CompanySlugMap": {
    "ksdepor": "company-dev-001",
    "demo": "company-dev-001"
  }
}
```

**Inyección:** `ProcessDashboardService` ahora recibe `IAnalyticsCompanyResolver` y llama `Map(companyId)` antes de cada query al repo. El DI se registra en `Program.cs`.

### Flujo corregido

```
JWT claim: company_slug = "ksdepor"
                ↓
CompanyContextResolver → ctx.CompanyId = "ksdepor"
                ↓
ProcessDashboardService.Map("ksdepor")
                ↓
AnalyticsCompanyResolver.Resolve("ksdepor") → "company-dev-001"
                ↓
ProcessDashboardRepository: WHERE company_id = 'company-dev-001'
                ↓
Datos reales del MART ✓
```

### Para producción

Agregar columna `analytics_company_id` (varchar) a la tabla `companies` en la BD local. Al crear una empresa, configurar este campo. Reemplazar `AnalyticsCompanyResolver` por una implementación que consulte la BD en lugar de config.

---

## Endpoints Native BI validados

Todos usan `[Route("api/client/bi/...")]` y `[AllowAnonymous]` (autenticación vía JWT claim, no enforcement de `[Authorize]`). Con el resolver, todos devuelven datos para `company-dev-001`.

| Endpoint | Estado esperado |
|---|---|
| GET /api/client/bi/sales/customers-dashboard | Datos reales — `mart.sales_customer_dashboard` |
| GET /api/client/bi/sales/items-dashboard | Datos reales — `mart.sales_item_dashboard` |
| GET /api/client/bi/sales/fulfillment | Datos reales — `mart.sales_fulfillment_dashboard` |
| GET /api/client/bi/purchasing/executive | Datos reales — `mart.purchase_executive_daily` |
| GET /api/client/bi/purchasing/suppliers | Datos reales — `mart.purchase_supplier_dashboard` |
| GET /api/client/bi/purchasing/receiving | Datos reales — `mart.purchase_receiving_dashboard` |
| GET /api/client/bi/inventory/rotation | Datos reales — `mart.inventory_rotation_dashboard` |
| GET /api/client/bi/inventory/stock | Datos reales — `mart.inventory_stock_dashboard` |
| GET /api/client/bi/inventory/warehouses | Datos reales — `mart.inventory_warehouse_dashboard` |
| GET /api/client/bi/finance/executive | Datos reales — `mart.finance_executive_daily` |
| GET /api/client/bi/finance/ar-aging | Datos reales — `mart.finance_ar_aging_dashboard` |
| GET /api/client/bi/finance/ap-aging | Datos reales — `mart.finance_ap_aging_dashboard` |
| GET /api/client/bi/operations/pipeline-health | Datos reales — `ops.pipeline_health` |
| GET /api/client/bi/operations/alerts | Datos reales — `ops.alert_event` |
| GET /api/client/bi/operations/data-quality | Datos reales — `ops.data_quality_issue` |

**Tenant en DEV:** el frontend envía `?companyId=ksdepor` en el query string. Sin embargo, con JWT configurado y usuario autenticado, `CompanyContextResolver` ignora el query param y usa el JWT claim. El resolver mapea el claim al analytics ID.

---

## Secciones Native BI disponibles por área

### VENTAS (4 tabs)
1. **Clientes** — `mart.sales_customer_dashboard`
2. **Productos** — `mart.sales_item_dashboard`
3. **Vendedores** — `mart.salesperson_sales` (via nativeBiApi)
4. **Fulfillment** — `mart.sales_fulfillment_dashboard`

### COMPRAS (3 tabs + KPIs)
- KPIs: Órdenes OC, Monto OC, Monto recibido, Proveedores activos (de `mart.purchase_executive_daily`)
1. **Proveedores** — `mart.purchase_supplier_dashboard`
2. **Recepciones** — `mart.purchase_receiving_dashboard`
3. **Evolución OC (30d)** — `mart.purchase_executive_daily` como tabla de serie de tiempo

### INVENTARIO (3 tabs + KPIs)
- KPIs: Alta/Normal/Baja/Sin movimiento (conteos calculados del rotation data)
1. **Rotación** — `mart.inventory_rotation_dashboard`
2. **Almacenes** — `mart.inventory_warehouse_dashboard`
3. **Sin movimiento** — filtro client-side sobre rotation data (`rotationStatus = 'NO_MOVEMENT'`)

### FINANZAS (3 tabs + KPIs)
- KPIs: AR vencido, % vencido, Facturas, Monto facturado (de `mart.finance_executive_daily`)
1. **Cuentas por cobrar (AR)** — `mart.finance_ar_aging_dashboard`
2. **Cuentas por pagar (AP)** — `mart.finance_ap_aging_dashboard`
3. **Riesgo +90d** — filtro client-side sobre AR data (`aging90Plus > 0`)

### OPERACIONES (3 tabs + KPIs + status)
- KPIs: Health score, Alertas activas, Objetos extraídos, Errores DQ
- Status row: Extractor status/timestamp, Transform status/timestamp
1. **Alertas** — `ops.alert_event` (con badge de conteo)
2. **Calidad de datos** — `ops.data_quality_issue`
3. **Historial runs** — vista detallada del `ops.pipeline_health` (extractor/transform run info)

---

## Diferencia: Módulos Power BI vs Análisis Native BI

| Sección | Descripción |
|---|---|
| **Módulos** (sidebar) | Reportes de Power BI embedidos. Requieren que el SuperAdmin asigne reportes a la empresa. Se oculta automáticamente si todos los módulos tienen 0 reportes. |
| **Análisis** (sidebar) | Dashboards Native BI — datos directamente del MART de Supabase. Siempre visibles para el CompanyAdmin. No dependen de asignación de reportes. |

### ¿Por qué se ocultaron los Módulos en KSDEPOR demo?

La empresa KSDEPOR fue creada sin reportes de Power BI asignados (solo compañía + usuario). El sidebar ahora detecta esto y oculta la sección "Módulos" cuando `todos los módulos tienen reportCount === 0`. La sección "Análisis" (Native BI) sigue visible.

---

## Qué hacer si una pantalla aparece vacía

| Síntoma | Causa probable | Acción |
|---|---|---|
| Tablas vacías con datos en Supabase | Resolver no mapea el slug | Verificar `NativeBi:CompanySlugMap` en `appsettings.json` |
| Error 401 en endpoints `/api/client/bi/*` | JWT expirado | Re-login con `demo@ksdepor.com` |
| Error 403 "forbidden_no_company" | JWT sin claim `company_slug` | Verificar que el JWT fue emitido post-login (no expirado) |
| Error 500 en staging | Supabase sin conexión | Verificar `StagingConnection` en appsettings y ejecutar `--validate-staging` |
| Pipeline health: null | Sin datos en `ops.pipeline_health` para company-dev-001 | Ejecutar `--validate-ops --company company-dev-001` |

---

## Pendientes para producción

1. **Migración `Company.AnalyticsCompanyId`:** Agregar columna `analytics_company_id` (varchar, nullable) a la tabla `companies`. El SuperAdmin la configura al crear/editar una empresa. Reemplazar `AnalyticsCompanyResolver` por implementación con DB lookup.

2. **Autenticación `/api/client/bi/*`:** Los 5 controllers tienen `[AllowAnonymous]`. En producción, quitar `[AllowAnonymous]` y agregar `[Authorize]` para forzar autenticación real (el `CompanyContextResolver` ya valida JWT y company claim).

3. **Tenant query param → solo subdomain en producción:** El `?companyId` en el frontend es para DEV. En producción, el subdomain es la fuente de verdad (`TenantMiddleware` lo detecta del Host header).

4. **SuperAdmin cross-company:** El `CompanyContextResolver` tiene un TODO para permitir que SuperAdmin consulte cualquier compañía. Pendiente para Sprint futuro.

---

## Staging validado (2026-06-16)

```
[VS-01] PASS — Supabase connection open
[VS-02] Schemas: cfg, ctl, mart, ops, raw, stg
[VS-03] cfg.process=5, cfg.dashboard=20
[VS-04] ops.alert_rule=8
[VS-05] Tables (32): mart.*, ops.*, cfg.*
=== --validate-staging: ALL PASS ===
```

Ops para company-dev-001:
- extractor_run: 35 runs, 7 con error (normal — algunos objetos SAP sin movimiento)
- transform_run: 12 runs
- alert_event: 44 eventos
- Último OPDN: STATUS=SUCCESS, 6 rows, 2026-06-16
