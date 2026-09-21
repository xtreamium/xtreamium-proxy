using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Xtreamium.Tray.Services;
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
      FollowProxyLifecycle(desktop);
    }

    base.OnFrameworkInitializationCompleted();
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
