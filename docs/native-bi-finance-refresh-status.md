# Native BI Finance — Refresh Status Endpoint

**Sprint:** 21D  
**Fecha:** 2026-06-21

---

## Endpoint

```
GET /api/client/bi/finance/refresh-status
```

Parámetros: `?companyId=[slug]` (DEV) / subdomain (PROD)

Retorna el estado de la última ejecución del extractor y del pipeline MART para la empresa. Solo lectura — no dispara ninguna actualización.

---

## Response Shape

```json
{
  "data": {
    "lastOactExtraction": {
      "startedAt": "2026-06-20T06:01:12Z",
      "finishedAt": "2026-06-20T06:01:45Z",
      "status": "OK",
      "rowsExtracted": 20,
      "lastError": null
    },
    "lastOjdtExtraction": {
      "startedAt": "2026-06-20T06:01:45Z",
      "finishedAt": "2026-06-20T06:06:12Z",
      "status": "OK",
      "rowsExtracted": 50,
      "lastError": null
    },
    "lastMartRefresh": {
      "startedAt": "2026-06-20T06:06:15Z",
      "finishedAt": "2026-06-20T06:06:42Z",
      "status": "OK",
      "objectsRefreshed": 8,
      "lastError": null
    },
    "lastDataRefreshedAt": "2026-06-20T06:06:42Z",
    "overallStatus": "ok",
    "statusMessage": "Actualizado hace 3 h."
  }
}
```

---

## overallStatus values

| Valor | Condición |
|---|---|
| `ok` | Última ejecución OK, datos frescos (< 48h) |
| `warning` | Última ejecución OK pero datos > 48h, o MART sin refresh |
| `error` | Última extracción OACT u OJDT con status ERROR |
| `never_run` | No hay registros en `ops.extractor_run` |

---

## Fuentes de datos

| Campo | Tabla Supabase |
|---|---|
| `lastOactExtraction` | `ops.extractor_run` WHERE `sap_object = 'OACT'` |
| `lastOjdtExtraction` | `ops.extractor_run` WHERE `sap_object = 'OJDT'` |
| `lastMartRefresh` | `ops.transform_run` WHERE `transform_type = 'MART'` |
| `lastDataRefreshedAt` | `GREATEST(MAX(refreshed_at))` de `mart.income_statement_summary`, `mart.balance_sheet_summary`, `mart.ebitda_summary` |

---

## Frontend widget

El widget `FinanceRefreshStatusWidget` en `FinanceDashboardPage.tsx` muestra:

- Chip de estado (color según `overallStatus`)
- Mensaje de estado
- Hora de último refresh
- Botón "Ver detalle" → expande para mostrar:
  - OACT: status, filas extraídas, timestamp
  - OJDT: status, filas extraídas, timestamp
  - MART transform: status, pasos completados, timestamp

**No hay botón para ejecutar el refresh desde la UI.** El refresh debe ser iniciado por el scheduler o el consultor DataBision vía CLI.

---

## Limitación conocida

Si el extractor no tiene `OperationsLogger` configurado (sin `Staging:ConnectionString` en el extractor), las tablas `ops.extractor_run` y `ops.transform_run` estarán vacías. En ese caso:
- `overallStatus = "never_run"`  
- El widget muestra "Sin ejecuciones registradas"

El workaround: configurar `Staging:ConnectionString` en `appsettings.Development.json` del extractor para que se registren las ejecuciones.

Alternativa: `lastDataRefreshedAt` siempre tiene datos (viene de `mart.refreshed_at`) y es suficiente para mostrar cuándo se actualizaron los datos MART, independientemente de los logs de extracción.

---

## Archivos modificados (Sprint 21D)

| Archivo | Cambio |
|---|---|
| `src/DataBision.Application/DTOs/Dashboard/FinanceRefreshStatusDto.cs` | Nuevo DTO |
| `src/DataBision.Application/Interfaces/Dashboard/IProcessDashboardRepository.cs` | + `GetFinanceRefreshStatusAsync` |
| `src/DataBision.Application/Interfaces/Dashboard/IProcessDashboardService.cs` | + `GetFinanceRefreshStatusAsync` |
| `src/DataBision.Infrastructure/Repositories/Dashboard/ProcessDashboardRepository.cs` | Implementación Dapper |
| `src/DataBision.Application/Services/Dashboard/ProcessDashboardService.cs` | Delegación al repo |
| `src/DataBision.Api/Controllers/ClientBiFinanceController.cs` | + GET refresh-status |
| `databision-frontend/src/client/types/processBi.ts` | + `FinanceRefreshStatus` |
| `databision-frontend/src/client/api/processBiApi.ts` | + `getBiFinanceRefreshStatus` |
| `databision-frontend/src/client/hooks/useProcessBi.ts` | + `useBiFinanceRefreshStatus` |
| `databision-frontend/src/client/pages/FinanceDashboardPage.tsx` | + widget `FinanceRefreshStatusWidget` |
