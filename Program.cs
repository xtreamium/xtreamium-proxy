using CrystalQuartz.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Endpoints;
using Xtreamium.Proxy.Hubs;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

// Handle --version flag
if (args.Contains("--version")) {
  Console.WriteLine(VersionHelper.GetVersion());
  return;
}

// Handle Velopack events first (Windows only)
if (OperatingSystem.IsWindows()) {
  UpdateManager.HandleVelopackEvents();
}

var builder = WebApplication.CreateBuilder(args);

// Configure log directory in the application data folder
var logDirectory = Path.Combine(
  Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
  "xtreamium",
  "logs");
Directory.CreateDirectory(logDirectory);

// Set the log path for Serilog
builder.Configuration["Serilog:WriteTo:1:Args:path"] = Path.Combine(logDirectory, "applog-.txt");

builder.Services.Configure<AppConfiguration>(builder.Configuration.GetSection(AppConfiguration.SectionName));
builder.Services.Configure<CorsConfiguration>(builder.Configuration.GetSection(CorsConfiguration.SectionName));

builder.Services.AddDatabase();
builder.Services.AddMigrations();

if (OperatingSystem.IsLinux()) {
  builder.Host.UseSystemd();
}

if (OperatingSystem.IsWindows()) {
  builder.Host.UseWindowsService();
}

builder.Host.UseSerilog((context, configuration) =>
  configuration.ReadFrom.Configuration(context.Configuration));

var connectionString = DatabaseServiceExtensions.GetConfigurationDbConnectionString();

builder.Services.AddJobs(connectionString);

var corsConfig =
  builder
    .Configuration
    .GetSection(CorsConfiguration.SectionName).Get<CorsConfiguration>()
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
builder.Services.AddScoped<ILogService, LogService>();

// Register validators
builder.Services.AddRecordVmValidator();
builder.Services.AddSettingsVmValidator();

// Register update manager (Windows only)
if (OperatingSystem.IsWindows()) {
  builder.Services.AddSingleton<UpdateManager>();
}

builder.Services.AddSignalR();

// Get the port from settings and configure the server URLs BEFORE building
var defaultPort = GetPortFromSettings(connectionString).Result;
builder.WebHost.UseUrls($"http://0.0.0.0:{defaultPort}");

var app = builder.Build();

// Initialize Quartz tables if needed
var dbFile = connectionString.Replace("Data Source=", "");
await app.InitializeConfigurationDbAsync(dbFile);

app.MigrateDatabase();

app.UseCors("WebFrontend");

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, Sailor!");
app.MapGet("/ping", () => new {Ping = "Pong"});
app.MapGet("/config", async ([FromServices] Microsoft.Extensions.Options.IOptions<AppConfiguration> config) => {
  return new {
    Port = config.Value.Networking.Port
  };
});

app.RegisterBrowseEndpoints();
app.RegisterVersionEndpoints();
app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();
app.RegisterSettingsEndpoints();
app.RegisterLogEndpoints();
app.UseCrystalQuartz(() => app
  .Services.GetRequiredService<ISchedulerFactory>()
  .GetScheduler());


// Start update check in background (Windows only)
if (OperatingSystem.IsWindows()) {
  var logger = app.Services.GetRequiredService<ILogger<Program>>();
  _ = Task.Run(async () => {
    await Task.Delay(TimeSpan.FromMinutes(1)); // Wait 1 minute after startup
    var updateManager = app.Services.GetRequiredService<UpdateManager>();

    try {
      if (await updateManager.CheckForUpdatesAsync()) {
        logger.LogInformation("Update available. Downloading...");

        if (await updateManager.DownloadAndInstallUpdatesAsync()) {
          logger.LogInformation("Update installed. Application will restart");
          // Velopack will automatically restart the app
        }
      }
    } catch (Exception ex) {
      logger.LogError(ex, "Error during update check");
    }
  });
}

app.Run();
return;

// Helper function to read port from settings database
// Kinda ick to do it here, but we need the port before building the app
async Task<int> GetPortFromSettings(string dbConnectionString) {
  try {
    await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(dbConnectionString);
    await connection.OpenAsync();

    const string sql = "SELECT Value FROM settings WHERE Key = 'Port' LIMIT 1";
    using var command = connection.CreateCommand();
    command.CommandText = sql;

    var result = await command.ExecuteScalarAsync();
    if (result is string portValue && int.TryParse(portValue, out var port)) {
      return port;
    }
  } catch {
    // If database doesn't exist or query fails, fall through to default
  }

  // Fallback to 5000
  return 5000;
}

