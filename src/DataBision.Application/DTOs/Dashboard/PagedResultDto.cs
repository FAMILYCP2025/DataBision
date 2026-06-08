namespace DataBision.Application.DTOs.Dashboard;

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public PagedMetaDto Meta { get; init; } = new();
}

public sealed class PagedMetaDto
{
    public int Limit { get; init; }
    public int Offset { get; init; }
    public int Count { get; init; }
    public bool HasMore { get; init; }
}
