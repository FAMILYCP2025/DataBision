using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint Sales MART: Creates PostgreSQL refresh functions for all sales MART tables.
    ///
    /// Functions created (all CREATE OR REPLACE — idempotent):
    ///   mart.refresh_sales_period_kpi(company_id)  — aggregates OINV + ORIN by year/month
    ///   mart.refresh_top_customers(company_id)      — customer ranking, 12-month window, + DSO
    ///   mart.refresh_top_items(company_id)          — item ranking via INV1/RIN1 join, 12-month window
    ///   mart.refresh_top_salespersons(company_id)   — salesperson ranking via OSLP join
    ///   mart.refresh_open_sales_orders(company_id)  — open pipeline from ORDR (doc_status='O')
    ///   mart.refresh_sales(company_id)              — orchestrator: calls all 5 functions in order,
    ///                                                 returns TABLE(object_name TEXT, rows_affected INT)
    ///
    /// mart.refresh_sales is called by TransformationRunner.RefreshSalesMartAsync via ExecuteFunctionAsync.
    /// Column names (object_name, rows_affected) must match what ExecuteFunctionAsync selects.
    ///
    /// Applied manually: dotnet ef database update --context StagingDbContext (port 5432)
    /// </summary>
    public partial class AddSalesMartRefreshFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_sales_period_kpi(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.sales_period_kpi WHERE company_id = p_company_id;

  INSERT INTO mart.sales_period_kpi (
    company_id, period_year, period_month,
    gross_sales, credit_memo_amount, net_sales,
    invoice_count, credit_memo_count, active_customers,
    avg_ticket, return_rate_pct, refreshed_at
  )
  WITH invoices AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS gross_sales,
      COUNT(*)                                AS invoice_count,
      COUNT(DISTINCT card_code)               AS active_customers
    FROM raw.sap_oinv
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  ),
  credits AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS credit_amount,
      COUNT(*)                                AS credit_count
    FROM raw.sap_orin
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  )
  SELECT
    p_company_id,
    i.yr, i.mo,
    COALESCE(i.gross_sales, 0),
    COALESCE(c.credit_amount, 0),
    COALESCE(i.gross_sales, 0) - COALESCE(c.credit_amount, 0),
    COALESCE(i.invoice_count, 0),
    COALESCE(c.credit_count, 0),
    COALESCE(i.active_customers, 0),
    CASE WHEN COALESCE(i.invoice_count, 0) > 0
         THEN COALESCE(i.gross_sales, 0) / i.invoice_count
         ELSE 0 END,
    CASE WHEN COALESCE(i.gross_sales, 0) > 0
         THEN (COALESCE(c.credit_amount, 0) / i.gross_sales) * 100
         ELSE 0 END,
    NOW()
  FROM invoices i
  LEFT JOIN credits c ON i.yr = c.yr AND i.mo = c.mo;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_top_customers(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.top_customers WHERE company_id = p_company_id;

  INSERT INTO mart.top_customers (
    company_id, card_code, card_name,
    gross_sales, credit_memo_amount, net_sales,
    invoice_count, last_invoice_date, dso_days, refreshed_at
  )
  WITH inv AS (
    SELECT
      card_code, card_name,
      SUM(doc_total)          AS gross_sales,
      COUNT(*)                AS invoice_count,
      MAX(doc_date::date)     AS last_invoice_date
    FROM raw.sap_oinv
    WHERE company_id = p_company_id
      AND doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND cancelled = 'N'
    GROUP BY card_code, card_name
  ),
  nc AS (
    SELECT card_code, SUM(doc_total) AS credit_amount
    FROM raw.sap_orin
    WHERE company_id = p_company_id
      AND doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND cancelled = 'N'
    GROUP BY card_code
  ),
  ar AS (
    SELECT card_code, SUM(balance) AS open_balance
    FROM raw.sap_ocrd
    WHERE company_id = p_company_id
      AND crd_card_type = 'C'
    GROUP BY card_code
  )
  SELECT
    p_company_id,
    i.card_code, i.card_name,
    i.gross_sales,
    COALESCE(nc.credit_amount, 0),
    i.gross_sales - COALESCE(nc.credit_amount, 0),
    i.invoice_count,
    i.last_invoice_date,
    CASE
      WHEN (i.gross_sales - COALESCE(nc.credit_amount, 0)) > 0
      THEN ROUND((COALESCE(ar.open_balance, 0) /
                  NULLIF(i.gross_sales - COALESCE(nc.credit_amount, 0), 0)) * 365, 1)
      ELSE NULL
    END,
    NOW()
  FROM inv i
  LEFT JOIN nc ON i.card_code = nc.card_code
  LEFT JOIN ar ON i.card_code = ar.card_code;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_top_items(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.top_items WHERE company_id = p_company_id;

  INSERT INTO mart.top_items (
    company_id, item_code, item_name, item_group_name,
    gross_sales, credit_memo_amount, net_sales,
    quantity_sold, invoice_count, avg_unit_price, refreshed_at
  )
  WITH inv_lines AS (
    SELECT
      l.item_code,
      MAX(m.item_name)              AS item_name,
      SUM(l.line_total)             AS gross_sales,
      SUM(l.quantity)               AS quantity_sold,
      COUNT(DISTINCT l.doc_entry)   AS invoice_count
    FROM raw.sap_inv1 l
    JOIN raw.sap_oinv h
      ON h.company_id = l.company_id AND h.doc_entry = l.doc_entry
    LEFT JOIN raw.sap_oitm m
      ON m.company_id = l.company_id AND m.item_code = l.item_code
    WHERE l.company_id = p_company_id
      AND h.doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND h.cancelled = 'N'
    GROUP BY l.item_code
  ),
  nc_lines AS (
    SELECT l.item_code, SUM(l.line_total) AS credit_amount
    FROM raw.sap_rin1 l
    JOIN raw.sap_orin h
      ON h.company_id = l.company_id AND h.doc_entry = l.doc_entry
    WHERE l.company_id = p_company_id
      AND h.doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND h.cancelled = 'N'
    GROUP BY l.item_code
  )
  SELECT
    p_company_id,
    i.item_code, i.item_name, NULL::TEXT,
    i.gross_sales,
    COALESCE(nc.credit_amount, 0),
    i.gross_sales - COALESCE(nc.credit_amount, 0),
    i.quantity_sold,
    i.invoice_count,
    CASE WHEN i.quantity_sold > 0 THEN i.gross_sales / i.quantity_sold ELSE 0 END,
    NOW()
  FROM inv_lines i
  LEFT JOIN nc_lines nc ON i.item_code = nc.item_code;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_top_salespersons(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.top_salespersons WHERE company_id = p_company_id;

  INSERT INTO mart.top_salespersons (
    company_id, sales_person_code, sales_person_name,
    net_sales, gross_sales, invoice_count,
    active_customers, avg_ticket, return_rate_pct, refreshed_at
  )
  WITH inv AS (
    SELECT
      h.slp_code,
      MAX(s.slp_name)              AS slp_name,
      SUM(h.doc_total)             AS gross_sales,
      COUNT(*)                     AS invoice_count,
      COUNT(DISTINCT h.card_code)  AS active_customers
    FROM raw.sap_oinv h
    LEFT JOIN raw.sap_oslp s
      ON s.company_id = h.company_id AND s.slp_code = h.slp_code
    WHERE h.company_id = p_company_id
      AND h.doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND h.cancelled = 'N'
      AND h.slp_code IS NOT NULL
    GROUP BY h.slp_code
  ),
  nc AS (
    SELECT h.slp_code, SUM(h.doc_total) AS credit_amount
    FROM raw.sap_orin h
    WHERE h.company_id = p_company_id
      AND h.doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND h.cancelled = 'N'
    GROUP BY h.slp_code
  )
  SELECT
    p_company_id,
    i.slp_code, i.slp_name,
    i.gross_sales - COALESCE(nc.credit_amount, 0),
    i.gross_sales,
    i.invoice_count, i.active_customers,
    CASE WHEN i.invoice_count > 0 THEN i.gross_sales / i.invoice_count ELSE 0 END,
    CASE WHEN i.gross_sales > 0 THEN (COALESCE(nc.credit_amount, 0) / i.gross_sales) * 100 ELSE 0 END,
    NOW()
  FROM inv i
  LEFT JOIN nc ON i.slp_code = nc.slp_code;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_open_sales_orders(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.open_sales_orders WHERE company_id = p_company_id;

  INSERT INTO mart.open_sales_orders (
    company_id, doc_num, card_code, card_name,
    doc_date, doc_due_date, doc_total, open_amount,
    days_open, is_overdue, sales_person_name, refreshed_at
  )
  SELECT
    p_company_id,
    h.doc_num,
    h.card_code, h.card_name,
    h.doc_date::date,
    h.doc_due_date::date,
    h.doc_total,
    h.doc_total - COALESCE(h.paid_to_date, 0),
    (CURRENT_DATE - h.doc_date::date)::INT,
    (CURRENT_DATE > h.doc_due_date::date),
    s.slp_name,
    NOW()
  FROM raw.sap_ordr h
  LEFT JOIN raw.sap_oslp s
    ON s.company_id = h.company_id AND s.slp_code = h.slp_code
  WHERE h.company_id = p_company_id
    AND h.doc_status = 'O'
    AND h.cancelled  = 'N';

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            // Orchestrator: calls all 5 functions in order.
            // Returns TABLE(object_name TEXT, rows_affected INT) to match ExecuteFunctionAsync contract.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_sales(p_company_id TEXT)
RETURNS TABLE(object_name TEXT, rows_affected INT) AS $$
BEGIN
  RETURN QUERY SELECT 'sales_period_kpi'::TEXT,  mart.refresh_sales_period_kpi(p_company_id);
  RETURN QUERY SELECT 'top_customers'::TEXT,      mart.refresh_top_customers(p_company_id);
  RETURN QUERY SELECT 'top_items'::TEXT,          mart.refresh_top_items(p_company_id);
  RETURN QUERY SELECT 'top_salespersons'::TEXT,   mart.refresh_top_salespersons(p_company_id);
  RETURN QUERY SELECT 'open_sales_orders'::TEXT,  mart.refresh_open_sales_orders(p_company_id);
END;
$$ LANGUAGE plpgsql;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_open_sales_orders(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_top_salespersons(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_top_items(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_top_customers(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_period_kpi(TEXT);");
        }
    }
}
