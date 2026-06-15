# KSDEPOR — MART Refresh (Sprint 8G)

**Fecha:** 2026-06-10  
**Estado:** PENDIENTE — requiere configuración manual

---

## Blocker

El extractor necesita `Staging:ConnectionString` en su `appsettings.Development.json` para poder ejecutar `--transform`. Esta clave sí está configurada en el proyecto API (para EF migrations) pero no en el extractor.

**Acción requerida** — añadir al archivo `src/DataBision.Extractor/appsettings.Development.json`:

```json
"Staging": {
  "ConnectionString": "<mismo valor que StagingConnection en src/DataBision.Api/appsettings.Development.json>"
}
```

Ver template en: `src/DataBision.Extractor/appsettings.Development.template.json`

---

## Comando a ejecutar (una vez configurado)

```bash
# Refresh STG + MART completo
dotnet run --project src/DataBision.Extractor -- --transform --include-mart

# Solo MART (si STG ya tiene datos)
dotnet run --project src/DataBision.Extractor -- --transform-mart
```

---

## Tablas a validar post-refresh

### MART Sales (existentes desde Sprint 5, actualización esperada)
```sql
SELECT COUNT(*) FROM mart.sales_daily WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.sales_monthly WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.customer_sales WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.item_sales WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.salesperson_sales WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.sales_kpi_summary WHERE company_id = '<id>';
```

### MART nuevas (Sprint 8C)
```sql
SELECT COUNT(*) FROM mart.sales_customer_dashboard WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.sales_item_dashboard WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.inventory_rotation_dashboard WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.finance_ar_aging_dashboard WHERE company_id = '<id>';
SELECT COUNT(*) FROM mart.finance_executive_daily WHERE company_id = '<id>';
```

### KPIs a verificar post-refresh

```sql
-- KPIs ventas
SELECT gross_sales_amount, net_sales_amount, invoice_count, active_customers, avg_ticket_amount
FROM mart.sales_kpi_summary WHERE company_id = '<id>' ORDER BY period_month DESC LIMIT 3;

-- AR Aging
SELECT COUNT(*) total_clientes, SUM(balance_due) total_ar, SUM(overdue_amount) ar_vencido
FROM mart.finance_ar_aging_dashboard WHERE company_id = '<id>';

-- Finance executive
SELECT period_date, ar_total, ar_overdue, ar_overdue_pct
FROM mart.finance_executive_daily WHERE company_id = '<id>' ORDER BY period_date DESC LIMIT 5;

-- Inventory rotation
SELECT rotation_status, COUNT(*) items
FROM mart.inventory_rotation_dashboard WHERE company_id = '<id>' GROUP BY rotation_status;
```

---

## KPIs esperados (estimación con datos parciales 8F)

| KPI | Valor esperado | Fuente |
|-----|----------------|--------|
| Facturas activas | > 70 (total histórico OINV) | mart.sales_kpi_summary |
| Clientes activos | Subset del total OCRD | mart.sales_kpi_summary |
| Notas de crédito | > 54 (total histórico ORIN) | mart.sales_kpi_summary.credit_memo_count |
| AR total | Sum(doc_total) de OINV vigentes | mart.finance_ar_aging_dashboard |
| Items con movimiento | Subset OITM con ventas en INV1 | mart.inventory_rotation_dashboard |
| Items sin movimiento | Items en OITM sin INV1 | mart.inventory_rotation_dashboard |

**Nota:** Los KPIs de inventario (stock real) requieren OITW. Los KPIs de compras requieren OPOR. Ambos pendientes Sprint 8F-ext.

---

## Tablas que quedarán vacías (esperado)

| Tabla | Razón |
|-------|-------|
| mart.sales_fulfillment_dashboard | Requiere ORDR + ODLN |
| mart.purchase_* | Requiere OPOR + OPDN |
| mart.inventory_stock_dashboard | Requiere OITW |
| mart.inventory_warehouse_dashboard | Requiere OWTR |
| mart.finance_ap_aging_dashboard | Requiere OPCH |

Estas tablas son defensivas — devuelven 0 rows sin error.

---

## Una vez ejecutado — acciones de seguimiento

```bash
# Evaluar alertas
# SELECT ops.evaluate_alert_rules('<company_id>');

# Ver health
# SELECT * FROM ops.pipeline_health WHERE company_id = '<company_id>';
```
