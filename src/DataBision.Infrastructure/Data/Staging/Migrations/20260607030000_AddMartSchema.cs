using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class AddMartSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS mart;");

            // ── mart.sales_daily ───────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_daily (
                    company_id              TEXT            NOT NULL,
                    sales_date              DATE            NOT NULL,
                    gross_sales_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memo_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    credit_memo_count       INTEGER         NOT NULL DEFAULT 0,
                    active_customers        INTEGER         NOT NULL DEFAULT 0,
                    avg_ticket_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, sales_date)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_sales_daily_company_date
                    ON mart.sales_daily (company_id, sales_date DESC);
                """);

            // ── mart.sales_monthly ─────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_monthly (
                    company_id              TEXT            NOT NULL,
                    sales_month             DATE            NOT NULL,
                    gross_sales_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memo_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    credit_memo_count       INTEGER         NOT NULL DEFAULT 0,
                    active_customers        INTEGER         NOT NULL DEFAULT 0,
                    avg_ticket_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, sales_month)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_sales_monthly_company_month
                    ON mart.sales_monthly (company_id, sales_month DESC);
                """);

            // ── mart.customer_sales ────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.customer_sales (
                    company_id              TEXT            NOT NULL,
                    card_code               TEXT            NOT NULL,
                    card_name               TEXT,
                    sales_amount            NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memo_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    credit_memo_count       INTEGER         NOT NULL DEFAULT 0,
                    last_invoice_date       DATE,
                    first_invoice_date      DATE,
                    avg_ticket_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, card_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_customer_sales_net
                    ON mart.customer_sales (company_id, net_sales_amount DESC);
                """);

            // ── mart.item_sales ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.item_sales (
                    company_id              TEXT            NOT NULL,
                    item_code               TEXT            NOT NULL,
                    item_name               TEXT,
                    quantity_sold           NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    gross_sales_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    line_count              INTEGER         NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    last_sale_date          DATE,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, item_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_item_sales_gross
                    ON mart.item_sales (company_id, gross_sales_amount DESC);
                """);

            // ── mart.salesperson_sales ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.salesperson_sales (
                    company_id              TEXT            NOT NULL,
                    sales_person_code       TEXT            NOT NULL,
                    sales_person_name       TEXT,
                    sales_amount            NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memo_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    credit_memo_count       INTEGER         NOT NULL DEFAULT 0,
                    active_customers        INTEGER         NOT NULL DEFAULT 0,
                    avg_ticket_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, sales_person_code)
                );
                CREATE INDEX IF NOT EXISTS idx_mart_slp_sales_net
                    ON mart.salesperson_sales (company_id, net_sales_amount DESC);
                """);

            // ── mart.sales_kpi_summary ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.sales_kpi_summary (
                    company_id              TEXT            NOT NULL,
                    gross_sales_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    credit_memo_amount      NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    net_sales_amount        NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    invoice_count           INTEGER         NOT NULL DEFAULT 0,
                    credit_memo_count       INTEGER         NOT NULL DEFAULT 0,
                    active_customers        INTEGER         NOT NULL DEFAULT 0,
                    active_items            INTEGER         NOT NULL DEFAULT 0,
                    avg_ticket_amount       NUMERIC(19,6)   NOT NULL DEFAULT 0,
                    last_invoice_date       DATE,
                    last_credit_memo_date   DATE,
                    last_sync_at_utc        TIMESTAMPTZ,
                    transformed_at_utc      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id)
                );
                """);

            // ── FASE 3: MART transformation functions ──────────────────────────────

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_sales_daily(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.sales_daily (
                        company_id, sales_date,
                        gross_sales_amount, credit_memo_amount, net_sales_amount,
                        invoice_count, credit_memo_count, active_customers, avg_ticket_amount,
                        transformed_at_utc
                    )
                    SELECT
                        COALESCE(i.company_id, c.company_id),
                        COALESCE(i.sales_date, c.sales_date),
                        COALESCE(i.gross, 0),
                        COALESCE(c.cm, 0),
                        COALESCE(i.gross, 0) - COALESCE(c.cm, 0),
                        COALESCE(i.inv_cnt, 0),
                        COALESCE(c.cm_cnt, 0),
                        COALESCE(i.customers, 0),
                        CASE WHEN COALESCE(i.inv_cnt, 0) > 0
                             THEN COALESCE(i.gross, 0) / i.inv_cnt ELSE 0 END,
                        NOW()
                    FROM (
                        SELECT company_id, doc_date AS sales_date,
                               SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                        THEN COALESCE(doc_total, 0) ELSE 0 END) AS gross,
                               COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS inv_cnt,
                               COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                                   THEN card_code END) AS customers
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, doc_date
                    ) i
                    FULL OUTER JOIN (
                        SELECT company_id, doc_date AS sales_date,
                               SUM(COALESCE(doc_total, 0)) AS cm,
                               COUNT(*) AS cm_cnt
                        FROM stg.credit_memo
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, doc_date
                    ) c ON c.company_id = i.company_id AND c.sales_date = i.sales_date
                    ON CONFLICT (company_id, sales_date) DO UPDATE SET
                        gross_sales_amount  = EXCLUDED.gross_sales_amount,
                        credit_memo_amount  = EXCLUDED.credit_memo_amount,
                        net_sales_amount    = EXCLUDED.net_sales_amount,
                        invoice_count       = EXCLUDED.invoice_count,
                        credit_memo_count   = EXCLUDED.credit_memo_count,
                        active_customers    = EXCLUDED.active_customers,
                        avg_ticket_amount   = EXCLUDED.avg_ticket_amount,
                        transformed_at_utc  = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_sales_monthly(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.sales_monthly (
                        company_id, sales_month,
                        gross_sales_amount, credit_memo_amount, net_sales_amount,
                        invoice_count, credit_memo_count, active_customers, avg_ticket_amount,
                        transformed_at_utc
                    )
                    SELECT
                        COALESCE(i.company_id, c.company_id),
                        COALESCE(i.sales_month, c.sales_month),
                        COALESCE(i.gross, 0),
                        COALESCE(c.cm, 0),
                        COALESCE(i.gross, 0) - COALESCE(c.cm, 0),
                        COALESCE(i.inv_cnt, 0),
                        COALESCE(c.cm_cnt, 0),
                        COALESCE(i.customers, 0),
                        CASE WHEN COALESCE(i.inv_cnt, 0) > 0
                             THEN COALESCE(i.gross, 0) / i.inv_cnt ELSE 0 END,
                        NOW()
                    FROM (
                        SELECT company_id,
                               DATE_TRUNC('month', doc_date)::DATE AS sales_month,
                               SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                        THEN COALESCE(doc_total, 0) ELSE 0 END) AS gross,
                               COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS inv_cnt,
                               COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                                   THEN card_code END) AS customers
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, DATE_TRUNC('month', doc_date)::DATE
                    ) i
                    FULL OUTER JOIN (
                        SELECT company_id,
                               DATE_TRUNC('month', doc_date)::DATE AS sales_month,
                               SUM(COALESCE(doc_total, 0)) AS cm,
                               COUNT(*) AS cm_cnt
                        FROM stg.credit_memo
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, DATE_TRUNC('month', doc_date)::DATE
                    ) c ON c.company_id = i.company_id AND c.sales_month = i.sales_month
                    ON CONFLICT (company_id, sales_month) DO UPDATE SET
                        gross_sales_amount  = EXCLUDED.gross_sales_amount,
                        credit_memo_amount  = EXCLUDED.credit_memo_amount,
                        net_sales_amount    = EXCLUDED.net_sales_amount,
                        invoice_count       = EXCLUDED.invoice_count,
                        credit_memo_count   = EXCLUDED.credit_memo_count,
                        active_customers    = EXCLUDED.active_customers,
                        avg_ticket_amount   = EXCLUDED.avg_ticket_amount,
                        transformed_at_utc  = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_customer_sales(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.customer_sales (
                        company_id, card_code, card_name,
                        sales_amount, credit_memo_amount, net_sales_amount,
                        invoice_count, credit_memo_count,
                        last_invoice_date, first_invoice_date, avg_ticket_amount,
                        transformed_at_utc
                    )
                    SELECT
                        COALESCE(i.company_id, c.company_id),
                        COALESCE(i.card_code, c.card_code),
                        COALESCE(i.card_name, c.card_name),
                        COALESCE(i.sales, 0),
                        COALESCE(c.cm, 0),
                        COALESCE(i.sales, 0) - COALESCE(c.cm, 0),
                        COALESCE(i.inv_cnt, 0),
                        COALESCE(c.cm_cnt, 0),
                        i.last_date,
                        i.first_date,
                        CASE WHEN COALESCE(i.inv_cnt, 0) > 0
                             THEN COALESCE(i.sales, 0) / i.inv_cnt ELSE 0 END,
                        NOW()
                    FROM (
                        SELECT company_id, card_code,
                               MAX(card_name) AS card_name,
                               SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                        THEN COALESCE(doc_total, 0) ELSE 0 END) AS sales,
                               COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS inv_cnt,
                               MAX(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END) AS last_date,
                               MIN(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END) AS first_date
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id AND card_code IS NOT NULL
                        GROUP BY company_id, card_code
                    ) i
                    FULL OUTER JOIN (
                        SELECT company_id, card_code,
                               MAX(card_name) AS card_name,
                               SUM(COALESCE(doc_total, 0)) AS cm,
                               COUNT(*) AS cm_cnt
                        FROM stg.credit_memo
                        WHERE company_id = p_company_id AND card_code IS NOT NULL
                        GROUP BY company_id, card_code
                    ) c ON c.company_id = i.company_id AND c.card_code = i.card_code
                    ON CONFLICT (company_id, card_code) DO UPDATE SET
                        card_name           = EXCLUDED.card_name,
                        sales_amount        = EXCLUDED.sales_amount,
                        credit_memo_amount  = EXCLUDED.credit_memo_amount,
                        net_sales_amount    = EXCLUDED.net_sales_amount,
                        invoice_count       = EXCLUDED.invoice_count,
                        credit_memo_count   = EXCLUDED.credit_memo_count,
                        last_invoice_date   = EXCLUDED.last_invoice_date,
                        first_invoice_date  = EXCLUDED.first_invoice_date,
                        avg_ticket_amount   = EXCLUDED.avg_ticket_amount,
                        transformed_at_utc  = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_item_sales(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.item_sales (
                        company_id, item_code, item_name,
                        quantity_sold, gross_sales_amount, line_count, invoice_count,
                        last_sale_date, transformed_at_utc
                    )
                    SELECT
                        sil.company_id,
                        sil.item_code,
                        MAX(it.item_name) AS item_name,
                        SUM(COALESCE(sil.quantity, 0)) AS quantity_sold,
                        SUM(COALESCE(sil.line_total, 0)) AS gross_sales_amount,
                        COUNT(*) AS line_count,
                        COUNT(DISTINCT sil.doc_entry) AS invoice_count,
                        MAX(si.doc_date) AS last_sale_date,
                        NOW()
                    FROM stg.sales_invoice_line sil
                    LEFT JOIN stg.sales_invoice si
                           ON si.company_id = sil.company_id AND si.doc_entry = sil.doc_entry
                          AND COALESCE(si.cancelled, 'N') != 'Y'
                    LEFT JOIN stg.item it
                           ON it.company_id = sil.company_id AND it.item_code = sil.item_code
                    WHERE sil.company_id = p_company_id AND sil.item_code IS NOT NULL
                    GROUP BY sil.company_id, sil.item_code
                    ON CONFLICT (company_id, item_code) DO UPDATE SET
                        item_name           = EXCLUDED.item_name,
                        quantity_sold       = EXCLUDED.quantity_sold,
                        gross_sales_amount  = EXCLUDED.gross_sales_amount,
                        line_count          = EXCLUDED.line_count,
                        invoice_count       = EXCLUDED.invoice_count,
                        last_sale_date      = EXCLUDED.last_sale_date,
                        transformed_at_utc  = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_salesperson_sales(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.salesperson_sales (
                        company_id, sales_person_code, sales_person_name,
                        sales_amount, credit_memo_amount, net_sales_amount,
                        invoice_count, credit_memo_count, active_customers, avg_ticket_amount,
                        transformed_at_utc
                    )
                    SELECT
                        COALESCE(i.company_id, c.company_id),
                        COALESCE(i.spcode, c.spcode),
                        sp.slp_name,
                        COALESCE(i.sales, 0),
                        COALESCE(c.cm, 0),
                        COALESCE(i.sales, 0) - COALESCE(c.cm, 0),
                        COALESCE(i.inv_cnt, 0),
                        COALESCE(c.cm_cnt, 0),
                        COALESCE(i.customers, 0),
                        CASE WHEN COALESCE(i.inv_cnt, 0) > 0
                             THEN COALESCE(i.sales, 0) / i.inv_cnt ELSE 0 END,
                        NOW()
                    FROM (
                        SELECT company_id, sales_person_code AS spcode,
                               SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                        THEN COALESCE(doc_total, 0) ELSE 0 END) AS sales,
                               COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS inv_cnt,
                               COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                                   THEN card_code END) AS customers
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id
                          AND NULLIF(sales_person_code, '') IS NOT NULL
                        GROUP BY company_id, sales_person_code
                    ) i
                    FULL OUTER JOIN (
                        SELECT company_id, sales_person_code AS spcode,
                               SUM(COALESCE(doc_total, 0)) AS cm,
                               COUNT(*) AS cm_cnt
                        FROM stg.credit_memo
                        WHERE company_id = p_company_id
                          AND NULLIF(sales_person_code, '') IS NOT NULL
                        GROUP BY company_id, sales_person_code
                    ) c ON c.company_id = i.company_id AND c.spcode = i.spcode
                    LEFT JOIN stg.salesperson sp
                           ON sp.company_id = COALESCE(i.company_id, c.company_id)
                          AND sp.slp_code::TEXT = COALESCE(i.spcode, c.spcode)
                    ON CONFLICT (company_id, sales_person_code) DO UPDATE SET
                        sales_person_name   = EXCLUDED.sales_person_name,
                        sales_amount        = EXCLUDED.sales_amount,
                        credit_memo_amount  = EXCLUDED.credit_memo_amount,
                        net_sales_amount    = EXCLUDED.net_sales_amount,
                        invoice_count       = EXCLUDED.invoice_count,
                        credit_memo_count   = EXCLUDED.credit_memo_count,
                        active_customers    = EXCLUDED.active_customers,
                        avg_ticket_amount   = EXCLUDED.avg_ticket_amount,
                        transformed_at_utc  = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_sales_kpi_summary(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO mart.sales_kpi_summary (
                        company_id,
                        gross_sales_amount, credit_memo_amount, net_sales_amount,
                        invoice_count, credit_memo_count,
                        active_customers, active_items, avg_ticket_amount,
                        last_invoice_date, last_credit_memo_date,
                        last_sync_at_utc, transformed_at_utc
                    )
                    SELECT
                        p_company_id,
                        COALESCE(gross, 0),
                        COALESCE(cm, 0),
                        COALESCE(gross, 0) - COALESCE(cm, 0),
                        COALESCE(inv_cnt, 0),
                        COALESCE(cm_cnt, 0),
                        COALESCE(customers, 0),
                        COALESCE(items, 0),
                        CASE WHEN COALESCE(inv_cnt, 0) > 0
                             THEN COALESCE(gross, 0) / inv_cnt ELSE 0 END,
                        last_inv,
                        last_cm,
                        NULL,
                        NOW()
                    FROM (
                        SELECT
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                     THEN COALESCE(doc_total, 0) ELSE 0 END) AS gross,
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END) AS inv_cnt,
                            COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y'
                                               THEN card_code END) AS customers,
                            MAX(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN doc_date END) AS last_inv
                        FROM stg.sales_invoice
                        WHERE company_id = p_company_id
                    ) inv_agg
                    CROSS JOIN (
                        SELECT
                            SUM(COALESCE(doc_total, 0)) AS cm,
                            COUNT(*) AS cm_cnt,
                            MAX(doc_date) AS last_cm
                        FROM stg.credit_memo
                        WHERE company_id = p_company_id
                    ) cm_agg
                    CROSS JOIN (
                        SELECT COUNT(DISTINCT item_code) AS items
                        FROM stg.sales_invoice_line
                        WHERE company_id = p_company_id AND item_code IS NOT NULL
                    ) item_agg
                    ON CONFLICT (company_id) DO UPDATE SET
                        gross_sales_amount      = EXCLUDED.gross_sales_amount,
                        credit_memo_amount      = EXCLUDED.credit_memo_amount,
                        net_sales_amount        = EXCLUDED.net_sales_amount,
                        invoice_count           = EXCLUDED.invoice_count,
                        credit_memo_count       = EXCLUDED.credit_memo_count,
                        active_customers        = EXCLUDED.active_customers,
                        active_items            = EXCLUDED.active_items,
                        avg_ticket_amount       = EXCLUDED.avg_ticket_amount,
                        last_invoice_date       = EXCLUDED.last_invoice_date,
                        last_credit_memo_date   = EXCLUDED.last_credit_memo_date,
                        transformed_at_utc      = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            // ── mart.refresh_all — orchestrator ────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_all(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                BEGIN
                    RETURN QUERY SELECT 'sales_daily'::TEXT,       mart.refresh_sales_daily(p_company_id);
                    RETURN QUERY SELECT 'sales_monthly'::TEXT,     mart.refresh_sales_monthly(p_company_id);
                    RETURN QUERY SELECT 'customer_sales'::TEXT,    mart.refresh_customer_sales(p_company_id);
                    RETURN QUERY SELECT 'item_sales'::TEXT,        mart.refresh_item_sales(p_company_id);
                    RETURN QUERY SELECT 'salesperson_sales'::TEXT, mart.refresh_salesperson_sales(p_company_id);
                    RETURN QUERY SELECT 'sales_kpi_summary'::TEXT, mart.refresh_sales_kpi_summary(p_company_id);
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_all(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_kpi_summary(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_salesperson_sales(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_item_sales(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_customer_sales(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_monthly(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_daily(TEXT);");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_kpi_summary;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.salesperson_sales;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.item_sales;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.customer_sales;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_monthly;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS mart.sales_daily;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS mart;");
        }
    }
}
