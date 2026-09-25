using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        IgnoreTrayIconShutdownCancellation();
        DataContext = new TrayViewModel(desktop);
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

  // On Linux, Avalonia's D-Bus tray icon cancels its own watcher when it's disposed at shutdown,
  // and that TaskCanceledException surfaces on the dispatcher as unhandled. Left alone it aborts
  // the process (SIGABRT), which systemd's Restart=on-failure treats as a crash - so quitting from
  // the tray menu just brought the tray straight back. Only that one exception is swallowed; any
  // other unhandled exception still crashes (and restarts) as before.
  private static void IgnoreTrayIconShutdownCancellation() {
    Dispatcher.UIThread.UnhandledException += (_, e) => {
      if (e.Exception is OperationCanceledException &&
          e.Exception.StackTrace?.Contains("DBusTrayIconImpl") == true) {
        e.Handled = true;
      }
    };
  }
}
