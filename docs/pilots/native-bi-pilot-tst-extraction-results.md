# Native BI — Extraction Results TST (24D)

**Sprint:** 24D  
**Fecha:** 2026-06-21  
**Pre-requisito:** 24C completo — dry-run OK

---

## FASE 1: Extracción OACT (Chart of Accounts)

### Comando
```powershell
dotnet run --project src\DataBision.Extractor -- --profile tst --object OACT --send
```

### Salida esperada
```
[INF] Profile resolved: id=1 name=tst db=CLTSTKSDEPOR concurrency=3
[INF] SAP credentials loaded from profile. DB=CLTSTKSDEPOR
[INF] OACT: full-refresh (pageSize=100)
[INF] OACT: NNN accounts in XXXms (select=Code,Name,GroupMask,AccountType,...)
[INF]   OACT sample: Code=..., Name=..., Type=..., Postable=...
[INF] OACT sent: inserted=NNN, updated=0, skipped=0 in XXXms
```

**Reglas:**
- Si `OACT: 0 accounts` → detener. No avanzar a OJDT.
- Si send falla (HTTP error) → revisar Supabase connection string y ingest endpoint.

### Resultado OACT

| Métrica | Valor obtenido |
|---|---|
| Cuentas extraídas | |
| select utilizado | |
| Tiempo extracción (ms) | |
| inserted | |
| updated | |
| skipped | |
| Errores | |
| Exit code | |

#### Verificación en Supabase
```sql
SELECT COUNT(*) FROM raw.sap_oact WHERE company_id = 'company-dev-001';
SELECT "AccountType", COUNT(*) FROM raw.sap_oact 
WHERE company_id = 'company-dev-001' 
GROUP BY "AccountType" ORDER BY COUNT(*) DESC;
```

| Consulta | Resultado |
|---|---|
| COUNT raw.sap_oact | |
| AccountType breakdown | |

**Decisión OACT:** ☐ OK — avanzar a OJDT  /  ☐ DETENER

---

## FASE 2: Extracción OJDT (Journal Entries + JDT1)

### Comando
```powershell
dotnet run --project src\DataBision.Extractor -- --profile tst --object OJDT --send
```

**Nota:** Primera extracción es full (no hay checkpoint). Las siguientes son incrementales por `ReferenceDate`.

### Salida esperada
```
[INF] Profile resolved: id=1 name=tst db=CLTSTKSDEPOR concurrency=3
[INF] OJDT: no checkpoint — full extraction (pageSize=100)
[INF] OJDT: NNN entries in XXXms (select=..., filter=FULL)
[INF] OJDT-PROBE-17A: GET JournalEntries(N) — probing for embedded lines
[INF] OJDT-PROBE-17A: FOUND 'JournalEntryLines' — X lines in GET JournalEntries(N). Single-record GET EXPOSES LINES.
[INF] OJDT-17C: extracting lines via individual GET (NNN entries, property='JournalEntryLines', concurrency=3)
[INF] OJDT-17C: extracted NNN lines from NNN/NNN entries (0 failed) in XXXms (~Xms/GET, concurrency=3)
[INF] OJDT: mapped NNN headers, NNN lines
[INF] OJDT headers sent: inserted=NNN, updated=0, skipped=0
[INF] JDT1 lines sent: inserted=NNN, updated=0, skipped=0
```

**Casos posibles en PROBE-17A:**
- `FOUND 'JournalEntryLines'` → líneas extraídas individualmente (Sprint 17C) ✅
- `FOUND 'Lines'` → misma lógica, propiedad diferente ✅
- `GET does not expose JDT1 lines` → fallback a JournalEntryLines top-level
- `JournalEntryLines top-level not accessible` → JDT1 vacío — documentar

### Resultado OJDT/JDT1

| Métrica | Valor obtenido |
|---|---|
| Entradas OJDT extraídas | |
| Estrategia de líneas | `$expand` / `individual GET` / `top-level` / vacío |
| Propiedad de líneas encontrada | `JournalEntryLines` / `Lines` / N/A |
| Líneas JDT1 extraídas | |
| Concurrencia GET (17C) | 3 |
| Tiempo total (ms) | |
| Failed GETs individuales | |
| OJDT inserted | |
| JDT1 inserted | |
| Exit code | |

#### Verificación en Supabase
```sql
-- OJDT headers
SELECT COUNT(*) FROM raw.sap_ojdt WHERE company_id = 'company-dev-001';

-- JDT1 lines
SELECT COUNT(*) FROM raw.sap_jdt1 WHERE company_id = 'company-dev-001';

-- Rango de fechas de los asientos
SELECT 
  MIN("ReferenceDate") as desde,
  MAX("ReferenceDate") as hasta,
  COUNT(DISTINCT "JdtNum") as entradas_unicas
FROM raw.sap_ojdt
WHERE company_id = 'company-dev-001';
```

| Consulta | Resultado |
|---|---|
| COUNT raw.sap_ojdt | |
| COUNT raw.sap_jdt1 | |
| ReferenceDate desde | |
| ReferenceDate hasta | |
| Entradas únicas | |

**Ratio líneas/entradas (esperado > 1):** ___

---

## Decisión Final 24D

| Criterio | Resultado | Estado |
|---|---|---|
| raw.sap_oact > 0 | | ☐ OK / ☐ FAIL |
| raw.sap_ojdt > 0 | | ☐ OK / ☐ FAIL |
| raw.sap_jdt1 > 0 | | ☐ OK / ☐ FAIL (puede ser vacío en SL limitado) |
| Sin errores críticos | | ☐ OK / ☐ FAIL |
| Sin secretos en logs | | ☐ OK / ☐ FAIL |

**Decisión:** ☐ **Continuar a 24E — MART**  /  ☐ **DETENER**

---

## Advertencias/Observaciones

_(Registrar aquí cualquier warning, performance issues, o comportamientos inesperados)_

| # | Observación | Impacto |
|---|---|---|
| 1 | | |
| 2 | | |
