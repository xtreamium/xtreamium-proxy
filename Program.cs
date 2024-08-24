using Serilog;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Hubs;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

var builder = WebApplication.CreateBuilder(args);
var jobsDb = await QuartzDbHelpers.ScaffoldDb();
if (string.IsNullOrEmpty(jobsDb)) {
  throw new InvalidOperationException("Failed to scaffold Quartz database.");
}

builder.Services.AddSignalR();

builder.Host
  .UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration))
  .ConfigureServices(((_, services) => {
    services.AddJobs(jobsDb);
  }));
builder.Services.AddCors(options => {
  options.AddPolicy(name: "WebFrontend", policy => {
    policy.WithOrigins(
        "https://streams.dev.fergl.ie:3000",
        "https://streams.fergl.ie",
        "https://streams.ferg.al")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

builder.Services.AddTransient<VideoPlayerService>();
builder.Services.AddSingleton<RecordingService>();
builder.Services.AddRecordVmValidator();


var app = builder.Build();

app.UseCors(options =>
  options.WithOrigins(
      "https://streams.dev.fergl.ie:3000",
      "https://streams.fergl.ie",
      "https://streams.ferg.al")
    .AllowAnyHeader()
    .WithMethods("GET", "POST")
    .AllowCredentials());

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, Sailor!");
app.MapGet("/ping", () => new {Ping = "Pong"});

app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();

app.Run();
