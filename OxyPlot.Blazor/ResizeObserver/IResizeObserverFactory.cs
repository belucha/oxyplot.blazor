namespace OxyPlot.Blazor.Services
{
    public interface IResizeObserverFactory
    {
        IResizeObserver Create(ResizeObserverOptions options);
        IResizeObserver Create();
    }
}
