# DataBision — Monitoreo y Alertas (ops schema)

**Sprint:** 8D  
**Fecha:** 2026-06-10  
**Schema:** `ops` (PostgreSQL/Supabase)

---

## Arquitectura

El schema `ops` centraliza la observabilidad del pipeline ETL. No depende de schemas externos salvo una consulta defensiva a `mart.finance_executive_daily` para la alerta AR_OVERDUE_HIGH.

```
Extractor → ops.extractor_run / ops.extractor_page_log
Transform → ops.transform_run
DQ checks → ops.data_quality_issue  (vía ops.log_data_quality_issue)
Alertas   → ops.alert_event         (vía ops.evaluate_alert_rules)
Health    → ops.pipeline_health     (vía ops.refresh_pipeline_health)
```

---

## Tablas

### ops.extractor_run
Registro por ejecución de extracción (1 fila por objeto por run).

| Columna | Tipo | Descripción |
|---------|------|-------------|
| id | BIGSERIAL PK | ID autoincremental |
| company_id | TEXT | Tenant (ej. "ksdepor") |
| sap_object | TEXT | Código SAP (ej. "OINV") |
| started_at_utc / finished_at_utc | TIMESTAMPTZ | Inicio y fin |
| status | TEXT | RUNNING / SUCCESS / ERROR / PARTIAL |
| pages_fetched | INT | Páginas paginadas |
| rows_extracted | INT | Filas leídas desde SAP |
| rows_inserted / rows_updated | INT | Filas escritas en STG |
| hit_max_pages | BOOL | TRUE si se llegó al límite MaxPages |
| last_error | TEXT | Mensaje del último error |
| watermark_date | TEXT | Fecha watermark usada en el run |

### ops.extractor_page_log
Log granular por página dentro de un run.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| run_id | BIGINT FK | Referencia a extractor_run |
| sap_object | TEXT | Objeto del run |
| page_number | INT | Número de página (1-based) |
| skip_offset / top_count | INT | Parámetros $skip y $top |
| rows_received | INT | Filas recibidas en esta página |
| elapsed_ms | BIGINT | Tiempo de la página |
| status / error_code / error_message | TEXT | Estado de la página |

### ops.transform_run
Registro por ejecución del proceso de transformación MART.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| transform_type | TEXT | ALL / SALES / PURCHASING / INVENTORY / FINANCE |
| status | TEXT | RUNNING / SUCCESS / ERROR |
| objects_refreshed | INT | Cuántos MART fueron refrescados |

### ops.data_quality_issue
Problemas de calidad detectados en STG o MART.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| issue_type | TEXT | NULL_KEY / DUPLICATE / NEGATIVE_QTY / etc. |
| severity | TEXT | WARNING / ERROR / CRITICAL |
| affected_rows | INT | Filas afectadas |
| sample_key | TEXT | Ejemplo de DocEntry/CardCode afectado |
| is_resolved | BOOL | FALSE hasta que se corrija manualmente |

### ops.alert_rule
Catálogo de reglas de alerta (semilla fija, 8 reglas).

| rule_code | severity | threshold | Condición |
|-----------|----------|-----------|-----------|
| EXTRACTOR_NOT_RUN_RECENTLY | ERROR | 24h | Sin run exitoso en 24h |
| MART_EMPTY | WARNING | 0 rows | MART principal vacía |
| STG_EMPTY | WARNING | 0 rows | STG principal vacía |
| SALES_DROP_DAILY | WARNING | 50% | Caída ventas > 50% vs promedio 30d |
| STOCKOUT_ITEMS | WARNING | 10 items | Items con stock <= 0 > umbral |
| AR_OVERDUE_HIGH | WARNING | 30% | CxC vencida > 30% |
| DATA_QUALITY_ERRORS | WARNING | 5 issues | DQ sin resolver > 5 |
| TRANSFORM_FAILED | ERROR | — | Último transform en ERROR |

### ops.alert_event
Disparos de alertas por empresa.

### ops.pipeline_health
Vista materializada de salud (1 fila por empresa, upsert).

| Columna | Descripción |
|---------|-------------|
| extractor_status / transform_status | Estado del último run |
| active_alerts | Alertas no resueltas |
| dq_errors_unresolved | DQ issues abiertos |
| objects_extracted | Objetos SAP con al menos 1 run exitoso |
| health_score | 0–100; se descuentan puntos por fallos |

**health_score cálculo:**
- Base: 100
- extractor ERROR/NEVER_RUN: -40
- transform ERROR/NEVER_RUN: -30
- active_alerts > 0: -5 por alerta (máx -20)
- dq_errors > 0: -2 por error (máx -10)
- Mínimo: 0

---

## Funciones

### ops.refresh_pipeline_health(p_company_id TEXT) → VOID
Recalcula `ops.pipeline_health` para la empresa. Upsert basado en `company_id`.  
**Cuándo llamar:** al final de cada run de extracción y transformación.

### ops.evaluate_alert_rules(p_company_id TEXT) → INT
Evalúa las reglas activas y crea eventos en `ops.alert_event`.  
Retorna el número de alertas disparadas.  
Llama `refresh_pipeline_health` al final.  
**Cuándo llamar:** al final de cada ciclo completo extracción+transformación.

**Reglas evaluadas actualmente:**
- EXTRACTOR_NOT_RUN_RECENTLY (sin run en 24h)
- TRANSFORM_FAILED (último transform en ERROR últimas 48h)
- DATA_QUALITY_ERRORS (más de 5 DQ sin resolver)
- AR_OVERDUE_HIGH (si mart.finance_executive_daily existe: ar_overdue_pct > 0.30)

**Reglas pendientes de activar** (requieren STG de objetos preparados):
- MART_EMPTY, STG_EMPTY, SALES_DROP_DAILY, STOCKOUT_ITEMS → Sprint 8F

### ops.log_data_quality_issue(...) → BIGINT
Inserta un issue en `ops.data_quality_issue` y retorna el ID.  
**Signatura:**
```sql
ops.log_data_quality_issue(
    p_company_id    TEXT,
    p_sap_object    TEXT,
    p_issue_type    TEXT,
    p_severity      TEXT,
    p_description   TEXT,
    p_affected_rows INTEGER DEFAULT 0,
    p_sample_key    TEXT    DEFAULT NULL
) RETURNS BIGINT
```

---

## Invocación desde Extractor (roadmap)

```bash
# Después de extraction + transform:
SELECT ops.evaluate_alert_rules('ksdepor');

# Health snapshot:
SELECT * FROM ops.pipeline_health WHERE company_id = 'ksdepor';

# Ver alertas activas:
SELECT rule_code, severity, message, triggered_at_utc
FROM ops.alert_event
WHERE company_id = 'ksdepor' AND is_resolved = FALSE
ORDER BY triggered_at_utc DESC;
```

---

## Notas de implementación

- **Resolución de alertas:** `is_resolved = TRUE` debe hacerse manualmente o via job de limpieza. No hay auto-resolve implementado en Sprint 8D.
- **Retención:** No hay TTL automático sobre `extractor_page_log`. Para instancias grandes, considerar un job de limpieza mensual.
- **SALES_DROP_DAILY:** Requiere lógica en el Extractor (comparar ventas del día vs promedio 30d en MART). Se activa en Sprint 8F cuando MART tenga datos históricos suficientes.
- **health_score:** Es orientativo. No es una métrica SLA. Un score 100 indica que todos los runs pasaron — no garantiza que los datos sean correctos.
