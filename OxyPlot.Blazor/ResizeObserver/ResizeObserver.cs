using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using OxyPlot.Blazor.Interop;

namespace OxyPlot.Blazor.Services
{
    public class ResizeObserver : IResizeObserver, IDisposable, IAsyncDisposable
    {
        private Boolean _isDisposed = false;

        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        private readonly DotNetObjectReference<ResizeObserver> _dotNetRef;
        private readonly IJSRuntime _jsRuntime;

        private readonly Dictionary<Guid, ElementReference> _cachedValueIds = new();
        private readonly Dictionary<ElementReference, BoundingClientRect> _cachedValues = new();

        private Guid _id = Guid.NewGuid();
        private ResizeObserverOptions _options;

        [DynamicDependency(nameof(OnSizeChanged))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SizeChangeUpdateInfo))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BoundingClientRect))]
        public ResizeObserver(IJSRuntime jsRuntime, ResizeObserverOptions options)
        {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>("import", "/_content/OxyPlot.Blazor/OxyPlot.Blazor.js").AsTask());
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsRuntime = jsRuntime;
            _options = options;
        }

        public ResizeObserver(IJSRuntime jsRuntime, IOptions<ResizeObserverOptions>? options = null) : this(jsRuntime, options?.Value ?? new ResizeObserverOptions())
        {
        }

        public async Task<BoundingClientRect?> Observe(ElementReference element) => (await Observe(new[] { element })).FirstOrDefault();

        public async Task<IEnumerable<BoundingClientRect>> Observe(IEnumerable<ElementReference> elements)
        {
            var filteredElements = elements.Where(x => x.Context != null && _cachedValues.ContainsKey(x) == false).ToList();
            if (filteredElements.Any() == false)
            {
                return Array.Empty<BoundingClientRect>();
            }

            List<Guid> elementIds = new();

            foreach (var item in filteredElements)
            {
                var id = Guid.NewGuid();
                elementIds.Add(id);
                _cachedValueIds.Add(id, item);
            }
            var js = await _moduleTask.Value;
            var result = await js.InvokeAsync<IEnumerable<BoundingClientRect>>("connect", _id, _dotNetRef, filteredElements, elementIds, _options) ?? Array.Empty<BoundingClientRect>();
            var counter = 0;
            foreach (var item in result)
            {
                _cachedValues.Add(filteredElements.ElementAt(counter), item);
                counter++;
            }

            return result;
        }

        public async Task Unobserve(ElementReference element)
        {
            var elementId = _cachedValueIds.FirstOrDefault(x => x.Value.Id == element.Id).Key;
            if (elementId == default) { return; }

            //if the unobserve happens during a component teardown, the try-catch is a safe guard to prevent a "pseudo" exception
            try
            {
                var js = await _moduleTask.Value;
                await js.InvokeVoidAsync("disconnect", _id, elementId);
            }
            catch (Exception)
            {
            }

            _cachedValueIds.Remove(elementId);
            _cachedValues.Remove(element);
        }

        public bool IsElementObserved(ElementReference reference) => _cachedValues.ContainsKey(reference);

        public record SizeChangeUpdateInfo(Guid Id, BoundingClientRect Size);

        [JSInvokable]
        public void OnSizeChanged(IEnumerable<SizeChangeUpdateInfo> changes)
        {
            Dictionary<ElementReference, BoundingClientRect> parsedChanges = new();
            foreach (var item in changes)
            {
                if (_cachedValueIds.TryGetValue(item.Id, out var elementRef)) 
                {
                    _cachedValues[elementRef] = item.Size;
                    parsedChanges.Add(elementRef, item.Size);
                }
            }

            OnResized?.Invoke(parsedChanges);
        }

        public event SizeChanged? OnResized;

        public BoundingClientRect? GetSizeInfo(ElementReference reference)
        {
            if (_cachedValues.ContainsKey(reference) == false)
            {
                return null;
            }

            return _cachedValues[reference];
        }

        public double GetHeight(ElementReference reference) => GetSizeInfo(reference)?.Height ?? 0.0;
        public double GetWidth(ElementReference reference) => GetSizeInfo(reference)?.Width ?? 0.0;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _isDisposed == false)
            {
                _isDisposed = true;
                _dotNetRef.Dispose();
                _cachedValueIds.Clear();
                _cachedValues.Clear();

                //in a fire and forget manner, we just "trying" to cancel the listener. So, we are not interested in an potential error 
                try
                {
                    if (_moduleTask.Value.IsCompletedSuccessfully)
                    {
                        _ = _moduleTask.Value.Result.InvokeVoidAsync($"cancelListener", _id);
                    }
                }
                catch (Exception) { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the client element pos of the target element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="JSRuntime"></param>
        public async ValueTask SetCursor(ElementReference element, string cursorName)
        {
            var js = await _moduleTask.Value.ConfigureAwait(false);
            await js.InvokeVoidAsync("setCursor", element, cursorName).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed == true) { return; }

            _isDisposed = true;

            _dotNetRef.Dispose();
            _cachedValueIds.Clear();
            _cachedValues.Clear();

            //in a fire and forget manner, we just "trying" to cancel the listener. So, we are not interested in an potential error 
            try
            {
                var js = await _moduleTask.Value;
                await js.InvokeVoidAsync($"cancelListener", _id);
            }
            catch (Exception) { }
        }
    }
}
