# Native BI — MART Validation TST (24E)

**Sprint:** 24E  
**Fecha:** 2026-06-21  
**Pre-requisito:** 24D completo — raw.sap_oact, raw.sap_ojdt, raw.sap_jdt1 pobladas

---

## Paso 1: Ejecutar MART refresh

```powershell
dotnet run --project src\DataBision.Extractor -- --transform-mart --company company-dev-001
```

### Salida esperada
```
[INF] === MART Transform: company=company-dev-001 ===
[INF]   mart.refresh_all.gl_accounts: NNN row(s) affected
[INF]   mart.refresh_all.account_balances: NNN row(s) affected
[INF]   mart.refresh_all.income_statement_summary: NNN row(s) affected
[INF]   mart.refresh_all.balance_sheet_summary: NNN row(s) affected
[INF]   mart.refresh_all.ebitda_summary: NNN row(s) affected
[INF] === MART base done — 5 object(s) in XXXms ===
[INF] === MART process-dashboards done — 12 object(s) in XXXms ===
[INF] === MART Transform: DONE (base XXXms + processes XXXms) ===
```

**Regla:** Si `mart.refresh_all` falla → no mostrar dashboards como validados.

---

## Paso 2: Verificar conteos STG/MART

### SQL a ejecutar en Supabase (SQL Editor)

```sql
-- ── STG ──────────────────────────────────────────────────────────────
SELECT 'stg.gl_account'       AS tabla, COUNT(*) FROM stg.gl_account       WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'stg.journal_entry'    AS tabla, COUNT(*) FROM stg.journal_entry    WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'stg.journal_entry_line' AS tabla, COUNT(*) FROM stg.journal_entry_line WHERE company_id = 'company-dev-001'

-- ── MART ─────────────────────────────────────────────────────────────
UNION ALL
SELECT 'mart.gl_accounts'              AS tabla, COUNT(*) FROM mart.gl_accounts              WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'mart.account_balances'         AS tabla, COUNT(*) FROM mart.account_balances         WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'mart.income_statement_summary' AS tabla, COUNT(*) FROM mart.income_statement_summary WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'mart.balance_sheet_summary'    AS tabla, COUNT(*) FROM mart.balance_sheet_summary    WHERE company_id = 'company-dev-001'
UNION ALL
SELECT 'mart.ebitda_summary'           AS tabla, COUNT(*) FROM mart.ebitda_summary           WHERE company_id = 'company-dev-001'

ORDER BY tabla;
```

### Resultado conteos

| Tabla | Filas | Estado |
|---|---|---|
| stg.gl_account | | ☐ > 0 |
| stg.journal_entry | | ☐ > 0 |
| stg.journal_entry_line | | ☐ > 0 (puede ser 0 si JDT1 vacío) |
| mart.gl_accounts | | ☐ > 0 |
| mart.account_balances | | ☐ > 0 |
| mart.income_statement_summary | | ☐ > 0 |
| mart.balance_sheet_summary | | ☐ > 0 |
| mart.ebitda_summary | | ☐ > 0 |

---

## Paso 3: Verificar clasificación

```sql
-- Cuentas sin clasificar
SELECT classification, COUNT(*) 
FROM mart.gl_accounts 
WHERE company_id = 'company-dev-001'
GROUP BY classification
ORDER BY COUNT(*) DESC;

-- Detalle de unclassified (si hay)
SELECT code, name, account_type, group_mask
FROM mart.gl_accounts
WHERE company_id = 'company-dev-001'
  AND classification = 'unclassified'
ORDER BY code;
```

| Clasificación | Cantidad | Acción requerida |
|---|---|---|
| revenue | | |
| cogs | | |
| opex | | |
| assets | | |
| liabilities | | |
| equity | | |
| financial | | |
| tax | | |
| **unclassified** | | ⚠️ Revisar si > 0 |

**Nota para PCGE Perú:** Prefijos 4x = liabilities (NO revenue), 70-79 = revenue, 60-69 = cogs/expenses.

---

## Paso 4: Validar endpoints dashboard

La API debe estar corriendo en `http://localhost:5103`.

```powershell
# Health check primero
Invoke-RestMethod "http://localhost:5103/api/health" | ConvertTo-Json

# Readiness
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/readiness?companyId=ksdepor" | ConvertTo-Json -Depth 5

# Validations
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/validations?companyId=ksdepor" | ConvertTo-Json -Depth 5

# Income Statement
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/income-statement?companyId=ksdepor" | ConvertTo-Json -Depth 3

# Balance Sheet
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/balance-sheet?companyId=ksdepor" | ConvertTo-Json -Depth 3

# EBITDA
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/ebitda?companyId=ksdepor" | ConvertTo-Json -Depth 3

# Chart of Accounts
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/chart-of-accounts?companyId=ksdepor" | ConvertTo-Json -Depth 2

# Refresh Status
Invoke-RestMethod "http://localhost:5103/api/client/bi/finance/refresh-status?companyId=ksdepor" | ConvertTo-Json -Depth 5
```

### Resultado endpoints

| Endpoint | HTTP Status | `overall_status` / clave principal | Estado |
|---|---|---|---|
| `/readiness` | | | ☐ OK |
| `/validations` | | `healthScore: ___` | ☐ OK |
| `/income-statement` | | `periods: ___` | ☐ OK |
| `/balance-sheet` | | `periods: ___` | ☐ OK |
| `/ebitda` | | `periods: ___` | ☐ OK |
| `/chart-of-accounts` | | `accounts: ___` | ☐ OK |
| `/refresh-status` | | `overallStatus: ___` | ☐ OK |

**Meta:** 7/7 HTTP 200

---

## Paso 5: Validación contable básica (sin contador)

### P&L — consistencia interna
```sql
-- Verificar que ingresos y gastos tienen datos
SELECT 
  period_year, period_month,
  revenue,
  cogs,
  gross_profit,
  opex,
  ebitda,
  net_income
FROM mart.ebitda_summary
WHERE company_id = 'company-dev-001'
ORDER BY period_year, period_month;
```

| Período | Revenue | COGS | Gross Profit | OPEX | EBITDA | Net Income |
|---|---|---|---|---|---|---|
| | | | | | | |

### Balance — cuadre básico
```sql
-- Verificar que activos ≈ pasivos + patrimonio (tolerancia 1%)
SELECT 
  period_year, period_month,
  SUM(CASE WHEN classification = 'assets'      THEN balance ELSE 0 END) AS total_assets,
  SUM(CASE WHEN classification = 'liabilities' THEN balance ELSE 0 END) AS total_liabilities,
  SUM(CASE WHEN classification = 'equity'      THEN balance ELSE 0 END) AS total_equity,
  ABS(
    SUM(CASE WHEN classification = 'assets'      THEN balance ELSE 0 END) -
    SUM(CASE WHEN classification = 'liabilities' THEN balance ELSE 0 END) -
    SUM(CASE WHEN classification = 'equity'      THEN balance ELSE 0 END)
  ) AS diferencia
FROM mart.account_balances
WHERE company_id = 'company-dev-001'
GROUP BY period_year, period_month
ORDER BY period_year, period_month;
```

| Período | Activos | Pasivos | Patrimonio | Diferencia | Cuadra |
|---|---|---|---|---|---|
| | | | | | ☐ Sí / ☐ No |

---

## Decisión Final 24E

| Criterio | Resultado | Estado |
|---|---|---|
| mart.refresh_all OK | | ☐ OK / ☐ FAIL |
| mart.gl_accounts > 0 | | ☐ OK / ☐ FAIL |
| mart.income_statement_summary > 0 | | ☐ OK / ☐ FAIL |
| mart.balance_sheet_summary > 0 | | ☐ OK / ☐ FAIL |
| mart.ebitda_summary > 0 | | ☐ OK / ☐ FAIL |
| endpoints 7/7 HTTP 200 | | ☐ OK / ☐ parcial |
| unclassified documentadas | | ☐ OK / ☐ pendiente |

**Conclusión demo interna:**
- healthScore: ___
- Períodos con datos: ___
- Balance cuadra: ☐ Sí / ☐ No (diferencia: ___)
- Unclassified críticas: ___

**Decisión:** ☐ **Continuar a 24F — Go/No-Go**  /  ☐ **DETENER**
