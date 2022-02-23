using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OxyPlot.Blazor
{
    internal class OxyPlotJsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        public OxyPlotJsInterop(IJSRuntime jsRuntime)
        {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/OxyPlot.Blazor/OxyPlot.Blazor.js").AsTask());
        }

        /// <summary>
        /// Gets the client element pos of the target element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="JSRuntime"></param>
        public async ValueTask<OxyRect> GetBoundingClientRectAsync(ElementReference element)
        {
            try
            {
                var js = await _moduleTask.Value.ConfigureAwait(false);
                var o = await js.InvokeAsync<double[]>("getBoundingClientRect", element).ConfigureAwait(false);
                return new OxyRect(left: o[0], top: o[1], width: o[2], height: o[3]);
            }
            catch (TaskCanceledException)
            {
                // just some dummy default, we want to avoid exceptions
                return new OxyRect(left: 0, top: 0, width: 500, height: 300);
            }
        }
        public async ValueTask DisableContextMenu(ElementReference element)
        {
            var js = await _moduleTask.Value.ConfigureAwait(false);
            await js.InvokeVoidAsync("disableContextMenu", element).ConfigureAwait(false);
        }
        public async ValueTask SetCursor(ElementReference element, string cursorName)
        {
            var js = await _moduleTask.Value.ConfigureAwait(false);
            await js.InvokeVoidAsync("setCursor", element, cursorName).ConfigureAwait(false);
        }
        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
