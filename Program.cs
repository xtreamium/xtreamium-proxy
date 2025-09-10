using System.Reflection;
using FluentMigrator.Runner;
using Serilog;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Hubs;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Legacy database initialization for Quartz - will be improved later
#pragma warning disable CS0618 // Type or member is obsolete
var connectionString = await DbHelper.ScaffoldDb();
#pragma warning restore CS0618 // Type or member is obsolete
if (string.IsNullOrEmpty(connectionString)) {
  throw new InvalidOperationException("Failed to scaffold Quartz database.");
}

// Configure strongly-typed configuration
builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection(AppConfiguration.SectionName));
builder.Services.Configure<CorsConfiguration>(builder.Configuration.GetSection(CorsConfiguration.SectionName));

// Add new database services
builder.Services.AddDatabase();

builder.Services.AddSignalR();

builder.Host
  .UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration))
  .ConfigureServices(((_, services) => {
    services.AddJobs(connectionString);
  }));
// CORS configuration using strongly-typed config
var corsConfig = builder.Configuration.GetSection(CorsConfiguration.SectionName).Get<CorsConfiguration>()
  ?? new CorsConfiguration();

builder.Services.AddCors(options => {
  options.AddPolicy(name: "WebFrontend", policy => {
    policy.WithOrigins(corsConfig.AllowedOrigins.ToArray())
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
  });
});

builder.Services.AddScoped<IVideoPlayerService, VideoPlayerService>();
builder.Services.AddScoped<IRecordingService, RecordingService>();
builder.Services.AddRecordVmValidator();
builder.Services.AddSettingsVmValidator();

builder.Services.AddMigrations();

var app = builder.Build();

app.MigrateDatabase();

// Use the centralized CORS policy
app.UseCors("WebFrontend");

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, Sailor!");
app.MapGet("/ping", () => new {Ping = "Pong"});

app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();
app.RegisterSettingsEndpoints();

app.Run();
