using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace OxyPlot.Blazor
{
    public class BlazorPlotView : ComponentBase, IPlotView, IDisposable
    {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Parameter] public string Width { get; set; } = "100%";
        [Parameter] public string Height { get; set; } = "500px";

        private DotNetObjectReference<BlazorPlotView> _self;
        private ElementReference _svg;
        private OxyRect _svgPos = new OxyRect(0, 0, 0, 0);

        [JSInvokable]
        public void UpdateSvgBoundingRect(double[] pos)
        {
            var n = new OxyRect(pos[0], pos[1], pos[2], pos[3]);
            // OxyRect.Equals is very picky
            if (false
                || Math.Abs(n.Left - _svgPos.Left) > 0.5
                || Math.Abs(n.Top - _svgPos.Top) > 0.5
                || Math.Abs(n.Width - _svgPos.Width) > 0.5
                || Math.Abs(n.Height - _svgPos.Height) > 0.5
                )
            {
                System.Diagnostics.Debug.WriteLine($"New svg pos {_svgPos}!={n}");
                _svgPos = n;
                StateHasChanged();
            }
        }
        /// <summary>
        /// The current model (holding a reference to this plot view).
        /// </summary>
        private PlotModel currentModel;

        /// <summary>
        /// The is model invalidated.
        /// </summary>
        private bool isModelInvalidated;

        /// <summary>
        /// The model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The default controller.
        /// </summary>
        private IPlotController defaultController;

        /// <summary>
        /// The update data flag.
        /// </summary>
        private bool updateDataFlag = true;

        /// <summary>
        /// The zoom rectangle.
        /// </summary>
        private OxyRect zoomRectangle;

        /// <summary>
        /// Gets the actual model in the view.
        /// </summary>
        /// <value>
        /// The actual model.
        /// </value>
        Model IView.ActualModel => Model;

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        public PlotModel ActualModel => Model;

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
        public OxyRect ClientArea => OxyRect.Create(0, 0, _svgPos.Width, _svgPos.Height);

        /// <summary>
        /// Gets the actual plot controller.
        /// </summary>
        /// <value>The actual plot controller.</value>
        public IPlotController ActualController => this.Controller ?? (this.defaultController ?? (this.defaultController = new PlotController()));

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [Parameter]
        public PlotModel Model
        {
            get => model;
            set
            {
                if (this.model != value)
                {
                    this.model = value;
                    this.OnModelChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the plot controller.
        /// </summary>
        /// <value>The controller.</value>
        [Parameter] public IPlotController Controller { get; set; }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            /*
            if (this.trackerLabel != null)
            {
                this.trackerLabel.Visible = false;
            }
            */
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
            this.zoomRectangle = OxyRect.Create(0, 0, 0, 0);
            this.Invalidate();
        }

        protected void Invalidate()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Invalidates the plot (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">if set to <c>true</c>, all data collections will be updated.</param>
        public void InvalidatePlot(bool updateData)
        {
            this.updateDataFlag |= updateData;
            this.Invalidate();
        }

        /// <summary>
        /// Called when the Model property has been changed.
        /// </summary>
        public void OnModelChanged()
        {
            if (this.currentModel != null)
            {
                ((IPlotModel)this.currentModel).AttachPlotView(null);
                this.currentModel = null;
            }

            if (this.Model != null)
            {
                ((IPlotModel)this.Model).AttachPlotView(this);
                this.currentModel = this.Model;

                this.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public void SetCursorType(CursorType cursorType)
        {
            // TODO: call JSInterop to set cursor
        }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="data">The data.</param>
        public void ShowTracker(TrackerHitResult data)
        {
            // TODO: show hide tracker
            /*
            if (this.trackerLabel == null)
            {
                this.trackerLabel = new Label { Parent = this, BackColor = Color.LightSkyBlue, AutoSize = true, Padding = new Padding(5) };
            }

            this.trackerLabel.Text = data.ToString();
            this.trackerLabel.Top = (int)data.Position.Y - this.trackerLabel.Height;
            this.trackerLabel.Left = (int)data.Position.X - (this.trackerLabel.Width / 2);
            this.trackerLabel.Visible = true;
            */
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        public void ShowZoomRectangle(OxyRect rectangle)
        {
            this.zoomRectangle = rectangle;
            this.Invalidate();
        }

        /// <summary>
        /// Sets the clipboard text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
            // TODO: set clipboardtext
        }

        void AddEventCallback<T>(RenderTreeBuilder builder, int sequence, string name, Action<T> callback)
        {
            builder.AddEventPreventDefaultAttribute(sequence, name, true);
            builder.AddEventStopPropagationAttribute(sequence, name, true);
            builder.AddAttribute(sequence, name, EventCallback.Factory.Create<T>(this, callback));
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // note this gist about seequence numbers
            // https://gist.github.com/SteveSandersonMS/ec232992c2446ab9a0059dd0fbc5d0c3
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "width", Width);
            builder.AddAttribute(2, "height", Height);
            if (_svgPos.Width > 0)
            {
                builder.AddAttribute(1, "viewBox", FormattableString.Invariant($"0 0 {_svgPos.Width} {_svgPos.Height}"));
                // available event handlers
                // https://github.com/aspnet/AspNetCore/blob/master/src/Components/Web/ref/Microsoft.AspNetCore.Components.Web.netcoreapp.cs
                // mouse handlers
                AddEventCallback<MouseEventArgs>(builder, 5, "onmousedown", e => ActualController.HandleMouseDown(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 5, "onmousemove", e => ActualController.HandleMouseMove(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 5, "onmouseup", e => ActualController.HandleMouseUp(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 5, "onmousein", e => ActualController.HandleMouseEnter(this, TranslateMouseEventArgs(e)));
                AddEventCallback<MouseEventArgs>(builder, 5, "onmouseout", e => ActualController.HandleMouseEnter(this, TranslateMouseEventArgs(e)));
                // wheel, can't prevent default
                builder.AddAttribute(5, "onmousewheel", EventCallback.Factory.Create<WheelEventArgs>(this, e => ActualController.HandleMouseWheel(this, TranslateWheelEventArgs(e))));
                // keyboard handlers
                AddEventCallback<KeyboardEventArgs>(builder, 5, "onkeydown", e => ActualController.HandleKeyDown(this, TranslateKeyEventArgs(e)));
                // todo: add missing gesture support
            }
            builder.AddElementReferenceCapture(8, elementReference => _svg = elementReference);
            if (_svgPos.Width > 0)
            {
                var model = ((IPlotModel)this.currentModel);
                var renderer = new BlazorSvgFragmentRenderContext(builder)
                {
                    TextMeasurer = new PdfRenderContext(_svgPos.Width, _svgPos.Height, model?.Background ?? OxyColors.Transparent),
                };

                if (model != null)
                {
                    model.Update(updateDataFlag);
                    updateDataFlag = false;

                    renderer.SequenceNumber = 11;
                    if (model.Background != OxyColors.Transparent)
                    {
                        renderer.FillRectangle(ClientArea, model.Background);
                    }
                    renderer.SequenceNumber = 10;
                    model.Render(renderer, _svgPos.Width, _svgPos.Height);
                }
                // zoom rectangle
                if (this.zoomRectangle.Width > 0 || this.zoomRectangle.Height > 0)
                {
                    renderer.SequenceNumber = 15;
                    renderer.DrawRectangle(zoomRectangle, OxyColor.FromArgb(0x40, 0xFF, 0xFF, 0x00), OxyColors.Black, 0.5);
                }
            }
            builder.CloseElement();
        }

        void IDisposable.Dispose()
        {
            if (_self != null)
            {
                JSRuntime.InvokeVoidAsync("OxyPlotBlazor.removeResizeObserver", _svg);
                _self.Dispose();
                _self = null;
            }

        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _self = DotNetObjectReference.Create(this);
                UpdateSvgBoundingRect(await JSRuntime.InvokeAsync<double[]>("OxyPlotBlazor.installResizeObserver", _svg, _self, nameof(UpdateSvgBoundingRect)));
            }
        }

        /*
                /// <summary>
                /// Raises the <see cref="E:System.Windows.Forms.Control.PreviewKeyDown" /> event.
                /// </summary>
                /// <param name="e">A <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs" /> that contains the event data.</param>
                protected void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
                {
                    base.OnPreviewKeyDown(e);

                    var args = new OxyKeyEventArgs { ModifierKeys = GetModifiers(), Key = e.KeyCode.Convert() };
                    this.ActualController.HandleKeyDown(this, args);
                }

        */
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
        {
            switch (e.Button)
            {
                case 0: return OxyMouseButton.Left;
                case 1: return OxyMouseButton.Middle;
                case 2: return OxyMouseButton.Right;
                default: return OxyMouseButton.None;
            }
        }

        private OxyMouseDownEventArgs TranslateMouseEventArgs(MouseEventArgs e)
            => new OxyMouseDownEventArgs
            {
                Position = new ScreenPoint(e.ClientX - _svgPos.Left, e.ClientY - _svgPos.Top),
                ChangedButton = TranslateButton(e),
                ClickCount = (int)e.Detail,
                ModifierKeys = TranslateModifierKeys(e),
            };
        private OxyMouseWheelEventArgs TranslateWheelEventArgs(WheelEventArgs e)
            => new OxyMouseWheelEventArgs
            {
                Position = new ScreenPoint(e.ClientX - _svgPos.Left, e.ClientY - _svgPos.Top),
                Delta = (int)(e.DeltaY != 0 ? e.DeltaY : e.DeltaX),
                ModifierKeys = TranslateModifierKeys(e),
            };

        private OxyKeyEventArgs TranslateKeyEventArgs(KeyboardEventArgs e)
            => new OxyKeyEventArgs
            {
                Key = Enum.TryParse<OxyKey>(e.Key, true, out var oxyKey) ? oxyKey : OxyKey.Unknown,
                ModifierKeys = TranslateModifierKeys(e),
            };
    }
}
