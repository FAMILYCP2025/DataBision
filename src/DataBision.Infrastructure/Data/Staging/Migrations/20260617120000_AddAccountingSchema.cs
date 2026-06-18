using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations
{
    /// <summary>
    /// Sprint 13A: Adds RAW + STG + CFG + MART placeholder tables for SAP B1 accounting objects.
    /// RAW:  raw.sap_oact, raw.sap_ojdt, raw.sap_jdt1
    /// STG:  stg.gl_account, stg.journal_entry, stg.journal_entry_line
    /// CFG:  cfg.account_classification_rules
    /// MART: mart.gl_accounts, mart.account_balances, mart.income_statement_summary, mart.balance_sheet_summary
    ///
    /// MART tables are empty placeholders. Finance accounting tabs remain on the FinancialDataPending
    /// fallback screen until Sprint 13B wires the ETL refresh functions.
    ///
    /// OACT uses full-refresh (no UpdateDate in SL ChartOfAccounts).
    /// OJDT uses incremental by ReferenceDate via OjdtExtractorJob.
    /// JDT1 lines are embedded in OJDT responses via $expand=JournalEntryLines.
    /// </summary>
    public partial class AddAccountingSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── RAW — sap_oact (Chart of Accounts) ───────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_oact" (
                    company_id          TEXT          NOT NULL,
                    "Code"              VARCHAR(50)   NOT NULL,
                    "Name"              VARCHAR(200),
                    "FatherNum"         VARCHAR(50),
                    "Levels"            VARCHAR(10),
                    "GroupMask"         VARCHAR(10),
                    "AccountType"       VARCHAR(20),
                    "Postable"          VARCHAR(1),
                    "Frozen"            VARCHAR(1),
                    "ValidFor"          VARCHAR(1),
                    "CashAccount"       VARCHAR(1),
                    "ControlAccount"    VARCHAR(1),
                    "Currency"          VARCHAR(10),
                    "FormatCode"        VARCHAR(50),
                    "ExternalCode"      VARCHAR(50),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(40),
                    batch_id            VARCHAR(40),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "Code")
                );
                CREATE INDEX IF NOT EXISTS ix_raw_sap_oact_company ON "raw"."sap_oact" (company_id);
                """);

            // ── RAW — sap_ojdt (Journal Entry headers) ───────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_ojdt" (
                    company_id          TEXT          NOT NULL,
                    "TransId"           INTEGER       NOT NULL,
                    "JdtNum"            INTEGER,
                    "RefDate"           DATE,
                    "DueDate"           DATE,
                    "TaxDate"           DATE,
                    "Memo"              VARCHAR(200),
                    "TransType"         VARCHAR(20),
                    "BaseRef"           VARCHAR(50),
                    "UserRef"           VARCHAR(100),
                    "CreatedBy"         VARCHAR(50),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(40),
                    batch_id            VARCHAR(40),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "TransId")
                );
                CREATE INDEX IF NOT EXISTS ix_raw_sap_ojdt_refdate ON "raw"."sap_ojdt" (company_id, "RefDate");
                """);

            // ── RAW — sap_jdt1 (Journal Entry lines) ─────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "raw"."sap_jdt1" (
                    company_id          TEXT          NOT NULL,
                    "TransId"           INTEGER       NOT NULL,
                    "LineId"            INTEGER       NOT NULL,
                    "Account"           VARCHAR(50),
                    "Debit"             NUMERIC(18,4),
                    "Credit"            NUMERIC(18,4),
                    "FcDebit"           NUMERIC(18,4),
                    "FcCredit"          NUMERIC(18,4),
                    "SysDebit"          NUMERIC(18,4),
                    "SysCredit"         NUMERIC(18,4),
                    "ShortName"         VARCHAR(50),
                    "ContraAct"         VARCHAR(50),
                    "LineMemo"          VARCHAR(200),
                    "RefDate"           DATE,
                    "ProfitCode"        VARCHAR(50),
                    "OcrCode"           VARCHAR(50),
                    "OcrCode2"          VARCHAR(50),
                    "OcrCode3"          VARCHAR(50),
                    "OcrCode4"          VARCHAR(50),
                    "OcrCode5"          VARCHAR(50),
                    "ProjectCode"       VARCHAR(50),
                    source_hash_hex     VARCHAR(64),
                    extraction_run_id   VARCHAR(40),
                    batch_id            VARCHAR(40),
                    extracted_at_utc    TIMESTAMPTZ,
                    ingestion_mode      VARCHAR(30),
                    raw_created_at_utc  TIMESTAMPTZ DEFAULT NOW(),
                    raw_updated_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, "TransId", "LineId")
                );
                CREATE INDEX IF NOT EXISTS ix_raw_sap_jdt1_account ON "raw"."sap_jdt1" (company_id, "Account");
                """);

            // ── STG — gl_account ─────────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.gl_account (
                    company_id          TEXT        NOT NULL,
                    code                VARCHAR(50) NOT NULL,
                    name                VARCHAR(200),
                    father_num          VARCHAR(50),
                    level               INTEGER,
                    group_mask          VARCHAR(10),
                    account_type        VARCHAR(20),
                    postable            BOOLEAN,
                    frozen              BOOLEAN,
                    valid_for           BOOLEAN,
                    cash_account        BOOLEAN,
                    control_account     BOOLEAN,
                    currency            VARCHAR(10),
                    format_code         VARCHAR(50),
                    external_code       VARCHAR(50),
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, code)
                );
                """);

            // ── STG — journal_entry ───────────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.journal_entry (
                    company_id          TEXT        NOT NULL,
                    trans_id            INTEGER     NOT NULL,
                    jdt_num             INTEGER,
                    ref_date            DATE,
                    due_date            DATE,
                    tax_date            DATE,
                    memo                VARCHAR(200),
                    trans_type          VARCHAR(20),
                    base_ref            VARCHAR(50),
                    user_ref            VARCHAR(100),
                    created_by          VARCHAR(50),
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, trans_id)
                );
                CREATE INDEX IF NOT EXISTS ix_stg_journal_entry_refdate ON stg.journal_entry (company_id, ref_date);
                """);

            // ── STG — journal_entry_line ──────────────────────────────────────────
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS stg.journal_entry_line (
                    company_id          TEXT          NOT NULL,
                    trans_id            INTEGER       NOT NULL,
                    line_id             INTEGER       NOT NULL,
                    account             VARCHAR(50),
                    debit               NUMERIC(18,4),
                    credit              NUMERIC(18,4),
                    fc_debit            NUMERIC(18,4),
                    fc_credit           NUMERIC(18,4),
                    sys_debit           NUMERIC(18,4),
                    sys_credit          NUMERIC(18,4),
                    short_name          VARCHAR(50),
                    contra_act          VARCHAR(50),
                    line_memo           VARCHAR(200),
                    ref_date            DATE,
                    profit_code         VARCHAR(50),
                    ocr_code            VARCHAR(50),
                    ocr_code2           VARCHAR(50),
                    ocr_code3           VARCHAR(50),
                    ocr_code4           VARCHAR(50),
                    ocr_code5           VARCHAR(50),
                    project_code        VARCHAR(50),
                    extracted_at_utc    TIMESTAMPTZ,
                    transformed_at_utc  TIMESTAMPTZ,
                    PRIMARY KEY (company_id, trans_id, line_id)
                );
                CREATE INDEX IF NOT EXISTS ix_stg_jel_account ON stg.journal_entry_line (company_id, account);
                """);

            // ── CFG — account_classification_rules ───────────────────────────────
            // Manual override table: maps account codes / format-code prefixes to P&L / BS lines.
            // Used by Sprint 13B ETL to populate mart.gl_accounts.statement_line.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS cfg.account_classification_rules (
                    id              SERIAL        PRIMARY KEY,
                    company_id      TEXT          NOT NULL,
                    account_code    VARCHAR(50),       -- NULL = applies to all matching format_code prefix
                    format_code     VARCHAR(50),       -- NULL = applies to specific account_code only
                    statement_line  VARCHAR(50)   NOT NULL,
                    -- Recognised values: 'revenue', 'cogs', 'opex', 'other_income', 'other_expense',
                    --                    'current_assets', 'non_current_assets', 'current_liabilities',
                    --                    'non_current_liabilities', 'equity'
                    created_at      TIMESTAMPTZ   DEFAULT NOW(),
                    updated_at      TIMESTAMPTZ   DEFAULT NOW(),
                    UNIQUE (company_id, account_code)
                );
                CREATE INDEX IF NOT EXISTS ix_cfg_acr_company ON cfg.account_classification_rules (company_id);
                """);

            // ── MART placeholders — populated by Sprint 13B ETL ──────────────────
            // Tables exist so the Finance accounting endpoints can query them without errors.
            // They will return empty result sets until the ETL refresh functions are wired.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.gl_accounts (
                    company_id      TEXT          NOT NULL,
                    code            VARCHAR(50)   NOT NULL,
                    name            VARCHAR(200),
                    father_num      VARCHAR(50),
                    level           INTEGER,
                    account_type    VARCHAR(20),
                    statement_line  VARCHAR(50),
                    postable        BOOLEAN,
                    currency        VARCHAR(10),
                    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
                    PRIMARY KEY (company_id, code)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.account_balances (
                    company_id      TEXT          NOT NULL,
                    code            VARCHAR(50)   NOT NULL,
                    period_year     INTEGER       NOT NULL,
                    period_month    INTEGER       NOT NULL,
                    debit_sum       NUMERIC(18,4) DEFAULT 0,
                    credit_sum      NUMERIC(18,4) DEFAULT 0,
                    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
                    PRIMARY KEY (company_id, code, period_year, period_month)
                );
                CREATE INDEX IF NOT EXISTS ix_mart_acb_period ON mart.account_balances (company_id, period_year, period_month);
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.income_statement_summary (
                    company_id      TEXT          NOT NULL,
                    period_year     INTEGER       NOT NULL,
                    period_month    INTEGER       NOT NULL,
                    statement_line  VARCHAR(50)   NOT NULL,
                    amount          NUMERIC(18,4) DEFAULT 0,
                    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
                    PRIMARY KEY (company_id, period_year, period_month, statement_line)
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS mart.balance_sheet_summary (
                    company_id      TEXT          NOT NULL,
                    snapshot_date   DATE          NOT NULL,
                    category        VARCHAR(50)   NOT NULL,
                    sub_category    VARCHAR(100)  NOT NULL DEFAULT '',
                    amount          NUMERIC(18,4) DEFAULT 0,
                    refreshed_at    TIMESTAMPTZ   DEFAULT NOW(),
                    PRIMARY KEY (company_id, snapshot_date, category, sub_category)
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS mart.balance_sheet_summary;
                DROP TABLE IF EXISTS mart.income_statement_summary;
                DROP TABLE IF EXISTS mart.account_balances;
                DROP TABLE IF EXISTS mart.gl_accounts;
                DROP TABLE IF EXISTS cfg.account_classification_rules;
                DROP TABLE IF EXISTS stg.journal_entry_line;
                DROP TABLE IF EXISTS stg.journal_entry;
                DROP TABLE IF EXISTS stg.gl_account;
                DROP TABLE IF EXISTS "raw"."sap_jdt1";
                DROP TABLE IF EXISTS "raw"."sap_ojdt";
                DROP TABLE IF EXISTS "raw"."sap_oact";
                """);
        }
    }
}
