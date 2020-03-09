// OxyPlot.Blazor Client Side Interop
window.OxyPlotBlazor = {
    getBoundingClientRect: function (e) {
        const r = e.getBoundingClientRect();
        return [r.x, r.y, r.width, r.height];
    },
    disableContextMenu: function (element) {
        element.addEventListener('contextmenu', ev => {
            ev.preventDefault();
            return false;
        }
        );
    },
    setCursor: function (element, cursorName) {
        element.style.cursor = cursorName;
    },
};
