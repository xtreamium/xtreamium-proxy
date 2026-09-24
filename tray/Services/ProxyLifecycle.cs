using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace Xtreamium.Tray.Services;

public enum ProxyServiceState {
  /// <summary>Not a Windows service we can see (console/dev run, or the query itself failed).
  /// Nothing to follow, so the tray never exits on this.</summary>
  Unknown,
  Running,
  Stopped,
}

/// <summary>Decides when the tray should exit because the proxy service has gone away. Pure — no
/// clock, no OS calls — so the policy can be tested without a Windows service.
///
/// A stop only counts once it has lasted <c>stopGrace</c>: a Velopack update or a service restart
/// takes the service down for a few seconds and the tray should ride that out, whereas an
/// explicit stop stays stopped.</summary>
public sealed class ProxyLifecycleTracker(TimeSpan stopGrace) {
  private DateTimeOffset? _stoppedSince;

  /// <summary>Feed each poll result in; returns true once the tray should shut down.</summary>
  public bool ShouldExit(ProxyServiceState state, DateTimeOffset now) {
    if (state != ProxyServiceState.Stopped) {
      _stoppedSince = null;
      return false;
    }

    _stoppedSince ??= now;
    return now - _stoppedSince.Value >= stopGrace;
  }
}

/// <summary>Windows-only: ends the tray when the XtreamiumProxy service stops. The service runs in
/// session 0 with no desktop, so it can't own the tray process the way the systemd unit does on
/// Linux — the tray follows the service instead. (Starting is covered by the HKCU Run entry at
/// login and the Velopack first-run hook.)</summary>
public sealed class ProxyLifecycleMonitor(
  Func<ProxyServiceState> probe,
  Action onProxyGone,
  TimeSpan pollInterval,
  TimeSpan stopGrace) : IDisposable {
  public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(3);

  // Long enough to cover an auto-update (proxy exits, Velopack swaps files, service is restarted).
  public static readonly TimeSpan DefaultStopGrace = TimeSpan.FromSeconds(60);

  private readonly ProxyLifecycleTracker _tracker = new(stopGrace);
  private readonly CancellationTokenSource _cts = new();

  public void Start() {
    _ = RunAsync(_cts.Token);
  }

  private async Task RunAsync(CancellationToken ct) {
    using var timer = new PeriodicTimer(pollInterval);
    try {
      while (await timer.WaitForNextTickAsync(ct)) {
        if (_tracker.ShouldExit(Probe(), DateTimeOffset.UtcNow)) {
          onProxyGone();
          return;
        }
      }
    } catch (OperationCanceledException) {
      // Disposed — the tray is already shutting down.
    }
  }

  private ProxyServiceState Probe() {
    try {
      return probe();
    } catch {
      // Not being able to tell is never a reason to close the tray.
      return ProxyServiceState.Unknown;
    }
  }

  public void Dispose() {
    _cts.Cancel();
    _cts.Dispose();
  }
}

[SupportedOSPlatform("windows")]
public static class ProxyServiceProbe {
  // Must match the name UpdateManager registers with sc.exe / schtasks.exe / the registry Run key.
  public const string ServiceName = "XtreamiumProxy";

  // The name Process.GetProcessesByName expects - no ".exe", since Process.ProcessName strips it.
  private const string ProcessName = "xtreamium-proxy";

  /// <summary>Branches on which autostart mode the user picked at first run (see
  /// ProxyDiscovery.ReadProxyAutostartMode) - ScheduledTask/RunKey aren't Windows Services, so
  /// ServiceController can't see them and a running-process check is used instead. Anything else
  /// (including no marker at all, e.g. an install from before this feature existed) assumes
  /// Service, matching UpdateManager's own default for those installs.</summary>
  public static ProxyServiceState Query() {
    var mode = ProxyDiscovery.ReadProxyAutostartMode();
    return mode is "ScheduledTask" or "RunKey" ? QueryByProcess() : QueryByService();
  }

  private static ProxyServiceState QueryByService() {
    try {
      using var service = new ServiceController(ServiceName);
      return service.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending
        ? ProxyServiceState.Stopped
        : ProxyServiceState.Running;
    } catch (InvalidOperationException) {
      // Service doesn't exist (proxy run from a console) or can't be opened.
      return ProxyServiceState.Unknown;
    }
  }

  private static ProxyServiceState QueryByProcess() =>
    Process.GetProcessesByName(ProcessName).Length > 0
      ? ProxyServiceState.Running
      : ProxyServiceState.Stopped;
}
