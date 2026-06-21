# Native BI Finance — Linux Cron / Systemd Setup

**Sprint:** 21C  
**Fecha:** 2026-06-20  
**Audiencia:** Administrador de sistema Linux (servidor DataBision Extractor)

---

## Opción A — crontab (simple)

```bash
# Editar crontab del usuario que ejecuta el extractor
crontab -e

# Agregar (ejecutar lunes-sábado a las 06:00):
0 6 * * 1-6 /opt/databision/extractors/[SLUG]/scripts/run-nativebi-finance-refresh.sh --company [ANALYTICS_COMPANY_ID] >> /opt/databision/extractors/[SLUG]/logs/cron.log 2>&1

# Para alto frecuencia (cada 4h):
0 2,6,10,14,18 * * * /opt/databision/extractors/[SLUG]/scripts/run-nativebi-finance-refresh.sh --company [ANALYTICS_COMPANY_ID] --skip-oact >> /opt/databision/extractors/[SLUG]/logs/cron.log 2>&1

# Para OACT una vez por semana (lunes):
0 1 * * 1 /opt/databision/extractors/[SLUG]/scripts/run-nativebi-finance-refresh.sh --company [ANALYTICS_COMPANY_ID]
```

Permisos del script:
```bash
chmod +x /opt/databision/extractors/[SLUG]/scripts/run-nativebi-finance-refresh.sh
chmod +x /opt/databision/extractors/[SLUG]/DataBision.Extractor
```

---

## Opción B — systemd timer (recomendado para producción)

Systemd timer garantiza que: (a) la tarea no se superpone consigo misma, (b) se reinicia si el servidor se reinicia, (c) los logs van a journald.

### 1. Crear el servicio

```ini
# /etc/systemd/system/databision-finance-[SLUG].service

[Unit]
Description=DataBision Finance Refresh - [SLUG]
After=network.target

[Service]
Type=oneshot
User=databision
WorkingDirectory=/opt/databision/extractors/[SLUG]
ExecStart=/opt/databision/extractors/[SLUG]/scripts/run-nativebi-finance-refresh.sh --company [ANALYTICS_COMPANY_ID]
StandardOutput=journal
StandardError=journal
SyslogIdentifier=databision-[SLUG]
TimeoutStartSec=7200

[Install]
WantedBy=multi-user.target
```

### 2. Crear el timer

```ini
# /etc/systemd/system/databision-finance-[SLUG].timer

[Unit]
Description=DataBision Finance Refresh Timer - [SLUG]
Requires=databision-finance-[SLUG].service

[Timer]
OnCalendar=Mon-Sat 06:00
AccuracySec=5m
Persistent=true

[Install]
WantedBy=timers.target
```

### 3. Activar

```bash
systemctl daemon-reload
systemctl enable databision-finance-[SLUG].timer
systemctl start databision-finance-[SLUG].timer
```

### 4. Verificar estado

```bash
systemctl list-timers | grep databision
systemctl status databision-finance-[SLUG].timer
journalctl -u databision-finance-[SLUG].service -n 100
```

### 5. Ejecutar manualmente

```bash
systemctl start databision-finance-[SLUG].service
```

---

## Estructura de directorios Linux

```
/opt/databision/
└── extractors/
    └── [SLUG]/
        ├── DataBision.Extractor          ← binario Linux
        ├── appsettings.json              ← config base
        ├── appsettings.Production.json   ← credenciales cliente
        ├── logs/
        │   └── finance-refresh-20260620.log
        └── scripts/
            └── run-nativebi-finance-refresh.sh
```

Permisos:
```bash
useradd -r -s /sbin/nologin databision
chown -R databision:databision /opt/databision/
chmod 700 /opt/databision/extractors/[SLUG]/appsettings.Production.json
```

---

## Monitoreo de logs

```bash
# Ver log del día
tail -100 /opt/databision/extractors/[SLUG]/logs/finance-refresh-$(date +%Y%m%d).log

# Buscar errores en todos los logs
grep "\[ERR\]" /opt/databision/extractors/[SLUG]/logs/finance-refresh-*.log

# Via journald (si usa systemd)
journalctl -u databision-finance-[SLUG].service --since "2 hours ago"
```

---

## Alertas por email (opcional)

```bash
# Al final del script, agregar:
if [ "$EXIT_CODE" -ne 0 ]; then
    echo "DataBision [SLUG]: Finance Refresh FAILED. Log: $LOG_FILE" | \
        mail -s "DataBision [SLUG] ERROR" ops@databision.app
fi
```

O integrar con el sistema de alertas existente (Prometheus Alertmanager, PagerDuty, etc.) leyendo el exit code del servicio systemd.
