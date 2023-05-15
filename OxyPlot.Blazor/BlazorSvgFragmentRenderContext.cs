// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlazorSvgRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a render context for scalable vector graphics output.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Blazor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.AspNetCore.Components.Rendering;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides a render context for scalable vector graphics output.
    /// </summary>
    public class BlazorSvgFragmentRenderContext : ClippingRenderContext
    {
        /// <summary>
        /// Tooltip(title) for next svg element
        /// </summary>
        private string? title;

        private readonly RenderTreeBuilder _b;
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Sets the tooltip for the next element
        /// </summary>
        /// <param name="text"></param>
        public override void SetToolTip(string text) => title = text;
        /// <summary>
        /// Adds the title if present
        /// </summary>
        protected void WriteTitle()
        {
            if (title != null)
            {
                WriteStartElement("title");
                WriteText(title);
                WriteEndElement();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgWriter" /> class.
        /// </summary>
        /// <param name="renderTreeBuilder">The render tree builder.</param>
        public BlazorSvgFragmentRenderContext(RenderTreeBuilder renderTreeBuilder)
        {
            _b = renderTreeBuilder ?? throw new ArgumentNullException(nameof(renderTreeBuilder));
            NumberFormat = "0.####";
            RendersToScreen = true;
        }
        public IRenderContext? TextMeasurer { get; set; }

        /// <summary>
        /// Gets or sets the number format.
        /// </summary>
        /// <value>The number format.</value>
        public string NumberFormat { get; set; }

        /// <summary>
        /// Creates a style.
        /// </summary>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness (in user units).</param>
        /// <param name="dashArray">The line dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <returns>A style string.</returns>
        public string CreateStyle(
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            double[]? dashArray = null,
            LineJoin lineJoin = LineJoin.Miter)
        {
            // http://oreilly.com/catalog/svgess/chapter/ch03.html
            var style = new StringBuilder(512);
            if (fill.IsInvisible())
            {
                style.Append("fill:none;");
            }
            else
            {
                style.Append("fill:");
                style.Append(fill.FormatColor());
                style.Append(';');
                if (fill.A != 0xFF)
                {
                    style.Append("fill-opacity:");
                    style.Append((fill.A / 255.0).ToString("0.00", CultureInfo.InvariantCulture));
                    style.Append(';');
                }
            }

            if (stroke.IsInvisible())
            {
                style.Append("stroke:none;");
            }
            else
            {
                style.Append("stroke:");
                style.Append(stroke.FormatColor());
                style.Append(";stroke-width:");
                style.AppendInvariant(thickness, NumberFormat);
                switch (lineJoin)
                {
                    case LineJoin.Round:
                        style.Append(";stroke-linejoin:round");
                        break;
                    case LineJoin.Bevel:
                        style.Append(";stroke-linejoin:bevel");
                        break;
                }

                if (stroke.A != 0xFF)
                {
                    style.Append(";stroke-opacity:");
                    style.AppendInvariant(stroke.A / 255.0, "0.00");
                }

                if (dashArray != null && dashArray.Length > 0)
                {
                    style.Append(";stroke-dasharray:");
                    for (int i = 0; i < dashArray.Length; i++)
                    {
                        if (i > 0)
                        {
                            style.Append(',');
                        }
                        style.AppendInvariant(dashArray[i], NumberFormat);
                    }
                }
            }
            return style.ToString();
        }

        /// <summary>
        /// Writes an ellipse.
        /// </summary>
        /// <param name="x">The x-coordinate of the center.</param>
        /// <param name="y">The y-coordinate of the center.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="style">The style.</param>
        public void WriteEllipse(double x, double y, double width, double height, string style, EdgeRenderingMode edgeRenderingMode)
        {
            // http://www.w3.org/TR/SVG/shapes.html#EllipseElement
            WriteStartElement("ellipse");
            WriteAttributeString("cx", x + (width / 2)); ;
            WriteAttributeString("cy", y + (height / 2));
            WriteAttributeString("rx", width / 2);
            WriteAttributeString("ry", height / 2);
            WriteAttributeString("style", style);
            WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            WriteEndElement();
        }

        protected override void SetClip(OxyRect clippingRectangle) => BeginClip(clippingRectangle.Left, clippingRectangle.Top, clippingRectangle.Width, clippingRectangle.Height);

        /// <inheritdoc/>
        protected override void ResetClip() => EndClip();

        /// <summary>
        /// Sets a clipping rectangle.
        /// </summary>
        /// <param name="x">The x coordinate of the clipping rectangle.</param>
        /// <param name="y">The y coordinate of the clipping rectangle.</param>
        /// <param name="width">The width of the clipping rectangle.</param>
        /// <param name="height">The height of the clipping rectangle.</param>
        public void BeginClip(double x, double y, double width, double height)
        {
            // http://www.w3.org/TR/SVG/masking.html
            // https://developer.mozilla.org/en-US/docs/Web/SVG/Element/clipPath
            // http://www.svgbasics.com/clipping.html
            // WARNING the clip path id must be unique in the document, not just within this svg fragment!
            var clipPath = $"_{Guid.NewGuid()}";
            WriteStartElement("defs");
            WriteStartElement("clipPath");
            WriteAttributeString("id", clipPath);
            WriteStartElement("rect");
            WriteAttributeString("x", x);
            WriteAttributeString("y", y);
            WriteAttributeString("width", width);
            WriteAttributeString("height", height);
            WriteEndElement(); // rect
            WriteEndElement(); // clipPath
            WriteEndElement(); // defs

            WriteStartElement("g");
            WriteAttributeString("clip-path", $"url(#{clipPath})");
        }

        /// <summary>
        /// Resets the clipping rectangle.
        /// </summary>
        public void EndClip() => WriteEndElement(); // g

        /// <summary>
        /// Writes a portion of the specified image.
        /// </summary>
        /// <param name="srcX">The x-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcY">The y-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcWidth">Width of the portion of the source image to draw.</param>
        /// <param name="srcHeight">Height of the portion of the source image to draw.</param>
        /// <param name="destX">The destination x-coordinate.</param>
        /// <param name="destY">The destination y-coordinate.</param>
        /// <param name="destWidth">Width of the destination rectangle.</param>
        /// <param name="destHeight">Height of the destination rectangle.</param>
        /// <param name="image">The image.</param>
        public void WriteImage(
            double srcX,
            double srcY,
            double srcWidth,
            double srcHeight,
            double destX,
            double destY,
            double destWidth,
            double destHeight,
            OxyImage image)
        {
            double x = destX - (srcX / srcWidth * destWidth);
            double width = image.Width / srcWidth * destWidth;
            double y = destY - (srcY / srcHeight * destHeight);
            double height = image.Height / srcHeight * destHeight;
            BeginClip(destX, destY, destWidth, destHeight);
            WriteImage(x, y, width, height, image);
            EndClip();
        }

        /// <summary>
        /// Writes the specified image.
        /// </summary>
        /// <param name="x">The x-coordinate.</param>
        /// <param name="y">The y-coordinate.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="image">The image.</param>
        public void WriteImage(double x, double y, double width, double height, OxyImage image)
        {
            // http://www.w3.org/TR/SVG/shapes.html#ImageElement
            WriteStartElement("image");
            WriteAttributeString("x", x);
            WriteAttributeString("y", y);
            WriteAttributeString("width", width);
            WriteAttributeString("height", height);
            WriteAttributeString("preserveAspectRatio", "none");
            var imageData = image.GetData();
            var encodedImage = new StringBuilder();
            encodedImage.Append("data:");
            encodedImage.Append("image/png");
            encodedImage.Append(";base64,");
            encodedImage.Append(Convert.ToBase64String(imageData));
            WriteAttributeString("xlink", "href", "", encodedImage.ToString());
            WriteEndElement();
        }

        /// <summary>
        /// Writes a line.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <param name="style">The style.</param>
        public void WriteLine(ScreenPoint p1, ScreenPoint p2, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#LineElement
            // http://www.w3schools.com/svg/svg_line.asp
            if (double.IsFinite(p1.X) && double.IsFinite(p1.Y) && double.IsFinite(p2.X) && double.IsFinite(p2.Y))
            {
                WriteStartElement("line");
                WriteAttributeString("x1", p1.X);
                WriteAttributeString("y1", p1.Y);
                WriteAttributeString("x2", p2.X);
                WriteAttributeString("y2", p2.Y);
                WriteAttributeString("style", style);
                WriteEndElement();
            }
        }

        /// <summary>
        /// Writes a polygon.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="style">The style.</param>
        public void WritePolygon(IEnumerable<ScreenPoint> points, string style, EdgeRenderingMode edgeRenderingMode)
        {
            // http://www.w3.org/TR/SVG/shapes.html#PolygonElement
            WriteStartElement("polygon");
            WriteAttributeString("points", PointsToString(points));
            WriteAttributeString("style", style);
            WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            WriteTitle();
            WriteEndElement();
        }

        /// <summary>
        /// Writes a polyline.
        /// </summary>
        /// <param name="pts">The points.</param>
        /// <param name="style">The style.</param>
        public void WritePolyline(IEnumerable<ScreenPoint> pts, string style, EdgeRenderingMode edgeRenderingMode)
        {
            // http://www.w3.org/TR/SVG/shapes.html#PolylineElement
            WriteStartElement("polyline");
            WriteAttributeString("points", PointsToString(pts));
            WriteAttributeString("style", style);
            WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            WriteEndElement();
        }

        /// <summary>
        /// Writes a rectangle.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="style">The style.</param>
        public void WriteRectangle(double x, double y, double width, double height, string style, EdgeRenderingMode edgeRenderingMode)
        {
            // http://www.w3.org/TR/SVG/shapes.html#RectangleElement
            WriteStartElement("rect");
            WriteAttributeString("x", x);
            WriteAttributeString("y", y);
            WriteAttributeString("width", width);
            WriteAttributeString("height", height);
            WriteAttributeString("style", style);
            WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            WriteTitle();
            WriteEndElement();
        }

        /// <summary>
        /// Writes text.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="text">The text.</param>
        /// <param name="fill">The text color.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">The font size (in user units).</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="rotate">The rotation angle.</param>
        /// <param name="halign">The horizontal alignment.</param>
        /// <param name="valign">The vertical alignment.</param>
        public void WriteText(
            ScreenPoint position,
            string text,
            OxyColor fill,
            string? fontFamily = null,
            double fontSize = 10,
            double fontWeight = FontWeights.Normal,
            double rotate = 0,
            HorizontalAlignment halign = HorizontalAlignment.Left,
            VerticalAlignment valign = VerticalAlignment.Top)
        {
            // http://www.w3.org/TR/SVG/text.html
            WriteStartElement("text");

            // WriteAttributeString("x", position.X);
            // WriteAttributeString("y", position.Y);
            string baselineAlignment = "hanging";
            if (valign == VerticalAlignment.Middle)
            {
                baselineAlignment = "middle";
            }

            if (valign == VerticalAlignment.Bottom)
            {
                baselineAlignment = "baseline";
            }

            WriteAttributeString("dominant-baseline", baselineAlignment);

            string textAnchor = "start";
            if (halign == HorizontalAlignment.Center)
            {
                textAnchor = "middle";
            }

            if (halign == HorizontalAlignment.Right)
            {
                textAnchor = "end";
            }

            WriteAttributeString("text-anchor", textAnchor);

            var transform = new StringBuilder(128);
            transform.Append("translate(");
            transform.AppendInvariant(position, NumberFormat);
            transform.Append(')');
            if (Math.Abs(rotate) > 0)
            {
                transform.Append(" rotate(");
                transform.AppendInvariant(rotate, "0.##");
                transform.Append(')');
            }

            WriteAttributeString("transform", transform.ToString());

            if (fontFamily != null)
            {
                WriteAttributeString("font-family", fontFamily);
            }

            if (fontSize > 0)
            {
                WriteAttributeString("font-size", fontSize);
            }

            if (fontWeight > 0)
            {
                WriteAttributeString("font-weight", fontWeight);
            }

            if (fill.IsInvisible())
            {
                WriteAttributeString("fill", "none");
            }
            else
            {
                WriteAttributeString("fill", fill.FormatColor());
                if (fill.A != 0xFF)
                {
                    WriteAttributeString("fill-opacity", fill.A / 255.0);
                }
            }

            // WriteAttributeString("style", style);
            WriteString(text);
            WriteEndElement();
        }

        /// <summary>
        /// Writes an double attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        protected void WriteAttributeString(string name, double value) => _b.AddAttribute(SequenceNumber, name, value.ToString(NumberFormat, CultureInfo.InvariantCulture));

        protected void WriteAttributeString(string prefix, string localName, string _1, string value) => _b.AddAttribute(SequenceNumber, prefix + ":" + localName, value);

        protected void WriteString(string value) => _b.AddMarkupContent(SequenceNumber, value);

        protected void WriteText(string value) => _b.AddContent(SequenceNumber, value);

        protected void WriteAttributeString(string name, string value) => _b.AddAttribute(SequenceNumber, name, value);

        /// <summary>
        /// Writes the edge rendering mode attribute if necessary.
        /// </summary>
        /// <param name="edgeRenderingMode">The edge rendering mode.</param>
        protected void WriteEdgeRenderingModeAttribute(EdgeRenderingMode edgeRenderingMode)
        {
            string value;
            switch (edgeRenderingMode)
            {
                case EdgeRenderingMode.PreferSharpness:
                    value = "crispEdges";
                    break;
                case EdgeRenderingMode.PreferSpeed:
                    value = "optimizeSpeed";
                    break;
                case EdgeRenderingMode.PreferGeometricAccuracy:
                    value = "geometricPrecision";
                    break;
                default:
                    return;
            }
            WriteAttributeString("shape-rendering", value);
        }

        protected void WriteStartElement(string name) => _b.OpenElement(SequenceNumber, name);

        protected void WriteEndElement() => _b.CloseElement();

        /// <summary>
        /// Converts a list of points to a string.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A string.</returns>
        private string PointsToString(IEnumerable<ScreenPoint> points)
        {
            var sb = new StringBuilder((points.TryGetNonEnumeratedCount(out var c) ? c : 20) * (6 + 6 + 2));
            foreach (var p in points)
            {
                if (double.IsFinite(p.X) && double.IsFinite(p.Y))
                {
                    sb.Append(p.X.ToString(NumberFormat, CultureInfo.InvariantCulture));
                    sb.Append(',');
                    sb.Append(p.Y.ToString(NumberFormat, CultureInfo.InvariantCulture));
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use a workaround for vertical text alignment to support renderers with limited support for the dominate-baseline attribute.
        /// </summary>
        public bool UseVerticalTextAlignmentWorkaround { get; set; }

        /// <summary>
        /// Draws an ellipse.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The thickness.</param>
        public override void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode) => WriteEllipse(rect.Left, rect.Top, rect.Width, rect.Height, CreateStyle(fill, stroke, thickness), edgeRenderingMode);

        /// <summary>
        /// Draws the polyline from the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <param name="aliased">if set to <c>true</c> the shape will be aliased.</param>
        public override void DrawLine(IList<ScreenPoint> points, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) => WritePolyline(points, CreateStyle(OxyColors.Undefined, stroke, thickness, dashArray, lineJoin), edgeRenderingMode);

        /// <summary>
        /// Draws the polygon from the specified points. The polygon can have stroke and/or fill.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <param name="aliased">if set to <c>true</c> the shape will be aliased.</param>
        public override void DrawPolygon(IList<ScreenPoint> points, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) => WritePolygon(points, CreateStyle(fill, stroke, thickness, dashArray, lineJoin), edgeRenderingMode);

        /// <summary>
        /// Draws the rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        public override void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode) => WriteRectangle(rect.Left, rect.Top, rect.Width, rect.Height, CreateStyle(fill, stroke, thickness), edgeRenderingMode);

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="text">The text.</param>
        /// <param name="c">The c.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="rotate">The rotate.</param>
        /// <param name="halign">The horizontal alignment.</param>
        /// <param name="valign">The vertical alignment.</param>
        /// <param name="maxSize">Size of the max.</param>
        public override void DrawText(
            ScreenPoint p,
            string text,
            OxyColor c,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign,
            OxySize? maxSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var lines = Regex.Split(text, "\r\n");

            var textSize = MeasureText(text, fontFamily, fontSize, fontWeight);
            var lineHeight = textSize.Height / lines.Length;
            var lineOffset = new ScreenVector(-Math.Sin(rotate / 180.0 * Math.PI) * lineHeight, +Math.Cos(rotate / 180.0 * Math.PI) * lineHeight);

            if (UseVerticalTextAlignmentWorkaround)
            {
                // offset the position, and set the valign to neutral value of `Bottom`
                double offsetRatio = valign == VerticalAlignment.Bottom ? (1.0 - lines.Length) : valign == VerticalAlignment.Top ? 1.0 : (1.0 - (lines.Length / 2.0));
                valign = VerticalAlignment.Bottom;

                p += lineOffset * offsetRatio;

                foreach (var line in lines)
                {
                    var size = MeasureText(line, fontFamily, fontSize, fontWeight);
                    WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                    p += lineOffset;
                }
            }
            else
            {
                if (valign == VerticalAlignment.Bottom)
                {
                    for (var i = lines.Length - 1; i >= 0; i--)
                    {
                        var line = lines[i];
                        _ = MeasureText(line, fontFamily, fontSize, fontWeight);
                        WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                        p -= lineOffset;
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        var size = MeasureText(line, fontFamily, fontSize, fontWeight);
                        WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                        p += lineOffset;
                    }
                }
            }
        }

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <returns>The text size.</returns>
        public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight)
        {
            if (string.IsNullOrEmpty(text))
            {
                return OxySize.Empty;
            }

            return TextMeasurer?.MeasureText(text, fontFamily, fontSize, fontWeight) ?? OxySize.Empty;
        }

        /// <summary>
        /// Draws the specified portion of the specified <see cref="OxyImage" /> at the specified location and with the specified size.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="srcX">The x-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcY">The y-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcWidth">Width of the portion of the source image to draw.</param>
        /// <param name="srcHeight">Height of the portion of the source image to draw.</param>
        /// <param name="destX">The x-coordinate of the upper-left corner of drawn image.</param>
        /// <param name="destY">The y-coordinate of the upper-left corner of drawn image.</param>
        /// <param name="destWidth">The width of the drawn image.</param>
        /// <param name="destHeight">The height of the drawn image.</param>
        /// <param name="opacity">The opacity.</param>
        /// <param name="interpolate">Interpolate if set to <c>true</c>.</param>
        public override void DrawImage(
            OxyImage source,
            double srcX,
            double srcY,
            double srcWidth,
            double srcHeight,
            double destX,
            double destY,
            double destWidth,
            double destHeight,
            double opacity,
            bool interpolate) => WriteImage(srcX, srcY, srcWidth, srcHeight, destX, destY, destWidth, destHeight, source);
    }
}
