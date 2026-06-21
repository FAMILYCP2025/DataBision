# Native BI Finance — Windows Task Scheduler Setup

**Sprint:** 21C  
**Fecha:** 2026-06-20  
**Audiencia:** Administrador de sistema Windows (servidor DataBision Extractor)

---

## Configuración de tarea programada Windows

### PowerShell — crear tarea con schtasks

```powershell
# Variables — ajustar por cliente
$TaskName    = "DataBision-Finance-Refresh-[SLUG]"
$ScriptPath  = "C:\DataBision\Extractors\[SLUG]\scripts\run-nativebi-finance-refresh.ps1"
$CompanyId   = "[ANALYTICS_COMPANY_ID]"
$TriggerTime = "06:00"   # 6am diario

# Crear tarea (ejecutar como SYSTEM o con cuenta de servicio)
$Action  = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File `"$ScriptPath`" -CompanyId `"$CompanyId`""

$Trigger = New-ScheduledTaskTrigger -Daily -At $TriggerTime

$Settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
    -RestartCount 1 `
    -RestartInterval (New-TimeSpan -Minutes 30) `
    -StartWhenAvailable

Register-ScheduledTask `
    -TaskName  $TaskName `
    -Action    $Action `
    -Trigger   $Trigger `
    -Settings  $Settings `
    -RunLevel  Highest `
    -Force
```

### Verificar tarea creada

```powershell
Get-ScheduledTask -TaskName "DataBision-Finance-Refresh-[SLUG]" | Select-Object TaskName, State
```

### Ejecutar manualmente (para prueba)

```powershell
Start-ScheduledTask -TaskName "DataBision-Finance-Refresh-[SLUG]"
# Ver resultado
Get-ScheduledTaskInfo -TaskName "DataBision-Finance-Refresh-[SLUG]" | Select-Object LastRunTime, LastTaskResult
```

### Eliminar tarea (al desactivar cliente)

```powershell
Unregister-ScheduledTask -TaskName "DataBision-Finance-Refresh-[SLUG]" -Confirm:$false
```

---

## Configuración recomendada por tipo de cliente

| Tipo | Trigger | Hora | SkipOact |
|---|---|---|---|
| Piloto (pocos JEs) | Diario | 06:00 | No (siempre full) |
| Producción activa | Diario | 02:00 | Sí los días sin cierre |
| Cierre mensual | Manual | — | No |
| Alta frecuencia | Cada 4h | 06:00, 10:00, 14:00, 18:00 | Sí (OJDT + transform solo) |

**OACT (Chart of Accounts):** Cambia raramente. Incluirlo diario es seguro. Si el cliente tiene > 5,000 cuentas y la extracción tarda, programar OACT semanal y OJDT diario (usar `-SkipOact` para las ejecuciones diarias).

---

## Estructura de directorios recomendada

```
C:\DataBision\
└── Extractors\
    └── [SLUG]\                          ← una carpeta por cliente
        ├── DataBision.Extractor.exe
        ├── appsettings.json             ← config base (vacía)
        ├── appsettings.Production.json  ← credenciales del cliente
        ├── logs\
        │   ├── finance-refresh-20260620.log
        │   └── databision-extractor-20260620.log
        └── scripts\
            └── run-nativebi-finance-refresh.ps1
```

---

## Monitoreo de logs

```powershell
# Ver el log del día de hoy
Get-Content "C:\DataBision\Extractors\[SLUG]\logs\finance-refresh-$(Get-Date -f yyyyMMdd).log" -Tail 50

# Buscar errores en log actual
Select-String -Path "C:\DataBision\Extractors\[SLUG]\logs\finance-refresh-*.log" -Pattern "\[ERR\]"

# Ver todos los logs de extractor
Get-Content "C:\DataBision\Extractors\[SLUG]\logs\databision-extractor-*.log" -Tail 100
```

---

## Notificaciones de error (opcional)

Para recibir email si la tarea falla, agregar al script:

```powershell
# Al final del script, después del exit code check
if ($ExitCode -ne 0) {
    Send-MailMessage `
        -To "ops@databision.app" `
        -From "extractor@databision.app" `
        -Subject "DataBision [SLUG]: Finance Refresh FAILED" `
        -Body "Ver log: $LogFile" `
        -SmtpServer "smtp.databision.app"
}
```

O usar el Event Log de Windows para integrarlo con el sistema de alertas existente del cliente.

---

## Troubleshooting

| Problema | Diagnóstico |
|---|---|
| Tarea no ejecuta | Verificar cuenta de usuario, permisos de ejecución de scripts PS |
| `LastTaskResult: 0x1` | Error en el script — revisar log del día |
| OACT falla | Revisar conectividad SAP (VPN, firewall) |
| OJDT falla con timeout | Aumentar `TimeoutSeconds` en appsettings o reducir `PageSize` |
| Transform falla | Verificar `Staging:ConnectionString` (Supabase) en appsettings |
| Log file no se crea | Verificar que la cuenta Task Scheduler tiene write en `C:\DataBision\Extractors\[SLUG]\logs\` |
