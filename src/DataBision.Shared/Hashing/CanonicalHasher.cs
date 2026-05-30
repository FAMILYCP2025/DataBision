using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DataBision.Shared.Hashing;

/// <summary>
/// Computes a deterministic SHA-256 hash over the business columns of a SAP raw row.
/// Authoritative implementation — agents may pre-validate but the Ingest API always recomputes.
/// </summary>
public static class CanonicalHasher
{
    // Technical fields injected by the ingestion pipeline — excluded from hash
    private static readonly HashSet<string> ExcludedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "SourceHashHex",
        "ExtractionRunId",
        "BatchId",
        "ExtractedAtUtc",
        "IngestionMode"
    };

    /// <summary>
    /// Returns the lowercase hex SHA-256 hash of the canonical JSON representation of <paramref name="row"/>.
    /// </summary>
    public static string ComputeHex(IDictionary<string, object?> row)
    {
        var bytes = ComputeBytes(row);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Returns the raw 32-byte SHA-256 of the canonical JSON representation of <paramref name="row"/>.
    /// </summary>
    public static byte[] ComputeBytes(IDictionary<string, object?> row)
    {
        var canonical = BuildCanonicalJson(row);
        var utf8 = Encoding.UTF8.GetBytes(canonical);
        return SHA256.HashData(utf8);
    }

    // ── internal ──────────────────────────────────────────────────────────────

    internal static string BuildCanonicalJson(IDictionary<string, object?> row)
    {
        // 1. Filter excluded fields
        // 2. Sort keys (ordinal, case-sensitive — consistent across platforms)
        // 3. Normalise values
        var sorted = row
            .Where(kv => !ExcludedFields.Contains(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal);

        var node = new JsonObject();
        foreach (var (key, rawValue) in sorted)
        {
            node[key] = NormaliseValue(rawValue);
        }

        return node.ToJsonString(JsonSerializerOptions.Default);
    }

    private static JsonNode? NormaliseValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => JsonValue.Create(s.Trim()),
            decimal d => JsonValue.Create(NormaliseDecimal(d)),
            double dbl => JsonValue.Create(NormaliseDecimal((decimal)dbl)),
            float f => JsonValue.Create(NormaliseDecimal((decimal)f)),
            int i => JsonValue.Create((long)i),
            long l => JsonValue.Create(l),
            bool b => JsonValue.Create(b),
            DateTime dt => JsonValue.Create(dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")),
            DateTimeOffset dto => JsonValue.Create(dto.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")),
            _ => JsonValue.Create(Convert.ToString(value) ?? string.Empty)
        };
    }

    // Strips trailing zeros after the decimal point for stable representation.
    // 1.5000 → "1.5", 100.00 → "100", 3.14 → "3.14"
    private static string NormaliseDecimal(decimal d)
        => d.ToString("G29").TrimEnd('0').TrimEnd('.');
}
