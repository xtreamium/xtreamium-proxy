using Quartz;
using Serilog;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Services;

var builder = WebApplication.CreateBuilder(args);
if (!await QuartzDbHelpers.ScaffoldDb()) {
  throw new InvalidOperationException("Failed to scaffold Quartz database.");
}

builder.Host
  .UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration))
  .ConfigureServices(((hostContext, services) => {
    services.AddQuartz(q => { }).AddQuartzHostedService(options => {
      options.WaitForJobsToComplete = true;
    });
  }));

builder.Services.AddTransient<VideoPlayerService>();
var app = builder.Build();

app.MapGet("/", () => "Hello, Sailor!");
app.RegisterPlayerEndpoints();
app.Run();
