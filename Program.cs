using CrystalQuartz.AspNetCore;
using Dapper;
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

// Configure Dapper type handlers for SQLite compatibility
SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());

// Handle Velopack events first (Windows only)
if (OperatingSystem.IsWindows()) {
  UpdateManager.HandleVelopackEvents();
}

var builder = WebApplication.CreateBuilder(args);

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

// Register validators
builder.Services.AddRecordVmValidator();
builder.Services.AddSettingsVmValidator();

// Register update manager (Windows only)
if (OperatingSystem.IsWindows()) {
  builder.Services.AddSingleton<UpdateManager>();
}

builder.Services.AddSignalR();

var app = builder.Build();

// Initialize Quartz tables if needed
var dbFile = connectionString.Replace("Data Source=", "");
await app.InitializeConfigurationDbAsync(dbFile);

app.MigrateDatabase();

app.UseCors("WebFrontend");

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, Sailor!");
app.MapGet("/ping", () => new {Ping = "Pong"});

app.RegisterVersionEndpoints();
app.RegisterPlayerEndpoints();
app.RegisterRecordEndpoints();
app.RegisterSettingsEndpoints();
app.UseCrystalQuartz(() => app
  .Services.GetRequiredService<ISchedulerFactory>()
  .GetScheduler());

// Start update check in background (Windows only)
if (OperatingSystem.IsWindows()) {
  _ = Task.Run(async () => {
    await Task.Delay(TimeSpan.FromMinutes(1)); // Wait 1 minute after startup
    var updateManager = app.Services.GetRequiredService<UpdateManager>();

    try {
      if (await updateManager.CheckForUpdatesAsync()) {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Update available. Downloading...");

        if (await updateManager.DownloadAndInstallUpdatesAsync()) {
          logger.LogInformation("Update installed. Application will restart.");
          // Velopack will automatically restart the app
        }
      }
    } catch (Exception ex) {
      var logger = app.Services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "Error during update check");
    }
  });
}

app.Run();
