using Xtreamium.Proxy.Data.Models;
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
      MediaPlayerPath = settings.MediaPlayerPath,
      MediaPlayerArguments = settings.MediaPlayerArguments,
      RecordingsPath = settings.RecordingsPath,
      Port = settings.Port
    };
  }

  public static async Task WriteSettings(SettingsVm request) {
    if (_repository == null) {
      throw new InvalidOperationException("SettingsHelper not initialized. Use ISettingsRepository directly instead.");
    }

    var settings = new Setting {
      MediaPlayerPath = request.MediaPlayerPath,
      MediaPlayerArguments = request.MediaPlayerArguments,
      RecordingsPath = request.RecordingsPath,
      Port = request.Port
    };

    await _repository.UpdateOrCreateSettingsAsync(settings);
  }
}
