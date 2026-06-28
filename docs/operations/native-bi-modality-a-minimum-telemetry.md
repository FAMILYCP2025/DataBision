# Native BI Modalidad A — Telemetría Mínima Centralizada

**DataBision · Junio 2026**  
**Versión:** 1.0 — Gate 4 pre-deployment Modalidad A  
**Estado:** Inventario de telemetría existente + brechas identificadas

---

## 1. Diagnóstico de situación actual

Antes de este documento, la telemetría de Modalidad A dependía principalmente de **logs en disco** en el servidor del cliente. Esto tiene dos problemas críticos:

1. DataBision no puede saber si el refresh falló **sin entrar al servidor del cliente**
2. El cliente no puede saber desde el browser si sus datos están actualizados

Este documento describe la telemetría **que ya existe** (no inventada) y cómo usarla como MVP de monitoreo centralizado.

---

## 2. Telemetría existente en código

### 2.1 ops.extractor_run — Registro de cada extracción

**Qué es:** Tabla en Supabase (schema `ops`) que registra cada ejecución del extractor.

**Confirmado en código:** `Program.cs` ejecuta queries `SELECT COUNT(*) FROM ops.extractor_run` en `--validate-ops`.

**Campos disponibles:**
- `sap_object` — objeto extraído (OACT, OJDT, etc.)
- `status` — estado: `OK`, `ERROR`, `PARTIAL`
- `pages_fetched` — páginas paginadas desde SAP SL
- `rows_extracted` — filas extraídas
- `started_at_utc` — timestamp de inicio
- `company_id` — identificador del tenant

**Cómo consultar:**
```bash
# Desde el extractor
dotnet DataBision.Extractor.dll --validate-ops --company ksdepor-analytics

# Salida esperada:
# [OPS-02] extractor_run: total=45, errors=0
# [OPS-04] transform_run: 15 runs
# [OPS-05] Recent OJDT: sap_object=OJDT status=OK pages_fetched=3 rows_extracted=68 started_at_utc=2026-06-23T02:05:12
```

**Cómo consultar desde Supabase directamente:**
```sql
-- Últimas 5 extracciones de OJDT
SELECT sap_object, status, rows_extracted, started_at_utc
FROM ops.extractor_run
WHERE company_id = 'ksdepor-analytics'
ORDER BY started_at_utc DESC
LIMIT 5;

-- Última extracción exitosa
SELECT MAX(started_at_utc) as ultima_extraccion
FROM ops.extractor_run
WHERE company_id = 'ksdepor-analytics' AND status = 'OK';

-- Alerta: OJDT sin extracción en 25h
SELECT CASE
    WHEN MAX(started_at_utc) < NOW() - INTERVAL '25 hours' THEN 'STALE'
    ELSE 'OK'
END as estado
FROM ops.extractor_run
WHERE company_id = 'ksdepor-analytics' AND sap_object = 'OJDT' AND status = 'OK';
```

### 2.2 ops.transform_run — Registro de cada refresh STG/MART

**Qué es:** Tabla en Supabase (schema `ops`) que registra cada ejecución de `--transform` o `--transform-mart`.

**Confirmado en código:** `Program.cs` ejecuta `SELECT COUNT(*) FROM ops.transform_run` en `--validate-ops`.

**Cómo consultar:**
```sql
-- Último MART refresh
SELECT status, started_at_utc, completed_at_utc
FROM ops.transform_run
WHERE company_id = 'ksdepor-analytics'
ORDER BY started_at_utc DESC
LIMIT 1;

-- Alerta: MART sin refresh en 25h
SELECT CASE
    WHEN MAX(started_at_utc) < NOW() - INTERVAL '25 hours' THEN 'STALE'
    ELSE 'OK'
END as estado
FROM ops.transform_run
WHERE company_id = 'ksdepor-analytics' AND status = 'OK';
```

### 2.3 /api/client/bi/finance/refresh-status — Estado de refresh visible desde web

**Qué es:** Endpoint HTTP que retorna el estado actual de los datos financieros del tenant. Es la fuente central de estado visible desde el browser.

**Confirmado en código:** `ClientBiFinanceController.cs:159` → `[HttpGet("refresh-status")]` → `GetFinanceRefreshStatusAsync`.

**Cómo consultar:**
```http
GET https://{slug}.databision.app/api/client/bi/finance/refresh-status
Authorization: Bearer {jwt}
```

**Respuesta esperada (campos clave):**
```json
{
  "data": {
    "healthScore": 100,
    "lastSuccessfulRefresh": "2026-06-23T04:05:22Z",
    "oactRows": 20,
    "ojdtRows": 187,
    "jdt1Rows": 740,
    "martStatus": "OK",
    "unclassifiedAccounts": 0,
    "refreshAgeHours": 2.1
  }
}
```

**Interpretación del healthScore:**

| healthScore | Estado | Acción |
|---|---|---|
| 100 | Todo correcto | Normal |
| 80–99 | Degradado leve | Monitorear — puede ser normal |
| 50–79 | Degradado significativo | Investigar refresh-status completo |
| < 50 | Crítico | Ejecutar retry manual y escalar |
| 0 | Sin datos | Primera extracción pendiente |

---

## 3. Qué se registra centralmente vs. qué queda en log local

| Dato | Dónde está | Accesible sin entrar al servidor |
|---|---|---|
| Estado de extractor (OK/ERROR) | `ops.extractor_run` en Supabase | ✅ Sí — via --validate-ops o SQL directo |
| Estado de MART refresh | `ops.transform_run` en Supabase | ✅ Sí |
| healthScore y conteos de datos | `/api/client/bi/finance/refresh-status` | ✅ Sí — via API HTTP |
| Dashboard actualizado | Frontend DataBision | ✅ Sí — visible desde browser |
| Errores detallados del extractor | Logs en disco (servidor cliente) | ❌ No — requiere acceso SSH/RDP |
| Stack trace de excepciones | Logs en disco (servidor cliente) | ❌ No |
| Salida de stdout del proceso | Logs en disco (servidor cliente) | ❌ No |

**Conclusión de brecha:** Los errores detallados aún requieren acceso al servidor para diagnóstico completo. Este es un gap aceptable para el MVP comercial — los síntomas (datos stale, healthScore bajo) son visibles centralmente; la causa raíz requiere acceso al log local.

---

## 4. Watchdog de refresh-status

Para verificar el estado central sin acceder al servidor del cliente:

```bash
# Script de watchdog (ejecutar desde DataBision o desde el equipo del operador)
# Requiere JWT válido del tenant

SLUG="ksdepor"
JWT="eyJ..."  # Obtener via login

RESPONSE=$(curl -s -H "Authorization: Bearer $JWT" \
    "https://${SLUG}.databision.app/api/client/bi/finance/refresh-status")

HEALTH_SCORE=$(echo $RESPONSE | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['data']['healthScore'])")
LAST_REFRESH=$(echo $RESPONSE | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['data']['lastSuccessfulRefresh'])")

echo "healthScore=$HEALTH_SCORE"
echo "lastRefresh=$LAST_REFRESH"

if [ "$HEALTH_SCORE" -lt 80 ]; then
    echo "ALERTA: healthScore=$HEALTH_SCORE — requiere atención"
fi
```

---

## 5. Alertas mínimas a configurar

Estas alertas corresponden al documento [native-bi-monitoring-and-alerts.md](native-bi-monitoring-and-alerts.md). Para MVP, priorizar:

| ID | Alerta | Fuente | Umbral |
|---|---|---|---|
| A-01 | API DataBision no responde | `/api/client/bi/finance/refresh-status` HTTP timeout | > 30s sin respuesta |
| A-02 | Extractor OJDT inactivo | `ops.extractor_run` o `refresh-status.lastSuccessfulRefresh` | > 25 horas sin extracción OK |
| A-03 | MART refresh fallido | `ops.transform_run` | Último registro status='ERROR' |
| A-04 | healthScore bajo | `/api/client/bi/finance/refresh-status` | healthScore < 80 |
| A-05 | Cuentas sin clasificar | `refresh-status.unclassifiedAccounts` | > 0 |

**Implementación MVP:** Monitoreo manual diario del `refresh-status` endpoint. La implementación de alertas automáticas via email/WhatsApp es un item del roadmap Q4 2026.

---

## 6. Cómo validar que el último run fue exitoso (post-extracción)

### Desde el extractor (en el servidor del cliente)

```bash
dotnet DataBision.Extractor.dll --validate-ops --company ksdepor-analytics
```

Salida esperada (post-extracción exitosa):
```
[OPS-02] extractor_run: total=N, errors=0
[OPS-04] transform_run: N runs
[OPS-05] Recent: OJDT OK pages=3 rows=68 at=2026-06-23T02:05:12
=== --validate-ops: DONE ===
```

### Desde la API (sin acceso al servidor)

```http
GET /api/client/bi/finance/refresh-status
```

Interpretar:
- `healthScore >= 80` → extracción reciente exitosa
- `lastSuccessfulRefresh` dentro de las últimas 25 horas → sin datos stale
- `unclassifiedAccounts = 0` → clasificación completa

### Desde Supabase (DataBision directamente)

```sql
-- Resumen de estado de todos los tenants
SELECT 
    company_id,
    MAX(started_at_utc) FILTER (WHERE sap_object='OJDT' AND status='OK') as last_ojdt_ok,
    MAX(started_at_utc) FILTER (WHERE sap_object='OACT' AND status='OK') as last_oact_ok,
    CASE 
        WHEN MAX(started_at_utc) FILTER (WHERE sap_object='OJDT' AND status='OK') 
             < NOW() - INTERVAL '25 hours' THEN 'STALE'
        ELSE 'OK'
    END as ojdt_status
FROM ops.extractor_run
GROUP BY company_id;
```

---

## 7. Backlog — Telemetría futura (no comprometida para MVP)

| Item | Prioridad | Sprint estimado |
|---|---|---|
| Email/WhatsApp alert automático cuando healthScore < 80 | Alta | Q4 2026 |
| Dashboard de estado de todos los tenants en panel Admin | Media | Q3 2026 |
| Push de logs del extractor a Supabase (table `ops.extractor_log`) | Media | Q4 2026 |
| Notificación automática cuando `unclassifiedAccounts > 0` | Alta | Q4 2026 |
| SLA tracking por tenant | Baja | Q1 2027 |

---

## Criterio GO Gate 4

| Criterio | Estado |
|---|---|
| Cliente puede verificar desde web si datos están actualizados (`refresh-status`) | ✅ Implementado — endpoint existente |
| DataBision puede verificar estado sin entrar al servidor (`validate-ops`, `refresh-status`) | ✅ Implementado |
| Logs locales no son la única fuente operativa | ✅ — `ops.extractor_run`, `ops.transform_run`, `refresh-status` son fuentes centrales |
| Existe inventario de qué se registra centralmente vs. localmente | ✅ Sección 3 de este documento |
| Alertas mínimas identificadas | ✅ Sección 5 — implementación MVP manual, automática en roadmap |

**Estado Gate 4: GO ✅** (con brecha documentada: alertas automáticas en roadmap, no en MVP)
