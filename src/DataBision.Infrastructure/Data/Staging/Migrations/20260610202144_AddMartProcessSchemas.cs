using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class AddMartProcessSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SALES supplement tables ────────────────────────────────────────────

            // mart.sales_customer_dashboard — enriched customer view (extends customer_sales)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_customer_dashboard (
                    company_id          TEXT            NOT NULL,
                    card_code           TEXT            NOT NULL,
                    card_name           TEXT,
                    card_type           TEXT,
                    sales_group_code    TEXT,
                    salesperson_code    TEXT,
                    salesperson_name    TEXT,
                    gross_sales         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memos        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales           NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count       INTEGER         NOT NULL DEFAULT 0,
                    avg_ticket          NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    first_invoice_date  DATE,
                    last_invoice_date   DATE,
                    is_active           BOOLEAN         NOT NULL DEFAULT FALSE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, card_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_cust_dash_net
                    ON mart.sales_customer_dashboard (company_id, net_sales DESC);
                """);

            // mart.sales_item_dashboard — item-level with nullable margin (cost not yet available)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_item_dashboard (
                    company_id          TEXT            NOT NULL,
                    item_code           TEXT            NOT NULL,
                    item_name           TEXT,
                    item_group_code     TEXT,
                    quantity_sold       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    gross_sales         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    cost_amount         NUMERIC(19,6),
                    gross_margin        NUMERIC(19,6),
                    gross_margin_pct    NUMERIC(10,4),
                    invoice_count       INTEGER         NOT NULL DEFAULT 0,
                    line_count          INTEGER         NOT NULL DEFAULT 0,
                    first_sale_date     DATE,
                    last_sale_date      DATE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, item_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_item_dash_gross
                    ON mart.sales_item_dashboard (company_id, gross_sales DESC);
                """);

            // mart.sales_fulfillment_dashboard — requires ORDR+ODLN (defensive)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_fulfillment_dashboard (
                    company_id          TEXT            NOT NULL,
                    period_date         DATE            NOT NULL,
                    orders_count        INTEGER         NOT NULL DEFAULT 0,
                    orders_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    delivered_count     INTEGER         NOT NULL DEFAULT 0,
                    delivered_amount    NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    fill_rate_pct       NUMERIC(10,4),
                    pending_orders      INTEGER         NOT NULL DEFAULT 0,
                    avg_delivery_days   NUMERIC(10,2),
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, period_date)
                );
                """);

            // ── PURCHASING tables ──────────────────────────────────────────────────

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.purchase_executive_daily (
                    company_id          TEXT            NOT NULL,
                    purchase_date       DATE            NOT NULL,
                    po_count            INTEGER         NOT NULL DEFAULT 0,
                    po_amount           NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    received_count      INTEGER         NOT NULL DEFAULT 0,
                    received_amount     NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    active_suppliers    INTEGER         NOT NULL DEFAULT 0,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, purchase_date)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_pur_exec_date
                    ON mart.purchase_executive_daily (company_id, purchase_date DESC);
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.purchase_supplier_dashboard (
                    company_id          TEXT            NOT NULL,
                    supplier_code       TEXT            NOT NULL,
                    supplier_name       TEXT,
                    po_count            INTEGER         NOT NULL DEFAULT 0,
                    po_amount           NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    received_amount     NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_po_date        DATE,
                    avg_po_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, supplier_code)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.purchase_receiving_dashboard (
                    company_id          TEXT            NOT NULL,
                    supplier_code       TEXT            NOT NULL,
                    supplier_name       TEXT,
                    gr_count            INTEGER         NOT NULL DEFAULT 0,
                    gr_amount           NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_gr_date        DATE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, supplier_code)
                );
                """);

            // ── INVENTORY tables ───────────────────────────────────────────────────

            // Requires OITW — defensive, stock fields nullable
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.inventory_stock_dashboard (
                    company_id          TEXT            NOT NULL,
                    warehouse_code      TEXT            NOT NULL,
                    item_code           TEXT            NOT NULL,
                    item_name           TEXT,
                    item_group_code     TEXT,
                    on_hand_qty         NUMERIC(19,6),
                    committed_qty       NUMERIC(19,6),
                    available_qty       NUMERIC(19,6),
                    avg_cost_price      NUMERIC(19,6),
                    stock_value         NUMERIC(19,6),
                    is_stockout         BOOLEAN         NOT NULL DEFAULT FALSE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, warehouse_code, item_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_inv_stock_item
                    ON mart.inventory_stock_dashboard (company_id, item_code);
                """);

            // Partially functional with OITM+OINV (on_hand requires OITW)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.inventory_rotation_dashboard (
                    company_id          TEXT            NOT NULL,
                    item_code           TEXT            NOT NULL,
                    item_name           TEXT,
                    item_group_code     TEXT,
                    qty_sold_30d        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    qty_sold_90d        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_sale_date      DATE,
                    avg_daily_sales_qty NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    on_hand_qty         NUMERIC(19,6),
                    coverage_days       NUMERIC(10,2),
                    rotation_status     TEXT            NOT NULL DEFAULT 'UNKNOWN',
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, item_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_inv_rot_status
                    ON mart.inventory_rotation_dashboard (company_id, rotation_status);
                """);

            // Requires OWTR — defensive
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.inventory_warehouse_dashboard (
                    company_id              TEXT            NOT NULL,
                    warehouse_code          TEXT            NOT NULL,
                    warehouse_name          TEXT,
                    transfer_in_count       INTEGER         NOT NULL DEFAULT 0,
                    transfer_in_qty         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transfer_out_count      INTEGER         NOT NULL DEFAULT 0,
                    transfer_out_qty        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_transfer_date      DATE,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, warehouse_code)
                );
                """);

            // ── FINANCE tables ─────────────────────────────────────────────────────

            // AR aging — functional with OINV (balance approximated as doc_total, no ledger data)
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.finance_ar_aging_dashboard (
                    company_id          TEXT            NOT NULL,
                    card_code           TEXT            NOT NULL,
                    card_name           TEXT,
                    invoice_count       INTEGER         NOT NULL DEFAULT 0,
                    total_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    balance_due         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    overdue_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_0_30          NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_31_60         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_61_90         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_90_plus       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_invoice_date   DATE,
                    oldest_overdue_date DATE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, card_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_fin_ar_overdue
                    ON mart.finance_ar_aging_dashboard (company_id, overdue_amount DESC);
                """);

            // AP aging — defensive, requires OPCH
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.finance_ap_aging_dashboard (
                    company_id          TEXT            NOT NULL,
                    supplier_code       TEXT            NOT NULL,
                    supplier_name       TEXT,
                    invoice_count       INTEGER         NOT NULL DEFAULT 0,
                    balance_due         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    overdue_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_0_30          NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_31_60         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_61_90         NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    aging_90_plus       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    oldest_overdue_date DATE,
                    transformed_at_utc  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, supplier_code)
                );
                """);

            // Finance executive daily — AR functional, AP defensive
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.finance_executive_daily (
                    company_id              TEXT            NOT NULL,
                    period_date             DATE            NOT NULL,
                    ar_total                NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    ar_overdue              NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    ar_overdue_pct          NUMERIC(10,4)   NOT NULL DEFAULT 0,
                    ap_total                NUMERIC(19,6),
                    ap_overdue              NUMERIC(19,6),
                    new_invoices_count      INTEGER         NOT NULL DEFAULT 0,
                    new_invoices_amount     NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, period_date)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_fin_exec_date
                    ON mart.finance_executive_daily (company_id, period_date DESC);
                """);

            // ── MART process functions ─────────────────────────────────────────────

            // mart.refresh_sales_process — calls existing SALES functions + supplements
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_sales_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    -- Existing SALES tables (called via existing functions)
                    PERFORM mart.refresh_sales_daily(p_company_id);
                    PERFORM mart.refresh_sales_monthly(p_company_id);
                    PERFORM mart.refresh_customer_sales(p_company_id);
                    PERFORM mart.refresh_item_sales(p_company_id);
                    PERFORM mart.refresh_salesperson_sales(p_company_id);
                    PERFORM mart.refresh_sales_kpi_summary(p_company_id);

                    -- Customer dashboard supplement
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='sales_invoice') THEN
                        INSERT INTO mart.sales_customer_dashboard (
                            company_id, card_code, card_name, card_type,
                            gross_sales, credit_memos, net_sales,
                            invoice_count, avg_ticket,
                            first_invoice_date, last_invoice_date, is_active, transformed_at_utc
                        )
                        SELECT
                            i.company_id,
                            i.card_code,
                            MAX(i.card_name),
                            MAX(c.card_type),
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
                            card_name           = EXCLUDED.card_name,
                            card_type           = EXCLUDED.card_type,
                            gross_sales         = EXCLUDED.gross_sales,
                            net_sales           = EXCLUDED.net_sales,
                            invoice_count       = EXCLUDED.invoice_count,
                            avg_ticket          = EXCLUDED.avg_ticket,
                            first_invoice_date  = EXCLUDED.first_invoice_date,
                            last_invoice_date   = EXCLUDED.last_invoice_date,
                            is_active           = EXCLUDED.is_active,
                            transformed_at_utc  = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_invoice not available, skipping customer_dashboard';
                    END IF;

                    -- Item dashboard supplement (uses INV1 lines if available)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='invoice_line') THEN
                        INSERT INTO mart.sales_item_dashboard (
                            company_id, item_code, item_name, item_group_code,
                            quantity_sold, gross_sales,
                            invoice_count, line_count,
                            first_sale_date, last_sale_date, transformed_at_utc
                        )
                        SELECT
                            l.company_id,
                            l.item_code,
                            MAX(m.item_name),
                            MAX(m.items_group_code),
                            SUM(COALESCE(l.quantity, 0)),
                            SUM(COALESCE(l.line_total, 0)),
                            COUNT(DISTINCT l.doc_entry),
                            COUNT(*),
                            MIN(i.doc_date),
                            MAX(i.doc_date),
                            NOW()
                        FROM stg.invoice_line l
                        LEFT JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                        LEFT JOIN stg.item m ON m.company_id = l.company_id AND m.item_code = l.item_code
                        WHERE l.company_id = p_company_id
                        GROUP BY l.company_id, l.item_code
                        ON CONFLICT (company_id, item_code) DO UPDATE SET
                            item_name           = EXCLUDED.item_name,
                            item_group_code     = EXCLUDED.item_group_code,
                            quantity_sold       = EXCLUDED.quantity_sold,
                            gross_sales         = EXCLUDED.gross_sales,
                            invoice_count       = EXCLUDED.invoice_count,
                            line_count          = EXCLUDED.line_count,
                            first_sale_date     = EXCLUDED.first_sale_date,
                            last_sale_date      = EXCLUDED.last_sale_date,
                            transformed_at_utc  = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.invoice_line not available, skipping item_dashboard';
                    END IF;

                    -- Fulfillment dashboard — requires ORDR/ODLN (skipped until activated)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='sales_order')
                    AND EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='delivery') THEN
                        RAISE NOTICE 'refresh_sales_process: ORDR/ODLN available — fulfillment refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_order/delivery not available, skipping fulfillment';
                    END IF;
                END;
                $$;
                """);

            // mart.refresh_purchasing_process — fully defensive (OPOR/OPDN not yet extracted)
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_purchasing_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_order') THEN
                        -- Purchase executive daily
                        INSERT INTO mart.purchase_executive_daily (
                            company_id, purchase_date, po_count, po_amount, active_suppliers, transformed_at_utc
                        )
                        SELECT
                            company_id, doc_date,
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN card_code END),
                            NOW()
                        FROM stg.purchase_order
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, doc_date
                        ON CONFLICT (company_id, purchase_date) DO UPDATE SET
                            po_count            = EXCLUDED.po_count,
                            po_amount           = EXCLUDED.po_amount,
                            active_suppliers    = EXCLUDED.active_suppliers,
                            transformed_at_utc  = NOW();

                        -- Purchase supplier dashboard
                        INSERT INTO mart.purchase_supplier_dashboard (
                            company_id, supplier_code, supplier_name,
                            po_count, po_amount, last_po_date, avg_po_amount, transformed_at_utc
                        )
                        SELECT
                            company_id, card_code, MAX(card_name),
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            MAX(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END),
                            CASE WHEN COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) > 0
                                 THEN SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END)
                                      / COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END)
                                 ELSE 0 END,
                            NOW()
                        FROM stg.purchase_order
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, supplier_code) DO UPDATE SET
                            supplier_name   = EXCLUDED.supplier_name,
                            po_count        = EXCLUDED.po_count,
                            po_amount       = EXCLUDED.po_amount,
                            last_po_date    = EXCLUDED.last_po_date,
                            avg_po_amount   = EXCLUDED.avg_po_amount,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_order not available, skipping';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_delivery') THEN
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
                        FROM stg.purchase_delivery
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, supplier_code) DO UPDATE SET
                            supplier_name   = EXCLUDED.supplier_name,
                            gr_count        = EXCLUDED.gr_count,
                            gr_amount       = EXCLUDED.gr_amount,
                            last_gr_date    = EXCLUDED.last_gr_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_delivery not available, skipping';
                    END IF;
                END;
                $$;
                """);

            // mart.refresh_inventory_process — rotation functional, stock defensive
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_inventory_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    -- Rotation: uses OITM (available) + INV1 lines for sold qty (if available)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='item') THEN
                        INSERT INTO mart.inventory_rotation_dashboard (
                            company_id, item_code, item_name, item_group_code,
                            qty_sold_30d, qty_sold_90d,
                            last_sale_date, avg_daily_sales_qty,
                            rotation_status, transformed_at_utc
                        )
                        SELECT
                            m.company_id, m.item_code, m.item_name, m.items_group_code::TEXT,
                            COALESCE(s30.qty, 0),
                            COALESCE(s90.qty, 0),
                            s90.last_date,
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
                            FROM stg.invoice_line l
                            JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                            WHERE l.company_id = m.company_id AND l.item_code = m.item_code
                              AND COALESCE(i.cancelled,'N') != 'Y'
                              AND i.doc_date >= CURRENT_DATE - 30
                        ) s30 ON TRUE
                        LEFT JOIN LATERAL (
                            SELECT SUM(COALESCE(l.quantity,0)) AS qty, MAX(i.doc_date) AS last_date
                            FROM stg.invoice_line l
                            JOIN stg.sales_invoice i ON i.company_id = l.company_id AND i.doc_entry = l.doc_entry
                            WHERE l.company_id = m.company_id AND l.item_code = m.item_code
                              AND COALESCE(i.cancelled,'N') != 'Y'
                              AND i.doc_date >= CURRENT_DATE - 90
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

                    -- Stock: requires OITW (not yet extracted)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='item_warehouse') THEN
                        RAISE NOTICE 'refresh_inventory_process: OITW available — stock refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item_warehouse not available, skipping stock';
                    END IF;

                    -- Warehouse: requires OWTR
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='stock_transfer') THEN
                        RAISE NOTICE 'refresh_inventory_process: OWTR available — warehouse refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.stock_transfer not available, skipping warehouse';
                    END IF;
                END;
                $$;
                """);

            // mart.refresh_finance_process — AR functional, AP defensive
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_finance_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    -- AR aging (functional with OINV — balance approximated as doc_total)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='sales_invoice') THEN
                        INSERT INTO mart.finance_ar_aging_dashboard (
                            company_id, card_code, card_name,
                            invoice_count, total_amount, balance_due,
                            overdue_amount,
                            aging_0_30, aging_31_60, aging_61_90, aging_90_plus,
                            last_invoice_date, oldest_overdue_date, transformed_at_utc
                        )
                        SELECT
                            company_id, card_code, MAX(card_name),
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date < CURRENT_DATE
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date >= CURRENT_DATE - 30
                                          AND doc_due_date < CURRENT_DATE
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date >= CURRENT_DATE - 60
                                          AND doc_due_date < CURRENT_DATE - 30
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date >= CURRENT_DATE - 90
                                          AND doc_due_date < CURRENT_DATE - 60
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date < CURRENT_DATE - 90
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            MAX(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END),
                            MIN(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date < CURRENT_DATE
                                     THEN doc_due_date END),
                            NOW()
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, card_code) DO UPDATE SET
                            card_name           = EXCLUDED.card_name,
                            invoice_count       = EXCLUDED.invoice_count,
                            total_amount        = EXCLUDED.total_amount,
                            balance_due         = EXCLUDED.balance_due,
                            overdue_amount      = EXCLUDED.overdue_amount,
                            aging_0_30          = EXCLUDED.aging_0_30,
                            aging_31_60         = EXCLUDED.aging_31_60,
                            aging_61_90         = EXCLUDED.aging_61_90,
                            aging_90_plus       = EXCLUDED.aging_90_plus,
                            last_invoice_date   = EXCLUDED.last_invoice_date,
                            oldest_overdue_date = EXCLUDED.oldest_overdue_date,
                            transformed_at_utc  = NOW();

                        -- Finance executive daily (AR side)
                        INSERT INTO mart.finance_executive_daily (
                            company_id, period_date,
                            ar_total, ar_overdue, ar_overdue_pct,
                            new_invoices_count, new_invoices_amount, transformed_at_utc
                        )
                        SELECT
                            company_id,
                            CURRENT_DATE,
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_due_date < CURRENT_DATE
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            CASE WHEN SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END) > 0
                                 THEN ROUND(
                                     SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' AND doc_due_date < CURRENT_DATE
                                              THEN COALESCE(doc_total,0) ELSE 0 END)
                                     / SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END)
                                     , 4)
                                 ELSE 0 END,
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                            AND doc_date = CURRENT_DATE THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                          AND doc_date = CURRENT_DATE
                                     THEN COALESCE(doc_total,0) ELSE 0 END),
                            NOW()
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id
                        GROUP BY company_id
                        ON CONFLICT (company_id, period_date) DO UPDATE SET
                            ar_total                = EXCLUDED.ar_total,
                            ar_overdue              = EXCLUDED.ar_overdue,
                            ar_overdue_pct          = EXCLUDED.ar_overdue_pct,
                            new_invoices_count      = EXCLUDED.new_invoices_count,
                            new_invoices_amount     = EXCLUDED.new_invoices_amount,
                            transformed_at_utc      = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_finance_process: stg.sales_invoice not available, skipping AR';
                    END IF;

                    -- AP aging — requires OPCH (not yet extracted)
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_invoice') THEN
                        RAISE NOTICE 'refresh_finance_process: OPCH available — AP aging refresh TBD';
                    ELSE
                        RAISE NOTICE 'refresh_finance_process: stg.purchase_invoice not available, skipping AP';
                    END IF;
                END;
                $$;
                """);

            // mart.refresh_all_processes — orchestrator for all 4 process groups
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_all_processes(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM mart.refresh_sales_process(p_company_id);
                    PERFORM mart.refresh_purchasing_process(p_company_id);
                    PERFORM mart.refresh_inventory_process(p_company_id);
                    PERFORM mart.refresh_finance_process(p_company_id);
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_all_processes(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_finance_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_inventory_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_purchasing_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_process(TEXT);");

            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.finance_executive_daily;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.finance_ap_aging_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.finance_ar_aging_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.inventory_warehouse_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.inventory_rotation_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.inventory_stock_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.purchase_receiving_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.purchase_supplier_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.purchase_executive_daily;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_fulfillment_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_item_dashboard;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_customer_dashboard;");
        }
    }
}
