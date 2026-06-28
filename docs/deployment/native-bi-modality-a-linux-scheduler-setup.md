# Native BI Modalidad A — Configuración del Scheduler Linux (Producción)

**DataBision · Junio 2026**  
**Versión:** 1.0 — Gate 2 pre-deployment Modalidad A  
**Aplica a:** Ubuntu 20.04/22.04 LTS, Debian 11/12, y distribuciones compatibles

---

## Decisión de diseño: one-shot, no proceso permanente

Igual que en Windows, el extractor **no corre como proceso permanente** en Linux. El cron o systemd timer lanza la ejecución, el proceso corre y termina con un exit code. Si falla, el siguiente ciclo programado lo reintenta. Los lockfiles evitan ejecuciones concurrentes.

---

## Prerequisitos

- .NET 8 Runtime instalado:
  ```bash
  dotnet --version  # debe retornar 8.x.x
  ```
- Extractor publicado en `/opt/databision/extractor/`
- Usuario de servicio dedicado (sin sudo):
  ```bash
  sudo useradd -r -s /bin/false -d /opt/databision databision
  sudo chown -R databision:databision /opt/databision
  ```
- Variables de entorno configuradas en `/etc/databision.env`

---

## Archivo de variables de entorno

Crear `/etc/databision.env` (solo root puede leer):

```bash
sudo nano /etc/databision.env
```

Contenido (sin comillas, sin espacios alrededor del =):

```
ASPNETCORE_ENVIRONMENT=Production
DATABISION_API__BASEURL=https://api.databision.app
DATABISION_API__APIKEY=<tu-api-key>
DATABISION_STAGING__CONNECTIONSTRING=<supabase-connection-string>
```

Asegurar permisos restrictivos:

```bash
sudo chmod 640 /etc/databision.env
sudo chown root:databision /etc/databision.env
```

---

## Scripts Bash

### /opt/databision/scripts/run-oact.sh

```bash
#!/bin/bash
# Extracción semanal OACT (plan de cuentas)
set -euo pipefail

LOG_DIR="/var/log/databision"
LOCK_FILE="/run/databision/oact.lock"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/oact-$TIMESTAMP.log"

mkdir -p "$LOG_DIR" "$(dirname $LOCK_FILE)"

if [ -f "$LOCK_FILE" ]; then
    echo "[$(date -Is)] SKIP: Lock file exists. Previous run still active or crashed." >> "$LOG_FILE"
    exit 1
fi

touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

source /etc/databision.env

echo "[$(date -Is)] START: OACT extraction" >> "$LOG_FILE"

dotnet /opt/databision/extractor/DataBision.Extractor.dll \
    --profile ksdepor-prd \
    --object OACT \
    --run-once --send \
    >> "$LOG_FILE" 2>&1

EXIT_CODE=$?
echo "[$(date -Is)] EXIT_CODE=$EXIT_CODE" >> "$LOG_FILE"

if [ $EXIT_CODE -ne 0 ]; then
    echo "[$(date -Is)] ERROR: OACT extraction failed. Manual review required." >> "$LOG_FILE"
fi

exit $EXIT_CODE
```

### /opt/databision/scripts/run-ojdt.sh

```bash
#!/bin/bash
# Extracción diaria OJDT (libro diario incremental)
set -euo pipefail

LOG_DIR="/var/log/databision"
LOCK_FILE="/run/databision/ojdt.lock"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/ojdt-$TIMESTAMP.log"

mkdir -p "$LOG_DIR" "$(dirname $LOCK_FILE)"

if [ -f "$LOCK_FILE" ]; then
    echo "[$(date -Is)] SKIP: Lock file exists. Previous run still active or crashed." >> "$LOG_FILE"
    exit 1
fi

touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

source /etc/databision.env

echo "[$(date -Is)] START: OJDT extraction" >> "$LOG_FILE"

dotnet /opt/databision/extractor/DataBision.Extractor.dll \
    --profile ksdepor-prd \
    --object OJDT \
    --run-once --send \
    >> "$LOG_FILE" 2>&1

EXIT_CODE=$?
echo "[$(date -Is)] EXIT_CODE=$EXIT_CODE" >> "$LOG_FILE"

if [ $EXIT_CODE -ne 0 ]; then
    echo "[$(date -Is)] ERROR: OJDT extraction failed." >> "$LOG_FILE"
fi

exit $EXIT_CODE
```

### /opt/databision/scripts/run-mart.sh

```bash
#!/bin/bash
# Refresh diario MART financiero (STG → MART)
set -euo pipefail

LOG_DIR="/var/log/databision"
LOCK_FILE="/run/databision/mart.lock"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$LOG_DIR/mart-$TIMESTAMP.log"

mkdir -p "$LOG_DIR" "$(dirname $LOCK_FILE)"

if [ -f "$LOCK_FILE" ]; then
    echo "[$(date -Is)] SKIP: Lock file exists." >> "$LOG_FILE"
    exit 1
fi

touch "$LOCK_FILE"
trap 'rm -f "$LOCK_FILE"' EXIT

source /etc/databision.env

echo "[$(date -Is)] START: MART refresh" >> "$LOG_FILE"

dotnet /opt/databision/extractor/DataBision.Extractor.dll \
    --transform --include-mart \
    >> "$LOG_FILE" 2>&1

EXIT_CODE=$?
echo "[$(date -Is)] EXIT_CODE=$EXIT_CODE" >> "$LOG_FILE"

if [ $EXIT_CODE -ne 0 ]; then
    echo "[$(date -Is)] ERROR: MART refresh failed. Dashboard may show stale data." >> "$LOG_FILE"
fi

exit $EXIT_CODE
```

Hacer ejecutables:

```bash
sudo chmod +x /opt/databision/scripts/*.sh
sudo chown databision:databision /opt/databision/scripts/*.sh
```

---

## Opción A — Crontab del usuario databision

```bash
sudo crontab -u databision -e
```

Agregar:

```cron
# DataBision Extractor — Producción
# OACT (plan de cuentas) — domingos 1:00 AM
0 1 * * 0  /opt/databision/scripts/run-oact.sh >> /var/log/databision/cron.log 2>&1

# OJDT (libro diario) — diario 2:00 AM
0 2 * * *  /opt/databision/scripts/run-ojdt.sh >> /var/log/databision/cron.log 2>&1

# MART (refresh financiero) — diario 4:00 AM
0 4 * * *  /opt/databision/scripts/run-mart.sh >> /var/log/databision/cron.log 2>&1
```

---

## Opción B — Systemd timers (recomendado para producción)

Los systemd timers permiten retry, logging a journald, y control de estado más preciso que crontab.

### Service units

**/etc/systemd/system/databision-ojdt.service:**

```ini
[Unit]
Description=DataBision OJDT Extractor (libro diario SAP)
After=network.target

[Service]
Type=oneshot
User=databision
Group=databision
EnvironmentFile=/etc/databision.env
ExecStart=/opt/databision/scripts/run-ojdt.sh
StandardOutput=journal
StandardError=journal
SyslogIdentifier=databision-ojdt
TimeoutStartSec=7200
```

**/etc/systemd/system/databision-mart.service:**

```ini
[Unit]
Description=DataBision MART Refresh (financiero)
After=network.target

[Service]
Type=oneshot
User=databision
Group=databision
EnvironmentFile=/etc/databision.env
ExecStart=/opt/databision/scripts/run-mart.sh
StandardOutput=journal
StandardError=journal
SyslogIdentifier=databision-mart
TimeoutStartSec=3600
```

### Timer units

**/etc/systemd/system/databision-ojdt.timer:**

```ini
[Unit]
Description=DataBision OJDT — diario 2:00 AM

[Timer]
OnCalendar=*-*-* 02:00:00
RandomizedDelaySec=120
Persistent=true

[Install]
WantedBy=timers.target
```

**/etc/systemd/system/databision-mart.timer:**

```ini
[Unit]
Description=DataBision MART — diario 4:00 AM

[Timer]
OnCalendar=*-*-* 04:00:00
RandomizedDelaySec=60
Persistent=true

[Install]
WantedBy=timers.target
```

Activar:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now databision-ojdt.timer
sudo systemctl enable --now databision-mart.timer
```

---

## Verificación

```bash
# Estado de timers
systemctl list-timers databision-*

# Última ejecución
systemctl status databision-ojdt.service

# Logs de journald
journalctl -u databision-ojdt --since "24 hours ago" --no-pager

# Logs de archivo
tail -100 /var/log/databision/ojdt-*.log | grep "EXIT_CODE"
```

---

## Retención de logs

```bash
# Agregar a crontab (semanal — lunes 0:30 AM)
30 0 * * 1  find /var/log/databision -name "*.log" -mtime +30 -delete
```

---

## Criterio GO Gate 2 (Linux)

| Criterio | Cómo verificar |
|---|---|
| Scripts ejecutan sin error | `sudo -u databision /opt/databision/scripts/run-ojdt.sh` → EXIT_CODE=0 |
| Logs se crean correctamente | `ls /var/log/databision/ojdt-*.log` |
| Lock files se eliminan post-ejecución | `ls /run/databision/*.lock` debe estar vacío |
| Timer activo | `systemctl list-timers databision-*` |
| Retry en caso de fallo | `Persistent=true` en timer + scripts retornan exit code correcto |
