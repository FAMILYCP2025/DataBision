-- =============================================================================
-- DataBision — Accounting Deployment Smoke Test
-- Sprint 14A — 2026-06-18
-- Run against Supabase after completing the deployment checklist.
-- Replace 'YOUR_COMPANY_ID' with the Analytics Company ID for the tenant.
-- =============================================================================

-- Variables (set via \set in psql, or replace manually in Supabase SQL editor)
-- \set company_id 'YOUR_COMPANY_ID'

-- 1. Verify all required tables exist
SELECT table_schema, table_name, (SELECT COUNT(*) FROM information_schema.columns c2 WHERE c2.table_schema = t.table_schema AND c2.table_name = t.table_name) AS col_count
FROM information_schema.tables t
WHERE (table_schema, table_name) IN (
    ('raw', 'sap_oact'),
    ('raw', 'sap_ojdt'),
    ('raw', 'sap_jdt1'),
    ('stg', 'gl_account'),
    ('stg', 'journal_entry'),
    ('stg', 'journal_entry_line'),
    ('cfg', 'account_classification_rules'),
    ('mart', 'gl_accounts'),
    ('mart', 'account_balances'),
    ('mart', 'income_statement_summary'),
    ('mart', 'balance_sheet_summary'),
    ('mart', 'ebitda_summary')
)
ORDER BY table_schema, table_name;

-- 2. Verify all required functions exist
SELECT routine_schema, routine_name, routine_type
FROM information_schema.routines
WHERE routine_schema IN ('stg', 'mart')
  AND routine_name IN (
      'refresh_gl_accounts',
      'refresh_journal_entries',
      'refresh_gl_accounts',
      'refresh_account_balances',
      'refresh_income_statement',
      'refresh_balance_sheet',
      'refresh_ebitda',
      'refresh_accounting_all'
  )
ORDER BY routine_schema, routine_name;

-- 3. Row counts per layer (replace 'YOUR_COMPANY_ID')
SELECT 'raw.sap_oact'                  AS layer, COUNT(*) AS rows FROM "raw"."sap_oact"              WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'raw.sap_ojdt',       COUNT(*) FROM "raw"."sap_ojdt"             WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'raw.sap_jdt1',       COUNT(*) FROM "raw"."sap_jdt1"             WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'stg.gl_account',     COUNT(*) FROM stg.gl_account               WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'stg.journal_entry',  COUNT(*) FROM stg.journal_entry            WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'stg.journal_entry_line', COUNT(*) FROM stg.journal_entry_line   WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'cfg.acr',            COUNT(*) FROM cfg.account_classification_rules WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'mart.gl_accounts',   COUNT(*) FROM mart.gl_accounts             WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'mart.account_balances', COUNT(*) FROM mart.account_balances     WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'mart.income_statement', COUNT(*) FROM mart.income_statement_summary WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'mart.balance_sheet', COUNT(*) FROM mart.balance_sheet_summary   WHERE company_id = 'YOUR_COMPANY_ID'
UNION ALL SELECT 'mart.ebitda',        COUNT(*) FROM mart.ebitda_summary          WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY layer;

-- 4. Unclassified accounts (critical — all should be classified before demo)
SELECT code, name, account_type, statement_line
FROM mart.gl_accounts
WHERE company_id = 'YOUR_COMPANY_ID'
  AND statement_line = 'unclassified'
ORDER BY code;

-- 5. Statement-line distribution (verify classification coverage)
SELECT statement_line, COUNT(*) AS account_count, ROUND(COUNT(*) * 100.0 / NULLIF(SUM(COUNT(*)) OVER (), 0), 1) AS pct
FROM mart.gl_accounts
WHERE company_id = 'YOUR_COMPANY_ID'
GROUP BY statement_line
ORDER BY statement_line;

-- 6. Active classification rules
SELECT account_code, format_code, statement_line, created_at
FROM cfg.account_classification_rules
WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY statement_line, COALESCE(account_code, format_code);

-- 7. Income statement last 12 months
SELECT period_year, period_month, statement_line, ROUND(amount, 2) AS amount
FROM mart.income_statement_summary
WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY period_year DESC, period_month DESC, statement_line
LIMIT 60;

-- 8. EBITDA trend last 12 months
SELECT
    period_year, period_month,
    ROUND(revenue,      2) AS revenue,
    ROUND(cogs,         2) AS cogs,
    ROUND(gross_profit, 2) AS gross_profit,
    ROUND(opex,         2) AS opex,
    ROUND(ebitda,       2) AS ebitda,
    ROUND(net_income,   2) AS net_income,
    refreshed_at
FROM mart.ebitda_summary
WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY period_year DESC, period_month DESC
LIMIT 12;

-- 9. Balance sheet — latest snapshot
SELECT snapshot_date, category, sub_category, ROUND(amount, 2) AS amount
FROM mart.balance_sheet_summary
WHERE company_id = 'YOUR_COMPANY_ID'
  AND snapshot_date = (SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = 'YOUR_COMPANY_ID')
ORDER BY category, sub_category;

-- 10. Balance sheet sanity: assets == liabilities + equity (imbalance should be 0 or near-0)
SELECT
    ROUND(SUM(CASE WHEN category IN ('current_assets', 'non_current_assets') THEN amount ELSE 0 END), 2) AS total_assets,
    ROUND(SUM(CASE WHEN category IN ('current_liabilities', 'non_current_liabilities', 'equity') THEN amount ELSE 0 END), 2) AS total_liab_equity,
    ROUND(
        SUM(CASE WHEN category IN ('current_assets', 'non_current_assets') THEN amount ELSE 0 END)
        - SUM(CASE WHEN category IN ('current_liabilities', 'non_current_liabilities', 'equity') THEN amount ELSE 0 END)
    , 2) AS imbalance
FROM mart.balance_sheet_summary
WHERE company_id = 'YOUR_COMPANY_ID'
  AND snapshot_date = (SELECT MAX(snapshot_date) FROM mart.balance_sheet_summary WHERE company_id = 'YOUR_COMPANY_ID');

-- 11. Revenue sign check (should be positive for income periods)
SELECT period_year, period_month, ROUND(revenue, 2) AS revenue,
       CASE WHEN revenue < 0 THEN 'WARNING: negative revenue' ELSE 'OK' END AS sign_check
FROM mart.ebitda_summary
WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY period_year DESC, period_month DESC
LIMIT 12;

-- 12. Orphan journal lines (account in JEL but not in OACT)
SELECT DISTINCT jel.account, COUNT(*) AS line_count
FROM stg.journal_entry_line jel
WHERE jel.company_id = 'YOUR_COMPANY_ID'
  AND NOT EXISTS (
      SELECT 1 FROM mart.gl_accounts ga
      WHERE ga.company_id = jel.company_id AND ga.code = jel.account
  )
GROUP BY jel.account
ORDER BY line_count DESC;

-- 13. ETL step log — run full refresh and inspect
-- (Uncomment and replace company_id before running — this WRITES to MART tables)
-- SELECT step_name, status, executed_at_utc, message
-- FROM mart.refresh_accounting_all('YOUR_COMPANY_ID');

-- 14. Periods available in MART
SELECT DISTINCT period_year, period_month
FROM mart.income_statement_summary
WHERE company_id = 'YOUR_COMPANY_ID'
ORDER BY period_year, period_month;
