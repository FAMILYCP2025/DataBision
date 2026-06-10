# DataBision — Catálogo de Procesos, Dashboards y KPIs

**Versión:** 1.0 — 2026-06-10  
**Sprint:** 8A  
**Base de datos:** Supabase PostgreSQL — schema `cfg`

---

## 1. Procesos

| Código | Nombre | Descripción | Orden |
|---|---|---|---|
| SALES | Ventas | Análisis de ventas, clientes y cumplimiento de pedidos | 1 |
| PURCHASING | Compras | Gestión de proveedores, órdenes de compra y recepciones | 2 |
| INVENTORY | Inventario | Control de stock, rotación y valorización | 3 |
| FINANCE | Finanzas | Cuentas por cobrar, cuentas por pagar y flujo de caja | 4 |
| OPERATIONS | Operaciones | Salud del pipeline de datos y monitoreo operativo | 5 |

---

## 2. Dashboards por proceso

### SALES

| Código | Nombre | Tipo | Estado |
|---|---|---|---|
| SALES_EXECUTIVE | Resumen Ejecutivo | EXECUTIVE | ✅ Implementado (`/client/bi/dashboard`) |
| SALES_CUSTOMERS | Análisis de Clientes | ANALYTICAL | ✅ Implementado (`/client/bi/sales`) |
| SALES_ITEMS_MARGIN | Productos y Margen | ANALYTICAL | ✅ Parcial (sin margen — no hay costo) |
| SALES_ORDER_FULFILLMENT | Cumplimiento de Pedidos | OPERATIONAL | ⏳ Pendiente (requiere ORDR/ODLN) |

### PURCHASING

| Código | Nombre | Tipo | Estado |
|---|---|---|---|
| PURCHASING_EXECUTIVE | Resumen Ejecutivo Compras | EXECUTIVE | ⏳ MART creado, sin datos (requiere OPOR) |
| PURCHASING_SUPPLIERS | Análisis de Proveedores | ANALYTICAL | ⏳ MART creado, sin datos |
| PURCHASING_RECEIVING | Control de Recepciones | OPERATIONAL | ⏳ MART creado, sin datos (requiere OPDN) |
| PURCHASING_PRICE_VARIATION | Variación de Precios | CONTROL | ❌ No implementado aún |

### INVENTORY

| Código | Nombre | Tipo | Estado |
|---|---|---|---|
| INVENTORY_EXECUTIVE | Resumen Ejecutivo Inventario | EXECUTIVE | ⏳ MART creado, parcial (sin OITW) |
| INVENTORY_STOCK_VALUE | Valor de Inventario | ANALYTICAL | ⏳ MART creado, sin stock value (requiere OITW) |
| INVENTORY_ROTATION_COVERAGE | Rotación y Cobertura | ANALYTICAL | ⏳ MART creado, rotación funcional con ventas |
| INVENTORY_WAREHOUSE_TRANSFERS | Transferencias entre Bodegas | OPERATIONAL | ⏳ MART creado, sin datos (requiere OWTR) |

### FINANCE

| Código | Nombre | Tipo | Estado |
|---|---|---|---|
| FINANCE_EXECUTIVE | Resumen Ejecutivo Finanzas | EXECUTIVE | ⏳ MART creado, AR funcional |
| FINANCE_AR_AGING | Aging Cuentas por Cobrar | CONTROL | ✅ Funcional (OINV extraído) |
| FINANCE_AP_AGING | Aging Cuentas por Pagar | CONTROL | ⏳ MART creado, sin datos (requiere OPCH) |
| FINANCE_CASHFLOW_CONTROL | Control de Flujo de Caja | CONTROL | ❌ No implementado aún |

### OPERATIONS

| Código | Nombre | Tipo | Estado |
|---|---|---|---|
| OPERATIONS_EXECUTIVE | Salud del Pipeline | EXECUTIVE | ✅ Implementado (`/client/bi/diagnostics`) |
| OPERATIONS_PIPELINE_HEALTH | Detalle del Pipeline | OPERATIONAL | ✅ Implementado (diagnósticos) |
| OPERATIONS_DATA_QUALITY | Calidad de Datos | CONTROL | ⏳ ops.data_quality_issue creado, sin UI |
| OPERATIONS_ALERTS | Alertas Activas | CONTROL | ⏳ ops.alert_event creado, sin UI |

---

## 3. KPIs por proceso

### SALES KPIs

| Código | Nombre | Fórmula | Tipo | Fuente | Estado |
|---|---|---|---|---|---|
| NET_SALES | Ventas Netas | `gross_sales - credit_memos` | AMOUNT | mart.sales_kpi_summary | ✅ Funcional |
| GROSS_SALES | Ventas Brutas | `SUM(doc_total) WHERE cancelled != Y` | AMOUNT | mart.sales_kpi_summary | ✅ Funcional |
| INVOICE_COUNT | Facturas Emitidas | `COUNT(OINV) WHERE cancelled != Y` | COUNT | mart.sales_kpi_summary | ✅ Funcional |
| ACTIVE_CUSTOMERS | Clientes Activos | `COUNT(DISTINCT card_code)` | COUNT | mart.sales_kpi_summary | ✅ Funcional |
| AVG_TICKET | Ticket Promedio | `gross_sales / NULLIF(invoice_count, 0)` | AMOUNT | mart.sales_kpi_summary | ✅ Funcional |
| FILL_RATE_PCT | Fill Rate % | `delivered_qty / NULLIF(ordered_qty, 0)` | PERCENT | mart.sales_fulfillment_dashboard | ⏳ Requiere ORDR+ODLN |
| GROSS_MARGIN_PCT | Margen Bruto % | `gross_profit / NULLIF(net_sales, 0)` | PERCENT | mart.sales_item_dashboard | ⏳ Requiere datos de costo |

### PURCHASING KPIs

| Código | Nombre | Fórmula | Tipo | Dependencia |
|---|---|---|---|---|
| PO_AMOUNT | Monto Órdenes de Compra | `SUM(doc_total) FROM stg.purchase_order` | AMOUNT | ⏳ Requiere OPOR |
| GOODS_RECEIPT_AMT | Monto Recepciones | `SUM(doc_total) FROM stg.purchase_delivery` | AMOUNT | ⏳ Requiere OPDN |
| ACTIVE_SUPPLIERS | Proveedores Activos | `COUNT(DISTINCT supplier_code)` | COUNT | ⏳ Requiere OPOR |
| PO_COUNT | Órdenes de Compra | `COUNT(OPOR)` | COUNT | ⏳ Requiere OPOR |

### INVENTORY KPIs

| Código | Nombre | Fórmula | Tipo | Dependencia |
|---|---|---|---|---|
| STOCK_VALUE | Valor de Stock | `SUM(on_hand * avg_price)` | AMOUNT | ⏳ Requiere OITW |
| STOCKOUT_ITEMS | Ítems sin Stock | `COUNT WHERE available_qty <= 0` | COUNT | ⏳ Requiere OITW |
| COVERAGE_DAYS | Días de Cobertura | `on_hand / NULLIF(avg_daily_sales_qty, 0)` | DAYS | ⏳ Parcial (rotación funcional, on_hand requiere OITW) |
| SLOW_MOVING_ITEMS | Ítems Movimiento Lento | `COUNT WHERE rotation_status IN (SLOW, NO_MOVEMENT)` | COUNT | ⏳ Rotación funcional con OITM+OINV |

### FINANCE KPIs

| Código | Nombre | Fórmula | Tipo | Dependencia |
|---|---|---|---|---|
| AR_TOTAL | Total CxC | `SUM(balance_due) FROM finance_ar_aging` | AMOUNT | ✅ Funcional (OINV) |
| AR_OVERDUE | CxC Vencida | `SUM WHERE doc_due_date < CURRENT_DATE` | AMOUNT | ✅ Funcional (OINV) |
| AR_OVERDUE_PCT | Mora CxC % | `ar_overdue / NULLIF(ar_total, 0)` | PERCENT | ✅ Funcional (OINV) |
| AP_TOTAL | Total CxP | `SUM(balance_due) FROM finance_ap_aging` | AMOUNT | ⏳ Requiere OPCH |

### OPERATIONS KPIs

| Código | Nombre | Fuente | Estado |
|---|---|---|---|
| PIPELINE_HEALTH | Estado Pipeline | ops.pipeline_health | ⏳ ops schema creado, función implementada |
| DQ_ERRORS | Errores Calidad | ops.data_quality_issue | ⏳ ops schema creado |
| LAST_EXTRACTOR_RUN | Último Run Extractor | ops.extractor_run | ⏳ ops schema creado |

---

## 4. Objetos SAP por proceso

### Activos (extraídos hoy)

| SAP Object | Endpoint | Proceso | Tipo | Incremental |
|---|---|---|---|---|
| OINV | Invoices | SALES | HEADER | ✅ UpdateDate |
| INV1 | Invoices/.../DocumentLines | SALES | LINE | — |
| ORIN | CreditNotes | SALES | HEADER | ✅ UpdateDate |
| RIN1 | CreditNotes/.../DocumentLines | SALES | LINE | — |
| OCRD | BusinessPartners | SALES | MASTER | ✅ UpdateDate |
| OSLP | SalesPersons | SALES | MASTER | — |
| OITM | Items | SALES | MASTER | ✅ UpdateDate |

### Preparados (is_active = false)

| SAP Object | Proceso | Dependencia MART |
|---|---|---|
| ORDR / RDR1 | SALES | mart.sales_fulfillment_dashboard |
| ODLN / DLN1 | SALES | mart.sales_fulfillment_dashboard |
| OPOR / POR1 | PURCHASING | mart.purchase_executive_daily, purchase_supplier |
| OPDN / PDN1 | PURCHASING | mart.purchase_receiving_dashboard |
| OPCH / PCH1 | PURCHASING/FINANCE | mart.purchase_supplier, finance_ap_aging |
| OITW | INVENTORY | mart.inventory_stock_dashboard |
| OWHS | INVENTORY | mart.inventory_warehouse_dashboard |
| OWTR / WTR1 | INVENTORY | mart.inventory_warehouse_dashboard |

---

## 5. Habilitación por empresa

La tabla `cfg.company_process_enabled` controla qué procesos están activos por empresa.

**Seeds incluidos:** `company-dev-001` → todos los procesos activos.

**KSDEPOR:** No incluido en seed automático. Confirmar `company_id` de KSDEPOR en la base de datos y ejecutar:
```sql
INSERT INTO cfg.company_process_enabled (company_id, process_code, is_enabled, enabled_at_utc)
SELECT '<ksdepor-company-id>', process_code, TRUE, NOW()
FROM cfg.process WHERE is_active = TRUE;
```

---

## 6. Dependencias de datos pendientes

| Dependencia | Impacto | Sprint activación |
|---|---|---|
| ORDR/RDR1 extraídos | Fill Rate, Fulfillment dashboard | Sprint 8F |
| ODLN/DLN1 extraídos | Fill Rate, Fulfillment dashboard | Sprint 8F |
| OPOR/POR1 extraídos | Todos KPIs PURCHASING | Sprint 8F |
| OPCH/PCH1 extraídos | AP Aging, Finance executive | Sprint 8F |
| OITW extraído | Stock value, Coverage days, Stockout | Sprint 8F |
| Datos de costo en OITM/INV1 | Margen bruto | Sprint 9+ |

---

*Documento vivo — actualizar al activar nuevos objetos o dashboards.*
