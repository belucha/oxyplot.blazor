// OxyPlot.Blazor Client Side Interop
window.OxyPlotBlazor = {
    // returns the client rect of an element reference
    getBoundingClientRect: function (e) {
        const r = e.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
    },
    attachMouseHandler: function (element, eventName, target) {
        element.addEventListener(eventName, function (event) {            
            var dim = element.getBoundingClientRect();
            var x = event.clientX - dim.left;
            var y = event.clientY - dim.top;
            target.invokeMethodAsync("OnMouse", eventName, x, y, event.button, event.ctrlKey, event.altKey, event.shiftKey);
            event.preventDefault();
        });
    },
};
