using DataBision.Shared.Watermarks;
using FluentAssertions;
using Xunit;

namespace DataBision.Application.Tests.Shared;

public sealed class TsNormalizerTests
{
    [Theory]
    [InlineData("123456", "123456")]
    [InlineData("23456",  "023456")]  // 5 digits → pad left
    [InlineData("0",      "000000")]  // SAP returns 0 for midnight
    [InlineData(123456,   "123456")]  // int form
    [InlineData(23456,    "023456")]  // int < 6 digits
    [InlineData("12:34:56", "123456")] // strip separators
    [InlineData("123456.0", "123456")] // SAP HANA sometimes returns decimal
    [InlineData("1234567", "123456")]  // truncate to 6
    public void Normalize_ReturnsSixDigitHHMMSS(object input, string expected)
    {
        TsNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void Normalize_ReturnsNullForEmptyOrNonDigit(object? input)
    {
        TsNormalizer.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void Normalize_DoesNotTreatTSAsDecimalVisual()
    {
        // "1.5" must NOT round to "1" or "2" — must extract all digits → "000015"
        TsNormalizer.Normalize("1.5").Should().Be("000015");
    }
}
