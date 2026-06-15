# DataBision — Contrato API Dashboards por Proceso (Sprint 8H)

**Fecha:** 2026-06-10  
**Base URL:** `/api/client/`  
**Auth:** JWT Bearer (company context extraído de claims `company_id` / `company_slug`)  
**Response:** `{ "data": T }` o `{ "data": [], "meta": { limit, offset, count, hasMore } }`

---

## Procesos y Catálogo

### GET /api/client/processes
Retorna los procesos habilitados para la empresa actual.

**Response:**
```json
{
  "data": [
    { "processCode": "SALES", "processName": "Ventas", "displayOrder": 1, "isEnabled": true },
    { "processCode": "FINANCE", "processName": "Finanzas", "displayOrder": 4, "isEnabled": true }
  ]
}
```

### GET /api/client/processes/{processCode}/dashboards
Retorna los dashboards de un proceso.

**Params:** `processCode` (path) — ej. `SALES`, `FINANCE`, `INVENTORY`, `PURCHASING`

---

## SALES — /api/client/bi/sales/

### GET /api/client/bi/sales/customers-dashboard
**Query params:** `limit` (1-200, def 50), `offset` (def 0), `sortBy` (netSales|grossSales|invoiceCount|lastInvoiceDate|cardCode), `sortDir` (asc|desc)

**Response (paged):**
```json
{
  "data": [
    {
      "cardCode": "C001",
      "cardName": "Cliente Ejemplo",
      "cardType": "cCustomer",
      "salespersonName": "OFICINA-SOLIDEZ",
      "grossSales": 150000.00,
      "creditMemos": 5000.00,
      "netSales": 145000.00,
      "invoiceCount": 24,
      "avgTicket": 6041.67,
      "lastInvoiceDate": "2026-05-28",
      "isActive": true
    }
  ],
  "meta": { "limit": 50, "offset": 0, "count": 1, "hasMore": false }
}
```

### GET /api/client/bi/sales/items-dashboard
**Query params:** `limit`, `offset`, `sortBy` (grossSales|qtySold|invoiceCount|itemCode), `sortDir`

**Response (paged):** array de `SalesItemDashboardDto`

### GET /api/client/bi/sales/fulfillment
**Query params:** `days` (1-365, def 30)

**Response:** array de `SalesFulfillmentDto` (vacío si ORDR/ODLN no extraídos)

---

## FINANCE — /api/client/bi/finance/

### GET /api/client/bi/finance/executive
**Query params:** `days` (1-365, def 30)

**Response:**
```json
{
  "data": [
    {
      "periodDate": "2026-05-18",
      "arTotal": 250000.00,
      "arOverdue": 45000.00,
      "arOverduePct": 0.18,
      "apTotal": null,
      "apOverdue": null,
      "newInvoicesCount": 5,
      "newInvoicesAmount": 12500.00
    }
  ]
}
```

### GET /api/client/bi/finance/ar-aging
**Query params:** `limit`, `offset`, `sortBy` (overdueAmount|balanceDue|cardCode), `sortDir`

**Response (paged):** array de `FinanceArAgingDto`

| Campo | Tipo | Nota |
|-------|------|------|
| balanceDue | decimal | Aproximación: = doc_total (sin pagos parciales) |
| overdueAmount | decimal | Facturas vencidas por fecha de vencimiento |
| aging0To30..90Plus | decimal | Buckets de antigüedad |

### GET /api/client/bi/finance/ap-aging
**Query params:** `limit`, `offset`

**Response (paged):** vacío hasta que OPCH sea extraído (Sprint 8F-ext)

---

## INVENTORY — /api/client/bi/inventory/

### GET /api/client/bi/inventory/rotation
**Query params:** `limit`, `offset`, `sortBy` (qtySold30d|qtySold90d|coverageDays|rotationStatus|itemCode), `sortDir`

**Response (paged):**
```json
{
  "data": [
    {
      "itemCode": "PROD-001",
      "itemName": "Producto Ejemplo",
      "qtySold30d": 120.0,
      "qtySold90d": 350.0,
      "rotationStatus": "FAST",
      "onHandQty": null,
      "coverageDays": null
    }
  ]
}
```
**Nota:** `onHandQty`/`coverageDays` null hasta que OITW sea extraído.

**rotationStatus values:** FAST | NORMAL | SLOW | NO_MOVEMENT | NO_STOCK_DATA | UNKNOWN

### GET /api/client/bi/inventory/stock
**Query params:** `limit`, `offset`

**Response (paged):** vacío hasta que OITW sea extraído (Sprint 8F-ext)

### GET /api/client/bi/inventory/warehouses
**Response:** vacío hasta que OWTR sea extraído (Sprint 8F-ext)

---

## PURCHASING — /api/client/bi/purchasing/

> Todos los endpoints de Purchasing retornan vacío hasta que OPOR/OPDN sean extraídos (Sprint 8F-ext).

### GET /api/client/bi/purchasing/executive
**Query params:** `days` (1-365)

### GET /api/client/bi/purchasing/suppliers
**Query params:** `limit`, `offset`

### GET /api/client/bi/purchasing/receiving
**Query params:** `limit`, `offset`

---

## OPERATIONS — /api/client/bi/operations/

### GET /api/client/bi/operations/pipeline-health
**Response:**
```json
{
  "data": {
    "lastExtractorRunUtc": "2026-06-10T17:05:59Z",
    "lastTransformRunUtc": null,
    "extractorStatus": "SUCCESS",
    "transformStatus": "NEVER_RUN",
    "activeAlerts": 0,
    "dqErrorsUnresolved": 0,
    "objectsExtracted": 5,
    "healthScore": 70,
    "updatedAtUtc": "2026-06-10T17:10:00Z"
  }
}
```
Retorna `null` en `data` si no hay run previo.

### GET /api/client/bi/operations/alerts
**Query params:** `limit` (1-100, def 20), `offset`

**Response (paged):** alertas activas no resueltas

### GET /api/client/bi/operations/data-quality
**Query params:** `limit` (1-100, def 20), `offset`

**Response (paged):** issues de calidad no resueltos

---

## Códigos de error estándar

| HTTP | error_code | Cuando |
|------|-----------|--------|
| 400 | invalid_limit | limit fuera de rango |
| 400 | invalid_days | days fuera de rango |
| 400 | invalid_process_code | processCode vacío |
| 401 | unauthorized | Sin JWT o JWT inválido |
| 403 | forbidden | Company context no resuelto |

---

## Comportamiento con tablas vacías

Si la tabla MART subyacente no tiene datos:
- Endpoints paginados retornan `{ "data": [], "meta": { count: 0, hasMore: false } }`
- Endpoints de lista retornan `{ "data": [] }`
- Pipeline health retorna `{ "data": null }` si no hay registro en `ops.pipeline_health`
- **Nunca retornan 500** por tabla vacía
