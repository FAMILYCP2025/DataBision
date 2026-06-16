using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Corrective migration: mart.refresh_inventory_process inventory_stock_dashboard INSERT
    /// used MAX(i.item_name) without GROUP BY, causing 42803. Since stg.item has PK
    /// (company_id, item_code), the LEFT JOIN produces at most one row per iw row — no
    /// aggregate or GROUP BY needed. Use scalar columns directly.
    /// </summary>
    public partial class FixInventoryStockGroupBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_inventory_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='item') THEN
                        INSERT INTO mart.inventory_rotation_dashboard (
                            company_id, item_code, item_name, item_group_code,
                            qty_sold_30d, qty_sold_90d, last_sale_date, avg_daily_sales_qty,
                            rotation_status, transformed_at_utc
                        )
                        SELECT
                            m.company_id, m.item_code, m.item_name, m.item_group_code::TEXT,
                            COALESCE(s30.qty, 0), COALESCE(s90.qty, 0), s90.last_date,
                            ROUND(COALESCE(s90.qty, 0) / 90.0, 4),
                            CASE
                                WHEN COALESCE(s30.qty, 0) > 0 THEN 'FAST'
                                WHEN COALESCE(s90.qty, 0) > 0 THEN 'NORMAL'
                                WHEN s90.last_date IS NOT NULL THEN 'SLOW'
                                ELSE 'NO_MOVEMENT'
                            END,
                            NOW()
                        FROM stg.item m
                        LEFT JOIN LATERAL (
                            SELECT SUM(COALESCE(l.quantity,0)) AS qty
                            FROM stg.sales_invoice_line l
                            JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                            WHERE l.company_id = m.company_id AND l.item_code = m.item_code
                              AND COALESCE(i.cancelled,'N') != 'Y' AND i.doc_date >= CURRENT_DATE - 30
                        ) s30 ON TRUE
                        LEFT JOIN LATERAL (
                            SELECT SUM(COALESCE(l.quantity,0)) AS qty, MAX(i.doc_date) AS last_date
                            FROM stg.sales_invoice_line l
                            JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                            WHERE l.company_id = m.company_id AND l.item_code = m.item_code
                              AND COALESCE(i.cancelled,'N') != 'Y' AND i.doc_date >= CURRENT_DATE - 90
                        ) s90 ON TRUE
                        WHERE m.company_id = p_company_id
                        ON CONFLICT (company_id, item_code) DO UPDATE SET
                            item_name           = EXCLUDED.item_name,
                            item_group_code     = EXCLUDED.item_group_code,
                            qty_sold_30d        = EXCLUDED.qty_sold_30d,
                            qty_sold_90d        = EXCLUDED.qty_sold_90d,
                            last_sale_date      = EXCLUDED.last_sale_date,
                            avg_daily_sales_qty = EXCLUDED.avg_daily_sales_qty,
                            rotation_status     = EXCLUDED.rotation_status,
                            transformed_at_utc  = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item not available, skipping rotation';
                    END IF;

                    -- stg.item has PK (company_id, item_code) so the LEFT JOIN to stg.item
                    -- produces at most one match per iw row — no aggregate/GROUP BY needed.
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='item_warehouse') THEN
                        INSERT INTO mart.inventory_stock_dashboard (
                            company_id, warehouse_code, item_code,
                            item_name, item_group_code,
                            on_hand_qty, committed_qty, available_qty,
                            avg_cost_price, stock_value, is_stockout,
                            transformed_at_utc
                        )
                        SELECT
                            iw.company_id, iw.whs_code, iw.item_code,
                            i.item_name,
                            i.item_group_code::TEXT,
                            COALESCE(iw.on_hand, 0),
                            COALESCE(iw.is_committed, 0),
                            COALESCE(iw.on_hand, 0) - COALESCE(iw.is_committed, 0),
                            NULL::NUMERIC,
                            NULL::NUMERIC,
                            COALESCE(iw.on_hand, 0) <= 0,
                            NOW()
                        FROM stg.item_warehouse iw
                        LEFT JOIN stg.item i ON i.company_id = iw.company_id AND i.item_code = iw.item_code
                        WHERE iw.company_id = p_company_id
                        ON CONFLICT (company_id, warehouse_code, item_code) DO UPDATE SET
                            item_name          = EXCLUDED.item_name,
                            item_group_code    = EXCLUDED.item_group_code,
                            on_hand_qty        = EXCLUDED.on_hand_qty,
                            committed_qty      = EXCLUDED.committed_qty,
                            available_qty      = EXCLUDED.available_qty,
                            is_stockout        = EXCLUDED.is_stockout,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item_warehouse not available, skipping stock';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='stock_transfer') THEN
                        WITH out_agg AS (
                            SELECT from_warehouse AS warehouse_code,
                                COUNT(*)                   AS transfer_out_count,
                                SUM(COALESCE(doc_total,0)) AS transfer_out_qty,
                                MAX(doc_date)              AS last_date
                            FROM stg.stock_transfer
                            WHERE company_id = p_company_id AND from_warehouse IS NOT NULL
                            GROUP BY from_warehouse
                        ),
                        in_agg AS (
                            SELECT to_warehouse AS warehouse_code,
                                COUNT(*)                   AS transfer_in_count,
                                SUM(COALESCE(doc_total,0)) AS transfer_in_qty,
                                MAX(doc_date)              AS last_date
                            FROM stg.stock_transfer
                            WHERE company_id = p_company_id AND to_warehouse IS NOT NULL
                            GROUP BY to_warehouse
                        ),
                        combined AS (
                            SELECT
                                COALESCE(o.warehouse_code, i.warehouse_code) AS warehouse_code,
                                COALESCE(o.transfer_out_count, 0)            AS transfer_out_count,
                                COALESCE(o.transfer_out_qty, 0)              AS transfer_out_qty,
                                COALESCE(i.transfer_in_count, 0)             AS transfer_in_count,
                                COALESCE(i.transfer_in_qty, 0)               AS transfer_in_qty,
                                GREATEST(o.last_date, i.last_date)           AS last_transfer_date
                            FROM out_agg o FULL OUTER JOIN in_agg i ON i.warehouse_code = o.warehouse_code
                        )
                        INSERT INTO mart.inventory_warehouse_dashboard (
                            company_id, warehouse_code,
                            transfer_in_count, transfer_in_qty,
                            transfer_out_count, transfer_out_qty,
                            last_transfer_date, transformed_at_utc
                        )
                        SELECT
                            p_company_id, c.warehouse_code,
                            c.transfer_in_count, c.transfer_in_qty,
                            c.transfer_out_count, c.transfer_out_qty,
                            c.last_transfer_date, NOW()
                        FROM combined c
                        ON CONFLICT (company_id, warehouse_code) DO UPDATE SET
                            transfer_in_count  = EXCLUDED.transfer_in_count,
                            transfer_in_qty    = EXCLUDED.transfer_in_qty,
                            transfer_out_count = EXCLUDED.transfer_out_count,
                            transfer_out_qty   = EXCLUDED.transfer_out_qty,
                            last_transfer_date = EXCLUDED.last_transfer_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.stock_transfer not available, skipping warehouse';
                    END IF;
                END;
                $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory_process(TEXT);");
        }
    }
}
