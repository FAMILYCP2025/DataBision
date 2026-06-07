# Dashboard Backend Endpoints — Source Map

Maps each API endpoint to its MART/STG/CTL source and the UI component it feeds.

---

## Dashboard ejecutivo

| Endpoint | MART/STG source | UI component |
|---|---|---|
| `GET /dashboard/summary` | `mart.sales_kpi_summary` | KPI cards: Ventas Brutas, Ventas Netas, N° Facturas, N° Clientes, Ticket Promedio |
| `GET /dashboard/sales-daily?days=30` | `mart.sales_daily` | Gráfico de barras diario (últimos 30 días) |
| `GET /dashboard/sales-monthly?months=12` | `mart.sales_monthly` | Gráfico de barras mensual (12 meses) |
| `GET /dashboard/top-customers?limit=10` | `mart.customer_sales` | Tabla Top 10 Clientes |
| `GET /dashboard/top-items?limit=10` | `mart.item_sales` | Tabla Top 10 Productos |
| `GET /dashboard/salespersons?limit=20` | `mart.salesperson_sales` | Tabla Vendedores |

---

## Módulo Ventas

| Endpoint | MART source | UI component |
|---|---|---|
| `GET /sales/overview?dateFrom=...&dateTo=...` | `mart.sales_daily` (SUM) | KPIs de período filtrado |
| `GET /sales/daily?dateFrom=...&dateTo=...` | `mart.sales_daily` | Gráfico diario con filtro de fecha |
| `GET /sales/monthly?dateFrom=...&dateTo=...` | `mart.sales_monthly` | Gráfico mensual con filtro de fecha |
| `GET /sales/customers?limit=50` | `mart.customer_sales` | Tabla completa de clientes |
| `GET /sales/items?limit=50` | `mart.item_sales` | Tabla completa de productos |
| `GET /sales/salespersons?limit=50` | `mart.salesperson_sales` | Tabla completa de vendedores |

---

## Sync Center

| Endpoint | CTL/MART/STG source | UI component |
|---|---|---|
| `GET /sync/status` | `ctl.ingest_checkpoint` + `ctl.extraction_run` + `mart.sales_kpi_summary` + `stg.sales_invoice` | Panel de estado general (badge OK/Warning/Error) |
| `GET /sync/objects` | `ctl.ingest_checkpoint` | Lista por objeto SAP con watermark y filas |
| `GET /sync/transform-status` | `mart.*` (row counts + transformed_at_utc) + `stg.sales_invoice` | Panel de frescura STG/MART por tabla |

---

## Diagrama de capas

```
SAP B1 Service Layer
    └── Extractor (dotnet run -- --run-once)
            └── Ingest API POST /api/ingest/sap-b1/*
                    └── raw.o_inv, raw.inv_1, raw.o_rin, raw.rin_1, raw.o_itm, raw.o_slp
                            └── Extractor --transform
                                    └── stg.sales_invoice, stg.credit_memo, ...
                                            └── Extractor --transform-mart
                                                    └── mart.sales_daily, mart.sales_monthly, ...
                                                            └── GET /api/client/dashboard/*
                                                            └── GET /api/client/sales/*
                                                            └── GET /api/client/sync/*
                                                                    └── Frontend React (Sprint 7A)
```

---

## Dependencias de datos por endpoint

### `/dashboard/summary`
Requires: at least one run of `mart.refresh_sales_kpi_summary(company_id)`
Null if: no MART data for the company

### `/dashboard/sales-daily`
Requires: `mart.refresh_sales_daily(company_id)`
Empty if: no data in `mart.sales_daily` for the last N days

### `/sync/status`
Requires: `ctl.ingest_checkpoint` rows (populated after first `--run-once`)
Status "unknown" if: no `mart.sales_kpi_summary` row exists

---

## Default values and limits

| Param | Default | Min | Max | Behavior when out of range |
|---|---|---|---|---|
| `days` | 30 | 1 | 365 | Clamped |
| `months` | 12 | 1 | 36 | Clamped |
| `limit` (dashboard) | 10 or 20 | 1 | 100 | Clamped |
| `limit` (sales) | 50 | 1 | 100 | Clamped |
| `dateFrom` | today - 30d | — | — | Uses default if blank |
| `dateTo` | today | — | — | Uses default if blank |
