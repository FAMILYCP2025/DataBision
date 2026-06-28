# Native BI Modalidad A — Checklist Operativo Pre-Deployment Cliente Real

**DataBision · Junio 2026**  
**Versión:** 1.0 — Gate 2 pre-deployment Modalidad A  
**Uso:** Completar antes de considerar el extractor como productivamente operable en el servidor del cliente

---

## Instrucciones

Completar de arriba a abajo. Cada ítem debe ser verificado y marcado GO antes de pasar al siguiente. Si algún ítem queda en NO-GO, documentar la acción de remediación y el responsable.

---

## SECCIÓN 1 — Ambiente y prerequisitos

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 1.1 | .NET 8 Runtime instalado (`dotnet --version` retorna 8.x.x) | ☐ GO / ☐ NO-GO | |
| 1.2 | `ASPNETCORE_ENVIRONMENT=Production` configurada como variable de sistema (no de usuario) | ☐ GO / ☐ NO-GO | |
| 1.3 | Extractor publicado en directorio dedicado con permisos correctos | ☐ GO / ☐ NO-GO | |
| 1.4 | Usuario de servicio dedicado (sin permisos sudo) creado | ☐ GO / ☐ NO-GO | |
| 1.5 | Variables de entorno sensibles en archivo restringido (no en código) | ☐ GO / ☐ NO-GO | Ver [native-bi-production-env-template.md](../security/native-bi-production-env-template.md) |
| 1.6 | Directorio de logs creado con permisos correctos | ☐ GO / ☐ NO-GO | |
| 1.7 | Directorio de lockfiles creado | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 2 — Perfil de conexión SAP

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 2.1 | Perfil creado en panel Admin DataBision con nombre identificable (`ksdepor-prd`) | ☐ GO / ☐ NO-GO | |
| 2.2 | `EnvironmentName` del perfil = `PRD` | ☐ GO / ☐ NO-GO | |
| 2.3 | `IgnoreSslErrors = false` en el perfil de producción | ☐ GO / ☐ NO-GO | BLOQUEANTE — ver [native-bi-ignore-ssl-errors-production-block.md](../security/native-bi-ignore-ssl-errors-production-block.md) |
| 2.4 | SecretRef configurado como `env:NOMBRE_VAR` o `azure-kv://...` (nunca literal) | ☐ GO / ☐ NO-GO | |
| 2.5 | Test de conexión desde panel Admin retorna `success=true` | ☐ GO / ☐ NO-GO | |
| 2.6 | ChartOfAccounts: OK en el test | ☐ GO / ☐ NO-GO | |
| 2.7 | JournalEntries: OK en el test (puede ser "no disponible" en algunas versiones SL) | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 3 — Extractor: dry-run y validación

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 3.1 | `--dry-run` con el perfil PRD retorna configuración correcta | ☐ GO / ☐ NO-GO | Verificar DB, URL, profile ID |
| 3.2 | `--validate` pasa sin errores de configuración | ☐ GO / ☐ NO-GO | |
| 3.3 | `--validate-staging` pasa (tablas raw/stg/mart existen) | ☐ GO / ☐ NO-GO | |
| 3.4 | `--validate-ops` retorna conteos válidos (si ya hay datos previos) | ☐ GO / ☐ NO-GO | |
| 3.5 | `--run-once --send --object OACT` ejecuta sin error (exit code 0) | ☐ GO / ☐ NO-GO | |
| 3.6 | `--run-once --send --object OJDT` ejecuta sin error (exit code 0) | ☐ GO / ☐ NO-GO | |
| 3.7 | `--transform --include-mart` ejecuta sin error (exit code 0) | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 4 — Scheduler y recuperación automática

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 4.1 | Tarea OACT registrada (semanal, domingo 1 AM) | ☐ GO / ☐ NO-GO | |
| 4.2 | Tarea OJDT registrada (diario, 2 AM) | ☐ GO / ☐ NO-GO | |
| 4.3 | Tarea MART registrada (diario, 4 AM) | ☐ GO / ☐ NO-GO | |
| 4.4 | Retry automático configurado (RestartCount ≥ 1 en Windows; Persistent=true en Linux) | ☐ GO / ☐ NO-GO | |
| 4.5 | `MultipleInstances = IgnoreNew` configurado (sin ejecuciones paralelas) | ☐ GO / ☐ NO-GO | |
| 4.6 | Lock files se crean y eliminan correctamente en ejecución de prueba | ☐ GO / ☐ NO-GO | |
| 4.7 | Ejecución manual desde scheduler retorna LastTaskResult=0 (Windows) o estado active→dead (Linux) | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 5 — Telemetría y monitoreo mínimo

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 5.1 | `/api/client/bi/finance/refresh-status` retorna datos válidos post-extracción | ☐ GO / ☐ NO-GO | |
| 5.2 | `healthScore` ≥ 80 post-extracción inicial | ☐ GO / ☐ NO-GO | |
| 5.3 | `lastSuccessfulRefresh` no es null | ☐ GO / ☐ NO-GO | |
| 5.4 | Logs en disco accesibles con información de últimas ejecuciones | ☐ GO / ☐ NO-GO | |
| 5.5 | `--validate-ops` retorna al menos 1 extractor_run y 1 transform_run post-extracción | ☐ GO / ☐ NO-GO | |
| 5.6 | Dashboard accesible en el browser para el cliente (≥1 usuario) | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 6 — Seguridad

| # | Ítem | Estado | Notas |
|---|---|---|---|
| 6.1 | Perfil SAP con `IgnoreSslErrors=false` verificado | ☐ GO / ☐ NO-GO | Bloqueante |
| 6.2 | Password SAP almacenada como env var, no como texto en `appsettings.json` | ☐ GO / ☐ NO-GO | |
| 6.3 | Logs no contienen credenciales SAP ni JWT | ☐ GO / ☐ NO-GO | Revisar muestra de logs |
| 6.4 | Procedimiento de rotación de credenciales documentado y probado | ☐ GO / ☐ NO-GO | Ver [native-bi-sap-credential-rotation-runbook.md](../security/native-bi-sap-credential-rotation-runbook.md) |
| 6.5 | Extractor corre con usuario de servicio dedicado (sin sudo/admin) | ☐ GO / ☐ NO-GO | |
| 6.6 | Firewall del cliente permite salida al SAP Service Layer desde el servidor del extractor | ☐ GO / ☐ NO-GO | |

---

## SECCIÓN 7 — Procedimiento de retry manual

Si una extracción falla y se requiere retry inmediato (no esperar al próximo ciclo):

**Windows:**
```powershell
# Forzar ejecución inmediata
Start-ScheduledTask -TaskName "DataBision-OJDT"

# Verificar resultado en 2 minutos
Get-ScheduledTask -TaskName "DataBision-OJDT" | Select-Object LastRunTime, LastTaskResult
```

**Linux:**
```bash
# Ejecutar manualmente
sudo systemctl start databision-ojdt.service
sudo systemctl status databision-ojdt.service

# O ejecutar el script directamente
sudo -u databision /opt/databision/scripts/run-ojdt.sh
```

---

## SECCIÓN 8 — Procedimiento si el lock file queda huérfano

Si el proceso murió inesperadamente y dejó un lock file:

```bash
# Linux: verificar que no hay proceso extractor corriendo
ps aux | grep DataBision.Extractor

# Si no hay proceso, eliminar el lock
rm -f /run/databision/ojdt.lock

# Windows PowerShell
Get-Process -Name "dotnet" | Where-Object { $_.CommandLine -like "*DataBision.Extractor*" }
# Si no hay proceso:
Remove-Item "C:\DataBision\Locks\ojdt.lock" -Force
```

---

## Firma de GO/NO-GO

| Rol | Nombre | Fecha | Firma |
|---|---|---|---|
| DataBision (técnico) | | | |
| TI Cliente | | | |

**Estado final:** ☐ GO — deployment aprobado | ☐ NO-GO — pendiente de remediación
