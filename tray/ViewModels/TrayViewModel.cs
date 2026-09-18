using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xtreamium.Tray.Services;

namespace Xtreamium.Tray.ViewModels;

public partial class TrayViewModel : ObservableObject {
  private static readonly TimeSpan DisconnectedPromotionDelay = TimeSpan.FromSeconds(5);
  private static readonly TimeSpan ProgressTooltipThrottle = TimeSpan.FromSeconds(8);

  private readonly IClassicDesktopStyleApplicationLifetime _desktop;
  private readonly RecordingStateTracker _tracker = new();
  private readonly HttpClient _httpClient = new();
  private readonly ProxyApiClient _apiClient;
  private readonly ProxyHubClient _hubClient = new();

  private readonly WindowIcon _idleIcon;
  private readonly WindowIcon _recordingIcon;
  private readonly WindowIcon _disconnectedIcon;

  private string? _webUiUrl;
  private bool _isConfirmedConnected;
  private CancellationTokenSource? _disconnectPromotionCts;
  private DateTime _lastProgressTooltipUpdate = DateTime.MinValue;

  [ObservableProperty] private WindowIcon _iconSource;
  [ObservableProperty] private string _tooltipText = "Xtreamium — starting…";
  [ObservableProperty] private bool _isWebUiEnabled;

  public TrayViewModel(IClassicDesktopStyleApplicationLifetime desktop) {
    _desktop = desktop;
    _apiClient = new ProxyApiClient(_httpClient);

    _idleIcon = LoadIcon("tray-icon-idle.ico");
    _recordingIcon = LoadIcon("tray-icon-recording.ico");
    _disconnectedIcon = LoadIcon("tray-icon-disconnected.ico");
    _iconSource = _disconnectedIcon;

    _tracker.Changed += OnRecordingStateChanged;

    _hubClient.RecordingChanged += e => _tracker.Apply(e);
    _hubClient.RecordingProgress += OnRecordingProgress;
    _hubClient.PhaseChanged += OnPhaseChanged;
    _hubClient.Reconciling += ReconcileAsync;
    _hubClient.Start();
  }

  private static WindowIcon LoadIcon(string fileName) {
    using var stream = AssetLoader.Open(new Uri($"avares://xtreamium-tray/Assets/{fileName}"));
    return new WindowIcon(stream);
  }

  private async Task ReconcileAsync() {
    try {
      var endpoints = ProxyDiscovery.Resolve();
      var active = await _apiClient.GetActiveRecordingsAsync(endpoints.BaseUrl, CancellationToken.None);
      _tracker.Reset(active);

      var webUiUrl = endpoints.WebUiUrl;
      Dispatcher.UIThread.Post(() => {
        _webUiUrl = webUiUrl;
        IsWebUiEnabled = webUiUrl is not null;
      });
    } catch {
      // A failed reconciliation just means the next event or reconnect cycle tries again —
      // status plumbing must never surface an error to the user.
    }
  }

  private void OnPhaseChanged(HubConnectionPhase phase) {
    if (phase == HubConnectionPhase.Connected) {
      _isConfirmedConnected = true;
      CancelDisconnectPromotion();
      Dispatcher.UIThread.Post(RecomputeIcon);
      return;
    }

    _isConfirmedConnected = false;
    ArmDisconnectPromotion();
  }

  private void ArmDisconnectPromotion() {
    CancelDisconnectPromotion();
    var cts = new CancellationTokenSource();
    _disconnectPromotionCts = cts;
    _ = PromoteToDisconnectedAfterDelayAsync(cts.Token);
  }

  private async Task PromoteToDisconnectedAfterDelayAsync(CancellationToken ct) {
    try {
      await Task.Delay(DisconnectedPromotionDelay, ct);
    } catch (OperationCanceledException) {
      return;
    }

    // A brief reconnect blip resolves before this fires; only a genuinely stuck connection
    // (proxy down, or a Velopack update mid-restart) should flip the icon.
    if (!_isConfirmedConnected) {
      Dispatcher.UIThread.Post(() => {
        IconSource = _disconnectedIcon;
        TooltipText = "Xtreamium — can't reach proxy";
      });
    }
  }

  private void CancelDisconnectPromotion() {
    _disconnectPromotionCts?.Cancel();
    _disconnectPromotionCts = null;
  }

  private void OnRecordingStateChanged() {
    if (_isConfirmedConnected) {
      Dispatcher.UIThread.Post(RecomputeIcon);
    }
  }

  private void OnRecordingProgress(Models.RecordingProgressEventDto progress) {
    // Recorded on every tick regardless of the throttle below, so whichever tick does get
    // rendered reflects the current elapsed time rather than whatever was current when the
    // throttle window last opened.
    _tracker.ApplyProgress(progress);

    if (!_isConfirmedConnected) {
      return;
    }
    if (DateTime.UtcNow - _lastProgressTooltipUpdate < ProgressTooltipThrottle) {
      return;
    }
    _lastProgressTooltipUpdate = DateTime.UtcNow;
    Dispatcher.UIThread.Post(RecomputeIcon);
  }

  private void RecomputeIcon() {
    var active = _tracker.ActiveRecordings;
    IconSource = active.Count == 0 ? _idleIcon : _recordingIcon;
    TooltipText = TooltipFormatter.Format(active);
  }

  [RelayCommand]
  private void OpenWebUi() {
    if (_webUiUrl is null) {
      return;
    }
    Process.Start(new ProcessStartInfo(_webUiUrl) { UseShellExecute = true });
  }

  [RelayCommand]
  private void Quit() {
    _desktop.Shutdown();
  }
}
