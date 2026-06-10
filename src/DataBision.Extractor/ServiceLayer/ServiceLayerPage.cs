using System.Text.Json.Nodes;

namespace DataBision.Extractor.ServiceLayer;

/// <summary>Result of a single SAP Service Layer page request.</summary>
public sealed record ServiceLayerPage(JsonArray Rows, string? NextLink);
