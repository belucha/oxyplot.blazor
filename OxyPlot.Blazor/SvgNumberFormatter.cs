using System;
using System.Globalization;
using System.Text;

namespace OxyPlot.Blazor
{
    /// <summary>
    /// Formats numbers for SVG attributes.
    /// </summary>
    /// <remarks>
    /// Formats of the form "0", "0.#", "0.##", ... are formatted by a simple integer based algorithm.
    /// The .NET custom numeric format parser is very slow on WebAssembly, where it dominated the rendering time
    /// of plots with thousands of points. Any other format falls back to <see cref="double.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)"/>.
    /// </remarks>
    internal readonly struct SvgNumberFormatter
    {
        /// <summary>Maximum length of a formatted number.</summary>
        public const int MaxLength = 64;

        private const int MaxDecimals = 6;
        private const double MaxFastValue = 1e15;
        private static readonly uint[] s_powersOf10 = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000];
        private const string DigitPairs =
            "00010203040506070809" +
            "10111213141516171819" +
            "20212223242526272829" +
            "30313233343536373839" +
            "40414243444546474849" +
            "50515253545556575859" +
            "60616263646566676869" +
            "70717273747576777879" +
            "80818283848586878889" +
            "90919293949596979899";

        private readonly string? _format;
        private readonly int _decimals;
        private readonly double _scale;

        public SvgNumberFormatter(string? format)
        {
            _format = format;
            _decimals = GetFastDecimals(format);
            _scale = _decimals >= 0 ? s_powersOf10[_decimals] : 0;
        }

        /// <summary>
        /// Gets the number of decimals of a "0.##" like format or -1 if the format is not supported by the fast path.
        /// </summary>
        internal static int GetFastDecimals(string? format)
        {
            if (format == "0")
            {
                return 0;
            }
            if (format == null || format.Length < 3 || format.Length - 2 > MaxDecimals || format[0] != '0' || format[1] != '.')
            {
                return -1;
            }
            for (var i = 2; i < format.Length; i++)
            {
                if (format[i] != '#')
                {
                    return -1;
                }
            }
            return format.Length - 2;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="Round"/> knows the precision of the format.
        /// </summary>
        public bool CanRound => _decimals >= 0;

        /// <summary>
        /// Rounds a value to the precision of the format, formatting the result gives the same text as formatting the value.
        /// </summary>
        public double Round(double value)
        {
            if (!CanRound)
            {
                return value;
            }
            var scaled = RoundScaled(Math.Abs(value) * _scale);
            return scaled < MaxFastValue ? Math.CopySign(scaled / _scale, value) : value;
        }

        public bool TryFormat(double value, Span<char> destination, out int charsWritten)
        {
            if (_decimals >= 0 && destination.Length >= MaxLength)
            {
                var negative = value < 0;
                var scaled = RoundScaled((negative ? -value : value) * _scale);
                if (scaled < uint.MaxValue)
                {
                    charsWritten = Write((uint)scaled, negative, destination);
                    return true;
                }
                if (scaled < MaxFastValue)
                {
                    charsWritten = Write((ulong)scaled, negative, destination);
                    return true;
                }
            }
            return value.TryFormat(destination, out charsWritten, _format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Rounds half away from zero, like the custom numeric format does. That format works with 15 significant digits,
        /// so a value like 1147.485, which is stored slightly below the midpoint, is rounded up as well.
        /// </summary>
        private static double RoundScaled(double scaled) => Math.Floor(scaled + 0.5 + scaled * 1e-15);

        public string Format(double value)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            return TryFormat(value, buffer, out var length) ? new string(buffer[..length]) : value.ToString(_format, CultureInfo.InvariantCulture);
        }

        public void Append(StringBuilder builder, double value)
        {
            Span<char> buffer = stackalloc char[MaxLength];
            if (TryFormat(value, buffer, out var length))
            {
                builder.Append(buffer[..length]);
            }
            else
            {
                builder.Append(value.ToString(_format, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Writes <paramref name="scaled"/> / 10^decimals without trailing zeros (32 bit version, used for most values).
        /// </summary>
        private int Write(uint scaled, bool negative, Span<char> destination)
        {
            var length = 0;
            if (negative && scaled != 0)
            {
                destination[length++] = '-';
            }
            var decimals = _decimals;
            while (decimals > 0 && scaled % 10 == 0)
            {
                scaled /= 10;
                decimals--;
            }
            if (decimals == 0)
            {
                return length + WriteInteger(scaled, destination[length..]);
            }
            var power = s_powersOf10[decimals];
            var integer = scaled / power;
            var fraction = scaled - integer * power;
            length += WriteInteger(integer, destination[length..]);
            destination[length++] = '.';
            for (var i = length + decimals - 1; i >= length; i--)
            {
                var next = fraction / 10;
                destination[i] = (char)('0' + (fraction - next * 10));
                fraction = next;
            }
            return length + decimals;
        }

        /// <summary>
        /// Writes <paramref name="scaled"/> / 10^decimals without trailing zeros (64 bit version for large values).
        /// </summary>
        private int Write(ulong scaled, bool negative, Span<char> destination)
        {
            var decimals = _decimals;
            while (decimals > 0 && scaled % 10 == 0)
            {
                scaled /= 10;
                decimals--;
            }
            var power = s_powersOf10[decimals];
            var integer = scaled / power;
            var fraction = (uint)(scaled - integer * power);
            var length = 0;
            if (negative)
            {
                destination[length++] = '-';
            }
            integer.TryFormat(destination[length..], out var integerLength, default, CultureInfo.InvariantCulture);
            length += integerLength;
            if (decimals == 0)
            {
                return length;
            }
            destination[length++] = '.';
            for (var i = length + decimals - 1; i >= length; i--)
            {
                destination[i] = (char)('0' + fraction % 10);
                fraction /= 10;
            }
            return length + decimals;
        }

        /// <summary>
        /// Writes the decimal digits of <paramref name="value"/>, two digits at a time.
        /// </summary>
        private static int WriteInteger(uint value, Span<char> destination)
        {
            var count = CountDigits(value);
            var i = count - 1;
            while (value >= 10)
            {
                var next = value / 100;
                var pair = (int)(value - next * 100) * 2;
                destination[i--] = DigitPairs[pair + 1];
                destination[i--] = DigitPairs[pair];
                value = next;
            }
            if (i == 0)
            {
                destination[0] = (char)('0' + value);
            }
            return count;
        }

        private static int CountDigits(uint value) =>
            value < 10 ? 1 :
            value < 100 ? 2 :
            value < 1_000 ? 3 :
            value < 10_000 ? 4 :
            value < 100_000 ? 5 :
            value < 1_000_000 ? 6 :
            value < 10_000_000 ? 7 :
            value < 100_000_000 ? 8 :
            value < 1_000_000_000 ? 9 : 10;
    }
}
