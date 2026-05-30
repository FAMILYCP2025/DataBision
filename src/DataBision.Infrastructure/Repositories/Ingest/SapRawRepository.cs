using Dapper;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using Npgsql; // Sprint 1C: SQL inside still T-SQL MERGE — rewrite pending
using Microsoft.Extensions.Logging;

namespace DataBision.Infrastructure.Repositories.Ingest;

/// <summary>
/// Dapper-based upsert for raw.sap_* tables using MERGE with temporal guard
/// (source_update_date, source_update_ts_norm). Separate from EF to avoid
/// change-tracking overhead on high-volume upserts.
/// </summary>
public sealed class SapRawRepository(string connectionString, ILogger<SapRawRepository> _logger)
    : ISapRawRepository
{
    // ── Sales Invoices (OINV) ──────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalesInvoicesAsync(
        string companyId, IEnumerable<SapOinvRow> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_oinv] AS tgt
USING (SELECT @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
              @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum, @PaidToDate,
              @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
              @CreateDate, @CreateTS, @CreateTSNorm,
              @UpdateDate, @UpdateTS, @UpdateTSNorm,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, DocEntry, DocNum, DocDate, DocDueDate, TaxDate,
               CardCode, CardName, DocTotal, DocTotalSy, VatSum, PaidToDate,
               DocCur, DocStatus, SlpCode, Comments, ObjType, DocType, Cancelled,
               CreateDate, CreateTS, CreateTSNorm,
               UpdateDate, UpdateTS, UpdateTSNorm,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.DocEntry = src.DocEntry)
WHEN MATCHED AND (
    tgt.source_hash_hex != src.source_hash_hex
    AND (
        src.UpdateDate > tgt.UpdateDate
        OR (src.UpdateDate = tgt.UpdateDate AND ISNULL(src.UpdateTSNorm,'000000') >= ISNULL(tgt.UpdateTSNorm,'000000'))
    )
) THEN UPDATE SET
    tgt.DocNum = src.DocNum, tgt.DocDate = src.DocDate, tgt.DocDueDate = src.DocDueDate, tgt.TaxDate = src.TaxDate,
    tgt.CardCode = src.CardCode, tgt.CardName = src.CardName,
    tgt.DocTotal = src.DocTotal, tgt.DocTotalSy = src.DocTotalSy, tgt.VatSum = src.VatSum, tgt.PaidToDate = src.PaidToDate,
    tgt.DocCur = src.DocCur, tgt.DocStatus = src.DocStatus, tgt.SlpCode = src.SlpCode, tgt.Comments = src.Comments,
    tgt.ObjType = src.ObjType, tgt.DocType = src.DocType, tgt.Cancelled = src.Cancelled,
    tgt.CreateDate = src.CreateDate, tgt.CreateTS = src.CreateTS, tgt.CreateTSNorm = src.CreateTSNorm,
    tgt.UpdateDate = src.UpdateDate, tgt.UpdateTS = src.UpdateTS, tgt.UpdateTSNorm = src.UpdateTSNorm,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, DocEntry, DocNum, DocDate, DocDueDate, TaxDate,
     CardCode, CardName, DocTotal, DocTotalSy, VatSum, PaidToDate,
     DocCur, DocStatus, SlpCode, Comments, ObjType, DocType, Cancelled,
     CreateDate, CreateTS, CreateTSNorm, UpdateDate, UpdateTS, UpdateTSNorm,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.DocEntry, src.DocNum, src.DocDate, src.DocDueDate, src.TaxDate,
     src.CardCode, src.CardName, src.DocTotal, src.DocTotalSy, src.VatSum, src.PaidToDate,
     src.DocCur, src.DocStatus, src.SlpCode, src.Comments, src.ObjType, src.DocType, src.Cancelled,
     src.CreateDate, src.CreateTS, src.CreateTSNorm, src.UpdateDate, src.UpdateTS, src.UpdateTSNorm,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapOinv(companyId, r)), ct);
    }

    public async Task<(int inserted, int updated)> UpsertSalesInvoiceLinesAsync(
        string companyId, IEnumerable<SapInv1Row> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_inv1] AS tgt
USING (SELECT @company_id, @DocEntry, @LineNum, @ItemCode, @Dscription, @Quantity,
              @Price, @LineTotal, @Currency, @SlpCode, @WhsCode, @UomCode, @DiscPrcnt, @GrossBuyPr,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, DocEntry, LineNum, ItemCode, Dscription, Quantity,
               Price, LineTotal, Currency, SlpCode, WhsCode, UomCode, DiscPrcnt, GrossBuyPr,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.DocEntry = src.DocEntry AND tgt.LineNum = src.LineNum)
WHEN MATCHED AND tgt.source_hash_hex != src.source_hash_hex THEN UPDATE SET
    tgt.ItemCode = src.ItemCode, tgt.Dscription = src.Dscription, tgt.Quantity = src.Quantity,
    tgt.Price = src.Price, tgt.LineTotal = src.LineTotal, tgt.Currency = src.Currency,
    tgt.SlpCode = src.SlpCode, tgt.WhsCode = src.WhsCode, tgt.UomCode = src.UomCode,
    tgt.DiscPrcnt = src.DiscPrcnt, tgt.GrossBuyPr = src.GrossBuyPr,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, DocEntry, LineNum, ItemCode, Dscription, Quantity, Price, LineTotal,
     Currency, SlpCode, WhsCode, UomCode, DiscPrcnt, GrossBuyPr,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.DocEntry, src.LineNum, src.ItemCode, src.Dscription, src.Quantity,
     src.Price, src.LineTotal, src.Currency, src.SlpCode, src.WhsCode, src.UomCode,
     src.DiscPrcnt, src.GrossBuyPr,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapInv1(companyId, r)), ct);
    }

    // ── Credit Memos (ORIN) ────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertCreditMemosAsync(
        string companyId, IEnumerable<SapOrinRow> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_orin] AS tgt
USING (SELECT @company_id, @DocEntry, @DocNum, @DocDate, @DocDueDate, @TaxDate,
              @CardCode, @CardName, @DocTotal, @DocTotalSy, @VatSum,
              @DocCur, @DocStatus, @SlpCode, @Comments, @ObjType, @DocType, @Cancelled,
              @CreateDate, @CreateTS, @CreateTSNorm,
              @UpdateDate, @UpdateTS, @UpdateTSNorm,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, DocEntry, DocNum, DocDate, DocDueDate, TaxDate,
               CardCode, CardName, DocTotal, DocTotalSy, VatSum,
               DocCur, DocStatus, SlpCode, Comments, ObjType, DocType, Cancelled,
               CreateDate, CreateTS, CreateTSNorm,
               UpdateDate, UpdateTS, UpdateTSNorm,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.DocEntry = src.DocEntry)
WHEN MATCHED AND (
    tgt.source_hash_hex != src.source_hash_hex
    AND (
        src.UpdateDate > tgt.UpdateDate
        OR (src.UpdateDate = tgt.UpdateDate AND ISNULL(src.UpdateTSNorm,'000000') >= ISNULL(tgt.UpdateTSNorm,'000000'))
    )
) THEN UPDATE SET
    tgt.DocNum = src.DocNum, tgt.DocDate = src.DocDate, tgt.DocDueDate = src.DocDueDate, tgt.TaxDate = src.TaxDate,
    tgt.CardCode = src.CardCode, tgt.CardName = src.CardName,
    tgt.DocTotal = src.DocTotal, tgt.DocTotalSy = src.DocTotalSy, tgt.VatSum = src.VatSum,
    tgt.DocCur = src.DocCur, tgt.DocStatus = src.DocStatus, tgt.SlpCode = src.SlpCode, tgt.Comments = src.Comments,
    tgt.ObjType = src.ObjType, tgt.DocType = src.DocType, tgt.Cancelled = src.Cancelled,
    tgt.CreateDate = src.CreateDate, tgt.CreateTS = src.CreateTS, tgt.CreateTSNorm = src.CreateTSNorm,
    tgt.UpdateDate = src.UpdateDate, tgt.UpdateTS = src.UpdateTS, tgt.UpdateTSNorm = src.UpdateTSNorm,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, DocEntry, DocNum, DocDate, DocDueDate, TaxDate,
     CardCode, CardName, DocTotal, DocTotalSy, VatSum,
     DocCur, DocStatus, SlpCode, Comments, ObjType, DocType, Cancelled,
     CreateDate, CreateTS, CreateTSNorm, UpdateDate, UpdateTS, UpdateTSNorm,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.DocEntry, src.DocNum, src.DocDate, src.DocDueDate, src.TaxDate,
     src.CardCode, src.CardName, src.DocTotal, src.DocTotalSy, src.VatSum,
     src.DocCur, src.DocStatus, src.SlpCode, src.Comments, src.ObjType, src.DocType, src.Cancelled,
     src.CreateDate, src.CreateTS, src.CreateTSNorm, src.UpdateDate, src.UpdateTS, src.UpdateTSNorm,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapOrin(companyId, r)), ct);
    }

    public async Task<(int inserted, int updated)> UpsertCreditMemoLinesAsync(
        string companyId, IEnumerable<SapRin1Row> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_rin1] AS tgt
USING (SELECT @company_id, @DocEntry, @LineNum, @ItemCode, @Dscription, @Quantity,
              @Price, @LineTotal, @Currency, @SlpCode, @WhsCode, @UomCode, @DiscPrcnt,
              @BaseRef, @BaseEntry, @BaseLine, @BaseType,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, DocEntry, LineNum, ItemCode, Dscription, Quantity,
               Price, LineTotal, Currency, SlpCode, WhsCode, UomCode, DiscPrcnt,
               BaseRef, BaseEntry, BaseLine, BaseType,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.DocEntry = src.DocEntry AND tgt.LineNum = src.LineNum)
WHEN MATCHED AND tgt.source_hash_hex != src.source_hash_hex THEN UPDATE SET
    tgt.ItemCode = src.ItemCode, tgt.Dscription = src.Dscription, tgt.Quantity = src.Quantity,
    tgt.Price = src.Price, tgt.LineTotal = src.LineTotal, tgt.Currency = src.Currency,
    tgt.SlpCode = src.SlpCode, tgt.WhsCode = src.WhsCode, tgt.UomCode = src.UomCode,
    tgt.DiscPrcnt = src.DiscPrcnt,
    tgt.BaseRef = src.BaseRef, tgt.BaseEntry = src.BaseEntry, tgt.BaseLine = src.BaseLine, tgt.BaseType = src.BaseType,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, DocEntry, LineNum, ItemCode, Dscription, Quantity, Price, LineTotal,
     Currency, SlpCode, WhsCode, UomCode, DiscPrcnt, BaseRef, BaseEntry, BaseLine, BaseType,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.DocEntry, src.LineNum, src.ItemCode, src.Dscription, src.Quantity,
     src.Price, src.LineTotal, src.Currency, src.SlpCode, src.WhsCode, src.UomCode,
     src.DiscPrcnt, src.BaseRef, src.BaseEntry, src.BaseLine, src.BaseType,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapRin1(companyId, r)), ct);
    }

    // ── Customers (OCRD) ──────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertCustomersAsync(
        string companyId, IEnumerable<SapOcrdRow> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_ocrd] AS tgt
USING (SELECT @company_id, @CardCode, @CardName, @CardType, @GroupCode, @CntctPrsn,
              @Phone1, @Phone2, @Fax, @EMail, @Country, @City, @ZipCode,
              @Currency, @SlpCode, @VatLiable, @LicTradNum, @FrozenFor, @Balance, @CreditLine,
              @CreateDate, @CreateTS, @CreateTSNorm,
              @UpdateDate, @UpdateTS, @UpdateTSNorm,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, CardCode, CardName, CardType, GroupCode, CntctPrsn,
               Phone1, Phone2, Fax, EMail, Country, City, ZipCode,
               Currency, SlpCode, VatLiable, LicTradNum, FrozenFor, Balance, CreditLine,
               CreateDate, CreateTS, CreateTSNorm,
               UpdateDate, UpdateTS, UpdateTSNorm,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.CardCode = src.CardCode)
WHEN MATCHED AND (
    tgt.source_hash_hex != src.source_hash_hex
    AND (
        src.UpdateDate > tgt.UpdateDate
        OR (src.UpdateDate = tgt.UpdateDate AND ISNULL(src.UpdateTSNorm,'000000') >= ISNULL(tgt.UpdateTSNorm,'000000'))
    )
) THEN UPDATE SET
    tgt.CardName = src.CardName, tgt.CardType = src.CardType, tgt.GroupCode = src.GroupCode,
    tgt.CntctPrsn = src.CntctPrsn, tgt.Phone1 = src.Phone1, tgt.Phone2 = src.Phone2,
    tgt.Fax = src.Fax, tgt.EMail = src.EMail, tgt.Country = src.Country,
    tgt.City = src.City, tgt.ZipCode = src.ZipCode, tgt.Currency = src.Currency,
    tgt.SlpCode = src.SlpCode, tgt.VatLiable = src.VatLiable, tgt.LicTradNum = src.LicTradNum, tgt.FrozenFor = src.FrozenFor,
    tgt.Balance = src.Balance, tgt.CreditLine = src.CreditLine,
    tgt.CreateDate = src.CreateDate, tgt.CreateTS = src.CreateTS, tgt.CreateTSNorm = src.CreateTSNorm,
    tgt.UpdateDate = src.UpdateDate, tgt.UpdateTS = src.UpdateTS, tgt.UpdateTSNorm = src.UpdateTSNorm,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, CardCode, CardName, CardType, GroupCode, CntctPrsn,
     Phone1, Phone2, Fax, EMail, Country, City, ZipCode,
     Currency, SlpCode, VatLiable, LicTradNum, FrozenFor, Balance, CreditLine,
     CreateDate, CreateTS, CreateTSNorm, UpdateDate, UpdateTS, UpdateTSNorm,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.CardCode, src.CardName, src.CardType, src.GroupCode, src.CntctPrsn,
     src.Phone1, src.Phone2, src.Fax, src.EMail, src.Country, src.City, src.ZipCode,
     src.Currency, src.SlpCode, src.VatLiable, src.LicTradNum, src.FrozenFor, src.Balance, src.CreditLine,
     src.CreateDate, src.CreateTS, src.CreateTSNorm, src.UpdateDate, src.UpdateTS, src.UpdateTSNorm,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapOcrd(companyId, r)), ct);
    }

    // ── Items (OITM) ──────────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertItemsAsync(
        string companyId, IEnumerable<SapOitmRow> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_oitm] AS tgt
USING (SELECT @company_id, @ItemCode, @ItemName, @FrgnName, @ItmsGrpCod, @CstGrpCode,
              @InvntryUom, @BuyUnitMsr, @SalUnitMsr, @ManSerNum, @OnHand, @IsCommited,
              @OnOrder, @AvgPrice, @LastPurPrc, @ItemType, @SWW, @Canceled,
              @CreateDate, @CreateTS, @CreateTSNorm,
              @UpdateDate, @UpdateTS, @UpdateTSNorm,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, ItemCode, ItemName, FrgnName, ItmsGrpCod, CstGrpCode,
               InvntryUom, BuyUnitMsr, SalUnitMsr, ManSerNum, OnHand, IsCommited,
               OnOrder, AvgPrice, LastPurPrc, ItemType, SWW, Canceled,
               CreateDate, CreateTS, CreateTSNorm,
               UpdateDate, UpdateTS, UpdateTSNorm,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.ItemCode = src.ItemCode)
WHEN MATCHED AND (
    tgt.source_hash_hex != src.source_hash_hex
    AND (
        src.UpdateDate > tgt.UpdateDate
        OR (src.UpdateDate = tgt.UpdateDate AND ISNULL(src.UpdateTSNorm,'000000') >= ISNULL(tgt.UpdateTSNorm,'000000'))
    )
) THEN UPDATE SET
    tgt.ItemName = src.ItemName, tgt.FrgnName = src.FrgnName, tgt.ItmsGrpCod = src.ItmsGrpCod,
    tgt.CstGrpCode = src.CstGrpCode, tgt.InvntryUom = src.InvntryUom,
    tgt.BuyUnitMsr = src.BuyUnitMsr, tgt.SalUnitMsr = src.SalUnitMsr,
    tgt.ManSerNum = src.ManSerNum, tgt.OnHand = src.OnHand, tgt.IsCommited = src.IsCommited,
    tgt.OnOrder = src.OnOrder, tgt.AvgPrice = src.AvgPrice, tgt.LastPurPrc = src.LastPurPrc,
    tgt.ItemType = src.ItemType, tgt.SWW = src.SWW, tgt.Canceled = src.Canceled,
    tgt.CreateDate = src.CreateDate, tgt.CreateTS = src.CreateTS, tgt.CreateTSNorm = src.CreateTSNorm,
    tgt.UpdateDate = src.UpdateDate, tgt.UpdateTS = src.UpdateTS, tgt.UpdateTSNorm = src.UpdateTSNorm,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, ItemCode, ItemName, FrgnName, ItmsGrpCod, CstGrpCode,
     InvntryUom, BuyUnitMsr, SalUnitMsr, ManSerNum, OnHand, IsCommited,
     OnOrder, AvgPrice, LastPurPrc, ItemType, SWW, Canceled,
     CreateDate, CreateTS, CreateTSNorm, UpdateDate, UpdateTS, UpdateTSNorm,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.ItemCode, src.ItemName, src.FrgnName, src.ItmsGrpCod, src.CstGrpCode,
     src.InvntryUom, src.BuyUnitMsr, src.SalUnitMsr, src.ManSerNum, src.OnHand, src.IsCommited,
     src.OnOrder, src.AvgPrice, src.LastPurPrc, src.ItemType, src.SWW, src.Canceled,
     src.CreateDate, src.CreateTS, src.CreateTSNorm, src.UpdateDate, src.UpdateTS, src.UpdateTSNorm,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapOitm(companyId, r)), ct);
    }

    // ── Salespersons (OSLP) ────────────────────────────────────────────────────

    public async Task<(int inserted, int updated)> UpsertSalespersonsAsync(
        string companyId, IEnumerable<SapOslpRow> rows, CancellationToken ct)
    {
        const string sql = @"
MERGE [raw].[sap_oslp] AS tgt
USING (SELECT @company_id, @SlpCode, @SlpName, @Commission, @Email, @Mobile, @Telephone, @Active, @GroupCode,
              @CreateDate, @CreateTS, @CreateTSNorm,
              @UpdateDate, @UpdateTS, @UpdateTSNorm,
              @source_hash_hex, @extraction_run_id, @batch_id, @extracted_at_utc, @ingestion_mode)
       AS src (company_id, SlpCode, SlpName, Commission, Email, Mobile, Telephone, Active, GroupCode,
               CreateDate, CreateTS, CreateTSNorm,
               UpdateDate, UpdateTS, UpdateTSNorm,
               source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
ON (tgt.company_id = src.company_id AND tgt.SlpCode = src.SlpCode)
WHEN MATCHED AND (
    tgt.source_hash_hex != src.source_hash_hex
    AND (
        src.UpdateDate > tgt.UpdateDate
        OR (src.UpdateDate = tgt.UpdateDate AND ISNULL(src.UpdateTSNorm,'000000') >= ISNULL(tgt.UpdateTSNorm,'000000'))
    )
) THEN UPDATE SET
    tgt.SlpName = src.SlpName, tgt.Commission = src.Commission,
    tgt.Email = src.Email, tgt.Mobile = src.Mobile, tgt.Telephone = src.Telephone,
    tgt.Active = src.Active, tgt.GroupCode = src.GroupCode,
    tgt.CreateDate = src.CreateDate, tgt.CreateTS = src.CreateTS, tgt.CreateTSNorm = src.CreateTSNorm,
    tgt.UpdateDate = src.UpdateDate, tgt.UpdateTS = src.UpdateTS, tgt.UpdateTSNorm = src.UpdateTSNorm,
    tgt.source_hash_hex = src.source_hash_hex,
    tgt.extraction_run_id = src.extraction_run_id, tgt.batch_id = src.batch_id,
    tgt.extracted_at_utc = src.extracted_at_utc, tgt.ingestion_mode = src.ingestion_mode,
    tgt.raw_updated_at_utc = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN INSERT
    (company_id, SlpCode, SlpName, Commission, Email, Mobile, Telephone, Active, GroupCode,
     CreateDate, CreateTS, CreateTSNorm, UpdateDate, UpdateTS, UpdateTSNorm,
     source_hash_hex, extraction_run_id, batch_id, extracted_at_utc, ingestion_mode)
    VALUES
    (src.company_id, src.SlpCode, src.SlpName, src.Commission, src.Email, src.Mobile, src.Telephone, src.Active, src.GroupCode,
     src.CreateDate, src.CreateTS, src.CreateTSNorm, src.UpdateDate, src.UpdateTS, src.UpdateTSNorm,
     src.source_hash_hex, src.extraction_run_id, src.batch_id, src.extracted_at_utc, src.ingestion_mode)
OUTPUT $action;";

        return await ExecuteMergeAsync(sql, rows.Select(r => MapOslp(companyId, r)), ct);
    }

    // ── Cross-table helpers ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<int>> GetExistingCreditMemoDocEntriesAsync(
        string companyId, IEnumerable<int> docEntries, CancellationToken ct)
    {
        var list = docEntries.Distinct().ToList();
        if (list.Count == 0) return Array.Empty<int>();

        const string sql = @"
SELECT DocEntry
FROM [raw].[sap_orin]
WHERE company_id = @CompanyId AND DocEntry IN @DocEntries;";

        await using var conn = new NpgsqlConnection(connectionString);
        var rows = await conn.QueryAsync<int>(
            new CommandDefinition(sql, new { CompanyId = companyId, DocEntries = list }, cancellationToken: ct));
        return rows.ToList();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<(int inserted, int updated)> ExecuteMergeAsync(
        string sql, IEnumerable<DynamicParameters> paramsList, CancellationToken ct)
    {
        var inserted = 0;
        var updated = 0;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var p in paramsList)
        {
            var actions = (await conn.QueryAsync<string>(new CommandDefinition(sql, p, cancellationToken: ct))).ToList();
            inserted += actions.Count(a => a == "INSERT");
            updated += actions.Count(a => a == "UPDATE");
        }

        return (inserted, updated);
    }

    private static DynamicParameters MapOinv(string companyId, SapOinvRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("DocEntry", r.DocEntry);
        p.Add("DocNum", r.DocNum);
        p.Add("DocDate", r.DocDate);
        p.Add("DocDueDate", r.DocDueDate);
        p.Add("TaxDate", r.TaxDate);
        p.Add("CardCode", r.CardCode);
        p.Add("CardName", r.CardName);
        p.Add("DocTotal", r.DocTotal);
        p.Add("DocTotalSy", r.DocTotalSy);
        p.Add("VatSum", r.VatSum);
        p.Add("PaidToDate", r.PaidToDate);
        p.Add("DocCur", r.DocCur);
        p.Add("DocStatus", r.DocStatus);
        p.Add("SlpCode", r.SlpCode);
        p.Add("Comments", r.Comments);
        p.Add("ObjType", r.ObjType);
        p.Add("DocType", r.DocType);
        p.Add("Cancelled", r.Cancelled);
        p.Add("CreateDate", r.CreateDate);
        p.Add("CreateTS", r.CreateTS);
        p.Add("CreateTSNorm", r.CreateTSNorm);
        p.Add("UpdateDate", r.UpdateDate);
        p.Add("UpdateTS", r.UpdateTS);
        p.Add("UpdateTSNorm", r.UpdateTSNorm);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapInv1(string companyId, SapInv1Row r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("DocEntry", r.DocEntry);
        p.Add("LineNum", r.LineNum);
        p.Add("ItemCode", r.ItemCode);
        p.Add("Dscription", r.Dscription);
        p.Add("Quantity", r.Quantity);
        p.Add("Price", r.Price);
        p.Add("LineTotal", r.LineTotal);
        p.Add("Currency", r.Currency);
        p.Add("SlpCode", r.SlpCode);
        p.Add("WhsCode", r.WhsCode);
        p.Add("UomCode", r.UomCode);
        p.Add("DiscPrcnt", r.DiscPrcnt);
        p.Add("GrossBuyPr", r.GrossBuyPr);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOrin(string companyId, SapOrinRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("DocEntry", r.DocEntry);
        p.Add("DocNum", r.DocNum);
        p.Add("DocDate", r.DocDate);
        p.Add("DocDueDate", r.DocDueDate);
        p.Add("TaxDate", r.TaxDate);
        p.Add("CardCode", r.CardCode);
        p.Add("CardName", r.CardName);
        p.Add("DocTotal", r.DocTotal);
        p.Add("DocTotalSy", r.DocTotalSy);
        p.Add("VatSum", r.VatSum);
        p.Add("DocCur", r.DocCur);
        p.Add("DocStatus", r.DocStatus);
        p.Add("SlpCode", r.SlpCode);
        p.Add("Comments", r.Comments);
        p.Add("ObjType", r.ObjType);
        p.Add("DocType", r.DocType);
        p.Add("Cancelled", r.Cancelled);
        p.Add("CreateDate", r.CreateDate);
        p.Add("CreateTS", r.CreateTS);
        p.Add("CreateTSNorm", r.CreateTSNorm);
        p.Add("UpdateDate", r.UpdateDate);
        p.Add("UpdateTS", r.UpdateTS);
        p.Add("UpdateTSNorm", r.UpdateTSNorm);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapRin1(string companyId, SapRin1Row r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("DocEntry", r.DocEntry);
        p.Add("LineNum", r.LineNum);
        p.Add("ItemCode", r.ItemCode);
        p.Add("Dscription", r.Dscription);
        p.Add("Quantity", r.Quantity);
        p.Add("Price", r.Price);
        p.Add("LineTotal", r.LineTotal);
        p.Add("Currency", r.Currency);
        p.Add("SlpCode", r.SlpCode);
        p.Add("WhsCode", r.WhsCode);
        p.Add("UomCode", r.UomCode);
        p.Add("DiscPrcnt", r.DiscPrcnt);
        p.Add("BaseRef", r.BaseRef);
        p.Add("BaseEntry", r.BaseEntry);
        p.Add("BaseLine", r.BaseLine);
        p.Add("BaseType", r.BaseType);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOcrd(string companyId, SapOcrdRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("CardCode", r.CardCode);
        p.Add("CardName", r.CardName);
        p.Add("CardType", r.CardType);
        p.Add("GroupCode", r.GroupCode);
        p.Add("CntctPrsn", r.CntctPrsn);
        p.Add("Phone1", r.Phone1);
        p.Add("Phone2", r.Phone2);
        p.Add("Fax", r.Fax);
        p.Add("EMail", r.EMail);
        p.Add("Country", r.Country);
        p.Add("City", r.City);
        p.Add("ZipCode", r.ZipCode);
        p.Add("Currency", r.Currency);
        p.Add("SlpCode", r.SlpCode);
        p.Add("VatLiable", r.VatLiable);
        p.Add("LicTradNum", r.LicTradNum);
        p.Add("FrozenFor", r.FrozenFor);
        p.Add("Balance", r.Balance);
        p.Add("CreditLine", r.CreditLine);
        p.Add("CreateDate", r.CreateDate);
        p.Add("CreateTS", r.CreateTS);
        p.Add("CreateTSNorm", r.CreateTSNorm);
        p.Add("UpdateDate", r.UpdateDate);
        p.Add("UpdateTS", r.UpdateTS);
        p.Add("UpdateTSNorm", r.UpdateTSNorm);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOitm(string companyId, SapOitmRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("ItemCode", r.ItemCode);
        p.Add("ItemName", r.ItemName);
        p.Add("FrgnName", r.FrgnName);
        p.Add("ItmsGrpCod", r.ItmsGrpCod);
        p.Add("CstGrpCode", r.CstGrpCode);
        p.Add("InvntryUom", r.InvntryUom);
        p.Add("BuyUnitMsr", r.BuyUnitMsr);
        p.Add("SalUnitMsr", r.SalUnitMsr);
        p.Add("ManSerNum", r.ManSerNum);
        p.Add("OnHand", r.OnHand);
        p.Add("IsCommited", r.IsCommited);
        p.Add("OnOrder", r.OnOrder);
        p.Add("AvgPrice", r.AvgPrice);
        p.Add("LastPurPrc", r.LastPurPrc);
        p.Add("ItemType", r.ItemType);
        p.Add("SWW", r.SWW);
        p.Add("Canceled", r.Canceled);
        p.Add("CreateDate", r.CreateDate);
        p.Add("CreateTS", r.CreateTS);
        p.Add("CreateTSNorm", r.CreateTSNorm);
        p.Add("UpdateDate", r.UpdateDate);
        p.Add("UpdateTS", r.UpdateTS);
        p.Add("UpdateTSNorm", r.UpdateTSNorm);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }

    private static DynamicParameters MapOslp(string companyId, SapOslpRow r)
    {
        var p = new DynamicParameters();
        p.Add("company_id", companyId);
        p.Add("SlpCode", r.SlpCode);
        p.Add("SlpName", r.SlpName);
        p.Add("Commission", r.Commission);
        p.Add("Email", r.Email);
        p.Add("Mobile", r.Mobile);
        p.Add("Telephone", r.Telephone);
        p.Add("Active", r.Active);
        p.Add("GroupCode", r.GroupCode);
        p.Add("CreateDate", r.CreateDate);
        p.Add("CreateTS", r.CreateTS);
        p.Add("CreateTSNorm", r.CreateTSNorm);
        p.Add("UpdateDate", r.UpdateDate);
        p.Add("UpdateTS", r.UpdateTS);
        p.Add("UpdateTSNorm", r.UpdateTSNorm);
        p.Add("source_hash_hex", r.SourceHashHex);
        p.Add("extraction_run_id", r.ExtractionRunId);
        p.Add("batch_id", r.BatchId);
        p.Add("extracted_at_utc", r.ExtractedAtUtc);
        p.Add("ingestion_mode", r.IngestionMode);
        return p;
    }
}
