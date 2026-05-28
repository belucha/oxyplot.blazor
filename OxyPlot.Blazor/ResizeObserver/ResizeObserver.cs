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
    public class ResizeObserver : IResizeObserver, IAsyncDisposable
    {
        private bool _disposed = false;

        private readonly DotNetObjectReference<ResizeObserver> _dotNetRef;
        private IJSObjectReference? _module;
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
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsRuntime = jsRuntime;
            _options = options;
        }

        // Access it safely anywhere via a property or method
        private async ValueTask<IJSObjectReference> GetModuleAsync()
        {
            // Return cached reference if it already exists
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/OxyPlot.Blazor/OxyPlot.Blazor.js"
            ).ConfigureAwait(false);
            return _module;
        }

        public ResizeObserver(IJSRuntime jsRuntime, IOptions<ResizeObserverOptions>? options = null) : this(jsRuntime, options?.Value ?? new ResizeObserverOptions())
        {
        }

        public async Task<BoundingClientRect?> Observe(ElementReference element) => (await Observe([element]).ConfigureAwait(false)).FirstOrDefault();

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
            var js = await GetModuleAsync().ConfigureAwait(false);
            var result = await js.InvokeAsync<IEnumerable<BoundingClientRect>>("connect", _id, _dotNetRef, filteredElements, elementIds, _options).ConfigureAwait(false) ?? Array.Empty<BoundingClientRect>();
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
                var js = await GetModuleAsync().ConfigureAwait(false);
                await js.InvokeVoidAsync("disconnect", _id, elementId).ConfigureAwait(false);
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

        /// <summary>
        /// Gets the client element pos of the target element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="JSRuntime"></param>
        public async ValueTask SetCursor(ElementReference element, string cursorName)
        {
            if (!_disposed)
            {
                var js = await GetModuleAsync().ConfigureAwait(false);
                await js.InvokeVoidAsync("setCursor", element, cursorName).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed == true) { return; }

            _disposed = true;

            //in a fire and forget manner, we just "trying" to cancel the listener. So, we are not interested in an potential error 
            try
            {
                if (_module != null)
                {
                    await _module.InvokeVoidAsync($"cancelListener", _id).ConfigureAwait(false);
                    await _module.DisposeAsync().ConfigureAwait(false);
                    _module = null;
                }

            }
            catch (Exception)
            {
                // Suppress all JS interop exceptions (including disconnections and runtime errors)
                // to prevent an execution halt from leaking the .NET references below.
            }

            _dotNetRef.Dispose();
            _cachedValueIds.Clear();
            _cachedValues.Clear();
        }
    }
}
