// OxyPlot.Blazor Client Side Interop
export function getBoundingClientRect(e) {
    const r = e.getBoundingClientRect();
    // might return null in some cases for some values, which causes deserialization problems
    // in blazor to double.json 
    // https://github.com/belucha/oxyplot.blazor/issues/3
    // see https://developer.mozilla.org/de/docs/Web/API/Element/getBoundingClientRect
    // System.InvalidOperationException: Cannot get the value of a token type 'Null' as a number.
    return [r.x ?? 0, r.y ?? 0, r.width ?? 0, r.height ?? 0];
}

export function disableContextMenu(element) {
    element.addEventListener('contextmenu', ev => {
        ev.preventDefault();
        return false;
    }
    );
}

export function setCursor(element, cursorName) {
    element.style.cursor = cursorName;
}
