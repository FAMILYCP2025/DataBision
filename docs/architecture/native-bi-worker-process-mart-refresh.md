# Native BI — Worker Process MART Refresh

**Sprint:** 23E  
**Fecha:** 2026-06-21

---

## Problema

`ExtractorWorkerService` en modo `--service` ya llamaba a `RefreshMartAsync` (finance MART) después de cada ciclo exitoso. Sin embargo, no llamaba a `RefreshProcessMartAsync` (dashboards de procesos). Esto significaba que los datos de los dashboards de procesos quedaban desactualizados hasta el próximo `--transform-mart` manual.

---

## Solución

### Nueva opción en ExtractorOptions

```csharp
public bool RunProcessMartRefreshAfterExtraction { get; init; } = false;
```

### Comportamiento en ExtractorWorkerService

```
Ciclo N:
  → Extracción (OACT + OJDT)
  
  Si RunMartRefreshAfterExtraction=true Y extracción exitosa:
    → TransformationRunner.RefreshMartAsync(companyId)   ← finance MART
    
    Si RefreshMartAsync exitoso Y RunProcessMartRefreshAfterExtraction=true:
      → TransformationRunner.RefreshProcessMartAsync(companyId)  ← process MART
    
  Si finance MART falla:
    → LogError
    → Process MART NO se ejecuta
    → Ciclo continúa
    
  Si process MART falla:
    → LogError
    → Finance MART intacto
    → Ciclo continúa
```

**Regla crítica:** Process MART solo corre si finance MART tuvo éxito. Esto evita poblar dashboards de procesos con datos de estado inconsistente.

---

## Configuración

```json
{
  "Extractor": {
    "RunMartRefreshAfterExtraction": true,
    "RunProcessMartRefreshAfterExtraction": true,
    "MartRefreshCompanyId": "company-client-001"
  }
}
```

---

## Tabla de casos

| RunMartRefresh | Finance MART | RunProcessMartRefresh | Process MART | Resultado |
|---|---|---|---|---|
| false | N/A | N/A | N/A | Solo extracción |
| true | exitoso | false | N/A | Finance MART actualizado |
| true | exitoso | true | exitoso | Ambos MART actualizados |
| true | exitoso | true | fallido | Finance OK, error en process (log) |
| true | fallido | true | N/A | Finance falló, process OMITIDO (log) |
| true | N/A | true | N/A | Sin StagingConnection: warning al inicio |

---

## Logs esperados (ciclo exitoso con ambos refreshes)

```
=== Finance MART refresh after cycle 1 — company=company-client-001 ===
=== Finance MART refresh done — 4 object(s) refreshed ===
=== Process MART refresh after cycle 1 — company=company-client-001 ===
=== Process MART refresh done — 3 object(s) refreshed ===
```

---

## Relación con --transform-mart CLI

El modo `--transform-mart` en CLI siempre ejecuta ambos (finance + process):

```bash
dotnet DataBision.Extractor.exe --transform-mart --company company-client-001
```

El worker service ahora tiene el mismo comportamiento cuando ambas flags están habilitadas.
