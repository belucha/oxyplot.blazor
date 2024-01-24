
- add support for different line break symbols in tracker (closes #15 by archer0302)
- add net8.0 as target framework

## 2023-05-16 v2.5.7

- improve: svg clip path id is not regenerated on upon every update (only for dynamic data)

## 2023-05-15 v2.5.6

- fix: svg clip-path ids and thus urls where not unique when multiple BlazorPlotView
  where used on a page. this update uses guids to make them unique.

## 2023-05-12 v2.5.5

- fix: wrong scaling after Plot in animation (e.g. dialog popup)
   * change ResizeObserver to use the `contentRect` supplied 
     from the observer instead of calling getBoundingClientRect()

## 2023-03-02 v2.5.4

- change ResizeObserver, don't suprees first OnSizeChanged event

## 2023-03-02 v2.5.3

- change ResizeObserver namespace to allow the use of options

## 2023-02-21 v2.5.2

- use ResizeObserver instaed of Timer for size observation (copied from MudBlazor)

## 2023-02-01 v2.5.0

- fix some browser console warnings about invalid preventDefault binding
- OxyPlot ExampleLibrary Browser included in demo project
- close #8 by adding a new parameter ReverseMouseWheel to BlazorPlotView (defaults to true)
- optimize performance critical calls to StringBuilder, (avoids boxing)
- close #5 (unbind model from BlazorPlotView when disposing)

## 2023-01-09 v2.4.0

- add: keyboard support
- add: Class and Style parameters
- add: FocusAsync()
- catch right click (contextmenu)
- add: tracker formatting parameters

## 2023-01-05 v2.3.0

- net6.0 and net7.0 support
- OxyPlot 2.1.2

## 2022-02-23 v2.1.2

- sample project with .net 6
- JS Interop is done using IJSObjectReference
- requires to add `builder.Services.AddOxyPlotBlazor()` to `Program.cs` or `Startup`
- no java script include is requried

## 2022-02-06 v2.1.0

- OxyPlot.Core 2.1.0

# 2020-11-19 v2.0.2

- update to .net 6.0

# 2020-12-16 v2.0.1

- update to .net 5.0
- use OffsetX and OffsetY of MouseEventArgs to obtain client coordinates
- improve tracker positioning, so it is not hidden by mouse cursor
