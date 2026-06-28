using Dapper;
using DataBision.Application.DTOs.Dashboard;
using DataBision.Application.Interfaces.Dashboard;
using Npgsql;

namespace DataBision.Infrastructure.Repositories.Dashboard;

public sealed class FinanceMartRepository(string connectionString) : IFinanceMartRepository
{
    private NpgsqlConnection OpenConnection() => new(connectionString);

    public async Task<FinanceMartSummaryDto?> GetSummaryAsync(
        string companyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                total_open_ar     AS "TotalOpenAr",
                total_overdue_ar  AS "TotalOverdueAr",
                ar_customer_count AS "ArCustomerCount",
                dso_days          AS "DsoDays",
                total_open_ap     AS "TotalOpenAp",
                total_overdue_ap  AS "TotalOverdueAp",
                ap_supplier_count AS "ApSupplierCount",
                dpo_days          AS "DpoDays"
            FROM mart.finance_summary
            WHERE company_id = @company_id;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<FinanceMartSummaryDto>(
            new CommandDefinition(sql, new { company_id = companyId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ArAgingRowDto>> GetArAgingAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                card_code       AS "CardCode",
                card_name       AS "CardName",
                current_amount  AS "CurrentAmount",
                bucket_1_30     AS "Bucket1To30",
                bucket_31_60    AS "Bucket31To60",
                bucket_61_90    AS "Bucket61To90",
                bucket_91_120   AS "Bucket91To120",
                bucket_over_120 AS "BucketOver120",
                total_open      AS "TotalOpen",
                invoice_count   AS "InvoiceCount",
                oldest_due_date AS "OldestDueDate"
            FROM mart.ar_aging
            WHERE company_id = @company_id
            ORDER BY total_open DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<ArAgingRowDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ApAgingRowDto>> GetApAgingAsync(
        string companyId, int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                card_code       AS "CardCode",
                card_name       AS "CardName",
                current_amount  AS "CurrentAmount",
                bucket_1_30     AS "Bucket1To30",
                bucket_31_60    AS "Bucket31To60",
                bucket_61_90    AS "Bucket61To90",
                bucket_91_120   AS "Bucket91To120",
                bucket_over_120 AS "BucketOver120",
                total_open      AS "TotalOpen",
                invoice_count   AS "InvoiceCount",
                oldest_due_date AS "OldestDueDate"
            FROM mart.ap_aging
            WHERE company_id = @company_id
            ORDER BY total_open DESC
            LIMIT @limit;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<ApAgingRowDto>(
            new CommandDefinition(sql, new { company_id = companyId, limit }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<FinancePeriodKpiDto>> GetPeriodKpiAsync(
        string companyId, int months, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                period_year      AS "Year",
                period_month     AS "Month",
                ar_billed        AS "ArBilled",
                ar_credit_memo   AS "ArCreditMemo",
                ar_net           AS "ArNet",
                ar_invoice_count AS "ArInvoiceCount",
                ap_billed        AS "ApBilled",
                ap_credit_memo   AS "ApCreditMemo",
                ap_net           AS "ApNet",
                ap_invoice_count AS "ApInvoiceCount"
            FROM mart.finance_period_kpi
            WHERE company_id = @company_id
              AND (period_year * 100 + period_month) >
                  (EXTRACT(YEAR  FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT * 100 +
                   EXTRACT(MONTH FROM CURRENT_DATE - (INTERVAL '1 month' * @months))::INT)
            ORDER BY period_year ASC, period_month ASC;
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<FinancePeriodKpiDto>(
            new CommandDefinition(sql, new { company_id = companyId, months }, cancellationToken: ct));
        return rows.AsList();
    }
}
