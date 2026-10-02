using System.Text.Json;
using System.Text.Json.Serialization;
using DemoApp;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

// Services
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
    });

builder.Services.AddCensusServices(options =>
{
    options.LogCensusErrors = true;
});

builder.Services.AddSingleton<IWebsocketMonitor, WebsocketMonitor>();

// Uncomment the line below to demo the websocket client
//builder.Services.AddHostedService<WebsocketMonitorHostedService>();

// Build and run
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseStaticFiles();

app.MapControllers();

await app.RunAsync();
