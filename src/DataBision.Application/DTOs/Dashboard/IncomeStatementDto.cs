namespace DataBision.Application.DTOs.Dashboard;

public sealed class IncomeStatementLineDto
{
    public string StatementLine { get; set; } = string.Empty;
    public decimal Amount       { get; set; }
    public decimal PctOfRevenue { get; set; }
}

public sealed class IncomeStatementPeriodDto
{
    public int PeriodYear       { get; set; }
    public int PeriodMonth      { get; set; }
    public decimal Revenue      { get; set; }
    public decimal Cogs         { get; set; }
    public decimal GrossProfit  { get; set; }
    public decimal GrossProfitPct { get; set; }
    public decimal Opex         { get; set; }
    public decimal OperatingIncome { get; set; }
    public decimal OperatingPct  { get; set; }
    public decimal Financial    { get; set; }
    public decimal Tax          { get; set; }
    public decimal NetIncome    { get; set; }
    public decimal NetPct       { get; set; }
    public IReadOnlyList<IncomeStatementLineDto> Lines { get; set; } = [];
}
