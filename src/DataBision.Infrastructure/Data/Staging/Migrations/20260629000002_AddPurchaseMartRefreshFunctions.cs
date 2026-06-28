using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint Purchase MART: Creates PostgreSQL refresh functions for all purchase MART tables.
    ///
    /// Functions created (all CREATE OR REPLACE — idempotent):
    ///   mart.refresh_purchase_period_kpi(company_id) — aggregates OPCH + ORPC by year/month
    ///   mart.refresh_top_suppliers(company_id)        — supplier ranking, 12-month window, + DPO
    ///   mart.refresh_top_purchase_items(company_id)   — item ranking via PCH1 join, 12-month window
    ///   mart.refresh_open_purchase_orders(company_id) — open pipeline from OPOR (doc_status='O')
    ///   mart.refresh_purchases(company_id)            — orchestrator: calls all 4 functions in order,
    ///                                                   returns TABLE(object_name TEXT, rows_affected INT)
    ///
    /// mart.refresh_purchases is called by TransformationRunner.RefreshPurchasesMartAsync via ExecuteFunctionAsync.
    /// Column names (object_name, rows_affected) must match what ExecuteFunctionAsync selects.
    ///
    /// Applied manually: dotnet ef database update --context StagingDbContext (port 5432)
    /// </summary>
    public partial class AddPurchaseMartRefreshFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_purchase_period_kpi(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.purchase_period_kpi WHERE company_id = p_company_id;

  INSERT INTO mart.purchase_period_kpi (
    company_id, period_year, period_month,
    gross_purchases, credit_memo_amount, net_purchases,
    invoice_count, credit_memo_count, active_suppliers,
    avg_ticket, refreshed_at
  )
  WITH invoices AS (
    SELECT
      EXTRACT(YEAR  FROM doc_date::date)::INT AS yr,
      EXTRACT(MONTH FROM doc_date::date)::INT AS mo,
      SUM(doc_total)                          AS gross_purchases,
      COUNT(*)                                AS invoice_count,
      COUNT(DISTINCT card_code)               AS active_suppliers
    FROM raw.sap_opch
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
    FROM raw.sap_orpc
    WHERE company_id = p_company_id
      AND doc_date IS NOT NULL
      AND cancelled = 'N'
    GROUP BY yr, mo
  )
  SELECT
    p_company_id,
    i.yr, i.mo,
    COALESCE(i.gross_purchases, 0),
    COALESCE(c.credit_amount, 0),
    COALESCE(i.gross_purchases, 0) - COALESCE(c.credit_amount, 0),
    COALESCE(i.invoice_count, 0),
    COALESCE(c.credit_count, 0),
    COALESCE(i.active_suppliers, 0),
    CASE WHEN COALESCE(i.invoice_count, 0) > 0
         THEN COALESCE(i.gross_purchases, 0) / i.invoice_count
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
CREATE OR REPLACE FUNCTION mart.refresh_top_suppliers(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.top_suppliers WHERE company_id = p_company_id;

  INSERT INTO mart.top_suppliers (
    company_id, card_code, card_name,
    gross_purchases, credit_memo_amount, net_purchases,
    invoice_count, last_invoice_date, dpo_days, refreshed_at
  )
  WITH inv AS (
    SELECT
      card_code, card_name,
      SUM(doc_total)          AS gross_purchases,
      COUNT(*)                AS invoice_count,
      MAX(doc_date::date)     AS last_invoice_date
    FROM raw.sap_opch
    WHERE company_id = p_company_id
      AND doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND cancelled = 'N'
    GROUP BY card_code, card_name
  ),
  nc AS (
    SELECT card_code, SUM(doc_total) AS credit_amount
    FROM raw.sap_orpc
    WHERE company_id = p_company_id
      AND doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND cancelled = 'N'
    GROUP BY card_code
  ),
  ap AS (
    SELECT card_code, SUM(balance) AS open_balance
    FROM raw.sap_ocrd
    WHERE company_id = p_company_id
      AND crd_card_type = 'S'
    GROUP BY card_code
  )
  SELECT
    p_company_id,
    i.card_code, i.card_name,
    i.gross_purchases,
    COALESCE(nc.credit_amount, 0),
    i.gross_purchases - COALESCE(nc.credit_amount, 0),
    i.invoice_count,
    i.last_invoice_date,
    CASE
      WHEN (i.gross_purchases - COALESCE(nc.credit_amount, 0)) > 0
      THEN ROUND((COALESCE(ap.open_balance, 0) /
                  NULLIF(i.gross_purchases - COALESCE(nc.credit_amount, 0), 0)) * 365, 1)
      ELSE NULL
    END,
    NOW()
  FROM inv i
  LEFT JOIN nc ON i.card_code = nc.card_code
  LEFT JOIN ap ON i.card_code = ap.card_code;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_top_purchase_items(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.top_purchase_items WHERE company_id = p_company_id;

  INSERT INTO mart.top_purchase_items (
    company_id, item_code, item_name, item_group_name,
    gross_purchases, quantity_purchased, invoice_count, avg_unit_price, refreshed_at
  )
  WITH inv_lines AS (
    SELECT
      l.item_code,
      MAX(m.item_name)              AS item_name,
      SUM(l.line_total)             AS gross_purchases,
      SUM(l.quantity)               AS quantity_purchased,
      COUNT(DISTINCT l.doc_entry)   AS invoice_count
    FROM raw.sap_pch1 l
    JOIN raw.sap_opch h
      ON h.company_id = l.company_id AND h.doc_entry = l.doc_entry
    LEFT JOIN raw.sap_oitm m
      ON m.company_id = l.company_id AND m.item_code = l.item_code
    WHERE l.company_id = p_company_id
      AND h.doc_date::date >= CURRENT_DATE - INTERVAL '12 months'
      AND h.cancelled = 'N'
    GROUP BY l.item_code
  )
  SELECT
    p_company_id,
    i.item_code, i.item_name, NULL::TEXT,
    i.gross_purchases,
    i.quantity_purchased,
    i.invoice_count,
    CASE WHEN i.quantity_purchased > 0 THEN i.gross_purchases / i.quantity_purchased ELSE 0 END,
    NOW()
  FROM inv_lines i;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_open_purchase_orders(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.open_purchase_orders WHERE company_id = p_company_id;

  INSERT INTO mart.open_purchase_orders (
    company_id, doc_num, card_code, card_name,
    doc_date, doc_due_date, doc_total, open_amount,
    days_open, is_overdue, refreshed_at
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
    NOW()
  FROM raw.sap_opor h
  WHERE h.company_id = p_company_id
    AND h.doc_status = 'O'
    AND h.cancelled  = 'N';

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            // Orchestrator: calls all 4 functions in order.
            // Returns TABLE(object_name TEXT, rows_affected INT) to match ExecuteFunctionAsync contract.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_purchases(p_company_id TEXT)
RETURNS TABLE(object_name TEXT, rows_affected INT) AS $$
BEGIN
  RETURN QUERY SELECT 'purchase_period_kpi'::TEXT,   mart.refresh_purchase_period_kpi(p_company_id);
  RETURN QUERY SELECT 'top_suppliers'::TEXT,          mart.refresh_top_suppliers(p_company_id);
  RETURN QUERY SELECT 'top_purchase_items'::TEXT,     mart.refresh_top_purchase_items(p_company_id);
  RETURN QUERY SELECT 'open_purchase_orders'::TEXT,   mart.refresh_open_purchase_orders(p_company_id);
END;
$$ LANGUAGE plpgsql;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_purchases(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_open_purchase_orders(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_top_purchase_items(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_top_suppliers(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_purchase_period_kpi(TEXT);");
        }
    }
}
