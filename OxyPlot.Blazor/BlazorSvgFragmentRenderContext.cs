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
    using System.IO;
    using System.Text;
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;
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

        /// <summary>
        /// The clip path number
        /// </summary>
        private int clipPathNumber = 1;

        private readonly RenderTreeBuilder _b;
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Sets the tooltip for the next element
        /// </summary>
        /// <param name="text"></param>
        public override void SetToolTip(string text)
        {
            title = text;
        }
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
            this.NumberFormat = "0.####";
            this.RendersToScreen = true;
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
            var style = new StringBuilder();
            if (fill.IsInvisible())
            {
                style.AppendFormat("fill:none;");
            }
            else
            {
                style.AppendFormat("fill:{0};", this.ColorToString(fill));
                if (fill.A != 0xFF)
                {
                    style.AppendFormat(CultureInfo.InvariantCulture, "fill-opacity:{0};", fill.A / 255.0);
                }
            }

            if (stroke.IsInvisible())
            {
                style.AppendFormat("stroke:none;");
            }
            else
            {
                string formatString = "stroke:{0};stroke-width:{1:" + this.NumberFormat + "}";
                style.AppendFormat(CultureInfo.InvariantCulture, formatString, this.ColorToString(stroke), thickness);
                switch (lineJoin)
                {
                    case LineJoin.Round:
                        style.AppendFormat(";stroke-linejoin:round");
                        break;
                    case LineJoin.Bevel:
                        style.AppendFormat(";stroke-linejoin:bevel");
                        break;
                }

                if (stroke.A != 0xFF)
                {
                    style.AppendFormat(CultureInfo.InvariantCulture, ";stroke-opacity:{0}", stroke.A / 255.0);
                }

                if (dashArray != null && dashArray.Length > 0)
                {
                    style.Append(";stroke-dasharray:");
                    for (int i = 0; i < dashArray.Length; i++)
                    {
                        style.AppendFormat(
                            CultureInfo.InvariantCulture, "{0}{1}", i > 0 ? "," : string.Empty, dashArray[i]);
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
            this.WriteStartElement("ellipse");
            this.WriteAttributeString("cx", x + (width / 2)); ;
            this.WriteAttributeString("cy", y + (height / 2));
            this.WriteAttributeString("rx", width / 2);
            this.WriteAttributeString("ry", height / 2);
            this.WriteAttributeString("style", style);
            this.WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            this.WriteEndElement();
        }

        protected override void SetClip(OxyRect clippingRectangle)
        {
            this.BeginClip(clippingRectangle.Left, clippingRectangle.Top, clippingRectangle.Width, clippingRectangle.Height);
        }

        /// <inheritdoc/>
        protected override void ResetClip()
        {
            this.EndClip();
        }

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
            var clipPath = $"clipPath{this.clipPathNumber++}";

            this.WriteStartElement("defs");
            this.WriteStartElement("clipPath");
            this.WriteAttributeString("id", clipPath);
            this.WriteStartElement("rect");
            this.WriteAttributeString("x", x);
            this.WriteAttributeString("y", y);
            this.WriteAttributeString("width", width);
            this.WriteAttributeString("height", height);
            this.WriteEndElement(); // rect
            this.WriteEndElement(); // clipPath
            this.WriteEndElement(); // defs

            this.WriteStartElement("g");
            this.WriteAttributeString("clip-path", $"url(#{clipPath})");
        }

        /// <summary>
        /// Resets the clipping rectangle.
        /// </summary>
        public void EndClip()
        {
            this.WriteEndElement(); // g
        }

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
            this.BeginClip(destX, destY, destWidth, destHeight);
            this.WriteImage(x, y, width, height, image);
            this.EndClip();
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
            this.WriteStartElement("image");
            this.WriteAttributeString("x", x);
            this.WriteAttributeString("y", y);
            this.WriteAttributeString("width", width);
            this.WriteAttributeString("height", height);
            this.WriteAttributeString("preserveAspectRatio", "none");
            var imageData = image.GetData();
            var encodedImage = new StringBuilder();
            encodedImage.Append("data:");
            encodedImage.Append("image/png");
            encodedImage.Append(";base64,");
            encodedImage.Append(Convert.ToBase64String(imageData));
            this.WriteAttributeString("xlink", "href", "", encodedImage.ToString());
            this.WriteEndElement();
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
                this.WriteStartElement("line");
                this.WriteAttributeString("x1", p1.X);
                this.WriteAttributeString("y1", p1.Y);
                this.WriteAttributeString("x2", p2.X);
                this.WriteAttributeString("y2", p2.Y);
                this.WriteAttributeString("style", style);
                this.WriteEndElement();
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
            this.WriteStartElement("polygon");
            this.WriteAttributeString("points", this.PointsToString(points));
            this.WriteAttributeString("style", style);
            this.WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            this.WriteTitle();
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes a polyline.
        /// </summary>
        /// <param name="pts">The points.</param>
        /// <param name="style">The style.</param>
        public void WritePolyline(IEnumerable<ScreenPoint> pts, string style, EdgeRenderingMode edgeRenderingMode)
        {
            // http://www.w3.org/TR/SVG/shapes.html#PolylineElement
            this.WriteStartElement("polyline");
            this.WriteAttributeString("points", this.PointsToString(pts));
            this.WriteAttributeString("style", style);
            this.WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            this.WriteEndElement();
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
            this.WriteStartElement("rect");
            this.WriteAttributeString("x", x);
            this.WriteAttributeString("y", y);
            this.WriteAttributeString("width", width);
            this.WriteAttributeString("height", height);
            this.WriteAttributeString("style", style);
            this.WriteEdgeRenderingModeAttribute(edgeRenderingMode);
            this.WriteTitle();
            this.WriteEndElement();
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
            this.WriteStartElement("text");

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

            this.WriteAttributeString("dominant-baseline", baselineAlignment);

            string textAnchor = "start";
            if (halign == HorizontalAlignment.Center)
            {
                textAnchor = "middle";
            }

            if (halign == HorizontalAlignment.Right)
            {
                textAnchor = "end";
            }

            this.WriteAttributeString("text-anchor", textAnchor);

            string fmt = "translate({0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "})";
            string transform = string.Format(CultureInfo.InvariantCulture, fmt, position.X, position.Y);
            if (Math.Abs(rotate) > 0)
            {
                transform += string.Format(CultureInfo.InvariantCulture, " rotate({0})", rotate);
            }

            this.WriteAttributeString("transform", transform);

            if (fontFamily != null)
            {
                this.WriteAttributeString("font-family", fontFamily);
            }

            if (fontSize > 0)
            {
                this.WriteAttributeString("font-size", fontSize);
            }

            if (fontWeight > 0)
            {
                this.WriteAttributeString("font-weight", fontWeight);
            }

            if (fill.IsInvisible())
            {
                this.WriteAttributeString("fill", "none");
            }
            else
            {
                this.WriteAttributeString("fill", this.ColorToString(fill));
                if (fill.A != 0xFF)
                {
                    this.WriteAttributeString("fill-opacity", fill.A / 255.0);
                }
            }

            // WriteAttributeString("style", style);
            this.WriteString(text);
            this.WriteEndElement();
        }

        /// <summary>
        /// Converts a color to a svg color string.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The color string.</returns>
        protected string ColorToString(OxyColor color)
        {
            if (color.Equals(OxyColors.Black))
            {
                return "black";
            }

            var formatString = "rgb({0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "},{2:" + this.NumberFormat + "})";
            return string.Format(formatString, color.R, color.G, color.B);
        }

        /// <summary>
        /// Writes an double attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        protected void WriteAttributeString(string name, double value)
        {
            _b.AddAttribute(SequenceNumber, name, value.ToString(this.NumberFormat, CultureInfo.InvariantCulture));
        }

        protected void WriteAttributeString(string prefix, string localName, string _1, string value)
        {
            _b.AddAttribute(SequenceNumber, prefix + ":" + localName, value);
        }

        protected void WriteString(string value)
        {
            _b.AddMarkupContent(SequenceNumber, value);
        }

        protected void WriteText(string value)
        {
            _b.AddContent(SequenceNumber, value);
        }

        protected void WriteAttributeString(string name, string value)
        {
            _b.AddAttribute(SequenceNumber, name, value);
        }

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

        protected void WriteStartElement(string name)
        {
            _b.OpenElement(SequenceNumber, name);
        }

        protected void WriteEndElement()
        {
            _b.CloseElement();
        }

        /// <summary>
        /// Converts a list of points to a string.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A string.</returns>
        private string PointsToString(IEnumerable<ScreenPoint> points)
        {
            var sb = new StringBuilder();
            string fmt = "{0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "} ";
            foreach (var p in points)
            {
                if (double.IsFinite(p.X) && double.IsFinite(p.Y))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, fmt, p.X, p.Y);
                }
            }
            return sb.ToString().Trim();
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
        public override void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode)
        {
            this.WriteEllipse(rect.Left, rect.Top, rect.Width, rect.Height, this.CreateStyle(fill, stroke, thickness), edgeRenderingMode);
        }

        /// <summary>
        /// Draws the polyline from the specified points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashArray">The dash array.</param>
        /// <param name="lineJoin">The line join type.</param>
        /// <param name="aliased">if set to <c>true</c> the shape will be aliased.</param>
        public override void DrawLine(IList<ScreenPoint> points, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin)
        {
            this.WritePolyline(points, this.CreateStyle(OxyColors.Undefined, stroke, thickness, dashArray, lineJoin), edgeRenderingMode);
        }

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
        public override void DrawPolygon(IList<ScreenPoint> points, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin)
        {
            this.WritePolygon(points, this.CreateStyle(fill, stroke, thickness, dashArray, lineJoin), edgeRenderingMode);
        }

        /// <summary>
        /// Draws the rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="fill">The fill color.</param>
        /// <param name="stroke">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        public override void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode)
        {
            this.WriteRectangle(rect.Left, rect.Top, rect.Width, rect.Height, this.CreateStyle(fill, stroke, thickness), edgeRenderingMode);
        }

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

            var textSize = this.MeasureText(text, fontFamily, fontSize, fontWeight);
            var lineHeight = textSize.Height / lines.Length;
            var lineOffset = new ScreenVector(-Math.Sin(rotate / 180.0 * Math.PI) * lineHeight, +Math.Cos(rotate / 180.0 * Math.PI) * lineHeight);

            if (this.UseVerticalTextAlignmentWorkaround)
            {
                // offset the position, and set the valign to neutral value of `Bottom`
                double offsetRatio = valign == VerticalAlignment.Bottom ? (1.0 - lines.Length) : valign == VerticalAlignment.Top ? 1.0 : (1.0 - (lines.Length / 2.0));
                valign = VerticalAlignment.Bottom;

                p += lineOffset * offsetRatio;

                foreach (var line in lines)
                {
                    var size = this.MeasureText(line, fontFamily, fontSize, fontWeight);
                    this.WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

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
                        _ = this.MeasureText(line, fontFamily, fontSize, fontWeight);
                        this.WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

                        p -= lineOffset;
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        var size = this.MeasureText(line, fontFamily, fontSize, fontWeight);
                        this.WriteText(p, line, c, fontFamily, fontSize, fontWeight, rotate, halign, valign);

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

            return this.TextMeasurer?.MeasureText(text, fontFamily, fontSize, fontWeight)??OxySize.Empty;
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
            bool interpolate)
        {
            this.WriteImage(srcX, srcY, srcWidth, srcHeight, destX, destY, destWidth, destHeight, source);
        }
    }
}
