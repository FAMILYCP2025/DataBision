# Register-DataBisionScheduler.ps1
# DataBision Native BI — Registro de tareas en Windows Task Scheduler
#
# Registra las tres tareas programadas de extracción y healthcheck.
# Debe ejecutarse como Administrador.
#
# Uso:
#   .\Register-DataBisionScheduler.ps1 -ExtractorPath "C:\DataBision\Extractor" -CompanyId "ksdepor-analytics"
#   .\Register-DataBisionScheduler.ps1 -ExtractorPath "C:\DataBision\Extractor" -CompanyId "ksdepor-analytics" -RunAsUser "DATABISION_SVC"
#   .\Register-DataBisionScheduler.ps1 -Unregister

param(
    [string]$ExtractorPath = "",
    [string]$CompanyId     = "",
    [string]$RunAsUser     = "SYSTEM",
    [switch]$Unregister
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$TaskNames = @("DataBision-OACT-Weekly", "DataBision-OJDT-Daily", "DataBision-Healthcheck-Daily")
$LogFile   = Join-Path $env:TEMP "databision-scheduler-register.log"

function Write-Log {
    param([string]$Level, [string]$Message)
    $ts   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts $Level] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

# ── Modo -Unregister ───────────────────────────────────────────────────────────

if ($Unregister) {
    Write-Log "INF" "=== DataBision Task Scheduler — Eliminando tareas ==="
    foreach ($name in $TaskNames) {
        if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
            Unregister-ScheduledTask -TaskName $name -Confirm:$false
            Write-Log "INF" "  Eliminada: $name"
        } else {
            Write-Log "WRN" "  No encontrada (ya eliminada?): $name"
        }
    }
    Write-Log "INF" "Tareas DataBision restantes:"
    Get-ScheduledTask | Where-Object TaskName -like "DataBision*" | Format-Table TaskName, State, TaskPath -AutoSize
    return
}

# ── Validaciones ───────────────────────────────────────────────────────────────

if ([string]::IsNullOrWhiteSpace($ExtractorPath)) {
    Write-Error "El parametro -ExtractorPath es obligatorio al registrar tareas."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($CompanyId)) {
    Write-Error "El parametro -CompanyId es obligatorio al registrar tareas."
    exit 1
}

if (-not (Test-Path $ExtractorPath)) {
    Write-Error "ExtractorPath no existe: $ExtractorPath"
    exit 1
}

$ExePath    = Join-Path $ExtractorPath "DataBision.Extractor.exe"
$ScriptPath = Join-Path $ExtractorPath "run-nativebi-finance-refresh.ps1"

if (-not (Test-Path $ExePath)) {
    Write-Error "DataBision.Extractor.exe no encontrado en: $ExePath"
    exit 1
}

if (-not (Test-Path $ScriptPath)) {
    Write-Error "run-nativebi-finance-refresh.ps1 no encontrado en: $ScriptPath"
    exit 1
}

Write-Log "INF" "=== DataBision Task Scheduler — Registrando tareas ==="
Write-Log "INF" "ExtractorPath : $ExtractorPath"
Write-Log "INF" "CompanyId     : $CompanyId"
Write-Log "INF" "RunAsUser     : $RunAsUser"

# ── Settings compartidos (OACT + OJDT) ────────────────────────────────────────

$sharedSettings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
    -MultipleInstances IgnoreNew `
    -RestartCount 1 `
    -RestartInterval (New-TimeSpan -Minutes 15)

# ── Tarea A — DataBision-OACT-Weekly ─────────────────────────────────────────
# Trigger : semanal, domingo 01:00 AM
# Accion  : extraccion completa OACT + OJDT + transform

Write-Log "INF" "Registrando: DataBision-OACT-Weekly (domingo 01:00 AM)"

$triggerOact = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At "01:00"
$actionOact  = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File `"$ScriptPath`" -ExtractorPath `"$ExtractorPath`" -CompanyId `"$CompanyId`""

Register-ScheduledTask `
    -TaskName "DataBision-OACT-Weekly" `
    -Trigger $triggerOact `
    -Action $actionOact `
    -Settings $sharedSettings `
    -RunLevel Highest `
    -User $RunAsUser `
    -Force | Out-Null

Write-Log "INF" "  OK: DataBision-OACT-Weekly"

# ── Tarea B — DataBision-OJDT-Daily ──────────────────────────────────────────
# Trigger : diario 02:00 AM
# Accion  : extraccion incremental OJDT + transform (sin OACT)

Write-Log "INF" "Registrando: DataBision-OJDT-Daily (diario 02:00 AM)"

$triggerOjdt = New-ScheduledTaskTrigger -Daily -At "02:00"
$actionOjdt  = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File `"$ScriptPath`" -ExtractorPath `"$ExtractorPath`" -CompanyId `"$CompanyId`" -SkipOact"

Register-ScheduledTask `
    -TaskName "DataBision-OJDT-Daily" `
    -Trigger $triggerOjdt `
    -Action $actionOjdt `
    -Settings $sharedSettings `
    -RunLevel Highest `
    -User $RunAsUser `
    -Force | Out-Null

Write-Log "INF" "  OK: DataBision-OJDT-Daily"

# ── Tarea C — DataBision-Healthcheck-Daily ────────────────────────────────────
# Trigger : diario 05:00 AM
# Accion  : verifica que la API responde y escribe resultado en healthcheck.log

Write-Log "INF" "Registrando: DataBision-Healthcheck-Daily (diario 05:00 AM)"

$hcLogFile  = Join-Path $ExtractorPath "logs\healthcheck.log"
$hcCommand  = "Invoke-WebRequest -Uri 'http://localhost:5103/api/health' " +
              "-UseBasicParsing | Select-Object -ExpandProperty Content | " +
              "Out-File -Append '$hcLogFile'"

$hcSettings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 1) `
    -MultipleInstances IgnoreNew

$triggerHc  = New-ScheduledTaskTrigger -Daily -At "05:00"
$actionHc   = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -Command `"$hcCommand`""

Register-ScheduledTask `
    -TaskName "DataBision-Healthcheck-Daily" `
    -Trigger $triggerHc `
    -Action $actionHc `
    -Settings $hcSettings `
    -RunLevel Highest `
    -User $RunAsUser `
    -Force | Out-Null

Write-Log "INF" "  OK: DataBision-Healthcheck-Daily"

# ── Resumen ───────────────────────────────────────────────────────────────────

Write-Log "INF" "=== Registro completado. Tareas activas: ==="
Get-ScheduledTask | Where-Object TaskName -like "DataBision*" | Format-Table TaskName, State, TaskPath -AutoSize
