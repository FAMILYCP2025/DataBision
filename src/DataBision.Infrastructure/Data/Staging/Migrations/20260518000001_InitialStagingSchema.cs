using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBision.Infrastructure.Data.Staging.Migrations;

/// <inheritdoc />
public partial class InitialStagingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Schemas ─────────────────────────────────────────────────────────
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'raw')  EXEC('CREATE SCHEMA [raw]');");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'stg')  EXEC('CREATE SCHEMA [stg]');");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ctl')  EXEC('CREATE SCHEMA [ctl]');");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'audit') EXEC('CREATE SCHEMA [audit]');");

        // ── 2. ctl.source_object_config ────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "source_object_config",
            schema: "ctl",
            columns: table => new
            {
                SourceObject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ExtractionMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "INCREMENTAL"),
                FrequencyMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                LookbackNormalDays = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                LookbackNightlyDays = table.Column<int>(type: "int", nullable: false, defaultValue: 7),
                LookbackMonthCloseDays = table.Column<int>(type: "int", nullable: false, defaultValue: 35),
                InitialLoadFromDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                SupportsUpdateTs = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                SupportsCreateTs = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                HeaderTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                LineTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                PrimaryKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                NaturalKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsMasterData = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                PageSize = table.Column<int>(type: "int", nullable: false, defaultValue: 500),
                MaxPerRun = table.Column<int>(type: "int", nullable: false, defaultValue: 10000),
                RetentionDaysRaw = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                AlertEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_source_object_config", x => x.SourceObject));

        // ── 3. ctl.extraction_run ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "extraction_run",
            schema: "ctl",
            columns: table => new
            {
                RunId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                CompanyId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                SapObject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                IngestionMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowsReceived = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RowsInserted = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RowsUpdated = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                RowsSkipped = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                WatermarkDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                WatermarkTs = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_extraction_run", x => x.RunId));

        // ── 4. ctl.ingest_checkpoint ───────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ingest_checkpoint",
            schema: "ctl",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                CompanyId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                SapObject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                WatermarkDate = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                WatermarkTs = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                LastSuccessfulRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                TotalRowsIngested = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ingest_checkpoint", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "UX_ingest_checkpoint_tenant_company_object",
            schema: "ctl",
            table: "ingest_checkpoint",
            columns: ["TenantId", "CompanyId", "SapObject"],
            unique: true);

        // ── 5. audit.ingest_audit_log ──────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "ingest_audit_log",
            schema: "audit",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                RunId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                CompanyId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                SapObject = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Detail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ingest_audit_log", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ingest_audit_log_run_tenant",
            schema: "audit",
            table: "ingest_audit_log",
            columns: ["RunId", "TenantId"]);

        // ── 6. raw tables (MVP: 7 SAP objects with raw.sap_* prefix) ───────────

        // raw.sap_oinv — AR Invoice headers
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_oinv] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [DocEntry]           INT           NOT NULL,
    [DocNum]             INT           NULL,
    [DocDate]            DATE          NULL,
    [DocDueDate]         DATE          NULL,
    [TaxDate]            DATE          NULL,
    [CardCode]           NVARCHAR(15)  NULL,
    [CardName]           NVARCHAR(100) NULL,
    [DocTotal]           DECIMAL(19,6) NULL,
    [DocTotalSy]         DECIMAL(19,6) NULL,
    [VatSum]             DECIMAL(19,6) NULL,
    [PaidToDate]         DECIMAL(19,6) NULL,
    [DocCur]             NVARCHAR(3)   NULL,
    [DocStatus]          NVARCHAR(1)   NULL,
    [SlpCode]            NVARCHAR(20)  NULL,
    [Comments]           NVARCHAR(254) NULL,
    [ObjType]            NVARCHAR(20)  NULL,
    [DocType]            NVARCHAR(1)   NULL,
    [Cancelled]          NVARCHAR(1)   NULL,
    [CreateDate]         DATE          NULL,
    [CreateTS]           NVARCHAR(20)  NULL,
    [CreateTSNorm]       CHAR(6)       NULL,
    [UpdateDate]         DATE          NULL,
    [UpdateTS]           NVARCHAR(20)  NULL,
    [UpdateTSNorm]       CHAR(6)       NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_oinv] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_oinv_company_docentry] UNIQUE ([company_id], [DocEntry])
);
CREATE INDEX [IX_raw_sap_oinv_company_updatedate] ON [raw].[sap_oinv] ([company_id], [UpdateDate], [UpdateTSNorm]);");

        // raw.sap_inv1 — AR Invoice lines
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_inv1] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [DocEntry]           INT           NOT NULL,
    [LineNum]            INT           NOT NULL,
    [ItemCode]           NVARCHAR(50)  NULL,
    [Dscription]         NVARCHAR(100) NULL,
    [Quantity]           DECIMAL(19,6) NULL,
    [Price]              DECIMAL(19,6) NULL,
    [LineTotal]          DECIMAL(19,6) NULL,
    [Currency]           NVARCHAR(3)   NULL,
    [SlpCode]            NVARCHAR(20)  NULL,
    [WhsCode]            NVARCHAR(8)   NULL,
    [UomCode]            NVARCHAR(20)  NULL,
    [DiscPrcnt]          DECIMAL(19,6) NULL,
    [GrossBuyPr]         DECIMAL(19,6) NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_inv1] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_inv1_company_docentry_linenum] UNIQUE ([company_id], [DocEntry], [LineNum])
);");

        // raw.sap_orin — AR Credit Memo headers
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_orin] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [DocEntry]           INT           NOT NULL,
    [DocNum]             INT           NULL,
    [DocDate]            DATE          NULL,
    [DocDueDate]         DATE          NULL,
    [TaxDate]            DATE          NULL,
    [CardCode]           NVARCHAR(15)  NULL,
    [CardName]           NVARCHAR(100) NULL,
    [DocTotal]           DECIMAL(19,6) NULL,
    [DocTotalSy]         DECIMAL(19,6) NULL,
    [VatSum]             DECIMAL(19,6) NULL,
    [DocCur]             NVARCHAR(3)   NULL,
    [DocStatus]          NVARCHAR(1)   NULL,
    [SlpCode]            NVARCHAR(20)  NULL,
    [Comments]           NVARCHAR(254) NULL,
    [ObjType]            NVARCHAR(20)  NULL,
    [DocType]            NVARCHAR(1)   NULL,
    [Cancelled]          NVARCHAR(1)   NULL,
    [CreateDate]         DATE          NULL,
    [CreateTS]           NVARCHAR(20)  NULL,
    [CreateTSNorm]       CHAR(6)       NULL,
    [UpdateDate]         DATE          NULL,
    [UpdateTS]           NVARCHAR(20)  NULL,
    [UpdateTSNorm]       CHAR(6)       NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_orin] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_orin_company_docentry] UNIQUE ([company_id], [DocEntry])
);
CREATE INDEX [IX_raw_sap_orin_company_updatedate] ON [raw].[sap_orin] ([company_id], [UpdateDate], [UpdateTSNorm]);");

        // raw.sap_rin1 — AR Credit Memo lines
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_rin1] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [DocEntry]           INT           NOT NULL,
    [LineNum]            INT           NOT NULL,
    [ItemCode]           NVARCHAR(50)  NULL,
    [Dscription]         NVARCHAR(100) NULL,
    [Quantity]           DECIMAL(19,6) NULL,
    [Price]              DECIMAL(19,6) NULL,
    [LineTotal]          DECIMAL(19,6) NULL,
    [Currency]           NVARCHAR(3)   NULL,
    [SlpCode]            NVARCHAR(20)  NULL,
    [WhsCode]            NVARCHAR(8)   NULL,
    [UomCode]            NVARCHAR(20)  NULL,
    [DiscPrcnt]          DECIMAL(19,6) NULL,
    [BaseRef]            NVARCHAR(50)  NULL,
    [BaseEntry]          INT           NULL,
    [BaseLine]           INT           NULL,
    [BaseType]           NVARCHAR(20)  NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_rin1] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_rin1_company_docentry_linenum] UNIQUE ([company_id], [DocEntry], [LineNum])
);");

        // raw.sap_ocrd — Business Partners / Customers (master data)
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_ocrd] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [CardCode]           NVARCHAR(15)  NOT NULL,
    [CardName]           NVARCHAR(100) NULL,
    [CardType]           NVARCHAR(1)   NULL,
    [GroupCode]          NVARCHAR(20)  NULL,
    [CntctPrsn]          NVARCHAR(90)  NULL,
    [Phone1]             NVARCHAR(20)  NULL,
    [Phone2]             NVARCHAR(20)  NULL,
    [Fax]                NVARCHAR(20)  NULL,
    [EMail]              NVARCHAR(100) NULL,
    [Country]            NVARCHAR(3)   NULL,
    [City]               NVARCHAR(100) NULL,
    [ZipCode]            NVARCHAR(20)  NULL,
    [Currency]           NVARCHAR(3)   NULL,
    [SlpCode]            NVARCHAR(20)  NULL,
    [VatLiable]          NVARCHAR(1)   NULL,
    [LicTradNum]         NVARCHAR(32)  NULL,
    [FrozenFor]          NVARCHAR(1)   NULL,
    [Balance]            DECIMAL(19,6) NULL,
    [CreditLine]         DECIMAL(19,6) NULL,
    [CreateDate]         DATE          NULL,
    [CreateTS]           NVARCHAR(20)  NULL,
    [CreateTSNorm]       CHAR(6)       NULL,
    [UpdateDate]         DATE          NULL,
    [UpdateTS]           NVARCHAR(20)  NULL,
    [UpdateTSNorm]       CHAR(6)       NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_ocrd] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_ocrd_company_cardcode] UNIQUE ([company_id], [CardCode])
);
CREATE INDEX [IX_raw_sap_ocrd_company_updatedate] ON [raw].[sap_ocrd] ([company_id], [UpdateDate], [UpdateTSNorm]);");

        // raw.sap_oitm — Items (master data)
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_oitm] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [ItemCode]           NVARCHAR(50)  NOT NULL,
    [ItemName]           NVARCHAR(100) NULL,
    [FrgnName]           NVARCHAR(100) NULL,
    [ItmsGrpCod]         NVARCHAR(20)  NULL,
    [CstGrpCode]         NVARCHAR(20)  NULL,
    [InvntryUom]         NVARCHAR(20)  NULL,
    [BuyUnitMsr]         NVARCHAR(20)  NULL,
    [SalUnitMsr]         NVARCHAR(20)  NULL,
    [ManSerNum]          NVARCHAR(1)   NULL,
    [OnHand]             DECIMAL(19,6) NULL,
    [IsCommited]         DECIMAL(19,6) NULL,
    [OnOrder]            DECIMAL(19,6) NULL,
    [AvgPrice]           DECIMAL(19,6) NULL,
    [LastPurPrc]         DECIMAL(19,6) NULL,
    [ItemType]           NVARCHAR(1)   NULL,
    [SWW]                NVARCHAR(50)  NULL,
    [Canceled]           NVARCHAR(1)   NULL,
    [CreateDate]         DATE          NULL,
    [CreateTS]           NVARCHAR(20)  NULL,
    [CreateTSNorm]       CHAR(6)       NULL,
    [UpdateDate]         DATE          NULL,
    [UpdateTS]           NVARCHAR(20)  NULL,
    [UpdateTSNorm]       CHAR(6)       NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_oitm] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_oitm_company_itemcode] UNIQUE ([company_id], [ItemCode])
);
CREATE INDEX [IX_raw_sap_oitm_company_updatedate] ON [raw].[sap_oitm] ([company_id], [UpdateDate], [UpdateTSNorm]);");

        // raw.sap_oslp — Salespersons (master data)
        migrationBuilder.Sql(@"
CREATE TABLE [raw].[sap_oslp] (
    [raw_id]             BIGINT IDENTITY(1,1) NOT NULL,
    [company_id]         NVARCHAR(36)  NOT NULL,
    [SlpCode]            INT           NOT NULL,
    [SlpName]            NVARCHAR(50)  NULL,
    [Commission]         DECIMAL(19,6) NULL,
    [Email]              NVARCHAR(100) NULL,
    [Mobile]             NVARCHAR(50)  NULL,
    [Telephone]          NVARCHAR(50)  NULL,
    [Active]             NVARCHAR(1)   NULL,
    [GroupCode]          INT           NULL,
    [CreateDate]         DATE          NULL,
    [CreateTS]           NVARCHAR(20)  NULL,
    [CreateTSNorm]       CHAR(6)       NULL,
    [UpdateDate]         DATE          NULL,
    [UpdateTS]           NVARCHAR(20)  NULL,
    [UpdateTSNorm]       CHAR(6)       NULL,
    [source_hash_hex]    CHAR(64)      NULL,
    [extraction_run_id]  NVARCHAR(36)  NULL,
    [batch_id]           NVARCHAR(36)  NULL,
    [extracted_at_utc]   DATETIME2     NULL,
    [ingestion_mode]     NVARCHAR(30)  NULL,
    [raw_created_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    [raw_updated_at_utc] DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_raw_sap_oslp] PRIMARY KEY CLUSTERED ([raw_id]),
    CONSTRAINT [UX_raw_sap_oslp_company_slpcode] UNIQUE ([company_id], [SlpCode])
);
CREATE INDEX [IX_raw_sap_oslp_company_updatedate] ON [raw].[sap_oslp] ([company_id], [UpdateDate], [UpdateTSNorm]);");

        // ── 7. Seed ctl.source_object_config (MVP 7 objects) ──────────────────
        migrationBuilder.Sql(@"
INSERT INTO [ctl].[source_object_config]
    (SourceObject, Enabled, ExtractionMode, FrequencyMinutes,
     LookbackNormalDays, LookbackNightlyDays, LookbackMonthCloseDays,
     SupportsUpdateTs, SupportsCreateTs, HeaderTable, LineTable,
     PrimaryKey, NaturalKey, IsMasterData, PageSize, MaxPerRun, RetentionDaysRaw,
     UpdatedAt)
VALUES
    ('OINV',1,'INCREMENTAL',60, 3,7,35, 1,1, 'OINV','INV1', 'DocEntry','DocNum,DocDate,CardCode',0, 500,10000,90, GETUTCDATE()),
    ('INV1',1,'INCREMENTAL',60, 3,7,35, 0,0, 'OINV','INV1', 'DocEntry,LineNum','DocEntry,LineNum',0, 1000,50000,90, GETUTCDATE()),
    ('ORIN',1,'INCREMENTAL',60, 3,7,35, 1,1, 'ORIN','RIN1', 'DocEntry','DocNum,DocDate,CardCode',0, 500,10000,90, GETUTCDATE()),
    ('RIN1',1,'INCREMENTAL',60, 3,7,35, 0,0, 'ORIN','RIN1', 'DocEntry,LineNum','DocEntry,LineNum',0, 1000,50000,90, GETUTCDATE()),
    ('OCRD',1,'FULL',1440,   7,7,35, 1,1, 'OCRD',NULL,      'CardCode','CardCode',1, 1000,100000,90, GETUTCDATE()),
    ('OITM',1,'FULL',1440,   7,7,35, 1,1, 'OITM',NULL,      'ItemCode','ItemCode',1, 1000,100000,90, GETUTCDATE()),
    ('OSLP',1,'FULL',1440,   7,7,35, 1,1, 'OSLP',NULL,      'SlpCode','SlpCode',1, 1000,100000,90, GETUTCDATE());
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_oslp];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_oitm];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_ocrd];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_rin1];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_orin];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_inv1];");
        migrationBuilder.Sql("DROP TABLE IF EXISTS [raw].[sap_oinv];");

        migrationBuilder.DropTable(name: "ingest_audit_log", schema: "audit");
        migrationBuilder.DropTable(name: "ingest_checkpoint", schema: "ctl");
        migrationBuilder.DropTable(name: "extraction_run", schema: "ctl");
        migrationBuilder.DropTable(name: "source_object_config", schema: "ctl");
    }
}
