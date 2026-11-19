using System.Globalization;
using System.Text;
using OxyPlot.Blazor;

namespace OxyPlot.Blazor.Tests;

/// <summary>
/// <see cref="SvgNumberFormatter"/> replaces the .NET custom numeric format parser for the
/// "0", "0.#", "0.##", ... formats, because that parser dominated the rendering time on WebAssembly.
/// These tests pin down that it writes exactly the same text as <c>double.ToString(format, InvariantCulture)</c>
/// for the value range an svg coordinate, stroke width or dash length can actually have.
/// </summary>
/// <remarks>
/// Known and accepted deviations:
/// <list type="bullet">
/// <item>a value that rounds to zero is written as "0", the .NET format writes "-0" for a negative one.</item>
/// <item>the fast path emulates the 15 significant digits of the .NET format with a relative epsilon.
/// It is exact while <c>|value| * 10^decimals &lt;= 1e10</c>; the first deviations in the last written
/// decimal appear at 1e11. A plot coordinate stays far below that bound - a 4000 pixel wide plot with
/// the default "0.##" reaches 4e5 - so the tests pin the documented bound.</item>
/// </list>
/// </remarks>
public class SvgNumberFormatterTests
{
    /// <summary>The formats handled by the fast path.</summary>
    public static TheoryData<string> FastFormats => new() { "0", "0.#", "0.##", "0.###", "0.####", "0.#####", "0.######" };

    /// <summary>Formats that have to fall back to the .NET formatter.</summary>
    public static TheoryData<string> FallbackFormats => new() { "0.00", "0.0#", "F2", "G", "E2", "#.##", "0.#######" };

    /// <summary>
    /// Asserts that the formatter writes the same text as the .NET custom numeric format,
    /// tolerating only the documented "-0" difference.
    /// </summary>
    private static void AssertSameAsDotNet(string format, double value)
    {
        var expected = value.ToString(format, CultureInfo.InvariantCulture);
        var actual = new SvgNumberFormatter(format).Format(value);
        if (expected == actual)
        {
            return;
        }
        // a negative value that rounds to zero is written as "0" instead of "-0"
        if (expected.Length == actual.Length + 1 && expected[0] == '-' && expected[1..] == actual
            && double.Parse(actual, CultureInfo.InvariantCulture) == 0)
        {
            return;
        }
        Assert.Fail($"format '{format}', value {value:R}: expected '{expected}', got '{actual}'");
    }

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Format_MatchesDotNet_ForPlotCoordinates(string format)
    {
        var random = new Random(20261119);
        for (var i = 0; i < 20_000; i++)
        {
            // the range of an svg coordinate: a few thousand pixels, slightly negative for clipped points
            AssertSameAsDotNet(format, (random.NextDouble() - 0.05) * 4000);
        }
    }

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Format_MatchesDotNet_ForExactMidpoints(string format)
    {
        var random = new Random(4711);
        for (var i = 0; i < 20_000; i++)
        {
            // values like 1234.565 and 1234.5, which are exactly between two formatted values
            AssertSameAsDotNet(format, random.Next(-4_000_000, 4_000_000) / 2.0 / 1000.0);
            AssertSameAsDotNet(format, random.Next(-40_000, 40_000) / 2.0 / 10.0);
        }
    }

    /// <summary>
    /// The largest <c>|value| * 10^decimals</c> the fast path still formats exactly like the .NET format.
    /// </summary>
    private const double MaxExactScaledValue = 1e10;

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Format_MatchesDotNet_UpToTheDocumentedMagnitude(string format)
    {
        var decimals = SvgNumberFormatter.GetFastDecimals(format);
        var maxValue = MaxExactScaledValue / Math.Pow(10, decimals);
        var random = new Random(1234);
        // stroke widths and dash lengths are small, axis values before scaling can be large
        for (var scale = 1e-7; scale <= maxValue; scale *= 10)
        {
            for (var i = 0; i < 2_000; i++)
            {
                AssertSameAsDotNet(format, (random.NextDouble() * 2 - 1) * scale);
            }
        }
        // and densely right at the documented bound
        for (var i = 0; i < 20_000; i++)
        {
            AssertSameAsDotNet(format, (random.NextDouble() * 2 - 1) * maxValue);
        }
    }

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Format_MatchesDotNet_ForSpecialValues(string format)
    {
        double[] values =
        [
            0.0, -0.0, 1, -1, 0.5, -0.5, 0.05, -0.05, 0.005, 0.0005, 0.00005, 0.000005, 0.0000005,
            0.125, 0.135, 2.675, 1147.485, 1.005, 9.995, 99.995, 0.1, 0.2, 0.3, 1.0 / 3.0, 2.0 / 3.0,
            1e-7, -1e-7, 1e6, -1e6, 123456.789, 999999.999, 1.5, 2.5, -1.5, -2.5,
            double.NaN, double.PositiveInfinity, double.NegativeInfinity,
        ];
        foreach (var value in values)
        {
            AssertSameAsDotNet(format, value);
        }
    }

    [Theory]
    [MemberData(nameof(FallbackFormats))]
    public void Format_FallsBackToDotNet_ForUnsupportedFormats(string format)
    {
        Assert.False(new SvgNumberFormatter(format).CanRound);
        var random = new Random(99);
        for (var i = 0; i < 5_000; i++)
        {
            var value = (random.NextDouble() - 0.05) * 4000;
            Assert.Equal(value.ToString(format, CultureInfo.InvariantCulture), new SvgNumberFormatter(format).Format(value));
        }
    }

    [Fact]
    public void Format_WritesZeroWithoutSign_WhenNegativeValueRoundsToZero()
    {
        var formatter = new SvgNumberFormatter("0.##");
        Assert.Equal("0", formatter.Format(-0.0));
        Assert.Equal("0", formatter.Format(-0.001));
        Assert.Equal("0", formatter.Format(-0.004999));
        // this is the documented difference to the .NET format, which writes "-0" here
        Assert.Equal("-0", (-0.001).ToString("0.##", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0.##", 1.5, "1.5")]
    [InlineData("0.##", 1.50, "1.5")]
    [InlineData("0.##", 2.0, "2")]
    [InlineData("0.##", 2.001, "2")]
    [InlineData("0.##", 2.005, "2.01")]
    [InlineData("0.##", -2.005, "-2.01")]
    [InlineData("0.##", 0.1, "0.1")]
    [InlineData("0", 2.5, "3")]
    [InlineData("0", -2.5, "-3")]
    [InlineData("0", 0.4, "0")]
    [InlineData("0.######", 0.0000005, "0.000001")]
    [InlineData("0.######", 1234.5678905, "1234.567891")]
    public void Format_WritesExpectedText(string format, double value, string expected)
        => Assert.Equal(expected, new SvgNumberFormatter(format).Format(value));

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Append_WritesTheSameTextAsFormat(string format)
    {
        var formatter = new SvgNumberFormatter(format);
        var random = new Random(777);
        var builder = new StringBuilder();
        for (var i = 0; i < 5_000; i++)
        {
            var value = (random.NextDouble() - 0.05) * 4000;
            builder.Clear();
            formatter.Append(builder, value);
            Assert.Equal(formatter.Format(value), builder.ToString());
        }
    }

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void TryFormat_WritesTheSameTextAsFormat(string format)
    {
        var formatter = new SvgNumberFormatter(format);
        var random = new Random(888);
        var buffer = new char[SvgNumberFormatter.MaxLength];
        for (var i = 0; i < 5_000; i++)
        {
            var value = (random.NextDouble() - 0.05) * 4000;
            Assert.True(formatter.TryFormat(value, buffer, out var length));
            Assert.Equal(formatter.Format(value), new string(buffer, 0, length));
        }
    }

    [Fact]
    public void TryFormat_FailsInsteadOfOverflowing_WhenTheBufferIsTooSmall()
    {
        var formatter = new SvgNumberFormatter("0.##");
        Span<char> tooSmall = stackalloc char[2];
        Assert.False(formatter.TryFormat(1234.56, tooSmall, out var length));
        Assert.Equal(0, length);
    }

    [Theory]
    [MemberData(nameof(FastFormats))]
    public void Round_ProducesAValueThatFormatsIdentically(string format)
    {
        var formatter = new SvgNumberFormatter(format);
        Assert.True(formatter.CanRound);
        var random = new Random(31415);
        for (var i = 0; i < 20_000; i++)
        {
            // DrawEllipses relies on this: the radius it rounds has to be written exactly like the unrounded one,
            // otherwise the arcs of the combined path do not meet the ends of the ellipse
            var value = (random.NextDouble() - 0.05) * 4000;
            Assert.Equal(formatter.Format(value), formatter.Format(formatter.Round(value)));
        }
    }

    [Fact]
    public void Round_ReturnsTheValueUnchanged_WhenTheFormatIsNotSupported()
    {
        var formatter = new SvgNumberFormatter("0.00");
        Assert.False(formatter.CanRound);
        Assert.Equal(1.23456789, formatter.Round(1.23456789));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.#", 1)]
    [InlineData("0.##", 2)]
    [InlineData("0.###", 3)]
    [InlineData("0.####", 4)]
    [InlineData("0.#####", 5)]
    [InlineData("0.######", 6)]
    [InlineData("0.#######", -1)] // more decimals than the fast path supports
    [InlineData("0.00", -1)]
    [InlineData("0.#0", -1)]
    [InlineData("0.", -1)]
    [InlineData("#.##", -1)]
    [InlineData("00.##", -1)]
    [InlineData("", -1)]
    [InlineData("G", -1)]
    [InlineData(null, -1)]
    public void GetFastDecimals_RecognizesOnlyTheSupportedFormats(string? format, int expected)
        => Assert.Equal(expected, SvgNumberFormatter.GetFastDecimals(format));
}
