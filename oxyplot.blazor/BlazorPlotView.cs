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

        private ElementReference _svg;

        private OxyRect _svgPos = new OxyRect(0, 0, 800, 600);

        /// <summary>
        /// The category for the properties of this control.
        /// </summary>
        private const string OxyPlotCategory = "OxyPlot";

        /// <summary>
        /// The invalidate lock.
        /// </summary>
        private readonly object invalidateLock = new object();

        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The rendering lock.
        /// </summary>
        private readonly object renderingLock = new object();

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
        public OxyRect ClientArea => _svgPos;

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
        public IPlotController Controller { get; set; }

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
            lock (this.invalidateLock)
            {
                this.isModelInvalidated = true;
                this.updateDataFlag = this.updateDataFlag || updateData;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Called when the Model property has been changed.
        /// </summary>
        public void OnModelChanged()
        {
            lock (this.modelLock)
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
                }
            }

            this.InvalidatePlot(true);
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

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var w = new RenderTreeSvgWriter(builder)
            {
                Capture = svgRef => _svg = svgRef,
            };
            w.TextMeasurer = new PdfRenderContext(_svgPos.Width, _svgPos.Height, model.Background);
            var r = new BlazorSvgRenderContext(w);
            w.WriteHeader(Width, Height, _svgPos.Width, _svgPos.Height);
            ((IPlotModel)this.currentModel).Update(true);
            ((IPlotModel)this.currentModel).Render(r, _svgPos.Width, _svgPos.Height);
            r.Complete();
        }

        BlazorMouseEventHandler _mouseDown = new BlazorMouseEventHandler();
        DotNetObjectReference<BlazorMouseEventHandler> _mouseDownJsRef;

        void IDisposable.Dispose()
        {
            _mouseDownJsRef.Dispose();
        }
        class BlazorMouseEventHandler
        {
            
            [JSInvokable] public void OnMouse(string eventName, double x, double y, int button, bool ctrl, bool alt, bool shift)
            {
                Console.WriteLine($"{eventName}, {x}, {y}, {button}, {ctrl}, {alt}, {shift}");
            }
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _mouseDownJsRef = DotNetObjectReference.Create(_mouseDown);
                await JSRuntime.InvokeVoidAsync("OxyPlotBlazor.attachMouseHandler", _svg, "mousedown", _mouseDownJsRef).AsTask();
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseDown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        [JSInvokable]
        public void OnMouseDown(MouseEventArgs e)
        {
            var oe = TranslateMouseEventArgs(e);
            this.ActualController.HandleMouseDown(this, oe);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseMove" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        [JSInvokable]
        public void OnMouseMove(MouseEventArgs e)
        {
            var oe = TranslateMouseEventArgs(e);
            this.ActualController.HandleMouseMove(this, oe);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        [JSInvokable]
        public void OnMouseUp(MouseEventArgs e)
        {
            var oe = TranslateMouseEventArgs(e);
            this.ActualController.HandleMouseUp(this, oe);
        }

        /*
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseEnter" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.ActualController.HandleMouseEnter(this, e.ToMouseEventArgs(GetModifiers()));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.ActualController.HandleMouseLeave(this, e.ToMouseEventArgs(GetModifiers()));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseWheel" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            this.ActualController.HandleMouseWheel(this, e.ToMouseWheelEventArgs(GetModifiers()));
        }
        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try
            {
                lock (this.invalidateLock)
                {
                    if (this.isModelInvalidated)
                    {
                        if (this.model != null)
                        {
                            ((IPlotModel)this.model).Update(this.updateDataFlag);
                            this.updateDataFlag = false;
                        }

                        this.isModelInvalidated = false;
                    }
                }

                lock (this.renderingLock)
                {
                    this.renderContext.SetGraphicsTarget(e.Graphics);

                    if (this.model != null)
                    {
                        if (!this.model.Background.IsUndefined())
                        {
                            using (var brush = new SolidBrush(this.model.Background.ToColor()))
                            {
                                e.Graphics.FillRectangle(brush, e.ClipRectangle);
                            }
                        }

                        ((IPlotModel)this.model).Render(this.renderContext, this.Width, this.Height);
                    }

                    if (this.zoomRectangle != Rectangle.Empty)
                    {
                        using (var zoomBrush = new SolidBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0x00)))
                        using (var zoomPen = new Pen(Color.Black))
                        {
                            zoomPen.DashPattern = new float[] { 3, 1 };
                            e.Graphics.FillRectangle(zoomBrush, this.zoomRectangle);
                            e.Graphics.DrawRectangle(zoomPen, this.zoomRectangle);
                        }
                    }
                }
            }
            catch (Exception paintException)
            {
                var trace = new StackTrace(paintException);
                Debug.WriteLine(paintException);
                Debug.WriteLine(trace);
                using (var font = new Font("Arial", 10))
                {
                    e.Graphics.ResetTransform();
                    e.Graphics.DrawString(
                        "OxyPlot paint exception: " + paintException.Message, font, Brushes.Red, this.Width * 0.5f, this.Height * 0.5f, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }
        }

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

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Resize" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Disposes the PlotView.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources or not.</param>
        protected override void Dispose(bool disposing)
        {
            bool disposed = this.IsDisposed;

            base.Dispose(disposing);

            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                this.renderContext.Dispose();
            }
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
    }
}
