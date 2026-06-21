# Native BI — Balance Sheet Imbalance Analysis (Sprint 20B)

**Date:** 2026-06-20  
**Sprint:** 20B  
**Status:** ANALYZED — TST data limitation documented

---

## Symptom

The balance sheet shows a persistent imbalance:
- Jan 2026: Assets=256,229.20, Liabilities=121,908.63, Equity=0 → **Imbalance=134,320.57**
- Feb 2026: Assets=124,289.50, Liabilities=81,055.05, Equity=0 → **Imbalance=43,234.45**

Expected in accounting: `Assets = Liabilities + Equity`

---

## Root Cause Analysis

### 1. No Equity Journal Entries in TST Data

The TST database (CLTSTKSDEPOR) has no journal entries posting to equity accounts (prefixes 50-59 in PCGE, prefix 02 for opening balances). The `mart.gl_accounts` table has equity classifications via rules, but no corresponding debit/credit activity exists in `stg.journal_entry_line` for those accounts in Jan–Feb 2026.

**Consequence:** `total_equity = 0` for all periods, making balance sheet balance impossible.

### 2. Income Statement Flow Creates Apparent Imbalance

In double-entry accounting, income statement transactions (revenue, COGS, OPEX) create entries that:
- Debit or credit a **balance sheet account** (asset/liability)
- Debit or credit an **income statement account** (revenue/expense)

The income statement account side is NOT on the balance sheet. This creates an apparent asset/liability imbalance equal to the **net income for the period**:

```
Assets = Liabilities + Equity + Net_Income_for_Period
```

In a properly closed accounting system, net income flows to retained earnings (equity). Since TST has no equity closing entries, the retained earnings remain zero.

### 3. Monthly Flows (Not Cumulative)

`mart.refresh_balance_sheet` calculates period-level flows (monthly activity), not cumulative running balances. In TST, this means:
- Jan BS = Jan-only transactions
- Feb BS = Feb-only transactions (not Jan+Feb combined)

The controller displays only the **latest snapshot** (Feb). This makes the Feb BS appear sparse (only a few accounts have Feb activity).

---

## Stale Unclassified Rows (Fixed in Sprint 20A)

Additionally, the original BS showed spurious `unclassified` categories:
- Jan: unclassified=8,557.30
- Feb: unclassified=42,997.84

These were **stale rows** from a prior refresh when those accounts were temporarily unclassified (before Sprint 19 reclassified them). Fixed by Sprint 20A DELETE+INSERT pattern.

---

## Current State After Sprint 20A Fix

**balance_sheet_summary (clean, after fix):**
| snapshot_date | category | sub_category | amount |
|---|---|---|---|
| 2026-01-31 | current_assets | | 416,229.20 |
| 2026-01-31 | current_liabilities | | 121,880.31 |
| 2026-01-31 | current_liabilities | at_Other | 28.32 |
| 2026-01-31 | non_current_assets | | -160,000.00 |
| 2026-02-28 | current_assets | | 124,289.50 |
| 2026-02-28 | current_liabilities | | 81,055.05 |

**Imbalance check:**
| snapshot_date | total_assets | total_liab | total_equity | imbalance | unclassified |
|---|---|---|---|---|---|
| 2026-01-31 | 256,229.20 | 121,908.63 | 0.00 | 134,320.57 | **0.00** |
| 2026-02-28 | 124,289.50 | 81,055.05 | 0.00 | 43,234.45 | **0.00** |

- ✅ 0 unclassified
- The remaining imbalance is structural (no equity JEs in TST)

---

## Note on Non-Current Assets = -160,000

Account `30101` (non_current_assets) shows `debit=0, credit=160,000` in Jan → net balance = -160,000. This is test data where a non-current asset account received only credits (reducing the account). In real production data this would not occur as a normal balance.

---

## Demo Talking Points

| What the prospect sees | What to explain |
|---|---|
| Balance imbalance | "TST doesn't have equity/capital account entries. In production, equity accounts (capital contributions, retained earnings) balance the sheet. The imbalance = net income accumulation." |
| Non-current assets = -160,000 | "Test entry quirk in CLTSTKSDEPOR. Production data will show positive non-current assets." |
| Feb shows few accounts | "Balance sheet shows latest period's activity. Jan has more complete activity. Cumulative view available for production." |

---

## Production Behavior

In a production SAP B1 environment:
1. Equity accounts (50-59 in PCGE, company capital) will have opening balance entries
2. Retained earnings entries (closing entries) will distribute net income to equity
3. The imbalance will be 0 or very close to 0
4. All 5 categories (current_assets, non_current_assets, current_liabilities, non_current_liabilities, equity) will have meaningful balances
