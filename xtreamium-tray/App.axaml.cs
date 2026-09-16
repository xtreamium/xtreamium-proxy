using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Xtreamium.Tray.ViewModels;

namespace Xtreamium.Tray;

public class App : Application {
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted() {
    // No main window is ever created — the tray icon is the entire UI.
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
      DataContext = new TrayViewModel(desktop);
    }

    base.OnFrameworkInitializationCompleted();
  }
}
