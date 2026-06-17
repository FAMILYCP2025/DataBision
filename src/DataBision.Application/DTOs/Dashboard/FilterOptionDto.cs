namespace DataBision.Application.DTOs.Dashboard;

/// <summary>A single selectable option returned by filter-options endpoints.</summary>
public sealed record FilterOptionDto(string Code, string Name);
