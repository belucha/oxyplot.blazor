using System;
using System.Buffers;

namespace OxyPlot.Blazor
{
    /// <summary>
    /// Growable character buffer rented from the shared array pool, used to build long SVG attribute values.
    /// </summary>
    internal struct PooledCharBuffer : IDisposable
    {
        private char[] _buffer;
        private int _length;

        public PooledCharBuffer(int capacity)
        {
            _buffer = ArrayPool<char>.Shared.Rent(Math.Max(capacity, 256));
            _length = 0;
        }

        public readonly int Length => _length;

        /// <summary>
        /// Gets a span with at least <paramref name="size"/> free characters at the end of the buffer.
        /// </summary>
        public Span<char> GetSpan(int size)
        {
            if (_length + size > _buffer.Length)
            {
                var larger = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, _length + size));
                _buffer.AsSpan(0, _length).CopyTo(larger);
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = larger;
            }
            return _buffer.AsSpan(_length);
        }

        public void Advance(int count) => _length += count;

        public void Clear() => _length = 0;

        public void Append(char c)
        {
            GetSpan(1)[0] = c;
            _length++;
        }

        public void Append(ReadOnlySpan<char> value)
        {
            value.CopyTo(GetSpan(value.Length));
            _length += value.Length;
        }

        public readonly override string ToString() => new(_buffer, 0, _length);

        public void Dispose()
        {
            if (_buffer != null)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = null!;
            }
        }
    }
}
