# Native BI Finance — Linux Cron y Systemd Timer (Producción)

**Sprint 28 · DataBision · Junio 2026**

---

## Prerequisitos

```bash
# Verificar .NET 8 runtime
dotnet --version  # debe ser >= 8.0

# Crear usuario de servicio (sin login)
sudo useradd -r -s /bin/false databision-svc

# Crear estructura de directorios
sudo mkdir -p /opt/databision/extractor
sudo mkdir -p /opt/databision/logs
sudo mkdir -p /opt/databision/scripts
sudo chown -R databision-svc:databision-svc /opt/databision
```

---

## Variables de entorno del servicio

Crear archivo `/opt/databision/.env` (propiedad de `databision-svc`, modo 600):

```bash
sudo -u databision-svc touch /opt/databision/.env
sudo chmod 600 /opt/databision/.env
# Editar con valores reales — nunca commitear este archivo
sudo nano /opt/databision/.env
```

Contenido del `.env`:
```bash
ASPNETCORE_ENVIRONMENT=Production
DataBisionApi__BaseUrl=https://[API_URL]
DataBisionApi__ApiKey=[API_KEY_SIN_VALOR_AQUI]
SAP_PASSWORD_KSDEPOR=[PASSWORD_SIN_VALOR_AQUI]
DATABISION_PROFILE=ksdepor
DATABISION_COMPANY_ID=[COMPANY_ID_REAL]
```

---

## Scripts de ejecución

### `/opt/databision/scripts/run-oact.sh`

```bash
#!/bin/bash
set -euo pipefail

DATE=$(date +%Y-%m-%d)
LOG_FILE="/opt/databision/logs/oact-${DATE}.log"
PROFILE="${DATABISION_PROFILE:-ksdepor}"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Iniciando extraccion OACT - Perfil: $PROFILE" | tee -a "$LOG_FILE"

cd /opt/databision/extractor

dotnet DataBision.Extractor.dll \
    --profile "$PROFILE" \
    --object OACT \
    >> "$LOG_FILE" 2>&1

EXIT_CODE=$?
echo "[$(date '+%Y-%m-%d %H:%M:%S')] OACT completado. Exit code: $EXIT_CODE" | tee -a "$LOG_FILE"

exit $EXIT_CODE
```

### `/opt/databision/scripts/run-ojdt.sh`

```bash
#!/bin/bash
set -euo pipefail

DATE=$(date +%Y-%m-%d)
LOG_FILE="/opt/databision/logs/ojdt-${DATE}.log"
PROFILE="${DATABISION_PROFILE:-ksdepor}"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Iniciando extraccion OJDT - Perfil: $PROFILE" | tee -a "$LOG_FILE"

cd /opt/databision/extractor

dotnet DataBision.Extractor.dll \
    --profile "$PROFILE" \
    --object OJDT \
    >> "$LOG_FILE" 2>&1

EXIT_CODE=$?
echo "[$(date '+%Y-%m-%d %H:%M:%S')] OJDT completado. Exit code: $EXIT_CODE" | tee -a "$LOG_FILE"

exit $EXIT_CODE
```

### `/opt/databision/scripts/run-mart.sh`

```bash
#!/bin/bash
set -euo pipefail

DATE=$(date +%Y-%m-%d)
LOG_FILE="/opt/databision/logs/mart-${DATE}.log"
API_URL="${DataBisionApi__BaseUrl}"
API_KEY="${DataBisionApi__ApiKey}"
COMPANY_ID="${DATABISION_COMPANY_ID}"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Iniciando refresh MART - company_id: $COMPANY_ID" | tee -a "$LOG_FILE"

RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST \
    -H "X-Api-Key: $API_KEY" \
    "${API_URL}/api/admin/bi/finance/refresh-mart?company_id=${COMPANY_ID}")

echo "[$(date '+%Y-%m-%d %H:%M:%S')] MART refresh HTTP: $RESPONSE" | tee -a "$LOG_FILE"

if [ "$RESPONSE" != "200" ]; then
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] ERROR: MART refresh fallo con HTTP $RESPONSE" | tee -a "$LOG_FILE"
    exit 1
fi

echo "[$(date '+%Y-%m-%d %H:%M:%S')] MART completado OK" | tee -a "$LOG_FILE"
```

```bash
# Hacer ejecutables y asignar propietario
sudo chmod +x /opt/databision/scripts/*.sh
sudo chown databision-svc:databision-svc /opt/databision/scripts/*.sh
```

---

## Opción A — Crontab (simple, recomendado para piloto)

```bash
# Editar crontab del usuario de servicio
sudo crontab -u databision-svc -e
```

Contenido del crontab:
```cron
# DataBision Native BI Finance — Refresh Schedule
# Formato: minuto hora dia-mes mes dia-semana comando

# OACT semanal — lunes 01:00 AM
0 1 * * 1 . /opt/databision/.env && /opt/databision/scripts/run-oact.sh

# OJDT diario — 02:00 AM (todos los días)
0 2 * * * . /opt/databision/.env && /opt/databision/scripts/run-ojdt.sh

# MART diario — 02:30 AM (después de OJDT)
30 2 * * * . /opt/databision/.env && /opt/databision/scripts/run-mart.sh

# Limpieza de logs — primer día de mes, 03:00 AM
0 3 1 * * find /opt/databision/logs -name "*.log" -mtime +30 -delete
```

---

## Opción B — Systemd Timers (recomendado para producción Enterprise)

### Servicio OJDT: `/etc/systemd/system/databision-ojdt.service`

```ini
[Unit]
Description=DataBision OJDT Daily Extraction
After=network.target

[Service]
Type=oneshot
User=databision-svc
WorkingDirectory=/opt/databision/extractor
EnvironmentFile=/opt/databision/.env
ExecStart=/opt/databision/scripts/run-ojdt.sh
StandardOutput=append:/opt/databision/logs/ojdt-systemd.log
StandardError=append:/opt/databision/logs/ojdt-systemd.log
```

### Timer OJDT: `/etc/systemd/system/databision-ojdt.timer`

```ini
[Unit]
Description=DataBision OJDT Daily Timer
Requires=databision-ojdt.service

[Timer]
OnCalendar=*-*-* 02:00:00
Persistent=true

[Install]
WantedBy=timers.target
```

### Servicio MART: `/etc/systemd/system/databision-mart.service`

```ini
[Unit]
Description=DataBision MART Daily Refresh
After=databision-ojdt.service

[Service]
Type=oneshot
User=databision-svc
WorkingDirectory=/opt/databision
EnvironmentFile=/opt/databision/.env
ExecStart=/opt/databision/scripts/run-mart.sh
StandardOutput=append:/opt/databision/logs/mart-systemd.log
StandardError=append:/opt/databision/logs/mart-systemd.log
```

### Timer MART: `/etc/systemd/system/databision-mart.timer`

```ini
[Unit]
Description=DataBision MART Daily Timer
Requires=databision-mart.service

[Timer]
OnCalendar=*-*-* 02:30:00
Persistent=true

[Install]
WantedBy=timers.target
```

### Activar timers

```bash
sudo systemctl daemon-reload
sudo systemctl enable databision-ojdt.timer databision-mart.timer
sudo systemctl start databision-ojdt.timer databision-mart.timer

# Verificar estado
sudo systemctl list-timers --all | grep databision
sudo systemctl status databision-ojdt.timer
```

---

## Ejecución manual (retry)

```bash
# Cargar variables de entorno y ejecutar
sudo -u databision-svc bash -c '. /opt/databision/.env && /opt/databision/scripts/run-ojdt.sh'

# Ver últimos logs
tail -50 /opt/databision/logs/ojdt-$(date +%Y-%m-%d).log

# Ver logs de systemd (si usa timers)
sudo journalctl -u databision-ojdt -n 50
```
