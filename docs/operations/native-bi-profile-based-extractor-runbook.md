# Native BI — Runbook: Extractor con Perfiles de Conexión

**Sprint:** 23F  
**Fecha:** 2026-06-21  
**Aplica a:** Clientes con NativeBiConnectionProfile configurado en Admin

---

## Contexto

Desde Sprint 23, el extractor puede cargar credenciales SAP directamente desde la DataBision API en lugar de `appsettings.json`. Esto permite:

- Rotación de credenciales desde el Admin web (sin SSH al servidor del extractor)
- Auditoría centralizada de perfiles activos
- Un único punto de configuración de credenciales por cliente

---

## Pre-requisitos

| Requisito | Verificación |
|---|---|
| DataBision API accesible desde el servidor del extractor | `curl https://api.databision.app/api/health` |
| Variable de entorno `SAP_PASSWORD_CLIENT` configurada en el servidor de la **API** | `echo $SAP_PASSWORD_CLIENT` en el servidor API |
| Perfil creado en Admin → Empresa → Native BI | Ver lista de perfiles, estado "Activo" |
| Test de conexión exitoso desde Admin UI | Botón "Test" → ✓ OK |
| `DataBisionApi:BaseUrl` y `DataBisionApi:ApiKey` en appsettings del extractor | `--dry-run` muestra `[set]` |

---

## Paso 1: Crear perfil en Admin UI

1. Ir a `admin.databision.app` → Empresa → pestaña "Native BI — Configuración avanzada"
2. Click en **+ Nuevo perfil**
3. Completar campos:
   - **Nombre:** `produccion` (sin espacios, minúsculas)
   - **Entorno:** `Production`
   - **SL Base URL:** `https://sap-host:50000/b1s/v1`
   - **CompanyDB:** `CLIENT_PRD`
   - **Usuario SAP:** `databision_ro`
   - **SecretRef:** `env:SAP_PASSWORD_CLIENT` ← apunta a la variable de entorno en el servidor API
4. Click **Crear perfil**

---

## Paso 2: Test de conexión desde UI

1. En la fila del perfil recién creado, click **Test**
2. Esperar resultado:
   - ✓ OK → Login, CoA, JE exitosos → continuar
   - ✗ FAIL → Ver mensaje de error → verificar SecretRef y conectividad de red

**Nota:** El test se ejecuta desde el servidor de la API hacia SAP. Confirma que la variable de entorno está correcta y que la API tiene acceso de red al SAP Service Layer.

---

## Paso 3: Verificar dry-run del extractor

```bash
dotnet DataBision.Extractor.exe --profile produccion --dry-run
```

Salida esperada:
```
[INFO] Resolving SAP credentials from API: company=company-client-001 profile=produccion
[INFO] Profile resolved: id=1 name=produccion db=CLIENT_PRD concurrency=3
[INFO] SAP credentials loaded from profile. DB=CLIENT_PRD
[INFO] === DRY-RUN: configuration check ===
```

---

## Paso 4: Extracción manual (primera vez)

```bash
# Extraer Chart of Accounts (full refresh)
dotnet DataBision.Extractor.exe --profile produccion --object OACT --send

# Extraer Journal Entries (incremental)
dotnet DataBision.Extractor.exe --profile produccion --object OJDT --send

# Transformar MART
dotnet DataBision.Extractor.exe --transform-mart --company company-client-001
```

---

## Paso 5: Configurar scheduler con --profile

### Windows Task Scheduler (PowerShell)

```powershell
$action = New-ScheduledTaskAction `
    -Execute "dotnet" `
    -Argument "C:\DataBision\DataBision.Extractor.dll --profile produccion --object OACT --send" `
    -WorkingDirectory "C:\DataBision"

$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"
Register-ScheduledTask -TaskName "DataBision-OACT" -Action $action -Trigger $trigger
```

### Linux cron

```bash
# /etc/cron.d/databision-extractor
0 2 * * * databision cd /opt/databision && dotnet DataBision.Extractor.dll --profile produccion --object OACT --send >> /var/log/databision/oact.log 2>&1
30 2 * * * databision cd /opt/databision && dotnet DataBision.Extractor.dll --profile produccion --object OJDT --send >> /var/log/databision/ojdt.log 2>&1
0 4 * * * databision cd /opt/databision && dotnet DataBision.Extractor.dll --transform-mart --company company-client-001 >> /var/log/databision/mart.log 2>&1
```

### Windows Service (appsettings.json)

```json
{
  "Extractor": {
    "ProfileName": "produccion",
    "CompanyId": "company-client-001",
    "TenantId": "tenant-client",
    "Objects": ["OACT", "OJDT"],
    "SendEnabled": true,
    "IntervalMinutes": 1440,
    "RunMartRefreshAfterExtraction": true,
    "RunProcessMartRefreshAfterExtraction": true,
    "MartRefreshCompanyId": "company-client-001"
  },
  "DataBisionApi": {
    "BaseUrl": "https://api.databision.app",
    "ApiKey": "extractor-api-key"
  }
}
```

Con esta configuración, el servicio Windows resuelve el perfil automáticamente al iniciar.

---

## Rotación de credenciales SAP

1. Actualizar la variable de entorno `SAP_PASSWORD_CLIENT` en el **servidor de la API**
2. Reiniciar el servidor de la API (para recargar las env vars)
3. En Admin UI → Test → confirmar que el test pasa
4. El extractor resolverá el nuevo password en el próximo inicio (CLI) o próximo ciclo si el servicio se reinicia

**No es necesario tocar el servidor del extractor para rotar el password.**

---

## Rollback: volver a appsettings

Si la resolución de perfil falla (API no disponible, perfil eliminado), el extractor muestra:

```
[ERROR] Failed to resolve connection profile 'produccion' for company 'company-client-001'.
```

Y termina con exit code 2 (CLI) o no inicia (--service).

Para hacer rollback rápido:
1. Quitar `--profile produccion` del comando del scheduler
2. Agregar credenciales en `appsettings.json`:
   ```json
   {
     "SapServiceLayer": {
       "BaseUrl": "https://sap-host:50000/b1s/v1",
       "CompanyDB": "CLIENT_PRD",
       "UserName": "databision_ro",
       "Password": "..."
     }
   }
   ```
3. La extracción continúa sin interrupción

---

## Troubleshooting rápido

| Síntoma | Causa probable | Acción |
|---|---|---|
| `HTTP 401` al resolver | API Key incorrecto | Verificar `DataBisionApi:ApiKey` vs `Ingest:ApiKeys` en API |
| `HTTP 404: company_not_found` | Analytics Company ID no configurado | Admin → Empresa → Editar → Analytics Company ID |
| `HTTP 404: profile_not_found` | Nombre de perfil incorrecto | Verificar nombre exacto (case-sensitive) |
| `HTTP 500: secret_resolution_failed` | ENV var no configurada en servidor API | SSH al servidor API, `export SAP_PASSWORD_CLIENT=...`, restart API |
| Test de conexión falla desde UI | API no puede llegar a SAP SL | Verificar firewall entre servidor API y SAP Service Layer |
| Extractor no puede llegar a API | Firewall o URL incorrecta | `curl https://api.databision.app/api/health` desde servidor extractor |
