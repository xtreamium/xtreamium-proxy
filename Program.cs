using Serilog;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Hubs;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Configure strongly-typed configuration
builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection(AppConfiguration.SectionName));
builder.Services.Configure<CorsConfiguration>(builder.Configuration.GetSection(CorsConfiguration.SectionName));

// Add database services (includes DatabaseInitializer)
builder.Services.AddDatabase();
builder.Services.AddMigrations();

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Get the connection string for Quartz jobs configuration
// We need to build a temporary service provider to initialize the database
using (var tempServices = builder.Services.BuildServiceProvider())
{
  var connectionString = await tempServices.InitializeDatabaseAsync();
  if (string.IsNullOrEmpty(connectionString)) {
    throw new InvalidOperationException("Failed to initialize database.");
  }

  // Now we can configure Quartz jobs with the connection string
  builder.Services.AddJobs(connectionString);
}

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
builder.Services.AddSignalR();

var app = builder.Build();

// Run database migrations
app.MigrateDatabase();

// Use the centralized CORS policy
app.UseCors("WebFrontend");

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, Sailor!");
app.MapGet("/ping", () => new { Ping = "Pong" });

app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();
app.RegisterSettingsEndpoints();

app.Run();
