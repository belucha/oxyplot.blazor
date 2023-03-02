using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OxyPlot.Blazor.Interop;

namespace OxyPlot.Blazor.Services
{

    public delegate void SizeChanged(IDictionary<ElementReference, BoundingClientRect> changes);

    public interface IResizeObserver : IAsyncDisposable, IDisposable
    {
        Task<BoundingClientRect?> Observe(ElementReference element);
        Task<IEnumerable<BoundingClientRect>> Observe(IEnumerable<ElementReference> elements);
        Task Unobserve(ElementReference element);
        double GetWidth(ElementReference reference);
        double GetHeight(ElementReference reference);
        BoundingClientRect? GetSizeInfo(ElementReference reference);

        event SizeChanged? OnResized;
        bool IsElementObserved(ElementReference reference);
        ValueTask SetCursor(ElementReference element, string cursorName);
    }
}
