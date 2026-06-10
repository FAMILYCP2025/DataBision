# DataBision — Modelo MART por Proceso (Supabase PostgreSQL)

**Sprint:** 8C  
**Fecha:** 2026-06-10  
**Schema:** `mart`

---

## Arquitectura

Las tablas MART son vistas materializadas actualizadas por funciones PostgreSQL invocadas desde el Extractor (`--transform --include-mart` o `--transform-mart`). No hay lógica de transformación en C#.

```
SAP B1 → Extractor → stg.* (Dapper) → mart.refresh_*_process() → mart.*
```

---

## Tablas por proceso

### SALES (parcialmente existentes desde Sprint 5)

| Tabla | Estado | Depende de STG | Función |
|-------|--------|---------------|---------|
| mart.sales_daily | ✅ Existe | stg.sales_invoice | refresh_sales_daily() |
| mart.sales_monthly | ✅ Existe | stg.sales_invoice | refresh_sales_monthly() |
| mart.customer_sales | ✅ Existe | stg.sales_invoice | refresh_customer_sales() |
| mart.item_sales | ✅ Existe | stg.invoice_line | refresh_item_sales() |
| mart.salesperson_sales | ✅ Existe | stg.sales_invoice | refresh_salesperson_sales() |
| mart.sales_kpi_summary | ✅ Existe | stg.sales_invoice | refresh_sales_kpi_summary() |
| mart.sales_customer_dashboard | 🆕 Sprint 8C | stg.sales_invoice + stg.customer | refresh_sales_process() |
| mart.sales_item_dashboard | 🆕 Sprint 8C | stg.invoice_line | refresh_sales_process() |
| mart.sales_fulfillment_dashboard | 🆕 Sprint 8C | stg.sales_order + stg.delivery | ⏳ Defensivo (ORDR/ODLN pendientes) |

### PURCHASING (nuevas Sprint 8C)

| Tabla | Estado | Depende de STG | Activación |
|-------|--------|---------------|-----------|
| mart.purchase_executive_daily | 🆕 Creada, vacía | stg.purchase_order (OPOR) | Sprint 8F |
| mart.purchase_supplier_dashboard | 🆕 Creada, vacía | stg.purchase_order | Sprint 8F |
| mart.purchase_receiving_dashboard | 🆕 Creada, vacía | stg.purchase_delivery (OPDN) | Sprint 8F |

### INVENTORY (nuevas Sprint 8C)

| Tabla | Estado | Depende de STG | Activación |
|-------|--------|---------------|-----------|
| mart.inventory_rotation_dashboard | 🆕 Funcional parcial | stg.item + stg.invoice_line | Ahora (OITM+INV1) |
| mart.inventory_stock_dashboard | 🆕 Creada, vacía | stg.item_warehouse (OITW) | Sprint 8F |
| mart.inventory_warehouse_dashboard | 🆕 Creada, vacía | stg.stock_transfer (OWTR) | Sprint 8F |

### FINANCE (nuevas Sprint 8C)

| Tabla | Estado | Depende de STG | Activación |
|-------|--------|---------------|-----------|
| mart.finance_ar_aging_dashboard | 🆕 Funcional | stg.sales_invoice (OINV) | Ahora |
| mart.finance_ap_aging_dashboard | 🆕 Creada, vacía | stg.purchase_invoice (OPCH) | Sprint 8F |
| mart.finance_executive_daily | 🆕 Funcional (AR) | stg.sales_invoice | Ahora |

---

## Funciones de proceso

### refresh_sales_process(company_id)
Llama todos los refresh de ventas en orden:
1. refresh_sales_daily → refresh_sales_monthly → refresh_customer_sales
2. refresh_item_sales → refresh_salesperson_sales → refresh_sales_kpi_summary
3. Nuevo: refresh sales_customer_dashboard + sales_item_dashboard
4. sales_fulfillment_dashboard: salta (defensivo, ORDR/ODLN no extraídos)

### refresh_purchasing_process(company_id)
- Defensiva total: verifica `IF EXISTS stg.purchase_order`
- Si no existe: `RAISE NOTICE` y sale limpio

### refresh_inventory_process(company_id)
- inventory_rotation_dashboard: funcional si stg.item existe
- inventory_stock_dashboard: defensivo (requiere OITW)
- inventory_warehouse_dashboard: defensivo (requiere OWTR)

### refresh_finance_process(company_id)
- finance_ar_aging_dashboard + finance_executive_daily: funcionales con OINV
- finance_ap_aging_dashboard: defensivo (requiere OPCH)

### refresh_all_processes(company_id)
Orquestador:
```sql
PERFORM mart.refresh_sales_process(p_company_id);
PERFORM mart.refresh_purchasing_process(p_company_id);
PERFORM mart.refresh_inventory_process(p_company_id);
PERFORM mart.refresh_finance_process(p_company_id);
```

---

## Invocación desde Extractor

```bash
# Refresh completo de todos los procesos
dotnet run --project src/DataBision.Extractor -- --transform --include-mart --company ksdepor

# Solo MART (STG ya poblado)
dotnet run --project src/DataBision.Extractor -- --transform-mart --company ksdepor
```

---

## Notas sobre approximaciones

**AR Aging**: `balance_due` = `doc_total` (no hay datos de pagos en SAP B1 Service Layer sin acceder a módulo de ledger). Es una aproximación conservadora (worst-case). Cuando se active OPCH/JDT1 en el futuro, se puede refinar.

**Rotación**: Basada en INV1 (líneas de factura). Si INV1 no está extraído, los campos `qty_sold_*` quedan en 0. Los ítems sin ventas aparecen como `NO_MOVEMENT`.

**Stock**: Campos de stock (`on_hand_qty`, `available_qty`) son NULL hasta que OITW sea extraído.
