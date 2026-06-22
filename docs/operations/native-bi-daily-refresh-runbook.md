# Native BI Finance — Runbook de Refresh Diario

**Sprint 28 · DataBision · Junio 2026**  
**Audiencia:** Consultor DataBision / Operador técnico del cliente

---

## Resumen del proceso diario

| Proceso | Frecuencia | Hora recomendada | Duración estimada |
|---|---|---|---|
| Extracción OJDT + JDT1 | Diaria (L–D) | 02:00 AM | 5–15 min |
| Refresh MART | Diaria (L–D) | 02:30 AM | 2–5 min |
| Extracción OACT | Semanal (Lunes) | 01:00 AM | 2–5 min |
| Validación refresh-status | Manual / monitor | Diario 09:00 AM | 2 min |

---

## Comandos de extracción

```bash
# Extracción OJDT (diaria) — desde directorio del extractor
dotnet DataBision.Extractor.dll --profile [CLIENTE] --object OJDT

# Extracción OACT (semanal)
dotnet DataBision.Extractor.dll --profile [CLIENTE] --object OACT

# NUNCA ejecutar:
# --object JDT1 (JDT1 se extrae automáticamente vía GET individual JournalEntries(N))
# --object ALL  (no usar en producción)
```

---

## Proceso OACT (semanal)

### ¿Qué hace?
Extrae el plan de cuentas completo (Chart of Accounts) desde SAP B1.

### Cuándo ejecutar
- Lunes 01:00 AM (automático)
- Manualmente si el contador agrega o modifica cuentas en SAP

### Verificación post-ejecución
```bash
# Verificar que hay cuentas en la tabla RAW
# Usar el endpoint de refresh-status o consultar directamente:
curl -H "Authorization: Bearer [TOKEN]" \
  "https://[API_URL]/api/client/bi/finance/refresh-status"
# Buscar: oact_count > 0
```

### ¿Qué hacer si falla OACT?
1. Verificar logs del extractor: `logs/extractor-[FECHA].log`
2. Confirmar que SAP Service Layer responde: `curl https://[SL_URL]/b1s/v1/ChartOfAccounts?$top=1`
3. Verificar credenciales SAP: variable `SAP_PASSWORD_[CLIENTE]` configurada
4. Si el error es timeout: aumentar `Timeout` en el perfil de conexión
5. Re-ejecutar manualmente: `dotnet DataBision.Extractor.dll --profile [CLIENTE] --object OACT`
6. Si persiste: escalar a DataBision con log completo

---

## Proceso OJDT + JDT1 (diario)

### ¿Qué hace?
1. Extrae asientos del período actual desde SAP (`OJDT` — headers)
2. Para cada asiento, hace un GET individual `JournalEntries(N)` que incluye las líneas `JDT1`
3. Inserta los datos en las tablas RAW de Supabase
4. Llama al endpoint `/api/ingest/ojdt` para procesar hacia staging

### ¿Por qué GET individual para JDT1?
SAP Service Layer no permite filtrar directamente `JDT1`. El extractor obtiene cada asiento con `GET /JournalEntries(N)` que incluye las líneas automáticamente.

### Verificación post-ejecución
```bash
curl -H "Authorization: Bearer [TOKEN]" \
  "https://[API_URL]/api/client/bi/finance/refresh-status"
# Buscar: ojdt_count > 0, jdt1_count > 0, last_extraction reciente
```

### ¿Qué hacer si falla OJDT?
1. Revisar log del extractor para identificar el error específico
2. Errores comunes:
   - `401 Unauthorized` → credenciales SAP expiradas o incorrectas
   - `Connection refused` → SAP Service Layer no disponible
   - `Timeout` → SAP lento, aumentar timeout en perfil
   - `0 records returned` → período sin asientos (posiblemente correcto en fin de semana)
3. Re-ejecutar manualmente el mismo día antes de las 6 AM
4. Si el error persiste más de 2 días: alertar a cliente y escalar

### ¿Qué hacer si falla MART?

---

## Proceso MART (diario)

### ¿Qué hace?
Regenera las vistas financieras en la capa MART desde los datos de staging:
- `mart.finance_income_statement` (P&L)
- `mart.finance_balance_sheet` (Balance)
- `mart.finance_ebitda` (EBITDA)

### Cuándo ejecutar
- 02:30 AM automático (después de OJDT)
- Manualmente después de un cambio de clasificación de cuentas

### Verificación post-MART
```bash
curl -H "Authorization: Bearer [TOKEN]" \
  "https://[API_URL]/api/client/bi/finance/income-statement?period=[YYYY-MM]"
# Si retorna HTTP 200 con datos → MART OK
```

### ¿Qué hacer si falla MART?
1. Revisar log del API: buscar errores en `refresh_finance_mart`
2. Verificar que OJDT se completó: si OJDT falló, MART no tiene datos frescos
3. Ejecutar MART manualmente via API:
   ```bash
   curl -X POST -H "Authorization: Bearer [ADMIN_TOKEN]" \
     "https://[API_URL]/api/admin/bi/finance/refresh-mart?company_id=[COMPANY_ID]"
   ```
4. Si el error es SQL: revisar que las funciones MART existen en Supabase
5. Si las funciones faltan: re-ejecutar migración `AddAccountingMartFunctions`

---

## Endpoint refresh-status — Interpretación

`GET /api/client/bi/finance/refresh-status`

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

| Campo | Valor esperado | Acción si anómalo |
|---|---|---|
| `lastExtraction` | < 25 horas | Si > 25h: extractor no corrió — ejecutar manual |
| `oactCount` | > 0 (típico: 50–500) | Si 0: OACT no extraído o tabla vacía |
| `ojdtCount` | > 0 en día hábil | Si 0 en día hábil: revisar conexión SAP |
| `jdt1Count` | > ojdtCount | Si igual: asientos sin líneas — anómalo |
| `martLastRefresh` | < 25 horas | Si > 25h: MART no refrescó |
| `status` | `"healthy"` | Si `"warning"` o `"error"`: revisar campos anteriores |
| `unclassifiedAccounts` | 0 idealmente | Si > 0: hay cuentas sin clasificar en el P&L |
| `healthScore` | 100 | < 80: alerta — revisar causas |

---

## Retry manual completo (procedimiento de emergencia)

Si el proceso diario falló completamente:

```bash
# 1. OACT (si es lunes o si hay cambios en cuentas)
dotnet DataBision.Extractor.dll --profile [CLIENTE] --object OACT

# 2. OJDT (asientos del período actual)
dotnet DataBision.Extractor.dll --profile [CLIENTE] --object OJDT

# 3. MART (regenerar vistas financieras)
curl -X POST -H "Authorization: Bearer [ADMIN_TOKEN]" \
  "https://[API_URL]/api/admin/bi/finance/refresh-mart?company_id=[COMPANY_ID]"

# 4. Validar
curl -H "Authorization: Bearer [TOKEN]" \
  "https://[API_URL]/api/client/bi/finance/refresh-status"
```

---

## Registro de incidentes

Anotar cada incidente con:
- Fecha y hora
- Proceso que falló (OACT / OJDT / MART)
- Error observado
- Acción tomada
- Resultado

Guardar en: `docs/operations/incident-log-[CLIENTE].md`
