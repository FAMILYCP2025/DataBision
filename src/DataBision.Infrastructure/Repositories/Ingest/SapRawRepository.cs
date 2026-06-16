using Dapper;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Ingest;

/// <summary>
/// Dapper-based upsert for raw.sap_* tables using PostgreSQL INSERT ON CONFLICT.
/// Separate from EF to avoid change-tracking overhead on high-volume upserts.
/// PostgreSQL xmax is used only for MVP insert/update counting.
/// Revisit during hardening if stricter audit is required.
/// </summary>
public sealed class SapRawRepository(string connectionString, ILogger<SapRawRepository> _logger)
    : ISapRawRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    private static (int inserted, int updated) CountResults(IEnumerable<int> results)
    {
        // xmax = 0 → newly inserted row → is_insert = 1
        // xmax ≠ 0 → updated row        → is_insert = 0
        // no row returned               → skipped by hash guard or date guard
        var list = results.ToList();
        return (list.Count(x => x == 1), list.Count(x => x == 0));
    }

    // ── Sales Invoices (OINV) ──────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalesInvoicesAsync(
        string companyId, IEnumerable<SapOinvRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_oinv" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "PaidToDate",
                "DocCur", "DocStatus", "SlpCode", "Comments", "ObjType", "DocType", "Cancelled",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @PaidToDate,
                @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum"           = EXCLUDED."DocNum",
                "DocDate"          = EXCLUDED."DocDate",
                "DocDueDate"       = EXCLUDED."DocDueDate",
                "TaxDate"          = EXCLUDED."TaxDate",
                "CardCode"         = EXCLUDED."CardCode",
                "CardName"         = EXCLUDED."CardName",
                "DocTotal"         = EXCLUDED."DocTotal",
                "DocTotalSy"       = EXCLUDED."DocTotalSy",
                "VatSum"           = EXCLUDED."VatSum",
                "PaidToDate"       = EXCLUDED."PaidToDate",
                "DocCur"           = EXCLUDED."DocCur",
                "DocStatus"        = EXCLUDED."DocStatus",
                "SlpCode"          = EXCLUDED."SlpCode",
                "Comments"         = EXCLUDED."Comments",
                "ObjType"          = EXCLUDED."ObjType",
                "DocType"          = EXCLUDED."DocType",
                "Cancelled"        = EXCLUDED."Cancelled",
                "CreateDate"       = EXCLUDED."CreateDate",
                "CreateTS"         = EXCLUDED."CreateTS",
                "CreateTSNorm"     = EXCLUDED."CreateTSNorm",
                "UpdateDate"       = EXCLUDED."UpdateDate",
                "UpdateTS"         = EXCLUDED."UpdateTS",
                "UpdateTSNorm"     = EXCLUDED."UpdateTSNorm",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oinv".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oinv"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oinv"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oinv"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapOinv(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Sales Invoice Lines (INV1) ─────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalesInvoiceLinesAsync(
        string companyId, IEnumerable<SapInv1Row> rows, CancellationToken ct)
    {
        // SapInv1Row has no UpdateDate/UpdateTS — watermarks are inherited from header (OINV).
        // Using hash-only guard: if date guard were applied against NULL UpdateDate,
        // the comparison would always evaluate to NULL (falsy) and no updates would ever occur.
        const string sql = """
            INSERT INTO "raw"."sap_inv1" (
                company_id, "DocEntry", "LineNum",
                "ItemCode", "Dscription", "Quantity", "Price", "Currency", "LineTotal",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @LineNum,
                @ItemCode, @Dscription, @Quantity, @Price, @Currency, @LineTotal,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry", "LineNum") DO UPDATE SET
                "ItemCode"         = EXCLUDED."ItemCode",
                "Dscription"       = EXCLUDED."Dscription",
                "Quantity"         = EXCLUDED."Quantity",
                "Price"            = EXCLUDED."Price",
                "Currency"         = EXCLUDED."Currency",
                "LineTotal"        = EXCLUDED."LineTotal",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_inv1".source_hash_hex != EXCLUDED.source_hash_hex
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapInv1(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Credit Memos (ORIN) ────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertCreditMemosAsync(
        string companyId, IEnumerable<SapOrinRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_orin" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum",
                "DocCur", "DocStatus", "SlpCode", "Comments", "ObjType", "DocType", "Cancelled",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum,
                @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum"           = EXCLUDED."DocNum",
                "DocDate"          = EXCLUDED."DocDate",
                "DocDueDate"       = EXCLUDED."DocDueDate",
                "TaxDate"          = EXCLUDED."TaxDate",
                "CardCode"         = EXCLUDED."CardCode",
                "CardName"         = EXCLUDED."CardName",
                "DocTotal"         = EXCLUDED."DocTotal",
                "DocTotalSy"       = EXCLUDED."DocTotalSy",
                "VatSum"           = EXCLUDED."VatSum",
                "DocCur"           = EXCLUDED."DocCur",
                "DocStatus"        = EXCLUDED."DocStatus",
                "SlpCode"          = EXCLUDED."SlpCode",
                "Comments"         = EXCLUDED."Comments",
                "ObjType"          = EXCLUDED."ObjType",
                "DocType"          = EXCLUDED."DocType",
                "Cancelled"        = EXCLUDED."Cancelled",
                "CreateDate"       = EXCLUDED."CreateDate",
                "CreateTS"         = EXCLUDED."CreateTS",
                "CreateTSNorm"     = EXCLUDED."CreateTSNorm",
                "UpdateDate"       = EXCLUDED."UpdateDate",
                "UpdateTS"         = EXCLUDED."UpdateTS",
                "UpdateTSNorm"     = EXCLUDED."UpdateTSNorm",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_orin".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_orin"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_orin"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_orin"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapOrin(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Credit Memo Lines (RIN1) ───────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertCreditMemoLinesAsync(
        string companyId, IEnumerable<SapRin1Row> rows, CancellationToken ct)
    {
        // SapRin1Row has no UpdateDate/UpdateTS — watermarks are inherited from header (ORIN).
        // Hash-only guard for the same reason as sap_inv1 (see UpsertSalesInvoiceLinesAsync).
        const string sql = """
            INSERT INTO "raw"."sap_rin1" (
                company_id, "DocEntry", "LineNum",
                "ItemCode", "Dscription", "Quantity", "Price", "Currency", "LineTotal",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @LineNum,
                @ItemCode, @Dscription, @Quantity, @Price, @Currency, @LineTotal,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry", "LineNum") DO UPDATE SET
                "ItemCode"         = EXCLUDED."ItemCode",
                "Dscription"       = EXCLUDED."Dscription",
                "Quantity"         = EXCLUDED."Quantity",
                "Price"            = EXCLUDED."Price",
                "Currency"         = EXCLUDED."Currency",
                "LineTotal"        = EXCLUDED."LineTotal",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_rin1".source_hash_hex != EXCLUDED.source_hash_hex
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapRin1(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Customers (OCRD) ──────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertCustomersAsync(
        string companyId, IEnumerable<SapOcrdRow> rows, CancellationToken ct)
    {
        // SapOcrdRow also has Fax, EMail, Country, City, ZipCode — not in raw.sap_ocrd DDL.
        // Those DynamicParameters are silently ignored by Dapper since they are not referenced in SQL.
        const string sql = """
            INSERT INTO "raw"."sap_ocrd" (
                company_id, "CardCode", "CardName", "CardType", "GroupCode",
                "CntctPrsn", "Phone1", "Phone2", "Currency", "SlpCode",
                "VatLiable", "LicTradNum", "FrozenFor", "Balance", "CreditLine",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @CardCode, @CardName, @CardType, @GroupCode,
                @CntctPrsn, @Phone1, @Phone2, @Currency, @SlpCode,
                @VatLiable, @LicTradNum, @FrozenFor, @Balance, @CreditLine,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "CardCode") DO UPDATE SET
                "CardName"         = EXCLUDED."CardName",
                "CardType"         = EXCLUDED."CardType",
                "GroupCode"        = EXCLUDED."GroupCode",
                "CntctPrsn"        = EXCLUDED."CntctPrsn",
                "Phone1"           = EXCLUDED."Phone1",
                "Phone2"           = EXCLUDED."Phone2",
                "Currency"         = EXCLUDED."Currency",
                "SlpCode"          = EXCLUDED."SlpCode",
                "VatLiable"        = EXCLUDED."VatLiable",
                "LicTradNum"       = EXCLUDED."LicTradNum",
                "FrozenFor"        = EXCLUDED."FrozenFor",
                "Balance"          = EXCLUDED."Balance",
                "CreditLine"       = EXCLUDED."CreditLine",
                "CreateDate"       = EXCLUDED."CreateDate",
                "CreateTS"         = EXCLUDED."CreateTS",
                "CreateTSNorm"     = EXCLUDED."CreateTSNorm",
                "UpdateDate"       = EXCLUDED."UpdateDate",
                "UpdateTS"         = EXCLUDED."UpdateTS",
                "UpdateTSNorm"     = EXCLUDED."UpdateTSNorm",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_ocrd".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_ocrd"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_ocrd"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_ocrd"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapOcrd(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Items (OITM) ──────────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertItemsAsync(
        string companyId, IEnumerable<SapOitmRow> rows, CancellationToken ct)
    {
        // SapOitmRow has many extra fields (FrgnName, CstGrpCode, InvntryUom, etc.)
        // not present in the raw.sap_oitm DDL — silently ignored by Dapper.
        // raw.sap_oitm has MinLevel/MaxLevel which are not in the DTO — omitted from INSERT (nullable).
        // SapOitmRow.ItmsGrpCod is string? but DB column is INTEGER — see MapOitm for safe conversion.
        const string sql = """
            INSERT INTO "raw"."sap_oitm" (
                company_id, "ItemCode", "ItemName", "ItmsGrpCod",
                "OnHand", "IsCommited", "OnOrder", "AvgPrice", "LastPurPrc",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @ItemCode, @ItemName, @ItmsGrpCod,
                @OnHand, @IsCommited, @OnOrder, @AvgPrice, @LastPurPrc,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "ItemCode") DO UPDATE SET
                "ItemName"         = EXCLUDED."ItemName",
                "ItmsGrpCod"       = EXCLUDED."ItmsGrpCod",
                "OnHand"           = EXCLUDED."OnHand",
                "IsCommited"       = EXCLUDED."IsCommited",
                "OnOrder"          = EXCLUDED."OnOrder",
                "AvgPrice"         = EXCLUDED."AvgPrice",
                "LastPurPrc"       = EXCLUDED."LastPurPrc",
                "CreateDate"       = EXCLUDED."CreateDate",
                "CreateTS"         = EXCLUDED."CreateTS",
                "CreateTSNorm"     = EXCLUDED."CreateTSNorm",
                "UpdateDate"       = EXCLUDED."UpdateDate",
                "UpdateTS"         = EXCLUDED."UpdateTS",
                "UpdateTSNorm"     = EXCLUDED."UpdateTSNorm",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oitm".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oitm"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oitm"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oitm"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapOitm(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Salespersons (OSLP) ────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalespersonsAsync(
        string companyId, IEnumerable<SapOslpRow> rows, CancellationToken ct)
    {
        // SapOslpRow has Commission, Email, Mobile, Telephone, Active, GroupCode —
        // not in the raw.sap_oslp DDL — silently ignored by Dapper.
        const string sql = """
            INSERT INTO "raw"."sap_oslp" (
                company_id, "SlpCode", "SlpName",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @SlpCode, @SlpName,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "SlpCode") DO UPDATE SET
                "SlpName"          = EXCLUDED."SlpName",
                "CreateDate"       = EXCLUDED."CreateDate",
                "CreateTS"         = EXCLUDED."CreateTS",
                "CreateTSNorm"     = EXCLUDED."CreateTSNorm",
                "UpdateDate"       = EXCLUDED."UpdateDate",
                "UpdateTS"         = EXCLUDED."UpdateTS",
                "UpdateTSNorm"     = EXCLUDED."UpdateTSNorm",
                source_hash_hex    = EXCLUDED.source_hash_hex,
                extraction_run_id  = EXCLUDED.extraction_run_id,
                batch_id           = EXCLUDED.batch_id,
                extracted_at_utc   = EXCLUDED.extracted_at_utc,
                ingestion_mode     = EXCLUDED.ingestion_mode,
                raw_updated_at_utc = NOW()
            WHERE
                "raw"."sap_oslp".source_hash_hex != EXCLUDED.source_hash_hex
                AND (
                    EXCLUDED."UpdateDate" > "raw"."sap_oslp"."UpdateDate"
                    OR (
                        EXCLUDED."UpdateDate" = "raw"."sap_oslp"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm", '000000')
                            >= COALESCE("raw"."sap_oslp"."UpdateTSNorm", '000000')
                    )
                )
            RETURNING (xmax = 0)::int AS is_insert;
            """;

        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapOslp(companyId, r));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Purchase Orders (OPOR) ────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertPurchaseOrdersAsync(
        string companyId, IEnumerable<SapOporRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_opor" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "DocCur",
                "DocStatus", "Cancelled", "SlpCode", "ObjType", "DocType", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @DocCur,
                @DocStatus, @Cancelled, @SlpCode, @ObjType, @DocType, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "DocDueDate" = EXCLUDED."DocDueDate", "CardCode" = EXCLUDED."CardCode",
                "CardName" = EXCLUDED."CardName", "DocTotal" = EXCLUDED."DocTotal",
                "DocTotalSy" = EXCLUDED."DocTotalSy", "VatSum" = EXCLUDED."VatSum",
                "DocCur" = EXCLUDED."DocCur", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "SlpCode" = EXCLUDED."SlpCode",
                "ObjType" = EXCLUDED."ObjType", "DocType" = EXCLUDED."DocType",
                "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_opor".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_opor"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_opor"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_opor"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapDocHeader(companyId, r.DocEntry, r.DocNum, r.DocDate, r.DocDueDate, r.CardCode, r.CardName, r.DocTotal, r.DocTotalSy, r.VatSum, r.DocCur, r.DocStatus, r.Cancelled, r.SlpCode, r.ObjType, r.DocType, r.Comments, r.CreateDate, r.CreateTS, r.CreateTSNorm, r.UpdateDate, r.UpdateTS, r.UpdateTSNorm, r.SourceHashHex, r.ExtractionRunId, r.BatchId, r.ExtractedAtUtc, r.IngestionMode));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Purchase Receipts / Goods Receipts (OPDN) ─────────────────────────────

    public async Task<(int inserted, int updated)> UpsertPurchaseReceiptsAsync(
        string companyId, IEnumerable<SapOpdnRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_opdn" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "DocCur",
                "DocStatus", "Cancelled", "SlpCode", "ObjType", "DocType", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @DocCur,
                @DocStatus, @Cancelled, @SlpCode, @ObjType, @DocType, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "DocDueDate" = EXCLUDED."DocDueDate", "CardCode" = EXCLUDED."CardCode",
                "CardName" = EXCLUDED."CardName", "DocTotal" = EXCLUDED."DocTotal",
                "DocTotalSy" = EXCLUDED."DocTotalSy", "VatSum" = EXCLUDED."VatSum",
                "DocCur" = EXCLUDED."DocCur", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "SlpCode" = EXCLUDED."SlpCode",
                "ObjType" = EXCLUDED."ObjType", "DocType" = EXCLUDED."DocType",
                "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_opdn".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_opdn"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_opdn"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_opdn"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapDocHeader(companyId, r.DocEntry, r.DocNum, r.DocDate, r.DocDueDate, r.CardCode, r.CardName, r.DocTotal, r.DocTotalSy, r.VatSum, r.DocCur, r.DocStatus, r.Cancelled, r.SlpCode, r.ObjType, r.DocType, r.Comments, r.CreateDate, r.CreateTS, r.CreateTSNorm, r.UpdateDate, r.UpdateTS, r.UpdateTSNorm, r.SourceHashHex, r.ExtractionRunId, r.BatchId, r.ExtractedAtUtc, r.IngestionMode));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Purchase Invoices (OPCH) ───────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertPurchaseInvoicesAsync(
        string companyId, IEnumerable<SapOpchRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_opch" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "DocCur",
                "DocStatus", "Cancelled", "SlpCode", "ObjType", "DocType", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @DocCur,
                @DocStatus, @Cancelled, @SlpCode, @ObjType, @DocType, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "DocDueDate" = EXCLUDED."DocDueDate", "CardCode" = EXCLUDED."CardCode",
                "CardName" = EXCLUDED."CardName", "DocTotal" = EXCLUDED."DocTotal",
                "DocTotalSy" = EXCLUDED."DocTotalSy", "VatSum" = EXCLUDED."VatSum",
                "DocCur" = EXCLUDED."DocCur", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "SlpCode" = EXCLUDED."SlpCode",
                "ObjType" = EXCLUDED."ObjType", "DocType" = EXCLUDED."DocType",
                "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_opch".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_opch"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_opch"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_opch"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapDocHeader(companyId, r.DocEntry, r.DocNum, r.DocDate, r.DocDueDate, r.CardCode, r.CardName, r.DocTotal, r.DocTotalSy, r.VatSum, r.DocCur, r.DocStatus, r.Cancelled, r.SlpCode, r.ObjType, r.DocType, r.Comments, r.CreateDate, r.CreateTS, r.CreateTSNorm, r.UpdateDate, r.UpdateTS, r.UpdateTSNorm, r.SourceHashHex, r.ExtractionRunId, r.BatchId, r.ExtractedAtUtc, r.IngestionMode));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Item Warehouse Levels (OITW) ───────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertItemWarehousesAsync(
        string companyId, IEnumerable<SapOitwRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_oitw" (
                company_id, "ItemCode", "WhsCode", "OnHand", "IsCommited", "OnOrder",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @ItemCode, @WhsCode, @OnHand, @IsCommited, @OnOrder,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "ItemCode", "WhsCode") DO UPDATE SET
                "OnHand" = EXCLUDED."OnHand", "IsCommited" = EXCLUDED."IsCommited",
                "OnOrder" = EXCLUDED."OnOrder",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_oitw".source_hash_hex != EXCLUDED.source_hash_hex
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("ItemCode",          r.ItemCode);
            p.Add("WhsCode",           r.WhsCode);
            p.Add("OnHand",            r.OnHand);
            p.Add("IsCommited",        r.IsCommited);
            p.Add("OnOrder",           r.OnOrder);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);
            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Sales Orders (ORDR) ────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalesOrdersAsync(
        string companyId, IEnumerable<SapOrdrRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_ordr" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "DocCur",
                "DocStatus", "Cancelled", "SlpCode", "ObjType", "DocType", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @DocCur,
                @DocStatus, @Cancelled, @SlpCode, @ObjType, @DocType, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "DocDueDate" = EXCLUDED."DocDueDate", "CardCode" = EXCLUDED."CardCode",
                "CardName" = EXCLUDED."CardName", "DocTotal" = EXCLUDED."DocTotal",
                "DocTotalSy" = EXCLUDED."DocTotalSy", "VatSum" = EXCLUDED."VatSum",
                "DocCur" = EXCLUDED."DocCur", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "SlpCode" = EXCLUDED."SlpCode",
                "ObjType" = EXCLUDED."ObjType", "DocType" = EXCLUDED."DocType",
                "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_ordr".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_ordr"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_ordr"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_ordr"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapDocHeader(companyId, r.DocEntry, r.DocNum, r.DocDate, r.DocDueDate, r.CardCode, r.CardName, r.DocTotal, r.DocTotalSy, r.VatSum, r.DocCur, r.DocStatus, r.Cancelled, r.SlpCode, r.ObjType, r.DocType, r.Comments, r.CreateDate, r.CreateTS, r.CreateTSNorm, r.UpdateDate, r.UpdateTS, r.UpdateTSNorm, r.SourceHashHex, r.ExtractionRunId, r.BatchId, r.ExtractedAtUtc, r.IngestionMode));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Delivery Notes (ODLN) ─────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertDeliveriesAsync(
        string companyId, IEnumerable<SapOdlnRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_odln" (
                company_id, "DocEntry", "DocNum", "DocDate", "DocDueDate",
                "CardCode", "CardName", "DocTotal", "DocTotalSy", "VatSum", "DocCur",
                "DocStatus", "Cancelled", "SlpCode", "ObjType", "DocType", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate,
                @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @DocCur,
                @DocStatus, @Cancelled, @SlpCode, @ObjType, @DocType, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "DocDueDate" = EXCLUDED."DocDueDate", "CardCode" = EXCLUDED."CardCode",
                "CardName" = EXCLUDED."CardName", "DocTotal" = EXCLUDED."DocTotal",
                "DocTotalSy" = EXCLUDED."DocTotalSy", "VatSum" = EXCLUDED."VatSum",
                "DocCur" = EXCLUDED."DocCur", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "SlpCode" = EXCLUDED."SlpCode",
                "ObjType" = EXCLUDED."ObjType", "DocType" = EXCLUDED."DocType",
                "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_odln".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_odln"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_odln"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_odln"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var result = await conn.QueryAsync<int>(sql, MapDocHeader(companyId, r.DocEntry, r.DocNum, r.DocDate, r.DocDueDate, r.CardCode, r.CardName, r.DocTotal, r.DocTotalSy, r.VatSum, r.DocCur, r.DocStatus, r.Cancelled, r.SlpCode, r.ObjType, r.DocType, r.Comments, r.CreateDate, r.CreateTS, r.CreateTSNorm, r.UpdateDate, r.UpdateTS, r.UpdateTSNorm, r.SourceHashHex, r.ExtractionRunId, r.BatchId, r.ExtractedAtUtc, r.IngestionMode));
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Stock Transfers (OWTR) ─────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertStockTransfersAsync(
        string companyId, IEnumerable<SapOwtrRow> rows, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "raw"."sap_owtr" (
                company_id, "DocEntry", "DocNum", "DocDate",
                "FromWarehouse", "ToWarehouse", "DocTotal", "DocStatus", "Cancelled", "Comments",
                "CreateDate", "CreateTS", "CreateTSNorm",
                "UpdateDate", "UpdateTS", "UpdateTSNorm",
                source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode,
                raw_created_at_utc
            )
            VALUES (
                @company_id, @DocEntry, @DocNum, @DocDate,
                @FromWarehouse, @ToWarehouse, @DocTotal, @DocStatus, @Cancelled, @Comments,
                @CreateDate, @CreateTS, @CreateTSNorm,
                @UpdateDate, @UpdateTS, @UpdateTSNorm,
                @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode,
                NOW()
            )
            ON CONFLICT (company_id, "DocEntry") DO UPDATE SET
                "DocNum" = EXCLUDED."DocNum", "DocDate" = EXCLUDED."DocDate",
                "FromWarehouse" = EXCLUDED."FromWarehouse", "ToWarehouse" = EXCLUDED."ToWarehouse",
                "DocTotal" = EXCLUDED."DocTotal", "DocStatus" = EXCLUDED."DocStatus",
                "Cancelled" = EXCLUDED."Cancelled", "Comments" = EXCLUDED."Comments",
                "CreateDate" = EXCLUDED."CreateDate", "CreateTS" = EXCLUDED."CreateTS",
                "CreateTSNorm" = EXCLUDED."CreateTSNorm",
                "UpdateDate" = EXCLUDED."UpdateDate", "UpdateTS" = EXCLUDED."UpdateTS",
                "UpdateTSNorm" = EXCLUDED."UpdateTSNorm",
                source_hash_hex = EXCLUDED.source_hash_hex,
                extraction_run_id = EXCLUDED.extraction_run_id,
                batch_id = EXCLUDED.batch_id, extracted_at_utc = EXCLUDED.extracted_at_utc,
                ingestion_mode = EXCLUDED.ingestion_mode, raw_updated_at_utc = NOW()
            WHERE "raw"."sap_owtr".source_hash_hex != EXCLUDED.source_hash_hex
                AND (EXCLUDED."UpdateDate" > "raw"."sap_owtr"."UpdateDate"
                    OR (EXCLUDED."UpdateDate" = "raw"."sap_owtr"."UpdateDate"
                        AND COALESCE(EXCLUDED."UpdateTSNorm",'000000') >= COALESCE("raw"."sap_owtr"."UpdateTSNorm",'000000')))
            RETURNING (xmax = 0)::int AS is_insert;
            """;
        var rowList = rows.ToList();
        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var allResults = new List<int>(rowList.Count);
        foreach (var r in rowList)
        {
            var p = new DynamicParameters();
            p.Add("company_id",        companyId);
            p.Add("DocEntry",          r.DocEntry);
            p.Add("DocNum",            r.DocNum);
            p.Add("DocDate",           r.DocDate);
            p.Add("FromWarehouse",     r.FromWarehouse);
            p.Add("ToWarehouse",       r.ToWarehouse);
            p.Add("DocTotal",          r.DocTotal);
            p.Add("DocStatus",         r.DocStatus);
            p.Add("Cancelled",         r.Cancelled);
            p.Add("Comments",          r.Comments);
            p.Add("CreateDate",        r.CreateDate);
            p.Add("CreateTS",          r.CreateTS);
            p.Add("CreateTSNorm",      r.CreateTSNorm);
            p.Add("UpdateDate",        r.UpdateDate);
            p.Add("UpdateTS",          r.UpdateTS);
            p.Add("UpdateTSNorm",      r.UpdateTSNorm);
            p.Add("source_hash_hex",   r.SourceHashHex);
            p.Add("extraction_run_id", r.ExtractionRunId);
            p.Add("batch_id",          r.BatchId);
            p.Add("extracted_at_utc",  r.ExtractedAtUtc);
            p.Add("ingestion_mode",    r.IngestionMode);
            var result = await conn.QueryAsync<int>(sql, p);
            allResults.AddRange(result);
        }
        return CountResults(allResults);
    }

    // ── Cross-table helpers ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<int>> GetExistingCreditMemoDocEntriesAsync(
        string companyId, IEnumerable<int> docEntries, CancellationToken ct)
    {
        var entries = docEntries.Distinct().ToList();
        if (entries.Count == 0) return [];

        // PostgreSQL uses = ANY(@array) instead of IN @list.
        // Npgsql maps int[] to a PostgreSQL integer array automatically.
        const string sql = """
            SELECT "DocEntry"
            FROM "raw"."sap_orin"
            WHERE company_id = @company_id
              AND "DocEntry" = ANY(@doc_entries);
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var result = await conn.QueryAsync<int>(
            new CommandDefinition(sql, new { company_id = companyId, doc_entries = entries.ToArray() }, cancellationToken: ct));
        return result.ToList().AsReadOnly();
    }

    // ── Parameter mappers ─────────────────────────────────────────────────────

    private static DynamicParameters MapOinv(string companyId, SapOinvRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("DocEntry",          r.DocEntry);
        p.Add("DocNum",            r.DocNum);
        p.Add("DocDate",           r.DocDate);
        p.Add("DocDueDate",        r.DocDueDate);
        p.Add("TaxDate",           r.TaxDate);
        p.Add("CardCode",          r.CardCode);
        p.Add("CardName",          r.CardName);
        p.Add("DocTotal",          r.DocTotal);
        p.Add("DocTotalSy",        r.DocTotalSy);
        p.Add("VatSum",            r.VatSum);
        p.Add("PaidToDate",        r.PaidToDate);
        p.Add("DocCur",            r.DocCur);
        p.Add("DocStatus",         r.DocStatus);
        p.Add("SlpCode",           r.SlpCode);
        p.Add("Comments",          r.Comments);
        p.Add("ObjType",           r.ObjType);
        p.Add("DocType",           r.DocType);
        p.Add("Cancelled",         r.Cancelled);
        p.Add("CreateDate",        r.CreateDate);
        p.Add("CreateTS",          r.CreateTS);
        p.Add("CreateTSNorm",      r.CreateTSNorm);
        p.Add("UpdateDate",        r.UpdateDate);
        p.Add("UpdateTS",          r.UpdateTS);
        p.Add("UpdateTSNorm",      r.UpdateTSNorm);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapInv1(string companyId, SapInv1Row r)
    {
        // SlpCode, WhsCode, UomCode, DiscPrcnt, GrossBuyPr are in the DTO but not in
        // raw.sap_inv1 DDL — adding them would not cause errors (Dapper ignores unused params),
        // but they are omitted here for clarity.
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("DocEntry",          r.DocEntry);
        p.Add("LineNum",           r.LineNum);
        p.Add("ItemCode",          r.ItemCode);
        p.Add("Dscription",        r.Dscription);
        p.Add("Quantity",          r.Quantity);
        p.Add("Price",             r.Price);
        p.Add("Currency",          r.Currency);
        p.Add("LineTotal",         r.LineTotal);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOrin(string companyId, SapOrinRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("DocEntry",          r.DocEntry);
        p.Add("DocNum",            r.DocNum);
        p.Add("DocDate",           r.DocDate);
        p.Add("DocDueDate",        r.DocDueDate);
        p.Add("TaxDate",           r.TaxDate);
        p.Add("CardCode",          r.CardCode);
        p.Add("CardName",          r.CardName);
        p.Add("DocTotal",          r.DocTotal);
        p.Add("DocTotalSy",        r.DocTotalSy);
        p.Add("VatSum",            r.VatSum);
        p.Add("DocCur",            r.DocCur);
        p.Add("DocStatus",         r.DocStatus);
        p.Add("SlpCode",           r.SlpCode);
        p.Add("Comments",          r.Comments);
        p.Add("ObjType",           r.ObjType);
        p.Add("DocType",           r.DocType);
        p.Add("Cancelled",         r.Cancelled);
        p.Add("CreateDate",        r.CreateDate);
        p.Add("CreateTS",          r.CreateTS);
        p.Add("CreateTSNorm",      r.CreateTSNorm);
        p.Add("UpdateDate",        r.UpdateDate);
        p.Add("UpdateTS",          r.UpdateTS);
        p.Add("UpdateTSNorm",      r.UpdateTSNorm);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapRin1(string companyId, SapRin1Row r)
    {
        // Same reasoning as MapInv1 — extra DTO fields omitted, hash-only guard used.
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("DocEntry",          r.DocEntry);
        p.Add("LineNum",           r.LineNum);
        p.Add("ItemCode",          r.ItemCode);
        p.Add("Dscription",        r.Dscription);
        p.Add("Quantity",          r.Quantity);
        p.Add("Price",             r.Price);
        p.Add("Currency",          r.Currency);
        p.Add("LineTotal",         r.LineTotal);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOcrd(string companyId, SapOcrdRow r)
    {
        // Fax, EMail, Country, City, ZipCode exist in DTO but not in raw.sap_ocrd DDL.
        // Not added here — omitting keeps the mapper aligned with the actual schema.
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("CardCode",          r.CardCode);
        p.Add("CardName",          r.CardName);
        p.Add("CardType",          r.CardType);
        p.Add("GroupCode",         r.GroupCode);
        p.Add("CntctPrsn",         r.CntctPrsn);
        p.Add("Phone1",            r.Phone1);
        p.Add("Phone2",            r.Phone2);
        p.Add("Currency",          r.Currency);
        p.Add("SlpCode",           r.SlpCode);
        p.Add("VatLiable",         r.VatLiable);
        p.Add("LicTradNum",        r.LicTradNum);
        p.Add("FrozenFor",         r.FrozenFor);
        p.Add("Balance",           r.Balance);
        p.Add("CreditLine",        r.CreditLine);
        p.Add("CreateDate",        r.CreateDate);
        p.Add("CreateTS",          r.CreateTS);
        p.Add("CreateTSNorm",      r.CreateTSNorm);
        p.Add("UpdateDate",        r.UpdateDate);
        p.Add("UpdateTS",          r.UpdateTS);
        p.Add("UpdateTSNorm",      r.UpdateTSNorm);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOitm(string companyId, SapOitmRow r)
    {
        // ItmsGrpCod: DTO is string?, DB column is INTEGER.
        // Safe conversion — null propagated on parse failure rather than throwing.
        int? itmsGrpCod = int.TryParse(r.ItmsGrpCod, out var grp) ? grp : null;

        // FrgnName, CstGrpCode, InvntryUom, BuyUnitMsr, SalUnitMsr, ManSerNum, ItemType, SWW, Canceled
        // exist in the DTO but not in raw.sap_oitm DDL — omitted from mapper.
        // MinLevel, MaxLevel exist in DDL but not in DTO — omitted from INSERT (nullable, default NULL).
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("ItemCode",          r.ItemCode);
        p.Add("ItemName",          r.ItemName);
        p.Add("ItmsGrpCod",        itmsGrpCod);
        p.Add("OnHand",            r.OnHand);
        p.Add("IsCommited",        r.IsCommited);
        p.Add("OnOrder",           r.OnOrder);
        p.Add("AvgPrice",          r.AvgPrice);
        p.Add("LastPurPrc",        r.LastPurPrc);
        p.Add("CreateDate",        r.CreateDate);
        p.Add("CreateTS",          r.CreateTS);
        p.Add("CreateTSNorm",      r.CreateTSNorm);
        p.Add("UpdateDate",        r.UpdateDate);
        p.Add("UpdateTS",          r.UpdateTS);
        p.Add("UpdateTSNorm",      r.UpdateTSNorm);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOslp(string companyId, SapOslpRow r)
    {
        // Commission, Email, Mobile, Telephone, Active, GroupCode exist in DTO
        // but not in raw.sap_oslp DDL — omitted from mapper.
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("SlpCode",           r.SlpCode);
        p.Add("SlpName",           r.SlpName);
        p.Add("CreateDate",        r.CreateDate);
        p.Add("CreateTS",          r.CreateTS);
        p.Add("CreateTSNorm",      r.CreateTSNorm);
        p.Add("UpdateDate",        r.UpdateDate);
        p.Add("UpdateTS",          r.UpdateTS);
        p.Add("UpdateTSNorm",      r.UpdateTSNorm);
        p.Add("source_hash_hex",   r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id",          r.BatchId);
        p.Add("extracted_at_utc",  r.ExtractedAtUtc);
        p.Add("ingestion_mode",    r.IngestionMode);
        return p;
    }

    // Shared mapper for document-header objects (OPOR, OPDN, OPCH, ORDR, ODLN)
    private static DynamicParameters MapDocHeader(
        string companyId, int docEntry, int docNum,
        DateTime? docDate, DateTime? docDueDate,
        string? cardCode, string? cardName,
        decimal? docTotal, decimal? docTotalSy, decimal? vatSum, string? docCur,
        string? docStatus, string? cancelled, string? slpCode,
        string? objType, string? docType, string? comments,
        DateTime? createDate, string? createTS, string? createTSNorm,
        DateTime? updateDate, string? updateTS, string? updateTSNorm,
        string? sourceHashHex, string extractionRunId, string batchId,
        DateTime extractedAtUtc, string ingestionMode)
    {
        var p = new DynamicParameters();
        p.Add("company_id",        companyId);
        p.Add("DocEntry",          docEntry);
        p.Add("DocNum",            docNum);
        p.Add("DocDate",           docDate);
        p.Add("DocDueDate",        docDueDate);
        p.Add("CardCode",          cardCode);
        p.Add("CardName",          cardName);
        p.Add("DocTotal",          docTotal);
        p.Add("DocTotalSy",        docTotalSy);
        p.Add("VatSum",            vatSum);
        p.Add("DocCur",            docCur);
        p.Add("DocStatus",         docStatus);
        p.Add("Cancelled",         cancelled);
        p.Add("SlpCode",           slpCode);
        p.Add("ObjType",           objType);
        p.Add("DocType",           docType);
        p.Add("Comments",          comments);
        p.Add("CreateDate",        createDate);
        p.Add("CreateTS",          createTS);
        p.Add("CreateTSNorm",      createTSNorm);
        p.Add("UpdateDate",        updateDate);
        p.Add("UpdateTS",          updateTS);
        p.Add("UpdateTSNorm",      updateTSNorm);
        p.Add("source_hash_hex",   sourceHashHex);
        p.Add("extraction_run_id", extractionRunId);
        p.Add("batch_id",          batchId);
        p.Add("extracted_at_utc",  extractedAtUtc);
        p.Add("ingestion_mode",    ingestionMode);
        return p;
    }
}
