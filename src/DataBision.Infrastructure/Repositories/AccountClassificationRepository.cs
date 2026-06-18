using Dapper;
using DataBision.Application.DTOs.Admin;
using DataBision.Application.Interfaces;
using Npgsql;

namespace DataBision.Infrastructure.Repositories;

public sealed class AccountClassificationRepository(string connectionString) : IAccountClassificationService
{
    // SAP account_type → suggested statement_line (same fallback as ETL)
    private static readonly Dictionary<string, string> SapTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"]  = "current_assets",
        ["L"]  = "current_liabilities",
        ["E"]  = "equity",
        ["R"]  = "revenue",
        ["X"]  = "opex",
        ["N"]  = "cogs",
    };

    private static readonly HashSet<string> AllowedStatementLines = new(StringComparer.OrdinalIgnoreCase)
    {
        "revenue", "cogs", "opex", "other_income", "other_expense",
        "financial", "tax", "depreciation", "amortization",
        "current_assets", "non_current_assets",
        "current_liabilities", "non_current_liabilities",
        "equity", "unclassified"
    };

    private NpgsqlConnection OpenConnection() => new(connectionString);

    public static bool IsValidStatementLine(string line) =>
        AllowedStatementLines.Contains(line);

    public async Task<IReadOnlyList<AccountClassificationRuleDto>> GetRulesAsync(
        string analyticsCompanyId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, company_id, account_code, format_code, statement_line, created_at, updated_at
            FROM cfg.account_classification_rules
            WHERE company_id = @company_id
            ORDER BY statement_line, COALESCE(account_code, format_code)
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<AccountClassificationRuleRow>(
            new CommandDefinition(sql, new { company_id = analyticsCompanyId }, cancellationToken: ct));

        return rows.Select(r => new AccountClassificationRuleDto
        {
            Id            = r.Id,
            CompanyId     = r.CompanyId,
            AccountCode   = r.AccountCode,
            FormatCode    = r.FormatCode,
            StatementLine = r.StatementLine,
            CreatedAt     = r.CreatedAt,
            UpdatedAt     = r.UpdatedAt
        }).ToList();
    }

    public async Task<AccountClassificationRuleDto> CreateRuleAsync(
        string analyticsCompanyId, UpsertAccountClassificationRuleDto dto, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO cfg.account_classification_rules
                (company_id, account_code, format_code, statement_line, created_at, updated_at)
            VALUES
                (@company_id, @account_code, @format_code, @statement_line, NOW(), NOW())
            RETURNING id, company_id, account_code, format_code, statement_line, created_at, updated_at
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var row = await conn.QuerySingleAsync<AccountClassificationRuleRow>(
            new CommandDefinition(sql, new
            {
                company_id     = analyticsCompanyId,
                account_code   = dto.AccountCode,
                format_code    = dto.FormatCode,
                statement_line = dto.StatementLine
            }, cancellationToken: ct));

        return new AccountClassificationRuleDto
        {
            Id            = row.Id,
            CompanyId     = row.CompanyId,
            AccountCode   = row.AccountCode,
            FormatCode    = row.FormatCode,
            StatementLine = row.StatementLine,
            CreatedAt     = row.CreatedAt,
            UpdatedAt     = row.UpdatedAt
        };
    }

    public async Task<AccountClassificationRuleDto?> UpdateRuleAsync(
        string analyticsCompanyId, int ruleId, UpsertAccountClassificationRuleDto dto, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE cfg.account_classification_rules
            SET account_code   = @account_code,
                format_code    = @format_code,
                statement_line = @statement_line,
                updated_at     = NOW()
            WHERE id = @id AND company_id = @company_id
            RETURNING id, company_id, account_code, format_code, statement_line, created_at, updated_at
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<AccountClassificationRuleRow>(
            new CommandDefinition(sql, new
            {
                id             = ruleId,
                company_id     = analyticsCompanyId,
                account_code   = dto.AccountCode,
                format_code    = dto.FormatCode,
                statement_line = dto.StatementLine
            }, cancellationToken: ct));

        if (row is null) return null;

        return new AccountClassificationRuleDto
        {
            Id            = row.Id,
            CompanyId     = row.CompanyId,
            AccountCode   = row.AccountCode,
            FormatCode    = row.FormatCode,
            StatementLine = row.StatementLine,
            CreatedAt     = row.CreatedAt,
            UpdatedAt     = row.UpdatedAt
        };
    }

    public async Task<bool> DeleteRuleAsync(
        string analyticsCompanyId, int ruleId, CancellationToken ct = default)
    {
        const string sql = """
            DELETE FROM cfg.account_classification_rules
            WHERE id = @id AND company_id = @company_id
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { id = ruleId, company_id = analyticsCompanyId }, cancellationToken: ct));

        return affected > 0;
    }

    public async Task<IReadOnlyList<AccountClassificationTemplateSuggestionDto>> GetTemplateSuggestionsAsync(
        string analyticsCompanyId, CancellationToken ct = default)
    {
        // Query raw.sap_oact to get postable accounts not yet classified
        const string sql = """
            SELECT
                a."Code"        AS code,
                a."Name"        AS name,
                a."AccountType" AS account_type,
                a."FormatCode"  AS format_code
            FROM "raw"."sap_oact" a
            WHERE a.company_id = @company_id
              AND UPPER(TRIM(COALESCE(a."Postable", 'N'))) = 'Y'
              AND NOT EXISTS (
                  SELECT 1 FROM cfg.account_classification_rules r
                  WHERE r.company_id = a.company_id
                    AND r.account_code = a."Code"
              )
            ORDER BY a."Code"
            LIMIT 200
            """;

        await using var conn = OpenConnection();
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<TemplateRow>(
            new CommandDefinition(sql, new { company_id = analyticsCompanyId }, cancellationToken: ct));

        return rows.Select(r => new AccountClassificationTemplateSuggestionDto
        {
            AccountCode           = r.Code,
            FormatCode            = r.FormatCode,
            AccountType           = r.AccountType ?? string.Empty,
            AccountName           = r.Name,
            SuggestedStatementLine = SapTypeMap.TryGetValue(r.AccountType ?? "", out var sl) ? sl : "unclassified",
            Reason                = SapTypeMap.ContainsKey(r.AccountType ?? "")
                ? $"SAP account_type '{r.AccountType}' default mapping"
                : "No default mapping — review manually with accountant"
        }).ToList();
    }

    // ── Private row types ─────────────────────────────────────────────────────

    private sealed record AccountClassificationRuleRow(
        int      Id,
        string   CompanyId,
        string?  AccountCode,
        string?  FormatCode,
        string   StatementLine,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record TemplateRow(
        string  Code,
        string? Name,
        string? AccountType,
        string? FormatCode);
}
