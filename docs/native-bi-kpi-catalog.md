# Native BI — Catálogo de KPIs

Catálogo de todos los indicadores clave de rendimiento (KPIs) expuestos por el módulo Native BI de DataBision.

## Convenciones

| Campo | Descripción |
|---|---|
| **Módulo** | Dashboard / Ventas / Compras / Inventario / Finanzas / Operaciones |
| **Fuente** | Tabla MART, STG, OPS o CFG de Supabase |
| **Grain** | Nivel de agregación del dato |
| **Cómputo** | Dónde se calcula: backend (MART/SQL) o client-side (React derivado) |

---

## Dashboard (resumen ejecutivo)

| KPI | Descripción funcional | Fórmula | Fuente | Grain |
|---|---|---|---|---|
| Ventas netas | Ventas brutas menos notas de crédito | `gross_sales - credit_memo` | `mart.dashboard_summary` | Período completo |
| Ventas brutas | Total bruto facturado | `SUM(invoice_lines.line_total)` | `mart.dashboard_summary` | Período completo |
| Facturas emitidas | Número de documentos OINV | `COUNT(DISTINCT doc_num)` | `mart.dashboard_summary` | Período completo |
| Clientes activos | Clientes con al menos 1 factura | `COUNT(DISTINCT card_code)` | `mart.dashboard_summary` | Período completo |
| Artículos activos | Artículos vendidos al menos 1 vez | `COUNT(DISTINCT item_code)` | `mart.dashboard_summary` | Período completo |
| Ticket promedio | Ventas netas / número de facturas | `net_sales / invoice_count` | `mart.dashboard_summary` | Período completo |

---

## Ventas (NativeBiSalesPage)

| KPI | Descripción funcional | Fórmula | Fuente | Grain | Cómputo |
|---|---|---|---|---|---|
| Ventas netas | Neto facturado en el período | `gross - credit_memos` | `mart.sales_overview` | Período seleccionado | Backend |
| Ventas brutas | Bruto facturado | `SUM(gross_sales)` | `mart.sales_overview` | Período seleccionado | Backend |
| Facturas | Documentos emitidos | `invoice_count` | `mart.sales_overview` | Período seleccionado | Backend |
| Ticket promedio | Promedio por factura | `net_sales / invoice_count` | `mart.sales_overview` | Período seleccionado | Backend |
| Clientes activos | Clientes que compraron | `active_customers` | `mart.sales_overview` | Período seleccionado | Backend |
| NC / Devoluciones | Notas de crédito | `credit_memo_amount` | `mart.sales_overview` | Período seleccionado | Backend |
| Promedio por cliente | Ventas netas / clientes | `net_sales / active_customers` | `mart.sales_overview` | Período seleccionado | Client-side |
| Concentración top 5 | % de ventas en top 5 clientes | `sum(top5_net) / total_net * 100` | `mart.customers_dashboard` | Período seleccionado | Client-side |
| Participación % cliente | Peso de cada cliente en total neto | `customer_net / total_net * 100` | `mart.customers_dashboard` | Período seleccionado | Client-side |
| Fill rate promedio | Tasa de cumplimiento de pedidos | `delivered / orders * 100` | `mart.sales_fulfillment` | Por período | Backend |

---

## Compras (PurchasingDashboardPage)

| KPI | Descripción funcional | Fórmula | Fuente | Grain | Cómputo |
|---|---|---|---|---|---|
| Órdenes de compra | Total de OC emitidas | `SUM(po_count)` | `mart.purchasing_executive` | Período | Client-side |
| Monto OC | Valor total de OC | `SUM(po_amount)` | `mart.purchasing_executive` | Período | Client-side |
| Monto recibido | Total recepciones de mercadería | `SUM(received_amount)` | `mart.purchasing_executive` | Período | Client-side |
| % Recibido | Cumplimiento vs emitido | `received / po_amount * 100` | Derivado | Período | Client-side |
| Proveedores activos | Proveedores con al menos 1 OC | `MAX(active_suppliers)` | `mart.purchasing_executive` | Período | Client-side |
| Promedio por OC | Monto medio por orden | `po_amount / po_count` | Derivado | Período | Client-side |
| Brecha pendiente | OC no recibida | `po_amount - received_amount` | Derivado | Período | Client-side |
| % recibido por proveedor | Cumplimiento individual | `received / po_amount * 100` | `mart.purchasing_supplier` | Por proveedor | Client-side |

---

## Inventario (InventoryDashboardPage)

| KPI | Descripción funcional | Fórmula | Fuente | Grain | Cómputo |
|---|---|---|---|---|---|
| Alta rotación | SKUs con rotación FAST | `COUNT(WHERE status = 'FAST')` | `mart.inventory_rotation` | Página actual | Client-side |
| Rotación normal | SKUs con rotación NORMAL | `COUNT(WHERE status = 'NORMAL')` | `mart.inventory_rotation` | Página actual | Client-side |
| Baja rotación | SKUs con rotación SLOW | `COUNT(WHERE status = 'SLOW')` | `mart.inventory_rotation` | Página actual | Client-side |
| Sin movimiento | SKUs sin ventas | `COUNT(WHERE status = 'NO_MOVEMENT')` | `mart.inventory_rotation` | Página actual | Client-side |
| Cobertura promedio | Días promedio de stock | `AVG(coverage_days)` | `mart.inventory_rotation` | Página actual | Client-side |
| % Sin movimiento | Proporción de SKUs inactivos | `no_movement / total * 100` | Derivado | Página actual | Client-side |
| Cantidad vendida 30d | Unidades en últimos 30 días | `qty_sold_30d` | `mart.inventory_rotation` | Por SKU | Backend |
| Cantidad vendida 90d | Unidades en últimos 90 días | `qty_sold_90d` | `mart.inventory_rotation` | Por SKU | Backend |
| Cobertura en días | Stock / venta diaria promedio | `on_hand_qty / avg_daily_sales_qty` | `mart.inventory_rotation` | Por SKU | Backend |

---

## Finanzas (FinanceDashboardPage)

| KPI | Descripción funcional | Fórmula | Fuente | Grain | Cómputo |
|---|---|---|---|---|---|
| AR vencido | Cuentas por cobrar vencidas | `SUM(ar_overdue)` | `mart.finance_executive` | Acumulado período | Client-side |
| % vencido | Proporción vencida sobre AR total | `ar_overdue_pct` | `mart.finance_executive` | Período | Backend |
| Facturas emitidas 30d | Facturas en período | `SUM(new_invoices_count)` | `mart.finance_executive` | Período | Client-side |
| Monto facturado 30d | Valor facturado | `SUM(new_invoices_amount)` | `mart.finance_executive` | Período | Client-side |
| Riesgo +90d | Saldo con aging mayor a 90 días | `SUM(aging_90_plus)` | `mart.ar_aging` | Por cliente | Client-side |
| Top deudor | Cliente con mayor saldo vencido | `MAX(overdue_amount)` | `mart.ar_aging` | Por cliente | Client-side |
| Deuda promedio | Saldo medio por cliente | `SUM(balance_due) / COUNT` | `mart.ar_aging` | Página actual | Client-side |
| % sobre saldo +90d | Proporción aging+90 vs saldo | `aging_90_plus / balance_due * 100` | Derivado | Por cliente | Client-side |
| Aging 0-30d | Saldo corriente | `SUM(aging_0_to_30)` | `mart.ar_aging` | Página actual | Client-side |
| Aging 31-60d | Vencido 1-2 meses | `SUM(aging_31_to_60)` | `mart.ar_aging` | Página actual | Client-side |
| Aging 61-90d | Vencido 2-3 meses | `SUM(aging_61_to_90)` | `mart.ar_aging` | Página actual | Client-side |
| Aging +90d | Vencido más de 3 meses | `SUM(aging_90_plus)` | `mart.ar_aging` | Página actual | Client-side |

---

## Operaciones (OperationsDashboardPage)

| KPI | Descripción funcional | Fuente | Cómputo |
|---|---|---|---|
| Health score | Puntaje de salud 0-100 | `ops.pipeline_health` | Backend |
| Alertas activas | Alertas no resueltas | `ops.alerts WHERE is_resolved = false` | Backend |
| Objetos extraídos | SAP objects procesados | `ops.pipeline_health` | Backend |
| Errores DQ sin resolver | Issues de calidad abiertos | `ops.data_quality WHERE is_resolved = false` | Backend |
| Estado extractor | OK / Warning / Error / Unknown | `ops.pipeline_health.extractor_status` | Backend |
| Estado transform | OK / Warning / Error / Unknown | `ops.pipeline_health.transform_status` | Backend |

---

## Diagnósticos (NativeBiDiagnosticsPage)

| Indicador | Descripción | Fuente |
|---|---|---|
| Status general | Estado agregado del sistema | Derivado de checks |
| Checks individuales | Verificaciones staging, MART, etc. | API /diagnostics |
| Conteo por tabla | Filas por tabla STG/MART/OPS/CFG | API /table-counts |
| Objetos SAP | Estado de extracción por objeto | API /sync/objects |
| Watermark | Fecha de último dato extraído | `cfg.sync_state` |
| Transform STG/MART | Fecha última transformación | API /sync/transform-status |
