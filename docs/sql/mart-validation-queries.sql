-- ============================================================
-- MART Validation Queries — Sprint 5B
-- Run against Supabase after database update + --transform --include-mart
-- ============================================================

-- ── Row counts ───────────────────────────────────────────────

SELECT 'mart.sales_daily'        AS table_name, COUNT(*) AS rows FROM mart.sales_daily;
SELECT 'mart.sales_monthly'      AS table_name, COUNT(*) AS rows FROM mart.sales_monthly;
SELECT 'mart.customer_sales'     AS table_name, COUNT(*) AS rows FROM mart.customer_sales;
SELECT 'mart.item_sales'         AS table_name, COUNT(*) AS rows FROM mart.item_sales;
SELECT 'mart.salesperson_sales'  AS table_name, COUNT(*) AS rows FROM mart.salesperson_sales;
SELECT 'mart.sales_kpi_summary'  AS table_name, COUNT(*) AS rows FROM mart.sales_kpi_summary;

-- ── KPI Summary ──────────────────────────────────────────────

SELECT
    company_id,
    gross_sales_amount,
    credit_memo_amount,
    net_sales_amount,
    invoice_count,
    credit_memo_count,
    active_customers,
    active_items,
    ROUND(avg_ticket_amount, 2)     AS avg_ticket,
    last_invoice_date,
    last_credit_memo_date,
    transformed_at_utc
FROM mart.sales_kpi_summary;

-- ── Top 10 customers by net sales ────────────────────────────

SELECT
    card_code,
    card_name,
    ROUND(sales_amount, 2)       AS gross_sales,
    ROUND(credit_memo_amount, 2) AS credit_memos,
    ROUND(net_sales_amount, 2)   AS net_sales,
    invoice_count,
    last_invoice_date,
    first_invoice_date,
    ROUND(avg_ticket_amount, 2)  AS avg_ticket
FROM mart.customer_sales
ORDER BY net_sales_amount DESC
LIMIT 10;

-- ── Top 10 products by gross sales ───────────────────────────

SELECT
    item_code,
    item_name,
    ROUND(quantity_sold, 2)         AS qty_sold,
    ROUND(gross_sales_amount, 2)    AS gross_sales,
    line_count,
    invoice_count,
    last_sale_date
FROM mart.item_sales
ORDER BY gross_sales_amount DESC
LIMIT 10;

-- ── Sales by day (last 30 days) ───────────────────────────────

SELECT
    sales_date,
    ROUND(gross_sales_amount, 2)    AS gross_sales,
    ROUND(credit_memo_amount, 2)    AS credit_memos,
    ROUND(net_sales_amount, 2)      AS net_sales,
    invoice_count,
    active_customers,
    ROUND(avg_ticket_amount, 2)     AS avg_ticket
FROM mart.sales_daily
ORDER BY sales_date DESC
LIMIT 30;

-- ── Sales by month ────────────────────────────────────────────

SELECT
    sales_month,
    ROUND(gross_sales_amount, 2)    AS gross_sales,
    ROUND(credit_memo_amount, 2)    AS credit_memos,
    ROUND(net_sales_amount, 2)      AS net_sales,
    invoice_count,
    credit_memo_count,
    active_customers,
    ROUND(avg_ticket_amount, 2)     AS avg_ticket
FROM mart.sales_monthly
ORDER BY sales_month DESC;

-- ── Sales by salesperson ──────────────────────────────────────

SELECT
    sales_person_code,
    COALESCE(sales_person_name, '(no name)') AS salesperson,
    ROUND(sales_amount, 2)          AS gross_sales,
    ROUND(credit_memo_amount, 2)    AS credit_memos,
    ROUND(net_sales_amount, 2)      AS net_sales,
    invoice_count,
    active_customers,
    ROUND(avg_ticket_amount, 2)     AS avg_ticket
FROM mart.salesperson_sales
ORDER BY net_sales_amount DESC;

-- ── Gross/Net comparison MART vs STG (conciliation) ──────────

SELECT
    'STG gross (non-cancelled)'  AS metric,
    ROUND(SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END), 2) AS amount,
    COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS count
FROM stg.sales_invoice
UNION ALL
SELECT
    'MART kpi gross_sales_amount', ROUND(gross_sales_amount, 2), invoice_count
FROM mart.sales_kpi_summary;

SELECT
    'STG credit memos total'     AS metric,
    ROUND(SUM(COALESCE(doc_total,0)), 2) AS amount,
    COUNT(*) AS count
FROM stg.credit_memo
UNION ALL
SELECT
    'MART kpi credit_memo_amount', ROUND(credit_memo_amount, 2), credit_memo_count
FROM mart.sales_kpi_summary;

-- ── Freshness check ───────────────────────────────────────────

SELECT
    'sales_kpi_summary'   AS mart_table,
    transformed_at_utc,
    NOW() - transformed_at_utc AS age
FROM mart.sales_kpi_summary;

SELECT
    MIN(transformed_at_utc) AS oldest_row,
    MAX(transformed_at_utc) AS newest_row
FROM mart.sales_daily;

-- ── Quick manual full refresh ─────────────────────────────────
-- SELECT * FROM mart.refresh_all('company-dev-001');
