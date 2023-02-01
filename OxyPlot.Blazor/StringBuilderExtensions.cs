using System.Drawing;
using System.Text;

namespace OxyPlot.Blazor
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder AppendInvariant<T>(this StringBuilder builder, T value, string? format = default) where T: System.IFormattable
        {
            builder.Append(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
            return builder;
        }
        public static StringBuilder AppendInvariant(this StringBuilder builder, ScreenPoint point, string? format = default)
        {
            builder.Append(point.X.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.Y.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
            return builder;
        }
    }
}
