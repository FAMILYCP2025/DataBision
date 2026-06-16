using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Corrective migration: re-writes three mart process functions applied in 20260615210000
    /// with wrong column names / wrong PK assumptions.
    ///
    /// mart.refresh_purchasing_process:
    ///   purchase_executive_daily was missing received_count / received_amount.
    ///   purchase_supplier_dashboard was missing received_amount.
    ///   Fix: insert PO data first (received=0 defaults), then UPDATE with receipt data.
    ///
    /// mart.refresh_sales_process:
    ///   sales_fulfillment_dashboard PK is (company_id, period_date), not (company_id, card_code).
    ///   Columns are orders_count/orders_amount/delivered_count/delivered_amount/
    ///   fill_rate_pct/pending_orders/avg_delivery_days, not card_name/orders_open/etc.
    ///   Fix: group by doc_date and insert into correct columns.
    ///
    /// mart.refresh_inventory_process:
    ///   inventory_stock_dashboard PK is (company_id, warehouse_code, item_code),
    ///   not (company_id, item_code); columns are on_hand_qty/committed_qty/available_qty/
    ///   is_stockout, not total_on_hand/total_committed/total_on_order/warehouse_count.
    ///   inventory_warehouse_dashboard columns are transfer_in_count/transfer_in_qty/
    ///   transfer_out_count/transfer_out_qty, not transfers_in/transfers_out/net_movement.
    /// </summary>
    public partial class FixMartProcessFunctionsV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── mart.refresh_purchasing_process ───────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_purchasing_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_order') THEN

                        INSERT INTO mart.purchase_executive_daily (
                            company_id, purchase_date,
                            po_count, po_amount,
                            received_count, received_amount,
                            active_suppliers, transformed_at_utc
                        )
                        SELECT
                            company_id, doc_date,
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            0, 0,
                            COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN card_code END),
                            NOW()
                        FROM stg.purchase_order
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, doc_date
                        ON CONFLICT (company_id, purchase_date) DO UPDATE SET
                            po_count           = EXCLUDED.po_count,
                            po_amount          = EXCLUDED.po_amount,
                            active_suppliers   = EXCLUDED.active_suppliers,
                            transformed_at_utc = NOW();

                        INSERT INTO mart.purchase_supplier_dashboard (
                            company_id, supplier_code, supplier_name,
                            po_count, po_amount, received_amount,
                            last_po_date, avg_po_amount, transformed_at_utc
                        )
                        SELECT
                            company_id, card_code, MAX(card_name),
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            0,
                            MAX(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END),
                            CASE WHEN COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) > 0
                                 THEN ROUND(
                                     SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END)
                                     / COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END), 6)
                                 ELSE 0 END,
                            NOW()
                        FROM stg.purchase_order
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, supplier_code) DO UPDATE SET
                            supplier_name      = EXCLUDED.supplier_name,
                            po_count           = EXCLUDED.po_count,
                            po_amount          = EXCLUDED.po_amount,
                            last_po_date       = EXCLUDED.last_po_date,
                            avg_po_amount      = EXCLUDED.avg_po_amount,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_order not available, skipping';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_receipt') THEN

                        UPDATE mart.purchase_executive_daily ped
                        SET
                            received_count     = gr.cnt,
                            received_amount    = gr.amt,
                            transformed_at_utc = NOW()
                        FROM (
                            SELECT doc_date,
                                COUNT(*)                   AS cnt,
                                SUM(COALESCE(doc_total,0)) AS amt
                            FROM stg.purchase_receipt
                            WHERE company_id = p_company_id AND doc_date IS NOT NULL
                            GROUP BY doc_date
                        ) gr
                        WHERE ped.company_id = p_company_id AND ped.purchase_date = gr.doc_date;

                        UPDATE mart.purchase_supplier_dashboard psd
                        SET
                            received_amount    = gr.amt,
                            transformed_at_utc = NOW()
                        FROM (
                            SELECT card_code, SUM(COALESCE(doc_total,0)) AS amt
                            FROM stg.purchase_receipt
                            WHERE company_id = p_company_id
                            GROUP BY card_code
                        ) gr
                        WHERE psd.company_id = p_company_id AND psd.supplier_code = gr.card_code;

                        INSERT INTO mart.purchase_receiving_dashboard (
                            company_id, supplier_code, supplier_name,
                            gr_count, gr_amount, last_gr_date, transformed_at_utc
                        )
                        SELECT
                            company_id, card_code, MAX(card_name),
                            COUNT(*),
                            SUM(COALESCE(doc_total,0)),
                            MAX(doc_date),
                            NOW()
                        FROM stg.purchase_receipt
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, supplier_code) DO UPDATE SET
                            supplier_name      = EXCLUDED.supplier_name,
                            gr_count           = EXCLUDED.gr_count,
                            gr_amount          = EXCLUDED.gr_amount,
                            last_gr_date       = EXCLUDED.last_gr_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_receipt not available, skipping';
                    END IF;
                END;
                $$;
                """);

            // ── mart.refresh_sales_process ────────────────────────────────────────
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
                            card_name          = EXCLUDED.card_name,
                            card_type          = EXCLUDED.card_type,
                            gross_sales        = EXCLUDED.gross_sales,
                            net_sales          = EXCLUDED.net_sales,
                            invoice_count      = EXCLUDED.invoice_count,
                            avg_ticket         = EXCLUDED.avg_ticket,
                            first_invoice_date = EXCLUDED.first_invoice_date,
                            last_invoice_date  = EXCLUDED.last_invoice_date,
                            is_active          = EXCLUDED.is_active,
                            transformed_at_utc = NOW();
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
                            l.company_id, l.item_code, MAX(m.item_name),
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
                            item_name          = EXCLUDED.item_name,
                            item_group_code    = EXCLUDED.item_group_code,
                            quantity_sold      = EXCLUDED.quantity_sold,
                            gross_sales        = EXCLUDED.gross_sales,
                            invoice_count      = EXCLUDED.invoice_count,
                            line_count         = EXCLUDED.line_count,
                            first_sale_date    = EXCLUDED.first_sale_date,
                            last_sale_date     = EXCLUDED.last_sale_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_invoice_line not available, skipping item_dashboard';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema='stg' AND table_name='sales_order')
                    AND EXISTS (SELECT FROM information_schema.tables WHERE table_schema='stg' AND table_name='delivery') THEN
                        WITH order_by_date AS (
                            SELECT doc_date,
                                COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END)                           AS orders_count,
                                SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END)  AS orders_amount
                            FROM stg.sales_order
                            WHERE company_id = p_company_id AND doc_date IS NOT NULL
                            GROUP BY doc_date
                        ),
                        delivery_by_date AS (
                            SELECT doc_date,
                                COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END)                           AS delivered_count,
                                SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END)  AS delivered_amount
                            FROM stg.delivery
                            WHERE company_id = p_company_id AND doc_date IS NOT NULL
                            GROUP BY doc_date
                        ),
                        all_dates AS (
                            SELECT doc_date FROM order_by_date
                            UNION
                            SELECT doc_date FROM delivery_by_date
                        )
                        INSERT INTO mart.sales_fulfillment_dashboard (
                            company_id, period_date,
                            orders_count, orders_amount,
                            delivered_count, delivered_amount,
                            fill_rate_pct, pending_orders, avg_delivery_days,
                            transformed_at_utc
                        )
                        SELECT
                            p_company_id,
                            d.doc_date,
                            COALESCE(o.orders_count, 0),
                            COALESCE(o.orders_amount, 0),
                            COALESCE(del.delivered_count, 0),
                            COALESCE(del.delivered_amount, 0),
                            CASE WHEN COALESCE(o.orders_count, 0) > 0
                                 THEN ROUND(COALESCE(del.delivered_count,0)::NUMERIC / o.orders_count * 100, 4)
                                 ELSE 0 END,
                            GREATEST(COALESCE(o.orders_count,0) - COALESCE(del.delivered_count,0), 0),
                            NULL::NUMERIC,
                            NOW()
                        FROM all_dates d
                        LEFT JOIN order_by_date o      ON o.doc_date   = d.doc_date
                        LEFT JOIN delivery_by_date del ON del.doc_date = d.doc_date
                        ON CONFLICT (company_id, period_date) DO UPDATE SET
                            orders_count       = EXCLUDED.orders_count,
                            orders_amount      = EXCLUDED.orders_amount,
                            delivered_count    = EXCLUDED.delivered_count,
                            delivered_amount   = EXCLUDED.delivered_amount,
                            fill_rate_pct      = EXCLUDED.fill_rate_pct,
                            pending_orders     = EXCLUDED.pending_orders,
                            avg_delivery_days  = EXCLUDED.avg_delivery_days,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_order/delivery not available, skipping fulfillment';
                    END IF;
                END;
                $$;
                """);

            // ── mart.refresh_inventory_process ────────────────────────────────────
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
                            MAX(i.item_name), MAX(i.item_group_code::TEXT),
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
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_purchasing_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory_process(TEXT);");
        }
    }
}
