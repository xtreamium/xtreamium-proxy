using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Data.Services;

/// <summary>
/// Helper service for settings operations - will be replaced by direct repository usage
/// </summary>
[Obsolete("Use ISettingsRepository directly instead")]
public static class SettingsHelper {
  private static ISettingsRepository? _repository;
  private static AppConfiguration? _config;

  public static void Initialize(ISettingsRepository repository, IOptions<AppConfiguration> config) {
    _repository = repository;
    _config = config.Value;
  }

  public static async Task<SettingsVm> GetSettings() {
    if (_repository == null || _config == null) {
      throw new InvalidOperationException("SettingsHelper not initialized. Use ISettingsRepository directly instead.");
    }

    var settings = await _repository.GetSettingsAsync();
    return new SettingsVm {
      MediaPlayerPath = settings.GetValueOrDefault("MediaPlayerPath", ""),
      MediaPlayerArguments = settings.GetValueOrDefault("MediaPlayerArguments", ""),
      RecordingsPath = settings.GetValueOrDefault("RecordingsPath", ""),
      Port = int.TryParse(settings.GetValueOrDefault("Port", _config.Networking.Port.ToString()), out var port) ? port : _config.Networking.Port
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
