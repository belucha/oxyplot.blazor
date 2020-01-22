// OxyPlot.Blazor Client Side Interop
window.OxyPlotBlazor = {
    observers: 0,
    // returns the client rect of an element reference
    getBoundingClientRect: function (e) {
        const r = e.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
    },
    removeResizeObserver: function (element) {
        if (element && element.resizeObserver) {
            window.OxyPlotBlazor.observers--;
            //console.log("uninstall resize observer", window.OxyPlotBlazor.observers)
            element.resizeObserver.unobserve(element);
            element.resizeObserver = null;
        }
    },
    setCursor: function (element, cursorName) {
        element.style.cursor = cursorName;
    },
    installResizeObserver: function (element, target, method) {
        if (!element.resizeObserver) {
            window.OxyPlotBlazor.observers++;
            //console.log("install resize observer", window.OxyPlotBlazor.observers)
            /*
            // DISABLE SCROLL
            // https://github.com/WICG/EventListenerOptions/blob/gh-pages/explainer.md
            // Test via a getter in the options object to see if the passive property is accessed
            var supportsPassive = false;
            try {
                var opts = Object.defineProperty({}, 'passive',
                {
                    get: function () {
                        supportsPassive = true;
                    }
                });
                window.addEventListener("testPassive", null, opts);
                window.removeEventListener("testPassive", null, opts);
            } catch (e) { }
            // Use our detect's results. passive applied if supported, capture will be false either way.
                if (ev.shiftKey) {
                    ev.preventDefault()
                }
            }, supportsPassive ? { passive: true } : false); 
            */
            // disable context menu
            element.addEventListener('contextmenu', ev => {
                ev.preventDefault();
                return false;
            }); 
            // create observer
            // https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver
            const resizeObserver = new ResizeObserver(entries => {
                const r = element.getBoundingClientRect();
                target.invokeMethodAsync(method, [r.x, r.y, r.width, r.height]);
            });
            element.resizeObserver = resizeObserver;
            resizeObserver.observe(element);
        }
        // return current size
        const r = element.getBoundingClientRect();
        target.invokeMethodAsync(method, [r.x, r.y, r.width, r.height]);
    },
};
