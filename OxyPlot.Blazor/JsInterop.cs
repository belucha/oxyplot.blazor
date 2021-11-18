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
        public static async ValueTask<OxyRect> GetBoundingClientRectAsync(this ElementReference element, IJSRuntime JSRuntime)
        {
            try
            {
                var o = await JSRuntime.InvokeAsync<double[]>("OxyPlotBlazor.getBoundingClientRect", element).ConfigureAwait(false);
                return new OxyRect(left: o[0], top: o[1], width: o[2], height: o[3]);
            } catch (TaskCanceledException)
            {
                // just some dummy default, we want to avoid exceptions
                return new OxyRect(left: 0, top: 0, width: 500, height: 300);
            }
        }
        public static ValueTask DisableContextMenu(this ElementReference element, IJSRuntime JSRuntime)
            => JSRuntime.InvokeVoidAsync("OxyPlotBlazor.disableContextMenu", element);
        public static ValueTask SetCursor(this ElementReference element, IJSRuntime JSRuntime, string cursorName)
            => JSRuntime.InvokeVoidAsync("OxyPlotBlazor.setCursor", element, cursorName);
    }
}
