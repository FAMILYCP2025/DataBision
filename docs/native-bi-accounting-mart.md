# Native BI — Accounting MART ETL (Sprint 13B)

Generated: 2026-06-17

## Overview

Sprint 13B wires the ETL pipeline between raw SAP B1 accounting data (from Sprint 13A) and the MART tables that power the Finance accounting sub-reports (Sprints 13C–13E).

---

## New artifacts

| Artifact | Path |
|---|---|
| EF migration | `src/DataBision.Infrastructure/Data/Staging/Migrations/20260617130000_AddAccountingMartFunctions.cs` |
| Standalone SQL | `sql/native-bi/accounting-mart.sql` |
| Validation queries | `docs/sql/accounting-mart-validation-queries.sql` |

---

## New table

### `mart.ebitda_summary`

| Column | Type | Description |
|---|---|---|
| company_id | TEXT | Tenant key |
| period_year | INTEGER | Calendar year |
| period_month | INTEGER | Calendar month (1–12) |
| revenue | NUMERIC | Total revenue for period |
| cogs | NUMERIC | Cost of goods sold |
| gross_profit | NUMERIC | revenue − cogs |
| opex | NUMERIC | Operating expenses |
| ebitda | NUMERIC | gross_profit − opex (D&A not yet separated) |
| depreciation | NUMERIC | 0 until classified via cfg |
| amortization | NUMERIC | 0 until classified via cfg |
| financial_result | NUMERIC | Financial income/expense net |
| tax_result | NUMERIC | Tax expense |
| net_income | NUMERIC | ebitda − financial − tax |
| refreshed_at | TIMESTAMPTZ | Last ETL run |

---

## ETL functions

### Full pipeline

```sql
SELECT * FROM mart.refresh_accounting_all('company-analytics-id');
```

Returns one row per step with `step_name`, `status` (OK/ERROR), `executed_at_utc`, `message`.

### Individual steps (in order)

| Step | Function | Source → Destination |
|---|---|---|
| 1 | `stg.refresh_gl_accounts(p)` | `raw.sap_oact` → `stg.gl_account` |
| 2 | `stg.refresh_journal_entries(p)` | `raw.sap_ojdt/jdt1` → `stg.journal_entry/line` |
| 3 | `mart.refresh_gl_accounts(p)` | `stg.gl_account` + `cfg.account_classification_rules` → `mart.gl_accounts` |
| 4 | `mart.refresh_account_balances(p)` | `stg.journal_entry_line` → `mart.account_balances` |
| 5 | `mart.refresh_income_statement(p)` | `mart.account_balances` + `mart.gl_accounts` → `mart.income_statement_summary` |
| 6 | `mart.refresh_balance_sheet(p)` | `mart.account_balances` + `mart.gl_accounts` → `mart.balance_sheet_summary` |
| 7 | `mart.refresh_ebitda(p)` | `mart.income_statement_summary` → `mart.ebitda_summary` |

---

## Account classification

### How it works

`mart.refresh_gl_accounts` resolves `statement_line` using 3-tier priority:

1. **Exact match** — `cfg.account_classification_rules.account_code = stg.gl_account.code`
2. **Format-code prefix** — rules where `account_code IS NULL` and `format_code` is a prefix of the account's format code
3. **SAP account_type fallback** — generic mapping; clients SHOULD override via cfg

### SAP account_type fallback mapping

| SAP account_type | statement_line assigned |
|---|---|
| act_AccountsReceivable | current_assets |
| act_AccountsPayable | current_liabilities |
| act_Sales | revenue |
| act_Expense | opex |
| act_FixedAssets | non_current_assets |
| (all others) | unclassified |

> **⚠️ Warning:** The SAP account_type fallback is intentionally generic and will likely misclassify many accounts. It exists only to avoid NULL statement_lines before cfg rules are populated. Always populate `cfg.account_classification_rules` per client before relying on MART data for financial reporting.

### Recognized statement_line values

**P&L (Income Statement):**
- `revenue` — top-line sales
- `cogs` — cost of goods sold
- `opex` — operating expenses (SGA, R&D, etc.)
- `other_income` — non-operating income
- `other_expense` — non-operating expense
- `financial` — financial income/expense
- `tax` — income tax
- `depreciation` — D&A (optional — used by ebitda if classified)
- `amortization` — amortization (optional)

**Balance Sheet:**
- `current_assets`
- `non_current_assets`
- `current_liabilities`
- `non_current_liabilities`
- `equity`

### Populating cfg.account_classification_rules

```sql
-- Example: classify by account code
INSERT INTO cfg.account_classification_rules (company_id, account_code, statement_line)
VALUES ('acme', '70000000', 'revenue'),
       ('acme', '60000000', 'cogs'),
       ('acme', '40000000', 'opex');

-- Example: classify entire format_code range
INSERT INTO cfg.account_classification_rules (company_id, format_code, statement_line)
VALUES ('acme', '7', 'revenue'),   -- all accounts with format_code starting with '7'
       ('acme', '6', 'cogs'),
       ('acme', '4', 'opex');
```

---

## Sign convention

### P&L (income_statement_summary)

- Income lines (`revenue`, `other_income`): `amount = credit_sum − debit_sum` → positive = income
- Expense lines (`cogs`, `opex`, `financial`, `tax`): `amount = debit_sum − credit_sum` → positive = expense

### Balance sheet (balance_sheet_summary)

- Asset lines: `amount = debit_sum − credit_sum` → positive = asset balance
- Liability/equity lines: `amount = credit_sum − debit_sum` → positive = liability/equity balance

---

## Dependencies

| Requirement | Status |
|---|---|
| Sprint 13A extractor run: `--object OACT --send` | Must be run first |
| Sprint 13A extractor run: `--object OJDT --send` | Must be run first |
| `cfg.account_classification_rules` populated per company | Needed for correct classification |

---

## Running the ETL

```sql
-- Full pipeline (recommended)
SELECT * FROM mart.refresh_accounting_all('company-analytics-id');

-- Partial re-run after new classifications
PERFORM mart.refresh_gl_accounts('company-analytics-id');
PERFORM mart.refresh_account_balances('company-analytics-id');
PERFORM mart.refresh_income_statement('company-analytics-id');
PERFORM mart.refresh_balance_sheet('company-analytics-id');
PERFORM mart.refresh_ebitda('company-analytics-id');
```

Run validation queries from `docs/sql/accounting-mart-validation-queries.sql` to verify row counts, unclassified accounts, and balance sheet sanity.

---

## Limitations

- **D&A not separated**: `depreciation` and `amortization` in `mart.ebitda_summary` are always 0 until `depreciation`/`amortization` statement_line values are added to `cfg.account_classification_rules`. This means EBITDA currently equals EBIT.
- **No multi-currency**: account balances use local currency (`debit`/`credit` fields). Foreign currency amounts (`fc_debit`/`fc_credit`) are stored in stg but not used in MART aggregation.
- **Balance sheet is flow-based**: `mart.balance_sheet_summary` aggregates debit/credit flows per period — it is not a running cumulative balance. For proper balance sheet, you need cumulative account balances (all periods up to snapshot).

> **TODO Sprint 14:** Add cumulative balance sheet calculation — requires period-over-period running SUM in mart.refresh_balance_sheet.
