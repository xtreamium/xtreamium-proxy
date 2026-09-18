namespace Xtreamium.Proxy.Configuration;

public static class AppPaths {
  public const string DirectoryName = "xtreamium-proxy";

  public static string AppDataDirectory => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    DirectoryName);

  public static string AppSettingsPath => Path.Combine(AppDataDirectory, "appsettings.json");

  public static string LogsDirectory => Path.Combine(AppDataDirectory, "logs");
}
