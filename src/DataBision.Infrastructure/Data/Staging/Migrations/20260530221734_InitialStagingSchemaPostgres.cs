using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <inheritdoc />
    public partial class InitialStagingSchemaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ctl");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "extraction_run",
                schema: "ctl",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    company_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sap_object = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ingestion_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rows_received = table.Column<int>(type: "integer", nullable: false),
                    rows_inserted = table.Column<int>(type: "integer", nullable: false),
                    rows_updated = table.Column<int>(type: "integer", nullable: false),
                    rows_skipped = table.Column<int>(type: "integer", nullable: false),
                    watermark_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    watermark_ts = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extraction_run", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "ingest_audit_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    company_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sap_object = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingest_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ingest_checkpoint",
                schema: "ctl",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    company_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sap_object = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    watermark_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    watermark_ts = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    last_successful_run_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_rows_ingested = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingest_checkpoint", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_object_config",
                schema: "ctl",
                columns: table => new
                {
                    source_object = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    extraction_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    frequency_minutes = table.Column<int>(type: "integer", nullable: false),
                    lookback_normal_days = table.Column<int>(type: "integer", nullable: false),
                    lookback_nightly_days = table.Column<int>(type: "integer", nullable: false),
                    lookback_month_close_days = table.Column<int>(type: "integer", nullable: false),
                    initial_load_from_date = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    supports_update_ts = table.Column<bool>(type: "boolean", nullable: false),
                    supports_create_ts = table.Column<bool>(type: "boolean", nullable: false),
                    header_table = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    line_table = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    primary_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    natural_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_master_data = table.Column<bool>(type: "boolean", nullable: false),
                    page_size = table.Column<int>(type: "integer", nullable: false),
                    max_per_run = table.Column<int>(type: "integer", nullable: false),
                    retention_days_raw = table.Column<int>(type: "integer", nullable: false),
                    alert_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_object_config", x => x.source_object);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ingest_checkpoint_tenant_company_object",
                schema: "ctl",
                table: "ingest_checkpoint",
                columns: new[] { "tenant_id", "company_id", "sap_object" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_source_object_config_object",
                schema: "ctl",
                table: "source_object_config",
                column: "source_object",
                unique: true);

            // Schemas for raw ingest data
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS raw;");
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS stg;");

            // ── raw.sap_oinv — AR Invoice headers ──────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_oinv" (
                    company_id           TEXT          NOT NULL,
                    "DocEntry"           INTEGER       NOT NULL,
                    "DocNum"             INTEGER,
                    "DocDate"            DATE,
                    "DocDueDate"         DATE,
                    "TaxDate"            DATE,
                    "CardCode"           VARCHAR(15),
                    "CardName"           VARCHAR(100),
                    "DocTotal"           NUMERIC(19,6),
                    "DocTotalSy"         NUMERIC(19,6),
                    "VatSum"             NUMERIC(19,6),
                    "PaidToDate"         NUMERIC(19,6),
                    "DocCur"             VARCHAR(3),
                    "DocStatus"          VARCHAR(1),
                    "SlpCode"            VARCHAR(10),
                    "Comments"           TEXT,
                    "ObjType"            VARCHAR(20),
                    "DocType"            VARCHAR(1),
                    "Cancelled"          VARCHAR(1),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                CREATE INDEX idx_sap_oinv_company_update ON "raw"."sap_oinv" (company_id, "UpdateDate");
                CREATE INDEX idx_sap_oinv_company_card   ON "raw"."sap_oinv" (company_id, "CardCode");
                """);

            // ── raw.sap_inv1 — AR Invoice lines ────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_inv1" (
                    company_id           TEXT          NOT NULL,
                    "DocEntry"           INTEGER       NOT NULL,
                    "LineNum"            INTEGER       NOT NULL,
                    "ItemCode"           VARCHAR(20),
                    "Dscription"         VARCHAR(100),
                    "Quantity"           NUMERIC(19,6),
                    "Price"              NUMERIC(19,6),
                    "Currency"           VARCHAR(3),
                    "LineTotal"          NUMERIC(19,6),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry", "LineNum")
                );
                """);

            // ── raw.sap_orin — AR Credit Memo headers ──────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_orin" (
                    company_id           TEXT          NOT NULL,
                    "DocEntry"           INTEGER       NOT NULL,
                    "DocNum"             INTEGER,
                    "DocDate"            DATE,
                    "DocDueDate"         DATE,
                    "TaxDate"            DATE,
                    "CardCode"           VARCHAR(15),
                    "CardName"           VARCHAR(100),
                    "DocTotal"           NUMERIC(19,6),
                    "DocTotalSy"         NUMERIC(19,6),
                    "VatSum"             NUMERIC(19,6),
                    "DocCur"             VARCHAR(3),
                    "DocStatus"          VARCHAR(1),
                    "SlpCode"            VARCHAR(10),
                    "Comments"           TEXT,
                    "ObjType"            VARCHAR(20),
                    "DocType"            VARCHAR(1),
                    "Cancelled"          VARCHAR(1),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry")
                );
                CREATE INDEX idx_sap_orin_company_update ON "raw"."sap_orin" (company_id, "UpdateDate");
                """);

            // ── raw.sap_rin1 — AR Credit Memo lines ────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_rin1" (
                    company_id           TEXT          NOT NULL,
                    "DocEntry"           INTEGER       NOT NULL,
                    "LineNum"            INTEGER       NOT NULL,
                    "ItemCode"           VARCHAR(20),
                    "Dscription"         VARCHAR(100),
                    "Quantity"           NUMERIC(19,6),
                    "Price"              NUMERIC(19,6),
                    "Currency"           VARCHAR(3),
                    "LineTotal"          NUMERIC(19,6),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "DocEntry", "LineNum")
                );
                """);

            // ── raw.sap_ocrd — Business Partners ───────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_ocrd" (
                    company_id           TEXT          NOT NULL,
                    "CardCode"           VARCHAR(15)   NOT NULL,
                    "CardName"           VARCHAR(100),
                    "CardType"           VARCHAR(1),
                    "GroupCode"          VARCHAR(10),
                    "CntctPrsn"          VARCHAR(90),
                    "Phone1"             VARCHAR(20),
                    "Phone2"             VARCHAR(20),
                    "Currency"           VARCHAR(3),
                    "SlpCode"            VARCHAR(10),
                    "VatLiable"          VARCHAR(1),
                    "LicTradNum"         VARCHAR(30),
                    "FrozenFor"          VARCHAR(1),
                    "Balance"            NUMERIC(19,6),
                    "CreditLine"         NUMERIC(19,6),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "CardCode")
                );
                CREATE INDEX idx_sap_ocrd_company_update ON "raw"."sap_ocrd" (company_id, "UpdateDate");
                """);

            // ── raw.sap_oitm — Items ────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_oitm" (
                    company_id           TEXT          NOT NULL,
                    "ItemCode"           VARCHAR(20)   NOT NULL,
                    "ItemName"           VARCHAR(100),
                    "ItmsGrpCod"         INTEGER,
                    "OnHand"             NUMERIC(19,6),
                    "IsCommited"         NUMERIC(19,6),
                    "OnOrder"            NUMERIC(19,6),
                    "MinLevel"           NUMERIC(19,6),
                    "MaxLevel"           NUMERIC(19,6),
                    "AvgPrice"           NUMERIC(19,6),
                    "LastPurPrc"         NUMERIC(19,6),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "ItemCode")
                );
                CREATE INDEX idx_sap_oitm_company_update ON "raw"."sap_oitm" (company_id, "UpdateDate");
                """);

            // ── raw.sap_oslp — Salespersons ─────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_oslp" (
                    company_id           TEXT          NOT NULL,
                    "SlpCode"            INTEGER       NOT NULL,
                    "SlpName"            VARCHAR(50),
                    "CreateDate"         DATE,
                    "CreateTS"           VARCHAR(10),
                    "CreateTSNorm"       CHAR(6),
                    "UpdateDate"         DATE,
                    "UpdateTS"           VARCHAR(10),
                    "UpdateTSNorm"       CHAR(6),
                    source_hash_hex      CHAR(64)      NOT NULL,
                    extraction_run_id    TEXT,
                    batch_id             TEXT,
                    extracted_at_utc     TIMESTAMPTZ,
                    ingestion_mode       VARCHAR(20),
                    raw_created_at_utc   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    raw_updated_at_utc   TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "SlpCode")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extraction_run",
                schema: "ctl");

            migrationBuilder.DropTable(
                name: "ingest_audit_log",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "ingest_checkpoint",
                schema: "ctl");

            migrationBuilder.DropTable(
                name: "source_object_config",
                schema: "ctl");

            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oslp";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oitm";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_ocrd";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_rin1";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_orin";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_inv1";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "raw"."sap_oinv";""");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS stg CASCADE;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS raw CASCADE;");
        }
    }
}
