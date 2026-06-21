# PCGE Peru Sign Convention — Income Statement

**Date:** 2026-06-20  
**Sprint:** 19A — Correct PCGE Peru signs in mart.refresh_income_statement

## Problem

The PCGE (Plan Contable General Empresarial Peruano) uses a different journaling convention from Chilean IFRS. In PCGE:

- **Compras (60-61xxx)** are journaled as debit initially, then reversed by **Variación existencias (61xxx)** via credit entries
- **Costo de ventas (69xxx)** may also have mixed debit/credit patterns depending on inventory accounting method
- Net result: COGS accounts show net credit > net debit in many test periods → `debit_sum - credit_sum < 0`

The original `mart.refresh_income_statement` computed `cogs = SUM(debit_sum - credit_sum)`, producing **negative COGS** (-128,474.80) — mathematically correct per raw accounting, but counter-intuitive for executive dashboards.

Similarly, `financial` income (accounts 77xxx — Ingresos financieros) was computed as `debit - credit`, producing a negative value for net income from financial activities when credit > debit.

## Fix Applied

`sql/native-bi/accounting-mart.sql` — `mart.refresh_income_statement`:

| statement_line | Old formula | New formula | Rationale |
|---|---|---|---|
| revenue | credit - debit | credit - debit | ✅ unchanged — correct |
| other_income | credit - debit | credit - debit | ✅ unchanged |
| financial | debit - credit | credit - debit | Fixed — financial income is credit-heavy |
| cogs | debit - credit | ABS(debit - credit) | Fixed — always positive regardless of PCGE netting |
| opex | debit - credit | debit - credit | ✅ unchanged — correct |
| other_expense | debit - credit | debit - credit | ✅ unchanged |
| tax | debit - credit | ABS(debit - credit) | Defensive ABS |
| unclassified | credit - debit | credit - debit | ✅ unchanged |

Also fixed: `ProcessDashboardRepository.cs` line 728 — income-statement DTO computation:
- Old: `ni = oi - fin - tax` (subtracted financial as if it were a cost)
- New: `ni = oi + fin - tax` (adds financial income to net income)

## Results After Fix

| Metric | Before 19A | After 19A |
|---|---|---|
| income_statement cogs (Jan 2026) | -128,474.80 | +128,474.80 |
| income_statement financial (Jan 2026) | -2.21 | +2.21 |
| API income-statement netIncome (Jan 2026) | -130,925.82 | -130,921.40 |
| API ebitda cogs (Jan 2026) | 0.00 | +128,474.80 |

## PCGE Accounting Note

In PCGE Peru, the complete cost-of-goods flow for a purchase is:
1. `DR 60xxx Compras / CR 42xxx CxP` — purchase recorded
2. `DR 20xxx Mercaderías / CR 61xxx Variación existencias` — goods received to inventory
3. `DR 61xxx Variación / CR 60xxx Compras` — purchase cancelled against inventory variation
4. `DR 69xxx Costo de ventas / CR 20xxx Mercaderías` — COGS recognized on sale

The net effect across 60xxx + 61xxx + 69xxx accounts produces mixed debit/credit signals. ABS normalization is the correct approach for executive dashboard display.
