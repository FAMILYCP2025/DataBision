using DataBision.Application.DTOs.Dashboard;

namespace DataBision.Api.Contracts;

public sealed class PagedApiResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public PagedMetaDto Meta { get; init; } = new();
    public string? TraceId { get; init; }
}
