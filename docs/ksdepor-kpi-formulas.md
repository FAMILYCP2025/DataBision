# KSDEPOR — Fórmulas KPI

**Sprint:** 8C  
**Fecha:** 2026-06-10

---

## SALES KPIs

| KPI | Código | Fórmula SQL | Tabla fuente | Estado |
|-----|--------|------------|-------------|--------|
| Ventas Brutas | GROSS_SALES | `SUM(doc_total) WHERE cancelled != 'Y'` | mart.sales_kpi_summary.gross_sales_amount | ✅ |
| Ventas Netas | NET_SALES | `gross_sales - credit_memos` | mart.sales_kpi_summary.net_sales_amount | ✅ |
| Facturas Emitidas | INVOICE_COUNT | `COUNT(OINV) WHERE cancelled != 'Y'` | mart.sales_kpi_summary.invoice_count | ✅ |
| Clientes Activos | ACTIVE_CUSTOMERS | `COUNT(DISTINCT card_code)` | mart.sales_kpi_summary.active_customers | ✅ |
| Ticket Promedio | AVG_TICKET | `gross_sales / NULLIF(invoice_count, 0)` | mart.sales_kpi_summary.avg_ticket_amount | ✅ |
| Fill Rate % | FILL_RATE_PCT | `delivered_qty / NULLIF(ordered_qty, 0) * 100` | mart.sales_fulfillment_dashboard | ⏳ ORDR+ODLN |
| Margen Bruto % | GROSS_MARGIN_PCT | `gross_profit / NULLIF(net_sales, 0) * 100` | mart.sales_item_dashboard | ⏳ Costo pendiente |

## PURCHASING KPIs

| KPI | Código | Fórmula SQL | Tabla fuente | Estado |
|-----|--------|------------|-------------|--------|
| Monto OC | PO_AMOUNT | `SUM(po_amount)` | mart.purchase_executive_daily | ⏳ OPOR |
| Monto Recepciones | GOODS_RECEIPT_AMT | `SUM(gr_amount)` | mart.purchase_receiving_dashboard | ⏳ OPDN |
| Proveedores Activos | ACTIVE_SUPPLIERS | `SUM(active_suppliers)` | mart.purchase_executive_daily | ⏳ OPOR |
| OC Emitidas | PO_COUNT | `SUM(po_count)` | mart.purchase_executive_daily | ⏳ OPOR |

## INVENTORY KPIs

| KPI | Código | Fórmula SQL | Tabla fuente | Estado |
|-----|--------|------------|-------------|--------|
| Valor de Stock | STOCK_VALUE | `SUM(stock_value)` | mart.inventory_stock_dashboard | ⏳ OITW |
| Ítems sin Stock | STOCKOUT_ITEMS | `COUNT WHERE available_qty <= 0` | mart.inventory_stock_dashboard | ⏳ OITW |
| Días de Cobertura | COVERAGE_DAYS | `on_hand_qty / NULLIF(avg_daily_sales_qty, 0)` | mart.inventory_rotation_dashboard | ⏳ OITW para on_hand |
| Ítem Movimiento Lento | SLOW_MOVING_ITEMS | `COUNT WHERE rotation_status IN ('SLOW','NO_MOVEMENT')` | mart.inventory_rotation_dashboard | ✅ Parcial |

**Lógica rotation_status:**
- `FAST`: qty_sold_30d > 0
- `NORMAL`: qty_sold_90d > 0 (pero 30d = 0)
- `SLOW`: last_sale_date existe pero > 90 días
- `NO_MOVEMENT`: sin ventas registradas
- `NO_STOCK_DATA`: on_hand_qty IS NULL (requiere OITW)

## FINANCE KPIs

| KPI | Código | Fórmula SQL | Tabla fuente | Estado |
|-----|--------|------------|-------------|--------|
| Total CxC | AR_TOTAL | `SUM(balance_due)` | mart.finance_ar_aging_dashboard | ✅ |
| CxC Vencida | AR_OVERDUE | `SUM(overdue_amount)` | mart.finance_ar_aging_dashboard | ✅ |
| Mora CxC % | AR_OVERDUE_PCT | `ar_overdue / NULLIF(ar_total, 0) * 100` | mart.finance_executive_daily.ar_overdue_pct | ✅ |
| Total CxP | AP_TOTAL | `SUM(balance_due)` | mart.finance_ap_aging_dashboard | ⏳ OPCH |

**Nota AR**: `balance_due` es una aproximación (= `doc_total`). No incluye pagos parciales. Precisión mejora cuando se active integración con módulo de pagos SAP.

## OPERATIONS KPIs

| KPI | Código | Fuente | Estado |
|-----|--------|--------|--------|
| Estado Pipeline | PIPELINE_HEALTH | ops.pipeline_health | ⏳ Sprint 8D |
| Errores Calidad | DQ_ERRORS | ops.data_quality_issue | ⏳ Sprint 8D |
| Último Run | LAST_EXTRACTOR_RUN | ops.extractor_run | ⏳ Sprint 8D |

---

## Dependencias de activación

Para activar los KPIs pendientes, el orden recomendado es:

1. **Sprint 8F**: Activar OPOR → habilita todos los KPIs PURCHASING
2. **Sprint 8F**: Activar OPDN → habilita GOODS_RECEIPT_AMT
3. **Sprint 8F**: Activar OITW → habilita STOCK_VALUE, STOCKOUT_ITEMS, COVERAGE_DAYS
4. **Sprint 8F**: Activar OPCH → habilita AP_TOTAL y AP aging
5. **Sprint 9+**: Datos de costo en OITM/INV1 → habilita GROSS_MARGIN_PCT
