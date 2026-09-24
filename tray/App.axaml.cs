using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xtreamium.Tray.Services;
using Xtreamium.Tray.ViewModels;
using Xtreamium.Tray.Views;

namespace Xtreamium.Tray;

public class App : Application {
  private const string PickAutostartModeArg = "--pick-autostart-mode";

  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      var pickerOutputPath = FindPickerOutputPath(desktop.Args);
      if (pickerOutputPath is not null) {
        ShowAutostartPicker(desktop, pickerOutputPath);
      } else {
        // No main window is ever created in normal tray-icon mode — the tray icon is the entire UI.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DataContext = new TrayViewModel(desktop);
        FollowProxyLifecycle(desktop);
      }
    }

    base.OnFrameworkInitializationCompleted();
  }

  // A one-shot invocation used only by the proxy's Velopack OnFirstRun hook (see
  // UpdateManager.PromptForAutostartMode on the proxy side): show the picker, let the user's
  // choice write itself to outputPath, then exit as soon as that window closes. This never
  // reaches the normal tray-icon startup above.
  private static string? FindPickerOutputPath(string[]? args) {
    if (args is null) {
      return null;
    }

    var flagIndex = Array.IndexOf(args, PickAutostartModeArg);
    return flagIndex >= 0 && flagIndex + 1 < args.Length ? args[flagIndex + 1] : null;
  }

  private static void ShowAutostartPicker(IClassicDesktopStyleApplicationLifetime desktop, string outputPath) {
    desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
    var window = new AutostartChoiceWindow {
      DataContext = new AutostartChoiceViewModel(outputPath)
    };
    desktop.MainWindow = window;
    window.Show();
  }

  // Linux needs nothing here — the packaged systemd unit is bound to xtreamium-proxy.service, so
  // systemd stops the tray whenever it stops the proxy. Windows has no equivalent for a per-user
  // GUI process, so the tray watches the service and shuts itself down when it goes away.
  private static void FollowProxyLifecycle(IClassicDesktopStyleApplicationLifetime desktop) {
    if (!OperatingSystem.IsWindows()) {
      return;
    }

    var monitor = new ProxyLifecycleMonitor(
      ProxyServiceProbe.Query,
      () => Dispatcher.UIThread.Post(() => desktop.Shutdown()),
      ProxyLifecycleMonitor.DefaultPollInterval,
      ProxyLifecycleMonitor.DefaultStopGrace);
    desktop.Exit += (_, _) => monitor.Dispose();
    monitor.Start();
  }
}
