# KSDEPOR — Plan de Producción

**Sprint:** 8D  
**Fecha:** 2026-06-10  
**Estado:** Pre-producción (ambiente de pruebas activo, producción pendiente)

---

## Estado actual

| Capa | Estado | Notas |
|------|--------|-------|
| Extractor SAP | ✅ Funcional | 7 objetos activos (OINV, ORIN, INV1, RIN1, OCRD, OITM, OSLP) |
| Paginación multi-page | ✅ Implementado | ServiceLayerPaginator con retry + MaxPages |
| STG PostgreSQL | ✅ Funcional | stg.* poblado por Dapper |
| MART Sales | ✅ Funcional | mart.sales_* + mart.sales_kpi_summary |
| MART Finance (AR) | ✅ Funcional (aprox.) | ar aging con balance_due = doc_total |
| MART Finance (AP) | ⏳ Vacío | Requiere OPCH (Sprint 8F) |
| MART Purchasing | ⏳ Vacío | Requiere OPOR/OPDN (Sprint 8F) |
| MART Inventory | ⏳ Parcial | inventory_rotation funcional; stock requiere OITW |
| ops schema | ✅ Migración lista | ops.* tablas + funciones + 8 alert rules |
| cfg schema | ✅ Presente | cfg.sap_object_catalog y cfg.company_config |
| Power BI | 🔄 En preparación | Dashboards conectados a MART |
| Autenticación | ✅ Funcional | JWT RS256 + refresh token |

---

## Checklist de producción

### Infraestructura
- [ ] Confirmar connection string Supabase producción en Azure Key Vault / appsettings.Production.json
- [ ] Confirmar que SAP Service Layer de producción es accesible desde el servidor Extractor
- [ ] Confirmar `App__BaseDomain = databision.app` en variables de entorno
- [ ] DNS `ksdepor.databision.app` apuntando al servidor correcto
- [ ] Certificado SSL wildcard `*.databision.app` activo

### Base de datos
- [ ] Ejecutar `dotnet ef database update --context StagingDbContext` contra Supabase producción
  - Incluye: AddCfgSchema, AddMartProcessSchemas, AddOpsSchema
- [ ] Verificar que los schemas `ctl`, `cfg`, `stg`, `mart`, `ops` existen en producción
- [ ] Insertar tenant KSDEPOR en `ctl.companies` y `cfg.company_config`
- [ ] Verificar semillas en `ops.alert_rule` (8 reglas)

### Extractor
- [ ] Configurar `appsettings.Production.json` con PageSize=100, MaxPages=500
- [ ] Primer run manual con `--run-once` y objeto OSLP (volumen mínimo)
- [ ] Verificar logs: páginas, filas extraídas, sin errores 400
- [ ] Primer run OCRD, OITM
- [ ] Primer run OINV con watermark vacío (full inicial — puede tomar varios minutos)
- [ ] Verificar `ops.extractor_run` registra runs correctamente
- [ ] Configurar modo service (scheduler activado, IntervalMinutes según acuerdo KSDEPOR)

### Transformaciones MART
- [ ] Ejecutar `mart.refresh_all_processes('ksdepor')` post primera extracción
- [ ] Verificar filas en `mart.sales_kpi_summary`, `mart.finance_ar_aging_dashboard`
- [ ] Ejecutar `ops.evaluate_alert_rules('ksdepor')` y revisar alertas iniciales
- [ ] Verificar `ops.pipeline_health` con health_score > 60

### Power BI
- [ ] Confirmar workspace ID en `PowerBI__WorkspaceId`
- [ ] Confirmar Service Principal tiene permisos en el workspace
- [ ] Verificar embed token con RLS `username = 'ksdepor'`, `role = 'CompanyRole'`
- [ ] Probar dashboard Sales con datos reales KSDEPOR
- [ ] Probar dashboard Finance (AR) con datos reales

### Portal
- [ ] Login en `ksdepor.databision.app` con usuario KSDEPOR
- [ ] Verificar que branding/tema aplica correctamente (ThemeProvider)
- [ ] Navegar a cada reporte Power BI y confirmar carga sin errores
- [ ] Verificar que un usuario de KSDEPOR no puede ver datos de otro tenant (tenant isolation)

---

## Objetos SAP pendientes (Sprint 8F)

| Objeto | Proceso | Bloqueante para |
|--------|---------|----------------|
| OPOR / POR1 | Compras | mart.purchase_executive_daily |
| OPDN / PDN1 | Compras | mart.purchase_receiving_dashboard |
| OPCH / PCH1 | Compras/Finanzas | mart.finance_ap_aging_dashboard |
| OITW | Inventario | mart.inventory_stock_dashboard |
| OWHS | Inventario | mart.inventory_warehouse_dashboard |
| OWTR / WTR1 | Inventario | mart.inventory_warehouse_dashboard |
| ORDR / RDR1 | Ventas | mart.sales_fulfillment_dashboard |
| ODLN / DLN1 | Ventas | mart.sales_fulfillment_dashboard |

**Pasos para activar cada objeto preparado:**
1. Crear extractor job (patrón OinvExtractorJob)
2. Registrar en `Program.cs` (service + CLI)
3. Crear migración STG si tabla no existe
4. Actualizar `cfg.sap_object_catalog` → `is_active = TRUE`
5. Ejecutar `dotnet ef database update` y validar
6. Run manual controlado con PageSize=100, MaxPages=10 (prueba)

---

## Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|---------|-----------|
| SAP SL timeout en full initial load OINV | Media | Medio | MaxPages=500 limita el run; extracción incremental posterior |
| balance_due ≠ saldo real (AR sin pagos) | Alta | Bajo | Documentado; se refina con OPCH/JDT1 en Sprint 8F |
| hit_max_pages TRUE en producción | Baja | Medio | Aumentar MaxPages en appsettings si OINV > 50k filas |
| RLS Power BI no filtra por tenant | Baja | Alto | Verificar token embed incluye RLS identity en pruebas |
| Supabase conexión inestable | Baja | Medio | Retry en Dapper; monitorear ops.extractor_run |
| Cambios API SAP SL entre ambientes | Baja | Medio | Probar con OSLP (5 campos) antes de OINV (30+ campos) |

---

## Criterios de Go-Live

1. **Build 0 errores** en rama main
2. **73/73 tests pasando**
3. **ops.pipeline_health.health_score >= 70** para KSDEPOR post primer run
4. **Ninguna alerta EXTRACTOR_NOT_RUN_RECENTLY activa** tras 24h de servicio
5. **Dashboard Sales visible** en `ksdepor.databision.app` con datos reales
6. **Tenant isolation verificado**: usuario KSDEPOR no accede a datos de otro tenant

---

## Sprints siguientes

| Sprint | Objetivo |
|--------|---------|
| 8E | Integración ops → Extractor C# (log runs, page logs, DQ checks en código) |
| 8F | Activar OPOR/OPDN/OPCH/OITW y sus MART proceso Compras+Inventario |
| 8G | Dashboard Power BI Compras e Inventario |
| 9+ | Datos de costo (margen bruto), módulo de pagos (AR real), multi-empresa |
