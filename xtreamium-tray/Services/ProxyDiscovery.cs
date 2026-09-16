using Microsoft.Extensions.Configuration;
using Xtreamium.Tray.Configuration;

namespace Xtreamium.Tray.Services;

/// <summary>Where xtreamium-proxy actually is right now, resolved fresh on every (re)connect
/// attempt so a proxy restart on a different port is picked up without restarting the tray.</summary>
public record ProxyEndpoints(int Port, string? WebUiUrl) {
  public string BaseUrl => $"http://127.0.0.1:{Port}";
}

public static class ProxyDiscovery {
  private const int DefaultPort = 8963;

  // Mirrors Xtreamium.Proxy.Configuration.AppPaths.AppSettingsPath exactly, just pointed at the
  // proxy's own app-data directory instead of the tray's. Reading the proxy's live config here is
  // what lets the tray find it with zero changes to the proxy itself.
  private static string ProxyAppSettingsPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "xtreamium-proxy", "appsettings.json");

  public static ProxyEndpoints Resolve() {
    var port = DefaultPort;
    string? webUiUrl = null;

    if (File.Exists(ProxyAppSettingsPath)) {
      var config = new ConfigurationBuilder()
        .AddJsonFile(ProxyAppSettingsPath, optional: true)
        .Build();
      port = config.GetValue<int?>("App:Networking:Port") ?? DefaultPort;
      webUiUrl = config.GetSection("Cors:AllowedOrigins").Get<string[]>()?.FirstOrDefault();
    }

    // Escape hatch for anyone whose real web UI isn't the first configured CORS origin.
    if (File.Exists(TrayPaths.SettingsOverridePath)) {
      var overrideConfig = new ConfigurationBuilder()
        .AddJsonFile(TrayPaths.SettingsOverridePath, optional: true)
        .Build();
      webUiUrl = overrideConfig["WebUiUrl"] ?? webUiUrl;
    }

    return new ProxyEndpoints(port, webUiUrl);
  }
}
