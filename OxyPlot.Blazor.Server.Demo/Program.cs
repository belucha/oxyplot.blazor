using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OxyPlot.Blazor.Server.Demo;
using OxyPlot.Blazor.Server.Demo.Data;
using OxyPlot.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<ExampleService>();
builder.Services.AddOptions<ResizeObserverOptions>().Configure(options => options.EnableLogging = true);
builder.Services.AddOxyPlotBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
