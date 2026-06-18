-- ============================================================
-- DataBision — Accounting MART Validation Queries
-- Sprint 13B — 2026-06-17
-- Run these against the staging DB after refresh_accounting_all().
-- ============================================================

-- 1. Row counts per layer
SELECT 'raw.sap_oact'                AS layer, COUNT(*) AS rows FROM "raw"."sap_oact"              WHERE company_id = :'company_id'
UNION ALL
SELECT 'raw.sap_ojdt',                                  COUNT(*) FROM "raw"."sap_ojdt"             WHERE company_id = :'company_id'
UNION ALL
SELECT 'raw.sap_jdt1',                                  COUNT(*) FROM "raw"."sap_jdt1"             WHERE company_id = :'company_id'
UNION ALL
SELECT 'stg.gl_account',                                COUNT(*) FROM stg.gl_account               WHERE company_id = :'company_id'
UNION ALL
SELECT 'stg.journal_entry',                             COUNT(*) FROM stg.journal_entry            WHERE company_id = :'company_id'
UNION ALL
SELECT 'stg.journal_entry_line',                        COUNT(*) FROM stg.journal_entry_line       WHERE company_id = :'company_id'
UNION ALL
SELECT 'mart.gl_accounts',                              COUNT(*) FROM mart.gl_accounts             WHERE company_id = :'company_id'
UNION ALL
SELECT 'mart.account_balances',                         COUNT(*) FROM mart.account_balances        WHERE company_id = :'company_id'
UNION ALL
SELECT 'mart.income_statement_summary',                 COUNT(*) FROM mart.income_statement_summary WHERE company_id = :'company_id'
UNION ALL
SELECT 'mart.balance_sheet_summary',                    COUNT(*) FROM mart.balance_sheet_summary   WHERE company_id = :'company_id'
UNION ALL
SELECT 'mart.ebitda_summary',                           COUNT(*) FROM mart.ebitda_summary          WHERE company_id = :'company_id'
ORDER BY layer;

-- 2. Unclassified accounts (must be addressed via cfg.account_classification_rules)
SELECT code, name, account_type, statement_line
FROM mart.gl_accounts
WHERE company_id = :'company_id'
  AND statement_line = 'unclassified'
ORDER BY code;

-- 3. Classification rules in place
SELECT account_code, format_code, statement_line, created_at
FROM cfg.account_classification_rules
WHERE company_id = :'company_id'
ORDER BY statement_line, account_code;

-- 4. Income statement — last 12 months
SELECT period_year, period_month, statement_line, ROUND(amount, 2) AS amount
FROM mart.income_statement_summary
WHERE company_id = :'company_id'
ORDER BY period_year DESC, period_month DESC, statement_line;

-- 5. EBITDA trend — last 12 months
SELECT
    period_year, period_month,
    ROUND(revenue,      2) AS revenue,
    ROUND(cogs,         2) AS cogs,
    ROUND(gross_profit, 2) AS gross_profit,
    ROUND(opex,         2) AS opex,
    ROUND(ebitda,       2) AS ebitda,
    ROUND(net_income,   2) AS net_income
FROM mart.ebitda_summary
WHERE company_id = :'company_id'
ORDER BY period_year DESC, period_month DESC
LIMIT 12;

-- 6. Balance sheet — latest snapshot
SELECT category, sub_category, ROUND(amount, 2) AS amount
FROM mart.balance_sheet_summary
WHERE company_id = :'company_id'
  AND snapshot_date = (SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = :'company_id')
ORDER BY category, sub_category;

-- 7. Balance sheet sanity: total assets == total liabilities + equity
SELECT
    SUM(CASE WHEN category IN ('current_assets','non_current_assets') THEN amount ELSE 0 END) AS total_assets,
    SUM(CASE WHEN category IN ('current_liabilities','non_current_liabilities','equity') THEN amount ELSE 0 END) AS total_liab_equity,
    SUM(CASE WHEN category IN ('current_assets','non_current_assets') THEN amount ELSE 0 END)
    - SUM(CASE WHEN category IN ('current_liabilities','non_current_liabilities','equity') THEN amount ELSE 0 END) AS imbalance
FROM mart.balance_sheet_summary
WHERE company_id = :'company_id'
  AND snapshot_date = (SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = :'company_id');

-- 8. Accounts with activity but no classification in mart.gl_accounts
SELECT DISTINCT jel.account
FROM stg.journal_entry_line jel
WHERE jel.company_id = :'company_id'
  AND NOT EXISTS (SELECT 1 FROM mart.gl_accounts ga WHERE ga.company_id = jel.company_id AND ga.code = jel.account)
ORDER BY jel.account;

-- 9. Run the full refresh and inspect the step log
SELECT step_name, status, executed_at_utc, message
FROM mart.refresh_accounting_all(:'company_id');
