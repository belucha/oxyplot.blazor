# OxyPlot Blazor Component

## Requirements

- dotNet Core 3.1

## Usage

Add Package Reference to OxyPlot.Core
Add `OxyPlot.Blazor` library

add script reference to `_Host.cshtml` after `<app></app>` tag
```html
    <script src="_content/OxyPlot.Blazor/OxyPlot.Blazor.js"></script>
```

```
@using OxyPlot.Blazor
<BlazorPlotView Width="100%" Height="300px" Model="_model1" />
@code {
	IPlotModel _model = ... // some model
}
```

## Known issues

- No keyboard binding
- Images anoations don't work yet (I do not understand that, they are just not visible)
- Tracker is an extra `<div/>` - not pretty, only shown, when Parameter `TrackerEnabled` is set to true