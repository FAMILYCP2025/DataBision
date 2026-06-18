namespace DataBision.Application.DTOs.Dashboard;

public sealed class BalanceSheetEntryDto
{
    public string Category    { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public decimal Amount     { get; set; }
}

public sealed class BalanceSheetSnapshotDto
{
    public string SnapshotDate      { get; set; } = string.Empty;
    public decimal TotalAssets      { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity      { get; set; }
    public decimal Imbalance        { get; set; }
    public IReadOnlyList<BalanceSheetEntryDto> Entries { get; set; } = [];
}
