// OxyPlot.Blazor Client Side Interop
export function getBoundingClientRect(e) {
    const r = e.getBoundingClientRect();
    // might return null in some cases for some values, which causes deserialization problems
    // in blazor to double.json 
    // https://github.com/belucha/oxyplot.blazor/issues/3
    // see https://developer.mozilla.org/de/docs/Web/API/Element/getBoundingClientRect
    // System.InvalidOperationException: Cannot get the value of a token type 'Null' as a number.
    return [r.x ?? 0, r.y ?? 0, r.width ?? 500, r.height ?? 500];
}

export function setCursor(element, cursorName) {
    element.style.cursor = cursorName;
}


// Copyright (c) MudBlazor 2021
// MudBlazor licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// taken from and credits go to
// https://github.com/MudBlazor/MudBlazor/blob/dev/src/MudBlazor/TScripts/mudResizeObserver.js

class MudResizeObserver {

    constructor(dotNetRef, options) {
        this.logger = options.enableLogging ? console.log : (message) => { };
        this.options = options;
        this._dotNetRef = dotNetRef

        var delay = (this.options || {}).reportRate || 200;

        this.throttleResizeHandlerId = -1;

        var observervedElements = [];
        this._observervedElements = observervedElements;

        this.logger('[OxyPlot.Blazor| ResizeObserver] Observer initialized');

        this._resizeObserver = new ResizeObserver(entries => {
            var changes = [];
            this.logger('[OxyPlot.Blazor| ResizeObserver] changes detected');
            for (let entry of entries) {
                var target = entry.target;
                var affectedObservedElement = observervedElements.find((x) => x.element == target);
                if (affectedObservedElement) {

                    var size = entry.target.getBoundingClientRect();
                    if (affectedObservedElement.isInitialized == true) {

                        changes.push({ id: affectedObservedElement.id, size: size });
                    }
                    else {
                        affectedObservedElement.isInitialized = true;
                    }
                }
            }

            if (changes.length > 0) {
                if (this.throttleResizeHandlerId >= 0) {
                    clearTimeout(this.throttleResizeHandlerId);
                }

                this.throttleResizeHandlerId = window.setTimeout(this.resizeHandler.bind(this, changes), delay);

            }
        });
    }

    resizeHandler(changes) {
        try {
            this.logger("[OxyPlot.Blazor| ResizeObserver] OnSizeChanged handler invoked");
            this._dotNetRef.invokeMethodAsync("OnSizeChanged", changes);
        } catch (error) {
            this.logger("[OxyPlot.Blazor| ResizeObserver] Error in OnSizeChanged handler:", { error });
        }
    }

    connect(elements, ids) {
        var result = [];
        this.logger('[OxyPlot.Blazor| ResizeObserver] Start observing elements...');

        for (var i = 0; i < elements.length; i++) {
            var newEntry = {
                element: elements[i],
                id: ids[i],
                isInitialized: false,
            };

            this.logger("[OxyPlot.Blazor| ResizeObserver] Start observing element:", { newEntry });

            result.push(elements[i].getBoundingClientRect());

            this._observervedElements.push(newEntry);
            this._resizeObserver.observe(elements[i]);
        }

        return result;
    }

    disconnect(elementId) {
        this.logger('[OxyPlot.Blazor| ResizeObserver] Try to unobserve element with id', { elementId });

        var affectedObservedElement = this._observervedElements.find((x) => x.id == elementId);
        if (affectedObservedElement) {

            var element = affectedObservedElement.element;
            this._resizeObserver.unobserve(element);
            this.logger('[OxyPlot.Blazor| ResizeObserver] Element found. Ubobserving size changes of element', { element });

            var index = this._observervedElements.indexOf(affectedObservedElement);
            this._observervedElements.splice(index, 1);
        }
    }

    cancelListener() {
        this.logger('[OxyPlot.Blazor| ResizeObserver] Closing ResizeObserver. Detaching all observed elements');

        this._resizeObserver.disconnect();
        this._dotNetRef = undefined;
    }
}


var _maps = {};

export function connect(id, dotNetRef, elements, elementIds, options) {
    var existingEntry = _maps[id];
    if (!existingEntry) {
        var observer = new MudResizeObserver(dotNetRef, options);
        _maps[id] = observer;
    }

    var result = _maps[id].connect(elements, elementIds);
    return result;
}

export function disconnect(id, element) {
    //I can't think about a case, where this can be called, without observe has been called before
    //however, a check is not harmful either		
    var existingEntry = _maps[id];
    if (existingEntry) {
        existingEntry.disconnect(element);
    }
}

export function cancelListener(id) {
    //cancelListener is called during dispose of .net instance
    //in rare cases it could be possible, that no object has been connect so far
    //and no entry exists. Therefore, a little check to prevent an error in this case		
    var existingEntry = _maps[id];
    if (existingEntry) {
        existingEntry.cancelListener();
        delete _maps[id];
    }
}
