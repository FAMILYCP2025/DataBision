-- ============================================================
-- STG Validation Queries — Sprint 5A
-- Run against Supabase after: dotnet ef database update
-- and after: dotnet run -- --transform --company <company_id>
-- ============================================================

-- ── Row count comparison RAW vs STG ──────────────────────────

SELECT 'raw.sap_oslp'       AS source, COUNT(*) AS rows FROM raw.sap_oslp;
SELECT 'stg.salesperson'    AS source, COUNT(*) AS rows FROM stg.salesperson;

SELECT 'raw.sap_ocrd'       AS source, COUNT(*) AS rows FROM raw.sap_ocrd;
SELECT 'stg.customer'       AS source, COUNT(*) AS rows FROM stg.customer;

SELECT 'raw.sap_oitm'       AS source, COUNT(*) AS rows FROM raw.sap_oitm;
SELECT 'stg.item'           AS source, COUNT(*) AS rows FROM stg.item;

SELECT 'raw.sap_oinv'       AS source, COUNT(*) AS rows FROM raw.sap_oinv;
SELECT 'stg.sales_invoice'  AS source, COUNT(*) AS rows FROM stg.sales_invoice;

SELECT 'raw.sap_inv1'            AS source, COUNT(*) AS rows FROM raw.sap_inv1;
SELECT 'stg.sales_invoice_line'  AS source, COUNT(*) AS rows FROM stg.sales_invoice_line;

SELECT 'raw.sap_orin'       AS source, COUNT(*) AS rows FROM raw.sap_orin;
SELECT 'stg.credit_memo'    AS source, COUNT(*) AS rows FROM stg.credit_memo;

SELECT 'raw.sap_rin1'            AS source, COUNT(*) AS rows FROM raw.sap_rin1;
SELECT 'stg.credit_memo_line'    AS source, COUNT(*) AS rows FROM stg.credit_memo_line;

-- ── Samples ───────────────────────────────────────────────────

SELECT *
FROM stg.sales_invoice
ORDER BY doc_date DESC
LIMIT 10;

SELECT *
FROM stg.customer
ORDER BY card_code
LIMIT 10;

SELECT *
FROM stg.item
ORDER BY item_code
LIMIT 10;

SELECT *
FROM stg.sales_invoice_line
ORDER BY doc_entry DESC, line_num
LIMIT 10;

SELECT *
FROM stg.credit_memo
ORDER BY doc_date DESC
LIMIT 10;

SELECT *
FROM stg.credit_memo_line
ORDER BY doc_entry DESC, line_num
LIMIT 10;

SELECT *
FROM stg.salesperson
ORDER BY slp_code
LIMIT 10;

-- ── Hash guard verification (should return 0 rows on re-run) ──

-- If this returns rows, it means refresh updated rows with identical hashes (bug)
SELECT 'sales_invoice hash drift check' AS check,
       COUNT(*) AS unexpected_updates
FROM stg.sales_invoice i
JOIN raw.sap_oinv r ON r.company_id = i.company_id AND r."DocEntry" = i.doc_entry
WHERE i.source_hash_hex != r.source_hash_hex;

SELECT 'customer hash drift check' AS check,
       COUNT(*) AS unexpected_updates
FROM stg.customer c
JOIN raw.sap_ocrd r ON r.company_id = c.company_id AND r."CardCode" = c.card_code
WHERE c.source_hash_hex != r.source_hash_hex;

-- ── Nullability sanity checks ────────────────────────────────

-- card_code must never be null in stg.customer
SELECT COUNT(*) AS null_card_code FROM stg.customer WHERE card_code IS NULL;

-- doc_entry must never be null in stg.sales_invoice
SELECT COUNT(*) AS null_doc_entry FROM stg.sales_invoice WHERE doc_entry IS NULL;

-- slp_code must never be null in stg.salesperson
SELECT COUNT(*) AS null_slp_code FROM stg.salesperson WHERE slp_code IS NULL;

-- ── Quick manual refresh (run once Staging:ConnectionString is set) ──
-- SELECT * FROM stg.refresh_all('company-dev-001');
