namespace DataBision.Api.Contracts;

public sealed class ApiErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }
}
