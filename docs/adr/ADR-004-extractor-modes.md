# ADR-004 — Modos de Extracción: Dedicated Extractor (A) + Service Layer Delta (B)

**Fecha:** 2026-05-29  
**Estado:** Aceptado  
**Autor:** Chief Architect  

---

## Contexto

SAP Business One tiene múltiples modalidades de instalación (HANA on-prem, SQL Server on-prem, cloud con partner) y distintos niveles de acceso que se pueden otorgar. No existe un único método de extracción que funcione para todos los escenarios.

Este ADR confirma el modelo dual de extracción y define cuándo usar cada modalidad.

---

## Decisión

**Mantener dos modalidades coexistentes. Cada cliente usa una u otra.**

### Modalidad A — Dedicated Extractor

**Cuándo:**
- SAP B1 HANA on-prem con acceso al puerto 30015
- SAP B1 SQL Server on-prem con acceso ODBC
- Cliente con IT propio que puede instalar un servicio Windows
- Volumen alto (> 500k transacciones/año)

**Implementación:**
- .NET 8 Worker Service instalado como Windows Service en infra del cliente
- Conexión directa HANA SQL / ODBC
- Cola offline SQLite local (resiliencia ante fallas de red)
- Watermark por tabla + lookback adaptativo
- Heartbeat a `/api/ingest/heartbeat` cada 5 minutos

**Contrato de API:**
```
POST /api/ingest/{company}/{table}
X-DataBision-ApiKey: {key}
Content-Type: application/json
Content-Encoding: gzip
Body: [{ "DocEntry": 123, ... }, ...]
```

### Modalidad B — Service Layer Delta

**Cuándo:**
- SAP B1 en partner cloud (sin acceso a puerto de BD)
- Service Layer habilitado (puerto 50000)
- Cliente no puede instalar servicios en su infra
- Volumen bajo-medio (< 200k transacciones/año)

**Implementación:**
- Azure Function TimerTrigger (en cloud DataBision, no en infra del cliente)
- Pull via OData REST de Service Layer
- Cola en SAP B1: UDT `@DBI_SYNC_QUEUE` + FMS triggers
- Procesa en orden `U_CreatedAt`, marca procesados, maneja reintentos

**Limitación conocida:** Service Layer es lento (~100-300 req/min). No apto para carga inicial masiva. Estrategia para carga histórica: CSV inicial manual + drenaje de cola durante días.

---

## Por qué no un tercer modo (polling directo)

Se descartó un modo C de polling directo via Service Layer sin cola SAP porque:
- Sin cola en SAP, DataBision debe hacer polling completo de cada tabla
- Con `$filter=UpdateDate gt 'watermark'`, Service Layer falla en tablas grandes
- La cola en SAP (Modalidad B) es más eficiente: solo cambios reales
- Si el cliente no puede crear UDT, se acepta polling diferencial como último recurso (no es un modo oficial)

---

## Contrato Común: Ingest API

Ambas modalidades usan el mismo contrato de Ingest API. La API no distingue de dónde viene el batch.

```
POST /api/ingest/{company}/{table}
Headers:
  X-DataBision-ApiKey: {key por tenant}
  Content-Type: application/json
  Content-Encoding: gzip (recomendado)

Body: Array de objetos JSON con campos del objeto SAP
Response: { "data": { "inserted": N, "updated": M, "skipped": K } }
```

La API:
1. Valida API key → resuelve `company_id`
2. Calcula `source_hash_hex` para cada fila
3. Upsert idempotente en `raw.sap_{table}`
4. Actualiza `ctl.ingest_checkpoint`
5. Retorna conteo de INSERT/UPDATE/SKIP

---

## Criterio de Selección de Modalidad por Cliente

```
¿El cliente tiene SAP B1 HANA?
  ├── Sí → ¿Se puede instalar agente en servidor del cliente?
  │         ├── Sí → Modalidad A (HANA SQL directo)
  │         └── No → ¿Service Layer habilitado?
  │                    ├── Sí → Modalidad B
  │                    └── No → Bloquear onboarding, escalar
  └── No (SQL Server) → ¿Se puede instalar agente?
              ├── Sí → Modalidad A (ODBC)
              └── No → ¿Service Layer habilitado?
                         ├── Sí → Modalidad B
                         └── No → Bloquear onboarding
```

**Regla práctica:** siempre tener Modalidad B como plan B. Si hay duda, empezar con B mientras se gestiona el acceso para A.

---

## Documentos de Referencia

- `dedicated-extractor-design.md` — Especificación técnica completa Modalidad A
- `cloud-connector-queue-mode-design.md` — Especificación técnica completa Modalidad B
- `databision-product-architecture.md` — Secciones 4-8: topología, componentes, pitfalls SAP B1
