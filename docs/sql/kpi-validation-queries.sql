-- ============================================================
-- KPI Validation Queries — Sprint 5D
-- Purpose: verify MART KPIs match STG source data
-- Run against Supabase after --transform --include-mart
-- ============================================================

-- ── Parameters (edit before running) ─────────────────────────
-- Replace 'KSDEPOR' with the actual company_id under test.

WITH params AS (
    SELECT 'KSDEPOR'::text AS company_id
)

-- ============================================================
-- KPI 1: Gross sales amount (non-cancelled invoices)
-- ============================================================
SELECT
    'KPI-01 Gross Sales'                            AS kpi,
    ROUND(m.gross_sales_amount, 2)                  AS mart_value,
    ROUND(SUM(CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                   THEN COALESCE(s.doc_total, 0) ELSE 0 END), 2) AS stg_value,
    ROUND(m.gross_sales_amount -
          SUM(CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                   THEN COALESCE(s.doc_total, 0) ELSE 0 END), 2) AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.sales_invoice s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.gross_sales_amount;

-- ============================================================
-- KPI 2: Credit memo amount
-- ============================================================
SELECT
    'KPI-02 Credit Memo Amount'                     AS kpi,
    ROUND(m.credit_memo_amount, 2)                  AS mart_value,
    ROUND(SUM(COALESCE(s.doc_total, 0)), 2)         AS stg_value,
    ROUND(m.credit_memo_amount -
          SUM(COALESCE(s.doc_total, 0)), 2)         AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.credit_memo s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.credit_memo_amount;

-- ============================================================
-- KPI 3: Net sales amount = gross_sales - credit_memos
-- ============================================================
SELECT
    'KPI-03 Net Sales'                              AS kpi,
    ROUND(m.net_sales_amount, 2)                    AS mart_value,
    ROUND(m.gross_sales_amount - m.credit_memo_amount, 2) AS stg_derived,
    ROUND(m.net_sales_amount -
         (m.gross_sales_amount - m.credit_memo_amount), 2) AS delta
FROM mart.sales_kpi_summary m
JOIN params p ON m.company_id = p.company_id;

-- ============================================================
-- KPI 4: Invoice count (non-cancelled)
-- ============================================================
SELECT
    'KPI-04 Invoice Count'                          AS kpi,
    m.invoice_count                                 AS mart_value,
    COUNT(CASE WHEN COALESCE(s.cancelled,'N') != 'Y' THEN 1 END) AS stg_value,
    m.invoice_count -
    COUNT(CASE WHEN COALESCE(s.cancelled,'N') != 'Y' THEN 1 END) AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.sales_invoice s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.invoice_count;

-- ============================================================
-- KPI 5: Credit memo count
-- ============================================================
SELECT
    'KPI-05 Credit Memo Count'                      AS kpi,
    m.credit_memo_count                             AS mart_value,
    COUNT(*)                                        AS stg_value,
    m.credit_memo_count - COUNT(*)                 AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.credit_memo s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.credit_memo_count;

-- ============================================================
-- KPI 6: Active customers (distinct card_code, non-cancelled invoices)
-- ============================================================
SELECT
    'KPI-06 Active Customers'                       AS kpi,
    m.active_customers                              AS mart_value,
    COUNT(DISTINCT CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                        THEN s.card_code END)       AS stg_value,
    m.active_customers -
    COUNT(DISTINCT CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                        THEN s.card_code END)       AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.sales_invoice s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.active_customers;

-- ============================================================
-- KPI 7: Active items (distinct item_code from invoice lines)
-- ============================================================
SELECT
    'KPI-07 Active Items'                           AS kpi,
    m.active_items                                  AS mart_value,
    COUNT(DISTINCT l.item_code)                     AS stg_value,
    m.active_items - COUNT(DISTINCT l.item_code)   AS delta
FROM mart.sales_kpi_summary m
CROSS JOIN stg.sales_invoice_line l
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND l.company_id = p.company_id
GROUP BY m.active_items;

-- ============================================================
-- KPI 8: Average ticket (gross_sales / invoice_count)
-- ============================================================
SELECT
    'KPI-08 Avg Ticket'                             AS kpi,
    ROUND(m.avg_ticket_amount, 2)                   AS mart_value,
    CASE WHEN m.invoice_count > 0
         THEN ROUND(m.gross_sales_amount / m.invoice_count, 2)
         ELSE 0 END                                 AS stg_derived,
    ROUND(m.avg_ticket_amount -
          CASE WHEN m.invoice_count > 0
               THEN m.gross_sales_amount / m.invoice_count
               ELSE 0 END, 4)                       AS delta
FROM mart.sales_kpi_summary m
JOIN params p ON m.company_id = p.company_id;

-- ============================================================
-- KPI 9: Last invoice date
-- ============================================================
SELECT
    'KPI-09 Last Invoice Date'                      AS kpi,
    m.last_invoice_date                             AS mart_value,
    MAX(CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
             THEN s.doc_date END)                   AS stg_value
FROM mart.sales_kpi_summary m
CROSS JOIN stg.sales_invoice s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.last_invoice_date;

-- ============================================================
-- KPI 10: Last credit memo date
-- ============================================================
SELECT
    'KPI-10 Last Credit Memo Date'                  AS kpi,
    m.last_credit_memo_date                         AS mart_value,
    MAX(s.doc_date)                                 AS stg_value
FROM mart.sales_kpi_summary m
CROSS JOIN stg.credit_memo s
CROSS JOIN params p
WHERE m.company_id = p.company_id
  AND s.company_id = p.company_id
GROUP BY m.last_credit_memo_date;

-- ============================================================
-- KPI 11: Daily sales row count matches distinct invoice dates
-- ============================================================
SELECT
    'KPI-11 Daily Sales Row Count'                  AS kpi,
    COUNT(*) AS mart_rows,
    (SELECT COUNT(DISTINCT doc_date)
     FROM stg.sales_invoice i
     JOIN params p ON i.company_id = p.company_id
     WHERE COALESCE(i.cancelled,'N') != 'Y') AS stg_distinct_dates
FROM mart.sales_daily d
JOIN params p ON d.company_id = p.company_id;

-- ============================================================
-- KPI 12: Monthly sales — gross sales sum matches KPI summary
-- ============================================================
SELECT
    'KPI-12 Monthly Gross Sales Sum'                AS kpi,
    ROUND(SUM(d.gross_sales_amount), 2)             AS mart_monthly_sum,
    ROUND(k.gross_sales_amount, 2)                  AS kpi_gross_sales,
    ROUND(SUM(d.gross_sales_amount) - k.gross_sales_amount, 2) AS delta
FROM mart.sales_monthly d
CROSS JOIN mart.sales_kpi_summary k
JOIN params p ON d.company_id = p.company_id
WHERE k.company_id = p.company_id
GROUP BY k.gross_sales_amount;

-- ============================================================
-- KPI 13: Customer sales — net sales sum matches KPI net_sales
-- ============================================================
SELECT
    'KPI-13 Customer Net Sales Sum'                 AS kpi,
    ROUND(SUM(c.net_sales_amount), 2)               AS mart_customer_sum,
    ROUND(k.net_sales_amount, 2)                    AS kpi_net_sales,
    ROUND(SUM(c.net_sales_amount) - k.net_sales_amount, 2) AS delta
FROM mart.customer_sales c
CROSS JOIN mart.sales_kpi_summary k
JOIN params p ON c.company_id = p.company_id
WHERE k.company_id = p.company_id
GROUP BY k.net_sales_amount;

-- ============================================================
-- KPI 14: Item sales — gross sales sum vs STG invoice lines total
-- ============================================================
SELECT
    'KPI-14 Item Gross Sales Sum'                   AS kpi,
    ROUND(SUM(i.gross_sales_amount), 2)             AS mart_item_sum,
    ROUND((SELECT SUM(l.line_total)
           FROM stg.sales_invoice_line l
           JOIN stg.sales_invoice h ON h.company_id = l.company_id
                                    AND h.doc_entry = l.doc_entry
           JOIN params p ON l.company_id = p.company_id
           WHERE COALESCE(h.cancelled,'N') != 'Y'), 2) AS stg_line_total
FROM mart.item_sales i
JOIN params p ON i.company_id = p.company_id;

-- ============================================================
-- KPI 15: Salesperson count — active codes with sales
-- ============================================================
SELECT
    'KPI-15 Salesperson Count'                      AS kpi,
    COUNT(*) AS mart_salesperson_rows,
    (SELECT COUNT(DISTINCT h.sales_person_code)
     FROM stg.sales_invoice h
     JOIN params p ON h.company_id = p.company_id
     WHERE COALESCE(h.cancelled,'N') != 'Y'
       AND NULLIF(h.sales_person_code, '') IS NOT NULL) AS stg_distinct_codes
FROM mart.salesperson_sales s
JOIN params p ON s.company_id = p.company_id;

-- ============================================================
-- All-in-one delta summary (run last — red flags any non-zero delta)
-- ============================================================
SELECT
    kpi,
    mart_value,
    stg_value,
    delta,
    CASE WHEN ABS(delta) < 0.01 THEN 'OK' ELSE '*** MISMATCH ***' END AS status
FROM (
    SELECT 'KPI-01 Gross Sales'    AS kpi,
           ROUND(m.gross_sales_amount, 2) AS mart_value,
           ROUND(SUM(CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                          THEN COALESCE(s.doc_total, 0) ELSE 0 END), 2) AS stg_value,
           ROUND(m.gross_sales_amount -
                 SUM(CASE WHEN COALESCE(s.cancelled,'N') != 'Y'
                          THEN COALESCE(s.doc_total, 0) ELSE 0 END), 2) AS delta
    FROM mart.sales_kpi_summary m
    CROSS JOIN stg.sales_invoice s
    CROSS JOIN params p
    WHERE m.company_id = p.company_id AND s.company_id = p.company_id
    GROUP BY m.gross_sales_amount

    UNION ALL

    SELECT 'KPI-02 Credit Memos',
           ROUND(m.credit_memo_amount, 2),
           ROUND(SUM(COALESCE(s.doc_total, 0)), 2),
           ROUND(m.credit_memo_amount - SUM(COALESCE(s.doc_total, 0)), 2)
    FROM mart.sales_kpi_summary m
    CROSS JOIN stg.credit_memo s
    CROSS JOIN params p
    WHERE m.company_id = p.company_id AND s.company_id = p.company_id
    GROUP BY m.credit_memo_amount
) summary
ORDER BY kpi;
