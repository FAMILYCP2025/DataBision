# Service Layer Pagination Strategy

**Sprint:** 8B  
**Fecha:** 2026-06-10

---

## Problema

El patrón anterior de extracción obtenía una sola página por ejecución (`$top={PageSize}&$skip=0`). Para clientes con catálogos grandes (OITM >5k artículos, OCRD >2k clientes) esto deja registros sin extraer y el checkpoint nunca avanza más allá de los primeros N registros.

---

## Solución: ServiceLayerPaginator

`ServiceLayerPaginator` (en `src/DataBision.Extractor/ServiceLayer/`) es un componente reutilizable que pagina automáticamente a través de todas las páginas de un endpoint SAP Service Layer.

### Contrato

```csharp
Task<PaginationResult> PaginateAsync(
    string sapObject,   // "OINV" — para logging
    string entity,      // "Invoices" — endpoint SL
    string baseQuery,   // "$select=...&$filter=...&$orderby=..." — SIN $top/$skip
    int pageSize,       // filas por página
    int maxPages,       // límite de seguridad
    CancellationToken ct)
```

### Algoritmo

```
query = "$top={pageSize}&$skip=0&{baseQuery}"
skip  = 0

while pageNumber < maxPages:
    (rows, nextLink) = GetPage(entity, query)
    allRows += rows
    skip    += rows.Count
    
    if nextLink:
        query = ExtractQuery(nextLink)   # usa @odata.nextLink
    elif rows.Count < pageSize:
        break                            # última página
    else:
        query = "$top={pageSize}&$skip={skip}&{baseQuery}"
```

### Soporte dual: nextLink + skip

1. **@odata.nextLink**: Si SAP lo devuelve, el paginador extrae el query string del URL y lo usa directamente (más confiable, preserva filtros del servidor).
2. **$top/$skip**: Fallback cuando no hay nextLink. Incrementa skip por `rows.Count` en cada página.

### Retry por página

Cada página se reintenta hasta 2 veces ante errores transitorios (`HttpRequestException`, timeout). Delay configurable (default: 2 segundos). En tests: delay = `Task.CompletedTask` vía `delayFactory`.

### Safety cap (MaxPages)

`ExtractorOptions.MaxPages = 500` (default). Con `PageSize=100` cubre hasta 50,000 filas por objeto por run. Si se alcanza, `PaginationResult.HitMaxPages = true`. El checkpoint solo avanza sobre las filas efectivamente procesadas.

---

## Integración con Jobs

Cada job refactorizado sigue este patrón:

```csharp
// En el job:
var baseQuery = $"$select={FullSelect}{filterPart}&$orderby=UpdateDate asc";
var result = await _paginator.PaginateAsync(SapObject, "Invoices", baseQuery,
    _options.PageSize, _options.MaxPages, ct);

// Fallback a MinimalSelect si primer error contiene "400" o "invalid"
if (result.LastError?.Contains("400") == true || ...)
    result = await _paginator.PaginateAsync(...MinimalSelect...);
```

### Jobs refactorizados (Sprint 8B)

| Job | Estrategia | Fallback select |
|-----|-----------|-----------------|
| OCRD | incremental UpdateDate + multi-page | ✅ MinimalSelect |
| OITM | incremental UpdateDate + multi-page | ✅ MinimalSelect |
| OSLP | full-refresh + multi-page | ❌ (no aplica) |
| OINV | incremental UpdateDate + multi-page | ✅ MinimalSelect |
| ORIN | incremental + capped initial (top=10) + multi-page | ✅ MinimalSelect |

### Jobs NO refactorizados (sin cambios)

| Job | Razón |
|-----|-------|
| INV1 | Usa DocumentLines embedded — paginator no aplica |
| RIN1 | Usa DocumentLines embedded — paginator no aplica |

---

## Modelos

```csharp
record PaginationPageLog(
    string SapObject, int PageNumber, int Skip, int Top,
    int RowsReceived, long ElapsedMs, string Status,
    string? ErrorCode, string? ErrorMessage);

record PaginationResult(
    JsonArray AllRows, List<PaginationPageLog> Logs,
    bool HitMaxPages, string? LastError);
```

---

## Registro DI

```csharp
services.AddSingleton<ServiceLayerPaginator>();
```

`ILogger<ServiceLayerPaginator>` se resuelve automáticamente. El parámetro opcional `delayFactory` usa el default (2s delay) en producción.

---

## Tests unitarios

6 casos en `tests/DataBision.Extractor.Tests/ServiceLayer/ServiceLayerPaginatorTests.cs`:

1. **Single page** — menos filas que pageSize → 1 página, termina.
2. **Multi-page skip** — paginación por $skip cuando no hay nextLink.
3. **NextLink** — usa @odata.nextLink para construir query de siguiente página.
4. **MaxPages cap** — detiene cuando se alcanza el límite.
5. **Retry transient** — falla una vez con `HttpRequestException`, reintenta y tiene éxito.
6. **Permanent error** — falla permanentemente, `LastError` registrado, retorna 0 filas.
