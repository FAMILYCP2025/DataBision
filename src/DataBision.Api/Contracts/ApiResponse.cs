namespace DataBision.Api.Contracts;

public sealed class ApiResponse<T>
{
    public T Data { get; init; } = default!;
    public string? TraceId { get; init; }
}
