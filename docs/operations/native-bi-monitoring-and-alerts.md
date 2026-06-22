# Native BI Finance — Monitoreo y Alertas

**Sprint 28 · DataBision · Junio 2026**

---

## Endpoint principal de monitoreo

```
GET /api/client/bi/finance/refresh-status
Authorization: Bearer [TOKEN]
```

Este endpoint es la fuente de verdad del estado del sistema. Devuelve:

```json
{
  "lastExtraction": "2026-06-22T02:15:43Z",
  "oactCount": 120,
  "ojdtCount": 847,
  "jdt1Count": 3241,
  "martLastRefresh": "2026-06-22T02:31:05Z",
  "status": "healthy",
  "unclassifiedAccounts": 3,
  "healthScore": 97
}
```

---

## Catálogo de alertas

### A1 — API DataBision no responde

| Severidad | Crítica |
|---|---|
| Condición | `GET /api/client/bi/finance/refresh-status` retorna error o timeout |
| Impacto | Dashboard completamente inaccesible para el cliente |
| Respuesta inmediata | Verificar que el proceso API está corriendo: `dotnet DataBision.Api` |
| Tiempo máximo de respuesta | 15 minutos |

```bash
# Check de uptime (ejecutar desde monitor externo o cron)
curl -f -s -o /dev/null -w "%{http_code}" \
  "https://[API_URL]/api/client/bi/finance/refresh-status" \
  -H "Authorization: Bearer [TOKEN]"
# Si no retorna 200 → alerta
```

---

### A2 — Extractor no corrió en más de 25 horas

| Severidad | Alta |
|---|---|
| Condición | `lastExtraction` tiene antigüedad > 25 horas |
| Impacto | Dashboard muestra datos del día anterior |
| Respuesta inmediata | Revisar logs del extractor y ejecutar retry manual |
| Tiempo máximo de respuesta | 2 horas |

```bash
# Verificar antigüedad de la última extracción (JavaScript)
node -e "
const last = new Date('2026-06-22T02:15:43Z');
const diff = (Date.now() - last.getTime()) / 1000 / 3600;
console.log(diff > 25 ? 'ALERTA: extractor atrasado ' + diff.toFixed(1) + 'h' : 'OK');
"
```

---

### A3 — OACT count = 0

| Severidad | Alta |
|---|---|
| Condición | `oactCount` = 0 después de una extracción OACT completada |
| Impacto | Plan de cuentas vacío — clasificación no funciona |
| Causa más probable | Error de conexión SAP o tabla RAW vacía |
| Respuesta inmediata | Re-ejecutar extracción OACT manualmente |

---

### A4 — OJDT count = 0 en día hábil

| Severidad | Alta |
|---|---|
| Condición | `ojdtCount` = 0 en día lunes–viernes |
| Impacto | P&L y Balance del día sin datos nuevos |
| Causa más probable | SAP sin asientos ese día (posible cierre o feriado) o fallo de extracción |
| Respuesta | Verificar en SAP si hay asientos en ese período antes de alarmar |

---

### A5 — JDT1 count = 0

| Severidad | Alta |
|---|---|
| Condición | `jdt1Count` = 0 cuando `ojdtCount` > 0 |
| Impacto | Asientos sin líneas — estados financieros incorrectos |
| Causa más probable | Fallo en los GET individuales `JournalEntries(N)` |
| Respuesta | Revisar logs de extracción para errores de GET individual |

---

### A6 — MART refresh fallido

| Severidad | Alta |
|---|---|
| Condición | `martLastRefresh` > 25 horas o endpoint MART devuelve error |
| Impacto | Dashboard muestra datos del día anterior |
| Respuesta | Ejecutar refresh MART manual via API admin |

```bash
curl -X POST \
  -H "X-Api-Key: [ADMIN_KEY]" \
  "https://[API_URL]/api/admin/bi/finance/refresh-mart?company_id=[COMPANY_ID]"
```

---

### A7 — Cuentas sin clasificar > 0

| Severidad | Media |
|---|---|
| Condición | `unclassifiedAccounts` > 0 |
| Impacto | P&L muestra línea "Sin clasificar" o cuentas no aparecen en reportes |
| Respuesta | Revisar con el contador del cliente qué cuentas clasificar |
| Urgencia | No urgente — notificar en el siguiente ciclo de revisión |

---

### A8 — Refresh con antigüedad > 24 horas en producción

| Severidad | Media |
|---|---|
| Condición | `martLastRefresh` > 24 horas en día hábil |
| Impacto | Cliente ve datos con 1 día de atraso |
| Respuesta | Verificar scheduler y ejecutar retry manual |

---

### A9 — healthScore < 80

| Severidad | Media |
|---|---|
| Condición | `healthScore` < 80 |
| Impacto | Sistema operativo pero con degradación |
| Respuesta | Revisar todos los campos del refresh-status para identificar causa |

---

### A10 — Endpoint dashboard no responde HTTP 200

| Severidad | Alta |
|---|---|
| Condición | Alguno de los 7 endpoints de finance devuelve != 200 |
| Impacto | El cliente no puede ver esa vista del dashboard |
| Respuesta | Revisar API logs para el endpoint específico |

Endpoints a monitorear:
```
GET /api/client/bi/finance/refresh-status
GET /api/client/bi/finance/income-statement
GET /api/client/bi/finance/balance-sheet
GET /api/client/bi/finance/ebitda
GET /api/client/bi/finance/cash-flow
GET /api/client/bi/finance/account-classification
GET /api/client/bi/finance/chart-of-accounts
```

---

## Script de monitoreo básico (cron cada hora)

```bash
#!/bin/bash
# /opt/databision/scripts/monitor-health.sh

API_URL="${DataBisionApi__BaseUrl}"
TOKEN="${DATABISION_MONITOR_TOKEN}"
ALERT_EMAIL="campillayparedes@gmail.com"

STATUS=$(curl -s \
  -H "Authorization: Bearer $TOKEN" \
  "${API_URL}/api/client/bi/finance/refresh-status")

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  "${API_URL}/api/client/bi/finance/refresh-status")

if [ "$HTTP_CODE" != "200" ]; then
    echo "ALERTA CRITICA: API no responde (HTTP $HTTP_CODE)" | \
      mail -s "[DataBision] CRITICO: API down" "$ALERT_EMAIL"
    exit 1
fi

HEALTH_SCORE=$(echo "$STATUS" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('healthScore',0))")
UNCLASSIFIED=$(echo "$STATUS" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('unclassifiedAccounts',0))")

if [ "$HEALTH_SCORE" -lt 80 ]; then
    echo "ALERTA: healthScore=$HEALTH_SCORE" | \
      mail -s "[DataBision] ALERTA: healthScore bajo" "$ALERT_EMAIL"
fi

if [ "$UNCLASSIFIED" -gt 0 ]; then
    echo "INFO: $UNCLASSIFIED cuentas sin clasificar" | \
      mail -s "[DataBision] INFO: cuentas sin clasificar" "$ALERT_EMAIL"
fi
```

```cron
# Monitoreo cada hora (8am–8pm días hábiles)
0 8-20 * * 1-5 . /opt/databision/.env && /opt/databision/scripts/monitor-health.sh
```

---

## Herramientas de monitoreo externo recomendadas

| Herramienta | Uso | Costo |
|---|---|---|
| **UptimeRobot** | Monitoreo HTTP del API (alertas email) | Gratis hasta 50 monitores |
| **Sentry** | Captura de excepciones en el API .NET | Gratis tier disponible |
| **Grafana + Loki** | Logs centralizados (Enterprise) | Open source |
| **Azure Monitor** | Si API en Azure App Service | Incluido en Azure |

Para piloto inicial: UptimeRobot + revisión manual diaria del refresh-status es suficiente.
