# KSDEPOR — Resultados Extracción Controlada (Sprint 8F)

**Fecha:** 2026-06-10  
**Entorno:** SAP B1 KSDEPOR (CLTSTKSDEPOR) → DataBision API local → Supabase DEV/TST  
**Mode:** INCREMENTAL  
**SAP SL Version:** 1000290

---

## Tabla de resultados

| Objeto | PageSize | MaxPages | Páginas | Filas extraídas | Insertadas | Actualizadas | Skipped | Estado | Observación |
|--------|----------|----------|---------|----------------|------------|-------------|---------|--------|-------------|
| OSLP | 20 | 2 | 1 | 13 | 0 | 0 | 13 | ✅ OK | Full-refresh, datos ya actualizados |
| OCRD | 20 | 2 | 2 | 40 | 20 | 20 | 0 | ✅ OK | MaxPages hit (esperado, más datos disponibles) |
| OITM | 20 | 2 | 2 | 40 | 20 | 0 | 20 | ✅ OK | MaxPages hit (esperado, más datos disponibles) |
| OINV | 20 | 2 | 2 | 40 | 34 | 6 | 0 | ✅ OK | MaxPages hit (esperado, más datos disponibles) |
| ORIN | 10 | 2 | 2 | 12 | 3 | 7 | 2 | ✅ OK | 12 notas en ventana incremental |

**Total:** 145 filas extraídas, 77 insertadas, 33 actualizadas, 48 skipped. 0 errores.

---

## Checkpoints SAP observados

| Objeto | Watermark utilizado | Total ingested (checkpoint) |
|--------|---------------------|---------------------------|
| OSLP | Full-refresh (sin watermark) | — |
| OCRD | 2025-12-10 (lookback desde 2025-12-11) | 21 |
| OITM | 2025-12-10 (lookback desde 2025-12-11) | 21 |
| OINV | 2026-05-17 (lookback desde 2026-05-18) | 70 |
| ORIN | 2026-06-02 (lookback desde 2026-06-03) | 54 |

---

## Detalles por objeto

### OSLP — SalesPersons
- 13 vendedores en el sistema (incluyendo `-1 = Ningún empleado de ventas/comprador`)
- Código de vendedor conocido: 1=OFICINA-SOLIDEZ, 2=OFICINA-TPD
- No requirió paginación (< PageSize)

### OCRD — BusinessPartners
- Filtro incremental aplicado (`UpdateDate ge '2025-12-10'`)
- Mix de clientes y proveedores (CardType: cSupplier, cCustomer)
- MaxPages=2 cortó extracción — hay más socios disponibles
- Para extracción completa: subir MaxPages o PageSize en config

### OITM — Items
- Ítems con código patrón `NUMEROPEQUENO-X-Y` → datos de test KSDEPOR
- MaxPages=2 cortó extracción
- Encoding UTF-8 correcto en DB (display terminal mostraba Ã' por terminal encoding)

### OINV — Invoices
- Watermark reciente (2026-05-18) → facturas activas y recientes
- 34 facturas nuevas insertadas en este run
- DocTotal range: ~491 – 18,873

### ORIN — CreditNotes
- 12 notas en ventana 2026-06-02
- Paginación natural (no requirió MaxPages completo)

---

## Validación en Supabase (queries post-extracción)

```sql
-- Verificar conteos base
SELECT COUNT(*) FROM stg.sales_person WHERE company_id = '<company_id>';         -- esperado >= 13
SELECT COUNT(*) FROM stg.customer WHERE company_id = '<company_id>';              -- esperado >= 21+40
SELECT COUNT(*) FROM stg.item WHERE company_id = '<company_id>';                  -- esperado >= 21+20
SELECT COUNT(*) FROM stg.sales_invoice WHERE company_id = '<company_id>';         -- esperado >= 70+34
SELECT COUNT(*) FROM stg.credit_memo WHERE company_id = '<company_id>';           -- esperado >= 54+3
```

---

## Comando utilizado

```bash
# OSLP
dotnet run --project src/DataBision.Extractor -- --object OSLP --send --page-size 20 --max-pages 2

# OCRD
dotnet run --project src/DataBision.Extractor -- --object OCRD --send --page-size 20 --max-pages 2

# OITM
dotnet run --project src/DataBision.Extractor -- --object OITM --send --page-size 20 --max-pages 2

# OINV
dotnet run --project src/DataBision.Extractor -- --object OINV --send --page-size 20 --max-pages 2

# ORIN
dotnet run --project src/DataBision.Extractor -- --object ORIN --send --page-size 10 --max-pages 2
```

---

## Observaciones

- **MaxPages hit en OCRD/OITM/OINV** es esperado con limit=2. No es un error. Para run de producción usar MaxPages=500 (default) o sin límite bajo.
- **INV1/RIN1 pendientes**: DocumentLines embebidos, se prueban después de confirmar OINV/ORIN OK.
- **OSLP skipped=13**: datos ya actualizados en DB. Normal para full-refresh con datos sin cambios.
- **B1SESSION**: NO fue imprimido en ningún momento ✅

---

## Próximos pasos

- Sprint 8G: ejecutar MART refresh con datos reales
- Sprint 8F-ext: aumentar MaxPages/PageSize para obtener extracción completa de OCRD, OITM, OINV
- Sprint 8F-ext: probar INV1/RIN1 con volumen controlado
