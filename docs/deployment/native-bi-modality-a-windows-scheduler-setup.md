# Native BI Modalidad A — Configuración del Scheduler Windows (Producción)

**DataBision · Junio 2026**  
**Versión:** 1.0 — Gate 2 pre-deployment Modalidad A  
**Aplica a:** Servidores Windows Server 2019/2022 y Windows 10/11 Pro

---

## Decisión de diseño: one-shot, no proceso permanente

El extractor **no corre como proceso permanente**. Cada ejecución:
1. Inicia
2. Extrae y envía datos
3. Registra resultado con exit code
4. Termina

El Windows Task Scheduler se encarga de iniciar el proceso en el horario configurado. Si una ejecución falla, la siguiente ejecución programada la reintenta automáticamente. Esta arquitectura elimina el riesgo de procesos zombie y simplifica la rotación de credenciales (cada inicio lee las env vars frescas).

---

## Prerequisitos

- .NET 8 Runtime instalado (`dotnet --version` → 8.x.x)
- `DataBision.Extractor.dll` publicado en un directorio local (e.g., `C:\DataBision\Extractor\`)
- Variables de entorno configuradas en el sistema (o en el script PS1)
- `ASPNETCORE_ENVIRONMENT=Production` configurada en el sistema
- Perfil de conexión configurado en el panel Admin con `IgnoreSslErrors=false`

---

## Scripts PowerShell

### run-oact.ps1 — Extracción semanal del plan de cuentas

```powershell
# C:\DataBision\Scripts\run-oact.ps1
# Ejecuta extracción OACT (full-refresh, solo lectura)

$logDir  = "C:\DataBision\Logs"
$logFile = "$logDir\oact-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$lockFile = "C:\DataBision\Locks\oact.lock"
if (Test-Path $lockFile) {
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] SKIP: Lock file exists. Previous run still active or crashed."
    exit 1
}

New-Item -Force $lockFile | Out-Null

try {
    $env:ASPNETCORE_ENVIRONMENT = "Production"

    $result = & dotnet "C:\DataBision\Extractor\DataBision.Extractor.dll" `
        --profile ksdepor-prd `
        --object OACT `
        --run-once --send `
        2>&1 | Tee-Object $logFile

    $exitCode = $LASTEXITCODE
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] EXIT_CODE=$exitCode"

    if ($exitCode -ne 0) {
        Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] ERROR: OACT extraction failed. Manual review required."
    }
}
finally {
    Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
}
```

### run-ojdt.ps1 — Extracción diaria del libro diario

```powershell
# C:\DataBision\Scripts\run-ojdt.ps1
# Ejecuta extracción OJDT incremental (por ReferenceDate watermark)

$logDir  = "C:\DataBision\Logs"
$logFile = "$logDir\ojdt-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$lockFile = "C:\DataBision\Locks\ojdt.lock"
if (Test-Path $lockFile) {
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] SKIP: Lock file exists. Previous run still active or crashed."
    exit 1
}

New-Item -Force $lockFile | Out-Null

try {
    $env:ASPNETCORE_ENVIRONMENT = "Production"

    $result = & dotnet "C:\DataBision\Extractor\DataBision.Extractor.dll" `
        --profile ksdepor-prd `
        --object OJDT `
        --run-once --send `
        2>&1 | Tee-Object $logFile

    $exitCode = $LASTEXITCODE
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] EXIT_CODE=$exitCode"

    if ($exitCode -ne 0) {
        Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] ERROR: OJDT extraction failed. Manual review required."
    }
}
finally {
    Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
}
```

### run-mart.ps1 — Refresh diario del MART financiero

```powershell
# C:\DataBision\Scripts\run-mart.ps1
# Ejecuta STG → MART refresh (agrega calculos al staging)

$logDir  = "C:\DataBision\Logs"
$logFile = "$logDir\mart-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$lockFile = "C:\DataBision\Locks\mart.lock"
if (Test-Path $lockFile) {
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] SKIP: Lock file exists. Previous run still active or crashed."
    exit 1
}

New-Item -Force $lockFile | Out-Null

try {
    $env:ASPNETCORE_ENVIRONMENT = "Production"

    $result = & dotnet "C:\DataBision\Extractor\DataBision.Extractor.dll" `
        --transform --include-mart `
        2>&1 | Tee-Object $logFile

    $exitCode = $LASTEXITCODE
    Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] EXIT_CODE=$exitCode"

    if ($exitCode -ne 0) {
        Add-Content $logFile "[$(Get-Date -Format 'HH:mm:ss')] ERROR: MART refresh failed. Dashboard may show stale data."
    }
}
finally {
    Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
}
```

---

## Registro de tareas en Task Scheduler

Ejecutar como Administrador en PowerShell:

```powershell
# Crear directorio de locks
New-Item -ItemType Directory -Force -Path "C:\DataBision\Locks" | Out-Null

# OACT — semanal, domingo 1:00 AM
Register-ScheduledTask -TaskName "DataBision-OACT" `
    -Description "DataBision: extracción semanal OACT (plan de cuentas SAP)" `
    -Action (New-ScheduledTaskAction `
        -Execute "powershell.exe" `
        -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\Scripts\run-oact.ps1") `
    -Trigger (New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At "01:00AM") `
    -Settings (New-ScheduledTaskSettingsSet `
        -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
        -RestartCount 1 `
        -RestartInterval (New-TimeSpan -Minutes 30) `
        -MultipleInstances IgnoreNew) `
    -RunLevel Highest `
    -Force

# OJDT — diario, 2:00 AM
Register-ScheduledTask -TaskName "DataBision-OJDT" `
    -Description "DataBision: extracción diaria OJDT (libro diario SAP)" `
    -Action (New-ScheduledTaskAction `
        -Execute "powershell.exe" `
        -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\Scripts\run-ojdt.ps1") `
    -Trigger (New-ScheduledTaskTrigger -Daily -At "02:00AM") `
    -Settings (New-ScheduledTaskSettingsSet `
        -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
        -RestartCount 2 `
        -RestartInterval (New-TimeSpan -Minutes 15) `
        -MultipleInstances IgnoreNew) `
    -RunLevel Highest `
    -Force

# MART — diario, 4:00 AM (después de OJDT)
Register-ScheduledTask -TaskName "DataBision-MART" `
    -Description "DataBision: refresh diario MART financiero" `
    -Action (New-ScheduledTaskAction `
        -Execute "powershell.exe" `
        -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\Scripts\run-mart.ps1") `
    -Trigger (New-ScheduledTaskTrigger -Daily -At "04:00AM") `
    -Settings (New-ScheduledTaskSettingsSet `
        -ExecutionTimeLimit (New-TimeSpan -Hours 1) `
        -RestartCount 2 `
        -RestartInterval (New-TimeSpan -Minutes 10) `
        -MultipleInstances IgnoreNew) `
    -RunLevel Highest `
    -Force
```

---

## Verificación post-registro

```powershell
# Listar tareas DataBision
Get-ScheduledTask -TaskName "DataBision-*" | Select-Object TaskName, State, LastRunTime, LastTaskResult

# Ejecutar manualmente para verificar
Start-ScheduledTask -TaskName "DataBision-OJDT"
Start-Sleep -Seconds 60
Get-ScheduledTask -TaskName "DataBision-OJDT" | Select-Object LastRunTime, LastTaskResult
# LastTaskResult = 0 → éxito
```

---

## Interpretación de LastTaskResult

| Código | Significado | Acción |
|---|---|---|
| 0 | Éxito | Normal |
| 1 | Lock file existía — skip (ejecución anterior activa) | Verificar logs. Si el lock es huérfano, eliminar `C:\DataBision\Locks\*.lock` |
| 2 | Error de configuración (env var, API key, perfil) | Revisar env vars y `appsettings.json` |
| 3 | Error de negocio (SAP no accesible, credenciales) | Revisar logs. Verificar estado SAP Service Layer |
| 267011 | PowerShell script no encontrado | Verificar ruta del script |
| 2147942402 | Archivo no encontrado | Verificar ruta del Extractor |

---

## Retención de logs

Los logs se crean por fecha. Para limpiar logs con más de 30 días:

```powershell
# Agregar como tarea semanal adicional (DataBision-LogCleanup)
Get-ChildItem "C:\DataBision\Logs" -Filter "*.log" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item
```

---

## Criterio GO Gate 2 (Windows)

| Criterio | Cómo verificar |
|---|---|
| Las 3 tareas aparecen en Task Scheduler | `Get-ScheduledTask -TaskName "DataBision-*"` |
| OJDT ejecuta sin error (LastTaskResult=0) | `Start-ScheduledTask -TaskName "DataBision-OJDT"` → esperar → verificar |
| Logs se crean en `C:\DataBision\Logs\` | Ver archivos ojdt-*.log |
| Lock files se eliminan post-ejecución | No deben existir `.lock` files 10 min después de la ejecución |
| Retry automático configurado | `RestartCount` y `RestartInterval` visibles en propiedades de tarea |
