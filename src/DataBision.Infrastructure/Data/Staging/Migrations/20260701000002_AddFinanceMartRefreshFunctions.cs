using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    public partial class AddFinanceMartRefreshFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_ar_aging(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.ar_aging WHERE company_id = p_company_id;

  INSERT INTO mart.ar_aging (
    company_id, card_code, card_name,
    current_amount, bucket_1_30, bucket_31_60,
    bucket_61_90, bucket_91_120, bucket_over_120,
    total_open, invoice_count, oldest_due_date, refreshed_at
  )
  SELECT
    p_company_id,
    card_code,
    MAX(card_name)                                                AS card_name,
    SUM(CASE WHEN (doc_due_date::date - CURRENT_DATE) >= 0
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS current_amount,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 1  AND 30
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_1_30,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 31 AND 60
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_31_60,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 61 AND 90
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_61_90,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 91 AND 120
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_91_120,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) > 120
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_over_120,
    SUM(doc_total - COALESCE(paid_to_date, 0))                    AS total_open,
    COUNT(*)                                                       AS invoice_count,
    MIN(doc_due_date::date)                                        AS oldest_due_date,
    NOW()
  FROM raw.sap_oinv
  WHERE company_id = p_company_id
    AND doc_status = 'O'
    AND cancelled  = 'N'
    AND doc_due_date IS NOT NULL
  GROUP BY card_code
  HAVING SUM(doc_total - COALESCE(paid_to_date, 0)) > 0;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_ap_aging(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.ap_aging WHERE company_id = p_company_id;

  INSERT INTO mart.ap_aging (
    company_id, card_code, card_name,
    current_amount, bucket_1_30, bucket_31_60,
    bucket_61_90, bucket_91_120, bucket_over_120,
    total_open, invoice_count, oldest_due_date, refreshed_at
  )
  SELECT
    p_company_id,
    card_code,
    MAX(card_name)                                                AS card_name,
    SUM(CASE WHEN (doc_due_date::date - CURRENT_DATE) >= 0
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS current_amount,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 1  AND 30
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_1_30,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 31 AND 60
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_31_60,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 61 AND 90
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_61_90,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) BETWEEN 91 AND 120
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_91_120,
    SUM(CASE WHEN (CURRENT_DATE - doc_due_date::date) > 120
             THEN doc_total - COALESCE(paid_to_date, 0) ELSE 0 END) AS bucket_over_120,
    SUM(doc_total - COALESCE(paid_to_date, 0))                    AS total_open,
    COUNT(*)                                                       AS invoice_count,
    MIN(doc_due_date::date)                                        AS oldest_due_date,
    NOW()
  FROM raw.sap_opch
  WHERE company_id = p_company_id
    AND doc_status = 'O'
    AND cancelled  = 'N'
    AND doc_due_date IS NOT NULL
  GROUP BY card_code
  HAVING SUM(doc_total - COALESCE(paid_to_date, 0)) > 0;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_finance_period_kpi(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.finance_period_kpi WHERE company_id = p_company_id;

  INSERT INTO mart.finance_period_kpi (
    company_id, period_year, period_month,
    ar_billed, ar_credit_memo, ar_net, ar_invoice_count,
    ap_billed, ap_credit_memo, ap_net, ap_invoice_count,
    refreshed_at
  )
  WITH ar_inv AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS billed,
      COUNT(*)                                AS inv_count
    FROM raw.sap_oinv
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  ),
  ar_cm AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS credit_amount
    FROM raw.sap_orin
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  ),
  ap_inv AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS billed,
      COUNT(*)                                AS inv_count
    FROM raw.sap_opch
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  ),
  ap_cm AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS credit_amount
    FROM raw.sap_orpc
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  ),
  periods AS (
    SELECT yr, mo FROM ar_inv
    UNION SELECT yr, mo FROM ap_inv
  )
  SELECT
    p_company_id,
    p.yr, p.mo,
    COALESCE(ai.billed, 0),
    COALESCE(ac.credit_amount, 0),
    COALESCE(ai.billed, 0) - COALESCE(ac.credit_amount, 0),
    COALESCE(ai.inv_count, 0),
    COALESCE(api.billed, 0),
    COALESCE(apc.credit_amount, 0),
    COALESCE(api.billed, 0) - COALESCE(apc.credit_amount, 0),
    COALESCE(api.inv_count, 0),
    NOW()
  FROM periods p
  LEFT JOIN ar_inv  ai  ON ai.yr  = p.yr AND ai.mo  = p.mo
  LEFT JOIN ar_cm   ac  ON ac.yr  = p.yr AND ac.mo  = p.mo
  LEFT JOIN ap_inv  api ON api.yr = p.yr AND api.mo = p.mo
  LEFT JOIN ap_cm   apc ON apc.yr = p.yr AND apc.mo = p.mo;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_finance_summary(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.finance_summary WHERE company_id = p_company_id;

  INSERT INTO mart.finance_summary (
    company_id,
    total_open_ar, total_overdue_ar, ar_customer_count, dso_days,
    total_open_ap, total_overdue_ap, ap_supplier_count, dpo_days,
    refreshed_at
  )
  WITH ar AS (
    SELECT
      COALESCE(SUM(total_open), 0)                                                              AS total_open,
      COALESCE(SUM(bucket_1_30 + bucket_31_60 + bucket_61_90 + bucket_91_120 + bucket_over_120), 0) AS total_overdue,
      COUNT(DISTINCT card_code)                                                                  AS customer_count
    FROM mart.ar_aging
    WHERE company_id = p_company_id
  ),
  ap AS (
    SELECT
      COALESCE(SUM(total_open), 0)                                                              AS total_open,
      COALESCE(SUM(bucket_1_30 + bucket_31_60 + bucket_61_90 + bucket_91_120 + bucket_over_120), 0) AS total_overdue,
      COUNT(DISTINCT card_code)                                                                  AS supplier_count
    FROM mart.ap_aging
    WHERE company_id = p_company_id
  ),
  ar_ltm AS (
    SELECT SUM(ar_net) AS ar_net_ltm
    FROM mart.finance_period_kpi
    WHERE company_id = p_company_id
      AND (period_year * 100 + period_month) >
          (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '12 months')::INT * 100 +
           EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '12 months')::INT)
  ),
  ap_ltm AS (
    SELECT SUM(ap_net) AS ap_net_ltm
    FROM mart.finance_period_kpi
    WHERE company_id = p_company_id
      AND (period_year * 100 + period_month) >
          (EXTRACT(YEAR  FROM CURRENT_DATE - INTERVAL '12 months')::INT * 100 +
           EXTRACT(MONTH FROM CURRENT_DATE - INTERVAL '12 months')::INT)
  )
  SELECT
    p_company_id,
    ar.total_open,
    ar.total_overdue,
    ar.customer_count::INT,
    CASE WHEN COALESCE(alt.ar_net_ltm, 0) > 0
         THEN ROUND((ar.total_open / (alt.ar_net_ltm / 365)), 1)
         ELSE NULL END,
    ap.total_open,
    ap.total_overdue,
    ap.supplier_count::INT,
    CASE WHEN COALESCE(apt.ap_net_ltm, 0) > 0
         THEN ROUND((ap.total_open / (apt.ap_net_ltm / 365)), 1)
         ELSE NULL END,
    NOW()
  FROM ar
  CROSS JOIN ap
  CROSS JOIN ar_ltm alt
  CROSS JOIN ap_ltm apt;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_finance(p_company_id TEXT)
RETURNS TABLE(object_name TEXT, rows_affected INT) AS $$
BEGIN
  RETURN QUERY SELECT 'ar_aging'::TEXT,           mart.refresh_ar_aging(p_company_id);
  RETURN QUERY SELECT 'ap_aging'::TEXT,           mart.refresh_ap_aging(p_company_id);
  RETURN QUERY SELECT 'finance_period_kpi'::TEXT, mart.refresh_finance_period_kpi(p_company_id);
  RETURN QUERY SELECT 'finance_summary'::TEXT,    mart.refresh_finance_summary(p_company_id);
END;
$$ LANGUAGE plpgsql;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_finance(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_finance_summary(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_finance_period_kpi(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_ap_aging(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_ar_aging(TEXT);");
        }
    }
}
