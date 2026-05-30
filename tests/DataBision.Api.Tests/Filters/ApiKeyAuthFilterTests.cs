using DataBision.Api.Filters;
using DataBision.Application.DTOs.Ingest;
using DataBision.Application.DTOs.Ingest.Rows;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DataBision.Api.Tests.Filters;

public sealed class ApiKeyAuthFilterTests
{
    private const string ValidKey = "test-key-001";
    private const string TenantId = "tenant-acme";
    private const string CompanyId = "company-acme-cl";

    [Fact]
    public async Task NoHeader_Returns401()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(actionArguments: new());

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        ctx.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task EmptyHeader_Returns401()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(apiKey: "  ", actionArguments: new());

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        ctx.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task UnknownKey_Returns401()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(apiKey: "not-registered", actionArguments: new());

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        ctx.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ValidKey_NoBody_PassesThroughAndSetsItems()
    {
        var filter = BuildFilter();
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new());

        var nextCalled = false;
        await filter.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        nextCalled.Should().BeTrue();
        ctx.HttpContext.Items[ApiKeyAuthFilter.TenantIdItemKey].Should().Be(TenantId);
        ctx.HttpContext.Items[ApiKeyAuthFilter.CompanyIdItemKey].Should().Be(CompanyId);
    }

    [Fact]
    public async Task ValidKey_BodyMatches_PassesThrough()
    {
        var filter = BuildFilter();
        var batch = new IngestBatchRequest<SapOcrdRow>
        {
            TenantId = TenantId, CompanyId = CompanyId, SapObject = "OCRD",
            Rows = [new SapOcrdRow { CardCode = "C001" }],
        };
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new() { ["request"] = batch });

        var nextCalled = false;
        await filter.OnActionExecutionAsync(ctx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        nextCalled.Should().BeTrue();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task ValidKey_BodyEmpty_FillsFromKey()
    {
        var filter = BuildFilter();
        var batch = new IngestBatchRequest<SapOcrdRow>
        {
            // TenantId and CompanyId both empty → DEV/MVP autofill
            SapObject = "OCRD",
            Rows = [new SapOcrdRow { CardCode = "C001" }],
        };
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new() { ["request"] = batch });

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        batch.TenantId.Should().Be(TenantId);
        batch.CompanyId.Should().Be(CompanyId);
    }

    [Fact]
    public async Task ValidKey_TenantMismatch_Returns403()
    {
        var filter = BuildFilter();
        var batch = new IngestBatchRequest<SapOcrdRow>
        {
            TenantId = "wrong-tenant", CompanyId = CompanyId, SapObject = "OCRD",
            Rows = [new SapOcrdRow { CardCode = "C001" }],
        };
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new() { ["request"] = batch });

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ValidKey_CompanyMismatch_Returns403()
    {
        var filter = BuildFilter();
        var batch = new IngestBatchRequest<SapOcrdRow>
        {
            TenantId = TenantId, CompanyId = "wrong-company", SapObject = "OCRD",
            Rows = [new SapOcrdRow { CardCode = "C001" }],
        };
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new() { ["request"] = batch });

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task ValidKey_PartialBody_Returns403()
    {
        var filter = BuildFilter();
        var batch = new IngestBatchRequest<SapOcrdRow>
        {
            TenantId = TenantId, CompanyId = "", // partial population
            SapObject = "OCRD",
            Rows = [new SapOcrdRow { CardCode = "C001" }],
        };
        var ctx = BuildContext(apiKey: ValidKey, actionArguments: new() { ["request"] = batch });

        await filter.OnActionExecutionAsync(ctx, NoOpNext);

        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(403);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ApiKeyAuthFilter BuildFilter()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Ingest:ApiKeys:{ValidKey}"] = $"{TenantId}:{CompanyId}",
            })
            .Build();
        return new ApiKeyAuthFilter(config);
    }

    private static ActionExecutingContext BuildContext(
        string? apiKey = null,
        Dictionary<string, object?>? actionArguments = null)
    {
        var httpContext = new DefaultHttpContext();
        if (apiKey is not null)
            httpContext.Request.Headers[ApiKeyAuthFilter.HeaderName] = apiKey;

        var actionContext = new ActionContext(
            httpContext, new RouteData(), new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments ?? new Dictionary<string, object?>(),
            controller: null!);
    }

    private static Task<ActionExecutedContext> NoOpNext() =>
        Task.FromResult<ActionExecutedContext>(null!);
}
