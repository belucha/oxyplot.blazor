#nullable disable
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.Timers;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using OxyPlot.Series;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OxyPlot.Blazor
{
    public class BlazorPlotView : ComponentBase, IPlotView, IDisposable
    {
        const double TrackerOffset = 10.0;
        readonly Timer _timer = new(500) { Enabled = false, };
        private ElementReference _svg;
        private TrackerHitResult _tracker;
        private PlotModel _currentModel;
        private OxyRect _svgPos = new(0, 0, 0, 0);
        private IPlotController _defaultController;
        private bool _updateDataFlag = true;
        private OxyRect _zoomRectangle;
        private bool _disposed;
        private bool _preventKey;
        [Inject] OxyPlotJsInterop OxyJS { get; set; }
        [Parameter] public string PreserveAspectRation { get; set; } = "none";
        [Parameter] public string Width { get; set; }
        [Parameter] public string Height { get; set; }
        [Parameter] public string Class { get; set; }
        [Parameter] public string Style { get; set; }
        /// <summary>
        /// Set to -1 to disable keyboard binding
        /// </summary>
        [Parameter] public int TabIndex { get; set; }
        /// <summary>
        /// BackgroundColor of the Tracker (defaults to Model.PlotAreaBackground)
        /// </summary>
        [Parameter] public OxyColor? TrackerBackground { get; set; }
        /// <summary>
        /// BackgroundColor of the Tracker (defaults to Model.TextColor)
        /// </summary>
        [Parameter] public OxyColor? TrackerStrokeColor { get; set; }
        /// <summary>
        /// TextColor of the Tracker (defaults to Model.PlotAreaBackground)
        /// </summary>
        [Parameter] public OxyColor? TrackerTextColor { get; set; }
        /// <summary>
        /// tracker border stroke thickness (defaults to 0)
        /// </summary>
        [Parameter] public double TrackerStrokeThickness { get; set; } = 0.0;
        /// <summary>
        /// tracker font size (defaults to Model.DefaultFontSize)
        /// </summary>
        [Parameter] public double? TrackerFontSize { get; set; }
        /// <summary>
        /// tracker font family (defaults to Model.DefaultFontFamily)
        /// </summary>
        [Parameter] public string TrackerFontFamily { get; set; }
        [Parameter] public double TrackerFontWeight { get; set; } = 400.0;
        /// <summary>
        /// tracker padding around text (width=50, height=20)
        /// </summary>
        [Parameter] public OxySize? TrackerPadding { get; set; }
        [Parameter] public bool TrackerEnabled { get; set; } = true;
        /// <summary>
        /// Gets or sets the plot controller.
        /// </summary>
        /// <value>The controller.</value>
        [Parameter] public IPlotController Controller { get; set; }
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [Parameter][Required] public PlotModel Model { get; set; }

        async void TimerExpired(object _, EventArgs __)
        {
            if (_svg.Id == null || _disposed)
                return;
            try
            {
                await InvokeAsync(UpdateSvgBoundingRect);
            }
            catch
            {
                // swallow this one
            }
        }

        public ValueTask FocusAsync() => _svg.Id!=null?_svg.FocusAsync():ValueTask.CompletedTask;
        async void UpdateSvgBoundingRect()
        {
            if (_svg.Id == null || _disposed)
                return;
            try
            {
                var n = await OxyJS.GetBoundingClientRectAsync(_svg);
                // OxyRect.Equals is very picky
                if (false
                    || Math.Abs(n.Left - _svgPos.Left) > 0.5
                    || Math.Abs(n.Top - _svgPos.Top) > 0.5
                    || Math.Abs(n.Width - _svgPos.Width) > 0.5
                    || Math.Abs(n.Height - _svgPos.Height) > 0.5
                    )
                {
                    _svgPos = n;
                    StateHasChanged();
                }
            }
            catch
            {
                // swallow all errors
            }
        }

        /// <summary>
        /// Gets the actual model in the view.
        /// </summary>
        /// <value>
        /// The actual model.
        /// </value>
        Model IView.ActualModel => _currentModel;

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        public PlotModel ActualModel => _currentModel;

        /// <summary>
        /// Gets the actual controller.
        /// </summary>
        /// <value>
        /// The actual <see cref="IController" />.
        /// </value>
        IController IView.ActualController => ActualController;

        /// <summary>
        /// Gets the coordinates of the client area of the view.
        /// </summary>
        public OxyRect ClientArea => _svgPos;

        /// <summary>
        /// Gets the actual plot controller.
        /// </summary>
        /// <value>The actual plot controller.</value>
        public IPlotController ActualController => Controller ?? (_defaultController ??= new PlotController());

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="data">The data.</param>
        public void ShowTracker(TrackerHitResult data)
        {
            _tracker = data;
            StateHasChanged();
        }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            _tracker = null;
            StateHasChanged();
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
            _zoomRectangle = OxyRect.Create(0, 0, 0, 0);
            StateHasChanged();
        }

        /// <summary>
        /// Invalidates the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">if set to <c>true</c>, all data collections will be updated.</param>
        public void InvalidatePlot(bool updateData)
        {
            _updateDataFlag |= updateData;
            StateHasChanged();
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public async void SetCursorType(CursorType cursorType)
        {
            if (!_disposed)
            {
                await OxyJS.SetCursor(_svg, TranslateCursorType(cursorType));
            }
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public void ShowZoomRectangle(OxyRect rectangle)
        {
            _zoomRectangle = rectangle;
            StateHasChanged();
        }

        /// <summary>
        /// Sets the clipboard text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
            // not implemented
        }

        protected override void OnParametersSet()
        {
            if (_currentModel != Model)
            {
                if (_currentModel != null)
                {
                    ((IPlotModel)_currentModel).AttachPlotView(null);
                    _currentModel = null;
                }
                if (Model != null)
                {
                    ((IPlotModel)Model).AttachPlotView(this);
                    _currentModel = Model;
                }
                InvalidatePlot(true);
            }
        }

        void AddEventCallback<T>(RenderTreeBuilder builder, int sequence, string name, Action<T> callback)
        {
            builder.AddAttribute(sequence, name, EventCallback.Factory.Create<T>(this, callback));
            builder.AddEventPreventDefaultAttribute(sequence, name, true);
            builder.AddEventStopPropagationAttribute(sequence, name, true);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_currentModel == null)
            {
                _svg = new ElementReference();
                _timer.Enabled = false;
                return;
            }
            // note this gist about seequence numbers
            // https://gist.github.com/SteveSandersonMS/ec232992c2446ab9a0059dd0fbc5d0c3
            builder.OpenElement(1, "svg");
            builder.AddAttribute(2, "tabindex", TabIndex);
            if (!string.IsNullOrEmpty(Width))
            {
                builder.AddAttribute(3, "width", Width);
            }

            if (!string.IsNullOrEmpty(Height))
            {
                builder.AddAttribute(4, "height", Height);
            }
            // if the svg size is specified in pixels, we can start rendering right now
            if (_svgPos.Width == 0 && _svgPos.Height == 0
                && Width != null && Width.EndsWith("px") && int.TryParse(Width[..^-2], out var wpx) && wpx > 0
                && Height != null && Width.EndsWith("px") && int.TryParse(Height[..^-2], out var hpx) && hpx > 0)
            {
                _svgPos = new OxyRect(0, 0, wpx, hpx);
            }
            if (_svgPos.Width > 0)
            {
                builder.AddAttribute(5, "viewBox", FormattableString.Invariant($"0 0 {_svgPos.Width} {_svgPos.Height}"));
                if (!string.IsNullOrEmpty(PreserveAspectRation))
                {
                    builder.AddAttribute(6, "preserveAspectRatio", PreserveAspectRation);
                }
                // available event handlers
                // https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Web/EventHandlers.cs  
                // mouse handlers
                AddEventCallback<MouseEventArgs>(builder, 7, "onmousedown", e => ActualController.HandleMouseDown(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 8, "onmouseup", e => ActualController.HandleMouseUp(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 9, "oncontextmenu", e => ActualController.HandleMouseUp(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 10, "onmousemove", e => ActualController.HandleMouseMove(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 11, "onmouseenter", e => ActualController.HandleMouseEnter(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 12, "onmouseleave", e => ActualController.HandleMouseEnter(this, TranslateMouseEventArgs(e)));
                AddEventCallback<WheelEventArgs>(builder, 13, "onmousewheel", e => ActualController.HandleMouseWheel(this, TranslateWheelEventArgs(e)));
                // don't prevent default for onkeydown
                builder.AddAttribute(14, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownEvent));
                builder.AddEventPreventDefaultAttribute(15, "onkeydown", _preventKey);
            }
            builder.AddAttribute(15, "class", Class);
            builder.AddAttribute(16, "style", Style);
            builder.AddElementReferenceCapture(17, elementReference =>
            {
                _svg = elementReference;
                _timer.Enabled = _svg.Id != null;
            });
            if (_svgPos.Width > 0 && _currentModel is IPlotModel plotModel)
            {
                var renderer = new BlazorSvgFragmentRenderContext(builder)
                {
                    TextMeasurer = new PdfRenderContext(_svgPos.Width, _svgPos.Height, plotModel.Background),
                };

                plotModel.Update(_updateDataFlag);
                _updateDataFlag = false;

                renderer.SequenceNumber = 20;
                if (plotModel.Background != OxyColors.Transparent)
                {
                    renderer.FillRectangle(OxyRect.Create(0, 0, _svgPos.Width, _svgPos.Height), plotModel.Background, EdgeRenderingMode.Automatic);
                }
                renderer.SequenceNumber = 30;

                plotModel.Render(renderer, new OxyRect(0, 0, _svgPos.Width, _svgPos.Height));

                // zoom rectangle
                if (_zoomRectangle.Width > 0 || _zoomRectangle.Height > 0)
                {
                    renderer.SequenceNumber = 40;
                    renderer.DrawRectangle(_zoomRectangle, OxyColor.FromArgb(0x40, 0xFF, 0xFF, 0x00), OxyColors.Black, 0.5, EdgeRenderingMode.Automatic);
                }
                // tracker
                if (_tracker != null && TrackerEnabled)
                {
                    var backgroundColor = TrackerBackground ?? _currentModel.PlotAreaBackground;
                    var textColor = TrackerTextColor ?? _currentModel.TextColor;
                    var strokeColor = TrackerStrokeColor ?? _currentModel.PlotAreaBorderColor;
                    var strokeThickness = TrackerStrokeThickness;
                    var fontFamily = TrackerFontFamily ?? _currentModel.DefaultFont;
                    var fontSize = TrackerFontSize ?? _currentModel.DefaultFontSize;
                    var fontWeight = TrackerFontWeight;
                    var marigin = TrackerPadding ?? new OxySize(50, 20);
                    renderer.SequenceNumber = 50;
                    var s = renderer.MeasureText(_tracker.Text, fontFamily, fontSize, fontWeight);
                    var x = _tracker.Position.X + TrackerOffset;
                    var y = _tracker.Position.Y + TrackerOffset;
                    var w = s.Width + marigin.Width;
                    var h = s.Height + marigin.Height;
                    // check, if the tracker goes of the svg area?
                    if ((x + w) > _svgPos.Width)
                    {
                        // right side out, fix it
                        x = _tracker.Position.X - w;
                    }
                    if ((y + h) > _svgPos.Height)
                    {
                        // bottom out, fix it
                        y = _tracker.Position.Y - h;
                    }
                    // build rect and fill
                    var r = new OxyRect(x, y, w, h);
                    renderer.DrawRectangle(r, fill: backgroundColor, stroke: strokeColor, thickness: strokeThickness, EdgeRenderingMode.Automatic);
                    renderer.DrawText(
                          p: r.Center
                        , text: _tracker.Text
                        , c: textColor
                        , fontFamily: fontFamily
                        , fontSize: fontSize
                        , fontWeight: fontWeight
                        , rotate: 0
                        , halign: HorizontalAlignment.Center
                        , valign: VerticalAlignment.Middle
                        , maxSize: null
                    );
                }
            }
            builder.CloseElement();
        }
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
                _timer.Elapsed += TimerExpired;
            UpdateSvgBoundingRect();
        }

        private static OxyModifierKeys TranslateModifierKeys(MouseEventArgs e)
        {
            var result = OxyModifierKeys.None;
            if (e.ShiftKey)
                result |= OxyModifierKeys.Shift;
            if (e.AltKey)
                result |= OxyModifierKeys.Alt;
            if (e.CtrlKey)
                result |= OxyModifierKeys.Control;
            if (e.MetaKey)
                result |= OxyModifierKeys.Windows;
            return result;
        }
        private static OxyModifierKeys TranslateModifierKeys(KeyboardEventArgs e)
        {
            var result = OxyModifierKeys.None;
            if (e.ShiftKey)
                result |= OxyModifierKeys.Shift;
            if (e.AltKey)
                result |= OxyModifierKeys.Alt;
            if (e.CtrlKey)
                result |= OxyModifierKeys.Control;
            if (e.MetaKey)
                result |= OxyModifierKeys.Windows;
            return result;
        }

        private static OxyMouseButton TranslateButton(MouseEventArgs e)
            => e.Button switch
            {
                0 => OxyMouseButton.Left,
                1 => OxyMouseButton.Middle,
                2 => OxyMouseButton.Right,
                _ => OxyMouseButton.None,
            };

        private static OxyMouseDownEventArgs TranslateMouseEventArgs(MouseEventArgs e)
            => new()
            {
                Position = new ScreenPoint(e.OffsetX, e.OffsetY),
                ChangedButton = TranslateButton(e),
                ClickCount = (int)e.Detail,
                ModifierKeys = TranslateModifierKeys(e),
            };
        private static OxyMouseWheelEventArgs TranslateWheelEventArgs(WheelEventArgs e)
            => new()
            {
                Position = new ScreenPoint(e.OffsetX, e.OffsetY),
                Delta = (int)(e.DeltaY != 0 ? e.DeltaY : e.DeltaX),
                ModifierKeys = TranslateModifierKeys(e),
            };

        static OxyKey ParseKey(string key)
        {
            switch (key.Length)
            {
                case 0: // invalid
                    return OxyKey.Unknown;
                case 1: // single keys
                    switch (key[0])
                    {
                        case '*': return OxyKey.Multiply;
                        case '/': return OxyKey.Divide;
                        case '+': return OxyKey.Add;
                        case '-': return OxyKey.Subtract;
                        case char c when c >= 'a' && c <= 'z':
                            return OxyKey.A + c - 'a';
                        case char c when c >= 'A' && c <= 'Z':
                            return OxyKey.A + c - 'A';
                        case char c when c >= '0' && c <= '9':
                            return OxyKey.D0 + c - '0';
                        case ' ':
                            return OxyKey.Space;
                        default:
                            return OxyKey.Unknown;
                    }

                case 2: // function keys F1-F9
                    if (key[0] == 'F' && key[1] >= '1' && key[1] <= '9')
                        return OxyKey.F1 + key[1] - '1';
                    return OxyKey.Unknown;
                default: // Other
                    return key switch
                    {
                        "Enter" => OxyKey.Enter,
                        "Tab" => OxyKey.Tab,
                        "PageUp" => OxyKey.PageUp,
                        "PageDown" => OxyKey.PageDown,
                        "Home" => OxyKey.Home,
                        "End" => OxyKey.End,
                        "Escape" => OxyKey.Escape,
                        "Backspace" => OxyKey.Backspace,
                        "Insert" => OxyKey.Insert,
                        "Delete" => OxyKey.Delete,
                        "ArrowLeft" => OxyKey.Left,
                        "ArrowRight" => OxyKey.Right,
                        "ArrowUp" => OxyKey.Up,
                        "ArrowDown" => OxyKey.Down,
                        "F10" => OxyKey.F10,
                        "F11" => OxyKey.F11,
                        "F12" => OxyKey.F12,
                        _ => OxyKey.Unknown,
                    };
            }
        }

        private void HandleKeyDownEvent(KeyboardEventArgs e)
        {
            var oxyKey = ParseKey(e.Key);
            if (oxyKey != OxyKey.Unknown)
            {
                var oe = new OxyKeyEventArgs()
                {
                    Key = oxyKey,
                    ModifierKeys = TranslateModifierKeys(e),
                };
                _preventKey = ActualController.HandleKeyDown(this, oe);
            }
            else
            {
                _preventKey = false;
            }
        }

        /// <summary>
        /// translate OxyPlot Cursor type to browser cursor type name
        /// </summary>
        /// <see cref="https://developer.mozilla.org/de/docs/Web/CSS/cursor"/>
        /// <param name="cursorType"></param>
        /// <returns>browser css class cursor type</returns>
        private static string TranslateCursorType(CursorType cursorType) =>
            cursorType switch
            {
                CursorType.Pan => "grabbing",
                CursorType.ZoomRectangle => "zoom-in",
                CursorType.ZoomHorizontal => "col-resize",
                CursorType.ZoomVertical => "row-resize",
                CursorType.Default => "default",
                _ => "default",
            };

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer.Elapsed -= TimerExpired;
                _timer.Dispose();
                // detach model from view (closes #5)
                if (_currentModel != null)
                {
                    ((IPlotModel)_currentModel).AttachPlotView(null);
                    _currentModel = null;
                }
                GC.SuppressFinalize(this);
            }
            _disposed = true;
        }
    }
}
