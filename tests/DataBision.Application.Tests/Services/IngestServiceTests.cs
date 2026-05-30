using DataBision.Application.DTOs.Ingest;
using DataBision.Application.DTOs.Ingest.Rows;
using DataBision.Application.Interfaces.Ingest;
using DataBision.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class IngestServiceTests
{
    private readonly Mock<ISapRawRepository> _rawRepo = new();
    private readonly Mock<IIngestCheckpointRepository> _checkpointRepo = new();

    private IngestService NewService() => new(_rawRepo.Object, _checkpointRepo.Object);

    // ── ORIN insert/update/idempotency ─────────────────────────────────────────

    [Fact]
    public async Task IngestCreditMemos_NewRow_CallsRepositoryAndUpdatesCheckpoint()
    {
        _rawRepo.Setup(r => r.UpsertCreditMemosAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<SapOrinRow>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 0));

        var request = NewOrinRequest(new SapOrinRow
        {
            DocEntry = 1, DocNum = 1001, CardCode = "C001",
            UpdateDate = new DateTime(2026, 5, 10), UpdateTS = "123456",
        });

        var result = await NewService().IngestCreditMemosAsync(request);

        result.RowsInserted.Should().Be(1);
        result.RowsUpdated.Should().Be(0);
        request.Rows[0].SourceHashHex.Should().NotBeNullOrEmpty().And.HaveLength(64);
        request.Rows[0].UpdateTSNorm.Should().Be("123456");
        _checkpointRepo.Verify(c => c.UpsertAsync(It.Is<CheckpointDto>(
            d => d.SapObject == "ORIN" && d.WatermarkDate == "2026-05-10" && d.WatermarkTs == "123456"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestCreditMemos_SameBatchTwice_IsIdempotent_NoSecondUpsertSideEffect()
    {
        // The repository's MERGE handles idempotency. The service must still compute the same hash
        // each time so a downstream UPDATE only happens when business columns actually changed.
        _rawRepo.Setup(r => r.UpsertCreditMemosAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<SapOrinRow>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        var row = new SapOrinRow { DocEntry = 1, DocNum = 1001, CardCode = "C001" };
        var request = NewOrinRequest(row);

        await NewService().IngestCreditMemosAsync(request);
        var firstHash = row.SourceHashHex;

        // Re-send the same row
        row.SourceHashHex = null;
        await NewService().IngestCreditMemosAsync(request);

        row.SourceHashHex.Should().Be(firstHash);
    }

    // ── RIN1 with / without ORIN header ────────────────────────────────────────

    [Fact]
    public async Task IngestCreditMemoLines_WhenHeaderExists_ProceedsWithUpsert()
    {
        _rawRepo.Setup(r => r.GetExistingCreditMemoDocEntriesAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });
        _rawRepo.Setup(r => r.UpsertCreditMemoLinesAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<SapRin1Row>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 0));

        var request = new IngestBatchRequest<SapRin1Row>
        {
            TenantId = "t1", CompanyId = "c1", SapObject = "RIN1",
            Rows = [new SapRin1Row { DocEntry = 1, LineNum = 0, ItemCode = "I001" }],
        };

        var result = await NewService().IngestCreditMemoLinesAsync(request);

        result.RowsInserted.Should().Be(1);
        _rawRepo.Verify(r => r.UpsertCreditMemoLinesAsync(
            "c1", It.IsAny<IEnumerable<SapRin1Row>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestCreditMemoLines_WhenHeaderMissing_ThrowsAndDoesNotUpsert()
    {
        _rawRepo.Setup(r => r.GetExistingCreditMemoDocEntriesAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>()); // no ORIN headers exist

        var request = new IngestBatchRequest<SapRin1Row>
        {
            TenantId = "t1", CompanyId = "c1", SapObject = "RIN1",
            Rows = [new SapRin1Row { DocEntry = 99, LineNum = 0 }],
        };

        var act = () => NewService().IngestCreditMemoLinesAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORIN header*");
        _rawRepo.Verify(r => r.UpsertCreditMemoLinesAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<SapRin1Row>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── OSLP insert/update ─────────────────────────────────────────────────────

    [Fact]
    public async Task IngestSalespersons_ComputesHashAndNormalisesTs()
    {
        _rawRepo.Setup(r => r.UpsertSalespersonsAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<SapOslpRow>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 0));

        var row = new SapOslpRow
        {
            SlpCode = 5, SlpName = "Jane Doe",
            UpdateDate = new DateTime(2026, 5, 18), UpdateTS = "9", // 1-digit TS → normalises to 000009
        };
        var request = new IngestBatchRequest<SapOslpRow>
        {
            TenantId = "t1", CompanyId = "c1", SapObject = "OSLP",
            Rows = [row],
        };

        var result = await NewService().IngestSalespersonsAsync(request);

        result.RowsInserted.Should().Be(1);
        row.UpdateTSNorm.Should().Be("000009");
        row.SourceHashHex.Should().NotBeNullOrEmpty();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IngestBatchRequest<SapOrinRow> NewOrinRequest(SapOrinRow row) =>
        new()
        {
            TenantId = "t1",
            CompanyId = "c1",
            SapObject = "ORIN",
            Rows = [row],
        };
}
