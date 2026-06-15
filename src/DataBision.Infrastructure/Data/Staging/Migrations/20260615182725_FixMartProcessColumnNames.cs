using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Corrects column name typo in mart process functions:
    ///   m.items_group_code → m.item_group_code (stg.item actual column is item_group_code INTEGER)
    /// Affects mart.refresh_sales_process (item_dashboard) and mart.refresh_inventory_process (rotation).
    /// Both functions also already carry the stg.sales_invoice_line fix from FixMartProcessFunctions.
    /// </summary>
    public partial class FixMartProcessColumnNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix mart.refresh_sales_process — MAX(m.items_group_code) → MAX(m.item_group_code::TEXT)
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_sales_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM mart.refresh_sales_daily(p_company_id);
                    PERFORM mart.refresh_sales_monthly(p_company_id);
                    PERFORM mart.refresh_customer_sales(p_company_id);
                    PERFORM mart.refresh_item_sales(p_company_id);
                    PERFORM mart.refresh_salesperson_sales(p_company_id);
                    PERFORM mart.refresh_sales_kpi_summary(p_company_id);

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='sales_invoice') THEN
                        INSERT INTO mart.sales_customer_dashboard (
                            company_id, card_code, card_name, card_type,
                            gross_sales, credit_memos, net_sales,
                            invoice_count, avg_ticket,
                            first_invoice_date, last_invoice_date, is_active, transformed_at_utc
                        )
                        SELECT
                            i.company_id, i.card_code, MAX(i.card_name), MAX(c.card_type),
                            SUM(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN COALESCE(i.doc_total,0) ELSE 0 END),
                            0,
                            SUM(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN COALESCE(i.doc_total,0) ELSE 0 END),
                            COUNT(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN 1 END),
                            CASE WHEN COUNT(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN 1 END) > 0
                                 THEN SUM(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN COALESCE(i.doc_total,0) ELSE 0 END)
                                      / COUNT(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN 1 END)
                                 ELSE 0 END,
                            MIN(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN i.doc_date END),
                            MAX(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN i.doc_date END),
                            MAX(CASE WHEN COALESCE(i.cancelled,'N') != 'Y' THEN i.doc_date END) >= CURRENT_DATE - 90,
                            NOW()
                        FROM stg.sales_invoice i
                        LEFT JOIN stg.customer c ON c.company_id = i.company_id AND c.card_code = i.card_code
                        WHERE i.company_id = p_company_id
                        GROUP BY i.company_id, i.card_code
                        ON CONFLICT (company_id, card_code) DO UPDATE SET
                            card_name = EXCLUDED.card_name, card_type = EXCLUDED.card_type,
                            gross_sales = EXCLUDED.gross_sales, net_sales = EXCLUDED.net_sales,
                            invoice_count = EXCLUDED.invoice_count, avg_ticket = EXCLUDED.avg_ticket,
                            first_invoice_date = EXCLUDED.first_invoice_date,
                            last_invoice_date = EXCLUDED.last_invoice_date,
                            is_active = EXCLUDED.is_active, transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_invoice not available, skipping customer_dashboard';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='sales_invoice_line') THEN
                        INSERT INTO mart.sales_item_dashboard (
                            company_id, item_code, item_name, item_group_code,
                            quantity_sold, gross_sales, invoice_count, line_count,
                            first_sale_date, last_sale_date, transformed_at_utc
                        )
                        SELECT
                            l.company_id, l.item_code,
                            MAX(m.item_name),
                            MAX(m.item_group_code::TEXT),
                            SUM(COALESCE(l.quantity, 0)), SUM(COALESCE(l.line_total, 0)),
                            COUNT(DISTINCT l.doc_entry), COUNT(*),
                            MIN(i.doc_date), MAX(i.doc_date), NOW()
                        FROM stg.sales_invoice_line l
                        LEFT JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                        LEFT JOIN stg.item m ON m.company_id = l.company_id AND m.item_code = l.item_code
                        WHERE l.company_id = p_company_id
                        GROUP BY l.company_id, l.item_code
                        ON CONFLICT (company_id, item_code) DO UPDATE SET
                            item_name = EXCLUDED.item_name, item_group_code = EXCLUDED.item_group_code,
                            quantity_sold = EXCLUDED.quantity_sold, gross_sales = EXCLUDED.gross_sales,
                            invoice_count = EXCLUDED.invoice_count, line_count = EXCLUDED.line_count,
                            first_sale_date = EXCLUDED.first_sale_date, last_sale_date = EXCLUDED.last_sale_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_invoice_line not available, skipping item_dashboard';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema='stg' AND table_name='sales_order')
                    AND EXISTS (SELECT FROM information_schema.tables WHERE table_schema='stg' AND table_name='delivery') THEN
                        RAISE NOTICE 'refresh_sales_process: ORDR/ODLN available — fulfillment refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_order/delivery not available, skipping fulfillment';
                    END IF;
                END;
                $$;
                """);

            // Fix mart.refresh_inventory_process — m.items_group_code::TEXT → m.item_group_code::TEXT
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
                            item_name = EXCLUDED.item_name, item_group_code = EXCLUDED.item_group_code,
                            qty_sold_30d = EXCLUDED.qty_sold_30d, qty_sold_90d = EXCLUDED.qty_sold_90d,
                            last_sale_date = EXCLUDED.last_sale_date,
                            avg_daily_sales_qty = EXCLUDED.avg_daily_sales_qty,
                            rotation_status = EXCLUDED.rotation_status, transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item not available, skipping rotation';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='item_warehouse') THEN
                        RAISE NOTICE 'refresh_inventory_process: OITW available — stock refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item_warehouse not available, skipping stock';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='stock_transfer') THEN
                        RAISE NOTICE 'refresh_inventory_process: OWTR available — warehouse refresh TBD';
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
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_process(TEXT);");
        }
    }
}
