namespace DataBision.Application.DTOs.Dashboard;

public sealed class ChartOfAccountEntryDto
{
    public string  Code          { get; set; } = string.Empty;
    public string? Name          { get; set; }
    public string? FatherNum     { get; set; }
    public int?    Level         { get; set; }
    public string? AccountType   { get; set; }
    public string? StatementLine { get; set; }
    public bool    Postable      { get; set; }
    public decimal Balance       { get; set; }
}
