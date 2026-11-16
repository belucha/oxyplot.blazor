using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace OxyPlot.Blazor;

/// <summary>
/// Renders the plot model of a <see cref="BlazorPlotView"/>.
/// </summary>
/// <remarks>
/// The plot is rendered by its own component, so that the overlays (tracker, zoom rectangle) can be updated
/// without rendering and diffing the whole plot again. The plot is rendered only when <see cref="Version"/> changes.
/// </remarks>
internal sealed class BlazorPlotSurface : ComponentBase
{
    private int _renderedVersion;

    [Parameter] public BlazorPlotView View { get; set; } = default!;

    [Parameter] public int Version { get; set; }

    protected override bool ShouldRender() => _renderedVersion != Version;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _renderedVersion = Version;
        View.RenderPlot(builder);
    }
}
