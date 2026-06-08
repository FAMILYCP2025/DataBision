using Microsoft.AspNetCore.Mvc;

namespace DataBision.Api.Security;

public sealed class CompanyContextResult
{
    public string? CompanyId { get; init; }
    public string? Role { get; init; }
    public bool IsDevelopmentFallback { get; init; }
    public IActionResult? Error { get; init; }
    public bool IsSuccess => Error is null;
}
