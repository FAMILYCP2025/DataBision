# Native BI — Limitaciones de Demo y Backlog Técnico

Documento de referencia para el equipo de ventas, preventa e ingeniería.
Describe qué funciona hoy, qué requiere configuración adicional, y qué es backlog productivo.

---

## Estado actual de la demo (2026-06)

### ¿Qué está funcionando?

| Módulo | Estado | Datos |
|---|---|---|
| Dashboard resumen ejecutivo | ✅ Funciona | Datos reales KSDEPOR |
| Ventas — KPIs + rankings | ✅ Funciona | Datos reales |
| Ventas — Fulfillment de pedidos | ✅ Funciona | Datos reales |
| Compras — OC + proveedores | ✅ Funciona | Datos reales |
| Inventario — Rotación de SKUs | ✅ Funciona | Datos reales |
| Finanzas — AR Aging | ✅ Funciona | Datos reales |
| Operaciones — Pipeline + alertas | ✅ Funciona | Datos reales OPS |
| Diagnósticos — Checks + tablas | ✅ Funciona | Datos reales |

---

## Limitaciones conocidas

### 1. Cuentas por pagar (AP) puede estar vacía

**Causa**: La extracción de OPCH (facturas de proveedor) puede no tener datos suficientes en el ambiente de demo.

**Impacto**: La pestaña "Cuentas por pagar" muestra empty state profesional.

**Solución**: Completar la carga de OPCH con el extractor SAP. Una vez procesado, el módulo AP se activa automáticamente.

### 2. Almacenes por almacén puede estar vacío

**Causa**: La extracción de OWTR (traspasos entre almacenes) no siempre tiene datos en ambientes de demo.

**Impacto**: La pestaña "Almacenes" en Inventario muestra empty state.

**Solución**: Activar objeto OWTR en el extractor. Requiere datos de traspasos en SAP.

### 3. KPIs de participación % son client-side y paginados

**Causa**: Los rankings de clientes, productos y proveedores se cargan con paginación (20 por página). La participación % se calcula sobre los datos de la página actual, no sobre el universo completo.

**Impacto**: El % mostrado es relativo a la página cargada, no al 100% de clientes. El top 5 de concentración usa el endpoint de resumen (`sales_overview`) que sí tiene el total completo.

**Solución productiva**: Agregar un endpoint de totales globales separado o agregar metadata de totales en la respuesta paginada.

### 4. Cobertura de inventario requiere `avg_daily_sales_qty > 0`

**Causa**: Si un artículo no tuvo ventas en el período analizado, `avg_daily_sales_qty = 0` y la cobertura no se puede calcular.

**Impacto**: `coverage_days = null` para artículos sin movimiento. Se muestra `—` en la tabla.

**Resolución**: Esperada — artículos sin movimiento no tienen cobertura calculable.

### 5. Aging de AR es snapshot de la página actual

**Causa**: Los aging buckets (0-30d, 31-60d, etc.) en el tab Resumen de Finanzas se calculan sobre la primera página de datos AR (20 clientes por defecto), no sobre el universo completo.

**Impacto**: Los totales de aging pueden ser menores al total real si hay más de 20 clientes.

**Solución productiva**: Agregar endpoint de resumen de aging agregado a nivel de compañía.

### 6. Analytics Company ID es hardcodeado para demo

**Causa**: El mapping `ksdepor → company-dev-001` está en `appsettings.json` vía `NativeBi:CompanySlugMap`.

**Impacto**: Solo funciona para el cliente KSDEPOR en demo. No es multi-cliente productivo.

**Solución**: Sprint 9 agrega `analytics_company_id` a la tabla `companies` en Azure SQL, y el resolver consulta la DB. El fallback de appsettings queda solo para entorno Development.

### 7. EBITDA / Estado de Resultados no disponible

**Causa**: Requiere modelo contable completo (cuentas, asientos OAEP, etc.) que está fuera del scope del MVP Native BI.

**Estado**: Backlog.

### 8. Comparación interanual no disponible

**Causa**: Los KPIs actuales muestran el período seleccionado vs ningún período de referencia.

**Estado**: Backlog. Requiere agregar filtros `dateFrom`/`dateTo` de período de comparación y endpoints que devuelvan ambos períodos.

---

## Backlog técnico MART

### Endpoints a agregar en el futuro

| Endpoint | Descripción | Prioridad |
|---|---|---|
| `GET /api/nativebi/sales/summary-totals` | Totales globales sin paginar para concentración % real | Alta |
| `GET /api/nativebi/finance/ar-aging-summary` | Totales de aging a nivel empresa (no paginado) | Alta |
| `GET /api/nativebi/inventory/stock-by-warehouse` | Stock valorizado por almacén (requiere OITW) | Media |
| `GET /api/nativebi/finance/ap-aging-summary` | Totales AP nivel empresa | Media |
| `GET /api/nativebi/sales/comparison` | Ventas período actual vs período anterior | Baja |

### Objetos SAP requeridos para features completos

| Objeto SAP | Feature | Estado |
|---|---|---|
| OINV / INV1 | Ventas, Dashboard, Finanzas AR | ✅ Implementado |
| ORIN / RIN1 | Notas de crédito | ✅ Implementado |
| OPOR / POR1 | Compras OC | ✅ Implementado |
| OPDN / PDN1 | Recepciones de mercadería | ✅ Implementado |
| OCRD | Clientes y proveedores | ✅ Implementado |
| OITM | Artículos | ✅ Implementado |
| OSLP | Vendedores | ✅ Implementado |
| OOPCH / PCH1 | Facturas de proveedor (AP) | ⚠️ Requiere datos |
| OWTR | Traspasos entre almacenes | ⚠️ Requiere datos |
| OITW | Stock por almacén | 📋 Backlog |
| OAEP | Asientos contables | 📋 Backlog (EBITDA) |

### Vistas MART a crear en el futuro

| Vista | Descripción |
|---|---|
| `mart.sales_comparison` | Ventas período actual vs período anterior |
| `mart.inventory_stock_by_warehouse` | Stock valorizado por almacén y artículo |
| `mart.finance_company_ar_summary` | Aging AR agregado a nivel empresa |
| `mart.finance_company_ap_summary` | Aging AP agregado a nivel empresa |
| `mart.sales_customer_totals` | Totales globales de clientes (sin paginación) |
| `mart.pl_monthly` | Estado de resultados mensual |

---

## Riesgos

| Riesgo | Descripción | Mitigación |
|---|---|---|
| Datos paginados en KPIs de % | La participación % no refleja el 100% del universo | Documentado en UI; mejorable con endpoint de totales |
| Demo usa un solo cliente KSDEPOR | No es multi-cliente real | Sprint 9 resuelve esto con `analytics_company_id` en DB |
| Freshness de datos depende del extractor | Si el extractor falla, los KPIs quedan desactualizados | Módulo de Operaciones y Diagnósticos monitorea esto |
| SQLite en dev, Azure SQL en prod | Algunos comportamientos de EF pueden diferir | Tests corren con SQLite en memoria; validar migración en staging |
