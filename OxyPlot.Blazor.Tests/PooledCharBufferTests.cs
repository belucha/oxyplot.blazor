using System.Text;
using OxyPlot.Blazor;

namespace OxyPlot.Blazor.Tests;

/// <summary>
/// <see cref="PooledCharBuffer"/> builds the long "d" and "points" attribute values.
/// It hands out spans of the rented array, so growing it must not lose or duplicate anything.
/// </summary>
public class PooledCharBufferTests
{
    [Fact]
    public void Append_KeepsTheContent_WhenTheBufferHasToGrow()
    {
        var expected = new StringBuilder();
        var buffer = new PooledCharBuffer(8); // below the minimum capacity, forces several grows
        try
        {
            for (var i = 0; i < 5_000; i++)
            {
                var text = $"{i},{i * 2} ";
                buffer.Append(text);
                expected.Append(text);
                buffer.Append('|');
                expected.Append('|');
            }
            Assert.Equal(expected.Length, buffer.Length);
            Assert.Equal(expected.ToString(), buffer.ToString());
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void GetSpan_ReturnsTheRequestedRoom_AndAdvanceKeepsOnlyWhatWasWritten()
    {
        var buffer = new PooledCharBuffer(16);
        try
        {
            for (var i = 0; i < 1_000; i++)
            {
                var span = buffer.GetSpan(64);
                Assert.True(span.Length >= 64);
                "abc".AsSpan().CopyTo(span);
                buffer.Advance(3); // the rest of the span must not end up in the result
            }
            Assert.Equal(3_000, buffer.Length);
            Assert.Equal(string.Concat(Enumerable.Repeat("abc", 1_000)), buffer.ToString());
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void Clear_EmptiesTheBuffer_AndItCanBeFilledAgain()
    {
        var buffer = new PooledCharBuffer(32);
        try
        {
            buffer.Append("M10,20");
            Assert.Equal(6, buffer.Length);
            buffer.Clear();
            Assert.Equal(0, buffer.Length);
            Assert.Equal(string.Empty, buffer.ToString());
            buffer.Append("M30,40");
            Assert.Equal("M30,40", buffer.ToString());
        }
        finally
        {
            buffer.Dispose();
        }
    }

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var buffer = new PooledCharBuffer(32);
        buffer.Append("x");
        buffer.Dispose();
        buffer.Dispose();
    }
}
