namespace DataBision.Application.DTOs.Dashboard;

public sealed class FinanceReadinessDto
{
    // Raw layer counts
    public int  RawOactCount    { get; set; }  // raw.sap_oact rows for this company
    public int  RawOjdtCount    { get; set; }  // raw.sap_ojdt rows
    public int  RawJdt1Count    { get; set; }  // raw.sap_jdt1 rows

    // Staging layer counts
    public int  StgOactCount    { get; set; }  // stg.sap_oact rows
    public int  StgOjdtCount    { get; set; }  // stg.sap_ojdt rows
    public int  StgJdt1Count    { get; set; }  // stg.sap_jdt1 rows

    // MART layer counts
    public int  MartGlAccounts      { get; set; }  // mart.gl_accounts rows
    public int  MartIncomeStatement { get; set; }  // mart.income_statement_summary rows
    public int  MartBalanceSheet    { get; set; }  // mart.balance_sheet_summary rows
    public int  MartEbitda          { get; set; }  // mart.ebitda_summary rows

    // Classification state
    public int  ClassificationRules      { get; set; }  // cfg.account_classification_rules rows
    public int  UnclassifiedPostable     { get; set; }  // mart.gl_accounts with statement_line = 'unclassified'

    // Readiness status
    public string ReadinessStatus { get; set; } = "blocked"; // blocked | warning | ready

    public IReadOnlyList<string> BlockingReasons { get; set; } = [];
    public IReadOnlyList<string> Warnings        { get; set; } = [];
}
