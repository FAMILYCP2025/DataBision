using DataBision.Application.Services;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Services;

public sealed class SecretRefResolverTests
{
    [Fact]
    public void Resolve_EnvScheme_ReturnsVariableValue()
    {
        const string varName = "DATABISION_TEST_SECRET_ABC123";
        Environment.SetEnvironmentVariable(varName, "secret-value-xyz");
        try
        {
            var result = SecretRefResolver.Resolve($"env:{varName}");
            result.Should().Be("secret-value-xyz");
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, null);
        }
    }

    [Fact]
    public void Resolve_EnvScheme_VariableNotSet_Throws()
    {
        const string varName = "DATABISION_TEST_SECRET_NOT_SET_XYZ";
        Environment.SetEnvironmentVariable(varName, null);

        var act = () => SecretRefResolver.Resolve($"env:{varName}");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{varName}'*");
    }

    [Fact]
    public void Resolve_EnvScheme_NoVarName_Throws()
    {
        var act = () => SecretRefResolver.Resolve("env:");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*variable name*");
    }

    [Fact]
    public void Resolve_LocalDevOnlyScheme_ReturnsValue()
    {
        var original = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        try
        {
            var result = SecretRefResolver.Resolve("local-dev-only:my-dev-password");
            result.Should().Be("my-dev-password");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", original);
        }
    }

    [Fact]
    public void Resolve_LocalDevOnlyScheme_BlockedWhenAspnetcoreEnvironmentIsProduction()
    {
        var original = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        try
        {
            var act = () => SecretRefResolver.Resolve("local-dev-only:my-dev-password");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*local-dev-only*Production*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", original);
        }
    }

    [Fact]
    public void Resolve_LocalDevOnlyScheme_EmptyValue_Throws()
    {
        var act = () => SecretRefResolver.Resolve("local-dev-only:");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolve_AzureKvScheme_ThrowsNotSupported()
    {
        var act = () => SecretRefResolver.Resolve("azure-kv://my-vault/my-secret");
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*azure-kv*");
    }

    [Fact]
    public void Resolve_UnknownScheme_Throws()
    {
        var act = () => SecretRefResolver.Resolve("plaintext:password123");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown SecretRef scheme*");
    }

    [Fact]
    public void Resolve_EmptyString_Throws()
    {
        var act = () => SecretRefResolver.Resolve(string.Empty);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void Resolve_WhitespaceOnly_Throws()
    {
        var act = () => SecretRefResolver.Resolve("   ");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*required*");
    }

    [Theory]
    [InlineData("env:MY_VAR",       "env:***")]
    [InlineData("azure-kv://v/s",   "azure-kv:***")]
    [InlineData("local-dev-only:x", "local-dev-only:***")]
    [InlineData("nocodon",          "***")]
    [InlineData("",                 "(empty)")]
    public void ToHint_ReturnsSafeRepresentation(string secretRef, string expectedHint)
    {
        SecretRefResolver.ToHint(secretRef).Should().Be(expectedHint);
    }
}
