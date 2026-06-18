namespace DataBision.Application.DTOs.Dashboard;

public sealed class FinanceValidationSummaryDto
{
    public int    HealthScore           { get; set; }  // 0-100
    public string HealthStatus          { get; set; } = string.Empty; // ok | warning | critical
    public int    CriticalIssues        { get; set; }
    public int    WarningIssues         { get; set; }
    public int    InfoIssues            { get; set; }
    public string? LastPeriodValidated  { get; set; }  // YYYY-MM
    public decimal BalanceImbalance     { get; set; }  // |assets - liabilities - equity|
    public int    UnclassifiedAccounts  { get; set; }
    public int    OrphanJournalLines    { get; set; }
    public IReadOnlyList<FinanceValidationIssueDto> Issues { get; set; } = [];
    public FinanceReconciliationDto? Reconciliation        { get; set; }
}

public sealed class FinanceValidationIssueDto
{
    public string Severity    { get; set; } = string.Empty; // critical | warning | info
    public string IssueType   { get; set; } = string.Empty; // unclassified_accounts | balance_imbalance | orphan_lines | negative_revenue | no_data | sign_issue
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    Count       { get; set; }
    public string? Period     { get; set; }
}

public sealed class FinanceReconciliationDto
{
    public string? SnapshotDate     { get; set; }
    public decimal TotalAssets      { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity      { get; set; }
    public decimal Imbalance        { get; set; }
    public bool    IsBalanced       { get; set; }
}
