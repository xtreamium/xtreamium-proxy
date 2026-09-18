using Avalonia;
using Xtreamium.Tray.Services;

namespace Xtreamium.Tray;

internal static class Program {
  [STAThread]
  public static void Main(string[] args) {
    AutostartInstaller.EnsureInstalled();
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
  }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
      .UsePlatformDetect()
      .LogToTrace();
}
