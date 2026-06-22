# Native BI Finance — Windows Task Scheduler (Producción)

**Sprint 28 · DataBision · Junio 2026**

---

## Prerequisitos

- .NET 8 runtime instalado en el servidor
- Extractor publicado en: `C:\DataBision\Extractor\`
- Variables de entorno configuradas en el sistema (System environment variables)
- Usuario de servicio creado: `DATABISION_SVC` (sin login interactivo)
- Directorio de logs: `C:\DataBision\logs\`

---

## Estructura de directorios

```
C:\DataBision\
├── Extractor\
│   ├── DataBision.Extractor.exe
│   ├── appsettings.json
│   └── profiles\
│       └── [cliente]-profile.json
├── logs\
│   ├── oact-2026-06-22.log
│   ├── ojdt-2026-06-22.log
│   └── mart-2026-06-22.log
└── scripts\
    ├── run-oact.ps1
    ├── run-ojdt.ps1
    └── run-mart.ps1
```

---

## Scripts PowerShell de ejecución

### `C:\DataBision\scripts\run-oact.ps1`

```powershell
$date = Get-Date -Format "yyyy-MM-dd"
$logFile = "C:\DataBision\logs\oact-$date.log"
$profile = "KSDEPOR"  # Reemplazar por slug del cliente

"[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Iniciando extraccion OACT - Perfil: $profile" | Tee-Object -FilePath $logFile

try {
    & "C:\DataBision\Extractor\DataBision.Extractor.exe" `
        --profile $profile `
        --object OACT `
        2>&1 | Tee-Object -FilePath $logFile -Append

    $exitCode = $LASTEXITCODE
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] OACT completado. Exit code: $exitCode" | Add-Content $logFile

    if ($exitCode -ne 0) {
        "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: OACT fallo con exit code $exitCode" | Add-Content $logFile
        exit 1
    }
} catch {
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] EXCEPCION: $($_.Exception.Message)" | Add-Content $logFile
    exit 1
}
```

### `C:\DataBision\scripts\run-ojdt.ps1`

```powershell
$date = Get-Date -Format "yyyy-MM-dd"
$logFile = "C:\DataBision\logs\ojdt-$date.log"
$profile = "KSDEPOR"  # Reemplazar por slug del cliente

"[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Iniciando extraccion OJDT - Perfil: $profile" | Tee-Object -FilePath $logFile

try {
    & "C:\DataBision\Extractor\DataBision.Extractor.exe" `
        --profile $profile `
        --object OJDT `
        2>&1 | Tee-Object -FilePath $logFile -Append

    $exitCode = $LASTEXITCODE
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] OJDT completado. Exit code: $exitCode" | Add-Content $logFile

    if ($exitCode -ne 0) {
        "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: OJDT fallo con exit code $exitCode" | Add-Content $logFile
        exit 1
    }
} catch {
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] EXCEPCION: $($_.Exception.Message)" | Add-Content $logFile
    exit 1
}
```

### `C:\DataBision\scripts\run-mart.ps1`

```powershell
$date = Get-Date -Format "yyyy-MM-dd"
$logFile = "C:\DataBision\logs\mart-$date.log"
$apiUrl = $env:DataBisionApi__BaseUrl
$apiKey = $env:DataBisionApi__ApiKey
$companyId = "COMPANY_ID_CLIENTE"  # Reemplazar por company_id real

"[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] Iniciando refresh MART - company_id: $companyId" | Tee-Object -FilePath $logFile

try {
    $response = Invoke-RestMethod `
        -Uri "$apiUrl/api/admin/bi/finance/refresh-mart?company_id=$companyId" `
        -Method POST `
        -Headers @{ "X-Api-Key" = $apiKey } `
        -ErrorAction Stop

    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] MART completado: $($response | ConvertTo-Json -Compress)" | Add-Content $logFile
} catch {
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR MART: $($_.Exception.Message)" | Add-Content $logFile
    exit 1
}
```

---

## Creación de tareas en Task Scheduler

### Método 1 — PowerShell (recomendado para automatización)

```powershell
# Configurar usuario de servicio
$serviceUser = "SERVIDOR\DATABISION_SVC"  # O "NT AUTHORITY\SYSTEM" si no hay usuario dedicado
$psExe = "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"

# TAREA 1: OACT semanal (lunes 01:00 AM)
$actionOact = New-ScheduledTaskAction `
    -Execute $psExe `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\scripts\run-oact.ps1" `
    -WorkingDirectory "C:\DataBision\Extractor"

$triggerOact = New-ScheduledTaskTrigger `
    -Weekly `
    -DaysOfWeek Monday `
    -At "01:00AM"

Register-ScheduledTask `
    -TaskName "DataBision-OACT-Weekly" `
    -Action $actionOact `
    -Trigger $triggerOact `
    -RunLevel Highest `
    -Description "DataBision: Extraccion semanal OACT desde SAP B1" `
    -Force

# TAREA 2: OJDT diario (02:00 AM)
$actionOjdt = New-ScheduledTaskAction `
    -Execute $psExe `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\scripts\run-ojdt.ps1" `
    -WorkingDirectory "C:\DataBision\Extractor"

$triggerOjdt = New-ScheduledTaskTrigger `
    -Daily `
    -At "02:00AM"

Register-ScheduledTask `
    -TaskName "DataBision-OJDT-Daily" `
    -Action $actionOjdt `
    -Trigger $triggerOjdt `
    -RunLevel Highest `
    -Description "DataBision: Extraccion diaria OJDT+JDT1 desde SAP B1" `
    -Force

# TAREA 3: MART diario (02:30 AM)
$actionMart = New-ScheduledTaskAction `
    -Execute $psExe `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\scripts\run-mart.ps1" `
    -WorkingDirectory "C:\DataBision"

$triggerMart = New-ScheduledTaskTrigger `
    -Daily `
    -At "02:30AM"

Register-ScheduledTask `
    -TaskName "DataBision-MART-Daily" `
    -Action $actionMart `
    -Trigger $triggerMart `
    -RunLevel Highest `
    -Description "DataBision: Refresh diario del MART financiero" `
    -Force
```

### Método 2 — Interfaz gráfica (Task Scheduler)

1. Abrir **Task Scheduler** (`taskschd.msc`)
2. Click derecho en **Task Scheduler Library** → **Create Task**
3. Pestaña **General:**
   - Name: `DataBision-OJDT-Daily`
   - Run whether user is logged on or not
   - Run with highest privileges
4. Pestaña **Triggers:**
   - New → Daily → 02:00 AM
5. Pestaña **Actions:**
   - New → Start a program
   - Program: `powershell.exe`
   - Arguments: `-NonInteractive -ExecutionPolicy Bypass -File C:\DataBision\scripts\run-ojdt.ps1`
   - Start in: `C:\DataBision\Extractor`
6. Pestaña **Settings:**
   - If the task fails, restart every: 15 minutes (up to 2 times)
7. OK → Ingresar credenciales del usuario de servicio

---

## Verificación de tareas

```powershell
# Listar tareas DataBision
Get-ScheduledTask | Where-Object TaskName -like "DataBision*" | 
    Select-Object TaskName, State, @{N="LastRun";E={$_.LastRunTime}}, @{N="LastResult";E={$_.LastTaskResult}}

# Ver historial de una tarea específica
Get-ScheduledTaskInfo -TaskName "DataBision-OJDT-Daily"

# Ejecutar manualmente para test
Start-ScheduledTask -TaskName "DataBision-OJDT-Daily"
```

---

## Rotación de logs

```powershell
# Limpiar logs de más de 30 días (agregar como tarea mensual)
Get-ChildItem "C:\DataBision\logs\*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
    Remove-Item -Force
```
