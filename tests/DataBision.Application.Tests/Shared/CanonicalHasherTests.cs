using DataBision.Shared.Hashing;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Shared;

public sealed class CanonicalHasherTests
{
    [Fact]
    public void ComputeHex_SameInputDifferentOrder_ReturnsSameHash()
    {
        // Keys in different order must produce the same hash (sorted keys guarantee this)
        var row1 = new Dictionary<string, object?>
        {
            ["CardCode"] = "C001",
            ["DocTotal"] = 1500.00m,
            ["DocDate"] = new DateTime(2026, 1, 15),
        };
        var row2 = new Dictionary<string, object?>
        {
            ["DocDate"] = new DateTime(2026, 1, 15),
            ["DocTotal"] = 1500.00m,
            ["CardCode"] = "C001",
        };

        CanonicalHasher.ComputeHex(row1).Should().Be(CanonicalHasher.ComputeHex(row2));
    }

    [Fact]
    public void ComputeHex_ExcludesTechnicalFields()
    {
        // Adding technical fields must not change the hash
        var businessOnly = new Dictionary<string, object?>
        {
            ["CardCode"] = "C001",
            ["DocTotal"] = 1500.00m,
        };
        var withTechnical = new Dictionary<string, object?>
        {
            ["CardCode"] = "C001",
            ["DocTotal"] = 1500.00m,
            ["SourceHashHex"] = "abc123",
            ["ExtractionRunId"] = "run-42",
            ["BatchId"] = "batch-1",
            ["ExtractedAtUtc"] = DateTime.UtcNow,
            ["IngestionMode"] = "DEDICATED_HANA",
        };

        CanonicalHasher.ComputeHex(businessOnly).Should().Be(CanonicalHasher.ComputeHex(withTechnical));
    }

    [Fact]
    public void ComputeHex_StringTrimsWhitespace()
    {
        // Leading/trailing whitespace in strings must be normalised
        var clean = new Dictionary<string, object?> { ["CardName"] = "Acme Corp" };
        var padded = new Dictionary<string, object?> { ["CardName"] = "  Acme Corp  " };

        CanonicalHasher.ComputeHex(clean).Should().Be(CanonicalHasher.ComputeHex(padded));
    }

    [Fact]
    public void ComputeHex_DecimalNormalisation_TrailingZerosIgnored()
    {
        // 1500.00 and 1500 and 1500.0 must all produce the same hash
        var a = new Dictionary<string, object?> { ["DocTotal"] = 1500.00m };
        var b = new Dictionary<string, object?> { ["DocTotal"] = 1500m };
        var c = new Dictionary<string, object?> { ["DocTotal"] = 1500.0m };

        var hashA = CanonicalHasher.ComputeHex(a);
        CanonicalHasher.ComputeHex(b).Should().Be(hashA);
        CanonicalHasher.ComputeHex(c).Should().Be(hashA);
    }

    [Fact]
    public void ComputeHex_NullValues_StableAcrossRuns()
    {
        // Null fields must be stable (not random)
        var row = new Dictionary<string, object?>
        {
            ["CardCode"] = "C001",
            ["Comments"] = null,
        };

        var hash1 = CanonicalHasher.ComputeHex(row);
        var hash2 = CanonicalHasher.ComputeHex(row);

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputeHex_DifferentValues_ReturnsDifferentHash()
    {
        var row1 = new Dictionary<string, object?> { ["DocTotal"] = 100m };
        var row2 = new Dictionary<string, object?> { ["DocTotal"] = 200m };

        CanonicalHasher.ComputeHex(row1).Should().NotBe(CanonicalHasher.ComputeHex(row2));
    }

    [Fact]
    public void ComputeBytes_Returns32Bytes()
    {
        var row = new Dictionary<string, object?> { ["Key"] = "value" };
        CanonicalHasher.ComputeBytes(row).Should().HaveCount(32);
    }
}
