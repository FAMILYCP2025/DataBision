using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    public partial class AddInventoryMartRefreshFunctions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_inventory_snapshot(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.inventory_snapshot WHERE company_id = p_company_id;

  INSERT INTO mart.inventory_snapshot (
    company_id, item_code, item_name, item_group_name,
    on_hand, committed, ordered, available,
    avg_price, stock_value,
    last_purchase_date, last_sale_date, refreshed_at
  )
  SELECT
    p_company_id,
    w.item_code,
    m.item_name,
    NULL::TEXT,
    SUM(w.on_hand)                                          AS on_hand,
    SUM(w.is_committed)                                     AS committed,
    SUM(w.on_order)                                         AS ordered,
    SUM(w.on_hand) - SUM(w.is_committed) + SUM(w.on_order) AS available,
    COALESCE(m.avg_price, 0)                                AS avg_price,
    SUM(w.on_hand) * COALESCE(m.avg_price, 0)              AS stock_value,
    NULL::DATE                                              AS last_purchase_date,
    NULL::DATE                                              AS last_sale_date,
    NOW()
  FROM raw.sap_oitw w
  LEFT JOIN raw.sap_oitm m
    ON m.company_id = w.company_id AND m.item_code = w.item_code
  WHERE w.company_id = p_company_id
    AND w.on_hand > 0
  GROUP BY w.item_code, m.item_name, m.avg_price;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_inventory_movement_kpi(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.inventory_movement_kpi WHERE company_id = p_company_id;

  INSERT INTO mart.inventory_movement_kpi (
    company_id, period_year, period_month,
    inbound_qty, outbound_qty, net_qty,
    inbound_value, outbound_value,
    transaction_count, refreshed_at
  )
  SELECT
    p_company_id,
    EXTRACT(YEAR  FROM doc_date::date)::INT             AS period_year,
    EXTRACT(MONTH FROM doc_date::date)::INT             AS period_month,
    SUM(CASE WHEN in_qty  > 0 THEN in_qty  ELSE 0 END) AS inbound_qty,
    SUM(CASE WHEN out_qty > 0 THEN out_qty ELSE 0 END) AS outbound_qty,
    SUM(in_qty - out_qty)                               AS net_qty,
    SUM(CASE WHEN in_qty  > 0 THEN price * in_qty  ELSE 0 END) AS inbound_value,
    SUM(CASE WHEN out_qty > 0 THEN price * out_qty ELSE 0 END) AS outbound_value,
    COUNT(*)                                            AS transaction_count,
    NOW()
  FROM raw.sap_oinm
  WHERE company_id = p_company_id
    AND doc_date IS NOT NULL
  GROUP BY period_year, period_month;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_slow_moving_items(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.slow_moving_items WHERE company_id = p_company_id;

  INSERT INTO mart.slow_moving_items (
    company_id, item_code, item_name, item_group_name,
    on_hand, stock_value, last_movement_date, days_without_movement, refreshed_at
  )
  WITH last_mv AS (
    SELECT
      item_code,
      MAX(doc_date::date) AS last_movement_date
    FROM raw.sap_oinm
    WHERE company_id = p_company_id
    GROUP BY item_code
  )
  SELECT
    p_company_id,
    w.item_code,
    m.item_name,
    NULL::TEXT,
    SUM(w.on_hand)                             AS on_hand,
    SUM(w.on_hand) * COALESCE(m.avg_price, 0) AS stock_value,
    lm.last_movement_date,
    (CURRENT_DATE - COALESCE(lm.last_movement_date, '1900-01-01'::date))::INT AS days_without_movement,
    NOW()
  FROM raw.sap_oitw w
  LEFT JOIN raw.sap_oitm m
    ON m.company_id = w.company_id AND m.item_code = w.item_code
  LEFT JOIN last_mv lm ON lm.item_code = w.item_code
  WHERE w.company_id = p_company_id
    AND w.on_hand > 0
    AND (CURRENT_DATE - COALESCE(lm.last_movement_date, '1900-01-01'::date)) >= 90
  GROUP BY w.item_code, m.item_name, m.avg_price, lm.last_movement_date;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_warehouse_stock(p_company_id TEXT)
RETURNS INT AS $$
DECLARE v_count INT;
BEGIN
  DELETE FROM mart.warehouse_stock WHERE company_id = p_company_id;

  INSERT INTO mart.warehouse_stock (
    company_id, warehouse_code, warehouse_name,
    total_items, total_on_hand, total_stock_value,
    items_below_min, refreshed_at
  )
  SELECT
    p_company_id,
    w.warehouse_code,
    wh.whs_name,
    COUNT(DISTINCT w.item_code)::INT                                                          AS total_items,
    SUM(w.on_hand)                                                                            AS total_on_hand,
    SUM(w.on_hand * COALESCE(m.avg_price, 0))                                                AS total_stock_value,
    SUM(CASE WHEN w.on_hand < COALESCE(m.min_level, 0) AND m.min_level > 0 THEN 1 ELSE 0 END)::INT AS items_below_min,
    NOW()
  FROM raw.sap_oitw w
  LEFT JOIN raw.sap_oitm m
    ON m.company_id = w.company_id AND m.item_code = w.item_code
  LEFT JOIN raw.sap_owhs wh
    ON wh.company_id = w.company_id AND wh.warehouse_code = w.warehouse_code
  WHERE w.company_id = p_company_id
    AND w.on_hand > 0
  GROUP BY w.warehouse_code, wh.whs_name;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$ LANGUAGE plpgsql;
");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION mart.refresh_inventory(p_company_id TEXT)
RETURNS TABLE(object_name TEXT, rows_affected INT) AS $$
BEGIN
  RETURN QUERY SELECT 'inventory_snapshot'::TEXT,     mart.refresh_inventory_snapshot(p_company_id);
  RETURN QUERY SELECT 'inventory_movement_kpi'::TEXT, mart.refresh_inventory_movement_kpi(p_company_id);
  RETURN QUERY SELECT 'slow_moving_items'::TEXT,      mart.refresh_slow_moving_items(p_company_id);
  RETURN QUERY SELECT 'warehouse_stock'::TEXT,        mart.refresh_warehouse_stock(p_company_id);
END;
$$ LANGUAGE plpgsql;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_warehouse_stock(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_slow_moving_items(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory_movement_kpi(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory_snapshot(TEXT);");
        }
    }
}
