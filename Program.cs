using Quartz;
using Quartz.Simpl;
using Serilog;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Services;

var builder = WebApplication.CreateBuilder(args);
var jobsDb = await QuartzDbHelpers.ScaffoldDb();
if (string.IsNullOrEmpty(jobsDb)) {
  throw new InvalidOperationException("Failed to scaffold Quartz database.");
}

builder.Host
  .UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration))
  .ConfigureServices(((_, services) => {
    services.AddQuartz(q => {
      q.UsePersistentStore(options => {
        options.UseProperties = true;
        options.UseSerializer<BinaryObjectSerializer>();
        options.UseSQLite(jobsDb);
      });
    }).AddQuartzHostedService(options => {
      options.WaitForJobsToComplete = true;
    });
  }));
builder.Services.AddCors(options => {
  options.AddPolicy(name: "WebFrontend", policy => {
    policy.WithOrigins(
        "https://streams.dev.fergl.ie:3000/",
        "https://streams.fergl.ie",
        "https://streams.ferg.al")
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

builder.Services.AddTransient<VideoPlayerService>();
builder.Services.AddSingleton<RecordingService>();
var app = builder.Build();

app.MapGet("/", () => "Hello, Sailor!");
app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();
app.UseCors();
app.Run();
