namespace DataBision.Application.DTOs.Dashboard;

public sealed class EbitdaPeriodDto
{
    public int PeriodYear       { get; set; }
    public int PeriodMonth      { get; set; }
    public decimal Revenue      { get; set; }
    public decimal Cogs         { get; set; }
    public decimal GrossProfit  { get; set; }
    public decimal Opex         { get; set; }
    public decimal Ebitda       { get; set; }
    public decimal Depreciation { get; set; }
    public decimal Amortization { get; set; }
    public decimal FinancialResult { get; set; }
    public decimal TaxResult    { get; set; }
    public decimal NetIncome    { get; set; }
    public decimal EbitdaMargin { get; set; }
    public decimal NetMargin    { get; set; }
}
