namespace DataBision.Application.DTOs.Admin;

public sealed class AccountClassificationRuleDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public string? FormatCode { get; set; }
    public string StatementLine { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpsertAccountClassificationRuleDto
{
    public string? AccountCode { get; set; }
    public string? FormatCode { get; set; }
    public string StatementLine { get; set; } = string.Empty;
}

public sealed class AccountClassificationTemplateSuggestionDto
{
    public string? AccountCode { get; set; }
    public string? FormatCode { get; set; }
    public string SuggestedStatementLine { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string Reason { get; set; } = string.Empty;
}
