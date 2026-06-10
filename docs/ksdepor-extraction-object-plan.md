# KSDEPOR — Plan de Extracción de Objetos SAP

**Sprint:** 8B  
**Fecha:** 2026-06-10  
**Cliente:** KSDEPOR (ambiente de pruebas)

---

## Estado actual de objetos

### Activos (extraídos hoy)

| Objeto | Endpoint SL | Proceso | Estrategia | Job |
|--------|------------|---------|-----------|-----|
| OINV | Invoices | SALES | Incremental UpdateDate + multi-page | OinvExtractorJob |
| ORIN | CreditNotes | SALES | Incremental UpdateDate + capped inicial | OrinExtractorJob |
| INV1 | Invoices (DocumentLines) | SALES | DocumentLines embedded | Inv1ExtractorJob |
| RIN1 | CreditNotes (DocumentLines) | SALES | DocumentLines embedded | Rin1ExtractorJob |
| OCRD | BusinessPartners | SALES | Incremental UpdateDate + multi-page | OcrdExtractorJob |
| OITM | Items | SALES | Incremental UpdateDate + multi-page | OitmExtractorJob |
| OSLP | SalesPersons | SALES | Full-refresh + multi-page | OslpExtractorJob |

### Preparados (código SapObjectRegistry, sin job activo)

| Objeto | Endpoint SL | Proceso | MART dependiente | Sprint activación |
|--------|------------|---------|-----------------|------------------|
| ORDR | Orders | SALES | mart.sales_fulfillment_dashboard | Sprint 8F |
| RDR1 | (Orders/DocumentLines) | SALES | mart.sales_fulfillment_dashboard | Sprint 8F |
| ODLN | DeliveryNotes | SALES | mart.sales_fulfillment_dashboard | Sprint 8F |
| DLN1 | (DeliveryNotes/DocumentLines) | SALES | mart.sales_fulfillment_dashboard | Sprint 8F |
| OPOR | PurchaseOrders | PURCHASING | mart.purchase_executive_daily | Sprint 8F |
| POR1 | (PurchaseOrders/DocumentLines) | PURCHASING | mart.purchase_executive_daily | Sprint 8F |
| OPDN | PurchaseDeliveryNotes | PURCHASING | mart.purchase_receiving_dashboard | Sprint 8F |
| PDN1 | (PurchaseDeliveryNotes/lines) | PURCHASING | mart.purchase_receiving_dashboard | Sprint 8F |
| OPCH | PurchaseInvoices | PURCHASING/FINANCE | mart.finance_ap_aging_dashboard | Sprint 8F |
| PCH1 | (PurchaseInvoices/lines) | PURCHASING/FINANCE | mart.finance_ap_aging_dashboard | Sprint 8F |
| OITW | ItemWarehouseInfoCollection | INVENTORY | mart.inventory_stock_dashboard | Sprint 8F |
| OWHS | Warehouses | INVENTORY | mart.inventory_warehouse_dashboard | Sprint 8F |
| OWTR | StockTransfers | INVENTORY | mart.inventory_warehouse_dashboard | Sprint 8F |
| WTR1 | (StockTransfers/lines) | INVENTORY | mart.inventory_warehouse_dashboard | Sprint 8F |

---

## Volumen estimado KSDEPOR

| Objeto | Estimado filas | Páginas @100 | Tiempo estimado |
|--------|--------------|-------------|----------------|
| OCRD | ~500–2000 | 5–20 | < 30s |
| OITM | ~1000–5000 | 10–50 | < 60s |
| OSLP | ~10–50 | 1 | < 5s |
| OINV | ~5000–20000 | 50–200 | 2–10 min |
| ORIN | ~100–500 | 1–5 | < 30s |

> **MaxPages=500** cubre hasta 50,000 filas. Si KSDEPOR supera este límite en algún objeto, aumentar `Extractor:MaxPages` en appsettings.Development.json.

---

## Pasos para activar objeto preparado

1. Crear extractor job (seguir patrón OinvExtractorJob)
2. Registrar en `Program.cs` (service mode + CLI mode)
3. Agregar al `ExtractorRunner.SupportedObjects` y `AllObjects` si debe incluirse en `--run-once`
4. Crear migración STG (si la tabla no existe)
5. Actualizar `cfg.sap_object_catalog` (is_active = TRUE)
6. Verificar que el MART correspondiente sea defensivo y funcione con datos parciales

---

## Configuración recomendada KSDEPOR (appsettings.Development.json)

```json
"Extractor": {
  "TenantId": "ksdepor",
  "CompanyId": "<id-confirmado>",
  "Mode": "INCREMENTAL",
  "PageSize": 100,
  "MaxPages": 500,
  "Objects": ["OCRD", "OITM", "OSLP", "OINV", "ORIN"],
  "SendEnabled": true,
  "IntervalMinutes": 30
}
```

> **IMPORTANTE:** No activar objetos preparados (OPOR, OPDN, OITW, etc.) hasta confirmar con KSDEPOR que esas tablas tienen datos y el equipo está listo para recibirlos.
