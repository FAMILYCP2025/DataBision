using System.Text.Json.Serialization;

namespace DataBision.Extractor.ServiceLayer;

public sealed class ServiceLayerAuthResponse
{
    [JsonPropertyName("SessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("Version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("SessionTimeout")]
    public int SessionTimeout { get; init; }
}
