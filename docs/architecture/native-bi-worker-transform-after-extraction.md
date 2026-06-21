# Native BI — Worker Transform After Extraction

**Sprint:** 22E  
**Fecha:** 2026-06-21

---

## Problema

El scheduler OS (Windows Task Scheduler / cron Linux) requiere dos pasos separados:
1. `dotnet DataBision.Extractor.exe --object OACT --send`
2. `dotnet DataBision.Extractor.exe --object OJDT --send`
3. `dotnet DataBision.Extractor.exe --transform-mart --company {id}`

En `--service` mode (Windows Service), los pasos 1 y 2 están integrados en el `ExtractorWorkerService`. El paso 3 no estaba integrado, lo que significaba que los datos del MART no se actualizaban automáticamente después de cada ciclo.

---

## Solución implementada

### ExtractorOptions (nuevas propiedades)

```csharp
public bool    RunMartRefreshAfterExtraction { get; init; } = false;
public string? MartRefreshCompanyId          { get; init; } = null;
```

### Comportamiento en ExtractorWorkerService

```
Ciclo N:
  → slClient.LoginAsync()
  → scheduler.RunCycleAsync() [OACT + OJDT]
  → slClient.LogoutAsync()
  
  Si RunMartRefreshAfterExtraction=true Y extracción exitosa:
    → TransformationRunner.RefreshMartAsync(companyId)
    → Log de resultado
    
  Si MART falla:
    → LogError (mensaje descriptivo)
    → Ciclo continúa (no detiene el worker)
```

**Regla:** Si la extracción falla, el MART refresh no se ejecuta. Los datos MART quedan con la última versión válida.

### Configuración en appsettings

```json
{
  "Extractor": {
    "TenantId": "tenant-client",
    "CompanyId": "company-client-001",
    "Objects": ["OACT", "OJDT"],
    "SendEnabled": true,
    "IntervalMinutes": 1440,
    "RunMartRefreshAfterExtraction": true,
    "MartRefreshCompanyId": "company-client-001"
  },
  "Staging": {
    "ConnectionString": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

Si `MartRefreshCompanyId` está vacío, usa `CompanyId` como fallback.

---

## Registro de DI (--service mode)

`TransformationRunner` se registra como singleton solo si `Staging:ConnectionString` está configurado:

```csharp
if (!string.IsNullOrWhiteSpace(svcStagingOpts.ConnectionString))
{
    services.AddSingleton<ITransformationRunner>(sp =>
        new TransformationRunner(
            svcStagingOpts.ConnectionString,
            sp.GetRequiredService<ILogger<TransformationRunner>>(),
            svcOpsLogger));
}
```

`ExtractorWorkerService` recibe `ITransformationRunner?` (nullable). Si no está registrado, la feature queda desactivada con un warning al startup.

---

## Casos de uso

| Escenario | RunMartRefreshAfterExtraction | Resultado |
|---|---|---|
| Scheduler OS con pasos separados | false (default) | Sin cambio de comportamiento |
| Windows Service modo automático | true | MART se refresca tras cada ciclo exitoso |
| Windows Service sin StagingConnection | true | Warning al inicio, MART refresh saltado silenciosamente |
| Extracción falla | true | MART refresh NO se ejecuta, último MART válido se mantiene |
| MART refresh falla | true | Error registrado, próximo ciclo de extracción se ejecuta igual |

---

## Limitaciones

- Solo refresca MART base (income_statement, balance_sheet, ebitda, gl_accounts)
- No llama a `RefreshProcessMartAsync` (dashboards de procesos) — backlog Sprint 23
- Timeout hardcodeado en 10 min para el refresh del MART
