using CrystalQuartz.AspNetCore;
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

// Refuse to run as root on Linux — this is a per-user service and must never
// touch files as uid 0.
if (OperatingSystem.IsLinux() && Environment.UserName == "root") {
  Console.Error.WriteLine("xtreamium-proxy must not be run as root. Use 'systemctl --user' or run as your desktop user.");
  return;
}

// Handle Velopack events first (Windows only)
if (OperatingSystem.IsWindows()) {
  UpdateManager.HandleVelopackEvents();
}

Directory.CreateDirectory(AppPaths.AppDataDirectory);
Directory.CreateDirectory(AppPaths.LogsDirectory);

// Seed appsettings.json into the user config dir on first run, so the user has
// a writable copy they can tweak. Source order: a sibling appsettings.json next
// to the binary (dev / publish output), then /usr/share/xtreamium-proxy/appsettings.json.example
// (system package install).
if (!File.Exists(AppPaths.AppSettingsPath)) {
  var seedCandidates = new[] {
    Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
    "/usr/share/xtreamium-proxy/appsettings.json.example",
  };
  foreach (var seed in seedCandidates) {
    if (File.Exists(seed)) {
      File.Copy(seed, AppPaths.AppSettingsPath);
      break;
    }
  }
}

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json from the user config dir, not the binary's working directory.
// This is what lets us ship the binary in /usr/bin and keep all user state in $HOME.
builder.Configuration.Sources.Clear();
builder.Configuration
  .AddJsonFile(AppPaths.AppSettingsPath, optional: true, reloadOnChange: true)
  .AddEnvironmentVariables()
  .AddCommandLine(args);

// Set the log path for Serilog
builder.Configuration["Serilog:WriteTo:1:Args:path"] = Path.Combine(AppPaths.LogsDirectory, "applog-.txt");

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

builder.Services.AddHttpClient("StreamPassthrough", c => {
  c.Timeout = Timeout.InfiniteTimeSpan;
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

// Configure server URLs using AppConfiguration (from appsettings.json)
var appConfig = builder.Configuration.GetSection(AppConfiguration.SectionName).Get<AppConfiguration>()
  ?? new AppConfiguration();
var port = appConfig.Networking.Port;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Initialise Quartz tables if needed
var dbFile = connectionString.Replace("Data Source=", "");
await app.InitializeConfigurationDbAsync(dbFile);

app.MigrateDatabase();

app.UseCors("WebFrontend");

app.MapHub<ProxyStatusHub>("/hubs/proxyStatus");
app.MapGet("/", () => "Hello, asdsadsSailor!");
app.MapGet("/ping", () => new {Ping = "pongpingpong"});

app.RegisterBrowseEndpoints();
app.RegisterVersionEndpoints();
app.RegisterPlayerEndpoints();
app.RegisterStreamEndpoints();
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
