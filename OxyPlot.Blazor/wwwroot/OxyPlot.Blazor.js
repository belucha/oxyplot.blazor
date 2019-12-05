// OxyPlot.Blazor Client Side Interop
window.OxyPlotBlazor = {
    // returns the client rect of an element reference
    getBoundingClientRect: function (e) {
        const r = e.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
    },
    removeResizeObserver: function (element) {
        console.log("unobserve");
        element.resizeObserver.unobserve(element);
        element.resizeObserver = null;
    },
    installResizeObserver: function (element, target, method) {
        // disable context menu
        element.oncontextmenu = ev => ev.preventDefault();
        // create observer
        // https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver
        const resizeObserver = new ResizeObserver(entries => {
            const r = element.getBoundingClientRect();
            target.invokeMethodAsync(method, [r.x, r.y, r.width, r.height]);
        });
        element.resizeObserver = resizeObserver;
        resizeObserver.observe(element);
        // return current size
        const r = element.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
    },
};
