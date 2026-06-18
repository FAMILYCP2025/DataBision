# Native BI — KSDEPOR Account Classification Plan

Sprint 15D — 2026-06-18

---

## Company Setup

| Field | Value |
|---|---|
| Company name | KSDEPOR |
| App DB slug | `ksdepor` (also mapped: `demo`) |
| AnalyticsCompanyId | `company-dev-001` |
| SAP CompanyDB | CLTSTKSDEPOR |
| Extractor config | `appsettings.Development.json` — already configured |

Both `ksdepor` and `demo` slugs resolve to the same `AnalyticsCompanyId = "company-dev-001"`. This means the demo company in the portal and KSDEPOR share the same analytics data.

---

## AnalyticsCompanyId Resolution

The resolver (`AnalyticsCompanyResolver.cs`) uses `NativeBi:CompanySlugMap` in `appsettings.json`:

```json
"NativeBi": {
  "CompanySlugMap": {
    "ksdepor": "company-dev-001",
    "demo":    "company-dev-001"
  }
}
```

To persist this in the app DB (so it survives config changes), run the API once in Development mode — `DatabaseSeeder.SeedAnalyticsCompanyIdsAsync()` will write `AnalyticsCompanyId = "company-dev-001"` to both company rows.

---

## Classification Strategy

Classification rules are stored in `cfg.account_classification_rules` (Supabase) and applied in `mart.refresh_gl_accounts`. Three-tier priority:

1. **Exact account_code match** — highest priority, set by admin via SuperAdmin UI
2. **Format_code prefix match** — covers entire account ranges; rules in `accounting-classification-demo-ksdepor.sql`
3. **SAP account_type fallback** — automatic; likely misclassifies many accounts

The demo SQL file (`sql/native-bi/accounting-classification-demo-ksdepor.sql`) installs format-code prefix rules covering a typical Chilean SAP B1 chart of accounts.

---

## Format-Code Prefix Rules (Demo)

These rules are based on the standard Chilean accounting plan structure. The `format_code` in SAP B1 represents the account hierarchy path.

| format_code prefix | statement_line | Accounts covered |
|---|---|---|
| 11 | current_assets | Caja, efectivo, equivalentes |
| 12 | current_assets | CxC, clientes |
| 13 | current_assets | Inventario, existencias |
| 14 | current_assets | Otros activos corrientes |
| 15 | non_current_assets | Activo fijo, PPE |
| 16 | non_current_assets | Intangibles |
| 17 | non_current_assets | Inversiones LP |
| 18 | non_current_assets | Otros activos NC |
| 21 | current_liabilities | CxP, proveedores |
| 22 | current_liabilities | Deudas bancarias CP |
| 23 | current_liabilities | Remuneraciones por pagar |
| 24 | current_liabilities | IVA, impuestos por pagar |
| 25 | current_liabilities | Otros pasivos corrientes |
| 26 | non_current_liabilities | Deudas LP |
| 27 | non_current_liabilities | Provisiones LP |
| 31 | equity | Capital pagado |
| 32 | equity | Reservas |
| 33 | equity | Utilidades retenidas |
| 34 | equity | Resultado del ejercicio |
| 41–43, 4 | revenue | Ventas netas, otros ingresos operacionales |
| 51–52, 5 | cogs | Costo de mercadería vendida |
| 61–66, 6 | opex | Remuneraciones, arriendos, administración |
| 67 | depreciation | Depreciación y amortización |
| 71, 7 | other_income | Otros ingresos no operacionales |
| 72 | other_expense | Otros gastos no operacionales |
| 81–83, 8 | financial | Intereses, diferencias de cambio |
| 91, 9 | tax | Impuesto a la renta |

---

## ⚠️ Critical Validation Steps (with Client Accountant)

These rules are DEFAULTS, not confirmed truth. Before relying on MART data for financial reporting:

### 1. Verify format_code structure in OACT

```sql
-- Check what format_codes actually exist in KSDEPOR OACT
SELECT DISTINCT LEFT("FormatCode", 2) AS prefix, COUNT(*) AS accounts
FROM "raw"."sap_oact"
WHERE company_id = 'company-dev-001'
GROUP BY LEFT("FormatCode", 2)
ORDER BY prefix;
```

If the prefixes don't follow the 1x/2x/3x/4x/5x/6x/7x/8x/9x pattern, adjust the rules accordingly.

### 2. Identify specific equity accounts

Equity accounts (capital, reserves, retained earnings, result) are critical for balance cuadra. Identify them by code or name:

```sql
SELECT code, name, account_type, format_code
FROM "raw"."sap_oact"
WHERE company_id = 'company-dev-001'
  AND ("Name" ILIKE '%capital%'
    OR "Name" ILIKE '%reserva%'
    OR "Name" ILIKE '%patrimonio%'
    OR "Name" ILIKE '%resultado%'
    OR "Name" ILIKE '%utilidad%')
ORDER BY code;
```

Add explicit account-code rules for each identified equity account:

```sql
INSERT INTO cfg.account_classification_rules (company_id, account_code, format_code, statement_line, created_at, updated_at)
VALUES ('company-dev-001', '<equity-account-code>', NULL, 'equity', NOW(), NOW())
ON CONFLICT (company_id, account_code, format_code) DO UPDATE SET statement_line = 'equity', updated_at = NOW();
```

### 3. Verify revenue sign convention

In some Chilean SAP instances, revenue accounts have credit balances stored as negative debits. After running `mart.refresh_gl_accounts` and `mart.refresh_income_statement`, check:

```sql
SELECT period_year, period_month, statement_line, ROUND(amount, 2)
FROM mart.income_statement_summary
WHERE company_id = 'company-dev-001'
  AND statement_line = 'revenue'
ORDER BY period_year DESC, period_month DESC
LIMIT 6;
```

Revenue should be **positive**. If negative, the sign convention in `mart.refresh_income_statement` may need adjustment for this client.

### 4. Check unclassified accounts

After running `mart.refresh_gl_accounts`, inspect what remains unclassified:

```sql
SELECT code, name, account_type, format_code
FROM mart.gl_accounts
WHERE company_id = 'company-dev-001'
  AND statement_line = 'unclassified'
ORDER BY code;
```

For each unclassified account, add a specific rule. Do NOT add to the unclassified allowlist — find the correct statement_line with the accountant.

---

## Execution Sequence

```bash
# Step 1: Extract OACT (run if not already done)
dotnet run --project src/DataBision.Extractor -- --object OACT --company-id company-dev-001 --send

# Step 2: Apply classification rules (run in Supabase SQL Editor)
# sql/native-bi/accounting-classification-demo-ksdepor.sql

# Step 3: Extract OJDT + JDT1 (if not already done)
dotnet run --project src/DataBision.Extractor -- --object OJDT --company-id company-dev-001 --send

# Step 4: Run MART refresh
# SELECT * FROM mart.refresh_accounting_all('company-dev-001');

# Step 5: Validate unclassified
# SELECT code, name FROM mart.gl_accounts WHERE company_id = 'company-dev-001' AND statement_line = 'unclassified';

# Step 6: Add specific rules for unclassified accounts (via SuperAdmin UI or SQL)

# Step 7: Re-run MART refresh (only refresh_gl_accounts + downstream)
# PERFORM mart.refresh_gl_accounts('company-dev-001');
# PERFORM mart.refresh_account_balances('company-dev-001');
# PERFORM mart.refresh_income_statement('company-dev-001');
# PERFORM mart.refresh_balance_sheet('company-dev-001');
# PERFORM mart.refresh_ebitda('company-dev-001');
```

---

## Classification Checklist (with KSDEPOR accountant)

- [ ] Format-code prefix structure verified (OACT extracted, prefixes confirmed)
- [ ] All accounts with `account_type = 'act_Sales'` → `revenue` ✅
- [ ] All accounts with `account_type = 'act_Expense'` → `opex` (or finer classification) ✅
- [ ] Capital / reserves accounts → `equity` explicitly set
- [ ] Retained earnings account → `equity` explicitly set
- [ ] CxC (accounts receivable) → `current_assets` ✅
- [ ] CxP (accounts payable) → `current_liabilities` ✅
- [ ] Inventory / existencias → `current_assets` ✅
- [ ] PPE / activo fijo → `non_current_assets` ✅
- [ ] Depreciation accounts → `depreciation` (to enable D&A separation in EBITDA)
- [ ] Tax accounts → `tax` ✅
- [ ] Financial income/expense → `financial` ✅
- [ ] Unclassified count after rules = 0 (or < 3 with documented justification)
- [ ] Revenue positive in last 3 periods ✅
- [ ] Balance cuadra (imbalance < 0.01) ✅
- [ ] KSDEPOR accountant has reviewed and approved

---

## References

- Classification SQL: `sql/native-bi/accounting-classification-demo-ksdepor.sql`
- ETL function map: `docs/native-bi-accounting-function-map.md`
- Account classification guide: `docs/native-bi-account-classification.md`
- MART validation: `docs/native-bi-accounting-mart-validation-results.md`
- Operations runbook: `docs/native-bi-accounting-operations-runbook.md`
