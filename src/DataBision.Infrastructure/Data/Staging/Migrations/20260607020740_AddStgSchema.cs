using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class AddStgSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── stg.salesperson ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.salesperson (
                    company_id          TEXT        NOT NULL,
                    slp_code            INTEGER     NOT NULL,
                    slp_name            TEXT,
                    is_active           BOOLEAN,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, slp_code)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_salesperson_company
                    ON stg.salesperson (company_id);
                """);

            // ── stg.customer ───────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.customer (
                    company_id          TEXT        NOT NULL,
                    card_code           TEXT        NOT NULL,
                    card_name           TEXT,
                    card_type           CHAR(1),
                    group_code          TEXT,
                    federal_tax_id      TEXT,
                    balance             NUMERIC(19,6),
                    sales_person_code   TEXT,
                    update_date         DATE,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, card_code)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_customer_company
                    ON stg.customer (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_customer_card_type
                    ON stg.customer (company_id, card_type);
                CREATE INDEX IF NOT EXISTS idx_stg_customer_slp
                    ON stg.customer (company_id, sales_person_code);
                """);

            // ── stg.item ───────────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.item (
                    company_id          TEXT        NOT NULL,
                    item_code           TEXT        NOT NULL,
                    item_name           TEXT,
                    item_group_code     INTEGER,
                    on_hand             NUMERIC(19,6),
                    is_inventory_item   BOOLEAN,
                    is_sales_item       BOOLEAN,
                    is_purchase_item    BOOLEAN,
                    update_date         DATE,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, item_code)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_item_company
                    ON stg.item (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_item_group
                    ON stg.item (company_id, item_group_code);
                """);

            // ── stg.sales_invoice ──────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.sales_invoice (
                    company_id          TEXT        NOT NULL,
                    doc_entry           INTEGER     NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    tax_date            DATE,
                    card_code           TEXT        NOT NULL,
                    card_name           TEXT,
                    doc_total           NUMERIC(19,6),
                    vat_sum             NUMERIC(19,6),
                    doc_currency        CHAR(3),
                    doc_status          CHAR(1),
                    cancelled           CHAR(1),
                    sales_person_code   TEXT,
                    update_date         DATE,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, doc_entry)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_sales_invoice_company
                    ON stg.sales_invoice (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_sales_invoice_doc_date
                    ON stg.sales_invoice (company_id, doc_date);
                CREATE INDEX IF NOT EXISTS idx_stg_sales_invoice_update_date
                    ON stg.sales_invoice (company_id, update_date);
                CREATE INDEX IF NOT EXISTS idx_stg_sales_invoice_card_code
                    ON stg.sales_invoice (company_id, card_code);
                CREATE INDEX IF NOT EXISTS idx_stg_sales_invoice_slp
                    ON stg.sales_invoice (company_id, sales_person_code);
                """);

            // ── stg.sales_invoice_line ─────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.sales_invoice_line (
                    company_id          TEXT        NOT NULL,
                    doc_entry           INTEGER     NOT NULL,
                    line_num            INTEGER     NOT NULL,
                    item_code           TEXT,
                    description         TEXT,
                    quantity            NUMERIC(19,6),
                    price               NUMERIC(19,6),
                    line_total          NUMERIC(19,6),
                    currency            CHAR(3),
                    warehouse_code      TEXT,
                    sales_person_code   TEXT,
                    discount_percent    NUMERIC(19,6),
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, doc_entry, line_num)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_sil_company
                    ON stg.sales_invoice_line (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_sil_doc_entry
                    ON stg.sales_invoice_line (company_id, doc_entry);
                CREATE INDEX IF NOT EXISTS idx_stg_sil_item_code
                    ON stg.sales_invoice_line (company_id, item_code);
                CREATE INDEX IF NOT EXISTS idx_stg_sil_warehouse
                    ON stg.sales_invoice_line (company_id, warehouse_code);
                """);

            // ── stg.credit_memo ────────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.credit_memo (
                    company_id          TEXT        NOT NULL,
                    doc_entry           INTEGER     NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    tax_date            DATE,
                    card_code           TEXT        NOT NULL,
                    card_name           TEXT,
                    doc_total           NUMERIC(19,6),
                    vat_sum             NUMERIC(19,6),
                    doc_currency        CHAR(3),
                    doc_status          CHAR(1),
                    cancelled           CHAR(1),
                    sales_person_code   TEXT,
                    update_date         DATE,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, doc_entry)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_credit_memo_company
                    ON stg.credit_memo (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_credit_memo_doc_date
                    ON stg.credit_memo (company_id, doc_date);
                CREATE INDEX IF NOT EXISTS idx_stg_credit_memo_update_date
                    ON stg.credit_memo (company_id, update_date);
                CREATE INDEX IF NOT EXISTS idx_stg_credit_memo_card_code
                    ON stg.credit_memo (company_id, card_code);
                CREATE INDEX IF NOT EXISTS idx_stg_credit_memo_slp
                    ON stg.credit_memo (company_id, sales_person_code);
                """);

            // ── stg.credit_memo_line ───────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.credit_memo_line (
                    company_id          TEXT        NOT NULL,
                    doc_entry           INTEGER     NOT NULL,
                    line_num            INTEGER     NOT NULL,
                    item_code           TEXT,
                    description         TEXT,
                    quantity            NUMERIC(19,6),
                    price               NUMERIC(19,6),
                    line_total          NUMERIC(19,6),
                    currency            CHAR(3),
                    warehouse_code      TEXT,
                    sales_person_code   TEXT,
                    discount_percent    NUMERIC(19,6),
                    base_entry          INTEGER,
                    base_line           INTEGER,
                    base_type           INTEGER,
                    source_hash_hex     CHAR(64)    NOT NULL,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (company_id, doc_entry, line_num)
                );
                CREATE INDEX IF NOT EXISTS idx_stg_cml_company
                    ON stg.credit_memo_line (company_id);
                CREATE INDEX IF NOT EXISTS idx_stg_cml_doc_entry
                    ON stg.credit_memo_line (company_id, doc_entry);
                CREATE INDEX IF NOT EXISTS idx_stg_cml_item_code
                    ON stg.credit_memo_line (company_id, item_code);
                CREATE INDEX IF NOT EXISTS idx_stg_cml_warehouse
                    ON stg.credit_memo_line (company_id, warehouse_code);
                """);

            // ── FASE 3: Transformation functions ──────────────────────────────────

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_salesperson(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.salesperson (
                        company_id,
                        slp_code, slp_name,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."SlpCode",
                        r."SlpName",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_oslp r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, slp_code) DO UPDATE SET
                        slp_name           = EXCLUDED.slp_name,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.salesperson.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_customer(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.customer (
                        company_id,
                        card_code, card_name, card_type,
                        group_code, federal_tax_id, balance,
                        sales_person_code, update_date,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."CardCode",
                        r."CardName",
                        r."CardType",
                        r."GroupCode",
                        r."LicTradNum",
                        r."Balance",
                        r."SlpCode",
                        r."UpdateDate",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_ocrd r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, card_code) DO UPDATE SET
                        card_name          = EXCLUDED.card_name,
                        card_type          = EXCLUDED.card_type,
                        group_code         = EXCLUDED.group_code,
                        federal_tax_id     = EXCLUDED.federal_tax_id,
                        balance            = EXCLUDED.balance,
                        sales_person_code  = EXCLUDED.sales_person_code,
                        update_date        = EXCLUDED.update_date,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.customer.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_item(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.item (
                        company_id,
                        item_code, item_name, item_group_code,
                        on_hand, update_date,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."ItemCode",
                        r."ItemName",
                        r."ItmsGrpCod",
                        r."OnHand",
                        r."UpdateDate",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_oitm r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, item_code) DO UPDATE SET
                        item_name          = EXCLUDED.item_name,
                        item_group_code    = EXCLUDED.item_group_code,
                        on_hand            = EXCLUDED.on_hand,
                        update_date        = EXCLUDED.update_date,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.item.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_sales_invoice(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.sales_invoice (
                        company_id,
                        doc_entry, doc_num, doc_date, doc_due_date, tax_date,
                        card_code, card_name, doc_total, vat_sum,
                        doc_currency, doc_status, cancelled,
                        sales_person_code, update_date,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."DocEntry",
                        r."DocNum",
                        r."DocDate",
                        r."DocDueDate",
                        r."TaxDate",
                        r."CardCode",
                        r."CardName",
                        r."DocTotal",
                        r."VatSum",
                        r."DocCur",
                        r."DocStatus",
                        r."Cancelled",
                        r."SlpCode",
                        r."UpdateDate",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_oinv r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num            = EXCLUDED.doc_num,
                        doc_date           = EXCLUDED.doc_date,
                        doc_due_date       = EXCLUDED.doc_due_date,
                        tax_date           = EXCLUDED.tax_date,
                        card_code          = EXCLUDED.card_code,
                        card_name          = EXCLUDED.card_name,
                        doc_total          = EXCLUDED.doc_total,
                        vat_sum            = EXCLUDED.vat_sum,
                        doc_currency       = EXCLUDED.doc_currency,
                        doc_status         = EXCLUDED.doc_status,
                        cancelled          = EXCLUDED.cancelled,
                        sales_person_code  = EXCLUDED.sales_person_code,
                        update_date        = EXCLUDED.update_date,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.sales_invoice.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_sales_invoice_line(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.sales_invoice_line (
                        company_id,
                        doc_entry, line_num,
                        item_code, description, quantity, price, line_total,
                        currency,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."DocEntry",
                        r."LineNum",
                        r."ItemCode",
                        r."Dscription",
                        r."Quantity",
                        r."Price",
                        r."LineTotal",
                        r."Currency",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_inv1 r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry, line_num) DO UPDATE SET
                        item_code          = EXCLUDED.item_code,
                        description        = EXCLUDED.description,
                        quantity           = EXCLUDED.quantity,
                        price              = EXCLUDED.price,
                        line_total         = EXCLUDED.line_total,
                        currency           = EXCLUDED.currency,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.sales_invoice_line.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_credit_memo(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.credit_memo (
                        company_id,
                        doc_entry, doc_num, doc_date, doc_due_date, tax_date,
                        card_code, card_name, doc_total, vat_sum,
                        doc_currency, doc_status, cancelled,
                        sales_person_code, update_date,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."DocEntry",
                        r."DocNum",
                        r."DocDate",
                        r."DocDueDate",
                        r."TaxDate",
                        r."CardCode",
                        r."CardName",
                        r."DocTotal",
                        r."VatSum",
                        r."DocCur",
                        r."DocStatus",
                        r."Cancelled",
                        r."SlpCode",
                        r."UpdateDate",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_orin r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num            = EXCLUDED.doc_num,
                        doc_date           = EXCLUDED.doc_date,
                        doc_due_date       = EXCLUDED.doc_due_date,
                        tax_date           = EXCLUDED.tax_date,
                        card_code          = EXCLUDED.card_code,
                        card_name          = EXCLUDED.card_name,
                        doc_total          = EXCLUDED.doc_total,
                        vat_sum            = EXCLUDED.vat_sum,
                        doc_currency       = EXCLUDED.doc_currency,
                        doc_status         = EXCLUDED.doc_status,
                        cancelled          = EXCLUDED.cancelled,
                        sales_person_code  = EXCLUDED.sales_person_code,
                        update_date        = EXCLUDED.update_date,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.credit_memo.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_credit_memo_line(p_company_id TEXT)
                RETURNS INT LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    INSERT INTO stg.credit_memo_line (
                        company_id,
                        doc_entry, line_num,
                        item_code, description, quantity, price, line_total,
                        currency,
                        source_hash_hex, extracted_at_utc, transformed_at_utc
                    )
                    SELECT
                        r.company_id,
                        r."DocEntry",
                        r."LineNum",
                        r."ItemCode",
                        r."Dscription",
                        r."Quantity",
                        r."Price",
                        r."LineTotal",
                        r."Currency",
                        r.source_hash_hex,
                        r.extracted_at_utc,
                        NOW()
                    FROM raw.sap_rin1 r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry, line_num) DO UPDATE SET
                        item_code          = EXCLUDED.item_code,
                        description        = EXCLUDED.description,
                        quantity           = EXCLUDED.quantity,
                        price              = EXCLUDED.price,
                        line_total         = EXCLUDED.line_total,
                        currency           = EXCLUDED.currency,
                        source_hash_hex    = EXCLUDED.source_hash_hex,
                        extracted_at_utc   = EXCLUDED.extracted_at_utc,
                        transformed_at_utc = NOW()
                    WHERE stg.credit_memo_line.source_hash_hex != EXCLUDED.source_hash_hex;
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN v_count;
                END;
                $$;
                """);

            // ── stg.refresh_all — orchestrator ─────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_all(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                BEGIN
                    RETURN QUERY SELECT 'salesperson'::TEXT,       stg.refresh_salesperson(p_company_id);
                    RETURN QUERY SELECT 'customer'::TEXT,          stg.refresh_customer(p_company_id);
                    RETURN QUERY SELECT 'item'::TEXT,              stg.refresh_item(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice'::TEXT,     stg.refresh_sales_invoice(p_company_id);
                    RETURN QUERY SELECT 'sales_invoice_line'::TEXT, stg.refresh_sales_invoice_line(p_company_id);
                    RETURN QUERY SELECT 'credit_memo'::TEXT,       stg.refresh_credit_memo(p_company_id);
                    RETURN QUERY SELECT 'credit_memo_line'::TEXT,  stg.refresh_credit_memo_line(p_company_id);
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_all(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_credit_memo_line(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_credit_memo(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_sales_invoice_line(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_sales_invoice(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_item(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_customer(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_salesperson(TEXT);");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.credit_memo_line;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.credit_memo;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.sales_invoice_line;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.sales_invoice;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.item;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.customer;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.salesperson;");
        }
    }
}
