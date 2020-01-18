using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OxyPlot.Blazor
{
    public static class JsInterop
    {
        /// <summary>
        /// Gets the client element pos of the target element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="JSRuntime"></param>
        /// <returns></returns>
        public static async ValueTask<OxyRect> GetBoundingClientRectAsync(this ElementReference element, IJSRuntime JSRuntime)
        {
            var o = await JSRuntime.InvokeAsync<double[]>("OxyPlotBlazor.getBoundingClientRect", element);
            return new OxyRect(left: o[0], top: o[1], width: o[2], height: o[3]);
        }

        public static ValueTask SetCursor(this ElementReference element, IJSRuntime JSRuntime, string cursor)
        {
            return JSRuntime.InvokeVoidAsync("OxyPlotBlazor.setCursor", element, cursor);
        }

        public static ValueTask InstallSizeChangedListener<T>(this ElementReference element, IJSRuntime JSRuntime, DotNetObjectReference<T> dotNetObjectReference, string nameOfSizeChangedCallback) where T: class
        {
            return JSRuntime.InvokeVoidAsync("OxyPlotBlazor.installResizeObserver", element, dotNetObjectReference, nameOfSizeChangedCallback);
        }
        public static ValueTask UninstallSizeChangedListener(this ElementReference element, IJSRuntime JSRuntime) 
            => JSRuntime.InvokeVoidAsync("OxyPlotBlazor.removeResizeObserver", element);

        public static ValueTask<double[]> InstallCallback<TValue>(this ElementReference element, IJSRuntime JSRuntime, object receiver, Action<TValue> callback)
        {
            var cb = EventCallback.Factory.Create<TValue>(receiver, callback);
            return JSRuntime.InvokeAsync<double[]>("OxyPlotBlazor.installResizeObserver", element);
        }
    }
}
