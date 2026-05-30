using System.ComponentModel.DataAnnotations;

namespace DataBision.Application.DTOs.Ingest;

/// <summary>
/// Non-generic surface so ApiKeyAuthFilter can validate TenantId/CompanyId against the API key
/// without knowing the row type T.
/// </summary>
public interface IIngestBatchRequest
{
    string TenantId { get; set; }
    string CompanyId { get; set; }
    string SapObject { get; set; }
}

public sealed class IngestBatchRequest<T> : IIngestBatchRequest where T : IIngestRow
{
    public string TenantId { get; set; } = string.Empty;

    public string CompanyId { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string SapObject { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<T> Rows { get; set; } = [];
}
