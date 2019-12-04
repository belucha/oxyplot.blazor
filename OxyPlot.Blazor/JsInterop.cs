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
        public static async Task<OxyRect> GetBoundingClientRectAsync(this ElementReference element, IJSRuntime JSRuntime)
        {
            var o = await JSRuntime.InvokeAsync<double[]>("OxyPlotBlazor.getBoundingClientRect", element).AsTask();
            return new OxyRect(left: o[0], top: o[1], width: o[2], height: o[3]);
        }
    }
}
