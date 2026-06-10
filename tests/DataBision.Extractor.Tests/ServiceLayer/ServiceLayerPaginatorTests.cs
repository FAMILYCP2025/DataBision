using System.Text.Json.Nodes;
using DataBision.Extractor.ServiceLayer;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DataBision.Extractor.Tests.ServiceLayer;

public sealed class ServiceLayerPaginatorTests
{
    private readonly Mock<IServiceLayerClient> _mockSl;
    private readonly ServiceLayerPaginator _paginator;

    public ServiceLayerPaginatorTests()
    {
        _mockSl    = new Mock<IServiceLayerClient>();
        _paginator = new ServiceLayerPaginator(
            _mockSl.Object,
            NullLogger<ServiceLayerPaginator>.Instance,
            delayFactory: (_, _) => Task.CompletedTask); // no actual sleep in tests
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static JsonArray MakeRows(int count, string updateDate = "2024-01-01")
    {
        var arr = new JsonArray();
        for (int i = 0; i < count; i++)
            arr.Add(JsonNode.Parse($"{{\"DocEntry\":{i + 1},\"UpdateDate\":\"{updateDate}\"}}"));
        return arr;
    }

    // ── Test 1: single page — fewer rows than pageSize → done ─────────────────

    [Fact]
    public async Task PaginateAsync_SinglePage_ReturnsAllRowsAndOnePage()
    {
        var rows = MakeRows(5);
        _mockSl.Setup(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ServiceLayerPage(rows, null));

        var result = await _paginator.PaginateAsync("OINV", "Invoices", "$select=DocEntry", 100, 500, CancellationToken.None);

        result.AllRows.Count.Should().Be(5);
        result.Logs.Should().HaveCount(1);
        result.Logs[0].Status.Should().Be("OK");
        result.Logs[0].RowsReceived.Should().Be(5);
        result.HitMaxPages.Should().BeFalse();
        result.LastError.Should().BeNull();
    }

    // ── Test 2: multi-page by $skip — page 1 full, page 2 partial ─────────────

    [Fact]
    public async Task PaginateAsync_MultiPageSkip_AccumulatesAllRows()
    {
        var page1 = MakeRows(3); // full page (pageSize=3)
        var page2 = MakeRows(2); // last page (< pageSize)

        _mockSl.SetupSequence(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ServiceLayerPage(page1, null))
               .ReturnsAsync(new ServiceLayerPage(page2, null));

        var result = await _paginator.PaginateAsync("OCRD", "BusinessPartners", "$select=CardCode", 3, 500, CancellationToken.None);

        result.AllRows.Count.Should().Be(5);
        result.Logs.Should().HaveCount(2);
        result.Logs[0].Skip.Should().Be(0);
        result.Logs[1].Skip.Should().Be(3);
        result.HitMaxPages.Should().BeFalse();
        result.LastError.Should().BeNull();
    }

    // ── Test 3: multi-page by @odata.nextLink ─────────────────────────────────

    [Fact]
    public async Task PaginateAsync_NextLink_UsesExtractedQueryForNextPage()
    {
        var page1 = MakeRows(100);
        var page2 = MakeRows(42);
        const string nextLink = "https://sap-host:50000/b1s/v1/Invoices?$skip=100&$top=100&$select=DocEntry";

        _mockSl.SetupSequence(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ServiceLayerPage(page1, nextLink))
               .ReturnsAsync(new ServiceLayerPage(page2, null));

        var result = await _paginator.PaginateAsync("OINV", "Invoices", "$select=DocEntry", 100, 500, CancellationToken.None);

        result.AllRows.Count.Should().Be(142);
        result.Logs.Should().HaveCount(2);

        // Second call must use the query extracted from nextLink
        _mockSl.Verify(
            s => s.GetPageAsync("Invoices", "$skip=100&$top=100&$select=DocEntry", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 4: maxPages cap stops early ──────────────────────────────────────

    [Fact]
    public async Task PaginateAsync_MaxPagesCap_StopsAndSetsFlag()
    {
        // Every call returns a full page (would paginate forever without cap)
        _mockSl.Setup(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ServiceLayerPage(MakeRows(10), null));

        var result = await _paginator.PaginateAsync("OITM", "Items", "$select=ItemCode", 10, maxPages: 3, CancellationToken.None);

        result.HitMaxPages.Should().BeTrue();
        result.AllRows.Count.Should().Be(30); // 3 pages × 10 rows
        result.Logs.Should().HaveCount(3);
        result.LastError.Should().BeNull();
    }

    // ── Test 5: retry on transient error → succeeds on second attempt ─────────

    [Fact]
    public async Task PaginateAsync_TransientError_RetriesAndSucceeds()
    {
        var rows = MakeRows(5);

        _mockSl.SetupSequence(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new HttpRequestException("network timeout"))
               .ReturnsAsync(new ServiceLayerPage(rows, null));

        var result = await _paginator.PaginateAsync("OINV", "Invoices", "$select=DocEntry", 100, 500, CancellationToken.None);

        result.AllRows.Count.Should().Be(5);
        result.LastError.Should().BeNull();
        result.Logs.Should().HaveCount(1);
        result.Logs[0].Status.Should().Be("OK");
        // GetPageAsync was called twice: once failing, once succeeding
        _mockSl.Verify(
            s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ── Test 6: permanent error → stops and records in LastError ──────────────

    [Fact]
    public async Task PaginateAsync_PermanentError_StopsAndReturnsLastError()
    {
        _mockSl.Setup(s => s.GetPageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("GET Invoices failed. HTTP 400: invalid field 'BadField'"));

        var result = await _paginator.PaginateAsync("OINV", "Invoices", "$select=BadField", 100, 500, CancellationToken.None);

        result.AllRows.Count.Should().Be(0);
        result.LastError.Should().Contain("400");
        result.Logs.Should().HaveCount(1);
        result.Logs[0].Status.Should().Be("ERROR");
        result.HitMaxPages.Should().BeFalse();
    }
}
