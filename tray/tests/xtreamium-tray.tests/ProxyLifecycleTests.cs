using Xtreamium.Tray.Services;
using Xunit;

namespace Xtreamium.Tray.Tests;

public class ProxyLifecycleTests {
  private static readonly TimeSpan Grace = TimeSpan.FromSeconds(60);
  private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void Running_service_never_exits() {
    var tracker = new ProxyLifecycleTracker(Grace);

    Assert.False(tracker.ShouldExit(ProxyServiceState.Running, T0));
    Assert.False(tracker.ShouldExit(ProxyServiceState.Running, T0.AddHours(5)));
  }

  [Fact]
  public void Stop_shorter_than_grace_is_ridden_out() {
    var tracker = new ProxyLifecycleTracker(Grace);

    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0));
    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(59)));
  }

  [Fact]
  public void Stop_lasting_the_full_grace_exits() {
    var tracker = new ProxyLifecycleTracker(Grace);

    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0));
    Assert.True(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(60)));
  }

  [Fact]
  public void Service_coming_back_resets_the_grace_window() {
    var tracker = new ProxyLifecycleTracker(Grace);

    tracker.ShouldExit(ProxyServiceState.Stopped, T0);
    tracker.ShouldExit(ProxyServiceState.Running, T0.AddSeconds(50));

    // A fresh stop starts a fresh window, even though 70s have passed since the first one.
    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(70)));
    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(120)));
    Assert.True(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(130)));
  }

  [Fact]
  public void Unknown_state_never_exits_and_clears_a_pending_stop() {
    var tracker = new ProxyLifecycleTracker(Grace);

    tracker.ShouldExit(ProxyServiceState.Stopped, T0);

    Assert.False(tracker.ShouldExit(ProxyServiceState.Unknown, T0.AddSeconds(90)));
    Assert.False(tracker.ShouldExit(ProxyServiceState.Stopped, T0.AddSeconds(100)));
  }

  [Fact]
  public async Task Monitor_calls_back_once_the_service_stays_stopped() {
    var gone = new TaskCompletionSource();
    using var monitor = new ProxyLifecycleMonitor(
      () => ProxyServiceState.Stopped,
      () => gone.TrySetResult(),
      TimeSpan.FromMilliseconds(10),
      TimeSpan.FromMilliseconds(50));

    monitor.Start();

    await gone.Task.WaitAsync(TimeSpan.FromSeconds(10));
  }

  [Fact]
  public async Task Monitor_stays_up_while_the_service_runs_and_survives_a_throwing_probe() {
    var gone = false;
    var probeCalls = 0;
    using var monitor = new ProxyLifecycleMonitor(
      () => ++probeCalls % 2 == 0 ? throw new InvalidOperationException("scm hiccup") : ProxyServiceState.Running,
      () => gone = true,
      TimeSpan.FromMilliseconds(10),
      TimeSpan.FromMilliseconds(20));

    monitor.Start();
    await Task.Delay(300);

    Assert.True(probeCalls > 3, "monitor should have kept polling");
    Assert.False(gone);
  }
}
