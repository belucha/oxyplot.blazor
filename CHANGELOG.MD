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
