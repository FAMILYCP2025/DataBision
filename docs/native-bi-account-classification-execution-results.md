# Sprint 18C — Account Classification Execution Results

**Date:** 2026-06-19  
**Sprint:** 18C — Aplicar clasificación contable KSDEPOR

## Classification Schema Used

**Plan Contable General Empresarial Peruano (PCGE Peru)** — adjusted from the initial demo SQL (which used Chilean classification where prefix 4xxx = revenue; PCGE Peru uses 4xxx = liabilities).

## Rules Inserted

```sql
DELETE FROM cfg.account_classification_rules WHERE company_id = 'company-dev-001';
INSERT INTO cfg.account_classification_rules ... 83 rows
```

**Result:** 83 classification rules for company-dev-001.

### Rules by statement_line

| statement_line | Rules |
|---|---|
| cogs | 4 |
| current_assets | 16 |
| current_liabilities | 11 |
| depreciation | 2 |
| equity | 6 |
| financial | 2 |
| non_current_assets | 14 |
| non_current_liabilities | 3 |
| opex | 13 |
| other_expense | 1 |
| other_income | 4 |
| revenue | 5 |
| tax | 2 |

## Key PCGE Corrections vs Chilean Demo SQL

| Prefix | Chile (incorrect) | PCGE Peru (correct) |
|---|---|---|
| 4 (fallback) | revenue | current_liabilities |
| 40-49 | revenue | current_liabilities |
| 42xxx | revenue | current_liabilities (CxP Comerciales) |
| 70-79 | (missing) | revenue / other_income / financial |
| 69xxx | (missing) | cogs (Costo de ventas) |
| 60-61xxx | (missing) | cogs (Compras / Variación existencias) |
| 97xxx | (missing) | opex (Cargas imputables) |

## mart.gl_accounts After Classification

### OACT accounts classified (initial refresh)

| statement_line | Accounts |
|---|---|
| current_assets | 11 |
| current_liabilities | 1 (40114 IGV) |
| non_current_assets | 6 |
| unclassified | 2 (02, 02111 — no rule for prefix '02') |

### JDT1 orphan accounts injected (Sprint 18E enhancement)

35 JDT1 accounts not in raw.sap_oact were injected directly into mart.gl_accounts with PCGE prefix-based classification.

**Total mart.gl_accounts after injection:** 55

| statement_line | Accounts |
|---|---|
| cogs | 12 |
| current_assets | 20 |
| current_liabilities | 7 |
| depreciation | 1 |
| financial | 1 |
| non_current_assets | 7 |
| opex | 4 |
| revenue | 1 (70122 — Ventas) |
| unclassified | 2 |

## JDT1 Accounts by Classification

| Account | Name (PCGE) | statement_line |
|---|---|---|
| 70122 | Ventas | revenue |
| 60122, 60123, 60911-60916, 60921 | Compras | cogs |
| 61122, 61123 | Variación existencias | cogs |
| 69111, 69112, 69115 | Costo de ventas | cogs |
| 20111, 20121-20123 | Mercaderías | current_assets |
| 10711 | Efectivo | current_assets |
| 12130 | Facturas por cobrar | current_assets |
| 18211, 18213 | Anticipos | current_assets |
| 28111 | Existencias por recibir | current_assets |
| 40111 | IGV por pagar | current_liabilities |
| 42114, 42120-42122 | CxP Comerciales | current_liabilities |
| 48911 | Provisiones | current_liabilities |
| 67611 | Gastos financieros | depreciation |
| 77611 | Ingresos financieros | financial |
| 65610, 65622 | Otros gastos gestión | opex |
| 95305, 97762 | Costos por función | opex |
| 30101 | Inversiones | non_current_assets |

## Design Decision: JDT1 Account Injection

Since CLTSTKSDEPOR only has 20 header-level accounts in OACT, and `mart.refresh_gl_accounts` only processes accounts in `stg.gl_account` (from OACT), the JDT1 posting accounts (5-digit) were never loaded into mart.gl_accounts by the standard ETL. 

To ensure the income statement and balance sheet show meaningful classified data, the 35 JDT1 orphan accounts were injected directly into `mart.gl_accounts` post-ETL with PCGE-derived classification via account code prefix matching against `cfg.account_classification_rules`.

This approach is valid for demo/dev environments. In production, a complete OACT extraction from a production SAP DB would include all posting accounts, making this injection unnecessary.
