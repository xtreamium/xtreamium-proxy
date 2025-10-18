using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Data.Services;

/// <summary>
/// Helper service for settings operations - will be replaced by direct repository usage
/// </summary>
[Obsolete("Use ISettingsRepository directly instead")]
public static class SettingsHelper {
  private static ISettingsRepository? _repository;

  public static void Initialize(ISettingsRepository repository) {
    _repository = repository;
  }

  public static async Task<SettingsVm> GetSettings() {
    if (_repository == null) {
      throw new InvalidOperationException("SettingsHelper not initialized. Use ISettingsRepository directly instead.");
    }

    var settings = await _repository.GetSettingsAsync();
    return new SettingsVm {
      MediaPlayerPath = settings.GetValueOrDefault("MediaPlayerPath", ""),
      MediaPlayerArguments = settings.GetValueOrDefault("MediaPlayerArguments", ""),
      RecordingsPath = settings.GetValueOrDefault("RecordingsPath", ""),
      Port = int.TryParse(settings.GetValueOrDefault("Port", "8963"), out var port) ? port : 8963
    };
  }

  public static async Task WriteSettings(SettingsVm request) {
    if (_repository == null) {
      throw new InvalidOperationException("SettingsHelper not initialized. Use ISettingsRepository directly instead.");
    }

    var settings = new Dictionary<string, string> {
      { "MediaPlayerPath", request.MediaPlayerPath },
      { "MediaPlayerArguments", request.MediaPlayerArguments },
      { "RecordingsPath", request.RecordingsPath },
      { "Port", request.Port.ToString() }
    };

    await _repository.UpdateOrCreateSettingsAsync(settings);
  }
}
