using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint 8J: Adds raw + stg tables for purchasing, fulfillment, and inventory objects.
    /// RAW: sap_opor, sap_opdn, sap_opch, sap_oitw, sap_ordr, sap_odln, sap_owtr
    /// STG: purchase_order, purchase_receipt, purchase_invoice, item_warehouse, sales_order, delivery, stock_transfer
    /// Updates stg.refresh_all and mart process functions for new data.
    /// </summary>
    public partial class AddPurchaseFulfillmentSchemas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── RAW tables ─────────────────────────────────────────────────────────

            // Document header helper columns: all 5 purchasing/fulfillment headers share this shape
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_opor" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "DocDueDate"        DATE,
                    "CardCode"          VARCHAR(15),
                    "CardName"          VARCHAR(100),
                    "DocTotal"          NUMERIC(18,6),
                    "DocTotalSy"        NUMERIC(18,6),
                    "VatSum"            NUMERIC(18,6),
                    "DocCur"            VARCHAR(3),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "SlpCode"           VARCHAR(6),
                    "ObjType"           VARCHAR(20),
                    "DocType"           VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_opdn" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "DocDueDate"        DATE,
                    "CardCode"          VARCHAR(15),
                    "CardName"          VARCHAR(100),
                    "DocTotal"          NUMERIC(18,6),
                    "DocTotalSy"        NUMERIC(18,6),
                    "VatSum"            NUMERIC(18,6),
                    "DocCur"            VARCHAR(3),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "SlpCode"           VARCHAR(6),
                    "ObjType"           VARCHAR(20),
                    "DocType"           VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_opch" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "DocDueDate"        DATE,
                    "CardCode"          VARCHAR(15),
                    "CardName"          VARCHAR(100),
                    "DocTotal"          NUMERIC(18,6),
                    "DocTotalSy"        NUMERIC(18,6),
                    "VatSum"            NUMERIC(18,6),
                    "DocCur"            VARCHAR(3),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "SlpCode"           VARCHAR(6),
                    "ObjType"           VARCHAR(20),
                    "DocType"           VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_oitw" (
                    company_id          TEXT NOT NULL,
                    "ItemCode"          VARCHAR(50) NOT NULL,
                    "WhsCode"           VARCHAR(8) NOT NULL,
                    "OnHand"            NUMERIC(18,6),
                    "IsCommited"        NUMERIC(18,6),
                    "OnOrder"           NUMERIC(18,6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "ItemCode", "WhsCode")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_ordr" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "DocDueDate"        DATE,
                    "CardCode"          VARCHAR(15),
                    "CardName"          VARCHAR(100),
                    "DocTotal"          NUMERIC(18,6),
                    "DocTotalSy"        NUMERIC(18,6),
                    "VatSum"            NUMERIC(18,6),
                    "DocCur"            VARCHAR(3),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "SlpCode"           VARCHAR(6),
                    "ObjType"           VARCHAR(20),
                    "DocType"           VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_odln" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "DocDueDate"        DATE,
                    "CardCode"          VARCHAR(15),
                    "CardName"          VARCHAR(100),
                    "DocTotal"          NUMERIC(18,6),
                    "DocTotalSy"        NUMERIC(18,6),
                    "VatSum"            NUMERIC(18,6),
                    "DocCur"            VARCHAR(3),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "SlpCode"           VARCHAR(6),
                    "ObjType"           VARCHAR(20),
                    "DocType"           VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_owtr" (
                    company_id          TEXT NOT NULL,
                    "DocEntry"          INTEGER NOT NULL,
                    "DocNum"            INTEGER,
                    "DocDate"           DATE,
                    "FromWarehouse"     VARCHAR(8),
                    "ToWarehouse"       VARCHAR(8),
                    "DocTotal"          NUMERIC(18,6),
                    "DocStatus"         VARCHAR(1),
                    "Cancelled"         VARCHAR(1),
                    "Comments"          TEXT,
                    "CreateDate"        DATE,
                    "CreateTS"          VARCHAR(6),
                    "CreateTSNorm"      VARCHAR(6),
                    "UpdateDate"        DATE,
                    "UpdateTS"          VARCHAR(6),
                    "UpdateTSNorm"      VARCHAR(6),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(36),
                    batch_id            VARCHAR(36),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                """);

            // ── STG tables ─────────────────────────────────────────────────────────

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.purchase_order (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    card_code           VARCHAR(15),
                    card_name           VARCHAR(100),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    slp_code            VARCHAR(6),
                    vat_sum             NUMERIC(18,6),
                    doc_cur             VARCHAR(3),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.purchase_receipt (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    card_code           VARCHAR(15),
                    card_name           VARCHAR(100),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    slp_code            VARCHAR(6),
                    vat_sum             NUMERIC(18,6),
                    doc_cur             VARCHAR(3),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.purchase_invoice (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    card_code           VARCHAR(15),
                    card_name           VARCHAR(100),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    slp_code            VARCHAR(6),
                    vat_sum             NUMERIC(18,6),
                    doc_cur             VARCHAR(3),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.item_warehouse (
                    company_id          TEXT NOT NULL,
                    item_code           VARCHAR(50) NOT NULL,
                    whs_code            VARCHAR(8) NOT NULL,
                    on_hand             NUMERIC(18,6),
                    is_committed        NUMERIC(18,6),
                    on_order            NUMERIC(18,6),
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, item_code, whs_code)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.sales_order (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    card_code           VARCHAR(15),
                    card_name           VARCHAR(100),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    slp_code            VARCHAR(6),
                    vat_sum             NUMERIC(18,6),
                    doc_cur             VARCHAR(3),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.delivery (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    doc_due_date        DATE,
                    card_code           VARCHAR(15),
                    card_name           VARCHAR(100),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    slp_code            VARCHAR(6),
                    vat_sum             NUMERIC(18,6),
                    doc_cur             VARCHAR(3),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.stock_transfer (
                    company_id          TEXT NOT NULL,
                    doc_entry           INTEGER NOT NULL,
                    doc_num             INTEGER,
                    doc_date            DATE,
                    from_warehouse      VARCHAR(8),
                    to_warehouse        VARCHAR(8),
                    doc_total           NUMERIC(18,6),
                    doc_status          VARCHAR(1),
                    cancelled           VARCHAR(1),
                    create_date         DATE,
                    update_date         DATE,
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, doc_entry)
                );
                """);

            // ── STG refresh functions ──────────────────────────────────────────────

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_purchase_order(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_opor') THEN
                        RETURN QUERY SELECT 'purchase_order'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.purchase_order (
                        company_id, doc_entry, doc_num, doc_date, doc_due_date,
                        card_code, card_name, doc_total, doc_status, cancelled,
                        slp_code, vat_sum, doc_cur, create_date, update_date,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate", r."DocDueDate",
                           r."CardCode", r."CardName", r."DocTotal", r."DocStatus", r."Cancelled",
                           r."SlpCode", r."VatSum", r."DocCur", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_opor r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        doc_due_date = EXCLUDED.doc_due_date, card_code = EXCLUDED.card_code,
                        card_name = EXCLUDED.card_name, doc_total = EXCLUDED.doc_total,
                        doc_status = EXCLUDED.doc_status, cancelled = EXCLUDED.cancelled,
                        slp_code = EXCLUDED.slp_code, vat_sum = EXCLUDED.vat_sum,
                        doc_cur = EXCLUDED.doc_cur, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'purchase_order'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_purchase_receipt(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_opdn') THEN
                        RETURN QUERY SELECT 'purchase_receipt'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.purchase_receipt (
                        company_id, doc_entry, doc_num, doc_date, doc_due_date,
                        card_code, card_name, doc_total, doc_status, cancelled,
                        slp_code, vat_sum, doc_cur, create_date, update_date,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate", r."DocDueDate",
                           r."CardCode", r."CardName", r."DocTotal", r."DocStatus", r."Cancelled",
                           r."SlpCode", r."VatSum", r."DocCur", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_opdn r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        doc_due_date = EXCLUDED.doc_due_date, card_code = EXCLUDED.card_code,
                        card_name = EXCLUDED.card_name, doc_total = EXCLUDED.doc_total,
                        doc_status = EXCLUDED.doc_status, cancelled = EXCLUDED.cancelled,
                        slp_code = EXCLUDED.slp_code, vat_sum = EXCLUDED.vat_sum,
                        doc_cur = EXCLUDED.doc_cur, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'purchase_receipt'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_purchase_invoice(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_opch') THEN
                        RETURN QUERY SELECT 'purchase_invoice'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.purchase_invoice (
                        company_id, doc_entry, doc_num, doc_date, doc_due_date,
                        card_code, card_name, doc_total, doc_status, cancelled,
                        slp_code, vat_sum, doc_cur, create_date, update_date,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate", r."DocDueDate",
                           r."CardCode", r."CardName", r."DocTotal", r."DocStatus", r."Cancelled",
                           r."SlpCode", r."VatSum", r."DocCur", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_opch r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        doc_due_date = EXCLUDED.doc_due_date, card_code = EXCLUDED.card_code,
                        card_name = EXCLUDED.card_name, doc_total = EXCLUDED.doc_total,
                        doc_status = EXCLUDED.doc_status, cancelled = EXCLUDED.cancelled,
                        slp_code = EXCLUDED.slp_code, vat_sum = EXCLUDED.vat_sum,
                        doc_cur = EXCLUDED.doc_cur, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'purchase_invoice'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_item_warehouse(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_oitw') THEN
                        RETURN QUERY SELECT 'item_warehouse'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.item_warehouse (
                        company_id, item_code, whs_code,
                        on_hand, is_committed, on_order,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."ItemCode", r."WhsCode",
                           r."OnHand", r."IsCommited", r."OnOrder",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_oitw r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, item_code, whs_code) DO UPDATE SET
                        on_hand = EXCLUDED.on_hand, is_committed = EXCLUDED.is_committed,
                        on_order = EXCLUDED.on_order,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'item_warehouse'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_sales_order(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_ordr') THEN
                        RETURN QUERY SELECT 'sales_order'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.sales_order (
                        company_id, doc_entry, doc_num, doc_date, doc_due_date,
                        card_code, card_name, doc_total, doc_status, cancelled,
                        slp_code, vat_sum, doc_cur, create_date, update_date,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate", r."DocDueDate",
                           r."CardCode", r."CardName", r."DocTotal", r."DocStatus", r."Cancelled",
                           r."SlpCode", r."VatSum", r."DocCur", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_ordr r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        doc_due_date = EXCLUDED.doc_due_date, card_code = EXCLUDED.card_code,
                        card_name = EXCLUDED.card_name, doc_total = EXCLUDED.doc_total,
                        doc_status = EXCLUDED.doc_status, cancelled = EXCLUDED.cancelled,
                        slp_code = EXCLUDED.slp_code, vat_sum = EXCLUDED.vat_sum,
                        doc_cur = EXCLUDED.doc_cur, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'sales_order'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_delivery(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_odln') THEN
                        RETURN QUERY SELECT 'delivery'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.delivery (
                        company_id, doc_entry, doc_num, doc_date, doc_due_date,
                        card_code, card_name, doc_total, doc_status, cancelled,
                        slp_code, vat_sum, doc_cur, create_date, update_date,
                        extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate", r."DocDueDate",
                           r."CardCode", r."CardName", r."DocTotal", r."DocStatus", r."Cancelled",
                           r."SlpCode", r."VatSum", r."DocCur", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_odln r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        doc_due_date = EXCLUDED.doc_due_date, card_code = EXCLUDED.card_code,
                        card_name = EXCLUDED.card_name, doc_total = EXCLUDED.doc_total,
                        doc_status = EXCLUDED.doc_status, cancelled = EXCLUDED.cancelled,
                        slp_code = EXCLUDED.slp_code, vat_sum = EXCLUDED.vat_sum,
                        doc_cur = EXCLUDED.doc_cur, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'delivery'::TEXT, v_count;
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_stock_transfer(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                DECLARE v_count INT;
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_schema='raw' AND table_name='sap_owtr') THEN
                        RETURN QUERY SELECT 'stock_transfer'::TEXT, 0;
                        RETURN;
                    END IF;
                    INSERT INTO stg.stock_transfer (
                        company_id, doc_entry, doc_num, doc_date,
                        from_warehouse, to_warehouse, doc_total, doc_status, cancelled,
                        create_date, update_date, extracted_at_utc, transformed_at_utc
                    )
                    SELECT r.company_id, r."DocEntry", r."DocNum", r."DocDate",
                           r."FromWarehouse", r."ToWarehouse", r."DocTotal",
                           r."DocStatus", r."Cancelled", r."CreateDate", r."UpdateDate",
                           r.extracted_at_utc, NOW()
                    FROM raw.sap_owtr r
                    WHERE r.company_id = p_company_id
                    ON CONFLICT (company_id, doc_entry) DO UPDATE SET
                        doc_num = EXCLUDED.doc_num, doc_date = EXCLUDED.doc_date,
                        from_warehouse = EXCLUDED.from_warehouse, to_warehouse = EXCLUDED.to_warehouse,
                        doc_total = EXCLUDED.doc_total, doc_status = EXCLUDED.doc_status,
                        cancelled = EXCLUDED.cancelled, update_date = EXCLUDED.update_date,
                        extracted_at_utc = EXCLUDED.extracted_at_utc, transformed_at_utc = NOW();
                    GET DIAGNOSTICS v_count = ROW_COUNT;
                    RETURN QUERY SELECT 'stock_transfer'::TEXT, v_count;
                END;
                $$;
                """);

            // ── Update stg.refresh_all to include new functions ───────────────────

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION stg.refresh_all(p_company_id TEXT)
                RETURNS TABLE(object_name TEXT, rows_affected INT) LANGUAGE plpgsql AS $$
                BEGIN
                    RETURN QUERY SELECT * FROM stg.refresh_salesperson(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_customer(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_item(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_sales_invoice(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_sales_invoice_line(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_credit_memo(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_credit_memo_line(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_order(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_receipt(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_purchase_invoice(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_item_warehouse(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_sales_order(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_delivery(p_company_id);
                    RETURN QUERY SELECT * FROM stg.refresh_stock_transfer(p_company_id);
                END;
                $$;
                """);

            // ── Update mart.refresh_purchasing_process to use purchase_receipt ─────
            // Previously checked for stg.purchase_delivery (wrong name) — corrected to purchase_receipt.

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION mart.refresh_purchasing_process(p_company_id TEXT)
                RETURNS VOID LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_order') THEN
                        INSERT INTO mart.purchase_executive_daily (
                            company_id, purchase_date, po_count, po_amount, active_suppliers, transformed_at_utc
                        )
                        SELECT company_id, doc_date,
                            COUNT(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN 1 END),
                            SUM(CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN COALESCE(doc_total,0) ELSE 0 END),
                            COUNT(DISTINCT CASE WHEN COALESCE(cancelled,'N') != 'Y' THEN card_code END),
                            NOW()
                        FROM stg.purchase_order
                        WHERE company_id = p_company_id AND doc_date IS NOT NULL
                        GROUP BY company_id, doc_date
                        ON CONFLICT (company_id, purchase_date) DO UPDATE SET
                            po_count = EXCLUDED.po_count, po_amount = EXCLUDED.po_amount,
                            active_suppliers = EXCLUDED.active_suppliers, transformed_at_utc = NOW();

                        INSERT INTO mart.purchase_supplier_dashboard (
                            company_id, supplier_code, supplier_name,
                            po_count, po_amount, last_po_date, avg_po_amount, transformed_at_utc
                        )
                        SELECT company_id, card_code, MAX(card_name),
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
                            supplier_name = EXCLUDED.supplier_name, po_count = EXCLUDED.po_count,
                            po_amount = EXCLUDED.po_amount, last_po_date = EXCLUDED.last_po_date,
                            avg_po_amount = EXCLUDED.avg_po_amount, transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_order not available, skipping';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='purchase_receipt') THEN
                        INSERT INTO mart.purchase_receiving_dashboard (
                            company_id, supplier_code, supplier_name,
                            gr_count, gr_amount, last_gr_date, transformed_at_utc
                        )
                        SELECT company_id, card_code, MAX(card_name),
                            COUNT(*),
                            SUM(COALESCE(doc_total,0)),
                            MAX(doc_date),
                            NOW()
                        FROM stg.purchase_receipt
                        WHERE company_id = p_company_id
                        GROUP BY company_id, card_code
                        ON CONFLICT (company_id, supplier_code) DO UPDATE SET
                            supplier_name = EXCLUDED.supplier_name, gr_count = EXCLUDED.gr_count,
                            gr_amount = EXCLUDED.gr_amount, last_gr_date = EXCLUDED.last_gr_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_purchasing_process: stg.purchase_receipt not available, skipping';
                    END IF;
                END;
                $$;
                """);

            // ── Update mart.refresh_sales_process for fulfillment dashboard ─────────

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
                        INSERT INTO mart.sales_fulfillment_dashboard (
                            company_id, card_code, card_name,
                            orders_open, orders_total, deliveries_total,
                            fulfillment_rate, last_order_date, last_delivery_date,
                            transformed_at_utc
                        )
                        SELECT
                            o.company_id, o.card_code, MAX(o.card_name),
                            COUNT(CASE WHEN COALESCE(o.cancelled,'N') != 'Y' AND o.doc_status = 'O' THEN 1 END),
                            COUNT(CASE WHEN COALESCE(o.cancelled,'N') != 'Y' THEN 1 END),
                            COUNT(DISTINCT d.doc_entry),
                            CASE WHEN COUNT(CASE WHEN COALESCE(o.cancelled,'N') != 'Y' THEN 1 END) > 0
                                 THEN ROUND(COUNT(DISTINCT d.doc_entry)::NUMERIC
                                      / COUNT(CASE WHEN COALESCE(o.cancelled,'N') != 'Y' THEN 1 END) * 100, 2)
                                 ELSE 0 END,
                            MAX(CASE WHEN COALESCE(o.cancelled,'N') != 'Y' THEN o.doc_date END),
                            MAX(d.doc_date),
                            NOW()
                        FROM stg.sales_order o
                        LEFT JOIN stg.delivery d ON d.company_id = o.company_id AND d.card_code = o.card_code
                        WHERE o.company_id = p_company_id
                        GROUP BY o.company_id, o.card_code
                        ON CONFLICT (company_id, card_code) DO UPDATE SET
                            card_name = EXCLUDED.card_name,
                            orders_open = EXCLUDED.orders_open,
                            orders_total = EXCLUDED.orders_total,
                            deliveries_total = EXCLUDED.deliveries_total,
                            fulfillment_rate = EXCLUDED.fulfillment_rate,
                            last_order_date = EXCLUDED.last_order_date,
                            last_delivery_date = EXCLUDED.last_delivery_date,
                            transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_sales_process: stg.sales_order/delivery not available, skipping fulfillment';
                    END IF;
                END;
                $$;
                """);

            // ── Update mart.refresh_inventory_process for stock dashboard ──────────

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
                        INSERT INTO mart.inventory_stock_dashboard (
                            company_id, item_code, item_name, item_group_code,
                            total_on_hand, total_committed, total_on_order,
                            warehouse_count, transformed_at_utc
                        )
                        SELECT
                            iw.company_id, iw.item_code, MAX(i.item_name), MAX(i.item_group_code::TEXT),
                            SUM(COALESCE(iw.on_hand, 0)),
                            SUM(COALESCE(iw.is_committed, 0)),
                            SUM(COALESCE(iw.on_order, 0)),
                            COUNT(DISTINCT iw.whs_code),
                            NOW()
                        FROM stg.item_warehouse iw
                        LEFT JOIN stg.item i ON i.company_id = iw.company_id AND i.item_code = iw.item_code
                        WHERE iw.company_id = p_company_id
                        GROUP BY iw.company_id, iw.item_code
                        ON CONFLICT (company_id, item_code) DO UPDATE SET
                            item_name = EXCLUDED.item_name, item_group_code = EXCLUDED.item_group_code,
                            total_on_hand = EXCLUDED.total_on_hand,
                            total_committed = EXCLUDED.total_committed,
                            total_on_order = EXCLUDED.total_on_order,
                            warehouse_count = EXCLUDED.warehouse_count, transformed_at_utc = NOW();
                    ELSE
                        RAISE NOTICE 'refresh_inventory_process: stg.item_warehouse not available, skipping stock';
                    END IF;

                    IF EXISTS (SELECT FROM information_schema.tables
                               WHERE table_schema='stg' AND table_name='stock_transfer') THEN
                        INSERT INTO mart.inventory_warehouse_dashboard (
                            company_id, warehouse_code,
                            transfers_out, transfers_in, net_movement,
                            last_transfer_date, transformed_at_utc
                        )
                        SELECT
                            company_id, from_warehouse AS warehouse_code,
                            COUNT(*) AS transfers_out,
                            0 AS transfers_in,
                            -SUM(COALESCE(doc_total, 0)) AS net_movement,
                            MAX(doc_date),
                            NOW()
                        FROM stg.stock_transfer
                        WHERE company_id = p_company_id AND COALESCE(cancelled,'N') != 'Y'
                        GROUP BY company_id, from_warehouse
                        ON CONFLICT (company_id, warehouse_code) DO UPDATE SET
                            transfers_out = EXCLUDED.transfers_out,
                            net_movement = EXCLUDED.net_movement,
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
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_sales_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS mart.refresh_purchasing_process(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_all(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_stock_transfer(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_delivery(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_sales_order(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_item_warehouse(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_purchase_invoice(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_purchase_receipt(TEXT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS stg.refresh_purchase_order(TEXT);");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.stock_transfer;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.delivery;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.sales_order;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.item_warehouse;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.purchase_invoice;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.purchase_receipt;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS stg.purchase_order;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_owtr\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_odln\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_ordr\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_oitw\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_opch\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_opdn\";");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"raw\".\"sap_opor\";");
        }
    }
}
