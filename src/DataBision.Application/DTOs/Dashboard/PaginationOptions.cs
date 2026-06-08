namespace DataBision.Application.DTOs.Dashboard;

/// <summary>
/// Pagination parameters passed from controller → service → repository.
/// SortBy and SortDir are validated at the controller boundary before reaching here.
/// </summary>
public sealed record PaginationOptions(
    int Limit,
    int Offset = 0,
    string? SortBy = null,
    string? SortDir = null
);
