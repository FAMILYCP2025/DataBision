# EBITDA PCGE Peru — Validation Results

**Date:** 2026-06-20  
**Sprint:** 19B — Fix mart.refresh_ebitda for PCGE normalized values

## Problem (Sprint 18 State)

`mart.refresh_ebitda` read `cogs` directly from `mart.income_statement_summary`. When COGS was negative (-128,474.80 due to PCGE sign convention), the EBITDA function's `MAX(CASE WHEN statement_line='cogs' THEN COALESCE(amount,0) ELSE 0 END)` returned the negative value. The API controller was further clamping or truncating this, resulting in `cogs=0` in the EBITDA response.

Downstream effects:
- `gross_profit = revenue - 0 = 201.19` (wrong — ignored real COGS)
- `ebitda = 201.19 - 0 - 2,650 = -2,448.81` (wrong — excluded COGS)
- `net_income = -2,448.81` (wrong)

## Fix Applied

`mart.refresh_ebitda` updated with defensive `ABS()` on all cogs references:

```sql
MAX(CASE WHEN statement_line='cogs' THEN ABS(COALESCE(amount,0)) ELSE 0 END)
```

Applied to all 3 positions where cogs is referenced (cogs column, gross_profit, ebitda, net_income).

Additionally: `financial_result` now treated as income (`+ financial` in net_income, not `- financial`), matching the 19A sign convention fix where `financial = credit - debit` (positive = net financial income).

## Validation Results

| Metric | Before 19B | After 19B |
|---|---|---|
| ebitda.cogs (Jan 2026) | 0.00 | 128,474.80 |
| ebitda.gross_profit (Jan 2026) | 201.19 | -128,273.61 |
| ebitda.ebitda (Jan 2026) | -2,448.81 | -130,923.61 |
| ebitda.financial_result (Jan 2026) | 0.00 | 2.21 |
| ebitda.net_income (Jan 2026) | -2,448.81 | -130,921.40 |

## Economic Interpretation

The test company CLTSTKSDEPOR has:
- Revenue: 201.19 (only 1 active revenue account with minor activity in TST period)
- COGS: 128,474.80 (multiple purchase/inventory accounts with significant activity)
- Gross margin: -63,757% (extremely negative — test data is not representative of a live business)
- OPEX: 2,650.00
- Financial income: 2.21 (interest / financial charges)
- Net income: -130,921.40

These numbers correctly reflect the actual SAP data — the test database has limited revenue entries but many cost entries, which is typical for a newly seeded SAP instance.

## EBITDA Formula (Post-Fix)

```
revenue        = MAX(income_statement.amount WHERE line='revenue')
cogs           = ABS(MAX(income_statement.amount WHERE line='cogs'))
gross_profit   = revenue - cogs
opex           = MAX(income_statement.amount WHERE line='opex')
ebitda         = gross_profit - opex
financial      = MAX(income_statement.amount WHERE line='financial')   [positive = income]
tax            = ABS(MAX(income_statement.amount WHERE line='tax'))
net_income     = revenue - cogs - opex + financial - tax
```
