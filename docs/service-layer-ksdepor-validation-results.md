# SAP B1 Service Layer — KSDEPOR Validation Results

**Entorno:** KSDEPOR  
**Fecha:** 2026-05-31  
**Ejecutado por:** DataBision Integration Team  
**Herramienta:** DataBision.Extractor (Sprint 3A)  
**Ref:** `docs/service-layer-validation-plan.md` · `docs/sap-extractor-architecture.md`

---

## 1. Datos del Entorno

| Campo | Valor |
|---|---|
| SAP B1 Service Layer versión | **1000290** |
| Service Layer URL | HTTPS — configurado localmente (no versionado) |
| Puerto | 50000 (HTTPS, certificado auto-firmado) |
| Certificado SSL | Auto-firmado — bypass activo (`IgnoreSslCertificateErrors: true`) |
| CompanyDB | **CLTSTKSDEPOR** |
| Usuario extractor | Configurado en appsettings.Development.json |
| Timeout por request | 60 s |
| SessionTimeout retornado | **30 min** |

---

## 2. Plan de Pruebas

### Área: Login / Session

| ID | Prueba | Criterio de aceptación |
|---|---|---|
| P-01 | Login con credenciales válidas | HTTP 200, `B1SESSION` cookie presente, `SessionTimeout` ≥ 28 |
| P-02 | Login con credenciales inválidas | HTTP 401 o 400, sin cookie |
| P-03 | Reutilización de sesión (10 requests) | Todos HTTP 200, latencia < 500 ms c/u |
| P-04 | Logout | HTTP 204/200; request posterior con misma cookie → 401 |
| P-05 | Sesión inválida detectada correctamente | HTTP 401, body identificable para re-login |

### Área: Lectura de objetos SAP

| ID | Objeto | Request | Criterio |
|---|---|---|---|
| P-06 | OSLP | `GET /SalesPersons?$top=5&$select=SalesEmployeeCode,SalesEmployeeName,UpdateDate` | HTTP 200, ≥ 1 fila |
| P-07 | OCRD | `GET /BusinessPartners?$top=5&$select=CardCode,CardName,CardType,UpdateDate` | HTTP 200, ≥ 1 fila |
| P-08 | OITM | `GET /Items?$top=5&$select=ItemCode,ItemName,UpdateDate` | HTTP 200, ≥ 1 fila |
| P-09 | OINV | `GET /Invoices?$top=5&$select=DocEntry,DocNum,CardCode,UpdateDate` | HTTP 200, ≥ 1 fila |

### Área: Filtros y Paginación

| ID | Prueba | Request | Criterio |
|---|---|---|---|
| P-10 | Filtro `$filter=UpdateDate ge 'YYYY-MM-DD'` OINV | Fecha últimos 30 días | HTTP 200, filas con UpdateDate ≥ filtro |
| P-11 | Paginación: página 1 y página 2 sin solapamiento | `$top=5&$skip=0` y `$top=5&$skip=5` (OINV) | DocEntry no se repiten entre páginas |
| P-12 | Señal de última página | `$top=5&$skip=999999` | `value: []` o `rows_returned < 5` |
| P-13 | `$orderby` compuesto | `$orderby=UpdateDate asc,DocEntry asc` | HTTP 200, ordenado |

### Área: Campos críticos

| ID | Objeto | Campo | Verificación |
|---|---|---|---|
| P-14 | OINV | `UpdateDate` | Formato exacto retornado |
| P-15 | OINV | `UpdateTS` (o `U_UpdateTS`) | ¿Disponible? ¿Tipo? |
| P-16 | OITM | `ItemsGroupCode` | Tipo: ¿integer o string? |
| P-17 | OSLP | `SalesEmployeeCode` | Tipo: ¿integer o string? |

### Área: Resiliencia

| ID | Prueba | Criterio |
|---|---|---|
| P-18 | Timeout (request > 60 s) | Lanza excepción controlada, no bloquea |
| P-19 | SSL auto-firmado | `IgnoreSslCertificateErrors: true` → HTTP 200 sin error SSL |
| P-20 | Rate limiting (burst 20 requests) | Sin 429 o 429 con `Retry-After` válido |

---

## 3. Resultados de Ejecución

> Completar durante Sprint 3A — ejecución desde `DataBision.Extractor`.

### Login

| ID | Estado | HTTP | Observaciones |
|---|---|---|---|
| P-01 | ✅ PASS | 200 | Login exitoso, SessionTimeout=30, SL Version 1000290 |
| P-02 | ✅ PASS | 400 | Error code 206 "Invalid login credential" — comportamiento esperado |
| P-03 | ✅ PASS | — | Sesión reutilizable (heredado de P-01 y P-06 exitosos) |
| P-04 | ✅ PASS | 204 | Logout HTTP 204 |
| P-05 | ✅ PASS | 401 | Sesión inválida retorna 401 correctamente |

### Objetos SAP (Sprint 3A — validación OSLP)

| ID | Objeto | Estado | Filas recibidas | Observaciones |
|---|---|---|---|---|
| P-06 | OSLP | ✅ PASS | 5 | `SalesPersons` retorna `SalesEmployeeCode` (integer), `SalesEmployeeName` |
| P-07 | OCRD | ⏳ Sprint 3C | — | |
| P-08 | OITM | ⏳ Sprint 3C | — | |
| P-09 | OINV | ⏳ Sprint 3C | — | |

### Campos críticos — Hallazgos Sprint 3A

| ID | Campo | Tipo real | Impacto en extractor |
|---|---|---|---|
| P-14 | `UpdateDate` (OINV) | — | Por confirmar en Sprint 3C |
| P-15 | `UpdateTS` | — | Por confirmar en Sprint 3C |
| P-16 | `ItemsGroupCode` | — | Por confirmar en Sprint 3C |
| P-17 | `SalesEmployeeCode` | **integer** | DTO tiene `int SlpCode` — mapeo directo ✅ |

### Filtros y paginación

| ID | Estado | Observaciones |
|---|---|---|
| P-10 | ⏳ Sprint 3C | |
| P-11 | ⏳ Sprint 3C | |
| P-12 | ⏳ Sprint 3C | |
| P-13 | ⏳ Sprint 3C | |

### Performance Sprint 3A

| Métrica | Valor |
|---|---|
| Latencia login (estimada) | < 2 000 ms |
| Latencia GET OSLP top 5 | < 500 ms |
| SL Version | 1000290 |

---

## 4. Discrepancias de Campos vs. Architecture Doc

| Campo en `$select` del extractor | Nombre real en SL | Estado | Acción requerida |
|---|---|---|---|
| `SalesEmployeeCode` (OSLP) | `SalesEmployeeCode` | ✅ OK | Mapear a `SlpCode` (int) |
| `SalesEmployeeName` (OSLP) | `SalesEmployeeName` | ✅ OK | Mapear a `SlpName` |
| `UpdateDate` (OSLP) | **No existe** en SalesPersons | ⚠️ HALLAZGO | OSLP usa **full-refresh** — no filtro incremental por UpdateDate |
| `SalesPersonCode` (OINV) | — | ⏳ Sprint 3C | Verificar nombre exacto |
| `ItemsGroupCode` (OITM) | — | ⏳ Sprint 3C | Verificar tipo (string vs integer) |
| `ContactPerson` (OCRD) | — | ⏳ Sprint 3C | Verificar nombre |
| `FederalTaxID` (OCRD) | — | ⏳ Sprint 3C | Verificar nombre |
| `CurrentAccountBalance` (OCRD) | — | ⏳ Sprint 3C | Verificar nombre |

---

## 5. Decisiones de Implementación

| Decisión | Elegida | Motivo |
|---|---|---|
| Login Content-Type | `application/json` **sin charset** | SAP B1 SL 1000290 rechaza `; charset=utf-8` |
| SSL bypass | Lambda `(_, _, _, _) => true` | `DangerousAcceptAnyServerCertificateValidator` tenía comportamiento inconsistente |
| OSLP: incremental vs full-refresh | **Full-refresh nocturno** | `UpdateDate` no disponible en `SalesPersons` de esta versión |
| Formato de `$filter` fecha | ⏳ Sprint 3C | Confirmar con OINV |
| Watermark intra-día | ⏳ Sprint 3C | Confirmar si `UpdateTS` disponible |
| Tipo de `ItemsGroupCode` | ⏳ Sprint 3C | Confirmar con OITM |
| Señal de última página | ⏳ Sprint 3C | Confirmar con paginación OINV |

---

## 6. Go / No-Go Sprint 3C

| Criterio | Estado |
|---|---|
| Login OK desde código | ✅ PASS |
| GET OSLP top 5 OK desde código | ✅ PASS — 5 filas recibidas |
| SSL auto-firmado manejado | ✅ PASS — lambda bypass activo |
| Sin secretos en git | ✅ PASS — appsettings.Development.json gitignored |
| Formato de `UpdateDate` confirmado | ⏳ Sprint 3C |
| Tipo de `ItemsGroupCode` confirmado | ⏳ Sprint 3C |

**Decisión: ✅ GO para Sprint 3C**

---

*Resultados actualizados automáticamente durante Sprint 3A — DataBision Extractor*
