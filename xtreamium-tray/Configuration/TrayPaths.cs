namespace Xtreamium.Tray.Configuration;

public static class TrayPaths {
  public const string DirectoryName = "xtreamium-tray";

  public static string AppDataDirectory => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    DirectoryName);

  public static string LogsDirectory => Path.Combine(AppDataDirectory, "logs");

  public static string SettingsOverridePath => Path.Combine(AppDataDirectory, "settings.json");

  public static string AutostartMarkerPath => Path.Combine(AppDataDirectory, "autostart-installed");
}
